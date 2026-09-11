using System;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [Serializable]
    public struct FlashbangBoneRotation
    {
        [SerializeField] private string path;
        [SerializeField] private Quaternion localRotation;

        public FlashbangBoneRotation(string path, Quaternion localRotation)
        {
            this.path = path;
            this.localRotation = localRotation;
        }

        public string Path => path;
        public Quaternion LocalRotation => localRotation;
    }

    // The Animator owns the character. This component only post-processes the approved
    // Idle right-arm chain and transfers the Release prop from hand-follow to Rigidbody.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class FlashbangThrowReleaseFlightBehaviour : MonoBehaviour
    {
        // Match the approved HoloSpray carry-motion retention: the authored grip remains
        // dominant while the unmodified source locomotion contributes natural sway.
        public const float RightShoulderMotionWeight = 1f;
        public const float RightArmMotionWeight = 0.05f;
        public const float RightForeArmMotionWeight = 0.03f;
        public const float RightHandMotionWeight = 0.01f;
        public const float FingerMotionWeight = 0f;

        public enum MotionMode
        {
            IdlePalmForward,
            ThrowRelease
        }

        [SerializeField] private MotionMode mode;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform flashbang;
        [SerializeField] private Vector3 heldLocalPosition;
        [SerializeField] private Quaternion heldLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 heldLocalScale = Vector3.one;
        [SerializeField] private FlashbangBoneRotation[] sourceRightArmPose =
            Array.Empty<FlashbangBoneRotation>();
        [SerializeField] private FlashbangBoneRotation[] authoredRightArmPose =
            Array.Empty<FlashbangBoneRotation>();
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
        private float maximumIdleBoneRotationDrift;
        private float maximumReleaseHandoffPositionError;
        private float maximumReleaseHandoffRotationError;
        private float maximumReleaseHandoffVerticalDrop;
        private bool hasPeakHeldPose;
        private float peakHeldHandHeight;
        private Vector3 peakHeldHandPosition;
        private Vector3 peakHeldPropPosition;
        private Quaternion peakHeldPropRotation;
        private int lateUpdateCount;
        private float lastNormalizedTime;

        public MotionMode Mode => mode;
        public Transform Flashbang => flashbang;
        public bool IsReleased => released;
        public float FlightElapsed => flightElapsed;
        public int ReleaseCount => releaseCount;
        public int ResetCount => resetCount;
        public float ReleaseTime => releaseTime;
        public float ClipLength => clipLength;
        public float CurrentClipTime { get; private set; }
        public Vector3 ReleasePosition => releasePosition;
        public Vector3 ReleaseVelocity => releaseVelocity;
        public Vector3 CurrentFlightVelocity => body != null ? body.linearVelocity : Vector3.zero;
        public float SpinRadiansPerSecond => spinRadiansPerSecond;
        public float MaximumHeldPositionError => maximumHeldPositionError;
        public float MaximumHeldRotationError => maximumHeldRotationError;
        public float MaximumVelocityAlignmentError => maximumVelocityAlignmentError;
        public float MaximumIdleBoneRotationDrift => maximumIdleBoneRotationDrift;
        public int IdleFrozenBoneCount => authoredRightArmPose.Length;
        public float MaximumReleaseHandoffPositionError =>
            maximumReleaseHandoffPositionError;
        public float MaximumReleaseHandoffRotationError =>
            maximumReleaseHandoffRotationError;
        public float MaximumReleaseHandoffVerticalDrop =>
            maximumReleaseHandoffVerticalDrop;
        public Vector3 PeakHeldHandPosition => peakHeldHandPosition;
        public int LateUpdateCount => lateUpdateCount;
        public float LastNormalizedTime => lastNormalizedTime;
        public float PalmForwardDeviationDegrees => mode == MotionMode.IdlePalmForward
            ? Vector3.Angle(RightPalmNormal(), transform.forward)
            : 0f;

        public void ConfigureIdle(
            Animator targetAnimator,
            Transform hand,
            Transform prop,
            string[] bonePaths,
            Quaternion[] sourceRotations,
            Quaternion[] authoredRotations)
        {
            if (bonePaths == null || sourceRotations == null || authoredRotations == null ||
                bonePaths.Length != sourceRotations.Length ||
                bonePaths.Length != authoredRotations.Length)
                throw new ArgumentException("Flashbang Idle pose arrays must have equal lengths.");
            mode = MotionMode.IdlePalmForward;
            animator = targetAnimator;
            rightHand = hand;
            flashbang = prop;
            sourceRightArmPose = new FlashbangBoneRotation[bonePaths.Length];
            authoredRightArmPose = new FlashbangBoneRotation[bonePaths.Length];
            for (int index = 0; index < bonePaths.Length; index++)
            {
                sourceRightArmPose[index] = new FlashbangBoneRotation(
                    bonePaths[index], sourceRotations[index]);
                authoredRightArmPose[index] = new FlashbangBoneRotation(
                    bonePaths[index], authoredRotations[index]);
            }
            CaptureHeldTransform();
            ResetRuntimeState();
            RefreshPreview();
        }

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
            flashbang = prop;
            clipLength = animationLength;
            releaseTime = handMaximumHeightTime;
            launchVelocityInTransporterSpace = launchVelocityLocal;
            spinRadiansPerSecond = sourceSpinRadians;
            modelFlightAxis = flightAxis.normalized;
            colliderCenter = localColliderCenter;
            colliderSize = localColliderSize;
            sourceRightArmPose = Array.Empty<FlashbangBoneRotation>();
            authoredRightArmPose = Array.Empty<FlashbangBoneRotation>();
            CaptureHeldTransform();
            ResetRuntimeState();
        }

        public void RefreshPreview()
        {
            if (mode == MotionMode.IdlePalmForward)
                ApplyAuthoredRightArmPose();
            else if (!Application.isPlaying)
                RestoreHeldTransform();
        }

        private void OnEnable()
        {
            ResetRuntimeState();
            if (!Application.isPlaying) RefreshPreview();
        }

        private void Awake()
        {
            if (mode == MotionMode.ThrowRelease && Application.isPlaying)
                EnsurePhysicsComponents();
        }

        private void LateUpdate()
        {
            ApplyAfterAnimator();
        }

        private void ApplyAfterAnimator()
        {
            if (!Application.isPlaying || animator == null || rightHand == null || flashbang == null)
                return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            lateUpdateCount++;
            lastNormalizedTime = state.normalizedTime;
            if (mode == MotionMode.IdlePalmForward)
            {
                ApplyAnimatedRightArmOffset();
                MeasureIdleBoneRotationDrift();
                return;
            }

            int nextCycle = Mathf.FloorToInt(state.normalizedTime);
            CurrentClipTime = (state.normalizedTime - nextCycle) * clipLength;
            if (nextCycle != cycle)
            {
                cycle = nextCycle;
                RestoreHeldTransform();
                resetCount++;
            }

            if (!released)
            {
                ApplyHeldLocalTransform();
                TrackHighestHeldPose();
                maximumHeldPositionError = Mathf.Max(
                    maximumHeldPositionError,
                    Vector3.Distance(
                        flashbang.position,
                        rightHand.TransformPoint(heldLocalPosition)));
                maximumHeldRotationError = Mathf.Max(
                    maximumHeldRotationError,
                    Quaternion.Angle(
                        flashbang.rotation,
                        rightHand.rotation * heldLocalRotation));
                if (CurrentClipTime >= releaseTime) Release();
            }
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || mode != MotionMode.ThrowRelease ||
                !released || body == null || body.isKinematic)
                return;

            flightElapsed += Time.fixedDeltaTime;
            Vector3 velocity = body.linearVelocity;
            if (velocity.sqrMagnitude <= 0.000001f) return;

            Vector3 direction = velocity.normalized;
            Vector3 currentAxis = body.rotation * modelFlightAxis;
            Quaternion aligned =
                Quaternion.FromToRotation(currentAxis, direction) * body.rotation;
            float alignmentError = Vector3.Angle(
                aligned * modelFlightAxis, direction);
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
                : flashbang.position;
            Quaternion handoffRotation = hasPeakHeldPose
                ? peakHeldPropRotation
                : flashbang.rotation;
            released = true;
            releaseCount++;
            flightElapsed = 0f;
            releasePosition = handoffPosition;
            releaseVelocity = transform.TransformDirection(launchVelocityInTransporterSpace);
            body.interpolation = RigidbodyInterpolation.None;
            body.isKinematic = true;
            body.useGravity = false;
            flashbang.SetParent(null, true);
            body.position = handoffPosition;
            body.rotation = handoffRotation;
            Physics.SyncTransforms();
            maximumReleaseHandoffPositionError = Mathf.Max(
                maximumReleaseHandoffPositionError,
                Vector3.Distance(flashbang.position, handoffPosition));
            maximumReleaseHandoffRotationError = Mathf.Max(
                maximumReleaseHandoffRotationError,
                Quaternion.Angle(flashbang.rotation, handoffRotation));
            maximumReleaseHandoffVerticalDrop = Mathf.Max(
                maximumReleaseHandoffVerticalDrop,
                Mathf.Max(0f, Vector3.Dot(
                    handoffPosition - flashbang.position, transform.up)));
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = releaseVelocity;
            body.angularVelocity = releaseVelocity.normalized * spinRadiansPerSecond;
        }

        private void RestoreHeldTransform()
        {
            if (flashbang == null || rightHand == null) return;
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
            if (flashbang.parent != rightHand)
                flashbang.SetParent(rightHand, false);
            flashbang.localPosition = heldLocalPosition;
            flashbang.localRotation = heldLocalRotation;
            flashbang.localScale = heldLocalScale;
        }

        private void TrackHighestHeldPose()
        {
            float handHeight = Vector3.Dot(
                rightHand.position - transform.position, transform.up);
            if (hasPeakHeldPose && handHeight <= peakHeldHandHeight) return;
            hasPeakHeldPose = true;
            peakHeldHandHeight = handHeight;
            peakHeldHandPosition = rightHand.position;
            peakHeldPropPosition = flashbang.position;
            peakHeldPropRotation = flashbang.rotation;
        }

        private void ResetPeakHeldPose()
        {
            hasPeakHeldPose = false;
            peakHeldHandHeight = float.NegativeInfinity;
            peakHeldHandPosition = Vector3.zero;
            peakHeldPropPosition = Vector3.zero;
            peakHeldPropRotation = Quaternion.identity;
        }

        private void CaptureHeldTransform()
        {
            if (flashbang == null) return;
            heldLocalPosition = flashbang.localPosition;
            heldLocalRotation = flashbang.localRotation;
            heldLocalScale = flashbang.localScale;
        }

        private void ResetRuntimeState()
        {
            body = flashbang != null ? flashbang.GetComponent<Rigidbody>() : null;
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
            maximumIdleBoneRotationDrift = 0f;
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
            if (flashbang == null)
                throw new MissingReferenceException("Flashbang prop is missing.");
            body = flashbang.GetComponent<Rigidbody>();
            if (body == null) body = flashbang.gameObject.AddComponent<Rigidbody>();
            // Interpolation can render the previous kinematic parent pose for one frame
            // when the prop is detached. Keeping it off preserves the exact handoff pose.
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (!released)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            BoxCollider collider = flashbang.GetComponent<BoxCollider>();
            if (collider == null) collider = flashbang.gameObject.AddComponent<BoxCollider>();
            collider.center = colliderCenter;
            collider.size = colliderSize;
            collider.isTrigger = true;
        }

        private void ApplyAuthoredRightArmPose()
        {
            foreach (FlashbangBoneRotation authored in authoredRightArmPose)
            {
                Transform bone = transform.Find(authored.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        "Flashbang right-arm pose path could not be resolved: " + authored.Path);
                bone.localRotation = authored.LocalRotation;
            }
        }

        private void ApplyAnimatedRightArmOffset()
        {
            foreach (FlashbangBoneRotation authored in authoredRightArmPose)
            {
                Transform bone = transform.Find(authored.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        "Flashbang right-arm pose path could not be resolved: " + authored.Path);
                if (!TryFindSourcePose(authored.Path, out Quaternion sourceRotation))
                    throw new MissingReferenceException(
                        "Flashbang source right-arm pose path could not be resolved: " +
                        authored.Path);

                Quaternion animatedDelta =
                    Quaternion.Inverse(sourceRotation) * bone.localRotation;
                Quaternion retainedMotion = Quaternion.SlerpUnclamped(
                    Quaternion.identity,
                    animatedDelta,
                    MotionWeightForPath(authored.Path));
                bone.localRotation = authored.LocalRotation * retainedMotion;
            }
        }

        private bool TryFindSourcePose(string path, out Quaternion localRotation)
        {
            foreach (FlashbangBoneRotation source in sourceRightArmPose)
            {
                if (source.Path != path) continue;
                localRotation = source.LocalRotation;
                return true;
            }
            localRotation = Quaternion.identity;
            return false;
        }

        private static float MotionWeightForPath(string path)
        {
            if (path.IndexOf("/RightHand/", StringComparison.Ordinal) >= 0)
                return FingerMotionWeight;
            if (path.EndsWith("/RightHand", StringComparison.Ordinal))
                return RightHandMotionWeight;
            if (path.EndsWith("/RightForeArm", StringComparison.Ordinal))
                return RightForeArmMotionWeight;
            if (path.EndsWith("/RightArm", StringComparison.Ordinal))
                return RightArmMotionWeight;
            return RightShoulderMotionWeight;
        }

        private void MeasureIdleBoneRotationDrift()
        {
            foreach (FlashbangBoneRotation authored in authoredRightArmPose)
            {
                Transform bone = transform.Find(authored.Path);
                if (bone == null) continue;
                maximumIdleBoneRotationDrift = Mathf.Max(
                    maximumIdleBoneRotationDrift,
                    Quaternion.Angle(bone.localRotation, authored.LocalRotation));
            }
        }

        private Vector3 RightPalmNormal()
        {
            Transform index = rightHand.Find("RightIndexProximal");
            Transform middle = rightHand.Find("RightMiddleProximal");
            Transform little = rightHand.Find("RightLittleProximal");
            if (index == null || middle == null || little == null)
                throw new MissingReferenceException("Flashbang right-hand finger bones are missing.");
            Vector3 fingerDirection = (middle.position - rightHand.position).normalized;
            Vector3 palmWidth = (little.position - index.position).normalized;
            return Vector3.Cross(palmWidth, fingerDirection).normalized;
        }
    }
}
