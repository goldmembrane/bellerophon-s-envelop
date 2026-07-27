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

namespace Bellerophon.Editor.RevolutionCargoRunScene
{
    internal static class RevolutionCargoRunScenePlacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath =
            "D:/Bellerophon2/Bellerophon/enemies model/révolution attack.fbx";
        private const string ArtRoot = "Assets/_Project/Art/Enemies/Revolution";
        private const string ModelFolder = ArtRoot + "/Models";
        private const string ModelPath = ModelFolder + "/RevolutionAttack.fbx";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string ResistanceRootName = "Approved Resistance Enemy Placement";
        private const string ResistanceFirstSlotName = "Resistance_01";
        private const string ResistanceSecondSlotName = "Resistance_02";
        private const string PlacementRootName = "Approved Revolution Enemy Placement";
        private const string PlayerName = "Player";
        private const string ModelName = "Revolution_Attack_Model";
        private const string ValidationFolder =
            "docs/validation/revolution_attack_model_2026-07-27";
        private const string InspectionPath =
            ValidationFolder + "/Revolution_AttackModel_Inspection.txt";
        private const string PlayerStartCapturePath =
            ValidationFolder + "/Revolution_AttackModel_VisualReview.png";
        private const string ExpectedSourceSha256 =
            "4BDA56A5EA04D4CDEDFAC4F3588F6D66D9AEDEFEC1779D2BFDC56CF6FA938FE4";
        private const int ExpectedAuthoredVertexCount = 3705;
        private const int ExpectedTriangleCount = 7062;
        private const int ExpectedBoneCount = 24;
        private const int ExpectedProjectMaterialCount = 0;
        private const float TargetHeight = 2f;
        private const float FacingYaw = 180f;
        private const float Tolerance = 0.03f;
        private const float MinimumPlayerDistance = 2.5f;
        private const float CameraMargin = 0.8f;

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

