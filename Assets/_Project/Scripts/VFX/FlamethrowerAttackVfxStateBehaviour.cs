using UnityEngine;

namespace Bellerophon.Vfx
{
    public sealed class FlamethrowerAttackVfxStateBehaviour :
        StateMachineBehaviour
    {
        private FlamethrowerAttackVfx effect;

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            effect = animator.GetComponentInChildren<
                FlamethrowerAttackVfx>(true);
            if (effect != null)
            {
                effect.BeginRepeatingEmission();
            }
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (effect != null)
            {
                effect.EndRepeatingEmission();
            }
        }
    }
}
