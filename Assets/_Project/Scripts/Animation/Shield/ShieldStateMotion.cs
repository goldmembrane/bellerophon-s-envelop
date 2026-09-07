using System;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    public enum ShieldStateMotionKind
    {
        None,
        Draw,
        Raise,
        BlockIdle,
        Lower,
        Impact,
        BreakReaction
    }

    [DefaultExecutionOrder(10750)]
    [DisallowMultipleComponent]
    public sealed class ShieldStateMotion : MonoBehaviour
    {
        [SerializeField] private ShieldStateMotionKind motionKind;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform transporter;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform rightShoulder;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private float cycleDurationSeconds = 1f;
        [SerializeField] private float drawStartForearmRightOffsetMeters = 0.36f;
        [SerializeField] private Vector3 raisedArmWorldOffsetMeters = new Vector3(0f, 0.05f, 0.10f);
        // Block Idle breathing uses a small chest pitch only; it never changes bone position or scale.
        [SerializeField] private float blockIdleBreathingPitchDegrees = 1.5f;

        // These arrays store source-derived reference poses used for baseline alignment.
        [SerializeField] private Transform[] baselineBones = Array.Empty<Transform>();
        [SerializeField] private Vector3[] sourceReferencePositions = Array.Empty<Vector3>();
        [SerializeField] private Quaternion[] sourceReferenceRotations = Array.Empty<Quaternion>();
        [SerializeField] private Vector3[] desiredBaselinePositions = Array.Empty<Vector3>();
        [SerializeField] private Quaternion[] desiredBaselineRotations = Array.Empty<Quaternion>();
        [SerializeField] private bool preserveBaselineRelativeMotion;
        [SerializeField] private Transform[] positionLockedBones = Array.Empty<Transform>();
        [SerializeField] private Vector3[] stableLocalPositions = Array.Empty<Vector3>();

        private float cycleStartedAt;

        public ShieldStateMotionKind MotionKind => motionKind;
        public float CycleDurationSeconds => cycleDurationSeconds;
        public float LastCycleProgress { get; private set; }
        public float LastRaisedAmount { get; private set; }
        public float LastDrawAmount { get; private set; }
        public float LastBreathingPitchDegrees { get; private set; }
        public float LastMaximumScaleChange { get; private set; }

        public void ConfigureCommon(ShieldStateMotionKind kind, Animator targetAnimator, Transform transporterRoot,
            Transform spineBone, Transform shoulder, Transform upperArm, Transform forearm, Transform hand,
            float durationSeconds, float drawRightOffsetMeters, Vector3 raisedWorldOffsetMeters)
        {
            motionKind = kind;
            animator = targetAnimator;
            transporter = transporterRoot;
            spine = spineBone;
            rightShoulder = shoulder;
            rightArm = upperArm;
            rightForearm = forearm;
            rightHand = hand;
            cycleDurationSeconds = Mathf.Max(0.01f, durationSeconds);
            drawStartForearmRightOffsetMeters = drawRightOffsetMeters;
            raisedArmWorldOffsetMeters = raisedWorldOffsetMeters;
            cycleStartedAt = Time.time;
        }

        public void ConfigureBaseline(Transform[] bones, Vector3[] sourcePositions,
            Quaternion[] sourceRotations, Vector3[] desiredPositions, Quaternion[] desiredRotations,
            bool preserveRelativeMotion)
        {
            RequireMatchingLengths(bones, sourcePositions, sourceRotations, "baseline source");
            RequireMatchingLengths(bones, desiredPositions, desiredRotations, "baseline destination");
            baselineBones = bones ?? Array.Empty<Transform>();
            sourceReferencePositions = sourcePositions ?? Array.Empty<Vector3>();
            sourceReferenceRotations = sourceRotations ?? Array.Empty<Quaternion>();
            desiredBaselinePositions = desiredPositions ?? Array.Empty<Vector3>();
            desiredBaselineRotations = desiredRotations ?? Array.Empty<Quaternion>();
            preserveBaselineRelativeMotion = preserveRelativeMotion;
        }

        public void ConfigurePositionLocks(Transform[] bones, Vector3[] localPositions)
        {
            if ((bones?.Length ?? 0) != (localPositions?.Length ?? 0))
                throw new ArgumentException("Shield position-lock arrays must have matching lengths.");
            positionLockedBones = bones ?? Array.Empty<Transform>();
            stableLocalPositions = localPositions ?? Array.Empty<Vector3>();
        }

        private static void RequireMatchingLengths(Transform[] bones, Vector3[] positions,
            Quaternion[] rotations, string label)
        {
            int boneCount = bones?.Length ?? 0;
            if ((positions?.Length ?? 0) != boneCount || (rotations?.Length ?? 0) != boneCount)
                throw new ArgumentException("Shield " + label + " pose arrays must have matching lengths.");
        }

        private void OnEnable()
        {
            cycleStartedAt = Time.time;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || animator == null || transporter == null) return;

            float rawProgress = Mathf.Repeat(Time.time - cycleStartedAt, cycleDurationSeconds) /
                cycleDurationSeconds;
            LastCycleProgress = rawProgress;
            float easedProgress = rawProgress * rawProgress * (3f - 2f * rawProgress);
            LastDrawAmount = motionKind == ShieldStateMotionKind.Draw ? 1f - easedProgress : 0f;
            LastBreathingPitchDegrees = 0f;
            LastRaisedAmount = motionKind == ShieldStateMotionKind.Lower
                ? 1f - easedProgress
                : motionKind == ShieldStateMotionKind.Raise ? easedProgress
                : motionKind == ShieldStateMotionKind.BlockIdle || motionKind == ShieldStateMotionKind.Impact ? 1f
                : 0f;

            LastMaximumScaleChange = 0f;
            RestoreStableBonePositions();
            if (motionKind == ShieldStateMotionKind.Impact || motionKind == ShieldStateMotionKind.BreakReaction)
                ApplyBaselineAlignment();
            RestoreEstablishedArmLowering();
            if (motionKind == ShieldStateMotionKind.BlockIdle)
                ApplyBlockIdleBreathing(rawProgress);
            if (motionKind == ShieldStateMotionKind.Draw)
                ApplyDrawStartPose(LastDrawAmount);
            if (LastRaisedAmount > 0f)
                TranslateRaisedArm(LastRaisedAmount);
            if (motionKind != ShieldStateMotionKind.Draw)
            {
                var clearance = GetComponent<ShieldRightArmClearance>();
                if (clearance != null) clearance.ConstrainRightOffsetImmediately();
            }
        }

        private void ApplyBlockIdleBreathing(float cycleProgress)
        {
            if (spine == null) return;
            LastBreathingPitchDegrees = Mathf.Sin(cycleProgress * Mathf.PI * 2f) *
                blockIdleBreathingPitchDegrees;
            spine.rotation = Quaternion.AngleAxis(LastBreathingPitchDegrees, transporter.right) * spine.rotation;
        }

        private void RestoreStableBonePositions()
        {
            for (int index = 0; index < positionLockedBones.Length; index++)
            {
                Transform bone = positionLockedBones[index];
                if (bone != null) bone.localPosition = stableLocalPositions[index];
            }
        }

        private void RestoreEstablishedArmLowering()
        {
            if (rightShoulder == null) return;
            var clearance = GetComponent<ShieldRightArmClearance>();
            if (clearance != null)
                rightShoulder.position -= transporter.up * clearance.ForearmCenterDownwardOffsetMeters;
        }

        private void ApplyBaselineAlignment()
        {
            for (int index = 0; index < baselineBones.Length; index++)
            {
                Transform bone = baselineBones[index];
                if (bone == null) continue;
                if (preserveBaselineRelativeMotion)
                {
                    Quaternion relativeRotation = Quaternion.Inverse(sourceReferenceRotations[index]) * bone.localRotation;
                    bone.localPosition = desiredBaselinePositions[index];
                    bone.localRotation = desiredBaselineRotations[index] * relativeRotation;
                }
                else
                {
                    bone.localPosition = desiredBaselinePositions[index];
                    bone.localRotation = desiredBaselineRotations[index];
                }
            }
        }

        private void ApplyDrawStartPose(float drawAmount)
        {
            if (drawAmount <= 0f || spine == null || rightShoulder == null || rightArm == null ||
                rightForearm == null || rightHand == null) return;

            Vector3 pivot = rightArm.position;
            Vector3 forearmCenter = Vector3.Lerp(rightForearm.position, rightHand.position, 0.5f);
            Vector3 fromPivot = forearmCenter - pivot;
            float vertical = Vector3.Dot(fromPivot, transporter.up);
            float horizontalLength = Mathf.Sqrt(Mathf.Max(0f, fromPivot.sqrMagnitude - vertical * vertical));
            Vector3 rightSideVector = transporter.right * horizontalLength + transporter.up * vertical;
            if (rightSideVector.sqrMagnitude > 0.000001f && fromPivot.sqrMagnitude > 0.000001f)
            {
                Quaternion fullOutwardRotation = Quaternion.FromToRotation(fromPivot, rightSideVector);
                rightArm.rotation = Quaternion.Slerp(Quaternion.identity, fullOutwardRotation, drawAmount) *
                    rightArm.rotation;
            }

            forearmCenter = Vector3.Lerp(rightForearm.position, rightHand.position, 0.5f);
            float currentRightOffset = Vector3.Dot(forearmCenter - spine.position, transporter.right);
            float requestedShift = Mathf.Max(0f, drawStartForearmRightOffsetMeters - currentRightOffset) * drawAmount;
            rightShoulder.position += transporter.right * requestedShift;
        }

        private void TranslateRaisedArm(float raisedAmount)
        {
            if (rightShoulder == null) return;
            Vector3 worldOffset = transporter.right * raisedArmWorldOffsetMeters.x +
                transporter.up * raisedArmWorldOffsetMeters.y +
                transporter.forward * raisedArmWorldOffsetMeters.z;
            rightShoulder.position += worldOffset * raisedAmount;
        }
    }
}
