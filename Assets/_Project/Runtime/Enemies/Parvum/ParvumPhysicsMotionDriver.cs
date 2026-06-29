using UnityEngine;

namespace Bellerophon.Enemies.Parvum
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ParvumPhysicsMotionDriver : MonoBehaviour
    {
        [SerializeField] private Transform motionPathTarget;
        [SerializeField, Min(0f)] private float targetFollowGain = 2.5f;
        [SerializeField, Min(0f)] private float maxLinearSpeed = 2.0f;
        [SerializeField] private bool followVerticalAxis;
        [SerializeField] private bool lockRootMotionForReview;

        private Rigidbody attachedRigidbody;

        public Transform MotionPathTarget
        {
            get => motionPathTarget;
            set => motionPathTarget = value;
        }

        public bool LockRootMotionForReview
        {
            get => lockRootMotionForReview;
            set => lockRootMotionForReview = value;
        }

        private void Awake()
        {
            attachedRigidbody = GetComponent<Rigidbody>();
            ApplyRootMotionMode();
        }

        private void OnEnable()
        {
            if (attachedRigidbody == null)
            {
                attachedRigidbody = GetComponent<Rigidbody>();
            }

            ApplyRootMotionMode();
        }

        private void FixedUpdate()
        {
            if (attachedRigidbody == null)
            {
                attachedRigidbody = GetComponent<Rigidbody>();
            }

            if (lockRootMotionForReview)
            {
                ApplyRootMotionMode();
                return;
            }

            ApplyRuntimeRootMotionMode();

            if (motionPathTarget == null)
            {
                return;
            }

            var targetPosition = motionPathTarget.position;
            if (!followVerticalAxis)
            {
                targetPosition.y = attachedRigidbody.position.y;
            }

            var delta = targetPosition - attachedRigidbody.position;
            attachedRigidbody.linearVelocity = Vector3.ClampMagnitude(delta * targetFollowGain, maxLinearSpeed);
        }

        private void OnDisable()
        {
            if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
            {
                attachedRigidbody.linearVelocity = Vector3.zero;
                attachedRigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void ApplyReviewRootMotionLock()
        {
            if (!lockRootMotionForReview || attachedRigidbody == null)
            {
                return;
            }

            attachedRigidbody.useGravity = false;
            if (!attachedRigidbody.isKinematic)
            {
                attachedRigidbody.linearVelocity = Vector3.zero;
                attachedRigidbody.angularVelocity = Vector3.zero;
                attachedRigidbody.isKinematic = true;
            }

            attachedRigidbody.Sleep();
        }

        private void ApplyRuntimeRootMotionMode()
        {
            if (attachedRigidbody == null)
            {
                return;
            }

            attachedRigidbody.useGravity = false;
            attachedRigidbody.isKinematic = false;
        }

        private void ApplyRootMotionMode()
        {
            if (lockRootMotionForReview)
            {
                ApplyReviewRootMotionLock();
            }
            else
            {
                ApplyRuntimeRootMotionMode();
            }
        }
    }
}
