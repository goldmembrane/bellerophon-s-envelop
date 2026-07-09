using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.MonstrumCargoRunScene
{
    internal static class MonstrumCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SocietasPlacementRootName = "Approved Societas Enemy Placement";
        private const string TergoPlacementRootName = "Approved Tergo Enemy Placement";
        private const string UrzerePlacementRootName = "Approved Urzere Enemy Placement";
        private const string PlacementRootName = "Approved Monstrum Enemy Placement";
        private const string PlacementObjectName = "Monstrum_00_Static_Review";
        private const string ModelChildName = "MonstrumPrepared_Model";
        private const string PlayerRootName = "Player";

        private const string SourceModelAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/monstrum.fbx";
        private const string MonstrumArtRoot = "Assets/_Project/Art/Enemies/Monstrum";
        private const string UnityModelFolder = MonstrumArtRoot + "/Models";
        private const string UnityMeshFolder = MonstrumArtRoot + "/Meshes";
        private const string UnityMaterialFolder = MonstrumArtRoot + "/Materials";
        private const string UnityMaterialTextureFolder = UnityMaterialFolder + "/Textures";
        private const string UnityAnimationFolder = MonstrumArtRoot + "/Animations";
        private const string UnityAnimationSourceFolder = UnityAnimationFolder + "/Source";
        private const string UnityModelAssetPath = UnityModelFolder + "/monstrum.fbx";
        private const string SourceAnimationAssetPath = UnityAnimationSourceFolder + "/monstrum_source_animation.fbx";
        private const string ApprovedBodyTextureAssetPath = UnityMaterialTextureFolder + "/Monstrum_Approved_DarkMossBody_Albedo.png";
        private const string ApprovedBodyMaterialAssetPath = UnityMaterialFolder + "/Monstrum_Approved_DarkMossBody.mat";
        private const string ApprovedEyeMaterialAssetPath = UnityMaterialFolder + "/Monstrum_Approved_YellowEye.mat";
        private const string IdleSlotObjectName = "Monstrum_02_Idle";
        private const string MoveSlotObjectName = "Monstrum_03_Move_HeavyStomp";
        private const string IdleBreathingBlendShapeName = "IdleBreathIn";
        private const string IdleBreathingClipAssetPath = UnityAnimationFolder + "/Monstrum_02_Idle_Breathing.anim";
        private const string IdleBreathingControllerAssetPath = UnityAnimationFolder + "/Monstrum_02_Idle.controller";
        private const string MoveSourceAnimationControllerAssetPath = UnityAnimationFolder + "/Monstrum_03_Move_HeavyStomp.controller";
        private const string IdleBreathingMeshAssetPrefix = "Monstrum_02_Idle_Breathing_";
        private const string ValidationFolder = "docs/validation/monstrum";
        private const string VisualRecolorEyeArtSampleFolder = "artSample/enemies/monstrum/visual_recolor_eye_sample";
        private const string ApprovedEyeRootName = "Monstrum_ApprovedVisualEyes";
        private const string VisualRecolorEyeReferenceImagePath = "image/monstrum(몬스트룸).png";

        private const float MonstrumTargetHeightMeters = 2.50f;
        private const float MonstrumFacingYawDegrees = 180f;
        private const float FallbackTergoUrzereSpacing = 4.00f;
        private const float PlacementToleranceMeters = 0.015f;
        private const float HeightToleranceMeters = 0.08f;
        private const float ReviewCameraMinimumFrontDistance = 4.00f;
        private const float ReviewCameraMaximumFrontDistance = 10.00f;
        private const float ReviewPlayerMinimumFrontDistance = 5.25f;
        private const float ReviewPlayerMaximumFrontDistance = 8.00f;
        private const float AnimationSlotMinimumSpacing = 3.35f;
        private const float IdleBreathingDurationSeconds = 1.80f;
        private const float LooseGrainPositionWeldTolerance = 0.0001f;
        private const float LooseGrainMinimumGapFromBodyMeters = 0.12f;
        private const float LooseGrainMaximumWorldExtentMeters = 0.70f;
        private const float LooseGrainMaximumAreaRatio = 0.055f;
        private static readonly Color SampleDarkMossBodyColor = new(0.055f, 0.145f, 0.065f, 1f);
        private static readonly Color SampleDarkMossBodyShadowColor = new(0.035f, 0.085f, 0.035f, 1f);
        private static readonly Color SampleEyeGlowColor = new(0.94f, 0.78f, 0.16f, 1f);

        private static readonly MotionSlotSpec[] AnimationSlotSpecs =
        {
            new MotionSlotSpec("Monstrum_02_Idle"),
            new MotionSlotSpec("Monstrum_03_Move_HeavyStomp"),
            new MotionSlotSpec("Monstrum_04_Attack_DoubleHammerSlam"),
            new MotionSlotSpec("Monstrum_05_Attack_ImpactShake"),
            new MotionSlotSpec("Monstrum_06_MetalConsume_FacilityBreak"),
            new MotionSlotSpec("Monstrum_07_Retarget_ToPlayer"),
            new MotionSlotSpec("Monstrum_08_Hit_Recoil"),
            new MotionSlotSpec("Monstrum_09_Death")
        };

        [MenuItem("Bellerophon/Enemies/Monstrum/Apply Prepared Model To CargoRunMvp")]
        public static void ApplyPreparedModelToCurrentCargoRunScene()
        {
            RequirePreparedModelFile();
            EnsureUnityFolders();
            CopyPreparedModelAsset();
            ConfigureImportedModelAsset();

            var modelAsset = LoadPreparedModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlacePreparedModel(modelAsset, scene);
            var inspection = InspectSceneState(placementRoot.transform);
            WritePlacementSummary(inspection);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Monstrum model applied to CargoRunMvp scene.");
        }

        public static void InspectAppliedSceneState()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var inspection = InspectSceneState(placementRoot.transform);
            WritePlacementSummary(inspection);
            Debug.Log("Prepared Monstrum CargoRunMvp scene state inspected.");
        }

        public static void CaptureReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var inspection = InspectSceneState(placementRoot.transform);
            WritePlacementSummary(inspection);

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var focus = FindMonstrumCameraFocus(placementRoot.transform);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var cameraObject = new GameObject("MonstrumStatic_CaptureCamera");
            var lightObject = new GameObject("MonstrumStatic_CaptureLight");
            Texture2D texture = null;
            var outputPath = Path.Combine(outputDirectory, "Monstrum_00_Static_Review.png");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureCaptureCamera(camera, focus, bounds);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.transform.rotation = Quaternion.Euler(45f, focus.eulerAngles.y - 28f, 0f);

                texture = CaptureCameraTexture(camera, 1400, 900);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("MonstrumStaticCapture Path=" + outputPath);
        }

        public static void CaptureEyeCloseupReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var focus = FindMonstrumCameraFocus(placementRoot.transform);
            var eyeRoot = FindApprovedEyeRoot(focus);
            if (eyeRoot == null)
            {
                throw new InvalidOperationException($"{ApprovedEyeRootName} is missing under {focus.name}.");
            }

            var eyeRenderers = eyeRoot.GetComponentsInChildren<Renderer>(true);
            if (eyeRenderers.Length == 0)
            {
                throw new InvalidOperationException($"{ApprovedEyeRootName} contains no eye renderers.");
            }

            var eyeBounds = eyeRenderers[0].bounds;
            for (var i = 1; i < eyeRenderers.Length; i++)
            {
                eyeBounds.Encapsulate(eyeRenderers[i].bounds);
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            CaptureEyeCloseupImage(
                focus,
                eyeBounds,
                Quaternion.Euler(0f, 8f, 0f) * CalculateMonstrumVisualFrontDirection(focus),
                Path.Combine(outputDirectory, "Monstrum_00_Static_Eye_Closeup_Front.png"));
            CaptureEyeCloseupImage(
                focus,
                eyeBounds,
                Quaternion.Euler(0f, 68f, 0f) * CalculateMonstrumVisualFrontDirection(focus),
                Path.Combine(outputDirectory, "Monstrum_00_Static_Eye_Closeup_Side.png"));

            Debug.Log("MonstrumEyeCloseupCapture Directory=" + outputDirectory);
        }

        public static void MovePlayerStartToOppositeSide()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var inspection = MoveExistingPlayerStartToMonstrumFront(placementRoot.transform);
            InspectPlayerStart(placementRoot.transform);
            WritePlayerStartSummary(inspection);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Prepared Monstrum player start moved to the opposite side.");
        }

        public static void InspectPlayerStartInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var inspection = InspectPlayerStart(placementRoot.transform);
            WritePlayerStartSummary(inspection);
            Debug.Log("Prepared Monstrum player start inspected.");
        }

        public static void ApplyAnimationReviewSlots()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var staticObject = RequireStaticReviewObject(placementRoot.transform);
            AddOrRebuildAnimationReviewSlots(placementRoot.transform, staticObject);
            var inspection = InspectAnimationReviewSlots(placementRoot.transform);
            WriteAnimationSlotSummary(inspection);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Prepared Monstrum animation review slots 02-09 applied.");
        }

        public static void ValidateAnimationReviewSlots()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var inspection = InspectAnimationReviewSlots(placementRoot.transform);
            WriteAnimationSlotSummary(inspection);
            Debug.Log("Prepared Monstrum animation review slots 02-09 validated.");
        }

        public static void ApplyIdleBreathingAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolder(UnityMeshFolder);
            EnsureUnityFolder(UnityAnimationFolder);
            InspectSceneState(placementRoot.transform);
            var result = ApplyIdleBreathingAnimationToSlot(placementRoot.transform);
            ValidateIdleBreathingAnimationOnPlacementRoot(placementRoot.transform);
            WriteIdleBreathingAnimationSummary(result);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Monstrum idle breathing animation applied to Monstrum_02_Idle.");
        }

        public static void ValidateIdleBreathingAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var result = ValidateIdleBreathingAnimationOnPlacementRoot(placementRoot.transform);
            WriteIdleBreathingAnimationSummary(result);
            Debug.Log("Prepared Monstrum idle breathing animation validated.");
        }

        public static void ApplyMoveSourceAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityAnimationSourceFolder);
            CopyMoveSourceAnimationAsset();
            ConfigureMoveSourceAnimationImporter();
            var clip = LoadMoveSourceAnimationClip();
            var controller = CreateOrUpdateMoveSourceAnimationController(clip);
            var result = ApplyMoveSourceAnimationToSlot(placementRoot.transform, controller, clip);
            ValidateMoveSourceAnimationOnPlacementRoot(placementRoot.transform);
            WriteMoveSourceAnimationSummary(result);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Monstrum move source animation applied to Monstrum_03_Move_HeavyStomp.");
        }

        public static void ValidateMoveSourceAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var result = ValidateMoveSourceAnimationOnPlacementRoot(placementRoot.transform);
            WriteMoveSourceAnimationSummary(result);
            Debug.Log("Prepared Monstrum move source animation validated.");
        }

        public static void ApplyLooseGrainRemoval()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolder(UnityMeshFolder);
            InspectSceneState(placementRoot.transform);
            var result = ApplyLooseGrainRemovalToPlacementRoot(placementRoot.transform);
            ValidateLooseGrainRemovalOnPlacementRoot(placementRoot.transform, result);
            WriteLooseGrainRemovalSummary(result);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Monstrum loose grain removal applied.");
        }

        public static void ValidateLooseGrainRemoval()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var result = InspectLooseGrainRemovalOnPlacementRoot(placementRoot.transform);
            Debug.Log("Prepared Monstrum loose grain removal validated.");
        }

        public static void CreateVisualRecolorEyeArtSample()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var staticObject = RequireStaticReviewObject(placementRoot.transform);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", VisualRecolorEyeArtSampleFolder));
            PrepareVisualRecolorEyeArtSampleDirectory(outputDirectory);
            var rendersDirectory = Path.Combine(outputDirectory, "renders");

            var previewObject = UnityEngine.Object.Instantiate(staticObject.gameObject);
            previewObject.name = "Monstrum_VisualRecolorEye_ArtSample_Preview";
            previewObject.transform.SetPositionAndRotation(new Vector3(0f, staticObject.position.y, 0f), staticObject.rotation);
            previewObject.transform.localScale = staticObject.lossyScale;
            DisableImportedAnimationPlayback(previewObject.transform);

            var bodyTexture = CreateArtSampleBodyTexture(outputDirectory);
            var bodyMaterial = CreateArtSampleBodyMaterial(bodyTexture);
            var eyeMaterial = CreateArtSampleEyeMaterial();
            var eyeInfo = default(EyeSampleInfo);

            try
            {
                ApplyArtSampleBodyMaterial(previewObject.transform, bodyMaterial);
                eyeInfo = AddArtSampleEyes(previewObject.transform, eyeMaterial);

                var frontRenderPath = Path.Combine(rendersDirectory, "front.png");
                var sideRenderPath = Path.Combine(rendersDirectory, "side.png");
                var backRenderPath = Path.Combine(rendersDirectory, "back.png");
                var headCloseRenderPath = Path.Combine(rendersDirectory, "head_close.png");

                CaptureArtSamplePreview(
                    previewObject.transform,
                    frontRenderPath,
                    0f,
                    1.16f,
                    null);
                CaptureArtSamplePreview(
                    previewObject.transform,
                    sideRenderPath,
                    90f,
                    1.20f,
                    null);
                CaptureArtSamplePreview(
                    previewObject.transform,
                    backRenderPath,
                    180f,
                    1.16f,
                    null);
                CaptureArtSamplePreview(
                    previewObject.transform,
                    headCloseRenderPath,
                    5f,
                    0.36f,
                    eyeInfo.EyeCenter);

                CreateReferenceComparisonImage(outputDirectory, frontRenderPath);
                ExportVisualRecolorEyeObj(previewObject.transform, outputDirectory);
                WriteVisualRecolorEyeArtSampleReadme(outputDirectory, eyeInfo);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(previewObject);
                UnityEngine.Object.DestroyImmediate(bodyMaterial);
                UnityEngine.Object.DestroyImmediate(eyeMaterial);
                UnityEngine.Object.DestroyImmediate(bodyTexture);
            }

            Debug.Log("Prepared Monstrum visual recolor eye art sample created at " + outputDirectory);
        }

        public static void ApplyVisualRecolorEyeToScene()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var bodyMaterial = CreateOrUpdateApprovedBodyMaterialAsset();
            var eyeMaterial = CreateOrUpdateApprovedEyeMaterialAsset();
            var result = ApplyVisualRecolorEyeToPlacementRoot(placementRoot.transform, bodyMaterial, eyeMaterial);
            ValidateVisualRecolorEyeOnPlacementRoot(placementRoot.transform, bodyMaterial, eyeMaterial, result.TargetCount);
            WriteVisualRecolorEyeSceneSummary(result);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Monstrum visual recolor eye scene visuals applied.");
        }

        public static void ValidateVisualRecolorEyeScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedBodyMaterialAssetPath);
            var eyeMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedEyeMaterialAssetPath);
            if (bodyMaterial == null)
            {
                throw new InvalidOperationException($"Approved body material is missing at {ApprovedBodyMaterialAssetPath}.");
            }

            if (eyeMaterial == null)
            {
                throw new InvalidOperationException($"Approved eye material is missing at {ApprovedEyeMaterialAssetPath}.");
            }

            var result = ValidateVisualRecolorEyeOnPlacementRoot(placementRoot.transform, bodyMaterial, eyeMaterial, 1 + AnimationSlotSpecs.Length);
            WriteVisualRecolorEyeSceneSummary(result);
            Debug.Log("Prepared Monstrum visual recolor eye scene visuals validated.");
        }

        private static void PrepareVisualRecolorEyeArtSampleDirectory(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            Directory.CreateDirectory(Path.Combine(outputDirectory, "renders"));
            Directory.CreateDirectory(Path.Combine(outputDirectory, "textures"));
            Directory.CreateDirectory(Path.Combine(outputDirectory, "exports"));

            var legacyFiles = new[]
            {
                "monstrum_dark_green_eye_front.png",
                "monstrum_dark_green_eye_three_quarter.png",
                "monstrum_dark_green_eye_head_close.png",
                "monstrum_dark_moss_body_texture.png",
                "monstrum_reference_vs_sample_side_by_side.png",
                "preview.html",
                "APPROVAL_STATUS.md",
                "sample_manifest.txt"
            };

            foreach (var legacyFile in legacyFiles)
            {
                var path = Path.Combine(outputDirectory, legacyFile);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            var legacyExportDirectory = Path.Combine(outputDirectory, "export");
            if (Directory.Exists(legacyExportDirectory))
            {
                Directory.Delete(legacyExportDirectory, true);
            }
        }

        private static GameObject PlacePreparedModel(GameObject modelAsset, Scene scene)
        {
            var societasRoot = RequireSceneRoot(SocietasPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var urzereRoot = RequireSceneRoot(UrzerePlacementRootName);
            var spacing = CalculateTergoUrzereSpacing(tergoRoot.transform, urzereRoot.transform);
            var placementPosition = new Vector3(
                societasRoot.transform.position.x,
                societasRoot.transform.position.y,
                societasRoot.transform.position.z - spacing);

            var existingRoot = GameObject.Find(PlacementRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            var placementRoot = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(placementRoot, scene);
            placementRoot.transform.position = placementPosition;
            placementRoot.transform.rotation = Quaternion.identity;
            placementRoot.transform.localScale = Vector3.one;

            var reviewRoot = new GameObject(PlacementObjectName);
            reviewRoot.transform.SetParent(placementRoot.transform, false);
            reviewRoot.transform.localPosition = Vector3.zero;
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, MonstrumFacingYawDegrees, 0f);
            reviewRoot.transform.localScale = Vector3.one;

            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = ModelChildName;
            modelInstance.transform.SetParent(reviewRoot.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            DisableImportedAnimationPlayback(reviewRoot.transform);
            RequireRenderers(reviewRoot.transform);
            ScaleToTargetHeightAndAlignToGround(reviewRoot.transform, placementRoot.transform.position.y);

            EditorUtility.SetDirty(placementRoot);
            EditorUtility.SetDirty(reviewRoot);
            return placementRoot;
        }

        private static PlacementInspection InspectSceneState(Transform placementRoot)
        {
            var societasRoot = RequireSceneRoot(SocietasPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var urzereRoot = RequireSceneRoot(UrzerePlacementRootName);
            var reviewRoot = placementRoot.Find(PlacementObjectName);
            if (reviewRoot == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            var modelRoot = reviewRoot.Find(ModelChildName);
            if (modelRoot == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {PlacementObjectName}.");
            }

            var rendererCount = RequireRenderers(reviewRoot);
            var bounds = CalculateRendererBounds(reviewRoot, new Bounds(reviewRoot.position, Vector3.one));
            var spacing = CalculateTergoUrzereSpacing(tergoRoot.transform, urzereRoot.transform);
            var expectedPosition = new Vector3(
                societasRoot.transform.position.x,
                societasRoot.transform.position.y,
                societasRoot.transform.position.z - spacing);
            var placementDelta = Vector3.Distance(placementRoot.position, expectedPosition);
            var actualSocietasZSpacing = Mathf.Abs(societasRoot.transform.position.z - placementRoot.position.z);
            var spacingDelta = Mathf.Abs(actualSocietasZSpacing - spacing);
            var heightDelta = Mathf.Abs(bounds.size.y - MonstrumTargetHeightMeters);

            if (placementRoot.position.z >= societasRoot.transform.position.z)
            {
                throw new InvalidOperationException(
                    $"Monstrum must be below Societas on Z. SocietasZ={societasRoot.transform.position.z:0.###}, MonstrumZ={placementRoot.position.z:0.###}.");
            }

            if (placementDelta > PlacementToleranceMeters)
            {
                throw new InvalidOperationException(
                    $"Monstrum placement does not match Societas minus Tergo-Urzere spacing. Delta={placementDelta:0.###}, Expected={FormatVector(expectedPosition)}, Actual={FormatVector(placementRoot.position)}.");
            }

            if (spacingDelta > PlacementToleranceMeters)
            {
                throw new InvalidOperationException(
                    $"Monstrum-Societas spacing must match Tergo-Urzere spacing. Delta={spacingDelta:0.###}, ExpectedSpacing={spacing:0.###}, ActualSpacing={actualSocietasZSpacing:0.###}.");
            }

            if (heightDelta > HeightToleranceMeters)
            {
                throw new InvalidOperationException(
                    $"Monstrum height must remain near {MonstrumTargetHeightMeters:0.###}m. Height={bounds.size.y:0.###}, Delta={heightDelta:0.###}.");
            }

            Debug.Log(
                "MonstrumPlacementInspection " +
                $"Root={PlacementRootName}, Object={PlacementObjectName}, Model={ModelChildName}, " +
                $"Position={FormatVector(placementRoot.position)}, SocietasPosition={FormatVector(societasRoot.transform.position)}, " +
                $"TergoUrzereSpacing={spacing:0.###}, SocietasMonstrumSpacing={actualSocietasZSpacing:0.###}, " +
                $"RendererCount={rendererCount}, BoundsCenter={FormatVector(bounds.center)}, BoundsSize={FormatVector(bounds.size)}.");

            return new PlacementInspection(
                placementRoot.position,
                societasRoot.transform.position,
                tergoRoot.transform.position,
                urzereRoot.transform.position,
                spacing,
                actualSocietasZSpacing,
                rendererCount,
                bounds);
        }

        private static void RequirePreparedModelFile()
        {
            if (!File.Exists(SourceModelAbsolutePath))
            {
                throw new FileNotFoundException("Monstrum prepared FBX file is missing.", SourceModelAbsolutePath);
            }
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(UnityModelFolder);
        }

        private static void CopyPreparedModelAsset()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var destinationPath = Path.GetFullPath(Path.Combine(projectRoot, UnityModelAssetPath));
            var destinationFolder = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrEmpty(destinationFolder))
            {
                throw new InvalidOperationException("Monstrum model destination folder could not be resolved.");
            }

            Directory.CreateDirectory(destinationFolder);
            File.Copy(SourceModelAbsolutePath, destinationPath, true);
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureImportedModelAsset()
        {
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(UnityModelAssetPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                importer.SaveAndReimport();
            }
        }

        private static GameObject LoadPreparedModelAsset()
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Could not load Monstrum model asset at {UnityModelAssetPath}.");
            }

            return modelAsset;
        }

        private static void DisableImportedAnimationPlayback(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                EditorUtility.SetDirty(animator);
            }

            foreach (var animation in root.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static int RequireRenderers(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Monstrum prepared model contains no renderers.");
            }

            return renderers.Length;
        }

        private static Transform RequireStaticReviewObject(Transform placementRoot)
        {
            var staticObject = placementRoot.Find(PlacementObjectName);
            if (staticObject == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            return staticObject;
        }

        private static void AddOrRebuildAnimationReviewSlots(Transform placementRoot, Transform staticObject)
        {
            foreach (var spec in AnimationSlotSpecs)
            {
                var existing = placementRoot.Find(spec.ObjectName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
            }

            var staticBounds = CalculateAnimationReviewBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            var spacing = CalculateAnimationSlotSpacing(staticBounds);
            for (var i = 0; i < AnimationSlotSpecs.Length; i++)
            {
                var spec = AnimationSlotSpecs[i];
                var instance = UnityEngine.Object.Instantiate(staticObject.gameObject, placementRoot);
                instance.name = spec.ObjectName;
                instance.transform.SetPositionAndRotation(
                    new Vector3(
                        staticObject.position.x + spacing * (i + 1),
                        staticObject.position.y,
                        staticObject.position.z),
                    staticObject.rotation);
                instance.transform.localScale = staticObject.localScale;

                DisableImportedAnimationPlayback(instance.transform);
                EditorUtility.SetDirty(instance);
            }

            Debug.Log(
                $"Monstrum animation review slots rebuilt. Count={AnimationSlotSpecs.Length}, StaticPosition={FormatVector(staticObject.position)}, Spacing={spacing:0.###}.");
        }

        private static AnimationSlotInspection InspectAnimationReviewSlots(Transform placementRoot)
        {
            var staticObject = RequireStaticReviewObject(placementRoot);
            var staticBounds = CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            var spacing = CalculateAnimationSlotSpacing(staticBounds);
            var previousBoundsMaxX = staticBounds.max.x;

            for (var i = 0; i < AnimationSlotSpecs.Length; i++)
            {
                var spec = AnimationSlotSpecs[i];
                var slot = placementRoot.Find(spec.ObjectName);
                if (slot == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing under {PlacementRootName}.");
                }

                var expectedPosition = new Vector3(
                    staticObject.position.x + spacing * (i + 1),
                    staticObject.position.y,
                    staticObject.position.z);
                var distance = Vector3.Distance(slot.position, expectedPosition);
                if (distance > 0.02f)
                {
                    throw new InvalidOperationException(
                        $"{spec.ObjectName} is not in the expected animation review row. Expected={FormatVector(expectedPosition)}, Actual={FormatVector(slot.position)}, Delta={distance:0.###}.");
                }

                if (Mathf.Abs(slot.position.z - staticObject.position.z) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"{spec.ObjectName} must stay on the same Z axis as {PlacementObjectName}. StaticZ={staticObject.position.z:0.###}, SlotZ={slot.position.z:0.###}.");
                }

                if (Mathf.Abs(slot.position.y - staticObject.position.y) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"{spec.ObjectName} must stay on the same base Y as {PlacementObjectName}. StaticY={staticObject.position.y:0.###}, SlotY={slot.position.y:0.###}.");
                }

                if (Quaternion.Angle(slot.rotation, staticObject.rotation) > 0.01f)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} rotation must match {PlacementObjectName}.");
                }

                var renderers = slot.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} contains no renderers.");
                }

                var bounds = CalculateAnimationReviewBounds(slot, new Bounds(slot.position, Vector3.one));
                if (bounds.min.x <= previousBoundsMaxX + 0.10f)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} overlaps the previous Monstrum review object on X.");
                }

                if (Mathf.Abs(bounds.size.y - MonstrumTargetHeightMeters) > HeightToleranceMeters)
                {
                    throw new InvalidOperationException(
                        $"{spec.ObjectName} height must stay near {MonstrumTargetHeightMeters:0.###}m. Height={bounds.size.y:0.###}.");
                }

                previousBoundsMaxX = bounds.max.x;
            }

            Debug.Log(
                $"MonstrumAnimationSlotInspection Count={AnimationSlotSpecs.Length}, StaticPosition={FormatVector(staticObject.position)}, StaticBoundsSize={FormatVector(staticBounds.size)}, Spacing={spacing:0.###}.");

            return new AnimationSlotInspection(
                staticObject.position,
                staticBounds.size,
                spacing,
                AnimationSlotSpecs.Length);
        }

        private static float CalculateAnimationSlotSpacing(Bounds staticBounds)
        {
            return Mathf.Max(staticBounds.size.x + 1.10f, AnimationSlotMinimumSpacing);
        }

        private static Bounds CalculateAnimationReviewBounds(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return fallback;
            }

            var bounds = ResolveAnimationReviewRendererBounds(renderers[0]);
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(ResolveAnimationReviewRendererBounds(renderers[i]));
            }

            return bounds;
        }

        private static Bounds ResolveAnimationReviewRendererBounds(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedRenderer)
            {
                return TransformLocalBoundsToWorld(
                    skinnedRenderer.rootBone != null ? skinnedRenderer.rootBone : skinnedRenderer.transform,
                    skinnedRenderer.localBounds);
            }

            return renderer.bounds;
        }

        private static Bounds TransformLocalBoundsToWorld(Transform transform, Bounds localBounds)
        {
            var corners = new[]
            {
                new Vector3(localBounds.min.x, localBounds.min.y, localBounds.min.z),
                new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z),
                new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z),
                new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z),
                new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z),
                new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z),
                new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z),
                new Vector3(localBounds.max.x, localBounds.max.y, localBounds.max.z)
            };

            var worldBounds = new Bounds(transform.TransformPoint(corners[0]), Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                worldBounds.Encapsulate(transform.TransformPoint(corners[i]));
            }

            return worldBounds;
        }

        private static IdleBreathingAnimationResult ApplyIdleBreathingAnimationToSlot(Transform placementRoot)
        {
            var slot = RebuildIdleSlotFromStaticReviewObject(placementRoot);
            var eyeRoot = FindApprovedEyeRoot(slot);
            if (eyeRoot == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} is missing {ApprovedEyeRootName}; apply the approved Monstrum visual sample before adding idle breathing.");
            }

            var bodyRenderers = GetBodyRenderers(slot, eyeRoot);
            if (bodyRenderers.Count == 0)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} has no body renderers to morph.");
            }

            var staticObject = RequireStaticReviewObject(placementRoot);
            var skinnedRenderers = new List<SkinnedMeshRenderer>();
            var meshAssetPaths = new List<string>();
            foreach (var renderer in bodyRenderers)
            {
                var referenceRenderer = ResolveReferenceBodyRenderer(staticObject, slot, renderer);
                var sourceMesh = referenceRenderer != null ? ResolveAssignedRendererMesh(referenceRenderer) : ResolveAssignedRendererMesh(renderer);
                if (sourceMesh == null)
                {
                    throw new InvalidOperationException($"{IdleSlotObjectName}/{renderer.name} has no assigned mesh for idle breathing.");
                }

                var meshAssetPath = BuildIdleBreathingMeshAssetPath(slot, renderer, sourceMesh);
                var breathingMesh = CreateOrUpdateIdleBreathingMesh(sourceMesh, meshAssetPath, referenceRenderer is SkinnedMeshRenderer);
                var skinnedRenderer = EnsureIdleBreathingSkinnedRenderer(renderer, breathingMesh, referenceRenderer, staticObject, slot);
                skinnedRenderers.Add(skinnedRenderer);
                meshAssetPaths.Add(meshAssetPath);
            }

            var clip = CreateOrUpdateIdleBreathingClip(slot, skinnedRenderers);
            var controller = CreateOrUpdateIdleBreathingController(clip);
            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(slot.gameObject);

            return new IdleBreathingAnimationResult(
                IdleSlotObjectName,
                bodyRenderers.Count,
                skinnedRenderers.Count,
                meshAssetPaths.Count,
                string.Join("; ", meshAssetPaths),
                IdleBreathingClipAssetPath,
                IdleBreathingControllerAssetPath,
                CountIdleBreathingBlendShapeBindings(clip),
                CountEyeFollowPositionBindings(clip, BuildTransformPath(slot, FindApprovedEyeRoot(slot))),
                AnimationUtility.GetCurveBindings(clip).Length);
        }

        private static IdleBreathingAnimationResult ValidateIdleBreathingAnimationOnPlacementRoot(Transform placementRoot)
        {
            var slot = RequireAnimationReviewSlot(placementRoot, IdleSlotObjectName);
            ValidateIdleSlotTransform(placementRoot, slot);
            var eyeRoot = FindApprovedEyeRoot(slot);
            if (eyeRoot == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} is missing {ApprovedEyeRootName}.");
            }

            if (!EyeRootUsesFaceFollowParent(slot, eyeRoot))
            {
                throw new InvalidOperationException($"{IdleSlotObjectName}/{ApprovedEyeRootName} must use a face follow parent.");
            }

            var eyeRenderers = eyeRoot.GetComponentsInChildren<Renderer>(true);
            if (eyeRenderers.Length < 2)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} must keep the approved left/right eye renderers.");
            }

            var bodyRenderers = GetBodyRenderers(slot, eyeRoot);
            if (bodyRenderers.Count == 0)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} has no body renderers.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathingClipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Idle breathing clip is missing at {IdleBreathingClipAssetPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleBreathingControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Idle breathing AnimatorController is missing at {IdleBreathingControllerAssetPath}.");
            }

            var animator = slot.GetComponent<Animator>();
            if (animator == null || !animator.enabled)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} must have an enabled Animator.");
            }

            if (animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} does not use the idle breathing AnimatorController.");
            }

            var skinnedRendererCount = 0;
            var meshAssetPaths = new List<string>();
            foreach (var renderer in bodyRenderers)
            {
                if (renderer is not SkinnedMeshRenderer skinnedRenderer)
                {
                    throw new InvalidOperationException($"{IdleSlotObjectName}/{renderer.name} is not a SkinnedMeshRenderer, so it cannot play the body morph curve.");
                }

                var mesh = skinnedRenderer.sharedMesh;
                if (mesh == null)
                {
                    throw new InvalidOperationException($"{IdleSlotObjectName}/{renderer.name} has no skinned mesh.");
                }

                var blendShapeIndex = mesh.GetBlendShapeIndex(IdleBreathingBlendShapeName);
                if (blendShapeIndex < 0)
                {
                    throw new InvalidOperationException($"{IdleSlotObjectName}/{renderer.name} is missing blendShape.{IdleBreathingBlendShapeName}.");
                }

                if (mesh.GetBlendShapeFrameCount(blendShapeIndex) <= 0 ||
                    mesh.GetBlendShapeFrameWeight(blendShapeIndex, mesh.GetBlendShapeFrameCount(blendShapeIndex) - 1) < 99.00f)
                {
                    throw new InvalidOperationException($"{IdleSlotObjectName}/{renderer.name} has an invalid idle breathing blend shape frame.");
                }

                var bindingPath = BuildTransformPath(slot, skinnedRenderer.transform);
                if (!ClipHasBlendShapeBinding(clip, bindingPath))
                {
                    throw new InvalidOperationException($"{IdleSlotObjectName}/{renderer.name} has no clip curve bound to blendShape.{IdleBreathingBlendShapeName}.");
                }

                skinnedRendererCount++;
                meshAssetPaths.Add(AssetDatabase.GetAssetPath(mesh));
            }

            var eyeRootPath = BuildTransformPath(slot, eyeRoot);
            if (!ClipHasEyeFollowPositionBindings(clip, eyeRootPath))
            {
                throw new InvalidOperationException($"{IdleSlotObjectName}/{ApprovedEyeRootName} has no idle breathing eye follow position curves.");
            }

            ValidateIdleBreathingNotAppliedToOtherTargets(placementRoot);

            var curveCount = AnimationUtility.GetCurveBindings(clip).Length;
            if (curveCount < skinnedRendererCount)
            {
                throw new InvalidOperationException(
                    $"Idle breathing clip must have at least one blend shape curve per skinned body renderer. Curves={curveCount}, Renderers={skinnedRendererCount}.");
            }

            Debug.Log(
                $"MonstrumIdleBreathingValidation Target={IdleSlotObjectName}, BodyRenderers={bodyRenderers.Count}, " +
                $"SkinnedRenderers={skinnedRendererCount}, CurveCount={curveCount}, Clip={IdleBreathingClipAssetPath}.");

            return new IdleBreathingAnimationResult(
                IdleSlotObjectName,
                bodyRenderers.Count,
                skinnedRendererCount,
                meshAssetPaths.Count,
                string.Join("; ", meshAssetPaths),
                IdleBreathingClipAssetPath,
                IdleBreathingControllerAssetPath,
                CountIdleBreathingBlendShapeBindings(clip),
                CountEyeFollowPositionBindings(clip, eyeRootPath),
                curveCount);
        }

        private static void ValidateIdleSlotTransform(Transform placementRoot, Transform slot)
        {
            var staticObject = RequireStaticReviewObject(placementRoot);
            var staticBounds = CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            var spacing = CalculateAnimationSlotSpacing(staticBounds);
            var expectedPosition = new Vector3(
                staticObject.position.x + spacing,
                staticObject.position.y,
                staticObject.position.z);
            var distance = Vector3.Distance(slot.position, expectedPosition);
            if (distance > 0.02f)
            {
                throw new InvalidOperationException(
                    $"{IdleSlotObjectName} is not in the expected idle review position. Expected={FormatVector(expectedPosition)}, Actual={FormatVector(slot.position)}, Delta={distance:0.###}.");
            }

            if (Quaternion.Angle(slot.rotation, staticObject.rotation) > 0.01f)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} rotation must match {PlacementObjectName}.");
            }
        }

        private static Transform RequireAnimationReviewSlot(Transform placementRoot, string slotObjectName)
        {
            var slot = placementRoot.Find(slotObjectName);
            if (slot == null)
            {
                throw new InvalidOperationException($"{slotObjectName} is missing under {PlacementRootName}.");
            }

            return slot;
        }

        private static Transform RebuildIdleSlotFromStaticReviewObject(Transform placementRoot)
        {
            var existingSlot = RequireAnimationReviewSlot(placementRoot, IdleSlotObjectName);
            var staticObject = RequireStaticReviewObject(placementRoot);
            var position = existingSlot.position;
            var rotation = existingSlot.rotation;
            var localScale = existingSlot.localScale;

            UnityEngine.Object.DestroyImmediate(existingSlot.gameObject);
            var rebuiltSlot = UnityEngine.Object.Instantiate(staticObject.gameObject, placementRoot);
            rebuiltSlot.name = IdleSlotObjectName;
            rebuiltSlot.transform.SetPositionAndRotation(position, rotation);
            rebuiltSlot.transform.localScale = localScale;
            DisableImportedAnimationPlayback(rebuiltSlot.transform);
            EditorUtility.SetDirty(rebuiltSlot);
            return rebuiltSlot.transform;
        }

        private static Mesh CreateOrUpdateIdleBreathingMesh(Mesh sourceMesh, string meshAssetPath, bool preserveReferenceSkinning)
        {
            if (sourceMesh.vertexCount == 0)
            {
                throw new InvalidOperationException($"{sourceMesh.name} has no vertices for idle breathing.");
            }

            var breathingMesh = UnityEngine.Object.Instantiate(sourceMesh);
            breathingMesh.name = Path.GetFileNameWithoutExtension(meshAssetPath);
            breathingMesh.ClearBlendShapes();
            AddIdleBreathingBlendShapeFrame(breathingMesh);
            var copiedReferenceSkinning = preserveReferenceSkinning && CopySkinningData(sourceMesh, breathingMesh);
            if (!copiedReferenceSkinning && (breathingMesh.bindposes.Length == 0 || breathingMesh.boneWeights.Length == 0))
            {
                ApplySingleBoneSkinningForIdleBreathing(breathingMesh);
            }

            breathingMesh.RecalculateBounds();

            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(meshAssetPath);
            }

            AssetDatabase.CreateAsset(breathingMesh, meshAssetPath);
            AssetDatabase.ImportAsset(meshAssetPath, ImportAssetOptions.ForceUpdate);
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            if (savedMesh == null)
            {
                throw new InvalidOperationException($"Failed to create idle breathing mesh asset at {meshAssetPath}.");
            }

            return savedMesh;
        }

        private static bool CopySkinningData(Mesh sourceMesh, Mesh targetMesh)
        {
            targetMesh.bindposes = sourceMesh.bindposes;
            var bonesPerVertex = sourceMesh.GetBonesPerVertex();
            if (bonesPerVertex.Length > 0)
            {
                targetMesh.SetBoneWeights(bonesPerVertex, sourceMesh.GetAllBoneWeights());
                return true;
            }

            var boneWeights = sourceMesh.boneWeights;
            if (boneWeights.Length > 0)
            {
                targetMesh.boneWeights = boneWeights;
                return true;
            }

            return false;
        }

        private static void AddIdleBreathingBlendShapeFrame(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var deltas = new Vector3[vertices.Length];
            var bounds = mesh.bounds;

            for (var i = 0; i < vertices.Length; i++)
            {
                deltas[i] = CalculateIdleBreathingDelta(vertices[i], bounds);
            }

            mesh.AddBlendShapeFrame(IdleBreathingBlendShapeName, 100.00f, deltas, null, null);
        }

        private static Vector3 CalculateIdleBreathingDelta(Vector3 vertex, Bounds bounds)
        {
            var height = Mathf.Max(bounds.size.y, 0.001f);
            var width = Mathf.Max(bounds.size.x, 0.001f);
            var depth = Mathf.Max(bounds.size.z, 0.001f);
            var horizontalScale = Mathf.Clamp(0.0065f, 0.0045f, 0.0085f);
            var depthScale = Mathf.Clamp(0.0120f, 0.0080f, 0.0150f);
            var verticalScale = Mathf.Clamp(0.0180f, 0.0140f, 0.0240f);
            var wholeBodyLift = Mathf.Clamp(height * 0.0075f, 0.0080f, 0.0180f);
            var armLift = Mathf.Clamp(height * 0.0300f, 0.0240f, 0.0550f);
            var center = bounds.center;
            var breathOrigin = new Vector3(center.x, bounds.min.y + height * 0.12f, center.z);
            var halfWidth = Mathf.Max(width * 0.50f, 0.001f);

            var normalizedHeight = Mathf.InverseLerp(bounds.min.y, bounds.max.y, vertex.y);
            var footAnchor = Mathf.SmoothStep(0.00f, 1.00f, Mathf.InverseLerp(0.05f, 0.20f, normalizedHeight));
            var bodyWeight = footAnchor * (1.00f - Mathf.SmoothStep(0.00f, 1.00f, Mathf.InverseLerp(0.94f, 1.00f, normalizedHeight)) * 0.20f);
            var offset = vertex - breathOrigin;
            var bodyExpansion = new Vector3(
                offset.x * horizontalScale,
                offset.y * verticalScale,
                offset.z * depthScale) * bodyWeight;

            var sideWeight = Mathf.SmoothStep(0.00f, 1.00f, Mathf.InverseLerp(0.44f, 0.88f, Mathf.Abs(vertex.x - center.x) / halfWidth));
            var armHeightWeight = Mathf.SmoothStep(0.00f, 1.00f, Mathf.InverseLerp(0.12f, 0.34f, normalizedHeight)) *
                                  (1.00f - Mathf.SmoothStep(0.00f, 1.00f, Mathf.InverseLerp(0.78f, 0.96f, normalizedHeight)));
            var armLiftWeight = sideWeight * armHeightWeight;
            var lift = Vector3.up * (wholeBodyLift * bodyWeight + armLift * armLiftWeight);
            return bodyExpansion + lift;
        }

        private static void ApplySingleBoneSkinningForIdleBreathing(Mesh mesh)
        {
            var boneWeights = new BoneWeight[mesh.vertexCount];
            for (var i = 0; i < boneWeights.Length; i++)
            {
                boneWeights[i].boneIndex0 = 0;
                boneWeights[i].weight0 = 1.00f;
            }

            mesh.boneWeights = boneWeights;
            mesh.bindposes = new[] { Matrix4x4.identity };
        }

        private static SkinnedMeshRenderer EnsureIdleBreathingSkinnedRenderer(
            Renderer renderer,
            Mesh breathingMesh,
            Renderer referenceRenderer,
            Transform staticObject,
            Transform targetSlot)
        {
            if (renderer is SkinnedMeshRenderer existingSkinnedRenderer)
            {
                existingSkinnedRenderer.sharedMesh = breathingMesh;
                existingSkinnedRenderer.updateWhenOffscreen = true;
                ApplyReferenceSkinning(existingSkinnedRenderer, referenceRenderer, staticObject, targetSlot, breathingMesh.bounds);
                EditorUtility.SetDirty(existingSkinnedRenderer);
                return existingSkinnedRenderer;
            }

            if (renderer is not MeshRenderer meshRenderer)
            {
                throw new InvalidOperationException($"{renderer.name} is not a MeshRenderer or SkinnedMeshRenderer.");
            }

            var gameObject = meshRenderer.gameObject;
            var materials = meshRenderer.sharedMaterials;
            var enabled = meshRenderer.enabled;
            var shadowCastingMode = meshRenderer.shadowCastingMode;
            var receiveShadows = meshRenderer.receiveShadows;
            var motionVectorGenerationMode = meshRenderer.motionVectorGenerationMode;
            var lightProbeUsage = meshRenderer.lightProbeUsage;
            var reflectionProbeUsage = meshRenderer.reflectionProbeUsage;
            var probeAnchor = meshRenderer.probeAnchor;
            var allowOcclusionWhenDynamic = meshRenderer.allowOcclusionWhenDynamic;
            var sortingLayerId = meshRenderer.sortingLayerID;
            var sortingOrder = meshRenderer.sortingOrder;

            var skinnedRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer == null)
            {
                skinnedRenderer = gameObject.AddComponent<SkinnedMeshRenderer>();
            }

            skinnedRenderer.sharedMesh = breathingMesh;
            skinnedRenderer.sharedMaterials = materials;
            skinnedRenderer.enabled = enabled;
            skinnedRenderer.shadowCastingMode = shadowCastingMode;
            skinnedRenderer.receiveShadows = receiveShadows;
            skinnedRenderer.motionVectorGenerationMode = motionVectorGenerationMode;
            skinnedRenderer.lightProbeUsage = lightProbeUsage;
            skinnedRenderer.reflectionProbeUsage = reflectionProbeUsage;
            skinnedRenderer.probeAnchor = probeAnchor;
            skinnedRenderer.allowOcclusionWhenDynamic = allowOcclusionWhenDynamic;
            skinnedRenderer.sortingLayerID = sortingLayerId;
            skinnedRenderer.sortingOrder = sortingOrder;
            skinnedRenderer.updateWhenOffscreen = true;
            ApplyReferenceSkinning(skinnedRenderer, referenceRenderer, staticObject, targetSlot, breathingMesh.bounds);

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            UnityEngine.Object.DestroyImmediate(meshRenderer);
            if (meshFilter != null)
            {
                UnityEngine.Object.DestroyImmediate(meshFilter);
            }

            EditorUtility.SetDirty(skinnedRenderer);
            EditorUtility.SetDirty(gameObject);
            return skinnedRenderer;
        }

        private static void ApplyReferenceSkinning(
            SkinnedMeshRenderer targetRenderer,
            Renderer referenceRenderer,
            Transform staticObject,
            Transform targetSlot,
            Bounds fallbackLocalBounds)
        {
            if (referenceRenderer is SkinnedMeshRenderer referenceSkinnedRenderer)
            {
                targetRenderer.bones = RemapReferenceBones(staticObject, targetSlot, referenceSkinnedRenderer.bones);
                targetRenderer.rootBone = RemapReferenceTransform(staticObject, targetSlot, referenceSkinnedRenderer.rootBone);
                if (referenceSkinnedRenderer.rootBone != null && targetRenderer.rootBone == null)
                {
                    throw new InvalidOperationException(
                        $"Could not remap Monstrum idle breathing root bone {referenceSkinnedRenderer.rootBone.name} from {staticObject.name} to {targetSlot.name}.");
                }

                targetRenderer.localBounds = BuildTargetLocalBoundsFromReferenceWorldBounds(
                    targetRenderer,
                    referenceSkinnedRenderer.bounds,
                    staticObject,
                    targetSlot);
                return;
            }

            targetRenderer.bones = new[] { targetRenderer.transform };
            targetRenderer.rootBone = targetRenderer.transform;
            targetRenderer.localBounds = ExpandBoundsForBreathing(fallbackLocalBounds);
        }

        private static Bounds BuildTargetLocalBoundsFromReferenceWorldBounds(
            SkinnedMeshRenderer targetRenderer,
            Bounds referenceWorldBounds,
            Transform staticObject,
            Transform targetSlot)
        {
            var targetWorldBounds = referenceWorldBounds;
            targetWorldBounds.center += targetSlot.position - staticObject.position;
            var localTransform = targetRenderer.rootBone != null ? targetRenderer.rootBone : targetRenderer.transform;
            var corners = new[]
            {
                new Vector3(targetWorldBounds.min.x, targetWorldBounds.min.y, targetWorldBounds.min.z),
                new Vector3(targetWorldBounds.min.x, targetWorldBounds.min.y, targetWorldBounds.max.z),
                new Vector3(targetWorldBounds.min.x, targetWorldBounds.max.y, targetWorldBounds.min.z),
                new Vector3(targetWorldBounds.min.x, targetWorldBounds.max.y, targetWorldBounds.max.z),
                new Vector3(targetWorldBounds.max.x, targetWorldBounds.min.y, targetWorldBounds.min.z),
                new Vector3(targetWorldBounds.max.x, targetWorldBounds.min.y, targetWorldBounds.max.z),
                new Vector3(targetWorldBounds.max.x, targetWorldBounds.max.y, targetWorldBounds.min.z),
                new Vector3(targetWorldBounds.max.x, targetWorldBounds.max.y, targetWorldBounds.max.z)
            };

            var localBounds = new Bounds(localTransform.InverseTransformPoint(corners[0]), Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                localBounds.Encapsulate(localTransform.InverseTransformPoint(corners[i]));
            }

            return ExpandBoundsForBreathing(localBounds);
        }

        private static Transform[] RemapReferenceBones(Transform staticObject, Transform targetSlot, Transform[] referenceBones)
        {
            if (referenceBones == null || referenceBones.Length == 0)
            {
                return new[] { targetSlot };
            }

            var remappedBones = new Transform[referenceBones.Length];
            for (var i = 0; i < referenceBones.Length; i++)
            {
                remappedBones[i] = RemapReferenceTransform(staticObject, targetSlot, referenceBones[i]);
                if (remappedBones[i] == null)
                {
                    throw new InvalidOperationException(
                        $"Could not remap Monstrum idle breathing bone {referenceBones[i].name} from {staticObject.name} to {targetSlot.name}.");
                }
            }

            return remappedBones;
        }

        private static Transform RemapReferenceTransform(Transform staticObject, Transform targetSlot, Transform reference)
        {
            if (reference == null)
            {
                return null;
            }

            if (reference == staticObject)
            {
                return targetSlot;
            }

            if (!reference.IsChildOf(staticObject))
            {
                return null;
            }

            var referencePath = BuildTransformPath(staticObject, reference);
            return string.IsNullOrEmpty(referencePath) ? targetSlot : targetSlot.Find(referencePath);
        }

        private static Bounds ExpandBoundsForBreathing(Bounds sourceBounds)
        {
            sourceBounds.Expand(new Vector3(0.14f, 0.42f, 0.14f));
            return sourceBounds;
        }

        private static AnimationClip CreateOrUpdateIdleBreathingClip(Transform slot, List<SkinnedMeshRenderer> skinnedRenderers)
        {
            var clip = new AnimationClip
            {
                name = "Monstrum_02_Idle_Breathing",
                frameRate = 30.00f,
                wrapMode = WrapMode.Loop
            };

            foreach (var skinnedRenderer in skinnedRenderers)
            {
                var binding = EditorCurveBinding.FloatCurve(
                    BuildTransformPath(slot, skinnedRenderer.transform),
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + IdleBreathingBlendShapeName);
                AnimationUtility.SetEditorCurve(clip, binding, CreateIdleBreathingWeightCurve());
            }

            AddIdleEyeFollowCurves(clip, slot, skinnedRenderers);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathingClipAssetPath);
            if (existingClip != null)
            {
                EditorUtility.CopySerialized(clip, existingClip);
                EditorUtility.SetDirty(existingClip);
                return existingClip;
            }

            AssetDatabase.CreateAsset(clip, IdleBreathingClipAssetPath);
            AssetDatabase.ImportAsset(IdleBreathingClipAssetPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathingClipAssetPath);
        }

        private static void AddIdleEyeFollowCurves(AnimationClip clip, Transform slot, List<SkinnedMeshRenderer> skinnedRenderers)
        {
            var eyeRoot = FindApprovedEyeRoot(slot);
            if (eyeRoot == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} is missing {ApprovedEyeRootName} for idle eye follow curves.");
            }

            var bodyRenderer = FindPrimaryIdleBreathingRenderer(skinnedRenderers);
            var eyeBounds = CalculateRendererBounds(eyeRoot, new Bounds(eyeRoot.position, Vector3.one * 0.01f));
            var localEyeCenter = bodyRenderer.transform.InverseTransformPoint(eyeBounds.center);
            var localEyeDelta = CalculateIdleBreathingDelta(localEyeCenter, bodyRenderer.sharedMesh.bounds);
            var worldEyeDelta = bodyRenderer.transform.TransformVector(localEyeDelta);
            var parentLocalDelta = eyeRoot.parent != null ? eyeRoot.parent.InverseTransformVector(worldEyeDelta) : worldEyeDelta;
            var rest = eyeRoot.localPosition;
            var inhale = rest + parentLocalDelta;
            var path = BuildTransformPath(slot, eyeRoot);

            SetTransformCurve(clip, path, "m_LocalPosition.x", CreateIdleBreathingPositionCurve(rest.x, inhale.x));
            SetTransformCurve(clip, path, "m_LocalPosition.y", CreateIdleBreathingPositionCurve(rest.y, inhale.y));
            SetTransformCurve(clip, path, "m_LocalPosition.z", CreateIdleBreathingPositionCurve(rest.z, inhale.z));
        }

        private static SkinnedMeshRenderer FindPrimaryIdleBreathingRenderer(List<SkinnedMeshRenderer> skinnedRenderers)
        {
            foreach (var skinnedRenderer in skinnedRenderers)
            {
                if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
                {
                    return skinnedRenderer;
                }
            }

            throw new InvalidOperationException($"{IdleSlotObjectName} has no skinned body renderer for idle eye follow curves.");
        }

        private static AnimationCurve CreateIdleBreathingWeightCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, 0.00f),
                new Keyframe(IdleBreathingDurationSeconds * 0.50f, 100.00f),
                new Keyframe(IdleBreathingDurationSeconds, 0.00f));
            curve.preWrapMode = WrapMode.Loop;
            curve.postWrapMode = WrapMode.Loop;
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static AnimationCurve CreateIdleBreathingPositionCurve(float rest, float inhale)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, rest),
                new Keyframe(IdleBreathingDurationSeconds * 0.50f, inhale),
                new Keyframe(IdleBreathingDurationSeconds, rest));
            curve.preWrapMode = WrapMode.Loop;
            curve.postWrapMode = WrapMode.Loop;
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static void SetTransformCurve(AnimationClip clip, string path, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName), curve);
        }

        private static AnimatorController CreateOrUpdateIdleBreathingController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleBreathingControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(IdleBreathingControllerAssetPath);
            }

            if (controller.layers.Length == 0)
            {
                var stateMachine = new AnimatorStateMachine { name = "Base Layer" };
                AssetDatabase.AddObjectToAsset(stateMachine, controller);
                controller.layers = new[]
                {
                    new AnimatorControllerLayer
                    {
                        name = "Base Layer",
                        defaultWeight = 1.00f,
                        stateMachine = stateMachine
                    }
                };
            }

            var rootStateMachine = controller.layers[0].stateMachine;
            foreach (var transition in rootStateMachine.anyStateTransitions)
            {
                rootStateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (var childState in rootStateMachine.states)
            {
                rootStateMachine.RemoveState(childState.state);
            }

            foreach (var childStateMachine in rootStateMachine.stateMachines)
            {
                rootStateMachine.RemoveStateMachine(childStateMachine.stateMachine);
            }

            var idleState = rootStateMachine.AddState("Idle_Breathing");
            idleState.motion = clip;
            idleState.writeDefaultValues = true;
            rootStateMachine.defaultState = idleState;

            EditorUtility.SetDirty(rootStateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(IdleBreathingControllerAssetPath, ImportAssetOptions.ForceUpdate);
            return controller;
        }

        private static bool ClipHasBlendShapeBinding(AnimationClip clip, string bindingPath)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.path == bindingPath &&
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    binding.propertyName == "blendShape." + IdleBreathingBlendShapeName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ClipHasEyeFollowPositionBindings(AnimationClip clip, string bindingPath)
        {
            var hasX = false;
            var hasY = false;
            var hasZ = false;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.path != bindingPath || binding.type != typeof(Transform))
                {
                    continue;
                }

                hasX |= binding.propertyName == "m_LocalPosition.x";
                hasY |= binding.propertyName == "m_LocalPosition.y";
                hasZ |= binding.propertyName == "m_LocalPosition.z";
            }

            return hasX && hasY && hasZ;
        }

        private static int CountIdleBreathingBlendShapeBindings(AnimationClip clip)
        {
            var count = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type == typeof(SkinnedMeshRenderer) &&
                    binding.propertyName == "blendShape." + IdleBreathingBlendShapeName)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEyeFollowPositionBindings(AnimationClip clip, string bindingPath)
        {
            var count = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.path == bindingPath &&
                    binding.type == typeof(Transform) &&
                    (binding.propertyName == "m_LocalPosition.x" ||
                     binding.propertyName == "m_LocalPosition.y" ||
                     binding.propertyName == "m_LocalPosition.z"))
                {
                    count++;
                }
            }

            return count;
        }

        private static void ValidateIdleBreathingNotAppliedToOtherTargets(Transform placementRoot)
        {
            ValidateNoIdleBreathingArtifacts(RequireStaticReviewObject(placementRoot));
            foreach (var spec in AnimationSlotSpecs)
            {
                if (spec.ObjectName == IdleSlotObjectName)
                {
                    continue;
                }

                var slot = placementRoot.Find(spec.ObjectName);
                if (slot != null)
                {
                    ValidateNoIdleBreathingArtifacts(slot);
                }
            }
        }

        private static void ValidateNoIdleBreathingArtifacts(Transform target)
        {
            var animator = target.GetComponent<Animator>();
            if (animator != null &&
                animator.runtimeAnimatorController != null &&
                AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) == IdleBreathingControllerAssetPath)
            {
                throw new InvalidOperationException($"{target.name} unexpectedly uses the Monstrum idle breathing AnimatorController.");
            }

            var eyeRoot = FindApprovedEyeRoot(target);
            foreach (var renderer in GetBodyRenderers(target, eyeRoot))
            {
                var mesh = ResolveAssignedRendererMesh(renderer);
                if (mesh == null)
                {
                    continue;
                }

                var meshPath = AssetDatabase.GetAssetPath(mesh);
                if (mesh.GetBlendShapeIndex(IdleBreathingBlendShapeName) >= 0 ||
                    meshPath.IndexOf("/" + IdleBreathingMeshAssetPrefix, StringComparison.Ordinal) >= 0)
                {
                    throw new InvalidOperationException($"{target.name}/{renderer.name} unexpectedly uses the Monstrum idle breathing mesh.");
                }
            }
        }

        private static void CopyMoveSourceAnimationAsset()
        {
            RequirePreparedModelFile();
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var destinationPath = Path.GetFullPath(Path.Combine(projectRoot, SourceAnimationAssetPath));
            var destinationFolder = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrEmpty(destinationFolder))
            {
                throw new InvalidOperationException("Monstrum source animation destination folder could not be resolved.");
            }

            Directory.CreateDirectory(destinationFolder);
            File.Copy(SourceModelAbsolutePath, destinationPath, true);
            AssetDatabase.ImportAsset(SourceAnimationAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureMoveSourceAnimationImporter()
        {
            AssetDatabase.ImportAsset(SourceAnimationAssetPath, ImportAssetOptions.ForceUpdate);
            var importer = RequireMoveSourceAnimationImporter();

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();

            importer = RequireMoveSourceAnimationImporter();
            EnsureMoveSourceAnimationClipLooping(importer);
            importer.SaveAndReimport();
        }

        private static ModelImporter RequireMoveSourceAnimationImporter()
        {
            var importer = AssetImporter.GetAtPath(SourceAnimationAssetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Could not load ModelImporter for {SourceAnimationAssetPath}.");
            }

            return importer;
        }

        private static void EnsureMoveSourceAnimationClipLooping(ModelImporter importer)
        {
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips == null || clips.Length == 0)
            {
                throw new InvalidOperationException($"{SourceAnimationAssetPath} contains no ModelImporter animation clips to loop.");
            }

            for (var i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = true;
            }

            importer.clipAnimations = clips;
        }

        private static AnimationClip LoadMoveSourceAnimationClip()
        {
            var clips = new List<AnimationClip>();
            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(SourceAnimationAssetPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                {
                    clips.Add(clip);
                }
            }

            if (clips.Count == 0)
            {
                throw new InvalidOperationException($"{SourceAnimationAssetPath} contains no imported AnimationClip from the external Monstrum FBX.");
            }

            clips.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return clips[0];
        }

        private static AnimatorController CreateOrUpdateMoveSourceAnimationController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MoveSourceAnimationControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(MoveSourceAnimationControllerAssetPath);
            }

            EnsureSingleClipController(controller, "Move_Source_HeavyStomp", clip);
            AssetDatabase.ImportAsset(MoveSourceAnimationControllerAssetPath, ImportAssetOptions.ForceUpdate);
            return controller;
        }

        private static void EnsureSingleClipController(AnimatorController controller, string stateName, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                var stateMachine = new AnimatorStateMachine { name = "Base Layer" };
                AssetDatabase.AddObjectToAsset(stateMachine, controller);
                controller.layers = new[]
                {
                    new AnimatorControllerLayer
                    {
                        name = "Base Layer",
                        defaultWeight = 1.00f,
                        stateMachine = stateMachine
                    }
                };
            }

            var rootStateMachine = controller.layers[0].stateMachine;
            foreach (var transition in rootStateMachine.anyStateTransitions)
            {
                rootStateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (var childState in rootStateMachine.states)
            {
                rootStateMachine.RemoveState(childState.state);
            }

            foreach (var childStateMachine in rootStateMachine.stateMachines)
            {
                rootStateMachine.RemoveStateMachine(childStateMachine.stateMachine);
            }

            var state = rootStateMachine.AddState(stateName);
            state.motion = clip;
            state.writeDefaultValues = true;
            rootStateMachine.defaultState = state;
            EditorUtility.SetDirty(rootStateMachine);
            EditorUtility.SetDirty(controller);
        }

        private static MoveSourceAnimationResult ApplyMoveSourceAnimationToSlot(Transform placementRoot, AnimatorController controller, AnimationClip clip)
        {
            var slot = RequireAnimationReviewSlot(placementRoot, MoveSlotObjectName);
            var animatorRoot = SelectBestMoveSourceAnimatorRoot(slot, clip);
            DisableImportedAnimationPlayback(slot);
            var animator = animatorRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = animatorRoot.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(animatorRoot.gameObject);

            var result = BuildMoveSourceAnimationResult(slot, animatorRoot, controller, clip);
            Debug.Log(
                $"MonstrumMoveSourceAnimationApplied Target={MoveSlotObjectName}, AnimatorRoot={BuildTransformPath(slot, animatorRoot)}, " +
                $"Clip={clip.name}, Source={SourceAnimationAssetPath}, BindingCount={AnimationUtility.GetCurveBindings(clip).Length}.");
            return result;
        }

        private static MoveSourceAnimationResult ValidateMoveSourceAnimationOnPlacementRoot(Transform placementRoot)
        {
            var slot = RequireAnimationReviewSlot(placementRoot, MoveSlotObjectName);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MoveSourceAnimationControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Move source AnimatorController is missing at {MoveSourceAnimationControllerAssetPath}.");
            }

            var clip = LoadMoveSourceAnimationClip();
            var animator = FindMoveSourceAnimator(slot, controller);
            if (animator == null)
            {
                throw new InvalidOperationException($"{MoveSlotObjectName} does not have an enabled Animator using {MoveSourceAnimationControllerAssetPath}.");
            }

            if (!ControllerUsesClip(controller, clip))
            {
                throw new InvalidOperationException($"{MoveSourceAnimationControllerAssetPath} does not use source clip {clip.name}.");
            }

            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!clipSettings.loopTime)
            {
                throw new InvalidOperationException($"{SourceAnimationAssetPath}/{clip.name} is not configured for repeated playback.");
            }

            ValidateMoveSourceAnimationNotAppliedToOtherTargets(placementRoot, controller);
            var result = BuildMoveSourceAnimationResult(slot, animator.transform, controller, clip);
            Debug.Log(
                $"MonstrumMoveSourceAnimationValidation Target={MoveSlotObjectName}, AnimatorRoot={result.AnimatorRootPath}, " +
                $"Clip={result.SourceClipName}, BindingCount={result.ClipBindingCount}, LoopTime={result.ClipLoopTime}.");
            return result;
        }

        private static Transform SelectBestMoveSourceAnimatorRoot(Transform slot, AnimationClip clip)
        {
            var candidates = new List<Transform> { slot };
            var model = slot.Find(ModelChildName);
            if (model != null)
            {
                candidates.Add(model);
            }

            foreach (var skinnedRenderer in slot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!candidates.Contains(skinnedRenderer.transform))
                {
                    candidates.Add(skinnedRenderer.transform);
                }
            }

            Transform best = null;
            var bestScore = -1;
            foreach (var candidate in candidates)
            {
                var score = CountResolvedClipBindings(candidate, clip);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best == null || bestScore <= 0)
            {
                throw new InvalidOperationException($"{MoveSlotObjectName} could not resolve source animation clip bindings.");
            }

            return best;
        }

        private static int CountResolvedClipBindings(Transform root, AnimationClip clip)
        {
            var resolvedPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (string.IsNullOrEmpty(binding.path))
                {
                    resolvedPaths.Add(string.Empty);
                    continue;
                }

                if (root.Find(binding.path) != null)
                {
                    resolvedPaths.Add(binding.path);
                }
            }

            return resolvedPaths.Count;
        }

        private static Animator FindMoveSourceAnimator(Transform slot, RuntimeAnimatorController controller)
        {
            foreach (var animator in slot.GetComponentsInChildren<Animator>(true))
            {
                if (animator.enabled && animator.runtimeAnimatorController == controller)
                {
                    return animator;
                }
            }

            return null;
        }

        private static bool ControllerUsesClip(AnimatorController controller, AnimationClip clip)
        {
            foreach (var controllerClip in controller.animationClips)
            {
                if (controllerClip == clip)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateMoveSourceAnimationNotAppliedToOtherTargets(Transform placementRoot, RuntimeAnimatorController controller)
        {
            ValidateMoveSourceControllerAbsent(RequireStaticReviewObject(placementRoot), controller);
            foreach (var spec in AnimationSlotSpecs)
            {
                if (spec.ObjectName == MoveSlotObjectName)
                {
                    continue;
                }

                var slot = placementRoot.Find(spec.ObjectName);
                if (slot != null)
                {
                    ValidateMoveSourceControllerAbsent(slot, controller);
                }
            }
        }

        private static void ValidateMoveSourceControllerAbsent(Transform target, RuntimeAnimatorController controller)
        {
            foreach (var animator in target.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == controller)
                {
                    throw new InvalidOperationException($"{target.name} unexpectedly uses the Monstrum move source AnimatorController.");
                }
            }
        }

        private static MoveSourceAnimationResult BuildMoveSourceAnimationResult(
            Transform slot,
            Transform animatorRoot,
            AnimatorController controller,
            AnimationClip clip)
        {
            return new MoveSourceAnimationResult(
                MoveSlotObjectName,
                BuildTransformPath(slot, animatorRoot),
                SourceModelAbsolutePath,
                SourceAnimationAssetPath,
                AssetDatabase.GetAssetPath(clip),
                clip.name,
                MoveSourceAnimationControllerAssetPath,
                AnimationUtility.GetCurveBindings(clip).Length,
                AnimationUtility.GetObjectReferenceCurveBindings(clip).Length,
                AnimationUtility.GetAnimationClipSettings(clip).loopTime,
                clip.isLooping);
        }

        private static Mesh ResolveAssignedRendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static Renderer ResolveReferenceBodyRenderer(Transform staticObject, Transform slot, Renderer slotRenderer)
        {
            var rendererPath = BuildTransformPath(slot, slotRenderer.transform);
            var staticRendererTransform = string.IsNullOrEmpty(rendererPath) ? staticObject : staticObject.Find(rendererPath);
            if (staticRendererTransform == null)
            {
                return null;
            }

            var staticRenderer = staticRendererTransform.GetComponent<Renderer>();
            if (staticRenderer == null)
            {
                return null;
            }

            var mesh = ResolveAssignedRendererMesh(staticRenderer);
            return mesh != null ? staticRenderer : null;
        }

        private static string BuildIdleBreathingMeshAssetPath(Transform slot, Renderer renderer, Mesh sourceMesh)
        {
            var rendererPath = BuildTransformPath(slot, renderer.transform);
            if (string.IsNullOrEmpty(rendererPath))
            {
                rendererPath = renderer.name;
            }

            var sourceName = sourceMesh.name;
            while (sourceName.StartsWith(IdleBreathingMeshAssetPrefix, StringComparison.Ordinal))
            {
                sourceName = sourceName.Substring(IdleBreathingMeshAssetPrefix.Length);
            }

            return UnityMeshFolder + "/" + IdleBreathingMeshAssetPrefix + SanitizeAssetName(rendererPath + "_" + sourceName) + ".asset";
        }

        private static string BuildTransformPath(Transform root, Transform target)
        {
            if (root == target)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (current != root)
            {
                throw new InvalidOperationException($"{target.name} is not a child of {root.name}.");
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static LooseGrainRemovalResult ApplyLooseGrainRemovalToPlacementRoot(Transform placementRoot)
        {
            var targets = CollectMeshAssignmentTargets(placementRoot);
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("No Monstrum mesh targets were found for loose grain removal.");
            }

            var cleanedMeshesBySource = new Dictionary<Mesh, Mesh>();
            var builder = new LooseGrainRemovalResultBuilder();
            foreach (var target in targets)
            {
                var sourceMesh = ResolveLooseGrainSourceMesh(target.SharedMesh);
                if (sourceMesh == null)
                {
                    continue;
                }

                if (!cleanedMeshesBySource.TryGetValue(sourceMesh, out var cleanedMesh))
                {
                    var meshAssetPath = BuildLooseGrainRemovedMeshAssetPath(sourceMesh);
                    var cleanup = BuildLooseGrainRemovedMesh(sourceMesh, target.Transform, meshAssetPath);
                    cleanedMesh = cleanup.CleanedMesh;
                    SaveCleanedMeshAsset(cleanedMesh, meshAssetPath);
                    cleanedMeshesBySource[sourceMesh] = cleanedMesh;
                    builder.AddCleanup(cleanup);
                }
            }

            Mesh fallbackCleanedMesh = null;
            foreach (var cleanedMesh in cleanedMeshesBySource.Values)
            {
                fallbackCleanedMesh = cleanedMesh;
                break;
            }

            if (fallbackCleanedMesh == null)
            {
                fallbackCleanedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(UnityMeshFolder + "/Monstrum_LooseGrainRemoved_char1.asset");
            }

            if (fallbackCleanedMesh == null)
            {
                throw new InvalidOperationException("Could not resolve a cleaned Monstrum mesh for assignment.");
            }

            foreach (var target in targets)
            {
                var sourceMesh = target.SharedMesh;
                var cleanedMesh = sourceMesh != null && cleanedMeshesBySource.TryGetValue(sourceMesh, out var resolvedMesh)
                    ? resolvedMesh
                    : fallbackCleanedMesh;
                target.Assign(cleanedMesh);
                builder.AssignedRendererCount++;
            }

            var result = builder.Build();
            Debug.Log(
                "MonstrumLooseGrainRemoval " +
                $"SourceMeshes={result.SourceMeshCount}, AssignedRenderers={result.AssignedRendererCount}, " +
                $"RemovedComponents={result.RemovedComponentCount}, RemovedTriangles={result.RemovedTriangleCount}, " +
                $"KeptTriangles={result.KeptTriangleCount}, CleanedMeshAsset={result.CleanedMeshAssetPaths}.");
            return result;
        }

        private static Mesh ResolveLooseGrainSourceMesh(Mesh currentMesh)
        {
            if (currentMesh == null)
            {
                return null;
            }

            var currentPath = AssetDatabase.GetAssetPath(currentMesh);
            if (!string.IsNullOrEmpty(currentPath) && currentPath.StartsWith(UnityMeshFolder, StringComparison.Ordinal))
            {
                var originalName = currentMesh.name;
                const string prefix = "Monstrum_LooseGrainRemoved_";
                while (originalName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    originalName = originalName.Substring(prefix.Length);
                }

                var importedMeshes = AssetDatabase.LoadAllAssetsAtPath(UnityModelAssetPath);
                Mesh firstImportedMesh = null;
                foreach (var importedAsset in importedMeshes)
                {
                    if (importedAsset is not Mesh importedMesh)
                    {
                        continue;
                    }

                    firstImportedMesh ??= importedMesh;
                    if (string.Equals(importedMesh.name, originalName, StringComparison.Ordinal))
                    {
                        return importedMesh;
                    }
                }

                if (firstImportedMesh != null)
                {
                    return firstImportedMesh;
                }
            }

            return currentMesh;
        }

        private static LooseGrainRemovalResult InspectLooseGrainRemovalOnPlacementRoot(Transform placementRoot)
        {
            var targets = CollectMeshAssignmentTargets(placementRoot);
            if (targets.Count == 0)
            {
                throw new InvalidOperationException("No Monstrum mesh targets were found for loose grain removal validation.");
            }

            var cleanedMeshPaths = new HashSet<string>();
            foreach (var target in targets)
            {
                var mesh = target.SharedMesh;
                if (mesh == null)
                {
                    throw new InvalidOperationException($"{target.OwnerName} has no assigned mesh.");
                }

                var path = AssetDatabase.GetAssetPath(mesh);
                if (string.IsNullOrEmpty(path) || !path.StartsWith(UnityMeshFolder, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"{target.OwnerName} does not use a cleaned Monstrum mesh. MeshPath={path}.");
                }

                cleanedMeshPaths.Add(path);
            }

            var result = new LooseGrainRemovalResult(
                cleanedMeshPaths.Count,
                targets.Count,
                0,
                0,
                0,
                string.Join(", ", cleanedMeshPaths));
            Debug.Log(
                $"MonstrumLooseGrainRemovalInspection CleanedMeshes={result.SourceMeshCount}, AssignedRenderers={result.AssignedRendererCount}, MeshAssets={result.CleanedMeshAssetPaths}.");
            return result;
        }

        private static void ValidateLooseGrainRemovalOnPlacementRoot(Transform placementRoot, LooseGrainRemovalResult result)
        {
            if (result.SourceMeshCount <= 0)
            {
                throw new InvalidOperationException("Loose grain removal did not create any cleaned mesh assets.");
            }

            if (result.AssignedRendererCount <= 0)
            {
                throw new InvalidOperationException("Loose grain removal did not assign any renderers.");
            }

            InspectLooseGrainRemovalOnPlacementRoot(placementRoot);
            InspectAnimationReviewSlots(placementRoot);
        }

        private static List<MeshAssignmentTarget> CollectMeshAssignmentTargets(Transform placementRoot)
        {
            var targets = new List<MeshAssignmentTarget>();
            foreach (var reviewObject in EnumerateMonstrumReviewObjects(placementRoot))
            {
                foreach (var meshFilter in reviewObject.GetComponentsInChildren<MeshFilter>(true))
                {
                    targets.Add(MeshAssignmentTarget.ForMeshFilter(meshFilter));
                }

                foreach (var skinnedRenderer in reviewObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    targets.Add(MeshAssignmentTarget.ForSkinnedMeshRenderer(skinnedRenderer));
                }
            }

            return targets;
        }

        private static IEnumerable<Transform> EnumerateMonstrumReviewObjects(Transform placementRoot)
        {
            var staticObject = RequireStaticReviewObject(placementRoot);
            yield return staticObject;

            foreach (var spec in AnimationSlotSpecs)
            {
                var slot = placementRoot.Find(spec.ObjectName);
                if (slot != null)
                {
                    yield return slot;
                }
            }
        }

        private static MeshCleanupResult BuildLooseGrainRemovedMesh(Mesh sourceMesh, Transform sourceTransform, string meshAssetPath)
        {
            if (sourceMesh.vertexCount == 0)
            {
                throw new InvalidOperationException($"{sourceMesh.name} contains no vertices.");
            }

            var vertices = sourceMesh.vertices;
            var worldVertices = new Vector3[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                worldVertices[i] = sourceTransform.TransformPoint(vertices[i]);
            }

            var triangles = BuildMeshTriangles(sourceMesh, vertices, worldVertices);
            if (triangles.Count == 0)
            {
                throw new InvalidOperationException($"{sourceMesh.name} contains no triangle submeshes.");
            }

            var components = BuildConnectedTriangleComponents(vertices, worldVertices, triangles);
            var mainComponent = FindMainBodyComponent(components);
            var mainWorldVertices = BuildComponentWorldVertexList(mainComponent, worldVertices);
            var keptTriangles = new HashSet<int>();
            var removedComponentCount = 0;
            var removedTriangleCount = 0;

            foreach (var component in components)
            {
                var keep = component == mainComponent || !ShouldRemoveLooseGrainComponent(component, mainComponent, mainWorldVertices);
                if (keep)
                {
                    foreach (var triangleIndex in component.TriangleIndices)
                    {
                        keptTriangles.Add(triangleIndex);
                    }
                }
                else
                {
                    removedComponentCount++;
                    removedTriangleCount += component.TriangleIndices.Count;
                }
            }

            var cleanedMesh = CreateRemappedMesh(sourceMesh, triangles, keptTriangles);
            cleanedMesh.name = Path.GetFileNameWithoutExtension(meshAssetPath);
            return new MeshCleanupResult(
                sourceMesh.name,
                meshAssetPath,
                cleanedMesh,
                components.Count,
                removedComponentCount,
                removedTriangleCount,
                keptTriangles.Count,
                triangles.Count);
        }

        private static List<MeshTriangle> BuildMeshTriangles(Mesh sourceMesh, Vector3[] vertices, Vector3[] worldVertices)
        {
            var triangles = new List<MeshTriangle>();
            for (var submesh = 0; submesh < sourceMesh.subMeshCount; submesh++)
            {
                if (sourceMesh.GetTopology(submesh) != MeshTopology.Triangles)
                {
                    continue;
                }

                var indices = sourceMesh.GetTriangles(submesh);
                for (var index = 0; index + 2 < indices.Length; index += 3)
                {
                    var a = indices[index];
                    var b = indices[index + 1];
                    var c = indices[index + 2];
                    var area = CalculateTriangleArea(worldVertices[a], worldVertices[b], worldVertices[c]);
                    var bounds = new Bounds(worldVertices[a], Vector3.zero);
                    bounds.Encapsulate(worldVertices[b]);
                    bounds.Encapsulate(worldVertices[c]);
                    triangles.Add(new MeshTriangle(submesh, a, b, c, area, bounds));
                }
            }

            return triangles;
        }

        private static List<TriangleComponent> BuildConnectedTriangleComponents(Vector3[] vertices, Vector3[] worldVertices, List<MeshTriangle> triangles)
        {
            var disjointSet = new DisjointSet();
            var nodeByPosition = new Dictionary<QuantizedVector3, int>();
            var triangleRootNodes = new int[triangles.Count];

            for (var triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                var triangle = triangles[triangleIndex];
                var nodeA = GetOrCreatePositionNode(vertices[triangle.A], nodeByPosition, disjointSet);
                var nodeB = GetOrCreatePositionNode(vertices[triangle.B], nodeByPosition, disjointSet);
                var nodeC = GetOrCreatePositionNode(vertices[triangle.C], nodeByPosition, disjointSet);
                disjointSet.Union(nodeA, nodeB);
                disjointSet.Union(nodeB, nodeC);
                triangleRootNodes[triangleIndex] = nodeA;
            }

            var componentByRoot = new Dictionary<int, TriangleComponent>();
            for (var triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                var root = disjointSet.Find(triangleRootNodes[triangleIndex]);
                if (!componentByRoot.TryGetValue(root, out var component))
                {
                    component = new TriangleComponent();
                    componentByRoot.Add(root, component);
                }

                var triangle = triangles[triangleIndex];
                component.AddTriangle(triangleIndex, triangle, worldVertices);
            }

            return new List<TriangleComponent>(componentByRoot.Values);
        }

        private static int GetOrCreatePositionNode(Vector3 position, Dictionary<QuantizedVector3, int> nodeByPosition, DisjointSet disjointSet)
        {
            var key = new QuantizedVector3(position, LooseGrainPositionWeldTolerance);
            if (nodeByPosition.TryGetValue(key, out var node))
            {
                return node;
            }

            node = disjointSet.Add();
            nodeByPosition.Add(key, node);
            return node;
        }

        private static TriangleComponent FindMainBodyComponent(List<TriangleComponent> components)
        {
            if (components.Count == 0)
            {
                throw new InvalidOperationException("Monstrum mesh has no connected triangle components.");
            }

            var mainComponent = components[0];
            for (var i = 1; i < components.Count; i++)
            {
                if (components[i].WorldArea > mainComponent.WorldArea)
                {
                    mainComponent = components[i];
                }
            }

            return mainComponent;
        }

        private static List<Vector3> BuildComponentWorldVertexList(TriangleComponent component, Vector3[] worldVertices)
        {
            var points = new List<Vector3>(component.VertexIndices.Count);
            foreach (var vertexIndex in component.VertexIndices)
            {
                points.Add(worldVertices[vertexIndex]);
            }

            return points;
        }

        private static bool ShouldRemoveLooseGrainComponent(
            TriangleComponent component,
            TriangleComponent mainComponent,
            List<Vector3> mainWorldVertices)
        {
            var areaRatio = mainComponent.WorldArea > 0.000001f ? component.WorldArea / mainComponent.WorldArea : 0f;
            var maxExtent = MaxComponent(component.WorldBounds.size);
            return areaRatio <= LooseGrainMaximumAreaRatio && maxExtent <= LooseGrainMaximumWorldExtentMeters;
        }

        private static float CalculateMinimumDistanceToMainComponent(TriangleComponent component, List<Vector3> mainWorldVertices)
        {
            var minDistanceSqr = float.PositiveInfinity;
            foreach (var vertex in component.WorldVertices)
            {
                for (var i = 0; i < mainWorldVertices.Count; i++)
                {
                    var distanceSqr = (vertex - mainWorldVertices[i]).sqrMagnitude;
                    if (distanceSqr < minDistanceSqr)
                    {
                        minDistanceSqr = distanceSqr;
                    }

                    if (minDistanceSqr <= LooseGrainMinimumGapFromBodyMeters * LooseGrainMinimumGapFromBodyMeters)
                    {
                        return Mathf.Sqrt(minDistanceSqr);
                    }
                }
            }

            return Mathf.Sqrt(minDistanceSqr);
        }

        private static Mesh CreateRemappedMesh(Mesh sourceMesh, List<MeshTriangle> triangles, HashSet<int> keptTriangles)
        {
            var sourceVertexCount = sourceMesh.vertexCount;
            var sourceVertices = sourceMesh.vertices;
            var oldToNew = new int[sourceVertexCount];
            for (var i = 0; i < oldToNew.Length; i++)
            {
                oldToNew[i] = -1;
            }

            var newVertices = new List<Vector3>();
            for (var triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                if (!keptTriangles.Contains(triangleIndex))
                {
                    continue;
                }

                RegisterRemappedVertex(triangles[triangleIndex].A, oldToNew, newVertices, sourceVertices);
                RegisterRemappedVertex(triangles[triangleIndex].B, oldToNew, newVertices, sourceVertices);
                RegisterRemappedVertex(triangles[triangleIndex].C, oldToNew, newVertices, sourceVertices);
            }

            var cleanedMesh = new Mesh
            {
                indexFormat = sourceMesh.indexFormat
            };
            cleanedMesh.SetVertices(newVertices);
            CopyMeshAttributes(sourceMesh, cleanedMesh, oldToNew);
            cleanedMesh.subMeshCount = sourceMesh.subMeshCount;

            for (var submesh = 0; submesh < sourceMesh.subMeshCount; submesh++)
            {
                var newIndices = new List<int>();
                for (var triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
                {
                    if (!keptTriangles.Contains(triangleIndex))
                    {
                        continue;
                    }

                    var triangle = triangles[triangleIndex];
                    if (triangle.Submesh != submesh)
                    {
                        continue;
                    }

                    newIndices.Add(oldToNew[triangle.A]);
                    newIndices.Add(oldToNew[triangle.B]);
                    newIndices.Add(oldToNew[triangle.C]);
                }

                cleanedMesh.SetTriangles(newIndices, submesh, true);
            }

            cleanedMesh.bindposes = sourceMesh.bindposes;
            cleanedMesh.RecalculateBounds();
            return cleanedMesh;
        }

        private static void RegisterRemappedVertex(int sourceIndex, int[] oldToNew, List<Vector3> newVertices, Vector3[] sourceVertices)
        {
            if (oldToNew[sourceIndex] >= 0)
            {
                return;
            }

            oldToNew[sourceIndex] = newVertices.Count;
            newVertices.Add(sourceVertices[sourceIndex]);
        }

        private static void CopyMeshAttributes(Mesh sourceMesh, Mesh cleanedMesh, int[] oldToNew)
        {
            CopyVector3Attribute(sourceMesh.normals, cleanedMesh.SetNormals, oldToNew);
            CopyVector4Attribute(sourceMesh.tangents, cleanedMesh.SetTangents, oldToNew);
            CopyVector2Attribute(sourceMesh.uv, cleanedMesh.SetUVs, 0, oldToNew);
            CopyVector2Attribute(sourceMesh.uv2, cleanedMesh.SetUVs, 1, oldToNew);
            CopyVector2Attribute(sourceMesh.uv3, cleanedMesh.SetUVs, 2, oldToNew);
            CopyVector2Attribute(sourceMesh.uv4, cleanedMesh.SetUVs, 3, oldToNew);
            CopyColors(sourceMesh.colors, cleanedMesh, oldToNew);
            CopyBoneWeights(sourceMesh.boneWeights, cleanedMesh, oldToNew);
        }

        private static void CopyVector3Attribute(Vector3[] source, Action<List<Vector3>> assign, int[] oldToNew)
        {
            if (source == null || source.Length != oldToNew.Length)
            {
                return;
            }

            var values = new Vector3[CountRemappedVertices(oldToNew)];
            for (var oldIndex = 0; oldIndex < oldToNew.Length; oldIndex++)
            {
                var newIndex = oldToNew[oldIndex];
                if (newIndex >= 0)
                {
                    values[newIndex] = source[oldIndex];
                }
            }

            assign(new List<Vector3>(values));
        }

        private static void CopyVector4Attribute(Vector4[] source, Action<List<Vector4>> assign, int[] oldToNew)
        {
            if (source == null || source.Length != oldToNew.Length)
            {
                return;
            }

            var values = new Vector4[CountRemappedVertices(oldToNew)];
            for (var oldIndex = 0; oldIndex < oldToNew.Length; oldIndex++)
            {
                var newIndex = oldToNew[oldIndex];
                if (newIndex >= 0)
                {
                    values[newIndex] = source[oldIndex];
                }
            }

            assign(new List<Vector4>(values));
        }

        private static void CopyVector2Attribute(Vector2[] source, Action<int, List<Vector2>> assign, int channel, int[] oldToNew)
        {
            if (source == null || source.Length != oldToNew.Length)
            {
                return;
            }

            var values = new Vector2[CountRemappedVertices(oldToNew)];
            for (var oldIndex = 0; oldIndex < oldToNew.Length; oldIndex++)
            {
                var newIndex = oldToNew[oldIndex];
                if (newIndex >= 0)
                {
                    values[newIndex] = source[oldIndex];
                }
            }

            assign(channel, new List<Vector2>(values));
        }

        private static void CopyColors(Color[] source, Mesh cleanedMesh, int[] oldToNew)
        {
            if (source == null || source.Length != oldToNew.Length)
            {
                return;
            }

            var values = new Color[CountRemappedVertices(oldToNew)];
            for (var oldIndex = 0; oldIndex < oldToNew.Length; oldIndex++)
            {
                var newIndex = oldToNew[oldIndex];
                if (newIndex >= 0)
                {
                    values[newIndex] = source[oldIndex];
                }
            }

            cleanedMesh.SetColors(new List<Color>(values));
        }

        private static void CopyBoneWeights(BoneWeight[] source, Mesh cleanedMesh, int[] oldToNew)
        {
            if (source == null || source.Length != oldToNew.Length)
            {
                return;
            }

            var values = new BoneWeight[CountRemappedVertices(oldToNew)];
            for (var oldIndex = 0; oldIndex < oldToNew.Length; oldIndex++)
            {
                var newIndex = oldToNew[oldIndex];
                if (newIndex >= 0)
                {
                    values[newIndex] = source[oldIndex];
                }
            }

            cleanedMesh.boneWeights = values;
        }

        private static int CountRemappedVertices(int[] oldToNew)
        {
            var count = 0;
            for (var i = 0; i < oldToNew.Length; i++)
            {
                if (oldToNew[i] >= 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SaveCleanedMeshAsset(Mesh cleanedMesh, string meshAssetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(meshAssetPath);
            }

            AssetDatabase.CreateAsset(cleanedMesh, meshAssetPath);
            AssetDatabase.ImportAsset(meshAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static string BuildLooseGrainRemovedMeshAssetPath(Mesh sourceMesh)
        {
            var sourceName = sourceMesh.name;
            const string prefix = "Monstrum_LooseGrainRemoved_";
            while (sourceName.StartsWith(prefix, StringComparison.Ordinal))
            {
                sourceName = sourceName.Substring(prefix.Length);
            }

            return UnityMeshFolder + "/" + prefix + SanitizeAssetName(sourceName) + ".asset";
        }

        private static string SanitizeAssetName(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                builder.Append(char.IsLetterOrDigit(character) || character == '_' || character == '-' ? character : '_');
            }

            return builder.Length > 0 ? builder.ToString() : "Mesh";
        }

        private static float CalculateTriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
        }

        private static float MaxComponent(Vector3 value)
        {
            return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
        }

        private static PlayerStartInspection MoveExistingPlayerStartToMonstrumFront(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = FindMonstrumCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = CalculatePlayerLookAt(bounds);
            var frontDirection = CalculateMonstrumVisualFrontDirection(focus);
            var playerSideDirection = -frontDirection;
            var distance = CalculatePlayerReviewDistance(bounds);
            var previousPosition = player.position;
            var startPosition = new Vector3(
                lookAt.x + playerSideDirection.x * distance,
                0f,
                lookAt.z + playerSideDirection.z * distance);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);

            var inspection = BuildPlayerStartInspection(player, lookAt, playerSideDirection, previousPosition, distance);
            Debug.Log(
                "MonstrumPlayerStartUpdate " +
                $"Previous={FormatVector(previousPosition)}, New={FormatVector(player.position)}, " +
                $"LookAt={FormatVector(lookAt)}, Distance={distance:0.###}, FacingDot={inspection.FacingDot:0.###}, FrontSideDot={inspection.FrontSideDot:0.###}.");
            return inspection;
        }

        private static PlayerStartInspection InspectPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Player start transform is missing.");
            }

            var focus = FindMonstrumCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = CalculatePlayerLookAt(bounds);
            var playerSideDirection = -CalculateMonstrumVisualFrontDirection(focus);
            var inspection = BuildPlayerStartInspection(player, lookAt, playerSideDirection, player.position, CalculatePlayerReviewDistance(bounds));

            if (inspection.FacingDot < 0.95f)
            {
                throw new InvalidOperationException(
                    $"Player start transform is not facing Monstrum. FacingDot={inspection.FacingDot:0.###}.");
            }

            if (inspection.FrontSideDot < 0.85f)
            {
                throw new InvalidOperationException(
                    $"Player start transform is not on the Monstrum front review side. FrontSideDot={inspection.FrontSideDot:0.###}.");
            }

            Debug.Log(
                "MonstrumPlayerStartInspection " +
                $"Position={FormatVector(player.position)}, RotationY={player.eulerAngles.y:0.###}, " +
                $"LookAt={FormatVector(lookAt)}, HorizontalDistance={inspection.HorizontalDistance:0.###}, " +
                $"FacingDot={inspection.FacingDot:0.###}, FrontSideDot={inspection.FrontSideDot:0.###}.");
            return inspection;
        }

        private static void ScaleToTargetHeightAndAlignToGround(Transform root, float groundY)
        {
            var bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            if (bounds.size.y > 0.0001f)
            {
                var scaleFactor = Mathf.Clamp(MonstrumTargetHeightMeters / bounds.size.y, 0.001f, 100f);
                root.localScale = Vector3.one * scaleFactor;
            }

            bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            root.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static void ConfigureCaptureCamera(Camera camera, Transform focus, Bounds bounds)
        {
            var frontDirection = CalculateMonstrumVisualFrontDirection(focus);
            var sideBias = Quaternion.Euler(0f, 18f, 0f) * frontDirection;
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.08f, 0.28f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 3.30f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var position = lookAt + sideBias.normalized * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.18f, 0.15f, 0.55f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.35f, 1.35f);
            camera.nearClipPlane = Mathf.Max(0.01f, distance - bounds.extents.magnitude - 0.75f);
            camera.farClipPlane = distance + bounds.extents.magnitude + 0.75f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void CaptureEyeCloseupImage(Transform focus, Bounds eyeBounds, Vector3 viewDirection, string outputPath)
        {
            var cameraObject = new GameObject("MonstrumEyeCloseup_CaptureCamera");
            var keyLightObject = new GameObject("MonstrumEyeCloseup_KeyLight");
            var fillLightObject = new GameObject("MonstrumEyeCloseup_FillLight");
            Texture2D texture = null;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                var view = viewDirection.sqrMagnitude > 0.001f ? viewDirection.normalized : CalculateMonstrumVisualFrontDirection(focus);
                var lookAt = eyeBounds.center;
                var distance = 1.10f;
                camera.transform.SetPositionAndRotation(
                    lookAt + view * distance + Vector3.up * 0.015f,
                    Quaternion.LookRotation((lookAt - (lookAt + view * distance + Vector3.up * 0.015f)).normalized, Vector3.up));
                camera.orthographic = true;
                camera.orthographicSize = 0.165f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 3.00f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);

                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.intensity = 1.65f;
                keyLight.transform.rotation = Quaternion.LookRotation(-view + Vector3.down * 0.35f, Vector3.up);

                var fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = SampleEyeGlowColor;
                fillLight.intensity = 0.12f;
                fillLight.range = 0.55f;
                fillLight.transform.position = lookAt + view * 0.25f + Vector3.up * 0.08f;

                texture = CaptureCameraTexture(camera, 1200, 900);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
            }

            Debug.Log("MonstrumEyeCloseupCapture Path=" + outputPath);
        }

        private static Texture2D CaptureCameraTexture(Camera camera, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Material CreateArtSampleBodyMaterial(Texture2D bodyTexture)
        {
            var material = new Material(FindArtSampleShader())
            {
                name = "ArtSample_Monstrum_DarkMossBody"
            };
            SetMaterialColor(material, SampleDarkMossBodyColor);
            SetMaterialTextureIfPresent(material, "_BaseMap", bodyTexture);
            SetMaterialTextureIfPresent(material, "_MainTex", bodyTexture);
            SetMaterialFloatIfPresent(material, "_Metallic", 0f);
            SetMaterialFloatIfPresent(material, "_Smoothness", 0.24f);
            SetMaterialFloatIfPresent(material, "_Roughness", 0.86f);
            return material;
        }

        private static Material CreateOrUpdateApprovedBodyMaterialAsset()
        {
            EnsureUnityFolder(UnityMaterialFolder);
            var bodyTexture = CreateOrUpdateApprovedBodyTextureAsset();
            var material = AssetDatabase.LoadAssetAtPath<Material>(ApprovedBodyMaterialAssetPath);
            if (material == null)
            {
                material = new Material(FindArtSampleShader())
                {
                    name = "Monstrum_Approved_DarkMossBody"
                };
                AssetDatabase.CreateAsset(material, ApprovedBodyMaterialAssetPath);
            }
            else
            {
                material.shader = FindArtSampleShader();
            }

            SetMaterialColor(material, SampleDarkMossBodyColor);
            SetMaterialTextureIfPresent(material, "_BaseMap", bodyTexture);
            SetMaterialTextureIfPresent(material, "_MainTex", bodyTexture);
            SetMaterialFloatIfPresent(material, "_Metallic", 0f);
            SetMaterialFloatIfPresent(material, "_Smoothness", 0.24f);
            SetMaterialFloatIfPresent(material, "_Roughness", 0.86f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateOrUpdateApprovedEyeMaterialAsset()
        {
            EnsureUnityFolder(UnityMaterialFolder);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ApprovedEyeMaterialAssetPath);
            if (material == null)
            {
                material = new Material(FindArtSampleShader())
                {
                    name = "Monstrum_Approved_YellowEye"
                };
                AssetDatabase.CreateAsset(material, ApprovedEyeMaterialAssetPath);
            }
            else
            {
                material.shader = FindArtSampleShader();
            }

            SetMaterialColor(material, SampleEyeGlowColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", SampleEyeGlowColor * 1.10f);
                material.EnableKeyword("_EMISSION");
            }

            SetMaterialFloatIfPresent(material, "_Metallic", 0f);
            SetMaterialFloatIfPresent(material, "_Smoothness", 0.22f);
            SetMaterialFloatIfPresent(material, "_Cull", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CreateOrUpdateApprovedBodyTextureAsset()
        {
            EnsureUnityFolder(UnityMaterialFolder);
            EnsureUnityFolder(UnityMaterialTextureFolder);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var texturePath = Path.GetFullPath(Path.Combine(projectRoot, ApprovedBodyTextureAssetPath));
            var texture = CreateDarkMossBodyTexture("Monstrum_Approved_DarkMossBody_Albedo");
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ApprovedBodyTextureAssetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(ApprovedBodyTextureAssetPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureType = TextureImporterType.Default;
                importer.SaveAndReimport();
            }

            var importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedBodyTextureAssetPath);
            if (importedTexture == null)
            {
                throw new InvalidOperationException($"Could not create approved Monstrum body texture at {ApprovedBodyTextureAssetPath}.");
            }

            return importedTexture;
        }

        private static Texture2D CreateArtSampleBodyTexture(string outputDirectory)
        {
            var texture = CreateDarkMossBodyTexture("ArtSample_Monstrum_DarkMossBodyTexture");
            File.WriteAllBytes(
                Path.Combine(outputDirectory, "textures", "monstrum_dark_moss_body_albedo.png"),
                texture.EncodeToPNG());
            return texture;
        }

        private static Texture2D CreateDarkMossBodyTexture(string textureName)
        {
            const int textureSize = 512;
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            var highlight = new Color(0.115f, 0.235f, 0.085f, 1f);
            for (var y = 0; y < textureSize; y++)
            {
                for (var x = 0; x < textureSize; x++)
                {
                    var large = Mathf.PerlinNoise(x * 0.018f + 17.3f, y * 0.018f + 5.7f);
                    var medium = Mathf.PerlinNoise(x * 0.043f + 31.8f, y * 0.043f + 9.4f);
                    var fine = Mathf.PerlinNoise(x * 0.112f + 4.1f, y * 0.112f + 21.6f);
                    var verticalStain = Mathf.Sin(x * 0.032f + large * 3.4f) * 0.5f + 0.5f;
                    var shade = Mathf.Clamp01(large * 0.50f + medium * 0.28f + fine * 0.14f + verticalStain * 0.08f);
                    var color = Color.Lerp(SampleDarkMossBodyShadowColor, SampleDarkMossBodyColor, shade);
                    if (medium > 0.66f)
                    {
                        color = Color.Lerp(color, highlight, (medium - 0.66f) * 0.82f);
                    }

                    if (fine < 0.22f)
                    {
                        color = Color.Lerp(color, SampleDarkMossBodyShadowColor, (0.22f - fine) * 0.70f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        private static Material CreateArtSampleEyeMaterial()
        {
            var material = new Material(FindArtSampleShader())
            {
                name = "ArtSample_Monstrum_YellowEye"
            };
            SetMaterialColor(material, SampleEyeGlowColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", SampleEyeGlowColor * 1.10f);
                material.EnableKeyword("_EMISSION");
            }

            SetMaterialFloatIfPresent(material, "_Metallic", 0f);
            SetMaterialFloatIfPresent(material, "_Smoothness", 0.22f);
            SetMaterialFloatIfPresent(material, "_Cull", 0f);
            return material;
        }

        private static Shader FindArtSampleShader()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("HDRP/Lit") ??
                         Shader.Find("Standard") ??
                         Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("Could not find a shader for the Monstrum art sample material.");
            }

            return shader;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetMaterialTextureIfPresent(Material material, string propertyName, Texture texture)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void ApplyArtSampleBodyMaterial(Transform previewRoot, Material bodyMaterial)
        {
            var renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Monstrum art sample preview contains no renderers.");
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = bodyMaterial;
                    continue;
                }

                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i] = bodyMaterial;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static VisualRecolorEyeSceneResult ApplyVisualRecolorEyeToPlacementRoot(Transform placementRoot, Material bodyMaterial, Material eyeMaterial)
        {
            var targets = GetVisualRecolorEyeSceneTargets(placementRoot);
            var bodyRendererCount = 0;
            var eyeRendererCount = 0;
            var eyeLightCount = 0;

            foreach (var target in targets)
            {
                RemoveExistingApprovedEyes(target);
                bodyRendererCount += ApplyApprovedBodyMaterial(target, bodyMaterial);
                var eyeInfo = AddApprovedSceneEyes(target, eyeMaterial);
                eyeRendererCount += 2;
                eyeLightCount += 2;
                Debug.Log(
                    $"MonstrumVisualRecolorEyeApplied Target={target.name}, EyeCenter={FormatVector(eyeInfo.EyeCenter)}, " +
                    $"LeftEye={FormatVector(eyeInfo.LeftEyePosition)}, RightEye={FormatVector(eyeInfo.RightEyePosition)}.");
            }

            return new VisualRecolorEyeSceneResult(targets.Count, bodyRendererCount, eyeRendererCount, eyeLightCount);
        }

        private static VisualRecolorEyeSceneResult ValidateVisualRecolorEyeOnPlacementRoot(
            Transform placementRoot,
            Material bodyMaterial,
            Material eyeMaterial,
            int expectedTargetCount)
        {
            var targets = GetVisualRecolorEyeSceneTargets(placementRoot);
            if (targets.Count != expectedTargetCount)
            {
                throw new InvalidOperationException(
                    $"Unexpected Monstrum visual target count. Expected={expectedTargetCount}, Actual={targets.Count}.");
            }

            var bodyRendererCount = 0;
            var eyeRendererCount = 0;
            var eyeLightCount = 0;
            foreach (var target in targets)
            {
                var eyeRoot = FindApprovedEyeRoot(target);
                if (eyeRoot == null)
                {
                    throw new InvalidOperationException($"{target.name} is missing {ApprovedEyeRootName}.");
                }

                if (!EyeRootUsesFaceFollowParent(target, eyeRoot))
                {
                    throw new InvalidOperationException($"{target.name}/{ApprovedEyeRootName} must use a face follow parent instead of staying fixed under the review root.");
                }

                var bodyRenderers = GetBodyRenderers(target, eyeRoot);
                if (bodyRenderers.Count == 0)
                {
                    throw new InvalidOperationException($"{target.name} has no body renderers for approved material validation.");
                }

                foreach (var renderer in bodyRenderers)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != bodyMaterial)
                        {
                            throw new InvalidOperationException($"{target.name}/{renderer.name} does not use the approved Monstrum body material.");
                        }
                    }
                }

                var leftEye = eyeRoot.Find("Monstrum_ApprovedScene_LeftEyeGlow");
                var rightEye = eyeRoot.Find("Monstrum_ApprovedScene_RightEyeGlow");
                if (leftEye == null || rightEye == null)
                {
                    throw new InvalidOperationException($"{target.name} is missing approved left/right eye meshes.");
                }

                var frontDirection = CalculateMonstrumVisualFrontDirection(target);
                var targetBounds = CalculateRendererBounds(target, new Bounds(target.position, Vector3.one));
                foreach (var eye in new[] { leftEye, rightEye })
                {
                    var renderer = eye.GetComponent<MeshRenderer>();
                    var meshFilter = eye.GetComponent<MeshFilter>();
                    if (renderer == null || meshFilter == null || meshFilter.sharedMesh == null)
                    {
                        throw new InvalidOperationException($"{target.name}/{eye.name} is missing its mesh renderer or mesh.");
                    }

                    if (renderer.sharedMaterial != eyeMaterial)
                    {
                        throw new InvalidOperationException($"{target.name}/{eye.name} does not use the approved Monstrum eye material.");
                    }

                    var mesh = meshFilter.sharedMesh;
                    var minimumVertexSurfaceOffset = float.PositiveInfinity;
                    var maximumVertexSurfaceOffset = float.NegativeInfinity;
                    foreach (var localVertex in mesh.vertices)
                    {
                        var worldVertex = eye.TransformPoint(localVertex);
                        if (!TryFindFrontSurfacePoint(target, eyeRoot, worldVertex, frontDirection, targetBounds, out var vertexSurfacePoint))
                        {
                            if (!TryFindClosestSurfacePoint(target, eyeRoot, worldVertex, out vertexSurfacePoint))
                            {
                                throw new InvalidOperationException($"{target.name}/{eye.name} could not resolve the face surface under an eye vertex.");
                            }
                        }

                        var vertexSurfaceOffset = Vector3.Dot(worldVertex - vertexSurfacePoint, frontDirection.normalized);
                        minimumVertexSurfaceOffset = Mathf.Min(minimumVertexSurfaceOffset, vertexSurfaceOffset);
                        maximumVertexSurfaceOffset = Mathf.Max(maximumVertexSurfaceOffset, vertexSurfaceOffset);
                    }

                    if (minimumVertexSurfaceOffset < -0.006f || maximumVertexSurfaceOffset > 0.155f)
                    {
                        throw new InvalidOperationException(
                            $"{target.name}/{eye.name} is not surface-attached. " +
                            $"VertexSurfaceOffsetRange={minimumVertexSurfaceOffset:0.###}..{maximumVertexSurfaceOffset:0.###}.");
                    }

                    var eyeCenter = renderer.bounds.center;
                    if (!TryFindFrontSurfacePoint(target, eyeRoot, eyeCenter, frontDirection, targetBounds, out var surfacePoint))
                    {
                        if (!TryFindClosestSurfacePoint(target, eyeRoot, eyeCenter, out surfacePoint))
                        {
                            throw new InvalidOperationException($"{target.name}/{eye.name} could not resolve the face surface under the eye mesh.");
                        }
                    }

                    var surfaceOffset = Vector3.Dot(eyeCenter - surfacePoint, frontDirection.normalized);
                    if (surfaceOffset < -0.006f || surfaceOffset > 0.155f)
                    {
                        throw new InvalidOperationException(
                            $"{target.name}/{eye.name} is not visibly attached to the face surface. SurfaceOffset={surfaceOffset:0.###}.");
                    }

                    eyeRendererCount++;
                }

                var lights = eyeRoot.GetComponentsInChildren<Light>(true);
                if (lights.Length < 2)
                {
                    throw new InvalidOperationException($"{target.name} must contain approved eye glow lights.");
                }

                bodyRendererCount += bodyRenderers.Count;
                eyeLightCount += lights.Length;
            }

            var result = new VisualRecolorEyeSceneResult(targets.Count, bodyRendererCount, eyeRendererCount, eyeLightCount);
            Debug.Log(
                $"MonstrumVisualRecolorEyeValidation Targets={result.TargetCount}, BodyRenderers={result.BodyRendererCount}, " +
                $"EyeRenderers={result.EyeRendererCount}, EyeLights={result.EyeLightCount}.");
            return result;
        }

        private static List<Transform> GetVisualRecolorEyeSceneTargets(Transform placementRoot)
        {
            var targets = new List<Transform> { RequireStaticReviewObject(placementRoot) };
            foreach (var spec in AnimationSlotSpecs)
            {
                var slot = placementRoot.Find(spec.ObjectName);
                if (slot == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing under {PlacementRootName}.");
                }

                targets.Add(slot);
            }

            return targets;
        }

        private static void RemoveExistingApprovedEyes(Transform target)
        {
            var existing = FindApprovedEyeRoot(target);
            while (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
                existing = FindApprovedEyeRoot(target);
            }

            foreach (var legacyName in new[] { "Monstrum_ArtSample_LeftEyeGlow", "Monstrum_ArtSample_RightEyeGlow", "Monstrum_ArtSample_LeftEye_Light", "Monstrum_ArtSample_RightEye_Light" })
            {
                var legacy = target.Find(legacyName);
                if (legacy != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacy.gameObject);
                }
            }
        }

        private static int ApplyApprovedBodyMaterial(Transform target, Material bodyMaterial)
        {
            var eyeRoot = FindApprovedEyeRoot(target);
            var renderers = GetBodyRenderers(target, eyeRoot);
            if (renderers.Count == 0)
            {
                throw new InvalidOperationException($"{target.name} contains no body renderers.");
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = bodyMaterial;
                    EditorUtility.SetDirty(renderer);
                    continue;
                }

                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i] = bodyMaterial;
                }

                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }

            return renderers.Count;
        }

        private static List<Renderer> GetBodyRenderers(Transform target, Transform eyeRoot)
        {
            var renderers = new List<Renderer>();
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (eyeRoot != null && renderer.transform.IsChildOf(eyeRoot))
                {
                    continue;
                }

                renderers.Add(renderer);
            }

            return renderers;
        }

        private static EyeSampleInfo AddApprovedSceneEyes(Transform target, Material eyeMaterial)
        {
            var eyeRootObject = new GameObject(ApprovedEyeRootName);
            eyeRootObject.transform.SetParent(target, false);
            eyeRootObject.transform.localPosition = Vector3.zero;
            eyeRootObject.transform.localRotation = Quaternion.identity;
            eyeRootObject.transform.localScale = Vector3.one;
            var eyeInfo = AddMonstrumVisualEyes(target, eyeRootObject.transform, eyeMaterial, "Monstrum_ApprovedScene", false);
            AttachApprovedEyesToFaceFollowParent(target, eyeRootObject.transform, eyeInfo.EyeCenter);
            EditorUtility.SetDirty(eyeRootObject);
            return eyeInfo;
        }

        private static EyeSampleInfo AddArtSampleEyes(Transform previewRoot, Material eyeMaterial)
        {
            return AddMonstrumVisualEyes(previewRoot, previewRoot, eyeMaterial, "Monstrum_ArtSample", true);
        }

        private static Transform FindApprovedEyeRoot(Transform target)
        {
            foreach (var child in target.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == ApprovedEyeRootName)
                {
                    return child;
                }
            }

            return null;
        }

        private static bool EyeRootUsesFaceFollowParent(Transform target, Transform eyeRoot)
        {
            return eyeRoot != null && eyeRoot.parent != null && eyeRoot.parent != target && eyeRoot.IsChildOf(target);
        }

        private static void AttachApprovedEyesToFaceFollowParent(Transform target, Transform eyeRoot, Vector3 eyeCenter)
        {
            var followParent = FindBestEyeFollowParent(target, eyeRoot, eyeCenter);
            if (followParent == null || followParent == target)
            {
                throw new InvalidOperationException($"{target.name} could not resolve a Monstrum face follow bone for {ApprovedEyeRootName}.");
            }

            eyeRoot.SetParent(followParent, true);
            eyeRoot.localScale = Vector3.one;
            EditorUtility.SetDirty(eyeRoot);
            EditorUtility.SetDirty(followParent);
        }

        private static Transform FindBestEyeFollowParent(Transform target, Transform eyeRoot, Vector3 eyeCenter)
        {
            Transform best = null;
            var bestScore = float.PositiveInfinity;
            foreach (var skinnedRenderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (eyeRoot != null && skinnedRenderer.transform.IsChildOf(eyeRoot))
                {
                    continue;
                }

                foreach (var bone in skinnedRenderer.bones)
                {
                    if (bone == null || bone == target || !bone.IsChildOf(target))
                    {
                        continue;
                    }

                    if (eyeRoot != null && bone.IsChildOf(eyeRoot))
                    {
                        continue;
                    }

                    var distance = Vector3.Distance(bone.position, eyeCenter);
                    var verticalPenalty = Mathf.Abs(bone.position.y - eyeCenter.y) * 0.35f;
                    var score = distance + verticalPenalty;
                    var boneName = bone.name.ToLowerInvariant();
                    if (boneName.Contains("head") || boneName.Contains("face") || boneName.Contains("eye"))
                    {
                        score *= 0.20f;
                    }
                    else if (boneName.Contains("neck") || boneName.Contains("jaw"))
                    {
                        score *= 0.45f;
                    }
                    else if (boneName.Contains("arm") || boneName.Contains("hand") || boneName.Contains("leg") || boneName.Contains("foot"))
                    {
                        score *= 3.00f;
                    }

                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    best = bone;
                }
            }

            return best;
        }

        private static EyeSampleInfo AddMonstrumVisualEyes(
            Transform targetRoot,
            Transform eyeParent,
            Material eyeMaterial,
            string namePrefix,
            bool hideMesh)
        {
            var bounds = CalculateRendererBounds(targetRoot, new Bounds(targetRoot.position, Vector3.one));
            var frontDirection = CalculateMonstrumVisualFrontDirection(targetRoot);
            var rightDirection = targetRoot.right;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = Vector3.right;
            }

            rightDirection.Normalize();
            var eyeHorizontalOffset = Mathf.Clamp(bounds.size.x * 0.018f, 0.032f, 0.048f);
            var eyeBaseCenter = bounds.center -
                                rightDirection * eyeHorizontalOffset +
                                Vector3.up * Mathf.Clamp(bounds.extents.y * 0.304f, 0.365f, 0.420f);
            var eyeSeparation = Mathf.Clamp(bounds.size.x * 0.020f, 0.038f, 0.052f);
            var eyeWidth = Mathf.Clamp(bounds.size.x * 0.0175f, 0.028f, 0.037f);
            var eyeHeight = Mathf.Clamp(bounds.size.y * 0.0042f, 0.0070f, 0.0095f);
            var leftBasePosition = eyeBaseCenter - rightDirection * eyeSeparation;
            var rightBasePosition = eyeBaseCenter + rightDirection * eyeSeparation;
            var surfaceLift = Mathf.Clamp(bounds.extents.z * 0.082f, 0.095f, 0.115f);
            var fallbackForwardOffset = Mathf.Clamp(bounds.extents.z * 0.205f, 0.165f, 0.280f);
            var leftPosition = ResolveEyeSurfacePosition(targetRoot, null, leftBasePosition, frontDirection, bounds, fallbackForwardOffset, surfaceLift);
            var rightPosition = ResolveEyeSurfacePosition(targetRoot, null, rightBasePosition, frontDirection, bounds, fallbackForwardOffset, surfaceLift);
            var eyeCenter = (leftPosition + rightPosition) * 0.5f;

            CreateArtSampleAlmondEye(targetRoot, bounds, fallbackForwardOffset, surfaceLift, namePrefix + "_LeftEyeGlow", eyeParent, leftPosition, frontDirection, rightDirection, Vector3.up, eyeWidth, eyeHeight, -1f, eyeMaterial, 0.66f, hideMesh);
            CreateArtSampleAlmondEye(targetRoot, bounds, fallbackForwardOffset, surfaceLift, namePrefix + "_RightEyeGlow", eyeParent, rightPosition, frontDirection, rightDirection, Vector3.up, eyeWidth, eyeHeight, 1f, eyeMaterial, 0.66f, hideMesh);
            CreateArtSampleEyeLight(namePrefix + "_LeftEye_Light", eyeParent, leftPosition, eyeHeight);
            CreateArtSampleEyeLight(namePrefix + "_RightEye_Light", eyeParent, rightPosition, eyeHeight);

            return new EyeSampleInfo(eyeCenter, leftPosition, rightPosition, eyeHeight, eyeSeparation);
        }

        private static Vector3 ResolveEyeSurfacePosition(
            Transform targetRoot,
            Transform ignoredRoot,
            Vector3 basePosition,
            Vector3 frontDirection,
            Bounds bounds,
            float fallbackForwardOffset,
            float surfaceLift)
        {
            if (TryFindFrontSurfacePoint(targetRoot, ignoredRoot, basePosition, frontDirection, bounds, out var surfacePoint))
            {
                return BuildEyePositionAtSurfaceDepth(basePosition, surfacePoint, frontDirection, surfaceLift);
            }

            var fallbackPosition = basePosition + frontDirection.normalized * fallbackForwardOffset;
            if (TryFindClosestSurfacePoint(targetRoot, ignoredRoot, fallbackPosition, out surfacePoint))
            {
                return BuildEyePositionAtSurfaceDepth(basePosition, surfacePoint, frontDirection, surfaceLift);
            }

            return fallbackPosition;
        }

        private static Vector3 BuildEyePositionAtSurfaceDepth(
            Vector3 basePosition,
            Vector3 surfacePoint,
            Vector3 frontDirection,
            float surfaceLift)
        {
            var front = frontDirection.normalized;
            var surfaceDepth = Vector3.Dot(surfacePoint - basePosition, front);
            return basePosition + front * (surfaceDepth + surfaceLift);
        }

        private static bool TryFindFrontSurfacePoint(
            Transform targetRoot,
            Transform ignoredRoot,
            Vector3 basePosition,
            Vector3 frontDirection,
            Bounds bounds,
            out Vector3 surfacePoint)
        {
            var rightDirection = targetRoot.right.sqrMagnitude > 0.001f ? targetRoot.right.normalized : Vector3.right;
            var verticalDirection = Vector3.up;
            var sampleStep = Mathf.Clamp(bounds.size.x * 0.012f, 0.018f, 0.036f);
            var candidateOffsets = new[]
            {
                Vector3.zero,
                rightDirection * sampleStep,
                -rightDirection * sampleStep,
                verticalDirection * sampleStep,
                -verticalDirection * sampleStep,
                rightDirection * sampleStep + verticalDirection * sampleStep,
                rightDirection * sampleStep - verticalDirection * sampleStep,
                -rightDirection * sampleStep + verticalDirection * sampleStep,
                -rightDirection * sampleStep - verticalDirection * sampleStep,
                rightDirection * sampleStep * 1.85f,
                -rightDirection * sampleStep * 1.85f,
                verticalDirection * sampleStep * 1.85f,
                -verticalDirection * sampleStep * 1.85f
            };

            foreach (var offset in candidateOffsets)
            {
                if (TryFindFrontSurfacePointSingleRay(targetRoot, ignoredRoot, basePosition + offset, frontDirection, bounds, out surfacePoint))
                {
                    return true;
                }
            }

            surfacePoint = default;
            return false;
        }

        private static bool TryFindFrontSurfacePointSingleRay(
            Transform targetRoot,
            Transform ignoredRoot,
            Vector3 basePosition,
            Vector3 frontDirection,
            Bounds bounds,
            out Vector3 surfacePoint)
        {
            surfacePoint = default;
            var rayDirection = -frontDirection.normalized;
            if (rayDirection.sqrMagnitude < 0.001f)
            {
                return false;
            }

            var rayStartDistance = Mathf.Clamp(bounds.extents.magnitude * 1.35f, 1.25f, 5.00f);
            var maxDistance = rayStartDistance + Mathf.Clamp(bounds.extents.magnitude * 2.20f, 2.00f, 8.00f);
            var origin = basePosition + frontDirection.normalized * rayStartDistance;
            var bestDistance = float.PositiveInfinity;

            foreach (var renderer in targetRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (ignoredRoot != null && renderer.transform.IsChildOf(ignoredRoot))
                {
                    continue;
                }

                var mesh = ResolveReadableRendererMesh(renderer);
                if (mesh == null)
                {
                    continue;
                }

                var localToWorld = renderer.transform.localToWorldMatrix;
                var vertices = mesh.vertices;
                for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    var indices = mesh.GetTriangles(submesh);
                    for (var i = 0; i + 2 < indices.Length; i += 3)
                    {
                        var a = localToWorld.MultiplyPoint3x4(vertices[indices[i]]);
                        var b = localToWorld.MultiplyPoint3x4(vertices[indices[i + 1]]);
                        var c = localToWorld.MultiplyPoint3x4(vertices[indices[i + 2]]);
                        if (!RayIntersectsTriangle(origin, rayDirection, a, b, c, out var distance))
                        {
                            continue;
                        }

                        if (distance <= 0.0001f || distance >= bestDistance || distance > maxDistance)
                        {
                            continue;
                        }

                        bestDistance = distance;
                        surfacePoint = origin + rayDirection * distance;
                    }
                }

                if (renderer is SkinnedMeshRenderer)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            return !float.IsPositiveInfinity(bestDistance);
        }

        private static bool TryFindClosestSurfacePoint(
            Transform targetRoot,
            Transform ignoredRoot,
            Vector3 referencePoint,
            out Vector3 closestPoint)
        {
            closestPoint = default;
            var bestDistanceSqr = float.PositiveInfinity;
            foreach (var renderer in targetRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (ignoredRoot != null && renderer.transform.IsChildOf(ignoredRoot))
                {
                    continue;
                }

                var mesh = ResolveReadableRendererMesh(renderer);
                if (mesh == null)
                {
                    continue;
                }

                var localToWorld = renderer.transform.localToWorldMatrix;
                var vertices = mesh.vertices;
                for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    var indices = mesh.GetTriangles(submesh);
                    for (var i = 0; i + 2 < indices.Length; i += 3)
                    {
                        var a = localToWorld.MultiplyPoint3x4(vertices[indices[i]]);
                        var b = localToWorld.MultiplyPoint3x4(vertices[indices[i + 1]]);
                        var c = localToWorld.MultiplyPoint3x4(vertices[indices[i + 2]]);
                        var candidate = ClosestPointOnTriangle(referencePoint, a, b, c);
                        var distanceSqr = (candidate - referencePoint).sqrMagnitude;
                        if (distanceSqr >= bestDistanceSqr)
                        {
                            continue;
                        }

                        bestDistanceSqr = distanceSqr;
                        closestPoint = candidate;
                    }
                }

                if (renderer is SkinnedMeshRenderer)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            return !float.IsPositiveInfinity(bestDistanceSqr);
        }

        private static Mesh ResolveReadableRendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                var bakedMesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(bakedMesh);
                return bakedMesh;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = point - a;
            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
            {
                return a;
            }

            var bp = point - b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
            {
                return b;
            }

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                var v = d1 / (d1 - d3);
                return a + ab * v;
            }

            var cp = point - c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
            {
                return c;
            }

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                var w = d2 / (d2 - d6);
                return a + ac * w;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                var w = (d4 - d3) / (d4 - d3 + d5 - d6);
                return b + (c - b) * w;
            }

            var denominator = 1f / (va + vb + vc);
            var vInside = vb * denominator;
            var wInside = vc * denominator;
            return a + ab * vInside + ac * wInside;
        }

        private static bool RayIntersectsTriangle(
            Vector3 origin,
            Vector3 direction,
            Vector3 vertex0,
            Vector3 vertex1,
            Vector3 vertex2,
            out float distance)
        {
            const float epsilon = 0.000001f;
            distance = 0f;
            var edge1 = vertex1 - vertex0;
            var edge2 = vertex2 - vertex0;
            var p = Vector3.Cross(direction, edge2);
            var determinant = Vector3.Dot(edge1, p);
            if (Mathf.Abs(determinant) < epsilon)
            {
                return false;
            }

            var inverseDeterminant = 1f / determinant;
            var t = origin - vertex0;
            var u = Vector3.Dot(t, p) * inverseDeterminant;
            if (u < 0f || u > 1f)
            {
                return false;
            }

            var q = Vector3.Cross(t, edge1);
            var v = Vector3.Dot(direction, q) * inverseDeterminant;
            if (v < 0f || u + v > 1f)
            {
                return false;
            }

            distance = Vector3.Dot(edge2, q) * inverseDeterminant;
            return distance > epsilon;
        }

        private static void CreateArtSampleAlmondEye(
            Transform targetRoot,
            Bounds bounds,
            float fallbackForwardOffset,
            float surfaceLift,
            string name,
            Transform parent,
            Vector3 center,
            Vector3 frontDirection,
            Vector3 rightDirection,
            Vector3 upDirection,
            float width,
            float height,
            float slantSign,
            Material material,
            float taper,
            bool hideMesh = true)
        {
            var longAxis = (rightDirection + upDirection * 0.10f * slantSign).normalized;
            var verticalAxis = (upDirection - rightDirection * 0.05f * slantSign).normalized;
            var ignoredRoot = parent == targetRoot ? null : parent;
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var segmentCount = 18;
            var vertices = new Vector3[segmentCount + 1];
            vertices[0] = parent.InverseTransformPoint(
                ResolveEyeSurfacePosition(targetRoot, ignoredRoot, center, frontDirection, bounds, fallbackForwardOffset, surfaceLift));
            for (var i = 0; i < segmentCount; i++)
            {
                var angle = Mathf.PI * 2f * i / segmentCount;
                var x = Mathf.Cos(angle);
                var y = Mathf.Sin(angle);
                var sharpenedY = y * Mathf.Lerp(1f, Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(x)), 0.48f), Mathf.Clamp01(taper));
                var point = center + longAxis * (x * halfWidth) + verticalAxis * (sharpenedY * halfHeight);
                var attachedPoint = ResolveEyeSurfacePosition(targetRoot, ignoredRoot, point, frontDirection, bounds, fallbackForwardOffset, surfaceLift);
                vertices[i + 1] = parent.InverseTransformPoint(attachedPoint);
            }

            var triangles = new int[segmentCount * 3];
            for (var i = 0; i < segmentCount; i++)
            {
                var next = i == segmentCount - 1 ? 0 : i + 1;
                var baseIndex = i * 3;
                triangles[baseIndex] = 0;
                triangles[baseIndex + 1] = i + 1;
                triangles[baseIndex + 2] = next + 1;
            }

            CreateArtSampleFlatEyeMesh(name, parent, vertices, triangles, material, hideMesh);
        }

        private static void CreateArtSampleEyeSlit(
            string name,
            Transform parent,
            Vector3 center,
            Vector3 frontDirection,
            Vector3 rightDirection,
            Vector3 upDirection,
            float width,
            float height,
            Material material)
        {
            var frontOffset = frontDirection.normalized * Mathf.Max(height * 0.34f, 0.004f);
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var vertices = new[]
            {
                parent.InverseTransformPoint(center - rightDirection * halfWidth + frontOffset),
                parent.InverseTransformPoint(center + upDirection * halfHeight + frontOffset),
                parent.InverseTransformPoint(center + rightDirection * halfWidth + frontOffset),
                parent.InverseTransformPoint(center - upDirection * halfHeight + frontOffset)
            };
            var triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };

            CreateArtSampleFlatEyeMesh(name, parent, vertices, triangles, material);
        }

        private static void CreateArtSampleFlatEyeMesh(string name, Transform parent, Vector3[] vertices, int[] triangles, Material material, bool hideMesh = true)
        {
            var eye = new GameObject(name);
            eye.transform.SetParent(parent, false);
            var mesh = new Mesh
            {
                name = name + "_Mesh",
                vertices = vertices,
                triangles = triangles
            };
            if (hideMesh)
            {
                mesh.hideFlags = HideFlags.HideAndDontSave;
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = eye.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var renderer = eye.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        private static void CreateArtSampleEyeLight(string name, Transform parent, Vector3 position, float eyeRadius)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.position = position;
            lightObject.transform.SetParent(parent, true);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = SampleEyeGlowColor;
            light.intensity = 0.008f;
            light.range = Mathf.Max(eyeRadius * 2.4f, 0.060f);
        }

        private static void CaptureArtSamplePreview(
            Transform previewRoot,
            string outputPath,
            float yawOffsetDegrees,
            float orthographicMultiplier,
            Vector3? focusOverride)
        {
            var bounds = CalculateRendererBounds(previewRoot, new Bounds(previewRoot.position, Vector3.one));
            var focus = focusOverride ?? (bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.08f, 0.24f));
            var frontDirection = Quaternion.Euler(0f, yawOffsetDegrees, 0f) * CalculateMonstrumVisualFrontDirection(previewRoot);
            if (frontDirection.sqrMagnitude < 0.001f)
            {
                frontDirection = Vector3.forward;
            }

            frontDirection.Normalize();
            var distance = Mathf.Clamp(bounds.extents.magnitude * 3.45f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var position = focus + frontDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.06f, 0.05f, 0.18f);
            var cameraObject = new GameObject("MonstrumArtSample_CaptureCamera");
            var keyLightObject = new GameObject("MonstrumArtSample_KeyLight");
            var fillLightObject = new GameObject("MonstrumArtSample_FillLight");
            Texture2D texture = null;

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((focus - position).normalized, Vector3.up));
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.y * orthographicMultiplier, 0.45f);
                camera.nearClipPlane = Mathf.Max(0.01f, distance - bounds.extents.magnitude - 0.85f);
                camera.farClipPlane = distance + bounds.extents.magnitude + 1.25f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.070f, 0.075f, 0.065f, 1f);

                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(0.92f, 0.98f, 0.80f, 1f);
                keyLight.intensity = 1.25f;
                keyLight.transform.rotation = Quaternion.Euler(42f, previewRoot.eulerAngles.y - 24f, 0f);

                var fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.color = new Color(0.35f, 0.48f, 0.32f, 1f);
                fillLight.intensity = 0.50f;
                fillLight.transform.rotation = Quaternion.Euler(18f, previewRoot.eulerAngles.y + 145f, 0f);

                texture = CaptureCameraTexture(camera, 1400, 900);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
            }
        }

        private static void CreateReferenceComparisonImage(string outputDirectory, string sampleRenderPath)
        {
            var referencePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", VisualRecolorEyeReferenceImagePath));
            if (!File.Exists(referencePath))
            {
                throw new FileNotFoundException("Monstrum reference image is missing.", referencePath);
            }

            var referenceTexture = LoadArtSampleTexture(referencePath);
            var sampleTexture = LoadArtSampleTexture(sampleRenderPath);
            Texture2D comparisonTexture = null;

            try
            {
                const int panelWidth = 1400;
                const int panelHeight = 900;
                const int dividerWidth = 14;
                comparisonTexture = new Texture2D(panelWidth * 2 + dividerWidth, panelHeight, TextureFormat.RGBA32, false)
                {
                    name = "Monstrum_Reference_Vs_Sample_SideBySide",
                    filterMode = FilterMode.Bilinear
                };

                FillTexture(comparisonTexture, new Color(0.018f, 0.022f, 0.018f, 1f));
                CopyTextureScaled(referenceTexture, comparisonTexture, 0, 0, panelWidth, panelHeight);
                FillRect(comparisonTexture, panelWidth, 0, dividerWidth, panelHeight, new Color(0.52f, 0.58f, 0.42f, 1f));
                CopyTextureScaled(sampleTexture, comparisonTexture, panelWidth + dividerWidth, 0, panelWidth, panelHeight);
                comparisonTexture.Apply();

                File.WriteAllBytes(
                    Path.Combine(outputDirectory, "renders", "reference_comparison.png"),
                    comparisonTexture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(referenceTexture);
                UnityEngine.Object.DestroyImmediate(sampleTexture);
                if (comparisonTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(comparisonTexture);
                }
            }
        }

        private static Texture2D LoadArtSampleTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Could not load art sample texture: " + path);
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            return texture;
        }

        private static void FillTexture(Texture2D texture, Color color)
        {
            FillRect(texture, 0, 0, texture.width, texture.height, color);
        }

        private static void FillRect(Texture2D texture, int startX, int startY, int width, int height, Color color)
        {
            for (var y = startY; y < startY + height; y++)
            {
                for (var x = startX; x < startX + width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void CopyTextureScaled(Texture2D source, Texture2D target, int startX, int startY, int width, int height)
        {
            for (var y = 0; y < height; y++)
            {
                var v = height <= 1 ? 0f : y / (float)(height - 1);
                for (var x = 0; x < width; x++)
                {
                    var u = width <= 1 ? 0f : x / (float)(width - 1);
                    target.SetPixel(startX + x, startY + y, source.GetPixelBilinear(u, v));
                }
            }
        }

        private static void ExportVisualRecolorEyeObj(Transform previewRoot, string outputDirectory)
        {
            var exportDirectory = Path.Combine(outputDirectory, "exports");
            Directory.CreateDirectory(exportDirectory);

            var objPath = Path.Combine(exportDirectory, "monstrum_visual_recolor_eye_sample.obj");
            var mtlPath = Path.Combine(exportDirectory, "monstrum_visual_recolor_eye_sample.mtl");
            var obj = new StringBuilder();
            obj.AppendLine("# Monstrum visual recolor eye art sample export");
            obj.AppendLine("# Generated from the temporary preview object based on the current Unity Monstrum.");
            obj.AppendLine("mtllib monstrum_visual_recolor_eye_sample.mtl");
            obj.AppendLine();

            var vertexOffset = 1;
            var exportedMeshCount = 0;
            foreach (var renderer in previewRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                Mesh mesh = null;
                var destroyMesh = false;
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    mesh = meshFilter.sharedMesh;
                }
                else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    mesh = new Mesh();
                    skinnedMeshRenderer.BakeMesh(mesh);
                    destroyMesh = true;
                }

                if (mesh == null || mesh.vertexCount == 0)
                {
                    continue;
                }

                AppendMeshToObj(previewRoot, renderer, mesh, obj, ref vertexOffset);
                exportedMeshCount++;

                if (destroyMesh)
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            if (exportedMeshCount == 0)
            {
                throw new InvalidOperationException("Could not export Monstrum art sample OBJ because no meshes were found.");
            }

            File.WriteAllText(objPath, obj.ToString(), Encoding.UTF8);
            File.WriteAllText(mtlPath, CreateVisualRecolorEyeMtl(), Encoding.UTF8);
        }

        private static void AppendMeshToObj(
            Transform previewRoot,
            Renderer renderer,
            Mesh mesh,
            StringBuilder obj,
            ref int vertexOffset)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            var rootWorldToLocal = previewRoot.worldToLocalMatrix;
            var localToWorld = renderer.transform.localToWorldMatrix;

            obj.AppendLine("o " + SanitizeObjName(renderer.gameObject.name));
            for (var i = 0; i < vertices.Length; i++)
            {
                var position = rootWorldToLocal.MultiplyPoint3x4(localToWorld.MultiplyPoint3x4(vertices[i]));
                obj.AppendLine("v " + FormatObjFloat(position.x) + " " + FormatObjFloat(position.y) + " " + FormatObjFloat(position.z));
            }

            for (var i = 0; i < vertices.Length; i++)
            {
                var uv = uvs != null && i < uvs.Length ? uvs[i] : Vector2.zero;
                obj.AppendLine("vt " + FormatObjFloat(uv.x) + " " + FormatObjFloat(uv.y));
            }

            for (var i = 0; i < vertices.Length; i++)
            {
                var normal = normals != null && i < normals.Length ? normals[i] : Vector3.up;
                normal = rootWorldToLocal.MultiplyVector(localToWorld.MultiplyVector(normal)).normalized;
                obj.AppendLine("vn " + FormatObjFloat(normal.x) + " " + FormatObjFloat(normal.y) + " " + FormatObjFloat(normal.z));
            }

            var submeshCount = Mathf.Max(mesh.subMeshCount, 1);
            for (var submesh = 0; submesh < submeshCount; submesh++)
            {
                obj.AppendLine("usemtl " + ResolveObjMaterialName(renderer, submesh));
                var triangles = mesh.GetTriangles(submesh);
                for (var i = 0; i + 2 < triangles.Length; i += 3)
                {
                    var a = triangles[i] + vertexOffset;
                    var b = triangles[i + 1] + vertexOffset;
                    var c = triangles[i + 2] + vertexOffset;
                    obj.AppendLine(
                        "f " +
                        FormatObjFaceVertex(a) + " " +
                        FormatObjFaceVertex(c) + " " +
                        FormatObjFaceVertex(b));
                }
            }

            obj.AppendLine();
            vertexOffset += vertices.Length;
        }

        private static string ResolveObjMaterialName(Renderer renderer, int submesh)
        {
            var materials = renderer.sharedMaterials;
            var materialName = materials != null && submesh < materials.Length && materials[submesh] != null
                ? materials[submesh].name
                : string.Empty;
            if (materialName.IndexOf("YellowEye", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Monstrum_Eye_Glow";
            }

            return "Monstrum_Dark_Moss_Body";
        }

        private static string CreateVisualRecolorEyeMtl()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Monstrum visual recolor eye art sample materials");
            builder.AppendLine("newmtl Monstrum_Dark_Moss_Body");
            builder.AppendLine("Ka 0.02 0.04 0.02");
            builder.AppendLine("Kd " + FormatObjColor(SampleDarkMossBodyColor));
            builder.AppendLine("Ks 0.04 0.04 0.035");
            builder.AppendLine("Ns 18");
            builder.AppendLine("d 1");
            builder.AppendLine("illum 2");
            builder.AppendLine("map_Kd ../textures/monstrum_dark_moss_body_albedo.png");
            builder.AppendLine();
            builder.AppendLine("newmtl Monstrum_Eye_Glow");
            builder.AppendLine("Ka 0.25 0.20 0.03");
            builder.AppendLine("Kd " + FormatObjColor(SampleEyeGlowColor));
            builder.AppendLine("Ks 0.16 0.14 0.04");
            builder.AppendLine("Ns 40");
            builder.AppendLine("d 1");
            builder.AppendLine("illum 2");
            return builder.ToString();
        }

        private static string FormatObjFaceVertex(int index)
        {
            return index.ToString(CultureInfo.InvariantCulture) + "/" +
                   index.ToString(CultureInfo.InvariantCulture) + "/" +
                   index.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatObjColor(Color color)
        {
            return FormatObjFloat(color.r) + " " + FormatObjFloat(color.g) + " " + FormatObjFloat(color.b);
        }

        private static string FormatObjFloat(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string SanitizeObjName(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                builder.Append(char.IsLetterOrDigit(character) || character == '_' || character == '-' ? character : '_');
            }

            return builder.Length == 0 ? "MonstrumMesh" : builder.ToString();
        }

        private static void WriteVisualRecolorEyeArtSampleReadme(string outputDirectory, EyeSampleInfo eyeInfo)
        {
            var readmePath = Path.Combine(outputDirectory, "README.md");
            var manifestPath = Path.Combine(outputDirectory, "ASSET_MANIFEST.json");
            var builder = new StringBuilder();
            builder.AppendLine("# 몬스트룸 어두운 녹색/눈 오브젝트 샘플");
            builder.AppendLine();
            builder.AppendLine("- 기준 모델: 현재 Unity `CargoRunMvp`에 배치된 `Monstrum_00_Static_Review` 정리 메시 기반");
            builder.AppendLine("- 참고 이미지: `image/monstrum(몬스트룸).png`");
            builder.AppendLine("- 반영 의도: 씨앗/솜털/떠 있는 알갱이는 제외하고, 어두운 녹색 몸통과 노란 눈 인상만 샘플링");
            builder.AppendLine("- 실제 씬 적용 여부: 아니오. 이 폴더의 PNG/설명/내보내기 파일만 생성");
            builder.AppendLine();
            builder.AppendLine("## 샘플 파일");
            builder.AppendLine();
            builder.AppendLine("- `index.html`: 기준 이미지와 생성 렌더를 직접 비교하는 대표 미리보기");
            builder.AppendLine("- `renders/front.png`: 정면 검토용");
            builder.AppendLine("- `renders/side.png`: 측면 검토용");
            builder.AppendLine("- `renders/back.png`: 후면 검토용");
            builder.AppendLine("- `renders/head_close.png`: 눈 위치/색 확인용 근접 샷");
            builder.AppendLine("- `renders/reference_comparison.png`: 기준 이미지와 샘플 정면 렌더 좌우 비교");
            builder.AppendLine("- `textures/monstrum_dark_moss_body_albedo.png`: 어두운 녹색 몸통용 절차적 표면 텍스처");
            builder.AppendLine("- `exports/monstrum_visual_recolor_eye_sample.obj`: 검토용 OBJ 내보내기 파일");
            builder.AppendLine("- `exports/monstrum_visual_recolor_eye_sample.mtl`: OBJ용 머티리얼 정의");
            builder.AppendLine("- `APPROVAL_STATUS.json`: 승인 상태");
            builder.AppendLine("- `VISUAL_ANALYSIS.md`: 기준 이미지/기획서/사용자 확인 사항 분석");
            builder.AppendLine("- `UNITY_APPLICATION_PLAN.md`: 승인 후 Unity 적용 계획");
            builder.AppendLine("- `RULE_COMPLIANCE_CHECKLIST.md`: `AGENTS.md` 샘플 규칙 점검표");
            builder.AppendLine();
            builder.AppendLine("## 적용 예정 방식");
            builder.AppendLine();
            builder.AppendLine("- 몸통은 어두운 녹색 계열 머티리얼과 절차적 얼룩 텍스처를 몬스트룸 렌더러에 적용");
            builder.AppendLine("- 눈은 본체 메시를 직접 변형하지 않고, 머리 앞쪽의 어두운 얼굴면 위에 작고 날카로운 황색 눈틈을 자식 오브젝트로 추가");
            builder.AppendLine("- 원본 FBX는 직접 수정하지 않음");
            builder.AppendLine("- 실제 Unity 씬/프리팹 적용은 이 샘플 승인 후 별도 승인으로만 진행");
            builder.AppendLine();
            builder.AppendLine("## 샘플 수치");
            builder.AppendLine();
            builder.AppendLine("- BodyColor: " + FormatColor(SampleDarkMossBodyColor));
            builder.AppendLine("- BodyShadowReference: " + FormatColor(SampleDarkMossBodyShadowColor));
            builder.AppendLine("- EyeGlowColor: " + FormatColor(SampleEyeGlowColor));
            builder.AppendLine("- EyeCenter: " + FormatVector(eyeInfo.EyeCenter));
            builder.AppendLine("- LeftEye: " + FormatVector(eyeInfo.LeftEyePosition));
            builder.AppendLine("- RightEye: " + FormatVector(eyeInfo.RightEyePosition));
            builder.AppendLine("- EyeHeightReference: " + FormatFloat(eyeInfo.EyeRadius));
            builder.AppendLine("- EyeSeparation: " + FormatFloat(eyeInfo.EyeSeparation));
            File.WriteAllText(readmePath, builder.ToString(), Encoding.UTF8);

            var manifest = new StringBuilder();
            manifest.AppendLine("{");
            manifest.AppendLine("  \"enemyId\": \"monstrum\",");
            manifest.AppendLine("  \"createdAt\": \"2026-07-09\",");
            manifest.AppendLine("  \"status\": \"pending_user_approval\",");
            manifest.AppendLine("  \"runtimeSceneModified\": false,");
            manifest.AppendLine("  \"sourceReferences\": [");
            manifest.AppendLine("    \"docs/GAME_DESIGN_SOURCE.txt\",");
            manifest.AppendLine("    \"" + VisualRecolorEyeReferenceImagePath.Replace("\\", "/") + "\",");
            manifest.AppendLine("    \"" + CargoRunScenePath + "::" + PlacementObjectName + "\"");
            manifest.AppendLine("  ],");
            manifest.AppendLine("  \"basedOnCleanedMesh\": \"" + UnityMeshFolder + "/Monstrum_LooseGrainRemoved_char1.asset\",");
            manifest.AppendLine("  \"revisionNotes\": [");
            manifest.AppendLine("    \"Longa Arma style review HTML with direct reference/render comparisons.\",");
            manifest.AppendLine("    \"Uses the current Unity Monstrum mesh instead of rebuilding the model from scratch.\",");
            manifest.AppendLine("    \"Eyes are placed on the visual front side, not the back side.\",");
            manifest.AppendLine("    \"Reference seed/fluff objects are intentionally excluded per user instruction.\"");
            manifest.AppendLine("  ],");
            manifest.AppendLine("  \"modelScaleMeters\": { \"height\": 2.5, \"width\": 2.0, \"depth\": 3.0 },");
            manifest.AppendLine("  \"files\": [");
            manifest.AppendLine("    \"README.md\",");
            manifest.AppendLine("    \"TEXTURE_ANALYSIS.md\",");
            manifest.AppendLine("    \"APPROVAL_STATUS.json\",");
            manifest.AppendLine("    \"ASSET_MANIFEST.json\",");
            manifest.AppendLine("    \"index.html\",");
            manifest.AppendLine("    \"VISUAL_ANALYSIS.md\",");
            manifest.AppendLine("    \"UNITY_APPLICATION_PLAN.md\",");
            manifest.AppendLine("    \"RULE_COMPLIANCE_CHECKLIST.md\",");
            manifest.AppendLine("    \"MATERIAL_SETTINGS.txt\",");
            manifest.AppendLine("    \"exports/monstrum_visual_recolor_eye_sample.obj\",");
            manifest.AppendLine("    \"exports/monstrum_visual_recolor_eye_sample.mtl\",");
            manifest.AppendLine("    \"textures/monstrum_dark_moss_body_albedo.png\",");
            manifest.AppendLine("    \"renders/front.png\",");
            manifest.AppendLine("    \"renders/side.png\",");
            manifest.AppendLine("    \"renders/back.png\",");
            manifest.AppendLine("    \"renders/head_close.png\",");
            manifest.AppendLine("    \"renders/reference_comparison.png\"");
            manifest.AppendLine("  ]");
            manifest.AppendLine("}");
            File.WriteAllText(manifestPath, manifest.ToString(), Encoding.UTF8);

            WriteVisualRecolorEyeSupplementalDocs(outputDirectory, eyeInfo);
        }

        private static void WriteVisualRecolorEyeSupplementalDocs(string outputDirectory, EyeSampleInfo eyeInfo)
        {
            WriteVisualRecolorEyeApprovalStatus(outputDirectory);
            WriteVisualRecolorEyeAnalysis(outputDirectory);
            WriteVisualRecolorEyeTextureAnalysis(outputDirectory);
            WriteVisualRecolorEyeApplicationPlan(outputDirectory, eyeInfo);
            WriteVisualRecolorEyeMaterialSettings(outputDirectory, eyeInfo);
            WriteVisualRecolorEyeRuleChecklist(outputDirectory);
            WriteVisualRecolorEyeHtmlPreview(outputDirectory);
        }

        private static void WriteVisualRecolorEyeApprovalStatus(string outputDirectory)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"enemyId\": \"monstrum\",");
            builder.AppendLine("  \"status\": \"pending_user_approval\",");
            builder.AppendLine("  \"approved\": false,");
            builder.AppendLine("  \"createdAt\": \"2026-07-09\",");
            builder.AppendLine("  \"note\": \"현재 Unity CargoRunMvp의 Monstrum_00_Static_Review 정리 메시 기반 샘플입니다. 사용자 승인 전에는 Unity 런타임 씬, 프리팹, 런타임 에셋, AI, 피격 판정, UI 흐름에 연결하지 않습니다. 기준 이미지의 씨앗 오브젝트는 사용자 지시에 따라 제외했습니다.\"");
            builder.AppendLine("}");
            File.WriteAllText(Path.Combine(outputDirectory, "APPROVAL_STATUS.json"), builder.ToString(), Encoding.UTF8);
        }

        private static void WriteVisualRecolorEyeAnalysis(string outputDirectory)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# 몬스트룸 시각 분석");
            builder.AppendLine();
            builder.AppendLine("## 기준");
            builder.AppendLine();
            builder.AppendLine("- 원본 기획서: 몬스트룸은 약 250cm 높이의 거대한 이족보행 괴수이며, 비정상적으로 큰 발과 망치 같은 양손을 가짐");
            builder.AppendLine("- 기준 이미지: 어두운 녹색 몸통, 강한 눈 인상, 거대한 팔과 상체 실루엣을 참고");
            builder.AppendLine("- 사용자 확인: 현재 Unity에 있는 몬스트룸 모델을 기반으로 하고, 이미지의 씨앗/솜털/알갱이는 참고하지 않음");
            builder.AppendLine();
            builder.AppendLine("## 반영 항목");
            builder.AppendLine();
            builder.AppendLine("- 실루엣/비율: 현재 Unity 몬스트룸 정리 메시를 유지함. 새 모델링이나 본체 리모델링은 하지 않음");
            builder.AppendLine("- 주요 부위: 큰 팔, 둔기형 손, 큰 발, 낮고 무거운 상체는 현재 모델 그대로 유지");
            builder.AppendLine("- 색 분포: 몸통 전체를 어두운 녹색 계열로 통일하되, 절차적 얼룩 텍스처로 어두운 그림자와 짙은 녹색 변화를 추가");
            builder.AppendLine("- 눈: 기준 이미지의 노란 눈 인상만 가져와 머리 앞쪽의 어두운 얼굴면 위에 작고 날카로운 황색 눈틈을 추가");
            builder.AppendLine("- 재질/질감: 비금속, 낮은 광택, 거친 이끼/피부 느낌을 목표로 함. 단순 단색이 되지 않도록 `textures/monstrum_dark_moss_body_albedo.png`를 사용");
            builder.AppendLine("- 제외 항목: 씨앗, 솜털, 떠 있는 알갱이, 새 VFX, 새 애니메이션, 새 AI 연결");
            builder.AppendLine();
            builder.AppendLine("## 비교 파일");
            builder.AppendLine();
            builder.AppendLine("- `renders/reference_comparison.png`의 왼쪽은 기준 이미지, 오른쪽은 현재 샘플 정면 렌더");
            builder.AppendLine("- 기준 이미지와 다른 부분 중 씨앗/솜털은 사용자 지시에 따라 의도적으로 제외");
            File.WriteAllText(Path.Combine(outputDirectory, "VISUAL_ANALYSIS.md"), builder.ToString(), Encoding.UTF8);
        }

        private static void WriteVisualRecolorEyeApplicationPlan(string outputDirectory, EyeSampleInfo eyeInfo)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Unity 적용 계획");
            builder.AppendLine();
            builder.AppendLine("이 문서는 샘플 승인 후 별도 승인 요청에 포함할 적용 계획 초안입니다. 현재 샘플 생성 단계에서는 실제 씬을 저장하거나 런타임 에셋에 연결하지 않았습니다.");
            builder.AppendLine();
            builder.AppendLine("## 대상");
            builder.AppendLine();
            builder.AppendLine("- 씬: `Assets/_Project/Scenes/CargoRunMvp.unity`");
            builder.AppendLine("- 루트: `Approved Monstrum Enemy Placement`");
            builder.AppendLine("- 기준 오브젝트: `Monstrum_00_Static_Review`");
            builder.AppendLine("- 필요 시 같은 시각 상태를 맞출 후보 슬롯: `Monstrum_02_Idle`부터 `Monstrum_09_Death`까지의 검토 슬롯");
            builder.AppendLine();
            builder.AppendLine("## 적용 방식");
            builder.AppendLine();
            builder.AppendLine("- 몸통: 몬스트룸 렌더러에 어두운 녹색 머티리얼과 `textures/monstrum_dark_moss_body_albedo.png` 계열 텍스처를 적용");
            builder.AppendLine("- 눈: 본체 메시를 직접 수정하지 않고, 머리 앞쪽 자식 오브젝트로 작은 황색 눈틈만 추가");
            builder.AppendLine("- 눈 충돌: 눈 오브젝트에는 Collider를 두지 않음");
            builder.AppendLine("- 원본 FBX: 직접 수정하지 않음");
            builder.AppendLine("- 씬 저장: 승인받은 대상 루트 외 오브젝트는 생성, 삭제, 비활성화, 이동, 이름 변경하지 않음");
            builder.AppendLine();
            builder.AppendLine("## 샘플 좌표");
            builder.AppendLine();
            builder.AppendLine("- EyeCenter: " + FormatVector(eyeInfo.EyeCenter));
            builder.AppendLine("- LeftEye: " + FormatVector(eyeInfo.LeftEyePosition));
            builder.AppendLine("- RightEye: " + FormatVector(eyeInfo.RightEyePosition));
            builder.AppendLine("- EyeHeightReference: " + FormatFloat(eyeInfo.EyeRadius));
            builder.AppendLine("- EyeSeparation: " + FormatFloat(eyeInfo.EyeSeparation));
            File.WriteAllText(Path.Combine(outputDirectory, "UNITY_APPLICATION_PLAN.md"), builder.ToString(), Encoding.UTF8);
        }

        private static void WriteVisualRecolorEyeTextureAnalysis(string outputDirectory)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# 몬스트룸 텍스처/머티리얼 분석");
            builder.AppendLine();
            builder.AppendLine("## 몸통");
            builder.AppendLine();
            builder.AppendLine("- 기준 이미지는 짙은 녹색 피부/식물성 표면, 어두운 그림자, 거친 질감이 함께 보입니다.");
            builder.AppendLine("- 사용자 지시에 따라 씨앗/솜털/떠 있는 알갱이는 제외하고, 현재 Unity 몬스트룸 메시 위에 어두운 녹색 계열 표면만 적용합니다.");
            builder.AppendLine("- `textures/monstrum_dark_moss_body_albedo.png`는 단순 단색을 피하기 위해 큰 얼룩, 중간 노이즈, 미세 얼룩을 섞은 절차적 알베도 텍스처입니다.");
            builder.AppendLine("- 머티리얼 의도는 비금속, 낮은 광택, 높은 거칠기입니다.");
            builder.AppendLine();
            builder.AppendLine("## 눈");
            builder.AppendLine();
            builder.AppendLine("- 기준 이미지의 노란 눈 인상만 반영합니다.");
            builder.AppendLine("- 눈은 본체 메시를 직접 수정하지 않고 전면 얼굴 쪽 자식 오브젝트로 추가합니다.");
            builder.AppendLine("- 별도 큰 흰자나 동그란 동공은 만들지 않고, 어두운 얼굴면 위에 작은 황색 눈틈만 배치합니다.");
            File.WriteAllText(Path.Combine(outputDirectory, "TEXTURE_ANALYSIS.md"), builder.ToString(), Encoding.UTF8);
        }

        private static void WriteVisualRecolorEyeMaterialSettings(string outputDirectory, EyeSampleInfo eyeInfo)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Monstrum Visual Recolor Eye Material Settings");
            builder.AppendLine("BodyMaterialName: ArtSample_Monstrum_DarkMossBody");
            builder.AppendLine("BodyColor: " + FormatColor(SampleDarkMossBodyColor));
            builder.AppendLine("BodyShadowReference: " + FormatColor(SampleDarkMossBodyShadowColor));
            builder.AppendLine("BodyTexture: textures/monstrum_dark_moss_body_albedo.png");
            builder.AppendLine("Metallic: 0");
            builder.AppendLine("Smoothness: 0.24");
            builder.AppendLine("RoughnessReference: 0.86");
            builder.AppendLine("EyeMaterialName: ArtSample_Monstrum_YellowEye");
            builder.AppendLine("EyeGlowColor: " + FormatColor(SampleEyeGlowColor));
            builder.AppendLine("EyeCenter: " + FormatVector(eyeInfo.EyeCenter));
            builder.AppendLine("EyeSeparation: " + FormatFloat(eyeInfo.EyeSeparation));
            File.WriteAllText(Path.Combine(outputDirectory, "MATERIAL_SETTINGS.txt"), builder.ToString(), Encoding.UTF8);
        }

        private static void WriteVisualRecolorEyeRuleChecklist(string outputDirectory)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# AGENTS.md 샘플 규칙 점검표");
            builder.AppendLine();
            builder.AppendLine("- `artSample/enemies/monstrum/` 아래 생성: 충족");
            builder.AppendLine("- 사용자 승인 전 Unity 런타임 씬/프리팹/에셋 연결 금지: 충족, 샘플 생성용 임시 오브젝트만 사용");
            builder.AppendLine("- `docs/GAME_DESIGN_SOURCE.txt`, 사용자 확인 사항, `image/` 기준 이미지 참고: 충족");
            builder.AppendLine("- 기준 이미지의 외형/색/재질/질감 분석 문서화: `VISUAL_ANALYSIS.md`에 기록");
            builder.AppendLine("- 텍스처와 머티리얼 포함: `textures/monstrum_dark_moss_body_albedo.png`, `MATERIAL_SETTINGS.txt`, `exports/*.mtl` 포함");
            builder.AppendLine("- 단순 단색 머티리얼 금지: 절차적 어두운 녹색 얼룩 텍스처를 적용");
            builder.AppendLine("- 정적 렌더 포함: 정면, 3/4, 근접 PNG 포함");
            builder.AppendLine("- 기준 이미지 대비 side-by-side 비교 포함: `renders/reference_comparison.png` 포함");
            builder.AppendLine("- 검토용 원본/내보내기 파일 포함: `exports/monstrum_visual_recolor_eye_sample.obj`, `exports/monstrum_visual_recolor_eye_sample.mtl` 포함");
            builder.AppendLine("- README, 승인 상태 JSON, 에셋 매니페스트 JSON, `index.html` 미리보기 포함: 충족");
            builder.AppendLine("- 애니메이션 샘플 필수 아님: 이번 작업은 비애니메이션 시각 샘플이며 애니메이션은 생성하지 않음");
            File.WriteAllText(Path.Combine(outputDirectory, "RULE_COMPLIANCE_CHECKLIST.md"), builder.ToString(), Encoding.UTF8);
        }

        private static void WriteVisualRecolorEyeHtmlPreview(string outputDirectory)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<!doctype html>");
            builder.AppendLine("<html lang=\"ko\">");
            builder.AppendLine("<head>");
            builder.AppendLine("  <meta charset=\"utf-8\">");
            builder.AppendLine("  <title>몬스트룸 모델링 샘플</title>");
            builder.AppendLine("  <style>");
            builder.AppendLine("    body { margin: 0; font-family: Arial, \"Malgun Gothic\", sans-serif; background: #18201a; color: #edf2e6; }");
            builder.AppendLine("    main { max-width: 1180px; margin: 0 auto; padding: 28px; }");
            builder.AppendLine("    h1, h2, h3 { margin: 0 0 14px; }");
            builder.AppendLine("    section { margin: 30px 0; }");
            builder.AppendLine("    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; }");
            builder.AppendLine("    .comparison { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 22px; background: #222b24; border: 1px solid #435041; padding: 14px; }");
            builder.AppendLine("    .comparison h3 { grid-column: 1 / -1; }");
            builder.AppendLine("    figure { margin: 0; background: #111711; border: 1px solid #364236; padding: 10px; }");
            builder.AppendLine("    img { width: 100%; height: auto; display: block; object-fit: contain; }");
            builder.AppendLine("    figcaption { margin-top: 8px; font-size: 13px; color: #cdd7c5; word-break: break-all; }");
            builder.AppendLine("    p, li { line-height: 1.55; }");
            builder.AppendLine("    code { color: #d7e6c8; }");
            builder.AppendLine("    @media (max-width: 760px) { .comparison { grid-template-columns: 1fr; } }");
            builder.AppendLine("  </style>");
            builder.AppendLine("</head>");
            builder.AppendLine("<body>");
            builder.AppendLine("<main>");
            builder.AppendLine("  <h1>몬스트룸 모델링 샘플</h1>");
            builder.AppendLine("  <p>현재 Unity에 배치된 몬스트룸 정리 메시를 기반으로 한 승인 전 샘플입니다. 기준 이미지에서 씨앗/솜털/떠 있는 알갱이는 제외하고, 어두운 녹색 몸통과 얼굴 전면의 노란 눈 인상만 반영했습니다.</p>");
            builder.AppendLine();
            builder.AppendLine("  <section>");
            builder.AppendLine("    <h2>기준 이미지와 생성 렌더 비교</h2>");
            builder.AppendLine("    <article class=\"comparison\">");
            builder.AppendLine("      <h3>정면</h3>");
            builder.AppendLine("      <figure><img src=\"../../../../image/monstrum(몬스트룸).png\" alt=\"몬스트룸 기준 이미지\"><figcaption>기준 이미지: ../../../../image/monstrum(몬스트룸).png</figcaption></figure>");
            builder.AppendLine("      <figure><img src=\"renders/front.png\" alt=\"정면 생성 렌더\"><figcaption>생성 렌더: renders/front.png</figcaption></figure>");
            builder.AppendLine("    </article>");
            builder.AppendLine("  </section>");
            builder.AppendLine();
            builder.AppendLine("  <section>");
            builder.AppendLine("    <h2>생성 렌더</h2>");
            builder.AppendLine("    <div class=\"grid\">");
            builder.AppendLine("      <figure><img src=\"renders/front.png\" alt=\"front\"><figcaption>front.png</figcaption></figure>");
            builder.AppendLine("      <figure><img src=\"renders/side.png\" alt=\"side\"><figcaption>side.png</figcaption></figure>");
            builder.AppendLine("      <figure><img src=\"renders/back.png\" alt=\"back\"><figcaption>back.png</figcaption></figure>");
            builder.AppendLine("      <figure><img src=\"renders/head_close.png\" alt=\"head close\"><figcaption>head_close.png</figcaption></figure>");
            builder.AppendLine("      <figure><img src=\"renders/reference_comparison.png\" alt=\"reference comparison\"><figcaption>reference_comparison.png</figcaption></figure>");
            builder.AppendLine("    </div>");
            builder.AppendLine("  </section>");
            builder.AppendLine();
            builder.AppendLine("  <section>");
            builder.AppendLine("    <h2>사용 텍스처</h2>");
            builder.AppendLine("    <div class=\"grid\">");
            builder.AppendLine("      <figure><img src=\"textures/monstrum_dark_moss_body_albedo.png\" alt=\"body albedo\"><figcaption>monstrum_dark_moss_body_albedo.png</figcaption></figure>");
            builder.AppendLine("    </div>");
            builder.AppendLine("  </section>");
            builder.AppendLine("</main>");
            builder.AppendLine("</body>");
            builder.AppendLine("</html>");
            File.WriteAllText(Path.Combine(outputDirectory, "index.html"), builder.ToString(), Encoding.UTF8);
        }

        private static Transform FindMonstrumCameraFocus(Transform placementRoot)
        {
            return placementRoot.Find(PlacementObjectName) ?? placementRoot;
        }

        private static Vector3 CalculateMonstrumVisualFrontDirection(Transform focus)
        {
            var yawRotation = Quaternion.Euler(0f, focus.eulerAngles.y, 0f);
            var frontDirection = yawRotation * Vector3.forward;
            frontDirection.y = 0f;
            return frontDirection.sqrMagnitude > 0.001f ? frontDirection.normalized : Vector3.forward;
        }

        private static Vector3 CalculatePlayerLookAt(Bounds bounds)
        {
            return bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.10f, 0.30f);
        }

        private static float CalculatePlayerReviewDistance(Bounds bounds)
        {
            var horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return Mathf.Clamp(horizontalExtent + 4.65f, ReviewPlayerMinimumFrontDistance, ReviewPlayerMaximumFrontDistance);
        }

        private static Quaternion CalculateYawRotationToward(Vector3 position, Vector3 target)
        {
            var facing = target - position;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : Quaternion.identity;
        }

        private static Transform FindPlayerStartTransform()
        {
            var player = GameObject.Find(PlayerRootName);
            if (player != null)
            {
                return player.transform;
            }

            var characterController = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            return characterController != null ? characterController.transform : null;
        }

        private static PlayerStartInspection BuildPlayerStartInspection(
            Transform player,
            Vector3 lookAt,
            Vector3 frontDirection,
            Vector3 previousPosition,
            float targetDistance)
        {
            var playerToLookAt = lookAt - player.position;
            playerToLookAt.y = 0f;

            var playerForward = player.forward;
            playerForward.y = 0f;

            var playerSide = player.position - lookAt;
            playerSide.y = 0f;

            var facingDot = playerToLookAt.sqrMagnitude > 0.001f && playerForward.sqrMagnitude > 0.001f
                ? Vector3.Dot(playerForward.normalized, playerToLookAt.normalized)
                : -1f;
            var frontSideDot = playerSide.sqrMagnitude > 0.001f
                ? Vector3.Dot(playerSide.normalized, frontDirection.normalized)
                : -1f;
            var horizontalDistance = playerSide.magnitude;

            return new PlayerStartInspection(
                previousPosition,
                player.position,
                player.eulerAngles,
                lookAt,
                targetDistance,
                horizontalDistance,
                facingDot,
                frontSideDot);
        }

        private static GameObject RequireSceneRoot(string objectName)
        {
            var root = GameObject.Find(objectName);
            if (root == null)
            {
                throw new InvalidOperationException($"{objectName} is missing in CargoRunMvp scene.");
            }

            return root;
        }

        private static float CalculateTergoUrzereSpacing(Transform tergoRoot, Transform urzereRoot)
        {
            var zSpacing = Mathf.Abs(tergoRoot.position.z - urzereRoot.position.z);
            if (zSpacing > 0.10f)
            {
                return zSpacing;
            }

            return Mathf.Max(Vector3.Distance(tergoRoot.position, urzereRoot.position), FallbackTergoUrzereSpacing);
        }

        private static Bounds CalculateRendererBounds(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
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

        private static void EnsureUnityFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
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

        private static void WritePlacementSummary(PlacementInspection inspection)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "Monstrum_Static_Placement_Summary.txt");
            var builder = new StringBuilder();
            builder.AppendLine("Monstrum Static Placement Summary");
            builder.AppendLine("Date: 2026-07-09");
            builder.AppendLine("SourceModel: " + SourceModelAbsolutePath);
            builder.AppendLine("UnityModelAsset: " + UnityModelAssetPath);
            builder.AppendLine("Scene: " + CargoRunScenePath);
            builder.AppendLine("PlacementRoot: " + PlacementRootName);
            builder.AppendLine("PlacementObject: " + PlacementObjectName);
            builder.AppendLine("ModelChild: " + ModelChildName);
            builder.AppendLine("MonstrumPosition: " + FormatVector(inspection.MonstrumPosition));
            builder.AppendLine("SocietasPosition: " + FormatVector(inspection.SocietasPosition));
            builder.AppendLine("TergoPosition: " + FormatVector(inspection.TergoPosition));
            builder.AppendLine("UrzerePosition: " + FormatVector(inspection.UrzerePosition));
            builder.AppendLine("TergoUrzereSpacing: " + FormatFloat(inspection.TergoUrzereSpacing));
            builder.AppendLine("SocietasMonstrumSpacing: " + FormatFloat(inspection.SocietasMonstrumSpacing));
            builder.AppendLine("RendererCount: " + inspection.RendererCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("BoundsCenter: " + FormatVector(inspection.Bounds.center));
            builder.AppendLine("BoundsSize: " + FormatVector(inspection.Bounds.size));
            builder.AppendLine("AuthoredNewMaterial: false");
            builder.AppendLine("AnimationApplied: false");
            File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WritePlayerStartSummary(PlayerStartInspection inspection)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "Monstrum_Player_Start_Summary.txt");
            var builder = new StringBuilder();
            builder.AppendLine("Monstrum Player Start Summary");
            builder.AppendLine("Date: 2026-07-09");
            builder.AppendLine("Scene: " + CargoRunScenePath);
            builder.AppendLine("PlacementRoot: " + PlacementRootName);
            builder.AppendLine("PlacementObject: " + PlacementObjectName);
            builder.AppendLine("PlayerRoot: " + PlayerRootName);
            builder.AppendLine("PreviousPosition: " + FormatVector(inspection.PreviousPosition));
            builder.AppendLine("PlayerPosition: " + FormatVector(inspection.PlayerPosition));
            builder.AppendLine("PlayerEulerAngles: " + FormatVector(inspection.PlayerEulerAngles));
            builder.AppendLine("LookAt: " + FormatVector(inspection.LookAt));
            builder.AppendLine("TargetDistance: " + FormatFloat(inspection.TargetDistance));
            builder.AppendLine("HorizontalDistance: " + FormatFloat(inspection.HorizontalDistance));
            builder.AppendLine("FacingDot: " + FormatFloat(inspection.FacingDot));
            builder.AppendLine("FrontSideDot: " + FormatFloat(inspection.FrontSideDot));
            builder.AppendLine("CameraModified: false");
            builder.AppendLine("LightModified: false");
            builder.AppendLine("AnimationApplied: false");
            File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WriteAnimationSlotSummary(AnimationSlotInspection inspection)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "Monstrum_Animation_Slots_Summary.txt");
            var builder = new StringBuilder();
            builder.AppendLine("Monstrum Animation Slots Summary");
            builder.AppendLine("Date: 2026-07-09");
            builder.AppendLine("Scene: " + CargoRunScenePath);
            builder.AppendLine("PlacementRoot: " + PlacementRootName);
            builder.AppendLine("StaticObject: " + PlacementObjectName);
            builder.AppendLine("StaticPosition: " + FormatVector(inspection.StaticPosition));
            builder.AppendLine("StaticBoundsSize: " + FormatVector(inspection.StaticBoundsSize));
            builder.AppendLine("SlotCount: " + inspection.SlotCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("SlotSpacing: " + FormatFloat(inspection.SlotSpacing));
            foreach (var spec in AnimationSlotSpecs)
            {
                builder.AppendLine("Slot: " + spec.ObjectName);
            }

            builder.AppendLine("AnimationClipCreated: false");
            builder.AppendLine("AnimatorControllerCreated: false");
            builder.AppendLine("ModelCopiedFrom: " + PlacementObjectName);
            File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WriteIdleBreathingAnimationSummary(IdleBreathingAnimationResult result)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "Monstrum_02_Idle_Breathing_Animation_Summary.txt");
            var builder = new StringBuilder();
            builder.AppendLine("Monstrum Idle Breathing Animation Summary");
            builder.AppendLine("Date: 2026-07-09");
            builder.AppendLine("Scene: " + CargoRunScenePath);
            builder.AppendLine("PlacementRoot: " + PlacementRootName);
            builder.AppendLine("TargetSlot: " + result.TargetSlotName);
            builder.AppendLine("AnimationMethod: SkinnedMeshRenderer blendShape body morph");
            builder.AppendLine("BlendShapeName: " + IdleBreathingBlendShapeName);
            builder.AppendLine("BodyRendererCount: " + result.BodyRendererCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("SkinnedRendererCount: " + result.SkinnedRendererCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("BreathingMeshAssetCount: " + result.BreathingMeshAssetCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("BreathingMeshAssets: " + result.BreathingMeshAssetPaths);
            builder.AppendLine("AnimationClip: " + result.AnimationClipAssetPath);
            builder.AppendLine("AnimatorController: " + result.AnimatorControllerAssetPath);
            builder.AppendLine("BlendShapeCurveCount: " + result.BlendShapeCurveCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("EyeFollowPositionCurveCount: " + result.EyeFollowPositionCurveCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("TotalCurveBindingCount: " + result.TotalCurveBindingCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("LoopDurationSeconds: " + FormatFloat(IdleBreathingDurationSeconds));
            builder.AppendLine("RootTransformCurveCreated: false");
            builder.AppendLine("EyesModified: true");
            builder.AppendLine("EyeFollowApplied: true");
            builder.AppendLine("OtherSlotsModified: false");
            builder.AppendLine("HarnessValidationRun: false");
            builder.AppendLine("EditModeTestsRun: false");
            builder.AppendLine("PlayModeTestsRun: false");
            builder.AppendLine("BuildRun: false");
            File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WriteMoveSourceAnimationSummary(MoveSourceAnimationResult result)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "Monstrum_03_Move_Source_Animation_Summary.txt");
            var builder = new StringBuilder();
            builder.AppendLine("Monstrum Move Source Animation Summary");
            builder.AppendLine("Date: 2026-07-09");
            builder.AppendLine("Scene: " + CargoRunScenePath);
            builder.AppendLine("PlacementRoot: " + PlacementRootName);
            builder.AppendLine("TargetSlot: " + result.TargetSlotName);
            builder.AppendLine("AnimatorRootPath: " + result.AnimatorRootPath);
            builder.AppendLine("ExternalSourceFbx: " + result.ExternalSourceFbx);
            builder.AppendLine("UnitySourceAnimationAsset: " + result.UnitySourceAnimationAsset);
            builder.AppendLine("SourceClipAssetPath: " + result.SourceClipAssetPath);
            builder.AppendLine("SourceClipName: " + result.SourceClipName);
            builder.AppendLine("AnimatorController: " + result.AnimatorControllerAssetPath);
            builder.AppendLine("ClipCurveBindingCount: " + result.ClipBindingCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("ClipObjectReferenceBindingCount: " + result.ObjectReferenceBindingCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("ClipLoopTime: " + result.ClipLoopTime.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("AnimationClipIsLooping: " + result.AnimationClipIsLooping.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("ExternalSourceFbxModified: false");
            builder.AppendLine("UnityPreparedModelImportAnimationChanged: false");
            builder.AppendLine("ModelingModified: false");
            builder.AppendLine("EyesModified: false");
            builder.AppendLine("MaterialsModified: false");
            builder.AppendLine("IdleAnimationModified: false");
            builder.AppendLine("OtherSlotsModified: false");
            builder.AppendLine("HarnessValidationRun: false");
            builder.AppendLine("EditModeTestsRun: false");
            builder.AppendLine("PlayModeTestsRun: false");
            builder.AppendLine("BuildRun: false");
            File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WriteLooseGrainRemovalSummary(LooseGrainRemovalResult result)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "Monstrum_Loose_Grain_Removal_Summary.txt");
            var builder = new StringBuilder();
            builder.AppendLine("Monstrum Loose Grain Removal Summary");
            builder.AppendLine("Date: 2026-07-09");
            builder.AppendLine("Scene: " + CargoRunScenePath);
            builder.AppendLine("PlacementRoot: " + PlacementRootName);
            builder.AppendLine("SourceModelPreserved: true");
            builder.AppendLine("UnityMeshFolder: " + UnityMeshFolder);
            builder.AppendLine("SourceMeshCount: " + result.SourceMeshCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("AssignedRendererCount: " + result.AssignedRendererCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("RemovedComponentCount: " + result.RemovedComponentCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("RemovedTriangleCount: " + result.RemovedTriangleCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("KeptTriangleCount: " + result.KeptTriangleCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("CleanedMeshAssets: " + result.CleanedMeshAssetPaths);
            builder.AppendLine("BodyRemodeled: false");
            builder.AppendLine("AnimationClipCreated: false");
            File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private static void WriteVisualRecolorEyeSceneSummary(VisualRecolorEyeSceneResult result)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "Monstrum_Visual_Recolor_Eye_UnityApply_Summary.txt");
            var builder = new StringBuilder();
            builder.AppendLine("Monstrum Visual Recolor Eye Unity Apply Summary");
            builder.AppendLine("Date: 2026-07-09");
            builder.AppendLine("ApprovedSample: " + VisualRecolorEyeArtSampleFolder);
            builder.AppendLine("Scene: " + CargoRunScenePath);
            builder.AppendLine("PlacementRoot: " + PlacementRootName);
            builder.AppendLine("TargetCount: " + result.TargetCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("BodyRendererCount: " + result.BodyRendererCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("EyeRendererCount: " + result.EyeRendererCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("EyeLightCount: " + result.EyeLightCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("BodyMaterial: " + ApprovedBodyMaterialAssetPath);
            builder.AppendLine("BodyTexture: " + ApprovedBodyTextureAssetPath);
            builder.AppendLine("EyeMaterial: " + ApprovedEyeMaterialAssetPath);
            builder.AppendLine("EyeRootName: " + ApprovedEyeRootName);
            builder.AppendLine("EyeFollowParentApplied: true");
            builder.AppendLine("RuntimeSceneApplied: true");
            builder.AppendLine("SourceFbxModified: false");
            builder.AppendLine("AnimationApplied: false");
            File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" + FormatFloat(value.x) + ", " + FormatFloat(value.y) + ", " + FormatFloat(value.z) + ")";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatColor(Color value)
        {
            return "(" +
                   FormatFloat(value.r) + ", " +
                   FormatFloat(value.g) + ", " +
                   FormatFloat(value.b) + ", " +
                   FormatFloat(value.a) + ")";
        }

        private readonly struct PlacementInspection
        {
            public PlacementInspection(
                Vector3 monstrumPosition,
                Vector3 societasPosition,
                Vector3 tergoPosition,
                Vector3 urzerePosition,
                float tergoUrzereSpacing,
                float societasMonstrumSpacing,
                int rendererCount,
                Bounds bounds)
            {
                MonstrumPosition = monstrumPosition;
                SocietasPosition = societasPosition;
                TergoPosition = tergoPosition;
                UrzerePosition = urzerePosition;
                TergoUrzereSpacing = tergoUrzereSpacing;
                SocietasMonstrumSpacing = societasMonstrumSpacing;
                RendererCount = rendererCount;
                Bounds = bounds;
            }

            public Vector3 MonstrumPosition { get; }
            public Vector3 SocietasPosition { get; }
            public Vector3 TergoPosition { get; }
            public Vector3 UrzerePosition { get; }
            public float TergoUrzereSpacing { get; }
            public float SocietasMonstrumSpacing { get; }
            public int RendererCount { get; }
            public Bounds Bounds { get; }
        }

        private readonly struct PlayerStartInspection
        {
            public PlayerStartInspection(
                Vector3 previousPosition,
                Vector3 playerPosition,
                Vector3 playerEulerAngles,
                Vector3 lookAt,
                float targetDistance,
                float horizontalDistance,
                float facingDot,
                float frontSideDot)
            {
                PreviousPosition = previousPosition;
                PlayerPosition = playerPosition;
                PlayerEulerAngles = playerEulerAngles;
                LookAt = lookAt;
                TargetDistance = targetDistance;
                HorizontalDistance = horizontalDistance;
                FacingDot = facingDot;
                FrontSideDot = frontSideDot;
            }

            public Vector3 PreviousPosition { get; }
            public Vector3 PlayerPosition { get; }
            public Vector3 PlayerEulerAngles { get; }
            public Vector3 LookAt { get; }
            public float TargetDistance { get; }
            public float HorizontalDistance { get; }
            public float FacingDot { get; }
            public float FrontSideDot { get; }
        }

        private readonly struct AnimationSlotInspection
        {
            public AnimationSlotInspection(Vector3 staticPosition, Vector3 staticBoundsSize, float slotSpacing, int slotCount)
            {
                StaticPosition = staticPosition;
                StaticBoundsSize = staticBoundsSize;
                SlotSpacing = slotSpacing;
                SlotCount = slotCount;
            }

            public Vector3 StaticPosition { get; }
            public Vector3 StaticBoundsSize { get; }
            public float SlotSpacing { get; }
            public int SlotCount { get; }
        }

        private readonly struct IdleBreathingAnimationResult
        {
            public IdleBreathingAnimationResult(
                string targetSlotName,
                int bodyRendererCount,
                int skinnedRendererCount,
                int breathingMeshAssetCount,
                string breathingMeshAssetPaths,
                string animationClipAssetPath,
                string animatorControllerAssetPath,
                int blendShapeCurveCount,
                int eyeFollowPositionCurveCount,
                int totalCurveBindingCount)
            {
                TargetSlotName = targetSlotName;
                BodyRendererCount = bodyRendererCount;
                SkinnedRendererCount = skinnedRendererCount;
                BreathingMeshAssetCount = breathingMeshAssetCount;
                BreathingMeshAssetPaths = breathingMeshAssetPaths;
                AnimationClipAssetPath = animationClipAssetPath;
                AnimatorControllerAssetPath = animatorControllerAssetPath;
                BlendShapeCurveCount = blendShapeCurveCount;
                EyeFollowPositionCurveCount = eyeFollowPositionCurveCount;
                TotalCurveBindingCount = totalCurveBindingCount;
            }

            public string TargetSlotName { get; }
            public int BodyRendererCount { get; }
            public int SkinnedRendererCount { get; }
            public int BreathingMeshAssetCount { get; }
            public string BreathingMeshAssetPaths { get; }
            public string AnimationClipAssetPath { get; }
            public string AnimatorControllerAssetPath { get; }
            public int BlendShapeCurveCount { get; }
            public int EyeFollowPositionCurveCount { get; }
            public int TotalCurveBindingCount { get; }
        }

        private readonly struct MoveSourceAnimationResult
        {
            public MoveSourceAnimationResult(
                string targetSlotName,
                string animatorRootPath,
                string externalSourceFbx,
                string unitySourceAnimationAsset,
                string sourceClipAssetPath,
                string sourceClipName,
                string animatorControllerAssetPath,
                int clipBindingCount,
                int objectReferenceBindingCount,
                bool clipLoopTime,
                bool animationClipIsLooping)
            {
                TargetSlotName = targetSlotName;
                AnimatorRootPath = animatorRootPath;
                ExternalSourceFbx = externalSourceFbx;
                UnitySourceAnimationAsset = unitySourceAnimationAsset;
                SourceClipAssetPath = sourceClipAssetPath;
                SourceClipName = sourceClipName;
                AnimatorControllerAssetPath = animatorControllerAssetPath;
                ClipBindingCount = clipBindingCount;
                ObjectReferenceBindingCount = objectReferenceBindingCount;
                ClipLoopTime = clipLoopTime;
                AnimationClipIsLooping = animationClipIsLooping;
            }

            public string TargetSlotName { get; }
            public string AnimatorRootPath { get; }
            public string ExternalSourceFbx { get; }
            public string UnitySourceAnimationAsset { get; }
            public string SourceClipAssetPath { get; }
            public string SourceClipName { get; }
            public string AnimatorControllerAssetPath { get; }
            public int ClipBindingCount { get; }
            public int ObjectReferenceBindingCount { get; }
            public bool ClipLoopTime { get; }
            public bool AnimationClipIsLooping { get; }
        }

        private readonly struct EyeSampleInfo
        {
            public EyeSampleInfo(
                Vector3 eyeCenter,
                Vector3 leftEyePosition,
                Vector3 rightEyePosition,
                float eyeRadius,
                float eyeSeparation)
            {
                EyeCenter = eyeCenter;
                LeftEyePosition = leftEyePosition;
                RightEyePosition = rightEyePosition;
                EyeRadius = eyeRadius;
                EyeSeparation = eyeSeparation;
            }

            public Vector3 EyeCenter { get; }
            public Vector3 LeftEyePosition { get; }
            public Vector3 RightEyePosition { get; }
            public float EyeRadius { get; }
            public float EyeSeparation { get; }
        }

        private readonly struct VisualRecolorEyeSceneResult
        {
            public VisualRecolorEyeSceneResult(int targetCount, int bodyRendererCount, int eyeRendererCount, int eyeLightCount)
            {
                TargetCount = targetCount;
                BodyRendererCount = bodyRendererCount;
                EyeRendererCount = eyeRendererCount;
                EyeLightCount = eyeLightCount;
            }

            public int TargetCount { get; }
            public int BodyRendererCount { get; }
            public int EyeRendererCount { get; }
            public int EyeLightCount { get; }
        }

        private readonly struct MotionSlotSpec
        {
            public MotionSlotSpec(string objectName)
            {
                ObjectName = objectName;
            }

            public string ObjectName { get; }
        }

        private readonly struct MeshTriangle
        {
            public MeshTriangle(int submesh, int a, int b, int c, float worldArea, Bounds worldBounds)
            {
                Submesh = submesh;
                A = a;
                B = b;
                C = c;
                WorldArea = worldArea;
                WorldBounds = worldBounds;
            }

            public int Submesh { get; }
            public int A { get; }
            public int B { get; }
            public int C { get; }
            public float WorldArea { get; }
            public Bounds WorldBounds { get; }
        }

        private sealed class TriangleComponent
        {
            public readonly List<int> TriangleIndices = new();
            public readonly HashSet<int> VertexIndices = new();
            public readonly List<Vector3> WorldVertices = new();
            public Bounds WorldBounds;
            public float WorldArea;
            private bool hasBounds;

            public void AddTriangle(int triangleIndex, MeshTriangle triangle, Vector3[] worldVertices)
            {
                TriangleIndices.Add(triangleIndex);
                AddVertex(triangle.A, worldVertices[triangle.A]);
                AddVertex(triangle.B, worldVertices[triangle.B]);
                AddVertex(triangle.C, worldVertices[triangle.C]);
                WorldArea += triangle.WorldArea;

                if (!hasBounds)
                {
                    WorldBounds = triangle.WorldBounds;
                    hasBounds = true;
                }
                else
                {
                    WorldBounds.Encapsulate(triangle.WorldBounds);
                }
            }

            private void AddVertex(int vertexIndex, Vector3 worldPosition)
            {
                if (VertexIndices.Add(vertexIndex))
                {
                    WorldVertices.Add(worldPosition);
                }
            }
        }

        private sealed class MeshAssignmentTarget
        {
            private readonly MeshFilter meshFilter;
            private readonly SkinnedMeshRenderer skinnedMeshRenderer;

            private MeshAssignmentTarget(MeshFilter meshFilter, SkinnedMeshRenderer skinnedMeshRenderer, Transform transform, string ownerName, Mesh sharedMesh)
            {
                this.meshFilter = meshFilter;
                this.skinnedMeshRenderer = skinnedMeshRenderer;
                Transform = transform;
                OwnerName = ownerName;
                SharedMesh = sharedMesh;
            }

            public Transform Transform { get; }
            public string OwnerName { get; }
            public Mesh SharedMesh { get; }

            public static MeshAssignmentTarget ForMeshFilter(MeshFilter meshFilter)
            {
                return new MeshAssignmentTarget(meshFilter, null, meshFilter.transform, meshFilter.name, meshFilter.sharedMesh);
            }

            public static MeshAssignmentTarget ForSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return new MeshAssignmentTarget(null, skinnedMeshRenderer, skinnedMeshRenderer.transform, skinnedMeshRenderer.name, skinnedMeshRenderer.sharedMesh);
            }

            public void Assign(Mesh mesh)
            {
                if (meshFilter != null)
                {
                    meshFilter.sharedMesh = mesh;
                    EditorUtility.SetDirty(meshFilter);
                }

                if (skinnedMeshRenderer != null)
                {
                    skinnedMeshRenderer.sharedMesh = mesh;
                    EditorUtility.SetDirty(skinnedMeshRenderer);
                }
            }
        }

        private readonly struct MeshCleanupResult
        {
            public MeshCleanupResult(
                string sourceMeshName,
                string cleanedMeshAssetPath,
                Mesh cleanedMesh,
                int componentCount,
                int removedComponentCount,
                int removedTriangleCount,
                int keptTriangleCount,
                int originalTriangleCount)
            {
                SourceMeshName = sourceMeshName;
                CleanedMeshAssetPath = cleanedMeshAssetPath;
                CleanedMesh = cleanedMesh;
                ComponentCount = componentCount;
                RemovedComponentCount = removedComponentCount;
                RemovedTriangleCount = removedTriangleCount;
                KeptTriangleCount = keptTriangleCount;
                OriginalTriangleCount = originalTriangleCount;
            }

            public string SourceMeshName { get; }
            public string CleanedMeshAssetPath { get; }
            public Mesh CleanedMesh { get; }
            public int ComponentCount { get; }
            public int RemovedComponentCount { get; }
            public int RemovedTriangleCount { get; }
            public int KeptTriangleCount { get; }
            public int OriginalTriangleCount { get; }
        }

        private readonly struct LooseGrainRemovalResult
        {
            public LooseGrainRemovalResult(
                int sourceMeshCount,
                int assignedRendererCount,
                int removedComponentCount,
                int removedTriangleCount,
                int keptTriangleCount,
                string cleanedMeshAssetPaths)
            {
                SourceMeshCount = sourceMeshCount;
                AssignedRendererCount = assignedRendererCount;
                RemovedComponentCount = removedComponentCount;
                RemovedTriangleCount = removedTriangleCount;
                KeptTriangleCount = keptTriangleCount;
                CleanedMeshAssetPaths = cleanedMeshAssetPaths;
            }

            public int SourceMeshCount { get; }
            public int AssignedRendererCount { get; }
            public int RemovedComponentCount { get; }
            public int RemovedTriangleCount { get; }
            public int KeptTriangleCount { get; }
            public string CleanedMeshAssetPaths { get; }
        }

        private sealed class LooseGrainRemovalResultBuilder
        {
            private readonly List<string> cleanedMeshAssetPaths = new();

            public int SourceMeshCount { get; private set; }
            public int AssignedRendererCount { get; set; }
            public int RemovedComponentCount { get; private set; }
            public int RemovedTriangleCount { get; private set; }
            public int KeptTriangleCount { get; private set; }

            public void AddCleanup(MeshCleanupResult cleanup)
            {
                SourceMeshCount++;
                RemovedComponentCount += cleanup.RemovedComponentCount;
                RemovedTriangleCount += cleanup.RemovedTriangleCount;
                KeptTriangleCount += cleanup.KeptTriangleCount;
                cleanedMeshAssetPaths.Add(cleanup.CleanedMeshAssetPath);
            }

            public LooseGrainRemovalResult Build()
            {
                return new LooseGrainRemovalResult(
                    SourceMeshCount,
                    AssignedRendererCount,
                    RemovedComponentCount,
                    RemovedTriangleCount,
                    KeptTriangleCount,
                    string.Join(", ", cleanedMeshAssetPaths));
            }
        }

        private sealed class DisjointSet
        {
            private readonly List<int> parents = new();
            private readonly List<int> ranks = new();

            public int Add()
            {
                var index = parents.Count;
                parents.Add(index);
                ranks.Add(0);
                return index;
            }

            public int Find(int value)
            {
                if (parents[value] != value)
                {
                    parents[value] = Find(parents[value]);
                }

                return parents[value];
            }

            public void Union(int a, int b)
            {
                var rootA = Find(a);
                var rootB = Find(b);
                if (rootA == rootB)
                {
                    return;
                }

                if (ranks[rootA] < ranks[rootB])
                {
                    parents[rootA] = rootB;
                }
                else if (ranks[rootA] > ranks[rootB])
                {
                    parents[rootB] = rootA;
                }
                else
                {
                    parents[rootB] = rootA;
                    ranks[rootA]++;
                }
            }
        }

        private readonly struct QuantizedVector3 : IEquatable<QuantizedVector3>
        {
            public QuantizedVector3(Vector3 value, float tolerance)
            {
                X = Mathf.RoundToInt(value.x / tolerance);
                Y = Mathf.RoundToInt(value.y / tolerance);
                Z = Mathf.RoundToInt(value.z / tolerance);
            }

            private int X { get; }
            private int Y { get; }
            private int Z { get; }

            public bool Equals(QuantizedVector3 other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is QuantizedVector3 other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = X;
                    hash = (hash * 397) ^ Y;
                    hash = (hash * 397) ^ Z;
                    return hash;
                }
            }
        }
    }
}
