using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    public enum ShieldFacingTransition
    {
        Forward,
        RightToForward,
        FollowForearm
    }

    [DefaultExecutionOrder(11000)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ShieldForearmFollower : MonoBehaviour
    {
        private const float RightToForwardSweepDegrees = 90f;
        [SerializeField] private Transform transporter;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Vector3 localHandleAnchor;
        [SerializeField] private Bounds localHandleBounds;
        [SerializeField] private Vector3 localFaceAxis = Vector3.forward;
        [SerializeField] private Vector3 localVerticalAxis = Vector3.up;
        [SerializeField] private float forearmSkinBeyondFrontBoneMeters;
        [SerializeField] private float requiredHandleOverlapMeters = 0.02f;
        [SerializeField] private float additionalForwardOffsetMeters = 0.01f;
        [SerializeField] private Vector3[] localForearmSurfacePoints = System.Array.Empty<Vector3>();
        [SerializeField] private ShieldFacingTransition facingTransition;
        [SerializeField] private float facingTransitionSeconds = 1f;
        [SerializeField] private Quaternion forearmReferenceRotation = Quaternion.identity;
        [SerializeField] private Vector3 followReferenceFace = Vector3.forward;
        private Vector3 followReferenceVertical = Vector3.up;
        [SerializeField] private float maximumFollowYawDegrees = 30f;
        private float maximumVerticalTiltDegrees = 10f;

        private float facingCycleStartedAt;
        private bool followReferenceCaptured;

        public float AdditionalForwardOffsetMeters => additionalForwardOffsetMeters;
        public float LastDesiredFaceAlignment { get; private set; } = 1f;
        public float LastVerticalAlignment { get; private set; } = 1f;
        public float LastForearmRotationErrorDegrees { get; private set; }
        public float LastFollowYawDegrees { get; private set; }
        public float LastVerticalTiltDegrees { get; private set; }
        public float LastHandleOverlapMeters { get; private set; }
        public float MaximumVerticalTiltDegrees => maximumVerticalTiltDegrees;
        public ShieldFacingTransition FacingTransition => facingTransition;

        public void Configure(Transform transporterRoot, Transform forearm, Transform hand,
            Vector3 handleAnchor, Bounds handleBounds, Vector3 faceAxis, Vector3 verticalAxis,
            float skinBeyondFrontBoneMeters, float handleOverlapMeters, float forwardOffsetMeters)
        {
            transporter = transporterRoot;
            rightForearm = forearm;
            rightHand = hand;
            localHandleAnchor = handleAnchor;
            localHandleBounds = handleBounds;
            localFaceAxis = faceAxis.normalized;
            localVerticalAxis = verticalAxis.normalized;
            forearmSkinBeyondFrontBoneMeters = skinBeyondFrontBoneMeters;
            requiredHandleOverlapMeters = handleOverlapMeters;
            additionalForwardOffsetMeters = forwardOffsetMeters;
            facingTransition = ShieldFacingTransition.Forward;
            facingTransitionSeconds = 1f;
            facingCycleStartedAt = Time.time;
            FollowImmediately();
        }

        public void ConfigureFacing(ShieldFacingTransition transition, float durationSeconds)
        {
            facingTransition = transition;
            facingTransitionSeconds = Mathf.Max(0.01f, durationSeconds);
            facingCycleStartedAt = Time.time;
            followReferenceCaptured = false;
            FollowImmediately();
        }

        public void ConfigureForearmSurface(Vector3[] forearmLocalSurfacePoints)
        {
            localForearmSurfacePoints = forearmLocalSurfacePoints ?? System.Array.Empty<Vector3>();
        }

        public void ConfigureFollowYawLimit(float maximumYawDegrees)
        {
            maximumFollowYawDegrees = Mathf.Clamp(maximumYawDegrees, 0f, 90f);
        }

        public void ConfigureVerticalTiltLimit(float maximumTiltDegrees)
        {
            maximumVerticalTiltDegrees = Mathf.Clamp(maximumTiltDegrees, 0f, 90f);
        }

        public void FollowImmediately()
        {
            if (transporter == null || rightForearm == null || rightHand == null) return;
            if (Application.isPlaying && facingTransition == ShieldFacingTransition.RightToForward)
            {
                if (!followReferenceCaptured)
                {
                    forearmReferenceRotation = rightForearm.rotation;
                    followReferenceFace = transporter.right.normalized;
                    followReferenceVertical = transporter.up.normalized;
                    followReferenceCaptured = true;
                }

                Quaternion forearmDelta = rightForearm.rotation * Quaternion.Inverse(forearmReferenceRotation);
                Vector3 rotatedFace = forearmDelta * followReferenceFace;
                Vector3 horizontalRotatedFace = Vector3.ProjectOnPlane(rotatedFace, transporter.up);
                if (horizontalRotatedFace.sqrMagnitude < 0.000001f)
                    horizontalRotatedFace = followReferenceFace;
                horizontalRotatedFace.Normalize();
                float forearmYaw = Vector3.SignedAngle(followReferenceFace,
                    horizontalRotatedFace, transporter.up);
                // The established Draw arm turns about 30 degrees while the shield must cover
                // the full right-to-forward 90-degree arc, so retain the live arm phase and scale its yaw.
                float yawScale = maximumFollowYawDegrees <= 0.0001f
                    ? 0f
                    : RightToForwardSweepDegrees / maximumFollowYawDegrees;
                LastFollowYawDegrees = Mathf.Clamp(forearmYaw * yawScale,
                    -RightToForwardSweepDegrees, RightToForwardSweepDegrees);
                Vector3 followFace = Quaternion.AngleAxis(LastFollowYawDegrees, transporter.up) *
                    followReferenceFace;
                Vector3 rawVertical = forearmDelta * followReferenceVertical;
                Vector3 followVertical = Vector3.RotateTowards(transporter.up.normalized,
                    rawVertical.normalized, maximumVerticalTiltDegrees * Mathf.Deg2Rad, 0f).normalized;
                ShieldStateMotion drawMotion = GetComponentInParent<ShieldStateMotion>();
                float drawProgress = drawMotion != null && drawMotion.MotionKind == ShieldStateMotionKind.Draw
                    ? drawMotion.LastMotionProgress
                    : 0f;
                float idleBasisBlend = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(0.80f, 0.95f, drawProgress));
                followFace = Vector3.Slerp(followFace, transporter.forward.normalized,
                    idleBasisBlend).normalized;
                followVertical = Vector3.Slerp(followVertical, transporter.up.normalized,
                    idleBasisBlend).normalized;
                if (drawProgress >= 0.95f)
                {
                    followFace = transporter.forward.normalized;
                    followVertical = transporter.up.normalized;
                }
                followFace = Vector3.ProjectOnPlane(followFace, followVertical);
                if (followFace.sqrMagnitude < 0.000001f)
                    followFace = Vector3.ProjectOnPlane(followReferenceFace, followVertical);
                followFace.Normalize();
                Vector3 appliedHorizontalFace = Vector3.ProjectOnPlane(followFace, transporter.up);
                if (appliedHorizontalFace.sqrMagnitude > 0.000001f)
                    LastFollowYawDegrees = Vector3.SignedAngle(followReferenceFace,
                        appliedHorizontalFace.normalized, transporter.up);
                AlignFaceAndVertical(followFace, followVertical);
                LastForearmRotationErrorDegrees = Quaternion.Angle(
                    Quaternion.LookRotation(transform.TransformDirection(localFaceAxis).normalized,
                        transform.TransformDirection(localVerticalAxis).normalized),
                    Quaternion.LookRotation(followFace, followVertical));
                LastDesiredFaceAlignment = Vector3.Dot(
                    transform.TransformDirection(localFaceAxis).normalized, followFace);
                LastVerticalAlignment = Vector3.Dot(
                    transform.TransformDirection(localVerticalAxis).normalized, transporter.up.normalized);
                LastVerticalTiltDegrees = Vector3.Angle(
                    transform.TransformDirection(localVerticalAxis).normalized, transporter.up.normalized);
                PositionHandleAgainstForearm(followFace);
                return;
            }
            if (facingTransition == ShieldFacingTransition.FollowForearm)
            {
                if (!Application.isPlaying)
                {
                    AlignFaceAndVertical(transporter.forward, transporter.up);
                    PositionHandleAgainstForearm(transporter.forward.normalized);
                    return;
                }

                if (!followReferenceCaptured)
                {
                    forearmReferenceRotation = rightForearm.rotation;
                    followReferenceFace = transporter.forward.normalized;
                    followReferenceCaptured = true;
                }

                Quaternion forearmDelta = rightForearm.rotation * Quaternion.Inverse(forearmReferenceRotation);
                Vector3 followFace = forearmDelta * followReferenceFace;
                followFace = Vector3.ProjectOnPlane(followFace, transporter.up);
                if (followFace.sqrMagnitude < 0.000001f) followFace = followReferenceFace;
                followFace.Normalize();
                float followYaw = Vector3.SignedAngle(followReferenceFace, followFace, transporter.up);
                LastFollowYawDegrees = Mathf.Clamp(followYaw,
                    -maximumFollowYawDegrees, maximumFollowYawDegrees);
                followFace = Quaternion.AngleAxis(LastFollowYawDegrees, transporter.up) * followReferenceFace;
                AlignFaceAndVertical(followFace, transporter.up);
                LastForearmRotationErrorDegrees = Quaternion.Angle(
                    Quaternion.LookRotation(transform.TransformDirection(localFaceAxis).normalized,
                        transform.TransformDirection(localVerticalAxis).normalized),
                    Quaternion.LookRotation(followFace, transporter.up.normalized));
                LastDesiredFaceAlignment = Vector3.Dot(
                    transform.TransformDirection(localFaceAxis).normalized, followFace);
                LastVerticalAlignment = Vector3.Dot(
                    transform.TransformDirection(localVerticalAxis).normalized, transporter.up.normalized);
                LastVerticalTiltDegrees = Vector3.Angle(
                    transform.TransformDirection(localVerticalAxis).normalized, transporter.up.normalized);
                // The face angle follows the forearm, while the contact depth stays on the
                // transporter's front axis so a side-facing source pose cannot pull the plate through the torso.
                PositionHandleAgainstForearm(transporter.forward.normalized);
                return;
            }
            Vector3 currentFace = transform.TransformDirection(localFaceAxis).normalized;
            Vector3 currentVertical = transform.TransformDirection(localVerticalAxis).normalized;
            Quaternion currentBasis = Quaternion.LookRotation(currentFace, currentVertical);
            Vector3 desiredFace = transporter.forward;
            if (Application.isPlaying && facingTransition == ShieldFacingTransition.RightToForward)
            {
                float progress = Mathf.Repeat(Time.time - facingCycleStartedAt, facingTransitionSeconds) /
                    facingTransitionSeconds;
                float eased = progress * progress * (3f - 2f * progress);
                desiredFace = Vector3.Slerp(transporter.right, transporter.forward, eased).normalized;
            }
            Quaternion desiredBasis = Quaternion.LookRotation(desiredFace, transporter.up);
            transform.rotation = desiredBasis * Quaternion.Inverse(currentBasis) * transform.rotation;
            LastDesiredFaceAlignment = Vector3.Dot(transform.TransformDirection(localFaceAxis).normalized,
                desiredFace.normalized);
            LastVerticalAlignment = Vector3.Dot(transform.TransformDirection(localVerticalAxis).normalized,
                transporter.up.normalized);
            LastVerticalTiltDegrees = Vector3.Angle(transform.TransformDirection(localVerticalAxis).normalized,
                transporter.up.normalized);
            LastForearmRotationErrorDegrees = 0f;
            LastFollowYawDegrees = 0f;
            PositionHandleAgainstForearm(desiredFace.normalized);
        }

        private void AlignFaceAndVertical(Vector3 desiredFace, Vector3 desiredVertical)
        {
            desiredFace.Normalize();
            desiredVertical.Normalize();
            if (Mathf.Abs(Vector3.Dot(desiredFace, desiredVertical)) > 0.995f)
                desiredFace = Vector3.ProjectOnPlane(transporter.forward, desiredVertical).normalized;
            Vector3 currentFace = transform.TransformDirection(localFaceAxis).normalized;
            Vector3 currentVertical = transform.TransformDirection(localVerticalAxis).normalized;
            Quaternion currentBasis = Quaternion.LookRotation(currentFace, currentVertical);
            Quaternion desiredBasis = Quaternion.LookRotation(desiredFace, desiredVertical);
            transform.rotation = desiredBasis * Quaternion.Inverse(currentBasis) * transform.rotation;
        }

        public void ConfigureAdditionalForwardOffset(float forwardOffsetMeters)
        {
            additionalForwardOffsetMeters = Mathf.Max(0f, forwardOffsetMeters);
            FollowImmediately();
        }

        private void PositionHandleAgainstForearm(Vector3 outward)
        {
            Vector3 forearmCenter = Vector3.Lerp(rightForearm.position, rightHand.position, 0.5f);
            transform.position += forearmCenter - transform.TransformPoint(localHandleAnchor);

            float forearmFront = ProjectedForearmFront(outward);
            float desiredHandleBack = forearmFront - requiredHandleOverlapMeters + additionalForwardOffsetMeters;
            float currentHandleBack = ProjectedHandleBack(outward);
            transform.position += outward * (desiredHandleBack - currentHandleBack);
            LastHandleOverlapMeters = ProjectedForearmFront(outward) - ProjectedHandleBack(outward);
        }

        private float ProjectedHandleBack(Vector3 axis)
        {
            float minimum = float.PositiveInfinity;
            Vector3 min = localHandleBounds.min;
            Vector3 max = localHandleBounds.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 local = new Vector3(x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
                minimum = Mathf.Min(minimum, Vector3.Dot(transform.TransformPoint(local), axis));
            }
            return minimum;
        }

        private float ProjectedForearmFront(Vector3 axis)
        {
            if (localForearmSurfacePoints.Length == 0)
            {
                float boneFront = Mathf.Max(Vector3.Dot(rightForearm.position, axis),
                    Vector3.Dot(rightHand.position, axis));
                return boneFront + forearmSkinBeyondFrontBoneMeters;
            }

            float maximum = float.NegativeInfinity;
            for (int index = 0; index < localForearmSurfacePoints.Length; index++)
                maximum = Mathf.Max(maximum,
                    Vector3.Dot(rightForearm.TransformPoint(localForearmSurfacePoints[index]), axis));
            return maximum;
        }

        private void LateUpdate()
        {
            FollowImmediately();
        }

        private void OnEnable()
        {
            facingCycleStartedAt = Time.time;
            followReferenceCaptured = false;
        }
    }
}
