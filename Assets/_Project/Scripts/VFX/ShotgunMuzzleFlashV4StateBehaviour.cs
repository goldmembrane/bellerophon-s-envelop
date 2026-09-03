using UnityEngine;

namespace Bellerophon.Vfx
{
    public sealed class ShotgunMuzzleFlashV4StateBehaviour :
        StateMachineBehaviour
    {
        [SerializeField, Range(0f, 0.95f)]
        private float normalizedTriggerTime = 0.05f;

        private ShotgunMuzzleFlashV4Vfx effect;
        private int observedLoop;
        private bool emittedInLoop;

        public float NormalizedTriggerTime => normalizedTriggerTime;

        public void Configure(float triggerTime)
        {
            normalizedTriggerTime = Mathf.Clamp(triggerTime, 0f, 0.95f);
        }

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            effect = animator.GetComponentInChildren<
                ShotgunMuzzleFlashV4Vfx>(true);
            observedLoop = Mathf.FloorToInt(stateInfo.normalizedTime);
            emittedInLoop = false;
            TryEmit(stateInfo.normalizedTime);
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            int loop = Mathf.FloorToInt(stateInfo.normalizedTime);
            if (loop != observedLoop)
            {
                observedLoop = loop;
                emittedInLoop = false;
            }

            TryEmit(stateInfo.normalizedTime);
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            emittedInLoop = false;
        }

        private void TryEmit(float normalizedTime)
        {
            if (emittedInLoop || effect == null ||
                Mathf.Repeat(normalizedTime, 1f) < normalizedTriggerTime)
            {
                return;
            }

            effect.PlayEffect();
            emittedInLoop = true;
        }
    }
}
