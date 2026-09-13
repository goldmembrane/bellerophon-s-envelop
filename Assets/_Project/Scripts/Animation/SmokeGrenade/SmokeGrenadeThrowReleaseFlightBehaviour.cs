using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    // Keeps the approved Flashbang throw algorithm while making the recorded peak
    // pose explicit on the Transform before Rigidbody ownership begins.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class SmokeGrenadeThrowReleaseFlightBehaviour : MonoBehaviour
    {
        public enum MotionMode
        {
            ThrowRelease
        }

        [SerializeField] private MotionMode mode;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform smokeGrenade;
        [SerializeField] private Vector3 heldLocalPosition;
        [SerializeField] private Quaternion heldLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 heldLocalScale = Vector3.one;
        [SerializeField] private float clipLength;
        [SerializeField] private float releaseTime;
        [SerializeField] private Vector3 launchVelocityInTransporterSpace;
        [SerializeField] private float spinRadiansPerSecond;
        [SerializeField] private Vector3 modelFlightAxis = Vector3.forward;
        [SerializeField] private Vector3 colliderCenter;
        [SerializeField] private Vector3 colliderSize = Vector3.one;

        private Rigidbody body;
        private int cycle = -1;
        private bool released;
        private float flightElapsed;
        private int releaseCount;
        private int resetCount;
        private Vector3 releasePosition;
        private Vector3 releaseVelocity;
        private float maximumHeldPositionError;
        private float maximumHeldRotationError;
        private float maximumVelocityAlignmentError;
        private float maximumReleaseHandoffPositionError;
        private float maximumReleaseHandoffRotationError;
        private float maximumReleaseHandoffVerticalDrop;
        private bool hasPeakHeldPose;
        private float peakHeldHandHeight;
        private Vector3 peakHeldPropPosition;
        private Quaternion peakHeldPropRotation;
        private int lateUpdateCount;
        private float lastNormalizedTime;

        public MotionMode Mode => mode;
        public Transform Flashbang => smokeGrenade;
        public bool IsReleased => released;
        public float FlightElapsed => flightElapsed;
        public int ReleaseCount => releaseCount;
        public int ResetCount => resetCount;
        public float ReleaseTime => releaseTime;
        public float ClipLength => clipLength;
        public float CurrentClipTime { get; private set; }
        public Vector3 ReleasePosition => releasePosition;
        public Vector3 ReleaseVelocity => releaseVelocity;
        public float SpinRadiansPerSecond => spinRadiansPerSecond;
        public float MaximumHeldPositionError => maximumHeldPositionError;
        public float MaximumHeldRotationError => maximumHeldRotationError;
        public float MaximumVelocityAlignmentError => maximumVelocityAlignmentError;
        public float MaximumReleaseHandoffPositionError =>
            maximumReleaseHandoffPositionError;
        public float MaximumReleaseHandoffRotationError =>
            maximumReleaseHandoffRotationError;
        public float MaximumReleaseHandoffVerticalDrop =>
            maximumReleaseHandoffVerticalDrop;
        public int LateUpdateCount => lateUpdateCount;
        public float LastNormalizedTime => lastNormalizedTime;

        public void ConfigureThrow(
            Animator targetAnimator,
            Transform hand,
            Transform prop,
            float animationLength,
            float handMaximumHeightTime,
            Vector3 launchVelocityLocal,
            float sourceSpinRadians,
            Vector3 flightAxis,
            Vector3 localColliderCenter,
            Vector3 localColliderSize)
        {
            mode = MotionMode.ThrowRelease;
            animator = targetAnimator;
            rightHand = hand;
            smokeGrenade = prop;
            clipLength = animationLength;
            releaseTime = handMaximumHeightTime;
            launchVelocityInTransporterSpace = launchVelocityLocal;
            spinRadiansPerSecond = sourceSpinRadians;
            modelFlightAxis = flightAxis.normalized;
            colliderCenter = localColliderCenter;
            colliderSize = localColliderSize;
            CaptureHeldTransform();
            ResetRuntimeState();
            RefreshPreview();
        }

        public void RefreshPreview()
        {
            if (!Application.isPlaying) RestoreHeldTransform();
        }

        private void OnEnable()
        {
            ResetRuntimeState();
            if (!Application.isPlaying) RefreshPreview();
        }

        private void Awake()
        {
            if (Application.isPlaying) EnsurePhysicsComponents();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || animator == null || rightHand == null ||
                smokeGrenade == null) return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            lateUpdateCount++;
            lastNormalizedTime = state.normalizedTime;
            int nextCycle = Mathf.FloorToInt(state.normalizedTime);
            CurrentClipTime = (state.normalizedTime - nextCycle) * clipLength;
            if (nextCycle != cycle)
            {
                cycle = nextCycle;
                RestoreHeldTransform();
                resetCount++;
            }

            if (released) return;
            ApplyHeldLocalTransform();
            TrackHighestHeldPose();
            maximumHeldPositionError = Mathf.Max(
                maximumHeldPositionError,
                Vector3.Distance(
                    smokeGrenade.position,
                    rightHand.TransformPoint(heldLocalPosition)));
            maximumHeldRotationError = Mathf.Max(
                maximumHeldRotationError,
                Quaternion.Angle(
                    smokeGrenade.rotation,
                    rightHand.rotation * heldLocalRotation));
            if (CurrentClipTime >= releaseTime) Release();
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || !released || body == null || body.isKinematic)
                return;
            flightElapsed += Time.fixedDeltaTime;
            Vector3 velocity = body.linearVelocity;
            if (velocity.sqrMagnitude <= 0.000001f) return;
            Vector3 direction = velocity.normalized;
            Vector3 currentAxis = body.rotation * modelFlightAxis;
            Quaternion aligned =
                Quaternion.FromToRotation(currentAxis, direction) * body.rotation;
            float alignmentError = Vector3.Angle(aligned * modelFlightAxis, direction);
            maximumVelocityAlignmentError = Mathf.Max(
                maximumVelocityAlignmentError, alignmentError);
            body.MoveRotation(aligned);
            body.angularVelocity = direction * spinRadiansPerSecond;
        }

        private void Release()
        {
            EnsurePhysicsComponents();
            Vector3 handoffPosition = hasPeakHeldPose
                ? peakHeldPropPosition
                : smokeGrenade.position;
            Quaternion handoffRotation = hasPeakHeldPose
                ? peakHeldPropRotation
                : smokeGrenade.rotation;
            released = true;
            releaseCount++;
            flightElapsed = 0f;
            releasePosition = handoffPosition;
            releaseVelocity = transform.TransformDirection(launchVelocityInTransporterSpace);
            body.interpolation = RigidbodyInterpolation.None;
            body.isKinematic = true;
            body.useGravity = false;
            smokeGrenade.SetParent(null, true);
            // Rigidbody.position is applied on the physics timeline. Explicitly setting the
            // Transform first prevents the current-frame hand pose from appearing below the
            // recorded maximum-height handoff pose.
            smokeGrenade.SetPositionAndRotation(handoffPosition, handoffRotation);
            body.position = handoffPosition;
            body.rotation = handoffRotation;
            Physics.SyncTransforms();
            maximumReleaseHandoffPositionError = Mathf.Max(
                maximumReleaseHandoffPositionError,
                Vector3.Distance(smokeGrenade.position, handoffPosition));
            maximumReleaseHandoffRotationError = Mathf.Max(
                maximumReleaseHandoffRotationError,
                Quaternion.Angle(smokeGrenade.rotation, handoffRotation));
            maximumReleaseHandoffVerticalDrop = Mathf.Max(
                maximumReleaseHandoffVerticalDrop,
                Mathf.Max(0f, Vector3.Dot(
                    handoffPosition - smokeGrenade.position, transform.up)));
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = releaseVelocity;
            body.angularVelocity = releaseVelocity.normalized * spinRadiansPerSecond;
        }

        private void RestoreHeldTransform()
        {
            if (smokeGrenade == null || rightHand == null) return;
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.useGravity = false;
                body.isKinematic = true;
            }
            released = false;
            flightElapsed = 0f;
            ApplyHeldLocalTransform();
            ResetPeakHeldPose();
        }

        private void ApplyHeldLocalTransform()
        {
            if (smokeGrenade.parent != rightHand)
                smokeGrenade.SetParent(rightHand, false);
            smokeGrenade.localPosition = heldLocalPosition;
            smokeGrenade.localRotation = heldLocalRotation;
            smokeGrenade.localScale = heldLocalScale;
        }

        private void TrackHighestHeldPose()
        {
            float handHeight = Vector3.Dot(
                rightHand.position - transform.position, transform.up);
            if (hasPeakHeldPose && handHeight <= peakHeldHandHeight) return;
            hasPeakHeldPose = true;
            peakHeldHandHeight = handHeight;
            peakHeldPropPosition = smokeGrenade.position;
            peakHeldPropRotation = smokeGrenade.rotation;
        }

        private void ResetPeakHeldPose()
        {
            hasPeakHeldPose = false;
            peakHeldHandHeight = float.NegativeInfinity;
            peakHeldPropPosition = Vector3.zero;
            peakHeldPropRotation = Quaternion.identity;
        }

        private void CaptureHeldTransform()
        {
            if (smokeGrenade == null) return;
            heldLocalPosition = smokeGrenade.localPosition;
            heldLocalRotation = smokeGrenade.localRotation;
            heldLocalScale = smokeGrenade.localScale;
        }

        private void ResetRuntimeState()
        {
            body = smokeGrenade != null ? smokeGrenade.GetComponent<Rigidbody>() : null;
            cycle = -1;
            released = false;
            flightElapsed = 0f;
            releaseCount = 0;
            resetCount = 0;
            releasePosition = Vector3.zero;
            releaseVelocity = Vector3.zero;
            maximumHeldPositionError = 0f;
            maximumHeldRotationError = 0f;
            maximumVelocityAlignmentError = 0f;
            maximumReleaseHandoffPositionError = 0f;
            maximumReleaseHandoffRotationError = 0f;
            maximumReleaseHandoffVerticalDrop = 0f;
            ResetPeakHeldPose();
            lateUpdateCount = 0;
            lastNormalizedTime = 0f;
            CurrentClipTime = 0f;
        }

        private void EnsurePhysicsComponents()
        {
            if (smokeGrenade == null)
                throw new MissingReferenceException("Smoke grenade prop is missing.");
            body = smokeGrenade.GetComponent<Rigidbody>();
            if (body == null) body = smokeGrenade.gameObject.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (!released)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
            BoxCollider collider = smokeGrenade.GetComponent<BoxCollider>();
            if (collider == null) collider = smokeGrenade.gameObject.AddComponent<BoxCollider>();
            collider.center = colliderCenter;
            collider.size = colliderSize;
            collider.isTrigger = true;
        }
    }
}
