using UnityEngine;

namespace Bellerophon.Vfx
{
    public sealed class FlamethrowerTankExplosionVfxStateBehaviour :
        StateMachineBehaviour
    {
        private FlamethrowerTankExplosionVfx effect;

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            effect = animator.GetComponentInChildren<
                FlamethrowerTankExplosionVfx>(true);
            if (effect != null)
            {
                effect.BeginLooping();
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
