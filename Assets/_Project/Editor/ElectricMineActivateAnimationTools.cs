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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class ElectricMineActivateAnimationTools
    {
        internal const string TargetName = "ElectricMine_Activate";
        internal const string TempFolder =
            "Temp/ElectricMineActivateSmoothNoTorsoOverlap";
        internal const string OutputFolder =
            "docs/validation/ElectricMineActivateSmoothNoTorsoOverlap";
        internal const string ReviewImagePath = TempFolder + "/Review.png";
        internal const string ReviewReportPath = TempFolder + "/Review.txt";
        internal const string ContinuousCaptureFolder =
            TempFolder + "/UnityContinuousFrames";
        internal const string ContinuousCaptureListPath =
            TempFolder + "/UnityContinuousFrames.ffconcat";
        internal const string ContinuousCaptureReportPath =
            TempFolder + "/ContinuousCapture.txt";
        internal const string FinalImagePath = OutputFolder + "/Final.png";
        internal const string FinalReportPath = OutputFolder + "/Final.txt";
        internal const string ClipPath =
            "Assets/_Project/Art/Player/Animations/ElectricMineActivate/ElectricMine_Activate_ButtonPress.anim";
        internal const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/ElectricMineActivate/ElectricMine_Activate_ButtonPress.controller";
        internal const float ReachSeconds = 0.4f;
        internal const float ContactSeconds = 0.2f;
        internal const float ReturnSeconds = 0.5f;
        internal const float DurationSeconds = ReachSeconds + ContactSeconds + ReturnSeconds;

        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string AssetFolder =
            "Assets/_Project/Art/Player/Animations/ElectricMineActivate";
        private const string LeftShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder";
        private const string LeftArmPath = LeftShoulderPath + "/LeftArm";
        private const string LeftForeArmPath = LeftArmPath + "/LeftForeArm";
        private const string LeftHandPath = LeftForeArmPath + "/LeftHand";
        private const string SpineLowerPath = "Armature/Hips/Spine02";
        private const string SpineUpperPath = "Armature/Hips/Spine02/Spine01/Spine";
        private const string RightShoulderPath = SpineUpperPath + "/RightShoulder";
        private const string RightUpperArmPath = RightShoulderPath + "/RightArm";
        private const string LeftIndexProximalPath = LeftHandPath + "/LeftIndexProximal";
        private const string LeftIndexIntermediatePath =
            LeftIndexProximalPath + "/LeftIndexIntermediate";
        private const string LeftIndexDistalPath =
            LeftIndexIntermediatePath + "/LeftIndexDistal";
        private const string RightArmPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm";
        private const string PropName = "ElectricMine_Prop";
        private const float ContactClearanceMeters = 0.002f;
        private const float ContactToleranceMeters = 0.012f;
        internal const float RequiredHandSurfaceClearanceMeters = 0f;
        private const float SolvedPoseSurfaceClearanceMeters = 0.003f;
        internal const float RequiredTorsoClearanceMeters = 0.002f;
        private const float InitialWristSurfaceOffsetMeters = 0.045f;
        private const float WristSurfaceOffsetStepMeters = 0.005f;
        private const int WristSurfaceOffsetAttempts = 20;
        private const float TransformTolerance = 0.00001f;
        private const float RotationTolerance = 0.02f;
        private const int SampleRate = 60;
        private const int CurveKeyRate = 240;
        private const float MaximumAdjacentLeftArmRotationDegrees = 3f;
        private const float MaximumAdjacentRotationAccelerationDegrees = 0.65f;
        private const int PanelSize = 420;
        private const int PanelGap = 8;

        private static readonly string[] LeftPosePaths =
        {
            LeftShoulderPath,
            LeftArmPath,
            LeftForeArmPath,
            LeftHandPath,
            LeftIndexProximalPath,
            LeftIndexIntermediatePath,
            LeftIndexDistalPath,
            LeftHandPath + "/LeftMiddleProximal",
            LeftHandPath + "/LeftMiddleProximal/LeftMiddleIntermediate",
            LeftHandPath + "/LeftMiddleProximal/LeftMiddleIntermediate/LeftMiddleDistal",
            LeftHandPath + "/LeftRingProximal",
            LeftHandPath + "/LeftRingProximal/LeftRingIntermediate",
            LeftHandPath + "/LeftRingProximal/LeftRingIntermediate/LeftRingDistal",
            LeftHandPath + "/LeftLittleProximal",
            LeftHandPath + "/LeftLittleProximal/LeftLittleIntermediate",
            LeftHandPath + "/LeftLittleProximal/LeftLittleIntermediate/LeftLittleDistal",
            LeftHandPath + "/LeftThumbProximal",
            LeftHandPath + "/LeftThumbProximal/LeftThumbIntermediate",
            LeftHandPath + "/LeftThumbProximal/LeftThumbIntermediate/LeftThumbDistal"
        };

        internal static string ReviewAbsolutePath => Absolute(ReviewImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        [MenuItem("Bellerophon/Player/Inspect Electric Mine Activate Sources")]
        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            Vector3 buttonSurfaceLocal = ButtonSurfaceLocal(prop);
            var report = new StringBuilder()
                .AppendLine("ElectricMine_Activate source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sceneDirty=" + scene.isDirty)
                .AppendLine("target=" + TargetName)
                .AppendLine("targetGlobalId=" + GlobalObjectId.GetGlobalObjectIdSlow(target))
                .AppendLine("animatorController=" +
                    (animator.runtimeAnimatorController == null
                        ? "<none>"
                        : AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)))
                .AppendLine("animatorAvatar=" + AssetDatabase.GetAssetPath(animator.avatar))
                .AppendLine("animatorApplyRootMotion=" + animator.applyRootMotion)
                .AppendLine("propParent=" + RelativePath(target.transform, prop.parent))
                .AppendLine("propLocalPosition=" + Vec(prop.localPosition))
                .AppendLine("propLocalRotation=" + Quat(prop.localRotation))
                .AppendLine("propLocalScale=" + Vec(prop.localScale))
                .AppendLine("buttonSurfaceLocal=" + Vec(buttonSurfaceLocal))
                .AppendLine("buttonSurfaceWorld=" + Vec(prop.TransformPoint(buttonSurfaceLocal)))
                .AppendLine("buttonNormalWorld=" + Vec(prop.up.normalized));
            foreach (string path in LeftPosePaths)
            {
                Transform bone = RequirePath(target.transform, path);
                report.AppendLine("bone=" + path)
                    .AppendLine("boneLocalPosition=" + Vec(bone.localPosition))
                    .AppendLine("boneLocalRotation=" + Quat(bone.localRotation))
                    .AppendLine("boneWorldPosition=" + Vec(bone.position));
            }
            Transform hand = RequirePath(target.transform, LeftHandPath);
            foreach (Transform bone in hand.GetComponentsInChildren<Transform>(true))
                report.AppendLine("leftHandDescendant=" + RelativePath(target.transform, bone))
                    .AppendLine("descendantWorldPosition=" + Vec(bone.position))
                    .AppendLine("descendantLocalRotation=" + Quat(bone.localRotation));
            foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
                report.AppendLine("renderer=" + RelativePath(prop, renderer.transform))
                    .AppendLine("rendererType=" + renderer.GetType().Name)
                    .AppendLine("rendererBoundsCenter=" + Vec(renderer.bounds.center))
                    .AppendLine("rendererBoundsSize=" + Vec(renderer.bounds.size))
                    .AppendLine("rendererMaterials=" + string.Join(",",
                        renderer.sharedMaterials.Select(material =>
                            material == null ? "<null>" : material.name)));
            WriteTemp("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMineActivate] Sources inspected read-only.");
        }

        [MenuItem("Bellerophon/Player/Apply Electric Mine Activate Animation")]
        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            string outsideBefore = SceneSignatureOutsideTarget(scene);
            string targetTransformsBefore = HierarchyTransformSignature(target.transform);
            string rightArmBefore = HierarchySignature(
                RequirePath(target.transform, RightArmPath));
            string propBefore = HierarchySignature(prop);
            Vector3 targetPosition = target.transform.position;
            Quaternion targetRotation = target.transform.rotation;
            Vector3 targetScale = target.transform.localScale;
            Dictionary<string, Quaternion> startPose = CaptureArmaturePose(target);
            PressPoseResult press = BuildPressPoseOnClone(target);
            EnsureFolder(AssetFolder);
            AnimationClip clip = CreateClip(target, startPose, press.LocalRotations);
            AnimatorController controller = CreateController(clip);
            Undo.RecordObject(animator, "Connect ElectricMine_Activate button press");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();
            RequireNear(target.transform.position, targetPosition, "target world position");
            RequireNear(target.transform.rotation, targetRotation, "target world rotation");
            RequireNear(target.transform.localScale, targetScale, "target local scale");
            RequireEqual(targetTransformsBefore,
                HierarchyTransformSignature(target.transform), "target transforms");
            RequireEqual(rightArmBefore,
                HierarchySignature(RequirePath(target.transform, RightArmPath)),
                "right-arm grip hierarchy");
            RequireEqual(propBefore, HierarchySignature(prop), "electric mine hierarchy");
            RequireEqual(outsideBefore, SceneSignatureOutsideTarget(scene),
                "scene outside ElectricMine_Activate");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Absolute(OutputFolder));
            WriteOutput("right_arm_signature.txt", rightArmBefore);
            WriteOutput("mine_signature.txt", propBefore);
            WriteOutput("outside_scene_signature.txt", outsideBefore);
            WriteOutput("target_transform_signature.txt", targetTransformsBefore);
            WriteOutput("scene_asset_hash.txt", Sha256File(Absolute(ScenePath)));
            WriteOutput("application.txt", new StringBuilder()
                .AppendLine("ElectricMine_Activate button press application")
                .AppendLine("sceneSaved=True")
                .AppendLine("controller=" + ControllerPath)
                .AppendLine("clip=" + ClipPath)
                .AppendLine("loopTime=True")
                .AppendLine("reachSeconds=" + F(ReachSeconds))
                .AppendLine("contactSeconds=" + F(ContactSeconds))
                .AppendLine("returnSeconds=" + F(ReturnSeconds))
                .AppendLine("durationSeconds=" + F(DurationSeconds))
                .AppendLine("buttonAnimated=False")
                .AppendLine("leftIndexRigUsed=True")
                .AppendLine("pressPoseSolvedOnTemporaryClone=True")
                .AppendLine("pressContactDistanceMeters=" + F(press.ContactDistance))
                .AppendLine("pressMinimumNonIndexSurfaceClearanceMeters=" +
                    F(press.MinimumNonIndexSurfaceClearance))
                .AppendLine("pressWristSurfaceOffsetMeters=" +
                    F(press.WristSurfaceOffset))
                .AppendLine("pressElbowBendDegrees=" + F(press.ElbowBendDegrees))
                .AppendLine("pressWristDeltaDegrees=" + F(press.WristDeltaDegrees))
                .AppendLine("rightArmGripChanged=False")
                .AppendLine("electricMineTransformChanged=False")
                .AppendLine("electricMineFollowsRightHand=True")
                .AppendLine("otherSceneObjectsChanged=False")
                .ToString());
            InspectStructure();
            Debug.Log("[ElectricMineActivate] Button press loop applied and saved.");
        }

        [MenuItem("Bellerophon/Player/Inspect Electric Mine Activate Animation")]
        internal static void InspectStructure()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null || AssetDatabase.GetAssetPath(controller) != ControllerPath)
                throw new InvalidOperationException("ElectricMine_Activate controller differs.");
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException("Button press default state is missing.");
            AnimationClip clip = state.motion as AnimationClip ??
                throw new InvalidOperationException("Button press state motion is not a clip.");
            if (AssetDatabase.GetAssetPath(clip) != ClipPath)
                throw new InvalidOperationException("Button press clip path differs.");
            if (Mathf.Abs(clip.length - DurationSeconds) > 0.0001f)
                throw new InvalidOperationException("Button press duration differs: " + F(clip.length));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
                throw new InvalidOperationException("Button press clip is not looping.");
            if (animator.applyRootMotion)
                throw new InvalidOperationException("Root Motion must remain disabled.");
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0 || bindings.Any(binding =>
                    !binding.path.StartsWith("Armature", StringComparison.Ordinal) ||
                    binding.path.Contains("/" + PropName, StringComparison.Ordinal) ||
                    !binding.propertyName.StartsWith("m_LocalRotation.",
                        StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Button press clip contains an unexpected binding.");
            RequireEqual(ReadOutput("right_arm_signature.txt"),
                HierarchySignature(RequirePath(target.transform, RightArmPath)),
                "right-arm grip hierarchy");
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            RequireEqual(ReadOutput("mine_signature.txt"), HierarchySignature(prop),
                "electric mine hierarchy");
            RequireEqual(ReadOutput("scene_asset_hash.txt"),
                Sha256File(Absolute(ScenePath)), "saved CargoRunMvp scene asset");
            RequireEqual(ReadOutput("target_transform_signature.txt"),
                HierarchyTransformSignature(target.transform), "target transforms");
            SampleMetrics start = SampleOnClone(target, clip, 0f);
            SampleMetrics contact = SampleOnClone(
                target, clip, ReachSeconds + ContactSeconds * 0.5f);
            SampleMetrics returned = SampleOnClone(target, clip, DurationSeconds);
            PoseContinuityMetrics continuity = MeasureClipContinuity(target, clip);
            float minimumNonIndexClearance = float.MaxValue;
            float minimumTorsoClearance = float.MaxValue;
            int structuralSampleRate = CurveKeyRate * 2;
            int sampleCount = Mathf.RoundToInt(
                DurationSeconds * structuralSampleRate);
            for (int sample = 0; sample <= sampleCount; sample++)
            {
                SampleMetrics metrics = SampleOnClone(
                    target, clip, sample / (float)structuralSampleRate);
                minimumNonIndexClearance = Mathf.Min(
                    minimumNonIndexClearance,
                    metrics.MinimumNonIndexSurfaceClearance);
                minimumTorsoClearance = Mathf.Min(
                    minimumTorsoClearance,
                    metrics.MinimumTorsoClearance);
            }
            if (contact.ContactDistance > ContactToleranceMeters)
                throw new InvalidOperationException(
                    "Left index does not contact the button: " + F(contact.ContactDistance));
            if (minimumNonIndexClearance < RequiredHandSurfaceClearanceMeters)
                throw new InvalidOperationException(
                    "Left hand penetrates the electric mine surface: " +
                    F(minimumNonIndexClearance));
            if (minimumTorsoClearance < RequiredTorsoClearanceMeters)
                throw new InvalidOperationException(
                    "Left arm penetrates the torso envelope: " +
                    F(minimumTorsoClearance));
            if (returned.LeftPoseDeviation > 0.05f || start.LeftPoseDeviation > 0.05f)
                throw new InvalidOperationException("Loop endpoints differ from the start pose.");
            if (continuity.MaximumAdjacentRotationDegrees >
                MaximumAdjacentLeftArmRotationDegrees)
                throw new InvalidOperationException(
                    "Left-arm clip continuity failed: bone=" +
                    continuity.MaximumRotationPath +
                    ", from=" + F(continuity.MaximumRotationFromTime) +
                    ", to=" + F(continuity.MaximumRotationToTime) +
                    ", degrees=" +
                    F(continuity.MaximumAdjacentRotationDegrees));
            if (continuity.MaximumRotationAccelerationDegrees >
                MaximumAdjacentRotationAccelerationDegrees)
                throw new InvalidOperationException(
                    "Left-arm angular velocity changes abruptly: bone=" +
                    continuity.MaximumAccelerationPath +
                    ", at=" + F(continuity.MaximumAccelerationTime) +
                    ", degrees=" +
                    F(continuity.MaximumRotationAccelerationDegrees));
            if (contact.RightArmDeviation > RotationTolerance ||
                returned.RightArmDeviation > RotationTolerance)
                throw new InvalidOperationException("Right-arm grip changed in the clip.");
            if (contact.MineLocalPositionError > TransformTolerance ||
                contact.MineLocalRotationError > RotationTolerance ||
                contact.MineLocalScaleError > TransformTolerance)
                throw new InvalidOperationException("Electric mine local state changed.");
            WriteOutput("inspection.txt", new StringBuilder()
                .AppendLine("ElectricMine_Activate structural inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("controllerPath=" + ControllerPath)
                .AppendLine("clipPath=" + ClipPath)
                .AppendLine("loopTime=True")
                .AppendLine("reachSeconds=" + F(ReachSeconds))
                .AppendLine("contactSeconds=" + F(ContactSeconds))
                .AppendLine("returnSeconds=" + F(ReturnSeconds))
                .AppendLine("durationSeconds=" + F(DurationSeconds))
                .AppendLine("buttonAnimated=False")
                .AppendLine("leftIndexRigUsed=True")
                .AppendLine("curveBindingCount=" + bindings.Length)
                .AppendLine("contactDistanceMeters=" + F(contact.ContactDistance))
                .AppendLine("minimumNonIndexSurfaceClearanceMeters=" +
                    F(minimumNonIndexClearance))
                .AppendLine("leftHandMinePenetration=False")
                .AppendLine("minimumLeftArmTorsoClearanceMeters=" +
                    F(minimumTorsoClearance))
                .AppendLine("leftArmTorsoIntersection=False")
                .AppendLine("contactElbowBendDegrees=" + F(contact.ElbowBendDegrees))
                .AppendLine("contactWristDeltaDegrees=" + F(contact.WristDeltaDegrees))
                .AppendLine("loopEndpointLeftPoseDeviationDegrees=" +
                    F(returned.LeftPoseDeviation))
                .AppendLine("maximumAdjacentLeftArmRotationDegrees=" +
                    F(continuity.MaximumAdjacentRotationDegrees))
                .AppendLine("maximumAdjacentLeftArmRotationBone=" +
                    continuity.MaximumRotationPath)
                .AppendLine("maximumAdjacentLeftArmRotationFromSeconds=" +
                    F(continuity.MaximumRotationFromTime))
                .AppendLine("maximumAdjacentLeftArmRotationToSeconds=" +
                    F(continuity.MaximumRotationToTime))
                .AppendLine("leftArmContinuityThresholdDegrees=" +
                    F(MaximumAdjacentLeftArmRotationDegrees))
                .AppendLine(
                    "leftArmPath=MinimumJerkBezierWithStableElbowPole")
                .AppendLine("maximumRotationAccelerationDegrees=" +
                    F(continuity.MaximumRotationAccelerationDegrees))
                .AppendLine("maximumRotationAccelerationBone=" +
                    continuity.MaximumAccelerationPath)
                .AppendLine("maximumRotationAccelerationAtSeconds=" +
                    F(continuity.MaximumAccelerationTime))
                .AppendLine("rotationAccelerationThresholdDegrees=" +
                    F(MaximumAdjacentRotationAccelerationDegrees))
                .AppendLine("returnSettledHoldSeconds=0.05")
                .AppendLine("maximumSampledRightArmDeviationDegrees=" +
                    F(Mathf.Max(contact.RightArmDeviation, returned.RightArmDeviation)))
                .AppendLine("electricMineTransformChanged=False")
                .AppendLine("electricMineFollowsRightHand=True")
                .AppendLine("targetTransformsChanged=False")
                .AppendLine("otherSceneObjectsChanged=False")
                .ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMineActivate] Structural inspection passed.");
        }

        internal static void BeginNaturalRuntimeReview(string requestId, string logPath)
        {
            RequireEditMode();
            ElectricMineActivatePlayModeReview.Start(requestId, logPath, false);
        }

        internal static void BeginContinuousRuntimeReview(
            string requestId,
            string logPath)
        {
            RequireEditMode();
            ElectricMineActivatePlayModeReview.Start(requestId, logPath, true);
        }

        [MenuItem("Bellerophon/Player/Capture Electric Mine Activate Animation Final")]
        internal static void CaptureFinal()
        {
            RequireEditMode();
            InspectStructure();
            string review = Absolute(ReviewImagePath);
            string reviewReport = Absolute(ReviewReportPath);
            if (!File.Exists(review) || !File.Exists(reviewReport))
                throw new InvalidOperationException("Natural button press review is missing.");
            string report = File.ReadAllText(reviewReport, Encoding.UTF8);
            if (!report.Contains("passed=True") ||
                !report.Contains("naturalPlayback=True") ||
                !report.Contains("targetManipulatedByValidation=False") ||
                !report.Contains("cyclesObserved=2") ||
                !report.Contains("leftIndexContactsButton=True") ||
                !report.Contains("leftHandMinePenetration=False") ||
                !report.Contains("leftArmTorsoIntersection=False") ||
                !report.Contains("electricMineFollowsRightHand=True"))
                throw new InvalidOperationException("Natural button press review did not pass.");
            string final = Absolute(FinalImagePath);
            string finalReport = Absolute(FinalReportPath);
            if (File.Exists(final) || File.Exists(finalReport))
                throw new InvalidOperationException("Final button press capture already exists.");
            Directory.CreateDirectory(Absolute(OutputFolder));
            File.Copy(review, final, false);
            File.WriteAllText(finalReport, new StringBuilder()
                .AppendLine("ElectricMine_Activate final direct contact sheet")
                .AppendLine("leftToRight=Start,ReachEarly,Contact,ContactHold,Return,Returned")
                .AppendLine("topRow=full body")
                .AppendLine("bottomRow=left arm, left index and red button close-up")
                .AppendLine("reachSeconds=" + F(ReachSeconds))
                .AppendLine("contactSeconds=" + F(ContactSeconds))
                .AppendLine("returnSeconds=" + F(ReturnSeconds))
                .AppendLine("buttonAnimated=False")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("finalSha256=" + Sha256File(final))
                .ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMineActivate] Final contact sheet captured once.");
        }

        internal static GameObject RequireRuntimeTarget()
        {
            return FindUnique(RequireScene(), TargetName);
        }

        internal static AnimationClip RequireRuntimeClip(GameObject target)
        {
            AnimatorController controller = RequireAnimator(target).runtimeAnimatorController
                as AnimatorController;
            AnimationClip clip = controller?.layers[0].stateMachine.defaultState?.motion
                as AnimationClip;
            return clip ?? throw new InvalidOperationException(
                "ElectricMine_Activate runtime clip is missing.");
        }

        internal static RuntimeMetrics MeasureRuntime(
            GameObject target,
            IReadOnlyDictionary<string, Quaternion> initialRightPose,
            Vector3 initialPropPosition,
            Quaternion initialPropRotation,
            Vector3 initialPropScale)
        {
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            float rightDeviation = 0f;
            foreach (KeyValuePair<string, Quaternion> item in initialRightPose)
                rightDeviation = Mathf.Max(rightDeviation, Quaternion.Angle(
                    RequirePath(target.transform, item.Key).localRotation, item.Value));
            float contact = Vector3.Distance(EstimateIndexTip(target), ButtonWorld(prop));
            Transform upper = RequirePath(target.transform, LeftArmPath);
            Transform fore = RequirePath(target.transform, LeftForeArmPath);
            Transform hand = RequirePath(target.transform, LeftHandPath);
            return new RuntimeMetrics(
                contact,
                ElbowBend(upper, fore, hand),
                MeasureNonIndexSurfaceClearance(target, prop),
                MeasureLeftArmTorsoClearance(target),
                rightDeviation,
                Vector3.Distance(prop.localPosition, initialPropPosition),
                Quaternion.Angle(prop.localRotation, initialPropRotation),
                Vector3.Distance(prop.localScale, initialPropScale),
                prop.parent != null && prop.parent.name == "RightHand");
        }

        internal static Dictionary<string, Quaternion> CaptureRightPose(GameObject target)
        {
            Transform rightArm = RequirePath(target.transform, RightArmPath);
            return rightArm.GetComponentsInChildren<Transform>(true)
                .Where(item => !HasAncestorNamed(item, PropName))
                .ToDictionary(
                    item => RelativePath(target.transform, item),
                    item => item.localRotation,
                    StringComparer.Ordinal);
        }

        internal static Dictionary<string, Quaternion> CaptureLeftPose(GameObject target)
        {
            return LeftPosePaths.ToDictionary(
                path => path,
                path => RequirePath(target.transform, path).localRotation,
                StringComparer.Ordinal);
        }

        internal static Dictionary<string, Quaternion> CaptureClipStartLeftPose(
            GameObject target,
            AnimationClip clip)
        {
            GameObject clone = UnityEngine.Object.Instantiate(target);
            clone.name = "ElectricMineActivate_StartPoseSample";
            SetHideFlagsRecursively(clone, HideFlags.HideAndDontSave);
            clone.SetActive(false);
            try
            {
                Animator animator = clone.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;
                clip.SampleAnimation(clone, 0f);
                return CaptureLeftPose(clone);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        internal static float LeftPoseDeviation(
            GameObject target,
            IReadOnlyDictionary<string, Quaternion> pose)
        {
            float maximum = 0f;
            foreach (KeyValuePair<string, Quaternion> item in pose)
                maximum = Mathf.Max(maximum, Quaternion.Angle(
                    RequirePath(target.transform, item.Key).localRotation, item.Value));
            return maximum;
        }

        internal static Texture2D CaptureTargetPanel(GameObject target, bool closeUp)
        {
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            if (closeUp)
            {
                var buttonBounds = new Bounds(
                    ButtonWorld(prop), new Vector3(0.52f, 0.52f, 0.52f));
                return CaptureActualScene(
                    buttonBounds, prop.up.normalized, target.transform.up, 0.22f);
            }
            Bounds bounds;
            Transform armature = target.transform.Find("Armature") ??
                throw new InvalidOperationException("Armature is missing.");
            Transform[] bones = armature.GetComponentsInChildren<Transform>(true);
            bounds = new Bounds(bones[0].position, Vector3.zero);
            foreach (Transform bone in bones) bounds.Encapsulate(bone.position);
            foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.18f);
            return CaptureActualScene(
                bounds,
                (target.transform.forward - target.transform.right * 0.68f).normalized,
                target.transform.up,
                4f);
        }

        internal static GameObject CreateContinuousReviewCamera(GameObject target)
        {
            Scene scene = RequireScene();
            Bounds bounds = RendererBounds(target);
            var root = new GameObject("ElectricMineActivate_ContinuousReviewCamera");
            SceneManager.MoveGameObjectToScene(root, scene);
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(root.transform, false);
            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(root.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(
                bounds.extents.y,
                Mathf.Max(bounds.extents.x, bounds.extents.z)) * 1.18f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
            camera.depth = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            Vector3 viewDirection = (
                target.transform.forward - target.transform.right * 0.68f).normalized;
            Vector3 cameraUp = Vector3.ProjectOnPlane(
                target.transform.up, viewDirection).normalized;
            cameraObject.transform.SetPositionAndRotation(
                bounds.center + viewDirection * 4f,
                Quaternion.LookRotation(-viewDirection, cameraUp));
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            return root;
        }

        internal static void WriteContinuousCaptureReport(string value)
        {
            Directory.CreateDirectory(Absolute(TempFolder));
            File.WriteAllText(
                Absolute(ContinuousCaptureReportPath), value, new UTF8Encoding(false));
        }

        internal static void ComposeReview(IReadOnlyList<Texture2D> panels)
        {
            if (panels.Count != 12 || panels.Any(panel => panel == null))
                throw new InvalidOperationException("Review requires twelve panels.");
            int width = PanelSize * 6 + PanelGap * 5;
            int height = PanelSize * 2 + PanelGap;
            var output = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                output.SetPixels32(Enumerable.Repeat(
                    new Color32(5, 7, 10, 255), width * height).ToArray());
                for (int index = 0; index < panels.Count; index++)
                {
                    int column = index % 6;
                    int row = index < 6 ? 1 : 0;
                    output.SetPixels32(
                        column * (PanelSize + PanelGap),
                        row * (PanelSize + PanelGap),
                        PanelSize,
                        PanelSize,
                        panels[index].GetPixels32());
                }
                output.Apply(false, false);
                Directory.CreateDirectory(Absolute(TempFolder));
                File.WriteAllBytes(ReviewAbsolutePath, output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output);
            }
        }

        internal static void WriteReviewReport(string value)
        {
            Directory.CreateDirectory(Absolute(TempFolder));
            File.WriteAllText(Absolute(ReviewReportPath), value, new UTF8Encoding(false));
        }

        internal static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }

        private static AnimationClip CreateClip(
            GameObject target,
            IReadOnlyDictionary<string, Quaternion> startPose,
            IReadOnlyDictionary<string, Quaternion> pressPose)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            clip.name = "ElectricMine_Activate_ButtonPress";
            clip.frameRate = SampleRate;
            clip.wrapMode = WrapMode.Loop;
            int frameCount = Mathf.RoundToInt(DurationSeconds * CurveKeyRate);
            IReadOnlyList<Dictionary<string, Quaternion>> framePoses =
                BuildCollisionFreeFramePoses(
                    target, startPose, pressPose, frameCount);
            PoseContinuityMetrics generatedContinuity =
                MeasurePoseContinuity(framePoses);
            if (generatedContinuity.MaximumAdjacentRotationDegrees >
                MaximumAdjacentLeftArmRotationDegrees)
                throw new InvalidOperationException(
                    "Generated left-arm path is discontinuous: bone=" +
                    generatedContinuity.MaximumRotationPath +
                    ", from=" + F(generatedContinuity.MaximumRotationFromTime) +
                    ", to=" + F(generatedContinuity.MaximumRotationToTime) +
                    ", degrees=" +
                    F(generatedContinuity.MaximumAdjacentRotationDegrees));
            if (generatedContinuity.MaximumRotationAccelerationDegrees >
                MaximumAdjacentRotationAccelerationDegrees)
                throw new InvalidOperationException(
                    "Generated left-arm path changes angular velocity abruptly: bone=" +
                    generatedContinuity.MaximumAccelerationPath +
                    ", at=" + F(generatedContinuity.MaximumAccelerationTime) +
                    ", degrees=" +
                    F(generatedContinuity.MaximumRotationAccelerationDegrees));
            foreach (KeyValuePair<string, Quaternion> item in startPose)
            {
                var x = new AnimationCurve();
                var y = new AnimationCurve();
                var z = new AnimationCurve();
                var w = new AnimationCurve();
                Quaternion previous = Normalize(item.Value);
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    float time = frame / (float)CurveKeyRate;
                    Quaternion rotation = Normalize(framePoses[frame][item.Key]);
                    if (Quaternion.Dot(previous, rotation) < 0f)
                        rotation = new Quaternion(
                            -rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    previous = rotation;
                    x.AddKey(time, rotation.x);
                    y.AddKey(time, rotation.y);
                    z.AddKey(time, rotation.z);
                    w.AddKey(time, rotation.w);
                }
                SetLinear(x);
                SetLinear(y);
                SetLinear(z);
                SetLinear(w);
                SetRotationCurve(clip, item.Key, "x", x);
                SetRotationCurve(clip, item.Key, "y", y);
                SetRotationCurve(clip, item.Key, "z", z);
                SetRotationCurve(clip, item.Key, "w", w);
            }
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = DurationSeconds;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static IReadOnlyList<Dictionary<string, Quaternion>>
            BuildCollisionFreeFramePoses(
                GameObject target,
                IReadOnlyDictionary<string, Quaternion> startPose,
                IReadOnlyDictionary<string, Quaternion> pressPose,
                int frameCount)
        {
            GameObject clone = UnityEngine.Object.Instantiate(target);
            clone.name = "ElectricMineActivate_AnatomicalPathSolver";
            SetHideFlagsRecursively(clone, HideFlags.HideAndDontSave);
            clone.SetActive(false);
            try
            {
                Animator animator = clone.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;
                Transform prop = ElectricMineSetupTools.RequireRuntimeProp(clone);
                int reachFrameCount = Mathf.RoundToInt(
                    ReachSeconds * CurveKeyRate);
                int contactFrameCount = Mathf.RoundToInt(
                    ContactSeconds * CurveKeyRate);
                int returnFrameCount = Mathf.RoundToInt(
                    ReturnSeconds * CurveKeyRate);
                if (reachFrameCount + contactFrameCount + returnFrameCount !=
                    frameCount)
                    throw new InvalidOperationException(
                        "Button press frame partition differs from clip duration.");

                RestoreArmaturePose(clone, startPose);
                Transform upper = RequirePath(clone.transform, LeftArmPath);
                Transform fore = RequirePath(clone.transform, LeftForeArmPath);
                Transform hand = RequirePath(clone.transform, LeftHandPath);
                Transform rightUpper = RequirePath(clone.transform, RightUpperArmPath);
                Transform spineUpper = RequirePath(clone.transform, SpineUpperPath);
                Vector3 startHandPosition = hand.position;
                Quaternion startHandRotation = hand.rotation;
                Vector3 startPole = PoleDirection(upper.position, fore.position,
                    startHandPosition);
                Vector3 up = clone.transform.up.normalized;
                Vector3 outward = Vector3.ProjectOnPlane(
                    upper.position - rightUpper.position, up).normalized;
                if (outward.sqrMagnitude < 0.000001f)
                    outward = clone.transform.right;
                Vector3 front = Vector3.ProjectOnPlane(
                    prop.position - spineUpper.position, up).normalized;
                if (front.sqrMagnitude < 0.000001f)
                    front = clone.transform.forward;

                RestoreArmaturePose(clone, pressPose);
                upper = RequirePath(clone.transform, LeftArmPath);
                fore = RequirePath(clone.transform, LeftForeArmPath);
                hand = RequirePath(clone.transform, LeftHandPath);
                Vector3 pressHandPosition = hand.position;
                Quaternion pressHandRotation = hand.rotation;
                Vector3 pressPole = PoleDirection(upper.position, fore.position,
                    pressHandPosition);
                Vector3 surfaceNormal = prop.up.normalized;

                IReadOnlyList<Dictionary<string, Quaternion>> reachPoses = null;
                float minimumTorsoClearance = float.MinValue;
                float minimumMineClearance = float.MinValue;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    float frontClearance = 0.19f + attempt * 0.025f;
                    float outwardClearance = 0.065f + attempt * 0.0125f;
                    Vector3 towardShoulder = (
                        upper.position - startHandPosition).normalized;
                    Vector3 firstControl = startHandPosition +
                        towardShoulder * 0.105f + front * frontClearance +
                        outward * outwardClearance * 0.35f + up * 0.025f;
                    Vector3 secondControl = pressHandPosition +
                        surfaceNormal * (0.155f + attempt * 0.01f) +
                        outward * outwardClearance;
                    Vector3 safePole = (
                        outward * 1.15f + front * 0.75f + up * 0.10f).normalized;
                    if (Vector3.Dot(startPole, safePole) < 0f)
                        safePole = -safePole;
                    if (Vector3.Dot(safePole, pressPole) < 0f &&
                        Vector3.Dot(startPole, pressPole) >= 0f)
                        safePole = (startPole + pressPole).normalized;
                    var candidate = new List<Dictionary<string, Quaternion>>(
                        reachFrameCount + 1);
                    minimumTorsoClearance = float.MaxValue;
                    minimumMineClearance = float.MaxValue;
                    for (int frame = 0; frame <= reachFrameCount; frame++)
                    {
                        if (frame == 0)
                            RestoreArmaturePose(clone, startPose);
                        else
                        {
                            float progress = MinimumJerk01(
                                frame / (float)reachFrameCount);
                            RestoreArmaturePose(clone, candidate[frame - 1]);
                            foreach (string path in LeftPosePaths.Skip(4))
                            {
                                Quaternion start = startPose[path];
                                Quaternion end = pressPose[path];
                                if (Quaternion.Dot(start, end) < 0f)
                                    end = new Quaternion(
                                        -end.x, -end.y, -end.z, -end.w);
                                RequirePath(clone.transform, path).localRotation =
                                    Normalize(Quaternion.SlerpUnclamped(
                                        start, end, progress));
                            }
                            upper = RequirePath(clone.transform, LeftArmPath);
                            fore = RequirePath(clone.transform, LeftForeArmPath);
                            hand = RequirePath(clone.transform, LeftHandPath);
                            Vector3 handTarget = CubicBezier(
                                startHandPosition, firstControl, secondControl,
                                pressHandPosition, progress);
                            float bendInset = 0.028f * Mathf.Sin(
                                Mathf.PI * progress);
                            handTarget = LimitHandTargetForElbowBend(
                                upper, fore, hand, handTarget, bendInset);
                            Vector3 pole = QuadraticDirection(
                                startPole, safePole, pressPole, progress);
                            SolveTwoBoneWithPole(
                                upper, fore, hand, handTarget, pole);
                            hand.rotation = Quaternion.Slerp(
                                startHandRotation, pressHandRotation, progress);
                        }
                        minimumMineClearance = Mathf.Min(
                            minimumMineClearance,
                            MeasureNonIndexSurfaceClearance(clone, prop));
                        minimumTorsoClearance = Mathf.Min(
                            minimumTorsoClearance,
                            MeasureLeftArmTorsoClearance(clone));
                        candidate.Add(CaptureArmaturePose(clone));
                    }
                    if (minimumMineClearance < RequiredHandSurfaceClearanceMeters ||
                        minimumTorsoClearance < RequiredTorsoClearanceMeters)
                        continue;
                    IReadOnlyList<Dictionary<string, Quaternion>> processed =
                        RetimePosePathByAngularDistance(clone, prop, candidate);
                    processed = SmoothPosePath(processed, 72, 0.78f);
                    processed = LimitPosePathSteps(processed, 2.55f, 24);
                    processed = SmoothPosePath(processed, 28, 0.58f);
                    float processedMineClearance = float.MaxValue;
                    float processedTorsoClearance = float.MaxValue;
                    foreach (Dictionary<string, Quaternion> pose in processed)
                    {
                        RestoreArmaturePose(clone, pose);
                        processedMineClearance = Mathf.Min(
                            processedMineClearance,
                            MeasureNonIndexSurfaceClearance(clone, prop));
                        processedTorsoClearance = Mathf.Min(
                            processedTorsoClearance,
                            MeasureLeftArmTorsoClearance(clone));
                    }
                    reachPoses = processed;
                    minimumMineClearance = processedMineClearance;
                    minimumTorsoClearance = processedTorsoClearance;
                    if (minimumMineClearance >= RequiredHandSurfaceClearanceMeters &&
                        minimumTorsoClearance >= RequiredTorsoClearanceMeters)
                        break;
                }
                if (minimumMineClearance < RequiredHandSurfaceClearanceMeters)
                    throw new InvalidOperationException(
                        "Anatomical reach path intersects the electric mine: " +
                        F(minimumMineClearance));
                if (minimumTorsoClearance < RequiredTorsoClearanceMeters)
                    throw new InvalidOperationException(
                        "Anatomical reach path intersects the torso envelope: " +
                        F(minimumTorsoClearance));

                var result = new List<Dictionary<string, Quaternion>>(frameCount + 1);
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    Dictionary<string, Quaternion> pose;
                    if (frame <= reachFrameCount)
                        pose = reachPoses[frame];
                    else if (frame <= reachFrameCount + contactFrameCount)
                        pose = reachPoses[reachFrameCount];
                    else
                    {
                        float returnProgress = (frame - reachFrameCount -
                            contactFrameCount) / (float)returnFrameCount;
                        float reachIndex = Mathf.Clamp(
                            (1f - ReturnProgress01(returnProgress)) * reachFrameCount,
                            0f, reachFrameCount);
                        pose = SamplePoseSequence(reachPoses, reachIndex);
                    }
                    result.Add(new Dictionary<string, Quaternion>(
                        pose, StringComparer.Ordinal));
                }
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static Vector3 PoleDirection(
            Vector3 root,
            Vector3 joint,
            Vector3 end)
        {
            Vector3 direction = (end - root).normalized;
            Vector3 pole = Vector3.ProjectOnPlane(joint - root, direction);
            return pole.sqrMagnitude < 0.000001f ? Vector3.up : pole.normalized;
        }

        private static Vector3 CubicBezier(
            Vector3 start,
            Vector3 firstControl,
            Vector3 secondControl,
            Vector3 end,
            float progress)
        {
            float inverse = 1f - progress;
            return inverse * inverse * inverse * start +
                3f * inverse * inverse * progress * firstControl +
                3f * inverse * progress * progress * secondControl +
                progress * progress * progress * end;
        }

        private static Vector3 QuadraticDirection(
            Vector3 start,
            Vector3 control,
            Vector3 end,
            float progress)
        {
            Vector3 first = Vector3.Slerp(start, control, progress);
            Vector3 second = Vector3.Slerp(control, end, progress);
            Vector3 value = Vector3.Slerp(first, second, progress);
            return value.sqrMagnitude < 0.000001f ? control : value.normalized;
        }

        private static Vector3 LimitHandTargetForElbowBend(
            Transform upper,
            Transform fore,
            Transform hand,
            Vector3 target,
            float extensionInset)
        {
            Vector3 requested = target - upper.position;
            float maximum = Vector3.Distance(upper.position, fore.position) +
                Vector3.Distance(fore.position, hand.position) -
                Mathf.Max(0.00005f, extensionInset);
            if (requested.sqrMagnitude <= maximum * maximum) return target;
            return upper.position + requested.normalized * maximum;
        }

        private static void PushLeftHandOutsideMine(
            GameObject root,
            Transform prop)
        {
            Transform upper = RequirePath(root.transform, LeftArmPath);
            Transform fore = RequirePath(root.transform, LeftForeArmPath);
            Transform hand = RequirePath(root.transform, LeftHandPath);
            for (int iteration = 0; iteration < 24; iteration++)
            {
                float clearance = MeasureNonIndexSurfaceClearance(root, prop);
                if (clearance >= SolvedPoseSurfaceClearanceMeters)
                    return;
                float correction = Mathf.Max(
                    0.006f,
                    (SolvedPoseSurfaceClearanceMeters - clearance) * 1.5f +
                        0.002f);
                Quaternion handWorldRotation = hand.rotation;
                Vector3 surfaceNormal = prop.up.normalized;
                Vector3 lateral = Vector3.ProjectOnPlane(
                    hand.position - ButtonWorld(prop), surfaceNormal).normalized;
                if (lateral.sqrMagnitude < 0.000001f)
                    lateral = Vector3.ProjectOnPlane(
                        upper.position - ButtonWorld(prop), surfaceNormal).normalized;
                SolveTwoBone(
                    upper, fore, hand,
                    hand.position + surfaceNormal * correction +
                        lateral * correction * 1.5f);
                hand.rotation = handWorldRotation;
            }
        }

        private static IReadOnlyList<Dictionary<string, Quaternion>>
            RetimePosePathByAngularDistance(
                GameObject clone,
                Transform prop,
                IReadOnlyList<Dictionary<string, Quaternion>> source)
        {
            int sourceLast = source.Count - 1;
            var cumulativeDistance = new float[source.Count];
            for (int index = 1; index <= sourceLast; index++)
                cumulativeDistance[index] = cumulativeDistance[index - 1] +
                    Mathf.Max(0.15f, PoseSegmentDistance(
                        source[index - 1], source[index]));
            float totalDistance = cumulativeDistance[sourceLast];
            if (totalDistance <= 0.0001f) return source;
            var retimed = new List<Dictionary<string, Quaternion>>(source.Count);
            int sourceUpper = 1;
            for (int frame = 0; frame <= sourceLast; frame++)
            {
                float targetDistance = totalDistance *
                    MinimumJerk01(frame / (float)sourceLast);
                while (sourceUpper < sourceLast &&
                    cumulativeDistance[sourceUpper] < targetDistance)
                    sourceUpper++;
                int sourceLower = Mathf.Max(0, sourceUpper - 1);
                float segmentDistance = cumulativeDistance[sourceUpper] -
                    cumulativeDistance[sourceLower];
                float progress = segmentDistance <= 0.000001f
                    ? 0f
                    : (targetDistance - cumulativeDistance[sourceLower]) /
                        segmentDistance;
                Dictionary<string, Quaternion> pose = InterpolatePose(
                    source[sourceLower], source[sourceUpper], progress);
                retimed.Add(pose);
            }
            return retimed;
        }

        private static IReadOnlyList<Dictionary<string, Quaternion>> SmoothPosePath(
            IReadOnlyList<Dictionary<string, Quaternion>> source,
            int iterations,
            float strength)
        {
            var current = source.Select(pose =>
                new Dictionary<string, Quaternion>(pose, StringComparer.Ordinal)).ToList();
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var next = current.Select(pose =>
                    new Dictionary<string, Quaternion>(pose, StringComparer.Ordinal)).ToList();
                for (int frame = 1; frame < current.Count - 1; frame++)
                foreach (string path in LeftPosePaths.Skip(1))
                {
                    Quaternion neighbors = Quaternion.Slerp(
                        current[frame - 1][path], current[frame + 1][path], 0.5f);
                    float normalized = frame / (float)(current.Count - 1);
                    float taper = Mathf.Sin(Mathf.PI * normalized);
                    float weightedStrength = strength * taper * taper;
                    next[frame][path] = Normalize(Quaternion.Slerp(
                        current[frame][path], neighbors, weightedStrength));
                }
                current = next;
            }
            return current;
        }

        private static IReadOnlyList<Dictionary<string, Quaternion>> LimitPosePathSteps(
            IReadOnlyList<Dictionary<string, Quaternion>> source,
            float maximumStepDegrees,
            int iterations)
        {
            var result = source.Select(pose =>
                new Dictionary<string, Quaternion>(pose, StringComparer.Ordinal)).ToList();
            for (int iteration = 0; iteration < iterations; iteration++)
            foreach (string path in LeftPosePaths.Skip(1))
            {
                for (int frame = 1; frame < result.Count - 1; frame++)
                    result[frame][path] = Normalize(Quaternion.RotateTowards(
                        result[frame - 1][path], result[frame][path],
                        maximumStepDegrees));
                for (int frame = result.Count - 2; frame >= 1; frame--)
                    result[frame][path] = Normalize(Quaternion.RotateTowards(
                        result[frame + 1][path], result[frame][path],
                        maximumStepDegrees));
            }
            return result;
        }

        private static float PoseSegmentDistance(
            IReadOnlyDictionary<string, Quaternion> from,
            IReadOnlyDictionary<string, Quaternion> to)
        {
            float maximum = 0f;
            foreach (string path in LeftPosePaths)
                maximum = Mathf.Max(maximum, Quaternion.Angle(
                    from[path], to[path]));
            return maximum;
        }

        private static Dictionary<string, Quaternion> SamplePoseSequence(
            IReadOnlyList<Dictionary<string, Quaternion>> source,
            float sourceIndex)
        {
            int lower = Mathf.Clamp(
                Mathf.FloorToInt(sourceIndex), 0, source.Count - 1);
            int upper = Mathf.Clamp(lower + 1, 0, source.Count - 1);
            return InterpolatePose(
                source[lower], source[upper], sourceIndex - lower);
        }

        private static Dictionary<string, Quaternion> InterpolatePose(
            IReadOnlyDictionary<string, Quaternion> from,
            IReadOnlyDictionary<string, Quaternion> to,
            float progress)
        {
            var result = new Dictionary<string, Quaternion>(
                from.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, Quaternion> item in from)
            {
                Quaternion start = Normalize(item.Value);
                Quaternion end = Normalize(to[item.Key]);
                if (Quaternion.Dot(start, end) < 0f)
                    end = new Quaternion(-end.x, -end.y, -end.z, -end.w);
                result.Add(item.Key, Normalize(Quaternion.SlerpUnclamped(
                    start, end, progress)));
            }
            return result;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState state = machine.AddState("ElectricMineActivateButtonPress");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static PressPoseResult BuildPressPoseOnClone(GameObject target)
        {
            GameObject clone = UnityEngine.Object.Instantiate(target);
            clone.name = "ElectricMineActivate_PressPoseSolver";
            SetHideFlagsRecursively(clone, HideFlags.HideAndDontSave);
            clone.SetActive(false);
            try
            {
                Animator animator = clone.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;
                Transform prop = ElectricMineSetupTools.RequireRuntimeProp(clone);
                Vector3 contact = ButtonWorld(prop);
                Dictionary<string, Quaternion> start = CaptureLeftPose(clone);
                Dictionary<string, Quaternion> baseline = CaptureArmaturePose(clone);
                PressPoseResult best = default;
                bool hasContactCandidate = false;
                for (int attempt = 0; attempt < WristSurfaceOffsetAttempts; attempt++)
                {
                    RestoreArmaturePose(clone, baseline);
                    float wristOffset = InitialWristSurfaceOffsetMeters +
                        attempt * WristSurfaceOffsetStepMeters;
                    SolvePressPose(clone, contact, wristOffset);
                    float contactDistance = Vector3.Distance(
                        EstimateIndexTip(clone), contact);
                    float clearance = MeasureNonIndexSurfaceClearance(clone, prop);
                    if (contactDistance > ContactToleranceMeters)
                        continue;
                    Transform upper = RequirePath(clone.transform, LeftArmPath);
                    Transform fore = RequirePath(clone.transform, LeftForeArmPath);
                    Transform hand = RequirePath(clone.transform, LeftHandPath);
                    var candidate = new PressPoseResult(
                        CaptureArmaturePose(clone),
                        contactDistance,
                        ElbowBend(upper, fore, hand),
                        Quaternion.Angle(start[LeftHandPath], hand.localRotation),
                        clearance,
                        wristOffset);
                    if (!hasContactCandidate ||
                        candidate.MinimumNonIndexSurfaceClearance >
                            best.MinimumNonIndexSurfaceClearance)
                    {
                        best = candidate;
                        hasContactCandidate = true;
                    }
                    if (clearance >= SolvedPoseSurfaceClearanceMeters)
                        return candidate;
                }
                if (!hasContactCandidate)
                    throw new InvalidOperationException(
                        "IK solver did not reach the red button without moving the prop.");
                throw new InvalidOperationException(
                    "IK solver reached the button but the left hand still intersects the mine. " +
                    "bestClearance=" + F(best.MinimumNonIndexSurfaceClearance) +
                    ", contactDistance=" + F(best.ContactDistance) +
                    ", wristSurfaceOffset=" + F(best.WristSurfaceOffset));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static void SolvePressPose(
            GameObject root,
            Vector3 contact,
            float wristSurfaceOffset)
        {
            Transform upper = RequirePath(root.transform, LeftArmPath);
            Transform fore = RequirePath(root.transform, LeftForeArmPath);
            Transform hand = RequirePath(root.transform, LeftHandPath);
            Transform proximal = RequirePath(root.transform, LeftIndexProximalPath);
            Transform intermediate = RequirePath(root.transform, LeftIndexIntermediatePath);
            Transform distal = RequirePath(root.transform, LeftIndexDistalPath);
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(root);
            Vector3 surfaceNormal = prop.up.normalized;
            Vector3 lateral = Vector3.ProjectOnPlane(
                hand.position - contact, surfaceNormal).normalized;
            if (lateral.sqrMagnitude < 0.000001f)
                lateral = Vector3.ProjectOnPlane(
                    upper.position - contact, surfaceNormal).normalized;
            float handToTip = Vector3.Distance(hand.position, proximal.position) +
                Vector3.Distance(proximal.position, intermediate.position) +
                Vector3.Distance(intermediate.position, distal.position) +
                IndexTipLength(intermediate, distal);
            float usableReach = handToTip * 0.995f;
            float lateralReach = Mathf.Sqrt(Mathf.Max(
                0.0001f,
                usableReach * usableReach -
                    wristSurfaceOffset * wristSurfaceOffset));
            Vector3 wristTarget = contact + lateral * lateralReach +
                surfaceNormal * wristSurfaceOffset;
            SolveTwoBone(upper, fore, hand, wristTarget);
            Vector3 fingerDirection = (distal.position - proximal.position).normalized;
            Vector3 desiredFingerDirection = (contact - hand.position).normalized;
            hand.rotation = Quaternion.FromToRotation(
                fingerDirection, desiredFingerDirection) * hand.rotation;
            AlignPalmParallelToSurface(
                hand, proximal, surfaceNormal, desiredFingerDirection);
            Quaternion proximalBase = proximal.localRotation;
            Quaternion intermediateBase = intermediate.localRotation;
            Quaternion distalBase = distal.localRotation;
            Quaternion handBase = hand.localRotation;
            for (int iteration = 0; iteration < 64; iteration++)
            {
                RotateToward(distal, root, contact, distalBase, 45f, 1f);
                RotateToward(intermediate, root, contact, intermediateBase, 55f, 0.90f);
                RotateToward(proximal, root, contact, proximalBase, 35f, 0.78f);
                RotateToward(hand, root, contact, handBase, 18f, 0.22f);
                if (Vector3.Distance(EstimateIndexTip(root), contact) < 0.0025f)
                    break;
            }
            CurlOtherFingers(hand);
        }

        private static void AlignPalmParallelToSurface(
            Transform hand,
            Transform indexProximal,
            Vector3 surfaceNormal,
            Vector3 fingerDirection)
        {
            Transform littleProximal = hand.Find("LeftLittleProximal") ??
                throw new InvalidOperationException("LeftLittleProximal is missing.");
            Vector3 currentAcross = Vector3.ProjectOnPlane(
                littleProximal.position - indexProximal.position,
                fingerDirection).normalized;
            Vector3 desiredAcross = Vector3.Cross(
                surfaceNormal, fingerDirection).normalized;
            if (Vector3.Dot(currentAcross, desiredAcross) < 0f)
                desiredAcross = -desiredAcross;
            float twist = Vector3.SignedAngle(
                currentAcross, desiredAcross, fingerDirection);
            hand.rotation = Quaternion.AngleAxis(twist, fingerDirection) * hand.rotation;
        }

        private static void RestoreArmaturePose(
            GameObject root,
            IReadOnlyDictionary<string, Quaternion> pose)
        {
            foreach (KeyValuePair<string, Quaternion> item in pose)
                RequirePath(root.transform, item.Key).localRotation = item.Value;
        }

        private static void CurlOtherFingers(Transform hand)
        {
            CurlFinger(hand, "LeftMiddleProximal", "LeftMiddleIntermediate",
                "LeftMiddleDistal", 48f, 62f, 38f, 0.34f);
            CurlFinger(hand, "LeftRingProximal", "LeftRingIntermediate",
                "LeftRingDistal", 52f, 68f, 42f, 0.32f);
            CurlFinger(hand, "LeftLittleProximal", "LeftLittleIntermediate",
                "LeftLittleDistal", 56f, 72f, 46f, 0.30f);
            CurlFinger(hand, "LeftThumbProximal", "LeftThumbIntermediate",
                "LeftThumbDistal", 34f, 46f, 28f, 0.48f);
        }

        private static void CurlFinger(
            Transform hand,
            string proximalName,
            string intermediateName,
            string distalName,
            float proximalDegrees,
            float intermediateDegrees,
            float distalDegrees,
            float palmFraction)
        {
            Transform proximal = hand.Find(proximalName) ??
                throw new InvalidOperationException(proximalName + " is missing.");
            Transform intermediate = proximal.Find(intermediateName) ??
                throw new InvalidOperationException(intermediateName + " is missing.");
            Transform distal = intermediate.Find(distalName) ??
                throw new InvalidOperationException(distalName + " is missing.");
            Vector3 palmTarget = Vector3.Lerp(hand.position, proximal.position, palmFraction);
            BendSegmentToward(proximal, intermediate, palmTarget, proximalDegrees);
            BendSegmentToward(intermediate, distal, palmTarget, intermediateDegrees);
            Vector3 segment = (distal.position - intermediate.position).normalized;
            Vector3 desired = (palmTarget - distal.position).normalized;
            Vector3 axis = Vector3.Cross(segment, desired);
            if (axis.sqrMagnitude > 0.000001f)
                distal.rotation = Quaternion.AngleAxis(
                    distalDegrees, axis.normalized) * distal.rotation;
        }

        private static void BendSegmentToward(
            Transform joint,
            Transform child,
            Vector3 target,
            float maximumDegrees)
        {
            Vector3 current = (child.position - joint.position).normalized;
            Vector3 desired = (target - joint.position).normalized;
            Quaternion delta = Quaternion.FromToRotation(current, desired);
            joint.rotation = Quaternion.RotateTowards(
                joint.rotation, delta * joint.rotation, maximumDegrees);
        }

        private static void SolveTwoBone(
            Transform upper,
            Transform fore,
            Transform hand,
            Vector3 requestedGoal)
        {
            Vector3 direction = (requestedGoal - upper.position).normalized;
            Vector3 pole = Vector3.ProjectOnPlane(
                fore.position - upper.position, direction);
            if (pole.sqrMagnitude < 0.000001f)
                pole = Vector3.ProjectOnPlane(upper.root.up, direction);
            SolveTwoBoneWithPole(upper, fore, hand, requestedGoal, pole);
        }

        private static void SolveTwoBoneWithPole(
            Transform upper,
            Transform fore,
            Transform hand,
            Vector3 requestedGoal,
            Vector3 stablePoleDirection)
        {
            Vector3 root = upper.position;
            Vector3 joint = fore.position;
            Vector3 end = hand.position;
            float upperLength = Vector3.Distance(root, joint);
            float lowerLength = Vector3.Distance(joint, end);
            Vector3 requested = requestedGoal - root;
            const float singularityMargin = 0.00005f;
            float minimum = Mathf.Abs(upperLength - lowerLength) +
                singularityMargin;
            float maximum = upperLength + lowerLength - singularityMargin;
            float distance = Mathf.Clamp(requested.magnitude, minimum, maximum);
            Vector3 direction = requested.normalized;
            Vector3 goal = root + direction * distance;
            Vector3 pole = Vector3.ProjectOnPlane(
                stablePoleDirection, direction);
            if (pole.sqrMagnitude < 0.000001f)
                pole = Vector3.ProjectOnPlane(joint - root, direction);
            if (pole.sqrMagnitude < 0.000001f)
                pole = Vector3.ProjectOnPlane(upper.root.up, direction);
            pole.Normalize();
            float along = (upperLength * upperLength + distance * distance -
                lowerLength * lowerLength) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(
                0f, upperLength * upperLength - along * along));
            Vector3 desiredJoint = root + direction * along + pole * height;
            upper.rotation = Quaternion.FromToRotation(
                joint - root, desiredJoint - root) * upper.rotation;
            joint = fore.position;
            end = hand.position;
            fore.rotation = Quaternion.FromToRotation(
                end - joint, goal - joint) * fore.rotation;
        }

        private static void RotateToward(
            Transform bone,
            GameObject root,
            Vector3 target,
            Quaternion referenceLocal,
            float maximumLocalDelta,
            float fraction)
        {
            Vector3 tip = EstimateIndexTip(root);
            Vector3 from = tip - bone.position;
            Vector3 to = target - bone.position;
            if (from.sqrMagnitude < 0.0000001f || to.sqrMagnitude < 0.0000001f)
                return;
            Quaternion delta = Quaternion.FromToRotation(from, to);
            bone.rotation = Quaternion.SlerpUnclamped(
                Quaternion.identity, delta, fraction) * bone.rotation;
            bone.localRotation = Quaternion.RotateTowards(
                referenceLocal, bone.localRotation, maximumLocalDelta);
        }

        private static SampleMetrics SampleOnClone(
            GameObject target,
            AnimationClip clip,
            float time)
        {
            Dictionary<string, Quaternion> startLeft = CaptureLeftPose(target);
            Dictionary<string, Quaternion> startRight = CaptureRightPose(target);
            Transform sourceProp = ElectricMineSetupTools.RequireRuntimeProp(target);
            Vector3 sourcePropPosition = sourceProp.localPosition;
            Quaternion sourcePropRotation = sourceProp.localRotation;
            Vector3 sourcePropScale = sourceProp.localScale;
            GameObject clone = UnityEngine.Object.Instantiate(target);
            clone.name = "ElectricMineActivate_ClipSample";
            SetHideFlagsRecursively(clone, HideFlags.HideAndDontSave);
            clone.SetActive(false);
            try
            {
                Animator animator = clone.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;
                clip.SampleAnimation(clone, Mathf.Clamp(time, 0f, DurationSeconds));
                float leftDeviation = 0f;
                foreach (KeyValuePair<string, Quaternion> item in startLeft)
                    leftDeviation = Mathf.Max(leftDeviation, Quaternion.Angle(
                        RequirePath(clone.transform, item.Key).localRotation, item.Value));
                float rightDeviation = 0f;
                foreach (KeyValuePair<string, Quaternion> item in startRight)
                    rightDeviation = Mathf.Max(rightDeviation, Quaternion.Angle(
                        RequirePath(clone.transform, item.Key).localRotation, item.Value));
                Transform prop = ElectricMineSetupTools.RequireRuntimeProp(clone);
                Transform upper = RequirePath(clone.transform, LeftArmPath);
                Transform fore = RequirePath(clone.transform, LeftForeArmPath);
                Transform hand = RequirePath(clone.transform, LeftHandPath);
                return new SampleMetrics(
                    Vector3.Distance(EstimateIndexTip(clone), ButtonWorld(prop)),
                    ElbowBend(upper, fore, hand),
                    Quaternion.Angle(startLeft[LeftHandPath], hand.localRotation),
                    MeasureNonIndexSurfaceClearance(clone, prop),
                    MeasureLeftArmTorsoClearance(clone),
                    leftDeviation,
                    rightDeviation,
                    Vector3.Distance(prop.localPosition, sourcePropPosition),
                    Quaternion.Angle(prop.localRotation, sourcePropRotation),
                    Vector3.Distance(prop.localScale, sourcePropScale));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static PoseContinuityMetrics MeasurePoseContinuity(
            IReadOnlyList<Dictionary<string, Quaternion>> poses)
        {
            float maximum = 0f;
            string maximumPath = string.Empty;
            int maximumFrame = 0;
            float maximumAcceleration = 0f;
            string maximumAccelerationPath = string.Empty;
            int maximumAccelerationFrame = 0;
            for (int frame = 1; frame < poses.Count; frame++)
            foreach (string path in LeftPosePaths)
            {
                float angle = Quaternion.Angle(
                    poses[frame - 1][path], poses[frame][path]);
                if (angle <= maximum) continue;
                maximum = angle;
                maximumPath = path;
                maximumFrame = frame;
            }
            for (int frame = 2; frame < poses.Count; frame++)
            foreach (string path in LeftPosePaths)
            {
                Quaternion previousDelta = Normalize(
                    Quaternion.Inverse(poses[frame - 2][path]) *
                    poses[frame - 1][path]);
                Quaternion currentDelta = Normalize(
                    Quaternion.Inverse(poses[frame - 1][path]) *
                    poses[frame][path]);
                float acceleration = Quaternion.Angle(previousDelta, currentDelta);
                if (acceleration <= maximumAcceleration) continue;
                maximumAcceleration = acceleration;
                maximumAccelerationPath = path;
                maximumAccelerationFrame = frame;
            }
            return new PoseContinuityMetrics(
                maximum,
                maximumPath,
                (maximumFrame - 1) / (float)CurveKeyRate,
                maximumFrame / (float)CurveKeyRate,
                maximumAcceleration,
                maximumAccelerationPath,
                maximumAccelerationFrame / (float)CurveKeyRate);
        }

        private static PoseContinuityMetrics MeasureClipContinuity(
            GameObject target,
            AnimationClip clip)
        {
            GameObject clone = UnityEngine.Object.Instantiate(target);
            clone.name = "ElectricMineActivate_ContinuitySample";
            SetHideFlagsRecursively(clone, HideFlags.HideAndDontSave);
            clone.SetActive(false);
            try
            {
                Animator animator = clone.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;
                int frameCount = Mathf.RoundToInt(DurationSeconds * CurveKeyRate);
                var poses = new List<Dictionary<string, Quaternion>>(frameCount + 1);
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    clip.SampleAnimation(clone, frame / (float)CurveKeyRate);
                    poses.Add(CaptureLeftPose(clone));
                }
                return MeasurePoseContinuity(poses);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static Dictionary<string, Quaternion> CaptureArmaturePose(GameObject target)
        {
            Transform armature = target.transform.Find("Armature") ??
                throw new InvalidOperationException("Armature is missing.");
            return armature.GetComponentsInChildren<Transform>(true)
                .Where(item => !HasAncestorNamed(item, PropName))
                .ToDictionary(
                    item => RelativePath(target.transform, item),
                    item => item.localRotation,
                    StringComparer.Ordinal);
        }

        private static float MotionWeight(float time)
        {
            if (time <= ReachSeconds)
                return Smooth01(time / ReachSeconds);
            if (time <= ReachSeconds + ContactSeconds)
                return 1f;
            return Smooth01((DurationSeconds - time) / ReturnSeconds);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float MinimumJerk01(float value)
        {
            value = Mathf.Clamp01(value);
            float squared = value * value;
            float cubed = squared * value;
            return cubed * (10f - 15f * value + 6f * squared);
        }

        private static float GentleEase01(float value)
        {
            const float easeFraction = 0.1f;
            value = Mathf.Clamp01(value);
            float maximumSlope = 1f / (1f - easeFraction);
            if (value < easeFraction)
                return maximumSlope * value * value /
                    (2f * easeFraction);
            if (value > 1f - easeFraction)
                return 1f - GentleEase01(1f - value);
            return maximumSlope * (value - easeFraction * 0.5f);
        }

        private static float ReturnProgress01(float value)
        {
            const float motionFraction = 0.9f;
            if (value >= motionFraction) return 1f;
            return GentleEase01(value / motionFraction);
        }

        private static void SetRotationCurve(
            AnimationClip clip,
            string path,
            string component,
            AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path, typeof(Transform), "m_LocalRotation." + component),
                curve);
        }

        private static void SetLinear(AnimationCurve curve)
        {
            for (int index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
            }
        }

        private static Vector3 ButtonWorld(Transform prop)
        {
            return prop.TransformPoint(ButtonSurfaceLocal(prop)) +
                prop.up.normalized * ContactClearanceMeters;
        }

        private static Vector3 ButtonSurfaceLocal(Transform prop)
        {
            OrientedBounds bounds = MeasureInPropSpace(prop);
            return new Vector3(
                (bounds.Minimum.x + bounds.Maximum.x) * 0.5f,
                bounds.Maximum.y,
                (bounds.Minimum.z + bounds.Maximum.z) * 0.5f);
        }

        private static OrientedBounds MeasureInPropSpace(Transform prop)
        {
            bool initialized = false;
            Vector3 minimum = Vector3.zero;
            Vector3 maximum = Vector3.zero;
            foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
            {
                Bounds localBounds;
                if (renderer is SkinnedMeshRenderer skinned)
                    localBounds = skinned.localBounds;
                else
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    localBounds = filter.sharedMesh.bounds;
                }
                Vector3 center = localBounds.center;
                Vector3 extents = localBounds.extents;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localCorner = center + Vector3.Scale(
                        extents, new Vector3(x, y, z));
                    Vector3 propLocal = prop.InverseTransformPoint(
                        renderer.transform.TransformPoint(localCorner));
                    if (!initialized)
                    {
                        minimum = maximum = propLocal;
                        initialized = true;
                    }
                    else
                    {
                        minimum = Vector3.Min(minimum, propLocal);
                        maximum = Vector3.Max(maximum, propLocal);
                    }
                }
            }
            if (!initialized)
                throw new InvalidOperationException("Electric mine render bounds are missing.");
            return new OrientedBounds(minimum, maximum);
        }

        private static Vector3 EstimateIndexTip(GameObject target)
        {
            Transform intermediate = RequirePath(
                target.transform, LeftIndexIntermediatePath);
            Transform distal = RequirePath(target.transform, LeftIndexDistalPath);
            return distal.TransformPoint(Vector3.up * IndexTipLength(intermediate, distal));
        }

        private static float IndexTipLength(Transform intermediate, Transform distal)
        {
            return Vector3.Distance(intermediate.position, distal.position) * 0.75f;
        }

        private static float MeasureNonIndexSurfaceClearance(
            GameObject target,
            Transform prop)
        {
            return MeasureNonIndexSurfaceClearance(target, prop, out _);
        }

        private static float MeasureNonIndexSurfaceClearance(
            GameObject target,
            Transform prop,
            out string minimumProbeLabel)
        {
            return MeasureNonIndexSurfaceClearance(
                target, prop, out minimumProbeLabel,
                out _, out _, out _);
        }

        private static float MeasureNonIndexSurfaceClearance(
            GameObject target,
            Transform prop,
            out string minimumProbeLabel,
            out Vector3 minimumProbeLocal,
            out float minimumProbeRadius,
            out OrientedBounds mineBounds)
        {
            Transform hand = RequirePath(target.transform, LeftHandPath);
            OrientedBounds bounds = MeasureInPropSpace(prop);
            mineBounds = bounds;
            float minimum = float.MaxValue;
            minimumProbeLabel = string.Empty;
            minimumProbeLocal = Vector3.zero;
            minimumProbeRadius = 0f;
            var probes = new List<HandProbe>();
            AddProbe(probes, hand.position, 0.042f, "LeftHandWrist");

            string[] fingerRoots =
            {
                "LeftThumbProximal",
                "LeftMiddleProximal",
                "LeftRingProximal",
                "LeftLittleProximal"
            };
            var proximalPositions = new List<Vector3>();
            foreach (string proximalName in fingerRoots)
            {
                Transform proximal = hand.Find(proximalName) ??
                    throw new InvalidOperationException(proximalName + " is missing.");
                Transform intermediate = proximal.GetChild(0);
                Transform distal = intermediate.GetChild(0);
                proximalPositions.Add(proximal.position);
                AddSegmentProbes(
                    probes, hand.position, proximal.position, 0.018f,
                    proximalName + "Palm");
                AddSegmentProbes(
                    probes, proximal.position, intermediate.position, 0.013f,
                    proximalName);
                AddSegmentProbes(
                    probes, intermediate.position, distal.position, 0.011f,
                    intermediate.name);
                Vector3 tip = distal.TransformPoint(
                    Vector3.up * IndexTipLength(intermediate, distal));
                AddSegmentProbes(
                    probes, distal.position, tip, 0.009f, distal.name);
            }
            Vector3 palmCenter = proximalPositions.Aggregate(
                Vector3.zero, (sum, point) => sum + point) /
                proximalPositions.Count;
            AddSegmentProbes(
                probes, hand.position, palmCenter, 0.035f, "LeftPalmCore");

            foreach (HandProbe probe in probes)
            {
                float clearance = SurfaceClearance(
                    prop.InverseTransformPoint(probe.Position),
                    probe.Radius,
                    bounds);
                if (clearance >= minimum) continue;
                minimum = clearance;
                minimumProbeLabel = probe.Label;
                minimumProbeLocal = prop.InverseTransformPoint(probe.Position);
                minimumProbeRadius = probe.Radius;
            }
            return minimum;
        }

        private static void AddProbe(
            ICollection<HandProbe> probes,
            Vector3 position,
            float radius,
            string label)
        {
            probes.Add(new HandProbe(position, radius, label));
        }

        private static void AddSegmentProbes(
            ICollection<HandProbe> probes,
            Vector3 start,
            Vector3 end,
            float radius,
            string label)
        {
            AddProbe(probes, start, radius, label + "Start");
            AddProbe(probes, Vector3.Lerp(start, end, 0.25f), radius,
                label + "Quarter");
            AddProbe(probes, Vector3.Lerp(start, end, 0.5f), radius,
                label + "Middle");
            AddProbe(probes, Vector3.Lerp(start, end, 0.75f), radius,
                label + "ThreeQuarter");
            AddProbe(probes, end, radius, label + "End");
        }

        private static float SurfaceClearance(
            Vector3 point,
            float radius,
            OrientedBounds bounds)
        {
            float outsideX = Mathf.Max(
                bounds.Minimum.x - point.x, point.x - bounds.Maximum.x, 0f);
            float outsideZ = Mathf.Max(
                bounds.Minimum.z - point.z, point.z - bounds.Maximum.z, 0f);
            float planarDistance = Mathf.Sqrt(
                outsideX * outsideX + outsideZ * outsideZ);
            if (planarDistance >= radius)
                return planarDistance - radius;
            float frontRadius = Mathf.Sqrt(Mathf.Max(
                0f, radius * radius - planarDistance * planarDistance));
            return point.y - bounds.Maximum.y - frontRadius;
        }

        private static float MeasureLeftArmTorsoClearance(GameObject root)
        {
            Transform lowerSpine = RequirePath(root.transform, SpineLowerPath);
            Transform upperSpine = RequirePath(root.transform, SpineUpperPath);
            Transform leftUpper = RequirePath(root.transform, LeftArmPath);
            Transform rightUpper = RequirePath(root.transform, RightUpperArmPath);
            Transform fore = RequirePath(root.transform, LeftForeArmPath);
            Transform hand = RequirePath(root.transform, LeftHandPath);
            float shoulderWidth = Vector3.Distance(
                leftUpper.position, rightUpper.position);
            float torsoRadius = Mathf.Clamp(shoulderWidth * 0.36f, 0.115f, 0.19f);
            Vector3 axisStart = Vector3.Lerp(
                lowerSpine.position, upperSpine.position, 0.18f);
            Vector3 axisEnd = upperSpine.position;
            float minimum = float.MaxValue;
            for (int index = 2; index <= 8; index++)
            {
                Vector3 point = Vector3.Lerp(
                    fore.position, hand.position, index / 8f);
                minimum = Mathf.Min(
                    minimum,
                    DistancePointToSegment(point, axisStart, axisEnd) -
                    torsoRadius - 0.052f);
            }
            minimum = Mathf.Min(
                minimum,
                DistancePointToSegment(hand.position, axisStart, axisEnd) -
                torsoRadius - 0.065f);
            return minimum;
        }

        private static float DistancePointToSegment(
            Vector3 point,
            Vector3 start,
            Vector3 end)
        {
            Vector3 segment = end - start;
            float squaredLength = segment.sqrMagnitude;
            if (squaredLength < 0.0000001f)
                return Vector3.Distance(point, start);
            float progress = Mathf.Clamp01(
                Vector3.Dot(point - start, segment) / squaredLength);
            return Vector3.Distance(point, start + segment * progress);
        }

        private static float ElbowBend(
            Transform upper,
            Transform fore,
            Transform hand)
        {
            return 180f - Vector3.Angle(
                upper.position - fore.position,
                hand.position - fore.position);
        }

        private static Texture2D CaptureActualScene(
            Bounds bounds,
            Vector3 viewDirection,
            Vector3 up,
            float cameraDistance)
        {
            Scene scene = RequireScene();
            var cameraObject = new GameObject("ElectricMineActivate_ReadOnlyCamera");
            var lightObject = new GameObject("ElectricMineActivate_ReadOnlyLight");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y,
                    Mathf.Max(bounds.extents.x, bounds.extents.z)) * 1.18f;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(up, viewDirection).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + viewDirection * cameraDistance,
                    Quaternion.LookRotation(-viewDirection, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(
                    PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static string HierarchyTransformSignature(Transform root)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => RelativePath(root, item), StringComparer.Ordinal))
                text.Append(RelativePath(root, item)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(item.gameObject.activeSelf).AppendLine();
            return Sha256Text(text.ToString());
        }

        private static string HierarchySignature(Transform root)
        {
            var text = new StringBuilder(HierarchyTransformSignature(root));
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)
                         .OrderBy(item => RelativePath(root, item.transform),
                             StringComparer.Ordinal))
                text.Append(RelativePath(root, renderer.transform)).Append('|')
                    .Append(renderer.enabled).Append('|')
                    .Append(string.Join(",", renderer.sharedMaterials.Select(material =>
                        material == null ? "<null>" : AssetDatabase.GetAssetPath(material))))
                    .AppendLine();
            return Sha256Text(text.ToString());
        }

        private static string SceneSignatureOutsideTarget(Scene scene)
        {
            var text = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects().OrderBy(item => item.name))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .Where(item => !HasAncestorNamed(item, TargetName))
                         .OrderBy(item => RelativePath(root.transform, item),
                             StringComparer.Ordinal))
                text.Append(root.name).Append('|')
                    .Append(RelativePath(root.transform, item)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(item.gameObject.activeSelf).AppendLine();
            return Sha256Text(text.ToString());
        }

        private static bool HasAncestorNamed(Transform item, string name)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
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
                    "CargoRunMvp must be the active scene. Active=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("This command requires Edit Mode.");
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
                    "Expected one " + name + ", found " + matches.Length + ".");
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject target)
        {
            return target.GetComponent<Animator>() ??
                throw new InvalidOperationException(target.name + " Animator is missing.");
        }

        private static Transform RequirePath(Transform root, string path)
        {
            Transform found = root.Find(path);
            return found ?? throw new InvalidOperationException(
                root.name + " path is missing: " + path);
        }

        private static string RelativePath(Transform root, Transform item)
        {
            return AnimationUtility.CalculateTransformPath(item, root);
        }

        private static void SetHideFlagsRecursively(GameObject root, HideFlags flags)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.hideFlags = flags;
        }

        private static Quaternion Normalize(Quaternion value)
        {
            float magnitude = Mathf.Sqrt(
                value.x * value.x + value.y * value.y +
                value.z * value.z + value.w * value.w);
            if (magnitude < 0.000001f) return Quaternion.identity;
            return new Quaternion(
                value.x / magnitude,
                value.y / magnitude,
                value.z / magnitude,
                value.w / magnitude);
        }

        private static void RequireNear(Vector3 actual, Vector3 expected, string label)
        {
            if (Vector3.Distance(actual, expected) > TransformTolerance)
                throw new InvalidOperationException(label + " changed.");
        }

        private static void RequireNear(
            Quaternion actual,
            Quaternion expected,
            string label)
        {
            if (Quaternion.Angle(actual, expected) > RotationTolerance)
                throw new InvalidOperationException(label + " changed.");
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed.");
        }

        private static void WriteTemp(string name, string value)
        {
            Directory.CreateDirectory(Absolute(TempFolder));
            File.WriteAllText(
                Absolute(TempFolder + "/" + name), value, new UTF8Encoding(false));
        }

        private static void WriteOutput(string name, string value)
        {
            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(
                Absolute(OutputFolder + "/" + name), value, new UTF8Encoding(false));
        }

        private static string ReadOutput(string name)
        {
            string path = Absolute(OutputFolder + "/" + name);
            if (!File.Exists(path))
                throw new FileNotFoundException("Validation baseline is missing.", path);
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static string Sha256Text(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value))
                    .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(stream)
                    .Select(item => item.ToString("X2", CultureInfo.InvariantCulture)));
        }

        internal static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w);
        }

        private readonly struct OrientedBounds
        {
            internal OrientedBounds(Vector3 minimum, Vector3 maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            internal Vector3 Minimum { get; }
            internal Vector3 Maximum { get; }
        }

        private readonly struct PressPoseResult
        {
            internal PressPoseResult(
                Dictionary<string, Quaternion> localRotations,
                float contactDistance,
                float elbowBendDegrees,
                float wristDeltaDegrees,
                float minimumNonIndexSurfaceClearance,
                float wristSurfaceOffset)
            {
                LocalRotations = localRotations;
                ContactDistance = contactDistance;
                ElbowBendDegrees = elbowBendDegrees;
                WristDeltaDegrees = wristDeltaDegrees;
                MinimumNonIndexSurfaceClearance = minimumNonIndexSurfaceClearance;
                WristSurfaceOffset = wristSurfaceOffset;
            }

            internal Dictionary<string, Quaternion> LocalRotations { get; }
            internal float ContactDistance { get; }
            internal float ElbowBendDegrees { get; }
            internal float WristDeltaDegrees { get; }
            internal float MinimumNonIndexSurfaceClearance { get; }
            internal float WristSurfaceOffset { get; }
        }

        private readonly struct SampleMetrics
        {
            internal SampleMetrics(
                float contactDistance,
                float elbowBendDegrees,
                float wristDeltaDegrees,
                float minimumNonIndexSurfaceClearance,
                float minimumTorsoClearance,
                float leftPoseDeviation,
                float rightArmDeviation,
                float mineLocalPositionError,
                float mineLocalRotationError,
                float mineLocalScaleError)
            {
                ContactDistance = contactDistance;
                ElbowBendDegrees = elbowBendDegrees;
                WristDeltaDegrees = wristDeltaDegrees;
                MinimumNonIndexSurfaceClearance = minimumNonIndexSurfaceClearance;
                MinimumTorsoClearance = minimumTorsoClearance;
                LeftPoseDeviation = leftPoseDeviation;
                RightArmDeviation = rightArmDeviation;
                MineLocalPositionError = mineLocalPositionError;
                MineLocalRotationError = mineLocalRotationError;
                MineLocalScaleError = mineLocalScaleError;
            }

            internal float ContactDistance { get; }
            internal float ElbowBendDegrees { get; }
            internal float WristDeltaDegrees { get; }
            internal float MinimumNonIndexSurfaceClearance { get; }
            internal float MinimumTorsoClearance { get; }
            internal float LeftPoseDeviation { get; }
            internal float RightArmDeviation { get; }
            internal float MineLocalPositionError { get; }
            internal float MineLocalRotationError { get; }
            internal float MineLocalScaleError { get; }
        }

        private readonly struct PoseContinuityMetrics
        {
            internal PoseContinuityMetrics(
                float maximumAdjacentRotationDegrees,
                string maximumRotationPath,
                float maximumRotationFromTime,
                float maximumRotationToTime,
                float maximumRotationAccelerationDegrees,
                string maximumAccelerationPath,
                float maximumAccelerationTime)
            {
                MaximumAdjacentRotationDegrees = maximumAdjacentRotationDegrees;
                MaximumRotationPath = maximumRotationPath;
                MaximumRotationFromTime = maximumRotationFromTime;
                MaximumRotationToTime = maximumRotationToTime;
                MaximumRotationAccelerationDegrees =
                    maximumRotationAccelerationDegrees;
                MaximumAccelerationPath = maximumAccelerationPath;
                MaximumAccelerationTime = maximumAccelerationTime;
            }

            internal float MaximumAdjacentRotationDegrees { get; }
            internal string MaximumRotationPath { get; }
            internal float MaximumRotationFromTime { get; }
            internal float MaximumRotationToTime { get; }
            internal float MaximumRotationAccelerationDegrees { get; }
            internal string MaximumAccelerationPath { get; }
            internal float MaximumAccelerationTime { get; }
        }

        internal readonly struct RuntimeMetrics
        {
            internal RuntimeMetrics(
                float contactDistance,
                float elbowBendDegrees,
                float minimumNonIndexSurfaceClearance,
                float minimumTorsoClearance,
                float rightArmDeviation,
                float propPositionError,
                float propRotationError,
                float propScaleError,
                bool propParentIsRightHand)
            {
                ContactDistance = contactDistance;
                ElbowBendDegrees = elbowBendDegrees;
                MinimumNonIndexSurfaceClearance = minimumNonIndexSurfaceClearance;
                MinimumTorsoClearance = minimumTorsoClearance;
                RightArmDeviation = rightArmDeviation;
                PropPositionError = propPositionError;
                PropRotationError = propRotationError;
                PropScaleError = propScaleError;
                PropParentIsRightHand = propParentIsRightHand;
            }

            internal float ContactDistance { get; }
            internal float ElbowBendDegrees { get; }
            internal float MinimumNonIndexSurfaceClearance { get; }
            internal float MinimumTorsoClearance { get; }
            internal float RightArmDeviation { get; }
            internal float PropPositionError { get; }
            internal float PropRotationError { get; }
            internal float PropScaleError { get; }
            internal bool PropParentIsRightHand { get; }
        }

        private readonly struct HandProbe
        {
            internal HandProbe(Vector3 position, float radius, string label)
            {
                Position = position;
                Radius = radius;
                Label = label;
            }

            internal Vector3 Position { get; }
            internal float Radius { get; }
            internal string Label { get; }
        }
    }

    [InitializeOnLoad]
    internal static class ElectricMineActivatePlayModeReview
    {
        private const string PendingKey = "Bellerophon.ElectricMineActivateReview.Pending";
        private const string RequestIdKey = "Bellerophon.ElectricMineActivateReview.RequestId";
        private const string LogPathKey = "Bellerophon.ElectricMineActivateReview.LogPath";
        private const string StateKey = "Bellerophon.ElectricMineActivateReview.State";
        private const string FailureKey = "Bellerophon.ElectricMineActivateReview.Failure";
        private const string ContinuousOnlyKey =
            "Bellerophon.ElectricMineActivateReview.ContinuousOnly";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const double TimeoutSeconds = 40d;
        private const int ContinuousCaptureWidth = 540;
        private const int ContinuousCaptureHeight = 630;
        private const int MinimumContinuousFrameCount = 55;
        private const float MinimumContinuousDurationSeconds = 1.95f;
        private const float MaximumContinuousFrameIntervalSeconds = 0.055f;
        private static readonly float[] CapturePhaseTimes =
        {
            0f, 0.26f, 0.405f, 0.54f, 0.635f, 1.075f
        };
        private static GameObject target;
        private static Animator animator;
        private static AnimationClip clip;
        private static Texture2D[] panels;
        private static Dictionary<string, Quaternion> initialRightPose;
        private static Dictionary<string, Quaternion> initialLeftPose;
        private static Vector3 rootPosition;
        private static Quaternion rootRotation;
        private static Vector3 rootScale;
        private static Vector3 propPosition;
        private static Quaternion propRotation;
        private static Vector3 propScale;
        private static double startedAt;
        private static int baseCycle = -1;
        private static int nextCapture;
        private static int lastCaptureCycle = -1;
        private static int cleanBaseCycle = -1;
        private static int lastObservedCycle = -1;
        private static double lastCycleStartedAt;
        private static float observedCycleDuration = -1f;
        private static float minimumContactCycle0 = float.MaxValue;
        private static float minimumContactCycle1 = float.MaxValue;
        private static float minimumNonIndexSurfaceClearance = float.MaxValue;
        private static float minimumClearancePhase = -1f;
        private static int minimumClearanceCycle = -1;
        private static float minimumTorsoClearance = float.MaxValue;
        private static float minimumTorsoClearancePhase = -1f;
        private static int minimumTorsoClearanceCycle = -1;
        private static float bestReturnDeviation = float.MaxValue;
        private static float bestReturnPhase = -1f;
        private static float maximumRightArmDeviation;
        private static float maximumPropPositionError;
        private static float maximumPropRotationError;
        private static float maximumPropScaleError;
        private static readonly List<string> Observations = new List<string>();
        private static bool continuousOnly;
        private static GameObject continuousReviewCamera;
        private static Camera continuousUnityCamera;
        private static RenderTexture continuousRenderTexture;
        private static double continuousCaptureStartedAt;
        private static readonly List<ContinuousFrameData> ContinuousFrames =
            new List<ContinuousFrameData>();
        private static int continuousPendingReadbacks;
        private static int continuousFailedReadbacks;
        private static int continuousRequestedFrames;

        static ElectricMineActivatePlayModeReview()
        {
            if (SessionState.GetBool(PendingKey, false)) Subscribe();
        }

        internal static void Start(
            string requestId,
            string logPath,
            bool continuous)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Review must start in Edit Mode.");
            if (SessionState.GetBool(PendingKey, false))
                throw new InvalidOperationException("A button press review is already pending.");
            Directory.CreateDirectory(ElectricMineActivateAnimationTools.Absolute(
                ElectricMineActivateAnimationTools.TempFolder));
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(RequestIdKey, requestId);
            SessionState.SetString(LogPathKey, logPath);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetBool(ContinuousOnlyKey, continuous);
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }
            try
            {
                int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying) return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before review completed.");
                    Observe();
                    return;
                }
                if (state == WaitingForEditMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CompleteInEditMode();
                    return;
                }
                throw new InvalidOperationException("Unknown button press review state.");
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void InitializeRuntime()
        {
            startedAt = EditorApplication.timeSinceStartup;
            continuousOnly = SessionState.GetBool(ContinuousOnlyKey, false);
            target = ElectricMineActivateAnimationTools.RequireRuntimeTarget();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException("Runtime Animator is missing.");
            clip = ElectricMineActivateAnimationTools.RequireRuntimeClip(target);
            panels = continuousOnly ? null : new Texture2D[12];
            initialRightPose = ElectricMineActivateAnimationTools.CaptureRightPose(target);
            initialLeftPose =
                ElectricMineActivateAnimationTools.CaptureClipStartLeftPose(
                    target, clip);
            rootPosition = target.transform.position;
            rootRotation = target.transform.rotation;
            rootScale = target.transform.localScale;
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            propPosition = prop.localPosition;
            propRotation = prop.localRotation;
            propScale = prop.localScale;
            continuousReviewCamera = continuousOnly
                ? ElectricMineActivateAnimationTools.CreateContinuousReviewCamera(target)
                : null;
            continuousUnityCamera = continuousReviewCamera == null
                ? null
                : continuousReviewCamera.GetComponentInChildren<Camera>(true);
            if (continuousOnly)
            {
                if (continuousUnityCamera == null)
                    throw new InvalidOperationException(
                        "Continuous review camera component is missing.");
                continuousRenderTexture = new RenderTexture(
                    ContinuousCaptureWidth,
                    ContinuousCaptureHeight,
                    24,
                    RenderTextureFormat.ARGB32)
                {
                    name = "ElectricMineActivate_ContinuousCapture",
                    antiAliasing = 1,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                continuousRenderTexture.Create();
                continuousUnityCamera.targetTexture = continuousRenderTexture;
            }
            baseCycle = -1;
            nextCapture = continuousOnly ? CapturePhaseTimes.Length : 0;
            lastCaptureCycle = -1;
            cleanBaseCycle = -1;
            lastObservedCycle = -1;
            lastCycleStartedAt = 0d;
            observedCycleDuration = -1f;
            minimumContactCycle0 = float.MaxValue;
            minimumContactCycle1 = float.MaxValue;
            minimumNonIndexSurfaceClearance = float.MaxValue;
            minimumClearancePhase = -1f;
            minimumClearanceCycle = -1;
            minimumTorsoClearance = float.MaxValue;
            minimumTorsoClearancePhase = -1f;
            minimumTorsoClearanceCycle = -1;
            bestReturnDeviation = float.MaxValue;
            bestReturnPhase = -1f;
            maximumRightArmDeviation = 0f;
            maximumPropPositionError = 0f;
            maximumPropRotationError = 0f;
            maximumPropScaleError = 0f;
            Observations.Clear();
            ContinuousFrames.Clear();
            continuousPendingReadbacks = 0;
            continuousFailedReadbacks = 0;
            continuousRequestedFrames = 0;
            continuousCaptureStartedAt = -1d;
        }

        private static void Observe()
        {
            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
                throw new TimeoutException("Natural button press review exceeded 40 seconds.");
            if (!animator.isInitialized) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            float absoluteSeconds = state.normalizedTime * clip.length;
            int cycle = Mathf.FloorToInt(absoluteSeconds / clip.length);
            float phase = Mathf.Repeat(absoluteSeconds, clip.length);
            if (baseCycle < 0)
            {
                if (phase > 0.055f) return;
                baseCycle = cycle;
                if (continuousOnly) lastCaptureCycle = cycle - 1;
            }
            ElectricMineActivateAnimationTools.RuntimeMetrics metrics =
                ElectricMineActivateAnimationTools.MeasureRuntime(
                    target, initialRightPose,
                    propPosition, propRotation, propScale);
            if (!metrics.PropParentIsRightHand)
                throw new InvalidOperationException(
                    "Electric mine stopped following the RightHand hierarchy.");
            if (Vector3.Distance(target.transform.position, rootPosition) > 0.0001f ||
                Quaternion.Angle(target.transform.rotation, rootRotation) > 0.02f ||
                Vector3.Distance(target.transform.localScale, rootScale) > 0.00001f)
                throw new InvalidOperationException("Target root moved during playback.");
            int relativeCycle = cycle - baseCycle;
            if (relativeCycle >= 0 && nextCapture < CapturePhaseTimes.Length &&
                cycle != lastCaptureCycle &&
                phase >= CapturePhaseTimes[nextCapture])
            {
                panels[nextCapture] =
                    ElectricMineActivateAnimationTools.CaptureTargetPanel(target, false);
                panels[nextCapture + 6] =
                    ElectricMineActivateAnimationTools.CaptureTargetPanel(target, true);
                Observations.Add("capture=" + nextCapture +
                    "|phaseSeconds=" + ElectricMineActivateAnimationTools.F(phase) +
                    "|contactDistance=" +
                    ElectricMineActivateAnimationTools.F(metrics.ContactDistance) +
                    "|nonIndexSurfaceClearance=" +
                    ElectricMineActivateAnimationTools.F(
                        metrics.MinimumNonIndexSurfaceClearance) +
                    "|elbowBend=" +
                    ElectricMineActivateAnimationTools.F(metrics.ElbowBendDegrees));
                lastCaptureCycle = cycle;
                nextCapture++;
            }
            if (cleanBaseCycle < 0)
            {
                if (nextCapture < CapturePhaseTimes.Length ||
                    cycle <= lastCaptureCycle || phase > 0.055f)
                    return;
                cleanBaseCycle = cycle;
                lastObservedCycle = cycle;
                lastCycleStartedAt = EditorApplication.timeSinceStartup;
                observedCycleDuration = -1f;
                minimumContactCycle0 = float.MaxValue;
                minimumContactCycle1 = float.MaxValue;
                minimumNonIndexSurfaceClearance = float.MaxValue;
                minimumClearancePhase = -1f;
                minimumClearanceCycle = -1;
                minimumTorsoClearance = float.MaxValue;
                minimumTorsoClearancePhase = -1f;
                minimumTorsoClearanceCycle = -1;
                bestReturnDeviation = float.MaxValue;
                bestReturnPhase = -1f;
                maximumRightArmDeviation = 0f;
                maximumPropPositionError = 0f;
                maximumPropRotationError = 0f;
                maximumPropScaleError = 0f;
            }
            if (cycle != lastObservedCycle)
            {
                if (lastObservedCycle >= cleanBaseCycle)
                    observedCycleDuration = (float)(
                        EditorApplication.timeSinceStartup - lastCycleStartedAt);
                lastCycleStartedAt = EditorApplication.timeSinceStartup;
                lastObservedCycle = cycle;
            }
            maximumRightArmDeviation = Mathf.Max(
                maximumRightArmDeviation, metrics.RightArmDeviation);
            maximumPropPositionError = Mathf.Max(
                maximumPropPositionError, metrics.PropPositionError);
            maximumPropRotationError = Mathf.Max(
                maximumPropRotationError, metrics.PropRotationError);
            maximumPropScaleError = Mathf.Max(
                maximumPropScaleError, metrics.PropScaleError);
            if (metrics.MinimumNonIndexSurfaceClearance <
                minimumNonIndexSurfaceClearance)
            {
                minimumNonIndexSurfaceClearance =
                    metrics.MinimumNonIndexSurfaceClearance;
                minimumClearancePhase = phase;
                minimumClearanceCycle = cycle - cleanBaseCycle;
            }
            if (metrics.MinimumTorsoClearance < minimumTorsoClearance)
            {
                minimumTorsoClearance = metrics.MinimumTorsoClearance;
                minimumTorsoClearancePhase = phase;
                minimumTorsoClearanceCycle = cycle - cleanBaseCycle;
            }
            int cleanRelativeCycle = cycle - cleanBaseCycle;
            if (phase >= ElectricMineActivateAnimationTools.ReachSeconds &&
                phase <= ElectricMineActivateAnimationTools.ReachSeconds +
                    ElectricMineActivateAnimationTools.ContactSeconds)
            {
                if (cleanRelativeCycle == 0)
                    minimumContactCycle0 = Mathf.Min(
                        minimumContactCycle0, metrics.ContactDistance);
                else if (cleanRelativeCycle == 1)
                    minimumContactCycle1 = Mathf.Min(
                        minimumContactCycle1, metrics.ContactDistance);
            }
            if (phase >= 1.06f)
            {
                float returnDeviation =
                    ElectricMineActivateAnimationTools.LeftPoseDeviation(
                        target, initialLeftPose);
                if (returnDeviation < bestReturnDeviation)
                {
                    bestReturnDeviation = returnDeviation;
                    bestReturnPhase = phase;
                }
            }
            if (continuousOnly && cleanRelativeCycle >= 0 && cleanRelativeCycle <= 2)
                CaptureContinuousFrame();
            if (cleanRelativeCycle < 2 || phase < 0.075f) return;
            if (continuousOnly && continuousPendingReadbacks > 0) return;
            FinishPlayMode();
        }

        private static void CaptureContinuousFrame()
        {
            if (continuousUnityCamera == null || continuousRenderTexture == null)
                throw new InvalidOperationException(
                    "Continuous capture camera is unavailable.");
            continuousUnityCamera.Render();
            double now = EditorApplication.timeSinceStartup;
            if (continuousCaptureStartedAt < 0d) continuousCaptureStartedAt = now;
            int frameIndex = continuousRequestedFrames++;
            float frameTime = (float)(now - continuousCaptureStartedAt);
            continuousPendingReadbacks++;
            AsyncGPUReadback.Request(
                continuousRenderTexture,
                0,
                TextureFormat.RGB24,
                request =>
                {
                    continuousPendingReadbacks--;
                    if (request.hasError)
                    {
                        continuousFailedReadbacks++;
                        return;
                    }
                    ContinuousFrames.Add(new ContinuousFrameData(
                        frameIndex,
                        frameTime,
                        request.GetData<byte>().ToArray()));
                });
        }

        private static string SaveContinuousCapture()
        {
            ContinuousFrames.Sort((left, right) => left.Index.CompareTo(right.Index));
            if (continuousPendingReadbacks != 0 || continuousFailedReadbacks != 0 ||
                ContinuousFrames.Count != continuousRequestedFrames ||
                ContinuousFrames.Count < 2)
                throw new InvalidOperationException(
                    "Continuous capture GPU readback is incomplete: requested=" +
                    continuousRequestedFrames + ", completed=" + ContinuousFrames.Count +
                    ", pending=" + continuousPendingReadbacks +
                    ", failed=" + continuousFailedReadbacks);
            float firstTime = ContinuousFrames[0].Time;
            float duration = ContinuousFrames[ContinuousFrames.Count - 1].Time - firstTime;
            float minimumInterval = float.MaxValue;
            float maximumInterval = 0f;
            float intervalSum = 0f;
            for (int index = 1; index < ContinuousFrames.Count; index++)
            {
                float interval = ContinuousFrames[index].Time -
                    ContinuousFrames[index - 1].Time;
                minimumInterval = Mathf.Min(minimumInterval, interval);
                maximumInterval = Mathf.Max(maximumInterval, interval);
                intervalSum += interval;
            }
            float averageInterval = intervalSum / (ContinuousFrames.Count - 1);
            if (ContinuousFrames.Count < MinimumContinuousFrameCount)
                throw new InvalidOperationException(
                    "Continuous capture frame count is too low: " +
                    ContinuousFrames.Count);
            if (duration < MinimumContinuousDurationSeconds)
                throw new InvalidOperationException(
                    "Continuous capture duration is too short: " +
                    ElectricMineActivateAnimationTools.F(duration));
            if (maximumInterval > MaximumContinuousFrameIntervalSeconds)
                throw new InvalidOperationException(
                    "Continuous capture contains a visible sampling gap: " +
                    ElectricMineActivateAnimationTools.F(maximumInterval));

            string frameFolder = ElectricMineActivateAnimationTools.Absolute(
                ElectricMineActivateAnimationTools.ContinuousCaptureFolder);
            Directory.CreateDirectory(frameFolder);
            var concat = new StringBuilder().AppendLine("ffconcat version 1.0");
            for (int index = 0; index < ContinuousFrames.Count; index++)
            {
                string fileName = "frame_" + index.ToString("D4", CultureInfo.InvariantCulture) +
                    ".png";
                var frame = new Texture2D(
                    ContinuousCaptureWidth,
                    ContinuousCaptureHeight,
                    TextureFormat.RGB24,
                    false);
                try
                {
                    frame.LoadRawTextureData(ContinuousFrames[index].Pixels);
                    frame.Apply(false, false);
                    File.WriteAllBytes(
                        Path.Combine(frameFolder, fileName),
                        frame.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }
                concat.AppendLine("file 'UnityContinuousFrames/" + fileName + "'");
                float interval = index + 1 < ContinuousFrames.Count
                    ? ContinuousFrames[index + 1].Time - ContinuousFrames[index].Time
                    : averageInterval;
                concat.AppendLine("duration " +
                    interval.ToString("R", CultureInfo.InvariantCulture));
            }
            concat.AppendLine("file 'UnityContinuousFrames/frame_" +
                (ContinuousFrames.Count - 1).ToString("D4", CultureInfo.InvariantCulture) +
                ".png'");
            File.WriteAllText(
                ElectricMineActivateAnimationTools.Absolute(
                    ElectricMineActivateAnimationTools.ContinuousCaptureListPath),
                concat.ToString(),
                new UTF8Encoding(false));
            float averageFramesPerSecond = 1f / averageInterval;
            var report = new StringBuilder()
                .AppendLine("ElectricMine_Activate Unity internal continuous capture")
                .AppendLine("passed=True")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("foregroundWindowIndependent=True")
                .AppendLine("cameraTarget=ElectricMine_Activate")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("cyclesCaptured=2")
                .AppendLine("frameCount=" + ContinuousFrames.Count)
                .AppendLine("captureWidth=" + ContinuousCaptureWidth)
                .AppendLine("captureHeight=" + ContinuousCaptureHeight)
                .AppendLine("captureDurationSeconds=" +
                    ElectricMineActivateAnimationTools.F(duration))
                .AppendLine("averageFramesPerSecond=" +
                    ElectricMineActivateAnimationTools.F(averageFramesPerSecond))
                .AppendLine("minimumFrameIntervalSeconds=" +
                    ElectricMineActivateAnimationTools.F(minimumInterval))
                .AppendLine("maximumFrameIntervalSeconds=" +
                    ElectricMineActivateAnimationTools.F(maximumInterval))
                .AppendLine("maximumAllowedFrameIntervalSeconds=" +
                    ElectricMineActivateAnimationTools.F(
                        MaximumContinuousFrameIntervalSeconds));
            ElectricMineActivateAnimationTools.WriteContinuousCaptureReport(
                report.ToString());
            return report.ToString();
        }

        private static void FinishPlayMode()
        {
            if (!continuousOnly)
            {
                if (panels == null || panels.Any(panel => panel == null))
                    throw new InvalidOperationException(
                        "Button press review panels are incomplete.");
                ElectricMineActivateAnimationTools.ComposeReview(panels);
            }
            if (minimumContactCycle0 > 0.015f || minimumContactCycle1 > 0.015f)
                throw new InvalidOperationException(
                    "Left index did not contact the red button in both cycles.");
            if (minimumNonIndexSurfaceClearance <
                ElectricMineActivateAnimationTools.RequiredHandSurfaceClearanceMeters)
                throw new InvalidOperationException(
                    "Left hand intersected the electric mine during natural playback: " +
                    ElectricMineActivateAnimationTools.F(
                        minimumNonIndexSurfaceClearance) +
                    ", phase=" + ElectricMineActivateAnimationTools.F(
                        minimumClearancePhase) +
                    ", cycle=" + minimumClearanceCycle);
            if (minimumTorsoClearance <
                ElectricMineActivateAnimationTools.RequiredTorsoClearanceMeters)
                throw new InvalidOperationException(
                    "Left arm intersected the torso during natural playback: " +
                    ElectricMineActivateAnimationTools.F(minimumTorsoClearance) +
                    ", phase=" + ElectricMineActivateAnimationTools.F(
                        minimumTorsoClearancePhase) +
                    ", cycle=" + minimumTorsoClearanceCycle);
            if (bestReturnDeviation > 2f)
                throw new InvalidOperationException(
                    "Left arm did not return to the starting pose: degrees=" +
                    ElectricMineActivateAnimationTools.F(bestReturnDeviation) +
                    ", phase=" +
                    ElectricMineActivateAnimationTools.F(bestReturnPhase));
            if (observedCycleDuration < 0.90f || observedCycleDuration > 1.30f)
                throw new InvalidOperationException(
                    "Observed natural cycle duration differs from 1.1 seconds.");
            if (maximumRightArmDeviation > 0.1f)
                throw new InvalidOperationException("Right-arm grip pose drifted.");
            if (maximumPropPositionError > 0.00001f ||
                maximumPropRotationError > 0.02f ||
                maximumPropScaleError > 0.00001f)
                throw new InvalidOperationException("Electric mine local state changed.");
            string continuousCaptureReport = continuousOnly
                ? SaveContinuousCapture()
                : string.Empty;
            var report = new StringBuilder()
                .AppendLine("ElectricMine_Activate natural button press review")
                .AppendLine("passed=True")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("continuousOnly=" + continuousOnly)
                .AppendLine("capturesDistributedAcrossSeparateCycles=" +
                    (!continuousOnly))
                .AppendLine("postCaptureNaturalCyclesObserved=" +
                    (continuousOnly ? 0 : 2))
                .AppendLine("cyclesObserved=2")
                .AppendLine("reachSeconds=" +
                    ElectricMineActivateAnimationTools.F(
                        ElectricMineActivateAnimationTools.ReachSeconds))
                .AppendLine("contactSeconds=" +
                    ElectricMineActivateAnimationTools.F(
                        ElectricMineActivateAnimationTools.ContactSeconds))
                .AppendLine("returnSeconds=" +
                    ElectricMineActivateAnimationTools.F(
                        ElectricMineActivateAnimationTools.ReturnSeconds))
                .AppendLine("observedCycleDuration=" +
                    ElectricMineActivateAnimationTools.F(observedCycleDuration))
                .AppendLine("leftIndexContactsButton=True")
                .AppendLine("leftHandMinePenetration=False")
                .AppendLine("leftArmTorsoIntersection=False")
                .AppendLine("minimumNonIndexSurfaceClearanceMeters=" +
                    ElectricMineActivateAnimationTools.F(
                        minimumNonIndexSurfaceClearance))
                .AppendLine("minimumLeftArmTorsoClearanceMeters=" +
                    ElectricMineActivateAnimationTools.F(
                        minimumTorsoClearance))
                .AppendLine("minimumContactDistanceCycle0=" +
                    ElectricMineActivateAnimationTools.F(minimumContactCycle0))
                .AppendLine("minimumContactDistanceCycle1=" +
                    ElectricMineActivateAnimationTools.F(minimumContactCycle1))
                .AppendLine("bestReturnPoseDeviationDegrees=" +
                    ElectricMineActivateAnimationTools.F(bestReturnDeviation))
                .AppendLine("bestReturnPosePhaseSeconds=" +
                    ElectricMineActivateAnimationTools.F(bestReturnPhase))
                .AppendLine("maximumRightArmDeviationDegrees=" +
                    ElectricMineActivateAnimationTools.F(maximumRightArmDeviation))
                .AppendLine("electricMineFollowsRightHand=True")
                .AppendLine("maximumMineLocalPositionError=" +
                    ElectricMineActivateAnimationTools.F(maximumPropPositionError))
                .AppendLine("maximumMineLocalRotationErrorDegrees=" +
                    ElectricMineActivateAnimationTools.F(maximumPropRotationError))
                .AppendLine("maximumMineLocalScaleError=" +
                    ElectricMineActivateAnimationTools.F(maximumPropScaleError))
                .AppendLine("buttonAnimated=False");
            if (continuousOnly)
                report.AppendLine("unityInternalContinuousCapture=True")
                    .AppendLine("foregroundWindowIndependent=True")
                    .AppendLine("continuousCaptureReport=" +
                        ElectricMineActivateAnimationTools.ContinuousCaptureReportPath);
            foreach (string observation in Observations) report.AppendLine(observation);
            if (continuousOnly) report.Append(continuousCaptureReport);
            ElectricMineActivateAnimationTools.WriteReviewReport(report.ToString());
            CleanupRuntime();
            SessionState.SetInt(StateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void CompleteInEditMode()
        {
            string requestId = SessionState.GetString(RequestIdKey, string.Empty);
            string logPath = SessionState.GetString(LogPathKey, string.Empty);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            if (string.IsNullOrEmpty(failure))
            {
                ElectricMineActivateAnimationTools.InspectStructure();
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId +
                    Environment.NewLine + "status=passed" + Environment.NewLine +
                    "ElectricMine_Activate natural two-cycle review completed.");
            }
            else
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId +
                    Environment.NewLine + "status=failed" + Environment.NewLine + failure);
            ClearSession();
        }

        private static void Fail(Exception exception)
        {
            CleanupRuntime();
            SessionState.SetString(FailureKey, exception.ToString());
            SessionState.SetInt(StateKey, WaitingForEditMode);
            Debug.LogWarning(
                "ElectricMine_Activate review failed: " + exception.Message);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.ExitPlaymode();
            else
                CompleteInEditMode();
        }

        private static void CleanupRuntime()
        {
            if (panels != null)
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            target = null;
            animator = null;
            clip = null;
            panels = null;
            initialRightPose = null;
            initialLeftPose = null;
            if (continuousReviewCamera != null)
                UnityEngine.Object.DestroyImmediate(continuousReviewCamera);
            continuousReviewCamera = null;
            continuousUnityCamera = null;
            if (continuousRenderTexture != null)
            {
                continuousRenderTexture.Release();
                UnityEngine.Object.DestroyImmediate(continuousRenderTexture);
            }
            continuousRenderTexture = null;
            continuousCaptureStartedAt = -1d;
            ContinuousFrames.Clear();
            continuousPendingReadbacks = 0;
            continuousFailedReadbacks = 0;
            continuousRequestedFrames = 0;
            Observations.Clear();
        }

        private sealed class ContinuousFrameData
        {
            internal ContinuousFrameData(int index, float time, byte[] pixels)
            {
                Index = index;
                Time = time;
                Pixels = pixels;
            }

            internal int Index { get; }
            internal float Time { get; }
            internal byte[] Pixels { get; }
        }

        private static void ClearSession()
        {
            EditorApplication.update -= Tick;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseString(RequestIdKey);
            SessionState.EraseString(LogPathKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseString(FailureKey);
            SessionState.EraseBool(ContinuousOnlyKey);
        }

        private static void WriteBridgeLog(string path, string value)
        {
            string absolute = ElectricMineActivateAnimationTools.Absolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Bridge log folder is unavailable."));
            File.WriteAllText(absolute, value, new UTF8Encoding(false));
        }
    }
}
