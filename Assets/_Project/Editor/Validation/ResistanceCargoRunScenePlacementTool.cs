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

namespace Bellerophon.Editor.ResistanceCargoRunScene
{
    internal static class ResistanceCargoRunScenePlacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath =
            "D:/Bellerophon2/Bellerophon/enemies model/résistance.fbx";
        private const string ArtRoot = "Assets/_Project/Art/Enemies/Resistance";
        private const string ModelFolder = ArtRoot + "/Models";
        private const string ModelPath = ModelFolder + "/Resistance.fbx";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string RebellionRootName = "Approved Rebellion Enemy Placement";
        private const string PlacementRootName = "Approved Resistance Enemy Placement";
        private const string PlayerName = "Player";
        private const string ModelName = "Resistance_Model";
        private const string InspectionPath =
            "docs/validation/resistance_placement_2026-07-26/Resistance_Placement_Inspection.txt";
        private const string CapturePath =
            "docs/validation/resistance_placement_2026-07-26/Resistance_Placement_VisualReview.png";
        private const int SlotCount = 14;
        private const int FocusSlotIndex = 6;
        private const float TargetHeight = 1.5f;
        private const float FacingYaw = 180f;
        private const float Tolerance = 0.03f;
        private const float MinimumCameraDistance = 2.5f;
        private const float CameraMargin = 0.6f;
        private const float FramingAngleMargin = 4f;

