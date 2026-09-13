using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    public sealed class MarkerSprayIdleLocomotionCycleBehaviour : StateMachineBehaviour
    {
        public const int MotionCount = 6;
        public const float SecondsPerMotion = 1f;
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
            animator.SetFloat(DiagonalBlendParameter, 0.5f);
        }
    }
}
