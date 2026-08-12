using UnityEngine;

namespace Bellerophon.Enemies.Ata
{
    [DisallowMultipleComponent]
    public sealed class AtaCommandStanceEffectAlternator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer guardianStanceEffect;
        [SerializeField] private SpriteRenderer breakthroughStanceEffect;

        private void Awake()
        {
            ApplyCurrentLoop();
        }

        private void OnEnable()
        {
            ApplyCurrentLoop();
        }

        private void LateUpdate()
        {
            ApplyCurrentLoop();
        }

        private void ApplyCurrentLoop()
        {
            if (animator == null || guardianStanceEffect == null ||
                breakthroughStanceEffect == null)
            {
                return;
            }

            var loopIndex = Mathf.Max(
                0,
                Mathf.FloorToInt(animator.GetCurrentAnimatorStateInfo(0).normalizedTime));
            var showGuardianStance = loopIndex % 2 == 0;
            guardianStanceEffect.enabled = showGuardianStance;
            breakthroughStanceEffect.enabled = !showGuardianStance;
        }

        public void Configure(
            Animator targetAnimator,
            SpriteRenderer guardianEffect,
            SpriteRenderer breakthroughEffect)
        {
            animator = targetAnimator;
            guardianStanceEffect = guardianEffect;
            breakthroughStanceEffect = breakthroughEffect;
            ApplyCurrentLoop();
        }
    }
}
