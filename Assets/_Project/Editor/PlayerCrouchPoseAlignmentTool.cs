using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerCrouchPoseAlignmentTool
    {
        internal const string ApplyMetricsPath =
            "docs/validation/player_crouch_pose_alignment_apply_metrics.json";
        internal const string ForwardLeftArmStraightDownMetricsPath =
            "docs/validation/player_crouch_forward_left_arm_straight_down_apply_metrics.json";

        private const float CurveTolerance = 0.0001f;
        private const float SwingPreservationToleranceDegrees = 0.2f;
        internal const float ForwardLeftArmSwingSerializationToleranceDegrees = 0.25f;
        private const float ArmAlignmentToleranceDegrees = 0.1f;
        private const float EnterBlendStartNormalized = 0.55f;
        private const float DesiredWaistAngleDegrees = 80f;
        internal const float DesiredHeadDownDegrees = 75f;
        private const float PoseAngleToleranceDegrees = 0.01f;
        private const float UpperBodyCenterTolerance = 0.001f;
        internal const float ArmTorsoClearanceDegrees = 12f;
        internal const float ForwardArmAdvanceDegrees = 45f;
        internal const float RightArmAdditionalClearanceDegrees = 8f;
        internal const float ForwardLeftArmDownDegrees = 30f;
        internal const float ForwardLeftElbowStraightToleranceDegrees = 0.5f;
        internal const float ForwardLeftArmDownwardMeanToleranceDegrees = 30f;
        internal const float ForwardLeftArmDownwardMaximumToleranceDegrees = 45f;
        internal const float ForwardLeftHandKneeMinimumGap = 0.05f;
        internal const float ForwardLeftHandKneeGapTolerance = 0.005f;
        private const float ForwardLeftArmAngleSearchLimit = 60f;
        private const float ForwardLeftArmAngleSearchStep = 0.25f;

        private static readonly string[] SpineBones =
        {
            "Spine02",
            "Spine01",
            "Spine"
        };

        private static readonly string[] ArmBones =
        {
            "LeftShoulder",
            "LeftArm",
            "LeftForeArm",
            "RightShoulder",
            "RightArm",
            "RightForeArm"
        };

        private static readonly string[] RightArmBones =
        {
            "RightShoulder",
            "RightArm",
            "RightForeArm"
        };

        private static readonly string[] LeftArmBones =
        {
            "LeftShoulder",
            "LeftArm",
            "LeftForeArm"
        };

        private static readonly string[] RotationAxes = { "x", "y", "z" };

        [Serializable]
        internal sealed class AlignmentApplyMetrics
        {
            public string waistReference;
            public string armReference;
            public int waistBindingsChanged;
            public int armBindingsChanged;
            public float enterBlendStartNormalized;
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
            public float enterArmSourceDifferenceDegreesMax;
            public float idleArmSourceDifferenceDegreesMax;
            public float armTorsoClearanceDegrees;
            public float forwardArmAdvanceDegrees;
            public float forwardUpperBodyCenterCorrectionDegrees;
            public float forwardUpperBodyMeanLateralOffsetBefore;
            public float forwardUpperBodyMeanLateralOffsetAfter;
            public float rightArmAdditionalClearanceDegrees;
            public float forwardLeftArmDownDegrees;
            public int headBindingsChanged;
            public bool leftArmCurvesUnchanged;
            public bool rightArmCurvesUnchanged;
            public bool enterNonHeadCurvesUnchanged;
            public bool idleNonHeadCurvesUnchanged;
            public bool enterControllerUnchanged;
            public bool idleControllerUnchanged;
            public bool enterNonWaistCurvesUnchanged;
            public bool forwardNonArmCurvesUnchanged;
            public bool armKeyTimingAndTangentsUnchanged;
            public bool armFrameTimingUnchanged;
            public bool armPerFrameSwingPreserved;
            public bool idleMatchesEnterRuntimePose;
            public bool sourceFbxFilesUnchanged;
            public bool enterClipUnchanged;
            public bool idleClipUnchanged;
            public bool forwardControllerUnchanged;
            public bool sceneAssetUnchanged;
            public bool rootTransformsUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool clipTimingUnchanged;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        internal sealed class ForwardLeftArmStraightDownApplyMetrics
        {
            public string target;
            public string clipPath;
            public float leftElbowBendDegreesMaxBefore;
            public float leftElbowBendDegreesMaxAfter;
            public float leftArmDownwardMeanAngleDegreesBefore;
            public float leftArmDownwardMeanAngleDegreesAfter;
            public float leftArmDownwardMaximumAngleDegreesAfter;
            public float leftHandKneeMinimumBoneGapTarget;
            public float leftHandKneeMinimumBoneGapBefore;
            public float leftHandKneeMinimumBoneGapAfter;
            public float leftArmClearanceAdjustmentDegrees;
            public float leftShoulderSwingDifferenceDegreesMax;
            public float leftUpperArmSwingDifferenceDegreesMax;
            public bool curvesOutsideLeftArmUnchanged;
            public bool rightArmCurvesUnchanged;
            public bool referenceClipsUnchanged;
            public bool forwardControllerUnchanged;
            public bool sceneAssetUnchanged;
            public bool sourceFbxFileUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool rootTransformUnchanged;
            public bool leftArmFrameTimingUnchanged;
            public bool clipTimingUnchanged;
            public bool clipIsLooping;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class RootPose
        {
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal Vector3 Scale;
        }

        private sealed class UpperBodyCenterGeometry
        {
            internal float BaseLateral;
            internal Vector3 PivotToShoulderCenter;
            internal float MeanLateralOffset;
        }

        private sealed class LeftArmGeometrySamples
        {
            internal Vector3[] UpperArmDirections;
            internal Vector3[] ForeArmDirections;
            internal Vector3[] LeftHandPositions;
            internal Vector3[] LeftKneePositions;
        }

        private static void ApplyLegacyCurveMean()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch pose alignment.");
            }

            Transform enterTarget = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Transform idleTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            Transform[] targets = { enterTarget, idleTarget, forwardTarget };
            Dictionary<string, RootPose> rootsBefore = CaptureRootPoses(targets);
            Dictionary<string, string> otherAnimatorsBefore = CaptureOtherAnimators(
                scene,
                targets);
            string enterSourceHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.SourcePath);
            string forwardSourceHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath);

            AnimationClip enter = LoadClip(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            float enterDurationBefore = enter.length;
            float forwardDurationBefore = forward.length;
            Dictionary<EditorCurveBinding, AnimationCurve> enterBefore =
                CaptureCurves(enter);
            Dictionary<EditorCurveBinding, AnimationCurve> forwardBefore =
                CaptureCurves(forward);

            float sourceMotionDuration = enter.length -
                                         PlayerCrouchEnterAnimationTool
                                             .HoldDurationSeconds;
            if (sourceMotionDuration <= 0f)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter source motion duration is invalid.");
            }

            string[] enterSpinePaths = ResolveBonePaths(
                enterTarget,
                SpineBones);
            string[] forwardSpinePaths = ResolveBonePaths(
                forwardTarget,
                SpineBones);
            RequireSamePaths(enterSpinePaths, forwardSpinePaths, "spine");
            EditorCurveBinding[] spineBindings = ResolveRotationBindings(
                enter,
                enterSpinePaths);
            AnimationClip alignedEnter = UnityEngine.Object.Instantiate(enter);
            alignedEnter.name = enter.name;
            alignedEnter.hideFlags = HideFlags.None;
            foreach (EditorCurveBinding binding in spineBindings)
            {
                AnimationCurve enterCurve = RequireCurve(alignedEnter, binding);
                AnimationCurve forwardCurve = RequireCurve(forward, binding);
                float desiredMean = CircularMean(
                    forwardCurve,
                    forward.length,
                    forward.frameRate);
                float currentEnd = enterCurve.Evaluate(sourceMotionDuration);
                float offset = Mathf.DeltaAngle(currentEnd, desiredMean);
                ApplyEndWeightedOffset(
                    enterCurve,
                    sourceMotionDuration,
                    offset);
                AnimationUtility.SetEditorCurve(
                    alignedEnter,
                    binding,
                    enterCurve);
            }

            SaveOverExisting(
                alignedEnter,
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            UnityEngine.Object.DestroyImmediate(alignedEnter);

            // Reuse the existing exact final-hold copy path so Idle remains the
            // current Enter final pose without introducing another pose source.
            PlayerCrouchIdleForwardAnimationTool.ApplyIdleFromEnter();
            enter = LoadClip(PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip idle = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);

            string[] idleArmPaths = ResolveBonePaths(idleTarget, ArmBones);
            string[] forwardArmPaths = ResolveBonePaths(forwardTarget, ArmBones);
            RequireSamePaths(idleArmPaths, forwardArmPaths, "arm");
            EditorCurveBinding[] armBindings = ResolveRotationBindings(
                forward,
                forwardArmPaths);
            AnimationClip alignedForward = UnityEngine.Object.Instantiate(forward);
            alignedForward.name = forward.name;
            alignedForward.hideFlags = HideFlags.None;
            foreach (EditorCurveBinding binding in armBindings)
            {
                AnimationCurve idleCurve = RequireCurve(idle, binding);
                AnimationCurve forwardCurve = RequireCurve(
                    alignedForward,
                    binding);
                float idlePose = idleCurve.Evaluate(idle.length * 0.5f);
                float forwardMean = CircularMean(
                    forwardCurve,
                    forward.length,
                    forward.frameRate);
                float offset = Mathf.DeltaAngle(forwardMean, idlePose);
                ApplyConstantOffset(forwardCurve, offset);
                AnimationUtility.SetEditorCurve(
                    alignedForward,
                    binding,
                    forwardCurve);
            }

            SaveOverExisting(
                alignedForward,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            UnityEngine.Object.DestroyImmediate(alignedForward);
            AssetDatabase.SaveAssets();

            enter = LoadClip(PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            idle = LoadClip(PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            AlignmentApplyMetrics metrics = MeasureCurrentAlignment();
            metrics.waistBindingsChanged = spineBindings.Length;
            metrics.armBindingsChanged = armBindings.Length;
            metrics.enterBlendStartNormalized = EnterBlendStartNormalized;
            metrics.enterNonWaistCurvesUnchanged = VerifyOnlyBindingsChanged(
                enterBefore,
                enter,
                new HashSet<EditorCurveBinding>(spineBindings));
            metrics.forwardNonArmCurvesUnchanged = VerifyOnlyBindingsChanged(
                forwardBefore,
                forward,
                new HashSet<EditorCurveBinding>(armBindings));
            metrics.armKeyTimingAndTangentsUnchanged = VerifyCurveMetadata(
                forwardBefore,
                forward,
                armBindings);
            metrics.forwardArmRangeDifferenceDegreesMax = MaxRangeDifference(
                forwardBefore,
                forward,
                armBindings);
            metrics.sourceFbxFilesUnchanged =
                string.Equals(
                    enterSourceHashBefore,
                    HashFile(PlayerCrouchEnterAnimationTool.SourcePath),
                    StringComparison.Ordinal) &&
                string.Equals(
                    forwardSourceHashBefore,
                    HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath),
                    StringComparison.Ordinal);
            metrics.rootTransformsUnchanged = RootPosesEqual(
                rootsBefore,
                CaptureRootPoses(targets));
            metrics.otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimators(scene, targets));
            metrics.clipTimingUnchanged =
                Mathf.Abs(enter.length - enterDurationBefore) <= CurveTolerance &&
                Mathf.Abs(forward.length - forwardDurationBefore) <= CurveTolerance &&
                Mathf.Abs(
                    idle.length -
                    PlayerCrouchIdleForwardAnimationTool.IdleDurationSeconds) <=
                CurveTolerance;
            metrics.applyRootMotion = targets
                .Select(target => target.GetComponent<Animator>())
                .Any(animator => animator == null || animator.applyRootMotion);
            metrics.passedNumericChecks =
                metrics.enterWaistMeanDifferenceDegreesMax <= CurveTolerance &&
                metrics.idleWaistMeanDifferenceDegreesMax <= CurveTolerance &&
                metrics.enterIdleWaistDifferenceDegreesMax <= CurveTolerance &&
                metrics.forwardArmMeanDifferenceDegreesMax <= CurveTolerance &&
                metrics.forwardArmRangeDifferenceDegreesMax <= CurveTolerance &&
                metrics.enterNonWaistCurvesUnchanged &&
                metrics.forwardNonArmCurvesUnchanged &&
                metrics.armKeyTimingAndTangentsUnchanged &&
                metrics.sourceFbxFilesUnchanged &&
                metrics.rootTransformsUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.clipTimingUnchanged &&
                !metrics.applyRootMotion;
            WriteMetrics(metrics);

            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch pose alignment support checks failed." +
                    " EnterWaist=" + Num(
                        metrics.enterWaistMeanDifferenceDegreesMax) +
                    ", IdleWaist=" + Num(
                        metrics.idleWaistMeanDifferenceDegreesMax) +
                    ", Arms=" + Num(
                        metrics.forwardArmMeanDifferenceDegreesMax) +
                    ", ArmRange=" + Num(
                        metrics.forwardArmRangeDifferenceDegreesMax) + ".");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp became dirty during crouch pose alignment.");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Applied approved mean-pose alignment." +
                " WaistReference=Player_Crouch_Forward one-cycle circular mean" +
                ", EnterBlendStartNormalized=" + Num(EnterBlendStartNormalized) +
                ", WaistBindings=" + spineBindings.Length.ToString(
                    CultureInfo.InvariantCulture) +
                ", ArmReference=Player_Crouch_Idle static pose" +
                ", ArmBindings=" + armBindings.Length.ToString(
                    CultureInfo.InvariantCulture) +
                ", EnterWaistDifference=" + Num(
                    metrics.enterWaistMeanDifferenceDegreesMax) +
                ", IdleWaistDifference=" + Num(
                    metrics.idleWaistMeanDifferenceDegreesMax) +
                ", ForwardArmMeanDifference=" + Num(
                    metrics.forwardArmMeanDifferenceDegreesMax) +
                ", ArmSwingRangeDifference=" + Num(
                    metrics.forwardArmRangeDifferenceDegreesMax) +
                ", TimingChanged=False, NonTargetCurvesChanged=False" +
                ", ApplyRootMotion=False.");
        }

        private static AlignmentApplyMetrics MeasureLegacyCurveAlignment()
        {
            AnimationClip enter = LoadClip(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip idle = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            float sourceMotionDuration = enter.length -
                                         PlayerCrouchEnterAnimationTool
                                             .HoldDurationSeconds;
            Scene scene = RequireScene();
            Transform enterTarget = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Transform idleTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            string[] spinePaths = ResolveBonePaths(forwardTarget, SpineBones);
            string[] enterSpinePaths = ResolveBonePaths(enterTarget, SpineBones);
            string[] idleSpinePaths = ResolveBonePaths(idleTarget, SpineBones);
            RequireSamePaths(spinePaths, enterSpinePaths, "enter spine");
            RequireSamePaths(spinePaths, idleSpinePaths, "idle spine");
            EditorCurveBinding[] spineBindings = ResolveRotationBindings(
                forward,
                spinePaths);
            float enterWaistError = 0f;
            float idleWaistError = 0f;
            float enterIdleWaistError = 0f;
            foreach (EditorCurveBinding binding in spineBindings)
            {
                float forwardMean = CircularMean(
                    RequireCurve(forward, binding),
                    forward.length,
                    forward.frameRate);
                float enterEnd = RequireCurve(enter, binding).Evaluate(
                    sourceMotionDuration);
                float idlePose = RequireCurve(idle, binding).Evaluate(
                    idle.length * 0.5f);
                enterWaistError = Mathf.Max(
                    enterWaistError,
                    Mathf.Abs(Mathf.DeltaAngle(enterEnd, forwardMean)));
                idleWaistError = Mathf.Max(
                    idleWaistError,
                    Mathf.Abs(Mathf.DeltaAngle(idlePose, forwardMean)));
                enterIdleWaistError = Mathf.Max(
                    enterIdleWaistError,
                    Mathf.Abs(Mathf.DeltaAngle(enterEnd, idlePose)));
            }

            string[] armPaths = ResolveBonePaths(forwardTarget, ArmBones);
            string[] idleArmPaths = ResolveBonePaths(idleTarget, ArmBones);
            RequireSamePaths(armPaths, idleArmPaths, "idle arm");
            EditorCurveBinding[] armBindings = ResolveRotationBindings(
                forward,
                armPaths);
            float armError = 0f;
            foreach (EditorCurveBinding binding in armBindings)
            {
                float forwardMean = CircularMean(
                    RequireCurve(forward, binding),
                    forward.length,
                    forward.frameRate);
                float idlePose = RequireCurve(idle, binding).Evaluate(
                    idle.length * 0.5f);
                armError = Mathf.Max(
                    armError,
                    Mathf.Abs(Mathf.DeltaAngle(forwardMean, idlePose)));
            }

            return new AlignmentApplyMetrics
            {
                waistReference =
                    "Player_Crouch_Forward one-cycle circular mean per Spine rotation curve",
                armReference =
                    "Player_Crouch_Idle static pose with Player_Crouch_Forward per-frame deviations preserved",
                enterWaistMeanDifferenceDegreesMax = enterWaistError,
                idleWaistMeanDifferenceDegreesMax = idleWaistError,
                enterIdleWaistDifferenceDegreesMax = enterIdleWaistError,
                forwardArmMeanDifferenceDegreesMax = armError,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
        }

        private sealed class TransformSnapshot
        {
            internal string Path;
            internal Vector3 LocalPosition;
            internal Quaternion LocalRotation;
            internal bool HasPosition;
            internal bool HasRotation;
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Forward Arm Reach")]
        internal static void ApplyForwardArmReach()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch forward arm reach.");
            }

            Transform enterTarget = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Transform idleTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            Transform[] modifiedTargets = { forwardTarget };
            Dictionary<string, RootPose> rootsBefore = CaptureRootPoses(
                modifiedTargets);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimators(scene, modifiedTargets);
            string enterSourceHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.SourcePath);
            string forwardSourceHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath);
            string enterClipHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            string idleClipHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            string forwardControllerHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath);
            string sceneHashBefore = HashFile(scene.path);

            AnimationClip enterSource =
                PlayerCrouchEnterAnimationTool.LoadSingleSourceClip();
            AnimationClip enter = LoadClip(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip idle = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            float forwardDurationBefore = forward.length;
            Dictionary<EditorCurveBinding, AnimationCurve> forwardBefore =
                CaptureCurves(forward);
            float[] forwardTimes = FrameTimes(
                forward.length,
                forward.frameRate,
                includeEnd: true);
            Dictionary<string, Quaternion[]> forwardArmRotationsBefore =
                SampleTargetRelativeArmRotations(
                    forward,
                    forwardTarget,
                    forwardTimes);
            float enterEnd = enter.length -
                             PlayerCrouchEnterAnimationTool.HoldDurationSeconds;
            float sourceReferenceTime = Mathf.Min(enterEnd, enterSource.length);

            Dictionary<string, Quaternion[]> desiredForwardArmRotations =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            foreach (string boneName in ArmBones)
            {
                Quaternion rawSourceReference = SampleTargetRelativeRotations(
                    enterSource,
                    enterTarget,
                    boneName,
                    new[] { sourceReferenceTime })[0];
                Quaternion desiredMean = ApplyArmPoseOffset(
                    boneName,
                    rawSourceReference,
                    advanceForward: true);
                Quaternion currentMean = QuaternionMean(
                    forwardArmRotationsBefore[boneName]
                        .Take(forwardArmRotationsBefore[boneName].Length - 1));
                Quaternion meanOffset = desiredMean *
                                        Quaternion.Inverse(currentMean);
                desiredForwardArmRotations.Add(
                    boneName,
                    forwardArmRotationsBefore[boneName]
                        .Select(rotation => meanOffset * rotation)
                        .ToArray());
            }

            Dictionary<string, Quaternion[]> alignedForwardArmLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    forward,
                    forwardTarget,
                    forwardTimes,
                    desiredForwardArmRotations);
            AnimationClip alignedForward = UnityEngine.Object.Instantiate(forward);
            alignedForward.name = forward.name;
            alignedForward.hideFlags = HideFlags.None;
            foreach (string boneName in ArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    alignedForward,
                    BonePath(forwardTarget, boneName),
                    forwardTimes,
                    alignedForwardArmLocals[boneName]);
            }

            SaveOverExisting(
                alignedForward,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            UnityEngine.Object.DestroyImmediate(alignedForward);
            AssetDatabase.SaveAssets();
            forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            Animator forwardAnimator = forwardTarget.GetComponent<Animator>() ??
                                       throw new InvalidOperationException(
                                           "Player_Crouch_Forward Animator is missing.");
            forwardAnimator.Rebind();

            Dictionary<string, Quaternion[]> forwardArmRotationsAfter =
                SampleTargetRelativeArmRotations(
                    forward,
                    forwardTarget,
                    forwardTimes);
            float forwardArmSwingDifference = ArmBones.Max(boneName =>
                SwingDifference(
                    forwardArmRotationsBefore[boneName],
                    forwardArmRotationsAfter[boneName]));
            HashSet<string> forwardArmPaths = new HashSet<string>(
                ArmBones.Select(boneName => BonePath(forwardTarget, boneName)),
                StringComparer.Ordinal);

            AlignmentApplyMetrics metrics = MeasureCurrentAlignment();
            metrics.waistBindingsChanged = 0;
            metrics.armBindingsChanged = ArmBones.Length;
            metrics.enterBlendStartNormalized = EnterBlendStartNormalized;
            metrics.armTorsoClearanceDegrees = ArmTorsoClearanceDegrees;
            metrics.forwardArmAdvanceDegrees = ForwardArmAdvanceDegrees;
            metrics.forwardArmRangeDifferenceDegreesMax =
                forwardArmSwingDifference;
            metrics.enterClipUnchanged = string.Equals(
                enterClipHashBefore,
                HashFile(PlayerCrouchEnterAnimationTool.CorrectedClipPath),
                StringComparison.Ordinal);
            metrics.idleClipUnchanged = string.Equals(
                idleClipHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.IdleClipPath),
                StringComparison.Ordinal);
            metrics.forwardControllerUnchanged = string.Equals(
                forwardControllerHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath),
                StringComparison.Ordinal);
            metrics.sceneAssetUnchanged = string.Equals(
                sceneHashBefore,
                HashFile(scene.path),
                StringComparison.Ordinal);
            metrics.enterNonWaistCurvesUnchanged =
                metrics.enterClipUnchanged && metrics.idleClipUnchanged;
            metrics.forwardNonArmCurvesUnchanged = VerifyOutsidePathsUnchanged(
                forwardBefore,
                forward,
                forwardArmPaths);
            metrics.armKeyTimingAndTangentsUnchanged = false;
            metrics.armFrameTimingUnchanged =
                forwardTimes.Length == forwardArmRotationsAfter[ArmBones[0]].Length;
            metrics.armPerFrameSwingPreserved =
                forwardArmSwingDifference <=
                SwingPreservationToleranceDegrees;
            metrics.idleMatchesEnterRuntimePose = RuntimePosesMatch(
                enter,
                enterTarget,
                enterEnd,
                idle,
                idleTarget,
                0f);
            metrics.sourceFbxFilesUnchanged =
                string.Equals(
                    enterSourceHashBefore,
                    HashFile(PlayerCrouchEnterAnimationTool.SourcePath),
                    StringComparison.Ordinal) &&
                string.Equals(
                    forwardSourceHashBefore,
                    HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath),
                    StringComparison.Ordinal);
            metrics.rootTransformsUnchanged = RootPosesEqual(
                rootsBefore,
                CaptureRootPoses(modifiedTargets));
            metrics.otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimators(scene, modifiedTargets));
            metrics.clipTimingUnchanged =
                Mathf.Abs(forward.length - forwardDurationBefore) <=
                CurveTolerance;
            metrics.applyRootMotion = forwardAnimator.applyRootMotion;
            metrics.passedNumericChecks =
                metrics.forwardArmMeanDifferenceDegreesMax <=
                    ArmAlignmentToleranceDegrees &&
                metrics.forwardArmRangeDifferenceDegreesMax <=
                    SwingPreservationToleranceDegrees &&
                metrics.enterClipUnchanged &&
                metrics.idleClipUnchanged &&
                metrics.forwardControllerUnchanged &&
                metrics.sceneAssetUnchanged &&
                metrics.forwardNonArmCurvesUnchanged &&
                metrics.armFrameTimingUnchanged &&
                metrics.armPerFrameSwingPreserved &&
                metrics.idleMatchesEnterRuntimePose &&
                metrics.sourceFbxFilesUnchanged &&
                metrics.rootTransformsUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.clipTimingUnchanged &&
                !metrics.applyRootMotion;
            WriteMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch forward arm reach support checks failed." +
                    " ForwardArms=" + Num(
                        metrics.forwardArmMeanDifferenceDegreesMax) +
                    ", ArmSwing=" + Num(
                        metrics.forwardArmRangeDifferenceDegreesMax) +
                    ", EnterUnchanged=" + metrics.enterClipUnchanged +
                    ", IdleUnchanged=" + metrics.idleClipUnchanged +
                    ", ControllerUnchanged=" +
                    metrics.forwardControllerUnchanged +
                    ", SceneUnchanged=" + metrics.sceneAssetUnchanged + ".");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp became dirty during crouch forward arm reach.");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Applied Forward-only arm reach." +
                " ForwardArmAdvanceDegrees=" + Num(
                    ForwardArmAdvanceDegrees) +
                ", ArmTorsoClearanceDegrees=" + Num(
                    ArmTorsoClearanceDegrees) +
                ", ForwardArmMeanDifference=" + Num(
                    metrics.forwardArmMeanDifferenceDegreesMax) +
                ", ForwardArmSwingDifference=" + Num(
                    metrics.forwardArmRangeDifferenceDegreesMax) +
                ", EnterClipChanged=False, IdleClipChanged=False" +
                ", ControllerChanged=False, SceneChanged=False" +
                ", TimingChanged=False, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Forward Upper Body And Right Arm Correction")]
        internal static void ApplyForwardUpperBodyAndRightArmCorrection()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch forward upper-body correction.");
            }

            Transform enterTarget = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            Transform[] modifiedTargets = { forwardTarget };
            Dictionary<string, RootPose> rootsBefore = CaptureRootPoses(
                modifiedTargets);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimators(scene, modifiedTargets);
            string enterSourceHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.SourcePath);
            string forwardSourceHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath);
            string enterClipHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            string idleClipHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            string forwardControllerHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath);
            string sceneHashBefore = HashFile(scene.path);

            AnimationClip enterSource =
                PlayerCrouchEnterAnimationTool.LoadSingleSourceClip();
            AnimationClip forwardSource = LoadForwardSourceClip();
            AnimationClip enter = LoadClip(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip idle = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            if (Mathf.Abs(forwardSource.length - forward.length) > CurveTolerance ||
                Mathf.Abs(forwardSource.frameRate - forward.frameRate) >
                CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Forward source and in-place timing differ.");
            }

            float forwardDurationBefore = forward.length;
            Dictionary<EditorCurveBinding, AnimationCurve> forwardBefore =
                CaptureCurves(forward);
            float[] forwardTimes = FrameTimes(
                forward.length,
                forward.frameRate,
                includeEnd: true);
            float[] analysisTimes = FrameTimes(
                forward.length,
                forward.frameRate,
                includeEnd: false);
            UpperBodyCenterGeometry sourceCenter = MeasureUpperBodyCenterGeometry(
                forwardSource,
                forwardTarget,
                analysisTimes);
            float centerCorrectionDegrees = SolveUpperBodyCenterCorrectionDegrees(
                sourceCenter);
            Quaternion centerCorrection = Quaternion.AngleAxis(
                centerCorrectionDegrees,
                Vector3.forward);

            Quaternion[] sourceSpineRotations = SampleTargetRelativeRotations(
                forwardSource,
                forwardTarget,
                "Spine02",
                forwardTimes);
            Quaternion[] desiredSpineRotations = sourceSpineRotations
                .Select(rotation => centerCorrection * rotation)
                .ToArray();
            Quaternion[] desiredSpineLocals = ConvertTargetRelativeRotationToLocal(
                forward,
                forwardTarget,
                "Spine02",
                forwardTimes,
                desiredSpineRotations);

            AnimationClip correctedForward = UnityEngine.Object.Instantiate(forward);
            correctedForward.name = forward.name;
            correctedForward.hideFlags = HideFlags.None;
            string lowerSpinePath = BonePath(forwardTarget, "Spine02");
            ReplaceRotationWithQuaternionCurves(
                correctedForward,
                lowerSpinePath,
                forwardTimes,
                desiredSpineLocals);

            float enterEnd = enter.length -
                             PlayerCrouchEnterAnimationTool.HoldDurationSeconds;
            float sourceReferenceTime = Mathf.Min(enterEnd, enterSource.length);
            Dictionary<string, Quaternion[]> sourceForwardArmRotations =
                SampleTargetRelativeArmRotations(
                    forwardSource,
                    forwardTarget,
                    forwardTimes);
            Dictionary<string, Quaternion[]> desiredRightArmRotations =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            Quaternion additionalRightClearance = Quaternion.AngleAxis(
                RightArmAdditionalClearanceDegrees,
                Vector3.forward);
            foreach (string boneName in RightArmBones)
            {
                Quaternion rawEnterReference = SampleTargetRelativeRotations(
                    enterSource,
                    enterTarget,
                    boneName,
                    new[] { sourceReferenceTime })[0];
                Quaternion desiredBaseMean = ApplyArmPoseOffset(
                    boneName,
                    rawEnterReference,
                    advanceForward: true);
                Quaternion sourceMean = QuaternionMean(
                    sourceForwardArmRotations[boneName]
                        .Take(sourceForwardArmRotations[boneName].Length - 1));
                Quaternion baseMeanOffset = desiredBaseMean *
                                            Quaternion.Inverse(sourceMean);
                desiredRightArmRotations.Add(
                    boneName,
                    sourceForwardArmRotations[boneName]
                        .Select(rotation =>
                            centerCorrection *
                            additionalRightClearance *
                            baseMeanOffset *
                            rotation)
                        .ToArray());
            }

            Dictionary<string, Quaternion[]> desiredRightArmLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    correctedForward,
                    forwardTarget,
                    forwardTimes,
                    desiredRightArmRotations,
                    RightArmBones);
            foreach (string boneName in RightArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    correctedForward,
                    BonePath(forwardTarget, boneName),
                    forwardTimes,
                    desiredRightArmLocals[boneName]);
            }

            SaveOverExisting(
                correctedForward,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            UnityEngine.Object.DestroyImmediate(correctedForward);
            AssetDatabase.SaveAssets();
            forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            Animator forwardAnimator = forwardTarget.GetComponent<Animator>() ??
                                       throw new InvalidOperationException(
                                           "Player_Crouch_Forward Animator is missing.");
            forwardAnimator.Rebind();

            UpperBodyCenterGeometry correctedCenter =
                MeasureUpperBodyCenterGeometry(
                    forward,
                    forwardTarget,
                    analysisTimes);
            Dictionary<string, Quaternion[]> forwardArmRotationsAfter =
                SampleTargetRelativeArmRotations(
                    forward,
                    forwardTarget,
                    forwardTimes);
            float forwardArmSwingDifference = RightArmBones.Max(boneName =>
                SwingDifference(
                    desiredRightArmRotations[boneName],
                    forwardArmRotationsAfter[boneName]));
            float rightArmMeanDifference = RightArmBones.Max(boneName =>
                Quaternion.Angle(
                    QuaternionMean(
                        desiredRightArmRotations[boneName]
                            .Take(desiredRightArmRotations[boneName].Length - 1)),
                    QuaternionMean(
                        forwardArmRotationsAfter[boneName]
                            .Take(forwardArmRotationsAfter[boneName].Length - 1))));

            HashSet<string> approvedPaths = new HashSet<string>(
                RightArmBones.Select(boneName => BonePath(forwardTarget, boneName)),
                StringComparer.Ordinal)
            {
                lowerSpinePath
            };
            HashSet<string> leftArmPaths = new HashSet<string>(
                ArmBones
                    .Where(boneName => boneName.StartsWith(
                        "Left",
                        StringComparison.Ordinal))
                    .Select(boneName => BonePath(forwardTarget, boneName)),
                StringComparer.Ordinal);

            AlignmentApplyMetrics metrics = MeasureCurrentAlignment();
            metrics.waistReference =
                "Player_Crouch_Forward shoulder midpoint one-cycle mean centered over Hips";
            metrics.armReference =
                "Left arm curves unchanged; Right arm keeps 45-degree advance with additional outward clearance";
            metrics.waistBindingsChanged = 1;
            metrics.armBindingsChanged = RightArmBones.Length;
            metrics.enterBlendStartNormalized = EnterBlendStartNormalized;
            metrics.forwardArmMeanDifferenceDegreesMax = rightArmMeanDifference;
            metrics.forwardArmRangeDifferenceDegreesMax =
                forwardArmSwingDifference;
            metrics.armTorsoClearanceDegrees = ArmTorsoClearanceDegrees;
            metrics.forwardArmAdvanceDegrees = ForwardArmAdvanceDegrees;
            metrics.forwardUpperBodyCenterCorrectionDegrees =
                centerCorrectionDegrees;
            metrics.forwardUpperBodyMeanLateralOffsetBefore =
                sourceCenter.MeanLateralOffset;
            metrics.forwardUpperBodyMeanLateralOffsetAfter =
                correctedCenter.MeanLateralOffset;
            metrics.rightArmAdditionalClearanceDegrees =
                RightArmAdditionalClearanceDegrees;
            metrics.enterClipUnchanged = string.Equals(
                enterClipHashBefore,
                HashFile(PlayerCrouchEnterAnimationTool.CorrectedClipPath),
                StringComparison.Ordinal);
            metrics.idleClipUnchanged = string.Equals(
                idleClipHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.IdleClipPath),
                StringComparison.Ordinal);
            metrics.forwardControllerUnchanged = string.Equals(
                forwardControllerHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath),
                StringComparison.Ordinal);
            metrics.sceneAssetUnchanged = string.Equals(
                sceneHashBefore,
                HashFile(scene.path),
                StringComparison.Ordinal);
            metrics.enterNonWaistCurvesUnchanged =
                metrics.enterClipUnchanged && metrics.idleClipUnchanged;
            metrics.forwardNonArmCurvesUnchanged = VerifyOutsidePathsUnchanged(
                forwardBefore,
                forward,
                approvedPaths);
            metrics.leftArmCurvesUnchanged = VerifyPathsUnchanged(
                forwardBefore,
                forward,
                leftArmPaths);
            metrics.armKeyTimingAndTangentsUnchanged = false;
            metrics.armFrameTimingUnchanged =
                forwardTimes.Length == forwardArmRotationsAfter[RightArmBones[0]].Length;
            metrics.armPerFrameSwingPreserved =
                forwardArmSwingDifference <=
                SwingPreservationToleranceDegrees;
            metrics.idleMatchesEnterRuntimePose = RuntimePosesMatch(
                enter,
                enterTarget,
                enterEnd,
                idle,
                PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                    scene,
                    PlayerCrouchIdleForwardAnimationTool.IdleTargetName),
                0f);
            metrics.sourceFbxFilesUnchanged =
                string.Equals(
                    enterSourceHashBefore,
                    HashFile(PlayerCrouchEnterAnimationTool.SourcePath),
                    StringComparison.Ordinal) &&
                string.Equals(
                    forwardSourceHashBefore,
                    HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath),
                    StringComparison.Ordinal);
            metrics.rootTransformsUnchanged = RootPosesEqual(
                rootsBefore,
                CaptureRootPoses(modifiedTargets));
            metrics.otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimators(scene, modifiedTargets));
            metrics.clipTimingUnchanged =
                Mathf.Abs(forward.length - forwardDurationBefore) <=
                CurveTolerance;
            metrics.applyRootMotion = forwardAnimator.applyRootMotion;
            metrics.passedNumericChecks =
                Mathf.Abs(metrics.forwardUpperBodyMeanLateralOffsetAfter) <=
                    UpperBodyCenterTolerance &&
                metrics.forwardArmMeanDifferenceDegreesMax <=
                    ArmAlignmentToleranceDegrees &&
                metrics.forwardArmRangeDifferenceDegreesMax <=
                    SwingPreservationToleranceDegrees &&
                metrics.leftArmCurvesUnchanged &&
                metrics.enterClipUnchanged &&
                metrics.idleClipUnchanged &&
                metrics.forwardControllerUnchanged &&
                metrics.sceneAssetUnchanged &&
                metrics.forwardNonArmCurvesUnchanged &&
                metrics.armFrameTimingUnchanged &&
                metrics.armPerFrameSwingPreserved &&
                metrics.idleMatchesEnterRuntimePose &&
                metrics.sourceFbxFilesUnchanged &&
                metrics.rootTransformsUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.clipTimingUnchanged &&
                !metrics.applyRootMotion;
            WriteMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch forward upper-body and right-arm support checks failed." +
                    " UpperBodyLateral=" + Num(
                        metrics.forwardUpperBodyMeanLateralOffsetAfter) +
                    ", RightArm=" + Num(
                        metrics.forwardArmMeanDifferenceDegreesMax) +
                    ", ArmSwing=" + Num(
                        metrics.forwardArmRangeDifferenceDegreesMax) +
                    ", LeftArmUnchanged=" + metrics.leftArmCurvesUnchanged +
                    ", EnterUnchanged=" + metrics.enterClipUnchanged +
                    ", IdleUnchanged=" + metrics.idleClipUnchanged +
                    ", ControllerUnchanged=" +
                    metrics.forwardControllerUnchanged +
                    ", SceneUnchanged=" + metrics.sceneAssetUnchanged + ".");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp became dirty during crouch forward correction.");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Applied Forward upper-body and right-arm correction." +
                " UpperBodyCenterCorrectionDegrees=" + Num(
                    metrics.forwardUpperBodyCenterCorrectionDegrees) +
                ", UpperBodyLateralBefore=" + Num(
                    metrics.forwardUpperBodyMeanLateralOffsetBefore) +
                ", UpperBodyLateralAfter=" + Num(
                    metrics.forwardUpperBodyMeanLateralOffsetAfter) +
                ", ForwardArmAdvanceDegrees=" + Num(
                    ForwardArmAdvanceDegrees) +
                ", RightArmAdditionalClearanceDegrees=" + Num(
                    RightArmAdditionalClearanceDegrees) +
                ", LeftArmChanged=False, EnterClipChanged=False" +
                ", IdleClipChanged=False, ControllerChanged=False" +
                ", SceneChanged=False, TimingChanged=False" +
                ", ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Forward Left Arm And Head Correction")]
        internal static void ApplyForwardLeftArmAndHeadCorrection()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch left-arm and head correction.");
            }

            Transform enterTarget = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Transform idleTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            Transform[] modifiedTargets =
            {
                enterTarget,
                idleTarget,
                forwardTarget
            };
            Dictionary<string, RootPose> rootsBefore = CaptureRootPoses(
                modifiedTargets);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimators(scene, modifiedTargets);
            string enterSourceHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.SourcePath);
            string forwardSourceHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath);
            string enterClipHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            string idleClipHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            string enterControllerHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.ControllerPath);
            string idleControllerHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleControllerPath);
            string forwardControllerHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath);
            string sceneHashBefore = HashFile(scene.path);

            AnimationClip enterSource =
                PlayerCrouchEnterAnimationTool.LoadSingleSourceClip();
            AnimationClip forwardSource = LoadForwardSourceClip();
            AnimationClip enter = LoadClip(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip idle = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            float enterDurationBefore = enter.length;
            float idleDurationBefore = idle.length;
            float forwardDurationBefore = forward.length;
            Dictionary<EditorCurveBinding, AnimationCurve> enterBefore =
                CaptureCurves(enter);
            Dictionary<EditorCurveBinding, AnimationCurve> idleBefore =
                CaptureCurves(idle);
            Dictionary<EditorCurveBinding, AnimationCurve> forwardBefore =
                CaptureCurves(forward);

            float sourceMotionDuration = enter.length -
                                         PlayerCrouchEnterAnimationTool
                                             .HoldDurationSeconds;
            float[] enterTimes = FrameTimes(
                enter.length,
                enter.frameRate,
                includeEnd: true);
            Vector3 enterWaistDirection = SampleTargetRelativeDirections(
                enter,
                enterTarget,
                new[] { sourceMotionDuration })[0];
            Vector3 desiredHeadForward = DesiredHeadForwardDirection(
                enterWaistDirection,
                DesiredHeadDownDegrees);
            Quaternion headCorrection = CalculateBoneForwardDirectionCorrection(
                enter,
                enterTarget,
                "Head",
                sourceMotionDuration,
                desiredHeadForward);
            Quaternion[] enterHeadSamples = SampleLocalRotations(
                enter,
                enterTarget,
                "Head",
                enterTimes);
            Quaternion[] correctedEnterHeadSamples =
                new Quaternion[enterHeadSamples.Length];
            float blendStart = sourceMotionDuration * EnterBlendStartNormalized;
            for (int index = 0; index < enterTimes.Length; index++)
            {
                float normalized = Mathf.InverseLerp(
                    blendStart,
                    sourceMotionDuration,
                    enterTimes[index]);
                float weight = normalized * normalized * (3f - 2f * normalized);
                correctedEnterHeadSamples[index] =
                    Quaternion.Slerp(Quaternion.identity, headCorrection, weight) *
                    enterHeadSamples[index];
            }

            AnimationClip correctedEnter = UnityEngine.Object.Instantiate(enter);
            correctedEnter.name = enter.name;
            correctedEnter.hideFlags = HideFlags.None;
            string enterHeadPath = BonePath(enterTarget, "Head");
            ReplaceRotationWithQuaternionCurves(
                correctedEnter,
                enterHeadPath,
                enterTimes,
                correctedEnterHeadSamples);
            SaveOverExisting(
                correctedEnter,
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            UnityEngine.Object.DestroyImmediate(correctedEnter);
            enter = LoadClip(PlayerCrouchEnterAnimationTool.CorrectedClipPath);

            Quaternion idleHeadPose = SampleLocalRotations(
                enter,
                enterTarget,
                "Head",
                new[] { sourceMotionDuration })[0];
            AnimationClip correctedIdle = UnityEngine.Object.Instantiate(idle);
            correctedIdle.name = idle.name;
            correctedIdle.hideFlags = HideFlags.None;
            string idleHeadPath = BonePath(idleTarget, "Head");
            float[] idleTimes =
            {
                0f,
                idle.length
            };
            ReplaceRotationWithQuaternionCurves(
                correctedIdle,
                idleHeadPath,
                idleTimes,
                new[] { idleHeadPose, idleHeadPose });
            SaveOverExisting(
                correctedIdle,
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            UnityEngine.Object.DestroyImmediate(correctedIdle);
            idle = LoadClip(PlayerCrouchIdleForwardAnimationTool.IdleClipPath);

            float[] forwardTimes = FrameTimes(
                forward.length,
                forward.frameRate,
                includeEnd: true);
            float[] analysisTimes = FrameTimes(
                forward.length,
                forward.frameRate,
                includeEnd: false);
            UpperBodyCenterGeometry sourceCenter = MeasureUpperBodyCenterGeometry(
                forwardSource,
                forwardTarget,
                analysisTimes);
            float centerCorrectionDegrees = SolveUpperBodyCenterCorrectionDegrees(
                sourceCenter);
            Quaternion centerCorrection = Quaternion.AngleAxis(
                centerCorrectionDegrees,
                Vector3.forward);
            Quaternion leftArmDown = Quaternion.AngleAxis(
                ForwardLeftArmDownDegrees,
                Vector3.right);
            Dictionary<string, Quaternion[]> sourceForwardArmRotations =
                SampleTargetRelativeArmRotations(
                    forwardSource,
                    forwardTarget,
                    forwardTimes);
            float sourceReferenceTime = Mathf.Min(
                sourceMotionDuration,
                enterSource.length);
            Dictionary<string, Quaternion[]> desiredLeftArmRotations =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            foreach (string boneName in LeftArmBones)
            {
                Quaternion rawEnterReference = SampleTargetRelativeRotations(
                    enterSource,
                    enterTarget,
                    boneName,
                    new[] { sourceReferenceTime })[0];
                Quaternion desiredBaseMean = ApplyArmPoseOffset(
                    boneName,
                    rawEnterReference,
                    advanceForward: true);
                Quaternion sourceMean = QuaternionMean(
                    sourceForwardArmRotations[boneName]
                        .Take(sourceForwardArmRotations[boneName].Length - 1));
                Quaternion baseMeanOffset = desiredBaseMean *
                                            Quaternion.Inverse(sourceMean);
                desiredLeftArmRotations.Add(
                    boneName,
                    sourceForwardArmRotations[boneName]
                        .Select(rotation =>
                            leftArmDown *
                            centerCorrection *
                            baseMeanOffset *
                            rotation)
                        .ToArray());
            }

            Dictionary<string, Quaternion[]> desiredLeftArmLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    forward,
                    forwardTarget,
                    forwardTimes,
                    desiredLeftArmRotations,
                    LeftArmBones);
            AnimationClip correctedForward = UnityEngine.Object.Instantiate(forward);
            correctedForward.name = forward.name;
            correctedForward.hideFlags = HideFlags.None;
            foreach (string boneName in LeftArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    correctedForward,
                    BonePath(forwardTarget, boneName),
                    forwardTimes,
                    desiredLeftArmLocals[boneName]);
            }

            SaveOverExisting(
                correctedForward,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            UnityEngine.Object.DestroyImmediate(correctedForward);
            AssetDatabase.SaveAssets();
            enter = LoadClip(PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            idle = LoadClip(PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            Animator enterAnimator = enterTarget.GetComponent<Animator>() ??
                                     throw new InvalidOperationException(
                                         "Player_Crouch_Enter Animator is missing.");
            Animator idleAnimator = idleTarget.GetComponent<Animator>() ??
                                    throw new InvalidOperationException(
                                        "Player_Crouch_Idle Animator is missing.");
            Animator forwardAnimator = forwardTarget.GetComponent<Animator>() ??
                                       throw new InvalidOperationException(
                                           "Player_Crouch_Forward Animator is missing.");
            enterAnimator.Rebind();
            idleAnimator.Rebind();
            forwardAnimator.Rebind();

            Vector3 actualEnterWaist = SampleTargetRelativeDirections(
                enter,
                enterTarget,
                new[] { sourceMotionDuration })[0];
            Vector3 actualIdleWaist = SampleTargetRelativeDirections(
                idle,
                idleTarget,
                new[] { 0f })[0];
            Vector3 actualEnterHead = SampleTargetRelativeBoneForwardDirections(
                enter,
                enterTarget,
                "Head",
                new[] { sourceMotionDuration })[0];
            Vector3 actualIdleHead = SampleTargetRelativeBoneForwardDirections(
                idle,
                idleTarget,
                "Head",
                new[] { 0f })[0];
            Vector3 desiredEnterHead = DesiredHeadForwardDirection(
                actualEnterWaist,
                DesiredHeadDownDegrees);
            Vector3 desiredIdleHead = DesiredHeadForwardDirection(
                actualIdleWaist,
                DesiredHeadDownDegrees);
            Dictionary<string, Quaternion[]> forwardArmRotationsAfter =
                SampleTargetRelativeArmRotations(
                    forward,
                    forwardTarget,
                    forwardTimes);
            float leftArmMeanDifference = LeftArmBones.Max(boneName =>
                Quaternion.Angle(
                    QuaternionMean(
                        desiredLeftArmRotations[boneName]
                            .Take(desiredLeftArmRotations[boneName].Length - 1)),
                    QuaternionMean(
                        forwardArmRotationsAfter[boneName]
                            .Take(forwardArmRotationsAfter[boneName].Length - 1))));
            float leftArmSwingDifference = LeftArmBones.Max(boneName =>
                SwingDifference(
                    desiredLeftArmRotations[boneName],
                    forwardArmRotationsAfter[boneName]));
            UpperBodyCenterGeometry correctedCenter =
                MeasureUpperBodyCenterGeometry(
                    forward,
                    forwardTarget,
                    analysisTimes);

            HashSet<string> enterHeadPaths = new HashSet<string>(
                new[] { enterHeadPath },
                StringComparer.Ordinal);
            HashSet<string> idleHeadPaths = new HashSet<string>(
                new[] { idleHeadPath },
                StringComparer.Ordinal);
            HashSet<string> leftArmPaths = new HashSet<string>(
                LeftArmBones.Select(boneName => BonePath(forwardTarget, boneName)),
                StringComparer.Ordinal);
            HashSet<string> rightArmPaths = new HashSet<string>(
                RightArmBones.Select(boneName => BonePath(forwardTarget, boneName)),
                StringComparer.Ordinal);

            AlignmentApplyMetrics metrics = MeasureCurrentAlignment();
            metrics.waistReference =
                "Enter and Idle keep 80-degree waist; Forward keeps centered one-cycle upper-body mean";
            metrics.armReference =
                "Forward Left arm lowers 30 degrees; 45-degree advance and original per-frame swing remain";
            metrics.waistBindingsChanged = 0;
            metrics.armBindingsChanged = LeftArmBones.Length;
            metrics.headBindingsChanged = 2;
            metrics.enterBlendStartNormalized = EnterBlendStartNormalized;
            metrics.enterHeadDownDegrees = Vector3.Angle(
                TorsoForwardDirection(actualEnterWaist),
                actualEnterHead);
            metrics.idleHeadDownDegrees = Vector3.Angle(
                TorsoForwardDirection(actualIdleWaist),
                actualIdleHead);
            metrics.enterHeadAngleDifferenceDegreesMax = Vector3.Angle(
                actualEnterHead,
                desiredEnterHead);
            metrics.idleHeadAngleDifferenceDegreesMax = Vector3.Angle(
                actualIdleHead,
                desiredIdleHead);
            metrics.forwardArmMeanDifferenceDegreesMax =
                leftArmMeanDifference;
            metrics.forwardArmRangeDifferenceDegreesMax =
                leftArmSwingDifference;
            metrics.armTorsoClearanceDegrees = ArmTorsoClearanceDegrees;
            metrics.forwardArmAdvanceDegrees = ForwardArmAdvanceDegrees;
            metrics.forwardUpperBodyCenterCorrectionDegrees =
                centerCorrectionDegrees;
            metrics.forwardUpperBodyMeanLateralOffsetBefore =
                sourceCenter.MeanLateralOffset;
            metrics.forwardUpperBodyMeanLateralOffsetAfter =
                correctedCenter.MeanLateralOffset;
            metrics.rightArmAdditionalClearanceDegrees =
                RightArmAdditionalClearanceDegrees;
            metrics.forwardLeftArmDownDegrees = ForwardLeftArmDownDegrees;
            metrics.enterClipUnchanged = string.Equals(
                enterClipHashBefore,
                HashFile(PlayerCrouchEnterAnimationTool.CorrectedClipPath),
                StringComparison.Ordinal);
            metrics.idleClipUnchanged = string.Equals(
                idleClipHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.IdleClipPath),
                StringComparison.Ordinal);
            metrics.enterControllerUnchanged = string.Equals(
                enterControllerHashBefore,
                HashFile(PlayerCrouchEnterAnimationTool.ControllerPath),
                StringComparison.Ordinal);
            metrics.idleControllerUnchanged = string.Equals(
                idleControllerHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.IdleControllerPath),
                StringComparison.Ordinal);
            metrics.forwardControllerUnchanged = string.Equals(
                forwardControllerHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath),
                StringComparison.Ordinal);
            metrics.sceneAssetUnchanged = string.Equals(
                sceneHashBefore,
                HashFile(scene.path),
                StringComparison.Ordinal);
            metrics.enterNonHeadCurvesUnchanged = VerifyOutsidePathsUnchanged(
                enterBefore,
                enter,
                enterHeadPaths);
            metrics.idleNonHeadCurvesUnchanged = VerifyOutsidePathsUnchanged(
                idleBefore,
                idle,
                idleHeadPaths);
            metrics.enterNonWaistCurvesUnchanged =
                metrics.enterNonHeadCurvesUnchanged &&
                metrics.idleNonHeadCurvesUnchanged;
            metrics.forwardNonArmCurvesUnchanged = VerifyOutsidePathsUnchanged(
                forwardBefore,
                forward,
                leftArmPaths);
            metrics.leftArmCurvesUnchanged = VerifyPathsUnchanged(
                forwardBefore,
                forward,
                leftArmPaths);
            metrics.rightArmCurvesUnchanged = VerifyPathsUnchanged(
                forwardBefore,
                forward,
                rightArmPaths);
            metrics.armKeyTimingAndTangentsUnchanged = false;
            metrics.armFrameTimingUnchanged =
                forwardTimes.Length == forwardArmRotationsAfter[LeftArmBones[0]].Length;
            metrics.armPerFrameSwingPreserved =
                leftArmSwingDifference <=
                ForwardLeftArmSwingSerializationToleranceDegrees;
            metrics.idleMatchesEnterRuntimePose = RuntimePosesMatch(
                enter,
                enterTarget,
                sourceMotionDuration,
                idle,
                idleTarget,
                0f);
            metrics.sourceFbxFilesUnchanged =
                string.Equals(
                    enterSourceHashBefore,
                    HashFile(PlayerCrouchEnterAnimationTool.SourcePath),
                    StringComparison.Ordinal) &&
                string.Equals(
                    forwardSourceHashBefore,
                    HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath),
                    StringComparison.Ordinal);
            metrics.rootTransformsUnchanged = RootPosesEqual(
                rootsBefore,
                CaptureRootPoses(modifiedTargets));
            metrics.otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimators(scene, modifiedTargets));
            metrics.clipTimingUnchanged =
                Mathf.Abs(enter.length - enterDurationBefore) <= CurveTolerance &&
                Mathf.Abs(idle.length - idleDurationBefore) <= CurveTolerance &&
                Mathf.Abs(forward.length - forwardDurationBefore) <= CurveTolerance;
            metrics.applyRootMotion =
                enterAnimator.applyRootMotion ||
                idleAnimator.applyRootMotion ||
                forwardAnimator.applyRootMotion;
            metrics.passedNumericChecks =
                metrics.enterHeadAngleDifferenceDegreesMax <=
                    PoseAngleToleranceDegrees &&
                metrics.idleHeadAngleDifferenceDegreesMax <=
                    PoseAngleToleranceDegrees &&
                metrics.forwardArmMeanDifferenceDegreesMax <=
                    ArmAlignmentToleranceDegrees &&
                metrics.forwardArmRangeDifferenceDegreesMax <=
                    ForwardLeftArmSwingSerializationToleranceDegrees &&
                Mathf.Abs(metrics.forwardUpperBodyMeanLateralOffsetAfter) <=
                    UpperBodyCenterTolerance &&
                metrics.enterNonHeadCurvesUnchanged &&
                metrics.idleNonHeadCurvesUnchanged &&
                metrics.forwardNonArmCurvesUnchanged &&
                metrics.rightArmCurvesUnchanged &&
                metrics.enterControllerUnchanged &&
                metrics.idleControllerUnchanged &&
                metrics.forwardControllerUnchanged &&
                metrics.sceneAssetUnchanged &&
                metrics.armFrameTimingUnchanged &&
                metrics.armPerFrameSwingPreserved &&
                metrics.idleMatchesEnterRuntimePose &&
                metrics.sourceFbxFilesUnchanged &&
                metrics.rootTransformsUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.clipTimingUnchanged &&
                !metrics.applyRootMotion;
            WriteMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch left-arm and head support checks failed." +
                    " EnterHead=" + Num(
                        metrics.enterHeadAngleDifferenceDegreesMax) +
                    ", IdleHead=" + Num(
                        metrics.idleHeadAngleDifferenceDegreesMax) +
                    ", LeftArm=" + Num(
                        metrics.forwardArmMeanDifferenceDegreesMax) +
                    ", ArmSwing=" + Num(
                        metrics.forwardArmRangeDifferenceDegreesMax) +
                    ", RightArmUnchanged=" + metrics.rightArmCurvesUnchanged +
                    ", EnterNonHeadUnchanged=" +
                    metrics.enterNonHeadCurvesUnchanged +
                    ", IdleNonHeadUnchanged=" +
                    metrics.idleNonHeadCurvesUnchanged +
                    ", ControllersUnchanged=" +
                    (metrics.enterControllerUnchanged &&
                     metrics.idleControllerUnchanged &&
                     metrics.forwardControllerUnchanged) +
                    ", SceneUnchanged=" + metrics.sceneAssetUnchanged + ".");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp became dirty during crouch left-arm and head correction.");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Applied Forward left-arm and Enter/Idle head correction." +
                " ForwardLeftArmDownDegrees=" + Num(
                    ForwardLeftArmDownDegrees) +
                ", ForwardArmAdvanceDegrees=" + Num(
                    ForwardArmAdvanceDegrees) +
                ", EnterHeadDownDegrees=" + Num(
                    metrics.enterHeadDownDegrees) +
                ", IdleHeadDownDegrees=" + Num(
                    metrics.idleHeadDownDegrees) +
                ", ForwardUpperBodyLateral=" + Num(
                    metrics.forwardUpperBodyMeanLateralOffsetAfter) +
                ", RightArmChanged=False, NonHeadChanged=False" +
                ", ControllersChanged=False, SceneChanged=False" +
                ", TimingChanged=False, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Forward Left Arm Straight Down")]
        internal static void ApplyForwardLeftArmStraightDown()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the Forward left-arm correction.");
            }

            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            Transform[] modifiedTargets = { forwardTarget };
            Dictionary<string, RootPose> rootsBefore = CaptureRootPoses(
                modifiedTargets);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimators(scene, modifiedTargets);
            string enterClipHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            string idleClipHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            string forwardControllerHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath);
            string sceneHashBefore = HashFile(scene.path);
            string sourceHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath);

            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            float durationBefore = forward.length;
            Dictionary<EditorCurveBinding, AnimationCurve> curvesBefore =
                CaptureCurves(forward);
            float[] times = FrameTimes(
                forward.length,
                forward.frameRate,
                includeEnd: true);
            Dictionary<string, Quaternion[]> armRotationsBefore =
                SampleTargetRelativeArmRotations(
                    forward,
                    forwardTarget,
                    times);
            LeftArmGeometrySamples geometryBefore = SampleLeftArmGeometry(
                forward,
                forwardTarget,
                times);
            float bendBefore = LeftElbowBendMaximum(geometryBefore);
            float downwardMeanBefore = LeftArmDownwardMeanAngle(geometryBefore);
            float gapBefore = LeftHandKneeMinimumSideGap(geometryBefore);

            Quaternion[] straightForeArmRotations =
                StraightenedLeftForeArmTargetRotations(
                    armRotationsBefore["LeftForeArm"],
                    geometryBefore);
            Quaternion downwardCorrection = Quaternion.FromToRotation(
                DirectionMean(
                    geometryBefore.UpperArmDirections.Take(
                        geometryBefore.UpperArmDirections.Length - 1)),
                Vector3.down);
            Dictionary<string, Quaternion[]> downwardArmRotations =
                LeftArmBones.ToDictionary(
                    boneName => boneName,
                    boneName => (boneName == "LeftForeArm"
                            ? straightForeArmRotations
                            : armRotationsBefore[boneName])
                        .Select(rotation => downwardCorrection * rotation)
                        .ToArray(),
                    StringComparer.Ordinal);

            float clearanceDegrees = FindLeftArmClearanceDegrees(
                forward,
                forwardTarget,
                times,
                downwardArmRotations,
                geometryBefore);
            Quaternion clearanceCorrection = Quaternion.AngleAxis(
                clearanceDegrees,
                Vector3.forward);
            Dictionary<string, Quaternion[]> desiredArmRotations =
                LeftArmBones.ToDictionary(
                    boneName => boneName,
                    boneName => downwardArmRotations[boneName]
                        .Select(rotation => clearanceCorrection * rotation)
                        .ToArray(),
                    StringComparer.Ordinal);
            AnimationClip correctedForward = CreateLeftArmCorrectedClip(
                forward,
                forwardTarget,
                times,
                desiredArmRotations);
            SaveOverExisting(
                correctedForward,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            UnityEngine.Object.DestroyImmediate(correctedForward);
            AssetDatabase.SaveAssets();

            forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            Animator forwardAnimator = forwardTarget.GetComponent<Animator>() ??
                                       throw new InvalidOperationException(
                                           "Player_Crouch_Forward Animator is missing.");
            forwardAnimator.Rebind();
            Dictionary<string, Quaternion[]> armRotationsAfter =
                SampleTargetRelativeArmRotations(
                    forward,
                    forwardTarget,
                    times);
            LeftArmGeometrySamples geometryAfter = SampleLeftArmGeometry(
                forward,
                forwardTarget,
                times);
            HashSet<string> leftArmPaths = new HashSet<string>(
                LeftArmBones.Select(boneName => BonePath(forwardTarget, boneName)),
                StringComparer.Ordinal);
            HashSet<string> rightArmPaths = new HashSet<string>(
                RightArmBones.Select(boneName => BonePath(forwardTarget, boneName)),
                StringComparer.Ordinal);
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(forward);

            ForwardLeftArmStraightDownApplyMetrics metrics =
                new ForwardLeftArmStraightDownApplyMetrics
                {
                    target = forwardTarget.name,
                    clipPath =
                        PlayerCrouchIdleForwardAnimationTool.ForwardClipPath,
                    leftElbowBendDegreesMaxBefore = bendBefore,
                    leftElbowBendDegreesMaxAfter =
                        LeftElbowBendMaximum(geometryAfter),
                    leftArmDownwardMeanAngleDegreesBefore = downwardMeanBefore,
                    leftArmDownwardMeanAngleDegreesAfter =
                        LeftArmDownwardMeanAngle(geometryAfter),
                    leftArmDownwardMaximumAngleDegreesAfter =
                        LeftArmDownwardMaximumAngle(geometryAfter),
                    leftHandKneeMinimumBoneGapTarget =
                        ForwardLeftHandKneeMinimumGap,
                    leftHandKneeMinimumBoneGapBefore = gapBefore,
                    leftHandKneeMinimumBoneGapAfter =
                        LeftHandKneeMinimumSideGap(geometryAfter),
                    leftArmClearanceAdjustmentDegrees = clearanceDegrees,
                    leftShoulderSwingDifferenceDegreesMax = SwingDifference(
                        armRotationsBefore["LeftShoulder"],
                        armRotationsAfter["LeftShoulder"]),
                    leftUpperArmSwingDifferenceDegreesMax = SwingDifference(
                        armRotationsBefore["LeftArm"],
                        armRotationsAfter["LeftArm"]),
                    curvesOutsideLeftArmUnchanged = VerifyOutsidePathsUnchanged(
                        curvesBefore,
                        forward,
                        leftArmPaths),
                    rightArmCurvesUnchanged = VerifyPathsUnchanged(
                        curvesBefore,
                        forward,
                        rightArmPaths),
                    referenceClipsUnchanged =
                        string.Equals(
                            enterClipHashBefore,
                            HashFile(PlayerCrouchEnterAnimationTool.CorrectedClipPath),
                            StringComparison.Ordinal) &&
                        string.Equals(
                            idleClipHashBefore,
                            HashFile(PlayerCrouchIdleForwardAnimationTool.IdleClipPath),
                            StringComparison.Ordinal),
                    forwardControllerUnchanged = string.Equals(
                        forwardControllerHashBefore,
                        HashFile(
                            PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath),
                        StringComparison.Ordinal),
                    sceneAssetUnchanged = string.Equals(
                        sceneHashBefore,
                        HashFile(scene.path),
                        StringComparison.Ordinal),
                    sourceFbxFileUnchanged = string.Equals(
                        sourceHashBefore,
                        HashFile(
                            PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath),
                        StringComparison.Ordinal),
                    otherAnimatorsUnchanged = DictionariesEqual(
                        otherAnimatorsBefore,
                        CaptureOtherAnimators(scene, modifiedTargets)),
                    rootTransformUnchanged = RootPosesEqual(
                        rootsBefore,
                        CaptureRootPoses(modifiedTargets)),
                    leftArmFrameTimingUnchanged =
                        times.Length ==
                        armRotationsAfter["LeftShoulder"].Length,
                    clipTimingUnchanged =
                        Mathf.Abs(forward.length - durationBefore) <=
                        CurveTolerance,
                    clipIsLooping = settings.loopTime && !settings.loopBlend,
                    applyRootMotion = forwardAnimator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.leftElbowBendDegreesMaxAfter <=
                    ForwardLeftElbowStraightToleranceDegrees &&
                metrics.leftArmDownwardMeanAngleDegreesAfter <=
                    ForwardLeftArmDownwardMeanToleranceDegrees &&
                metrics.leftArmDownwardMaximumAngleDegreesAfter <=
                    ForwardLeftArmDownwardMaximumToleranceDegrees &&
                metrics.leftHandKneeMinimumBoneGapAfter >=
                    metrics.leftHandKneeMinimumBoneGapTarget -
                    ForwardLeftHandKneeGapTolerance &&
                metrics.leftShoulderSwingDifferenceDegreesMax <=
                    ForwardLeftArmSwingSerializationToleranceDegrees &&
                metrics.leftUpperArmSwingDifferenceDegreesMax <=
                    ForwardLeftArmSwingSerializationToleranceDegrees &&
                metrics.curvesOutsideLeftArmUnchanged &&
                metrics.rightArmCurvesUnchanged &&
                metrics.referenceClipsUnchanged &&
                metrics.forwardControllerUnchanged &&
                metrics.sceneAssetUnchanged &&
                metrics.sourceFbxFileUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.rootTransformUnchanged &&
                metrics.leftArmFrameTimingUnchanged &&
                metrics.clipTimingUnchanged &&
                metrics.clipIsLooping &&
                !metrics.applyRootMotion;
            WriteForwardLeftArmStraightDownMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Forward left-arm support checks failed." +
                    " Elbow=" + Num(metrics.leftElbowBendDegreesMaxAfter) +
                    ", DownMean=" + Num(
                        metrics.leftArmDownwardMeanAngleDegreesAfter) +
                    ", DownMax=" + Num(
                        metrics.leftArmDownwardMaximumAngleDegreesAfter) +
                    ", KneeGap=" + Num(
                        metrics.leftHandKneeMinimumBoneGapAfter) +
                    ", OutsideLeftUnchanged=" +
                    metrics.curvesOutsideLeftArmUnchanged +
                    ", RightArmUnchanged=" +
                    metrics.rightArmCurvesUnchanged + ".");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp became dirty during the Forward left-arm correction.");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Straightened and lowered the Forward left arm." +
                " ElbowBefore=" + Num(bendBefore) +
                ", ElbowAfter=" + Num(
                    metrics.leftElbowBendDegreesMaxAfter) +
                ", DownMeanAfter=" + Num(
                    metrics.leftArmDownwardMeanAngleDegreesAfter) +
                ", KneeGapAfter=" + Num(
                    metrics.leftHandKneeMinimumBoneGapAfter) +
                ", ClearanceDegrees=" + Num(clearanceDegrees) +
                ", RightArmChanged=False, OutsideLeftArmChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Pose Alignment")]
        internal static void ApplyQuaternionAlignment()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch pose alignment.");
            }

            Transform enterTarget = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Transform idleTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            Transform[] targets = { enterTarget, idleTarget, forwardTarget };
            Dictionary<string, RootPose> rootsBefore = CaptureRootPoses(targets);
            Dictionary<string, string> otherAnimatorsBefore = CaptureOtherAnimators(
                scene,
                targets);
            string enterSourceHashBefore = HashFile(
                PlayerCrouchEnterAnimationTool.SourcePath);
            string forwardSourceHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath);

            AnimationClip enter = LoadClip(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            AnimationClip enterSource =
                PlayerCrouchEnterAnimationTool.LoadSingleSourceClip();
            float enterDurationBefore = enter.length;
            float forwardDurationBefore = forward.length;
            Dictionary<EditorCurveBinding, AnimationCurve> enterBefore =
                CaptureCurves(enter);
            Dictionary<EditorCurveBinding, AnimationCurve> forwardBefore =
                CaptureCurves(forward);
            float sourceMotionDuration = enter.length -
                                         PlayerCrouchEnterAnimationTool
                                             .HoldDurationSeconds;
            float[] enterAllTimes = FrameTimes(
                enter.length,
                enter.frameRate,
                includeEnd: true);
            float[] forwardTimes = FrameTimes(
                forward.length,
                forward.frameRate,
                includeEnd: true);
            Dictionary<string, Quaternion[]> forwardArmRotationsBefore =
                SampleTargetRelativeArmRotations(
                    forward,
                    forwardTarget,
                    forwardTimes);

            Vector3 currentWaistDirection = SampleTargetRelativeDirections(
                enter,
                enterTarget,
                new[] { sourceMotionDuration })[0];
            Vector3 desiredWaistDirection = DirectionAtGroundAngle(
                currentWaistDirection,
                DesiredWaistAngleDegrees);
            Quaternion spine02Correction = CalculateLowerSpineDirectionCorrection(
                enter,
                enterTarget,
                sourceMotionDuration,
                desiredWaistDirection);
            Quaternion[] enterLowerSpineSamples = SampleLocalRotations(
                enter,
                enterTarget,
                "Spine02",
                enterAllTimes);
            Quaternion[] alignedLowerSpineSamples = new Quaternion[
                enterLowerSpineSamples.Length];
            float blendStart = sourceMotionDuration * EnterBlendStartNormalized;
            for (int index = 0; index < enterAllTimes.Length; index++)
            {
                float normalized = Mathf.InverseLerp(
                    blendStart,
                    sourceMotionDuration,
                    enterAllTimes[index]);
                float weight = normalized * normalized * (3f - 2f * normalized);
                alignedLowerSpineSamples[index] =
                    Quaternion.Slerp(Quaternion.identity, spine02Correction, weight) *
                    enterLowerSpineSamples[index];
            }

            AnimationClip alignedEnter = UnityEngine.Object.Instantiate(enter);
            alignedEnter.name = enter.name;
            alignedEnter.hideFlags = HideFlags.None;
            string enterLowerSpinePath = BonePath(enterTarget, "Spine02");
            ReplaceRotationWithQuaternionCurves(
                alignedEnter,
                enterLowerSpinePath,
                enterAllTimes,
                alignedLowerSpineSamples);
            SaveOverExisting(
                alignedEnter,
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            UnityEngine.Object.DestroyImmediate(alignedEnter);
            enter = LoadClip(PlayerCrouchEnterAnimationTool.CorrectedClipPath);

            Vector3 desiredHeadForward = DesiredHeadForwardDirection(
                desiredWaistDirection,
                DesiredHeadDownDegrees);
            Quaternion headCorrection = CalculateBoneForwardDirectionCorrection(
                enter,
                enterTarget,
                "Head",
                sourceMotionDuration,
                desiredHeadForward);
            Quaternion[] enterHeadSamples = SampleLocalRotations(
                enter,
                enterTarget,
                "Head",
                enterAllTimes);
            Quaternion[] alignedHeadSamples = new Quaternion[
                enterHeadSamples.Length];
            for (int index = 0; index < enterAllTimes.Length; index++)
            {
                float normalized = Mathf.InverseLerp(
                    blendStart,
                    sourceMotionDuration,
                    enterAllTimes[index]);
                float weight = normalized * normalized * (3f - 2f * normalized);
                alignedHeadSamples[index] =
                    Quaternion.Slerp(Quaternion.identity, headCorrection, weight) *
                    enterHeadSamples[index];
            }

            AnimationClip headAlignedEnter = UnityEngine.Object.Instantiate(enter);
            headAlignedEnter.name = enter.name;
            headAlignedEnter.hideFlags = HideFlags.None;
            string enterHeadPath = BonePath(enterTarget, "Head");
            ReplaceRotationWithQuaternionCurves(
                headAlignedEnter,
                enterHeadPath,
                enterAllTimes,
                alignedHeadSamples);
            SaveOverExisting(
                headAlignedEnter,
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            UnityEngine.Object.DestroyImmediate(headAlignedEnter);
            enter = LoadClip(PlayerCrouchEnterAnimationTool.CorrectedClipPath);

            float sourceReferenceTime = Mathf.Min(
                sourceMotionDuration,
                enterSource.length);
            float[] sourceArmTimes = enterAllTimes
                .Select(time => Mathf.Min(time, sourceReferenceTime))
                .ToArray();
            Dictionary<string, Quaternion[]> sourceArmRotations =
                SampleTargetRelativeArmRotations(
                    enterSource,
                    enterTarget,
                    sourceArmTimes);
            Dictionary<string, Quaternion[]> clearedEnterArmRotations =
                OffsetArmRotations(
                    sourceArmRotations,
                    advanceForward: false);
            Dictionary<string, Quaternion[]> restoredEnterArmLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    enter,
                    enterTarget,
                    enterAllTimes,
                    clearedEnterArmRotations);
            AnimationClip armRestoredEnter = UnityEngine.Object.Instantiate(enter);
            armRestoredEnter.name = enter.name;
            armRestoredEnter.hideFlags = HideFlags.None;
            foreach (string boneName in ArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    armRestoredEnter,
                    BonePath(enterTarget, boneName),
                    enterAllTimes,
                    restoredEnterArmLocals[boneName]);
            }

            SaveOverExisting(
                armRestoredEnter,
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            UnityEngine.Object.DestroyImmediate(armRestoredEnter);
            enter = LoadClip(PlayerCrouchEnterAnimationTool.CorrectedClipPath);

            TransformSnapshot[] idlePose = SampleTransformSnapshot(
                enter,
                enterTarget,
                sourceMotionDuration);
            AnimationClip idle = CreateStaticIdleClip(
                idlePose,
                enter.frameRate);
            SaveOverExisting(
                idle,
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            UnityEngine.Object.DestroyImmediate(idle);
            idle = LoadClip(PlayerCrouchIdleForwardAnimationTool.IdleClipPath);

            Dictionary<string, Quaternion[]> desiredForwardArmRotations =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            foreach (string boneName in ArmBones)
            {
                Quaternion sourceFinal = ApplyArmPoseOffset(
                    boneName,
                    sourceArmRotations[boneName][
                        sourceArmRotations[boneName].Length - 1],
                    advanceForward: true);
                Quaternion currentMean = QuaternionMean(
                    forwardArmRotationsBefore[boneName]
                        .Take(forwardArmRotationsBefore[boneName].Length - 1));
                Quaternion meanOffset = sourceFinal *
                                        Quaternion.Inverse(currentMean);
                desiredForwardArmRotations.Add(
                    boneName,
                    forwardArmRotationsBefore[boneName]
                        .Select(rotation => meanOffset * rotation)
                        .ToArray());
            }

            Dictionary<string, Quaternion[]> alignedForwardArmLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    forward,
                    forwardTarget,
                    forwardTimes,
                    desiredForwardArmRotations);
            AnimationClip alignedForward = UnityEngine.Object.Instantiate(forward);
            alignedForward.name = forward.name;
            alignedForward.hideFlags = HideFlags.None;
            foreach (string boneName in ArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    alignedForward,
                    BonePath(forwardTarget, boneName),
                    forwardTimes,
                    alignedForwardArmLocals[boneName]);
            }

            SaveOverExisting(
                alignedForward,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            UnityEngine.Object.DestroyImmediate(alignedForward);
            AssetDatabase.SaveAssets();
            enter = LoadClip(PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            idle = LoadClip(PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            AnimatorController enterController = ConfigureControllerBinding(
                PlayerCrouchEnterAnimationTool.ControllerPath,
                PlayerCrouchEnterAnimationTool.StateName,
                enter,
                1f,
                0f);
            AnimatorController idleController = ConfigureControllerBinding(
                PlayerCrouchIdleForwardAnimationTool.IdleControllerPath,
                PlayerCrouchIdleForwardAnimationTool.IdleStateName,
                idle,
                1f,
                0f);
            RebindAnimator(enterTarget, enterController);
            RebindAnimator(idleTarget, idleController);
            EditorSceneManager.SaveScene(scene);

            Dictionary<string, Quaternion[]> forwardArmRotationsAfter =
                SampleTargetRelativeArmRotations(
                    forward,
                    forwardTarget,
                    forwardTimes);
            float forwardArmSwingDifference = ArmBones.Max(boneName =>
                SwingDifference(
                    forwardArmRotationsBefore[boneName],
                    forwardArmRotationsAfter[boneName]));

            AlignmentApplyMetrics metrics = MeasureCurrentAlignment();
            metrics.waistBindingsChanged = 2;
            metrics.armBindingsChanged = ArmBones.Length * 2;
            metrics.enterBlendStartNormalized = EnterBlendStartNormalized;
            metrics.armTorsoClearanceDegrees = ArmTorsoClearanceDegrees;
            metrics.forwardArmAdvanceDegrees = ForwardArmAdvanceDegrees;
            metrics.forwardArmRangeDifferenceDegreesMax =
                forwardArmSwingDifference;
            HashSet<string> enterChangedPaths = new HashSet<string>(
                new[] { enterLowerSpinePath, enterHeadPath },
                StringComparer.Ordinal);
            foreach (string boneName in ArmBones)
            {
                enterChangedPaths.Add(BonePath(enterTarget, boneName));
            }

            metrics.enterNonWaistCurvesUnchanged = VerifyOutsidePathsUnchanged(
                enterBefore,
                enter,
                enterChangedPaths);
            HashSet<string> forwardArmPaths = new HashSet<string>(
                ArmBones.Select(boneName => BonePath(forwardTarget, boneName)),
                StringComparer.Ordinal);
            metrics.forwardNonArmCurvesUnchanged = VerifyOutsidePathsUnchanged(
                forwardBefore,
                forward,
                forwardArmPaths);
            metrics.armKeyTimingAndTangentsUnchanged = false;
            metrics.armFrameTimingUnchanged =
                forwardTimes.Length == forwardArmRotationsAfter[ArmBones[0]].Length;
            metrics.armPerFrameSwingPreserved =
                forwardArmSwingDifference <=
                SwingPreservationToleranceDegrees;
            metrics.idleMatchesEnterRuntimePose = RuntimePosesMatch(
                enter,
                enterTarget,
                sourceMotionDuration,
                idle,
                idleTarget,
                0f);
            metrics.sourceFbxFilesUnchanged =
                string.Equals(
                    enterSourceHashBefore,
                    HashFile(PlayerCrouchEnterAnimationTool.SourcePath),
                    StringComparison.Ordinal) &&
                string.Equals(
                    forwardSourceHashBefore,
                    HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath),
                    StringComparison.Ordinal);
            metrics.rootTransformsUnchanged = RootPosesEqual(
                rootsBefore,
                CaptureRootPoses(targets));
            metrics.otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimators(scene, targets));
            metrics.clipTimingUnchanged =
                Mathf.Abs(enter.length - enterDurationBefore) <= CurveTolerance &&
                Mathf.Abs(forward.length - forwardDurationBefore) <= CurveTolerance &&
                Mathf.Abs(
                    idle.length -
                    PlayerCrouchIdleForwardAnimationTool.IdleDurationSeconds) <=
                CurveTolerance;
            metrics.applyRootMotion = targets
                .Select(target => target.GetComponent<Animator>())
                .Any(animator => animator == null || animator.applyRootMotion);
            metrics.passedNumericChecks =
                metrics.enterWaistMeanDifferenceDegreesMax <=
                    PoseAngleToleranceDegrees &&
                metrics.idleWaistMeanDifferenceDegreesMax <=
                    PoseAngleToleranceDegrees &&
                metrics.enterIdleWaistDifferenceDegreesMax <=
                    PoseAngleToleranceDegrees &&
                metrics.enterHeadAngleDifferenceDegreesMax <=
                    PoseAngleToleranceDegrees &&
                metrics.idleHeadAngleDifferenceDegreesMax <=
                    PoseAngleToleranceDegrees &&
                metrics.enterArmSourceDifferenceDegreesMax <=
                    ArmAlignmentToleranceDegrees &&
                metrics.idleArmSourceDifferenceDegreesMax <=
                    ArmAlignmentToleranceDegrees &&
                metrics.forwardArmMeanDifferenceDegreesMax <=
                    ArmAlignmentToleranceDegrees &&
                metrics.forwardArmRangeDifferenceDegreesMax <=
                    SwingPreservationToleranceDegrees &&
                metrics.enterNonWaistCurvesUnchanged &&
                metrics.forwardNonArmCurvesUnchanged &&
                metrics.armFrameTimingUnchanged &&
                metrics.armPerFrameSwingPreserved &&
                metrics.idleMatchesEnterRuntimePose &&
                metrics.sourceFbxFilesUnchanged &&
                metrics.rootTransformsUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.clipTimingUnchanged &&
                !metrics.applyRootMotion;
            WriteMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Quaternion crouch pose alignment support checks failed." +
                    " EnterWaist=" + Num(
                        metrics.enterWaistMeanDifferenceDegreesMax) +
                    ", IdleWaist=" + Num(
                        metrics.idleWaistMeanDifferenceDegreesMax) +
                    ", EnterHead=" + Num(
                        metrics.enterHeadAngleDifferenceDegreesMax) +
                    ", IdleHead=" + Num(
                        metrics.idleHeadAngleDifferenceDegreesMax) +
                    ", EnterArms=" + Num(
                        metrics.enterArmSourceDifferenceDegreesMax) +
                    ", IdleArms=" + Num(
                        metrics.idleArmSourceDifferenceDegreesMax) +
                    ", ForwardArms=" + Num(
                        metrics.forwardArmMeanDifferenceDegreesMax) +
                    ", ArmSwing=" + Num(
                        metrics.forwardArmRangeDifferenceDegreesMax) +
                    ", IdleRuntimeMatch=" +
                    metrics.idleMatchesEnterRuntimePose + ".");
            }

            Debug.Log(
                "[PlayerCrouchPoseAlignment] Applied approved crouch pose alignment." +
                " WaistGroundAngle=" + Num(DesiredWaistAngleDegrees) +
                ", HeadDownFromTorso=" + Num(DesiredHeadDownDegrees) +
                ", EnterBlendStartNormalized=" + Num(EnterBlendStartNormalized) +
                ", WaistBone=Spine02, HeadBone=Head" +
                ", EnterWaistActual=" + Num(metrics.enterWaistAngleDegrees) +
                ", IdleWaistActual=" + Num(metrics.idleWaistAngleDegrees) +
                ", EnterHeadDownActual=" + Num(metrics.enterHeadDownDegrees) +
                ", IdleHeadDownActual=" + Num(metrics.idleHeadDownDegrees) +
                ", EnterArmSourceDifference=" + Num(
                    metrics.enterArmSourceDifferenceDegreesMax) +
                ", IdleArmSourceDifference=" + Num(
                    metrics.idleArmSourceDifferenceDegreesMax) +
                ", ForwardArmMeanDifference=" + Num(
                    metrics.forwardArmMeanDifferenceDegreesMax) +
                ", ForwardArmSwingDifference=" + Num(
                    metrics.forwardArmRangeDifferenceDegreesMax) +
                ", ArmTorsoClearanceDegrees=" + Num(
                    ArmTorsoClearanceDegrees) +
                ", ForwardArmAdvanceDegrees=" + Num(
                    ForwardArmAdvanceDegrees) +
                ", IdleRuntimePoseMatch=True, IdleStaticClip=True" +
                ", ForwardArmMeanChanged=True, ForwardSwingPreserved=True" +
                ", TimingChanged=False" +
                ", ApplyRootMotion=False.");
        }

        internal static AlignmentApplyMetrics MeasureCurrentAlignment()
        {
            Scene scene = RequireScene();
            Transform enterTarget = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Transform idleTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform forwardTarget = PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            AnimationClip enterSource =
                PlayerCrouchEnterAnimationTool.LoadSingleSourceClip();
            AnimationClip enter = LoadClip(
                PlayerCrouchEnterAnimationTool.CorrectedClipPath);
            AnimationClip idle = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            float enterEnd = enter.length -
                             PlayerCrouchEnterAnimationTool.HoldDurationSeconds;
            Vector3 enterWaist = SampleTargetRelativeDirections(
                enter,
                enterTarget,
                new[] { enterEnd })[0];
            Vector3 idleWaist = SampleTargetRelativeDirections(
                idle,
                idleTarget,
                new[] { 0f })[0];
            Vector3 enterHeadForward = SampleTargetRelativeBoneForwardDirections(
                enter,
                enterTarget,
                "Head",
                new[] { enterEnd })[0];
            Vector3 idleHeadForward = SampleTargetRelativeBoneForwardDirections(
                idle,
                idleTarget,
                "Head",
                new[] { 0f })[0];
            float enterWaistAngle = GroundAngleDegrees(enterWaist);
            float idleWaistAngle = GroundAngleDegrees(idleWaist);
            Vector3 desiredEnterHead = DesiredHeadForwardDirection(
                enterWaist,
                DesiredHeadDownDegrees);
            Vector3 desiredIdleHead = DesiredHeadForwardDirection(
                idleWaist,
                DesiredHeadDownDegrees);
            float sourceReferenceTime = Mathf.Min(enterEnd, enterSource.length);
            float[] forwardTimes = FrameTimes(
                forward.length,
                forward.frameRate,
                includeEnd: false);
            float enterArmSourceDifference = 0f;
            float idleArmSourceDifference = 0f;
            float forwardArmMeanDifference = 0f;
            foreach (string boneName in ArmBones)
            {
                Quaternion rawSourceReference = SampleTargetRelativeRotations(
                    enterSource,
                    enterTarget,
                    boneName,
                    new[] { sourceReferenceTime })[0];
                Quaternion enterReference = ApplyArmPoseOffset(
                    boneName,
                    rawSourceReference,
                    advanceForward: false);
                Quaternion forwardReference = ApplyArmPoseOffset(
                    boneName,
                    rawSourceReference,
                    advanceForward: true);
                Quaternion enterArm = SampleTargetRelativeRotations(
                    enter,
                    enterTarget,
                    boneName,
                    new[] { enterEnd })[0];
                Quaternion idleArm = SampleTargetRelativeRotations(
                    idle,
                    idleTarget,
                    boneName,
                    new[] { 0f })[0];
                Quaternion forwardMean = QuaternionMean(
                    SampleTargetRelativeRotations(
                        forward,
                        forwardTarget,
                        boneName,
                        forwardTimes));
                enterArmSourceDifference = Mathf.Max(
                    enterArmSourceDifference,
                    Quaternion.Angle(enterReference, enterArm));
                idleArmSourceDifference = Mathf.Max(
                    idleArmSourceDifference,
                    Quaternion.Angle(enterReference, idleArm));
                forwardArmMeanDifference = Mathf.Max(
                    forwardArmMeanDifference,
                    Quaternion.Angle(forwardReference, forwardMean));
            }

            return new AlignmentApplyMetrics
            {
                waistReference =
                    "Target-relative Spine02-to-Spine axis at 80 degrees above the ground plane",
                armReference =
                    "Original crouch arm pose with bilateral 12-degree torso clearance; Forward advances to just before the knees and preserves per-frame swing",
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
                enterArmSourceDifferenceDegreesMax =
                    enterArmSourceDifference,
                idleArmSourceDifferenceDegreesMax =
                    idleArmSourceDifference,
                forwardArmMeanDifferenceDegreesMax =
                    forwardArmMeanDifference,
                armTorsoClearanceDegrees = ArmTorsoClearanceDegrees,
                forwardArmAdvanceDegrees = ForwardArmAdvanceDegrees,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
        }

        private static float[] FrameTimes(
            float duration,
            float frameRate,
            bool includeEnd)
        {
            int frames = Mathf.RoundToInt(duration * frameRate);
            int count = frames + (includeEnd ? 1 : 0);
            return Enumerable.Range(0, count)
                .Select(frame => Mathf.Min(frame / frameRate, duration))
                .ToArray();
        }

        private static string BonePath(Transform target, string boneName)
        {
            return AnimationUtility.CalculateTransformPath(
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    boneName),
                target);
        }

        private static Quaternion[] SampleLocalRotations(
            AnimationClip clip,
            Transform target,
            string boneName,
            float[] times)
        {
            Transform bone = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                boneName);
            return SampleRotations(
                clip,
                target,
                times,
                () => bone.localRotation);
        }

        private static Quaternion[] SampleTargetRelativeRotations(
            AnimationClip clip,
            Transform target,
            string boneName,
            float[] times)
        {
            Transform bone = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                boneName);
            return SampleRotations(
                clip,
                target,
                times,
                () => Quaternion.Inverse(target.rotation) * bone.rotation);
        }

        private static Dictionary<string, Quaternion[]>
            SampleTargetRelativeArmRotations(
                AnimationClip clip,
                Transform target,
                float[] times)
        {
            return ArmBones.ToDictionary(
                boneName => boneName,
                boneName => SampleTargetRelativeRotations(
                    clip,
                    target,
                    boneName,
                    times),
                StringComparer.Ordinal);
        }

        private static Dictionary<string, Quaternion[]> OffsetArmRotations(
            Dictionary<string, Quaternion[]> source,
            bool advanceForward)
        {
            return ArmBones.ToDictionary(
                boneName => boneName,
                boneName => source[boneName]
                    .Select(rotation => ApplyArmPoseOffset(
                        boneName,
                        rotation,
                        advanceForward))
                    .ToArray(),
                StringComparer.Ordinal);
        }

        private static Quaternion ApplyArmPoseOffset(
            string boneName,
            Quaternion rotation,
            bool advanceForward)
        {
            float outwardDegrees = boneName.StartsWith(
                "Left",
                StringComparison.Ordinal)
                ? -ArmTorsoClearanceDegrees
                : ArmTorsoClearanceDegrees;
            Quaternion outward = Quaternion.AngleAxis(
                outwardDegrees,
                Vector3.forward);
            Quaternion advance = advanceForward
                ? Quaternion.AngleAxis(
                    -ForwardArmAdvanceDegrees,
                    Vector3.right)
                : Quaternion.identity;
            return advance * outward * rotation;
        }

        private static Dictionary<string, Quaternion[]>
            ConvertTargetRelativeArmRotationsToLocal(
                AnimationClip baseClip,
                Transform target,
                float[] times,
                Dictionary<string, Quaternion[]> desiredTargetRelative)
        {
            return ConvertTargetRelativeArmRotationsToLocal(
                baseClip,
                target,
                times,
                desiredTargetRelative,
                ArmBones);
        }

        private static Dictionary<string, Quaternion[]>
            ConvertTargetRelativeArmRotationsToLocal(
                AnimationClip baseClip,
                Transform target,
                float[] times,
                Dictionary<string, Quaternion[]> desiredTargetRelative,
                IEnumerable<string> boneNames)
        {
            Quaternion[] spineRotations = SampleTargetRelativeRotations(
                baseClip,
                target,
                "Spine",
                times);
            Dictionary<string, Quaternion[]> localRotations =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            foreach (string boneName in boneNames)
            {
                Quaternion[] desired = desiredTargetRelative[boneName];
                if (desired.Length != times.Length)
                {
                    throw new InvalidOperationException(
                        boneName + " arm rotation sample counts differ.");
                }

                string parentBoneName = ArmParentBoneName(boneName);
                Quaternion[] parentRotations = parentBoneName == null
                    ? spineRotations
                    : desiredTargetRelative[parentBoneName];
                Quaternion[] locals = new Quaternion[times.Length];
                for (int index = 0; index < times.Length; index++)
                {
                    locals[index] = Quaternion.Inverse(parentRotations[index]) *
                                    desired[index];
                }

                localRotations.Add(boneName, locals);
            }

            return localRotations;
        }

        private static Quaternion[] ConvertTargetRelativeRotationToLocal(
            AnimationClip baseClip,
            Transform target,
            string boneName,
            float[] times,
            Quaternion[] desiredTargetRelative)
        {
            if (desiredTargetRelative.Length != times.Length)
            {
                throw new InvalidOperationException(
                    boneName + " rotation sample counts differ.");
            }

            Transform bone = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                boneName);
            Transform parent = bone.parent ??
                               throw new InvalidOperationException(
                                   boneName + " parent is missing.");
            Quaternion[] parentRotations = SampleRotations(
                baseClip,
                target,
                times,
                () => Quaternion.Inverse(target.rotation) * parent.rotation);
            Quaternion[] locals = new Quaternion[times.Length];
            for (int index = 0; index < times.Length; index++)
            {
                locals[index] = Quaternion.Inverse(parentRotations[index]) *
                                desiredTargetRelative[index];
            }

            return locals;
        }

        private static LeftArmGeometrySamples SampleLeftArmGeometry(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            Transform shoulder =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "LeftArm");
            Transform elbow =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "LeftForeArm");
            Transform hand =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "LeftHand");
            Transform knee =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "LeftLeg");
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            LeftArmGeometrySamples samples = new LeftArmGeometrySamples
            {
                UpperArmDirections = new Vector3[times.Length],
                ForeArmDirections = new Vector3[times.Length],
                LeftHandPositions = new Vector3[times.Length],
                LeftKneePositions = new Vector3[times.Length]
            };
            AnimationMode.StartAnimationMode();
            try
            {
                for (int index = 0; index < times.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        target.gameObject,
                        clip,
                        times[index]);
                    AnimationMode.EndSampling();
                    samples.UpperArmDirections[index] =
                        (Quaternion.Inverse(target.rotation) *
                         (elbow.position - shoulder.position)).normalized;
                    samples.ForeArmDirections[index] =
                        (Quaternion.Inverse(target.rotation) *
                         (hand.position - elbow.position)).normalized;
                    samples.LeftHandPositions[index] =
                        target.InverseTransformPoint(hand.position);
                    samples.LeftKneePositions[index] =
                        target.InverseTransformPoint(knee.position);
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            return samples;
        }

        private static Quaternion[] StraightenedLeftForeArmTargetRotations(
            Quaternion[] currentTargetRotations,
            LeftArmGeometrySamples geometry)
        {
            if (currentTargetRotations.Length !=
                geometry.UpperArmDirections.Length)
            {
                throw new InvalidOperationException(
                    "Forward left-forearm sample counts differ.");
            }

            return currentTargetRotations
                .Select((rotation, index) =>
                    Quaternion.FromToRotation(
                        geometry.ForeArmDirections[index],
                        geometry.UpperArmDirections[index]) * rotation)
                .ToArray();
        }

        private static AnimationClip CreateLeftArmCorrectedClip(
            AnimationClip baseClip,
            Transform target,
            float[] times,
            Dictionary<string, Quaternion[]> desiredTargetRotations)
        {
            Dictionary<string, Quaternion[]> desiredLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    baseClip,
                    target,
                    times,
                    desiredTargetRotations,
                    LeftArmBones);
            AnimationClip corrected = UnityEngine.Object.Instantiate(baseClip);
            corrected.name = baseClip.name;
            corrected.hideFlags = HideFlags.None;
            foreach (string boneName in LeftArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    corrected,
                    BonePath(target, boneName),
                    times,
                    desiredLocals[boneName]);
            }

            return corrected;
        }

        private static float FindLeftArmClearanceDegrees(
            AnimationClip baseClip,
            Transform target,
            float[] times,
            Dictionary<string, Quaternion[]> downwardRotations,
            LeftArmGeometrySamples sourceGeometry)
        {
            float kneeSide = Mathf.Sign(
                sourceGeometry.LeftKneePositions
                    .Take(sourceGeometry.LeftKneePositions.Length - 1)
                    .Average(position => position.x));
            if (Mathf.Approximately(kneeSide, 0f))
            {
                kneeSide = -1f;
            }

            for (float magnitude = 0f;
                 magnitude <= ForwardLeftArmAngleSearchLimit;
                 magnitude += ForwardLeftArmAngleSearchStep)
            {
                float degrees = kneeSide * magnitude;
                Quaternion correction = Quaternion.AngleAxis(
                    degrees,
                    Vector3.forward);
                Dictionary<string, Quaternion[]> candidateRotations =
                    LeftArmBones.ToDictionary(
                        boneName => boneName,
                        boneName => downwardRotations[boneName]
                            .Select(rotation => correction * rotation)
                            .ToArray(),
                        StringComparer.Ordinal);
                AnimationClip candidate = CreateLeftArmCorrectedClip(
                    baseClip,
                    target,
                    times,
                    candidateRotations);
                float gap;
                try
                {
                    gap = LeftHandKneeMinimumSideGap(
                        SampleLeftArmGeometry(candidate, target, times));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(candidate);
                }

                if (gap >= ForwardLeftHandKneeMinimumGap)
                {
                    return degrees;
                }
            }

            throw new InvalidOperationException(
                "The Forward left hand could not reach the approved knee-side clearance.");
        }

        private static float LeftElbowBendMaximum(
            LeftArmGeometrySamples geometry)
        {
            float maximum = 0f;
            for (int index = 0;
                 index < geometry.UpperArmDirections.Length;
                 index++)
            {
                maximum = Mathf.Max(
                    maximum,
                    Vector3.Angle(
                        geometry.UpperArmDirections[index],
                        geometry.ForeArmDirections[index]));
            }

            return maximum;
        }

        private static float LeftArmDownwardMeanAngle(
            LeftArmGeometrySamples geometry)
        {
            return Vector3.Angle(
                DirectionMean(
                    geometry.UpperArmDirections.Take(
                        geometry.UpperArmDirections.Length - 1)),
                Vector3.down);
        }

        private static float LeftArmDownwardMaximumAngle(
            LeftArmGeometrySamples geometry)
        {
            return geometry.UpperArmDirections.Max(
                direction => Vector3.Angle(direction, Vector3.down));
        }

        private static float LeftHandKneeMinimumSideGap(
            LeftArmGeometrySamples geometry)
        {
            float kneeSide = Mathf.Sign(
                geometry.LeftKneePositions
                    .Take(geometry.LeftKneePositions.Length - 1)
                    .Average(position => position.x));
            if (Mathf.Approximately(kneeSide, 0f))
            {
                kneeSide = -1f;
            }

            float minimum = float.PositiveInfinity;
            for (int index = 0;
                 index < geometry.LeftHandPositions.Length;
                 index++)
            {
                minimum = Mathf.Min(
                    minimum,
                    kneeSide *
                    (geometry.LeftHandPositions[index].x -
                     geometry.LeftKneePositions[index].x));
            }

            return minimum;
        }

        private static string ArmParentBoneName(string boneName)
        {
            switch (boneName)
            {
                case "LeftShoulder":
                case "RightShoulder":
                    return null;
                case "LeftArm":
                    return "LeftShoulder";
                case "LeftForeArm":
                    return "LeftArm";
                case "RightArm":
                    return "RightShoulder";
                case "RightForeArm":
                    return "RightArm";
                default:
                    throw new InvalidOperationException(
                        "Unsupported crouch arm bone: " + boneName + ".");
            }
        }

        private static UpperBodyCenterGeometry MeasureUpperBodyCenterGeometry(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            if (times.Length == 0)
            {
                throw new InvalidOperationException(
                    "Upper-body center sampling requires at least one frame.");
            }

            Transform hips = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Hips");
            Transform pivot = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Spine02");
            Transform leftShoulder =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "LeftShoulder");
            Transform rightShoulder =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "RightShoulder");
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            float baseLateral = 0f;
            Vector3 pivotToShoulder = Vector3.zero;
            float meanLateral = 0f;
            AnimationMode.StartAnimationMode();
            try
            {
                foreach (float time in times)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        target.gameObject,
                        clip,
                        time);
                    AnimationMode.EndSampling();
                    Vector3 hipsLocal = target.InverseTransformPoint(hips.position);
                    Vector3 pivotLocal = target.InverseTransformPoint(pivot.position);
                    Vector3 shoulderCenterLocal = target.InverseTransformPoint(
                        (leftShoulder.position + rightShoulder.position) * 0.5f);
                    baseLateral += pivotLocal.x - hipsLocal.x;
                    pivotToShoulder += shoulderCenterLocal - pivotLocal;
                    meanLateral += shoulderCenterLocal.x - hipsLocal.x;
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            float inverseCount = 1f / times.Length;
            return new UpperBodyCenterGeometry
            {
                BaseLateral = baseLateral * inverseCount,
                PivotToShoulderCenter = pivotToShoulder * inverseCount,
                MeanLateralOffset = meanLateral * inverseCount
            };
        }

        private static float SolveUpperBodyCenterCorrectionDegrees(
            UpperBodyCenterGeometry geometry)
        {
            float radians = 0f;
            Vector3 upper = geometry.PivotToShoulderCenter;
            for (int iteration = 0; iteration < 12; iteration++)
            {
                float sine = Mathf.Sin(radians);
                float cosine = Mathf.Cos(radians);
                float lateral = geometry.BaseLateral +
                                cosine * upper.x -
                                sine * upper.y;
                float derivative = -sine * upper.x - cosine * upper.y;
                if (Mathf.Abs(derivative) <= 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Player_Crouch_Forward upper-body center derivative is degenerate.");
                }

                radians -= lateral / derivative;
            }

            float degrees = radians * Mathf.Rad2Deg;
            if (float.IsNaN(degrees) ||
                float.IsInfinity(degrees) ||
                Mathf.Abs(degrees) > 20f)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Forward upper-body center correction is invalid: " +
                    Num(degrees) + ".");
            }

            return degrees;
        }

        private static Vector3[] SampleTargetRelativeDirections(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            Transform lower = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Spine02");
            Transform spine = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Spine");
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            Vector3[] directions = new Vector3[times.Length];
            AnimationMode.StartAnimationMode();
            try
            {
                for (int index = 0; index < times.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        target.gameObject,
                        clip,
                        times[index]);
                    AnimationMode.EndSampling();
                    Vector3 worldDirection =
                        (spine.position - lower.position).normalized;
                    directions[index] = Quaternion.Inverse(target.rotation) *
                                        worldDirection;
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            return directions;
        }

        private static Vector3[] SampleTargetRelativeBoneForwardDirections(
            AnimationClip clip,
            Transform target,
            string boneName,
            float[] times)
        {
            Transform bone = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                boneName);
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            Vector3[] directions = new Vector3[times.Length];
            AnimationMode.StartAnimationMode();
            try
            {
                for (int index = 0; index < times.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        target.gameObject,
                        clip,
                        times[index]);
                    AnimationMode.EndSampling();
                    directions[index] =
                        (Quaternion.Inverse(target.rotation) * bone.forward)
                        .normalized;
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            return directions;
        }

        private static Vector3 DirectionAtGroundAngle(
            Vector3 currentDirection,
            float angleDegrees)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(
                currentDirection,
                Vector3.up);
            if (horizontal.sqrMagnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Current crouch waist direction has no horizontal lean reference.");
            }

            float radians = angleDegrees * Mathf.Deg2Rad;
            return (horizontal.normalized * Mathf.Cos(radians) +
                    Vector3.up * Mathf.Sin(radians)).normalized;
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

        private static Quaternion[] SampleRotations(
            AnimationClip clip,
            Transform target,
            float[] times,
            Func<Quaternion> capture)
        {
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            Quaternion[] rotations = new Quaternion[times.Length];
            AnimationMode.StartAnimationMode();
            try
            {
                for (int index = 0; index < times.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        target.gameObject,
                        clip,
                        times[index]);
                    AnimationMode.EndSampling();
                    rotations[index] = capture();
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            return rotations;
        }

        private static Quaternion CalculateLowerSpineCorrection(
            AnimationClip enter,
            Transform target,
            float time,
            Quaternion desiredUpperTargetRelative)
        {
            Transform lower = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Spine02");
            Transform upper = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Spine");
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(target.gameObject, enter, time);
                AnimationMode.EndSampling();
                Quaternion parentTargetRelative =
                    Quaternion.Inverse(target.rotation) * lower.parent.rotation;
                Quaternion lowerToUpper =
                    Quaternion.Inverse(lower.rotation) * upper.rotation;
                Quaternion desiredLowerLocal =
                    Quaternion.Inverse(parentTargetRelative) *
                    desiredUpperTargetRelative *
                    Quaternion.Inverse(lowerToUpper);
                return desiredLowerLocal * Quaternion.Inverse(lower.localRotation);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static Quaternion CalculateLowerSpineDirectionCorrection(
            AnimationClip enter,
            Transform target,
            float time,
            Vector3 desiredTargetRelativeDirection)
        {
            Transform lower = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Spine02");
            Transform upper = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                "Spine");
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(target.gameObject, enter, time);
                AnimationMode.EndSampling();
                Vector3 currentTargetRelativeDirection =
                    Quaternion.Inverse(target.rotation) *
                    (upper.position - lower.position).normalized;
                Quaternion targetSpaceCorrection = Quaternion.FromToRotation(
                    currentTargetRelativeDirection,
                    desiredTargetRelativeDirection.normalized);
                Quaternion parentTargetRelative =
                    Quaternion.Inverse(target.rotation) * lower.parent.rotation;
                return Quaternion.Inverse(parentTargetRelative) *
                       targetSpaceCorrection *
                       parentTargetRelative;
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static Quaternion CalculateBoneForwardDirectionCorrection(
            AnimationClip clip,
            Transform target,
            string boneName,
            float time,
            Vector3 desiredTargetRelativeDirection)
        {
            Transform bone = PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                target,
                boneName);
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(target.gameObject, clip, time);
                AnimationMode.EndSampling();
                Vector3 currentTargetRelativeDirection =
                    (Quaternion.Inverse(target.rotation) * bone.forward).normalized;
                Quaternion targetSpaceCorrection = Quaternion.FromToRotation(
                    currentTargetRelativeDirection,
                    desiredTargetRelativeDirection.normalized);
                Quaternion parentTargetRelative =
                    Quaternion.Inverse(target.rotation) * bone.parent.rotation;
                return Quaternion.Inverse(parentTargetRelative) *
                       targetSpaceCorrection *
                       parentTargetRelative;
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static Vector3 DirectionMean(IEnumerable<Vector3> values)
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

        private static Quaternion QuaternionMean(IEnumerable<Quaternion> values)
        {
            Quaternion[] rotations = values.ToArray();
            if (rotations.Length == 0)
            {
                throw new InvalidOperationException(
                    "Quaternion mean requires at least one sample.");
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

            float magnitude = sum.magnitude;
            if (magnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Quaternion mean is degenerate.");
            }

            sum /= magnitude;
            return new Quaternion(sum.x, sum.y, sum.z, sum.w);
        }

        private static void ReplaceRotationWithQuaternionCurves(
            AnimationClip clip,
            string path,
            float[] times,
            Quaternion[] rotations)
        {
            if (times.Length != rotations.Length)
            {
                throw new InvalidOperationException(
                    "Quaternion curve sample counts differ.");
            }

            foreach (EditorCurveBinding binding in AnimationUtility
                         .GetCurveBindings(clip)
                         .Where(binding =>
                             binding.type == typeof(Transform) &&
                             string.Equals(
                                 binding.path,
                                 path,
                                 StringComparison.Ordinal) &&
                             IsRotationProperty(binding.propertyName))
                         .ToArray())
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }

            Quaternion[] continuous = new Quaternion[rotations.Length];
            for (int index = 0; index < rotations.Length; index++)
            {
                Quaternion rotation = rotations[index].normalized;
                if (index > 0 && Quaternion.Dot(continuous[index - 1], rotation) < 0f)
                {
                    rotation = new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w);
                }

                continuous[index] = rotation;
            }

            SetQuaternionComponent(clip, path, "x", times, continuous, q => q.x);
            SetQuaternionComponent(clip, path, "y", times, continuous, q => q.y);
            SetQuaternionComponent(clip, path, "z", times, continuous, q => q.z);
            SetQuaternionComponent(clip, path, "w", times, continuous, q => q.w);
        }

        private static void SetQuaternionComponent(
            AnimationClip clip,
            string path,
            string axis,
            float[] times,
            Quaternion[] rotations,
            Func<Quaternion, float> component)
        {
            Keyframe[] keys = times
                .Select((time, index) =>
                    new Keyframe(time, component(rotations[index])))
                .ToArray();
            AnimationCurve curve = new AnimationCurve(keys);
            for (int index = 0; index < keys.Length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation." + axis),
                curve);
        }

        private static bool IsRotationProperty(string property)
        {
            return property.StartsWith(
                       "localEulerAnglesRaw.",
                       StringComparison.Ordinal) ||
                   property.StartsWith(
                       "m_LocalRotation.",
                       StringComparison.Ordinal);
        }

        private static TransformSnapshot[] SampleTransformSnapshot(
            AnimationClip clip,
            Transform target,
            float time)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    binding.type == typeof(Transform) &&
                    !string.IsNullOrEmpty(binding.path))
                .ToArray();
            string[] paths = bindings.Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(target.gameObject, clip, time);
                AnimationMode.EndSampling();
                return paths.Select(path =>
                {
                    Transform transform = target.Find(path) ??
                                          throw new InvalidOperationException(
                                              "Crouch snapshot path is missing: " +
                                              path + ".");
                    EditorCurveBinding[] pathBindings = bindings
                        .Where(binding => string.Equals(
                            binding.path,
                            path,
                            StringComparison.Ordinal))
                        .ToArray();
                    return new TransformSnapshot
                    {
                        Path = path,
                        LocalPosition = transform.localPosition,
                        LocalRotation = transform.localRotation,
                        HasPosition = pathBindings.Any(binding =>
                            binding.propertyName.StartsWith(
                                "m_LocalPosition.",
                                StringComparison.Ordinal)),
                        HasRotation = pathBindings.Any(binding =>
                            IsRotationProperty(binding.propertyName))
                    };
                }).ToArray();
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static AnimationClip CreateStaticIdleClip(
            IEnumerable<TransformSnapshot> snapshots,
            float frameRate)
        {
            AnimationClip idle = UnityEngine.Object.Instantiate(
                LoadClip(PlayerCrouchIdleForwardAnimationTool.IdleClipPath));
            idle.name = "Player_Crouch_Idle";
            idle.hideFlags = HideFlags.None;
            foreach (EditorCurveBinding binding in AnimationUtility
                         .GetCurveBindings(idle))
            {
                AnimationUtility.SetEditorCurve(idle, binding, null);
            }

            float[] times =
            {
                0f,
                PlayerCrouchIdleForwardAnimationTool.IdleDurationSeconds
            };
            foreach (TransformSnapshot snapshot in snapshots)
            {
                if (snapshot.HasPosition)
                {
                    SetConstantCurve(
                        idle,
                        snapshot.Path,
                        "m_LocalPosition.x",
                        times,
                        snapshot.LocalPosition.x);
                    SetConstantCurve(
                        idle,
                        snapshot.Path,
                        "m_LocalPosition.y",
                        times,
                        snapshot.LocalPosition.y);
                    SetConstantCurve(
                        idle,
                        snapshot.Path,
                        "m_LocalPosition.z",
                        times,
                        snapshot.LocalPosition.z);
                }

                if (snapshot.HasRotation)
                {
                    Quaternion[] rotations =
                    {
                        snapshot.LocalRotation,
                        snapshot.LocalRotation
                    };
                    ReplaceRotationWithQuaternionCurves(
                        idle,
                        snapshot.Path,
                        times,
                        rotations);
                }
            }

            idle.frameRate = frameRate;
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(idle);
            settings.startTime = 0f;
            settings.stopTime =
                PlayerCrouchIdleForwardAnimationTool.IdleDurationSeconds;
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(idle, settings);
            return idle;
        }

        private static AnimationClip CreateHeldIdleClipFromEnter(
            AnimationClip enter)
        {
            AnimationClip idle = UnityEngine.Object.Instantiate(enter);
            idle.name = "Player_Crouch_Idle";
            idle.hideFlags = HideFlags.None;
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(idle);
            settings.startTime = 0f;
            settings.stopTime = enter.length;
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(idle, settings);
            return idle;
        }

        private static void ConfigureHeldIdleController(
            AnimationClip idle,
            float holdCycleOffset)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    PlayerCrouchIdleForwardAnimationTool.IdleControllerPath) ??
                throw new FileNotFoundException(
                    "Player_Crouch_Idle controller is missing.",
                    PlayerCrouchIdleForwardAnimationTool.IdleControllerPath);
            AnimatorState[] states = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(child => child.state)
                .Where(state => state != null)
                .ToArray();
            if (states.Length != 1 ||
                !string.Equals(
                    states[0].name,
                    PlayerCrouchIdleForwardAnimationTool.IdleStateName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Idle controller state differs.");
            }

            AnimatorState state = states[0];
            state.motion = idle;
            state.speed = 0f;
            state.cycleOffset = holdCycleOffset;
            state.mirror = false;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void SetConstantCurve(
            AnimationClip clip,
            string path,
            string property,
            float[] times,
            float value)
        {
            AnimationCurve curve = new AnimationCurve(
                times.Select(time => new Keyframe(time, value, 0f, 0f)).ToArray());
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static float SwingDifference(
            Quaternion[] before,
            Quaternion[] after)
        {
            if (before.Length != after.Length)
            {
                return float.PositiveInfinity;
            }

            Quaternion beforeMean = QuaternionMean(before.Take(before.Length - 1));
            Quaternion afterMean = QuaternionMean(after.Take(after.Length - 1));
            float max = 0f;
            for (int index = 0; index < before.Length; index++)
            {
                max = Mathf.Max(
                    max,
                    Mathf.Abs(
                        Quaternion.Angle(beforeMean, before[index]) -
                        Quaternion.Angle(afterMean, after[index])));
                if (index > 0)
                {
                    max = Mathf.Max(
                        max,
                        Mathf.Abs(
                            Quaternion.Angle(before[index - 1], before[index]) -
                            Quaternion.Angle(after[index - 1], after[index])));
                }
            }

            return max;
        }

        private static float RotationSequenceDifference(
            Quaternion[] expected,
            Quaternion[] actual)
        {
            if (expected.Length != actual.Length)
            {
                return float.PositiveInfinity;
            }

            float max = 0f;
            for (int index = 0; index < expected.Length; index++)
            {
                max = Mathf.Max(
                    max,
                    Quaternion.Angle(expected[index], actual[index]));
            }

            return max;
        }

        private static bool RuntimePosesMatch(
            AnimationClip firstClip,
            Transform firstTarget,
            float firstTime,
            AnimationClip secondClip,
            Transform secondTarget,
            float secondTime)
        {
            TransformSnapshot[] first = SampleTransformSnapshot(
                firstClip,
                firstTarget,
                firstTime);
            TransformSnapshot[] second = SampleTransformSnapshot(
                secondClip,
                secondTarget,
                secondTime);
            Dictionary<string, TransformSnapshot> secondByPath = second
                .ToDictionary(snapshot => snapshot.Path, StringComparer.Ordinal);
            return first.Length == second.Length && first.All(snapshot =>
                secondByPath.TryGetValue(
                    snapshot.Path,
                    out TransformSnapshot other) &&
                (!snapshot.HasPosition ||
                 Vector3.Distance(
                     snapshot.LocalPosition,
                     other.LocalPosition) <= CurveTolerance) &&
                (!snapshot.HasRotation ||
                 Quaternion.Angle(
                     snapshot.LocalRotation,
                     other.LocalRotation) <= CurveTolerance));
        }

        private static bool VerifyOutsidePathsUnchanged(
            Dictionary<EditorCurveBinding, AnimationCurve> before,
            AnimationClip after,
            HashSet<string> allowedPaths)
        {
            Dictionary<EditorCurveBinding, AnimationCurve> afterCurves =
                CaptureCurves(after);
            EditorCurveBinding[] beforeOutside = before.Keys
                .Where(binding => !allowedPaths.Contains(binding.path))
                .ToArray();
            EditorCurveBinding[] afterOutside = afterCurves.Keys
                .Where(binding => !allowedPaths.Contains(binding.path))
                .ToArray();
            if (!new HashSet<EditorCurveBinding>(beforeOutside).SetEquals(
                    afterOutside))
            {
                return false;
            }

            return beforeOutside.All(binding =>
                CurvesEqual(
                    before[binding],
                    afterCurves[binding],
                    compareValues: true));
        }

        private static bool VerifyPathsUnchanged(
            Dictionary<EditorCurveBinding, AnimationCurve> before,
            AnimationClip after,
            HashSet<string> requiredPaths)
        {
            Dictionary<EditorCurveBinding, AnimationCurve> afterCurves =
                CaptureCurves(after);
            EditorCurveBinding[] beforeSelected = before.Keys
                .Where(binding => requiredPaths.Contains(binding.path))
                .ToArray();
            EditorCurveBinding[] afterSelected = afterCurves.Keys
                .Where(binding => requiredPaths.Contains(binding.path))
                .ToArray();
            if (!new HashSet<EditorCurveBinding>(beforeSelected).SetEquals(
                    afterSelected))
            {
                return false;
            }

            return beforeSelected.All(binding =>
                CurvesEqual(
                    before[binding],
                    afterCurves[binding],
                    compareValues: true));
        }

        private static AnimationClip LoadClip(string path)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                throw new FileNotFoundException(
                    "Crouch animation clip is missing.",
                    path);
            }

            return clip;
        }

        private static AnimationClip LoadForwardSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(
                    PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    clips[0].name,
                    PlayerCrouchIdleForwardAnimationTool.ExpectedForwardTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Forward source must expose exactly one mixamo.com Take.");
            }

            return clips[0];
        }

        private static Dictionary<EditorCurveBinding, AnimationCurve> CaptureCurves(
            AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip).ToDictionary(
                binding => binding,
                binding => CloneCurve(RequireCurve(clip, binding)));
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static string[] ResolveBonePaths(Transform target, string[] names)
        {
            return names.Select(name =>
                    AnimationUtility.CalculateTransformPath(
                        PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                            target,
                            name),
                        target))
                .ToArray();
        }

        private static void RequireSamePaths(
            string[] first,
            string[] second,
            string label)
        {
            if (!first.SequenceEqual(second, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Crouch " + label + " paths differ between targets.");
            }
        }

        private static EditorCurveBinding[] ResolveRotationBindings(
            AnimationClip clip,
            IEnumerable<string> paths)
        {
            Dictionary<string, EditorCurveBinding> bindings =
                AnimationUtility.GetCurveBindings(clip)
                    .Where(binding =>
                        binding.type == typeof(Transform) &&
                        binding.propertyName.StartsWith(
                            "localEulerAnglesRaw.",
                            StringComparison.Ordinal))
                    .ToDictionary(
                        binding => binding.path + "|" + binding.propertyName,
                        binding => binding,
                        StringComparer.Ordinal);
            List<EditorCurveBinding> result = new List<EditorCurveBinding>();
            foreach (string path in paths)
            {
                foreach (string axis in RotationAxes)
                {
                    string property = "localEulerAnglesRaw." + axis;
                    string key = path + "|" + property;
                    if (!bindings.TryGetValue(key, out EditorCurveBinding binding))
                    {
                        throw new InvalidOperationException(
                            "Required crouch rotation curve is missing: " + key + ".");
                    }

                    result.Add(binding);
                }
            }

            return result.ToArray();
        }

        private static AnimationCurve RequireCurve(
            AnimationClip clip,
            EditorCurveBinding binding)
        {
            return AnimationUtility.GetEditorCurve(clip, binding) ??
                   throw new InvalidOperationException(
                       "Crouch curve is missing: " + binding.path + "/" +
                       binding.propertyName + ".");
        }

        private static float CircularMean(
            AnimationCurve curve,
            float duration,
            float frameRate)
        {
            int frames = Mathf.RoundToInt(duration * frameRate);
            if (frames < 1)
            {
                throw new InvalidOperationException(
                    "Crouch clip has no frames for circular mean.");
            }

            double sin = 0d;
            double cos = 0d;
            for (int frame = 0; frame < frames; frame++)
            {
                float angle = curve.Evaluate(frame / frameRate) * Mathf.Deg2Rad;
                sin += Math.Sin(angle);
                cos += Math.Cos(angle);
            }

            return (float)(Math.Atan2(sin, cos) * Mathf.Rad2Deg);
        }

        private static void ApplyEndWeightedOffset(
            AnimationCurve curve,
            float motionDuration,
            float offset)
        {
            float start = motionDuration * EnterBlendStartNormalized;
            Keyframe[] keys = curve.keys;
            for (int index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                float normalized = Mathf.InverseLerp(start, motionDuration, key.time);
                float weight = normalized * normalized * (3f - 2f * normalized);
                key.value += offset * weight;
                keys[index] = key;
            }

            curve.keys = keys;
        }

        private static void ApplyConstantOffset(AnimationCurve curve, float offset)
        {
            Keyframe[] keys = curve.keys;
            for (int index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                key.value += offset;
                keys[index] = key;
            }

            curve.keys = keys;
        }

        private static void SaveOverExisting(AnimationClip source, string path)
        {
            AnimationClip target = LoadClip(path);
            EditorUtility.CopySerialized(source, target);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static AnimatorController ConfigureControllerBinding(
            string controllerPath,
            string stateName,
            AnimationClip clip,
            float speed,
            float cycleOffset)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) ??
                throw new FileNotFoundException(
                    "Crouch controller is missing.",
                    controllerPath);
            AnimatorState[] states = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(child => child.state)
                .Where(state => state != null)
                .ToArray();
            if (states.Length != 1 ||
                !string.Equals(
                    states[0].name,
                    stateName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Crouch controller state differs: " + controllerPath + ".");
            }

            AnimatorState state = states[0];
            state.motion = clip;
            state.speed = speed;
            state.cycleOffset = cycleOffset;
            state.mirror = false;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                controllerPath,
                ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) ??
                   throw new InvalidOperationException(
                       "Crouch controller could not be reloaded: " +
                       controllerPath + ".");
        }

        private static void RebindAnimator(
            Transform target,
            RuntimeAnimatorController controller)
        {
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    target.name + " Animator is missing.");
            animator.runtimeAnimatorController = null;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }

        private static bool VerifyOnlyBindingsChanged(
            Dictionary<EditorCurveBinding, AnimationCurve> before,
            AnimationClip afterClip,
            HashSet<EditorCurveBinding> allowed)
        {
            EditorCurveBinding[] afterBindings =
                AnimationUtility.GetCurveBindings(afterClip);
            if (!new HashSet<EditorCurveBinding>(before.Keys).SetEquals(
                    afterBindings))
            {
                return false;
            }

            foreach (KeyValuePair<EditorCurveBinding, AnimationCurve> pair in before)
            {
                if (allowed.Contains(pair.Key))
                {
                    continue;
                }

                if (!CurvesEqual(
                        pair.Value,
                        RequireCurve(afterClip, pair.Key),
                        compareValues: true))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool VerifyCurveMetadata(
            Dictionary<EditorCurveBinding, AnimationCurve> before,
            AnimationClip after,
            IEnumerable<EditorCurveBinding> bindings)
        {
            return bindings.All(binding =>
                before.TryGetValue(binding, out AnimationCurve first) &&
                CurvesEqual(
                    first,
                    RequireCurve(after, binding),
                    compareValues: false));
        }

        private static bool CurvesEqual(
            AnimationCurve first,
            AnimationCurve second,
            bool compareValues)
        {
            if (first == null || second == null ||
                first.length != second.length ||
                first.preWrapMode != second.preWrapMode ||
                first.postWrapMode != second.postWrapMode)
            {
                return false;
            }

            Keyframe[] firstKeys = first.keys;
            Keyframe[] secondKeys = second.keys;
            for (int index = 0; index < firstKeys.Length; index++)
            {
                Keyframe a = firstKeys[index];
                Keyframe b = secondKeys[index];
                if (Mathf.Abs(a.time - b.time) > CurveTolerance ||
                    (compareValues &&
                     Mathf.Abs(a.value - b.value) > CurveTolerance) ||
                    Mathf.Abs(a.inTangent - b.inTangent) > CurveTolerance ||
                    Mathf.Abs(a.outTangent - b.outTangent) > CurveTolerance ||
                    Mathf.Abs(a.inWeight - b.inWeight) > CurveTolerance ||
                    Mathf.Abs(a.outWeight - b.outWeight) > CurveTolerance ||
                    a.weightedMode != b.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }

        private static float MaxRangeDifference(
            Dictionary<EditorCurveBinding, AnimationCurve> before,
            AnimationClip after,
            IEnumerable<EditorCurveBinding> bindings)
        {
            float max = 0f;
            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationCurve first = before[binding];
                AnimationCurve second = RequireCurve(after, binding);
                float firstRange = first.keys.Max(key => key.value) -
                                   first.keys.Min(key => key.value);
                float secondRange = second.keys.Max(key => key.value) -
                                    second.keys.Min(key => key.value);
                max = Mathf.Max(max, Mathf.Abs(firstRange - secondRange));
            }

            return max;
        }

        private static Dictionary<string, RootPose> CaptureRootPoses(
            IEnumerable<Transform> targets)
        {
            return targets.ToDictionary(
                target => target.name,
                target => new RootPose
                {
                    Position = target.position,
                    Rotation = target.rotation,
                    Scale = target.localScale
                },
                StringComparer.Ordinal);
        }

        private static bool RootPosesEqual(
            Dictionary<string, RootPose> first,
            Dictionary<string, RootPose> second)
        {
            return first.Count == second.Count && first.All(pair =>
                second.TryGetValue(pair.Key, out RootPose other) &&
                Vector3.Distance(pair.Value.Position, other.Position) <=
                CurveTolerance &&
                Quaternion.Angle(pair.Value.Rotation, other.Rotation) <=
                CurveTolerance &&
                Vector3.Distance(pair.Value.Scale, other.Scale) <=
                CurveTolerance);
        }

        private static Dictionary<string, string> CaptureOtherAnimators(
            Scene scene,
            IEnumerable<Transform> excludedTargets)
        {
            HashSet<int> excluded = excludedTargets
                .Select(target => target.GetInstanceID())
                .ToHashSet();
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Animator>(true))
                .Where(animator => !excluded.Contains(animator.transform.GetInstanceID()))
                .ToDictionary(
                    animator => AnimationUtility.CalculateTransformPath(
                        animator.transform,
                        null),
                    animator =>
                        (animator.runtimeAnimatorController == null
                            ? "none"
                            : AssetDatabase.GetAssetPath(
                                animator.runtimeAnimatorController)) + "|" +
                        animator.applyRootMotion + "|" + animator.cullingMode,
                    StringComparer.Ordinal);
        }

        private static bool DictionariesEqual(
            Dictionary<string, string> first,
            Dictionary<string, string> second)
        {
            return first.Count == second.Count && first.All(pair =>
                second.TryGetValue(pair.Key, out string value) &&
                string.Equals(pair.Value, value, StringComparison.Ordinal));
        }

        private static string HashFile(string assetPath)
        {
            string path = Path.GetFullPath(assetPath);
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }
        }

        private static void WriteMetrics(AlignmentApplyMetrics metrics)
        {
            string path = Path.GetFullPath(ApplyMetricsPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "Crouch alignment metrics directory is unavailable."));
            File.WriteAllText(
                path,
                JsonUtility.ToJson(metrics, true));
        }

        private static void WriteForwardLeftArmStraightDownMetrics(
            ForwardLeftArmStraightDownApplyMetrics metrics)
        {
            string path = Path.GetFullPath(
                ForwardLeftArmStraightDownMetricsPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "Forward left-arm metrics directory is unavailable."));
            File.WriteAllText(
                path,
                JsonUtility.ToJson(metrics, true));
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(
                    scene.path,
                    PlayerCrouchEnterAnimationTool.ScenePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene for crouch pose alignment.");
            }

            return scene;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
