using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RebellionCargoRunScene
{
    internal static class RebellionApprovedAppearanceApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Rebellion Enemy Placement";
        private const string ModelName = "Rebellion_Model";
        private const string ApprovedModelPath =
            "Assets/_Project/Art/Enemies/Rebellion/ApprovedAppearance/Rebellion_ApprovedAppearance.glb";
        private const string ApprovedModelSha256 =
            "8DB44E37CDFB7B3C4D838C0C629A877871207C2A93CFF5660121925689680B51";
        private const string CapturePath = "Logs/Rebellion_ApprovedAppearance_Final.png";

        private static readonly string[] SlotNames =
        {
            "Rebellion_00_Static_Review",
            "Rebellion_01_Move",
            "Rebellion_02_Attack_Mode_Transition",
            "Rebellion_03_Forward_Scan",
            "Rebellion_04_Forward_Burst_Fire",
            "Rebellion_05_Hit_Reaction",
            "Rebellion_06_Death"
        };

        [MenuItem("Bellerophon/Enemies/Rebellion/Apply Approved Appearance")]
        public static void ApplyApprovedRebellionAppearance()
        {
            RequireEditMode();
            RequireApprovedHash();
            AssetDatabase.ImportAsset(
                ApprovedModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var approvedAsset = RequireApprovedAsset();
            var scene = OpenApprovedScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the approved Rebellion appearance.");
            }

            var placementRoot = RequirePlacementRoot(scene);
            var protectedRootsBefore = CaptureProtectedRootSignatures(scene);
            var placementRootState = TransformState.Capture(placementRoot.transform);
            var slotStates = new TransformState[SlotNames.Length];

            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = RequireSlot(placementRoot.transform, index);
                slotStates[index] = TransformState.Capture(slot);
                if (slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        slot.name + " must contain exactly one visual child before replacement.");
                }

                var oldModel = slot.GetChild(0);
                var visualState = TransformState.Capture(oldModel);
                UnityEngine.Object.DestroyImmediate(oldModel.gameObject);

                var approvedModel =
                    PrefabUtility.InstantiatePrefab(approvedAsset, scene) as GameObject ??
                    throw new InvalidOperationException(
                        "The approved Rebellion GLB could not be instantiated.");
                approvedModel.name = ModelName;
                approvedModel.transform.SetParent(slot, false);
                visualState.ApplyTo(approvedModel.transform);
                DisableImportedAnimation(approvedModel.transform);
                EditorUtility.SetDirty(approvedModel);
            }

            RequireSameTransform(placementRootState, placementRoot.transform, PlacementRootName);
            for (var index = 0; index < SlotNames.Length; index++)
            {
                RequireSameTransform(
                    slotStates[index],
                    placementRoot.transform.GetChild(index),
                    SlotNames[index]);
            }

            var inspection = Inspect(scene);
            var protectedRootsAfter = CaptureProtectedRootSignatures(scene);
            if (!protectedRootsBefore.SequenceEqual(
                    protectedRootsAfter,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A CargoRunMvp scene root outside the approved Rebellion placement changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the approved Rebellion appearance was applied.");
            }

            AssetDatabase.SaveAssets();
            RequireApprovedHash();
            Debug.Log(
                "ApprovedRebellionAppearanceApplied Result=PASS, Slots=" +
                inspection.SlotCount.ToString(CultureInfo.InvariantCulture) +
                ", ModelSha256=" + ApprovedModelSha256 +
                ", RendererCountPerSlot=" +
                inspection.RendererCountPerSlot.ToString(CultureInfo.InvariantCulture) +
                ", MaterialNames=" + string.Join("|", inspection.MaterialNames) +
                ", PlacementRootTransformPreserved=True, SlotTransformsPreserved=True, " +
                "OtherSceneRootsUnchanged=True, PlayerUnchanged=True, AnimationsChanged=False, " +
                "SourceRebellionGlbOverwritten=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Approved Appearance")]
        public static void InspectApprovedRebellionAppearance()
        {
            RequireEditMode();
            RequireApprovedHash();
            var scene = OpenApprovedScene();
            var wasDirty = scene.isDirty;
            var inspection = Inspect(scene);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Rebellion appearance inspection changed the scene dirty state.");
            }

            Debug.Log(
                "ApprovedRebellionAppearanceInspected Result=PASS, Slots=" +
                inspection.SlotCount.ToString(CultureInfo.InvariantCulture) +
                ", ModelSha256=" + ApprovedModelSha256 +
                ", RendererCountPerSlot=" +
                inspection.RendererCountPerSlot.ToString(CultureInfo.InvariantCulture) +
                ", BoundsSizePerSlot=" + Format(inspection.BoundsSizePerSlot) +
                ", MaterialNames=" + string.Join("|", inspection.MaterialNames) +
                ", ShaderNames=" + string.Join("|", inspection.ShaderNames) +
                ", DirectApprovedGlbInstances=7, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Capture Approved Appearance Final")]
        public static void CaptureApprovedRebellionAppearanceFinal()
        {
            RequireEditMode();
            var scene = OpenApprovedScene();
            var wasDirty = scene.isDirty;
            Inspect(scene);
            var player = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "Player") ??
                throw new InvalidOperationException("Player root is missing.");
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("Player camera is missing.");
            Capture(camera, Absolute(CapturePath), 1920, 1080);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Rebellion appearance capture changed the scene dirty state.");
            }

            Debug.Log(
                "ApprovedRebellionAppearanceCaptured Result=PASS, Image=" +
                CapturePath + ", SceneChanged=False, CaptureCount=1.");
        }

        private static Inspection Inspect(Scene scene)
        {
            var approvedAsset = RequireApprovedAsset();
            var placementRoot = RequirePlacementRoot(scene);
            if (placementRoot.transform.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "The approved Rebellion placement must contain seven slots.");
            }

            var rendererCount = -1;
            var boundsSize = Vector3.zero;
            var materialNames = new SortedSet<string>(StringComparer.Ordinal);
            var shaderNames = new SortedSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = RequireSlot(placementRoot.transform, index);
                if (slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        slot.name + " must contain exactly one approved visual child.");
                }

                var model = slot.GetChild(0);
                var source =
                    PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                if (model.name != ModelName ||
                    source == null ||
                    AssetDatabase.GetAssetPath(source) != ApprovedModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name + " is not a direct instance of the approved Rebellion GLB.");
                }

                var renderers = model.GetComponentsInChildren<Renderer>(true)
                    .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                    .ToArray();
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        slot.name + " has no visible approved Rebellion renderer.");
                }

                if (rendererCount < 0)
                {
                    rendererCount = renderers.Length;
                    boundsSize = BoundsOf(renderers).size;
                }
                else if (rendererCount != renderers.Length)
                {
                    throw new InvalidOperationException(
                        "Approved Rebellion renderer counts differ between slots.");
                }

                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || material.shader == null ||
                            material.shader.name.Contains(
                                "InternalErrorShader",
                                StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                slot.name + " contains a missing or error material.");
                        }

                        materialNames.Add(material.name);
                        shaderNames.Add(material.shader.name);
                    }
                }

                if (model.GetComponentsInChildren<Animator>(true)
                        .Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true)
                        .Any(item => item.enabled))
                {
                    throw new InvalidOperationException(
                        slot.name + " must remain a static animation placeholder.");
                }
            }

            if (approvedAsset == null)
            {
                throw new InvalidOperationException("Approved Rebellion GLB is missing.");
            }

            return new Inspection(
                SlotNames.Length,
                rendererCount,
                boundsSize,
                materialNames.ToArray(),
                shaderNames.ToArray());
        }

        private static Scene OpenApprovedScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path == ScenePath)
            {
                return activeScene;
            }

            if (activeScene.IsValid() && activeScene.isDirty)
            {
                throw new InvalidOperationException(
                    "The current Unity scene has unsaved changes. Save or discard them before opening CargoRunMvp.");
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static GameObject RequirePlacementRoot(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(item => item.name == PlacementRootName) ??
                   throw new InvalidOperationException(
                       "The approved Rebellion placement root is missing or duplicated.");
        }

        private static Transform RequireSlot(Transform placementRoot, int index)
        {
            if (index < 0 || index >= SlotNames.Length ||
                index >= placementRoot.childCount)
            {
                throw new InvalidOperationException(
                    "The approved Rebellion slot index is invalid.");
            }

            var slot = placementRoot.GetChild(index);
            if (slot.name != SlotNames[index])
            {
                throw new InvalidOperationException(
                    "The approved Rebellion slot order or name changed at index " + index + ".");
            }

            return slot;
        }

        private static GameObject RequireApprovedAsset()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedModelPath) ??
                   throw new InvalidOperationException(
                       "Unity did not import the approved Rebellion GLB as a GameObject asset.");
        }

        private static void DisableImportedAnimation(Transform model)
        {
            foreach (var animator in model.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                EditorUtility.SetDirty(animator);
            }

            foreach (var animation in model.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static string[] CaptureProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(HierarchySignature)
                .ToArray();
        }

        private static string HierarchySignature(GameObject root)
        {
            var builder = new StringBuilder();
            foreach (var current in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(current.GetSiblingIndex())
                    .Append(':').Append(current.name)
                    .Append(':').Append(current.gameObject.activeSelf)
                    .Append(':').Append(Format(current.localPosition))
                    .Append(':').Append(Format(current.localRotation))
                    .Append(':').Append(Format(current.localScale))
                    .Append(':').Append(string.Join(
                        ",",
                        current.GetComponents<Component>()
                            .Where(item => item != null)
                            .Select(item => item.GetType().FullName)
                            .OrderBy(item => item, StringComparer.Ordinal)))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static void RequireApprovedHash()
        {
            var absolutePath = Absolute(ApprovedModelPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "The approved Rebellion GLB is missing.",
                    absolutePath);
            }

            var actual = Sha256(absolutePath);
            if (!string.Equals(
                    actual,
                    ApprovedModelSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The Unity approved Rebellion GLB hash differs from the user-approved sample.");
            }
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Approved Rebellion appearance work requires Unity Edit Mode.");
            }
        }

        private static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void Capture(Camera camera, string path, int width, int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Invalid capture path."));
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
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException("Unity project root is unavailable."),
                    projectRelativePath));
        }

        private static string Format(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:R},{1:R},{2:R})",
                value.x,
                value.y,
                value.z);
        }

        private static string Format(Quaternion value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:R},{1:R},{2:R},{3:R})",
                value.x,
                value.y,
                value.z,
                value.w);
        }

        private static void RequireSameTransform(
            TransformState expected,
            Transform actual,
            string label)
        {
            if (!expected.Matches(actual))
            {
                throw new InvalidOperationException(
                    label + " transform changed while applying the approved appearance.");
            }
        }

        private readonly struct Inspection
        {
            public Inspection(
                int slotCount,
                int rendererCountPerSlot,
                Vector3 boundsSizePerSlot,
                string[] materialNames,
                string[] shaderNames)
            {
                SlotCount = slotCount;
                RendererCountPerSlot = rendererCountPerSlot;
                BoundsSizePerSlot = boundsSizePerSlot;
                MaterialNames = materialNames;
                ShaderNames = shaderNames;
            }

            public int SlotCount { get; }
            public int RendererCountPerSlot { get; }
            public Vector3 BoundsSizePerSlot { get; }
            public string[] MaterialNames { get; }
            public string[] ShaderNames { get; }
        }

        private readonly struct TransformState
        {
            private TransformState(
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            private Vector3 LocalPosition { get; }
            private Quaternion LocalRotation { get; }
            private Vector3 LocalScale { get; }

            public static TransformState Capture(Transform target)
            {
                return new TransformState(
                    target.localPosition,
                    target.localRotation,
                    target.localScale);
            }

            public void ApplyTo(Transform target)
            {
                target.SetLocalPositionAndRotation(LocalPosition, LocalRotation);
                target.localScale = LocalScale;
            }

            public bool Matches(Transform target)
            {
                return LocalPosition == target.localPosition &&
                       LocalRotation == target.localRotation &&
                       LocalScale == target.localScale;
            }
        }
    }
}
