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

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaPlacementEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath =
            "D:/Bellerophon2/Bellerophon/enemies model/KUŠkursa.fbx";
        private const string ArtRoot =
            "Assets/_Project/Art/Enemies/Kursa";
        private const string ModelFolder = ArtRoot + "/Models";
        private const string ModelPath = ModelFolder + "/Kursa.fbx";
        private const string PlacementRootName =
            "Approved Kursa Enemy Placement";
        private const string PahurRootName =
            "Approved Pahur Enemy Placement";
        private const string LongaRootName =
            "Approved Longa Arma Enemy Placement";
        private const string TergoRootName =
            "Approved Tergo Enemy Placement";
        private const string PlayerName = "Player";
        private const string ModelName = "Kursa_Model";
        private const string ExpectedSourceSha256 =
            "C1FD1C872ADA95B597DC2F93C9BFC523E5A7E88410541F85B2F6B2DA2F7D18A7";
        private const int SlotCount = 12;
        private const float TargetHeight = 1.55f;
        private const float FacingYaw = 180f;
        private const float Tolerance = 0.03f;
        private const float MinimumPlayerDistance = 2.5f;
        private const float CameraMargin = 0.8f;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review",
            "Kursa_02_Idle",
            "Kursa_03_Move",
            "Kursa_04_ShieldBash",
            "Kursa_05_ToShieldStance",
            "Kursa_06_PostBreakRecovery",
            "Kursa_07_ShieldStanceMove",
            "Kursa_08_FromShieldStance",
            "Kursa_09_Stop",
            "Kursa_10_Hit",
            "Kursa_11_Death",
            "Kursa_12_ShieldBreakReaction"
        };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Placement")]
        public static void ApplyKursaPlacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying Kursa placement.");
            }

            var sourceHash = Sha256(SourcePath);
            RequireSameHash(ExpectedSourceSha256, sourceHash);
            CopyAndImportModel();
            var importedHash = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHash, importedHash);

            var modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException(
                    "The imported Kursa FBX is unavailable.");
            RequireVisibleGeometry(modelAsset.transform);

            var protectedBefore = ProtectedRootSignatures(scene);
            var pahur = RequireRoot(PahurRootName).transform;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = PahurSlotSpacing(pahur);

            var oldRoot = GameObject.Find(PlacementRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(
                new Vector3(
                    pahur.position.x,
                    pahur.position.y,
                    pahur.position.z - zSpacing),
                Quaternion.identity);

            for (var index = 0; index < SlotCount; index++)
            {
                var slot = new GameObject(SlotNames[index]);
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition =
                    new Vector3(index * xSpacing, 0f, 0f);
                slot.transform.localRotation =
                    Quaternion.Euler(0f, FacingYaw, 0f);

                var model =
                    PrefabUtility.InstantiatePrefab(modelAsset, scene)
                    as GameObject ??
                    throw new InvalidOperationException(
                        "The supplied Kursa FBX could not be instantiated.");
                model.name = ModelName;
                model.transform.SetParent(slot.transform, false);
                model.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                model.transform.localScale = Vector3.one;
                ConfigureStaticModel(model.transform);
                ScaleAndGround(model.transform, root.transform.position.y);
                EditorUtility.SetDirty(slot);
                EditorUtility.SetDirty(model);
            }

            ConfigurePlayer(root.transform);
            var metrics = InspectState(scene, root.transform, true);
            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(
                    protectedAfter,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Kursa and Player changed during placement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Kursa placement.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(sourceHash, Sha256(SourcePath));
            RequireSameHash(importedHash, Sha256(Absolute(ModelPath)));
            Debug.Log(
                "KursaPlacementApplied Result=PASS" +
                ", Slots=" + SlotCount +
                ", Position=" + Vec(metrics.Kursa) +
                ", PahurPosition=" + Vec(metrics.Pahur) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", PahurXSpacing=" + Num(metrics.XSpacing) +
                ", TargetHeight=" + Num(TargetHeight) +
                ", Player=" + Vec(metrics.Player) +
                ", PlayerForward=" + Vec(metrics.PlayerForward) +
                ", FullLineupVisible=" + metrics.FullLineupVisible +
                ", SourceSha256=" + sourceHash +
                ", DirectFbxInstances=" + SlotCount +
                ", AnimationApplied=False" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Placement")]
        public static void InspectKursaPlacement()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Kursa placement root is missing.");
            var metrics = InspectState(scene, root.transform, true);
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException(
                    "Unity reports script compilation errors.");
            }

            var sourceHash = Sha256(SourcePath);
            var importedHash = Sha256(Absolute(ModelPath));
            RequireSameHash(ExpectedSourceSha256, sourceHash);
            RequireSameHash(sourceHash, importedHash);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Kursa placement inspection changed the scene dirty state.");
            }

            Debug.Log(
                "KursaPlacementInspected Result=PASS" +
                ", Slots=" + SlotCount +
                ", Position=" + Vec(metrics.Kursa) +
                ", PahurPosition=" + Vec(metrics.Pahur) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", PahurXSpacing=" + Num(metrics.XSpacing) +
                ", TargetHeight=" + Num(TargetHeight) +
                ", LineupBounds=" + Vec(metrics.Bounds.size) +
                ", Player=" + Vec(metrics.Player) +
                ", PlayerForward=" + Vec(metrics.PlayerForward) +
                ", FullLineupVisible=" + metrics.FullLineupVisible +
                ", SourceSha256=" + sourceHash +
                ", DirectFbxInstances=" + SlotCount +
                ", SceneChanged=False.");
        }

        private static Metrics InspectState(
            Scene scene,
            Transform root,
            bool requireFullLineupVisible)
        {
            var pahur = RequireRoot(PahurRootName).transform;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = PahurSlotSpacing(pahur);
            var expectedPosition = new Vector3(
                pahur.position.x,
                pahur.position.y,
                pahur.position.z - zSpacing);
            if (Vector3.Distance(root.position, expectedPosition) > Tolerance ||
                Quaternion.Angle(root.rotation, Quaternion.identity) > 0.1f ||
                Vector3.Distance(root.localScale, Vector3.one) > Tolerance ||
                root.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "Kursa root position or twelve-slot contract differs.");
            }

            var rendererCount = -1;
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = root.GetChild(index);
                if (slot.name != SlotNames[index] ||
                    Vector3.Distance(
                        slot.localPosition,
                        new Vector3(index * xSpacing, 0f, 0f)) > Tolerance ||
                    Quaternion.Angle(
                        slot.localRotation,
                        Quaternion.Euler(0f, FacingYaw, 0f)) > 0.1f ||
                    Vector3.Distance(slot.localScale, Vector3.one) > Tolerance ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Kursa slot contract differs at index " + index + ".");
                }

                var model = slot.GetChild(0);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(
                    model.gameObject);
                if (model.name != ModelName ||
                    source == null ||
                    AssetDatabase.GetAssetPath(source) != ModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " is not a direct instance of the supplied Kursa FBX.");
                }

                var renderers = RequireVisibleGeometry(model);
                if (rendererCount < 0)
                {
                    rendererCount = renderers.Length;
                }
                else if (rendererCount != renderers.Length)
                {
                    throw new InvalidOperationException(
                        "Kursa renderer count differs between slots.");
                }

                var modelBounds = BoundsOf(
                    model,
                    new Bounds(model.position, Vector3.one));
                if (Mathf.Abs(modelBounds.size.y - TargetHeight) > Tolerance ||
                    Mathf.Abs(modelBounds.min.y - root.position.y) > Tolerance)
                {
                    throw new InvalidOperationException(
                        slot.name + " height or ground alignment differs.");
                }

                if (model.GetComponentsInChildren<Animator>(true)
                        .Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true)
                        .Any(item => item.enabled))
                {
                    throw new InvalidOperationException(
                        "Kursa placement models must remain static.");
                }
            }

            var bounds = BoundsOf(
                root,
                new Bounds(root.position, Vector3.one));
            var player = RequirePlayer();
            var camera =
                player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var fullLineupVisible = InspectPlayer(
                player,
                camera,
                root,
                bounds,
                requireFullLineupVisible);

            return new Metrics
            {
                Kursa = root.position,
                Pahur = pahur.position,
                Player = player.position,
                PlayerForward = player.forward,
                ZSpacing = zSpacing,
                XSpacing = xSpacing,
                Bounds = bounds,
                RendererCount = rendererCount,
                FullLineupVisible = fullLineupVisible
            };
        }

        private static void ConfigurePlayer(Transform root)
        {
            var player = RequirePlayer();
            var camera =
                player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var bounds = BoundsOf(
                root,
                new Bounds(root.position, Vector3.one));
            var front = root.GetChild(0).forward;
            front.y = 0f;
            front.Normalize();
            var desiredCamera =
                bounds.center + front * PlayerDistance(bounds, camera);
            var yaw = YawToward(desiredCamera, bounds.center);
            var cameraOffsetLocal =
                player.InverseTransformPoint(camera.transform.position);
            var desiredPlayer = desiredCamera - yaw * cameraOffsetLocal;
            desiredPlayer.y = player.position.y;
            player.SetPositionAndRotation(desiredPlayer, yaw);
            EditorUtility.SetDirty(player);
        }

        private static bool InspectPlayer(
            Transform player,
            Camera camera,
            Transform placementRoot,
            Bounds bounds,
            bool requireFullLineupVisible)
        {
            var fromFocus = camera.transform.position - bounds.center;
            fromFocus.y = 0f;
            var front = placementRoot.GetChild(0).forward;
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
                Vector3.Dot(
                    toFocus.normalized,
                    cameraForward.normalized) < 0.98f)
            {
                throw new InvalidOperationException(
                    "Player camera is not centered in front of Kursa.");
            }

            var fullLineupVisible = Corners(bounds).All(corner =>
            {
                var view = camera.WorldToViewportPoint(corner);
                return view.z > 0f &&
                       view.x >= -0.02f && view.x <= 1.02f &&
                       view.y >= -0.02f && view.y <= 1.02f;
            });
            if (requireFullLineupVisible && !fullLineupVisible)
            {
                throw new InvalidOperationException(
                    "Player camera does not contain the full Kursa lineup.");
            }

            foreach (Transform slot in placementRoot)
            {
                var slotBounds = BoundsOf(
                    slot,
                    new Bounds(slot.position, Vector3.one));
                var view = camera.WorldToViewportPoint(slotBounds.center);
                if (view.z <= 0f ||
                    view.x < 0f || view.x > 1f ||
                    view.y < 0f || view.y > 1f)
                {
                    throw new InvalidOperationException(
                        slot.name + " center is outside the Player camera.");
                }
            }

            var playerToCenter = bounds.center - player.position;
            playerToCenter.y = 0f;
            var playerForward = player.forward;
            playerForward.y = 0f;
            if (playerToCenter.sqrMagnitude < 0.001f ||
                playerForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(
                    playerToCenter.normalized,
                    playerForward.normalized) < 0.98f)
            {
                throw new InvalidOperationException(
                    "Player root does not face the Kursa lineup.");
            }

            return fullLineupVisible;
        }

        private static float PlayerDistance(Bounds bounds, Camera camera)
        {
            var vertical = Mathf.Max(1f, camera.fieldOfView * 0.5f) *
                           Mathf.Deg2Rad;
            var aspect = camera.aspect > 0.1f ?
                camera.aspect : 16f / 9f;
            var horizontal = Mathf.Atan(Mathf.Tan(vertical) * aspect);
            return Mathf.Max(
                MinimumPlayerDistance,
                bounds.extents.x /
                Mathf.Max(0.01f, Mathf.Tan(horizontal)) + CameraMargin,
                bounds.extents.y /
                Mathf.Max(0.01f, Mathf.Tan(vertical)) + CameraMargin);
        }

        private static void CopyAndImportModel()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(ModelFolder);
            var destination = Absolute(ModelPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Kursa model folder."));
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

        private static void ScaleAndGround(Transform model, float groundY)
        {
            var bounds = BoundsOf(
                model,
                new Bounds(model.position, Vector3.one));
            if (bounds.size.y <= 0.00001f)
            {
                throw new InvalidOperationException(
                    "Kursa has no usable visible height.");
            }

            var scale = TargetHeight / bounds.size.y;
            if (float.IsNaN(scale) ||
                float.IsInfinity(scale) ||
                scale <= 0f ||
                scale > 1000f)
            {
                throw new InvalidOperationException(
                    "Kursa target-height scale is invalid.");
            }

            model.localScale = Vector3.one * scale;
            bounds = BoundsOf(
                model,
                new Bounds(model.position, Vector3.one));
            model.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static float LongaTergoSpacing(
            Transform longa,
            Transform tergo)
        {
            var spacing = Mathf.Abs(longa.position.z - tergo.position.z);
            if (spacing <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Longa Arma/Tergo Z spacing is unusable.");
            }

            return spacing;
        }

        private static float PahurSlotSpacing(Transform root)
        {
            if (root.childCount < 2)
            {
                throw new InvalidOperationException(
                    "Pahur needs at least two slots for X spacing.");
            }

            var spacing = Mathf.Abs(
                root.GetChild(1).position.x -
                root.GetChild(0).position.x);
            if (spacing <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Pahur X spacing is unusable.");
            }

            return spacing;
        }

        private static Renderer[] RequireVisibleGeometry(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Kursa has no visible renderer.");
            }

            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer skinned &&
                    skinned.sharedMesh == null)
                {
                    throw new InvalidOperationException(
                        "Kursa has a SkinnedMeshRenderer without a mesh.");
                }

                if (renderer is MeshRenderer)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        throw new InvalidOperationException(
                            "Kursa has a MeshRenderer without a mesh.");
                    }
                }
            }

            return renderers;
        }

        private static Bounds BoundsOf(Transform root, Bounds fallback)
        {
            var renderers = RequireVisibleGeometry(root);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.size.sqrMagnitude > 0.000001f ? bounds : fallback;
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        yield return bounds.center + Vector3.Scale(
                            bounds.extents,
                            new Vector3(x, y, z));
                    }
                }
            }
        }

        private static Quaternion YawToward(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException(
                    "The Kursa Player view direction is unusable.");
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerName) ??
                         throw new InvalidOperationException(
                             "The Player root is missing.");
            if (player.transform.parent != null)
            {
                throw new InvalidOperationException(
                    "The Player object is not a scene root.");
            }

            return player.transform;
        }

        private static GameObject RequireRoot(string name)
        {
            var root = GameObject.Find(name) ??
                       throw new InvalidOperationException(
                           name + " is missing.");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(
                    name + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. ActiveScene=" +
                    scene.path + ".");
            }

            return scene;
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item =>
                    item.name != PlacementRootName &&
                    item.name != PlayerName)
                .Select(item => HierarchySignature(item.transform))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string HierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             item => RelativePath(root, item),
                             StringComparer.Ordinal))
            {
                builder.Append(RelativePath(root, item));
                builder.Append('|');
                builder.Append(item.gameObject.activeSelf);
                builder.Append('|');
                builder.Append(Vec(item.localPosition));
                builder.Append('|');
                builder.Append(Quat(item.localRotation));
                builder.Append('|');
                builder.Append(Vec(item.localScale));
                builder.Append(';');
            }

            return builder.ToString();
        }

        private static string RelativePath(Transform root, Transform item)
        {
            return item == root ?
                root.name :
                root.name + "/" +
                AnimationUtility.CalculateTransformPath(item, root);
        }

        private static void EnsureFolder(string assetPath)
        {
            var parts = assetPath.Split('/');
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

        private static void RequireSource()
        {
            if (!File.Exists(SourcePath))
            {
                throw new FileNotFoundException(
                    "The supplied Kursa FBX is missing.",
                    SourcePath);
            }
        }

        private static void RequireSameHash(string expected, string actual)
        {
            if (!string.Equals(
                    expected,
                    actual,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Kursa FBX SHA-256 differs. Expected=" + expected +
                    ", Actual=" + actual + ".");
            }
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static string Absolute(string relativePath)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "Unity project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," +
                   Num(value.y) + "," +
                   Num(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return Num(value.x) + "," +
                   Num(value.y) + "," +
                   Num(value.z) + "," +
                   Num(value.w);
        }

        private sealed class Metrics
        {
            public Vector3 Kursa { get; set; }
            public Vector3 Pahur { get; set; }
            public Vector3 Player { get; set; }
            public Vector3 PlayerForward { get; set; }
            public float ZSpacing { get; set; }
            public float XSpacing { get; set; }
            public Bounds Bounds { get; set; }
            public int RendererCount { get; set; }
            public bool FullLineupVisible { get; set; }
        }
    }
}
