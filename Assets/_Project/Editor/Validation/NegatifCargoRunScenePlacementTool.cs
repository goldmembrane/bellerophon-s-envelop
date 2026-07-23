using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.NegatifCargoRunScene
{
    internal static class NegatifCargoRunScenePlacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath = "D:/Bellerophon2/Bellerophon/enemies model/négatif.fbx";
        private const string ArtRoot = "Assets/_Project/Art/Enemies/Negatif";
        private const string ModelFolder = ArtRoot + "/Models";
        private const string ModelPath = ModelFolder + "/Negatif.fbx";
        private const string ApprovedModelPath = ModelFolder + "/Negatif_ApprovedAppearance.fbx";
        private const string ApprovedGlbModelPath =
            ModelFolder + "/Negatif_Glb_ApprovedAppearance.glb";
        private const string SourceGlbPath =
            "D:/Bellerophon2/Bellerophon/enemies model/négatif.glb";
        private const string ApprovedGlbSamplePath =
            "artSample/enemies/negatif/glb_appearance_sync/exports/Negatif_Glb_AppearanceSync.glb";
        private const string ApprovedGlbSampleApprovalPath =
            "artSample/enemies/negatif/glb_appearance_sync/APPROVAL_STATUS.json";
        private const string SourceGlbHash =
            "BD27F171F84E212273C841A2BAE44832519CEA0F0201C0D3FBD5707F870CE8E8";
        private const string ApprovedGlbHash =
            "0E19530D01B15E79DFDF23D9B951C5E5B8C72496C568584CBCCEEA7C6A41AC60";
        private const string TextureFolder = ArtRoot + "/Textures";
        private const string MaterialFolder = ArtRoot + "/Materials";
        private const string ApprovedShaderPath =
            ArtRoot + "/Shaders/NegatifApprovedAppearance.shader";
        private const string ApprovedShaderName = "Bellerophon/Negatif/ApprovedAppearance";
        private const string SampleApprovalPath =
            "artSample/enemies/negatif/appearance_reference_sync/APPROVAL_STATUS.json";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string DoloreRootName = "Approved Dolore Enemy Placement";
        private const string DoloreFirstSlotName = "Dolore_01_Static_Review";
        private const string DoloreSecondSlotName = "Dolore_02_Idle";
        private const string PlacementRootName = "Approved Negatif Enemy Placement";
        private const string PlayerName = "Player";
        private const string ModelName = "Negatif_Model";
        private const string PlayerStartCapturePath = "Logs/Negatif_PlayerStartView.png";
        private const string ApprovedAppearanceCapturePath =
            "Logs/Negatif_ApprovedAppearance_Unity.png";
        private const string ApprovedGlbAppearanceCapturePath =
            "Logs/Negatif_Glb_ApprovedAppearance_Unity.png";
        private const float TargetHeight = 0.5f;
        private const float Tolerance = 0.03f;
        private const float MinimumPlayerDistance = 6f;
        private const float CameraMargin = 1.5f;

        private static readonly string[] SlotNames =
        {
            "Negatif_00_Static_Review",
            "Negatif_01_Idle",
            "Negatif_02_Move",
            "Negatif_03_Claw_Attack",
            "Negatif_04_Hit_Reaction",
            "Negatif_05_Flee",
            "Negatif_06_Death"
        };

        // These values are copied from the approved Blender sample's
        // MATERIAL_SPECS and are the Unity-side material conversion contract.
        private static readonly ApprovedMaterialSpec[] ApprovedMaterialSpecs =
        {
            new ApprovedMaterialSpec(
                "Negatif_Worn_Bronze", "negatif_worn_bronze", 0.84f, 0.38f,
                Color.white, true, Color.black, 0f),
            new ApprovedMaterialSpec(
                "Negatif_Dark_Mechanism", "negatif_dark_mechanism", 0.98f, 0.20f,
                Color.white, true, Color.black, 0f),
            new ApprovedMaterialSpec(
                "Negatif_Canvas_Sack", "negatif_canvas", 0.02f, 0.42f,
                Color.white, true, Color.black, 0f),
            new ApprovedMaterialSpec(
                "Negatif_Leather_Strap", "negatif_leather", 0.01f, 0.72f,
                new Color(0.7228f, 0.6157f, 0.59176f, 1f), true,
                Color.black, 0f),
            new ApprovedMaterialSpec(
                "Negatif_Copper_Accent", "negatif_copper_accent", 0.76f, 0.13f,
                Color.white, true, Color.black, 0f),
            new ApprovedMaterialSpec(
                "Negatif_Amber_Eye", "negatif_amber_eye", 0.12f, 0.05f,
                new Color(1f, 0.10f, 0.002f, 1f), false,
                new Color(1f, 0.18f, 0.004f, 1f), 9f)
        };

        [MenuItem("Bellerophon/Enemies/Negatif/Apply Placement")]
        public static void ApplyPlacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying Negatif placement.");
            }

            var sourceHashBefore = Sha256(SourcePath);
            CopyAndImportModel();
            var importedHashBefore = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, importedHashBefore);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException("The imported Negatif FBX is missing.");
            var protectedBefore = ProtectedRootSignatures(scene);
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var dolore = RequireRoot(DoloreRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = DoloreSlotSpacing(dolore);

            var oldRoot = GameObject.Find(PlacementRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(NegatifPosition(dolore, zSpacing), Quaternion.identity);

            for (var i = 0; i < SlotNames.Length; i++)
            {
                var slot = new GameObject(SlotNames[i]);
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3(i * xSpacing, 0f, 0f);
                slot.transform.localRotation = Quaternion.identity;

                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                            throw new InvalidOperationException("The supplied Negatif FBX could not be instantiated.");
                model.name = ModelName;
                model.transform.SetParent(slot.transform, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = Vector3.one;
                ConfigureStaticModel(model.transform);
                AlignVisualFront(slot.transform, model.transform);
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
                    "A scene root outside Negatif and Player changed during placement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after Negatif placement.");
            }

            AssetDatabase.SaveAssets();
            var sourceHashAfter = Sha256(SourcePath);
            var importedHashAfter = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHashBefore, sourceHashAfter);
            RequireSameHash(importedHashBefore, importedHashAfter);
            Debug.Log(
                "NegatifPlacementApplied Result=PASS, Slots=7, Position=" + Vec(metrics.Negatif) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", XSpacing=" + Num(metrics.XSpacing) +
                ", Bounds=" + Vec(metrics.Bounds.size) +
                ", Player=" + Vec(metrics.Player) +
                ", SourceHashPreserved=True, OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Inspect Applied Placement")]
        public static void InspectAppliedPlacement()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Negatif placement root is missing.");
            var metrics = InspectState(scene, root.transform);
            RequireSameHash(Sha256(SourcePath), Sha256(Absolute(ModelPath)));
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("Negatif inspection changed the scene dirty state.");
            }

            Debug.Log(
                "NegatifPlacementInspected Result=PASS, Slots=7, DirectFbxInstances=7, " +
                "AnimationApplied=False, PlayerFacesNegatif=True, AllNegatifVisible=True, " +
                "Position=" + Vec(metrics.Negatif) +
                ", Bounds=" + Vec(metrics.Bounds.size) +
                ", VisualFront=" + Vec(metrics.VisualFront) +
                ", Materials=" + string.Join("|", metrics.MaterialNames) + ".");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Capture Player Start View")]
        public static void CapturePlayerStartView()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Negatif placement root is missing.");
            var metrics = InspectState(scene, root.transform);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(PlayerStartCapturePath), 1920, 1080);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("Negatif capture changed the scene dirty state.");
            }

            Debug.Log(
                "NegatifPlayerStartViewCaptured Result=PASS, Image=" + PlayerStartCapturePath +
                ", Player=" + Vec(metrics.Player) +
                ", PlayerForward=" + Vec(metrics.PlayerForward) +
                ", Bounds=" + Vec(metrics.Bounds.size) + ".");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Apply Approved Appearance")]
        public static void ApplyApprovedAppearance()
        {
            RequireSampleApproval();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the approved Negatif appearance.");
            }

            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Negatif placement root is missing.");
            var protectedBefore = AppearanceProtectedRootSignatures(scene);
            var rootTransformBefore = TransformSignature(root.transform);
            var slotTransformsBefore = root.transform.Cast<Transform>()
                .Select(TransformSignature)
                .ToArray();
            var sourceHashBefore = Sha256(SourcePath);
            var approvedModelHashBefore = Sha256(Absolute(ApprovedModelPath));
            var approvedMaterials = PrepareApprovedAssets();
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedModelPath) ??
                             throw new InvalidOperationException("The approved Negatif FBX is missing.");

            for (var i = 0; i < SlotNames.Length; i++)
            {
                var slot = root.transform.GetChild(i);
                if (slot.name != SlotNames[i] || slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Negatif slot contract changed before appearance application at index " + i + ".");
                }

                var oldModel = slot.GetChild(0);
                var desiredFront = ModelVisualForward(oldModel);
                UnityEngine.Object.DestroyImmediate(oldModel.gameObject);

                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                            throw new InvalidOperationException(
                                "The approved Negatif FBX could not be instantiated.");
                model.name = ModelName;
                model.transform.SetParent(slot, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = Vector3.one;
                ConfigureStaticModel(model.transform);
                AlignModelVisualFront(model.transform, desiredFront);
                ScaleAndGround(model.transform, root.transform.position.y);
                AssignApprovedMaterials(model.transform, approvedMaterials);
                EditorUtility.SetDirty(model);
            }

            if (rootTransformBefore != TransformSignature(root.transform) ||
                !slotTransformsBefore.SequenceEqual(
                    root.transform.Cast<Transform>().Select(TransformSignature),
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Negatif root or slot transforms changed while applying the approved appearance.");
            }

            var protectedAfter = AppearanceProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside the Negatif placement changed during appearance application.");
            }

            var metrics = InspectState(scene, root.transform, ApprovedModelPath);
            var inspection = InspectApprovedAppearanceAssets(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the approved Negatif appearance.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(sourceHashBefore, Sha256(SourcePath));
            RequireSameHash(approvedModelHashBefore, Sha256(Absolute(ApprovedModelPath)));
            Debug.Log(
                "NegatifApprovedAppearanceApplied Result=PASS, Slots=7, ApprovedModelInstances=" +
                inspection.ModelCount + ", Materials=" + inspection.MaterialCount +
                ", TrianglesPerModel=" + inspection.TrianglesPerModel +
                ", BonesPerModel=" + inspection.BonesPerModel +
                ", Position=" + Vec(metrics.Negatif) +
                ", Bounds=" + Vec(metrics.Bounds.size) +
                ", RootAndSlotTransformsUnchanged=True, OtherSceneRootsUnchanged=True, " +
                "SourceHashPreserved=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Inspect Approved Appearance")]
        public static void InspectApprovedAppearance()
        {
            RequireSampleApproval();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Negatif placement root is missing.");
            var metrics = InspectState(scene, root.transform, ApprovedModelPath);
            var inspection = InspectApprovedAppearanceAssets(root.transform);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Negatif appearance inspection changed the scene dirty state.");
            }

            Debug.Log(
                "NegatifApprovedAppearanceInspected Result=PASS, Slots=7, ApprovedModelInstances=" +
                inspection.ModelCount + ", Materials=" + inspection.MaterialCount +
                ", TrianglesPerModel=" + inspection.TrianglesPerModel +
                ", BonesPerModel=" + inspection.BonesPerModel +
                ", Height=" + Num(metrics.Bounds.size.y) +
                ", RootAndPlacementPreserved=True, Shader=" + ApprovedShaderName + ".");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Capture Approved Appearance")]
        public static void CaptureApprovedAppearance()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Negatif placement root is missing.");
            InspectState(scene, root.transform, ApprovedModelPath);
            InspectApprovedAppearanceAssets(root.transform);

            var sourceCamera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("The Player camera is missing.");
            var firstModel = root.transform.GetChild(0).GetChild(0);
            var bounds = BoundsOf(firstModel, new Bounds(firstModel.position, Vector3.one));
            var front = ModelVisualForward(firstModel);
            var right = Vector3.Cross(Vector3.up, front).normalized;
            var distance = Mathf.Max(1.1f, bounds.size.magnitude * 2.4f);
            var cameraObject = new GameObject(
                "NegatifApprovedAppearanceCaptureCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.fieldOfView = 38f;
                camera.aspect = 16f / 9f;
                camera.transform.position =
                    bounds.center + front * distance + right * distance * 0.34f +
                    Vector3.up * bounds.extents.y * 0.35f;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
                Capture(
                    camera,
                    Absolute(ApprovedAppearanceCapturePath),
                    1920,
                    1080);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Negatif appearance capture changed the scene dirty state.");
            }

            Debug.Log(
                "NegatifApprovedAppearanceCaptured Result=PASS, Image=" +
                ApprovedAppearanceCapturePath +
                ", Model=Negatif_00_Static_Review, SceneUnchanged=True.");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Apply Approved GLB Appearance")]
        public static void ApplyApprovedGlbAppearance()
        {
            RequireApprovedGlbSample();
            RequireApprovedGlbHashes();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the approved Negatif GLB appearance.");
            }

            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Negatif placement root is missing.");
            var protectedBefore = AppearanceProtectedRootSignatures(scene);
            var rootTransformBefore = TransformSignature(root.transform);
            var slotTransformsBefore = root.transform.Cast<Transform>()
                .Select(TransformSignature)
                .ToArray();
            var materials = PrepareApprovedGlbAssets();
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedGlbModelPath) ??
                             throw new InvalidOperationException(
                                 "Unity did not import the approved Negatif GLB as a GameObject asset.");

            for (var i = 0; i < SlotNames.Length; i++)
            {
                var slot = root.transform.GetChild(i);
                if (slot.name != SlotNames[i] || slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Negatif slot contract changed before GLB replacement at index " + i + ".");
                }

                var oldModel = slot.GetChild(0);
                var desiredFront = CurrentModelVisualForward(oldModel);
                UnityEngine.Object.DestroyImmediate(oldModel.gameObject);

                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                            throw new InvalidOperationException(
                                "The approved Negatif GLB could not be instantiated.");
                model.name = ModelName;
                model.transform.SetParent(slot, false);
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = Vector3.one;
                ConfigureStaticModel(model.transform);
                AlignModelVisualFront(model.transform, desiredFront, ApprovedGlbVisualForward);
                ScaleAndGround(model.transform, root.transform.position.y);
                AssignApprovedGlbMaterials(model.transform, materials);
                EditorUtility.SetDirty(model);
            }

            if (rootTransformBefore != TransformSignature(root.transform) ||
                !slotTransformsBefore.SequenceEqual(
                    root.transform.Cast<Transform>().Select(TransformSignature),
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Negatif root or slot transforms changed during approved GLB replacement.");
            }

            var protectedAfter = AppearanceProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside the Negatif placement changed during approved GLB replacement.");
            }

            var metrics = InspectState(
                scene,
                root.transform,
                ApprovedGlbModelPath,
                ApprovedGlbVisualForward,
                false);
            var inspection = InspectApprovedGlbAppearanceAssets(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after approved Negatif GLB replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireApprovedGlbHashes();
            Debug.Log(
                "NegatifApprovedGlbAppearanceApplied Result=PASS, Slots=7, DirectGlbInstances=" +
                inspection.ModelCount + ", RenderersPerModel=" + inspection.RenderersPerModel +
                ", EyesPerModel=" + inspection.EyesPerModel +
                ", TrianglesPerModel=" + inspection.TrianglesPerModel +
                ", BonesPerModel=" + inspection.BonesPerModel +
                ", Position=" + Vec(metrics.Negatif) +
                ", Bounds=" + Vec(metrics.Bounds.size) +
                ", RootAndSlotTransformsUnchanged=True, OtherSceneRootsUnchanged=True, " +
                "SourceAndApprovedGlbHashesPreserved=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Inspect Approved GLB Appearance")]
        public static void InspectApprovedGlbAppearance()
        {
            RequireApprovedGlbSample();
            RequireApprovedGlbHashes();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Negatif placement root is missing.");
            var metrics = InspectState(
                scene,
                root.transform,
                ApprovedGlbModelPath,
                ApprovedGlbVisualForward,
                false);
            var inspection = InspectApprovedGlbAppearanceAssets(root.transform);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Negatif GLB inspection changed the scene dirty state.");
            }

            Debug.Log(
                "NegatifApprovedGlbAppearanceInspected Result=PASS, Slots=7, DirectGlbInstances=" +
                inspection.ModelCount + ", RenderersPerModel=" + inspection.RenderersPerModel +
                ", EyesPerModel=" + inspection.EyesPerModel +
                ", TrianglesPerModel=" + inspection.TrianglesPerModel +
                ", BonesPerModel=" + inspection.BonesPerModel +
                ", Materials=" + ApprovedMaterialSpecs.Length +
                ", Height=" + Num(metrics.Bounds.size.y) +
                ", RootAndPlacementPreserved=True, Shader=" + ApprovedShaderName +
                ", SourceAndApprovedGlbHashesPreserved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Negatif/Capture Approved GLB Appearance")]
        public static void CaptureApprovedGlbAppearance()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException("The Negatif placement root is missing.");
            InspectState(
                scene,
                root.transform,
                ApprovedGlbModelPath,
                ApprovedGlbVisualForward,
                false);
            InspectApprovedGlbAppearanceAssets(root.transform);

            var sourceCamera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException("The Player camera is missing.");
            var firstModel = root.transform.GetChild(0).GetChild(0);
            var bounds = BoundsOf(firstModel, new Bounds(firstModel.position, Vector3.one));
            var front = ApprovedGlbVisualForward(firstModel);
            var right = Vector3.Cross(Vector3.up, front).normalized;
            var distance = Mathf.Max(1.1f, bounds.size.magnitude * 2.4f);
            var cameraObject = new GameObject(
                "NegatifApprovedGlbAppearanceCaptureCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.fieldOfView = 38f;
                camera.aspect = 16f / 9f;
                camera.transform.position =
                    bounds.center + front * distance + right * distance * 0.34f +
                    Vector3.up * bounds.extents.y * 0.35f;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
                Capture(
                    camera,
                    Absolute(ApprovedGlbAppearanceCapturePath),
                    1920,
                    1080);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Negatif GLB capture changed the scene dirty state.");
            }

            Debug.Log(
                "NegatifApprovedGlbAppearanceCaptured Result=PASS, Image=" +
                ApprovedGlbAppearanceCapturePath +
                ", Model=Negatif_00_Static_Review, SceneUnchanged=True.");
        }

        private static Metrics InspectState(
            Scene scene,
            Transform root,
            string expectedModelPath = ModelPath,
            Func<Transform, Vector3> visualForward = null,
            bool requireLegacySource = true)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be the current active scene.");
            }

            if (requireLegacySource)
            {
                RequireSource();
            }

            var resolveVisualForward = visualForward ?? ModelVisualForward;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var dolore = RequireRoot(DoloreRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = DoloreSlotSpacing(dolore);
            var expectedRoot = NegatifPosition(dolore, zSpacing);
            if (Vector3.Distance(root.position, expectedRoot) > Tolerance ||
                root.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException("Negatif root position or seven-slot contract changed.");
            }

            var materialNames = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < SlotNames.Length; i++)
            {
                var slot = root.GetChild(i);
                if (slot.name != SlotNames[i] ||
                    Vector3.Distance(slot.localPosition, new Vector3(i * xSpacing, 0f, 0f)) > Tolerance ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException("Negatif slot contract changed at index " + i + ".");
                }

                var model = slot.GetChild(0);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                if (model.name != ModelName || source == null ||
                    AssetDatabase.GetAssetPath(source) != expectedModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name + " is not a direct instance of the expected Negatif FBX.");
                }

                var renderers = model.GetComponentsInChildren<Renderer>(false)
                    .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                    .ToArray();
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(slot.name + " has no visible renderer.");
                }

                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null)
                        {
                            throw new InvalidOperationException(slot.name + " has a missing material.");
                        }

                        if (material.shader == null ||
                            material.shader.name.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            throw new InvalidOperationException(slot.name + " has an invalid material shader.");
                        }

                        materialNames.Add(material.name);
                    }
                }

                var modelBounds = BoundsOf(model, new Bounds(model.position, Vector3.one));
                if (Mathf.Abs(modelBounds.size.y - TargetHeight) > Tolerance ||
                    Mathf.Abs(modelBounds.min.y - root.position.y) > Tolerance)
                {
                    throw new InvalidOperationException(slot.name + " height or ground alignment changed.");
                }

                if (model.GetComponentsInChildren<Animator>(true).Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true).Any(item => item.enabled))
                {
                    throw new InvalidOperationException("Negatif placeholders must remain static.");
                }

                var visualFront = resolveVisualForward(model);
                if (Vector3.Dot(visualFront, Vector3.back) < 0.98f)
                {
                    throw new InvalidOperationException(slot.name + " does not face the Player side.");
                }
            }

            var bounds = BoundsOf(root, new Bounds(root.position, Vector3.one));
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            InspectPlayer(player, camera, root.GetChild(0), bounds, resolveVisualForward);
            return new Metrics
            {
                Longa = longa.position,
                Tergo = tergo.position,
                Dolore = dolore.position,
                Negatif = root.position,
                Player = player.position,
                PlayerForward = player.forward,
                ZSpacing = zSpacing,
                XSpacing = xSpacing,
                Bounds = bounds,
                VisualFront = resolveVisualForward(root.GetChild(0).GetChild(0)),
                MaterialNames = materialNames.OrderBy(value => value, StringComparer.Ordinal).ToArray()
            };
        }

        private static void ConfigurePlayer(Transform root)
        {
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var bounds = BoundsOf(root, new Bounds(root.position, Vector3.one));
            var front = ModelVisualForward(root.GetChild(0).GetChild(0));
            var desiredCamera = bounds.center + front * PlayerDistance(bounds, camera);
            var yaw = YawToward(desiredCamera, bounds.center);
            player.rotation = yaw;
            var cameraOffset = camera.transform.position - player.position;
            var desiredPlayer = desiredCamera - cameraOffset;
            desiredPlayer.y = 0f;
            player.SetPositionAndRotation(desiredPlayer, yaw);
            EditorUtility.SetDirty(player);
        }

        private static void InspectPlayer(
            Transform player,
            Camera camera,
            Transform firstSlot,
            Bounds bounds,
            Func<Transform, Vector3> visualForward)
        {
            var fromFocus = player.position - bounds.center;
            fromFocus.y = 0f;
            var front = visualForward(firstSlot.GetChild(0));
            var toFocus = bounds.center - player.position;
            toFocus.y = 0f;
            var forward = player.forward;
            forward.y = 0f;
            if (fromFocus.sqrMagnitude < 0.001f || front.sqrMagnitude < 0.001f ||
                Vector3.Dot(fromFocus.normalized, front.normalized) < 0.98f ||
                toFocus.sqrMagnitude < 0.001f || forward.sqrMagnitude < 0.001f ||
                Vector3.Dot(toFocus.normalized, forward.normalized) < 0.98f)
            {
                throw new InvalidOperationException("Player is not centered in front of Negatif.");
            }

            foreach (var corner in Corners(bounds))
            {
                var view = camera.WorldToViewportPoint(corner);
                if (view.z <= 0f || view.x < -0.02f || view.x > 1.02f ||
                    view.y < -0.02f || view.y > 1.02f)
                {
                    throw new InvalidOperationException(
                        "Player camera does not contain the full Negatif lineup.");
                }
            }
        }

        private static float PlayerDistance(Bounds bounds, Camera camera)
        {
            var vertical = Mathf.Max(1f, camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            var aspect = camera.aspect > 0.1f ? camera.aspect : 16f / 9f;
            var horizontal = Mathf.Atan(Mathf.Tan(vertical) * aspect);
            return Mathf.Max(
                MinimumPlayerDistance,
                bounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontal)) + CameraMargin,
                bounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(vertical)) + CameraMargin);
        }

        private static Material[] PrepareApprovedAssets()
        {
            if (!File.Exists(Absolute(ApprovedModelPath)))
            {
                throw new FileNotFoundException(
                    "The approved Negatif appearance FBX is missing.",
                    Absolute(ApprovedModelPath));
            }

            AssetDatabase.ImportAsset(
                ApprovedModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var modelImporter = AssetImporter.GetAtPath(ApprovedModelPath) as ModelImporter ??
                                throw new InvalidOperationException(
                                    "The approved Negatif ModelImporter is missing.");
            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.importAnimation = false;
            modelImporter.importBlendShapes = true;
            modelImporter.importVisibility = false;
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.globalScale = 1f;
            modelImporter.SaveAndReimport();

            return PrepareApprovedMaterials();
        }

        private static Material[] PrepareApprovedGlbAssets()
        {
            if (!File.Exists(Absolute(ApprovedGlbModelPath)))
            {
                throw new FileNotFoundException(
                    "The approved Negatif GLB Unity asset is missing.",
                    Absolute(ApprovedGlbModelPath));
            }

            AssetDatabase.ImportAsset(
                ApprovedGlbModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedGlbModelPath) == null)
            {
                throw new InvalidOperationException(
                    "The installed glTFast importer did not produce a Negatif GameObject asset.");
            }

            return PrepareApprovedMaterials();
        }

        private static Material[] PrepareApprovedMaterials()
        {
            foreach (var spec in ApprovedMaterialSpecs)
            {
                ConfigureApprovedTexture(spec.TexturePath("albedo"), true);
                ConfigureApprovedTexture(spec.TexturePath("roughness"), false);
                ConfigureApprovedTexture(spec.TexturePath("bump"), false);
            }

            AssetDatabase.ImportAsset(
                ApprovedShaderPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ApprovedShaderPath) ??
                         throw new InvalidOperationException(
                             "The approved Negatif appearance shader is missing.");
            if (!shader.isSupported || shader.name != ApprovedShaderName)
            {
                throw new InvalidOperationException(
                    "The approved Negatif appearance shader did not compile for the current render pipeline.");
            }

            var materials = new Material[ApprovedMaterialSpecs.Length];
            for (var i = 0; i < ApprovedMaterialSpecs.Length; i++)
            {
                var spec = ApprovedMaterialSpecs[i];
                var materialPath = spec.MaterialPath;
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader)
                    {
                        name = spec.Name
                    };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                else
                {
                    material.shader = shader;
                }

                material.SetTexture(
                    "_BaseMap",
                    spec.UseAlbedo
                        ? RequireTexture(spec.TexturePath("albedo"))
                        : null);
                material.SetTexture(
                    "_RoughnessMap",
                    RequireTexture(spec.TexturePath("roughness")));
                material.SetTexture(
                    "_HeightMap",
                    RequireTexture(spec.TexturePath("bump")));
                material.SetColor("_BaseColor", spec.BaseColor);
                material.SetFloat("_Metallic", spec.Metallic);
                material.SetFloat("_BumpStrength", spec.BumpStrength);
                material.SetColor("_EmissionColor", spec.EmissionColor);
                material.SetFloat("_EmissionStrength", spec.EmissionStrength);
                EditorUtility.SetDirty(material);
                materials[i] = material;
            }

            AssetDatabase.SaveAssets();
            return materials;
        }

        private static void ConfigureApprovedTexture(string path, bool sRgb)
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                           throw new InvalidOperationException(
                               "Approved Negatif texture importer is missing: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = sRgb;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }

        private static Texture2D RequireTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                   throw new InvalidOperationException(
                       "Approved Negatif texture is missing: " + path);
        }

        private static void AssignApprovedMaterials(
            Transform model,
            IReadOnlyList<Material> materials)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "The approved Negatif model must have exactly one renderer.");
            }

            var subMeshCount = RendererSubMeshCount(renderers[0]);
            if (subMeshCount != ApprovedMaterialSpecs.Length)
            {
                throw new InvalidOperationException(
                    "The approved Negatif model material partition changed. SubMeshes=" +
                    subMeshCount + ".");
            }

            renderers[0].sharedMaterials = materials.ToArray();
            EditorUtility.SetDirty(renderers[0]);
        }

        private static void AssignApprovedGlbMaterials(
            Transform model,
            IReadOnlyList<Material> materials)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 3)
            {
                throw new InvalidOperationException(
                    "The approved Negatif GLB must import as one body renderer and two eye renderers. Renderers=" +
                    renderers.Length + ".");
            }

            foreach (var renderer in renderers)
            {
                var imported = renderer.sharedMaterials;
                if (imported == null || imported.Length == 0)
                {
                    throw new InvalidOperationException(
                        "The approved Negatif GLB renderer has no imported material slots: " +
                        renderer.name + ".");
                }

                var remapped = new Material[imported.Length];
                for (var i = 0; i < imported.Length; i++)
                {
                    if (imported[i] == null)
                    {
                        throw new InvalidOperationException(
                            "The approved Negatif GLB renderer has a null imported material: " +
                            renderer.name + "[" + i + "].");
                    }

                    var match = ApprovedMaterialSpecs
                        .Select((spec, index) => new { spec, index })
                        .SingleOrDefault(item =>
                            string.Equals(
                                item.spec.Name,
                                imported[i].name,
                                StringComparison.Ordinal) ||
                            imported[i].name.StartsWith(
                                item.spec.Name + " ",
                                StringComparison.Ordinal));
                    if (match == null)
                    {
                        throw new InvalidOperationException(
                            "The approved Negatif GLB contains an unexpected material slot: " +
                            imported[i].name + ".");
                    }

                    remapped[i] = materials[match.index];
                }

                renderer.sharedMaterials = remapped;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static ApprovedAppearanceInspection InspectApprovedGlbAppearanceAssets(Transform root)
        {
            RequireApprovedGlbHashes();
            var expectedMaterials = ApprovedMaterialSpecs
                .Select(spec =>
                    AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath) ??
                    throw new InvalidOperationException(
                        "Approved Negatif material is missing: " + spec.MaterialPath))
                .ToArray();
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ApprovedShaderPath) ??
                         throw new InvalidOperationException(
                             "Approved Negatif shader is missing.");
            if (!shader.isSupported || shader.name != ApprovedShaderName)
            {
                throw new InvalidOperationException("Approved Negatif shader is invalid.");
            }

            var expectedBodyMaterials = new[]
            {
                expectedMaterials[0],
                expectedMaterials[1],
                expectedMaterials[2],
                expectedMaterials[3],
                expectedMaterials[4]
            };
            var expectedEyeMaterials = new[]
            {
                expectedMaterials[1],
                expectedMaterials[4],
                expectedMaterials[5]
            };

            for (var i = 0; i < SlotNames.Length; i++)
            {
                var model = root.GetChild(i).GetChild(0);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length != 3)
                {
                    throw new InvalidOperationException(
                        SlotNames[i] + " must have one approved body renderer and two approved eye renderers.");
                }

                var body = renderers.OfType<SkinnedMeshRenderer>().SingleOrDefault() ??
                           throw new InvalidOperationException(
                               SlotNames[i] + " must retain one approved skinned body renderer.");
                if (body.bones.Length != 46 ||
                    MeshTriangleCount(body) != 6397 ||
                    !body.sharedMaterials.SequenceEqual(expectedBodyMaterials))
                {
                    throw new InvalidOperationException(
                        SlotNames[i] +
                        " approved GLB body contract changed. Triangles=" +
                        MeshTriangleCount(body) + ", Bones=" + body.bones.Length +
                        ", Materials=" + body.sharedMaterials.Length + ".");
                }

                var eyeNames = new[]
                {
                    "Negatif_ReferenceEye_NegativeX",
                    "Negatif_ReferenceEye_PositiveX"
                };
                foreach (var eyeName in eyeNames)
                {
                    var eye = FindDescendant(model, eyeName) ??
                              throw new InvalidOperationException(
                                  SlotNames[i] + " is missing approved eye " + eyeName + ".");
                    var eyeRenderer = eye.GetComponent<Renderer>() ??
                                      throw new InvalidOperationException(
                                          SlotNames[i] + " approved eye has no renderer: " + eyeName + ".");
                    if (eyeRenderer is SkinnedMeshRenderer ||
                        MeshTriangleCount(eyeRenderer) != 676 ||
                        !eyeRenderer.sharedMaterials.SequenceEqual(expectedEyeMaterials))
                    {
                        throw new InvalidOperationException(
                            SlotNames[i] + " approved eye contract changed: " + eyeName + ".");
                    }
                }

                var totalTriangles = renderers.Sum(MeshTriangleCount);
                if (totalTriangles != 7749)
                {
                    throw new InvalidOperationException(
                        SlotNames[i] +
                        " approved GLB total triangle contract changed. Triangles=" +
                        totalTriangles + ".");
                }
            }

            foreach (var spec in ApprovedMaterialSpecs)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath);
                if (material == null || material.shader != shader ||
                    material.GetTexture("_BaseMap") !=
                    (spec.UseAlbedo ? RequireTexture(spec.TexturePath("albedo")) : null) ||
                    material.GetTexture("_RoughnessMap") != RequireTexture(spec.TexturePath("roughness")) ||
                    material.GetTexture("_HeightMap") != RequireTexture(spec.TexturePath("bump")) ||
                    Mathf.Abs(material.GetFloat("_Metallic") - spec.Metallic) > 0.0001f ||
                    Mathf.Abs(material.GetFloat("_BumpStrength") - spec.BumpStrength) > 0.0001f ||
                    Mathf.Abs(material.GetFloat("_EmissionStrength") - spec.EmissionStrength) > 0.0001f ||
                    material.GetColor("_BaseColor") != spec.BaseColor ||
                    material.GetColor("_EmissionColor") != spec.EmissionColor)
                {
                    throw new InvalidOperationException(
                        spec.Name + " no longer matches the approved sample material contract.");
                }
            }

            return new ApprovedAppearanceInspection
            {
                ModelCount = SlotNames.Length,
                MaterialCount = expectedMaterials.Length,
                TrianglesPerModel = 7749,
                BonesPerModel = 46,
                RenderersPerModel = 3,
                EyesPerModel = 2
            };
        }

        private static int MeshTriangleCount(Renderer renderer)
        {
            var mesh = RendererMesh(renderer);
            var triangles = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                triangles += checked((int)(mesh.GetIndexCount(subMesh) / 3));
            }

            return triangles;
        }

        private static ApprovedAppearanceInspection InspectApprovedAppearanceAssets(Transform root)
        {
            var expectedMaterials = ApprovedMaterialSpecs
                .Select(spec =>
                    AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath) ??
                    throw new InvalidOperationException(
                        "Approved Negatif material is missing: " + spec.MaterialPath))
                .ToArray();
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ApprovedShaderPath) ??
                         throw new InvalidOperationException(
                             "Approved Negatif shader is missing.");
            if (!shader.isSupported || shader.name != ApprovedShaderName)
            {
                throw new InvalidOperationException(
                    "Approved Negatif shader is invalid.");
            }

            var trianglesPerModel = -1;
            var bonesPerModel = -1;
            for (var i = 0; i < SlotNames.Length; i++)
            {
                var model = root.GetChild(i).GetChild(0);
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length != 1)
                {
                    throw new InvalidOperationException(
                        SlotNames[i] + " must have exactly one approved renderer.");
                }

                var renderer = renderers[0];
                if (!renderer.sharedMaterials.SequenceEqual(expectedMaterials))
                {
                    throw new InvalidOperationException(
                        SlotNames[i] + " does not use the six approved materials in sample order.");
                }

                var mesh = RendererMesh(renderer);
                var triangles = 0;
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    triangles += checked((int)(mesh.GetIndexCount(subMesh) / 3));
                }

                var skinned = renderer as SkinnedMeshRenderer ??
                              throw new InvalidOperationException(
                                  SlotNames[i] + " must retain the approved skinned mesh.");
                var bones = skinned.bones.Length;
                if (triangles != 6330 || bones != 27 || mesh.subMeshCount != 6)
                {
                    throw new InvalidOperationException(
                        SlotNames[i] + " approved geometry contract changed. Triangles=" +
                        triangles + ", Bones=" + bones + ", SubMeshes=" + mesh.subMeshCount + ".");
                }

                trianglesPerModel = triangles;
                bonesPerModel = bones;
            }

            foreach (var spec in ApprovedMaterialSpecs)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath);
                if (material == null || material.shader != shader ||
                    material.GetTexture("_BaseMap") !=
                    (spec.UseAlbedo ? RequireTexture(spec.TexturePath("albedo")) : null) ||
                    material.GetTexture("_RoughnessMap") != RequireTexture(spec.TexturePath("roughness")) ||
                    material.GetTexture("_HeightMap") != RequireTexture(spec.TexturePath("bump")) ||
                    Mathf.Abs(material.GetFloat("_Metallic") - spec.Metallic) > 0.0001f ||
                    Mathf.Abs(material.GetFloat("_BumpStrength") - spec.BumpStrength) > 0.0001f ||
                    Mathf.Abs(material.GetFloat("_EmissionStrength") - spec.EmissionStrength) > 0.0001f ||
                    material.GetColor("_BaseColor") != spec.BaseColor ||
                    material.GetColor("_EmissionColor") != spec.EmissionColor)
                {
                    throw new InvalidOperationException(
                        spec.Name + " no longer matches the approved sample material contract.");
                }
            }

            return new ApprovedAppearanceInspection
            {
                ModelCount = SlotNames.Length,
                MaterialCount = expectedMaterials.Length,
                TrianglesPerModel = trianglesPerModel,
                BonesPerModel = bonesPerModel
            };
        }

        private static Mesh RendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                return skinned.sharedMesh;
            }

            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException(
                    "The approved Negatif renderer has no mesh.");
        }

        private static int RendererSubMeshCount(Renderer renderer)
        {
            return RendererMesh(renderer).subMeshCount;
        }

        private static void CopyAndImportModel()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(ModelFolder);
            var destination = Absolute(ModelPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid Negatif model folder."));
            File.Copy(SourcePath, destination, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                           throw new InvalidOperationException("Negatif ModelImporter is missing.");
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

        private static void ConfigureStaticModel(Transform model)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
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
            if (bounds.size.y <= 0.00001f)
            {
                throw new InvalidOperationException("Negatif has no usable visible height.");
            }

            var scale = TargetHeight / bounds.size.y;
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f || scale > 1000f)
            {
                throw new InvalidOperationException("Negatif target-height scale is invalid.");
            }

            model.localScale = Vector3.one * scale;
            bounds = BoundsOf(model, new Bounds(model.position, Vector3.one));
            model.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static void AlignVisualFront(Transform slot, Transform model)
        {
            var current = ModelVisualForward(model);
            var yaw = Vector3.SignedAngle(current, Vector3.back, Vector3.up);
            slot.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * slot.rotation;
            var aligned = ModelVisualForward(model);
            if (Vector3.Dot(aligned, Vector3.back) < 0.999f)
            {
                throw new InvalidOperationException(
                    "Negatif head-to-tail axis could not be aligned toward the Player side.");
            }
        }

        private static void AlignModelVisualFront(Transform model, Vector3 desiredFront)
        {
            AlignModelVisualFront(model, desiredFront, ModelVisualForward);
        }

        private static void AlignModelVisualFront(
            Transform model,
            Vector3 desiredFront,
            Func<Transform, Vector3> visualForward)
        {
            desiredFront.y = 0f;
            if (desiredFront.sqrMagnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "The existing Negatif visual front is unusable.");
            }

            var current = visualForward(model);
            var yaw = Vector3.SignedAngle(current, desiredFront.normalized, Vector3.up);
            model.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * model.rotation;
            if (Vector3.Dot(visualForward(model), desiredFront.normalized) < 0.999f)
            {
                throw new InvalidOperationException(
                    "The approved Negatif model could not preserve the existing visual front.");
            }
        }

        private static Vector3 ModelVisualForward(Transform model)
        {
            var head = FindDescendant(model, "head") ??
                       throw new InvalidOperationException("Negatif head bone is missing.");
            var tail = FindDescendant(model, "tailstart") ??
                       throw new InvalidOperationException("Negatif tailstart bone is missing.");
            var direction = head.position - tail.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                throw new InvalidOperationException("Negatif head-to-tail axis is unusable.");
            }

            return direction.normalized;
        }

        private static Vector3 CurrentModelVisualForward(Transform model)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
            return source != null &&
                   AssetDatabase.GetAssetPath(source) == ApprovedGlbModelPath
                ? ApprovedGlbVisualForward(model)
                : ModelVisualForward(model);
        }

        private static Vector3 ApprovedGlbVisualForward(Transform model)
        {
            var negativeEye = FindDescendant(model, "Negatif_ReferenceEye_NegativeX") ??
                              throw new InvalidOperationException(
                                  "The approved Negatif negative-X eye is missing.");
            var positiveEye = FindDescendant(model, "Negatif_ReferenceEye_PositiveX") ??
                              throw new InvalidOperationException(
                                  "The approved Negatif positive-X eye is missing.");
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                       throw new InvalidOperationException(
                           "The approved Negatif GLB skinned body is missing.");
            var eyeCenter = (negativeEye.position + positiveEye.position) * 0.5f;
            var direction = eyeCenter - body.bounds.center;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "The approved Negatif GLB eye-to-body visual front is unusable.");
            }

            return direction.normalized;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => string.Equals(item.name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static float LongaTergoSpacing(Transform longa, Transform tergo)
        {
            var value = Mathf.Abs(longa.position.z - tergo.position.z);
            if (value <= 0.1f)
            {
                throw new InvalidOperationException("Longa/Tergo Z spacing is unusable.");
            }

            return value;
        }

        private static float DoloreSlotSpacing(Transform root)
        {
            var first = root.Find(DoloreFirstSlotName) ??
                        throw new InvalidOperationException("Dolore slot 1 is missing.");
            var second = root.Find(DoloreSecondSlotName) ??
                         throw new InvalidOperationException("Dolore slot 2 is missing.");
            var value = Mathf.Abs(second.position.x - first.position.x);
            if (value <= 0.1f)
            {
                throw new InvalidOperationException("Dolore X spacing is unusable.");
            }

            return value;
        }

        private static Vector3 NegatifPosition(Transform dolore, float spacing)
        {
            return new Vector3(
                dolore.position.x,
                dolore.position.y,
                dolore.position.z - spacing);
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

        private static void Capture(Camera camera, string path, int width, int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Invalid Negatif capture folder."));
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

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != PlacementRootName && root.name != PlayerName)
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

        private static string[] AppearanceProtectedRootSignatures(Scene scene)
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

        private static string TransformSignature(Transform transform)
        {
            return transform.name + "|" +
                   Vec(transform.localPosition) + "|" +
                   Quat(transform.localRotation) + "|" +
                   Vec(transform.localScale) + "|" +
                   transform.gameObject.activeSelf + "|" +
                   transform.childCount;
        }

        private static void RequireSampleApproval()
        {
            var path = Absolute(SampleApprovalPath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "The Negatif sample approval record is missing.",
                    path);
            }

            var approval = File.ReadAllText(path);
            if (approval.IndexOf(
                    "\"status\": \"APPROVED\"",
                    StringComparison.Ordinal) < 0 ||
                approval.IndexOf(
                    "\"approved_for_unity\": true",
                    StringComparison.Ordinal) < 0 ||
                approval.IndexOf(
                    "\"modeling_modified\": false",
                    StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    "The Negatif appearance sample is not approved for Unity or its no-modeling contract changed.");
            }
        }

        private static void RequireApprovedGlbSample()
        {
            var path = Absolute(ApprovedGlbSampleApprovalPath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "The approved Negatif GLB sample record is missing.",
                    path);
            }

            var approval = File.ReadAllText(path);
            if (approval.IndexOf(
                    "\"status\": \"APPROVED\"",
                    StringComparison.Ordinal) < 0 ||
                approval.IndexOf(
                    "\"approved_for_unity\": true",
                    StringComparison.Ordinal) < 0 ||
                approval.IndexOf(
                    "\"modeling_modified\": true",
                    StringComparison.Ordinal) < 0 ||
                approval.IndexOf(
                    "\"source_glb_modified\": false",
                    StringComparison.Ordinal) < 0 ||
                approval.IndexOf(
                    "\"added_eye_object_count\": 2",
                    StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    "The final Negatif GLB sample approval or its exact two-eye contract changed.");
            }
        }

        private static void RequireApprovedGlbHashes()
        {
            RequireHash(
                SourceGlbPath,
                SourceGlbHash,
                "The supplied Négatif GLB changed.");
            RequireHash(
                Absolute(ApprovedGlbSamplePath),
                ApprovedGlbHash,
                "The approved Négatif GLB sample changed.");
            RequireHash(
                Absolute(ApprovedGlbModelPath),
                ApprovedGlbHash,
                "The Unity Négatif GLB asset is not an exact approved-sample copy.");
        }

        private static void RequireHash(string path, string expectedHash, string message)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(message, path);
            }

            var actualHash = Sha256(path);
            if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    message + " Expected=" + expectedHash + ", Actual=" + actualHash + ".");
            }
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the current active scene. ActiveScene=" + scene.path);
            }

            return scene;
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerName);
            if (player != null)
            {
                return player.transform;
            }

            var controller = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            return controller != null
                ? controller.transform
                : throw new InvalidOperationException("Player is missing.");
        }

        private static GameObject RequireRoot(string name)
        {
            return GameObject.Find(name) ??
                   throw new InvalidOperationException(name + " is missing from CargoRunMvp.");
        }

        private static void RequireSource()
        {
            if (!File.Exists(SourcePath))
            {
                throw new FileNotFoundException("The supplied Negatif FBX is missing.", SourcePath);
            }
        }

        private static void RequireSameHash(string first, string second)
        {
            if (!string.Equals(first, second, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The supplied and imported Negatif FBX hashes differ.");
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
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
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

        private sealed class ApprovedMaterialSpec
        {
            public ApprovedMaterialSpec(
                string name,
                string texturePrefix,
                float metallic,
                float bumpStrength,
                Color baseColor,
                bool useAlbedo,
                Color emissionColor,
                float emissionStrength)
            {
                Name = name;
                TexturePrefix = texturePrefix;
                Metallic = metallic;
                BumpStrength = bumpStrength;
                BaseColor = baseColor;
                UseAlbedo = useAlbedo;
                EmissionColor = emissionColor;
                EmissionStrength = emissionStrength;
            }

            public string Name { get; }
            public string TexturePrefix { get; }
            public float Metallic { get; }
            public float BumpStrength { get; }
            public Color BaseColor { get; }
            public bool UseAlbedo { get; }
            public Color EmissionColor { get; }
            public float EmissionStrength { get; }
            public string MaterialPath => MaterialFolder + "/" + Name + ".mat";

            public string TexturePath(string channel)
            {
                return TextureFolder + "/" + TexturePrefix + "_" + channel + ".png";
            }
        }

        private sealed class ApprovedAppearanceInspection
        {
            public int ModelCount;
            public int MaterialCount;
            public int TrianglesPerModel;
            public int BonesPerModel;
            public int RenderersPerModel;
            public int EyesPerModel;
        }

        private sealed class Metrics
        {
            public Vector3 Longa;
            public Vector3 Tergo;
            public Vector3 Dolore;
            public Vector3 Negatif;
            public Vector3 Player;
            public Vector3 PlayerForward;
            public Vector3 VisualFront;
            public float ZSpacing;
            public float XSpacing;
            public Bounds Bounds;
            public string[] MaterialNames;
        }
    }
}
