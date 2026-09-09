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
        [SerializeField] private float motionDurationSeconds = 1f;
        [SerializeField] private float endHoldDurationSeconds;
        [SerializeField] private bool reverseTimedPlayback;
        [SerializeField] private bool sampleTimedPlayback;
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
        [SerializeField] private Vector3[] stableLocalScales = Array.Empty<Vector3>();
        [SerializeField] private Transform[] rotationLockedBones = Array.Empty<Transform>();
        [SerializeField] private Quaternion[] lockedLocalRotations = Array.Empty<Quaternion>();
        [SerializeField] private bool groundFeet;
        [SerializeField] private Transform leftUpperLeg;
        [SerializeField] private Transform leftLowerLeg;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightUpperLeg;
        [SerializeField] private Transform rightLowerLeg;
        [SerializeField] private Transform rightFoot;
        [SerializeField] private float leftFootReferenceHeightMeters;
        [SerializeField] private float rightFootReferenceHeightMeters;

        private float cycleStartedAt;

        public ShieldStateMotionKind MotionKind => motionKind;
        public float CycleDurationSeconds => cycleDurationSeconds;
        public float LastCycleProgress { get; private set; }
        public float LastRaisedAmount { get; private set; }
        public float LastDrawAmount { get; private set; }
        public float LastBreathingPitchDegrees { get; private set; }
        public float LastMaximumScaleChange { get; private set; }
        public float LastMotionProgress { get; private set; }
        public bool LastHoldingFinalPose { get; private set; }
        public int CompletedCycleCount { get; private set; }
        public float LastLeftFootGroundErrorMeters { get; private set; }
        public float LastRightFootGroundErrorMeters { get; private set; }
        public float LastLeftFootPreGroundDeltaMeters { get; private set; }
        public float LastRightFootPreGroundDeltaMeters { get; private set; }
        public float LastRigidBodyGroundDropMeters { get; private set; }
        public float MotionDurationSeconds => motionDurationSeconds;
        public float EndHoldDurationSeconds => endHoldDurationSeconds;
        public float LastSampledNormalizedTime { get; private set; }

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

        public void ConfigureTimedPlayback(float motionSeconds, float holdSeconds, bool reverse)
        {
            motionDurationSeconds = Mathf.Max(0.01f, motionSeconds);
            endHoldDurationSeconds = Mathf.Max(0f, holdSeconds);
            cycleDurationSeconds = motionDurationSeconds + endHoldDurationSeconds;
            reverseTimedPlayback = reverse;
            sampleTimedPlayback = holdSeconds > 0f;
            if (animator != null) animator.speed = sampleTimedPlayback ? 0f : 1f;
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

        public void ConfigurePositionLocks(Transform[] bones, Vector3[] localPositions, Vector3[] localScales)
        {
            int boneCount = bones?.Length ?? 0;
            if ((localPositions?.Length ?? 0) != boneCount || (localScales?.Length ?? 0) != boneCount)
                throw new ArgumentException("Shield position and scale-lock arrays must have matching lengths.");
            positionLockedBones = bones ?? Array.Empty<Transform>();
            stableLocalPositions = localPositions ?? Array.Empty<Vector3>();
            stableLocalScales = localScales ?? Array.Empty<Vector3>();
        }

        public void ConfigureRotationLocks(Transform[] bones, Quaternion[] localRotations)
        {
            if ((bones?.Length ?? 0) != (localRotations?.Length ?? 0))
                throw new ArgumentException("Shield rotation-lock arrays must have matching lengths.");
            rotationLockedBones = bones ?? Array.Empty<Transform>();
            lockedLocalRotations = localRotations ?? Array.Empty<Quaternion>();
        }

        public void ConfigureFootGrounding(Transform leftThigh, Transform leftCalf, Transform leftAnkle,
            Transform rightThigh, Transform rightCalf, Transform rightAnkle,
            float leftReferenceHeight, float rightReferenceHeight, bool enabled)
        {
            leftUpperLeg = leftThigh;
            leftLowerLeg = leftCalf;
            leftFoot = leftAnkle;
            rightUpperLeg = rightThigh;
            rightLowerLeg = rightCalf;
            rightFoot = rightAnkle;
            leftFootReferenceHeightMeters = leftReferenceHeight;
            rightFootReferenceHeightMeters = rightReferenceHeight;
            groundFeet = enabled;
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
            if (animator != null) animator.speed = sampleTimedPlayback ? 0f : 1f;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || animator == null || transporter == null) return;

            float elapsed = Mathf.Max(0f, Time.time - cycleStartedAt);
            float cycleTime = Mathf.Repeat(elapsed, cycleDurationSeconds);
            float rawProgress = cycleTime / cycleDurationSeconds;
            LastCycleProgress = rawProgress;
            CompletedCycleCount = Mathf.FloorToInt(elapsed / cycleDurationSeconds);
            float motionProgress = Mathf.Clamp01(cycleTime / motionDurationSeconds);
            LastMotionProgress = motionProgress;
            LastHoldingFinalPose = sampleTimedPlayback && cycleTime >= motionDurationSeconds;
            if (sampleTimedPlayback)
                SampleTimedAnimatorPose(motionProgress);
            float easedProgress = motionProgress * motionProgress * (3f - 2f * motionProgress);
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
            RestoreLockedBoneRotations();
            if (groundFeet)
                GroundBothFeet();
            RestoreEstablishedArmLowering();
            if (motionKind == ShieldStateMotionKind.BlockIdle)
                ApplyBlockIdleBreathing(rawProgress);
            if (motionKind == ShieldStateMotionKind.Draw)
                ApplyDrawStartPose(LastDrawAmount);
            if (LastRaisedAmount > 0f)
                TranslateRaisedArm(LastRaisedAmount);
            if (motionKind != ShieldStateMotionKind.Draw &&
                motionKind != ShieldStateMotionKind.BreakReaction)
            {
                var clearance = GetComponent<ShieldRightArmClearance>();
                if (clearance != null) clearance.ConstrainRightOffsetImmediately();
            }
        }

        private void SampleTimedAnimatorPose(float motionProgress)
        {
            float normalizedTime = reverseTimedPlayback ? 1f - motionProgress : motionProgress;
            // A looping Animator state maps exactly 1.0 back to the first frame. Keep the
            // terminal sample inside the clip so the requested hold displays the last pose.
            if (normalizedTime >= 1f) normalizedTime = 0.999999f;
            LastSampledNormalizedTime = normalizedTime;
            animator.Play(0, 0, normalizedTime);
            animator.Update(0f);
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
                if (bone == null) continue;
                bone.localPosition = stableLocalPositions[index];
                bone.localScale = stableLocalScales[index];
                LastMaximumScaleChange = Mathf.Max(LastMaximumScaleChange,
                    Vector3.Distance(bone.localScale, stableLocalScales[index]));
            }
        }

        private void RestoreLockedBoneRotations()
        {
            for (int index = 0; index < rotationLockedBones.Length; index++)
            {
                Transform bone = rotationLockedBones[index];
                if (bone != null) bone.localRotation = lockedLocalRotations[index];
            }
        }

        private void GroundBothFeet()
        {
            Vector3 up = transporter.up.normalized;
            Transform hips = leftUpperLeg != null ? leftUpperLeg.parent : null;
            LastLeftFootPreGroundDeltaMeters = leftFoot == null ? 0f :
                Vector3.Dot(leftFoot.position - transporter.position, up) - leftFootReferenceHeightMeters;
            LastRightFootPreGroundDeltaMeters = rightFoot == null ? 0f :
                Vector3.Dot(rightFoot.position - transporter.position, up) - rightFootReferenceHeightMeters;
            LastRigidBodyGroundDropMeters = 0f;
            if (hips != null && rightUpperLeg != null && rightUpperLeg.parent == hips &&
                leftFoot != null && rightFoot != null)
            {
                LastRigidBodyGroundDropMeters = Mathf.Max(0f,
                    Mathf.Max(LastLeftFootPreGroundDeltaMeters, LastRightFootPreGroundDeltaMeters));
                hips.position -= up * LastRigidBodyGroundDropMeters;
            }
            LastLeftFootGroundErrorMeters = GroundLeg(leftUpperLeg, leftLowerLeg, leftFoot,
                leftFootReferenceHeightMeters);
            LastRightFootGroundErrorMeters = GroundLeg(rightUpperLeg, rightLowerLeg, rightFoot,
                rightFootReferenceHeightMeters);
        }

        private float GroundLeg(Transform upperLeg, Transform lowerLeg, Transform foot, float referenceHeight)
        {
            if (upperLeg == null || lowerLeg == null || foot == null) return float.PositiveInfinity;
            Vector3 up = transporter.up.normalized;
            float currentHeight = Vector3.Dot(foot.position - transporter.position, up);
            Vector3 target = foot.position + up * (referenceHeight - currentHeight);
            Quaternion footWorldRotation = foot.rotation;
            SolveTwoBone(upperLeg, lowerLeg, foot, target, transporter.forward);
            foot.rotation = footWorldRotation;
            return Mathf.Abs(Vector3.Dot(foot.position - transporter.position, up) - referenceHeight);
        }

        private static void SolveTwoBone(Transform upperLeg, Transform lowerLeg, Transform foot,
            Vector3 target, Vector3 fallbackBendDirection)
        {
            Vector3 hip = upperLeg.position;
            Vector3 knee = lowerLeg.position;
            Vector3 ankle = foot.position;
            float upperLength = Vector3.Distance(hip, knee);
            float lowerLength = Vector3.Distance(knee, ankle);
            Vector3 toTarget = target - hip;
            float requestedDistance = toTarget.magnitude;
            if (upperLength < 0.0001f || lowerLength < 0.0001f || requestedDistance < 0.0001f) return;

            Vector3 targetDirection = toTarget / requestedDistance;
            float distance = Mathf.Clamp(requestedDistance,
                Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            Vector3 bendDirection = Vector3.ProjectOnPlane(knee - hip, targetDirection);
            if (bendDirection.sqrMagnitude < 0.000001f)
                bendDirection = Vector3.ProjectOnPlane(fallbackBendDirection, targetDirection);
            bendDirection.Normalize();
            float along = (upperLength * upperLength + distance * distance - lowerLength * lowerLength) /
                (2f * distance);
            float perpendicular = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 desiredKnee = hip + targetDirection * along + bendDirection * perpendicular;

            upperLeg.rotation = Quaternion.FromToRotation(knee - hip, desiredKnee - hip) * upperLeg.rotation;
            knee = lowerLeg.position;
            ankle = foot.position;
            lowerLeg.rotation = Quaternion.FromToRotation(ankle - knee, target - knee) * lowerLeg.rotation;
        }

        private void RestoreEstablishedArmLowering()
        {
            if (rightShoulder == null || motionKind == ShieldStateMotionKind.BreakReaction) return;
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
