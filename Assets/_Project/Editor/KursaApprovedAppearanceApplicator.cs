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

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaApprovedAppearanceApplicator
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Kursa Enemy Placement";
        private const string ModelName = "Kursa_Model";
        private const string SourceModelPath =
            "Assets/_Project/Art/Enemies/Kursa/Models/Kursa.fbx";
        private const string AppearanceRoot =
            "Assets/_Project/Art/Enemies/Kursa/ApprovedAppearance";
        private const string ApprovedModelPath =
            AppearanceRoot + "/Models/Kursa_Appearance_ReferenceSync.fbx";
        private const string RuntimeProjectionModelPath =
            AppearanceRoot + "/Models/Kursa_Appearance_RuntimeProjection.fbx";
        private const string ApprovedIdleControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_02_GroundedIdle.controller";
        private const string TextureFolder = AppearanceRoot + "/Textures";
        private const string MaterialFolder = AppearanceRoot + "/Materials";
        private const string ShaderPath =
            AppearanceRoot + "/Shaders/KursaApprovedAppearance.shader";
        private const string SampleTextureRelativePath =
            "artSample/enemies/kursa/appearance_reference_sync/textures";
        private const string SampleReviewRelativePath =
            "artSample/enemies/kursa/appearance_reference_sync/renders/02_three_quarter_kursa_reference_match.png";
        private const string ReportRelativePath =
            "docs/validation/kursa_approved_appearance_2026-08-02/Kursa_ApprovedAppearance_Inspection.txt";
        private const string RuntimeProjectionReportRelativePath =
            "docs/validation/kursa_approved_appearance_2026-08-02/Kursa_RuntimeProjection_Export.json";
        private const string CaptureRelativePath =
            "docs/validation/kursa_approved_appearance_2026-08-02/Kursa_ApprovedAppearance_UnityComparison.png";
        private const string ExpectedSourceSha256 =
            "C1FD1C872ADA95B597DC2F93C9BFC523E5A7E88410541F85B2F6B2DA2F7D18A7";
        private const string ExpectedApprovedFbxSha256 =
            "6BEC48F1822B4815ACD18E40006BD8D0567B5E4E67611A692B12F038A3942F9E";
        private const string ExpectedRuntimeProjectionFbxSha256 =
            "7C39FC6B7587E66A73899C282ABAFAC7671ADEFF84EC2304548795EF6A9E639C";
        private const int ExpectedSlots = 12;
        private const int ApprovedSampleVertices = 2109;
        private const int ExpectedUnityImportedVertices = 3372;
        private const int ExpectedUnityRuntimeVertices = 3376;
        private const int ExpectedTriangles = 3913;
        private const int ExpectedBones = 24;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review",
            "Kursa_02_Idle",
            "Kursa_03_Move",
            "Kursa_04_ShieldBash",
            "Kursa_05_ToShieldStance",
            "Kursa_06_ShieldStance",
            "Kursa_07_ShieldStanceMove",
            "Kursa_08_FromShieldStance",
            "Kursa_09_Stop",
            "Kursa_10_Hit",
            "Kursa_11_Death",
            "Kursa_12_ShieldBreakReaction"
        };

        private static readonly string[] ApprovedPoseBoneNames =
        {
            "LeftArm",
            "LeftForeArm",
            "LeftHand"
        };

        private static readonly MaterialDefinition[] MaterialDefinitions =
        {
            new("armor_gunmetal", "Kursa_Armor_Gunmetal", 0.46f, 1.25f, Feature.Basic),
            new("armor_bluegray", "Kursa_Armor_BlueGray", 0.48f, 1.25f, Feature.Basic),
            new("light_steel", "Kursa_Light_Steel", 0.40f, 1.25f, Feature.Basic),
            new("dark_mechanics", "Kursa_Dark_Mechanics", 0.58f, 1.25f, Feature.Basic),
            new("torso_mechanical", "Kursa_Torso_Mechanical_Plates", 0.54f, 1.25f, Feature.Torso),
            new("hood_cloth", "Kursa_Hood_Navy_Cloth", 0.18f, 1.25f, Feature.Hood),
            new("face_metal", "Kursa_Face_Metal_Blue_Optics", 0.30f, 1.25f, Feature.Face),
            new("shield_worn", "Kursa_Shield_Worn_Gunmetal", 0.70f, 1.05f, Feature.Basic),
            new("shield_frame", "Kursa_Shield_Frame_Steel", 0.44f, 1.25f, Feature.Basic)
        };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Approved Appearance")]
        public static void ApplyApprovedKursaAppearance()
        {
            var scene = RequireActiveScene(requireClean: true);
            var root = RequirePlacementRoot(scene);
            var before = CaptureContract(scene, root);

            RequireApprovedSourceFiles();
            ConfigureTextureAssets();
            ConfigureRuntimeProjectionModelImporter();
            var sourceAssetRenderer = RequireAssetRenderer(SourceModelPath);
            var approvedAssetRenderer = RequireAssetRenderer(ApprovedModelPath);
            var runtimeAssetRenderer = RequireAssetRenderer(RuntimeProjectionModelPath);
            RequireGeometryParity(
                sourceAssetRenderer,
                approvedAssetRenderer,
                "source and approved FBX");
            RequireApprovedShieldBasePose(
                approvedAssetRenderer,
                runtimeAssetRenderer,
                "approved and runtime-projection FBX");
            RequireAnimationParity(ApprovedModelPath, RuntimeProjectionModelPath);
            RequireRuntimeEyeProjectionChannels(runtimeAssetRenderer);
            var materials = CreateOrUpdateMaterials();
            var orderedMaterials = ResolveApprovedMaterialOrder(
                runtimeAssetRenderer,
                materials);

            foreach (var renderer in RequireSceneRenderers(root))
            {
                if (renderer.sharedMesh != sourceAssetRenderer.sharedMesh &&
                    renderer.sharedMesh != approvedAssetRenderer.sharedMesh &&
                    renderer.sharedMesh != runtimeAssetRenderer.sharedMesh)
                {
                    throw new InvalidOperationException(
                        "A placed Kursa renderer uses an unapproved mesh before application: " +
                        renderer.name);
                }
                RequireBoneOrder(renderer, runtimeAssetRenderer);
                renderer.sharedMesh = runtimeAssetRenderer.sharedMesh;
                renderer.sharedMaterials = orderedMaterials;
                ApplyApprovedBindPoseTransforms(
                    renderer,
                    runtimeAssetRenderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
            }

            var after = CaptureContract(scene, root);
            RequireContractPreserved(before, after);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the approved Kursa appearance.");
            }

            AssetDatabase.SaveAssets();
            var result = InspectAppliedState(scene, root, materials);
            WriteReport(result, "APPLY_PASS");
            Debug.Log(
                "KursaApprovedAppearanceApplied Result=PASS" +
                ", Slots=" + result.SlotCount +
                ", MaterialsPerRenderer=" + result.MaterialCount +
                ", Vertices=" + result.VertexCount +
                ", Triangles=" + result.TriangleCount +
                ", Bones=" + result.BoneCount +
                ", TextureFiles=" + result.TextureCount +
                ", EyeProjectionVertices=" + result.LeftEyeProjectionCount +
                "/" + result.RightEyeProjectionCount +
                ", TextureHashesMatch=True" +
                ", TopologyUv0WeightsAnimationPreserved=True" +
                ", ShieldBasePoseForwardAngle=" +
                    result.ShieldForwardAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", ShieldBasePoseVerticalAngle=" +
                    result.ShieldVerticalAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", ShieldCenterShift=" +
                    result.ShieldCenterShift.ToString("R", CultureInfo.InvariantCulture) +
                ", ShieldFrontOffset=" +
                    result.ShieldFrontOffset.ToString("R", CultureInfo.InvariantCulture) +
                ", ShieldTorsoLateralGap=" +
                    result.ShieldTorsoLateralGap.ToString("R", CultureInfo.InvariantCulture) +
                ", ShieldTorsoLateralGapRatio=" +
                    result.ShieldTorsoLateralGapRatio.ToString("R", CultureInfo.InvariantCulture) +
                ", RightUpperDownAngle=" +
                    result.RightUpperDownAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", RightForearmDownAngle=" +
                    result.RightForearmDownAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", RightArmTorsoGap=" +
                    result.RightArmLateralGap.ToString("R", CultureInfo.InvariantCulture) +
                ", RightArmMinimumHeight=" +
                    result.RightArmMinimumHeight.ToString("R", CultureInfo.InvariantCulture) +
                ", SlotAndNonApprovedArmTransformsPreserved=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Approved Appearance")]
        public static void InspectApprovedKursaAppearance()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var result = InspectAppliedState(
                scene,
                root,
                LoadApprovedMaterials());
            WriteReport(result, "INSPECTION_PASS");
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Kursa appearance inspection changed the scene dirty state.");
            }

            Debug.Log(
                "KursaApprovedAppearanceInspected Result=PASS" +
                ", ActiveScene=" + scene.path +
                ", Slots=" + result.SlotCount +
                ", MaterialsPerRenderer=" + result.MaterialCount +
                ", Vertices=" + result.VertexCount +
                ", Triangles=" + result.TriangleCount +
                ", Bones=" + result.BoneCount +
                ", TextureFiles=" + result.TextureCount +
                ", EyeProjectionVertices=" + result.LeftEyeProjectionCount +
                "/" + result.RightEyeProjectionCount +
                ", ShieldBasePoseForwardAngle=" +
                    result.ShieldForwardAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", ShieldBasePoseVerticalAngle=" +
                    result.ShieldVerticalAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", ShieldCenterShift=" +
                    result.ShieldCenterShift.ToString("R", CultureInfo.InvariantCulture) +
                ", ShieldFrontOffset=" +
                    result.ShieldFrontOffset.ToString("R", CultureInfo.InvariantCulture) +
                ", ShieldTorsoLateralGap=" +
                    result.ShieldTorsoLateralGap.ToString("R", CultureInfo.InvariantCulture) +
                ", ShieldTorsoLateralGapRatio=" +
                    result.ShieldTorsoLateralGapRatio.ToString("R", CultureInfo.InvariantCulture) +
                ", RightUpperDownAngle=" +
                    result.RightUpperDownAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", RightForearmDownAngle=" +
                    result.RightForearmDownAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", RightArmTorsoGap=" +
                    result.RightArmLateralGap.ToString("R", CultureInfo.InvariantCulture) +
                ", RightArmMinimumHeight=" +
                    result.RightArmMinimumHeight.ToString("R", CultureInfo.InvariantCulture) +
                ", TextureHashesMatch=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Approved Appearance Review")]
        public static void CaptureApprovedKursaAppearanceReview()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            InspectAppliedState(scene, root, LoadApprovedMaterials());
            var source = root.transform.Find(SlotNames[0] + "/" + ModelName);
            if (source == null)
            {
                throw new InvalidOperationException(
                    "The static-review Kursa model is missing.");
            }

            CaptureComparison(source.gameObject);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Kursa appearance capture changed the scene dirty state.");
            }

            Debug.Log(
                "KursaApprovedAppearanceReviewCaptured Result=PASS" +
                ", Image=" + CaptureRelativePath +
                ", Left=ApprovedBlenderSample" +
                ", Right=UnityApprovedAppearance" +
                ", SceneChanged=False.");
        }

        private static void ConfigureRuntimeProjectionModelImporter()
        {
            AssetDatabase.ImportAsset(
                RuntimeProjectionModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(RuntimeProjectionModelPath)
                as ModelImporter ??
                throw new InvalidOperationException(
                    "Kursa runtime projection ModelImporter is missing.");
            importer.importAnimation = true;
            importer.importBlendShapes = true;
            importer.optimizeGameObjects = false;
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
                        "Approved Kursa texture importer is missing: " + assetPath);
                var fileName = Path.GetFileName(assetPath);
                var normalMap = fileName.EndsWith(
                    "_normal.png",
                    StringComparison.OrdinalIgnoreCase);
                var nonColor =
                    normalMap ||
                    fileName.EndsWith(
                        "_roughness.png",
                        StringComparison.OrdinalIgnoreCase) ||
                    fileName.EndsWith(
                        "_metallic.png",
                        StringComparison.OrdinalIgnoreCase);
                var clamp = fileName.IndexOf(
                    "reference",
                    StringComparison.OrdinalIgnoreCase) >= 0;
                importer.textureType = normalMap
                    ? TextureImporterType.NormalMap
                    : TextureImporterType.Default;
                importer.sRGBTexture = !nonColor;
                importer.alphaIsTransparency = false;
                importer.mipmapEnabled = true;
                importer.wrapMode = clamp
                    ? TextureWrapMode.Clamp
                    : TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, Material> CreateOrUpdateMaterials()
        {
            AssetDatabase.ImportAsset(
                ShaderPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                throw new InvalidOperationException(
                    "Approved Kursa shader is missing: " + ShaderPath);
            if (!shader.isSupported)
            {
                throw new InvalidOperationException(
                    "Approved Kursa shader is not supported by the active renderer.");
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

                ConfigureMaterial(material, definition);
                EditorUtility.SetDirty(material);
                result.Add(definition.DisplayName, material);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static void ConfigureMaterial(
            Material material,
            MaterialDefinition definition)
        {
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_TextureScale", definition.TextureScale);
            material.SetFloat("_NormalStrength", definition.NormalStrength);
            material.SetFloat("_FeatureMode", (float)definition.Feature);
            material.SetFloat("_ApprovedAmbientStrength", 0.88f);
            material.SetFloat("_ApprovedKeyStrength", 3.00f);
            material.SetFloat("_ApprovedFillStrength", 1.35f);
            material.SetFloat("_ApprovedRimStrength", 2.10f);
            material.SetFloat("_PreviewUnlit", 0f);
            material.SetTexture(
                "_BaseMap",
                LoadTexture("kursa_" + definition.Id + "_albedo.png"));
            material.SetTexture(
                "_RoughnessMap",
                LoadTexture("kursa_" + definition.Id + "_roughness.png"));
            material.SetTexture(
                "_MetallicMap",
                LoadTexture("kursa_" + definition.Id + "_metallic.png"));
            material.SetTexture(
                "_NormalMap",
                LoadTexture("kursa_" + definition.Id + "_normal.png"));
            material.SetTexture(
                "_TorsoGlyph",
                LoadTexture("kursa_torso_reference_glyph.png"));
            material.SetTexture(
                "_HoodDecal",
                LoadTexture("kursa_hood_reference_decal.png"));
            material.SetTexture(
                "_ScarfDecal",
                LoadTexture("kursa_scarf_reference_decal.png"));
            material.SetTexture(
                "_EyeLeft",
                LoadTexture("kursa_eye_left_reference_overlay.png"));
            material.SetTexture(
                "_EyeRight",
                LoadTexture("kursa_eye_right_reference_overlay.png"));
            material.enableInstancing = true;
            material.renderQueue = -1;
        }

        private static Texture2D LoadTexture(string fileName)
        {
            var path = TextureFolder + "/" + fileName;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                throw new InvalidOperationException(
                    "Approved Kursa texture failed to load: " + path);
        }

        private static Material[] ResolveApprovedMaterialOrder(
            SkinnedMeshRenderer approvedRenderer,
            IReadOnlyDictionary<string, Material> materials)
        {
            if (approvedRenderer.sharedMaterials.Length !=
                approvedRenderer.sharedMesh.subMeshCount)
            {
                throw new InvalidOperationException(
                    "Approved Kursa FBX material slots do not match its submeshes.");
            }

            var result = new Material[approvedRenderer.sharedMaterials.Length];
            for (var index = 0; index < result.Length; index++)
            {
                var source = approvedRenderer.sharedMaterials[index] ??
                    throw new InvalidOperationException(
                        "Approved Kursa FBX contains a null material slot.");
                if (!materials.TryGetValue(source.name, out result[index]))
                {
                    throw new InvalidOperationException(
                        "Approved Kursa FBX material order contains an unknown material: " +
                        source.name);
                }
            }

            if (result.Length != MaterialDefinitions.Length ||
                result.Distinct().Count() != MaterialDefinitions.Length)
            {
                throw new InvalidOperationException(
                    "Approved Kursa FBX does not contain the exact nine approved materials.");
            }

            return result;
        }

        private static Dictionary<string, Material> LoadApprovedMaterials()
        {
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var definition in MaterialDefinitions)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(
                    definition.MaterialPath) ??
                    throw new InvalidOperationException(
                        "Approved Kursa material is missing: " +
                        definition.MaterialPath);
                result.Add(definition.DisplayName, material);
            }

            return result;
        }

        private static AppearanceResult InspectAppliedState(
            Scene scene,
            GameObject root,
            IReadOnlyDictionary<string, Material> materials)
        {
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes during approved Kursa appearance inspection.");
            }

            RequireApprovedSourceFiles();
            var sourceRenderer = RequireAssetRenderer(SourceModelPath);
            var approvedRenderer = RequireAssetRenderer(ApprovedModelPath);
            var runtimeRenderer = RequireAssetRenderer(RuntimeProjectionModelPath);
            RequireGeometryParity(
                sourceRenderer,
                approvedRenderer,
                "source and approved FBX during inspection");
            var shieldBasePose = RequireApprovedShieldBasePose(
                approvedRenderer,
                runtimeRenderer,
                "approved and runtime-projection FBX during inspection");
            RequireAnimationParity(ApprovedModelPath, RuntimeProjectionModelPath);
            var eyeProjectionCounts =
                RequireRuntimeEyeProjectionChannels(runtimeRenderer);
            var expectedMaterials = ResolveApprovedMaterialOrder(
                runtimeRenderer,
                materials);
            var renderers = RequireSceneRenderers(root);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh != runtimeRenderer.sharedMesh)
                {
                    throw new InvalidOperationException(
                        "A placed Kursa renderer does not use the approved material-slot mesh: " +
                        renderer.name);
                }
                RequireBoneOrder(renderer, runtimeRenderer);
                RequireApprovedBindPoseTransforms(renderer);
                if (!renderer.sharedMaterials.SequenceEqual(expectedMaterials))
                {
                    throw new InvalidOperationException(
                        "A placed Kursa renderer does not use the exact approved material order: " +
                        renderer.name);
                }
            }

            var textureCount = RequireTextureHashesMatch();
            return new AppearanceResult(
                renderers.Length,
                expectedMaterials.Length,
                runtimeRenderer.sharedMesh.vertexCount,
                TriangleCount(runtimeRenderer.sharedMesh),
                runtimeRenderer.bones.Length,
                textureCount,
                eyeProjectionCounts[0],
                eyeProjectionCounts[1],
                shieldBasePose.ForwardAngleDegrees,
                shieldBasePose.VerticalAngleDegrees,
                shieldBasePose.CenterShift,
                shieldBasePose.FrontOffset,
                shieldBasePose.TorsoLateralGap,
                shieldBasePose.TorsoLateralGapRatio,
                shieldBasePose.RightUpperDownAngleDegrees,
                shieldBasePose.RightForearmDownAngleDegrees,
                shieldBasePose.RightArmLateralGap,
                shieldBasePose.RightArmMinimumHeight,
                shieldBasePose.RightArmThighClearance,
                shieldBasePose.RightArmOutwardOffset,
                shieldBasePose.ChangedVertices,
                shieldBasePose.ChangedBindPoses);
        }

        // UV0 remains the approved model UV. UV1/UV2 contain the exact frame-1
        // left/right projection coordinates and UV3 contains their normalized
        // signed depth, so the eye overlays stay attached after skinning.
        private static int[] RequireRuntimeEyeProjectionChannels(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Kursa runtime projection mesh is missing.");
            var uv0 = mesh.uv;
            var leftProjection = mesh.uv2;
            var rightProjection = mesh.uv3;
            var signedDepth = mesh.uv4;
            if (uv0.Length != mesh.vertexCount ||
                leftProjection.Length != mesh.vertexCount ||
                rightProjection.Length != mesh.vertexCount ||
                signedDepth.Length != mesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "Kursa runtime projection FBX does not contain UV0 and the exact three eye projection channels.");
            }

            var faceSubmesh = Array.FindIndex(
                renderer.sharedMaterials,
                material => material != null && material.name ==
                    "Kursa_Face_Metal_Blue_Optics");
            if (faceSubmesh < 0)
            {
                throw new InvalidOperationException(
                    "Kursa runtime projection face submesh is missing.");
            }

            var faceIndices = mesh.GetIndices(faceSubmesh).Distinct().ToArray();
            var leftCount = faceIndices.Count(index =>
                InUnitSquare(leftProjection[index]) &&
                Mathf.Abs(signedDepth[index].x) < 1f);
            var rightCount = faceIndices.Count(index =>
                InUnitSquare(rightProjection[index]) &&
                Mathf.Abs(signedDepth[index].y) < 1f);
            if (leftCount == 0 || rightCount == 0)
            {
                throw new InvalidOperationException(
                    "Kursa runtime eye projection channels do not cover both approved eye regions. Left=" +
                    leftCount + ", Right=" + rightCount);
            }
            return new[] { leftCount, rightCount };
        }

        private static bool InUnitSquare(Vector2 value)
        {
            return value.x >= 0f && value.x <= 1f &&
                   value.y >= 0f && value.y <= 1f;
        }

        private static SkinnedMeshRenderer[] RequireSceneRenderers(GameObject root)
        {
            if (root.transform.childCount != ExpectedSlots)
            {
                throw new InvalidOperationException(
                    "Approved Kursa placement must contain exactly twelve slots.");
            }

            var renderers = new List<SkinnedMeshRenderer>();
            for (var index = 0; index < ExpectedSlots; index++)
            {
                var slot = root.transform.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Kursa slot contract differs at index " + index + ".");
                }
                var model = slot.GetChild(0);
                if (model.name != ModelName)
                {
                    throw new InvalidOperationException(
                        "Kursa model child differs in " + slot.name + ".");
                }
                var enabledAnimators = model
                    .GetComponentsInChildren<Animator>(true)
                    .Where(item => item.enabled)
                    .ToArray();
                var enabledLegacyAnimations = model
                    .GetComponentsInChildren<Animation>(true)
                    .Where(item => item.enabled)
                    .ToArray();
                var hasApprovedIdleAnimator =
                    slot.name == "Kursa_02_Idle" &&
                    enabledAnimators.Length == 1 &&
                    !enabledAnimators[0].applyRootMotion &&
                    AssetDatabase.GetAssetPath(
                        enabledAnimators[0].runtimeAnimatorController) ==
                    ApprovedIdleControllerPath;
                var hasApprovedMoveAnimator =
                    slot.name == "Kursa_03_Move" &&
                    enabledAnimators.Length == 1 &&
                    !enabledAnimators[0].applyRootMotion &&
                    AssetDatabase.GetAssetPath(
                        enabledAnimators[0].runtimeAnimatorController) ==
                    KursaMoveAnimationTool.ControllerPath;
                if (enabledLegacyAnimations.Length != 0 ||
                    (slot.name == "Kursa_02_Idle"
                        ? !hasApprovedIdleAnimator
                        : slot.name == "Kursa_03_Move"
                            ? !hasApprovedMoveAnimator
                            : enabledAnimators.Length != 0))
                {
                    throw new InvalidOperationException(
                        "Kursa animation state changed in " + slot.name + ".");
                }
                var renderer = model
                    .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .SingleOrDefault() ??
                    throw new InvalidOperationException(
                        "Kursa slot must contain exactly one skinned renderer: " +
                        slot.name);
                if (slot.name != "Kursa_03_Move")
                {
                    renderers.Add(renderer);
                }
            }

            return renderers.ToArray();
        }

        private static SkinnedMeshRenderer RequireAssetRenderer(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                throw new InvalidOperationException(
                    "Kursa model asset failed to load: " + path);
            return asset.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                       .SingleOrDefault() ??
                   throw new InvalidOperationException(
                       "Kursa model asset must contain exactly one skinned renderer: " +
                       path);
        }

        private static void RequireGeometryParity(
            SkinnedMeshRenderer expected,
            SkinnedMeshRenderer actual,
            string label)
        {
            var expectedMesh = expected.sharedMesh ??
                throw new InvalidOperationException(label + " expected mesh is missing.");
            var actualMesh = actual.sharedMesh ??
                throw new InvalidOperationException(label + " actual mesh is missing.");
            if (expectedMesh.vertexCount != ExpectedUnityImportedVertices ||
                actualMesh.vertexCount != ExpectedUnityImportedVertices ||
                TriangleCount(expectedMesh) != ExpectedTriangles ||
                TriangleCount(actualMesh) != ExpectedTriangles ||
                expected.bones.Length != ExpectedBones ||
                actual.bones.Length != ExpectedBones)
            {
                throw new InvalidOperationException(
                    "Kursa approved geometry counts differ for " + label +
                    ". ApprovedSampleVertices=" + ApprovedSampleVertices +
                    ", ExpectedUnityImportedVertices=" +
                    ExpectedUnityImportedVertices +
                    ", SourceVertices=" + expectedMesh.vertexCount +
                    ", ApprovedVertices=" + actualMesh.vertexCount +
                    ", ExpectedTriangles=" + ExpectedTriangles +
                    ", SourceTriangles=" + TriangleCount(expectedMesh) +
                    ", ApprovedTriangles=" + TriangleCount(actualMesh) +
                    ", ExpectedBones=" + ExpectedBones +
                    ", SourceBones=" + expected.bones.Length +
                    ", ApprovedBones=" + actual.bones.Length + ".");
            }

            var expectedVertices = CanonicalVertexAttributeSignatures(expectedMesh);
            var actualVertices = CanonicalVertexAttributeSignatures(actualMesh);
            if (!expectedVertices.SequenceEqual(
                    actualVertices,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Kursa vertex coordinates, UV0, or bone weights differ for " +
                    label + ". SourceBoundsCenter=" + Vec(expectedMesh.bounds.center) +
                    ", SourceBoundsSize=" + Vec(expectedMesh.bounds.size) +
                    ", ApprovedBoundsCenter=" + Vec(actualMesh.bounds.center) +
                    ", ApprovedBoundsSize=" + Vec(actualMesh.bounds.size) +
                    ", PositionsMatch=" + CanonicalPositionSignatures(expectedMesh)
                        .SequenceEqual(
                            CanonicalPositionSignatures(actualMesh),
                            StringComparer.Ordinal) +
                    ", PositionNormalsMatch=" + CanonicalPositionNormalSignatures(expectedMesh)
                        .SequenceEqual(
                            CanonicalPositionNormalSignatures(actualMesh),
                            StringComparer.Ordinal) +
                    ", PositionTangentsMatch=" + CanonicalPositionTangentSignatures(expectedMesh)
                        .SequenceEqual(
                            CanonicalPositionTangentSignatures(actualMesh),
                            StringComparer.Ordinal) +
                    ", PositionUvMatch=" + CanonicalPositionUvSignatures(expectedMesh)
                        .SequenceEqual(
                            CanonicalPositionUvSignatures(actualMesh),
                            StringComparer.Ordinal) +
                    ", PositionWeightsMatch=" + CanonicalPositionWeightSignatures(expectedMesh)
                        .SequenceEqual(
                            CanonicalPositionWeightSignatures(actualMesh),
                            StringComparer.Ordinal) + ".");
            }
            if (!CanonicalTriangleAttributeSignatures(expectedMesh).SequenceEqual(
                    CanonicalTriangleAttributeSignatures(actualMesh),
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Kursa topology differs for " + label + ".");
            }
            RequireBindPoseParity(expectedMesh.bindposes, actualMesh.bindposes, label);
            RequireBoneOrder(expected, actual);
        }

        private static ShieldBasePoseResult RequireApprovedShieldBasePose(
            SkinnedMeshRenderer approved,
            SkinnedMeshRenderer runtime,
            string label)
        {
            var approvedMesh = approved.sharedMesh ??
                throw new InvalidOperationException(label + " approved mesh is missing.");
            var runtimeMesh = runtime.sharedMesh ??
                throw new InvalidOperationException(label + " runtime mesh is missing.");
            if (approvedMesh.vertexCount != ExpectedUnityImportedVertices ||
                runtimeMesh.vertexCount != ExpectedUnityRuntimeVertices ||
                TriangleCount(approvedMesh) != ExpectedTriangles ||
                TriangleCount(runtimeMesh) != ExpectedTriangles ||
                approved.bones.Length != ExpectedBones ||
                runtime.bones.Length != ExpectedBones ||
                approvedMesh.subMeshCount != runtimeMesh.subMeshCount)
            {
                throw new InvalidOperationException(
                    "Kursa controlled shield-pose counts differ for " + label +
                    ". ApprovedVertices=" + approvedMesh.vertexCount +
                    ", RuntimeVertices=" + runtimeMesh.vertexCount +
                    ", ApprovedTriangles=" + TriangleCount(approvedMesh) +
                    ", RuntimeTriangles=" + TriangleCount(runtimeMesh) +
                    ", ApprovedBones=" + approved.bones.Length +
                    ", RuntimeBones=" + runtime.bones.Length +
                    ", ApprovedSubmeshes=" + approvedMesh.subMeshCount +
                    ", RuntimeSubmeshes=" + runtimeMesh.subMeshCount + ".");
            }

            RequireBoneOrder(approved, runtime);
            var approvedUv0 = approvedMesh.uv;
            var runtimeUv0 = runtimeMesh.uv;
            var approvedWeights = approvedMesh.boneWeights;
            var runtimeWeights = runtimeMesh.boneWeights;
            if (approvedUv0.Length != approvedMesh.vertexCount ||
                runtimeUv0.Length != runtimeMesh.vertexCount ||
                approvedWeights.Length != approvedMesh.vertexCount ||
                runtimeWeights.Length != runtimeMesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "Kursa UV0 or skin weights are incomplete for " + label + ".");
            }

            if (!CanonicalTriangleUvWeightSignatures(approvedMesh).SequenceEqual(
                    CanonicalTriangleUvWeightSignatures(runtimeMesh),
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Kursa topology, material assignment, UV0, or skin weights changed during the shield base-pose edit for " +
                    label + ".");
            }

            var allowedBoneNames = new HashSet<string>(
                ApprovedPoseBoneNames,
                StringComparer.Ordinal);
            var allowedBoneIndices = new HashSet<int>(
                approved.bones
                    .Select((bone, index) => new { bone.name, index })
                    .Where(item => allowedBoneNames.Contains(item.name))
                    .Select(item => item.index));
            if (allowedBoneIndices.Count != allowedBoneNames.Count)
            {
                throw new InvalidOperationException(
                    "Kursa approved bilateral arm bone chains are incomplete for " + label + ".");
            }
            var blenderEvidence = RequireBlenderPoseEvidence();
            var changedVertices = blenderEvidence.ChangedVertices;

            var approvedBindPoses = approvedMesh.bindposes;
            var runtimeBindPoses = runtimeMesh.bindposes;
            if (approvedBindPoses.Length != runtimeBindPoses.Length)
            {
                throw new InvalidOperationException(
                    "Kursa bind pose count changed for " + label + ".");
            }
            var changedBindPoses = 0;
            for (var index = 0; index < approvedBindPoses.Length; index++)
            {
                var changed = false;
                for (var element = 0; element < 16; element++)
                {
                    changed |= Mathf.Abs(
                        approvedBindPoses[index][element] -
                        runtimeBindPoses[index][element]) > 0.0001f;
                }
                if (!changed)
                {
                    continue;
                }
                changedBindPoses++;
                if (!allowedBoneIndices.Contains(index))
                {
                    throw new InvalidOperationException(
                        "Kursa bind pose outside the approved bilateral arm chains changed at " +
                        approved.bones[index].name + " for " + label + ".");
                }
            }
            if (changedBindPoses != 3)
            {
                throw new InvalidOperationException(
                    "Kursa base-pose edit must change exactly the approved three left-arm bind poses for " +
                    label + ". Changed=" + changedBindPoses + ".");
            }

            var shieldSubmesh = Array.FindIndex(
                runtime.sharedMaterials,
                material => material != null && material.name ==
                    "Kursa_Shield_Worn_Gunmetal");
            if (shieldSubmesh < 0)
            {
                throw new InvalidOperationException(
                    "Kursa worn shield submesh is missing for " + label + ".");
            }
            var shieldFrameSubmesh = Array.FindIndex(
                runtime.sharedMaterials,
                material => material != null && material.name ==
                    "Kursa_Shield_Frame_Steel");
            if (shieldFrameSubmesh < 0)
            {
                throw new InvalidOperationException(
                    "Kursa shield frame submesh is missing for " + label + ".");
            }
            var forwardNormal = DominantForwardNormal(runtimeMesh, shieldSubmesh);
            var forwardAngle = Vector3.Angle(forwardNormal, Vector3.forward);
            if (forwardAngle > 2f)
            {
                throw new InvalidOperationException(
                    "Kursa shield base-pose face is not forward for " + label +
                    ". Angle=" + forwardAngle.ToString("R", CultureInfo.InvariantCulture) +
                    ", Normal=" + Vec(forwardNormal) + ".");
            }
            var longAxis = DominantLongAxis(
                runtimeMesh,
                new[] { shieldSubmesh, shieldFrameSubmesh },
                forwardNormal);
            var verticalAngle = Vector3.Angle(longAxis, Vector3.up);
            if (verticalAngle > 2f)
            {
                throw new InvalidOperationException(
                    "Kursa shield base-pose long axis is not vertical for " + label +
                    ". Angle=" + verticalAngle.ToString("R", CultureInfo.InvariantCulture) +
                    ", Axis=" + Vec(longAxis) + ".");
            }

            var runtimeBounds = SubmeshBounds(runtimeMesh, shieldSubmesh);
            var wholeShieldBounds = runtimeBounds;
            var frameBounds = SubmeshBounds(runtimeMesh, shieldFrameSubmesh);
            wholeShieldBounds.Encapsulate(frameBounds.min);
            wholeShieldBounds.Encapsulate(frameBounds.max);
            if (runtimeBounds.size.z >=
                Mathf.Min(runtimeBounds.size.x, runtimeBounds.size.y) * 0.3f)
            {
                throw new InvalidOperationException(
                    "Kursa shield base-pose depth is not aligned to model forward for " +
                    label + ". Size=" + Vec(runtimeBounds.size) + ".");
            }
            var bodyBounds = ExcludedSubmeshBounds(
                runtimeMesh,
                new HashSet<int> { shieldSubmesh, shieldFrameSubmesh });
            var frontOffset = runtimeBounds.center.z - bodyBounds.center.z;
            if (frontOffset <= 0f)
            {
                throw new InvalidOperationException(
                    "Kursa shield is behind the character in the base pose for " + label +
                    ". ShieldCenter=" + Vec(runtimeBounds.center) +
                    ", BodyCenter=" + Vec(bodyBounds.center) +
                    ", FrontOffset=" + frontOffset.ToString(
                        "R", CultureInfo.InvariantCulture) + ".");
            }
            var torsoCenterlineX = TorsoCenterlineXAtY(
                runtime,
                wholeShieldBounds.center.y);
            var torsoLateralGap = Mathf.Abs(
                wholeShieldBounds.center.x - torsoCenterlineX);
            if (Mathf.Abs(
                    torsoLateralGap - blenderEvidence.FinalLateralGapUnity) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Kursa shield Unity torso lateral gap differs from the approved Blender half-gap target for " +
                    label + ". Actual=" + torsoLateralGap.ToString(
                        "R", CultureInfo.InvariantCulture) +
                    ", Expected=" + blenderEvidence.FinalLateralGapUnity.ToString(
                        "R", CultureInfo.InvariantCulture) + ".");
            }
            var rightArmGeometry = BoneInfluenceGeometry(
                runtime,
                new[] { "RightArm", "RightForeArm", "RightHand" });
            var rightArmCenterlineX = TorsoCenterlineXAtY(
                runtime,
                rightArmGeometry.Centroid.y);
            var rightArmLateralGap = Mathf.Abs(
                rightArmGeometry.Centroid.x - rightArmCenterlineX);
            if (Mathf.Abs(
                    rightArmLateralGap -
                    blenderEvidence.RightArmCentroidLateralGapUnity) > 0.05f)
            {
                throw new InvalidOperationException(
                    "Kursa Unity right arm is not at the approved torso-side attention gap for " +
                    label + ". Actual=" + rightArmLateralGap.ToString(
                        "R", CultureInfo.InvariantCulture) +
                    ", Expected=" +
                    blenderEvidence.RightArmCentroidLateralGapUnity.ToString(
                        "R", CultureInfo.InvariantCulture) + ".");
            }
            if (Mathf.Abs(
                    rightArmGeometry.Bounds.min.y -
                    blenderEvidence.RightArmMinimumHeightUnity) > 0.05f)
            {
                throw new InvalidOperationException(
                    "Kursa Unity right hand-side geometry is not at the approved thigh height for " +
                    label + ". ActualMinY=" + rightArmGeometry.Bounds.min.y.ToString(
                        "R", CultureInfo.InvariantCulture) +
                    ", Expected=" + blenderEvidence.RightArmMinimumHeightUnity.ToString(
                        "R", CultureInfo.InvariantCulture) + ".");
            }
            var rightArmThighSeparation = RightArmThighSurfaceSeparation(runtime);
            if (rightArmThighSeparation.ArmTriangles !=
                    blenderEvidence.RightArmSurfaceTriangles ||
                rightArmThighSeparation.ThighTriangles !=
                    blenderEvidence.RightThighSurfaceTriangles ||
                rightArmThighSeparation.OverlapPairs != 0 ||
                rightArmThighSeparation.MinimumClearance < 0.01f ||
                Mathf.Abs(
                    rightArmThighSeparation.MinimumClearance -
                    blenderEvidence.RightArmThighClearanceUnity) > 0.005f)
            {
                throw new InvalidOperationException(
                    "Kursa right arm still intersects or is too close to the right thigh for " +
                    label + ". ArmTriangles=" + rightArmThighSeparation.ArmTriangles +
                    ", ThighTriangles=" + rightArmThighSeparation.ThighTriangles +
                    ", OverlapPairs=" + rightArmThighSeparation.OverlapPairs +
                    ", Clearance=" + rightArmThighSeparation.MinimumClearance.ToString(
                        "R", CultureInfo.InvariantCulture) +
                    ", BlenderClearance=" +
                    blenderEvidence.RightArmThighClearanceUnity.ToString(
                        "R", CultureInfo.InvariantCulture) + ".");
            }
            var centerShift = blenderEvidence.CenterShiftUnity;

            return new ShieldBasePoseResult(
                forwardAngle,
                verticalAngle,
                centerShift,
                frontOffset,
                torsoLateralGap,
                blenderEvidence.TargetLateralGapRatio,
                blenderEvidence.RightUpperDownAngleDegrees,
                blenderEvidence.RightForearmDownAngleDegrees,
                rightArmLateralGap,
                rightArmGeometry.Bounds.min.y,
                rightArmThighSeparation.MinimumClearance,
                blenderEvidence.RightArmOutwardOffsetUnity,
                changedVertices,
                changedBindPoses);
        }

        private static string[] CanonicalTriangleUvWeightSignatures(Mesh mesh)
        {
            var uv = mesh.uv;
            var weights = mesh.boneWeights;
            var vertexKeys = Enumerable.Range(0, mesh.vertexCount)
                .Select(index => UvWeightSignature(uv[index], weights[index]))
                .ToArray();
            var triangles = new List<string>();
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                var indices = mesh.GetIndices(submesh);
                if (indices.Length % 3 != 0)
                {
                    throw new InvalidOperationException(
                        "Kursa mesh contains a non-triangle shield-pose submesh.");
                }
                for (var index = 0; index < indices.Length; index += 3)
                {
                    var triangle = new[]
                    {
                        vertexKeys[indices[index]],
                        vertexKeys[indices[index + 1]],
                        vertexKeys[indices[index + 2]]
                    };
                    Array.Sort(triangle, StringComparer.Ordinal);
                    triangles.Add(
                        submesh + ":" + triangle[0] + "|" + triangle[1] + "|" +
                        triangle[2]);
                }
            }
            triangles.Sort(StringComparer.Ordinal);
            return triangles.ToArray();
        }

        private static Dictionary<string, Vector3[]> PositionSignaturesByUvWeight(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Kursa rest-position mesh is missing.");
            var uv = mesh.uv;
            var weights = mesh.boneWeights;
            var restPositions = mesh.vertices;
            return Enumerable.Range(0, mesh.vertexCount)
                .GroupBy(
                    index => UvWeightSignature(uv[index], weights[index]),
                    StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(index => restPositions[index])
                        .ToArray(),
                    StringComparer.Ordinal);
        }

        private static Vector3[] RestPositions(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Kursa rest-position mesh is missing.");
            var weights = mesh.boneWeights;
            var vertices = mesh.vertices;
            var bindPoses = mesh.bindposes;
            var rendererWorldToLocal = renderer.transform.worldToLocalMatrix;
            var skinMatrices = renderer.bones
                .Select((bone, index) =>
                    rendererWorldToLocal * bone.localToWorldMatrix * bindPoses[index])
                .ToArray();
            return vertices.Select((vertex, index) =>
            {
                var weight = weights[index];
                return skinMatrices[weight.boneIndex0].MultiplyPoint3x4(vertex) *
                           weight.weight0 +
                       skinMatrices[weight.boneIndex1].MultiplyPoint3x4(vertex) *
                           weight.weight1 +
                       skinMatrices[weight.boneIndex2].MultiplyPoint3x4(vertex) *
                           weight.weight2 +
                       skinMatrices[weight.boneIndex3].MultiplyPoint3x4(vertex) *
                           weight.weight3;
            }).ToArray();
        }

        private static float PositionSetDistance(
            Vector3[] left,
            Vector3[] right)
        {
            if (left == null || right == null)
            {
                return left == null && right == null
                    ? 0f
                    : float.PositiveInfinity;
            }
            var leftToRight = left.Max(value =>
                right.Min(candidate => Vector3.Distance(value, candidate)));
            var rightToLeft = right.Max(value =>
                left.Min(candidate => Vector3.Distance(value, candidate)));
            return Mathf.Max(leftToRight, rightToLeft);
        }

        private static string UvWeightSignature(Vector2 uv, BoneWeight weight)
        {
            return string.Join(
                ":",
                Quantize(uv.x), Quantize(uv.y),
                weight.boneIndex0, Quantize(weight.weight0),
                weight.boneIndex1, Quantize(weight.weight1),
                weight.boneIndex2, Quantize(weight.weight2),
                weight.boneIndex3, Quantize(weight.weight3));
        }

        private static bool BoneWeightEqual(BoneWeight left, BoneWeight right)
        {
            return left.boneIndex0 == right.boneIndex0 &&
                   left.boneIndex1 == right.boneIndex1 &&
                   left.boneIndex2 == right.boneIndex2 &&
                   left.boneIndex3 == right.boneIndex3 &&
                   Mathf.Abs(left.weight0 - right.weight0) <= 0.00001f &&
                   Mathf.Abs(left.weight1 - right.weight1) <= 0.00001f &&
                   Mathf.Abs(left.weight2 - right.weight2) <= 0.00001f &&
                   Mathf.Abs(left.weight3 - right.weight3) <= 0.00001f;
        }

        private static bool BoneWeightTouches(
            BoneWeight weight,
            HashSet<int> allowed)
        {
            return (weight.weight0 > 0f && allowed.Contains(weight.boneIndex0)) ||
                   (weight.weight1 > 0f && allowed.Contains(weight.boneIndex1)) ||
                   (weight.weight2 > 0f && allowed.Contains(weight.boneIndex2)) ||
                   (weight.weight3 > 0f && allowed.Contains(weight.boneIndex3));
        }

        private static Vector3 DominantForwardNormal(Mesh mesh, int submesh)
        {
            var vertices = mesh.vertices;
            var indices = mesh.GetIndices(submesh);
            var clusters = new List<NormalCluster>();
            for (var index = 0; index < indices.Length; index += 3)
            {
                var cross = Vector3.Cross(
                    vertices[indices[index + 1]] - vertices[indices[index]],
                    vertices[indices[index + 2]] - vertices[indices[index]]);
                var area = cross.magnitude * 0.5f;
                if (area <= 0f)
                {
                    continue;
                }
                var normal = cross.normalized;
                var cluster = clusters.FirstOrDefault(item =>
                    Vector3.Dot(normal, item.Sum.normalized) >= 0.985f);
                if (cluster == null)
                {
                    cluster = new NormalCluster();
                    clusters.Add(cluster);
                }
                cluster.Sum += normal * area;
                cluster.Area += area;
            }
            var minimumRepresentativeArea = clusters.Max(item => item.Area) * 0.05f;
            var result = clusters
                .Where(item =>
                    item.Sum.z > 0f &&
                    item.Area >= minimumRepresentativeArea)
                .OrderByDescending(item => item.Sum.normalized.z)
                .ThenByDescending(item => item.Area)
                .FirstOrDefault() ??
                throw new InvalidOperationException(
                    "Kursa shield has no outward forward surface cluster.");
            return result.Sum.normalized;
        }

        private static Vector3 DominantLongAxis(
            Mesh mesh,
            IEnumerable<int> submeshes,
            Vector3 forwardNormal)
        {
            var vertices = mesh.vertices;
            var vertexIndices = new HashSet<int>();
            foreach (var submesh in submeshes)
            {
                vertexIndices.UnionWith(mesh.GetIndices(submesh));
            }
            if (vertexIndices.Count == 0)
            {
                throw new InvalidOperationException(
                    "Kursa shield has no vertices for long-axis analysis.");
            }

            var center = Vector3.zero;
            foreach (var index in vertexIndices)
            {
                center += vertices[index];
            }
            center /= vertexIndices.Count;

            var xx = 0f;
            var xy = 0f;
            var xz = 0f;
            var yy = 0f;
            var yz = 0f;
            var zz = 0f;
            foreach (var index in vertexIndices)
            {
                var delta = Vector3.ProjectOnPlane(
                    vertices[index] - center,
                    forwardNormal);
                xx += delta.x * delta.x;
                xy += delta.x * delta.y;
                xz += delta.x * delta.z;
                yy += delta.y * delta.y;
                yz += delta.y * delta.z;
                zz += delta.z * delta.z;
            }

            var axis = Vector3.ProjectOnPlane(Vector3.up, forwardNormal).normalized;
            if (axis.sqrMagnitude <= 0.000001f)
            {
                axis = Vector3.ProjectOnPlane(Vector3.right, forwardNormal).normalized;
            }
            for (var iteration = 0; iteration < 32; iteration++)
            {
                var next = new Vector3(
                    xx * axis.x + xy * axis.y + xz * axis.z,
                    xy * axis.x + yy * axis.y + yz * axis.z,
                    xz * axis.x + yz * axis.y + zz * axis.z);
                next = Vector3.ProjectOnPlane(next, forwardNormal);
                if (next.sqrMagnitude <= 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Kursa shield long-axis covariance is degenerate.");
                }
                axis = next.normalized;
            }
            return axis.y >= 0f ? axis : -axis;
        }

        private static Bounds SubmeshBounds(Mesh mesh, int submesh)
        {
            var indices = mesh.GetIndices(submesh);
            if (indices.Length == 0)
            {
                throw new InvalidOperationException("Kursa shield submesh is empty.");
            }
            var vertices = mesh.vertices;
            var bounds = new Bounds(vertices[indices[0]], Vector3.zero);
            foreach (var index in indices.Skip(1))
            {
                bounds.Encapsulate(vertices[index]);
            }
            return bounds;
        }

        private static Bounds ExcludedSubmeshBounds(
            Mesh mesh,
            HashSet<int> excludedSubmeshes)
        {
            var vertices = mesh.vertices;
            var indices = Enumerable.Range(0, mesh.subMeshCount)
                .Where(submesh => !excludedSubmeshes.Contains(submesh))
                .SelectMany(submesh => mesh.GetIndices(submesh))
                .ToArray();
            if (indices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Kursa non-shield body geometry is empty.");
            }
            var bounds = new Bounds(vertices[indices[0]], Vector3.zero);
            foreach (var index in indices.Skip(1))
            {
                bounds.Encapsulate(vertices[index]);
            }
            return bounds;
        }

        private static float TorsoCenterlineXAtY(
            SkinnedMeshRenderer renderer,
            float targetY)
        {
            var names = new[] { "Hips", "Spine02", "Spine01", "Spine" };
            var points = names.Select(name =>
            {
                var bone = renderer.bones.FirstOrDefault(
                    candidate => candidate != null && candidate.name == name) ??
                    throw new InvalidOperationException(
                        "Kursa " + name +
                        " bone is missing for torso centerline analysis.");
                return renderer.transform.InverseTransformPoint(bone.position);
            }).ToArray();
            for (var index = 0; index < points.Length - 1; index++)
            {
                var head = points[index];
                var tail = points[index + 1];
                var minimumY = Mathf.Min(head.y, tail.y);
                var maximumY = Mathf.Max(head.y, tail.y);
                if (targetY < minimumY || targetY > maximumY ||
                    Mathf.Abs(tail.y - head.y) <= 0.000001f)
                {
                    continue;
                }
                var factor = (targetY - head.y) / (tail.y - head.y);
                return Mathf.LerpUnclamped(head.x, tail.x, factor);
            }
            throw new InvalidOperationException(
                "Kursa target height is outside the Hips-to-Spine torso centerline span. " +
                "TargetY=" + targetY.ToString("R", CultureInfo.InvariantCulture) + ".");
        }

        private static InfluenceGeometry BoneInfluenceGeometry(
            SkinnedMeshRenderer renderer,
            IEnumerable<string> boneNames)
        {
            var names = new HashSet<string>(boneNames, StringComparer.Ordinal);
            var boneIndices = new HashSet<int>(renderer.bones
                .Select((bone, index) => new { bone, index })
                .Where(item => item.bone != null && names.Contains(item.bone.name))
                .Select(item => item.index));
            if (boneIndices.Count != names.Count)
            {
                throw new InvalidOperationException(
                    "Kursa right-arm influence bones are incomplete.");
            }
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Kursa mesh is missing for right-arm influence analysis.");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var selected = Enumerable.Range(0, vertices.Length)
                .Where(index => BoneWeightTouches(weights[index], boneIndices))
                .ToArray();
            if (selected.Length == 0)
            {
                throw new InvalidOperationException(
                    "Kursa right-arm influence geometry is empty.");
            }
            var bounds = new Bounds(vertices[selected[0]], Vector3.zero);
            var centroid = Vector3.zero;
            foreach (var index in selected)
            {
                bounds.Encapsulate(vertices[index]);
                centroid += vertices[index];
            }
            centroid /= selected.Length;
            return new InfluenceGeometry(selected.Length, centroid, bounds);
        }

        private static SurfaceSeparation RightArmThighSurfaceSeparation(
            SkinnedMeshRenderer renderer)
        {
            var armTriangles = InfluenceSurfaceTriangles(
                renderer,
                new[] { "RightArm", "RightForeArm", "RightHand" });
            var thighTriangles = InfluenceSurfaceTriangles(
                renderer,
                new[] { "RightUpLeg" });
            var overlapPairs = 0;
            var minimumSquaredDistance = float.PositiveInfinity;
            foreach (var arm in armTriangles)
            {
                foreach (var thigh in thighTriangles)
                {
                    var squaredDistance = TriangleDistanceSquared(arm, thigh);
                    minimumSquaredDistance = Mathf.Min(
                        minimumSquaredDistance,
                        squaredDistance);
                    if (squaredDistance <= 0.0000000001f)
                    {
                        overlapPairs++;
                    }
                }
            }
            if (float.IsPositiveInfinity(minimumSquaredDistance))
            {
                throw new InvalidOperationException(
                    "Kursa right-arm/right-thigh surface separation is unavailable.");
            }
            return new SurfaceSeparation(
                armTriangles.Count,
                thighTriangles.Count,
                overlapPairs,
                Mathf.Sqrt(Mathf.Max(0f, minimumSquaredDistance)));
        }

        private static List<SurfaceTriangle> InfluenceSurfaceTriangles(
            SkinnedMeshRenderer renderer,
            IEnumerable<string> boneNames)
        {
            var names = new HashSet<string>(boneNames, StringComparer.Ordinal);
            var boneIndices = new HashSet<int>(renderer.bones
                .Select((bone, index) => new { bone, index })
                .Where(item => item.bone != null && names.Contains(item.bone.name))
                .Select(item => item.index));
            if (boneIndices.Count != names.Count)
            {
                throw new InvalidOperationException(
                    "Kursa influence surface bones are incomplete: " +
                    string.Join(",", names) + ".");
            }
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Kursa mesh is missing for influence surface analysis.");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var result = new List<SurfaceTriangle>();
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                var indices = mesh.GetIndices(submesh);
                for (var index = 0; index < indices.Length; index += 3)
                {
                    var first = indices[index];
                    var second = indices[index + 1];
                    var third = indices[index + 2];
                    var totalWeight =
                        InfluenceWeight(weights[first], boneIndices) +
                        InfluenceWeight(weights[second], boneIndices) +
                        InfluenceWeight(weights[third], boneIndices);
                    if (totalWeight > 1.5f)
                    {
                        result.Add(new SurfaceTriangle(
                            vertices[first],
                            vertices[second],
                            vertices[third]));
                    }
                }
            }
            if (result.Count == 0)
            {
                throw new InvalidOperationException(
                    "Kursa influence surface contains no triangles: " +
                    string.Join(",", names) + ".");
            }
            return result;
        }

        private static float InfluenceWeight(
            BoneWeight weight,
            HashSet<int> boneIndices)
        {
            var result = 0f;
            if (boneIndices.Contains(weight.boneIndex0)) result += weight.weight0;
            if (boneIndices.Contains(weight.boneIndex1)) result += weight.weight1;
            if (boneIndices.Contains(weight.boneIndex2)) result += weight.weight2;
            if (boneIndices.Contains(weight.boneIndex3)) result += weight.weight3;
            return result;
        }

        private static float TriangleDistanceSquared(
            SurfaceTriangle first,
            SurfaceTriangle second)
        {
            var firstEdges = new[]
            {
                (first.A, first.B),
                (first.B, first.C),
                (first.C, first.A)
            };
            var secondEdges = new[]
            {
                (second.A, second.B),
                (second.B, second.C),
                (second.C, second.A)
            };
            if (firstEdges.Any(edge => SegmentIntersectsTriangle(
                    edge.Item1, edge.Item2, second)) ||
                secondEdges.Any(edge => SegmentIntersectsTriangle(
                    edge.Item1, edge.Item2, first)))
            {
                return 0f;
            }
            var minimum = Mathf.Min(
                PointTriangleDistanceSquared(first.A, second),
                PointTriangleDistanceSquared(first.B, second),
                PointTriangleDistanceSquared(first.C, second),
                PointTriangleDistanceSquared(second.A, first),
                PointTriangleDistanceSquared(second.B, first),
                PointTriangleDistanceSquared(second.C, first));
            foreach (var firstEdge in firstEdges)
            {
                foreach (var secondEdge in secondEdges)
                {
                    minimum = Mathf.Min(
                        minimum,
                        SegmentDistanceSquared(
                            firstEdge.Item1,
                            firstEdge.Item2,
                            secondEdge.Item1,
                            secondEdge.Item2));
                }
            }
            return minimum;
        }

        private static bool SegmentIntersectsTriangle(
            Vector3 start,
            Vector3 end,
            SurfaceTriangle triangle)
        {
            var direction = end - start;
            var edge1 = triangle.B - triangle.A;
            var edge2 = triangle.C - triangle.A;
            var cross = Vector3.Cross(direction, edge2);
            var determinant = Vector3.Dot(edge1, cross);
            if (Mathf.Abs(determinant) <= 0.0000001f)
            {
                return false;
            }
            var inverse = 1f / determinant;
            var delta = start - triangle.A;
            var u = Vector3.Dot(delta, cross) * inverse;
            if (u < 0f || u > 1f)
            {
                return false;
            }
            var q = Vector3.Cross(delta, edge1);
            var v = Vector3.Dot(direction, q) * inverse;
            if (v < 0f || u + v > 1f)
            {
                return false;
            }
            var distance = Vector3.Dot(edge2, q) * inverse;
            return distance >= 0f && distance <= 1f;
        }

        private static float PointTriangleDistanceSquared(
            Vector3 point,
            SurfaceTriangle triangle)
        {
            return (point - ClosestPointOnTriangle(point, triangle)).sqrMagnitude;
        }

        private static Vector3 ClosestPointOnTriangle(
            Vector3 point,
            SurfaceTriangle triangle)
        {
            var ab = triangle.B - triangle.A;
            var ac = triangle.C - triangle.A;
            var ap = point - triangle.A;
            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return triangle.A;
            var bp = point - triangle.B;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return triangle.B;
            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                return triangle.A + ab * (d1 / (d1 - d3));
            }
            var cp = point - triangle.C;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return triangle.C;
            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                return triangle.A + ac * (d2 / (d2 - d6));
            }
            var va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                var factor = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return triangle.B + (triangle.C - triangle.B) * factor;
            }
            var denominator = 1f / (va + vb + vc);
            return triangle.A + ab * (vb * denominator) + ac * (vc * denominator);
        }

        private static float SegmentDistanceSquared(
            Vector3 firstStart,
            Vector3 firstEnd,
            Vector3 secondStart,
            Vector3 secondEnd)
        {
            var firstDirection = firstEnd - firstStart;
            var secondDirection = secondEnd - secondStart;
            var difference = firstStart - secondStart;
            var firstLengthSquared = Vector3.Dot(firstDirection, firstDirection);
            var secondLengthSquared = Vector3.Dot(secondDirection, secondDirection);
            var secondProjection = Vector3.Dot(secondDirection, difference);
            float firstFactor;
            float secondFactor;
            if (firstLengthSquared <= 0.0000001f &&
                secondLengthSquared <= 0.0000001f)
            {
                return difference.sqrMagnitude;
            }
            if (firstLengthSquared <= 0.0000001f)
            {
                firstFactor = 0f;
                secondFactor = Mathf.Clamp01(secondProjection / secondLengthSquared);
            }
            else
            {
                var firstProjection = Vector3.Dot(firstDirection, difference);
                if (secondLengthSquared <= 0.0000001f)
                {
                    secondFactor = 0f;
                    firstFactor = Mathf.Clamp01(-firstProjection / firstLengthSquared);
                }
                else
                {
                    var crossProjection = Vector3.Dot(
                        firstDirection,
                        secondDirection);
                    var denominator =
                        firstLengthSquared * secondLengthSquared -
                        crossProjection * crossProjection;
                    firstFactor = denominator == 0f
                        ? 0f
                        : Mathf.Clamp01(
                            (crossProjection * secondProjection -
                             firstProjection * secondLengthSquared) /
                            denominator);
                    secondFactor =
                        (crossProjection * firstFactor + secondProjection) /
                        secondLengthSquared;
                    if (secondFactor < 0f)
                    {
                        secondFactor = 0f;
                        firstFactor = Mathf.Clamp01(
                            -firstProjection / firstLengthSquared);
                    }
                    else if (secondFactor > 1f)
                    {
                        secondFactor = 1f;
                        firstFactor = Mathf.Clamp01(
                            (crossProjection - firstProjection) /
                            firstLengthSquared);
                    }
                }
            }
            var firstPoint = firstStart + firstDirection * firstFactor;
            var secondPoint = secondStart + secondDirection * secondFactor;
            return (firstPoint - secondPoint).sqrMagnitude;
        }

        private static void RequireAnimationParity(string approvedPath, string runtimePath)
        {
            var approved = AssetDatabase.LoadAllAssetsAtPath(approvedPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderBy(clip => clip.name, StringComparer.Ordinal)
                .ToArray();
            var runtime = AssetDatabase.LoadAllAssetsAtPath(runtimePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderBy(clip => clip.name, StringComparer.Ordinal)
                .ToArray();
            if (approved.Length != runtime.Length || approved.Length == 0)
            {
                throw new InvalidOperationException(
                    "Kursa embedded animation clip count changed. Approved=" +
                    approved.Length + ", Runtime=" + runtime.Length + ".");
            }
            for (var index = 0; index < approved.Length; index++)
            {
                if (approved[index].name != runtime[index].name ||
                    Mathf.Abs(approved[index].frameRate - runtime[index].frameRate) > 0.001f ||
                    Mathf.Abs(approved[index].length - runtime[index].length) > 0.001f)
                {
                    throw new InvalidOperationException(
                        "Kursa embedded animation metadata changed at clip " + index + ".");
                }
            }
        }

        private static BlenderPoseEvidence RequireBlenderPoseEvidence()
        {
            var path = ProjectAbsolutePath(RuntimeProjectionReportRelativePath);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException(
                    "Kursa Blender runtime projection report is missing: " + path);
            }
            var report = JsonUtility.FromJson<RuntimeProjectionExportReport>(
                File.ReadAllText(path, Encoding.UTF8)) ??
                throw new InvalidOperationException(
                    "Kursa Blender runtime projection report could not be parsed.");
            if (!string.Equals(
                    report.output_fbx_sha256,
                    ExpectedRuntimeProjectionFbxSha256,
                    StringComparison.OrdinalIgnoreCase) ||
                report.unauthorized_changed_vertices != 0 ||
                report.changed_pose_vertices <= 0 ||
                report.changed_left_arm_vertices <= 0 ||
                report.changed_right_arm_vertices != 0 ||
                !report.topology_material_uv0_preserved_after_pose ||
                !report.skin_weights_preserved_after_pose ||
                report.changed_rest_bones == null ||
                !report.changed_rest_bones.SequenceEqual(
                    ApprovedPoseBoneNames,
                    StringComparer.Ordinal) ||
                report.base_pose == null ||
                report.base_pose.final_angle_degrees > 0.001f ||
                report.base_pose.final_vertical_angle_degrees > 0.001f ||
                Mathf.Abs(report.base_pose.target_lateral_gap_ratio - 0.5f) > 0.000001f ||
                Mathf.Abs(
                    report.base_pose.final_lateral_gap -
                    report.base_pose.baseline_lateral_gap * 0.5f) > 0.001f ||
                report.base_pose.shield_center_before == null ||
                report.base_pose.shield_center_after == null ||
                report.base_pose.shield_center_before.Length != 3 ||
                report.base_pose.shield_center_after.Length != 3 ||
                report.base_pose.shield_center_after[2] <= 0f ||
                report.base_pose.right_arm == null ||
                Mathf.Abs(report.base_pose.right_arm.extension_ratio - 1f) > 0.000001f ||
                Mathf.Abs(report.base_pose.right_arm.outward_offset) > 0.000001f ||
                Mathf.Abs(report.base_pose.right_arm.target_thigh_clearance) > 0.000001f ||
                Mathf.Abs(
                    report.base_pose.right_arm.final_mesh_centroid_lateral_gap -
                    report.base_pose.right_arm.source_mesh_centroid_lateral_gap) > 0.000001f ||
                report.base_pose.right_arm.thigh_surface_overlap_count != 0 ||
                report.base_pose.right_arm.thigh_surface_clearance <
                    report.base_pose.right_arm.target_thigh_clearance ||
                report.base_pose.right_arm.final_mesh_bounds_min == null ||
                report.base_pose.right_arm.final_mesh_bounds_min.Length != 3 ||
                report.base_pose.right_arm.maximum_baked_mesh_position_error > 0.001f)
            {
                throw new InvalidOperationException(
                    "Kursa Blender runtime projection report does not prove the approved front shield base-pose contract.");
            }
            var before = new Vector3(
                report.base_pose.shield_center_before[0],
                report.base_pose.shield_center_before[1],
                report.base_pose.shield_center_before[2]);
            var after = new Vector3(
                report.base_pose.shield_center_after[0],
                report.base_pose.shield_center_after[1],
                report.base_pose.shield_center_after[2]);
            var centerShiftUnity = Vector3.Distance(before, after) / 100f;
            return new BlenderPoseEvidence(
                report.changed_pose_vertices,
                centerShiftUnity,
                Mathf.Abs(report.base_pose.final_lateral_gap) / 100f,
                report.base_pose.target_lateral_gap_ratio,
                report.base_pose.right_arm.target_upper_down_angle_degrees,
                report.base_pose.right_arm.target_forearm_down_angle_degrees,
                report.base_pose.right_arm.final_mesh_centroid_lateral_gap / 100f,
                report.base_pose.right_arm.final_mesh_bounds_min[1] / 100f,
                report.base_pose.right_arm.thigh_surface_clearance / 100f,
                report.base_pose.right_arm.outward_offset / 100f,
                report.base_pose.right_arm.thigh_surface_arm_polygons,
                report.base_pose.right_arm.thigh_surface_polygons);
        }

        private static string[] CanonicalVertexAttributeSignatures(Mesh mesh)
        {
            var signatures = VertexAttributeSignatures(mesh);
            Array.Sort(signatures, StringComparer.Ordinal);
            return signatures;
        }

        private static string[] CanonicalPositionSignatures(Mesh mesh)
        {
            var result = mesh.vertices
                .Select(value => string.Join(
                    ":",
                    Quantize(value.x),
                    Quantize(value.y),
                    Quantize(value.z)))
                .ToArray();
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static string[] CanonicalPositionNormalSignatures(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var result = vertices.Select((value, index) => string.Join(
                    ":",
                    Quantize(value.x), Quantize(value.y), Quantize(value.z),
                    Quantize(normals[index].x), Quantize(normals[index].y),
                    Quantize(normals[index].z)))
                .ToArray();
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static string[] CanonicalPositionTangentSignatures(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var tangents = mesh.tangents;
            var result = vertices.Select((value, index) => string.Join(
                    ":",
                    Quantize(value.x), Quantize(value.y), Quantize(value.z),
                    Quantize(tangents[index].x), Quantize(tangents[index].y),
                    Quantize(tangents[index].z), Quantize(tangents[index].w)))
                .ToArray();
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static string[] CanonicalPositionUvSignatures(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            var result = vertices.Select((value, index) => string.Join(
                    ":",
                    Quantize(value.x), Quantize(value.y), Quantize(value.z),
                    Quantize(uv[index].x), Quantize(uv[index].y)))
                .ToArray();
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static string[] CanonicalPositionWeightSignatures(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var result = vertices.Select((value, index) =>
            {
                var weight = weights[index];
                return string.Join(
                    ":",
                    Quantize(value.x), Quantize(value.y), Quantize(value.z),
                    weight.boneIndex0, Quantize(weight.weight0),
                    weight.boneIndex1, Quantize(weight.weight1),
                    weight.boneIndex2, Quantize(weight.weight2),
                    weight.boneIndex3, Quantize(weight.weight3));
            }).ToArray();
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        private static string[] VertexAttributeSignatures(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            var uv = mesh.uv;
            var weights = mesh.boneWeights;
            if (normals.Length != vertices.Length ||
                tangents.Length != vertices.Length ||
                uv.Length != vertices.Length ||
                weights.Length != vertices.Length)
            {
                throw new InvalidOperationException(
                    "Kursa imported mesh vertex attributes are incomplete: " + mesh.name);
            }
            var result = new string[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var coordinate = uv[index];
                var weight = weights[index];
                result[index] = string.Join(
                    ":",
                    Quantize(vertex.x), Quantize(vertex.y), Quantize(vertex.z),
                    Quantize(coordinate.x), Quantize(coordinate.y),
                    weight.boneIndex0, Quantize(weight.weight0),
                    weight.boneIndex1, Quantize(weight.weight1),
                    weight.boneIndex2, Quantize(weight.weight2),
                    weight.boneIndex3, Quantize(weight.weight3));
            }
            return result;
        }

        private static string[] CanonicalTriangleAttributeSignatures(Mesh mesh)
        {
            var vertexSignatures = VertexAttributeSignatures(mesh);
            var result = new List<string>();
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                var indices = mesh.GetIndices(submesh);
                if (indices.Length % 3 != 0)
                {
                    throw new InvalidOperationException(
                        "Kursa mesh contains a non-triangle submesh.");
                }
                for (var index = 0; index < indices.Length; index += 3)
                {
                    var triangle = new[]
                    {
                        vertexSignatures[indices[index]],
                        vertexSignatures[indices[index + 1]],
                        vertexSignatures[indices[index + 2]]
                    };
                    Array.Sort(triangle, StringComparer.Ordinal);
                    result.Add(triangle[0] + "|" + triangle[1] + "|" + triangle[2]);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result.ToArray();
        }

        private static int Quantize(float value)
        {
            return Mathf.RoundToInt(value * 100000f);
        }


        private static void RequireBindPoseParity(
            Matrix4x4[] expected,
            Matrix4x4[] actual,
            string label)
        {
            if (expected.Length != actual.Length)
            {
                throw new InvalidOperationException(
                    "Kursa bind pose count differs for " + label + ".");
            }
            for (var index = 0; index < expected.Length; index++)
            {
                for (var element = 0; element < 16; element++)
                {
                    if (Mathf.Abs(expected[index][element] - actual[index][element]) >
                        0.0001f)
                    {
                        throw new InvalidOperationException(
                            "Kursa bind poses differ for " + label + ".");
                    }
                }
            }
        }

        private static void RequireBoneOrder(
            SkinnedMeshRenderer expected,
            SkinnedMeshRenderer actual)
        {
            var expectedNames = expected.bones.Select(item => item.name).ToArray();
            var actualNames = actual.bones.Select(item => item.name).ToArray();
            if (!expectedNames.SequenceEqual(actualNames, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Kursa bone order differs from the approved source.");
            }
        }

        private static void ApplyApprovedBindPoseTransforms(
            SkinnedMeshRenderer destination,
            SkinnedMeshRenderer source)
        {
            var destinationByName = destination.bones.ToDictionary(
                bone => bone.name,
                StringComparer.Ordinal);
            foreach (var name in ApprovedPoseBoneNames)
            {
                var sourceIndex = Array.FindIndex(
                    source.bones,
                    bone => bone.name == name);
                if (sourceIndex < 0 ||
                    !destinationByName.TryGetValue(name, out var destinationBone))
                {
                    throw new InvalidOperationException(
                        "Kursa approved arm rest bone is missing during scene application: " +
                        name);
                }
                var desiredWorld =
                    destination.transform.localToWorldMatrix *
                    source.sharedMesh.bindposes[sourceIndex].inverse;
                var desiredLocal = destinationBone.parent.worldToLocalMatrix *
                    desiredWorld;
                var position = desiredLocal.GetColumn(3);
                destinationBone.localPosition = new Vector3(
                    position.x,
                    position.y,
                    position.z);
                destinationBone.localRotation = desiredLocal.rotation;
                destinationBone.localScale = desiredLocal.lossyScale;
                EditorUtility.SetDirty(destinationBone);
            }
        }

        private static void RequireApprovedBindPoseTransforms(
            SkinnedMeshRenderer renderer)
        {
            foreach (var name in ApprovedPoseBoneNames)
            {
                var index = Array.FindIndex(
                    renderer.bones,
                    bone => bone.name == name);
                if (index < 0)
                {
                    throw new InvalidOperationException(
                        "A placed Kursa is missing the approved bilateral base-pose bone " +
                        name + ".");
                }
                var skinMatrix =
                    renderer.transform.worldToLocalMatrix *
                    renderer.bones[index].localToWorldMatrix *
                    renderer.sharedMesh.bindposes[index];
                for (var element = 0; element < 16; element++)
                {
                    var expected = element % 5 == 0 ? 1f : 0f;
                    if (Mathf.Abs(skinMatrix[element] - expected) > 0.0002f)
                    {
                        throw new InvalidOperationException(
                            "A placed Kursa does not use the approved bilateral bind-rest base pose at " +
                            name + ". Element=" + element +
                            ", Actual=" + skinMatrix[element].ToString(
                                "R", CultureInfo.InvariantCulture) + ".");
                    }
                }
            }
        }

        private static int RequireTextureHashesMatch()
        {
            var sourceFolder = ProjectAbsolutePath(SampleTextureRelativePath);
            var destinationFolder = ProjectAbsolutePath(TextureFolder);
            var sourceFiles = Directory.GetFiles(sourceFolder, "*.png")
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .ToArray();
            var destinationFiles = Directory.GetFiles(destinationFolder, "*.png")
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)
                .ToArray();
            if (sourceFiles.Length != destinationFiles.Length)
            {
                throw new InvalidOperationException(
                    "Approved Kursa texture file count differs from the sample.");
            }
            for (var index = 0; index < sourceFiles.Length; index++)
            {
                if (Path.GetFileName(sourceFiles[index]) !=
                        Path.GetFileName(destinationFiles[index]) ||
                    !string.Equals(
                        Sha256(sourceFiles[index]),
                        Sha256(destinationFiles[index]),
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Approved Kursa texture differs from the sample: " +
                        Path.GetFileName(sourceFiles[index]));
                }
            }
            return sourceFiles.Length;
        }

        private static void RequireApprovedSourceFiles()
        {
            RequireHash(SourceModelPath, ExpectedSourceSha256);
            RequireHash(ApprovedModelPath, ExpectedApprovedFbxSha256);
            RequireHash(
                RuntimeProjectionModelPath,
                ExpectedRuntimeProjectionFbxSha256);
            RequireTextureHashesMatch();
        }

        private static void RequireHash(string assetPath, string expected)
        {
            var actual = Sha256(ProjectAbsolutePath(assetPath));
            if (!string.Equals(
                    actual,
                    expected,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Kursa approved source hash differs. Path=" + assetPath +
                    ", Expected=" + expected +
                    ", Actual=" + actual);
            }
        }

        private static Scene RequireActiveScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active loaded scene.");
            }
            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the approved Kursa appearance.");
            }
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException(
                    "Unity reports script compilation errors.");
            }
            return scene;
        }

        private static GameObject RequirePlacementRoot(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(item => item.name == PlacementRootName) ??
                   throw new InvalidOperationException(
                       "The approved Kursa placement root is missing.");
        }

        private static SceneContract CaptureContract(Scene scene, GameObject root)
        {
            return new SceneContract(
                RecursiveSignature(
                    root.transform,
                    includeRendererAssets: false,
                    ignoreApprovedArmPose: true),
                scene.GetRootGameObjects()
                    .Where(candidate => candidate != root)
                    .OrderBy(candidate => candidate.name, StringComparer.Ordinal)
                    .Select(candidate => RecursiveSignature(
                        candidate.transform,
                        includeRendererAssets: true,
                        ignoreApprovedArmPose: false))
                    .ToArray());
        }

        private static string RecursiveSignature(
            Transform root,
            bool includeRendererAssets,
            bool ignoreApprovedArmPose)
        {
            var builder = new StringBuilder();
            foreach (var current in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(GetPath(current, root)).Append('|')
                    .Append(current.gameObject.activeSelf).Append('|');
                if (ignoreApprovedArmPose &&
                    ApprovedPoseBoneNames.Contains(
                        current.name,
                        StringComparer.Ordinal))
                {
                    builder.Append("ApprovedBilateralArmBasePose|");
                }
                else
                {
                    builder.Append(Vec(current.localPosition)).Append('|')
                        .Append(Quat(current.localRotation)).Append('|')
                        .Append(Vec(current.localScale)).Append('|');
                }
                foreach (var componentName in current
                             .GetComponents<Component>()
                             .Where(item => item != null)
                             .Select(item => item.GetType().FullName)
                             .OrderBy(item => item, StringComparer.Ordinal))
                {
                    builder.Append(componentName).Append(';');
                }
                if (includeRendererAssets)
                {
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
                }
                builder.AppendLine();
            }
            return Sha256(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static void RequireContractPreserved(
            SceneContract before,
            SceneContract after)
        {
            if (before.KursaStructure != after.KursaStructure)
            {
                throw new InvalidOperationException(
                    "A Kursa transform outside the approved bilateral arm base pose, active state, component, rig, or animation object changed during appearance application.");
            }
            if (!before.OtherRoots.SequenceEqual(
                    after.OtherRoots,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside the approved Kursa placement changed during appearance application.");
            }
        }

        private static void CaptureComparison(GameObject source)
        {
            var outputPath = ProjectAbsolutePath(CaptureRelativePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(outputPath) ??
                throw new InvalidOperationException(
                    "Kursa comparison output folder is invalid."));
            var unityImage = CapturePreview(source, 32f);
            var referencePath = ProjectAbsolutePath(SampleReviewRelativePath);
            var reference = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D combined = null;
            try
            {
                if (!reference.LoadImage(File.ReadAllBytes(referencePath), false))
                {
                    throw new InvalidOperationException(
                        "Approved Kursa comparison render failed to load.");
                }
                if (reference.width != unityImage.width ||
                    reference.height != unityImage.height)
                {
                    throw new InvalidOperationException(
                        "Approved and Unity Kursa review images have different dimensions.");
                }
                combined = new Texture2D(
                    reference.width * 2,
                    reference.height,
                    TextureFormat.RGB24,
                    false);
                combined.SetPixels(0, 0, reference.width, reference.height, reference.GetPixels());
                combined.SetPixels(reference.width, 0, unityImage.width, unityImage.height, unityImage.GetPixels());
                combined.Apply();
                File.WriteAllBytes(outputPath, combined.EncodeToPNG());
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

        private static Texture2D CapturePreview(GameObject source, float angle)
        {
            var preview = new PreviewRenderUtility();
            GameObject clone = null;
            Texture2D rendered = null;
            try
            {
                clone = UnityEngine.Object.Instantiate(source);
                clone.name = "Kursa_ApprovedAppearance_Preview";
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
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Kursa preview clone contains no renderers.");
                }
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                    renderer.forceRenderingOff = false;
                    renderer.SetPropertyBlock(null);
                }
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1))
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.86f, 0.89f, 0.90f, 1f);
                camera.fieldOfView = 31.2f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.allowHDR = true;
                var forward = Quaternion.AngleAxis(angle, Vector3.up) *
                              clone.transform.forward;
                var distance = Mathf.Max(bounds.size.x, bounds.size.y) /
                    (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) *
                    1.10f;
                var target = bounds.center + Vector3.up * bounds.size.y * 0.02f;
                camera.transform.position =
                    target + forward.normalized * distance +
                    Vector3.up * bounds.size.y * 0.06f;
                camera.transform.LookAt(target, Vector3.up);

                preview.lights[0].transform.rotation = camera.transform.rotation;
                preview.lights[0].color = new Color(1f, 0.90f, 0.78f);
                preview.lights[0].intensity = 1.65f;
                preview.lights[0].shadows = LightShadows.Soft;
                preview.lights[1].transform.rotation = Quaternion.LookRotation(
                    (camera.transform.forward + camera.transform.right * 0.65f).normalized,
                    Vector3.up);
                preview.lights[1].color = new Color(0.58f, 0.76f, 1f);
                preview.lights[1].intensity = 0.85f;
                preview.lights[1].shadows = LightShadows.None;
                preview.ambientColor = new Color(0.3612f, 0.3738f, 0.378f, 1f);
                preview.BeginStaticPreview(new Rect(0f, 0f, 1280f, 1280f));
                preview.Render(true);
                rendered = preview.EndStaticPreview();
                if (rendered == null)
                {
                    throw new InvalidOperationException(
                        "Unity PreviewRenderUtility returned no Kursa image.");
                }
                return UnityEngine.Object.Instantiate(rendered);
            }
            finally
            {
                if (rendered != null)
                {
                    UnityEngine.Object.DestroyImmediate(rendered);
                }
                preview.Cleanup();
            }
        }

        private static void WriteReport(AppearanceResult result, string status)
        {
            var path = ProjectAbsolutePath(ReportRelativePath);
            var approvedMesh = RequireAssetRenderer(ApprovedModelPath).sharedMesh;
            var runtimeMesh = RequireAssetRenderer(RuntimeProjectionModelPath).sharedMesh;
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "Kursa appearance report folder is invalid."));
            File.WriteAllLines(
                path,
                new[]
                {
                    "Result=" + status,
                    "Scene=" + ScenePath,
                    "PlacementRoot=" + PlacementRootName,
                    "Slots=" + result.SlotCount,
                    "MaterialsPerRenderer=" + result.MaterialCount,
                    "ApprovedSampleVertices=" + ApprovedSampleVertices,
                    "Vertices=" + result.VertexCount,
                    "Triangles=" + result.TriangleCount,
                    "Bones=" + result.BoneCount,
                    "TextureFiles=" + result.TextureCount,
                    "SourceFbxSha256=" + ExpectedSourceSha256,
                    "ApprovedFbxSha256=" + ExpectedApprovedFbxSha256,
                    "RuntimeProjectionFbxSha256=" +
                        ExpectedRuntimeProjectionFbxSha256,
                    "TextureHashesMatch=True",
                    "BasePoseGeometryChanged=BilateralArmInfluenceOnly",
                    "TopologyMaterialUv0SkinWeightsPreserved=True",
                    "ChangedBlenderVertices=" + result.ChangedVertices,
                    "ChangedBindPoses=" + result.ChangedBindPoses +
                        ":LeftArm,LeftForeArm,LeftHand",
                    "EmbeddedAnimationLocalChannelsPreserved=True",
                    "ShieldBasePoseForwardAngleDegrees=" +
                        result.ShieldForwardAngleDegrees.ToString(
                            "F6", CultureInfo.InvariantCulture),
                    "ShieldBasePoseVerticalAngleDegrees=" +
                        result.ShieldVerticalAngleDegrees.ToString(
                            "F6", CultureInfo.InvariantCulture),
                    "ShieldBasePoseCenterShift=" +
                        result.ShieldCenterShift.ToString(
                            "R", CultureInfo.InvariantCulture),
                    "ShieldBasePoseFrontOffset=" +
                        result.ShieldFrontOffset.ToString(
                            "R", CultureInfo.InvariantCulture),
                    "ShieldTorsoLateralGap=" +
                        result.ShieldTorsoLateralGap.ToString(
                            "R", CultureInfo.InvariantCulture),
                    "ShieldTorsoLateralGapRatio=" +
                        result.ShieldTorsoLateralGapRatio.ToString(
                            "R", CultureInfo.InvariantCulture),
                    "RightUpperDownAngleDegrees=" +
                        result.RightUpperDownAngleDegrees.ToString(
                            "F6", CultureInfo.InvariantCulture),
                    "RightForearmDownAngleDegrees=" +
                        result.RightForearmDownAngleDegrees.ToString(
                            "F6", CultureInfo.InvariantCulture),
                    "RightArmTorsoLateralGap=" +
                        result.RightArmLateralGap.ToString(
                            "R", CultureInfo.InvariantCulture),
                    "RightArmMinimumHeight=" +
                        result.RightArmMinimumHeight.ToString(
                            "R", CultureInfo.InvariantCulture),
                    "RightArmOutwardOffset=" +
                        result.RightArmOutwardOffset.ToString(
                            "R", CultureInfo.InvariantCulture),
                    "RightArmThighSurfaceOverlapPairs=0",
                    "RightArmThighSurfaceClearance=" +
                        result.RightArmThighClearance.ToString(
                            "R", CultureInfo.InvariantCulture),
                    "RuntimeEyeProjectionUvChannels=UV1:Left;UV2:Right;UV3:SignedDepth",
                    "RuntimeEyeProjectionVertices=" +
                        result.LeftEyeProjectionCount + "/" +
                        result.RightEyeProjectionCount,
                    "NormalsTangents=ReimportedForApprovedBilateralArmBasePose",
                    "ApprovedStudioLightingParity=Ambient:0.88;Key:3.00;Fill:1.35;Rim:2.10",
                    "ApprovedProjectionCoordinateScale=100",
                    "ApprovedSampleHeight=170",
                    "UnityObjectHeight=1.7",
                    "ApprovedMeshBoundsCenter=" + Vec(approvedMesh.bounds.center),
                    "ApprovedMeshBoundsSize=" + Vec(approvedMesh.bounds.size),
                    "RuntimeMeshBoundsCenter=" + Vec(runtimeMesh.bounds.center),
                    "RuntimeMeshBoundsSize=" + Vec(runtimeMesh.bounds.size),
                    "EyeLeft=Center:3.343094,151.815475,24.579956;Size:8.348116,8.988050;Depth:2.05;Polygon:3801",
                    "EyeRight=Center:5.916458,152.454803,19.357758;Size:10.076670,8.897684;Depth:2.05;Polygon:3627",
                    "EyeProjectionNormal=0.552875,-0.117583,0.824926",
                    "SceneTransformsChanged=LeftArm,LeftForeArm,LeftHandOnly",
                    "OtherSceneRootsChanged=False"
                },
                new UTF8Encoding(false));
        }

        // Reports the FBX handedness mapping without changing any approved eye value.
        // Distances are measured from the approved frame-1 eye centers to imported
        // mesh vertices after the exact 100 sample-units-per-Unity-unit conversion.
        private static string ProjectionMappingDistances(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var centers = new[]
            {
                new Vector3(3.343094f, 151.815475f, 24.579956f),
                new Vector3(5.916458f, 152.454803f, 19.357758f)
            };
            return string.Join(
                ";",
                "ProjectionMappingNearestVertexSampleUnits",
                "XYZ=" + NearestCenterDistance(vertices, centers, 1f, 1f),
                "NegXYZ=" + NearestCenterDistance(vertices, centers, -1f, 1f),
                "XYNegZ=" + NearestCenterDistance(vertices, centers, 1f, -1f),
                "NegXYNegZ=" + NearestCenterDistance(vertices, centers, -1f, -1f));
        }

        private static string NearestCenterDistance(
            Vector3[] unityVertices,
            Vector3[] sampleCenters,
            float xSign,
            float zSign)
        {
            var total = 0f;
            foreach (var center in sampleCenters)
            {
                var mappedCenter = new Vector3(
                    center.x * xSign,
                    center.y,
                    center.z * zSign);
                total += unityVertices.Min(vertex =>
                    Vector3.Distance(vertex * 100f, mappedCenter));
            }
            return (total / sampleCenters.Length).ToString(
                "F6",
                CultureInfo.InvariantCulture);
        }

        // Uses the actual static-review bone pose and the face submesh so the
        // approved frame-1 eye projection can be checked independently of rendering.
        private static string StaticReviewEyeMaskDiagnostics()
        {
            var scene = SceneManager.GetActiveScene();
            var root = RequirePlacementRoot(scene);
            var renderer = RequireSceneRenderers(root)[0];
            var approvedRenderer = RequireAssetRenderer(ApprovedModelPath);
            var bakedMesh = new Mesh();
            try
            {
                renderer.BakeMesh(bakedMesh);
                var faceSubmeshIndex = Array.FindIndex(
                    approvedRenderer.sharedMaterials,
                    material => material != null && material.name ==
                        "Kursa_Face_Metal_Blue_Optics");
                if (faceSubmeshIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Approved Kursa face material submesh is missing.");
                }
                var faceVertexIndices = bakedMesh
                    .GetIndices(faceSubmeshIndex)
                    .Distinct()
                    .ToArray();
                var sharedFaceVertexIndices = approvedRenderer.sharedMesh
                    .GetIndices(faceSubmeshIndex)
                    .Distinct()
                    .ToArray();
                var faceBounds = new Bounds(
                    bakedMesh.vertices[faceVertexIndices[0]] * 100f,
                    Vector3.zero);
                foreach (var index in faceVertexIndices.Skip(1))
                {
                    faceBounds.Encapsulate(bakedMesh.vertices[index] * 100f);
                }
                var centers = new[]
                {
                    new Vector3(3.343094f, 151.815475f, 24.579956f),
                    new Vector3(5.916458f, 152.454803f, 19.357758f)
                };
                return string.Join(
                    ";",
                    "StaticReviewFaceEyeMaskVertexCounts",
                    "FaceSubmesh=" + faceSubmeshIndex,
                    "SharedXYZ=" + EyeMaskCounts(
                        approvedRenderer.sharedMesh.vertices,
                        sharedFaceVertexIndices,
                        1f,
                        1f),
                    "SharedNegXYZ=" + EyeMaskCounts(
                        approvedRenderer.sharedMesh.vertices,
                        sharedFaceVertexIndices,
                        -1f,
                        1f),
                    "SharedXYNegZ=" + EyeMaskCounts(
                        approvedRenderer.sharedMesh.vertices,
                        sharedFaceVertexIndices,
                        1f,
                        -1f),
                    "SharedNegXYNegZ=" + EyeMaskCounts(
                        approvedRenderer.sharedMesh.vertices,
                        sharedFaceVertexIndices,
                        -1f,
                        -1f),
                    "FaceBoundsCenter=" + Vec(faceBounds.center),
                    "FaceBoundsSize=" + Vec(faceBounds.size),
                    "FaceNearestXYZ=" + NearestIndexedCenterDistance(
                        bakedMesh.vertices, faceVertexIndices, centers, 1f, 1f),
                    "FaceNearestNegXYZ=" + NearestIndexedCenterDistance(
                        bakedMesh.vertices, faceVertexIndices, centers, -1f, 1f),
                    "FaceNearestXYNegZ=" + NearestIndexedCenterDistance(
                        bakedMesh.vertices, faceVertexIndices, centers, 1f, -1f),
                    "FaceNearestNegXYNegZ=" + NearestIndexedCenterDistance(
                        bakedMesh.vertices, faceVertexIndices, centers, -1f, -1f),
                    "XYZ=" + EyeMaskCounts(
                        bakedMesh.vertices, faceVertexIndices, 1f, 1f),
                    "NegXYZ=" + EyeMaskCounts(
                        bakedMesh.vertices, faceVertexIndices, -1f, 1f),
                    "XYNegZ=" + EyeMaskCounts(
                        bakedMesh.vertices, faceVertexIndices, 1f, -1f),
                    "NegXYNegZ=" + EyeMaskCounts(
                        bakedMesh.vertices, faceVertexIndices, -1f, -1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakedMesh);
            }
        }

        private static string NearestIndexedCenterDistance(
            Vector3[] unityVertices,
            int[] indices,
            Vector3[] sampleCenters,
            float xSign,
            float zSign)
        {
            var total = 0f;
            foreach (var center in sampleCenters)
            {
                total += indices.Min(index =>
                {
                    var vertex = unityVertices[index] * 100f;
                    vertex.x *= xSign;
                    vertex.z *= zSign;
                    return Vector3.Distance(vertex, center);
                });
            }
            return (total / sampleCenters.Length).ToString(
                "F6",
                CultureInfo.InvariantCulture);
        }

        private static string EyeMaskCounts(
            Vector3[] unityVertices,
            int[] faceVertexIndices,
            float xSign,
            float zSign)
        {
            var left = EyeMaskCount(
                unityVertices,
                faceVertexIndices,
                xSign,
                zSign,
                new Vector3(3.343094f, 151.815475f, 24.579956f),
                new Vector3(-0.182243f, -0.571750f, 0.799931f),
                new Vector2(8.348116f, 8.988050f));
            var right = EyeMaskCount(
                unityVertices,
                faceVertexIndices,
                xSign,
                zSign,
                new Vector3(5.916458f, 152.454803f, 19.357758f),
                new Vector3(0.257649f, -0.965079f, -0.047329f),
                new Vector2(10.076670f, 8.897684f));
            return left + "," + right;
        }

        private static int EyeMaskCount(
            Vector3[] unityVertices,
            int[] faceVertexIndices,
            float xSign,
            float zSign,
            Vector3 center,
            Vector3 surfaceNormal,
            Vector2 size)
        {
            var projectionNormal = new Vector3(
                0.552875f,
                -0.117583f,
                0.824926f).normalized;
            var vertical = (Vector3.up -
                projectionNormal * Vector3.Dot(Vector3.up, projectionNormal)).normalized;
            var horizontal = Vector3.Cross(vertical, projectionNormal).normalized;
            var count = 0;
            foreach (var index in faceVertexIndices)
            {
                var vertex = unityVertices[index] * 100f;
                vertex.x *= xSign;
                vertex.z *= zSign;
                var delta = vertex - center;
                var u = Vector3.Dot(delta, horizontal) / size.x + 0.5f;
                var v = Vector3.Dot(delta, vertical) / size.y + 0.5f;
                if (u >= 0f && u <= 1f && v >= 0f && v <= 1f &&
                    Mathf.Abs(Vector3.Dot(delta, surfaceNormal.normalized)) < 2.05f)
                {
                    count++;
                }
            }
            return count;
        }

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
            {
                count += mesh.GetIndexCount(index) > int.MaxValue
                    ? throw new InvalidOperationException("Kursa mesh index count is too large.")
                    : (int)mesh.GetIndexCount(index) / 3;
            }
            return count;
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

        private static string GetPath(Transform current, Transform root)
        {
            if (current == root)
            {
                return root.name;
            }
            var names = new Stack<string>();
            var cursor = current;
            while (cursor != null && cursor != root)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }
            return root.name + "/" + string.Join("/", names);
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

        private enum Feature
        {
            Basic = 0,
            Torso = 1,
            Hood = 2,
            Face = 3
        }

        private sealed class NormalCluster
        {
            public Vector3 Sum;
            public float Area;
        }

        private readonly struct InfluenceGeometry
        {
            public readonly int Count;
            public readonly Vector3 Centroid;
            public readonly Bounds Bounds;

            public InfluenceGeometry(int count, Vector3 centroid, Bounds bounds)
            {
                Count = count;
                Centroid = centroid;
                Bounds = bounds;
            }
        }

        private readonly struct SurfaceTriangle
        {
            public readonly Vector3 A;
            public readonly Vector3 B;
            public readonly Vector3 C;

            public SurfaceTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;
            }
        }

        private readonly struct SurfaceSeparation
        {
            public readonly int ArmTriangles;
            public readonly int ThighTriangles;
            public readonly int OverlapPairs;
            public readonly float MinimumClearance;

            public SurfaceSeparation(
                int armTriangles,
                int thighTriangles,
                int overlapPairs,
                float minimumClearance)
            {
                ArmTriangles = armTriangles;
                ThighTriangles = thighTriangles;
                OverlapPairs = overlapPairs;
                MinimumClearance = minimumClearance;
            }
        }

        [Serializable]
        private sealed class RuntimeProjectionExportReport
        {
            public string output_fbx_sha256;
            public bool topology_material_uv0_preserved_after_pose;
            public bool skin_weights_preserved_after_pose;
            public int changed_pose_vertices;
            public int changed_left_arm_vertices;
            public int changed_right_arm_vertices;
            public int unauthorized_changed_vertices;
            public string[] changed_rest_bones;
            public RuntimeProjectionBasePoseReport base_pose;
        }

        [Serializable]
        private sealed class RuntimeProjectionBasePoseReport
        {
            public float final_angle_degrees;
            public float final_vertical_angle_degrees;
            public float baseline_lateral_gap;
            public float target_lateral_gap_ratio;
            public float final_lateral_gap;
            public float[] shield_center_before;
            public float[] shield_center_after;
            public RuntimeProjectionRightArmReport right_arm;
        }

        [Serializable]
        private sealed class RuntimeProjectionRightArmReport
        {
            public float extension_ratio;
            public float outward_offset;
            public float target_thigh_clearance;
            public float target_upper_down_angle_degrees;
            public float target_forearm_down_angle_degrees;
            public float source_mesh_centroid_lateral_gap;
            public float final_mesh_centroid_lateral_gap;
            public int thigh_surface_arm_polygons;
            public int thigh_surface_polygons;
            public int thigh_surface_overlap_count;
            public float thigh_surface_clearance;
            public float[] final_mesh_bounds_min;
            public float maximum_baked_mesh_position_error;
        }

        private readonly struct BlenderPoseEvidence
        {
            public readonly int ChangedVertices;
            public readonly float CenterShiftUnity;
            public readonly float FinalLateralGapUnity;
            public readonly float TargetLateralGapRatio;
            public readonly float RightUpperDownAngleDegrees;
            public readonly float RightForearmDownAngleDegrees;
            public readonly float RightArmCentroidLateralGapUnity;
            public readonly float RightArmMinimumHeightUnity;
            public readonly float RightArmThighClearanceUnity;
            public readonly float RightArmOutwardOffsetUnity;
            public readonly int RightArmSurfaceTriangles;
            public readonly int RightThighSurfaceTriangles;

            public BlenderPoseEvidence(
                int changedVertices,
                float centerShiftUnity,
                float finalLateralGapUnity,
                float targetLateralGapRatio,
                float rightUpperDownAngleDegrees,
                float rightForearmDownAngleDegrees,
                float rightArmCentroidLateralGapUnity,
                float rightArmMinimumHeightUnity,
                float rightArmThighClearanceUnity,
                float rightArmOutwardOffsetUnity,
                int rightArmSurfaceTriangles,
                int rightThighSurfaceTriangles)
            {
                ChangedVertices = changedVertices;
                CenterShiftUnity = centerShiftUnity;
                FinalLateralGapUnity = finalLateralGapUnity;
                TargetLateralGapRatio = targetLateralGapRatio;
                RightUpperDownAngleDegrees = rightUpperDownAngleDegrees;
                RightForearmDownAngleDegrees = rightForearmDownAngleDegrees;
                RightArmCentroidLateralGapUnity = rightArmCentroidLateralGapUnity;
                RightArmMinimumHeightUnity = rightArmMinimumHeightUnity;
                RightArmThighClearanceUnity = rightArmThighClearanceUnity;
                RightArmOutwardOffsetUnity = rightArmOutwardOffsetUnity;
                RightArmSurfaceTriangles = rightArmSurfaceTriangles;
                RightThighSurfaceTriangles = rightThighSurfaceTriangles;
            }
        }

        private readonly struct ShieldBasePoseResult
        {
            public readonly float ForwardAngleDegrees;
            public readonly float VerticalAngleDegrees;
            public readonly float CenterShift;
            public readonly float FrontOffset;
            public readonly float TorsoLateralGap;
            public readonly float TorsoLateralGapRatio;
            public readonly float RightUpperDownAngleDegrees;
            public readonly float RightForearmDownAngleDegrees;
            public readonly float RightArmLateralGap;
            public readonly float RightArmMinimumHeight;
            public readonly float RightArmThighClearance;
            public readonly float RightArmOutwardOffset;
            public readonly int ChangedVertices;
            public readonly int ChangedBindPoses;

            public ShieldBasePoseResult(
                float forwardAngleDegrees,
                float verticalAngleDegrees,
                float centerShift,
                float frontOffset,
                float torsoLateralGap,
                float torsoLateralGapRatio,
                float rightUpperDownAngleDegrees,
                float rightForearmDownAngleDegrees,
                float rightArmLateralGap,
                float rightArmMinimumHeight,
                float rightArmThighClearance,
                float rightArmOutwardOffset,
                int changedVertices,
                int changedBindPoses)
            {
                ForwardAngleDegrees = forwardAngleDegrees;
                VerticalAngleDegrees = verticalAngleDegrees;
                CenterShift = centerShift;
                FrontOffset = frontOffset;
                TorsoLateralGap = torsoLateralGap;
                TorsoLateralGapRatio = torsoLateralGapRatio;
                RightUpperDownAngleDegrees = rightUpperDownAngleDegrees;
                RightForearmDownAngleDegrees = rightForearmDownAngleDegrees;
                RightArmLateralGap = rightArmLateralGap;
                RightArmMinimumHeight = rightArmMinimumHeight;
                RightArmThighClearance = rightArmThighClearance;
                RightArmOutwardOffset = rightArmOutwardOffset;
                ChangedVertices = changedVertices;
                ChangedBindPoses = changedBindPoses;
            }
        }

        private readonly struct MaterialDefinition
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly float NormalStrength;
            public readonly float TextureScale;
            public readonly Feature Feature;

            public MaterialDefinition(
                string id,
                string displayName,
                float normalStrength,
                float textureScale,
                Feature feature)
            {
                Id = id;
                DisplayName = displayName;
                NormalStrength = normalStrength;
                TextureScale = textureScale;
                Feature = feature;
            }

            public string MaterialPath =>
                MaterialFolder + "/Kursa_" + Id + "_Approved.mat";
        }

        private readonly struct SceneContract
        {
            public readonly string KursaStructure;
            public readonly string[] OtherRoots;

            public SceneContract(string kursaStructure, string[] otherRoots)
            {
                KursaStructure = kursaStructure;
                OtherRoots = otherRoots;
            }
        }

        private readonly struct AppearanceResult
        {
            public readonly int SlotCount;
            public readonly int MaterialCount;
            public readonly int VertexCount;
            public readonly int TriangleCount;
            public readonly int BoneCount;
            public readonly int TextureCount;
            public readonly int LeftEyeProjectionCount;
            public readonly int RightEyeProjectionCount;
            public readonly float ShieldForwardAngleDegrees;
            public readonly float ShieldVerticalAngleDegrees;
            public readonly float ShieldCenterShift;
            public readonly float ShieldFrontOffset;
            public readonly float ShieldTorsoLateralGap;
            public readonly float ShieldTorsoLateralGapRatio;
            public readonly float RightUpperDownAngleDegrees;
            public readonly float RightForearmDownAngleDegrees;
            public readonly float RightArmLateralGap;
            public readonly float RightArmMinimumHeight;
            public readonly float RightArmThighClearance;
            public readonly float RightArmOutwardOffset;
            public readonly int ChangedVertices;
            public readonly int ChangedBindPoses;

            public AppearanceResult(
                int slotCount,
                int materialCount,
                int vertexCount,
                int triangleCount,
                int boneCount,
                int textureCount,
                int leftEyeProjectionCount,
                int rightEyeProjectionCount,
                float shieldForwardAngleDegrees,
                float shieldVerticalAngleDegrees,
                float shieldCenterShift,
                float shieldFrontOffset,
                float shieldTorsoLateralGap,
                float shieldTorsoLateralGapRatio,
                float rightUpperDownAngleDegrees,
                float rightForearmDownAngleDegrees,
                float rightArmLateralGap,
                float rightArmMinimumHeight,
                float rightArmThighClearance,
                float rightArmOutwardOffset,
                int changedVertices,
                int changedBindPoses)
            {
                SlotCount = slotCount;
                MaterialCount = materialCount;
                VertexCount = vertexCount;
                TriangleCount = triangleCount;
                BoneCount = boneCount;
                TextureCount = textureCount;
                LeftEyeProjectionCount = leftEyeProjectionCount;
                RightEyeProjectionCount = rightEyeProjectionCount;
                ShieldForwardAngleDegrees = shieldForwardAngleDegrees;
                ShieldVerticalAngleDegrees = shieldVerticalAngleDegrees;
                ShieldCenterShift = shieldCenterShift;
                ShieldFrontOffset = shieldFrontOffset;
                ShieldTorsoLateralGap = shieldTorsoLateralGap;
                ShieldTorsoLateralGapRatio = shieldTorsoLateralGapRatio;
                RightUpperDownAngleDegrees = rightUpperDownAngleDegrees;
                RightForearmDownAngleDegrees = rightForearmDownAngleDegrees;
                RightArmLateralGap = rightArmLateralGap;
                RightArmMinimumHeight = rightArmMinimumHeight;
                RightArmThighClearance = rightArmThighClearance;
                RightArmOutwardOffset = rightArmOutwardOffset;
                ChangedVertices = changedVertices;
                ChangedBindPoses = changedBindPoses;
            }
        }
    }
}
