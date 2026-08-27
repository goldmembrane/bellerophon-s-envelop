using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerCrouchBackwardSidestepAnimationTool
    {
        internal const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string LayoutRootName = "PlayerAnimationLayout";
        internal const string BackwardTargetName = "Player_Crouch_Backward";
        internal const string SidestepTargetName = "Player_Crouch_Sidestep";
        internal const string BackwardStateName = "PlayerCrouchBackward";
        internal const string SidestepStateName = "PlayerCrouchSidestep";
        internal const string BackwardSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Backward_Mixamo.fbx";
        internal const string SidestepSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Sidestep_Mixamo.fbx";
        internal const string BackwardClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Backward_Mixamo_InPlace.anim";
        internal const string SidestepClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Sidestep_Mixamo_InPlace.anim";
        internal const string BackwardControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Backward.controller";
        internal const string SidestepControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Sidestep.controller";
        internal const string ExpectedTakeName = "mixamo.com";
        internal const string ApplyMetricsPath =
            "docs/validation/player_crouch_backward_sidestep_apply_metrics.json";
        internal const string ArmAlignmentMetricsPath =
            "docs/validation/player_crouch_backward_sidestep_arm_alignment_apply_metrics.json";
        internal const string ArmClearanceMetricsPath =
            "docs/validation/player_crouch_backward_sidestep_arm_clearance_apply_metrics.json";
        internal const string MovingKneeSideArmMetricsPath =
            "docs/validation/player_crouch_moving_knee_side_arm_apply_metrics.json";
        internal const string LeftArmStraightDownMetricsPath =
            "docs/validation/player_crouch_backward_sidestep_left_arm_straight_down_apply_metrics.json";

        private const string ExpectedBackwardSourceHash =
            "2C4C852B0C24375B3AAD9DDD4D0C470FD825B59635E91DD90336382A638B0488";
        private const string ExpectedSidestepSourceHash =
            "66C79A36F288D22413F7E20591524D407BECD1E187A4866B6F0375105CE1D07F";
        private const float CurveTolerance = 0.000001f;
        internal const float ArmMeanToleranceDegrees = 0.1f;
        internal const float ArmSwingToleranceDegrees = 0.25f;
        internal const float ArmClearanceDegrees = 18f;
        internal const float KneeSideMinimumBoneGap = 0.05f;
        internal const float KneeSideGapTolerance = 0.005f;
        internal const float LeftElbowStraightToleranceDegrees = 0.5f;
        internal const float LeftArmDownwardMeanToleranceDegrees = 30f;
        internal const float LeftArmDownwardMaximumToleranceDegrees = 45f;
        private const float KneeSideAngleSearchLimit = 60f;
        private const float KneeSideAngleSearchStep = 0.25f;

        internal static readonly string[] ArmBones =
        {
            "LeftShoulder",
            "LeftArm",
            "LeftForeArm",
            "RightShoulder",
            "RightArm",
            "RightForeArm"
        };

        [Serializable]
        private sealed class ApplyMetrics
        {
            public string targetSet;
            public TargetMetrics backward;
            public TargetMetrics sidestep;
            public bool sourceFbxFilesUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool rootTransformsUnchanged;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class TargetMetrics
        {
            public string target;
            public string sourcePath;
            public string sourceHash;
            public string sourceTake;
            public string derivedClipPath;
            public string controllerPath;
            public string carrierPath;
            public string horizontalProperties;
            public string verticalProperty;
            public float clipDurationSeconds;
            public float clipFrameRate;
            public int sourceCurveBindings;
            public int changedHorizontalBindings;
            public bool nonHorizontalCurvesUnchanged;
            public bool clipTimingUnchanged;
            public bool clipIsLooping;
            public bool applyRootMotion;
        }

        [Serializable]
        private sealed class ArmAlignmentApplyMetrics
        {
            public string targetSet;
            public string armReference;
            public ArmTargetMetrics forward;
            public ArmTargetMetrics backward;
            public ArmTargetMetrics sidestep;
            public bool idleClipUnchanged;
            public bool controllersUnchanged;
            public bool sceneAssetUnchanged;
            public bool sourceFbxFilesUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool rootTransformsUnchanged;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ArmTargetMetrics
        {
            public string target;
            public string clipPath;
            public int armBonesChanged;
            public float armMeanDifferenceDegreesMax;
            public float armSwingDifferenceDegreesMax;
            public float leftArmAdjustmentDegrees;
            public float rightArmAdjustmentDegrees;
            public float handKneeMinimumBoneGapTarget;
            public float handKneeMinimumBoneGapBefore;
            public float handKneeMinimumBoneGapAfter;
            public bool nonArmCurvesUnchanged;
            public bool armFrameTimingUnchanged;
            public bool clipTimingUnchanged;
            public bool clipIsLooping;
            public bool applyRootMotion;
        }

        [Serializable]
        private sealed class LeftArmStraightDownApplyMetrics
        {
            public string targetSet;
            public LeftArmStraightDownTargetMetrics backward;
            public LeftArmStraightDownTargetMetrics sidestep;
            public bool referenceClipsUnchanged;
            public bool controllersUnchanged;
            public bool sceneAssetUnchanged;
            public bool sourceFbxFilesUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool rootTransformsUnchanged;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class LeftArmStraightDownTargetMetrics
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
            public bool leftArmFrameTimingUnchanged;
            public bool clipTimingUnchanged;
            public bool clipIsLooping;
            public bool applyRootMotion;
        }

        private sealed class TargetSpec
        {
            internal string TargetName;
            internal string StateName;
            internal string SourcePath;
            internal string ExpectedHash;
            internal string ClipPath;
            internal string ControllerPath;
            internal string ClipName;
        }

        private sealed class CarrierSelection
        {
            internal EditorCurveBinding[] Bindings;
            internal string[] HorizontalProperties;
            internal string VerticalProperty;
        }

        private readonly struct RootPose
        {
            internal readonly Vector3 Position;
            internal readonly Quaternion Rotation;
            internal readonly Vector3 Scale;

            internal RootPose(Transform target)
            {
                Position = target.position;
                Rotation = target.rotation;
                Scale = target.localScale;
            }
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Backward And Sidestep Mixamo In Place")]
        internal static void Apply()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch backward and sidestep apply.");
            }

            TargetSpec backwardSpec = BackwardSpec();
            TargetSpec sidestepSpec = SidestepSpec();
            EnsureSourceHash(backwardSpec);
            EnsureSourceHash(sidestepSpec);
            ConfigureImporter(backwardSpec);
            ConfigureImporter(sidestepSpec);

            Transform backward = RequireTarget(scene, BackwardTargetName);
            Transform sidestep = RequireTarget(scene, SidestepTargetName);
            RootPose backwardRootBefore = new RootPose(backward);
            RootPose sidestepRootBefore = new RootPose(sidestep);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimatorStates(backward.parent);

            TargetMetrics backwardMetrics = ApplyTarget(backwardSpec, backward);
            TargetMetrics sidestepMetrics = ApplyTarget(sidestepSpec, sidestep);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            bool rootsUnchanged = RootPoseMatches(backward, backwardRootBefore) &&
                                  RootPoseMatches(sidestep, sidestepRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimatorStates(backward.parent));
            EnsureSourceHash(backwardSpec);
            EnsureSourceHash(sidestepSpec);
            bool sourceFilesUnchanged = true;
            ApplyMetrics metrics = new ApplyMetrics
            {
                targetSet = BackwardTargetName + ", " + SidestepTargetName,
                backward = backwardMetrics,
                sidestep = sidestepMetrics,
                sourceFbxFilesUnchanged = sourceFilesUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                rootTransformsUnchanged = rootsUnchanged,
                passedNumericChecks = sourceFilesUnchanged &&
                    otherAnimatorsUnchanged &&
                    rootsUnchanged &&
                    TargetPassed(backwardMetrics) &&
                    TargetPassed(sidestepMetrics),
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch backward and sidestep apply support checks failed." +
                    " Backward=" + TargetPassed(backwardMetrics) +
                    ", Sidestep=" + TargetPassed(sidestepMetrics) +
                    ", Roots=" + rootsUnchanged +
                    ", OtherAnimators=" + otherAnimatorsUnchanged + ".");
            }

            Debug.Log(
                "[PlayerCrouchBackwardSidestep] Applied the two exact embedded Take '" +
                ExpectedTakeName + "' clips as in-place loops." +
                " BackwardDuration=" + Num(backwardMetrics.clipDurationSeconds) +
                ", BackwardCarrier=" + backwardMetrics.carrierPath +
                ", BackwardHorizontal=" + backwardMetrics.horizontalProperties +
                ", SidestepDuration=" + Num(sidestepMetrics.clipDurationSeconds) +
                ", SidestepCarrier=" + sidestepMetrics.carrierPath +
                ", SidestepHorizontal=" + sidestepMetrics.horizontalProperties +
                ", NonHorizontalCurvesChanged=False, Speed=1, Mirror=False" +
                ", Loop=True, ApplyRootMotion=False, OtherPlayersUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Align Crouch Backward And Sidestep Arms To Idle")]
        internal static void ApplyIdleArmAlignment()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch arm alignment.");
            }

            Transform idleTarget = RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform backwardTarget = RequireTarget(scene, BackwardTargetName);
            Transform sidestepTarget = RequireTarget(scene, SidestepTargetName);
            AnimationClip idle = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            AnimationClip backward = LoadClip(BackwardClipPath);
            AnimationClip sidestep = LoadClip(SidestepClipPath);
            VerifyAllTransformBindingsExist(idle, idleTarget);
            VerifyAllTransformBindingsExist(backward, backwardTarget);
            VerifyAllTransformBindingsExist(sidestep, sidestepTarget);

            string idleHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            string backwardControllerHashBefore = HashFile(
                BackwardControllerPath);
            string sidestepControllerHashBefore = HashFile(
                SidestepControllerPath);
            string sceneHashBefore = HashFile(scene.path);
            EnsureSourceHash(BackwardSpec());
            EnsureSourceHash(SidestepSpec());
            RootPose backwardRootBefore = new RootPose(backwardTarget);
            RootPose sidestepRootBefore = new RootPose(sidestepTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimatorStates(backwardTarget.parent);

            ArmTargetMetrics backwardMetrics = AlignTargetArms(
                backward,
                backwardTarget,
                BackwardClipPath,
                idle,
                idleTarget);
            ArmTargetMetrics sidestepMetrics = AlignTargetArms(
                sidestep,
                sidestepTarget,
                SidestepClipPath,
                idle,
                idleTarget);
            AssetDatabase.SaveAssets();
            Animator backwardAnimator = backwardTarget.GetComponent<Animator>() ??
                                        throw new InvalidOperationException(
                                            "Player_Crouch_Backward Animator is missing.");
            Animator sidestepAnimator = sidestepTarget.GetComponent<Animator>() ??
                                        throw new InvalidOperationException(
                                            "Player_Crouch_Sidestep Animator is missing.");
            backwardAnimator.Rebind();
            sidestepAnimator.Rebind();

            bool idleUnchanged = string.Equals(
                idleHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.IdleClipPath),
                StringComparison.OrdinalIgnoreCase);
            bool controllersUnchanged = string.Equals(
                    backwardControllerHashBefore,
                    HashFile(BackwardControllerPath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    sidestepControllerHashBefore,
                    HashFile(SidestepControllerPath),
                    StringComparison.OrdinalIgnoreCase);
            bool sceneUnchanged = string.Equals(
                sceneHashBefore,
                HashFile(scene.path),
                StringComparison.OrdinalIgnoreCase);
            EnsureSourceHash(BackwardSpec());
            EnsureSourceHash(SidestepSpec());
            bool sourcesUnchanged = true;
            bool rootsUnchanged =
                RootPoseMatches(backwardTarget, backwardRootBefore) &&
                RootPoseMatches(sidestepTarget, sidestepRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimatorStates(backwardTarget.parent));
            ArmAlignmentApplyMetrics metrics = new ArmAlignmentApplyMetrics
            {
                targetSet = BackwardTargetName + ", " + SidestepTargetName,
                armReference =
                    "Player_Crouch_Idle static bilateral Shoulder/Arm/ForeArm target-relative pose with each source per-frame deviation preserved",
                backward = backwardMetrics,
                sidestep = sidestepMetrics,
                idleClipUnchanged = idleUnchanged,
                controllersUnchanged = controllersUnchanged,
                sceneAssetUnchanged = sceneUnchanged,
                sourceFbxFilesUnchanged = sourcesUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                rootTransformsUnchanged = rootsUnchanged,
                passedNumericChecks = ArmTargetPassed(backwardMetrics) &&
                    ArmTargetPassed(sidestepMetrics) &&
                    idleUnchanged &&
                    controllersUnchanged &&
                    sceneUnchanged &&
                    sourcesUnchanged &&
                    otherAnimatorsUnchanged &&
                    rootsUnchanged,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteArmAlignmentMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch backward and sidestep Idle arm alignment support checks failed." +
                    " BackwardMean=" +
                    Num(backwardMetrics.armMeanDifferenceDegreesMax) +
                    ", BackwardSwing=" +
                    Num(backwardMetrics.armSwingDifferenceDegreesMax) +
                    ", SidestepMean=" +
                    Num(sidestepMetrics.armMeanDifferenceDegreesMax) +
                    ", SidestepSwing=" +
                    Num(sidestepMetrics.armSwingDifferenceDegreesMax) +
                    ", IdleUnchanged=" + idleUnchanged +
                    ", ControllersUnchanged=" + controllersUnchanged +
                    ", SceneUnchanged=" + sceneUnchanged + ".");
            }

            Debug.Log(
                "[PlayerCrouchBackwardSidestep] Aligned both moving arm means to Player_Crouch_Idle." +
                " BackwardMeanDifference=" +
                Num(backwardMetrics.armMeanDifferenceDegreesMax) +
                ", BackwardSwingDifference=" +
                Num(backwardMetrics.armSwingDifferenceDegreesMax) +
                ", SidestepMeanDifference=" +
                Num(sidestepMetrics.armMeanDifferenceDegreesMax) +
                ", SidestepSwingDifference=" +
                Num(sidestepMetrics.armSwingDifferenceDegreesMax) +
                ", ArmBones=6, NonArmCurvesChanged=False" +
                ", IdleChanged=False, ControllersChanged=False, SceneChanged=False" +
                ", TimingChanged=False, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Backward And Sidestep Arm Clearance")]
        internal static void ApplyArmClearance()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch arm clearance.");
            }

            Transform idleTarget = RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.IdleTargetName);
            Transform backwardTarget = RequireTarget(scene, BackwardTargetName);
            Transform sidestepTarget = RequireTarget(scene, SidestepTargetName);
            AnimationClip idle = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            AnimationClip backward = LoadClip(BackwardClipPath);
            AnimationClip sidestep = LoadClip(SidestepClipPath);
            VerifyAllTransformBindingsExist(idle, idleTarget);
            VerifyAllTransformBindingsExist(backward, backwardTarget);
            VerifyAllTransformBindingsExist(sidestep, sidestepTarget);

            string idleHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            string backwardControllerHashBefore = HashFile(
                BackwardControllerPath);
            string sidestepControllerHashBefore = HashFile(
                SidestepControllerPath);
            string sceneHashBefore = HashFile(scene.path);
            EnsureSourceHash(BackwardSpec());
            EnsureSourceHash(SidestepSpec());
            RootPose backwardRootBefore = new RootPose(backwardTarget);
            RootPose sidestepRootBefore = new RootPose(sidestepTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimatorStates(backwardTarget.parent);

            ArmTargetMetrics backwardMetrics = ApplyTargetArmClearance(
                backward,
                backwardTarget,
                BackwardClipPath,
                idle,
                idleTarget);
            ArmTargetMetrics sidestepMetrics = ApplyTargetArmClearance(
                sidestep,
                sidestepTarget,
                SidestepClipPath,
                idle,
                idleTarget);
            AssetDatabase.SaveAssets();
            Animator backwardAnimator = backwardTarget.GetComponent<Animator>() ??
                                        throw new InvalidOperationException(
                                            "Player_Crouch_Backward Animator is missing.");
            Animator sidestepAnimator = sidestepTarget.GetComponent<Animator>() ??
                                        throw new InvalidOperationException(
                                            "Player_Crouch_Sidestep Animator is missing.");
            backwardAnimator.Rebind();
            sidestepAnimator.Rebind();

            bool idleUnchanged = string.Equals(
                idleHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.IdleClipPath),
                StringComparison.OrdinalIgnoreCase);
            bool controllersUnchanged = string.Equals(
                    backwardControllerHashBefore,
                    HashFile(BackwardControllerPath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    sidestepControllerHashBefore,
                    HashFile(SidestepControllerPath),
                    StringComparison.OrdinalIgnoreCase);
            bool sceneUnchanged = string.Equals(
                sceneHashBefore,
                HashFile(scene.path),
                StringComparison.OrdinalIgnoreCase);
            EnsureSourceHash(BackwardSpec());
            EnsureSourceHash(SidestepSpec());
            bool rootsUnchanged =
                RootPoseMatches(backwardTarget, backwardRootBefore) &&
                RootPoseMatches(sidestepTarget, sidestepRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimatorStates(backwardTarget.parent));
            ArmAlignmentApplyMetrics metrics = new ArmAlignmentApplyMetrics
            {
                targetSet = BackwardTargetName + ", " + SidestepTargetName,
                armReference =
                    "Player_Crouch_Idle bilateral Shoulder/Arm/ForeArm target-relative pose plus " +
                    Num(ArmClearanceDegrees) +
                    " degrees outward with each current per-frame deviation preserved",
                backward = backwardMetrics,
                sidestep = sidestepMetrics,
                idleClipUnchanged = idleUnchanged,
                controllersUnchanged = controllersUnchanged,
                sceneAssetUnchanged = sceneUnchanged,
                sourceFbxFilesUnchanged = true,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                rootTransformsUnchanged = rootsUnchanged,
                passedNumericChecks = ArmTargetPassed(backwardMetrics) &&
                    ArmTargetPassed(sidestepMetrics) &&
                    idleUnchanged &&
                    controllersUnchanged &&
                    sceneUnchanged &&
                    otherAnimatorsUnchanged &&
                    rootsUnchanged,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteArmMetrics(metrics, ArmClearanceMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch backward and sidestep arm clearance support checks failed." +
                    " BackwardMean=" +
                    Num(backwardMetrics.armMeanDifferenceDegreesMax) +
                    ", BackwardSwing=" +
                    Num(backwardMetrics.armSwingDifferenceDegreesMax) +
                    ", SidestepMean=" +
                    Num(sidestepMetrics.armMeanDifferenceDegreesMax) +
                    ", SidestepSwing=" +
                    Num(sidestepMetrics.armSwingDifferenceDegreesMax) +
                    ", IdleUnchanged=" + idleUnchanged +
                    ", ControllersUnchanged=" + controllersUnchanged +
                    ", SceneUnchanged=" + sceneUnchanged + ".");
            }

            Debug.Log(
                "[PlayerCrouchBackwardSidestep] Applied bilateral outward arm clearance." +
                " ClearanceDegrees=" + Num(ArmClearanceDegrees) +
                ", BackwardMeanDifference=" +
                Num(backwardMetrics.armMeanDifferenceDegreesMax) +
                ", BackwardSwingDifference=" +
                Num(backwardMetrics.armSwingDifferenceDegreesMax) +
                ", SidestepMeanDifference=" +
                Num(sidestepMetrics.armMeanDifferenceDegreesMax) +
                ", SidestepSwingDifference=" +
                Num(sidestepMetrics.armSwingDifferenceDegreesMax) +
                ", ArmBones=6, NonArmCurvesChanged=False" +
                ", IdleChanged=False, ControllersChanged=False, SceneChanged=False" +
                ", TimingChanged=False, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Moving Knee Side Arm Pose")]
        internal static void ApplyMovingKneeSideArmPose()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before moving crouch knee-side arm apply.");
            }

            Transform forwardTarget = RequireTarget(
                scene,
                PlayerCrouchIdleForwardAnimationTool.ForwardTargetName);
            Transform backwardTarget = RequireTarget(scene, BackwardTargetName);
            Transform sidestepTarget = RequireTarget(scene, SidestepTargetName);
            AnimationClip forward = LoadClip(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            AnimationClip backward = LoadClip(BackwardClipPath);
            AnimationClip sidestep = LoadClip(SidestepClipPath);
            VerifyAllTransformBindingsExist(forward, forwardTarget);
            VerifyAllTransformBindingsExist(backward, backwardTarget);
            VerifyAllTransformBindingsExist(sidestep, sidestepTarget);

            string idleHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            string forwardControllerHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath);
            string backwardControllerHashBefore = HashFile(
                BackwardControllerPath);
            string sidestepControllerHashBefore = HashFile(
                SidestepControllerPath);
            string sceneHashBefore = HashFile(scene.path);
            string forwardSourceHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath);
            EnsureSourceHash(BackwardSpec());
            EnsureSourceHash(SidestepSpec());
            RootPose forwardRootBefore = new RootPose(forwardTarget);
            RootPose backwardRootBefore = new RootPose(backwardTarget);
            RootPose sidestepRootBefore = new RootPose(sidestepTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimatorStates(forwardTarget.parent);

            ArmTargetMetrics forwardMetrics = ApplyTargetKneeSideArms(
                forward,
                forwardTarget,
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            ArmTargetMetrics backwardMetrics = ApplyTargetKneeSideArms(
                backward,
                backwardTarget,
                BackwardClipPath);
            ArmTargetMetrics sidestepMetrics = ApplyTargetKneeSideArms(
                sidestep,
                sidestepTarget,
                SidestepClipPath);
            AssetDatabase.SaveAssets();
            foreach (Transform target in new[]
                     {
                         forwardTarget,
                         backwardTarget,
                         sidestepTarget
                     })
            {
                Animator animator = target.GetComponent<Animator>() ??
                                    throw new InvalidOperationException(
                                        target.name + " Animator is missing.");
                animator.Rebind();
            }

            bool idleUnchanged = string.Equals(
                idleHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.IdleClipPath),
                StringComparison.OrdinalIgnoreCase);
            bool controllersUnchanged = string.Equals(
                    forwardControllerHashBefore,
                    HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardControllerPath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    backwardControllerHashBefore,
                    HashFile(BackwardControllerPath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    sidestepControllerHashBefore,
                    HashFile(SidestepControllerPath),
                    StringComparison.OrdinalIgnoreCase);
            bool sceneUnchanged = string.Equals(
                sceneHashBefore,
                HashFile(scene.path),
                StringComparison.OrdinalIgnoreCase);
            bool sourcesUnchanged = string.Equals(
                forwardSourceHashBefore,
                HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardSourcePath),
                StringComparison.OrdinalIgnoreCase);
            EnsureSourceHash(BackwardSpec());
            EnsureSourceHash(SidestepSpec());
            bool rootsUnchanged =
                RootPoseMatches(forwardTarget, forwardRootBefore) &&
                RootPoseMatches(backwardTarget, backwardRootBefore) &&
                RootPoseMatches(sidestepTarget, sidestepRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimatorStates(forwardTarget.parent));
            ArmAlignmentApplyMetrics metrics = new ArmAlignmentApplyMetrics
            {
                targetSet =
                    PlayerCrouchIdleForwardAnimationTool.ForwardTargetName +
                    ", " + BackwardTargetName + ", " + SidestepTargetName,
                armReference =
                    "Each current bilateral arm chain moved inward to the same-side knee with minimum target-relative hand-to-knee bone gap " +
                    Num(KneeSideMinimumBoneGap) +
                    " meters and current per-frame deviations preserved",
                forward = forwardMetrics,
                backward = backwardMetrics,
                sidestep = sidestepMetrics,
                idleClipUnchanged = idleUnchanged,
                controllersUnchanged = controllersUnchanged,
                sceneAssetUnchanged = sceneUnchanged,
                sourceFbxFilesUnchanged = sourcesUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                rootTransformsUnchanged = rootsUnchanged,
                passedNumericChecks = KneeSideArmTargetPassed(forwardMetrics) &&
                    KneeSideArmTargetPassed(backwardMetrics) &&
                    KneeSideArmTargetPassed(sidestepMetrics) &&
                    idleUnchanged &&
                    controllersUnchanged &&
                    sceneUnchanged &&
                    sourcesUnchanged &&
                    otherAnimatorsUnchanged &&
                    rootsUnchanged,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteArmMetrics(metrics, MovingKneeSideArmMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Moving crouch knee-side arm support checks failed." +
                    " Forward=" + KneeSideArmTargetPassed(forwardMetrics) +
                    ", Backward=" + KneeSideArmTargetPassed(backwardMetrics) +
                    ", Sidestep=" + KneeSideArmTargetPassed(sidestepMetrics) +
                    ", IdleUnchanged=" + idleUnchanged +
                    ", ControllersUnchanged=" + controllersUnchanged +
                    ", SceneUnchanged=" + sceneUnchanged + ".");
            }

            Debug.Log(
                "[PlayerCrouchMovingKneeSideArms] Applied three moving arm poses." +
                " GapTarget=" + Num(KneeSideMinimumBoneGap) +
                ", ForwardLeft=" + Num(forwardMetrics.leftArmAdjustmentDegrees) +
                ", ForwardRight=" + Num(forwardMetrics.rightArmAdjustmentDegrees) +
                ", ForwardGap=" + Num(forwardMetrics.handKneeMinimumBoneGapAfter) +
                ", ForwardSwing=" + Num(forwardMetrics.armSwingDifferenceDegreesMax) +
                ", BackwardLeft=" + Num(backwardMetrics.leftArmAdjustmentDegrees) +
                ", BackwardRight=" + Num(backwardMetrics.rightArmAdjustmentDegrees) +
                ", BackwardGap=" + Num(backwardMetrics.handKneeMinimumBoneGapAfter) +
                ", BackwardSwing=" + Num(backwardMetrics.armSwingDifferenceDegreesMax) +
                ", SidestepLeft=" + Num(sidestepMetrics.leftArmAdjustmentDegrees) +
                ", SidestepRight=" + Num(sidestepMetrics.rightArmAdjustmentDegrees) +
                ", SidestepGap=" + Num(sidestepMetrics.handKneeMinimumBoneGapAfter) +
                ", SidestepSwing=" + Num(sidestepMetrics.armSwingDifferenceDegreesMax) +
                ", NonArmCurvesChanged=False, IdleChanged=False" +
                ", ControllersChanged=False, SceneChanged=False" +
                ", TimingChanged=False, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Backward And Sidestep Left Arms Straight Down")]
        internal static void ApplyLeftArmsStraightDown()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before left-arm straight-down apply.");
            }

            Transform backwardTarget = RequireTarget(scene, BackwardTargetName);
            Transform sidestepTarget = RequireTarget(scene, SidestepTargetName);
            AnimationClip backward = LoadClip(BackwardClipPath);
            AnimationClip sidestep = LoadClip(SidestepClipPath);
            VerifyAllTransformBindingsExist(backward, backwardTarget);
            VerifyAllTransformBindingsExist(sidestep, sidestepTarget);

            string idleHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.IdleClipPath);
            string forwardHashBefore = HashFile(
                PlayerCrouchIdleForwardAnimationTool.ForwardClipPath);
            string backwardControllerHashBefore = HashFile(
                BackwardControllerPath);
            string sidestepControllerHashBefore = HashFile(
                SidestepControllerPath);
            string sceneHashBefore = HashFile(scene.path);
            EnsureSourceHash(BackwardSpec());
            EnsureSourceHash(SidestepSpec());
            RootPose backwardRootBefore = new RootPose(backwardTarget);
            RootPose sidestepRootBefore = new RootPose(sidestepTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimatorStates(backwardTarget.parent);

            LeftArmStraightDownTargetMetrics backwardMetrics =
                ApplyTargetLeftArmStraightDown(
                    backward,
                    backwardTarget,
                    BackwardClipPath);
            LeftArmStraightDownTargetMetrics sidestepMetrics =
                ApplyTargetLeftArmStraightDown(
                    sidestep,
                    sidestepTarget,
                    SidestepClipPath);
            AssetDatabase.SaveAssets();
            foreach (Transform target in new[] { backwardTarget, sidestepTarget })
            {
                Animator animator = target.GetComponent<Animator>() ??
                                    throw new InvalidOperationException(
                                        target.name + " Animator is missing.");
                animator.Rebind();
            }

            bool referenceClipsUnchanged = string.Equals(
                    idleHashBefore,
                    HashFile(PlayerCrouchIdleForwardAnimationTool.IdleClipPath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    forwardHashBefore,
                    HashFile(PlayerCrouchIdleForwardAnimationTool.ForwardClipPath),
                    StringComparison.OrdinalIgnoreCase);
            bool controllersUnchanged = string.Equals(
                    backwardControllerHashBefore,
                    HashFile(BackwardControllerPath),
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    sidestepControllerHashBefore,
                    HashFile(SidestepControllerPath),
                    StringComparison.OrdinalIgnoreCase);
            bool sceneUnchanged = string.Equals(
                sceneHashBefore,
                HashFile(scene.path),
                StringComparison.OrdinalIgnoreCase);
            EnsureSourceHash(BackwardSpec());
            EnsureSourceHash(SidestepSpec());
            bool rootsUnchanged =
                RootPoseMatches(backwardTarget, backwardRootBefore) &&
                RootPoseMatches(sidestepTarget, sidestepRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimatorStates(backwardTarget.parent));
            LeftArmStraightDownApplyMetrics metrics =
                new LeftArmStraightDownApplyMetrics
                {
                    targetSet = BackwardTargetName + ", " + SidestepTargetName,
                    backward = backwardMetrics,
                    sidestep = sidestepMetrics,
                    referenceClipsUnchanged = referenceClipsUnchanged,
                    controllersUnchanged = controllersUnchanged,
                    sceneAssetUnchanged = sceneUnchanged,
                    sourceFbxFilesUnchanged = true,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    rootTransformsUnchanged = rootsUnchanged,
                    passedNumericChecks =
                        LeftArmStraightDownTargetPassed(backwardMetrics) &&
                        LeftArmStraightDownTargetPassed(sidestepMetrics) &&
                        referenceClipsUnchanged &&
                        controllersUnchanged &&
                        sceneUnchanged &&
                        otherAnimatorsUnchanged &&
                        rootsUnchanged,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteLeftArmStraightDownMetrics(metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch backward and sidestep left-arm straight-down support checks failed." +
                    " Backward=" + LeftArmStraightDownTargetPassed(backwardMetrics) +
                    ", Sidestep=" + LeftArmStraightDownTargetPassed(sidestepMetrics) +
                    ", References=" + referenceClipsUnchanged +
                    ", Controllers=" + controllersUnchanged +
                    ", Scene=" + sceneUnchanged + ".");
            }

            Debug.Log(
                "[PlayerCrouchLeftArmsStraightDown] Applied to two targets." +
                " BackwardBendBefore=" +
                Num(backwardMetrics.leftElbowBendDegreesMaxBefore) +
                ", BackwardBendAfter=" +
                Num(backwardMetrics.leftElbowBendDegreesMaxAfter) +
                ", BackwardDownMean=" +
                Num(backwardMetrics.leftArmDownwardMeanAngleDegreesAfter) +
                ", BackwardDownMax=" +
                Num(backwardMetrics.leftArmDownwardMaximumAngleDegreesAfter) +
                ", BackwardGap=" +
                Num(backwardMetrics.leftHandKneeMinimumBoneGapAfter) +
                ", SidestepBendBefore=" +
                Num(sidestepMetrics.leftElbowBendDegreesMaxBefore) +
                ", SidestepBendAfter=" +
                Num(sidestepMetrics.leftElbowBendDegreesMaxAfter) +
                ", SidestepDownMean=" +
                Num(sidestepMetrics.leftArmDownwardMeanAngleDegreesAfter) +
                ", SidestepDownMax=" +
                Num(sidestepMetrics.leftArmDownwardMaximumAngleDegreesAfter) +
                ", SidestepGap=" +
                Num(sidestepMetrics.leftHandKneeMinimumBoneGapAfter) +
                ", RightArmCurvesChanged=False, NonLeftArmCurvesChanged=False" +
                ", TimingChanged=False, ApplyRootMotion=False.");
        }

        internal static Transform RequireTarget(Scene scene, string targetName)
        {
            Transform[] layoutRoots = scene.GetRootGameObjects()
                .Where(root => root.name == LayoutRootName)
                .Select(root => root.transform)
                .ToArray();
            if (layoutRoots.Length != 1)
            {
                throw new InvalidOperationException(
                    "PlayerAnimationLayout root count differs.");
            }

            Transform[] targets = Enumerable.Range(0, layoutRoots[0].childCount)
                .Select(layoutRoots[0].GetChild)
                .Where(child => child.name == targetName)
                .ToArray();
            if (targets.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + targetName + "; found " +
                    targets.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return targets[0];
        }

        internal static Transform FindUniqueBone(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    StripNamespace(item.name),
                    name,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + name + " under " + root.name +
                    "; found " + matches.Length.ToString(
                        CultureInfo.InvariantCulture) + ".");
            }

            return matches[0];
        }

        internal static Quaternion ArmClearanceRotation(
            Transform referenceTarget,
            string boneName)
        {
            string shoulderName;
            if (boneName.StartsWith("Left", StringComparison.Ordinal))
            {
                shoulderName = "LeftShoulder";
            }
            else if (boneName.StartsWith("Right", StringComparison.Ordinal))
            {
                shoulderName = "RightShoulder";
            }
            else
            {
                throw new InvalidOperationException(
                    "Arm clearance requires a left or right arm bone: " +
                    boneName + ".");
            }

            Transform shoulder = FindUniqueBone(referenceTarget, shoulderName);
            float lateral = referenceTarget
                .InverseTransformPoint(shoulder.position).x;
            if (Mathf.Abs(lateral) <= 0.0001f)
            {
                throw new InvalidOperationException(
                    shoulderName + " has no usable lateral side for arm clearance.");
            }

            return Quaternion.AngleAxis(
                Mathf.Sign(lateral) * ArmClearanceDegrees,
                Vector3.forward);
        }

        private static LeftArmStraightDownTargetMetrics
            ApplyTargetLeftArmStraightDown(
                AnimationClip clip,
                Transform target,
                string clipPath)
        {
            float durationBefore = clip.length;
            float frameRateBefore = clip.frameRate;
            Dictionary<EditorCurveBinding, AnimationCurve> curvesBefore =
                CaptureCurves(clip);
            float[] times = FrameTimes(
                clip.length,
                clip.frameRate,
                includeEnd: true);
            Quaternion[] shoulderBefore = SampleTargetRelativeRotations(
                clip,
                target,
                "LeftShoulder",
                times);
            Quaternion[] upperArmBefore = SampleTargetRelativeRotations(
                clip,
                target,
                "LeftArm",
                times);
            float bendBefore = LeftElbowBendMaximum(clip, target, times);
            float downwardMeanBefore = LeftArmDownwardMeanAngle(
                clip,
                target,
                times);
            float gapBefore = HandKneeMinimumGap(
                clip,
                target,
                times,
                "LeftHand",
                "LeftLeg");

            Quaternion[] straightForeArmLocals =
                SampleStraightLeftForeArmLocalRotations(clip, target, times);
            AnimationClip adjusted = UnityEngine.Object.Instantiate(clip);
            adjusted.name = clip.name;
            adjusted.hideFlags = HideFlags.None;
            ReplaceRotationWithQuaternionCurves(
                adjusted,
                BonePath(target, "LeftForeArm"),
                times,
                straightForeArmLocals);

            Quaternion downwardRotation = FindLeftArmDownwardRotation(
                adjusted,
                target,
                times);
            ApplyTargetRelativeArmRotationToClip(
                adjusted,
                target,
                times,
                new[] { "LeftShoulder", "LeftArm", "LeftForeArm" },
                downwardRotation);

            float clearanceAdjustment = 0f;
            float straightGap = HandKneeMinimumGap(
                adjusted,
                target,
                times,
                "LeftHand",
                "LeftLeg");
            if (straightGap < KneeSideMinimumBoneGap - KneeSideGapTolerance)
            {
                clearanceAdjustment = FindKneeSideArmAdjustment(
                    adjusted,
                    target,
                    times,
                    "LeftShoulder",
                    "LeftHand",
                    "LeftLeg",
                    out _);
                ApplyTargetRelativeArmRotationToClip(
                    adjusted,
                    target,
                    times,
                    new[] { "LeftShoulder", "LeftArm", "LeftForeArm" },
                    Quaternion.AngleAxis(
                        clearanceAdjustment,
                        Vector3.forward));
            }

            SaveOverExisting(adjusted, clipPath);
            UnityEngine.Object.DestroyImmediate(adjusted);
            AnimationClip after = LoadClip(clipPath);
            Quaternion[] shoulderAfter = SampleTargetRelativeRotations(
                after,
                target,
                "LeftShoulder",
                times);
            Quaternion[] upperArmAfter = SampleTargetRelativeRotations(
                after,
                target,
                "LeftArm",
                times);
            float bendAfter = LeftElbowBendMaximum(after, target, times);
            float downwardMeanAfter = LeftArmDownwardMeanAngle(
                after,
                target,
                times);
            float downwardMaximumAfter = LeftArmDownwardMaximumAngle(
                after,
                target,
                times);
            float gapAfter = HandKneeMinimumGap(
                after,
                target,
                times,
                "LeftHand",
                "LeftLeg");
            HashSet<string> leftArmPaths = new HashSet<string>(
                new[] { "LeftShoulder", "LeftArm", "LeftForeArm" }
                    .Select(boneName => BonePath(target, boneName)),
                StringComparer.Ordinal);
            bool outsideLeftArmUnchanged = VerifyOutsidePathsUnchanged(
                curvesBefore,
                after,
                leftArmPaths);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    target.name + " Animator is missing.");
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(after);
            return new LeftArmStraightDownTargetMetrics
            {
                target = target.name,
                clipPath = clipPath,
                leftElbowBendDegreesMaxBefore = bendBefore,
                leftElbowBendDegreesMaxAfter = bendAfter,
                leftArmDownwardMeanAngleDegreesBefore = downwardMeanBefore,
                leftArmDownwardMeanAngleDegreesAfter = downwardMeanAfter,
                leftArmDownwardMaximumAngleDegreesAfter = downwardMaximumAfter,
                leftHandKneeMinimumBoneGapTarget = KneeSideMinimumBoneGap,
                leftHandKneeMinimumBoneGapBefore = gapBefore,
                leftHandKneeMinimumBoneGapAfter = gapAfter,
                leftArmClearanceAdjustmentDegrees = clearanceAdjustment,
                leftShoulderSwingDifferenceDegreesMax = SwingDifference(
                    shoulderBefore,
                    shoulderAfter),
                leftUpperArmSwingDifferenceDegreesMax = SwingDifference(
                    upperArmBefore,
                    upperArmAfter),
                curvesOutsideLeftArmUnchanged = outsideLeftArmUnchanged,
                rightArmCurvesUnchanged = outsideLeftArmUnchanged,
                leftArmFrameTimingUnchanged =
                    shoulderAfter.Length == times.Length &&
                    upperArmAfter.Length == times.Length,
                clipTimingUnchanged =
                    Mathf.Abs(after.length - durationBefore) <= CurveTolerance &&
                    Mathf.Abs(after.frameRate - frameRateBefore) <= CurveTolerance,
                clipIsLooping = settings.loopTime && !settings.loopBlend,
                applyRootMotion = animator.applyRootMotion
            };
        }

        private static bool LeftArmStraightDownTargetPassed(
            LeftArmStraightDownTargetMetrics metrics)
        {
            return metrics != null &&
                   metrics.leftElbowBendDegreesMaxAfter <=
                       LeftElbowStraightToleranceDegrees &&
                   metrics.leftArmDownwardMeanAngleDegreesAfter <=
                       LeftArmDownwardMeanToleranceDegrees &&
                   metrics.leftArmDownwardMaximumAngleDegreesAfter <=
                       LeftArmDownwardMaximumToleranceDegrees &&
                   Mathf.Abs(
                       metrics.leftHandKneeMinimumBoneGapTarget -
                       KneeSideMinimumBoneGap) <= CurveTolerance &&
                   metrics.leftHandKneeMinimumBoneGapAfter >=
                       KneeSideMinimumBoneGap - KneeSideGapTolerance &&
                   metrics.leftShoulderSwingDifferenceDegreesMax <=
                       ArmSwingToleranceDegrees &&
                   metrics.leftUpperArmSwingDifferenceDegreesMax <=
                       ArmSwingToleranceDegrees &&
                   metrics.curvesOutsideLeftArmUnchanged &&
                   metrics.rightArmCurvesUnchanged &&
                   metrics.leftArmFrameTimingUnchanged &&
                   metrics.clipTimingUnchanged &&
                   metrics.clipIsLooping &&
                   !metrics.applyRootMotion;
        }

        private static ArmTargetMetrics ApplyTargetKneeSideArms(
            AnimationClip clip,
            Transform target,
            string clipPath)
        {
            float durationBefore = clip.length;
            float frameRateBefore = clip.frameRate;
            Dictionary<EditorCurveBinding, AnimationCurve> curvesBefore =
                CaptureCurves(clip);
            float[] times = FrameTimes(
                clip.length,
                clip.frameRate,
                includeEnd: true);
            Dictionary<string, Quaternion[]> rotationsBefore =
                SampleTargetRelativeArmRotations(clip, target, times);
            Dictionary<string, Quaternion[]> desired = rotationsBefore
                .ToDictionary(
                    item => item.Key,
                    item => item.Value.ToArray(),
                    StringComparer.Ordinal);

            float leftAdjustment = FindKneeSideArmAdjustment(
                clip,
                target,
                times,
                "LeftShoulder",
                "LeftHand",
                "LeftLeg",
                out float leftGapBefore);
            float rightAdjustment = FindKneeSideArmAdjustment(
                clip,
                target,
                times,
                "RightShoulder",
                "RightHand",
                "RightLeg",
                out float rightGapBefore);
            ApplyConstantTargetRotation(
                desired,
                new[] { "LeftShoulder", "LeftArm", "LeftForeArm" },
                leftAdjustment);
            ApplyConstantTargetRotation(
                desired,
                new[] { "RightShoulder", "RightArm", "RightForeArm" },
                rightAdjustment);
            Dictionary<string, Quaternion> desiredMeans = ArmBones.ToDictionary(
                boneName => boneName,
                boneName => QuaternionMean(
                    desired[boneName].Take(desired[boneName].Length - 1)),
                StringComparer.Ordinal);

            Dictionary<string, Quaternion[]> desiredLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    clip,
                    target,
                    times,
                    desired);
            AnimationClip adjusted = UnityEngine.Object.Instantiate(clip);
            adjusted.name = clip.name;
            adjusted.hideFlags = HideFlags.None;
            foreach (string boneName in ArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    adjusted,
                    BonePath(target, boneName),
                    times,
                    desiredLocals[boneName]);
            }

            SaveOverExisting(adjusted, clipPath);
            UnityEngine.Object.DestroyImmediate(adjusted);
            AnimationClip after = LoadClip(clipPath);
            Dictionary<string, Quaternion[]> rotationsAfter =
                SampleTargetRelativeArmRotations(after, target, times);
            float meanDifference = ArmBones.Max(boneName =>
                Quaternion.Angle(
                    desiredMeans[boneName],
                    QuaternionMean(
                        rotationsAfter[boneName]
                            .Take(rotationsAfter[boneName].Length - 1))));
            float swingDifference = ArmBones.Max(boneName =>
                SwingDifference(
                    rotationsBefore[boneName],
                    rotationsAfter[boneName]));
            float gapAfter = HandKneeMinimumGap(after, target, times);

            HashSet<string> armPaths = new HashSet<string>(
                ArmBones.Select(boneName => BonePath(target, boneName)),
                StringComparer.Ordinal);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    target.name + " Animator is missing.");
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(after);
            return new ArmTargetMetrics
            {
                target = target.name,
                clipPath = clipPath,
                armBonesChanged = ArmBones.Length,
                armMeanDifferenceDegreesMax = meanDifference,
                armSwingDifferenceDegreesMax = swingDifference,
                leftArmAdjustmentDegrees = leftAdjustment,
                rightArmAdjustmentDegrees = rightAdjustment,
                handKneeMinimumBoneGapTarget = KneeSideMinimumBoneGap,
                handKneeMinimumBoneGapBefore = Mathf.Min(
                    leftGapBefore,
                    rightGapBefore),
                handKneeMinimumBoneGapAfter = gapAfter,
                nonArmCurvesUnchanged = VerifyOutsidePathsUnchanged(
                    curvesBefore,
                    after,
                    armPaths),
                armFrameTimingUnchanged = times.Length ==
                    rotationsAfter[ArmBones[0]].Length,
                clipTimingUnchanged =
                    Mathf.Abs(after.length - durationBefore) <= CurveTolerance &&
                    Mathf.Abs(after.frameRate - frameRateBefore) <= CurveTolerance,
                clipIsLooping = settings.loopTime && !settings.loopBlend,
                applyRootMotion = animator.applyRootMotion
            };
        }

        private static float FindKneeSideArmAdjustment(
            AnimationClip clip,
            Transform target,
            float[] times,
            string shoulderName,
            string handName,
            string kneeName,
            out float minimumGapBefore)
        {
            Vector3[] shoulders = SampleTargetRelativePositions(
                clip,
                target,
                shoulderName,
                times);
            Vector3[] hands = SampleTargetRelativePositions(
                clip,
                target,
                handName,
                times);
            Vector3[] knees = SampleTargetRelativePositions(
                clip,
                target,
                kneeName,
                times);
            int sampleCount = times.Length - 1;
            float sideSign = Mathf.Sign(
                knees.Take(sampleCount).Average(position => position.x));
            if (Mathf.Abs(sideSign) < 0.5f)
            {
                throw new InvalidOperationException(
                    target.name + "/" + kneeName +
                    " has no usable lateral side for knee-side arm placement.");
            }

            minimumGapBefore = Enumerable.Range(0, sampleCount).Min(index =>
                sideSign * (hands[index].x - knees[index].x));
            float bestAngle = float.NaN;
            float bestScore = float.PositiveInfinity;
            float maximumFeasibleGap = float.NegativeInfinity;
            float maximumFeasibleGapAngle = 0f;
            int steps = Mathf.RoundToInt(
                KneeSideAngleSearchLimit / KneeSideAngleSearchStep);
            for (int step = -steps; step <= steps; step++)
            {
                float angle = step * KneeSideAngleSearchStep;
                Quaternion rotation = Quaternion.AngleAxis(
                    angle,
                    Vector3.forward);
                float minimumGap = float.PositiveInfinity;
                float averageGap = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    Vector3 rotatedHand = shoulders[index] +
                                          rotation *
                                          (hands[index] - shoulders[index]);
                    float gap = sideSign *
                                (rotatedHand.x - knees[index].x);
                    minimumGap = Mathf.Min(minimumGap, gap);
                    averageGap += gap;
                }

                if (minimumGap > maximumFeasibleGap)
                {
                    maximumFeasibleGap = minimumGap;
                    maximumFeasibleGapAngle = angle;
                }

                if (minimumGap <
                    KneeSideMinimumBoneGap - KneeSideGapTolerance)
                {
                    continue;
                }

                averageGap /= sampleCount;
                float score =
                    Mathf.Abs(minimumGap - KneeSideMinimumBoneGap) * 100f +
                    Mathf.Abs(averageGap - KneeSideMinimumBoneGap) +
                    Mathf.Abs(angle) * 0.0001f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestAngle = angle;
                }
            }

            if (float.IsNaN(bestAngle))
            {
                throw new InvalidOperationException(
                    target.name + "/" + handName +
                    " could not reach the approved same-side knee gap within " +
                    Num(KneeSideAngleSearchLimit) + " degrees." +
                    " GapBefore=" + Num(minimumGapBefore) +
                    ", MaximumFeasibleGap=" + Num(maximumFeasibleGap) +
                    ", MaximumFeasibleGapAngle=" +
                    Num(maximumFeasibleGapAngle) + ".");
            }

            return bestAngle;
        }

        private static void ApplyConstantTargetRotation(
            Dictionary<string, Quaternion[]> desired,
            IEnumerable<string> boneNames,
            float degrees)
        {
            ApplyConstantTargetRotation(
                desired,
                boneNames,
                Quaternion.AngleAxis(degrees, Vector3.forward));
        }

        private static void ApplyConstantTargetRotation(
            Dictionary<string, Quaternion[]> desired,
            IEnumerable<string> boneNames,
            Quaternion offset)
        {
            foreach (string boneName in boneNames)
            {
                desired[boneName] = desired[boneName]
                    .Select(rotation => offset * rotation)
                    .ToArray();
            }
        }

        private static void ApplyTargetRelativeArmRotationToClip(
            AnimationClip clip,
            Transform target,
            float[] times,
            IEnumerable<string> boneNames,
            Quaternion offset)
        {
            string[] names = boneNames.ToArray();
            Dictionary<string, Quaternion[]> current =
                SampleTargetRelativeArmRotations(clip, target, times);
            Dictionary<string, Quaternion[]> desired = current.ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.Ordinal);
            ApplyConstantTargetRotation(desired, names, offset);
            Dictionary<string, Quaternion[]> desiredLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    clip,
                    target,
                    times,
                    desired);
            foreach (string boneName in names)
            {
                ReplaceRotationWithQuaternionCurves(
                    clip,
                    BonePath(target, boneName),
                    times,
                    desiredLocals[boneName]);
            }
        }

        private static float HandKneeMinimumGap(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            return Mathf.Min(
                HandKneeMinimumGap(
                    clip,
                    target,
                    times,
                    "LeftHand",
                    "LeftLeg"),
                HandKneeMinimumGap(
                    clip,
                    target,
                    times,
                    "RightHand",
                    "RightLeg"));
        }

        private static float HandKneeMinimumGap(
            AnimationClip clip,
            Transform target,
            float[] times,
            string handName,
            string kneeName)
        {
            Vector3[] hands = SampleTargetRelativePositions(
                clip,
                target,
                handName,
                times);
            Vector3[] knees = SampleTargetRelativePositions(
                clip,
                target,
                kneeName,
                times);
            int sampleCount = times.Length - 1;
            float sideSign = Mathf.Sign(
                knees.Take(sampleCount).Average(position => position.x));
            return Enumerable.Range(0, sampleCount).Min(index =>
                sideSign * (hands[index].x - knees[index].x));
        }

        private static bool KneeSideArmTargetPassed(ArmTargetMetrics metrics)
        {
            return metrics != null &&
                   metrics.armBonesChanged == ArmBones.Length &&
                   Mathf.Abs(
                       metrics.handKneeMinimumBoneGapTarget -
                       KneeSideMinimumBoneGap) <= CurveTolerance &&
                   metrics.handKneeMinimumBoneGapAfter >=
                       KneeSideMinimumBoneGap - KneeSideGapTolerance &&
                   metrics.armMeanDifferenceDegreesMax <=
                       ArmMeanToleranceDegrees &&
                   metrics.armSwingDifferenceDegreesMax <=
                       ArmSwingToleranceDegrees &&
                   metrics.nonArmCurvesUnchanged &&
                   metrics.armFrameTimingUnchanged &&
                   metrics.clipTimingUnchanged &&
                   metrics.clipIsLooping &&
                   !metrics.applyRootMotion;
        }

        private static ArmTargetMetrics ApplyTargetArmClearance(
            AnimationClip clip,
            Transform target,
            string clipPath,
            AnimationClip idle,
            Transform idleTarget)
        {
            float durationBefore = clip.length;
            float frameRateBefore = clip.frameRate;
            Dictionary<EditorCurveBinding, AnimationCurve> curvesBefore =
                CaptureCurves(clip);
            float[] times = FrameTimes(
                clip.length,
                clip.frameRate,
                includeEnd: true);
            Dictionary<string, Quaternion[]> rotationsBefore =
                SampleTargetRelativeArmRotations(clip, target, times);
            Dictionary<string, Quaternion> targetMeans =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
            Dictionary<string, Quaternion[]> desired =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            foreach (string boneName in ArmBones)
            {
                Quaternion idlePose = SampleTargetRelativeRotations(
                    idle,
                    idleTarget,
                    boneName,
                    new[] { idle.length * 0.5f })[0];
                Quaternion targetMean =
                    ArmClearanceRotation(idleTarget, boneName) * idlePose;
                Quaternion currentMean = QuaternionMean(
                    rotationsBefore[boneName]
                        .Take(rotationsBefore[boneName].Length - 1));
                Quaternion meanOffset = targetMean *
                                        Quaternion.Inverse(currentMean);
                targetMeans.Add(boneName, targetMean);
                desired.Add(
                    boneName,
                    rotationsBefore[boneName]
                        .Select(rotation => meanOffset * rotation)
                        .ToArray());
            }

            Dictionary<string, Quaternion[]> desiredLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    clip,
                    target,
                    times,
                    desired);
            AnimationClip adjusted = UnityEngine.Object.Instantiate(clip);
            adjusted.name = clip.name;
            adjusted.hideFlags = HideFlags.None;
            foreach (string boneName in ArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    adjusted,
                    BonePath(target, boneName),
                    times,
                    desiredLocals[boneName]);
            }

            SaveOverExisting(adjusted, clipPath);
            UnityEngine.Object.DestroyImmediate(adjusted);
            AnimationClip after = LoadClip(clipPath);
            Dictionary<string, Quaternion[]> rotationsAfter =
                SampleTargetRelativeArmRotations(after, target, times);
            float meanDifference = 0f;
            float swingDifference = 0f;
            foreach (string boneName in ArmBones)
            {
                Quaternion actualMean = QuaternionMean(
                    rotationsAfter[boneName]
                        .Take(rotationsAfter[boneName].Length - 1));
                meanDifference = Mathf.Max(
                    meanDifference,
                    Quaternion.Angle(targetMeans[boneName], actualMean));
                swingDifference = Mathf.Max(
                    swingDifference,
                    SwingDifference(
                        rotationsBefore[boneName],
                        rotationsAfter[boneName]));
            }

            HashSet<string> armPaths = new HashSet<string>(
                ArmBones.Select(boneName => BonePath(target, boneName)),
                StringComparer.Ordinal);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    target.name + " Animator is missing.");
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(after);
            return new ArmTargetMetrics
            {
                target = target.name,
                clipPath = clipPath,
                armBonesChanged = ArmBones.Length,
                armMeanDifferenceDegreesMax = meanDifference,
                armSwingDifferenceDegreesMax = swingDifference,
                nonArmCurvesUnchanged = VerifyOutsidePathsUnchanged(
                    curvesBefore,
                    after,
                    armPaths),
                armFrameTimingUnchanged = times.Length ==
                    rotationsAfter[ArmBones[0]].Length,
                clipTimingUnchanged =
                    Mathf.Abs(after.length - durationBefore) <= CurveTolerance &&
                    Mathf.Abs(after.frameRate - frameRateBefore) <= CurveTolerance,
                clipIsLooping = settings.loopTime && !settings.loopBlend,
                applyRootMotion = animator.applyRootMotion
            };
        }

        private static ArmTargetMetrics AlignTargetArms(
            AnimationClip clip,
            Transform target,
            string clipPath,
            AnimationClip idle,
            Transform idleTarget)
        {
            float durationBefore = clip.length;
            float frameRateBefore = clip.frameRate;
            Dictionary<EditorCurveBinding, AnimationCurve> curvesBefore =
                CaptureCurves(clip);
            float[] times = FrameTimes(
                clip.length,
                clip.frameRate,
                includeEnd: true);
            Dictionary<string, Quaternion[]> rotationsBefore =
                SampleTargetRelativeArmRotations(clip, target, times);
            Dictionary<string, Quaternion[]> desired =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            foreach (string boneName in ArmBones)
            {
                Quaternion idlePose = SampleTargetRelativeRotations(
                    idle,
                    idleTarget,
                    boneName,
                    new[] { idle.length * 0.5f })[0];
                Quaternion currentMean = QuaternionMean(
                    rotationsBefore[boneName]
                        .Take(rotationsBefore[boneName].Length - 1));
                Quaternion meanOffset = idlePose *
                                        Quaternion.Inverse(currentMean);
                desired.Add(
                    boneName,
                    rotationsBefore[boneName]
                        .Select(rotation => meanOffset * rotation)
                        .ToArray());
            }

            Dictionary<string, Quaternion[]> desiredLocals =
                ConvertTargetRelativeArmRotationsToLocal(
                    clip,
                    target,
                    times,
                    desired);
            AnimationClip aligned = UnityEngine.Object.Instantiate(clip);
            aligned.name = clip.name;
            aligned.hideFlags = HideFlags.None;
            foreach (string boneName in ArmBones)
            {
                ReplaceRotationWithQuaternionCurves(
                    aligned,
                    BonePath(target, boneName),
                    times,
                    desiredLocals[boneName]);
            }

            SaveOverExisting(aligned, clipPath);
            UnityEngine.Object.DestroyImmediate(aligned);
            AnimationClip after = LoadClip(clipPath);
            Dictionary<string, Quaternion[]> rotationsAfter =
                SampleTargetRelativeArmRotations(after, target, times);
            float meanDifference = 0f;
            float swingDifference = 0f;
            foreach (string boneName in ArmBones)
            {
                Quaternion idlePose = SampleTargetRelativeRotations(
                    idle,
                    idleTarget,
                    boneName,
                    new[] { idle.length * 0.5f })[0];
                Quaternion actualMean = QuaternionMean(
                    rotationsAfter[boneName]
                        .Take(rotationsAfter[boneName].Length - 1));
                meanDifference = Mathf.Max(
                    meanDifference,
                    Quaternion.Angle(idlePose, actualMean));
                swingDifference = Mathf.Max(
                    swingDifference,
                    SwingDifference(
                        rotationsBefore[boneName],
                        rotationsAfter[boneName]));
            }

            HashSet<string> armPaths = new HashSet<string>(
                ArmBones.Select(boneName => BonePath(target, boneName)),
                StringComparer.Ordinal);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    target.name + " Animator is missing.");
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(after);
            return new ArmTargetMetrics
            {
                target = target.name,
                clipPath = clipPath,
                armBonesChanged = ArmBones.Length,
                armMeanDifferenceDegreesMax = meanDifference,
                armSwingDifferenceDegreesMax = swingDifference,
                nonArmCurvesUnchanged = VerifyOutsidePathsUnchanged(
                    curvesBefore,
                    after,
                    armPaths),
                armFrameTimingUnchanged = times.Length ==
                    rotationsAfter[ArmBones[0]].Length,
                clipTimingUnchanged =
                    Mathf.Abs(after.length - durationBefore) <= CurveTolerance &&
                    Mathf.Abs(after.frameRate - frameRateBefore) <= CurveTolerance,
                clipIsLooping = settings.loopTime && !settings.loopBlend,
                applyRootMotion = animator.applyRootMotion
            };
        }

        private static bool ArmTargetPassed(ArmTargetMetrics metrics)
        {
            return metrics != null &&
                   metrics.armBonesChanged == ArmBones.Length &&
                   metrics.armMeanDifferenceDegreesMax <=
                       ArmMeanToleranceDegrees &&
                   metrics.armSwingDifferenceDegreesMax <=
                       ArmSwingToleranceDegrees &&
                   metrics.nonArmCurvesUnchanged &&
                   metrics.armFrameTimingUnchanged &&
                   metrics.clipTimingUnchanged &&
                   metrics.clipIsLooping &&
                   !metrics.applyRootMotion;
        }

        private static TargetMetrics ApplyTarget(TargetSpec spec, Transform target)
        {
            AnimationClip source = LoadSingleSourceClip(spec);
            VerifyAllTransformBindingsExist(source, target);
            AnimationClip derived = CreateOrUpdateInPlaceClip(
                source,
                target,
                spec,
                out CarrierSelection carrier,
                out EditorCurveBinding[] changedBindings,
                out bool nonHorizontalUnchanged,
                out bool timingUnchanged);
            AnimatorController controller = CreateOrUpdateController(
                spec.ControllerPath,
                spec.StateName,
                derived);
            Animator animator = ConfigureAnimator(target, controller);
            AssertAnimator(animator, controller, derived);
            return new TargetMetrics
            {
                target = spec.TargetName,
                sourcePath = spec.SourcePath,
                sourceHash = spec.ExpectedHash,
                sourceTake = ExpectedTakeName,
                derivedClipPath = spec.ClipPath,
                controllerPath = spec.ControllerPath,
                carrierPath = carrier.Bindings[0].path,
                horizontalProperties = string.Join(",", carrier.HorizontalProperties),
                verticalProperty = carrier.VerticalProperty,
                clipDurationSeconds = derived.length,
                clipFrameRate = derived.frameRate,
                sourceCurveBindings = AnimationUtility.GetCurveBindings(source).Length,
                changedHorizontalBindings = changedBindings.Length,
                nonHorizontalCurvesUnchanged = nonHorizontalUnchanged,
                clipTimingUnchanged = timingUnchanged,
                clipIsLooping = derived.isLooping,
                applyRootMotion = animator.applyRootMotion
            };
        }

        private static bool TargetPassed(TargetMetrics metrics)
        {
            return metrics != null &&
                   metrics.sourceTake == ExpectedTakeName &&
                   metrics.sourceCurveBindings > 0 &&
                   metrics.changedHorizontalBindings > 0 &&
                   metrics.changedHorizontalBindings <= 2 &&
                   metrics.nonHorizontalCurvesUnchanged &&
                   metrics.clipTimingUnchanged &&
                   metrics.clipIsLooping &&
                   !metrics.applyRootMotion;
        }

        private static void ConfigureImporter(TargetSpec spec)
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(spec.SourcePath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    spec.TargetName + " ModelImporter is missing.");
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1 ||
                !string.Equals(
                    clips[0].takeName,
                    ExpectedTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    spec.SourcePath + " must expose exactly one Take named '" +
                    ExpectedTakeName + "'.");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            clips[0].name = clips[0].takeName;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadSingleSourceClip(TargetSpec spec)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(
                    spec.SourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    clips[0].name,
                    ExpectedTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Unity does not expose exactly one " + spec.TargetName +
                    " clip named '" + ExpectedTakeName + "'.");
            }

            return clips[0];
        }

        private static AnimationClip CreateOrUpdateInPlaceClip(
            AnimationClip source,
            Transform target,
            TargetSpec spec,
            out CarrierSelection carrier,
            out EditorCurveBinding[] changedBindings,
            out bool nonHorizontalUnchanged,
            out bool timingUnchanged)
        {
            AnimationClip clone = UnityEngine.Object.Instantiate(source);
            clone.name = spec.ClipName;
            clone.hideFlags = HideFlags.None;
            Dictionary<EditorCurveBinding, AnimationCurve> sourceCurves =
                AnimationUtility.GetCurveBindings(source)
                    .ToDictionary(
                        binding => binding,
                        binding => AnimationUtility.GetEditorCurve(
                            source,
                            binding));
            EditorCurveBinding[] sourceObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(source);
            carrier = SelectCarrier(source, target);
            HashSet<string> horizontalProperties = new HashSet<string>(
                carrier.HorizontalProperties,
                StringComparer.Ordinal);
            List<EditorCurveBinding> changed = new List<EditorCurveBinding>();

            foreach (EditorCurveBinding binding in carrier.Bindings)
            {
                if (!horizontalProperties.Contains(binding.propertyName))
                {
                    continue;
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    clone,
                    binding);
                if (curve == null || curve.length == 0 ||
                    CurveRange(curve) <= CurveTolerance)
                {
                    continue;
                }

                Keyframe[] keys = curve.keys;
                float lockedValue = keys[0].value;
                for (int index = 0; index < keys.Length; index++)
                {
                    Keyframe key = keys[index];
                    key.value = lockedValue;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    keys[index] = key;
                }

                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clone, binding, curve);
                changed.Add(binding);
            }

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clone);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clone, settings);
            changedBindings = changed.ToArray();
            VerifyDerivedClip(
                source,
                clone,
                sourceCurves,
                sourceObjectBindings,
                changedBindings,
                carrier);
            nonHorizontalUnchanged = true;
            timingUnchanged =
                Mathf.Abs(source.length - clone.length) <= CurveTolerance &&
                Mathf.Abs(source.frameRate - clone.frameRate) <= CurveTolerance;
            return SaveClip(clone, spec.ClipPath);
        }

        private static CarrierSelection SelectCarrier(
            AnimationClip source,
            Transform target)
        {
            var groups = AnimationUtility.GetCurveBindings(source)
                .Where(binding => binding.type == typeof(Transform) &&
                    IsPositionProperty(binding.propertyName))
                .GroupBy(binding => binding.path)
                .Select(group => new
                {
                    Bindings = group.ToArray(),
                    Transform = string.IsNullOrEmpty(group.Key)
                        ? target
                        : target.Find(group.Key)
                })
                .Where(item => item.Transform != null &&
                    string.Equals(
                        StripNamespace(item.Transform.name),
                        "Hips",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var moving = groups.Where(item => item.Bindings.Any(binding =>
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    source,
                    binding);
                return curve != null && CurveRange(curve) > CurveTolerance;
            })).ToArray();
            if (moving.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one animated Hips position carrier for " +
                    target.name + "; found " +
                    moving.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            Transform parent = moving[0].Transform.parent;
            if (parent == null)
            {
                throw new InvalidOperationException(
                    target.name + " Hips has no parent for direct world-axis mapping.");
            }

            var axes = new[]
            {
                new
                {
                    Property = "m_LocalPosition.x",
                    Direction = parent.TransformDirection(Vector3.right).normalized
                },
                new
                {
                    Property = "m_LocalPosition.y",
                    Direction = parent.TransformDirection(Vector3.up).normalized
                },
                new
                {
                    Property = "m_LocalPosition.z",
                    Direction = parent.TransformDirection(Vector3.forward).normalized
                }
            };
            var vertical = axes
                .OrderByDescending(axis => Mathf.Abs(
                    Vector3.Dot(axis.Direction, Vector3.up)))
                .First();
            float verticalDot = Mathf.Abs(
                Vector3.Dot(vertical.Direction, Vector3.up));
            string[] horizontal = axes
                .Where(axis => axis.Property != vertical.Property)
                .Where(axis => Mathf.Abs(
                    Vector3.Dot(axis.Direction, Vector3.up)) < 0.1f)
                .Select(axis => axis.Property)
                .ToArray();
            string[] available = moving[0].Bindings
                .Select(binding => binding.propertyName)
                .ToArray();
            if (verticalDot < 0.9f || horizontal.Length != 2 ||
                horizontal.Any(property => !available.Contains(property)))
            {
                throw new InvalidOperationException(
                    target.name +
                    " Hips axes are not clear enough for direct horizontal lock without inference.");
            }

            return new CarrierSelection
            {
                Bindings = moving[0].Bindings,
                HorizontalProperties = horizontal,
                VerticalProperty = vertical.Property
            };
        }

        private static void VerifyDerivedClip(
            AnimationClip source,
            AnimationClip derived,
            IReadOnlyDictionary<EditorCurveBinding, AnimationCurve> sourceCurves,
            IReadOnlyCollection<EditorCurveBinding> sourceObjectBindings,
            IReadOnlyCollection<EditorCurveBinding> changedBindings,
            CarrierSelection carrier)
        {
            if (!new HashSet<EditorCurveBinding>(sourceCurves.Keys)
                    .SetEquals(AnimationUtility.GetCurveBindings(derived)) ||
                Mathf.Abs(source.length - derived.length) > CurveTolerance ||
                Mathf.Abs(source.frameRate - derived.frameRate) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Derived crouch clip structure or timing differs from source.");
            }

            foreach (KeyValuePair<EditorCurveBinding, AnimationCurve> pair in
                     sourceCurves)
            {
                AnimationCurve actual = AnimationUtility.GetEditorCurve(
                    derived,
                    pair.Key);
                if (changedBindings.Contains(pair.Key))
                {
                    AssertLockedCurve(pair.Value, actual, pair.Key);
                }
                else if (!CurvesEqual(pair.Value, actual))
                {
                    throw new InvalidOperationException(
                        "A curve outside the approved Hips horizontal scope changed: " +
                        pair.Key.path + "/" + pair.Key.propertyName + ".");
                }
            }

            if (!new HashSet<EditorCurveBinding>(sourceObjectBindings)
                    .SetEquals(
                        AnimationUtility.GetObjectReferenceCurveBindings(derived)) ||
                changedBindings.Count == 0 ||
                changedBindings.Any(binding =>
                    binding.path != carrier.Bindings[0].path ||
                    !carrier.HorizontalProperties.Contains(binding.propertyName)))
            {
                throw new InvalidOperationException(
                    "Derived crouch clip changed outside the approved carrier scope.");
            }

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(derived);
            if (!settings.loopTime || settings.loopBlend)
            {
                throw new InvalidOperationException(
                    "Derived crouch clip loop settings differ.");
            }
        }

        private static void AssertLockedCurve(
            AnimationCurve source,
            AnimationCurve actual,
            EditorCurveBinding binding)
        {
            if (source == null || actual == null ||
                source.length != actual.length ||
                CurveRange(actual) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Crouch horizontal carrier was not locked: " +
                    binding.path + "/" + binding.propertyName + ".");
            }

            float expected = source.keys[0].value;
            for (int index = 0; index < source.length; index++)
            {
                Keyframe before = source.keys[index];
                Keyframe after = actual.keys[index];
                if (Mathf.Abs(before.time - after.time) > CurveTolerance ||
                    Mathf.Abs(after.value - expected) > CurveTolerance ||
                    Mathf.Abs(after.inTangent) > CurveTolerance ||
                    Mathf.Abs(after.outTangent) > CurveTolerance ||
                    Mathf.Abs(before.inWeight - after.inWeight) > CurveTolerance ||
                    Mathf.Abs(before.outWeight - after.outWeight) > CurveTolerance ||
                    before.weightedMode != after.weightedMode)
                {
                    throw new InvalidOperationException(
                        "Locked carrier key structure differs: " +
                        binding.path + "/" + binding.propertyName + ".");
                }
            }
        }

        private static AnimationClip SaveClip(AnimationClip generated, string path)
        {
            AnimationClip existing =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static AnimatorController CreateOrUpdateController(
            string path,
            string stateName,
            AnimationClip clip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    stateName + " controller layer count differs.");
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
            state.speed = 1f;
            state.mirror = false;
            state.cycleOffset = 0f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Animator ConfigureAnimator(
            Transform target,
            RuntimeAnimatorController controller)
        {
            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                animator = target.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return animator;
        }

        private static void AssertAnimator(
            Animator animator,
            RuntimeAnimatorController controller,
            AnimationClip clip)
        {
            AnimationClip[] clips = controller.animationClips
                .Where(item => item != null)
                .Distinct()
                .ToArray();
            if (animator == null ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate ||
                clips.Length != 1 || clips[0] != clip)
            {
                throw new InvalidOperationException(
                    "Crouch Animator configuration differs for " +
                    (animator == null ? "missing target" : animator.name) + ".");
            }
        }

        private static void VerifyAllTransformBindingsExist(
            AnimationClip clip,
            Transform target)
        {
            string[] missing = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct()
                .Where(path => !string.IsNullOrEmpty(path) &&
                    target.Find(path) == null)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "Direct crouch animation paths do not match " + target.name +
                    ". Retargeting is prohibited. Missing=" +
                    string.Join(",", missing) + ".");
            }
        }

        private static TargetSpec BackwardSpec()
        {
            return new TargetSpec
            {
                TargetName = BackwardTargetName,
                StateName = BackwardStateName,
                SourcePath = BackwardSourcePath,
                ExpectedHash = ExpectedBackwardSourceHash,
                ClipPath = BackwardClipPath,
                ControllerPath = BackwardControllerPath,
                ClipName = "Player_Crouch_Backward_Mixamo_InPlace"
            };
        }

        private static TargetSpec SidestepSpec()
        {
            return new TargetSpec
            {
                TargetName = SidestepTargetName,
                StateName = SidestepStateName,
                SourcePath = SidestepSourcePath,
                ExpectedHash = ExpectedSidestepSourceHash,
                ClipPath = SidestepClipPath,
                ControllerPath = SidestepControllerPath,
                ClipName = "Player_Crouch_Sidestep_Mixamo_InPlace"
            };
        }

        private static void EnsureSourceHash(TargetSpec spec)
        {
            string actual = HashFile(spec.SourcePath);
            if (!string.Equals(
                    actual,
                    spec.ExpectedHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    spec.TargetName + " source FBX hash changed. Expected=" +
                    spec.ExpectedHash + ", Actual=" + actual + ".");
            }
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for crouch animation apply.");
            }

            return scene;
        }

        private static Dictionary<string, string> CaptureOtherAnimatorStates(
            Transform layoutRoot)
        {
            if (layoutRoot == null || layoutRoot.name != LayoutRootName)
            {
                throw new InvalidOperationException(
                    "Crouch targets do not share PlayerAnimationLayout.");
            }

            return Enumerable.Range(0, layoutRoot.childCount)
                .Select(layoutRoot.GetChild)
                .Where(child => child.name != BackwardTargetName &&
                    child.name != SidestepTargetName)
                .ToDictionary(
                    child => child.name,
                    child =>
                    {
                        Animator animator = child.GetComponent<Animator>();
                        return animator == null
                            ? "none"
                            : string.Join(
                                "|",
                                animator.enabled,
                                animator.applyRootMotion,
                                AssetDatabase.GetAssetPath(
                                    animator.runtimeAnimatorController));
                    },
                    StringComparer.Ordinal);
        }

        private static bool DictionariesEqual(
            IReadOnlyDictionary<string, string> expected,
            IReadOnlyDictionary<string, string> actual)
        {
            return expected.Count == actual.Count &&
                   expected.All(pair =>
                       actual.TryGetValue(pair.Key, out string value) &&
                       string.Equals(pair.Value, value, StringComparison.Ordinal));
        }

        private static bool RootPoseMatches(Transform target, RootPose pose)
        {
            return Vector3.Distance(target.position, pose.Position) <= CurveTolerance &&
                   Quaternion.Angle(target.rotation, pose.Rotation) <= CurveTolerance &&
                   Vector3.Distance(target.localScale, pose.Scale) <= CurveTolerance;
        }

        private static AnimationClip LoadClip(string path)
        {
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            return clip ?? throw new FileNotFoundException(
                "Required crouch animation clip is missing.",
                Path.GetFullPath(path));
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
                FindUniqueBone(target, boneName),
                target);
        }

        private static Quaternion[] SampleTargetRelativeRotations(
            AnimationClip clip,
            Transform target,
            string boneName,
            float[] times)
        {
            Transform bone = FindUniqueBone(target, boneName);
            return SampleRotations(
                clip,
                target,
                times,
                () => Quaternion.Inverse(target.rotation) * bone.rotation);
        }

        private static Vector3[] SampleTargetRelativePositions(
            AnimationClip clip,
            Transform target,
            string boneName,
            float[] times)
        {
            Transform bone = FindUniqueBone(target, boneName);
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            Vector3[] positions = new Vector3[times.Length];
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
                    positions[index] = target.InverseTransformPoint(
                        bone.position);
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            return positions;
        }

        private static Quaternion[] SampleStraightLeftForeArmLocalRotations(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            Transform upperArm = FindUniqueBone(target, "LeftArm");
            Transform foreArm = FindUniqueBone(target, "LeftForeArm");
            Transform hand = FindUniqueBone(target, "LeftHand");
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
                    Vector3 upperDirection = foreArm.position - upperArm.position;
                    Vector3 lowerDirection = hand.position - foreArm.position;
                    if (upperDirection.sqrMagnitude <= 0.0000001f ||
                        lowerDirection.sqrMagnitude <= 0.0000001f)
                    {
                        throw new InvalidOperationException(
                            target.name +
                            " left arm contains a degenerate bone segment.");
                    }

                    Quaternion straighten = Quaternion.FromToRotation(
                        lowerDirection.normalized,
                        upperDirection.normalized);
                    Quaternion desiredWorld = straighten * foreArm.rotation;
                    rotations[index] =
                        Quaternion.Inverse(foreArm.parent.rotation) * desiredWorld;
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            return rotations;
        }

        private static float LeftElbowBendMaximum(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            Transform upperArm = FindUniqueBone(target, "LeftArm");
            Transform foreArm = FindUniqueBone(target, "LeftForeArm");
            Transform hand = FindUniqueBone(target, "LeftHand");
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Another Animation Mode session is active.");
            }

            float maximum = 0f;
            AnimationMode.StartAnimationMode();
            try
            {
                int sampleCount = Mathf.Max(1, times.Length - 1);
                for (int index = 0; index < sampleCount; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        target.gameObject,
                        clip,
                        times[index]);
                    AnimationMode.EndSampling();
                    Vector3 upperDirection = foreArm.position - upperArm.position;
                    Vector3 lowerDirection = hand.position - foreArm.position;
                    if (upperDirection.sqrMagnitude <= 0.0000001f ||
                        lowerDirection.sqrMagnitude <= 0.0000001f)
                    {
                        throw new InvalidOperationException(
                            target.name +
                            " left arm contains a degenerate bone segment.");
                    }

                    maximum = Mathf.Max(
                        maximum,
                        Vector3.Angle(upperDirection, lowerDirection));
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            return maximum;
        }

        private static Quaternion FindLeftArmDownwardRotation(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            Vector3 meanDirection = LeftUpperArmMeanDirection(
                clip,
                target,
                times);
            return Quaternion.FromToRotation(meanDirection, Vector3.down);
        }

        private static float LeftArmDownwardMeanAngle(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            return Vector3.Angle(
                LeftUpperArmMeanDirection(clip, target, times),
                Vector3.down);
        }

        private static float LeftArmDownwardMaximumAngle(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            Vector3[] directions = LeftUpperArmDirections(clip, target, times);
            int sampleCount = Mathf.Max(1, times.Length - 1);
            return directions.Take(sampleCount).Max(direction =>
                Vector3.Angle(direction, Vector3.down));
        }

        private static Vector3 LeftUpperArmMeanDirection(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            Vector3[] directions = LeftUpperArmDirections(clip, target, times);
            int sampleCount = Mathf.Max(1, times.Length - 1);
            Vector3 sum = directions.Take(sampleCount)
                .Aggregate(Vector3.zero, (current, value) => current + value);
            if (sum.sqrMagnitude <= 0.0000001f)
            {
                throw new InvalidOperationException(
                    target.name +
                    " left upper-arm mean direction is degenerate.");
            }

            return sum.normalized;
        }

        private static Vector3[] LeftUpperArmDirections(
            AnimationClip clip,
            Transform target,
            float[] times)
        {
            Vector3[] upperArm = SampleTargetRelativePositions(
                clip,
                target,
                "LeftArm",
                times);
            Vector3[] foreArm = SampleTargetRelativePositions(
                clip,
                target,
                "LeftForeArm",
                times);
            Vector3[] directions = new Vector3[times.Length];
            for (int index = 0; index < times.Length; index++)
            {
                Vector3 direction = foreArm[index] - upperArm[index];
                if (direction.sqrMagnitude <= 0.0000001f)
                {
                    throw new InvalidOperationException(
                        target.name +
                        " left upper-arm segment is degenerate.");
                }

                directions[index] = direction.normalized;
            }

            return directions;
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

        private static Dictionary<string, Quaternion[]>
            ConvertTargetRelativeArmRotationsToLocal(
                AnimationClip baseClip,
                Transform target,
                float[] times,
                Dictionary<string, Quaternion[]> desiredTargetRelative)
        {
            Quaternion[] spineRotations = SampleTargetRelativeRotations(
                baseClip,
                target,
                "Spine",
                times);
            Dictionary<string, Quaternion[]> localRotations =
                new Dictionary<string, Quaternion[]>(StringComparer.Ordinal);
            foreach (string boneName in ArmBones)
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

            if (sum.magnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Quaternion mean is degenerate.");
            }

            sum /= sum.magnitude;
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
                if (index > 0 &&
                    Quaternion.Dot(continuous[index - 1], rotation) < 0f)
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
            AnimationCurve curve = new AnimationCurve(
                times.Select((time, index) =>
                    new Keyframe(time, component(rotations[index])))
                    .ToArray());
            for (int index = 0; index < curve.length; index++)
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

        private static float SwingDifference(
            Quaternion[] before,
            Quaternion[] after)
        {
            if (before.Length != after.Length || before.Length < 2)
            {
                return float.PositiveInfinity;
            }

            Quaternion beforeMean = QuaternionMean(
                before.Take(before.Length - 1));
            Quaternion afterMean = QuaternionMean(
                after.Take(after.Length - 1));
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

        private static Dictionary<EditorCurveBinding, AnimationCurve>
            CaptureCurves(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip).ToDictionary(
                binding => binding,
                binding => CloneCurve(
                    AnimationUtility.GetEditorCurve(clip, binding) ??
                    throw new InvalidOperationException(
                        "Animation curve is missing: " +
                        binding.path + "/" + binding.propertyName + ".")));
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
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
            return new HashSet<EditorCurveBinding>(beforeOutside).SetEquals(
                       afterOutside) &&
                   beforeOutside.All(binding => CurvesEqual(
                       before[binding],
                       afterCurves[binding]));
        }

        private static void SaveOverExisting(AnimationClip source, string path)
        {
            AnimationClip target = LoadClip(path);
            EditorUtility.CopySerialized(source, target);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static float CurveRange(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return 0f;
            }

            return curve.keys.Max(key => key.value) -
                   curve.keys.Min(key => key.value);
        }

        private static bool CurvesEqual(
            AnimationCurve first,
            AnimationCurve second)
        {
            if (first == null || second == null ||
                first.length != second.length ||
                first.preWrapMode != second.preWrapMode ||
                first.postWrapMode != second.postWrapMode)
            {
                return false;
            }

            for (int index = 0; index < first.length; index++)
            {
                Keyframe left = first.keys[index];
                Keyframe right = second.keys[index];
                if (Mathf.Abs(left.time - right.time) > CurveTolerance ||
                    Mathf.Abs(left.value - right.value) > CurveTolerance ||
                    Mathf.Abs(left.inTangent - right.inTangent) > CurveTolerance ||
                    Mathf.Abs(left.outTangent - right.outTangent) > CurveTolerance ||
                    Mathf.Abs(left.inWeight - right.inWeight) > CurveTolerance ||
                    Mathf.Abs(left.outWeight - right.outWeight) > CurveTolerance ||
                    left.weightedMode != right.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPositionProperty(string property)
        {
            return property == "m_LocalPosition.x" ||
                   property == "m_LocalPosition.y" ||
                   property == "m_LocalPosition.z";
        }

        private static string HashFile(string assetPath)
        {
            string absolutePath = Path.GetFullPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Required source FBX is missing.",
                    absolutePath);
            }

            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolutePath))
            {
                return BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }
        }

        private static void WriteMetrics(ApplyMetrics metrics)
        {
            string absolutePath = Path.GetFullPath(ApplyMetricsPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "Crouch apply metrics directory is unavailable."));
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(metrics, true) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static void WriteArmAlignmentMetrics(
            ArmAlignmentApplyMetrics metrics)
        {
            WriteArmMetrics(metrics, ArmAlignmentMetricsPath);
        }

        private static void WriteArmMetrics(
            ArmAlignmentApplyMetrics metrics,
            string metricsPath)
        {
            string absolutePath = Path.GetFullPath(metricsPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "Crouch arm metrics directory is unavailable."));
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(metrics, true) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static void WriteLeftArmStraightDownMetrics(
            LeftArmStraightDownApplyMetrics metrics)
        {
            string absolutePath = Path.GetFullPath(
                LeftArmStraightDownMetricsPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "Crouch left-arm metrics directory is unavailable."));
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(metrics, true) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static string StripNamespace(string value)
        {
            int separator = value.LastIndexOf(':');
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
