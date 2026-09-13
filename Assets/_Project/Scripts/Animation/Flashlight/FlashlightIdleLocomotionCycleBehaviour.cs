using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    public sealed class FlashlightIdleLocomotionCycleBehaviour : StateMachineBehaviour
    {
        public const int MotionCount = 6;
        public const float IdleDurationSeconds = 5f;
        public const float SecondsPerMotion = 1f;
        public const float TotalCycleDurationSeconds =
            IdleDurationSeconds + SecondsPerMotion * (MotionCount - 1);
        public const string MoveXParameter = "MoveX";
        public const string MoveYParameter = "MoveY";
        public const string DiagonalBlendParameter = "ForwardSidestepBlend";

        private static readonly Vector2[] MotionPositions =
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(1f, 0f),
            new Vector2(0.70710677f, 0.70710677f),
            new Vector2(0f, 2f)
        };

        private static readonly string[] MotionNames =
        {
            "Idle", "WalkForward", "WalkBackward",
            "Sidestep", "WalkDiagonal", "RunForward"
        };

        private static readonly Dictionary<int, float> StartTimes =
            new Dictionary<int, float>();

        public static Vector2 MotionPosition(int phase)
        {
            if (phase < 0 || phase >= MotionPositions.Length)
                throw new ArgumentOutOfRangeException(nameof(phase));
            return MotionPositions[phase];
        }

        public static string MotionName(int phase)
        {
            if (phase < 0 || phase >= MotionNames.Length)
                throw new ArgumentOutOfRangeException(nameof(phase));
            return MotionNames[phase];
        }

        public static float MotionDuration(int phase)
        {
            if (phase < 0 || phase >= MotionCount)
                throw new ArgumentOutOfRangeException(nameof(phase));
            return phase == 0 ? IdleDurationSeconds : SecondsPerMotion;
        }

        public static bool TryGetSequenceState(
            Animator animator,
            out int absolutePhase,
            out int phase,
            out float phaseElapsed)
        {
            absolutePhase = 0;
            phase = 0;
            phaseElapsed = 0f;
            if (animator == null ||
                !StartTimes.TryGetValue(animator.GetInstanceID(), out float start))
                return false;

            CalculateSequenceState(
                Mathf.Max(0f, Time.time - start),
                out absolutePhase,
                out phase,
                out phaseElapsed);
            return true;
        }

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            StartTimes[animator.GetInstanceID()] = Time.time;
            ApplyPhase(animator, 0);
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (!StartTimes.TryGetValue(animator.GetInstanceID(), out float start))
            {
                start = Time.time;
                StartTimes[animator.GetInstanceID()] = start;
            }

            CalculateSequenceState(
                Mathf.Max(0f, Time.time - start),
                out _,
                out int phase,
                out _);
            ApplyPhase(animator, phase);
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            StartTimes.Remove(animator.GetInstanceID());
        }

        private static void ApplyPhase(Animator animator, int phase)
        {
            Vector2 position = MotionPositions[phase];
            animator.SetFloat(MoveXParameter, position.x);
            animator.SetFloat(MoveYParameter, position.y);
            animator.SetFloat(DiagonalBlendParameter, 0.5f);
        }

        private static void CalculateSequenceState(
            float elapsed,
            out int absolutePhase,
            out int phase,
            out float phaseElapsed)
        {
            int completedCycles = Mathf.FloorToInt(
                elapsed / TotalCycleDurationSeconds);
            float cycleElapsed = elapsed -
                completedCycles * TotalCycleDurationSeconds;

            float phaseStart = 0f;
            phase = 0;
            for (; phase < MotionCount - 1; phase++)
            {
                float phaseEnd = phaseStart + MotionDuration(phase);
                if (cycleElapsed < phaseEnd) break;
                phaseStart = phaseEnd;
            }

            absolutePhase = completedCycles * MotionCount + phase;
            phaseElapsed = cycleElapsed - phaseStart;
        }
    }
}
