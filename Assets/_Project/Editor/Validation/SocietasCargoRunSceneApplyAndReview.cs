using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.SocietasCargoRunScene
{
    internal static class SocietasCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TergoPlacementRootName = "Approved Tergo Enemy Placement";
        private const string LongaArmaPlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string UrzerePlacementRootName = "Approved Urzere Enemy Placement";
        private const string PlacementRootName = "Approved Societas Enemy Placement";
        private const string PlacementObjectName = "Societas_00_Static_Review";
        private const string ModelChildName = "SocietasPrepared_Model";
        private const string ReviewCameraName = "Model Cam";
        private const string PlayerRootName = "Player";

        private const string SourceModelAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/societas.glb";
        private const string AttackConsumeSourceModelAbsolutePath = SourceModelAbsolutePath;
        private const string SocietasArtRoot = "Assets/_Project/Art/Enemies/Societas";
        private const string UnityModelFolder = SocietasArtRoot + "/Models";
        private const string UnityMaterialFolder = SocietasArtRoot + "/Materials";
        private const string UnityAnimationFolder = SocietasArtRoot + "/Animations";
        private const string UnityControllerFolder = SocietasArtRoot + "/Controllers";
        private const string UnityModelAssetPath = UnityModelFolder + "/societas.glb";
        private const string AttackConsumeModelAssetPath = UnityModelAssetPath;
        private const string UnityMaterialAssetPath = UnityMaterialFolder + "/M_Societas_Glossy_Green_Body.mat";
        private const string IdleBreathTentacleClipName = "Societas_01_Idle_Breath_Tentacles";
        private const string IdleBreathTentacleClipPath = UnityAnimationFolder + "/" + IdleBreathTentacleClipName + ".anim";
        private const string IdleBreathTentacleControllerPath = UnityControllerFolder + "/Societas_01_Idle.controller";
        private const string MoveCaterpillarClipName = "Societas_02_Move_Caterpillar";
        private const string MoveCaterpillarClipPath = UnityAnimationFolder + "/" + MoveCaterpillarClipName + ".anim";
        private const string MoveCaterpillarControllerPath = UnityControllerFolder + "/Societas_02_Move.controller";
        private const string AttackConsumeBiteChewClipName = "Societas_03_AttackConsume_BiteChew";
        private const string AttackConsumeBiteChewClipPath = UnityAnimationFolder + "/" + AttackConsumeBiteChewClipName + ".anim";
        private const string AttackConsumeBiteChewControllerPath = UnityControllerFolder + "/Societas_03_AttackConsume.controller";
        private const string DeathMeltPuddleClipName = "Societas_04_Death_MeltPuddle";
        private const string DeathMeltPuddleClipPath = UnityAnimationFolder + "/" + DeathMeltPuddleClipName + ".anim";
        private const string DeathMeltPuddleControllerPath = UnityControllerFolder + "/Societas_04_Death.controller";
        private const string ValidationFolder = "docs/validation/societas_static";
        private const string AnimationSlotsValidationFolder = "docs/validation/societas_animation_slots";
        private const string IdleValidationFolder = "docs/validation/societas_idle";
        private const string MoveValidationFolder = "docs/validation/societas_move";
        private const string AttackConsumeValidationFolder = "docs/validation/societas_attack_consume";
        private const string DeathValidationFolder = "docs/validation/societas_death";

        private const float SocietasTargetHeightMeters = 0.30f;
        private const float SocietasFacingYawDegrees = 180f;
        private const float FallbackTergoLongaSpacing = 4.00f;
        private const float ReviewCameraMinimumFrontDistance = 1.85f;
        private const float ReviewCameraMaximumFrontDistance = 4.25f;
        private const float ReviewPlayerFrontDistance = 2.50f;
        private const float AnimationSlotMinimumSpacing = 1.05f;
        private const float IdleBreathTentacleDurationSeconds = 3.20f;
        private const float MoveCaterpillarDurationSeconds = 2.40f;
        private const float AttackConsumeBiteChewDurationSeconds = 1.40f;
        private const float DeathMeltPuddleDurationSeconds = 2.20f;
        private const string DeathMeltProxyPrefix = "SocietasDeathMelt_";
        private static readonly Color SocietasGlossyGreenColor = new(0.03f, 0.32f, 0.17f, 1f);
        private static readonly string[] AnimationReviewSlotNames =
        {
            "Societas_01_Idle",
            "Societas_02_Move",
            "Societas_03_AttackConsume",
            "Societas_04_Death"
        };
        // These names are imported from the approved Societas GLB rig and define the Idle animation target controls.
        private static readonly string[] IdleBodyMorphControlNames = { "CTRL_body_morph" };
        private static readonly string[] IdleTentacleControlNames =
        {
            "CTRL_mouth",
            "DEF_mouth_tip",
            "CTRL_front_left_ik",
            "CTRL_front_right_ik",
            "CTRL_front_left_pole",
            "CTRL_front_right_pole",
            "DEF_front_left_upper",
            "DEF_front_left_lower",
            "DEF_front_left_foot",
            "DEF_front_left_toe",
            "DEF_front_right_upper",
            "DEF_front_right_lower",
            "DEF_front_right_foot",
            "DEF_front_right_toe"
        };
        private static readonly string[] AttackConsumeMouthNameFragments =
        {
            "mouth",
            "jaw",
            "lip",
            "tooth",
            "teeth",
            "fang",
            "bite",
            "consume",
            "eat"
        };
        // Attack consume targets the upper tooth-bearing front mass; rear/lower anchor bones stay unkeyed.
        private static readonly string[] AttackConsumeEatingMouthBoneNames =
        {
            "Bone_002"
        };

        [MenuItem("Bellerophon/Enemies/Societas/Apply Prepared Model To CargoRunMvp")]
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
            ConfigureReviewCamera(placementRoot.transform);
            ConfigurePlayerStart(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Societas model applied to CargoRunMvp scene.");
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
            Debug.Log("Prepared Societas CargoRunMvp scene state inspected.");
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
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Prepared Societas player start moved to the opposite side.");
        }

        public static void CaptureReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var focus = FindSocietasCameraFocus(placementRoot.transform);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var cameraObject = new GameObject("SocietasStatic_CaptureCamera");
            var lightObject = new GameObject("SocietasStatic_CaptureLight");
            Texture2D texture = null;
            var outputPath = Path.Combine(outputDirectory, "Societas_00_Static_Review.png");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureCaptureCamera(camera, focus, bounds);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(44f, focus.eulerAngles.y - 32f, 0f);

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

            Debug.Log("SocietasStaticCapture Path=" + outputPath);
        }

        public static void ApplyAnimationReviewSlots()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureUnityFolders();
            var modelAsset = LoadPreparedModelAsset();
            var material = EnsureReferenceMaterial();
            EnsureAnimationReviewSlots(placementRoot.transform, modelAsset, material);
            InspectAnimationReviewSlots(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Prepared Societas animation review slots applied.");
        }

        public static void ValidateAnimationReviewSlots()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectAnimationReviewSlots(placementRoot.transform);
            Debug.Log("Prepared Societas animation review slots validated.");
        }

        public static void CaptureAnimationReviewSlots()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectAnimationReviewSlots(placementRoot.transform);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AnimationSlotsValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var captureBounds = CalculateAnimationSlotsBounds(placementRoot.transform);
            var cameraObject = new GameObject("SocietasAnimationSlots_CaptureCamera");
            var lightObject = new GameObject("SocietasAnimationSlots_CaptureLight");
            Texture2D texture = null;
            var outputPath = Path.Combine(outputDirectory, "Societas_AnimationReviewSlots.png");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureAnimationSlotsCaptureCamera(camera, placementRoot.transform, captureBounds);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(44f, placementRoot.transform.eulerAngles.y - 32f, 0f);

                texture = CaptureCameraTexture(camera, 1600, 900);
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

            Debug.Log("SocietasAnimationSlotsCapture Path=" + outputPath);
        }

        public static void ApplyIdleBreathTentacleAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureSocietasAnimationFolders();
            var idleSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[0]);
            var clip = EnsureIdleBreathTentacleClip(idleSlot);
            var controller = EnsureIdleBreathTentacleController(clip);
            ConfigureAnimationSlotAnimator(idleSlot, controller);
            InspectIdleBreathTentacleAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Prepared Societas 01 idle breath tentacle animation applied.");
        }

        public static void ValidateIdleBreathTentacleAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectIdleBreathTentacleAnimation(placementRoot.transform);
            Debug.Log("Prepared Societas 01 idle breath tentacle animation validated.");
        }

        public static void CaptureIdleBreathTentacleAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectIdleBreathTentacleAnimation(placementRoot.transform);
            var idleSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[0]);
            CaptureIdleBreathTentacleReviewFrames(idleSlot);
            Debug.Log("Prepared Societas 01 idle breath tentacle review frames captured.");
        }

        public static void InspectIdleRigStructure()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var idleSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[0]);
            InspectIdleRigStructure(idleSlot);
            Debug.Log("Prepared Societas 01 idle rig structure inspected.");
        }

        public static void ApplyMoveCaterpillarAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureSocietasAnimationFolders();
            var moveSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[1]);
            var clip = EnsureMoveCaterpillarClip(moveSlot);
            var controller = EnsureMoveCaterpillarController(clip);
            ConfigureAnimationSlotAnimator(moveSlot, controller);
            InspectMoveCaterpillarAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Prepared Societas 02 move caterpillar animation applied.");
        }

        public static void ValidateMoveCaterpillarAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectMoveCaterpillarAnimation(placementRoot.transform);
            Debug.Log("Prepared Societas 02 move caterpillar animation validated.");
        }

        public static void CaptureMoveCaterpillarAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectMoveCaterpillarAnimation(placementRoot.transform);
            var moveSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[1]);
            CaptureMoveCaterpillarReviewFrames(moveSlot);
            Debug.Log("Prepared Societas 02 move caterpillar review frames captured.");
        }

        public static void InspectAttackConsumeRigStructure()
        {
            var modelAsset = EnsureAttackConsumeModelAssetImported();
            var material = EnsureReferenceMaterial();
            var previewRoot = new GameObject("SocietasAttackConsumeRigInspection");
            GameObject modelInstance = null;

            try
            {
                modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                if (modelInstance == null)
                {
                    modelInstance = UnityEngine.Object.Instantiate(modelAsset);
                }

                modelInstance.name = ModelChildName;
                modelInstance.transform.SetParent(previewRoot.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;
                AssignMaterial(previewRoot.transform, material);
                ScaleToTargetHeightAndAlignToGround(previewRoot.transform, 0f);
                InspectAttackConsumeRigStructure(previewRoot.transform);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(previewRoot);
            }

            Debug.Log("Prepared Societas 03 attack consume rig structure inspected.");
        }

        public static void ApplyAttackConsumeBiteChewAnimation()
        {
            ApplyAttackConsumeBiteChewAnimationInternal(true, "Prepared Societas 03 attack consume bite chew animation applied.");
        }

        public static void ApplyAttackConsumeBiteChewAnimationVisualOnly()
        {
            ApplyAttackConsumeBiteChewAnimationInternal(false, "Prepared Societas 03 attack consume bite chew animation applied for visual review.");
        }

        public static void RemovePreparedSocietasAttackConsumeAnimationVisualOnly()
        {
            RemoveAttackConsumeAnimationInternal("Prepared Societas 03 attack consume animation removed for visual review.");
        }

        public static void RemoveAttackConsumeAnimationVisualOnly()
        {
            RemoveAttackConsumeAnimationInternal("Prepared Societas 03 attack consume animation removed for visual review.");
        }

        private static void ApplyAttackConsumeBiteChewAnimationInternal(bool inspectAnimation, string completionLog)
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureSocietasAnimationFolders();
            var modelAsset = EnsureAttackConsumeModelAssetImported();
            var material = EnsureReferenceMaterial();
            var attackSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[2]);
            ApplyAttackConsumeModelToSlot(attackSlot, modelAsset, material, placementRoot.transform.position.y);
            var clip = EnsureAttackConsumeBiteChewClip(attackSlot);
            var controller = EnsureAttackConsumeBiteChewController(clip);
            ConfigureAnimationSlotAnimator(attackSlot, controller);
            if (inspectAnimation)
            {
                InspectAttackConsumeBiteChewAnimation(placementRoot.transform);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(completionLog);
        }

        private static void RemoveAttackConsumeAnimationInternal(string completionLog)
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var attackSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[2]);
            var removedAnimator = false;
            var animator = attackSlot.GetComponent<Animator>();
            if (animator != null)
            {
                UnityEngine.Object.DestroyImmediate(animator);
                removedAnimator = true;
            }

            DisableImportedAnimationPlayback(attackSlot);
            EditorUtility.SetDirty(attackSlot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"{completionLog} AnimatorRemoved={removedAnimator}, Slot={AnimationReviewSlotNames[2]}.");
        }

        public static void ValidateAttackConsumeBiteChewAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectAttackConsumeBiteChewAnimation(placementRoot.transform);
            Debug.Log("Prepared Societas 03 attack consume bite chew animation validated.");
        }

        public static void CaptureAttackConsumeBiteChewAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectAttackConsumeBiteChewAnimation(placementRoot.transform);
            var attackSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[2]);
            CaptureAttackConsumeBiteChewReviewFrames(attackSlot);
            Debug.Log("Prepared Societas 03 attack consume bite chew review frames captured.");
        }

        public static void CaptureAttackConsumeBiteChewAnimationVisualOnly()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var attackSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[2]);
            CaptureAttackConsumeBiteChewReviewFrames(attackSlot);
            Debug.Log("Prepared Societas 03 attack consume bite chew visual review frames captured.");
        }

        public static void InspectAttackConsumeBoneWeightsVisualOnly()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var attackSlot = RequireAnimationReviewSlot(placementRoot.transform, AnimationReviewSlotNames[2]);
            RequireAttackConsumeEatingModelSlot(attackSlot);
            InspectAttackConsumeBoneWeights(attackSlot);
            Debug.Log("Prepared Societas 03 attack consume bone weights inspected for visual retune.");
        }

        public static void ApplyDeathMeltPuddleAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            EnsureSocietasAnimationFolders();
            var modelAsset = EnsureAttackConsumeModelAssetImported();
            var material = EnsureReferenceMaterial();
            var deathSlot = RequireAnimationReviewSlotRoot(placementRoot.transform, AnimationReviewSlotNames[3]);
            RemoveDeathMeltProxyVisuals(deathSlot);
            ApplyAttackConsumeModelToSlot(deathSlot, modelAsset, material, placementRoot.transform.position.y);
            var visuals = EnsureDeathMeltProxyVisuals(deathSlot, material);
            var clip = EnsureDeathMeltPuddleClip(deathSlot, visuals);
            var controller = EnsureDeathMeltPuddleController(clip);
            ConfigureAnimationSlotAnimator(deathSlot, controller);
            InspectDeathMeltPuddleAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Prepared Societas 04 death melt puddle animation applied.");
        }

        public static void ValidateDeathMeltPuddleAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectDeathMeltPuddleAnimation(placementRoot.transform);
            Debug.Log("Prepared Societas 04 death melt puddle animation validated.");
        }

        public static void CaptureDeathMeltPuddleAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectDeathMeltPuddleAnimation(placementRoot.transform);
            var deathSlot = RequireDeathAnimationSlot(placementRoot.transform);
            CaptureDeathMeltPuddleReviewFrames(deathSlot);
            Debug.Log("Prepared Societas 04 death melt puddle review frames captured.");
        }

        private static void RequirePreparedModelFile()
        {
            if (!File.Exists(SourceModelAbsolutePath))
            {
                throw new FileNotFoundException("Prepared Societas GLB model is missing.", SourceModelAbsolutePath);
            }
        }

        private static void RequireAttackConsumeModelFile()
        {
            if (!File.Exists(AttackConsumeSourceModelAbsolutePath))
            {
                throw new FileNotFoundException("Prepared Societas attack consume GLB model is missing.", AttackConsumeSourceModelAbsolutePath);
            }
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(SocietasArtRoot);
            EnsureUnityFolder(UnityModelFolder);
            EnsureUnityFolder(UnityMaterialFolder);
        }

        private static void EnsureSocietasAnimationFolders()
        {
            EnsureUnityFolder(SocietasArtRoot);
            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityControllerFolder);
        }

        private static AnimationClip EnsureIdleBreathTentacleClip(Transform idleSlot)
        {
            var bodyControls = CollectBodyMorphTargets(idleSlot);
            if (bodyControls.Count == 0)
            {
                throw new InvalidOperationException("Societas idle animation requires a body morph target in the existing rig.");
            }

            var tentacleControls = CollectTentacleRigTargets(idleSlot);
            if (tentacleControls.Count < 3)
            {
                throw new InvalidOperationException("Societas idle animation requires mouth/front tentacle rig controls.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathTentacleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = IdleBreathTentacleClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, IdleBreathTentacleClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            for (var i = 0; i < bodyControls.Count; i++)
            {
                AddBodyBreathCurves(clip, idleSlot, bodyControls[i], i);
            }

            for (var i = 0; i < tentacleControls.Count; i++)
            {
                AddTentacleIdleCurves(clip, idleSlot, tentacleControls[i], i);
            }

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(IdleBreathTentacleClipPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathTentacleClipPath);
        }

        private static RuntimeAnimatorController EnsureIdleBreathTentacleController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleBreathTentacleControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(IdleBreathTentacleControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                AssetDatabase.DeleteAsset(IdleBreathTentacleControllerPath);
                controller = AnimatorController.CreateAnimatorControllerAtPath(IdleBreathTentacleControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null && string.Equals(childState.state.name, IdleBreathTentacleClipName, StringComparison.Ordinal))
                {
                    state = childState.state;
                    break;
                }
            }

            state ??= stateMachine.AddState(IdleBreathTentacleClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(IdleBreathTentacleControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleBreathTentacleControllerPath);
        }

        private static AnimationClip EnsureMoveCaterpillarClip(Transform moveSlot)
        {
            var bodyControls = CollectMoveCaterpillarRigTargets(moveSlot);
            if (bodyControls.Count < 5)
            {
                throw new InvalidOperationException("Societas move animation requires at least five usable body rig controls.");
            }

            var tentacleControls = CollectTentacleRigTargets(moveSlot);
            if (tentacleControls.Count < 3)
            {
                throw new InvalidOperationException("Societas move animation requires mouth/front tentacle rig controls.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveCaterpillarClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = MoveCaterpillarClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, MoveCaterpillarClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            for (var i = 0; i < bodyControls.Count; i++)
            {
                AddCaterpillarBodyWaveCurves(clip, moveSlot, bodyControls[i], i, bodyControls.Count);
            }

            for (var i = 0; i < tentacleControls.Count; i++)
            {
                AddCaterpillarFrontPullCurves(clip, moveSlot, tentacleControls[i], i);
            }

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(MoveCaterpillarClipPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveCaterpillarClipPath);
        }

        private static RuntimeAnimatorController EnsureMoveCaterpillarController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MoveCaterpillarControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(MoveCaterpillarControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                AssetDatabase.DeleteAsset(MoveCaterpillarControllerPath);
                controller = AnimatorController.CreateAnimatorControllerAtPath(MoveCaterpillarControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null && string.Equals(childState.state.name, MoveCaterpillarClipName, StringComparison.Ordinal))
                {
                    state = childState.state;
                    break;
                }
            }

            state ??= stateMachine.AddState(MoveCaterpillarClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(MoveCaterpillarControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MoveCaterpillarControllerPath);
        }

        private static AnimationClip EnsureAttackConsumeBiteChewClip(Transform attackSlot)
        {
            var mouthControls = CollectAttackConsumeMouthRigTargets(attackSlot);
            if (mouthControls.Count < 1)
            {
                throw new InvalidOperationException("Societas attack consume animation requires an upper body rig control from the eating model.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackConsumeBiteChewClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = AttackConsumeBiteChewClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, AttackConsumeBiteChewClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            for (var i = 0; i < mouthControls.Count; i++)
            {
                AddAttackConsumeMouthBiteCurves(clip, attackSlot, mouthControls[i], i);
            }

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(AttackConsumeBiteChewClipPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackConsumeBiteChewClipPath);
        }

        private static RuntimeAnimatorController EnsureAttackConsumeBiteChewController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AttackConsumeBiteChewControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AttackConsumeBiteChewControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                AssetDatabase.DeleteAsset(AttackConsumeBiteChewControllerPath);
                controller = AnimatorController.CreateAnimatorControllerAtPath(AttackConsumeBiteChewControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null && string.Equals(childState.state.name, AttackConsumeBiteChewClipName, StringComparison.Ordinal))
                {
                    state = childState.state;
                    break;
                }
            }

            state ??= stateMachine.AddState(AttackConsumeBiteChewClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(AttackConsumeBiteChewControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AttackConsumeBiteChewControllerPath);
        }

        private static AnimationClip EnsureDeathMeltPuddleClip(Transform deathSlot, DeathMeltProxyVisuals visuals)
        {
            var model = deathSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {AnimationReviewSlotNames[3]}.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathMeltPuddleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = DeathMeltPuddleClipName,
                    frameRate = 30f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, DeathMeltPuddleClipPath);
            }

            clip.ClearCurves();
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var modelBounds = CalculateRendererBounds(model, new Bounds(deathSlot.position, Vector3.one * SocietasTargetHeightMeters));
            var sinkDistance = Mathf.Max(modelBounds.size.y * 0.86f, 0.22f);
            var localSink = deathSlot.InverseTransformVector(Vector3.down * sinkDistance);
            var times = new[] { 0.00f, 0.32f, 0.70f, 1.10f, 1.55f, DeathMeltPuddleDurationSeconds };
            var bodyScaleFactors = new[]
            {
                Vector3.one,
                new Vector3(1.12f, 0.72f, 1.10f),
                new Vector3(1.36f, 0.32f, 1.28f),
                new Vector3(1.62f, 0.055f, 1.54f),
                new Vector3(1.92f, 0.010f, 1.84f),
                new Vector3(0.01f, 0.004f, 0.01f)
            };

            AddLocalPositionOffsetCurves(
                clip,
                deathSlot,
                model,
                times,
                CreateDeathBodyCenterLockedOffsets(deathSlot, model, modelBounds, localSink, bodyScaleFactors));
            AddLocalScaleFactorCurves(
                clip,
                deathSlot,
                model,
                times,
                bodyScaleFactors);

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                AddRendererEnabledCurve(clip, deathSlot, renderer.transform, times, new[] { 1f, 1f, 1f, 0f, 0f, 0f });
            }

            AddDeathMeltProxyCurves(clip, deathSlot, visuals, times);

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.ImportAsset(DeathMeltPuddleClipPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathMeltPuddleClipPath);
        }

        private static RuntimeAnimatorController EnsureDeathMeltPuddleController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DeathMeltPuddleControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(DeathMeltPuddleControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                AssetDatabase.DeleteAsset(DeathMeltPuddleControllerPath);
                controller = AnimatorController.CreateAnimatorControllerAtPath(DeathMeltPuddleControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null && string.Equals(childState.state.name, DeathMeltPuddleClipName, StringComparison.Ordinal))
                {
                    state = childState.state;
                    break;
                }
            }

            state ??= stateMachine.AddState(DeathMeltPuddleClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.ImportAsset(DeathMeltPuddleControllerPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DeathMeltPuddleControllerPath);
        }

        private static void ConfigureAnimationSlotAnimator(Transform slot, RuntimeAnimatorController controller)
        {
            DisableImportedAnimationPlayback(slot);

            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.avatar = FindImportedAvatar(slot, animator);
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.fireEvents = false;
            animator.keepAnimatorStateOnDisable = false;

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(slot);
        }

        private static Avatar FindImportedAvatar(Transform slot, Animator slotAnimator)
        {
            foreach (var animator in slot.GetComponentsInChildren<Animator>(true))
            {
                if (animator != slotAnimator && animator.avatar != null)
                {
                    return animator.avatar;
                }
            }

            return null;
        }

        private static void AddBodyBreathCurves(AnimationClip clip, Transform idleSlot, Transform control, int controlIndex)
        {
            var path = AnimationUtility.CalculateTransformPath(control, idleSlot);
            var scale = control.localScale;
            var position = control.localPosition;
            var phase = IdleOrganicPhase(controlIndex, 11);

            SetTransformCurve(clip, path, "m_LocalScale.x", CreateIdleLoopCurve(scale.x, Mathf.Max(Mathf.Abs(scale.x) * 0.045f, 0.0001f), phase, 0.18f));
            SetTransformCurve(clip, path, "m_LocalScale.y", CreateIdleLoopCurve(scale.y, -Mathf.Max(Mathf.Abs(scale.y) * 0.0035f, 0.0001f), phase + 0.18f, 0.08f));
            SetTransformCurve(clip, path, "m_LocalScale.z", CreateIdleLoopCurve(scale.z, Mathf.Max(Mathf.Abs(scale.z) * 0.050f, 0.0001f), phase + 0.10f, 0.16f));
            SetTransformCurve(clip, path, "m_LocalPosition.y", CreateIdleLoopCurve(position.y, 0.00006f, phase + 0.35f, 0.02f));
        }

        private static void AddTentacleIdleCurves(AnimationClip clip, Transform idleSlot, Transform control, int controlIndex)
        {
            var path = AnimationUtility.CalculateTransformPath(control, idleSlot);
            var position = control.localPosition;
            var euler = NormalizeEuler(control.localEulerAngles);
            var isMouthTip = string.Equals(control.name, "DEF_mouth_tip", StringComparison.Ordinal);
            var isMouthControl = control.name.IndexOf("mouth", StringComparison.OrdinalIgnoreCase) >= 0;
            var positionAmplitude = isMouthTip ? 0.028f : isMouthControl ? 0.020f : 0.014f;
            var rotationAmplitude = isMouthTip ? 13.0f : isMouthControl ? 8.0f : 6.0f;
            var phase = IdleOrganicPhase(controlIndex, 31);

            SetTransformCurve(clip, path, "m_LocalPosition.x", CreateIdleLoopCurve(position.x, positionAmplitude * IdleSignedScale(controlIndex, 41), phase, 0.34f));
            SetTransformCurve(clip, path, "m_LocalPosition.y", CreateIdleLoopCurve(position.y, positionAmplitude * 0.55f * IdleSignedScale(controlIndex, 43), phase + 0.85f, 0.28f));
            SetTransformCurve(clip, path, "m_LocalPosition.z", CreateIdleLoopCurve(position.z, positionAmplitude * 0.75f * IdleSignedScale(controlIndex, 47), phase + 1.44f, 0.31f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.x", CreateIdleLoopCurve(euler.x, rotationAmplitude * IdleSignedScale(controlIndex, 53), phase + 0.32f, 0.25f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.y", CreateIdleLoopCurve(euler.y, rotationAmplitude * 0.65f * IdleSignedScale(controlIndex, 59), phase + 1.18f, 0.22f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.z", CreateIdleLoopCurve(euler.z, rotationAmplitude * 0.80f * IdleSignedScale(controlIndex, 61), phase + 2.03f, 0.30f));
        }

        private static void AddCaterpillarBodyWaveCurves(AnimationClip clip, Transform moveSlot, Transform control, int controlIndex, int controlCount)
        {
            var path = AnimationUtility.CalculateTransformPath(control, moveSlot);
            var position = control.localPosition;
            var scale = control.localScale;
            var euler = NormalizeEuler(control.localEulerAngles);
            var segment01 = controlCount > 1 ? controlIndex / (float)(controlCount - 1) : 0.5f;
            var phase = Mathf.Lerp(Mathf.PI * 1.85f, -Mathf.PI * 1.85f, segment01) + IdleOrganicPhase(controlIndex, 131) * 0.10f;
            var segmentWeight = Mathf.Lerp(0.82f, 1.14f, IdleOrganicNoise01(controlIndex, 137));

            SetTransformCurve(clip, path, "m_LocalScale.x", CreateMoveLoopCurve(scale.x, -Mathf.Max(Mathf.Abs(scale.x) * 0.050f, 0.0001f) * segmentWeight, phase + 0.20f, 0.18f));
            SetTransformCurve(clip, path, "m_LocalScale.y", CreateMoveLoopCurve(scale.y, Mathf.Max(Mathf.Abs(scale.y) * 0.080f, 0.0001f) * segmentWeight, phase + 1.12f, 0.24f));
            SetTransformCurve(clip, path, "m_LocalScale.z", CreateMoveLoopCurve(scale.z, Mathf.Max(Mathf.Abs(scale.z) * 0.120f, 0.0001f) * segmentWeight, phase, 0.26f));
            SetTransformCurve(clip, path, "m_LocalPosition.y", CreateMoveLiftCurve(position.y, 0.0120f * segmentWeight, phase + 0.55f));
            SetTransformCurve(clip, path, "m_LocalPosition.z", CreateMoveLoopCurve(position.z, 0.0140f * segmentWeight, phase + 0.72f, 0.20f));
            SetTransformCurve(clip, path, "m_LocalPosition.x", CreateMoveLoopCurve(position.x, 0.0060f * IdleSignedScale(controlIndex, 139), phase + 1.74f, 0.15f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.x", CreateMoveLoopCurve(euler.x, 11.0f * IdleSignedScale(controlIndex, 149), phase + 0.35f, 0.16f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.y", CreateMoveLoopCurve(euler.y, 5.0f * IdleSignedScale(controlIndex, 151), phase + 1.08f, 0.14f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.z", CreateMoveLoopCurve(euler.z, 8.0f * IdleSignedScale(controlIndex, 157), phase + 1.92f, 0.18f));
        }

        private static void AddCaterpillarFrontPullCurves(AnimationClip clip, Transform moveSlot, Transform control, int controlIndex)
        {
            var path = AnimationUtility.CalculateTransformPath(control, moveSlot);
            var position = control.localPosition;
            var euler = NormalizeEuler(control.localEulerAngles);
            var phase = IdleOrganicPhase(controlIndex, 173) * 0.12f;
            var amplitude = Mathf.Lerp(0.85f, 1.20f, IdleOrganicNoise01(controlIndex, 179));

            SetTransformCurve(clip, path, "m_LocalPosition.z", CreateMoveLoopCurve(position.z, 0.0180f * amplitude, phase + 0.15f, 0.28f));
            SetTransformCurve(clip, path, "m_LocalPosition.y", CreateMoveLiftCurve(position.y, 0.0080f * amplitude, phase + 0.85f));
            SetTransformCurve(clip, path, "m_LocalPosition.x", CreateMoveLoopCurve(position.x, 0.0065f * IdleSignedScale(controlIndex, 181), phase + 1.30f, 0.20f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.x", CreateMoveLoopCurve(euler.x, 12.0f * IdleSignedScale(controlIndex, 191), phase + 0.40f, 0.22f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.y", CreateMoveLoopCurve(euler.y, 7.0f * IdleSignedScale(controlIndex, 193), phase + 1.20f, 0.18f));
            SetTransformCurve(clip, path, "localEulerAnglesRaw.z", CreateMoveLoopCurve(euler.z, 10.0f * IdleSignedScale(controlIndex, 197), phase + 2.10f, 0.24f));
        }

        private static void AddAttackConsumeMouthBiteCurves(AnimationClip clip, Transform attackSlot, Transform control, int controlIndex)
        {
            var path = AnimationUtility.CalculateTransformPath(control, attackSlot);
            var position = control.localPosition;
            var bounds = CalculateRendererBounds(attackSlot, new Bounds(attackSlot.position, Vector3.one));

            var modelHeight = Mathf.Max(bounds.size.y, 0.001f);
            var liftAmplitude = Mathf.Clamp(modelHeight * 5.20f, 0.390f, 1.350f);
            var slamAmplitude = Mathf.Clamp(modelHeight * 3.60f, 0.270f, 0.920f);

            SetWorldVerticalSlamPositionCurves(clip, path, control, position, liftAmplitude, -slamAmplitude);
        }

        private static float AttackConsumeMouthControlScale(string controlName)
        {
            return controlName switch
            {
                "Bone_009" => 0.48f,
                "Bone_013" => 0.50f,
                "Bone_017" => 0.50f,
                "Bone_029" => 0.50f,
                "Bone_021" => 0.50f,
                "Bone_025" => 0.50f,
                "Bone_033" => 0.40f,
                "Bone_037" => 0.40f,
                _ => 0.32f
            };
        }

        private static float AttackConsumeFrontSlamRotationDegrees(string controlName)
        {
            return controlName switch
            {
                "Bone_009" => 22.0f,
                "Bone_013" => 18.0f,
                "Bone_017" => 18.0f,
                "Bone_029" => 26.0f,
                "Bone_021" => 22.0f,
                "Bone_025" => 22.0f,
                "Bone_033" => 16.0f,
                "Bone_037" => 16.0f,
                _ => 10.0f
            };
        }

        private static bool IsAttackConsumeFrontBaseControl(string controlName)
        {
            return string.Equals(controlName, "Bone_008", StringComparison.Ordinal);
        }

        private static bool IsAttackConsumeUpperMassControl(string controlName)
        {
            return string.Equals(controlName, "Bone_009", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_013", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_017", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_021", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_025", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_029", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_033", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_037", StringComparison.Ordinal);
        }

        private static bool IsAttackConsumeUpperMouthChildControl(string controlName)
        {
            return string.Equals(controlName, "Bone_020", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_023", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_024", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_025", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_026", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_028", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_029", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_032", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_036", StringComparison.Ordinal);
        }

        private static bool IsAttackConsumeSideLipControl(string controlName)
        {
            return string.Equals(controlName, "Bone_020", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_021", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_024", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_025", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_032", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_033", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_036", StringComparison.Ordinal)
                || string.Equals(controlName, "Bone_037", StringComparison.Ordinal);
        }

        private static bool IsAttackConsumeFrontTipControl(string controlName)
        {
            return string.Equals(controlName, "Bone_026", StringComparison.Ordinal);
        }

        private static bool IsAttackConsumeLowerBaseControl(string controlName)
        {
            return string.Equals(controlName, "Bone_007", StringComparison.Ordinal);
        }

        private static bool IsAttackConsumeLowerAssistControl(string controlName)
        {
            return string.Equals(controlName, "Bone_009", StringComparison.Ordinal);
        }

        private static void SetWorldVerticalSlamPositionCurves(
            AnimationClip clip,
            string path,
            Transform control,
            Vector3 position,
            float liftAmplitude,
            float slamAmplitude)
        {
            var reference = control.parent != null ? control.parent : control;
            var localUp = reference.InverseTransformDirection(Vector3.up);
            if (localUp.sqrMagnitude < 0.001f)
            {
                localUp = Vector3.up;
            }

            localUp.Normalize();
            var curveCount = 0;
            curveCount += SetWorldOffsetAxisCurve(clip, path, "m_LocalPosition.x", position.x, localUp.x, liftAmplitude, slamAmplitude);
            curveCount += SetWorldOffsetAxisCurve(clip, path, "m_LocalPosition.y", position.y, localUp.y, liftAmplitude, slamAmplitude);
            curveCount += SetWorldOffsetAxisCurve(clip, path, "m_LocalPosition.z", position.z, localUp.z, liftAmplitude, slamAmplitude);
            if (curveCount == 0)
            {
                SetTransformCurve(clip, path, "m_LocalPosition.y", CreateAttackSlamCurve(position.y, liftAmplitude, slamAmplitude));
            }
        }

        private static int SetWorldOffsetAxisCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            float neutral,
            float axisWeight,
            float liftAmplitude,
            float slamAmplitude)
        {
            if (Mathf.Abs(axisWeight) < 0.001f)
            {
                return 0;
            }

            SetTransformCurve(clip, path, propertyName, CreateAttackSlamCurve(neutral, liftAmplitude * axisWeight, slamAmplitude * axisWeight));
            return 1;
        }

        private static void SetWorldPitchSlamRotationCurve(
            AnimationClip clip,
            string path,
            Transform control,
            Vector3 euler,
            Vector3 worldAxis,
            float liftDegrees,
            float slamDegrees)
        {
            var axis = SelectLocalEulerAxis(control, worldAxis);
            SetTransformCurve(
                clip,
                path,
                axis.PropertyName,
                CreateAttackSlamCurve(axis.Read(euler), liftDegrees * axis.Sign, slamDegrees * axis.Sign));
        }

        private static LocalPositionAxis SelectLocalPositionAxis(Transform control, Vector3 worldDirection)
        {
            var reference = control.parent != null ? control.parent : control;
            var normalizedDirection = worldDirection.sqrMagnitude > 0.001f ? worldDirection.normalized : Vector3.up;
            var xScore = Vector3.Dot(reference.TransformDirection(Vector3.right).normalized, normalizedDirection);
            var yScore = Vector3.Dot(reference.TransformDirection(Vector3.up).normalized, normalizedDirection);
            var zScore = Vector3.Dot(reference.TransformDirection(Vector3.forward).normalized, normalizedDirection);
            var xAbs = Mathf.Abs(xScore);
            var yAbs = Mathf.Abs(yScore);
            var zAbs = Mathf.Abs(zScore);

            if (xAbs >= yAbs && xAbs >= zAbs)
            {
                return new LocalPositionAxis("m_LocalPosition.x", Mathf.Sign(xScore == 0f ? 1f : xScore), 0);
            }

            if (zAbs >= yAbs)
            {
                return new LocalPositionAxis("m_LocalPosition.z", Mathf.Sign(zScore == 0f ? 1f : zScore), 2);
            }

            return new LocalPositionAxis("m_LocalPosition.y", Mathf.Sign(yScore == 0f ? 1f : yScore), 1);
        }

        private static LocalEulerAxis SelectLocalEulerAxis(Transform control, Vector3 worldAxis)
        {
            var reference = control.parent != null ? control.parent : control;
            var normalizedAxis = worldAxis.sqrMagnitude > 0.001f ? worldAxis.normalized : Vector3.right;
            var xScore = Vector3.Dot(reference.TransformDirection(Vector3.right).normalized, normalizedAxis);
            var yScore = Vector3.Dot(reference.TransformDirection(Vector3.up).normalized, normalizedAxis);
            var zScore = Vector3.Dot(reference.TransformDirection(Vector3.forward).normalized, normalizedAxis);
            var xAbs = Mathf.Abs(xScore);
            var yAbs = Mathf.Abs(yScore);
            var zAbs = Mathf.Abs(zScore);

            if (xAbs >= yAbs && xAbs >= zAbs)
            {
                return new LocalEulerAxis("localEulerAnglesRaw.x", Mathf.Sign(xScore == 0f ? 1f : xScore), 0);
            }

            if (zAbs >= yAbs)
            {
                return new LocalEulerAxis("localEulerAnglesRaw.z", Mathf.Sign(zScore == 0f ? 1f : zScore), 2);
            }

            return new LocalEulerAxis("localEulerAnglesRaw.y", Mathf.Sign(yScore == 0f ? 1f : yScore), 1);
        }

        private static float AttackConsumeFrontJawSign(string controlName, float height01)
        {
            return controlName switch
            {
                "Bone_008" => 1f,
                "Bone_018" => 1f,
                "Bone_007" => -1f,
                "Bone_009" => -1f,
                "Bone_017" => 1f,
                _ => height01 >= 0.52f ? 1f : -1f
            };
        }

        private static void AddAttackConsumeBodyBraceCurves(AnimationClip clip, Transform attackSlot, Transform control, int controlIndex)
        {
            var path = AnimationUtility.CalculateTransformPath(control, attackSlot);
            var position = control.localPosition;
            var scale = control.localScale;
            var organicScale = Mathf.Lerp(0.82f, 1.12f, IdleOrganicNoise01(controlIndex, 251));

            SetTransformCurve(clip, path, "m_LocalScale.x", CreateBiteBodyPulseCurve(scale.x, -Mathf.Max(Mathf.Abs(scale.x) * 0.018f, 0.0001f) * organicScale));
            SetTransformCurve(clip, path, "m_LocalScale.y", CreateBiteBodyPulseCurve(scale.y, -Mathf.Max(Mathf.Abs(scale.y) * 0.020f, 0.0001f) * organicScale));
            SetTransformCurve(clip, path, "m_LocalScale.z", CreateBiteBodyPulseCurve(scale.z, Mathf.Max(Mathf.Abs(scale.z) * 0.040f, 0.0001f) * organicScale));
            SetTransformCurve(clip, path, "m_LocalPosition.z", CreateBiteBodyPulseCurve(position.z, -0.0100f * organicScale));
        }

        private static AnimationCurve CreateIdleLoopCurve(float neutral, float amplitude, float phase, float secondaryRatio)
        {
            const int sampleCount = 9;
            var keys = new Keyframe[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var time = IdleBreathTentacleDurationSeconds * i / (sampleCount - 1);
                var angle = Mathf.PI * 2f * time / IdleBreathTentacleDurationSeconds;
                var value = neutral +
                    Mathf.Sin(angle + phase) * amplitude +
                    Mathf.Sin(angle * 2f + phase * 0.57f + 1.13f) * amplitude * secondaryRatio;
                keys[i] = new Keyframe(time, value);
            }

            var curve = new AnimationCurve(keys);
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateMoveLoopCurve(float neutral, float amplitude, float phase, float secondaryRatio)
        {
            const int sampleCount = 13;
            var keys = new Keyframe[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var time = MoveCaterpillarDurationSeconds * i / (sampleCount - 1);
                var angle = Mathf.PI * 2f * time / MoveCaterpillarDurationSeconds;
                var value = neutral +
                    Mathf.Sin(angle + phase) * amplitude +
                    Mathf.Sin(angle * 2f + phase * 0.63f + 0.91f) * amplitude * secondaryRatio;
                keys[i] = new Keyframe(time, value);
            }

            var curve = new AnimationCurve(keys);
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateMoveLiftCurve(float neutral, float amplitude, float phase)
        {
            const int sampleCount = 13;
            var keys = new Keyframe[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var time = MoveCaterpillarDurationSeconds * i / (sampleCount - 1);
                var angle = Mathf.PI * 2f * time / MoveCaterpillarDurationSeconds;
                var liftPulse = Mathf.Pow(Mathf.Clamp01((Mathf.Sin(angle + phase) + 1f) * 0.5f), 1.70f);
                var settling = Mathf.Sin(angle * 2f + phase * 0.45f) * amplitude * 0.10f;
                keys[i] = new Keyframe(time, neutral + liftPulse * amplitude + settling);
            }

            var curve = new AnimationCurve(keys);
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateBiteChewCurve(float neutral, float openAmplitude, float closeAmplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutral),
                new Keyframe(0.22f, neutral + openAmplitude * 0.42f),
                new Keyframe(0.42f, neutral + openAmplitude),
                new Keyframe(0.58f, neutral + closeAmplitude * 1.20f),
                new Keyframe(0.72f, neutral + closeAmplitude),
                new Keyframe(0.92f, neutral + openAmplitude * 0.36f),
                new Keyframe(1.08f, neutral + closeAmplitude * 0.70f),
                new Keyframe(1.26f, neutral + openAmplitude * 0.24f),
                new Keyframe(1.44f, neutral + closeAmplitude * 0.42f),
                new Keyframe(1.72f, neutral + openAmplitude * 0.10f),
                new Keyframe(2.00f, neutral),
                new Keyframe(AttackConsumeBiteChewDurationSeconds, neutral));
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateBiteChewScaleFactorCurve(float neutral, float openFactor, float closeFactor)
        {
            var safeCloseFactor = Mathf.Max(closeFactor, 0.18f);
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutral),
                new Keyframe(0.22f, neutral * Mathf.Lerp(1f, openFactor, 0.42f)),
                new Keyframe(0.42f, neutral * openFactor),
                new Keyframe(0.58f, neutral * safeCloseFactor),
                new Keyframe(0.72f, neutral * Mathf.Lerp(safeCloseFactor, 1f, 0.18f)),
                new Keyframe(0.92f, neutral * Mathf.Lerp(1f, openFactor, 0.36f)),
                new Keyframe(1.08f, neutral * Mathf.Lerp(safeCloseFactor, 1f, 0.30f)),
                new Keyframe(1.26f, neutral * Mathf.Lerp(1f, openFactor, 0.24f)),
                new Keyframe(1.44f, neutral * Mathf.Lerp(safeCloseFactor, 1f, 0.58f)),
                new Keyframe(1.72f, neutral * Mathf.Lerp(1f, openFactor, 0.10f)),
                new Keyframe(2.00f, neutral),
                new Keyframe(AttackConsumeBiteChewDurationSeconds, neutral));
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateAttackSlamCurve(float neutral, float liftAmplitude, float slamAmplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutral),
                new Keyframe(0.10f, neutral + liftAmplitude * 0.18f),
                new Keyframe(0.24f, neutral + liftAmplitude * 0.68f),
                new Keyframe(0.40f, neutral + liftAmplitude),
                new Keyframe(0.46f, neutral + liftAmplitude * 0.82f),
                new Keyframe(0.50f, neutral + liftAmplitude * 0.24f),
                new Keyframe(0.56f, neutral + slamAmplitude * 1.10f),
                new Keyframe(0.64f, neutral + slamAmplitude * 0.60f),
                new Keyframe(0.78f, neutral - slamAmplitude * 0.12f),
                new Keyframe(0.98f, neutral + liftAmplitude * 0.06f),
                new Keyframe(1.12f, neutral),
                new Keyframe(AttackConsumeBiteChewDurationSeconds, neutral));
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateBiteBodyPulseCurve(float neutral, float amplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, neutral),
                new Keyframe(0.30f, neutral + amplitude * 0.25f),
                new Keyframe(0.52f, neutral + amplitude),
                new Keyframe(0.68f, neutral - amplitude * 0.35f),
                new Keyframe(0.90f, neutral + amplitude * 0.55f),
                new Keyframe(1.08f, neutral - amplitude * 0.22f),
                new Keyframe(1.28f, neutral + amplitude * 0.38f),
                new Keyframe(1.54f, neutral - amplitude * 0.12f),
                new Keyframe(1.90f, neutral),
                new Keyframe(AttackConsumeBiteChewDurationSeconds, neutral));
            SetAutoTangents(curve);
            return curve;
        }

        private static void SetTransformCurve(AnimationClip clip, string path, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName), curve);
        }

        private static void AddLocalPositionOffsetCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] localOffsets)
        {
            ValidateVectorCurveArrays(times, localOffsets, "local position offset");
            var positions = new Vector3[times.Length];
            var basePosition = target.localPosition;
            for (var i = 0; i < times.Length; i++)
            {
                positions[i] = basePosition + localOffsets[i];
            }

            AddLocalPositionAbsoluteCurves(clip, root, target, times, positions);
        }

        private static void AddLocalPositionAbsoluteCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] localPositions)
        {
            ValidateVectorCurveArrays(times, localPositions, "local position");
            var path = AnimationUtility.CalculateTransformPath(target, root);
            SetTransformCurve(clip, path, "m_LocalPosition.x", CreateFloatCurve(times, localPositions, 0));
            SetTransformCurve(clip, path, "m_LocalPosition.y", CreateFloatCurve(times, localPositions, 1));
            SetTransformCurve(clip, path, "m_LocalPosition.z", CreateFloatCurve(times, localPositions, 2));
        }

        private static void AddLocalScaleFactorCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] scaleFactors)
        {
            ValidateVectorCurveArrays(times, scaleFactors, "local scale factor");
            var scales = new Vector3[times.Length];
            var baseScale = target.localScale;
            for (var i = 0; i < times.Length; i++)
            {
                scales[i] = Vector3.Scale(baseScale, scaleFactors[i]);
            }

            AddLocalScaleAbsoluteCurves(clip, root, target, times, scales);
        }

        private static void AddLocalScaleAbsoluteCurves(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            Vector3[] localScales)
        {
            ValidateVectorCurveArrays(times, localScales, "local scale");
            var path = AnimationUtility.CalculateTransformPath(target, root);
            SetTransformCurve(clip, path, "m_LocalScale.x", CreateFloatCurve(times, localScales, 0));
            SetTransformCurve(clip, path, "m_LocalScale.y", CreateFloatCurve(times, localScales, 1));
            SetTransformCurve(clip, path, "m_LocalScale.z", CreateFloatCurve(times, localScales, 2));
        }

        private static Vector3[] CreateDeathBodyCenterLockedOffsets(
            Transform root,
            Transform model,
            Bounds modelBounds,
            Vector3 localSink,
            Vector3[] scaleFactors)
        {
            var centerLocal = root.InverseTransformPoint(modelBounds.center);
            var pivotToCenter = centerLocal - model.localPosition;
            var sinkFactors = new[] { 0.00f, 0.09f, 0.38f, 0.80f, 0.96f, 1.02f };
            if (scaleFactors.Length != sinkFactors.Length)
            {
                throw new ArgumentException("Societas death body scale factors and sink factors must have the same length.");
            }

            var offsets = new Vector3[scaleFactors.Length];
            for (var i = 0; i < scaleFactors.Length; i++)
            {
                offsets[i] = localSink * sinkFactors[i];
                offsets[i].x += pivotToCenter.x * (1f - scaleFactors[i].x);
                offsets[i].z += pivotToCenter.z * (1f - scaleFactors[i].z);
            }

            return offsets;
        }

        private static void AddRendererEnabledCurve(
            AnimationClip clip,
            Transform root,
            Transform target,
            float[] times,
            float[] values)
        {
            if (times.Length != values.Length)
            {
                throw new ArgumentException("Renderer enabled curve times and values must have the same length.");
            }

            var path = AnimationUtility.CalculateTransformPath(target, root);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Renderer), "m_Enabled"),
                CreateStepCurve(times, values));
        }

        private static AnimationCurve CreateFloatCurve(float[] times, Vector3[] values, int componentIndex)
        {
            var keys = new Keyframe[times.Length];
            for (var i = 0; i < times.Length; i++)
            {
                var value = componentIndex switch
                {
                    0 => values[i].x,
                    2 => values[i].z,
                    _ => values[i].y
                };
                keys[i] = new Keyframe(times[i], value);
            }

            var curve = new AnimationCurve(keys);
            SetAutoTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateStepCurve(float[] times, float[] values)
        {
            var keys = new Keyframe[times.Length];
            for (var i = 0; i < times.Length; i++)
            {
                keys[i] = new Keyframe(times[i], values[i]);
            }

            var curve = new AnimationCurve(keys);
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }

            return curve;
        }

        private static void ValidateVectorCurveArrays(float[] times, Vector3[] values, string label)
        {
            if (times.Length != values.Length)
            {
                throw new ArgumentException($"Societas death {label} curve times and values must have the same length.");
            }
        }

        private static DeathMeltProxyVisuals EnsureDeathMeltProxyVisuals(Transform deathSlot, Material material)
        {
            RemoveDeathMeltProxyVisuals(deathSlot);

            var model = deathSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {AnimationReviewSlotNames[3]}.");
            }

            var rendererBounds = CalculateRendererBounds(model, new Bounds(deathSlot.position, Vector3.one * SocietasTargetHeightMeters));
            var localReferenceSize = WorldSizeToRootLocal(deathSlot, rendererBounds.size);
            var center = rendererBounds.center;
            var groundY = rendererBounds.min.y + Mathf.Max(rendererBounds.size.y * 0.018f, 0.004f);
            var height = Mathf.Max(rendererBounds.size.y, SocietasTargetHeightMeters);
            var width = Mathf.Max(rendererBounds.size.x, SocietasTargetHeightMeters * 0.70f);
            var depth = Mathf.Max(rendererBounds.size.z, SocietasTargetHeightMeters * 0.70f);
            var front = CalculateSocietasVisualFrontDirection(deathSlot);
            var right = Vector3.Cross(Vector3.up, front).normalized;
            if (right.sqrMagnitude < 0.001f)
            {
                right = deathSlot.right;
            }

            var localRotation = Quaternion.Inverse(deathSlot.rotation);
            var bodyMass = EnsureDeathMeltProxyVisual(
                deathSlot,
                DeathMeltProxyPrefix + "BodyMass",
                new Vector3(center.x, groundY + (height * 0.16f), center.z),
                localRotation,
                CreateDeathMeltDomeMesh(
                    DeathMeltProxyPrefix + "BodyMassMesh",
                    localReferenceSize,
                    radiusXMultiplier: 0.44f,
                    radiusZMultiplier: 0.36f,
                    heightMultiplier: 0.19f,
                    wobblePhase: 0.35f),
                material);
            var frontFlow = EnsureDeathMeltProxyVisual(
                deathSlot,
                DeathMeltProxyPrefix + "FrontFlow",
                center + (front * depth * 0.32f) + (Vector3.up * height * 0.34f),
                localRotation,
                CreateDeathMeltDomeMesh(
                    DeathMeltProxyPrefix + "FrontFlowMesh",
                    localReferenceSize,
                    radiusXMultiplier: 0.24f,
                    radiusZMultiplier: 0.34f,
                    heightMultiplier: 0.11f,
                    wobblePhase: 1.20f),
                material);
            var leftFlow = EnsureDeathMeltProxyVisual(
                deathSlot,
                DeathMeltProxyPrefix + "LeftFlow",
                center - (right * width * 0.34f) + (front * depth * 0.05f) + (Vector3.up * height * 0.24f),
                localRotation,
                CreateDeathMeltDomeMesh(
                    DeathMeltProxyPrefix + "LeftFlowMesh",
                    localReferenceSize,
                    radiusXMultiplier: 0.18f,
                    radiusZMultiplier: 0.32f,
                    heightMultiplier: 0.09f,
                    wobblePhase: 2.05f),
                material);
            var rightFlow = EnsureDeathMeltProxyVisual(
                deathSlot,
                DeathMeltProxyPrefix + "RightFlow",
                center + (right * width * 0.34f) + (front * depth * 0.03f) + (Vector3.up * height * 0.24f),
                localRotation,
                CreateDeathMeltDomeMesh(
                    DeathMeltProxyPrefix + "RightFlowMesh",
                    localReferenceSize,
                    radiusXMultiplier: 0.18f,
                    radiusZMultiplier: 0.32f,
                    heightMultiplier: 0.09f,
                    wobblePhase: 2.90f),
                material);
            var finalPuddle = EnsureDeathMeltProxyVisual(
                deathSlot,
                DeathMeltProxyPrefix + "FinalPuddle",
                new Vector3(center.x, groundY, center.z),
                localRotation,
                CreateDeathMeltPuddleMesh(DeathMeltProxyPrefix + "FinalPuddleMesh", localReferenceSize),
                material);

            return new DeathMeltProxyVisuals(bodyMass, frontFlow, leftFlow, rightFlow, finalPuddle);
        }

        private static void RemoveDeathMeltProxyVisuals(Transform deathSlot)
        {
            for (var i = deathSlot.childCount - 1; i >= 0; i--)
            {
                var child = deathSlot.GetChild(i);
                if (child.name.StartsWith(DeathMeltProxyPrefix, StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static Transform EnsureDeathMeltProxyVisual(
            Transform root,
            string name,
            Vector3 worldPosition,
            Quaternion localRotation,
            Mesh mesh,
            Material material)
        {
            var proxy = new GameObject(name);
            proxy.transform.SetParent(root, false);
            proxy.transform.localPosition = root.InverseTransformPoint(worldPosition);
            proxy.transform.localRotation = localRotation;
            proxy.transform.localScale = Vector3.one;

            var meshFilter = proxy.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = proxy.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.enabled = false;
            proxy.SetActive(true);

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(meshRenderer);
            EditorUtility.SetDirty(proxy);
            return proxy.transform;
        }

        private static Mesh CreateDeathMeltDomeMesh(
            string meshName,
            Vector3 localReferenceSize,
            float radiusXMultiplier,
            float radiusZMultiplier,
            float heightMultiplier,
            float wobblePhase)
        {
            const int segmentCount = 30;
            const int ringCount = 4;
            var radiusX = Mathf.Max(localReferenceSize.x * radiusXMultiplier, localReferenceSize.y * 0.12f);
            var radiusZ = Mathf.Max(localReferenceSize.z * radiusZMultiplier, localReferenceSize.y * 0.12f);
            var height = Mathf.Max(localReferenceSize.y * heightMultiplier, localReferenceSize.y * 0.045f);
            var vertices = new Vector3[1 + (ringCount * segmentCount) + 1];
            var triangles = new int[(segmentCount * 3) + ((ringCount - 1) * segmentCount * 6) + (segmentCount * 3)];

            vertices[0] = new Vector3(0f, height, 0f);
            for (var ring = 0; ring < ringCount; ring++)
            {
                var dome01 = (ring + 1f) / ringCount;
                var ringRadius = Mathf.Sin(dome01 * Mathf.PI * 0.5f);
                var ringHeight = Mathf.Cos(dome01 * Mathf.PI * 0.5f) * height;
                for (var index = 0; index < segmentCount; index++)
                {
                    var angle = (Mathf.PI * 2f * index) / segmentCount;
                    var wobble =
                        1f +
                        (Mathf.Sin((angle * 2.4f) + wobblePhase) * 0.13f) +
                        (Mathf.Cos((angle * 5.1f) - wobblePhase) * 0.07f);
                    vertices[1 + (ring * segmentCount) + index] = new Vector3(
                        Mathf.Cos(angle) * radiusX * ringRadius * wobble,
                        ringHeight,
                        Mathf.Sin(angle) * radiusZ * ringRadius * wobble);
                }
            }

            var bottomCenterIndex = vertices.Length - 1;
            vertices[bottomCenterIndex] = Vector3.zero;
            var triangleCursor = 0;
            for (var index = 0; index < segmentCount; index++)
            {
                var next = index == segmentCount - 1 ? 1 : index + 2;
                triangles[triangleCursor++] = 0;
                triangles[triangleCursor++] = index + 1;
                triangles[triangleCursor++] = next;
            }

            for (var ring = 0; ring < ringCount - 1; ring++)
            {
                var currentStart = 1 + (ring * segmentCount);
                var nextStart = currentStart + segmentCount;
                for (var index = 0; index < segmentCount; index++)
                {
                    var next = index == segmentCount - 1 ? 0 : index + 1;
                    var current = currentStart + index;
                    var currentNext = currentStart + next;
                    var lower = nextStart + index;
                    var lowerNext = nextStart + next;
                    triangles[triangleCursor++] = current;
                    triangles[triangleCursor++] = lower;
                    triangles[triangleCursor++] = currentNext;
                    triangles[triangleCursor++] = currentNext;
                    triangles[triangleCursor++] = lower;
                    triangles[triangleCursor++] = lowerNext;
                }
            }

            var bottomRingStart = 1 + ((ringCount - 1) * segmentCount);
            for (var index = 0; index < segmentCount; index++)
            {
                var next = index == segmentCount - 1 ? 0 : index + 1;
                triangles[triangleCursor++] = bottomCenterIndex;
                triangles[triangleCursor++] = bottomRingStart + next;
                triangles[triangleCursor++] = bottomRingStart + index;
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateDeathMeltPuddleMesh(string meshName, Vector3 localReferenceSize)
        {
            const int segmentCount = 44;
            var radiusX = Mathf.Max(localReferenceSize.x * 0.78f, localReferenceSize.y * 0.42f);
            var radiusZ = Mathf.Max(localReferenceSize.z * 0.62f, localReferenceSize.y * 0.36f);
            var vertices = new Vector3[segmentCount + 1];
            var triangles = new int[segmentCount * 3];
            vertices[0] = Vector3.zero;

            for (var index = 0; index < segmentCount; index++)
            {
                var angle = (Mathf.PI * 2f * index) / segmentCount;
                var frontLobe = Mathf.Max(0f, Mathf.Cos(angle - 0.22f)) * 0.20f;
                var sideLobe = Mathf.Abs(Mathf.Sin(angle * 1.08f + 0.50f)) * 0.12f;
                var wobble =
                    1f +
                    frontLobe +
                    sideLobe +
                    (Mathf.Sin(angle * 3.2f) * 0.09f) +
                    (Mathf.Cos(angle * 6.3f) * 0.06f);
                vertices[index + 1] = new Vector3(
                    Mathf.Cos(angle) * radiusX * wobble,
                    0f,
                    Mathf.Sin(angle) * radiusZ * wobble);
            }

            for (var index = 0; index < segmentCount; index++)
            {
                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index == segmentCount - 1 ? 1 : index + 2;
                triangles[triangleIndex + 2] = index + 1;
            }

            var mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddDeathMeltProxyCurves(
            AnimationClip clip,
            Transform root,
            DeathMeltProxyVisuals visuals,
            float[] times)
        {
            var model = root.Find(ModelChildName);
            var rendererBounds = model != null
                ? CalculateRendererBounds(model, new Bounds(root.position, Vector3.one * SocietasTargetHeightMeters))
                : CalculateRendererBounds(root, new Bounds(root.position, Vector3.one * SocietasTargetHeightMeters));
            var center = rendererBounds.center;
            var groundY = rendererBounds.min.y + Mathf.Max(rendererBounds.size.y * 0.018f, 0.004f);
            var height = Mathf.Max(rendererBounds.size.y, SocietasTargetHeightMeters);
            var width = Mathf.Max(rendererBounds.size.x, SocietasTargetHeightMeters * 0.70f);
            var depth = Mathf.Max(rendererBounds.size.z, SocietasTargetHeightMeters * 0.70f);
            var groundCenter = new Vector3(center.x, groundY, center.z);
            var front = CalculateSocietasVisualFrontDirection(root);
            var right = Vector3.Cross(Vector3.up, front).normalized;
            if (right.sqrMagnitude < 0.001f)
            {
                right = root.right;
            }

            AddDeathMeltProxyMotion(
                clip,
                root,
                visuals.BodyMass,
                times,
                new[]
                {
                    center + (Vector3.up * height * 0.16f),
                    center + (Vector3.up * height * 0.04f),
                    groundCenter + (Vector3.up * height * 0.060f),
                    groundCenter + (Vector3.up * height * 0.020f),
                    groundCenter + (Vector3.up * height * 0.006f),
                    groundCenter + (Vector3.up * height * 0.008f)
                },
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.48f, 0.62f, 0.48f),
                    new Vector3(1.05f, 0.82f, 0.96f),
                    new Vector3(0.86f, 0.22f, 1.08f),
                    new Vector3(0.30f, 0.04f, 0.46f),
                    new Vector3(0.01f, 0.01f, 0.01f)
                },
                new[] { 0f, 0f, 1f, 0f, 0f, 0f });
            AddDeathMeltProxyMotion(
                clip,
                root,
                visuals.FrontFlow,
                times,
                new[]
                {
                    groundCenter + (front * depth * 0.10f) + (Vector3.up * height * 0.018f),
                    groundCenter + (front * depth * 0.12f) + (Vector3.up * height * 0.016f),
                    groundCenter + (front * depth * 0.14f) + (Vector3.up * height * 0.014f),
                    groundCenter + (front * depth * 0.16f) + (Vector3.up * height * 0.010f),
                    groundCenter + (front * depth * 0.18f) + (Vector3.up * height * 0.004f),
                    groundCenter + (front * depth * 0.18f) + (Vector3.up * height * 0.004f)
                },
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.12f, 0.18f, 0.14f),
                    new Vector3(0.52f, 0.72f, 0.62f),
                    new Vector3(0.74f, 0.10f, 0.88f),
                    new Vector3(0.36f, 0.025f, 0.62f),
                    new Vector3(0.01f, 0.01f, 0.01f)
                },
                new[] { 0f, 0f, 0f, 1f, 0f, 0f });
            AddDeathMeltProxyMotion(
                clip,
                root,
                visuals.LeftFlow,
                times,
                new[]
                {
                    groundCenter - (right * width * 0.12f) + (Vector3.up * height * 0.016f),
                    groundCenter - (right * width * 0.14f) + (Vector3.up * height * 0.014f),
                    groundCenter - (right * width * 0.16f) + (Vector3.up * height * 0.012f),
                    groundCenter - (right * width * 0.18f) + (front * depth * 0.02f) + (Vector3.up * height * 0.008f),
                    groundCenter - (right * width * 0.20f) + (front * depth * 0.03f) + (Vector3.up * height * 0.004f),
                    groundCenter - (right * width * 0.20f) + (front * depth * 0.03f) + (Vector3.up * height * 0.004f)
                },
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.10f, 0.14f, 0.14f),
                    new Vector3(0.48f, 0.58f, 0.62f),
                    new Vector3(0.64f, 0.09f, 0.76f),
                    new Vector3(0.28f, 0.020f, 0.48f),
                    new Vector3(0.01f, 0.01f, 0.01f)
                },
                new[] { 0f, 0f, 0f, 1f, 0f, 0f });
            AddDeathMeltProxyMotion(
                clip,
                root,
                visuals.RightFlow,
                times,
                new[]
                {
                    groundCenter + (right * width * 0.12f) + (Vector3.up * height * 0.016f),
                    groundCenter + (right * width * 0.14f) + (Vector3.up * height * 0.014f),
                    groundCenter + (right * width * 0.16f) + (Vector3.up * height * 0.012f),
                    groundCenter + (right * width * 0.18f) + (front * depth * 0.02f) + (Vector3.up * height * 0.008f),
                    groundCenter + (right * width * 0.20f) + (front * depth * 0.03f) + (Vector3.up * height * 0.004f),
                    groundCenter + (right * width * 0.20f) + (front * depth * 0.03f) + (Vector3.up * height * 0.004f)
                },
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.10f, 0.14f, 0.14f),
                    new Vector3(0.48f, 0.58f, 0.62f),
                    new Vector3(0.64f, 0.09f, 0.76f),
                    new Vector3(0.28f, 0.020f, 0.48f),
                    new Vector3(0.01f, 0.01f, 0.01f)
                },
                new[] { 0f, 0f, 0f, 1f, 0f, 0f });
            AddDeathMeltProxyMotion(
                clip,
                root,
                visuals.FinalPuddle,
                times,
                CreateRepeatedWorldPositions(groundCenter, times.Length),
                new[]
                {
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.01f, 0.01f, 0.01f),
                    new Vector3(0.22f, 0.03f, 0.18f),
                    new Vector3(0.56f, 0.04f, 0.50f),
                    new Vector3(0.88f, 0.05f, 0.84f),
                    new Vector3(1.00f, 0.05f, 1.00f)
                },
                new[] { 0f, 0f, 1f, 1f, 1f, 1f });
        }

        private static void AddDeathMeltProxyMotion(
            AnimationClip clip,
            Transform root,
            Transform proxy,
            float[] times,
            Vector3[] worldPositions,
            Vector3[] scaleFactors,
            float[] enabledValues)
        {
            ValidateVectorCurveArrays(times, worldPositions, "proxy position");
            ValidateVectorCurveArrays(times, scaleFactors, "proxy scale");
            if (times.Length != enabledValues.Length)
            {
                throw new ArgumentException("Societas death proxy enabled curve times and values must have the same length.");
            }

            var localPositions = new Vector3[times.Length];
            for (var i = 0; i < times.Length; i++)
            {
                localPositions[i] = root.InverseTransformPoint(worldPositions[i]);
            }

            AddLocalPositionAbsoluteCurves(clip, root, proxy, times, localPositions);
            AddLocalScaleFactorCurves(clip, root, proxy, times, scaleFactors);
            AddRendererEnabledCurve(clip, root, proxy, times, enabledValues);
        }

        private static Vector3[] CreateRepeatedWorldPositions(Vector3 position, int count)
        {
            var values = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = position;
            }

            return values;
        }

        private static Vector3 WorldSizeToRootLocal(Transform root, Vector3 worldSize)
        {
            var scale = root.lossyScale;
            return new Vector3(
                worldSize.x / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                worldSize.y / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                worldSize.z / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
        }

        private static void SetAutoTangents(AnimationCurve curve)
        {
            for (var i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
        }

        private static float IdleOrganicPhase(int index, int salt)
        {
            return IdleOrganicNoise01(index, salt) * Mathf.PI * 2f;
        }

        private static float IdleSignedScale(int index, int salt)
        {
            return Mathf.Lerp(0.78f, 1.18f, IdleOrganicNoise01(index, salt)) * (IdleOrganicNoise01(index, salt + 3) < 0.5f ? -1f : 1f);
        }

        private static float IdleOrganicNoise01(int index, int salt)
        {
            unchecked
            {
                var value = (uint)(index + 1) * 747796405u + (uint)(salt + 71) * 2891336453u;
                value ^= value >> 16;
                value *= 2246822519u;
                value ^= value >> 13;
                value *= 3266489917u;
                value ^= value >> 16;
                return (value & 0x00FFFFFFu) / 16777216f;
            }
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            return new Vector3(NormalizeEulerDegrees(euler.x), NormalizeEulerDegrees(euler.y), NormalizeEulerDegrees(euler.z));
        }

        private static float NormalizeEulerDegrees(float degrees)
        {
            while (degrees > 180f)
            {
                degrees -= 360f;
            }

            while (degrees < -180f)
            {
                degrees += 360f;
            }

            return degrees;
        }

        private static void CopyPreparedModelAsset()
        {
            CopyFileToAsset(SourceModelAbsolutePath, UnityModelAssetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureImportedModelAsset()
        {
            ConfigureImportedModelAsset(UnityModelAssetPath);
        }

        private static void ConfigureImportedModelAsset(string modelAssetPath)
        {
            var modelImporter = AssetImporter.GetAtPath(modelAssetPath) as ModelImporter;
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
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.globalScale = 1f;
            modelImporter.SaveAndReimport();
        }

        private static GameObject EnsureAttackConsumeModelAssetImported()
        {
            RequireAttackConsumeModelFile();
            EnsureUnityFolders();
            CopyFileToAsset(AttackConsumeSourceModelAbsolutePath, AttackConsumeModelAssetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(AttackConsumeModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImportedModelAsset(AttackConsumeModelAssetPath);
            return LoadModelAsset(AttackConsumeModelAssetPath, "Societas attack consume GLB");
        }

        private static GameObject LoadPreparedModelAsset()
        {
            return LoadModelAsset(UnityModelAssetPath, "Societas GLB");
        }

        private static GameObject LoadModelAsset(string modelAssetPath, string label)
        {
            var glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
            if (glbAsset != null)
            {
                return glbAsset;
            }

            throw new InvalidOperationException(
                $"Could not load {label} as a Unity model asset. GLB path={modelAssetPath}.");
        }

        private static Material EnsureReferenceMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnityMaterialAssetPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = "M_Societas_Glossy_Green_Body"
                };
                AssetDatabase.CreateAsset(material, UnityMaterialAssetPath);
            }

            SetMaterialColor(material, SocietasGlossyGreenColor);
            SetMaterialFloat(material, "_Smoothness", 0.88f);
            SetMaterialFloat(material, "_Glossiness", 0.88f);
            SetMaterialFloat(material, "_Metallic", 0f);
            if (material.HasProperty("_SpecColor"))
            {
                material.SetColor("_SpecColor", new Color(0.22f, 0.55f, 0.30f, 1f));
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject PlacePreparedModel(GameObject modelAsset, Material material, Scene scene)
        {
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var urzereRoot = RequireSceneRoot(UrzerePlacementRootName);
            var spacing = CalculateTergoLongaSpacing(tergoRoot.transform, longaRoot.transform);
            var placementPosition = new Vector3(
                urzereRoot.transform.position.x,
                urzereRoot.transform.position.y,
                urzereRoot.transform.position.z - spacing);

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
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, SocietasFacingYawDegrees, 0f);
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
                throw new InvalidOperationException("Societas prepared model contains no renderers.");
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
                var scaleFactor = Mathf.Clamp(SocietasTargetHeightMeters / bounds.size.y, 0.001f, 100f);
                root.localScale = Vector3.one * scaleFactor;
            }

            bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            root.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static void ConfigureReviewCamera(Transform placementRoot)
        {
            var focus = FindSocietasCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var camera = FindOrCreateReviewCamera();
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.12f, 0.03f, 0.12f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.25f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var verticalOffset = Mathf.Clamp(bounds.extents.y * 0.22f, 0.05f, 0.18f);
            var position = lookAt + frontDirection * distance + Vector3.up * verticalOffset;

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = distance + Mathf.Max(bounds.extents.x, bounds.extents.z) + 12.00f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.12f, 0.12f, 1f);
            camera.orthographic = false;
            camera.fieldOfView = 32f;
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(camera.transform);

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(lookAt, camera.transform.rotation, distance, false, true);
            }
        }

        private static void ConfigurePlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = FindSocietasCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.12f);
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
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

            var focus = FindSocietasCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.12f);
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
            var previousPosition = player.position;
            var offset = previousPosition - lookAt;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.001f)
            {
                offset = frontDirection * ReviewPlayerFrontDistance;
            }

            if (Vector3.Dot(offset.normalized, frontDirection.normalized) > -0.70f)
            {
                offset = -offset;
            }

            var startPosition = new Vector3(
                lookAt.x + offset.x,
                0f,
                lookAt.z + offset.z);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
            Debug.Log($"Societas player start opposite side update. Previous={previousPosition}, New={startPosition}, Center={lookAt}.");
        }

        private static void InspectSceneState(Transform placementRoot)
        {
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var urzereRoot = RequireSceneRoot(UrzerePlacementRootName);
            var spacing = CalculateTergoLongaSpacing(tergoRoot.transform, longaRoot.transform);
            var expectedPosition = new Vector3(
                urzereRoot.transform.position.x,
                urzereRoot.transform.position.y,
                urzereRoot.transform.position.z - spacing);

            if (Vector3.Distance(placementRoot.position, expectedPosition) > 0.05f)
            {
                throw new InvalidOperationException($"Societas placement root is not at the approved position. Expected={expectedPosition}, Actual={placementRoot.position}.");
            }

            var staticObject = placementRoot.Find(PlacementObjectName);
            if (staticObject == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            var model = staticObject.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {PlacementObjectName}.");
            }

            var renderers = staticObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Societas prepared model contains no renderers.");
            }

            var bounds = CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            if (Mathf.Abs(bounds.size.y - SocietasTargetHeightMeters) > 0.035f)
            {
                throw new InvalidOperationException($"Societas height must be close to {SocietasTargetHeightMeters:0.##}m. Actual={bounds.size.y:0.###}m.");
            }

            var camera = FindReviewCamera();
            if (camera == null)
            {
                throw new InvalidOperationException($"{ReviewCameraName} is missing.");
            }

            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.12f);
            var frontDirection = CalculateSocietasVisualFrontDirection(staticObject);
            RequireFrontSideView(camera.transform.position, lookAt, frontDirection, ReviewCameraName);

            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            RequireBackSideView(player.position, lookAt, frontDirection, PlayerRootName);
            RequireFacingTarget(player, lookAt, PlayerRootName);

            Debug.Log(
                $"SocietasSceneState Root={PlacementRootName}, Object={PlacementObjectName}, Model={UnityModelAssetPath}, Position={placementRoot.position}, TergoLongaSpacing={spacing:0.###}, BoundsSize={bounds.size}, RendererCount={renderers.Length}.");
        }

        private static void EnsureAnimationReviewSlots(Transform placementRoot, GameObject modelAsset, Material material)
        {
            var staticObject = RequireStaticReviewObject(placementRoot);
            var staticBounds = CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            var spacing = Mathf.Max(Mathf.Max(staticBounds.size.x, staticBounds.size.z) * 1.65f, AnimationSlotMinimumSpacing);

            for (var i = 0; i < AnimationReviewSlotNames.Length; i++)
            {
                var existingSlot = placementRoot.Find(AnimationReviewSlotNames[i]);
                if (existingSlot != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingSlot.gameObject);
                }

                CreateAnimationReviewSlot(
                    placementRoot,
                    modelAsset,
                    material,
                    AnimationReviewSlotNames[i],
                    new Vector3(spacing * (i + 1), 0f, 0f));
            }

            EditorUtility.SetDirty(placementRoot);
        }

        private static Transform CreateAnimationReviewSlot(
            Transform placementRoot,
            GameObject modelAsset,
            Material material,
            string slotName,
            Vector3 localPosition)
        {
            var slotObject = new GameObject(slotName);
            slotObject.transform.SetParent(placementRoot, false);
            slotObject.transform.localPosition = localPosition;
            slotObject.transform.localRotation = Quaternion.Euler(0f, SocietasFacingYawDegrees, 0f);
            slotObject.transform.localScale = Vector3.one;

            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = ModelChildName;
            modelInstance.transform.SetParent(slotObject.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            DisableImportedAnimationPlayback(slotObject.transform);
            AssignMaterial(slotObject.transform, material);
            ScaleToTargetHeightAndAlignToGround(slotObject.transform, placementRoot.position.y);

            EditorUtility.SetDirty(slotObject);
            EditorUtility.SetDirty(modelInstance);
            return slotObject.transform;
        }

        private static void ApplyAttackConsumeModelToSlot(
            Transform attackSlot,
            GameObject modelAsset,
            Material material,
            float groundY)
        {
            attackSlot.localScale = Vector3.one;

            var existingModel = attackSlot.Find(ModelChildName);
            if (existingModel != null)
            {
                UnityEngine.Object.DestroyImmediate(existingModel.gameObject);
            }

            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = ModelChildName;
            modelInstance.transform.SetParent(attackSlot, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            DisableImportedAnimationPlayback(attackSlot);
            AssignMaterial(attackSlot, material);
            ScaleToTargetHeightAndAlignToGround(attackSlot, groundY);

            EditorUtility.SetDirty(attackSlot);
            EditorUtility.SetDirty(modelInstance);
        }

        private static void InspectAnimationReviewSlots(Transform placementRoot)
        {
            InspectSceneState(placementRoot);
            var staticObject = RequireStaticReviewObject(placementRoot);
            RequireSocietasSlotVisual(staticObject, PlacementObjectName);

            for (var i = 0; i < AnimationReviewSlotNames.Length; i++)
            {
                var slot = placementRoot.Find(AnimationReviewSlotNames[i]);
                if (slot == null)
                {
                    throw new InvalidOperationException($"{AnimationReviewSlotNames[i]} is missing under {PlacementRootName}.");
                }

                RequireSocietasSlotVisual(slot, AnimationReviewSlotNames[i]);
                if (Vector3.Distance(slot.position, staticObject.position) < 0.30f)
                {
                    throw new InvalidOperationException($"{slot.name} overlaps the Societas static review slot.");
                }
            }

            Debug.Log(
                $"SocietasAnimationSlots Root={PlacementRootName}, Static={PlacementObjectName}, Slots={string.Join(",", AnimationReviewSlotNames)}.");
        }

        private static void InspectIdleBreathTentacleAnimation(Transform placementRoot)
        {
            InspectAnimationReviewSlots(placementRoot);

            var idleSlot = RequireAnimationReviewSlot(placementRoot, AnimationReviewSlotNames[0]);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathTentacleClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Societas idle clip is missing at {IdleBreathTentacleClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(IdleBreathTentacleControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Societas idle controller is missing at {IdleBreathTentacleControllerPath}.");
            }

            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[0]} must have the Societas idle AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[0]} idle Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Societas idle breath tentacle clip must loop.");
            }

            var bodyControls = CollectBodyMorphTargets(idleSlot);
            if (bodyControls.Count == 0)
            {
                throw new InvalidOperationException("Societas idle validation could not find a body morph target.");
            }

            var tentacleControls = CollectTentacleRigTargets(idleSlot);
            if (tentacleControls.Count < 3)
            {
                throw new InvalidOperationException("Societas idle validation could not find enough mouth/front tentacle controls.");
            }

            foreach (var control in bodyControls)
            {
                var path = AnimationUtility.CalculateTransformPath(control, idleSlot);
                RequireCurveDelta(clip, path, "m_LocalScale.x", Mathf.Max(Mathf.Abs(control.localScale.x) * 0.040f, 0.0001f), "Societas idle body X breath");
                RequireCurveDelta(clip, path, "m_LocalScale.y", Mathf.Max(Mathf.Abs(control.localScale.y) * 0.0025f, 0.0001f), "Societas idle body Y compression");
                RequireCurveDelta(clip, path, "m_LocalScale.z", Mathf.Max(Mathf.Abs(control.localScale.z) * 0.044f, 0.0001f), "Societas idle body Z breath");
                RequireCurveDelta(clip, path, "m_LocalPosition.y", 0.00004f, "Societas idle body vertical morph");
                RequireCurveMaxDelta(clip, path, "m_LocalPosition.y", 0.0002f, "Societas idle body vertical morph");
            }

            var animatedTentacles = 0;
            foreach (var control in tentacleControls)
            {
                var path = AnimationUtility.CalculateTransformPath(control, idleSlot);
                if (HasCurveDelta(clip, path, "m_LocalPosition.x", 0.006f) &&
                    HasCurveDelta(clip, path, "localEulerAnglesRaw.x", 3.0f))
                {
                    animatedTentacles++;
                }
            }

            if (animatedTentacles < Mathf.Min(4, tentacleControls.Count))
            {
                throw new InvalidOperationException($"Societas idle tentacle controls are under-animated. Animated={animatedTentacles}, Total={tentacleControls.Count}.");
            }

            RejectRootTransformCurves(clip);
            RejectControllerOnOtherSocietasSlots(placementRoot, controller, AnimationReviewSlotNames[0], "Societas idle controller");

            Debug.Log(
                $"SocietasIdleBreathTentacles Slot={AnimationReviewSlotNames[0]}, BodyControls={bodyControls.Count}, TentacleControls={tentacleControls.Count}, AnimatedTentacles={animatedTentacles}, CurveBindings={AnimationUtility.GetCurveBindings(clip).Length}.");
        }

        private static void InspectMoveCaterpillarAnimation(Transform placementRoot)
        {
            InspectAnimationReviewSlots(placementRoot);

            var moveSlot = RequireAnimationReviewSlot(placementRoot, AnimationReviewSlotNames[1]);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveCaterpillarClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Societas move clip is missing at {MoveCaterpillarClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MoveCaterpillarControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Societas move controller is missing at {MoveCaterpillarControllerPath}.");
            }

            var animator = moveSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[1]} must have the Societas move AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[1]} move Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Societas move caterpillar clip must loop.");
            }

            var bodyControls = CollectMoveCaterpillarRigTargets(moveSlot);
            if (bodyControls.Count < 5)
            {
                throw new InvalidOperationException("Societas move validation could not find enough body rig controls.");
            }

            var animatedBody = 0;
            foreach (var control in bodyControls)
            {
                var path = AnimationUtility.CalculateTransformPath(control, moveSlot);
                RequireCurveDelta(clip, path, "m_LocalScale.z", Mathf.Max(Mathf.Abs(control.localScale.z) * 0.052f, 0.0001f), "Societas move body longitudinal pulse");
                RequireCurveDelta(clip, path, "m_LocalPosition.y", 0.0040f, "Societas move body lift wave");
                RequireCurveDelta(clip, path, "localEulerAnglesRaw.x", 4.0f, "Societas move body curl");

                if (HasCurveDelta(clip, path, "m_LocalScale.z", Mathf.Max(Mathf.Abs(control.localScale.z) * 0.052f, 0.0001f)) &&
                    HasCurveDelta(clip, path, "m_LocalPosition.z", 0.0050f) &&
                    HasCurveDelta(clip, path, "localEulerAnglesRaw.x", 4.0f))
                {
                    animatedBody++;
                }
            }

            if (animatedBody < Mathf.Min(5, bodyControls.Count))
            {
                throw new InvalidOperationException($"Societas move body controls are under-animated. Animated={animatedBody}, Total={bodyControls.Count}.");
            }

            var tentacleControls = CollectTentacleRigTargets(moveSlot);
            if (tentacleControls.Count < 3)
            {
                throw new InvalidOperationException("Societas move validation could not find enough front pull controls.");
            }

            var animatedTentacles = 0;
            foreach (var control in tentacleControls)
            {
                var path = AnimationUtility.CalculateTransformPath(control, moveSlot);
                if (HasCurveDelta(clip, path, "m_LocalPosition.z", 0.0080f) &&
                    HasCurveDelta(clip, path, "localEulerAnglesRaw.x", 5.0f))
                {
                    animatedTentacles++;
                }
            }

            if (animatedTentacles < Mathf.Min(3, tentacleControls.Count))
            {
                throw new InvalidOperationException($"Societas move front controls are under-animated. Animated={animatedTentacles}, Total={tentacleControls.Count}.");
            }

            RejectRootTransformCurves(clip);
            RejectControllerOnOtherSocietasSlots(placementRoot, controller, AnimationReviewSlotNames[1], "Societas move controller");

            Debug.Log(
                $"SocietasMoveCaterpillar Slot={AnimationReviewSlotNames[1]}, BodyControls={bodyControls.Count}, TentacleControls={tentacleControls.Count}, AnimatedBody={animatedBody}, AnimatedTentacles={animatedTentacles}, CurveBindings={AnimationUtility.GetCurveBindings(clip).Length}.");
        }

        private static void InspectAttackConsumeBiteChewAnimation(Transform placementRoot)
        {
            InspectAnimationReviewSlots(placementRoot);

            var attackSlot = RequireAnimationReviewSlot(placementRoot, AnimationReviewSlotNames[2]);
            RequireAttackConsumeEatingModelSlot(attackSlot);
            RequireSocietasMaterial(attackSlot, EnsureReferenceMaterial());

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackConsumeBiteChewClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Societas attack consume clip is missing at {AttackConsumeBiteChewClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AttackConsumeBiteChewControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Societas attack consume controller is missing at {AttackConsumeBiteChewControllerPath}.");
            }

            var animator = attackSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[2]} must have the Societas attack consume AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[2]} attack consume Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Societas attack consume bite chew clip must loop for review.");
            }

            var mouthControls = CollectAttackConsumeMouthRigTargets(attackSlot);
            if (mouthControls.Count < 1)
            {
                throw new InvalidOperationException("Societas attack consume validation could not find an upper body rig control.");
            }

            var animatedMouth = 0;
            foreach (var control in mouthControls)
            {
                var path = AnimationUtility.CalculateTransformPath(control, attackSlot);
                RequireAnyPositionCurveDelta(clip, path, 0.020f, "Societas attack consume upper body lift/slam");
                RejectCurveDelta(clip, path, "localEulerAnglesRaw.x", 0.0001f, "Societas attack consume upper body X rotation deformation");
                RejectCurveDelta(clip, path, "localEulerAnglesRaw.y", 0.0001f, "Societas attack consume upper body Y rotation deformation");
                RejectCurveDelta(clip, path, "localEulerAnglesRaw.z", 0.0001f, "Societas attack consume upper body Z rotation deformation");
                RejectCurveDelta(clip, path, "m_LocalScale.x", 0.0001f, "Societas attack consume upper teeth X scale deformation");
                RejectCurveDelta(clip, path, "m_LocalScale.y", 0.0001f, "Societas attack consume upper teeth Y scale deformation");
                RejectCurveDelta(clip, path, "m_LocalScale.z", 0.0001f, "Societas attack consume upper teeth Z scale deformation");
                if (HasAnyPositionCurveDelta(clip, path, 0.020f))
                {
                    animatedMouth++;
                }
            }

            if (animatedMouth < mouthControls.Count)
            {
                throw new InvalidOperationException($"Societas attack consume upper body controls are under-animated. Animated={animatedMouth}, Total={mouthControls.Count}.");
            }

            var bodyControls = CollectAttackConsumeBodyRigTargets(attackSlot);
            foreach (var control in bodyControls)
            {
                var path = AnimationUtility.CalculateTransformPath(control, attackSlot);
                RejectCurveDelta(clip, path, "m_LocalPosition.x", 0.001f, "Societas attack consume body lateral bend");
                RejectCurveDelta(clip, path, "m_LocalPosition.y", 0.003f, "Societas attack consume body vertical bend");
                RejectCurveDelta(clip, path, "m_LocalPosition.z", 0.003f, "Societas attack consume body forward bend");
                RejectCurveDelta(clip, path, "m_LocalScale.x", Mathf.Max(Mathf.Abs(control.localScale.x) * 0.006f, 0.0001f), "Societas attack consume body X scale pulse");
                RejectCurveDelta(clip, path, "m_LocalScale.y", Mathf.Max(Mathf.Abs(control.localScale.y) * 0.006f, 0.0001f), "Societas attack consume body Y scale pulse");
                RejectCurveDelta(clip, path, "m_LocalScale.z", Mathf.Max(Mathf.Abs(control.localScale.z) * 0.006f, 0.0001f), "Societas attack consume body Z scale pulse");
                RejectCurveDelta(clip, path, "localEulerAnglesRaw.x", 1.0f, "Societas attack consume body curl bend");
                RejectCurveDelta(clip, path, "localEulerAnglesRaw.y", 1.0f, "Societas attack consume body yaw bend");
                RejectCurveDelta(clip, path, "localEulerAnglesRaw.z", 1.0f, "Societas attack consume body roll bend");
            }

            RejectRootTransformCurves(clip);
            RejectControllerOnOtherSocietasSlots(placementRoot, controller, AnimationReviewSlotNames[2], "Societas attack consume controller");

            Debug.Log(
                $"SocietasAttackConsumeUpperBodyLiftSlam Slot={AnimationReviewSlotNames[2]}, UpperBodyControls={mouthControls.Count}, BodyControlsChecked={bodyControls.Count}, AnimatedUpperBody={animatedMouth}, BodyBraceCurves=0, CurveBindings={AnimationUtility.GetCurveBindings(clip).Length}.");
        }

        private static void InspectDeathMeltPuddleAnimation(Transform placementRoot)
        {
            InspectAnimationReviewSlots(placementRoot);

            var deathSlot = RequireDeathAnimationSlot(placementRoot);
            RequireSocietasMaterial(deathSlot, EnsureReferenceMaterial());

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathMeltPuddleClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Societas death melt puddle clip is missing at {DeathMeltPuddleClipPath}.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DeathMeltPuddleControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException($"Societas death melt puddle controller is missing at {DeathMeltPuddleControllerPath}.");
            }

            var animator = deathSlot.GetComponent<Animator>();
            if (animator == null || !animator.enabled || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[3]} must have the Societas death AnimatorController assigned on the slot root.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[3]} death Animator must not use root motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Societas death melt puddle clip must loop for review.");
            }

            var model = deathSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {AnimationReviewSlotNames[3]}.");
            }

            var modelPath = AnimationUtility.CalculateTransformPath(model, deathSlot);
            RequireCurveDelta(clip, modelPath, "m_LocalScale.y", 0.85f, "Societas death body vertical melt flattening");
            RequireCurveDelta(
                clip,
                modelPath,
                "m_LocalPosition.y",
                Mathf.Max(Mathf.Abs(deathSlot.InverseTransformVector(Vector3.up * 0.05f).y), 0.001f),
                "Societas death body sinking");

            var modelRendererCurves = 0;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (HasRendererEnabledCurve(clip, deathSlot, renderer.transform))
                {
                    modelRendererCurves++;
                }
            }

            if (modelRendererCurves == 0)
            {
                throw new InvalidOperationException("Societas death clip must hide the melted original body renderers at the final puddle frame.");
            }

            var finalPuddle = deathSlot.Find(DeathMeltProxyPrefix + "FinalPuddle");
            if (finalPuddle == null)
            {
                throw new InvalidOperationException("Societas death final puddle proxy is missing.");
            }

            var finalPuddlePath = AnimationUtility.CalculateTransformPath(finalPuddle, deathSlot);
            RequireCurveDelta(clip, finalPuddlePath, "m_LocalScale.x", 0.75f, "Societas death final puddle horizontal spread");
            RequireCurveDelta(clip, finalPuddlePath, "m_LocalScale.z", 0.75f, "Societas death final puddle depth spread");
            if (!HasRendererEnabledCurve(clip, deathSlot, finalPuddle))
            {
                throw new InvalidOperationException("Societas death final puddle proxy must have a renderer enabled curve.");
            }

            RejectRootTransformCurves(clip);
            RejectControllerOnOtherSocietasSlots(placementRoot, controller, AnimationReviewSlotNames[3], "Societas death controller");

            Debug.Log(
                $"SocietasDeathMeltPuddle Slot={AnimationReviewSlotNames[3]}, ProxyVisuals={CountDeathMeltProxyVisuals(deathSlot)}, BodyRendererEnabledCurves={modelRendererCurves}, CurveBindings={AnimationUtility.GetCurveBindings(clip).Length}.");
        }

        private static void InspectIdleRigStructure(Transform idleSlot)
        {
            var bounds = CalculateRendererBounds(idleSlot, new Bounds(idleSlot.position, Vector3.one));
            var renderers = idleSlot.GetComponentsInChildren<Renderer>(true);
            var rendererSummaries = new List<string>();
            foreach (var renderer in renderers)
            {
                var skinned = renderer as SkinnedMeshRenderer;
                var mesh = skinned != null ? skinned.sharedMesh : null;
                rendererSummaries.Add(
                    $"{renderer.GetType().Name}:{AnimationUtility.CalculateTransformPath(renderer.transform, idleSlot)}:Bounds={renderer.bounds.size}:Bones={(skinned != null && skinned.bones != null ? skinned.bones.Length : 0)}:BlendShapes={(mesh != null ? mesh.blendShapeCount : 0)}");
            }

            var bodyTargets = CollectBodyMorphTargets(idleSlot);
            var tentacleTargets = CollectTentacleRigTargets(idleSlot);
            Debug.Log(
                $"SocietasIdleRigStructure Slot={AnimationReviewSlotNames[0]}, BoundsMin={bounds.min}, BoundsMax={bounds.max}, BoundsSize={bounds.size}, Renderers={string.Join("|", rendererSummaries)}.");
            Debug.Log("SocietasIdleRigBodyTargets " + FormatRigTargetSummary(idleSlot, bounds, bodyTargets));
            Debug.Log("SocietasIdleRigTentacleTargets " + FormatRigTargetSummary(idleSlot, bounds, tentacleTargets));
        }

        private static void InspectAttackConsumeRigStructure(Transform attackSlot)
        {
            var bounds = CalculateRendererBounds(attackSlot, new Bounds(attackSlot.position, Vector3.one));
            var renderers = attackSlot.GetComponentsInChildren<Renderer>(true);
            var rendererSummaries = new List<string>();
            foreach (var renderer in renderers)
            {
                var skinned = renderer as SkinnedMeshRenderer;
                var mesh = skinned != null ? skinned.sharedMesh : null;
                rendererSummaries.Add(
                    $"{renderer.GetType().Name}:{AnimationUtility.CalculateTransformPath(renderer.transform, attackSlot)}:Bounds={renderer.bounds.size}:Bones={(skinned != null && skinned.bones != null ? skinned.bones.Length : 0)}:BlendShapes={(mesh != null ? mesh.blendShapeCount : 0)}");
            }

            var mouthTargets = CollectAttackConsumeMouthRigTargets(attackSlot);
            var bodyTargets = CollectAttackConsumeBodyRigTargets(attackSlot);
            Debug.Log(
                $"SocietasAttackConsumeRigStructure Slot={AnimationReviewSlotNames[2]}, BoundsMin={bounds.min}, BoundsMax={bounds.max}, BoundsSize={bounds.size}, Renderers={string.Join("|", rendererSummaries)}.");
            Debug.Log("SocietasAttackConsumeMouthTargets " + FormatRigTargetSummary(attackSlot, bounds, mouthTargets));
            Debug.Log("SocietasAttackConsumeBodyTargets " + FormatRigTargetSummary(attackSlot, bounds, bodyTargets));
        }

        private static string FormatRigTargetSummary(Transform idleSlot, Bounds bounds, IReadOnlyList<Transform> targets)
        {
            var summaries = new List<string>();
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var height01 = Mathf.InverseLerp(bounds.min.y, bounds.max.y, target.position.y);
                summaries.Add(
                    $"{i}:{target.name}:{AnimationUtility.CalculateTransformPath(target, idleSlot)}:Height01={height01:0.###}:LocalPos={target.localPosition}:World={target.position}");
            }

            return string.Join("|", summaries);
        }

        private static Transform RequireAnimationReviewSlot(Transform placementRoot, string slotName)
        {
            var slot = RequireAnimationReviewSlotRoot(placementRoot, slotName);
            RequireSocietasSlotVisual(slot, slotName);
            return slot;
        }

        private static Transform RequireAnimationReviewSlotRoot(Transform placementRoot, string slotName)
        {
            var slot = placementRoot.Find(slotName);
            if (slot == null)
            {
                throw new InvalidOperationException($"{slotName} is missing under {PlacementRootName}.");
            }

            return slot;
        }

        private static Transform RequireDeathAnimationSlot(Transform placementRoot)
        {
            var slot = RequireAnimationReviewSlotRoot(placementRoot, AnimationReviewSlotNames[3]);

            var model = slot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {AnimationReviewSlotNames[3]}.");
            }

            if (model.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[3]} contains no Societas model renderers.");
            }

            return slot;
        }

        private static List<Transform> CollectNamedTransforms(Transform root, IReadOnlyList<string> names)
        {
            var matches = new List<Transform>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (ContainsName(names, transform.name))
                {
                    matches.Add(transform);
                }
            }

            matches.Sort((left, right) =>
                string.Compare(
                    AnimationUtility.CalculateTransformPath(left, root),
                    AnimationUtility.CalculateTransformPath(right, root),
                    StringComparison.Ordinal));
            return matches;
        }

        private static List<Transform> CollectBodyMorphTargets(Transform idleSlot)
        {
            var named = CollectNamedTransforms(idleSlot, IdleBodyMorphControlNames);
            if (named.Count > 0)
            {
                return named;
            }

            return CollectUpperBodyRigTargets(idleSlot);
        }

        private static List<Transform> CollectUpperBodyRigTargets(Transform idleSlot)
        {
            var bounds = CalculateRendererBounds(idleSlot, new Bounds(idleSlot.position, Vector3.one));
            var frontDirection = CalculateSocietasVisualFrontDirection(idleSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = idleSlot.right;
            }

            var radius = Mathf.Max(bounds.extents.x, bounds.extents.z, 0.001f);
            var candidates = new List<RigCandidate>();
            foreach (var transform in idleSlot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == idleSlot || !IsBoneLikeTransform(transform))
                {
                    continue;
                }

                var height01 = Mathf.InverseLerp(bounds.min.y, bounds.max.y, transform.position.y);
                if (height01 < 0.55f || CalculateRelativeDepth(idleSlot, transform) < 5)
                {
                    continue;
                }

                var horizontalOffset = transform.position - bounds.center;
                horizontalOffset.y = 0f;
                var frontDistance = Mathf.Abs(Vector3.Dot(horizontalOffset, frontDirection)) / radius;
                var lateralDistance = Mathf.Abs(Vector3.Dot(horizontalOffset, rightDirection)) / radius;
                var score = height01 * 2.40f - frontDistance * 0.28f - lateralDistance * 0.42f + IdleOrganicNoise01(candidates.Count, 97) * 0.02f;
                candidates.Add(new RigCandidate(transform, score));
            }

            if (candidates.Count == 0)
            {
                foreach (var transform in idleSlot.GetComponentsInChildren<Transform>(true))
                {
                    if (transform == idleSlot || !IsBoneLikeTransform(transform))
                    {
                        continue;
                    }

                    var height01 = Mathf.InverseLerp(bounds.min.y, bounds.max.y, transform.position.y);
                    candidates.Add(new RigCandidate(transform, height01 + IdleOrganicNoise01(candidates.Count, 101) * 0.01f));
                }
            }

            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(candidates.Count, 5);
            var selected = new List<Transform>(count);
            for (var i = 0; i < count; i++)
            {
                selected.Add(candidates[i].Transform);
            }

            selected.Sort((left, right) =>
                string.Compare(
                    AnimationUtility.CalculateTransformPath(left, idleSlot),
                    AnimationUtility.CalculateTransformPath(right, idleSlot),
                    StringComparison.Ordinal));
            return selected;
        }

        private static List<Transform> CollectTentacleRigTargets(Transform idleSlot)
        {
            var named = CollectNamedTransforms(idleSlot, IdleTentacleControlNames);
            if (named.Count >= 3)
            {
                return named;
            }

            var bounds = CalculateRendererBounds(idleSlot, new Bounds(idleSlot.position, Vector3.one));
            var frontDirection = CalculateSocietasVisualFrontDirection(idleSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = idleSlot.right;
            }

            var candidates = new List<RigCandidate>();
            foreach (var transform in idleSlot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == idleSlot || !IsBoneLikeTransform(transform))
                {
                    continue;
                }

                var horizontalOffset = transform.position - bounds.center;
                horizontalOffset.y = 0f;
                var frontDistance = Vector3.Dot(horizontalOffset, frontDirection);
                var lateralDistance = Mathf.Abs(Vector3.Dot(horizontalOffset, rightDirection));
                if (frontDistance < -Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.20f)
                {
                    continue;
                }

                var height01 = Mathf.InverseLerp(bounds.min.y, bounds.max.y, transform.position.y);
                if (height01 < 0.38f || CalculateRelativeDepth(idleSlot, transform) < 5)
                {
                    continue;
                }

                var mouthBandScore = 1f - Mathf.Abs(height01 - 0.42f);
                var score = frontDistance * 2.70f - lateralDistance * 0.34f + mouthBandScore * 0.10f + CalculateRelativeDepth(idleSlot, transform) * 0.018f + IdleOrganicNoise01(candidates.Count, 83) * 0.01f;
                candidates.Add(new RigCandidate(transform, score));
            }

            if (candidates.Count == 0)
            {
                foreach (var transform in idleSlot.GetComponentsInChildren<Transform>(true))
                {
                    if (transform != idleSlot && IsBoneLikeTransform(transform))
                    {
                        candidates.Add(new RigCandidate(transform, IdleOrganicNoise01(candidates.Count, 89)));
                    }
                }
            }

            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
            var count = Mathf.Min(candidates.Count, 8);
            var selected = new List<Transform>(count);
            for (var i = 0; i < count; i++)
            {
                selected.Add(candidates[i].Transform);
            }

            selected.Sort((left, right) =>
                string.Compare(
                    AnimationUtility.CalculateTransformPath(left, idleSlot),
                    AnimationUtility.CalculateTransformPath(right, idleSlot),
                    StringComparison.Ordinal));
            return selected;
        }

        private static List<Transform> CollectMoveCaterpillarRigTargets(Transform moveSlot)
        {
            var candidates = CollectMoveCaterpillarCandidates(moveSlot, minimumDepth: 4, minimumHeight01: 0.16f);
            if (candidates.Count < 5)
            {
                candidates = CollectMoveCaterpillarCandidates(moveSlot, minimumDepth: 5, minimumHeight01: 0.20f);
            }

            candidates.Sort((left, right) => left.Front01.CompareTo(right.Front01));
            var desiredCount = Mathf.Min(candidates.Count, 12);
            var selected = new List<MoveRigCandidate>(desiredCount);
            var used = new HashSet<Transform>();

            for (var i = 0; i < desiredCount; i++)
            {
                var targetFront01 = desiredCount > 1 ? i / (float)(desiredCount - 1) : 0.5f;
                var bestIndex = -1;
                var bestScore = float.PositiveInfinity;
                for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    var candidate = candidates[candidateIndex];
                    if (used.Contains(candidate.Transform))
                    {
                        continue;
                    }

                    var score = Mathf.Abs(candidate.Front01 - targetFront01) - candidate.Height01 * 0.07f - candidate.Depth * 0.004f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = candidateIndex;
                    }
                }

                if (bestIndex < 0)
                {
                    break;
                }

                var selectedCandidate = candidates[bestIndex];
                selected.Add(selectedCandidate);
                used.Add(selectedCandidate.Transform);
            }

            selected.Sort((left, right) => left.Front01.CompareTo(right.Front01));
            var transforms = new List<Transform>(selected.Count);
            for (var i = 0; i < selected.Count; i++)
            {
                transforms.Add(selected[i].Transform);
            }

            return transforms;
        }

        private static List<MoveRigCandidate> CollectMoveCaterpillarCandidates(Transform moveSlot, int minimumDepth, float minimumHeight01)
        {
            var bounds = CalculateRendererBounds(moveSlot, new Bounds(moveSlot.position, Vector3.one));
            var frontDirection = CalculateSocietasVisualFrontDirection(moveSlot);
            var radius = Mathf.Max(bounds.extents.x, bounds.extents.z, 0.001f);
            var candidates = new List<MoveRigCandidate>();
            foreach (var transform in moveSlot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == moveSlot || !IsBoneLikeTransform(transform))
                {
                    continue;
                }

                var depth = CalculateRelativeDepth(moveSlot, transform);
                if (depth < minimumDepth)
                {
                    continue;
                }

                var height01 = Mathf.InverseLerp(bounds.min.y, bounds.max.y, transform.position.y);
                if (height01 < minimumHeight01 || height01 > 0.86f)
                {
                    continue;
                }

                var frontDistance = Vector3.Dot(transform.position - bounds.center, frontDirection) / radius;
                var front01 = Mathf.Clamp01((frontDistance + 1f) * 0.5f);
                candidates.Add(new MoveRigCandidate(transform, front01, height01, depth));
            }

            return candidates;
        }

        private static List<Transform> CollectAttackConsumeMouthRigTargets(Transform attackSlot)
        {
            var eatingMouthBones = CollectNamedTransforms(attackSlot, AttackConsumeEatingMouthBoneNames);
            if (eatingMouthBones.Count >= 1)
            {
                return eatingMouthBones;
            }

            var namedTargets = new List<Transform>();
            foreach (var transform in attackSlot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == attackSlot || !IsBoneLikeTransform(transform))
                {
                    continue;
                }

                if (ContainsNameFragment(AttackConsumeMouthNameFragments, transform.name))
                {
                    namedTargets.Add(transform);
                }
            }

            if (namedTargets.Count >= 3)
            {
                namedTargets.Sort((left, right) =>
                    string.Compare(
                        AnimationUtility.CalculateTransformPath(left, attackSlot),
                        AnimationUtility.CalculateTransformPath(right, attackSlot),
                        StringComparison.Ordinal));
                return LimitTransformList(namedTargets, 10);
            }

            var bounds = CalculateRendererBounds(attackSlot, new Bounds(attackSlot.position, Vector3.one));
            var frontDirection = CalculateSocietasVisualFrontDirection(attackSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = attackSlot.right;
            }

            var radius = Mathf.Max(bounds.extents.x, bounds.extents.z, 0.001f);
            var candidates = new List<RigCandidate>();
            foreach (var transform in attackSlot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == attackSlot || !IsBoneLikeTransform(transform))
                {
                    continue;
                }

                var depth = CalculateRelativeDepth(attackSlot, transform);
                if (depth < 7)
                {
                    continue;
                }

                var offset = transform.position - bounds.center;
                offset.y = 0f;
                var frontDistance = Vector3.Dot(offset, frontDirection);
                var lateralDistance = Mathf.Abs(Vector3.Dot(offset, rightDirection));
                var height01 = Mathf.InverseLerp(bounds.min.y, bounds.max.y, transform.position.y);
                if (height01 < 0.54f || height01 > 0.92f || frontDistance < radius * 0.04f)
                {
                    continue;
                }

                var mouthBandScore = 1f - Mathf.Abs(height01 - 0.74f);
                var score = frontDistance / radius * 3.50f - lateralDistance / radius * 0.42f + mouthBandScore * 0.90f + depth * 0.085f + IdleOrganicNoise01(candidates.Count, 263) * 0.02f;
                candidates.Add(new RigCandidate(transform, score));
            }

            if (candidates.Count < 3)
            {
                foreach (var transform in attackSlot.GetComponentsInChildren<Transform>(true))
                {
                    if (transform == attackSlot || !IsBoneLikeTransform(transform))
                    {
                        continue;
                    }

                    var depth = CalculateRelativeDepth(attackSlot, transform);
                    if (depth < 7)
                    {
                        continue;
                    }

                    var height01 = Mathf.InverseLerp(bounds.min.y, bounds.max.y, transform.position.y);
                    if (height01 < 0.50f || height01 > 0.94f)
                    {
                        continue;
                    }

                    candidates.Add(new RigCandidate(transform, depth + IdleOrganicNoise01(candidates.Count, 269)));
                }
            }

            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
            var selectedCount = Mathf.Min(candidates.Count, 8);
            var selected = new List<Transform>(selectedCount);
            for (var i = 0; i < selectedCount; i++)
            {
                selected.Add(candidates[i].Transform);
            }

            selected.Sort((left, right) =>
                string.Compare(
                    AnimationUtility.CalculateTransformPath(left, attackSlot),
                    AnimationUtility.CalculateTransformPath(right, attackSlot),
                    StringComparison.Ordinal));
            return selected;
        }

        private static List<Transform> CollectAttackConsumeBodyRigTargets(Transform attackSlot)
        {
            var mouthTargets = CollectAttackConsumeMouthRigTargets(attackSlot);
            var mouthSet = new HashSet<Transform>(mouthTargets);
            var bodyTargets = CollectMoveCaterpillarRigTargets(attackSlot);
            bodyTargets.RemoveAll(mouthSet.Contains);

            if (bodyTargets.Count >= 3)
            {
                return LimitTransformList(bodyTargets, 8);
            }

            var fallback = CollectUpperBodyRigTargets(attackSlot);
            fallback.RemoveAll(mouthSet.Contains);
            if (fallback.Count >= 3)
            {
                return LimitTransformList(fallback, 8);
            }

            foreach (var transform in attackSlot.GetComponentsInChildren<Transform>(true))
            {
                if (transform != attackSlot && IsBoneLikeTransform(transform) && !mouthSet.Contains(transform) && !bodyTargets.Contains(transform))
                {
                    bodyTargets.Add(transform);
                }
            }

            bodyTargets.Sort((left, right) =>
                string.Compare(
                    AnimationUtility.CalculateTransformPath(left, attackSlot),
                    AnimationUtility.CalculateTransformPath(right, attackSlot),
                    StringComparison.Ordinal));
            return LimitTransformList(bodyTargets, 8);
        }

        private static void InspectAttackConsumeBoneWeights(Transform attackSlot)
        {
            var skinnedRenderer = attackSlot.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedRenderer == null || skinnedRenderer.sharedMesh == null)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[2]} requires a SkinnedMeshRenderer with a mesh for bone weight inspection.");
            }

            var mesh = skinnedRenderer.sharedMesh;
            var bones = skinnedRenderer.bones;
            if (bones == null || bones.Length == 0)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[2]} SkinnedMeshRenderer has no bones.");
            }

            var vertices = mesh.vertices;
            var boneWeights = mesh.boneWeights;
            if (vertices == null || boneWeights == null || vertices.Length == 0 || boneWeights.Length != vertices.Length)
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[2]} mesh has invalid vertex or bone weight data.");
            }

            var localPoints = new Vector3[vertices.Length];
            var localBounds = new Bounds(attackSlot.InverseTransformPoint(skinnedRenderer.transform.TransformPoint(vertices[0])), Vector3.zero);
            for (var i = 0; i < vertices.Length; i++)
            {
                var localPoint = attackSlot.InverseTransformPoint(skinnedRenderer.transform.TransformPoint(vertices[i]));
                localPoints[i] = localPoint;
                localBounds.Encapsulate(localPoint);
            }

            var summaries = new BoneWeightInfluenceSummary[bones.Length];
            for (var i = 0; i < bones.Length; i++)
            {
                summaries[i] = new BoneWeightInfluenceSummary(bones[i]);
            }

            for (var i = 0; i < vertices.Length; i++)
            {
                var weight = boneWeights[i];
                AddBoneWeightInfluence(summaries, weight.boneIndex0, weight.weight0, localPoints[i], localBounds);
                AddBoneWeightInfluence(summaries, weight.boneIndex1, weight.weight1, localPoints[i], localBounds);
                AddBoneWeightInfluence(summaries, weight.boneIndex2, weight.weight2, localPoints[i], localBounds);
                AddBoneWeightInfluence(summaries, weight.boneIndex3, weight.weight3, localPoints[i], localBounds);
            }

            var lines = new List<string>
            {
                $"SocietasAttackConsumeBoneWeightSummary Slot={AnimationReviewSlotNames[2]} Mesh={mesh.name} Vertices={vertices.Length} Bones={bones.Length} LocalBoundsMin={localBounds.min} LocalBoundsMax={localBounds.max} LocalBoundsSize={localBounds.size}.",
                "BlendShapes " + FormatBlendShapeSummary(mesh),
                "ModelSubAssets " + FormatModelSubAssetSummary(AttackConsumeModelAssetPath),
                "TopTotalWeight " + FormatBoneWeightTopList(attackSlot, summaries, BoneWeightSortMode.TotalWeight, 12),
                "TopFrontMinusZCenter " + FormatBoneWeightTopList(attackSlot, summaries, BoneWeightSortMode.FrontMinusZCenter, 12),
                "TopFrontPlusZCenter " + FormatBoneWeightTopList(attackSlot, summaries, BoneWeightSortMode.FrontPlusZCenter, 12),
                "TopLeftX " + FormatBoneWeightTopList(attackSlot, summaries, BoneWeightSortMode.LeftX, 8),
                "TopRightX " + FormatBoneWeightTopList(attackSlot, summaries, BoneWeightSortMode.RightX, 8),
                "NamedMouthCandidates " + FormatNamedBoneWeightSummaries(attackSlot, summaries, AttackConsumeMouthNameFragments)
            };

            foreach (var line in lines)
            {
                Debug.Log(line);
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AttackConsumeValidationFolder));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, "Societas_AttackConsume_BoneWeightSummary_20260708.txt");
            File.WriteAllLines(outputPath, lines);
            Debug.Log("SocietasAttackConsumeBoneWeightSummaryPath " + outputPath);
        }

        private static string FormatBlendShapeSummary(Mesh mesh)
        {
            if (mesh.blendShapeCount <= 0)
            {
                return "Count=0";
            }

            var names = new List<string>();
            for (var i = 0; i < mesh.blendShapeCount; i++)
            {
                names.Add(mesh.GetBlendShapeName(i));
            }

            return $"Count={mesh.blendShapeCount};Names={string.Join("|", names)}";
        }

        private static string FormatModelSubAssetSummary(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            if (assets == null || assets.Length == 0)
            {
                return "Count=0";
            }

            var parts = new List<string>();
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] == null)
                {
                    continue;
                }

                parts.Add($"{assets[i].GetType().Name}:{assets[i].name}");
            }

            return $"Count={parts.Count};Assets={string.Join("|", parts)}";
        }

        private static void AddBoneWeightInfluence(
            IReadOnlyList<BoneWeightInfluenceSummary> summaries,
            int boneIndex,
            float weight,
            Vector3 localPoint,
            Bounds localBounds)
        {
            if (boneIndex < 0 || boneIndex >= summaries.Count || weight <= 0f)
            {
                return;
            }

            var extents = localBounds.extents;
            var center = localBounds.center;
            var radiusX = Mathf.Max(extents.x, 0.0001f);
            var radiusZ = Mathf.Max(extents.z, 0.0001f);
            var height01 = Mathf.InverseLerp(localBounds.min.y, localBounds.max.y, localPoint.y);
            var centerX01 = 1f - Mathf.Clamp01(Mathf.Abs(localPoint.x - center.x) / radiusX);
            var mouthHeight01 = 1f - Mathf.Clamp01(Mathf.Abs(height01 - 0.50f) / 0.34f);
            var minusZ01 = Mathf.Clamp01((center.z - localPoint.z) / radiusZ);
            var plusZ01 = Mathf.Clamp01((localPoint.z - center.z) / radiusZ);
            var leftX01 = Mathf.Clamp01((center.x - localPoint.x) / radiusX);
            var rightX01 = Mathf.Clamp01((localPoint.x - center.x) / radiusX);

            summaries[boneIndex].Add(
                weight,
                localPoint,
                minusZ01 * centerX01 * mouthHeight01,
                plusZ01 * centerX01 * mouthHeight01,
                leftX01 * mouthHeight01,
                rightX01 * mouthHeight01);
        }

        private static string FormatNamedBoneWeightSummaries(
            Transform root,
            IReadOnlyList<BoneWeightInfluenceSummary> summaries,
            IReadOnlyList<string> nameFragments)
        {
            var selected = new List<BoneWeightInfluenceSummary>();
            for (var i = 0; i < summaries.Count; i++)
            {
                if (summaries[i].Bone != null && (summaries[i].Bone.name.StartsWith("Bone_", StringComparison.Ordinal) || ContainsNameFragment(nameFragments, summaries[i].Bone.name)))
                {
                    selected.Add(summaries[i]);
                }
            }

            selected.Sort((left, right) => string.Compare(left.Bone.name, right.Bone.name, StringComparison.Ordinal));
            return FormatBoneWeightList(root, selected, selected.Count);
        }

        private static string FormatBoneWeightTopList(
            Transform root,
            IReadOnlyList<BoneWeightInfluenceSummary> summaries,
            BoneWeightSortMode sortMode,
            int maxCount)
        {
            var selected = new List<BoneWeightInfluenceSummary>();
            for (var i = 0; i < summaries.Count; i++)
            {
                if (summaries[i].TotalWeight > 0.0001f)
                {
                    selected.Add(summaries[i]);
                }
            }

            selected.Sort((left, right) => BoneWeightScore(right, sortMode).CompareTo(BoneWeightScore(left, sortMode)));
            return FormatBoneWeightList(root, selected, Mathf.Min(maxCount, selected.Count));
        }

        private static float BoneWeightScore(BoneWeightInfluenceSummary summary, BoneWeightSortMode sortMode)
        {
            return sortMode switch
            {
                BoneWeightSortMode.FrontMinusZCenter => summary.FrontMinusZCenter,
                BoneWeightSortMode.FrontPlusZCenter => summary.FrontPlusZCenter,
                BoneWeightSortMode.LeftX => summary.LeftX,
                BoneWeightSortMode.RightX => summary.RightX,
                _ => summary.TotalWeight
            };
        }

        private static string FormatBoneWeightList(Transform root, IReadOnlyList<BoneWeightInfluenceSummary> summaries, int count)
        {
            var parts = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var summary = summaries[i];
                if (summary.Bone == null)
                {
                    continue;
                }

                parts.Add(
                    $"{i}:{summary.Bone.name}:{AnimationUtility.CalculateTransformPath(summary.Bone, root)}:Total={summary.TotalWeight:0.###}:MinusZ={summary.FrontMinusZCenter:0.###}:PlusZ={summary.FrontPlusZCenter:0.###}:LeftX={summary.LeftX:0.###}:RightX={summary.RightX:0.###}:Centroid={summary.Centroid}");
            }

            return string.Join("|", parts);
        }

        private static List<Transform> LimitTransformList(List<Transform> transforms, int maxCount)
        {
            if (transforms.Count <= maxCount)
            {
                return transforms;
            }

            return transforms.GetRange(0, maxCount);
        }

        private static bool IsBoneLikeTransform(Transform transform)
        {
            return transform.name.StartsWith("Bone_", StringComparison.Ordinal) ||
                   transform.name.StartsWith("DEF_", StringComparison.Ordinal) ||
                   transform.name.StartsWith("CTRL_", StringComparison.Ordinal);
        }

        private static int CalculateRelativeDepth(Transform root, Transform transform)
        {
            var depth = 0;
            var current = transform;
            while (current != null && current != root)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static bool ContainsName(IReadOnlyList<string> names, string candidate)
        {
            for (var i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], candidate, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsNameFragment(IReadOnlyList<string> fragments, string candidate)
        {
            for (var i = 0; i < fragments.Count; i++)
            {
                if (candidate.IndexOf(fragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RequireCurveDelta(AnimationClip clip, string path, string propertyName, float minimumDelta, string label)
        {
            if (HasCurveDelta(clip, path, propertyName, minimumDelta))
            {
                return;
            }

            throw new InvalidOperationException($"{label} curve must change by at least {minimumDelta:0.###}: {path}/{propertyName}.");
        }

        private static void RequireAnyPositionCurveDelta(AnimationClip clip, string path, float minimumDelta, string label)
        {
            if (HasAnyPositionCurveDelta(clip, path, minimumDelta))
            {
                return;
            }

            throw new InvalidOperationException($"{label} curve must change by at least {minimumDelta:0.###}: {path}/m_LocalPosition.*.");
        }

        private static void RequireAnyEulerCurveDelta(AnimationClip clip, string path, float minimumDelta, string label)
        {
            if (HasAnyEulerCurveDelta(clip, path, minimumDelta))
            {
                return;
            }

            throw new InvalidOperationException($"{label} curve must change by at least {minimumDelta:0.###}: {path}/localEulerAnglesRaw.*.");
        }

        private static void RequireCurveMaxDelta(AnimationClip clip, string path, string propertyName, float maximumDelta, string label)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
            if (curve == null || curve.length == 0)
            {
                throw new InvalidOperationException($"{label} curve is missing: {path}/{propertyName}.");
            }

            var min = curve.keys[0].value;
            var max = curve.keys[0].value;
            foreach (var key in curve.keys)
            {
                min = Mathf.Min(min, key.value);
                max = Mathf.Max(max, key.value);
            }

            if (max - min > maximumDelta)
            {
                throw new InvalidOperationException($"{label} curve must not change by more than {maximumDelta:0.###}: {path}/{propertyName}.");
            }
        }

        private static bool HasCurveDelta(AnimationClip clip, string path, string propertyName, float minimumDelta)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
            if (curve == null || curve.length == 0)
            {
                return false;
            }

            var min = curve.keys[0].value;
            var max = curve.keys[0].value;
            foreach (var key in curve.keys)
            {
                min = Mathf.Min(min, key.value);
                max = Mathf.Max(max, key.value);
            }

            return max - min >= minimumDelta;
        }

        private static bool HasRendererEnabledCurve(AnimationClip clip, Transform root, Transform target)
        {
            var path = AnimationUtility.CalculateTransformPath(target, root);
            var curve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Renderer), "m_Enabled"));
            return curve != null && curve.length > 0;
        }

        private static int CountDeathMeltProxyVisuals(Transform deathSlot)
        {
            var count = 0;
            for (var i = 0; i < deathSlot.childCount; i++)
            {
                if (deathSlot.GetChild(i).name.StartsWith(DeathMeltProxyPrefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasAnyPositionCurveDelta(AnimationClip clip, string path, float minimumDelta)
        {
            return HasCurveDelta(clip, path, "m_LocalPosition.x", minimumDelta)
                || HasCurveDelta(clip, path, "m_LocalPosition.y", minimumDelta)
                || HasCurveDelta(clip, path, "m_LocalPosition.z", minimumDelta);
        }

        private static bool HasAnyEulerCurveDelta(AnimationClip clip, string path, float minimumDelta)
        {
            return HasCurveDelta(clip, path, "localEulerAnglesRaw.x", minimumDelta)
                || HasCurveDelta(clip, path, "localEulerAnglesRaw.y", minimumDelta)
                || HasCurveDelta(clip, path, "localEulerAnglesRaw.z", minimumDelta);
        }

        private static void RejectCurveDelta(AnimationClip clip, string path, string propertyName, float maximumAllowedDelta, string label)
        {
            if (!HasCurveDelta(clip, path, propertyName, maximumAllowedDelta))
            {
                return;
            }

            throw new InvalidOperationException($"{label} curve must stay below {maximumAllowedDelta:0.###}: {path}/{propertyName}.");
        }

        private static void RejectRootTransformCurves(AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (string.IsNullOrEmpty(binding.path) &&
                    (binding.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal) ||
                     binding.propertyName.StartsWith("m_LocalScale", StringComparison.Ordinal) ||
                     binding.propertyName.StartsWith("localEulerAnglesRaw", StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException($"Societas animation clip must not animate the slot root Transform: {binding.propertyName}.");
                }
            }
        }

        private static void RejectControllerOnOtherSocietasSlots(
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

            for (var i = 0; i < AnimationReviewSlotNames.Length; i++)
            {
                var slotName = AnimationReviewSlotNames[i];
                if (string.Equals(slotName, allowedSlotName, StringComparison.Ordinal))
                {
                    continue;
                }

                var slot = placementRoot.Find(slotName);
                if (slot == null)
                {
                    continue;
                }

                var animator = slot.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController == controller)
                {
                    throw new InvalidOperationException($"{slotName} must not use the {controllerLabel}.");
                }
            }
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

        private static void RequireSocietasSlotVisual(Transform slot, string label)
        {
            var model = slot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {label}.");
            }

            var renderers = slot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"{label} contains no Societas renderers.");
            }

            var bounds = CalculateRendererBounds(slot, new Bounds(slot.position, Vector3.one));
            if (Mathf.Abs(bounds.size.y - SocietasTargetHeightMeters) > 0.035f)
            {
                throw new InvalidOperationException($"{label} height must be close to {SocietasTargetHeightMeters:0.##}m. Actual={bounds.size.y:0.###}m.");
            }
        }

        private static void RequireAttackConsumeEatingModelSlot(Transform attackSlot)
        {
            var model = attackSlot.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {AnimationReviewSlotNames[2]}.");
            }

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject);
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                if (source != null)
                {
                    prefabPath = AssetDatabase.GetAssetPath(source);
                }
            }

            if (!string.Equals(prefabPath, AttackConsumeModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{AnimationReviewSlotNames[2]} must use the base Societas GLB model. Expected={AttackConsumeModelAssetPath}, Actual={prefabPath}.");
            }
        }

        private static void RequireSocietasMaterial(Transform slot, Material material)
        {
            var renderers = slot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"{slot.name} contains no renderers for material validation.");
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    throw new InvalidOperationException($"{slot.name} renderer {renderer.name} has no material.");
                }

                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != material)
                    {
                        throw new InvalidOperationException($"{slot.name} renderer {renderer.name} material must match {UnityMaterialAssetPath}.");
                    }
                }
            }
        }

        private static Bounds CalculateAnimationSlotsBounds(Transform placementRoot)
        {
            var bounds = CalculateRendererBounds(RequireStaticReviewObject(placementRoot), new Bounds(placementRoot.position, Vector3.one));
            for (var i = 0; i < AnimationReviewSlotNames.Length; i++)
            {
                var slot = placementRoot.Find(AnimationReviewSlotNames[i]);
                if (slot == null)
                {
                    continue;
                }

                bounds.Encapsulate(CalculateRendererBounds(slot, new Bounds(slot.position, Vector3.one)));
            }

            return bounds;
        }

        private static void ConfigureAnimationSlotsCaptureCamera(Camera camera, Transform placementRoot, Bounds bounds)
        {
            var focus = RequireStaticReviewObject(placementRoot);
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.12f, 0.03f, 0.12f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 2.60f, 3.20f, 8.50f);
            var position = lookAt + frontDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.22f, 0.05f, 0.24f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 3.10f, 0.72f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 40f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void CaptureIdleBreathTentacleReviewFrames(Transform idleSlot)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathTentacleClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Societas idle clip is missing at {IdleBreathTentacleClipPath}.");
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", IdleValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var savedTransforms = CaptureTransformSnapshots(idleSlot);
            var cameraObject = new GameObject("SocietasIdle_CaptureCamera");
            var lightObject = new GameObject("SocietasIdle_CaptureLight");
            var captures = new List<Texture2D>();
            var capturePaths = new List<string>();

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureIdleCaptureCamera(camera, idleSlot, CalculateRendererBounds(idleSlot, new Bounds(idleSlot.position, Vector3.one)));

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.32f;
                light.transform.rotation = Quaternion.Euler(46f, idleSlot.eulerAngles.y - 34f, 0f);

                var sampleTimes = new[] { 0.00f, 0.53f, 1.07f, 1.60f, 2.13f, 2.67f };
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    clip.SampleAnimation(idleSlot.gameObject, sampleTimes[i]);
                    var outputPath = Path.Combine(outputDirectory, $"Societas_01_Idle_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureCameraTexture(camera, 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    captures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var contactSheetPath = Path.Combine(outputDirectory, "Societas_01_Idle_ContactSheet.png");
                SaveContactSheet(captures, contactSheetPath);
                capturePaths.Add(contactSheetPath);
            }
            finally
            {
                RestoreTransformSnapshots(savedTransforms);
                foreach (var capture in captures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("SocietasIdleCapture Paths=" + string.Join(";", capturePaths));
        }

        private static void CaptureMoveCaterpillarReviewFrames(Transform moveSlot)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveCaterpillarClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Societas move clip is missing at {MoveCaterpillarClipPath}.");
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", MoveValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var savedTransforms = CaptureTransformSnapshots(moveSlot);
            var cameraObject = new GameObject("SocietasMove_CaptureCamera");
            var lightObject = new GameObject("SocietasMove_CaptureLight");
            var captures = new List<Texture2D>();
            var capturePaths = new List<string>();

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureMoveCaptureCamera(camera, moveSlot, CalculateRendererBounds(moveSlot, new Bounds(moveSlot.position, Vector3.one)));

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.34f;
                light.transform.rotation = Quaternion.Euler(46f, moveSlot.eulerAngles.y - 38f, 0f);

                var sampleTimes = new[] { 0.00f, 0.40f, 0.80f, 1.20f, 1.60f, 2.00f };
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    clip.SampleAnimation(moveSlot.gameObject, sampleTimes[i]);
                    var outputPath = Path.Combine(outputDirectory, $"Societas_02_Move_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureCameraTexture(camera, 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    captures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var contactSheetPath = Path.Combine(outputDirectory, "Societas_02_Move_ContactSheet.png");
                SaveContactSheet(captures, contactSheetPath);
                capturePaths.Add(contactSheetPath);
            }
            finally
            {
                RestoreTransformSnapshots(savedTransforms);
                foreach (var capture in captures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("SocietasMoveCapture Paths=" + string.Join(";", capturePaths));
        }

        private static void CaptureAttackConsumeBiteChewReviewFrames(Transform attackSlot)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackConsumeBiteChewClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Societas attack consume clip is missing at {AttackConsumeBiteChewClipPath}.");
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AttackConsumeValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var savedTransforms = CaptureTransformSnapshots(attackSlot);
            var siblingRendererSnapshots = CaptureSiblingRendererSnapshots(attackSlot);
            var cameraObject = new GameObject("SocietasAttackConsume_CaptureCamera");
            var lightObject = new GameObject("SocietasAttackConsume_CaptureLight");
            var captures = new List<Texture2D>();
            var closeCaptures = new List<Texture2D>();
            var capturePaths = new List<string>();
            var animator = attackSlot.GetComponent<Animator>();
            var animatorWasEnabled = animator != null && animator.enabled;

            try
            {
                HideSiblingRenderersForCapture(siblingRendererSnapshots);
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureAttackConsumeCaptureCamera(camera, attackSlot, CalculateRendererBounds(attackSlot, new Bounds(attackSlot.position, Vector3.one)));

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.36f;
                light.transform.rotation = Quaternion.Euler(46f, attackSlot.eulerAngles.y - 36f, 0f);

                var sampleTimes = new[] { 0.00f, 0.24f, 0.40f, 0.50f, 0.56f, 1.00f };
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    RestoreTransformSnapshots(savedTransforms);
                    var outputPath = Path.Combine(outputDirectory, $"Societas_03_AttackConsume_UpperBodyLiftSlam_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureSampledAttackConsumeFrame(camera, attackSlot, clip, animator, sampleTimes[i], 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    captures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var contactSheetPath = Path.Combine(outputDirectory, "Societas_03_AttackConsume_UpperBodyLiftSlam_ContactSheet.png");
                SaveContactSheet(captures, contactSheetPath);
                capturePaths.Add(contactSheetPath);

                ConfigureAttackConsumeCloseCaptureCamera(camera, attackSlot, CalculateRendererBounds(attackSlot, new Bounds(attackSlot.position, Vector3.one)));
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    RestoreTransformSnapshots(savedTransforms);
                    var outputPath = Path.Combine(outputDirectory, $"Societas_03_AttackConsume_UpperBodyLiftSlam_Close_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureSampledAttackConsumeFrame(camera, attackSlot, clip, animator, sampleTimes[i], 1200, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    closeCaptures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var closeContactSheetPath = Path.Combine(outputDirectory, "Societas_03_AttackConsume_UpperBodyLiftSlam_Close_ContactSheet.png");
                SaveContactSheet(closeCaptures, closeContactSheetPath);
                capturePaths.Add(closeContactSheetPath);
            }
            finally
            {
                RestoreSiblingRendererSnapshots(siblingRendererSnapshots);
                RestoreTransformSnapshots(savedTransforms);
                if (animator != null)
                {
                    animator.enabled = animatorWasEnabled;
                }

                foreach (var capture in captures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                foreach (var capture in closeCaptures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("SocietasAttackConsumeCapture Paths=" + string.Join(";", capturePaths));
        }

        private static void CaptureDeathMeltPuddleReviewFrames(Transform deathSlot)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathMeltPuddleClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Societas death melt puddle clip is missing at {DeathMeltPuddleClipPath}.");
            }

            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", DeathValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var savedTransforms = CaptureTransformSnapshots(deathSlot);
            var rendererSnapshots = CaptureRendererSnapshots(deathSlot);
            var siblingRendererSnapshots = CaptureSiblingRendererSnapshots(deathSlot);
            var cameraObject = new GameObject("SocietasDeathMeltPuddle_CaptureCamera");
            var lightObject = new GameObject("SocietasDeathMeltPuddle_CaptureLight");
            var captures = new List<Texture2D>();
            var capturePaths = new List<string>();
            var animator = deathSlot.GetComponent<Animator>();
            var animatorWasEnabled = animator != null && animator.enabled;

            try
            {
                HideSiblingRenderersForCapture(siblingRendererSnapshots);
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureDeathMeltPuddleCaptureCamera(camera, deathSlot, CalculateRendererBounds(deathSlot, new Bounds(deathSlot.position, Vector3.one)));

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.34f;
                light.transform.rotation = Quaternion.Euler(48f, deathSlot.eulerAngles.y - 32f, 0f);

                var sampleTimes = new[] { 0.00f, 0.32f, 0.70f, 1.10f, 1.55f, DeathMeltPuddleDurationSeconds };
                for (var i = 0; i < sampleTimes.Length; i++)
                {
                    RestoreTransformSnapshots(savedTransforms);
                    RestoreRendererSnapshots(rendererSnapshots);
                    var outputPath = Path.Combine(outputDirectory, $"Societas_04_Death_MeltPuddle_Frame_{i:00}_{Mathf.RoundToInt(sampleTimes[i] * 1000f):0000}ms.png");
                    var texture = CaptureSampledDeathMeltPuddleFrame(camera, deathSlot, clip, animator, sampleTimes[i], 1400, 900);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    captures.Add(texture);
                    capturePaths.Add(outputPath);
                }

                var contactSheetPath = Path.Combine(outputDirectory, "Societas_04_Death_MeltPuddle_ContactSheet.png");
                SaveContactSheet(captures, contactSheetPath);
                capturePaths.Add(contactSheetPath);
            }
            finally
            {
                RestoreSiblingRendererSnapshots(siblingRendererSnapshots);
                RestoreRendererSnapshots(rendererSnapshots);
                RestoreTransformSnapshots(savedTransforms);
                if (animator != null)
                {
                    animator.enabled = animatorWasEnabled;
                }

                foreach (var capture in captures)
                {
                    UnityEngine.Object.DestroyImmediate(capture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("SocietasDeathMeltPuddleCapture Paths=" + string.Join(";", capturePaths));
        }

        private static Texture2D CaptureSampledAttackConsumeFrame(
            Camera camera,
            Transform attackSlot,
            AnimationClip clip,
            Animator animator,
            float time,
            int width,
            int height)
        {
            SampleAttackConsumeAnimatorForCapture(attackSlot, clip, animator, time);
            SampleTransformCurvesDirectly(clip, attackSlot, time);
            ForceSkinnedRendererCaptureRefresh(attackSlot);
            return CaptureCameraTexture(camera, width, height);
        }

        private static Texture2D CaptureSampledDeathMeltPuddleFrame(
            Camera camera,
            Transform deathSlot,
            AnimationClip clip,
            Animator animator,
            float time,
            int width,
            int height)
        {
            SampleDeathMeltPuddleAnimatorForCapture(deathSlot, clip, animator, time);
            SampleTransformCurvesDirectly(clip, deathSlot, time);
            SampleRendererEnabledCurvesDirectly(clip, deathSlot, time);
            ForceSkinnedRendererCaptureRefresh(deathSlot);
            return CaptureCameraTexture(camera, width, height);
        }

        private static void ForceSkinnedRendererCaptureRefresh(Transform root)
        {
            foreach (var skinnedRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var wasEnabled = skinnedRenderer.enabled;
                skinnedRenderer.updateWhenOffscreen = true;
                skinnedRenderer.enabled = false;
                skinnedRenderer.enabled = wasEnabled;
                EditorUtility.SetDirty(skinnedRenderer);
            }
        }

        private static void ConfigureIdleCaptureCamera(Camera camera, Transform idleSlot, Bounds bounds)
        {
            var frontDirection = CalculateSocietasVisualFrontDirection(idleSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = idleSlot.right;
            }

            var viewDirection = (frontDirection * 0.88f + rightDirection * 0.22f).normalized;
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.11f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.00f, 1.35f, 3.00f);
            var position = lookAt + viewDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.20f, 0.04f, 0.16f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 2.85f, 0.48f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void ConfigureMoveCaptureCamera(Camera camera, Transform moveSlot, Bounds bounds)
        {
            var frontDirection = CalculateSocietasVisualFrontDirection(moveSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = moveSlot.right;
            }

            var viewDirection = (frontDirection * 0.58f + rightDirection * 0.82f).normalized;
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.03f, 0.10f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.20f, 1.45f, 3.10f);
            var position = lookAt + viewDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.18f, 0.04f, 0.15f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.85f, 0.28f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void ConfigureAttackConsumeCaptureCamera(Camera camera, Transform attackSlot, Bounds bounds)
        {
            var frontDirection = CalculateSocietasVisualFrontDirection(attackSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = attackSlot.right;
            }

            var viewDirection = (frontDirection * 0.97f + rightDirection * 0.14f).normalized;
            var lookAt = bounds.center +
                frontDirection * Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.24f, 0.03f, 0.08f) +
                Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.02f, 0.09f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.00f, 1.35f, 2.90f);
            var position = lookAt + viewDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.20f, 0.04f, 0.16f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.45f, 0.22f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void ConfigureAttackConsumeCloseCaptureCamera(Camera camera, Transform attackSlot, Bounds bounds)
        {
            var frontDirection = CalculateSocietasVisualFrontDirection(attackSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = attackSlot.right;
            }

            var viewDirection = (frontDirection * 0.42f + rightDirection * 0.91f).normalized;
            var forwardExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            var lookAt = bounds.center +
                frontDirection * Mathf.Clamp(forwardExtent * 0.18f, 0.03f, 0.08f) +
                Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.02f, 0.08f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 3.05f, 0.95f, 2.00f);
            var position = lookAt + viewDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.18f, 0.04f, 0.12f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.12f, 0.20f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static void ConfigureDeathMeltPuddleCaptureCamera(Camera camera, Transform deathSlot, Bounds bounds)
        {
            var frontDirection = CalculateSocietasVisualFrontDirection(deathSlot);
            var rightDirection = Vector3.Cross(Vector3.up, frontDirection).normalized;
            if (rightDirection.sqrMagnitude < 0.001f)
            {
                rightDirection = deathSlot.right;
            }

            var viewDirection = (frontDirection * 0.86f + rightDirection * 0.24f).normalized;
            var horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            var lookAt = bounds.center +
                frontDirection * Mathf.Clamp(horizontalExtent * 0.10f, 0.02f, 0.07f) -
                Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.00f, 0.04f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.20f, 1.20f, 2.85f);
            var position = lookAt + viewDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.20f, 0.04f, 0.14f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(
                bounds.extents.y * 1.30f,
                horizontalExtent * 1.34f,
                0.28f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static TransformSnapshot[] CaptureTransformSnapshots(Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var snapshots = new TransformSnapshot[transforms.Length];
            for (var i = 0; i < transforms.Length; i++)
            {
                snapshots[i] = new TransformSnapshot(transforms[i]);
            }

            return snapshots;
        }

        private static void SampleTransformCurvesDirectly(AnimationClip clip, Transform root, float time)
        {
            var samples = new Dictionary<Transform, TransformCurveSample>();
            var bindingCount = 0;
            var matchedBindingCount = 0;
            var missingBindingCount = 0;
            var sampledPreview = new List<string>();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform))
                {
                    continue;
                }

                bindingCount++;
                var target = string.IsNullOrEmpty(binding.path) ? root : root.Find(binding.path);
                if (target == null)
                {
                    missingBindingCount++;
                    continue;
                }

                if (!samples.TryGetValue(target, out var sample))
                {
                    sample = new TransformCurveSample(target);
                    samples.Add(target, sample);
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                var value = curve.Evaluate(time);
                matchedBindingCount++;
                if (sampledPreview.Count < 4)
                {
                    sampledPreview.Add($"{binding.path}/{binding.propertyName}={value:0.###}");
                }

                switch (binding.propertyName)
                {
                    case "m_LocalPosition.x":
                        sample.Position.x = value;
                        break;
                    case "m_LocalPosition.y":
                        sample.Position.y = value;
                        break;
                    case "m_LocalPosition.z":
                        sample.Position.z = value;
                        break;
                    case "m_LocalScale.x":
                        sample.Scale.x = value;
                        break;
                    case "m_LocalScale.y":
                        sample.Scale.y = value;
                        break;
                    case "m_LocalScale.z":
                        sample.Scale.z = value;
                        break;
                    case "localEulerAnglesRaw.x":
                        sample.Euler.x = value;
                        break;
                    case "localEulerAnglesRaw.y":
                        sample.Euler.y = value;
                        break;
                    case "localEulerAnglesRaw.z":
                        sample.Euler.z = value;
                        break;
                }
            }

            foreach (var pair in samples)
            {
                pair.Key.localPosition = pair.Value.Position;
                pair.Key.localEulerAngles = pair.Value.Euler;
                pair.Key.localScale = pair.Value.Scale;
                EditorUtility.SetDirty(pair.Key);
            }

            if (time <= 0.001f || Mathf.Abs(time - 0.44f) <= 0.001f || Mathf.Abs(time - 0.68f) <= 0.001f)
            {
                Debug.Log(
                    $"SocietasAttackConsumeDirectSample Time={time:0.###}, Bindings={bindingCount}, Matched={matchedBindingCount}, Missing={missingBindingCount}, Targets={samples.Count}, Preview={string.Join("|", sampledPreview)}.");
            }
        }

        private static void SampleRendererEnabledCurvesDirectly(AnimationClip clip, Transform root, float time)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.propertyName != "m_Enabled")
                {
                    continue;
                }

                var target = string.IsNullOrEmpty(binding.path) ? root : root.Find(binding.path);
                if (target == null)
                {
                    continue;
                }

                var renderer = target.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                renderer.enabled = curve.Evaluate(time) >= 0.5f;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void SampleAttackConsumeAnimatorForCapture(
            Transform attackSlot,
            AnimationClip clip,
            Animator animator,
            float time)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                clip.SampleAnimation(attackSlot.gameObject, time);
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
            animator.Play(AttackConsumeBiteChewClipName, 0, 0f);
            animator.Update(0f);
            animator.Update(Mathf.Max(time, 1f / 120f));
            clip.SampleAnimation(attackSlot.gameObject, time);
        }

        private static void SampleDeathMeltPuddleAnimatorForCapture(
            Transform deathSlot,
            AnimationClip clip,
            Animator animator,
            float time)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                clip.SampleAnimation(deathSlot.gameObject, time);
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
            animator.Play(DeathMeltPuddleClipName, 0, 0f);
            animator.Update(0f);
            animator.Update(Mathf.Max(time, 1f / 120f));
            clip.SampleAnimation(deathSlot.gameObject, time);
        }

        private static void RestoreTransformSnapshots(IReadOnlyList<TransformSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                snapshots[i].Restore();
            }
        }

        private static List<RendererSnapshot> CaptureRendererSnapshots(Transform root)
        {
            var snapshots = new List<RendererSnapshot>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                snapshots.Add(new RendererSnapshot(renderer));
            }

            return snapshots;
        }

        private static void RestoreRendererSnapshots(IReadOnlyList<RendererSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                snapshots[i].Restore();
            }
        }

        private static List<RendererSnapshot> CaptureSiblingRendererSnapshots(Transform targetSlot)
        {
            var snapshots = new List<RendererSnapshot>();
            var parent = targetSlot.parent;
            if (parent == null)
            {
                return snapshots;
            }

            foreach (Transform sibling in parent)
            {
                if (sibling == targetSlot)
                {
                    continue;
                }

                foreach (var renderer in sibling.GetComponentsInChildren<Renderer>(true))
                {
                    snapshots.Add(new RendererSnapshot(renderer));
                }
            }

            return snapshots;
        }

        private static void HideSiblingRenderersForCapture(IReadOnlyList<RendererSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                snapshots[i].SetEnabled(false);
            }
        }

        private static void RestoreSiblingRendererSnapshots(IReadOnlyList<RendererSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                snapshots[i].Restore();
            }
        }

        private static void SaveContactSheet(IReadOnlyList<Texture2D> captures, string outputPath)
        {
            if (captures.Count == 0)
            {
                throw new InvalidOperationException("Cannot save a Societas contact sheet without captures.");
            }

            var frameWidth = captures[0].width;
            var frameHeight = captures[0].height;
            const int columns = 3;
            var rows = Mathf.CeilToInt(captures.Count / (float)columns);
            var sheet = new Texture2D(frameWidth * columns, frameHeight * rows, TextureFormat.RGBA32, false);
            var background = new Color(0.075f, 0.08f, 0.085f, 1f);
            var pixels = new Color[sheet.width * sheet.height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = background;
            }

            sheet.SetPixels(pixels);
            for (var i = 0; i < captures.Count; i++)
            {
                var column = i % columns;
                var row = rows - 1 - i / columns;
                sheet.SetPixels(column * frameWidth, row * frameHeight, frameWidth, frameHeight, captures[i].GetPixels());
            }

            sheet.Apply();
            File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
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
                EditorUtility.SetDirty(transform);
            }
        }

        private sealed class TransformCurveSample
        {
            public Vector3 Position;
            public Vector3 Euler;
            public Vector3 Scale;

            public TransformCurveSample(Transform transform)
            {
                Position = transform.localPosition;
                Euler = NormalizeEuler(transform.localEulerAngles);
                Scale = transform.localScale;
            }
        }

        private readonly struct LocalPositionAxis
        {
            public readonly string PropertyName;
            public readonly float Sign;
            private readonly int componentIndex;

            public LocalPositionAxis(string propertyName, float sign, int componentIndex)
            {
                PropertyName = propertyName;
                Sign = sign >= 0f ? 1f : -1f;
                this.componentIndex = componentIndex;
            }

            public float Read(Vector3 value)
            {
                return componentIndex switch
                {
                    0 => value.x,
                    2 => value.z,
                    _ => value.y
                };
            }
        }

        private readonly struct LocalEulerAxis
        {
            public readonly string PropertyName;
            public readonly float Sign;
            private readonly int componentIndex;

            public LocalEulerAxis(string propertyName, float sign, int componentIndex)
            {
                PropertyName = propertyName;
                Sign = sign >= 0f ? 1f : -1f;
                this.componentIndex = componentIndex;
            }

            public float Read(Vector3 value)
            {
                return componentIndex switch
                {
                    0 => value.x,
                    2 => value.z,
                    _ => value.y
                };
            }
        }

        private readonly struct RendererSnapshot
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                this.renderer = renderer;
                enabled = renderer.enabled;
            }

            public void SetEnabled(bool value)
            {
                if (renderer == null)
                {
                    return;
                }

                renderer.enabled = value;
            }

            public void Restore()
            {
                if (renderer == null)
                {
                    return;
                }

                renderer.enabled = enabled;
                EditorUtility.SetDirty(renderer);
            }
        }

        private readonly struct DeathMeltProxyVisuals
        {
            public readonly Transform BodyMass;
            public readonly Transform FrontFlow;
            public readonly Transform LeftFlow;
            public readonly Transform RightFlow;
            public readonly Transform FinalPuddle;

            public DeathMeltProxyVisuals(
                Transform bodyMass,
                Transform frontFlow,
                Transform leftFlow,
                Transform rightFlow,
                Transform finalPuddle)
            {
                BodyMass = bodyMass;
                FrontFlow = frontFlow;
                LeftFlow = leftFlow;
                RightFlow = rightFlow;
                FinalPuddle = finalPuddle;
            }
        }

        private readonly struct RigCandidate
        {
            public readonly Transform Transform;
            public readonly float Score;

            public RigCandidate(Transform transform, float score)
            {
                Transform = transform;
                Score = score;
            }
        }

        private readonly struct MoveRigCandidate
        {
            public readonly Transform Transform;
            public readonly float Front01;
            public readonly float Height01;
            public readonly int Depth;

            public MoveRigCandidate(Transform transform, float front01, float height01, int depth)
            {
                Transform = transform;
                Front01 = front01;
                Height01 = height01;
                Depth = depth;
            }
        }

        private enum BoneWeightSortMode
        {
            TotalWeight,
            FrontMinusZCenter,
            FrontPlusZCenter,
            LeftX,
            RightX
        }

        private sealed class BoneWeightInfluenceSummary
        {
            public readonly Transform Bone;
            public float TotalWeight;
            public float FrontMinusZCenter;
            public float FrontPlusZCenter;
            public float LeftX;
            public float RightX;
            private Vector3 weightedPosition;

            public BoneWeightInfluenceSummary(Transform bone)
            {
                Bone = bone;
            }

            public Vector3 Centroid => TotalWeight > 0.0001f ? weightedPosition / TotalWeight : Vector3.zero;

            public void Add(float weight, Vector3 localPoint, float frontMinusZCenter, float frontPlusZCenter, float leftX, float rightX)
            {
                TotalWeight += weight;
                weightedPosition += localPoint * weight;
                FrontMinusZCenter += weight * frontMinusZCenter;
                FrontPlusZCenter += weight * frontPlusZCenter;
                LeftX += weight * leftX;
                RightX += weight * rightX;
            }
        }

        private static void RequireFrontSideView(Vector3 viewerPosition, Vector3 lookAt, Vector3 frontDirection, string label)
        {
            var viewOffset = viewerPosition - lookAt;
            viewOffset.y = 0f;
            if (viewOffset.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException($"{label} is too close to Societas center for front view validation.");
            }

            var dot = Vector3.Dot(viewOffset.normalized, frontDirection.normalized);
            if (dot < 0.70f)
            {
                throw new InvalidOperationException($"{label} must be on the Societas front side. Dot={dot:0.###}.");
            }
        }

        private static void RequireBackSideView(Vector3 viewerPosition, Vector3 lookAt, Vector3 frontDirection, string label)
        {
            var viewOffset = viewerPosition - lookAt;
            viewOffset.y = 0f;
            if (viewOffset.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException($"{label} is too close to Societas center for opposite-side validation.");
            }

            var dot = Vector3.Dot(viewOffset.normalized, frontDirection.normalized);
            if (dot > -0.70f)
            {
                throw new InvalidOperationException($"{label} must be on the Societas opposite side. Dot={dot:0.###}.");
            }
        }

        private static void RequireFacingTarget(Transform viewer, Vector3 target, string label)
        {
            var toTarget = target - viewer.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException($"{label} is too close to Societas center for facing validation.");
            }

            var forward = viewer.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException($"{label} has no horizontal forward vector.");
            }

            var dot = Vector3.Dot(forward.normalized, toTarget.normalized);
            if (dot < 0.70f)
            {
                throw new InvalidOperationException($"{label} must face Societas after moving to the opposite side. Dot={dot:0.###}.");
            }
        }

        private static void ConfigureCaptureCamera(Camera camera, Transform focus, Bounds bounds)
        {
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.12f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.50f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var position = lookAt + frontDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.18f, 0.05f, 0.18f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 2.85f, 0.48f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
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

        private static Transform FindSocietasCameraFocus(Transform placementRoot)
        {
            return placementRoot.Find(PlacementObjectName) ?? placementRoot;
        }

        private static Vector3 CalculateSocietasVisualFrontDirection(Transform focus)
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

            return Mathf.Max(Vector3.Distance(tergoRoot.position, longaRoot.position), FallbackTergoLongaSpacing);
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
    }
}
