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
    internal static class NegatifHitReactionAnimationTool
    {
        private const string PlacementRootName = "Approved Negatif Enemy Placement";
        private const string HitSlotName = "Negatif_04_Hit_Reaction";
        private const string ModelName = "Negatif_Model";
        private const string PlayerName = "Player";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Negatif/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Negatif/Controllers";
        private const string HitClipPath =
            AnimationFolder + "/Negatif_04_Hit_Reaction_Left.anim";
        private const string HitControllerPath =
            ControllerFolder + "/Negatif_04_Hit_Reaction_Left.controller";
        private const string HitStateName = "HitReaction";
        private const float HitSeconds = 0.7f;
        private const float PeakTimeSeconds = 0.14f;
        private const float LeftTiltDegrees = 15f;
        private const int PanelWidth = 520;
        private const int PanelHeight = 600;

        private static readonly PoseKey[] PoseKeys =
        {
            new PoseKey(0f, 0f),
            new PoseKey(PeakTimeSeconds, 1f),
            new PoseKey(HitSeconds, 0f)
        };

        [MenuItem("Bellerophon/Enemies/Negatif/Apply Hit Reaction Animation")]
        public static void ApplyHitReactionAnimation()
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
            var slot = placementRoot.transform.Find(HitSlotName) ??
                       throw new InvalidOperationException(
                           HitSlotName + " is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(
                            ModelName + " is missing under " + HitSlotName + ".");

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var clip = CreateHitClip(slot, model);
            var controller = CreateHitController(clip);
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
                    "CargoRunMvp could not be saved after Negatif hit reaction application.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "NegatifHitReactionAnimationApplied " +
                "Slot=" + HitSlotName +
                ", Clip=" + HitClipPath +
                ", Controller=" + HitControllerPath +
                ", Duration=" + HitSeconds.ToString("0.###") +
                ", PeakTime=" + PeakTimeSeconds.ToString("0.###") +
                ", Direction=NegatifLeft" +
                ", TiltDegrees=" + LeftTiltDegrees.ToString("0.###") +
                ", Pivot=VisibleBoundsBottomCenter" +
                ", AnimatedTransform=Negatif_Model" +
                ", BoneCurves=0" +
                ", MeshDeformation=False" +
                ", SlotRootMotion=False" +
                ", ReturnToRest=True" +
                ", SceneSaved=True.");
        }

        internal static void CaptureRuntimeFrame(string path)
        {
            var placementRoot = GameObject.Find(PlacementRootName) ??
                                throw new InvalidOperationException(
                                    PlacementRootName + " is missing in Play Mode.");
            var slot = placementRoot.transform.Find(HitSlotName) ??
                       throw new InvalidOperationException(
                           HitSlotName + " is missing in Play Mode.");
            CapturePanel(slot, path);
        }

        internal static void ComposeRuntimeReview(
            IReadOnlyList<string> panelPaths,
            string outputPath)
        {
            const int columns = 4;
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
                                "Could not decode hit reaction panel " +
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
                        "Invalid hit reaction review folder."));
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static AnimationClip CreateHitClip(
            Transform slot,
            Transform model)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(HitClipPath) != null)
            {
                AssetDatabase.DeleteAsset(HitClipPath);
            }

            var visibleBounds = BoundsOf(model);
            var pivot = new Vector3(
                visibleBounds.center.x,
                visibleBounds.min.y,
                visibleBounds.center.z);
            var baseWorldPosition = model.position;
            var baseWorldRotation = model.rotation;
            var modelPositionKeys = new List<VectorKey>(PoseKeys.Length);
            var modelRotationKeys = new List<QuaternionKey>(PoseKeys.Length);

            foreach (var pose in PoseKeys)
            {
                // Positive rotation around the unit's forward axis moves its up vector
                // toward -slot.right, which is Negatif's left.
                var rigidTilt = Quaternion.AngleAxis(
                    LeftTiltDegrees * pose.Weight,
                    slot.forward);
                var worldPosition =
                    pivot + rigidTilt * (baseWorldPosition - pivot);
                var worldRotation = rigidTilt * baseWorldRotation;
                modelPositionKeys.Add(
                    new VectorKey(
                        pose.Time,
                        model.parent.InverseTransformPoint(worldPosition)));
                modelRotationKeys.Add(
                    new QuaternionKey(
                        pose.Time,
                        Quaternion.Inverse(model.parent.rotation) *
                        worldRotation));
            }

            var clip = new AnimationClip
            {
                name = "Negatif_04_Hit_Reaction_Left",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var modelPath = AnimationUtility.CalculateTransformPath(model, slot);
            SetVectorCurves(
                clip,
                modelPath,
                "m_LocalPosition",
                modelPositionKeys);
            SetQuaternionCurves(clip, modelPath, modelRotationKeys);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, HitClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateHitController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(HitControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(HitControllerPath);
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(HitControllerPath);
            var state = controller.layers[0].stateMachine.AddState(HitStateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void SetVectorCurves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            IReadOnlyList<VectorKey> values)
        {
            SetLinearCurve(
                clip,
                path,
                propertyPrefix + ".x",
                values.Select(
                    value => new Keyframe(value.Time, value.Value.x)).ToArray());
            SetLinearCurve(
                clip,
                path,
                propertyPrefix + ".y",
                values.Select(
                    value => new Keyframe(value.Time, value.Value.y)).ToArray());
            SetLinearCurve(
                clip,
                path,
                propertyPrefix + ".z",
                values.Select(
                    value => new Keyframe(value.Time, value.Value.z)).ToArray());
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<QuaternionKey> values)
        {
            var continuous = new List<QuaternionKey>(values.Count);
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

                continuous.Add(new QuaternionKey(value.Time, rotation));
                previous = rotation;
            }

            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.x",
                continuous.Select(
                    value => new Keyframe(value.Time, value.Rotation.x)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.y",
                continuous.Select(
                    value => new Keyframe(value.Time, value.Rotation.y)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.z",
                continuous.Select(
                    value => new Keyframe(value.Time, value.Rotation.z)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.w",
                continuous.Select(
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
                "NegatifHitReactionCaptureCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject(
                "NegatifHitReactionKeyLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject(
                "NegatifHitReactionFillLight",
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
                var focus = bounds.center;
                camera.transform.position =
                    focus + front * distance + right * distance * 0.12f +
                    Vector3.up * bounds.extents.y * 0.08f;
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
                    focus - front * distance * 0.35f -
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
                    "Invalid Negatif hit reaction capture folder."));
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

        private readonly struct PoseKey
        {
            public readonly float Time;
            public readonly float Weight;

            public PoseKey(float time, float weight)
            {
                Time = time;
                Weight = weight;
            }
        }

        private readonly struct VectorKey
        {
            public readonly float Time;
            public readonly Vector3 Value;

            public VectorKey(float time, Vector3 value)
            {
                Time = time;
                Value = value;
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
