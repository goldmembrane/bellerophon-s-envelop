using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.ResistanceCargoRunScene
{
    internal static class ResistanceApprovedBakedAppearanceApply
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Resistance Enemy Placement";
        private const string ModelName = "Resistance_Model";
        private const string ApprovedModelPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/ResistanceApprovedAppearance.fbx";
        private const string ApprovedModelSha256 =
            "84B6A36298F357D59820EF2F05AE9E557E7A5DD2E13B95A5EFEA7F65179248B1";
        private const string OriginalModelPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/Resistance.fbx";
        private const string OriginalModelSha256 =
            "282B05DA51DEE72291B865940AA84E83CD0F90E49BCABA0A480996AA21A7C303";
        private const string ShaderName =
            "Bellerophon/Resistance/ApprovedBakedAppearance";
        private const string ShaderPath =
            "Assets/_Project/Art/Enemies/Resistance/Shaders/ResistanceApprovedBakedAppearance.shader";
        private const string TextureRoot =
            "Assets/_Project/Art/Enemies/Resistance/Textures";
        private const string MaterialRoot =
            "Assets/_Project/Art/Enemies/Resistance/Materials/BakedApproved";
        private const int SlotCount = 14;
        private const int ExpectedTriangleCount = 6037;
        private const int ExpectedBoneCount = 24;
        private const float BoundsTolerance = 0.01f;

        // These files are direct bakes from the approved Blender sample.
        private static readonly BakedTextureContract[] TextureContracts =
        {
            new BakedTextureContract(
                "_BaseMap",
                "resistance_approved_triangle_albedo.png",
                "972D1775CDB363E8393DBA6D50C87B297C2F952CF1EE048DC65DCE56522E4520",
                true,
                false),
            new BakedTextureContract(
                "_EmissionMap",
                "resistance_approved_triangle_emission.png",
                "8C7E76835B9ECCC409E90A003C8A33367F0ED9748A3B96443E9EA58BD5392060",
                true,
                false),
            new BakedTextureContract(
                "_RoughnessMap",
                "resistance_approved_triangle_roughness.png",
                "5630EDF42913F842E57308503A7E0977E721D09027A12C453B8878C22108CD37",
                false,
                false),
            new BakedTextureContract(
                "_NormalMap",
                "resistance_approved_triangle_normal.png",
                "16B149632C6F01DA354ABEC63CAB1FB704DF3551B76FFE3882DBAE4D4654E271",
                false,
                true)
        };

        private static readonly ApprovedMaterialContract[] MaterialContracts =
        {
            new ApprovedMaterialContract(
                "M_Resistance_Worn_Silver",
                "M_Resistance_Approved_Baked_Silver.mat",
                0.38f,
                1.0f),
            new ApprovedMaterialContract(
                "M_Resistance_Dark_Mechanics",
                "M_Resistance_Approved_Baked_Dark.mat",
                0.48f,
                1.0f),
            new ApprovedMaterialContract(
                "M_Resistance_Cyan_Emission",
                "M_Resistance_Approved_Baked_Cyan.mat",
                0.32f,
                1.45f),
            new ApprovedMaterialContract(
                "M_Resistance_Bronze_Accents",
                "M_Resistance_Approved_Baked_Bronze.mat",
                0.58f,
                1.0f),
            new ApprovedMaterialContract(
                "M_Resistance_Bandana_Olive",
                "M_Resistance_Approved_Baked_Olive.mat",
                0.0f,
                1.0f)
        };

        [MenuItem(
            "Bellerophon/Enemies/Resistance/Apply Direct Approved Baked Appearance")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Direct approved Resistance appearance must be applied in Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            }

            RequireHash(OriginalModelPath, OriginalModelSha256);
            RequireHash(ApprovedModelPath, ApprovedModelSha256);
            var textures = EnsureBakedTextures();
            var materials = EnsureApprovedMaterials(textures);
            var modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ApprovedModelPath) ??
                throw new InvalidOperationException(
                    "Approved baked Resistance FBX is missing.");
            var placementRoot = scene
                .GetRootGameObjects()
                .SingleOrDefault(item =>
                    item.name == PlacementRootName) ??
                throw new InvalidOperationException(
                    "Resistance placement root is missing.");
            if (placementRoot.transform.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "Resistance slot count changed.");
            }

            var totalRenderVertices = 0;
            var totalTriangles = 0;
            for (var slotIndex = 0;
                 slotIndex < SlotCount;
                 slotIndex++)
            {
                var slot = placementRoot.transform.GetChild(slotIndex);
                if (slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        slot.name + " does not contain exactly one model.");
                }

                var previousModel = slot.GetChild(0);
                var previousBounds = RendererBounds(previousModel);
                var localPosition = previousModel.localPosition;
                var localRotation = previousModel.localRotation;
                var localScale = previousModel.localScale;
                var siblingIndex = previousModel.GetSiblingIndex();

                var replacement =
                    PrefabUtility.InstantiatePrefab(
                        modelAsset,
                        slot) as GameObject ??
                    throw new InvalidOperationException(
                        "Approved baked Resistance FBX could not be instantiated.");
                replacement.name = ModelName;
                replacement.transform.localPosition = localPosition;
                replacement.transform.localRotation = localRotation;
                replacement.transform.localScale = localScale;
                replacement.transform.SetSiblingIndex(siblingIndex);

                try
                {
                    var replacementBounds =
                        RendererBounds(replacement.transform);
                    if (Vector3.Distance(
                            previousBounds.size,
                            replacementBounds.size) >
                        BoundsTolerance)
                    {
                        throw new InvalidOperationException(
                            slot.name +
                            " approved FBX bounds differ from the existing model. Existing=" +
                            Format(previousBounds.size) +
                            ", Approved=" +
                            Format(replacementBounds.size) + ".");
                    }

                    var metrics = AssignApprovedMaterials(
                        replacement,
                        materials);
                    totalRenderVertices += metrics.VertexCount;
                    totalTriangles += metrics.TriangleCount;
                }
                catch
                {
                    UnityEngine.Object.DestroyImmediate(replacement);
                    throw;
                }

                UnityEngine.Object.DestroyImmediate(
                    previousModel.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after direct approved Resistance replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(OriginalModelPath, OriginalModelSha256);
            RequireHash(ApprovedModelPath, ApprovedModelSha256);
            Debug.Log(
                "ApprovedResistanceDirectBakedAppearance Result=PASS, Slots=" +
                SlotCount +
                ", AuthoredVerticesPerModel=3004, TrianglesPerModel=" +
                ExpectedTriangleCount +
                ", BonesPerModel=" +
                ExpectedBoneCount +
                ", TotalImportedRenderVertices=" +
                totalRenderVertices +
                ", TotalTriangles=" +
                totalTriangles +
                ", ApprovedFbxSha256=" +
                ApprovedModelSha256 +
                ", OriginalFbxSha256=" +
                OriginalModelSha256 +
                ", GeometryChanged=False, PlacementTransformsPreserved=True, SceneSaved=True.");
        }

        private static Dictionary<string, Texture2D> EnsureBakedTextures()
        {
            EnsureFolder(TextureRoot);
            var result = new Dictionary<string, Texture2D>(
                StringComparer.Ordinal);
            foreach (var contract in TextureContracts)
            {
                var source =
                    "artSample/enemies/resistance/textures/" +
                    contract.FileName;
                RequireHash(source, contract.Sha256);
                var destination =
                    TextureRoot + "/" + contract.FileName;
                var destinationAbsolute = Absolute(destination);
                if (!File.Exists(destinationAbsolute) ||
                    Sha256(destinationAbsolute) != contract.Sha256)
                {
                    File.Copy(
                        Absolute(source),
                        destinationAbsolute,
                        true);
                }

                AssetDatabase.ImportAsset(
                    destination,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                var importer =
                    AssetImporter.GetAtPath(destination) as
                        TextureImporter ??
                    throw new InvalidOperationException(
                        "Approved baked texture importer is missing: " +
                        destination);
                importer.textureType = contract.IsNormal
                    ? TextureImporterType.NormalMap
                    : TextureImporterType.Default;
                importer.sRGBTexture = contract.Srgb;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                result.Add(
                    contract.PropertyName,
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        destination) ??
                    throw new InvalidOperationException(
                        "Approved baked texture could not be loaded: " +
                        destination));
            }

            return result;
        }

        private static Dictionary<string, Material> EnsureApprovedMaterials(
            IReadOnlyDictionary<string, Texture2D> textures)
        {
            EnsureFolder(MaterialRoot);
            var shader =
                AssetDatabase.LoadAssetAtPath<Shader>(
                    ShaderPath) ??
                Shader.Find(ShaderName) ??
                throw new InvalidOperationException(
                    "Approved baked Resistance shader is missing.");
            var result = new Dictionary<string, Material>(
                StringComparer.Ordinal);
            foreach (var contract in MaterialContracts)
            {
                var path =
                    MaterialRoot + "/" + contract.FileName;
                var material =
                    AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader)
                    {
                        name = Path.GetFileNameWithoutExtension(
                            contract.FileName)
                    };
                    AssetDatabase.CreateAsset(material, path);
                }
                else
                {
                    material.shader = shader;
                }

                foreach (var texture in textures)
                {
                    material.SetTexture(
                        texture.Key,
                        texture.Value);
                }

                material.SetFloat(
                    "_Metallic",
                    contract.Metallic);
                material.SetFloat(
                    "_EmissionStrength",
                    contract.EmissionStrength);
                material.SetFloat("_NormalScale", 1.0f);
                EditorUtility.SetDirty(material);
                result.Add(
                    contract.SourceMaterialName,
                    material);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static ModelMetrics AssignApprovedMaterials(
            GameObject model,
            IReadOnlyDictionary<string, Material> materials)
        {
            var renderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    model.name +
                    " must contain one approved skinned renderer.");
            }

            var renderer = renderers[0];
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           "Approved Resistance renderer mesh is missing.");
            var triangles = Enumerable.Range(
                    0,
                    mesh.subMeshCount)
                .Sum(index =>
                    checked(
                        (int)mesh.GetIndexCount(index)) / 3);
            if (triangles != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount)
            {
                throw new InvalidOperationException(
                    "Approved Resistance imported geometry contract differs. Triangles=" +
                    triangles +
                    ", Bones=" +
                    renderer.bones.Length + ".");
            }

            var importedMaterials = renderer.sharedMaterials;
            if (importedMaterials.Length != mesh.subMeshCount)
            {
                throw new InvalidOperationException(
                    "Approved Resistance imported material slots differ from submeshes.");
            }

            renderer.sharedMaterials = importedMaterials
                .Select(imported =>
                {
                    if (imported == null ||
                        !materials.TryGetValue(
                            imported.name,
                            out var approved))
                    {
                        throw new InvalidOperationException(
                            "Approved Resistance imported material slot is unknown: " +
                            (imported != null
                                ? imported.name
                                : "<null>") + ".");
                    }

                    return approved;
                })
                .ToArray();
            EditorUtility.SetDirty(renderer);
            return new ModelMetrics(
                mesh.vertexCount,
                triangles);
        }

        private static Bounds RendererBounds(Transform model)
        {
            var renderers = model
                .GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    model.name + " has no renderer bounds.");
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1;
                 index < segments.Length;
                 index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }

                current = next;
            }
        }

        private static void RequireHash(
            string projectRelativePath,
            string expected)
        {
            var path = Absolute(projectRelativePath);
            if (!File.Exists(path) ||
                Sha256(path) != expected)
            {
                throw new InvalidOperationException(
                    "Approved Resistance file hash differs: " +
                    projectRelativePath);
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

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(
                        Application.dataPath)?.FullName ??
                    throw new InvalidOperationException(
                        "Unity project root is unavailable."),
                    projectRelativePath));
        }

        private static string Format(Vector3 value)
        {
            return "(" +
                   value.x.ToString(
                       "0.######",
                       CultureInfo.InvariantCulture) +
                   ", " +
                   value.y.ToString(
                       "0.######",
                       CultureInfo.InvariantCulture) +
                   ", " +
                   value.z.ToString(
                       "0.######",
                       CultureInfo.InvariantCulture) +
                   ")";
        }

        private readonly struct BakedTextureContract
        {
            public BakedTextureContract(
                string propertyName,
                string fileName,
                string sha256,
                bool srgb,
                bool isNormal)
            {
                PropertyName = propertyName;
                FileName = fileName;
                Sha256 = sha256;
                Srgb = srgb;
                IsNormal = isNormal;
            }

            public string PropertyName { get; }
            public string FileName { get; }
            public string Sha256 { get; }
            public bool Srgb { get; }
            public bool IsNormal { get; }
        }

        private readonly struct ApprovedMaterialContract
        {
            public ApprovedMaterialContract(
                string sourceMaterialName,
                string fileName,
                float metallic,
                float emissionStrength)
            {
                SourceMaterialName = sourceMaterialName;
                FileName = fileName;
                Metallic = metallic;
                EmissionStrength = emissionStrength;
            }

            public string SourceMaterialName { get; }
            public string FileName { get; }
            public float Metallic { get; }
            public float EmissionStrength { get; }
        }

        private readonly struct ModelMetrics
        {
            public ModelMetrics(
                int vertexCount,
                int triangleCount)
            {
                VertexCount = vertexCount;
                TriangleCount = triangleCount;
            }

            public int VertexCount { get; }
            public int TriangleCount { get; }
        }
    }
}
