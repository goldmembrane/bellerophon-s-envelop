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
        [SerializeField] private SkinnedMeshRenderer deathMeltRenderer;
        [SerializeField] private Transform deathMeltVisualRoot;
        [SerializeField] private Collider deathBodyCollider;
        [SerializeField] private float deathGroundWorldY;
        [SerializeField] private string deathMeltBlendShapeName = "Fuga_Death_WholeBody_Melt";
        [SerializeField] private AnimationCurve deathMeltCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.72f, 32f),
            new Keyframe(1.52f, 78f),
            new Keyframe(2f, 100f));
        [SerializeField] private float deathMeltDuration = 2f;
        [SerializeField] private float deathMeltHoldDuration = 1f;
        [SerializeField] private float deathSideTiltDegrees = 45f;
        [SerializeField] private bool idleHoverEnabled;
        [SerializeField] private float idleHoverAmplitude = 0.015f;
        [SerializeField] private float idleHoverFrequency = 5f;
        [SerializeField] private Vector3 idleHoverBaseLocalPosition;

        public Rigidbody Body => body;
        public Transform MotionPathTarget => motionPathTarget;
        public bool FollowVerticalAxis => followVerticalAxis;
        public bool UseDeathFallSequence => useDeathFallSequence;
        public bool LoopDeathFallForReview => loopDeathFallForReview;
        public Vector3 DeathFallVelocity => deathFallVelocity;
        public float DeathFallDuration => deathFallDuration;
        public float DeathImpactSettleDuration => deathImpactSettleDuration;
        public float DeathFinalHoldDuration => deathFinalHoldDuration;
        public SkinnedMeshRenderer DeathMeltRenderer => deathMeltRenderer;
        public Transform DeathMeltVisualRoot => deathMeltVisualRoot;
        public Collider DeathBodyCollider => deathBodyCollider;
        public float DeathGroundWorldY => deathGroundWorldY;
        public string DeathMeltBlendShapeName => deathMeltBlendShapeName;
        public AnimationCurve DeathMeltCurve => deathMeltCurve;
        public float DeathMeltDuration => deathMeltDuration;
        public float DeathMeltHoldDuration => deathMeltHoldDuration;
        public float DeathSideTiltDegrees => deathSideTiltDegrees;
        public bool DeathImpactDetected => deathImpactDetected;
        public float DeathMeltElapsedSeconds => deathMeltElapsedSeconds;
        public int DeathCompletedLoopCount => deathCompletedLoopCount;
        public float DeathCurrentSideTiltDegrees => deathCurrentSideTiltDegrees;
        public float DeathLastImpactSideTiltDegrees => deathLastImpactSideTiltDegrees;
        public int DeathLeftTiltCount => deathLeftTiltCount;
        public int DeathRightTiltCount => deathRightTiltCount;
        public int DeathLeftImpactCount => deathLeftImpactCount;
        public int DeathRightImpactCount => deathRightImpactCount;
        public int DeathInvalidImpactTiltCount => deathInvalidImpactTiltCount;
        public float DeathMeltVisualLevelProgress => deathMeltVisualLevelProgress;
        public float DeathLastPreMeltVisualTiltDegrees => deathLastPreMeltVisualTiltDegrees;
        public float DeathLastFinalPuddleLevelErrorDegrees => deathLastFinalPuddleLevelErrorDegrees;
        public float DeathLastFinalPuddleGroundErrorMeters => deathLastFinalPuddleGroundErrorMeters;
        public int DeathInvalidPreMeltVisualTiltCount => deathInvalidPreMeltVisualTiltCount;
        public int DeathInvalidFinalPuddleLevelCount => deathInvalidFinalPuddleLevelCount;
        public int DeathInvalidFinalPuddleGroundCount => deathInvalidFinalPuddleGroundCount;
        public int DeathFinalPuddleLevelRecordedCount => deathFinalPuddleLevelRecordedCount;
        public bool IdleHoverEnabled => idleHoverEnabled;
        public float IdleHoverAmplitude => idleHoverAmplitude;
        public float IdleHoverFrequency => idleHoverFrequency;
        public Vector3 IdleHoverBaseLocalPosition => idleHoverBaseLocalPosition;
        public bool LockRootMotionForReview
        {
            get => lockRootMotionForReview;
            set => lockRootMotionForReview = value;
        }

        private float deathElapsedSeconds;
        private bool deathSettled;
        private bool deathInitialPoseCaptured;
        private bool deathInitialConstraintsCaptured;
        private bool deathInitialBodyStateCaptured;
        private bool deathSequenceStarted;
        private bool deathImpactDetected;
        private Vector3 deathInitialPosition;
        private Quaternion deathInitialRotation;
        private RigidbodyConstraints deathInitialConstraints;
        private bool deathInitialUseGravity;
        private bool deathInitialIsKinematic;
        private float deathMeltElapsedSeconds;
        private int deathCompletedLoopCount;
        private int deathMeltBlendShapeIndex = -1;
        private float deathCurrentSideTiltDegrees;
        private float deathLastImpactSideTiltDegrees;
        private int deathLeftTiltCount;
        private int deathRightTiltCount;
        private int deathLeftImpactCount;
        private int deathRightImpactCount;
        private int deathInvalidImpactTiltCount;
        private Vector3 deathMeltInitialLocalPosition;
        private Quaternion deathMeltInitialLocalRotation;
        private Quaternion deathMeltInitialWorldRotation;
        private bool deathMeltVisualPoseCaptured;
        private bool deathFinalPuddleLevelRecorded;
        private float deathMeltVisualLevelProgress;
        private float deathLastPreMeltVisualTiltDegrees;
        private float deathLastFinalPuddleLevelErrorDegrees;
        private float deathLastFinalPuddleGroundErrorMeters;
        private int deathInvalidPreMeltVisualTiltCount;
        private int deathInvalidFinalPuddleLevelCount;
        private int deathInvalidFinalPuddleGroundCount;
        private int deathFinalPuddleLevelRecordedCount;
        private float idleHoverElapsedSeconds;

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
            deathInitialBodyStateCaptured = false;
            deathSequenceStarted = false;
            deathImpactDetected = false;
            deathMeltElapsedSeconds = 0f;
            deathCompletedLoopCount = 0;
            deathMeltBlendShapeIndex = -1;
            ResetDeathTiltReviewCounts();
            CaptureDeathInitialPose();
            CaptureDeathMeltVisualPose();
        }

        // The target oscillates independently; the Rigidbody remains the only object that moves the enemy root.
        public void ConfigureIdleHover(float amplitude, float frequency, float followGain, float speedLimit)
        {
            idleHoverEnabled = true;
            idleHoverAmplitude = Mathf.Max(0f, amplitude);
            idleHoverFrequency = Mathf.Max(0.01f, frequency);
            targetFollowGain = Mathf.Max(0.01f, followGain);
            maximumSpeed = Mathf.Max(0.01f, speedLimit);
            idleHoverElapsedSeconds = 0f;
            if (motionPathTarget != null)
            {
                idleHoverBaseLocalPosition = motionPathTarget.localPosition;
                UpdateIdleHoverTarget();
            }
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

        // Rigidbody owns the root fall; this driver only changes the renderer BlendShape after an upward floor contact.
        public void ConfigureDeathFallAndMelt(
            SkinnedMeshRenderer configuredRenderer,
            Transform configuredVisualRoot,
            Collider configuredBodyCollider,
            float configuredGroundWorldY,
            string configuredBlendShapeName,
            AnimationCurve configuredMeltCurve,
            float configuredMeltDuration,
            float configuredMeltHoldDuration,
            float configuredSideTiltDegrees)
        {
            deathMeltRenderer = configuredRenderer;
            deathMeltVisualRoot = configuredVisualRoot;
            deathBodyCollider = configuredBodyCollider;
            deathGroundWorldY = configuredGroundWorldY;
            deathMeltBlendShapeName = configuredBlendShapeName;
            deathMeltCurve = configuredMeltCurve;
            deathMeltDuration = Mathf.Max(0.02f, configuredMeltDuration);
            deathMeltHoldDuration = Mathf.Max(0.02f, configuredMeltHoldDuration);
            deathSideTiltDegrees = Mathf.Abs(configuredSideTiltDegrees);
            deathMeltBlendShapeIndex = -1;
            deathSequenceStarted = false;
            deathImpactDetected = false;
            deathMeltElapsedSeconds = 0f;
            deathCompletedLoopCount = 0;
            deathMeltVisualPoseCaptured = false;
            ResetDeathTiltReviewCounts();
            CaptureDeathMeltVisualPose();
            SetDeathMeltWeight(0f);
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
            deathInitialBodyStateCaptured = false;
            deathSequenceStarted = false;
            deathImpactDetected = false;
            deathMeltElapsedSeconds = 0f;
            deathCompletedLoopCount = 0;
            deathMeltBlendShapeIndex = -1;
            deathMeltVisualPoseCaptured = false;
            ResetDeathTiltReviewCounts();
            idleHoverElapsedSeconds = 0f;
            CaptureDeathInitialPose();
            CaptureDeathMeltVisualPose();
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            if (idleHoverEnabled && !lockRootMotionForReview && motionPathTarget != null)
            {
                UpdateIdleHoverTarget();
                idleHoverElapsedSeconds += Time.fixedDeltaTime;
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

        private void UpdateIdleHoverTarget()
        {
            var phase = 2f * Mathf.PI * idleHoverFrequency * idleHoverElapsedSeconds;
            var offset = -Mathf.Cos(phase) * idleHoverAmplitude;
            motionPathTarget.localPosition = idleHoverBaseLocalPosition + Vector3.up * offset;
        }

        private void UpdateDeathFallSequence()
        {
            CaptureDeathInitialPose();
            if (!deathSequenceStarted)
            {
                BeginDeathFallSequence();
            }

            if (body.isKinematic)
            {
                return;
            }

            if (deathImpactDetected)
            {
                StopDeathBodyAndFreeze();
                deathMeltElapsedSeconds += Time.fixedDeltaTime;
                var curveTime = Mathf.Min(deathMeltElapsedSeconds, deathMeltDuration);
                var meltWeight = deathMeltCurve != null && deathMeltCurve.length > 0
                    ? deathMeltCurve.Evaluate(curveTime)
                    : 100f * Mathf.Clamp01(curveTime / deathMeltDuration);
                SetDeathMeltWeight(meltWeight);
                RecordFinalPuddleLevelIfReady(meltWeight);
                if (loopDeathFallForReview && deathMeltElapsedSeconds >= deathMeltDuration + deathMeltHoldDuration)
                {
                    ResetDeathFallSequence();
                }

                return;
            }

            deathElapsedSeconds += Time.fixedDeltaTime;
            if (deathBodyCollider != null && deathBodyCollider.bounds.min.y <= deathGroundWorldY)
            {
                var correction = deathGroundWorldY - deathBodyCollider.bounds.min.y;
                if (correction > 0f)
                {
                    body.position += Vector3.up * correction;
                }

                RegisterDeathGroundImpact();
                return;
            }

            body.angularVelocity = Vector3.zero;
            var fallingVelocity = body.linearVelocity;
            fallingVelocity.x = 0f;
            fallingVelocity.z = 0f;
            body.linearVelocity = fallingVelocity;
        }

        private void BeginDeathFallSequence()
        {
            deathSequenceStarted = true;
            deathSettled = false;
            deathImpactDetected = false;
            deathFinalPuddleLevelRecorded = false;
            deathElapsedSeconds = 0f;
            deathMeltElapsedSeconds = 0f;
            SetDeathMeltWeight(0f);
            body.isKinematic = false;
            body.useGravity = true;
            deathCurrentSideTiltDegrees = Random.value < 0.5f
                ? -deathSideTiltDegrees
                : deathSideTiltDegrees;
            if (deathCurrentSideTiltDegrees < 0f)
            {
                deathLeftTiltCount++;
            }
            else
            {
                deathRightTiltCount++;
            }

            body.constraints = deathInitialConstraints & ~RigidbodyConstraints.FreezeRotation;
            body.MoveRotation(
                deathInitialRotation * Quaternion.AngleAxis(deathCurrentSideTiltDegrees, Vector3.forward));
            body.constraints = deathInitialConstraints | RigidbodyConstraints.FreezeRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryRegisterDeathGroundImpact(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            TryRegisterDeathGroundImpact(collision);
        }

        private void TryRegisterDeathGroundImpact(Collision collision)
        {
            if (!useDeathFallSequence || lockRootMotionForReview || !deathSequenceStarted || deathImpactDetected)
            {
                return;
            }

            for (var index = 0; index < collision.contactCount; index++)
            {
                if (collision.GetContact(index).normal.y < 0.5f)
                {
                    continue;
                }

                deathImpactDetected = true;
                RegisterDeathGroundImpact();
                return;
            }
        }

        private void RegisterDeathGroundImpact()
        {
            deathImpactDetected = true;
            deathSettled = true;
            var relativeRotation = Quaternion.Inverse(deathInitialRotation) * body.rotation;
            deathLastImpactSideTiltDegrees = NormalizeSignedAngle(relativeRotation.eulerAngles.z);
            if (Mathf.Abs(Mathf.Abs(deathLastImpactSideTiltDegrees) - deathSideTiltDegrees) > 0.1f)
            {
                deathInvalidImpactTiltCount++;
            }
            else if (deathLastImpactSideTiltDegrees < 0f)
            {
                deathLeftImpactCount++;
            }
            else
            {
                deathRightImpactCount++;
            }
            CaptureDeathMeltVisualPose();
            deathLastPreMeltVisualTiltDegrees = deathMeltVisualRoot != null
                ? Quaternion.Angle(deathMeltInitialWorldRotation, deathMeltVisualRoot.rotation)
                : 0f;
            if (Mathf.Abs(deathLastPreMeltVisualTiltDegrees - deathSideTiltDegrees) > 0.1f)
            {
                deathInvalidPreMeltVisualTiltCount++;
            }
            deathMeltElapsedSeconds = 0f;
            SetDeathMeltWeight(0f);
            StopDeathBodyAndFreeze();
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

            if (!deathInitialBodyStateCaptured)
            {
                deathInitialUseGravity = body.useGravity;
                deathInitialIsKinematic = body.isKinematic;
                deathInitialBodyStateCaptured = true;
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
            deathCompletedLoopCount++;
            if (deathInitialConstraintsCaptured)
            {
                body.constraints = deathInitialConstraints;
            }

            body.position = deathInitialPosition;
            body.rotation = deathInitialRotation;
            body.useGravity = deathInitialUseGravity;
            body.isKinematic = deathInitialIsKinematic;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            deathElapsedSeconds = 0f;
            deathMeltElapsedSeconds = 0f;
            deathSettled = false;
            deathImpactDetected = false;
            deathSequenceStarted = false;
            deathFinalPuddleLevelRecorded = false;
            SetDeathMeltWeight(0f);
        }

        private void ResetDeathTiltReviewCounts()
        {
            deathCurrentSideTiltDegrees = 0f;
            deathLastImpactSideTiltDegrees = 0f;
            deathLeftTiltCount = 0;
            deathRightTiltCount = 0;
            deathLeftImpactCount = 0;
            deathRightImpactCount = 0;
            deathInvalidImpactTiltCount = 0;
            deathMeltVisualLevelProgress = 0f;
            deathLastPreMeltVisualTiltDegrees = 0f;
            deathLastFinalPuddleLevelErrorDegrees = 0f;
            deathLastFinalPuddleGroundErrorMeters = 0f;
            deathInvalidPreMeltVisualTiltCount = 0;
            deathInvalidFinalPuddleLevelCount = 0;
            deathInvalidFinalPuddleGroundCount = 0;
            deathFinalPuddleLevelRecordedCount = 0;
            deathFinalPuddleLevelRecorded = false;
        }

        private static float NormalizeSignedAngle(float degrees)
        {
            return Mathf.Repeat(degrees + 180f, 360f) - 180f;
        }

        private void SetDeathMeltWeight(float weight)
        {
            if (deathMeltRenderer == null || deathMeltRenderer.sharedMesh == null)
            {
                return;
            }

            if (deathMeltBlendShapeIndex < 0)
            {
                deathMeltBlendShapeIndex = deathMeltRenderer.sharedMesh.GetBlendShapeIndex(deathMeltBlendShapeName);
            }

            if (deathMeltBlendShapeIndex >= 0)
            {
                var clampedWeight = Mathf.Clamp(weight, 0f, 100f);
                deathMeltRenderer.SetBlendShapeWeight(deathMeltBlendShapeIndex, clampedWeight);
                ApplyDeathMeltVisualLevel(clampedWeight * 0.01f);
            }
        }

        // The physics root remains tilted; only the melting visual counter-rotates so its puddle reaches world level.
        private void ApplyDeathMeltVisualLevel(float progress)
        {
            CaptureDeathMeltVisualPose();
            deathMeltVisualLevelProgress = Mathf.Clamp01(progress);
            if (deathMeltVisualRoot == null)
            {
                return;
            }

            var counterTilt = -deathCurrentSideTiltDegrees * deathMeltVisualLevelProgress;
            deathMeltVisualRoot.localPosition = deathMeltInitialLocalPosition;
            deathMeltVisualRoot.localRotation =
                Quaternion.AngleAxis(counterTilt, Vector3.forward) * deathMeltInitialLocalRotation;
            if (deathMeltVisualLevelProgress > 0f && deathMeltRenderer != null)
            {
                var groundOffset = deathGroundWorldY - deathMeltRenderer.bounds.min.y;
                if (deathMeltVisualRoot.parent != null)
                {
                    deathMeltVisualRoot.localPosition +=
                        deathMeltVisualRoot.parent.InverseTransformVector(Vector3.up * groundOffset);
                }
                else
                {
                    deathMeltVisualRoot.position += Vector3.up * groundOffset;
                }
            }
        }

        private void CaptureDeathMeltVisualPose()
        {
            if (deathMeltVisualPoseCaptured || deathMeltVisualRoot == null)
            {
                return;
            }

            deathMeltInitialLocalPosition = deathMeltVisualRoot.localPosition;
            deathMeltInitialLocalRotation = deathMeltVisualRoot.localRotation;
            deathMeltInitialWorldRotation = deathMeltVisualRoot.rotation;
            deathMeltVisualPoseCaptured = true;
        }

        private void RecordFinalPuddleLevelIfReady(float meltWeight)
        {
            if (deathFinalPuddleLevelRecorded || meltWeight < 99.999f || deathMeltVisualRoot == null)
            {
                return;
            }

            deathFinalPuddleLevelRecorded = true;
            deathFinalPuddleLevelRecordedCount++;
            deathLastFinalPuddleLevelErrorDegrees =
                Quaternion.Angle(deathMeltInitialWorldRotation, deathMeltVisualRoot.rotation);
            deathLastFinalPuddleGroundErrorMeters =
                deathMeltRenderer != null ? Mathf.Abs(deathMeltRenderer.bounds.min.y - deathGroundWorldY) : float.MaxValue;
            if (deathLastFinalPuddleLevelErrorDegrees > 0.1f)
            {
                deathInvalidFinalPuddleLevelCount++;
            }

            if (deathLastFinalPuddleGroundErrorMeters > 0.001f)
            {
                deathInvalidFinalPuddleGroundCount++;
            }
        }
    }
}
