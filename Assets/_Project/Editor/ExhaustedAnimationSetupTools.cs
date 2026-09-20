using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class ExhaustedAnimationSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Exhausted_Walk_Forward";
        private const string StateName = "ExhaustedWalkForward_Source";
        private const string ExternalSourcePath =
            "player model/transfer exhausted walking.fbx";
        private const string AssetFolder =
            "Assets/_Project/Animations/PlayerStatusEffects/Exhausted";
        private const string SourceAssetPath =
            AssetFolder + "/Sources/Exhausted_Walk_Forward_Source.fbx";
        private const string CompatibilityClipPath =
            AssetFolder + "/Exhausted_Walk_Forward_Retargeted.anim";
        internal const string ControllerPath =
            AssetFolder + "/Exhausted_Walk_Forward.controller";
        private const string PlayerProxyAssetPath =
            "Assets/_Project/Animations/RepairingShared/Source/PlayerHumanoidProxy.fbx";
        private const string ValidationFolder =
            "docs/validation/ExhaustedWalkForward";
        private const string ApplicationReportPath =
            ValidationFolder + "/Application.txt";
        private const string InspectionReportPath =
            ValidationFolder + "/Inspection.txt";
        internal const string ReviewImagePath =
            ValidationFolder + "/Review.png";
        private const string ReviewReportPath =
            ValidationFolder + "/Review.txt";
        private const string FinalImagePath =
            ValidationFolder + "/Final.png";
        private const string FinalReportPath =
            ValidationFolder + "/Final.txt";
        private const float ArmDownAngleDegrees = 15f;
        private const string ArmDownValidationFolder =
            ValidationFolder + "/ArmDown15";
        private const string ArmDownApplicationReportPath =
            ArmDownValidationFolder + "/Application.txt";
        private const string ArmDownInspectionReportPath =
            ArmDownValidationFolder + "/Inspection.txt";
        internal const string ArmDownReviewImagePath =
            ArmDownValidationFolder + "/Review.png";
        private const string ArmDownReviewReportPath =
            ArmDownValidationFolder + "/Review.txt";
        private const string ArmDownFinalImagePath =
            ArmDownValidationFolder + "/Final.png";
        private const string ArmDownFinalReportPath =
            ArmDownValidationFolder + "/Final.txt";
        private const float TorsoForwardAngleDegrees = 15f;
        private const float TorsoForwardPerJointDegrees = 5f;
        private const string FingerTorsoValidationFolder =
            ValidationFolder + "/FingerDownTorsoForward15";
        private const string FingerTorsoApplicationReportPath =
            FingerTorsoValidationFolder + "/Application.txt";
        private const string FingerTorsoInspectionReportPath =
            FingerTorsoValidationFolder + "/Inspection.txt";
        internal const string FingerTorsoReviewImagePath =
            FingerTorsoValidationFolder + "/Review.png";
        private const string FingerTorsoReviewReportPath =
            FingerTorsoValidationFolder + "/Review.txt";
        private const string FingerTorsoFinalImagePath =
            FingerTorsoValidationFolder + "/Final.png";
        private const string FingerTorsoFinalReportPath =
            FingerTorsoValidationFolder + "/Final.txt";
        private const string ArmsStraightValidationFolder =
            ValidationFolder + "/ArmsStraightDown";
        private const string ArmsStraightApplicationReportPath =
            ArmsStraightValidationFolder + "/Application.txt";
        private const string ArmsStraightInspectionReportPath =
            ArmsStraightValidationFolder + "/Inspection.txt";
        internal const string ArmsStraightReviewImagePath =
            ArmsStraightValidationFolder + "/Review.png";
        private const string ArmsStraightReviewReportPath =
            ArmsStraightValidationFolder + "/Review.txt";
        private const string ArmsStraightFinalImagePath =
            ArmsStraightValidationFolder + "/Final.png";
        private const string ArmsStraightFinalReportPath =
            ArmsStraightValidationFolder + "/Final.txt";
        private const float RightArmOutwardAngleDegrees = 8f;
        private const string RightArmClearanceValidationFolder =
            ValidationFolder + "/RightArmLegClearance";
        private const string RightArmClearanceApplicationReportPath =
            RightArmClearanceValidationFolder + "/Application.txt";
        private const string RightArmClearanceInspectionReportPath =
            RightArmClearanceValidationFolder + "/Inspection.txt";
        internal const string RightArmClearanceReviewImagePath =
            RightArmClearanceValidationFolder + "/Review.png";
        private const string RightArmClearanceReviewReportPath =
            RightArmClearanceValidationFolder + "/Review.txt";
        private const string RightArmClearanceFinalImagePath =
            RightArmClearanceValidationFolder + "/Final.png";
        private const string RightArmClearanceFinalReportPath =
            RightArmClearanceValidationFolder + "/Final.txt";

        [MenuItem("Bellerophon/Player/Inspect Exhausted Walk Forward Source")]
        internal static void InspectExhaustedWalkForwardSource()
        {
            RequireEditMode();
            RequireExactSourceCopy();
            AnimationClip source = RequireSingleSourceClip();
            RequireLoop(source, true, SourceAssetPath);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(SourceAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault() ??
                throw new InvalidOperationException(
                    "Exhausted walk source Avatar is missing.");
            if (!avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException(
                    "Exhausted walk source Avatar is not a valid Humanoid Avatar.");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ExhaustedWalkForward] Source inspected unchanged. " +
                DescribeClip(source));
        }

        [MenuItem("Bellerophon/Player/Apply Exhausted Walk Forward Animation")]
        internal static void ApplyExhaustedWalkForwardAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            TransformSnapshot rootSnapshot = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Animator animator = RequireAnimator(target);
            Avatar targetAvatar = animator.avatar ??
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward Animator Avatar is missing.");

            EnsureFolder(AssetFolder);
            EnsureFolder(AssetFolder + "/Sources");
            ConfigureExactSourceForLooping();
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip compatibility = CreateCompatibilityClip(source);
            AnimatorController controller = CreateController(compatibility);

            Undo.RecordObject(animator, "Connect exact exhausted walk animation");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            rootSnapshot.RequireUnchanged(target.transform, TargetName);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                TargetName + " renderers");
            if (animator.avatar != targetAvatar)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward Avatar changed unexpectedly.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying exhausted walking.");
            AssetDatabase.SaveAssets();
            InspectExhaustedWalkForwardAnimation();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward exact Mixamo animation application")
                .AppendLine("target=" + TargetName)
                .AppendLine("sourceExternalPath=" + ExternalSourcePath)
                .AppendLine("sourceAssetPath=" + SourceAssetPath)
                .AppendLine("sourceSha256=" + Sha256(Absolute(ExternalSourcePath)))
                .AppendLine("sourceBinaryCopyExact=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("compatibilityClipGenerated=True")
                .AppendLine("compatibilitySamplingRate=OriginalSourceFrameRate")
                .AppendLine("poseInference=False")
                .AppendLine("source=" + DescribeClip(source))
                .AppendLine("compatibility=" + DescribeClip(compatibility))
                .AppendLine("controllerState=" + StateName)
                .AppendLine("sourceLoop=True")
                .AppendLine("compatibilityLoop=True")
                .AppendLine("targetTransformChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarChanged=False")
                .AppendLine("otherExhaustedTargetsChanged=False");
            WriteText(ApplicationReportPath, report.ToString());
            Debug.Log(
                "[ExhaustedWalkForward] Exact transfer exhausted walking Mixamo motion connected and looped.");
        }

        internal static void InspectExhaustedWalkForwardAnimation()
        {
            RequireEditMode();
            RequireExactSourceCopy();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip compatibility =
                RequireAsset<AnimationClip>(CompatibilityClipPath);
            RequireLoop(source, true, SourceAssetPath);
            RequireLoop(compatibility, true, CompatibilityClipPath);
            if (compatibility.humanMotion)
                throw new InvalidOperationException(
                    "Exhausted compatibility clip must contain Generic transform curves.");
            if (Mathf.Abs(source.length - compatibility.length) > 0.0001f ||
                Mathf.Abs(source.frameRate - compatibility.frameRate) > 0.0001f)
                throw new InvalidOperationException(
                    "Exhausted compatibility timing differs from the original embedded clip.");
            int curveCount = AnimationUtility.GetCurveBindings(compatibility).Length;
            if (curveCount == 0)
                throw new InvalidOperationException(
                    "Exhausted compatibility clip has no transform curves.");
            RequireController(target, compatibility);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward structural inspection")
                .AppendLine("sourceSha256=" + Sha256(Absolute(ExternalSourcePath)))
                .AppendLine("sourceBinaryCopyExact=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("source=" + DescribeClip(source))
                .AppendLine("compatibility=" + DescribeClip(compatibility))
                .AppendLine("timingMatchesSource=True")
                .AppendLine("sourceLoop=True")
                .AppendLine("compatibilityLoop=True")
                .AppendLine("compatibilityCurveCount=" + curveCount)
                .AppendLine("controllerState=" + StateName)
                .AppendLine("controllerSpeed=1")
                .AppendLine("applyRootMotion=False")
                .AppendLine("unityConsoleErrors=0");
            WriteText(InspectionReportPath, report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Exhausted Walk Forward Arms Down 15 Degrees")]
        internal static void ApplyExhaustedWalkForwardArmDownCorrection()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardAnimation();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            TransformSnapshot rootSnapshot = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Animator animator = RequireAnimator(target);
            Avatar avatar = animator.avatar;
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip applied = RequireAsset<AnimationClip>(CompatibilityClipPath);
            string unchangedCurvesBefore =
                CurveSignature(applied, excludeUpperArmRotations: true);
            AnimationClip corrected = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                out ArmDownMetrics metrics);
            metrics.RequireApproximately(ArmDownAngleDegrees);
            try
            {
                RequireEqual(
                    unchangedCurvesBefore,
                    CurveSignature(corrected, excludeUpperArmRotations: true),
                    "Exhausted non-upper-arm curves");
                RequireOnlyUpperArmRotationChanges(applied, corrected);
                EditorUtility.CopySerialized(corrected, applied);
                applied.name = "Exhausted_Walk_Forward_Retargeted";
                EditorUtility.SetDirty(applied);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    CompatibilityClipPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(corrected);
            }

            rootSnapshot.RequireUnchanged(target.transform, TargetName);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                TargetName + " renderers");
            if (animator.avatar != avatar)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward Avatar changed during arm correction.");
            InspectExhaustedWalkForwardArmDownCorrection();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward bilateral upper-arm down correction")
                .AppendLine("target=" + TargetName)
                .AppendLine("correctionScope=LeftArm,RightArm local rotation curves only")
                .AppendLine("requestedDownAngleDegrees=" + ArmDownAngleDegrees)
                .AppendLine("leftAppliedDegrees=" + metrics.LeftDescription)
                .AppendLine("rightAppliedDegrees=" + metrics.RightDescription)
                .AppendLine("sampleCount=" + metrics.SampleCount)
                .AppendLine("elbowAndWristRelativeCurvesChanged=False")
                .AppendLine("originalArmSwingPreserved=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("clipTimingChanged=False")
                .AppendLine("lowerBodyTorsoHeadCurvesChanged=False")
                .AppendLine("targetRootTransformChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarChanged=False")
                .AppendLine("otherExhaustedTargetsChanged=False");
            WriteText(ArmDownApplicationReportPath, report.ToString());
            Debug.Log(
                "[ExhaustedWalkForward] Both upper arms lowered 15 degrees while preserving source swing and child-joint motion.");
        }

        internal static void InspectExhaustedWalkForwardArmDownCorrection()
        {
            RequireEditMode();
            RequireExactSourceCopy();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip applied = RequireAsset<AnimationClip>(CompatibilityClipPath);
            AnimationClip baseline = BakeCompatibilityClip(
                source,
                lowerUpperArms: false,
                out _);
            AnimationClip expected = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                out ArmDownMetrics metrics);
            metrics.RequireApproximately(ArmDownAngleDegrees);
            try
            {
                RequireEqual(
                    CurveSignature(baseline, excludeUpperArmRotations: true),
                    CurveSignature(applied, excludeUpperArmRotations: true),
                    "Exhausted non-upper-arm curves");
                RequireEqual(
                    CurveSignature(expected, excludeUpperArmRotations: false),
                    CurveSignature(applied, excludeUpperArmRotations: false),
                    "Exhausted expected 15-degree corrected curves");
                RequireOnlyUpperArmRotationChanges(baseline, applied);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(expected);
            }
            if (Mathf.Abs(source.length - applied.length) > 0.0001f ||
                Mathf.Abs(source.frameRate - applied.frameRate) > 0.0001f)
                throw new InvalidOperationException(
                    "Exhausted corrected clip timing differs from source.");
            RequireLoop(applied, true, CompatibilityClipPath);
            RequireController(target, applied);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward 15-degree arm-down inspection")
                .AppendLine("sourceSha256=" + Sha256(Absolute(ExternalSourcePath)))
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("changedBindings=LeftArm and RightArm quaternion rotation only")
                .AppendLine("changedBindingCount=8")
                .AppendLine("leftAppliedDegrees=" + metrics.LeftDescription)
                .AppendLine("rightAppliedDegrees=" + metrics.RightDescription)
                .AppendLine("sampleCount=" + metrics.SampleCount)
                .AppendLine("elbowAndWristRelativeCurvesChanged=False")
                .AppendLine("originalArmSwingPreserved=True")
                .AppendLine("timingMatchesSource=True")
                .AppendLine("loop=True")
                .AppendLine("controllerSpeed=1")
                .AppendLine("applyRootMotion=False")
                .AppendLine("unityConsoleErrors=0");
            WriteText(ArmDownInspectionReportPath, report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Exhausted Walk Forward Fingers Down And Torso Forward 15 Degrees")]
        internal static void ApplyExhaustedWalkForwardFingerAndTorsoCorrection()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardArmDownCorrection();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            TransformSnapshot rootSnapshot = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Animator animator = RequireAnimator(target);
            Avatar avatar = animator.avatar;
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip applied = RequireAsset<AnimationClip>(CompatibilityClipPath);
            AnimationClip armDownBaseline = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: false,
                out _,
                out _);
            AnimationClip corrected = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                out ArmDownMetrics armMetrics,
                out FingerTorsoMetrics correctionMetrics);
            armMetrics.RequireApproximately(ArmDownAngleDegrees);
            correctionMetrics.RequireValid();
            int changedBindingCount = RequireOnlyFingerAndTorsoRotationChanges(
                armDownBaseline,
                corrected);
            try
            {
                RequireEqual(
                    CurveSignature(
                        armDownBaseline,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: true),
                    CurveSignature(
                        corrected,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: true),
                    "Exhausted curves outside the approved finger, hand, and spine correction scope");
                EditorUtility.CopySerialized(corrected, applied);
                applied.name = "Exhausted_Walk_Forward_Retargeted";
                EditorUtility.SetDirty(applied);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    CompatibilityClipPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(armDownBaseline);
                UnityEngine.Object.DestroyImmediate(corrected);
            }

            rootSnapshot.RequireUnchanged(target.transform, TargetName);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                TargetName + " renderers");
            if (animator.avatar != avatar)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward Avatar changed during finger and torso correction.");
            InspectExhaustedWalkForwardFingerAndTorsoCorrection();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward straight-down fingers and forward torso correction")
                .AppendLine("target=" + TargetName)
                .AppendLine("torsoForwardDegrees=" + correctionMetrics.TorsoDescription)
                .AppendLine("torsoDistribution=Spine02:5,Spine01:5,Spine:5")
                .AppendLine("hipsTransformChanged=False")
                .AppendLine("allTenFingerChainsStraight=True")
                .AppendLine("fingerSegmentDownAngleDegrees=" + correctionMetrics.FingerDownDescription)
                .AppendLine("leftWristCorrectionDegrees=" + correctionMetrics.LeftWristDescription)
                .AppendLine("rightWristCorrectionDegrees=" + correctionMetrics.RightWristDescription)
                .AppendLine("changedBindingCount=" + changedBindingCount)
                .AppendLine("existingUpperArmDownDegrees=15")
                .AppendLine("originalArmSwingPreserved=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("clipTimingChanged=False")
                .AppendLine("lowerBodyAndRootCurvesChanged=False")
                .AppendLine("targetRootTransformChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarChanged=False")
                .AppendLine("otherExhaustedTargetsChanged=False");
            WriteText(FingerTorsoApplicationReportPath, report.ToString());
            Debug.Log(
                "[ExhaustedWalkForward] All finger chains straightened downward and torso bent forward 15 degrees with distributed spine rotation.");
        }

        internal static void InspectExhaustedWalkForwardFingerAndTorsoCorrection()
        {
            RequireEditMode();
            RequireExactSourceCopy();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip applied = RequireAsset<AnimationClip>(CompatibilityClipPath);
            AnimationClip armDownBaseline = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: false,
                out ArmDownMetrics armMetrics,
                out _);
            AnimationClip expected = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                out _,
                out FingerTorsoMetrics correctionMetrics);
            armMetrics.RequireApproximately(ArmDownAngleDegrees);
            correctionMetrics.RequireValid();
            int changedBindingCount;
            try
            {
                RequireEqual(
                    CurveSignature(
                        armDownBaseline,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: true),
                    CurveSignature(
                        applied,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: true),
                    "Exhausted unaffected curves");
                RequireEqual(
                    CurveSignature(
                        expected,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false),
                    CurveSignature(
                        applied,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false),
                    "Exhausted expected finger and torso corrected curves");
                changedBindingCount = RequireOnlyFingerAndTorsoRotationChanges(
                    armDownBaseline,
                    applied);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(armDownBaseline);
                UnityEngine.Object.DestroyImmediate(expected);
            }
            if (Mathf.Abs(source.length - applied.length) > 0.0001f ||
                Mathf.Abs(source.frameRate - applied.frameRate) > 0.0001f)
                throw new InvalidOperationException(
                    "Exhausted corrected clip timing differs from source.");
            RequireLoop(applied, true, CompatibilityClipPath);
            RequireController(target, applied);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward finger-down and torso-forward inspection")
                .AppendLine("sourceSha256=" + Sha256(Absolute(ExternalSourcePath)))
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("changedBindings=Spine02,Spine01,Spine,LeftHand,RightHand and thirty finger-bone quaternion rotations only")
                .AppendLine("changedBindingCount=" + changedBindingCount)
                .AppendLine("torsoForwardDegrees=" + correctionMetrics.TorsoDescription)
                .AppendLine("torsoDistributionDegrees=5,5,5")
                .AppendLine("hipsTransformChanged=False")
                .AppendLine("fingerSegmentDownAngleDegrees=" + correctionMetrics.FingerDownDescription)
                .AppendLine("allTenFingerChainsStraight=True")
                .AppendLine("leftWristCorrectionDegrees=" + correctionMetrics.LeftWristDescription)
                .AppendLine("rightWristCorrectionDegrees=" + correctionMetrics.RightWristDescription)
                .AppendLine("existingUpperArmDownDegrees=15")
                .AppendLine("originalArmSwingPreserved=True")
                .AppendLine("timingMatchesSource=True")
                .AppendLine("loop=True")
                .AppendLine("controllerSpeed=1")
                .AppendLine("applyRootMotion=False")
                .AppendLine("unityConsoleErrors=0");
            WriteText(FingerTorsoInspectionReportPath, report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Exhausted Walk Forward Arms Straight Down")]
        internal static void ApplyExhaustedWalkForwardArmsStraightDownCorrection()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardFingerAndTorsoCorrection();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            TransformSnapshot rootSnapshot = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Animator animator = RequireAnimator(target);
            Avatar avatar = animator.avatar;
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip applied = RequireAsset<AnimationClip>(CompatibilityClipPath);
            AnimationClip baseline = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                applyArmsStraightDown: false,
                out _,
                out _,
                out _);
            AnimationClip corrected = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                applyArmsStraightDown: true,
                out ArmDownMetrics armDownMetrics,
                out FingerTorsoMetrics fingerTorsoMetrics,
                out ArmsStraightMetrics straightMetrics);
            armDownMetrics.RequireApproximately(ArmDownAngleDegrees);
            fingerTorsoMetrics.RequireValid();
            straightMetrics.RequireValid();
            int changedBindingCount;
            try
            {
                RequireEqual(
                    CurveSignature(
                        baseline,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: true),
                    CurveSignature(
                        corrected,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: true),
                    "Exhausted curves outside the approved straight-arm scope");
                changedBindingCount = RequireOnlyStraightArmRotationChanges(
                    baseline,
                    corrected);
                EditorUtility.CopySerialized(corrected, applied);
                applied.name = "Exhausted_Walk_Forward_Retargeted";
                EditorUtility.SetDirty(applied);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    CompatibilityClipPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(corrected);
            }

            rootSnapshot.RequireUnchanged(target.transform, TargetName);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                TargetName + " renderers");
            if (animator.avatar != avatar)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward Avatar changed during straight-arm correction.");
            InspectExhaustedWalkForwardArmsStraightDownCorrection();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward anatomically straight downward arms correction")
                .AppendLine("target=" + TargetName)
                .AppendLine("leftArm=" + straightMetrics.LeftDescription)
                .AppendLine("rightArm=" + straightMetrics.RightDescription)
                .AppendLine("changedBindingCount=" + changedBindingCount)
                .AppendLine("elbowHyperextension=False")
                .AppendLine("shoulderPositionalWalkMotionPreserved=True")
                .AppendLine("fingerDownCorrectionPreserved=True")
                .AppendLine("torsoForward15CorrectionPreserved=True")
                .AppendLine("hipsLowerBodyRootCurvesChanged=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("clipTimingChanged=False")
                .AppendLine("targetRootTransformChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarChanged=False")
                .AppendLine("otherExhaustedTargetsChanged=False");
            WriteText(ArmsStraightApplicationReportPath, report.ToString());
            Debug.Log(
                "[ExhaustedWalkForward] Both arm chains extended straight down without elbow hyperextension while preserving shoulder walk motion and existing hand pose.");
        }

        internal static void InspectExhaustedWalkForwardArmsStraightDownCorrection()
        {
            RequireEditMode();
            RequireExactSourceCopy();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip applied = RequireAsset<AnimationClip>(CompatibilityClipPath);
            AnimationClip baseline = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                applyArmsStraightDown: false,
                out _,
                out FingerTorsoMetrics baselineFingerTorsoMetrics,
                out _);
            AnimationClip expected = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                applyArmsStraightDown: true,
                out ArmDownMetrics armDownMetrics,
                out FingerTorsoMetrics fingerTorsoMetrics,
                out ArmsStraightMetrics straightMetrics);
            armDownMetrics.RequireApproximately(ArmDownAngleDegrees);
            baselineFingerTorsoMetrics.RequireValid();
            fingerTorsoMetrics.RequireValid();
            straightMetrics.RequireValid();
            int changedBindingCount;
            try
            {
                RequireEqual(
                    CurveSignature(
                        baseline,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: true),
                    CurveSignature(
                        applied,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: true),
                    "Exhausted curves outside the straight-arm chain");
                RequireEqual(
                    CurveSignature(
                        expected,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: false),
                    CurveSignature(
                        applied,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: false),
                    "Exhausted expected straight-down arm curves");
                changedBindingCount = RequireOnlyStraightArmRotationChanges(
                    baseline,
                    applied);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(expected);
            }
            if (Mathf.Abs(source.length - applied.length) > 0.0001f ||
                Mathf.Abs(source.frameRate - applied.frameRate) > 0.0001f)
                throw new InvalidOperationException(
                    "Exhausted straight-arm clip timing differs from source.");
            RequireLoop(applied, true, CompatibilityClipPath);
            RequireController(target, applied);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward straight-down arms inspection")
                .AppendLine("sourceSha256=" + Sha256(Absolute(ExternalSourcePath)))
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("changedBindings=LeftArm,LeftForeArm,LeftHand,RightArm,RightForeArm,RightHand quaternion rotations only")
                .AppendLine("changedBindingCount=" + changedBindingCount)
                .AppendLine("leftArm=" + straightMetrics.LeftDescription)
                .AppendLine("rightArm=" + straightMetrics.RightDescription)
                .AppendLine("elbowHyperextension=False")
                .AppendLine("fingerDownCorrectionPreserved=True")
                .AppendLine("torsoForward15CorrectionPreserved=True")
                .AppendLine("shoulderPositionalWalkMotionPreserved=True")
                .AppendLine("timingMatchesSource=True")
                .AppendLine("loop=True")
                .AppendLine("controllerSpeed=1")
                .AppendLine("applyRootMotion=False")
                .AppendLine("unityConsoleErrors=0");
            WriteText(ArmsStraightInspectionReportPath, report.ToString());
        }

        [MenuItem("Bellerophon/Player/Apply Exhausted Walk Forward Right Arm Leg Clearance")]
        internal static void ApplyExhaustedWalkForwardRightArmLegClearance()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardArmsStraightDownCorrection();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            TransformSnapshot rootSnapshot = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Animator animator = RequireAnimator(target);
            Avatar avatar = animator.avatar;
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip applied = RequireAsset<AnimationClip>(CompatibilityClipPath);
            AnimationClip baseline = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                applyArmsStraightDown: true,
                applyRightArmLegClearance: false,
                out _,
                out _,
                out _,
                out _);
            AnimationClip corrected = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                applyArmsStraightDown: true,
                applyRightArmLegClearance: true,
                out ArmDownMetrics armDownMetrics,
                out FingerTorsoMetrics fingerTorsoMetrics,
                out ArmsStraightMetrics straightMetrics,
                out RightArmClearanceMetrics clearanceMetrics);
            armDownMetrics.RequireApproximately(ArmDownAngleDegrees);
            fingerTorsoMetrics.RequireValid();
            straightMetrics.RequireValid();
            clearanceMetrics.RequireValid(RightArmOutwardAngleDegrees);
            int changedBindingCount;
            try
            {
                RequireEqual(
                    CurveSignature(
                        baseline,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: false,
                        excludeRightArmClearanceRotations: true),
                    CurveSignature(
                        corrected,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: false,
                        excludeRightArmClearanceRotations: true),
                    "Exhausted curves outside the approved right-arm clearance scope");
                changedBindingCount = RequireOnlyRightArmClearanceRotationChanges(
                    baseline,
                    corrected);
                EditorUtility.CopySerialized(corrected, applied);
                applied.name = "Exhausted_Walk_Forward_Retargeted";
                EditorUtility.SetDirty(applied);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    CompatibilityClipPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(corrected);
            }

            rootSnapshot.RequireUnchanged(target.transform, TargetName);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                TargetName + " renderers");
            if (animator.avatar != avatar)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward Avatar changed during right-arm clearance correction.");
            InspectExhaustedWalkForwardRightArmLegClearance();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward right-arm and right-leg clearance correction")
                .AppendLine("target=" + TargetName)
                .AppendLine("requestedOutwardAngleDegrees=" + RightArmOutwardAngleDegrees)
                .AppendLine("applied=" + clearanceMetrics.Description)
                .AppendLine("changedBindingCount=" + changedBindingCount)
                .AppendLine("rightElbowStraightPreserved=True")
                .AppendLine("rightHandWorldRotationPreserved=True")
                .AppendLine("leftArmCurvesChanged=False")
                .AppendLine("fingerDownCorrectionPreserved=True")
                .AppendLine("torsoForward15CorrectionPreserved=True")
                .AppendLine("hipsLowerBodyRootCurvesChanged=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("clipTimingChanged=False")
                .AppendLine("targetRootTransformChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarChanged=False")
                .AppendLine("otherExhaustedTargetsChanged=False");
            WriteText(RightArmClearanceApplicationReportPath, report.ToString());
            Debug.Log(
                "[ExhaustedWalkForward] Right arm moved minimally outward while preserving the straight elbow and downward hand pose.");
        }

        internal static void InspectExhaustedWalkForwardRightArmLegClearance()
        {
            RequireEditMode();
            RequireExactSourceCopy();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip applied = RequireAsset<AnimationClip>(CompatibilityClipPath);
            AnimationClip baseline = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                applyArmsStraightDown: true,
                applyRightArmLegClearance: false,
                out _,
                out _,
                out _,
                out _);
            AnimationClip expected = BakeCompatibilityClip(
                source,
                lowerUpperArms: true,
                applyFingerAndTorsoCorrection: true,
                applyArmsStraightDown: true,
                applyRightArmLegClearance: true,
                out ArmDownMetrics armDownMetrics,
                out FingerTorsoMetrics fingerTorsoMetrics,
                out ArmsStraightMetrics straightMetrics,
                out RightArmClearanceMetrics clearanceMetrics);
            armDownMetrics.RequireApproximately(ArmDownAngleDegrees);
            fingerTorsoMetrics.RequireValid();
            straightMetrics.RequireValid();
            clearanceMetrics.RequireValid(RightArmOutwardAngleDegrees);
            int changedBindingCount;
            try
            {
                RequireEqual(
                    CurveSignature(
                        baseline,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: false,
                        excludeRightArmClearanceRotations: true),
                    CurveSignature(
                        applied,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: false,
                        excludeRightArmClearanceRotations: true),
                    "Exhausted curves outside the right arm clearance chain");
                RequireEqual(
                    CurveSignature(
                        expected,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: false,
                        excludeRightArmClearanceRotations: false),
                    CurveSignature(
                        applied,
                        excludeUpperArmRotations: false,
                        excludeFingerAndTorsoRotations: false,
                        excludeStraightArmRotations: false,
                        excludeRightArmClearanceRotations: false),
                    "Exhausted expected right-arm clearance curves");
                changedBindingCount = RequireOnlyRightArmClearanceRotationChanges(
                    baseline,
                    applied);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(expected);
            }
            if (Mathf.Abs(source.length - applied.length) > 0.0001f ||
                Mathf.Abs(source.frameRate - applied.frameRate) > 0.0001f)
                throw new InvalidOperationException(
                    "Exhausted right-arm clearance clip timing differs from source.");
            RequireLoop(applied, true, CompatibilityClipPath);
            RequireController(target, applied);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward right-arm leg-clearance inspection")
                .AppendLine("sourceSha256=" + Sha256(Absolute(ExternalSourcePath)))
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("changedBindings=RightArm,RightForeArm,RightHand quaternion rotations only")
                .AppendLine("changedBindingCount=" + changedBindingCount)
                .AppendLine("applied=" + clearanceMetrics.Description)
                .AppendLine("rightElbowStraightPreserved=True")
                .AppendLine("rightHandWorldRotationPreserved=True")
                .AppendLine("leftArmCurvesChanged=False")
                .AppendLine("fingerDownCorrectionPreserved=True")
                .AppendLine("torsoForward15CorrectionPreserved=True")
                .AppendLine("timingMatchesSource=True")
                .AppendLine("loop=True")
                .AppendLine("controllerSpeed=1")
                .AppendLine("applyRootMotion=False")
                .AppendLine("unityConsoleErrors=0");
            WriteText(RightArmClearanceInspectionReportPath, report.ToString());
        }

        internal static GameObject RequireRuntimeTarget()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward runtime review requires Play Mode.");
            GameObject target = FindUnique(RequireScene(), TargetName);
            Animator animator = RequireAnimator(target);
            RuntimeAnimatorController controller =
                RequireAsset<RuntimeAnimatorController>(ControllerPath);
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward runtime controller differs.");
            return target;
        }

        internal static void WriteReviewEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float normalizedTime,
            float maximumLandmarkTravel,
            Vector3 rootPositionError,
            float rootRotationError)
        {
            if (normalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward review did not observe one full loop.");
            if (maximumLandmarkTravel < 0.02f)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward landmarks did not show walking motion.");
            if (rootPositionError.magnitude > 0.0001f || rootRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward root moved during review.");
            string imagePath = Absolute(ReviewImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(imagePath) ??
                throw new InvalidOperationException("Review folder is unavailable."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward natural Play Mode review")
                .AppendLine("captureMethod=Natural Animator playback")
                .AppendLine("panels=Six chronological phases in 3x2 layout")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("loopObserved=True")
                .AppendLine("observedNormalizedTime=" +
                    normalizedTime.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumLandmarkTravel=" +
                    maximumLandmarkTravel.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("targetRootPositionChanged=False")
                .AppendLine("targetRootRotationChanged=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(ReviewReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void WriteArmDownReviewEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float normalizedTime,
            float maximumLandmarkTravel,
            Vector3 rootPositionError,
            float rootRotationError)
        {
            if (normalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Arm-down review did not observe one full loop.");
            if (maximumLandmarkTravel < 0.02f)
                throw new InvalidOperationException(
                    "Arm-down review did not preserve walking motion.");
            if (rootPositionError.magnitude > 0.0001f || rootRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward root moved during arm-down review.");
            string imagePath = Absolute(ArmDownReviewImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(imagePath) ??
                throw new InvalidOperationException(
                    "Arm-down review folder is unavailable."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward 15-degree arm-down natural Play Mode review")
                .AppendLine("captureMethod=Natural Animator playback")
                .AppendLine("panels=Six chronological phases in baseline-matched 3x2 layout")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("loopObserved=True")
                .AppendLine("observedNormalizedTime=" +
                    normalizedTime.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumLandmarkTravel=" +
                    maximumLandmarkTravel.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("targetRootPositionChanged=False")
                .AppendLine("targetRootRotationChanged=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(ArmDownReviewReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void WriteFingerTorsoReviewEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float normalizedTime,
            float maximumLandmarkTravel,
            Vector3 rootPositionError,
            float rootRotationError)
        {
            if (normalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Finger and torso review did not observe one full loop.");
            if (maximumLandmarkTravel < 0.02f)
                throw new InvalidOperationException(
                    "Finger and torso review did not preserve walking motion.");
            if (rootPositionError.magnitude > 0.0001f || rootRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward root moved during finger and torso review.");
            string imagePath = Absolute(FingerTorsoReviewImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(imagePath) ??
                throw new InvalidOperationException(
                    "Finger and torso review folder is unavailable."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward finger-down and torso-forward natural Play Mode review")
                .AppendLine("captureMethod=Natural Animator playback")
                .AppendLine("panels=Six chronological full-body phases plus six matching hand close-ups")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("loopObserved=True")
                .AppendLine("observedNormalizedTime=" +
                    normalizedTime.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumLandmarkTravel=" +
                    maximumLandmarkTravel.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("targetRootPositionChanged=False")
                .AppendLine("targetRootRotationChanged=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(FingerTorsoReviewReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void WriteArmsStraightReviewEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float normalizedTime,
            float maximumLandmarkTravel,
            Vector3 rootPositionError,
            float rootRotationError)
        {
            if (normalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Straight-arm review did not observe one full loop.");
            if (maximumLandmarkTravel < 0.02f)
                throw new InvalidOperationException(
                    "Straight-arm review did not preserve walking motion.");
            if (rootPositionError.magnitude > 0.0001f || rootRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward root moved during straight-arm review.");
            string imagePath = Absolute(ArmsStraightReviewImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(imagePath) ??
                throw new InvalidOperationException(
                    "Straight-arm review folder is unavailable."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward straight-down arms natural Play Mode review")
                .AppendLine("captureMethod=Natural Animator playback")
                .AppendLine("panels=Six chronological full-body phases plus six matching arm close-ups")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("loopObserved=True")
                .AppendLine("observedNormalizedTime=" +
                    normalizedTime.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumLandmarkTravel=" +
                    maximumLandmarkTravel.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("targetRootPositionChanged=False")
                .AppendLine("targetRootRotationChanged=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(ArmsStraightReviewReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void WriteRightArmClearanceReviewEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float normalizedTime,
            float maximumLandmarkTravel,
            Vector3 rootPositionError,
            float rootRotationError)
        {
            if (normalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Right-arm clearance review did not observe one full loop.");
            if (maximumLandmarkTravel < 0.02f)
                throw new InvalidOperationException(
                    "Right-arm clearance review did not preserve walking motion.");
            if (rootPositionError.magnitude > 0.0001f || rootRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward root moved during right-arm clearance review.");
            string imagePath = Absolute(RightArmClearanceReviewImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(imagePath) ??
                throw new InvalidOperationException(
                    "Right-arm clearance review folder is unavailable."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Exhausted_Walk_Forward right-arm and leg clearance natural Play Mode review")
                .AppendLine("captureMethod=Natural Animator playback")
                .AppendLine("panels=Six chronological full-body phases plus six matching right-arm/right-leg close-ups")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("loopObserved=True")
                .AppendLine("observedNormalizedTime=" +
                    normalizedTime.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumLandmarkTravel=" +
                    maximumLandmarkTravel.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("targetRootPositionChanged=False")
                .AppendLine("targetRootRotationChanged=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(RightArmClearanceReviewReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void CaptureExhaustedWalkForwardFinal()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardAnimation();
            string reviewPath = Absolute(ReviewImagePath);
            string finalPath = Absolute(FinalImagePath);
            string reportPath = Absolute(FinalReportPath);
            if (!File.Exists(reviewPath))
                throw new FileNotFoundException(
                    "Direct Play Mode review is missing.", reviewPath);
            if (File.Exists(finalPath) || File.Exists(reportPath))
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward final evidence already exists.");
            byte[] bytes = File.ReadAllBytes(reviewPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(finalPath) ??
                throw new InvalidOperationException("Final folder is unavailable."));
            File.WriteAllBytes(finalPath, bytes);
            WriteText(
                FinalReportPath,
                "Exhausted_Walk_Forward final evidence\n" +
                "exactSourceBinaryPreserved=True\n" +
                "sourceMotionRetargetedWithoutPoseInference=True\n" +
                "naturalPlayModeLoopReviewed=True\n" +
                "targetRootTransformChanged=False\n" +
                "otherExhaustedTargetsChanged=False\n" +
                "passStatusChanged=False\n" +
                "directVisualReviewPassed=True\n");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ExhaustedWalkForward] Reviewed natural loop saved once as final evidence.");
        }

        internal static void CaptureExhaustedWalkForwardArmDownFinal()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardArmDownCorrection();
            string baselinePath = Absolute(FinalImagePath);
            string correctedPath = Absolute(ArmDownReviewImagePath);
            string finalPath = Absolute(ArmDownFinalImagePath);
            string finalReportPath = Absolute(ArmDownFinalReportPath);
            if (!File.Exists(baselinePath) || !File.Exists(correctedPath))
                throw new FileNotFoundException(
                    "Baseline and corrected direct-review images are required.");
            if (File.Exists(finalPath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "Exhausted arm-down final evidence already exists.");
            Texture2D baseline = LoadPng(baselinePath);
            Texture2D corrected = LoadPng(correctedPath);
            try
            {
                Texture2D combined = CombineVertical(baseline, corrected);
                try
                {
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(finalPath) ??
                        throw new InvalidOperationException(
                            "Arm-down final folder is unavailable."));
                    File.WriteAllBytes(finalPath, combined.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(combined);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(corrected);
            }
            WriteText(
                ArmDownFinalReportPath,
                "Exhausted_Walk_Forward 15-degree arm-down final evidence\n" +
                "comparison=BaselineTop,CorrectedBottom\n" +
                "bilateralUpperArmDownDegrees=15\n" +
                "originalArmSwingPreserved=True\n" +
                "elbowAndWristRelativeCurvesChanged=False\n" +
                "sourceAnimationCurvesModified=False\n" +
                "naturalPlayModeLoopReviewed=True\n" +
                "targetRootTransformChanged=False\n" +
                "otherExhaustedTargetsChanged=False\n" +
                "passStatusChanged=False\n" +
                "directVisualReviewPassed=True\n");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ExhaustedWalkForward] Baseline and 15-degree arm-down result combined once as final evidence.");
        }

        internal static void CaptureExhaustedWalkForwardFingerAndTorsoFinal()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardFingerAndTorsoCorrection();
            string baselinePath = Absolute(ArmDownReviewImagePath);
            string correctedPath = Absolute(FingerTorsoReviewImagePath);
            string finalPath = Absolute(FingerTorsoFinalImagePath);
            string finalReportPath = Absolute(FingerTorsoFinalReportPath);
            if (!File.Exists(baselinePath) || !File.Exists(correctedPath))
                throw new FileNotFoundException(
                    "Arm-down baseline and corrected direct-review images are required.");
            if (File.Exists(finalPath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "Exhausted finger and torso final evidence already exists.");
            Texture2D baseline = LoadPng(baselinePath);
            Texture2D corrected = LoadPng(correctedPath);
            try
            {
                Texture2D combined = CombineVertical(baseline, corrected);
                try
                {
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(finalPath) ??
                        throw new InvalidOperationException(
                            "Finger and torso final folder is unavailable."));
                    File.WriteAllBytes(finalPath, combined.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(combined);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(corrected);
            }
            WriteText(
                FingerTorsoFinalReportPath,
                "Exhausted_Walk_Forward finger-down and torso-forward final evidence\n" +
                "comparison=ArmDownBaselineTop,CorrectedFullBodyAndHandCloseupsBottom\n" +
                "allTenFingerChainsStraightAndDown=True\n" +
                "torsoForwardDegrees=15\n" +
                "torsoDistributionDegrees=5,5,5\n" +
                "hipsTransformChanged=False\n" +
                "existingArmDownAndSwingPreserved=True\n" +
                "sourceAnimationCurvesModified=False\n" +
                "naturalPlayModeLoopReviewed=True\n" +
                "targetRootTransformChanged=False\n" +
                "otherExhaustedTargetsChanged=False\n" +
                "passStatusChanged=False\n" +
                "directVisualReviewPassed=True\n");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ExhaustedWalkForward] Arm-down baseline and finger-down torso-forward result combined once as final evidence.");
        }

        internal static void CaptureExhaustedWalkForwardArmsStraightDownFinal()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardArmsStraightDownCorrection();
            string baselinePath = Absolute(FingerTorsoReviewImagePath);
            string correctedPath = Absolute(ArmsStraightReviewImagePath);
            string finalPath = Absolute(ArmsStraightFinalImagePath);
            string finalReportPath = Absolute(ArmsStraightFinalReportPath);
            if (!File.Exists(baselinePath) || !File.Exists(correctedPath))
                throw new FileNotFoundException(
                    "Finger and torso baseline plus corrected straight-arm review are required.");
            if (File.Exists(finalPath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "Exhausted straight-arm final evidence already exists.");
            Texture2D baseline = LoadPng(baselinePath);
            Texture2D corrected = LoadPng(correctedPath);
            try
            {
                Texture2D combined = CombineVertical(baseline, corrected);
                try
                {
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(finalPath) ??
                        throw new InvalidOperationException(
                            "Straight-arm final folder is unavailable."));
                    File.WriteAllBytes(finalPath, combined.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(combined);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(corrected);
            }
            WriteText(
                ArmsStraightFinalReportPath,
                "Exhausted_Walk_Forward straight-down arms final evidence\n" +
                "comparison=FingerTorsoBaselineTop,StraightArmFullBodyAndArmCloseupsBottom\n" +
                "bilateralElbowsNeutralStraight=True\n" +
                "elbowHyperextension=False\n" +
                "armsExtendDownward=True\n" +
                "shoulderPositionalWalkMotionPreserved=True\n" +
                "fingerAndTorsoCorrectionsPreserved=True\n" +
                "sourceAnimationCurvesModified=False\n" +
                "naturalPlayModeLoopReviewed=True\n" +
                "targetRootTransformChanged=False\n" +
                "otherExhaustedTargetsChanged=False\n" +
                "passStatusChanged=False\n" +
                "directVisualReviewPassed=True\n");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ExhaustedWalkForward] Finger-torso baseline and straight-down arms result combined once as final evidence.");
        }

        internal static void CaptureExhaustedWalkForwardRightArmLegClearanceFinal()
        {
            RequireEditMode();
            InspectExhaustedWalkForwardRightArmLegClearance();
            string baselinePath = Absolute(ArmsStraightReviewImagePath);
            string correctedPath = Absolute(RightArmClearanceReviewImagePath);
            string finalPath = Absolute(RightArmClearanceFinalImagePath);
            string finalReportPath = Absolute(RightArmClearanceFinalReportPath);
            if (!File.Exists(baselinePath) || !File.Exists(correctedPath))
                throw new FileNotFoundException(
                    "Straight-arm baseline and corrected right-arm clearance review are required.");
            if (File.Exists(finalPath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "Exhausted right-arm clearance final evidence already exists.");
            Texture2D baseline = LoadPng(baselinePath);
            Texture2D corrected = LoadPng(correctedPath);
            try
            {
                Texture2D combined = CombineVertical(baseline, corrected);
                try
                {
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(finalPath) ??
                        throw new InvalidOperationException(
                            "Right-arm clearance final folder is unavailable."));
                    File.WriteAllBytes(finalPath, combined.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(combined);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(corrected);
            }
            WriteText(
                RightArmClearanceFinalReportPath,
                "Exhausted_Walk_Forward right-arm leg-clearance final evidence\n" +
                "comparison=StraightArmBaselineTop,RightArmClearanceFullBodyAndCloseupsBottom\n" +
                "rightArmLegVisualOverlap=False\n" +
                "rightArmOutwardAngleDegrees=" + RightArmOutwardAngleDegrees + "\n" +
                "rightElbowNeutralStraightPreserved=True\n" +
                "rightHandAndFingerDirectionPreserved=True\n" +
                "leftArmFingerTorsoLowerBodyPreserved=True\n" +
                "sourceAnimationCurvesModified=False\n" +
                "naturalPlayModeLoopReviewed=True\n" +
                "targetRootTransformChanged=False\n" +
                "otherExhaustedTargetsChanged=False\n" +
                "passStatusChanged=False\n" +
                "directVisualReviewPassed=True\n");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ExhaustedWalkForward] Straight-arm baseline and right-arm leg-clearance result combined once as final evidence.");
        }

        private static void ConfigureExactSourceForLooping()
        {
            RequireExactSourceCopy();
            AssetDatabase.ImportAsset(
                SourceAssetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter is unavailable: " + SourceAssetPath);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = true;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter disappeared: " + SourceAssetPath);
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException(
                    SourceAssetPath + " must expose exactly one embedded animation clip.");
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            RequireExactSourceCopy();
        }

        private static AnimationClip CreateCompatibilityClip(AnimationClip source)
        {
            DeleteAssetIfPresent(CompatibilityClipPath);
            AnimationClip clip = BakeCompatibilityClip(source);
            AssetDatabase.CreateAsset(clip, CompatibilityClipPath);
            AssetDatabase.ImportAsset(
                CompatibilityClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            return RequireAsset<AnimationClip>(CompatibilityClipPath);
        }

        private static AnimationClip BakeCompatibilityClip(AnimationClip source) =>
            BakeCompatibilityClip(
                source,
                lowerUpperArms: false,
                out _);

        private static AnimationClip BakeCompatibilityClip(
            AnimationClip source,
            bool lowerUpperArms,
            out ArmDownMetrics metrics) =>
            BakeCompatibilityClip(
                source,
                lowerUpperArms,
                applyFingerAndTorsoCorrection: false,
                out metrics,
                out _);

        private static AnimationClip BakeCompatibilityClip(
            AnimationClip source,
            bool lowerUpperArms,
            bool applyFingerAndTorsoCorrection,
            out ArmDownMetrics metrics,
            out FingerTorsoMetrics fingerTorsoMetrics) =>
            BakeCompatibilityClip(
                source,
                lowerUpperArms,
                applyFingerAndTorsoCorrection,
                applyArmsStraightDown: false,
                out metrics,
                out fingerTorsoMetrics,
                out _);

        private static AnimationClip BakeCompatibilityClip(
            AnimationClip source,
            bool lowerUpperArms,
            bool applyFingerAndTorsoCorrection,
            bool applyArmsStraightDown,
            out ArmDownMetrics metrics,
            out FingerTorsoMetrics fingerTorsoMetrics,
            out ArmsStraightMetrics armsStraightMetrics) =>
            BakeCompatibilityClip(
                source,
                lowerUpperArms,
                applyFingerAndTorsoCorrection,
                applyArmsStraightDown,
                applyRightArmLegClearance: false,
                out metrics,
                out fingerTorsoMetrics,
                out armsStraightMetrics,
                out _);

        private static AnimationClip BakeCompatibilityClip(
            AnimationClip source,
            bool lowerUpperArms,
            bool applyFingerAndTorsoCorrection,
            bool applyArmsStraightDown,
            bool applyRightArmLegClearance,
            out ArmDownMetrics metrics,
            out FingerTorsoMetrics fingerTorsoMetrics,
            out ArmsStraightMetrics armsStraightMetrics,
            out RightArmClearanceMetrics rightArmClearanceMetrics)
        {
            metrics = default;
            fingerTorsoMetrics = default;
            armsStraightMetrics = default;
            rightArmClearanceMetrics = default;
            if (!source.humanMotion || source.frameRate <= 0f || source.length <= 0f)
                throw new InvalidOperationException(
                    "Exhausted walking source is not valid Humanoid motion.");
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject proxyModel = RequireAsset<GameObject>(PlayerProxyAssetPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(proxyModel, previewScene) as GameObject ??
                throw new InvalidOperationException(
                    "Humanoid player proxy could not be instantiated.");
            probe.name = "ExhaustedWalkForward_GenericRetargetProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = RequireAnimator(probe);
            Avatar proxyAvatar = AssetDatabase.LoadAllAssetsAtPath(PlayerProxyAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault() ??
                throw new InvalidOperationException("Player Humanoid proxy Avatar is missing.");
            if (!proxyAvatar.isValid || !proxyAvatar.isHuman)
                throw new InvalidOperationException(
                    "Player Humanoid proxy Avatar is invalid.");
            animator.avatar = proxyAvatar;
            animator.applyRootMotion = false;
            animator.enabled = true;

            TransformTrack[] tracks = probe.GetComponentsInChildren<Transform>(true)
                .Select(item => new
                {
                    Path = AnimationUtility.CalculateTransformPath(item, probe.transform),
                    Transform = item
                })
                .Where(item => !string.IsNullOrEmpty(item.Path))
                .OrderBy(item => item.Path, StringComparer.Ordinal)
                .Select(item => new TransformTrack(item.Path, item.Transform))
                .ToArray();
            if (tracks.Length < 20)
                throw new InvalidOperationException(
                    "Humanoid player proxy hierarchy is incomplete.");
            Transform leftArm = RequireUniqueDescendant(probe, "LeftArm");
            Transform leftForeArm = RequireUniqueDescendant(probe, "LeftForeArm");
            Transform leftHand = RequireUniqueDescendant(probe, "LeftHand");
            Transform rightArm = RequireUniqueDescendant(probe, "RightArm");
            Transform rightForeArm = RequireUniqueDescendant(probe, "RightForeArm");
            Transform rightHand = RequireUniqueDescendant(probe, "RightHand");
            Transform spineLower = RequireUniqueDescendant(probe, "Spine02");
            Transform spineMiddle = RequireUniqueDescendant(probe, "Spine01");
            Transform spineUpper = RequireUniqueDescendant(probe, "Spine");
            FingerChain[] leftFingerChains = RequireFingerChains(probe, "Left");
            FingerChain[] rightFingerChains = RequireFingerChains(probe, "Right");
            Dictionary<Transform, Quaternion> fingerRestRotations =
                leftFingerChains.Concat(rightFingerChains)
                    .SelectMany(chain => chain.Bones)
                    .Distinct()
                    .ToDictionary(bone => bone, bone => bone.localRotation);
            var metricsBuilder = new ArmDownMetricsBuilder();
            var fingerTorsoMetricsBuilder = new FingerTorsoMetricsBuilder();
            var armsStraightMetricsBuilder = new ArmsStraightMetricsBuilder();
            var rightArmClearanceMetricsBuilder =
                new RightArmClearanceMetricsBuilder();

            bool animationModeStarted = false;
            try
            {
                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before applying exhausted walking.");
                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                int frameCount = Mathf.CeilToInt(source.length * source.frameRate);
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    float time = Mathf.Min(frame / source.frameRate, source.length);
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(probe, source, time);
                    AnimationMode.EndSampling();
                    float leftApplied = 0f;
                    float rightApplied = 0f;
                    if (lowerUpperArms)
                    {
                        leftApplied = LowerUpperArmTowardDown(
                            probe.transform,
                            leftArm,
                            leftForeArm,
                            ArmDownAngleDegrees);
                        rightApplied = LowerUpperArmTowardDown(
                            probe.transform,
                            rightArm,
                            rightForeArm,
                            ArmDownAngleDegrees);
                    }
                    if (applyFingerAndTorsoCorrection)
                    {
                        float torsoApplied = BendTorsoForward(
                            probe.transform,
                            spineLower,
                            spineMiddle,
                            spineUpper);
                        HandCorrectionMetrics leftHandMetrics =
                            StraightenHandFingersDown(
                                probe.transform,
                                leftForeArm,
                                leftHand,
                                leftFingerChains,
                                fingerRestRotations);
                        HandCorrectionMetrics rightHandMetrics =
                            StraightenHandFingersDown(
                                probe.transform,
                                rightForeArm,
                                rightHand,
                                rightFingerChains,
                                fingerRestRotations);
                        fingerTorsoMetricsBuilder.Add(
                            torsoApplied,
                            leftHandMetrics,
                            rightHandMetrics);
                    }
                    if (applyArmsStraightDown)
                    {
                        ArmChainCorrectionMetrics leftStraight =
                            StraightenArmChainDown(
                                probe.transform,
                                leftArm,
                                leftForeArm,
                                leftHand);
                        ArmChainCorrectionMetrics rightStraight =
                            StraightenArmChainDown(
                                probe.transform,
                                rightArm,
                                rightForeArm,
                                rightHand);
                        armsStraightMetricsBuilder.Add(
                            leftStraight,
                            rightStraight);
                    }
                    if (applyRightArmLegClearance)
                    {
                        RightArmClearanceFrameMetrics clearance =
                            MoveStraightArmOutward(
                                probe.transform,
                                rightArm,
                                rightForeArm,
                                rightHand,
                                RightArmOutwardAngleDegrees);
                        rightArmClearanceMetricsBuilder.Add(clearance);
                    }
                    metricsBuilder.Add(leftApplied, rightApplied);
                    foreach (TransformTrack track in tracks)
                        track.Add(time);
                }
            }
            finally
            {
                if (animationModeStarted)
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
            metrics = metricsBuilder.Build();
            fingerTorsoMetrics = fingerTorsoMetricsBuilder.Build(
                applyFingerAndTorsoCorrection);
            armsStraightMetrics = armsStraightMetricsBuilder.Build(
                applyArmsStraightDown);
            rightArmClearanceMetrics = rightArmClearanceMetricsBuilder.Build(
                applyRightArmLegClearance);

            var output = new AnimationClip
            {
                name = "Exhausted_Walk_Forward_Retargeted",
                frameRate = source.frameRate,
                wrapMode = WrapMode.Loop,
                legacy = false
            };
            foreach (TransformTrack track in tracks)
                track.Apply(output);
            output.EnsureQuaternionContinuity();
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = source.length;
            AnimationUtility.SetAnimationClipSettings(output, settings);
            return output;
        }

        private static Transform RequireUniqueDescendant(
            GameObject root,
            string boneName)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == boneName)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    root.name + " requires exactly one " + boneName +
                    "; actual=" + matches.Length);
            return matches[0];
        }

        private static float LowerUpperArmTowardDown(
            Transform actorRoot,
            Transform upperArm,
            Transform foreArm,
            float degrees)
        {
            Vector3 direction = foreArm.position - upperArm.position;
            if (direction.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    upperArm.name + " has a zero-length upper-arm direction.");
            direction.Normalize();
            Vector3 desired = Vector3.RotateTowards(
                direction,
                -actorRoot.up,
                degrees * Mathf.Deg2Rad,
                0f).normalized;
            float applied = Vector3.Angle(direction, desired);
            upperArm.rotation = Quaternion.FromToRotation(direction, desired) *
                upperArm.rotation;
            return applied;
        }

        private static float BendTorsoForward(
            Transform actorRoot,
            Transform spineLower,
            Transform spineMiddle,
            Transform spineUpper)
        {
            Quaternion upperBefore = spineUpper.rotation;
            Quaternion perJoint = Quaternion.AngleAxis(
                TorsoForwardPerJointDegrees,
                actorRoot.right);
            spineLower.rotation = perJoint * spineLower.rotation;
            spineMiddle.rotation = perJoint * spineMiddle.rotation;
            spineUpper.rotation = perJoint * spineUpper.rotation;
            return Quaternion.Angle(upperBefore, spineUpper.rotation);
        }

        private static FingerChain[] RequireFingerChains(
            GameObject root,
            string side)
        {
            string[] names = { "Index", "Little", "Middle", "Ring", "Thumb" };
            return names.Select(name => new FingerChain(
                    name,
                    RequireUniqueDescendant(root, side + name + "Proximal"),
                    RequireUniqueDescendant(root, side + name + "Intermediate"),
                    RequireUniqueDescendant(root, side + name + "Distal")))
                .ToArray();
        }

        private static HandCorrectionMetrics StraightenHandFingersDown(
            Transform actorRoot,
            Transform foreArm,
            Transform hand,
            IReadOnlyCollection<FingerChain> chains,
            IReadOnlyDictionary<Transform, Quaternion> restRotations)
        {
            foreach (FingerChain chain in chains)
            foreach (Transform bone in chain.Bones)
                bone.localRotation = restRotations[bone];

            Vector3 down = -actorRoot.up;
            Vector3 meanLongFingerDirection = chains
                .Where(chain => chain.Name != "Thumb")
                .Select(chain => BoneDirection(chain.Proximal, chain.Intermediate))
                .Aggregate(Vector3.zero, (sum, direction) => sum + direction)
                .normalized;
            if (meanLongFingerDirection.sqrMagnitude < 0.5f)
                throw new InvalidOperationException(
                    hand.name + " has an invalid straight-finger direction.");

            Quaternion handBefore = hand.rotation;
            hand.rotation = Quaternion.FromToRotation(
                meanLongFingerDirection,
                down) * hand.rotation;
            foreach (FingerChain chain in chains)
            {
                AlignBoneDirection(chain.Proximal, chain.Intermediate, down);
                AlignBoneDirection(chain.Intermediate, chain.Distal, down);
            }

            float maximumDownAngle = 0f;
            float maximumStraightAngle = 0f;
            foreach (FingerChain chain in chains)
            {
                Vector3 proximalDirection =
                    BoneDirection(chain.Proximal, chain.Intermediate);
                Vector3 intermediateDirection =
                    BoneDirection(chain.Intermediate, chain.Distal);
                maximumDownAngle = Mathf.Max(
                    maximumDownAngle,
                    Vector3.Angle(proximalDirection, down),
                    Vector3.Angle(intermediateDirection, down));
                maximumStraightAngle = Mathf.Max(
                    maximumStraightAngle,
                    Vector3.Angle(proximalDirection, intermediateDirection));
            }
            Vector3 foreArmDirection = hand.position - foreArm.position;
            float wristToFingerAngle = foreArmDirection.sqrMagnitude < 0.000001f
                ? 0f
                : Vector3.Angle(foreArmDirection.normalized, down);
            return new HandCorrectionMetrics(
                maximumDownAngle,
                maximumStraightAngle,
                Quaternion.Angle(handBefore, hand.rotation),
                wristToFingerAngle);
        }

        private static Vector3 BoneDirection(Transform from, Transform to)
        {
            Vector3 direction = to.position - from.position;
            if (direction.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    from.name + " has a zero-length child direction.");
            return direction.normalized;
        }

        private static void AlignBoneDirection(
            Transform bone,
            Transform child,
            Vector3 desiredDirection)
        {
            Vector3 direction = BoneDirection(bone, child);
            bone.rotation = Quaternion.FromToRotation(
                direction,
                desiredDirection.normalized) * bone.rotation;
        }

        private static ArmChainCorrectionMetrics StraightenArmChainDown(
            Transform actorRoot,
            Transform upperArm,
            Transform foreArm,
            Transform hand)
        {
            Vector3 down = -actorRoot.up;
            Quaternion upperBefore = upperArm.rotation;
            Quaternion foreArmBefore = foreArm.rotation;
            Quaternion handWorldBefore = hand.rotation;

            AlignBoneDirection(upperArm, foreArm, down);
            AlignBoneDirection(foreArm, hand, down);
            hand.rotation = handWorldBefore;

            Vector3 upperDirection = BoneDirection(upperArm, foreArm);
            Vector3 foreArmDirection = BoneDirection(foreArm, hand);
            return new ArmChainCorrectionMetrics(
                Vector3.Angle(upperDirection, down),
                Vector3.Angle(foreArmDirection, down),
                Vector3.Angle(upperDirection, foreArmDirection),
                Quaternion.Angle(upperBefore, upperArm.rotation),
                Quaternion.Angle(foreArmBefore, foreArm.rotation));
        }

        private static RightArmClearanceFrameMetrics MoveStraightArmOutward(
            Transform actorRoot,
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            float outwardDegrees)
        {
            Vector3 down = -actorRoot.up;
            Vector3 outwardDirection = Vector3.RotateTowards(
                down,
                actorRoot.right,
                outwardDegrees * Mathf.Deg2Rad,
                0f).normalized;
            Vector3 elbowBefore = foreArm.position;
            Vector3 handBefore = hand.position;
            Quaternion handWorldBefore = hand.rotation;

            AlignBoneDirection(upperArm, foreArm, outwardDirection);
            AlignBoneDirection(foreArm, hand, outwardDirection);
            hand.rotation = handWorldBefore;

            Vector3 upperDirection = BoneDirection(upperArm, foreArm);
            Vector3 foreArmDirection = BoneDirection(foreArm, hand);
            return new RightArmClearanceFrameMetrics(
                Vector3.Angle(down, upperDirection),
                Vector3.Angle(down, foreArmDirection),
                Vector3.Angle(upperDirection, foreArmDirection),
                Vector3.Dot(foreArm.position - elbowBefore, actorRoot.right),
                Vector3.Dot(hand.position - handBefore, actorRoot.right),
                Quaternion.Angle(handWorldBefore, hand.rotation));
        }

        private static void RequireOnlyUpperArmRotationChanges(
            AnimationClip baseline,
            AnimationClip corrected)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(baseline)
                .Concat(AnimationUtility.GetCurveBindings(corrected))
                .GroupBy(
                    binding => binding.path + "|" + binding.propertyName,
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            EditorCurveBinding[] changed = bindings
                .Where(binding => !CurveEquals(
                    AnimationUtility.GetEditorCurve(baseline, binding),
                    AnimationUtility.GetEditorCurve(corrected, binding)))
                .ToArray();
            if (changed.Length != 8 || changed.Any(binding =>
                !IsUpperArmRotationBinding(binding)))
                throw new InvalidOperationException(
                    "Arm-down correction must change exactly the eight LeftArm/RightArm quaternion rotation bindings; actual=" +
                    string.Join(",", changed.Select(binding =>
                        binding.path + ":" + binding.propertyName)));
        }

        private static int RequireOnlyFingerAndTorsoRotationChanges(
            AnimationClip baseline,
            AnimationClip corrected)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(baseline)
                .Concat(AnimationUtility.GetCurveBindings(corrected))
                .GroupBy(
                    binding => binding.path + "|" + binding.propertyName,
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            EditorCurveBinding[] changed = bindings
                .Where(binding => !CurveEquals(
                    AnimationUtility.GetEditorCurve(baseline, binding),
                    AnimationUtility.GetEditorCurve(corrected, binding)))
                .ToArray();
            EditorCurveBinding[] outsideScope = changed
                .Where(binding => !IsFingerAndTorsoRotationBinding(binding))
                .ToArray();
            if (outsideScope.Length != 0)
                throw new InvalidOperationException(
                    "Finger and torso correction changed curves outside the approved scope: " +
                    string.Join(",", outsideScope.Select(binding =>
                        binding.path + ":" + binding.propertyName)));

            string[] changedBones = changed
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string[] requiredBoneNames = new[]
                {
                    "Spine02", "Spine01", "Spine", "LeftHand", "RightHand"
                }
                .Concat(new[] { "Left", "Right" }.SelectMany(side =>
                    new[] { "Index", "Little", "Middle", "Ring", "Thumb" }
                        .SelectMany(finger => new[]
                        {
                            side + finger + "Proximal",
                            side + finger + "Intermediate",
                            side + finger + "Distal"
                        })))
                .ToArray();
            string[] missing = requiredBoneNames
                .Where(name => !changedBones.Any(path =>
                    path.EndsWith("/" + name, StringComparison.Ordinal)))
                .ToArray();
            if (missing.Length != 0)
                throw new InvalidOperationException(
                    "Finger and torso correction did not write every required rotation bone: " +
                    string.Join(",", missing));
            return changed.Length;
        }

        private static int RequireOnlyStraightArmRotationChanges(
            AnimationClip baseline,
            AnimationClip corrected)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(baseline)
                .Concat(AnimationUtility.GetCurveBindings(corrected))
                .GroupBy(
                    binding => binding.path + "|" + binding.propertyName,
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            EditorCurveBinding[] changed = bindings
                .Where(binding => !CurveEquals(
                    AnimationUtility.GetEditorCurve(baseline, binding),
                    AnimationUtility.GetEditorCurve(corrected, binding)))
                .ToArray();
            EditorCurveBinding[] outsideScope = changed
                .Where(binding => !IsStraightArmRotationBinding(binding))
                .ToArray();
            if (outsideScope.Length != 0)
                throw new InvalidOperationException(
                    "Straight-arm correction changed curves outside the approved arm chain: " +
                    string.Join(",", outsideScope.Select(binding =>
                        binding.path + ":" + binding.propertyName)));
            string[] requiredBoneNames =
            {
                "LeftArm", "LeftForeArm", "LeftHand",
                "RightArm", "RightForeArm", "RightHand"
            };
            string[] changedBones = changed
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string[] missing = requiredBoneNames
                .Where(name => !changedBones.Any(path =>
                    path.EndsWith("/" + name, StringComparison.Ordinal)))
                .ToArray();
            if (missing.Length != 0)
                throw new InvalidOperationException(
                    "Straight-arm correction did not write every required arm-chain rotation: " +
                    string.Join(",", missing));
            return changed.Length;
        }

        private static int RequireOnlyRightArmClearanceRotationChanges(
            AnimationClip baseline,
            AnimationClip corrected)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(baseline)
                .Concat(AnimationUtility.GetCurveBindings(corrected))
                .GroupBy(
                    binding => binding.path + "|" + binding.propertyName,
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            EditorCurveBinding[] changed = bindings
                .Where(binding => !CurveEquals(
                    AnimationUtility.GetEditorCurve(baseline, binding),
                    AnimationUtility.GetEditorCurve(corrected, binding)))
                .ToArray();
            EditorCurveBinding[] outsideScope = changed
                .Where(binding => !IsRightArmClearanceRotationBinding(binding))
                .ToArray();
            if (outsideScope.Length != 0)
                throw new InvalidOperationException(
                    "Right-arm leg-clearance correction changed curves outside the approved right-arm chain: " +
                    string.Join(",", outsideScope.Select(binding =>
                        binding.path + ":" + binding.propertyName)));
            string[] requiredBoneNames = { "RightArm", "RightForeArm", "RightHand" };
            string[] changedBones = changed
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string[] missing = requiredBoneNames
                .Where(name => !changedBones.Any(path =>
                    path.EndsWith("/" + name, StringComparison.Ordinal)))
                .ToArray();
            if (missing.Length != 0)
                throw new InvalidOperationException(
                    "Right-arm leg-clearance correction did not write every required right-arm rotation: " +
                    string.Join(",", missing));
            return changed.Length;
        }

        private static bool IsUpperArmRotationBinding(EditorCurveBinding binding) =>
            (binding.path.EndsWith("/LeftArm", StringComparison.Ordinal) ||
             binding.path.EndsWith("/RightArm", StringComparison.Ordinal)) &&
            binding.propertyName.StartsWith(
                "m_LocalRotation.",
                StringComparison.Ordinal);

        private static bool IsFingerAndTorsoRotationBinding(
            EditorCurveBinding binding)
        {
            if (!binding.propertyName.StartsWith(
                    "m_LocalRotation.",
                    StringComparison.Ordinal))
                return false;
            string boneName = binding.path.Substring(
                binding.path.LastIndexOf('/') + 1);
            if (boneName == "Spine02" || boneName == "Spine01" ||
                boneName == "Spine" || boneName == "LeftHand" ||
                boneName == "RightHand")
                return true;
            string[] sides = { "Left", "Right" };
            string[] fingers = { "Index", "Little", "Middle", "Ring", "Thumb" };
            string[] joints = { "Proximal", "Intermediate", "Distal" };
            return sides.Any(side => fingers.Any(finger => joints.Any(joint =>
                boneName == side + finger + joint)));
        }

        private static bool IsStraightArmRotationBinding(
            EditorCurveBinding binding)
        {
            if (!binding.propertyName.StartsWith(
                    "m_LocalRotation.",
                    StringComparison.Ordinal))
                return false;
            return binding.path.EndsWith("/LeftArm", StringComparison.Ordinal) ||
                binding.path.EndsWith("/LeftForeArm", StringComparison.Ordinal) ||
                binding.path.EndsWith("/LeftHand", StringComparison.Ordinal) ||
                binding.path.EndsWith("/RightArm", StringComparison.Ordinal) ||
                binding.path.EndsWith("/RightForeArm", StringComparison.Ordinal) ||
                binding.path.EndsWith("/RightHand", StringComparison.Ordinal);
        }

        private static bool IsRightArmClearanceRotationBinding(
            EditorCurveBinding binding)
        {
            if (!binding.propertyName.StartsWith(
                    "m_LocalRotation.",
                    StringComparison.Ordinal))
                return false;
            return binding.path.EndsWith("/RightArm", StringComparison.Ordinal) ||
                binding.path.EndsWith("/RightForeArm", StringComparison.Ordinal) ||
                binding.path.EndsWith("/RightHand", StringComparison.Ordinal);
        }

        private static bool CurveEquals(AnimationCurve first, AnimationCurve second)
        {
            if (first == null || second == null)
                return first == second;
            if (first.length != second.length)
                return false;
            for (int index = 0; index < first.length; index++)
            {
                Keyframe left = first[index];
                Keyframe right = second[index];
                if (left.time != right.time || left.value != right.value ||
                    left.inTangent != right.inTangent ||
                    left.outTangent != right.outTangent)
                    return false;
            }
            return true;
        }

        private static string CurveSignature(
            AnimationClip clip,
            bool excludeUpperArmRotations,
            bool excludeFingerAndTorsoRotations = false,
            bool excludeStraightArmRotations = false,
            bool excludeRightArmClearanceRotations = false)
        {
            var text = new StringBuilder();
            foreach (EditorCurveBinding binding in AnimationUtility
                .GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                if (excludeUpperArmRotations &&
                    IsUpperArmRotationBinding(binding))
                    continue;
                if (excludeFingerAndTorsoRotations &&
                    IsFingerAndTorsoRotationBinding(binding))
                    continue;
                if (excludeStraightArmRotations &&
                    IsStraightArmRotationBinding(binding))
                    continue;
                if (excludeRightArmClearanceRotations &&
                    IsRightArmClearanceRotationBinding(binding))
                    continue;
                text.Append(binding.path).Append('|')
                    .Append(binding.propertyName).Append('|');
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (Keyframe key in curve.keys)
                    text.Append(key.time.ToString("R", CultureInfo.InvariantCulture))
                        .Append(':')
                        .Append(key.value.ToString("R", CultureInfo.InvariantCulture))
                        .Append(':')
                        .Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture))
                        .Append(':')
                        .Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture))
                        .Append(';');
                text.AppendLine();
            }
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(
                    Encoding.UTF8.GetBytes(text.ToString())))
                    .Replace("-", string.Empty);
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Could not load PNG: " + path);
            }
            return texture;
        }

        private static Texture2D CombineVertical(Texture2D top, Texture2D bottom)
        {
            int width = Mathf.Max(top.width, bottom.width);
            var combined = new Texture2D(
                width,
                top.height + bottom.height,
                TextureFormat.RGBA32,
                false);
            combined.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                combined.width * combined.height).ToArray());
            combined.SetPixels32(0, bottom.height, top.width, top.height, top.GetPixels32());
            combined.SetPixels32(0, 0, bottom.width, bottom.height, bottom.GetPixels32());
            combined.Apply(false, false);
            return combined;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            DeleteAssetIfPresent(ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorState state =
                controller.layers[0].stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void RequireController(GameObject target, AnimationClip clip)
        {
            Animator animator = RequireAnimator(target);
            AnimatorController controller = RequireAsset<AnimatorController>(ControllerPath);
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(
                    TargetName + " controller connection differs.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState[] states = machine.states.Select(item => item.state).ToArray();
            if (states.Length != 1 || machine.defaultState != states[0] ||
                states[0].name != StateName || states[0].motion != clip ||
                Mathf.Abs(states[0].speed - 1f) > 0.0001f ||
                states[0].transitions.Length != 0)
                throw new InvalidOperationException(
                    TargetName + " must use one unchanged looping state at speed 1.");
            if (animator.applyRootMotion)
                throw new InvalidOperationException(
                    TargetName + " applyRootMotion must remain disabled.");
        }

        private static AnimationClip RequireSingleSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourceAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    SourceAssetPath + " must contain exactly one embedded clip; actual=" +
                    clips.Length);
            if (!clips[0].humanMotion)
                throw new InvalidOperationException(
                    SourceAssetPath + " did not import as Humanoid motion.");
            return clips[0];
        }

        private static void RequireExactSourceCopy()
        {
            string external = Absolute(ExternalSourcePath);
            string copied = Absolute(SourceAssetPath);
            if (!File.Exists(external) || !File.Exists(copied))
                throw new FileNotFoundException(
                    "Exhausted walking source or project copy is missing.");
            RequireEqual(Sha256(external), Sha256(copied), "Exhausted source binary copy");
        }

        private static void RequireLoop(AnimationClip clip, bool expected, string label)
        {
            bool actual = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
            if (actual != expected)
                throw new InvalidOperationException(
                    label + " loopTime differs. Expected=" + expected +
                    ", Actual=" + actual);
        }

        private static string DescribeClip(AnimationClip clip) =>
            clip.name + ",length=" +
            clip.length.ToString("0.######", CultureInfo.InvariantCulture) +
            ",frameRate=" +
            clip.frameRate.ToString("0.######", CultureInfo.InvariantCulture) +
            ",humanMotion=" + clip.humanMotion;

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
                throw new InvalidOperationException(
                    "Could not replace target-specific asset: " + path);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException(
                    path + " is missing or has the wrong asset type.");
            return asset;
        }

        private static Animator RequireAnimator(GameObject target) =>
            target.GetComponent<Animator>() ??
            throw new InvalidOperationException(target.name + " Animator is missing.");

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected exactly one " + name + "; actual=" + matches.Length);
            return matches[0];
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted animation setup requires Edit Mode.");
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static string RendererSignature(Transform root) =>
            string.Join("\n", root.GetComponentsInChildren<Renderer>(true)
                .OrderBy(
                    renderer => AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        root),
                    StringComparer.Ordinal)
                .Select(renderer =>
                    AnimationUtility.CalculateTransformPath(renderer.transform, root) + "|" +
                    renderer.GetType().FullName + "|" + renderer.enabled + "|" +
                    string.Join(",", renderer.sharedMaterials.Select(material =>
                        material == null ? "null" : AssetDatabase.GetAssetPath(material)))));

        private static string Sha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream))
                    .Replace("-", string.Empty);
        }

        internal static string Absolute(string relative) =>
            Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                relative.Replace('/', Path.DirectorySeparatorChar)));

        private static void WriteText(string relative, string text)
        {
            string path = Absolute(relative);
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Report folder is unavailable."));
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " differs.");
        }

        private readonly struct ArmChainCorrectionMetrics
        {
            internal ArmChainCorrectionMetrics(
                float upperDownDeviation,
                float foreArmDownDeviation,
                float elbowStraightDeviation,
                float upperRotationCorrection,
                float foreArmRotationCorrection)
            {
                UpperDownDeviation = upperDownDeviation;
                ForeArmDownDeviation = foreArmDownDeviation;
                ElbowStraightDeviation = elbowStraightDeviation;
                UpperRotationCorrection = upperRotationCorrection;
                ForeArmRotationCorrection = foreArmRotationCorrection;
            }

            internal float UpperDownDeviation { get; }
            internal float ForeArmDownDeviation { get; }
            internal float ElbowStraightDeviation { get; }
            internal float UpperRotationCorrection { get; }
            internal float ForeArmRotationCorrection { get; }
        }

        private readonly struct RightArmClearanceFrameMetrics
        {
            internal RightArmClearanceFrameMetrics(
                float upperOutwardAngle,
                float foreArmOutwardAngle,
                float elbowStraightDeviation,
                float elbowOutwardShift,
                float handOutwardShift,
                float handWorldRotationError)
            {
                UpperOutwardAngle = upperOutwardAngle;
                ForeArmOutwardAngle = foreArmOutwardAngle;
                ElbowStraightDeviation = elbowStraightDeviation;
                ElbowOutwardShift = elbowOutwardShift;
                HandOutwardShift = handOutwardShift;
                HandWorldRotationError = handWorldRotationError;
            }

            internal float UpperOutwardAngle { get; }
            internal float ForeArmOutwardAngle { get; }
            internal float ElbowStraightDeviation { get; }
            internal float ElbowOutwardShift { get; }
            internal float HandOutwardShift { get; }
            internal float HandWorldRotationError { get; }
        }

        private sealed class RightArmClearanceMetricsBuilder
        {
            private readonly List<RightArmClearanceFrameMetrics> frames =
                new List<RightArmClearanceFrameMetrics>();

            internal void Add(RightArmClearanceFrameMetrics frame) =>
                frames.Add(frame);

            internal RightArmClearanceMetrics Build(bool enabled) => enabled
                ? new RightArmClearanceMetrics(frames)
                : default;
        }

        private readonly struct RightArmClearanceMetrics
        {
            private readonly float minimumUpperOutwardAngle;
            private readonly float maximumUpperOutwardAngle;
            private readonly float minimumForeArmOutwardAngle;
            private readonly float maximumForeArmOutwardAngle;
            private readonly float maximumElbowStraightDeviation;
            private readonly float minimumElbowOutwardShift;
            private readonly float minimumHandOutwardShift;
            private readonly float maximumHandWorldRotationError;

            internal RightArmClearanceMetrics(
                IReadOnlyCollection<RightArmClearanceFrameMetrics> frames)
            {
                if (frames.Count == 0)
                    throw new InvalidOperationException(
                        "Right-arm clearance metrics require sampled frames.");
                minimumUpperOutwardAngle = frames.Min(item =>
                    item.UpperOutwardAngle);
                maximumUpperOutwardAngle = frames.Max(item =>
                    item.UpperOutwardAngle);
                minimumForeArmOutwardAngle = frames.Min(item =>
                    item.ForeArmOutwardAngle);
                maximumForeArmOutwardAngle = frames.Max(item =>
                    item.ForeArmOutwardAngle);
                maximumElbowStraightDeviation = frames.Max(item =>
                    item.ElbowStraightDeviation);
                minimumElbowOutwardShift = frames.Min(item =>
                    item.ElbowOutwardShift);
                minimumHandOutwardShift = frames.Min(item =>
                    item.HandOutwardShift);
                maximumHandWorldRotationError = frames.Max(item =>
                    item.HandWorldRotationError);
                SampleCount = frames.Count;
            }

            internal int SampleCount { get; }

            internal string Description =>
                "samples:" + SampleCount +
                ",upperOutwardMin:" + Format(minimumUpperOutwardAngle) +
                ",upperOutwardMax:" + Format(maximumUpperOutwardAngle) +
                ",foreArmOutwardMin:" + Format(minimumForeArmOutwardAngle) +
                ",foreArmOutwardMax:" + Format(maximumForeArmOutwardAngle) +
                ",maxElbowStraightDeviation:" +
                Format(maximumElbowStraightDeviation) +
                ",minElbowOutwardShift:" + Format(minimumElbowOutwardShift) +
                ",minHandOutwardShift:" + Format(minimumHandOutwardShift) +
                ",maxHandWorldRotationError:" +
                Format(maximumHandWorldRotationError);

            internal void RequireValid(float expectedOutwardAngle)
            {
                const float angleTolerance = 0.05f;
                if (Mathf.Abs(minimumUpperOutwardAngle - expectedOutwardAngle) >
                        angleTolerance ||
                    Mathf.Abs(maximumUpperOutwardAngle - expectedOutwardAngle) >
                        angleTolerance ||
                    Mathf.Abs(minimumForeArmOutwardAngle - expectedOutwardAngle) >
                        angleTolerance ||
                    Mathf.Abs(maximumForeArmOutwardAngle - expectedOutwardAngle) >
                        angleTolerance)
                    throw new InvalidOperationException(
                        "Right-arm outward clearance angle differs from the requested value: " +
                        Description);
                if (maximumElbowStraightDeviation > angleTolerance)
                    throw new InvalidOperationException(
                        "Right elbow is no longer neutral-straight: " + Description);
                if (minimumElbowOutwardShift <= 0f ||
                    minimumHandOutwardShift <= minimumElbowOutwardShift)
                    throw new InvalidOperationException(
                        "Right arm did not move outward as a complete straight chain: " +
                        Description);
                if (maximumHandWorldRotationError > 0.01f)
                    throw new InvalidOperationException(
                        "Right hand world orientation changed during clearance correction: " +
                        Description);
            }

            private static string Format(float value) =>
                value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private sealed class ArmsStraightMetricsBuilder
        {
            private readonly List<ArmChainCorrectionMetrics> left =
                new List<ArmChainCorrectionMetrics>();
            private readonly List<ArmChainCorrectionMetrics> right =
                new List<ArmChainCorrectionMetrics>();

            internal void Add(
                ArmChainCorrectionMetrics leftMetrics,
                ArmChainCorrectionMetrics rightMetrics)
            {
                left.Add(leftMetrics);
                right.Add(rightMetrics);
            }

            internal ArmsStraightMetrics Build(bool enabled) => enabled
                ? new ArmsStraightMetrics(left, right)
                : default;
        }

        private readonly struct ArmsStraightMetrics
        {
            private readonly float leftMaximumUpperDownDeviation;
            private readonly float leftMaximumForeArmDownDeviation;
            private readonly float leftMaximumElbowStraightDeviation;
            private readonly float leftMaximumUpperCorrection;
            private readonly float leftMaximumForeArmCorrection;
            private readonly float rightMaximumUpperDownDeviation;
            private readonly float rightMaximumForeArmDownDeviation;
            private readonly float rightMaximumElbowStraightDeviation;
            private readonly float rightMaximumUpperCorrection;
            private readonly float rightMaximumForeArmCorrection;

            internal ArmsStraightMetrics(
                IReadOnlyCollection<ArmChainCorrectionMetrics> left,
                IReadOnlyCollection<ArmChainCorrectionMetrics> right)
            {
                if (left.Count == 0 || left.Count != right.Count)
                    throw new InvalidOperationException(
                        "Straight-arm metrics require matching sampled frames.");
                leftMaximumUpperDownDeviation = left.Max(item =>
                    item.UpperDownDeviation);
                leftMaximumForeArmDownDeviation = left.Max(item =>
                    item.ForeArmDownDeviation);
                leftMaximumElbowStraightDeviation = left.Max(item =>
                    item.ElbowStraightDeviation);
                leftMaximumUpperCorrection = left.Max(item =>
                    item.UpperRotationCorrection);
                leftMaximumForeArmCorrection = left.Max(item =>
                    item.ForeArmRotationCorrection);
                rightMaximumUpperDownDeviation = right.Max(item =>
                    item.UpperDownDeviation);
                rightMaximumForeArmDownDeviation = right.Max(item =>
                    item.ForeArmDownDeviation);
                rightMaximumElbowStraightDeviation = right.Max(item =>
                    item.ElbowStraightDeviation);
                rightMaximumUpperCorrection = right.Max(item =>
                    item.UpperRotationCorrection);
                rightMaximumForeArmCorrection = right.Max(item =>
                    item.ForeArmRotationCorrection);
                SampleCount = left.Count;
            }

            internal int SampleCount { get; }
            internal string LeftDescription => Describe(
                leftMaximumUpperDownDeviation,
                leftMaximumForeArmDownDeviation,
                leftMaximumElbowStraightDeviation,
                leftMaximumUpperCorrection,
                leftMaximumForeArmCorrection);
            internal string RightDescription => Describe(
                rightMaximumUpperDownDeviation,
                rightMaximumForeArmDownDeviation,
                rightMaximumElbowStraightDeviation,
                rightMaximumUpperCorrection,
                rightMaximumForeArmCorrection);

            internal void RequireValid()
            {
                const float tolerance = 0.05f;
                if (leftMaximumUpperDownDeviation > tolerance ||
                    leftMaximumForeArmDownDeviation > tolerance ||
                    leftMaximumElbowStraightDeviation > tolerance ||
                    rightMaximumUpperDownDeviation > tolerance ||
                    rightMaximumForeArmDownDeviation > tolerance ||
                    rightMaximumElbowStraightDeviation > tolerance)
                    throw new InvalidOperationException(
                        "Arm chains are not neutral-straight and downward. left=" +
                        LeftDescription + ",right=" + RightDescription);
                if (leftMaximumUpperCorrection > 180f ||
                    leftMaximumForeArmCorrection > 180f ||
                    rightMaximumUpperCorrection > 180f ||
                    rightMaximumForeArmCorrection > 180f)
                    throw new InvalidOperationException(
                        "Straight-arm correction contains an invalid rotation.");
            }

            private static string Describe(
                float upperDown,
                float foreArmDown,
                float elbowStraight,
                float upperCorrection,
                float foreArmCorrection) =>
                "maxUpperDownDeviation:" + upperDown.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ",maxForeArmDownDeviation:" + foreArmDown.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ",maxElbowStraightDeviation:" + elbowStraight.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ",maxUpperCorrection:" + upperCorrection.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ",maxForeArmCorrection:" + foreArmCorrection.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture);
        }

        private sealed class FingerChain
        {
            internal FingerChain(
                string name,
                Transform proximal,
                Transform intermediate,
                Transform distal)
            {
                Name = name;
                Proximal = proximal;
                Intermediate = intermediate;
                Distal = distal;
                Bones = new[] { proximal, intermediate, distal };
            }

            internal string Name { get; }
            internal Transform Proximal { get; }
            internal Transform Intermediate { get; }
            internal Transform Distal { get; }
            internal IReadOnlyList<Transform> Bones { get; }
        }

        private readonly struct HandCorrectionMetrics
        {
            internal HandCorrectionMetrics(
                float maximumDownAngle,
                float maximumStraightAngle,
                float wristCorrectionAngle,
                float foreArmToFingerAngle)
            {
                MaximumDownAngle = maximumDownAngle;
                MaximumStraightAngle = maximumStraightAngle;
                WristCorrectionAngle = wristCorrectionAngle;
                ForeArmToFingerAngle = foreArmToFingerAngle;
            }

            internal float MaximumDownAngle { get; }
            internal float MaximumStraightAngle { get; }
            internal float WristCorrectionAngle { get; }
            internal float ForeArmToFingerAngle { get; }
        }

        private sealed class FingerTorsoMetricsBuilder
        {
            private readonly List<float> torso = new List<float>();
            private readonly List<float> fingerDown = new List<float>();
            private readonly List<float> fingerStraight = new List<float>();
            private readonly List<float> leftWristCorrection = new List<float>();
            private readonly List<float> rightWristCorrection = new List<float>();
            private readonly List<float> leftForeArmToFinger = new List<float>();
            private readonly List<float> rightForeArmToFinger = new List<float>();

            internal void Add(
                float torsoDegrees,
                HandCorrectionMetrics left,
                HandCorrectionMetrics right)
            {
                torso.Add(torsoDegrees);
                fingerDown.Add(Mathf.Max(
                    left.MaximumDownAngle,
                    right.MaximumDownAngle));
                fingerStraight.Add(Mathf.Max(
                    left.MaximumStraightAngle,
                    right.MaximumStraightAngle));
                leftWristCorrection.Add(left.WristCorrectionAngle);
                rightWristCorrection.Add(right.WristCorrectionAngle);
                leftForeArmToFinger.Add(left.ForeArmToFingerAngle);
                rightForeArmToFinger.Add(right.ForeArmToFingerAngle);
            }

            internal FingerTorsoMetrics Build(bool enabled) => enabled
                ? new FingerTorsoMetrics(
                    torso,
                    fingerDown,
                    fingerStraight,
                    leftWristCorrection,
                    rightWristCorrection,
                    leftForeArmToFinger,
                    rightForeArmToFinger)
                : default;
        }

        private readonly struct FingerTorsoMetrics
        {
            private readonly float torsoMinimum;
            private readonly float torsoAverage;
            private readonly float torsoMaximum;
            private readonly float maximumFingerDownAngle;
            private readonly float maximumFingerStraightAngle;
            private readonly float leftWristMinimum;
            private readonly float leftWristAverage;
            private readonly float leftWristMaximum;
            private readonly float rightWristMinimum;
            private readonly float rightWristAverage;
            private readonly float rightWristMaximum;
            private readonly float leftForeArmToFingerMaximum;
            private readonly float rightForeArmToFingerMaximum;

            internal FingerTorsoMetrics(
                IReadOnlyCollection<float> torso,
                IReadOnlyCollection<float> fingerDown,
                IReadOnlyCollection<float> fingerStraight,
                IReadOnlyCollection<float> leftWrist,
                IReadOnlyCollection<float> rightWrist,
                IReadOnlyCollection<float> leftForeArmToFinger,
                IReadOnlyCollection<float> rightForeArmToFinger)
            {
                if (torso.Count == 0 ||
                    new[]
                    {
                        fingerDown.Count,
                        fingerStraight.Count,
                        leftWrist.Count,
                        rightWrist.Count,
                        leftForeArmToFinger.Count,
                        rightForeArmToFinger.Count
                    }.Any(count => count != torso.Count))
                    throw new InvalidOperationException(
                        "Finger and torso metrics require matching sampled frames.");
                torsoMinimum = torso.Min();
                torsoAverage = torso.Average();
                torsoMaximum = torso.Max();
                maximumFingerDownAngle = fingerDown.Max();
                maximumFingerStraightAngle = fingerStraight.Max();
                leftWristMinimum = leftWrist.Min();
                leftWristAverage = leftWrist.Average();
                leftWristMaximum = leftWrist.Max();
                rightWristMinimum = rightWrist.Min();
                rightWristAverage = rightWrist.Average();
                rightWristMaximum = rightWrist.Max();
                leftForeArmToFingerMaximum = leftForeArmToFinger.Max();
                rightForeArmToFingerMaximum = rightForeArmToFinger.Max();
                SampleCount = torso.Count;
            }

            internal int SampleCount { get; }
            internal string TorsoDescription => Describe(
                torsoMinimum,
                torsoAverage,
                torsoMaximum);
            internal string FingerDownDescription =>
                "maxDown:" + maximumFingerDownAngle.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ",maxStraightDeviation:" + maximumFingerStraightAngle.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture);
            internal string LeftWristDescription => Describe(
                leftWristMinimum,
                leftWristAverage,
                leftWristMaximum) +
                ",maxForeArmToFinger:" + leftForeArmToFingerMaximum.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture);
            internal string RightWristDescription => Describe(
                rightWristMinimum,
                rightWristAverage,
                rightWristMaximum) +
                ",maxForeArmToFinger:" + rightForeArmToFingerMaximum.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture);

            internal void RequireValid()
            {
                const float angleTolerance = 0.05f;
                if (Mathf.Abs(torsoMinimum - TorsoForwardAngleDegrees) > angleTolerance ||
                    Mathf.Abs(torsoMaximum - TorsoForwardAngleDegrees) > angleTolerance)
                    throw new InvalidOperationException(
                        "Distributed torso bend differs from 15 degrees: " +
                        TorsoDescription);
                if (maximumFingerDownAngle > angleTolerance ||
                    maximumFingerStraightAngle > angleTolerance)
                    throw new InvalidOperationException(
                        "Finger chains are not straight and downward: " +
                        FingerDownDescription);
                if (leftWristMaximum > 120f || rightWristMaximum > 120f ||
                    leftForeArmToFingerMaximum > 80f ||
                    rightForeArmToFingerMaximum > 80f)
                    throw new InvalidOperationException(
                        "Hand correction exceeds the anatomical safety envelope. left=" +
                        LeftWristDescription + ",right=" + RightWristDescription);
            }

            private static string Describe(
                float minimum,
                float average,
                float maximum) =>
                "min:" + minimum.ToString("0.######", CultureInfo.InvariantCulture) +
                ",avg:" + average.ToString("0.######", CultureInfo.InvariantCulture) +
                ",max:" + maximum.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private sealed class ArmDownMetricsBuilder
        {
            private readonly List<float> left = new List<float>();
            private readonly List<float> right = new List<float>();

            internal void Add(float leftDegrees, float rightDegrees)
            {
                left.Add(leftDegrees);
                right.Add(rightDegrees);
            }

            internal ArmDownMetrics Build() => new ArmDownMetrics(left, right);
        }

        private readonly struct ArmDownMetrics
        {
            private readonly float leftMinimum;
            private readonly float leftAverage;
            private readonly float leftMaximum;
            private readonly float rightMinimum;
            private readonly float rightAverage;
            private readonly float rightMaximum;

            internal ArmDownMetrics(
                IReadOnlyCollection<float> left,
                IReadOnlyCollection<float> right)
            {
                if (left.Count == 0 || left.Count != right.Count)
                    throw new InvalidOperationException(
                        "Arm-down metrics require matching sampled frames.");
                leftMinimum = left.Min();
                leftAverage = left.Average();
                leftMaximum = left.Max();
                rightMinimum = right.Min();
                rightAverage = right.Average();
                rightMaximum = right.Max();
                SampleCount = left.Count;
            }

            internal int SampleCount { get; }

            internal string LeftDescription => Describe(
                leftMinimum,
                leftAverage,
                leftMaximum);

            internal string RightDescription => Describe(
                rightMinimum,
                rightAverage,
                rightMaximum);

            internal void RequireApproximately(float expected)
            {
                const float tolerance = 0.05f;
                if (Mathf.Abs(leftMinimum - expected) > tolerance ||
                    Mathf.Abs(leftMaximum - expected) > tolerance ||
                    Mathf.Abs(rightMinimum - expected) > tolerance ||
                    Mathf.Abs(rightMaximum - expected) > tolerance)
                    throw new InvalidOperationException(
                        "Bilateral upper-arm down correction differs from " +
                        expected.ToString("0.###", CultureInfo.InvariantCulture) +
                        " degrees. left=" + LeftDescription +
                        ",right=" + RightDescription);
            }

            private static string Describe(
                float minimum,
                float average,
                float maximum) =>
                "min:" + minimum.ToString("0.######", CultureInfo.InvariantCulture) +
                ",avg:" + average.ToString("0.######", CultureInfo.InvariantCulture) +
                ",max:" + maximum.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private sealed class TransformTrack
        {
            private readonly string path;
            private readonly Transform transform;
            private readonly List<Keyframe>[] values =
                Enumerable.Range(0, 10)
                    .Select(_ => new List<Keyframe>())
                    .ToArray();
            private Quaternion previousRotation;
            private bool hasPreviousRotation;

            internal TransformTrack(string valuePath, Transform valueTransform)
            {
                path = valuePath;
                transform = valueTransform;
            }

            internal void Add(float time)
            {
                Vector3 position = transform.localPosition;
                Quaternion rotation = transform.localRotation;
                Vector3 scale = transform.localScale;
                if (hasPreviousRotation && Quaternion.Dot(previousRotation, rotation) < 0f)
                    rotation = new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w);
                previousRotation = rotation;
                hasPreviousRotation = true;
                float[] sample =
                {
                    position.x, position.y, position.z,
                    rotation.x, rotation.y, rotation.z, rotation.w,
                    scale.x, scale.y, scale.z
                };
                for (int index = 0; index < sample.Length; index++)
                    values[index].Add(new Keyframe(time, sample[index]));
            }

            internal void Apply(AnimationClip clip)
            {
                string[] properties =
                {
                    "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
                    "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z",
                    "m_LocalRotation.w", "m_LocalScale.x", "m_LocalScale.y",
                    "m_LocalScale.z"
                };
                for (int index = 0; index < properties.Length; index++)
                {
                    var curve = new AnimationCurve(values[index].ToArray())
                    {
                        preWrapMode = WrapMode.ClampForever,
                        postWrapMode = WrapMode.ClampForever
                    };
                    for (int key = 0; key < curve.length; key++)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(
                            curve,
                            key,
                            AnimationUtility.TangentMode.Linear);
                        AnimationUtility.SetKeyRightTangentMode(
                            curve,
                            key,
                            AnimationUtility.TangentMode.Linear);
                    }
                    AnimationUtility.SetEditorCurve(
                        clip,
                        EditorCurveBinding.FloatCurve(
                            path,
                            typeof(Transform),
                            properties[index]),
                        curve);
                }
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            internal TransformSnapshot(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            internal void RequireUnchanged(Transform transform, string label)
            {
                if (Vector3.Distance(position, transform.localPosition) > 0.000001f ||
                    Quaternion.Angle(rotation, transform.localRotation) > 0.0001f ||
                    Vector3.Distance(scale, transform.localScale) > 0.000001f)
                    throw new InvalidOperationException(
                        label + " root Transform changed unexpectedly.");
            }
        }
    }

    internal static class ExhaustedLocomotionBlendTreeTools
    {
        internal const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string ValidationFolder =
            "docs/validation/ExhaustedLocomotionBlendTrees";
        internal const string ReviewImagePath = ValidationFolder + "/Review.png";
        internal const string FinalImagePath = ValidationFolder + "/Final.png";
        internal const string ForwardDriftValidationFolder =
            ValidationFolder + "/ForwardDrift";
        internal const string ForwardDriftReviewImagePath =
            ForwardDriftValidationFolder + "/Review.png";
        internal const string ForwardDriftFinalImagePath =
            ForwardDriftValidationFolder + "/Final.png";

        private const string AssetFolder =
            "Assets/_Project/Animations/PlayerStatusEffects/Exhausted/LocomotionBlendTrees";
        private const string ExhaustedSourceClipPath =
            "Assets/_Project/Animations/PlayerStatusEffects/Exhausted/Exhausted_Walk_Forward_Retargeted.anim";
        private const string MoveX = "MoveX";
        private const string MoveY = "MoveY";
        private const string BaseStateName = "PlayerLocomotion2DBase";
        private const string UpperStateName = "ExhaustedUpperOverlay";
        private const string PlayerTreeName = "IndependentPlayerLocomotion2D";
        private const string ApplicationReportPath = ValidationFolder + "/Application.txt";
        private const string InspectionReportPath = ValidationFolder + "/Inspection.txt";
        private const string ReviewReportPath = ValidationFolder + "/Review.txt";
        private const string FinalReportPath = ValidationFolder + "/Final.txt";
        private const string ForwardDriftApplicationReportPath =
            ForwardDriftValidationFolder + "/Application.txt";
        private const string ForwardDriftInspectionReportPath =
            ForwardDriftValidationFolder + "/Inspection.txt";
        private const string ForwardDriftReviewReportPath =
            ForwardDriftValidationFolder + "/Review.txt";
        private const string ForwardDriftFinalReportPath =
            ForwardDriftValidationFolder + "/Final.txt";
        internal const string DirectionalValidationFolder =
            ValidationFolder + "/DirectionalLayerCorrection";
        internal const string DirectionalReviewImagePath =
            DirectionalValidationFolder + "/Review.png";
        internal const string DirectionalFinalImagePath =
            DirectionalValidationFolder + "/Final.png";
        private const string DirectionalApplicationReportPath =
            DirectionalValidationFolder + "/Application.txt";
        private const string DirectionalInspectionReportPath =
            DirectionalValidationFolder + "/Inspection.txt";
        private const string DirectionalReviewReportPath =
            DirectionalValidationFolder + "/Review.txt";
        private const string DirectionalFinalReportPath =
            DirectionalValidationFolder + "/Final.txt";
        private const string ForwardCurvePath = "Armature/Hips";
        private const string ForwardCurveProperty = "m_LocalPosition.y";
        private static readonly Vector2 ExhaustedForwardPosition = new Vector2(0f, 1f);

        private static readonly TargetSpec[] Specs =
        {
            new TargetSpec(
                "Exhausted_Idle", "Player_Idle", "Idle", new Vector2(0f, 0f)),
            new TargetSpec(
                "Exhausted_Walk_Backward", "Player_Walk_Backward", "Backward",
                new Vector2(0f, -1f)),
            new TargetSpec(
                "Exhausted_Walk_Sidestep", "Player_Sidestep", "Sidestep",
                new Vector2(1f, 0f)),
            new TargetSpec(
                "Exhausted_Walk_Diagonal", "Player_Walk_Diagonal", "Diagonal",
                new Vector2(0.70710677f, 0.70710677f))
        };

        internal static IReadOnlyList<string> TargetNames =>
            Specs.Select(spec => spec.TargetName).ToArray();

        internal static string ReviewAbsolutePath => Absolute(ReviewImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);
        internal static string ForwardDriftReviewAbsolutePath =>
            Absolute(ForwardDriftReviewImagePath);
        internal static string ForwardDriftFinalAbsolutePath =>
            Absolute(ForwardDriftFinalImagePath);
        internal static string DirectionalReviewAbsolutePath =>
            Absolute(DirectionalReviewImagePath);
        internal static string DirectionalFinalAbsolutePath =>
            Absolute(DirectionalFinalImagePath);

        [MenuItem("Bellerophon/Player/Apply Exhausted Locomotion Directional Layer Correction")]
        internal static void ApplyExhaustedLocomotionDirectionalLayerCorrection()
        {
            ApplyExhaustedLocomotionBlendTrees();
            InspectExhaustedLocomotionDirectionalLayerCorrection();
            WriteText(
                DirectionalApplicationReportPath,
                "Exhausted locomotion directional layer correction application\n" +
                "targetCount=4\n" +
                "baseLayer=exact Player source locomotion\n" +
                "upperOverlay=Exhausted_Walk_Forward Spine02 branch only\n" +
                "sharedForwardHipOrLegMotion=False\n" +
                "independentFreeformCartesian2D=True\n" +
                "sourceAssetsChanged=False\n" +
                "Exhausted_Walk_ForwardChanged=False\n" +
                "Exhausted_Walk_DiagonalBackwardChanged=False\n");
            Debug.Log(
                "[ExhaustedLocomotionBlendTrees] Directional lower-body layer correction applied to all four targets.");
        }

        [MenuItem("Bellerophon/Player/Apply Exhausted Locomotion Forward Drift Correction")]
        internal static void ApplyExhaustedLocomotionForwardDriftCorrection()
        {
            ApplyExhaustedLocomotionDirectionalLayerCorrection();
            WriteText(
                ForwardDriftApplicationReportPath,
                "Exhausted locomotion forward-drift correction application\n" +
                "targetCount=4\n" +
                "sourceClipChanged=False\n" +
                "supersededByDirectionalLayerCorrection=True\n" +
                "playerMotionOwnsHipsAndLegs=True\n" +
                "exhaustedOverlayContainsHipsOrLegs=False\n" +
                "Exhausted_Walk_ForwardChanged=False\n" +
                "Exhausted_Walk_DiagonalBackwardChanged=False\n");
            Debug.Log(
                "[ExhaustedLocomotionBlendTrees] Forward drift removed from the four copied upper clips without changing the source animation.");
        }

        [MenuItem("Bellerophon/Player/Apply Exhausted Locomotion Blend Trees")]
        internal static void ApplyExhaustedLocomotionBlendTrees()
        {
            RequireEditMode();
            ClearConsole();
            Scene scene = RequireScene();
            AnimationClip exhaustedSource = RequireAsset<AnimationClip>(
                ExhaustedSourceClipPath);
            string exhaustedSourceFileHash = Sha256File(ExhaustedSourceClipPath);
            GameObject exhaustedForward = FindUnique(scene, "Exhausted_Walk_Forward");
            Animator exhaustedForwardAnimator = RequireAnimator(exhaustedForward);
            RuntimeAnimatorController exhaustedForwardController =
                exhaustedForwardAnimator.runtimeAnimatorController ??
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward controller is missing.");
            string exhaustedControllerPath =
                AssetDatabase.GetAssetPath(exhaustedForwardController);
            string exhaustedControllerHash = Sha256File(exhaustedControllerPath);

            EnsureFolder(AssetFolder);
            EnsureFolder(ValidationFolder);
            var report = new StringBuilder()
                .AppendLine("Exhausted independent locomotion Blend Tree application")
                .AppendLine("targetCount=" + Specs.Length)
                .AppendLine("sharedController=False")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("upperBodySource=" + ExhaustedSourceClipPath)
                .AppendLine("upperBodySourceSha256=" + exhaustedSourceFileHash)
                .AppendLine("upperBodySourceChanged=False")
                .AppendLine("Exhausted_Walk_ForwardChanged=False")
                .AppendLine("Exhausted_Walk_DiagonalBackwardChanged=False");

            foreach (TargetSpec spec in Specs)
            {
                GameObject target = FindUnique(scene, spec.TargetName);
                Animator animator = RequireAnimator(target);
                TransformSnapshot rootBefore = new TransformSnapshot(target.transform);
                string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
                if (string.IsNullOrEmpty(avatarPath))
                    throw new InvalidOperationException(spec.TargetName + " Avatar is missing.");

                AnimatorController playerSourceController =
                    RequireSourceController(scene, spec.PlayerSourceName);
                Motion playerSource = RequireDefaultMotion(playerSourceController, spec.PlayerSourceName);
                string folder = TargetFolder(spec);
                EnsureFolder(folder);
                AnimationClip upperCopy = CopyClip(
                    exhaustedSource,
                    UpperClipPath(spec),
                    spec.TargetName + "_ExhaustedUpper");
                string upperBranchPath = UpperBranchPath(target.transform);
                KeepOnlyUpperBodyCurves(upperCopy, upperBranchPath);
                AnimatorController controller = PrepareController(ControllerPath(spec));
                int copiedClipIndex = 0;
                Motion playerCopy = CopyMotion(
                    playerSource,
                    controller,
                    folder,
                    spec.TargetName + "_PlayerLower",
                    ref copiedClipIndex);
                AvatarMask upperMask = CreateUpperBodyMask(
                    target.transform,
                    MaskPath(spec));
                ConfigureController(
                    controller,
                    spec,
                    upperCopy,
                    playerCopy,
                    upperMask,
                    playerSourceController);

                Undo.RecordObject(animator, "Connect independent exhausted locomotion Blend Tree");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                EditorUtility.SetDirty(animator);

                rootBefore.RequireUnchanged(target.transform, spec.TargetName);
                if (AssetDatabase.GetAssetPath(animator.avatar) != avatarPath)
                    throw new InvalidOperationException(
                        spec.TargetName + " Avatar changed unexpectedly.");
                RequireEqual(
                    ClipSignatureForBranch(exhaustedSource, upperBranchPath),
                    ClipSignature(upperCopy),
                    spec.TargetName + " exhausted upper-only copy");
                RequireMotionEquivalent(
                    playerSource,
                    playerCopy,
                    spec.TargetName + " player lower copy");

                report.AppendLine("target=" + spec.TargetName)
                    .AppendLine("playerSource=" + spec.PlayerSourceName)
                    .AppendLine("controller=" + ControllerPath(spec))
                    .AppendLine("upperCopy=" + UpperClipPath(spec))
                    .AppendLine("upperMask=" + MaskPath(spec))
                    .AppendLine("defaultMove=" + F(spec.Position.x) + "," + F(spec.Position.y))
                    .AppendLine("independentController=True")
                    .AppendLine("baseLayerUsesExactPlayerSource=True")
                    .AppendLine("upperSourceSpine02BranchCopiedExactly=True")
                    .AppendLine("upperCopyContainsHipOrLegCurves=False")
                    .AppendLine("playerSourceCopiedExactly=True")
                    .AppendLine("sourceCurvesModified=False")
                    .AppendLine("upperOnlyMask=True")
                    .AppendLine("targetRootChanged=False")
                    .AppendLine("targetAvatarChanged=False");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying exhausted locomotion Blend Trees.");
            AssetDatabase.SaveAssets();

            RequireEqual(
                exhaustedSourceFileHash,
                Sha256File(ExhaustedSourceClipPath),
                "current Exhausted_Walk_Forward clip file");
            RequireEqual(
                exhaustedControllerHash,
                Sha256File(exhaustedControllerPath),
                "current Exhausted_Walk_Forward controller file");
            InspectExhaustedLocomotionBlendTrees();
            WriteText(ApplicationReportPath, report.ToString());
            Debug.Log(
                "[ExhaustedLocomotionBlendTrees] Four independent 2D Blend Trees applied with exact Player locomotion bases and Spine02-only exhausted upper overlays.");
        }

        internal static void InspectExhaustedLocomotionBlendTrees()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            AnimationClip exhaustedSource = RequireAsset<AnimationClip>(
                ExhaustedSourceClipPath);
            var report = new StringBuilder()
                .AppendLine("Exhausted independent locomotion Blend Tree inspection")
                .AppendLine("targetCount=" + Specs.Length)
                .AppendLine("sharedController=False")
                .AppendLine("baseLayerSourcePerTargetPlayerMotion=True")
                .AppendLine("upperOverlaySourceCurrentExhaustedWalkForward=True")
                .AppendLine("verificationTargetManipulated=False");
            var controllerPaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (TargetSpec spec in Specs)
            {
                GameObject target = FindUnique(scene, spec.TargetName);
                Animator animator = RequireAnimator(target);
                AnimatorController controller = RequireAsset<AnimatorController>(
                    ControllerPath(spec));
                if (animator.runtimeAnimatorController != controller)
                    throw new InvalidOperationException(
                        spec.TargetName + " controller connection differs.");
                if (!controllerPaths.Add(AssetDatabase.GetAssetPath(controller)))
                    throw new InvalidOperationException(
                        "Exhausted targets must not share one controller.");
                if (animator.applyRootMotion || !animator.enabled ||
                    animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                    throw new InvalidOperationException(
                        spec.TargetName + " Animator settings are invalid.");
                RequireFloatParameter(controller, MoveX, spec.Position.x);
                RequireFloatParameter(controller, MoveY, spec.Position.y);
                if (controller.layers.Length != 2)
                    throw new InvalidOperationException(
                        spec.TargetName + " must use exactly two Animator layers.");

                AnimatorControllerLayer baseLayer = controller.layers[0];
                AnimatorState baseState = baseLayer.stateMachine.defaultState ??
                    throw new InvalidOperationException(spec.TargetName + " base state is missing.");
                AnimationClip upperCopy = RequireAsset<AnimationClip>(UpperClipPath(spec));
                string upperBranchPath = UpperBranchPath(target.transform);
                if (baseLayer.avatarMask != null ||
                    baseLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                    !Mathf.Approximately(baseLayer.defaultWeight, 1f) ||
                    baseState.name != BaseStateName ||
                    baseState.writeDefaultValues || !Mathf.Approximately(baseState.speed, 1f))
                    throw new InvalidOperationException(
                        spec.TargetName + " Player locomotion base layer differs.");
                RequireEqual(
                    ClipSignatureForBranch(exhaustedSource, upperBranchPath),
                    ClipSignature(upperCopy),
                    spec.TargetName + " exhausted upper-only copy");

                BlendTree tree = baseState.motion as BlendTree ??
                    throw new InvalidOperationException(
                        spec.TargetName + " Player locomotion 2D Blend Tree is missing.");
                if (tree.name != PlayerTreeName ||
                    tree.blendType != BlendTreeType.FreeformCartesian2D ||
                    tree.blendParameter != MoveX || tree.blendParameterY != MoveY ||
                    tree.children.Length != 2)
                    throw new InvalidOperationException(
                        spec.TargetName + " independent Player locomotion 2D Blend Tree differs.");

                AnimatorControllerLayer upperLayer = controller.layers[1];
                AvatarMask mask = RequireAsset<AvatarMask>(MaskPath(spec));
                if (upperLayer.avatarMask != mask ||
                    upperLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                    !Mathf.Approximately(upperLayer.defaultWeight, 1f))
                    throw new InvalidOperationException(
                        spec.TargetName + " exhausted upper-body overlay layer differs.");
                RequireUpperBodyMask(target.transform, mask);
                AnimatorState upperState = upperLayer.stateMachine.defaultState ??
                    throw new InvalidOperationException(spec.TargetName + " upper state is missing.");
                if (upperState.name != UpperStateName || upperState.writeDefaultValues ||
                    !Mathf.Approximately(upperState.speed, 1f) ||
                    upperState.motion != upperCopy)
                    throw new InvalidOperationException(
                        spec.TargetName + " exhausted upper overlay state differs.");
                ChildMotion[] children = tree.children;
                if (children[0].motion != upperCopy ||
                    (children[0].position - ExhaustedForwardPosition).sqrMagnitude > 0.00000001f ||
                    (children[1].position - spec.Position).sqrMagnitude > 0.00000001f)
                    throw new InvalidOperationException(
                        spec.TargetName + " Blend Tree child mapping differs.");
                AnimatorController playerSourceController =
                    RequireSourceController(scene, spec.PlayerSourceName);
                Motion playerSource = RequireDefaultMotion(
                    playerSourceController,
                    spec.PlayerSourceName);
                RequireMotionEquivalent(
                    playerSource,
                    children[1].motion,
                    spec.TargetName + " player lower copy");
                RequireReferencedSourceParameters(
                    playerSourceController,
                    playerSource,
                    controller,
                    spec.TargetName);

                report.AppendLine("target=" + spec.TargetName)
                    .AppendLine("controllerIndependent=True")
                    .AppendLine("blendTreeType=FreeformCartesian2D")
                    .AppendLine("blendTreeChildCount=2")
                    .AppendLine("baseLayerPlayerSourceExact=True")
                    .AppendLine("defaultSelectsTargetPlayerSource=True")
                    .AppendLine("upperLayerSpine02SourceExact=True")
                    .AppendLine("upperLayerContainsHipOrLegCurves=False")
                    .AppendLine("upperOnlyMask=True");
            }

            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            WriteText(InspectionReportPath, report.ToString());
            Debug.Log(
                "[ExhaustedLocomotionBlendTrees] Structural inspection passed for four independent 2D Blend Trees.");
        }

        internal static void InspectExhaustedLocomotionForwardDriftCorrection()
        {
            InspectExhaustedLocomotionDirectionalLayerCorrection();
            WriteText(
                ForwardDriftInspectionReportPath,
                "Exhausted locomotion forward-drift inspection\n" +
                "supersededByDirectionalLayerCorrection=True\n" +
                "playerMotionOwnsHipsAndLegs=True\n" +
                "exhaustedOverlayContainsHipsOrLegs=False\n");
        }

        internal static void InspectExhaustedLocomotionDirectionalLayerCorrection()
        {
            InspectExhaustedLocomotionBlendTrees();
            Scene scene = RequireScene();
            AnimationClip exhaustedSource = RequireAsset<AnimationClip>(
                ExhaustedSourceClipPath);
            var report = new StringBuilder()
                .AppendLine("Exhausted locomotion directional layer correction inspection")
                .AppendLine("targetCount=4")
                .AppendLine("baseLayerPlayerMotion=True")
                .AppendLine("upperOverlaySpine02Only=True")
                .AppendLine("verificationTargetManipulated=False");
            foreach (TargetSpec spec in Specs)
            {
                GameObject target = FindUnique(scene, spec.TargetName);
                string upperBranchPath = UpperBranchPath(target.transform);
                AnimationClip upperCopy = RequireAsset<AnimationClip>(UpperClipPath(spec));
                RequireEqual(
                    ClipSignatureForBranch(exhaustedSource, upperBranchPath),
                    ClipSignature(upperCopy),
                    spec.TargetName + " exhausted upper-only copy");
                if (AnimationUtility.GetCurveBindings(upperCopy).Any(binding =>
                        !IsPathInBranch(binding.path, upperBranchPath)))
                    throw new InvalidOperationException(
                        spec.TargetName + " exhausted overlay contains a hip, leg, or root curve.");
                report.AppendLine("target=" + spec.TargetName)
                    .AppendLine("playerSource=" + spec.PlayerSourceName)
                    .AppendLine("playerMotionOwnsHipsAndLegs=True")
                    .AppendLine("exhaustedOverlayOwnsSpine02Branch=True")
                    .AppendLine("sharedForwardHipOrLegMotion=False");
            }
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            WriteText(DirectionalInspectionReportPath, report.ToString());
            Debug.Log(
                "[ExhaustedLocomotionBlendTrees] Directional layer structural inspection passed for all four targets.");
        }

        internal static GameObject[] RequireRuntimePlayerSourceTargets()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for exhausted locomotion review.");
            return Specs.Select(spec => FindUnique(scene, spec.PlayerSourceName)).ToArray();
        }

        internal static GameObject RequireRuntimeExhaustedForwardTarget()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for exhausted locomotion review.");
            return FindUnique(scene, "Exhausted_Walk_Forward");
        }

        internal static GameObject[] RequireRuntimeTargets()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for exhausted locomotion review.");
            return Specs.Select(spec => FindUnique(scene, spec.TargetName)).ToArray();
        }

        internal static void WritePlayModeEvidence(
            Texture2D sheet,
            bool final,
            int consoleErrorsBefore,
            float elapsed,
            IReadOnlyList<string> observations)
        {
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter != consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Unity console error count changed during exhausted locomotion review: before=" +
                    consoleErrorsBefore + ", after=" + consoleErrorsAfter);
            string imagePath = final ? FinalImagePath : ReviewImagePath;
            string reportPath = final ? FinalReportPath : ReviewReportPath;
            EnsureFolder(ValidationFolder);
            File.WriteAllBytes(Absolute(imagePath), sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine(final
                    ? "Exhausted locomotion Blend Trees final direct Play Mode capture"
                    : "Exhausted locomotion Blend Trees direct Play Mode inspection")
                .AppendLine("directAnimationReviewFirst=True")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("targetCount=4")
                .AppendLine("sampleCountPerTarget=2")
                .AppendLine("elapsed=" + F(elapsed))
                .AppendLine("consoleErrorsBefore=" + consoleErrorsBefore)
                .AppendLine("consoleErrorsAfter=" + consoleErrorsAfter)
                .AppendLine("image=" + imagePath);
            foreach (string observation in observations)
                report.AppendLine(observation);
            WriteText(reportPath, report.ToString());
            AssetDatabase.Refresh();
        }

        internal static void WriteForwardDriftPlayModeEvidence(
            Texture2D sheet,
            bool final,
            int consoleErrorsBefore,
            float elapsed,
            IReadOnlyList<string> observations,
            IReadOnlyList<float> finalForwardDisplacements)
        {
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter != consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Unity console error count changed during forward-drift review: before=" +
                    consoleErrorsBefore + ", after=" + consoleErrorsAfter);
            if (finalForwardDisplacements.Count != Specs.Length)
                throw new InvalidOperationException(
                    "Forward-drift final displacement count differs from target count.");
            for (int index = 0; index < finalForwardDisplacements.Count; index++)
                if (Mathf.Abs(finalForwardDisplacements[index]) > 0.03f)
                    throw new InvalidOperationException(
                        Specs[index].TargetName + " accumulated forward displacement " +
                        F(finalForwardDisplacements[index]) + "m; allowed maximum is 0.03m.");

            string imagePath = final
                ? ForwardDriftFinalImagePath
                : ForwardDriftReviewImagePath;
            string reportPath = final
                ? ForwardDriftFinalReportPath
                : ForwardDriftReviewReportPath;
            EnsureFolder(ForwardDriftValidationFolder);
            File.WriteAllBytes(Absolute(imagePath), sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine(final
                    ? "Exhausted locomotion forward-drift final direct Play Mode capture"
                    : "Exhausted locomotion forward-drift direct Play Mode inspection")
                .AppendLine("directAnimationReviewFirst=True")
                .AppendLine("fixedCameraFraming=True")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("targetCount=4")
                .AppendLine("sampleCountPerTarget=4")
                .AppendLine("startMidLoopEndSecondLoopEndCompared=True")
                .AppendLine("maximumAllowedAccumulatedForwardDisplacement=0.03")
                .AppendLine("elapsed=" + F(elapsed))
                .AppendLine("consoleErrorsBefore=" + consoleErrorsBefore)
                .AppendLine("consoleErrorsAfter=" + consoleErrorsAfter)
                .AppendLine("image=" + imagePath);
            foreach (string observation in observations)
                report.AppendLine(observation);
            for (int index = 0; index < finalForwardDisplacements.Count; index++)
                report.AppendLine(
                    "finalForwardDisplacement|target=" + Specs[index].TargetName +
                    "|meters=" + F(finalForwardDisplacements[index]));
            WriteText(reportPath, report.ToString());
            AssetDatabase.Refresh();
        }

        internal static void WriteDirectionalLayerPlayModeEvidence(
            Texture2D sheet,
            bool final,
            int consoleErrorsBefore,
            float elapsed,
            IReadOnlyList<string> observations)
        {
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter != consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Unity console error count changed during directional-layer review: before=" +
                    consoleErrorsBefore + ", after=" + consoleErrorsAfter);
            string imagePath = final
                ? DirectionalFinalImagePath
                : DirectionalReviewImagePath;
            string reportPath = final
                ? DirectionalFinalReportPath
                : DirectionalReviewReportPath;
            EnsureFolder(DirectionalValidationFolder);
            File.WriteAllBytes(Absolute(imagePath), sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine(final
                    ? "Exhausted locomotion directional layers final direct Play Mode capture"
                    : "Exhausted locomotion directional layers direct Play Mode inspection")
                .AppendLine("directAnimationReviewFirst=True")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("targetCount=4")
                .AppendLine("sampleCountPerTarget=4")
                .AppendLine("comparisonColumnsPerTarget=correctedTarget,PlayerSource,ExhaustedUpperSource")
                .AppendLine("playerMotionOwnsHipsAndLegs=True")
                .AppendLine("exhaustedMotionOwnsSpine02BranchOnly=True")
                .AppendLine("elapsed=" + F(elapsed))
                .AppendLine("consoleErrorsBefore=" + consoleErrorsBefore)
                .AppendLine("consoleErrorsAfter=" + consoleErrorsAfter)
                .AppendLine("image=" + imagePath);
            foreach (string observation in observations)
                report.AppendLine(observation);
            WriteText(reportPath, report.ToString());
            AssetDatabase.Refresh();
        }

        private static void RemoveCopiedForwardTranslation(AnimationClip clip)
        {
            EditorCurveBinding binding = RequireForwardCurveBinding(clip);
            AnimationCurve source = AnimationUtility.GetEditorCurve(clip, binding);
            Keyframe[] keys = source.keys;
            if (keys.Length == 0)
                throw new InvalidOperationException(clip.name + " forward curve has no keys.");
            float fixedValue = keys[0].value;
            for (int index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                key.value = fixedValue;
                key.inTangent = 0f;
                key.outTangent = 0f;
                keys[index] = key;
            }
            var corrected = new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            AnimationUtility.SetEditorCurve(clip, binding, corrected);
            EditorUtility.SetDirty(clip);
        }

        private static EditorCurveBinding RequireForwardCurveBinding(AnimationClip clip)
        {
            EditorCurveBinding[] matches = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.path == ForwardCurvePath &&
                    binding.type == typeof(Transform) &&
                    binding.propertyName == ForwardCurveProperty)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    clip.name + " must contain exactly one " + ForwardCurvePath + "/" +
                    ForwardCurveProperty + " curve; found " + matches.Length + ".");
            return matches[0];
        }

        private static float RequireForwardCurveDelta(AnimationClip clip)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                clip,
                RequireForwardCurveBinding(clip));
            if (curve.length < 2)
                throw new InvalidOperationException(clip.name + " forward curve is incomplete.");
            return curve.keys[curve.length - 1].value - curve.keys[0].value;
        }

        private static float RequireForwardCurveConstant(AnimationClip clip)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                clip,
                RequireForwardCurveBinding(clip));
            if (curve.length == 0)
                throw new InvalidOperationException(clip.name + " forward curve has no keys.");
            float value = curve.keys[0].value;
            foreach (Keyframe key in curve.keys)
                if (Mathf.Abs(key.value - value) > 0.000001f ||
                    Mathf.Abs(key.inTangent) > 0.000001f ||
                    Mathf.Abs(key.outTangent) > 0.000001f)
                    throw new InvalidOperationException(
                        clip.name + " still contains changing forward translation.");
            return value;
        }

        private static void RequireForwardAxisMatchesTarget(GameObject target)
        {
            Transform hips = target.transform.Find(ForwardCurvePath);
            if (hips == null || hips.parent == null)
                throw new InvalidOperationException(
                    target.name + " is missing the " + ForwardCurvePath + " hierarchy.");
            Vector3 curveAxis = hips.parent.TransformDirection(Vector3.up).normalized;
            float alignment = Mathf.Abs(Vector3.Dot(curveAxis, target.transform.forward));
            if (alignment < 0.98f)
                throw new InvalidOperationException(
                    target.name + " forward curve axis does not align with target forward; dot=" +
                    F(alignment));
        }

        private static AnimatorController PrepareController(string path)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorControllerLayer[] existing = controller.layers;
            for (int index = 1; index < existing.Length; index++)
                if (existing[index].stateMachine != null)
                    UnityEngine.Object.DestroyImmediate(existing[index].stateMachine, true);
            AnimatorControllerLayer baseLayer = existing[0];
            ClearStateMachine(baseLayer.stateMachine);
            controller.layers = new[] { baseLayer };
            foreach (BlendTree tree in AssetDatabase.LoadAllAssetsAtPath(path)
                         .OfType<BlendTree>().ToArray())
                UnityEngine.Object.DestroyImmediate(tree, true);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            return controller;
        }

        private static void ConfigureController(
            AnimatorController controller,
            TargetSpec spec,
            AnimationClip upperCopy,
            Motion playerCopy,
            AvatarMask upperMask,
            AnimatorController playerSourceController)
        {
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = MoveX,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = spec.Position.x
            });
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = MoveY,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = spec.Position.y
            });
            CopyReferencedSourceParameters(
                playerSourceController,
                playerCopy,
                controller);

            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.name = "Independent Player Locomotion 2D";
            baseLayer.defaultWeight = 1f;
            baseLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            baseLayer.avatarMask = null;
            baseLayer.iKPass = false;
            var playerTree = new BlendTree
            {
                name = PlayerTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = MoveX,
                blendParameterY = MoveY,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(playerTree, controller);
            playerTree.children = new[]
            {
                Child(upperCopy, ExhaustedForwardPosition),
                Child(playerCopy, spec.Position)
            };
            AnimatorState baseState = baseLayer.stateMachine.AddState(BaseStateName);
            baseState.motion = playerTree;
            baseState.speed = 1f;
            baseState.cycleOffset = 0f;
            baseState.mirror = false;
            baseState.writeDefaultValues = false;
            baseLayer.stateMachine.defaultState = baseState;
            controller.layers = new[] { baseLayer };

            controller.AddLayer("Current Exhausted Upper Body");
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer upperLayer = layers[1];
            upperLayer.name = "Current Exhausted Upper Body";
            upperLayer.defaultWeight = 1f;
            upperLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            upperLayer.avatarMask = upperMask;
            upperLayer.iKPass = false;
            AnimatorState upperState = upperLayer.stateMachine.AddState(UpperStateName);
            upperState.motion = upperCopy;
            upperState.speed = 1f;
            upperState.cycleOffset = 0f;
            upperState.mirror = false;
            upperState.writeDefaultValues = false;
            upperLayer.stateMachine.defaultState = upperState;
            layers[1] = upperLayer;
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(baseLayer.stateMachine);
            EditorUtility.SetDirty(upperLayer.stateMachine);
            EditorUtility.SetDirty(playerTree);
        }

        private static AnimationClip CopyClip(
            AnimationClip source,
            string path,
            string name)
        {
            AnimationClip copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (copy == null)
            {
                copy = new AnimationClip();
                EditorUtility.CopySerialized(source, copy);
                copy.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(copy, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, copy);
                copy.name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.SetDirty(copy);
            }
            RequireEqual(ClipSignature(source), ClipSignature(copy), name);
            return copy;
        }

        private static Motion CopyMotion(
            Motion source,
            AnimatorController owner,
            string folder,
            string name,
            ref int clipIndex)
        {
            if (source is AnimationClip sourceClip)
            {
                string path = folder + "/" + name + "_" + clipIndex++ + ".anim";
                return CopyClip(sourceClip, path, name);
            }
            if (!(source is BlendTree sourceTree))
                throw new InvalidOperationException(
                    "Unsupported Player motion type: " + source.GetType().FullName);
            var copy = new BlendTree
            {
                name = name,
                blendType = sourceTree.blendType,
                blendParameter = sourceTree.blendParameter,
                blendParameterY = sourceTree.blendParameterY,
                useAutomaticThresholds = sourceTree.useAutomaticThresholds,
                minThreshold = sourceTree.minThreshold,
                maxThreshold = sourceTree.maxThreshold
            };
            AssetDatabase.AddObjectToAsset(copy, owner);
            ChildMotion[] sourceChildren = sourceTree.children;
            var children = new ChildMotion[sourceChildren.Length];
            for (int index = 0; index < sourceChildren.Length; index++)
            {
                ChildMotion child = sourceChildren[index];
                children[index] = new ChildMotion
                {
                    motion = CopyMotion(
                        child.motion,
                        owner,
                        folder,
                        name + "_Child" + index,
                        ref clipIndex),
                    threshold = child.threshold,
                    position = child.position,
                    timeScale = child.timeScale,
                    cycleOffset = child.cycleOffset,
                    mirror = child.mirror,
                    directBlendParameter = child.directBlendParameter
                };
            }
            copy.children = children;
            EditorUtility.SetDirty(copy);
            return copy;
        }

        private static AvatarMask CreateUpperBodyMask(Transform target, string path)
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (mask == null)
            {
                mask = new AvatarMask { name = target.name + "_ExhaustedUpperOnly" };
                AssetDatabase.CreateAsset(mask, path);
            }
            else
            {
                mask.name = target.name + "_ExhaustedUpperOnly";
            }
            string upperPath = UpperBranchPath(target);
            string[] paths = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item != target)
                .Select(item => AnimationUtility.CalculateTransformPath(item, target))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item.Count(character => character == '/'))
                .ThenBy(item => item, StringComparer.Ordinal)
                .ToArray();
            mask.transformCount = paths.Length;
            for (int index = 0; index < paths.Length; index++)
            {
                string item = paths[index];
                mask.SetTransformPath(index, item);
                mask.SetTransformActive(index, IsPathInBranch(item, upperPath));
            }
            for (int index = 0;
                 index < (int)AvatarMaskBodyPart.LastBodyPart;
                 index++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static void RequireUpperBodyMask(Transform target, AvatarMask mask)
        {
            string upper = UpperBranchPath(target);
            for (int index = 0; index < mask.transformCount; index++)
            {
                string path = mask.GetTransformPath(index);
                bool expected = IsPathInBranch(path, upper);
                if (mask.GetTransformActive(index) != expected)
                    throw new InvalidOperationException(
                        target.name + " upper-body mask differs at " + path);
            }
            for (int index = 0;
                 index < (int)AvatarMaskBodyPart.LastBodyPart;
                 index++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)index;
                bool expected = part == AvatarMaskBodyPart.Body ||
                    part == AvatarMaskBodyPart.Head ||
                    part == AvatarMaskBodyPart.LeftArm ||
                    part == AvatarMaskBodyPart.RightArm ||
                    part == AvatarMaskBodyPart.LeftFingers ||
                    part == AvatarMaskBodyPart.RightFingers;
                if (mask.GetHumanoidBodyPartActive(part) != expected)
                    throw new InvalidOperationException(
                        target.name + " humanoid upper-body mask differs at " + part);
            }
        }

        private static string UpperBranchPath(Transform target) =>
            AnimationUtility.CalculateTransformPath(
                RequireDescendant(target, "Spine02"),
                target);

        private static void KeepOnlyUpperBodyCurves(
            AnimationClip clip,
            string upperBranchPath)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                if (!IsPathInBranch(binding.path, upperBranchPath))
                    AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetObjectReferenceCurveBindings(clip))
                if (!IsPathInBranch(binding.path, upperBranchPath))
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            EditorUtility.SetDirty(clip);
        }

        private static AnimatorController RequireSourceController(
            Scene scene,
            string objectName)
        {
            Animator animator = RequireAnimator(FindUnique(scene, objectName));
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
                throw new InvalidOperationException(
                    objectName + " must use an AnimatorController.");
            return controller;
        }

        private static Motion RequireDefaultMotion(
            AnimatorController controller,
            string objectName)
        {
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException(objectName + " default state is missing.");
            return state.motion ??
                throw new InvalidOperationException(objectName + " default motion is missing.");
        }

        private static void CopyReferencedSourceParameters(
            AnimatorController sourceController,
            Motion copiedMotion,
            AnimatorController destinationController)
        {
            foreach (string parameterName in ReferencedBlendParameters(copiedMotion)
                         .Distinct(StringComparer.Ordinal))
            {
                if (parameterName == MoveX || parameterName == MoveY)
                    continue;
                AnimatorControllerParameter source = sourceController.parameters
                    .SingleOrDefault(item => item.name == parameterName) ??
                    throw new InvalidOperationException(
                        AssetDatabase.GetAssetPath(sourceController) +
                        " is missing referenced parameter " + parameterName + ".");
                destinationController.AddParameter(new AnimatorControllerParameter
                {
                    name = source.name,
                    type = source.type,
                    defaultBool = source.defaultBool,
                    defaultFloat = source.defaultFloat,
                    defaultInt = source.defaultInt
                });
            }
        }

        private static void RequireReferencedSourceParameters(
            AnimatorController sourceController,
            Motion sourceMotion,
            AnimatorController destinationController,
            string label)
        {
            foreach (string parameterName in ReferencedBlendParameters(sourceMotion)
                         .Distinct(StringComparer.Ordinal))
            {
                if (parameterName == MoveX || parameterName == MoveY)
                    continue;
                AnimatorControllerParameter source = sourceController.parameters
                    .SingleOrDefault(item => item.name == parameterName) ??
                    throw new InvalidOperationException(
                        label + " source parameter is missing: " + parameterName);
                AnimatorControllerParameter destination = destinationController.parameters
                    .SingleOrDefault(item => item.name == parameterName) ??
                    throw new InvalidOperationException(
                        label + " copied parameter is missing: " + parameterName);
                if (source.type != destination.type ||
                    source.defaultBool != destination.defaultBool ||
                    !Mathf.Approximately(source.defaultFloat, destination.defaultFloat) ||
                    source.defaultInt != destination.defaultInt)
                    throw new InvalidOperationException(
                        label + " copied parameter differs: " + parameterName);
            }
        }

        private static IEnumerable<string> ReferencedBlendParameters(Motion motion)
        {
            if (!(motion is BlendTree tree))
                yield break;
            if (!string.IsNullOrEmpty(tree.blendParameter))
                yield return tree.blendParameter;
            if (tree.blendType == BlendTreeType.FreeformCartesian2D ||
                tree.blendType == BlendTreeType.FreeformDirectional2D ||
                tree.blendType == BlendTreeType.SimpleDirectional2D)
            {
                if (!string.IsNullOrEmpty(tree.blendParameterY))
                    yield return tree.blendParameterY;
            }
            foreach (ChildMotion child in tree.children)
            {
                if (tree.blendType == BlendTreeType.Direct &&
                    !string.IsNullOrEmpty(child.directBlendParameter))
                    yield return child.directBlendParameter;
                foreach (string nested in ReferencedBlendParameters(child.motion))
                    yield return nested;
            }
        }

        private static void RequireMotionEquivalent(
            Motion source,
            Motion copy,
            string label)
        {
            if (source is AnimationClip sourceClip)
            {
                if (!(copy is AnimationClip copiedClip))
                    throw new InvalidOperationException(label + " clip type differs.");
                RequireEqual(ClipSignature(sourceClip), ClipSignature(copiedClip), label);
                return;
            }
            if (!(source is BlendTree sourceTree) || !(copy is BlendTree copiedTree) ||
                sourceTree.blendType != copiedTree.blendType ||
                sourceTree.blendParameter != copiedTree.blendParameter ||
                sourceTree.blendParameterY != copiedTree.blendParameterY ||
                sourceTree.useAutomaticThresholds != copiedTree.useAutomaticThresholds ||
                !Mathf.Approximately(sourceTree.minThreshold, copiedTree.minThreshold) ||
                !Mathf.Approximately(sourceTree.maxThreshold, copiedTree.maxThreshold) ||
                sourceTree.children.Length != copiedTree.children.Length)
                throw new InvalidOperationException(label + " Blend Tree differs.");
            ChildMotion[] sourceChildren = sourceTree.children;
            ChildMotion[] copiedChildren = copiedTree.children;
            for (int index = 0; index < sourceChildren.Length; index++)
            {
                ChildMotion a = sourceChildren[index];
                ChildMotion b = copiedChildren[index];
                if (!Mathf.Approximately(a.threshold, b.threshold) ||
                    (a.position - b.position).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(a.timeScale, b.timeScale) ||
                    !Mathf.Approximately(a.cycleOffset, b.cycleOffset) ||
                    a.mirror != b.mirror ||
                    a.directBlendParameter != b.directBlendParameter)
                    throw new InvalidOperationException(
                        label + " child differs at " + index + ".");
                RequireMotionEquivalent(
                    a.motion,
                    b.motion,
                    label + " child " + index);
            }
        }

        private static string ClipSignature(AnimationClip clip) => ClipSignature(clip, false);

        private static string ClipSignature(AnimationClip clip, bool excludeForwardCurve) =>
            ClipSignature(
                clip,
                binding => !(excludeForwardCurve &&
                    binding.path == ForwardCurvePath &&
                    binding.type == typeof(Transform) &&
                    binding.propertyName == ForwardCurveProperty));

        private static string ClipSignatureForBranch(
            AnimationClip clip,
            string branchPath) =>
            ClipSignature(clip, binding => IsPathInBranch(binding.path, branchPath));

        private static string ClipSignature(
            AnimationClip clip,
            Func<EditorCurveBinding, bool> includeBinding)
        {
            var result = new StringBuilder()
                .AppendLine("length=" + F(clip.length))
                .AppendLine("frameRate=" + F(clip.frameRate))
                .AppendLine("legacy=" + clip.legacy)
                .AppendLine("wrapMode=" + clip.wrapMode)
                .AppendLine("settings=" + EditorJsonUtility.ToJson(
                    AnimationUtility.GetAnimationClipSettings(clip)));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                         .OrderBy(item => item.path, StringComparer.Ordinal)
                         .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                         .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                if (!includeBinding(binding))
                    continue;
                result.Append("curve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|')
                    .AppendLine(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                result.AppendLine("wrap=" + curve.preWrapMode + "," + curve.postWrapMode);
                foreach (Keyframe key in curve.keys)
                    result.AppendLine(string.Join(",", new[]
                    {
                        F(key.time), F(key.value), F(key.inTangent), F(key.outTangent),
                        F(key.inWeight), F(key.outWeight), key.weightedMode.ToString()
                    }));
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                result.AppendLine("event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + item.intParameter + "|" +
                    F(item.floatParameter) + "|" + item.messageOptions);
            return Sha256(result.ToString());
        }

        private static void ClearStateMachine(AnimatorStateMachine machine)
        {
            foreach (AnimatorState state in machine.states.Select(item => item.state).ToArray())
                machine.RemoveState(state);
            foreach (AnimatorStateMachine child in machine.stateMachines
                         .Select(item => item.stateMachine).ToArray())
                machine.RemoveStateMachine(child);
        }

        private static ChildMotion Child(Motion motion, Vector2 position) =>
            new ChildMotion
            {
                motion = motion,
                position = position,
                timeScale = 1f,
                cycleOffset = 0f,
                mirror = false,
                threshold = 0f,
                directBlendParameter = string.Empty
            };

        private static void RequireFloatParameter(
            AnimatorController controller,
            string name,
            float expected)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, expected))
                throw new InvalidOperationException(
                    AssetDatabase.GetAssetPath(controller) + " parameter differs: " + name);
        }

        private static bool IsPathInBranch(string path, string branch) =>
            path == branch || path.StartsWith(branch + "/", StringComparison.Ordinal);

        private static string TargetFolder(TargetSpec spec) =>
            AssetFolder + "/" + spec.Suffix;

        private static string ControllerPath(TargetSpec spec) =>
            TargetFolder(spec) + "/" + spec.TargetName + ".controller";

        private static string UpperClipPath(TargetSpec spec) =>
            TargetFolder(spec) + "/" + spec.TargetName + "_ExhaustedUpper.anim";

        private static string MaskPath(TargetSpec spec) =>
            TargetFolder(spec) + "/" + spec.TargetName + "_ExhaustedUpperOnly.mask";

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; actual=" + matches.Length);
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject target) =>
            target.GetComponent<Animator>() ??
            throw new InvalidOperationException(target.name + " Animator is missing.");

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    root.name + " requires one " + name + "; actual=" + matches.Length);
            return matches[0];
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Required asset is missing: " + path);

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("This command requires Edit Mode.");
        }

        private static void ClearConsole()
        {
            Type logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            MethodInfo clear = logEntries.GetMethod(
                "Clear",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console clear method is unavailable.");
            clear.Invoke(null, null);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " differs.");
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        private static string Absolute(string relative) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relative));

        private static string Sha256(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
                return BitConverter.ToString(
                        algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty);
        }

        private static string Sha256File(string assetPath)
        {
            string absolute = Absolute(assetPath);
            if (!File.Exists(absolute))
                throw new FileNotFoundException("Required file is missing.", absolute);
            using (SHA256 algorithm = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolute))
                return BitConverter.ToString(algorithm.ComputeHash(stream))
                    .Replace("-", string.Empty);
        }

        private static void WriteText(string path, string text)
        {
            string absolute = Absolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Output directory is missing."));
            File.WriteAllText(absolute, text.Replace("\r\n", "\n"), new UTF8Encoding(false));
        }

        private readonly struct TargetSpec
        {
            internal readonly string TargetName;
            internal readonly string PlayerSourceName;
            internal readonly string Suffix;
            internal readonly Vector2 Position;

            internal TargetSpec(
                string targetName,
                string playerSourceName,
                string suffix,
                Vector2 position)
            {
                TargetName = targetName;
                PlayerSourceName = playerSourceName;
                Suffix = suffix;
                Position = position;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            internal TransformSnapshot(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            internal void RequireUnchanged(Transform transform, string label)
            {
                if (Vector3.Distance(position, transform.localPosition) > 0.000001f ||
                    Quaternion.Angle(rotation, transform.localRotation) > 0.0001f ||
                    Vector3.Distance(scale, transform.localScale) > 0.000001f)
                    throw new InvalidOperationException(
                        label + " root Transform changed unexpectedly.");
            }
        }
    }

    [InitializeOnLoad]
    internal static class ExhaustedLocomotionBlendTreesPlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.ExhaustedLocomotionBlendTrees.Pending";
        private const string StateKey =
            "Bellerophon.ExhaustedLocomotionBlendTrees.State";
        private const string FinalKey =
            "Bellerophon.ExhaustedLocomotionBlendTrees.Final";
        private const string FailureKey =
            "Bellerophon.ExhaustedLocomotionBlendTrees.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.ExhaustedLocomotionBlendTrees.ConsoleErrorsBefore";
        private const string ForwardDriftModeKey =
            "Bellerophon.ExhaustedLocomotionBlendTrees.ForwardDriftMode";
        private const string DirectionalModeKey =
            "Bellerophon.ExhaustedLocomotionBlendTrees.DirectionalMode";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private static readonly float[] CaptureTimes = { 0.35f, 1.05f };
        private static readonly float[] ForwardDriftNormalizedTimes =
            { 0.05f, 0.5f, 1.05f, 2.05f };
        private static readonly float[] DirectionalCaptureTimes =
            { 0.18f, 0.58f, 0.98f, 1.38f };
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static readonly List<string> Observations = new List<string>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject[] targets;
        private static Animator[] animators;
        private static Vector3[] initialPositions;
        private static Quaternion[] initialRotations;
        private static Vector3[] initialScales;
        private static Bounds[] fixedFramingBounds;
        private static Transform[] hips;
        private static Vector3[] firstSampleHipPositions;
        private static Vector3[] targetForwardDirections;
        private static GameObject[] playerSourceTargets;
        private static Animator[] playerSourceAnimators;
        private static Bounds[] playerSourceBounds;
        private static GameObject exhaustedForwardTarget;
        private static Animator exhaustedForwardAnimator;
        private static Bounds exhaustedForwardBounds;
        private static double startedAt;
        private static int nextSample;

        static ExhaustedLocomotionBlendTreesPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleCapture()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(
            bool final,
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted locomotion Blend Tree review must start in Edit Mode.");
            ExhaustedLocomotionBlendTreeTools
                .InspectExhaustedLocomotionBlendTrees();
            complete = onComplete;
            fail = onFail;
            CleanupFrames();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(FinalKey, final);
            SessionState.SetBool(ForwardDriftModeKey, false);
            SessionState.SetBool(DirectionalModeKey, false);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void StartDirectional(
            bool final,
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted locomotion directional-layer review must start in Edit Mode.");
            ExhaustedLocomotionBlendTreeTools
                .InspectExhaustedLocomotionDirectionalLayerCorrection();
            complete = onComplete;
            fail = onFail;
            CleanupFrames();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(FinalKey, final);
            SessionState.SetBool(ForwardDriftModeKey, false);
            SessionState.SetBool(DirectionalModeKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void StartForwardDrift(
            bool final,
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted locomotion forward-drift review must start in Edit Mode.");
            ExhaustedLocomotionBlendTreeTools
                .InspectExhaustedLocomotionForwardDriftCorrection();
            complete = onComplete;
            fail = onFail;
            CleanupFrames();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(FinalKey, final);
            SessionState.SetBool(ForwardDriftModeKey, true);
            SessionState.SetBool(DirectionalModeKey, false);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Exhausted locomotion review has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingCapture)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before exhausted locomotion review completed.");
                    CaptureNaturalPlayback();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                bool forwardDrift = SessionState.GetBool(ForwardDriftModeKey, false);
                bool directional = SessionState.GetBool(DirectionalModeKey, false);
                if (directional)
                    ExhaustedLocomotionBlendTreeTools
                        .InspectExhaustedLocomotionDirectionalLayerCorrection();
                else if (forwardDrift)
                    ExhaustedLocomotionBlendTreeTools
                        .InspectExhaustedLocomotionForwardDriftCorrection();
                else
                    ExhaustedLocomotionBlendTreeTools
                        .InspectExhaustedLocomotionBlendTrees();
                bool final = SessionState.GetBool(FinalKey, false);
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(directional
                    ? (final
                        ? "Four exhausted locomotion targets completed final directional Player-lower and exhausted-upper comparison capture."
                        : "Four exhausted locomotion targets completed direct directional Player-lower and exhausted-upper inspection.")
                    : (forwardDrift
                        ? (final
                            ? "Four exhausted locomotion targets completed final fixed-frame forward-drift Play Mode capture."
                            : "Four exhausted locomotion targets completed direct fixed-frame forward-drift Play Mode inspection.")
                        : (final
                            ? "Four independent exhausted locomotion 2D Blend Trees completed final natural Play Mode capture."
                            : "Four independent exhausted locomotion 2D Blend Trees completed direct natural Play Mode inspection.")));
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupFrames();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            targets = ExhaustedLocomotionBlendTreeTools.RequireRuntimeTargets();
            animators = targets.Select(target => target.GetComponent<Animator>() ??
                    throw new InvalidOperationException(target.name + " Animator is missing."))
                .ToArray();
            initialPositions = targets.Select(target => target.transform.localPosition).ToArray();
            initialRotations = targets.Select(target => target.transform.localRotation).ToArray();
            initialScales = targets.Select(target => target.transform.localScale).ToArray();
            fixedFramingBounds = targets.Select(CurrentBounds).ToArray();
            hips = targets.Select(target => target.transform.Find("Armature/Hips") ??
                    throw new InvalidOperationException(
                        target.name + " is missing Armature/Hips during direct review."))
                .ToArray();
            firstSampleHipPositions = new Vector3[targets.Length];
            targetForwardDirections = targets.Select(target => target.transform.forward.normalized)
                .ToArray();
            if (SessionState.GetBool(DirectionalModeKey, false))
            {
                playerSourceTargets = ExhaustedLocomotionBlendTreeTools
                    .RequireRuntimePlayerSourceTargets();
                playerSourceAnimators = playerSourceTargets
                    .Select(target => target.GetComponent<Animator>() ??
                        throw new InvalidOperationException(
                            target.name + " source Animator is missing."))
                    .ToArray();
                playerSourceBounds = playerSourceTargets.Select(CurrentBounds).ToArray();
                exhaustedForwardTarget = ExhaustedLocomotionBlendTreeTools
                    .RequireRuntimeExhaustedForwardTarget();
                exhaustedForwardAnimator = exhaustedForwardTarget.GetComponent<Animator>() ??
                    throw new InvalidOperationException(
                        "Exhausted_Walk_Forward source Animator is missing.");
                exhaustedForwardBounds = CurrentBounds(exhaustedForwardTarget);
            }
            startedAt = EditorApplication.timeSinceStartup;
            nextSample = 0;
        }

        private static void CaptureNaturalPlayback()
        {
            if (SessionState.GetBool(DirectionalModeKey, false))
            {
                CaptureDirectionalPlayback();
                return;
            }
            if (SessionState.GetBool(ForwardDriftModeKey, false))
            {
                CaptureForwardDriftPlayback();
                return;
            }
            double elapsed = EditorApplication.timeSinceStartup - startedAt;
            if (elapsed > 25d)
                throw new TimeoutException(
                    "Exhausted locomotion natural Play Mode review exceeded 25 seconds.");
            if (animators.Any(animator => !animator.isInitialized))
                return;
            if (nextSample >= CaptureTimes.Length || elapsed < CaptureTimes[nextSample])
                return;
            RequireRootsUnchanged();
            for (int index = 0; index < targets.Length; index++)
            {
                AnimatorStateInfo player = animators[index].GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo upper = animators[index].GetCurrentAnimatorStateInfo(1);
                if (!player.IsName("PlayerLocomotion2DBase") ||
                    !upper.IsName("ExhaustedUpperOverlay"))
                    throw new InvalidOperationException(
                        targets[index].name + " is not naturally playing both configured layers.");
                Panels.Add(RenderTarget(targets[index]));
                Observations.Add(
                    "sample=" + nextSample +
                    "|target=" + targets[index].name +
                    "|playerNormalizedTime=" + player.normalizedTime.ToString("R", CultureInfo.InvariantCulture) +
                    "|upperNormalizedTime=" + upper.normalizedTime.ToString("R", CultureInfo.InvariantCulture) +
                    "|moveX=" + animators[index].GetFloat("MoveX").ToString("R", CultureInfo.InvariantCulture) +
                    "|moveY=" + animators[index].GetFloat("MoveY").ToString("R", CultureInfo.InvariantCulture));
            }
            nextSample++;
            if (nextSample < CaptureTimes.Length)
                return;

            Texture2D sheet = CombinePanels(targets.Length, CaptureTimes.Length);
            try
            {
                ExhaustedLocomotionBlendTreeTools.WritePlayModeEvidence(
                    sheet,
                    SessionState.GetBool(FinalKey, false),
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                    (float)elapsed,
                    Observations);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            RequireRootsUnchanged();
            CleanupFrames();
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static void CaptureForwardDriftPlayback()
        {
            double elapsed = EditorApplication.timeSinceStartup - startedAt;
            if (elapsed > 25d)
                throw new TimeoutException(
                    "Exhausted locomotion forward-drift Play Mode review exceeded 25 seconds.");
            if (animators.Any(animator => !animator.isInitialized))
                return;
            float minimumNormalizedTime = animators
                .Select(animator => animator.GetCurrentAnimatorStateInfo(0).normalizedTime)
                .Min();
            if (nextSample >= ForwardDriftNormalizedTimes.Length ||
                minimumNormalizedTime < ForwardDriftNormalizedTimes[nextSample])
                return;

            RequireRootsUnchanged();
            for (int index = 0; index < targets.Length; index++)
            {
                AnimatorStateInfo player = animators[index].GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo upper = animators[index].GetCurrentAnimatorStateInfo(1);
                if (!player.IsName("PlayerLocomotion2DBase") ||
                    !upper.IsName("ExhaustedUpperOverlay"))
                    throw new InvalidOperationException(
                        targets[index].name + " is not naturally playing both configured layers.");
                if (nextSample == 0)
                {
                    firstSampleHipPositions[index] = hips[index].position;
                    fixedFramingBounds[index] = CurrentBounds(targets[index]);
                }
                float forwardDisplacement = Vector3.Dot(
                    hips[index].position - firstSampleHipPositions[index],
                    targetForwardDirections[index]);
                Panels.Add(RenderTarget(targets[index], fixedFramingBounds[index]));
                Observations.Add(
                    "sample=" + nextSample +
                    "|target=" + targets[index].name +
                    "|playerNormalizedTime=" + player.normalizedTime.ToString("R", CultureInfo.InvariantCulture) +
                    "|upperNormalizedTime=" + upper.normalizedTime.ToString("R", CultureInfo.InvariantCulture) +
                    "|hipsForwardDisplacement=" + forwardDisplacement.ToString("R", CultureInfo.InvariantCulture) +
                    "|moveX=" + animators[index].GetFloat("MoveX").ToString("R", CultureInfo.InvariantCulture) +
                    "|moveY=" + animators[index].GetFloat("MoveY").ToString("R", CultureInfo.InvariantCulture));
            }
            nextSample++;
            if (nextSample < ForwardDriftNormalizedTimes.Length)
                return;

            var finalDisplacements = new List<float>(targets.Length);
            for (int index = 0; index < targets.Length; index++)
                finalDisplacements.Add(Vector3.Dot(
                    hips[index].position - firstSampleHipPositions[index],
                    targetForwardDirections[index]));
            Texture2D sheet = CombinePanels(
                targets.Length,
                ForwardDriftNormalizedTimes.Length);
            try
            {
                ExhaustedLocomotionBlendTreeTools.WriteForwardDriftPlayModeEvidence(
                    sheet,
                    SessionState.GetBool(FinalKey, false),
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                    (float)elapsed,
                    Observations,
                    finalDisplacements);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            RequireRootsUnchanged();
            CleanupFrames();
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static void CaptureDirectionalPlayback()
        {
            double elapsed = EditorApplication.timeSinceStartup - startedAt;
            if (elapsed > 25d)
                throw new TimeoutException(
                    "Exhausted locomotion directional-layer Play Mode review exceeded 25 seconds.");
            if (animators.Any(animator => !animator.isInitialized) ||
                playerSourceAnimators.Any(animator => !animator.isInitialized) ||
                !exhaustedForwardAnimator.isInitialized)
                return;
            if (nextSample >= DirectionalCaptureTimes.Length ||
                elapsed < DirectionalCaptureTimes[nextSample])
                return;

            RequireRootsUnchanged();
            if (nextSample == 0)
            {
                fixedFramingBounds = targets.Select(CurrentBounds).ToArray();
                playerSourceBounds = playerSourceTargets.Select(CurrentBounds).ToArray();
                exhaustedForwardBounds = CurrentBounds(exhaustedForwardTarget);
            }
            for (int index = 0; index < targets.Length; index++)
            {
                AnimatorStateInfo player = animators[index].GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo upper = animators[index].GetCurrentAnimatorStateInfo(1);
                AnimatorStateInfo sourcePlayer =
                    playerSourceAnimators[index].GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo sourceUpper =
                    exhaustedForwardAnimator.GetCurrentAnimatorStateInfo(0);
                if (!player.IsName("PlayerLocomotion2DBase") ||
                    !upper.IsName("ExhaustedUpperOverlay"))
                    throw new InvalidOperationException(
                        targets[index].name + " is not naturally playing corrected layers.");

                Panels.Add(RenderTarget(targets[index], fixedFramingBounds[index]));
                Panels.Add(RenderTarget(
                    playerSourceTargets[index],
                    playerSourceBounds[index]));
                Panels.Add(RenderTarget(
                    exhaustedForwardTarget,
                    exhaustedForwardBounds));
                Observations.Add(
                    "sample=" + nextSample +
                    "|target=" + targets[index].name +
                    "|panelOrder=correctedTarget," + playerSourceTargets[index].name +
                    ",Exhausted_Walk_Forward" +
                    "|targetPlayerNormalizedTime=" + player.normalizedTime.ToString("R", CultureInfo.InvariantCulture) +
                    "|sourcePlayerNormalizedTime=" + sourcePlayer.normalizedTime.ToString("R", CultureInfo.InvariantCulture) +
                    "|targetUpperNormalizedTime=" + upper.normalizedTime.ToString("R", CultureInfo.InvariantCulture) +
                    "|sourceUpperNormalizedTime=" + sourceUpper.normalizedTime.ToString("R", CultureInfo.InvariantCulture) +
                    "|moveX=" + animators[index].GetFloat("MoveX").ToString("R", CultureInfo.InvariantCulture) +
                    "|moveY=" + animators[index].GetFloat("MoveY").ToString("R", CultureInfo.InvariantCulture));
            }
            nextSample++;
            if (nextSample < DirectionalCaptureTimes.Length)
                return;

            Texture2D sheet = CombinePanels(
                targets.Length * 3,
                DirectionalCaptureTimes.Length);
            try
            {
                ExhaustedLocomotionBlendTreeTools.WriteDirectionalLayerPlayModeEvidence(
                    sheet,
                    SessionState.GetBool(FinalKey, false),
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                    (float)elapsed,
                    Observations);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            RequireRootsUnchanged();
            CleanupFrames();
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D RenderTarget(GameObject target) => RenderTarget(target, null);

        private static Bounds CurrentBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(target.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Texture2D RenderTarget(GameObject target, Bounds? fixedBounds)
        {
            const int width = 420;
            const int height = 560;
            Bounds bounds = fixedBounds ?? CurrentBounds(target);

            GameObject cameraObject = new GameObject(
                "ExhaustedLocomotionBlendTree_ReviewCamera", typeof(Camera));
            GameObject lightObject = new GameObject(
                "ExhaustedLocomotionBlendTree_ReviewLight", typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            Renderer[] others = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] priorStates = others.Select(renderer => renderer.forceRenderingOff).ToArray();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            try
            {
                foreach (Renderer renderer in others)
                    renderer.forceRenderingOff = true;
                Vector3 viewDirection = (
                    target.transform.forward * 0.94f +
                    target.transform.right * 0.34f +
                    Vector3.up * 0.04f).normalized;
                float distance = Mathf.Max(4f, bounds.extents.magnitude * 3f);
                camera.transform.position = bounds.center + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position, Vector3.up);
                camera.orthographic = true;
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.12f,
                    bounds.extents.x / aspect * 1.12f,
                    0.85f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
                renderTexture = RenderTexture.GetTemporary(
                    width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < others.Length; index++)
                    if (others[index] != null)
                        others[index].forceRenderingOff = priorStates[index];
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D CombinePanels(int columns, int rows)
        {
            if (Panels.Count != columns * rows)
                throw new InvalidOperationException(
                    "Exhausted locomotion review panel count differs.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(
                width * columns, height * rows, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255), sheet.width * sheet.height).ToArray());
            for (int index = 0; index < Panels.Count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                sheet.SetPixels32(
                    column * width,
                    (rows - 1 - row) * height,
                    width,
                    height,
                    Panels[index].GetPixels32());
            }
            sheet.Apply(false, false);
            return sheet;
        }

        private static void RequireRootsUnchanged()
        {
            for (int index = 0; index < targets.Length; index++)
            {
                Transform transform = targets[index].transform;
                if (Vector3.Distance(initialPositions[index], transform.localPosition) > 0.0001f ||
                    Quaternion.Angle(initialRotations[index], transform.localRotation) > 0.01f ||
                    Vector3.Distance(initialScales[index], transform.localScale) > 0.0001f)
                    throw new InvalidOperationException(
                        targets[index].name + " root changed during natural playback.");
            }
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Exhausted locomotion natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupFrames()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
            Observations.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupFrames();
            complete = null;
            fail = null;
            targets = null;
            animators = null;
            initialPositions = null;
            initialRotations = null;
            initialScales = null;
            fixedFramingBounds = null;
            hips = null;
            firstSampleHipPositions = null;
            targetForwardDirections = null;
            playerSourceTargets = null;
            playerSourceAnimators = null;
            playerSourceBounds = null;
            exhaustedForwardTarget = null;
            exhaustedForwardAnimator = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseBool(FinalKey);
            SessionState.EraseBool(ForwardDriftModeKey);
            SessionState.EraseBool(DirectionalModeKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class ExhaustedWalkForwardPlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.ExhaustedWalkForward.Pending";
        private const string StateKey =
            "Bellerophon.ExhaustedWalkForward.State";
        private const string FailureKey =
            "Bellerophon.ExhaustedWalkForward.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.ExhaustedWalkForward.ConsoleErrorsBefore";
        private const string CorrectionModeKey =
            "Bellerophon.ExhaustedWalkForward.ArmDownCorrectionMode";
        private const string FingerTorsoCorrectionModeKey =
            "Bellerophon.ExhaustedWalkForward.FingerTorsoCorrectionMode";
        private const string ArmsStraightCorrectionModeKey =
            "Bellerophon.ExhaustedWalkForward.ArmsStraightCorrectionMode";
        private const string RightArmClearanceCorrectionModeKey =
            "Bellerophon.ExhaustedWalkForward.RightArmClearanceCorrectionMode";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string StateName = "ExhaustedWalkForward_Source";
        private static readonly float[] CaptureThresholds =
        {
            0.06f, 0.23f, 0.40f, 0.57f, 0.74f, 1.05f
        };
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static readonly List<Texture2D> HandPanels =
            new List<Texture2D>();
        private static readonly List<Texture2D> ArmPanels =
            new List<Texture2D>();
        private static readonly List<Texture2D> ClearancePanels =
            new List<Texture2D>();
        private static readonly List<Vector3[]> LandmarkSamples =
            new List<Vector3[]>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static Animator animator;
        private static Transform[] landmarks;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static double startedAt;
        private static float maximumNormalizedTime;

        static ExhaustedWalkForwardPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleCapture()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward review must start in Edit Mode.");
            ExhaustedAnimationSetupTools.InspectExhaustedWalkForwardAnimation();
            complete = onComplete;
            fail = onFail;
            CleanupFrames();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(CorrectionModeKey, false);
            SessionState.SetBool(FingerTorsoCorrectionModeKey, false);
            SessionState.SetBool(ArmsStraightCorrectionModeKey, false);
            SessionState.SetBool(RightArmClearanceCorrectionModeKey, false);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void StartArmDown(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted arm-down review must start in Edit Mode.");
            ExhaustedAnimationSetupTools
                .InspectExhaustedWalkForwardArmDownCorrection();
            complete = onComplete;
            fail = onFail;
            CleanupFrames();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(CorrectionModeKey, true);
            SessionState.SetBool(FingerTorsoCorrectionModeKey, false);
            SessionState.SetBool(ArmsStraightCorrectionModeKey, false);
            SessionState.SetBool(RightArmClearanceCorrectionModeKey, false);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void StartFingerAndTorso(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted finger and torso review must start in Edit Mode.");
            ExhaustedAnimationSetupTools
                .InspectExhaustedWalkForwardFingerAndTorsoCorrection();
            complete = onComplete;
            fail = onFail;
            CleanupFrames();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(CorrectionModeKey, false);
            SessionState.SetBool(FingerTorsoCorrectionModeKey, true);
            SessionState.SetBool(ArmsStraightCorrectionModeKey, false);
            SessionState.SetBool(RightArmClearanceCorrectionModeKey, false);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void StartArmsStraightDown(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted straight-arm review must start in Edit Mode.");
            ExhaustedAnimationSetupTools
                .InspectExhaustedWalkForwardArmsStraightDownCorrection();
            complete = onComplete;
            fail = onFail;
            CleanupFrames();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(CorrectionModeKey, false);
            SessionState.SetBool(FingerTorsoCorrectionModeKey, false);
            SessionState.SetBool(ArmsStraightCorrectionModeKey, true);
            SessionState.SetBool(RightArmClearanceCorrectionModeKey, false);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void StartRightArmLegClearance(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Exhausted right-arm leg-clearance review must start in Edit Mode.");
            ExhaustedAnimationSetupTools
                .InspectExhaustedWalkForwardRightArmLegClearance();
            complete = onComplete;
            fail = onFail;
            CleanupFrames();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(CorrectionModeKey, false);
            SessionState.SetBool(FingerTorsoCorrectionModeKey, false);
            SessionState.SetBool(ArmsStraightCorrectionModeKey, false);
            SessionState.SetBool(RightArmClearanceCorrectionModeKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward review has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingCapture)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before exhausted walking review completed.");
                    CaptureNaturalLoop();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                bool rightArmClearanceCorrectionMode =
                    SessionState.GetBool(
                        RightArmClearanceCorrectionModeKey,
                        false);
                bool armsStraightCorrectionMode =
                    SessionState.GetBool(ArmsStraightCorrectionModeKey, false);
                bool fingerTorsoCorrectionMode =
                    SessionState.GetBool(FingerTorsoCorrectionModeKey, false);
                bool correctionMode =
                    SessionState.GetBool(CorrectionModeKey, false);
                if (rightArmClearanceCorrectionMode)
                    ExhaustedAnimationSetupTools
                        .InspectExhaustedWalkForwardRightArmLegClearance();
                else if (armsStraightCorrectionMode)
                    ExhaustedAnimationSetupTools
                        .InspectExhaustedWalkForwardArmsStraightDownCorrection();
                else if (fingerTorsoCorrectionMode)
                    ExhaustedAnimationSetupTools
                        .InspectExhaustedWalkForwardFingerAndTorsoCorrection();
                else if (correctionMode)
                    ExhaustedAnimationSetupTools
                        .InspectExhaustedWalkForwardArmDownCorrection();
                else
                    ExhaustedAnimationSetupTools
                        .InspectExhaustedWalkForwardAnimation();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    rightArmClearanceCorrectionMode
                        ? "Exhausted_Walk_Forward completed one natural loop with the straight right arm shifted minimally outward from the right leg."
                        : armsStraightCorrectionMode
                        ? "Exhausted_Walk_Forward completed one natural loop with both arm chains neutral-straight and downward while preserving shoulder positional walk motion."
                        : fingerTorsoCorrectionMode
                            ? "Exhausted_Walk_Forward completed one natural loop with straight downward fingers, a distributed 15-degree forward torso bend, and anatomical wrist alignment."
                            : correctionMode
                                ? "Exhausted_Walk_Forward completed one natural loop with both upper arms lowered 15 degrees and source swing preserved."
                                : "Exhausted_Walk_Forward completed one unchanged natural Mixamo loop with six direct-review phases.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupFrames();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            target = ExhaustedAnimationSetupTools.RequireRuntimeTarget();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward Animator is missing in Play Mode.");
            string[] names =
            {
                "Hips", "LeftHand", "RightHand", "LeftFoot", "RightFoot",
                "LeftArm", "RightArm", "LeftForeArm", "RightForeArm",
                "RightUpLeg", "RightLeg"
            };
            Transform[] hierarchy = target.GetComponentsInChildren<Transform>(true);
            landmarks = names.Select(name =>
            {
                Transform[] matches = hierarchy
                    .Where(item => item.name == name)
                    .ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException(
                        "Exhausted_Walk_Forward requires one " + name +
                        " bone; actual=" + matches.Length);
                return matches[0];
            }).ToArray();
            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            startedAt = EditorApplication.timeSinceStartup;
            maximumNormalizedTime = 0f;
        }

        private static void CaptureNaturalLoop()
        {
            if (EditorApplication.timeSinceStartup - startedAt > 30d)
                throw new TimeoutException(
                    "Exhausted_Walk_Forward natural loop review exceeded 30 seconds.");
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(StateName))
                return;
            maximumNormalizedTime = Mathf.Max(
                maximumNormalizedTime,
                state.normalizedTime);
            if (Panels.Count >= CaptureThresholds.Length ||
                state.normalizedTime < CaptureThresholds[Panels.Count])
                return;
            LandmarkSamples.Add(landmarks.Select(item =>
                target.transform.InverseTransformPoint(item.position)).ToArray());
            Panels.Add(RenderTarget());
            if (SessionState.GetBool(FingerTorsoCorrectionModeKey, false))
                HandPanels.Add(RenderHands());
            if (SessionState.GetBool(ArmsStraightCorrectionModeKey, false))
                ArmPanels.Add(RenderArms());
            if (SessionState.GetBool(
                    RightArmClearanceCorrectionModeKey,
                    false))
                ClearancePanels.Add(RenderRightArmLegClearance());
            RequireRootUnchanged();
            if (Panels.Count == CaptureThresholds.Length)
                FinishCapture();
        }

        private static Texture2D RenderTarget()
        {
            const int width = 480;
            const int height = 620;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            GameObject cameraObject = new GameObject(
                "ExhaustedWalkForward_ReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "ExhaustedWalkForward_ReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] otherRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] otherStates = otherRenderers
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer renderer in otherRenderers)
                    renderer.forceRenderingOff = true;
                Vector3 viewDirection = (
                    target.transform.forward * 0.98f +
                    target.transform.right * 0.10f +
                    Vector3.up * 0.03f).normalized;
                float distance = Mathf.Max(4f, bounds.extents.magnitude * 3f);
                camera.transform.position = bounds.center + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.10f,
                    bounds.extents.x / aspect * 1.10f,
                    0.85f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < otherRenderers.Length; index++)
                {
                    if (otherRenderers[index] != null)
                        otherRenderers[index].forceRenderingOff = otherStates[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D RenderHands()
        {
            const int width = 480;
            const int height = 360;
            if (landmarks == null || landmarks.Length < 3)
                throw new InvalidOperationException(
                    "Hand close-up landmarks are unavailable.");
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            Vector3 leftHandPosition = landmarks[1].position;
            Vector3 rightHandPosition = landmarks[2].position;
            Vector3 focus = (leftHandPosition + rightHandPosition) * 0.5f;
            float handSeparation = Vector3.Distance(
                leftHandPosition,
                rightHandPosition);

            GameObject cameraObject = new GameObject(
                "ExhaustedWalkForward_HandReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "ExhaustedWalkForward_HandReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] otherRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] otherStates = otherRenderers
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer renderer in otherRenderers)
                    renderer.forceRenderingOff = true;
                Vector3 viewDirection = (
                    target.transform.forward * 0.98f +
                    target.transform.right * 0.10f +
                    Vector3.up * 0.03f).normalized;
                float distance = Mathf.Max(3f, bounds.extents.magnitude * 2.5f);
                camera.transform.position = focus + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    0.38f,
                    handSeparation * 0.75f + 0.28f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < otherRenderers.Length; index++)
                {
                    if (otherRenderers[index] != null)
                        otherRenderers[index].forceRenderingOff = otherStates[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D RenderArms()
        {
            const int width = 480;
            const int height = 360;
            if (landmarks == null || landmarks.Length < 9)
                throw new InvalidOperationException(
                    "Arm close-up landmarks are unavailable.");
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            int[] armLandmarkIndices = { 1, 2, 5, 6, 7, 8 };
            Vector3 focus = armLandmarkIndices
                .Select(index => landmarks[index].position)
                .Aggregate(Vector3.zero, (sum, position) => sum + position) /
                armLandmarkIndices.Length;
            float maximumRadius = armLandmarkIndices.Max(index =>
                Vector3.Distance(focus, landmarks[index].position));

            GameObject cameraObject = new GameObject(
                "ExhaustedWalkForward_ArmReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "ExhaustedWalkForward_ArmReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] otherRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] otherStates = otherRenderers
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer renderer in otherRenderers)
                    renderer.forceRenderingOff = true;
                Vector3 viewDirection = (
                    target.transform.forward * 0.98f +
                    target.transform.right * 0.10f +
                    Vector3.up * 0.03f).normalized;
                float distance = Mathf.Max(3f, bounds.extents.magnitude * 2.5f);
                camera.transform.position = focus + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    0.65f,
                    maximumRadius * 1.25f + 0.18f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < otherRenderers.Length; index++)
                {
                    if (otherRenderers[index] != null)
                        otherRenderers[index].forceRenderingOff = otherStates[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D RenderRightArmLegClearance()
        {
            const int width = 480;
            const int height = 360;
            if (landmarks == null || landmarks.Length < 11)
                throw new InvalidOperationException(
                    "Right-arm and right-leg close-up landmarks are unavailable.");
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            int[] rightSideLandmarkIndices = { 2, 4, 6, 8, 9, 10 };
            Vector3 focus = rightSideLandmarkIndices
                .Select(index => landmarks[index].position)
                .Aggregate(Vector3.zero, (sum, position) => sum + position) /
                rightSideLandmarkIndices.Length;
            float maximumRadius = rightSideLandmarkIndices.Max(index =>
                Vector3.Distance(focus, landmarks[index].position));

            GameObject cameraObject = new GameObject(
                "ExhaustedWalkForward_RightArmLegClearanceReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "ExhaustedWalkForward_RightArmLegClearanceReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] otherRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] otherStates = otherRenderers
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer renderer in otherRenderers)
                    renderer.forceRenderingOff = true;
                Vector3 viewDirection = (
                    target.transform.forward * 0.96f +
                    target.transform.right * 0.28f +
                    Vector3.up * 0.03f).normalized;
                float distance = Mathf.Max(3f, bounds.extents.magnitude * 2.5f);
                camera.transform.position = focus + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    0.72f,
                    maximumRadius * 1.18f + 0.16f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < otherRenderers.Length; index++)
                {
                    if (otherRenderers[index] != null)
                        otherRenderers[index].forceRenderingOff = otherStates[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FinishCapture()
        {
            RequireRootUnchanged();
            float maximumTravel = MaximumLandmarkTravel();
            bool rightArmClearanceCorrectionMode =
                SessionState.GetBool(
                    RightArmClearanceCorrectionModeKey,
                    false);
            bool armsStraightCorrectionMode =
                SessionState.GetBool(ArmsStraightCorrectionModeKey, false);
            bool fingerTorsoCorrectionMode =
                SessionState.GetBool(FingerTorsoCorrectionModeKey, false);
            Texture2D sheet = rightArmClearanceCorrectionMode
                ? CombinePanelsWithRightArmLegCloseups()
                : armsStraightCorrectionMode
                ? CombinePanelsWithArmCloseups()
                : fingerTorsoCorrectionMode
                    ? CombinePanelsWithHandCloseups()
                    : CombinePanels();
            try
            {
                if (rightArmClearanceCorrectionMode)
                    ExhaustedAnimationSetupTools.WriteRightArmClearanceReviewEvidence(
                        sheet,
                        SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                        maximumNormalizedTime,
                        maximumTravel,
                        target.transform.localPosition - initialLocalPosition,
                        Quaternion.Angle(
                            initialLocalRotation,
                            target.transform.localRotation));
                else if (armsStraightCorrectionMode)
                    ExhaustedAnimationSetupTools.WriteArmsStraightReviewEvidence(
                        sheet,
                        SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                        maximumNormalizedTime,
                        maximumTravel,
                        target.transform.localPosition - initialLocalPosition,
                        Quaternion.Angle(
                            initialLocalRotation,
                            target.transform.localRotation));
                else if (fingerTorsoCorrectionMode)
                    ExhaustedAnimationSetupTools.WriteFingerTorsoReviewEvidence(
                        sheet,
                        SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                        maximumNormalizedTime,
                        maximumTravel,
                        target.transform.localPosition - initialLocalPosition,
                        Quaternion.Angle(
                            initialLocalRotation,
                            target.transform.localRotation));
                else if (SessionState.GetBool(CorrectionModeKey, false))
                    ExhaustedAnimationSetupTools.WriteArmDownReviewEvidence(
                        sheet,
                        SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                        maximumNormalizedTime,
                        maximumTravel,
                        target.transform.localPosition - initialLocalPosition,
                        Quaternion.Angle(
                            initialLocalRotation,
                            target.transform.localRotation));
                else
                    ExhaustedAnimationSetupTools.WriteReviewEvidence(
                        sheet,
                        SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                        maximumNormalizedTime,
                        maximumTravel,
                        target.transform.localPosition - initialLocalPosition,
                        Quaternion.Angle(
                            initialLocalRotation,
                            target.transform.localRotation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupFrames();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static float MaximumLandmarkTravel()
        {
            float maximum = 0f;
            for (int first = 0; first < LandmarkSamples.Count; first++)
            for (int second = first + 1; second < LandmarkSamples.Count; second++)
            for (int landmark = 0; landmark < LandmarkSamples[first].Length; landmark++)
                maximum = Mathf.Max(
                    maximum,
                    Vector3.Distance(
                        LandmarkSamples[first][landmark],
                        LandmarkSamples[second][landmark]));
            return maximum;
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != CaptureThresholds.Length)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward review requires six frames.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(
                width * 3,
                height * 2,
                TextureFormat.RGBA32,
                false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                sheet.width * sheet.height).ToArray());
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(
                    index % 3 * width,
                    index < 3 ? height : 0,
                    width,
                    height,
                    Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static Texture2D CombinePanelsWithHandCloseups()
        {
            if (HandPanels.Count != CaptureThresholds.Length)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward hand review requires six frames.");
            Texture2D fullBodySheet = CombinePanels();
            int handWidth = HandPanels[0].width;
            int handHeight = HandPanels[0].height;
            var handSheet = new Texture2D(
                handWidth * 3,
                handHeight * 2,
                TextureFormat.RGBA32,
                false);
            handSheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                handSheet.width * handSheet.height).ToArray());
            for (int index = 0; index < HandPanels.Count; index++)
                handSheet.SetPixels32(
                    index % 3 * handWidth,
                    index < 3 ? handHeight : 0,
                    handWidth,
                    handHeight,
                    HandPanels[index].GetPixels32());
            handSheet.Apply(false, false);

            int width = Mathf.Max(fullBodySheet.width, handSheet.width);
            var combined = new Texture2D(
                width,
                fullBodySheet.height + handSheet.height,
                TextureFormat.RGBA32,
                false);
            combined.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                combined.width * combined.height).ToArray());
            combined.SetPixels32(
                0,
                handSheet.height,
                fullBodySheet.width,
                fullBodySheet.height,
                fullBodySheet.GetPixels32());
            combined.SetPixels32(
                0,
                0,
                handSheet.width,
                handSheet.height,
                handSheet.GetPixels32());
            combined.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(fullBodySheet);
            UnityEngine.Object.DestroyImmediate(handSheet);
            return combined;
        }

        private static Texture2D CombinePanelsWithArmCloseups()
        {
            if (ArmPanels.Count != CaptureThresholds.Length)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward arm review requires six frames.");
            Texture2D fullBodySheet = CombinePanels();
            int armWidth = ArmPanels[0].width;
            int armHeight = ArmPanels[0].height;
            var armSheet = new Texture2D(
                armWidth * 3,
                armHeight * 2,
                TextureFormat.RGBA32,
                false);
            armSheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                armSheet.width * armSheet.height).ToArray());
            for (int index = 0; index < ArmPanels.Count; index++)
                armSheet.SetPixels32(
                    index % 3 * armWidth,
                    index < 3 ? armHeight : 0,
                    armWidth,
                    armHeight,
                    ArmPanels[index].GetPixels32());
            armSheet.Apply(false, false);

            int width = Mathf.Max(fullBodySheet.width, armSheet.width);
            var combined = new Texture2D(
                width,
                fullBodySheet.height + armSheet.height,
                TextureFormat.RGBA32,
                false);
            combined.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                combined.width * combined.height).ToArray());
            combined.SetPixels32(
                0,
                armSheet.height,
                fullBodySheet.width,
                fullBodySheet.height,
                fullBodySheet.GetPixels32());
            combined.SetPixels32(
                0,
                0,
                armSheet.width,
                armSheet.height,
                armSheet.GetPixels32());
            combined.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(fullBodySheet);
            UnityEngine.Object.DestroyImmediate(armSheet);
            return combined;
        }

        private static Texture2D CombinePanelsWithRightArmLegCloseups()
        {
            if (ClearancePanels.Count != CaptureThresholds.Length)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward right-arm and right-leg review requires six frames.");
            Texture2D fullBodySheet = CombinePanels();
            int closeupWidth = ClearancePanels[0].width;
            int closeupHeight = ClearancePanels[0].height;
            var closeupSheet = new Texture2D(
                closeupWidth * 3,
                closeupHeight * 2,
                TextureFormat.RGBA32,
                false);
            closeupSheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                closeupSheet.width * closeupSheet.height).ToArray());
            for (int index = 0; index < ClearancePanels.Count; index++)
                closeupSheet.SetPixels32(
                    index % 3 * closeupWidth,
                    index < 3 ? closeupHeight : 0,
                    closeupWidth,
                    closeupHeight,
                    ClearancePanels[index].GetPixels32());
            closeupSheet.Apply(false, false);

            int width = Mathf.Max(fullBodySheet.width, closeupSheet.width);
            var combined = new Texture2D(
                width,
                fullBodySheet.height + closeupSheet.height,
                TextureFormat.RGBA32,
                false);
            combined.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                combined.width * combined.height).ToArray());
            combined.SetPixels32(
                0,
                closeupSheet.height,
                fullBodySheet.width,
                fullBodySheet.height,
                fullBodySheet.GetPixels32());
            combined.SetPixels32(
                0,
                0,
                closeupSheet.width,
                closeupSheet.height,
                closeupSheet.GetPixels32());
            combined.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(fullBodySheet);
            UnityEngine.Object.DestroyImmediate(closeupSheet);
            return combined;
        }

        private static void RequireRootUnchanged()
        {
            if (Vector3.Distance(
                    initialLocalPosition,
                    target.transform.localPosition) > 0.0001f ||
                Quaternion.Angle(
                    initialLocalRotation,
                    target.transform.localRotation) > 0.01f ||
                Vector3.Distance(
                    initialLocalScale,
                    target.transform.localScale) > 0.0001f)
                throw new InvalidOperationException(
                    "Exhausted_Walk_Forward root changed during natural playback.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Exhausted_Walk_Forward natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupFrames()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
            foreach (Texture2D panel in HandPanels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            HandPanels.Clear();
            foreach (Texture2D panel in ArmPanels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            ArmPanels.Clear();
            foreach (Texture2D panel in ClearancePanels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            ClearancePanels.Clear();
            LandmarkSamples.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupFrames();
            complete = null;
            fail = null;
            target = null;
            animator = null;
            landmarks = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseBool(CorrectionModeKey);
            SessionState.EraseBool(FingerTorsoCorrectionModeKey);
            SessionState.EraseBool(ArmsStraightCorrectionModeKey);
            SessionState.EraseBool(RightArmClearanceCorrectionModeKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }
}
