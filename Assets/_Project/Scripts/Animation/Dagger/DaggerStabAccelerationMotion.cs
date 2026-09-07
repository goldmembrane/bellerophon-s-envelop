using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    // Repeats the authored stab while matching the design delay ramp over a five-second hold preview.
    public sealed class DaggerStabAccelerationMotion : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float sourceClipLength = 2.7f;
        [SerializeField] private float skipTriggerNormalizedTime = 31f / 81f;
        [SerializeField] private float resumeNormalizedTime = 69f / 81f;
        [SerializeField] private float accelerationDurationSeconds = 4f;
        [SerializeField] private float patternDurationSeconds = 5f;
        [SerializeField] private float initialAttackIntervalSeconds = 2f;
        [SerializeField] private float minimumAttackIntervalSeconds = 1.5f;

        private float patternStartTime;

        public float PatternElapsed { get; private set; }
        public int PatternCycle { get; private set; }
        public float CurrentAttackInterval { get; private set; }
        public float CurrentPlaybackSpeed { get; private set; }
        public float AccelerationDurationSeconds => accelerationDurationSeconds;
        public float PatternDurationSeconds => patternDurationSeconds;

        public void Configure(
            Animator targetAnimator,
            float clipLength,
            float skipNormalizedTime,
            float resumeAtNormalizedTime,
            float accelerationSeconds,
            float patternSeconds,
            float initialIntervalSeconds,
            float minimumIntervalSeconds)
        {
            animator = targetAnimator;
            sourceClipLength = Mathf.Max(0.01f, clipLength);
            skipTriggerNormalizedTime = Mathf.Clamp01(skipNormalizedTime);
            resumeNormalizedTime = Mathf.Clamp(resumeAtNormalizedTime, skipTriggerNormalizedTime, 1f);
            accelerationDurationSeconds = Mathf.Max(0.01f, accelerationSeconds);
            patternDurationSeconds = Mathf.Max(accelerationDurationSeconds, patternSeconds);
            initialAttackIntervalSeconds = Mathf.Max(0.01f, initialIntervalSeconds);
            minimumAttackIntervalSeconds = Mathf.Clamp(minimumIntervalSeconds, 0.01f, initialAttackIntervalSeconds);
        }

        private void OnEnable()
        {
            patternStartTime = Time.time;
            ApplyPlaybackSpeed();
        }

        private void Update()
        {
            ApplyPlaybackSpeed();
        }

        private void OnDisable()
        {
            if (animator != null) animator.speed = 1f;
        }

        private void ApplyPlaybackSpeed()
        {
            if (animator == null) return;

            float elapsed = Mathf.Max(0f, Time.time - patternStartTime);
            PatternCycle = Mathf.FloorToInt(elapsed / patternDurationSeconds);
            PatternElapsed = Mathf.Repeat(elapsed, patternDurationSeconds);
            float accelerationProgress = Mathf.Clamp01(PatternElapsed / accelerationDurationSeconds);
            CurrentAttackInterval = Mathf.Lerp(
                initialAttackIntervalSeconds,
                minimumAttackIntervalSeconds,
                accelerationProgress);

            float playedNormalizedLength = skipTriggerNormalizedTime + (1f - resumeNormalizedTime);
            float playedSourceDuration = sourceClipLength * playedNormalizedLength;
            CurrentPlaybackSpeed = playedSourceDuration / CurrentAttackInterval;
            animator.speed = CurrentPlaybackSpeed;
        }
    }
}
