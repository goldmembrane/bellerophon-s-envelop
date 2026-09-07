using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    // Keeps the source clip and torso curves intact while skipping the unwanted arm-back section.
    public sealed class DaggerStabLoopStateBehaviour : StateMachineBehaviour
    {
        [SerializeField, Range(0.1f, 0.9f)]
        private float skipTriggerNormalizedTime = 31f / 81f;

        [SerializeField, Range(0.1f, 0.9f)]
        private float resumeNormalizedTime = 69f / 81f;

        private bool skipIssued;

        public void Configure(float triggerNormalizedTime, float targetNormalizedTime)
        {
            skipTriggerNormalizedTime = Mathf.Clamp(triggerNormalizedTime, 0.1f, 0.9f);
            resumeNormalizedTime = Mathf.Clamp(targetNormalizedTime, skipTriggerNormalizedTime, 0.9f);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            skipIssued = false;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            float normalizedTime = Mathf.Repeat(stateInfo.normalizedTime, 1f);
            if (normalizedTime < skipTriggerNormalizedTime)
            {
                skipIssued = false;
                return;
            }

            if (skipIssued || normalizedTime >= resumeNormalizedTime) return;
            var poseTransition = animator.GetComponent<DaggerStabPoseTransitionMotion>();
            if (poseTransition == null)
            {
                return;
            }

            skipIssued = poseTransition.BeginTransition(
                stateInfo.fullPathHash,
                layerIndex,
                resumeNormalizedTime);
        }
    }
}
