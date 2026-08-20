using UnityEngine;

namespace Bellerophon.Enemies.Ispant
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class IspantRigidSwordFollower : MonoBehaviour
    {
        [SerializeField] private Transform rightForeArm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform sword;
        [SerializeField] private Transform animatedModel;
        [SerializeField] private Animator animator;
        [SerializeField] private Vector3 rightHandGripLocalPosition;
        [SerializeField] private Vector3 swordGripLocalPosition;
        [SerializeField] private Vector3 swordBladeLocalAxis = Vector3.left;
        [SerializeField] private Vector3 swordRollLocalAxis = Vector3.up;
        [SerializeField] private float gripOutwardOffset;
        [SerializeField] private bool followHandRotation;
        [SerializeField] private Vector3 bladeDirectionInHandSpace;
        [SerializeField] private Vector3 rollDirectionInHandSpace;
        [SerializeField] private bool followForeArmSlash;
        [SerializeField] private float foreArmSlashBlendEndNormalized = 0.2f;
        [SerializeField] private bool followReferenceSwordTrajectory;
        [SerializeField] private bool followLegacySwordTrajectory;
        [SerializeField] private Vector3[] legacyBladeDirectionsInModelSpace;
        [SerializeField] private Vector3[] legacyRollDirectionsInModelSpace;
        [SerializeField] private bool centerUpperBody;
        [SerializeField] private Transform hips;
        [SerializeField] private Transform upperBodyRoot;
        [SerializeField] private Transform leftShoulder;
        [SerializeField] private Transform rightShoulder;
        [SerializeField] private float upperBodyTargetLateralOffset;
        [SerializeField] private float upperBodyTargetVerticalOffset;

        public Transform RightForeArm => rightForeArm;
        public Transform RightHand => rightHand;
        public Transform Sword => sword;
        public Vector3 RightHandGripLocalPosition => rightHandGripLocalPosition;
        public Vector3 SwordGripLocalPosition => swordGripLocalPosition;
        public Vector3 SwordBladeLocalAxis => swordBladeLocalAxis;
        public Vector3 SwordRollLocalAxis => swordRollLocalAxis;
        public float GripOutwardOffset => gripOutwardOffset;
        public bool FollowHandRotation => followHandRotation;
        public bool FollowForeArmSlash => followForeArmSlash;
        public bool FollowReferenceSwordTrajectory => followReferenceSwordTrajectory;
        public bool FollowLegacySwordTrajectory => followLegacySwordTrajectory;
        public int LegacySwordTrajectoryFrameCount =>
            legacyBladeDirectionsInModelSpace == null ? 0 : legacyBladeDirectionsInModelSpace.Length;
        public bool CenterUpperBody => centerUpperBody;

        // These are the measured right-hand grip and visible blade-tip pixel positions
        // from every frame of the supplied 220x260, 10 fps GIF. Components are
        // gripX, gripY, tipX, tipY in top-left image coordinates. Keeping the source
        // measurements here prevents the runtime path from drifting into an invented
        // direction table. The longest visible grip-to-tip sample defines the screen-
        // plane blade length; shorter samples become model-forward foreshortening.
        private const float ReferenceBladeFullLengthPixels = 152f;
        private const float ReferenceBodyCenterXPixels = 110f;
        private static readonly Vector4[] ReferenceSwordPixelTrace =
        {
            new Vector4(56f, 104f, 81f, 0f),
            new Vector4(56f, 104f, 81f, 0f),
            new Vector4(54f, 49f, 140f, 8f),
            new Vector4(59f, 42f, 147f, 65f),
            new Vector4(58f, 24f, 58f, 24f),
            new Vector4(191f, 88f, 219f, 61f),
            new Vector4(167f, 137f, 202f, 143f),
            new Vector4(161f, 146f, 110f, 164f),
            new Vector4(160f, 146f, 109f, 164f),
            new Vector4(160f, 146f, 109f, 164f),
            new Vector4(167f, 146f, 168f, 151f),
            new Vector4(69f, 81f, 207f, 142f),
            new Vector4(69f, 81f, 207f, 142f),
            new Vector4(56f, 103f, 123f, 8f),
            new Vector4(57f, 111f, 100f, 9f)
        };

        public static int ReferenceTrajectoryFrameCount => ReferenceSwordPixelTrace.Length;
        public static float ReferenceTraceBladeFullLengthPixels => ReferenceBladeFullLengthPixels;
        public static float ReferenceTraceBodyCenterXPixels => ReferenceBodyCenterXPixels;

        public static Vector4 GetReferenceSwordPixelTrace(int frameIndex)
        {
            return ReferenceSwordPixelTrace[Mathf.Clamp(
                frameIndex, 0, ReferenceSwordPixelTrace.Length - 1)];
        }

        public static Vector2 EvaluateReferenceGripPixel(float normalizedTime)
        {
            var frameCount = ReferenceSwordPixelTrace.Length;
            var progress = normalizedTime >= 1f
                ? 0f
                : Mathf.Repeat(normalizedTime, 1f);
            var frame = progress * frameCount;
            var from = Mathf.Clamp(Mathf.FloorToInt(frame), 0, frameCount - 1);
            var to = (from + 1) % frameCount;
            var amount = frame - from;
            var fromTrace = ReferenceSwordPixelTrace[from];
            var toTrace = ReferenceSwordPixelTrace[to];
            return Vector2.Lerp(
                new Vector2(fromTrace.x, fromTrace.y),
                new Vector2(toTrace.x, toTrace.y),
                amount);
        }

        public void Configure(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator,
            Vector3 handGripLocalPosition,
            Vector3 gripLocalPosition,
            Vector3 bladeLocalAxis,
            Vector3 rollLocalAxis,
            float outwardOffset)
        {
            rightForeArm = foreArm;
            rightHand = hand;
            sword = rigidSword;
            animatedModel = model;
            animator = sourceAnimator;
            rightHandGripLocalPosition = handGripLocalPosition;
            swordGripLocalPosition = gripLocalPosition;
            swordBladeLocalAxis = bladeLocalAxis.normalized;
            swordRollLocalAxis = Vector3.ProjectOnPlane(rollLocalAxis, swordBladeLocalAxis).normalized;
            gripOutwardOffset = Mathf.Max(0f, outwardOffset);
            followHandRotation = false;
            bladeDirectionInHandSpace = Vector3.zero;
            rollDirectionInHandSpace = Vector3.zero;
            followForeArmSlash = false;
            foreArmSlashBlendEndNormalized = 0.2f;
            followReferenceSwordTrajectory = false;
            followLegacySwordTrajectory = false;
            legacyBladeDirectionsInModelSpace = null;
            legacyRollDirectionsInModelSpace = null;
        }

        public void ConfigureHandRelative(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator,
            Vector3 handGripLocalPosition,
            Vector3 gripLocalPosition,
            Vector3 bladeLocalAxis,
            Vector3 rollLocalAxis,
            Vector3 initialBladeWorldDirection,
            Vector3 initialRollWorldDirection)
        {
            Configure(
                foreArm,
                hand,
                rigidSword,
                model,
                sourceAnimator,
                handGripLocalPosition,
                gripLocalPosition,
                bladeLocalAxis,
                rollLocalAxis,
                0f);
            var blade = initialBladeWorldDirection.normalized;
            var roll = Vector3.ProjectOnPlane(initialRollWorldDirection, blade).normalized;
            if (blade.sqrMagnitude <= 0.99f || roll.sqrMagnitude <= 0.99f)
                return;
            followHandRotation = true;
            bladeDirectionInHandSpace = hand.InverseTransformDirection(blade).normalized;
            rollDirectionInHandSpace = hand.InverseTransformDirection(roll).normalized;
        }

        public void ConfigureForeArmSlash(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator,
            Vector3 handGripLocalPosition,
            Vector3 gripLocalPosition,
            Vector3 bladeLocalAxis,
            Vector3 rollLocalAxis,
            float blendEndNormalized)
        {
            Configure(
                foreArm,
                hand,
                rigidSword,
                model,
                sourceAnimator,
                handGripLocalPosition,
                gripLocalPosition,
                bladeLocalAxis,
                rollLocalAxis,
                0f);
            followForeArmSlash = true;
            foreArmSlashBlendEndNormalized = Mathf.Clamp(blendEndNormalized, 0.01f, 1f);
        }

        public void ConfigureReferenceSwordTrajectory(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator,
            Vector3 handGripLocalPosition,
            Vector3 gripLocalPosition,
            Vector3 bladeLocalAxis,
            Vector3 rollLocalAxis)
        {
            Configure(
                foreArm,
                hand,
                rigidSword,
                model,
                sourceAnimator,
                handGripLocalPosition,
                gripLocalPosition,
                bladeLocalAxis,
                rollLocalAxis,
                0f);
            followReferenceSwordTrajectory = true;
        }

        public void ConfigureLegacySwordTrajectory(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator,
            Vector3 handGripLocalPosition,
            Vector3 gripLocalPosition,
            Vector3 bladeLocalAxis,
            Vector3 rollLocalAxis,
            Vector3[] bladeDirectionsInModelSpace,
            Vector3[] rollDirectionsInModelSpace)
        {
            Configure(
                foreArm,
                hand,
                rigidSword,
                model,
                sourceAnimator,
                handGripLocalPosition,
                gripLocalPosition,
                bladeLocalAxis,
                rollLocalAxis,
                0f);
            if (bladeDirectionsInModelSpace == null || rollDirectionsInModelSpace == null ||
                bladeDirectionsInModelSpace.Length < 2 ||
                bladeDirectionsInModelSpace.Length != rollDirectionsInModelSpace.Length)
                return;
            legacyBladeDirectionsInModelSpace = (Vector3[])bladeDirectionsInModelSpace.Clone();
            legacyRollDirectionsInModelSpace = (Vector3[])rollDirectionsInModelSpace.Clone();
            followLegacySwordTrajectory = true;
        }

        public void SetRightHandGripLocalPosition(Vector3 handGripLocalPosition)
        {
            rightHandGripLocalPosition = handGripLocalPosition;
        }

        public void ConfigureUpperBodyCentering(
            Transform sourceHips,
            Transform sourceUpperBodyRoot,
            Transform sourceLeftShoulder,
            Transform sourceRightShoulder,
            float targetLateralOffset,
            float targetVerticalOffset)
        {
            hips = sourceHips;
            upperBodyRoot = sourceUpperBodyRoot;
            leftShoulder = sourceLeftShoulder;
            rightShoulder = sourceRightShoulder;
            upperBodyTargetLateralOffset = targetLateralOffset;
            upperBodyTargetVerticalOffset = targetVerticalOffset;
            centerUpperBody = hips != null && upperBodyRoot != null &&
                              leftShoulder != null && rightShoulder != null;
        }

        public bool Matches(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator)
        {
            return rightForeArm == foreArm && rightHand == hand && sword == rigidSword &&
                   animatedModel == model && animator == sourceAnimator &&
                   swordBladeLocalAxis.sqrMagnitude > 0.99f && swordRollLocalAxis.sqrMagnitude > 0.99f;
        }

        public bool MatchesHandRelative(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator)
        {
            return Matches(foreArm, hand, rigidSword, model, sourceAnimator) &&
                   followHandRotation && bladeDirectionInHandSpace.sqrMagnitude > 0.99f &&
                   rollDirectionInHandSpace.sqrMagnitude > 0.99f;
        }

        public bool MatchesForeArmSlashAndUpperBodyCentering(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator,
            Transform sourceHips,
            Transform sourceUpperBodyRoot,
            Transform sourceLeftShoulder,
            Transform sourceRightShoulder)
        {
            return Matches(foreArm, hand, rigidSword, model, sourceAnimator) &&
                   followForeArmSlash && foreArmSlashBlendEndNormalized > 0f &&
                   centerUpperBody && hips == sourceHips && upperBodyRoot == sourceUpperBodyRoot &&
                   leftShoulder == sourceLeftShoulder && rightShoulder == sourceRightShoulder;
        }

        public bool MatchesReferenceTrajectoryAndUpperBodyCentering(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator,
            Transform sourceHips,
            Transform sourceUpperBodyRoot,
            Transform sourceLeftShoulder,
            Transform sourceRightShoulder)
        {
            return Matches(foreArm, hand, rigidSword, model, sourceAnimator) &&
                   followReferenceSwordTrajectory && !followForeArmSlash &&
                   centerUpperBody && hips == sourceHips && upperBodyRoot == sourceUpperBodyRoot &&
                   leftShoulder == sourceLeftShoulder && rightShoulder == sourceRightShoulder;
        }

        public bool MatchesLegacyTrajectoryAndUpperBodyCentering(
            Transform foreArm,
            Transform hand,
            Transform rigidSword,
            Transform model,
            Animator sourceAnimator,
            Transform sourceHips,
            Transform sourceUpperBodyRoot,
            Transform sourceLeftShoulder,
            Transform sourceRightShoulder,
            int expectedFrameCount)
        {
            return Matches(foreArm, hand, rigidSword, model, sourceAnimator) &&
                   followLegacySwordTrajectory && !followReferenceSwordTrajectory &&
                   !followForeArmSlash &&
                   legacyBladeDirectionsInModelSpace != null &&
                   legacyRollDirectionsInModelSpace != null &&
                   legacyBladeDirectionsInModelSpace.Length == expectedFrameCount &&
                   legacyRollDirectionsInModelSpace.Length == expectedFrameCount &&
                   centerUpperBody && hips == sourceHips && upperBodyRoot == sourceUpperBodyRoot &&
                   leftShoulder == sourceLeftShoulder && rightShoulder == sourceRightShoulder;
        }

        public Vector3 EvaluateLegacyBladeDirectionInModelSpace(float normalizedTime)
        {
            return EvaluateLegacyDirection(legacyBladeDirectionsInModelSpace, normalizedTime);
        }

        public Vector3 EvaluateLegacyRollDirectionInModelSpace(float normalizedTime)
        {
            return EvaluateLegacyDirection(legacyRollDirectionsInModelSpace, normalizedTime);
        }

        private static Vector3 EvaluateLegacyDirection(Vector3[] values, float normalizedTime)
        {
            if (values == null || values.Length == 0)
                return Vector3.zero;
            if (values.Length == 1)
                return values[0].normalized;
            var progress = normalizedTime >= 1f ? 0f : Mathf.Repeat(normalizedTime, 1f);
            var frame = progress * (values.Length - 1);
            var from = Mathf.Clamp(Mathf.FloorToInt(frame), 0, values.Length - 1);
            var to = Mathf.Min(from + 1, values.Length - 1);
            return Vector3.Slerp(values[from], values[to], frame - from).normalized;
        }

        public static Vector3 EvaluateReferenceBladeScreenDirection(float normalizedTime)
        {
            var frameCount = ReferenceSwordPixelTrace.Length;
            var progress = normalizedTime >= 1f
                ? 0f
                : Mathf.Repeat(normalizedTime, 1f);
            var frame = progress * frameCount;
            var from = Mathf.Clamp(Mathf.FloorToInt(frame), 0, frameCount - 1);
            var to = (from + 1) % frameCount;
            return Vector3.Slerp(
                    TraceToBladeDirection(ReferenceSwordPixelTrace[from]),
                    TraceToBladeDirection(ReferenceSwordPixelTrace[to]),
                    frame - from)
                .normalized;
        }

        private static Vector3 TraceToBladeDirection(Vector4 trace)
        {
            var screenRight = (trace.z - trace.x) / ReferenceBladeFullLengthPixels;
            var screenUp = (trace.y - trace.w) / ReferenceBladeFullLengthPixels;
            var projectedLengthSquared = screenRight * screenRight + screenUp * screenUp;
            if (projectedLengthSquared >= 1f)
            {
                var projected = new Vector2(screenRight, screenUp).normalized;
                return new Vector3(projected.x, projected.y, 0f);
            }

            return new Vector3(
                screenRight,
                screenUp,
                Mathf.Sqrt(1f - projectedLengthSquared));
        }

        public void ApplyFollow()
        {
            ApplyFollow(CurrentNormalizedTime());
        }

        public void ApplyFollow(float normalizedTime)
        {
            if (rightForeArm == null || rightHand == null || sword == null || animatedModel == null)
                return;

            ApplyUpperBodyCentering();

            Vector3 bladeDirection;
            Vector3 rollDirection;
            if (followLegacySwordTrajectory)
            {
                var bladeInModelSpace = EvaluateLegacyBladeDirectionInModelSpace(normalizedTime);
                var rollInModelSpace = EvaluateLegacyRollDirectionInModelSpace(normalizedTime);
                if (bladeInModelSpace.sqrMagnitude <= 0.99f || rollInModelSpace.sqrMagnitude <= 0.99f)
                    return;
                bladeDirection = animatedModel.TransformDirection(bladeInModelSpace).normalized;
                rollDirection = animatedModel.TransformDirection(rollInModelSpace);
                rollDirection = Vector3.ProjectOnPlane(rollDirection, bladeDirection);
            }
            else if (followReferenceSwordTrajectory)
            {
                var screenDirection = EvaluateReferenceBladeScreenDirection(normalizedTime);
                var referenceGrip = EvaluateReferenceGripPixel(normalizedTime);
                var referenceGripSide = referenceGrip.x - ReferenceBodyCenterXPixels;
                var currentGripSide = Vector3.Dot(
                    rightHand.position - animatedModel.position, -animatedModel.right);
                // The supplied GIF changes which screen side holds the sword. The
                // approved Slash arm stays on the opposite side during frames 06-11;
                // mirror X there so the measured outward/inward trace remains on the
                // same body-relative side without altering the arm animation.
                if (referenceGripSide * currentGripSide < 0f)
                    screenDirection.x = -screenDirection.x;
                // The approved front review sees model-right as screen-left. Using the
                // opposite axis makes the blade pass above the right hand like the GIF,
                // independent of the editor or runtime camera currently rendering it.
                bladeDirection = (
                    -animatedModel.right * screenDirection.x +
                    animatedModel.up * screenDirection.y +
                    animatedModel.forward * screenDirection.z).normalized;
                // Keep the cutting plane aimed along the Ispant's own front axis while
                // the blade direction itself follows the supplied GIF in screen space.
                rollDirection = Vector3.ProjectOnPlane(animatedModel.forward, bladeDirection);
            }
            else if (followForeArmSlash)
            {
                if (leftShoulder == null || rightShoulder == null)
                    return;
                var upperCenter = Vector3.Lerp(leftShoulder.position, rightShoulder.position, 0.5f);
                var outwardDirection = Vector3.ProjectOnPlane(
                    rightHand.position - upperCenter, animatedModel.forward);
                if (outwardDirection.sqrMagnitude <= 0.000001f)
                    outwardDirection = Vector3.ProjectOnPlane(
                        rightHand.position - rightForeArm.position, animatedModel.forward);
                if (outwardDirection.sqrMagnitude <= 0.000001f)
                    return;
                outwardDirection.Normalize();
                var progress = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(normalizedTime / foreArmSlashBlendEndNormalized));
                bladeDirection = Vector3.Slerp(animatedModel.up, outwardDirection, progress).normalized;
                // The sword's local roll axis is its blade width. Pointing that axis toward
                // model-forward keeps the cutting plane aimed at the enemy in front.
                rollDirection = Vector3.ProjectOnPlane(animatedModel.forward, bladeDirection);
            }
            else if (followHandRotation)
            {
                bladeDirection = rightHand.TransformDirection(bladeDirectionInHandSpace).normalized;
                rollDirection = Vector3.ProjectOnPlane(
                    rightHand.TransformDirection(rollDirectionInHandSpace), bladeDirection);
            }
            else
            {
                var initialBladeDirection = rightForeArm.position - rightHand.position;
                if (initialBladeDirection.sqrMagnitude <= 0.000001f)
                    return;
                initialBladeDirection.Normalize();

                // This imported model's visible upward direction is opposite its local up axis.
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedTime));
                bladeDirection = Vector3.Slerp(initialBladeDirection, -animatedModel.up, progress).normalized;
                rollDirection = Vector3.ProjectOnPlane(rightHand.up, bladeDirection);
            }
            if (rollDirection.sqrMagnitude <= 0.000001f)
                rollDirection = Vector3.ProjectOnPlane(animatedModel.forward, bladeDirection);
            if (rollDirection.sqrMagnitude <= 0.000001f)
                rollDirection = Vector3.ProjectOnPlane(animatedModel.right, bladeDirection);
            rollDirection.Normalize();

            var localBasis = Quaternion.LookRotation(swordBladeLocalAxis, swordRollLocalAxis);
            var worldBasis = Quaternion.LookRotation(bladeDirection, rollDirection);
            sword.rotation = worldBasis * Quaternion.Inverse(localBasis);

            var gripWorld = rightHand.TransformPoint(rightHandGripLocalPosition) -
                            bladeDirection * gripOutwardOffset;
            sword.position = gripWorld - sword.TransformVector(swordGripLocalPosition);
        }

        private void ApplyUpperBodyCentering()
        {
            if (!centerUpperBody || hips == null || upperBodyRoot == null ||
                leftShoulder == null || rightShoulder == null)
                return;
            var upperCenter = Vector3.Lerp(leftShoulder.position, rightShoulder.position, 0.5f);
            var currentOffset = Vector3.Dot(upperCenter - hips.position, animatedModel.right);
            var visibleUp = animatedModel.up;
            var currentVerticalOffset = Vector3.Dot(upperCenter - hips.position, visibleUp);
            upperBodyRoot.position +=
                animatedModel.right * (upperBodyTargetLateralOffset - currentOffset) +
                visibleUp * (upperBodyTargetVerticalOffset - currentVerticalOffset);
        }

        private float CurrentNormalizedTime()
        {
            if (animator == null || !animator.isActiveAndEnabled)
                return 0f;
            var value = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            var progress = value - Mathf.Floor(value);
            return value > 0f && progress < 0.0001f ? 1f : progress;
        }

        private void LateUpdate()
        {
            ApplyFollow();
        }
    }
}
