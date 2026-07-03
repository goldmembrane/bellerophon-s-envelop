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
        [SerializeField] private bool useDeathFallSequence;
        [SerializeField] private Vector3 deathFallVelocity = new Vector3(0f, -1.18f, 0f);
        [SerializeField] private float deathFallDuration = 0.68f;
        [SerializeField] private float deathImpactSettleDuration = 0.02f;
        [SerializeField] private float deathFinalHoldDuration = 1.3f;
        [SerializeField] private bool loopDeathFallForReview;

        public Rigidbody Body => body;
        public Transform MotionPathTarget => motionPathTarget;
        public bool FollowVerticalAxis => followVerticalAxis;
        public bool UseDeathFallSequence => useDeathFallSequence;
        public bool LoopDeathFallForReview => loopDeathFallForReview;
        public Vector3 DeathFallVelocity => deathFallVelocity;
        public float DeathFallDuration => deathFallDuration;
        public float DeathImpactSettleDuration => deathImpactSettleDuration;
        public float DeathFinalHoldDuration => deathFinalHoldDuration;
        public bool LockRootMotionForReview
        {
            get => lockRootMotionForReview;
            set => lockRootMotionForReview = value;
        }

        private float deathElapsedSeconds;
        private bool deathSettled;
        private bool deathInitialPoseCaptured;
        private bool deathInitialConstraintsCaptured;
        private Vector3 deathInitialPosition;
        private Quaternion deathInitialRotation;
        private RigidbodyConstraints deathInitialConstraints;

        public void Configure(Rigidbody configuredBody, Transform configuredMotionPathTarget, bool reviewLocked)
        {
            Configure(configuredBody, configuredMotionPathTarget, reviewLocked, followVerticalAxis, useDeathFallSequence);
        }

        public void Configure(
            Rigidbody configuredBody,
            Transform configuredMotionPathTarget,
            bool reviewLocked,
            bool configuredFollowVerticalAxis,
            bool configuredUseDeathFallSequence)
        {
            Configure(
                configuredBody,
                configuredMotionPathTarget,
                reviewLocked,
                configuredFollowVerticalAxis,
                configuredUseDeathFallSequence,
                loopDeathFallForReview);
        }

        public void Configure(
            Rigidbody configuredBody,
            Transform configuredMotionPathTarget,
            bool reviewLocked,
            bool configuredFollowVerticalAxis,
            bool configuredUseDeathFallSequence,
            bool configuredLoopDeathFallForReview)
        {
            body = configuredBody;
            motionPathTarget = configuredMotionPathTarget;
            lockRootMotionForReview = reviewLocked;
            followVerticalAxis = configuredFollowVerticalAxis;
            useDeathFallSequence = configuredUseDeathFallSequence;
            loopDeathFallForReview = configuredLoopDeathFallForReview;
            deathElapsedSeconds = 0f;
            deathSettled = false;
            deathInitialPoseCaptured = false;
            deathInitialConstraintsCaptured = false;
            CaptureDeathInitialPose();
        }

        public void ConfigureDeathFall(
            Vector3 configuredDeathFallVelocity,
            float configuredDeathFallDuration,
            float configuredDeathImpactSettleDuration,
            float configuredDeathFinalHoldDuration)
        {
            deathFallVelocity = configuredDeathFallVelocity;
            deathFallDuration = Mathf.Max(0.02f, configuredDeathFallDuration);
            deathImpactSettleDuration = Mathf.Max(0.02f, configuredDeathImpactSettleDuration);
            deathFinalHoldDuration = Mathf.Max(0.02f, configuredDeathFinalHoldDuration);
            deathElapsedSeconds = 0f;
            deathSettled = false;
            if (body != null && deathInitialConstraintsCaptured)
            {
                body.constraints = deathInitialConstraints;
            }
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

        private void OnEnable()
        {
            deathElapsedSeconds = 0f;
            deathSettled = false;
            deathInitialPoseCaptured = false;
            deathInitialConstraintsCaptured = false;
            CaptureDeathInitialPose();
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            if (useDeathFallSequence && !lockRootMotionForReview)
            {
                UpdateDeathFallSequence();
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

        private void UpdateDeathFallSequence()
        {
            if (body.isKinematic)
            {
                return;
            }

            CaptureDeathInitialPose();

            if (deathSettled)
            {
                StopDeathBodyAndFreeze();
                deathElapsedSeconds += Time.fixedDeltaTime;
                if (loopDeathFallForReview &&
                    deathElapsedSeconds >= deathFallDuration + deathImpactSettleDuration + deathFinalHoldDuration)
                {
                    ResetDeathFallSequence();
                }

                return;
            }

            deathElapsedSeconds += Time.fixedDeltaTime;
            if (deathElapsedSeconds < deathFallDuration)
            {
                if (deathInitialConstraintsCaptured)
                {
                    body.constraints = deathInitialConstraints;
                }

                body.WakeUp();
                body.linearVelocity = deathFallVelocity;
                body.angularVelocity = Vector3.zero;
                return;
            }

            StopDeathBodyAndFreeze();
            deathSettled = true;
        }

        private void CaptureDeathInitialPose()
        {
            if (deathInitialPoseCaptured || body == null)
            {
                return;
            }

            deathInitialPosition = body.position;
            deathInitialRotation = body.rotation;
            deathInitialPoseCaptured = true;
            if (!deathInitialConstraintsCaptured)
            {
                deathInitialConstraints = body.constraints;
                deathInitialConstraintsCaptured = true;
            }
        }

        private void StopDeathBodyAndFreeze()
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.Sleep();
        }

        private void ResetDeathFallSequence()
        {
            if (deathInitialConstraintsCaptured)
            {
                body.constraints = deathInitialConstraints;
            }

            body.position = deathInitialPosition;
            body.rotation = deathInitialRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            deathElapsedSeconds = 0f;
            deathSettled = false;
        }
    }
}
