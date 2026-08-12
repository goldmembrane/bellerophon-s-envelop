using UnityEngine;

namespace Bellerophon.Enemies.Ata
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1010)]
    public sealed class AtaPistolMuzzleFlashDriver : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private Transform flash;
        [SerializeField]
        private int shootingStateHash;
        [SerializeField]
        private float flashStartNormalized = 0.285714f;
        [SerializeField]
        private float flashEndNormalized = 0.354286f;

        public void Configure(
            Animator targetAnimator,
            Transform targetFlash,
            string shootingStateName)
        {
            animator = targetAnimator;
            flash = targetFlash;
            shootingStateHash = Animator.StringToHash(shootingStateName);
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (animator == null || flash == null)
            {
                return;
            }

            var state = animator.GetCurrentAnimatorStateInfo(0);
            // The source recoil is a 1.5-second cycle repeated twice in the 3-second state.
            var phase = Mathf.Repeat(state.normalizedTime, 1f);
            var visible =
                state.shortNameHash == shootingStateHash &&
                phase >= flashStartNormalized &&
                phase < flashEndNormalized;
            if (!visible && animator.IsInTransition(0))
            {
                // The transition uses the source clip's final third as its bridge pose.
                var nextState = animator.GetNextAnimatorStateInfo(0);
                var nextPhase = Mathf.Repeat(nextState.normalizedTime, 1f);
                visible = nextState.shortNameHash == shootingStateHash &&
                          nextPhase >= flashStartNormalized &&
                          nextPhase < flashEndNormalized;
            }

            SetVisible(visible);
        }

        private void SetVisible(bool visible)
        {
            if (flash != null)
            {
                flash.localScale = visible ? Vector3.one : Vector3.zero;
            }
        }
    }
}
