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

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static class PahurStopAnimationTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Pahur Enemy Placement";
        private const string StopSlotName = "Pahur_07_Stop";
        private const string ModelName = "Pahur_Model";
        private const string HeadBoneName = "Head";
        private const string ArtRoot =
            "Assets/_Project/Art/Enemies/Pahur";
        private const string ModelPath = ArtRoot + "/Models/Pahur.fbx";
        private const string ApprovedMaterialFolder =
            ArtRoot + "/ApprovedAppearance/Materials";
        private const string StopMaterialFolder =
            ApprovedMaterialFolder + "/Stop";
        private const string FaceSourceMaterialPath =
            ApprovedMaterialFolder + "/Pahur_face_metal_Approved.mat";
        private const string StopFaceMaterialPath =
            StopMaterialFolder + "/Pahur_07_Stop_face_metal.mat";
        private const string SupersededStopOpticMaterialPath =
            StopMaterialFolder + "/Pahur_07_Stop_optic.mat";
        private const string ShaderPath =
            ArtRoot + "/Shaders/PahurStopAppearance.shader";
        private const string AnimationFolder = ArtRoot + "/Animations";
        private const string ControllerFolder = ArtRoot + "/Controllers";
        private const string ClipPath =
            AnimationFolder + "/Pahur_07_Stop.anim";
        private const string ControllerPath =
            ControllerFolder + "/Pahur_07_Stop.controller";
        private const string StateName = "PahurStopLoop";
        private const string ValidationFolder =
            "docs/validation/pahur_stop_animation_2026-08-01";
        private const string ReportPath =
            ValidationFolder + "/Pahur_07_Stop_Validation.txt";
        private const string CapturePath =
            ValidationFolder + "/Pahur_07_Stop_Review_3SecondHold.png";
        private const string ShutdownBlendProperty =
            "material._ShutdownBlend";
        private const float TransitionSeconds = 2f;
        private const float HoldSeconds = 1f;
        private const float DurationSeconds =
            TransitionSeconds + HoldSeconds;
        private const float HeadPitchDegrees = 45f;
        private const float FrameRate = 60f;
        private const float Tolerance = 0.001f;
        private const int ExpectedTriangles = 4330;
        private const int ExpectedBones = 24;

        private static readonly Color ShutdownColor =
            new(3f / 255f, 3f / 255f, 3f / 255f, 1f);

        private static readonly string[] SlotNames =
        {
            "Pahur_01_Static_Review",
            "Pahur_02_Idle",
            "Pahur_03_Move",
            "Pahur_04_MiniFlamethrower",
            "Pahur_05_BreakthroughFlamethrower",
            "Pahur_06_GuardianFlamethrower",
            "Pahur_07_Stop",
            "Pahur_08_ToGuardianStance",
            "Pahur_09_FromGuardianStance",
            "Pahur_10_Hit",
            "Pahur_11_Death"
        };

        [MenuItem("Bellerophon/Enemies/Pahur/Apply Stop Animation")]
        public static void ApplyPahurStopAnimation()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp contains pre-existing unsaved changes.");
            }

            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var slot = RequireDirectChild(root.transform, StopSlotName);
            var model = RequireModel(slot);
            var renderer = RequireApprovedRenderer(model, true);
            var otherSlotsBefore = OtherSlotSignatures(root.transform);
            var placementBefore = PlacementTransformSignatures(root.transform);
            var protectedBefore = ProtectedRootSignatures(scene);

            EnsureAssetFolder(StopMaterialFolder);
            EnsureAssetFolder(AnimationFolder);
            EnsureAssetFolder(ControllerFolder);
            AssetDatabase.ImportAsset(
                ShaderPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            AssignStopMaterials(renderer);
            var clip = CreateStopClip(model, renderer);
            var controller = CreateStopController(clip);
            var animator = GetOrCreateAnimator(model);
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            foreach (var legacy in
                     model.GetComponentsInChildren<Animation>(true))
            {
                legacy.enabled = false;
                EditorUtility.SetDirty(legacy);
            }

            animator.Rebind();
            animator.Update(0f);
            EditorUtility.SetDirty(animator);

            var metrics = InspectState(
                root.transform,
                slot,
                model,
                renderer,
                animator,
                clip,
                controller);

            RequireSameSignatures(
                otherSlotsBefore,
                OtherSlotSignatures(root.transform),
                "A Pahur slot outside Pahur_07_Stop changed during the stop animation apply.");
            RequireSameSignatures(
                placementBefore,
                PlacementTransformSignatures(root.transform),
                "A Pahur placement or bone transform changed outside animation evaluation.");
            RequireSameSignatures(
                protectedBefore,
                ProtectedRootSignatures(scene),
                "A scene root outside the Pahur placement changed during the stop animation apply.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Pahur stop animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "PahurStopAnimationApplied Result=PASS" +
                ", Slot=" + StopSlotName +
                ", DurationSeconds=" + Num(DurationSeconds) +
                ", HoldSeconds=" + Num(HoldSeconds) +
                ", FinalHeadPitchDegrees=" +
                Num(metrics.FinalHeadPitchDegrees) +
                ", FinalEyeColor=#030303" +
                ", Loop=True" +
                ", ReturnSegment=False" +
                ", OtherSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Validate Stop Animation")]
        public static void ValidatePahurStopAnimation()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var slot = RequireDirectChild(root.transform, StopSlotName);
            var model = RequireModel(slot);
            var renderer = RequireApprovedRenderer(model, true);
            var animator = RequireAnimator(model);
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException(
                    "The Pahur stop clip is missing.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "The Pahur stop controller is missing.");

            var metrics = InspectState(
                root.transform,
                slot,
                model,
                renderer,
                animator,
                clip,
                controller);
            WriteValidationReport(metrics);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur stop validation changed the scene dirty state.");
            }

            Debug.Log(
                "PahurStopAnimationValidated Result=PASS" +
                ", Slot=" + StopSlotName +
                ", FinalHeadPitchDegrees=" +
                Num(metrics.FinalHeadPitchDegrees) +
                ", MidHeadPitchDegrees=" +
                Num(metrics.MidHeadPitchDegrees) +
                ", HoldHeadPitchDegrees=" +
                Num(metrics.HoldHeadPitchDegrees) +
                ", FinalShutdownBlend=" +
                Num(metrics.FinalShutdownBlend) +
                ", FinalEyeColor=#030303" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Capture Stop Animation Review")]
        public static void CapturePahurStopAnimationReview()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var slot = RequireDirectChild(root.transform, StopSlotName);
            var model = RequireModel(slot);
            var renderer = RequireApprovedRenderer(model, true);
            var animator = RequireAnimator(model);
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException(
                    "The Pahur stop clip is missing.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "The Pahur stop controller is missing.");

            InspectState(
                root.transform,
                slot,
                model,
                renderer,
                animator,
                clip,
                controller);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Pahur stop review already exists: " +
                    CapturePath);
            }

            CapturePoseStrip(model, clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur stop capture changed the scene dirty state.");
            }

            Debug.Log(
                "PahurStopAnimationReviewCaptured Result=PASS" +
                ", Slot=" + StopSlotName +
                ", Times=0,1,2,2.5,2.999" +
                ", Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static void AssignStopMaterials(
            SkinnedMeshRenderer renderer)
        {
            DeleteAssetIfPresent(SupersededStopOpticMaterialPath);
            var shader = Shader.Find("Bellerophon/Pahur/StopAppearance") ??
                         throw new InvalidOperationException(
                             "The Pahur stop shader failed to import.");
            var face = CreateOrUpdateStopMaterial(
                FaceSourceMaterialPath,
                StopFaceMaterialPath,
                shader);
            var materials = renderer.sharedMaterials;
            var faceIndex = RequireMaterialIndex(
                materials,
                FaceSourceMaterialPath,
                StopFaceMaterialPath,
                "face");
            materials[faceIndex] = face;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        private static Material CreateOrUpdateStopMaterial(
            string sourcePath,
            string destinationPath,
            Shader shader)
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath) ??
                         throw new InvalidOperationException(
                             "The approved Pahur source material is missing: " +
                             sourcePath);
            var material =
                AssetDatabase.LoadAssetAtPath<Material>(destinationPath);
            if (material == null)
            {
                material = new Material(source)
                {
                    name = Path.GetFileNameWithoutExtension(destinationPath)
                };
                AssetDatabase.CreateAsset(material, destinationPath);
            }
            else
            {
                material.shader = source.shader;
                material.CopyPropertiesFromMaterial(source);
            }

            material.shader = shader;
            material.SetFloat("_ShutdownBlend", 0f);
            material.SetColor("_ShutdownColor", ShutdownColor);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static int RequireMaterialIndex(
            IReadOnlyList<Material> materials,
            string sourcePath,
            string destinationPath,
            string label)
        {
            var indices = Enumerable.Range(0, materials.Count)
                .Where(index =>
                {
                    var path = AssetDatabase.GetAssetPath(materials[index]);
                    return path == sourcePath || path == destinationPath;
                })
                .ToArray();
            if (indices.Length != 1)
            {
                throw new InvalidOperationException(
                    "The Pahur stop renderer must contain one " + label +
                    " material slot. Count=" + indices.Length + ".");
            }

            return indices[0];
        }

        private static AnimationClip CreateStopClip(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            DeleteAssetIfPresent(ClipPath);
            var head = RequireDescendant(model, HeadBoneName);
            if (head.parent == null)
            {
                throw new InvalidOperationException(
                    "The Pahur Head bone has no parent.");
            }

            var start = head.localRotation;
            var targetWorld =
                Quaternion.AngleAxis(
                    HeadPitchDegrees,
                    model.right) * head.rotation;
            var end =
                Quaternion.Inverse(head.parent.rotation) * targetWorld;
            if (Quaternion.Dot(start, end) < 0f)
            {
                end = new Quaternion(-end.x, -end.y, -end.z, -end.w);
            }

            var clip = new AnimationClip
            {
                name = "Pahur_07_Stop",
                frameRate = FrameRate,
                wrapMode = WrapMode.Loop
            };
            var headPath =
                AnimationUtility.CalculateTransformPath(head, model);
            SetQuaternionCurves(
                clip,
                headPath,
                new[]
                {
                    new TimedQuaternion(0f, start),
                    new TimedQuaternion(TransitionSeconds, end),
                    new TimedQuaternion(DurationSeconds, end)
                });
            var rendererPath =
                AnimationUtility.CalculateTransformPath(
                    renderer.transform,
                    model);
            clip.SetCurve(
                rendererPath,
                typeof(SkinnedMeshRenderer),
                ShutdownBlendProperty,
                LinearCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(TransitionSeconds, 1f),
                    new Keyframe(DurationSeconds, 1f)));
            var settings =
                AnimationUtility.GetAnimationClipSettings(clip);
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

        private static AnimatorController CreateStopController(
            AnimationClip clip)
        {
            DeleteAssetIfPresent(ControllerPath);
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static StopMetrics InspectState(
            Transform placementRoot,
            Transform stopSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            RequireApprovedRenderer(model, true);
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Pahur_07_Stop Animator does not match the stop contract.");
            }

            if (controller.layers.Length != 1 ||
                controller.layers[0].stateMachine.defaultState == null ||
                controller.layers[0].stateMachine.defaultState.name !=
                    StateName ||
                controller.layers[0].stateMachine.defaultState.motion != clip ||
                Mathf.Abs(clip.length - DurationSeconds) > Tolerance ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException(
                    "The Pahur stop clip or controller contract differs.");
            }

            var head = RequireDescendant(model, HeadBoneName);
            var headPath =
                AnimationUtility.CalculateTransformPath(head, model);
            var rendererPath =
                AnimationUtility.CalculateTransformPath(
                    renderer.transform,
                    model);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 5 ||
                bindings.Count(binding =>
                    binding.type == typeof(Transform) &&
                    binding.path == headPath &&
                    binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal)) != 4 ||
                bindings.Count(binding =>
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    binding.path == rendererPath &&
                    binding.propertyName == ShutdownBlendProperty) != 1 ||
                bindings.Any(binding =>
                    binding.path != headPath &&
                    binding.path != rendererPath))
            {
                throw new InvalidOperationException(
                    "The Pahur stop clip contains unexpected bindings.");
            }

            var shutdownBinding = bindings.Single(binding =>
                binding.propertyName == ShutdownBlendProperty);
            var shutdownCurve =
                AnimationUtility.GetEditorCurve(clip, shutdownBinding) ??
                throw new InvalidOperationException(
                    "The Pahur stop eye shutdown curve is missing.");
            if (shutdownCurve.length != 3 ||
                Mathf.Abs(shutdownCurve.keys[0].time) > Tolerance ||
                Mathf.Abs(shutdownCurve.keys[0].value) > Tolerance ||
                Mathf.Abs(
                    shutdownCurve.keys[1].time - TransitionSeconds) >
                    Tolerance ||
                Mathf.Abs(shutdownCurve.keys[1].value - 1f) > Tolerance ||
                Mathf.Abs(
                    shutdownCurve.keys[2].time - DurationSeconds) >
                    Tolerance ||
                Mathf.Abs(shutdownCurve.keys[2].value - 1f) > Tolerance)
            {
                throw new InvalidOperationException(
                    "The Pahur stop eye curve must transition over two seconds and hold for one second.");
            }

            var face = RequireStopMaterial(
                renderer,
                StopFaceMaterialPath,
                "face");
            foreach (var material in new[] { face })
            {
                if (material.shader == null ||
                    material.shader.name !=
                        "Bellerophon/Pahur/StopAppearance" ||
                    !Approximately(
                        material.GetColor("_ShutdownColor"),
                        ShutdownColor))
                {
                    throw new InvalidOperationException(
                        "A Pahur stop eye material differs from #030303.");
                }
            }

            foreach (var slot in placementRoot.Cast<Transform>())
            {
                if (slot == stopSlot)
                {
                    continue;
                }

                if (slot.GetComponentsInChildren<Renderer>(true)
                        .SelectMany(item => item.sharedMaterials)
                        .Any(item => item == face) ||
                    slot.GetComponentsInChildren<Animator>(true)
                        .Any(item =>
                            item.runtimeAnimatorController == controller))
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " incorrectly uses a Pahur stop asset.");
                }
            }

            var metrics = MeasureClip(model, clip, shutdownCurve);
            if (Mathf.Abs(
                    metrics.FinalHeadPitchDegrees -
                    HeadPitchDegrees) > 0.05f ||
                Mathf.Abs(
                    metrics.MidHeadPitchDegrees -
                    HeadPitchDegrees * 0.5f) > 0.05f ||
                Mathf.Abs(
                    metrics.HoldHeadPitchDegrees -
                    HeadPitchDegrees) > 0.05f ||
                Mathf.Abs(metrics.HoldHeadRotationChangeDegrees) > 0.01f ||
                Mathf.Abs(metrics.StartShutdownBlend) > Tolerance ||
                Mathf.Abs(metrics.MidShutdownBlend - 0.5f) > Tolerance ||
                Mathf.Abs(metrics.FinalShutdownBlend - 1f) > Tolerance ||
                Mathf.Abs(metrics.HoldShutdownBlend - 1f) > Tolerance)
            {
                throw new InvalidOperationException(
                    "Pahur stop motion metrics differ. FinalHeadPitch=" +
                    Num(metrics.FinalHeadPitchDegrees) +
                    ", MidHeadPitch=" +
                    Num(metrics.MidHeadPitchDegrees) +
                    ", HoldHeadPitch=" +
                    Num(metrics.HoldHeadPitchDegrees) +
                    ", HoldHeadRotationChange=" +
                    Num(metrics.HoldHeadRotationChangeDegrees) +
                    ", StartBlend=" +
                    Num(metrics.StartShutdownBlend) +
                    ", MidBlend=" +
                    Num(metrics.MidShutdownBlend) +
                    ", FinalBlend=" +
                    Num(metrics.FinalShutdownBlend) +
                    ", HoldBlend=" +
                    Num(metrics.HoldShutdownBlend) + ".");
            }

            return metrics;
        }

        private static StopMetrics MeasureClip(
            Transform sourceModel,
            AnimationClip clip,
            AnimationCurve shutdownCurve)
        {
            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject clone = null;
            try
            {
                clone = UnityEngine.Object.Instantiate(
                    sourceModel.gameObject);
                clone.name = "PahurStopInspectionClone";
                SceneManager.MoveGameObjectToScene(clone, previewScene);
                var animator = clone.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }

                var head = RequireDescendant(
                    clone.transform,
                    HeadBoneName);
                clip.SampleAnimation(clone, 0f);
                var start = head.localRotation;
                clip.SampleAnimation(clone, TransitionSeconds * 0.5f);
                var middle = head.localRotation;
                clip.SampleAnimation(
                    clone,
                    TransitionSeconds - 0.0001f);
                var transitionEnd = head.localRotation;
                clip.SampleAnimation(
                    clone,
                    TransitionSeconds + HoldSeconds * 0.5f);
                var holdMiddle = head.localRotation;
                clip.SampleAnimation(
                    clone,
                    DurationSeconds - 0.0001f);
                var holdEnd = head.localRotation;
                return new StopMetrics(
                    Quaternion.Angle(start, middle),
                    Quaternion.Angle(start, transitionEnd),
                    Quaternion.Angle(start, holdMiddle),
                    Quaternion.Angle(transitionEnd, holdEnd),
                    shutdownCurve.Evaluate(0f),
                    shutdownCurve.Evaluate(TransitionSeconds * 0.5f),
                    shutdownCurve.Evaluate(TransitionSeconds),
                    shutdownCurve.Evaluate(
                        TransitionSeconds + HoldSeconds * 0.5f));
            }
            finally
            {
                if (clone != null)
                {
                    UnityEngine.Object.DestroyImmediate(clone);
                }

                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static Material RequireStopMaterial(
            SkinnedMeshRenderer renderer,
            string path,
            string label)
        {
            var materials = renderer.sharedMaterials
                .Where(material =>
                    material != null &&
                    AssetDatabase.GetAssetPath(material) == path)
                .ToArray();
            if (materials.Length != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_07_Stop must use exactly one stop " + label +
                    " material. Count=" + materials.Length + ".");
            }

            return materials[0];
        }

        private static void CapturePoseStrip(
            Transform sourceModel,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur stop review folder."));
            var poseSnapshots =
                sourceModel.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformPoseSnapshot(item))
                    .ToArray();
            var targetRendererSnapshots =
                sourceModel.GetComponentsInChildren<Renderer>(true)
                    .Select(item => new RendererPropertySnapshot(item))
                    .ToArray();
            var otherRendererSnapshots =
                sourceModel.gameObject.scene.GetRootGameObjects()
                    .SelectMany(item =>
                        item.GetComponentsInChildren<Renderer>(true))
                    .Where(item =>
                        !item.transform.IsChildOf(sourceModel))
                    .Select(item => new RendererEnabledSnapshot(item))
                    .ToArray();
            var animator = RequireAnimator(sourceModel);
            var animatorEnabled = animator.enabled;
            var player = GameObject.Find("Player") ??
                         throw new InvalidOperationException(
                             "Player is missing.");
            var sourceCamera =
                player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var cameraObject = new GameObject(
                "PahurStopReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int panelWidth = 384;
            const int panelHeight = 640;
            var target = new RenderTexture(
                panelWidth,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var strip = new Texture2D(
                panelWidth * 5,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in otherRendererSnapshots)
                {
                    snapshot.Renderer.enabled = false;
                }

                animator.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor =
                    new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.aspect = panelWidth / (float)panelHeight;
                camera.targetTexture = target;
                var times =
                    new[] { 0f, 1f, 2f, 2.5f, 2.999f };
                clip.SampleAnimation(sourceModel.gameObject, 1f);
                FrameCamera(
                    camera,
                    sourceModel,
                    sourceCamera,
                    panelWidth / (float)panelHeight);
                for (var index = 0; index < times.Length; index++)
                {
                    clip.SampleAnimation(
                        sourceModel.gameObject,
                        times[index]);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(0f, 0f, panelWidth, panelHeight),
                        0,
                        0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 &&
                            pixel.b >= 240 &&
                            pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Pahur stop review contains Unity's magenta shader fallback.");
                    }

                    strip.SetPixels32(
                        index * panelWidth,
                        0,
                        panelWidth,
                        panelHeight,
                        pixels);
                }

                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var snapshot in otherRendererSnapshots)
                {
                    snapshot.Restore();
                }

                foreach (var snapshot in targetRendererSnapshots)
                {
                    snapshot.Restore();
                }

                foreach (var snapshot in poseSnapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(
            Camera camera,
            Transform model,
            Camera sourceCamera,
            float aspect)
        {
            var bounds = BoundsOf(model);
            var viewDirection =
                sourceCamera.transform.position - bounds.center;
            viewDirection.y = 0f;
            if (viewDirection.sqrMagnitude < 0.0001f)
            {
                viewDirection = -model.forward;
            }

            viewDirection.Normalize();
            camera.aspect = aspect;
            var verticalDistance =
                bounds.extents.y /
                Mathf.Tan(
                    camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov =
                2f * Mathf.Atan(
                    Mathf.Tan(
                        camera.fieldOfView * Mathf.Deg2Rad * 0.5f) *
                    aspect);
            var horizontalDistance =
                Mathf.Max(bounds.extents.x, bounds.extents.z) /
                Mathf.Tan(horizontalFov * 0.5f);
            var distance =
                Mathf.Max(verticalDistance, horizontalDistance) * 1.18f;
            camera.transform.position =
                bounds.center + viewDirection * distance +
                Vector3.up * (bounds.extents.y * 0.02f);
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static Bounds BoundsOf(Transform model)
        {
            var renderers =
                model.GetComponentsInChildren<Renderer>(false)
                    .Where(item =>
                        item.enabled && item.gameObject.activeInHierarchy)
                    .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Pahur_07_Stop has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void WriteValidationReport(StopMetrics metrics)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur stop validation folder."));
            var report = new StringBuilder()
                .AppendLine("Pahur 07 Stop Animation Validation")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine(
                    "Target=" + PlacementRootName + "/" +
                    StopSlotName + "/" + ModelName)
                .AppendLine("Clip=" + ClipPath)
                .AppendLine("Controller=" + ControllerPath)
                .AppendLine("State=" + StateName)
                .AppendLine("TransitionSeconds=2")
                .AppendLine("HoldSeconds=1")
                .AppendLine("DurationSeconds=3")
                .AppendLine("LoopEnabled=True")
                .AppendLine("ReturnSegment=False")
                .AppendLine("LoopBoundaryReset=Immediate")
                .AppendLine("FrameRate=60")
                .AppendLine("HeadBone=Head")
                .AppendLine("HeadPitchTargetDegrees=45")
                .AppendLine(
                    "MidHeadPitchDegrees=" +
                    Num(metrics.MidHeadPitchDegrees))
                .AppendLine(
                    "FinalHeadPitchDegrees=" +
                    Num(metrics.FinalHeadPitchDegrees))
                .AppendLine(
                    "HoldHeadPitchDegrees=" +
                    Num(metrics.HoldHeadPitchDegrees))
                .AppendLine(
                    "HoldHeadRotationChangeDegrees=" +
                    Num(metrics.HoldHeadRotationChangeDegrees))
                .AppendLine("EyeStart=ApprovedWarmRed")
                .AppendLine("EyeFinalHex=#030303")
                .AppendLine("EyeTransition=Linear")
                .AppendLine(
                    "StartShutdownBlend=" +
                    Num(metrics.StartShutdownBlend))
                .AppendLine(
                    "MidShutdownBlend=" +
                    Num(metrics.MidShutdownBlend))
                .AppendLine(
                    "FinalShutdownBlend=" +
                    Num(metrics.FinalShutdownBlend))
                .AppendLine(
                    "HoldShutdownBlend=" +
                    Num(metrics.HoldShutdownBlend))
                .AppendLine("TransformBindings=4")
                .AppendLine("MaterialBindings=1")
                .AppendLine("PositionBindings=0")
                .AppendLine("BlendShapeBindings=0")
                .AppendLine("RootMotion=False")
                .AppendLine("TargetOnlyFaceMaterial=True")
                .AppendLine("SharedApprovedShaderChanged=False")
                .AppendLine("SharedApprovedMaterialsChanged=False")
                .AppendLine("OtherPahurSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("SceneChangedByValidation=False");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static SkinnedMeshRenderer RequireApprovedRenderer(
            Transform model,
            bool allowStopMaterials)
        {
            var renderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_07_Stop must contain exactly one skinned renderer. Count=" +
                    renderers.Length + ".");
            }

            var renderer = renderers[0];
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           "Pahur_07_Stop has no shared mesh.");
            var triangles = Enumerable.Range(0, mesh.subMeshCount)
                .Sum(index =>
                    checked((int)mesh.GetIndexCount(index) / 3));
            if (AssetDatabase.GetAssetPath(mesh) != ModelPath ||
                triangles != ExpectedTriangles ||
                renderer.bones.Length != ExpectedBones ||
                renderer.sharedMaterials.Length != mesh.subMeshCount ||
                renderer.sharedMaterials.Any(material =>
                {
                    if (material == null)
                    {
                        return true;
                    }

                    var path = AssetDatabase.GetAssetPath(material);
                    return !path.StartsWith(
                           ApprovedMaterialFolder + "/",
                           StringComparison.Ordinal) &&
                           !(allowStopMaterials &&
                             path == StopFaceMaterialPath);
                }))
            {
                throw new InvalidOperationException(
                    "Pahur_07_Stop no longer uses the approved Pahur mesh and material contract.");
            }

            return renderer;
        }

        private static Animator GetOrCreateAnimator(Transform model)
        {
            var animators =
                model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Pahur_07_Stop must not contain multiple Animators. Count=" +
                    animators.Length + ".");
            }

            return animators.Length == 1
                ? animators[0]
                : model.gameObject.AddComponent<Animator>();
        }

        private static Animator RequireAnimator(Transform model)
        {
            var animators =
                model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_07_Stop must contain exactly one Animator. Count=" +
                    animators.Length + ".");
            }

            return animators[0];
        }

        private static Transform RequireDescendant(
            Transform root,
            string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                       .SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException(
                       "Required Pahur bone is missing or duplicated: " +
                       name + ".");
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 ||
                slot.GetChild(0).name != ModelName)
            {
                throw new InvalidOperationException(
                    StopSlotName +
                    " must contain exactly one Pahur_Model.");
            }

            return slot.GetChild(0);
        }

        private static Transform RequireDirectChild(
            Transform parent,
            string name)
        {
            return parent.Cast<Transform>()
                       .SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException(
                       "Required Pahur slot is missing: " + name + ".");
        }

        private static GameObject RequirePlacementRoot()
        {
            return GameObject.Find(PlacementRootName) ??
                   throw new InvalidOperationException(
                       "The Pahur placement root is missing.");
        }

        private static void RequireSlotContract(Transform root)
        {
            if (root.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "The Pahur placement must contain exactly eleven slots.");
            }

            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = root.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "The Pahur slot contract differs at index " +
                        index + ".");
                }
            }
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the current active scene. ActiveScene=" +
                    scene.path);
            }

            return scene;
        }

        private static string[] OtherSlotSignatures(Transform root)
        {
            return root.Cast<Transform>()
                .Where(item => item.name != StopSlotName)
                .Select(HierarchyAndAssetSignature)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string HierarchyAndAssetSignature(Transform slot)
        {
            var builder = new StringBuilder();
            foreach (var item in
                     slot.GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             item => RelativePath(slot, item),
                             StringComparer.Ordinal))
            {
                builder.Append(RelativePath(slot, item));
                builder.Append('|');
                builder.Append(Vec(item.localPosition));
                builder.Append('|');
                builder.Append(Quat(item.localRotation));
                builder.Append('|');
                builder.Append(Vec(item.localScale));
                builder.Append(';');
            }

            foreach (var renderer in
                     slot.GetComponentsInChildren<Renderer>(true))
            {
                builder.Append(
                    AssetDatabase.GetAssetPath(
                        (renderer as SkinnedMeshRenderer)?.sharedMesh));
                builder.Append('|');
                builder.Append(
                    string.Join(
                        ",",
                        renderer.sharedMaterials.Select(
                            AssetDatabase.GetAssetPath)));
                builder.Append(';');
            }

            foreach (var animator in
                     slot.GetComponentsInChildren<Animator>(true))
            {
                builder.Append(animator.enabled);
                builder.Append('|');
                builder.Append(animator.applyRootMotion);
                builder.Append('|');
                builder.Append(
                    AssetDatabase.GetAssetPath(
                        animator.runtimeAnimatorController));
                builder.Append(';');
            }

            return builder.ToString();
        }

        private static string[] PlacementTransformSignatures(
            Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(item =>
                    RelativePath(root, item) + "|" +
                    Vec(item.localPosition) + "|" +
                    Quat(item.localRotation) + "|" +
                    Vec(item.localScale))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .Select(item =>
                    GlobalObjectId.GetGlobalObjectIdSlow(item) + "|" +
                    item.name + "|" + item.activeSelf + "|" +
                    Vec(item.transform.position) + "|" +
                    Quat(item.transform.rotation) + "|" +
                    Vec(item.transform.localScale) + "|" +
                    item.transform.childCount)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string RelativePath(
            Transform root,
            Transform item)
        {
            return item == root
                ? root.name
                : root.name + "/" +
                  AnimationUtility.CalculateTransformPath(item, root);
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<TimedQuaternion> keys)
        {
            SetTransformCurve(
                clip,
                path,
                "m_LocalRotation.x",
                keys.Select(item =>
                    new Keyframe(item.Time, item.Value.x)));
            SetTransformCurve(
                clip,
                path,
                "m_LocalRotation.y",
                keys.Select(item =>
                    new Keyframe(item.Time, item.Value.y)));
            SetTransformCurve(
                clip,
                path,
                "m_LocalRotation.z",
                keys.Select(item =>
                    new Keyframe(item.Time, item.Value.z)));
            SetTransformCurve(
                clip,
                path,
                "m_LocalRotation.w",
                keys.Select(item =>
                    new Keyframe(item.Time, item.Value.w)));
        }

        private static void SetTransformCurve(
            AnimationClip clip,
            string path,
            string property,
            IEnumerable<Keyframe> keys)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                LinearCurve(keys.ToArray()));
        }

        private static AnimationCurve LinearCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys)
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

            return curve;
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent =
                Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) ||
                string.IsNullOrEmpty(name) ||
                !AssetDatabase.IsValidFolder(parent))
            {
                throw new InvalidOperationException(
                    "Invalid Pahur stop asset folder: " + folder);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) !=
                    null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException(
                    "Could not replace Pahur stop asset: " + path);
            }
        }

        private static void RequireSameSignatures(
            IReadOnlyList<string> before,
            IReadOnlyList<string> after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static bool Approximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= Tolerance &&
                   Mathf.Abs(a.g - b.g) <= Tolerance &&
                   Mathf.Abs(a.b - b.b) <= Tolerance &&
                   Mathf.Abs(a.a - b.a) <= Tolerance;
        }

        private static string Absolute(string assetPath)
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName ??
                       throw new InvalidOperationException(
                           "Unity project root is unavailable.");
            return Path.GetFullPath(
                Path.Combine(root, assetPath.Replace('/', '\\')));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + "," + Num(value.w);
        }

        private readonly struct TimedQuaternion
        {
            public TimedQuaternion(float time, Quaternion value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }
            public Quaternion Value { get; }
        }

        private sealed class TransformPoseSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformPoseSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void Restore()
            {
                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
            }
        }

        private sealed class RendererEnabledSnapshot
        {
            private readonly bool enabled;

            public RendererEnabledSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public Renderer Renderer { get; }

            public void Restore()
            {
                Renderer.enabled = enabled;
            }
        }

        private sealed class RendererPropertySnapshot
        {
            private readonly Renderer renderer;
            private readonly MaterialPropertyBlock properties = new();

            public RendererPropertySnapshot(Renderer renderer)
            {
                this.renderer = renderer;
                renderer.GetPropertyBlock(properties);
            }

            public void Restore()
            {
                renderer.SetPropertyBlock(properties);
            }
        }

        private readonly struct StopMetrics
        {
            public StopMetrics(
                float midHeadPitchDegrees,
                float finalHeadPitchDegrees,
                float holdHeadPitchDegrees,
                float holdHeadRotationChangeDegrees,
                float startShutdownBlend,
                float midShutdownBlend,
                float finalShutdownBlend,
                float holdShutdownBlend)
            {
                MidHeadPitchDegrees = midHeadPitchDegrees;
                FinalHeadPitchDegrees = finalHeadPitchDegrees;
                HoldHeadPitchDegrees = holdHeadPitchDegrees;
                HoldHeadRotationChangeDegrees =
                    holdHeadRotationChangeDegrees;
                StartShutdownBlend = startShutdownBlend;
                MidShutdownBlend = midShutdownBlend;
                FinalShutdownBlend = finalShutdownBlend;
                HoldShutdownBlend = holdShutdownBlend;
            }

            public float MidHeadPitchDegrees { get; }
            public float FinalHeadPitchDegrees { get; }
            public float HoldHeadPitchDegrees { get; }
            public float HoldHeadRotationChangeDegrees { get; }
            public float StartShutdownBlend { get; }
            public float MidShutdownBlend { get; }
            public float FinalShutdownBlend { get; }
            public float HoldShutdownBlend { get; }
        }
    }
}
