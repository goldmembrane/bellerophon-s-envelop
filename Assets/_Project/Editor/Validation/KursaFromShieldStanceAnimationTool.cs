using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaFromShieldStanceAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string StanceSlotName = "Kursa_05_ToShieldStance";
        private const string TargetSlotName = "Kursa_08_FromShieldStance";
        private const string ModelName = "Kursa_Model";
        private const string EffectName = "Kursa_ShieldStanceIcon";
        private const string StanceClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_05_ToShieldStance_Loop.anim";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_08_FromShieldStance_Loop.anim";
        internal const string ControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_08_FromShieldStance.controller";
        private const string ValidationFolder =
            "docs/validation/kursa_from_shield_stance_hold_2026-08-04";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Kursa_FromShieldStanceHold_Diagnostic_{0:00}.png";
        private const string FinalReviewPath =
            ValidationFolder + "/Kursa_FromShieldStanceHold_FinalReview.png";
        private const float StanceCompletionTime = 1f;
        private const float TransitionSeconds = 1.5f;
        private const float HoldSeconds = 1f;
        private const float DurationSeconds = TransitionSeconds + HoldSeconds;
        private const float FrameRate = 60f;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_PostBreakRecovery",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        private static readonly float[] ReviewTimes =
        {
            0f, 0.5f, 1f, TransitionSeconds, 2f,
            DurationSeconds - 1f / FrameRate
        };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply From Shield Stance Animation")]
        public static void ApplyKursaFromShieldStanceAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireDirectChild(
                placement.transform,
                StaticSlotName));
            var stanceModel = RequireModel(RequireDirectChild(
                placement.transform,
                StanceSlotName));
            var targetSlot = RequireDirectChild(placement.transform, TargetSlotName);
            var previousModel = RequireModel(targetSlot);
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var stanceRenderer = RequireRenderer(stanceModel, StanceSlotName);
            var stanceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StanceClipPath) ??
                throw new InvalidOperationException(
                    "The approved Kursa to-shield-stance clip is missing.");
            var sourceEffect = stanceModel.GetComponentsInChildren<SpriteRenderer>(true)
                .SingleOrDefault(item => item.name == EffectName) ??
                throw new InvalidOperationException(
                    "The approved Kursa shield-stance icon is missing from slot 5.");
            RequireExactStaticAppearance(staticRenderer, stanceRenderer, StanceSlotName);

            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var targetSlotTransformBefore = LocalTransformSignature(targetSlot);

            DeleteAssetIfPresent(ControllerPath);
            DeleteAssetIfPresent(ClipPath);

            GameObject replacement = null;
            try
            {
                replacement = UnityEngine.Object.Instantiate(staticModel.gameObject);
                replacement.name = ModelName;
                replacement.transform.SetParent(targetSlot, false);
                replacement.transform.SetLocalPositionAndRotation(
                    staticModel.localPosition,
                    staticModel.localRotation);
                replacement.transform.localScale = staticModel.localScale;

                foreach (var animator in replacement.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
                foreach (var legacy in replacement.GetComponentsInChildren<Animation>(true))
                    UnityEngine.Object.DestroyImmediate(legacy);

                var replacementRenderer = RequireRenderer(
                    replacement.transform,
                    TargetSlotName);
                RequireExactStaticAppearance(
                    staticRenderer,
                    replacementRenderer,
                    TargetSlotName);

                var effectObject = UnityEngine.Object.Instantiate(
                    sourceEffect.gameObject,
                    replacement.transform,
                    false);
                effectObject.name = EffectName;
                var effectRenderer = effectObject.GetComponent<SpriteRenderer>() ??
                    throw new InvalidOperationException(
                        "The copied Kursa shield icon has no SpriteRenderer.");
                var effectColor = effectRenderer.color;
                effectColor.a = 1f;
                effectRenderer.color = effectColor;
                EditorUtility.SetDirty(effectRenderer);

                var clip = CreateReverseTransitionClip(
                    replacement.transform,
                    replacementRenderer,
                    effectRenderer,
                    stanceClip);
                var controller = CreateController(clip);
                var animatorComponent = replacement.AddComponent<Animator>();
                animatorComponent.runtimeAnimatorController = controller;
                animatorComponent.applyRootMotion = false;
                animatorComponent.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animatorComponent.updateMode = AnimatorUpdateMode.Normal;
                animatorComponent.enabled = true;
                animatorComponent.Rebind();
                animatorComponent.Update(0f);
                EditorUtility.SetDirty(animatorComponent);

                RequirePlacedContract(
                    replacement.transform,
                    staticModel,
                    staticRenderer,
                    sourceEffect,
                    clip,
                    controller,
                    stanceClip);

                UnityEngine.Object.DestroyImmediate(previousModel.gameObject);
                replacement = null;

                RequireEqual(
                    otherSlotsBefore,
                    OtherSlotSignatures(placement.transform),
                    "A Kursa slot outside Kursa_08_FromShieldStance changed.");
                RequireEqual(
                    otherRootsBefore,
                    OtherRootSignatures(scene, placement),
                    "A scene root outside the Kursa placement changed.");
                if (targetSlotTransformBefore != LocalTransformSignature(targetSlot))
                    throw new InvalidOperationException(
                        "The Kursa_08_FromShieldStance slot transform changed.");
                RequireSlotContract(placement.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after applying Kursa_08_FromShieldStance.");
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "KursaFromShieldStanceAnimationApplied Result=PASS, " +
                    "Slot=Kursa_08_FromShieldStance, StaticAppearanceCopied=True, " +
                    "StartPose=Kursa_05_Completion, EndPose=Kursa_01_Static, " +
                    "TransitionSeconds=1.5, HoldSeconds=1, DurationSeconds=2.5, " +
                    "ReverseOfApprovedEntry=True, CompletionHold=True, " +
                    "ShieldIconOffDuringHold=True, " +
                    "Loop=True, RootMotion=False, OtherSlotsUnchanged=True, " +
                    "OtherSceneRootsUnchanged=True, SceneSaved=True.");
            }
            catch
            {
                if (replacement != null)
                    UnityEngine.Object.DestroyImmediate(replacement);
                DeleteAssetIfPresent(ControllerPath);
                DeleteAssetIfPresent(ClipPath);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        public static void ClearFailedApplyDirtyState()
        {
            var scene = RequireScene(requireClean: false);
            if (!scene.isDirty)
            {
                Debug.Log("KursaFromShieldStanceFailedApplyDirtyState AlreadyClean=True.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ClipPath) != null ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
            {
                throw new InvalidOperationException(
                    "Slot 8 generated assets still exist; failed-apply dirty state cannot be cleared.");
            }
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var targetModel = RequireModel(RequireDirectChild(
                placement.transform,
                TargetSlotName));
            if (targetModel.GetComponentsInChildren<Animator>(true).Any(item =>
                    AssetDatabase.GetAssetPath(item.runtimeAnimatorController) == ControllerPath))
            {
                throw new InvalidOperationException(
                    "Slot 8 still uses the generated controller; failed-apply dirty state cannot be cleared.");
            }
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the failed slot 8 apply cleanup.");
            Debug.Log(
                "KursaFromShieldStanceFailedApplyDirtyState Cleared=True, " +
                "RestoredSceneStateSaved=True, Slot8GeneratedAssetsAbsent=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture From Shield Stance Diagnostic")]
        public static void CaptureKursaFromShieldStanceDiagnostic()
        {
            CaptureReview(NextDiagnosticPath(), "Diagnostic");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture From Shield Stance Final Review")]
        public static void CaptureKursaFromShieldStanceFinalReview()
        {
            var destination = Absolute(FinalReviewPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa from-shield-stance final review already exists: " +
                    destination);
            CaptureReview(destination, "Final");
        }

        private static AnimationClip CreateReverseTransitionClip(
            Transform model,
            SkinnedMeshRenderer renderer,
            SpriteRenderer effectRenderer,
            AnimationClip stanceClip)
        {
            var skeleton = RequireSkeletonPaths(model, renderer);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var keys = skeleton.ToDictionary(
                item => item.Key,
                item => new TransformCurveKeys(),
                StringComparer.Ordinal);
            var frameCount = Mathf.RoundToInt(DurationSeconds * FrameRate);
            try
            {
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    foreach (var snapshot in snapshots) snapshot.Restore();
                    var targetTime = frame / FrameRate;
                    var normalized = Mathf.Clamp01(
                        targetTime / TransitionSeconds);
                    var sourceTime = Mathf.Lerp(
                        StanceCompletionTime,
                        0f,
                        normalized);
                    stanceClip.SampleAnimation(model.gameObject, sourceTime);
                    foreach (var item in skeleton)
                    {
                        keys[item.Key].Add(
                            targetTime,
                            item.Value.localPosition,
                            item.Value.localRotation);
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
            }

            var clip = new AnimationClip
            {
                name = "Kursa_08_FromShieldStance_Loop",
                frameRate = FrameRate,
                wrapMode = WrapMode.Loop
            };
            foreach (var item in keys.OrderBy(item => item.Key, StringComparer.Ordinal))
                item.Value.Apply(clip, item.Key);

            var effectPath = AnimationUtility.CalculateTransformPath(
                effectRenderer.transform,
                model);
            var effectCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(TransitionSeconds - 1f / FrameRate, 1f),
                new Keyframe(TransitionSeconds, 0f),
                new Keyframe(DurationSeconds, 0f));
            effectCurve.preWrapMode = WrapMode.ClampForever;
            effectCurve.postWrapMode = WrapMode.ClampForever;
            for (var index = 0; index < effectCurve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    effectCurve,
                    index,
                    AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(
                    effectCurve,
                    index,
                    AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    effectPath,
                    typeof(SpriteRenderer),
                    "m_Color.a"),
                effectCurve);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("From Shield Stance");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RequirePlacedContract(
            Transform model,
            Transform staticModel,
            SkinnedMeshRenderer staticRenderer,
            SpriteRenderer sourceEffect,
            AnimationClip clip,
            RuntimeAnimatorController controller,
            AnimationClip stanceClip)
        {
            var renderer = RequireRenderer(model, TargetSlotName);
            RequireExactStaticAppearance(staticRenderer, renderer, TargetSlotName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Kursa_08_FromShieldStance must contain one Animator.");
            if (animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Kursa_08_FromShieldStance Animator configuration differs.");
            }
            var effect = model.GetComponentsInChildren<SpriteRenderer>(true)
                .SingleOrDefault(item => item.name == EffectName) ??
                throw new InvalidOperationException(
                    "Kursa_08_FromShieldStance approved shield icon is missing.");
            if (effect.sprite != sourceEffect.sprite ||
                effect.sharedMaterial != sourceEffect.sharedMaterial)
            {
                throw new InvalidOperationException(
                    "Kursa_08_FromShieldStance shield icon differs from slot 5.");
            }
            if (Mathf.Abs(clip.length - DurationSeconds) > 0.001f)
                throw new InvalidOperationException(
                    "Kursa_08_FromShieldStance duration differs from 2.5 seconds.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || settings.loopBlend)
                throw new InvalidOperationException(
                    "Kursa_08_FromShieldStance must loop without return blending.");

            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                clip.SampleAnimation(model.gameObject, 0f);
                var startPose = CaptureNamedBonePoses(model);
                foreach (var snapshot in snapshots) snapshot.Restore();
                stanceClip.SampleAnimation(model.gameObject, StanceCompletionTime);
                RequirePoseMatch(
                    startPose,
                    CaptureNamedBonePoses(model),
                    "start pose and slot 5 completion pose");

                foreach (var snapshot in snapshots) snapshot.Restore();
                var staticPose = CaptureNamedBonePoses(staticModel);
                clip.SampleAnimation(model.gameObject, TransitionSeconds);
                RequirePoseMatch(
                    staticPose,
                    CaptureNamedBonePoses(model),
                    "end pose and static pose");
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
            }
        }

        private static Dictionary<string, LocalPose> CaptureNamedBonePoses(Transform model) =>
            model.GetComponentsInChildren<Transform>(true)
                .Where(item => item != model && item.name != EffectName)
                .ToDictionary(
                    item => AnimationUtility.CalculateTransformPath(item, model),
                    item => new LocalPose(item),
                    StringComparer.Ordinal);

        private static void RequirePoseMatch(
            IReadOnlyDictionary<string, LocalPose> expected,
            IReadOnlyDictionary<string, LocalPose> actual,
            string context)
        {
            if (expected.Count != actual.Count || expected.Keys.Any(key =>
                    !actual.TryGetValue(key, out var pose) ||
                    Vector3.Distance(expected[key].Position, pose.Position) > 0.0001f ||
                    Quaternion.Angle(expected[key].Rotation, pose.Rotation) > 0.01f))
            {
                throw new InvalidOperationException(
                    "Kursa pose mismatch between " + context + ".");
            }
        }

        private static void CaptureReview(string destination, string kind)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireDirectChild(
                placement.transform,
                StaticSlotName));
            var stanceModel = RequireModel(RequireDirectChild(
                placement.transform,
                StanceSlotName));
            var targetModel = RequireModel(RequireDirectChild(
                placement.transform,
                TargetSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var stanceRenderer = RequireRenderer(stanceModel, StanceSlotName);
            var targetRenderer = RequireRenderer(targetModel, TargetSlotName);
            var sourceEffect = stanceModel.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(item => item.name == EffectName);
            var stanceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StanceClipPath) ??
                throw new InvalidOperationException("Slot 5 stance clip is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException("Slot 8 release clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath) ?? throw new InvalidOperationException(
                    "Slot 8 release controller is missing.");
            RequirePlacedContract(
                targetModel,
                staticModel,
                staticRenderer,
                sourceEffect,
                clip,
                controller,
                stanceClip);
            CaptureContactSheet(
                scene,
                staticModel,
                staticRenderer,
                stanceModel,
                stanceRenderer,
                targetModel,
                targetRenderer,
                stanceClip,
                clip,
                destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa from-shield-stance capture changed the scene dirty state.");
            Debug.Log(
                "KursaFromShieldStanceReviewCaptured Kind=" + kind +
                ", DirectVisualReviewRequired=True, " +
                "Columns=Slot5Completion|StaticReference|0|0.5|1|1.5|2|FinalHoldFrame, " +
                "Rows=Oblique|Side, Image=" + destination +
                ", SceneChanged=False.");
        }

        private static void CaptureContactSheet(
            Scene scene,
            Transform staticModel,
            SkinnedMeshRenderer staticRenderer,
            Transform stanceModel,
            SkinnedMeshRenderer stanceRenderer,
            Transform targetModel,
            SkinnedMeshRenderer targetRenderer,
            AnimationClip stanceClip,
            AnimationClip targetClip,
            string destination)
        {
            const int panelWidth = 260;
            const int panelHeight = 420;
            const int columns = 8;
            const int rows = 2;
            var sceneRenderers = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var rendererStates = sceneRenderers
                .Select(item => new RendererSnapshot(item))
                .ToArray();
            var stanceSnapshots = stanceModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var targetSnapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var animators = new[] { stanceModel, targetModel }
                .SelectMany(item => item.GetComponentsInChildren<Animator>(true))
                .Select(item => new AnimatorSnapshot(item))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                .GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("The Player camera is missing.");
            var cameraObject = new GameObject(
                "KursaFromShieldStanceReviewCamera",
                typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var targetTexture = new RenderTexture(
                panelWidth,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var sheet = new Texture2D(
                panelWidth * columns,
                panelHeight * rows,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var animator in animators) animator.Animator.enabled = false;
                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                stanceClip.SampleAnimation(stanceModel.gameObject, StanceCompletionTime);
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
                targetClip.SampleAnimation(targetModel.gameObject, 0f);
                var targetBodyBounds = FullLoopRendererBounds(
                    targetModel,
                    targetRenderer,
                    targetClip,
                    targetSnapshots);
                var targetFullBounds = targetBodyBounds;
                var targetEffect = targetModel.GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(item => item.name == EffectName);
                targetFullBounds.Encapsulate(targetEffect.bounds);
                var bodyToReviewCenter =
                    targetFullBounds.center - targetBodyBounds.center;
                var sharedSize = targetFullBounds.size;
                var stanceBounds = new Bounds(
                    stanceRenderer.bounds.center + bodyToReviewCenter,
                    sharedSize);
                var staticBounds = new Bounds(
                    staticRenderer.bounds.center + bodyToReviewCenter,
                    sharedSize);

                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 30f;
                camera.aspect = panelWidth / (float)panelHeight;
                camera.targetTexture = targetTexture;

                for (var row = 0; row < rows; row++)
                {
                    var yaw = row == 0 ? 35f : 90f;
                    foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                    stanceClip.SampleAnimation(stanceModel.gameObject, StanceCompletionTime);
                    RenderPanel(
                        camera,
                        stanceModel,
                        sceneRenderers,
                        targetTexture,
                        panel,
                        stanceBounds,
                        yaw);
                    CopyPanel(panel, sheet, 0, rows - 1 - row, panelWidth, panelHeight);

                    RenderPanel(
                        camera,
                        staticModel,
                        sceneRenderers,
                        targetTexture,
                        panel,
                        staticBounds,
                        yaw);
                    CopyPanel(panel, sheet, 1, rows - 1 - row, panelWidth, panelHeight);

                    for (var index = 0; index < ReviewTimes.Length; index++)
                    {
                        foreach (var snapshot in targetSnapshots) snapshot.Restore();
                        targetClip.SampleAnimation(
                            targetModel.gameObject,
                            ReviewTimes[index]);
                        var targetBounds = new Bounds(
                            targetRenderer.bounds.center + bodyToReviewCenter,
                            sharedSize);
                        RenderPanel(
                            camera,
                            targetModel,
                            sceneRenderers,
                            targetTexture,
                            panel,
                            targetBounds,
                            yaw);
                        CopyPanel(
                            panel,
                            sheet,
                            index + 2,
                            rows - 1 - row,
                            panelWidth,
                            panelHeight);
                    }
                }

                sheet.Apply();
                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Invalid Kursa from-shield-stance review folder."));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
                foreach (var snapshot in animators) snapshot.Restore();
                foreach (var snapshot in rendererStates) snapshot.Restore();
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds FullLoopRendererBounds(
            Transform model,
            Renderer bodyRenderer,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var result = new Bounds();
            var initialized = false;
            foreach (var time in ReviewTimes)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(model.gameObject, time);
                var current = bodyRenderer.bounds;
                if (!initialized)
                {
                    result = current;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(current);
                }
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static Bounds BoundsOf(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeSelf)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The Kursa review model has no renderer.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void RenderPanel(
            Camera camera,
            Transform model,
            IEnumerable<Renderer> sceneRenderers,
            RenderTexture target,
            Texture2D panel,
            Bounds bounds,
            float yaw)
        {
            foreach (var renderer in sceneRenderers)
                renderer.enabled = renderer.transform.IsChildOf(model);
            FrameCamera(camera, model, bounds, target.width / (float)target.height, yaw);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            panel.Apply();
        }

        private static void FrameCamera(
            Camera camera,
            Transform model,
            Bounds bounds,
            float aspect,
            float yaw)
        {
            var direction = Quaternion.AngleAxis(yaw, model.up) * model.forward.normalized;
            var vertical = bounds.extents.y /
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.16f;
            camera.transform.position = bounds.center + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static void CopyPanel(
            Texture2D panel,
            Texture2D sheet,
            int column,
            int row,
            int width,
            int height)
        {
            sheet.SetPixels(
                column * width,
                row * height,
                width,
                height,
                panel.GetPixels());
        }

        private static Dictionary<string, Transform> RequireSkeletonPaths(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var transforms = new HashSet<Transform>(renderer.bones);
            foreach (var bone in renderer.bones)
            {
                var current = bone.parent;
                while (current != null && current != model)
                {
                    transforms.Add(current);
                    current = current.parent;
                }
            }
            var result = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var item in transforms)
            {
                var path = AnimationUtility.CalculateTransformPath(item, model);
                if (string.IsNullOrEmpty(path))
                    throw new InvalidOperationException(
                        "The Kursa skeleton unexpectedly includes the model root.");
                result.Add(path, item);
            }
            return result;
        }

        private static void RequireExactStaticAppearance(
            SkinnedMeshRenderer expected,
            SkinnedMeshRenderer actual,
            string context)
        {
            if (expected.sharedMesh != actual.sharedMesh ||
                expected.bones.Length != actual.bones.Length ||
                expected.rootBone.name != actual.rootBone.name ||
                expected.sharedMaterials.Length != actual.sharedMaterials.Length ||
                !expected.sharedMaterials.SequenceEqual(actual.sharedMaterials))
            {
                throw new InvalidOperationException(
                    context + " does not use the exact static Kursa appearance.");
            }
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on Kursa_08_FromShieldStance.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects().SingleOrDefault(item =>
                item.name == PlacementRootName) ??
            throw new InvalidOperationException("Approved Kursa placement is missing.");

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Kursa slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1 ||
                    slot.GetChild(0).name != ModelName)
                {
                    throw new InvalidOperationException(
                        "Kursa slot contract differs at index " + index + ".");
                }
            }
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            return matches[0];
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                throw new InvalidOperationException(slot.name + " model contract differs.");
            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireRenderer(
            Transform model,
            string context) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    context + " must contain one skinned renderer.");

        private static string[] OtherSlotSignatures(Transform placement) =>
            SlotNames.Where(item => item != TargetSlotName)
                .Select(item => RecursiveSignature(RequireDirectChild(placement, item)))
                .ToArray();

        private static string[] OtherRootSignatures(Scene scene, GameObject placement) =>
            scene.GetRootGameObjects()
                .Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform))
                .ToArray();

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(item.gameObject.activeSelf).Append('|')
                    .Append(item.localPosition).Append('|')
                    .Append(item.localRotation).Append('|')
                    .Append(item.localScale);
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    builder.Append("|R:").Append(renderer.enabled);
                    if (renderer is SkinnedMeshRenderer skinned)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
                foreach (var animator in item.GetComponents<Animator>())
                {
                    builder.Append("|A:").Append(animator.enabled)
                        .Append(':').Append(animator.applyRootMotion)
                        .Append(':').Append(AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController));
                }
            }
            return builder.ToString();
        }

        private static string LocalTransformSignature(Transform transform) =>
            transform.localPosition + "|" + transform.localRotation + "|" +
            transform.localScale;

        private static void RequireEqual(
            string[] before,
            string[] after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string NextDiagnosticPath()
        {
            for (var index = 1; index <= 2; index++)
            {
                var candidate = Absolute(string.Format(DiagnosticPathFormat, index));
                if (!File.Exists(candidate)) return candidate;
            }
            throw new InvalidOperationException(
                "The two approved Kursa from-shield-stance hold diagnostics already exist.");
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException("Asset could not be replaced: " + path);
            }
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private sealed class TransformCurveKeys
        {
            private readonly List<Keyframe> positionX = new List<Keyframe>();
            private readonly List<Keyframe> positionY = new List<Keyframe>();
            private readonly List<Keyframe> positionZ = new List<Keyframe>();
            private readonly List<Keyframe> rotationX = new List<Keyframe>();
            private readonly List<Keyframe> rotationY = new List<Keyframe>();
            private readonly List<Keyframe> rotationZ = new List<Keyframe>();
            private readonly List<Keyframe> rotationW = new List<Keyframe>();
            private Quaternion previousRotation;
            private bool hasPreviousRotation;

            public void Add(float time, Vector3 position, Quaternion rotation)
            {
                if (hasPreviousRotation && Quaternion.Dot(previousRotation, rotation) < 0f)
                    rotation = Negate(rotation);
                previousRotation = rotation;
                hasPreviousRotation = true;
                positionX.Add(new Keyframe(time, position.x));
                positionY.Add(new Keyframe(time, position.y));
                positionZ.Add(new Keyframe(time, position.z));
                rotationX.Add(new Keyframe(time, rotation.x));
                rotationY.Add(new Keyframe(time, rotation.y));
                rotationZ.Add(new Keyframe(time, rotation.z));
                rotationW.Add(new Keyframe(time, rotation.w));
            }

            public void Apply(AnimationClip clip, string path)
            {
                SetCurve(clip, path, "m_LocalPosition.x", positionX);
                SetCurve(clip, path, "m_LocalPosition.y", positionY);
                SetCurve(clip, path, "m_LocalPosition.z", positionZ);
                SetCurve(clip, path, "m_LocalRotation.x", rotationX);
                SetCurve(clip, path, "m_LocalRotation.y", rotationY);
                SetCurve(clip, path, "m_LocalRotation.z", rotationZ);
                SetCurve(clip, path, "m_LocalRotation.w", rotationW);
            }

            private static void SetCurve(
                AnimationClip clip,
                string path,
                string property,
                IReadOnlyCollection<Keyframe> keys)
            {
                var curve = new AnimationCurve(keys.ToArray())
                {
                    preWrapMode = WrapMode.ClampForever,
                    postWrapMode = WrapMode.ClampForever
                };
                for (var index = 0; index < curve.length; index++)
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
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                    curve);
            }
        }

        private static Quaternion Negate(Quaternion value) =>
            new Quaternion(-value.x, -value.y, -value.z, -value.w);

        private readonly struct LocalPose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public LocalPose(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform value)
            {
                transform = value;
                position = value.localPosition;
                rotation = value.localRotation;
                scale = value.localScale;
            }

            public void Restore()
            {
                if (transform == null) return;
                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }
        }

        private readonly struct RendererSnapshot
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererSnapshot(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (renderer != null) renderer.enabled = enabled;
            }
        }

        private readonly struct AnimatorSnapshot
        {
            public readonly Animator Animator;
            private readonly bool enabled;

            public AnimatorSnapshot(Animator animator)
            {
                Animator = animator;
                enabled = animator.enabled;
            }

            public void Restore()
            {
                if (Animator != null) Animator.enabled = enabled;
            }
        }
    }
}