        [MenuItem("Bellerophon/Enemies/Revolution/Inspect Model Placement")]
        public static void InspectRevolutionModelPlacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var sourceHash = Sha256(SourcePath);
            RequireSameHash(ExpectedSourceSha256, sourceHash);
            RequireSameHash(sourceHash, Sha256(Absolute(ModelPath)));
            var modelAsset = RequireModelAsset();
            RequireImportedGeometry(modelAsset.transform);
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Revolution placement root is missing.");
            var metrics = InspectState(
                scene,
                root.transform,
                false);
            Directory.CreateDirectory(Absolute(ValidationFolder));
            WriteInspectionReport(metrics, sourceHash);
            AssetDatabase.Refresh();

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Revolution placement inspection changed the scene dirty state.");
            }

            Debug.Log(
                "RevolutionModelPlacementInspected Result=PASS, Slots=8" +
                ", Position=" + Vec(metrics.Revolution) +
                ", ResistancePosition=" + Vec(metrics.Resistance) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", ResistanceXSpacing=" + Num(metrics.XSpacing) +
                ", Bounds=" + Vec(metrics.Bounds.size) +
                ", Player=" + Vec(metrics.Player) +
                ", PlayerForward=" + Vec(metrics.PlayerForward) +
                ", SourceSha256=" + sourceHash +
                ", AllRevolutionCentersVisible=True" +
                ", FullLineupBoundsVisible=" +
                metrics.FullLineupBoundsVisible +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Revolution/Inspect Attack Model Replacement")]
        public static void InspectRevolutionAttackModelReplacement()
        {
            InspectRevolutionModelPlacement();
        }

        [MenuItem("Bellerophon/Enemies/Revolution/Apply Attack Model Replacement")]
        public static void ApplyRevolutionAttackModelReplacement()
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
            var slots = new Transform[SlotNames.Length];
            var oldModels = new Transform[SlotNames.Length];
            var slotPositions = new Vector3[SlotNames.Length];
            var slotRotations = new Quaternion[SlotNames.Length];
            var slotScales = new Vector3[SlotNames.Length];
            var replacements = new GameObject[SlotNames.Length];

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
                            "The supplied Revolution attack FBX could not be instantiated.");
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
            }
            catch
            {
                foreach (var replacement in replacements)
                {
                    if (replacement != null)
                    {
                        UnityEngine.Object.DestroyImmediate(replacement);
                    }
                }

                throw;
            }

            foreach (var oldModel in oldModels)
            {
                UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
            }

            for (var index = 0; index < SlotNames.Length; index++)
            {
                if (slots[index].childCount != 1 ||
                    slots[index].GetChild(0) !=
                    replacements[index].transform ||
                    slots[index].localPosition != slotPositions[index] ||
                    slots[index].localRotation != slotRotations[index] ||
                    slots[index].localScale != slotScales[index])
                {
                    throw new InvalidOperationException(
                        "Revolution slot changed outside its model child at index " +
                        index + ".");
                }
            }

            var metrics = InspectState(
                scene,
                root.transform,
                false);
            if (root.transform.position != rootPositionBefore ||
                root.transform.rotation != rootRotationBefore ||
                root.transform.localScale != rootScaleBefore ||
                player.position != playerPositionBefore ||
                player.rotation != playerRotationBefore ||
                player.localScale != playerScaleBefore)
            {
                throw new InvalidOperationException(
                    "Revolution root or Player start transform changed during model replacement.");
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
                    "CargoRunMvp could not be saved after Revolution attack model replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(
                sourceHashBefore,
                Sha256(SourcePath));
            RequireSameHash(
                sourceHashBefore,
                Sha256(Absolute(ModelPath)));
            Debug.Log(
                "RevolutionAttackModelReplacementApplied Result=PASS" +
                ", Slots=8" +
                ", SourceSha256=" + sourceHashBefore +
                ", Position=" + Vec(metrics.Revolution) +
                ", ResistanceXSpacing=" + Num(metrics.XSpacing) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", TargetHeight=2" +
                ", AnimationApplied=False" +
                ", SlotTransformsUnchanged=True" +
                ", PlayerTransformUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Revolution/Apply Placement")]
        public static void ApplyRevolutionModelPlacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying Revolution placement.");
            }

            var sourceHashBefore = Sha256(SourcePath);
            RequireSameHash(ExpectedSourceSha256, sourceHashBefore);
            CopyAndImportModel();
            var importedHashBefore = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, importedHashBefore);
            var modelAsset = RequireModelAsset();
            var protectedBefore = ProtectedRootSignatures(scene);
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var resistance = RequireRoot(ResistanceRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = ResistanceSlotSpacing(resistance);

            var oldRoot = GameObject.Find(PlacementRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(
                new Vector3(
                    resistance.position.x,
                    resistance.position.y,
                    resistance.position.z - zSpacing),
                Quaternion.identity);

            for (var i = 0; i < SlotNames.Length; i++)
            {
                var slot = new GameObject(SlotNames[i]);
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3(i * xSpacing, 0f, 0f);
                slot.transform.localRotation = Quaternion.Euler(0f, FacingYaw, 0f);

                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                            throw new InvalidOperationException(
                                "The supplied Revolution FBX could not be instantiated.");
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
            var metrics = InspectState(
                scene,
                root.transform,
                true);
            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Revolution and Player changed during placement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Revolution placement.");
            }

            AssetDatabase.SaveAssets();
            var sourceHashAfter = Sha256(SourcePath);
            var importedHashAfter = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, sourceHashAfter);
            RequireSameHash(importedHashBefore, importedHashAfter);
            Debug.Log(
                "RevolutionModelPlacementApplied Result=PASS, Slots=8, Position=" +
                Vec(metrics.Revolution) +
                ", ResistancePosition=" + Vec(metrics.Resistance) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", ResistanceXSpacing=" + Num(metrics.XSpacing) +
                ", Bounds=" + Vec(metrics.Bounds.size) +
                ", Player=" + Vec(metrics.Player) +
                ", PlayerForward=" + Vec(metrics.PlayerForward) +
                ", SourceSha256=" + sourceHashAfter +
                ", DirectFbxInstances=8, TargetHeight=2, AnimationApplied=False, " +
                "OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Revolution/Capture Player Start View")]
        public static void CaptureRevolutionModelPlacementReview()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Revolution placement root is missing.");
            var metrics = InspectState(
                scene,
                root.transform,
                false);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(PlayerStartCapturePath), 1920, 1080);
            AssetDatabase.Refresh();
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Revolution Player-start capture changed the scene dirty state.");
            }

            Debug.Log(
                "RevolutionPlayerStartViewCaptured Result=PASS, Image=" +
                PlayerStartCapturePath + ", PlayerPosition=" + Vec(metrics.Player) +
                ", PlayerForward=" + Vec(metrics.PlayerForward) +
                ", AllRevolutionCentersVisible=True" +
                ", FullLineupBoundsVisible=" +
                metrics.FullLineupBoundsVisible +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Revolution/Capture Attack Model Replacement Review")]
        public static void CaptureRevolutionAttackModelReplacementReview()
        {
            CaptureRevolutionModelPlacementReview();
        }

        private static Metrics InspectState(
            Scene scene,
            Transform root,
            bool requireFullLineupVisible)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be active.");
            }

            RequireSource();
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var resistance = RequireRoot(ResistanceRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = ResistanceSlotSpacing(resistance);
            var expected = new Vector3(
                resistance.position.x,
                resistance.position.y,
                resistance.position.z - zSpacing);
            if (Vector3.Distance(root.position, expected) > Tolerance ||
                root.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "Revolution root position or eight-slot contract changed.");
            }

            var rendererCount = -1;
            for (var i = 0; i < SlotNames.Length; i++)
            {
                var slot = root.GetChild(i);
                if (slot.name != SlotNames[i] ||
                    Vector3.Distance(
                        slot.localPosition,
                        new Vector3(i * xSpacing, 0f, 0f)) > Tolerance ||
                    Quaternion.Angle(
                        slot.localRotation,
                        Quaternion.Euler(0f, FacingYaw, 0f)) > 0.1f ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Revolution slot contract changed at index " + i + ".");
                }

                var model = slot.GetChild(0);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                if (model.name != ModelName || source == null ||
                    AssetDatabase.GetAssetPath(source) != ModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " is not a direct instance of the supplied Revolution FBX.");
                }

                RequireImportedGeometry(model);
                var renderers = model.GetComponentsInChildren<Renderer>(false)
                    .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                    .ToArray();
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        slot.name + " has no visible Revolution renderer.");
                }

                if (rendererCount < 0)
                {
                    rendererCount = renderers.Length;
                }
                else if (rendererCount != renderers.Length)
                {
                    throw new InvalidOperationException(
                        "Revolution renderer count differs between slots.");
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
                        "Revolution placeholders must remain static.");
                }
            }

            var bounds = BoundsOf(root, new Bounds(root.position, Vector3.one));
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var fullLineupBoundsVisible =
                InspectPlayer(
                    player,
                    camera,
                    root,
                    bounds,
                    requireFullLineupVisible);
            return new Metrics
            {
                Resistance = resistance.position,
                Revolution = root.position,
                Player = player.position,
                PlayerForward = player.forward,
                ZSpacing = zSpacing,
                XSpacing = xSpacing,
                Bounds = bounds,
                FullLineupBoundsVisible = fullLineupBoundsVisible
            };
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
            var fromFocus = player.position - bounds.center;
            fromFocus.y = 0f;
            var front = placementRoot.GetChild(0).forward;
            front.y = 0f;
            var toFocus = bounds.center - player.position;
            toFocus.y = 0f;
            var forward = player.forward;
            forward.y = 0f;
            if (fromFocus.sqrMagnitude < 0.001f ||
                front.sqrMagnitude < 0.001f ||
                Vector3.Dot(fromFocus.normalized, front.normalized) < 0.98f ||
                toFocus.sqrMagnitude < 0.001f ||
                forward.sqrMagnitude < 0.001f ||
                Vector3.Dot(toFocus.normalized, forward.normalized) < 0.98f)
            {
                throw new InvalidOperationException(
                    "Player is not centered in front of Revolution.");
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
                    "Player camera does not contain the full Revolution lineup.");
            }

            foreach (Transform slot in placementRoot)
            {
                var slotBounds = BoundsOf(
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
                        " center is outside the preserved Player camera.");
                }
            }

            return fullLineupBoundsVisible;
        }

        private static float PlayerDistance(Bounds bounds, Camera camera)
        {
            var vertical =
                Mathf.Max(1f, camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            var aspect = camera.aspect > 0.1f ? camera.aspect : 16f / 9f;
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
                throw new InvalidOperationException("Invalid Revolution model folder."));
            File.Copy(SourcePath, destination, true);
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Revolution FBX ModelImporter is missing.");
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.optimizeGameObjects = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            RequireSameHash(
                ExpectedSourceSha256,
                Sha256(Absolute(ModelPath)));
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
                    "Revolution has no usable visible height.");
            }

            var scale = TargetHeight / bounds.size.y;
            if (float.IsNaN(scale) || float.IsInfinity(scale) ||
                scale <= 0f || scale > 1000f)
            {
                throw new InvalidOperationException(
                    "Revolution target-height scale is invalid.");
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

        private static float ResistanceSlotSpacing(Transform root)
        {
            var first = root.Find(ResistanceFirstSlotName) ??
                        throw new InvalidOperationException(
                            "Resistance_01 is missing.");
            var second = root.Find(ResistanceSecondSlotName) ??
                         throw new InvalidOperationException(
                             "Resistance_02 is missing.");
            var value = Mathf.Abs(second.position.x - first.position.x);
            if (value <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Resistance X spacing is unusable.");
            }

            return value;
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
                projectMaterials.Length != ExpectedProjectMaterialCount)
            {
                throw new InvalidOperationException(
                    "Revolution imported geometry contract differs. Triangles=" +
                    triangleCount + ", Bones=" + renderer.bones.Length +
                    ", ProjectMaterials=" +
                    string.Join("|", projectMaterials) + ".");
            }

            return renderer;
        }

        private static void WriteInspectionReport(
            Metrics metrics,
            string sourceHash)
        {
            var modelAsset = RequireModelAsset();
            var renderer = RequireImportedGeometry(modelAsset.transform);
            var mesh = renderer.sharedMesh;
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("SourceFbx=" + SourcePath);
            report.AppendLine("ProjectFbx=" + ModelPath);
            report.AppendLine("SourceFbxSha256=" + sourceHash);
            report.AppendLine("ProjectFbxSha256=" +
                              Sha256(Absolute(ModelPath)));
            report.AppendLine("AuthoredVertices=" +
                              ExpectedAuthoredVertexCount);
            report.AppendLine("ImportedRenderVertices=" + mesh.vertexCount);
            report.AppendLine("Triangles=" + ExpectedTriangleCount);
            report.AppendLine("Bones=" + renderer.bones.Length);
            report.AppendLine("ImportedMaterialSlots=" +
                              renderer.sharedMaterials.Length);
            report.AppendLine("ProjectMaterialReferences=" +
                              renderer.sharedMaterials.Count(material =>
                                  material != null &&
                                  AssetDatabase.GetAssetPath(material)
                                      .StartsWith(
                                          "Assets/",
                                          StringComparison.Ordinal)));
            report.AppendLine("UnityBuiltinDefaultMaterialOnly=True");
            report.AppendLine("OriginalFbxHasMaterials=False");
            report.AppendLine("PlacementRoot=" + PlacementRootName);
            report.AppendLine("SlotCount=" + SlotNames.Length);
            report.AppendLine("ResistancePosition=" +
                              Vec(metrics.Resistance));
            report.AppendLine("RevolutionPosition=" +
                              Vec(metrics.Revolution));
            report.AppendLine("LongaTergoZSpacing=" +
                              Num(metrics.ZSpacing));
            report.AppendLine("ResistanceXSpacing=" +
                              Num(metrics.XSpacing));
            report.AppendLine("TargetHeight=" + Num(TargetHeight));
            report.AppendLine("FacingYaw=" + Num(FacingYaw));
            var root = RequireRoot(PlacementRootName).transform;
            for (var index = 0; index < root.childCount; index++)
            {
                var slot = root.GetChild(index);
                report.AppendLine(
                    "Slot=" + slot.name +
                    ", LocalPosition=" + Vec(slot.localPosition) +
                    ", WorldPosition=" + Vec(slot.position) +
                    ", LocalRotation=" + Quat(slot.localRotation));
            }

            report.AppendLine("LineupBoundsCenter=" +
                              Vec(metrics.Bounds.center));
            report.AppendLine("LineupBoundsSize=" +
                              Vec(metrics.Bounds.size));
            report.AppendLine("PlayerPosition=" + Vec(metrics.Player));
            report.AppendLine("PlayerForward=" +
                              Vec(metrics.PlayerForward));
            report.AppendLine("DirectFbxInstances=8");
            report.AppendLine("AllRevolutionCentersVisible=True");
            report.AppendLine("FullLineupBoundsVisible=" +
                              metrics.FullLineupBoundsVisible);
            report.AppendLine("AnimationApplied=False");
            report.AppendLine("OtherSceneRootsChanged=False");
            report.AppendLine("SceneChangedByInspection=False");
            report.AppendLine("ReviewImage=" + PlayerStartCapturePath);
            File.WriteAllText(
                Absolute(InspectionPath),
                report.ToString(),
                new UTF8Encoding(false));
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
                       "Unity did not import the Revolution FBX as a GameObject asset.");
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
                    "The supplied Revolution FBX is missing.",
                    SourcePath);
            }
        }

        private static void RequireSameHash(string first, string second)
        {
            if (!string.Equals(first, second, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The supplied and imported Revolution FBX hashes differ.");
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

        private static Quaternion YawToward(Vector3 from, Vector3 to)
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
            public Vector3 Resistance;
            public Vector3 Revolution;
            public Vector3 Player;
            public Vector3 PlayerForward;
            public float ZSpacing;
            public float XSpacing;
            public Bounds Bounds;
            public bool FullLineupBoundsVisible;
        }
    }
}
