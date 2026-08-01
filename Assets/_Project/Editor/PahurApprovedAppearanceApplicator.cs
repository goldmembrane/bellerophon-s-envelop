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

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static class PahurApprovedAppearanceApplicator
    {
        private const string Revision =
            "2026-07-31_red_brown_palette_rework_22";
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Pahur Enemy Placement";
        private const string ModelName = "Pahur_Model";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx";
        private const string AppearanceRoot =
            "Assets/_Project/Art/Enemies/Pahur/ApprovedAppearance";
        private const string TextureFolder = AppearanceRoot + "/Textures";
        private const string MaterialFolder = AppearanceRoot + "/Materials";
        private const string ShaderPath =
            "Assets/_Project/Art/Enemies/Pahur/Shaders/PahurApprovedAppearance.shader";
        private const string ReportRelativePath =
            "artSample/enemies/pahur/appearance_reference_sync/unity_validation/Pahur_ApprovedAppearance_UnityReport.txt";
        private const string CaptureFolderRelativePath =
            "artSample/enemies/pahur/appearance_reference_sync/unity_validation";
        private const string SceneParityCaptureRelativePath =
            "docs/validation/pahur_appearance_scene_parity_2026-08-01/Pahur_ActualScene_ApprovedParity.png";
        private const string ExpectedFbxSha256 =
            "5A2354A0B89A451DB98EF5AA5409C61EE12CF5638FA6EC2C88110B4C146B537C";
        private const int ExpectedSlots = 11;
        private const int ExpectedTriangles = 4330;
        private const int ExpectedBones = 24;

        private static readonly string[] SlotNames =
        {
            "Pahur_01_Static_Review",
            "Pahur_02_Idle",
            "Pahur_03_Move",
            "Pahur_04_MiniFlamethrower",
            "Pahur_05_BreakthroughFlamethrower",
            "Pahur_06_GuardianFlamethrower",
            "Pahur_07_Stop",
            "Pahur_08_ToGuardianStance",
            "Pahur_09_FromGuardianStance",
            "Pahur_10_Hit",
            "Pahur_11_Death"
        };

        private static readonly MaterialDefinition[] MaterialDefinitions =
        {
            new("armor_bluegray", "Pahur_Armor_RedBrown_RigidPlate", 0.14f, 1f, Feature.Basic),
            new("light_steel", "Pahur_Light_Steel_Panels", 0.14f, 1f, Feature.Basic),
            new("leg_steel", "Pahur_Dark_RedBrown_Leg_Steel", 0.14f, 1f, Feature.Basic),
            new("dark_mechanics", "Pahur_Dark_Mechanics", 0.18f, 1.5f, Feature.Basic),
            new("torso_rigid_shell", "Pahur_Torso_Outer_RedBrown_Armor", 0.06f, 1.5f, Feature.Basic),
            new("torso_center_plate", "Pahur_Torso_Center_LightSteel_Plate", 0.06f, 1.5f, Feature.Basic),
            new("torso_inner_mechanics", "Pahur_Torso_Inner_Dark_Mechanics", 0.10f, 1.5f, Feature.Basic),
            new("torso_pelvis_plate", "Pahur_Torso_Pelvis_RedBrown_Plate", 0.06f, 1.5f, Feature.Basic),
            new("shoulder_machine_blue", "Pahur_Shoulder_Mechanical_RedBrown", 0.14f, 1f, Feature.Machine, 5f, 0.04f, 0.18f, new Color(0.060f, 0.025f, 0.020f, 1f)),
            new("left_arm_machine", "Pahur_LeftArm_Segmented_Mechanical", 0.14f, 1f, Feature.Machine, 6f, 0.05f, 0.14f, new Color(0.060f, 0.025f, 0.020f, 1f)),
            new("left_hand_machine", "Pahur_LeftHand_Articulated_Mechanical", 0.16f, 1.5f, Feature.Machine, 10f, 0.08f, 0.10f, new Color(0.025f, 0.045f, 0.060f, 1f)),
            new("hood_navy_cloth", "Pahur_Hood_Dark_RedBrown_Cloth", 0.12f, 0.8f, Feature.Hood),
            new("face_metal", "Pahur_Faceplate_Dark_Metal", 0f, 1.5f, Feature.Face),
            new("weapon_gunmetal", "Pahur_Weapon_Gunmetal", 0.14f, 1.5f, Feature.Basic),
            new("heat_bronze", "Pahur_Heat_Bronze", 0.18f, 1.5f, Feature.Basic),
            new("fuel_tank_steel", "Pahur_Fuel_Tank_Worn_Steel", 0.08f, 1.5f, Feature.Basic),
            new("hose_rubber", "Pahur_Hose_Rubber", 0.18f, 1.5f, Feature.Basic),
            new("optic_blue", "Pahur_Optic_WarmRed_Emission", 0f, 1.5f, Feature.Emission, emissionStrength: 3.6f),
            new("flame_orange", "Pahur_Flame_Orange_Emission", 0f, 1.5f, Feature.Emission, emissionStrength: 2.8f),
            new("orange_trim", "Pahur_Orange_Armor_Trim", 0.18f, 1.5f, Feature.Basic)
        };

        [MenuItem("Bellerophon/Enemies/Pahur/Apply Approved Appearance")]
        public static void ApplyApprovedPahurAppearance()
        {
            var scene = RequireActiveScene(requireClean: true);
            var root = RequirePlacementRoot(scene);
            var before = CaptureContract(scene, root);

            RequireExactModelHash();
            ConfigureTextureAssets();
            ConfigureModelImporter();
            var approvedMaterials = CreateOrUpdateMaterials();
            var orderedMaterials = ResolveApprovedMaterialOrder(approvedMaterials);

            foreach (var renderer in RequireSceneRenderers(root))
            {
                if (renderer.sharedMesh == null)
                {
                    throw new InvalidOperationException(
                        "Pahur scene renderer has no mesh: " + renderer.name);
                }

                if (renderer.sharedMesh.subMeshCount != orderedMaterials.Length)
                {
                    throw new InvalidOperationException(
                        "Pahur scene material slot count differs from the approved FBX. Renderer=" +
                        renderer.name +
                        ", SubMeshes=" + renderer.sharedMesh.subMeshCount +
                        ", ApprovedMaterials=" + orderedMaterials.Length);
                }

                renderer.sharedMaterials = orderedMaterials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
            }

            var after = CaptureContract(scene, root);
            RequireContractPreserved(before, after);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the approved Pahur appearance.");
            }

            AssetDatabase.SaveAssets();
            var result = InspectAppliedState(scene, root, approvedMaterials);
            WriteReport(result, "APPLY_PASS");
            Debug.Log(
                "PahurApprovedAppearanceApplied Result=PASS" +
                ", Revision=" + Revision +
                ", Slots=" + result.SlotCount +
                ", MaterialsPerRenderer=" + result.MaterialCount +
                ", Triangles=" + result.TriangleCount +
                ", Bones=" + result.BoneCount +
                ", FbxSha256=" + result.FbxSha256 +
                ", SlotTransformsPreserved=True" +
                ", GameplayComponentsPreserved=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Validate Approved Appearance")]
        public static void ValidateApprovedPahurAppearance()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var materials = LoadApprovedMaterials();
            var result = InspectAppliedState(scene, root, materials);
            WriteReport(result, "VALIDATION_PASS");
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Pahur appearance validation changed the scene dirty state.");
            }

            Debug.Log(
                "PahurApprovedAppearanceValidated Result=PASS" +
                ", Revision=" + Revision +
                ", ActiveScene=" + scene.path +
                ", Slots=" + result.SlotCount +
                ", MaterialsPerRenderer=" + result.MaterialCount +
                ", Triangles=" + result.TriangleCount +
                ", Bones=" + result.BoneCount +
                ", FbxSha256=" + result.FbxSha256 +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Capture Approved Appearance")]
        public static void CaptureApprovedPahurAppearance()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            InspectAppliedState(scene, root, LoadApprovedMaterials());

            var source = root.transform.Find(SlotNames[0] + "/" + ModelName);
            if (source == null)
            {
                throw new InvalidOperationException(
                    "The static-review Pahur model is missing.");
            }

            var outputFolder = ProjectAbsolutePath(CaptureFolderRelativePath);
            Directory.CreateDirectory(outputFolder);
            CapturePreview(
                source.gameObject,
                Path.Combine(outputFolder, "Pahur_UnityApprovedAppearance_Front.png"),
                0f);
            CapturePreview(
                source.gameObject,
                Path.Combine(outputFolder, "Pahur_UnityApprovedAppearance_ThreeQuarter.png"),
                32f);

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Pahur appearance capture changed the active scene dirty state.");
            }

            Debug.Log(
                "PahurApprovedAppearanceCaptured Result=PASS" +
                ", Revision=" + Revision +
                ", Folder=" + CaptureFolderRelativePath +
                ", PreviewSceneOnly=True" +
                ", ActiveSceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Apply Approved Scene Parity")]
        public static void ApplyPahurApprovedAppearanceSceneLightingParity()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            CreateOrUpdateMaterials();
            AssetDatabase.SaveAssets();
            InspectPahurApprovedAppearanceSceneLightingParity();
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur scene-parity material application changed the active scene dirty state.");
            }

            Debug.Log(
                "PahurApprovedAppearanceSceneParityApplied Result=PASS" +
                ", Materials=" + MaterialDefinitions.Length +
                ", PreviewUnlit=False" +
                ", MeshChanged=False" +
                ", AnimationChanged=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Inspect Approved Scene Parity")]
        public static void InspectPahurApprovedAppearanceSceneLightingParity()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                throw new InvalidOperationException(
                    "Approved Pahur shader is missing: " + ShaderPath);
            if (!shader.isSupported)
            {
                throw new InvalidOperationException(
                    "Approved Pahur shader is not supported by the active renderer.");
            }

            var materials = LoadApprovedMaterials().Values.Distinct().ToArray();
            foreach (var material in materials)
            {
                RequireSceneParityMaterial(material, shader);
            }

            var rendererCount = 0;
            var modelRendererCount = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                rendererCount++;
                if (renderer is SkinnedMeshRenderer)
                {
                    modelRendererCount++;
                    if (renderer.sharedMaterials.Length == 0)
                    {
                        throw new InvalidOperationException(
                            renderer.name + " has no Pahur model materials.");
                    }
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || material.shader != shader)
                        {
                            throw new InvalidOperationException(
                                renderer.name +
                                " contains a material outside the approved Pahur appearance.");
                        }
                    }
                }
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null && material.shader == shader)
                    {
                        RequireSceneParityMaterial(material, shader);
                    }
                }
            }

            if (rendererCount == 0)
            {
                throw new InvalidOperationException(
                    "The Pahur placement contains no renderers.");
            }
            if (modelRendererCount == 0)
            {
                throw new InvalidOperationException(
                    "The Pahur placement contains no skinned model renderers.");
            }

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur scene-parity inspection changed the active scene dirty state.");
            }

            Debug.Log(
                "PahurApprovedAppearanceSceneParityInspected Result=PASS" +
                ", Materials=" + materials.Length +
                ", Renderers=" + rendererCount +
                ", ModelRenderers=" + modelRendererCount +
                ", PreviewUnlit=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Capture Actual Scene Parity")]
        public static void CapturePahurApprovedAppearanceActualSceneParity()
        {
            var scene = RequireActiveScene(requireClean: false);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var source = root.transform.Find(SlotNames[0] + "/" + ModelName);
            if (source == null)
            {
                throw new InvalidOperationException(
                    "The static-review Pahur model is missing.");
            }

            CaptureActualSceneModel(
                source.gameObject,
                ProjectAbsolutePath(SceneParityCaptureRelativePath));
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur actual-scene parity capture changed the active scene dirty state.");
            }

            Debug.Log(
                "PahurApprovedAppearanceActualSceneParityCaptured Result=PASS" +
                ", Image=" + SceneParityCaptureRelativePath +
                ", TemporaryLights=0" +
                ", PreviewUnlit=False" +
                ", SceneSaved=False.");
        }

        private static void ConfigureModelImporter()
        {
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Pahur ModelImporter is missing.");
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
            foreach (var definition in MaterialDefinitions)
            {
                if (definition.Feature == Feature.Emission)
                {
                    ConfigureTexture(
                        TextureFolder + "/pahur_" + definition.Id +
                        "_emission.png",
                        true,
                        false,
                        false);
                    continue;
                }

                ConfigureTexture(definition.TexturePath("albedo"), true, false, false);
                ConfigureTexture(definition.TexturePath("roughness"), false, false, false);
                ConfigureTexture(definition.TexturePath("metallic"), false, false, false);
                ConfigureTexture(definition.TexturePath("normal"), false, true, false);
            }

            ConfigureTexture(
                TextureFolder + "/pahur_face_reference_overlay.png",
                true,
                false,
                true);
            ConfigureTexture(
                TextureFolder + "/pahur_face_reference_emission.png",
                true,
                false,
                true);
            ConfigureTexture(
                TextureFolder + "/pahur_head_reference_projection_decal.png",
                true,
                false,
                true);
        }

        private static void ConfigureTexture(
            string path,
            bool srgb,
            bool normalMap,
            bool clamp)
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException(
                    "Approved Pahur texture is missing: " + path);
            importer.textureType =
                normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb && !normalMap;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static Dictionary<string, Material> CreateOrUpdateMaterials()
        {
            EnsureAssetFolder(AppearanceRoot);
            EnsureAssetFolder(TextureFolder);
            EnsureAssetFolder(MaterialFolder);
            AssetDatabase.ImportAsset(
                ShaderPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ??
                throw new InvalidOperationException(
                    "Approved Pahur shader is missing: " + ShaderPath);
            if (!shader.isSupported)
            {
                throw new InvalidOperationException(
                    "Approved Pahur shader is not supported by the active renderer.");
            }

            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var definition in MaterialDefinitions)
            {
                var path = definition.MaterialPath;
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader)
                    {
                        name = Path.GetFileNameWithoutExtension(path)
                    };
                    AssetDatabase.CreateAsset(material, path);
                }
                else
                {
                    material.shader = shader;
                    material.name = Path.GetFileNameWithoutExtension(path);
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
            var black = Texture2D.blackTexture;
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_TextureScale", definition.TextureScale);
            material.SetFloat("_NormalStrength", definition.NormalStrength);
            material.SetFloat("_FeatureMode", (float)definition.Feature);
            material.SetFloat("_MachineScale", definition.MachineScale);
            material.SetFloat("_MachineThreshold", definition.MachineThreshold);
            material.SetFloat("_MachineStrength", definition.MachineStrength);
            material.SetColor("_MachineColor", definition.MachineColor);
            material.SetFloat("_ApprovedAmbientStrength", 0.88f);
            material.SetFloat("_ApprovedKeyStrength", 3.00f);
            material.SetFloat("_ApprovedFillStrength", 1.35f);
            material.SetFloat("_ApprovedRimStrength", 2.10f);
            material.SetFloat("_PreviewUnlit", 0f);
            material.SetVector(
                "_GeneratedBoundsMin",
                new Vector4(-64.637962f, 0f, -54.07045f, 0f));
            material.SetVector(
                "_GeneratedBoundsInvSize",
                new Vector4(
                    1f / 129.275924f,
                    1f / 180f,
                    1f / 108.1409f,
                    0f));
            material.SetTexture(
                "_FaceOverlay",
                LoadTexture("pahur_face_reference_overlay.png"));
            material.SetTexture(
                "_FaceEmission",
                LoadTexture("pahur_face_reference_emission.png"));
            material.SetTexture(
                "_HoodDecal",
                LoadTexture("pahur_head_reference_projection_decal.png"));

            if (definition.Feature == Feature.Emission)
            {
                var emission = LoadTexture(
                    "pahur_" + definition.Id + "_emission.png");
                material.SetTexture("_BaseMap", emission);
                material.SetTexture("_EmissionMap", emission);
                material.SetTexture("_RoughnessMap", Texture2D.grayTexture);
                material.SetTexture("_MetallicMap", Texture2D.blackTexture);
                material.SetTexture("_NormalMap", Texture2D.normalTexture);
                material.SetFloat("_EmissionStrength", definition.EmissionStrength);
            }
            else
            {
                material.SetTexture(
                    "_BaseMap",
                    LoadTexture("pahur_" + definition.Id + "_albedo.png"));
                material.SetTexture(
                    "_RoughnessMap",
                    LoadTexture("pahur_" + definition.Id + "_roughness.png"));
                material.SetTexture(
                    "_MetallicMap",
                    LoadTexture("pahur_" + definition.Id + "_metallic.png"));
                material.SetTexture(
                    "_NormalMap",
                    LoadTexture("pahur_" + definition.Id + "_normal.png"));
                material.SetTexture("_EmissionMap", black);
                material.SetFloat("_EmissionStrength", 0f);
            }

            material.enableInstancing = true;
            material.renderQueue = -1;
        }

        private static void RequireSceneParityMaterial(
            Material material,
            Shader expectedShader)
        {
            if (material == null || material.shader != expectedShader)
            {
                throw new InvalidOperationException(
                    "A Pahur approved material does not use the approved shader.");
            }

            RequireFloat(material, "_PreviewUnlit", 0f);
            RequireFloat(material, "_ApprovedAmbientStrength", 0.88f);
            RequireFloat(material, "_ApprovedKeyStrength", 3.00f);
            RequireFloat(material, "_ApprovedFillStrength", 1.35f);
            RequireFloat(material, "_ApprovedRimStrength", 2.10f);
        }

        private static void RequireFloat(
            Material material,
            string property,
            float expected)
        {
            if (!material.HasProperty(property) ||
                Mathf.Abs(material.GetFloat(property) - expected) > 0.0001f)
            {
                throw new InvalidOperationException(
                    material.name + " has an unexpected " + property + " value.");
            }
        }

        private static Texture2D LoadTexture(string fileName)
        {
            var path = TextureFolder + "/" + fileName;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                throw new InvalidOperationException(
                    "Approved Pahur texture failed to load: " + path);
        }

        private static Material[] ResolveApprovedMaterialOrder(
            IReadOnlyDictionary<string, Material> approvedMaterials)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException(
                    "Approved Pahur FBX failed to load.");
            var renderer =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Approved Pahur FBX must contain exactly one skinned renderer.");
            RequireApprovedGeometry(renderer);

            var sourceMaterials = renderer.sharedMaterials;
            if (sourceMaterials.Length != renderer.sharedMesh.subMeshCount)
            {
                throw new InvalidOperationException(
                    "Approved Pahur FBX material slots do not match its submeshes.");
            }

            var ordered = new Material[sourceMaterials.Length];
            for (var index = 0; index < sourceMaterials.Length; index++)
            {
                var source = sourceMaterials[index] ??
                    throw new InvalidOperationException(
                        "Approved Pahur FBX contains a null material slot at index " +
                        index + ".");
                if (!approvedMaterials.TryGetValue(source.name, out var material))
                {
                    throw new InvalidOperationException(
                        "Approved Pahur FBX contains an unrecognized material name: " +
                        source.name);
                }

                ordered[index] = material;
            }

            return ordered;
        }

        private static Dictionary<string, Material> LoadApprovedMaterials()
        {
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var definition in MaterialDefinitions)
            {
                var material =
                    AssetDatabase.LoadAssetAtPath<Material>(
                        definition.MaterialPath) ??
                    throw new InvalidOperationException(
                        "Approved Pahur material is missing: " +
                        definition.MaterialPath);
                result.Add(definition.DisplayName, material);
            }

            return result;
        }

        private static AppearanceResult InspectAppliedState(
            Scene scene,
            GameObject root,
            IReadOnlyDictionary<string, Material> approvedMaterials)
        {
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes during approved Pahur appearance inspection.");
            }

            RequireExactModelHash();
            var expectedOrder = ResolveApprovedMaterialOrder(approvedMaterials);
            var renderers = RequireSceneRenderers(root);
            foreach (var renderer in renderers)
            {
                RequireApprovedGeometry(renderer);
                if (!renderer.sharedMaterials.SequenceEqual(expectedOrder))
                {
                    throw new InvalidOperationException(
                        "A Pahur renderer does not use the exact approved material order: " +
                        renderer.name);
                }
            }

            var mesh = renderers[0].sharedMesh;
            return new AppearanceResult(
                renderers.Length,
                expectedOrder.Length,
                TriangleCount(mesh),
                renderers[0].bones.Length,
                Sha256(ProjectAbsolutePath(ModelPath)));
        }

        private static void RequireApprovedGeometry(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Approved Pahur renderer has no shared mesh.");
            var triangleCount = TriangleCount(mesh);
            if (triangleCount != ExpectedTriangles)
            {
                throw new InvalidOperationException(
                    "Approved Pahur triangle count differs from the approved sample. Expected=" +
                    ExpectedTriangles + ", Actual=" + triangleCount);
            }

            if (renderer.bones.Length != ExpectedBones)
            {
                throw new InvalidOperationException(
                    "Approved Pahur bone count differs from the approved sample. Expected=" +
                    ExpectedBones + ", Actual=" + renderer.bones.Length);
            }

            var approvedSamplePositions = new List<Vector3>();
            mesh.GetUVs(3, approvedSamplePositions);
            if (approvedSamplePositions.Count != mesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "Approved Pahur bind-pose projection coordinates are missing. Expected=" +
                    mesh.vertexCount +
                    ", Actual=" +
                    approvedSamplePositions.Count);
            }
        }

        private static int TriangleCount(Mesh mesh)
        {
            long total = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
            {
                total += (long)mesh.GetIndexCount(index) / 3;
            }

            if (total > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "Approved Pahur triangle count exceeds the supported range.");
            }

            return (int)total;
        }

        private static SkinnedMeshRenderer[] RequireSceneRenderers(GameObject root)
        {
            var renderers = new List<SkinnedMeshRenderer>(ExpectedSlots);
            if (root.transform.childCount != ExpectedSlots)
            {
                throw new InvalidOperationException(
                    "Pahur placement root must contain exactly " +
                    ExpectedSlots + " slots.");
            }

            for (var index = 0; index < ExpectedSlots; index++)
            {
                var slot = root.transform.GetChild(index);
                if (slot.name != SlotNames[index])
                {
                    throw new InvalidOperationException(
                        "Pahur slot contract differs at index " + index + ".");
                }

                if (slot.childCount != 1 ||
                    slot.GetChild(0).name != ModelName)
                {
                    throw new InvalidOperationException(
                        "Pahur model child contract differs for " + slot.name + ".");
                }

                var renderer =
                    slot.GetChild(0)
                        .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .SingleOrDefault() ??
                    throw new InvalidOperationException(
                        "Pahur slot must contain exactly one skinned renderer: " +
                        slot.name);
                renderers.Add(renderer);
            }

            return renderers.ToArray();
        }

        private static SceneContract CaptureContract(Scene scene, GameObject root)
        {
            return new SceneContract(
                RecursiveSignature(root.transform, includeMaterials: false),
                scene.GetRootGameObjects()
                    .Where(candidate => candidate != root)
                    .OrderBy(candidate => candidate.name, StringComparer.Ordinal)
                    .Select(candidate =>
                        RecursiveSignature(
                            candidate.transform,
                            includeMaterials: true))
                    .ToArray());
        }

        private static string RecursiveSignature(
            Transform root,
            bool includeMaterials)
        {
            var builder = new StringBuilder();
            foreach (var current in
                     root.GetComponentsInChildren<Transform>(true))
            {
                builder
                    .Append(GetPath(current, root))
                    .Append('|')
                    .Append(current.gameObject.activeSelf)
                    .Append('|')
                    .Append(Vec(current.localPosition))
                    .Append('|')
                    .Append(Quat(current.localRotation))
                    .Append('|')
                    .Append(Vec(current.localScale))
                    .Append('|');
                foreach (var component in
                         current.GetComponents<Component>()
                             .Where(component => component != null))
                {
                    builder.Append(component.GetType().FullName).Append(',');
                    if (includeMaterials && component is Renderer renderer)
                    {
                        builder.Append('[');
                        foreach (var material in renderer.sharedMaterials)
                        {
                            builder
                                .Append(
                                    material == null
                                        ? "null"
                                        : AssetDatabase.GetAssetPath(material))
                                .Append(';');
                        }
                        builder.Append(']');
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
            if (before.PahurStructure != after.PahurStructure)
            {
                throw new InvalidOperationException(
                    "A Pahur transform, active state, or gameplay component changed during appearance application.");
            }

            if (!before.OtherRoots.SequenceEqual(
                    after.OtherRoots,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside the approved Pahur placement changed during appearance application.");
            }
        }

        private static Scene RequireActiveScene(bool requireClean)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Approved Pahur appearance commands require Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. ActiveScene=" +
                    scene.path);
            }

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the approved Pahur appearance.");
            }

            return scene;
        }

        private static GameObject RequirePlacementRoot(Scene scene)
        {
            var root =
                scene.GetRootGameObjects()
                    .SingleOrDefault(candidate =>
                        candidate.name == PlacementRootName) ??
                throw new InvalidOperationException(
                    "Approved Pahur placement root is missing.");
            RequireSceneRenderers(root);
            return root;
        }

        private static void RequireExactModelHash()
        {
            var hash = Sha256(ProjectAbsolutePath(ModelPath));
            if (!string.Equals(
                    hash,
                    ExpectedFbxSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Unity Pahur FBX does not match the approved sample. Expected=" +
                    ExpectedFbxSha256 + ", Actual=" + hash);
            }
        }

        private static void CapturePreview(
            GameObject source,
            string outputPath,
            float angle)
        {
            var preview = new PreviewRenderUtility();
            GameObject clone = null;
            Texture2D image = null;
            try
            {
                clone = UnityEngine.Object.Instantiate(source);
                clone.name = "Pahur_ApprovedAppearance_Preview";
                clone.hideFlags = HideFlags.HideAndDontSave;
                clone.transform.SetPositionAndRotation(
                    Vector3.zero,
                    source.transform.rotation);
                clone.transform.localScale = source.transform.lossyScale;
                foreach (var animator in
                         clone.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                }

                preview.AddSingleGO(clone);
                var renderers = clone.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                    renderer.forceRenderingOff = false;
                    var properties = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(properties);
                    properties.SetFloat("_PreviewUnlit", 1f);
                    renderer.SetPropertyBlock(properties);
                }

                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1))
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                var skinnedRenderer =
                    renderers.OfType<SkinnedMeshRenderer>().Single();
                var meshBounds = skinnedRenderer.sharedMesh.bounds;
                var faceSubmeshIndex =
                    Array.FindIndex(
                        skinnedRenderer.sharedMaterials,
                        material =>
                            material != null &&
                            AssetDatabase.GetAssetPath(material)
                                .IndexOf(
                                    "face_metal",
                                    StringComparison.OrdinalIgnoreCase) >= 0);
                var faceBounds =
                    faceSubmeshIndex >= 0
                        ? SubmeshBounds(
                            skinnedRenderer.sharedMesh,
                            faceSubmeshIndex)
                        : new Bounds();

                var camera = preview.camera;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor =
                    new Color(0.86f, 0.89f, 0.90f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 31.2f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.allowHDR = true;

                var forward =
                    Quaternion.AngleAxis(angle, Vector3.up) *
                    clone.transform.forward;
                var distance =
                    Mathf.Max(bounds.size.x, bounds.size.y) /
                    (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) *
                    0.84f;
                var target =
                    bounds.center + Vector3.up * bounds.size.y * 0.02f;
                camera.transform.position =
                    target + forward.normalized * distance +
                    Vector3.up * bounds.size.y * 0.06f;
                camera.transform.LookAt(target, Vector3.up);
                Debug.Log(
                    "PahurApprovedAppearanceCaptureSetup" +
                    ", Angle=" + angle.ToString("R", CultureInfo.InvariantCulture) +
                    ", CloneActive=" + clone.activeInHierarchy +
                    ", Renderers=" + renderers.Length +
                    ", RendererEnabled=" + renderers[0].enabled +
                    ", BoundsCenter=" + Vec(bounds.center) +
                    ", BoundsSize=" + Vec(bounds.size) +
                    ", MeshBoundsCenter=" + Vec(meshBounds.center) +
                    ", MeshBoundsSize=" + Vec(meshBounds.size) +
                    ", RendererLocalPosition=" +
                    Vec(skinnedRenderer.transform.localPosition) +
                    ", RendererLocalRotation=" +
                    Quat(skinnedRenderer.transform.localRotation) +
                    ", RendererLocalScale=" +
                    Vec(skinnedRenderer.transform.localScale) +
                    ", FaceSubmesh=" + faceSubmeshIndex +
                    ", FaceBoundsMin=" + Vec(faceBounds.min) +
                    ", FaceBoundsMax=" + Vec(faceBounds.max) +
                    ", Forward=" + Vec(forward) +
                    ", Distance=" +
                    distance.ToString("R", CultureInfo.InvariantCulture) +
                    ", CameraPosition=" + Vec(camera.transform.position) +
                    ", Target=" + Vec(target));

                preview.lights[0].transform.rotation =
                    camera.transform.rotation;
                preview.lights[0].color =
                    new Color(1f, 0.90f, 0.78f);
                preview.lights[0].intensity = 1.65f;
                preview.lights[0].shadows = LightShadows.Soft;
                preview.lights[1].transform.rotation =
                    Quaternion.LookRotation(
                        (camera.transform.forward +
                         camera.transform.right * 0.65f).normalized,
                        Vector3.up);
                preview.lights[1].color =
                    new Color(0.58f, 0.76f, 1f);
                preview.lights[1].intensity = 0.85f;
                preview.lights[1].shadows = LightShadows.None;
                preview.ambientColor =
                    new Color(0.72f, 0.74f, 0.76f, 1f);
                preview.BeginStaticPreview(
                    new Rect(0f, 0f, 1280f, 1280f));
                preview.Render(true);
                image = preview.EndStaticPreview();
                if (image == null)
                {
                    throw new InvalidOperationException(
                        "Unity PreviewRenderUtility returned no Pahur image.");
                }

                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }

                preview.Cleanup();
            }
        }

        private static void CaptureActualSceneModel(
            GameObject source,
            string outputPath)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(outputPath) ??
                throw new InvalidOperationException(
                    "Pahur scene-parity capture folder is invalid."));
            var sceneRenderers = source.scene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(renderer =>
                    new RendererEnabledState(renderer, renderer.enabled))
                .ToArray();
            GameObject clone = null;
            GameObject cameraObject = null;
            RenderTexture target = null;
            Texture2D image = null;
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var state in sceneRenderers)
                {
                    state.Renderer.enabled = false;
                }

                clone = UnityEngine.Object.Instantiate(source);
                clone.name = "Pahur_ActualScene_Parity_Capture";
                clone.hideFlags = HideFlags.HideAndDontSave;
                foreach (var child in clone.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                }
                var renderers = clone.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        "The Pahur scene-parity clone contains no renderers.");
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

                cameraObject = new GameObject(
                    "PahurActualSceneParityCamera",
                    typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.86f, 0.89f, 0.90f, 1f);
                camera.fieldOfView = 31.2f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.allowHDR = true;
                camera.allowMSAA = true;

                var targetPoint = bounds.center + Vector3.up * bounds.size.y * 0.02f;
                var forward = clone.transform.forward.normalized;
                var distance =
                    Mathf.Max(bounds.size.x, bounds.size.y) /
                    (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) *
                    1.12f;
                camera.transform.position =
                    targetPoint + forward * distance +
                    Vector3.up * bounds.size.y * 0.06f;
                camera.transform.LookAt(targetPoint, Vector3.up);

                target = new RenderTexture(
                    1280,
                    1280,
                    24,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
                image = new Texture2D(
                    1280,
                    1280,
                    TextureFormat.RGB24,
                    false);
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image.ReadPixels(new Rect(0f, 0f, 1280f, 1280f), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                foreach (var state in sceneRenderers)
                {
                    if (state.Renderer != null)
                    {
                        state.Renderer.enabled = state.Enabled;
                    }
                }
                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
                if (target != null)
                {
                    if (cameraObject != null)
                    {
                        cameraObject.GetComponent<Camera>().targetTexture = null;
                    }
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }
                if (clone != null)
                {
                    UnityEngine.Object.DestroyImmediate(clone);
                }
            }
        }

        private static Bounds SubmeshBounds(
            Mesh mesh,
            int submeshIndex)
        {
            var vertices = mesh.vertices;
            var indices = mesh.GetIndices(submeshIndex);
            if (indices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Approved Pahur submesh is empty: " + submeshIndex);
            }

            var bounds = new Bounds(vertices[indices[0]], Vector3.zero);
            foreach (var index in indices.Skip(1))
            {
                bounds.Encapsulate(vertices[index]);
            }

            return bounds;
        }

        private static void WriteReport(
            AppearanceResult result,
            string resultName)
        {
            var absolute = ProjectAbsolutePath(ReportRelativePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Approved Pahur report folder is invalid."));
            File.WriteAllLines(
                absolute,
                new[]
                {
                    "Result=" + resultName,
                    "Revision=" + Revision,
                    "Scene=" + ScenePath,
                    "PlacementRoot=" + PlacementRootName,
                    "Slots=" + result.SlotCount,
                    "MaterialsPerRenderer=" + result.MaterialCount,
                    "Triangles=" + result.TriangleCount,
                    "ApprovedMeshShape=Vertices:2434,Edges:6548,Polygons:4330,Loops:12990",
                    "Bones=" + result.BoneCount,
                    "FbxSha256=" + result.FbxSha256,
                    "EyeProjection=Width:5.0,Height:4.5,LeftRotation:-16,RightRotation:-14",
                    "SceneTransformsChanged=False",
                    "GameplayComponentsChanged=False",
                    "OtherSceneRootsChanged=False"
                },
                new UTF8Encoding(false));
        }

        private static void EnsureAssetFolder(string path)
        {
            var normalized = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var parent = normalized.Substring(0, normalized.LastIndexOf('/'));
            var name = normalized.Substring(normalized.LastIndexOf('/') + 1);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
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
            Face = 1,
            Hood = 2,
            Machine = 3,
            Emission = 4
        }

        private readonly struct MaterialDefinition
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly float NormalStrength;
            public readonly float TextureScale;
            public readonly Feature Feature;
            public readonly float MachineScale;
            public readonly float MachineThreshold;
            public readonly float MachineStrength;
            public readonly Color MachineColor;
            public readonly float EmissionStrength;

            public MaterialDefinition(
                string id,
                string displayName,
                float normalStrength,
                float textureScale,
                Feature feature,
                float machineScale = 0f,
                float machineThreshold = 0f,
                float machineStrength = 0f,
                Color machineColor = default,
                float emissionStrength = 0f)
            {
                Id = id;
                DisplayName = displayName;
                NormalStrength = normalStrength;
                TextureScale = textureScale;
                Feature = feature;
                MachineScale = machineScale;
                MachineThreshold = machineThreshold;
                MachineStrength = machineStrength;
                MachineColor = machineColor;
                EmissionStrength = emissionStrength;
            }

            public string MaterialPath =>
                MaterialFolder + "/Pahur_" + Id + "_Approved.mat";

            public string TexturePath(string suffix)
            {
                return TextureFolder + "/pahur_" + Id + "_" + suffix + ".png";
            }
        }

        private readonly struct SceneContract
        {
            public readonly string PahurStructure;
            public readonly string[] OtherRoots;

            public SceneContract(
                string pahurStructure,
                string[] otherRoots)
            {
                PahurStructure = pahurStructure;
                OtherRoots = otherRoots;
            }
        }

        private sealed class RendererEnabledState
        {
            public RendererEnabledState(Renderer renderer, bool enabled)
            {
                Renderer = renderer;
                Enabled = enabled;
            }

            public Renderer Renderer { get; }
            public bool Enabled { get; }
        }

        private readonly struct AppearanceResult
        {
            public readonly int SlotCount;
            public readonly int MaterialCount;
            public readonly int TriangleCount;
            public readonly int BoneCount;
            public readonly string FbxSha256;

            public AppearanceResult(
                int slotCount,
                int materialCount,
                int triangleCount,
                int boneCount,
                string fbxSha256)
            {
                SlotCount = slotCount;
                MaterialCount = materialCount;
                TriangleCount = triangleCount;
                BoneCount = boneCount;
                FbxSha256 = fbxSha256;
            }
        }
    }
}
