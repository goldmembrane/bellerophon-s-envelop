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
    internal static class ConfusedAnimationSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Confused_Walk_Forward";
        private const string ZAlignmentReferenceName = "Fatigue_HeadShake";
        private const string StateName = "ConfusedWalkForward_Source";
        private const string ExternalSourcePath = "player model/transfer dizzy.fbx";
        private const string SourceAssetPath =
            "Assets/_Project/Animations/PlayerStatusEffects/Sources/Confused_Walk_Forward_Source.fbx";
        internal const string ControllerPath =
            "Assets/_Project/Animations/PlayerStatusEffects/Confused_Walk_Forward.controller";
        internal const string FinalImagePath =
            "docs/validation/ConfusedWalkForward/Final.png";
        internal const string ZAlignmentFinalImagePath =
            "docs/validation/ConfusedWalkForwardZAlignment/Final.png";

        [MenuItem("Bellerophon/Player/Apply Confused Walk Forward Animation")]
        internal static void ApplyConfusedWalkForwardAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            TransformSnapshot root = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Avatar avatar = animator.avatar;

            ConfigureExactGenericSourceForLooping();
            AnimationClip source = RequireSingleSourceClip();
            AnimatorController controller = CreateController(source);

            Undo.RecordObject(animator, "Connect exact confused walk animation");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            root.RequireUnchanged(target.transform, TargetName);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                TargetName + " renderer signature");
            if (animator.avatar != avatar)
                throw new InvalidOperationException(
                    TargetName + " Avatar changed unexpectedly.");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying Confused_Walk_Forward.");
            AssetDatabase.SaveAssets();
            InspectConfusedWalkForwardAnimation();

            Debug.Log(
                "[ConfusedWalkForward] Exact transfer dizzy embedded Mixamo clip " +
                "connected directly and looped. sourceAnimationCurvesModified=False," +
                "generatedMotion=False,poseInference=False,targetTransformChanged=False," +
                "targetRenderersChanged=False,targetAvatarChanged=False");
        }

        [MenuItem("Bellerophon/Player/Inspect Confused Walk Forward Animation")]
        internal static void InspectConfusedWalkForwardAnimation()
        {
            RequireEditMode();
            RequireExactSourceCopy();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            AnimationClip source = RequireSingleSourceClip();
            RequireLoop(source, true, SourceAssetPath);
            if (source.humanMotion)
                throw new InvalidOperationException(
                    "Confused walk source must remain a direct Generic clip.");
            int curveCount = AnimationUtility.GetCurveBindings(source).Length;
            if (curveCount == 0)
                throw new InvalidOperationException(
                    "Confused walk source has no animation curves.");

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(ControllerPath + " is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(
                    TargetName + " controller connection differs.");
            if (animator.applyRootMotion)
                throw new InvalidOperationException(
                    TargetName + " applyRootMotion must remain disabled.");
            if (controller.layers.Length != 1 || controller.parameters.Length != 0)
                throw new InvalidOperationException(
                    "Confused walk controller must contain one parameter-free layer.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState[] states = machine.states.Select(item => item.state).ToArray();
            if (states.Length != 1 ||
                machine.defaultState != states[0] ||
                states[0].name != StateName ||
                states[0].motion != source ||
                !Mathf.Approximately(states[0].speed, 1f) ||
                states[0].transitions.Length != 0)
                throw new InvalidOperationException(
                    "Confused walk controller must directly use the unchanged source " +
                    "in one speed-1 state without transitions.");

            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ConfusedWalkForward] Structural inspection passed. sourceSha256=" +
                Sha256(Absolute(ExternalSourcePath)) + ",sourceBinaryCopyExact=True," +
                "sourceAnimationCurvesModified=False,sourceClipDirect=True," +
                "generatedMotion=False,clip=" + DescribeClip(source) +
                ",curveCount=" + curveCount + ",loop=True,controllerSpeed=1," +
                "transitions=0,applyRootMotion=False,unityConsoleErrors=0");
        }

        [MenuItem("Bellerophon/Player/Apply Confused Walk Forward Z Alignment")]
        internal static void ApplyConfusedWalkForwardZAlignment()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject reference = FindUnique(scene, ZAlignmentReferenceName);
            Transform targetTransform = target.transform;
            Vector3 positionBefore = targetTransform.position;
            Vector3 localPositionBefore = targetTransform.localPosition;
            Quaternion localRotationBefore = targetTransform.localRotation;
            Vector3 localScaleBefore = targetTransform.localScale;
            string rendererSignatureBefore = RendererSignature(targetTransform);
            Animator animator = RequireAnimator(target);
            RuntimeAnimatorController controllerBefore =
                animator.runtimeAnimatorController;
            Avatar avatarBefore = animator.avatar;

            Undo.RecordObject(
                targetTransform,
                "Align Confused_Walk_Forward Z with Fatigue_HeadShake");
            Vector3 alignedPosition = targetTransform.position;
            alignedPosition.z = reference.transform.position.z;
            targetTransform.position = alignedPosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetTransform);
            EditorUtility.SetDirty(targetTransform);

            RequireApproximately(
                targetTransform.position.z,
                reference.transform.position.z,
                0.0001f,
                "Confused/Fatigue world Z alignment");
            RequireApproximately(
                targetTransform.position.x,
                positionBefore.x,
                0.0001f,
                TargetName + " world X preservation");
            RequireApproximately(
                targetTransform.position.y,
                positionBefore.y,
                0.0001f,
                TargetName + " world Y preservation");
            RequireApproximately(
                targetTransform.localPosition.x,
                localPositionBefore.x,
                0.0001f,
                TargetName + " local X preservation");
            RequireApproximately(
                targetTransform.localPosition.y,
                localPositionBefore.y,
                0.0001f,
                TargetName + " local Y preservation");
            if (Quaternion.Angle(targetTransform.localRotation, localRotationBefore) > 0.0001f)
                throw new InvalidOperationException(
                    TargetName + " local rotation changed unexpectedly.");
            if (Vector3.Distance(targetTransform.localScale, localScaleBefore) > 0.0001f)
                throw new InvalidOperationException(
                    TargetName + " local scale changed unexpectedly.");
            RequireEqual(
                rendererSignatureBefore,
                RendererSignature(targetTransform),
                TargetName + " renderer signature");
            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore)
                throw new InvalidOperationException(
                    TargetName + " animation connection changed unexpectedly.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while aligning Confused_Walk_Forward Z.");
            InspectConfusedWalkForwardZAlignment();
            Debug.Log(
                "[ConfusedWalkForwardZAlignment] Applied. targetWorldZBefore=" +
                positionBefore.z.ToString("0.######", CultureInfo.InvariantCulture) +
                ",referenceWorldZ=" +
                reference.transform.position.z.ToString("0.######", CultureInfo.InvariantCulture) +
                ",targetWorldZAfter=" +
                targetTransform.position.z.ToString("0.######", CultureInfo.InvariantCulture) +
                ",xPreserved=True,yPreserved=True,rotationPreserved=True," +
                "scalePreserved=True,controllerPreserved=True,avatarPreserved=True," +
                "rendererSignaturePreserved=True");
        }

        [MenuItem("Bellerophon/Player/Inspect Confused Walk Forward Z Alignment")]
        internal static void InspectConfusedWalkForwardZAlignment()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject reference = FindUnique(scene, ZAlignmentReferenceName);
            RequireApproximately(
                target.transform.position.z,
                reference.transform.position.z,
                0.0001f,
                "Confused/Fatigue world Z alignment");
            Animator animator = RequireAnimator(target);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(ControllerPath + " is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(
                    TargetName + " controller connection differs.");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ConfusedWalkForwardZAlignment] Inspection passed. targetWorld=" +
                DescribeVector(target.transform.position) + ",referenceWorld=" +
                DescribeVector(reference.transform.position) + ",worldZError=" +
                Mathf.Abs(
                    target.transform.position.z - reference.transform.position.z)
                    .ToString("0.######", CultureInfo.InvariantCulture) +
                ",controllerPreserved=True,unityConsoleErrors=0");
        }

        [MenuItem("Bellerophon/Player/Inspect Confused Walk Forward Visual Z Alignment")]
        internal static void InspectConfusedWalkForwardVisualZAlignment()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject reference = FindUnique(scene, ZAlignmentReferenceName);
            AnimationClip source = RequireSingleSourceClip();
            Vector2 range = MeasureSourceHipsZRange(source);
            float minimum = range.x;
            float maximum = range.y;
            Transform targetHips = target.transform.Find("Armature/Hips") ??
                throw new InvalidOperationException(
                    TargetName + " Armature/Hips is missing.");
            Transform referenceHips = reference.transform.Find("Armature/Hips") ??
                throw new InvalidOperationException(
                    ZAlignmentReferenceName + " Armature/Hips is missing.");
            Debug.Log(
                "[ConfusedWalkForwardVisualZAlignment] Source motion inspected without modification. " +
                "hipsZMin=" + minimum.ToString("0.######", CultureInfo.InvariantCulture) +
                ",hipsZMax=" + maximum.ToString("0.######", CultureInfo.InvariantCulture) +
                ",hipsZRange=" +
                (maximum - minimum).ToString("0.######", CultureInfo.InvariantCulture) +
                ",targetEditHipsWorldZ=" +
                targetHips.position.z.ToString("0.######", CultureInfo.InvariantCulture) +
                ",referenceEditHipsWorldZ=" +
                referenceHips.position.z.ToString("0.######", CultureInfo.InvariantCulture) +
                ",sourceAnimationCurvesModified=False");
        }

        [MenuItem("Bellerophon/Player/Apply Confused Walk Forward Visual Z Alignment")]
        internal static void ApplyConfusedWalkForwardVisualZAlignment()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject reference = FindUnique(scene, ZAlignmentReferenceName);
            Animator animator = RequireAnimator(target);
            Vector3 localPositionBefore = target.transform.localPosition;
            Quaternion localRotationBefore = target.transform.localRotation;
            Vector3 localScaleBefore = target.transform.localScale;
            string rendererSignatureBefore = RendererSignature(target.transform);
            RuntimeAnimatorController controllerBefore = animator.runtimeAnimatorController;
            Avatar avatarBefore = animator.avatar;
            AnimationClip source = RequireSingleSourceClip();
            Vector2 sourceHipsZRange = MeasureSourceHipsZRange(source);
            float dynamicRange = sourceHipsZRange.y - sourceHipsZRange.x;
            if (dynamicRange <= 0.0001f)
                throw new InvalidOperationException(
                    "Confused walk source does not contain the measured dynamic Hips Z movement required for runtime visual alignment.");

            Type alignmentType = RequireVisualZAlignmentType();
            Component alignment = target.GetComponent(alignmentType);
            if (alignment == null)
                alignment = Undo.AddComponent(target, alignmentType);
            Undo.RecordObject(alignment, "Configure confused walk visual Z alignment");
            var serializedAlignment = new SerializedObject(alignment);
            serializedAlignment.FindProperty("referenceRoot").objectReferenceValue =
                reference.transform;
            serializedAlignment.FindProperty("targetAnchorPath").stringValue =
                "Armature/Hips";
            serializedAlignment.FindProperty("referenceAnchorPath").stringValue =
                "Armature/Hips";
            serializedAlignment.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(alignment);
            EditorUtility.SetDirty(alignment);

            if (target.transform.localPosition != localPositionBefore ||
                target.transform.localRotation != localRotationBefore ||
                target.transform.localScale != localScaleBefore)
                throw new InvalidOperationException(
                    TargetName + " root Transform changed while configuring visual Z alignment.");
            RequireEqual(
                rendererSignatureBefore,
                RendererSignature(target.transform),
                TargetName + " renderer signature");
            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore)
                throw new InvalidOperationException(
                    TargetName + " animation connection changed unexpectedly.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while configuring visual Z alignment.");
            AssetDatabase.SaveAssets();
            InspectConfiguredVisualZAlignment();
            Debug.Log(
                "[ConfusedWalkForwardVisualZAlignment] Target-only runtime Hips Z alignment applied. " +
                "sourceHipsZMin=" +
                sourceHipsZRange.x.ToString("0.######", CultureInfo.InvariantCulture) +
                ",sourceHipsZMax=" +
                sourceHipsZRange.y.ToString("0.######", CultureInfo.InvariantCulture) +
                ",sourceHipsZRange=" +
                dynamicRange.ToString("0.######", CultureInfo.InvariantCulture) +
                ",sourceAnimationCurvesModified=False,xPreserved=True,yPreserved=True," +
                "rotationPreserved=True,scalePreserved=True,controllerPreserved=True," +
                "avatarPreserved=True,rendererSignaturePreserved=True");
        }

        internal static void InspectConfiguredVisualZAlignment()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject reference = FindUnique(scene, ZAlignmentReferenceName);
            Type alignmentType = RequireVisualZAlignmentType();
            Component alignment = target.GetComponent(alignmentType) ??
                throw new InvalidOperationException(
                    TargetName + " visual Z alignment component is missing.");
            var serializedAlignment = new SerializedObject(alignment);
            if (serializedAlignment.FindProperty("referenceRoot").objectReferenceValue !=
                    reference.transform ||
                serializedAlignment.FindProperty("targetAnchorPath").stringValue !=
                    "Armature/Hips" ||
                serializedAlignment.FindProperty("referenceAnchorPath").stringValue !=
                    "Armature/Hips")
                throw new InvalidOperationException(
                    TargetName + " visual Z alignment configuration differs.");
            Animator animator = RequireAnimator(target);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(ControllerPath + " is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(
                    TargetName + " controller connection differs.");
            RequireExactSourceCopy();
            Vector2 range = MeasureSourceHipsZRange(RequireSingleSourceClip());
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ConfusedWalkForwardVisualZAlignment] Configuration inspection passed. " +
                "reference=" + reference.name + ",targetAnchor=Armature/Hips," +
                "referenceAnchor=Armature/Hips,sourceHipsZRange=" +
                (range.y - range.x).ToString("0.######", CultureInfo.InvariantCulture) +
                ",sourceBinaryCopyExact=True,sourceAnimationCurvesModified=False," +
                "controllerPreserved=True,unityConsoleErrors=0");
        }

        internal static GameObject RequireRuntimeTarget()
        {
            return FindUnique(RequireScene(), TargetName);
        }

        internal static GameObject RequireRuntimeZAlignmentReference()
        {
            return FindUnique(RequireScene(), ZAlignmentReferenceName);
        }

        internal static void WriteZAlignmentFinalEvidence(
            Texture2D image,
            int consoleErrorsBefore,
            float observedNormalizedTime,
            float maximumVisualAnchorZError,
            float targetXYPositionError,
            float targetRotationError,
            float targetScaleError,
            float minimumTargetRootWorldZ,
            float maximumTargetRootWorldZ)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (observedNormalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward visual Z review did not cover one full loop.");
            if (maximumVisualAnchorZError > 0.0001f)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward and Fatigue_HeadShake visual Hips Z anchors diverged in Play Mode.");
            if (targetXYPositionError > 0.0001f ||
                targetRotationError > 0.01f ||
                targetScaleError > 0.0001f)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward non-Z root properties changed during visual alignment review.");
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter > consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Z-alignment review introduced Unity console errors. Before=" +
                    consoleErrorsBefore + ", After=" + consoleErrorsAfter + ".");

            string absolute = Absolute(ZAlignmentFinalImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Confused walk Z-alignment validation folder is unavailable."));
            File.WriteAllBytes(absolute, image.EncodeToPNG());
            Debug.Log(
                "[ConfusedWalkForwardZAlignment] Full-loop direct Play Mode comparison captured once. " +
                "observedNormalizedTime=" +
                observedNormalizedTime.ToString("0.######", CultureInfo.InvariantCulture) +
                ",maximumVisualAnchorZError=" +
                maximumVisualAnchorZError.ToString("0.######", CultureInfo.InvariantCulture) +
                ",targetXYPositionError=" +
                targetXYPositionError.ToString("0.######", CultureInfo.InvariantCulture) +
                ",targetRotationError=" +
                targetRotationError.ToString("0.######", CultureInfo.InvariantCulture) +
                ",targetScaleError=" +
                targetScaleError.ToString("0.######", CultureInfo.InvariantCulture) +
                ",targetRootWorldZMin=" +
                minimumTargetRootWorldZ.ToString("0.######", CultureInfo.InvariantCulture) +
                ",targetRootWorldZMax=" +
                maximumTargetRootWorldZ.ToString("0.######", CultureInfo.InvariantCulture) +
                ",consoleErrorsBefore=" + consoleErrorsBefore +
                ",consoleErrorsAfter=" + consoleErrorsAfter +
                ",finalImage=" + ZAlignmentFinalImagePath);
        }

        internal static void WriteFinalEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float observedNormalizedTime,
            float rootPositionError,
            float rootRotationError)
        {
            if (sheet == null)
                throw new ArgumentNullException(nameof(sheet));
            if (observedNormalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward did not complete one natural loop.");
            if (rootPositionError > 0.0001f || rootRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward root changed during natural playback.");
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter > consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Confused walk review introduced Unity console errors. Before=" +
                    consoleErrorsBefore + ", After=" + consoleErrorsAfter + ".");

            string absolute = Absolute(FinalImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Confused walk validation folder is unavailable."));
            File.WriteAllBytes(absolute, sheet.EncodeToPNG());
            Debug.Log(
                "[ConfusedWalkForward] One unchanged natural Mixamo loop captured " +
                "once. observedNormalizedTime=" +
                observedNormalizedTime.ToString("0.######", CultureInfo.InvariantCulture) +
                ",rootPositionError=" +
                rootPositionError.ToString("0.######", CultureInfo.InvariantCulture) +
                ",rootRotationError=" +
                rootRotationError.ToString("0.######", CultureInfo.InvariantCulture) +
                ",consoleErrorsBefore=" + consoleErrorsBefore +
                ",consoleErrorsAfter=" + consoleErrorsAfter +
                ",finalImage=" + FinalImagePath);
        }

        private static void ConfigureExactGenericSourceForLooping()
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
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = false;
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

        private static AnimatorController CreateController(AnimationClip source)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
                throw new InvalidOperationException(
                    "Could not replace target-specific controller: " + ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            AnimatorState state =
                controller.layers[0].stateMachine.AddState(StateName);
            state.motion = source;
            state.speed = 1f;
            state.cycleOffset = 0f;
            state.mirror = false;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
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
                    clips.Length + ".");
            return clips[0];
        }

        private static void RequireExactSourceCopy()
        {
            string external = Absolute(ExternalSourcePath);
            string copied = Absolute(SourceAssetPath);
            if (!File.Exists(external) || !File.Exists(copied))
                throw new FileNotFoundException(
                    "Confused walk source or project copy is missing.");
            RequireEqual(
                Sha256(external),
                Sha256(copied),
                "Confused walk source binary copy");
        }

        private static void RequireLoop(AnimationClip clip, bool expected, string label)
        {
            bool actual = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
            if (actual != expected)
                throw new InvalidOperationException(
                    label + " loopTime differs. Expected=" + expected +
                    ", Actual=" + actual + ".");
        }

        private static string DescribeClip(AnimationClip clip)
        {
            return clip.name + ",length=" +
                clip.length.ToString("0.######", CultureInfo.InvariantCulture) +
                ",frameRate=" +
                clip.frameRate.ToString("0.######", CultureInfo.InvariantCulture) +
                ",humanMotion=" + clip.humanMotion;
        }

        private static string DescribeVector(Vector3 value)
        {
            return "(" +
                value.x.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                value.y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                value.z.ToString("0.######", CultureInfo.InvariantCulture) + ")";
        }

        private static Vector2 MeasureSourceHipsZRange(AnimationClip source)
        {
            EditorCurveBinding binding = AnimationUtility.GetCurveBindings(source)
                .SingleOrDefault(item =>
                    item.path == "Armature/Hips" &&
                    item.propertyName == "m_LocalPosition.z");
            if (string.IsNullOrEmpty(binding.path))
                throw new InvalidOperationException(
                    "Confused walk source has no Armature/Hips m_LocalPosition.z curve.");
            AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding) ??
                throw new InvalidOperationException(
                    "Confused walk Hips Z curve could not be loaded.");
            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            const int sampleCount = 240;
            for (int index = 0; index <= sampleCount; index++)
            {
                float time = source.length * index / sampleCount;
                float value = curve.Evaluate(time);
                minimum = Mathf.Min(minimum, value);
                maximum = Mathf.Max(maximum, value);
            }
            return new Vector2(minimum, maximum);
        }

        private static Type RequireVisualZAlignmentType()
        {
            Type type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .SingleOrDefault(candidate =>
                    candidate.FullName ==
                    "Bellerophon.Animation.ConfusedWalkForwardVisualZAlignment");
            return type ?? throw new InvalidOperationException(
                "ConfusedWalkForwardVisualZAlignment runtime component type is unavailable.");
        }

        private static void RequireApproximately(
            float actual,
            float expected,
            float tolerance,
            string label)
        {
            if (Mathf.Abs(actual - expected) > tolerance)
                throw new InvalidOperationException(
                    label + " differs. Expected=" +
                    expected.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", Actual=" +
                    actual.ToString("0.######", CultureInfo.InvariantCulture) + ".");
        }

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
                    "Expected exactly one " + name + "; actual=" + matches.Length + ".");
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject target)
        {
            return target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    TargetName + " Animator is missing.");
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward setup requires Edit Mode.");
        }

        private static string RendererSignature(Transform root)
        {
            return string.Join(
                "\n",
                root.GetComponentsInChildren<Renderer>(true)
                    .OrderBy(renderer =>
                        AnimationUtility.CalculateTransformPath(
                            renderer.transform,
                            root),
                        StringComparer.Ordinal)
                    .Select(renderer =>
                        AnimationUtility.CalculateTransformPath(
                            renderer.transform,
                            root) + "|" + renderer.GetType().FullName + "|" +
                        renderer.enabled));
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException("Project root is unavailable."),
                    relativePath));
        }

        private static string Sha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    label + " differs. Expected=" + expected + ", Actual=" + actual + ".");
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
                if (transform.localPosition != position ||
                    transform.localRotation != rotation ||
                    transform.localScale != scale)
                    throw new InvalidOperationException(
                        label + " root Transform changed unexpectedly.");
            }
        }
    }

    [InitializeOnLoad]
    internal static class ConfusedWalkForwardPlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.ConfusedWalkForward.Pending";
        private const string StateKey =
            "Bellerophon.ConfusedWalkForward.State";
        private const string FailureKey =
            "Bellerophon.ConfusedWalkForward.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.ConfusedWalkForward.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string StateName = "ConfusedWalkForward_Source";
        private static readonly float[] CaptureThresholds =
        {
            0.06f, 0.24f, 0.42f, 0.60f, 0.78f, 1.06f
        };
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static Animator animator;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static double startedAt;
        private static float maximumNormalizedTime;
        private static float maximumRootPositionError;
        private static float maximumRootRotationError;

        static ConfusedWalkForwardPlayModeCapture()
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
                    "Confused_Walk_Forward review must start in Edit Mode.");
            ConfusedAnimationSetupTools.InspectConfusedWalkForwardAnimation();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
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
                    "Confused_Walk_Forward review has no pending state.");
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
                            "Play Mode ended before confused walk review completed.");
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

                ConfusedAnimationSetupTools.InspectConfusedWalkForwardAnimation();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Confused_Walk_Forward completed one unchanged natural Mixamo " +
                    "loop with six direct-review phases.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
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
            target = ConfusedAnimationSetupTools.RequireRuntimeTarget();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Confused_Walk_Forward Animator is missing in Play Mode.");
            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            startedAt = EditorApplication.timeSinceStartup;
            maximumNormalizedTime = 0f;
            maximumRootPositionError = 0f;
            maximumRootRotationError = 0f;
        }

        private static void CaptureNaturalLoop()
        {
            if (EditorApplication.timeSinceStartup - startedAt > 30d)
                throw new TimeoutException(
                    "Confused_Walk_Forward natural loop review exceeded 30 seconds.");
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(StateName))
                return;
            maximumNormalizedTime = Mathf.Max(
                maximumNormalizedTime,
                state.normalizedTime);
            RequireRootUnchanged();
            if (Panels.Count >= CaptureThresholds.Length ||
                state.normalizedTime < CaptureThresholds[Panels.Count])
                return;
            Panels.Add(RenderTarget());
            if (Panels.Count == CaptureThresholds.Length)
                FinishCapture();
        }

        private static void RequireRootUnchanged()
        {
            maximumRootPositionError = Mathf.Max(
                maximumRootPositionError,
                Vector3.Distance(initialLocalPosition, target.transform.localPosition));
            maximumRootRotationError = Mathf.Max(
                maximumRootRotationError,
                Quaternion.Angle(initialLocalRotation, target.transform.localRotation));
            float scaleError = Vector3.Distance(
                initialLocalScale,
                target.transform.localScale);
            if (maximumRootPositionError > 0.0001f ||
                maximumRootRotationError > 0.01f ||
                scaleError > 0.0001f)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward root changed during natural playback.");
        }

        private static Texture2D RenderTarget()
        {
            const int width = 300;
            const int height = 420;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            GameObject cameraObject = new GameObject(
                "ConfusedWalkForward_ReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "ConfusedWalkForward_ReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] others = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] otherStates = others
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer renderer in others)
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
                camera.allowMSAA = false;
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
                texture = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);
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
                {
                    if (others[index] != null)
                        others[index].forceRenderingOff = otherStates[index];
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
            Texture2D sheet = CombinePanels();
            try
            {
                ConfusedAnimationSetupTools.WriteFinalEvidence(
                    sheet,
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                    maximumNormalizedTime,
                    maximumRootPositionError,
                    maximumRootRotationError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != CaptureThresholds.Length)
                throw new InvalidOperationException(
                    "Confused walk review requires six frames.");
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
                    (1 - index / 3) * height,
                    width,
                    height,
                    Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Confused_Walk_Forward natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
            {
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            }
            Panels.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            animator = null;
            maximumNormalizedTime = 0f;
            maximumRootPositionError = 0f;
            maximumRootRotationError = 0f;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class ConfusedWalkForwardZAlignmentPlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.ConfusedWalkForwardZAlignment.Pending";
        private const string StateKey =
            "Bellerophon.ConfusedWalkForwardZAlignment.State";
        private const string FailureKey =
            "Bellerophon.ConfusedWalkForwardZAlignment.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.ConfusedWalkForwardZAlignment.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string StateName = "ConfusedWalkForward_Source";
        private static readonly float[] CaptureThresholds =
        {
            0.06f, 0.24f, 0.42f, 0.60f, 0.78f, 1.06f
        };
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static GameObject reference;
        private static Animator animator;
        private static Transform targetAnchor;
        private static Transform referenceAnchor;
        private static Component alignment;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static double startedAt;
        private static float maximumNormalizedTime;
        private static float maximumVisualAnchorZError;
        private static float maximumTargetXYPositionError;
        private static float maximumTargetRotationError;
        private static float maximumTargetScaleError;
        private static float minimumTargetRootWorldZ;
        private static float maximumTargetRootWorldZ;

        static ConfusedWalkForwardZAlignmentPlayModeCapture()
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
                    "Confused_Walk_Forward Z-alignment review must start in Edit Mode.");
            ConfusedAnimationSetupTools.InspectConfusedWalkForwardZAlignment();
            ConfusedAnimationSetupTools.InspectConfiguredVisualZAlignment();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
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
                    "Confused_Walk_Forward Z-alignment review has no pending state.");
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
                            "Play Mode ended before Z-alignment review completed.");
                    if (EditorApplication.timeSinceStartup - startedAt > 30d)
                        throw new TimeoutException(
                            "Confused_Walk_Forward full-loop visual Z review exceeded 30 seconds.");
                    int correctionCount = (int)(alignment.GetType()
                        .GetProperty("CorrectionCount")?.GetValue(alignment) ?? 0);
                    if (correctionCount == 0)
                        return;
                    InspectRuntimeAlignment();
                    AnimatorStateInfo animationState =
                        animator.GetCurrentAnimatorStateInfo(0);
                    if (!animationState.IsName(StateName))
                        return;
                    maximumNormalizedTime = Mathf.Max(
                        maximumNormalizedTime,
                        animationState.normalizedTime);
                    if (Panels.Count >= CaptureThresholds.Length ||
                        animationState.normalizedTime < CaptureThresholds[Panels.Count])
                        return;
                    Panels.Add(RenderComparison());
                    if (Panels.Count < CaptureThresholds.Length)
                        return;
                    Texture2D image = CombinePanels();
                    try
                    {
                        ConfusedAnimationSetupTools.WriteZAlignmentFinalEvidence(
                            image,
                            SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                            maximumNormalizedTime,
                            maximumVisualAnchorZError,
                            maximumTargetXYPositionError,
                            maximumTargetRotationError,
                            maximumTargetScaleError,
                            minimumTargetRootWorldZ,
                            maximumTargetRootWorldZ);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(image);
                        CleanupPanels();
                    }
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }

                ConfusedAnimationSetupTools.InspectConfusedWalkForwardZAlignment();
                ConfusedAnimationSetupTools.InspectConfiguredVisualZAlignment();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Confused_Walk_Forward and Fatigue_HeadShake kept matching visual " +
                    "Hips Z anchors through one full direct Play Mode loop.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
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
            target = ConfusedAnimationSetupTools.RequireRuntimeTarget();
            reference =
                ConfusedAnimationSetupTools.RequireRuntimeZAlignmentReference();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Confused_Walk_Forward Animator is missing in Play Mode.");
            targetAnchor = target.transform.Find("Armature/Hips") ??
                throw new InvalidOperationException(
                    "Confused_Walk_Forward Armature/Hips is missing in Play Mode.");
            referenceAnchor = reference.transform.Find("Armature/Hips") ??
                throw new InvalidOperationException(
                    "Fatigue_HeadShake Armature/Hips is missing in Play Mode.");
            Type alignmentType = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .SingleOrDefault(candidate =>
                    candidate.FullName ==
                    "Bellerophon.Animation.ConfusedWalkForwardVisualZAlignment") ??
                throw new InvalidOperationException(
                    "Confused walk visual Z alignment type is unavailable in Play Mode.");
            alignment = target.GetComponent(alignmentType) ??
                throw new InvalidOperationException(
                    "Confused_Walk_Forward visual Z alignment component is missing in Play Mode.");
            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            startedAt = EditorApplication.timeSinceStartup;
            maximumNormalizedTime = 0f;
            maximumVisualAnchorZError = 0f;
            maximumTargetXYPositionError = 0f;
            maximumTargetRotationError = 0f;
            maximumTargetScaleError = 0f;
            minimumTargetRootWorldZ = float.PositiveInfinity;
            maximumTargetRootWorldZ = float.NegativeInfinity;
        }

        private static void InspectRuntimeAlignment()
        {
            maximumVisualAnchorZError = Mathf.Max(
                maximumVisualAnchorZError,
                Mathf.Abs(
                    targetAnchor.position.z - referenceAnchor.position.z));
            maximumTargetXYPositionError = Mathf.Max(
                maximumTargetXYPositionError,
                Vector2.Distance(
                    new Vector2(initialLocalPosition.x, initialLocalPosition.y),
                    new Vector2(
                        target.transform.localPosition.x,
                        target.transform.localPosition.y)));
            maximumTargetRotationError = Mathf.Max(
                maximumTargetRotationError,
                Quaternion.Angle(
                    initialLocalRotation,
                    target.transform.localRotation));
            maximumTargetScaleError = Mathf.Max(
                maximumTargetScaleError,
                Vector3.Distance(
                    initialLocalScale,
                    target.transform.localScale));
            minimumTargetRootWorldZ = Mathf.Min(
                minimumTargetRootWorldZ,
                target.transform.position.z);
            maximumTargetRootWorldZ = Mathf.Max(
                maximumTargetRootWorldZ,
                target.transform.position.z);
            if (maximumVisualAnchorZError > 0.0001f ||
                maximumTargetXYPositionError > 0.0001f ||
                maximumTargetRotationError > 0.01f ||
                maximumTargetScaleError > 0.0001f)
                throw new InvalidOperationException(
                    "Confused_Walk_Forward visual Hips Z alignment or preserved non-Z root properties changed in Play Mode.");
        }

        private static Texture2D RenderComparison()
        {
            const int width = 600;
            const int height = 300;
            Renderer[] targetRenderers = target
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            Renderer[] referenceRenderers = reference
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            Renderer[] reviewedRenderers = targetRenderers
                .Concat(referenceRenderers)
                .ToArray();
            if (reviewedRenderers.Length == 0)
                throw new InvalidOperationException(
                    "Z-alignment comparison has no enabled renderers.");
            Bounds bounds = reviewedRenderers[0].bounds;
            foreach (Renderer renderer in reviewedRenderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            GameObject guide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guide.name = "ConfusedWalkForwardZAlignment_Guide";
            guide.hideFlags = HideFlags.HideAndDontSave;
            Collider guideCollider = guide.GetComponent<Collider>();
            if (guideCollider != null)
                UnityEngine.Object.DestroyImmediate(guideCollider);
            float minimumX = Mathf.Min(
                target.transform.position.x,
                reference.transform.position.x) - 1.2f;
            float maximumX = Mathf.Max(
                target.transform.position.x,
                reference.transform.position.x) + 1.2f;
            guide.transform.position = new Vector3(
                (minimumX + maximumX) * 0.5f,
                referenceAnchor.position.y,
                referenceAnchor.position.z);
            guide.transform.localScale = new Vector3(
                maximumX - minimumX,
                0.05f,
                0.025f);
            Material guideMaterial = null;
            Renderer guideRenderer = guide.GetComponent<Renderer>();
            Shader guideShader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default");
            if (guideShader != null)
            {
                guideMaterial = new Material(guideShader)
                {
                    color = new Color(1f, 0.1f, 0.75f, 1f),
                    hideFlags = HideFlags.HideAndDontSave
                };
                guideRenderer.sharedMaterial = guideMaterial;
            }

            GameObject cameraObject = new GameObject(
                "ConfusedWalkForwardZAlignment_ReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "ConfusedWalkForwardZAlignment_ReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] others = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer =>
                    !renderer.transform.IsChildOf(target.transform) &&
                    !renderer.transform.IsChildOf(reference.transform) &&
                    renderer != guideRenderer)
                .ToArray();
            bool[] otherStates = others
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer renderer in others)
                    renderer.forceRenderingOff = true;
                Vector3 viewDirection =
                    (Vector3.up * 1.25f + Vector3.forward * 0.72f).normalized;
                float distance = Mathf.Max(8f, bounds.extents.magnitude * 3f);
                Vector3 focus = new Vector3(
                    bounds.center.x,
                    Mathf.Max(bounds.center.y, 0.85f),
                    referenceAnchor.position.z);
                camera.transform.position = focus + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.35f,
                    bounds.extents.x / aspect * 1.20f,
                    2.4f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f, 1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
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
                texture = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);
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
                {
                    if (others[index] != null)
                        others[index].forceRenderingOff = otherStates[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(guide);
                if (guideMaterial != null)
                    UnityEngine.Object.DestroyImmediate(guideMaterial);
            }
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != CaptureThresholds.Length)
                throw new InvalidOperationException(
                    "Visual Z alignment review requires six full-loop phases.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(
                width * 3,
                height * 2,
                TextureFormat.RGBA32,
                false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(6, 9, 14, 255),
                sheet.width * sheet.height).ToArray());
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(
                    index % 3 * width,
                    (1 - index / 3) * height,
                    width,
                    height,
                    Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
            {
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            }
            Panels.Clear();
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Confused_Walk_Forward Z-alignment Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            reference = null;
            animator = null;
            targetAnchor = null;
            referenceAnchor = null;
            alignment = null;
            maximumNormalizedTime = 0f;
            maximumVisualAnchorZError = 0f;
            maximumTargetXYPositionError = 0f;
            maximumTargetRotationError = 0f;
            maximumTargetScaleError = 0f;
            minimumTargetRootWorldZ = 0f;
            maximumTargetRootWorldZ = 0f;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }
}

