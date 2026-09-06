using UnityEngine;

namespace Bellerophon.Vfx
{
    public sealed class BatonDischargeFireStateBehaviour :
        StateMachineBehaviour
    {
        private BatonDischargeFireCycle effect;

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            effect = animator.GetComponentInChildren<
                BatonDischargeFireCycle>(true);
            if (effect == null)
            {
                return;
            }

            effect.BeginLooping();
            effect.EvaluateNormalizedTime(stateInfo.normalizedTime);
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (effect != null)
            {
                effect.EvaluateNormalizedTime(stateInfo.normalizedTime);
            }
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (effect != null)
            {
                effect.EndLooping();
            }
        }
    }
}
