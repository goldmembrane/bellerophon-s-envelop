using System;
using UnityEngine;

namespace Bellerophon.Enemies.Fuga
{
    public enum FugaHitReactionDirection
    {
        Left,
        Right,
    }

    public static class FugaHitReactionDirectionSelector
    {
        public static FugaHitReactionDirection Select(float unitSample)
        {
            if (float.IsNaN(unitSample) || float.IsInfinity(unitSample) || unitSample < 0f || unitSample > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(unitSample),
                    unitSample,
                    "The random sample must be in [0, 1].");
            }

            return unitSample < 0.5f
                ? FugaHitReactionDirection.Left
                : FugaHitReactionDirection.Right;
        }
    }

    public static class FugaHitReactionReplayClock
    {
        public const float IntervalSeconds = 1.1f;

        public static int Advance(ref float elapsedSeconds, float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds),
                    deltaSeconds,
                    "The replay-clock delta must be finite and non-negative.");
            }

            elapsedSeconds += deltaSeconds;
            var completedCycles = Mathf.FloorToInt((elapsedSeconds + 0.000001f) / IntervalSeconds);
            if (completedCycles > 0)
            {
                elapsedSeconds -= completedCycles * IntervalSeconds;
            }

            return completedCycles;
        }
    }

    [DisallowMultipleComponent]
    public sealed class FugaHitReactionRandomDriver : MonoBehaviour
    {
        public const string LeftStateName = "Fuga_Hit_NewModel_Left";
        public const string RightStateName = "Fuga_Hit_NewModel_Right";

        [SerializeField] private Animator animator;
        [SerializeField] private FugaHitReactionDirection lastSelectedDirection;
        [SerializeField] private bool repeatPlayback = true;

        public Animator Animator => animator;
        public FugaHitReactionDirection LastSelectedDirection => lastSelectedDirection;
        public bool RepeatPlayback => repeatPlayback;

        private float replayElapsedSeconds;

        public void Configure(Animator configuredAnimator)
        {
            Configure(configuredAnimator, repeatPlayback);
        }

        public void Configure(Animator configuredAnimator, bool configuredRepeatPlayback)
        {
            animator = configuredAnimator != null
                ? configuredAnimator
                : throw new ArgumentNullException(nameof(configuredAnimator));
            repeatPlayback = configuredRepeatPlayback;
            replayElapsedSeconds = 0f;
        }

        // Call once for every received hit so each reaction gets a fresh 50:50 direction sample.
        public void PlayHitReaction()
        {
            PlayHitReactionFromUnitSample(UnityEngine.Random.value, resetReplayClock: true);
        }

        public void PlayHitReactionFromUnitSample(float unitSample)
        {
            PlayHitReactionFromUnitSample(unitSample, resetReplayClock: true);
        }

        private void PlayHitReactionFromUnitSample(float unitSample, bool resetReplayClock)
        {
            if (animator == null)
            {
                throw new InvalidOperationException("The Fuga hit-reaction Animator is not configured.");
            }

            lastSelectedDirection = FugaHitReactionDirectionSelector.Select(unitSample);
            var stateName = lastSelectedDirection == FugaHitReactionDirection.Left
                ? LeftStateName
                : RightStateName;
            var stateHash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, stateHash))
            {
                throw new InvalidOperationException("The Fuga hit-reaction controller is missing state " + stateName + ".");
            }

            animator.Play(stateHash, 0, 0f);
            if (resetReplayClock)
            {
                replayElapsedSeconds = 0f;
            }
        }

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void OnEnable()
        {
            replayElapsedSeconds = 0f;
            if (Application.isPlaying)
            {
                PlayHitReaction();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || !repeatPlayback)
            {
                return;
            }

            var completedCycles = FugaHitReactionReplayClock.Advance(ref replayElapsedSeconds, Time.deltaTime);
            for (var index = 0; index < completedCycles; index++)
            {
                // Every completed 1.1-second cycle receives a fresh random direction without losing clock remainder.
                PlayHitReactionFromUnitSample(UnityEngine.Random.value, resetReplayClock: false);
            }
        }
    }
}
