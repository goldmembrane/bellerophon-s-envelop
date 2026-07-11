using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Bellerophon.Editor.ConSpiritoCargoRunScene
{
    internal static class ConSpiritoCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LongaArmaPlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoPlacementRootName = "Approved Tergo Enemy Placement";
        private const string CantabilePlacementRootName = "Approved Cantabile Enemy Placement";
        private const string PlacementRootName = "Approved Con Spirito Enemy Placement";
        private const string PlacementObjectName = "ConSpirito_00_Static_Review";
        private const string ModelChildName = "ConSpiritoRerigged_Model";
        private const string PlayerRootName = "Player";

        private const string UnityModelAssetPath = "Assets/_Project/Art/Enemies/ConSpirito/Models/con_spirito_rerigged.fbx";
        private const string OriginalUnityModelAssetPath = "Assets/_Project/Art/Enemies/ConSpirito/Models/con_spirito_original.fbx";
        private const string ConSpiritoArtRoot = "Assets/_Project/Art/Enemies/ConSpirito";
        private const string UnityAnimationFolder = ConSpiritoArtRoot + "/Animations";
        private const string UnityControllerFolder = ConSpiritoArtRoot + "/Controllers";
        private const string UnityTextureFolder = ConSpiritoArtRoot + "/Textures";
        private const string UnityMaterialFolder = ConSpiritoArtRoot + "/Materials";
        private const string DefaultLoopControllerAssetPath = UnityControllerFolder + "/ConSpirito_DefaultLoop.controller";
        private const string OriginalLoopControllerAssetPath = UnityControllerFolder + "/ConSpirito_OriginalWalkLoop.controller";
        private const string DogWalkClipName = "ConSpirito_DogWalk_Loop";
        private const string DogWalkClipAssetPath = UnityAnimationFolder + "/" + DogWalkClipName + ".anim";
        private const string DogWalkControllerAssetPath = UnityControllerFolder + "/ConSpirito_DogWalk_Loop.controller";
        private const string IdleBreathClipName = "ConSpirito_IdleBreath_Loop";
        private const string IdleBreathClipAssetPath = UnityAnimationFolder + "/" + IdleBreathClipName + ".anim";
        private const string IdleBreathControllerAssetPath = UnityControllerFolder + "/ConSpirito_IdleBreath_Loop.controller";
        private const string ChargeClipName = "ConSpirito_Charge_Loop";
        private const string ChargeClipAssetPath = UnityAnimationFolder + "/" + ChargeClipName + ".anim";
        private const string ChargeControllerAssetPath = UnityControllerFolder + "/ConSpirito_Charge_Loop.controller";
        private const string ValidationFolder = "docs/validation/con_spirito";
        private const string ReferenceRunAnalysisFolder = ValidationFolder + "/reference_run_analysis";
        private const string CurrentConSpiritoRunVideoPath = "C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-07-11 21-12-51.mp4";
        private const string DogRunReferenceVideoPath = "C:/Users/gus68/Downloads/video.mp4";
        private const string OriginalModelChildName = "ConSpiritoOriginal_Model";
        private const string ApprovedSampleAlbedoSourcePath = "artSample/enemies/con_spirito/textures/con_spirito_blood_red_fur_albedo.png";
        private const string ApprovedSampleBumpSourcePath = "artSample/enemies/con_spirito/textures/con_spirito_fur_direction_bump.png";
        private const string ApprovedUnityAlbedoAssetPath = UnityTextureFolder + "/con_spirito_blood_red_fur_albedo.png";
        private const string ApprovedUnityBumpAssetPath = UnityTextureFolder + "/con_spirito_fur_direction_bump.png";
        private const string ApprovedUnityMaterialAssetPath = UnityMaterialFolder + "/ConSpirito_Approved_BrightRedFur.mat";

        private const float ConSpiritoFacingYawDegrees = 180f;
        private const float UnityImportUnitScale = 100f;
        private const float PlayerFrontDistance = 4.00f;
        private const float LongaTergoFallbackSpacing = 4.00f;
        private const float PlacementToleranceMeters = 0.05f;
        private const float PlayerFacingToleranceDot = 0.94f;
        private const float DogWalkLoopDurationSeconds = 1.20f;
        private const float DogWalkLegSwingDegrees = 34f;
        private const float DogWalkLegLiftDegrees = 14f;
        private const float DogWalkLegSplayDegrees = 7f;
        private const float DogWalkLegLiftMeters = 0.055f;
        private const float DogWalkLegStrideMeters = 0.055f;
        private const float DogWalkBodyBobMeters = 0.045f;
        private const float DogWalkBodyPitchDegrees = 4.0f;
        private const float DogWalkBodyRollDegrees = 3.0f;
        private const float IdleBreathLoopDurationSeconds = 2.80f;
        private const float IdleBreathWidthExpansion = 0.050f;
        private const float IdleBreathHeightExpansion = 0.008f;
        private const float IdleBreathLengthExpansion = 0.024f;
        private const float IdleBreathExhaleCompression = 0.006f;
        private const float IdleBreathBodyLiftMeters = 0.012f;
        private const int IdleBreathTargetLimit = 2;
        private const float ChargeRunLoopDurationSeconds = 0.56f;
        private const float ChargeRunLegSwingDegrees = 64f;
        private const float ChargeRunLegLiftDegrees = 18f;
        private const float ChargeRunLegSplayDegrees = 3f;
        private const float ChargeRunLegLiftMeters = 0.300f;
        private const float ChargeRunLegStrideMeters = 0.250f;
        private const float ChargeRunLegLateralMeters = 0.035f;
        private const float ChargeRunBodyBobMeters = 0.050f;
        private const float ChargeRunBodyForwardPitchDegrees = 5f;
        private const float ChargeRunBodyRollDegrees = 3.5f;
        private const float ChargeForwardLeanDegrees = 23.0f;
        private const float ChargeHeadForwardMeters = 0.032f;
        private const float ChargeChestForwardMeters = 0.025f;
        private const float ChargeHeadDownMeters = 0.010f;
        private const float AnimationReviewSlotSpacingMeters = 1.35f;
        private const int AnimationReviewIdleSlotIndex = 1;
        private const int AnimationReviewWalkingSlotIndex = 2;
        private const int AnimationReviewChargeSlotIndex = 3;

        private static readonly string[] AnimationReviewSlotNames =
        {
            "ConSpirito_00_Static",
            "ConSpirito_01_Idle",
            "ConSpirito_02_Walk",
            "ConSpirito_03_Charge",
            "ConSpirito_04_Rest",
            "ConSpirito_05_Play",
            "ConSpirito_06_Death"
        };

        private static readonly string[] AnimationReviewSlotLabels =
        {
            "Static",
            "Idle",
            "Walk",
            "Charge",
            "Rest",
            "Play",
            "Death"
        };

        private static readonly string[] RequiredLegBoneNames =
        {
            "frontleg",
            "R_frontleg",
            "backleg",
            "R_backleg"
        };

        private static readonly string[] RemovedChildLegBoneNames =
        {
            "frontleg0",
            "frontleg1",
            "frontleg2",
            "R_frontleg0",
            "R_frontleg1",
            "R_frontleg2",
            "backleg0",
            "backleg1",
            "backleg2",
            "R_backleg0",
            "R_backleg1",
            "R_backleg2"
        };

        private static readonly string[][] ChargeRunLegChains =
        {
            new[] { "frontleg", "frontleg0", "frontleg1", "frontleg2" },
            new[] { "R_frontleg", "R_frontleg0", "R_frontleg1", "R_frontleg2" },
            new[] { "backleg", "backleg0", "backleg1", "backleg2" },
            new[] { "R_backleg", "R_backleg0", "R_backleg1", "R_backleg2" }
        };

        [MenuItem("Bellerophon/Enemies/Con Spirito/Apply Rerigged Model To CargoRunMvp")]
        public static void ApplyReriggedModelToCurrentCargoRunScene()
        {
            RequireReriggedModelAssetFile();
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImportedModelAsset();

            var modelAsset = LoadReriggedModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlaceReriggedModel(modelAsset, scene);
            ConfigureInitialPlayerStart(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Rerigged Con Spirito model applied to CargoRunMvp scene.");
        }

        public static void InspectAppliedSceneState()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            Debug.Log("Rerigged Con Spirito CargoRunMvp scene state inspected.");
        }

        public static void CaptureReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var focus = RequirePlacementObject(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            CaptureTransformToPng(
                focus,
                Path.Combine(outputDirectory, "ConSpirito_Rerigged_StaticReview_UnityCapture.png"),
                1600,
                900);

            Debug.Log("Con Spirito rerigged Unity review capture saved.");
        }

        [MenuItem("Bellerophon/Enemies/Con Spirito/Apply Default Animation Loop")]
        public static void ApplyDefaultAnimationLoopToCurrentCargoRunScene()
        {
            RequireReriggedModelAssetFile();
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImportedModelAsset();

            var defaultClip = LoadDefaultAnimationClip();
            ConfigureDefaultAnimationClipLoop(defaultClip);
            defaultClip = LoadDefaultAnimationClip();
            var controller = EnsureDefaultLoopController(defaultClip);

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            ApplyDefaultLoopController(placementRoot.transform, controller);
            InspectSceneState(placementRoot.transform);
            InspectDefaultAnimationLoop(placementRoot.transform, defaultClip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Con Spirito default FBX animation loop applied to current CargoRunMvp scene.");
        }

        public static void InspectDefaultAnimationLoopInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var defaultClip = LoadDefaultAnimationClip();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DefaultLoopControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Con Spirito default loop controller is missing at {DefaultLoopControllerAssetPath}.");
            }

            InspectSceneState(placementRoot.transform);
            InspectDefaultAnimationLoop(placementRoot.transform, defaultClip, controller);
            Debug.Log("Con Spirito default FBX animation loop scene state inspected.");
        }

        [MenuItem("Bellerophon/Enemies/Con Spirito/Apply Dog Walk Loop")]
        public static void ApplyDogWalkLoopToCurrentCargoRunScene()
        {
            RequireReriggedModelAssetFile();
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImportedModelAsset();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var clip = EnsureDogWalkClip(placementRoot.transform);
            var controller = EnsureDogWalkController(clip);
            ApplyAnimatorController(placementRoot.transform, controller);
            InspectSceneState(placementRoot.transform);
            InspectDogWalkLoop(placementRoot.transform, clip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Con Spirito dog walk loop applied to current CargoRunMvp scene.");
        }

        public static void InspectDogWalkLoopInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DogWalkClipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Con Spirito dog walk clip is missing at {DogWalkClipAssetPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DogWalkControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Con Spirito dog walk controller is missing at {DogWalkControllerAssetPath}.");
            }

            InspectSceneState(placementRoot.transform);
            InspectDogWalkLoop(placementRoot.transform, clip, controller);
            Debug.Log("Con Spirito dog walk loop scene state inspected.");
        }

        public static void CaptureDogWalkLoopReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DogWalkClipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Con Spirito dog walk clip is missing at {DogWalkClipAssetPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DogWalkControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Con Spirito dog walk controller is missing at {DogWalkControllerAssetPath}.");
            }

            InspectDogWalkLoop(placementRoot.transform, clip, controller);

            var reviewObject = RequirePlacementObject(placementRoot.transform);
            var modelObject = RequireModelObject(reviewObject);
            var snapshots = CaptureTransformSnapshots(modelObject);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            try
            {
                var sampleFractions = new[] { 0f, 0.25f, 0.50f, 0.75f, 1.00f };
                foreach (var fraction in sampleFractions)
                {
                    RestoreTransformSnapshots(snapshots);
                    clip.SampleAnimation(modelObject.gameObject, Mathf.Clamp(DogWalkLoopDurationSeconds * fraction, 0f, DogWalkLoopDurationSeconds));
                    CaptureTransformToPng(
                        reviewObject,
                        Path.Combine(outputDirectory, $"ConSpirito_DogWalk_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900);
                    CaptureTransformToPng(
                        reviewObject,
                        Path.Combine(outputDirectory, $"ConSpirito_DogWalk_Oblique_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900,
                        35f);
                }
            }
            finally
            {
                RestoreTransformSnapshots(snapshots);
            }

            Debug.Log("Con Spirito dog walk loop review captures saved.");
        }

        [MenuItem("Bellerophon/Enemies/Con Spirito/Apply Original FBX Animation Loop")]
        public static void ApplyOriginalAnimationLoopToCurrentCargoRunScene()
        {
            RequireOriginalModelAssetFile();
            AssetDatabase.ImportAsset(OriginalUnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImportedModelAsset(OriginalUnityModelAssetPath);

            var originalClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            ConfigureImportedAnimationClipLoop(originalClip, OriginalUnityModelAssetPath);
            originalClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var controller = EnsureLoopController(OriginalLoopControllerAssetPath, "ConSpirito_OriginalFBXWalkLoop", originalClip);

            var modelAsset = LoadOriginalModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlaceOriginalModel(modelAsset, scene);
            ConfigureInitialPlayerStart(placementRoot.transform);
            ApplyAnimatorController(placementRoot.transform, OriginalModelChildName, modelAsset, controller);
            InspectOriginalSceneState(placementRoot.transform);
            InspectOriginalAnimationLoop(placementRoot.transform, originalClip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Original Con Spirito FBX animation loop applied to current CargoRunMvp scene.");
        }

        public static void InspectOriginalAnimationLoopInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var originalClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OriginalLoopControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Con Spirito original loop controller is missing at {OriginalLoopControllerAssetPath}.");
            }

            InspectOriginalSceneState(placementRoot.transform);
            InspectOriginalAnimationLoop(placementRoot.transform, originalClip, controller);
            Debug.Log("Original Con Spirito FBX animation loop scene state inspected.");
        }

        public static void CaptureOriginalAnimationLoopReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var originalClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OriginalLoopControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Con Spirito original loop controller is missing at {OriginalLoopControllerAssetPath}.");
            }

            InspectOriginalAnimationLoop(placementRoot.transform, originalClip, controller);

            var reviewObject = RequirePlacementObject(placementRoot.transform);
            var modelObject = RequireModelObject(reviewObject, OriginalModelChildName);
            var snapshots = CaptureTransformSnapshots(modelObject);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            try
            {
                var sampleFractions = new[] { 0f, 0.25f, 0.50f, 0.75f, 1.00f };
                foreach (var fraction in sampleFractions)
                {
                    RestoreTransformSnapshots(snapshots);
                    originalClip.SampleAnimation(modelObject.gameObject, Mathf.Clamp(originalClip.length * fraction, 0f, originalClip.length));
                    CaptureTransformToPng(
                        reviewObject,
                        Path.Combine(outputDirectory, $"ConSpirito_OriginalWalk_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900);
                    CaptureTransformToPng(
                        reviewObject,
                        Path.Combine(outputDirectory, $"ConSpirito_OriginalWalk_Oblique_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900,
                        35f);
                }
            }
            finally
            {
                RestoreTransformSnapshots(snapshots);
            }

            Debug.Log("Original Con Spirito FBX animation loop review captures saved.");
        }

        [MenuItem("Bellerophon/Enemies/Con Spirito/Apply Animation Review Slots")]
        public static void ApplyAnimationReviewSlots()
        {
            RequireOriginalModelAssetFile();
            AssetDatabase.ImportAsset(OriginalUnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImportedModelAsset(OriginalUnityModelAssetPath);

            var originalClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            ConfigureImportedAnimationClipLoop(originalClip, OriginalUnityModelAssetPath);
            originalClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var controller = EnsureLoopController(OriginalLoopControllerAssetPath, "ConSpirito_OriginalFBXWalkLoop", originalClip);

            var modelAsset = LoadOriginalModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = EnsurePlacementRootAtConSpiritoPosition(scene);
            ClearChildren(placementRoot.transform);

            for (var index = 0; index < AnimationReviewSlotNames.Length; index++)
            {
                var slotRoot = CreateAnimationReviewSlot(placementRoot.transform, modelAsset, index);
                if (index == AnimationReviewWalkingSlotIndex)
                {
                    var modelObject = RequireModelObject(slotRoot.transform, OriginalModelChildName);
                    ConfigureAnimatorOnModel(modelObject, modelAsset, controller);
                }
                else
                {
                    DisableImportedAnimationPlayback(slotRoot.transform);
                }
            }

            InspectAnimationReviewSlots(placementRoot.transform, originalClip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Con Spirito animation review slots applied.");
        }

        public static void InspectAnimationReviewSlotsInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var originalClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OriginalLoopControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Con Spirito original loop controller is missing at {OriginalLoopControllerAssetPath}.");
            }

            InspectAnimationReviewSlots(placementRoot.transform, originalClip, controller);
            Debug.Log("Con Spirito animation review slots inspected.");
        }

        public static void CaptureAnimationReviewSlots()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var originalClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OriginalLoopControllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Con Spirito original loop controller is missing at {OriginalLoopControllerAssetPath}.");
            }

            InspectAnimationReviewSlots(placementRoot.transform, originalClip, controller);

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            CaptureTransformToPng(
                placementRoot.transform,
                Path.Combine(outputDirectory, "ConSpirito_AnimationReviewSlots_Front.png"),
                2400,
                900,
                180f,
                50f,
                3.60f,
                16.00f);
            CaptureTransformToPng(
                placementRoot.transform,
                Path.Combine(outputDirectory, "ConSpirito_AnimationReviewSlots_Oblique.png"),
                2400,
                900,
                145f,
                50f,
                3.60f,
                16.00f);

            Debug.Log("Con Spirito animation review slots captured.");
        }

        [MenuItem("Bellerophon/Enemies/Con Spirito/Apply Idle Breath Loop")]
        public static void ApplyIdleBreathLoopToCurrentScene()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var idleSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewIdleSlotIndex);
            var idleModelObject = RequireModelObject(idleSlot, OriginalModelChildName);
            var idleClip = EnsureIdleBreathClip(idleModelObject);
            var idleController = EnsureLoopController(IdleBreathControllerAssetPath, IdleBreathClipName, idleClip);
            var modelAsset = LoadOriginalModelAsset();
            ConfigureAnimatorOnModel(idleModelObject, modelAsset, idleController);

            var walkClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var walkController = LoadRequiredAnimatorController(OriginalLoopControllerAssetPath);
            InspectIdleBreathLoop(placementRoot.transform, idleClip, idleController, walkClip, walkController);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Con Spirito idle breath loop applied.");
        }

        public static void InspectIdleBreathLoopInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var idleClip = LoadRequiredAnimationClip(IdleBreathClipAssetPath);
            var idleController = LoadRequiredAnimatorController(IdleBreathControllerAssetPath);
            var walkClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var walkController = LoadRequiredAnimatorController(OriginalLoopControllerAssetPath);
            InspectIdleBreathLoop(placementRoot.transform, idleClip, idleController, walkClip, walkController);

            Debug.Log("Con Spirito idle breath loop inspected.");
        }

        public static void CaptureIdleBreathLoopReview()
        {
            InspectIdleBreathLoopInScene();

            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var idleSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewIdleSlotIndex);
            var idleModelObject = RequireModelObject(idleSlot, OriginalModelChildName);
            var idleClip = LoadRequiredAnimationClip(IdleBreathClipAssetPath);
            var snapshots = CaptureTransformSnapshots(idleModelObject);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            try
            {
                var sampleFractions = new[] { 0f, 0.25f, 0.50f, 0.75f, 1.00f };
                foreach (var fraction in sampleFractions)
                {
                    RestoreTransformSnapshots(snapshots);
                    idleClip.SampleAnimation(
                        idleModelObject.gameObject,
                        Mathf.Clamp(IdleBreathLoopDurationSeconds * fraction, 0f, IdleBreathLoopDurationSeconds));
                    CaptureTransformToPng(
                        idleSlot,
                        Path.Combine(outputDirectory, $"ConSpirito_IdleBreath_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900);
                    CaptureTransformToPng(
                        idleSlot,
                        Path.Combine(outputDirectory, $"ConSpirito_IdleBreath_Oblique_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900,
                        35f);
                }
            }
            finally
            {
                RestoreTransformSnapshots(snapshots);
            }

            Debug.Log("Con Spirito idle breath loop review captures saved.");
        }

        [MenuItem("Bellerophon/Enemies/Con Spirito/Apply Charge Loop")]
        public static void ApplyChargeLoopToCurrentScene()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var chargeSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewChargeSlotIndex);
            var chargeModelObject = RequireModelObject(chargeSlot, OriginalModelChildName);
            var sourceWalkClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var chargeClip = EnsureChargeClip(sourceWalkClip, chargeModelObject);
            var chargeController = EnsureLoopController(ChargeControllerAssetPath, ChargeClipName, chargeClip);
            var modelAsset = LoadOriginalModelAsset();
            ConfigureAnimatorOnModel(chargeModelObject, modelAsset, chargeController);

            var idleClip = LoadRequiredAnimationClip(IdleBreathClipAssetPath);
            var idleController = LoadRequiredAnimatorController(IdleBreathControllerAssetPath);
            var walkController = LoadRequiredAnimatorController(OriginalLoopControllerAssetPath);
            InspectChargeLoop(placementRoot.transform, chargeClip, chargeController, idleClip, idleController, sourceWalkClip, walkController);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Con Spirito charge loop applied.");
        }

        public static void InspectChargeLoopInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var chargeClip = LoadRequiredAnimationClip(ChargeClipAssetPath);
            var chargeController = LoadRequiredAnimatorController(ChargeControllerAssetPath);
            var idleClip = LoadRequiredAnimationClip(IdleBreathClipAssetPath);
            var idleController = LoadRequiredAnimatorController(IdleBreathControllerAssetPath);
            var walkClip = LoadImportedAnimationClip(OriginalUnityModelAssetPath, preferWalkClip: true);
            var walkController = LoadRequiredAnimatorController(OriginalLoopControllerAssetPath);
            InspectChargeLoop(placementRoot.transform, chargeClip, chargeController, idleClip, idleController, walkClip, walkController);

            Debug.Log("Con Spirito charge loop inspected.");
        }

        public static void CaptureChargeLoopReview()
        {
            InspectChargeLoopInScene();

            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var chargeSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewChargeSlotIndex);
            var chargeModelObject = RequireModelObject(chargeSlot, OriginalModelChildName);
            var chargeClip = LoadRequiredAnimationClip(ChargeClipAssetPath);
            var snapshots = CaptureTransformSnapshots(chargeModelObject);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            try
            {
                var sampleFractions = new[] { 0f, 0.125f, 0.25f, 0.375f, 0.50f, 0.625f, 0.75f, 0.875f, 1.00f };
                foreach (var fraction in sampleFractions)
                {
                    RestoreTransformSnapshots(snapshots);
                    chargeClip.SampleAnimation(
                        chargeModelObject.gameObject,
                        Mathf.Clamp(chargeClip.length * fraction, 0f, chargeClip.length));
                    CaptureTransformToPng(
                        chargeSlot,
                        Path.Combine(outputDirectory, $"ConSpirito_Charge_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900);
                    CaptureTransformToPng(
                        chargeSlot,
                        Path.Combine(outputDirectory, $"ConSpirito_Charge_Oblique_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900,
                        35f);
                    CaptureTransformToPng(
                        chargeSlot,
                        Path.Combine(outputDirectory, $"ConSpirito_Charge_LeftSide_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900,
                        90f);
                    CaptureTransformToPng(
                        chargeSlot,
                        Path.Combine(outputDirectory, $"ConSpirito_Charge_RightSide_{Mathf.RoundToInt(fraction * 100f):000}.png"),
                        1600,
                        900,
                        -90f);
                }
            }
            finally
            {
                RestoreTransformSnapshots(snapshots);
            }

            Debug.Log("Con Spirito charge loop review captures saved.");
        }

        public static void CaptureReferenceRunVideos()
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReferenceRunAnalysisFolder));
            Directory.CreateDirectory(outputDirectory);

            CaptureVideoReferenceFrames(DogRunReferenceVideoPath, "DogRunReference", outputDirectory);
            CaptureVideoReferenceFrames(CurrentConSpiritoRunVideoPath, "CurrentConSpiritoCharge", outputDirectory);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log($"Con Spirito reference run videos captured to {outputDirectory}.");
        }

        public static void StartReferenceRunVideoCapture(Action<string> completeCallback, Action<Exception> failCallback)
        {
            ReferenceRunVideoCaptureSession.Start(completeCallback, failCallback);
        }

        public static void InspectMaterialStateInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            RequireOriginalModelAssetFile();

            var importer = AssetImporter.GetAtPath(OriginalUnityModelAssetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Con Spirito original model importer is missing at {OriginalUnityModelAssetPath}.");
            }

            var externalObjectMap = importer.GetExternalObjectMap();
            Debug.Log(
                "ConSpiritoMaterialImporterInspection " +
                $"Asset={OriginalUnityModelAssetPath}, " +
                $"MaterialImportMode={importer.materialImportMode}, " +
                $"MaterialLocation={importer.materialLocation}, " +
                $"MaterialName={importer.materialName}, " +
                $"MaterialSearch={importer.materialSearch}, " +
                $"ImportNormals={importer.importNormals}, " +
                $"ImportTangents={importer.importTangents}, " +
                $"ExternalObjectCount={externalObjectMap.Count}.");

            var assetModel = LoadOriginalModelAsset();
            var assetSummary = InspectMaterialSet(assetModel.transform, "AssetModel");

            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var sceneSummary = new MaterialInspectionSummary("SceneSlots");
            for (var index = 0; index < AnimationReviewSlotNames.Length; index++)
            {
                var slotRoot = placementRoot.transform.Find(AnimationReviewSlotNames[index]);
                if (slotRoot == null)
                {
                    throw new InvalidOperationException($"Con Spirito animation review slot is missing: {AnimationReviewSlotNames[index]}.");
                }

                var modelObject = RequireModelObject(slotRoot, OriginalModelChildName);
                InspectMaterialSet(modelObject, AnimationReviewSlotNames[index], sceneSummary);
            }

            Debug.Log(assetSummary.ToLogLine("ConSpiritoMaterialAssetInspection"));
            Debug.Log(sceneSummary.ToLogLine("ConSpiritoMaterialSceneInspection"));
        }

        public static void CaptureMaterialInspection()
        {
            InspectMaterialStateInScene();

            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var walkSlot = placementRoot.transform.Find(AnimationReviewSlotNames[AnimationReviewWalkingSlotIndex]);
            if (walkSlot == null)
            {
                throw new InvalidOperationException($"Con Spirito walking review slot is missing: {AnimationReviewSlotNames[AnimationReviewWalkingSlotIndex]}.");
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            CaptureTransformToPng(
                placementRoot.transform,
                Path.Combine(outputDirectory, "ConSpirito_MaterialInspection_Slots.png"),
                2400,
                900,
                180f,
                50f,
                3.60f,
                16.00f);
            CaptureTransformToPng(
                walkSlot,
                Path.Combine(outputDirectory, "ConSpirito_MaterialInspection_WalkSlot.png"),
                1600,
                900,
                180f,
                34f,
                2.25f,
                6.00f);

            Debug.Log("Con Spirito material inspection captures saved.");
        }

        public static void ApplyApprovedMaterialSampleToCurrentScene()
        {
            var approvedMaterial = EnsureApprovedMaterialSampleAssets();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            ApplyApprovedMaterialSample(placementRoot.transform, approvedMaterial);
            InspectApprovedMaterialSample(placementRoot.transform, approvedMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Con Spirito approved material sample applied to current scene.");
        }

        public static void InspectApprovedMaterialSampleInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var approvedMaterial = LoadApprovedMaterialSampleAsset();
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectApprovedMaterialSample(placementRoot.transform, approvedMaterial);
            Debug.Log("Con Spirito approved material sample inspected.");
        }

        public static void CaptureApprovedMaterialSampleReview()
        {
            InspectApprovedMaterialSampleInScene();

            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var walkSlot = placementRoot.transform.Find(AnimationReviewSlotNames[AnimationReviewWalkingSlotIndex]);
            if (walkSlot == null)
            {
                throw new InvalidOperationException($"Con Spirito walking review slot is missing: {AnimationReviewSlotNames[AnimationReviewWalkingSlotIndex]}.");
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            CaptureTransformToPng(
                placementRoot.transform,
                Path.Combine(outputDirectory, "ConSpirito_ApprovedMaterialSample_Slots.png"),
                2400,
                900,
                180f,
                50f,
                3.60f,
                16.00f);
            CaptureTransformToPng(
                walkSlot,
                Path.Combine(outputDirectory, "ConSpirito_ApprovedMaterialSample_WalkSlot.png"),
                1600,
                900,
                180f,
                34f,
                2.25f,
                6.00f);

            Debug.Log("Con Spirito approved material sample captures saved.");
        }

        private static void RequireReriggedModelAssetFile()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var assetFilePath = Path.GetFullPath(Path.Combine(projectRoot, UnityModelAssetPath));
            if (!File.Exists(assetFilePath))
            {
                throw new FileNotFoundException("Rerigged Con Spirito FBX is missing.", assetFilePath);
            }
        }

        private static void RequireOriginalModelAssetFile()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var assetFilePath = Path.GetFullPath(Path.Combine(projectRoot, OriginalUnityModelAssetPath));
            if (!File.Exists(assetFilePath))
            {
                throw new FileNotFoundException("Original Con Spirito FBX Unity asset is missing.", assetFilePath);
            }
        }

        private static GameObject LoadReriggedModelAsset()
        {
            return LoadModelAsset(UnityModelAssetPath, "rerigged Con Spirito");
        }

        private static GameObject LoadOriginalModelAsset()
        {
            return LoadModelAsset(OriginalUnityModelAssetPath, "original Con Spirito");
        }

        private static GameObject LoadModelAsset(string modelAssetPath, string label)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Could not load {label} model asset at {modelAssetPath}.");
            }

            return modelAsset;
        }

        private static void ConfigureImportedModelAsset()
        {
            ConfigureImportedModelAsset(UnityModelAssetPath);
        }

        private static void ConfigureImportedModelAsset(string modelAssetPath)
        {
            var importer = AssetImporter.GetAtPath(modelAssetPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            var changed = false;
            if (!Mathf.Approximately(importer.globalScale, UnityImportUnitScale))
            {
                importer.globalScale = UnityImportUnitScale;
                changed = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                changed = true;
            }

            var defaultClips = importer.defaultClipAnimations;
            if (defaultClips.Length > 0)
            {
                for (var i = 0; i < defaultClips.Length; i++)
                {
                    defaultClips[i].loopTime = true;
                    defaultClips[i].wrapMode = WrapMode.Loop;
                }

                importer.clipAnimations = defaultClips;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static AnimationClip LoadDefaultAnimationClip()
        {
            return LoadImportedAnimationClip(UnityModelAssetPath, preferWalkClip: false);
        }

        private static AnimationClip LoadImportedAnimationClip(string modelAssetPath, bool preferWalkClip)
        {
            AnimationClip fallbackClip = null;
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(modelAssetPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__", StringComparison.Ordinal))
                {
                    if (fallbackClip == null)
                    {
                        fallbackClip = clip;
                    }

                    if (preferWalkClip && IsWalkClipName(clip.name))
                    {
                        return clip;
                    }
                }
            }

            if (fallbackClip != null)
            {
                return fallbackClip;
            }

            throw new InvalidOperationException($"No animation clip was imported from {modelAssetPath}.");
        }

        private static bool IsWalkClipName(string clipName)
        {
            return clipName.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   clipName.IndexOf("move", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   clipName.IndexOf("locomotion", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ConfigureDefaultAnimationClipLoop(AnimationClip defaultClip)
        {
            ConfigureImportedAnimationClipLoop(defaultClip, UnityModelAssetPath);
        }

        private static void ConfigureImportedAnimationClipLoop(AnimationClip defaultClip, string modelAssetPath)
        {
            var clipSettings = AnimationUtility.GetAnimationClipSettings(defaultClip);
            var changed = false;
            if (!clipSettings.loopTime)
            {
                clipSettings.loopTime = true;
                changed = true;
            }

            if (defaultClip.wrapMode != WrapMode.Loop)
            {
                defaultClip.wrapMode = WrapMode.Loop;
                changed = true;
            }

            if (changed)
            {
                AnimationUtility.SetAnimationClipSettings(defaultClip, clipSettings);
                EditorUtility.SetDirty(defaultClip);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(modelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
        }

        private static AnimatorController EnsureDefaultLoopController(AnimationClip defaultClip)
        {
            return EnsureLoopController(DefaultLoopControllerAssetPath, "ConSpirito_DefaultFBXLoop", defaultClip);
        }

        private static AnimatorController EnsureLoopController(string controllerAssetPath, string stateName, AnimationClip clip)
        {
            EnsureUnityFolder(ConSpiritoArtRoot);
            EnsureUnityFolder(UnityControllerFolder);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states)
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(stateName);
            state.motion = clip;
            state.writeDefaultValues = true;
            state.speed = 1f;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadRequiredAnimationClip(string clipAssetPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Required Con Spirito animation clip is missing at {clipAssetPath}.");
            }

            return clip;
        }

        private static AnimatorController LoadRequiredAnimatorController(string controllerAssetPath)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerAssetPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Required Con Spirito animator controller is missing at {controllerAssetPath}.");
            }

            return controller;
        }

        private static void EnsureUnityFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                throw new InvalidOperationException($"Could not create Unity folder: {folderPath}.");
            }

            EnsureUnityFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void ApplyDefaultLoopController(Transform placementRoot, AnimatorController controller)
        {
            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = reviewObject.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {PlacementObjectName}.");
            }

            var animator = modelObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = modelObject.gameObject.AddComponent<Animator>();
            }

            var modelAsset = LoadReriggedModelAsset();
            var assetAnimator = modelAsset.GetComponent<Animator>();
            if (assetAnimator != null && assetAnimator.avatar != null)
            {
                animator.avatar = assetAnimator.avatar;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);

            foreach (var legacyAnimation in modelObject.GetComponentsInChildren<Animation>(true))
            {
                legacyAnimation.enabled = false;
                EditorUtility.SetDirty(legacyAnimation);
            }
        }

        private static AnimationClip EnsureDogWalkClip(Transform placementRoot)
        {
            EnsureUnityFolder(ConSpiritoArtRoot);
            EnsureUnityFolder(UnityAnimationFolder);

            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = RequireModelObject(reviewObject);
            var clip = CreateDogWalkClip(modelObject);
            clip.name = DogWalkClipName;
            clip.frameRate = 60f;
            ConfigureLoopSetting(clip, true);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(DogWalkClipAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(DogWalkClipAssetPath);
            }

            AssetDatabase.CreateAsset(clip, DogWalkClipAssetPath);
            var savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DogWalkClipAssetPath);
            if (savedClip == null)
            {
                throw new InvalidOperationException($"Could not create Con Spirito dog walk clip at {DogWalkClipAssetPath}.");
            }

            ConfigureLoopSetting(savedClip, true);
            EditorUtility.SetDirty(savedClip);
            AssetDatabase.SaveAssets();
            return savedClip;
        }

        private static AnimationClip EnsureIdleBreathClip(Transform modelObject)
        {
            EnsureUnityFolder(ConSpiritoArtRoot);
            EnsureUnityFolder(UnityAnimationFolder);

            var clip = CreateIdleBreathClip(modelObject);
            clip.name = IdleBreathClipName;
            clip.frameRate = 60f;
            ConfigureLoopSetting(clip, true);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathClipAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(IdleBreathClipAssetPath);
            }

            AssetDatabase.CreateAsset(clip, IdleBreathClipAssetPath);
            var savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathClipAssetPath);
            if (savedClip == null)
            {
                throw new InvalidOperationException($"Could not create Con Spirito idle breath clip at {IdleBreathClipAssetPath}.");
            }

            ConfigureLoopSetting(savedClip, true);
            EditorUtility.SetDirty(savedClip);
            AssetDatabase.SaveAssets();
            return savedClip;
        }

        private static AnimationClip EnsureChargeClip(AnimationClip sourceWalkClip, Transform modelObject)
        {
            EnsureUnityFolder(ConSpiritoArtRoot);
            EnsureUnityFolder(UnityAnimationFolder);

            var clip = CreateChargeClip(sourceWalkClip, modelObject);
            clip.name = ChargeClipName;
            clip.frameRate = 60f;
            ConfigureLoopSetting(clip, true);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ChargeClipAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(ChargeClipAssetPath);
            }

            AssetDatabase.CreateAsset(clip, ChargeClipAssetPath);
            var savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ChargeClipAssetPath);
            if (savedClip == null)
            {
                throw new InvalidOperationException($"Could not create Con Spirito charge clip at {ChargeClipAssetPath}.");
            }

            ConfigureLoopSetting(savedClip, true);
            EditorUtility.SetDirty(savedClip);
            AssetDatabase.SaveAssets();
            return savedClip;
        }

        private static AnimationClip CreateChargeClip(AnimationClip sourceWalkClip, Transform modelObject)
        {
            if (sourceWalkClip == null)
            {
                throw new InvalidOperationException("Con Spirito source walk clip is missing for charge clip generation.");
            }

            var clip = new AnimationClip();
            AddScaledSourceWalkCurves(clip, sourceWalkClip);
            AddChargeRunRootCurves(clip, modelObject);
            AddChargeRunLegChainCurves(clip, modelObject, ChargeRunLegChains[2], 0.00f, 1f, false);
            AddChargeRunLegChainCurves(clip, modelObject, ChargeRunLegChains[3], 0.12f, -1f, false);
            AddChargeRunLegChainCurves(clip, modelObject, ChargeRunLegChains[0], 0.54f, 1f, true);
            AddChargeRunLegChainCurves(clip, modelObject, ChargeRunLegChains[1], 0.68f, -1f, true);
            AddChargeForwardPoseCurves(clip, modelObject, ChargeRunLoopDurationSeconds);
            return clip;
        }

        private static void AddScaledSourceWalkCurves(AnimationClip targetClip, AnimationClip sourceWalkClip)
        {
            if (sourceWalkClip.length <= 0f)
            {
                throw new InvalidOperationException("Con Spirito source walk clip length must be positive.");
            }

            var speedMultiplier = sourceWalkClip.length / ChargeRunLoopDurationSeconds;
            foreach (var binding in AnimationUtility.GetCurveBindings(sourceWalkClip))
            {
                if (ShouldSkipChargeSourceBinding(binding.path))
                {
                    continue;
                }

                var sourceCurve = AnimationUtility.GetEditorCurve(sourceWalkClip, binding);
                if (sourceCurve == null || sourceCurve.length == 0)
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(targetClip, binding, ScaleAnimationCurveTime(sourceCurve, speedMultiplier));
            }
        }

        private static bool ShouldSkipChargeSourceBinding(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return IsChargeLegBindingPath(path) ||
                string.Equals(path, "Armature/Hips", StringComparison.Ordinal) ||
                string.Equals(path, "Armature/Hips/chest", StringComparison.Ordinal) ||
                string.Equals(path, "Armature/Hips/chest/head", StringComparison.Ordinal) ||
                path.EndsWith("/Hips", StringComparison.Ordinal) ||
                path.EndsWith("/chest", StringComparison.Ordinal) ||
                path.EndsWith("/head", StringComparison.Ordinal);
        }

        private static bool IsChargeLegBindingPath(string path)
        {
            foreach (var legChain in ChargeRunLegChains)
            {
                foreach (var boneName in legChain)
                {
                    if (string.Equals(path, boneName, StringComparison.Ordinal) ||
                        path.EndsWith("/" + boneName, StringComparison.Ordinal) ||
                        path.Contains("/" + boneName + "/", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AddChargeRunRootCurves(AnimationClip clip, Transform modelObject)
        {
            var baseY = modelObject.localPosition.y;
            SetTransformCurve(
                clip,
                string.Empty,
                "localPosition.y",
                Key(0.00f, baseY),
                Key(ChargeRunLoopDurationSeconds * 0.10f, baseY + ChargeRunBodyBobMeters * 0.72f),
                Key(ChargeRunLoopDurationSeconds * 0.22f, baseY + ChargeRunBodyBobMeters * 0.20f),
                Key(ChargeRunLoopDurationSeconds * 0.34f, baseY - ChargeRunBodyBobMeters * 0.34f),
                Key(ChargeRunLoopDurationSeconds * 0.50f, baseY + ChargeRunBodyBobMeters * 0.82f),
                Key(ChargeRunLoopDurationSeconds * 0.62f, baseY + ChargeRunBodyBobMeters * 0.24f),
                Key(ChargeRunLoopDurationSeconds * 0.78f, baseY - ChargeRunBodyBobMeters * 0.30f),
                Key(ChargeRunLoopDurationSeconds * 0.90f, baseY + ChargeRunBodyBobMeters * 0.10f),
                Key(ChargeRunLoopDurationSeconds, baseY));
            SetTransformCurve(
                clip,
                string.Empty,
                "localEulerAnglesRaw.x",
                Key(0.00f, ChargeRunBodyForwardPitchDegrees),
                Key(ChargeRunLoopDurationSeconds * 0.12f, ChargeRunBodyForwardPitchDegrees - 2.0f),
                Key(ChargeRunLoopDurationSeconds * 0.28f, ChargeRunBodyForwardPitchDegrees + 3.6f),
                Key(ChargeRunLoopDurationSeconds * 0.42f, ChargeRunBodyForwardPitchDegrees + 1.2f),
                Key(ChargeRunLoopDurationSeconds * 0.58f, ChargeRunBodyForwardPitchDegrees - 1.8f),
                Key(ChargeRunLoopDurationSeconds * 0.74f, ChargeRunBodyForwardPitchDegrees + 3.8f),
                Key(ChargeRunLoopDurationSeconds * 0.88f, ChargeRunBodyForwardPitchDegrees + 0.8f),
                Key(ChargeRunLoopDurationSeconds, ChargeRunBodyForwardPitchDegrees));
            SetTransformCurve(
                clip,
                string.Empty,
                "localEulerAnglesRaw.z",
                Key(0.00f, ChargeRunBodyRollDegrees),
                Key(ChargeRunLoopDurationSeconds * 0.16f, -ChargeRunBodyRollDegrees * 0.40f),
                Key(ChargeRunLoopDurationSeconds * 0.34f, -ChargeRunBodyRollDegrees),
                Key(ChargeRunLoopDurationSeconds * 0.52f, ChargeRunBodyRollDegrees * 0.55f),
                Key(ChargeRunLoopDurationSeconds * 0.72f, ChargeRunBodyRollDegrees),
                Key(ChargeRunLoopDurationSeconds * 0.88f, -ChargeRunBodyRollDegrees * 0.35f),
                Key(ChargeRunLoopDurationSeconds, ChargeRunBodyRollDegrees));
        }

        private static void AddChargeRunLegChainCurves(
            AnimationClip clip,
            Transform modelRoot,
            string[] boneNames,
            float phaseOffset,
            float sideSign,
            bool frontLeg)
        {
            if (boneNames == null || boneNames.Length == 0)
            {
                throw new InvalidOperationException("Con Spirito charge run leg chain is empty.");
            }

            var rootBone = FindChildByName(modelRoot, boneNames[0]);
            if (rootBone == null)
            {
                throw new InvalidOperationException($"Con Spirito charge run target bone is missing: {boneNames[0]}.");
            }

            var sampleCount = 32;
            var times = new float[sampleCount + 1];
            var rootOffsets = new Vector3[sampleCount + 1];
            var upperOffsets = new Vector3[sampleCount + 1];
            var lowerOffsets = new Vector3[sampleCount + 1];
            var toeOffsets = new Vector3[sampleCount + 1];
            for (var index = 0; index <= sampleCount; index++)
            {
                var fraction = index / (float)sampleCount;
                var phase = Mathf.Repeat(fraction + phaseOffset, 1f);
                var contact = SmoothChargeRunPulse(phase, 0.06f, 0.12f);
                var push = SmoothChargeRunPulse(phase, 0.20f, 0.18f);
                var tuck = SmoothChargeRunPulse(phase, 0.42f, 0.18f);
                var reach = SmoothChargeRunPulse(phase, 0.66f, 0.20f);
                var suspension = SmoothChargeRunPulse(phase, 0.54f, 0.15f);
                var lift = Mathf.Clamp01(tuck * 0.78f + reach * 0.38f + suspension * 0.24f);
                var rootSwing = frontLeg
                    ? ((-reach * 0.52f) + (push * 0.34f) + (tuck * 0.12f) - (contact * 0.04f)) * ChargeRunLegSwingDegrees
                    : ((reach * 0.34f) - (push * 0.52f) + (tuck * 0.14f) + (contact * 0.04f)) * ChargeRunLegSwingDegrees;
                var rootLift = sideSign * ((lift * ChargeRunLegLiftDegrees * 0.14f) - (contact * ChargeRunLegLiftDegrees * 0.04f));
                var upperFold = frontLeg
                    ? ((-reach * 0.10f) + (push * 0.10f) + (tuck * 0.30f) - (contact * 0.04f)) * ChargeRunLegSwingDegrees
                    : ((reach * 0.16f) - (push * 0.20f) - (tuck * 0.26f) + (contact * 0.04f)) * ChargeRunLegSwingDegrees;
                var lowerFold = frontLeg
                    ? ((reach * 0.08f) - (push * 0.08f) + (tuck * 0.38f) - (contact * 0.04f)) * ChargeRunLegSwingDegrees
                    : ((-reach * 0.10f) + (push * 0.12f) - (tuck * 0.44f) + (contact * 0.05f)) * ChargeRunLegSwingDegrees;
                var toeFlick = frontLeg
                    ? ((lift * 0.18f) - (contact * 0.08f) + (push * 0.04f)) * ChargeRunLegSwingDegrees
                    : ((-lift * 0.18f) + (contact * 0.08f) - (push * 0.04f)) * ChargeRunLegSwingDegrees;

                times[index] = ChargeRunLoopDurationSeconds * fraction;
                rootOffsets[index] = new Vector3(
                    rootSwing,
                    sideSign * lift * ChargeRunLegSplayDegrees * 0.12f,
                    rootLift);
                upperOffsets[index] = new Vector3(
                    upperFold,
                    sideSign * lift * ChargeRunLegSplayDegrees * 0.08f,
                    sideSign * upperFold * 0.03f);
                lowerOffsets[index] = new Vector3(
                    lowerFold,
                    0f,
                    sideSign * lowerFold * 0.02f);
                toeOffsets[index] = new Vector3(
                    toeFlick,
                    0f,
                    sideSign * toeFlick * 0.02f);
            }

            SetLocalRotationOffsetCurves(clip, modelRoot, rootBone, times, rootOffsets);
            AddOptionalChargeRunLegJointCurves(clip, modelRoot, boneNames, 1, times, upperOffsets);
            AddOptionalChargeRunLegJointCurves(clip, modelRoot, boneNames, 2, times, lowerOffsets);
            AddOptionalChargeRunLegJointCurves(clip, modelRoot, boneNames, 3, times, toeOffsets);
        }

        private static float SmoothChargeRunPulse(float phase, float center, float halfWidth)
        {
            var distance = Mathf.Abs(Mathf.DeltaAngle(phase * 360f, center * 360f)) / 360f;
            var normalized = Mathf.Clamp01(1f - (distance / Mathf.Max(halfWidth, 0.0001f)));
            return Mathf.SmoothStep(0f, 1f, normalized);
        }

        private static void AddOptionalChargeRunLegJointCurves(
            AnimationClip clip,
            Transform modelRoot,
            string[] boneNames,
            int index,
            float[] times,
            Vector3[] rotationOffsets)
        {
            if (index >= boneNames.Length)
            {
                return;
            }

            var bone = FindChildByName(modelRoot, boneNames[index]);
            if (bone == null)
            {
                return;
            }

            SetLocalRotationOffsetCurves(clip, modelRoot, bone, times, rotationOffsets);
        }

        private static AnimationCurve ScaleAnimationCurveTime(AnimationCurve sourceCurve, float speedMultiplier)
        {
            var keys = new Keyframe[sourceCurve.length];
            for (var index = 0; index < sourceCurve.length; index++)
            {
                var sourceKey = sourceCurve.keys[index];
                var key = new Keyframe(
                    sourceKey.time / speedMultiplier,
                    sourceKey.value,
                    sourceKey.inTangent * speedMultiplier,
                    sourceKey.outTangent * speedMultiplier)
                {
                    inWeight = sourceKey.inWeight,
                    outWeight = sourceKey.outWeight,
                    weightedMode = sourceKey.weightedMode
                };
                keys[index] = key;
            }

            return new AnimationCurve(keys)
            {
                preWrapMode = sourceCurve.preWrapMode,
                postWrapMode = WrapMode.Loop
            };
        }

        private static void AddChargeForwardPoseCurves(AnimationClip clip, Transform modelObject, float duration)
        {
            var head = FindFirstChildByName(modelObject, "head", "Head");
            var neck = FindFirstChildByName(modelObject, "neck", "Neck");
            var times = BuildChargeRunSupportTimes(duration);
            if (head != null)
            {
                AddChargeSupportPoseCurves(
                    clip,
                    modelObject,
                    head,
                    times,
                    new[]
                    {
                        ChargeHeadForwardMeters * 1.00f,
                        ChargeHeadForwardMeters * 1.12f,
                        ChargeHeadForwardMeters * 1.06f,
                        ChargeHeadForwardMeters * 0.96f,
                        ChargeHeadForwardMeters * 1.04f,
                        ChargeHeadForwardMeters * 1.13f,
                        ChargeHeadForwardMeters * 1.05f,
                        ChargeHeadForwardMeters * 0.97f,
                        ChargeHeadForwardMeters * 1.00f
                    },
                    new[]
                    {
                        -ChargeHeadDownMeters * 0.65f,
                        -ChargeHeadDownMeters * 0.95f,
                        -ChargeHeadDownMeters * 0.72f,
                        -ChargeHeadDownMeters * 0.25f,
                        -ChargeHeadDownMeters * 0.70f,
                        -ChargeHeadDownMeters * 1.00f,
                        -ChargeHeadDownMeters * 0.68f,
                        -ChargeHeadDownMeters * 0.22f,
                        -ChargeHeadDownMeters * 0.65f
                    },
                    new[] { 14.0f, 19.0f, 16.0f, 11.0f, 15.5f, 20.0f, 15.0f, 10.5f, 14.0f },
                    new[] { -1.2f, 1.4f, -1.0f, 0.6f, 1.0f, -1.5f, 1.2f, -0.6f, -1.2f });
            }

            if (neck != null)
            {
                AddChargeSupportPoseCurves(
                    clip,
                    modelObject,
                    neck,
                    times,
                    new[] { 0.006f, 0.010f, 0.008f, 0.004f, 0.008f, 0.012f, 0.007f, 0.003f, 0.006f },
                    new[] { -0.002f, -0.006f, -0.004f, 0.000f, -0.004f, -0.007f, -0.003f, 0.000f, -0.002f },
                    new[] { 7.0f, 12.0f, 8.5f, 3.0f, 8.5f, 12.5f, 8.0f, 2.5f, 7.0f },
                    new[] { -0.8f, 1.0f, -0.6f, 0.5f, 0.8f, -1.0f, 0.6f, -0.5f, -0.8f });
            }
        }

        private static float[] BuildChargeRunSupportTimes(float duration)
        {
            return new[]
            {
                0f,
                duration * 0.10f,
                duration * 0.22f,
                duration * 0.34f,
                duration * 0.50f,
                duration * 0.62f,
                duration * 0.75f,
                duration * 0.88f,
                duration
            };
        }

        private static void AddChargeSupportPoseCurves(
            AnimationClip clip,
            Transform modelObject,
            Transform target,
            float[] times,
            float[] zOffsets,
            float[] yOffsets,
            float[] pitchAngles,
            float[] rollAngles)
        {
            var path = AnimationUtility.CalculateTransformPath(target, modelObject);
            SetTransformCurve(clip, path, "localPosition.z", BuildChargeOffsetKeys(times, target.localPosition.z, zOffsets));
            SetTransformCurve(clip, path, "localPosition.y", BuildChargeOffsetKeys(times, target.localPosition.y, yOffsets));
            SetLocalRotationOffsetCurves(clip, modelObject, target, times, BuildChargeEulerOffsets(times, pitchAngles, rollAngles));
        }

        private static Keyframe[] BuildChargeOffsetKeys(float[] times, float baseValue, float[] offsets)
        {
            if (times.Length != offsets.Length)
            {
                throw new InvalidOperationException("Con Spirito charge support key count mismatch.");
            }

            var keys = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keys[index] = Key(times[index], baseValue + offsets[index]);
            }

            return keys;
        }

        private static Keyframe[] BuildChargeValueKeys(float[] times, float[] values)
        {
            if (times.Length != values.Length)
            {
                throw new InvalidOperationException("Con Spirito charge support value key count mismatch.");
            }

            var keys = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keys[index] = Key(times[index], values[index]);
            }

            return keys;
        }

        private static Vector3[] BuildChargeEulerOffsets(float[] times, float[] pitchAngles, float[] rollAngles)
        {
            if (times.Length != pitchAngles.Length || times.Length != rollAngles.Length)
            {
                throw new InvalidOperationException("Con Spirito charge support rotation key count mismatch.");
            }

            var offsets = new Vector3[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                offsets[index] = new Vector3(pitchAngles[index], 0f, rollAngles[index]);
            }

            return offsets;
        }

        private static AnimationClip CreateIdleBreathClip(Transform modelObject)
        {
            var clip = new AnimationClip();
            var breathTargets = SelectIdleBreathTargets(modelObject);
            for (var index = 0; index < breathTargets.Count; index++)
            {
                AddIdleBreathTargetCurves(clip, modelObject, breathTargets[index]);
            }

            AddIdleBreathLegCounterCurves(clip, modelObject, breathTargets[0]);
            return clip;
        }

        private static void AddIdleBreathTargetCurves(AnimationClip clip, Transform modelObject, IdleBreathTarget breathTarget)
        {
            var target = breathTarget.Transform;
            var weight = breathTarget.Weight;
            var path = AnimationUtility.CalculateTransformPath(target, modelObject);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Con Spirito idle breath must not animate the model root scale.");
            }

            var baseScale = target.localScale;
            var inhaleScale = new Vector3(
                baseScale.x * (1f + IdleBreathWidthExpansion * weight),
                baseScale.y * (1f + IdleBreathHeightExpansion * weight),
                baseScale.z * (1f + IdleBreathLengthExpansion * weight));
            var heldInhaleScale = new Vector3(
                Mathf.Lerp(baseScale.x, inhaleScale.x, 0.82f),
                Mathf.Lerp(baseScale.y, inhaleScale.y, 0.82f),
                Mathf.Lerp(baseScale.z, inhaleScale.z, 0.82f));
            var exhaleScale = new Vector3(
                baseScale.x * (1f - IdleBreathExhaleCompression * weight),
                baseScale.y * (1f - IdleBreathExhaleCompression * 0.25f * weight),
                baseScale.z * (1f - IdleBreathExhaleCompression * weight));

            SetTransformCurve(
                clip,
                path,
                "localScale.x",
                Key(0.00f, baseScale.x),
                Key(0.82f, inhaleScale.x),
                Key(1.18f, heldInhaleScale.x),
                Key(2.10f, exhaleScale.x),
                Key(IdleBreathLoopDurationSeconds, baseScale.x));
            SetTransformCurve(
                clip,
                path,
                "localScale.y",
                Key(0.00f, baseScale.y),
                Key(0.82f, inhaleScale.y),
                Key(1.18f, heldInhaleScale.y),
                Key(2.10f, exhaleScale.y),
                Key(IdleBreathLoopDurationSeconds, baseScale.y));
            SetTransformCurve(
                clip,
                path,
                "localScale.z",
                Key(0.00f, baseScale.z),
                Key(0.82f, inhaleScale.z),
                Key(1.18f, heldInhaleScale.z),
                Key(2.10f, exhaleScale.z),
                Key(IdleBreathLoopDurationSeconds, baseScale.z));
            SetTransformCurve(
                clip,
                path,
                "localPosition.y",
                Key(0.00f, target.localPosition.y),
                Key(0.82f, target.localPosition.y + IdleBreathBodyLiftMeters * weight),
                Key(1.18f, target.localPosition.y + IdleBreathBodyLiftMeters * 0.70f * weight),
                Key(2.10f, target.localPosition.y - IdleBreathBodyLiftMeters * 0.18f * weight),
                Key(IdleBreathLoopDurationSeconds, target.localPosition.y));
        }

        private static void AddIdleBreathLegCounterCurves(AnimationClip clip, Transform modelObject, IdleBreathTarget breathTarget)
        {
            var target = breathTarget.Transform;
            var targetBaseScale = target.localScale;
            var targetScales = BuildIdleBreathScaleSamples(targetBaseScale, breathTarget.Weight);

            foreach (var legBoneName in RequiredLegBoneNames)
            {
                var legRoot = FindChildByName(modelObject, legBoneName);
                if (legRoot == null)
                {
                    throw new InvalidOperationException($"Con Spirito idle breath leg counter target is missing: {legBoneName}.");
                }

                if (!legRoot.IsChildOf(target))
                {
                    continue;
                }

                AddIdleBreathLegCounterCurves(clip, modelObject, legRoot, targetBaseScale, targetScales);
            }
        }

        private static void AddIdleBreathLegCounterCurves(
            AnimationClip clip,
            Transform modelObject,
            Transform legRoot,
            Vector3 targetBaseScale,
            Vector3[] targetScales)
        {
            var path = AnimationUtility.CalculateTransformPath(legRoot, modelObject);
            var legScale = legRoot.localScale;
            var legPosition = legRoot.localPosition;
            SetTransformCurve(
                clip,
                path,
                "localScale.x",
                BuildLegCounterKeys(legScale.x, targetBaseScale.x, targetScales, scale => scale.x));
            SetTransformCurve(
                clip,
                path,
                "localScale.y",
                BuildLegCounterKeys(legScale.y, targetBaseScale.y, targetScales, scale => scale.y));
            SetTransformCurve(
                clip,
                path,
                "localScale.z",
                BuildLegCounterKeys(legScale.z, targetBaseScale.z, targetScales, scale => scale.z));
            SetTransformCurve(
                clip,
                path,
                "localPosition.x",
                BuildLegCounterKeys(legPosition.x, targetBaseScale.x, targetScales, scale => scale.x));
            SetTransformCurve(
                clip,
                path,
                "localPosition.y",
                BuildLegCounterKeys(legPosition.y, targetBaseScale.y, targetScales, scale => scale.y));
            SetTransformCurve(
                clip,
                path,
                "localPosition.z",
                BuildLegCounterKeys(legPosition.z, targetBaseScale.z, targetScales, scale => scale.z));
        }

        private static Vector3[] BuildIdleBreathScaleSamples(Vector3 baseScale, float weight)
        {
            var inhaleScale = new Vector3(
                baseScale.x * (1f + IdleBreathWidthExpansion * weight),
                baseScale.y * (1f + IdleBreathHeightExpansion * weight),
                baseScale.z * (1f + IdleBreathLengthExpansion * weight));
            var heldInhaleScale = new Vector3(
                Mathf.Lerp(baseScale.x, inhaleScale.x, 0.82f),
                Mathf.Lerp(baseScale.y, inhaleScale.y, 0.82f),
                Mathf.Lerp(baseScale.z, inhaleScale.z, 0.82f));
            var exhaleScale = new Vector3(
                baseScale.x * (1f - IdleBreathExhaleCompression * weight),
                baseScale.y * (1f - IdleBreathExhaleCompression * 0.25f * weight),
                baseScale.z * (1f - IdleBreathExhaleCompression * weight));
            return new[]
            {
                baseScale,
                inhaleScale,
                heldInhaleScale,
                exhaleScale,
                baseScale
            };
        }

        private static Keyframe[] BuildLegCounterKeys(
            float baseValue,
            float targetBaseScale,
            Vector3[] targetScales,
            Func<Vector3, float> scaleSelector)
        {
            var times = BuildIdleBreathKeyTimes();
            var keys = new Keyframe[targetScales.Length];
            for (var index = 0; index < targetScales.Length; index++)
            {
                var targetScale = scaleSelector(targetScales[index]);
                var scaleRatio = Mathf.Abs(targetScale) > 0.0001f ? targetBaseScale / targetScale : 1f;
                keys[index] = Key(times[index], baseValue * scaleRatio);
            }

            return keys;
        }

        private static float[] BuildIdleBreathKeyTimes()
        {
            return new[] { 0.00f, 0.82f, 1.18f, 2.10f, IdleBreathLoopDurationSeconds };
        }

        private static List<IdleBreathTarget> SelectIdleBreathTargets(Transform modelObject)
        {
            var candidates = new List<IdleBreathTarget>();
            var seenTransforms = new HashSet<Transform>();
            foreach (var transform in modelObject.GetComponentsInChildren<Transform>(true))
            {
                if (transform == modelObject)
                {
                    continue;
                }

                var score = ScoreIdleBreathTargetName(transform.name);
                if (score <= 0 || !seenTransforms.Add(transform))
                {
                    continue;
                }

                candidates.Add(new IdleBreathTarget(transform, score, 1f));
            }

            if (candidates.Count == 0)
            {
                AddCentralIdleBreathCandidates(modelObject, candidates, seenTransforms);
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("Could not find a Con Spirito torso transform for idle breathing.");
            }

            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
            var targetCount = Mathf.Min(candidates.Count, IdleBreathTargetLimit);
            var selectedTargets = new List<IdleBreathTarget>(targetCount);
            var minimumSelectedScore = candidates[0].Score >= 100 ? 100 : 1;
            for (var index = 0; index < candidates.Count && selectedTargets.Count < targetCount; index++)
            {
                if (candidates[index].Score < minimumSelectedScore)
                {
                    continue;
                }

                var weight = selectedTargets.Count == 0 ? 1f : 0.58f;
                selectedTargets.Add(candidates[index].WithWeight(weight));
            }

            return selectedTargets;
        }

        private static void AddCentralIdleBreathCandidates(
            Transform modelObject,
            List<IdleBreathTarget> candidates,
            HashSet<Transform> seenTransforms)
        {
            var bounds = CalculateMeshDataWorldBounds(modelObject, new Bounds(modelObject.position, Vector3.one));
            foreach (var skinnedRenderer in modelObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (var bone in skinnedRenderer.bones)
                {
                    if (bone == null ||
                        bone == modelObject ||
                        !seenTransforms.Add(bone) ||
                        IsRootLikeIdleBreathTargetName(bone.name) ||
                        IsExcludedIdleBreathTargetName(bone.name))
                    {
                        continue;
                    }

                    var normalizedDistance = CalculateNormalizedBoundsDistance(bounds, bone.position);
                    if (normalizedDistance > 0.86f)
                    {
                        continue;
                    }

                    var score = Mathf.RoundToInt((0.86f - normalizedDistance) * 100f);
                    if (score > 0)
                    {
                        candidates.Add(new IdleBreathTarget(bone, score, 1f));
                    }
                }
            }
        }

        private static int ScoreIdleBreathTargetName(string transformName)
        {
            var lowerName = transformName.ToLowerInvariant();
            if (IsExcludedIdleBreathTargetName(lowerName) ||
                IsRootLikeIdleBreathTargetName(lowerName))
            {
                return 0;
            }

            var score = 0;
            if (lowerName.Contains("chest") || lowerName.Contains("rib"))
            {
                score += 140;
            }

            if (lowerName.Contains("spine") || lowerName.Contains("torso"))
            {
                score += 120;
            }

            if (lowerName.Contains("body") || lowerName.Contains("abdomen") || lowerName.Contains("belly"))
            {
                score += 110;
            }

            if (lowerName.Contains("pelvis") || lowerName.Contains("hip"))
            {
                score += 55;
            }

            return score;
        }

        private static bool IsRootLikeIdleBreathTargetName(string transformName)
        {
            var lowerName = transformName.ToLowerInvariant();
            return lowerName == "armature" ||
                   lowerName.Contains("root");
        }

        private static bool IsExcludedIdleBreathTargetName(string transformName)
        {
            var lowerName = transformName.ToLowerInvariant();
            return lowerName.Contains("leg") ||
                   lowerName.Contains("foot") ||
                   lowerName.Contains("toe") ||
                   lowerName.Contains("head") ||
                   lowerName.Contains("neck") ||
                   lowerName.Contains("ear") ||
                   lowerName.Contains("tail") ||
                   lowerName.Contains("jaw") ||
                   lowerName.Contains("mouth") ||
                   lowerName.Contains("nose") ||
                   lowerName.Contains("muzzle") ||
                   lowerName.Contains("eye") ||
                   lowerName.Contains("tongue") ||
                   lowerName.Contains("teeth");
        }

        private static bool HasExcludedIdleBreathDescendant(Transform transform)
        {
            foreach (var child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child == transform)
                {
                    continue;
                }

                if (IsExcludedIdleBreathTargetName(child.name))
                {
                    return true;
                }
            }

            return false;
        }

        private static float CalculateNormalizedBoundsDistance(Bounds bounds, Vector3 position)
        {
            var extents = bounds.extents;
            var normalizedX = Mathf.Abs(position.x - bounds.center.x) / Mathf.Max(extents.x, 0.001f);
            var normalizedY = Mathf.Abs(position.y - bounds.center.y) / Mathf.Max(extents.y, 0.001f);
            var normalizedZ = Mathf.Abs(position.z - bounds.center.z) / Mathf.Max(extents.z, 0.001f);
            return Mathf.Max(normalizedX, Mathf.Max(normalizedY, normalizedZ));
        }

        private static AnimationClip CreateDogWalkClip(Transform modelRoot)
        {
            var clip = new AnimationClip();
            SetTransformCurve(
                clip,
                string.Empty,
                "localPosition.y",
                Key(0.00f, modelRoot.localPosition.y),
                Key(0.15f, modelRoot.localPosition.y + DogWalkBodyBobMeters),
                Key(0.30f, modelRoot.localPosition.y),
                Key(0.45f, modelRoot.localPosition.y - DogWalkBodyBobMeters * 0.45f),
                Key(0.60f, modelRoot.localPosition.y),
                Key(0.75f, modelRoot.localPosition.y + DogWalkBodyBobMeters),
                Key(0.90f, modelRoot.localPosition.y),
                Key(1.05f, modelRoot.localPosition.y - DogWalkBodyBobMeters * 0.45f),
                Key(DogWalkLoopDurationSeconds, modelRoot.localPosition.y));
            SetTransformCurve(
                clip,
                string.Empty,
                "localEulerAnglesRaw.x",
                Key(0.00f, 0f),
                Key(0.30f, DogWalkBodyPitchDegrees),
                Key(0.60f, 0f),
                Key(0.90f, -DogWalkBodyPitchDegrees),
                Key(DogWalkLoopDurationSeconds, 0f));
            SetTransformCurve(
                clip,
                string.Empty,
                "localEulerAnglesRaw.z",
                Key(0.00f, DogWalkBodyRollDegrees),
                Key(0.30f, 0f),
                Key(0.60f, -DogWalkBodyRollDegrees),
                Key(0.90f, 0f),
                Key(DogWalkLoopDurationSeconds, DogWalkBodyRollDegrees));

            AddDogWalkLegCurves(clip, modelRoot, "frontleg", 0f, 1f, true);
            AddDogWalkLegCurves(clip, modelRoot, "R_backleg", 0f, -1f, false);
            AddDogWalkLegCurves(clip, modelRoot, "R_frontleg", 0.5f, -1f, true);
            AddDogWalkLegCurves(clip, modelRoot, "backleg", 0.5f, 1f, false);
            return clip;
        }

        private static void AddDogWalkLegCurves(
            AnimationClip clip,
            Transform modelRoot,
            string boneName,
            float phaseOffset,
            float sideSign,
            bool frontLeg)
        {
            var bone = FindChildByName(modelRoot, boneName);
            if (bone == null)
            {
                throw new InvalidOperationException($"Con Spirito dog walk target bone is missing: {boneName}.");
            }

            var sampleCount = 12;
            var times = new float[sampleCount + 1];
            var rotationOffsets = new Vector3[sampleCount + 1];
            var liftOffsets = new float[sampleCount + 1];
            var strideOffsets = new float[sampleCount + 1];
            for (var index = 0; index <= sampleCount; index++)
            {
                var fraction = index / (float)sampleCount;
                var phase = Mathf.Repeat(fraction + phaseOffset, 1f);
                var stride = Mathf.Sin(phase * Mathf.PI * 2f);
                var lift = Mathf.Max(0f, Mathf.Sin((phase - 0.08f) * Mathf.PI * 2f));
                var plant = Mathf.Max(0f, -Mathf.Sin((phase - 0.08f) * Mathf.PI * 2f));
                var frontSwingSign = frontLeg ? -1f : 1f;

                times[index] = DogWalkLoopDurationSeconds * fraction;
                rotationOffsets[index] = new Vector3(
                    frontSwingSign * stride * DogWalkLegSwingDegrees,
                    sideSign * lift * DogWalkLegSplayDegrees,
                    sideSign * lift * DogWalkLegLiftDegrees);
                liftOffsets[index] = lift * DogWalkLegLiftMeters - plant * DogWalkLegLiftMeters * 0.18f;
                strideOffsets[index] = frontSwingSign * stride * DogWalkLegStrideMeters;
            }

            SetLocalRotationOffsetCurves(clip, modelRoot, bone, times, rotationOffsets);
            SetLocalPositionOffsetCurve(clip, modelRoot, bone, "localPosition.y", bone.localPosition.y, times, liftOffsets);
            SetLocalPositionOffsetCurve(clip, modelRoot, bone, "localPosition.z", bone.localPosition.z, times, strideOffsets);
        }

        private static AnimatorController EnsureDogWalkController(AnimationClip dogWalkClip)
        {
            EnsureUnityFolder(ConSpiritoArtRoot);
            EnsureUnityFolder(UnityControllerFolder);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DogWalkControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(DogWalkControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states)
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(DogWalkClipName);
            state.motion = dogWalkClip;
            state.writeDefaultValues = true;
            state.speed = 1f;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ApplyAnimatorController(Transform placementRoot, AnimatorController controller)
        {
            ApplyAnimatorController(placementRoot, ModelChildName, LoadReriggedModelAsset(), controller);
        }

        private static void ApplyAnimatorController(
            Transform placementRoot,
            string modelChildName,
            GameObject modelAsset,
            AnimatorController controller)
        {
            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = RequireModelObject(reviewObject, modelChildName);
            ConfigureAnimatorOnModel(modelObject, modelAsset, controller);
        }

        private static void ConfigureAnimatorOnModel(
            Transform modelObject,
            GameObject modelAsset,
            AnimatorController controller)
        {
            var animator = modelObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = modelObject.gameObject.AddComponent<Animator>();
            }

            var assetAnimator = modelAsset.GetComponent<Animator>();
            if (assetAnimator != null && assetAnimator.avatar != null)
            {
                animator.avatar = assetAnimator.avatar;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);

            foreach (var legacyAnimation in modelObject.GetComponentsInChildren<Animation>(true))
            {
                legacyAnimation.enabled = false;
                EditorUtility.SetDirty(legacyAnimation);
            }
        }

        private static void ConfigureLoopSetting(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Default;
        }

        private static GameObject PlaceReriggedModel(GameObject modelAsset, Scene scene)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var cantabileRoot = RequireSceneRoot(CantabilePlacementRootName);
            var spacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            var placementPosition = new Vector3(
                cantabileRoot.transform.position.x,
                cantabileRoot.transform.position.y,
                cantabileRoot.transform.position.z - spacing);

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
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, ConSpiritoFacingYawDegrees, 0f);
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
            InspectReriggedLegBones(reviewRoot.transform);

            EditorUtility.SetDirty(placementRoot);
            EditorUtility.SetDirty(reviewRoot);
            return placementRoot;
        }

        private static GameObject PlaceOriginalModel(GameObject modelAsset, Scene scene)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var cantabileRoot = RequireSceneRoot(CantabilePlacementRootName);
            var spacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            var placementPosition = new Vector3(
                cantabileRoot.transform.position.x,
                cantabileRoot.transform.position.y,
                cantabileRoot.transform.position.z - spacing);

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
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, ConSpiritoFacingYawDegrees, 0f);
            reviewRoot.transform.localScale = Vector3.one;

            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = OriginalModelChildName;
            modelInstance.transform.SetParent(reviewRoot.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            DisableImportedAnimationPlayback(reviewRoot.transform);
            RequireRenderers(reviewRoot.transform);

            EditorUtility.SetDirty(placementRoot);
            EditorUtility.SetDirty(reviewRoot);
            return placementRoot;
        }

        private static GameObject EnsurePlacementRootAtConSpiritoPosition(Scene scene)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var cantabileRoot = RequireSceneRoot(CantabilePlacementRootName);
            var spacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            var placementPosition = new Vector3(
                cantabileRoot.transform.position.x,
                cantabileRoot.transform.position.y,
                cantabileRoot.transform.position.z - spacing);

            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                placementRoot = new GameObject(PlacementRootName);
                SceneManager.MoveGameObjectToScene(placementRoot, scene);
            }

            placementRoot.transform.position = placementPosition;
            placementRoot.transform.rotation = Quaternion.identity;
            placementRoot.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(placementRoot);
            return placementRoot;
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(root.GetChild(index).gameObject);
            }
        }

        private static GameObject CreateAnimationReviewSlot(Transform placementRoot, GameObject modelAsset, int slotIndex)
        {
            var slotRoot = new GameObject(AnimationReviewSlotNames[slotIndex]);
            slotRoot.transform.SetParent(placementRoot, false);
            slotRoot.transform.localPosition = CalculateAnimationReviewSlotLocalPosition(slotIndex);
            slotRoot.transform.localRotation = Quaternion.Euler(0f, ConSpiritoFacingYawDegrees, 0f);
            slotRoot.transform.localScale = Vector3.one;

            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = OriginalModelChildName;
            modelInstance.transform.SetParent(slotRoot.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            DisableImportedAnimationPlayback(slotRoot.transform);
            RequireRenderers(slotRoot.transform);

            EditorUtility.SetDirty(slotRoot);
            return slotRoot;
        }

        private static Vector3 CalculateAnimationReviewSlotLocalPosition(int slotIndex)
        {
            var centeredIndex = slotIndex - (AnimationReviewSlotNames.Length - 1) * 0.5f;
            return new Vector3(centeredIndex * AnimationReviewSlotSpacingMeters, 0f, 0f);
        }

        private static void ConfigureInitialPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = RequirePlacementObject(placementRoot);
            var bounds = CalculateMeshDataWorldBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.05f, 0.30f);
            var frontDirection = CalculateVisualFrontDirection(focus);
            var startPosition = new Vector3(
                lookAt.x + frontDirection.x * PlayerFrontDistance,
                0f,
                lookAt.z + frontDirection.z * PlayerFrontDistance);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
        }

        private static void InspectSceneState(Transform placementRoot)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var cantabileRoot = RequireSceneRoot(CantabilePlacementRootName);
            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = reviewObject.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {PlacementObjectName}.");
            }

            var rendererCount = RequireRenderers(reviewObject);
            InspectReriggedLegBones(reviewObject);
            InspectPlacementPosition(placementRoot, cantabileRoot.transform, longaRoot.transform, tergoRoot.transform);
            InspectPlayerStart(placementRoot);

            var spacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            var bounds = CalculateMeshDataWorldBounds(reviewObject, new Bounds(reviewObject.position, Vector3.one));
            Debug.Log(
                "ConSpiritoPlacementInspection " +
                $"Root={PlacementRootName}, Object={PlacementObjectName}, Model={ModelChildName}, " +
                $"UnityAsset={UnityModelAssetPath}, Renderers={rendererCount}, " +
                $"LongaZ={longaRoot.transform.position.z:0.###}, TergoZ={tergoRoot.transform.position.z:0.###}, " +
                $"LongaTergoSpacing={spacing:0.###}, CantabileZ={cantabileRoot.transform.position.z:0.###}, " +
                $"ConSpiritoZ={placementRoot.position.z:0.###}, BoundsCenter={FormatVector(bounds.center)}, " +
                $"BoundsSize={FormatVector(bounds.size)}, Player={FormatVector(FindPlayerStartTransform().position)}.");
        }

        private static void InspectAnimationReviewSlots(
            Transform placementRoot,
            AnimationClip walkClip,
            AnimatorController walkController)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var cantabileRoot = RequireSceneRoot(CantabilePlacementRootName);
            InspectPlacementPosition(placementRoot, cantabileRoot.transform, longaRoot.transform, tergoRoot.transform);

            if (placementRoot.Find(PlacementObjectName) != null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} should not remain after applying Con Spirito animation review slots.");
            }

            if (placementRoot.childCount != AnimationReviewSlotNames.Length)
            {
                throw new InvalidOperationException(
                    $"Con Spirito animation review slot count mismatch. Expected={AnimationReviewSlotNames.Length}, Actual={placementRoot.childCount}.");
            }

            if (walkClip == null)
            {
                throw new InvalidOperationException("Con Spirito walk review clip is missing.");
            }

            var walkClipSettings = AnimationUtility.GetAnimationClipSettings(walkClip);
            if (!walkClipSettings.loopTime)
            {
                throw new InvalidOperationException($"Con Spirito walk review clip must loop: {walkClip.name}.");
            }

            var totalRenderers = 0;
            for (var index = 0; index < AnimationReviewSlotNames.Length; index++)
            {
                var slotRoot = placementRoot.Find(AnimationReviewSlotNames[index]);
                if (slotRoot == null)
                {
                    throw new InvalidOperationException($"Con Spirito animation review slot is missing: {AnimationReviewSlotNames[index]}.");
                }

                var expectedPosition = CalculateAnimationReviewSlotLocalPosition(index);
                if (Vector3.Distance(slotRoot.localPosition, expectedPosition) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"Con Spirito animation review slot position mismatch: {slotRoot.name}, Expected={FormatVector(expectedPosition)}, Actual={FormatVector(slotRoot.localPosition)}.");
                }

                var modelObject = RequireModelObject(slotRoot, OriginalModelChildName);
                totalRenderers += RequireRenderers(slotRoot);
                if (index == AnimationReviewWalkingSlotIndex)
                {
                    InspectWalkingReviewSlot(modelObject, walkClip, walkController);
                }
                else
                {
                    InspectDisabledReviewSlot(slotRoot);
                }
            }

            Debug.Log(
                "ConSpiritoAnimationReviewSlotsInspection " +
                $"Slots={AnimationReviewSlotNames.Length}, SlotOrder={string.Join(",", AnimationReviewSlotLabels)}, " +
                $"WalkSlot={AnimationReviewSlotNames[AnimationReviewWalkingSlotIndex]}, WalkClip={walkClip.name}, " +
                $"WalkLoopTime={walkClipSettings.loopTime}, TotalRenderers={totalRenderers}.");
        }

        private static void InspectIdleBreathLoop(
            Transform placementRoot,
            AnimationClip idleClip,
            AnimatorController idleController,
            AnimationClip walkClip,
            AnimatorController walkController)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var cantabileRoot = RequireSceneRoot(CantabilePlacementRootName);
            InspectPlacementPosition(placementRoot, cantabileRoot.transform, longaRoot.transform, tergoRoot.transform);

            if (placementRoot.Find(PlacementObjectName) != null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} should not remain after applying Con Spirito animation review slots.");
            }

            if (placementRoot.childCount != AnimationReviewSlotNames.Length)
            {
                throw new InvalidOperationException(
                    $"Con Spirito animation review slot count mismatch. Expected={AnimationReviewSlotNames.Length}, Actual={placementRoot.childCount}.");
            }

            var idleClipSettings = AnimationUtility.GetAnimationClipSettings(idleClip);
            if (!idleClipSettings.loopTime)
            {
                throw new InvalidOperationException($"Con Spirito idle breath clip must loop: {idleClip.name}.");
            }

            var walkClipSettings = AnimationUtility.GetAnimationClipSettings(walkClip);
            if (!walkClipSettings.loopTime)
            {
                throw new InvalidOperationException($"Con Spirito walk review clip must loop: {walkClip.name}.");
            }

            var curveBindings = AnimationUtility.GetCurveBindings(idleClip);
            InspectIdleBreathCurveBindings(
                idleClip,
                curveBindings,
                out var scaleCurveCount,
                out var positionCurveCount,
                out var minX,
                out var maxX,
                out var minY,
                out var maxY,
                out var minZ,
                out var maxZ,
                out var targetPaths,
                out var legCounterScaleCurveCount,
                out var legCounterPositionCurveCount,
                out var legCounterTargets);

            var totalRenderers = 0;
            var disabledSlots = 0;
            for (var index = 0; index < AnimationReviewSlotNames.Length; index++)
            {
                var slotRoot = RequireAnimationReviewSlot(placementRoot, index);
                var expectedPosition = CalculateAnimationReviewSlotLocalPosition(index);
                if (Vector3.Distance(slotRoot.localPosition, expectedPosition) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"Con Spirito animation review slot position mismatch: {slotRoot.name}, Expected={FormatVector(expectedPosition)}, Actual={FormatVector(slotRoot.localPosition)}.");
                }

                var modelObject = RequireModelObject(slotRoot, OriginalModelChildName);
                totalRenderers += RequireRenderers(slotRoot);
                if (index == AnimationReviewIdleSlotIndex)
                {
                    InspectIdleBreathReviewSlot(modelObject, idleClip, idleController);
                }
                else if (index == AnimationReviewWalkingSlotIndex)
                {
                    InspectWalkingReviewSlot(modelObject, walkClip, walkController);
                }
                else
                {
                    InspectDisabledReviewSlot(slotRoot);
                    disabledSlots++;
                }
            }

            Debug.Log(
                "ConSpiritoIdleBreathLoopInspection " +
                $"Slots={AnimationReviewSlotNames.Length}, IdleSlot={AnimationReviewSlotNames[AnimationReviewIdleSlotIndex]}, " +
                $"IdleClip={idleClip.name}, IdleLength={idleClip.length:0.###}, IdleLoopTime={idleClipSettings.loopTime}, " +
                $"IdleCurveBindings={curveBindings.Length}, IdleScaleCurves={scaleCurveCount}, IdlePositionCurves={positionCurveCount}, " +
                $"LegCounterScaleCurves={legCounterScaleCurveCount}, LegCounterPositionCurves={legCounterPositionCurveCount}, " +
                $"IdleScaleX={minX:0.###}-{maxX:0.###}, " +
                $"IdleScaleY={minY:0.###}-{maxY:0.###}, IdleScaleZ={minZ:0.###}-{maxZ:0.###}, " +
                $"IdleTargets={targetPaths}, LegCounterTargets={legCounterTargets}, " +
                $"WalkSlot={AnimationReviewSlotNames[AnimationReviewWalkingSlotIndex]}, WalkClip={walkClip.name}, " +
                $"WalkLoopTime={walkClipSettings.loopTime}, DisabledSlots={disabledSlots}, TotalRenderers={totalRenderers}.");
        }

        private static void InspectChargeLoop(
            Transform placementRoot,
            AnimationClip chargeClip,
            AnimatorController chargeController,
            AnimationClip idleClip,
            AnimatorController idleController,
            AnimationClip walkClip,
            AnimatorController walkController)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var cantabileRoot = RequireSceneRoot(CantabilePlacementRootName);
            InspectPlacementPosition(placementRoot, cantabileRoot.transform, longaRoot.transform, tergoRoot.transform);

            if (placementRoot.Find(PlacementObjectName) != null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} should not remain after applying Con Spirito animation review slots.");
            }

            if (placementRoot.childCount != AnimationReviewSlotNames.Length)
            {
                throw new InvalidOperationException(
                    $"Con Spirito animation review slot count mismatch. Expected={AnimationReviewSlotNames.Length}, Actual={placementRoot.childCount}.");
            }

            var chargeClipSettings = AnimationUtility.GetAnimationClipSettings(chargeClip);
            if (!chargeClipSettings.loopTime)
            {
                throw new InvalidOperationException($"Con Spirito charge clip must loop: {chargeClip.name}.");
            }

            var walkClipSettings = AnimationUtility.GetAnimationClipSettings(walkClip);
            if (!walkClipSettings.loopTime)
            {
                throw new InvalidOperationException($"Con Spirito walk review clip must loop: {walkClip.name}.");
            }

            if (Mathf.Abs(chargeClip.length - ChargeRunLoopDurationSeconds) > 0.05f)
            {
                throw new InvalidOperationException(
                    $"Con Spirito charge run clip length mismatch. Expected={ChargeRunLoopDurationSeconds:0.###}, Actual={chargeClip.length:0.###}.");
            }

            var curveBindings = AnimationUtility.GetCurveBindings(chargeClip);
            if (curveBindings.Length == 0)
            {
                throw new InvalidOperationException("Con Spirito charge clip has no curve bindings.");
            }

            var animatedLegBones = 0;
            foreach (var requiredBoneName in RequiredLegBoneNames)
            {
                if (ClipContainsBoneBinding(curveBindings, requiredBoneName))
                {
                    animatedLegBones++;
                }
            }

            if (animatedLegBones < RequiredLegBoneNames.Length)
            {
                throw new InvalidOperationException(
                    $"Con Spirito charge clip does not preserve all walking leg bindings. Animated={animatedLegBones}, Required={RequiredLegBoneNames.Length}.");
            }

            var chargePoseTargets = ExtractChargePoseTargets(curveBindings);
            if (chargePoseTargets.Count == 0)
            {
                throw new InvalidOperationException("Con Spirito charge clip is missing forward head/body pose bindings.");
            }

            var totalRenderers = 0;
            var disabledSlots = 0;
            for (var index = 0; index < AnimationReviewSlotNames.Length; index++)
            {
                var slotRoot = RequireAnimationReviewSlot(placementRoot, index);
                var expectedPosition = CalculateAnimationReviewSlotLocalPosition(index);
                if (Vector3.Distance(slotRoot.localPosition, expectedPosition) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"Con Spirito animation review slot position mismatch: {slotRoot.name}, Expected={FormatVector(expectedPosition)}, Actual={FormatVector(slotRoot.localPosition)}.");
                }

                var modelObject = RequireModelObject(slotRoot, OriginalModelChildName);
                totalRenderers += RequireRenderers(slotRoot);
                if (index == AnimationReviewIdleSlotIndex)
                {
                    InspectIdleBreathReviewSlot(modelObject, idleClip, idleController);
                }
                else if (index == AnimationReviewWalkingSlotIndex)
                {
                    InspectWalkingReviewSlot(modelObject, walkClip, walkController);
                }
                else if (index == AnimationReviewChargeSlotIndex)
                {
                    InspectChargeReviewSlot(modelObject, chargeClip, chargeController);
                }
                else
                {
                    InspectDisabledReviewSlot(slotRoot);
                    disabledSlots++;
                }
            }

            Debug.Log(
                "ConSpiritoChargeLoopInspection " +
                $"Slots={AnimationReviewSlotNames.Length}, ChargeSlot={AnimationReviewSlotNames[AnimationReviewChargeSlotIndex]}, " +
                $"ChargeClip={chargeClip.name}, ChargeLength={chargeClip.length:0.###}, SourceWalkLength={walkClip.length:0.###}, " +
                $"RunCycleSeconds={ChargeRunLoopDurationSeconds:0.###}, ChargeLoopTime={chargeClipSettings.loopTime}, " +
                $"CurveBindings={curveBindings.Length}, ChargePoseTargets={string.Join("|", chargePoseTargets)}, " +
                $"AnimatedRepresentativeLegBones={animatedLegBones}/{RequiredLegBoneNames.Length}, DisabledSlots={disabledSlots}, TotalRenderers={totalRenderers}.");
        }

        private static void InspectIdleBreathCurveBindings(
            AnimationClip idleClip,
            EditorCurveBinding[] curveBindings,
            out int scaleCurveCount,
            out int positionCurveCount,
            out float minX,
            out float maxX,
            out float minY,
            out float maxY,
            out float minZ,
            out float maxZ,
            out string targetPaths,
            out int legCounterScaleCurveCount,
            out int legCounterPositionCurveCount,
            out string legCounterTargets)
        {
            scaleCurveCount = 0;
            positionCurveCount = 0;
            legCounterScaleCurveCount = 0;
            legCounterPositionCurveCount = 0;
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;
            minZ = float.PositiveInfinity;
            maxZ = float.NegativeInfinity;
            var uniqueTargetPaths = new List<string>();
            var uniqueLegCounterPaths = new List<string>();
            var compensatedLegNames = new HashSet<string>();

            foreach (var binding in curveBindings)
            {
                if (binding.type != typeof(Transform))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(binding.path) &&
                    binding.propertyName.StartsWith("localScale.", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Con Spirito idle breath clip must not animate the model root scale.");
                }

                var legCounterName = ExtractRequiredLegBoneNameFromPath(binding.path);
                if (!string.IsNullOrEmpty(legCounterName))
                {
                    if (binding.propertyName.StartsWith("localScale.", StringComparison.Ordinal))
                    {
                        legCounterScaleCurveCount++;
                        compensatedLegNames.Add(legCounterName);
                        AddUniquePath(uniqueLegCounterPaths, binding.path);
                    }
                    else if (binding.propertyName.StartsWith("localPosition.", StringComparison.Ordinal))
                    {
                        legCounterPositionCurveCount++;
                        compensatedLegNames.Add(legCounterName);
                        AddUniquePath(uniqueLegCounterPaths, binding.path);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Con Spirito idle breath leg counter target has unsupported binding: {binding.path}/{binding.propertyName}.");
                    }

                    continue;
                }

                if (ContainsExcludedIdleBreathPathName(binding.path))
                {
                    throw new InvalidOperationException($"Con Spirito idle breath target path must not include leg-related transforms: {binding.path}.");
                }

                if (!string.IsNullOrEmpty(binding.path) &&
                    binding.propertyName.StartsWith("localScale.", StringComparison.Ordinal))
                {
                    var curve = AnimationUtility.GetEditorCurve(idleClip, binding);
                    if (curve == null)
                    {
                        throw new InvalidOperationException($"Con Spirito idle breath scale curve is missing: {binding.path}/{binding.propertyName}.");
                    }

                    GetCurveRange(curve, out var minimum, out var maximum);
                    if (string.Equals(binding.propertyName, "localScale.x", StringComparison.Ordinal))
                    {
                        minX = Mathf.Min(minX, minimum);
                        maxX = Mathf.Max(maxX, maximum);
                    }
                    else if (string.Equals(binding.propertyName, "localScale.y", StringComparison.Ordinal))
                    {
                        minY = Mathf.Min(minY, minimum);
                        maxY = Mathf.Max(maxY, maximum);
                    }
                    else if (string.Equals(binding.propertyName, "localScale.z", StringComparison.Ordinal))
                    {
                        minZ = Mathf.Min(minZ, minimum);
                        maxZ = Mathf.Max(maxZ, maximum);
                    }

                    scaleCurveCount++;
                    AddUniquePath(uniqueTargetPaths, binding.path);
                }
                else if (!string.IsNullOrEmpty(binding.path) &&
                         string.Equals(binding.propertyName, "localPosition.y", StringComparison.Ordinal))
                {
                    positionCurveCount++;
                    AddUniquePath(uniqueTargetPaths, binding.path);
                }
            }

            if (scaleCurveCount < 3 || positionCurveCount == 0)
            {
                throw new InvalidOperationException("Con Spirito idle breath clip must animate non-root torso scale and position curves.");
            }

            if (!IsFinite(minX) || !IsFinite(minY) || !IsFinite(minZ))
            {
                throw new InvalidOperationException("Con Spirito idle breath clip must contain x/y/z torso scale curves.");
            }

            if (maxX - minX < 0.010f || maxZ - minZ < 0.006f)
            {
                throw new InvalidOperationException("Con Spirito idle breath torso scale curves are too small to show visible breathing.");
            }

            targetPaths = string.Join("|", uniqueTargetPaths);
            legCounterTargets = string.Join("|", uniqueLegCounterPaths);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsInfinity(value) && !float.IsNaN(value);
        }

        private static string ExtractRequiredLegBoneNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            foreach (var requiredLegBoneName in RequiredLegBoneNames)
            {
                if (string.Equals(path, requiredLegBoneName, StringComparison.Ordinal) ||
                    path.EndsWith("/" + requiredLegBoneName, StringComparison.Ordinal))
                {
                    return requiredLegBoneName;
                }
            }

            return null;
        }

        private static void AddUniquePath(List<string> paths, string path)
        {
            foreach (var existingPath in paths)
            {
                if (string.Equals(existingPath, path, StringComparison.Ordinal))
                {
                    return;
                }
            }

            paths.Add(path);
        }

        private static void InspectIdleBreathReviewSlot(
            Transform modelObject,
            AnimationClip idleClip,
            AnimatorController idleController)
        {
            var animator = modelObject.GetComponent<Animator>();
            if (animator == null || !animator.enabled)
            {
                throw new InvalidOperationException("Con Spirito idle review slot Animator is missing or disabled.");
            }

            if (animator.runtimeAnimatorController != idleController)
            {
                throw new InvalidOperationException("Con Spirito idle review slot does not use the idle breath loop controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Con Spirito idle review slot must keep root motion disabled.");
            }

            if (idleController.layers.Length == 0 || idleController.layers[0].stateMachine.defaultState == null)
            {
                throw new InvalidOperationException("Con Spirito idle review controller has no default state.");
            }

            if (idleController.layers[0].stateMachine.defaultState.motion != idleClip)
            {
                throw new InvalidOperationException("Con Spirito idle review controller default state does not use the idle breath clip.");
            }

            InspectIdleBreathTargetHierarchy(modelObject, idleClip);
        }

        private static void InspectIdleBreathTargetHierarchy(Transform modelObject, AnimationClip idleClip)
        {
            var curveBindings = AnimationUtility.GetCurveBindings(idleClip);
            var bodyTargets = new List<Transform>();
            foreach (var binding in curveBindings)
            {
                if (binding.type != typeof(Transform) ||
                    string.IsNullOrEmpty(binding.path) ||
                    (!binding.propertyName.StartsWith("localScale.", StringComparison.Ordinal) &&
                     !binding.propertyName.StartsWith("localPosition.", StringComparison.Ordinal) &&
                     !string.Equals(binding.propertyName, "localPosition.y", StringComparison.Ordinal)))
                {
                    continue;
                }

                var legCounterName = ExtractRequiredLegBoneNameFromPath(binding.path);
                if (!string.IsNullOrEmpty(legCounterName))
                {
                    if (!binding.propertyName.StartsWith("localScale.", StringComparison.Ordinal) &&
                        !binding.propertyName.StartsWith("localPosition.", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Con Spirito idle breath leg counter target has unsupported binding: {binding.path}/{binding.propertyName}.");
                    }

                    continue;
                }

                if (ContainsExcludedIdleBreathPathName(binding.path))
                {
                    throw new InvalidOperationException($"Con Spirito idle breath target path must not include leg-related transforms: {binding.path}.");
                }

                var target = modelObject.Find(binding.path);
                if (target == null)
                {
                    throw new InvalidOperationException($"Con Spirito idle breath target is missing in model hierarchy: {binding.path}.");
                }

                AddUniqueTransform(bodyTargets, target);
            }

            foreach (var bodyTarget in bodyTargets)
            {
                foreach (var legBoneName in RequiredLegBoneNames)
                {
                    var legRoot = FindChildByName(modelObject, legBoneName);
                    if (legRoot == null || !legRoot.IsChildOf(bodyTarget))
                    {
                        continue;
                    }

                    var legPath = AnimationUtility.CalculateTransformPath(legRoot, modelObject);
                    RequireIdleBreathLegCounterBinding(curveBindings, legPath, "localScale.x");
                    RequireIdleBreathLegCounterBinding(curveBindings, legPath, "localScale.y");
                    RequireIdleBreathLegCounterBinding(curveBindings, legPath, "localScale.z");
                    RequireIdleBreathLegCounterBinding(curveBindings, legPath, "localPosition.x");
                    RequireIdleBreathLegCounterBinding(curveBindings, legPath, "localPosition.y");
                    RequireIdleBreathLegCounterBinding(curveBindings, legPath, "localPosition.z");
                }
            }
        }

        private static void AddUniqueTransform(List<Transform> transforms, Transform transform)
        {
            foreach (var existingTransform in transforms)
            {
                if (existingTransform == transform)
                {
                    return;
                }
            }

            transforms.Add(transform);
        }

        private static void RequireIdleBreathLegCounterBinding(EditorCurveBinding[] curveBindings, string path, string propertyName)
        {
            foreach (var binding in curveBindings)
            {
                if (binding.type == typeof(Transform) &&
                    string.Equals(binding.path, path, StringComparison.Ordinal) &&
                    string.Equals(binding.propertyName, propertyName, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Con Spirito idle breath leg counter binding is missing: {path}/{propertyName}.");
        }

        private static bool ContainsExcludedIdleBreathPathName(string path)
        {
            var parts = path.Split('/');
            foreach (var part in parts)
            {
                if (IsExcludedIdleBreathTargetName(part))
                {
                    return true;
                }
            }

            return false;
        }

        private static void InspectWalkingReviewSlot(
            Transform modelObject,
            AnimationClip walkClip,
            AnimatorController walkController)
        {
            var animator = modelObject.GetComponent<Animator>();
            if (animator == null || !animator.enabled)
            {
                throw new InvalidOperationException("Con Spirito walking review slot Animator is missing or disabled.");
            }

            if (animator.runtimeAnimatorController != walkController)
            {
                throw new InvalidOperationException("Con Spirito walking review slot does not use the original walk loop controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Con Spirito walking review slot must keep root motion disabled.");
            }

            if (walkController.layers.Length == 0 || walkController.layers[0].stateMachine.defaultState == null)
            {
                throw new InvalidOperationException("Con Spirito walking review controller has no default state.");
            }

            if (walkController.layers[0].stateMachine.defaultState.motion != walkClip)
            {
                throw new InvalidOperationException("Con Spirito walking review controller default state does not use the original FBX walk clip.");
            }
        }

        private static void InspectChargeReviewSlot(
            Transform modelObject,
            AnimationClip chargeClip,
            AnimatorController chargeController)
        {
            var animator = modelObject.GetComponent<Animator>();
            if (animator == null || !animator.enabled)
            {
                throw new InvalidOperationException("Con Spirito charge review slot Animator is missing or disabled.");
            }

            if (animator.runtimeAnimatorController != chargeController)
            {
                throw new InvalidOperationException("Con Spirito charge review slot does not use the charge loop controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Con Spirito charge review slot must keep root motion disabled.");
            }

            if (chargeController.layers.Length == 0 || chargeController.layers[0].stateMachine.defaultState == null)
            {
                throw new InvalidOperationException("Con Spirito charge review controller has no default state.");
            }

            if (chargeController.layers[0].stateMachine.defaultState.motion != chargeClip)
            {
                throw new InvalidOperationException("Con Spirito charge review controller default state does not use the charge clip.");
            }
        }

        private static void InspectDisabledReviewSlot(Transform slotRoot)
        {
            foreach (var animator in slotRoot.GetComponentsInChildren<Animator>(true))
            {
                if (animator.enabled || animator.runtimeAnimatorController != null)
                {
                    throw new InvalidOperationException($"Con Spirito non-walk review slot must not play Animator: {slotRoot.name}.");
                }
            }

            foreach (var animation in slotRoot.GetComponentsInChildren<Animation>(true))
            {
                if (animation.enabled)
                {
                    throw new InvalidOperationException($"Con Spirito non-walk review slot must not play legacy Animation: {slotRoot.name}.");
                }
            }
        }

        private static void InspectOriginalSceneState(Transform placementRoot)
        {
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var cantabileRoot = RequireSceneRoot(CantabilePlacementRootName);
            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = reviewObject.Find(OriginalModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{OriginalModelChildName} is missing under {PlacementObjectName}.");
            }

            if (reviewObject.Find(ModelChildName) != null)
            {
                throw new InvalidOperationException($"{ModelChildName} should have been deleted before loading the original Con Spirito FBX.");
            }

            var rendererCount = RequireRenderers(reviewObject);
            InspectPlacementPosition(placementRoot, cantabileRoot.transform, longaRoot.transform, tergoRoot.transform);
            InspectPlayerStart(placementRoot);

            var spacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            var bounds = CalculateMeshDataWorldBounds(reviewObject, new Bounds(reviewObject.position, Vector3.one));
            Debug.Log(
                "ConSpiritoOriginalPlacementInspection " +
                $"Root={PlacementRootName}, Object={PlacementObjectName}, Model={OriginalModelChildName}, " +
                $"UnityAsset={OriginalUnityModelAssetPath}, Renderers={rendererCount}, " +
                $"LongaZ={longaRoot.transform.position.z:0.###}, TergoZ={tergoRoot.transform.position.z:0.###}, " +
                $"LongaTergoSpacing={spacing:0.###}, CantabileZ={cantabileRoot.transform.position.z:0.###}, " +
                $"ConSpiritoZ={placementRoot.position.z:0.###}, BoundsCenter={FormatVector(bounds.center)}, " +
                $"BoundsSize={FormatVector(bounds.size)}, Player={FormatVector(FindPlayerStartTransform().position)}.");
        }

        private static void InspectPlacementPosition(
            Transform placementRoot,
            Transform cantabileRoot,
            Transform longaRoot,
            Transform tergoRoot)
        {
            var spacing = CalculateLongaTergoSpacing(longaRoot, tergoRoot);
            var expectedPosition = new Vector3(
                cantabileRoot.position.x,
                cantabileRoot.position.y,
                cantabileRoot.position.z - spacing);
            var delta = Vector3.Distance(placementRoot.position, expectedPosition);
            if (delta > PlacementToleranceMeters)
            {
                throw new InvalidOperationException(
                    $"Con Spirito placement must use Cantabile minus Longa/Tergo Z spacing. Expected={FormatVector(expectedPosition)}, Actual={FormatVector(placementRoot.position)}, Delta={delta:0.###}.");
            }
        }

        private static void InspectPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Player start transform is missing.");
            }

            var focus = RequirePlacementObject(placementRoot);
            var bounds = CalculateMeshDataWorldBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.05f, 0.30f);
            var frontDirection = CalculateVisualFrontDirection(focus);
            var playerFromFocus = player.position - lookAt;
            playerFromFocus.y = 0f;
            if (playerFromFocus.sqrMagnitude < 0.001f || Vector3.Dot(playerFromFocus.normalized, frontDirection) < PlayerFacingToleranceDot)
            {
                throw new InvalidOperationException("Player start is not placed in front of Con Spirito.");
            }

            var toFocus = lookAt - player.position;
            toFocus.y = 0f;
            var playerForward = player.forward;
            playerForward.y = 0f;
            if (toFocus.sqrMagnitude < 0.001f || playerForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(playerForward.normalized, toFocus.normalized) < PlayerFacingToleranceDot)
            {
                throw new InvalidOperationException("Player start is not facing Con Spirito.");
            }
        }

        private static void InspectReriggedLegBones(Transform root)
        {
            foreach (var requiredBoneName in RequiredLegBoneNames)
            {
                if (!ContainsTransformName(root, requiredBoneName))
                {
                    throw new InvalidOperationException($"Con Spirito required leg bone is missing: {requiredBoneName}.");
                }
            }

            foreach (var removedBoneName in RemovedChildLegBoneNames)
            {
                if (ContainsTransformName(root, removedBoneName))
                {
                    throw new InvalidOperationException($"Con Spirito child leg bone should have been removed: {removedBoneName}.");
                }
            }
        }

        private static void InspectDefaultAnimationLoop(
            Transform placementRoot,
            AnimationClip defaultClip,
            AnimatorController controller)
        {
            if (defaultClip == null)
            {
                throw new InvalidOperationException("Con Spirito default animation clip is missing.");
            }

            var clipSettings = AnimationUtility.GetAnimationClipSettings(defaultClip);
            if (!clipSettings.loopTime)
            {
                throw new InvalidOperationException($"Con Spirito default animation clip is not set to loop: {defaultClip.name}.");
            }

            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = reviewObject.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {PlacementObjectName}.");
            }

            var animator = modelObject.GetComponent<Animator>();
            if (animator == null || !animator.enabled)
            {
                throw new InvalidOperationException("Con Spirito model Animator is missing or disabled.");
            }

            if (animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Con Spirito model Animator does not use the default loop controller.");
            }

            if (controller.layers.Length == 0 || controller.layers[0].stateMachine.defaultState == null)
            {
                throw new InvalidOperationException("Con Spirito default loop controller has no default state.");
            }

            if (controller.layers[0].stateMachine.defaultState.motion != defaultClip)
            {
                throw new InvalidOperationException("Con Spirito default loop controller default state does not use the imported FBX clip.");
            }

            var curveBindings = AnimationUtility.GetCurveBindings(defaultClip);
            var animatedLegBones = 0;
            foreach (var requiredBoneName in RequiredLegBoneNames)
            {
                if (ClipContainsBoneBinding(curveBindings, requiredBoneName))
                {
                    animatedLegBones++;
                }
            }

            if (animatedLegBones < RequiredLegBoneNames.Length)
            {
                throw new InvalidOperationException(
                    $"Con Spirito default animation clip does not bind all representative leg bones. Animated={animatedLegBones}, Required={RequiredLegBoneNames.Length}.");
            }

            Debug.Log(
                "ConSpiritoDefaultAnimationLoopInspection " +
                $"Clip={defaultClip.name}, Length={defaultClip.length:0.###}, LoopTime={clipSettings.loopTime}, " +
                $"Controller={DefaultLoopControllerAssetPath}, CurveBindings={curveBindings.Length}, " +
                $"AnimatedRepresentativeLegBones={animatedLegBones}/{RequiredLegBoneNames.Length}.");
        }

        private static void InspectOriginalAnimationLoop(
            Transform placementRoot,
            AnimationClip originalClip,
            AnimatorController controller)
        {
            if (originalClip == null)
            {
                throw new InvalidOperationException("Con Spirito original animation clip is missing.");
            }

            var clipSettings = AnimationUtility.GetAnimationClipSettings(originalClip);
            if (!clipSettings.loopTime)
            {
                throw new InvalidOperationException($"Con Spirito original animation clip is not set to loop: {originalClip.name}.");
            }

            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = RequireModelObject(reviewObject, OriginalModelChildName);
            var animator = modelObject.GetComponent<Animator>();
            if (animator == null || !animator.enabled)
            {
                throw new InvalidOperationException("Original Con Spirito model Animator is missing or disabled.");
            }

            if (animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Original Con Spirito model Animator does not use the original loop controller.");
            }

            if (controller.layers.Length == 0 || controller.layers[0].stateMachine.defaultState == null)
            {
                throw new InvalidOperationException("Original Con Spirito loop controller has no default state.");
            }

            if (controller.layers[0].stateMachine.defaultState.motion != originalClip)
            {
                throw new InvalidOperationException("Original Con Spirito loop controller default state does not use the imported FBX clip.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Original Con Spirito Animator must keep root motion disabled.");
            }

            var dogWalkController = AssetDatabase.LoadAssetAtPath<AnimatorController>(DogWalkControllerAssetPath);
            if (dogWalkController != null && animator.runtimeAnimatorController == dogWalkController)
            {
                throw new InvalidOperationException("Original Con Spirito still uses the generated dog walk controller.");
            }

            var curveBindings = AnimationUtility.GetCurveBindings(originalClip);
            if (curveBindings.Length == 0)
            {
                throw new InvalidOperationException("Original Con Spirito imported animation clip has no curve bindings.");
            }

            var animatedLegBones = 0;
            foreach (var requiredBoneName in RequiredLegBoneNames)
            {
                if (ClipContainsBoneBinding(curveBindings, requiredBoneName))
                {
                    animatedLegBones++;
                }
            }

            Debug.Log(
                "ConSpiritoOriginalAnimationLoopInspection " +
                $"Clip={originalClip.name}, Length={originalClip.length:0.###}, LoopTime={clipSettings.loopTime}, " +
                $"Controller={OriginalLoopControllerAssetPath}, CurveBindings={curveBindings.Length}, " +
                $"AnimatedRepresentativeLegBones={animatedLegBones}/{RequiredLegBoneNames.Length}, " +
                $"DogWalkControllerDetached={animator.runtimeAnimatorController != dogWalkController}.");
        }

        private static void InspectDogWalkLoop(
            Transform placementRoot,
            AnimationClip dogWalkClip,
            AnimatorController controller)
        {
            if (dogWalkClip == null)
            {
                throw new InvalidOperationException("Con Spirito dog walk clip is missing.");
            }

            var clipSettings = AnimationUtility.GetAnimationClipSettings(dogWalkClip);
            if (!clipSettings.loopTime)
            {
                throw new InvalidOperationException($"Con Spirito dog walk clip is not set to loop: {dogWalkClip.name}.");
            }

            var reviewObject = RequirePlacementObject(placementRoot);
            var modelObject = RequireModelObject(reviewObject);
            var animator = modelObject.GetComponent<Animator>();
            if (animator == null || !animator.enabled)
            {
                throw new InvalidOperationException("Con Spirito model Animator is missing or disabled.");
            }

            if (animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Con Spirito model Animator does not use the dog walk controller.");
            }

            var defaultController = AssetDatabase.LoadAssetAtPath<AnimatorController>(DefaultLoopControllerAssetPath);
            if (defaultController != null && animator.runtimeAnimatorController == defaultController)
            {
                throw new InvalidOperationException("Con Spirito still uses the previous default FBX loop controller.");
            }

            if (controller.layers.Length == 0 || controller.layers[0].stateMachine.defaultState == null)
            {
                throw new InvalidOperationException("Con Spirito dog walk controller has no default state.");
            }

            if (controller.layers[0].stateMachine.defaultState.motion != dogWalkClip)
            {
                throw new InvalidOperationException("Con Spirito dog walk controller default state does not use the dog walk clip.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Con Spirito dog walk Animator must keep root motion disabled.");
            }

            var curveBindings = AnimationUtility.GetCurveBindings(dogWalkClip);
            var animatedLegBones = 0;
            foreach (var requiredBoneName in RequiredLegBoneNames)
            {
                if (ClipContainsBoneBinding(curveBindings, requiredBoneName))
                {
                    animatedLegBones++;
                }
            }

            if (animatedLegBones < RequiredLegBoneNames.Length)
            {
                throw new InvalidOperationException(
                    $"Con Spirito dog walk clip does not bind all representative leg bones. Animated={animatedLegBones}, Required={RequiredLegBoneNames.Length}.");
            }

            var rootPositionCurve = AnimationUtility.GetEditorCurve(
                dogWalkClip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localPosition.y"));
            if (rootPositionCurve == null)
            {
                throw new InvalidOperationException("Con Spirito dog walk clip is missing body bob localPosition.y curve.");
            }

            Debug.Log(
                "ConSpiritoDogWalkLoopInspection " +
                $"Clip={dogWalkClip.name}, Length={dogWalkClip.length:0.###}, LoopTime={clipSettings.loopTime}, " +
                $"Controller={DogWalkControllerAssetPath}, CurveBindings={curveBindings.Length}, " +
                $"AnimatedRepresentativeLegBones={animatedLegBones}/{RequiredLegBoneNames.Length}, " +
                $"DefaultControllerDetached={animator.runtimeAnimatorController != defaultController}.");
        }

        private static bool ClipContainsBoneBinding(EditorCurveBinding[] curveBindings, string boneName)
        {
            foreach (var binding in curveBindings)
            {
                if (string.Equals(binding.path, boneName, StringComparison.Ordinal) ||
                    binding.path.EndsWith("/" + boneName, StringComparison.Ordinal) ||
                    binding.path.Contains("/" + boneName + "/", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> ExtractChargePoseTargets(EditorCurveBinding[] curveBindings)
        {
            var paths = new List<string>();
            foreach (var binding in curveBindings)
            {
                if (!IsChargePoseBinding(binding))
                {
                    continue;
                }

                AddUniquePath(paths, binding.path);
            }

            return paths;
        }

        private static bool IsChargePoseBinding(EditorCurveBinding binding)
        {
            if (binding.type != typeof(Transform) ||
                (binding.propertyName != "localPosition.z" &&
                 binding.propertyName != "localPosition.y" &&
                 binding.propertyName != "localEulerAnglesRaw.x"))
            {
                return false;
            }

            var lowerPath = binding.path.ToLowerInvariant();
            if (lowerPath.Contains("leg") ||
                lowerPath.Contains("foot") ||
                lowerPath.Contains("toe"))
            {
                return false;
            }

            return lowerPath.Contains("head") ||
                   lowerPath.Contains("neck") ||
                   lowerPath.Contains("chest") ||
                   lowerPath.Contains("spine") ||
                   lowerPath.Contains("torso");
        }

        private static bool ContainsTransformName(Transform root, string targetName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, targetName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform RequirePlacementObject(Transform placementRoot)
        {
            var reviewObject = placementRoot.Find(PlacementObjectName);
            if (reviewObject == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            return reviewObject;
        }

        private static Transform RequireAnimationReviewSlot(Transform placementRoot, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= AnimationReviewSlotNames.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Con Spirito animation review slot index is out of range.");
            }

            var slotRoot = placementRoot.Find(AnimationReviewSlotNames[slotIndex]);
            if (slotRoot == null)
            {
                throw new InvalidOperationException($"Con Spirito animation review slot is missing: {AnimationReviewSlotNames[slotIndex]}.");
            }

            return slotRoot;
        }

        private static Transform RequireModelObject(Transform reviewObject)
        {
            return RequireModelObject(reviewObject, ModelChildName);
        }

        private static Transform RequireModelObject(Transform reviewObject, string modelChildName)
        {
            var modelObject = reviewObject.Find(modelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{modelChildName} is missing under {PlacementObjectName}.");
            }

            return modelObject;
        }

        private static Transform FindChildByName(Transform root, string targetName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, targetName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindFirstChildByName(Transform root, params string[] targetNames)
        {
            foreach (var targetName in targetNames)
            {
                var exactMatch = FindChildByName(root, targetName);
                if (exactMatch != null)
                {
                    return exactMatch;
                }
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var targetName in targetNames)
                {
                    if (child.name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private static void SetTransformCurve(AnimationClip clip, string path, string propertyName, params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private sealed class ReferenceRunVideoCaptureSession
        {
            private const double PrepareTimeoutSeconds = 90.0;
            private const double SeekTimeoutSeconds = 12.0;
            private const double FrameSettleSeconds = 0.12;

            private static readonly float[] SampleFractions =
            {
                0.000f,
                0.125f,
                0.250f,
                0.375f,
                0.500f,
                0.625f,
                0.750f,
                0.875f
            };

            private static ReferenceRunVideoCaptureSession active;

            private readonly Action<string> completeCallback;
            private readonly Action<Exception> failCallback;
            private readonly ReferenceVideoCaptureItem[] items;
            private readonly string outputDirectory;

            private int itemIndex = -1;
            private int sampleIndex = -1;
            private GameObject gameObject;
            private VideoPlayer videoPlayer;
            private RenderTexture renderTexture;
            private Texture2D texture;
            private double lengthSeconds;
            private double stepStartedAt;
            private double captureAt;
            private long targetFrame = -1L;
            private double targetTime;
            private bool prepared;
            private bool seekCompleted;
            private string videoError;
            private ReferenceCapturePhase phase;

            private ReferenceRunVideoCaptureSession(Action<string> completeCallback, Action<Exception> failCallback)
            {
                this.completeCallback = completeCallback ?? throw new ArgumentNullException(nameof(completeCallback));
                this.failCallback = failCallback ?? throw new ArgumentNullException(nameof(failCallback));
                outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReferenceRunAnalysisFolder));
                items = new[]
                {
                    new ReferenceVideoCaptureItem("DogRunReference", DogRunReferenceVideoPath),
                    new ReferenceVideoCaptureItem("CurrentConSpiritoCharge", CurrentConSpiritoRunVideoPath)
                };
            }

            public static void Start(Action<string> completeCallback, Action<Exception> failCallback)
            {
                if (active != null)
                {
                    failCallback(new InvalidOperationException("Con Spirito reference run video capture is already running."));
                    return;
                }

                active = new ReferenceRunVideoCaptureSession(completeCallback, failCallback);
                try
                {
                    Directory.CreateDirectory(active.outputDirectory);
                    EditorApplication.update += TickActive;
                    active.BeginNextItem();
                }
                catch (Exception exception)
                {
                    active.Fail(exception);
                }
            }

            private static void TickActive()
            {
                active?.Tick();
            }

            private void Tick()
            {
                try
                {
                    switch (phase)
                    {
                        case ReferenceCapturePhase.Preparing:
                            TickPreparing();
                            break;
                        case ReferenceCapturePhase.Seeking:
                            TickSeeking();
                            break;
                        case ReferenceCapturePhase.Capturing:
                            TickCapturing();
                            break;
                    }
                }
                catch (Exception exception)
                {
                    Fail(exception);
                }
            }

            private void BeginNextItem()
            {
                CleanupVideoObjects();
                itemIndex++;
                sampleIndex = -1;
                if (itemIndex >= items.Length)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    Complete($"Con Spirito reference run video frames captured to {outputDirectory}.");
                    return;
                }

                var item = items[itemIndex];
                var normalizedPath = Path.GetFullPath(item.VideoPath);
                if (!File.Exists(normalizedPath))
                {
                    throw new InvalidOperationException($"Con Spirito reference video is missing: {normalizedPath}");
                }

                gameObject = new GameObject($"ConSpirito_{item.Prefix}_VideoExtractor")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                renderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
                {
                    name = $"ConSpirito_{item.Prefix}_ReferenceRT"
                };
                texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = new Uri(normalizedPath).AbsoluteUri;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = renderTexture;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = false;
                videoPlayer.waitForFirstFrame = true;
                videoPlayer.skipOnDrop = false;
                videoPlayer.prepareCompleted += OnPrepared;
                videoPlayer.seekCompleted += OnSeekCompleted;
                videoPlayer.errorReceived += OnVideoError;

                prepared = false;
                seekCompleted = false;
                videoError = null;
                stepStartedAt = EditorApplication.timeSinceStartup;
                phase = ReferenceCapturePhase.Preparing;
                videoPlayer.Prepare();
            }

            private void TickPreparing()
            {
                ThrowIfVideoError();
                if (prepared || videoPlayer.isPrepared)
                {
                    lengthSeconds = videoPlayer.length;
                    if (lengthSeconds <= 0.0 &&
                        videoPlayer.frameCount > 0 &&
                        videoPlayer.frameRate > 0.0)
                    {
                        lengthSeconds = videoPlayer.frameCount / videoPlayer.frameRate;
                    }

                    if (lengthSeconds <= 0.0)
                    {
                        throw new InvalidOperationException($"Con Spirito reference video length is unavailable: {items[itemIndex].VideoPath}");
                    }

                    BeginNextSample();
                    return;
                }

                ThrowIfTimedOut(PrepareTimeoutSeconds, $"preparing {items[itemIndex].Prefix} reference video");
            }

            private void BeginNextSample()
            {
                sampleIndex++;
                if (sampleIndex >= SampleFractions.Length)
                {
                    Debug.Log(
                        $"ConSpiritoReferenceVideoCaptured Prefix={items[itemIndex].Prefix}, Length={lengthSeconds:0.###}, " +
                        $"FrameCount={videoPlayer.frameCount}, FrameRate={videoPlayer.frameRate:0.###}, Path={Path.GetFullPath(items[itemIndex].VideoPath)}.");
                    BeginNextItem();
                    return;
                }

                var fraction = SampleFractions[sampleIndex];
                targetTime = Math.Max(0.0, Math.Min(lengthSeconds * fraction, Math.Max(0.0, lengthSeconds - 0.04)));
                targetFrame = -1L;
                if (videoPlayer.frameCount > 0)
                {
                    var frameCount = videoPlayer.frameCount > (ulong)long.MaxValue
                        ? long.MaxValue
                        : (long)videoPlayer.frameCount;
                    targetFrame = Math.Max(0L, Math.Min((long)Math.Round((frameCount - 1) * (double)fraction), frameCount - 1));
                    videoPlayer.frame = targetFrame;
                }
                else
                {
                    videoPlayer.time = targetTime;
                }

                seekCompleted = false;
                stepStartedAt = EditorApplication.timeSinceStartup;
                phase = ReferenceCapturePhase.Seeking;
                videoPlayer.Play();
            }

            private void TickSeeking()
            {
                ThrowIfVideoError();
                var isCloseToFrame = targetFrame >= 0 && Math.Abs(videoPlayer.frame - targetFrame) <= 1;
                var isCloseToTime = targetFrame < 0 && Math.Abs(videoPlayer.time - targetTime) <= 0.08;
                if (seekCompleted || isCloseToFrame || isCloseToTime)
                {
                    videoPlayer.Pause();
                    captureAt = EditorApplication.timeSinceStartup + FrameSettleSeconds;
                    phase = ReferenceCapturePhase.Capturing;
                    return;
                }

                ThrowIfTimedOut(SeekTimeoutSeconds, $"seeking {items[itemIndex].Prefix} reference video frame {SampleFractions[sampleIndex]:0.###}");
            }

            private void TickCapturing()
            {
                if (EditorApplication.timeSinceStartup < captureAt)
                {
                    return;
                }

                var fraction = SampleFractions[sampleIndex];
                WriteRenderTextureToPng(
                    renderTexture,
                    texture,
                    Path.Combine(outputDirectory, $"{items[itemIndex].Prefix}_{Mathf.RoundToInt(fraction * 1000f):000}.png"));
                BeginNextSample();
            }

            private void OnPrepared(VideoPlayer source)
            {
                prepared = true;
            }

            private void OnSeekCompleted(VideoPlayer source)
            {
                seekCompleted = true;
            }

            private void OnVideoError(VideoPlayer source, string message)
            {
                videoError = message;
            }

            private void ThrowIfVideoError()
            {
                if (!string.IsNullOrEmpty(videoError))
                {
                    throw new InvalidOperationException($"Con Spirito reference video failed: {items[itemIndex].Prefix}, Error={videoError}");
                }
            }

            private void ThrowIfTimedOut(double timeoutSeconds, string description)
            {
                if (EditorApplication.timeSinceStartup - stepStartedAt > timeoutSeconds)
                {
                    throw new TimeoutException($"Timed out while {description}.");
                }
            }

            private void Complete(string successMarker)
            {
                DisposeActiveSession();
                completeCallback(successMarker);
            }

            private void Fail(Exception exception)
            {
                DisposeActiveSession();
                failCallback(exception);
            }

            private void DisposeActiveSession()
            {
                EditorApplication.update -= TickActive;
                CleanupVideoObjects();
                active = null;
            }

            private void CleanupVideoObjects()
            {
                if (videoPlayer != null)
                {
                    videoPlayer.prepareCompleted -= OnPrepared;
                    videoPlayer.seekCompleted -= OnSeekCompleted;
                    videoPlayer.errorReceived -= OnVideoError;
                    videoPlayer.Stop();
                }

                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    texture = null;
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                    renderTexture = null;
                }

                if (gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                    gameObject = null;
                }

                videoPlayer = null;
                phase = ReferenceCapturePhase.None;
            }

            private readonly struct ReferenceVideoCaptureItem
            {
                public ReferenceVideoCaptureItem(string prefix, string videoPath)
                {
                    Prefix = prefix;
                    VideoPath = videoPath;
                }

                public string Prefix { get; }
                public string VideoPath { get; }
            }

            private enum ReferenceCapturePhase
            {
                None,
                Preparing,
                Seeking,
                Capturing
            }
        }

        private static void CaptureVideoReferenceFrames(string videoPath, string outputPrefix, string outputDirectory)
        {
            var normalizedVideoPath = Path.GetFullPath(videoPath);
            if (!File.Exists(normalizedVideoPath))
            {
                throw new InvalidOperationException($"Con Spirito reference video is missing: {normalizedVideoPath}");
            }

            var gameObject = new GameObject($"ConSpirito_{outputPrefix}_VideoExtractor")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var renderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
            {
                name = $"ConSpirito_{outputPrefix}_ReferenceRT"
            };
            var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

            try
            {
                var videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.source = VideoSource.Url;
                videoPlayer.url = new Uri(normalizedVideoPath).AbsoluteUri;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = renderTexture;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = false;
                videoPlayer.waitForFirstFrame = true;
                videoPlayer.skipOnDrop = false;

                videoPlayer.Prepare();
                WaitForEditorCondition(
                    () => videoPlayer.isPrepared,
                    $"preparing {outputPrefix} reference video",
                    45.0);

                var lengthSeconds = videoPlayer.length;
                if (lengthSeconds <= 0.0 &&
                    videoPlayer.frameCount > 0 &&
                    videoPlayer.frameRate > 0.0)
                {
                    lengthSeconds = videoPlayer.frameCount / videoPlayer.frameRate;
                }

                if (lengthSeconds <= 0.0)
                {
                    throw new InvalidOperationException($"Con Spirito reference video length is unavailable: {normalizedVideoPath}");
                }

                var sampleFractions = new[] { 0.00f, 0.125f, 0.25f, 0.375f, 0.50f, 0.625f, 0.75f, 0.875f };
                foreach (var fraction in sampleFractions)
                {
                    CaptureVideoReferenceFrame(
                        videoPlayer,
                        renderTexture,
                        texture,
                        outputPrefix,
                        outputDirectory,
                        lengthSeconds,
                        fraction);
                }

                Debug.Log(
                    $"ConSpiritoReferenceVideoCaptured Prefix={outputPrefix}, Length={lengthSeconds:0.###}, " +
                    $"FrameCount={videoPlayer.frameCount}, FrameRate={videoPlayer.frameRate:0.###}, Path={normalizedVideoPath}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static void CaptureVideoReferenceFrame(
            VideoPlayer videoPlayer,
            RenderTexture renderTexture,
            Texture2D texture,
            string outputPrefix,
            string outputDirectory,
            double lengthSeconds,
            float fraction)
        {
            var targetTime = Math.Max(0.0, Math.Min(lengthSeconds * fraction, Math.Max(0.0, lengthSeconds - 0.04)));
            var targetFrame = -1L;
            if (videoPlayer.frameCount > 0)
            {
                var frameCount = videoPlayer.frameCount > (ulong)long.MaxValue
                    ? long.MaxValue
                    : (long)videoPlayer.frameCount;
                targetFrame = Math.Max(0L, Math.Min((long)Math.Round((frameCount - 1) * (double)fraction), frameCount - 1));
            }

            var seekCompleted = false;
            VideoPlayer.EventHandler seekHandler = _ => seekCompleted = true;
            videoPlayer.seekCompleted += seekHandler;
            try
            {
                if (targetFrame >= 0)
                {
                    videoPlayer.frame = targetFrame;
                }
                else
                {
                    videoPlayer.time = targetTime;
                }

                videoPlayer.Play();
                WaitForEditorCondition(
                    () => seekCompleted ||
                          (targetFrame >= 0 && Math.Abs(videoPlayer.frame - targetFrame) <= 1) ||
                          Math.Abs(videoPlayer.time - targetTime) <= 0.08,
                    $"seeking {outputPrefix} reference video frame {fraction:0.###}",
                    10.0);
                WaitForEditorDuration(0.08);
                videoPlayer.Pause();
            }
            finally
            {
                videoPlayer.seekCompleted -= seekHandler;
            }

            WriteRenderTextureToPng(
                renderTexture,
                texture,
                Path.Combine(outputDirectory, $"{outputPrefix}_{Mathf.RoundToInt(fraction * 1000f):000}.png"));
        }

        private static void WriteRenderTextureToPng(RenderTexture renderTexture, Texture2D texture, string outputPath)
        {
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply(false);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private static void WaitForEditorCondition(Func<bool> condition, string description, double timeoutSeconds)
        {
            var timeoutAt = EditorApplication.timeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (EditorApplication.timeSinceStartup > timeoutAt)
                {
                    throw new TimeoutException($"Timed out while {description}.");
                }

                EditorApplication.QueuePlayerLoopUpdate();
                System.Threading.Thread.Sleep(10);
            }
        }

        private static void WaitForEditorDuration(double durationSeconds)
        {
            var waitUntil = EditorApplication.timeSinceStartup + durationSeconds;
            while (EditorApplication.timeSinceStartup < waitUntil)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                System.Threading.Thread.Sleep(10);
            }
        }

        private static AnimationCurve RequireIdleBreathScaleCurve(AnimationClip clip, string propertyName)
        {
            var curve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), propertyName));
            if (curve == null)
            {
                throw new InvalidOperationException($"Con Spirito idle breath clip is missing {propertyName} curve.");
            }

            return curve;
        }

        private static void GetCurveRange(AnimationCurve curve, out float minimum, out float maximum)
        {
            if (curve.length == 0)
            {
                throw new InvalidOperationException("Con Spirito idle breath curve has no keys.");
            }

            minimum = curve.keys[0].value;
            maximum = curve.keys[0].value;
            foreach (var key in curve.keys)
            {
                minimum = Mathf.Min(minimum, key.value);
                maximum = Mathf.Max(maximum, key.value);
            }
        }

        private static Keyframe Key(float time, float value)
        {
            return new Keyframe(time, value);
        }

        private static void SetLocalRotationOffsetCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] eulerOffsets)
        {
            var path = AnimationUtility.CalculateTransformPath(target, root);
            var baseRotation = target.localRotation;
            var rotations = new Quaternion[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                rotations[index] = baseRotation * Quaternion.Euler(eulerOffsets[index]);
            }

            SetTransformCurve(clip, path, "m_LocalRotation.x", BuildComponentKeys(times, rotations, rotation => rotation.x));
            SetTransformCurve(clip, path, "m_LocalRotation.y", BuildComponentKeys(times, rotations, rotation => rotation.y));
            SetTransformCurve(clip, path, "m_LocalRotation.z", BuildComponentKeys(times, rotations, rotation => rotation.z));
            SetTransformCurve(clip, path, "m_LocalRotation.w", BuildComponentKeys(times, rotations, rotation => rotation.w));
        }

        private static Keyframe[] BuildComponentKeys(float[] times, Quaternion[] rotations, Func<Quaternion, float> selector)
        {
            var keys = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keys[index] = Key(times[index], selector(rotations[index]));
            }

            return keys;
        }

        private static void SetLocalPositionOffsetCurve(
            AnimationClip clip,
            Transform root,
            Transform target,
            string propertyName,
            float baseValue,
            float[] times,
            float[] offsets)
        {
            var path = AnimationUtility.CalculateTransformPath(target, root);
            var keys = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keys[index] = Key(times[index], baseValue + offsets[index]);
            }

            SetTransformCurve(clip, path, propertyName, keys);
        }

        private static List<TransformSnapshot> CaptureTransformSnapshots(Transform root)
        {
            var snapshots = new List<TransformSnapshot>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                snapshots.Add(new TransformSnapshot(transform));
            }

            return snapshots;
        }

        private static void RestoreTransformSnapshots(List<TransformSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Restore();
            }
        }

        private static void DisableImportedAnimationPlayback(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
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
                throw new InvalidOperationException($"{root.name} contains no renderers.");
            }

            return renderers.Length;
        }

        private static Material EnsureApprovedMaterialSampleAssets()
        {
            EnsureUnityAssetFolder(UnityTextureFolder);
            EnsureUnityAssetFolder(UnityMaterialFolder);
            CopyApprovedSampleTexture(ApprovedSampleAlbedoSourcePath, ApprovedUnityAlbedoAssetPath);
            CopyApprovedSampleTexture(ApprovedSampleBumpSourcePath, ApprovedUnityBumpAssetPath);
            ConfigureTextureImporter(ApprovedUnityAlbedoAssetPath, TextureImporterType.Default, sRgb: true);
            ConfigureTextureImporter(ApprovedUnityBumpAssetPath, TextureImporterType.NormalMap, sRgb: false);

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedUnityAlbedoAssetPath);
            if (albedo == null)
            {
                throw new InvalidOperationException($"Con Spirito approved albedo texture was not imported: {ApprovedUnityAlbedoAssetPath}.");
            }

            var bump = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedUnityBumpAssetPath);
            if (bump == null)
            {
                throw new InvalidOperationException($"Con Spirito approved bump texture was not imported: {ApprovedUnityBumpAssetPath}.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(ApprovedUnityMaterialAssetPath);
            var shader = FindLitShader();
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "ConSpirito_Approved_BrightRedFur"
                };
                AssetDatabase.CreateAsset(material, ApprovedUnityMaterialAssetPath);
            }

            material.shader = shader;
            SetMaterialTexture(material, albedo, "_BaseMap", "_MainTex");
            SetMaterialTexture(material, bump, "_BumpMap");
            SetMaterialColor(material, Color.white, "_BaseColor", "_Color");
            SetMaterialFloat(material, 0f, "_Metallic");
            SetMaterialFloat(material, 0.22f, "_Smoothness");
            SetMaterialFloat(material, 0.03f, "_BumpScale");
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return material;
        }

        private static Material LoadApprovedMaterialSampleAsset()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ApprovedUnityMaterialAssetPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Con Spirito approved material is missing at {ApprovedUnityMaterialAssetPath}.");
            }

            return material;
        }

        private static void EnsureUnityAssetFolder(string assetFolderPath)
        {
            Directory.CreateDirectory(ToAbsoluteProjectPath(assetFolderPath));
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void CopyApprovedSampleTexture(string sourceRelativePath, string destinationAssetPath)
        {
            var sourcePath = ToAbsoluteProjectPath(sourceRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Con Spirito approved sample texture source is missing.", sourcePath);
            }

            var destinationPath = ToAbsoluteProjectPath(destinationAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? ToAbsoluteProjectPath(UnityTextureFolder));
            File.Copy(sourcePath, destinationPath, overwrite: true);
            AssetDatabase.ImportAsset(destinationAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureTextureImporter(string textureAssetPath, TextureImporterType textureType, bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Con Spirito approved texture importer is missing: {textureAssetPath}.");
            }

            importer.textureType = textureType;
            importer.sRGBTexture = sRgb;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        private static Shader FindLitShader()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Standard");
            if (shader != null)
            {
                return shader;
            }

            throw new InvalidOperationException("Could not find a supported Lit shader for Con Spirito approved material.");
        }

        private static void SetMaterialTexture(Material material, Texture texture, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetTexture(propertyName, texture);
                }
            }
        }

        private static void SetMaterialColor(Material material, Color color, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetColor(propertyName, color);
                }
            }
        }

        private static void SetMaterialFloat(Material material, float value, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, value);
                }
            }
        }

        private static void ApplyApprovedMaterialSample(Transform placementRoot, Material approvedMaterial)
        {
            var rendererCount = 0;
            var materialSlotCount = 0;
            for (var index = 0; index < AnimationReviewSlotNames.Length; index++)
            {
                var slotRoot = placementRoot.Find(AnimationReviewSlotNames[index]);
                if (slotRoot == null)
                {
                    throw new InvalidOperationException($"Con Spirito animation review slot is missing: {AnimationReviewSlotNames[index]}.");
                }

                var modelObject = RequireModelObject(slotRoot, OriginalModelChildName);
                foreach (var renderer in modelObject.GetComponentsInChildren<Renderer>(true))
                {
                    rendererCount++;
                    var materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                    {
                        materials = new[] { approvedMaterial };
                    }
                    else
                    {
                        for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                        {
                            materials[materialIndex] = approvedMaterial;
                        }
                    }

                    materialSlotCount += materials.Length;
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }
            }

            Debug.Log(
                "ConSpiritoApprovedMaterialSampleApply " +
                $"Slots={AnimationReviewSlotNames.Length}, Renderers={rendererCount}, MaterialSlots={materialSlotCount}, " +
                $"Material={ApprovedUnityMaterialAssetPath}, Albedo={ApprovedUnityAlbedoAssetPath}, Bump={ApprovedUnityBumpAssetPath}.");
        }

        private static void InspectApprovedMaterialSample(Transform placementRoot, Material approvedMaterial)
        {
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedUnityAlbedoAssetPath);
            var bump = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedUnityBumpAssetPath);
            if (albedo == null || bump == null)
            {
                throw new InvalidOperationException("Con Spirito approved material textures are missing.");
            }

            var rendererCount = 0;
            var materialSlotCount = 0;
            var approvedMaterialSlotCount = 0;
            var albedoMatchCount = 0;
            var bumpMatchCount = 0;
            var vertexCount = 0;
            var normalCount = 0;

            for (var index = 0; index < AnimationReviewSlotNames.Length; index++)
            {
                var slotRoot = placementRoot.Find(AnimationReviewSlotNames[index]);
                if (slotRoot == null)
                {
                    throw new InvalidOperationException($"Con Spirito animation review slot is missing: {AnimationReviewSlotNames[index]}.");
                }

                var modelObject = RequireModelObject(slotRoot, OriginalModelChildName);
                var renderers = modelObject.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException($"{AnimationReviewSlotNames[index]} contains no Con Spirito renderers.");
                }

                foreach (var renderer in renderers)
                {
                    rendererCount++;
                    vertexCount += GetRendererVertexCount(renderer);
                    normalCount += GetRendererNormalCount(renderer);

                    var materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                    {
                        throw new InvalidOperationException($"{renderer.name} has no material slots after Con Spirito approved material application.");
                    }

                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        materialSlotCount++;
                        var material = materials[materialIndex];
                        if (IsApprovedMaterial(material, approvedMaterial))
                        {
                            approvedMaterialSlotCount++;
                        }

                        if (GetMaterialTextureAssetPath(material, "_BaseMap", "_MainTex") == ApprovedUnityAlbedoAssetPath)
                        {
                            albedoMatchCount++;
                        }

                        if (GetMaterialTextureAssetPath(material, "_BumpMap") == ApprovedUnityBumpAssetPath)
                        {
                            bumpMatchCount++;
                        }
                    }
                }
            }

            if (approvedMaterialSlotCount != materialSlotCount)
            {
                throw new InvalidOperationException(
                    $"Con Spirito approved material slot mismatch. Approved={approvedMaterialSlotCount}, Total={materialSlotCount}.");
            }

            if (albedoMatchCount != materialSlotCount)
            {
                throw new InvalidOperationException(
                    $"Con Spirito approved albedo texture mismatch. Matched={albedoMatchCount}, Total={materialSlotCount}.");
            }

            Debug.Log(
                "ConSpiritoApprovedMaterialSampleInspection " +
                $"Slots={AnimationReviewSlotNames.Length}, Renderers={rendererCount}, MaterialSlots={materialSlotCount}, " +
                $"ApprovedMaterialSlots={approvedMaterialSlotCount}, AlbedoMatchedSlots={albedoMatchCount}, BumpMatchedSlots={bumpMatchCount}, " +
                $"Vertices={vertexCount}, Normals={normalCount}, Material={ApprovedUnityMaterialAssetPath}, " +
                $"Albedo={ApprovedUnityAlbedoAssetPath}, Bump={ApprovedUnityBumpAssetPath}.");
        }

        private static bool IsApprovedMaterial(Material material, Material approvedMaterial)
        {
            return material == approvedMaterial || AssetDatabase.GetAssetPath(material) == ApprovedUnityMaterialAssetPath;
        }

        private static string GetMaterialTextureAssetPath(Material material, params string[] propertyNames)
        {
            if (material == null)
            {
                return string.Empty;
            }

            foreach (var propertyName in propertyNames)
            {
                if (!material.HasProperty(propertyName))
                {
                    continue;
                }

                var texture = material.GetTexture(propertyName);
                if (texture == null)
                {
                    continue;
                }

                return AssetDatabase.GetAssetPath(texture);
            }

            return string.Empty;
        }

        private static string ToAbsoluteProjectPath(string projectRelativePath)
        {
            var normalizedRelativePath = projectRelativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", normalizedRelativePath));
        }

        private static MaterialInspectionSummary InspectMaterialSet(Transform root, string label)
        {
            var summary = new MaterialInspectionSummary(label);
            InspectMaterialSet(root, label, summary);
            return summary;
        }

        private static void InspectMaterialSet(Transform root, string label, MaterialInspectionSummary summary)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"{root.name} contains no renderers for material inspection.");
            }

            foreach (var renderer in renderers)
            {
                summary.Renderers++;
                summary.Meshes += GetRendererMeshCount(renderer);
                summary.Normals += GetRendererNormalCount(renderer);
                summary.Vertices += GetRendererVertexCount(renderer);

                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    summary.EmptyMaterialRenderers++;
                    Debug.Log($"ConSpiritoMaterialRendererDetail Scope={label}, Renderer={renderer.name}, Type={renderer.GetType().Name}, Materials=0.");
                    continue;
                }

                for (var index = 0; index < materials.Length; index++)
                {
                    summary.MaterialSlots++;
                    var material = materials[index];
                    if (material == null)
                    {
                        summary.NullMaterials++;
                        Debug.Log($"ConSpiritoMaterialSlotDetail Scope={label}, Renderer={renderer.name}, Slot={index}, Material=<null>.");
                        continue;
                    }

                    var color = GetMaterialColor(material);
                    var hasTexture = TryGetMaterialTexture(material, out var textureName, out var texturePath);
                    var isWhiteLike = IsWhiteLikeMaterial(material, color, hasTexture);
                    if (hasTexture)
                    {
                        summary.TexturedMaterials++;
                    }

                    if (isWhiteLike)
                    {
                        summary.WhiteLikeMaterials++;
                    }

                    var materialPath = AssetDatabase.GetAssetPath(material);
                    var shaderName = material.shader != null ? material.shader.name : "<none>";
                    Debug.Log(
                        "ConSpiritoMaterialSlotDetail " +
                        $"Scope={label}, Renderer={renderer.name}, Type={renderer.GetType().Name}, Slot={index}, " +
                        $"Material={material.name}, MaterialPath={FormatAssetPath(materialPath)}, Shader={shaderName}, " +
                        $"Color={FormatColor(color)}, HasTexture={hasTexture}, Texture={textureName}, TexturePath={FormatAssetPath(texturePath)}, " +
                        $"WhiteLike={isWhiteLike}, Vertices={GetRendererVertexCount(renderer)}, Normals={GetRendererNormalCount(renderer)}.");
                }
            }
        }

        private static int GetRendererMeshCount(Renderer renderer)
        {
            return GetRendererMesh(renderer) != null ? 1 : 0;
        }

        private static int GetRendererVertexCount(Renderer renderer)
        {
            var mesh = GetRendererMesh(renderer);
            return mesh != null ? mesh.vertexCount : 0;
        }

        private static int GetRendererNormalCount(Renderer renderer)
        {
            var mesh = GetRendererMesh(renderer);
            return mesh != null && mesh.normals != null ? mesh.normals.Length : 0;
        }

        private static Mesh GetRendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static Color GetMaterialColor(Material material)
        {
            var colorProperties = new[] { "_BaseColor", "_Color", "_DiffuseColor" };
            foreach (var propertyName in colorProperties)
            {
                if (material.HasProperty(propertyName))
                {
                    return material.GetColor(propertyName);
                }
            }

            return Color.white;
        }

        private static bool TryGetMaterialTexture(Material material, out string textureName, out string texturePath)
        {
            var textureProperties = new[] { "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo", "_DiffuseMap", "_EmissionMap" };
            foreach (var propertyName in textureProperties)
            {
                if (!material.HasProperty(propertyName))
                {
                    continue;
                }

                var texture = material.GetTexture(propertyName);
                if (texture == null)
                {
                    continue;
                }

                textureName = texture.name;
                texturePath = AssetDatabase.GetAssetPath(texture);
                return true;
            }

            textureName = "<none>";
            texturePath = string.Empty;
            return false;
        }

        private static bool IsWhiteLikeMaterial(Material material, Color color, bool hasTexture)
        {
            if (hasTexture)
            {
                return false;
            }

            if (material.name.IndexOf("Default-Material", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return color.r >= 0.95f && color.g >= 0.95f && color.b >= 0.95f && color.a >= 0.95f;
        }

        private static string FormatColor(Color color)
        {
            return $"({color.r:0.###}, {color.g:0.###}, {color.b:0.###}, {color.a:0.###})";
        }

        private static string FormatAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? "<embedded-or-none>" : path;
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

        private static float CalculateLongaTergoSpacing(Transform longaRoot, Transform tergoRoot)
        {
            var zSpacing = Mathf.Abs(longaRoot.position.z - tergoRoot.position.z);
            if (zSpacing > 0.10f)
            {
                return zSpacing;
            }

            return Mathf.Max(Vector3.Distance(longaRoot.position, tergoRoot.position), LongaTergoFallbackSpacing);
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

        private static Bounds CalculateMeshDataWorldBounds(Transform root, Bounds fallback)
        {
            var hasBounds = false;
            var bounds = fallback;

            foreach (var skinnedRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinnedRenderer.sharedMesh == null)
                {
                    continue;
                }

                var worldBounds = TransformBounds(skinnedRenderer.transform.localToWorldMatrix, skinnedRenderer.sharedMesh.bounds);
                if (!hasBounds)
                {
                    bounds = worldBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(worldBounds);
                }
            }

            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null)
                {
                    continue;
                }

                var worldBounds = TransformBounds(meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh.bounds);
                if (!hasBounds)
                {
                    bounds = worldBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(worldBounds);
                }
            }

            return hasBounds ? bounds : CalculateRendererBounds(root, fallback);
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds localBounds)
        {
            var min = localBounds.min;
            var max = localBounds.max;
            var corners = new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            var worldBounds = new Bounds(matrix.MultiplyPoint3x4(corners[0]), Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                worldBounds.Encapsulate(matrix.MultiplyPoint3x4(corners[i]));
            }

            return worldBounds;
        }

        private static Vector3 CalculateVisualFrontDirection(Transform focus)
        {
            var frontDirection = Quaternion.Euler(0f, focus.eulerAngles.y, 0f) * Vector3.forward;
            frontDirection.y = 0f;
            return frontDirection.sqrMagnitude > 0.001f ? frontDirection.normalized : Vector3.back;
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

        private static void CaptureTransformToPng(Transform target, string outputPath, int width, int height)
        {
            CaptureTransformToPng(target, outputPath, width, height, 0f);
        }

        private static void CaptureTransformToPng(Transform target, string outputPath, int width, int height, float yawOffsetDegrees)
        {
            CaptureTransformToPng(target, outputPath, width, height, yawOffsetDegrees, 34f, 2.25f, 6.00f);
        }

        private static void CaptureTransformToPng(
            Transform target,
            string outputPath,
            int width,
            int height,
            float yawOffsetDegrees,
            float fieldOfView,
            float distanceMultiplier,
            float maxDistance)
        {
            var bounds = CalculateMeshDataWorldBounds(target, new Bounds(target.position, Vector3.one));
            var focus = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.25f);
            var frontDirection = Quaternion.AngleAxis(yawOffsetDegrees, Vector3.up) * CalculateVisualFrontDirection(target);
            frontDirection.y = 0f;
            frontDirection = frontDirection.sqrMagnitude > 0.001f ? frontDirection.normalized : Vector3.back;
            var radius = Mathf.Max(bounds.extents.magnitude, 0.35f);
            var cameraPosition = focus + frontDirection * Mathf.Clamp(radius * distanceMultiplier, 1.50f, maxDistance) + Vector3.up * Mathf.Clamp(radius * 0.30f, 0.10f, 1.00f);

            var cameraObject = new GameObject("ConSpirito_Rerigged_CaptureCamera");
            var lightObject = new GameObject("ConSpirito_Rerigged_CaptureLight");
            var disabledRenderers = HideNonTargetRenderers(target);
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = cameraPosition;
                camera.transform.rotation = Quaternion.LookRotation((focus - cameraPosition).normalized, Vector3.up);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.02f, 0.025f, 1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.fieldOfView = fieldOfView;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.0f;
                light.transform.rotation = Quaternion.LookRotation((focus - cameraPosition).normalized, Vector3.up);

                var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                var previousActive = RenderTexture.active;
                try
                {
                    camera.targetTexture = renderTexture;
                    RenderTexture.active = renderTexture;
                    camera.Render();
                    var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previousActive;
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }
            }
            finally
            {
                RestoreRenderers(disabledRenderers);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static List<Renderer> HideNonTargetRenderers(Transform target)
        {
            var disabledRenderers = new List<Renderer>();
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || renderer.transform.IsChildOf(target))
                {
                    continue;
                }

                if (renderer.enabled)
                {
                    renderer.enabled = false;
                    disabledRenderers.Add(renderer);
                }
            }

            return disabledRenderers;
        }

        private static void RestoreRenderers(List<Renderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public void Restore()
            {
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }

        private readonly struct IdleBreathTarget
        {
            public IdleBreathTarget(Transform transform, int score, float weight)
            {
                Transform = transform;
                Score = score;
                Weight = weight;
            }

            public Transform Transform { get; }
            public int Score { get; }
            public float Weight { get; }

            public IdleBreathTarget WithWeight(float weight)
            {
                return new IdleBreathTarget(Transform, Score, weight);
            }
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.###}, {value.y:0.###}, {value.z:0.###})";
        }

        private sealed class MaterialInspectionSummary
        {
            public MaterialInspectionSummary(string label)
            {
                Label = label;
            }

            public string Label { get; }
            public int Renderers { get; set; }
            public int Meshes { get; set; }
            public int Vertices { get; set; }
            public int Normals { get; set; }
            public int EmptyMaterialRenderers { get; set; }
            public int MaterialSlots { get; set; }
            public int NullMaterials { get; set; }
            public int WhiteLikeMaterials { get; set; }
            public int TexturedMaterials { get; set; }

            public string ToLogLine(string prefix)
            {
                return
                    $"{prefix} " +
                    $"Label={Label}, Renderers={Renderers}, Meshes={Meshes}, Vertices={Vertices}, Normals={Normals}, " +
                    $"EmptyMaterialRenderers={EmptyMaterialRenderers}, MaterialSlots={MaterialSlots}, NullMaterials={NullMaterials}, " +
                    $"WhiteLikeMaterials={WhiteLikeMaterials}, TexturedMaterials={TexturedMaterials}.";
            }
        }
    }
}
