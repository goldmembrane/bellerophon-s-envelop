using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.ResistanceCargoRunScene
{
    internal static class ResistanceIdleAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Resistance Enemy Placement";
        private const string IdleSlotName = "Resistance_02";
        private const string ModelName = "Resistance_Model";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Resistance/Animations";
        private const string ClipPath =
            AnimationFolder + "/Resistance_02_Idle_Morph.anim";
        private const string ControllerPath =
            AnimationFolder + "/Resistance_02_Idle_Morph.controller";
        private const string StateName = "Resistance_02_Idle_Morph";
        private const string ValidationFolder =
            "docs/validation/resistance_idle_2026-07-26";
        private const string InspectionPath =
            ValidationFolder + "/Resistance_02_Idle_Inspection.txt";
        private const string CapturePath =
            ValidationFolder + "/Resistance_02_Idle_VisualReview.png";
        private const int SlotCount = 14;
        private const int ReviewLayer = 30;
        private const int ReviewImageSize = 512;
        private const float LoopSeconds = 2f;
        private const float HeightVariation = 0.015f;
        private const float CurveTolerance = 0.0001f;
        private const float GroundTolerance = 0.002f;

        [MenuItem("Bellerophon/Enemies/Resistance/Apply Idle Animation")]
        public static void ApplyResistanceIdleAnimation()
        {
            var scene = RequireScene();
            var placementRoot = RequirePlacementRoot(scene);
            var idleSlot = RequireIdleSlot(placementRoot);
            var model = RequireModel(idleSlot);
            var renderers = RequireRenderers(model);
            var slotPositionBefore = idleSlot.localPosition;
            var slotRotationBefore = idleSlot.localRotation;
            var slotScaleBefore = idleSlot.localScale;
            var otherSlotsBefore = CaptureOtherSlotStates(placementRoot, idleSlot);
            var meshStatesBefore = CaptureMeshStates(renderers);

            if (slotScaleBefore != Vector3.one)
            {
                throw new InvalidOperationException(
                    "Resistance_02 must have unit scale before idle animation is applied. Actual=" +
                    Format(slotScaleBefore));
            }

            if (CountOtherConfiguredAnimators(placementRoot, idleSlot) != 0)
            {
                throw new InvalidOperationException(
                    "Only Resistance_02 may have a configured Animator in this step.");
            }

            var clip = CreateOrUpdateClip();
            var controller = CreateOrUpdateController(clip);
            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = idleSlot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            RequireUnchangedTransform(
                idleSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore,
                "Resistance_02");
            RequireOtherSlotsUnchanged(placementRoot, idleSlot, otherSlotsBefore);
            RequireMeshesUnchanged(renderers, meshStatesBefore);
            VerifyAnimationAssets(clip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Resistance idle application.");
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = idleSlot.gameObject;
            Debug.Log(
                "ResistanceIdleAnimationApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + IdleSlotName +
                ", DurationSeconds=" + LoopSeconds.ToString("0.0", CultureInfo.InvariantCulture) +
                ", HeightScaleMin=" + (1f - HeightVariation).ToString("0.000", CultureInfo.InvariantCulture) +
                ", HeightScaleMax=" + (1f + HeightVariation).ToString("0.000", CultureInfo.InvariantCulture) +
                ", SlotPositionFixed=True" +
                ", FeetGroundPivotFixed=True" +
                ", MeshesUnchanged=True" +
                ", OtherSlotsUnchanged=True" +
                ", Loop=True.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Inspect Idle Animation")]
        public static void InspectResistanceIdleAnimation()
        {
            var scene = RequireScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequirePlacementRoot(scene);
            var idleSlot = RequireIdleSlot(placementRoot);
            var model = RequireModel(idleSlot);
            var renderers = RequireRenderers(model);
            var animator = RequireAnimator(idleSlot);
            var clip = RequireClip();
            var controller = RequireController();
            VerifySceneAnimator(animator, controller);
            VerifyAnimationAssets(clip, controller);

            if (CountOtherConfiguredAnimators(placementRoot, idleSlot) != 0)
            {
                throw new InvalidOperationException(
                    "A Resistance slot outside Resistance_02 has a configured Animator.");
            }

            var slotPosition = idleSlot.localPosition;
            var groundSamples = SampleGroundAndScale(idleSlot, renderers, clip);
            var groundReference = groundSamples[0].GroundY;
            var maxGroundDelta = groundSamples.Max(sample =>
                Mathf.Abs(sample.GroundY - groundReference));
            if (maxGroundDelta > GroundTolerance)
            {
                throw new InvalidOperationException(
                    "Resistance_02 feet did not remain fixed to the ground. MaxGroundDelta=" +
                    maxGroundDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (idleSlot.localPosition != slotPosition)
            {
                throw new InvalidOperationException(
                    "Resistance_02 slot position changed during animation inspection.");
            }

            Directory.CreateDirectory(Absolute(ValidationFolder));
            WriteInspectionReport(
                idleSlot,
                renderers,
                animator,
                clip,
                controller,
                groundSamples,
                maxGroundDelta);
            AssetDatabase.Refresh();

            if (!sceneWasDirty && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Resistance idle inspection dirtied CargoRunMvp unexpectedly.");
            }

            Selection.activeGameObject = idleSlot.gameObject;
            Debug.Log(
                "ResistanceIdleAnimationInspected Result=PASS" +
                ", Target=" + PlacementRootName + "/" + IdleSlotName +
                ", DurationSeconds=" + clip.length.ToString("0.0", CultureInfo.InvariantCulture) +
                ", MaxGroundDelta=" + maxGroundDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", SlotPositionFixed=True" +
                ", MeshesUnchanged=True" +
                ", OtherConfiguredAnimators=0" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Capture Idle Animation Review")]
        public static void CaptureResistanceIdleAnimationReview()
        {
            var scene = RequireScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequirePlacementRoot(scene);
            var idleSlot = RequireIdleSlot(placementRoot);
            var model = RequireModel(idleSlot);
            var renderers = RequireRenderers(model);
            var animator = RequireAnimator(idleSlot);
            var clip = RequireClip();
            var controller = RequireController();
            VerifySceneAnimator(animator, controller);
            VerifyAnimationAssets(clip, controller);

            Directory.CreateDirectory(Absolute(ValidationFolder));
            var times = new[] { 0f, 0.5f, 1f, 1.5f, 2f };
            var fullFrames = new Texture2D[times.Length];
            var closeFrames = new Texture2D[times.Length];
            var layerStates = idleSlot.GetComponentsInChildren<Transform>(true)
                .Select(transform => new LayerState(transform.gameObject, transform.gameObject.layer))
                .ToArray();
            var cameraObject = new GameObject(
                "Resistance_Idle_ReviewCamera",
                typeof(Camera));
            var keyObject = new GameObject(
                "Resistance_Idle_KeyLight",
                typeof(Light));
            var fillObject = new GameObject(
                "Resistance_Idle_FillLight",
                typeof(Light));
            var camera = cameraObject.GetComponent<Camera>();
            var key = keyObject.GetComponent<Light>();
            var fill = fillObject.GetComponent<Light>();
            var neutralBounds = CombinedBounds(renderers);

            try
            {
                foreach (var layerState in layerStates)
                {
                    layerState.GameObject.layer = ReviewLayer;
                }

                ConfigureReviewCameraAndLights(
                    camera,
                    keyObject.transform,
                    key,
                    fillObject.transform,
                    fill);
                for (var index = 0; index < times.Length; index++)
                {
                    SampleClip(idleSlot.gameObject, clip, times[index]);
                    PositionReviewCamera(camera.transform, neutralBounds, 1f);
                    fullFrames[index] = RenderFrame(camera);
                    PositionReviewCamera(camera.transform, neutralBounds, 0.72f);
                    closeFrames[index] = RenderFrame(camera);
                }

                WriteContactSheet(fullFrames, closeFrames);
            }
            finally
            {
                StopSampling();
                foreach (var layerState in layerStates)
                {
                    layerState.GameObject.layer = layerState.Layer;
                }

                DestroyFrames(fullFrames);
                DestroyFrames(closeFrames);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }

            AssetDatabase.Refresh();
            if (!sceneWasDirty && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Resistance idle review capture dirtied CargoRunMvp unexpectedly.");
            }

            Selection.activeGameObject = idleSlot.gameObject;
            Debug.Log(
                "ResistanceIdleAnimationReviewCaptured Result=PASS" +
                ", Target=" + PlacementRootName + "/" + IdleSlotName +
                ", Checkpoints=0.0|0.5|1.0|1.5|2.0" +
                ", Output=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static AnimationClip CreateOrUpdateClip()
        {
            Directory.CreateDirectory(Absolute(AnimationFolder));
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.ClearCurves();
            clip.name = StateName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Transform),
                    "m_LocalScale.x"),
                ConstantCurve(1f));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Transform),
                    "m_LocalScale.y"),
                SmoothCurve(
                    new[]
                    {
                        new Keyframe(0f, 1f),
                        new Keyframe(0.5f, 1f + HeightVariation),
                        new Keyframe(1f, 1f),
                        new Keyframe(1.5f, 1f - HeightVariation),
                        new Keyframe(LoopSeconds, 1f)
                    }));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Transform),
                    "m_LocalScale.z"),
                ConstantCurve(1f));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            Directory.CreateDirectory(Absolute(AnimationFolder));
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void VerifySceneAnimator(
            Animator animator,
            AnimatorController controller)
        {
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Resistance_02 Animator is not configured for the root-locked idle loop.");
            }
        }

        private static void VerifyAnimationAssets(
            AnimationClip clip,
            AnimatorController controller)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime ||
                Mathf.Abs(clip.length - LoopSeconds) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Resistance idle clip must be a two-second loop.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 3 ||
                bindings.Any(binding =>
                    binding.path != string.Empty ||
                    binding.type != typeof(Transform)) ||
                bindings.Select(binding => binding.propertyName).OrderBy(value => value)
                    .SequenceEqual(
                        new[]
                        {
                            "m_LocalScale.x",
                            "m_LocalScale.y",
                            "m_LocalScale.z"
                        }.OrderBy(value => value)) == false)
            {
                throw new InvalidOperationException(
                    "Resistance idle clip must contain only root local-scale curves.");
            }

            var yBinding = bindings.Single(binding =>
                binding.propertyName == "m_LocalScale.y");
            var yCurve = AnimationUtility.GetEditorCurve(clip, yBinding) ??
                throw new InvalidOperationException(
                    "Resistance idle Y scale curve is missing.");
            RequireCurveValue(yCurve, 0f, 1f);
            RequireCurveValue(yCurve, 0.5f, 1f + HeightVariation);
            RequireCurveValue(yCurve, 1f, 1f);
            RequireCurveValue(yCurve, 1.5f, 1f - HeightVariation);
            RequireCurveValue(yCurve, LoopSeconds, 1f);

            foreach (var propertyName in new[] { "m_LocalScale.x", "m_LocalScale.z" })
            {
                var binding = bindings.Single(candidate =>
                    candidate.propertyName == propertyName);
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                    throw new InvalidOperationException(propertyName + " curve is missing.");
                RequireCurveValue(curve, 0f, 1f);
                RequireCurveValue(curve, LoopSeconds, 1f);
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Resistance idle controller must contain one layer.");
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            if (defaultState == null ||
                defaultState.name != StateName ||
                defaultState.motion != clip ||
                Mathf.Abs(defaultState.speed - 1f) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Resistance idle controller default state is not the approved idle clip.");
            }
        }

        private static GroundSample[] SampleGroundAndScale(
            Transform idleSlot,
            Renderer[] renderers,
            AnimationClip clip)
        {
            var times = new[] { 0f, 0.5f, 1f, 1.5f, 2f };
            var samples = new GroundSample[times.Length];
            try
            {
                for (var index = 0; index < times.Length; index++)
                {
                    SampleClip(idleSlot.gameObject, clip, times[index]);
                    var bounds = CombinedBounds(renderers);
                    samples[index] = new GroundSample(
                        times[index],
                        idleSlot.localScale.y,
                        bounds.min.y,
                        bounds.max.y);
                }
            }
            finally
            {
                StopSampling();
            }

            return samples;
        }

        private static void SampleClip(
            GameObject target,
            AnimationClip clip,
            float time)
        {
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(target, clip, time);
            AnimationMode.EndSampling();
            SceneView.RepaintAll();
        }

        private static void StopSampling()
        {
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static void WriteInspectionReport(
            Transform idleSlot,
            Renderer[] renderers,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            GroundSample[] samples,
            float maxGroundDelta)
        {
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + IdleSlotName);
            report.AppendLine("Model=" + ModelName);
            report.AppendLine("Clip=" + ClipPath);
            report.AppendLine("Controller=" + ControllerPath);
            report.AppendLine("DefaultState=" + StateName);
            report.AppendLine("DurationSeconds=" +
                              clip.length.ToString("0.0", CultureInfo.InvariantCulture));
            report.AppendLine("FrameRate=" +
                              clip.frameRate.ToString("0", CultureInfo.InvariantCulture));
            report.AppendLine("LoopTime=" +
                              AnimationUtility.GetAnimationClipSettings(clip).loopTime);
            report.AppendLine("HeightScaleMin=" +
                              (1f - HeightVariation).ToString("0.000", CultureInfo.InvariantCulture));
            report.AppendLine("HeightScaleMax=" +
                              (1f + HeightVariation).ToString("0.000", CultureInfo.InvariantCulture));
            report.AppendLine("MotionBinding=Resistance_02 root local scale Y only; X/Z held at 1");
            report.AppendLine("SlotLocalPosition=" + Format(idleSlot.localPosition));
            report.AppendLine("SlotPositionAnimated=False");
            report.AppendLine("RootMotion=" + animator.applyRootMotion);
            report.AppendLine("RendererCount=" +
                              renderers.Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("MeshVertexCounts=" + string.Join(
                "|",
                renderers.Select(renderer => MeshVertexCount(renderer)
                    .ToString(CultureInfo.InvariantCulture))));
            report.AppendLine("MeshTriangleCounts=" + string.Join(
                "|",
                renderers.Select(renderer => MeshTriangleCount(renderer)
                    .ToString(CultureInfo.InvariantCulture))));
            foreach (var sample in samples)
            {
                report.AppendLine(
                    "Sample=" +
                    sample.Time.ToString("0.0", CultureInfo.InvariantCulture) +
                    ",ScaleY=" +
                    sample.ScaleY.ToString("0.000000", CultureInfo.InvariantCulture) +
                    ",GroundY=" +
                    sample.GroundY.ToString("0.######", CultureInfo.InvariantCulture) +
                    ",TopY=" +
                    sample.TopY.ToString("0.######", CultureInfo.InvariantCulture));
            }

            report.AppendLine("MaxGroundDelta=" +
                              maxGroundDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("FeetGroundFixed=True");
            report.AppendLine("MeshesChanged=False");
            report.AppendLine("MaterialsChanged=False");
            report.AppendLine("OtherConfiguredAnimators=0");
            report.AppendLine("OtherSlotsChanged=False");
            report.AppendLine("SceneChangedByInspection=False");
            File.WriteAllText(
                Absolute(InspectionPath),
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static void WriteContactSheet(
            Texture2D[] fullFrames,
            Texture2D[] closeFrames)
        {
            var contactSheet = new Texture2D(
                ReviewImageSize * fullFrames.Length,
                ReviewImageSize * 2,
                TextureFormat.RGBA32,
                false);
            try
            {
                for (var index = 0; index < fullFrames.Length; index++)
                {
                    contactSheet.SetPixels32(
                        index * ReviewImageSize,
                        ReviewImageSize,
                        ReviewImageSize,
                        ReviewImageSize,
                        fullFrames[index].GetPixels32());
                    contactSheet.SetPixels32(
                        index * ReviewImageSize,
                        0,
                        ReviewImageSize,
                        ReviewImageSize,
                        closeFrames[index].GetPixels32());
                }

                contactSheet.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(CapturePath),
                    contactSheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contactSheet);
            }
        }

        private static void ConfigureReviewCameraAndLights(
            Camera camera,
            Transform keyTransform,
            Light key,
            Transform fillTransform,
            Light fill)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.64f, 0.69f, 0.74f, 1f);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.cullingMask = 1 << ReviewLayer;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            key.type = LightType.Directional;
            key.intensity = 1.35f;
            key.color = new Color(1f, 0.92f, 0.82f);
            key.cullingMask = 1 << ReviewLayer;
            keyTransform.rotation = Quaternion.Euler(38f, -28f, 0f);
            fill.type = LightType.Directional;
            fill.intensity = 0.75f;
            fill.color = new Color(0.50f, 0.70f, 1f);
            fill.cullingMask = 1 << ReviewLayer;
            fillTransform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void PositionReviewCamera(
            Transform cameraTransform,
            Bounds bounds,
            float distanceMultiplier)
        {
            var target = bounds.center + Vector3.up * (bounds.extents.y * 0.02f);
            var halfFovRadians = 42f * 0.5f * Mathf.Deg2Rad;
            var distance =
                (Mathf.Max(bounds.extents.y, bounds.extents.x) /
                 Mathf.Tan(halfFovRadians) +
                 bounds.extents.z +
                 0.35f) *
                distanceMultiplier;
            cameraTransform.position = target + Vector3.back * distance;
            cameraTransform.rotation = Quaternion.LookRotation(
                target - cameraTransform.position,
                Vector3.up);
        }

        private static Texture2D RenderFrame(Camera camera)
        {
            var renderTexture = RenderTexture.GetTemporary(
                ReviewImageSize,
                ReviewImageSize,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                var texture = new Texture2D(
                    ReviewImageSize,
                    ReviewImageSize,
                    TextureFormat.RGBA32,
                    false);
                texture.ReadPixels(
                    new Rect(0, 0, ReviewImageSize, ReviewImageSize),
                    0,
                    0,
                    false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static Scene RequireScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Resistance idle work must run in Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            }

            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            var root = scene.GetRootGameObjects()
                .SingleOrDefault(candidate => candidate.name == PlacementRootName) ??
                throw new InvalidOperationException(
                    "Approved Resistance placement root is missing.");
            if (root.transform.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "Approved Resistance placement must contain exactly fourteen slots.");
            }

            return root.transform;
        }

        private static Transform RequireIdleSlot(Transform placementRoot)
        {
            var idleSlot = placementRoot.Find(IdleSlotName) ??
                throw new InvalidOperationException(IdleSlotName + " is missing.");
            if (idleSlot.GetSiblingIndex() != 1)
            {
                throw new InvalidOperationException(
                    "Resistance_02 must remain the second Resistance slot.");
            }

            return idleSlot;
        }

        private static Transform RequireModel(Transform idleSlot)
        {
            if (idleSlot.childCount != 1 ||
                idleSlot.GetChild(0).name != ModelName)
            {
                throw new InvalidOperationException(
                    "Resistance_02 must contain exactly one direct Resistance_Model child.");
            }

            return idleSlot.GetChild(0);
        }

        private static Renderer[] RequireRenderers(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer is SkinnedMeshRenderer ||
                    renderer is MeshRenderer)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Resistance_Model has no mesh renderer.");
            }

            return renderers;
        }

        private static Animator RequireAnimator(Transform idleSlot)
        {
            return idleSlot.GetComponent<Animator>() ??
                   throw new InvalidOperationException(
                       "Resistance_02 has no Animator.");
        }

        private static AnimationClip RequireClip()
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                   throw new InvalidOperationException(
                       "Resistance idle clip is missing.");
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                   throw new InvalidOperationException(
                       "Resistance idle controller is missing.");
        }

        private static int CountOtherConfiguredAnimators(
            Transform placementRoot,
            Transform idleSlot)
        {
            return placementRoot.Cast<Transform>()
                .Where(slot => slot != idleSlot)
                .SelectMany(slot => slot.GetComponents<Animator>())
                .Count(animator => animator.runtimeAnimatorController != null);
        }

        private static SlotState[] CaptureOtherSlotStates(
            Transform placementRoot,
            Transform idleSlot)
        {
            return placementRoot.Cast<Transform>()
                .Where(slot => slot != idleSlot)
                .Select(SlotState.Capture)
                .ToArray();
        }

        private static void RequireOtherSlotsUnchanged(
            Transform placementRoot,
            Transform idleSlot,
            SlotState[] before)
        {
            var after = CaptureOtherSlotStates(placementRoot, idleSlot);
            if (!before.SequenceEqual(after))
            {
                throw new InvalidOperationException(
                    "A Resistance slot outside Resistance_02 changed during idle application.");
            }
        }

        private static MeshState[] CaptureMeshStates(Renderer[] renderers)
        {
            return renderers.Select(MeshState.Capture).ToArray();
        }

        private static void RequireMeshesUnchanged(
            Renderer[] renderers,
            MeshState[] before)
        {
            var after = CaptureMeshStates(renderers);
            if (!before.SequenceEqual(after))
            {
                throw new InvalidOperationException(
                    "Resistance_02 mesh or materials changed during idle application.");
            }
        }

        private static void RequireUnchangedTransform(
            Transform target,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            string label)
        {
            if (target.localPosition != position ||
                target.localRotation != rotation ||
                target.localScale != scale)
            {
                throw new InvalidOperationException(
                    label + " transform changed while configuring its Animator.");
            }
        }

        private static AnimationCurve ConstantCurve(float value)
        {
            return AnimationCurve.Constant(0f, LoopSeconds, value);
        }

        private static AnimationCurve SmoothCurve(Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Auto);
            }

            return curve;
        }

        private static void RequireCurveValue(
            AnimationCurve curve,
            float time,
            float expected)
        {
            var actual = curve.Evaluate(time);
            if (Mathf.Abs(actual - expected) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Resistance idle curve mismatch at " +
                    time.ToString("0.0", CultureInfo.InvariantCulture) +
                    " seconds. Expected=" +
                    expected.ToString("0.000", CultureInfo.InvariantCulture) +
                    ", Actual=" +
                    actual.ToString("0.000000", CultureInfo.InvariantCulture));
            }
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static int MeshVertexCount(Renderer renderer)
        {
            var mesh = SharedMesh(renderer);
            return mesh != null ? mesh.vertexCount : 0;
        }

        private static int MeshTriangleCount(Renderer renderer)
        {
            var mesh = SharedMesh(renderer);
            return mesh != null
                ? Enumerable.Range(0, mesh.subMeshCount)
                    .Sum(index => (int)mesh.GetIndexCount(index) / 3)
                : 0;
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static void DestroyFrames(IEnumerable<Texture2D> frames)
        {
            foreach (var frame in frames)
            {
                if (frame != null)
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }
            }
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Format(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######})",
                value.x,
                value.y,
                value.z);
        }

        private readonly struct GroundSample
        {
            public GroundSample(float time, float scaleY, float groundY, float topY)
            {
                Time = time;
                ScaleY = scaleY;
                GroundY = groundY;
                TopY = topY;
            }

            public float Time { get; }
            public float ScaleY { get; }
            public float GroundY { get; }
            public float TopY { get; }
        }

        private readonly struct LayerState
        {
            public LayerState(GameObject gameObject, int layer)
            {
                GameObject = gameObject;
                Layer = layer;
            }

            public GameObject GameObject { get; }
            public int Layer { get; }
        }

        private readonly struct SlotState : IEquatable<SlotState>
        {
            private SlotState(
                string name,
                int siblingIndex,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                int childCount)
            {
                Name = name;
                SiblingIndex = siblingIndex;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                ChildCount = childCount;
            }

            private string Name { get; }
            private int SiblingIndex { get; }
            private Vector3 Position { get; }
            private Quaternion Rotation { get; }
            private Vector3 Scale { get; }
            private int ChildCount { get; }

            public static SlotState Capture(Transform slot)
            {
                return new SlotState(
                    slot.name,
                    slot.GetSiblingIndex(),
                    slot.localPosition,
                    slot.localRotation,
                    slot.localScale,
                    slot.childCount);
            }

            public bool Equals(SlotState other)
            {
                return Name == other.Name &&
                       SiblingIndex == other.SiblingIndex &&
                       Position == other.Position &&
                       Rotation == other.Rotation &&
                       Scale == other.Scale &&
                       ChildCount == other.ChildCount;
            }

            public override bool Equals(object obj)
            {
                return obj is SlotState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Name != null ? Name.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ SiblingIndex;
                    hashCode = (hashCode * 397) ^ Position.GetHashCode();
                    hashCode = (hashCode * 397) ^ Rotation.GetHashCode();
                    hashCode = (hashCode * 397) ^ Scale.GetHashCode();
                    hashCode = (hashCode * 397) ^ ChildCount;
                    return hashCode;
                }
            }
        }

        private readonly struct MeshState : IEquatable<MeshState>
        {
            private MeshState(
                int rendererId,
                int meshId,
                int vertexCount,
                int triangleCount,
                string materials)
            {
                RendererId = rendererId;
                MeshId = meshId;
                VertexCount = vertexCount;
                TriangleCount = triangleCount;
                Materials = materials;
            }

            private int RendererId { get; }
            private int MeshId { get; }
            private int VertexCount { get; }
            private int TriangleCount { get; }
            private string Materials { get; }

            public static MeshState Capture(Renderer renderer)
            {
                var mesh = SharedMesh(renderer);
                return new MeshState(
                    renderer.GetInstanceID(),
                    mesh != null ? mesh.GetInstanceID() : 0,
                    mesh != null ? mesh.vertexCount : 0,
                    mesh != null
                        ? Enumerable.Range(0, mesh.subMeshCount)
                            .Sum(index => (int)mesh.GetIndexCount(index) / 3)
                        : 0,
                    string.Join(
                        "|",
                        renderer.sharedMaterials.Select(material =>
                            material != null
                                ? AssetDatabase.GetAssetPath(material)
                                : "<null>")));
            }

            public bool Equals(MeshState other)
            {
                return RendererId == other.RendererId &&
                       MeshId == other.MeshId &&
                       VertexCount == other.VertexCount &&
                       TriangleCount == other.TriangleCount &&
                       Materials == other.Materials;
            }

            public override bool Equals(object obj)
            {
                return obj is MeshState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = RendererId;
                    hashCode = (hashCode * 397) ^ MeshId;
                    hashCode = (hashCode * 397) ^ VertexCount;
                    hashCode = (hashCode * 397) ^ TriangleCount;
                    hashCode = (hashCode * 397) ^
                               (Materials != null ? Materials.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
