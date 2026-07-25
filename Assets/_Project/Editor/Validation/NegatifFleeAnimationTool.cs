using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bellerophon.Editor.NegatifCargoRunScene
{
    internal static class NegatifFleeAnimationTool
    {
        private const string PlacementRootName = "Approved Negatif Enemy Placement";
        private const string FleeSlotName = "Negatif_05_Flee";
        private const string ModelName = "Negatif_Model";
        private const string PlayerName = "Player";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Negatif/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Negatif/Controllers";
        private const string FleeClipPath =
            AnimationFolder + "/Negatif_05_Flee_QuadrupedTail.anim";
        private const string MoveClipPath =
            AnimationFolder + "/Negatif_02_Move_Quadruped.anim";
        private const string FleeControllerPath =
            ControllerFolder + "/Negatif_05_Flee_QuadrupedTail.controller";
        private const string FleeStateName = "Flee";
        // A six-second loop is the shortest common loop for the 0.4-second gait
        // and the three-second tail swing requested for this review object.
        private const float FleeLoopSeconds = 6f;
        private const float GaitCycleSeconds = 0.4f;
        private const float TailCycleSeconds = 3f;
        private const float TailSwingDegrees = 45f;
        private const float PoseStepSeconds = 0.05f;
        private const int PanelWidth = 520;
        private const int PanelHeight = 600;

        [MenuItem("Bellerophon/Enemies/Negatif/Apply Flee Animation")]
        public static void ApplyFleeAnimation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the current active scene.");
            }

            var placementRoot = GameObject.Find(PlacementRootName) ??
                                throw new InvalidOperationException(
                                    PlacementRootName + " is missing.");
            var slot = placementRoot.transform.Find(FleeSlotName) ??
                       throw new InvalidOperationException(
                           FleeSlotName + " is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(
                            ModelName + " is missing under " + FleeSlotName + ".");

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var clip = CreateFleeClip(slot, model);
            var controller = CreateFleeController(clip);
            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(slot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Negatif flee application.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "NegatifFleeAnimationApplied " +
                "Slot=" + FleeSlotName +
                ", Clip=" + FleeClipPath +
                ", Controller=" + FleeControllerPath +
                ", LoopSeconds=" + FleeLoopSeconds.ToString("0.###") +
                ", GaitCycleSeconds=" + GaitCycleSeconds.ToString("0.###") +
                ", MoveSpeedMultiplier=2.5" +
                ", Gait=ExactMoveClipCurveCopy" +
                ", MoveSource=" + MoveClipPath +
                ", TailRoot=Bone_021" +
                ", TailChain=Bone_021_to_Bone_016" +
                ", TailCycleSeconds=" + TailCycleSeconds.ToString("0.###") +
                ", TailSwingDegrees=PlusMinus" +
                TailSwingDegrees.ToString("0.###") +
                ", MoveCurves=AllSourceFloatBindings" +
                ", RootMotion=False" +
                ", SceneSaved=True.");
        }

        internal static void CaptureRuntimeFrame(string path)
        {
            var placementRoot = GameObject.Find(PlacementRootName) ??
                                throw new InvalidOperationException(
                                    PlacementRootName + " is missing in Play Mode.");
            var slot = placementRoot.transform.Find(FleeSlotName) ??
                       throw new InvalidOperationException(
                           FleeSlotName + " is missing in Play Mode.");
            CapturePanel(slot, path);
        }

        internal static void ComposeRuntimeReview(
            IReadOnlyList<string> panelPaths,
            string outputPath)
        {
            const int columns = 5;
            var rows = Mathf.CeilToInt(panelPaths.Count / (float)columns);
            var sheet = new Texture2D(
                PanelWidth * columns,
                PanelHeight * rows,
                TextureFormat.RGBA32,
                false);
            sheet.SetPixels32(
                Enumerable.Repeat(
                        new Color32(4, 6, 8, 255),
                        sheet.width * sheet.height)
                    .ToArray());

            try
            {
                for (var index = 0; index < panelPaths.Count; index++)
                {
                    var panel = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        if (!panel.LoadImage(File.ReadAllBytes(panelPaths[index])))
                        {
                            throw new InvalidOperationException(
                                "Could not decode flee panel " +
                                panelPaths[index] + ".");
                        }

                        var column = index % columns;
                        var rowFromTop = index / columns;
                        var y = (rows - rowFromTop - 1) * PanelHeight;
                        sheet.SetPixels(
                            column * PanelWidth,
                            y,
                            PanelWidth,
                            PanelHeight,
                            panel.GetPixels());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                    }
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(outputPath) ??
                    throw new InvalidOperationException(
                        "Invalid flee review folder."));
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static AnimationClip CreateFleeClip(
            Transform slot,
            Transform model)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(FleeClipPath) != null)
            {
                AssetDatabase.DeleteAsset(FleeClipPath);
            }

            var moveClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveClipPath) ??
                           throw new InvalidOperationException(
                               "Approved Negatif move clip is missing at " +
                               MoveClipPath + ".");
            var tail = RequireDescendant(model, "Bone_021");
            var requiredTailChain = new[]
            {
                "Bone_021", "Bone_020", "Bone_019",
                "Bone_018", "Bone_017", "Bone_016"
            }.Select(name => RequireDescendant(model, name)).ToArray();
            for (var index = 1; index < requiredTailChain.Length; index++)
            {
                if (requiredTailChain[index].parent != requiredTailChain[index - 1])
                {
                    throw new InvalidOperationException(
                        "Negatif tail hierarchy is not Bone_021 through Bone_016.");
                }
            }

            var tailRestWorldRotation = tail.rotation;
            var clip = new AnimationClip
            {
                name = "Negatif_05_Flee_QuadrupedTail",
                frameRate = 60f
            };
            foreach (var binding in AnimationUtility.GetCurveBindings(moveClip))
            {
                var moveCurve = AnimationUtility.GetEditorCurve(moveClip, binding);
                if (moveCurve == null)
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    RepeatMoveCurve(moveCurve));
            }

            var tailKeys = new List<QuaternionKey>();
            var poseCount =
                Mathf.RoundToInt(FleeLoopSeconds / PoseStepSeconds);
            for (var poseIndex = 0; poseIndex <= poseCount; poseIndex++)
            {
                var time = poseIndex == poseCount
                    ? FleeLoopSeconds
                    : poseIndex * PoseStepSeconds;
                var tailAngle =
                    Mathf.Sin(time / TailCycleSeconds * Mathf.PI * 2f) *
                    TailSwingDegrees;
                var worldRotation =
                    Quaternion.AngleAxis(tailAngle, Vector3.up) *
                    tailRestWorldRotation;
                tailKeys.Add(
                    new QuaternionKey(
                        time,
                        Quaternion.Inverse(tail.parent.rotation) *
                        worldRotation));
            }

            SetQuaternionCurves(
                clip,
                AnimationUtility.CalculateTransformPath(tail, slot),
                tailKeys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, FleeClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationCurve RepeatMoveCurve(AnimationCurve source)
        {
            const int gaitCycleCount = 15;
            var keys = new List<Keyframe>(
                source.length * gaitCycleCount);
            for (var cycleIndex = 0;
                 cycleIndex < gaitCycleCount;
                 cycleIndex++)
            {
                foreach (var sourceKey in source.keys)
                {
                    if (cycleIndex > 0 &&
                        sourceKey.time <= 0.00001f)
                    {
                        continue;
                    }

                    keys.Add(
                        new Keyframe(
                            cycleIndex * GaitCycleSeconds +
                            sourceKey.time / 2.5f,
                            sourceKey.value));
                }
            }

            var curve = new AnimationCurve(keys.ToArray());
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

        private static AnimatorController CreateFleeController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(FleeControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(FleeControllerPath);
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(FleeControllerPath);
            var state = controller.layers[0].stateMachine.AddState(FleeStateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<QuaternionKey> values)
        {
            var continuityValues = new List<QuaternionKey>(values.Count);
            Quaternion? previous = null;
            foreach (var value in values)
            {
                var rotation = value.Rotation;
                if (previous.HasValue &&
                    Quaternion.Dot(previous.Value, rotation) < 0f)
                {
                    rotation = new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w);
                }

                continuityValues.Add(
                    new QuaternionKey(value.Time, rotation));
                previous = rotation;
            }

            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.x",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.x)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.y",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.y)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.z",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.z)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.w",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.w)).ToArray());
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
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
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                curve);
        }

        private static Transform RequireDescendant(
            Transform root,
            string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + name +
                    " under " + root.name +
                    ", found " + matches.Length + ".");
            }

            return matches[0];
        }

        private static void CapturePanel(Transform slot, string path)
        {
            var bounds = BoundsOf(slot);
            var hiddenRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(item =>
                    item.enabled &&
                    !item.transform.IsChildOf(slot))
                .ToArray();
            foreach (var renderer in hiddenRenderers)
            {
                renderer.enabled = false;
            }

            var player = GameObject.Find(PlayerName);
            var sourceCamera = player != null
                ? player.GetComponentInChildren<Camera>(true)
                : null;
            var cameraObject = new GameObject(
                "NegatifFleeCaptureCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject(
                "NegatifFleeKeyLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject(
                "NegatifFleeFillLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                if (sourceCamera != null)
                {
                    camera.CopyFrom(sourceCamera);
                }

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.015f, 0.02f, 0.025f, 1f);
                camera.fieldOfView = 34f;
                camera.aspect = 1f;
                camera.orthographic = true;
                camera.orthographicSize =
                    Mathf.Max(0.2f, bounds.extents.magnitude * 1.12f);
                camera.nearClipPlane = 0.005f;
                camera.farClipPlane = 100f;

                var front = slot.forward.normalized;
                var right = slot.right.normalized;
                var distance = Mathf.Max(1f, bounds.extents.magnitude * 4f);
                var focus =
                    bounds.center -
                    Vector3.up * bounds.extents.y * 0.08f;
                camera.transform.position =
                    focus -
                    front * distance * 0.72f +
                    right * distance * 0.72f +
                    Vector3.up * bounds.extents.y * 0.2f;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);

                var keyLight = keyLightObject.GetComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(0.78f, 0.9f, 1f);
                keyLight.intensity = 2.2f;
                keyLight.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

                var fillLight = fillLightObject.GetComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.35f, 0.75f, 1f);
                fillLight.intensity = 9f;
                fillLight.range = distance * 2.5f;
                fillLight.transform.position =
                    focus +
                    front * distance * 0.35f -
                    right * distance * 0.25f +
                    Vector3.up * bounds.extents.y * 0.4f;

                Capture(camera, path, PanelWidth, PanelHeight);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fillLightObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                foreach (var renderer in hiddenRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }
            }
        }

        private static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    root.name + " has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void Capture(
            Camera camera,
            string path,
            int width,
            int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "Invalid Negatif flee capture folder."));
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private readonly struct QuaternionKey
        {
            public readonly float Time;
            public readonly Quaternion Rotation;

            public QuaternionKey(float time, Quaternion rotation)
            {
                Time = time;
                Rotation = rotation;
            }
        }

    }
}
