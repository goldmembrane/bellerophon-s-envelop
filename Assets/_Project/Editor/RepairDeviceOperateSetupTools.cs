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

namespace Bellerophon.Editor.Validation
{
    internal static class RepairDeviceOperateSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceRelativePath = "item model/repair kit close.fbx";
        private const string ItemFolder = "Assets/_Project/Art/Items/RepairKitClose";
        private const string ModelPath = ItemFolder + "/RepairKitClose.fbx";
        private const string ReviewFolder = ItemFolder + "/Review";
        private const string FinalImagePath = ReviewFolder + "/final.png";
        private const string HandleUpFinalImagePath = ReviewFolder + "/handle_up_final.png";
        private const string NoTransitionFinalImagePath =
            ReviewFolder + "/no_transition_final.png";
        private const string TargetName = "RepairDevice_Operate";
        private const string PlayerName = "Player";
        private const string InstanceName = "RepairKitClose_Placed";
        private const float PlacementDistanceMeters = 1f;
        private const float PositionToleranceMeters = 0.01f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        [MenuItem("Bellerophon/Player/Apply Repair Device Operate Start And Kit")]
        internal static void ApplyRepairDeviceOperateStartAndKit()
        {
            ApplyRepairKitStaticNoTransition();
        }

        [MenuItem("Bellerophon/Player/Inspect Repair Device Operate Start And Kit")]
        internal static void InspectRepairDeviceOperateStartAndKit()
        {
            InspectRepairKitStaticNoTransition();
        }

        [MenuItem("Bellerophon/Player/Capture Repair Device Operate Start And Kit Final")]
        internal static void CaptureRepairDeviceOperateStartAndKitFinal()
        {
            CaptureStaticRepairKit(FinalImagePath);
        }

        [MenuItem("Bellerophon/Player/Apply Repair Kit Static No Transition")]
        internal static void ApplyRepairKitStaticNoTransition()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform target = FindUnique(scene, TargetName);
            Transform existing = FindUnique(scene, InstanceName);
            GameObject modelAsset = RequireModelAsset();

            string outsideBefore = SceneSignature(scene, existing);
            Vector3 preservedPosition = existing.position;
            Vector3 preservedScale = existing.localScale;

            GameObject replacement = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                throw new InvalidOperationException("Static repair kit could not be instantiated.");
            Undo.RegisterCreatedObjectUndo(replacement, "Replace repair kit with static model");
            replacement.name = InstanceName;
            replacement.transform.SetPositionAndRotation(
                preservedPosition,
                Quaternion.identity);
            replacement.transform.localScale = preservedScale;
            foreach (Animator animator in replacement.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(animator);

            Bounds targetBounds = BoundsOf(target);
            Bounds replacementBounds = BoundsOf(replacement.transform);
            replacement.transform.position += Vector3.up *
                (targetBounds.min.y - replacementBounds.min.y);
            EditorUtility.SetDirty(replacement);
            PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);

            Undo.DestroyObjectImmediate(existing.gameObject);
            RequireEqual(
                outsideBefore,
                SceneSignature(scene, replacement.transform),
                "scene objects outside the repair kit");
            RequireStaticRepairKit(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoUnityConsoleErrors();
            Debug.Log("[RepairDeviceOperate] Static repair kit without transition saved.");
        }

        [MenuItem("Bellerophon/Player/Inspect Repair Kit Static No Transition")]
        internal static void InspectRepairKitStaticNoTransition()
        {
            Scene scene = RequireScene();
            RequireStaticRepairKit(scene);
            RequireNoUnityConsoleErrors();
            Debug.Log("[RepairDeviceOperate] Static repair kit inspection passed.");
        }

        [MenuItem("Bellerophon/Player/Capture Repair Kit Static No Transition Final")]
        internal static void CaptureRepairKitStaticNoTransitionFinal()
        {
            CaptureStaticRepairKit(NoTransitionFinalImagePath);
        }

        [MenuItem("Bellerophon/Player/Apply Repair Kit Handle Up Orientation")]
        internal static void ApplyRepairKitHandleUpOrientation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform target = FindUnique(scene, TargetName);
            Transform instance = FindUnique(scene, InstanceName);
            string outsideBefore = SceneSignature(scene, instance);
            Vector3 positionBefore = instance.position;
            Vector3 scaleBefore = instance.localScale;

            Undo.RecordObject(instance, "Point repair kit handle upward");
            instance.rotation = Quaternion.identity;
            Bounds targetBounds = BoundsOf(target);
            Bounds rotatedBounds = BoundsOf(instance);
            instance.position += Vector3.up * (targetBounds.min.y - rotatedBounds.min.y);
            EditorUtility.SetDirty(instance.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);

            if (Mathf.Abs(instance.position.x - positionBefore.x) > 0.000001f ||
                Mathf.Abs(instance.position.z - positionBefore.z) > 0.000001f)
                throw new InvalidOperationException(
                    "Repair kit horizontal position changed unexpectedly.");
            if (Vector3.Distance(instance.localScale, scaleBefore) > 0.000001f)
                throw new InvalidOperationException("Repair kit scale changed unexpectedly.");
            RequireEqual(
                outsideBefore,
                SceneSignature(scene, instance),
                "scene objects outside the repair kit");
            RequireStaticRepairKit(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoUnityConsoleErrors();
            Debug.Log("[RepairDeviceOperate] Static repair kit handle-up orientation saved.");
        }

