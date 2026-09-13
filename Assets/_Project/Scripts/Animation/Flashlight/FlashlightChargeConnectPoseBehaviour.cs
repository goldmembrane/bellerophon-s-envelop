using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [Serializable]
    public struct FlashlightChargeConnectBonePose
    {
        [SerializeField] private string path;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Quaternion localRotation;

        public FlashlightChargeConnectBonePose(
            string bonePath,
            Vector3 position,
            Quaternion rotation)
        {
            path = bonePath;
            localPosition = position;
            localRotation = rotation;
        }

        public string Path => path;
        public Vector3 LocalPosition => localPosition;
        public Quaternion LocalRotation => localRotation;
    }

    // The supplied charging clip remains the Animator source. The active restore
    // mode only replaces the approved Player_Idle lower body and complete left arm;
    // the torso, head, and right arm remain exactly as evaluated by the source clip.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1100)]
    public sealed class FlashlightChargeConnectPoseBehaviour : MonoBehaviour
    {
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";

        private static readonly float[] RightArmRotationShares =
        {
            0.10f,
            0.35f,
            0.55f
        };

        [SerializeField] private FlashlightRightHandFollowBehaviour carry;
        [SerializeField] private List<FlashlightChargeConnectBonePose> idleBodyPose =
            new List<FlashlightChargeConnectBonePose>();
        [SerializeField] private Animator animator;
        // Disconnect reuses this exact pose configuration while only reversing
        // playback. Edit-mode suppression prevents copied runtime data from
        // rewriting the authored target pose before natural playback begins.
        [SerializeField] private bool reversePlayback;
        [SerializeField] private bool sourceUpperBodyRestored;
        [SerializeField] private bool forwardHoldConfigured;
        [SerializeField] private float forwardHoldStartNormalized;
        [SerializeField] private float forwardHoldDurationSeconds = 1f;
        [SerializeField] private bool rightShoulderForwardConfigured;
        [SerializeField] private Vector3 rightShoulderForwardTargetHandLocal;
        [SerializeField] private float rightShoulderForwardReachDistance;
        [SerializeField] private Vector3 rightShoulderForwardElbowPoleLocal =
            new Vector3(0.8f, -0.45f, 0f);
        [SerializeField] private float rightShoulderForwardStartNormalized = 0.08f;
        [SerializeField] private float rightShoulderForwardFullNormalized = 0.55f;
        [SerializeField] private float rightShoulderForwardReturnNormalized = 0.70f;
        [SerializeField] private float rightShoulderForwardEndNormalized = 0.98f;
        [SerializeField] private float rightShoulderForwardShoulderShare = 0.18f;
        [SerializeField] private float rightShoulderForwardWristSourceWeight = 0.65f;
        [SerializeField] private bool neutralWristConfigured;
        [SerializeField] private bool faceClearanceConfigured;
        [SerializeField] private float faceClearanceLateralOffset;
        [SerializeField] private float faceClearanceShoulderShare = 0.18f;
        [SerializeField] private bool flashlightRightOffsetConfigured;
        [SerializeField] private float flashlightRightOffsetMeters;
        [SerializeField] private bool flashlightForwardOffsetConfigured;
        [SerializeField] private float flashlightForwardOffsetMeters;
        [SerializeField] private bool forwardReachConfigured;
        [SerializeField] private Quaternion rightShoulderReferenceRotation =
            Quaternion.identity;
        [SerializeField] private Vector3 reachStartHandLocal;
        [SerializeField] private Vector3 reachEndHandLocal;
        [SerializeField] private Vector3 elbowPoleLocalDirection =
            new Vector3(0.75f, -0.35f, 0f);
        [SerializeField] private float approachStartNormalized = 0.04f;
        [SerializeField] private float reachEndNormalized = 0.63f;
        [SerializeField] private float holdEndNormalized = 0.72f;
        [SerializeField] private float returnEndNormalized = 1f;

        private float maximumBodyRotationError;
        private float maximumBodyPositionError;
        private float maximumPalmForwardDeviation;
        private float maximumLensUpDeviation;
        private float currentReachWeight;
        private float currentNormalizedPhase;
        private Vector3 currentDesiredHandLocal;
        private float currentHandPositionError;
        private bool forwardHoldActive;
        // Reverse playback must pass through a complete moving segment before
        // it may hold the final pose; this prevents a hold on the entry frame.
        private bool reverseFinalHoldArmed;
        private int reverseFinalHoldArmedLoop = int.MinValue;
        private int completedForwardHoldLoop = int.MinValue;
        private int activeForwardHoldLoop = int.MinValue;
        private double forwardHoldStartedAt;
        private float currentForwardHoldNormalizedPhase;
        private float lastCompletedForwardHoldDuration;
        private int completedForwardHoldCount;
        private float currentRightShoulderForwardWeight;
        private float currentRightShoulderForwardDeviation;
        private float currentRightShoulderForwardLateralOffset;
        private float currentRightShoulderForwardElbowAngle;
        private float currentRightShoulderForwardHandError;
        private float currentNeutralWristBendAngle;
        private float currentFaceClearanceHandError;
        private float currentFaceClearanceElbowAngle;
        private float currentFaceClearanceAppliedOffset;
        private readonly List<FlashlightChargeConnectBonePose>
            forwardHoldRightArmSourcePose =
                new List<FlashlightChargeConnectBonePose>();

        public FlashlightRightHandFollowBehaviour Carry => carry;
        public IReadOnlyList<FlashlightChargeConnectBonePose> IdleBodyPose => idleBodyPose;
        public int IdleBodyPoseCount => idleBodyPose.Count;
        public float MaximumBodyRotationError => maximumBodyRotationError;
        public float MaximumBodyPositionError => maximumBodyPositionError;
        public float MaximumPalmForwardDeviation => maximumPalmForwardDeviation;
        public float MaximumLensUpDeviation => maximumLensUpDeviation;
        public Animator Animator => animator;
        public bool ReversePlayback => reversePlayback;
        public bool SourceUpperBodyRestored => sourceUpperBodyRestored;
        public bool ForwardHoldConfigured => forwardHoldConfigured;
        public float ForwardHoldStartNormalized => forwardHoldStartNormalized;
        public float ForwardHoldDurationSeconds => forwardHoldDurationSeconds;
        public bool IsForwardHolding => forwardHoldActive;
        public float CurrentForwardHoldNormalizedPhase =>
            currentForwardHoldNormalizedPhase;
        public float CurrentForwardHoldElapsedSeconds => forwardHoldActive
            ? (float)(Time.realtimeSinceStartupAsDouble - forwardHoldStartedAt)
            : 0f;
        public float LastCompletedForwardHoldDurationSeconds =>
            lastCompletedForwardHoldDuration;
        public int CompletedForwardHoldCount => completedForwardHoldCount;
        public bool RightShoulderForwardConfigured =>
            rightShoulderForwardConfigured;
        public Vector3 RightShoulderForwardTargetHandLocal =>
            rightShoulderForwardTargetHandLocal;
        public float RightShoulderForwardReachDistance =>
            rightShoulderForwardReachDistance;
        public Vector3 RightShoulderForwardElbowPoleLocal =>
            rightShoulderForwardElbowPoleLocal;
        public float RightShoulderForwardStartNormalized =>
            rightShoulderForwardStartNormalized;
        public float RightShoulderForwardFullNormalized =>
            rightShoulderForwardFullNormalized;
        public float RightShoulderForwardReturnNormalized =>
            rightShoulderForwardReturnNormalized;
        public float RightShoulderForwardEndNormalized =>
            rightShoulderForwardEndNormalized;
        public float RightShoulderForwardShoulderShare =>
            rightShoulderForwardShoulderShare;
        public float RightShoulderForwardWristSourceWeight =>
            rightShoulderForwardWristSourceWeight;
        public bool NeutralWristConfigured => neutralWristConfigured;
        public bool FaceClearanceConfigured => faceClearanceConfigured;
        public float FaceClearanceLateralOffsetMeters =>
            faceClearanceLateralOffset;
        public float FaceClearanceShoulderShare => faceClearanceShoulderShare;
        public bool FlashlightRightOffsetConfigured =>
            flashlightRightOffsetConfigured;
        public float FlashlightRightOffsetMeters => flashlightRightOffsetMeters;
        public bool FlashlightForwardOffsetConfigured =>
            flashlightForwardOffsetConfigured;
        public float FlashlightForwardOffsetMeters =>
            flashlightForwardOffsetMeters;
        public float CurrentRightShoulderForwardWeight =>
            currentRightShoulderForwardWeight;
        public float CurrentRightShoulderForwardDeviationDegrees =>
            currentRightShoulderForwardDeviation;
        public float CurrentRightShoulderForwardLateralOffsetMeters =>
            currentRightShoulderForwardLateralOffset;
        public float CurrentRightShoulderForwardElbowAngleDegrees =>
            currentRightShoulderForwardElbowAngle;
        public float CurrentRightShoulderForwardHandErrorMeters =>
            currentRightShoulderForwardHandError;
        public float CurrentNeutralWristBendAngleDegrees =>
            currentNeutralWristBendAngle;
        public float CurrentFaceClearanceHandErrorMeters =>
            currentFaceClearanceHandError;
        public float CurrentFaceClearanceElbowAngleDegrees =>
            currentFaceClearanceElbowAngle;
        public float CurrentFaceClearanceAppliedOffsetMeters =>
            currentFaceClearanceAppliedOffset;
        public bool ForwardReachConfigured => forwardReachConfigured;
        public Quaternion RightShoulderReferenceRotation =>
            rightShoulderReferenceRotation;
        public Vector3 ReachStartHandLocal => reachStartHandLocal;
        public Vector3 ReachEndHandLocal => reachEndHandLocal;
        public Vector3 ElbowPoleLocalDirection => elbowPoleLocalDirection;
        public float ApproachStartNormalized => approachStartNormalized;
        public float ReachEndNormalized => reachEndNormalized;
        public float HoldEndNormalized => holdEndNormalized;
        public float ReturnEndNormalized => returnEndNormalized;
        public float CurrentReachWeight => currentReachWeight;
        public float CurrentNormalizedPhase => currentNormalizedPhase;
        public Vector3 CurrentDesiredHandLocal => currentDesiredHandLocal;
        public float CurrentHandPositionError => currentHandPositionError;
        public float PalmForwardDeviationDegrees =>
            Vector3.Angle(RightPalmNormal(), transform.forward);
        public float LensUpDeviationDegrees => carry != null && carry.Holder != null
            ? Vector3.Angle(carry.Holder.transform.up, transform.up)
            : 180f;

        public void Configure(
            FlashlightRightHandFollowBehaviour handFollow,
            IEnumerable<FlashlightChargeConnectBonePose> referencePose)
        {
            carry = handFollow != null
                ? handFollow
                : throw new ArgumentNullException(nameof(handFollow));
            idleBodyPose = referencePose != null
                ? new List<FlashlightChargeConnectBonePose>(referencePose)
                : throw new ArgumentNullException(nameof(referencePose));
            if (idleBodyPose.Count == 0)
                throw new ArgumentException(
                    "Flashlight charge-connect idle body pose cannot be empty.",
                    nameof(referencePose));
            RestoreAnimatorSpeed();
            sourceUpperBodyRestored = false;
            forwardHoldConfigured = false;
            rightShoulderForwardConfigured = false;
            neutralWristConfigured = false;
            faceClearanceConfigured = false;
            ResetRuntimeMetrics();
            RefreshPreview();
        }

        public void RebindExactCopiedConfiguration(
            FlashlightRightHandFollowBehaviour handFollow,
            Animator targetAnimator,
            bool playInReverse)
        {
            carry = handFollow != null
                ? handFollow
                : throw new ArgumentNullException(nameof(handFollow));
            animator = targetAnimator != null
                ? targetAnimator
                : throw new ArgumentNullException(nameof(targetAnimator));
            reversePlayback = playInReverse;
            RestoreAnimatorSpeed();
            ResetForwardHoldControlState();
            ResetRuntimeMetrics();
        }

        public void ConfigureReverseFinalHold(float holdDurationSeconds)
        {
            if (!reversePlayback || !forwardHoldConfigured || animator == null)
                throw new InvalidOperationException(
                    "Reverse final hold requires the copied reverse-playback configuration.");
            if (holdDurationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(holdDurationSeconds));

            forwardHoldDurationSeconds = holdDurationSeconds;
            ResetForwardHoldControlState();
            ResetRuntimeMetrics();
        }

        public void ConfigureSourceUpperBodyRestore(
            FlashlightRightHandFollowBehaviour handFollow,
            IEnumerable<FlashlightChargeConnectBonePose> lowerBodyAndLeftArmPose)
        {
            carry = handFollow != null
                ? handFollow
                : throw new ArgumentNullException(nameof(handFollow));
            idleBodyPose = lowerBodyAndLeftArmPose != null
                ? new List<FlashlightChargeConnectBonePose>(lowerBodyAndLeftArmPose)
                : throw new ArgumentNullException(nameof(lowerBodyAndLeftArmPose));
            if (idleBodyPose.Count == 0)
                throw new ArgumentException(
                    "Flashlight charge-connect lower-body and left-arm pose cannot be empty.",
                    nameof(lowerBodyAndLeftArmPose));

            RestoreAnimatorSpeed();
            animator = null;
            sourceUpperBodyRestored = true;
            forwardHoldConfigured = false;
            rightShoulderForwardConfigured = false;
            neutralWristConfigured = false;
            faceClearanceConfigured = false;
            forwardReachConfigured = false;
            currentReachWeight = 0f;
            currentNormalizedPhase = 0f;
            currentDesiredHandLocal = Vector3.zero;
            currentHandPositionError = 0f;
            ResetRuntimeMetrics();
            RefreshPreview();
        }

        public void ConfigureForwardHold(
            Animator targetAnimator,
            float holdStartNormalized,
            float holdDurationSeconds)
        {
            if (targetAnimator == null)
                throw new ArgumentNullException(nameof(targetAnimator));
            if (!sourceUpperBodyRestored || forwardReachConfigured)
                throw new InvalidOperationException(
                    "Forward hold requires the supplied upper-body restore mode.");
            if (!(holdStartNormalized >= 0f && holdStartNormalized < 1f))
                throw new ArgumentOutOfRangeException(nameof(holdStartNormalized));
            if (holdDurationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(holdDurationSeconds));

            RestoreAnimatorSpeed();
            animator = targetAnimator;
            forwardHoldStartNormalized = holdStartNormalized;
            forwardHoldDurationSeconds = holdDurationSeconds;
            forwardHoldConfigured = true;
            ResetForwardHoldControlState();
            ResetRuntimeMetrics();
        }

        public void ConfigureRightShoulderForward(
            Vector3 targetHandLocal,
            float reachDistance,
            Vector3 elbowPoleLocal,
            float correctionStartNormalized,
            float correctionFullNormalized,
            float correctionReturnNormalized,
            float correctionEndNormalized,
            float shoulderShare,
            float wristSourceWeight)
        {
            if (!sourceUpperBodyRestored || !forwardHoldConfigured ||
                forwardReachConfigured)
                throw new InvalidOperationException(
                    "Right-shoulder forward correction requires the approved forward-hold mode.");
            if (elbowPoleLocal.sqrMagnitude <= 0.5f)
                throw new ArgumentException(
                    "Right-shoulder forward elbow pole is invalid.",
                    nameof(elbowPoleLocal));
            if (reachDistance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(reachDistance));
            if (!(0f <= correctionStartNormalized &&
                  correctionStartNormalized < correctionFullNormalized &&
                  correctionFullNormalized <= correctionReturnNormalized &&
                  correctionReturnNormalized < correctionEndNormalized &&
                  correctionEndNormalized <= 1f))
                throw new ArgumentOutOfRangeException(
                    nameof(correctionStartNormalized),
                    "Right-shoulder forward timing must be ordered within one source cycle.");
            if (!(shoulderShare >= 0f && shoulderShare <= 0.5f))
                throw new ArgumentOutOfRangeException(nameof(shoulderShare));
            if (!(wristSourceWeight >= 0f && wristSourceWeight <= 1f))
                throw new ArgumentOutOfRangeException(nameof(wristSourceWeight));

            rightShoulderForwardTargetHandLocal = targetHandLocal;
            rightShoulderForwardReachDistance = reachDistance;
            rightShoulderForwardElbowPoleLocal = elbowPoleLocal.normalized;
            rightShoulderForwardStartNormalized = correctionStartNormalized;
            rightShoulderForwardFullNormalized = correctionFullNormalized;
            rightShoulderForwardReturnNormalized = correctionReturnNormalized;
            rightShoulderForwardEndNormalized = correctionEndNormalized;
            rightShoulderForwardShoulderShare = shoulderShare;
            rightShoulderForwardWristSourceWeight = wristSourceWeight;
            rightShoulderForwardConfigured = true;
            neutralWristConfigured = false;
            faceClearanceConfigured = false;
            ResetRuntimeMetrics();
            RefreshPreview();
        }

        public void ConfigureNeutralWrist()
        {
            if (!sourceUpperBodyRestored || !forwardHoldConfigured ||
                !rightShoulderForwardConfigured || forwardReachConfigured)
                throw new InvalidOperationException(
                    "Neutral wrist correction requires the approved right-shoulder-forward hold mode.");

            // The flashlight holder is aligned independently. Retaining the old
            // hand world rotation here would force the wrist to counter-rotate.
            rightShoulderForwardWristSourceWeight = 0f;
            neutralWristConfigured = true;
            faceClearanceConfigured = false;
            ResetRuntimeMetrics();
            RefreshPreview();
        }

        public void ConfigureFaceClearance(
            float lateralOffsetMeters,
            float shoulderShare)
        {
            if (!sourceUpperBodyRestored || !forwardHoldConfigured ||
                !rightShoulderForwardConfigured || !neutralWristConfigured ||
                forwardReachConfigured)
                throw new InvalidOperationException(
                    "Face clearance requires the approved neutral-wrist forward-hold mode.");
            if (!(lateralOffsetMeters > 0f && lateralOffsetMeters <= 0.25f))
                throw new ArgumentOutOfRangeException(nameof(lateralOffsetMeters));
            if (!(shoulderShare >= 0f && shoulderShare <= 0.5f))
                throw new ArgumentOutOfRangeException(nameof(shoulderShare));

            faceClearanceLateralOffset = lateralOffsetMeters;
            faceClearanceShoulderShare = shoulderShare;
            faceClearanceConfigured = true;
            ResetRuntimeMetrics();
            RefreshPreview();
        }

        public void ConfigureFlashlightRightOffset(float rightOffsetMeters)
        {
            if (!faceClearanceConfigured)
                throw new InvalidOperationException(
                    "Flashlight right offset requires the approved face-clearance pose.");
            if (!(rightOffsetMeters > 0f && rightOffsetMeters <= 0.25f))
                throw new ArgumentOutOfRangeException(nameof(rightOffsetMeters));

            // This offset belongs to the prop only. The arm and body pose remain
            // untouched while the holder continues to follow the right hand.
            flashlightRightOffsetMeters = rightOffsetMeters;
            flashlightRightOffsetConfigured = true;
        }

        public void ConfigureFlashlightForwardOffset(float forwardOffsetMeters)
        {
            if (!flashlightRightOffsetConfigured)
                throw new InvalidOperationException(
                    "Flashlight forward offset requires the approved flashlight-right offset.");
            if (!(forwardOffsetMeters > 0f && forwardOffsetMeters <= 0.25f))
                throw new ArgumentOutOfRangeException(nameof(forwardOffsetMeters));

            // Keep the carrier pose untouched: this translates only the prop in
            // the carrier's forward direction after the right-hand follow pose.
            flashlightForwardOffsetMeters = forwardOffsetMeters;
            flashlightForwardOffsetConfigured = true;
        }

        public void ConfigureForwardReach(
            Animator targetAnimator,
            Quaternion shoulderReferenceRotation,
            Vector3 startHandLocal,
            Vector3 endHandLocal,
            Vector3 poleLocalDirection,
            float approachStart,
            float reachEnd,
            float holdEnd,
            float returnEnd)
        {
            if (targetAnimator == null)
                throw new ArgumentNullException(nameof(targetAnimator));
            if (!(0f <= approachStart && approachStart < reachEnd &&
                  reachEnd < holdEnd && holdEnd < returnEnd && returnEnd <= 1f))
                throw new ArgumentOutOfRangeException(
                    nameof(approachStart),
                    "Forward-reach timing must be ordered within one normalized cycle.");
            if (poleLocalDirection.sqrMagnitude <= 0.5f)
                throw new ArgumentException(
                    "Forward-reach elbow pole direction is invalid.",
                    nameof(poleLocalDirection));

            animator = targetAnimator;
            sourceUpperBodyRestored = false;
            forwardHoldConfigured = false;
            rightShoulderForwardConfigured = false;
            neutralWristConfigured = false;
            faceClearanceConfigured = false;
            rightShoulderReferenceRotation = shoulderReferenceRotation;
            reachStartHandLocal = startHandLocal;
            reachEndHandLocal = endHandLocal;
            elbowPoleLocalDirection = poleLocalDirection.normalized;
            approachStartNormalized = approachStart;
            reachEndNormalized = reachEnd;
            holdEndNormalized = holdEnd;
            returnEndNormalized = returnEnd;
            forwardReachConfigured = true;
            RefreshPreview();
        }

        public void ResetRuntimeMetrics()
        {
            maximumBodyRotationError = 0f;
            maximumBodyPositionError = 0f;
            maximumPalmForwardDeviation = 0f;
            maximumLensUpDeviation = 0f;
            lastCompletedForwardHoldDuration = 0f;
            completedForwardHoldCount = 0;
            currentRightShoulderForwardWeight = 0f;
            currentRightShoulderForwardDeviation = 0f;
            currentRightShoulderForwardLateralOffset = 0f;
            currentRightShoulderForwardElbowAngle = 0f;
            currentRightShoulderForwardHandError = 0f;
            currentNeutralWristBendAngle = 0f;
            currentFaceClearanceHandError = 0f;
            currentFaceClearanceElbowAngle = 0f;
            currentFaceClearanceAppliedOffset = 0f;
        }

        public void RefreshPreview()
        {
            if (!isActiveAndEnabled || carry == null || idleBodyPose.Count == 0) return;
            ApplyAtNormalizedPhase(ResolveNormalizedPhase());
        }

        public void RefreshPreviewAtNormalizedPhase(float normalizedPhase)
        {
            if (!isActiveAndEnabled || carry == null || idleBodyPose.Count == 0) return;
            ApplyAtNormalizedPhase(Mathf.Repeat(normalizedPhase, 1f));
        }

        public void RefreshPreviewWithoutFlashlightRightOffsetAtNormalizedPhase(
            float normalizedPhase)
        {
            if (!isActiveAndEnabled || carry == null || idleBodyPose.Count == 0) return;
            ApplyAtNormalizedPhase(
                Mathf.Repeat(normalizedPhase, 1f),
                applyFlashlightRightOffset: false);
        }

        public void RefreshPreviewWithoutFlashlightForwardOffsetAtNormalizedPhase(
            float normalizedPhase)
        {
            if (!isActiveAndEnabled || carry == null || idleBodyPose.Count == 0) return;
            ApplyAtNormalizedPhase(
                Mathf.Repeat(normalizedPhase, 1f),
                applyFlashlightRightOffset: true,
                applyFlashlightForwardOffset: false);
        }

        // Editor source analysis uses the same approved lower-body/left-arm base
        // as runtime while deliberately omitting only this right-arm correction.
        public void RefreshPreviewWithoutRightShoulderForward()
        {
            if (!isActiveAndEnabled || carry == null || idleBodyPose.Count == 0) return;
            ApplyIdleBodyPose();
            AlignFlashlightLensUp();
            ApplyFlashlightRightOffset();
            ApplyFlashlightForwardOffset();
        }

        public void RefreshPreviewWithoutFaceClearanceAtNormalizedPhase(
            float normalizedPhase)
        {
            if (!isActiveAndEnabled || carry == null || idleBodyPose.Count == 0) return;
            ApplyIdleBodyPose();
            if (sourceUpperBodyRestored && rightShoulderForwardConfigured)
                ApplyRightShoulderForward(Mathf.Repeat(normalizedPhase, 1f));
            AlignFlashlightLensUp();
            ApplyFlashlightRightOffset();
            ApplyFlashlightForwardOffset();
        }

        private void OnEnable()
        {
            ResetForwardHoldControlState();
            ResetRuntimeMetrics();
            if (reversePlayback && !Application.isPlaying) return;
            RefreshPreview();
        }

        private void OnDisable()
        {
            RestoreAnimatorSpeed();
            ResetForwardHoldControlState();
        }

        private void LateUpdate()
        {
            if (reversePlayback && !Application.isPlaying) return;
            UpdateForwardHold();
            RefreshPreview();
        }

        private void UpdateForwardHold()
        {
            if (!Application.isPlaying || !forwardHoldConfigured ||
                animator == null || !animator.isInitialized)
                return;

            if (forwardHoldActive)
            {
                float elapsed = CurrentForwardHoldElapsedSeconds;
                if (elapsed < forwardHoldDurationSeconds) return;
                RestoreForwardHoldRightArmSourcePose();
                animator.speed = 1f;
                forwardHoldActive = false;
                completedForwardHoldLoop = activeForwardHoldLoop;
                lastCompletedForwardHoldDuration = elapsed;
                completedForwardHoldCount++;
                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            float normalized = state.normalizedTime;
            int loop = Mathf.FloorToInt(normalized);
            float phase = PlaybackPhase(normalized);
            bool reverseState = reversePlayback || state.speed < 0f;
            if (reverseState)
            {
                UpdateReverseFinalHoldEntry(loop, phase);
                return;
            }

            bool outsideHoldEntry =
                phase < forwardHoldStartNormalized || phase >= 0.98f;
            if (loop == completedForwardHoldLoop || outsideHoldEntry)
                return;

            BeginForwardHold(loop, phase);
        }

        private void UpdateReverseFinalHoldEntry(int loop, float phase)
        {
            if (loop == completedForwardHoldLoop) return;
            if (!reverseFinalHoldArmed)
            {
                // The first evaluated frame can be phase zero. Arm only after
                // natural reverse playback has moved away from that entry pose.
                if (phase >= 0.90f)
                {
                    reverseFinalHoldArmed = true;
                    reverseFinalHoldArmedLoop = loop;
                }
                return;
            }

            bool reachedFinalPose =
                loop > reverseFinalHoldArmedLoop || phase <= 0.02f;
            if (!reachedFinalPose) return;

            BeginForwardHold(reverseFinalHoldArmedLoop, phase);
            reverseFinalHoldArmed = false;
            reverseFinalHoldArmedLoop = int.MinValue;
        }

        private void BeginForwardHold(int loop, float phase)
        {
            currentForwardHoldNormalizedPhase = phase;
            activeForwardHoldLoop = loop;
            forwardHoldStartedAt = Time.realtimeSinceStartupAsDouble;
            CaptureForwardHoldRightArmSourcePose();
            forwardHoldActive = true;
            animator.speed = 0f;
        }

        private void ResetForwardHoldControlState()
        {
            forwardHoldActive = false;
            reverseFinalHoldArmed = false;
            reverseFinalHoldArmedLoop = int.MinValue;
            completedForwardHoldLoop = int.MinValue;
            activeForwardHoldLoop = int.MinValue;
            forwardHoldStartedAt = 0d;
            currentForwardHoldNormalizedPhase = 0f;
            forwardHoldRightArmSourcePose.Clear();
        }

        private void RestoreAnimatorSpeed()
        {
            if (animator != null) animator.speed = 1f;
        }

        private void CaptureForwardHoldRightArmSourcePose()
        {
            forwardHoldRightArmSourcePose.Clear();
            string[] paths =
            {
                RightShoulderPath,
                RightArmPath,
                RightForeArmPath,
                RightHandPath
            };
            foreach (string path in paths)
            {
                Transform bone = RequirePath(path);
                forwardHoldRightArmSourcePose.Add(
                    new FlashlightChargeConnectBonePose(
                        path, bone.localPosition, bone.localRotation));
            }
        }

        private void RestoreForwardHoldRightArmSourcePose()
        {
            foreach (FlashlightChargeConnectBonePose pose in
                     forwardHoldRightArmSourcePose)
            {
                Transform bone = RequirePath(pose.Path);
                bone.localPosition = pose.LocalPosition;
                bone.localRotation = pose.LocalRotation;
            }
        }

        private void ApplyAtNormalizedPhase(
            float normalizedPhase,
            bool applyFlashlightRightOffset = true,
            bool applyFlashlightForwardOffset = true)
        {
            ApplyIdleBodyPose();
            if (sourceUpperBodyRestored)
            {
                if (rightShoulderForwardConfigured)
                    ApplyRightShoulderForward(normalizedPhase);
                if (faceClearanceConfigured)
                    ApplyFaceClearance();
            }
            else
            {
                if (forwardReachConfigured) ApplyForwardReach(normalizedPhase);
                ConstrainPalmForwardAnatomically();
                if (forwardReachConfigured) RestoreForwardReachAfterPalmConstraint();
            }
            AlignFlashlightLensUp();
            if (applyFlashlightRightOffset) ApplyFlashlightRightOffset();
            if (applyFlashlightForwardOffset) ApplyFlashlightForwardOffset();
            AccumulateMetrics();
        }

        private void ApplyRightShoulderForward(float normalizedPhase)
        {
            if (forwardHoldActive)
                RestoreForwardHoldRightArmSourcePose();
            currentNormalizedPhase = normalizedPhase;
            currentRightShoulderForwardWeight =
                RightShoulderForwardWeight(normalizedPhase);
            Transform shoulder = RequirePath(RightShoulderPath);
            Transform arm = RequirePath(RightArmPath);
            Transform foreArm = RequirePath(RightForeArmPath);
            Transform hand = RequirePath(RightHandPath);
            Vector3 sourceHandWorld = hand.position;
            Quaternion sourceHandRotation = hand.rotation;
            if (currentRightShoulderForwardWeight <= 0f)
            {
                UpdateRightShoulderForwardMetrics(
                    arm, foreArm, hand, hand.position);
                return;
            }
            Vector3 targetHandWorld = arm.position +
                transform.forward * rightShoulderForwardReachDistance;
            Vector3 desiredHandWorld = Vector3.Lerp(
                sourceHandWorld,
                targetHandWorld,
                currentRightShoulderForwardWeight);

            Vector3 currentDirection = hand.position - shoulder.position;
            Vector3 desiredDirection = desiredHandWorld - shoulder.position;
            if (currentDirection.sqrMagnitude > 0.000001f &&
                desiredDirection.sqrMagnitude > 0.000001f)
            {
                Quaternion shoulderCorrection = Quaternion.FromToRotation(
                    currentDirection,
                    desiredDirection);
                shoulder.rotation = Quaternion.Slerp(
                    Quaternion.identity,
                    shoulderCorrection,
                    rightShoulderForwardShoulderShare *
                    currentRightShoulderForwardWeight) * shoulder.rotation;
            }

            targetHandWorld = arm.position +
                transform.forward * rightShoulderForwardReachDistance;
            desiredHandWorld = Vector3.Lerp(
                sourceHandWorld,
                targetHandWorld,
                currentRightShoulderForwardWeight);

            SolveRightArm(
                desiredHandWorld,
                rightShoulderForwardElbowPoleLocal);
            if (neutralWristConfigured)
            {
                AlignRightWristNeutral(currentRightShoulderForwardWeight);
            }
            else
            {
                hand.rotation = Quaternion.Slerp(
                    hand.rotation,
                    sourceHandRotation,
                    rightShoulderForwardWristSourceWeight);
            }

            UpdateRightShoulderForwardMetrics(
                arm, foreArm, hand, desiredHandWorld);
        }

        private void ApplyFaceClearance()
        {
            Transform shoulder = RequirePath(RightShoulderPath);
            Transform arm = RequirePath(RightArmPath);
            Transform foreArm = RequirePath(RightForeArmPath);
            Transform hand = RequirePath(RightHandPath);
            Vector3 sourceHandWorld = hand.position;
            Vector3 desiredHandWorld = sourceHandWorld +
                transform.right * faceClearanceLateralOffset;

            Vector3 currentDirection = hand.position - shoulder.position;
            Vector3 desiredDirection = desiredHandWorld - shoulder.position;
            if (currentDirection.sqrMagnitude > 0.000001f &&
                desiredDirection.sqrMagnitude > 0.000001f)
            {
                Quaternion shoulderCorrection = Quaternion.FromToRotation(
                    currentDirection,
                    desiredDirection);
                shoulder.rotation = Quaternion.Slerp(
                    Quaternion.identity,
                    shoulderCorrection,
                    faceClearanceShoulderShare) * shoulder.rotation;
            }

            SolveRightArm(
                desiredHandWorld,
                rightShoulderForwardElbowPoleLocal);
            AlignRightWristNeutral(1f);
            currentFaceClearanceHandError = Vector3.Distance(
                hand.position,
                desiredHandWorld);
            currentFaceClearanceElbowAngle = Vector3.Angle(
                arm.position - foreArm.position,
                hand.position - foreArm.position);
            currentFaceClearanceAppliedOffset = Mathf.Abs(Vector3.Dot(
                hand.position - sourceHandWorld,
                transform.right));
            currentNeutralWristBendAngle = RightWristBendAngle();
        }

        private void AlignRightWristNeutral(float weight)
        {
            Transform foreArm = RequirePath(RightForeArmPath);
            Transform hand = RequirePath(RightHandPath);
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new MissingReferenceException(
                    "RightMiddleProximal is missing.");
            Vector3 foreArmAxis = (hand.position - foreArm.position).normalized;
            Vector3 handAxis = (middle.position - hand.position).normalized;
            if (foreArmAxis.sqrMagnitude <= 0.5f || handAxis.sqrMagnitude <= 0.5f)
                throw new InvalidOperationException(
                    "Right wrist alignment geometry is degenerate.");
            Quaternion correction = Quaternion.FromToRotation(
                handAxis,
                foreArmAxis);
            hand.rotation = Quaternion.Slerp(
                hand.rotation,
                correction * hand.rotation,
                Mathf.Clamp01(weight));
        }

        private void UpdateRightShoulderForwardMetrics(
            Transform arm,
            Transform foreArm,
            Transform hand,
            Vector3 desiredHandWorld)
        {
            Vector3 armToHand = hand.position - arm.position;
            Vector3 handLocal = transform.InverseTransformPoint(hand.position);
            Vector3 armLocal = transform.InverseTransformPoint(arm.position);
            currentRightShoulderForwardDeviation = Vector3.Angle(
                armToHand,
                transform.forward);
            currentRightShoulderForwardLateralOffset =
                Mathf.Abs(handLocal.x - armLocal.x);
            currentRightShoulderForwardElbowAngle = Vector3.Angle(
                arm.position - foreArm.position,
                hand.position - foreArm.position);
            currentRightShoulderForwardHandError = Vector3.Distance(
                hand.position,
                desiredHandWorld);
            currentNeutralWristBendAngle = RightWristBendAngle();
        }

        private float RightShoulderForwardWeight(float normalizedPhase)
        {
            if (normalizedPhase <= rightShoulderForwardStartNormalized) return 0f;
            if (normalizedPhase < rightShoulderForwardFullNormalized)
                return SmootherStep(Mathf.InverseLerp(
                    rightShoulderForwardStartNormalized,
                    rightShoulderForwardFullNormalized,
                    normalizedPhase));
            if (normalizedPhase <= rightShoulderForwardReturnNormalized) return 1f;
            if (normalizedPhase < rightShoulderForwardEndNormalized)
                return 1f - SmootherStep(Mathf.InverseLerp(
                    rightShoulderForwardReturnNormalized,
                    rightShoulderForwardEndNormalized,
                    normalizedPhase));
            return 0f;
        }

        private void ApplyForwardReach(float normalizedPhase)
        {
            currentNormalizedPhase = normalizedPhase;
            Transform shoulder = RequirePath(RightShoulderPath);
            shoulder.localRotation = rightShoulderReferenceRotation;

            currentReachWeight = ReachWeight(normalizedPhase);
            currentDesiredHandLocal = Vector3.LerpUnclamped(
                reachStartHandLocal,
                reachEndHandLocal,
                currentReachWeight);
            Vector3 desiredHandWorld = transform.TransformPoint(currentDesiredHandLocal);
            SolveRightArm(desiredHandWorld, elbowPoleLocalDirection);
        }

        private void RestoreForwardReachAfterPalmConstraint()
        {
            Vector3 desiredHandWorld = transform.TransformPoint(currentDesiredHandLocal);
            SolveRightArm(desiredHandWorld, elbowPoleLocalDirection);
            ConstrainPalmForwardAtHand();
            Transform hand = RequirePath(RightHandPath);
            currentHandPositionError = Vector3.Distance(hand.position, desiredHandWorld);
        }

        private void SolveRightArm(
            Vector3 desiredHandWorld,
            Vector3 poleLocalDirection)
        {
            Transform arm = RequirePath(RightArmPath);
            Transform foreArm = RequirePath(RightForeArmPath);
            Transform hand = RequirePath(RightHandPath);
            float upperLength = Vector3.Distance(arm.position, foreArm.position);
            float lowerLength = Vector3.Distance(foreArm.position, hand.position);
            Vector3 reach = desiredHandWorld - arm.position;
            float requestedDistance = reach.magnitude;
            if (requestedDistance <= 0.0001f || upperLength <= 0.0001f ||
                lowerLength <= 0.0001f)
                throw new InvalidOperationException(
                    name + " right-arm reach geometry is degenerate.");

            float minimumDistance = Mathf.Abs(upperLength - lowerLength) + 0.0001f;
            float maximumDistance = upperLength + lowerLength - 0.0001f;
            float distance = Mathf.Clamp(
                requestedDistance, minimumDistance, maximumDistance);
            Vector3 direction = reach / requestedDistance;
            Vector3 poleWorld = transform.TransformDirection(
                poleLocalDirection).normalized;
            Vector3 bendDirection = Vector3.ProjectOnPlane(
                poleWorld, direction).normalized;
            if (bendDirection.sqrMagnitude <= 0.5f)
                bendDirection = Vector3.ProjectOnPlane(
                    transform.right - transform.up * 0.35f,
                    direction).normalized;

            float along =
                (upperLength * upperLength - lowerLength * lowerLength +
                 distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(
                0f, upperLength * upperLength - along * along));
            Vector3 elbowWorld = arm.position + direction * along +
                bendDirection * height;

            arm.rotation = Quaternion.FromToRotation(
                foreArm.position - arm.position,
                elbowWorld - arm.position) * arm.rotation;
            foreArm.rotation = Quaternion.FromToRotation(
                hand.position - foreArm.position,
                desiredHandWorld - foreArm.position) * foreArm.rotation;
        }

        private float ResolveNormalizedPhase()
        {
            if (!Application.isPlaying || animator == null || !animator.isInitialized)
                return 0f;
            return PlaybackPhase(
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }

        private float PlaybackPhase(float stateNormalizedTime) =>
            reversePlayback
                ? Mathf.Repeat(1f - stateNormalizedTime, 1f)
                : Mathf.Repeat(stateNormalizedTime, 1f);

        private float ReachWeight(float normalizedPhase)
        {
            if (normalizedPhase <= approachStartNormalized) return 0f;
            if (normalizedPhase < reachEndNormalized)
                return SmootherStep(Mathf.InverseLerp(
                    approachStartNormalized, reachEndNormalized, normalizedPhase));
            if (normalizedPhase <= holdEndNormalized) return 1f;
            if (normalizedPhase < returnEndNormalized)
                return 1f - SmootherStep(Mathf.InverseLerp(
                    holdEndNormalized, returnEndNormalized, normalizedPhase));
            return 0f;
        }

        private static float SmootherStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value *
                (value * (value * 6f - 15f) + 10f);
        }

        private void ApplyIdleBodyPose()
        {
            foreach (FlashlightChargeConnectBonePose pose in idleBodyPose)
            {
                Transform bone = RequirePath(pose.Path);
                bone.localPosition = pose.LocalPosition;
                bone.localRotation = pose.LocalRotation;
            }
        }

        private void ConstrainPalmForwardAnatomically()
        {
            Transform[] bones =
            {
                RequirePath(RightShoulderPath),
                RequirePath(RightArmPath),
                RequirePath(RightForeArmPath)
            };
            Transform[] children =
            {
                RequirePath(RightArmPath),
                RequirePath(RightForeArmPath),
                RequirePath(RightHandPath)
            };

            for (int iteration = 0; iteration < 12; iteration++)
            {
                if (Vector3.Angle(RightPalmNormal(), transform.forward) <= 0.1f) break;
                for (int index = 0; index < bones.Length; index++)
                {
                    Vector3 axis = (children[index].position - bones[index].position).normalized;
                    Vector3 current = Vector3.ProjectOnPlane(
                        RightPalmNormal(), axis).normalized;
                    Vector3 desired = Vector3.ProjectOnPlane(
                        transform.forward, axis).normalized;
                    if (current.sqrMagnitude <= 0.5f || desired.sqrMagnitude <= 0.5f)
                        continue;
                    float angle = Vector3.SignedAngle(current, desired, axis) *
                        RightArmRotationShares[index];
                    bones[index].rotation = Quaternion.AngleAxis(angle, axis) *
                        bones[index].rotation;
                }
            }

            ConstrainPalmForwardAtHand();
        }

        private void ConstrainPalmForwardAtHand()
        {
            Transform hand = RequirePath(RightHandPath);
            Quaternion residual = Quaternion.FromToRotation(
                RightPalmNormal(), transform.forward);
            hand.rotation = residual * hand.rotation;
        }

        private void AlignFlashlightLensUp()
        {
            Transform holder = carry.Holder != null
                ? carry.Holder.transform
                : throw new MissingReferenceException(
                    name + " flashlight holder is missing.");
            Quaternion correction = Quaternion.FromToRotation(
                holder.up, transform.up);
            holder.rotation = correction * holder.rotation;
        }

        private void ApplyFlashlightRightOffset()
        {
            if (!flashlightRightOffsetConfigured) return;
            Transform holder = carry.Holder != null
                ? carry.Holder.transform
                : throw new MissingReferenceException(
                    name + " flashlight holder is missing.");
            holder.position += transform.right * flashlightRightOffsetMeters;
        }

        private void ApplyFlashlightForwardOffset()
        {
            if (!flashlightForwardOffsetConfigured) return;
            Transform holder = carry.Holder != null
                ? carry.Holder.transform
                : throw new MissingReferenceException(
                    name + " flashlight holder is missing.");
            holder.position += transform.forward * flashlightForwardOffsetMeters;
        }

        private void AccumulateMetrics()
        {
            foreach (FlashlightChargeConnectBonePose pose in idleBodyPose)
            {
                Transform bone = RequirePath(pose.Path);
                maximumBodyRotationError = Mathf.Max(
                    maximumBodyRotationError,
                    Quaternion.Angle(bone.localRotation, pose.LocalRotation));
                maximumBodyPositionError = Mathf.Max(
                    maximumBodyPositionError,
                    Vector3.Distance(bone.localPosition, pose.LocalPosition));
            }
            maximumPalmForwardDeviation = Mathf.Max(
                maximumPalmForwardDeviation,
                PalmForwardDeviationDegrees);
            maximumLensUpDeviation = Mathf.Max(
                maximumLensUpDeviation,
                LensUpDeviationDegrees);
        }

        private Transform RequirePath(string path)
        {
            return transform.Find(path) ?? throw new MissingReferenceException(
                name + " path is missing: " + path);
        }

        private Vector3 RightPalmNormal()
        {
            Transform hand = RequirePath(RightHandPath);
            Transform index = hand.Find("RightIndexProximal") ??
                throw new MissingReferenceException("RightIndexProximal is missing.");
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new MissingReferenceException("RightMiddleProximal is missing.");
            Transform little = hand.Find("RightLittleProximal") ??
                throw new MissingReferenceException("RightLittleProximal is missing.");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 fingers = (middle.position - hand.position).normalized;
            Vector3 normal = Vector3.Cross(width, fingers).normalized;
            if (normal.sqrMagnitude < 0.99f)
                throw new InvalidOperationException("Right palm normal is degenerate.");
            return normal;
        }

        private float RightWristBendAngle()
        {
            Transform foreArm = RequirePath(RightForeArmPath);
            Transform hand = RequirePath(RightHandPath);
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new MissingReferenceException(
                    "RightMiddleProximal is missing.");
            return Vector3.Angle(
                hand.position - foreArm.position,
                middle.position - hand.position);
        }
    }
}
