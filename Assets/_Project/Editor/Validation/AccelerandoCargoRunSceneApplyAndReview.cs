using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AccelerandoCargoRunScene
{
    internal static class AccelerandoCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LongaArmaPlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoPlacementRootName = "Approved Tergo Enemy Placement";
        private const string ConSpiritoPlacementRootName = "Approved Con Spirito Enemy Placement";
        private const string PlacementRootName = "Approved Accelerando Enemy Placement";
        private const string PlacementObjectName = "Accelerando_00_Static_Review";
        private const string ModelChildName = "Accelerando_Model";
        private const string AnimationSlotsFrontCaptureName = "Accelerando_AnimationSlots_Front.png";
        private const string AnimationSlotsObliqueCaptureName = "Accelerando_AnimationSlots_Oblique.png";
        private const string AntennaTipConnectionLeftSideCloseupName = "Accelerando_AntennaTipConnection_LeftSideCloseup.png";
        private const string AntennaTipConnectionRightSideCloseupName = "Accelerando_AntennaTipConnection_RightSideCloseup.png";
        private const string MaceChainLeftCloseupName = "Accelerando_MaceChain_NoPierce_LeftCloseup.png";
        private const string MaceChainRightCloseupName = "Accelerando_MaceChain_NoPierce_RightCloseup.png";
        private const string ReviewCameraName = "Model Cam";
        private const string PlayerRootName = "Player";

        private const string SourceModelAbsolutePath = "D:/Bellerophon2/Bellerophon/artSample/enemies/accelerando/antenna_tip_ring_embedded_connection_fix/exports/accelerando_antenna_tip_ring_embedded_connection_sample.glb";
        private const string AccelerandoArtRoot = "Assets/_Project/Art/Enemies/Accelerando";
        private const string UnityModelFolder = AccelerandoArtRoot + "/Models";
        private const string UnityMaterialFolder = AccelerandoArtRoot + "/Materials";
        private const string UnityAnimationFolder = AccelerandoArtRoot + "/Animations";
        private const string UnityControllerFolder = AccelerandoArtRoot + "/Controllers";
        private const string UnityModelAssetPath = UnityModelFolder + "/accelerando_antenna_tip_ring_embedded_connection_sample.glb";
        private const string UnityFallbackMaterialAssetPath = UnityMaterialFolder + "/M_Accelerando_Fallback_URP.mat";
        private const string UnityApprovedFleshMaterialAssetPath = UnityMaterialFolder + "/M_Accelerando_Approved_WetTaupeFlesh_URP.mat";
        private const string UnityApprovedShellMaterialAssetPath = UnityMaterialFolder + "/M_Accelerando_Approved_DarkShell_URP.mat";
        private const string UnityApprovedMetalMaterialAssetPath = UnityMaterialFolder + "/M_Accelerando_Approved_RustyMetal_URP.mat";
        private const string UnityIdleBreathMorphMeshAssetPath = UnityModelFolder + "/accelerando_idle_breath_morph_Mesh1.asset";
        private const string UnityIdleBreathClipAssetPath = UnityAnimationFolder + "/Accelerando_Idle_Breath_Morph.anim";
        private const string UnityIdleBreathControllerAssetPath = UnityControllerFolder + "/Accelerando_Idle_Breath_Morph.controller";
        private const string UnityCrawlForwardClipAssetPath = UnityAnimationFolder + "/Accelerando_Crawl_Forward_Loop.anim";
        private const string UnityCrawlForwardControllerAssetPath = UnityControllerFolder + "/Accelerando_Crawl_Forward_Loop.controller";
        private const string UnityAntennaStrikeClipAssetPath = UnityAnimationFolder + "/Accelerando_Antenna_Strike_Attack.anim";
        private const string UnityAntennaStrikeControllerAssetPath = UnityControllerFolder + "/Accelerando_Antenna_Strike_Attack.controller";
        private const string ValidationFolder = "docs/validation/accelerando";
        private const string IdleBreathBlendShapeName = "Accelerando_IdleBreathMorph";
        private const string IdleSlotObjectName = "Accelerando_01_Idle_Detect";
        private const string CrawlSlotObjectName = "Accelerando_02_Crawl_Accelerating";
        private const string AntennaStrikeSlotObjectName = "Accelerando_03_Antenna_Strike";
        private const string ChainPhysicsRootObjectName = "Accelerando_ChainPhysicsRoot";
        private const string IdleBreathStateName = "IdleBreathMorph";
        private const string CrawlForwardStateName = "CrawlForwardLoop";
        private const string AntennaStrikeStateName = "AntennaStrikeAttack";
        private const string IdleBreathRootBoneName = "Accelerando_IdleBreath_RootBone";
        private const string IdleBreathBodyObjectName = "Accelerando_IdleBreath_Body";
        private const string IdleBreathAntennaObjectName = "Accelerando_IdleBreath_StaticMaceAntennae";

        private const float AccelerandoFacingYawDegrees = 180f;
        private const float FallbackLongaTergoSpacing = 4.00f;
        private const float ReviewCameraMinimumFrontDistance = 3.25f;
        private const float ReviewCameraMaximumFrontDistance = 8.00f;
        private const float PlayerFrontDistance = 4.20f;
        private const float PlateRemovalHeightFraction = 0.115f;
        private const float PlateRemovalMinimumHeight = 0.16f;
        private const float PlateRemovalMaximumHeight = 0.24f;
        private const float PlateRemovalEpsilon = 0.002f;
        private const int ReviewCaptureLayer = 30;
        private const float AnimationSlotSpacingX = 6.10f;
        private const float AnimationSlotRowLocalZ = 0.00f;
        private const float IdleBreathLoopSeconds = 1.60f;
        private const float IdleBreathRadialScale = 0.052f;
        private const float IdleBreathVerticalScale = 0.026f;
        private const float IdleBreathTransformScaleXz = 0.000f;
        private const float IdleBreathTransformScaleY = 0.000f;
        private const float IdleBreathTransformLiftY = 0.000f;
        private const float CrawlForwardLoopSeconds = 1.20f;
        private const float CrawlForwardBodyForeAftMinimumRange = 0.035f;
        private const float CrawlForwardBodyScaleMinimumRange = 0.040f;
        private const float CrawlForwardChainMinimumRange = 0.040f;
        private const float CrawlForwardMaceMinimumRange = 30.000f;
        private const float CrawlForwardPhysicsDrivenCurveMaximumRange = 0.001f;
        private const float AntennaStrikeLoopSeconds = 1.20f;
        private const float AntennaStrikeInputMinimumRange = 0.100f;
        private const float AntennaStrikeMinimumMaceForwardRange = 0.120f;
        private const float AntennaStrikeMinimumMaceAmplification = 1.12f;
        private const float CrawlChainPhysicsMaximumMaceDrop = 0.220f;
        private const float CrawlChainPhysicsMinimumMaceHorizontalRange = 0.018f;
        private const float CrawlChainPhysicsMinimumGravitySag = 0.012f;
        private const float CrawlChainPhysicsMinimumLastLinkMaceVerticalOffset = 0.070f;
        private const float CrawlChainPhysicsMinimumMaceBoundsClearance = 0.010f;
        private const float CrawlChainPhysicsMinimumMaceTopClearance = 0.040f;
        private const float ConnectedChainMinimumMaceBoundsClearance = 0.015f;
        private const float ConnectedChainMinimumMaceTopClearance = 0.040f;
        private const float ConnectedChainMaximumMaceTopClearance = 0.450f;
        private const float IdleBreathAntennaHorizontalThreshold = 0.48f;
        private const float IdleBreathAntennaRaisedHorizontalThreshold = 0.31f;
        private const float IdleBreathAntennaRaisedVerticalThreshold = 0.42f;
        private const float IdleBreathAntennaRootHorizontalThreshold = 0.12f;
        private const float IdleBreathAntennaRootVerticalThreshold = 0.26f;
        private const float IdleBreathBodyFadeStartVertical = 0.34f;
        private const float IdleBreathStaticShellVerticalThreshold = 0.40f;
        private const int ConnectedChainLinkCount = 12;
        private const int ConnectedChainVisibleLinkCount = 8;
        private const float ConnectedChainMaceSideCutbackDistance = 0.420f;
        private const float ConnectedChainMaceSideCutbackAnchorFactor = 0.70f;
        private const float ConnectedChainMinimumAnchorDistance = 0.03f;
        private const float ConnectedChainEndpointTolerance = 0.015f;
        private const float ConnectedChainMaceSideCutbackTolerance = 0.030f;
        private const float AntennaTipEmbeddedHoopCenterTolerance = 0.012f;
        private const float AntennaTipEmbeddedPartTolerance = 0.090f;
        private static readonly Color AccelerandoFallbackColor = new(0.26f, 0.31f, 0.34f, 1f);
        private static readonly Color AccelerandoApprovedFleshColor = new(0.39f, 0.32f, 0.27f, 1f);
        private static readonly Color AccelerandoApprovedShellColor = new(0.14f, 0.12f, 0.10f, 1f);
        private static readonly Color AccelerandoApprovedMetalColor = new(0.30f, 0.15f, 0.08f, 1f);
        private static readonly Dictionary<int, PlateRemovedMeshCacheEntry> PlateRemovedMeshCache = new();
        private static readonly AnimationReviewSlot[] AnimationReviewSlots =
        {
            new("Accelerando_01_Idle_Detect", "IdleDetect", "대기/감지 대기"),
            new("Accelerando_02_Crawl_Accelerating", "CrawlAccelerating", "가속 기어 이동"),
            new("Accelerando_03_Antenna_Strike", "AntennaStrike", "고개 좌우 흔들기 및 더듬이 타격"),
            new("Accelerando_04_Disabled_Reset", "DisabledReset", "행동불가/공격속도 초기화"),
            new("Accelerando_05_Hit_Recoil", "HitRecoil", "피격 반응"),
            new("Accelerando_06_Death", "Death", "사망")
        };

        private readonly struct AnimationReviewSlot
        {
            public AnimationReviewSlot(string objectName, string stateId, string koreanName)
            {
                ObjectName = objectName;
                StateId = stateId;
                KoreanName = koreanName;
            }

            public string ObjectName { get; }
            public string StateId { get; }
            public string KoreanName { get; }
        }

        private sealed class PlateRemovedMeshCacheEntry
        {
            public Mesh Mesh { get; set; }
            public int RemovedTriangles { get; set; }
        }

        private sealed class ApprovedMaterialSet
        {
            public ApprovedMaterialSet(Material flesh, Material shell, Material metal)
            {
                Flesh = flesh;
                Shell = shell;
                Metal = metal;
            }

            public Material Flesh { get; }
            public Material Shell { get; }
            public Material Metal { get; }

            public Material Resolve(string rendererName, Material sourceMaterial)
            {
                var sourceName = sourceMaterial != null ? sourceMaterial.name : string.Empty;
                var combinedName = (rendererName + " " + sourceName).ToLowerInvariant();
                if (combinedName.Contains("embeddedfleshpressure", StringComparison.Ordinal))
                {
                    return Flesh;
                }

                if (combinedName.Contains("embeddedsocket", StringComparison.Ordinal) ||
                    combinedName.Contains("embeddedsurface", StringComparison.Ordinal) ||
                    combinedName.Contains("embeddedupperocclusion", StringComparison.Ordinal))
                {
                    return Shell;
                }

                if (combinedName.Contains("rusty", StringComparison.Ordinal) ||
                    combinedName.Contains("metal", StringComparison.Ordinal) ||
                    combinedName.Contains("iron", StringComparison.Ordinal) ||
                    combinedName.Contains("chain", StringComparison.Ordinal) ||
                    combinedName.Contains("mace", StringComparison.Ordinal) ||
                    combinedName.Contains("torus", StringComparison.Ordinal) ||
                    combinedName.Contains("ring", StringComparison.Ordinal) ||
                    combinedName.Contains("anchor", StringComparison.Ordinal) ||
                    combinedName.Contains("collar", StringComparison.Ordinal) ||
                    combinedName.Contains("pin", StringComparison.Ordinal) ||
                    combinedName.Contains("saddle", StringComparison.Ordinal) ||
                    combinedName.Contains("yoke", StringComparison.Ordinal) ||
                    combinedName.Contains("rivet", StringComparison.Ordinal))
                {
                    return Metal;
                }

                if (combinedName.Contains("shell", StringComparison.Ordinal) ||
                    combinedName.Contains("plate", StringComparison.Ordinal) ||
                    combinedName.Contains("armored", StringComparison.Ordinal) ||
                    combinedName.Contains("dark", StringComparison.Ordinal))
                {
                    return Shell;
                }

                return Flesh;
            }

            public bool Contains(Material material)
            {
                return material == Flesh || material == Shell || material == Metal;
            }
        }

        private readonly struct IdleBreathMeshSplit
        {
            public IdleBreathMeshSplit(Mesh bodyMesh, Mesh antennaMesh, int bodyTriangleCount, int antennaTriangleCount, int antennaComponentCount)
            {
                BodyMesh = bodyMesh;
                AntennaMesh = antennaMesh;
                BodyTriangleCount = bodyTriangleCount;
                AntennaTriangleCount = antennaTriangleCount;
                AntennaComponentCount = antennaComponentCount;
            }

            public Mesh BodyMesh { get; }
            public Mesh AntennaMesh { get; }
            public int BodyTriangleCount { get; }
            public int AntennaTriangleCount { get; }
            public int AntennaComponentCount { get; }
        }

        private sealed class MeshComponentInfo
        {
            public int TriangleCount { get; set; }
            public Bounds Bounds { get; private set; }
            public bool HasBounds { get; private set; }

            public void Include(Vector3 vertex)
            {
                if (!HasBounds)
                {
                    Bounds = new Bounds(vertex, Vector3.zero);
                    HasBounds = true;
                    return;
                }

                Bounds.Encapsulate(vertex);
            }
        }

        [MenuItem("Bellerophon/Enemies/Accelerando/Apply Prepared Model To CargoRunMvp")]
        public static void ApplyPreparedModelToCurrentCargoRunScene()
        {
            RequirePreparedModelFile();
            EnsureUnityFolders();
            CopyPreparedModelAsset();
            ConfigureImportedModelAsset();
            PlateRemovedMeshCache.Clear();

            var modelAsset = LoadPreparedModelAsset();
            var materialSet = EnsureApprovedMaterialSet();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlacePreparedModel(modelAsset, materialSet, scene);
            ConfigureInitialReviewCamera(placementRoot.transform);
            ConfigureInitialPlayerStart(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Accelerando model applied to CargoRunMvp scene.");
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
            Debug.Log("Prepared Accelerando CargoRunMvp scene state inspected.");
        }

        public static void CaptureReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            CaptureReviewImages(placementRoot.transform);
        }

        public static void CaptureAntennaTipConnectionCloseups()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            CaptureAntennaTipConnectionCloseupImages(placementRoot.transform);
        }

        public static void CaptureMaceChainCloseups()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            CaptureMaceChainCloseupImages(placementRoot.transform);
        }

        public static void CaptureAnimationSlotsReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            CaptureAnimationSlotReviewImages(placementRoot.transform);
        }

        public static void ApplyIdleBreathingAnimationToCurrentScene()
        {
            EnsureUnityFolders();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            ArrangeAlignedReviewRow(placementRoot.transform);
            ApplyIdleBreathingAnimation(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Accelerando idle breathing animation applied.");
        }

        public static void InspectIdleBreathingAnimationInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectIdleBreathingAnimation(placementRoot.transform);
            Debug.Log("Prepared Accelerando idle breathing animation inspected.");
        }

        public static void ApplyCrawlForwardMotionToCurrentScene()
        {
            EnsureUnityFolders();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            ApplyCrawlForwardMotion(placementRoot.transform);
            InspectCrawlForwardMotion(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Accelerando crawl forward motion applied.");
        }

        public static void InspectCrawlForwardMotionInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectCrawlForwardMotion(placementRoot.transform);
            Debug.Log("Prepared Accelerando crawl forward motion inspected.");
        }

        public static void ValidateCrawlForwardChainPhysicsResponseInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var crawlSlot = placementRoot.transform.Find(CrawlSlotObjectName);
            if (crawlSlot == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} is missing under {PlacementRootName}.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityCrawlForwardClipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Crawl forward clip is missing at {UnityCrawlForwardClipAssetPath}.");
            }

            ConfigureCrawlChainPhysicsRig(crawlSlot);
            ValidateCrawlForwardChainPhysicsResponse(crawlSlot, clip);
            Debug.Log("Prepared Accelerando crawl forward chain physics response validated.");
        }

        public static void ApplyPhysicsAntennaStrikeMotionToCurrentScene()
        {
            EnsureUnityFolders();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            ApplyPhysicsAntennaStrikeMotion(placementRoot.transform);
            InspectPhysicsAntennaStrikeMotion(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Accelerando physics antenna strike motion applied.");
        }

        public static void InspectPhysicsAntennaStrikeMotionInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectPhysicsAntennaStrikeMotion(placementRoot.transform);
            Debug.Log("Prepared Accelerando physics antenna strike motion inspected.");
        }

        public static void ValidatePhysicsAntennaStrikeResponseInScene()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var strikeSlot = placementRoot.transform.Find(AntennaStrikeSlotObjectName);
            if (strikeSlot == null)
            {
                throw new InvalidOperationException($"{AntennaStrikeSlotObjectName} is missing under {PlacementRootName}.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAntennaStrikeClipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Antenna strike clip is missing at {UnityAntennaStrikeClipAssetPath}.");
            }

            ConfigureAttackChainPhysicsRig(strikeSlot);
            ValidatePhysicsAntennaStrikeResponse(strikeSlot, clip);
            Debug.Log("Prepared Accelerando physics antenna strike response validated.");
        }

        public static void CaptureIdleBreathingAnimationReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            CaptureIdleBreathingReviewImages(placementRoot.transform);
        }

        public static void InspectPreparedModelStructure()
        {
            RequirePreparedModelFile();
            EnsureUnityFolders();
            CopyPreparedModelAsset();
            ConfigureImportedModelAsset();

            var modelAsset = LoadPreparedModelAsset();
            var instance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(modelAsset);
            }

            try
            {
                instance.name = "Accelerando_ModelStructure_Inspection";
                InspectMeshStructure(instance.transform);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void RequirePreparedModelFile()
        {
            if (!File.Exists(SourceModelAbsolutePath))
            {
                throw new FileNotFoundException("Prepared Accelerando GLB model is missing.", SourceModelAbsolutePath);
            }
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(AccelerandoArtRoot);
            EnsureUnityFolder(UnityModelFolder);
            EnsureUnityFolder(UnityMaterialFolder);
            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityControllerFolder);
        }

        private static void EnsureUnityFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var folderName = Path.GetFileName(folder);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException($"Invalid Unity folder path: {folder}");
            }

            EnsureUnityFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void CopyPreparedModelAsset()
        {
            var absoluteAssetPath = GetAbsoluteProjectPath(UnityModelAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteAssetPath) ?? throw new InvalidOperationException("Accelerando model asset directory is invalid."));
            File.Copy(SourceModelAbsolutePath, absoluteAssetPath, true);
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
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
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
                $"Could not load Accelerando GLB as a Unity model asset. GLB path={UnityModelAssetPath}. Ensure GLB import support is enabled.");
        }

        private static Material EnsureFallbackMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnityFallbackMaterialAssetPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = "M_Accelerando_Fallback_URP"
                };
                AssetDatabase.CreateAsset(material, UnityFallbackMaterialAssetPath);
            }

            SetMaterialColor(material, AccelerandoFallbackColor);
            SetMaterialFloat(material, "_Smoothness", 0.48f);
            SetMaterialFloat(material, "_Glossiness", 0.48f);
            SetMaterialFloat(material, "_Metallic", 0f);
            SetMaterialFloat(material, "_Cull", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static ApprovedMaterialSet EnsureApprovedMaterialSet()
        {
            EnsureUnityFolder(UnityMaterialFolder);
            return new ApprovedMaterialSet(
                EnsureApprovedMaterial(
                    UnityApprovedFleshMaterialAssetPath,
                    "M_Accelerando_Approved_WetTaupeFlesh_URP",
                    AccelerandoApprovedFleshColor,
                    0f,
                    0.72f),
                EnsureApprovedMaterial(
                    UnityApprovedShellMaterialAssetPath,
                    "M_Accelerando_Approved_DarkShell_URP",
                    AccelerandoApprovedShellColor,
                    0f,
                    0.32f),
                EnsureApprovedMaterial(
                    UnityApprovedMetalMaterialAssetPath,
                    "M_Accelerando_Approved_RustyMetal_URP",
                    AccelerandoApprovedMetalColor,
                    0.72f,
                    0.46f));
        }

        private static Material EnsureApprovedMaterial(string assetPath, string materialName, Color color, float metallic, float smoothness)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.name = materialName;
            SetMaterialColor(material, color);
            SetMaterialFloat(material, "_Metallic", metallic);
            SetMaterialFloat(material, "_Smoothness", smoothness);
            SetMaterialFloat(material, "_Glossiness", smoothness);
            SetMaterialFloat(material, "_Cull", 0f);
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

        private static void SetMaterialFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static GameObject PlacePreparedModel(GameObject modelAsset, ApprovedMaterialSet materialSet, Scene scene)
        {
            var conSpiritoRoot = RequireSceneRoot(ConSpiritoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var spacing = CalculateLongaTergoZSpacing(longaRoot.transform, tergoRoot.transform);
            var placementPosition = new Vector3(
                conSpiritoRoot.transform.position.x,
                conSpiritoRoot.transform.position.y,
                conSpiritoRoot.transform.position.z - spacing);

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

            var reviewRoot = CreatePreparedModelInstance(
                modelAsset,
                materialSet,
                placementRoot.transform,
                PlacementObjectName,
                new Vector3(CalculateStaticReviewAlignedX(), 0f, AnimationSlotRowLocalZ));

            CreateAnimationReviewSlots(modelAsset, materialSet, placementRoot.transform);
            ArrangeAlignedReviewRow(placementRoot.transform);
            ApplyIdleBreathingAnimation(placementRoot.transform);
            ApplyCrawlForwardMotion(placementRoot.transform);
            ApplyPhysicsAntennaStrikeMotion(placementRoot.transform);

            EditorUtility.SetDirty(placementRoot);
            EditorUtility.SetDirty(reviewRoot);
            return placementRoot;
        }

        private static GameObject CreatePreparedModelInstance(
            GameObject modelAsset,
            ApprovedMaterialSet materialSet,
            Transform placementRoot,
            string objectName,
            Vector3 localPosition)
        {
            var reviewRoot = new GameObject(objectName);
            reviewRoot.transform.SetParent(placementRoot, false);
            reviewRoot.transform.localPosition = localPosition;
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, AccelerandoFacingYawDegrees, 0f);
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
            RemoveLowerPlateGeometry(reviewRoot.transform);
            EnsureRenderableHierarchy(reviewRoot.transform);
            ReconnectMaceChains(reviewRoot.transform);
            AssignApprovedMaterials(reviewRoot.transform, materialSet);
            HideMaceSocketRingRenderers(reviewRoot.transform);
            AlignToGround(reviewRoot.transform, placementRoot.position.y);

            EditorUtility.SetDirty(reviewRoot);
            return reviewRoot;
        }

        private static void CreateAnimationReviewSlots(GameObject modelAsset, ApprovedMaterialSet materialSet, Transform placementRoot)
        {
            for (var i = 0; i < AnimationReviewSlots.Length; i++)
            {
                var slot = AnimationReviewSlots[i];
                var localPosition = new Vector3(CalculateAnimationSlotAlignedX(i), 0f, AnimationSlotRowLocalZ);
                CreatePreparedModelInstance(modelAsset, materialSet, placementRoot, slot.ObjectName, localPosition);
            }
        }

        private static void ArrangeAlignedReviewRow(Transform placementRoot)
        {
            var staticReview = placementRoot.Find(PlacementObjectName);
            if (staticReview == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            staticReview.localPosition = new Vector3(CalculateStaticReviewAlignedX(), staticReview.localPosition.y, AnimationSlotRowLocalZ);
            EditorUtility.SetDirty(staticReview);

            for (var i = 0; i < AnimationReviewSlots.Length; i++)
            {
                var slotObject = placementRoot.Find(AnimationReviewSlots[i].ObjectName);
                if (slotObject == null)
                {
                    throw new InvalidOperationException($"{AnimationReviewSlots[i].ObjectName} animation review slot is missing.");
                }

                slotObject.localPosition = new Vector3(CalculateAnimationSlotAlignedX(i), slotObject.localPosition.y, AnimationSlotRowLocalZ);
                EditorUtility.SetDirty(slotObject);
            }
        }

        private static float CalculateStaticReviewAlignedX()
        {
            return -(AnimationReviewSlots.Length / 2) * AnimationSlotSpacingX;
        }

        private static float CalculateAnimationSlotAlignedX(int slotIndex)
        {
            var offsetIndex = slotIndex - (AnimationReviewSlots.Length / 2) + 1;
            return offsetIndex * AnimationSlotSpacingX;
        }

        private static void ApplyIdleBreathingAnimation(Transform placementRoot)
        {
            var idleSlot = placementRoot.Find(IdleSlotObjectName);
            if (idleSlot == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} is missing under {PlacementRootName}.");
            }

            var modelObject = idleSlot.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {IdleSlotObjectName}.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(UnityFallbackMaterialAssetPath);
            if (material == null)
            {
                material = EnsureFallbackMaterial();
            }

            var meshTransform = FindIdleBreathMeshContainer(modelObject);
            var sourceMesh = FindIdleBreathSourceMesh(placementRoot, idleSlot);
            if (sourceMesh == null)
            {
                sourceMesh = FindPrimaryMesh(modelObject, out var fallbackMeshTransform);
                if (meshTransform == null)
                {
                    meshTransform = fallbackMeshTransform;
                }
            }

            if (sourceMesh == null || meshTransform == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} does not contain a mesh to morph.");
            }

            RemoveIdleBreathSplitChildren(meshTransform);
            var morphMesh = EnsureIdleBreathMorphMesh(sourceMesh);
            var bodyRenderer = ConvertToIdleBreathSkinnedRenderer(meshTransform, morphMesh, material);
            var clip = EnsureIdleBreathClip(
                GetRelativePath(idleSlot, bodyRenderer.transform),
                bodyRenderer.transform.localScale,
                bodyRenderer.transform.localPosition);
            var controller = EnsureIdleBreathController(clip);

            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = idleSlot.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            EditorUtility.SetDirty(bodyRenderer);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(idleSlot);
            Debug.Log(
                "AccelerandoIdleBreathApply " +
                $"Slot={IdleSlotObjectName}, Renderer={bodyRenderer.name}, Mesh={AssetDatabase.GetAssetPath(morphMesh)}, Clip={UnityIdleBreathClipAssetPath}, " +
                $"Controller={UnityIdleBreathControllerAssetPath}, BlendShape={IdleBreathBlendShapeName}.");
        }

        private static void ApplyCrawlForwardMotion(Transform placementRoot)
        {
            var crawlSlot = placementRoot.Find(CrawlSlotObjectName);
            if (crawlSlot == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} is missing under {PlacementRootName}.");
            }

            var modelObject = crawlSlot.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {CrawlSlotObjectName}.");
            }

            var clip = EnsureCrawlForwardClip(crawlSlot, modelObject);
            var controller = EnsureCrawlForwardController(clip);
            var animator = crawlSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = crawlSlot.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;

            var physicsSummary = ConfigureCrawlChainPhysicsRig(crawlSlot);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(crawlSlot);
            Debug.Log(
                "AccelerandoCrawlForwardApply " +
                $"Slot={CrawlSlotObjectName}, Clip={UnityCrawlForwardClipAssetPath}, Controller={UnityCrawlForwardControllerAssetPath}, " +
                $"LoopSeconds={CrawlForwardLoopSeconds:0.###}, RootMotion=False, ChainPhysics={physicsSummary}.");
        }

        private static void ApplyPhysicsAntennaStrikeMotion(Transform placementRoot)
        {
            var strikeSlot = placementRoot.Find(AntennaStrikeSlotObjectName);
            if (strikeSlot == null)
            {
                throw new InvalidOperationException($"{AntennaStrikeSlotObjectName} is missing under {PlacementRootName}.");
            }

            var modelObject = strikeSlot.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {AntennaStrikeSlotObjectName}.");
            }

            var clip = EnsurePhysicsAntennaStrikeClip(strikeSlot, modelObject);
            var controller = EnsurePhysicsAntennaStrikeController(clip);
            var animator = strikeSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = strikeSlot.gameObject.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;

            var physicsSummary = ConfigureAttackChainPhysicsRig(strikeSlot);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(strikeSlot);
            Debug.Log(
                "AccelerandoPhysicsAntennaStrikeApply " +
                $"Slot={AntennaStrikeSlotObjectName}, Clip={UnityAntennaStrikeClipAssetPath}, Controller={UnityAntennaStrikeControllerAssetPath}, " +
                $"LoopSeconds={AntennaStrikeLoopSeconds:0.###}, RootMotion=False, ChainPhysics={physicsSummary}.");
        }

        private static string ConfigureCrawlChainPhysicsRig(Transform crawlSlot)
        {
            var rigType = FindAccelerandoChainPhysicsRigType();
            var rig = crawlSlot.GetComponent(rigType) as MonoBehaviour;
            if (rig == null)
            {
                rig = crawlSlot.gameObject.AddComponent(rigType) as MonoBehaviour;
            }

            var configureMethod = rigType.GetMethod("Configure", new[] { typeof(int) });
            if (configureMethod == null)
            {
                throw new InvalidOperationException($"{rigType.FullName} is missing Configure(int).");
            }

            configureMethod.Invoke(rig, new object[] { ConnectedChainVisibleLinkCount });
            EditorUtility.SetDirty(rig);
            return InspectCrawlChainPhysicsRig(crawlSlot);
        }

        private static string ConfigureAttackChainPhysicsRig(Transform strikeSlot)
        {
            var rigType = FindAccelerandoChainPhysicsRigType();
            var rig = strikeSlot.GetComponent(rigType) as MonoBehaviour;
            if (rig == null)
            {
                rig = strikeSlot.gameObject.AddComponent(rigType) as MonoBehaviour;
            }

            var configureMethod = rigType.GetMethod("ConfigureAttackStrike", new[] { typeof(int) });
            if (configureMethod == null)
            {
                throw new InvalidOperationException($"{rigType.FullName} is missing ConfigureAttackStrike(int).");
            }

            configureMethod.Invoke(rig, new object[] { ConnectedChainVisibleLinkCount });
            EditorUtility.SetDirty(rig);
            return InspectCrawlChainPhysicsRig(strikeSlot, 0.065f);
        }

        private static Type FindAccelerandoChainPhysicsRigType()
        {
            const string typeName = "Bellerophon.Enemies.Accelerando.AccelerandoChainPhysicsRig";
            var type = Type.GetType(typeName + ", Assembly-CSharp");
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException($"{typeName} runtime script is not compiled.");
        }

        private static Mesh FindPrimaryMesh(Transform root, out Transform meshTransform)
        {
            var meshFilter = root.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshTransform = meshFilter.transform;
                return meshFilter.sharedMesh;
            }

            var skinnedRenderer = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedRenderer != null && skinnedRenderer.sharedMesh != null)
            {
                meshTransform = skinnedRenderer.transform;
                return skinnedRenderer.sharedMesh;
            }

            meshTransform = null;
            return null;
        }

        private static Transform FindRendererTransform(Transform root)
        {
            var renderer = root.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                return renderer.transform;
            }

            var meshFilter = root.GetComponentInChildren<MeshFilter>(true);
            return meshFilter != null ? meshFilter.transform : null;
        }

        private static Mesh FindIdleBreathSourceMesh(Transform placementRoot, Transform idleSlot)
        {
            var candidates = new List<Transform>();
            var staticReview = placementRoot.Find(PlacementObjectName);
            if (staticReview != null)
            {
                candidates.Add(staticReview);
            }

            for (var i = 0; i < AnimationReviewSlots.Length; i++)
            {
                var slot = placementRoot.Find(AnimationReviewSlots[i].ObjectName);
                if (slot != null && slot != idleSlot)
                {
                    candidates.Add(slot);
                }
            }

            foreach (var candidate in candidates)
            {
                var candidateModel = candidate.Find(ModelChildName) ?? candidate;
                var mesh = FindPrimaryMesh(candidateModel, out _);
                if (mesh == null)
                {
                    continue;
                }

                var meshPath = AssetDatabase.GetAssetPath(mesh);
                if (!string.Equals(meshPath, UnityIdleBreathMorphMeshAssetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return mesh;
                }
            }

            return null;
        }

        private static Transform FindIdleBreathMeshContainer(Transform modelObject)
        {
            var existingBody = FindChildByName(modelObject, IdleBreathBodyObjectName);
            if (existingBody != null && existingBody.parent != null)
            {
                return existingBody.parent;
            }

            var mesh = FindPrimaryMesh(modelObject, out var meshTransform);
            if (mesh != null && meshTransform != null)
            {
                return meshTransform;
            }

            return FindRendererTransform(modelObject);
        }

        private static void RemoveIdleBreathSplitChildren(Transform meshTransform)
        {
            DestroyNamedChild(meshTransform, IdleBreathBodyObjectName);
            DestroyNamedChild(meshTransform, IdleBreathAntennaObjectName);
        }

        private static void DestroyNamedChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static SkinnedMeshRenderer ConvertToIdleBreathSplitRenderers(Transform meshTransform, IdleBreathMeshSplit split, Material material)
        {
            var originalMaterials = FindExistingMaterials(meshTransform, material);
            RemoveRendererComponents(meshTransform.gameObject);

            var bodyTransform = EnsureNamedChild(meshTransform, IdleBreathBodyObjectName);
            var antennaTransform = EnsureNamedChild(meshTransform, IdleBreathAntennaObjectName);
            RemoveSkinnedRenderer(bodyTransform.gameObject);
            RemoveSkinnedRenderer(antennaTransform.gameObject);
            RemoveMeshFilterAndRenderer(bodyTransform.gameObject);

            var bodyRenderer = bodyTransform.GetComponent<SkinnedMeshRenderer>();
            if (bodyRenderer == null)
            {
                bodyRenderer = bodyTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
            }

            var rootBone = EnsureIdleBreathRootBone(bodyTransform);
            bodyRenderer.sharedMesh = split.BodyMesh;
            bodyRenderer.sharedMaterials = originalMaterials;
            bodyRenderer.rootBone = rootBone;
            bodyRenderer.bones = new[] { rootBone };
            bodyRenderer.localBounds = split.BodyMesh.bounds;
            bodyRenderer.quality = SkinQuality.Bone1;
            bodyRenderer.updateWhenOffscreen = true;
            bodyRenderer.enabled = true;
            bodyRenderer.forceRenderingOff = false;
            bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            bodyRenderer.receiveShadows = true;

            var antennaFilter = antennaTransform.GetComponent<MeshFilter>();
            if (antennaFilter == null)
            {
                antennaFilter = antennaTransform.gameObject.AddComponent<MeshFilter>();
            }

            var antennaRenderer = antennaTransform.GetComponent<MeshRenderer>();
            if (antennaRenderer == null)
            {
                antennaRenderer = antennaTransform.gameObject.AddComponent<MeshRenderer>();
            }

            antennaFilter.sharedMesh = split.AntennaMesh;
            antennaRenderer.sharedMaterials = originalMaterials;
            antennaRenderer.enabled = true;
            antennaRenderer.forceRenderingOff = false;
            antennaRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            antennaRenderer.receiveShadows = true;

            EditorUtility.SetDirty(bodyRenderer);
            EditorUtility.SetDirty(antennaFilter);
            EditorUtility.SetDirty(antennaRenderer);
            return bodyRenderer;
        }

        private static Material[] FindExistingMaterials(Transform meshTransform, Material fallbackMaterial)
        {
            foreach (var renderer in meshTransform.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
                {
                    return renderer.sharedMaterials;
                }
            }

            return new[] { fallbackMaterial };
        }

        private static Transform EnsureNamedChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                var childObject = new GameObject(childName);
                child = childObject.transform;
                child.SetParent(parent, false);
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            child.gameObject.SetActive(true);
            return child;
        }

        private static void RemoveRendererComponents(GameObject target)
        {
            RemoveMeshFilterAndRenderer(target);
            RemoveSkinnedRenderer(target);
        }

        private static void RemoveMeshFilterAndRenderer(GameObject target)
        {
            var meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                UnityEngine.Object.DestroyImmediate(meshRenderer);
            }

            var meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                UnityEngine.Object.DestroyImmediate(meshFilter);
            }
        }

        private static void RemoveSkinnedRenderer(GameObject target)
        {
            var skinnedRenderer = target.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer != null)
            {
                UnityEngine.Object.DestroyImmediate(skinnedRenderer);
            }
        }

        private static SkinnedMeshRenderer ConvertToIdleBreathSkinnedRenderer(Transform meshTransform, Mesh morphMesh, Material material)
        {
            var originalMaterials = FindExistingMaterials(meshTransform, material);
            var originalMeshRenderer = meshTransform.GetComponent<MeshRenderer>();

            var skinnedRenderer = meshTransform.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer == null)
            {
                skinnedRenderer = meshTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
            }

            var rootBone = EnsureIdleBreathRootBone(meshTransform);

            skinnedRenderer.sharedMesh = morphMesh;
            skinnedRenderer.sharedMaterials = originalMaterials;
            skinnedRenderer.rootBone = rootBone;
            skinnedRenderer.bones = new[] { rootBone };
            skinnedRenderer.localBounds = morphMesh.bounds;
            skinnedRenderer.quality = SkinQuality.Bone1;
            skinnedRenderer.updateWhenOffscreen = true;
            skinnedRenderer.enabled = true;
            skinnedRenderer.forceRenderingOff = false;
            skinnedRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            skinnedRenderer.receiveShadows = true;

            if (originalMeshRenderer != null)
            {
                UnityEngine.Object.DestroyImmediate(originalMeshRenderer);
            }

            var meshFilter = meshTransform.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                UnityEngine.Object.DestroyImmediate(meshFilter);
            }

            return skinnedRenderer;
        }

        private static Transform EnsureIdleBreathRootBone(Transform meshTransform)
        {
            var existing = meshTransform.Find(IdleBreathRootBoneName);
            if (existing != null)
            {
                existing.localPosition = Vector3.zero;
                existing.localRotation = Quaternion.identity;
                existing.localScale = Vector3.one;
                return existing;
            }

            var boneObject = new GameObject(IdleBreathRootBoneName);
            var bone = boneObject.transform;
            bone.SetParent(meshTransform, false);
            bone.localPosition = Vector3.zero;
            bone.localRotation = Quaternion.identity;
            bone.localScale = Vector3.one;
            return bone;
        }

        private static IdleBreathMeshSplit EnsureIdleBreathSplitMeshes(Mesh sourceMesh)
        {
            if (sourceMesh == null)
            {
                throw new ArgumentNullException(nameof(sourceMesh));
            }

            var sourceVertices = sourceMesh.vertices;
            if (sourceVertices.Length == 0)
            {
                throw new InvalidOperationException($"{sourceMesh.name} has no vertices.");
            }

            var submeshCount = Mathf.Max(sourceMesh.subMeshCount, 1);
            var sourceTriangles = new int[submeshCount][];
            var parent = new int[sourceVertices.Length];
            for (var i = 0; i < parent.Length; i++)
            {
                parent[i] = i;
            }

            for (var submesh = 0; submesh < submeshCount; submesh++)
            {
                sourceTriangles[submesh] = sourceMesh.GetTriangles(submesh);
                var triangles = sourceTriangles[submesh];
                for (var i = 0; i + 2 < triangles.Length; i += 3)
                {
                    UnionMeshComponent(parent, triangles[i], triangles[i + 1]);
                    UnionMeshComponent(parent, triangles[i], triangles[i + 2]);
                }
            }

            var components = BuildMeshComponentInfo(parent, sourceVertices, sourceTriangles);
            var mainComponentRoot = FindMainBodyComponentRoot(components);
            var antennaComponentRoots = new HashSet<int>();
            var useXAsLateralAxis = sourceMesh.bounds.size.x >= sourceMesh.bounds.size.z;
            foreach (var pair in components)
            {
                if (pair.Key != mainComponentRoot && IsIdleBreathAntennaComponent(pair.Value, sourceMesh.bounds, useXAsLateralAxis))
                {
                    antennaComponentRoots.Add(pair.Key);
                }
            }

            var bodySubmeshTriangles = CreateTriangleListArray(submeshCount);
            var antennaSubmeshTriangles = CreateTriangleListArray(submeshCount);
            var bodyTriangleCount = 0;
            var antennaTriangleCount = 0;

            for (var submesh = 0; submesh < submeshCount; submesh++)
            {
                var triangles = sourceTriangles[submesh];
                for (var i = 0; i + 2 < triangles.Length; i += 3)
                {
                    var a = triangles[i];
                    var b = triangles[i + 1];
                    var c = triangles[i + 2];
                    var root = FindMeshComponent(parent, a);
                    var centroid = (sourceVertices[a] + sourceVertices[b] + sourceVertices[c]) / 3f;
                    var isAntenna = antennaComponentRoots.Contains(root) ||
                        IsIdleBreathAntennaTriangle(centroid, sourceMesh.bounds, useXAsLateralAxis);

                    var targetTriangles = isAntenna ? antennaSubmeshTriangles[submesh] : bodySubmeshTriangles[submesh];
                    targetTriangles.Add(a);
                    targetTriangles.Add(b);
                    targetTriangles.Add(c);

                    if (isAntenna)
                    {
                        antennaTriangleCount++;
                    }
                    else
                    {
                        bodyTriangleCount++;
                    }
                }
            }

            if (bodyTriangleCount <= 0 || antennaTriangleCount <= 0)
            {
                throw new InvalidOperationException(
                    $"Accelerando idle breath split failed. BodyTriangles={bodyTriangleCount}, AntennaTriangles={antennaTriangleCount}.");
            }

            var bodyMesh = CreateIdleBreathSubsetMesh(sourceMesh, bodySubmeshTriangles, "Accelerando_Idle_Breath_Body_Mesh", true);
            var antennaMesh = CreateIdleBreathSubsetMesh(sourceMesh, antennaSubmeshTriangles, "Accelerando_Idle_Breath_Static_Mace_Antennae_Mesh", false);

            if (AssetDatabase.LoadAssetAtPath<Mesh>(UnityIdleBreathMorphMeshAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(UnityIdleBreathMorphMeshAssetPath);
            }

            AssetDatabase.CreateAsset(bodyMesh, UnityIdleBreathMorphMeshAssetPath);
            AssetDatabase.AddObjectToAsset(antennaMesh, UnityIdleBreathMorphMeshAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(UnityIdleBreathMorphMeshAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            Debug.Log(
                "AccelerandoIdleBreathMeshSplit " +
                $"BodyTriangles={bodyTriangleCount}, StaticAntennaTriangles={antennaTriangleCount}, " +
                $"AntennaComponents={antennaComponentRoots.Count}, LateralAxis={(useXAsLateralAxis ? "X" : "Z")}.");

            return new IdleBreathMeshSplit(bodyMesh, antennaMesh, bodyTriangleCount, antennaTriangleCount, antennaComponentRoots.Count);
        }

        private static Dictionary<int, MeshComponentInfo> BuildMeshComponentInfo(int[] parent, Vector3[] vertices, int[][] sourceTriangles)
        {
            var components = new Dictionary<int, MeshComponentInfo>();
            for (var submesh = 0; submesh < sourceTriangles.Length; submesh++)
            {
                var triangles = sourceTriangles[submesh];
                for (var i = 0; i + 2 < triangles.Length; i += 3)
                {
                    var a = triangles[i];
                    var b = triangles[i + 1];
                    var c = triangles[i + 2];
                    var root = FindMeshComponent(parent, a);
                    if (!components.TryGetValue(root, out var info))
                    {
                        info = new MeshComponentInfo();
                        components.Add(root, info);
                    }

                    info.TriangleCount++;
                    info.Include(vertices[a]);
                    info.Include(vertices[b]);
                    info.Include(vertices[c]);
                }
            }

            return components;
        }

        private static int FindMainBodyComponentRoot(Dictionary<int, MeshComponentInfo> components)
        {
            var bestRoot = -1;
            var bestTriangleCount = -1;
            foreach (var pair in components)
            {
                if (pair.Value.TriangleCount > bestTriangleCount)
                {
                    bestTriangleCount = pair.Value.TriangleCount;
                    bestRoot = pair.Key;
                }
            }

            return bestRoot;
        }

        private static List<int>[] CreateTriangleListArray(int submeshCount)
        {
            var triangleLists = new List<int>[submeshCount];
            for (var i = 0; i < triangleLists.Length; i++)
            {
                triangleLists[i] = new List<int>();
            }

            return triangleLists;
        }

        private static bool IsIdleBreathAntennaComponent(MeshComponentInfo component, Bounds sourceBounds, bool useXAsLateralAxis)
        {
            if (!component.HasBounds)
            {
                return false;
            }

            var center = component.Bounds.center;
            var lateralOffset = CalculateIdleBreathLateralOffset(center, sourceBounds, useXAsLateralAxis);
            var verticalPosition = Mathf.InverseLerp(sourceBounds.min.y, sourceBounds.max.y, center.y);
            var lateralSize = CalculateIdleBreathLateralSize(component.Bounds, useXAsLateralAxis);
            var sourceLateralSize = Mathf.Max(CalculateIdleBreathLateralSize(sourceBounds, useXAsLateralAxis), 0.001f);
            var isCompactSidePart = lateralSize / sourceLateralSize < 0.55f;
            return lateralOffset >= IdleBreathAntennaHorizontalThreshold ||
                IsIdleBreathStaticShell(verticalPosition) ||
                IsIdleBreathAntennaRoot(lateralOffset, verticalPosition) ||
                (lateralOffset >= IdleBreathAntennaRaisedHorizontalThreshold &&
                 verticalPosition >= IdleBreathAntennaRaisedVerticalThreshold &&
                 isCompactSidePart);
        }

        private static bool IsIdleBreathAntennaTriangle(Vector3 centroid, Bounds sourceBounds, bool useXAsLateralAxis)
        {
            var lateralOffset = CalculateIdleBreathLateralOffset(centroid, sourceBounds, useXAsLateralAxis);
            var verticalPosition = Mathf.InverseLerp(sourceBounds.min.y, sourceBounds.max.y, centroid.y);
            return lateralOffset >= IdleBreathAntennaHorizontalThreshold ||
                IsIdleBreathStaticShell(verticalPosition) ||
                IsIdleBreathAntennaRoot(lateralOffset, verticalPosition) ||
                (lateralOffset >= IdleBreathAntennaRaisedHorizontalThreshold &&
                 verticalPosition >= IdleBreathAntennaRaisedVerticalThreshold);
        }

        private static bool IsIdleBreathStaticShell(float verticalPosition)
        {
            return verticalPosition >= IdleBreathStaticShellVerticalThreshold;
        }

        private static bool IsIdleBreathAntennaRoot(float lateralOffset, float verticalPosition)
        {
            return lateralOffset >= IdleBreathAntennaRootHorizontalThreshold &&
                verticalPosition >= IdleBreathAntennaRootVerticalThreshold;
        }

        private static float CalculateIdleBreathLateralOffset(Vector3 point, Bounds sourceBounds, bool useXAsLateralAxis)
        {
            var center = useXAsLateralAxis ? sourceBounds.center.x : sourceBounds.center.z;
            var extent = Mathf.Max(useXAsLateralAxis ? sourceBounds.extents.x : sourceBounds.extents.z, 0.001f);
            var value = useXAsLateralAxis ? point.x : point.z;
            return Mathf.Abs(value - center) / extent;
        }

        private static float CalculateIdleBreathLateralSize(Bounds bounds, bool useXAsLateralAxis)
        {
            return useXAsLateralAxis ? bounds.size.x : bounds.size.z;
        }

        private static Mesh CreateIdleBreathSubsetMesh(Mesh sourceMesh, List<int>[] selectedSubmeshTriangles, string meshName, bool includeBlendShape)
        {
            var oldToNew = new Dictionary<int, int>();
            var newToOld = new List<int>();
            for (var submesh = 0; submesh < selectedSubmeshTriangles.Length; submesh++)
            {
                var triangles = selectedSubmeshTriangles[submesh];
                for (var i = 0; i < triangles.Count; i++)
                {
                    var oldIndex = triangles[i];
                    if (oldToNew.ContainsKey(oldIndex))
                    {
                        continue;
                    }

                    oldToNew.Add(oldIndex, newToOld.Count);
                    newToOld.Add(oldIndex);
                }
            }

            var sourceVertices = sourceMesh.vertices;
            var mesh = new Mesh
            {
                name = meshName,
                indexFormat = newToOld.Count > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            var vertices = new Vector3[newToOld.Count];
            for (var i = 0; i < newToOld.Count; i++)
            {
                vertices[i] = sourceVertices[newToOld[i]];
            }

            mesh.vertices = vertices;
            CopyVector3Channel(sourceMesh.normals, newToOld, values => mesh.normals = values);
            CopyVector4Channel(sourceMesh.tangents, newToOld, values => mesh.tangents = values);
            CopyColor32Channel(sourceMesh.colors32, newToOld, values => mesh.colors32 = values);
            CopyVector2Channel(sourceMesh.uv, newToOld, values => mesh.uv = values);
            CopyVector2Channel(sourceMesh.uv2, newToOld, values => mesh.uv2 = values);
            CopyVector2Channel(sourceMesh.uv3, newToOld, values => mesh.uv3 = values);
            CopyVector2Channel(sourceMesh.uv4, newToOld, values => mesh.uv4 = values);

            mesh.subMeshCount = selectedSubmeshTriangles.Length;
            for (var submesh = 0; submesh < selectedSubmeshTriangles.Length; submesh++)
            {
                var selected = selectedSubmeshTriangles[submesh];
                var remapped = new int[selected.Count];
                for (var i = 0; i < selected.Count; i++)
                {
                    remapped[i] = oldToNew[selected[i]];
                }

                mesh.SetTriangles(remapped, submesh, true);
            }

            if (includeBlendShape)
            {
                AddIdleBreathBlendShape(mesh, sourceMesh.bounds);
            }

            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
            return mesh;
        }

        private static void AddIdleBreathBlendShape(Mesh mesh, Bounds sourceBounds)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            var boneWeights = new BoneWeight[vertices.Length];
            var deltaVertices = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            var center = sourceBounds.center;
            var minY = sourceBounds.min.y;
            var height = Mathf.Max(sourceBounds.size.y, 0.001f);

            for (var i = 0; i < vertices.Length; i++)
            {
                boneWeights[i] = new BoneWeight
                {
                    boneIndex0 = 0,
                    weight0 = 1f
                };

                var offset = vertices[i] - center;
                var verticalWeight = Mathf.InverseLerp(minY, sourceBounds.max.y, vertices[i].y);
                var floorLock = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(verticalWeight * 1.45f));
                var breathWeight = floorLock * CalculateIdleBreathBodyMorphWeight(verticalWeight);
                deltaVertices[i] = new Vector3(
                    offset.x * IdleBreathRadialScale * breathWeight,
                    height * IdleBreathVerticalScale * breathWeight,
                    offset.z * IdleBreathRadialScale * breathWeight);

                if (normals.Length == vertices.Length)
                {
                    deltaNormals[i] = normals[i] * (0.002f * breathWeight);
                }

                if (tangents.Length == vertices.Length)
                {
                    deltaTangents[i] = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z) * (0.001f * breathWeight);
                }
            }

            mesh.bindposes = new[] { Matrix4x4.identity };
            mesh.boneWeights = boneWeights;
            mesh.AddBlendShapeFrame(IdleBreathBlendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
        }

        private static float CalculateIdleBreathBodyMorphWeight(float verticalWeight)
        {
            var upperBodyWeight = 1f - Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(IdleBreathBodyFadeStartVertical, IdleBreathStaticShellVerticalThreshold, verticalWeight));
            return Mathf.Clamp01(Mathf.Lerp(0.25f, 1f, upperBodyWeight));
        }

        private static bool IsIdleBreathStaticMorphVertex(Vector3 vertex, Bounds sourceBounds, bool useXAsLateralAxis)
        {
            var lateralOffset = CalculateIdleBreathLateralOffset(vertex, sourceBounds, useXAsLateralAxis);
            var verticalPosition = Mathf.InverseLerp(sourceBounds.min.y, sourceBounds.max.y, vertex.y);
            return lateralOffset >= IdleBreathAntennaHorizontalThreshold ||
                IsIdleBreathAntennaRoot(lateralOffset, verticalPosition) ||
                (lateralOffset >= IdleBreathAntennaRaisedHorizontalThreshold &&
                 verticalPosition >= IdleBreathAntennaRaisedVerticalThreshold);
        }

        private static void CopyVector2Channel(Vector2[] sourceValues, List<int> newToOld, Action<Vector2[]> assign)
        {
            if (sourceValues == null || sourceValues.Length == 0)
            {
                return;
            }

            var values = new Vector2[newToOld.Count];
            for (var i = 0; i < newToOld.Count; i++)
            {
                values[i] = sourceValues[newToOld[i]];
            }

            assign(values);
        }

        private static void CopyVector3Channel(Vector3[] sourceValues, List<int> newToOld, Action<Vector3[]> assign)
        {
            if (sourceValues == null || sourceValues.Length == 0)
            {
                return;
            }

            var values = new Vector3[newToOld.Count];
            for (var i = 0; i < newToOld.Count; i++)
            {
                values[i] = sourceValues[newToOld[i]];
            }

            assign(values);
        }

        private static void CopyVector4Channel(Vector4[] sourceValues, List<int> newToOld, Action<Vector4[]> assign)
        {
            if (sourceValues == null || sourceValues.Length == 0)
            {
                return;
            }

            var values = new Vector4[newToOld.Count];
            for (var i = 0; i < newToOld.Count; i++)
            {
                values[i] = sourceValues[newToOld[i]];
            }

            assign(values);
        }

        private static void CopyColor32Channel(Color32[] sourceValues, List<int> newToOld, Action<Color32[]> assign)
        {
            if (sourceValues == null || sourceValues.Length == 0)
            {
                return;
            }

            var values = new Color32[newToOld.Count];
            for (var i = 0; i < newToOld.Count; i++)
            {
                values[i] = sourceValues[newToOld[i]];
            }

            assign(values);
        }

        private static int FindMeshComponent(int[] parent, int index)
        {
            if (parent[index] == index)
            {
                return index;
            }

            parent[index] = FindMeshComponent(parent, parent[index]);
            return parent[index];
        }

        private static void UnionMeshComponent(int[] parent, int a, int b)
        {
            var rootA = FindMeshComponent(parent, a);
            var rootB = FindMeshComponent(parent, b);
            if (rootA != rootB)
            {
                parent[rootB] = rootA;
            }
        }

        private static Mesh EnsureIdleBreathMorphMesh(Mesh sourceMesh)
        {
            if (sourceMesh == null)
            {
                throw new ArgumentNullException(nameof(sourceMesh));
            }

            var morphMesh = UnityEngine.Object.Instantiate(sourceMesh);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(UnityIdleBreathMorphMeshAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(UnityIdleBreathMorphMeshAssetPath);
            }

            morphMesh.name = "Accelerando_Idle_Breath_Morph_Mesh";
            ClearExistingBlendShapes(morphMesh);

            var vertices = morphMesh.vertices;
            var normals = morphMesh.normals;
            var tangents = morphMesh.tangents;
            var boneWeights = new BoneWeight[vertices.Length];
            var deltaVertices = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            var bounds = morphMesh.bounds;
            var center = bounds.center;
            var minY = bounds.min.y;
            var height = Mathf.Max(bounds.size.y, 0.001f);
            var useXAsLateralAxis = bounds.size.x >= bounds.size.z;
            var animatedVertexCount = 0;
            var staticVertexCount = 0;
            var maxDelta = 0f;

            for (var i = 0; i < vertices.Length; i++)
            {
                boneWeights[i] = new BoneWeight
                {
                    boneIndex0 = 0,
                    weight0 = 1f
                };

                var offset = vertices[i] - center;
                var verticalWeight = Mathf.InverseLerp(minY, bounds.max.y, vertices[i].y);
                var floorLock = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(verticalWeight * 1.45f));
                var breathWeight = IsIdleBreathStaticMorphVertex(vertices[i], bounds, useXAsLateralAxis)
                    ? 0f
                    : floorLock * CalculateIdleBreathBodyMorphWeight(verticalWeight);
                deltaVertices[i] = new Vector3(
                    offset.x * IdleBreathRadialScale * breathWeight,
                    height * IdleBreathVerticalScale * breathWeight,
                    offset.z * IdleBreathRadialScale * breathWeight);
                if (breathWeight > 0.0001f)
                {
                    animatedVertexCount++;
                }
                else
                {
                    staticVertexCount++;
                }

                maxDelta = Mathf.Max(maxDelta, deltaVertices[i].magnitude);

                if (normals.Length == vertices.Length)
                {
                    deltaNormals[i] = normals[i] * (0.002f * breathWeight);
                }

                if (tangents.Length == vertices.Length)
                {
                    deltaTangents[i] = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z) * (0.001f * breathWeight);
                }
            }

            morphMesh.bindposes = new[] { Matrix4x4.identity };
            morphMesh.boneWeights = boneWeights;
            morphMesh.AddBlendShapeFrame(IdleBreathBlendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
            morphMesh.RecalculateBounds();
            morphMesh.UploadMeshData(false);
            AssetDatabase.CreateAsset(morphMesh, UnityIdleBreathMorphMeshAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(UnityIdleBreathMorphMeshAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Debug.Log(
                "AccelerandoIdleBreathMorphMesh " +
                $"AnimatedVertices={animatedVertexCount}, StaticVertices={staticVertexCount}, MaxDelta={maxDelta:0.###}, " +
                $"RadialScale={IdleBreathRadialScale:0.###}, VerticalScale={IdleBreathVerticalScale:0.###}.");
            return AssetDatabase.LoadAssetAtPath<Mesh>(UnityIdleBreathMorphMeshAssetPath) ?? morphMesh;
        }

        private static void ClearExistingBlendShapes(Mesh mesh)
        {
            if (mesh.blendShapeCount == 0)
            {
                return;
            }

            var copy = UnityEngine.Object.Instantiate(mesh);
            mesh.Clear();
            mesh.indexFormat = copy.indexFormat;
            mesh.vertices = copy.vertices;
            mesh.normals = copy.normals;
            mesh.tangents = copy.tangents;
            mesh.colors = copy.colors;
            mesh.colors32 = copy.colors32;
            mesh.uv = copy.uv;
            mesh.uv2 = copy.uv2;
            mesh.uv3 = copy.uv3;
            mesh.uv4 = copy.uv4;
            mesh.uv5 = copy.uv5;
            mesh.uv6 = copy.uv6;
            mesh.uv7 = copy.uv7;
            mesh.uv8 = copy.uv8;
            mesh.boneWeights = copy.boneWeights;
            mesh.bindposes = copy.bindposes;
            mesh.subMeshCount = copy.subMeshCount;
            for (var submesh = 0; submesh < copy.subMeshCount; submesh++)
            {
                mesh.SetTriangles(copy.GetTriangles(submesh), submesh, true);
            }

            mesh.RecalculateBounds();
            UnityEngine.Object.DestroyImmediate(copy);
        }

        private static AnimationClip EnsureIdleBreathClip(string rendererPath, Vector3 baseScale, Vector3 baseLocalPosition)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityIdleBreathClipAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(UnityIdleBreathClipAssetPath);
            }

            var clip = new AnimationClip
            {
                name = "Accelerando_Idle_Breath_Morph",
                frameRate = 30f
            };
            SetBlendShapeCurve(
                clip,
                rendererPath,
                CreateBreathValueCurve(0f, 100f, 0f));
            ConfigureLoopSetting(clip, true);
            AssetDatabase.CreateAsset(clip, UnityIdleBreathClipAssetPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityIdleBreathClipAssetPath) ?? clip;
        }

        private static AnimationClip EnsureCrawlForwardClip(Transform crawlSlot, Transform modelObject)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityCrawlForwardClipAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(UnityCrawlForwardClipAssetPath);
            }

            var clip = new AnimationClip
            {
                name = "Accelerando_Crawl_Forward_Loop",
                frameRate = 30f
            };

            var modelPath = GetRelativePath(crawlSlot, modelObject);
            var basePosition = modelObject.localPosition;
            var baseScale = modelObject.localScale;
            var baseEuler = modelObject.localEulerAngles;

            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalPosition.y",
                CreateCrawlLoopCurve(basePosition.y, basePosition.y + 0.014f, basePosition.y + 0.004f, basePosition.y + 0.010f));
            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalPosition.z",
                CreateCrawlBodyForeAftCurve(basePosition.z));
            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalScale.x",
                CreateCrawlLoopCurve(baseScale.x, baseScale.x * 0.972f, baseScale.x * 1.026f, baseScale.x * 0.982f));
            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalScale.y",
                CreateCrawlLoopCurve(baseScale.y, baseScale.y * 1.018f, baseScale.y * 0.988f, baseScale.y * 1.012f));
            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalScale.z",
                CreateCrawlLoopCurve(baseScale.z, baseScale.z * 1.046f, baseScale.z * 0.976f, baseScale.z * 1.034f));
            SetTransformCurve(
                clip,
                modelPath,
                "localEulerAnglesRaw.x",
                CreateCrawlLoopCurve(NormalizeEulerAngle(baseEuler.x), NormalizeEulerAngle(baseEuler.x) - 2.2f, NormalizeEulerAngle(baseEuler.x) + 1.2f, NormalizeEulerAngle(baseEuler.x) - 1.4f));

            ConfigureLoopSetting(clip, true);
            AssetDatabase.CreateAsset(clip, UnityCrawlForwardClipAssetPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(UnityCrawlForwardClipAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityCrawlForwardClipAssetPath) ?? clip;
        }

        private static AnimationClip EnsurePhysicsAntennaStrikeClip(Transform strikeSlot, Transform modelObject)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAntennaStrikeClipAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(UnityAntennaStrikeClipAssetPath);
            }

            var clip = new AnimationClip
            {
                name = "Accelerando_Antenna_Strike_Attack",
                frameRate = 30f
            };

            var modelPath = GetRelativePath(strikeSlot, modelObject);
            var basePosition = modelObject.localPosition;
            var baseScale = modelObject.localScale;
            var baseEuler = modelObject.localEulerAngles;

            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalPosition.y",
                CreateCrawlLoopCurve(basePosition.y, basePosition.y + 0.014f, basePosition.y + 0.004f, basePosition.y + 0.010f));
            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalPosition.z",
                CreateCrawlBodyForeAftCurve(basePosition.z));
            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalScale.x",
                CreateCrawlLoopCurve(baseScale.x, baseScale.x * 0.972f, baseScale.x * 1.026f, baseScale.x * 0.982f));
            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalScale.y",
                CreateCrawlLoopCurve(baseScale.y, baseScale.y * 1.018f, baseScale.y * 0.988f, baseScale.y * 1.012f));
            SetTransformCurve(
                clip,
                modelPath,
                "m_LocalScale.z",
                CreateCrawlLoopCurve(baseScale.z, baseScale.z * 1.046f, baseScale.z * 0.976f, baseScale.z * 1.034f));
            SetTransformCurve(
                clip,
                modelPath,
                "localEulerAnglesRaw.x",
                CreateCrawlLoopCurve(NormalizeEulerAngle(baseEuler.x), NormalizeEulerAngle(baseEuler.x) - 2.2f, NormalizeEulerAngle(baseEuler.x) + 1.2f, NormalizeEulerAngle(baseEuler.x) - 1.4f));

            SetAntennaStrikeInputCurves(clip, strikeSlot, "Left");
            SetAntennaStrikeInputCurves(clip, strikeSlot, "Right");

            ConfigureLoopSetting(clip, true);
            AssetDatabase.CreateAsset(clip, UnityAntennaStrikeClipAssetPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(UnityAntennaStrikeClipAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAntennaStrikeClipAssetPath) ?? clip;
        }

        private static void SetAntennaStrikeInputCurves(AnimationClip clip, Transform strikeSlot, string sideName)
        {
            var sideSign = string.Equals(sideName, "Left", StringComparison.Ordinal) ? -1f : 1f;
            var drivenTransforms = FindAntennaStrikeDrivenTransforms(strikeSlot, sideName);
            for (var i = 0; i < drivenTransforms.Count; i++)
            {
                var target = drivenTransforms[i];
                var path = GetRelativePath(strikeSlot, target);
                var basePosition = target.localPosition;
                var baseEuler = target.localEulerAngles;
                var influence = target.name.Contains("AntennaTip_Ring", StringComparison.Ordinal) ? 1f : 0.92f;

                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.z",
                    CreateAntennaStrikeForeAftCurve(basePosition.z, 0.118f * influence));
                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.y",
                    CreateAntennaStrikeLiftCurve(basePosition.y, 0.020f * influence));
                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.x",
                    CreateAntennaStrikeSideCurve(basePosition.x, sideSign * 0.010f * influence));
                SetTransformCurve(
                    clip,
                    path,
                    "localEulerAnglesRaw.x",
                    CreateAntennaStrikePitchCurve(NormalizeEulerAngle(baseEuler.x), 18.0f * influence));
                SetTransformCurve(
                    clip,
                    path,
                    "localEulerAnglesRaw.z",
                    CreateAntennaStrikePitchCurve(NormalizeEulerAngle(baseEuler.z), sideSign * 10.0f * influence));
            }
        }

        private static List<Transform> FindAntennaStrikeDrivenTransforms(Transform strikeSlot, string sideName)
        {
            var transforms = new List<Transform>
            {
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_Ring"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedVisibleChainEndHoop"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedSocketLip"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedSurfaceSaddle"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedRetainingPin"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedStem"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedFrontYoke"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedBackYoke"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedUpperOcclusionBulge"),
                RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_EmbeddedFleshPressurePad")
            };

            return transforms;
        }

        private static void SetCrawlChainFollowerCurves(AnimationClip clip, Transform crawlSlot, string sideName)
        {
            var sideSign = string.Equals(sideName, "Left", StringComparison.Ordinal) ? -1f : 1f;
            for (var i = 2; i <= ConnectedChainVisibleLinkCount; i++)
            {
                var link = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}");
                var path = GetRelativePath(crawlSlot, link);
                var basePosition = link.localPosition;
                var baseEuler = link.localEulerAngles;
                var t = (i - 1f) / (ConnectedChainVisibleLinkCount - 1f);
                var inertia = Mathf.SmoothStep(0f, 1f, t);
                var phaseDelay = Mathf.Lerp(0.035f, 0.160f, inertia);
                var followScale = Mathf.Lerp(0.24f, 1.18f, inertia);
                var lateralSway = Mathf.Lerp(0.004f, 0.026f, inertia);
                var lift = Mathf.Lerp(0.002f, 0.014f, inertia);
                var pitchLag = Mathf.Lerp(2.0f, 12.0f, inertia);
                var twistLag = Mathf.Lerp(2.0f, 9.0f, inertia);
                var lagSign = i % 2 == 0 ? 1f : -1f;

                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.x",
                    CreateCrawlBodyDrivenVelocityCurve(basePosition.x, sideSign * lagSign * lateralSway, phaseDelay + 0.018f));
                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.y",
                    CreateCrawlBodyDrivenLiftCurve(basePosition.y, lift, phaseDelay));
                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.z",
                    CreateCrawlBodyDrivenForeAftCurve(basePosition.z, followScale, phaseDelay));
                SetTransformCurve(
                    clip,
                    path,
                    "localEulerAnglesRaw.z",
                    CreateCrawlBodyDrivenVelocityCurve(NormalizeEulerAngle(baseEuler.z), sideSign * lagSign * twistLag, phaseDelay));
                SetTransformCurve(
                    clip,
                    path,
                    "localEulerAnglesRaw.x",
                    CreateCrawlBodyDrivenPitchCurve(NormalizeEulerAngle(baseEuler.x), lagSign * pitchLag, phaseDelay));
            }
        }

        private static void SetCrawlMacePhysicsCurves(AnimationClip clip, Transform crawlSlot, string sideName)
        {
            var sideSign = string.Equals(sideName, "Left", StringComparison.Ordinal) ? -1f : 1f;
            var maceSocket = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_MaceSocket_Ring");
            var maceTransforms = FindCrawlMaceTransforms(crawlSlot, sideName);
            Debug.Log(
                "AccelerandoCrawlForwardMaceTargets " +
                $"Side={sideName}, Count={maceTransforms.Count}, Targets={FormatTransformNames(maceTransforms)}, " +
                $"Details={FormatCrawlMaceTargetDetails(maceSocket, maceTransforms)}.");
            for (var i = 0; i < maceTransforms.Count; i++)
            {
                var maceTransform = maceTransforms[i];
                var path = GetRelativePath(crawlSlot, maceTransform);
                var basePosition = maceTransform.localPosition;
                var baseEuler = maceTransform.localEulerAngles;
                var isSocket = maceTransform.name.Contains("MaceSocket", StringComparison.Ordinal);
                var massFactor = isSocket ? 0.72f : 1.00f;
                var localIndexSign = i % 2 == 0 ? 1f : -1f;
                var phaseDelay = 0.188f + i * 0.012f;
                var followScale = 1.38f * massFactor;
                var lateralSway = 0.038f * massFactor;
                var liftDrop = 0.024f * massFactor;
                var spinLag = 22.0f * massFactor;
                var rollLag = 20.0f * massFactor;

                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.x",
                    CreateCrawlBodyDrivenVelocityCurve(basePosition.x, sideSign * localIndexSign * lateralSway, phaseDelay + 0.020f));
                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.y",
                    CreateCrawlBodyDrivenLiftCurve(basePosition.y, liftDrop, phaseDelay));
                SetTransformCurve(
                    clip,
                    path,
                    "m_LocalPosition.z",
                    CreateCrawlBodyDrivenForeAftCurve(basePosition.z, followScale, phaseDelay));
                SetTransformCurve(
                    clip,
                    path,
                    "localEulerAnglesRaw.x",
                    CreateCrawlBodyDrivenPitchCurve(NormalizeEulerAngle(baseEuler.x), -rollLag, phaseDelay));
                SetTransformCurve(
                    clip,
                    path,
                    "localEulerAnglesRaw.y",
                    CreateCrawlBodyDrivenVelocityCurve(NormalizeEulerAngle(baseEuler.y), sideSign * localIndexSign * rollLag, phaseDelay));
                SetTransformCurve(
                    clip,
                    path,
                    "localEulerAnglesRaw.z",
                    CreateCrawlBodyDrivenVelocityCurve(NormalizeEulerAngle(baseEuler.z), sideSign * spinLag, phaseDelay + 0.032f));
            }
        }

        private static List<Transform> FindCrawlMaceTransforms(Transform crawlSlot, string sideName)
        {
            var transforms = new List<Transform>();
            var prefix = $"Accelerando_{sideName}_";
            var maceSocket = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_MaceSocket_Ring");
            AddUniqueTransform(transforms, maceSocket);
            foreach (var child in crawlSlot.GetComponentsInChildren<Transform>(true))
            {
                var nameMatchesSide = child.name.StartsWith(prefix, StringComparison.Ordinal) &&
                    IsCrawlMaceTransformName(child.name);
                if (!nameMatchesSide &&
                    !IsCrawlMaceSpatialTransform(child, maceSocket))
                {
                    continue;
                }

                AddUniqueTransform(transforms, child);
            }

            return transforms;
        }

        private static string FormatTransformNames(List<Transform> transforms)
        {
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < transforms.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('|');
                }

                builder.Append(transforms[i] != null ? transforms[i].name : "<null>");
            }

            return builder.ToString();
        }

        private static string FormatCrawlMaceTargetDetails(Transform maceSocket, List<Transform> transforms)
        {
            var builder = new System.Text.StringBuilder();
            for (var i = 0; i < transforms.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('|');
                }

                var transform = transforms[i];
                var rendererCount = transform.GetComponentsInChildren<Renderer>(true).Length;
                var hasMeshFilter = transform.GetComponent<MeshFilter>() != null;
                builder.Append(transform.name);
                builder.Append("{Renderers=");
                builder.Append(rendererCount);
                builder.Append(",MeshFilter=");
                builder.Append(hasMeshFilter ? "true" : "false");
                builder.Append(",SocketDistance=");
                builder.Append(Vector3.Distance(transform.position, maceSocket.position).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                builder.Append('}');
            }

            return builder.ToString();
        }

        private static void AddUniqueTransform(List<Transform> transforms, Transform candidate)
        {
            if (candidate == null || transforms.Contains(candidate))
            {
                return;
            }

            transforms.Add(candidate);
        }

        private static bool IsCrawlMaceTransformName(string transformName)
        {
            return !transformName.Contains("ConnectedChain", StringComparison.Ordinal) &&
                !transformName.Contains("AntennaTip", StringComparison.Ordinal) &&
                (transformName.Contains("Mace", StringComparison.Ordinal) ||
                 transformName.Contains("Spiked", StringComparison.Ordinal) ||
                 transformName.Contains("Spike", StringComparison.Ordinal));
        }

        private static bool IsCrawlMaceSpatialTransform(Transform transform, Transform maceSocket)
        {
            if (transform == maceSocket ||
                transform.name.Contains("ConnectedChain", StringComparison.Ordinal) ||
                transform.name.Contains("Antenna", StringComparison.Ordinal) ||
                transform.name.Contains("Body", StringComparison.Ordinal) ||
                transform.name.Contains("Eye", StringComparison.Ordinal) ||
                transform.name.Contains("Mouth", StringComparison.Ordinal) ||
                transform.name.Contains("Embedded", StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryGetCrawlVisibleCenter(transform, out var visibleCenter))
            {
                return false;
            }

            var offset = visibleCenter - maceSocket.position;
            return offset.magnitude <= 1.45f &&
                visibleCenter.y <= maceSocket.position.y + 0.75f;
        }

        private static bool TryGetCrawlVisibleCenter(Transform transform, out Vector3 visibleCenter)
        {
            var renderer = transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                visibleCenter = renderer.bounds.center;
                return true;
            }

            var meshFilter = transform.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                visibleCenter = transform.TransformPoint(meshFilter.sharedMesh.bounds.center);
                return true;
            }

            visibleCenter = transform.position;
            return false;
        }

        private static AnimationCurve CreateCrawlBodyForeAftCurve(float baseValue)
        {
            return CreateCrawlLoopCurve(baseValue, baseValue + 0.026f, baseValue - 0.017f, baseValue + 0.018f);
        }

        private static AnimationCurve CreateAntennaStrikeForeAftCurve(float baseValue, float amplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, baseValue),
                new Keyframe(AntennaStrikeLoopSeconds * 0.12f, baseValue + amplitude * 0.24f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.26f, baseValue + amplitude * 0.64f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.42f, baseValue - amplitude),
                new Keyframe(AntennaStrikeLoopSeconds * 0.56f, baseValue - amplitude * 0.32f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.72f, baseValue + amplitude * 0.28f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.90f, baseValue - amplitude * 0.10f),
                new Keyframe(AntennaStrikeLoopSeconds, baseValue));
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateAntennaStrikeLiftCurve(float baseValue, float amplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, baseValue),
                new Keyframe(AntennaStrikeLoopSeconds * 0.26f, baseValue + amplitude * 0.24f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.42f, baseValue + amplitude),
                new Keyframe(AntennaStrikeLoopSeconds * 0.62f, baseValue - amplitude * 0.32f),
                new Keyframe(AntennaStrikeLoopSeconds, baseValue));
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateAntennaStrikeSideCurve(float baseValue, float signedAmplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, baseValue),
                new Keyframe(AntennaStrikeLoopSeconds * 0.26f, baseValue - signedAmplitude * 0.55f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.42f, baseValue + signedAmplitude),
                new Keyframe(AntennaStrikeLoopSeconds * 0.70f, baseValue - signedAmplitude * 0.34f),
                new Keyframe(AntennaStrikeLoopSeconds, baseValue));
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateAntennaStrikePitchCurve(float baseValue, float signedAmplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, baseValue),
                new Keyframe(AntennaStrikeLoopSeconds * 0.18f, baseValue - signedAmplitude * 0.25f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.34f, baseValue - signedAmplitude * 0.65f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.46f, baseValue + signedAmplitude),
                new Keyframe(AntennaStrikeLoopSeconds * 0.66f, baseValue - signedAmplitude * 0.42f),
                new Keyframe(AntennaStrikeLoopSeconds * 0.86f, baseValue + signedAmplitude * 0.16f),
                new Keyframe(AntennaStrikeLoopSeconds, baseValue));
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateCrawlBodyDrivenForeAftCurve(float baseValue, float followScale, float phaseDelay)
        {
            var samplePhases = GetCrawlBodyDrivenSamplePhases();
            var keys = new Keyframe[samplePhases.Length];
            for (var i = 0; i < samplePhases.Length; i++)
            {
                var phase = samplePhases[i];
                var bodyOffset = EvaluateCrawlBodyForeAftOffset01(phase - phaseDelay);
                keys[i] = new Keyframe(phase * CrawlForwardLoopSeconds, baseValue - bodyOffset * followScale);
            }

            var curve = new AnimationCurve(keys);
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateCrawlBodyDrivenVelocityCurve(float baseValue, float signedAmplitude, float phaseDelay)
        {
            var samplePhases = GetCrawlBodyDrivenSamplePhases();
            var keys = new Keyframe[samplePhases.Length];
            for (var i = 0; i < samplePhases.Length; i++)
            {
                var phase = samplePhases[i];
                var velocitySignal = EvaluateCrawlBodyVelocitySignal01(phase - phaseDelay);
                keys[i] = new Keyframe(phase * CrawlForwardLoopSeconds, baseValue + signedAmplitude * velocitySignal);
            }

            var curve = new AnimationCurve(keys);
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateCrawlBodyDrivenLiftCurve(float baseValue, float amplitude, float phaseDelay)
        {
            var samplePhases = GetCrawlBodyDrivenSamplePhases();
            var keys = new Keyframe[samplePhases.Length];
            for (var i = 0; i < samplePhases.Length; i++)
            {
                var phase = samplePhases[i];
                var velocityLoad = Mathf.Abs(EvaluateCrawlBodyVelocitySignal01(phase - phaseDelay));
                var sagLoad = Mathf.Abs(EvaluateCrawlBodyForeAftOffset01(phase - phaseDelay)) / 0.026f;
                var liftOffset = amplitude * (velocityLoad * 0.78f - Mathf.Clamp01(sagLoad) * 0.34f);
                keys[i] = new Keyframe(phase * CrawlForwardLoopSeconds, baseValue + liftOffset);
            }

            var curve = new AnimationCurve(keys);
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateCrawlBodyDrivenPitchCurve(float baseValue, float signedAmplitude, float phaseDelay)
        {
            var samplePhases = GetCrawlBodyDrivenSamplePhases();
            var keys = new Keyframe[samplePhases.Length];
            for (var i = 0; i < samplePhases.Length; i++)
            {
                var phase = samplePhases[i];
                var offsetSignal = -EvaluateCrawlBodyForeAftOffset01(phase - phaseDelay) / 0.026f;
                keys[i] = new Keyframe(phase * CrawlForwardLoopSeconds, baseValue + signedAmplitude * offsetSignal);
            }

            var curve = new AnimationCurve(keys);
            SmoothCurveTangents(curve);
            return curve;
        }

        private static float[] GetCrawlBodyDrivenSamplePhases()
        {
            return new[] { 0f, 0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1f };
        }

        private static float EvaluateCrawlBodyForeAftOffset01(float normalizedPhase)
        {
            var phase = Mathf.Repeat(normalizedPhase, 1f);
            if (phase <= 0.25f)
            {
                return Mathf.Lerp(0f, 0.026f, Mathf.SmoothStep(0f, 1f, phase / 0.25f));
            }

            if (phase <= 0.50f)
            {
                return Mathf.Lerp(0.026f, -0.017f, Mathf.SmoothStep(0f, 1f, (phase - 0.25f) / 0.25f));
            }

            if (phase <= 0.75f)
            {
                return Mathf.Lerp(-0.017f, 0.018f, Mathf.SmoothStep(0f, 1f, (phase - 0.50f) / 0.25f));
            }

            return Mathf.Lerp(0.018f, 0f, Mathf.SmoothStep(0f, 1f, (phase - 0.75f) / 0.25f));
        }

        private static float EvaluateCrawlBodyVelocitySignal01(float normalizedPhase)
        {
            var previous = EvaluateCrawlBodyForeAftOffset01(normalizedPhase - 0.035f);
            var next = EvaluateCrawlBodyForeAftOffset01(normalizedPhase + 0.035f);
            return Mathf.Clamp((next - previous) / 0.018f, -1f, 1f);
        }

        private static AnimationCurve CreateCrawlPhysicsLagCurve(float baseValue, float signedAmplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, baseValue),
                new Keyframe(CrawlForwardLoopSeconds * 0.18f, baseValue - signedAmplitude * 0.52f),
                new Keyframe(CrawlForwardLoopSeconds * 0.36f, baseValue - signedAmplitude),
                new Keyframe(CrawlForwardLoopSeconds * 0.58f, baseValue + signedAmplitude * 0.72f),
                new Keyframe(CrawlForwardLoopSeconds * 0.82f, baseValue + signedAmplitude * 0.28f),
                new Keyframe(CrawlForwardLoopSeconds, baseValue));
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateCrawlPhysicsLiftCurve(float baseValue, float amplitude)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, baseValue),
                new Keyframe(CrawlForwardLoopSeconds * 0.18f, baseValue + amplitude * 0.28f),
                new Keyframe(CrawlForwardLoopSeconds * 0.36f, baseValue + amplitude),
                new Keyframe(CrawlForwardLoopSeconds * 0.58f, baseValue - amplitude * 0.42f),
                new Keyframe(CrawlForwardLoopSeconds * 0.82f, baseValue + amplitude * 0.18f),
                new Keyframe(CrawlForwardLoopSeconds, baseValue));
            SmoothCurveTangents(curve);
            return curve;
        }

        private static AnimationCurve CreateCrawlLoopCurve(float startValue, float quarterValue, float halfValue, float threeQuarterValue)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, startValue),
                new Keyframe(CrawlForwardLoopSeconds * 0.25f, quarterValue),
                new Keyframe(CrawlForwardLoopSeconds * 0.50f, halfValue),
                new Keyframe(CrawlForwardLoopSeconds * 0.75f, threeQuarterValue),
                new Keyframe(CrawlForwardLoopSeconds, startValue));
            SmoothCurveTangents(curve);
            return curve;
        }

        private static float NormalizeEulerAngle(float value)
        {
            return value > 180f ? value - 360f : value;
        }

        private static void SetBlendShapeCurve(AnimationClip clip, string path, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(SkinnedMeshRenderer),
                    $"blendShape.{IdleBreathBlendShapeName}"),
                curve);
        }

        private static void SetTransformCurve(AnimationClip clip, string path, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static AnimationCurve CreateBreathValueCurve(float neutralValue, float fullValue, float endValue)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, neutralValue),
                new Keyframe(IdleBreathLoopSeconds * 0.25f, Mathf.Lerp(neutralValue, fullValue, 0.72f)),
                new Keyframe(IdleBreathLoopSeconds * 0.50f, fullValue),
                new Keyframe(IdleBreathLoopSeconds * 0.75f, Mathf.Lerp(neutralValue, fullValue, 0.68f)),
                new Keyframe(IdleBreathLoopSeconds, endValue));
            SmoothCurveTangents(curve);
            return curve;
        }

        private static void SmoothCurveTangents(AnimationCurve curve)
        {
            for (var i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
        }

        private static AnimatorController EnsureIdleBreathController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(UnityIdleBreathControllerAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(UnityIdleBreathControllerAssetPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(UnityIdleBreathControllerAssetPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(IdleBreathStateName);
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController EnsureCrawlForwardController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(UnityCrawlForwardControllerAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(UnityCrawlForwardControllerAssetPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(UnityCrawlForwardControllerAssetPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(CrawlForwardStateName);
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController EnsurePhysicsAntennaStrikeController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(UnityAntennaStrikeControllerAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(UnityAntennaStrikeControllerAssetPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(UnityAntennaStrikeControllerAssetPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(AntennaStrikeStateName);
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureLoopSetting(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
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

        private static void AssignApprovedMaterials(Transform root, ApprovedMaterialSet materialSet)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Accelerando prepared model contains no renderers.");
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = materialSet.Resolve(renderer.name, null);
                }
                else
                {
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = materialSet.Resolve(renderer.name, materials[i]);
                    }

                    renderer.sharedMaterials = materials;
                }

                EditorUtility.SetDirty(renderer);
            }
        }

        private static void HideMaceSocketRingRenderers(Transform root)
        {
            HideMaceSocketRingRenderer(root, "Left");
            HideMaceSocketRingRenderer(root, "Right");
        }

        private static void HideMaceSocketRingRenderer(Transform root, string sideName)
        {
            var maceSocket = RequireNamedChild(root, $"Accelerando_{sideName}_MaceSocket_Ring");
            foreach (var renderer in maceSocket.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
                renderer.forceRenderingOff = true;
                EditorUtility.SetDirty(renderer);
            }

            EditorUtility.SetDirty(maceSocket.gameObject);
            EditorUtility.SetDirty(maceSocket);
        }

        private static void RequireMaceSocketRingRenderersHidden(Transform root)
        {
            RequireMaceSocketRingRendererHidden(root, "Left");
            RequireMaceSocketRingRendererHidden(root, "Right");
        }

        private static void RequireMaceSocketRingRendererHidden(Transform root, string sideName)
        {
            var maceSocket = RequireNamedChild(root, $"Accelerando_{sideName}_MaceSocket_Ring");
            foreach (var renderer in maceSocket.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled)
                {
                    throw new InvalidOperationException($"{maceSocket.name} renderer must stay hidden to prevent lower mace protrusion.");
                }
            }
        }

        private static void EnsureRenderableHierarchy(Transform root)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.SetActive(true);
                child.gameObject.layer = root.gameObject.layer;
                EditorUtility.SetDirty(child.gameObject);
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ReconnectMaceChains(Transform root)
        {
            ReconnectMaceChain(root, "Left");
            ReconnectMaceChain(root, "Right");
        }

        private static void ReconnectMaceChain(Transform root, string sideName)
        {
            var antennaTip = RequireNamedChild(root, $"Accelerando_{sideName}_AntennaTip_Ring");
            var maceSocket = RequireNamedChild(root, $"Accelerando_{sideName}_MaceSocket_Ring");
            var links = new List<Transform>(ConnectedChainLinkCount);
            for (var i = 1; i <= ConnectedChainLinkCount; i++)
            {
                links.Add(RequireNamedChild(root, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}"));
            }

            var start = antennaTip.position;
            var end = maceSocket.position;
            var anchorDistance = Vector3.Distance(start, end);
            if (anchorDistance < ConnectedChainMinimumAnchorDistance)
            {
                throw new InvalidOperationException(
                    $"Accelerando {sideName} chain anchors are too close. Distance={anchorDistance:0.###}.");
            }

            var direction = (end - start).normalized;
            var visibleEnd = end - direction * GetConnectedChainMaceSideCutbackDistance(anchorDistance);
            var up = root.up;
            if (Vector3.Cross(direction, up).sqrMagnitude < 0.0001f)
            {
                up = root.forward;
            }

            var chainRotation = Quaternion.LookRotation(direction, up);
            for (var i = 0; i < links.Count; i++)
            {
                var link = links[i];
                var isVisible = i < ConnectedChainVisibleLinkCount;
                link.gameObject.SetActive(isVisible);
                var t = ConnectedChainVisibleLinkCount == 1 ? 0.5f : Mathf.Min(i, ConnectedChainVisibleLinkCount - 1) / (float)(ConnectedChainVisibleLinkCount - 1);
                link.position = Vector3.Lerp(start, visibleEnd, t);
                link.rotation = chainRotation * Quaternion.Euler(0f, 0f, i % 2 == 0 ? 0f : 90f);
                EditorUtility.SetDirty(link.gameObject);
                EditorUtility.SetDirty(link);
            }

            LogAntennaTipEmbeddedConnectionPlacement(root, sideName, antennaTip, links[0]);

            EditorUtility.SetDirty(antennaTip);
            EditorUtility.SetDirty(maceSocket);
            Debug.Log(
                "AccelerandoChainConnectionFix " +
                $"Root={root.name}, Side={sideName}, Links={links.Count}, VisibleLinks={ConnectedChainVisibleLinkCount}, " +
                $"Start={FormatVector(start)}, End={FormatVector(end)}, VisibleEnd={FormatVector(visibleEnd)}, " +
                $"AnchorDistance={anchorDistance:0.###}, FirstLinkDistance={Vector3.Distance(links[0].position, start):0.###}, " +
                $"VisibleLastToMace={Vector3.Distance(links[ConnectedChainVisibleLinkCount - 1].position, end):0.###}, " +
                $"HiddenMaceSideLinks={links.Count - ConnectedChainVisibleLinkCount}.");
        }

        private static float GetConnectedChainMaceSideCutbackDistance(float anchorDistance)
        {
            return Mathf.Min(ConnectedChainMaceSideCutbackDistance, anchorDistance * ConnectedChainMaceSideCutbackAnchorFactor);
        }

        private static void LogAntennaTipEmbeddedConnectionPlacement(Transform root, string sideName, Transform antennaTip, Transform firstLink)
        {
            var tip = antennaTip.position;
            var visibleHoop = RequireNamedChild(root, $"Accelerando_{sideName}_AntennaTip_EmbeddedVisibleChainEndHoop");
            var socketLip = RequireNamedChild(root, $"Accelerando_{sideName}_AntennaTip_EmbeddedSocketLip");

            Debug.Log(
                "AccelerandoAntennaTipEmbeddedConnectionPlacement " +
                $"Root={root.name}, Side={sideName}, Tip={FormatVector(tip)}, " +
                $"FirstLinkDistance={Vector3.Distance(firstLink.position, tip):0.###}, " +
                $"VisibleHoopDistance={Vector3.Distance(visibleHoop.position, tip):0.###}, " +
                $"SocketLipDistance={Vector3.Distance(socketLip.position, tip):0.###}.");
        }

        private static Transform RequireNamedChild(Transform root, string childName)
        {
            var child = FindChildByName(root, childName);
            if (child == null)
            {
                throw new InvalidOperationException($"{childName} is missing under {root.name}.");
            }

            return child;
        }

        private static void RemoveLowerPlateGeometry(Transform root)
        {
            Debug.Log("AccelerandoPlateRemoval Skipped=ApprovedSampleAlreadyHasNoLowerPlate.");
            return;

#pragma warning disable CS0162
            var removedTriangles = 0;
            var processedMeshes = 0;

            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var plateRemovedMesh = EnsurePlateRemovedMesh(mesh, out var removedFromMesh);
                if (removedFromMesh <= 0)
                {
                    continue;
                }

                meshFilter.sharedMesh = plateRemovedMesh;
                removedTriangles += removedFromMesh;
                processedMeshes++;
                EditorUtility.SetDirty(meshFilter);
            }

            foreach (var skinnedMesh in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skinnedMesh.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var plateRemovedMesh = EnsurePlateRemovedMesh(mesh, out var removedFromMesh);
                if (removedFromMesh <= 0)
                {
                    continue;
                }

                skinnedMesh.sharedMesh = plateRemovedMesh;
                removedTriangles += removedFromMesh;
                processedMeshes++;
                EditorUtility.SetDirty(skinnedMesh);
            }

            if (removedTriangles <= 0)
            {
                Debug.Log("AccelerandoPlateRemoval Skipped=ApprovedSampleAlreadyHasNoLowerPlate.");
                return;
            }

            Debug.Log(
                $"AccelerandoPlateRemoval ProcessedMeshes={processedMeshes}, RemovedTriangles={removedTriangles}.");
#pragma warning restore CS0162
        }

        private static Mesh EnsurePlateRemovedMesh(Mesh sourceMesh, out int removedTriangles)
        {
            removedTriangles = 0;
            var sourceMeshId = sourceMesh.GetInstanceID();
            if (PlateRemovedMeshCache.TryGetValue(sourceMeshId, out var cacheEntry))
            {
                removedTriangles = cacheEntry.RemovedTriangles;
                return cacheEntry.Mesh;
            }

            var vertices = sourceMesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                return sourceMesh;
            }

            var minY = vertices[0].y;
            var maxY = vertices[0].y;
            for (var i = 1; i < vertices.Length; i++)
            {
                minY = Mathf.Min(minY, vertices[i].y);
                maxY = Mathf.Max(maxY, vertices[i].y);
            }

            var height = Mathf.Max(maxY - minY, 0.001f);
            var cutoffY = minY + Mathf.Clamp(
                height * PlateRemovalHeightFraction,
                PlateRemovalMinimumHeight,
                PlateRemovalMaximumHeight);

            var remap = new int[vertices.Length];
            for (var i = 0; i < remap.Length; i++)
            {
                remap[i] = -1;
            }

            var normals = sourceMesh.normals;
            var tangents = sourceMesh.tangents;
            var colors = sourceMesh.colors;
            var colors32 = sourceMesh.colors32;
            var uv = sourceMesh.uv;
            var uv2 = sourceMesh.uv2;
            var uv3 = sourceMesh.uv3;
            var uv4 = sourceMesh.uv4;
            var uv5 = sourceMesh.uv5;
            var uv6 = sourceMesh.uv6;
            var uv7 = sourceMesh.uv7;
            var uv8 = sourceMesh.uv8;
            var boneWeights = sourceMesh.boneWeights;

            var newVertices = new System.Collections.Generic.List<Vector3>(vertices.Length);
            var newNormals = normals.Length == vertices.Length ? new System.Collections.Generic.List<Vector3>(vertices.Length) : null;
            var newTangents = tangents.Length == vertices.Length ? new System.Collections.Generic.List<Vector4>(vertices.Length) : null;
            var newColors = colors.Length == vertices.Length ? new System.Collections.Generic.List<Color>(vertices.Length) : null;
            var newColors32 = colors32.Length == vertices.Length ? new System.Collections.Generic.List<Color32>(vertices.Length) : null;
            var newUv = uv.Length == vertices.Length ? new System.Collections.Generic.List<Vector2>(vertices.Length) : null;
            var newUv2 = uv2.Length == vertices.Length ? new System.Collections.Generic.List<Vector2>(vertices.Length) : null;
            var newUv3 = uv3.Length == vertices.Length ? new System.Collections.Generic.List<Vector2>(vertices.Length) : null;
            var newUv4 = uv4.Length == vertices.Length ? new System.Collections.Generic.List<Vector2>(vertices.Length) : null;
            var newUv5 = uv5.Length == vertices.Length ? new System.Collections.Generic.List<Vector2>(vertices.Length) : null;
            var newUv6 = uv6.Length == vertices.Length ? new System.Collections.Generic.List<Vector2>(vertices.Length) : null;
            var newUv7 = uv7.Length == vertices.Length ? new System.Collections.Generic.List<Vector2>(vertices.Length) : null;
            var newUv8 = uv8.Length == vertices.Length ? new System.Collections.Generic.List<Vector2>(vertices.Length) : null;
            var newBoneWeights = boneWeights.Length == vertices.Length ? new System.Collections.Generic.List<BoneWeight>(vertices.Length) : null;
            var keptSubmeshTriangles = new System.Collections.Generic.List<int>[sourceMesh.subMeshCount];

            for (var submesh = 0; submesh < sourceMesh.subMeshCount; submesh++)
            {
                var triangles = sourceMesh.GetTriangles(submesh);
                var keptTriangles = new System.Collections.Generic.List<int>(triangles.Length);
                for (var i = 0; i + 2 < triangles.Length; i += 3)
                {
                    var aIndex = triangles[i];
                    var bIndex = triangles[i + 1];
                    var cIndex = triangles[i + 2];
                    if (IsLowerPlateTriangle(vertices[aIndex], vertices[bIndex], vertices[cIndex], cutoffY))
                    {
                        removedTriangles++;
                        continue;
                    }

                    keptTriangles.Add(MapPlateRemovedVertex(
                        aIndex,
                        remap,
                        vertices,
                        normals,
                        tangents,
                        colors,
                        colors32,
                        uv,
                        uv2,
                        uv3,
                        uv4,
                        uv5,
                        uv6,
                        uv7,
                        uv8,
                        boneWeights,
                        newVertices,
                        newNormals,
                        newTangents,
                        newColors,
                        newColors32,
                        newUv,
                        newUv2,
                        newUv3,
                        newUv4,
                        newUv5,
                        newUv6,
                        newUv7,
                        newUv8,
                        newBoneWeights));
                    keptTriangles.Add(MapPlateRemovedVertex(
                        bIndex,
                        remap,
                        vertices,
                        normals,
                        tangents,
                        colors,
                        colors32,
                        uv,
                        uv2,
                        uv3,
                        uv4,
                        uv5,
                        uv6,
                        uv7,
                        uv8,
                        boneWeights,
                        newVertices,
                        newNormals,
                        newTangents,
                        newColors,
                        newColors32,
                        newUv,
                        newUv2,
                        newUv3,
                        newUv4,
                        newUv5,
                        newUv6,
                        newUv7,
                        newUv8,
                        newBoneWeights));
                    keptTriangles.Add(MapPlateRemovedVertex(
                        cIndex,
                        remap,
                        vertices,
                        normals,
                        tangents,
                        colors,
                        colors32,
                        uv,
                        uv2,
                        uv3,
                        uv4,
                        uv5,
                        uv6,
                        uv7,
                        uv8,
                        boneWeights,
                        newVertices,
                        newNormals,
                        newTangents,
                        newColors,
                        newColors32,
                        newUv,
                        newUv2,
                        newUv3,
                        newUv4,
                        newUv5,
                        newUv6,
                        newUv7,
                        newUv8,
                        newBoneWeights));
                }

                keptSubmeshTriangles[submesh] = keptTriangles;
            }

            if (removedTriangles <= 0)
            {
                return sourceMesh;
            }

            var modifiedMesh = new Mesh
            {
                name = sourceMesh.name + "_PlateRemoved",
                indexFormat = sourceMesh.indexFormat
            };
            modifiedMesh.SetVertices(newVertices);
            if (newNormals != null)
            {
                modifiedMesh.SetNormals(newNormals);
            }

            if (newTangents != null)
            {
                modifiedMesh.SetTangents(newTangents);
            }

            if (newColors != null)
            {
                modifiedMesh.SetColors(newColors);
            }
            else if (newColors32 != null)
            {
                modifiedMesh.SetColors(newColors32);
            }

            SetMeshUvChannel(modifiedMesh, 0, newUv);
            SetMeshUvChannel(modifiedMesh, 1, newUv2);
            SetMeshUvChannel(modifiedMesh, 2, newUv3);
            SetMeshUvChannel(modifiedMesh, 3, newUv4);
            SetMeshUvChannel(modifiedMesh, 4, newUv5);
            SetMeshUvChannel(modifiedMesh, 5, newUv6);
            SetMeshUvChannel(modifiedMesh, 6, newUv7);
            SetMeshUvChannel(modifiedMesh, 7, newUv8);
            if (newBoneWeights != null)
            {
                modifiedMesh.boneWeights = newBoneWeights.ToArray();
                modifiedMesh.bindposes = sourceMesh.bindposes;
            }

            modifiedMesh.subMeshCount = sourceMesh.subMeshCount;
            for (var submesh = 0; submesh < sourceMesh.subMeshCount; submesh++)
            {
                modifiedMesh.SetTriangles(keptSubmeshTriangles[submesh], submesh, true);
            }

            modifiedMesh.RecalculateNormals();
            modifiedMesh.RecalculateTangents();
            modifiedMesh.RecalculateBounds();
            modifiedMesh.UploadMeshData(false);
            var assetPath = UnityModelFolder + "/accelerando_plate_removed_" + SanitizeAssetName(sourceMesh.name) + ".asset";
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existingMesh != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(modifiedMesh, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var resultMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath) ?? modifiedMesh;
            PlateRemovedMeshCache[sourceMeshId] = new PlateRemovedMeshCacheEntry
            {
                Mesh = resultMesh,
                RemovedTriangles = removedTriangles
            };
            return resultMesh;
        }

        private static int MapPlateRemovedVertex(
            int sourceIndex,
            int[] remap,
            Vector3[] vertices,
            Vector3[] normals,
            Vector4[] tangents,
            Color[] colors,
            Color32[] colors32,
            Vector2[] uv,
            Vector2[] uv2,
            Vector2[] uv3,
            Vector2[] uv4,
            Vector2[] uv5,
            Vector2[] uv6,
            Vector2[] uv7,
            Vector2[] uv8,
            BoneWeight[] boneWeights,
            System.Collections.Generic.List<Vector3> newVertices,
            System.Collections.Generic.List<Vector3> newNormals,
            System.Collections.Generic.List<Vector4> newTangents,
            System.Collections.Generic.List<Color> newColors,
            System.Collections.Generic.List<Color32> newColors32,
            System.Collections.Generic.List<Vector2> newUv,
            System.Collections.Generic.List<Vector2> newUv2,
            System.Collections.Generic.List<Vector2> newUv3,
            System.Collections.Generic.List<Vector2> newUv4,
            System.Collections.Generic.List<Vector2> newUv5,
            System.Collections.Generic.List<Vector2> newUv6,
            System.Collections.Generic.List<Vector2> newUv7,
            System.Collections.Generic.List<Vector2> newUv8,
            System.Collections.Generic.List<BoneWeight> newBoneWeights)
        {
            if (remap[sourceIndex] >= 0)
            {
                return remap[sourceIndex];
            }

            var mappedIndex = newVertices.Count;
            remap[sourceIndex] = mappedIndex;
            newVertices.Add(vertices[sourceIndex]);
            newNormals?.Add(normals[sourceIndex]);
            newTangents?.Add(tangents[sourceIndex]);
            newColors?.Add(colors[sourceIndex]);
            newColors32?.Add(colors32[sourceIndex]);
            newUv?.Add(uv[sourceIndex]);
            newUv2?.Add(uv2[sourceIndex]);
            newUv3?.Add(uv3[sourceIndex]);
            newUv4?.Add(uv4[sourceIndex]);
            newUv5?.Add(uv5[sourceIndex]);
            newUv6?.Add(uv6[sourceIndex]);
            newUv7?.Add(uv7[sourceIndex]);
            newUv8?.Add(uv8[sourceIndex]);
            newBoneWeights?.Add(boneWeights[sourceIndex]);
            return mappedIndex;
        }

        private static void SetMeshUvChannel(Mesh mesh, int channel, System.Collections.Generic.List<Vector2> uvChannel)
        {
            if (uvChannel != null)
            {
                mesh.SetUVs(channel, uvChannel);
            }
        }

        private static bool IsLowerPlateTriangle(Vector3 a, Vector3 b, Vector3 c, float cutoffY)
        {
            var maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            return maxY <= cutoffY + PlateRemovalEpsilon;
        }

        private static void InspectPlateRemovedGeometry(Transform modelObject)
        {
            var meshFilters = modelObject.GetComponentsInChildren<MeshFilter>(true);
            var skinnedMeshes = modelObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var assignedPlateRemovedMesh = false;

            foreach (var meshFilter in meshFilters)
            {
                if (IsPlateRemovedMeshAsset(meshFilter.sharedMesh))
                {
                    LogPlateRemovedMeshInspection(meshFilter.sharedMesh, meshFilter.GetComponent<Renderer>());
                    assignedPlateRemovedMesh = true;
                }
            }

            foreach (var skinnedMesh in skinnedMeshes)
            {
                if (IsPlateRemovedMeshAsset(skinnedMesh.sharedMesh))
                {
                    LogPlateRemovedMeshInspection(skinnedMesh.sharedMesh, skinnedMesh);
                    assignedPlateRemovedMesh = true;
                }
            }

            if (!assignedPlateRemovedMesh)
            {
                throw new InvalidOperationException("Accelerando model does not use a plate-removed mesh asset.");
            }
        }

        private static bool IsPlateRemovedMeshAsset(Mesh mesh)
        {
            if (mesh == null)
            {
                return false;
            }

            var assetPath = AssetDatabase.GetAssetPath(mesh);
            return assetPath.StartsWith(UnityModelFolder + "/accelerando_plate_removed_", StringComparison.Ordinal) ||
                string.Equals(assetPath, UnityIdleBreathMorphMeshAssetPath, StringComparison.Ordinal) ||
                string.Equals(assetPath, UnityModelAssetPath, StringComparison.Ordinal);
        }

        private static void LogPlateRemovedMeshInspection(Mesh mesh, Renderer renderer)
        {
            var triangles = 0;
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                triangles += mesh.GetTriangles(submesh).Length / 3;
            }

            Debug.Log(
                "AccelerandoPlateRemovedMeshInspection " +
                $"Asset={AssetDatabase.GetAssetPath(mesh)}, Vertices={mesh.vertexCount}, SubMeshes={mesh.subMeshCount}, " +
                $"Triangles={triangles}, LocalBoundsCenter={FormatVector(mesh.bounds.center)}, LocalBoundsSize={FormatVector(mesh.bounds.size)}, " +
                $"Renderer={renderer?.name ?? "(none)"}, RendererEnabled={renderer != null && renderer.enabled}, " +
                $"ForceRenderingOff={renderer != null && renderer.forceRenderingOff}, " +
                $"ActiveInHierarchy={renderer != null && renderer.gameObject.activeInHierarchy}, Layer={(renderer != null ? renderer.gameObject.layer : -1)}.");
        }

        private static void InspectMeshStructure(Transform root)
        {
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var meshFilter in meshFilters)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                LogMeshStructure(GetTransformPath(root, meshFilter.transform), mesh);
            }

            var skinnedMeshes = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var skinnedMesh in skinnedMeshes)
            {
                var mesh = skinnedMesh.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                LogMeshStructure(GetTransformPath(root, skinnedMesh.transform), mesh);
            }
        }

        private static void LogMeshStructure(string path, Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                Debug.Log($"AccelerandoMeshStructure Path={path}, Mesh={mesh.name}, Vertices=0.");
                return;
            }

            var min = vertices[0];
            var max = vertices[0];
            for (var i = 1; i < vertices.Length; i++)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }

            var height = Mathf.Max(max.y - min.y, 0.001f);
            var bottomThreshold = min.y + height * 0.10f;
            var bottomVertices = 0;
            for (var i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y <= bottomThreshold)
                {
                    bottomVertices++;
                }
            }

            var bottomTriangles = 0;
            var totalTriangles = 0;
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                var triangles = mesh.GetTriangles(submesh);
                totalTriangles += triangles.Length / 3;
                for (var i = 0; i + 2 < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]];
                    var b = vertices[triangles[i + 1]];
                    var c = vertices[triangles[i + 2]];
                    if (a.y <= bottomThreshold && b.y <= bottomThreshold && c.y <= bottomThreshold)
                    {
                        bottomTriangles++;
                    }
                }
            }

            Debug.Log(
                "AccelerandoMeshStructure " +
                $"Path={path}, Mesh={mesh.name}, Vertices={vertices.Length}, SubMeshes={mesh.subMeshCount}, " +
                $"Triangles={totalTriangles}, LocalMin={FormatVector(min)}, LocalMax={FormatVector(max)}, " +
                $"BottomThresholdY={bottomThreshold:0.###}, BottomVertices={bottomVertices}, BottomTriangles={bottomTriangles}.");
        }

        private static string GetTransformPath(Transform root, Transform target)
        {
            if (target == root)
            {
                return target.name;
            }

            var path = target.name;
            var current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return root.name + "/" + path;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }

            var path = target.name;
            var current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            if (current != root)
            {
                throw new InvalidOperationException($"{target.name} is not under {root.name}.");
            }

            return path;
        }

        private static int GetIdleBreathBlendShapeIndex(Mesh mesh)
        {
            if (mesh == null)
            {
                throw new InvalidOperationException("Idle breath morph mesh is missing.");
            }

            var index = mesh.GetBlendShapeIndex(IdleBreathBlendShapeName);
            if (index < 0)
            {
                throw new InvalidOperationException($"{mesh.name} does not contain {IdleBreathBlendShapeName}.");
            }

            return index;
        }

        private static string SanitizeAssetName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return "Mesh";
            }

            var invalidCharacters = Path.GetInvalidFileNameChars();
            var safeName = rawName;
            foreach (var invalidCharacter in invalidCharacters)
            {
                safeName = safeName.Replace(invalidCharacter, '_');
            }

            safeName = safeName.Replace(' ', '_');
            return string.IsNullOrWhiteSpace(safeName) ? "Mesh" : safeName;
        }

        private static void AlignToGround(Transform root, float groundY)
        {
            var bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            root.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static void ConfigureInitialReviewCamera(Transform placementRoot)
        {
            var focus = placementRoot;
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var camera = FindOrCreateReviewCamera();
            var frontDirection = CalculateAccelerandoVisualFrontDirection(FindAccelerandoCameraFocus(placementRoot));
            var lookAt = CalculateLookAt(bounds);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.0f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var verticalOffset = Mathf.Clamp(bounds.extents.y * 0.16f, 0.08f, 0.30f);
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

            var focus = placementRoot;
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = CalculateLookAt(bounds);
            var frontDirection = CalculateAccelerandoVisualFrontDirection(FindAccelerandoCameraFocus(placementRoot));
            var startPosition = new Vector3(
                lookAt.x - frontDirection.x * PlayerFrontDistance,
                0f,
                lookAt.z - frontDirection.z * PlayerFrontDistance);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
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
            InspectPlateRemovedGeometry(modelObject);
            InspectAnimationReviewSlots(placementRoot);
            InspectAlignedReviewRow(placementRoot);
            InspectIdleBreathingAnimation(placementRoot);
            InspectCrawlForwardMotion(placementRoot);
            InspectPhysicsAntennaStrikeMotion(placementRoot);
            InspectMaceChainConnections(placementRoot);
            InspectAntennaTipEmbeddedConnections(placementRoot);
            InspectConSpiritoRelativeZPlacement(placementRoot);
            InspectPlayerStart(placementRoot);
            InspectReviewCamera(placementRoot);

            var bounds = CalculateRendererBounds(reviewObject, new Bounds(reviewObject.position, Vector3.one));
            var conSpiritoRoot = RequireSceneRoot(ConSpiritoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var spacing = CalculateLongaTergoZSpacing(longaRoot.transform, tergoRoot.transform);
            var player = FindPlayerStartTransform();
            Debug.Log(
                "AccelerandoPlacementInspection " +
                $"Root={PlacementRootName}, Object={PlacementObjectName}, Model={ModelChildName}, " +
                $"UnityAsset={UnityModelAssetPath}, Materials={UnityApprovedFleshMaterialAssetPath}|{UnityApprovedShellMaterialAssetPath}|{UnityApprovedMetalMaterialAssetPath}, Renderers={renderers.Length}, " +
                $"ConSpiritoZ={conSpiritoRoot.transform.position.z:0.###}, LongaZ={longaRoot.transform.position.z:0.###}, " +
                $"TergoZ={tergoRoot.transform.position.z:0.###}, LongaTergoZSpacing={spacing:0.###}, " +
                $"AccelerandoPosition={FormatVector(placementRoot.position)}, BoundsCenter={FormatVector(bounds.center)}, " +
                $"BoundsSize={FormatVector(bounds.size)}, Player={FormatVector(player != null ? player.position : Vector3.zero)}.");
        }

        private static void InspectAnimationReviewSlots(Transform placementRoot)
        {
            var summary = new System.Text.StringBuilder();
            for (var i = 0; i < AnimationReviewSlots.Length; i++)
            {
                var slot = AnimationReviewSlots[i];
                var slotObject = placementRoot.Find(slot.ObjectName);
                if (slotObject == null)
                {
                    throw new InvalidOperationException($"{slot.ObjectName} animation review slot is missing.");
                }

                var modelObject = slotObject.Find(ModelChildName);
                if (modelObject == null)
                {
                    throw new InvalidOperationException($"{ModelChildName} is missing under {slot.ObjectName}.");
                }

                var renderers = slotObject.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException($"{slot.ObjectName} contains no renderers.");
                }

                InspectMaterialAssignments(renderers);
                InspectPlateRemovedGeometry(modelObject);

                if (summary.Length > 0)
                {
                    summary.Append("; ");
                }

                summary.Append(slot.StateId);
                summary.Append('=');
                summary.Append(slot.KoreanName);
                summary.Append('@');
                summary.Append(FormatVector(slotObject.localPosition));
            }

            Debug.Log(
                "AccelerandoAnimationSlotInspection " +
                $"Count={AnimationReviewSlots.Length}, Slots={summary}.");
        }

        private static void InspectMaceChainConnections(Transform placementRoot)
        {
            var summary = new System.Text.StringBuilder();
            InspectMaceChainConnectionsForObject(placementRoot, PlacementObjectName, summary);
            for (var i = 0; i < AnimationReviewSlots.Length; i++)
            {
                InspectMaceChainConnectionsForObject(placementRoot, AnimationReviewSlots[i].ObjectName, summary);
            }

            Debug.Log(
                "AccelerandoChainConnectionInspection " +
                $"Objects={AnimationReviewSlots.Length + 1}, Chains={summary}.");
        }

        private static void InspectMaceChainConnectionsForObject(Transform placementRoot, string objectName, System.Text.StringBuilder summary)
        {
            var reviewObject = placementRoot.Find(objectName);
            if (reviewObject == null)
            {
                throw new InvalidOperationException($"{objectName} is missing under {PlacementRootName}.");
            }

            InspectMaceChainConnectionForSide(reviewObject, objectName, "Left", summary);
            InspectMaceChainConnectionForSide(reviewObject, objectName, "Right", summary);
        }

        private static void InspectMaceChainConnectionForSide(Transform reviewObject, string objectName, string sideName, System.Text.StringBuilder summary)
        {
            var antennaTip = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_Ring");
            var maceSocket = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_MaceSocket_Ring");
            var firstLink = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_ConnectedChain_Link_01");
            var visibleLastLink = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_ConnectedChain_Link_{ConnectedChainVisibleLinkCount:00}");
            var hiddenMaceSideLink = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_ConnectedChain_Link_{ConnectedChainLinkCount:00}");

            var firstDistance = Vector3.Distance(firstLink.position, antennaTip.position);
            var visibleLastToMace = Vector3.Distance(visibleLastLink.position, maceSocket.position);
            var anchorDistance = Vector3.Distance(antennaTip.position, maceSocket.position);
            var expectedCutback = GetConnectedChainMaceSideCutbackDistance(anchorDistance);
            var maceBoundsClearance = GetMinimumVisibleChainMaceBoundsClearance(reviewObject, sideName);
            var maceTopClearance = GetMinimumVisibleChainMaceTopClearance(reviewObject, sideName);
            if (firstDistance > ConnectedChainEndpointTolerance ||
                Mathf.Abs(visibleLastToMace - expectedCutback) > ConnectedChainMaceSideCutbackTolerance ||
                maceBoundsClearance < ConnectedChainMinimumMaceBoundsClearance ||
                maceTopClearance < ConnectedChainMinimumMaceTopClearance ||
                maceTopClearance > ConnectedChainMaximumMaceTopClearance ||
                hiddenMaceSideLink.gameObject.activeSelf)
            {
                throw new InvalidOperationException(
                    $"{objectName} {sideName} chain mace-side trim is invalid. " +
                    $"First={firstDistance:0.###}, VisibleLastToMace={visibleLastToMace:0.###}, " +
                    $"ExpectedCutback={expectedCutback:0.###}, MaceBoundsClearance={maceBoundsClearance:0.###}, " +
                    $"MaceTopClearance={maceTopClearance:0.###}, " +
                    $"AllowedMaceTopClearance={ConnectedChainMinimumMaceTopClearance:0.###}-{ConnectedChainMaximumMaceTopClearance:0.###}, " +
                    $"HiddenMaceSideActive={hiddenMaceSideLink.gameObject.activeSelf}.");
            }

            if (summary.Length > 0)
            {
                summary.Append("; ");
            }

            summary.Append(objectName);
            summary.Append('/');
            summary.Append(sideName);
            summary.Append($"=first:{firstDistance:0.###},visibleLastToMace:{visibleLastToMace:0.###},maceBoundsClearance:{maceBoundsClearance:0.###},maceTopClearance:{maceTopClearance:0.###},maceTopClearanceLimit:{ConnectedChainMaximumMaceTopClearance:0.###},hidden:{!hiddenMaceSideLink.gameObject.activeSelf}");
        }

        private static float GetMinimumVisibleChainMaceBoundsClearance(Transform root, string sideName)
        {
            var maceBounds = CalculateActiveRendererBounds(
                FindCrawlMaceTransforms(root, sideName),
                new Bounds(RequireNamedChild(root, $"Accelerando_{sideName}_MaceSocket_Ring").position, Vector3.one * 0.05f));
            var minClearance = float.PositiveInfinity;
            for (var i = 1; i <= ConnectedChainVisibleLinkCount; i++)
            {
                var link = RequireNamedChild(root, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}");
                if (!link.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var linkBounds = CalculateActiveRendererBounds(new[] { link }, new Bounds(link.position, Vector3.one * 0.02f));
                minClearance = Mathf.Min(minClearance, GetBoundsClearance(linkBounds, maceBounds));
            }

            return float.IsPositiveInfinity(minClearance) ? 0f : minClearance;
        }

        private static float GetMinimumVisibleChainMaceTopClearance(Transform root, string sideName)
        {
            var maceBounds = CalculateActiveRendererBounds(
                FindCrawlMaceTransforms(root, sideName),
                new Bounds(RequireNamedChild(root, $"Accelerando_{sideName}_MaceSocket_Ring").position, Vector3.one * 0.05f));
            var minClearance = float.PositiveInfinity;
            for (var i = 1; i <= ConnectedChainVisibleLinkCount; i++)
            {
                var link = RequireNamedChild(root, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}");
                if (!link.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var linkBounds = CalculateActiveRendererBounds(new[] { link }, new Bounds(link.position, Vector3.one * 0.02f));
                minClearance = Mathf.Min(minClearance, linkBounds.min.y - maceBounds.max.y);
            }

            return float.IsPositiveInfinity(minClearance) ? 0f : minClearance;
        }

        private static void InspectAntennaTipEmbeddedConnections(Transform placementRoot)
        {
            var summary = new System.Text.StringBuilder();
            InspectAntennaTipEmbeddedConnectionsForObject(placementRoot, PlacementObjectName, summary);
            for (var i = 0; i < AnimationReviewSlots.Length; i++)
            {
                InspectAntennaTipEmbeddedConnectionsForObject(placementRoot, AnimationReviewSlots[i].ObjectName, summary);
            }

            Debug.Log(
                "AccelerandoAntennaTipEmbeddedConnectionInspection " +
                $"Objects={AnimationReviewSlots.Length + 1}, Connections={summary}.");
        }

        private static void InspectAntennaTipEmbeddedConnectionsForObject(Transform placementRoot, string objectName, System.Text.StringBuilder summary)
        {
            var reviewObject = placementRoot.Find(objectName);
            if (reviewObject == null)
            {
                throw new InvalidOperationException($"{objectName} is missing under {PlacementRootName}.");
            }

            InspectAntennaTipEmbeddedConnectionForSide(reviewObject, objectName, "Left", summary);
            InspectAntennaTipEmbeddedConnectionForSide(reviewObject, objectName, "Right", summary);
        }

        private static void InspectAntennaTipEmbeddedConnectionForSide(Transform reviewObject, string objectName, string sideName, System.Text.StringBuilder summary)
        {
            var antennaTip = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_Ring");
            var firstLink = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_ConnectedChain_Link_01");
            var visibleHoop = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedVisibleChainEndHoop");
            var socketLip = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedSocketLip");
            var retainingPin = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedRetainingPin");
            var stem = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedStem");
            var surfaceSaddle = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedSurfaceSaddle");
            var upperOcclusion = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedUpperOcclusionBulge");
            var pressurePad = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedFleshPressurePad");

            var hoopToTip = Vector3.Distance(visibleHoop.position, antennaTip.position);
            var hoopToFirst = Vector3.Distance(visibleHoop.position, firstLink.position);
            var socketToTip = Vector3.Distance(socketLip.position, antennaTip.position);
            var pinToTip = Vector3.Distance(retainingPin.position, antennaTip.position);
            var stemToTip = Vector3.Distance(stem.position, antennaTip.position);
            var saddleToTip = Vector3.Distance(surfaceSaddle.position, antennaTip.position);
            var occlusionToTip = Vector3.Distance(upperOcclusion.position, antennaTip.position);
            var pressureToTip = Vector3.Distance(pressurePad.position, antennaTip.position);

            if (hoopToTip > AntennaTipEmbeddedHoopCenterTolerance || hoopToFirst > AntennaTipEmbeddedHoopCenterTolerance)
            {
                throw new InvalidOperationException(
                    $"{objectName} {sideName} embedded chain-end hoop is detached from antenna tip chain start. " +
                    $"HoopToTip={hoopToTip:0.###}, HoopToFirst={hoopToFirst:0.###}, Tolerance={AntennaTipEmbeddedHoopCenterTolerance:0.###}.");
            }

            if (socketToTip > AntennaTipEmbeddedPartTolerance ||
                pinToTip > AntennaTipEmbeddedPartTolerance ||
                stemToTip > AntennaTipEmbeddedPartTolerance ||
                saddleToTip > AntennaTipEmbeddedPartTolerance ||
                occlusionToTip > AntennaTipEmbeddedPartTolerance ||
                pressureToTip > AntennaTipEmbeddedPartTolerance)
            {
                throw new InvalidOperationException(
                    $"{objectName} {sideName} embedded connection parts are detached from antenna tip. " +
                    $"Socket={socketToTip:0.###}, Pin={pinToTip:0.###}, Stem={stemToTip:0.###}, Saddle={saddleToTip:0.###}, " +
                    $"Occlusion={occlusionToTip:0.###}, Pressure={pressureToTip:0.###}, Tolerance={AntennaTipEmbeddedPartTolerance:0.###}.");
            }

            if (summary.Length > 0)
            {
                summary.Append("; ");
            }

            summary.Append(objectName);
            summary.Append('/');
            summary.Append(sideName);
            summary.Append($"=hoopTip:{hoopToTip:0.###},hoopFirst:{hoopToFirst:0.###},socket:{socketToTip:0.###},occlusion:{occlusionToTip:0.###}");
        }

        private static void InspectAlignedReviewRow(Transform placementRoot)
        {
            var staticReview = placementRoot.Find(PlacementObjectName);
            if (staticReview == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            var expectedZ = staticReview.localPosition.z;
            var expectedY = staticReview.localPosition.y;
            var expectedStaticX = CalculateStaticReviewAlignedX();
            if (Mathf.Abs(staticReview.localPosition.x - expectedStaticX) > 0.01f)
            {
                throw new InvalidOperationException(
                    $"{PlacementObjectName} is not the leftmost review object. ExpectedX={expectedStaticX:0.###}, Actual={staticReview.localPosition.x:0.###}.");
            }

            var summary = new System.Text.StringBuilder();
            summary.Append("Static=");
            summary.Append(FormatVector(staticReview.localPosition));

            for (var i = 0; i < AnimationReviewSlots.Length; i++)
            {
                var slot = AnimationReviewSlots[i];
                var slotObject = placementRoot.Find(slot.ObjectName);
                if (slotObject == null)
                {
                    throw new InvalidOperationException($"{slot.ObjectName} animation review slot is missing.");
                }

                var expectedSlotX = CalculateAnimationSlotAlignedX(i);
                if (Mathf.Abs(slotObject.localPosition.z - expectedZ) > 0.01f ||
                    Mathf.Abs(slotObject.localPosition.y - expectedY) > 0.01f ||
                    Mathf.Abs(slotObject.localPosition.x - expectedSlotX) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"{slot.ObjectName} is not aligned with {PlacementObjectName}. ExpectedX={expectedSlotX:0.###}, Static={staticReview.localPosition}, Slot={slotObject.localPosition}.");
                }

                summary.Append("; ");
                summary.Append(slot.StateId);
                summary.Append('=');
                summary.Append(FormatVector(slotObject.localPosition));
            }

            Debug.Log(
                "AccelerandoAlignedReviewRowInspection " +
                $"Count={AnimationReviewSlots.Length + 1}, Row={summary}.");
        }

        private static void InspectIdleBreathingAnimation(Transform placementRoot)
        {
            var idleSlot = placementRoot.Find(IdleSlotObjectName);
            if (idleSlot == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} is missing under {PlacementRootName}.");
            }

            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} does not have an Animator with a controller.");
            }

            var bodyRenderer = FindIdleBreathBodyRenderer(idleSlot);
            if (bodyRenderer == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} does not use {IdleBreathBodyObjectName} for body breathing.");
            }

            var mesh = bodyRenderer.sharedMesh;
            var bodyTriangleCount = CountMeshTriangles(mesh);
            var staticAccessoryTriangleCount = CountIdleBreathStaticAccessoryTriangles(idleSlot, bodyRenderer.transform);
            if (bodyTriangleCount <= 0 || staticAccessoryTriangleCount <= 0)
            {
                throw new InvalidOperationException(
                    $"{IdleSlotObjectName} breathing mesh triangles are invalid. Body={bodyTriangleCount}, StaticAccessories={staticAccessoryTriangleCount}.");
            }

            var blendShapeIndex = GetIdleBreathBlendShapeIndex(mesh);
            var deltaVertices = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(blendShapeIndex, 0, deltaVertices, deltaNormals, deltaTangents);
            var maxDelta = 0f;
            for (var i = 0; i < deltaVertices.Length; i++)
            {
                maxDelta = Mathf.Max(maxDelta, deltaVertices[i].magnitude);
            }

            if (maxDelta < 0.025f)
            {
                throw new InvalidOperationException($"{IdleBreathBlendShapeName} morph delta is too small. MaxDelta={maxDelta:0.###}.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityIdleBreathClipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Idle breath clip is missing at {UnityIdleBreathClipAssetPath}.");
            }

            var rendererPath = GetRelativePath(idleSlot, bodyRenderer.transform);
            var blendShapeCurveRange = GetCurveRange(clip, rendererPath, typeof(SkinnedMeshRenderer), $"blendShape.{IdleBreathBlendShapeName}");
            var staticAccessoryCurveRange = GetIdleBreathStaticAccessoryCurveRange(clip, idleSlot, bodyRenderer.transform);
            if (blendShapeCurveRange < 1f)
            {
                throw new InvalidOperationException(
                    $"{IdleSlotObjectName} blend shape breathing curve range is too small. " +
                    $"BlendShapeRange={blendShapeCurveRange:0.###}.");
            }

            if (staticAccessoryCurveRange > 0.001f)
            {
                throw new InvalidOperationException($"Idle breathing static accessories must remain static. CurveRange={staticAccessoryCurveRange:0.###}.");
            }

            Debug.Log(
                "AccelerandoIdleBreathInspection " +
                $"Slot={IdleSlotObjectName}, Renderer={bodyRenderer.name}, StaticAccessories=MeshRendererChildren, Mesh={AssetDatabase.GetAssetPath(mesh)}, " +
                $"BodyTriangles={bodyTriangleCount}, StaticAccessoryTriangles={staticAccessoryTriangleCount}, " +
                $"BlendShape={IdleBreathBlendShapeName}, BlendShapeIndex={blendShapeIndex}, MaxDelta={maxDelta:0.###}, " +
                $"BlendShapeCurveRange={blendShapeCurveRange:0.###}, StaticAccessoryCurveRange={staticAccessoryCurveRange:0.###}, " +
                $"Clip={UnityIdleBreathClipAssetPath}, ClipLength={clip.length:0.###}, Loop={AnimationUtility.GetAnimationClipSettings(clip).loopTime}, " +
                $"Controller={AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)}.");
        }

        private static void InspectCrawlForwardMotion(Transform placementRoot)
        {
            var crawlSlot = placementRoot.Find(CrawlSlotObjectName);
            if (crawlSlot == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} is missing under {PlacementRootName}.");
            }

            var modelObject = crawlSlot.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {CrawlSlotObjectName}.");
            }

            var animator = crawlSlot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} does not have an Animator with a controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} crawl motion must not use root motion.");
            }

            var controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
            if (!string.Equals(controllerPath, UnityCrawlForwardControllerAssetPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} uses the wrong crawl controller. Expected={UnityCrawlForwardControllerAssetPath}, Actual={controllerPath}.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityCrawlForwardClipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Crawl forward clip is missing at {UnityCrawlForwardClipAssetPath}.");
            }

            var loopSettings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!loopSettings.loopTime)
            {
                throw new InvalidOperationException($"{UnityCrawlForwardClipAssetPath} must be a loop clip.");
            }

            var rootPositionCurveRange =
                GetCurveRange(clip, string.Empty, typeof(Transform), "m_LocalPosition.x") +
                GetCurveRange(clip, string.Empty, typeof(Transform), "m_LocalPosition.y") +
                GetCurveRange(clip, string.Empty, typeof(Transform), "m_LocalPosition.z");
            if (rootPositionCurveRange > 0.001f)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} crawl motion must not keyframe the review root position. Range={rootPositionCurveRange:0.###}.");
            }

            var modelPath = GetRelativePath(crawlSlot, modelObject);
            var bodyForeAftRange = GetCurveRange(clip, modelPath, typeof(Transform), "m_LocalPosition.z");
            var bodyScaleRange = GetCurveRange(clip, modelPath, typeof(Transform), "m_LocalScale.z");
            var chainFollowerRange = GetCrawlChainFollowerCurveRange(clip, crawlSlot);
            var maceFollowerRange = GetCrawlMaceFollowerCurveRange(clip, crawlSlot);
            var chainPhysicsSummary = InspectCrawlChainPhysicsRig(crawlSlot);

            if (bodyForeAftRange < CrawlForwardBodyForeAftMinimumRange)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} crawl body fore-aft motion is too small. Range={bodyForeAftRange:0.###}.");
            }

            if (bodyScaleRange < CrawlForwardBodyScaleMinimumRange)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} crawl body scale motion is too small. Range={bodyScaleRange:0.###}.");
            }

            if (chainFollowerRange > CrawlForwardPhysicsDrivenCurveMaximumRange)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} chain links must be physics-driven, not Animator-driven. CurveRange={chainFollowerRange:0.###}.");
            }

            if (maceFollowerRange > CrawlForwardPhysicsDrivenCurveMaximumRange)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} mace must be physics-driven, not Animator-driven. CurveRange={maceFollowerRange:0.###}.");
            }

            Debug.Log(
                "AccelerandoCrawlForwardInspection " +
                $"Slot={CrawlSlotObjectName}, Clip={UnityCrawlForwardClipAssetPath}, ClipLength={clip.length:0.###}, " +
                $"Loop={loopSettings.loopTime}, Controller={controllerPath}, AnimatorSpeed={animator.speed:0.###}, ApplyRootMotion={animator.applyRootMotion}, " +
                $"RootPositionCurveRange={rootPositionCurveRange:0.###}, BodyForeAftRange={bodyForeAftRange:0.###}, " +
                $"BodyScaleRange={bodyScaleRange:0.###}, ChainFollowerRange={chainFollowerRange:0.###}, " +
                $"MaceFollowerRange={maceFollowerRange:0.###}, ChainPhysics={chainPhysicsSummary}.");
        }

        private static void InspectPhysicsAntennaStrikeMotion(Transform placementRoot)
        {
            var strikeSlot = placementRoot.Find(AntennaStrikeSlotObjectName);
            if (strikeSlot == null)
            {
                throw new InvalidOperationException($"{AntennaStrikeSlotObjectName} is missing under {PlacementRootName}.");
            }

            var modelObject = strikeSlot.Find(ModelChildName);
            if (modelObject == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {AntennaStrikeSlotObjectName}.");
            }

            var animator = strikeSlot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException($"{AntennaStrikeSlotObjectName} does not have an Animator with a controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException($"{AntennaStrikeSlotObjectName} attack motion must not use root motion.");
            }

            var controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
            if (!string.Equals(controllerPath, UnityAntennaStrikeControllerAssetPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{AntennaStrikeSlotObjectName} uses the wrong antenna strike controller. Expected={UnityAntennaStrikeControllerAssetPath}, Actual={controllerPath}.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAntennaStrikeClipAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Antenna strike clip is missing at {UnityAntennaStrikeClipAssetPath}.");
            }

            var loopSettings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!loopSettings.loopTime)
            {
                throw new InvalidOperationException($"{UnityAntennaStrikeClipAssetPath} must be a loop clip for review.");
            }

            var rootPositionCurveRange =
                GetCurveRange(clip, string.Empty, typeof(Transform), "m_LocalPosition.x") +
                GetCurveRange(clip, string.Empty, typeof(Transform), "m_LocalPosition.y") +
                GetCurveRange(clip, string.Empty, typeof(Transform), "m_LocalPosition.z");
            if (rootPositionCurveRange > 0.001f)
            {
                throw new InvalidOperationException(
                    $"{AntennaStrikeSlotObjectName} attack motion must not keyframe the review root position. Range={rootPositionCurveRange:0.###}.");
            }

            var modelPath = GetRelativePath(strikeSlot, modelObject);
            var bodyForeAftRange = GetCurveRange(clip, modelPath, typeof(Transform), "m_LocalPosition.z");
            var bodyScaleRange = GetCurveRange(clip, modelPath, typeof(Transform), "m_LocalScale.z");
            var antennaInputRange = GetAntennaStrikeInputCurveRange(clip, strikeSlot);
            var chainFollowerRange = GetCrawlChainFollowerCurveRange(clip, strikeSlot);
            var maceFollowerRange = GetCrawlMaceFollowerCurveRange(clip, strikeSlot);
            var chainPhysicsSummary = InspectCrawlChainPhysicsRig(strikeSlot, 0.065f);
            RequireMaceSocketRingRenderersHidden(strikeSlot);

            if (bodyForeAftRange < CrawlForwardBodyForeAftMinimumRange)
            {
                throw new InvalidOperationException(
                    $"{AntennaStrikeSlotObjectName} attack must include crawl body fore-aft motion. Range={bodyForeAftRange:0.###}.");
            }

            if (bodyScaleRange < CrawlForwardBodyScaleMinimumRange)
            {
                throw new InvalidOperationException(
                    $"{AntennaStrikeSlotObjectName} attack must include crawl body scale motion. Range={bodyScaleRange:0.###}.");
            }

            if (antennaInputRange < AntennaStrikeInputMinimumRange)
            {
                throw new InvalidOperationException(
                    $"{AntennaStrikeSlotObjectName} antenna strike input is too small. Range={antennaInputRange:0.###}, Minimum={AntennaStrikeInputMinimumRange:0.###}.");
            }

            if (chainFollowerRange > CrawlForwardPhysicsDrivenCurveMaximumRange)
            {
                throw new InvalidOperationException(
                    $"{AntennaStrikeSlotObjectName} chain links must be physics-driven, not Animator-driven. CurveRange={chainFollowerRange:0.###}.");
            }

            if (maceFollowerRange > CrawlForwardPhysicsDrivenCurveMaximumRange)
            {
                throw new InvalidOperationException(
                    $"{AntennaStrikeSlotObjectName} mace must be physics-driven, not Animator-driven. CurveRange={maceFollowerRange:0.###}.");
            }

            Debug.Log(
                "AccelerandoPhysicsAntennaStrikeInspection " +
                $"Slot={AntennaStrikeSlotObjectName}, Clip={UnityAntennaStrikeClipAssetPath}, ClipLength={clip.length:0.###}, " +
                $"Loop={loopSettings.loopTime}, Controller={controllerPath}, AnimatorSpeed={animator.speed:0.###}, ApplyRootMotion={animator.applyRootMotion}, " +
                $"RootPositionCurveRange={rootPositionCurveRange:0.###}, BodyForeAftRange={bodyForeAftRange:0.###}, " +
                $"BodyScaleRange={bodyScaleRange:0.###}, AntennaInputRange={antennaInputRange:0.###}, " +
                $"ChainFollowerRange={chainFollowerRange:0.###}, MaceFollowerRange={maceFollowerRange:0.###}, ChainPhysics={chainPhysicsSummary}.");
        }

        private static float GetAntennaStrikeInputCurveRange(AnimationClip clip, Transform strikeSlot)
        {
            var range = 0f;
            foreach (var sideName in new[] { "Left", "Right" })
            {
                var antennaTip = RequireNamedChild(strikeSlot, $"Accelerando_{sideName}_AntennaTip_Ring");
                var path = GetRelativePath(strikeSlot, antennaTip);
                range = Mathf.Max(range, GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.z"));
            }

            return range;
        }

        private static float GetCrawlChainFollowerCurveRange(AnimationClip clip, Transform crawlSlot)
        {
            var curveRange = 0f;
            foreach (var sideName in new[] { "Left", "Right" })
            {
                for (var i = 2; i <= ConnectedChainVisibleLinkCount; i++)
                {
                    var link = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}");
                    var path = GetRelativePath(crawlSlot, link);
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.x");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.y");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.z");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "localEulerAnglesRaw.x");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "localEulerAnglesRaw.z");
                }
            }

            return curveRange;
        }

        private static float GetCrawlMaceFollowerCurveRange(AnimationClip clip, Transform crawlSlot)
        {
            var curveRange = 0f;
            foreach (var sideName in new[] { "Left", "Right" })
            {
                var maceTransforms = FindCrawlMaceTransforms(crawlSlot, sideName);
                for (var i = 0; i < maceTransforms.Count; i++)
                {
                    var path = GetRelativePath(crawlSlot, maceTransforms[i]);
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.x");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.y");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.z");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "localEulerAnglesRaw.x");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "localEulerAnglesRaw.y");
                    curveRange += GetCurveRange(clip, path, typeof(Transform), "localEulerAnglesRaw.z");
                }
            }

            return curveRange;
        }

        private static void ValidateCrawlForwardChainPhysicsResponse(Transform crawlSlot, AnimationClip clip)
        {
            var rigType = FindAccelerandoChainPhysicsRigType();
            var rig = crawlSlot.GetComponent(rigType);
            if (rig == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} is missing AccelerandoChainPhysicsRig.");
            }

            var simulateMethod = rigType.GetMethod("SimulatePhysicsTick", new[] { typeof(float) });
            var syncMethod = rigType.GetMethod("SyncVisualsToPhysics", Type.EmptyTypes);
            if (simulateMethod == null || syncMethod == null)
            {
                throw new InvalidOperationException($"{rigType.FullName} is missing physics validation methods.");
            }

            var physicsRoot = crawlSlot.Find(ChainPhysicsRootObjectName);
            if (physicsRoot == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} is missing {ChainPhysicsRootObjectName}.");
            }

            var leftMetrics = new CrawlChainPhysicsResponseMetrics(
                "Left",
                RequireNamedChild(crawlSlot, "Accelerando_Left_AntennaTip_Ring"),
                RequireNamedChild(physicsRoot, ChainPhysicsMaceProxyName("Left")),
                RequireNamedChild(physicsRoot, ChainPhysicsLinkProxyName("Left", ConnectedChainVisibleLinkCount)),
                FindCrawlMaceTransforms(crawlSlot, "Left").ToArray(),
                GetVisibleChainLinkTransforms(crawlSlot, "Left"));
            var rightMetrics = new CrawlChainPhysicsResponseMetrics(
                "Right",
                RequireNamedChild(crawlSlot, "Accelerando_Right_AntennaTip_Ring"),
                RequireNamedChild(physicsRoot, ChainPhysicsMaceProxyName("Right")),
                RequireNamedChild(physicsRoot, ChainPhysicsLinkProxyName("Right", ConnectedChainVisibleLinkCount)),
                FindCrawlMaceTransforms(crawlSlot, "Right").ToArray(),
                GetVisibleChainLinkTransforms(crawlSlot, "Right"));

            var visualSnapshots = CaptureCrawlChainMaceVisualSnapshots(crawlSlot);
            var previousMode = Physics.simulationMode;
            var previousAutoSync = Physics.autoSyncTransforms;
            const float deltaTime = 1f / 60f;
            const int steps = 144;
            try
            {
                try
                {
                    Physics.simulationMode = SimulationMode.Script;
                    Physics.autoSyncTransforms = false;
                    for (var step = 0; step < steps; step++)
                    {
                        var sampleTime = (step * deltaTime) % clip.length;
                        clip.SampleAnimation(crawlSlot.gameObject, sampleTime);
                        simulateMethod.Invoke(rig, new object[] { deltaTime });
                        Physics.SyncTransforms();
                        Physics.Simulate(deltaTime);
                        syncMethod.Invoke(rig, Array.Empty<object>());

                        leftMetrics.Sample();
                        rightMetrics.Sample();
                    }
                }
                finally
                {
                    Physics.simulationMode = previousMode;
                    Physics.autoSyncTransforms = previousAutoSync;
                }

                leftMetrics.Validate();
                rightMetrics.Validate();
                Debug.Log($"AccelerandoCrawlChainPhysicsResponse {leftMetrics}");
                Debug.Log($"AccelerandoCrawlChainPhysicsResponse {rightMetrics}");
            }
            finally
            {
                RestoreTransformSnapshots(visualSnapshots);
                clip.SampleAnimation(crawlSlot.gameObject, 0f);
                ConfigureCrawlChainPhysicsRig(crawlSlot);
            }
        }

        private static void ValidatePhysicsAntennaStrikeResponse(Transform strikeSlot, AnimationClip clip)
        {
            var rigType = FindAccelerandoChainPhysicsRigType();
            var rig = strikeSlot.GetComponent(rigType);
            if (rig == null)
            {
                throw new InvalidOperationException($"{AntennaStrikeSlotObjectName} is missing AccelerandoChainPhysicsRig.");
            }

            var simulateMethod = rigType.GetMethod("SimulatePhysicsTick", new[] { typeof(float) });
            var syncMethod = rigType.GetMethod("SyncVisualsToPhysics", Type.EmptyTypes);
            if (simulateMethod == null || syncMethod == null)
            {
                throw new InvalidOperationException($"{rigType.FullName} is missing physics validation methods.");
            }

            var physicsRoot = strikeSlot.Find(ChainPhysicsRootObjectName);
            if (physicsRoot == null)
            {
                throw new InvalidOperationException($"{AntennaStrikeSlotObjectName} is missing {ChainPhysicsRootObjectName}.");
            }

            var leftMetrics = new AntennaStrikePhysicsResponseMetrics(
                "Left",
                strikeSlot,
                RequireNamedChild(strikeSlot, "Accelerando_Left_AntennaTip_Ring"),
                RequireNamedChild(physicsRoot, ChainPhysicsMaceProxyName("Left")));
            var rightMetrics = new AntennaStrikePhysicsResponseMetrics(
                "Right",
                strikeSlot,
                RequireNamedChild(strikeSlot, "Accelerando_Right_AntennaTip_Ring"),
                RequireNamedChild(physicsRoot, ChainPhysicsMaceProxyName("Right")));

            var visualSnapshots = CaptureAntennaStrikeVisualSnapshots(strikeSlot);
            var previousMode = Physics.simulationMode;
            var previousAutoSync = Physics.autoSyncTransforms;
            const float deltaTime = 1f / 90f;
            const int steps = 180;
            try
            {
                try
                {
                    Physics.simulationMode = SimulationMode.Script;
                    Physics.autoSyncTransforms = false;
                    for (var step = 0; step < steps; step++)
                    {
                        var sampleTime = (step * deltaTime) % clip.length;
                        clip.SampleAnimation(strikeSlot.gameObject, sampleTime);
                        simulateMethod.Invoke(rig, new object[] { deltaTime });
                        Physics.SyncTransforms();
                        Physics.Simulate(deltaTime);
                        syncMethod.Invoke(rig, Array.Empty<object>());

                        leftMetrics.Sample();
                        rightMetrics.Sample();
                    }
                }
                finally
                {
                    Physics.simulationMode = previousMode;
                    Physics.autoSyncTransforms = previousAutoSync;
                }

                leftMetrics.Validate();
                rightMetrics.Validate();
                Debug.Log($"AccelerandoPhysicsAntennaStrikeResponse {leftMetrics}");
                Debug.Log($"AccelerandoPhysicsAntennaStrikeResponse {rightMetrics}");
            }
            finally
            {
                RestoreTransformSnapshots(visualSnapshots);
                clip.SampleAnimation(strikeSlot.gameObject, 0f);
                ConfigureAttackChainPhysicsRig(strikeSlot);
            }
        }

        private static List<TransformSnapshot> CaptureCrawlChainMaceVisualSnapshots(Transform crawlSlot)
        {
            var snapshots = new List<TransformSnapshot>();
            foreach (var sideName in new[] { "Left", "Right" })
            {
                for (var i = 1; i <= ConnectedChainLinkCount; i++)
                {
                    snapshots.Add(new TransformSnapshot(
                        RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}")));
                }

                snapshots.Add(new TransformSnapshot(RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_MaceSocket_Ring")));
            }

            return snapshots;
        }

        private static List<TransformSnapshot> CaptureAntennaStrikeVisualSnapshots(Transform strikeSlot)
        {
            var snapshots = CaptureCrawlChainMaceVisualSnapshots(strikeSlot);
            var modelObject = strikeSlot.Find(ModelChildName);
            if (modelObject != null)
            {
                snapshots.Add(new TransformSnapshot(modelObject));
            }

            foreach (var sideName in new[] { "Left", "Right" })
            {
                var drivenTransforms = FindAntennaStrikeDrivenTransforms(strikeSlot, sideName);
                for (var i = 0; i < drivenTransforms.Count; i++)
                {
                    snapshots.Add(new TransformSnapshot(drivenTransforms[i]));
                }
            }

            return snapshots;
        }

        private static Transform[] GetVisibleChainLinkTransforms(Transform crawlSlot, string sideName)
        {
            var links = new Transform[ConnectedChainVisibleLinkCount];
            for (var i = 1; i <= ConnectedChainVisibleLinkCount; i++)
            {
                links[i - 1] = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}");
            }

            return links;
        }

        private static void RestoreTransformSnapshots(List<TransformSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                snapshots[i].Restore();
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly bool activeSelf;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
                activeSelf = transform.gameObject.activeSelf;
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
                transform.gameObject.SetActive(activeSelf);
            }
        }

        private sealed class AntennaStrikePhysicsResponseMetrics
        {
            private readonly string sideName;
            private readonly Transform strikeSlot;
            private readonly Transform antennaTip;
            private readonly Transform maceProxy;
            private readonly float restAntennaForward;
            private readonly float restMaceForward;

            private float minAntennaForward;
            private float maxAntennaForward;
            private float minMaceForward;
            private float maxMaceForward;
            private int sampleCount;

            public AntennaStrikePhysicsResponseMetrics(
                string sideName,
                Transform strikeSlot,
                Transform antennaTip,
                Transform maceProxy)
            {
                this.sideName = sideName;
                this.strikeSlot = strikeSlot;
                this.antennaTip = antennaTip;
                this.maceProxy = maceProxy;
                var forward = strikeSlot.forward;
                restAntennaForward = Vector3.Dot(antennaTip.position, forward);
                restMaceForward = Vector3.Dot(maceProxy.position, forward);
                minAntennaForward = maxAntennaForward = restAntennaForward;
                minMaceForward = maxMaceForward = restMaceForward;
            }

            public void Sample()
            {
                sampleCount++;
                var forward = strikeSlot.forward;
                var antennaForward = Vector3.Dot(antennaTip.position, forward);
                var maceForward = Vector3.Dot(maceProxy.position, forward);
                minAntennaForward = Mathf.Min(minAntennaForward, antennaForward);
                maxAntennaForward = Mathf.Max(maxAntennaForward, antennaForward);
                minMaceForward = Mathf.Min(minMaceForward, maceForward);
                maxMaceForward = Mathf.Max(maxMaceForward, maceForward);
            }

            public void Validate()
            {
                if (sampleCount <= 0)
                {
                    throw new InvalidOperationException($"{AntennaStrikeSlotObjectName} {sideName} antenna strike physics did not produce samples.");
                }

                if (AntennaForwardRange < AntennaStrikeInputMinimumRange)
                {
                    throw new InvalidOperationException(
                        $"{AntennaStrikeSlotObjectName} {sideName} antenna strike input range is too small. " +
                        $"AntennaForwardRange={AntennaForwardRange:0.###}, Minimum={AntennaStrikeInputMinimumRange:0.###}.");
                }

                if (MaceForwardRange < AntennaStrikeMinimumMaceForwardRange)
                {
                    throw new InvalidOperationException(
                        $"{AntennaStrikeSlotObjectName} {sideName} mace forward physics swing is too small. " +
                        $"MaceForwardRange={MaceForwardRange:0.###}, Minimum={AntennaStrikeMinimumMaceForwardRange:0.###}.");
                }

                if (MaceForwardRange < AntennaForwardRange * AntennaStrikeMinimumMaceAmplification)
                {
                    throw new InvalidOperationException(
                        $"{AntennaStrikeSlotObjectName} {sideName} mace swing must amplify the antenna input. " +
                        $"AntennaForwardRange={AntennaForwardRange:0.###}, MaceForwardRange={MaceForwardRange:0.###}, " +
                        $"MinimumAmplification={AntennaStrikeMinimumMaceAmplification:0.###}.");
                }

                if (MaxMaceForwardOffset <= MaxAntennaForwardOffset)
                {
                    throw new InvalidOperationException(
                        $"{AntennaStrikeSlotObjectName} {sideName} mace must overshoot forward beyond the antenna input. " +
                        $"AntennaForwardOffset={MaxAntennaForwardOffset:0.###}, MaceForwardOffset={MaxMaceForwardOffset:0.###}.");
                }
            }

            private float AntennaForwardRange => maxAntennaForward - minAntennaForward;

            private float MaceForwardRange => maxMaceForward - minMaceForward;

            private float MaxAntennaForwardOffset => maxAntennaForward - restAntennaForward;

            private float MaxMaceForwardOffset => maxMaceForward - restMaceForward;

            public override string ToString()
            {
                return
                    $"Side={sideName}, Samples={sampleCount}, " +
                    $"AntennaForwardRange={AntennaForwardRange:0.###}, MaceForwardRange={MaceForwardRange:0.###}, " +
                    $"MaxAntennaForwardOffset={MaxAntennaForwardOffset:0.###}, MaxMaceForwardOffset={MaxMaceForwardOffset:0.###}, " +
                    $"Amplification={(AntennaForwardRange > 0.0001f ? MaceForwardRange / AntennaForwardRange : 0f):0.###}.";
            }
        }

        private sealed class CrawlChainPhysicsResponseMetrics
        {
            private readonly string sideName;
            private readonly Transform antennaTip;
            private readonly Transform maceProxy;
            private readonly Transform lastLinkProxy;
            private readonly Transform[] maceVisuals;
            private readonly Transform[] visibleChainLinks;
            private readonly float restMaceY;
            private readonly float restMaceX;
            private readonly float restMaceZ;
            private readonly float restLastLinkY;

            private float minMaceX;
            private float maxMaceX;
            private float minMaceZ;
            private float maxMaceZ;
            private float minMaceY;
            private float maxMaceDrop;
            private float dropSum;
            private float maxLastLinkDrop;
            private float minLastLinkMaceVerticalOffset = float.PositiveInfinity;
            private float minMaceBoundsClearance = float.PositiveInfinity;
            private float minMaceTopClearance = float.PositiveInfinity;
            private int sampleCount;

            public CrawlChainPhysicsResponseMetrics(
                string sideName,
                Transform antennaTip,
                Transform maceProxy,
                Transform lastLinkProxy,
                Transform[] maceVisuals,
                Transform[] visibleChainLinks)
            {
                this.sideName = sideName;
                this.antennaTip = antennaTip;
                this.maceProxy = maceProxy;
                this.lastLinkProxy = lastLinkProxy;
                this.maceVisuals = maceVisuals;
                this.visibleChainLinks = visibleChainLinks;
                restMaceY = maceProxy.position.y;
                restMaceX = maceProxy.position.x;
                restMaceZ = maceProxy.position.z;
                restLastLinkY = lastLinkProxy.position.y;
                minMaceX = maxMaceX = restMaceX;
                minMaceZ = maxMaceZ = restMaceZ;
                minMaceY = restMaceY;
            }

            public void Sample()
            {
                sampleCount++;
                var macePosition = maceProxy.position;
                minMaceX = Mathf.Min(minMaceX, macePosition.x);
                maxMaceX = Mathf.Max(maxMaceX, macePosition.x);
                minMaceZ = Mathf.Min(minMaceZ, macePosition.z);
                maxMaceZ = Mathf.Max(maxMaceZ, macePosition.z);
                minMaceY = Mathf.Min(minMaceY, macePosition.y);

                var maceDrop = Mathf.Max(0f, restMaceY - macePosition.y);
                maxMaceDrop = Mathf.Max(maxMaceDrop, maceDrop);
                dropSum += maceDrop;

                var lastLinkDrop = Mathf.Max(0f, restLastLinkY - lastLinkProxy.position.y);
                maxLastLinkDrop = Mathf.Max(maxLastLinkDrop, lastLinkDrop);
                minLastLinkMaceVerticalOffset = Mathf.Min(
                    minLastLinkMaceVerticalOffset,
                    lastLinkProxy.position.y - macePosition.y);
                minMaceBoundsClearance = Mathf.Min(minMaceBoundsClearance, CalculateCurrentMaceBoundsClearance());
                minMaceTopClearance = Mathf.Min(minMaceTopClearance, CalculateCurrentMaceTopClearance());
            }

            public void Validate()
            {
                if (sampleCount <= 0)
                {
                    throw new InvalidOperationException($"{CrawlSlotObjectName} {sideName} chain physics did not produce samples.");
                }

                if (MaxMaceDrop > CrawlChainPhysicsMaximumMaceDrop)
                {
                    throw new InvalidOperationException(
                        $"{CrawlSlotObjectName} {sideName} mace drops too far during crawl physics. " +
                        $"Drop={MaxMaceDrop:0.###}, Limit={CrawlChainPhysicsMaximumMaceDrop:0.###}.");
                }

                if (AverageMaceDrop < CrawlChainPhysicsMinimumGravitySag)
                {
                    throw new InvalidOperationException(
                        $"{CrawlSlotObjectName} {sideName} mace does not show enough gravity sag. " +
                        $"AverageDrop={AverageMaceDrop:0.###}, Minimum={CrawlChainPhysicsMinimumGravitySag:0.###}.");
                }

                if (MaceHorizontalRange < CrawlChainPhysicsMinimumMaceHorizontalRange)
                {
                    throw new InvalidOperationException(
                        $"{CrawlSlotObjectName} {sideName} mace does not respond enough to crawl motion. " +
                        $"HorizontalRange={MaceHorizontalRange:0.###}, Minimum={CrawlChainPhysicsMinimumMaceHorizontalRange:0.###}.");
                }

                if (minLastLinkMaceVerticalOffset < CrawlChainPhysicsMinimumLastLinkMaceVerticalOffset)
                {
                    throw new InvalidOperationException(
                        $"{CrawlSlotObjectName} {sideName} chain last link gets too low relative to the mace. " +
                        $"VerticalOffset={minLastLinkMaceVerticalOffset:0.###}, Minimum={CrawlChainPhysicsMinimumLastLinkMaceVerticalOffset:0.###}.");
                }

                if (minMaceBoundsClearance < CrawlChainPhysicsMinimumMaceBoundsClearance)
                {
                    throw new InvalidOperationException(
                        $"{CrawlSlotObjectName} {sideName} visible chain renderer bounds pierce the mace renderer bounds. " +
                        $"Clearance={minMaceBoundsClearance:0.###}, Minimum={CrawlChainPhysicsMinimumMaceBoundsClearance:0.###}.");
                }

                if (minMaceTopClearance < CrawlChainPhysicsMinimumMaceTopClearance)
                {
                    throw new InvalidOperationException(
                        $"{CrawlSlotObjectName} {sideName} visible chain renderer drops into the mace top area. " +
                        $"TopClearance={minMaceTopClearance:0.###}, Minimum={CrawlChainPhysicsMinimumMaceTopClearance:0.###}.");
                }

                if (Vector3.Distance(antennaTip.position, lastLinkProxy.position) <= 0.001f)
                {
                    throw new InvalidOperationException($"{CrawlSlotObjectName} {sideName} chain collapsed into the antenna tip.");
                }
            }

            private float CalculateCurrentMaceBoundsClearance()
            {
                var maceBounds = CalculateActiveRendererBounds(
                    maceVisuals,
                    new Bounds(maceProxy.position, Vector3.one * 0.05f));
                var minClearance = float.PositiveInfinity;
                for (var i = 0; i < visibleChainLinks.Length; i++)
                {
                    var chainLink = visibleChainLinks[i];
                    if (chainLink == null || !chainLink.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var chainBounds = CalculateActiveRendererBounds(new[] { chainLink }, new Bounds(chainLink.position, Vector3.one * 0.02f));
                    minClearance = Mathf.Min(minClearance, GetBoundsClearance(chainBounds, maceBounds));
                }

                return float.IsPositiveInfinity(minClearance) ? 0f : minClearance;
            }

            private float CalculateCurrentMaceTopClearance()
            {
                var maceBounds = CalculateActiveRendererBounds(
                    maceVisuals,
                    new Bounds(maceProxy.position, Vector3.one * 0.05f));
                var minClearance = float.PositiveInfinity;
                for (var i = 0; i < visibleChainLinks.Length; i++)
                {
                    var chainLink = visibleChainLinks[i];
                    if (chainLink == null || !chainLink.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var chainBounds = CalculateActiveRendererBounds(new[] { chainLink }, new Bounds(chainLink.position, Vector3.one * 0.02f));
                    minClearance = Mathf.Min(minClearance, chainBounds.min.y - maceBounds.max.y);
                }

                return float.IsPositiveInfinity(minClearance) ? 0f : minClearance;
            }

            private float MaceHorizontalRange =>
                Mathf.Max(maxMaceX - minMaceX, maxMaceZ - minMaceZ);

            private float MaxMaceDrop => maxMaceDrop;

            private float AverageMaceDrop => dropSum / Mathf.Max(1, sampleCount);

            public override string ToString()
            {
                return
                    $"Side={sideName}, Samples={sampleCount}, " +
                    $"MaceHorizontalRange={MaceHorizontalRange:0.###}, " +
                    $"MaxMaceDrop={MaxMaceDrop:0.###}, AverageMaceDrop={AverageMaceDrop:0.###}, " +
                    $"MinMaceY={minMaceY:0.###}, MaxLastLinkDrop={maxLastLinkDrop:0.###}, " +
                    $"MinLastLinkMaceVerticalOffset={minLastLinkMaceVerticalOffset:0.###}, " +
                    $"MinMaceBoundsClearance={minMaceBoundsClearance:0.###}, " +
                    $"MinMaceTopClearance={minMaceTopClearance:0.###}.";
            }
        }

        private static string InspectCrawlChainPhysicsRig(Transform crawlSlot, float maximumJointLimit = 0.030f)
        {
            var rigType = FindAccelerandoChainPhysicsRigType();
            var rig = crawlSlot.GetComponent(rigType);
            if (rig == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} is missing AccelerandoChainPhysicsRig.");
            }

            var visibleLinkCountProperty = rigType.GetProperty("VisibleLinkCount");
            if (visibleLinkCountProperty == null)
            {
                throw new InvalidOperationException($"{rigType.FullName} is missing VisibleLinkCount.");
            }

            var actualVisibleLinkCount = (int)visibleLinkCountProperty.GetValue(rig);
            if (actualVisibleLinkCount != ConnectedChainVisibleLinkCount)
            {
                throw new InvalidOperationException(
                    $"AccelerandoChainPhysicsRig visible link count mismatch. " +
                    $"Expected={ConnectedChainVisibleLinkCount}, Actual={actualVisibleLinkCount}.");
            }

            var summary = new System.Text.StringBuilder();
            InspectCrawlChainPhysicsRigForSide(crawlSlot, "Left", summary, maximumJointLimit);
            InspectCrawlChainPhysicsRigForSide(crawlSlot, "Right", summary, maximumJointLimit);
            return summary.ToString();
        }

        private static void InspectCrawlChainPhysicsRigForSide(Transform crawlSlot, string sideName, System.Text.StringBuilder summary, float maximumJointLimit)
        {
            var physicsRoot = crawlSlot.Find(ChainPhysicsRootObjectName);
            if (physicsRoot == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} is missing {ChainPhysicsRootObjectName}.");
            }

            var antennaTip = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_AntennaTip_Ring");
            var firstLink = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_01");
            var firstProxy = RequireNamedChild(physicsRoot, ChainPhysicsLinkProxyName(sideName, 1));
            var hiddenMaceSideLink = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_{ConnectedChainLinkCount:00}");
            var firstBody = RequireRigidbody(firstProxy, $"{sideName} chain first link physics proxy");
            RequireCollider(firstProxy, $"{sideName} chain first link physics proxy");
            RequireChildOf(firstProxy, physicsRoot, $"{sideName} chain first link physics proxy");
            RequireVisualHasNoPhysics(firstLink, $"{sideName} chain first link visual");
            RequireVisualFollowsPhysicsProxy(firstLink, firstProxy, $"{sideName} chain first link");

            if (!firstBody.isKinematic)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {sideName} first chain physics proxy must be kinematic.");
            }

            if (firstProxy.GetComponent<ConfigurableJoint>() != null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {sideName} first chain physics proxy must not have a joint.");
            }

            if (Vector3.Distance(firstProxy.position, antennaTip.position) > ConnectedChainEndpointTolerance)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} {sideName} first chain physics proxy detached from antenna tip. " +
                    $"Distance={Vector3.Distance(firstProxy.position, antennaTip.position):0.###}.");
            }

            if (hiddenMaceSideLink.gameObject.activeSelf)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {sideName} hidden mace-side chain link is active.");
            }

            var previousBody = firstBody;
            var dynamicBodyCount = 0;
            var jointCount = 0;
            for (var i = 2; i <= ConnectedChainVisibleLinkCount; i++)
            {
                var link = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}");
                var proxy = RequireNamedChild(physicsRoot, ChainPhysicsLinkProxyName(sideName, i));
                if (!link.gameObject.activeInHierarchy)
                {
                    throw new InvalidOperationException($"{CrawlSlotObjectName} {sideName} visible chain link {i:00} is inactive.");
                }

                var body = RequireRigidbody(proxy, $"{sideName} chain link {i:00} physics proxy");
                RequireCollider(proxy, $"{sideName} chain link {i:00} physics proxy");
                RequireChildOf(proxy, physicsRoot, $"{sideName} chain link {i:00} physics proxy");
                RequireVisualHasNoPhysics(link, $"{sideName} chain link {i:00} visual");
                RequireVisualFollowsPhysicsProxy(link, proxy, $"{sideName} chain link {i:00}");
                if (body.isKinematic)
                {
                    throw new InvalidOperationException($"{CrawlSlotObjectName} {sideName} chain link {i:00} physics proxy must be dynamic.");
                }

                var joint = RequireConfigurableJoint(proxy, $"{sideName} chain link {i:00} physics proxy");
                InspectPhysicsJointConnection(joint, previousBody, $"{sideName} chain link {i:00}", maximumJointLimit);
                previousBody = body;
                dynamicBodyCount++;
                jointCount++;
            }

            var mace = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_MaceSocket_Ring");
            var maceProxy = RequireNamedChild(physicsRoot, ChainPhysicsMaceProxyName(sideName));
            var maceBody = RequireRigidbody(maceProxy, $"{sideName} mace physics proxy");
            RequireCollider(maceProxy, $"{sideName} mace physics proxy");
            RequireChildOf(maceProxy, physicsRoot, $"{sideName} mace physics proxy");
            RequireVisualHasNoPhysics(mace, $"{sideName} mace visual");
            RequireVisualFollowsPhysicsProxy(mace, maceProxy, $"{sideName} mace");
            if (maceBody.isKinematic)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {sideName} mace physics proxy must be dynamic.");
            }

            var maceJoint = RequireConfigurableJoint(maceProxy, $"{sideName} mace physics proxy");
            InspectPhysicsJointConnection(maceJoint, previousBody, $"{sideName} mace", maximumJointLimit);
            dynamicBodyCount++;
            jointCount++;

            if (summary.Length > 0)
            {
                summary.Append("; ");
            }

            summary.Append(sideName);
            summary.Append($"=physicsRoot:True,visibleLinks:{ConnectedChainVisibleLinkCount},dynamicBodies:{dynamicBodyCount},joints:{jointCount},hiddenMaceSide:True");
        }

        private static string ChainPhysicsLinkProxyName(string sideName, int linkIndex)
        {
            return $"Accelerando_{sideName}_ChainPhysics_Link_{linkIndex:00}";
        }

        private static string ChainPhysicsMaceProxyName(string sideName)
        {
            return $"Accelerando_{sideName}_ChainPhysics_MaceSocket";
        }

        private static void RequireChildOf(Transform transform, Transform parent, string label)
        {
            if (!transform.IsChildOf(parent))
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} {label} must be under {parent.name} for physics-driven motion.");
            }
        }

        private static void RequireVisualHasNoPhysics(Transform visual, string label)
        {
            if (visual.GetComponent<Rigidbody>() != null ||
                visual.GetComponent<ConfigurableJoint>() != null ||
                visual.GetComponent<Collider>() != null)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} {label} must not keep direct physics components; physics must live on the proxy.");
            }
        }

        private static void RequireVisualFollowsPhysicsProxy(Transform visual, Transform proxy, string label)
        {
            var distance = Vector3.Distance(visual.position, proxy.position);
            if (distance > ConnectedChainEndpointTolerance)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} {label} visual does not match its physics proxy. Distance={distance:0.###}.");
            }
        }

        private static Rigidbody RequireRigidbody(Transform transform, string label)
        {
            var body = transform.GetComponent<Rigidbody>();
            if (body == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {label} is missing Rigidbody.");
            }

            return body;
        }

        private static Collider RequireCollider(Transform transform, string label)
        {
            var collider = transform.GetComponent<Collider>();
            if (collider == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {label} is missing Collider.");
            }

            return collider;
        }

        private static ConfigurableJoint RequireConfigurableJoint(Transform transform, string label)
        {
            var joint = transform.GetComponent<ConfigurableJoint>();
            if (joint == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {label} is missing ConfigurableJoint.");
            }

            return joint;
        }

        private static void InspectPhysicsJointConnection(ConfigurableJoint joint, Rigidbody expectedConnectedBody, string label, float maximumJointLimit)
        {
            if (joint.connectedBody != expectedConnectedBody)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {label} joint is connected to the wrong body.");
            }

            if (joint.xMotion != ConfigurableJointMotion.Limited ||
                joint.yMotion != ConfigurableJointMotion.Limited ||
                joint.zMotion != ConfigurableJointMotion.Limited)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} {label} joint must use limited linear motion.");
            }

            if (joint.linearLimit.limit > maximumJointLimit)
            {
                throw new InvalidOperationException(
                    $"{CrawlSlotObjectName} {label} joint limit is too loose. Limit={joint.linearLimit.limit:0.###}, Maximum={maximumJointLimit:0.###}.");
            }
        }

        private static string GetCrawlBodyDrivenPhysicsSampleReport(
            AnimationClip clip,
            Transform crawlSlot,
            Transform modelObject,
            string modelPath,
            out float lagScore)
        {
            var forwardTime = CrawlForwardLoopSeconds * 0.25f;
            var pullbackTime = CrawlForwardLoopSeconds * 0.50f;
            var recoveryTime = CrawlForwardLoopSeconds * 0.75f;

            var forwardBody = GetCurveValueOffset(clip, modelPath, typeof(Transform), "m_LocalPosition.z", modelObject.localPosition.z, forwardTime);
            var pullbackBody = GetCurveValueOffset(clip, modelPath, typeof(Transform), "m_LocalPosition.z", modelObject.localPosition.z, pullbackTime);
            var recoveryBody = GetCurveValueOffset(clip, modelPath, typeof(Transform), "m_LocalPosition.z", modelObject.localPosition.z, recoveryTime);

            var forwardChain = GetAverageCrawlChainEndForeAftOffset(clip, crawlSlot, forwardTime);
            var pullbackChain = GetAverageCrawlChainEndForeAftOffset(clip, crawlSlot, pullbackTime);
            var recoveryChain = GetAverageCrawlChainEndForeAftOffset(clip, crawlSlot, recoveryTime);

            var forwardMace = GetAverageCrawlMaceForeAftOffset(clip, crawlSlot, forwardTime);
            var pullbackMace = GetAverageCrawlMaceForeAftOffset(clip, crawlSlot, pullbackTime);
            var recoveryMace = GetAverageCrawlMaceForeAftOffset(clip, crawlSlot, recoveryTime);

            if (forwardBody <= 0.020f ||
                forwardChain >= -0.004f ||
                forwardMace >= -0.004f ||
                pullbackBody >= -0.012f ||
                pullbackChain >= -0.004f ||
                pullbackMace >= -0.004f ||
                recoveryBody <= 0.012f ||
                recoveryChain <= 0.002f ||
                recoveryMace <= 0.003f)
            {
                lagScore = 0f;
            }
            else
            {
                lagScore =
                    -forwardChain +
                    -forwardMace +
                    -pullbackChain +
                    -pullbackMace +
                    recoveryChain +
                    recoveryMace;
            }

            return
                $"Forward(body={forwardBody:0.###},chain={forwardChain:0.###},mace={forwardMace:0.###});" +
                $"Pullback(body={pullbackBody:0.###},chain={pullbackChain:0.###},mace={pullbackMace:0.###});" +
                $"Recovery(body={recoveryBody:0.###},chain={recoveryChain:0.###},mace={recoveryMace:0.###})";
        }

        private static float GetAverageCrawlChainEndForeAftOffset(AnimationClip clip, Transform crawlSlot, float time)
        {
            var sum = 0f;
            var count = 0;
            foreach (var sideName in new[] { "Left", "Right" })
            {
                var chainEnd = RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_{ConnectedChainVisibleLinkCount:00}");
                var path = GetRelativePath(crawlSlot, chainEnd);
                sum += GetCurveValueOffset(clip, path, typeof(Transform), "m_LocalPosition.z", chainEnd.localPosition.z, time);
                count++;
            }

            return count > 0 ? sum / count : 0f;
        }

        private static float GetAverageCrawlMaceForeAftOffset(AnimationClip clip, Transform crawlSlot, float time)
        {
            var sum = 0f;
            var count = 0;
            foreach (var sideName in new[] { "Left", "Right" })
            {
                var maceTransforms = FindCrawlMaceTransforms(crawlSlot, sideName);
                for (var i = 0; i < maceTransforms.Count; i++)
                {
                    var maceTransform = maceTransforms[i];
                    var path = GetRelativePath(crawlSlot, maceTransform);
                    sum += GetCurveValueOffset(clip, path, typeof(Transform), "m_LocalPosition.z", maceTransform.localPosition.z, time);
                    count++;
                }
            }

            return count > 0 ? sum / count : 0f;
        }

        private static float GetCurveValueOffset(
            AnimationClip clip,
            string path,
            Type type,
            string propertyName,
            float baseValue,
            float time)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName));
            if (curve == null || curve.length == 0)
            {
                throw new InvalidOperationException(
                    $"{UnityCrawlForwardClipAssetPath} is missing a required curve. Path={path}, Property={propertyName}.");
            }

            return curve.Evaluate(time) - baseValue;
        }

        private static SkinnedMeshRenderer FindIdleBreathBodyRenderer(Transform idleSlot)
        {
            var bodyTransform = FindChildByName(idleSlot, IdleBreathBodyObjectName);
            if (bodyTransform != null)
            {
                var namedRenderer = bodyTransform.GetComponent<SkinnedMeshRenderer>();
                if (namedRenderer != null)
                {
                    return namedRenderer;
                }
            }

            foreach (var renderer in idleSlot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh != null && renderer.sharedMesh.GetBlendShapeIndex(IdleBreathBlendShapeName) >= 0)
                {
                    return renderer;
                }
            }

            return null;
        }

        private static MeshRenderer FindIdleBreathStaticAntennaRenderer(Transform idleSlot)
        {
            var antennaTransform = FindChildByName(idleSlot, IdleBreathAntennaObjectName);
            return antennaTransform != null ? antennaTransform.GetComponent<MeshRenderer>() : null;
        }

        private static int CountIdleBreathStaticAccessoryTriangles(Transform idleSlot, Transform bodyTransform)
        {
            var triangleCount = 0;
            foreach (var renderer in idleSlot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.transform == bodyTransform || renderer.transform.IsChildOf(bodyTransform))
                {
                    continue;
                }

                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    triangleCount += CountMeshTriangles(meshFilter.sharedMesh);
                }
            }

            return triangleCount;
        }

        private static float GetIdleBreathStaticAccessoryCurveRange(AnimationClip clip, Transform idleSlot, Transform bodyTransform)
        {
            var curveRange = 0f;
            foreach (var renderer in idleSlot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.transform == bodyTransform || renderer.transform.IsChildOf(bodyTransform))
                {
                    continue;
                }

                var path = GetRelativePath(idleSlot, renderer.transform);
                curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.x");
                curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.y");
                curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalPosition.z");
                curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalScale.x");
                curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalScale.y");
                curveRange += GetCurveRange(clip, path, typeof(Transform), "m_LocalScale.z");
            }

            return curveRange;
        }

        private static int CountMeshTriangles(Mesh mesh)
        {
            if (mesh == null)
            {
                return 0;
            }

            var triangleCount = 0;
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                triangleCount += mesh.GetTriangles(submesh).Length / 3;
            }

            return triangleCount;
        }

        private static float GetCurveRange(AnimationClip clip, string path, Type type, string propertyName)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName));
            if (curve == null || curve.length == 0)
            {
                return 0f;
            }

            var minValue = float.MaxValue;
            var maxValue = float.MinValue;
            for (var i = 0; i < curve.keys.Length; i++)
            {
                minValue = Mathf.Min(minValue, curve.keys[i].value);
                maxValue = Mathf.Max(maxValue, curve.keys[i].value);
            }

            return maxValue - minValue;
        }

        private static Bounds BakeRendererBounds(SkinnedMeshRenderer skinnedRenderer)
        {
            var bakedMesh = new Mesh();
            try
            {
                skinnedRenderer.BakeMesh(bakedMesh);
                return bakedMesh.bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakedMesh);
            }
        }

        private static void InspectMaterialAssignments(Renderer[] renderers)
        {
            var materialSet = EnsureApprovedMaterialSet();

            foreach (var renderer in renderers)
            {
                foreach (var assignedMaterial in renderer.sharedMaterials)
                {
                    if (assignedMaterial == null)
                    {
                        throw new InvalidOperationException($"{renderer.name} has an empty Accelerando material slot.");
                    }

                    if (!materialSet.Contains(assignedMaterial))
                    {
                        throw new InvalidOperationException($"{renderer.name} does not use an approved Accelerando material.");
                    }

                    if (assignedMaterial.shader == null ||
                        string.Equals(assignedMaterial.shader.name, "Hidden/InternalErrorShader", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"{renderer.name} uses a missing or error shader.");
                    }
                }
            }
        }

        private static void InspectConSpiritoRelativeZPlacement(Transform placementRoot)
        {
            var conSpiritoRoot = RequireSceneRoot(ConSpiritoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var spacing = CalculateLongaTergoZSpacing(longaRoot.transform, tergoRoot.transform);
            var expectedZ = conSpiritoRoot.transform.position.z - spacing;
            if (Mathf.Abs(placementRoot.position.x - conSpiritoRoot.transform.position.x) > 0.01f ||
                Mathf.Abs(placementRoot.position.z - expectedZ) > 0.01f)
            {
                throw new InvalidOperationException(
                    $"Accelerando root position must be below Con Spirito on Z by Longa-Tergo spacing. ExpectedX={conSpiritoRoot.transform.position.x:0.###}, ExpectedZ={expectedZ:0.###}, Actual={placementRoot.position}.");
            }
        }

        private static void InspectPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Player start transform is missing.");
            }

            var focus = placementRoot;
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = CalculateLookAt(bounds);
            var playerToLookAt = lookAt - player.position;
            playerToLookAt.y = 0f;
            if (playerToLookAt.sqrMagnitude < 0.001f || Vector3.Dot(player.forward, playerToLookAt.normalized) < 0.95f)
            {
                throw new InvalidOperationException("Player start transform is not facing Accelerando.");
            }

            var frontDirection = CalculateAccelerandoVisualFrontDirection(FindAccelerandoCameraFocus(placementRoot));
            var playerSide = player.position - lookAt;
            playerSide.y = 0f;
            if (playerSide.sqrMagnitude < 0.001f || Vector3.Dot(playerSide.normalized, -frontDirection) < 0.90f)
            {
                throw new InvalidOperationException("Player start transform is not positioned on the opposite side of Accelerando.");
            }
        }

        private static void InspectReviewCamera(Transform placementRoot)
        {
            var camera = FindReviewCamera();
            if (camera == null)
            {
                throw new InvalidOperationException("Accelerando review camera is missing.");
            }

            var focus = placementRoot;
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = CalculateLookAt(bounds);
            var cameraToLookAt = (lookAt - camera.transform.position).normalized;
            if (Vector3.Dot(camera.transform.forward, cameraToLookAt) < 0.985f)
            {
                throw new InvalidOperationException("Accelerando review camera is not facing the model front.");
            }

            var frontDirection = CalculateAccelerandoVisualFrontDirection(FindAccelerandoCameraFocus(placementRoot));
            var cameraSide = camera.transform.position - lookAt;
            cameraSide.y = 0f;
            if (cameraSide.sqrMagnitude < 0.001f || Vector3.Dot(cameraSide.normalized, frontDirection) < 0.90f)
            {
                throw new InvalidOperationException("Accelerando review camera is not positioned on the model front side.");
            }
        }

        private static void CaptureReviewImages(Transform placementRoot)
        {
            var focus = FindAccelerandoCameraFocus(placementRoot);
            CaptureReviewImagesForFocus(
                placementRoot,
                new[] { focus },
                focus,
                "Accelerando_StaticReview_Front.png",
                "Accelerando_StaticReview_Oblique.png",
                "AccelerandoReviewCapture");
        }

        private static void CaptureAnimationSlotReviewImages(Transform placementRoot)
        {
            var slotRoots = FindAlignedReviewTransforms(placementRoot);
            CaptureReviewImagesForFocus(
                placementRoot,
                slotRoots,
                slotRoots[0],
                AnimationSlotsFrontCaptureName,
                AnimationSlotsObliqueCaptureName,
                "AccelerandoAnimationSlotsCapture");
        }

        private static void CaptureAntennaTipConnectionCloseupImages(Transform placementRoot)
        {
            var reviewObject = placementRoot.Find(PlacementObjectName);
            if (reviewObject == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            CaptureAntennaTipConnectionCloseupForSide(reviewObject, "Left", AntennaTipConnectionLeftSideCloseupName);
            CaptureAntennaTipConnectionCloseupForSide(reviewObject, "Right", AntennaTipConnectionRightSideCloseupName);
        }

        private static void CaptureMaceChainCloseupImages(Transform placementRoot)
        {
            var crawlSlot = placementRoot.Find(CrawlSlotObjectName);
            if (crawlSlot == null)
            {
                throw new InvalidOperationException($"{CrawlSlotObjectName} is missing under {PlacementRootName}.");
            }

            CaptureMaceChainCloseupForSide(crawlSlot, "Left", MaceChainLeftCloseupName);
            CaptureMaceChainCloseupForSide(crawlSlot, "Right", MaceChainRightCloseupName);
        }

        private static void CaptureMaceChainCloseupForSide(Transform crawlSlot, string sideName, string fileName)
        {
            var targets = FindMaceChainCloseupTransforms(crawlSlot, sideName);
            var bounds = CalculateActiveRendererBounds(targets, new Bounds(RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_MaceSocket_Ring").position, Vector3.one * 0.35f));
            var outputDirectory = GetAbsoluteProjectPath(ValidationFolder);
            Directory.CreateDirectory(outputDirectory);

            var cameraObject = new GameObject($"Accelerando_{sideName}_MaceChainCloseupCamera");
            var lightObject = new GameObject($"Accelerando_{sideName}_MaceChainCloseupLight");
            var captureLayerTransforms = CollectChildTransforms(new[] { crawlSlot });
            var originalLayers = new int[captureLayerTransforms.Length];
            var previousAmbientMode = RenderSettings.ambientMode;
            var previousAmbientLight = RenderSettings.ambientLight;
            var previousAmbientIntensity = RenderSettings.ambientIntensity;
            try
            {
                for (var i = 0; i < captureLayerTransforms.Length; i++)
                {
                    originalLayers[i] = captureLayerTransforms[i].gameObject.layer;
                    captureLayerTransforms[i].gameObject.layer = ReviewCaptureLayer;
                }

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.47f, 0.50f, 0.50f, 1f);
                RenderSettings.ambientIntensity = 1.25f;

                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.07f, 0.075f, 0.072f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 5.00f;
                camera.cullingMask = 1 << ReviewCaptureLayer;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 3.00f;

                var frontDirection = CalculateAccelerandoVisualFrontDirection(crawlSlot);
                frontDirection.y = 0f;
                frontDirection = frontDirection.sqrMagnitude > 0.001f ? frontDirection.normalized : Vector3.forward;
                var lookAt = bounds.center + Vector3.up * 0.05f;
                var position = lookAt + frontDirection * 1.35f + Vector3.up * 0.03f;
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
                camera.orthographicSize = Mathf.Clamp(Mathf.Max(bounds.extents.y * 1.65f, bounds.extents.x * 1.45f, 0.36f), 0.36f, 0.78f);
                light.transform.rotation = Quaternion.LookRotation((lookAt - (position + Vector3.up * 0.5f)).normalized, Vector3.up);

                var outputPath = Path.Combine(outputDirectory, fileName);
                SaveCameraCapture(camera, outputPath);
                Debug.Log(
                    "AccelerandoMaceChainCloseup " +
                    $"Side={sideName}, Path={outputPath}, BoundsCenter={FormatVector(bounds.center)}, BoundsSize={FormatVector(bounds.size)}, OrthographicSize={camera.orthographicSize:0.###}.");
            }
            finally
            {
                for (var i = 0; i < captureLayerTransforms.Length; i++)
                {
                    if (captureLayerTransforms[i] != null)
                    {
                        captureLayerTransforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.ambientIntensity = previousAmbientIntensity;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Transform[] FindMaceChainCloseupTransforms(Transform crawlSlot, string sideName)
        {
            var transforms = new List<Transform>();
            transforms.AddRange(FindCrawlMaceTransforms(crawlSlot, sideName));
            for (var i = 1; i <= ConnectedChainVisibleLinkCount; i++)
            {
                AddUniqueTransform(transforms, RequireNamedChild(crawlSlot, $"Accelerando_{sideName}_ConnectedChain_Link_{i:00}"));
            }

            return transforms.ToArray();
        }

        private static void CaptureAntennaTipConnectionCloseupForSide(Transform reviewObject, string sideName, string fileName)
        {
            var antennaTip = RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_Ring");
            var connectionTransforms = FindAntennaTipConnectionTransforms(reviewObject, sideName);
            var bounds = CalculateRendererBounds(connectionTransforms, new Bounds(antennaTip.position, Vector3.one * 0.25f));
            var outputDirectory = GetAbsoluteProjectPath(ValidationFolder);
            Directory.CreateDirectory(outputDirectory);

            var cameraObject = new GameObject($"Accelerando_{sideName}_AntennaTipSideCloseupCamera");
            var lightObject = new GameObject($"Accelerando_{sideName}_AntennaTipSideCloseupLight");
            var captureLayerTransforms = CollectChildTransforms(new[] { reviewObject });
            var originalLayers = new int[captureLayerTransforms.Length];
            var previousAmbientMode = RenderSettings.ambientMode;
            var previousAmbientLight = RenderSettings.ambientLight;
            var previousAmbientIntensity = RenderSettings.ambientIntensity;
            try
            {
                for (var i = 0; i < captureLayerTransforms.Length; i++)
                {
                    originalLayers[i] = captureLayerTransforms[i].gameObject.layer;
                    captureLayerTransforms[i].gameObject.layer = ReviewCaptureLayer;
                }

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.46f, 0.48f, 0.48f, 1f);
                RenderSettings.ambientIntensity = 1.25f;

                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.10f, 0.11f, 0.105f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 4.00f;
                camera.cullingMask = 1 << ReviewCaptureLayer;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 3.20f;

                var sideDirection = string.Equals(sideName, "Left", StringComparison.Ordinal) ? -reviewObject.right : reviewObject.right;
                sideDirection.y = 0f;
                sideDirection = sideDirection.sqrMagnitude > 0.001f ? sideDirection.normalized : Vector3.right;
                var lookAt = bounds.center;
                var position = lookAt + sideDirection * 0.95f + Vector3.up * 0.02f;
                camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
                camera.orthographicSize = Mathf.Clamp(Mathf.Max(bounds.extents.y * 2.15f, bounds.extents.z * 2.10f, 0.25f), 0.25f, 0.52f);
                light.transform.rotation = Quaternion.LookRotation((lookAt - (position + Vector3.up * 0.35f)).normalized, Vector3.up);

                var outputPath = Path.Combine(outputDirectory, fileName);
                SaveCameraCapture(camera, outputPath);
                Debug.Log(
                    "AccelerandoAntennaTipConnectionSideCloseup " +
                    $"Side={sideName}, Path={outputPath}, LookAt={FormatVector(lookAt)}, BoundsCenter={FormatVector(bounds.center)}, BoundsSize={FormatVector(bounds.size)}, OrthographicSize={camera.orthographicSize:0.###}.");
            }
            finally
            {
                for (var i = 0; i < captureLayerTransforms.Length; i++)
                {
                    if (captureLayerTransforms[i] != null)
                    {
                        captureLayerTransforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.ambientIntensity = previousAmbientIntensity;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Transform[] FindAntennaTipConnectionTransforms(Transform reviewObject, string sideName)
        {
            return new[]
            {
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_Ring"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_ConnectedChain_Link_01"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_ConnectedChain_Link_02"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedVisibleChainEndHoop"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedSocketLip"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedSurfaceSaddle"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedRetainingPin"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedStem"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedFrontYoke"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedBackYoke"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedUpperOcclusionBulge"),
                RequireNamedChild(reviewObject, $"Accelerando_{sideName}_AntennaTip_EmbeddedFleshPressurePad")
            };
        }

        private static void CaptureIdleBreathingReviewImages(Transform placementRoot)
        {
            var idleSlot = placementRoot.Find(IdleSlotObjectName);
            if (idleSlot == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} is missing under {PlacementRootName}.");
            }

            var bodyRenderer = FindIdleBreathBodyRenderer(idleSlot);
            if (bodyRenderer == null)
            {
                throw new InvalidOperationException($"{IdleSlotObjectName} does not use {IdleBreathBodyObjectName} for body breathing.");
            }

            var animator = idleSlot.GetComponent<Animator>();
            var animatorWasEnabled = animator != null && animator.enabled;
            var originalScale = bodyRenderer.transform.localScale;
            var originalPosition = bodyRenderer.transform.localPosition;
            var blendShapeIndex = GetIdleBreathBlendShapeIndex(bodyRenderer.sharedMesh);
            var originalBlendShapeWeight = bodyRenderer.GetBlendShapeWeight(blendShapeIndex);
            try
            {
                if (animator == null || animator.runtimeAnimatorController == null)
                {
                    throw new InvalidOperationException($"{IdleSlotObjectName} does not have an Animator with a controller.");
                }

                animator.enabled = true;
                SampleIdleBreathAnimatorPose(animator, bodyRenderer, 0f, "000");
                var neutralBounds = CalculateRendererBounds(new[] { idleSlot }, new Bounds(idleSlot.position, Vector3.one));
                CaptureIdleBreathFrame(placementRoot, idleSlot, animator, bodyRenderer, neutralBounds, originalScale, originalPosition, 0f, "000");
                CaptureIdleBreathFrame(placementRoot, idleSlot, animator, bodyRenderer, neutralBounds, originalScale, originalPosition, 0.5f, "050");
                CaptureIdleBreathFrame(placementRoot, idleSlot, animator, bodyRenderer, neutralBounds, originalScale, originalPosition, 1f, "100");
                Debug.Log("AccelerandoIdleBreathCapture Frames=000;050;100.");
            }
            finally
            {
                bodyRenderer.transform.localScale = originalScale;
                bodyRenderer.transform.localPosition = originalPosition;
                bodyRenderer.SetBlendShapeWeight(blendShapeIndex, originalBlendShapeWeight);
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.Play(IdleBreathStateName, 0, 0f);
                    animator.Update(0f);
                }

                if (animator != null)
                {
                    animator.enabled = animatorWasEnabled;
                }
            }
        }

        private static void CaptureIdleBreathFrame(
            Transform placementRoot,
            Transform idleSlot,
            Animator animator,
            SkinnedMeshRenderer bodyRenderer,
            Bounds neutralBounds,
            Vector3 baseScale,
            Vector3 baseLocalPosition,
            float normalizedTime,
            string suffix)
        {
            bodyRenderer.transform.localScale = baseScale;
            bodyRenderer.transform.localPosition = baseLocalPosition;
            SampleIdleBreathAnimatorPose(animator, bodyRenderer, normalizedTime, suffix);
            var rendererWasEnabled = bodyRenderer.enabled;
            var bakedObject = CreateIdleBreathBakedCaptureObject(bodyRenderer, suffix);
            try
            {
                bodyRenderer.enabled = false;
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
                CaptureReviewImagesForFocus(
                    placementRoot,
                    new[] { idleSlot },
                    idleSlot,
                    $"Accelerando_IdleBreath_{suffix}.png",
                    $"Accelerando_IdleBreath_Oblique_{suffix}.png",
                    $"AccelerandoIdleBreathCapture_{suffix}",
                    neutralBounds);
            }
            finally
            {
                bodyRenderer.enabled = rendererWasEnabled;
                if (bakedObject != null)
                {
                    var meshFilter = bakedObject.GetComponent<MeshFilter>();
                    var bakedMesh = meshFilter != null ? meshFilter.sharedMesh : null;
                    UnityEngine.Object.DestroyImmediate(bakedObject);
                    if (bakedMesh != null)
                    {
                        UnityEngine.Object.DestroyImmediate(bakedMesh);
                    }
                }
            }
        }

        private static void SampleIdleBreathAnimatorPose(
            Animator animator,
            SkinnedMeshRenderer renderer,
            float normalizedTime,
            string suffix)
        {
            animator.Play(IdleBreathStateName, 0, normalizedTime);
            animator.Update(0f);
            var blendShapeIndex = GetIdleBreathBlendShapeIndex(renderer.sharedMesh);
            Debug.Log(
                "AccelerandoIdleBreathAnimatorSample " +
                $"Frame={suffix}, NormalizedTime={normalizedTime:0.###}, Weight={renderer.GetBlendShapeWeight(blendShapeIndex):0.###}.");
        }

        private static GameObject CreateIdleBreathBakedCaptureObject(SkinnedMeshRenderer sourceRenderer, string suffix)
        {
            var bakedMesh = new Mesh
            {
                name = $"Accelerando_IdleBreath_BakedCapture_{suffix}"
            };
            sourceRenderer.BakeMesh(bakedMesh);

            var captureObject = new GameObject($"Accelerando_IdleBreath_BakedCapture_{suffix}");
            captureObject.transform.SetParent(sourceRenderer.transform.parent, false);
            captureObject.transform.localPosition = sourceRenderer.transform.localPosition;
            captureObject.transform.localRotation = sourceRenderer.transform.localRotation;
            captureObject.transform.localScale = sourceRenderer.transform.localScale;

            var meshFilter = captureObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = bakedMesh;

            var meshRenderer = captureObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            meshRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            meshRenderer.receiveShadows = sourceRenderer.receiveShadows;
            meshRenderer.enabled = true;
            return captureObject;
        }

        private static void ApplyIdleBreathReviewPose(SkinnedMeshRenderer renderer, Vector3 baseScale, Vector3 baseLocalPosition, float weight)
        {
            var target = renderer.transform;
            target.localScale = new Vector3(
                baseScale.x * (1f + IdleBreathTransformScaleXz),
                baseScale.y * (1f + IdleBreathTransformScaleY),
                baseScale.z * (1f + IdleBreathTransformScaleXz));

            var localPosition = baseLocalPosition;
            localPosition.y += IdleBreathTransformLiftY;
            target.localPosition = localPosition;

            var blendShapeIndex = GetIdleBreathBlendShapeIndex(renderer.sharedMesh);
            renderer.SetBlendShapeWeight(blendShapeIndex, Mathf.Clamp(weight, 0f, 100f));
        }

        private static void CaptureReviewImagesForFocus(
            Transform placementRoot,
            Transform[] captureRoots,
            Transform focus,
            string frontFileName,
            string obliqueFileName,
            string logName,
            Bounds? fixedBounds = null)
        {
            var bounds = fixedBounds ?? CalculateRendererBounds(captureRoots, new Bounds(focus.position, Vector3.one));
            var outputDirectory = GetAbsoluteProjectPath(ValidationFolder);
            Directory.CreateDirectory(outputDirectory);

            var cameraObject = new GameObject("Accelerando_CaptureCamera");
            var lightObject = new GameObject("Accelerando_CaptureLight");
            var captureLayerTransforms = CollectChildTransforms(captureRoots);
            var originalLayers = new int[captureLayerTransforms.Length];
            var previousAmbientMode = RenderSettings.ambientMode;
            var previousAmbientLight = RenderSettings.ambientLight;
            var previousAmbientIntensity = RenderSettings.ambientIntensity;
            try
            {
                for (var i = 0; i < captureLayerTransforms.Length; i++)
                {
                    originalLayers[i] = captureLayerTransforms[i].gameObject.layer;
                    captureLayerTransforms[i].gameObject.layer = ReviewCaptureLayer;
                }

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.42f, 0.48f, 0.52f, 1f);
                RenderSettings.ambientIntensity = 1.15f;

                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.10f, 0.11f, 0.105f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.cullingMask = 1 << ReviewCaptureLayer;

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.75f;
                light.transform.rotation = Quaternion.Euler(48f, placementRoot.eulerAngles.y + 30f, 0f);

                var frontPath = Path.Combine(outputDirectory, frontFileName);
                ConfigureCaptureCamera(camera, focus, bounds, 0f);
                SaveCameraCapture(camera, frontPath);

                var obliquePath = Path.Combine(outputDirectory, obliqueFileName);
                ConfigureCaptureCamera(camera, focus, bounds, 35f);
                SaveCameraCapture(camera, obliquePath);

                Debug.Log($"{logName} Paths={frontPath};{obliquePath}");
            }
            finally
            {
                for (var i = 0; i < captureLayerTransforms.Length; i++)
                {
                    if (captureLayerTransforms[i] != null)
                    {
                        captureLayerTransforms[i].gameObject.layer = originalLayers[i];
                    }
                }

                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                RenderSettings.ambientIntensity = previousAmbientIntensity;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static void ConfigureCaptureCamera(Camera camera, Transform focus, Bounds bounds, float yawOffsetDegrees)
        {
            var frontDirection = Quaternion.AngleAxis(yawOffsetDegrees, Vector3.up) * CalculateAccelerandoVisualFrontDirection(focus);
            frontDirection.y = 0f;
            frontDirection = frontDirection.sqrMagnitude > 0.001f ? frontDirection.normalized : Vector3.forward;
            var lookAt = CalculateLookAt(bounds);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.2f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var position = lookAt + frontDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.18f, 0.08f, 0.35f);
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.farClipPlane = distance + Mathf.Max(bounds.extents.x, bounds.extents.z) + 12.00f;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.18f, bounds.extents.x * 0.72f, 1.2f);
            Debug.Log(
                "AccelerandoCaptureCamera " +
                $"YawOffset={yawOffsetDegrees:0.###}, Position={FormatVector(position)}, LookAt={FormatVector(lookAt)}, " +
                $"BoundsCenter={FormatVector(bounds.center)}, BoundsSize={FormatVector(bounds.size)}, OrthographicSize={camera.orthographicSize:0.###}.");
        }

        private static void SaveCameraCapture(Camera camera, string outputPath)
        {
            var renderTexture = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();

                var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Transform FindAccelerandoCameraFocus(Transform placementRoot)
        {
            return placementRoot.Find(PlacementObjectName) ?? placementRoot;
        }

        private static Transform[] FindAnimationSlotTransforms(Transform placementRoot)
        {
            var transforms = new Transform[AnimationReviewSlots.Length];
            for (var i = 0; i < AnimationReviewSlots.Length; i++)
            {
                var slotTransform = placementRoot.Find(AnimationReviewSlots[i].ObjectName);
                if (slotTransform == null)
                {
                    throw new InvalidOperationException($"{AnimationReviewSlots[i].ObjectName} animation review slot is missing.");
                }

                transforms[i] = slotTransform;
            }

            return transforms;
        }

        private static Transform[] FindAlignedReviewTransforms(Transform placementRoot)
        {
            var staticReview = placementRoot.Find(PlacementObjectName);
            if (staticReview == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            var slotTransforms = FindAnimationSlotTransforms(placementRoot);
            var transforms = new Transform[slotTransforms.Length + 1];
            transforms[0] = staticReview;
            Array.Copy(slotTransforms, 0, transforms, 1, slotTransforms.Length);
            return transforms;
        }

        private static Transform[] CollectChildTransforms(Transform[] roots)
        {
            var transforms = new List<Transform>();
            foreach (var root in roots)
            {
                transforms.AddRange(root.GetComponentsInChildren<Transform>(true));
            }

            return transforms.ToArray();
        }

        private static Vector3 CalculateAccelerandoVisualFrontDirection(Transform focus)
        {
            var yawRotation = Quaternion.Euler(0f, focus.eulerAngles.y, 0f);
            var frontDirection = yawRotation * Vector3.back;
            frontDirection.y = 0f;
            return frontDirection.sqrMagnitude > 0.001f ? frontDirection.normalized : Vector3.forward;
        }

        private static Vector3 CalculateLookAt(Bounds bounds)
        {
            return bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.08f, 0.04f, 0.22f);
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

        private static float CalculateLongaTergoZSpacing(Transform longaRoot, Transform tergoRoot)
        {
            var zSpacing = Mathf.Abs(longaRoot.position.z - tergoRoot.position.z);
            if (zSpacing > 0.10f)
            {
                return zSpacing;
            }

            return Mathf.Max(Vector3.Distance(longaRoot.position, tergoRoot.position), FallbackLongaTergoSpacing);
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

        private static Bounds CalculateRendererBounds(Transform[] roots, Bounds fallback)
        {
            var hasBounds = false;
            var bounds = fallback;
            foreach (var root in roots)
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return hasBounds ? bounds : fallback;
        }

        private static Bounds CalculateActiveRendererBounds(IReadOnlyList<Transform> roots, Bounds fallback)
        {
            var hasBounds = false;
            var bounds = fallback;
            for (var i = 0; i < roots.Count; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return hasBounds ? bounds : fallback;
        }

        private static float GetBoundsClearance(Bounds a, Bounds b)
        {
            if (a.Intersects(b))
            {
                return 0f;
            }

            var dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
            var dy = Mathf.Max(0f, Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y));
            var dz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static string GetAbsoluteProjectPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
        }

        private static string FormatVector(Vector3 vector)
        {
            return $"({vector.x:0.###},{vector.y:0.###},{vector.z:0.###})";
        }
    }
}
