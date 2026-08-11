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

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaCargoRunScenePlacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath =
            "D:/Bellerophon2/Bellerophon/enemies model/attas.fbx";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Ata/Models/Ata.fbx";
        // Original packed PNG restored at the FBX-relative path recorded in attas.fbx.
        private const string EmbeddedTexturePath =
            "Assets/_Project/Art/Enemies/Ata/Models/output.fbm/texture_0.png";
        private const string ImportedMaterialName = "Material_1";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string IspantRootName = "Approved Ispant Enemy Placement";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string PlayerRootName = "Player";
        private const string ModelObjectName = "Ata_Model";
        private const string ValidationFolder =
            "docs/validation/ata_placement_2026-08-11";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Ata_Placement_Diagnostic_{0:00}.png";
        private const string FinalPath =
            ValidationFolder + "/Ata_Placement_Final.png";
        private const string EmbeddedTextureFinalPath =
            ValidationFolder + "/Ata_EmbeddedTexture_Final.png";
        private const string FacingRotationFinalPath =
            ValidationFolder + "/Ata_FacingRotation_Final.png";
        private const string FacingRotationDiagnosticPath =
            ValidationFolder + "/Ata_FacingRotation_Diagnostic.png";
        private const string ExpectedSourceSha256 =
            "CF7EE9DA3D4C3C00A8F26CE2F9D71FB165043C9BF6E0407CB9503DE1F51A795D";
        private const int SlotCount = 9;
        private const float PositionTolerance = 0.003f;
        private const float GroundTolerance = 0.03f;
        // User-confirmed correction: the imported model needs a 180-degree local Y turn.
        private const float ModelFacingYawDegrees = 180f;
        // Keeps a small border around the full nine-model lineup in the actual Player camera.
        private const float ViewportMargin = 0.04f;

        private static readonly string[] SlotNames =
        {
            "Ata_01_Static",
            "Ata_02_Idle",
            "Ata_03_Move",
            "Ata_04_PistolAimAndFire",
            "Ata_05_Command",
            "Ata_06_Sabotage",
            "Ata_07_BombInstall",
            "Ata_08_Hit",
            "Ata_09_Death"
        };

        [MenuItem("Bellerophon/Enemies/Ata/Apply Embedded Texture")]
        public static void ApplyAtaEmbeddedTexture()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the Ata texture.");
            }

            RequireSourceAndImportedCopy();
            var textureAbsolutePath = Absolute(EmbeddedTexturePath);
            if (!File.Exists(textureAbsolutePath))
            {
                throw new FileNotFoundException(
                    "The extracted original Ata texture is missing.",
                    textureAbsolutePath);
            }

            AssetDatabase.ImportAsset(
                EmbeddedTexturePath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(EmbeddedTexturePath) ??
                          throw new InvalidOperationException(
                              "The extracted original Ata texture was not imported by Unity.");
            var material = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                               .OfType<Material>()
                               .FirstOrDefault(candidate =>
                                   candidate.name == ImportedMaterialName) ??
                           throw new InvalidOperationException(
                               "The imported Ata Material_1 subasset is missing.");
            if (!material.HasProperty("_BaseMap") ||
                material.GetTexture("_BaseMap") != texture)
            {
                throw new InvalidOperationException(
                    "Unity did not connect the extracted original Ata texture to Material_1.");
            }

            var ata = RequireRoot(PlacementRootName).transform;
            if (ata.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    $"Ata placement must contain {SlotCount} slots, but found {ata.childCount}.");
            }

            var rendererCount = 0;
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = ata.Find(SlotNames[index]) ??
                           throw new InvalidOperationException(
                               "Missing Ata slot: " + SlotNames[index]);
                var renderers = slot.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        slot.name + " has no renderer for the original Ata texture.");
                }

                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (var materialIndex = 0;
                         materialIndex < materials.Length;
                         materialIndex++)
                    {
                        materials[materialIndex] = material;
                    }

                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                    rendererCount++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the original Ata texture.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(ExpectedSourceSha256, Sha256(SourcePath));
            RequireSameHash(ExpectedSourceSha256, Sha256(Absolute(ModelPath)));
            Debug.Log(
                "AtaEmbeddedTextureApplied Result=PASS" +
                ", Slots=" + SlotCount +
                ", Renderers=" + rendererCount +
                ", Texture=" + EmbeddedTexturePath +
                ", TextureSize=" + texture.width + "x" + texture.height +
                ", Material=" + material.name +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Apply Facing Rotation")]
        public static void ApplyAtaFacingRotation()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before rotating Ata.");
            }

            var ata = RequireRoot(PlacementRootName).transform;
            if (ata.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    $"Ata placement must contain {SlotCount} slots, but found {ata.childCount}.");
            }

            var protectedBefore = ProtectedRootSignatures(scene);
            var player = RequirePlayer();
            var playerBefore = TransformState.Capture(player);
            var targetRotation = Quaternion.Euler(0f, ModelFacingYawDegrees, 0f);
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = ata.Find(SlotNames[index]) ??
                           throw new InvalidOperationException(
                               "Missing Ata slot: " + SlotNames[index]);
                var model = slot.Find(ModelObjectName) ??
                            throw new InvalidOperationException(
                                slot.name + " is missing its direct Ata model instance.");
                var positionBefore = model.localPosition;
                var scaleBefore = model.localScale;
                model.localRotation = targetRotation;
                if (model.localPosition != positionBefore || model.localScale != scaleBefore)
                {
                    throw new InvalidOperationException(
                        slot.name + " changed position or scale while rotating.");
                }

                EditorUtility.SetDirty(model);
            }

            RequireAtaFacingRotation(ata);
            if (!playerBefore.Matches(player) ||
                !protectedBefore.SequenceEqual(
                    ProtectedRootSignatures(scene),
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player or a scene root outside Ata changed while rotating Ata.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after rotating Ata.");
            }

            Debug.Log(
                "AtaFacingRotationApplied Result=PASS" +
                ", Slots=" + SlotCount +
                ", ModelLocalYaw=" + Num(ModelFacingYawDegrees) +
                ", PlayerUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Apply Nine Slot Placement")]
        public static void ApplyAtaNineSlotPlacement()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before placing Ata.");
            }

            RequireSourceAndImportedCopy();
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException(
                                 "The imported Ata FBX is unavailable.");
            RequireVisibleGeometry(modelAsset.transform);

            var ispant = RequireRoot(IspantRootName).transform;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var player = RequirePlayer();
            var protectedBefore = ProtectedRootSignatures(scene);
            var ispantState = TransformState.Capture(ispant);
            var longaState = TransformState.Capture(longa);
            var tergoState = TransformState.Capture(tergo);
            var playerY = player.position.y;

            var xSpacing = IspantSlotSpacing(ispant);
            var ispantCenterX = IspantLineupCenterX(ispant);
            var zSpacing = Mathf.Abs(tergo.position.z - longa.position.z);
            var zDirection = Mathf.Sign(tergo.position.z - longa.position.z);
            if (zSpacing <= PositionTolerance || Mathf.Abs(zDirection) < 0.5f)
            {
                throw new InvalidOperationException(
                    "Longa Arma and Tergo do not provide a usable Z-axis spacing direction.");
            }

            var oldRoot = GameObject.Find(PlacementRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(
                new Vector3(
                    ispantCenterX,
                    ispant.position.y,
                    ispant.position.z + zDirection * zSpacing),
                Quaternion.identity);
            root.transform.localScale = Vector3.one;

            for (var index = 0; index < SlotCount; index++)
            {
                var slot = new GameObject(SlotNames[index]);
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3(
                    (index - (SlotCount - 1) * 0.5f) * xSpacing,
                    0f,
                    0f);
                slot.transform.localRotation = Quaternion.identity;
                slot.transform.localScale = Vector3.one;

                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                            throw new InvalidOperationException(
                                "The supplied Ata FBX could not be instantiated.");
                model.name = ModelObjectName;
                model.transform.SetParent(slot.transform, false);
                model.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.Euler(0f, ModelFacingYawDegrees, 0f));
                model.transform.localScale = Vector3.one;
                AlignBottomToY(model.transform, root.transform.position.y);
                EditorUtility.SetDirty(slot);
                EditorUtility.SetDirty(model);
            }

            ConfigurePlayerStart(root.transform);
            player.position = new Vector3(player.position.x, playerY, player.position.z);
            EditorUtility.SetDirty(player);

            var metrics = InspectPlacement(scene, root.transform);
            if (!ispantState.Matches(ispant) ||
                !longaState.Matches(longa) ||
                !tergoState.Matches(tergo))
            {
                throw new InvalidOperationException(
                    "Ispant, Longa Arma, or Tergo changed during Ata placement.");
            }

            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Ata and Player changed during Ata placement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Ata placement.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(ExpectedSourceSha256, Sha256(SourcePath));
            RequireSameHash(ExpectedSourceSha256, Sha256(Absolute(ModelPath)));
            Debug.Log(
                "AtaNineSlotPlacementApplied Result=PASS" +
                ", Count=" + SlotCount +
                ", AtaCenter=" + Vec(root.transform.position) +
                ", IspantZ=" + Num(metrics.IspantZ) +
                ", LongaZ=" + Num(metrics.LongaZ) +
                ", TergoZ=" + Num(metrics.TergoZ) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", IspantXSpacing=" + Num(metrics.XSpacing) +
                ", IspantCenterX=" + Num(metrics.IspantCenterX) +
                ", AtaCenterX=" + Num(metrics.AtaCenterX) +
                ", Player=" + Vec(player.position) +
                ", PlayerFacesAta=True" +
                ", FullLineupVisible=True" +
                ", SourceSha256=" + ExpectedSourceSha256 +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Nine Slot Diagnostic")]
        public static void CaptureAtaNineSlotPlacementDiagnostic()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var ata = RequireRoot(PlacementRootName).transform;
            var ispant = RequireRoot(IspantRootName).transform;
            InspectPlacement(scene, ata);

            var combined = BoundsOf(ispant, new Bounds(ispant.position, Vector3.one));
            combined.Encapsulate(BoundsOf(ata, new Bounds(ata.position, Vector3.one)));
            var playerCamera = RequirePlayerCamera();
            var destination = NextDiagnosticPath();
            if (!destination.EndsWith("_01.png", StringComparison.Ordinal))
            {
                CaptureCamera(playerCamera, destination);
                if (scene.isDirty != wasDirty)
                {
                    throw new InvalidOperationException(
                        "Ata Player-view diagnostic changed the scene dirty state.");
                }

                Debug.Log("AtaNineSlotPlayerViewDiagnosticCaptured Result=PASS.");
                return;
            }

            var cameraObject = new GameObject("Ata Placement Diagnostic Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.CopyFrom(playerCamera);
                camera.enabled = false;
                camera.farClipPlane = Mathf.Max(playerCamera.farClipPlane, 220f);
                var direction = new Vector3(0f, 0.72f, -1f).normalized;
                var distance = FitDistance(combined, camera, 0.08f) +
                               combined.extents.z * 1.6f;
                camera.transform.position = combined.center + direction * distance;
                camera.transform.LookAt(combined.center);
                CaptureCamera(camera, destination);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata diagnostic capture changed the scene dirty state.");
            }

            Debug.Log("AtaNineSlotPlacementDiagnosticCaptured Result=PASS.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Nine Slot Final")]
        public static void CaptureAtaNineSlotPlacementFinal()
        {
            var destination = Absolute(FinalPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata placement final capture already exists: " + FinalPath);
            }

            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var ata = RequireRoot(PlacementRootName).transform;
            InspectPlacement(scene, ata);
            CaptureCamera(RequirePlayerCamera(), destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata final capture changed the scene dirty state.");
            }

            Debug.Log("AtaNineSlotPlacementFinalCaptured Result=PASS, Path=" + FinalPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Embedded Texture Final")]
        public static void CaptureAtaEmbeddedTextureFinal()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var destination = Absolute(EmbeddedTextureFinalPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata embedded texture final capture already exists: " +
                    EmbeddedTextureFinalPath);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(EmbeddedTexturePath) ??
                          throw new InvalidOperationException(
                              "The extracted original Ata texture is unavailable for capture.");
            var material = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                               .OfType<Material>()
                               .FirstOrDefault(candidate =>
                                   candidate.name == ImportedMaterialName) ??
                           throw new InvalidOperationException(
                               "The imported Ata Material_1 subasset is unavailable for capture.");
            if (!material.HasProperty("_BaseMap") ||
                material.GetTexture("_BaseMap") != texture)
            {
                throw new InvalidOperationException(
                    "The original Ata texture is not connected to Material_1.");
            }

            CaptureCamera(RequirePlayerCamera(), destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata embedded texture final capture changed the scene dirty state.");
            }

            Debug.Log(
                "AtaEmbeddedTextureFinalCaptured Result=PASS, Path=" +
                EmbeddedTextureFinalPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Facing Rotation Final")]
        public static void CaptureAtaFacingRotationFinal()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var ata = RequireRoot(PlacementRootName).transform;
            RequireAtaFacingRotation(ata);
            var destination = Absolute(FacingRotationFinalPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata facing rotation final capture already exists: " +
                    FacingRotationFinalPath);
            }

            CaptureCamera(RequirePlayerCamera(), destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata facing rotation final capture changed the scene dirty state.");
            }

            Debug.Log(
                "AtaFacingRotationFinalCaptured Result=PASS, Path=" +
                FacingRotationFinalPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Facing Rotation Diagnostic")]
        public static void CaptureAtaFacingRotationDiagnostic()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var ata = RequireRoot(PlacementRootName).transform;
            RequireAtaFacingRotation(ata);
            var destination = Absolute(FacingRotationDiagnosticPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The Ata facing rotation diagnostic capture already exists: " +
                    FacingRotationDiagnosticPath);
            }

            CaptureCamera(RequirePlayerCamera(), destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata facing rotation diagnostic capture changed the scene dirty state.");
            }

            Debug.Log(
                "AtaFacingRotationDiagnosticCaptured Result=PASS, Path=" +
                FacingRotationDiagnosticPath + ".");
        }

        private static void RequireAtaFacingRotation(Transform ata)
        {
            if (ata.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    $"Ata placement must contain {SlotCount} slots, but found {ata.childCount}.");
            }

            var targetRotation = Quaternion.Euler(0f, ModelFacingYawDegrees, 0f);
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = ata.Find(SlotNames[index]) ??
                           throw new InvalidOperationException(
                               "Missing Ata slot: " + SlotNames[index]);
                var model = slot.Find(ModelObjectName) ??
                            throw new InvalidOperationException(
                                slot.name + " is missing its direct Ata model instance.");
                if (Quaternion.Angle(model.localRotation, targetRotation) > 0.1f)
                {
                    throw new InvalidOperationException(
                        slot.name + " does not use the user-confirmed 180-degree model facing rotation.");
                }
            }
        }

        private static PlacementMetrics InspectPlacement(Scene scene, Transform ata)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must remain the active scene.");
            }

            var ispant = RequireRoot(IspantRootName).transform;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var xSpacing = IspantSlotSpacing(ispant);
            var ispantCenterX = IspantLineupCenterX(ispant);
            var zSpacing = Mathf.Abs(tergo.position.z - longa.position.z);
            var zDirection = Mathf.Sign(tergo.position.z - longa.position.z);
            var expectedZ = ispant.position.z + zDirection * zSpacing;

            if (ata.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    $"Ata placement must contain {SlotCount} slots, but found {ata.childCount}.");
            }

            if (Mathf.Abs(ata.position.z - expectedZ) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    $"Ata Z {ata.position.z:0.######} does not equal Ispant Z plus the Longa/Tergo gap {expectedZ:0.######}.");
            }

            var ataCenterX = 0f;
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = ata.Find(SlotNames[index]) ??
                           throw new InvalidOperationException(
                               "Missing Ata slot: " + SlotNames[index]);
                var expectedLocalX =
                    (index - (SlotCount - 1) * 0.5f) * xSpacing;
                if (Mathf.Abs(slot.localPosition.x - expectedLocalX) > PositionTolerance ||
                    Mathf.Abs(slot.localPosition.y) > PositionTolerance ||
                    Mathf.Abs(slot.localPosition.z) > PositionTolerance)
                {
                    throw new InvalidOperationException(
                        $"{slot.name} does not use the current Ispant X spacing.");
                }

                if (Quaternion.Angle(slot.localRotation, Quaternion.identity) > 0.1f)
                {
                    throw new InvalidOperationException(
                        slot.name + " does not preserve the source FBX front orientation.");
                }

                var model = slot.Find(ModelObjectName) ??
                            throw new InvalidOperationException(
                                slot.name + " is missing its direct Ata model instance.");
                if (Quaternion.Angle(
                        model.localRotation,
                        Quaternion.Euler(0f, ModelFacingYawDegrees, 0f)) > 0.1f)
                {
                    throw new InvalidOperationException(
                        slot.name + " does not use the user-confirmed 180-degree model facing rotation.");
                }

                RequireVisibleGeometry(model);
                var bounds = BoundsOf(model, new Bounds(model.position, Vector3.one));
                if (Mathf.Abs(bounds.min.y - ata.position.y) > GroundTolerance)
                {
                    throw new InvalidOperationException(
                        $"{slot.name} is not grounded at Ata root Y.");
                }

                ataCenterX += slot.position.x;
            }

            ataCenterX /= SlotCount;
            if (Mathf.Abs(ataCenterX - ispantCenterX) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Ata and Ispant lineup centers are not aligned on X.");
            }

            InspectPlayerFraming(ata);
            return new PlacementMetrics
            {
                IspantZ = ispant.position.z,
                LongaZ = longa.position.z,
                TergoZ = tergo.position.z,
                ZSpacing = zSpacing,
                XSpacing = xSpacing,
                IspantCenterX = ispantCenterX,
                AtaCenterX = ataCenterX
            };
        }

        private static void ConfigurePlayerStart(Transform ata)
        {
            var player = RequirePlayer();
            var camera = RequirePlayerCamera();
            var bounds = BoundsOf(ata, new Bounds(ata.position, Vector3.one));
            var cameraOffsetLocal = player.InverseTransformPoint(camera.transform.position);
            var distance = FitDistance(bounds, camera, ViewportMargin) + bounds.extents.z;

            for (var attempt = 0; attempt < 16; attempt++)
            {
                var desiredCamera = bounds.center + Vector3.back * distance;
                var yaw = YawToward(desiredCamera, bounds.center);
                var desiredPlayer = desiredCamera - yaw * cameraOffsetLocal;
                desiredPlayer.y = player.position.y;
                player.SetPositionAndRotation(desiredPlayer, yaw);
                if (BoundsFitCamera(camera, bounds, ViewportMargin))
                {
                    EditorUtility.SetDirty(player);
                    return;
                }

                distance *= 1.12f;
            }

            throw new InvalidOperationException(
                "The Player camera could not frame the complete Ata lineup from the front.");
        }

        private static void InspectPlayerFraming(Transform ata)
        {
            var player = RequirePlayer();
            var camera = RequirePlayerCamera();
            var bounds = BoundsOf(ata, new Bounds(ata.position, Vector3.one));
            var fromLineup = camera.transform.position - bounds.center;
            fromLineup.y = 0f;
            var toLineup = bounds.center - camera.transform.position;
            toLineup.y = 0f;
            var cameraForward = camera.transform.forward;
            cameraForward.y = 0f;
            if (fromLineup.sqrMagnitude < 0.001f ||
                Vector3.Dot(fromLineup.normalized, Vector3.back) < 0.98f ||
                toLineup.sqrMagnitude < 0.001f ||
                cameraForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(cameraForward.normalized, toLineup.normalized) < 0.98f ||
                Vector3.Dot(player.forward, toLineup.normalized) < 0.98f)
            {
                throw new InvalidOperationException(
                    "The Player start is not centered in front of the Ata lineup.");
            }

            if (!BoundsFitCamera(camera, bounds, ViewportMargin))
            {
                throw new InvalidOperationException(
                    "The complete Ata lineup is not visible from the Player start.");
            }
        }

        private static bool BoundsFitCamera(Camera camera, Bounds bounds, float margin)
        {
            foreach (var corner in BoundsCorners(bounds))
            {
                var viewport = camera.WorldToViewportPoint(corner);
                if (viewport.z <= 0f ||
                    viewport.x < margin || viewport.x > 1f - margin ||
                    viewport.y < margin || viewport.y > 1f - margin)
                {
                    return false;
                }
            }

            return true;
        }

        private static float FitDistance(Bounds bounds, Camera camera, float margin)
        {
            var usable = Mathf.Clamp01(1f - margin * 2f);
            var verticalHalfAngle = camera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            var horizontalHalfAngle = Mathf.Atan(
                Mathf.Tan(verticalHalfAngle) * Mathf.Max(0.01f, camera.aspect));
            var vertical = bounds.extents.y /
                           Mathf.Max(0.001f, Mathf.Tan(verticalHalfAngle) * usable);
            var horizontal = bounds.extents.x /
                             Mathf.Max(0.001f, Mathf.Tan(horizontalHalfAngle) * usable);
            return Mathf.Max(vertical, horizontal);
        }

        private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            for (var z = 0; z < 2; z++)
            {
                yield return new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
            }
        }

        private static void CaptureCamera(Camera camera, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException(
                                          "Invalid Ata capture output folder."));
            const int width = 1920;
            const int height = 1080;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(destination, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static string NextDiagnosticPath()
        {
            for (var index = 1; index <= 99; index++)
            {
                var candidate = Absolute(string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticPathFormat,
                    index));
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                "No Ata diagnostic capture slot remains.");
        }

        private static float IspantSlotSpacing(Transform ispant)
        {
            if (ispant.childCount < 2)
            {
                throw new InvalidOperationException(
                    "Ispant needs at least two slots to provide X spacing.");
            }

            var spacing = Mathf.Abs(
                ispant.GetChild(1).position.x - ispant.GetChild(0).position.x);
            if (spacing <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Ispant X spacing is unusable.");
            }

            return spacing;
        }

        private static float IspantLineupCenterX(Transform ispant)
        {
            if (ispant.childCount == 0)
            {
                throw new InvalidOperationException("Ispant has no lineup slots.");
            }

            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            for (var index = 0; index < ispant.childCount; index++)
            {
                var x = ispant.GetChild(index).position.x;
                minimum = Mathf.Min(minimum, x);
                maximum = Mathf.Max(maximum, x);
            }

            return (minimum + maximum) * 0.5f;
        }

        private static void AlignBottomToY(Transform root, float floorY)
        {
            var bounds = BoundsOf(root, new Bounds(root.position, Vector3.one));
            root.position += Vector3.up * (floorY - bounds.min.y);
        }

        private static Bounds BoundsOf(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
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

        private static void RequireVisibleGeometry(Transform root)
        {
            if (root.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderers.");
            }
        }

        private static Camera RequirePlayerCamera()
        {
            return RequirePlayer().GetComponentInChildren<Camera>(true) ??
                   throw new InvalidOperationException("The Player camera is missing.");
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerRootName) ??
                         throw new InvalidOperationException("The Player root is missing.");
            if (player.transform.parent != null)
            {
                throw new InvalidOperationException("The Player object is not a scene root.");
            }

            return player.transform;
        }

        private static GameObject RequireRoot(string name)
        {
            var root = GameObject.Find(name) ??
                       throw new InvalidOperationException(name + " is missing.");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(name + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Ata placement requires Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "The current active scene must be CargoRunMvp.");
            }

            return scene;
        }

        private static void RequireSourceAndImportedCopy()
        {
            if (!File.Exists(SourcePath))
            {
                throw new FileNotFoundException("The supplied Ata FBX is missing.", SourcePath);
            }

            var imported = Absolute(ModelPath);
            if (!File.Exists(imported))
            {
                throw new FileNotFoundException("The project Ata FBX copy is missing.", imported);
            }

            RequireSameHash(ExpectedSourceSha256, Sha256(SourcePath));
            RequireSameHash(ExpectedSourceSha256, Sha256(imported));
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != PlacementRootName && root.name != PlayerRootName)
                .Select(root =>
                    root.name + "|" +
                    root.activeSelf + "|" +
                    Vec(root.transform.localPosition) + "|" +
                    Quat(root.transform.localRotation) + "|" +
                    Vec(root.transform.localScale) + "|" +
                    root.transform.childCount.ToString(CultureInfo.InvariantCulture))
                .OrderBy(signature => signature, StringComparer.Ordinal)
                .ToArray();
        }

        private static Quaternion YawToward(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("Ata Player view direction is unusable.");
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                projectRelativePath));
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static void RequireSameHash(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Ata FBX SHA-256 mismatch. Expected=" + expected + ", Actual=" + actual);
            }
        }

        private static string Num(float value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";

        private static string Quat(Quaternion value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w) + ")";

        private sealed class PlacementMetrics
        {
            public float IspantZ { get; set; }
            public float LongaZ { get; set; }
            public float TergoZ { get; set; }
            public float ZSpacing { get; set; }
            public float XSpacing { get; set; }
            public float IspantCenterX { get; set; }
            public float AtaCenterX { get; set; }
        }

        private readonly struct TransformState
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            private TransformState(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }

            public static TransformState Capture(Transform transform)
            {
                return new TransformState(
                    transform.position,
                    transform.rotation,
                    transform.lossyScale);
            }

            public bool Matches(Transform transform)
            {
                return Vector3.Distance(position, transform.position) <= PositionTolerance &&
                       Quaternion.Angle(rotation, transform.rotation) <= 0.1f &&
                       Vector3.Distance(scale, transform.lossyScale) <= PositionTolerance;
            }
        }
    }
}
