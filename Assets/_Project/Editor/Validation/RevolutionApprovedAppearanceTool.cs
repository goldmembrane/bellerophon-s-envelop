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

namespace Bellerophon.Editor.RevolutionCargoRunScene
{
    internal static class RevolutionApprovedAppearanceTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Revolution Enemy Placement";
        private const string ApprovedRoot =
            "Assets/_Project/Art/Enemies/Revolution/ApprovedAppearance";
        private const string ApprovedModelPath =
            ApprovedRoot + "/Models/Revolution_ApprovedAppearance.fbx";
        private const string TextureFolder =
            ApprovedRoot + "/Textures";
        private const string MaterialFolder =
            ApprovedRoot + "/Materials";
        private const string ShaderPath =
            ApprovedRoot + "/Shaders/RevolutionApprovedAppearance.shader";
        private const string ShaderName =
            "Bellerophon/Revolution/ApprovedAppearance";
        private const string ApprovedSampleRoot =
            "artSample/enemies/revolution";
        private const string ApprovedSampleModelPath =
            ApprovedSampleRoot +
            "/exports/revolution_replaced_model_reference_sample.fbx";
        private const string ReportPath =
            "docs/validation/revolution_approved_appearance_2026-07-27/" +
            "Revolution_ApprovedAppearance_Inspection.txt";
        private const string CapturePath =
            "docs/validation/revolution_approved_appearance_2026-07-27/" +
            "Revolution_ApprovedAppearance_VisualReview.png";
        private const string ApprovedModelSha256 =
            "9F27490A8C786F409F02F8D8AA1D22BECD3E9384EC3AE6B938EB886665F75252";
        private const int AuthoredVertexCount = 2307;
        private const int TriangleCount = 3945;
        private const int LoopCount = 11835;
        private const int BoneCount = 24;
        private const int BlenderMaterialSlotCount = 9;
        private const int ActiveMaterialSlotCount = 8;
        private const int TorsoPolygonCount = 667;
        private const int ShoulderConnectionPolygonCount = 407;
        private const int MirroredArmChangedPolygonCount = 281;

        private static readonly string[] SlotNames =
        {
            "Revolution_01",
            "Revolution_02",
            "Revolution_03",
            "Revolution_04",
            "Revolution_05",
            "Revolution_06",
            "Revolution_07",
            "Revolution_08"
        };

        private static readonly int[] ApprovedSubMeshTriangles =
        {
            689,
            308,
            1283,
            663,
            96,
            744,
            48,
            114
        };

        private static readonly ApprovedMaterialSpec[] ApprovedMaterials =
        {
            new ApprovedMaterialSpec(
                "Reference_BodyPanel_DirectCrop",
                "reference_body_panel_direct_crop.png",
                "reference_body_wear_direct_crop.png",
                0.52f,
                0.56f,
                true,
                0f,
                Color.white),
            new ApprovedMaterialSpec(
                "Reference_BodyLightSteel_DirectCrop",
                "reference_body_light_steel_direct_crop.png",
                null,
                0.35f,
                0.60f,
                false,
                0f,
                Color.white),
            new ApprovedMaterialSpec(
                "Reference_WeaponHousing_DirectCrop",
                "reference_weapon_housing_direct_crop.png",
                null,
                0.58f,
                0.58f,
                false,
                0f,
                Color.white),
            new ApprovedMaterialSpec(
                "Reference_LegArmor_DirectCrop",
                "reference_leg_armor_direct_crop.png",
                null,
                0.40f,
                0.62f,
                false,
                0f,
                Color.white),
            new ApprovedMaterialSpec(
                "Reference_CopperMechanics_DirectCrop",
                "reference_copper_mechanics_direct_crop.png",
                null,
                0.55f,
                0.48f,
                false,
                0f,
                Color.white),
            new ApprovedMaterialSpec(
                "Reference_DarkMechanics_DirectCrop",
                "reference_dark_mechanics_direct_crop.png",
                null,
                0.46f,
                0.62f,
                false,
                0f,
                Color.white),
            new ApprovedMaterialSpec(
                "Reference_BlueOptic_DirectCrop",
                "reference_blue_optic_direct_crop.png",
                null,
                0.25f,
                0.22f,
                false,
                2.4f,
                Color.white),
            new ApprovedMaterialSpec(
                "Reference_TorsoInsetGunmetal_DirectCrop",
                "reference_body_wear_direct_crop.png",
                null,
                0.44f,
                0.56f,
                false,
                0f,
                Color.white)
        };

