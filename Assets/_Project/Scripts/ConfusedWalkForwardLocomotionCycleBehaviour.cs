using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    /// <summary>
    /// Drives the target's three-phase lower-body preview sequence without
    /// restarting the independent Confused_Walk_Forward upper-body layer.
    /// </summary>
    public sealed class ConfusedWalkForwardLocomotionCycleBehaviour :
        StateMachineBehaviour
    {
        public const int MotionCount = 3;
        public const float SecondsPerMotion = 1f;
        public const string MoveXParameter = "ConfusedMoveX";
        public const string MoveYParameter = "ConfusedMoveY";

        private static readonly Vector2[] MotionPositions =
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f)
        };

        private static readonly string[] MotionNames =
        {
            "Idle",
            "WalkForward",
            "WalkBackward"
        };

        private static readonly Dictionary<int, float> StartTimes =
            new Dictionary<int, float>();

        public static Vector2 MotionPosition(int phase)
        {
            if (phase < 0 || phase >= MotionCount)
                throw new ArgumentOutOfRangeException(nameof(phase));
            return MotionPositions[phase];
        }

        public static string MotionName(int phase)
        {
            if (phase < 0 || phase >= MotionCount)
                throw new ArgumentOutOfRangeException(nameof(phase));
            return MotionNames[phase];
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

            float elapsed = Mathf.Max(0f, Time.time - start);
            absolutePhase = Mathf.FloorToInt(elapsed / SecondsPerMotion);
            phase = absolutePhase % MotionCount;
            phaseElapsed = elapsed - absolutePhase * SecondsPerMotion;
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
            int id = animator.GetInstanceID();
            if (!StartTimes.TryGetValue(id, out float start))
            {
                start = Time.time;
                StartTimes[id] = start;
            }

            int absolutePhase = Mathf.FloorToInt(
                Mathf.Max(0f, Time.time - start) / SecondsPerMotion);
            ApplyPhase(animator, absolutePhase % MotionCount);
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
        }
    }
}
