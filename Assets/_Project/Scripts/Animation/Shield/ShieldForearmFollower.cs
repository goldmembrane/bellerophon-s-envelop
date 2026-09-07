using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    public enum ShieldFacingTransition
    {
        Forward,
        RightToForward
    }

    [DefaultExecutionOrder(11000)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ShieldForearmFollower : MonoBehaviour
    {
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

        private float facingCycleStartedAt;

        public float AdditionalForwardOffsetMeters => additionalForwardOffsetMeters;
        public float LastDesiredFaceAlignment { get; private set; } = 1f;
        public float LastVerticalAlignment { get; private set; } = 1f;

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
            FollowImmediately();
        }

        public void ConfigureForearmSurface(Vector3[] forearmLocalSurfacePoints)
        {
            localForearmSurfacePoints = forearmLocalSurfacePoints ?? System.Array.Empty<Vector3>();
        }

        public void FollowImmediately()
        {
            if (transporter == null || rightForearm == null || rightHand == null) return;
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
            Vector3 forearmCenter = Vector3.Lerp(rightForearm.position, rightHand.position, 0.5f);
            transform.position += forearmCenter - transform.TransformPoint(localHandleAnchor);

            Vector3 outward = desiredFace.normalized;
            float forearmFront = ProjectedForearmFront(outward);
            float desiredHandleBack = forearmFront - requiredHandleOverlapMeters + additionalForwardOffsetMeters;
            float currentHandleBack = ProjectedHandleBack(outward);
            transform.position += outward * (desiredHandleBack - currentHandleBack);
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
        }
    }
}
