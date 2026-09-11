using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    public sealed class VacuumUseLocomotionCycleBehaviour : StateMachineBehaviour
    {
        public const int MotionCount = 5;
        public const float SecondsPerMotion = 1f;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int DiagonalBlendHash = Animator.StringToHash("DiagonalBlend");
        private readonly Dictionary<int, float> startTimes = new Dictionary<int, float>();

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            startTimes[animator.GetInstanceID()] = Time.time;
            ApplyPhase(animator, 0);
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            int id = animator.GetInstanceID();
            if (!startTimes.TryGetValue(id, out float startedAt))
            {
                startedAt = Time.time;
                startTimes[id] = startedAt;
            }
            int absolutePhase = Mathf.FloorToInt(
                Mathf.Max(0f, Time.time - startedAt) / SecondsPerMotion);
            ApplyPhase(animator, absolutePhase % MotionCount);
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            startTimes.Remove(animator.GetInstanceID());
        }

        private static void ApplyPhase(Animator animator, int phase)
        {
            Vector2 motion;
            switch (phase)
            {
                case 0:
                    motion = new Vector2(0f, 1f);
                    break;
                case 1:
                    motion = new Vector2(0f, -1f);
                    break;
                case 2:
                    motion = new Vector2(1f, 0f);
                    break;
                case 3:
                    motion = new Vector2(0f, 2f);
                    break;
                default:
                    motion = new Vector2(0.70710677f, 0.70710677f);
                    break;
            }
            animator.SetFloat(MoveXHash, motion.x);
            animator.SetFloat(MoveYHash, motion.y);
            animator.SetFloat(DiagonalBlendHash, 0.5f);
        }
    }
}
