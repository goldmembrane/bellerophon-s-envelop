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

namespace Bellerophon.Editor.DoloreCargoRunScene
{
    internal static class DoloreCargoRunSceneApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath = "D:/Bellerophon2/Bellerophon/enemies model/dolore.fbx";
        private const string ArtRoot = "Assets/_Project/Art/Enemies/Dolore";
        private const string ModelFolder = ArtRoot + "/Models";
        private const string ModelPath = ModelFolder + "/Dolore.fbx";
        private const string ValidationFolder = "docs/validation/dolore_cargo_run_placement_2026-07-21";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string OstinatoRootName = "Approved Ostinato Enemy Placement";
        private const string OstinatoFirstSlotName = "Ostinato_01_Static_Review";
        private const string OstinatoSecondSlotName = "Ostinato_02_Idle_Breathing";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string PlayerName = "Player";
        private const string ModelName = "Dolore_Model";
        private const string VisibleMeshName = "char1";
        private const float TargetHeight = 1.8f;
        private const float FacingYaw = 180f;
        private const float Tolerance = 0.03f;
        private const float MinimumCameraDistance = 6f;
        private const float CameraMargin = 1.5f;

        private static readonly string[] SlotNames =
        {
            "Dolore_01_Static_Review",
            "Dolore_02_Idle",
            "Dolore_03_Move_Quadruped",
            "Dolore_04_Tentacle_Stab_Attack",
            "Dolore_05_Execution_Pull_In",
            "Dolore_06_Hit_Reaction",
            "Dolore_07_Death"
        };

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Placement Target")]
        public static void InspectPlacementTarget()
        {
            RequireSource();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wasDirty = scene.isDirty;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var ostinato = RequireRoot(OstinatoRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = OstinatoSlotSpacing(ostinato);
            var expected = DolorePosition(ostinato, zSpacing);
            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Source=" + SourcePath)
                .AppendLine("SourceSha256=" + Sha256(SourcePath))
                .AppendLine("SlotCount=7")
                .AppendLine("SlotNames=" + string.Join("|", SlotNames))
                .AppendLine("LongaPosition=" + Vec(longa.position))
                .AppendLine("TergoPosition=" + Vec(tergo.position))
                .AppendLine("LongaTergoZSpacing=" + Num(zSpacing))
                .AppendLine("OstinatoPosition=" + Vec(ostinato.position))
                .AppendLine("OstinatoSlotXSpacing=" + Num(xSpacing))
                .AppendLine("ExpectedDolorePosition=" + Vec(expected))
                .AppendLine("CurrentPlayerPosition=" + Vec(RequirePlayer().position))
                .AppendLine("ExistingDoloreRoot=" + (GameObject.Find(PlacementRootName) != null))
                .AppendLine("SceneChanged=False");
            WriteText(ValidationFolder + "/Dolore_PlacementTarget.txt", report.ToString());
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("Dolore target inspection changed the scene dirty state.");
            }
            Debug.Log("DolorePlacementTargetInspected Expected=" + Vec(expected) +
                      ", ZSpacing=" + Num(zSpacing) + ", XSpacing=" + Num(xSpacing) + ".");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Placement")]
        public static void ApplyPlacement()
        {
            RequireSource();
            var sourceHashBefore = Sha256(SourcePath);
            CopyAndImportModel();
            var importedHashBefore = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, importedHashBefore);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException("The imported Dolore FBX is missing.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var protectedBefore = ProtectedRootSignatures(scene);
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var ostinato = RequireRoot(OstinatoRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = OstinatoSlotSpacing(ostinato);
            var oldRoot = GameObject.Find(PlacementRootName);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);

            var root = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(DolorePosition(ostinato, zSpacing), Quaternion.identity);
            for (var i = 0; i < SlotNames.Length; i++)
            {
                var slot = new GameObject(SlotNames[i]);
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3(i * xSpacing, 0f, 0f);
                slot.transform.localRotation = Quaternion.Euler(0f, FacingYaw, 0f);
                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                            throw new InvalidOperationException("The supplied Dolore FBX could not be instantiated.");
                model.name = ModelName;
                model.transform.SetParent(slot.transform, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = Vector3.one;
                ConfigureStaticVisibleModel(model.transform);
                ScaleAndGround(model.transform, root.transform.position.y);
                EditorUtility.SetDirty(slot);
                EditorUtility.SetDirty(model);
            }

            ConfigurePlayer(root.transform);
            var metrics = InspectState(scene, root.transform);
            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside Dolore and Player changed during placement.");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after Dolore placement.");
            AssetDatabase.SaveAssets();

            var sourceHashAfter = Sha256(SourcePath);
            var importedHashAfter = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, sourceHashAfter);
            RequireSameHash(importedHashBefore, importedHashAfter);
            var report = PlacementReport(metrics)
                .AppendLine("SourceSha256Before=" + sourceHashBefore)
                .AppendLine("SourceSha256After=" + sourceHashAfter)
                .AppendLine("ImportedSha256Before=" + importedHashBefore)
                .AppendLine("ImportedSha256After=" + importedHashAfter)
                .AppendLine("SourceCopyHashesMatch=True")
                .AppendLine("OtherSceneRootsUnchanged=True")
                .AppendLine("SceneSaved=True");
            WriteText(ValidationFolder + "/Dolore_PlacementApply.txt", report.ToString());
            Debug.Log("DolorePlacementApplied Slots=7, Position=" + Vec(metrics.Dolore) +
                      ", Player=" + Vec(metrics.Player) + ".");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Applied Placement")]
        public static void InspectAppliedPlacement()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Dolore placement root is missing.");
            var metrics = InspectState(scene, root.transform);
            var sourceHash = Sha256(SourcePath);
            var importedHash = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHash, importedHash);
            var report = PlacementReport(metrics)
                .AppendLine("SourceSha256=" + sourceHash)
                .AppendLine("ImportedSha256=" + importedHash)
                .AppendLine("SourceCopyHashesMatch=True")
                .AppendLine("SceneChanged=False");
            WriteText(ValidationFolder + "/Dolore_PlacementInspection.txt", report.ToString());
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Dolore inspection changed the scene dirty state.");
            Debug.Log("DolorePlacementInspected Result=PASS, Slots=7, PlayerFront=True, CameraFramed=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Player Start View")]
        public static void CapturePlayerStartView()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Dolore placement root is missing.");
            var metrics = InspectState(scene, root.transform);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(ValidationFolder + "/Dolore_PlayerStartView.png"), 1920, 1080);
            WriteText(ValidationFolder + "/Dolore_PlayerStartView.txt",
                new StringBuilder().AppendLine("Result=PASS").AppendLine("Scene=" + ScenePath)
                    .AppendLine("Camera=Player").AppendLine("PlayerPosition=" + Vec(metrics.Player))
                    .AppendLine("PlayerForward=" + Vec(metrics.PlayerForward))
                    .AppendLine("LineupBoundsCenter=" + Vec(metrics.Bounds.center))
                    .AppendLine("LineupBoundsSize=" + Vec(metrics.Bounds.size))
                    .AppendLine("AllDoloreVisible=True")
                    .AppendLine("Image=" + ValidationFolder + "/Dolore_PlayerStartView.png").ToString());
            Debug.Log("DolorePlayerStartViewCaptured Image=" + ValidationFolder + "/Dolore_PlayerStartView.png.");
        }

        private static Metrics InspectState(Scene scene, Transform root)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active.");
            RequireSource();
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var ostinato = RequireRoot(OstinatoRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = OstinatoSlotSpacing(ostinato);
            var expectedRoot = DolorePosition(ostinato, zSpacing);
            if (Vector3.Distance(root.position, expectedRoot) > Tolerance || root.childCount != SlotNames.Length)
                throw new InvalidOperationException("Dolore root position or seven-slot contract changed.");

            for (var i = 0; i < SlotNames.Length; i++)
            {
                var slot = root.GetChild(i);
                if (slot.name != SlotNames[i] ||
                    Vector3.Distance(slot.localPosition, new Vector3(i * xSpacing, 0f, 0f)) > Tolerance ||
                    Quaternion.Angle(slot.localRotation, Quaternion.Euler(0f, FacingYaw, 0f)) > 0.1f ||
                    slot.childCount != 1)
                    throw new InvalidOperationException("Dolore slot contract changed at index " + i + ".");
                var model = slot.GetChild(0);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                if (model.name != ModelName || source == null || AssetDatabase.GetAssetPath(source) != ModelPath)
                    throw new InvalidOperationException(slot.name + " is not a direct instance of the supplied FBX.");
                var visible = model.GetComponentsInChildren<Renderer>(false)
                    .Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
                if (visible.Length != 1 || visible[0].name != VisibleMeshName)
                    throw new InvalidOperationException(slot.name + " must display only char1.");
                var modelBounds = BoundsOf(model, new Bounds(model.position, Vector3.one));
                if (Mathf.Abs(modelBounds.size.y - TargetHeight) > Tolerance ||
                    Mathf.Abs(modelBounds.min.y - root.position.y) > Tolerance)
                    throw new InvalidOperationException(slot.name + " height or ground alignment changed.");
                if (model.GetComponentsInChildren<Animator>(true).Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true).Any(item => item.enabled))
                    throw new InvalidOperationException("Dolore placeholders must remain static.");
            }

            var bounds = BoundsOf(root, new Bounds(root.position, Vector3.one));
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            InspectPlayer(player, camera, root.GetChild(0), bounds);
            return new Metrics
            {
                Longa = longa.position, Tergo = tergo.position, Ostinato = ostinato.position,
                Dolore = root.position, ZSpacing = zSpacing, XSpacing = xSpacing,
                Player = player.position, PlayerForward = player.forward, Bounds = bounds
            };
        }

        private static StringBuilder PlacementReport(Metrics m)
        {
            return new StringBuilder().AppendLine("Result=PASS").AppendLine("Scene=" + ScenePath)
                .AppendLine("Source=" + SourcePath).AppendLine("ImportedAsset=" + ModelPath)
                .AppendLine("PlacementRoot=" + PlacementRootName).AppendLine("SlotCount=7")
                .AppendLine("SlotNames=" + string.Join("|", SlotNames)).AppendLine("DirectFbxInstanceCount=7")
                .AppendLine("VisibleRendererPerSlot=char1").AppendLine("AnimationApplied=False")
                .AppendLine("TargetHeightMeters=" + Num(TargetHeight)).AppendLine("LongaPosition=" + Vec(m.Longa))
                .AppendLine("TergoPosition=" + Vec(m.Tergo)).AppendLine("LongaTergoZSpacing=" + Num(m.ZSpacing))
                .AppendLine("OstinatoPosition=" + Vec(m.Ostinato)).AppendLine("DolorePosition=" + Vec(m.Dolore))
                .AppendLine("OstinatoSlotXSpacing=" + Num(m.XSpacing)).AppendLine("PlayerPosition=" + Vec(m.Player))
                .AppendLine("PlayerForward=" + Vec(m.PlayerForward)).AppendLine("LineupBoundsCenter=" + Vec(m.Bounds.center))
                .AppendLine("LineupBoundsSize=" + Vec(m.Bounds.size)).AppendLine("PlayerFacesDolore=True")
                .AppendLine("AllDoloreVisible=True");
        }

        private static void ConfigurePlayer(Transform root)
        {
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var bounds = BoundsOf(root, new Bounds(root.position, Vector3.one));
            var front = root.GetChild(0).forward;
            front.y = 0f;
            front.Normalize();
            var desiredCamera = bounds.center + front * PlayerDistance(bounds, camera);
            var yaw = YawToward(desiredCamera, bounds.center);
            player.rotation = yaw;
            var cameraOffset = camera.transform.position - player.position;
            var desiredPlayer = desiredCamera - cameraOffset;
            desiredPlayer.y = 0f;
            player.SetPositionAndRotation(desiredPlayer, yaw);
            EditorUtility.SetDirty(player);
        }

        private static void InspectPlayer(Transform player, Camera camera, Transform firstSlot, Bounds bounds)
        {
            var fromFocus = player.position - bounds.center;
            fromFocus.y = 0f;
            var front = firstSlot.forward;
            front.y = 0f;
            var toFocus = bounds.center - player.position;
            toFocus.y = 0f;
            var forward = player.forward;
            forward.y = 0f;
            if (fromFocus.sqrMagnitude < 0.001f || front.sqrMagnitude < 0.001f ||
                Vector3.Dot(fromFocus.normalized, front.normalized) < 0.98f ||
                toFocus.sqrMagnitude < 0.001f || forward.sqrMagnitude < 0.001f ||
                Vector3.Dot(toFocus.normalized, forward.normalized) < 0.98f)
                throw new InvalidOperationException("Player is not centered in front of Dolore.");
            foreach (var corner in Corners(bounds))
            {
                var view = camera.WorldToViewportPoint(corner);
                if (view.z <= 0f || view.x < -0.02f || view.x > 1.02f || view.y < -0.02f || view.y > 1.02f)
                    throw new InvalidOperationException("Player camera does not contain the full Dolore lineup.");
            }
        }

        private static float PlayerDistance(Bounds bounds, Camera camera)
        {
            var vertical = Mathf.Max(1f, camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            var aspect = camera.aspect > 0.1f ? camera.aspect : 16f / 9f;
            var horizontal = Mathf.Atan(Mathf.Tan(vertical) * aspect);
            return Mathf.Max(MinimumCameraDistance,
                bounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontal)) + CameraMargin,
                bounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(vertical)) + CameraMargin);
        }

        private static void CopyAndImportModel()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(ModelFolder);
            var destination = Absolute(ModelPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Invalid model folder."));
            File.Copy(SourcePath, destination, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                           throw new InvalidOperationException("Dolore ModelImporter is missing.");
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = true;
            importer.importVisibility = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.globalScale = 1f;
            importer.SaveAndReimport();
        }

        private static void ConfigureStaticVisibleModel(Transform model)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = renderer.name == VisibleMeshName;
                if (!renderer.enabled && renderer.gameObject.name == "Cube") renderer.gameObject.SetActive(false);
                EditorUtility.SetDirty(renderer);
            }
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
            if (bounds.size.y <= 0.00001f) throw new InvalidOperationException("Dolore has no usable visible height.");
            var scale = TargetHeight / bounds.size.y;
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f || scale > 1000f)
                throw new InvalidOperationException("Dolore target-height scale is invalid.");
            model.localScale = Vector3.one * scale;
            bounds = BoundsOf(model, new Bounds(model.position, Vector3.one));
            model.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static float LongaTergoSpacing(Transform longa, Transform tergo)
        {
            var value = Mathf.Abs(longa.position.z - tergo.position.z);
            if (value <= 0.1f) throw new InvalidOperationException("Longa/Tergo Z spacing is unusable.");
            return value;
        }

        private static float OstinatoSlotSpacing(Transform root)
        {
            var first = root.Find(OstinatoFirstSlotName) ?? throw new InvalidOperationException("Ostinato slot 1 is missing.");
            var second = root.Find(OstinatoSecondSlotName) ?? throw new InvalidOperationException("Ostinato slot 2 is missing.");
            var value = Mathf.Abs(second.position.x - first.position.x);
            if (value <= 0.1f) throw new InvalidOperationException("Ostinato X spacing is unusable.");
            return value;
        }

        private static Vector3 DolorePosition(Transform ostinato, float spacing) =>
            new Vector3(ostinato.position.x, ostinato.position.y, ostinato.position.z - spacing);

        private static Bounds BoundsOf(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(false)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0) return fallback;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (var x = 0; x < 2; x++) for (var y = 0; y < 2; y++) for (var z = 0; z < 2; z++)
                yield return new Vector3(x == 0 ? b.min.x : b.max.x, y == 0 ? b.min.y : b.max.y, z == 0 ? b.min.z : b.max.z);
        }

        private static void Capture(Camera camera, string path, int width, int height)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Invalid capture folder."));
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
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

        private static string[] ProtectedRootSignatures(Scene scene) => scene.GetRootGameObjects()
            .Where(root => root.name != PlacementRootName && root.name != PlayerName)
            .Select(root => GlobalObjectId.GetGlobalObjectIdSlow(root) + "|" + root.name + "|" + root.activeSelf + "|" +
                            Vec(root.transform.position) + "|" + Quat(root.transform.rotation) + "|" +
                            Vec(root.transform.localScale) + "|" + root.transform.childCount)
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerName);
            if (player != null) return player.transform;
            var controller = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            return controller != null ? controller.transform : throw new InvalidOperationException("Player is missing.");
        }

        private static GameObject RequireRoot(string name) => GameObject.Find(name) ??
            throw new InvalidOperationException(name + " is missing from CargoRunMvp.");

        private static void RequireSource()
        {
            if (!File.Exists(SourcePath)) throw new FileNotFoundException("The supplied Dolore FBX is missing.", SourcePath);
        }

        private static void RequireSameHash(string first, string second)
        {
            if (!string.Equals(first, second, StringComparison.Ordinal))
                throw new InvalidOperationException("The supplied and imported Dolore FBX hashes differ.");
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static Quaternion YawToward(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction.normalized) : Quaternion.identity;
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void WriteText(string relative, string contents)
        {
            var path = Absolute(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Invalid report folder."));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        private static string Absolute(string relative) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        private static string Num(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) => "(" + Num(value.x) + ", " + Num(value.y) + ", " + Num(value.z) + ")";
        private static string Quat(Quaternion value) => "(" + Num(value.x) + ", " + Num(value.y) + ", " + Num(value.z) + ", " + Num(value.w) + ")";

        private sealed class Metrics
        {
            public Vector3 Longa, Tergo, Ostinato, Dolore, Player, PlayerForward;
            public float ZSpacing, XSpacing;
            public Bounds Bounds;
        }
    }
}
