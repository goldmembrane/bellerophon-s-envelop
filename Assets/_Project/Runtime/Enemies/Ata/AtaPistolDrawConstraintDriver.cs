using UnityEngine;

namespace Bellerophon.Enemies.Ata
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class AtaPistolDrawConstraintDriver : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private Transform hipAnchor;
        [SerializeField]
        private Transform rightHandAnchor;
        [SerializeField]
        private float drawStartNormalized = 0.08f;
        [SerializeField]
        private float drawEndNormalized = 0.32f;
        [SerializeField]
        private float returnStartNormalized = 0.82f;
        [SerializeField]
        private float returnEndNormalized = 0.98f;

        public void Configure(
            Animator targetAnimator,
            Transform targetHipAnchor,
            Transform targetRightHandAnchor)
        {
            animator = targetAnimator;
            hipAnchor = targetHipAnchor;
            rightHandAnchor = targetRightHandAnchor;
        }

        public void ApplyNormalizedPhase(float normalizedPhase)
        {
            if (hipAnchor == null || rightHandAnchor == null)
            {
                return;
            }

            var phase = Mathf.Repeat(normalizedPhase, 1f);
            var handWeight = HandWeight(phase);
            transform.SetPositionAndRotation(
                Vector3.Lerp(hipAnchor.position, rightHandAnchor.position, handWeight),
                Quaternion.Slerp(hipAnchor.rotation, rightHandAnchor.rotation, handWeight));
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || animator == null)
            {
                return;
            }

            ApplyNormalizedPhase(
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }

        private float HandWeight(float phase)
        {
            if (phase < drawStartNormalized)
            {
                return 0f;
            }

            if (phase < drawEndNormalized)
            {
                return Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        drawStartNormalized,
                        drawEndNormalized,
                        phase));
            }

            if (phase < returnStartNormalized)
            {
                return 1f;
            }

            if (phase < returnEndNormalized)
            {
                return Mathf.SmoothStep(
                    1f,
                    0f,
                    Mathf.InverseLerp(
                        returnStartNormalized,
                        returnEndNormalized,
                        phase));
            }

            return 0f;
        }
    }
}