namespace Bellerophon.Editor.Validation
{
    internal static class ConfusedWalkForwardLocomotionSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Confused_Walk_Forward";
        private const string ReferenceName = "Fatigue_HeadShake";
        private const string SourcePath =
            "Assets/_Project/Animations/PlayerStatusEffects/Sources/Confused_Walk_Forward_Source.fbx";
        private const string OutputFolder =
            "Assets/_Project/Animations/PlayerStatusEffects/ConfusedWalkForward";
        private const string IdlePath = OutputFolder + "/Lower_Idle.anim";
        private const string ForwardPath = OutputFolder + "/Lower_Forward.anim";
        private const string BackwardPath = OutputFolder + "/Lower_Backward.anim";
        private const string UpperMaskPath = OutputFolder + "/UpperBody.mask";
        private const string ControllerPath =
            "Assets/_Project/Animations/PlayerStatusEffects/Confused_Walk_Forward.controller";
        private const string BaseStateName = "ConfusedLowerLocomotion2D";
        private const string UpperStateName = "ConfusedUpperSource";
        private const string BaseLayerName = "Player Lower Body 2D";
        private const string UpperLayerName = "Confused Upper Body";
        private const string TreeName = "ConfusedLowerLocomotion2DTree";
        private const string MoveX = "ConfusedMoveX";
        private const string MoveY = "ConfusedMoveY";
        private const string CycleBehaviourTypeName =
            "Bellerophon.PlayerAnimation.ConfusedWalkForwardLocomotionCycleBehaviour";
        internal const string FinalImagePath =
            "docs/validation/ConfusedWalkForwardLocomotion/Final.png";