        [MenuItem("Bellerophon/Player/Inspect Repair Kit Handle Up Orientation")]
        internal static void InspectRepairKitHandleUpOrientation()
        {
            InspectRepairKitStaticNoTransition();
        }

        [MenuItem("Bellerophon/Player/Capture Repair Kit Handle Up Final")]
        internal static void CaptureRepairKitHandleUpFinal()
        {
            CaptureStaticRepairKit(HandleUpFinalImagePath);
        }

        private static void CaptureStaticRepairKit(string imagePath)
        {
            RequireEditMode();
            Scene scene = RequireScene();
            RequireStaticRepairKit(scene);
            RequireNoUnityConsoleErrors();
            if (File.Exists(Absolute(imagePath)))
                throw new InvalidOperationException(
                    "The one final static repair kit capture already exists: " + imagePath);
            EnsureFolder(ReviewFolder);
            Camera camera = RequirePlayerCamera(FindUnique(scene, PlayerName));
            Render(camera, Absolute(imagePath));
            RequireNoUnityConsoleErrors();
            Debug.Log("[RepairDeviceOperate] One final static repair kit capture completed.");
        }

        private static void RequireStaticRepairKit(Scene scene)
        {
            Transform target = FindUnique(scene, TargetName);
            Transform instance = FindUnique(scene, InstanceName);
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                instance.gameObject);
            if (!string.Equals(prefabPath, ModelPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Repair kit is not the approved static close model. Actual=" + prefabPath + ".");
            if (instance.GetComponentsInChildren<Animator>(true).Length != 0)
                throw new InvalidOperationException("Static repair kit still has an Animator.");
            if (instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0)
                throw new InvalidOperationException(
                    "Static repair kit still has skinned animation geometry.");
            if (Quaternion.Angle(instance.rotation, Quaternion.identity) > 0.001f ||
                Vector3.Dot(instance.up, Vector3.up) < 0.999999f)
                throw new InvalidOperationException(
                    "Static repair kit handle axis is not aligned to world up.");

            Bounds targetBounds = BoundsOf(target);
            Bounds kitBounds = BoundsOf(instance);
            float floorError = kitBounds.min.y - targetBounds.min.y;
            if (Mathf.Abs(floorError) > PositionToleranceMeters)
                throw new InvalidOperationException(
                    "Static repair kit is not resting on the floor. Error=" +
                    Num(floorError) + "m.");
            float horizontalDistance = Vector3.Distance(
                Vector3.ProjectOnPlane(instance.position, Vector3.up),
                Vector3.ProjectOnPlane(target.position, Vector3.up));
            if (Mathf.Abs(horizontalDistance - PlacementDistanceMeters) >
                PositionToleranceMeters)
                throw new InvalidOperationException(
                    "Static repair kit placement distance changed. Actual=" +
                    Num(horizontalDistance) + "m.");

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Static repair kit has no renderer.");
            if (renderers.Any(renderer => renderer.sharedMaterials.Any(material => material == null)))
                throw new InvalidOperationException("Static repair kit has a missing material.");
            if (!string.Equals(
                Sha256File(SourceAbsolutePath()),
                Sha256File(Absolute(ModelPath)),
                StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Imported static repair kit differs from the supplied source FBX.");
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene. Actual=" + scene.path + ".");
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "RepairDevice_Operate setup requires Edit Mode.");
        }

        private static Transform FindUnique(Scene scene, string name)
        {
            Transform[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(name + " count=" + matches.Length + ".");
            return matches[0];
        }

        private static Camera RequirePlayerCamera(Transform player)
        {
            Camera[] cameras = player.GetComponentsInChildren<Camera>(true);
            if (cameras.Length != 1)
                throw new InvalidOperationException(
                    "Player must contain exactly one camera. Count=" + cameras.Length + ".");
            return cameras[0];
        }

        private static GameObject RequireModelAsset()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Static repair kit FBX is missing.");
        }

        private static Bounds BoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static string SceneSignature(Scene scene, Transform ignoredKit)
        {
            var lines = new List<string>();
            foreach (Transform item in scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                if (item == ignoredKit || item.IsChildOf(ignoredKit))
                    continue;
                lines.Add(
                    HierarchyPath(item) + "|" + Vec(item.localPosition) + "|" +
                    Quat(item.localRotation) + "|" + Vec(item.localScale) + "|" +
                    item.gameObject.activeSelf);
            }
            lines.Sort(StringComparer.Ordinal);
            return Sha256Text(string.Join("\n", lines));
        }

        private static string HierarchyPath(Transform item)
        {
            var names = new Stack<string>();
            for (Transform current = item; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
        }

        private static void Render(Camera camera, string destination)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static string SourceAbsolutePath()
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot(), SourceRelativePath));
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot(), projectRelativePath));
        }

        private static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
        }

        private static string Sha256File(string path)
        {
            using (var stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256Text(string text)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void RequireNoUnityConsoleErrors()
        {
            Type type = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            var method = type.GetMethod(
                "GetCountsByType",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console count API is unavailable.");
            object[] arguments = { 0, 0, 0 };
            method.Invoke(null, arguments);
            if ((int)arguments[0] != 0)
                throw new InvalidOperationException(
                    "Unity console contains " + arguments[0] + " error(s).");
        }

        private static string Num(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " + Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " +
                Num(value.z) + ", " + Num(value.w) + ")";
        }
    }
}
