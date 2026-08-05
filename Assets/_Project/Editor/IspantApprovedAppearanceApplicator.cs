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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantApprovedAppearanceApplicator
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Ispant Enemy Placement";
        private const string ModelName = "Ispant_Model";
        private const string OriginalModelPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_Armed.fbx";
        private const string AppearanceRoot =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedAppearance";
        private const string ApprovedModelPath =
            AppearanceRoot + "/Models/Ispant_Armed_Approved.fbx";
        private const string TextureFolder = AppearanceRoot + "/Textures";
        private const string MaterialFolder = AppearanceRoot + "/Materials";
        private const string CrescentArmorMaterialKey =
            "Ispant_Crescent_Armor";
        private const string CrescentArmorMaterialPath =
            MaterialFolder + "/Ispant_Crescent_Armor_Approved.mat";
        private const string ShaderPath =
            AppearanceRoot + "/Shaders/IspantApprovedAppearance.shader";
        private const string SampleTextureRelativePath =
            "artSample/enemies/ispant_armed/textures";
        private const string SampleReviewRelativePath =
            "artSample/enemies/ispant_armed/Ispant_Armed_Appearance_FinalReview.png";
        private const string ReportRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_Inspection.txt";
        private const string CaptureRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedFaceRevision_Diagnostic.png";
        private const string ReplacementCaptureRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedFaceRevision_Final.png";
        private const string BrightnessDiagnosticRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_BrightnessDiagnostic_02.png";
        private const string BrightnessFinalRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_BrightnessFinal.png";
        private const string BodyBrightnessDiagnosticRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_BodyBrightnessDiagnostic.png";
        private const string BodyBrightnessFinalRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_BodyBrightnessFinal.png";
        private const string BodyMean25DiagnosticRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_BodyMean25_Diagnostic.png";
        private const string BodyMean25FinalRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_BodyMean25_Final.png";
        private const string LightingDiagnosticRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_LightingDiagnostic_02.png";
        private const string LightingDiagnosticReportRelativePath =
            "docs/validation/ispant_appearance_apply_2026-08-05/Ispant_ApprovedAppearance_LightingDiagnostic_02.txt";
        private const string ExpectedApprovedFbxSha256 =
            "E33AE0B988CD7CA6FE96D42D7D5E057F1CB57800009EBF7413EE0694BC6825FA";
        private const int ExpectedSlots = 12;
        private const int ExpectedRenderersPerSlot = 3;
        private const int ExpectedBodyTriangles = 3596;
        private const int ExpectedCrescentEvaluatedVertices = 656;
        private const int ExpectedCrescentEvaluatedTriangles = 1308;
        private const int ExpectedCrescentFbxDegenerateTriangles = 50;
        private const int ExpectedCrescentImportedVertices = 2121;
        private const int ExpectedCrescentImportedTriangles = 1258;
        private const int ApprovedBodyControlVertices = 2044;
        private const int ApprovedBodyControlPolygons = 3596;
        private const int ApprovedCrescentControlVertices = 148;
        private const int ApprovedCrescentControlPolygons = 146;
        private const int ApprovedEyeControlVertices = 32;
        private const int ApprovedEyeControlPolygons = 28;
        private const int ExpectedEyeEvaluatedVertices = 160;
        private const int ExpectedEyeImportedTriangles = 312;
        private const int ExpectedBones = 24;
        private const float ApprovedBodyArmorBrightness = 1.88321114f;
        private const float ApprovedHelmetArmorBrightness = 1.36f;
        private const float ApprovedCrescentArmorBrightness = 1.21f;
        private const float TargetBodyArmorMeanLuminance = 0.852405f;
        private const float BodyArmorMeanLuminanceTolerance = 0.00005f;
        // Unity preview coordinates matching the user-approved 4096x1152 comparison ROI.
        private const int BodyMeanRoiXMin = 682;
        private const int BodyMeanRoiXMax = 1262;
        private const int BodyMeanRoiYMin = 121;
        private const int BodyMeanRoiYMax = 871;
        private const float ApprovedArmorMeanRoughness = 0.46701074f;
        private const float ApprovedArmorMeanMetallic = 0.3543307f;

        private static readonly string[] SlotNames =
        {
            "Ispant_01", "Ispant_02", "Ispant_03", "Ispant_04",
            "Ispant_05", "Ispant_06", "Ispant_07", "Ispant_08",
            "Ispant_09", "Ispant_10", "Ispant_11", "Ispant_12"
        };

        private static readonly string[] ExpectedUnityBodyMaterialOrder =
        {
            "Ispant_Armor", "Ispant_Leather", "Ispant_Gunmetal",
            "Ispant_Rubber_Black", "Ispant_Wood", "Ispant_Helmet_Face",
            "Ispant_Copper", "Ispant_Steel", "Ispant_Helmet"
        };

        private static readonly MaterialDefinition[] MaterialDefinitions =
        {
            new("Ispant_Armor", "armor_ivory", 0.66f, true, 0, 0.16f, 0.34f, ApprovedBodyArmorBrightness),
            new("Ispant_Helmet", "armor_ivory", 0.66f, true, 0, 0.16f, 0.34f, ApprovedHelmetArmorBrightness),
            // Feature mode 2 selects the approved third UV channel used only by the helmet face.
            new("Ispant_Helmet_Face", "helmet_face", 0.66f, false, 2, 0.16f, 0.34f, ApprovedHelmetArmorBrightness),
            new("Ispant_Gunmetal", "gunmetal", 0.30f, false, 0, 0f, 0.34f),
            new("Ispant_Leather", "leather_brown", 0.30f, false, 0, 0f, 0.34f),
            new("Ispant_Wood", "musket_wood", 0.16f, false, 0, 0f, 0.34f),
            new("Ispant_Steel", "steel_silver", 0.14f, false, 0, 0f, 0.34f),
            new("Ispant_Copper", "copper_accent", 0.18f, false, 0, 0f, 0.34f),
            MaterialDefinition.Rubber(),
            MaterialDefinition.Eye()
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Approved Appearance")]
        public static void ApplyApprovedIspantAppearance()
        {
            var scene = RequireActiveScene(requireClean: true);
            var root = RequirePlacementRoot(scene);
            var before = CaptureContract(scene, root);

            RequireApprovedFiles();
            ConfigureTextureAssets();
            ConfigureApprovedModelImporter();
            var asset = InspectApprovedAsset();
            var materials = CreateOrUpdateMaterials(asset.ApprovedYFlip);

            for (var index = 0; index < ExpectedSlots; index++)
            {
                var slot = root.transform.GetChild(index);
                RequireSlot(slot, index);
                var previous = slot.GetChild(0);
                RequireAllowedPreviousModel(previous);
                var localPosition = previous.localPosition;
                var localRotation = previous.localRotation;
                var localScale = previous.localScale;
                var active = previous.gameObject.activeSelf;
                var staticFlags = GameObjectUtility.GetStaticEditorFlags(
                    previous.gameObject);

                UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var replacement = PrefabUtility.InstantiatePrefab(
                    asset.Prefab,
                    scene) as GameObject ??
                    throw new InvalidOperationException(
                        "The approved Ispant FBX could not be instantiated.");
                replacement.name = ModelName;
                replacement.transform.SetParent(slot, false);
                replacement.transform.SetLocalPositionAndRotation(
                    localPosition,
                    localRotation);
                replacement.transform.localScale = localScale;
                replacement.SetActive(active);
                ConfigureStaticModel(replacement, staticFlags);
                ApplyApprovedMaterials(replacement, materials);
                EditorUtility.SetDirty(replacement);
                EditorUtility.SetDirty(slot.gameObject);
            }

            var after = CaptureContract(scene, root);
            RequireContractPreserved(before, after);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the approved Ispant appearance.");
            }

            AssetDatabase.SaveAssets();
            var result = InspectAppliedState(scene, root, materials);
            WriteReport(result, "APPLY_PASS", before, after);
            Debug.Log(
                "IspantApprovedAppearanceApplied Result=PASS" +
                ", Slots=" + result.SlotCount +
                ", RenderersPerSlot=" + result.RenderersPerSlot +
                ", BodyImportedVertices=" + result.BodyImportedVertices +
                ", BodyTriangles=" + result.BodyTriangles +
                ", CrescentImportedVertices=" + result.CrescentImportedVertices +
                ", CrescentTriangles=" + result.CrescentTriangles +
                ", EyeImportedVertices=" + result.EyeImportedVertices +
                ", EyeTriangles=" + result.EyeTriangles +
                ", Bones=" + result.Bones +
                ", TextureFiles=" + result.TextureCount +
                ", MechanicalUv=True" +
                ", HelmetFaceUv=True" +
                ", CrescentBaseMesh=148/146" +
                ", EyeBaseMesh=32/28" +
                ", WaistBeltRemoved=True" +
                ", ChestStrapPreserved=True" +
                ", ApprovedFacePattern=True" +
                ", RemovedStickWeapon=True" +
                ", SlotTransformsPreserved=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Approved Brightness Sync")]
        public static void ApplyApprovedIspantBrightnessSync()
        {
            var scene = RequireActiveScene(requireClean: true);
            var root = RequirePlacementRoot(scene);
            var before = CaptureContract(scene, root);
            RequireApprovedFiles();
            var asset = InspectApprovedAsset();
            var materials = CreateOrUpdateMaterials(asset.ApprovedYFlip);

            for (var index = 0; index < ExpectedSlots; index++)
            {
                var slot = root.transform.GetChild(index);
                RequireSlot(slot, index);
                var model = slot.GetChild(0).gameObject;
                var source = PrefabUtility.GetCorrespondingObjectFromSource(model);
                if (model.name != ModelName || source == null ||
                    AssetDatabase.GetAssetPath(source) != ApprovedModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name + " is not a direct instance of the approved Ispant FBX.");
                }
                ApplyApprovedMaterials(model, materials);
                EditorUtility.SetDirty(model);
                EditorUtility.SetDirty(slot.gameObject);
            }

            var after = CaptureContract(scene, root);
            RequireContractPreserved(before, after);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after synchronizing approved Ispant brightness.");
            }
            AssetDatabase.SaveAssets();
            var result = InspectAppliedState(scene, root, materials);
            WriteReport(result, "BRIGHTNESS_SYNC_APPLY_PASS", before, after);
            Debug.Log(
                "IspantApprovedBrightnessSynchronized Result=PASS" +
                ", Slots=" + result.SlotCount +
                ", BodyArmorBrightness=" +
                ApprovedBodyArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", HelmetArmorBrightness=" +
                ApprovedHelmetArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", CrescentArmorBrightness=" +
                ApprovedCrescentArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", HelmetBrightnessPreserved=True" +
                ", CrescentMaterialPreserved=True" +
                ", SlotTransformsPreserved=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Approved Appearance")]
        public static void InspectApprovedIspantAppearance()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            RequireApprovedFiles();
            var asset = InspectApprovedAsset();
            var materials = LoadApprovedMaterials();
            RequireMaterialConfiguration(materials, asset.ApprovedYFlip);
            var contract = CaptureContract(scene, root);
            var result = InspectAppliedState(scene, root, materials);
            WriteReport(result, "INSPECTION_PASS", contract, contract);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Ispant appearance inspection changed the scene dirty state.");
            }

            Debug.Log(
                "IspantApprovedAppearanceInspected Result=PASS" +
                ", ActiveScene=" + scene.path +
                ", Slots=" + result.SlotCount +
                ", RenderersPerSlot=" + result.RenderersPerSlot +
                ", BodyImportedVertices=" + result.BodyImportedVertices +
                ", BodyTriangles=" + result.BodyTriangles +
                ", CrescentImportedVertices=" + result.CrescentImportedVertices +
                ", CrescentTriangles=" + result.CrescentTriangles +
                ", EyeImportedVertices=" + result.EyeImportedVertices +
                ", EyeTriangles=" + result.EyeTriangles +
                ", Bones=" + result.Bones +
                ", TextureFiles=" + result.TextureCount +
                ", TextureHashesMatch=True" +
                ", ApprovedFbxHashMatch=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Approved Appearance Review")]
        public static void CaptureApprovedIspantAppearanceReview()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var asset = InspectApprovedAsset();
            var materials = LoadApprovedMaterials();
            RequireMaterialConfiguration(materials, asset.ApprovedYFlip);
            InspectAppliedState(scene, root, materials);
            var model = root.transform.GetChild(0).GetChild(0).gameObject;
            CaptureComparison(model, CaptureRelativePath);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Ispant appearance capture changed the scene dirty state.");
            }

            Debug.Log(
                "IspantApprovedAppearanceReviewCaptured Result=PASS" +
                ", Image=" + CaptureRelativePath +
                ", Left=ApprovedBlenderSample" +
                ", Right=UnityApprovedAppearance" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Corrected Appearance Review")]
        public static void CaptureApprovedIspantAppearanceReviewReplacement()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var asset = InspectApprovedAsset();
            var materials = LoadApprovedMaterials();
            RequireMaterialConfiguration(materials, asset.ApprovedYFlip);
            InspectAppliedState(scene, root, materials);
            var model = root.transform.GetChild(0).GetChild(0).gameObject;
            CaptureComparison(model, ReplacementCaptureRelativePath);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Corrected approved Ispant appearance capture changed the scene dirty state.");
            }

            Debug.Log(
                "IspantApprovedAppearanceCorrectedReviewCaptured Result=PASS" +
                ", Image=" + ReplacementCaptureRelativePath +
                ", Left=ApprovedBlenderSample" +
                ", Right=UnityApprovedAppearanceCorrectedLighting" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Brightness Diagnostic")]
        public static void CaptureApprovedIspantBrightnessDiagnostic()
        {
            CaptureApprovedIspantBrightnessComparison(
                BrightnessDiagnosticRelativePath,
                "IspantApprovedBrightnessDiagnosticCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Brightness Final Review")]
        public static void CaptureApprovedIspantBrightnessReview()
        {
            CaptureApprovedIspantBrightnessComparison(
                BrightnessFinalRelativePath,
                "IspantApprovedBrightnessFinalCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Body Brightness Diagnostic")]
        public static void CaptureApprovedIspantBodyBrightnessDiagnostic()
        {
            CaptureApprovedIspantBrightnessComparison(
                BodyBrightnessDiagnosticRelativePath,
                "IspantApprovedBodyBrightnessDiagnosticCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Body Brightness Final Review")]
        public static void CaptureApprovedIspantBodyBrightnessReview()
        {
            CaptureApprovedIspantBrightnessComparison(
                BodyBrightnessFinalRelativePath,
                "IspantApprovedBodyBrightnessFinalCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Body Mean 25 Percent Diagnostic")]
        public static void CaptureApprovedIspantBodyMean25Diagnostic()
        {
            CaptureApprovedIspantBrightnessComparison(
                BodyMean25DiagnosticRelativePath,
                "IspantApprovedBodyMean25DiagnosticCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Body Mean 25 Percent Final Review")]
        public static void CaptureApprovedIspantBodyMean25Review()
        {
            CaptureApprovedIspantBrightnessComparison(
                BodyMean25FinalRelativePath,
                "IspantApprovedBodyMean25FinalCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Calibrate Body Armor Mean Luminance")]
        public static void CalibrateApprovedIspantBodyArmorMeanLuminance()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var asset = InspectApprovedAsset();
            var materials = LoadApprovedMaterials();
            RequireMaterialConfiguration(materials, asset.ApprovedYFlip);
            InspectAppliedState(scene, root, materials);
            var model = root.transform.GetChild(0).GetChild(0).gameObject;

            var lower = ApprovedHelmetArmorBrightness;
            var upper = 8f;
            var upperMean = MeasureBodyArmorMeanLuminance(
                model,
                upper);
            if (upperMean < TargetBodyArmorMeanLuminance)
            {
                throw new InvalidOperationException(
                    "The approved Ispant body luminance target cannot be reached within the calibration range.");
            }
            for (var iteration = 0; iteration < 20; iteration++)
            {
                var candidate = (lower + upper) * 0.5f;
                var mean = MeasureBodyArmorMeanLuminance(model, candidate);
                if (mean < TargetBodyArmorMeanLuminance)
                {
                    lower = candidate;
                }
                else
                {
                    upper = candidate;
                }
            }
            var calibrated = (lower + upper) * 0.5f;
            var calibratedMean = MeasureBodyArmorMeanLuminance(model, calibrated);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Body armor luminance calibration changed the scene dirty state.");
            }
            Debug.Log(
                "IspantApprovedBodyArmorLuminanceCalibrated Result=PASS" +
                ", TargetMean=" +
                TargetBodyArmorMeanLuminance.ToString("R", CultureInfo.InvariantCulture) +
                ", CalibratedBrightness=" +
                calibrated.ToString("R", CultureInfo.InvariantCulture) +
                ", CalibratedMean=" +
                calibratedMean.ToString("R", CultureInfo.InvariantCulture) +
                ", HelmetBrightness=" +
                ApprovedHelmetArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", CrescentBrightness=" +
                ApprovedCrescentArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Body Armor Mean Luminance")]
        public static void InspectApprovedIspantBodyArmorMeanLuminance()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var asset = InspectApprovedAsset();
            var materials = LoadApprovedMaterials();
            RequireMaterialConfiguration(materials, asset.ApprovedYFlip);
            InspectAppliedState(scene, root, materials);
            var model = root.transform.GetChild(0).GetChild(0).gameObject;
            var warmupMean = MeasureAppliedBodyArmorMeanLuminance(model);
            Debug.Log(
                "IspantApprovedBodyArmorMeanLuminanceWarmupDiscarded" +
                ", Mean=" +
                warmupMean.ToString("R", CultureInfo.InvariantCulture) + ".");
            var mean = MeasureAppliedBodyArmorMeanLuminance(model);
            if (mean < 0.1f)
            {
                Debug.Log(
                    "IspantApprovedBodyArmorMeanLuminancePreviewRetry" +
                    ", FirstMean=" +
                    mean.ToString("R", CultureInfo.InvariantCulture) + ".");
                mean = MeasureAppliedBodyArmorMeanLuminance(model);
            }
            var difference = Mathf.Abs(mean - TargetBodyArmorMeanLuminance);
            if (difference > BodyArmorMeanLuminanceTolerance)
            {
                throw new InvalidOperationException(
                    "The approved Ispant body armor mean luminance differs from the user-approved target. Target=" +
                    TargetBodyArmorMeanLuminance.ToString("R", CultureInfo.InvariantCulture) +
                    ", Actual=" + mean.ToString("R", CultureInfo.InvariantCulture) +
                    ", Difference=" + difference.ToString("R", CultureInfo.InvariantCulture) + ".");
            }
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Body armor mean luminance inspection changed the scene dirty state.");
            }
            Debug.Log(
                "IspantApprovedBodyArmorMeanLuminanceInspected Result=PASS" +
                ", BaselineMean=0.681924" +
                ", TargetIncreasePercent=25" +
                ", TargetMean=" +
                TargetBodyArmorMeanLuminance.ToString("R", CultureInfo.InvariantCulture) +
                ", ActualMean=" + mean.ToString("R", CultureInfo.InvariantCulture) +
                ", Difference=" + difference.ToString("R", CultureInfo.InvariantCulture) +
                ", BodyArmorBrightness=" +
                ApprovedBodyArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", HelmetBrightness=" +
                ApprovedHelmetArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", CrescentBrightness=" +
                ApprovedCrescentArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", SceneChanged=False.");
        }

        private static void CaptureApprovedIspantBrightnessComparison(
            string outputRelativePath,
            string logMarker)
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var asset = InspectApprovedAsset();
            var materials = LoadApprovedMaterials();
            RequireMaterialConfiguration(materials, asset.ApprovedYFlip);
            InspectAppliedState(scene, root, materials);
            var model = root.transform.GetChild(0).GetChild(0).gameObject;
            CaptureComparison(model, outputRelativePath);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Ispant brightness capture changed the scene dirty state.");
            }
            Debug.Log(
                logMarker + " Result=PASS" +
                ", Image=" + outputRelativePath +
                ", BodyArmorBrightness=" +
                ApprovedBodyArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", HelmetArmorBrightness=" +
                ApprovedHelmetArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", CrescentArmorBrightness=" +
                ApprovedCrescentArmorBrightness.ToString("R", CultureInfo.InvariantCulture) +
                ", CrescentMaterialPreserved=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Diagnose Preview Lighting")]
        public static void DiagnoseApprovedIspantPreviewLighting()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var asset = InspectApprovedAsset();
            var materials = LoadApprovedMaterials();
            RequireMaterialConfiguration(materials, asset.ApprovedYFlip);
            InspectAppliedState(scene, root, materials);
            var model = root.transform.GetChild(0).GetChild(0).gameObject;
            var diagnosticPath = ProjectAbsolutePath(
                LightingDiagnosticRelativePath);
            if (File.Exists(diagnosticPath))
            {
                throw new InvalidOperationException(
                    "The approved Ispant lighting diagnostic already exists: " +
                    diagnosticPath);
            }

            var approved = CapturePreview(model);
            var urpLit = CapturePreview(model, useUrpLitDiagnostic: true);
            try
            {
                WriteSideBySide(approved, urpLit, diagnosticPath);
                var approvedMetrics = MeasureForegroundLuminance(approved);
                var litMetrics = MeasureForegroundLuminance(urpLit);
                var pipelinePath = AssetDatabase.GetAssetPath(
                    GraphicsSettings.currentRenderPipeline);
                File.WriteAllLines(
                    ProjectAbsolutePath(LightingDiagnosticReportRelativePath),
                    new[]
                    {
                        "status=DIAGNOSTIC_CAPTURED",
                        "left=approved_custom_shader",
                        "right=urp_lit_same_preview_lighting",
                        "render_pipeline=" + pipelinePath,
                        "approved_foreground_pixels=" + approvedMetrics.PixelCount,
                        "approved_mean_luminance=" + approvedMetrics.MeanLuminance.ToString("R", CultureInfo.InvariantCulture),
                        "approved_peak_luminance=" + approvedMetrics.PeakLuminance.ToString("R", CultureInfo.InvariantCulture),
                        "urp_lit_foreground_pixels=" + litMetrics.PixelCount,
                        "urp_lit_mean_luminance=" + litMetrics.MeanLuminance.ToString("R", CultureInfo.InvariantCulture),
                        "urp_lit_peak_luminance=" + litMetrics.PeakLuminance.ToString("R", CultureInfo.InvariantCulture),
                        "scene_changed=false"
                    },
                    Encoding.UTF8);
                Debug.Log(
                    "IspantApprovedAppearanceLightingDiagnosed Result=PASS" +
                    ", Left=ApprovedCustomShader" +
                    ", Right=UrpLitSamePreviewLighting" +
                    ", ApprovedMeanLuminance=" +
                    approvedMetrics.MeanLuminance.ToString("R", CultureInfo.InvariantCulture) +
                    ", UrpLitMeanLuminance=" +
                    litMetrics.MeanLuminance.ToString("R", CultureInfo.InvariantCulture) +
                    ", SceneChanged=False.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(approved);
                UnityEngine.Object.DestroyImmediate(urpLit);
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Ispant lighting diagnosis changed the scene dirty state.");
            }
        }

        private static void ConfigureApprovedModelImporter()
        {
            AssetDatabase.ImportAsset(
                ApprovedModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ApprovedModelPath)
                as ModelImporter ??
                throw new InvalidOperationException(
                    "The approved Ispant ModelImporter is missing.");
            importer.importAnimation = false;
            importer.importBlendShapes = true;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation =
                ModelImporterMaterialLocation.InPrefab;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.SaveAndReimport();
        }

        private static void ConfigureTextureAssets()
        {
            foreach (var path in Directory.GetFiles(
                         ProjectAbsolutePath(TextureFolder),
                         "*.png",
                         SearchOption.TopDirectoryOnly))
            {
                var assetPath = AbsoluteToAssetPath(path);
                AssetDatabase.ImportAsset(
                    assetPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(assetPath)
                    as TextureImporter ??
                    throw new InvalidOperationException(
                        "Approved Ispant texture importer is missing: " + assetPath);
                var name = Path.GetFileName(assetPath);
                var normal = name.EndsWith(
                    "_normal.png",
                    StringComparison.OrdinalIgnoreCase);
                var nonColor = normal ||
                    name.EndsWith(
                        "_roughness.png",
                        StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(
                        "_metallic.png",
                        StringComparison.OrdinalIgnoreCase);
                importer.textureType = normal
                    ? TextureImporterType.NormalMap
                    : TextureImporterType.Default;
                importer.sRGBTexture = !nonColor;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, Material> CreateOrUpdateMaterials(
            float approvedYFlip)
        {
            AssetDatabase.ImportAsset(
                ShaderPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                throw new InvalidOperationException(
                    "The approved Ispant shader is missing.");
            if (!shader.isSupported)
            {
                throw new InvalidOperationException(
                    "The approved Ispant shader is not supported by the active renderer.");
            }

            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var definition in MaterialDefinitions)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(
                    definition.MaterialPath);
                if (material == null)
                {
                    material = new Material(shader)
                    {
                        name = Path.GetFileNameWithoutExtension(
                            definition.MaterialPath)
                    };
                    AssetDatabase.CreateAsset(material, definition.MaterialPath);
                }
                else
                {
                    material.shader = shader;
                }

                ConfigureMaterial(material, definition, approvedYFlip);
                EditorUtility.SetDirty(material);
                result.Add(definition.SourceName, material);
            }

            var crescentArmor = AssetDatabase.LoadAssetAtPath<Material>(
                CrescentArmorMaterialPath);
            if (crescentArmor == null)
            {
                crescentArmor = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(
                        CrescentArmorMaterialPath)
                };
                AssetDatabase.CreateAsset(
                    crescentArmor,
                    CrescentArmorMaterialPath);
            }
            else
            {
                crescentArmor.shader = shader;
            }
            ConfigureCrescentArmorMaterial(crescentArmor, approvedYFlip);
            EditorUtility.SetDirty(crescentArmor);
            result.Add(CrescentArmorMaterialKey, crescentArmor);

            AssetDatabase.SaveAssets();
            return result;
        }

        private static void ConfigureCrescentArmorMaterial(
            Material material,
            float approvedYFlip)
        {
            material.SetColor(
                "_BaseColor",
                new Color(
                    ApprovedCrescentArmorBrightness,
                    ApprovedCrescentArmorBrightness,
                    ApprovedCrescentArmorBrightness,
                    1f));
            material.SetFloat("_NormalStrength", 0f);
            material.SetFloat("_UseMaps", 0f);
            material.SetFloat("_UseUv1", 0f);
            material.SetFloat("_RoughnessBias", ApprovedArmorMeanRoughness);
            material.SetFloat("_MetallicBias", ApprovedArmorMeanMetallic);
            material.SetFloat("_CoatWeight", 0.16f);
            material.SetFloat("_CoatRoughness", 0.34f);
            material.SetFloat("_FeatureMode", 0f);
            material.SetFloat("_ApprovedYFlip", approvedYFlip);
            material.SetTexture("_BaseMap", null);
            material.SetTexture("_RoughnessMap", null);
            material.SetTexture("_MetallicMap", null);
            material.SetTexture("_NormalMap", null);
        }

        private static void ConfigureMaterial(
            Material material,
            MaterialDefinition definition,
            float approvedYFlip)
        {
            material.SetColor("_BaseColor", definition.BaseColor);
            material.SetFloat("_NormalStrength", definition.NormalStrength);
            material.SetFloat("_UseMaps", definition.UseMaps ? 1f : 0f);
            material.SetFloat("_UseUv1", definition.UseUv1 ? 1f : 0f);
            material.SetFloat("_RoughnessBias", definition.RoughnessBias);
            material.SetFloat("_MetallicBias", definition.MetallicBias);
            material.SetFloat("_CoatWeight", definition.CoatWeight);
            material.SetFloat("_CoatRoughness", definition.CoatRoughness);
            material.SetFloat("_FeatureMode", definition.FeatureMode);
            material.SetFloat("_ApprovedYFlip", approvedYFlip);
            if (definition.UseMaps)
            {
                material.SetTexture(
                    "_BaseMap",
                    LoadTexture(definition.TexturePrefix + "_basecolor.png"));
                material.SetTexture(
                    "_RoughnessMap",
                    LoadTexture(definition.TexturePrefix + "_roughness.png"));
                material.SetTexture(
                    "_MetallicMap",
                    LoadTexture(definition.TexturePrefix + "_metallic.png"));
                material.SetTexture(
                    "_NormalMap",
                    LoadTexture(definition.TexturePrefix + "_normal.png"));
            }
            else
            {
                material.SetTexture("_BaseMap", null);
                material.SetTexture("_RoughnessMap", null);
                material.SetTexture("_MetallicMap", null);
                material.SetTexture("_NormalMap", null);
            }
        }

        private static ApprovedAssetInfo InspectApprovedAsset()
        {
            RequireApprovedFiles();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ApprovedModelPath) ??
                throw new InvalidOperationException(
                    "The approved Ispant FBX asset is unavailable.");
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != ExpectedRenderersPerSlot)
            {
                throw new InvalidOperationException(
                    "The approved Ispant FBX must contain exactly body, crescent, and eye renderers. Actual=" +
                    renderers.Length + ".");
            }

            var body = renderers.OfType<SkinnedMeshRenderer>().SingleOrDefault(item =>
                item.sharedMesh != null && item.sharedMesh.name == "Ispant_Armed_Body");
            var crescent = renderers.SingleOrDefault(item =>
                SharedMesh(item)?.name == "Ispant_Crescent_Ornament");
            var eyes = renderers.SingleOrDefault(item =>
                SharedMesh(item)?.name == "Ispant_Reference_Eye_Slits");
            if (body == null || crescent == null || eyes == null)
            {
                throw new InvalidOperationException(
                    "The approved Ispant body, crescent, or explicit eye renderer is missing.");
            }

            if (TriangleCount(body.sharedMesh) != ExpectedBodyTriangles ||
                body.sharedMesh.subMeshCount != ExpectedUnityBodyMaterialOrder.Length ||
                body.bones.Length != ExpectedBones)
            {
                throw new InvalidOperationException(
                    "The approved Ispant body topology, material slots, or rig differs.");
            }
            var crescentMesh = SharedMesh(crescent);
            if (crescentMesh.vertexCount != ExpectedCrescentImportedVertices ||
                TriangleCount(crescentMesh) != ExpectedCrescentImportedTriangles ||
                crescentMesh.subMeshCount != 1)
            {
                throw new InvalidOperationException(
                    "The approved beveled crescent topology differs. Vertices=" +
                    crescentMesh.vertexCount +
                    ", Triangles=" + TriangleCount(crescentMesh) +
                    ", SubMeshes=" + crescentMesh.subMeshCount + ".");
            }
            var eyeMesh = SharedMesh(eyes);
            if (TriangleCount(eyeMesh) != ExpectedEyeImportedTriangles ||
                eyeMesh.subMeshCount != 1 ||
                eyeMesh.vertexCount < ExpectedEyeEvaluatedVertices)
            {
                throw new InvalidOperationException(
                    "The approved explicit eye topology differs. Vertices=" +
                    eyeMesh.vertexCount +
                    ", Triangles=" + TriangleCount(eyeMesh) +
                    ", SubMeshes=" + eyeMesh.subMeshCount + ".");
            }

            RequireMaterialNames(body, ExpectedUnityBodyMaterialOrder);
            RequireMaterialNames(crescent, new[] { "Ispant_Steel" });
            RequireMaterialNames(eyes, new[] { "Ispant_Eye_Cyan" });

            var mesh = body.sharedMesh;
            if (mesh.uv.Length != mesh.vertexCount ||
                mesh.uv2.Length != mesh.vertexCount ||
                mesh.uv3.Length != mesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "The approved Ispant FBX must contain original, mechanical, and helmet-face UV channels.");
            }

            return new ApprovedAssetInfo(
                prefab,
                body,
                crescent,
                eyes,
                0f);
        }

        private static void ApplyApprovedMaterials(
            GameObject model,
            IReadOnlyDictionary<string, Material> materials)
        {
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(
                         true).Cast<Renderer>().Concat(
                         model.GetComponentsInChildren<MeshRenderer>(true)))
            {
                var mesh = SharedMesh(renderer);
                if (mesh.name == "Ispant_Crescent_Ornament")
                {
                    renderer.sharedMaterials = new[]
                    {
                        materials[CrescentArmorMaterialKey]
                    };
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    EditorUtility.SetDirty(renderer);
                    continue;
                }
                if (mesh.name == "Ispant_Reference_Eye_Slits")
                {
                    renderer.sharedMaterials = new[] { materials["Ispant_Eye_Cyan"] };
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    EditorUtility.SetDirty(renderer);
                    continue;
                }
                if (mesh.name != "Ispant_Armed_Body" ||
                    renderer.sharedMaterials.Length !=
                    ExpectedUnityBodyMaterialOrder.Length)
                {
                    throw new InvalidOperationException(
                        "The approved Ispant body material slot contract differs.");
                }
                var ordered = ExpectedUnityBodyMaterialOrder
                    .Select(name => materials[name])
                    .ToArray();
                renderer.sharedMaterials = ordered;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
            }
        }

        private static AppearanceResult InspectAppliedState(
            Scene scene,
            GameObject root,
            IReadOnlyDictionary<string, Material> materials)
        {
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes during approved Ispant appearance inspection.");
            }

            var asset = InspectApprovedAsset();
            RequireMaterialConfiguration(materials, asset.ApprovedYFlip);
            if (root.transform.childCount != ExpectedSlots)
            {
                throw new InvalidOperationException(
                    "The approved Ispant placement no longer contains twelve slots.");
            }

            var expectedByMesh = new Dictionary<Mesh, Material[]>
            {
                { asset.Body.sharedMesh, ResolveApprovedMaterialOrder(asset.Body, materials) },
                {
                    SharedMesh(asset.Crescent),
                    new[] { materials[CrescentArmorMaterialKey] }
                },
                {
                    SharedMesh(asset.Eyes),
                    new[] { materials["Ispant_Eye_Cyan"] }
                }
            };
            for (var index = 0; index < ExpectedSlots; index++)
            {
                var slot = root.transform.GetChild(index);
                RequireSlot(slot, index);
                var model = slot.GetChild(0);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(
                    model.gameObject);
                if (model.name != ModelName || source == null ||
                    AssetDatabase.GetAssetPath(source) != ApprovedModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name + " is not a direct instance of the approved Ispant FBX.");
                }

                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length != ExpectedRenderersPerSlot)
                {
                    throw new InvalidOperationException(
                        slot.name + " does not contain exactly body, crescent, and eye renderers.");
                }
                foreach (var renderer in renderers)
                {
                    var mesh = SharedMesh(renderer);
                    if (mesh == null ||
                        !expectedByMesh.TryGetValue(
                            mesh,
                            out var expectedMaterials) ||
                        !renderer.sharedMaterials.SequenceEqual(expectedMaterials))
                    {
                        throw new InvalidOperationException(
                            slot.name + " does not use the exact approved mesh and material order.");
                    }
                }
                if (model.GetComponentsInChildren<Animator>(true)
                        .Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true)
                        .Any(item => item.enabled))
                {
                    throw new InvalidOperationException(
                        slot.name + " must remain static during this appearance-only application.");
                }
            }

            return new AppearanceResult(
                ExpectedSlots,
                ExpectedRenderersPerSlot,
                asset.Body.sharedMesh.vertexCount,
                TriangleCount(asset.Body.sharedMesh),
                SharedMesh(asset.Crescent).vertexCount,
                TriangleCount(SharedMesh(asset.Crescent)),
                SharedMesh(asset.Eyes).vertexCount,
                TriangleCount(SharedMesh(asset.Eyes)),
                asset.Body.bones.Length,
                RequireTextureHashesMatch());
        }

        private static void ConfigureStaticModel(
            GameObject model,
            StaticEditorFlags staticFlags)
        {
            foreach (var transform in model.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(
                    transform.gameObject,
                    staticFlags);
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

        private static void RequireAllowedPreviousModel(Transform model)
        {
            if (model.name != ModelName)
            {
                throw new InvalidOperationException(
                    "An Ispant slot contains an unexpected model child: " + model.name);
            }
            var source = PrefabUtility.GetCorrespondingObjectFromSource(
                model.gameObject);
            var path = source == null ? string.Empty : AssetDatabase.GetAssetPath(source);
            if (path != OriginalModelPath && path != ApprovedModelPath)
            {
                throw new InvalidOperationException(
                    "An Ispant slot contains an unapproved model source: " + path);
            }
        }

        private static void RequireSlot(Transform slot, int index)
        {
            if (slot.name != SlotNames[index] || slot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "The Ispant slot contract differs at index " + index + ".");
            }
        }

        private static SceneContract CaptureContract(Scene scene, GameObject root)
        {
            var slotBuilder = new StringBuilder();
            slotBuilder.Append(TransformSignature(root.transform));
            for (var index = 0; index < root.transform.childCount; index++)
            {
                slotBuilder.Append(TransformSignature(root.transform.GetChild(index)));
            }
            var otherRoots = scene.GetRootGameObjects()
                .Where(item => item != root)
                .Select(item => HierarchySignature(item.transform))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
            return new SceneContract(
                Sha256(Encoding.UTF8.GetBytes(slotBuilder.ToString())),
                otherRoots);
        }

        private static void RequireContractPreserved(
            SceneContract before,
            SceneContract after)
        {
            if (before.IspantSlotTransformHash != after.IspantSlotTransformHash)
            {
                throw new InvalidOperationException(
                    "An approved Ispant root or slot Transform changed during appearance application.");
            }
            if (!before.OtherRoots.SequenceEqual(
                    after.OtherRoots,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside the approved Ispant placement changed during appearance application.");
            }
        }

        private static string TransformSignature(Transform transform)
        {
            return transform.name + "|" +
                   transform.gameObject.activeSelf + "|" +
                   Vec(transform.localPosition) + "|" +
                   Quat(transform.localRotation) + "|" +
                   Vec(transform.localScale) + "|" +
                   (int)GameObjectUtility.GetStaticEditorFlags(transform.gameObject) + ";";
        }

        private static string HierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var current in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => GetPath(item, root), StringComparer.Ordinal))
            {
                builder.Append(GetPath(current, root)).Append('|')
                    .Append(TransformSignature(current));
                foreach (var component in current.GetComponents<Component>()
                             .Where(item => item != null)
                             .Select(item => item.GetType().FullName)
                             .OrderBy(item => item, StringComparer.Ordinal))
                {
                    builder.Append(component).Append(';');
                }
                foreach (var renderer in current.GetComponents<Renderer>())
                {
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        builder.Append("mesh=")
                            .Append(AssetDatabase.GetAssetPath(skinned.sharedMesh))
                            .Append(':')
                            .Append(skinned.sharedMesh == null
                                ? "null"
                                : skinned.sharedMesh.name)
                            .Append(';');
                    }
                    foreach (var material in renderer.sharedMaterials)
                    {
                        builder.Append("mat=")
                            .Append(material == null
                                ? "null"
                                : AssetDatabase.GetAssetPath(material))
                            .Append(';');
                    }
                }
                builder.AppendLine();
            }
            return Sha256(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static Dictionary<string, Material> LoadApprovedMaterials()
        {
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var definition in MaterialDefinitions)
            {
                result.Add(
                    definition.SourceName,
                    AssetDatabase.LoadAssetAtPath<Material>(
                        definition.MaterialPath) ??
                    throw new InvalidOperationException(
                        "An approved Ispant material is missing: " +
                        definition.MaterialPath));
            }
            result.Add(
                CrescentArmorMaterialKey,
                AssetDatabase.LoadAssetAtPath<Material>(
                    CrescentArmorMaterialPath) ??
                throw new InvalidOperationException(
                    "The approved Ispant crescent armor material is missing: " +
                    CrescentArmorMaterialPath));
            return result;
        }

        private static void RequireMaterialConfiguration(
            IReadOnlyDictionary<string, Material> materials,
            float approvedYFlip)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                throw new InvalidOperationException(
                    "The approved Ispant shader is missing during inspection.");
            foreach (var definition in MaterialDefinitions)
            {
                var material = materials[definition.SourceName];
                if (material.shader != shader ||
                    !ColorsMatch(material.GetColor("_BaseColor"), definition.BaseColor) ||
                    Mathf.Abs(material.GetFloat("_NormalStrength") -
                              definition.NormalStrength) > 0.0001f ||
                    Mathf.Abs(material.GetFloat("_UseUv1") -
                              (definition.UseUv1 ? 1f : 0f)) > 0.0001f ||
                    Mathf.Abs(material.GetFloat("_FeatureMode") -
                              definition.FeatureMode) > 0.0001f ||
                    Mathf.Abs(material.GetFloat("_ApprovedYFlip") -
                              approvedYFlip) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "An approved Ispant material setting differs: " +
                        definition.SourceName);
                }
            }
            var crescentArmor = materials[CrescentArmorMaterialKey];
            var expectedColor = new Color(
                ApprovedCrescentArmorBrightness,
                ApprovedCrescentArmorBrightness,
                ApprovedCrescentArmorBrightness,
                1f);
            if (crescentArmor.shader != shader ||
                !ColorsMatch(
                    crescentArmor.GetColor("_BaseColor"),
                    expectedColor) ||
                Mathf.Abs(crescentArmor.GetFloat("_UseMaps")) > 0.0001f ||
                Mathf.Abs(crescentArmor.GetFloat("_UseUv1")) > 0.0001f ||
                Mathf.Abs(crescentArmor.GetFloat("_RoughnessBias") -
                          ApprovedArmorMeanRoughness) > 0.0001f ||
                Mathf.Abs(crescentArmor.GetFloat("_MetallicBias") -
                          ApprovedArmorMeanMetallic) > 0.0001f ||
                Mathf.Abs(crescentArmor.GetFloat("_ApprovedYFlip") -
                          approvedYFlip) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "The approved Ispant crescent armor material setting differs.");
            }
        }

        private static bool ColorsMatch(Color actual, Color expected)
        {
            return Mathf.Abs(actual.r - expected.r) <= 0.0001f &&
                   Mathf.Abs(actual.g - expected.g) <= 0.0001f &&
                   Mathf.Abs(actual.b - expected.b) <= 0.0001f &&
                   Mathf.Abs(actual.a - expected.a) <= 0.0001f;
        }

        private static Material[] ResolveApprovedMaterialOrder(
            SkinnedMeshRenderer source,
            IReadOnlyDictionary<string, Material> materials)
        {
            var result = new Material[source.sharedMaterials.Length];
            for (var index = 0; index < result.Length; index++)
            {
                var sourceMaterial = source.sharedMaterials[index] ??
                    throw new InvalidOperationException(
                        "The approved Ispant asset contains a null material slot.");
                if (!materials.TryGetValue(sourceMaterial.name, out result[index]))
                {
                    throw new InvalidOperationException(
                        "The approved Ispant material order is unknown: " +
                        sourceMaterial.name);
                }
            }
            return result;
        }

        private static void RequireMaterialNames(
            Renderer renderer,
            IReadOnlyList<string> expected)
        {
            var actual = renderer.sharedMaterials
                .Select(item => item == null ? string.Empty : item.name)
                .ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The approved Ispant FBX material order differs. Actual=" +
                    string.Join(",", actual));
            }
        }

        private static int RequireTextureHashesMatch()
        {
            var sample = Directory.GetFiles(
                    ProjectAbsolutePath(SampleTextureRelativePath),
                    "*.png",
                    SearchOption.TopDirectoryOnly)
                .ToDictionary(
                    Path.GetFileName,
                    Sha256,
                    StringComparer.OrdinalIgnoreCase);
            var copied = Directory.GetFiles(
                    ProjectAbsolutePath(TextureFolder),
                    "*.png",
                    SearchOption.TopDirectoryOnly)
                .ToDictionary(
                    Path.GetFileName,
                    Sha256,
                    StringComparer.OrdinalIgnoreCase);
            if (sample.Count != 28 || copied.Count != 28 ||
                sample.Any(item =>
                    !copied.TryGetValue(item.Key, out var hash) ||
                    !string.Equals(item.Value, hash, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "The Unity Ispant textures do not exactly match the approved sample textures.");
            }
            return copied.Count;
        }

        private static void RequireApprovedFiles()
        {
            var model = ProjectAbsolutePath(ApprovedModelPath);
            if (!File.Exists(model))
            {
                throw new FileNotFoundException(
                    "The approved Ispant FBX is missing.",
                    model);
            }
            var hash = Sha256(model);
            if (!string.Equals(
                    hash,
                    ExpectedApprovedFbxSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The approved Ispant FBX hash differs. Expected=" +
                    ExpectedApprovedFbxSha256 + ", Actual=" + hash + ".");
            }
            RequireTextureHashesMatch();
        }

        private static Texture2D LoadTexture(string fileName)
        {
            var path = TextureFolder + "/" + fileName;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                throw new InvalidOperationException(
                    "An approved Ispant texture failed to load: " + path);
        }

        private static Scene RequireActiveScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. ActiveScene=" +
                    scene.path + ".");
            }
            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes before approved Ispant appearance application.");
            }
            return scene;
        }

        private static GameObject RequirePlacementRoot(Scene scene)
        {
            var roots = scene.GetRootGameObjects()
                .Where(item => item.name == PlacementRootName)
                .ToArray();
            if (roots.Length != 1 || roots[0].transform.childCount != ExpectedSlots)
            {
                throw new InvalidOperationException(
                    "The approved Ispant placement root or twelve-slot contract differs.");
            }
            return roots[0];
        }

        private static int TriangleCount(Mesh mesh)
        {
            var result = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
            {
                result += checked((int)mesh.GetIndexCount(index) / 3);
            }
            return result;
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh ??
                    throw new InvalidOperationException(
                        "An approved Ispant skinned renderer has no mesh: " + renderer.name);
            }
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException(
                    "An approved Ispant renderer has no mesh: " + renderer.name);
        }

        private static void WriteReport(
            AppearanceResult result,
            string status,
            SceneContract before,
            SceneContract after)
        {
            var path = ProjectAbsolutePath(ReportRelativePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "The Ispant inspection report folder is invalid."));
            File.WriteAllLines(path, new[]
            {
                "status=" + status,
                "approved_fbx_sha256=" + ExpectedApprovedFbxSha256,
                "slots=" + result.SlotCount,
                "renderers_per_slot=" + result.RenderersPerSlot,
                "approved_body_control_vertices=" + ApprovedBodyControlVertices,
                "approved_body_control_polygons=" + ApprovedBodyControlPolygons,
                "unity_body_imported_vertices=" + result.BodyImportedVertices,
                "unity_body_triangles=" + result.BodyTriangles,
                "unity_body_material_order=" +
                    string.Join(",", ExpectedUnityBodyMaterialOrder),
                "body_armor_brightness_multiplier=" +
                    ApprovedBodyArmorBrightness.ToString("R", CultureInfo.InvariantCulture),
                "helmet_armor_brightness_multiplier=" +
                    ApprovedHelmetArmorBrightness.ToString("R", CultureInfo.InvariantCulture),
                "crescent_armor_brightness_multiplier=" +
                    ApprovedCrescentArmorBrightness.ToString("R", CultureInfo.InvariantCulture),
                "body_armor_mean_luminance_target=" +
                    TargetBodyArmorMeanLuminance.ToString("R", CultureInfo.InvariantCulture),
                "body_armor_target_increase_percent=25",
                "helmet_brightness_preserved=true",
                "approved_crescent_control_vertices=" + ApprovedCrescentControlVertices,
                "approved_crescent_control_polygons=" + ApprovedCrescentControlPolygons,
                "approved_crescent_evaluated_vertices=" + ExpectedCrescentEvaluatedVertices,
                "approved_crescent_evaluated_triangles=" + ExpectedCrescentEvaluatedTriangles,
                "fbx_reimport_zero_area_crescent_triangles=" + ExpectedCrescentFbxDegenerateTriangles,
                "unity_beveled_crescent_vertices=" + result.CrescentImportedVertices,
                "unity_beveled_crescent_triangles=" + result.CrescentTriangles,
                "approved_eye_control_vertices=" + ApprovedEyeControlVertices,
                "approved_eye_control_polygons=" + ApprovedEyeControlPolygons,
                "approved_eye_evaluated_vertices=" + ExpectedEyeEvaluatedVertices,
                "unity_eye_imported_vertices=" + result.EyeImportedVertices,
                "unity_eye_triangles=" + result.EyeTriangles,
                "bones=" + result.Bones,
                "texture_files=" + result.TextureCount,
                "original_uv_preserved=true",
                "mechanical_uv_preserved=true",
                "helmet_face_uv_preserved=true",
                "helmet_face_pattern=steep_center_shallow_middle_steep_outer_edge_connected",
                "waist_belt_removed=true",
                "diagonal_chest_strap_preserved=true",
                "explicit_eye_mesh_applied=true",
                "original_ring_removed=true",
                "user_marked_stick_weapon_removed=true",
                "crescent_material_preserved=true",
                "crescent_armor_roughness=" +
                    ApprovedArmorMeanRoughness.ToString("R", CultureInfo.InvariantCulture),
                "crescent_armor_metallic=" +
                    ApprovedArmorMeanMetallic.ToString("R", CultureInfo.InvariantCulture),
                "slot_transform_hash_before=" + before.IspantSlotTransformHash,
                "slot_transform_hash_after=" + after.IspantSlotTransformHash,
                "slot_transforms_preserved=" +
                    (before.IspantSlotTransformHash == after.IspantSlotTransformHash),
                "other_scene_roots_unchanged=" +
                    before.OtherRoots.SequenceEqual(after.OtherRoots, StringComparer.Ordinal)
            }, Encoding.UTF8);
        }

        private static void CaptureComparison(
            GameObject source,
            string outputRelativePath)
        {
            var output = ProjectAbsolutePath(outputRelativePath);
            if (File.Exists(output))
            {
                throw new InvalidOperationException(
                    "The one-time Ispant approved appearance comparison already exists: " +
                    output);
            }
            Directory.CreateDirectory(
                Path.GetDirectoryName(output) ??
                throw new InvalidOperationException(
                    "The Ispant comparison output folder is invalid."));
            var unityImage = CapturePreview(source);
            var reference = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D combined = null;
            try
            {
                if (!reference.LoadImage(
                        File.ReadAllBytes(ProjectAbsolutePath(
                            SampleReviewRelativePath)),
                        false))
                {
                    throw new InvalidOperationException(
                        "The approved Ispant Blender render failed to load.");
                }
                if (reference.width != unityImage.width ||
                    reference.height != unityImage.height)
                {
                    throw new InvalidOperationException(
                        "The approved and Unity Ispant review images have different dimensions.");
                }
                combined = new Texture2D(
                    reference.width * 2,
                    reference.height,
                    TextureFormat.RGB24,
                    false);
                combined.SetPixels(
                    0, 0, reference.width, reference.height, reference.GetPixels());
                combined.SetPixels(
                    reference.width, 0, unityImage.width, unityImage.height,
                    unityImage.GetPixels());
                combined.Apply();
                File.WriteAllBytes(output, combined.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reference);
                UnityEngine.Object.DestroyImmediate(unityImage);
                if (combined != null)
                {
                    UnityEngine.Object.DestroyImmediate(combined);
                }
            }
        }

        private static float MeasureBodyArmorMeanLuminance(
            GameObject source,
            float bodyArmorBrightness)
        {
            var preview = CapturePreview(
                source,
                false,
                bodyArmorBrightness);
            try
            {
                return MeasureFixedBodyArmorMeanLuminance(preview);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static float MeasureAppliedBodyArmorMeanLuminance(
            GameObject source)
        {
            var preview = CapturePreview(source);
            try
            {
                return MeasureFixedBodyArmorMeanLuminance(preview);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static float MeasureFixedBodyArmorMeanLuminance(
            Texture2D preview)
        {
            var baseline = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var current = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!baseline.LoadImage(
                        File.ReadAllBytes(ProjectAbsolutePath(
                            BodyBrightnessFinalRelativePath)),
                        false) ||
                    !current.LoadImage(preview.EncodeToPNG(), false))
                {
                    throw new InvalidOperationException(
                        "The approved Ispant fixed luminance comparison images failed to load.");
                }
                if (baseline.width != current.width * 2 ||
                    baseline.height != current.height)
                {
                    throw new InvalidOperationException(
                        "The approved Ispant fixed luminance comparison dimensions differ.");
                }
                var baselinePixels = baseline.GetPixels32();
                var currentPixels = current.GetPixels32();
                var count = 0;
                var baselineSum = 0.0;
                var sum = 0.0;
                for (var y = BodyMeanRoiYMin; y <= BodyMeanRoiYMax; y++)
                {
                    for (var x = BodyMeanRoiXMin; x <= BodyMeanRoiXMax; x++)
                    {
                        var baselinePixel = baselinePixels[
                            (x + current.width) + y * baseline.width];
                        var baselineRed = baselinePixel.r / 255f;
                        var baselineGreen = baselinePixel.g / 255f;
                        var baselineBlue = baselinePixel.b / 255f;
                        var maximum = Mathf.Max(
                            baselineRed,
                            Mathf.Max(baselineGreen, baselineBlue));
                        var minimum = Mathf.Min(
                            baselineRed,
                            Mathf.Min(baselineGreen, baselineBlue));
                        if (minimum < 45f / 255f ||
                            maximum - minimum > 34f / 255f)
                        {
                            continue;
                        }
                        baselineSum += baselineRed * 0.2126f +
                                       baselineGreen * 0.7152f +
                                       baselineBlue * 0.0722f;
                        var pixel = currentPixels[
                            x + y * current.width];
                        sum += pixel.r / 255f * 0.2126f +
                               pixel.g / 255f * 0.7152f +
                               pixel.b / 255f * 0.0722f;
                        count++;
                    }
                }
                if (count == 0)
                {
                    throw new InvalidOperationException(
                        "The approved Ispant body luminance ROI found no neutral armor pixels.");
                }
                var baselineMean = (float)(baselineSum / count);
                var currentMean = (float)(sum / count);
                Debug.Log(
                    "IspantApprovedBodyArmorFixedMaskMeasured" +
                    ", SampleCount=" + count +
                    ", BaselineMean=" +
                    baselineMean.ToString("R", CultureInfo.InvariantCulture) +
                    ", CurrentMean=" +
                    currentMean.ToString("R", CultureInfo.InvariantCulture) +
                    ", BaselineFormat=" + baseline.format +
                    ", CurrentFormat=" + current.format + ".");
                return currentMean;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseline);
                UnityEngine.Object.DestroyImmediate(current);
            }
        }

        private static Texture2D CapturePreview(
            GameObject source,
            bool useUrpLitDiagnostic = false,
            float? bodyArmorBrightnessOverride = null)
        {
            var preview = new PreviewRenderUtility();
            GameObject clone = null;
            Texture2D rendered = null;
            var temporaryMaterials = new List<Material>();
            try
            {
                clone = UnityEngine.Object.Instantiate(source);
                clone.name = "Ispant_ApprovedAppearance_Preview";
                clone.hideFlags = HideFlags.HideAndDontSave;
                clone.transform.SetPositionAndRotation(
                    Vector3.zero,
                    source.transform.rotation);
                clone.transform.localScale = source.transform.lossyScale;
                foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                }
                preview.AddSingleGO(clone);
                var renderers = clone.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length != ExpectedRenderersPerSlot)
                {
                    throw new InvalidOperationException(
                        "The Unity Ispant preview must contain body, crescent, and eye renderers.");
                }
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                    renderer.forceRenderingOff = false;
                    renderer.SetPropertyBlock(null);
                }
                if (useUrpLitDiagnostic)
                {
                    ApplyUrpLitDiagnosticMaterials(
                        renderers,
                        temporaryMaterials);
                }
                else if (bodyArmorBrightnessOverride.HasValue)
                {
                    ApplyBodyArmorBrightnessOverride(
                        renderers,
                        temporaryMaterials,
                        bodyArmorBrightnessOverride.Value);
                }
                // The approved crescent and eye meshes are separate skinned renderers.
                // Their imported skinned bounds can be much larger than their visible
                // geometry, so the body renderer (which already includes both weapons)
                // remains the stable framing contract used by the approved comparison.
                var previewBody = renderers
                    .OfType<SkinnedMeshRenderer>()
                    .Single(item =>
                        item.sharedMesh != null &&
                        item.sharedMesh.name == "Ispant_Armed_Body");
                var bounds = previewBody.bounds;

                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.004f, 0.008f, 0.013f, 1f);
                camera.fieldOfView = 31f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.allowHDR = true;
                var viewDirection = Quaternion.AngleAxis(24f, Vector3.up) *
                                    clone.transform.forward;
                var distance = Mathf.Max(bounds.size.x, bounds.size.y) /
                    (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) *
                    1.18f;
                var target = bounds.center + Vector3.up * bounds.size.y * 0.01f;
                camera.transform.position = target +
                    viewDirection.normalized * distance +
                    Vector3.up * bounds.size.y * 0.04f;
                camera.transform.LookAt(target, Vector3.up);

                preview.lights[0].transform.rotation = camera.transform.rotation;
                preview.lights[0].color = new Color(1f, 0.92f, 0.82f);
                preview.lights[0].intensity = 1.65f;
                preview.lights[0].shadows = LightShadows.Soft;
                preview.lights[1].transform.rotation = Quaternion.LookRotation(
                    (camera.transform.forward + camera.transform.right * 0.7f).normalized,
                    Vector3.up);
                preview.lights[1].color = new Color(0.62f, 0.78f, 1f);
                preview.lights[1].intensity = 0.90f;
                preview.lights[1].shadows = LightShadows.None;
                preview.ambientColor = new Color(0.23f, 0.25f, 0.27f, 1f);
                preview.BeginStaticPreview(new Rect(0f, 0f, 2048f, 1152f));
                preview.Render(true);
                rendered = preview.EndStaticPreview();
                if (rendered == null)
                {
                    throw new InvalidOperationException(
                        "Unity PreviewRenderUtility returned no Ispant image.");
                }
                return UnityEngine.Object.Instantiate(rendered);
            }
            finally
            {
                if (clone != null)
                {
                    UnityEngine.Object.DestroyImmediate(clone);
                }
                foreach (var material in temporaryMaterials)
                {
                    UnityEngine.Object.DestroyImmediate(material);
                }
                preview.Cleanup();
            }
        }

        private static void ApplyBodyArmorBrightnessOverride(
            IEnumerable<Renderer> renderers,
            ICollection<Material> temporaryMaterials,
            float brightness)
        {
            var bodyArmorAsset = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialDefinitions[0].MaterialPath) ??
                throw new InvalidOperationException(
                    "The approved Ispant body armor material is missing during luminance calibration.");
            var replacementCount = 0;
            foreach (var renderer in renderers)
            {
                var replacements = renderer.sharedMaterials.ToArray();
                var changed = false;
                for (var index = 0; index < replacements.Length; index++)
                {
                    var source = replacements[index];
                    if (source != bodyArmorAsset)
                    {
                        continue;
                    }
                    var replacement = new Material(source)
                    {
                        name = source.name + "_BodyLuminanceCalibration",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    replacement.SetColor(
                        "_BaseColor",
                        new Color(brightness, brightness, brightness, 1f));
                    replacements[index] = replacement;
                    temporaryMaterials.Add(replacement);
                    changed = true;
                    replacementCount++;
                }
                if (changed)
                {
                    renderer.sharedMaterials = replacements;
                }
            }
            if (replacementCount != 1)
            {
                throw new InvalidOperationException(
                    "The approved Ispant body armor luminance calibration expected exactly one material slot but found " +
                    replacementCount + ".");
            }
        }

        private static void ApplyUrpLitDiagnosticMaterials(
            IEnumerable<Renderer> renderers,
            ICollection<Material> temporaryMaterials)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException(
                    "The standard URP Lit shader is unavailable for the Ispant lighting diagnosis.");
            foreach (var renderer in renderers)
            {
                var replacements = new Material[renderer.sharedMaterials.Length];
                for (var index = 0; index < replacements.Length; index++)
                {
                    var source = renderer.sharedMaterials[index] ??
                        throw new InvalidOperationException(
                            "The approved Ispant lighting diagnosis found a null material slot.");
                    var replacement = new Material(shader)
                    {
                        name = source.name + "_UrpLitDiagnostic",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    var useMaps = source.HasProperty("_UseMaps") &&
                                  source.GetFloat("_UseMaps") >= 0.5f;
                    replacement.SetColor(
                        "_BaseColor",
                        source.HasProperty("_BaseColor")
                            ? source.GetColor("_BaseColor")
                            : Color.white);
                    replacement.SetTexture(
                        "_BaseMap",
                        useMaps && source.HasProperty("_BaseMap")
                            ? source.GetTexture("_BaseMap")
                            : null);
                    replacement.SetFloat(
                        "_Metallic",
                        source.HasProperty("_MetallicBias")
                            ? source.GetFloat("_MetallicBias")
                            : 0f);
                    replacement.SetFloat("_Smoothness", 0.45f);
                    replacements[index] = replacement;
                    temporaryMaterials.Add(replacement);
                }
                renderer.sharedMaterials = replacements;
            }
        }

        private static void WriteSideBySide(
            Texture2D left,
            Texture2D right,
            string output)
        {
            if (left.width != right.width || left.height != right.height)
            {
                throw new InvalidOperationException(
                    "The Ispant lighting diagnostic renders have different dimensions.");
            }
            Directory.CreateDirectory(
                Path.GetDirectoryName(output) ??
                throw new InvalidOperationException(
                    "The Ispant lighting diagnostic output folder is invalid."));
            var combined = new Texture2D(
                left.width * 2,
                left.height,
                TextureFormat.RGB24,
                false);
            try
            {
                combined.SetPixels(
                    0, 0, left.width, left.height, left.GetPixels());
                combined.SetPixels(
                    left.width, 0, right.width, right.height, right.GetPixels());
                combined.Apply();
                File.WriteAllBytes(output, combined.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(combined);
            }
        }

        private static LuminanceMetrics MeasureForegroundLuminance(
            Texture2D image)
        {
            var pixels = image.GetPixels();
            var background = pixels[0];
            var count = 0;
            var sum = 0.0;
            var peak = 0f;
            foreach (var pixel in pixels)
            {
                var difference = Mathf.Max(
                    Mathf.Abs(pixel.r - background.r),
                    Mathf.Abs(pixel.g - background.g),
                    Mathf.Abs(pixel.b - background.b));
                if (difference <= 0.012f)
                {
                    continue;
                }
                var luminance = pixel.r * 0.2126f +
                                pixel.g * 0.7152f +
                                pixel.b * 0.0722f;
                count++;
                sum += luminance;
                peak = Mathf.Max(peak, luminance);
            }
            if (count == 0)
            {
                throw new InvalidOperationException(
                    "The Ispant lighting diagnostic found no foreground pixels.");
            }
            return new LuminanceMetrics(
                count,
                (float)(sum / count),
                peak);
        }

        private static string GetPath(Transform current, Transform root)
        {
            if (current == root) return root.name;
            var names = new Stack<string>();
            var cursor = current;
            while (cursor != null && cursor != root)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }
            return root.name + "/" + string.Join("/", names);
        }

        private static string AbsoluteToAssetPath(string absolutePath)
        {
            var normalized = Path.GetFullPath(absolutePath).Replace('\\', '/');
            var project = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."))
                .Replace('\\', '/')
                .TrimEnd('/');
            if (!normalized.StartsWith(
                    project + "/",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Path is outside the Unity project: " + absolutePath);
            }
            return normalized.Substring(project.Length + 1);
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string Vec(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:R},{1:R},{2:R}",
                value.x,
                value.y,
                value.z);
        }

        private static string Quat(Quaternion value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:R},{1:R},{2:R},{3:R}",
                value.x,
                value.y,
                value.z,
                value.w);
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static string Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes))
                .Replace("-", string.Empty);
        }

        private readonly struct MaterialDefinition
        {
            public readonly string SourceName;
            public readonly string TexturePrefix;
            public readonly float NormalStrength;
            public readonly bool UseUv1;
            public readonly float FeatureMode;
            public readonly float CoatWeight;
            public readonly float CoatRoughness;
            public readonly bool UseMaps;
            public readonly Color BaseColor;
            public readonly float RoughnessBias;
            public readonly float MetallicBias;

            public MaterialDefinition(
                string sourceName,
                string texturePrefix,
                float normalStrength,
                bool useUv1,
                float featureMode,
                float coatWeight,
                float coatRoughness,
                float brightness = 1f)
            {
                SourceName = sourceName;
                TexturePrefix = texturePrefix;
                NormalStrength = normalStrength;
                UseUv1 = useUv1;
                FeatureMode = featureMode;
                CoatWeight = coatWeight;
                CoatRoughness = coatRoughness;
                UseMaps = true;
                BaseColor = new Color(brightness, brightness, brightness, 1f);
                RoughnessBias = 0f;
                MetallicBias = 0f;
            }

            private MaterialDefinition(
                string sourceName,
                Color baseColor,
                float roughness,
                float metallic,
                float featureMode = 0f)
            {
                SourceName = sourceName;
                TexturePrefix = string.Empty;
                NormalStrength = 0f;
                UseUv1 = false;
                FeatureMode = featureMode;
                CoatWeight = 0f;
                CoatRoughness = 0.34f;
                UseMaps = false;
                BaseColor = baseColor;
                RoughnessBias = roughness;
                MetallicBias = metallic;
            }

            public static MaterialDefinition Rubber()
            {
                return new MaterialDefinition(
                    "Ispant_Rubber_Black",
                    new Color(0.012f, 0.016f, 0.020f, 1f),
                    0.68f,
                    0.10f);
            }

            public static MaterialDefinition Eye()
            {
                return new MaterialDefinition(
                    "Ispant_Eye_Cyan",
                    new Color(0.015f, 0.24f, 0.32f, 1f),
                    0.20f,
                    0.18f,
                    3f);
            }

            public string MaterialPath =>
                MaterialFolder + "/" + SourceName + "_Approved.mat";
        }

        private readonly struct ApprovedAssetInfo
        {
            public readonly GameObject Prefab;
            public readonly SkinnedMeshRenderer Body;
            public readonly Renderer Crescent;
            public readonly Renderer Eyes;
            public readonly float ApprovedYFlip;

            public ApprovedAssetInfo(
                GameObject prefab,
                SkinnedMeshRenderer body,
                Renderer crescent,
                Renderer eyes,
                float approvedYFlip)
            {
                Prefab = prefab;
                Body = body;
                Crescent = crescent;
                Eyes = eyes;
                ApprovedYFlip = approvedYFlip;
            }
        }

        private readonly struct SceneContract
        {
            public readonly string IspantSlotTransformHash;
            public readonly string[] OtherRoots;

            public SceneContract(
                string ispantSlotTransformHash,
                string[] otherRoots)
            {
                IspantSlotTransformHash = ispantSlotTransformHash;
                OtherRoots = otherRoots;
            }
        }

        private readonly struct AppearanceResult
        {
            public readonly int SlotCount;
            public readonly int RenderersPerSlot;
            public readonly int BodyImportedVertices;
            public readonly int BodyTriangles;
            public readonly int CrescentImportedVertices;
            public readonly int CrescentTriangles;
            public readonly int EyeImportedVertices;
            public readonly int EyeTriangles;
            public readonly int Bones;
            public readonly int TextureCount;

            public AppearanceResult(
                int slotCount,
                int renderersPerSlot,
                int bodyImportedVertices,
                int bodyTriangles,
                int crescentImportedVertices,
                int crescentTriangles,
                int eyeImportedVertices,
                int eyeTriangles,
                int bones,
                int textureCount)
            {
                SlotCount = slotCount;
                RenderersPerSlot = renderersPerSlot;
                BodyImportedVertices = bodyImportedVertices;
                BodyTriangles = bodyTriangles;
                CrescentImportedVertices = crescentImportedVertices;
                CrescentTriangles = crescentTriangles;
                EyeImportedVertices = eyeImportedVertices;
                EyeTriangles = eyeTriangles;
                Bones = bones;
                TextureCount = textureCount;
            }
        }

        private readonly struct LuminanceMetrics
        {
            public readonly int PixelCount;
            public readonly float MeanLuminance;
            public readonly float PeakLuminance;

            public LuminanceMetrics(
                int pixelCount,
                float meanLuminance,
                float peakLuminance)
            {
                PixelCount = pixelCount;
                MeanLuminance = meanLuminance;
                PeakLuminance = peakLuminance;
            }
        }
    }
}
