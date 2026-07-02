using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.LongaArmaCargoRunScene
{
    internal static class LongaArmaCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string CorridorRootName = "Approved Ship Corridor Segments";
        private const string ParvumPlacementRootName = "Approved Parvum Enemy Placement";
        private const string FugaPlacementRootName = "Approved Fuga Enemy Placement";
        private const string PlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string PlacementObjectName = "LongaArma_00_Static_Review";
        private const string ReviewCameraName = "Model Cam";
        private const string PlayerRootName = "Player";

        private const string SampleRootRelativePath = "artSample/enemies/longa_arma";
        private const string RuntimeLowPolySampleRootRelativePath = SampleRootRelativePath + "/runtime_lowpoly";
        private const string ApprovalStatusRelativePath = SampleRootRelativePath + "/APPROVAL_STATUS.json";
        private const string SourceModelRelativePath = RuntimeLowPolySampleRootRelativePath + "/exports/longa_arma_runtime_lowpoly.fbx";
        private const string SourceTextureRootRelativePath = RuntimeLowPolySampleRootRelativePath + "/textures";
        private const string ApprovedFrontRenderRelativePath = RuntimeLowPolySampleRootRelativePath + "/renders/front.png";
        private const string ReviewOutputRelativePath = "docs/validation/longa_arma_cargo_run_scene";

        private const string LongaArtRoot = "Assets/_Project/Art/Enemies/LongaArma";
        private const string UnityModelFolder = LongaArtRoot + "/Models";
        private const string UnityMaterialFolder = LongaArtRoot + "/Materials";
        private const string UnityTextureFolder = LongaArtRoot + "/Textures";
        private const string AnimationRootPath = LongaArtRoot + "/Animations";
        private const string AnimatorControllerRootPath = LongaArtRoot + "/AnimatorControllers";
        private const string UnityModelAssetPath = UnityModelFolder + "/longa_arma_runtime_lowpoly.fbx";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Enemies/LongaArma";
        private const string PrefabPath = PrefabFolder + "/LongaArmaApproved.prefab";
        private const string AnimationPivotChildName = "LongaArmaApproved_AnimationPivot";
        private const string ModelChildName = "LongaArmaApproved_Model";
        private const string ReviewLightName = "LongaArma_Approved_Review_KeyLight";

        private const float LongaFacingYawDegrees = 180f;
        private const float LongaSceneScale = 1f;
        private static readonly Vector3 LongaModelAxisCorrectionEuler = Vector3.zero;
        private const int RuntimeLowPolyMaximumTriangles = 15000;
        private const float MinimumFugaParvumZGap = 0.30f;
        private const float PlayerFrontDistance = 4.85f;
        private const float ReviewCameraMinimumFrontDistance = 4.75f;
        private const float ReviewCameraMaximumFrontDistance = 10.50f;
        private const int VisualCaptureWidth = 1280;
        private const int VisualCaptureHeight = 720;
        private const int SideBySideGapPixels = 24;

        private const string StaticReviewClipName = "LongaArma_Static_Review";
        private const string IdleClipName = "LongaArma_Idle";
        private const string MoveClipName = "LongaArma_Move_LimpingBladeArm";
        private const string AttackClipName = "LongaArma_Attack_Slam";
        private const string HitClipName = "LongaArma_Hit_Recoil";
        private const string ConsumeClipName = "LongaArma_Consume_BiteSlam";
        private const string DeathClipName = "LongaArma_Death_Melt";

        private const string IdleShapeKeyName = "Idle_Breath_BodySway";
        private const string MoveShapeKeyName = "Move_LimpingBladeArm_Drag";
        private const string MoveAlternateShapeKeyName = "Move_Crawl_AlternateStep";
        private const string MoveFrontRightReachShapeKeyName = "Move_FrontRight_LegReach";
        private const string MoveFrontRightPushShapeKeyName = "Move_FrontRight_LegPush";
        private const string MoveFrontLeftReachShapeKeyName = "Move_FrontLeft_LegReach";
        private const string MoveRearRightReachShapeKeyName = "Move_RearRight_LegReach";
        private const string MoveRearLeftReachShapeKeyName = "Move_RearLeft_LegReach";
        private const string MoveBladeArmSlowDragShapeKeyName = "Move_BladeArm_SlowDrag";
        private const string AttackShapeKeyName = "Attack_LeftBlade_SlamWindup";
        private const string AttackDragShapeKeyName = "Attack_FrontLeg_SlamDrag";
        private const string AttackUpperBodyRiseShapeKeyName = "Attack_UpperBody_Rise";
        private const string AttackForelimbsSlamShapeKeyName = "Attack_Forelimbs_ForwardSlam";
        private const string AttackGroundDragShapeKeyName = "Attack_GroundDrag_Pullback";
        private const string HitShapeKeyName = "Hit_HeadBack_Flinch";
        private const string HitSideShapeKeyName = "Hit_HeadSide_Shake";
        private const string ConsumeWindupShapeKeyName = "Consume_HeadBack_Windup";
        private const string ConsumeShapeKeyName = "Consume_HeadForward_BiteSlam";
        private const string ConsumeImpactShapeKeyName = "Consume_Peck_Impact";
        private const string DeathShapeKeyName = "Death_Melt_FlatLiquidSpread";
        private const string DeathPuddleShapeKeyName = "Death_Puddle_Final";

        private const string RootBoneName = "LongaRoot";
        private const string SpineBoneName = "LongaSpine";
        private const string ChestBoneName = "LongaChest";
        private const string HeadBoneName = "LongaHead";
        private const string BladeArmBoneName = "LongaBladeArm";
        private const string BladeArmForearmBoneName = "LongaBladeArmForearm";
        private const string BladeArmTipBoneName = "LongaBladeArmTip";
        private const string FrontRightLegBoneName = "LongaFrontRightLeg";
        private const string FrontRightLowerLegBoneName = "LongaFrontRightLowerLeg";
        private const string FrontRightFootBoneName = "LongaFrontRightFoot";
        private const string FrontLeftLegBoneName = "LongaFrontLeftLeg";
        private const string FrontLeftLowerLegBoneName = "LongaFrontLeftLowerLeg";
        private const string FrontLeftFootBoneName = "LongaFrontLeftFoot";
        private const string RearRightLegBoneName = "LongaRearRightLeg";
        private const string RearRightLowerLegBoneName = "LongaRearRightLowerLeg";
        private const string RearRightFootBoneName = "LongaRearRightFoot";
        private const string RearLeftLegBoneName = "LongaRearLeftLeg";
        private const string RearLeftLowerLegBoneName = "LongaRearLeftLowerLeg";
        private const string RearLeftFootBoneName = "LongaRearLeftFoot";

        private static readonly string[] RequiredBlendShapeNames =
        {
            IdleShapeKeyName,
            MoveShapeKeyName,
            MoveAlternateShapeKeyName,
            MoveFrontRightReachShapeKeyName,
            MoveFrontRightPushShapeKeyName,
            MoveFrontLeftReachShapeKeyName,
            MoveRearRightReachShapeKeyName,
            MoveRearLeftReachShapeKeyName,
            MoveBladeArmSlowDragShapeKeyName,
            AttackShapeKeyName,
            AttackDragShapeKeyName,
            AttackUpperBodyRiseShapeKeyName,
            AttackForelimbsSlamShapeKeyName,
            AttackGroundDragShapeKeyName,
            HitShapeKeyName,
            HitSideShapeKeyName,
            ConsumeWindupShapeKeyName,
            ConsumeShapeKeyName,
            ConsumeImpactShapeKeyName,
            DeathShapeKeyName,
            DeathPuddleShapeKeyName
        };

        private static readonly string[] RequiredRuntimeBoneNames =
        {
            RootBoneName,
            SpineBoneName,
            ChestBoneName,
            HeadBoneName,
            BladeArmBoneName,
            BladeArmForearmBoneName,
            BladeArmTipBoneName,
            FrontRightLegBoneName,
            FrontRightLowerLegBoneName,
            FrontRightFootBoneName,
            FrontLeftLegBoneName,
            FrontLeftLowerLegBoneName,
            FrontLeftFootBoneName,
            RearRightLegBoneName,
            RearRightLowerLegBoneName,
            RearRightFootBoneName,
            RearLeftLegBoneName,
            RearLeftLowerLegBoneName,
            RearLeftFootBoneName
        };

        private static readonly AnimationStateSpec[] AnimationStateSpecs =
        {
            new(PlacementObjectName, StaticReviewClipName, new Vector3(-6.30f, 0f, 0f), 0.00f, false),
            new("LongaArma_01_Idle", IdleClipName, new Vector3(-4.20f, 0f, 0f), 0.45f, true),
            new("LongaArma_02_Move_LimpingBladeArm", StaticReviewClipName, new Vector3(-2.10f, 0f, 0f), 0.00f, false),
            new("LongaArma_03_Attack_Slam", StaticReviewClipName, Vector3.zero, 0.00f, false),
            new("LongaArma_04_Hit_Recoil", HitClipName, new Vector3(2.10f, 0f, 0f), 0.12f, true),
            new("LongaArma_05_Consume_BiteSlam", ConsumeClipName, new Vector3(4.20f, 0f, 0f), 0.58f, true),
            new("LongaArma_06_Death_Melt", DeathClipName, new Vector3(6.30f, 0f, 0f), 1.65f, true)
        };

        private static readonly string[] TextureFileNames =
        {
            "longa_arma_wet_green_albedo.png",
            "longa_arma_wet_green_roughness.png",
            "longa_arma_wet_green_bump.png",
            "longa_arma_dark_blade_albedo.png",
            "longa_arma_dark_blade_roughness.png"
        };

        [MenuItem("Bellerophon/Enemies/Longa Arma/Apply Approved Sample To CargoRunMvp")]
        public static void ApplyApprovedSampleToCurrentCargoRunScene()
        {
            RequireApprovedSampleFiles();
            EnsureUnityFolders();
            CopyApprovedSampleAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImportedAssets();

            var materialSet = EnsureMaterials();
            var prefab = EnsurePrefab(materialSet);
            var animationClips = EnsureAnimationClips(prefab);
            var animatorControllers = EnsureAnimatorControllers(animationClips);

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlaceLongaArmaApprovedObjects(prefab, scene, animationClips, animatorControllers);
            ConfigureReviewLighting(placementRoot.transform);
            ConfigureInitialReviewCamera(placementRoot.transform);
            ConfigureInitialPlayerStart(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Approved Longa Arma sample applied to CargoRunMvp scene.");
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
            Debug.Log("Approved Longa Arma CargoRunMvp scene state inspected.");
        }

        public static void CaptureUnityVisualComparison()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var camera = FindReviewCamera();
            if (camera == null)
            {
                throw new InvalidOperationException("Longa Arma review camera is missing.");
            }

            var outputRoot = ProjectPath(ReviewOutputRelativePath);
            Directory.CreateDirectory(outputRoot);
            var unityCapturePath = Path.Combine(outputRoot, "longa_arma_unity_model_cam.png");
            var comparisonPath = Path.Combine(outputRoot, "longa_arma_approved_front_vs_unity.png");
            var readmePath = Path.Combine(outputRoot, "README.md");
            var approvedFrontPath = ProjectPath(ApprovedFrontRenderRelativePath);

            if (!File.Exists(approvedFrontPath))
            {
                throw new FileNotFoundException("Approved Longa Arma front render is missing.", approvedFrontPath);
            }

            CaptureCamera(camera, unityCapturePath, VisualCaptureWidth, VisualCaptureHeight);
            BuildSideBySideImage(approvedFrontPath, unityCapturePath, comparisonPath);
            File.WriteAllText(
                readmePath,
                "# Longa Arma Unity Visual Comparison\n\n" +
                $"- Created at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"- Scene: `{CargoRunScenePath}`\n" +
                $"- Placement root: `{PlacementRootName}`\n" +
                $"- Approved front render: `{ApprovedFrontRenderRelativePath}`\n" +
                $"- Unity model camera capture: `longa_arma_unity_model_cam.png`\n" +
                $"- Side-by-side comparison: `longa_arma_approved_front_vs_unity.png`\n" +
                "- Note: this is a visual comparison artifact for the approved sample; it is not a renderer-count-only validation.\n");

            Debug.Log("Approved Longa Arma Unity visual comparison captured.");
        }

        private static void RequireApprovedSampleFiles()
        {
            var approvalPath = ProjectPath(ApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new FileNotFoundException("Longa Arma approval status file is missing.", approvalPath);
            }

            var approvalText = File.ReadAllText(approvalPath);
            if (!approvalText.Contains("\"approved\": true", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Longa Arma sample must be approved before Unity application.");
            }

            var sourceModelPath = ProjectPath(SourceModelRelativePath);
            if (!File.Exists(sourceModelPath))
            {
                throw new FileNotFoundException("Approved Longa Arma FBX sample is missing.", sourceModelPath);
            }

            foreach (var textureFileName in TextureFileNames)
            {
                var texturePath = ProjectPath(Path.Combine(SourceTextureRootRelativePath, textureFileName));
                if (!File.Exists(texturePath))
                {
                    throw new FileNotFoundException($"Approved Longa Arma texture is missing: {textureFileName}", texturePath);
                }
            }
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(LongaArtRoot);
            EnsureUnityFolder(UnityModelFolder);
            EnsureUnityFolder(UnityMaterialFolder);
            EnsureUnityFolder(UnityTextureFolder);
            EnsureUnityFolder(AnimationRootPath);
            EnsureUnityFolder(AnimatorControllerRootPath);
            EnsureUnityFolder("Assets/_Project/Prefabs/Enemies");
            EnsureUnityFolder(PrefabFolder);
        }

        private static void CopyApprovedSampleAssets()
        {
            CopyFileToAsset(ProjectPath(SourceModelRelativePath), UnityModelAssetPath);

            foreach (var textureFileName in TextureFileNames)
            {
                CopyFileToAsset(
                    ProjectPath(Path.Combine(SourceTextureRootRelativePath, textureFileName)),
                    UnityTextureFolder + "/" + textureFileName);
            }
        }

        private static void ConfigureImportedAssets()
        {
            var modelImporter = AssetImporter.GetAtPath(UnityModelAssetPath) as ModelImporter;
            if (modelImporter != null)
            {
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.importBlendShapes = true;
                modelImporter.importVisibility = false;
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                modelImporter.importNormals = ModelImporterNormals.Import;
                modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                modelImporter.globalScale = 1f;
                modelImporter.SaveAndReimport();
            }

            foreach (var textureFileName in TextureFileNames)
            {
                var assetPath = UnityTextureFolder + "/" + textureFileName;
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var isNormal = textureFileName.Contains("bump", StringComparison.OrdinalIgnoreCase);
                var isRoughness = textureFileName.Contains("roughness", StringComparison.OrdinalIgnoreCase);
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !isNormal && !isRoughness;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.SaveAndReimport();
            }
        }

        private static MaterialSet EnsureMaterials()
        {
            var bodyAlbedo = LoadTexture("longa_arma_wet_green_albedo.png");
            var bodyNormal = LoadTexture("longa_arma_wet_green_bump.png");
            var bladeAlbedo = LoadTexture("longa_arma_dark_blade_albedo.png");

            return new MaterialSet(
                EnsureMaterial("M_LongaArma_Wet_Green_Flesh", bodyAlbedo, bodyNormal, new Color(0.68f, 0.84f, 0.72f), 0.28f, 0f),
                EnsureMaterial("M_LongaArma_Dark_Crescent_Blade", bladeAlbedo, null, new Color(0.66f, 0.66f, 0.62f), 0.42f, 0.18f));
        }

        private static GameObject EnsurePrefab(MaterialSet materialSet)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Could not load Longa Arma model asset at {UnityModelAssetPath}.");
            }

            var root = new GameObject("LongaArmaApproved");
            try
            {
                var animationPivot = new GameObject(AnimationPivotChildName);
                animationPivot.transform.SetParent(root.transform, false);
                animationPivot.transform.localPosition = Vector3.zero;
                animationPivot.transform.localRotation = Quaternion.identity;
                animationPivot.transform.localScale = Vector3.one;

                var modelInstance = UnityEngine.Object.Instantiate(modelAsset);
                modelInstance.name = ModelChildName;
                modelInstance.transform.SetParent(animationPivot.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.Euler(LongaModelAxisCorrectionEuler);
                modelInstance.transform.localScale = Vector3.one;

                AssignMaterials(root, materialSet);
                EnsurePrefabPhysics(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Could not create Longa Arma prefab at {PrefabPath}.");
            }

            return prefab;
        }

        private static Dictionary<string, AnimationClip> EnsureAnimationClips(GameObject prefab)
        {
            var animationPivot = prefab.transform.Find(AnimationPivotChildName);
            if (animationPivot == null)
            {
                throw new InvalidOperationException($"Longa Arma prefab must contain {AnimationPivotChildName} for runtime low-poly review animations.");
            }

            var skinnedRenderer = RequireLongaSkinnedRenderer(prefab);
            ValidateRequiredBlendShapes(skinnedRenderer, prefab.name);
            var runtimeBones = RequireRuntimeBoneBindings(prefab);

            var pivotPath = AnimationUtility.CalculateTransformPath(animationPivot, prefab.transform);
            var rendererPath = AnimationUtility.CalculateTransformPath(skinnedRenderer.transform, prefab.transform);
            var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);

            AddSavedAnimationClip(clips, CreateStaticReviewClip(pivotPath, rendererPath), false);
            AddSavedAnimationClip(clips, CreateIdleClip(pivotPath, rendererPath), true);
            AddSavedAnimationClip(clips, CreateMoveClip(pivotPath, rendererPath, runtimeBones), true);
            AddSavedAnimationClip(clips, CreateAttackClip(pivotPath, rendererPath, runtimeBones), true);
            AddSavedAnimationClip(clips, CreateHitClip(pivotPath, rendererPath), true);
            AddSavedAnimationClip(clips, CreateConsumeClip(pivotPath, rendererPath), true);
            AddSavedAnimationClip(clips, CreateDeathClip(pivotPath, rendererPath), true);
            AssetDatabase.SaveAssets();

            return clips;
        }

        private static Dictionary<string, AnimatorController> EnsureAnimatorControllers(Dictionary<string, AnimationClip> clips)
        {
            var controllers = new Dictionary<string, AnimatorController>(StringComparer.Ordinal);
            foreach (var spec in AnimationStateSpecs)
            {
                if (!clips.TryGetValue(spec.ClipName, out var clip))
                {
                    throw new InvalidOperationException($"Longa Arma animation clip is missing: {spec.ClipName}");
                }

                var controllerPath = AnimatorControllerRootPath + "/" + spec.ClipName + ".controller";
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                {
                    AssetDatabase.DeleteAsset(controllerPath);
                }

                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var stateMachine = controller.layers[0].stateMachine;
                var state = stateMachine.AddState(spec.ClipName);
                state.motion = clip;
                state.speed = 1f;
                stateMachine.defaultState = state;
                EditorUtility.SetDirty(controller);
                controllers[spec.ClipName] = controller;
            }

            AssetDatabase.SaveAssets();
            return controllers;
        }

        private static SkinnedMeshRenderer RequireLongaSkinnedRenderer(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                {
                    return renderer;
                }
            }

            throw new InvalidOperationException($"{root.name} must contain the runtime low-poly SkinnedMeshRenderer with BlendShapes.");
        }

        private static void ValidateRequiredBlendShapes(SkinnedMeshRenderer renderer, string context)
        {
            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                throw new InvalidOperationException($"{context} Longa Arma renderer has no shared mesh.");
            }

            foreach (var shapeName in RequiredBlendShapeNames)
            {
                if (mesh.GetBlendShapeIndex(shapeName) < 0)
                {
                    throw new InvalidOperationException($"{context} Longa Arma mesh is missing required BlendShape: {shapeName}");
                }
            }
        }

        private static Dictionary<string, BoneBinding> RequireRuntimeBoneBindings(GameObject root)
        {
            var bindings = new Dictionary<string, BoneBinding>(StringComparer.Ordinal);
            foreach (var boneName in RequiredRuntimeBoneNames)
            {
                var bone = FindChildTransform(root.transform, boneName);
                if (bone == null)
                {
                    throw new InvalidOperationException($"{root.name} Longa Arma runtime rig is missing required bone: {boneName}");
                }

                bindings[boneName] = new BoneBinding(
                    AnimationUtility.CalculateTransformPath(bone, root.transform),
                    bone.localPosition,
                    Vector3.zero,
                    bone.localScale);
            }

            return bindings;
        }

        private static Transform FindChildTransform(Transform root, string transformName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, transformName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject PlaceLongaArmaApprovedObjects(
            GameObject prefab,
            Scene scene,
            Dictionary<string, AnimationClip> clips,
            Dictionary<string, AnimatorController> controllers)
        {
            var existingRoot = GameObject.Find(PlacementRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            var targetPosition = CalculateLongaPlacementPosition();
            var placementRoot = new GameObject(PlacementRootName);
            placementRoot.transform.position = targetPosition;
            placementRoot.transform.rotation = Quaternion.identity;
            placementRoot.transform.localScale = Vector3.one;

            foreach (var spec in AnimationStateSpecs)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                {
                    instance = UnityEngine.Object.Instantiate(prefab);
                    SceneManager.MoveGameObjectToScene(instance, scene);
                }

                instance.name = spec.ObjectName;
                instance.transform.SetParent(placementRoot.transform, false);
                instance.transform.localPosition = spec.LocalOffset;
                instance.transform.localRotation = Quaternion.Euler(0f, LongaFacingYawDegrees, 0f);
                instance.transform.localScale = Vector3.one * LongaSceneScale;
                ConfigureAnimationReviewState(instance, spec, clips, controllers);
                ConfigureScenePhysics(instance.transform);
            }

            AlignRootBottomToCorridorFloor(placementRoot.transform);
            return placementRoot;
        }

        private static AnimationClip CreateStaticReviewClip(string pivotPath, string rendererPath)
        {
            return CreateTransformClip(StaticReviewClipName, 1.0f, pivotPath, rendererPath);
        }

        private static AnimationClip CreateIdleClip(string pivotPath, string rendererPath)
        {
            var clip = CreateTransformClip(IdleClipName, 1.60f, pivotPath, rendererPath);
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.y",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.40f, 0.018f),
                new Keyframe(0.80f, 0.030f),
                new Keyframe(1.20f, 0.012f),
                new Keyframe(1.60f, 0.00f));
            SetCurve(clip, pivotPath, typeof(Transform), "localEulerAnglesRaw.z",
                new Keyframe(0.00f, -0.6f),
                new Keyframe(0.80f, 0.8f),
                new Keyframe(1.60f, -0.6f));
            SetScaleCurve(clip, pivotPath, "y",
                new Keyframe(0.00f, 1.00f),
                new Keyframe(0.80f, 1.010f),
                new Keyframe(1.60f, 1.00f));
            SetBlendShapeCurve(clip, rendererPath, IdleShapeKeyName,
                new Keyframe(0.00f, 0f),
                new Keyframe(0.40f, 55f),
                new Keyframe(0.80f, 100f),
                new Keyframe(1.20f, 45f),
                new Keyframe(1.60f, 0f));
            return clip;
        }

        private static AnimationClip CreateMoveClip(string pivotPath, string rendererPath, Dictionary<string, BoneBinding> bones)
        {
            const float duration = 1.60f;
            var clip = CreateTransformClip(MoveClipName, duration, pivotPath, rendererPath, includeBlendShapes: false);
            SetRestBoneCurves(clip, bones, duration);

            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.y",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.20f, 0.016f),
                new Keyframe(0.40f, -0.012f),
                new Keyframe(0.60f, 0.018f),
                new Keyframe(0.80f, -0.010f),
                new Keyframe(1.00f, 0.016f),
                new Keyframe(1.20f, -0.012f),
                new Keyframe(1.40f, 0.010f),
                new Keyframe(duration, 0.00f));
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.z",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.20f, -0.014f),
                new Keyframe(0.40f, -0.020f),
                new Keyframe(0.60f, 0.012f),
                new Keyframe(0.80f, 0.020f),
                new Keyframe(1.00f, 0.006f),
                new Keyframe(1.20f, -0.016f),
                new Keyframe(1.40f, 0.012f),
                new Keyframe(duration, 0.00f));
            SetBonePositionOffsetCurves(clip, bones[SpineBoneName],
                x: PeriodicKeys(duration, 0.00f, p => 0.018f * Mathf.Sin(p * Mathf.PI * 2f)),
                y: PeriodicKeys(duration, 0.00f, p => -0.012f + 0.012f * Mathf.Sin((p + 0.18f) * Mathf.PI * 2f)),
                z: PeriodicKeys(duration, 0.00f, p => 0.012f * Mathf.Sin((p + 0.08f) * Mathf.PI * 2f)));
            SetBonePositionOffsetCurves(clip, bones[ChestBoneName],
                x: PeriodicKeys(duration, 0.00f, p => -0.018f + 0.020f * Mathf.Sin((p + 0.25f) * Mathf.PI * 2f)),
                y: PeriodicKeys(duration, 0.00f, p => 0.010f * Mathf.Sin((p + 0.10f) * Mathf.PI * 2f)),
                z: PeriodicKeys(duration, 0.00f, p => 0.018f * Mathf.Sin((p + 0.34f) * Mathf.PI * 2f)));

            SetQuadrupedLegCycle(
                clip,
                bones[RearLeftLegBoneName],
                bones[RearLeftLowerLegBoneName],
                bones[RearLeftFootBoneName],
                duration,
                phase: 0.00f,
                strideX: 0.115f,
                liftY: 0.115f,
                lateralZ: -0.030f,
                supportDropY: 0.024f);
            SetBladeLimpCycle(
                clip,
                bones[BladeArmBoneName],
                bones[BladeArmForearmBoneName],
                bones[BladeArmTipBoneName],
                duration,
                phase: 0.26f,
                strideX: -0.115f,
                lateralZ: -0.035f);
            SetQuadrupedLegCycle(
                clip,
                bones[RearRightLegBoneName],
                bones[RearRightLowerLegBoneName],
                bones[RearRightFootBoneName],
                duration,
                phase: 0.52f,
                strideX: 0.112f,
                liftY: 0.110f,
                lateralZ: 0.030f,
                supportDropY: 0.024f);
            SetQuadrupedLegCycle(
                clip,
                bones[FrontRightLegBoneName],
                bones[FrontRightLowerLegBoneName],
                bones[FrontRightFootBoneName],
                duration,
                phase: 0.78f,
                strideX: -0.128f,
                liftY: 0.130f,
                lateralZ: 0.026f,
                supportDropY: 0.020f);
            SetTuckedSupportCycle(
                clip,
                bones[FrontLeftLegBoneName],
                bones[FrontLeftLowerLegBoneName],
                bones[FrontLeftFootBoneName],
                duration,
                phase: 0.26f);
            return clip;
        }

        private static AnimationClip CreateAttackClip(string pivotPath, string rendererPath, Dictionary<string, BoneBinding> bones)
        {
            const float duration = 2.05f;
            var clip = CreateTransformClip(AttackClipName, duration, pivotPath, rendererPath, includeBlendShapes: false);
            SetRestBoneCurves(clip, bones, duration);
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.y",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.28f, 0.05f),
                new Keyframe(0.58f, 0.15f),
                new Keyframe(0.82f, 0.18f),
                new Keyframe(1.05f, -0.10f),
                new Keyframe(1.34f, -0.04f),
                new Keyframe(duration, 0.00f));
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.z",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.58f, -0.018f),
                new Keyframe(0.82f, -0.006f),
                new Keyframe(1.05f, 0.090f),
                new Keyframe(1.34f, 0.052f),
                new Keyframe(duration, 0.00f));

            SetBonePositionOffsetCurves(clip, bones[ChestBoneName],
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.28f, -0.040f), new Keyframe(0.58f, -0.120f), new Keyframe(0.82f, -0.150f), new Keyframe(1.05f, -0.020f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.28f, 0.145f), new Keyframe(0.58f, 0.350f), new Keyframe(0.82f, 0.450f), new Keyframe(1.05f, 0.070f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, 0.000f), new Keyframe(1.05f, 0.000f), new Keyframe(duration, 0f) });
            SetBonePositionOffsetCurves(clip, bones[HeadBoneName],
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, -0.105f), new Keyframe(0.82f, -0.120f), new Keyframe(1.05f, 0.095f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, 0.275f), new Keyframe(0.82f, 0.340f), new Keyframe(1.05f, 0.010f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(duration, 0f) });
            SetAttackForelimbSlam(clip, bones[BladeArmBoneName], bones[BladeArmForearmBoneName], bones[BladeArmTipBoneName], lateralZ: -0.050f, bladeLike: true, duration: duration);
            SetAttackForelimbSlam(clip, bones[FrontRightLegBoneName], bones[FrontRightLowerLegBoneName], bones[FrontRightFootBoneName], lateralZ: 0.050f, bladeLike: false, duration: duration);
            SetAttackBraceLeg(clip, bones[RearLeftLegBoneName], bones[RearLeftLowerLegBoneName], bones[RearLeftFootBoneName], lateralZ: -0.026f, duration: duration);
            SetAttackBraceLeg(clip, bones[RearRightLegBoneName], bones[RearRightLowerLegBoneName], bones[RearRightFootBoneName], lateralZ: 0.026f, duration: duration);
            SetAttackTuckedSupport(clip, bones[FrontLeftLegBoneName], bones[FrontLeftLowerLegBoneName], bones[FrontLeftFootBoneName], duration);
            return clip;
        }

        private static AnimationClip CreateHitClip(string pivotPath, string rendererPath)
        {
            var clip = CreateTransformClip(HitClipName, 1.05f, pivotPath, rendererPath);
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.y",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.10f, 0.055f),
                new Keyframe(0.22f, 0.020f),
                new Keyframe(1.05f, 0.00f));
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.z",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.10f, -0.130f),
                new Keyframe(0.22f, -0.075f),
                new Keyframe(0.52f, -0.020f),
                new Keyframe(1.05f, 0.00f));
            SetCurve(clip, pivotPath, typeof(Transform), "localEulerAnglesRaw.x",
                new Keyframe(0.00f, 0f),
                new Keyframe(0.10f, -13f),
                new Keyframe(0.22f, -6f),
                new Keyframe(0.52f, 3f),
                new Keyframe(1.05f, 0f));
            SetCurve(clip, pivotPath, typeof(Transform), "localEulerAnglesRaw.y",
                new Keyframe(0.00f, 0f),
                new Keyframe(0.10f, -7f),
                new Keyframe(0.22f, 8f),
                new Keyframe(0.36f, -5f),
                new Keyframe(0.52f, 4f),
                new Keyframe(1.05f, 0f));
            SetBlendShapeCurve(clip, rendererPath, HitShapeKeyName,
                new Keyframe(0.00f, 0f),
                new Keyframe(0.10f, 100f),
                new Keyframe(0.22f, 55f),
                new Keyframe(1.05f, 0f));
            SetBlendShapeCurve(clip, rendererPath, HitSideShapeKeyName,
                new Keyframe(0.00f, 0f),
                new Keyframe(0.10f, 80f),
                new Keyframe(0.22f, 0f),
                new Keyframe(0.36f, 100f),
                new Keyframe(0.52f, 0f),
                new Keyframe(1.05f, 0f));
            return clip;
        }

        private static AnimationClip CreateConsumeClip(string pivotPath, string rendererPath)
        {
            var clip = CreateTransformClip(ConsumeClipName, 1.55f, pivotPath, rendererPath);
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.y",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.28f, 0.17f),
                new Keyframe(0.54f, 0.09f),
                new Keyframe(0.70f, -0.16f),
                new Keyframe(0.90f, -0.09f),
                new Keyframe(1.55f, 0.00f));
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.z",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.28f, -0.050f),
                new Keyframe(0.70f, 0.145f),
                new Keyframe(0.90f, 0.075f),
                new Keyframe(1.55f, 0.00f));
            SetCurve(clip, pivotPath, typeof(Transform), "localEulerAnglesRaw.x",
                new Keyframe(0.00f, 0f),
                new Keyframe(0.28f, -22f),
                new Keyframe(0.54f, -12f),
                new Keyframe(0.70f, 28f),
                new Keyframe(0.90f, 18f),
                new Keyframe(1.55f, 0f));
            SetBlendShapeCurve(clip, rendererPath, ConsumeWindupShapeKeyName,
                new Keyframe(0.00f, 0f),
                new Keyframe(0.28f, 100f),
                new Keyframe(0.54f, 45f),
                new Keyframe(0.70f, 0f),
                new Keyframe(1.55f, 0f));
            SetBlendShapeCurve(clip, rendererPath, ConsumeShapeKeyName,
                new Keyframe(0.00f, 0f),
                new Keyframe(0.54f, 0f),
                new Keyframe(0.70f, 100f),
                new Keyframe(0.90f, 45f),
                new Keyframe(1.55f, 0f));
            SetBlendShapeCurve(clip, rendererPath, ConsumeImpactShapeKeyName,
                new Keyframe(0.00f, 0f),
                new Keyframe(0.66f, 0f),
                new Keyframe(0.74f, 100f),
                new Keyframe(0.84f, 0f),
                new Keyframe(1.55f, 0f));
            return clip;
        }

        private static AnimationClip CreateDeathClip(string pivotPath, string rendererPath)
        {
            var clip = CreateTransformClip(DeathClipName, 2.20f, pivotPath, rendererPath);
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.y",
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.40f, -0.07f),
                new Keyframe(0.92f, -0.32f),
                new Keyframe(1.48f, -0.53f),
                new Keyframe(2.20f, -0.58f));
            SetCurve(clip, pivotPath, typeof(Transform), "localEulerAnglesRaw.x",
                new Keyframe(0.00f, 0f),
                new Keyframe(0.40f, 6f),
                new Keyframe(0.92f, 12f),
                new Keyframe(2.20f, 0f));
            SetScaleCurve(clip, pivotPath, "x",
                new Keyframe(0.00f, 1.00f),
                new Keyframe(0.92f, 1.10f),
                new Keyframe(1.48f, 1.25f),
                new Keyframe(2.20f, 1.36f));
            SetScaleCurve(clip, pivotPath, "y",
                new Keyframe(0.00f, 1.00f),
                new Keyframe(0.92f, 0.54f),
                new Keyframe(1.48f, 0.20f),
                new Keyframe(2.20f, 0.12f));
            SetScaleCurve(clip, pivotPath, "z",
                new Keyframe(0.00f, 1.00f),
                new Keyframe(0.92f, 1.18f),
                new Keyframe(1.48f, 1.48f),
                new Keyframe(2.20f, 1.70f));
            SetBlendShapeCurve(clip, rendererPath, DeathShapeKeyName,
                new Keyframe(0.00f, 0f),
                new Keyframe(0.40f, 30f),
                new Keyframe(0.92f, 100f),
                new Keyframe(1.48f, 65f),
                new Keyframe(2.20f, 0f));
            SetBlendShapeCurve(clip, rendererPath, DeathPuddleShapeKeyName,
                new Keyframe(0.00f, 0f),
                new Keyframe(0.92f, 0f),
                new Keyframe(1.48f, 72f),
                new Keyframe(2.20f, 100f));
            return clip;
        }

        private static AnimationClip CreateTransformClip(string clipName, float duration, string pivotPath, string rendererPath, bool includeBlendShapes = true)
        {
            var clip = new AnimationClip
            {
                name = clipName,
                frameRate = 30f
            };

            SetIdentityTransformCurves(clip, pivotPath, duration);
            if (includeBlendShapes)
            {
                SetIdentityBlendShapeCurves(clip, rendererPath, duration);
            }

            return clip;
        }

        private static void AddSavedAnimationClip(Dictionary<string, AnimationClip> clips, AnimationClip clip, bool loop)
        {
            ConfigureLoopSetting(clip, loop);
            var clipPath = AnimationRootPath + "/" + clip.name + ".anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
            {
                AssetDatabase.DeleteAsset(clipPath);
            }

            AssetDatabase.CreateAsset(clip, clipPath);
            var savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (savedClip == null)
            {
                throw new InvalidOperationException($"Could not create Longa Arma animation clip at {clipPath}.");
            }

            ConfigureLoopSetting(savedClip, loop);
            EditorUtility.SetDirty(savedClip);
            clips[clip.name] = savedClip;
        }

        private static void ConfigureLoopSetting(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void SetIdentityTransformCurves(AnimationClip clip, string pivotPath, float duration)
        {
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.x", new Keyframe(0f, 0f), new Keyframe(duration, 0f));
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.y", new Keyframe(0f, 0f), new Keyframe(duration, 0f));
            SetCurve(clip, pivotPath, typeof(Transform), "localPosition.z", new Keyframe(0f, 0f), new Keyframe(duration, 0f));
            SetCurve(clip, pivotPath, typeof(Transform), "localEulerAnglesRaw.x", new Keyframe(0f, 0f), new Keyframe(duration, 0f));
            SetCurve(clip, pivotPath, typeof(Transform), "localEulerAnglesRaw.y", new Keyframe(0f, 0f), new Keyframe(duration, 0f));
            SetCurve(clip, pivotPath, typeof(Transform), "localEulerAnglesRaw.z", new Keyframe(0f, 0f), new Keyframe(duration, 0f));
            SetCurve(clip, pivotPath, typeof(Transform), "localScale.x", new Keyframe(0f, 1f), new Keyframe(duration, 1f));
            SetCurve(clip, pivotPath, typeof(Transform), "localScale.y", new Keyframe(0f, 1f), new Keyframe(duration, 1f));
            SetCurve(clip, pivotPath, typeof(Transform), "localScale.z", new Keyframe(0f, 1f), new Keyframe(duration, 1f));
        }

        private static void SetIdentityBlendShapeCurves(AnimationClip clip, string rendererPath, float duration)
        {
            foreach (var shapeName in RequiredBlendShapeNames)
            {
                SetBlendShapeCurve(clip, rendererPath, shapeName, new Keyframe(0f, 0f), new Keyframe(duration, 0f));
            }
        }

        private static void SetRestBoneCurves(AnimationClip clip, Dictionary<string, BoneBinding> bones, float duration)
        {
            foreach (var bone in bones.Values)
            {
                SetCurve(clip, bone.Path, typeof(Transform), "localPosition.x", new Keyframe(0f, bone.LocalPosition.x), new Keyframe(duration, bone.LocalPosition.x));
                SetCurve(clip, bone.Path, typeof(Transform), "localPosition.y", new Keyframe(0f, bone.LocalPosition.y), new Keyframe(duration, bone.LocalPosition.y));
                SetCurve(clip, bone.Path, typeof(Transform), "localPosition.z", new Keyframe(0f, bone.LocalPosition.z), new Keyframe(duration, bone.LocalPosition.z));
                SetCurve(clip, bone.Path, typeof(Transform), "localScale.x", new Keyframe(0f, bone.LocalScale.x), new Keyframe(duration, bone.LocalScale.x));
                SetCurve(clip, bone.Path, typeof(Transform), "localScale.y", new Keyframe(0f, bone.LocalScale.y), new Keyframe(duration, bone.LocalScale.y));
                SetCurve(clip, bone.Path, typeof(Transform), "localScale.z", new Keyframe(0f, bone.LocalScale.z), new Keyframe(duration, bone.LocalScale.z));
            }
        }

        private static void SetQuadrupedLegCycle(
            AnimationClip clip,
            BoneBinding upper,
            BoneBinding lower,
            BoneBinding foot,
            float duration,
            float phase,
            float strideX,
            float liftY,
            float lateralZ,
            float supportDropY)
        {
            SetBonePositionOffsetCurves(clip, upper,
                x: PeriodicKeys(duration, phase, p => LegUpperX(p, strideX)),
                y: PeriodicKeys(duration, phase, p => LegUpperY(p, liftY, supportDropY)),
                z: PeriodicKeys(duration, phase, p => LegUpperZ(p, lateralZ)));
            SetBonePositionOffsetCurves(clip, lower,
                x: PeriodicKeys(duration, phase, p => LegLowerX(p, strideX)),
                y: PeriodicKeys(duration, phase, p => LegLowerY(p, liftY, supportDropY)),
                z: PeriodicKeys(duration, phase, p => LegLowerZ(p, lateralZ)));
            SetBonePositionOffsetCurves(clip, foot,
                x: PeriodicKeys(duration, phase, p => LegFootX(p, strideX)),
                y: PeriodicKeys(duration, phase, p => LegFootY(p, liftY, supportDropY)),
                z: PeriodicKeys(duration, phase, p => LegFootZ(p, lateralZ)));
        }

        private static void SetBladeLimpCycle(
            AnimationClip clip,
            BoneBinding upper,
            BoneBinding forearm,
            BoneBinding tip,
            float duration,
            float phase,
            float strideX,
            float lateralZ)
        {
            SetBonePositionOffsetCurves(clip, upper,
                x: PeriodicKeys(duration, phase, p => BladeUpperX(p, strideX)),
                y: PeriodicKeys(duration, phase, p => BladeUpperY(p)),
                z: PeriodicKeys(duration, phase, p => BladeSideZ(p, lateralZ) * 0.45f));
            SetBonePositionOffsetCurves(clip, forearm,
                x: PeriodicKeys(duration, phase, p => BladeForearmX(p, strideX)),
                y: PeriodicKeys(duration, phase, p => BladeForearmY(p)),
                z: PeriodicKeys(duration, phase, p => BladeSideZ(p, lateralZ) * 0.72f));
            SetBonePositionOffsetCurves(clip, tip,
                x: PeriodicKeys(duration, phase, p => BladeTipX(p, strideX)),
                y: PeriodicKeys(duration, phase, BladeTipY),
                z: PeriodicKeys(duration, phase, p => BladeSideZ(p, lateralZ)));
        }

        private static void SetTuckedSupportCycle(AnimationClip clip, BoneBinding upper, BoneBinding lower, BoneBinding foot, float duration, float phase)
        {
            SetBonePositionOffsetCurves(clip, upper,
                x: PeriodicKeys(duration, phase, p => -0.020f + 0.020f * Mathf.Sin(p * Mathf.PI * 2f)),
                y: PeriodicKeys(duration, phase, p => -0.010f + 0.012f * Mathf.Sin((p + 0.10f) * Mathf.PI * 2f)),
                z: PeriodicKeys(duration, phase, p => -0.010f + 0.006f * Mathf.Sin((p + 0.30f) * Mathf.PI * 2f)));
            SetBonePositionOffsetCurves(clip, lower,
                x: PeriodicKeys(duration, phase, p => -0.034f + 0.026f * Mathf.Sin((p + 0.08f) * Mathf.PI * 2f)),
                y: PeriodicKeys(duration, phase, p => -0.016f + 0.012f * Mathf.Sin((p + 0.18f) * Mathf.PI * 2f)),
                z: PeriodicKeys(duration, phase, p => -0.012f + 0.008f * Mathf.Sin((p + 0.25f) * Mathf.PI * 2f)));
            SetBonePositionOffsetCurves(clip, foot,
                x: PeriodicKeys(duration, phase, p => -0.050f + 0.032f * Mathf.Sin((p + 0.10f) * Mathf.PI * 2f)),
                y: PeriodicKeys(duration, phase, p => -0.026f + 0.010f * Mathf.Sin((p + 0.20f) * Mathf.PI * 2f)),
                z: PeriodicKeys(duration, phase, p => -0.016f + 0.010f * Mathf.Sin((p + 0.28f) * Mathf.PI * 2f)));
        }

        private static void SetAttackForelimbSlam(AnimationClip clip, BoneBinding upper, BoneBinding lower, BoneBinding foot, float lateralZ, bool bladeLike, float duration)
        {
            var liftScale = bladeLike ? 1.10f : 1.00f;
            var slamScale = bladeLike ? 1.18f : 1.00f;
            SetBonePositionOffsetCurves(clip, upper,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.34f, -0.045f), new Keyframe(0.62f, -0.105f), new Keyframe(0.82f, -0.135f), new Keyframe(1.05f, -0.030f), new Keyframe(1.34f, 0.050f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.34f, 0.125f), new Keyframe(0.62f, 0.240f * liftScale), new Keyframe(0.82f, 0.300f * liftScale), new Keyframe(1.05f, -0.020f), new Keyframe(1.34f, -0.012f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.62f, lateralZ * 0.55f), new Keyframe(0.82f, lateralZ), new Keyframe(1.05f, lateralZ * 0.35f), new Keyframe(duration, 0f) });
            SetBonePositionOffsetCurves(clip, lower,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.34f, -0.070f), new Keyframe(0.62f, -0.165f), new Keyframe(0.82f, -0.225f), new Keyframe(1.05f, 0.080f * slamScale), new Keyframe(1.34f, 0.160f * slamScale), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.34f, 0.195f), new Keyframe(0.62f, 0.390f * liftScale), new Keyframe(0.82f, 0.500f * liftScale), new Keyframe(1.05f, -0.070f * slamScale), new Keyframe(1.34f, -0.050f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.62f, lateralZ * 0.95f), new Keyframe(0.82f, lateralZ * 1.25f), new Keyframe(1.05f, lateralZ * 0.50f), new Keyframe(duration, 0f) });
            SetBonePositionOffsetCurves(clip, foot,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.34f, -0.105f), new Keyframe(0.62f, -0.245f), new Keyframe(0.82f, -0.335f), new Keyframe(1.05f, 0.220f * slamScale), new Keyframe(1.34f, 0.360f * slamScale), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.34f, 0.255f), new Keyframe(0.62f, 0.520f * liftScale), new Keyframe(0.82f, 0.660f * liftScale), new Keyframe(1.05f, -0.160f * slamScale), new Keyframe(1.34f, -0.115f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.62f, lateralZ * 1.25f), new Keyframe(0.82f, lateralZ * 1.55f), new Keyframe(1.05f, lateralZ * 0.55f), new Keyframe(duration, 0f) });
        }

        private static void SetAttackBraceLeg(AnimationClip clip, BoneBinding upper, BoneBinding lower, BoneBinding foot, float lateralZ, float duration)
        {
            SetBonePositionOffsetCurves(clip, upper,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, 0.045f), new Keyframe(0.82f, 0.070f), new Keyframe(1.05f, 0.045f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, -0.035f), new Keyframe(0.82f, -0.060f), new Keyframe(1.05f, -0.030f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.82f, lateralZ), new Keyframe(duration, 0f) });
            SetBonePositionOffsetCurves(clip, lower,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, 0.070f), new Keyframe(0.82f, 0.110f), new Keyframe(1.05f, 0.060f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, -0.045f), new Keyframe(0.82f, -0.075f), new Keyframe(1.05f, -0.045f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.82f, lateralZ * 1.10f), new Keyframe(duration, 0f) });
            SetBonePositionOffsetCurves(clip, foot,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, 0.060f), new Keyframe(0.82f, 0.125f), new Keyframe(1.05f, 0.070f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, -0.060f), new Keyframe(0.82f, -0.090f), new Keyframe(1.05f, -0.075f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.82f, lateralZ * 1.25f), new Keyframe(duration, 0f) });
        }

        private static void SetAttackTuckedSupport(AnimationClip clip, BoneBinding upper, BoneBinding lower, BoneBinding foot, float duration)
        {
            SetBonePositionOffsetCurves(clip, upper,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, -0.035f), new Keyframe(0.82f, -0.060f), new Keyframe(1.05f, -0.020f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, 0.025f), new Keyframe(0.82f, 0.060f), new Keyframe(1.05f, -0.030f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.82f, -0.024f), new Keyframe(duration, 0f) });
            SetBonePositionOffsetCurves(clip, lower,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, -0.060f), new Keyframe(0.82f, -0.095f), new Keyframe(1.05f, -0.015f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, 0.035f), new Keyframe(0.82f, 0.090f), new Keyframe(1.05f, -0.040f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.82f, -0.030f), new Keyframe(duration, 0f) });
            SetBonePositionOffsetCurves(clip, foot,
                x: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, -0.080f), new Keyframe(0.82f, -0.125f), new Keyframe(1.05f, 0.025f), new Keyframe(duration, 0f) },
                y: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.58f, 0.045f), new Keyframe(0.82f, 0.120f), new Keyframe(1.05f, -0.060f), new Keyframe(duration, 0f) },
                z: new[] { new Keyframe(0.00f, 0f), new Keyframe(0.82f, -0.040f), new Keyframe(duration, 0f) });
        }

        private static Keyframe[] PeriodicKeys(float duration, float phase, Func<float, float> evaluate)
        {
            const int samples = 32;
            var keys = new Keyframe[samples + 1];
            for (var index = 0; index <= samples; index++)
            {
                var time = duration * index / samples;
                var progress = Mathf.Repeat((time / duration) - phase, 1f);
                keys[index] = new Keyframe(time, evaluate(progress));
            }

            return keys;
        }

        private static float LegFootX(float progress, float strideX)
        {
            if (progress < 0.34f)
            {
                var swing = SmoothStep01(progress / 0.34f);
                return Mathf.Lerp(-0.62f * strideX, 0.74f * strideX, swing);
            }

            var stance = SmoothStep01((progress - 0.34f) / 0.66f);
            return Mathf.Lerp(0.74f * strideX, -0.62f * strideX, stance);
        }

        private static float LegFootY(float progress, float liftY, float supportDropY)
        {
            if (progress < 0.34f)
            {
                var swing = Mathf.Clamp01(progress / 0.34f);
                return Mathf.Sin(swing * Mathf.PI) * liftY;
            }

            var stance = Mathf.Clamp01((progress - 0.34f) / 0.66f);
            return -supportDropY * Mathf.Sin(stance * Mathf.PI);
        }

        private static float LegFootZ(float progress, float lateralZ)
        {
            if (progress < 0.34f)
            {
                var swing = Mathf.Clamp01(progress / 0.34f);
                return lateralZ * Mathf.Sin(swing * Mathf.PI);
            }

            return lateralZ * 0.18f * Mathf.Sin((progress - 0.34f) / 0.66f * Mathf.PI);
        }

        private static float LegLowerX(float progress, float strideX)
        {
            return LegFootX(progress, strideX) * (progress < 0.34f ? 0.52f : 0.28f);
        }

        private static float LegLowerY(float progress, float liftY, float supportDropY)
        {
            if (progress < 0.34f)
            {
                var swing = Mathf.Clamp01(progress / 0.34f);
                return LegFootY(progress, liftY, supportDropY) * 0.60f + Mathf.Sin(swing * Mathf.PI) * liftY * 0.22f;
            }

            return LegFootY(progress, liftY, supportDropY) * 0.58f;
        }

        private static float LegLowerZ(float progress, float lateralZ)
        {
            return LegFootZ(progress, lateralZ) * 0.58f;
        }

        private static float LegUpperX(float progress, float strideX)
        {
            return LegFootX(progress, strideX) * (progress < 0.34f ? 0.18f : 0.10f);
        }

        private static float LegUpperY(float progress, float liftY, float supportDropY)
        {
            return LegFootY(progress, liftY, supportDropY) * (progress < 0.34f ? 0.22f : 0.35f);
        }

        private static float LegUpperZ(float progress, float lateralZ)
        {
            return LegFootZ(progress, lateralZ) * 0.22f;
        }

        private static float BladeTipX(float progress, float strideX)
        {
            if (progress < 0.42f)
            {
                var swing = SmoothStep01(progress / 0.42f);
                return Mathf.Lerp(-0.68f * strideX, 0.48f * strideX, swing);
            }

            var drag = SmoothStep01((progress - 0.42f) / 0.58f);
            return Mathf.Lerp(0.48f * strideX, -0.82f * strideX, drag);
        }

        private static float BladeTipY(float progress)
        {
            if (progress < 0.42f)
            {
                var swing = Mathf.Clamp01(progress / 0.42f);
                return Mathf.Sin(swing * Mathf.PI) * 0.058f - 0.010f;
            }

            var drag = Mathf.Clamp01((progress - 0.42f) / 0.58f);
            return -0.050f - 0.018f * Mathf.Sin(drag * Mathf.PI);
        }

        private static float BladeForearmX(float progress, float strideX)
        {
            return BladeTipX(progress, strideX) * 0.56f;
        }

        private static float BladeForearmY(float progress)
        {
            return BladeTipY(progress) * 0.72f + (progress < 0.42f ? 0.030f : 0.000f);
        }

        private static float BladeUpperX(float progress, float strideX)
        {
            return BladeTipX(progress, strideX) * 0.25f;
        }

        private static float BladeUpperY(float progress)
        {
            return BladeTipY(progress) * 0.32f + (progress < 0.42f ? 0.018f : -0.004f);
        }

        private static float BladeSideZ(float progress, float lateralZ)
        {
            return lateralZ * Mathf.Sin(progress * Mathf.PI);
        }

        private static float SmoothStep01(float value)
        {
            var t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private static void SetScaleCurve(AnimationClip clip, string pivotPath, string axis, params Keyframe[] keys)
        {
            SetCurve(clip, pivotPath, typeof(Transform), "localScale." + axis, keys);
        }

        private static void SetBonePositionOffsetCurves(AnimationClip clip, BoneBinding bone, Keyframe[] x, Keyframe[] y, Keyframe[] z)
        {
            SetOffsetCurve(clip, bone.Path, typeof(Transform), "localPosition.x", bone.LocalPosition.x, x);
            SetOffsetCurve(clip, bone.Path, typeof(Transform), "localPosition.y", bone.LocalPosition.y, y);
            SetOffsetCurve(clip, bone.Path, typeof(Transform), "localPosition.z", bone.LocalPosition.z, z);
        }

        private static void SetBoneEulerOffsetCurves(AnimationClip clip, BoneBinding bone, string axis, params Keyframe[] keys)
        {
            var baseValue = axis switch
            {
                "x" => bone.LocalEulerAngles.x,
                "y" => bone.LocalEulerAngles.y,
                "z" => bone.LocalEulerAngles.z,
                _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "Unknown transform axis.")
            };
            SetOffsetCurve(clip, bone.Path, typeof(Transform), "localEulerAnglesRaw." + axis, baseValue, keys);
        }

        private static void SetBlendShapeCurve(AnimationClip clip, string rendererPath, string shapeName, params Keyframe[] keys)
        {
            SetCurve(clip, rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + shapeName, keys);
        }

        private static void SetOffsetCurve(AnimationClip clip, string path, Type type, string propertyName, float baseValue, params Keyframe[] offsets)
        {
            var keys = new Keyframe[offsets.Length];
            for (var i = 0; i < offsets.Length; i++)
            {
                keys[i] = new Keyframe(offsets[i].time, baseValue + offsets[i].value, offsets[i].inTangent, offsets[i].outTangent);
                keys[i].weightedMode = offsets[i].weightedMode;
            }

            SetCurve(clip, path, type, propertyName, keys);
        }

        private static void SetCurve(AnimationClip clip, string path, Type type, string propertyName, params Keyframe[] keys)
        {
            clip.SetCurve(path, type, propertyName, new AnimationCurve(keys));
        }

        private static void ConfigureAnimationReviewState(
            GameObject instance,
            AnimationStateSpec spec,
            Dictionary<string, AnimationClip> clips,
            Dictionary<string, AnimatorController> controllers)
        {
            if (!clips.TryGetValue(spec.ClipName, out var clip))
            {
                throw new InvalidOperationException($"Longa Arma animation clip is missing for scene placement: {spec.ClipName}");
            }

            if (!controllers.TryGetValue(spec.ClipName, out var controller))
            {
                throw new InvalidOperationException($"Longa Arma animator controller is missing for scene placement: {spec.ClipName}");
            }

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.enabled = true;

            clip.SampleAnimation(instance, Mathf.Clamp(spec.SampleTime, 0f, Mathf.Max(clip.length, spec.SampleTime)));
            MarkHierarchyDirty(instance.transform);
        }

        private static void MarkHierarchyDirty(Transform root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                EditorUtility.SetDirty(transform);
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                EditorUtility.SetDirty(renderer);
            }
        }

        private static Vector3 CalculateLongaPlacementPosition()
        {
            var corridorBounds = FindRendererBounds(CorridorRootName, new Bounds(Vector3.zero, new Vector3(16f, 3f, 12f)));
            var parvumRoot = RequireObject(ParvumPlacementRootName);
            var fugaRoot = RequireObject(FugaPlacementRootName);
            var fugaParvumGap = Mathf.Max(Mathf.Abs(fugaRoot.transform.position.z - parvumRoot.transform.position.z), MinimumFugaParvumZGap);
            var directionFromParvumToFuga = Mathf.Sign(fugaRoot.transform.position.z - parvumRoot.transform.position.z);
            if (Mathf.Abs(directionFromParvumToFuga) < 0.001f)
            {
                directionFromParvumToFuga = -1f;
            }

            return new Vector3(
                fugaRoot.transform.position.x,
                corridorBounds.min.y,
                fugaRoot.transform.position.z + directionFromParvumToFuga * fugaParvumGap);
        }

        private static void AlignRootBottomToCorridorFloor(Transform placementRoot)
        {
            var corridorBounds = FindRendererBounds(CorridorRootName, new Bounds(Vector3.zero, new Vector3(16f, 3f, 12f)));
            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var yOffset = corridorBounds.min.y - bounds.min.y;
            placementRoot.position += new Vector3(0f, yOffset, 0f);
        }

        private static void ConfigureInitialReviewCamera(Transform placementRoot)
        {
            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var frontReference = FindLongaFrontReference(placementRoot);
            var camera = FindOrCreateReviewCamera();
            var reviewDirection = CalculateLongaReviewCameraDirection(frontReference);
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.24f);
            var distance = Mathf.Clamp(
                bounds.extents.z + Mathf.Clamp(bounds.extents.x * 0.16f, 0f, 1.80f) + 2.85f,
                ReviewCameraMinimumFrontDistance,
                ReviewCameraMaximumFrontDistance);
            var verticalOffset = Mathf.Clamp(bounds.extents.y * 0.10f, 0.04f, 0.14f);
            var position = lookAt + reviewDirection * distance + Vector3.up * verticalOffset;

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = distance + Mathf.Max(bounds.extents.z, bounds.extents.x) + 2.00f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.12f, 1f);
            camera.orthographic = false;
            camera.orthographicSize = CalculateReviewOrthographicSize(bounds, VisualCaptureWidth, VisualCaptureHeight);
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

            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.24f);
            var frontDirection = CalculateLongaVisualFrontDirection(FindLongaFrontReference(placementRoot));
            var startPosition = new Vector3(
                lookAt.x + frontDirection.x * PlayerFrontDistance,
                0f,
                lookAt.z + frontDirection.z * PlayerFrontDistance);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
        }

        private static void InspectSceneState(Transform placementRoot)
        {
            foreach (var spec in AnimationStateSpecs)
            {
                var child = placementRoot.Find(spec.ObjectName);
                if (child == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing under {PlacementRootName}.");
                }

                var renderers = child.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} has no renderers.");
                }

                var yaw = child.eulerAngles.y;
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, LongaFacingYawDegrees)) > 0.5f)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} yaw must be {LongaFacingYawDegrees:0.###}, but was {yaw:0.###}.");
                }

                var animator = child.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} must have an AnimatorController assigned.");
                }

                ValidateRuntimeLowPolyMesh(child, spec.ObjectName);
            }

            InspectFugaZSpacing(placementRoot);
            InspectPlayerStart(placementRoot);
            InspectReviewCamera(placementRoot);
        }

        private static void ValidateRuntimeLowPolyMesh(Transform child, string objectName)
        {
            RequireRuntimeBoneBindings(child.gameObject);
            var skinnedRenderers = child.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var meshFilters = child.GetComponentsInChildren<MeshFilter>(true);
            if (skinnedRenderers.Length == 0 && meshFilters.Length == 0)
            {
                throw new InvalidOperationException($"{objectName} must contain a runtime low-poly renderer.");
            }

            var totalTriangles = 0;
            foreach (var skinnedRenderer in skinnedRenderers)
            {
                if (skinnedRenderer.sharedMesh == null)
                {
                    continue;
                }

                ValidateRequiredBlendShapes(skinnedRenderer, objectName);
                totalTriangles += skinnedRenderer.sharedMesh.triangles.Length / 3;
            }

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh == null)
                {
                    continue;
                }

                totalTriangles += meshFilter.sharedMesh.triangles.Length / 3;
            }

            if (totalTriangles <= 0)
            {
                throw new InvalidOperationException($"{objectName} has no measurable low-poly triangles.");
            }

            if (totalTriangles > RuntimeLowPolyMaximumTriangles)
            {
                throw new InvalidOperationException(
                    $"{objectName} exceeds the runtime low-poly triangle budget. Triangles={totalTriangles}, Budget={RuntimeLowPolyMaximumTriangles}.");
            }
        }

        private static void InspectFugaZSpacing(Transform placementRoot)
        {
            var parvumRoot = RequireObject(ParvumPlacementRootName);
            var fugaRoot = RequireObject(FugaPlacementRootName);
            var fugaParvumGap = Mathf.Max(Mathf.Abs(fugaRoot.transform.position.z - parvumRoot.transform.position.z), MinimumFugaParvumZGap);
            var longaFugaGap = Mathf.Abs(placementRoot.position.z - fugaRoot.transform.position.z);
            if (Mathf.Abs(longaFugaGap - fugaParvumGap) > 0.05f)
            {
                throw new InvalidOperationException(
                    $"Longa Arma is not using the Parvum/Fuga Z gap. Longa/Fuga={longaFugaGap:0.###}, Parvum/Fuga={fugaParvumGap:0.###}.");
            }

            var fugaDirection = Mathf.Sign(fugaRoot.transform.position.z - parvumRoot.transform.position.z);
            var longaDirection = Mathf.Sign(placementRoot.position.z - fugaRoot.transform.position.z);
            if (Mathf.Abs(fugaDirection) > 0.001f && Mathf.Abs(longaDirection) > 0.001f && Mathf.Sign(fugaDirection) != Mathf.Sign(longaDirection))
            {
                throw new InvalidOperationException("Longa Arma must be placed below Fuga along the same Z direction from Parvum to Fuga.");
            }
        }

        private static void InspectPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Player start transform is missing.");
            }

            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var lookAt = bounds.center;
            var expectedFront = CalculateLongaVisualFrontDirection(FindLongaFrontReference(placementRoot));
            var playerFromFocus = player.position - lookAt;
            playerFromFocus.y = 0f;

            if (playerFromFocus.sqrMagnitude < 0.001f || Vector3.Dot(playerFromFocus.normalized, expectedFront) < 0.94f)
            {
                throw new InvalidOperationException("Player start is not placed in front of Longa Arma.");
            }

            var toFocus = lookAt - player.position;
            toFocus.y = 0f;
            if (toFocus.sqrMagnitude < 0.001f || Vector3.Dot(player.forward, toFocus.normalized) < 0.94f)
            {
                throw new InvalidOperationException("Player start is not facing Longa Arma.");
            }
        }

        private static void InspectReviewCamera(Transform placementRoot)
        {
            var camera = FindReviewCamera();
            if (camera == null)
            {
                throw new InvalidOperationException("Longa Arma review camera is missing.");
            }

            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var expectedFront = CalculateLongaVisualFrontDirection(FindLongaFrontReference(placementRoot));
            var cameraFromFocus = camera.transform.position - bounds.center;
            cameraFromFocus.y = 0f;
            if (cameraFromFocus.sqrMagnitude < 0.001f || Vector3.Dot(cameraFromFocus.normalized, expectedFront) < 0.94f)
            {
                throw new InvalidOperationException("Longa Arma review camera is not placed on the visual front.");
            }

            var toFocus = (bounds.center - camera.transform.position).normalized;
            if (Vector3.Dot(camera.transform.forward, toFocus) < 0.94f)
            {
                throw new InvalidOperationException("Longa Arma review camera is not looking at the model.");
            }
        }

        private static Transform FindLongaFocus(Transform placementRoot)
        {
            return placementRoot;
        }

        private static Transform FindLongaFrontReference(Transform placementRoot)
        {
            var staticReview = placementRoot.Find(PlacementObjectName);
            if (staticReview != null)
            {
                return staticReview;
            }

            return placementRoot.childCount > 0 ? placementRoot.GetChild(0) : placementRoot;
        }

        private static Vector3 CalculateLongaVisualFrontDirection(Transform focus)
        {
            var yawRotation = Quaternion.Euler(0f, focus.eulerAngles.y, 0f);
            var front = yawRotation * Vector3.forward;
            front.y = 0f;
            return front.sqrMagnitude > 0.001f ? front.normalized : Vector3.back;
        }

        private static Vector3 CalculateLongaReviewCameraDirection(Transform focus)
        {
            var front = CalculateLongaVisualFrontDirection(focus);
            return front.sqrMagnitude > 0.001f ? front : Vector3.back;
        }

        private static float CalculateReviewOrthographicSize(Bounds bounds, int width, int height)
        {
            var aspect = width / (float)height;
            var heightFit = bounds.extents.y * 1.12f;
            var widthFit = bounds.extents.x / aspect * 1.68f;
            return Mathf.Clamp(Mathf.Max(heightFit, widthFit), 0.70f, 5.80f);
        }

        private static void ConfigureReviewLighting(Transform placementRoot)
        {
            var existing = placementRoot.Find(ReviewLightName);
            var lightObject = existing != null ? existing.gameObject : new GameObject(ReviewLightName);
            lightObject.transform.SetParent(placementRoot, false);

            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var frontDirection = CalculateLongaVisualFrontDirection(FindLongaFrontReference(placementRoot));
            var lightPosition = bounds.center + frontDirection * 2.4f + Vector3.up * Mathf.Max(1.4f, bounds.extents.y * 1.2f);
            lightObject.transform.position = lightPosition;
            lightObject.transform.rotation = Quaternion.LookRotation((bounds.center - lightPosition).normalized, Vector3.up);

            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Spot;
            light.range = 9f;
            light.intensity = 5.6f;
            light.spotAngle = 64f;
            light.innerSpotAngle = 38f;
            light.shadows = LightShadows.Soft;

            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(lightObject.transform);
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

        private static void CaptureCamera(Camera camera, string outputPath, int width, int height)
        {
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                capture.Apply();
                RequireNonBlankCapture(capture);
                File.WriteAllBytes(outputPath, capture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(capture);
            }
        }

        private static void RequireNonBlankCapture(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            byte minimum = byte.MaxValue;
            byte maximum = byte.MinValue;
            var step = Math.Max(1, pixels.Length / 4096);
            for (var i = 0; i < pixels.Length; i += step)
            {
                var pixel = pixels[i];
                var brightness = (byte)((pixel.r + pixel.g + pixel.b) / 3);
                if (brightness < minimum)
                {
                    minimum = brightness;
                }

                if (brightness > maximum)
                {
                    maximum = brightness;
                }
            }

            if (maximum - minimum < 6)
            {
                throw new InvalidOperationException("Longa Arma Unity camera capture appears blank or nearly uniform.");
            }
        }

        private static void BuildSideBySideImage(string approvedPath, string unityPath, string outputPath)
        {
            var approved = LoadPngTexture(approvedPath);
            var unity = LoadPngTexture(unityPath);
            var width = approved.width + SideBySideGapPixels + unity.width;
            var height = Mathf.Max(approved.height, unity.height);
            var combined = new Texture2D(width, height, TextureFormat.RGBA32, false);

            try
            {
                var background = new Color32(22, 24, 24, 255);
                var fill = new Color32[width * height];
                for (var i = 0; i < fill.Length; i++)
                {
                    fill[i] = background;
                }

                combined.SetPixels32(fill);
                var approvedY = (height - approved.height) / 2;
                var unityY = (height - unity.height) / 2;
                combined.SetPixels32(0, approvedY, approved.width, approved.height, approved.GetPixels32());
                combined.SetPixels32(approved.width + SideBySideGapPixels, unityY, unity.width, unity.height, unity.GetPixels32());
                combined.Apply();
                File.WriteAllBytes(outputPath, combined.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(approved);
                UnityEngine.Object.DestroyImmediate(unity);
                UnityEngine.Object.DestroyImmediate(combined);
            }
        }

        private static Texture2D LoadPngTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException($"Could not load PNG texture: {path}");
            }

            return texture;
        }

        private static void EnsurePrefabPhysics(GameObject root)
        {
            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = root.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var collider = root.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = root.AddComponent<BoxCollider>();
            }

            ConfigureColliderFromRenderers(root.transform, collider);
        }

        private static void ConfigureScenePhysics(Transform root)
        {
            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = root.gameObject.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var collider = root.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = root.gameObject.AddComponent<BoxCollider>();
            }

            ConfigureColliderFromRenderers(root, collider);
        }

        private static void AssignMaterials(GameObject root, MaterialSet materialSet)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var sourceName = (renderer.name + " " + (materials[i] != null ? materials[i].name : string.Empty)).ToLowerInvariant();
                    materials[i] = SelectMaterialForSource(sourceName, i, materials.Length, materialSet);
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static Material SelectMaterialForSource(string sourceName, int materialIndex, int materialCount, MaterialSet materialSet)
        {
            if (sourceName.Contains("blade", StringComparison.Ordinal) ||
                sourceName.Contains("edge", StringComparison.Ordinal) ||
                sourceName.Contains("worn", StringComparison.Ordinal) ||
                sourceName.Contains("metal", StringComparison.Ordinal) ||
                (materialCount > 1 && materialIndex == materialCount - 1))
            {
                return materialSet.Blade;
            }

            return materialSet.Body;
        }

        private static void ConfigureColliderFromRenderers(Transform root, BoxCollider collider)
        {
            var bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            collider.center = root.InverseTransformPoint(bounds.center);
            var lossyScale = root.lossyScale;
            collider.size = new Vector3(
                SafeDivide(bounds.size.x, Mathf.Abs(lossyScale.x)),
                SafeDivide(bounds.size.y, Mathf.Abs(lossyScale.y)),
                SafeDivide(bounds.size.z, Mathf.Abs(lossyScale.z)));
        }

        private static Bounds FindRendererBounds(string objectName, Bounds fallback)
        {
            var root = GameObject.Find(objectName);
            return root != null ? CalculateRendererBounds(root.transform, fallback) : fallback;
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

        private static Material EnsureMaterial(
            string name,
            Texture2D albedo,
            Texture2D normal,
            Color fallbackColor,
            float smoothness,
            float metallic)
        {
            var path = UnityMaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            SetMaterialColor(material, fallbackColor);
            SetMaterialFloat(material, "_Smoothness", smoothness);
            SetMaterialFloat(material, "_Glossiness", smoothness);
            SetMaterialFloat(material, "_Metallic", metallic);
            SetMaterialTexture(material, "_BaseMap", albedo);
            SetMaterialTexture(material, "_MainTex", albedo);

            if (normal != null)
            {
                SetMaterialTexture(material, "_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            }

            EditorUtility.SetDirty(material);
            return material;
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

        private static void SetMaterialFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void SetMaterialTexture(Material material, string property, Texture texture)
        {
            if (texture != null && material.HasProperty(property))
            {
                material.SetTexture(property, texture);
            }
        }

        private static Texture2D LoadTexture(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(UnityTextureFolder + "/" + fileName);
        }

        private static float SafeDivide(float value, float divisor)
        {
            return divisor > 0.0001f ? value / divisor : value;
        }

        private static GameObject RequireObject(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            if (gameObject == null)
            {
                throw new InvalidOperationException($"{objectName} is required in CargoRunMvp scene.");
            }

            return gameObject;
        }

        private static void CopyFileToAsset(string sourceFullPath, string targetAssetPath)
        {
            var targetFullPath = ProjectPath(targetAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath) ?? ProjectRoot);
            File.Copy(sourceFullPath, targetFullPath, true);
        }

        private static void EnsureUnityFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var parts = assetFolder.Split('/');
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

        private static string ProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private readonly struct AnimationStateSpec
        {
            public AnimationStateSpec(string objectName, string clipName, Vector3 localOffset, float sampleTime, bool loop)
            {
                ObjectName = objectName;
                ClipName = clipName;
                LocalOffset = localOffset;
                SampleTime = sampleTime;
                Loop = loop;
            }

            public string ObjectName { get; }
            public string ClipName { get; }
            public Vector3 LocalOffset { get; }
            public float SampleTime { get; }
            public bool Loop { get; }
        }

        private readonly struct MaterialSet
        {
            public MaterialSet(Material body, Material blade)
            {
                Body = body;
                Blade = blade;
            }

            public Material Body { get; }
            public Material Blade { get; }
        }

        private readonly struct BoneBinding
        {
            public BoneBinding(string path, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
            {
                Path = path;
                LocalPosition = localPosition;
                LocalEulerAngles = localEulerAngles;
                LocalScale = localScale;
            }

            public string Path { get; }
            public Vector3 LocalPosition { get; }
            public Vector3 LocalEulerAngles { get; }
            public Vector3 LocalScale { get; }
        }
    }
}
