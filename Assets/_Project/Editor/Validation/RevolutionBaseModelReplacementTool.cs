using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RevolutionCargoRunScene
{
    internal static class RevolutionBaseModelReplacementTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath =
            "D:/Bellerophon2/Bellerophon/enemies model/révolution.fbx";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Revolution/Models/Revolution.fbx";
        private const string PlacementRootName =
            "Approved Revolution Enemy Placement";
        private const string ModelName = "Revolution_Model";
        private const string ExpectedSourceSha256 =
            "645226EEFA4AEBE8CF43168B8A16E0595506E77F032ADBABFB394DB67FFA578E";
        private const int ExpectedAuthoredVertexCount = 2307;
        private const int ExpectedTriangleCount = 3945;
        private const int ExpectedLoopCount = 11835;
        private const int ExpectedBoneCount = 24;
        private const float TargetHeight = 2f;
        private const float Tolerance = 0.03f;

        private static readonly string[] SlotNames =
        {
            "Revolution_01",
            "Revolution_02",
            "Revolution_03",
            "Revolution_04",
            "Revolution_05",
            "Revolution_06",
            "Revolution_07",
            "Revolution_08"
        };

        [MenuItem("Bellerophon/Enemies/Revolution/Apply Base Model Replacement")]
        public static void ApplyRevolutionBaseModelReplacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before replacing Revolution models.");
            }

            var sourceHashBefore = Sha256(SourcePath);
            RequireSameHash(ExpectedSourceSha256, sourceHashBefore);
            CopyAndImportModel();
            RequireSameHash(
                sourceHashBefore,
                Sha256(Absolute(ModelPath)));
            var modelAsset = RequireModelAsset();
            RequireImportedGeometry(modelAsset.transform);

            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Revolution placement root is missing.");
            if (root.transform.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "Revolution placement must contain exactly eight slots.");
            }

            var protectedBefore = ProtectedRootSignatures(scene);
            var rootPositionBefore = root.transform.position;
            var rootRotationBefore = root.transform.rotation;
            var rootScaleBefore = root.transform.localScale;
            var player = RequirePlayer();
            var playerPositionBefore = player.position;
            var playerRotationBefore = player.rotation;
            var playerScaleBefore = player.localScale;
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException(
                             "The Player camera is missing.");
            var cameraPositionBefore = camera.transform.position;
            var cameraRotationBefore = camera.transform.rotation;
            var cameraScaleBefore = camera.transform.localScale;

            var slots = new Transform[SlotNames.Length];
            var oldModels = new Transform[SlotNames.Length];
            var slotPositions = new Vector3[SlotNames.Length];
            var slotRotations = new Quaternion[SlotNames.Length];
            var slotScales = new Vector3[SlotNames.Length];
            var replacements = new GameObject[SlotNames.Length];
            var oldModelsDestroyed = false;

            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = root.transform.GetChild(index);
                if (slot.name != SlotNames[index] ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Revolution slot contract differs before replacement at index " +
                        index + ".");
                }

                slots[index] = slot;
                oldModels[index] = slot.GetChild(0);
                slotPositions[index] = slot.localPosition;
                slotRotations[index] = slot.localRotation;
                slotScales[index] = slot.localScale;
            }

            try
            {
                for (var index = 0; index < SlotNames.Length; index++)
                {
                    var replacement =
                        PrefabUtility.InstantiatePrefab(
                            modelAsset,
                            scene) as GameObject ??
                        throw new InvalidOperationException(
                            "The supplied Revolution base FBX could not be instantiated.");
                    replacements[index] = replacement;
                    replacement.name = ModelName;
                    replacement.transform.SetParent(slots[index], false);
                    replacement.transform.SetLocalPositionAndRotation(
                        Vector3.zero,
                        Quaternion.identity);
                    replacement.transform.localScale = Vector3.one;
                    ConfigureStaticModel(replacement.transform);
                    ScaleAndGround(
                        replacement.transform,
                        root.transform.position.y);
                    RequireImportedGeometry(replacement.transform);
                    EditorUtility.SetDirty(replacement);
                }

                foreach (var oldModel in oldModels)
                {
                    UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
                }

                oldModelsDestroyed = true;

                for (var index = 0; index < SlotNames.Length; index++)
                {
                    var slot = slots[index];
                    if (slot.childCount != 1 ||
                        slot.GetChild(0) != replacements[index].transform ||
                        slot.localPosition != slotPositions[index] ||
                        slot.localRotation != slotRotations[index] ||
                        slot.localScale != slotScales[index])
                    {
                        throw new InvalidOperationException(
                            "Revolution slot changed outside its model child at index " +
                            index + ".");
                    }

                    RequireDirectInstance(slot.GetChild(0));
                    RequireHeightAndGround(
                        slot.GetChild(0),
                        root.transform.position.y);
                }

                if (root.transform.position != rootPositionBefore ||
                    root.transform.rotation != rootRotationBefore ||
                    root.transform.localScale != rootScaleBefore ||
                    player.position != playerPositionBefore ||
                    player.rotation != playerRotationBefore ||
                    player.localScale != playerScaleBefore ||
                    camera.transform.position != cameraPositionBefore ||
                    camera.transform.rotation != cameraRotationBefore ||
                    camera.transform.localScale != cameraScaleBefore)
                {
                    throw new InvalidOperationException(
                        "Revolution root, Player, or camera transform changed during model replacement.");
                }

                var protectedAfter = ProtectedRootSignatures(scene);
                if (!protectedBefore.SequenceEqual(
                        protectedAfter,
                        StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A scene root outside Revolution changed during model replacement.");
                }

                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after Revolution base model replacement.");
                }

                AssetDatabase.SaveAssets();
                RequireSameHash(
                    sourceHashBefore,
                    Sha256(SourcePath));
                RequireSameHash(
                    sourceHashBefore,
                    Sha256(Absolute(ModelPath)));
            }
            catch
            {
                if (!oldModelsDestroyed)
                {
                    foreach (var replacement in replacements)
                    {
                        if (replacement != null)
                        {
                            UnityEngine.Object.DestroyImmediate(replacement);
                        }
                    }
                }
                else
                {
                    EditorSceneManager.OpenScene(
                        ScenePath,
                        OpenSceneMode.Single);
                }

                throw;
            }

            Debug.Log(
                "RevolutionBaseModelReplacementApplied Result=PASS" +
                ", Slots=8" +
                ", SourceSha256=" + sourceHashBefore +
                ", AuthoredVertices=" + ExpectedAuthoredVertexCount +
                ", Triangles=" + ExpectedTriangleCount +
                ", Loops=" + ExpectedLoopCount +
                ", Bones=" + ExpectedBoneCount +
                ", TargetHeight=2" +
                ", AnimationApplied=False" +
                ", SlotTransformsUnchanged=True" +
                ", PlayerTransformUnchanged=True" +
                ", CameraTransformUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Revolution/Inspect Base Model Replacement")]
        public static void InspectRevolutionBaseModelReplacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var sourceHash = Sha256(SourcePath);
            RequireSameHash(ExpectedSourceSha256, sourceHash);
            RequireSameHash(
                sourceHash,
                Sha256(Absolute(ModelPath)));

            var modelAsset = RequireModelAsset();
            var imported = RequireImportedGeometry(modelAsset.transform);
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Revolution placement root is missing.");
            if (root.transform.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "Revolution placement must contain exactly eight slots.");
            }

            var rendererCount = -1;
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = root.transform.GetChild(index);
                if (slot.name != SlotNames[index] ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Revolution slot contract differs at index " +
                        index + ".");
                }

                var model = slot.GetChild(0);
                RequireDirectInstance(model);
                RequireImportedGeometry(model);
                RequireHeightAndGround(
                    model,
                    root.transform.position.y);
                var renderers = model.GetComponentsInChildren<Renderer>(false)
                    .Where(item =>
                        item.enabled &&
                        item.gameObject.activeInHierarchy)
                    .ToArray();
                if (rendererCount < 0)
                {
                    rendererCount = renderers.Length;
                }
                else if (rendererCount != renderers.Length)
                {
                    throw new InvalidOperationException(
                        "Revolution renderer count differs between slots.");
                }
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Revolution base model inspection changed the scene dirty state.");
            }

            Debug.Log(
                "RevolutionBaseModelReplacementInspected Result=PASS" +
                ", Slots=8" +
                ", SourceSha256=" + sourceHash +
                ", AuthoredVertices=" + ExpectedAuthoredVertexCount +
                ", ImportedRenderVertices=" + imported.sharedMesh.vertexCount +
                ", Triangles=" + ExpectedTriangleCount +
                ", Loops=" + ExpectedLoopCount +
                ", Bones=" + imported.bones.Length +
                ", RenderersPerSlot=" + rendererCount +
                ", DirectFbxInstances=8" +
                ", TargetHeight=2" +
                ", AnimationApplied=False" +
                ", SceneChanged=False.");
        }

        private static void CopyAndImportModel()
        {
            var destination = Absolute(ModelPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Revolution model folder."));
            File.Copy(SourcePath, destination, true);
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer =
                AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Revolution FBX ModelImporter is missing.");
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.optimizeGameObjects = false;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureStaticModel(Transform model)
        {
            foreach (var animator in
                     model.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                EditorUtility.SetDirty(animator);
            }

            foreach (var animation in
                     model.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static void ScaleAndGround(
            Transform model,
            float groundY)
        {
            var bounds = BoundsOf(
                model,
                new Bounds(model.position, Vector3.one));
            if (bounds.size.y <= 0.00001f)
            {
                throw new InvalidOperationException(
                    "Revolution has no usable visible height.");
            }

            var scale = TargetHeight / bounds.size.y;
            if (float.IsNaN(scale) ||
                float.IsInfinity(scale) ||
                scale <= 0f ||
                scale > 1000f)
            {
                throw new InvalidOperationException(
                    "Revolution target-height scale is invalid.");
            }

            model.localScale = Vector3.one * scale;
            bounds = BoundsOf(
                model,
                new Bounds(model.position, Vector3.one));
            model.position +=
                Vector3.up * (groundY - bounds.min.y);
        }

        private static void RequireHeightAndGround(
            Transform model,
            float groundY)
        {
            var bounds = BoundsOf(
                model,
                new Bounds(model.position, Vector3.one));
            if (Mathf.Abs(bounds.size.y - TargetHeight) > Tolerance ||
                Mathf.Abs(bounds.min.y - groundY) > Tolerance)
            {
                throw new InvalidOperationException(
                    model.parent.name +
                    " height or ground alignment differs. Height=" +
                    Num(bounds.size.y) +
                    ", MinY=" + Num(bounds.min.y) + ".");
            }
        }

        private static SkinnedMeshRenderer RequireImportedGeometry(
            Transform root)
        {
            var renderer =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Revolution FBX must contain exactly one skinned renderer.");
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           "Revolution skinned mesh is missing.");
            var triangleCount = Enumerable.Range(0, mesh.subMeshCount)
                .Sum(index =>
                    checked((int)mesh.GetIndexCount(index)) / 3);
            var projectMaterials = renderer.sharedMaterials
                .Where(material => material != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(path =>
                    path.StartsWith(
                        "Assets/",
                        StringComparison.Ordinal))
                .ToArray();
            if (triangleCount != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount ||
                projectMaterials.Length != 0)
            {
                throw new InvalidOperationException(
                    "Revolution imported geometry contract differs. Triangles=" +
                    triangleCount +
                    ", Bones=" + renderer.bones.Length +
                    ", ProjectMaterials=" +
                    string.Join("|", projectMaterials) + ".");
            }

            return renderer;
        }

        private static void RequireDirectInstance(Transform model)
        {
            var source =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    model.gameObject);
            if (model.name != ModelName ||
                source == null ||
                AssetDatabase.GetAssetPath(source) != ModelPath)
            {
                throw new InvalidOperationException(
                    model.parent.name +
                    " is not a direct instance of the supplied Revolution base FBX.");
            }

            if (model.GetComponentsInChildren<Animator>(true)
                    .Any(item => item.enabled) ||
                model.GetComponentsInChildren<Animation>(true)
                    .Any(item => item.enabled))
            {
                throw new InvalidOperationException(
                    model.parent.name +
                    " must remain a static model slot.");
            }
        }

        private static Bounds BoundsOf(
            Transform root,
            Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(false)
                .Where(item =>
                    item.enabled &&
                    item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                return fallback;
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != PlacementRootName)
                .Select(root =>
                    GlobalObjectId.GetGlobalObjectIdSlow(root) + "|" +
                    root.name + "|" +
                    root.activeSelf + "|" +
                    Vec(root.transform.position) + "|" +
                    Quat(root.transform.rotation) + "|" +
                    Vec(root.transform.localScale) + "|" +
                    root.transform.childCount)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the current active scene. ActiveScene=" +
                    scene.path);
            }

            return scene;
        }

        private static GameObject RequireModelAsset()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                   throw new InvalidOperationException(
                       "Unity did not import the Revolution base FBX as a GameObject asset.");
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find("Player");
            if (player != null)
            {
                return player.transform;
            }

            var controller =
                UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            return controller != null
                ? controller.transform
                : throw new InvalidOperationException(
                    "Player is missing.");
        }

        private static void RequireSource()
        {
            if (!File.Exists(SourcePath))
            {
                throw new FileNotFoundException(
                    "The supplied Revolution base FBX is missing.",
                    SourcePath);
            }
        }

        private static void RequireSameHash(
            string first,
            string second)
        {
            if (!string.Equals(
                    first,
                    second,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The supplied and imported Revolution base FBX hashes differ.");
            }
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    relative));
        }

        private static string Num(float value)
        {
            return value.ToString(
                "0.######",
                CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" +
                   Num(value.x) + ", " +
                   Num(value.y) + ", " +
                   Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" +
                   Num(value.x) + ", " +
                   Num(value.y) + ", " +
                   Num(value.z) + ", " +
                   Num(value.w) + ")";
        }
    }
}