        [MenuItem("Bellerophon/Enemies/Resistance/Apply Placement")]
        public static void ApplyPlacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying Resistance placement.");
            }

            var sourceHashBefore = Sha256(SourcePath);
            CopyAndImportModel();
            var importedHashBefore = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, importedHashBefore);
            var modelAsset = RequireModelAsset();
            var protectedBefore = ProtectedRootSignatures(scene);
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var rebellion = RequireRoot(RebellionRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = RebellionSlotSpacing(rebellion);

            var oldRoot = GameObject.Find(PlacementRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(
                new Vector3(
                    rebellion.position.x,
                    rebellion.position.y,
                    rebellion.position.z - zSpacing),
                Quaternion.identity);

            for (var i = 0; i < SlotCount; i++)
            {
                var slot = new GameObject(SlotName(i));
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3(i * xSpacing, 0f, 0f);
                slot.transform.localRotation = Quaternion.Euler(0f, FacingYaw, 0f);

                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                            throw new InvalidOperationException(
                                "The supplied Resistance FBX could not be instantiated.");
                model.name = ModelName;
                model.transform.SetParent(slot.transform, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = Vector3.one;
                ConfigureStaticModel(model.transform);
                ScaleAndGround(model.transform, root.transform.position.y);
                EditorUtility.SetDirty(slot);
                EditorUtility.SetDirty(model);
            }

            ConfigurePlayer(root.transform);
            var metrics = InspectState(scene, root.transform);
            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Resistance and Player changed during placement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Resistance placement.");
            }

            AssetDatabase.SaveAssets();
            var sourceHashAfter = Sha256(SourcePath);
            var importedHashAfter = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, sourceHashAfter);
            RequireSameHash(importedHashBefore, importedHashAfter);
            Debug.Log(
                "ResistancePlacementApplied Result=PASS, Slots=" + SlotCount +
                ", Position=" + Vec(metrics.Resistance) +
                ", RebellionPosition=" + Vec(metrics.Rebellion) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", RebellionXSpacing=" + Num(metrics.XSpacing) +
                ", FocusSlot=" + SlotName(FocusSlotIndex) +
                ", FocusBounds=" + Vec(metrics.FocusBounds.size) +
                ", Player=" + Vec(metrics.Player) +
                ", Camera=" + Vec(metrics.Camera) +
                ", SourceSha256=" + sourceHashAfter +
                ", DirectFbxInstances=" + SlotCount +
                ", AnimationApplied=False, OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Inspect Placement")]
        public static void InspectPlacement()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Resistance placement root is missing.");
            var metrics = InspectState(scene, root.transform);
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException(
                    "Unity reports script compilation errors.");
            }

            var sourceHash = Sha256(SourcePath);
            var importedHash = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHash, importedHash);
            var report = new StringBuilder()
                .AppendLine("Resistance Placement Inspection")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("PlacementRoot=" + PlacementRootName)
                .AppendLine("Slots=" + SlotCount)
                .AppendLine("SlotNames=Resistance_01..Resistance_14")
                .AppendLine("ResistancePosition=" + Vec(metrics.Resistance))
                .AppendLine("RebellionPosition=" + Vec(metrics.Rebellion))
                .AppendLine("LongaTergoZSpacing=" + Num(metrics.ZSpacing))
                .AppendLine("RebellionXSpacing=" + Num(metrics.XSpacing))
                .AppendLine("FocusSlot=" + SlotName(FocusSlotIndex))
                .AppendLine("FocusBoundsCenter=" + Vec(metrics.FocusBounds.center))
                .AppendLine("FocusBoundsSize=" + Vec(metrics.FocusBounds.size))
                .AppendLine("PlayerPosition=" + Vec(metrics.Player))
                .AppendLine("PlayerForward=" + Vec(metrics.PlayerForward))
                .AppendLine("CameraPosition=" + Vec(metrics.Camera))
                .AppendLine("CameraForward=" + Vec(metrics.CameraForward))
                .AppendLine("SourceSha256=" + sourceHash)
                .AppendLine("ImportedSha256=" + importedHash)
                .AppendLine("DirectFbxInstances=" + SlotCount)
                .AppendLine("TargetHeight=" + Num(TargetHeight))
                .AppendLine("Grounded=True")
                .AppendLine("AnimationApplied=False")
                .AppendLine("ScriptCompilationFailed=False")
                .AppendLine("SceneChanged=False")
                .ToString();
            WriteText(Absolute(InspectionPath), report);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Resistance placement inspection changed the scene dirty state.");
            }

            Debug.Log(
                "ResistancePlacementInspected Result=PASS, Report=" +
                InspectionPath + ", Slots=" + SlotCount +
                ", FocusSlot=" + SlotName(FocusSlotIndex) +
                ", SourceSha256=" + sourceHash + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Capture Placement Review")]
        public static void CapturePlacementReview()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Resistance placement root is missing.");
            var metrics = InspectState(scene, root.transform);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(CapturePath), 1920, 1080);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Resistance placement capture changed the scene dirty state.");
            }

            Debug.Log(
                "ResistancePlacementReviewCaptured Result=PASS, Image=" +
                CapturePath + ", FocusSlot=" + SlotName(FocusSlotIndex) +
                ", FocusBounds=" + Vec(metrics.FocusBounds.size) +
                ", FullBodyVisible=True, SceneChanged=False.");
        }

        private static Metrics InspectState(Scene scene, Transform root)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be active.");
            }

            RequireSource();
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var rebellion = RequireRoot(RebellionRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = RebellionSlotSpacing(rebellion);
            var expected = new Vector3(
                rebellion.position.x,
                rebellion.position.y,
                rebellion.position.z - zSpacing);
            if (Vector3.Distance(root.position, expected) > Tolerance ||
                root.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "Resistance root position or fourteen-slot contract changed.");
            }

            var rendererCount = -1;
            for (var i = 0; i < SlotCount; i++)
            {
                var slot = root.GetChild(i);
                if (slot.name != SlotName(i) ||
                    Vector3.Distance(
                        slot.localPosition,
                        new Vector3(i * xSpacing, 0f, 0f)) > Tolerance ||
                    Quaternion.Angle(
                        slot.localRotation,
                        Quaternion.Euler(0f, FacingYaw, 0f)) > 0.1f ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Resistance slot contract changed at index " + i + ".");
                }

                var model = slot.GetChild(0);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                if (model.name != ModelName || source == null ||
                    AssetDatabase.GetAssetPath(source) != ModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " is not a direct instance of the supplied Resistance FBX.");
                }

                var renderers = model.GetComponentsInChildren<Renderer>(false)
                    .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                    .ToArray();
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        slot.name + " has no visible Resistance renderer.");
                }

                if (rendererCount < 0)
                {
                    rendererCount = renderers.Length;
                }
                else if (rendererCount != renderers.Length)
                {
                    throw new InvalidOperationException(
                        "Resistance renderer count differs between slots.");
                }

                var modelBounds = BoundsOf(
                    model,
                    new Bounds(model.position, Vector3.one));
                if (Mathf.Abs(modelBounds.size.y - TargetHeight) > Tolerance ||
                    Mathf.Abs(modelBounds.min.y - root.position.y) > Tolerance)
                {
                    throw new InvalidOperationException(
                        slot.name + " height or ground alignment changed.");
                }

                if (model.GetComponentsInChildren<Animator>(true)
                        .Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true)
                        .Any(item => item.enabled))
                {
                    throw new InvalidOperationException(
                        "Resistance placement must remain static.");
                }
            }

            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var focus = root.GetChild(FocusSlotIndex);
            var focusBounds = BoundsOf(
                focus,
                new Bounds(focus.position, Vector3.one));
            InspectPlayer(camera, focus, focusBounds);
            return new Metrics
            {
                Rebellion = rebellion.position,
                Resistance = root.position,
                Player = player.position,
                PlayerForward = player.forward,
                Camera = camera.transform.position,
                CameraForward = camera.transform.forward,
                ZSpacing = zSpacing,
                XSpacing = xSpacing,
                FocusBounds = focusBounds
            };
        }

        private static void ConfigurePlayer(Transform root)
        {
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var focus = root.GetChild(FocusSlotIndex);
            var bounds = BoundsOf(
                focus,
                new Bounds(focus.position, Vector3.one));
            var front = focus.forward;
            front.y = 0f;
            front.Normalize();
            var cameraOffsetLocal = player.InverseTransformPoint(camera.transform.position);
            var rotation = Quaternion.LookRotation(-front, Vector3.up);
            var futureCameraHeight =
                player.position.y + (rotation * cameraOffsetLocal).y;
            var distance = CameraDistance(bounds, camera, futureCameraHeight);
            var desiredCamera = bounds.center + front * distance;
            desiredCamera.y = futureCameraHeight;
            var desiredPlayer = desiredCamera - rotation * cameraOffsetLocal;
            desiredPlayer.y = player.position.y;
            player.SetPositionAndRotation(desiredPlayer, rotation);
            EditorUtility.SetDirty(player);
        }

        private static void InspectPlayer(
            Camera camera,
            Transform focus,
            Bounds bounds)
        {
            var fromFocus = camera.transform.position - bounds.center;
            fromFocus.y = 0f;
            var front = focus.forward;
            front.y = 0f;
            var toFocus = bounds.center - camera.transform.position;
            toFocus.y = 0f;
            var cameraForward = camera.transform.forward;
            cameraForward.y = 0f;
            if (fromFocus.sqrMagnitude < 0.001f ||
                front.sqrMagnitude < 0.001f ||
                Vector3.Dot(fromFocus.normalized, front.normalized) < 0.98f ||
                toFocus.sqrMagnitude < 0.001f ||
                cameraForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(toFocus.normalized, cameraForward.normalized) < 0.98f)
            {
                throw new InvalidOperationException(
                    "Player camera is not centered in front of Resistance_07.");
            }

            foreach (var corner in Corners(bounds))
            {
                var view = camera.WorldToViewportPoint(corner);
                if (view.z <= 0f || view.x < 0.04f || view.x > 0.96f ||
                    view.y < 0.04f || view.y > 0.96f)
                {
                    throw new InvalidOperationException(
                        "Player camera does not contain the full Resistance_07 model.");
                }
            }
        }

        private static float CameraDistance(
            Bounds bounds,
            Camera camera,
            float cameraHeight)
        {
            var verticalHalf = Mathf.Max(
                5f,
                camera.fieldOfView * 0.5f - FramingAngleMargin) *
                Mathf.Deg2Rad;
            var aspect = camera.aspect > 0.1f ? camera.aspect : 16f / 9f;
            var horizontalHalf = Mathf.Max(
                5f,
                Mathf.Atan(Mathf.Tan(verticalHalf) * aspect) *
                Mathf.Rad2Deg - FramingAngleMargin) *
                Mathf.Deg2Rad;
            var verticalExtent = Mathf.Max(
                Mathf.Abs(cameraHeight - bounds.min.y),
                Mathf.Abs(bounds.max.y - cameraHeight));
            return Mathf.Max(
                MinimumCameraDistance,
                verticalExtent /
                Mathf.Max(0.01f, Mathf.Tan(verticalHalf)) + CameraMargin,
                bounds.extents.x /
                Mathf.Max(0.01f, Mathf.Tan(horizontalHalf)) + CameraMargin);
        }

        private static void CopyAndImportModel()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(ModelFolder);
            var destination = Absolute(ModelPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid Resistance model folder."));
            File.Copy(SourcePath, destination, true);
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureStaticModel(Transform model)
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

        private static void ScaleAndGround(Transform model, float groundY)
        {
            var bounds = BoundsOf(model, new Bounds(model.position, Vector3.one));
            if (bounds.size.y <= 0.00001f)
            {
                throw new InvalidOperationException(
                    "Resistance has no usable visible height.");
            }

            var scale = TargetHeight / bounds.size.y;
            if (float.IsNaN(scale) || float.IsInfinity(scale) ||
                scale <= 0f || scale > 1000f)
            {
                throw new InvalidOperationException(
                    "Resistance target-height scale is invalid.");
            }

            model.localScale = Vector3.one * scale;
            bounds = BoundsOf(model, new Bounds(model.position, Vector3.one));
            model.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static float LongaTergoSpacing(Transform longa, Transform tergo)
        {
            var value = Mathf.Abs(longa.position.z - tergo.position.z);
            if (value <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Longa/Tergo Z spacing is unusable.");
            }

            return value;
        }

        private static float RebellionSlotSpacing(Transform root)
        {
            if (root.childCount < 2)
            {
                throw new InvalidOperationException(
                    "Rebellion requires at least two slots for X spacing.");
            }

            var value = Mathf.Abs(
                root.GetChild(1).position.x - root.GetChild(0).position.x);
            if (value <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Rebellion X spacing is unusable.");
            }

            return value;
        }

        private static Bounds BoundsOf(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(false)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                return fallback;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    for (var z = 0; z < 2; z++)
                    {
                        yield return new Vector3(
                            x == 0 ? bounds.min.x : bounds.max.x,
                            y == 0 ? bounds.min.y : bounds.max.y,
                            z == 0 ? bounds.min.z : bounds.max.z);
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
                throw new InvalidOperationException("Invalid capture folder."));
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target =
                new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image =
                new Texture2D(width, height, TextureFormat.RGB24, false);
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

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root =>
                    root.name != PlacementRootName &&
                    root.name != PlayerName)
                .Select(root =>
                    GlobalObjectId.GetGlobalObjectIdSlow(root) + "|" +
                    root.name + "|" + root.activeSelf + "|" +
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
            if (!scene.IsValid() || scene.path != ScenePath)
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
                       "Unity did not import the Resistance FBX as a GameObject asset.");
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerName);
            if (player != null)
            {
                return player.transform;
            }

            var controller =
                UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            return controller != null
                ? controller.transform
                : throw new InvalidOperationException("Player is missing.");
        }

        private static GameObject RequireRoot(string name)
        {
            return GameObject.Find(name) ??
                   throw new InvalidOperationException(
                       name + " is missing from CargoRunMvp.");
        }

        private static void RequireSource()
        {
            if (!File.Exists(SourcePath))
            {
                throw new FileNotFoundException(
                    "The supplied Resistance FBX is missing.",
                    SourcePath);
            }
        }

        private static void RequireSameHash(string first, string second)
        {
            if (!string.Equals(first, second, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The supplied and imported Resistance FBX hashes differ.");
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void WriteText(string path, string content)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Invalid report folder."));
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static string SlotName(int index)
        {
            return "Resistance_" + (index + 1).ToString("00", CultureInfo.InvariantCulture);
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
                Path.Combine(Application.dataPath, "..", relative));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " +
                   Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " +
                   Num(value.z) + ", " + Num(value.w) + ")";
        }

        private sealed class Metrics
        {
            public Vector3 Rebellion;
            public Vector3 Resistance;
            public Vector3 Player;
            public Vector3 PlayerForward;
            public Vector3 Camera;
            public Vector3 CameraForward;
            public float ZSpacing;
            public float XSpacing;
            public Bounds FocusBounds;
        }
    }
}
