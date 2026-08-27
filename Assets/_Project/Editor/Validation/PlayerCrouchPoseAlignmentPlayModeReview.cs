using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerCrouchPoseAlignmentPlayModeReview
    {
        private const string StageKey =
            "Bellerophon.PlayerCrouchPoseAlignment.ReviewStage";
        private const string ForwardLeftArmStageKey =
            "Bellerophon.PlayerCrouchForwardLeftArmStraightDown.ReviewStage";
        private const string ReviewMetricsPath =
            "docs/validation/player_crouch_pose_alignment_review_metrics.json";
        private const string EnterMetricsPath =
            "docs/validation/player_crouch_enter_corrected_metrics.json";
        private const string IdleMetricsPath =
            "docs/validation/player_crouch_idle_review_metrics.json";
        private const string ForwardMetricsPath =
            "docs/validation/player_crouch_forward_review_metrics.json";
        private const string EnterContactPath =
            "docs/validation/player_crouch_pose_alignment_enter_review_contact_sheet.png";
        private const string IdleContactPath =
            "docs/validation/player_crouch_pose_alignment_idle_review_contact_sheet.png";
        private const string ForwardContactPath =
            "docs/validation/player_crouch_pose_alignment_forward_review_contact_sheet.png";
        private const string EnterFinalPath =
            "docs/validation/player_crouch_pose_alignment_enter_final.png";
        private const string IdleFinalPath =
            "docs/validation/player_crouch_pose_alignment_idle_final.png";
        private const string ForwardFinalPath =
            "docs/validation/player_crouch_pose_alignment_forward_final.png";
        private const string ForwardLeftArmReviewMetricsPath =
            "docs/validation/player_crouch_forward_left_arm_straight_down_review_metrics.json";
        private const string ForwardLeftArmContactPath =
            "docs/validation/player_crouch_forward_left_arm_straight_down_review_contact_sheet.png";
        private const string ForwardLeftArmFinalPath =
            "docs/validation/player_crouch_forward_left_arm_straight_down_final.png";
        private const float RotationTolerance = 0.001f;
        private const float PosePositionTolerance = 0.0001f;
        private const float PoseRotationTolerance = 0.01f;
        private const float SwingTolerance = 0.2f;
        private const float ForwardLeftArmSwingTolerance =
            PlayerCrouchPoseAlignmentTool.ForwardLeftArmSwingSerializationToleranceDegrees;
        private const float ArmAlignmentTolerance = 0.1f;
        private const float UpperBodyCenterTolerance = 0.001f;
        private const float DesiredWaistAngleDegrees = 80f;
        private const float DesiredHeadDownDegrees =
            PlayerCrouchPoseAlignmentTool.DesiredHeadDownDegrees;
        private const float ArmTorsoClearanceDegrees =
            PlayerCrouchPoseAlignmentTool.ArmTorsoClearanceDegrees;
        private const float ForwardArmAdvanceDegrees =
            PlayerCrouchPoseAlignmentTool.ForwardArmAdvanceDegrees;
        private const float ForwardLeftArmDownDegrees =
            PlayerCrouchPoseAlignmentTool.ForwardLeftArmDownDegrees;
        private const int CaptureWidth = 400;
        private const int CaptureHeight = 500;

        private static readonly string[] ArmBones =
        {
            "LeftShoulder",
            "LeftArm",
            "LeftForeArm",
            "RightShoulder",
            "RightArm",
            "RightForeArm"
        };

        [Serializable]
        private sealed class ExistingReviewMetrics
        {
            public int loopsSampled;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class AlignmentReviewMetrics
        {
            public string targetSet;
            public string waistReference;
            public string armReference;
            public float enterWaistMeanDifferenceDegreesMax;
            public float idleWaistMeanDifferenceDegreesMax;
            public float enterIdleWaistDifferenceDegreesMax;
            public float forwardArmMeanDifferenceDegreesMax;
            public float forwardArmRangeDifferenceDegreesMax;
            public float enterWaistAngleDegrees;
            public float idleWaistAngleDegrees;
            public float enterHeadDownDegrees;
            public float idleHeadDownDegrees;
            public float enterHeadAngleDifferenceDegreesMax;
            public float idleHeadAngleDifferenceDegreesMax;
            public float armTorsoClearanceDegrees;
            public float forwardArmAdvanceDegrees;
            public float forwardUpperBodyCenterCorrectionDegrees;
            public float forwardUpperBodyMeanLateralOffset;
            public float rightArmAdditionalClearanceDegrees;
            public float forwardLeftArmDownDegrees;
            public bool leftArmCurvesUnchanged;
            public bool rightArmCurvesUnchanged;
            public bool enterNonHeadCurvesUnchanged;
            public bool idleNonHeadCurvesUnchanged;
            public int enterLoopsReviewed;
            public int idleLoopsReviewed;
            public int forwardLoopsReviewed;
            public bool enterReviewPassed;
            public bool idleReviewPassed;
            public bool forwardReviewPassed;
            public float enterHoldPositionDifferenceMax;
            public float enterHoldRotationDifferenceDegreesMax;
            public float idleStaticPositionDifferenceMax;
            public float idleStaticRotationDifferenceDegreesMax;
            public float enterIdlePositionDifferenceMax;
            public float enterIdleRotationDifferenceDegreesMax;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public float rootDisplacementMax;
            public float forwardHipsHorizontalDisplacementMax;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ForwardLeftArmStraightDownReviewMetrics
        {
            public string target;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootDisplacementMax;
            public float hipsHorizontalDisplacementMax;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public float leftElbowBendDegreesMax;
            public float leftElbowBendDegreesMaxApply;
            public float leftArmDownwardMeanAngleDegrees;
            public float leftArmDownwardMaximumAngleDegrees;
            public float leftHandKneeMinimumBoneGap;
            public float leftHandKneeMinimumBoneGapTarget;
            public float leftShoulderSwingDifferenceDegreesMaxApply;
            public float leftUpperArmSwingDifferenceDegreesMaxApply;
            public bool curvesOutsideLeftArmUnchanged;
            public bool rightArmCurvesUnchanged;
            public bool clipIsLooping;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class ActualPose
        {
            internal readonly Dictionary<string, Vector3> Positions =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Quaternion> Rotations =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        }

        private sealed class ActualTargetData
        {
            internal string Name;
            internal int FramesPerLoop;
            internal int SourceMotionFrames;
            internal ActualPose[] Poses;
            internal Quaternion[] WaistRotations;
            internal Vector3[] WaistDirections;
            internal Vector3[] HeadForwardDirections;
            internal float[] UpperBodyLateralOffsets;
            internal readonly Dictionary<string, Quaternion[]> ArmRotations =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            internal readonly Dictionary<int, ActualPose> RenderedPoses =
                new Dictionary<int, ActualPose>();
            internal readonly Dictionary<int, Quaternion> RenderedWaistRotations =
                new Dictionary<int, Quaternion>();
            internal readonly Dictionary<int, Vector3> RenderedWaistDirections =
                new Dictionary<int, Vector3>();
            internal readonly Dictionary<int, Vector3> RenderedHeadForwardDirections =
                new Dictionary<int, Vector3>();
            internal readonly Dictionary<string, Dictionary<int, Quaternion>>
                RenderedArmRotations = new Dictionary<
                    string,
                    Dictionary<int, Quaternion>>(StringComparer.Ordinal);
            internal Vector3[] LeftUpperArmDirections;
            internal Vector3[] LeftForeArmDirections;
            internal Vector3[] LeftHandPositions;
            internal Vector3[] LeftKneePositions;
            internal float LeftElbowBendDegreesMax;
            internal float LeftArmDownwardMeanAngleDegrees;
            internal float LeftArmDownwardMaximumAngleDegrees;
            internal float LeftHandKneeMinimumBoneGap;
            internal float RootDisplacementMax;
            internal float HipsHorizontalDisplacementMax;
            internal float LoopPositionDifferenceMax;
            internal float LoopRotationDifferenceMax;
        }

        private sealed class RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;
            private readonly SkinnedMeshRenderer skinnedRenderer;
            private readonly bool updateWhenOffscreen;

            internal RendererState(Renderer rendererValue)
            {
                renderer = rendererValue;
                enabled = rendererValue.enabled;
                skinnedRenderer = rendererValue as SkinnedMeshRenderer;
                updateWhenOffscreen = skinnedRenderer != null &&
                                      skinnedRenderer.updateWhenOffscreen;
            }

            internal void Restore()
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }

                if (skinnedRenderer != null)
                {
                    skinnedRenderer.updateWhenOffscreen = updateWhenOffscreen;
                }
            }
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Pose Alignment Review")]
        internal static void CaptureActualReview()
        {
            int stage = SessionState.GetInt(StageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch pose alignment review must start in Edit Mode.");
                    }

                    Scene scene = RequireActualScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before crouch pose alignment review.");
                    }

                    SessionState.SetInt(StageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerCrouchPoseAlignment] Entering Play Mode for combined three-target review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch pose alignment capture requires Play Mode.");
                    }

                    CaptureActualThreeTargetReview();
                    SessionState.SetInt(StageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch pose alignment review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(StageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerCrouchPoseAlignment] Exiting Play Mode after combined three-target review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Crouch pose alignment actual review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(StageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Pose Alignment Final")]
        internal static void CaptureActualFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment final requires Edit Mode.");
            }

            AlignmentReviewMetrics metrics = ReadJson<AlignmentReviewMetrics>(
                ReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                metrics.enterLoopsReviewed != 2 ||
                metrics.idleLoopsReviewed != 2 ||
                metrics.forwardLoopsReviewed != 2)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment actual review did not pass before final capture.");
            }

            CopyReviewedContact(EnterContactPath, EnterFinalPath);
            CopyReviewedContact(IdleContactPath, IdleFinalPath);
            CopyReviewedContact(ForwardContactPath, ForwardFinalPath);
            Debug.Log(
                "[PlayerCrouchPoseAlignment] Final copied once from combined actual Play Mode review." +
                " Enter=" + Path.GetFullPath(EnterFinalPath) +
                ", Idle=" + Path.GetFullPath(IdleFinalPath) +
                ", Forward=" + Path.GetFullPath(ForwardFinalPath) +
                ", LoopsPerTarget=2, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Forward Left Arm Straight Down Review")]
        internal static void CaptureForwardLeftArmStraightDownReview()
        {
            int stage = SessionState.GetInt(ForwardLeftArmStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Forward left-arm review must start in Edit Mode.");
                    }

                    Scene scene = RequireActualScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before the Forward left-arm review.");
                    }

                    SessionState.SetInt(ForwardLeftArmStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerCrouchPoseAlignment] Entering Play Mode for Forward left-arm review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Forward left-arm capture requires Play Mode.");
                    }

                    CaptureForwardLeftArmStraightDownActualReview();
                    SessionState.SetInt(ForwardLeftArmStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Forward left-arm review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(ForwardLeftArmStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerCrouchPoseAlignment] Exiting Play Mode after Forward left-arm review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Forward left-arm review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(ForwardLeftArmStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Forward Left Arm Straight Down Final")]
        internal static void CaptureForwardLeftArmStraightDownFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Forward left-arm final capture requires Edit Mode.");
            }

            ForwardLeftArmStraightDownReviewMetrics metrics =
                ReadJson<ForwardLeftArmStraightDownReviewMetrics>(
                    ForwardLeftArmReviewMetricsPath);
            if (!metrics.passedNumericChecks || metrics.loopsSampled != 2)
            {
                throw new InvalidOperationException(
                    "Forward left-arm review did not pass before final capture.");
            }

            CopyReviewedContact(
                ForwardLeftArmContactPath,
                ForwardLeftArmFinalPath);
            Debug.Log(
                "[PlayerCrouchPoseAlignment] Forward left-arm final copied once from actual Play Mode review." +
                " Final=" + Path.GetFullPath(ForwardLeftArmFinalPath) +
                ", Loops=2, SceneChanged=False.");
        }

        private static void CaptureForwardLeftArmStraightDownActualReview()
        {
            Scene scene = RequireActualScene();
            bool sceneWasDirty = scene.isDirty;
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            AnimationClip forwardClip = RequireAnimatorClip(
                forwardTarget,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            int forwardFrames = Mathf.RoundToInt(
                forwardClip.length * forwardClip.frameRate);
            ActualTargetData forward = CaptureActualTarget(
                scene,
                forwardTarget,
                forwardClip,
                PlayerCrouchIdleForwardAnimationTool.ForwardStateName,
                0,
                PhaseIndices(forwardFrames),
                ForwardLeftArmContactPath);
            PlayerCrouchPoseAlignmentTool.ForwardLeftArmStraightDownApplyMetrics
                apply = ReadJson<
                    PlayerCrouchPoseAlignmentTool
                        .ForwardLeftArmStraightDownApplyMetrics>(
                    PlayerCrouchPoseAlignmentTool
                        .ForwardLeftArmStraightDownMetricsPath);
            Animator animator = forwardTarget.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    "Player_Crouch_Forward Animator is missing.");

            ForwardLeftArmStraightDownReviewMetrics metrics =
                new ForwardLeftArmStraightDownReviewMetrics
                {
                    target = forwardTarget.name,
                    framesPerLoop = forward.FramesPerLoop,
                    framesSampled = forward.FramesPerLoop * 2,
                    loopsSampled = 2,
                    rootDisplacementMax = forward.RootDisplacementMax,
                    hipsHorizontalDisplacementMax =
                        forward.HipsHorizontalDisplacementMax,
                    loopPositionDifferenceMax =
                        forward.LoopPositionDifferenceMax,
                    loopRotationDifferenceDegreesMax =
                        forward.LoopRotationDifferenceMax,
                    leftElbowBendDegreesMax =
                        forward.LeftElbowBendDegreesMax,
                    leftElbowBendDegreesMaxApply =
                        apply.leftElbowBendDegreesMaxAfter,
                    leftArmDownwardMeanAngleDegrees =
                        forward.LeftArmDownwardMeanAngleDegrees,
                    leftArmDownwardMaximumAngleDegrees =
                        forward.LeftArmDownwardMaximumAngleDegrees,
                    leftHandKneeMinimumBoneGap =
                        forward.LeftHandKneeMinimumBoneGap,
                    leftHandKneeMinimumBoneGapTarget =
                        apply.leftHandKneeMinimumBoneGapTarget,
                    leftShoulderSwingDifferenceDegreesMaxApply =
                        apply.leftShoulderSwingDifferenceDegreesMax,
                    leftUpperArmSwingDifferenceDegreesMaxApply =
                        apply.leftUpperArmSwingDifferenceDegreesMax,
                    curvesOutsideLeftArmUnchanged =
                        apply.curvesOutsideLeftArmUnchanged,
                    rightArmCurvesUnchanged = apply.rightArmCurvesUnchanged,
                    clipIsLooping = forwardClip.isLooping,
                    applyRootMotion = animator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                apply.passedNumericChecks &&
                metrics.framesPerLoop > 0 &&
                metrics.framesSampled == metrics.framesPerLoop * 2 &&
                metrics.loopsSampled == 2 &&
                metrics.rootDisplacementMax <= PosePositionTolerance &&
                metrics.hipsHorizontalDisplacementMax <= PosePositionTolerance &&
                metrics.loopPositionDifferenceMax <= PosePositionTolerance &&
                metrics.loopRotationDifferenceDegreesMax <=
                    PoseRotationTolerance &&
                metrics.leftElbowBendDegreesMax <=
                    PlayerCrouchPoseAlignmentTool
                        .ForwardLeftElbowStraightToleranceDegrees &&
                metrics.leftElbowBendDegreesMaxApply <=
                    PlayerCrouchPoseAlignmentTool
                        .ForwardLeftElbowStraightToleranceDegrees &&
                metrics.leftArmDownwardMeanAngleDegrees <=
                    PlayerCrouchPoseAlignmentTool
                        .ForwardLeftArmDownwardMeanToleranceDegrees &&
                metrics.leftArmDownwardMaximumAngleDegrees <=
                    PlayerCrouchPoseAlignmentTool
                        .ForwardLeftArmDownwardMaximumToleranceDegrees &&
                metrics.leftHandKneeMinimumBoneGap >=
                    metrics.leftHandKneeMinimumBoneGapTarget -
                    PlayerCrouchPoseAlignmentTool
                        .ForwardLeftHandKneeGapTolerance &&
                metrics.leftShoulderSwingDifferenceDegreesMaxApply <=
                    ForwardLeftArmSwingTolerance &&
                metrics.leftUpperArmSwingDifferenceDegreesMaxApply <=
                    ForwardLeftArmSwingTolerance &&
                metrics.curvesOutsideLeftArmUnchanged &&
                metrics.rightArmCurvesUnchanged &&
                metrics.clipIsLooping &&
                !metrics.applyRootMotion;
            WriteJson(ForwardLeftArmReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Forward left-arm actual Play Mode support checks failed." +
                    " Elbow=" + Num(metrics.leftElbowBendDegreesMax) +
                    ", DownMean=" + Num(
                        metrics.leftArmDownwardMeanAngleDegrees) +
                    ", DownMax=" + Num(
                        metrics.leftArmDownwardMaximumAngleDegrees) +
                    ", KneeGap=" + Num(
                        metrics.leftHandKneeMinimumBoneGap) +
                    ", LoopRotation=" + Num(
                        metrics.loopRotationDifferenceDegreesMax) + ".");
            }

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "Forward left-arm review changed the scene dirty state.");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Captured Forward left-arm actual Play Mode two-loop review." +
                " Elbow=" + Num(metrics.leftElbowBendDegreesMax) +
                ", DownMean=" + Num(
                    metrics.leftArmDownwardMeanAngleDegrees) +
                ", DownMax=" + Num(
                    metrics.leftArmDownwardMaximumAngleDegrees) +
                ", KneeGap=" + Num(
                    metrics.leftHandKneeMinimumBoneGap) +
                ", RootDisplacement=" + Num(metrics.rootDisplacementMax) +
                ", Loops=2.");
        }

        private static void CaptureActualThreeTargetReview()
        {
            Scene scene = RequireActualScene();
            bool sceneWasDirty = scene.isDirty;
            Transform enterTarget = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Transform idleTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            AnimationClip enterClip = RequireAnimatorClip(
                enterTarget,
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip idleClip = RequireAnimatorClip(
                idleTarget,
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            AnimationClip forwardClip = RequireAnimatorClip(
                forwardTarget,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);

            int enterFrames = Mathf.RoundToInt(
                enterClip.length * enterClip.frameRate);
            int enterSourceFrames = Mathf.RoundToInt(
                (enterClip.length -
                 PlayerCrouchEnterAnimationTool.HoldDurationSeconds) *
                enterClip.frameRate);
            ActualTargetData enter = CaptureActualTarget(
                scene,
                enterTarget,
                enterClip,
                PlayerCrouchEnterAnimationTool.StateName,
                enterSourceFrames,
                new[]
                {
                    0,
                    enterSourceFrames / 2,
                    enterSourceFrames - 1,
                    enterSourceFrames,
                    enterFrames - 1
                },
                EnterContactPath);
            int idleFrames = Mathf.RoundToInt(
                PlayerCrouchIdleForwardAnimationTool.IdleDurationSeconds *
                idleClip.frameRate);
            ActualTargetData idle = CaptureActualTarget(
                scene,
                idleTarget,
                idleClip,
                PlayerCrouchIdleForwardAnimationTool.IdleStateName,
                0,
                PhaseIndices(idleFrames),
                IdleContactPath);
            int forwardFrames = Mathf.RoundToInt(
                forwardClip.length * forwardClip.frameRate);
            ActualTargetData forward = CaptureActualTarget(
                scene,
                forwardTarget,
                forwardClip,
                PlayerCrouchIdleForwardAnimationTool.ForwardStateName,
                0,
                PhaseIndices(forwardFrames),
                ForwardContactPath);

            Vector3 enterWaist = enter.RenderedWaistDirections[
                enter.SourceMotionFrames];
            Vector3 idleWaist = idle.RenderedWaistDirections[0];
            Vector3 enterHeadForward = enter.RenderedHeadForwardDirections[
                enter.SourceMotionFrames];
            Vector3 idleHeadForward = idle.RenderedHeadForwardDirections[0];
            float enterWaistAngle = GroundAngleDegrees(enterWaist);
            float idleWaistAngle = GroundAngleDegrees(idleWaist);
            Vector3 desiredEnterHead = DesiredHeadForwardDirection(
                enterWaist,
                DesiredHeadDownDegrees);
            Vector3 desiredIdleHead = DesiredHeadForwardDirection(
                idleWaist,
                DesiredHeadDownDegrees);
            PlayerCrouchPoseAlignmentTool.AlignmentApplyMetrics apply =
                ReadJson<PlayerCrouchPoseAlignmentTool.AlignmentApplyMetrics>(
                    PlayerCrouchPoseAlignmentTool.ApplyMetricsPath);
            Quaternion upperBodyCenterCorrection = Quaternion.AngleAxis(
                apply.forwardUpperBodyCenterCorrectionDegrees,
                Vector3.forward);
            Quaternion rightArmClearance = Quaternion.AngleAxis(
                apply.rightArmAdditionalClearanceDegrees,
                Vector3.forward);
            Quaternion leftArmDown = Quaternion.AngleAxis(
                apply.forwardLeftArmDownDegrees,
                Vector3.right);
            float forwardArmMeanDifference = 0f;
            foreach (string boneName in ArmBones)
            {
                Quaternion enterReference = enter.RenderedArmRotations[boneName][
                    enter.SourceMotionFrames];
                Quaternion advancedEnter = Quaternion.AngleAxis(
                    -ForwardArmAdvanceDegrees,
                    Vector3.right) * enterReference;
                Quaternion expectedForward = boneName.StartsWith(
                    "Left",
                    StringComparison.Ordinal)
                    ? leftArmDown * upperBodyCenterCorrection * advancedEnter
                    : upperBodyCenterCorrection *
                      rightArmClearance *
                      advancedEnter;
                Quaternion forwardMean = ActualQuaternionMean(
                    forward.ArmRotations[boneName]);
                forwardArmMeanDifference = Mathf.Max(
                    forwardArmMeanDifference,
                    Quaternion.Angle(expectedForward, forwardMean));
            }

            MaxPoseDifference(
                enter.RenderedPoses[enter.SourceMotionFrames],
                enter.Poses
                    .Skip(enter.SourceMotionFrames)
                    .Take(enter.FramesPerLoop - enter.SourceMotionFrames)
                    .Concat(enter.Poses
                        .Skip(enter.FramesPerLoop + enter.SourceMotionFrames)
                        .Take(enter.FramesPerLoop - enter.SourceMotionFrames)),
                out float enterHoldPosition,
                out float enterHoldRotation);
            MaxPoseDifference(
                idle.RenderedPoses[0],
                idle.Poses.Skip(1),
                out float idleStaticPosition,
                out float idleStaticRotation);
            float enterIdlePosition = PosePositionDifference(
                enter.RenderedPoses[enter.SourceMotionFrames],
                idle.RenderedPoses[0]);
            float enterIdleRotation = PoseRotationDifference(
                enter.RenderedPoses[enter.SourceMotionFrames],
                idle.RenderedPoses[0]);
            float forwardUpperBodyMeanLateralOffset =
                forward.UpperBodyLateralOffsets.Average();

            AlignmentReviewMetrics metrics = new AlignmentReviewMetrics
            {
                targetSet =
                    "Player_Crouch_Enter, Player_Crouch_Idle, Player_Crouch_Forward",
                waistReference =
                    "Target-relative Spine02-to-Spine axis at 80 degrees above the ground plane",
                armReference =
                    "Enter and Idle bilateral torso clearance; Forward adds shoulder-driven advance and preserves per-frame swing",
                enterWaistAngleDegrees = enterWaistAngle,
                idleWaistAngleDegrees = idleWaistAngle,
                enterHeadDownDegrees = Vector3.Angle(
                    TorsoForwardDirection(enterWaist),
                    enterHeadForward),
                idleHeadDownDegrees = Vector3.Angle(
                    TorsoForwardDirection(idleWaist),
                    idleHeadForward),
                enterWaistMeanDifferenceDegreesMax =
                    Mathf.Abs(enterWaistAngle - DesiredWaistAngleDegrees),
                idleWaistMeanDifferenceDegreesMax =
                    Mathf.Abs(idleWaistAngle - DesiredWaistAngleDegrees),
                enterIdleWaistDifferenceDegreesMax =
                    Vector3.Angle(enterWaist, idleWaist),
                enterHeadAngleDifferenceDegreesMax =
                    Vector3.Angle(enterHeadForward, desiredEnterHead),
                idleHeadAngleDifferenceDegreesMax =
                    Vector3.Angle(idleHeadForward, desiredIdleHead),
                forwardArmMeanDifferenceDegreesMax =
                    forwardArmMeanDifference,
                forwardArmRangeDifferenceDegreesMax =
                    apply.forwardArmRangeDifferenceDegreesMax,
                armTorsoClearanceDegrees = ArmTorsoClearanceDegrees,
                forwardArmAdvanceDegrees = ForwardArmAdvanceDegrees,
                forwardUpperBodyCenterCorrectionDegrees =
                    apply.forwardUpperBodyCenterCorrectionDegrees,
                forwardUpperBodyMeanLateralOffset =
                    forwardUpperBodyMeanLateralOffset,
                rightArmAdditionalClearanceDegrees =
                    apply.rightArmAdditionalClearanceDegrees,
                forwardLeftArmDownDegrees =
                    apply.forwardLeftArmDownDegrees,
                leftArmCurvesUnchanged = apply.leftArmCurvesUnchanged,
                rightArmCurvesUnchanged = apply.rightArmCurvesUnchanged,
                enterNonHeadCurvesUnchanged =
                    apply.enterNonHeadCurvesUnchanged,
                idleNonHeadCurvesUnchanged =
                    apply.idleNonHeadCurvesUnchanged,
                enterLoopsReviewed = 2,
                idleLoopsReviewed = 2,
                forwardLoopsReviewed = 2,
                enterHoldPositionDifferenceMax = enterHoldPosition,
                enterHoldRotationDifferenceDegreesMax = enterHoldRotation,
                idleStaticPositionDifferenceMax = idleStaticPosition,
                idleStaticRotationDifferenceDegreesMax = idleStaticRotation,
                enterIdlePositionDifferenceMax = enterIdlePosition,
                enterIdleRotationDifferenceDegreesMax = enterIdleRotation,
                loopPositionDifferenceMax = Mathf.Max(
                    enter.LoopPositionDifferenceMax,
                    Mathf.Max(
                        idle.LoopPositionDifferenceMax,
                        forward.LoopPositionDifferenceMax)),
                loopRotationDifferenceDegreesMax = Mathf.Max(
                    enter.LoopRotationDifferenceMax,
                    Mathf.Max(
                        idle.LoopRotationDifferenceMax,
                        forward.LoopRotationDifferenceMax)),
                rootDisplacementMax = Mathf.Max(
                    enter.RootDisplacementMax,
                    Mathf.Max(
                        idle.RootDisplacementMax,
                        forward.RootDisplacementMax)),
                forwardHipsHorizontalDisplacementMax =
                    forward.HipsHorizontalDisplacementMax,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            metrics.enterReviewPassed =
                metrics.enterHoldPositionDifferenceMax <= PosePositionTolerance &&
                metrics.enterHoldRotationDifferenceDegreesMax <=
                PoseRotationTolerance;
            metrics.idleReviewPassed =
                metrics.idleStaticPositionDifferenceMax <= PosePositionTolerance &&
                metrics.idleStaticRotationDifferenceDegreesMax <=
                PoseRotationTolerance &&
                metrics.enterIdlePositionDifferenceMax <= PosePositionTolerance &&
                metrics.enterIdleRotationDifferenceDegreesMax <=
                PoseRotationTolerance;
            metrics.forwardReviewPassed =
                metrics.forwardHipsHorizontalDisplacementMax <=
                    PosePositionTolerance &&
                Mathf.Abs(metrics.forwardUpperBodyMeanLateralOffset) <=
                    UpperBodyCenterTolerance &&
                metrics.rightArmCurvesUnchanged;
            metrics.passedNumericChecks =
                apply.passedNumericChecks &&
                metrics.enterReviewPassed &&
                metrics.idleReviewPassed &&
                metrics.forwardReviewPassed &&
                metrics.enterWaistMeanDifferenceDegreesMax <=
                PoseRotationTolerance &&
                metrics.idleWaistMeanDifferenceDegreesMax <=
                PoseRotationTolerance &&
                metrics.enterIdleWaistDifferenceDegreesMax <= RotationTolerance &&
                metrics.enterHeadAngleDifferenceDegreesMax <=
                PoseRotationTolerance &&
                metrics.idleHeadAngleDifferenceDegreesMax <=
                    PoseRotationTolerance &&
                metrics.forwardArmMeanDifferenceDegreesMax <=
                    ArmAlignmentTolerance &&
                metrics.forwardArmRangeDifferenceDegreesMax <=
                    ForwardLeftArmSwingTolerance &&
                metrics.loopPositionDifferenceMax <= PosePositionTolerance &&
                metrics.loopRotationDifferenceDegreesMax <= PoseRotationTolerance &&
                metrics.rootDisplacementMax <= PosePositionTolerance;
            WriteJson(ReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment actual Play Mode support checks failed." +
                    " EnterWaist=" + Num(
                        metrics.enterWaistMeanDifferenceDegreesMax) +
                    ", IdleWaist=" + Num(
                        metrics.idleWaistMeanDifferenceDegreesMax) +
                    ", EnterHead=" + Num(
                        metrics.enterHeadAngleDifferenceDegreesMax) +
                    ", IdleHead=" + Num(
                        metrics.idleHeadAngleDifferenceDegreesMax) +
                    ", ForwardArms=" + Num(
                        metrics.forwardArmMeanDifferenceDegreesMax) +
                    ", ArmSwing=" + Num(
                        metrics.forwardArmRangeDifferenceDegreesMax) +
                    ", EnterIdleRotation=" + Num(
                        metrics.enterIdleRotationDifferenceDegreesMax) +
                    ", LoopRotation=" + Num(
                        metrics.loopRotationDifferenceDegreesMax) + ".");
            }

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment review changed the scene dirty state.");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Captured combined actual Play Mode two-loop review." +
                " EnterWaistActual=" + Num(metrics.enterWaistAngleDegrees) +
                ", IdleWaistActual=" + Num(metrics.idleWaistAngleDegrees) +
                ", EnterHeadDownActual=" + Num(metrics.enterHeadDownDegrees) +
                ", IdleHeadDownActual=" + Num(metrics.idleHeadDownDegrees) +
                ", ForwardArmMeanDifference=" + Num(
                    metrics.forwardArmMeanDifferenceDegreesMax) +
                ", ForwardArmSwingDifference=" + Num(
                    metrics.forwardArmRangeDifferenceDegreesMax) +
                ", ArmTorsoClearanceDegrees=" + Num(
                    metrics.armTorsoClearanceDegrees) +
                ", ForwardArmAdvanceDegrees=" + Num(
                    metrics.forwardArmAdvanceDegrees) +
                ", ForwardUpperBodyLateral=" + Num(
                    metrics.forwardUpperBodyMeanLateralOffset) +
                ", RightArmAdditionalClearanceDegrees=" + Num(
                    metrics.rightArmAdditionalClearanceDegrees) +
                ", ForwardLeftArmDownDegrees=" + Num(
                    metrics.forwardLeftArmDownDegrees) +
                ", EnterIdleRotationDifference=" + Num(
                    metrics.enterIdleRotationDifferenceDegreesMax) +
                ", RootDisplacement=" + Num(
                    metrics.rootDisplacementMax) +
                ", ForwardHipsHorizontal=" + Num(
                    metrics.forwardHipsHorizontalDisplacementMax) +
                ", LoopsPerTarget=2.");
        }

        private static ActualTargetData CaptureActualTarget(
            Scene scene,
            Transform target,
            AnimationClip clip,
            string stateName,
            int sourceMotionFrames,
            int[] phaseIndices,
            string contactPath,
            bool holdState = false)
        {
            VerifyAndLogRendererBoneOwnership(target);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    target.name + " Animator is missing.");
            int framesPerLoop = holdState
                ? Mathf.RoundToInt(
                    PlayerCrouchIdleForwardAnimationTool.IdleDurationSeconds *
                    clip.frameRate)
                : Mathf.RoundToInt(clip.length * clip.frameRate);
            int totalFrames = framesPerLoop * 2;
            Transform hips = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Hips");
            Transform spine = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Spine");
            Transform lowerSpine =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "Spine02");
            Transform head = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Head");
            Dictionary<string, Transform> armTransforms = ArmBones.ToDictionary(
                name => name,
                name => PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    name),
                StringComparer.Ordinal);
            Transform leftShoulder = armTransforms["LeftShoulder"];
            Transform rightShoulder = armTransforms["RightShoulder"];
            Transform leftUpperArm = armTransforms["LeftArm"];
            Transform leftForeArm = armTransforms["LeftForeArm"];
            Transform leftHand =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "LeftHand");
            Transform leftKnee =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "LeftLeg");
            AnimatorCullingMode oldCulling = animator.cullingMode;
            float oldSpeed = animator.speed;
            RendererState[] rendererStates = null;
            GameObject fullCameraObject = null;
            GameObject torsoCameraObject = null;
            GameObject sideTorsoCameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            Texture2D frameTexture = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                rendererStates = IsolateActualTarget(scene, target);
                fullCameraObject = CreateActualCamera(target.name + "FullCamera");
                torsoCameraObject = CreateActualCamera(target.name + "TorsoCamera");
                sideTorsoCameraObject = CreateActualCamera(
                    target.name + "SideTorsoCamera");
                Camera fullCamera = fullCameraObject.GetComponent<Camera>();
                Camera torsoCamera = torsoCameraObject.GetComponent<Camera>();
                Camera sideTorsoCamera =
                    sideTorsoCameraObject.GetComponent<Camera>();
                ConfigureActualCamera(
                    fullCamera,
                    target,
                    target.position + target.up * 1.05f,
                    1.35f);
                ConfigureActualCamera(
                    torsoCamera,
                    target,
                    target.position + target.up * 1.08f,
                    0.82f);
                ConfigureActualSideCamera(
                    sideTorsoCamera,
                    target,
                    target.position + target.up * 1.08f,
                    0.82f);
                lightObject = new GameObject(
                    target.name + "AlignmentLight",
                    typeof(Light));
                lightObject.hideFlags = HideFlags.HideAndDontSave;
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.transform.rotation = Quaternion.LookRotation(
                    -target.forward - target.up * 0.65f,
                    target.up);
                renderTexture = new RenderTexture(
                    CaptureWidth,
                    CaptureHeight,
                    24,
                    RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 2
                };
                renderTexture.Create();
                frameTexture = new Texture2D(
                    CaptureWidth,
                    CaptureHeight,
                    TextureFormat.RGB24,
                    false);

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int stateHash = Animator.StringToHash(stateName);
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash !=
                    stateHash)
                {
                    throw new InvalidOperationException(
                        target.name + " review state was not entered.");
                }

                ActualTargetData data = new ActualTargetData
                {
                    Name = target.name,
                    FramesPerLoop = framesPerLoop,
                    SourceMotionFrames = sourceMotionFrames,
                    Poses = new ActualPose[totalFrames],
                    WaistRotations = new Quaternion[framesPerLoop],
                    WaistDirections = new Vector3[framesPerLoop],
                    HeadForwardDirections = new Vector3[framesPerLoop],
                    UpperBodyLateralOffsets = new float[framesPerLoop],
                    LeftUpperArmDirections = new Vector3[framesPerLoop],
                    LeftForeArmDirections = new Vector3[framesPerLoop],
                    LeftHandPositions = new Vector3[framesPerLoop],
                    LeftKneePositions = new Vector3[framesPerLoop]
                };
                foreach (string boneName in ArmBones)
                {
                    data.ArmRotations.Add(
                        boneName,
                        new Quaternion[framesPerLoop]);
                    data.RenderedArmRotations.Add(
                        boneName,
                        new Dictionary<int, Quaternion>());
                }

                Vector3 rootBaseline = target.position;
                Vector3 hipsBaseline = hips.position;
                for (int frame = 0; frame < totalFrames; frame++)
                {
                    animator.Play(
                        stateHash,
                        0,
                        holdState ? 0f : frame / (float)framesPerLoop);
                    animator.Update(0f);
                    data.Poses[frame] = CaptureActualPose(target);
                    data.RootDisplacementMax = Mathf.Max(
                        data.RootDisplacementMax,
                        Vector3.Distance(target.position, rootBaseline));
                    data.HipsHorizontalDisplacementMax = Mathf.Max(
                        data.HipsHorizontalDisplacementMax,
                        HorizontalDistance(hips.position, hipsBaseline));
                    if (frame < framesPerLoop)
                    {
                        data.WaistRotations[frame] =
                            Quaternion.Inverse(target.rotation) * spine.rotation;
                        data.WaistDirections[frame] =
                            (Quaternion.Inverse(target.rotation) *
                             (spine.position - lowerSpine.position)).normalized;
                        data.HeadForwardDirections[frame] =
                            (Quaternion.Inverse(target.rotation) * head.forward)
                            .normalized;
                        Vector3 hipsLocal = target.InverseTransformPoint(
                            hips.position);
                        Vector3 shoulderCenterLocal = target.InverseTransformPoint(
                            (leftShoulder.position + rightShoulder.position) * 0.5f);
                        data.UpperBodyLateralOffsets[frame] =
                            shoulderCenterLocal.x - hipsLocal.x;
                        data.LeftUpperArmDirections[frame] =
                            (Quaternion.Inverse(target.rotation) *
                             (leftForeArm.position - leftUpperArm.position))
                            .normalized;
                        data.LeftForeArmDirections[frame] =
                            (Quaternion.Inverse(target.rotation) *
                             (leftHand.position - leftForeArm.position))
                            .normalized;
                        data.LeftHandPositions[frame] =
                            target.InverseTransformPoint(leftHand.position);
                        data.LeftKneePositions[frame] =
                            target.InverseTransformPoint(leftKnee.position);
                        foreach (string boneName in ArmBones)
                        {
                            data.ArmRotations[boneName][frame] =
                                Quaternion.Inverse(target.rotation) *
                                armTransforms[boneName].rotation;
                        }
                    }
                }

                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    data.LoopPositionDifferenceMax = Mathf.Max(
                        data.LoopPositionDifferenceMax,
                        PosePositionDifference(
                            data.Poses[frame],
                            data.Poses[frame + framesPerLoop]));
                    data.LoopRotationDifferenceMax = Mathf.Max(
                        data.LoopRotationDifferenceMax,
                        PoseRotationDifference(
                            data.Poses[frame],
                            data.Poses[frame + framesPerLoop]));
                }

                data.LeftElbowBendDegreesMax = Enumerable
                    .Range(0, framesPerLoop)
                    .Max(frame => Vector3.Angle(
                        data.LeftUpperArmDirections[frame],
                        data.LeftForeArmDirections[frame]));
                data.LeftArmDownwardMeanAngleDegrees = Vector3.Angle(
                    ActualDirectionMean(data.LeftUpperArmDirections),
                    Vector3.down);
                data.LeftArmDownwardMaximumAngleDegrees =
                    data.LeftUpperArmDirections.Max(
                        direction => Vector3.Angle(direction, Vector3.down));
                float kneeSide = Mathf.Sign(
                    data.LeftKneePositions.Average(position => position.x));
                if (Mathf.Approximately(kneeSide, 0f))
                {
                    kneeSide = -1f;
                }

                data.LeftHandKneeMinimumBoneGap = Enumerable
                    .Range(0, framesPerLoop)
                    .Min(frame => kneeSide *
                        (data.LeftHandPositions[frame].x -
                         data.LeftKneePositions[frame].x));

                Dictionary<int, byte[]> fullFrames =
                    new Dictionary<int, byte[]>();
                Dictionary<int, byte[]> torsoFrames =
                    new Dictionary<int, byte[]>();
                Dictionary<int, byte[]> sideTorsoFrames =
                    new Dictionary<int, byte[]>();
                foreach (int frame in phaseIndices)
                {
                    animator.Play(
                        stateHash,
                        0,
                        holdState ? 0f : frame / (float)framesPerLoop);
                    animator.Update(0f);
                    fullFrames.Add(
                        frame,
                        CaptureActualFrame(
                            fullCamera,
                            renderTexture,
                            frameTexture,
                            target));
                    torsoFrames.Add(
                        frame,
                        CaptureActualFrame(
                            torsoCamera,
                            renderTexture,
                            frameTexture,
                            target));
                    sideTorsoFrames.Add(
                        frame,
                        CaptureActualFrame(
                            sideTorsoCamera,
                            renderTexture,
                            frameTexture,
                            target));
                    data.RenderedPoses.Add(
                        frame,
                        CaptureActualPose(target));
                    data.RenderedWaistRotations.Add(
                        frame,
                        Quaternion.Inverse(target.rotation) * spine.rotation);
                    data.RenderedWaistDirections.Add(
                        frame,
                        (Quaternion.Inverse(target.rotation) *
                         (spine.position - lowerSpine.position)).normalized);
                    data.RenderedHeadForwardDirections.Add(
                        frame,
                        (Quaternion.Inverse(target.rotation) * head.forward)
                        .normalized);
                    foreach (string boneName in ArmBones)
                    {
                        data.RenderedArmRotations[boneName].Add(
                            frame,
                            Quaternion.Inverse(target.rotation) *
                            armTransforms[boneName].rotation);
                    }
                }

                ComposeActualContact(
                    fullFrames,
                    torsoFrames,
                    sideTorsoFrames,
                    phaseIndices,
                    contactPath);
                return data;
            }
            finally
            {
                animator.speed = oldSpeed;
                animator.cullingMode = oldCulling;
                if (rendererStates != null)
                {
                    foreach (RendererState state in rendererStates)
                    {
                        state.Restore();
                    }
                }

                RenderTexture.active = previousActive;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                if (frameTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(frameTexture);
                }

                if (fullCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(fullCameraObject);
                }

                if (torsoCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(torsoCameraObject);
                }

                if (sideTorsoCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(sideTorsoCameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }
            }
        }

        private static AnimationClip RequireAnimatorClip(
            Transform target,
            string expectedPath)
        {
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    target.name + " Animator is missing.");
            if (animator.applyRootMotion ||
                animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(
                    target.name + " Animator configuration differs.");
            }

            AnimationClip[] clips = animator.runtimeAnimatorController
                .animationClips
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(clips[0]),
                    expectedPath,
                    StringComparison.Ordinal) ||
                !clips[0].isLooping)
            {
                throw new InvalidOperationException(
                    target.name + " controller clip differs.");
            }

            return clips[0];
        }

        private static void VerifyAndLogRendererBoneOwnership(Transform target)
        {
            SkinnedMeshRenderer[] renderers = target
                .GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    target.name + " has no SkinnedMeshRenderer.");
            }

            List<string> reports = new List<string>();
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                bool rootOwned = renderer.rootBone != null &&
                                 renderer.rootBone.IsChildOf(target);
                int externalBones = renderer.bones.Count(bone =>
                    bone == null || !bone.IsChildOf(target));
                reports.Add(
                    renderer.name +
                    ":RootOwned=" + rootOwned +
                    ",ExternalBones=" + externalBones.ToString(
                        CultureInfo.InvariantCulture) +
                    ",Bones=" + renderer.bones.Length.ToString(
                        CultureInfo.InvariantCulture) +
                    ",Mesh=" + AssetDatabase.GetAssetPath(renderer.sharedMesh) +
                    ",Vertices=" + (renderer.sharedMesh != null
                        ? renderer.sharedMesh.vertexCount.ToString(
                            CultureInfo.InvariantCulture)
                        : "0") +
                    ",BindPoses=" + (renderer.sharedMesh != null
                        ? renderer.sharedMesh.bindposes.Length.ToString(
                            CultureInfo.InvariantCulture)
                        : "0"));
                if (!rootOwned || externalBones != 0)
                {
                    throw new InvalidOperationException(
                        target.name +
                        " SkinnedMeshRenderer references bones outside its target. " +
                        reports[reports.Count - 1] + ".");
                }
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Renderer bone ownership " +
                target.name +
                ": RootWorldRotation=" + target.rotation.eulerAngles.ToString("F4") +
                ", RootLossyScale=" + target.lossyScale.ToString("F4") +
                ", Prefab=" + AssetDatabase.GetAssetPath(
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        target.gameObject)) +
                "; " + string.Join("; ", reports) + ".");
        }

        private static ActualPose CaptureActualPose(Transform root)
        {
            ActualPose pose = new ActualPose();
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(transform, root);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                pose.Positions.Add(path, transform.localPosition);
                pose.Rotations.Add(path, transform.localRotation);
            }

            return pose;
        }

        private static float PosePositionDifference(ActualPose first, ActualPose second)
        {
            if (first.Positions.Count != second.Positions.Count)
            {
                return float.PositiveInfinity;
            }

            return first.Positions.Max(pair =>
                second.Positions.TryGetValue(pair.Key, out Vector3 value)
                    ? Vector3.Distance(pair.Value, value)
                    : float.PositiveInfinity);
        }

        private static float PoseRotationDifference(ActualPose first, ActualPose second)
        {
            if (first.Rotations.Count != second.Rotations.Count)
            {
                return float.PositiveInfinity;
            }

            return first.Rotations.Max(pair =>
                second.Rotations.TryGetValue(pair.Key, out Quaternion value)
                    ? Quaternion.Angle(pair.Value, value)
                    : float.PositiveInfinity);
        }

        private static void MaxPoseDifference(
            ActualPose baseline,
            IEnumerable<ActualPose> poses,
            out float position,
            out float rotation)
        {
            position = 0f;
            rotation = 0f;
            foreach (ActualPose pose in poses)
            {
                position = Mathf.Max(
                    position,
                    PosePositionDifference(baseline, pose));
                rotation = Mathf.Max(
                    rotation,
                    PoseRotationDifference(baseline, pose));
            }
        }

        private static Quaternion ActualQuaternionMean(IEnumerable<Quaternion> values)
        {
            Quaternion[] rotations = values.ToArray();
            Quaternion reference = rotations[0];
            Vector4 sum = Vector4.zero;
            foreach (Quaternion rotation in rotations)
            {
                Quaternion value = Quaternion.Dot(reference, rotation) < 0f
                    ? new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w)
                    : rotation;
                sum += new Vector4(value.x, value.y, value.z, value.w);
            }

            sum /= sum.magnitude;
            return new Quaternion(sum.x, sum.y, sum.z, sum.w);
        }

        private static Vector3 ActualDirectionMean(IEnumerable<Vector3> values)
        {
            Vector3 sum = values.Aggregate(
                Vector3.zero,
                (current, value) => current + value.normalized);
            if (sum.sqrMagnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Crouch waist direction mean is degenerate.");
            }

            return sum.normalized;
        }

        private static float GroundAngleDegrees(Vector3 direction)
        {
            Vector3 normalized = direction.normalized;
            float horizontal = Vector3.ProjectOnPlane(
                normalized,
                Vector3.up).magnitude;
            return Mathf.Atan2(normalized.y, horizontal) * Mathf.Rad2Deg;
        }

        private static Vector3 TorsoForwardDirection(Vector3 waistDirection)
        {
            Vector3 torsoForward = Vector3.ProjectOnPlane(
                Vector3.forward,
                waistDirection.normalized);
            if (torsoForward.sqrMagnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Crouch torso forward direction is degenerate.");
            }

            return torsoForward.normalized;
        }

        private static Vector3 DesiredHeadForwardDirection(
            Vector3 waistDirection,
            float downDegrees)
        {
            Vector3 torsoUp = waistDirection.normalized;
            Vector3 torsoForward = TorsoForwardDirection(torsoUp);
            Vector3 torsoRight = Vector3.Cross(torsoUp, torsoForward).normalized;
            return (Quaternion.AngleAxis(downDegrees, torsoRight) * torsoForward)
                .normalized;
        }

        private static RendererState[] IsolateActualTarget(
            Scene scene,
            Transform target)
        {
            HashSet<int> targetRenderers = target
                .GetComponentsInChildren<Renderer>(true)
                .Select(renderer => renderer.GetInstanceID())
                .ToHashSet();
            Renderer[] renderers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            RendererState[] states = renderers
                .Select(renderer => new RendererState(renderer))
                .ToArray();
            foreach (Renderer renderer in renderers)
            {
                bool isTarget = targetRenderers.Contains(renderer.GetInstanceID());
                renderer.enabled = isTarget;
                if (isTarget && renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    skinnedRenderer.updateWhenOffscreen = true;
                }
            }

            return states;
        }

        private static GameObject CreateActualCamera(string name)
        {
            GameObject cameraObject = new GameObject(name, typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = true;
            camera.aspect = CaptureWidth / (float)CaptureHeight;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            return cameraObject;
        }

        private static void ConfigureActualCamera(
            Camera camera,
            Transform target,
            Vector3 center,
            float size)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                target.forward,
                Vector3.up).normalized;
            camera.transform.position = center + forward * 7f;
            camera.transform.rotation = Quaternion.LookRotation(
                -forward,
                target.up);
            camera.orthographicSize = size;
        }

        private static void ConfigureActualSideCamera(
            Camera camera,
            Transform target,
            Vector3 center,
            float size)
        {
            Vector3 right = Vector3.ProjectOnPlane(
                target.right,
                Vector3.up).normalized;
            camera.transform.position = center + right * 7f;
            camera.transform.rotation = Quaternion.LookRotation(
                -right,
                target.up);
            camera.orthographicSize = size;
        }

        private static byte[] CaptureActualFrame(
            Camera camera,
            RenderTexture renderTexture,
            Texture2D frameTexture,
            Transform target)
        {
            List<SkinnedMeshRenderer> sourceRenderers = target
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.enabled)
                .ToList();
            List<GameObject> bakedObjects = new List<GameObject>();
            List<Mesh> bakedMeshes = new List<Mesh>();
            try
            {
                foreach (SkinnedMeshRenderer source in sourceRenderers)
                {
                    Mesh bakedMesh = new Mesh
                    {
                        name = source.name + "_CrouchReviewBaked"
                    };
                    source.BakeMesh(bakedMesh, false);
                    GameObject bakedObject = new GameObject(
                        bakedMesh.name,
                        typeof(MeshFilter),
                        typeof(MeshRenderer));
                    bakedObject.hideFlags = HideFlags.HideAndDontSave;
                    bakedObject.layer = source.gameObject.layer;
                    bakedObject.transform.SetPositionAndRotation(
                        source.transform.position,
                        source.transform.rotation);
                    bakedObject.transform.localScale = source.transform.lossyScale;
                    bakedObject.GetComponent<MeshFilter>().sharedMesh = bakedMesh;
                    bakedObject.GetComponent<MeshRenderer>().sharedMaterials =
                        source.sharedMaterials;
                    bakedObjects.Add(bakedObject);
                    bakedMeshes.Add(bakedMesh);
                    source.enabled = false;
                }

                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                frameTexture.ReadPixels(
                    new Rect(0, 0, CaptureWidth, CaptureHeight),
                    0,
                    0,
                    false);
                frameTexture.Apply(false, false);
                camera.targetTexture = null;
                return frameTexture.EncodeToPNG();
            }
            finally
            {
                foreach (SkinnedMeshRenderer source in sourceRenderers)
                {
                    if (source != null)
                    {
                        source.enabled = true;
                    }
                }

                foreach (GameObject bakedObject in bakedObjects)
                {
                    UnityEngine.Object.DestroyImmediate(bakedObject);
                }

                foreach (Mesh bakedMesh in bakedMeshes)
                {
                    UnityEngine.Object.DestroyImmediate(bakedMesh);
                }
            }
        }

        private static void ComposeActualContact(
            Dictionary<int, byte[]> fullFrames,
            Dictionary<int, byte[]> torsoFrames,
            Dictionary<int, byte[]> sideTorsoFrames,
            int[] indices,
            string path)
        {
            Texture2D contact = new Texture2D(
                CaptureWidth * indices.Length,
                CaptureHeight * 3,
                TextureFormat.RGB24,
                false);
            Color background = new Color(0.055f, 0.065f, 0.08f, 1f);
            contact.SetPixels(
                Enumerable.Repeat(
                    background,
                    contact.width * contact.height).ToArray());
            List<Texture2D> decoded = new List<Texture2D>();
            try
            {
                for (int column = 0; column < indices.Length; column++)
                {
                    int frame = indices[column];
                    Texture2D full = DecodeActualFrame(fullFrames[frame]);
                    Texture2D torso = DecodeActualFrame(torsoFrames[frame]);
                    Texture2D sideTorso = DecodeActualFrame(
                        sideTorsoFrames[frame]);
                    decoded.Add(full);
                    decoded.Add(torso);
                    decoded.Add(sideTorso);
                    contact.SetPixels(
                        column * CaptureWidth,
                        CaptureHeight * 2,
                        CaptureWidth,
                        CaptureHeight,
                        full.GetPixels());
                    contact.SetPixels(
                        column * CaptureWidth,
                        CaptureHeight,
                        CaptureWidth,
                        CaptureHeight,
                        torso.GetPixels());
                    contact.SetPixels(
                        column * CaptureWidth,
                        0,
                        CaptureWidth,
                        CaptureHeight,
                        sideTorso.GetPixels());
                }

                contact.Apply(false, false);
                string absolutePath = Path.GetFullPath(path);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absolutePath) ??
                    throw new InvalidOperationException(
                        "Crouch alignment contact directory is unavailable."));
                File.WriteAllBytes(absolutePath, contact.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D texture in decoded)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                UnityEngine.Object.DestroyImmediate(contact);
            }
        }

        private static Texture2D DecodeActualFrame(byte[] png)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!texture.LoadImage(png, false) ||
                texture.width != CaptureWidth ||
                texture.height != CaptureHeight)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(
                    "Crouch alignment frame could not be decoded.");
            }

            return texture;
        }

        private static int[] PhaseIndices(int framesPerLoop)
        {
            return new[]
            {
                0,
                framesPerLoop / 4,
                framesPerLoop / 2,
                framesPerLoop * 3 / 4,
                framesPerLoop - 1
            };
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            Vector3 difference = first - second;
            difference.y = 0f;
            return difference.magnitude;
        }

        private static Scene RequireActualScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(
                    scene.path,
                    PlayerCrouchEnterAnimationTool.ScenePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for crouch pose alignment review.");
            }

            return scene;
        }

        private static void CaptureLegacyReview()
        {
            int stage = SessionState.GetInt(StageKey, 0);
            try
            {
                switch (stage)
                {
                    case 0:
                        RequireMode(playing: false, stage);
                        PlayerCrouchEnterPlayModeReview.CaptureHoldReview();
                        Advance(stage, "enter Play Mode requested");
                        return;
                    case 1:
                        RequireMode(playing: true, stage);
                        PlayerCrouchEnterPlayModeReview.CaptureHoldReview();
                        Advance(stage, "enter two-loop frames captured");
                        return;
                    case 2:
                        RequireMode(playing: true, stage);
                        PlayerCrouchEnterPlayModeReview.CaptureHoldReview();
                        Advance(stage, "enter Play Mode exit requested");
                        return;
                    case 3:
                        RequireMode(playing: false, stage);
                        PlayerCrouchIdleForwardPlayModeReview.CaptureIdleReview();
                        Advance(stage, "idle Play Mode requested");
                        return;
                    case 4:
                        RequireMode(playing: true, stage);
                        PlayerCrouchIdleForwardPlayModeReview.CaptureIdleReview();
                        Advance(stage, "idle two-loop frames captured");
                        return;
                    case 5:
                        RequireMode(playing: true, stage);
                        PlayerCrouchIdleForwardPlayModeReview.CaptureIdleReview();
                        Advance(stage, "idle Play Mode exit requested");
                        return;
                    case 6:
                        RequireMode(playing: false, stage);
                        PlayerCrouchIdleForwardPlayModeReview.CaptureForwardReview();
                        Advance(stage, "forward Play Mode requested");
                        return;
                    case 7:
                        RequireMode(playing: true, stage);
                        PlayerCrouchIdleForwardPlayModeReview.CaptureForwardReview();
                        Advance(stage, "forward two-loop frames captured");
                        return;
                    case 8:
                        RequireMode(playing: true, stage);
                        PlayerCrouchIdleForwardPlayModeReview.CaptureForwardReview();
                        Advance(stage, "forward Play Mode exit requested");
                        return;
                    case 9:
                        RequireMode(playing: false, stage);
                        ValidateReviewedOutputs();
                        SessionState.EraseInt(StageKey);
                        Debug.Log(
                            "[PlayerCrouchPoseAlignment] Three-target actual Play Mode review completed.");
                        return;
                    default:
                        throw new InvalidOperationException(
                            "Crouch pose alignment review stage is invalid: " +
                            stage.ToString(CultureInfo.InvariantCulture) + ".");
                }
            }
            catch
            {
                SessionState.EraseInt(StageKey);
                throw;
            }
        }

        private static void CaptureLegacyFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment final requires Edit Mode.");
            }

            AlignmentReviewMetrics metrics = ReadJson<AlignmentReviewMetrics>(
                ReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                metrics.enterLoopsReviewed != 2 ||
                metrics.idleLoopsReviewed != 2 ||
                metrics.forwardLoopsReviewed != 2)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment review did not pass before final capture.");
            }

            CopyReviewedContact(EnterContactPath, EnterFinalPath);
            CopyReviewedContact(IdleContactPath, IdleFinalPath);
            CopyReviewedContact(ForwardContactPath, ForwardFinalPath);
            Debug.Log(
                "[PlayerCrouchPoseAlignment] Final copied once from all directly reviewed contact sheets." +
                " Enter=" + Path.GetFullPath(EnterFinalPath) +
                ", Idle=" + Path.GetFullPath(IdleFinalPath) +
                ", Forward=" + Path.GetFullPath(ForwardFinalPath) +
                ", LoopsPerTarget=2, SceneChanged=False.");
        }

        private static void Advance(int stage, string marker)
        {
            SessionState.SetInt(StageKey, stage + 1);
            Debug.Log(
                "[PlayerCrouchPoseAlignment] Review stage " +
                stage.ToString(CultureInfo.InvariantCulture) +
                " completed: " + marker + ".");
        }

        private static void RequireMode(bool playing, int stage)
        {
            if (EditorApplication.isPlaying != playing)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment review mode differs at stage " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        private static void ValidateReviewedOutputs()
        {
            ExistingReviewMetrics enter = ReadJson<ExistingReviewMetrics>(
                EnterMetricsPath);
            ExistingReviewMetrics idle = ReadJson<ExistingReviewMetrics>(
                IdleMetricsPath);
            ExistingReviewMetrics forward = ReadJson<ExistingReviewMetrics>(
                ForwardMetricsPath);
            PlayerCrouchPoseAlignmentTool.AlignmentApplyMetrics apply =
                ReadJson<PlayerCrouchPoseAlignmentTool.AlignmentApplyMetrics>(
                    PlayerCrouchPoseAlignmentTool.ApplyMetricsPath);
            PlayerCrouchPoseAlignmentTool.AlignmentApplyMetrics current =
                PlayerCrouchPoseAlignmentTool.MeasureCurrentAlignment();

            AlignmentReviewMetrics metrics = new AlignmentReviewMetrics
            {
                targetSet =
                    "Player_Crouch_Enter, Player_Crouch_Idle, Player_Crouch_Forward",
                waistReference = current.waistReference,
                armReference = current.armReference,
                enterWaistMeanDifferenceDegreesMax =
                    current.enterWaistMeanDifferenceDegreesMax,
                idleWaistMeanDifferenceDegreesMax =
                    current.idleWaistMeanDifferenceDegreesMax,
                enterIdleWaistDifferenceDegreesMax =
                    current.enterIdleWaistDifferenceDegreesMax,
                forwardArmMeanDifferenceDegreesMax =
                    current.forwardArmMeanDifferenceDegreesMax,
                forwardArmRangeDifferenceDegreesMax =
                    apply.forwardArmRangeDifferenceDegreesMax,
                enterLoopsReviewed = enter.loopsSampled,
                idleLoopsReviewed = idle.loopsSampled,
                forwardLoopsReviewed = forward.loopsSampled,
                enterReviewPassed = enter.passedNumericChecks,
                idleReviewPassed = idle.passedNumericChecks,
                forwardReviewPassed = forward.passedNumericChecks,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            metrics.passedNumericChecks =
                apply.passedNumericChecks &&
                metrics.enterReviewPassed &&
                metrics.idleReviewPassed &&
                metrics.forwardReviewPassed &&
                metrics.enterLoopsReviewed == 2 &&
                metrics.idleLoopsReviewed == 2 &&
                metrics.forwardLoopsReviewed == 2 &&
                metrics.enterWaistMeanDifferenceDegreesMax <= RotationTolerance &&
                metrics.idleWaistMeanDifferenceDegreesMax <= RotationTolerance &&
                metrics.enterIdleWaistDifferenceDegreesMax <= RotationTolerance &&
                metrics.forwardArmMeanDifferenceDegreesMax <= RotationTolerance &&
                metrics.forwardArmRangeDifferenceDegreesMax <= RotationTolerance;
            WriteJson(ReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment reviewed support checks failed." +
                    " EnterWaist=" + Num(
                        metrics.enterWaistMeanDifferenceDegreesMax) +
                    ", IdleWaist=" + Num(
                        metrics.idleWaistMeanDifferenceDegreesMax) +
                    ", Arms=" + Num(
                        metrics.forwardArmMeanDifferenceDegreesMax) +
                    ", ArmRange=" + Num(
                        metrics.forwardArmRangeDifferenceDegreesMax) + ".");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Reviewed support checks passed." +
                " EnterWaistDifference=" + Num(
                    metrics.enterWaistMeanDifferenceDegreesMax) +
                ", IdleWaistDifference=" + Num(
                    metrics.idleWaistMeanDifferenceDegreesMax) +
                ", ForwardArmMeanDifference=" + Num(
                    metrics.forwardArmMeanDifferenceDegreesMax) +
                ", ForwardArmRangeDifference=" + Num(
                    metrics.forwardArmRangeDifferenceDegreesMax) +
                ", LoopsPerTarget=2.");
        }

        private static T ReadJson<T>(string path)
        {
            string absolutePath = Path.GetFullPath(path);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Crouch pose alignment review input is missing.",
                    absolutePath);
            }

            T value = JsonUtility.FromJson<T>(File.ReadAllText(absolutePath));
            if (value == null)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment review input could not be parsed: " +
                    path + ".");
            }

            return value;
        }

        private static void WriteJson<T>(string path, T value)
        {
            string absolutePath = Path.GetFullPath(path);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "Crouch pose alignment review directory is unavailable."));
            File.WriteAllText(absolutePath, JsonUtility.ToJson(value, true));
        }

        private static void CopyReviewedContact(string source, string destination)
        {
            string sourcePath = Path.GetFullPath(source);
            string destinationPath = Path.GetFullPath(destination);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    "Reviewed crouch contact sheet is missing.",
                    sourcePath);
            }

            Directory.CreateDirectory(
                Path.GetDirectoryName(destinationPath) ??
                throw new InvalidOperationException(
                    "Crouch pose alignment final directory is unavailable."));
            File.Copy(sourcePath, destinationPath, true);
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
