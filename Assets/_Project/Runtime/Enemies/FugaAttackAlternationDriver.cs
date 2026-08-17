using System;
using UnityEngine;

namespace Bellerophon.Enemies.Fuga
{
    public enum FugaAttackStartingWing
    {
        Left,
        Right,
    }

    public static class FugaAttackStartingWingSelector
    {
        public static FugaAttackStartingWing Select(float unitSample)
        {
            if (float.IsNaN(unitSample) || float.IsInfinity(unitSample) || unitSample < 0f || unitSample > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(unitSample), unitSample, "The random sample must be in [0, 1].");
            }

            return unitSample < 0.5f
                ? FugaAttackStartingWing.Left
                : FugaAttackStartingWing.Right;
        }
    }

    [DisallowMultipleComponent]
    public sealed class FugaAttackAlternationDriver : MonoBehaviour
    {
        public const string LeftFirstStateName = "Fuga_Attack_NewModel_LeftFirst";
        public const string RightFirstStateName = "Fuga_Attack_NewModel_RightFirst";

        [SerializeField] private Animator animator;
        [SerializeField] private FugaAttackStartingWing lastSelectedStartingWing;

        public Animator Animator => animator;
        public FugaAttackStartingWing LastSelectedStartingWing => lastSelectedStartingWing;

        public void Configure(Animator configuredAnimator)
        {
            animator = configuredAnimator != null
                ? configuredAnimator
                : throw new ArgumentNullException(nameof(configuredAnimator));
        }

        public void StartAttackSequence()
        {
            StartAttackSequenceFromUnitSample(UnityEngine.Random.value);
        }

        public void StartAttackSequenceFromUnitSample(float unitSample)
        {
            if (animator == null)
            {
                throw new InvalidOperationException("The Fuga attack Animator is not configured.");
            }

            lastSelectedStartingWing = FugaAttackStartingWingSelector.Select(unitSample);
            var stateName = lastSelectedStartingWing == FugaAttackStartingWing.Left
                ? LeftFirstStateName
                : RightFirstStateName;
            var stateHash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, stateHash))
            {
                throw new InvalidOperationException("The Fuga attack controller is missing state " + stateName + ".");
            }

            animator.Play(stateHash, 0, 0f);
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
            if (Application.isPlaying)
            {
                StartAttackSequence();
            }
        }
    }
}