        private static readonly string[] SourceNames =
        {
            "Player_Idle",
            "Player_Walk_Forward",
            "Player_Walk_Backward"
        };

        private static readonly string[] CopyPaths =
        {
            IdlePath,
            ForwardPath,
            BackwardPath
        };

        private static readonly Vector2[] Positions =
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f)
        };

        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target, TargetName);
            TransformSnapshot root = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Avatar avatar = animator.avatar;
            AnimationClip confusedSource = RequireSingleClip(SourcePath);
            AnimationClip[] sourceClips = SourceNames
                .Select(name => RequireDefaultClip(scene, name))
                .ToArray();

            ResetOutputFolder();
            AnimationClip[] copies = new AnimationClip[sourceClips.Length];
            for (int index = 0; index < sourceClips.Length; index++)
                copies[index] = CreateExactCopy(sourceClips[index], CopyPaths[index]);
            AvatarMask upperMask = CreateUpperBodyMask(target.transform);
            AnimatorController controller = ConfigureController(
                confusedSource,
                copies,
                upperMask);

            Undo.RecordObject(animator, "Connect confused locomotion Blend Tree");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            root.RequireUnchanged(target.transform, TargetName);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                TargetName + " renderer signature");
            if (animator.avatar != avatar)
                throw new InvalidOperationException(
                    TargetName + " Avatar changed unexpectedly.");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying confused locomotion.");
            AssetDatabase.SaveAssets();
            Inspect();
            Debug.Log(
                "[ConfusedWalkForwardLocomotion] Applied exact Player Idle/Forward/Backward copies to FreeformCartesian2D at (0,0)/(0,1)/(0,-1), one second each, while the unchanged confused source continues on the Spine02 upper-body layer. sourceCurvesGenerated=False,poseInference=False,cycleSeconds=3.");
        }

        internal static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target, TargetName);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(ControllerPath + " is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(
                    TargetName + " controller connection differs.");
            if (animator.applyRootMotion)
                throw new InvalidOperationException(
                    TargetName + " applyRootMotion must remain disabled.");
            if (controller.layers.Length != 2)
                throw new InvalidOperationException(
                    "Confused locomotion controller must contain two layers.");
            RequireFloatParameter(controller, MoveX);
            RequireFloatParameter(controller, MoveY);

            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorState baseState = RequireSingleDefaultState(
                baseLayer,
                BaseStateName);
            BlendTree tree = baseState.motion as BlendTree ??
                throw new InvalidOperationException(
                    "Confused lower locomotion state must use a Blend Tree.");
            if (baseLayer.name != BaseLayerName ||
                baseLayer.avatarMask != null ||
                baseLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(baseLayer.defaultWeight, 1f) ||
                tree.name != TreeName ||
                tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.blendParameter != MoveX ||
                tree.blendParameterY != MoveY ||
                tree.children.Length != 3 ||
                !Mathf.Approximately(baseState.speed, 1f) ||
                baseState.transitions.Length != 0 ||
                baseState.behaviours.Count(behaviour =>
                    behaviour != null &&
                    behaviour.GetType().FullName == CycleBehaviourTypeName) != 1)
                throw new InvalidOperationException(
                    "Confused lower locomotion layer differs from the approved definition.");

            ChildMotion[] children = tree.children;
            for (int index = 0; index < children.Length; index++)
            {
                if ((children[index].position - Positions[index]).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(children[index].timeScale, 1f) ||
                    !Mathf.Approximately(children[index].cycleOffset, 0f) ||
                    children[index].mirror)
                    throw new InvalidOperationException(
                        "Confused lower Blend Tree child differs at " + index + ".");
                AnimationClip source = RequireDefaultClip(scene, SourceNames[index]);
                AnimationClip copy =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(CopyPaths[index]) ??
                    throw new InvalidOperationException(CopyPaths[index] + " is missing.");
                if (children[index].motion != copy)
                    throw new InvalidOperationException(
                        "Confused lower Blend Tree child reference differs at " + index + ".");
                RequireEqual(
                    ClipSignature(source),
                    ClipSignature(copy),
                    SourceNames[index] + " exact clip copy");
            }

            AnimatorControllerLayer upperLayer = controller.layers[1];
            AnimatorState upperState = RequireSingleDefaultState(
                upperLayer,
                UpperStateName);
            AvatarMask upperMask =
                AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperMaskPath) ??
                throw new InvalidOperationException(UpperMaskPath + " is missing.");
            AnimationClip confusedSource = RequireSingleClip(SourcePath);
            if (upperLayer.name != UpperLayerName ||
                upperLayer.avatarMask != upperMask ||
                upperLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(upperLayer.defaultWeight, 1f) ||
                upperState.motion != confusedSource ||
                !Mathf.Approximately(upperState.speed, 1f) ||
                upperState.transitions.Length != 0)
                throw new InvalidOperationException(
                    "Confused continuous upper-body layer differs.");
            RequireUpperBodyMask(target.transform, upperMask);
            if (AnimationUtility.GetCurveBindings(confusedSource)
                    .Count(binding => IsInUpperBranch(binding.path, UpperBranchPath(target.transform))) == 0)
                throw new InvalidOperationException(
                    "Confused source has no Spine02 upper-body curves.");

            ConfusedAnimationSetupTools.InspectConfiguredVisualZAlignment();
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[ConfusedWalkForwardLocomotion] Structural inspection passed. blendTreeType=FreeformCartesian2D,positions=(0,0)|(0,1)|(0,-1),sequence=Idle|WalkForward|WalkBackward,secondsPerMotion=1,cycleSeconds=3,playerMotionCopiesExact=True,confusedUpperSourceDirect=True,continuousUpperLayer=True,upperMaskBranch=Spine02,visualZAlignmentPreserved=True,unityConsoleErrors=0.");
        }

        internal static GameObject RequireRuntimeTarget() =>
            FindUnique(RequireScene(), TargetName);

        internal static GameObject RequireRuntimeReference() =>
            FindUnique(RequireScene(), ReferenceName);

        internal static void WriteFinalEvidence(
            Texture2D image,
            int consoleErrorsBefore,
            float maximumVisualZError,
            float maximumRootXYError,
            float maximumRootRotationError,
            float maximumRootScaleError,
            float upperPoseMotionDegrees,
            int observedPhaseMask,
            int observedAbsolutePhase)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if ((observedPhaseMask & 0b111) != 0b111 || observedAbsolutePhase < 3)
                throw new InvalidOperationException(
                    "Confused locomotion direct review did not observe idle, forward, backward, and repeat idle.");
            if (maximumVisualZError > 0.0001f ||
                maximumRootXYError > 0.0001f ||
                maximumRootRotationError > 0.01f ||
                maximumRootScaleError > 0.0001f)
                throw new InvalidOperationException(
                    "Confused locomotion changed preserved target alignment properties.");
            if (upperPoseMotionDegrees < 0.01f)
                throw new InvalidOperationException(
                    "Confused upper-body source did not visibly continue moving.");
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter > consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Confused locomotion review introduced Unity console errors. Before=" +
                    consoleErrorsBefore + ", After=" + consoleErrorsAfter + ".");

            string absolute = Absolute(FinalImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Confused locomotion validation folder is unavailable."));
            File.WriteAllBytes(absolute, image.EncodeToPNG());
            Debug.Log(
                "[ConfusedWalkForwardLocomotion] Direct Play Mode review captured once. observedPhases=Idle|WalkForward|WalkBackward|IdleRepeat,maximumVisualZError=" +
                F(maximumVisualZError) + ",maximumRootXYError=" +
                F(maximumRootXYError) + ",maximumRootRotationError=" +
                F(maximumRootRotationError) + ",maximumRootScaleError=" +
                F(maximumRootScaleError) + ",upperPoseMotionDegrees=" +
                F(upperPoseMotionDegrees) + ",consoleErrorsBefore=" +
                consoleErrorsBefore + ",consoleErrorsAfter=" + consoleErrorsAfter +
                ",finalImage=" + FinalImagePath);
        }

        private static AnimatorController ConfigureController(
            AnimationClip confusedSource,
            AnimationClip[] copies,
            AvatarMask upperMask)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(ControllerPath + " is missing.");
            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                .Where(asset => asset != null && asset != controller)
                .ToArray();
            controller.layers = Array.Empty<AnimatorControllerLayer>();
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (UnityEngine.Object subAsset in subAssets)
                UnityEngine.Object.DestroyImmediate(subAsset, true);

            controller.AddParameter(MoveX, AnimatorControllerParameterType.Float);
            controller.AddParameter(MoveY, AnimatorControllerParameterType.Float);

            var baseMachine = new AnimatorStateMachine { name = BaseLayerName };
            AssetDatabase.AddObjectToAsset(baseMachine, controller);
            var baseLayer = new AnimatorControllerLayer
            {
                name = BaseLayerName,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                avatarMask = null,
                iKPass = false,
                stateMachine = baseMachine
            };
            var tree = new BlendTree
            {
                name = TreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = MoveX,
                blendParameterY = MoveY,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.children = copies.Select((clip, index) => new ChildMotion
            {
                motion = clip,
                position = Positions[index],
                timeScale = 1f,
                cycleOffset = 0f,
                mirror = false,
                threshold = 0f,
                directBlendParameter = string.Empty
            }).ToArray();
            AnimatorState baseState = baseMachine.AddState(BaseStateName);
            baseState.motion = tree;
            baseState.speed = 1f;
            baseState.cycleOffset = 0f;
            baseState.mirror = false;
            baseState.writeDefaultValues = false;
            baseState.AddStateMachineBehaviour(RequireCycleBehaviourType());
            baseMachine.defaultState = baseState;

            var upperMachine = new AnimatorStateMachine { name = UpperLayerName };
            AssetDatabase.AddObjectToAsset(upperMachine, controller);
            var upperLayer = new AnimatorControllerLayer
            {
                name = UpperLayerName,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                avatarMask = upperMask,
                iKPass = false,
                stateMachine = upperMachine
            };
            AnimatorState upperState = upperMachine.AddState(UpperStateName);
            upperState.motion = confusedSource;
            upperState.speed = 1f;
            upperState.cycleOffset = 0f;
            upperState.mirror = false;
            upperState.writeDefaultValues = false;
            upperMachine.defaultState = upperState;
            controller.layers = new[] { baseLayer, upperLayer };
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(baseMachine);
            EditorUtility.SetDirty(upperMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip CreateExactCopy(
            AnimationClip source,
            string path)
        {
            var copy = new AnimationClip();
            EditorUtility.CopySerialized(source, copy);
            copy.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy, path);
            RequireEqual(
                ClipSignature(source),
                ClipSignature(copy),
                path + " exact copy");
            return copy;
        }

        private static AvatarMask CreateUpperBodyMask(Transform target)
        {
            var mask = new AvatarMask { name = "Confused_UpperBody" };
            string upperPath = UpperBranchPath(target);
            string[] paths = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item != target)
                .Select(item => AnimationUtility.CalculateTransformPath(item, target))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path.Count(character => character == '/'))
                .ThenBy(path => path, StringComparer.Ordinal)
                .ToArray();
            mask.transformCount = paths.Length;
            for (int index = 0; index < paths.Length; index++)
            {
                mask.SetTransformPath(index, paths[index]);
                mask.SetTransformActive(index, IsInUpperBranch(paths[index], upperPath));
            }
            for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            AssetDatabase.CreateAsset(mask, UpperMaskPath);
            return mask;
        }

        private static void RequireUpperBodyMask(Transform target, AvatarMask mask)
        {
            string upperPath = UpperBranchPath(target);
            for (int index = 0; index < mask.transformCount; index++)
            {
                string path = mask.GetTransformPath(index);
                if (mask.GetTransformActive(index) != IsInUpperBranch(path, upperPath))
                    throw new InvalidOperationException(
                        "Confused upper-body mask differs at " + path + ".");
            }
        }

        private static string UpperBranchPath(Transform target)
        {
            Transform[] matches = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == "Spine02")
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected exactly one Spine02 under Confused_Walk_Forward.");
            return AnimationUtility.CalculateTransformPath(matches[0], target);
        }

        private static bool IsInUpperBranch(string path, string upperPath) =>
            path == upperPath || path.StartsWith(upperPath + "/", StringComparison.Ordinal);

        private static void ResetOutputFolder()
        {
            if (AssetDatabase.IsValidFolder(OutputFolder) &&
                !AssetDatabase.DeleteAsset(OutputFolder))
                throw new InvalidOperationException(
                    "Could not reset " + OutputFolder + ".");
            const string parent =
                "Assets/_Project/Animations/PlayerStatusEffects";
            string guid = AssetDatabase.CreateFolder(parent, "ConfusedWalkForward");
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException(
                    "Could not create " + OutputFolder + ".");
        }

        private static AnimationClip RequireDefaultClip(Scene scene, string objectName)
        {
            Animator animator = RequireAnimator(FindUnique(scene, objectName), objectName);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(
                    objectName + " must use an AnimatorController.");
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException(
                    objectName + " default state is missing.");
            return state.motion as AnimationClip ??
                throw new InvalidOperationException(
                    objectName + " default motion must be one AnimationClip.");
        }

        private static AnimationClip RequireSingleClip(string path)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    path + " must contain exactly one embedded clip; actual=" + clips.Length + ".");
            return clips[0];
        }

        private static AnimatorState RequireSingleDefaultState(
            AnimatorControllerLayer layer,
            string expectedName)
        {
            AnimatorState[] states = layer.stateMachine.states
                .Select(item => item.state)
                .ToArray();
            if (states.Length != 1 ||
                layer.stateMachine.defaultState != states[0] ||
                states[0].name != expectedName)
                throw new InvalidOperationException(
                    layer.name + " must contain one default state named " + expectedName + ".");
            return states[0];
        }

        private static void RequireFloatParameter(
            AnimatorController controller,
            string name)
        {
            AnimatorControllerParameter[] matches = controller.parameters
                .Where(parameter => parameter.name == name)
                .ToArray();
            if (matches.Length != 1 ||
                matches[0].type != AnimatorControllerParameterType.Float)
                throw new InvalidOperationException(
                    "Confused controller float parameter differs: " + name + ".");
        }

        private static Type RequireCycleBehaviourType()
        {
            Type type = TypeCache.GetTypesDerivedFrom<StateMachineBehaviour>()
                .SingleOrDefault(candidate =>
                    candidate.FullName == CycleBehaviourTypeName);
            return type ?? throw new InvalidOperationException(
                "Confused locomotion cycle behaviour type is unavailable.");
        }

        private static string ClipSignature(AnimationClip clip)
        {
            var result = new System.Text.StringBuilder()
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
                result.AppendLine(
                    "event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + F(item.floatParameter) + "|" +
                    item.intParameter);
            return result.ToString();
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
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
                    "Expected exactly one " + name + "; actual=" + matches.Length + ".");
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject target, string label) =>
            target.GetComponent<Animator>() ??
            throw new InvalidOperationException(label + " Animator is missing.");

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Confused locomotion setup requires Edit Mode.");
        }

        private static string RendererSignature(Transform root) =>
            string.Join(
                "\n",
                root.GetComponentsInChildren<Renderer>(true)
                    .OrderBy(renderer => AnimationUtility.CalculateTransformPath(
                        renderer.transform, root), StringComparer.Ordinal)
                    .Select(renderer => AnimationUtility.CalculateTransformPath(
                        renderer.transform, root) + "|" +
                        renderer.GetType().FullName + "|" + renderer.enabled));

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable."),
                relativePath));

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " differs.");
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
                if (transform.localPosition != position ||
                    transform.localRotation != rotation ||
                    transform.localScale != scale)
                    throw new InvalidOperationException(
                        label + " root Transform changed unexpectedly.");
            }
        }
    }

    [InitializeOnLoad]
    internal static class ConfusedWalkForwardLocomotionPlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.ConfusedWalkForwardLocomotion.Pending";
        private const string StateKey =
            "Bellerophon.ConfusedWalkForwardLocomotion.State";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.ConfusedWalkForwardLocomotion.ConsoleErrorsBefore";
        private const string FailureKey =
            "Bellerophon.ConfusedWalkForwardLocomotion.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditModeAfterSuccess = 3;
        private const int WaitingForEditModeAfterFailure = 4;

        private const int RequiredPanelCount = 4;
        private const float StablePhaseSeconds = 0.3f;
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static GameObject reference;
        private static Animator animator;
        private static Transform targetHips;
        private static Transform referenceHips;
        private static Transform upper;
        private static Transform[] upperTransforms;
        private static Quaternion[] initialUpperRotations;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static double startedAt;
        private static float maximumVisualZError;
        private static float maximumRootXYError;
        private static float maximumRootRotationError;
        private static float maximumRootScaleError;
        private static float maximumUpperPoseMotion;
        private static float previousUpperNormalizedTime;
        private static int observedPhaseMask;
        private static int observedAbsolutePhase;
        private static int lastObservedPhase;
        private static int reviewSequenceIndex;
        private static double phaseChangedAt;

        internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleCapture()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Confused locomotion review must start in Edit Mode.");
            ConfusedWalkForwardLocomotionSetupTools.Inspect();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Confused locomotion review has no pending state.");
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
                            "Play Mode ended before confused locomotion review completed.");
                    float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);
                    if (elapsed > 20f)
                        throw new TimeoutException(
                            "Confused locomotion direct review exceeded 20 seconds.");
                    InspectRuntime();
                    if (reviewSequenceIndex == Panels.Count &&
                        Panels.Count < RequiredPanelCount &&
                        EditorApplication.timeSinceStartup - phaseChangedAt >=
                            StablePhaseSeconds)
                        Panels.Add(RenderTarget());
                    if (Panels.Count < RequiredPanelCount || reviewSequenceIndex < 3)
                        return;
                    Texture2D image = CombinePanels();
                    try
                    {
                        ConfusedWalkForwardLocomotionSetupTools.WriteFinalEvidence(
                            image,
                            SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                            maximumVisualZError,
                            maximumRootXYError,
                            maximumRootRotationError,
                            maximumRootScaleError,
                            maximumUpperPoseMotion,
                            observedPhaseMask,
                            observedAbsolutePhase);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(image);
                        CleanupPanels();
                    }
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                ConfusedWalkForwardLocomotionSetupTools.Inspect();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Confused_Walk_Forward idle, forward, backward, and repeat-idle phases were directly captured in Play Mode with continuous confused upper-body motion and preserved visual Z alignment.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
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
            target = ConfusedWalkForwardLocomotionSetupTools.RequireRuntimeTarget();
            reference = ConfusedWalkForwardLocomotionSetupTools.RequireRuntimeReference();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Confused_Walk_Forward Animator is missing in Play Mode.");
            targetHips = target.transform.Find("Armature/Hips") ??
                throw new InvalidOperationException(
                    "Confused_Walk_Forward Hips is missing in Play Mode.");
            referenceHips = reference.transform.Find("Armature/Hips") ??
                throw new InvalidOperationException(
                    "Fatigue_HeadShake Hips is missing in Play Mode.");
            upper = target.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "Spine02");
            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            upperTransforms = upper.GetComponentsInChildren<Transform>(true);
            initialUpperRotations = upperTransforms
                .Select(item => item.localRotation)
                .ToArray();
            startedAt = EditorApplication.timeSinceStartup;
            maximumVisualZError = 0f;
            maximumRootXYError = 0f;
            maximumRootRotationError = 0f;
            maximumRootScaleError = 0f;
            maximumUpperPoseMotion = 0f;
            previousUpperNormalizedTime = -1f;
            observedPhaseMask = 0;
            observedAbsolutePhase = 0;
            lastObservedPhase = -1;
            reviewSequenceIndex = -1;
            phaseChangedAt = EditorApplication.timeSinceStartup;
        }

        private static void InspectRuntime()
        {
            float moveX = animator.GetFloat("ConfusedMoveX");
            float moveY = animator.GetFloat("ConfusedMoveY");
            if (Mathf.Abs(moveX) > 0.0001f)
                return;
            int phase;
            if (Mathf.Abs(moveY) <= 0.0001f)
                phase = 0;
            else if (Mathf.Abs(moveY - 1f) <= 0.0001f)
                phase = 1;
            else if (Mathf.Abs(moveY + 1f) <= 0.0001f)
                phase = 2;
            else
                return;
            if (phase != lastObservedPhase)
            {
                lastObservedPhase = phase;
                phaseChangedAt = EditorApplication.timeSinceStartup;
                if (reviewSequenceIndex < 0)
                {
                    if (phase == 0)
                        reviewSequenceIndex = 0;
                }
                else
                {
                    int expectedPhase = (reviewSequenceIndex + 1) % 3;
                    if (phase == expectedPhase)
                        reviewSequenceIndex++;
                    else if (phase == 0)
                        reviewSequenceIndex = 0;
                    else
                        reviewSequenceIndex = -1;
                }
            }
            if (reviewSequenceIndex < 0)
                return;
            observedAbsolutePhase = Mathf.Max(
                observedAbsolutePhase,
                reviewSequenceIndex);
            observedPhaseMask |= 1 << phase;
            AnimatorStateInfo upperState = animator.GetCurrentAnimatorStateInfo(1);
            if (!upperState.IsName("ConfusedUpperSource"))
                throw new InvalidOperationException(
                    "Confused upper-body layer left its continuous source state.");
            if (previousUpperNormalizedTime >= 0f &&
                upperState.normalizedTime + 0.001f < previousUpperNormalizedTime)
                throw new InvalidOperationException(
                    "Confused upper-body source restarted during lower-body phase switching.");
            previousUpperNormalizedTime = upperState.normalizedTime;
            for (int index = 0; index < upperTransforms.Length; index++)
                maximumUpperPoseMotion = Mathf.Max(
                    maximumUpperPoseMotion,
                    Quaternion.Angle(
                        initialUpperRotations[index],
                        upperTransforms[index].localRotation));
            maximumVisualZError = Mathf.Max(
                maximumVisualZError,
                Mathf.Abs(targetHips.position.z - referenceHips.position.z));
            maximumRootXYError = Mathf.Max(
                maximumRootXYError,
                Vector2.Distance(
                    new Vector2(initialLocalPosition.x, initialLocalPosition.y),
                    new Vector2(
                        target.transform.localPosition.x,
                        target.transform.localPosition.y)));
            maximumRootRotationError = Mathf.Max(
                maximumRootRotationError,
                Quaternion.Angle(initialLocalRotation, target.transform.localRotation));
            maximumRootScaleError = Mathf.Max(
                maximumRootScaleError,
                Vector3.Distance(initialLocalScale, target.transform.localScale));
        }

        private static Texture2D RenderTarget()
        {
            const int width = 420;
            const int height = 520;
            Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (targetRenderers.Length == 0)
                throw new InvalidOperationException(
                    "Confused locomotion target has no enabled renderers.");
            Bounds bounds = targetRenderers[0].bounds;
            foreach (Renderer renderer in targetRenderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            GameObject cameraObject = new GameObject(
                "ConfusedLocomotionReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "ConfusedLocomotionReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            Renderer[] others = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] states = others.Select(renderer => renderer.forceRenderingOff).ToArray();
            RenderTexture renderTexture = null;
            Texture2D result = null;
            try
            {
                foreach (Renderer renderer in others)
                    renderer.forceRenderingOff = true;
                Vector3 focus = bounds.center;
                Vector3 direction = (Vector3.forward + Vector3.up * 0.12f).normalized;
                float distance = Mathf.Max(6f, bounds.extents.magnitude * 3f);
                camera.transform.position = focus + direction * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.2f, 1.6f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                result = new Texture2D(width, height, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return result;
            }
            catch
            {
                if (result != null)
                    UnityEngine.Object.DestroyImmediate(result);
                throw;
            }
            finally
            {
                for (int index = 0; index < others.Length; index++)
                {
                    if (others[index] != null)
                        others[index].forceRenderingOff = states[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != RequiredPanelCount)
                throw new InvalidOperationException(
                    "Confused locomotion review requires four phase panels.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(width * 4, height, TextureFormat.RGBA32, false);
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(index * width, 0, width, height, Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
            {
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            }
            Panels.Clear();
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Confused locomotion Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            reference = null;
            animator = null;
            targetHips = null;
            referenceHips = null;
            upper = null;
            upperTransforms = null;
            initialUpperRotations = null;
            previousUpperNormalizedTime = -1f;
            observedPhaseMask = 0;
            observedAbsolutePhase = 0;
            lastObservedPhase = -1;
            reviewSequenceIndex = -1;
            phaseChangedAt = 0d;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }
}
