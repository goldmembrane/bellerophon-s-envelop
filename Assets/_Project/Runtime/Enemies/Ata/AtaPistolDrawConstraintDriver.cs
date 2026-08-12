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
        private Transform shootingRecoilRotationAnchor;
        [SerializeField]
        // Defines Ata's animated head reference during shooting.
        private Transform shootingGazeAnchor;
        [SerializeField]
        // Supplies the stable model-up reference that keeps the grip below the barrel.
        private Transform modelRoot;
        [SerializeField]
        // Maps the baked waist-pose mesh basis (grip pivot to muzzle) into aim space.
        private Quaternion pistolLocalAimBasis = Quaternion.identity;
        [SerializeField]
        private float drawStartNormalized = 0.08f;
        [SerializeField]
        private float drawEndNormalized = 0.32f;
        [SerializeField]
        private float returnStartNormalized = 0.995f;
        [SerializeField]
        private float returnEndNormalized = 1f;
        [SerializeField]
        private int aimStateHash;
        [SerializeField]
        private int shootingStateHash;
        [SerializeField]
        private float shootingExitNormalized = 3f;

        public void Configure(
            Animator targetAnimator,
            Transform targetHipAnchor,
            Transform targetRightHandAnchor,
            Transform targetShootingRecoilRotationAnchor,
            Transform targetShootingGazeAnchor,
            Transform targetModelRoot,
            Quaternion targetPistolLocalAimBasis,
            string aimStateName,
            string shootingStateName)
        {
            animator = targetAnimator;
            hipAnchor = targetHipAnchor;
            rightHandAnchor = targetRightHandAnchor;
            shootingRecoilRotationAnchor = targetShootingRecoilRotationAnchor;
            shootingGazeAnchor = targetShootingGazeAnchor;
            modelRoot = targetModelRoot;
            pistolLocalAimBasis = targetPistolLocalAimBasis;
            aimStateHash = Animator.StringToHash(aimStateName);
            shootingStateHash = Animator.StringToHash(shootingStateName);
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

            var state = animator.GetCurrentAnimatorStateInfo(0);
            // Both controller states are non-looping. Keep the terminal attachment state
            // during the short exit transition instead of wrapping back to phase zero.
            if (state.shortNameHash == shootingStateHash)
            {
                ApplyWeight(
                    ShootingHandWeight(state.normalizedTime),
                    ShootingAimRotation());
                return;
            }

            if (state.shortNameHash == aimStateHash)
            {
                ApplyWeight(
                    AimHandWeight(Mathf.Clamp01(state.normalizedTime)),
                    rightHandAnchor.rotation);
                return;
            }

            ApplyNormalizedPhase(state.normalizedTime);
        }

        private void ApplyWeight(float handWeight, Quaternion targetRotation)
        {
            transform.SetPositionAndRotation(
                Vector3.Lerp(hipAnchor.position, rightHandAnchor.position, handWeight),
                Quaternion.Slerp(hipAnchor.rotation, targetRotation, handWeight));
        }

        private Quaternion ShootingAimRotation()
        {
            if (shootingGazeAnchor == null || modelRoot == null)
            {
                return shootingRecoilRotationAnchor != null
                    ? shootingRecoilRotationAnchor.rotation
                    : rightHandAnchor.rotation;
            }

            var up = modelRoot.up.normalized;
            var forward = Vector3.ProjectOnPlane(
                modelRoot.forward,
                up).normalized;
            if (forward.sqrMagnitude < 0.999f || up.sqrMagnitude < 0.999f)
            {
                return shootingRecoilRotationAnchor != null
                    ? shootingRecoilRotationAnchor.rotation
                    : rightHandAnchor.rotation;
            }

            return Quaternion.LookRotation(forward, up) *
                   Quaternion.Inverse(pistolLocalAimBasis);
        }

        private float AimHandWeight(float phase)
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
                    Mathf.InverseLerp(drawStartNormalized, drawEndNormalized, phase));
            }

            return 1f;
        }

        private float ShootingHandWeight(float normalizedTime)
        {
            var returnWindow = returnEndNormalized - returnStartNormalized;
            var shootingReturnStart = shootingExitNormalized - returnWindow;
            if (normalizedTime < shootingReturnStart)
            {
                return 1f;
            }

            return Mathf.SmoothStep(
                1f,
                0f,
                Mathf.InverseLerp(
                    shootingReturnStart,
                    shootingExitNormalized,
                    normalizedTime));
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
