using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerCrouchBackwardSidestepPlayModeReview
    {
        private const string StageKey =
            "Bellerophon.PlayerCrouchMovingKneeSideArms.Review.Stage";
        private const string MetricsPath =
            "docs/validation/player_crouch_moving_knee_side_arm_review_metrics.json";
        private const string LeftArmStraightDownStageKey =
            "Bellerophon.PlayerCrouchBackwardSidestep.LeftArmStraightDown.Review.Stage";
        private const string LeftArmStraightDownMetricsPath =
            "docs/validation/player_crouch_backward_sidestep_left_arm_straight_down_review_metrics.json";
        private const string BackwardLeftArmStraightDownContactPath =
            "docs/validation/player_crouch_backward_left_arm_straight_down_review_contact_sheet.png";
        private const string SidestepLeftArmStraightDownContactPath =
            "docs/validation/player_crouch_sidestep_left_arm_straight_down_review_contact_sheet.png";
        private const string BackwardLeftArmStraightDownFinalPath =
            "docs/validation/player_crouch_backward_left_arm_straight_down_final.png";
        private const string SidestepLeftArmStraightDownFinalPath =
            "docs/validation/player_crouch_sidestep_left_arm_straight_down_final.png";
        private const string ForwardContactPath =
            "docs/validation/player_crouch_forward_knee_side_arm_review_contact_sheet.png";
        private const string BackwardContactPath =
            "docs/validation/player_crouch_backward_knee_side_arm_review_contact_sheet.png";
        private const string SidestepContactPath =
            "docs/validation/player_crouch_sidestep_knee_side_arm_review_contact_sheet.png";
        private const string IdleArmReferencePath =
            "docs/validation/player_crouch_backward_sidestep_idle_arm_reference.png";
        private const string ForwardFinalPath =
            "docs/validation/player_crouch_forward_knee_side_arm_final.png";
        private const string BackwardFinalPath =
            "docs/validation/player_crouch_backward_knee_side_arm_final.png";
        private const string SidestepFinalPath =
            "docs/validation/player_crouch_sidestep_knee_side_arm_final.png";
        private const int CaptureWidth = 400;
        private const int CaptureHeight = 500;
        private const float PositionTolerance = 0.0001f;
        private const float RotationTolerance = 0.01f;

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string targetSet;
            public TargetReviewMetrics forward;
            public TargetReviewMetrics backward;
            public TargetReviewMetrics sidestep;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class TargetReviewMetrics
        {
            public string target;
            public string state;
            public string clipName;
            public string clipAssetPath;
            public string sourceTake;
            public float clipDurationSeconds;
            public float clipFrameRate;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootHorizontalDisplacementMax;
            public float hipsHorizontalDisplacementMax;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public float motionPositionDifferenceMax;
            public float motionRotationDifferenceDegreesMax;
            public float armClearanceDegrees;
            public float leftArmAdjustmentDegrees;
            public float rightArmAdjustmentDegrees;
            public float handKneeMinimumBoneGapTarget;
            public float handKneeMinimumBoneGapAfter;
            public float armMeanDifferenceDegreesMax;
            public float armSwingDifferenceDegreesMax;
            public float leftElbowBendDegreesMax;
            public float leftElbowBendDegreesMaxApply;
            public float leftArmDownwardMeanAngleDegrees;
            public float leftArmDownwardMaximumAngleDegrees;
            public float leftArmDownwardMeanAngleDegreesApply;
            public float leftArmDownwardMaximumAngleDegreesApply;
            public float leftShoulderSwingDifferenceDegreesMax;
            public float leftUpperArmSwingDifferenceDegreesMax;
            public float leftFootHorizontalRange;
            public float rightFootHorizontalRange;
            public bool clipIsLooping;
            public bool applyRootMotion;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class ArmAlignmentApplyRead
        {
            public ArmAlignmentTargetRead forward;
            public ArmAlignmentTargetRead backward;
            public ArmAlignmentTargetRead sidestep;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class ArmAlignmentTargetRead
        {
            public float armMeanDifferenceDegreesMax;
            public float armSwingDifferenceDegreesMax;
            public float leftArmAdjustmentDegrees;
            public float rightArmAdjustmentDegrees;
            public float handKneeMinimumBoneGapTarget;
            public float handKneeMinimumBoneGapAfter;
        }

        [Serializable]
        private sealed class LeftArmStraightDownApplyRead
        {
            public LeftArmStraightDownTargetRead backward;
            public LeftArmStraightDownTargetRead sidestep;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class LeftArmStraightDownTargetRead
        {
            public float leftElbowBendDegreesMaxAfter;
            public float leftArmDownwardMeanAngleDegreesAfter;
            public float leftArmDownwardMaximumAngleDegreesAfter;
            public float leftHandKneeMinimumBoneGapTarget;
            public float leftHandKneeMinimumBoneGapAfter;
            public float leftShoulderSwingDifferenceDegreesMax;
            public float leftUpperArmSwingDifferenceDegreesMax;
        }

        private sealed class Pose
        {
            internal readonly Dictionary<string, Vector3> Positions =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Quaternion> Rotations =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            internal RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            internal void Hide()
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            internal void Restore()
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Moving Knee Side Arm Review")]
        internal static void CaptureReview()
        {
            int stage = SessionState.GetInt(StageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Crouch moving knee-side arm review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before crouch moving knee-side arm review.");
                    }

                    SessionState.SetInt(StageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerCrouchMovingKneeSideArms] Entering Play Mode for three-target review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch moving knee-side arm capture requires Play Mode.");
                    }

                    CaptureMovingTargets();
                    SessionState.SetInt(StageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch moving knee-side arm review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(StageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerCrouchMovingKneeSideArms] Exiting Play Mode after three-target review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Crouch moving knee-side arm review stage is invalid: " +
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

        [MenuItem("Bellerophon/Player/Capture Crouch Moving Knee Side Arm Final")]
        internal static void CaptureFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Crouch moving knee-side arm final requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before final capture.");
            }

            ReviewMetrics metrics = ReadMetrics();
            if (!metrics.passedNumericChecks ||
                !TargetPassed(metrics.forward) ||
                !TargetPassed(metrics.backward) ||
                !TargetPassed(metrics.sidestep) ||
                metrics.forward.loopsSampled != 2 ||
                metrics.backward.loopsSampled != 2 ||
                metrics.sidestep.loopsSampled != 2)
            {
                throw new InvalidOperationException(
                    "Crouch moving knee-side arm review did not pass before final capture.");
            }

            CopyReviewedContact(ForwardContactPath, ForwardFinalPath);
            CopyReviewedContact(BackwardContactPath, BackwardFinalPath);
            CopyReviewedContact(SidestepContactPath, SidestepFinalPath);
            Debug.Log(
                "[PlayerCrouchMovingKneeSideArms] Final copied once from directly reviewed two-loop frames." +
                " Forward=" + Path.GetFullPath(ForwardFinalPath) +
                ", Backward=" + Path.GetFullPath(BackwardFinalPath) +
                ", Sidestep=" + Path.GetFullPath(SidestepFinalPath) +
                ", LoopsPerTarget=2, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Backward And Sidestep Left Arms Straight Down Review")]
        internal static void CaptureLeftArmsStraightDownReview()
        {
            int stage = SessionState.GetInt(LeftArmStraightDownStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Left-arm straight-down review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before left-arm straight-down review.");
                    }

                    SessionState.SetInt(LeftArmStraightDownStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerCrouchLeftArmsStraightDown] Entering Play Mode for two-target review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Left-arm straight-down capture requires Play Mode.");
                    }

                    CaptureLeftArmStraightDownTargets();
                    SessionState.SetInt(LeftArmStraightDownStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Left-arm straight-down review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(LeftArmStraightDownStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerCrouchLeftArmsStraightDown] Exiting Play Mode after two-target review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Left-arm straight-down review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(LeftArmStraightDownStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Backward And Sidestep Left Arms Straight Down Final")]
        internal static void CaptureLeftArmsStraightDownFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Left-arm straight-down final requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before final capture.");
            }

            ReviewMetrics metrics = ReadMetrics(LeftArmStraightDownMetricsPath);
            if (!metrics.passedNumericChecks ||
                !LeftArmStraightDownTargetPassed(metrics.backward) ||
                !LeftArmStraightDownTargetPassed(metrics.sidestep) ||
                metrics.backward.loopsSampled != 2 ||
                metrics.sidestep.loopsSampled != 2)
            {
                throw new InvalidOperationException(
                    "Left-arm straight-down review did not pass before final capture.");
            }

            CopyReviewedContact(
                BackwardLeftArmStraightDownContactPath,
                BackwardLeftArmStraightDownFinalPath);
            CopyReviewedContact(
                SidestepLeftArmStraightDownContactPath,
                SidestepLeftArmStraightDownFinalPath);
            Debug.Log(
                "[PlayerCrouchLeftArmsStraightDown] Final copied once from directly reviewed two-loop frames." +
                " Backward=" +
                Path.GetFullPath(BackwardLeftArmStraightDownFinalPath) +
                ", Sidestep=" +
                Path.GetFullPath(SidestepLeftArmStraightDownFinalPath) +
                ", LoopsPerTarget=2, SceneChanged=False.");
        }

        private static void CaptureLeftArmStraightDownTargets()
        {
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            TargetReviewMetrics backward = CaptureTarget(
                scene,
                PlayerCrouchBackwardSidestepAnimationTool.BackwardTargetName,
                PlayerCrouchBackwardSidestepAnimationTool.BackwardStateName,
                PlayerCrouchBackwardSidestepAnimationTool.BackwardClipPath,
                BackwardLeftArmStraightDownContactPath);
            TargetReviewMetrics sidestep = CaptureTarget(
                scene,
                PlayerCrouchBackwardSidestepAnimationTool.SidestepTargetName,
                PlayerCrouchBackwardSidestepAnimationTool.SidestepStateName,
                PlayerCrouchBackwardSidestepAnimationTool.SidestepClipPath,
                SidestepLeftArmStraightDownContactPath);
            LeftArmStraightDownApplyRead apply =
                ReadLeftArmStraightDownApplyMetrics();
            AssignLeftArmStraightDownApplyMetrics(backward, apply.backward);
            AssignLeftArmStraightDownApplyMetrics(sidestep, apply.sidestep);
            backward.passedNumericChecks =
                LeftArmStraightDownTargetPassed(backward);
            sidestep.passedNumericChecks =
                LeftArmStraightDownTargetPassed(sidestep);
            ReviewMetrics metrics = new ReviewMetrics
            {
                targetSet =
                    PlayerCrouchBackwardSidestepAnimationTool.BackwardTargetName +
                    ", " +
                    PlayerCrouchBackwardSidestepAnimationTool.SidestepTargetName,
                backward = backward,
                sidestep = sidestep,
                passedNumericChecks =
                    apply.passedNumericChecks &&
                    LeftArmStraightDownTargetPassed(backward) &&
                    LeftArmStraightDownTargetPassed(sidestep),
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteMetrics(metrics, LeftArmStraightDownMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Left-arm straight-down Play Mode support checks failed." +
                    " BackwardBend=" +
                    Num(backward.leftElbowBendDegreesMax) +
                    ", BackwardDownMean=" +
                    Num(backward.leftArmDownwardMeanAngleDegrees) +
                    ", BackwardDownMax=" +
                    Num(backward.leftArmDownwardMaximumAngleDegrees) +
                    ", BackwardGap=" +
                    Num(backward.handKneeMinimumBoneGapAfter) +
                    ", SidestepBend=" +
                    Num(sidestep.leftElbowBendDegreesMax) +
                    ", SidestepDownMean=" +
                    Num(sidestep.leftArmDownwardMeanAngleDegrees) +
                    ", SidestepDownMax=" +
                    Num(sidestep.leftArmDownwardMaximumAngleDegrees) +
                    ", SidestepGap=" +
                    Num(sidestep.handKneeMinimumBoneGapAfter) + ".");
            }

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "Left-arm straight-down review changed the scene dirty state.");
            }

            Debug.Log(
                "[PlayerCrouchLeftArmsStraightDown] Captured actual Play Mode two-loop review." +
                " BackwardFrames=" +
                backward.framesSampled.ToString(CultureInfo.InvariantCulture) +
                ", BackwardBend=" +
                Num(backward.leftElbowBendDegreesMax) +
                ", BackwardDownMean=" +
                Num(backward.leftArmDownwardMeanAngleDegrees) +
                ", BackwardDownMax=" +
                Num(backward.leftArmDownwardMaximumAngleDegrees) +
                ", BackwardGap=" +
                Num(backward.handKneeMinimumBoneGapAfter) +
                ", SidestepFrames=" +
                sidestep.framesSampled.ToString(CultureInfo.InvariantCulture) +
                ", SidestepBend=" +
                Num(sidestep.leftElbowBendDegreesMax) +
                ", SidestepDownMean=" +
                Num(sidestep.leftArmDownwardMeanAngleDegrees) +
                ", SidestepDownMax=" +
                Num(sidestep.leftArmDownwardMaximumAngleDegrees) +
                ", SidestepGap=" +
                Num(sidestep.handKneeMinimumBoneGapAfter) +
                ", ApplyRootMotion=False, LoopsPerTarget=2.");
        }

        private static void CaptureMovingTargets()
        {
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            TargetReviewMetrics forward = CaptureTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName,
                PlayerCrouchIdleForwardAnimationTool.ForwardStateName,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath,
                ForwardContactPath);
            TargetReviewMetrics backward = CaptureTarget(
                scene,
                PlayerCrouchBackwardSidestepAnimationTool.BackwardTargetName,
                PlayerCrouchBackwardSidestepAnimationTool.BackwardStateName,
                PlayerCrouchBackwardSidestepAnimationTool.BackwardClipPath,
                BackwardContactPath);
            TargetReviewMetrics sidestep = CaptureTarget(
                scene,
                PlayerCrouchBackwardSidestepAnimationTool.SidestepTargetName,
                PlayerCrouchBackwardSidestepAnimationTool.SidestepStateName,
                PlayerCrouchBackwardSidestepAnimationTool.SidestepClipPath,
                SidestepContactPath);
            ArmAlignmentApplyRead armApply = ReadMovingKneeSideArmApplyMetrics();
            AssignApplyMetrics(forward, armApply.forward);
            AssignApplyMetrics(backward, armApply.backward);
            AssignApplyMetrics(sidestep, armApply.sidestep);
            forward.passedNumericChecks = TargetPassed(forward);
            backward.passedNumericChecks = TargetPassed(backward);
            sidestep.passedNumericChecks = TargetPassed(sidestep);
            ReviewMetrics metrics = new ReviewMetrics
            {
                targetSet =
                    PlayerCrouchIdleForwardAnimationTool.ForwardTargetName +
                    ", " +
                    PlayerCrouchBackwardSidestepAnimationTool.BackwardTargetName +
                    ", " +
                    PlayerCrouchBackwardSidestepAnimationTool.SidestepTargetName,
                forward = forward,
                backward = backward,
                sidestep = sidestep,
                passedNumericChecks =
                    armApply.passedNumericChecks &&
                    TargetPassed(forward) &&
                    TargetPassed(backward) &&
                    TargetPassed(sidestep),
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch moving knee-side arm Play Mode support checks failed." +
                    " ForwardRoot=" +
                    Num(forward.rootHorizontalDisplacementMax) +
                    ", ForwardHips=" +
                    Num(forward.hipsHorizontalDisplacementMax) +
                    ", ForwardGap=" +
                    Num(forward.handKneeMinimumBoneGapAfter) +
                    ", ForwardArmSwing=" +
                    Num(forward.armSwingDifferenceDegreesMax) +
                    " BackwardRoot=" +
                    Num(backward.rootHorizontalDisplacementMax) +
                    ", BackwardHips=" +
                    Num(backward.hipsHorizontalDisplacementMax) +
                    ", BackwardLoopRotation=" +
                    Num(backward.loopRotationDifferenceDegreesMax) +
                    ", BackwardArmMean=" +
                    Num(backward.armMeanDifferenceDegreesMax) +
                    ", BackwardArmSwing=" +
                    Num(backward.armSwingDifferenceDegreesMax) +
                    ", BackwardGap=" +
                    Num(backward.handKneeMinimumBoneGapAfter) +
                    ", SidestepRoot=" +
                    Num(sidestep.rootHorizontalDisplacementMax) +
                    ", SidestepHips=" +
                    Num(sidestep.hipsHorizontalDisplacementMax) +
                    ", SidestepLoopRotation=" +
                    Num(sidestep.loopRotationDifferenceDegreesMax) +
                    ", SidestepArmMean=" +
                    Num(sidestep.armMeanDifferenceDegreesMax) +
                    ", SidestepArmSwing=" +
                    Num(sidestep.armSwingDifferenceDegreesMax) +
                    ", SidestepGap=" +
                    Num(sidestep.handKneeMinimumBoneGapAfter) + ".");
            }

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "Crouch moving knee-side arm review changed the scene dirty state.");
            }

            Debug.Log(
                "[PlayerCrouchMovingKneeSideArms] Captured actual Play Mode two-loop review." +
                " ForwardFrames=" + forward.framesSampled.ToString(
                    CultureInfo.InvariantCulture) +
                ", ForwardRoot=" + Num(forward.rootHorizontalDisplacementMax) +
                ", ForwardHips=" + Num(forward.hipsHorizontalDisplacementMax) +
                ", ForwardGap=" + Num(forward.handKneeMinimumBoneGapAfter) +
                ", ForwardArmMean=" +
                Num(forward.armMeanDifferenceDegreesMax) +
                ", ForwardArmSwing=" +
                Num(forward.armSwingDifferenceDegreesMax) +
                " BackwardFrames=" + backward.framesSampled.ToString(
                    CultureInfo.InvariantCulture) +
                ", BackwardRoot=" + Num(backward.rootHorizontalDisplacementMax) +
                ", BackwardHips=" + Num(backward.hipsHorizontalDisplacementMax) +
                ", BackwardLoopRotation=" +
                Num(backward.loopRotationDifferenceDegreesMax) +
                ", BackwardArmMean=" +
                Num(backward.armMeanDifferenceDegreesMax) +
                ", BackwardArmSwing=" +
                Num(backward.armSwingDifferenceDegreesMax) +
                ", BackwardGap=" +
                Num(backward.handKneeMinimumBoneGapAfter) +
                ", SidestepFrames=" + sidestep.framesSampled.ToString(
                    CultureInfo.InvariantCulture) +
                ", SidestepRoot=" + Num(sidestep.rootHorizontalDisplacementMax) +
                ", SidestepHips=" + Num(sidestep.hipsHorizontalDisplacementMax) +
                ", SidestepLoopRotation=" +
                Num(sidestep.loopRotationDifferenceDegreesMax) +
                ", SidestepArmMean=" +
                Num(sidestep.armMeanDifferenceDegreesMax) +
                ", SidestepArmSwing=" +
                Num(sidestep.armSwingDifferenceDegreesMax) +
                ", SidestepGap=" +
                Num(sidestep.handKneeMinimumBoneGapAfter) +
                ", ApplyRootMotion=False, LoopsPerTarget=2.");
        }

        private static TargetReviewMetrics CaptureTarget(
            Scene scene,
            string targetName,
            string stateName,
            string expectedClipPath,
            string contactPath)
        {
            Transform target =
                PlayerCrouchBackwardSidestepAnimationTool.RequireTarget(
                    scene,
                    targetName);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    targetName + " Animator is missing.");
            if (animator.runtimeAnimatorController == null ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    targetName + " Animator configuration differs.");
            }

            AnimationClip[] clips = animator.runtimeAnimatorController
                .animationClips
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(clips[0]),
                    expectedClipPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    targetName + " controller does not reference the approved clip.");
            }

            AnimationClip clip = clips[0];
            if (!clip.isLooping || clip.frameRate <= 0f || clip.length <= 0f)
            {
                throw new InvalidOperationException(
                    targetName + " clip timing or loop setting differs.");
            }

            int framesPerLoop = Mathf.RoundToInt(clip.length * clip.frameRate);
            if (framesPerLoop < 4)
            {
                throw new InvalidOperationException(
                    targetName + " has too few review frames.");
            }

            Transform hips =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "Hips");
            Transform leftFoot =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "LeftFoot");
            Transform rightFoot =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "RightFoot");
            Transform leftHand =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "LeftHand");
            Transform leftUpperArm =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "LeftArm");
            Transform leftForeArm =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "LeftForeArm");
            Transform rightHand =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "RightHand");
            Transform leftKnee =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "LeftLeg");
            Transform rightKnee =
                PlayerCrouchBackwardSidestepAnimationTool.FindUniqueBone(
                    target,
                    "RightLeg");
            Renderer[] targetRenderers = target
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (targetRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    targetName + " has no enabled renderer.");
            }

            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            RendererState[] hiddenRendererStates = null;
            GameObject frontCameraObject = null;
            GameObject sideCameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            Texture2D frameTexture = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                hiddenRendererStates = IsolateTargetRenderers(targetRenderers);
                frontCameraObject = CreateCameraObject(targetName + "FrontCamera");
                sideCameraObject = CreateCameraObject(targetName + "SideCamera");
                Camera frontCamera = frontCameraObject.GetComponent<Camera>();
                Camera sideCamera = sideCameraObject.GetComponent<Camera>();
                Vector3 center = target.position + target.up * 1.05f;
                ConfigureFixedCamera(
                    frontCamera,
                    target,
                    center,
                    target.forward,
                    1.35f);
                ConfigureFixedCamera(
                    sideCamera,
                    target,
                    center,
                    target.right,
                    1.35f);

                lightObject = new GameObject(
                    targetName + "ReviewLight",
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
                        targetName + " state was not entered.");
                }

                const int loops = 2;
                int totalFrames = framesPerLoop * loops;
                Pose[] poses = new Pose[totalFrames];
                Vector3 rootBaseline = target.position;
                Vector3 hipsBaseline = hips.position;
                float rootHorizontalMax = 0f;
                float hipsHorizontalMax = 0f;
                List<Vector3> leftFootPositions = new List<Vector3>();
                List<Vector3> rightFootPositions = new List<Vector3>();
                List<Vector3> leftHandPositions = new List<Vector3>();
                List<Vector3> rightHandPositions = new List<Vector3>();
                List<Vector3> leftKneePositions = new List<Vector3>();
                List<Vector3> rightKneePositions = new List<Vector3>();
                float leftElbowBendMax = 0f;
                Vector3 leftUpperArmDirectionSum = Vector3.zero;
                float leftArmDownwardAngleMax = 0f;
                for (int frame = 0; frame < totalFrames; frame++)
                {
                    animator.Play(
                        stateHash,
                        0,
                        frame / (float)framesPerLoop);
                    animator.Update(0f);
                    poses[frame] = CapturePose(target);
                    rootHorizontalMax = Mathf.Max(
                        rootHorizontalMax,
                        HorizontalDistance(target.position, rootBaseline));
                    hipsHorizontalMax = Mathf.Max(
                        hipsHorizontalMax,
                        HorizontalDistance(hips.position, hipsBaseline));
                    leftFootPositions.Add(leftFoot.position);
                    rightFootPositions.Add(rightFoot.position);
                    if (frame < framesPerLoop)
                    {
                        Vector3 leftUpperArmDirection =
                            (leftForeArm.position - leftUpperArm.position)
                            .normalized;
                        leftUpperArmDirectionSum += leftUpperArmDirection;
                        leftArmDownwardAngleMax = Mathf.Max(
                            leftArmDownwardAngleMax,
                            Vector3.Angle(
                                leftUpperArmDirection,
                                -target.up));
                        leftElbowBendMax = Mathf.Max(
                            leftElbowBendMax,
                            Vector3.Angle(
                                leftForeArm.position - leftUpperArm.position,
                                leftHand.position - leftForeArm.position));
                        leftHandPositions.Add(
                            target.InverseTransformPoint(leftHand.position));
                        rightHandPositions.Add(
                            target.InverseTransformPoint(rightHand.position));
                        leftKneePositions.Add(
                            target.InverseTransformPoint(leftKnee.position));
                        rightKneePositions.Add(
                            target.InverseTransformPoint(rightKnee.position));
                    }
                }

                float handKneeMinimumGap = Mathf.Min(
                    NamedSideHandKneeMinimumGap(
                        leftHandPositions,
                        leftKneePositions),
                    NamedSideHandKneeMinimumGap(
                        rightHandPositions,
                        rightKneePositions));
                if (leftUpperArmDirectionSum.sqrMagnitude <= 0.0000001f)
                {
                    throw new InvalidOperationException(
                        targetName +
                        " left upper-arm mean direction is degenerate.");
                }
                float leftArmDownwardMeanAngle = Vector3.Angle(
                    leftUpperArmDirectionSum.normalized,
                    -target.up);

                float loopPositionDifference = 0f;
                float loopRotationDifference = 0f;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    loopPositionDifference = Mathf.Max(
                        loopPositionDifference,
                        PosePositionDifference(
                            poses[frame],
                            poses[frame + framesPerLoop]));
                    loopRotationDifference = Mathf.Max(
                        loopRotationDifference,
                        PoseRotationDifference(
                            poses[frame],
                            poses[frame + framesPerLoop]));
                }

                float motionPositionDifference = 0f;
                float motionRotationDifference = 0f;
                for (int frame = 1; frame < framesPerLoop; frame++)
                {
                    motionPositionDifference = Mathf.Max(
                        motionPositionDifference,
                        PosePositionDifference(poses[0], poses[frame]));
                    motionRotationDifference = Mathf.Max(
                        motionRotationDifference,
                        PoseRotationDifference(poses[0], poses[frame]));
                }

                int[] phaseIndices = PhaseFrameIndices(framesPerLoop);
                Dictionary<int, byte[]> frontFrames =
                    new Dictionary<int, byte[]>();
                Dictionary<int, byte[]> sideFrames =
                    new Dictionary<int, byte[]>();
                foreach (int frame in phaseIndices)
                {
                    animator.Play(
                        stateHash,
                        0,
                        frame / (float)framesPerLoop);
                    animator.Update(0f);
                    frontFrames.Add(
                        frame,
                        CaptureFrame(frontCamera, renderTexture, frameTexture));
                    sideFrames.Add(
                        frame,
                        CaptureFrame(sideCamera, renderTexture, frameTexture));
                }

                ComposeCapture(
                    frontFrames,
                    sideFrames,
                    phaseIndices,
                    contactPath);
                TargetReviewMetrics metrics = new TargetReviewMetrics
                {
                    target = targetName,
                    state = stateName,
                    clipName = clip.name,
                    clipAssetPath = expectedClipPath,
                    sourceTake =
                        PlayerCrouchBackwardSidestepAnimationTool.ExpectedTakeName,
                    clipDurationSeconds = clip.length,
                    clipFrameRate = clip.frameRate,
                    framesPerLoop = framesPerLoop,
                    framesSampled = totalFrames,
                    loopsSampled = loops,
                    rootHorizontalDisplacementMax = rootHorizontalMax,
                    hipsHorizontalDisplacementMax = hipsHorizontalMax,
                    loopPositionDifferenceMax = loopPositionDifference,
                    loopRotationDifferenceDegreesMax = loopRotationDifference,
                    motionPositionDifferenceMax = motionPositionDifference,
                    motionRotationDifferenceDegreesMax = motionRotationDifference,
                    handKneeMinimumBoneGapTarget =
                        PlayerCrouchBackwardSidestepAnimationTool
                            .KneeSideMinimumBoneGap,
                    handKneeMinimumBoneGapAfter = handKneeMinimumGap,
                    leftElbowBendDegreesMax = leftElbowBendMax,
                    leftArmDownwardMeanAngleDegrees =
                        leftArmDownwardMeanAngle,
                    leftArmDownwardMaximumAngleDegrees =
                        leftArmDownwardAngleMax,
                    leftFootHorizontalRange = HorizontalRange(leftFootPositions),
                    rightFootHorizontalRange = HorizontalRange(rightFootPositions),
                    clipIsLooping = clip.isLooping,
                    applyRootMotion = animator.applyRootMotion
                };
                metrics.passedNumericChecks = TargetPassed(metrics);
                return metrics;
            }
            finally
            {
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                if (hiddenRendererStates != null)
                {
                    foreach (RendererState state in hiddenRendererStates)
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

                if (frontCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(frontCameraObject);
                }

                if (sideCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(sideCameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }
            }
        }

        private static bool TargetPassed(TargetReviewMetrics metrics)
        {
            return metrics != null &&
                   metrics.sourceTake ==
                       PlayerCrouchBackwardSidestepAnimationTool.ExpectedTakeName &&
                   metrics.framesPerLoop >= 4 &&
                   metrics.framesSampled == metrics.framesPerLoop * 2 &&
                   metrics.loopsSampled == 2 &&
                   metrics.rootHorizontalDisplacementMax <= PositionTolerance &&
                   metrics.hipsHorizontalDisplacementMax <= PositionTolerance &&
                   metrics.loopPositionDifferenceMax <= PositionTolerance &&
                   metrics.loopRotationDifferenceDegreesMax <= RotationTolerance &&
                   metrics.motionRotationDifferenceDegreesMax > RotationTolerance &&
                   Mathf.Abs(
                       metrics.handKneeMinimumBoneGapTarget -
                       PlayerCrouchBackwardSidestepAnimationTool
                           .KneeSideMinimumBoneGap) <= PositionTolerance &&
                   metrics.handKneeMinimumBoneGapAfter >=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .KneeSideMinimumBoneGap -
                       PlayerCrouchBackwardSidestepAnimationTool
                           .KneeSideGapTolerance &&
                   metrics.armMeanDifferenceDegreesMax <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .ArmMeanToleranceDegrees &&
                   metrics.armSwingDifferenceDegreesMax <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .ArmSwingToleranceDegrees &&
                   metrics.clipIsLooping &&
                   !metrics.applyRootMotion;
        }

        private static bool LeftArmStraightDownTargetPassed(
            TargetReviewMetrics metrics)
        {
            return metrics != null &&
                   metrics.sourceTake ==
                       PlayerCrouchBackwardSidestepAnimationTool.ExpectedTakeName &&
                   metrics.framesPerLoop >= 4 &&
                   metrics.framesSampled == metrics.framesPerLoop * 2 &&
                   metrics.loopsSampled == 2 &&
                   metrics.rootHorizontalDisplacementMax <= PositionTolerance &&
                   metrics.hipsHorizontalDisplacementMax <= PositionTolerance &&
                   metrics.loopPositionDifferenceMax <= PositionTolerance &&
                   metrics.loopRotationDifferenceDegreesMax <= RotationTolerance &&
                   metrics.motionRotationDifferenceDegreesMax > RotationTolerance &&
                   metrics.leftElbowBendDegreesMax <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .LeftElbowStraightToleranceDegrees &&
                   metrics.leftElbowBendDegreesMaxApply <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .LeftElbowStraightToleranceDegrees &&
                   metrics.leftArmDownwardMeanAngleDegrees <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .LeftArmDownwardMeanToleranceDegrees &&
                   metrics.leftArmDownwardMaximumAngleDegrees <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .LeftArmDownwardMaximumToleranceDegrees &&
                   metrics.leftArmDownwardMeanAngleDegreesApply <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .LeftArmDownwardMeanToleranceDegrees &&
                   metrics.leftArmDownwardMaximumAngleDegreesApply <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .LeftArmDownwardMaximumToleranceDegrees &&
                   Mathf.Abs(
                       metrics.handKneeMinimumBoneGapTarget -
                       PlayerCrouchBackwardSidestepAnimationTool
                           .KneeSideMinimumBoneGap) <= PositionTolerance &&
                   metrics.handKneeMinimumBoneGapAfter >=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .KneeSideMinimumBoneGap -
                       PlayerCrouchBackwardSidestepAnimationTool
                           .KneeSideGapTolerance &&
                   metrics.leftShoulderSwingDifferenceDegreesMax <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .ArmSwingToleranceDegrees &&
                   metrics.leftUpperArmSwingDifferenceDegreesMax <=
                       PlayerCrouchBackwardSidestepAnimationTool
                           .ArmSwingToleranceDegrees &&
                   metrics.clipIsLooping &&
                   !metrics.applyRootMotion;
        }

        private static void CaptureIdleArmReference(Scene scene)
        {
            Transform target =
                PlayerCrouchBackwardSidestepAnimationTool.RequireTarget(
                    scene,
                    PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    "Player_Crouch_Idle Animator is missing.");
            Renderer[] targetRenderers = target
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (targetRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Idle has no enabled renderer.");
            }

            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            RendererState[] hiddenRendererStates = null;
            GameObject frontCameraObject = null;
            GameObject sideCameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            Texture2D frameTexture = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                hiddenRendererStates = IsolateTargetRenderers(targetRenderers);
                frontCameraObject = CreateCameraObject("PlayerCrouchIdleArmFrontCamera");
                sideCameraObject = CreateCameraObject("PlayerCrouchIdleArmSideCamera");
                Camera frontCamera = frontCameraObject.GetComponent<Camera>();
                Camera sideCamera = sideCameraObject.GetComponent<Camera>();
                Vector3 center = target.position + target.up * 1.05f;
                ConfigureFixedCamera(
                    frontCamera,
                    target,
                    center,
                    target.forward,
                    1.35f);
                ConfigureFixedCamera(
                    sideCamera,
                    target,
                    center,
                    target.right,
                    1.35f);

                lightObject = new GameObject(
                    "PlayerCrouchIdleArmReferenceLight",
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
                int stateHash = Animator.StringToHash(
                    PlayerCrouchIdleForwardAnimationTool.IdleStateName);
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                ComposeIdleReference(
                    CaptureFrame(frontCamera, renderTexture, frameTexture),
                    CaptureFrame(sideCamera, renderTexture, frameTexture));
            }
            finally
            {
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                if (hiddenRendererStates != null)
                {
                    foreach (RendererState state in hiddenRendererStates)
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

                if (frontCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(frontCameraObject);
                }

                if (sideCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(sideCameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }
            }
        }

        private static GameObject CreateCameraObject(string name)
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

        private static void ConfigureFixedCamera(
            Camera camera,
            Transform target,
            Vector3 center,
            Vector3 viewDirection,
            float orthographicSize)
        {
            Vector3 direction = Vector3.ProjectOnPlane(
                viewDirection,
                Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    target.name + " has no usable review direction.");
            }

            camera.transform.position = center + direction * 8f;
            camera.transform.LookAt(center, target.up);
            camera.orthographicSize = orthographicSize;
        }

        private static byte[] CaptureFrame(
            Camera camera,
            RenderTexture renderTexture,
            Texture2D frameTexture)
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            frameTexture.ReadPixels(
                new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                0,
                0,
                false);
            frameTexture.Apply(false, false);
            byte[] png = frameTexture.EncodeToPNG();
            camera.targetTexture = null;
            return png;
        }

        private static RendererState[] IsolateTargetRenderers(
            IReadOnlyCollection<Renderer> targetRenderers)
        {
            HashSet<Renderer> targetSet = targetRenderers.ToHashSet();
            RendererState[] states = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(renderer => !targetSet.Contains(renderer))
                .Select(renderer => new RendererState(renderer))
                .ToArray();
            foreach (RendererState state in states)
            {
                state.Hide();
            }

            return states;
        }

        private static Pose CapturePose(Transform root)
        {
            Pose pose = new Pose();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(item, root);
                pose.Positions[path] = item.localPosition;
                pose.Rotations[path] = item.localRotation;
            }

            return pose;
        }

        private static float PosePositionDifference(Pose first, Pose second)
        {
            if (first == null || second == null ||
                !first.Positions.Keys.ToHashSet().SetEquals(second.Positions.Keys))
            {
                return float.PositiveInfinity;
            }

            return first.Positions.Keys.Max(path => Vector3.Distance(
                first.Positions[path],
                second.Positions[path]));
        }

        private static float PoseRotationDifference(Pose first, Pose second)
        {
            if (first == null || second == null ||
                !first.Rotations.Keys.ToHashSet().SetEquals(second.Rotations.Keys))
            {
                return float.PositiveInfinity;
            }

            return first.Rotations.Keys.Max(path => Quaternion.Angle(
                first.Rotations[path],
                second.Rotations[path]));
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            return new Vector2(
                first.x - second.x,
                first.z - second.z).magnitude;
        }

        private static float HorizontalRange(
            IReadOnlyCollection<Vector3> positions)
        {
            if (positions.Count == 0)
            {
                return 0f;
            }

            float minX = positions.Min(position => position.x);
            float maxX = positions.Max(position => position.x);
            float minZ = positions.Min(position => position.z);
            float maxZ = positions.Max(position => position.z);
            return new Vector2(maxX - minX, maxZ - minZ).magnitude;
        }

        private static float NamedSideHandKneeMinimumGap(
            IReadOnlyList<Vector3> hands,
            IReadOnlyList<Vector3> knees)
        {
            if (hands == null || knees == null || hands.Count == 0 ||
                hands.Count != knees.Count)
            {
                return float.NegativeInfinity;
            }

            float sideSign = Mathf.Sign(knees.Average(position => position.x));
            if (Mathf.Abs(sideSign) < 0.5f)
            {
                return float.NegativeInfinity;
            }

            return Enumerable.Range(0, hands.Count).Min(index =>
                sideSign * (hands[index].x - knees[index].x));
        }

        private static Quaternion QuaternionMean(IEnumerable<Quaternion> values)
        {
            Quaternion[] rotations = values.ToArray();
            if (rotations.Length == 0)
            {
                throw new InvalidOperationException(
                    "Quaternion mean requires at least one arm sample.");
            }

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

            if (sum.magnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Quaternion arm mean is degenerate.");
            }

            sum /= sum.magnitude;
            return new Quaternion(sum.x, sum.y, sum.z, sum.w);
        }

        private static int[] PhaseFrameIndices(int framesPerLoop)
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

        private static void ComposeCapture(
            IReadOnlyDictionary<int, byte[]> frontFrames,
            IReadOnlyDictionary<int, byte[]> sideFrames,
            IReadOnlyList<int> indices,
            string outputPath)
        {
            if (indices.Count != 5 ||
                indices.Any(index =>
                    !frontFrames.ContainsKey(index) ||
                    !sideFrames.ContainsKey(index)))
            {
                throw new InvalidOperationException(
                    "Crouch backward and sidestep review frames are incomplete.");
            }

            Texture2D composite = new Texture2D(
                CaptureWidth * 5,
                CaptureHeight * 2,
                TextureFormat.RGB24,
                false);
            List<Texture2D> panels = new List<Texture2D>();
            try
            {
                for (int index = 0; index < indices.Count; index++)
                {
                    Texture2D front = DecodeFrame(frontFrames[indices[index]]);
                    Texture2D side = DecodeFrame(sideFrames[indices[index]]);
                    panels.Add(front);
                    panels.Add(side);
                    composite.SetPixels(
                        index * CaptureWidth,
                        CaptureHeight,
                        CaptureWidth,
                        CaptureHeight,
                        front.GetPixels());
                    composite.SetPixels(
                        index * CaptureWidth,
                        0,
                        CaptureWidth,
                        CaptureHeight,
                        side.GetPixels());
                }

                composite.Apply(false, false);
                string absoluteOutput = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absoluteOutput) ??
                    throw new InvalidOperationException(
                        "Crouch review output directory is unavailable."));
                File.WriteAllBytes(absoluteOutput, composite.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D panel in panels)
                {
                    UnityEngine.Object.DestroyImmediate(panel);
                }

                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static void ComposeIdleReference(byte[] frontPng, byte[] sidePng)
        {
            Texture2D front = DecodeFrame(frontPng);
            Texture2D side = DecodeFrame(sidePng);
            Texture2D composite = new Texture2D(
                CaptureWidth * 2,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                composite.SetPixels(
                    0,
                    0,
                    CaptureWidth,
                    CaptureHeight,
                    front.GetPixels());
                composite.SetPixels(
                    CaptureWidth,
                    0,
                    CaptureWidth,
                    CaptureHeight,
                    side.GetPixels());
                composite.Apply(false, false);
                string absoluteOutput = Path.GetFullPath(IdleArmReferencePath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absoluteOutput) ??
                    throw new InvalidOperationException(
                        "Idle arm reference output directory is unavailable."));
                File.WriteAllBytes(absoluteOutput, composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(front);
                UnityEngine.Object.DestroyImmediate(side);
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static Texture2D DecodeFrame(byte[] png)
        {
            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGB24,
                false);
            if (!texture.LoadImage(png, false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(
                    "Crouch review frame decoding failed.");
            }

            return texture;
        }

        private static void WriteMetrics(ReviewMetrics metrics)
        {
            WriteMetrics(metrics, MetricsPath);
        }

        private static void WriteMetrics(ReviewMetrics metrics, string metricsPath)
        {
            string absolutePath = Path.GetFullPath(metricsPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "Crouch review metrics directory is unavailable."));
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(metrics, true) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static ReviewMetrics ReadMetrics()
        {
            return ReadMetrics(MetricsPath);
        }

        private static ReviewMetrics ReadMetrics(string metricsPath)
        {
            string absolutePath = Path.GetFullPath(metricsPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Crouch backward and sidestep review metrics are missing.",
                    absolutePath);
            }

            ReviewMetrics metrics = JsonUtility.FromJson<ReviewMetrics>(
                File.ReadAllText(absolutePath, Encoding.UTF8));
            return metrics ?? throw new InvalidOperationException(
                "Crouch backward and sidestep review metrics could not be read.");
        }

        private static void AssignLeftArmStraightDownApplyMetrics(
            TargetReviewMetrics review,
            LeftArmStraightDownTargetRead apply)
        {
            if (review == null || apply == null)
            {
                throw new InvalidOperationException(
                    "Left-arm straight-down target metrics are missing.");
            }

            review.leftElbowBendDegreesMaxApply =
                apply.leftElbowBendDegreesMaxAfter;
            review.leftArmDownwardMeanAngleDegreesApply =
                apply.leftArmDownwardMeanAngleDegreesAfter;
            review.leftArmDownwardMaximumAngleDegreesApply =
                apply.leftArmDownwardMaximumAngleDegreesAfter;
            review.handKneeMinimumBoneGapTarget =
                apply.leftHandKneeMinimumBoneGapTarget;
            review.leftShoulderSwingDifferenceDegreesMax =
                apply.leftShoulderSwingDifferenceDegreesMax;
            review.leftUpperArmSwingDifferenceDegreesMax =
                apply.leftUpperArmSwingDifferenceDegreesMax;
        }

        private static LeftArmStraightDownApplyRead
            ReadLeftArmStraightDownApplyMetrics()
        {
            string absolutePath = Path.GetFullPath(
                PlayerCrouchBackwardSidestepAnimationTool
                    .LeftArmStraightDownMetricsPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Crouch left-arm straight-down apply metrics are missing.",
                    absolutePath);
            }

            LeftArmStraightDownApplyRead metrics =
                JsonUtility.FromJson<LeftArmStraightDownApplyRead>(
                    File.ReadAllText(absolutePath, Encoding.UTF8));
            if (metrics == null || metrics.backward == null ||
                metrics.sidestep == null)
            {
                throw new InvalidOperationException(
                    "Crouch left-arm straight-down apply metrics could not be read.");
            }

            return metrics;
        }

        private static void AssignApplyMetrics(
            TargetReviewMetrics review,
            ArmAlignmentTargetRead apply)
        {
            if (review == null || apply == null)
            {
                throw new InvalidOperationException(
                    "Moving crouch knee-side arm target metrics are missing.");
            }

            review.leftArmAdjustmentDegrees = apply.leftArmAdjustmentDegrees;
            review.rightArmAdjustmentDegrees = apply.rightArmAdjustmentDegrees;
            review.handKneeMinimumBoneGapTarget =
                apply.handKneeMinimumBoneGapTarget;
            review.armMeanDifferenceDegreesMax =
                apply.armMeanDifferenceDegreesMax;
            review.armSwingDifferenceDegreesMax =
                apply.armSwingDifferenceDegreesMax;
        }

        private static ArmAlignmentApplyRead ReadMovingKneeSideArmApplyMetrics()
        {
            string absolutePath = Path.GetFullPath(
                PlayerCrouchBackwardSidestepAnimationTool
                    .MovingKneeSideArmMetricsPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Crouch moving knee-side arm apply metrics are missing.",
                    absolutePath);
            }

            ArmAlignmentApplyRead metrics =
                JsonUtility.FromJson<ArmAlignmentApplyRead>(
                    File.ReadAllText(absolutePath, Encoding.UTF8));
            if (metrics == null || metrics.forward == null ||
                metrics.backward == null ||
                metrics.sidestep == null)
            {
                throw new InvalidOperationException(
                    "Crouch moving knee-side arm apply metrics could not be read.");
            }

            return metrics;
        }

        private static void CopyReviewedContact(string contactPath, string finalPath)
        {
            string absoluteContact = Path.GetFullPath(contactPath);
            string absoluteFinal = Path.GetFullPath(finalPath);
            if (!File.Exists(absoluteContact))
            {
                throw new FileNotFoundException(
                    "Reviewed crouch contact sheet is missing.",
                    absoluteContact);
            }

            Directory.CreateDirectory(
                Path.GetDirectoryName(absoluteFinal) ??
                throw new InvalidOperationException(
                    "Crouch final directory is unavailable."));
            File.Copy(absoluteContact, absoluteFinal, true);
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                scene.path !=
                PlayerCrouchBackwardSidestepAnimationTool.ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for crouch review.");
            }

            return scene;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
