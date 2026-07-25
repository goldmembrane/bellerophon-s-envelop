using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bellerophon.Editor.NegatifCargoRunScene
{
    internal static class NegatifDeathAnimationTool
    {
        private const string PlacementRootName = "Approved Negatif Enemy Placement";
        private const string DeathSlotName = "Negatif_06_Death";
        private const string ModelName = "Negatif_Model";
        private const string PlayerName = "Player";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Negatif/Models/Negatif_Glb_ApprovedAppearance.glb";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Negatif/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Negatif/Controllers";
        private const string DeathClipPath =
            AnimationFolder + "/Negatif_06_Death_RightRoll.anim";
        private const string DeathControllerPath =
            ControllerFolder + "/Negatif_06_Death_RightRoll.controller";
        private const string DeathStateName = "DeathRightRoll";
        private const float DeathSeconds = 2.5f;
        private const float FallEndSeconds = 1.125f;
        private const float FinalRollDegrees = -180f;
        private const float RightTravelWidthRatio = 0.65f;
        private const int PanelWidth = 560;
        private const int PanelHeight = 560;

        private static readonly PoseKey[] PoseKeys =
        {
            new PoseKey(0f, 0f),
            new PoseKey(0.15f, 0.02f),
            new PoseKey(0.275f, 0.07f),
            new PoseKey(0.4f, 0.16f),
            new PoseKey(0.525f, 0.29f),
            new PoseKey(0.65f, 0.45f),
            new PoseKey(0.775f, 0.62f),
            new PoseKey(0.9f, 0.78f),
            new PoseKey(1.025f, 0.92f),
            new PoseKey(FallEndSeconds, 1f),
            new PoseKey(DeathSeconds, 1f)
        };

        [MenuItem("Bellerophon/Enemies/Negatif/Apply Death Animation")]
        public static void ApplyDeathAnimation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the current active scene.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes. Save or discard them before applying the Negatif death animation.");
            }

            var placementRoot = GameObject.Find(PlacementRootName) ??
                                throw new InvalidOperationException(
                                    PlacementRootName + " is missing.");
            var slot = placementRoot.transform.Find(DeathSlotName) ??
                       throw new InvalidOperationException(
                           DeathSlotName + " is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(
                            ModelName + " is missing under " + DeathSlotName + ".");

            var modelHashBefore = Sha256(Absolute(ModelPath));
            var protectedRootsBefore = ProtectedRootSignatures(scene);
            var otherSlotsBefore = OtherSlotSignatures(placementRoot.transform);
            var placementTransformBefore = TransformSignature(placementRoot.transform);
            var slotTransformBefore = TransformSignature(slot);
            var modelScaleBefore = model.localScale;

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var clip = CreateDeathClip(slot, model);
            var controller = CreateDeathController(clip);
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

            RequireDeathContract(slot, model, clip, controller);
            if (placementTransformBefore != TransformSignature(placementRoot.transform) ||
                slotTransformBefore != TransformSignature(slot) ||
                modelScaleBefore != model.localScale)
            {
                throw new InvalidOperationException(
                    "Negatif placement, death slot, or model scale changed while applying the death animation.");
            }

            if (!otherSlotsBefore.SequenceEqual(
                    OtherSlotSignatures(placementRoot.transform),
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A Negatif slot outside Negatif_06_Death changed.");
            }

            if (!protectedRootsBefore.SequenceEqual(
                    ProtectedRootSignatures(scene),
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside the Negatif placement changed.");
            }

            if (modelHashBefore != Sha256(Absolute(ModelPath)))
            {
                throw new InvalidOperationException(
                    "The whisker-removed Negatif GLB changed while applying the death animation.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Negatif death animation application.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "NegatifDeathAnimationApplied " +
                "Slot=" + DeathSlotName +
                ", Clip=" + DeathClipPath +
                ", Controller=" + DeathControllerPath +
                ", Duration=" + DeathSeconds.ToString("0.###") +
                ", FallEnd=" + FallEndSeconds.ToString("0.###") +
                ", Direction=NegatifRight" +
                ", RollDegrees=" + FinalRollDegrees.ToString("0.###") +
                ", FinalPose=BellyUp" +
                ", Loop=True" +
                ", AnimatedTransform=Negatif_Model" +
                ", BoneCurves=4" +
                ", TailRoot=Bone_021" +
                ", TailChain=Bone_021_to_Bone_016" +
                ", TailGravityCompensation=WorldRestRotation" +
                ", MeshDeformation=False" +
                ", SlotRootMotion=False" +
                ", OtherNegatifSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", WhiskerRemovedModelHashPreserved=True" +
                ", SceneSaved=True.");
        }

        internal static void CaptureRuntimeFrame(string path)
        {
            var placementRoot = GameObject.Find(PlacementRootName) ??
                                throw new InvalidOperationException(
                                    PlacementRootName + " is missing in Play Mode.");
            var slot = placementRoot.transform.Find(DeathSlotName) ??
                       throw new InvalidOperationException(
                           DeathSlotName + " is missing in Play Mode.");
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
                                "Could not decode death animation panel " +
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
                        "Invalid death animation review folder."));
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static AnimationClip CreateDeathClip(
            Transform slot,
            Transform model)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DeathClipPath) != null)
            {
                AssetDatabase.DeleteAsset(DeathClipPath);
            }

            var visibleBounds = BoundsOf(model);
            var worldCorners = BoundsCorners(visibleBounds);
            var groundY = visibleBounds.min.y;
            var baseWorldPosition = model.position;
            var baseWorldRotation = model.rotation;
            var tail = RequireDescendant(model, "Bone_021");
            var tailChain = new[]
            {
                "Bone_021", "Bone_020", "Bone_019",
                "Bone_018", "Bone_017", "Bone_016"
            }.Select(name => RequireDescendant(model, name)).ToArray();
            for (var index = 1; index < tailChain.Length; index++)
            {
                if (tailChain[index].parent != tailChain[index - 1])
                {
                    throw new InvalidOperationException(
                        "Negatif tail hierarchy is not Bone_021 through Bone_016.");
                }
            }

            var tailRestWorldRotation = tail.rotation;
            var tailParentRestWorldRotation = tail.parent.rotation;
            var rightTravel =
                Mathf.Max(0.08f, ProjectedSize(worldCorners, slot.right) *
                    RightTravelWidthRatio);
            var modelPositionKeys = new List<VectorKey>(PoseKeys.Length);
            var modelRotationKeys = new List<QuaternionKey>(PoseKeys.Length);
            var tailRotationKeys = new List<QuaternionKey>(PoseKeys.Length);

            foreach (var pose in PoseKeys)
            {
                var rigidRoll = Quaternion.AngleAxis(
                    FinalRollDegrees * pose.Progress,
                    slot.forward);
                var tentativeWorldPosition =
                    baseWorldPosition + slot.right * rightTravel * pose.Progress;
                var lowestY = float.PositiveInfinity;
                foreach (var corner in worldCorners)
                {
                    var rotatedCorner =
                        tentativeWorldPosition +
                        rigidRoll * (corner - baseWorldPosition);
                    lowestY = Mathf.Min(lowestY, rotatedCorner.y);
                }

                var groundedWorldPosition =
                    tentativeWorldPosition + Vector3.up * (groundY - lowestY);
                var worldRotation = rigidRoll * baseWorldRotation;
                modelPositionKeys.Add(
                    new VectorKey(
                        pose.Time,
                        model.parent.InverseTransformPoint(groundedWorldPosition)));
                modelRotationKeys.Add(
                    new QuaternionKey(
                        pose.Time,
                        Quaternion.Inverse(model.parent.rotation) *
                        worldRotation));

                // The model rolls as a rigid unit, but the tail root counter-rotates
                // by the same world roll so its chain keeps the rest-world
                // orientation and continues to hang toward the floor.
                var animatedTailParentWorldRotation =
                    rigidRoll * tailParentRestWorldRotation;
                tailRotationKeys.Add(
                    new QuaternionKey(
                        pose.Time,
                        Quaternion.Inverse(animatedTailParentWorldRotation) *
                        tailRestWorldRotation));
            }

            var clip = new AnimationClip
            {
                name = "Negatif_06_Death_RightRoll",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var modelBindingPath =
                AnimationUtility.CalculateTransformPath(model, slot);
            SetVectorCurves(
                clip,
                modelBindingPath,
                "m_LocalPosition",
                modelPositionKeys);
            SetQuaternionCurves(clip, modelBindingPath, modelRotationKeys);
            SetQuaternionCurves(
                clip,
                AnimationUtility.CalculateTransformPath(tail, slot),
                tailRotationKeys);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, DeathClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateDeathController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DeathControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(DeathControllerPath);
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(DeathControllerPath);
            var state = controller.layers[0].stateMachine.AddState(DeathStateName);
            state.motion = clip;
            state.writeDefaultValues = true;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void RequireDeathContract(
            Transform slot,
            Transform model,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (Mathf.Abs(clip.length - DeathSeconds) > 0.001f ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException(
                    "Negatif death clip must be a looping four-second clip.");
            }

            var expectedPath = AnimationUtility.CalculateTransformPath(model, slot);
            var tail = RequireDescendant(model, "Bone_021");
            var expectedTailPath =
                AnimationUtility.CalculateTransformPath(tail, slot);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 11 ||
                bindings.Count(binding =>
                    binding.path == expectedPath &&
                    binding.type == typeof(Transform)) != 7 ||
                bindings.Count(binding =>
                    binding.path == expectedTailPath &&
                    binding.type == typeof(Transform)) != 4 ||
                bindings.Any(binding =>
                    binding.path != expectedPath &&
                    binding.path != expectedTailPath) ||
                bindings.Any(binding =>
                    binding.propertyName.StartsWith(
                        "blendShape.",
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Negatif death clip must animate only Negatif_Model position/rotation and Bone_021 rotation.");
            }

            var animator = slot.GetComponent<Animator>();
            if (animator == null ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Negatif death slot Animator contract is invalid.");
            }

            var finalUp =
                Quaternion.AngleAxis(FinalRollDegrees, slot.forward) * model.up;
            if (Vector3.Dot(finalUp.normalized, Vector3.up) > -0.999f)
            {
                throw new InvalidOperationException(
                    "Negatif final death pose does not face its belly upward.");
            }

            var finalRoll =
                Quaternion.AngleAxis(FinalRollDegrees, slot.forward);
            var finalTailLocalRotation =
                Quaternion.Inverse(finalRoll * tail.parent.rotation) *
                tail.rotation;
            var compensatedTailWorldRotation =
                finalRoll * tail.parent.rotation * finalTailLocalRotation;
            if (Quaternion.Angle(
                    compensatedTailWorldRotation,
                    tail.rotation) > 0.01f)
            {
                throw new InvalidOperationException(
                    "Negatif tail gravity compensation does not preserve its floor-oriented world rotation.");
            }
        }

        private static Transform RequireDescendant(
            Transform root,
            string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == name)
                {
                    return transform;
                }
            }

            throw new InvalidOperationException(
                name + " is missing under " + root.name + ".");
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
                values.Select(value => new Keyframe(value.Time, value.Value.x)).ToArray());
            SetLinearCurve(
                clip,
                path,
                propertyPrefix + ".y",
                values.Select(value => new Keyframe(value.Time, value.Value.y)).ToArray());
            SetLinearCurve(
                clip,
                path,
                propertyPrefix + ".z",
                values.Select(value => new Keyframe(value.Time, value.Value.z)).ToArray());
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
                .Where(renderer =>
                    renderer.enabled &&
                    !renderer.transform.IsChildOf(slot))
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
                "NegatifDeathCaptureCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject(
                "NegatifDeathKeyLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject(
                "NegatifDeathFillLight",
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
                camera.orthographic = true;
                camera.orthographicSize =
                    Mathf.Max(0.38f, bounds.extents.magnitude * 1.35f);
                camera.aspect = 1f;
                camera.nearClipPlane = 0.005f;
                camera.farClipPlane = 100f;

                var front = slot.forward.normalized;
                var right = slot.right.normalized;
                var distance = Mathf.Max(1f, bounds.extents.magnitude * 4.5f);
                var focus = bounds.center;
                camera.transform.position =
                    focus + front * distance + right * distance * 0.62f +
                    Vector3.up * distance * 0.58f;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);

                var keyLight = keyLightObject.GetComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(0.82f, 0.9f, 1f);
                keyLight.intensity = 2.4f;
                keyLight.transform.rotation = Quaternion.Euler(38f, -36f, 0f);

                var fillLight = fillLightObject.GetComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.4f, 0.72f, 1f);
                fillLight.intensity = 10f;
                fillLight.range = distance * 2.5f;
                fillLight.transform.position =
                    focus - front * distance * 0.3f -
                    right * distance * 0.2f +
                    Vector3.up * distance * 0.4f;

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

        private static void Capture(
            Camera camera,
            string path,
            int width,
            int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "Invalid Negatif death capture folder."));
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
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.enabled &&
                    renderer.gameObject.activeInHierarchy)
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

        private static Vector3[] BoundsCorners(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            return new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };
        }

        private static float ProjectedSize(
            IEnumerable<Vector3> points,
            Vector3 axis)
        {
            var projections = points.Select(point => Vector3.Dot(point, axis)).ToArray();
            return projections.Max() - projections.Min();
        }

        private static string[] OtherSlotSignatures(Transform placementRoot)
        {
            return placementRoot.Cast<Transform>()
                .Where(slot => slot.name != DeathSlotName)
                .OrderBy(slot => slot.name, StringComparer.Ordinal)
                .Select(HierarchySignature)
                .ToArray();
        }

        private static string[] ProtectedRootSignatures(UnityEngine.SceneManagement.Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != PlacementRootName)
                .OrderBy(root => root.name, StringComparer.Ordinal)
                .Select(root => TransformSignature(root.transform))
                .ToArray();
        }

        private static string HierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item =>
                             AnimationUtility.CalculateTransformPath(item, root),
                             StringComparer.Ordinal))
            {
                builder.Append(AnimationUtility.CalculateTransformPath(item, root))
                    .Append('|')
                    .Append(TransformSignature(item))
                    .Append('|')
                    .Append(item.gameObject.activeSelf)
                    .Append(';');
            }

            var animator = root.GetComponent<Animator>();
            if (animator != null)
            {
                builder.Append("Animator=")
                    .Append(AssetDatabase.GetAssetPath(
                        animator.runtimeAnimatorController))
                    .Append('|')
                    .Append(animator.applyRootMotion)
                    .Append('|')
                    .Append(animator.cullingMode);
            }

            return builder.ToString();
        }

        private static string TransformSignature(Transform transform)
        {
            return transform.name + "|" +
                   Vec(transform.localPosition) + "|" +
                   Quat(transform.localRotation) + "|" +
                   Vec(transform.localScale) + "|" +
                   transform.childCount;
        }

        private static string Vec(Vector3 value)
        {
            return value.x.ToString("R") + "," +
                   value.y.ToString("R") + "," +
                   value.z.ToString("R");
        }

        private static string Quat(Quaternion value)
        {
            return value.x.ToString("R") + "," +
                   value.y.ToString("R") + "," +
                   value.z.ToString("R") + "," +
                   value.w.ToString("R");
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

        private static string Absolute(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                              throw new InvalidOperationException(
                                  "Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private readonly struct PoseKey
        {
            public PoseKey(float time, float progress)
            {
                Time = time;
                Progress = progress;
            }

            public float Time { get; }
            public float Progress { get; }
        }

        private readonly struct VectorKey
        {
            public VectorKey(float time, Vector3 value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }
            public Vector3 Value { get; }
        }

        private readonly struct QuaternionKey
        {
            public QuaternionKey(float time, Quaternion rotation)
            {
                Time = time;
                Rotation = rotation;
            }

            public float Time { get; }
            public Quaternion Rotation { get; }
        }
    }
}