        private static readonly IReadOnlyDictionary<string, string>
            ApprovedTextureHashes =
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    {
                        "reference_blue_optic_direct_crop.png",
                        "26721B993574252E3F041975619D999B4183BC47231C73F7612F1CAE270545E3"
                    },
                    {
                        "reference_body_light_steel_direct_crop.png",
                        "C183FFCCF721DA4ECD862C94831FE9C1EEA11BF3B98EBCB2E285FDA5B9259E13"
                    },
                    {
                        "reference_body_panel_direct_crop.png",
                        "4C37ED7808A57C5AC546B241C3498FAFB9E8298EF6B91D6574B46EF4ED64E0C1"
                    },
                    {
                        "reference_body_wear_direct_crop.png",
                        "0893766B8760AD8CB8ED8ECC39AD32014E22B81F06D66C5E6E5EE83E065FECE4"
                    },
                    {
                        "reference_copper_mechanics_direct_crop.png",
                        "4DDC3A6E2C8DCDD5FF4F601A2E7C1ABE504EC4DFA97A16EE5CAD3109BE9E5266"
                    },
                    {
                        "reference_dark_mechanics_direct_crop.png",
                        "D8E8DF40D946AFFDBED3D34FAD917A627AF6E9BCDBA6E5940635F6D9C9A6C303"
                    },
                    {
                        "reference_leg_armor_direct_crop.png",
                        "49AC0CFCD61ACAC6E24209D82D739BAB7DC129298379388A801BD9C7CADCFCED"
                    },
                    {
                        "reference_weapon_housing_direct_crop.png",
                        "0692F4F7066993D3BCA085A62481D42CFFC113C94FF2C18E6F86D6D5167CF2BF"
                    }
                };

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Apply Approved Appearance")]
        public static void ApplyRevolutionApprovedAppearance()
        {
            RequireApprovedInputs();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the approved Revolution appearance.");
            }

            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var protectedBefore = ProtectedRootSignatures(scene);
            var hierarchyBefore = HierarchyTransformSignatures(root.transform);
            var sourceHashBefore = Sha256(
                Absolute(ApprovedSampleModelPath));
            var modelHashBefore = Sha256(
                Absolute(ApprovedModelPath));

            var approvedAsset = PrepareApprovedModel();
            var approvedRenderer = RequireSingleRenderer(
                approvedAsset.transform,
                "approved Revolution FBX");
            var approvedMesh = RequireApprovedGeometry(
                approvedRenderer,
                "approved Revolution FBX");
            var materials = PrepareApprovedMaterials(
                approvedMesh.bounds,
                approvedRenderer);
            var oldStates = new List<RendererState>();

            try
            {
                for (var index = 0; index < SlotNames.Length; index++)
                {
                    var model = root.transform.GetChild(index).GetChild(0);
                    var renderer = RequireSingleRenderer(
                        model,
                        SlotNames[index]);
                    RequireCompatibleBones(
                        renderer,
                        approvedRenderer,
                        SlotNames[index]);
                    oldStates.Add(new RendererState(renderer));
                    renderer.sharedMesh = approvedMesh;
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }

                if (!hierarchyBefore.SequenceEqual(
                        HierarchyTransformSignatures(root.transform),
                        StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A Revolution root, slot, model, or bone transform changed while applying the approved appearance.");
                }

                if (!protectedBefore.SequenceEqual(
                        ProtectedRootSignatures(scene),
                        StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A scene root outside the Revolution placement changed while applying the approved appearance.");
                }

                var inspection = InspectAppliedAppearance(root.transform);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after applying the approved Revolution appearance.");
                }

                AssetDatabase.SaveAssets();
                RequireSameHash(
                    sourceHashBefore,
                    Sha256(Absolute(ApprovedSampleModelPath)),
                    "approved sample FBX");
                RequireSameHash(
                    modelHashBefore,
                    Sha256(Absolute(ApprovedModelPath)),
                    "Unity approved FBX");
                Debug.Log(
                    "RevolutionApprovedAppearanceApplied Result=PASS" +
                    ", Slots=" + inspection.ModelCount +
                    ", AuthoredVertices=" + AuthoredVertexCount +
                    ", ImportedRenderVertices=" +
                    inspection.ImportedVertexCount +
                    ", Triangles=" + inspection.TriangleCount +
                    ", Loops=" + LoopCount +
                    ", Bones=" + inspection.BoneCount +
                    ", Materials=" + inspection.MaterialCount +
                    ", TorsoPolygons=" + TorsoPolygonCount +
                    ", ShoulderConnectionPolygons=" +
                    ShoulderConnectionPolygonCount +
                    ", RightArmSymmetryCopiedPolygons=" +
                    MirroredArmChangedPolygonCount +
                    ", MeshAndApprovedFaceAssignmentsPreserved=True" +
                    ", RootSlotModelBoneTransformsUnchanged=True" +
                    ", PlayerCameraAndOtherRootsUnchanged=True" +
                    ", SceneSaved=True.");
            }
            catch
            {
                foreach (var state in oldStates)
                {
                    state.Restore();
                }

                if (scene.isDirty)
                {
                    EditorSceneManager.SaveScene(scene);
                }

                throw;
            }
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Inspect Approved Appearance")]
        public static void InspectRevolutionApprovedAppearance()
        {
            RequireApprovedInputs();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var inspection = InspectAppliedAppearance(root.transform);
            RequireNoMagentaShaderFallback();
            WriteInspectionReport(inspection);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Revolution appearance inspection changed the scene dirty state.");
            }

            Debug.Log(
                "RevolutionApprovedAppearanceInspected Result=PASS" +
                ", Slots=" + inspection.ModelCount +
                ", AuthoredVertices=" + AuthoredVertexCount +
                ", ImportedRenderVertices=" +
                inspection.ImportedVertexCount +
                ", Triangles=" + inspection.TriangleCount +
                ", Loops=" + LoopCount +
                ", Bones=" + inspection.BoneCount +
                ", Materials=" + inspection.MaterialCount +
                ", SubMeshTriangles=" +
                string.Join(
                    ",",
                    inspection.ImportedSubMeshTriangles) +
                ", TorsoPolygons=" + TorsoPolygonCount +
                ", ShoulderConnectionPolygons=" +
                ShoulderConnectionPolygonCount +
                ", RightArmSymmetryCopiedPolygons=" +
                MirroredArmChangedPolygonCount +
                ", SceneChanged=False.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Capture Approved Appearance Review")]
        public static void CaptureRevolutionApprovedAppearanceReview()
        {
            RequireApprovedInputs();
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            InspectAppliedAppearance(root.transform);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time approved Revolution appearance capture already exists: " +
                    CapturePath);
            }

            var player = GameObject.Find("Player") ??
                         throw new InvalidOperationException(
                             "Player is missing.");
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException(
                             "The Player camera is missing.");
            Capture(camera, destination, 1920, 1080);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Approved Revolution appearance capture changed the scene dirty state.");
            }

            Debug.Log(
                "RevolutionApprovedAppearanceCaptured Result=PASS" +
                ", Slots=8" +
                ", Image=" + CapturePath +
                ", ExistingPlayerCameraUsed=True" +
                ", SceneChanged=False.");
        }

        private static GameObject PrepareApprovedModel()
        {
            AssetDatabase.ImportAsset(
                ApprovedModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer =
                AssetImporter.GetAtPath(ApprovedModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "The approved Revolution ModelImporter is missing.");
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = true;
            importer.importVisibility = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents =
                ModelImporterTangents.CalculateMikk;
            importer.optimizeGameObjects = false;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation =
                ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                       ApprovedModelPath) ??
                   throw new InvalidOperationException(
                       "Unity did not import the approved Revolution FBX.");
        }

        private static Material[] PrepareApprovedMaterials(
            Bounds meshBounds,
            SkinnedMeshRenderer approvedRenderer)
        {
            foreach (var pair in ApprovedTextureHashes)
            {
                ConfigureTexture(TextureFolder + "/" + pair.Key);
            }

            AssetDatabase.ImportAsset(
                ShaderPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                             ShaderPath) ??
                         throw new InvalidOperationException(
                             "The approved Revolution appearance shader is missing.");
            RequireSupportedShader(shader);

            var materials = new Material[ApprovedMaterials.Length];
            for (var index = 0; index < ApprovedMaterials.Length; index++)
            {
                var spec = ApprovedMaterials[index];
                var path = spec.MaterialPath;
                var material =
                    AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader)
                    {
                        name = spec.Name
                    };
                    AssetDatabase.CreateAsset(material, path);
                }
                else
                {
                    material.shader = shader;
                    material.name = spec.Name;
                }

                material.SetTexture(
                    "_BaseMap",
                    spec.BaseTexturePath == null
                        ? null
                        : RequireTexture(spec.BaseTexturePath));
                material.SetTexture(
                    "_DetailMap",
                    spec.DetailTexturePath == null
                        ? null
                        : RequireTexture(spec.DetailTexturePath));
                material.SetColor("_SolidColor", spec.SolidColor);
                material.SetFloat(
                    "_UseTexture",
                    spec.BaseTexturePath == null ? 0f : 1f);
                material.SetFloat(
                    "_UseDetail",
                    spec.UseDetail ? 1f : 0f);
                material.SetFloat("_DetailMix", 0.35f);
                material.SetFloat("_Metallic", spec.Metallic);
                material.SetFloat("_Roughness", spec.Roughness);
                material.SetFloat("_BumpStrength", 0.10f);
                material.SetFloat("_BumpDistance", 0.025f);
                material.SetFloat(
                    "_EmissionStrength",
                    spec.EmissionStrength);
                material.SetVector(
                    "_BoundsMin",
                    new Vector4(
                        meshBounds.min.x,
                        meshBounds.min.y,
                        meshBounds.min.z,
                        0f));
                material.SetVector(
                    "_BoundsSize",
                    new Vector4(
                        meshBounds.size.x,
                        meshBounds.size.y,
                        meshBounds.size.z,
                        0f));
                material.enableInstancing = true;
                material.renderQueue = -1;
                EditorUtility.SetDirty(material);
                materials[index] = material;
            }

            AssetDatabase.SaveAssets();
            return MaterialsInImportedSubMeshOrder(
                approvedRenderer,
                materials);
        }

        private static void ConfigureTexture(string path)
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var importer =
                AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException(
                    "The approved direct-crop texture importer is missing: " +
                    path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource =
                TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Inspection InspectAppliedAppearance(
            Transform root)
        {
            var approvedAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ApprovedModelPath) ??
                throw new InvalidOperationException(
                    "The approved Revolution FBX asset is missing.");
            var approvedRenderer = RequireSingleRenderer(
                approvedAsset.transform,
                "approved Revolution FBX");
            var approvedMesh = RequireApprovedGeometry(
                approvedRenderer,
                "approved Revolution FBX");
            var materialAssets = ApprovedMaterials
                .Select(spec =>
                    AssetDatabase.LoadAssetAtPath<Material>(
                        spec.MaterialPath) ??
                    throw new InvalidOperationException(
                        "Approved Revolution material is missing: " +
                        spec.MaterialPath))
                .ToArray();
            var expectedMaterials =
                MaterialsInImportedSubMeshOrder(
                    approvedRenderer,
                    materialAssets);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                             ShaderPath) ??
                         throw new InvalidOperationException(
                             "Approved Revolution shader is missing.");
            RequireSupportedShader(shader);

            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = root.GetChild(index);
                var renderer = RequireSingleRenderer(
                    slot.GetChild(0),
                    SlotNames[index]);
                if (renderer.sharedMesh != approvedMesh)
                {
                    throw new InvalidOperationException(
                        SlotNames[index] +
                        " does not use the approved sample mesh and face assignments.");
                }

                if (!renderer.sharedMaterials.SequenceEqual(
                        expectedMaterials))
                {
                    throw new InvalidOperationException(
                        SlotNames[index] +
                        " does not use the approved materials in sample slot order.");
                }

                RequireCompatibleBones(
                    renderer,
                    approvedRenderer,
                    SlotNames[index]);
                RequireApprovedGeometry(renderer, SlotNames[index]);
            }

            foreach (var spec in ApprovedMaterials)
            {
                InspectMaterial(
                    spec,
                    shader,
                    approvedMesh.bounds);
            }

            return new Inspection
            {
                ModelCount = SlotNames.Length,
                ImportedVertexCount = approvedMesh.vertexCount,
                TriangleCount = TriangleCount,
                BoneCount = BoneCount,
                MaterialCount = ApprovedMaterials.Length,
                ImportedMaterialNames =
                    approvedRenderer.sharedMaterials
                        .Select(item => item.name)
                        .ToArray(),
                ImportedSubMeshTriangles =
                    Enumerable.Range(
                            0,
                            approvedMesh.subMeshCount)
                        .Select(index => checked(
                            (int)approvedMesh.GetIndexCount(index) /
                            3))
                        .ToArray()
            };
        }

        private static Mesh RequireApprovedGeometry(
            SkinnedMeshRenderer renderer,
            string label)
        {
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           label + " has no skinned mesh.");
            if (renderer.bones.Length != BoneCount ||
                mesh.subMeshCount != ActiveMaterialSlotCount)
            {
                throw new InvalidOperationException(
                    label +
                    " geometry contract differs. Bones=" +
                    renderer.bones.Length +
                    ", SubMeshes=" + mesh.subMeshCount + ".");
            }

            var importedSubMeshTriangles =
                new int[mesh.subMeshCount];
            var triangles = 0;
            for (var index = 0;
                 index < ApprovedSubMeshTriangles.Length;
                 index++)
            {
                var count = checked(
                    (int)mesh.GetIndexCount(index) / 3);
                importedSubMeshTriangles[index] = count;
                triangles += count;
            }

            if (!importedSubMeshTriangles
                    .OrderBy(value => value)
                    .SequenceEqual(
                        ApprovedSubMeshTriangles
                            .OrderBy(value => value)) ||
                triangles != TriangleCount)
            {
                throw new InvalidOperationException(
                    label +
                    " approved material partition differs. Imported=" +
                    string.Join(",", importedSubMeshTriangles) +
                    ", Approved=" +
                    string.Join(",", ApprovedSubMeshTriangles) +
                    ", Triangles=" + triangles + ".");
            }

            return mesh;
        }

        private static void RequireCompatibleBones(
            SkinnedMeshRenderer target,
            SkinnedMeshRenderer approved,
            string label)
        {
            var targetNames = target.bones
                .Select(item => item != null ? item.name : string.Empty)
                .ToArray();
            var approvedNames = approved.bones
                .Select(item => item != null ? item.name : string.Empty)
                .ToArray();
            if (!targetNames.SequenceEqual(
                    approvedNames,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    label +
                    " bone order differs from the approved sample FBX.");
            }
        }

        private static void InspectMaterial(
            ApprovedMaterialSpec spec,
            Shader shader,
            Bounds bounds)
        {
            var material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    spec.MaterialPath) ??
                throw new InvalidOperationException(
                    "Approved Revolution material is missing: " +
                    spec.MaterialPath);
            var expectedBase = spec.BaseTexturePath == null
                ? null
                : RequireTexture(spec.BaseTexturePath);
            var expectedDetail = spec.DetailTexturePath == null
                ? null
                : RequireTexture(spec.DetailTexturePath);
            var expectedMin = new Vector4(
                bounds.min.x,
                bounds.min.y,
                bounds.min.z,
                0f);
            var expectedSize = new Vector4(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z,
                0f);
            if (material.shader != shader ||
                material.GetTexture("_BaseMap") != expectedBase ||
                material.GetTexture("_DetailMap") != expectedDetail ||
                material.GetColor("_SolidColor") != spec.SolidColor ||
                !Close(
                    material.GetFloat("_UseTexture"),
                    spec.BaseTexturePath == null ? 0f : 1f) ||
                !Close(
                    material.GetFloat("_UseDetail"),
                    spec.UseDetail ? 1f : 0f) ||
                !Close(material.GetFloat("_DetailMix"), 0.35f) ||
                !Close(material.GetFloat("_Metallic"), spec.Metallic) ||
                !Close(material.GetFloat("_Roughness"), spec.Roughness) ||
                !Close(material.GetFloat("_BumpStrength"), 0.10f) ||
                !Close(material.GetFloat("_BumpDistance"), 0.025f) ||
                !Close(
                    material.GetFloat("_EmissionStrength"),
                    spec.EmissionStrength) ||
                !Close(material.GetVector("_BoundsMin"), expectedMin) ||
                !Close(material.GetVector("_BoundsSize"), expectedSize))
            {
                throw new InvalidOperationException(
                    spec.Name +
                    " no longer matches the approved Blender material conversion contract.");
            }
        }

        private static void RequireSupportedShader(Shader shader)
        {
            var errors = ShaderUtil.GetShaderMessages(shader)
                .Where(message => string.Equals(
                    message.severity.ToString(),
                    "Error",
                    StringComparison.OrdinalIgnoreCase))
                .Select(message => message.message)
                .ToArray();
            if (!shader.isSupported ||
                !string.Equals(
                    shader.name,
                    ShaderName,
                    StringComparison.Ordinal) ||
                shader.passCount <= 0 ||
                errors.Length > 0)
            {
                throw new InvalidOperationException(
                    "The approved Revolution appearance shader did not compile for the current render pipeline. Errors=" +
                    string.Join(" | ", errors));
            }
        }

        private static void RequireNoMagentaShaderFallback()
        {
            var player = GameObject.Find("Player") ??
                         throw new InvalidOperationException(
                             "Player is missing.");
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException(
                             "The Player camera is missing.");
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(
                320,
                180,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(
                320,
                180,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(
                    new Rect(0f, 0f, 320f, 180f),
                    0,
                    0);
                image.Apply();
                var magentaPixels = image.GetPixels32()
                    .Count(pixel =>
                        pixel.r >= 240 &&
                        pixel.b >= 240 &&
                        pixel.g <= 24);
                if (magentaPixels > 0)
                {
                    throw new InvalidOperationException(
                        "The approved Revolution appearance rendered with Unity's magenta shader fallback. MagentaPixels=" +
                        magentaPixels + ".");
                }
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

        private static Material[] MaterialsInImportedSubMeshOrder(
            SkinnedMeshRenderer approvedRenderer,
            IReadOnlyList<Material> materialAssets)
        {
            var importedMaterials =
                approvedRenderer.sharedMaterials;
            if (importedMaterials.Length !=
                ActiveMaterialSlotCount)
            {
                throw new InvalidOperationException(
                    "The approved Revolution FBX imported material count differs. Count=" +
                    importedMaterials.Length + ".");
            }

            var ordered =
                new Material[importedMaterials.Length];
            for (var index = 0;
                 index < importedMaterials.Length;
                 index++)
            {
                var imported = importedMaterials[index] ??
                               throw new InvalidOperationException(
                                   "The approved Revolution FBX has a null imported material at index " +
                                   index + ".");
                var materialIndex = Array.FindIndex(
                    ApprovedMaterials,
                    item => string.Equals(
                        item.Name,
                        imported.name,
                        StringComparison.Ordinal));
                if (materialIndex < 0)
                {
                    throw new InvalidOperationException(
                        "The approved Revolution FBX imported an unexpected material name at submesh " +
                        index + ": " + imported.name + ". ImportedOrder=" +
                        string.Join(
                            ",",
                            importedMaterials.Select(
                                item => item != null
                                    ? item.name
                                    : "<null>")));
                }

                ordered[index] = materialAssets[materialIndex];
            }

            if (ordered.Distinct().Count() !=
                ActiveMaterialSlotCount)
            {
                throw new InvalidOperationException(
                    "The approved Revolution FBX imported material order contains duplicates.");
            }

            return ordered;
        }

        private static void RequireApprovedInputs()
        {
            RequireHash(
                ApprovedSampleModelPath,
                ApprovedModelSha256);
            RequireHash(
                ApprovedModelPath,
                ApprovedModelSha256);
            foreach (var pair in ApprovedTextureHashes)
            {
                RequireHash(
                    ApprovedSampleRoot + "/textures/" + pair.Key,
                    pair.Value);
                RequireHash(
                    TextureFolder + "/" + pair.Key,
                    pair.Value);
            }
        }

        private static void RequireHash(
            string relativePath,
            string expected)
        {
            var path = Absolute(relativePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "Approved Revolution input is missing.",
                    path);
            }

            RequireSameHash(
                expected,
                Sha256(path),
                relativePath);
        }

        private static Texture2D RequireTexture(
            string fileName)
        {
            var path = TextureFolder + "/" + fileName;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                   throw new InvalidOperationException(
                       "Approved direct-crop texture is missing: " +
                       path);
        }

        private static SkinnedMeshRenderer RequireSingleRenderer(
            Transform root,
            string label)
        {
            var renderers =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    label +
                    " must contain exactly one skinned renderer. Count=" +
                    renderers.Length + ".");
            }

            return renderers[0];
        }

        private static GameObject RequirePlacementRoot()
        {
            return GameObject.Find(PlacementRootName) ??
                   throw new InvalidOperationException(
                       "The Revolution placement root is missing.");
        }

        private static void RequireSlotContract(Transform root)
        {
            if (root.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "The Revolution placement must contain exactly eight slots.");
            }

            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = root.GetChild(index);
                if (slot.name != SlotNames[index] ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "The Revolution slot contract differs at index " +
                        index + ".");
                }
            }
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the current active scene. ActiveScene=" +
                    scene.path);
            }

            return scene;
        }

        private static string[] HierarchyTransformSignatures(
            Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(item =>
                    RelativePath(root, item) + "|" +
                    Vec(item.localPosition) + "|" +
                    Quat(item.localRotation) + "|" +
                    Vec(item.localScale))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .Select(item =>
                    GlobalObjectId.GetGlobalObjectIdSlow(item) + "|" +
                    item.name + "|" +
                    item.activeSelf + "|" +
                    Vec(item.transform.position) + "|" +
                    Quat(item.transform.rotation) + "|" +
                    Vec(item.transform.localScale) + "|" +
                    item.transform.childCount)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string RelativePath(
            Transform root,
            Transform item)
        {
            if (item == root)
            {
                return root.name;
            }

            var parts = new Stack<string>();
            var cursor = item;
            while (cursor != null && cursor != root)
            {
                parts.Push(cursor.name);
                cursor = cursor.parent;
            }

            return root.name + "/" + string.Join("/", parts);
        }

        private static void WriteInspectionReport(
            Inspection inspection)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Revolution appearance report folder."));
            var report = new StringBuilder();
            report.AppendLine(
                "Revolution Approved Appearance Inspection");
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine(
                "PlacementRoot=" + PlacementRootName);
            report.AppendLine(
                "ApprovedSampleFbxSha256=" +
                ApprovedModelSha256);
            report.AppendLine(
                "UnityApprovedFbxSha256=" +
                Sha256(Absolute(ApprovedModelPath)));
            report.AppendLine(
                "ModelCount=" +
                inspection.ModelCount.ToString(
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "AuthoredVertices=" +
                AuthoredVertexCount.ToString(
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "ImportedRenderVertices=" +
                inspection.ImportedVertexCount.ToString(
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "Triangles=" +
                inspection.TriangleCount.ToString(
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "Loops=" +
                LoopCount.ToString(
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "Bones=" +
                inspection.BoneCount.ToString(
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "BlenderMaterialSlots=" +
                BlenderMaterialSlotCount.ToString(
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "UnityActiveMaterialSlots=" +
                inspection.MaterialCount.ToString(
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "ZeroFaceBlenderSlotDroppedByUnity=Unseen_Back_Default_Preserved");
            report.AppendLine(
                "ApprovedBlenderMaterialOrder=" +
                string.Join(
                    ",",
                    ApprovedMaterials.Select(item => item.Name)));
            report.AppendLine(
                "ApprovedBlenderMaterialTriangles=" +
                string.Join(",", ApprovedSubMeshTriangles));
            report.AppendLine(
                "UnityImportedSubMeshMaterialOrder=" +
                string.Join(
                    ",",
                    inspection.ImportedMaterialNames));
            report.AppendLine(
                "UnityImportedSubMeshTriangles=" +
                string.Join(
                    ",",
                    inspection.ImportedSubMeshTriangles));
            report.AppendLine(
                "TorsoPolygons=" + TorsoPolygonCount);
            report.AppendLine(
                "ShoulderConnectionPolygons=" +
                ShoulderConnectionPolygonCount);
            report.AppendLine(
                "RightArmSymmetryCopiedPolygons=" +
                MirroredArmChangedPolygonCount);
            report.AppendLine(
                "TextureCoordinateRule=abs(2 * Generated.X - 1)");
            report.AppendLine(
                "BodyPanelMix=DirectCropBase mixed 35% toward DirectCropWear");
            report.AppendLine(
                "MeshGeometryModified=False");
            report.AppendLine(
                "TexturePixelsGenerated=False");
            report.AppendLine(
                "MemoryRenderMagentaFallbackPixels=0");
            report.AppendLine(
                "RootSlotModelBoneTransformsChanged=False");
            report.AppendLine(
                "PlayerCameraAndOtherRootsChanged=False");
            report.AppendLine("SceneChangedByInspection=False");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static void Capture(
            Camera camera,
            string path,
            int width,
            int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "Invalid Revolution appearance capture folder."));
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0);
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

        private static void RequireSameHash(
            string expected,
            string actual,
            string label)
        {
            if (!string.Equals(
                    expected,
                    actual,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    label +
                    " hash differs. Expected=" +
                    expected + ", Actual=" + actual + ".");
            }
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(
                    sha.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    relative));
        }

        private static bool Close(float first, float second)
        {
            return Mathf.Abs(first - second) <= 0.0001f;
        }

        private static bool Close(Vector4 first, Vector4 second)
        {
            return (first - second).sqrMagnitude <= 0.00000001f;
        }

        private static string Num(float value)
        {
            return value.ToString(
                "0.######",
                CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" +
                   Num(value.x) + ", " +
                   Num(value.y) + ", " +
                   Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" +
                   Num(value.x) + ", " +
                   Num(value.y) + ", " +
                   Num(value.z) + ", " +
                   Num(value.w) + ")";
        }

        private sealed class ApprovedMaterialSpec
        {
            public ApprovedMaterialSpec(
                string name,
                string baseTexturePath,
                string detailTexturePath,
                float metallic,
                float roughness,
                bool useDetail,
                float emissionStrength,
                Color solidColor)
            {
                Name = name;
                BaseTexturePath = baseTexturePath;
                DetailTexturePath = detailTexturePath;
                Metallic = metallic;
                Roughness = roughness;
                UseDetail = useDetail;
                EmissionStrength = emissionStrength;
                SolidColor = solidColor;
            }

            public string Name { get; }
            public string BaseTexturePath { get; }
            public string DetailTexturePath { get; }
            public float Metallic { get; }
            public float Roughness { get; }
            public bool UseDetail { get; }
            public float EmissionStrength { get; }
            public Color SolidColor { get; }

            public string MaterialPath =>
                MaterialFolder + "/" + Name + ".mat";
        }

        private sealed class RendererState
        {
            private readonly SkinnedMeshRenderer renderer;
            private readonly Mesh mesh;
            private readonly Material[] materials;

            public RendererState(SkinnedMeshRenderer renderer)
            {
                this.renderer = renderer;
                mesh = renderer.sharedMesh;
                materials = renderer.sharedMaterials;
            }

            public void Restore()
            {
                if (renderer == null)
                {
                    return;
                }

                renderer.sharedMesh = mesh;
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private sealed class Inspection
        {
            public int ModelCount { get; set; }
            public int ImportedVertexCount { get; set; }
            public int TriangleCount { get; set; }
            public int BoneCount { get; set; }
            public int MaterialCount { get; set; }
            public string[] ImportedMaterialNames { get; set; }
            public int[] ImportedSubMeshTriangles { get; set; }
        }
    }
}
