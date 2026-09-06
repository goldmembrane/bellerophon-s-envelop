using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class BatonDischargeExitHoldStateBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Animator targetAnimator;

        [SerializeField]
        private string modeHoldStateName;

        [SerializeField]
        private string sourceReverseStateName;

        [SerializeField]
        private string transitionReverseStateName;

        [SerializeField]
        private string idleHoldStateName;

        [SerializeField, Min(0f)]
        private float modeHoldSeconds;

        [SerializeField, Min(0.0001f)]
        private float sourceReverseSeconds;

        [SerializeField, Min(0f)]
        private float sourceReverseFastTailSeconds;

        [SerializeField, Range(0f, 1f)]
        private float sourceReverseFastTailEndNormalizedTime;

        [SerializeField, Min(0.0001f)]
        private float transitionReverseSeconds;

        [SerializeField, Min(0f)]
        private float idleHoldSeconds;

        private double cycleStartTime;
        private int modeHoldStateHash;
        private int sourceReverseStateHash;
        private int transitionReverseStateHash;
        private int idleHoldStateHash;

        public Animator TargetAnimator => targetAnimator;
        public string ModeHoldStateName => modeHoldStateName;
        public string SourceReverseStateName => sourceReverseStateName;
        public string TransitionReverseStateName => transitionReverseStateName;
        public string IdleHoldStateName => idleHoldStateName;
        public float ModeHoldSeconds => modeHoldSeconds;
        public float SourceReverseSeconds => sourceReverseSeconds;
        public float SourceReverseFastTailSeconds =>
            sourceReverseFastTailSeconds;
        public float SourceReverseFastTailEndNormalizedTime =>
            sourceReverseFastTailEndNormalizedTime;
        public float TransitionReverseSeconds => transitionReverseSeconds;
        public float IdleHoldSeconds => idleHoldSeconds;
        public float CycleDurationSeconds =>
            modeHoldSeconds + sourceReverseSeconds +
            transitionReverseSeconds + idleHoldSeconds;

        public void Configure(
            Animator animator,
            string modeHoldState,
            string sourceReverseState,
            string transitionReverseState,
            string idleHoldState,
            float requestedModeHoldSeconds,
            float requestedSourceReverseSeconds,
            float requestedSourceReverseFastTailSeconds,
            float requestedSourceReverseFastTailEndNormalizedTime,
            float requestedTransitionReverseSeconds,
            float requestedIdleHoldSeconds)
        {
            targetAnimator = animator;
            modeHoldStateName = modeHoldState;
            sourceReverseStateName = sourceReverseState;
            transitionReverseStateName = transitionReverseState;
            idleHoldStateName = idleHoldState;
            modeHoldSeconds = Mathf.Max(0f, requestedModeHoldSeconds);
            sourceReverseSeconds = Mathf.Max(
                0.0001f,
                requestedSourceReverseSeconds);
            // The supplied Mixamo Take ends with a long, visually unchanged
            // settling motion. This preserves those source poses in order but
            // compresses that tail so the explicit mode hold reads as 0.2 s.
            sourceReverseFastTailSeconds = Mathf.Clamp(
                requestedSourceReverseFastTailSeconds,
                0f,
                sourceReverseSeconds);
            sourceReverseFastTailEndNormalizedTime = Mathf.Clamp01(
                requestedSourceReverseFastTailEndNormalizedTime);
            transitionReverseSeconds = Mathf.Max(
                0.0001f,
                requestedTransitionReverseSeconds);
            idleHoldSeconds = Mathf.Max(0f, requestedIdleHoldSeconds);
            CacheStateHashes();
        }

        private void OnEnable()
        {
            CacheStateHashes();
            cycleStartTime = Time.timeAsDouble;
            EvaluateCurrentTime();
        }

        private void Update()
        {
            EvaluateCurrentTime();
        }

        private void CacheStateHashes()
        {
            modeHoldStateHash = Animator.StringToHash(modeHoldStateName);
            sourceReverseStateHash = Animator.StringToHash(
                sourceReverseStateName);
            transitionReverseStateHash = Animator.StringToHash(
                transitionReverseStateName);
            idleHoldStateHash = Animator.StringToHash(idleHoldStateName);
        }

        private void EvaluateCurrentTime()
        {
            if (targetAnimator == null)
            {
                targetAnimator = GetComponent<Animator>();
            }

            float cycleDuration = CycleDurationSeconds;
            if (targetAnimator == null || cycleDuration <= 0f)
            {
                return;
            }

            double elapsed = System.Math.Max(
                0d,
                Time.timeAsDouble - cycleStartTime);
            float cycleTime = (float)(elapsed % cycleDuration);
            if (cycleTime < modeHoldSeconds)
            {
                targetAnimator.Play(modeHoldStateHash, 0, 1f);
                return;
            }

            cycleTime -= modeHoldSeconds;
            if (cycleTime < sourceReverseSeconds)
            {
                float normalizedTime;
                if (sourceReverseFastTailSeconds > 0.0001f &&
                    cycleTime < sourceReverseFastTailSeconds)
                {
                    float fastProgress =
                        cycleTime / sourceReverseFastTailSeconds;
                    normalizedTime = Mathf.Lerp(
                        1f,
                        sourceReverseFastTailEndNormalizedTime,
                        fastProgress);
                }
                else
                {
                    float remainingSeconds = Mathf.Max(
                        0.0001f,
                        sourceReverseSeconds -
                        sourceReverseFastTailSeconds);
                    float remainingProgress = Mathf.Clamp01(
                        (cycleTime - sourceReverseFastTailSeconds) /
                        remainingSeconds);
                    normalizedTime = Mathf.Lerp(
                        sourceReverseFastTailEndNormalizedTime,
                        0f,
                        remainingProgress);
                }

                targetAnimator.Play(
                    sourceReverseStateHash,
                    0,
                    normalizedTime);
                return;
            }

            cycleTime -= sourceReverseSeconds;
            if (cycleTime < transitionReverseSeconds)
            {
                float progress = cycleTime / transitionReverseSeconds;
                targetAnimator.Play(
                    transitionReverseStateHash,
                    0,
                    1f - progress);
                return;
            }

            targetAnimator.Play(idleHoldStateHash, 0, 0f);
        }
    }

}
