using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.UrzereCargoRunScene
{
    internal static class UrzereCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TergoPlacementRootName = "Approved Tergo Enemy Placement";
        private const string LongaArmaPlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string PlacementRootName = "Approved Urzere Enemy Placement";
        private const string PlacementObjectName = "Urzere_00_Static_Review";
        private const string ModelChildName = "UrzerePrepared_Model";
        private const string ReviewCameraName = "Model Cam";
        private const string PlayerRootName = "Player";

        private const string SourceModelAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/urgere.glb";
        private const string SeedModelSourceAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/windy seed.glb";
        private const string UrzereArtRoot = "Assets/_Project/Art/Enemies/Urzere";
        private const string UnityModelFolder = UrzereArtRoot + "/Models";
        private const string UnityMaterialFolder = UrzereArtRoot + "/Materials";
        private const string UnityAnimationFolder = UrzereArtRoot + "/Animations";
        private const string UnityControllerFolder = UrzereArtRoot + "/Controllers";
        private const string UnityMeshFolder = UrzereArtRoot + "/Meshes";
        private const string UnityModelAssetPath = UnityModelFolder + "/urgere.glb";
        private const string SeedModelAssetPath = UnityModelFolder + "/windy_seed.glb";
        private const string UnityMaterialAssetPath = UnityMaterialFolder + "/M_Urzere_Olive_Wax_Body.mat";
        private const string IdleBreathingClipName = "Urzere_02_Idle_Breathing_Morph";
        private const string IdleBreathingClipPath = UnityAnimationFolder + "/" + IdleBreathingClipName + ".anim";
        private const string IdleBreathingControllerPath = UnityControllerFolder + "/" + IdleBreathingClipName + ".controller";
        private const string MoveBodyLiftWheelRollClipName = "Urzere_03_Move_BodyLift_WheelRoll";
        private const string MoveBodyLiftWheelRollClipPath = UnityAnimationFolder + "/" + MoveBodyLiftWheelRollClipName + ".anim";
        private const string MoveBodyLiftWheelRollControllerPath = UnityControllerFolder + "/" + MoveBodyLiftWheelRollClipName + ".controller";
        private const string MoveBodyLiftBlendShapeName = "BodyLift_RevealWheel";
        private const string MoveWheelRollABlendShapeName = "WheelRoll_PhaseA";
        private const string MoveWheelRollBBlendShapeName = "WheelRoll_PhaseB";
        private const string MoveWheelOnlyClipName = "Urzere_03_Move_WheelOnly_Roll";
        private const string MoveWheelOnlyClipPath = UnityAnimationFolder + "/" + MoveWheelOnlyClipName + ".anim";
        private const string MoveWheelOnlyControllerPath = UnityControllerFolder + "/" + MoveWheelOnlyClipName + ".controller";
        private const string MoveWheelOnlyRollABlendShapeName = "WheelOnly_RollForward_A";
        private const string MoveWheelOnlyRollBBlendShapeName = "WheelOnly_RollForward_B";
        private const string MoveWheelOnlyRollCBlendShapeName = "WheelOnly_RollForward_C";
        private const string MoveWheelOnlyVisualRootName = "Urzere_03_Move_RollingWheelVisuals";
        private const string MoveWheelOnlyLeftWheelName = "WheelRoll_Left";
        private const string MoveWheelOnlyRightWheelName = "WheelRoll_Right";
        private const string MoveWheelOnlyMeshPath = UnityMeshFolder + "/Urzere_03_Move_RollingWheelVisual.asset";
        private const string MoveWheelOnlyValidationFolder = "docs/validation/urzere_move_wheel_only";
        private const string SeedEmitClipName = "Urzere_05_Seed_Emit_Buff_Pulse";
        private const string SeedEmitClipPath = UnityAnimationFolder + "/" + SeedEmitClipName + ".anim";
        private const string SeedEmitControllerPath = UnityControllerFolder + "/" + SeedEmitClipName + ".controller";
        private const string SeedEmitVisualRootName = "Urzere_05_SeedEmitVisuals";
        private const string SeedEmitSeedPrefix = "WindySeed_";
        private const string SeedEmitSeedModelChildName = "WindySeed_Model";
        private const string SeedEmitSharedStaticMeshPath = UnityMeshFolder + "/Urzere_05_SeedEmit_WindySeed_StaticShared.asset";
        private const string SeedEmitValidationFolder = "docs/validation/urzere_seed_emit_buff_pulse";
        private const string DeathClipName = "Urzere_07_Death";
        private const string DeathClipPath = UnityAnimationFolder + "/" + DeathClipName + ".anim";
        private const string DeathControllerPath = UnityControllerFolder + "/" + DeathClipName + ".controller";
        private const string DeathRightRearCollapseBlendShapeName = "Death_RightRearWheel_Collapse";
        private const string DeathValidationFolder = "docs/validation/urzere_death";

        private const float UrzereTargetHeightMeters = 1.00f;
        private const float UrzereFacingYawDegrees = 180f;
        private const float UrzereFallbackTergoLongaSpacing = 4.00f;
        private const float ReviewCameraMinimumFrontDistance = 3.25f;
        private const float ReviewCameraMaximumFrontDistance = 7.00f;
        private const float ReviewPlayerFrontDistance = 4.20f;
        private const float MotionSlotMinimumSpacing = 1.55f;
        private const float IdleBreathingDurationSeconds = 3.00f;
        private const float MoveBodyLiftWheelRollDurationSeconds = 2.40f;
        private const float MoveBodyLiftLocalOffset = 0.072f;
        private const float MoveWheelRollLocalAmplitude = 0.036f;
        private const float MoveWheelRollStartSeconds = 0.68f;
        private const float MoveWheelRollDegrees = -720f;
        private const float MoveWheelOnlyDurationSeconds = 1.60f;
        private const float MoveWheelOnlyLocalAmplitude = 0.180f;
        private const float MoveWheelOnlyVisualRadius = 0.245f;
        private const float MoveWheelOnlyVisualThickness = 0.040f;
        private const float MoveWheelOnlyRotationDegrees = -900f;
        private const float SeedEmitDurationSeconds = 2.60f;
        private const float SeedEmitLaunchSeconds = 0.62f;
        private const float SeedEmitPeakSeconds = 1.18f;
        private const float SeedEmitFadeSeconds = 2.08f;
        private const float SeedEmitResetSeconds = 2.36f;
        private const int SeedEmitSeedCount = 144;
        private const float DeathDurationSeconds = 2.40f;
        private const float DeathSettleSeconds = 1.20f;
        private const float DeathRightRearCollapseLocalOffset = 0.300f;
        private const float DeathRightRearTiltXDegrees = -5.0f;
        private const float DeathRightRearTiltZDegrees = -7.0f;
        private const float DeathBodySettleLocalYOffset = -0.160f;
        private const int MoveBodyLiftTargetLimit = 18;
        private const int MoveWheelRollTargetLimit = 10;
        private const float PuddleCutHeightRatio = 0.16f;
        private const float PuddleCoreSampleHeightRatio = 0.26f;
        private const float PuddleCoreOutsetMeters = 0.055f;
        private const float OuterFootPlatformCutHeightRatio = 0.36f;
        private const float BodyFootprintBandMinRatio = 0.34f;
        private const float BodyFootprintBandMaxRatio = 0.68f;
        private const float BodyFootprintOutsetMeters = 0.025f;
        private const float BodyFootprintPercentileMargin = 0.04f;
        private static readonly Color UrzereOliveWaxColor = new(0.20f, 0.28f, 0.12f, 1f);
        private static readonly MotionSlotSpec[] MotionSlotSpecs =
        {
            new("Urzere_02_Idle"),
            new("Urzere_03_Move"),
            new("Urzere_04_Anchor_Deploy"),
            new("Urzere_05_Seed_Emit_Buff_Pulse"),
            new("Urzere_06_Hit"),
            new("Urzere_07_Death")
        };

        [MenuItem("Bellerophon/Enemies/Urzere/Apply Prepared Model To CargoRunMvp")]
        public static void ApplyPreparedModelToCurrentCargoRunScene()
        {
            RequirePreparedModelFile();
            EnsureUnityFolders();
            CopyPreparedModelAsset();
            ConfigureImportedModelAsset();

            var modelAsset = LoadPreparedModelAsset();
            var material = EnsureReferenceMaterial();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlacePreparedModel(modelAsset, material, scene);
            ConfigureInitialReviewCamera(placementRoot.transform);
            ConfigureInitialPlayerStart(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Urzere model applied to CargoRunMvp scene.");
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
            Debug.Log("Prepared Urzere CargoRunMvp scene state inspected.");
        }

        public static void MovePlayerStartToOppositeSide()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            MoveExistingPlayerStartToOppositeSide(placementRoot.transform);
            InspectPlayerStart(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Prepared Urzere player start moved to the opposite side.");
        }

        public static void AddMotionSlotObjectsOnCurrentZAxis()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var staticObject = RequireStaticReviewObject(placementRoot.transform);
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnityMaterialAssetPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Urzere material is missing at {UnityMaterialAssetPath}.");
            }

            AddOrRebuildMotionSlotObjects(placementRoot.transform, staticObject, material);
            InspectMotionSlotObjects(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Prepared Urzere motion slot objects 02-07 added on the current Z axis.");
        }

        public static void InspectMotionSlotObjectsInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectMotionSlotObjects(placementRoot.transform);
            Debug.Log("Prepared Urzere motion slot objects 02-07 inspected.");
        }

        public static void ApplyIdleBreathingAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityControllerFolder);
            EnsureUnityFolder(UnityMeshFolder);

            var idleSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_02_Idle");
            var clip = EnsureIdleBreathingClip(idleSlot);
            var controller = EnsureIdleBreathingController(clip);
            ConfigureIdleSlotAnimator(idleSlot, controller);
            InspectIdleBreathingAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Urzere 02 idle breathing animation applied.");
        }

        public static void ValidateIdleBreathingAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectIdleBreathingAnimation(placementRoot.transform);
            Debug.Log("Prepared Urzere 02 idle breathing animation validated.");
        }

        public static void ApplyMoveBodyLiftWheelRollAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityControllerFolder);

            var moveSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_03_Move");
            var clip = EnsureMoveBodyLiftWheelRollClip(moveSlot);
            var controller = EnsureMoveBodyLiftWheelRollController(clip);
            ConfigureMoveSlotAnimator(moveSlot, controller);
            InspectMoveBodyLiftWheelRollAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Urzere 03 move body-lift wheel-roll animation applied.");
        }

        public static void ValidateMoveBodyLiftWheelRollAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectMoveBodyLiftWheelRollAnimation(placementRoot.transform);
            Debug.Log("Prepared Urzere 03 move body-lift wheel-roll animation validated.");
        }

        public static void ApplyMoveWheelOnlyAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityControllerFolder);
            EnsureUnityFolder(UnityMeshFolder);

            var moveSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_03_Move");
            var clip = EnsureMoveWheelOnlyClip(moveSlot);
            var controller = EnsureMoveWheelOnlyController(clip);
            ConfigureMoveSlotAnimator(moveSlot, controller);
            InspectMoveWheelOnlyAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Urzere 03 move wheel-only animation applied.");
        }

        public static void ValidateMoveWheelOnlyAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectMoveWheelOnlyAnimation(placementRoot.transform);
            Debug.Log("Prepared Urzere 03 move wheel-only animation validated.");
        }

        public static void CaptureMoveWheelOnlyReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectMoveWheelOnlyAnimation(placementRoot.transform);
            var moveSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_03_Move");
            CaptureMoveWheelOnlyReviewFrames(moveSlot);
            Debug.Log("Prepared Urzere 03 move wheel-only review frames captured.");
        }

        public static void ApplySeedEmitBuffPulseAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"Could not find {PlacementRootName} in CargoRunMvp scene.");
            }

            EnsureUnityFolders();
            var seedSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_05_Seed_Emit_Buff_Pulse");
            var visuals = EnsureSeedEmitVisuals(seedSlot);
            var clip = EnsureSeedEmitBuffPulseClip(seedSlot, visuals);
            var controller = EnsureSeedEmitBuffPulseController(clip);
            ConfigureMoveSlotAnimator(seedSlot, controller);
            InspectSeedEmitBuffPulseAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Prepared Urzere 05 seed emit buff pulse animation applied.");
        }

        public static void ValidateSeedEmitBuffPulseAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"Could not find {PlacementRootName} in CargoRunMvp scene.");
            }

            InspectSeedEmitBuffPulseAnimation(placementRoot.transform);
            Debug.Log("Prepared Urzere 05 seed emit buff pulse animation validated.");
        }

        public static void CaptureSeedEmitBuffPulseReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"Could not find {PlacementRootName} in CargoRunMvp scene.");
            }

            InspectSeedEmitBuffPulseAnimation(placementRoot.transform);
            var seedSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_05_Seed_Emit_Buff_Pulse");
            CaptureSeedEmitBuffPulseReviewFrames(seedSlot);
            Debug.Log("Prepared Urzere 05 seed emit buff pulse review frames captured.");
        }

        public static void ApplyDeathAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"Could not find {PlacementRootName} in CargoRunMvp scene.");
            }

            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityControllerFolder);
            EnsureUnityFolder(UnityMeshFolder);

            var deathSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_07_Death");
            var renderer = EnsureDeathRenderer(deathSlot);
            var clip = EnsureDeathAnimationClip(deathSlot, renderer);
            var controller = EnsureDeathController(clip);
            ConfigureMoveSlotAnimator(deathSlot, controller);
            InspectDeathAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Prepared Urzere 07 death animation applied. Motion=RightRearTiltWholeBodySink, TiltX={DeathRightRearTiltXDegrees:0.#}, TiltZ={DeathRightRearTiltZDegrees:0.#}, SinkY={DeathBodySettleLocalYOffset:0.###}.");
        }

        public static void ValidateDeathAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"Could not find {PlacementRootName} in CargoRunMvp scene.");
            }

            InspectDeathAnimation(placementRoot.transform);
            Debug.Log("Prepared Urzere 07 death animation validated.");
        }

        public static void CaptureDeathReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"Could not find {PlacementRootName} in CargoRunMvp scene.");
            }

            InspectDeathAnimation(placementRoot.transform);
            var deathSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_07_Death");
            CaptureDeathReviewFrames(deathSlot);
            Debug.Log("Prepared Urzere 07 death review frames captured.");
        }

        public static void RemoveMoveAnimationFromScene()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"Could not find {PlacementRootName} in CargoRunMvp scene.");
            }

            var moveSlot = RequireMotionSlotObject(placementRoot.transform, "Urzere_03_Move");
            var rootAnimatorRemoved = RemoveRootAnimator(moveSlot);
            var wheelVisualRemoved = RemoveMoveWheelOnlyVisualRoot(moveSlot);
            DisableImportedAnimationPlayback(moveSlot);
            ResetMoveSlotBlendShapeWeights(moveSlot);
            ValidateMoveAnimationRemoved(moveSlot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Prepared Urzere 03 move animation removed. RootAnimatorRemoved={rootAnimatorRemoved}, WheelVisualRemoved={wheelVisualRemoved}.");
        }

        public static void RemoveGroundPuddleFromAllUrzereObjects()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolder(UnityMeshFolder);
            var result = RemoveGroundPuddleGeometry(placementRoot.transform);
            ValidateGroundPuddleRemoved(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"Prepared Urzere ground puddle removed. RenderersProcessed={result.RenderersProcessed}, RenderersDisabled={result.RenderersDisabled}, MeshesRebuilt={result.MeshesRebuilt}, TrianglesRemoved={result.TrianglesRemoved}.");
        }

        public static void ValidateGroundPuddleRemoval()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            ValidateGroundPuddleRemoved(placementRoot.transform);
            Debug.Log("Prepared Urzere ground puddle removal validated.");
        }

        public static void RemoveOuterFootPlatformsFromAllUrzereObjects()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolder(UnityMeshFolder);
            var result = RemoveOuterFootPlatformGeometry(placementRoot.transform);
            ValidateOuterFootPlatformsRemoved(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"Prepared Urzere outer foot platforms removed. RenderersProcessed={result.RenderersProcessed}, MeshesRebuilt={result.MeshesRebuilt}, TrianglesRemoved={result.TrianglesRemoved}.");
        }

        public static void ValidateOuterFootPlatformRemoval()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            ValidateOuterFootPlatformsRemoved(placementRoot.transform);
            Debug.Log("Prepared Urzere outer foot platform removal validated.");
        }

        public static void InspectRendererStructure()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var labels = new List<string> { PlacementObjectName };
            foreach (var spec in MotionSlotSpecs)
            {
                labels.Add(spec.ObjectName);
            }

            foreach (var label in labels)
            {
                var slot = placementRoot.transform.Find(label);
                if (slot == null)
                {
                    Debug.Log($"UrzereRendererStructure SlotMissing={label}");
                    continue;
                }

                var bounds = CalculateRendererBounds(slot, new Bounds(slot.position, Vector3.zero));
                Debug.Log($"UrzereRendererStructure Slot={label} BoundsCenter={bounds.center} BoundsSize={bounds.size} RendererCount={slot.GetComponentsInChildren<Renderer>(true).Length}");

                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh mesh = null;
                    if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                    {
                        mesh = skinnedMeshRenderer.sharedMesh;
                    }
                    else
                    {
                        var meshFilter = renderer.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            mesh = meshFilter.sharedMesh;
                        }
                    }

                    var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, slot);
                    var meshName = mesh != null ? mesh.name : "<no mesh>";
                    var vertexCount = mesh != null ? mesh.vertexCount : 0;
                    var triangleCount = mesh != null ? mesh.triangles.Length / 3 : 0;
                    Debug.Log(
                        $"UrzereRendererStructure Renderer={label}/{rendererPath} Type={renderer.GetType().Name} Mesh={meshName} Vertices={vertexCount} Triangles={triangleCount} BoundsCenter={renderer.bounds.center} BoundsSize={renderer.bounds.size}");
                }
            }
        }

        private static void RequirePreparedModelFile()
        {
            if (!File.Exists(SourceModelAbsolutePath))
            {
                throw new FileNotFoundException("Prepared Urzere GLB model is missing.", SourceModelAbsolutePath);
            }
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(UrzereArtRoot);
            EnsureUnityFolder(UnityModelFolder);
            EnsureUnityFolder(UnityMaterialFolder);
            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityControllerFolder);
            EnsureUnityFolder(UnityMeshFolder);
        }

        private static void CopyPreparedModelAsset()
        {
            CopyFileToAsset(SourceModelAbsolutePath, UnityModelAssetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureImportedModelAsset()
        {
            var modelImporter = AssetImporter.GetAtPath(UnityModelAssetPath) as ModelImporter;
            if (modelImporter == null)
            {
                return;
            }

            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.importBlendShapes = true;
            modelImporter.importAnimation = true;
            modelImporter.importVisibility = false;
            modelImporter.animationType = ModelImporterAnimationType.Generic;
            modelImporter.animationCompression = ModelImporterAnimationCompression.Off;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.globalScale = 1f;
            modelImporter.SaveAndReimport();
        }

        private static GameObject LoadPreparedModelAsset()
        {
            var glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath);
            if (glbAsset != null)
            {
                return glbAsset;
            }

            throw new InvalidOperationException(
                $"Could not load Urzere GLB as a Unity model asset. GLB path={UnityModelAssetPath}. Ensure com.unity.cloud.gltfast is installed and Editor import is enabled.");
        }

        private static Material EnsureReferenceMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnityMaterialAssetPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = "M_Urzere_Olive_Wax_Body"
                };
                AssetDatabase.CreateAsset(material, UnityMaterialAssetPath);
            }

            SetMaterialColor(material, UrzereOliveWaxColor);
            SetMaterialFloat(material, "_Smoothness", 0.82f);
            SetMaterialFloat(material, "_Glossiness", 0.82f);
            SetMaterialFloat(material, "_Metallic", 0f);
            if (material.HasProperty("_SpecColor"))
            {
                material.SetColor("_SpecColor", new Color(0.38f, 0.45f, 0.24f, 1f));
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject PlacePreparedModel(GameObject modelAsset, Material material, Scene scene)
        {
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var spacing = CalculateTergoLongaSpacing(tergoRoot.transform, longaRoot.transform);
            var placementPosition = new Vector3(
                tergoRoot.transform.position.x,
                tergoRoot.transform.position.y,
                tergoRoot.transform.position.z - spacing);

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
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, UrzereFacingYawDegrees, 0f);
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
            AssignMaterial(reviewRoot.transform, material);
            ScaleToTargetHeightAndAlignToGround(reviewRoot.transform, placementRoot.transform.position.y);

            EditorUtility.SetDirty(placementRoot);
            EditorUtility.SetDirty(reviewRoot);
            return placementRoot;
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

        private static void AssignMaterial(Transform root, Material material)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Urzere prepared model contains no renderers.");
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                }
                else
                {
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }

                    renderer.sharedMaterials = materials;
                }

                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ScaleToTargetHeightAndAlignToGround(Transform root, float groundY)
        {
            var bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            if (bounds.size.y > 0.0001f)
            {
                var scaleFactor = Mathf.Clamp(UrzereTargetHeightMeters / bounds.size.y, 0.001f, 100f);
                root.localScale = Vector3.one * scaleFactor;
            }

            bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            root.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static void ConfigureInitialReviewCamera(Transform placementRoot)
        {
            var focus = FindUrzereCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var camera = FindOrCreateReviewCamera();
            var frontDirection = CalculateUrzereVisualFrontDirection(focus);
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.18f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.0f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var verticalOffset = Mathf.Clamp(bounds.extents.y * 0.16f, 0.08f, 0.24f);
            var position = lookAt + frontDirection * distance + Vector3.up * verticalOffset;

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = distance + Mathf.Max(bounds.extents.x, bounds.extents.z) + 12.00f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.12f, 1f);
            camera.orthographic = false;
            camera.fieldOfView = 34f;
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(camera.transform);

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(lookAt, camera.transform.rotation, distance, false, true);
            }
        }

        private static void ConfigureInitialPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = FindUrzereCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.18f);
            var frontDirection = CalculateUrzereVisualFrontDirection(focus);
            var startPosition = new Vector3(
                lookAt.x - frontDirection.x * ReviewPlayerFrontDistance,
                0f,
                lookAt.z - frontDirection.z * ReviewPlayerFrontDistance);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
        }

        private static void MoveExistingPlayerStartToOppositeSide(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = FindUrzereCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.18f);
            var previousPosition = player.position;
            var offset = previousPosition - lookAt;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.001f)
            {
                var frontDirection = CalculateUrzereVisualFrontDirection(focus);
                offset = frontDirection * ReviewPlayerFrontDistance;
            }

            var startPosition = new Vector3(
                lookAt.x - offset.x,
                0f,
                lookAt.z - offset.z);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);

            Debug.Log($"Urzere player start reflected around model center. Previous={previousPosition}, New={startPosition}, Center={lookAt}.");
        }

        private static void AddOrRebuildMotionSlotObjects(Transform placementRoot, Transform staticObject, Material material)
        {
            foreach (var spec in MotionSlotSpecs)
            {
                var existing = placementRoot.Find(spec.ObjectName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }
            }

            var staticBounds = CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            var spacing = Mathf.Max(staticBounds.size.x + 0.85f, MotionSlotMinimumSpacing);
            for (var i = 0; i < MotionSlotSpecs.Length; i++)
            {
                var spec = MotionSlotSpecs[i];
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
                AssignMaterial(instance.transform, material);
                EditorUtility.SetDirty(instance);
            }

            Debug.Log($"Urzere motion slots rebuilt. Count={MotionSlotSpecs.Length}, StaticZ={staticObject.position.z:0.###}, Spacing={spacing:0.###}.");
        }

        private static PuddleRemovalResult RemoveGroundPuddleGeometry(Transform placementRoot)
        {
            var result = new PuddleRemovalResult();
            foreach (var slot in EnumerateUrzereSlots(placementRoot))
            {
                RemoveGroundPuddleGeometryFromSlot(slot, ref result);
            }

            return result;
        }

        private static void RemoveGroundPuddleGeometryFromSlot(Transform slot, ref PuddleRemovalResult result)
        {
            var slotBounds = CalculateRendererBounds(slot, new Bounds(slot.position, Vector3.one));
            var coreFootprint = CalculateCoreFootprint(slot, slotBounds);
            var cutY = slotBounds.min.y + Mathf.Min(slotBounds.size.y * PuddleCutHeightRatio, 0.18f);

            foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                result.RenderersProcessed++;
                if (IsGroundPuddleOnlyRenderer(renderer, slotBounds, coreFootprint, cutY))
                {
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                    result.RenderersDisabled++;
                    continue;
                }

                var mesh = GetSharedMeshForRenderer(renderer);
                if (mesh == null || mesh.vertexCount == 0)
                {
                    continue;
                }

                var filteredMesh = BuildNoPuddleMesh(mesh, renderer.transform, coreFootprint, cutY, out var removedTriangles);
                if (removedTriangles <= 0)
                {
                    continue;
                }

                var meshPath = BuildNoPuddleMeshAssetPath(slot.name, renderer);
                SaveMeshAsset(filteredMesh, meshPath);
                AssignSharedMeshToRenderer(renderer, filteredMesh);
                result.MeshesRebuilt++;
                result.TrianglesRemoved += removedTriangles;
            }
        }

        private static Mesh BuildNoPuddleMesh(
            Mesh sourceMesh,
            Transform meshTransform,
            XzBounds coreFootprint,
            float cutY,
            out int removedTriangles)
        {
            var filteredMesh = UnityEngine.Object.Instantiate(sourceMesh);
            filteredMesh.name = sourceMesh.name + "_NoGroundPuddle";
            removedTriangles = 0;

            var vertices = sourceMesh.vertices;
            filteredMesh.subMeshCount = sourceMesh.subMeshCount;

            for (var subMesh = 0; subMesh < sourceMesh.subMeshCount; subMesh++)
            {
                var sourceTriangles = sourceMesh.GetTriangles(subMesh);
                var keptTriangles = new List<int>(sourceTriangles.Length);

                for (var i = 0; i < sourceTriangles.Length; i += 3)
                {
                    var a = sourceTriangles[i];
                    var b = sourceTriangles[i + 1];
                    var c = sourceTriangles[i + 2];
                    var worldA = meshTransform.TransformPoint(vertices[a]);
                    var worldB = meshTransform.TransformPoint(vertices[b]);
                    var worldC = meshTransform.TransformPoint(vertices[c]);
                    var centroid = (worldA + worldB + worldC) / 3f;

                    if (IsGroundPuddleTriangle(worldA, worldB, worldC, centroid, coreFootprint, cutY))
                    {
                        removedTriangles++;
                        continue;
                    }

                    keptTriangles.Add(a);
                    keptTriangles.Add(b);
                    keptTriangles.Add(c);
                }

                filteredMesh.SetTriangles(keptTriangles, subMesh);
            }

            filteredMesh.RecalculateBounds();
            return filteredMesh;
        }

        private static bool IsGroundPuddleTriangle(
            Vector3 worldA,
            Vector3 worldB,
            Vector3 worldC,
            Vector3 centroid,
            XzBounds coreFootprint,
            float cutY)
        {
            var allLow = worldA.y <= cutY && worldB.y <= cutY && worldC.y <= cutY;
            var centroidLow = centroid.y <= cutY;
            return allLow && centroidLow && !coreFootprint.ContainsWithOutset(centroid, PuddleCoreOutsetMeters);
        }

        private static bool IsGroundPuddleOnlyRenderer(Renderer renderer, Bounds slotBounds, XzBounds coreFootprint, float cutY)
        {
            var bounds = renderer.bounds;
            var lowAndThin = bounds.max.y <= cutY + 0.025f && bounds.size.y <= Mathf.Max(slotBounds.size.y * 0.12f, 0.035f);
            if (!lowAndThin)
            {
                return false;
            }

            return bounds.min.x < coreFootprint.MinX - PuddleCoreOutsetMeters ||
                   bounds.max.x > coreFootprint.MaxX + PuddleCoreOutsetMeters ||
                   bounds.min.z < coreFootprint.MinZ - PuddleCoreOutsetMeters ||
                   bounds.max.z > coreFootprint.MaxZ + PuddleCoreOutsetMeters;
        }

        private static XzBounds CalculateCoreFootprint(Transform slot, Bounds slotBounds)
        {
            var sampleY = slotBounds.min.y + slotBounds.size.y * PuddleCoreSampleHeightRatio;
            var footprint = XzBounds.Empty;

            foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var mesh = GetSharedMeshForRenderer(renderer);
                if (mesh == null)
                {
                    continue;
                }

                foreach (var vertex in mesh.vertices)
                {
                    var world = renderer.transform.TransformPoint(vertex);
                    if (world.y >= sampleY)
                    {
                        footprint.Encapsulate(world);
                    }
                }
            }

            if (!footprint.IsValid)
            {
                footprint.Encapsulate(new Vector3(slotBounds.min.x, 0f, slotBounds.min.z));
                footprint.Encapsulate(new Vector3(slotBounds.max.x, 0f, slotBounds.max.z));
            }

            return footprint;
        }

        private static List<Transform> EnumerateUrzereSlots(Transform placementRoot)
        {
            var slots = new List<Transform>();
            slots.Add(RequireStaticReviewObject(placementRoot));
            foreach (var spec in MotionSlotSpecs)
            {
                var slot = placementRoot.Find(spec.ObjectName);
                if (slot != null)
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        private static PuddleRemovalResult RemoveOuterFootPlatformGeometry(Transform placementRoot)
        {
            var result = new PuddleRemovalResult();
            foreach (var slot in EnumerateUrzereSlots(placementRoot))
            {
                RemoveOuterFootPlatformGeometryFromSlot(slot, ref result);
            }

            return result;
        }

        private static void RemoveOuterFootPlatformGeometryFromSlot(Transform slot, ref PuddleRemovalResult result)
        {
            var slotBounds = CalculateRendererBounds(slot, new Bounds(slot.position, Vector3.one));
            var bodyFootprint = CalculateBodyFootprint(slot, slotBounds);
            var cutY = slotBounds.min.y + Mathf.Min(slotBounds.size.y * OuterFootPlatformCutHeightRatio, 0.38f);

            foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                result.RenderersProcessed++;
                var mesh = GetSharedMeshForRenderer(renderer);
                if (mesh == null || mesh.vertexCount == 0)
                {
                    continue;
                }

                var filteredMesh = BuildNoOuterFootPlatformMesh(mesh, renderer.transform, bodyFootprint, cutY, out var removedTriangles);
                if (removedTriangles <= 0)
                {
                    continue;
                }

                var meshPath = BuildNoOuterFootPlatformMeshAssetPath(slot.name, renderer);
                SaveMeshAsset(filteredMesh, meshPath);
                AssignSharedMeshToRenderer(renderer, filteredMesh);
                result.MeshesRebuilt++;
                result.TrianglesRemoved += removedTriangles;

                Debug.Log(
                    $"UrzereOuterFootPlatform Slot={slot.name} BodyFootprint=({bodyFootprint.MinX:0.###},{bodyFootprint.MaxX:0.###},{bodyFootprint.MinZ:0.###},{bodyFootprint.MaxZ:0.###}) CutY={cutY:0.###} RemovedTriangles={removedTriangles}");
            }
        }

        private static Mesh BuildNoOuterFootPlatformMesh(
            Mesh sourceMesh,
            Transform meshTransform,
            XzBounds bodyFootprint,
            float cutY,
            out int removedTriangles)
        {
            var filteredMesh = UnityEngine.Object.Instantiate(sourceMesh);
            filteredMesh.name = sourceMesh.name + "_NoOuterFootPlatform";
            removedTriangles = 0;

            var vertices = sourceMesh.vertices;
            filteredMesh.subMeshCount = sourceMesh.subMeshCount;

            for (var subMesh = 0; subMesh < sourceMesh.subMeshCount; subMesh++)
            {
                var sourceTriangles = sourceMesh.GetTriangles(subMesh);
                var keptTriangles = new List<int>(sourceTriangles.Length);

                for (var i = 0; i < sourceTriangles.Length; i += 3)
                {
                    var a = sourceTriangles[i];
                    var b = sourceTriangles[i + 1];
                    var c = sourceTriangles[i + 2];
                    var worldA = meshTransform.TransformPoint(vertices[a]);
                    var worldB = meshTransform.TransformPoint(vertices[b]);
                    var worldC = meshTransform.TransformPoint(vertices[c]);
                    var centroid = (worldA + worldB + worldC) / 3f;

                    if (IsOuterFootPlatformTriangle(worldA, worldB, worldC, centroid, bodyFootprint, cutY))
                    {
                        removedTriangles++;
                        continue;
                    }

                    keptTriangles.Add(a);
                    keptTriangles.Add(b);
                    keptTriangles.Add(c);
                }

                filteredMesh.SetTriangles(keptTriangles, subMesh);
            }

            filteredMesh.RecalculateBounds();
            return filteredMesh;
        }

        private static bool IsOuterFootPlatformTriangle(
            Vector3 worldA,
            Vector3 worldB,
            Vector3 worldC,
            Vector3 centroid,
            XzBounds bodyFootprint,
            float cutY)
        {
            var allLow = worldA.y <= cutY && worldB.y <= cutY && worldC.y <= cutY;
            if (!allLow || centroid.y > cutY)
            {
                return false;
            }

            return !bodyFootprint.ContainsWithOutset(centroid, BodyFootprintOutsetMeters);
        }

        private static XzBounds CalculateBodyFootprint(Transform slot, Bounds slotBounds)
        {
            var minY = slotBounds.min.y + slotBounds.size.y * BodyFootprintBandMinRatio;
            var maxY = slotBounds.min.y + slotBounds.size.y * BodyFootprintBandMaxRatio;
            var xs = new List<float>(512);
            var zs = new List<float>(512);

            foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var mesh = GetSharedMeshForRenderer(renderer);
                if (mesh == null)
                {
                    continue;
                }

                foreach (var vertex in mesh.vertices)
                {
                    var world = renderer.transform.TransformPoint(vertex);
                    if (world.y < minY || world.y > maxY)
                    {
                        continue;
                    }

                    xs.Add(world.x);
                    zs.Add(world.z);
                }
            }

            if (xs.Count < 16 || zs.Count < 16)
            {
                return CalculateCoreFootprint(slot, slotBounds);
            }

            xs.Sort();
            zs.Sort();
            return new XzBounds
            {
                MinX = Percentile(xs, BodyFootprintPercentileMargin),
                MaxX = Percentile(xs, 1f - BodyFootprintPercentileMargin),
                MinZ = Percentile(zs, BodyFootprintPercentileMargin),
                MaxZ = Percentile(zs, 1f - BodyFootprintPercentileMargin),
                IsValid = true
            };
        }

        private static float Percentile(IReadOnlyList<float> sortedValues, float percentile)
        {
            if (sortedValues.Count == 0)
            {
                return 0f;
            }

            var index = Mathf.Clamp(Mathf.RoundToInt((sortedValues.Count - 1) * percentile), 0, sortedValues.Count - 1);
            return sortedValues[index];
        }

        private static AnimationClip EnsureIdleBreathingClip(Transform idleSlot)
        {
            var model = idleSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_02_Idle.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathingClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = IdleBreathingClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, IdleBreathingClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var modelPath = AnimationUtility.CalculateTransformPath(model, idleSlot);
            SetCurve(clip, modelPath, "m_LocalScale.x", 1.000f, 1.022f, 0.996f);
            SetCurve(clip, modelPath, "m_LocalScale.y", 1.000f, 0.988f, 1.010f);
            SetCurve(clip, modelPath, "m_LocalScale.z", 1.000f, 1.026f, 0.996f);
            SetPositionYCurve(clip, modelPath, 0.000f, 0.006f, -0.002f);

            AddRendererSurfacePulseCurves(clip, idleSlot, model);

            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(IdleBreathingClipPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathingClipPath);
        }

        private static RuntimeAnimatorController EnsureIdleBreathingController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleBreathingControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(IdleBreathingControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(IdleBreathingControllerPath);
            var state = controller.layers[0].stateMachine.AddState(IdleBreathingClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            controller.layers[0].stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(IdleBreathingControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleBreathingControllerPath);
        }

        private static void ConfigureIdleSlotAnimator(Transform idleSlot, RuntimeAnimatorController controller)
        {
            DisableImportedAnimationPlayback(idleSlot);

            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = idleSlot.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.fireEvents = false;
            animator.keepAnimatorStateOnDisable = false;

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(idleSlot);
        }

        private static void AddRendererSurfacePulseCurves(AnimationClip clip, Transform idleSlot, Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var added = 0;
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.transform == model)
                {
                    continue;
                }

                var path = AnimationUtility.CalculateTransformPath(renderer.transform, idleSlot);
                var phaseOffset = added % 2 == 0 ? 0.002f : -0.002f;
                SetPositionYCurve(clip, path, 0.000f, 0.0035f + phaseOffset, -0.0015f);
                SetCurve(clip, path, "m_LocalScale.x", 1.000f, 1.006f, 0.998f);
                SetCurve(clip, path, "m_LocalScale.z", 1.000f, 1.007f, 0.998f);

                added++;
                if (added >= 5)
                {
                    break;
                }
            }
        }

        private static void SetCurve(AnimationClip clip, string path, string propertyName, float neutral, float inhale, float exhale)
        {
            var curve = CreateBreathingCurve(neutral, inhale, exhale);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName), curve);
        }

        private static void SetPositionYCurve(AnimationClip clip, string path, float neutral, float inhale, float exhale)
        {
            var curve = CreateBreathingCurve(neutral, inhale, exhale);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"), curve);
        }

        private static AnimationCurve CreateBreathingCurve(float neutral, float inhale, float exhale)
        {
            var half = IdleBreathingDurationSeconds * 0.5f;
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutral),
                new Keyframe(half * 0.52f, inhale),
                new Keyframe(half, neutral),
                new Keyframe(half + half * 0.48f, exhale),
                new Keyframe(IdleBreathingDurationSeconds, neutral));

            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }

            return curve;
        }

        private static AnimationClip EnsureMoveBodyLiftWheelRollClip(Transform moveSlot)
        {
            var renderer = EnsureMoveBodyLiftWheelRollMesh(moveSlot, out var meshInfo);

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveBodyLiftWheelRollClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = MoveBodyLiftWheelRollClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, MoveBodyLiftWheelRollClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, moveSlot);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + MoveBodyLiftBlendShapeName),
                CreateMoveBodyLiftBlendShapeCurve());
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + MoveWheelRollABlendShapeName),
                CreateMoveWheelRollBlendShapeCurve(0.00f));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + MoveWheelRollBBlendShapeName),
                CreateMoveWheelRollBlendShapeCurve(0.26f));

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(MoveBodyLiftWheelRollClipPath, ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"UrzereMoveBodyLiftWheelRollClip Mesh={renderer.sharedMesh.name}, BodyVertices={meshInfo.BodyVertexCount}, WheelVertices={meshInfo.WheelVertexCount}.");
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveBodyLiftWheelRollClipPath);
        }

        private static RuntimeAnimatorController EnsureMoveBodyLiftWheelRollController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MoveBodyLiftWheelRollControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(MoveBodyLiftWheelRollControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(MoveBodyLiftWheelRollControllerPath);
            var state = controller.layers[0].stateMachine.AddState(MoveBodyLiftWheelRollClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            controller.layers[0].stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(MoveBodyLiftWheelRollControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MoveBodyLiftWheelRollControllerPath);
        }

        private static void ConfigureMoveSlotAnimator(Transform moveSlot, RuntimeAnimatorController controller)
        {
            DisableImportedAnimationPlayback(moveSlot);

            var animator = moveSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = moveSlot.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.fireEvents = false;
            animator.keepAnimatorStateOnDisable = false;

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(moveSlot);
        }

        private static bool RemoveRootAnimator(Transform moveSlot)
        {
            var animator = moveSlot.GetComponent<Animator>();
            if (animator == null)
            {
                return false;
            }

            animator.runtimeAnimatorController = null;
            animator.enabled = false;
            EditorUtility.SetDirty(moveSlot);
            UnityEngine.Object.DestroyImmediate(animator);
            return true;
        }

        private static bool RemoveMoveWheelOnlyVisualRoot(Transform moveSlot)
        {
            var visualRoot = moveSlot.Find(MoveWheelOnlyVisualRootName);
            if (visualRoot == null)
            {
                return false;
            }

            UnityEngine.Object.DestroyImmediate(visualRoot.gameObject);
            EditorUtility.SetDirty(moveSlot);
            return true;
        }

        private static void ResetMoveSlotBlendShapeWeights(Transform moveSlot)
        {
            foreach (var renderer in moveSlot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                ResetAllBlendShapeWeights(renderer);
            }
        }

        private static void ValidateMoveAnimationRemoved(Transform moveSlot)
        {
            var rootAnimator = moveSlot.GetComponent<Animator>();
            if (rootAnimator != null && rootAnimator.runtimeAnimatorController != null)
            {
                throw new InvalidOperationException("Urzere_03_Move still has an AnimatorController assigned.");
            }

            if (moveSlot.Find(MoveWheelOnlyVisualRootName) != null)
            {
                throw new InvalidOperationException($"Urzere_03_Move still contains {MoveWheelOnlyVisualRootName}.");
            }

            foreach (var animator in moveSlot.GetComponentsInChildren<Animator>(true))
            {
                if (animator.enabled && animator.runtimeAnimatorController != null)
                {
                    throw new InvalidOperationException($"Urzere_03_Move still has an enabled child Animator with a controller: {animator.name}.");
                }
            }

            foreach (var animation in moveSlot.GetComponentsInChildren<Animation>(true))
            {
                if (animation.enabled)
                {
                    throw new InvalidOperationException($"Urzere_03_Move still has enabled legacy Animation playback: {animation.name}.");
                }
            }
        }

        private static SkinnedMeshRenderer EnsureDeathRenderer(Transform deathSlot)
        {
            var model = deathSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_07_Death.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null || renderer.sharedMesh == null)
            {
                throw new InvalidOperationException("Urzere_07_Death needs a SkinnedMeshRenderer with a mesh.");
            }

            ResetDeathBlendShapeWeight(renderer);
            EditorUtility.SetDirty(renderer);
            return renderer;
        }

        private static SkinnedMeshRenderer EnsureDeathAnimationMesh(Transform deathSlot, out DeathMeshInfo meshInfo)
        {
            var model = deathSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_07_Death.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
            {
                throw new InvalidOperationException("Urzere_07_Death needs a SkinnedMeshRenderer for right-rear collapse animation.");
            }

            var sourceMesh = renderer.sharedMesh;
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("Urzere_07_Death SkinnedMeshRenderer has no shared mesh.");
            }

            var animatedMesh = UnityEngine.Object.Instantiate(sourceMesh);
            animatedMesh.name = sourceMesh.name + "_DeathRightRearCollapseBlendShape";
            animatedMesh.ClearBlendShapes();
            meshInfo = AddDeathRightRearCollapseBlendShape(animatedMesh);

            var meshPath = BuildDeathMeshAssetPath(deathSlot.name, renderer);
            SaveMeshAsset(animatedMesh, meshPath);
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (savedMesh == null)
            {
                throw new InvalidOperationException($"Failed to save Urzere death BlendShape mesh at {meshPath}.");
            }

            renderer.sharedMesh = savedMesh;
            ResetDeathBlendShapeWeight(renderer);
            EditorUtility.SetDirty(renderer);
            return renderer;
        }

        private static DeathMeshInfo AddDeathRightRearCollapseBlendShape(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                throw new InvalidOperationException("Urzere_07_Death mesh has no vertices for death BlendShape creation.");
            }

            var collapseDeltas = new Vector3[vertices.Length];
            var zeroNormals = new Vector3[vertices.Length];
            var zeroTangents = new Vector3[vertices.Length];
            var bounds = mesh.bounds;
            var affectedCount = 0;
            var highVertexDeltaCount = 0;
            var maxCollapseDelta = 0f;

            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var weight = CalculateDeathRightRearCollapseWeight(vertex, bounds);
                if (weight <= 0.001f)
                {
                    continue;
                }

                var normalizedX = Mathf.Clamp((vertex.x - bounds.center.x) / Mathf.Max(bounds.extents.x, 0.001f), -1f, 1f);
                var normalizedZ = Mathf.Clamp((vertex.z - bounds.center.z) / Mathf.Max(bounds.extents.z, 0.001f), -1f, 1f);
                var sideSlide = Mathf.Max(normalizedX, 0f) * 0.020f * weight;
                var rearSlide = Mathf.Max(-normalizedZ, 0f) * -0.018f * weight;
                var yDelta = -DeathRightRearCollapseLocalOffset * weight;

                collapseDeltas[i] = new Vector3(sideSlide, yDelta, rearSlide);
                affectedCount++;
                maxCollapseDelta = Mathf.Max(maxCollapseDelta, Mathf.Abs(yDelta));

                var normalizedY = Mathf.Clamp01((vertex.y - bounds.min.y) / Mathf.Max(bounds.size.y, 0.001f));
                if (normalizedY > 0.78f)
                {
                    highVertexDeltaCount++;
                }
            }

            if (affectedCount == 0)
            {
                throw new InvalidOperationException("Urzere_07_Death mesh produced no right-rear vertices for collapse BlendShape.");
            }

            if (highVertexDeltaCount > 0)
            {
                throw new InvalidOperationException($"Urzere_07_Death collapse BlendShape affected {highVertexDeltaCount} upper column vertices.");
            }

            mesh.AddBlendShapeFrame(DeathRightRearCollapseBlendShapeName, 100f, collapseDeltas, zeroNormals, zeroTangents);
            EditorUtility.SetDirty(mesh);

            return new DeathMeshInfo
            {
                RightRearVertexCount = affectedCount,
                HighVertexDeltaCount = highVertexDeltaCount,
                MaxCollapseDelta = maxCollapseDelta
            };
        }

        private static float CalculateDeathRightRearCollapseWeight(Vector3 vertex, Bounds bounds)
        {
            var normalizedX = Mathf.Clamp((vertex.x - bounds.center.x) / Mathf.Max(bounds.extents.x, 0.001f), -1f, 1f);
            var normalizedZ = Mathf.Clamp((vertex.z - bounds.center.z) / Mathf.Max(bounds.extents.z, 0.001f), -1f, 1f);
            var normalizedY = Mathf.Clamp01((vertex.y - bounds.min.y) / Mathf.Max(bounds.size.y, 0.001f));

            var rightWeight = Smooth01(Mathf.InverseLerp(-0.08f, 0.78f, normalizedX));
            var rearWeight = Smooth01(1f - Mathf.InverseLerp(-0.25f, 0.65f, normalizedZ));
            var lowBodyWeight = Smooth01(1f - Mathf.InverseLerp(0.12f, 0.72f, normalizedY));
            var wheelWeight = Smooth01(1f - Mathf.InverseLerp(0.16f, 0.38f, normalizedY));
            return Mathf.Clamp01(rightWeight * rearWeight * Mathf.Max(lowBodyWeight * 0.58f, wheelWeight));
        }

        private static void ResetDeathBlendShapeWeight(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            SetBlendShapeWeightIfPresent(renderer, mesh, DeathRightRearCollapseBlendShapeName, 0f);
        }

        private static AnimationClip EnsureDeathAnimationClip(Transform deathSlot, SkinnedMeshRenderer renderer)
        {
            var model = deathSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_07_Death.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = DeathClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, DeathClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var modelPath = AnimationUtility.CalculateTransformPath(model, deathSlot);
            var neutralEuler = model.localEulerAngles;
            SetTransformCurve(clip, modelPath, "m_LocalPosition.y", CreateDeathModelPositionYCurve(model.localPosition.y));
            SetTransformCurve(clip, modelPath, "localEulerAnglesRaw.x", CreateDeathTiltCurve(NormalizeEulerDegrees(neutralEuler.x), DeathRightRearTiltXDegrees));
            SetTransformCurve(clip, modelPath, "localEulerAnglesRaw.z", CreateDeathTiltCurve(NormalizeEulerDegrees(neutralEuler.z), DeathRightRearTiltZDegrees));

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(DeathClipPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipPath);
        }

        private static AnimationCurve CreateDeathTiltCurve(float neutral, float finalDelta)
        {
            var final = neutral + finalDelta;
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutral),
                new Keyframe(0.16f, neutral),
                new Keyframe(0.48f, neutral + finalDelta * 0.72f),
                new Keyframe(0.78f, final),
                new Keyframe(DeathSettleSeconds, final),
                new Keyframe(DeathDurationSeconds, final));
            SetAutoTangents(curve);
            SetFinalHoldTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateDeathModelPositionYCurve(float neutral)
        {
            var final = neutral + DeathBodySettleLocalYOffset;
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutral),
                new Keyframe(0.18f, neutral),
                new Keyframe(0.70f, neutral + DeathBodySettleLocalYOffset * 0.55f),
                new Keyframe(DeathSettleSeconds, final),
                new Keyframe(DeathDurationSeconds, final));
            SetAutoTangents(curve);
            SetFinalHoldTangents(curve);
            return curve;
        }

        private static float NormalizeEulerDegrees(float degrees)
        {
            return Mathf.DeltaAngle(0f, degrees);
        }

        private static void SetFinalHoldTangents(AnimationCurve curve)
        {
            if (curve.length < 2)
            {
                return;
            }

            var last = curve.length - 1;
            var beforeLast = curve.length - 2;
            AnimationUtility.SetKeyLeftTangentMode(curve, last, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, beforeLast, AnimationUtility.TangentMode.Constant);
        }

        private static RuntimeAnimatorController EnsureDeathController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DeathControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(DeathControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(DeathControllerPath);
            var state = controller.layers[0].stateMachine.AddState(DeathClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            controller.layers[0].stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(DeathControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DeathControllerPath);
        }

        private static AnimationClip EnsureMoveWheelOnlyClip(Transform moveSlot)
        {
            var visuals = EnsureMoveWheelOnlyVisuals(moveSlot);

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveWheelOnlyClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = MoveWheelOnlyClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, MoveWheelOnlyClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var leftWheelPath = AnimationUtility.CalculateTransformPath(visuals.LeftWheel, moveSlot);
            var rightWheelPath = AnimationUtility.CalculateTransformPath(visuals.RightWheel, moveSlot);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(leftWheelPath, typeof(Transform), "localEulerAnglesRaw.z"),
                CreateMoveWheelOnlyRotationCurve(1f));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(rightWheelPath, typeof(Transform), "localEulerAnglesRaw.z"),
                CreateMoveWheelOnlyRotationCurve(1f));

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(MoveWheelOnlyClipPath, ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"UrzereMoveWheelOnlyClip WheelVisuals={MoveWheelOnlyLeftWheelName},{MoveWheelOnlyRightWheelName}, RotationDegrees={MoveWheelOnlyRotationDegrees:0.#}.");
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveWheelOnlyClipPath);
        }

        private static RuntimeAnimatorController EnsureMoveWheelOnlyController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MoveWheelOnlyControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(MoveWheelOnlyControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(MoveWheelOnlyControllerPath);
            var state = controller.layers[0].stateMachine.AddState(MoveWheelOnlyClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            controller.layers[0].stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(MoveWheelOnlyControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MoveWheelOnlyControllerPath);
        }

        private static SeedEmitVisuals EnsureSeedEmitVisuals(Transform seedSlot)
        {
            var model = seedSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_05_Seed_Emit_Buff_Pulse.");
            }

            var existingRoot = seedSlot.Find(SeedEmitVisualRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot.gameObject);
            }

            var seedModelAsset = LoadSeedModelAsset();
            var seedStaticMesh = EnsureSeedEmitSharedStaticMesh(seedModelAsset);
            var slotBounds = CalculateRendererBounds(seedSlot, new Bounds(seedSlot.position, Vector3.one));
            var emitterLocalPosition = CalculateSeedEmitterLocalPosition(seedSlot, slotBounds);
            var targetSeedHeight = Mathf.Clamp(slotBounds.size.y * 0.09f, 0.055f, 0.12f);

            var visualRootObject = new GameObject(SeedEmitVisualRootName);
            visualRootObject.transform.SetParent(seedSlot, false);
            visualRootObject.transform.localPosition = Vector3.zero;
            visualRootObject.transform.localRotation = Quaternion.identity;
            visualRootObject.transform.localScale = Vector3.one;

            var seeds = new Transform[SeedEmitSeedCount];
            for (var i = 0; i < SeedEmitSeedCount; i++)
            {
                seeds[i] = CreateSeedEmitSeedVisual(
                    visualRootObject.transform,
                    seedModelAsset,
                    seedStaticMesh,
                    emitterLocalPosition,
                    targetSeedHeight,
                    i);
            }

            EditorUtility.SetDirty(visualRootObject);
            return new SeedEmitVisuals(visualRootObject.transform, model, seeds, emitterLocalPosition);
        }

        private static GameObject LoadSeedModelAsset()
        {
            if (!File.Exists(SeedModelSourceAbsolutePath))
            {
                throw new InvalidOperationException($"Windy seed source model is missing: {SeedModelSourceAbsolutePath}.");
            }

            AssetDatabase.ImportAsset(SeedModelAssetPath, ImportAssetOptions.ForceUpdate);
            var seedModelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SeedModelAssetPath);
            if (seedModelAsset == null)
            {
                throw new InvalidOperationException(
                    $"Could not load windy seed GLB as a Unity model asset. GLB path={SeedModelAssetPath}.");
            }

            return seedModelAsset;
        }

        private static Transform CreateSeedEmitSeedVisual(
            Transform parent,
            GameObject seedModelAsset,
            Mesh seedStaticMesh,
            Vector3 emitterLocalPosition,
            float targetSeedHeight,
            int seedIndex)
        {
            var seedObject = new GameObject(SeedEmitSeedPrefix + seedIndex.ToString("00"));
            seedObject.transform.SetParent(parent, false);
            seedObject.transform.localPosition = emitterLocalPosition;
            seedObject.transform.localRotation = Quaternion.Euler(EvaluateSeedEmitEuler(seedIndex, 0f));
            seedObject.transform.localScale = Vector3.one;

            var seedModel = PrefabUtility.InstantiatePrefab(seedModelAsset) as GameObject;
            if (seedModel == null)
            {
                seedModel = UnityEngine.Object.Instantiate(seedModelAsset);
            }

            seedModel.name = SeedEmitSeedModelChildName;
            seedModel.transform.SetParent(seedObject.transform, false);
            seedModel.transform.localPosition = Vector3.zero;
            seedModel.transform.localRotation = Quaternion.identity;
            seedModel.transform.localScale = Vector3.one;
            var seedMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnityMaterialAssetPath);
            if (seedMaterial != null)
            {
                AssignMaterial(seedModel.transform, seedMaterial);
            }

            BakeSeedSkinnedRenderersToStatic(seedModel.transform, seedStaticMesh);
            EnableSeedModelRenderers(seedModel.transform);
            ScaleSeedModelToTargetHeight(seedModel.transform, targetSeedHeight);
            CenterSeedModelOnParent(seedModel.transform);
            seedObject.transform.localScale = Vector3.zero;

            EditorUtility.SetDirty(seedObject);
            EditorUtility.SetDirty(seedModel);
            return seedObject.transform;
        }

        private static Mesh EnsureSeedEmitSharedStaticMesh(GameObject seedModelAsset)
        {
            EnsureUnityFolder(UnityMeshFolder);

            var sourceRenderer = seedModelAsset.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
            {
                throw new InvalidOperationException("Windy seed model needs a SkinnedMeshRenderer source mesh for shared static seed visuals.");
            }

            var sourceMesh = sourceRenderer.sharedMesh;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(SeedEmitSharedStaticMeshPath);
            if (IsCompatibleSeedEmitStaticMesh(existing, sourceMesh))
            {
                return existing;
            }

            var staticMesh = UnityEngine.Object.Instantiate(sourceMesh);
            staticMesh.name = "Urzere_05_SeedEmit_WindySeed_StaticShared";
            staticMesh.RecalculateBounds();
            SaveMeshAsset(staticMesh, SeedEmitSharedStaticMeshPath);

            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(SeedEmitSharedStaticMeshPath);
            if (savedMesh == null)
            {
                throw new InvalidOperationException($"Failed to save Urzere seed emit shared static mesh at {SeedEmitSharedStaticMeshPath}.");
            }

            return savedMesh;
        }

        private static bool IsCompatibleSeedEmitStaticMesh(Mesh existing, Mesh source)
        {
            return existing != null &&
                   source != null &&
                   existing.vertexCount == source.vertexCount &&
                   existing.subMeshCount == source.subMeshCount;
        }

        private static void BakeSeedSkinnedRenderersToStatic(Transform seedModel, Mesh seedStaticMesh)
        {
            var skinnedRenderers = seedModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var skinnedRenderer in skinnedRenderers)
            {
                if (skinnedRenderer.sharedMesh == null)
                {
                    continue;
                }

                var staticObject = new GameObject(skinnedRenderer.gameObject.name + "_SeedEmitStatic");
                staticObject.transform.SetParent(skinnedRenderer.transform.parent, false);
                staticObject.transform.localPosition = skinnedRenderer.transform.localPosition;
                staticObject.transform.localRotation = skinnedRenderer.transform.localRotation;
                staticObject.transform.localScale = skinnedRenderer.transform.localScale;

                var meshFilter = staticObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = seedStaticMesh;

                var meshRenderer = staticObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = skinnedRenderer.sharedMaterials;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                meshRenderer.receiveShadows = true;

                skinnedRenderer.enabled = false;
                EditorUtility.SetDirty(staticObject);
                EditorUtility.SetDirty(meshFilter);
                EditorUtility.SetDirty(meshRenderer);
                EditorUtility.SetDirty(skinnedRenderer);
            }
        }

        private static void EnableSeedModelRenderers(Transform seedModel)
        {
            foreach (var renderer in seedModel.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SkinnedMeshRenderer)
                {
                    continue;
                }

                renderer.enabled = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ScaleSeedModelToTargetHeight(Transform seedModel, float targetHeight)
        {
            var bounds = CalculateRendererBounds(seedModel, new Bounds(seedModel.position, Vector3.one));
            var maxSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxSize <= 0.0001f)
            {
                seedModel.localScale = Vector3.one * 0.08f;
                return;
            }

            var scaleFactor = Mathf.Clamp(targetHeight / maxSize, 0.0001f, 100f);
            seedModel.localScale *= scaleFactor;
        }

        private static void CenterSeedModelOnParent(Transform seedModel)
        {
            var parent = seedModel.parent;
            if (parent == null)
            {
                return;
            }

            var bounds = CalculateRendererBounds(seedModel, new Bounds(seedModel.position, Vector3.one));
            seedModel.position += parent.position - bounds.center;
            EditorUtility.SetDirty(seedModel);
        }

        private static Vector3 CalculateSeedEmitterLocalPosition(Transform seedSlot, Bounds slotBounds)
        {
            var worldPosition = new Vector3(slotBounds.center.x, slotBounds.max.y - slotBounds.size.y * 0.035f, slotBounds.center.z);
            return seedSlot.InverseTransformPoint(worldPosition);
        }

        private static AnimationClip EnsureSeedEmitBuffPulseClip(Transform seedSlot, SeedEmitVisuals visuals)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SeedEmitClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = SeedEmitClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, SeedEmitClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var modelPath = AnimationUtility.CalculateTransformPath(visuals.Model, seedSlot);
            SetTransformCurve(clip, modelPath, "m_LocalScale.x", CreateSeedEmitBodyScaleXCurve(visuals.Model.localScale.x));
            SetTransformCurve(clip, modelPath, "m_LocalScale.y", CreateSeedEmitBodyScaleYCurve(visuals.Model.localScale.y));
            SetTransformCurve(clip, modelPath, "m_LocalScale.z", CreateSeedEmitBodyScaleZCurve(visuals.Model.localScale.z));
            SetTransformCurve(clip, modelPath, "m_LocalPosition.y", CreateSeedEmitBodyPositionYCurve(visuals.Model.localPosition.y));

            for (var i = 0; i < visuals.Seeds.Length; i++)
            {
                var seed = visuals.Seeds[i];
                var path = AnimationUtility.CalculateTransformPath(seed, seedSlot);
                var startLocalPosition = visuals.EmitterLocalPosition;
                SetSeedEmitPositionCurves(clip, path, startLocalPosition, i);
                SetSeedEmitScaleCurves(clip, path);
                SetSeedEmitRotationCurves(clip, path, i);
            }

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(SeedEmitClipPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(SeedEmitClipPath);
        }

        private static RuntimeAnimatorController EnsureSeedEmitBuffPulseController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SeedEmitControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(SeedEmitControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                AssetDatabase.DeleteAsset(SeedEmitControllerPath);
                controller = AnimatorController.CreateAnimatorControllerAtPath(SeedEmitControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null && string.Equals(childState.state.name, SeedEmitClipName, StringComparison.Ordinal))
                {
                    state = childState.state;
                    break;
                }
            }

            state ??= stateMachine.AddState(SeedEmitClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(SeedEmitControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SeedEmitControllerPath);
        }

        private static void SetSeedEmitPositionCurves(AnimationClip clip, string path, Vector3 startLocalPosition, int seedIndex)
        {
            var sampleTimes = new[]
            {
                0f,
                SeedEmitLaunchSeconds - 0.02f,
                SeedEmitLaunchSeconds + 0.16f,
                SeedEmitPeakSeconds,
                SeedEmitFadeSeconds,
                SeedEmitResetSeconds,
                SeedEmitDurationSeconds
            };
            SetTransformCurve(clip, path, "m_LocalPosition.x", CreateSeedEmitPositionAxisCurve(startLocalPosition, seedIndex, sampleTimes, 0));
            SetTransformCurve(clip, path, "m_LocalPosition.y", CreateSeedEmitPositionAxisCurve(startLocalPosition, seedIndex, sampleTimes, 1));
            SetTransformCurve(clip, path, "m_LocalPosition.z", CreateSeedEmitPositionAxisCurve(startLocalPosition, seedIndex, sampleTimes, 2));
        }

        private static AnimationCurve CreateSeedEmitPositionAxisCurve(Vector3 startLocalPosition, int seedIndex, float[] sampleTimes, int axis)
        {
            var keys = new Keyframe[sampleTimes.Length];
            for (var i = 0; i < sampleTimes.Length; i++)
            {
                var position = EvaluateSeedEmitLocalPosition(startLocalPosition, seedIndex, sampleTimes[i]);
                keys[i] = new Keyframe(sampleTimes[i], axis == 0 ? position.x : axis == 1 ? position.y : position.z);
            }

            var curve = new AnimationCurve(keys);
            SetLinearTangents(curve);
            return curve;
        }

        private static void SetSeedEmitScaleCurves(AnimationClip clip, string path)
        {
            var curve = CreateSeedEmitScaleCurve();
            SetTransformCurve(clip, path, "m_LocalScale.x", curve);
            SetTransformCurve(clip, path, "m_LocalScale.y", CreateSeedEmitScaleCurve());
            SetTransformCurve(clip, path, "m_LocalScale.z", CreateSeedEmitScaleCurve());
        }

        private static AnimationCurve CreateSeedEmitScaleCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(SeedEmitLaunchSeconds - 0.03f, 0f),
                new Keyframe(SeedEmitLaunchSeconds + 0.10f, 1f),
                new Keyframe(SeedEmitDurationSeconds - 0.10f, 1f),
                new Keyframe(SeedEmitDurationSeconds - 0.02f, 0f),
                new Keyframe(SeedEmitDurationSeconds, 0f));
            SetLinearTangents(curve);
            return curve;
        }

        private static void SetSeedEmitRotationCurves(AnimationClip clip, string path, int seedIndex)
        {
            var sampleTimes = new[]
            {
                0f,
                SeedEmitLaunchSeconds,
                SeedEmitPeakSeconds,
                SeedEmitFadeSeconds,
                SeedEmitResetSeconds,
                SeedEmitDurationSeconds
            };

            SetTransformCurve(clip, path, "localEulerAnglesRaw.x", CreateSeedEmitEulerAxisCurve(seedIndex, sampleTimes, 0));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.y", CreateSeedEmitEulerAxisCurve(seedIndex, sampleTimes, 1));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.z", CreateSeedEmitEulerAxisCurve(seedIndex, sampleTimes, 2));
        }

        private static AnimationCurve CreateSeedEmitEulerAxisCurve(int seedIndex, float[] sampleTimes, int axis)
        {
            var keys = new Keyframe[sampleTimes.Length];
            for (var i = 0; i < sampleTimes.Length; i++)
            {
                var euler = EvaluateSeedEmitEuler(seedIndex, sampleTimes[i]);
                keys[i] = new Keyframe(sampleTimes[i], axis == 0 ? euler.x : axis == 1 ? euler.y : euler.z);
            }

            var curve = new AnimationCurve(keys);
            SetLinearTangents(curve);
            return curve;
        }

        private static void SetTransformCurve(AnimationClip clip, string path, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName), curve);
        }

        private static Vector3 EvaluateSeedEmitLocalPosition(Vector3 startLocalPosition, int seedIndex, float time)
        {
            var start = CalculateSeedEmitStartLocalPosition(startLocalPosition, seedIndex);
            if (time < SeedEmitLaunchSeconds)
            {
                return start;
            }

            var end = CalculateSeedEmitEndLocalPosition(startLocalPosition, seedIndex);
            var progress = Mathf.Clamp01((time - SeedEmitLaunchSeconds) / Mathf.Max(SeedEmitDurationSeconds - SeedEmitLaunchSeconds, 0.001f));
            return Vector3.Lerp(start, end, progress);
        }

        private static Vector3 CalculateSeedEmitStartLocalPosition(Vector3 startLocalPosition, int seedIndex)
        {
            var direction = CalculateSeedEmitLocalDirection(seedIndex);
            var radius = Mathf.Lerp(0.018f, 0.105f, Mathf.Sqrt(SeedEmitNoise01(seedIndex, 13)));
            var heightOffset = Mathf.Lerp(-0.018f, 0.026f, SeedEmitNoise01(seedIndex, 17));
            return startLocalPosition + direction * radius + Vector3.up * heightOffset;
        }

        private static Vector3 CalculateSeedEmitEndLocalPosition(Vector3 startLocalPosition, int seedIndex)
        {
            var direction = CalculateSeedEmitLocalDirection(seedIndex);
            var radialDistance = Mathf.Lerp(3.62f, 5.50f, SeedEmitNoise01(seedIndex, 29));
            var upwardDistance = Mathf.Lerp(1.02f, 1.76f, SeedEmitNoise01(seedIndex, 31));
            return startLocalPosition + direction * radialDistance + Vector3.up * upwardDistance;
        }

        private static Vector3 CalculateSeedEmitLocalDirection(int seedIndex)
        {
            var angle = SeedEmitNoise01(seedIndex, 7) * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
        }

        private static float SeedEmitNoise01(int seedIndex, int salt)
        {
            unchecked
            {
                var value = (uint)(seedIndex + 1) * 747796405u + (uint)(salt + 31) * 2891336453u;
                value ^= value >> 16;
                value *= 2246822519u;
                value ^= value >> 13;
                value *= 3266489917u;
                value ^= value >> 16;
                return (value & 0x00FFFFFFu) / 16777216f;
            }
        }

        private static Vector3 EvaluateSeedEmitEuler(int seedIndex, float time)
        {
            var angleDegrees = SeedEmitNoise01(seedIndex, 37) * 360f;
            var spin = Mathf.Max(time - SeedEmitLaunchSeconds, 0f) / Mathf.Max(SeedEmitFadeSeconds - SeedEmitLaunchSeconds, 0.001f);
            return new Vector3(
                Mathf.Lerp(-34f, 46f, SeedEmitNoise01(seedIndex, 41)) + spin * Mathf.Lerp(310f, 780f, SeedEmitNoise01(seedIndex, 43)),
                angleDegrees + spin * Mathf.Lerp(120f, 420f, SeedEmitNoise01(seedIndex, 47)),
                Mathf.Lerp(0f, 360f, SeedEmitNoise01(seedIndex, 53)) + spin * Mathf.Lerp(360f, 940f, SeedEmitNoise01(seedIndex, 59)));
        }

        private static AnimationCurve CreateSeedEmitBodyScaleXCurve(float neutral)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, neutral),
                new Keyframe(0.18f, neutral),
                new Keyframe(0.48f, neutral * 1.055f),
                new Keyframe(0.72f, neutral * 0.985f),
                new Keyframe(1.05f, neutral),
                new Keyframe(SeedEmitDurationSeconds, neutral));
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateSeedEmitBodyScaleYCurve(float neutral)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, neutral),
                new Keyframe(0.18f, neutral),
                new Keyframe(0.48f, neutral * 0.78f),
                new Keyframe(0.72f, neutral * 1.06f),
                new Keyframe(1.05f, neutral),
                new Keyframe(SeedEmitDurationSeconds, neutral));
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateSeedEmitBodyScaleZCurve(float neutral)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, neutral),
                new Keyframe(0.18f, neutral),
                new Keyframe(0.48f, neutral * 1.065f),
                new Keyframe(0.72f, neutral * 0.985f),
                new Keyframe(1.05f, neutral),
                new Keyframe(SeedEmitDurationSeconds, neutral));
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateSeedEmitBodyPositionYCurve(float neutral)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, neutral),
                new Keyframe(0.18f, neutral),
                new Keyframe(0.48f, neutral - 0.066f),
                new Keyframe(0.72f, neutral + 0.014f),
                new Keyframe(1.05f, neutral),
                new Keyframe(SeedEmitDurationSeconds, neutral));
            SetAutoTangents(curve);
            return curve;
        }

        private static MoveWheelOnlyVisuals EnsureMoveWheelOnlyVisuals(Transform moveSlot)
        {
            var existingRoot = moveSlot.Find(MoveWheelOnlyVisualRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot.gameObject);
            }

            var model = moveSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException("Urzere_03_Move model child is missing for wheel-only visual setup.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
            {
                throw new InvalidOperationException("Urzere_03_Move needs a SkinnedMeshRenderer for wheel-only visual setup.");
            }

            AssignMoveWheelOnlyStaticMeshIfAvailable(moveSlot, renderer);
            ResetAllBlendShapeWeights(renderer);

            var wheelMesh = EnsureMoveWheelOnlyVisualMesh();
            var wheelMaterial = renderer.sharedMaterial != null
                ? renderer.sharedMaterial
                : AssetDatabase.LoadAssetAtPath<Material>(UnityMaterialAssetPath);
            if (wheelMaterial == null)
            {
                throw new InvalidOperationException($"Urzere wheel material is missing at {UnityMaterialAssetPath}.");
            }

            var bounds = CalculateRendererBounds(moveSlot, new Bounds(moveSlot.position, Vector3.one));
            var rootObject = new GameObject(MoveWheelOnlyVisualRootName);
            rootObject.transform.SetParent(moveSlot, false);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;

            var leftWheel = CreateMoveWheelOnlyVisualWheel(
                rootObject.transform,
                MoveWheelOnlyLeftWheelName,
                wheelMesh,
                wheelMaterial,
                CalculateMoveWheelOnlyLocalWheelPosition(moveSlot, bounds, -1f));
            var rightWheel = CreateMoveWheelOnlyVisualWheel(
                rootObject.transform,
                MoveWheelOnlyRightWheelName,
                wheelMesh,
                wheelMaterial,
                CalculateMoveWheelOnlyLocalWheelPosition(moveSlot, bounds, 1f));

            EditorUtility.SetDirty(rootObject);
            return new MoveWheelOnlyVisuals(rootObject.transform, leftWheel, rightWheel);
        }

        private static Transform CreateMoveWheelOnlyVisualWheel(
            Transform parent,
            string objectName,
            Mesh wheelMesh,
            Material wheelMaterial,
            Vector3 localPosition)
        {
            var wheelObject = new GameObject(objectName);
            wheelObject.transform.SetParent(parent, false);
            wheelObject.transform.localPosition = localPosition;
            wheelObject.transform.localRotation = Quaternion.identity;
            wheelObject.transform.localScale = Vector3.one;

            var filter = wheelObject.AddComponent<MeshFilter>();
            filter.sharedMesh = wheelMesh;

            var renderer = wheelObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = wheelMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;

            EditorUtility.SetDirty(filter);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(wheelObject);
            return wheelObject.transform;
        }

        private static Vector3 CalculateMoveWheelOnlyLocalWheelPosition(Transform moveSlot, Bounds bounds, float sideSign)
        {
            var frontDirection = CalculateUrzereVisualFrontDirection(moveSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = moveSlot.right;
            }

            var worldPosition = bounds.center +
                                rightDirection * (sideSign * bounds.extents.x * 0.43f) +
                                frontDirection * (bounds.extents.z * 0.54f);
            worldPosition.y = bounds.min.y + bounds.size.y * 0.175f;
            return moveSlot.InverseTransformPoint(worldPosition);
        }

        private static void AssignMoveWheelOnlyStaticMeshIfAvailable(Transform moveSlot, SkinnedMeshRenderer renderer)
        {
            var currentMesh = renderer.sharedMesh;
            if (currentMesh == null || currentMesh.name.IndexOf("WheelOnlyRollBlendShapes", StringComparison.Ordinal) < 0)
            {
                return;
            }

            var staticMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BuildNoOuterFootPlatformMeshAssetPath(moveSlot.name, renderer));
            if (staticMesh == null)
            {
                var guids = AssetDatabase.FindAssets(moveSlot.name + " NoOuterFootPlatform t:Mesh", new[] { UnityMeshFolder });
                for (var i = 0; i < guids.Length; i++)
                {
                    var candidate = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (candidate != null)
                    {
                        staticMesh = candidate;
                        break;
                    }
                }
            }

            if (staticMesh == null)
            {
                return;
            }

            renderer.sharedMesh = staticMesh;
            EditorUtility.SetDirty(renderer);
        }

        private static void ResetAllBlendShapeWeights(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                renderer.SetBlendShapeWeight(i, 0f);
            }

            EditorUtility.SetDirty(renderer);
        }

        private static Mesh EnsureMoveWheelOnlyVisualMesh()
        {
            var mesh = BuildMoveWheelOnlyVisualMesh();
            SaveMeshAsset(mesh, MoveWheelOnlyMeshPath);
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MoveWheelOnlyMeshPath);
            if (savedMesh == null)
            {
                throw new InvalidOperationException($"Failed to save Urzere rolling wheel mesh at {MoveWheelOnlyMeshPath}.");
            }

            return savedMesh;
        }

        private static Mesh BuildMoveWheelOnlyVisualMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int segments = 36;
            var radius = MoveWheelOnlyVisualRadius;
            var halfThickness = MoveWheelOnlyVisualThickness * 0.5f;

            var frontCenter = AddVertex(vertices, new Vector3(0f, 0f, -halfThickness));
            var backCenter = AddVertex(vertices, new Vector3(0f, 0f, halfThickness));
            var frontRingStart = vertices.Count;
            for (var i = 0; i < segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -halfThickness));
            }

            var backRingStart = vertices.Count;
            for (var i = 0; i < segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, halfThickness));
            }

            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                triangles.Add(frontCenter);
                triangles.Add(frontRingStart + next);
                triangles.Add(frontRingStart + i);

                triangles.Add(backCenter);
                triangles.Add(backRingStart + i);
                triangles.Add(backRingStart + next);

                triangles.Add(frontRingStart + i);
                triangles.Add(frontRingStart + next);
                triangles.Add(backRingStart + i);
                triangles.Add(backRingStart + i);
                triangles.Add(frontRingStart + next);
                triangles.Add(backRingStart + next);
            }

            AddBox(vertices, triangles, new Vector3(radius * 0.26f, 0f, -halfThickness - 0.018f), new Vector3(radius * 0.98f, radius * 0.12f, 0.030f));
            AddBox(vertices, triangles, new Vector3(-radius * 0.18f, radius * 0.17f, -halfThickness - 0.020f), new Vector3(radius * 0.22f, radius * 0.38f, 0.034f));

            var mesh = new Mesh
            {
                name = "Urzere_03_Move_RollingWheelVisual"
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int AddVertex(List<Vector3> vertices, Vector3 vertex)
        {
            vertices.Add(vertex);
            return vertices.Count - 1;
        }

        private static void AddBox(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
        {
            var half = size * 0.5f;
            var start = vertices.Count;
            vertices.Add(center + new Vector3(-half.x, -half.y, -half.z));
            vertices.Add(center + new Vector3(half.x, -half.y, -half.z));
            vertices.Add(center + new Vector3(half.x, half.y, -half.z));
            vertices.Add(center + new Vector3(-half.x, half.y, -half.z));
            vertices.Add(center + new Vector3(-half.x, -half.y, half.z));
            vertices.Add(center + new Vector3(half.x, -half.y, half.z));
            vertices.Add(center + new Vector3(half.x, half.y, half.z));
            vertices.Add(center + new Vector3(-half.x, half.y, half.z));

            AddQuad(triangles, start + 0, start + 1, start + 2, start + 3);
            AddQuad(triangles, start + 5, start + 4, start + 7, start + 6);
            AddQuad(triangles, start + 4, start + 0, start + 3, start + 7);
            AddQuad(triangles, start + 1, start + 5, start + 6, start + 2);
            AddQuad(triangles, start + 3, start + 2, start + 6, start + 7);
            AddQuad(triangles, start + 4, start + 5, start + 1, start + 0);
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        private static SkinnedMeshRenderer EnsureMoveWheelOnlyMesh(Transform moveSlot, out MoveWheelOnlyMeshInfo meshInfo)
        {
            var model = moveSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_03_Move.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
            {
                throw new InvalidOperationException("Urzere_03_Move needs a SkinnedMeshRenderer for wheel-only BlendShape animation.");
            }

            var sourceMesh = renderer.sharedMesh;
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("Urzere_03_Move SkinnedMeshRenderer has no shared mesh.");
            }

            var animatedMesh = UnityEngine.Object.Instantiate(sourceMesh);
            animatedMesh.name = sourceMesh.name + "_WheelOnlyRollBlendShapes";
            animatedMesh.ClearBlendShapes();
            meshInfo = AddMoveWheelOnlyBlendShapes(animatedMesh);

            var meshPath = BuildMoveWheelOnlyMeshAssetPath(moveSlot.name, renderer);
            SaveMeshAsset(animatedMesh, meshPath);
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (savedMesh == null)
            {
                throw new InvalidOperationException($"Failed to save Urzere wheel-only BlendShape mesh at {meshPath}.");
            }

            renderer.sharedMesh = savedMesh;
            ResetWheelOnlyBlendShapeWeights(renderer);
            EditorUtility.SetDirty(renderer);
            return renderer;
        }

        private static MoveWheelOnlyMeshInfo AddMoveWheelOnlyBlendShapes(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                throw new InvalidOperationException("Urzere_03_Move mesh has no vertices for wheel-only BlendShape creation.");
            }

            var rollADeltas = new Vector3[vertices.Length];
            var rollBDeltas = new Vector3[vertices.Length];
            var rollCDeltas = new Vector3[vertices.Length];
            var zeroNormals = new Vector3[vertices.Length];
            var zeroTangents = new Vector3[vertices.Length];
            var bounds = mesh.bounds;
            var wheelVertexCount = 0;
            var highVertexDeltaCount = 0;
            var maxAbsYDelta = 0f;

            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var wheelWeight = CalculateMoveWheelOnlyWeight(vertex, bounds);
                if (wheelWeight <= 0.001f)
                {
                    continue;
                }

                rollADeltas[i] = CalculateWheelOnlyForwardRollDelta(vertex, bounds, 0.00f, wheelWeight);
                rollBDeltas[i] = CalculateWheelOnlyForwardRollDelta(vertex, bounds, Mathf.PI * 0.74f, wheelWeight);
                rollCDeltas[i] = CalculateWheelOnlyForwardRollDelta(vertex, bounds, Mathf.PI * 1.48f, wheelWeight);
                wheelVertexCount++;

                var normalizedY = Mathf.Clamp01((vertex.y - bounds.min.y) / Mathf.Max(bounds.size.y, 0.001f));
                if (normalizedY > 0.40f &&
                    (rollADeltas[i].sqrMagnitude > 0.0000001f ||
                     rollBDeltas[i].sqrMagnitude > 0.0000001f ||
                     rollCDeltas[i].sqrMagnitude > 0.0000001f))
                {
                    highVertexDeltaCount++;
                }

                maxAbsYDelta = Mathf.Max(maxAbsYDelta, Mathf.Abs(rollADeltas[i].y), Mathf.Abs(rollBDeltas[i].y), Mathf.Abs(rollCDeltas[i].y));
            }

            if (wheelVertexCount == 0)
            {
                throw new InvalidOperationException("Urzere_03_Move mesh produced no lower wheel vertices for wheel-only BlendShapes.");
            }

            if (highVertexDeltaCount > 0)
            {
                throw new InvalidOperationException($"Urzere_03_Move wheel-only BlendShapes affected {highVertexDeltaCount} upper body vertices.");
            }

            if (maxAbsYDelta > 0.00001f)
            {
                throw new InvalidOperationException($"Urzere_03_Move wheel-only BlendShapes must not contain Y deltas. MaxAbsYDelta={maxAbsYDelta:0.######}.");
            }

            mesh.AddBlendShapeFrame(MoveWheelOnlyRollABlendShapeName, 100f, rollADeltas, zeroNormals, zeroTangents);
            mesh.AddBlendShapeFrame(MoveWheelOnlyRollBBlendShapeName, 100f, rollBDeltas, zeroNormals, zeroTangents);
            mesh.AddBlendShapeFrame(MoveWheelOnlyRollCBlendShapeName, 100f, rollCDeltas, zeroNormals, zeroTangents);
            EditorUtility.SetDirty(mesh);

            return new MoveWheelOnlyMeshInfo
            {
                WheelVertexCount = wheelVertexCount,
                HighVertexDeltaCount = highVertexDeltaCount,
                MaxAbsYDelta = maxAbsYDelta
            };
        }

        private static float CalculateMoveWheelOnlyWeight(Vector3 vertex, Bounds bounds)
        {
            var height = Mathf.Max(bounds.size.y, 0.001f);
            var normalizedY = Mathf.Clamp01((vertex.y - bounds.min.y) / height);
            return Smooth01(1f - Mathf.InverseLerp(0.18f, 0.36f, normalizedY));
        }

        private static Vector3 CalculateWheelOnlyForwardRollDelta(Vector3 vertex, Bounds bounds, float phaseOffset, float wheelWeight)
        {
            var width = Mathf.Max(bounds.size.x, 0.001f);
            var depth = Mathf.Max(bounds.size.z, 0.001f);
            var normalizedX = Mathf.Clamp((vertex.x - bounds.center.x) / (width * 0.5f), -1f, 1f);
            var normalizedZ = Mathf.Clamp01((vertex.z - bounds.min.z) / depth);
            var sideWeight = 0.64f + 0.36f * Mathf.Abs(normalizedX);
            var phase = normalizedZ * Mathf.PI * 2f + phaseOffset;
            var amplitude = MoveWheelOnlyLocalAmplitude * wheelWeight * sideWeight;

            return new Vector3(
                Mathf.Sin(phase + normalizedX * 0.7f) * amplitude * 0.75f,
                0f,
                Mathf.Cos(phase) * amplitude * 1.10f);
        }

        private static void ResetWheelOnlyBlendShapeWeights(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            SetBlendShapeWeightIfPresent(renderer, mesh, MoveWheelOnlyRollABlendShapeName, 0f);
            SetBlendShapeWeightIfPresent(renderer, mesh, MoveWheelOnlyRollBBlendShapeName, 0f);
            SetBlendShapeWeightIfPresent(renderer, mesh, MoveWheelOnlyRollCBlendShapeName, 0f);
        }

        private static SkinnedMeshRenderer EnsureMoveBodyLiftWheelRollMesh(Transform moveSlot, out MoveBlendShapeMeshInfo meshInfo)
        {
            var model = moveSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_03_Move.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
            {
                throw new InvalidOperationException("Urzere_03_Move needs a SkinnedMeshRenderer for BlendShape move animation.");
            }

            var sourceMesh = renderer.sharedMesh;
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("Urzere_03_Move SkinnedMeshRenderer has no shared mesh.");
            }

            var animatedMesh = UnityEngine.Object.Instantiate(sourceMesh);
            animatedMesh.name = sourceMesh.name + "_BodyLiftWheelRollBlendShapes";
            animatedMesh.ClearBlendShapes();
            meshInfo = AddMoveBodyLiftWheelRollBlendShapes(animatedMesh);

            var meshPath = BuildMoveBodyLiftWheelRollMeshAssetPath(moveSlot.name, renderer);
            SaveMeshAsset(animatedMesh, meshPath);
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (savedMesh == null)
            {
                throw new InvalidOperationException($"Failed to save Urzere move BlendShape mesh at {meshPath}.");
            }

            renderer.sharedMesh = savedMesh;
            ResetMoveBlendShapeWeights(renderer);
            EditorUtility.SetDirty(renderer);
            return renderer;
        }

        private static MoveBlendShapeMeshInfo AddMoveBodyLiftWheelRollBlendShapes(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                throw new InvalidOperationException("Urzere_03_Move mesh has no vertices for BlendShape creation.");
            }

            var bodyDeltas = new Vector3[vertices.Length];
            var wheelADeltas = new Vector3[vertices.Length];
            var wheelBDeltas = new Vector3[vertices.Length];
            var zeroNormals = new Vector3[vertices.Length];
            var zeroTangents = new Vector3[vertices.Length];
            var bounds = mesh.bounds;
            var bodyVertexCount = 0;
            var wheelVertexCount = 0;

            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var bodyWeight = CalculateMoveBodyLiftWeight(vertex, bounds);
                if (bodyWeight > 0.001f)
                {
                    bodyDeltas[i] = new Vector3(0f, MoveBodyLiftLocalOffset * bodyWeight, 0f);
                    bodyVertexCount++;
                }

                var wheelWeight = CalculateMoveWheelRollWeight(vertex, bounds);
                if (wheelWeight > 0.001f)
                {
                    wheelADeltas[i] = CalculateWheelRollDelta(vertex, bounds, 0.00f, wheelWeight);
                    wheelBDeltas[i] = CalculateWheelRollDelta(vertex, bounds, Mathf.PI * 0.82f, wheelWeight);
                    wheelVertexCount++;
                }
            }

            if (bodyVertexCount == 0)
            {
                throw new InvalidOperationException("Urzere_03_Move mesh produced no upper body vertices for the body lift BlendShape.");
            }

            if (wheelVertexCount == 0)
            {
                throw new InvalidOperationException("Urzere_03_Move mesh produced no lower vertices for the wheel roll BlendShapes.");
            }

            mesh.AddBlendShapeFrame(MoveBodyLiftBlendShapeName, 100f, bodyDeltas, zeroNormals, zeroTangents);
            mesh.AddBlendShapeFrame(MoveWheelRollABlendShapeName, 100f, wheelADeltas, zeroNormals, zeroTangents);
            mesh.AddBlendShapeFrame(MoveWheelRollBBlendShapeName, 100f, wheelBDeltas, zeroNormals, zeroTangents);
            EditorUtility.SetDirty(mesh);

            return new MoveBlendShapeMeshInfo
            {
                BodyVertexCount = bodyVertexCount,
                WheelVertexCount = wheelVertexCount
            };
        }

        private static float CalculateMoveBodyLiftWeight(Vector3 vertex, Bounds bounds)
        {
            var height = Mathf.Max(bounds.size.y, 0.001f);
            var normalizedY = Mathf.Clamp01((vertex.y - bounds.min.y) / height);
            return Smooth01(Mathf.InverseLerp(0.34f, 0.55f, normalizedY));
        }

        private static float CalculateMoveWheelRollWeight(Vector3 vertex, Bounds bounds)
        {
            var height = Mathf.Max(bounds.size.y, 0.001f);
            var normalizedY = Mathf.Clamp01((vertex.y - bounds.min.y) / height);
            return Smooth01(1f - Mathf.InverseLerp(0.20f, 0.40f, normalizedY));
        }

        private static Vector3 CalculateWheelRollDelta(Vector3 vertex, Bounds bounds, float phaseOffset, float wheelWeight)
        {
            var width = Mathf.Max(bounds.size.x, 0.001f);
            var depth = Mathf.Max(bounds.size.z, 0.001f);
            var normalizedX = Mathf.Clamp((vertex.x - bounds.center.x) / (width * 0.5f), -1f, 1f);
            var normalizedZ = Mathf.Clamp01((vertex.z - bounds.min.z) / depth);
            var sideWeight = 0.72f + 0.28f * Mathf.Cos(normalizedX * Mathf.PI * 0.5f);
            var phase = normalizedZ * Mathf.PI * 2f + phaseOffset;
            var amplitude = MoveWheelRollLocalAmplitude * wheelWeight * sideWeight;

            return new Vector3(
                Mathf.Sin(phase * 0.5f) * amplitude * 0.18f,
                Mathf.Sin(phase) * amplitude,
                Mathf.Cos(phase) * amplitude * 0.68f);
        }

        private static void ResetMoveBlendShapeWeights(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            SetBlendShapeWeightIfPresent(renderer, mesh, MoveBodyLiftBlendShapeName, 0f);
            SetBlendShapeWeightIfPresent(renderer, mesh, MoveWheelRollABlendShapeName, 0f);
            SetBlendShapeWeightIfPresent(renderer, mesh, MoveWheelRollBBlendShapeName, 0f);
        }

        private static void SetBlendShapeWeightIfPresent(
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            string blendShapeName,
            float weight)
        {
            var index = mesh.GetBlendShapeIndex(blendShapeName);
            if (index >= 0)
            {
                renderer.SetBlendShapeWeight(index, weight);
            }
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static MoveAnimationTargets BuildMoveAnimationTargets(Transform moveSlot)
        {
            var model = moveSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_03_Move.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
            {
                throw new InvalidOperationException("Urzere_03_Move needs a SkinnedMeshRenderer for body-lift and wheel-roll animation.");
            }

            var mesh = renderer.sharedMesh;
            var bones = renderer.bones;
            if (mesh == null || bones == null || bones.Length == 0)
            {
                throw new InvalidOperationException("Urzere_03_Move SkinnedMeshRenderer has no mesh or bone list.");
            }

            var vertices = mesh.vertices;
            var boneWeights = mesh.boneWeights;
            if (vertices == null || boneWeights == null || vertices.Length == 0 || boneWeights.Length != vertices.Length)
            {
                throw new InvalidOperationException("Urzere_03_Move mesh has no usable bone weight data.");
            }

            var bodyScores = new float[bones.Length];
            var wheelScores = new float[bones.Length];
            var bounds = mesh.bounds;
            var height = Mathf.Max(bounds.size.y, 0.001f);
            for (var i = 0; i < vertices.Length; i++)
            {
                var normalizedY = Mathf.Clamp01((vertices[i].y - bounds.min.y) / height);
                var bodyStrength = Mathf.InverseLerp(0.40f, 0.82f, normalizedY);
                var wheelStrength = 1f - Mathf.InverseLerp(0.20f, 0.42f, normalizedY);
                AddBoneRegionScore(boneWeights[i], bodyStrength, wheelStrength, bodyScores, wheelScores);
            }

            var bodyTargets = new List<Transform>();
            var wheelTargets = new List<Transform>();
            AddNamedTargets(bodyTargets, bones, renderer.rootBone, MoveBodyLiftTargetLimit, "body", "morph", "box");
            AddNamedTargets(wheelTargets, bones, renderer.rootBone, MoveWheelRollTargetLimit, "wheel", "tire", "roll");
            AddTopWeightedTargets(bodyTargets, bones, bodyScores, wheelScores, renderer.rootBone, MoveBodyLiftTargetLimit, true);
            AddTopWeightedTargets(wheelTargets, bones, wheelScores, bodyScores, renderer.rootBone, MoveWheelRollTargetLimit, true);

            if (bodyTargets.Count == 0)
            {
                AddTopWeightedTargets(bodyTargets, bones, bodyScores, wheelScores, renderer.rootBone, MoveBodyLiftTargetLimit, false);
            }

            if (wheelTargets.Count == 0)
            {
                AddTopWeightedTargets(wheelTargets, bones, wheelScores, bodyScores, renderer.rootBone, MoveWheelRollTargetLimit, false);
            }

            return new MoveAnimationTargets
            {
                BodyLiftTargets = bodyTargets,
                WheelRollTargets = wheelTargets
            };
        }

        private static void AddBoneRegionScore(
            BoneWeight boneWeight,
            float bodyStrength,
            float wheelStrength,
            float[] bodyScores,
            float[] wheelScores)
        {
            AddBoneRegionScore(boneWeight.boneIndex0, boneWeight.weight0, bodyStrength, wheelStrength, bodyScores, wheelScores);
            AddBoneRegionScore(boneWeight.boneIndex1, boneWeight.weight1, bodyStrength, wheelStrength, bodyScores, wheelScores);
            AddBoneRegionScore(boneWeight.boneIndex2, boneWeight.weight2, bodyStrength, wheelStrength, bodyScores, wheelScores);
            AddBoneRegionScore(boneWeight.boneIndex3, boneWeight.weight3, bodyStrength, wheelStrength, bodyScores, wheelScores);
        }

        private static void AddBoneRegionScore(
            int boneIndex,
            float weight,
            float bodyStrength,
            float wheelStrength,
            float[] bodyScores,
            float[] wheelScores)
        {
            if (boneIndex < 0 || boneIndex >= bodyScores.Length || weight <= 0.0001f)
            {
                return;
            }

            bodyScores[boneIndex] += weight * bodyStrength;
            wheelScores[boneIndex] += weight * wheelStrength;
        }

        private static void AddNamedTargets(
            List<Transform> targets,
            Transform[] bones,
            Transform excludedRoot,
            int limit,
            params string[] nameMarkers)
        {
            foreach (var bone in bones)
            {
                if (targets.Count >= limit)
                {
                    return;
                }

                if (!IsUsableMoveAnimationTarget(bone, excludedRoot) || !NameContainsAny(bone.name, nameMarkers))
                {
                    continue;
                }

                AddUniqueTarget(targets, bone, limit);
            }
        }

        private static void AddTopWeightedTargets(
            List<Transform> targets,
            Transform[] bones,
            float[] primaryScores,
            float[] secondaryScores,
            Transform excludedRoot,
            int limit,
            bool requirePrimaryLead)
        {
            var candidates = new List<WeightedTransform>();
            for (var i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                if (!IsUsableMoveAnimationTarget(bone, excludedRoot) || ContainsTransform(targets, bone))
                {
                    continue;
                }

                var primary = primaryScores[i];
                if (primary <= 0.0001f)
                {
                    continue;
                }

                if (requirePrimaryLead && primary <= secondaryScores[i] * 1.08f)
                {
                    continue;
                }

                candidates.Add(new WeightedTransform(bone, primary));
            }

            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
            foreach (var candidate in candidates)
            {
                if (targets.Count >= limit)
                {
                    return;
                }

                AddUniqueTarget(targets, candidate.Transform, limit);
            }
        }

        private static bool IsUsableMoveAnimationTarget(Transform target, Transform excludedRoot)
        {
            if (target == null || target == excludedRoot)
            {
                return false;
            }

            return !string.Equals(target.name, "Bone_000", StringComparison.Ordinal);
        }

        private static bool NameContainsAny(string value, params string[] markers)
        {
            foreach (var marker in markers)
            {
                if (value.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddUniqueTarget(List<Transform> targets, Transform target, int limit)
        {
            if (targets.Count >= limit || ContainsTransform(targets, target))
            {
                return;
            }

            targets.Add(target);
        }

        private static bool ContainsTransform(List<Transform> targets, Transform target)
        {
            foreach (var existing in targets)
            {
                if (existing == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static AnimationCurve CreateMoveBodyLiftBlendShapeCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, 0f),
                new Keyframe(0.22f, 52f),
                new Keyframe(0.44f, 100f),
                new Keyframe(MoveWheelRollStartSeconds, 96f),
                new Keyframe(1.36f, 78f),
                new Keyframe(2.08f, 32f),
                new Keyframe(MoveBodyLiftWheelRollDurationSeconds, 0f));

            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateMoveWheelRollBlendShapeCurve(float timeOffset)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, 0f),
                new Keyframe(MoveWheelRollStartSeconds, 0f),
                new Keyframe(0.86f + timeOffset, 100f),
                new Keyframe(1.08f + timeOffset, 0f),
                new Keyframe(1.34f + timeOffset, 96f),
                new Keyframe(1.58f + timeOffset, 0f),
                new Keyframe(1.86f + timeOffset, 92f),
                new Keyframe(2.10f + timeOffset, 0f),
                new Keyframe(MoveBodyLiftWheelRollDurationSeconds, 0f));

            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateMoveWheelOnlyBlendShapeCurve(float normalizedOffset)
        {
            var offset = MoveWheelOnlyDurationSeconds * normalizedOffset;
            var curve = new AnimationCurve(
                new Keyframe(0.00f, EvaluateWheelOnlyPulse(0.00f, offset)),
                new Keyframe(0.20f, EvaluateWheelOnlyPulse(0.20f, offset)),
                new Keyframe(0.40f, EvaluateWheelOnlyPulse(0.40f, offset)),
                new Keyframe(0.60f, EvaluateWheelOnlyPulse(0.60f, offset)),
                new Keyframe(0.80f, EvaluateWheelOnlyPulse(0.80f, offset)),
                new Keyframe(1.00f, EvaluateWheelOnlyPulse(1.00f, offset)),
                new Keyframe(1.20f, EvaluateWheelOnlyPulse(1.20f, offset)),
                new Keyframe(1.40f, EvaluateWheelOnlyPulse(1.40f, offset)),
                new Keyframe(MoveWheelOnlyDurationSeconds, EvaluateWheelOnlyPulse(MoveWheelOnlyDurationSeconds, offset)));

            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateMoveWheelOnlyRotationCurve(float directionSign)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, 0f),
                new Keyframe(MoveWheelOnlyDurationSeconds * 0.25f, MoveWheelOnlyRotationDegrees * directionSign * 0.25f),
                new Keyframe(MoveWheelOnlyDurationSeconds * 0.50f, MoveWheelOnlyRotationDegrees * directionSign * 0.50f),
                new Keyframe(MoveWheelOnlyDurationSeconds * 0.75f, MoveWheelOnlyRotationDegrees * directionSign * 0.75f),
                new Keyframe(MoveWheelOnlyDurationSeconds, MoveWheelOnlyRotationDegrees * directionSign));

            SetLinearTangents(curve);
            return curve;
        }

        private static float EvaluateWheelOnlyPulse(float time, float offset)
        {
            var cycle = Mathf.Repeat((time + offset) / MoveWheelOnlyDurationSeconds, 1f);
            var wave = Mathf.Sin(cycle * Mathf.PI * 2f);
            return Mathf.Clamp01((wave + 1f) * 0.5f) * 100f;
        }

        private static AnimationCurve CreateMoveBodyLiftCurve(float neutralY)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutralY),
                new Keyframe(0.22f, neutralY + MoveBodyLiftLocalOffset * 0.50f),
                new Keyframe(0.44f, neutralY + MoveBodyLiftLocalOffset),
                new Keyframe(MoveWheelRollStartSeconds, neutralY + MoveBodyLiftLocalOffset * 0.92f),
                new Keyframe(1.36f, neutralY + MoveBodyLiftLocalOffset * 0.62f),
                new Keyframe(2.08f, neutralY + MoveBodyLiftLocalOffset * 0.18f),
                new Keyframe(MoveBodyLiftWheelRollDurationSeconds, neutralY));

            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateMoveWheelRollCurve(float neutralX)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutralX),
                new Keyframe(MoveWheelRollStartSeconds, neutralX),
                new Keyframe(1.06f, neutralX + MoveWheelRollDegrees * 0.25f),
                new Keyframe(1.56f, neutralX + MoveWheelRollDegrees * 0.58f),
                new Keyframe(2.04f, neutralX + MoveWheelRollDegrees * 0.84f),
                new Keyframe(MoveBodyLiftWheelRollDurationSeconds, neutralX + MoveWheelRollDegrees));

            SetLinearTangents(curve);
            return curve;
        }

        private static void SetAutoTangents(AnimationCurve curve)
        {
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
        }

        private static void SetLinearTangents(AnimationCurve curve)
        {
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
        }

        private static void InspectSceneState(Transform placementRoot)
        {
            var reviewObject = placementRoot.Find(PlacementObjectName);
            if (reviewObject == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            var modelObject = reviewObject.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {PlacementObjectName}.");
            }

            var renderers = reviewObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"{PlacementObjectName} contains no renderers.");
            }

            InspectMaterialAssignments(renderers);
            InspectTargetHeight(reviewObject);
            InspectTergoLongaZPlacement(placementRoot);
            InspectReviewCamera(placementRoot);
            InspectPlayerStart(placementRoot);
            InspectMotionSlotObjectsIfPresent(placementRoot);
        }

        private static void InspectMaterialAssignments(Renderer[] renderers)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnityMaterialAssetPath);
            if (material == null)
            {
                throw new InvalidOperationException($"Urzere material is missing at {UnityMaterialAssetPath}.");
            }

            foreach (var renderer in renderers)
            {
                foreach (var assignedMaterial in renderer.sharedMaterials)
                {
                    if (assignedMaterial != material)
                    {
                        throw new InvalidOperationException($"{renderer.name} does not use the Urzere olive wax material.");
                    }
                }
            }

            if (material.HasProperty("_BaseColor") && !Approximately(material.GetColor("_BaseColor"), UrzereOliveWaxColor, 0.015f))
            {
                throw new InvalidOperationException("Urzere material base color does not match the approved olive wax color.");
            }
        }

        private static void InspectTargetHeight(Transform reviewObject)
        {
            var bounds = CalculateRendererBounds(reviewObject, new Bounds(reviewObject.position, Vector3.one));
            if (Mathf.Abs(bounds.size.y - UrzereTargetHeightMeters) > 0.08f)
            {
                throw new InvalidOperationException(
                    $"Urzere bounds height should be close to {UrzereTargetHeightMeters:0.###}m, but was {bounds.size.y:0.###}m.");
            }
        }

        private static void InspectTergoLongaZPlacement(Transform placementRoot)
        {
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var spacing = CalculateTergoLongaSpacing(tergoRoot.transform, longaRoot.transform);
            var expectedZ = tergoRoot.transform.position.z - spacing;
            if (Mathf.Abs(placementRoot.position.x - tergoRoot.transform.position.x) > 0.01f ||
                Mathf.Abs(placementRoot.position.z - expectedZ) > 0.01f)
            {
                throw new InvalidOperationException(
                    $"Urzere root position must be below Tergo on Z by Tergo-Longa spacing. ExpectedX={tergoRoot.transform.position.x:0.###}, ExpectedZ={expectedZ:0.###}, Actual={placementRoot.position}.");
            }
        }

        private static void InspectReviewCamera(Transform placementRoot)
        {
            var camera = FindReviewCamera();
            if (camera == null)
            {
                throw new InvalidOperationException("Urzere review camera is missing.");
            }

            var focus = FindUrzereCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.18f);
            var cameraToLookAt = (lookAt - camera.transform.position).normalized;
            if (Vector3.Dot(camera.transform.forward, cameraToLookAt) < 0.985f)
            {
                throw new InvalidOperationException("Urzere review camera is not facing the model front.");
            }

            var frontDirection = CalculateUrzereVisualFrontDirection(focus);
            var cameraSide = camera.transform.position - lookAt;
            cameraSide.y = 0f;
            if (cameraSide.sqrMagnitude < 0.001f || Vector3.Dot(cameraSide.normalized, frontDirection) < 0.90f)
            {
                throw new InvalidOperationException("Urzere review camera is not positioned on the model front side.");
            }
        }

        private static void InspectPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Player start transform is missing.");
            }

            var focus = FindUrzereCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.18f);
            var playerToLookAt = lookAt - player.position;
            playerToLookAt.y = 0f;
            if (playerToLookAt.sqrMagnitude < 0.001f || Vector3.Dot(player.forward, playerToLookAt.normalized) < 0.95f)
            {
                throw new InvalidOperationException("Player start transform is not facing Urzere.");
            }

            var frontDirection = CalculateUrzereVisualFrontDirection(focus);
            var playerSide = player.position - lookAt;
            playerSide.y = 0f;
            var oppositeDirection = -frontDirection;
            if (playerSide.sqrMagnitude < 0.001f || Vector3.Dot(playerSide.normalized, oppositeDirection) < 0.90f)
            {
                throw new InvalidOperationException("Player start transform is not positioned on the opposite side from the Urzere front.");
            }
        }

        private static void InspectMotionSlotObjectsIfPresent(Transform placementRoot)
        {
            var hasAnySlot = false;
            foreach (var spec in MotionSlotSpecs)
            {
                if (placementRoot.Find(spec.ObjectName) != null)
                {
                    hasAnySlot = true;
                    break;
                }
            }

            if (hasAnySlot)
            {
                InspectMotionSlotObjects(placementRoot);
            }
        }

        private static void InspectMotionSlotObjects(Transform placementRoot)
        {
            var staticObject = RequireStaticReviewObject(placementRoot);
            var staticBounds = CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            var staticZ = staticObject.position.z;
            var previousX = staticBounds.max.x;

            foreach (var spec in MotionSlotSpecs)
            {
                var slot = placementRoot.Find(spec.ObjectName);
                if (slot == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing under {PlacementRootName}.");
                }

                if (Mathf.Abs(slot.position.z - staticZ) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"{spec.ObjectName} must stay on the same Z axis as {PlacementObjectName}. StaticZ={staticZ:0.###}, SlotZ={slot.position.z:0.###}.");
                }

                var renderers = slot.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} contains no renderers.");
                }

                InspectMaterialAssignments(FilterUrzereBodyRenderers(renderers));

                var bounds = CalculateRendererBounds(slot, new Bounds(slot.position, Vector3.one));
                if (bounds.min.x <= previousX + 0.10f)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} must be listed to the right of the previous Urzere slot on the same Z axis.");
                }

                previousX = bounds.max.x;
            }
        }

        private static void InspectIdleBreathingAnimation(Transform placementRoot)
        {
            InspectMotionSlotObjects(placementRoot);

            var idleSlot = RequireMotionSlotObject(placementRoot, "Urzere_02_Idle");
            var model = idleSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_02_Idle.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathingClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Urzere idle breathing clip is missing at {IdleBreathingClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleBreathingControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Urzere idle breathing controller is missing at {IdleBreathingControllerPath}.");
            }

            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Urzere_02_Idle must have the idle breathing AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Urzere_02_Idle idle breathing Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Urzere idle breathing clip must loop.");
            }

            var modelPath = AnimationUtility.CalculateTransformPath(model, idleSlot);
            RequireCurvePeak(clip, modelPath, "m_LocalScale.x", 1.018f, "Idle breathing X expansion");
            RequireCurvePeak(clip, modelPath, "m_LocalScale.z", 1.020f, "Idle breathing Z expansion");
            RequireCurveBelow(clip, modelPath, "m_LocalScale.y", 0.992f, "Idle breathing vertical compression");
            RequireCurvePeak(clip, modelPath, "m_LocalPosition.y", 0.004f, "Idle breathing vertical lift");
            RejectRootTransformCurves(clip);
            RejectIdleControllerOnOtherSlots(placementRoot, controller);
        }

        private static void InspectMoveBodyLiftWheelRollAnimation(Transform placementRoot)
        {
            InspectMotionSlotObjects(placementRoot);

            var moveSlot = RequireMotionSlotObject(placementRoot, "Urzere_03_Move");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveBodyLiftWheelRollClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Urzere move body-lift wheel-roll clip is missing at {MoveBodyLiftWheelRollClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MoveBodyLiftWheelRollControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Urzere move body-lift wheel-roll controller is missing at {MoveBodyLiftWheelRollControllerPath}.");
            }

            var animator = moveSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Urzere_03_Move must have the move body-lift wheel-roll AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Urzere_03_Move move Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Urzere move body-lift wheel-roll clip must loop.");
            }

            var renderer = moveSlot.Find(ModelChildName)?.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
            {
                throw new InvalidOperationException("Urzere_03_Move needs a SkinnedMeshRenderer for BlendShape move validation.");
            }

            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                throw new InvalidOperationException("Urzere_03_Move SkinnedMeshRenderer has no mesh for BlendShape move validation.");
            }

            RequireBlendShape(mesh, MoveBodyLiftBlendShapeName);
            RequireBlendShape(mesh, MoveWheelRollABlendShapeName);
            RequireBlendShape(mesh, MoveWheelRollBBlendShapeName);

            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, moveSlot);
            RequireBlendShapeCurvePeakBefore(
                clip,
                rendererPath,
                MoveBodyLiftBlendShapeName,
                92f,
                MoveWheelRollStartSeconds,
                "Move body lift BlendShape phase");
            RequireBlendShapeCurveBelowBefore(
                clip,
                rendererPath,
                MoveWheelRollABlendShapeName,
                1f,
                MoveWheelRollStartSeconds - 0.01f,
                "Move wheel roll BlendShape pre-start");
            RequireBlendShapeCurvePeakAtOrAfter(
                clip,
                rendererPath,
                MoveWheelRollABlendShapeName,
                90f,
                MoveWheelRollStartSeconds + 0.10f,
                "Move wheel roll PhaseA BlendShape phase");
            RequireBlendShapeCurvePeakAtOrAfter(
                clip,
                rendererPath,
                MoveWheelRollBBlendShapeName,
                90f,
                MoveWheelRollStartSeconds + 0.20f,
                "Move wheel roll PhaseB BlendShape phase");

            RejectRootTransformCurves(clip);
            RejectControllerOnOtherSlots(placementRoot, controller, "Urzere_03_Move", "Urzere move body-lift wheel-roll controller");
            InspectIdleBreathingControllerStillAssignedIfPresent(placementRoot);

            Debug.Log(
                $"UrzereMoveBodyLiftWheelRollValidation Mesh={mesh.name}, BlendShapes={mesh.blendShapeCount}, RendererPath={rendererPath}.");
        }

        private static void InspectMoveWheelOnlyAnimation(Transform placementRoot)
        {
            InspectMotionSlotObjects(placementRoot);

            var moveSlot = RequireMotionSlotObject(placementRoot, "Urzere_03_Move");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveWheelOnlyClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Urzere move wheel-only clip is missing at {MoveWheelOnlyClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MoveWheelOnlyControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Urzere move wheel-only controller is missing at {MoveWheelOnlyControllerPath}.");
            }

            var animator = moveSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Urzere_03_Move must have the wheel-only AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Urzere_03_Move wheel-only Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Urzere wheel-only clip must loop.");
            }

            var visuals = RequireMoveWheelOnlyVisuals(moveSlot);
            var leftWheelPath = AnimationUtility.CalculateTransformPath(visuals.LeftWheel, moveSlot);
            var rightWheelPath = AnimationUtility.CalculateTransformPath(visuals.RightWheel, moveSlot);

            RequireMoveWheelOnlyRotationCurve(clip, leftWheelPath, "left rolling wheel");
            RequireMoveWheelOnlyRotationCurve(clip, rightWheelPath, "right rolling wheel");
            RejectMoveWheelOnlyNonWheelCurves(clip, leftWheelPath, rightWheelPath);
            RejectControllerOnOtherSlots(placementRoot, controller, "Urzere_03_Move", "Urzere move wheel-only controller");
            InspectIdleBreathingControllerStillAssignedIfPresent(placementRoot);

            Debug.Log(
                $"UrzereMoveWheelOnlyValidation WheelPaths={leftWheelPath},{rightWheelPath}, RotationDegrees={MoveWheelOnlyRotationDegrees:0.#}.");
        }

        private static MoveWheelOnlyVisuals RequireMoveWheelOnlyVisuals(Transform moveSlot)
        {
            var visualRoot = moveSlot.Find(MoveWheelOnlyVisualRootName);
            if (visualRoot == null)
            {
                throw new InvalidOperationException($"Urzere_03_Move is missing {MoveWheelOnlyVisualRootName}.");
            }

            var leftWheel = visualRoot.Find(MoveWheelOnlyLeftWheelName);
            var rightWheel = visualRoot.Find(MoveWheelOnlyRightWheelName);
            if (leftWheel == null || rightWheel == null)
            {
                throw new InvalidOperationException("Urzere_03_Move wheel-only visual root must contain left and right rolling wheels.");
            }

            RequireMoveWheelOnlyMeshRenderer(leftWheel);
            RequireMoveWheelOnlyMeshRenderer(rightWheel);
            return new MoveWheelOnlyVisuals(visualRoot, leftWheel, rightWheel);
        }

        private static void RequireMoveWheelOnlyMeshRenderer(Transform wheel)
        {
            var filter = wheel.GetComponent<MeshFilter>();
            var renderer = wheel.GetComponent<MeshRenderer>();
            if (filter == null || renderer == null || filter.sharedMesh == null)
            {
                throw new InvalidOperationException($"{wheel.name} must have a MeshFilter and MeshRenderer for visible rolling-wheel review.");
            }

            var expectedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MoveWheelOnlyMeshPath);
            if (expectedMesh != null && filter.sharedMesh != expectedMesh)
            {
                throw new InvalidOperationException($"{wheel.name} must use the prepared rolling-wheel visual mesh.");
            }
        }

        private static void RequireMoveWheelOnlyRotationCurve(AnimationClip clip, string path, string label)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"));
            if (curve == null)
            {
                throw new InvalidOperationException($"Urzere wheel-only clip is missing {label} rotation curve at {path}.");
            }

            var start = curve.Evaluate(0f);
            var end = curve.Evaluate(MoveWheelOnlyDurationSeconds);
            if (Mathf.Abs(end - start) < Mathf.Abs(MoveWheelOnlyRotationDegrees) * 0.80f)
            {
                throw new InvalidOperationException($"Urzere wheel-only {label} rotation is too small: {end - start:0.###} degrees.");
            }
        }

        private static void RejectMoveWheelOnlyNonWheelCurves(AnimationClip clip, string leftWheelPath, string rightWheelPath)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type == typeof(SkinnedMeshRenderer))
                {
                    throw new InvalidOperationException($"Urzere wheel-only clip must not deform the body mesh: {binding.path}/{binding.propertyName}.");
                }

                if (binding.type != typeof(Transform))
                {
                    continue;
                }

                var isWheel = string.Equals(binding.path, leftWheelPath, StringComparison.Ordinal) ||
                              string.Equals(binding.path, rightWheelPath, StringComparison.Ordinal);
                if (!isWheel || !string.Equals(binding.propertyName, "localEulerAnglesRaw.z", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Urzere wheel-only clip may only rotate wheel child transforms: {binding.path}/{binding.propertyName}.");
                }
            }
        }

        private static void InspectSeedEmitBuffPulseAnimation(Transform placementRoot)
        {
            InspectMotionSlotObjects(placementRoot);

            var seedSlot = RequireMotionSlotObject(placementRoot, "Urzere_05_Seed_Emit_Buff_Pulse");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SeedEmitClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Urzere seed emit clip is missing at {SeedEmitClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SeedEmitControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Urzere seed emit controller is missing at {SeedEmitControllerPath}.");
            }

            var animator = seedSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Urzere_05_Seed_Emit_Buff_Pulse must have the seed emit AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Urzere seed emit Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Urzere seed emit clip must loop.");
            }

            if (settings.loopBlend)
            {
                throw new InvalidOperationException("Urzere seed emit clip must not blend loop pose because it can look like seeds gather back.");
            }

            var visuals = RequireSeedEmitVisuals(seedSlot);
            RequireSeedEmitSharedStaticMesh(visuals);
            var modelPath = AnimationUtility.CalculateTransformPath(visuals.Model, seedSlot);
            RequireCurveBelow(
                clip,
                modelPath,
                "m_LocalScale.y",
                visuals.Model.localScale.y * 0.84f,
                "Seed emit body crouch compression");
            RequireCurvePeak(
                clip,
                modelPath,
                "m_LocalScale.y",
                visuals.Model.localScale.y * 1.04f,
                "Seed emit body rebound");

            for (var i = 0; i < visuals.Seeds.Length; i++)
            {
                var seed = visuals.Seeds[i];
                var seedPath = AnimationUtility.CalculateTransformPath(seed, seedSlot);
                RequireCurvePeak(clip, seedPath, "m_LocalScale.x", 0.95f, seed.name + " visible scale");
                RequireSeedEmitSpreadCurve(clip, seedPath, visuals.EmitterLocalPosition, i);
                RequireCurveDeltaAtOrAfter(
                    clip,
                    seedPath,
                    "m_LocalPosition.y",
                    visuals.EmitterLocalPosition.y,
                    0.34f,
                    SeedEmitLaunchSeconds,
                    seed.name + " upward emission");
            }

            RejectControllerOnOtherSlots(placementRoot, controller, "Urzere_05_Seed_Emit_Buff_Pulse", "Urzere seed emit controller");
            InspectIdleBreathingControllerStillAssignedIfPresent(placementRoot);
            InspectMoveSlotNoControllerIfPresent(placementRoot);

            Debug.Log(
                $"UrzereSeedEmitValidation Seeds={visuals.Seeds.Length}, EmitterLocal={visuals.EmitterLocalPosition}, Clip={SeedEmitClipName}.");
        }

        private static void RequireSeedEmitSharedStaticMesh(SeedEmitVisuals visuals)
        {
            var sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(SeedEmitSharedStaticMeshPath);
            if (sharedMesh == null)
            {
                throw new InvalidOperationException($"Urzere seed emit shared static mesh is missing at {SeedEmitSharedStaticMeshPath}.");
            }

            var meshFilterCount = 0;
            for (var i = 0; i < visuals.Seeds.Length; i++)
            {
                var filters = visuals.Seeds[i].GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length == 0)
                {
                    throw new InvalidOperationException($"{visuals.Seeds[i].name} must contain a static MeshFilter using the shared windy seed mesh.");
                }

                for (var filterIndex = 0; filterIndex < filters.Length; filterIndex++)
                {
                    var mesh = filters[filterIndex].sharedMesh;
                    if (mesh == null)
                    {
                        throw new InvalidOperationException($"{visuals.Seeds[i].name} contains a seed MeshFilter without a shared mesh.");
                    }

                    if (mesh != sharedMesh)
                    {
                        throw new InvalidOperationException(
                            $"{visuals.Seeds[i].name} must reference {SeedEmitSharedStaticMeshPath}, not per-seed mesh {mesh.name}.");
                    }

                    meshFilterCount++;
                }
            }

            if (meshFilterCount < SeedEmitSeedCount)
            {
                throw new InvalidOperationException($"Urzere seed emit expected at least {SeedEmitSeedCount} shared MeshFilters, found {meshFilterCount}.");
            }

            Debug.Log($"UrzereSeedEmitSharedMesh Path={SeedEmitSharedStaticMeshPath}, MeshFilters={meshFilterCount}.");
        }

        private static void InspectDeathAnimation(Transform placementRoot)
        {
            InspectMotionSlotObjects(placementRoot);

            var deathSlot = RequireMotionSlotObject(placementRoot, "Urzere_07_Death");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Urzere death clip is missing at {DeathClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DeathControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Urzere death controller is missing at {DeathControllerPath}.");
            }

            var animator = deathSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Urzere_07_Death must have the death AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException("Urzere death Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Urzere death clip must loop for review playback while preserving the final hold section.");
            }

            if (settings.loopBlend)
            {
                throw new InvalidOperationException("Urzere death clip must not blend loop pose.");
            }

            var model = deathSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_07_Death.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null || renderer.sharedMesh == null)
            {
                throw new InvalidOperationException("Urzere_07_Death needs a SkinnedMeshRenderer with a mesh.");
            }

            RejectDeathDeformationCurves(clip);
            var modelPath = AnimationUtility.CalculateTransformPath(model, deathSlot);
            var yCurve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(modelPath, typeof(Transform), "m_LocalPosition.y"));
            if (yCurve == null)
            {
                throw new InvalidOperationException("Urzere death model Y settle curve is missing.");
            }

            var startY = yCurve.Evaluate(0f);
            var settledY = yCurve.Evaluate(DeathSettleSeconds);
            if (settledY > startY + DeathBodySettleLocalYOffset * 0.75f)
            {
                throw new InvalidOperationException("Urzere death model must sink the whole body toward the floor.");
            }

            RequireCurveConstantAfter(yCurve, DeathSettleSeconds, "Death model movement final hold");

            var tiltXCurve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(modelPath, typeof(Transform), "localEulerAnglesRaw.x"));
            var tiltZCurve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(modelPath, typeof(Transform), "localEulerAnglesRaw.z"));
            RequireDeathTiltCurve(tiltXCurve, DeathRightRearTiltXDegrees, "right-rear X tilt");
            RequireDeathTiltCurve(tiltZCurve, DeathRightRearTiltZDegrees, "right-rear Z tilt");

            RejectControllerOnOtherSlots(placementRoot, controller, "Urzere_07_Death", "Urzere death controller");
            InspectIdleBreathingControllerStillAssignedIfPresent(placementRoot);
            InspectMoveSlotNoControllerIfPresent(placementRoot);
            InspectHitSlotNoControllerIfPresent(placementRoot);
            InspectSeedEmitControllerStillAssignedIfPresent(placementRoot);

            Debug.Log(
                $"UrzereDeathValidation Motion=RightRearTiltWholeBodySink, SinkY={settledY - startY:0.###}, TiltX={tiltXCurve.Evaluate(DeathSettleSeconds) - tiltXCurve.Evaluate(0f):0.#}, TiltZ={tiltZCurve.Evaluate(DeathSettleSeconds) - tiltZCurve.Evaluate(0f):0.#}, Clip={DeathClipName}, LoopTime={settings.loopTime}, WrapMode={clip.wrapMode}.");
        }

        private static void RejectDeathDeformationCurves(AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal) ||
                    binding.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Urzere death must use whole-body tilt/sink only, not deformation or scale curves: {binding.path}/{binding.propertyName}.");
                }
            }
        }

        private static void RequireDeathTiltCurve(AnimationCurve curve, float expectedDelta, string label)
        {
            if (curve == null)
            {
                throw new InvalidOperationException($"Urzere death is missing {label} curve.");
            }

            var start = curve.Evaluate(0f);
            var settled = curve.Evaluate(DeathSettleSeconds);
            var delta = settled - start;
            if (Mathf.Abs(delta) < Mathf.Abs(expectedDelta) * 0.70f ||
                Mathf.Sign(delta) != Mathf.Sign(expectedDelta))
            {
                throw new InvalidOperationException($"Urzere death {label} is too small or tilts in the wrong direction: {delta:0.###} degrees.");
            }

            RequireCurveConstantAfter(curve, DeathSettleSeconds, "Death " + label + " final hold");
        }

        private static SeedEmitVisuals RequireSeedEmitVisuals(Transform seedSlot)
        {
            var model = seedSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_05_Seed_Emit_Buff_Pulse.");
            }

            var visualRoot = seedSlot.Find(SeedEmitVisualRootName);
            if (visualRoot == null)
            {
                throw new InvalidOperationException($"Urzere_05_Seed_Emit_Buff_Pulse is missing {SeedEmitVisualRootName}.");
            }

            var seeds = new Transform[SeedEmitSeedCount];
            for (var i = 0; i < SeedEmitSeedCount; i++)
            {
                var seed = visualRoot.Find(SeedEmitSeedPrefix + i.ToString("00"));
                if (seed == null)
                {
                    throw new InvalidOperationException($"Urzere seed emit visual is missing {SeedEmitSeedPrefix}{i:00}.");
                }

                var seedModel = seed.Find(SeedEmitSeedModelChildName);
                if (seedModel == null || seedModel.GetComponentsInChildren<Renderer>(true).Length == 0)
                {
                    throw new InvalidOperationException($"{seed.name} must contain the windy seed model renderer.");
                }

                seeds[i] = seed;
            }

            var slotBounds = CalculateRendererBounds(model, new Bounds(seedSlot.position, Vector3.one));
            var emitterLocalPosition = CalculateSeedEmitterLocalPosition(seedSlot, slotBounds);
            return new SeedEmitVisuals(visualRoot, model, seeds, emitterLocalPosition);
        }

        private static void RequireSeedEmitSpreadCurve(AnimationClip clip, string seedPath, Vector3 startLocalPosition, int seedIndex)
        {
            var xCurve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(seedPath, typeof(Transform), "m_LocalPosition.x"));
            var zCurve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(seedPath, typeof(Transform), "m_LocalPosition.z"));
            if (xCurve == null || zCurve == null)
            {
                throw new InvalidOperationException($"Seed emit spread curves are missing: {seedPath}.");
            }

            var expectedEnd = CalculateSeedEmitEndLocalPosition(startLocalPosition, seedIndex);
            var requiredSpread = Mathf.Max(Vector2.Distance(
                new Vector2(startLocalPosition.x, startLocalPosition.z),
                new Vector2(expectedEnd.x, expectedEnd.z)) * 0.70f, 0.45f);

            var sampleTimes = new[]
            {
                SeedEmitLaunchSeconds + 0.16f,
                SeedEmitPeakSeconds,
                SeedEmitFadeSeconds,
                SeedEmitResetSeconds - 0.12f,
                SeedEmitDurationSeconds - 0.04f,
                SeedEmitDurationSeconds
            };
            var reachedRequiredSpread = false;
            var previousSpread = -1f;
            foreach (var sampleTime in sampleTimes)
            {
                var spread = Vector2.Distance(
                    new Vector2(startLocalPosition.x, startLocalPosition.z),
                    new Vector2(xCurve.Evaluate(sampleTime), zCurve.Evaluate(sampleTime)));
                if (previousSpread >= 0f && spread + 0.025f < previousSpread)
                {
                    throw new InvalidOperationException($"{seedPath} must not gather back toward the cylinder after spreading.");
                }

                if (spread >= requiredSpread)
                {
                    reachedRequiredSpread = true;
                }

                previousSpread = spread;
            }

            if (!reachedRequiredSpread)
            {
                throw new InvalidOperationException($"{seedPath} must spread outward from the cylinder top.");
            }
        }

        private static void InspectMoveSlotNoControllerIfPresent(Transform placementRoot)
        {
            var moveSlot = placementRoot.Find("Urzere_03_Move");
            if (moveSlot == null)
            {
                return;
            }

            var animator = moveSlot.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                throw new InvalidOperationException("Urzere_03_Move must remain without an AnimatorController while applying Urzere_05.");
            }
        }

        private static void CaptureSeedEmitBuffPulseReviewFrames(Transform seedSlot)
        {
            var visuals = RequireSeedEmitVisuals(seedSlot);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SeedEmitValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var modelBounds = CalculateRendererBounds(visuals.Model, new Bounds(seedSlot.position, Vector3.one));
            var savedModel = CaptureTransformSnapshot(visuals.Model);
            var savedSeeds = CaptureTransformSnapshots(visuals.Seeds);
            var cameraObject = new GameObject("UrzereSeedEmit_CaptureCamera");
            var lightObject = new GameObject("UrzereSeedEmit_CaptureLight");
            var captures = new List<Texture2D>();
            var closeupCaptures = new List<Texture2D>();
            var capturePaths = new List<string>();

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureSeedEmitCaptureCamera(camera, seedSlot, modelBounds);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.transform.rotation = Quaternion.Euler(44f, seedSlot.eulerAngles.y - 28f, 0f);

                var sampleTimes = new[] { 0.00f, 0.66f, 1.18f, 1.82f, 2.40f, 2.58f };
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    ApplySeedEmitSample(visuals, savedModel, savedSeeds, sampleTimes[i]);
                    if (Mathf.Abs(sampleTimes[i] - SeedEmitPeakSeconds) <= 0.001f)
                    {
                        LogSeedEmitSampleBounds(visuals, sampleTimes[i]);
                    }

                    var outputPath = Path.Combine(outputDirectory, $"Urzere_05_SeedEmit_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureCameraTexture(camera, 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    captures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var contactSheetPath = Path.Combine(outputDirectory, "Urzere_05_SeedEmit_ContactSheet.png");
                SaveContactSheet(captures, contactSheetPath);
                capturePaths.Add(contactSheetPath);

                ConfigureSeedEmitCloseupCamera(camera, seedSlot, modelBounds);
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    ApplySeedEmitSample(visuals, savedModel, savedSeeds, sampleTimes[i]);
                    var outputPath = Path.Combine(outputDirectory, $"Urzere_05_SeedEmit_Closeup_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureCameraTexture(camera, 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    closeupCaptures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var closeupContactSheetPath = Path.Combine(outputDirectory, "Urzere_05_SeedEmit_Closeup_ContactSheet.png");
                SaveContactSheet(closeupCaptures, closeupContactSheetPath);
                capturePaths.Add(closeupContactSheetPath);
            }
            finally
            {
                RestoreTransformSnapshot(visuals.Model, savedModel);
                RestoreTransformSnapshots(visuals.Seeds, savedSeeds);
                foreach (var capture in captures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                foreach (var capture in closeupCaptures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("UrzereSeedEmitCapture Paths=" + string.Join(";", capturePaths));
        }

        private static void LogSeedEmitSampleBounds(SeedEmitVisuals visuals, float time)
        {
            var limit = Mathf.Min(visuals.Seeds.Length, 4);
            for (var i = 0; i < limit; i++)
            {
                var seed = visuals.Seeds[i];
                var renderers = seed.GetComponentsInChildren<Renderer>(true);
                var bounds = CalculateRendererBounds(seed, new Bounds(seed.position, Vector3.zero));
                var rendererType = renderers.Length > 0 ? renderers[0].GetType().Name : "None";
                var rendererEnabled = renderers.Length > 0 && renderers[0].enabled;
                Debug.Log(
                    $"UrzereSeedEmitSample Time={time:0.###}, Seed={seed.name}, LocalPosition={seed.localPosition}, LocalScale={seed.localScale}, RendererCount={renderers.Length}, RendererType={rendererType}, RendererEnabled={rendererEnabled}, BoundsCenter={bounds.center}, BoundsSize={bounds.size}.");
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    Debug.Log(
                        $"UrzereSeedEmitSampleRenderer Seed={seed.name}, Index={rendererIndex}, Type={renderer.GetType().Name}, Enabled={renderer.enabled}, BoundsCenter={renderer.bounds.center}, BoundsSize={renderer.bounds.size}, Layer={renderer.gameObject.layer}.");
                }
            }
        }

        private static void ConfigureSeedEmitCaptureCamera(Camera camera, Transform seedSlot, Bounds bounds)
        {
            var frontDirection = CalculateUrzereVisualFrontDirection(seedSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = seedSlot.right;
            }

            var viewDirection = (frontDirection * 0.86f + rightDirection * 0.28f).normalized;
            var target = bounds.center + Vector3.up * bounds.extents.y * 1.38f;
            var distance = Mathf.Max(bounds.size.z * 4.65f, 4.20f);
            camera.transform.position = target + viewDirection * distance + Vector3.up * bounds.extents.y * 0.40f;
            camera.transform.rotation = Quaternion.LookRotation((target - camera.transform.position).normalized, Vector3.up);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 4.45f, 2.85f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void ConfigureSeedEmitCloseupCamera(Camera camera, Transform seedSlot, Bounds bounds)
        {
            var frontDirection = CalculateUrzereVisualFrontDirection(seedSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = seedSlot.right;
            }

            var viewDirection = (frontDirection * 0.55f + rightDirection * 0.78f).normalized;
            var target = bounds.center + Vector3.up * bounds.extents.y * 1.44f;
            var distance = Mathf.Max(bounds.size.z * 3.00f, 2.70f);
            camera.transform.position = target + viewDirection * distance + Vector3.up * bounds.extents.y * 0.34f;
            camera.transform.rotation = Quaternion.LookRotation((target - camera.transform.position).normalized, Vector3.up);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 2.95f, 1.90f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void ApplySeedEmitSample(
            SeedEmitVisuals visuals,
            TransformSnapshot savedModel,
            TransformSnapshot[] savedSeeds,
            float time)
        {
            visuals.Model.localPosition = new Vector3(
                savedModel.LocalPosition.x,
                CreateSeedEmitBodyPositionYCurve(savedModel.LocalPosition.y).Evaluate(time),
                savedModel.LocalPosition.z);
            visuals.Model.localScale = new Vector3(
                CreateSeedEmitBodyScaleXCurve(savedModel.LocalScale.x).Evaluate(time),
                CreateSeedEmitBodyScaleYCurve(savedModel.LocalScale.y).Evaluate(time),
                CreateSeedEmitBodyScaleZCurve(savedModel.LocalScale.z).Evaluate(time));

            for (var i = 0; i < visuals.Seeds.Length && i < savedSeeds.Length; i++)
            {
                var seed = visuals.Seeds[i];
                seed.localPosition = EvaluateSeedEmitLocalPosition(visuals.EmitterLocalPosition, i, time);
                seed.localRotation = Quaternion.Euler(EvaluateSeedEmitEuler(i, time));
                var scale = CreateSeedEmitScaleCurve().Evaluate(time);
                seed.localScale = Vector3.one * scale;
            }
        }

        private static void CaptureDeathReviewFrames(Transform deathSlot)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Urzere death clip is missing at {DeathClipPath}.");
            }

            var model = deathSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under Urzere_07_Death.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
            {
                throw new InvalidOperationException("Urzere_07_Death needs a SkinnedMeshRenderer for death capture.");
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DeathValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var modelBounds = CalculateRendererBounds(model, new Bounds(deathSlot.position, Vector3.one));
            var savedModel = CaptureTransformSnapshot(model);
            var savedWeights = CaptureBlendShapeWeights(renderer);
            var cameraObject = new GameObject("UrzereDeath_CaptureCamera");
            var lightObject = new GameObject("UrzereDeath_CaptureLight");
            var captures = new List<Texture2D>();
            var capturePaths = new List<string>();

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureDeathCaptureCamera(camera, deathSlot, modelBounds);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.transform.rotation = Quaternion.Euler(46f, deathSlot.eulerAngles.y - 34f, 0f);

                var sampleTimes = new[] { 0.00f, 0.36f, 0.72f, 1.20f, 1.80f, DeathDurationSeconds };
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    clip.SampleAnimation(deathSlot.gameObject, sampleTimes[i]);
                    var outputPath = Path.Combine(outputDirectory, $"Urzere_07_Death_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureCameraTexture(camera, 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    captures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var contactSheetPath = Path.Combine(outputDirectory, "Urzere_07_Death_ContactSheet.png");
                SaveContactSheet(captures, contactSheetPath);
                capturePaths.Add(contactSheetPath);
            }
            finally
            {
                RestoreTransformSnapshot(model, savedModel);
                RestoreBlendShapeWeights(renderer, savedWeights);
                foreach (var capture in captures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("UrzereDeathCapture Paths=" + string.Join(";", capturePaths));
        }

        private static void ConfigureDeathCaptureCamera(Camera camera, Transform deathSlot, Bounds bounds)
        {
            var frontDirection = CalculateUrzereVisualFrontDirection(deathSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = deathSlot.right;
            }

            var viewDirection = (rightDirection * 0.78f - frontDirection * 0.70f).normalized;
            var target = bounds.center + Vector3.down * bounds.extents.y * 0.06f;
            var distance = Mathf.Max(bounds.size.z * 2.25f, 2.10f);
            camera.transform.position = target + viewDirection * distance + Vector3.up * bounds.extents.y * 0.30f;
            camera.transform.rotation = Quaternion.LookRotation((target - camera.transform.position).normalized, Vector3.up);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 0.92f, 0.70f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static TransformSnapshot CaptureTransformSnapshot(Transform transform)
        {
            return new TransformSnapshot(transform.localPosition, transform.localRotation, transform.localScale);
        }

        private static TransformSnapshot[] CaptureTransformSnapshots(IReadOnlyList<Transform> transforms)
        {
            var snapshots = new TransformSnapshot[transforms.Count];
            for (var i = 0; i < transforms.Count; i++)
            {
                snapshots[i] = CaptureTransformSnapshot(transforms[i]);
            }

            return snapshots;
        }

        private static void RestoreTransformSnapshot(Transform transform, TransformSnapshot snapshot)
        {
            transform.localPosition = snapshot.LocalPosition;
            transform.localRotation = snapshot.LocalRotation;
            transform.localScale = snapshot.LocalScale;
        }

        private static void RestoreTransformSnapshots(IReadOnlyList<Transform> transforms, TransformSnapshot[] snapshots)
        {
            for (var i = 0; i < transforms.Count && i < snapshots.Length; i++)
            {
                RestoreTransformSnapshot(transforms[i], snapshots[i]);
            }
        }

        private static void CaptureMoveWheelOnlyReviewFrames(Transform moveSlot)
        {
            var renderer = moveSlot.Find(ModelChildName)?.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null || renderer.sharedMesh == null)
            {
                throw new InvalidOperationException("Urzere_03_Move needs a SkinnedMeshRenderer with a mesh for wheel-only capture.");
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", MoveWheelOnlyValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var bounds = CalculateRendererBounds(moveSlot, new Bounds(moveSlot.position, Vector3.one));
            var visuals = RequireMoveWheelOnlyVisuals(moveSlot);
            var savedRotations = CaptureMoveWheelOnlyRotations(visuals);
            var cameraObject = new GameObject("UrzereMoveWheelOnly_CaptureCamera");
            var lightObject = new GameObject("UrzereMoveWheelOnly_CaptureLight");
            var captures = new List<Texture2D>();
            var closeupCaptures = new List<Texture2D>();
            var capturePaths = new List<string>();

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureMoveWheelOnlyCaptureCamera(camera, moveSlot, bounds);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(48f, moveSlot.eulerAngles.y - 35f, 0f);

                var sampleTimes = new[] { 0.00f, 0.40f, 0.80f, 1.20f };
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    ApplyMoveWheelOnlySample(visuals, sampleTimes[i]);
                    var outputPath = Path.Combine(outputDirectory, $"Urzere_03_Move_WheelOnly_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureCameraTexture(camera, 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    captures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var contactSheetPath = Path.Combine(outputDirectory, "Urzere_03_Move_WheelOnly_ContactSheet.png");
                SaveContactSheet(captures, contactSheetPath);
                capturePaths.Add(contactSheetPath);

                ConfigureMoveWheelOnlyCloseupCamera(camera, moveSlot, bounds);
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    ApplyMoveWheelOnlySample(visuals, sampleTimes[i]);
                    var outputPath = Path.Combine(outputDirectory, $"Urzere_03_Move_WheelOnly_Closeup_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureCameraTexture(camera, 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    closeupCaptures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var closeupContactSheetPath = Path.Combine(outputDirectory, "Urzere_03_Move_WheelOnly_Closeup_ContactSheet.png");
                SaveContactSheet(closeupCaptures, closeupContactSheetPath);
                capturePaths.Add(closeupContactSheetPath);
            }
            finally
            {
                RestoreMoveWheelOnlyRotations(visuals, savedRotations);
                foreach (var capture in captures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                foreach (var capture in closeupCaptures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("UrzereMoveWheelOnlyCapture Paths=" + string.Join(";", capturePaths));
        }

        private static void ConfigureMoveWheelOnlyCaptureCamera(Camera camera, Transform moveSlot, Bounds bounds)
        {
            var frontDirection = CalculateUrzereVisualFrontDirection(moveSlot);
            var target = bounds.center + Vector3.down * bounds.extents.y * 0.14f;
            var distance = Mathf.Max(bounds.size.z * 2.55f, 2.10f);
            camera.transform.position = target + frontDirection * distance + Vector3.up * bounds.extents.y * 0.18f;
            camera.transform.rotation = Quaternion.LookRotation((target - camera.transform.position).normalized, Vector3.up);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.02f, 0.62f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void ConfigureMoveWheelOnlyCloseupCamera(Camera camera, Transform moveSlot, Bounds bounds)
        {
            var frontDirection = CalculateUrzereVisualFrontDirection(moveSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = moveSlot.right;
            }

            var viewDirection = (frontDirection * 0.72f + rightDirection * 0.58f).normalized;
            var target = bounds.center + Vector3.down * bounds.extents.y * 0.56f;
            var distance = Mathf.Max(bounds.size.z * 1.72f, 1.35f);
            camera.transform.position = target + viewDirection * distance + Vector3.up * bounds.extents.y * 0.08f;
            camera.transform.rotation = Quaternion.LookRotation((target - camera.transform.position).normalized, Vector3.up);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 0.43f, 0.27f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static float[] CaptureBlendShapeWeights(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            var weights = new float[mesh != null ? mesh.blendShapeCount : 0];
            for (var i = 0; i < weights.Length; i++)
            {
                weights[i] = renderer.GetBlendShapeWeight(i);
            }

            return weights;
        }

        private static void RestoreBlendShapeWeights(SkinnedMeshRenderer renderer, float[] weights)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            for (var i = 0; i < weights.Length && i < mesh.blendShapeCount; i++)
            {
                renderer.SetBlendShapeWeight(i, weights[i]);
            }
        }

        private static Quaternion[] CaptureMoveWheelOnlyRotations(MoveWheelOnlyVisuals visuals)
        {
            return new[]
            {
                visuals.LeftWheel.localRotation,
                visuals.RightWheel.localRotation
            };
        }

        private static void RestoreMoveWheelOnlyRotations(MoveWheelOnlyVisuals visuals, Quaternion[] rotations)
        {
            if (rotations == null || rotations.Length < 2)
            {
                return;
            }

            visuals.LeftWheel.localRotation = rotations[0];
            visuals.RightWheel.localRotation = rotations[1];
        }

        private static void ApplyMoveWheelOnlySample(MoveWheelOnlyVisuals visuals, float time)
        {
            var normalizedTime = Mathf.Repeat(time, MoveWheelOnlyDurationSeconds) / MoveWheelOnlyDurationSeconds;
            var rotation = Quaternion.Euler(0f, 0f, MoveWheelOnlyRotationDegrees * normalizedTime);
            visuals.LeftWheel.localRotation = rotation;
            visuals.RightWheel.localRotation = rotation;
        }

        private static Texture2D CaptureCameraTexture(Camera camera, int width, int height)
        {
            var previousActiveTexture = RenderTexture.active;
            var previousTargetTexture = camera.targetTexture;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply();
                return capture;
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void SaveContactSheet(IReadOnlyList<Texture2D> captures, string outputPath)
        {
            if (captures.Count == 0)
            {
                return;
            }

            var width = captures[0].width;
            var height = captures[0].height;
            var output = new Texture2D(width * captures.Count, height, TextureFormat.RGBA32, false);
            for (var i = 0; i < captures.Count; i++)
            {
                output.SetPixels(i * width, 0, width, height, captures[i].GetPixels());
            }

            output.Apply();
            File.WriteAllBytes(outputPath, output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
        }

        private static void ValidateGroundPuddleRemoved(Transform placementRoot)
        {
            foreach (var slot in EnumerateUrzereSlots(placementRoot))
            {
                var slotBounds = CalculateRendererBounds(slot, new Bounds(slot.position, Vector3.one));
                var coreFootprint = CalculateCoreFootprint(slot, slotBounds);
                var cutY = slotBounds.min.y + Mathf.Min(slotBounds.size.y * PuddleCutHeightRatio, 0.18f);

                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !renderer.enabled)
                    {
                        continue;
                    }

                    if (IsGroundPuddleOnlyRenderer(renderer, slotBounds, coreFootprint, cutY))
                    {
                        throw new InvalidOperationException($"{slot.name}/{renderer.name} still looks like a ground puddle-only renderer.");
                    }

                    var mesh = GetSharedMeshForRenderer(renderer);
                    if (mesh == null)
                    {
                        continue;
                    }

                    var vertices = mesh.vertices;
                    for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                    {
                        var triangles = mesh.GetTriangles(subMesh);
                        for (var i = 0; i < triangles.Length; i += 3)
                        {
                            var worldA = renderer.transform.TransformPoint(vertices[triangles[i]]);
                            var worldB = renderer.transform.TransformPoint(vertices[triangles[i + 1]]);
                            var worldC = renderer.transform.TransformPoint(vertices[triangles[i + 2]]);
                            var centroid = (worldA + worldB + worldC) / 3f;
                            if (IsGroundPuddleTriangle(worldA, worldB, worldC, centroid, coreFootprint, cutY))
                            {
                                throw new InvalidOperationException($"{slot.name}/{renderer.name} still contains low outside ground-puddle geometry.");
                            }
                        }
                    }
                }
            }

            InspectSceneState(placementRoot);
        }

        private static void ValidateOuterFootPlatformsRemoved(Transform placementRoot)
        {
            foreach (var slot in EnumerateUrzereSlots(placementRoot))
            {
                var slotBounds = CalculateRendererBounds(slot, new Bounds(slot.position, Vector3.one));
                var bodyFootprint = CalculateBodyFootprint(slot, slotBounds);
                var cutY = slotBounds.min.y + Mathf.Min(slotBounds.size.y * OuterFootPlatformCutHeightRatio, 0.38f);

                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || !renderer.enabled)
                    {
                        continue;
                    }

                    var mesh = GetSharedMeshForRenderer(renderer);
                    if (mesh == null)
                    {
                        continue;
                    }

                    var vertices = mesh.vertices;
                    for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                    {
                        var triangles = mesh.GetTriangles(subMesh);
                        for (var i = 0; i < triangles.Length; i += 3)
                        {
                            var worldA = renderer.transform.TransformPoint(vertices[triangles[i]]);
                            var worldB = renderer.transform.TransformPoint(vertices[triangles[i + 1]]);
                            var worldC = renderer.transform.TransformPoint(vertices[triangles[i + 2]]);
                            var centroid = (worldA + worldB + worldC) / 3f;
                            if (IsOuterFootPlatformTriangle(worldA, worldB, worldC, centroid, bodyFootprint, cutY))
                            {
                                throw new InvalidOperationException($"{slot.name}/{renderer.name} still contains low body-outside foot platform geometry.");
                            }
                        }
                    }
                }
            }

            InspectSceneState(placementRoot);
        }

        private static Renderer[] FilterUrzereBodyRenderers(Renderer[] renderers)
        {
            var filtered = new List<Renderer>(renderers.Length);
            foreach (var renderer in renderers)
            {
                if (renderer == null || IsUnderNamedAncestor(renderer.transform, SeedEmitVisualRootName))
                {
                    continue;
                }

                filtered.Add(renderer);
            }

            return filtered.ToArray();
        }

        private static bool IsUnderNamedAncestor(Transform transform, string ancestorName)
        {
            var current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, ancestorName, StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void RequireCurvePeak(AnimationClip clip, string path, string propertyName, float minimumValue, string label)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
            if (curve == null)
            {
                throw new InvalidOperationException($"{label} curve is missing: {path}/{propertyName}.");
            }

            foreach (var key in curve.keys)
            {
                if (key.value >= minimumValue)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"{label} must reach at least {minimumValue:0.###}.");
        }

        private static void RequireCurveBelow(AnimationClip clip, string path, string propertyName, float maximumValue, string label)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
            if (curve == null)
            {
                throw new InvalidOperationException($"{label} curve is missing: {path}/{propertyName}.");
            }

            foreach (var key in curve.keys)
            {
                if (key.value <= maximumValue)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"{label} must dip to {maximumValue:0.###} or lower.");
        }

        private static void RequireCurveDeltaPeakBefore(
            AnimationClip clip,
            string path,
            string propertyName,
            float neutral,
            float minimumDelta,
            float latestTime,
            string label)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
            if (curve == null)
            {
                throw new InvalidOperationException($"{label} curve is missing: {path}/{propertyName}.");
            }

            foreach (var key in curve.keys)
            {
                if (key.time <= latestTime + 0.0001f && key.value - neutral >= minimumDelta)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"{label} must rise by at least {minimumDelta:0.###} before {latestTime:0.###} seconds.");
        }

        private static void RequireCurveDeltaAtOrAfter(
            AnimationClip clip,
            string path,
            string propertyName,
            float neutral,
            float minimumAbsDelta,
            float earliestTime,
            string label)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
            if (curve == null)
            {
                throw new InvalidOperationException($"{label} curve is missing: {path}/{propertyName}.");
            }

            foreach (var key in curve.keys)
            {
                if (key.time >= earliestTime - 0.0001f && Mathf.Abs(key.value - neutral) >= minimumAbsDelta)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"{label} must change by at least {minimumAbsDelta:0.###} after {earliestTime:0.###} seconds.");
        }

        private static void RequireBlendShape(Mesh mesh, string blendShapeName)
        {
            if (mesh.GetBlendShapeIndex(blendShapeName) < 0)
            {
                throw new InvalidOperationException($"Urzere_03_Move mesh must contain BlendShape {blendShapeName}.");
            }
        }

        private static void RejectBlendShape(Mesh mesh, string blendShapeName)
        {
            if (mesh.GetBlendShapeIndex(blendShapeName) >= 0)
            {
                throw new InvalidOperationException($"Urzere_03_Move wheel-only mesh must not contain BlendShape {blendShapeName}.");
            }
        }

        private static void ValidateWheelOnlyBlendShapeDeltas(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var bounds = mesh.bounds;
            var highVertexDeltaCount = 0;
            var movedVertexCount = 0;
            var maxAbsYDelta = 0f;
            var deltaVertices = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];

            ValidateWheelOnlyBlendShapeDelta(
                mesh,
                MoveWheelOnlyRollABlendShapeName,
                vertices,
                bounds,
                deltaVertices,
                deltaNormals,
                deltaTangents,
                ref highVertexDeltaCount,
                ref movedVertexCount,
                ref maxAbsYDelta);
            ValidateWheelOnlyBlendShapeDelta(
                mesh,
                MoveWheelOnlyRollBBlendShapeName,
                vertices,
                bounds,
                deltaVertices,
                deltaNormals,
                deltaTangents,
                ref highVertexDeltaCount,
                ref movedVertexCount,
                ref maxAbsYDelta);
            ValidateWheelOnlyBlendShapeDelta(
                mesh,
                MoveWheelOnlyRollCBlendShapeName,
                vertices,
                bounds,
                deltaVertices,
                deltaNormals,
                deltaTangents,
                ref highVertexDeltaCount,
                ref movedVertexCount,
                ref maxAbsYDelta);

            if (movedVertexCount == 0)
            {
                throw new InvalidOperationException("Urzere_03_Move wheel-only BlendShapes do not move any lower wheel vertices.");
            }

            if (highVertexDeltaCount > 0)
            {
                throw new InvalidOperationException($"Urzere_03_Move wheel-only BlendShapes affected {highVertexDeltaCount} upper body or column vertices.");
            }

            if (maxAbsYDelta > 0.00001f)
            {
                throw new InvalidOperationException($"Urzere_03_Move wheel-only BlendShapes contain Y-axis movement. MaxAbsYDelta={maxAbsYDelta:0.######}.");
            }
        }

        private static void ValidateWheelOnlyBlendShapeDelta(
            Mesh mesh,
            string blendShapeName,
            Vector3[] vertices,
            Bounds bounds,
            Vector3[] deltaVertices,
            Vector3[] deltaNormals,
            Vector3[] deltaTangents,
            ref int highVertexDeltaCount,
            ref int movedVertexCount,
            ref float maxAbsYDelta)
        {
            var index = mesh.GetBlendShapeIndex(blendShapeName);
            if (index < 0)
            {
                throw new InvalidOperationException($"Urzere_03_Move wheel-only mesh is missing BlendShape {blendShapeName}.");
            }

            mesh.GetBlendShapeFrameVertices(index, 0, deltaVertices, deltaNormals, deltaTangents);
            var height = Mathf.Max(bounds.size.y, 0.001f);
            for (var i = 0; i < deltaVertices.Length; i++)
            {
                var delta = deltaVertices[i];
                if (delta.sqrMagnitude <= 0.0000001f)
                {
                    continue;
                }

                movedVertexCount++;
                maxAbsYDelta = Mathf.Max(maxAbsYDelta, Mathf.Abs(delta.y));

                var normalizedY = Mathf.Clamp01((vertices[i].y - bounds.min.y) / height);
                if (normalizedY > 0.40f)
                {
                    highVertexDeltaCount++;
                }
            }
        }

        private static void RequireBlendShapeCurvePeakBefore(
            AnimationClip clip,
            string rendererPath,
            string blendShapeName,
            float minimumValue,
            float latestTime,
            string label)
        {
            var curve = GetBlendShapeCurve(clip, rendererPath, blendShapeName, label);
            foreach (var key in curve.keys)
            {
                if (key.time <= latestTime + 0.0001f && key.value >= minimumValue)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"{label} must reach at least {minimumValue:0.###} before {latestTime:0.###} seconds.");
        }

        private static void RequireBlendShapeCurvePeakAtOrAfter(
            AnimationClip clip,
            string rendererPath,
            string blendShapeName,
            float minimumValue,
            float earliestTime,
            string label)
        {
            var curve = GetBlendShapeCurve(clip, rendererPath, blendShapeName, label);
            foreach (var key in curve.keys)
            {
                if (key.time >= earliestTime - 0.0001f && key.value >= minimumValue)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"{label} must reach at least {minimumValue:0.###} after {earliestTime:0.###} seconds.");
        }

        private static void RequireBlendShapeCurveBelowBefore(
            AnimationClip clip,
            string rendererPath,
            string blendShapeName,
            float maximumValue,
            float latestTime,
            string label)
        {
            var curve = GetBlendShapeCurve(clip, rendererPath, blendShapeName, label);
            foreach (var key in curve.keys)
            {
                if (key.time <= latestTime + 0.0001f && key.value > maximumValue)
                {
                    throw new InvalidOperationException($"{label} must stay at or below {maximumValue:0.###} before {latestTime:0.###} seconds.");
                }
            }
        }

        private static AnimationCurve GetBlendShapeCurve(
            AnimationClip clip,
            string rendererPath,
            string blendShapeName,
            string label)
        {
            var propertyName = "blendShape." + blendShapeName;
            var curve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(rendererPath, typeof(SkinnedMeshRenderer), propertyName));
            if (curve == null)
            {
                throw new InvalidOperationException($"{label} curve is missing: {rendererPath}/{propertyName}.");
            }

            return curve;
        }

        private static void RequireCurveConstantAfter(AnimationCurve curve, float startTime, string label)
        {
            var expected = curve.Evaluate(startTime);
            foreach (var key in curve.keys)
            {
                if (key.time < startTime - 0.0001f)
                {
                    continue;
                }

                if (Mathf.Abs(key.value - expected) > 0.001f)
                {
                    throw new InvalidOperationException($"{label} must stop changing after {startTime:0.###} seconds.");
                }
            }

            var durationValue = curve.Evaluate(DeathDurationSeconds);
            if (Mathf.Abs(durationValue - expected) > 0.001f)
            {
                throw new InvalidOperationException($"{label} must hold through the end of the death clip.");
            }
        }

        private static void RejectCurve(AnimationClip clip, string path, Type type, string propertyName)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName));
            if (curve == null)
            {
                return;
            }

            foreach (var key in curve.keys)
            {
                if (Mathf.Abs(key.value) > 0.0001f)
                {
                    throw new InvalidOperationException($"Curve must stay absent or zero: {path}/{propertyName}.");
                }
            }
        }

        private static void RejectRootTransformCurves(AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (string.IsNullOrEmpty(binding.path) &&
                    binding.type == typeof(Transform) &&
                    (binding.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal) ||
                     binding.propertyName.StartsWith("m_LocalRotation", StringComparison.Ordinal) ||
                     binding.propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("Urzere idle breathing must not animate the slot root position or rotation.");
                }
            }
        }

        private static void RejectAllTransformCurves(AnimationClip clip, string label)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type == typeof(Transform))
                {
                    throw new InvalidOperationException($"{label} must not animate Transform curves: {binding.path}/{binding.propertyName}.");
                }
            }
        }

        private static void RejectIdleControllerOnOtherSlots(Transform placementRoot, RuntimeAnimatorController controller)
        {
            foreach (var spec in MotionSlotSpecs)
            {
                if (string.Equals(spec.ObjectName, "Urzere_02_Idle", StringComparison.Ordinal))
                {
                    continue;
                }

                var slot = placementRoot.Find(spec.ObjectName);
                if (slot == null)
                {
                    continue;
                }

                var animator = slot.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController == controller)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} must not use the Urzere idle breathing controller.");
                }
            }
        }

        private static void RejectControllerOnOtherSlots(
            Transform placementRoot,
            RuntimeAnimatorController controller,
            string allowedSlotName,
            string controllerLabel)
        {
            var staticReview = placementRoot.Find(PlacementObjectName);
            if (staticReview != null)
            {
                var staticAnimator = staticReview.GetComponent<Animator>();
                if (staticAnimator != null && staticAnimator.runtimeAnimatorController == controller)
                {
                    throw new InvalidOperationException($"{PlacementObjectName} must not use the {controllerLabel}.");
                }
            }

            foreach (var spec in MotionSlotSpecs)
            {
                if (string.Equals(spec.ObjectName, allowedSlotName, StringComparison.Ordinal))
                {
                    continue;
                }

                var slot = placementRoot.Find(spec.ObjectName);
                if (slot == null)
                {
                    continue;
                }

                var animator = slot.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController == controller)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} must not use the {controllerLabel}.");
                }
            }
        }

        private static void InspectIdleBreathingControllerStillAssignedIfPresent(Transform placementRoot)
        {
            var idleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleBreathingControllerPath);
            if (idleController == null)
            {
                return;
            }

            var idleSlot = placementRoot.Find("Urzere_02_Idle");
            if (idleSlot == null)
            {
                return;
            }

            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != idleController)
            {
                throw new InvalidOperationException("Urzere_02_Idle idle breathing controller must stay assigned while applying Urzere_03_Move.");
            }
        }

        private static void InspectSeedEmitControllerStillAssignedIfPresent(Transform placementRoot)
        {
            var seedController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SeedEmitControllerPath);
            if (seedController == null)
            {
                return;
            }

            var seedSlot = placementRoot.Find("Urzere_05_Seed_Emit_Buff_Pulse");
            if (seedSlot == null)
            {
                return;
            }

            var animator = seedSlot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != seedController)
            {
                throw new InvalidOperationException("Urzere_05_Seed_Emit_Buff_Pulse seed emit controller must stay assigned while applying Urzere_07_Death.");
            }
        }

        private static void InspectHitSlotNoControllerIfPresent(Transform placementRoot)
        {
            var hitSlot = placementRoot.Find("Urzere_06_Hit");
            if (hitSlot == null)
            {
                return;
            }

            var animator = hitSlot.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                throw new InvalidOperationException("Urzere_06_Hit must remain without an AnimatorController while applying Urzere_07_Death.");
            }
        }

        private static Transform FindUrzereCameraFocus(Transform placementRoot)
        {
            return placementRoot.Find(PlacementObjectName) ?? placementRoot;
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

        private static Transform RequireMotionSlotObject(Transform placementRoot, string objectName)
        {
            var slot = placementRoot.Find(objectName);
            if (slot == null)
            {
                throw new InvalidOperationException($"{objectName} is missing under {PlacementRootName}.");
            }

            return slot;
        }

        private static Vector3 CalculateUrzereVisualFrontDirection(Transform focus)
        {
            var yawRotation = Quaternion.Euler(0f, focus.eulerAngles.y, 0f);
            var frontDirection = yawRotation * Vector3.back;
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

        private static Camera FindOrCreateReviewCamera()
        {
            var cameraObject = GameObject.Find(ReviewCameraName);
            if (cameraObject == null)
            {
                cameraObject = new GameObject(ReviewCameraName);
            }

            var camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            return camera;
        }

        private static Camera FindReviewCamera()
        {
            var cameraObject = GameObject.Find(ReviewCameraName);
            return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
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

        private static float CalculateTergoLongaSpacing(Transform tergoRoot, Transform longaRoot)
        {
            var zSpacing = Mathf.Abs(tergoRoot.position.z - longaRoot.position.z);
            if (zSpacing > 0.10f)
            {
                return zSpacing;
            }

            return Mathf.Max(Vector3.Distance(tergoRoot.position, longaRoot.position), UrzereFallbackTergoLongaSpacing);
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

        private static Mesh GetRendererSharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static Mesh GetSharedMeshForRenderer(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static void AssignSharedMeshToRenderer(Renderer renderer, Mesh mesh)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                skinnedMeshRenderer.sharedMesh = mesh;
                EditorUtility.SetDirty(skinnedMeshRenderer);
                return;
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                throw new InvalidOperationException($"{renderer.name} has no MeshFilter for no-puddle mesh assignment.");
            }

            meshFilter.sharedMesh = mesh;
            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(renderer);
        }

        private static void SaveMeshAsset(Mesh mesh, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(mesh, assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static string BuildNoPuddleMeshAssetPath(string slotName, Renderer renderer)
        {
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, renderer.transform.root);
            var fileName = SanitizeAssetFileName(slotName + "_" + renderer.name + "_" + rendererPath.GetHashCode().ToString("X8")) + "_NoPuddle.asset";
            return UnityMeshFolder + "/" + fileName;
        }

        private static string BuildNoOuterFootPlatformMeshAssetPath(string slotName, Renderer renderer)
        {
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, renderer.transform.root);
            var fileName = SanitizeAssetFileName(slotName + "_" + renderer.name + "_" + rendererPath.GetHashCode().ToString("X8")) + "_NoOuterFootPlatform.asset";
            return UnityMeshFolder + "/" + fileName;
        }

        private static string BuildMoveBodyLiftWheelRollMeshAssetPath(string slotName, Renderer renderer)
        {
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, renderer.transform.root);
            var fileName = SanitizeAssetFileName(slotName + "_" + renderer.name + "_" + rendererPath.GetHashCode().ToString("X8")) + "_BodyLiftWheelRollBlendShapes.asset";
            return UnityMeshFolder + "/" + fileName;
        }

        private static string BuildMoveWheelOnlyMeshAssetPath(string slotName, Renderer renderer)
        {
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, renderer.transform.root);
            var fileName = SanitizeAssetFileName(slotName + "_" + renderer.name + "_" + rendererPath.GetHashCode().ToString("X8")) + "_WheelOnlyRollBlendShapes.asset";
            return UnityMeshFolder + "/" + fileName;
        }

        private static string BuildDeathMeshAssetPath(string slotName, Renderer renderer)
        {
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, renderer.transform.root);
            var fileName = SanitizeAssetFileName(slotName + "_" + renderer.name + "_" + rendererPath.GetHashCode().ToString("X8")) + "_DeathRightRearCollapseBlendShape.asset";
            return UnityMeshFolder + "/" + fileName;
        }

        private static string SanitizeAssetFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
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

        private static void CopyFileToAsset(string sourceAbsolutePath, string destinationAssetPath)
        {
            var destinationAbsolutePath = AssetPathToAbsolutePath(destinationAssetPath);
            var destinationDirectory = Path.GetDirectoryName(destinationAbsolutePath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourceAbsolutePath, destinationAbsolutePath, true);
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
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

        private static void SetMaterialFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static bool Approximately(Color actual, Color expected, float tolerance)
        {
            return Mathf.Abs(actual.r - expected.r) <= tolerance &&
                   Mathf.Abs(actual.g - expected.g) <= tolerance &&
                   Mathf.Abs(actual.b - expected.b) <= tolerance &&
                   Mathf.Abs(actual.a - expected.a) <= tolerance;
        }

        private readonly struct MotionSlotSpec
        {
            public MotionSlotSpec(string objectName)
            {
                ObjectName = objectName;
            }

            public string ObjectName { get; }
        }

        private struct PuddleRemovalResult
        {
            public int RenderersProcessed;
            public int RenderersDisabled;
            public int MeshesRebuilt;
            public int TrianglesRemoved;
        }

        private struct MoveAnimationTargets
        {
            public List<Transform> BodyLiftTargets;
            public List<Transform> WheelRollTargets;
        }

        private struct MoveBlendShapeMeshInfo
        {
            public int BodyVertexCount;
            public int WheelVertexCount;
        }

        private struct MoveWheelOnlyMeshInfo
        {
            public int WheelVertexCount;
            public int HighVertexDeltaCount;
            public float MaxAbsYDelta;
        }

        private struct DeathMeshInfo
        {
            public int RightRearVertexCount;
            public int HighVertexDeltaCount;
            public float MaxCollapseDelta;
        }

        private readonly struct MoveWheelOnlyVisuals
        {
            public MoveWheelOnlyVisuals(Transform root, Transform leftWheel, Transform rightWheel)
            {
                Root = root;
                LeftWheel = leftWheel;
                RightWheel = rightWheel;
            }

            public Transform Root { get; }
            public Transform LeftWheel { get; }
            public Transform RightWheel { get; }
        }

        private readonly struct SeedEmitVisuals
        {
            public SeedEmitVisuals(Transform root, Transform model, Transform[] seeds, Vector3 emitterLocalPosition)
            {
                Root = root;
                Model = model;
                Seeds = seeds;
                EmitterLocalPosition = emitterLocalPosition;
            }

            public Transform Root { get; }
            public Transform Model { get; }
            public Transform[] Seeds { get; }
            public Vector3 EmitterLocalPosition { get; }
        }

        private readonly struct TransformSnapshot
        {
            public TransformSnapshot(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct WeightedTransform
        {
            public WeightedTransform(Transform transform, float score)
            {
                Transform = transform;
                Score = score;
            }

            public Transform Transform { get; }
            public float Score { get; }
        }

        private struct XzBounds
        {
            public float MinX;
            public float MaxX;
            public float MinZ;
            public float MaxZ;
            public bool IsValid;

            public static XzBounds Empty => new()
            {
                MinX = float.PositiveInfinity,
                MaxX = float.NegativeInfinity,
                MinZ = float.PositiveInfinity,
                MaxZ = float.NegativeInfinity,
                IsValid = false
            };

            public void Encapsulate(Vector3 point)
            {
                if (!IsValid)
                {
                    MinX = point.x;
                    MaxX = point.x;
                    MinZ = point.z;
                    MaxZ = point.z;
                    IsValid = true;
                    return;
                }

                MinX = Mathf.Min(MinX, point.x);
                MaxX = Mathf.Max(MaxX, point.x);
                MinZ = Mathf.Min(MinZ, point.z);
                MaxZ = Mathf.Max(MaxZ, point.z);
            }

            public bool ContainsWithOutset(Vector3 point, float outset)
            {
                return point.x >= MinX - outset &&
                       point.x <= MaxX + outset &&
                       point.z >= MinZ - outset &&
                       point.z <= MaxZ + outset;
            }
        }
    }
}
