using UnityEngine;

namespace Bellerophon.Enemies.Fuga
{
    [DisallowMultipleComponent]
    public sealed class FugaPhysicsMotionDriver : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform motionPathTarget;
        [SerializeField] private bool lockRootMotionForReview = true;
        [SerializeField] private bool followVerticalAxis;
        [SerializeField] private float targetFollowGain = 8f;
        [SerializeField] private float maximumSpeed = 3.5f;

        public Rigidbody Body => body;
        public Transform MotionPathTarget => motionPathTarget;
        public bool LockRootMotionForReview
        {
            get => lockRootMotionForReview;
            set => lockRootMotionForReview = value;
        }

        public void Configure(Rigidbody configuredBody, Transform configuredMotionPathTarget, bool reviewLocked)
        {
            body = configuredBody;
            motionPathTarget = configuredMotionPathTarget;
            lockRootMotionForReview = reviewLocked;
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            if (lockRootMotionForReview || motionPathTarget == null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                return;
            }

            if (body.isKinematic)
            {
                return;
            }

            var delta = motionPathTarget.position - body.position;
            if (!followVerticalAxis)
            {
                delta.y = 0f;
            }

            var velocity = Vector3.ClampMagnitude(delta * targetFollowGain, maximumSpeed);
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;
        }
    }
}
