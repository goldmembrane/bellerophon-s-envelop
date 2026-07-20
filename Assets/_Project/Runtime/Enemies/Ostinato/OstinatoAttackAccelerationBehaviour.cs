using UnityEngine;

namespace Bellerophon.Enemies.Ostinato
{
    public sealed class OstinatoAttackAccelerationBehaviour : StateMachineBehaviour
    {
        public const float DefaultSourceLastFrame = 196f;
        public const float DefaultApproachStartFrame = 53f;
        public const float DefaultApproachEndFrame = 84f;
        public const float DefaultStrikeEndFrame = 93f;
        public const float DefaultApproachStartMultiplier = 3f;
        public const float DefaultApproachEndMultiplier = 4f;
        public const float DefaultStrikeEndMultiplier = 5f;

        // These serialized values define the approved speed-only profile on the source timeline.
        [SerializeField] private float sourceLastFrame = DefaultSourceLastFrame;
        [SerializeField] private float approachStartFrame = DefaultApproachStartFrame;
        [SerializeField] private float approachEndFrame = DefaultApproachEndFrame;
        [SerializeField] private float strikeEndFrame = DefaultStrikeEndFrame;
        [SerializeField] private float approachStartMultiplier = DefaultApproachStartMultiplier;
        [SerializeField] private float approachEndMultiplier = DefaultApproachEndMultiplier;
        [SerializeField] private float strikeEndMultiplier = DefaultStrikeEndMultiplier;

        public float SourceLastFrame => sourceLastFrame;
        public float ApproachStartFrame => approachStartFrame;
        public float ApproachEndFrame => approachEndFrame;
        public float StrikeEndFrame => strikeEndFrame;
        public float ApproachStartMultiplier => approachStartMultiplier;
        public float ApproachEndMultiplier => approachEndMultiplier;
        public float StrikeEndMultiplier => strikeEndMultiplier;

        public void ConfigureApprovedProfile()
        {
            sourceLastFrame = DefaultSourceLastFrame;
            approachStartFrame = DefaultApproachStartFrame;
            approachEndFrame = DefaultApproachEndFrame;
            strikeEndFrame = DefaultStrikeEndFrame;
            approachStartMultiplier = DefaultApproachStartMultiplier;
            approachEndMultiplier = DefaultApproachEndMultiplier;
            strikeEndMultiplier = DefaultStrikeEndMultiplier;
        }

        public float EvaluateAdditionalSpeedAtSourceFrame(float sourceFrame)
        {
            var frame = Mathf.Clamp(sourceFrame, 0f, sourceLastFrame);
            if (frame < approachStartFrame)
            {
                return 1f;
            }

            if (frame <= approachEndFrame)
            {
                var t = Mathf.InverseLerp(approachStartFrame, approachEndFrame, frame);
                return Mathf.Lerp(approachStartMultiplier, approachEndMultiplier, Mathf.SmoothStep(0f, 1f, t));
            }

            if (frame <= strikeEndFrame)
            {
                var t = Mathf.InverseLerp(approachEndFrame, strikeEndFrame, frame);
                return Mathf.Lerp(approachEndMultiplier, strikeEndMultiplier, Mathf.SmoothStep(0f, 1f, t));
            }

            return 1f;
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.speed = 1f;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var normalizedPhase = stateInfo.normalizedTime - Mathf.Floor(stateInfo.normalizedTime);
            animator.speed = EvaluateAdditionalSpeedAtSourceFrame(normalizedPhase * sourceLastFrame);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.speed = 1f;
        }
    }
}
