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

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static class PahurPlacementEditor
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath =
            "D:/Bellerophon2/Bellerophon/enemies model/pāḫḫur.fbx";
        private const string ArtRoot = "Assets/_Project/Art/Enemies/Pahur";
        private const string ModelFolder = ArtRoot + "/Models";
        private const string ModelPath = ModelFolder + "/Pahur.fbx";
        private const string LongaRootName =
            "Approved Longa Arma Enemy Placement";
        private const string TergoRootName =
            "Approved Tergo Enemy Placement";
        private const string RevolutionRootName =
            "Approved Revolution Enemy Placement";
        private const string RevolutionFirstSlotName = "Revolution_01";
        private const string RevolutionSecondSlotName = "Revolution_02";
        private const string PlacementRootName =
            "Approved Pahur Enemy Placement";
        private const string PlayerName = "Player";
        private const string ModelName = "Pahur_Model";
        private const string ValidationFolder =
            "docs/validation/pahur_placement_2026-07-29";
        private const string InspectionPath =
            ValidationFolder + "/Pahur_Placement_Inspection.txt";
        private const string CapturePath =
            ValidationFolder + "/Pahur_Placement_VisualReview.png";
        private const string ExpectedSourceSha256 =
            "9D376E7BD5262E9DF347D8CCC37675DDBCF647D063011C0A71E80CDDA4A9DEA3";
        private const int SlotCount = 11;
        private const float TargetHeight = 1.5f;
        private const float FacingYaw = 180f;
        private const float Tolerance = 0.03f;
        private const float MinimumPlayerDistance = 2.5f;
        private const float CameraMargin = 0.8f;

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

        [MenuItem("Bellerophon/Enemies/Pahur/Replace Placed Models")]
        public static void ReplacePlacedPahurModels()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before replacing Pahur models.");
            }

            var sourceHash = Sha256(SourcePath);
            RequireSameHash(ExpectedSourceSha256, sourceHash);
            var root = RequireRoot(PlacementRootName).transform;
            var placementBefore = PlacementFrameSignatures(root);
            var protectedBefore = NonPahurRootSignatures(scene);

            CopyAndImportModel();
            var importedHash = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHash, importedHash);
            var modelAsset = RequireModelAsset();
            RequireVisibleGeometry(modelAsset.transform);

            var oldModels = new List<GameObject>();
            var newModels = new List<GameObject>();
            try
            {
                for (var index = 0; index < SlotCount; index++)
                {
                    var slot = root.GetChild(index);
                    if (slot.name != SlotNames[index] ||
                        slot.childCount != 1)
                    {
                        throw new InvalidOperationException(
                            "Pahur slot contract changed at index " +
                            index + ".");
                    }

                    oldModels.Add(slot.GetChild(0).gameObject);
                    var model =
                        PrefabUtility.InstantiatePrefab(modelAsset, scene)
                        as GameObject ??
                        throw new InvalidOperationException(
                            "The replacement Pahur FBX could not be instantiated.");
                    newModels.Add(model);
                    model.name = ModelName;
                    model.transform.SetParent(slot, false);
                    model.transform.SetLocalPositionAndRotation(
                        Vector3.zero,
                        Quaternion.identity);
                    model.transform.localScale = Vector3.one;
                    ConfigureStaticModel(model.transform);
                    ScaleAndGround(model.transform, root.position.y);
                    RequireVisibleGeometry(model.transform);
                }
            }
            catch
            {
                foreach (var model in newModels)
                {
                    if (model != null)
                    {
                        UnityEngine.Object.DestroyImmediate(model);
                    }
                }

                throw;
            }

            foreach (var oldModel in oldModels)
            {
                UnityEngine.Object.DestroyImmediate(oldModel);
            }

            foreach (var model in newModels)
            {
                EditorUtility.SetDirty(model);
                EditorUtility.SetDirty(model.transform.parent);
            }

            var placementAfter = PlacementFrameSignatures(root);
            if (!placementBefore.SequenceEqual(
                    placementAfter,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pahur root or slot transforms changed during model replacement.");
            }

            var protectedAfter = NonPahurRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(
                    protectedAfter,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Pahur changed during model replacement.");
            }

            var metrics = InspectState(scene, root, true);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Pahur model replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(sourceHash, Sha256(SourcePath));
            RequireSameHash(importedHash, Sha256(Absolute(ModelPath)));
            Debug.Log(
                "PahurModelsReplaced Result=PASS, Slots=" + SlotCount +
                ", SourceSha256=" + sourceHash +
                ", DirectFbxInstances=" + SlotCount +
                ", TargetHeight=" + Num(TargetHeight) +
                ", LineupBounds=" + Vec(metrics.Bounds.size) +
                ", SlotNamesAndTransformsPreserved=True" +
                ", PlayerUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Apply Placement")]
        public static void ApplyPahurPlacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying Pahur placement.");
            }

            var sourceHashBefore = Sha256(SourcePath);
            RequireSameHash(ExpectedSourceSha256, sourceHashBefore);
            CopyAndImportModel();
            var importedHashBefore = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, importedHashBefore);
            var modelAsset = RequireModelAsset();
            RequireVisibleGeometry(modelAsset.transform);

            var protectedBefore = ProtectedRootSignatures(scene);
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var revolution = RequireRoot(RevolutionRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = RevolutionSlotSpacing(revolution);

            var oldRoot = GameObject.Find(PlacementRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(
                new Vector3(
                    revolution.position.x,
                    revolution.position.y,
                    revolution.position.z - zSpacing),
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
                        "The supplied Pahur FBX could not be instantiated.");
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
                    "A scene root outside Pahur and Player changed during placement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Pahur placement.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(sourceHashBefore, Sha256(SourcePath));
            RequireSameHash(importedHashBefore, Sha256(Absolute(ModelPath)));
            Debug.Log(
                "PahurPlacementApplied Result=PASS, Slots=" + SlotCount +
                ", Position=" + Vec(metrics.Pahur) +
                ", RevolutionPosition=" + Vec(metrics.Revolution) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", RevolutionXSpacing=" + Num(metrics.XSpacing) +
                ", LineupBounds=" + Vec(metrics.Bounds.size) +
                ", Player=" + Vec(metrics.Player) +
                ", PlayerForward=" + Vec(metrics.PlayerForward) +
                ", SourceSha256=" + sourceHashBefore +
                ", DirectFbxInstances=" + SlotCount +
                ", TargetHeight=" + Num(TargetHeight) +
                ", AnimationApplied=False" +
                ", OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Inspect Placement")]
        public static void InspectPahurPlacement()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Pahur placement root is missing.");
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
            WriteInspectionReport(metrics, sourceHash);
            AssetDatabase.Refresh();
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur placement inspection changed the scene dirty state.");
            }

            Debug.Log(
                "PahurPlacementInspected Result=PASS, Report=" +
                InspectionPath + ", Slots=" + SlotCount +
                ", SourceSha256=" + sourceHash +
                ", FullLineupVisible=" +
                metrics.FullLineupBoundsVisible +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Capture Placement Review")]
        public static void CapturePahurPlacementReview()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Pahur placement root is missing.");
            var metrics = InspectState(scene, root.transform, true);
            var camera =
                RequirePlayer().GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            Capture(camera, Absolute(CapturePath), 1920, 1080);
            AssetDatabase.Refresh();
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur placement capture changed the scene dirty state.");
            }

            Debug.Log(
                "PahurPlacementReviewCaptured Result=PASS, Image=" +
                CapturePath + ", PlayerPosition=" + Vec(metrics.Player) +
                ", PlayerForward=" + Vec(metrics.PlayerForward) +
                ", FullLineupVisible=" +
                metrics.FullLineupBoundsVisible +
                ", SceneChanged=False.");
        }

        private static Metrics InspectState(
            Scene scene,
            Transform root,
            bool requireFullLineupVisible)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active.");
            }

            RequireSource();
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var revolution = RequireRoot(RevolutionRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = RevolutionSlotSpacing(revolution);
            var expected = new Vector3(
                revolution.position.x,
                revolution.position.y,
                revolution.position.z - zSpacing);
            if (Vector3.Distance(root.position, expected) > Tolerance ||
                root.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "Pahur root position or eleven-slot contract changed.");
            }

            var rendererCount = -1;
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = root.GetChild(index);
                if (slot.name != SlotNames[index] ||
                    Vector3.Distance(
                        slot.localPosition,
                        new Vector3(index * xSpacing, 0f, 0f)) >
                    Tolerance ||
                    Quaternion.Angle(
                        slot.localRotation,
                        Quaternion.Euler(0f, FacingYaw, 0f)) > 0.1f ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Pahur slot contract changed at index " +
                        index + ".");
                }

                var model = slot.GetChild(0);
                var source =
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        model.gameObject);
                if (model.name != ModelName ||
                    source == null ||
                    AssetDatabase.GetAssetPath(source) != ModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " is not a direct instance of the supplied Pahur FBX.");
                }

                var renderers = RequireVisibleGeometry(model);
                if (rendererCount < 0)
                {
                    rendererCount = renderers.Length;
                }
                else if (rendererCount != renderers.Length)
                {
                    throw new InvalidOperationException(
                        "Pahur renderer count differs between slots.");
                }

                var modelBounds = BoundsOf(
                    model,
                    new Bounds(model.position, Vector3.one));
                if (Mathf.Abs(modelBounds.size.y - TargetHeight) >
                    Tolerance ||
                    Mathf.Abs(modelBounds.min.y - root.position.y) >
                    Tolerance)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " height or ground alignment changed.");
                }

                if (model.GetComponentsInChildren<Animator>(true)
                        .Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true)
                        .Any(item => item.enabled))
                {
                    throw new InvalidOperationException(
                        "Pahur placeholders must remain static.");
                }
            }

            var bounds =
                BoundsOf(root, new Bounds(root.position, Vector3.one));
            var player = RequirePlayer();
            var camera =
                player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var fullLineupBoundsVisible =
                InspectPlayer(
                    player,
                    camera,
                    root,
                    bounds,
                    requireFullLineupVisible);
            return new Metrics
            {
                Revolution = revolution.position,
                Pahur = root.position,
                Player = player.position,
                PlayerForward = player.forward,
                Camera = camera.transform.position,
                CameraForward = camera.transform.forward,
                ZSpacing = zSpacing,
                XSpacing = xSpacing,
                Bounds = bounds,
                RendererCount = rendererCount,
                FullLineupBoundsVisible = fullLineupBoundsVisible
            };
        }

        private static void ConfigurePlayer(Transform root)
        {
            var player = RequirePlayer();
            var camera =
                player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var bounds =
                BoundsOf(root, new Bounds(root.position, Vector3.one));
            var front = root.GetChild(0).forward;
            front.y = 0f;
            front.Normalize();
            var desiredCamera =
                bounds.center + front * PlayerDistance(bounds, camera);
            var yaw = YawToward(desiredCamera, bounds.center);
            var cameraOffsetLocal =
                player.InverseTransformPoint(camera.transform.position);
            var desiredPlayer =
                desiredCamera - yaw * cameraOffsetLocal;
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
            var forward = camera.transform.forward;
            forward.y = 0f;
            if (fromFocus.sqrMagnitude < 0.001f ||
                front.sqrMagnitude < 0.001f ||
                Vector3.Dot(
                    fromFocus.normalized,
                    front.normalized) < 0.98f ||
                toFocus.sqrMagnitude < 0.001f ||
                forward.sqrMagnitude < 0.001f ||
                Vector3.Dot(
                    toFocus.normalized,
                    forward.normalized) < 0.98f)
            {
                throw new InvalidOperationException(
                    "Player camera is not centered in front of Pahur.");
            }

            var fullLineupBoundsVisible =
                Corners(bounds).All(corner =>
                {
                    var view = camera.WorldToViewportPoint(corner);
                    return view.z > 0f &&
                           view.x >= -0.02f &&
                           view.x <= 1.02f &&
                           view.y >= -0.02f &&
                           view.y <= 1.02f;
                });
            if (requireFullLineupVisible &&
                !fullLineupBoundsVisible)
            {
                throw new InvalidOperationException(
                    "Player camera does not contain the full Pahur lineup.");
            }

            foreach (Transform slot in placementRoot)
            {
                var slotBounds =
                    BoundsOf(
                        slot,
                        new Bounds(slot.position, Vector3.one));
                var view =
                    camera.WorldToViewportPoint(slotBounds.center);
                if (view.z <= 0f ||
                    view.x < 0f ||
                    view.x > 1f ||
                    view.y < 0f ||
                    view.y > 1f)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " center is outside the Player camera.");
                }
            }

            var playerToCenter =
                bounds.center - player.position;
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
                    "Player root does not face the Pahur lineup.");
            }

            return fullLineupBoundsVisible;
        }

        private static float PlayerDistance(
            Bounds bounds,
            Camera camera)
        {
            var vertical =
                Mathf.Max(1f, camera.fieldOfView * 0.5f) *
                Mathf.Deg2Rad;
            var aspect =
                camera.aspect > 0.1f ? camera.aspect : 16f / 9f;
            var horizontal =
                Mathf.Atan(Mathf.Tan(vertical) * aspect);
            return Mathf.Max(
                MinimumPlayerDistance,
                bounds.extents.x /
                Mathf.Max(0.01f, Mathf.Tan(horizontal)) +
                CameraMargin,
                bounds.extents.y /
                Mathf.Max(0.01f, Mathf.Tan(vertical)) +
                CameraMargin);
        }

        private static void CopyAndImportModel()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(ModelFolder);
            var destination = Absolute(ModelPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur model folder."));
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

        private static void ScaleAndGround(
            Transform model,
            float groundY)
        {
            var bounds =
                BoundsOf(model, new Bounds(model.position, Vector3.one));
            if (bounds.size.y <= 0.00001f)
            {
                throw new InvalidOperationException(
                    "Pahur has no usable visible height.");
            }

            var scale = TargetHeight / bounds.size.y;
            if (float.IsNaN(scale) ||
                float.IsInfinity(scale) ||
                scale <= 0f ||
                scale > 1000f)
            {
                throw new InvalidOperationException(
                    "Pahur target-height scale is invalid.");
            }

            model.localScale = Vector3.one * scale;
            bounds =
                BoundsOf(model, new Bounds(model.position, Vector3.one));
            model.position +=
                Vector3.up * (groundY - bounds.min.y);
        }

        private static float LongaTergoSpacing(
            Transform longa,
            Transform tergo)
        {
            var value =
                Mathf.Abs(longa.position.z - tergo.position.z);
            if (value <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Longa/Tergo Z spacing is unusable.");
            }

            return value;
        }

        private static float RevolutionSlotSpacing(Transform root)
        {
            var first =
                root.Find(RevolutionFirstSlotName) ??
                throw new InvalidOperationException(
                    "Revolution_01 is missing.");
            var second =
                root.Find(RevolutionSecondSlotName) ??
                throw new InvalidOperationException(
                    "Revolution_02 is missing.");
            var value =
                Mathf.Abs(second.position.x - first.position.x);
            if (value <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Revolution X spacing is unusable.");
            }

            return value;
        }

        private static Renderer[] RequireVisibleGeometry(
            Transform root)
        {
            var renderers =
                root.GetComponentsInChildren<Renderer>(true)
                    .Where(item => item.enabled)
                    .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Pahur has no visible renderer.");
            }

            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer skinned &&
                    skinned.sharedMesh == null)
                {
                    throw new InvalidOperationException(
                        "Pahur skinned renderer has no mesh.");
                }

                if (renderer is MeshRenderer &&
                    renderer.GetComponent<MeshFilter>()?.sharedMesh ==
                    null)
                {
                    throw new InvalidOperationException(
                        "Pahur mesh renderer has no mesh.");
                }
            }

            return renderers;
        }

        private static void WriteInspectionReport(
            Metrics metrics,
            string sourceHash)
        {
            var report = new StringBuilder()
                .AppendLine("Pahur Placement Inspection")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("SourceFbx=" + SourcePath)
                .AppendLine("ProjectFbx=" + ModelPath)
                .AppendLine("SourceFbxSha256=" + sourceHash)
                .AppendLine("ProjectFbxSha256=" +
                            Sha256(Absolute(ModelPath)))
                .AppendLine("SourceFbxBytes=" +
                            new FileInfo(SourcePath).Length)
                .AppendLine("PlacementRoot=" + PlacementRootName)
                .AppendLine("SlotCount=" + SlotCount)
                .AppendLine("RevolutionPosition=" +
                            Vec(metrics.Revolution))
                .AppendLine("PahurPosition=" + Vec(metrics.Pahur))
                .AppendLine("LongaTergoZSpacing=" +
                            Num(metrics.ZSpacing))
                .AppendLine("RevolutionXSpacing=" +
                            Num(metrics.XSpacing))
                .AppendLine("TargetHeight=" + Num(TargetHeight))
                .AppendLine("FacingYaw=" + Num(FacingYaw))
                .AppendLine("RendererCountPerSlot=" +
                            metrics.RendererCount);
            var root = RequireRoot(PlacementRootName).transform;
            for (var index = 0; index < root.childCount; index++)
            {
                var slot = root.GetChild(index);
                report.AppendLine(
                    "Slot=" + slot.name +
                    ", LocalPosition=" + Vec(slot.localPosition) +
                    ", WorldPosition=" + Vec(slot.position) +
                    ", LocalRotation=" +
                    Quat(slot.localRotation));
            }

            report
                .AppendLine("LineupBoundsCenter=" +
                            Vec(metrics.Bounds.center))
                .AppendLine("LineupBoundsSize=" +
                            Vec(metrics.Bounds.size))
                .AppendLine("PlayerPosition=" +
                            Vec(metrics.Player))
                .AppendLine("PlayerForward=" +
                            Vec(metrics.PlayerForward))
                .AppendLine("CameraPosition=" +
                            Vec(metrics.Camera))
                .AppendLine("CameraForward=" +
                            Vec(metrics.CameraForward))
                .AppendLine("DirectFbxInstances=" + SlotCount)
                .AppendLine("Grounded=True")
                .AppendLine("FullLineupBoundsVisible=" +
                            metrics.FullLineupBoundsVisible)
                .AppendLine("AnimationApplied=False")
                .AppendLine("ModelMeshRigMaterialModified=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("SceneChangedByInspection=False")
                .AppendLine("ReviewImage=" + CapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(Absolute(InspectionPath)) ??
                throw new InvalidOperationException(
                    "Invalid Pahur inspection folder."));
            File.WriteAllText(
                Absolute(InspectionPath),
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static Bounds BoundsOf(
            Transform root,
            Bounds fallback)
        {
            var renderers =
                root.GetComponentsInChildren<Renderer>(false)
                    .Where(item =>
                        item.enabled &&
                        item.gameObject.activeInHierarchy)
                    .ToArray();
            if (renderers.Length == 0)
            {
                return fallback;
            }

            var bounds = renderers[0].bounds;
            for (var index = 1;
                 index < renderers.Length;
                 index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
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
                throw new InvalidOperationException(
                    "Invalid Pahur capture folder."));
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target =
                new RenderTexture(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
            var image =
                new Texture2D(
                    width,
                    height,
                    TextureFormat.RGB24,
                    false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0);
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
                    root.name + "|" +
                    root.activeSelf + "|" +
                    Vec(root.transform.position) + "|" +
                    Quat(root.transform.rotation) + "|" +
                    Vec(root.transform.localScale) + "|" +
                    root.transform.childCount)
                .OrderBy(
                    value => value,
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] NonPahurRootSignatures(Scene scene)
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
                .OrderBy(
                    value => value,
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] PlacementFrameSignatures(Transform root)
        {
            if (root.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "The Pahur placement must retain eleven slots.");
            }

            var signatures = new List<string>
            {
                "Root|" + root.name + "|" +
                root.gameObject.activeSelf + "|" +
                Vec(root.position) + "|" +
                Quat(root.rotation) + "|" +
                Vec(root.localScale) + "|" +
                root.childCount
            };
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = root.GetChild(index);
                signatures.Add(
                    index + "|" + slot.name + "|" +
                    slot.gameObject.activeSelf + "|" +
                    Vec(slot.localPosition) + "|" +
                    Quat(slot.localRotation) + "|" +
                    Vec(slot.localScale));
            }

            return signatures.ToArray();
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
            return
                AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException(
                    "Unity did not import the Pahur FBX as a GameObject asset.");
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerName);
            if (player != null)
            {
                return player.transform;
            }

            var controller =
                UnityEngine.Object
                    .FindFirstObjectByType<CharacterController>();
            return controller != null
                ? controller.transform
                : throw new InvalidOperationException(
                    "Player is missing.");
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
                    "The supplied Pahur FBX is missing.",
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
                    "The supplied and imported Pahur FBX hashes differ.");
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1;
                 index < parts.Length;
                 index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[index]);
                }

                current = next;
            }
        }

        private static Quaternion YawToward(
            Vector3 from,
            Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction.normalized)
                : Quaternion.identity;
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(
                    sha.ComputeHash(stream))
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
            return "(" + Num(value.x) + ", " +
                   Num(value.y) + ", " +
                   Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + ", " +
                   Num(value.y) + ", " +
                   Num(value.z) + ", " +
                   Num(value.w) + ")";
        }

        private sealed class Metrics
        {
            public Vector3 Revolution;
            public Vector3 Pahur;
            public Vector3 Player;
            public Vector3 PlayerForward;
            public Vector3 Camera;
            public Vector3 CameraForward;
            public float ZSpacing;
            public float XSpacing;
            public Bounds Bounds;
            public int RendererCount;
            public bool FullLineupBoundsVisible;
        }
    }
}
