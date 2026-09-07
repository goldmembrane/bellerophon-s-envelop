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

namespace Bellerophon.Editor.Validation
{
    internal static class ShieldSurfaceAppearanceTools
    {
        private const string ModelPath = "Assets/_Project/Art/Items/Shield/shield.fbx";
        private const string BaseColorPath = "Assets/_Project/Art/Items/Shield/Textures/base_color.jpg";
        private const string NormalPath = "Assets/_Project/Art/Items/Shield/Textures/normal.jpg";
        private const string MetallicSmoothnessPath = "Assets/_Project/Art/Items/Shield/Textures/shield_metallic_smoothness.png";
        private const string MaterialPath = "Assets/_Project/Art/Items/Shield/Materials/Material.001.mat";
        private const string SampleDirectory = "artSample/shield_surface_2026-09-07";
        private const string ApprovedTexturePath = "Assets/_Project/Art/Items/Shield/Textures/shield_base_color_text_removed.png";
        private const string ApprovedSolidMaterialPath = "Assets/_Project/Art/Items/Shield/Materials/Shield_TextFree.mat";
        private const string ApprovedWindowMaterialPath = "Assets/_Project/Art/Items/Shield/Materials/Shield_Window_Transparent.mat";
        private const string ApprovedMeshPath = "Assets/_Project/Art/Items/Shield/Meshes/Shield_TextFree_Window.asset";
        private const string ValidationDirectory = "docs/validation/shield_surface_appearance_65_corrected_2026-09-07";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ShieldObjectName = "Shield_RightForeArm";
        private const float ApprovedWindowOpacity = 0.65f;
        private const int PreviewWidth = 640;
        private const int PreviewHeight = 900;
        private static readonly Color RenderBackground = new Color32(8, 12, 18, 255);
        private static readonly string[] TargetNames =
        {
            "Shield_Draw", "Shield_Idle", "Shield_Stow", "Shield_Raise",
            "Shield_Block_Idle", "Shield_Lower", "Shield_Block_Impact", "Shield_Break_Reaction"
        };

        internal static void Inspect()
        {
            Directory.CreateDirectory(SampleDirectory);
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported Shield prefab is missing.");
            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath) ??
                throw new InvalidOperationException("Shield base-color texture is missing.");
            var report = new StringBuilder();
            report.AppendLine("Shield surface source inspection.");
            report.AppendLine("model=" + ModelPath);
            report.AppendLine("baseColor=" + BaseColorPath + " size=" + baseColor.width + "x" + baseColor.height);
            foreach (MeshFilter filter in imported.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;
                report.AppendLine("mesh=" + mesh.name + " path=" +
                    AnimationUtility.CalculateTransformPath(filter.transform, imported.transform) +
                    " vertices=" + mesh.vertexCount + " triangles=" + mesh.triangles.Length / 3 +
                    " subMeshes=" + mesh.subMeshCount + " bounds=" + FormatBounds(mesh.bounds));
                DescribeConnectedComponents(report, mesh);
            }
            File.WriteAllText(Path.Combine(SampleDirectory, "source_inspection.txt"), report.ToString(), Encoding.UTF8);
            Debug.Log("Shield surface source inspection complete.\n" + report);
        }

        internal static void BuildArtSample()
        {
            Directory.CreateDirectory(SampleDirectory);
            string modelHash = HashFile(ModelPath);
            string textureHash = HashFile(BaseColorPath);
            string materialHash = HashFile(MaterialPath);
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported Shield prefab is missing.");
            var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            Texture2D cleanTexture = null;
            Texture2D current = null;
            Texture2D cleanOpaque = null;
            Texture2D alpha35 = null;
            Texture2D alpha50 = null;
            Texture2D alpha65 = null;
            Texture2D comparison = null;
            try
            {
                if (!sourceTexture.LoadImage(File.ReadAllBytes(BaseColorPath)))
                    throw new InvalidDataException("Shield base-color texture could not be decoded.");
                cleanTexture = RemoveLettering(sourceTexture);
                File.WriteAllBytes(Path.Combine(SampleDirectory, "text_removed_atlas_preview.png"),
                    cleanTexture.EncodeToPNG());

                current = RenderFront(imported, null);
                cleanOpaque = RenderFront(imported, cleanTexture);
                // These rectangles are measured from the fixed 640x900 orthographic preview below.
                var textPanel = new RectInt(160, 765, 320, 65);
                var viewport = new RectInt(218, 594, 204, 137);
                SmoothPanel(cleanOpaque, textPanel);
                ReplaceBackgroundWithChecker(current);
                ReplaceBackgroundWithChecker(cleanOpaque);
                alpha35 = WithSemitransparentWindow(cleanOpaque, viewport, 0.35f);
                alpha50 = WithSemitransparentWindow(cleanOpaque, viewport, 0.50f);
                alpha65 = WithSemitransparentWindow(cleanOpaque, viewport, 0.65f);
                File.WriteAllBytes(Path.Combine(SampleDirectory, "current.png"), current.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(SampleDirectory, "text_removed_window_35.png"), alpha35.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(SampleDirectory, "text_removed_window_50_recommended.png"), alpha50.EncodeToPNG());
                File.WriteAllBytes(Path.Combine(SampleDirectory, "text_removed_window_65.png"), alpha65.EncodeToPNG());
                comparison = Compose(current, alpha35, alpha50, alpha65);
                File.WriteAllBytes(Path.Combine(SampleDirectory, "comparison.png"), comparison.EncodeToPNG());
                WriteDocumentation(viewport, textPanel, modelHash, textureHash, materialHash);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceTexture);
                if (cleanTexture != null) UnityEngine.Object.DestroyImmediate(cleanTexture);
                if (current != null) UnityEngine.Object.DestroyImmediate(current);
                if (cleanOpaque != null) UnityEngine.Object.DestroyImmediate(cleanOpaque);
                if (alpha35 != null) UnityEngine.Object.DestroyImmediate(alpha35);
                if (alpha50 != null) UnityEngine.Object.DestroyImmediate(alpha50);
                if (alpha65 != null) UnityEngine.Object.DestroyImmediate(alpha65);
                if (comparison != null) UnityEngine.Object.DestroyImmediate(comparison);
            }
            if (modelHash != HashFile(ModelPath) || textureHash != HashFile(BaseColorPath) ||
                materialHash != HashFile(MaterialPath))
                throw new InvalidOperationException("Art-sample creation changed a live Shield source asset.");
            Debug.Log("Shield text-removal and semitransparent-window art sample built without changing live assets.");
        }

        internal static void ApplyApprovedAppearance()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Approved Shield appearance must be applied in Edit Mode.");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("The existing CargoRunMvp scene must be active.");
            Directory.CreateDirectory(ValidationDirectory);
            string originalModelHash = HashFile(ModelPath);
            string originalBaseColorHash = HashFile(BaseColorPath);
            string originalMaterialHash = HashFile(MaterialPath);
            int approvedTextMaskPixels = ApplyApprovedTextMaskToExistingChannels();

            Texture2D approvedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedTexturePath) ??
                throw new InvalidOperationException("The already approved text-free Shield texture is missing.");
            if (HashFile(ApprovedTexturePath) !=
                HashFile(Path.Combine(SampleDirectory, "text_removed_atlas_preview.png")))
                throw new InvalidOperationException("The applied Shield texture differs from the approved sample texture.");
            Material solidMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedSolidMaterialPath) ??
                throw new InvalidOperationException("The already approved solid Shield material is missing.");
            Material windowMaterial = UpdateWindowMaterial();
            Mesh approvedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ApprovedMeshPath) ??
                throw new InvalidOperationException("The already approved split-window Shield mesh is missing.");
            int solidTriangleCount = approvedMesh.GetTriangles(0).Length / 3;
            int windowTriangleCount = approvedMesh.GetTriangles(1).Length / 3;
            Bounds windowBounds = CalculateSubMeshBounds(approvedMesh, 1);
            foreach (string targetName in TargetNames)
            {
                Transform target = FindUnique(scene, targetName).transform;
                Transform forearm = RequireNamedTransform(target, "RightForeArm");
                Transform shield = forearm.Cast<Transform>().Single(child => child.name == ShieldObjectName);
                ApplyApprovedAppearanceToShield(shield, approvedMesh, solidMaterial, windowMaterial);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after approved Shield appearance application.");
            AssetDatabase.SaveAssets();
            if (originalModelHash != HashFile(ModelPath) || originalBaseColorHash != HashFile(BaseColorPath) ||
                originalMaterialHash != HashFile(MaterialPath))
                throw new InvalidOperationException("Approved appearance application changed an original Shield source asset.");

            var report = new StringBuilder();
            report.AppendLine("Approved Shield surface appearance applied.");
            report.AppendLine("approvedSample=artSample/shield_surface_2026-09-07/text_removed_window_65.png");
            report.AppendLine("approvedWindowOpacity=" + ApprovedWindowOpacity.ToString("F2", CultureInfo.InvariantCulture));
            report.AppendLine("approvedTextMaskPixels=" + approvedTextMaskPixels);
            report.AppendLine("derivedTexture=" + ApprovedTexturePath + " sha256=" + HashFile(ApprovedTexturePath));
            report.AppendLine("existingNormalChannel=" + NormalPath + " sha256=" + HashFile(NormalPath));
            report.AppendLine("existingMetallicSmoothnessChannel=" + MetallicSmoothnessPath + " sha256=" +
                HashFile(MetallicSmoothnessPath));
            report.AppendLine("solidMaterial=" + ApprovedSolidMaterialPath);
            report.AppendLine("windowMaterial=" + ApprovedWindowMaterialPath);
            report.AppendLine("derivedMesh=" + ApprovedMeshPath + " vertices=" + approvedMesh.vertexCount +
                " solidTriangles=" + solidTriangleCount + " windowTriangles=" + windowTriangleCount +
                " windowBounds=" + FormatBounds(windowBounds));
            report.AppendLine("allEightTargetsApplied=True sceneSaved=True");
            report.AppendLine("originalModelSHA256=" + originalModelHash);
            report.AppendLine("originalBaseColorSHA256=" + originalBaseColorHash);
            report.AppendLine("originalMaterialSHA256=" + originalMaterialHash);
            report.AppendLine("Original FBX, base color, material, mesh vertices, UVs, normals, rig, skin weights and animations were not modified.");
            report.AppendLine("No new texture, material, mesh or visual effect was generated. The approved text-free pixel mask was applied to the existing mapped channels.");
            File.WriteAllText(Path.Combine(ValidationDirectory, "application.txt"), report.ToString(), Encoding.UTF8);
            Debug.Log("Approved text-free Shield surface and 65% opaque window applied to all eight targets.\n" + report);
        }

        internal static bool TryApplyApprovedAppearanceToShield(Transform shield)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ApprovedMeshPath);
            Material solid = AssetDatabase.LoadAssetAtPath<Material>(ApprovedSolidMaterialPath);
            Material window = AssetDatabase.LoadAssetAtPath<Material>(ApprovedWindowMaterialPath);
            if (mesh == null || solid == null || window == null) return false;
            ApplyApprovedAppearanceToShield(shield, mesh, solid, window);
            return true;
        }

        internal static void InspectApprovedAppearance()
        {
            Directory.CreateDirectory(ValidationDirectory);
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("The existing CargoRunMvp scene must be active.");
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported Shield prefab is missing.");
            Mesh sourceMesh = imported.GetComponentsInChildren<MeshFilter>(true).Single().sharedMesh;
            Mesh approvedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ApprovedMeshPath) ??
                throw new InvalidOperationException("Approved Shield mesh is missing.");
            Texture2D approvedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedTexturePath) ??
                throw new InvalidOperationException("Approved Shield texture is missing.");
            Material solidMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedSolidMaterialPath) ??
                throw new InvalidOperationException("Approved solid Shield material is missing.");
            Material windowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedWindowMaterialPath) ??
                throw new InvalidOperationException("Approved window material is missing.");

            bool verticesEqual = sourceMesh.vertices.SequenceEqual(approvedMesh.vertices);
            bool normalsEqual = sourceMesh.normals.SequenceEqual(approvedMesh.normals);
            bool uvEqual = sourceMesh.uv.SequenceEqual(approvedMesh.uv);
            string sourceTriangleDigest = TriangleDigest(new[] { sourceMesh.triangles });
            string approvedTriangleDigest = TriangleDigest(Enumerable.Range(0, approvedMesh.subMeshCount)
                .Select(approvedMesh.GetTriangles));
            bool trianglesEqual = sourceTriangleDigest == approvedTriangleDigest;
            string sampleTextureHash = HashFile(Path.Combine(SampleDirectory, "text_removed_atlas_preview.png"));
            string approvedTextureHash = HashFile(ApprovedTexturePath);
            float opacity = windowMaterial.GetColor("_BaseColor").a;
            bool allTargetsApplied = true;
            var report = new StringBuilder();
            report.AppendLine("Approved Shield surface appearance inspection.");
            foreach (string targetName in TargetNames)
            {
                Transform target = FindUnique(scene, targetName).transform;
                Transform forearm = RequireNamedTransform(target, "RightForeArm");
                Transform shield = forearm.Cast<Transform>().Single(child => child.name == ShieldObjectName);
                MeshFilter filter = shield.GetComponentsInChildren<MeshFilter>(true).Single();
                Renderer renderer = filter.GetComponent<Renderer>();
                bool applied = filter.sharedMesh == approvedMesh && renderer.sharedMaterials.Length == 2 &&
                    renderer.sharedMaterials[0] == solidMaterial && renderer.sharedMaterials[1] == windowMaterial;
                allTargetsApplied &= applied;
                report.AppendLine(targetName + " applied=" + applied + " mesh=" + AssetDatabase.GetAssetPath(filter.sharedMesh) +
                    " materials=" + string.Join("|", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            }
            report.AppendLine("sampleTextureSHA256=" + sampleTextureHash);
            report.AppendLine("approvedTextureSHA256=" + approvedTextureHash);
            report.AppendLine("sampleTextureMatchesApproved=" + (sampleTextureHash == approvedTextureHash));
            report.AppendLine("sourceVertexCount=" + sourceMesh.vertexCount + " approvedVertexCount=" + approvedMesh.vertexCount);
            report.AppendLine("verticesEqual=" + verticesEqual + " normalsEqual=" + normalsEqual + " uvEqual=" + uvEqual);
            report.AppendLine("sourceTriangleDigest=" + sourceTriangleDigest);
            report.AppendLine("approvedTriangleDigest=" + approvedTriangleDigest + " trianglesEqual=" + trianglesEqual);
            report.AppendLine("approvedSubMeshCount=" + approvedMesh.subMeshCount +
                " solidTriangles=" + approvedMesh.GetTriangles(0).Length / 3 +
                " windowTriangles=" + approvedMesh.GetTriangles(1).Length / 3);
            report.AppendLine("windowShader=" + windowMaterial.shader.name +
                " surface=" + windowMaterial.GetFloat("_Surface").ToString("F1", CultureInfo.InvariantCulture) +
                " opacity=" + opacity.ToString("F2", CultureInfo.InvariantCulture) +
                " renderQueue=" + windowMaterial.renderQueue);
            report.AppendLine("allEightTargetsApplied=" + allTargetsApplied);
            report.AppendLine("originalModelSHA256=" + HashFile(ModelPath));
            File.WriteAllText(Path.Combine(ValidationDirectory, "inspection.txt"), report.ToString(), Encoding.UTF8);
            if (!allTargetsApplied || sampleTextureHash != approvedTextureHash || !verticesEqual || !normalsEqual ||
                !uvEqual || !trianglesEqual || approvedMesh.subMeshCount != 2 ||
                approvedMesh.GetTriangles(1).Length == 0 || Mathf.Abs(opacity - ApprovedWindowOpacity) > 0.0001f ||
                windowMaterial.GetFloat("_Surface") != 1f ||
                windowMaterial.shader.name != "Universal Render Pipeline/Unlit")
                throw new InvalidOperationException("Approved Shield surface inspection failed.\n" + report);
            Debug.Log("Approved Shield surface inspection passed.\n" + report);
        }

        private static Texture2D CreateApprovedTexture()
        {
            string sourcePath = Path.Combine(SampleDirectory, "text_removed_atlas_preview.png");
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("Approved Shield texture sample is missing.", sourcePath);
            File.WriteAllBytes(ApprovedTexturePath, File.ReadAllBytes(sourcePath));
            AssetDatabase.ImportAsset(ApprovedTexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ApprovedTexturePath) as TextureImporter ??
                throw new InvalidOperationException("Approved Shield texture importer is unavailable.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedTexturePath) ??
                throw new InvalidOperationException("Approved Shield texture failed to import.");
        }

        private static Material CreateSolidMaterial(Texture2D approvedTexture)
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ??
                throw new InvalidOperationException("Original extracted Shield material is missing.");
            var configured = new Material(source) { name = "Shield_TextFree" };
            configured.SetTexture("_BaseMap", approvedTexture);
            configured.SetTexture("_MainTex", approvedTexture);
            Material asset = SaveMaterial(configured, ApprovedSolidMaterialPath);
            UnityEngine.Object.DestroyImmediate(configured);
            return asset;
        }

        private static Material UpdateWindowMaterial()
        {
            Material asset = AssetDatabase.LoadAssetAtPath<Material>(ApprovedWindowMaterialPath) ??
                throw new InvalidOperationException("The already approved Shield window material is missing.");
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                throw new InvalidOperationException("The URP Unlit shader required by the approved flat window sample is missing.");
            asset.shader = shader;
            Color baseColor = asset.GetColor("_BaseColor");
            baseColor.a = ApprovedWindowOpacity;
            asset.SetColor("_BaseColor", baseColor);
            if (asset.HasProperty("_Color")) asset.SetColor("_Color", baseColor);
            if (asset.HasProperty("_BaseMap")) asset.SetTexture("_BaseMap", null);
            if (asset.HasProperty("_MainTex")) asset.SetTexture("_MainTex", null);
            if (asset.HasProperty("_Surface")) asset.SetFloat("_Surface", 1f);
            if (asset.HasProperty("_Blend")) asset.SetFloat("_Blend", 0f);
            if (asset.HasProperty("_AlphaClip")) asset.SetFloat("_AlphaClip", 0f);
            if (asset.HasProperty("_SrcBlend")) asset.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (asset.HasProperty("_DstBlend")) asset.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (asset.HasProperty("_ZWrite")) asset.SetFloat("_ZWrite", 0f);
            if (asset.HasProperty("_Cull")) asset.SetFloat("_Cull", (float)CullMode.Off);
            asset.SetOverrideTag("RenderType", "Transparent");
            asset.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            asset.DisableKeyword("_ALPHATEST_ON");
            asset.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        private static int ApplyApprovedTextMaskToExistingChannels()
        {
            Texture2D sourceBase = LoadTextureFile(BaseColorPath);
            Texture2D approvedBase = LoadTextureFile(ApprovedTexturePath);
            Texture2D normal = LoadTextureFile(NormalPath);
            Texture2D metallicSmoothness = LoadTextureFile(MetallicSmoothnessPath);
            try
            {
                int width = sourceBase.width;
                int height = sourceBase.height;
                if (approvedBase.width != width || approvedBase.height != height ||
                    normal.width != width || normal.height != height ||
                    metallicSmoothness.width != width || metallicSmoothness.height != height)
                    throw new InvalidOperationException("Approved Shield texture channels do not share the source UV dimensions.");

                Color32[] sourcePixels = sourceBase.GetPixels32();
                Color32[] approvedPixels = approvedBase.GetPixels32();
                var approvedMask = new bool[sourcePixels.Length];
                int maskPixelCount = 0;
                for (int index = 0; index < sourcePixels.Length; index++)
                {
                    Color32 source = sourcePixels[index];
                    Color32 approved = approvedPixels[index];
                    bool changed = source.r != approved.r || source.g != approved.g || source.b != approved.b;
                    approvedMask[index] = changed;
                    if (changed) maskPixelCount++;
                }
                if (maskPixelCount == 0)
                    throw new InvalidOperationException("The approved text-free sample contains no mapped pixel differences.");

                Color32[] cleanedNormal = InpaintApprovedMask(normal.GetPixels32(), approvedMask, width, height, true);
                Color32[] cleanedMetallic = InpaintApprovedMask(metallicSmoothness.GetPixels32(), approvedMask,
                    width, height, false);
                normal.SetPixels32(cleanedNormal);
                normal.Apply(false, false);
                metallicSmoothness.SetPixels32(cleanedMetallic);
                metallicSmoothness.Apply(false, false);
                File.WriteAllBytes(NormalPath, normal.EncodeToJPG(100));
                File.WriteAllBytes(MetallicSmoothnessPath, metallicSmoothness.EncodeToPNG());
                AssetDatabase.ImportAsset(NormalPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(MetallicSmoothnessPath, ImportAssetOptions.ForceSynchronousImport);
                return maskPixelCount;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceBase);
                UnityEngine.Object.DestroyImmediate(approvedBase);
                UnityEngine.Object.DestroyImmediate(normal);
                UnityEngine.Object.DestroyImmediate(metallicSmoothness);
            }
        }

        private static Texture2D LoadTextureFile(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidDataException("Shield texture could not be decoded: " + path);
            }
            return texture;
        }

        private static Color32[] InpaintApprovedMask(Color32[] source, bool[] mask, int width, int height,
            bool normalizeAsNormalMap)
        {
            Color32[] output = (Color32[])source.Clone();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (!mask[index]) continue;
                Vector4 sum = Vector4.zero;
                int count = 0;
                for (int radius = 4; radius <= 28 && count < 6; radius += 4)
                foreach (Vector2Int direction in InpaintDirections)
                {
                    int sampleX = Mathf.Clamp(x + direction.x * radius, 0, width - 1);
                    int sampleY = Mathf.Clamp(y + direction.y * radius, 0, height - 1);
                    int sampleIndex = sampleY * width + sampleX;
                    if (mask[sampleIndex]) continue;
                    Color32 sample = source[sampleIndex];
                    sum += new Vector4(sample.r, sample.g, sample.b, sample.a);
                    count++;
                }
                if (count == 0) continue;
                Vector4 average = sum / count;
                if (normalizeAsNormalMap)
                {
                    Vector3 direction = new Vector3(average.x / 127.5f - 1f, average.y / 127.5f - 1f,
                        average.z / 127.5f - 1f).normalized;
                    average.x = (direction.x + 1f) * 127.5f;
                    average.y = (direction.y + 1f) * 127.5f;
                    average.z = (direction.z + 1f) * 127.5f;
                }
                output[index] = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(average.x), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(average.y), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(average.z), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(average.w), 0, 255));
            }
            return output;
        }

        private static Bounds CalculateSubMeshBounds(Mesh mesh, int subMesh)
        {
            int[] indices = mesh.GetTriangles(subMesh);
            if (indices.Length == 0) throw new InvalidOperationException("Approved Shield window submesh is empty.");
            Vector3[] vertices = mesh.vertices;
            Bounds bounds = new Bounds(vertices[indices[0]], Vector3.zero);
            foreach (int index in indices.Skip(1)) bounds.Encapsulate(vertices[index]);
            return bounds;
        }

        private static Material SaveMaterial(Material configured, string path)
        {
            Material asset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (asset == null)
            {
                asset = new Material(configured);
                AssetDatabase.CreateAsset(asset, path);
            }
            else EditorUtility.CopySerialized(configured, asset);
            asset.name = configured.name;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        private static Mesh CreateWindowMesh(out int solidTriangleCount, out int windowTriangleCount,
            out Bounds windowBounds)
        {
            EnsureFolder("Assets/_Project/Art/Items/Shield/Meshes");
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported Shield prefab is missing.");
            Mesh source = imported.GetComponentsInChildren<MeshFilter>(true).Single().sharedMesh;
            Vector3[] vertices = source.vertices;
            int[] triangles = source.triangles;
            var solid = new List<int>(triangles.Length);
            var window = new List<int>();
            windowBounds = new Bounds();
            bool hasWindowBounds = false;
            for (int index = 0; index < triangles.Length; index += 3)
            {
                Vector3 a = vertices[triangles[index]];
                Vector3 b = vertices[triangles[index + 1]];
                Vector3 c = vertices[triangles[index + 2]];
                Vector3 center = (a + b + c) / 3f;
                bool isWindow = Mathf.Abs(center.x) <= 0.00120f &&
                    center.z >= 0.00202f && center.z <= 0.00334f;
                List<int> destination = isWindow ? window : solid;
                destination.Add(triangles[index]);
                destination.Add(triangles[index + 1]);
                destination.Add(triangles[index + 2]);
                if (!isWindow) continue;
                if (!hasWindowBounds)
                {
                    windowBounds = new Bounds(a, Vector3.zero);
                    hasWindowBounds = true;
                }
                windowBounds.Encapsulate(a);
                windowBounds.Encapsulate(b);
                windowBounds.Encapsulate(c);
            }
            solidTriangleCount = solid.Count / 3;
            windowTriangleCount = window.Count / 3;
            if (windowTriangleCount < 2 || windowTriangleCount > triangles.Length / 6)
                throw new InvalidOperationException("Window triangle selection is outside the safe range. Count=" + windowTriangleCount);
            var configured = UnityEngine.Object.Instantiate(source);
            configured.name = "Shield_TextFree_Window";
            configured.subMeshCount = 2;
            configured.SetTriangles(solid, 0, false);
            configured.SetTriangles(window, 1, false);
            configured.bounds = source.bounds;
            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(ApprovedMeshPath);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(configured, ApprovedMeshPath);
                asset = configured;
            }
            else
            {
                EditorUtility.CopySerialized(configured, asset);
                UnityEngine.Object.DestroyImmediate(configured);
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        private static void ApplyApprovedAppearanceToShield(Transform shield, Mesh mesh,
            Material solidMaterial, Material windowMaterial)
        {
            MeshFilter filter = shield.GetComponentsInChildren<MeshFilter>(true).Single();
            Renderer renderer = filter.GetComponent<Renderer>() ??
                throw new InvalidOperationException(shield.name + " has no renderer beside its mesh filter.");
            Undo.RecordObjects(new UnityEngine.Object[] { filter, renderer }, "Apply approved Shield surface appearance");
            filter.sharedMesh = mesh;
            renderer.sharedMaterials = new[] { solidMaterial, windowMaterial };
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(filter);
            EditorUtility.SetDirty(renderer);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one " + name + ", found " + matches.Length + ".");
            return matches[0];
        }

        private static Transform RequireNamedTransform(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " requires one " + name + ". Found=" + matches.Length);
            return matches[0];
        }

        private static string TriangleDigest(IEnumerable<int[]> triangleSets)
        {
            string canonical = string.Join("\n", triangleSets.SelectMany(set =>
            {
                var values = new List<string>(set.Length / 3);
                for (int index = 0; index < set.Length; index += 3)
                {
                    int[] triangle = { set[index], set[index + 1], set[index + 2] };
                    Array.Sort(triangle);
                    values.Add(triangle[0] + ":" + triangle[1] + ":" + triangle[2]);
                }
                return values;
            }).OrderBy(value => value, StringComparer.Ordinal));
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", string.Empty);
        }

        private static Texture2D RemoveLettering(Texture2D source)
        {
            Color32[] pixels = source.GetPixels32();
            int width = source.width;
            int height = source.height;
            var mask = new bool[pixels.Length];
            PixelRect[] regions =
            {
                FromTopLeft(20, 75, 440, 480, height),
                FromTopLeft(620, 125, 820, 275, height),
                FromTopLeft(1420, 70, 1650, 460, height),
                FromTopLeft(240, 570, 735, 990, height),
                FromTopLeft(760, 1280, 1160, 1580, height),
                FromTopLeft(350, 1290, 530, 1530, height),
                FromTopLeft(1850, 1680, 2048, 2048, height)
            };
            foreach (PixelRect region in regions)
            for (int y = region.YMin; y < region.YMax; y++)
            for (int x = region.XMin; x < region.XMax; x++)
            {
                int index = y * width + x;
                float luminance = Luminance(pixels[index]);
                if (luminance < 0.48f || Saturation(pixels[index]) > 0.24f) continue;
                float neighborhood = 0f;
                int count = 0;
                foreach (Vector2Int offset in NeighborhoodOffsets)
                {
                    int sampleX = Mathf.Clamp(x + offset.x, 0, width - 1);
                    int sampleY = Mathf.Clamp(y + offset.y, 0, height - 1);
                    neighborhood += Luminance(pixels[sampleY * width + sampleX]);
                    count++;
                }
                if (luminance - neighborhood / count >= 0.11f) mask[index] = true;
            }
            bool[] expanded = (bool[])mask.Clone();
            for (int y = 2; y < height - 2; y++)
            for (int x = 2; x < width - 2; x++)
            {
                if (!mask[y * width + x]) continue;
                for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                    if (dx * dx + dy * dy <= 5) expanded[(y + dy) * width + x + dx] = true;
            }
            Color32[] result = (Color32[])pixels.Clone();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (!expanded[index]) continue;
                Vector4 sum = Vector4.zero;
                int count = 0;
                for (int radius = 4; radius <= 28 && count < 6; radius += 4)
                foreach (Vector2Int direction in InpaintDirections)
                {
                    int sampleX = Mathf.Clamp(x + direction.x * radius, 0, width - 1);
                    int sampleY = Mathf.Clamp(y + direction.y * radius, 0, height - 1);
                    int sampleIndex = sampleY * width + sampleX;
                    if (expanded[sampleIndex]) continue;
                    Color32 sample = pixels[sampleIndex];
                    sum += new Vector4(sample.r, sample.g, sample.b, sample.a);
                    count++;
                }
                if (count > 0) result[index] = new Color32((byte)(sum.x / count), (byte)(sum.y / count),
                    (byte)(sum.z / count), 255);
            }
            var output = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            output.SetPixels32(result);
            output.Apply(false, false);
            return output;
        }

        private static readonly Vector2Int[] NeighborhoodOffsets =
        {
            new Vector2Int(-14, 0), new Vector2Int(14, 0), new Vector2Int(0, -14), new Vector2Int(0, 14),
            new Vector2Int(-10, -10), new Vector2Int(-10, 10), new Vector2Int(10, -10), new Vector2Int(10, 10)
        };

        private static readonly Vector2Int[] InpaintDirections =
        {
            Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up,
            new Vector2Int(-1, -1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(1, 1)
        };

        private static PixelRect FromTopLeft(int xMin, int yMin, int xMax, int yMax, int height) =>
            new PixelRect(xMin, height - yMax, xMax, height - yMin);

        private static float Luminance(Color32 color) =>
            (0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b) / 255f;

        private static float Saturation(Color32 color)
        {
            byte maximum = Math.Max(color.r, Math.Max(color.g, color.b));
            byte minimum = Math.Min(color.r, Math.Min(color.g, color.b));
            return maximum == 0 ? 0f : (maximum - minimum) / (float)maximum;
        }

        private static Texture2D RenderFront(GameObject imported, Texture2D overrideBaseColor)
        {
            const int previewLayer = 31;
            GameObject clone = null;
            GameObject cameraObject = null;
            GameObject keyLightObject = null;
            GameObject fillLightObject = null;
            RenderTexture renderTarget = null;
            var temporaryMaterials = new List<Material>();
            try
            {
                clone = UnityEngine.Object.Instantiate(imported);
                clone.name = "Shield_Surface_Sample_Temporary";
                clone.hideFlags = HideFlags.HideAndDontSave;
                clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                SetLayerRecursively(clone.transform, previewLayer);
                foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials.Select(source =>
                    {
                        var material = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
                        if (overrideBaseColor != null)
                        {
                            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", overrideBaseColor);
                            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", overrideBaseColor);
                        }
                        temporaryMaterials.Add(material);
                        return material;
                    }).ToArray();
                    renderer.sharedMaterials = materials;
                }
                Bounds bounds = clone.GetComponentsInChildren<Renderer>(true)
                    .Select(renderer => renderer.bounds).Aggregate((left, right) => { left.Encapsulate(right); return left; });
                cameraObject = new GameObject("Shield_Surface_Sample_Camera") { hideFlags = HideFlags.HideAndDontSave };
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = bounds.size.z * 0.58f;
                camera.aspect = PreviewWidth / (float)PreviewHeight;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 10f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = RenderBackground;
                camera.cullingMask = 1 << previewLayer;
                camera.transform.position = bounds.center + Vector3.down * 2f;
                camera.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                keyLightObject = CreateDirectionalLight("Shield_Surface_Key", previewLayer, 1.25f,
                    Quaternion.Euler(80f, 20f, 0f));
                fillLightObject = CreateDirectionalLight("Shield_Surface_Fill", previewLayer, 0.55f,
                    Quaternion.Euler(100f, -35f, 180f));
                renderTarget = new RenderTexture(PreviewWidth, PreviewHeight, 24, RenderTextureFormat.ARGB32)
                    { antiAliasing = 4, hideFlags = HideFlags.HideAndDontSave };
                camera.targetTexture = renderTarget;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTarget;
                var output = new Texture2D(PreviewWidth, PreviewHeight, TextureFormat.RGBA32, false, false);
                output.ReadPixels(new Rect(0, 0, PreviewWidth, PreviewHeight), 0, 0);
                output.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return output;
            }
            finally
            {
                if (renderTarget != null) UnityEngine.Object.DestroyImmediate(renderTarget);
                foreach (Material material in temporaryMaterials) UnityEngine.Object.DestroyImmediate(material);
                if (fillLightObject != null) UnityEngine.Object.DestroyImmediate(fillLightObject);
                if (keyLightObject != null) UnityEngine.Object.DestroyImmediate(keyLightObject);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static GameObject CreateDirectionalLight(string name, int layer, float intensity, Quaternion rotation)
        {
            var owner = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave, layer = layer };
            Light light = owner.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = new Color(0.82f, 0.90f, 1f);
            light.cullingMask = 1 << layer;
            owner.transform.rotation = rotation;
            return owner;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }

        private static void SmoothPanel(Texture2D texture, RectInt area)
        {
            Color[] source = texture.GetPixels();
            Color[] output = (Color[])source.Clone();
            int lowerY = Mathf.Clamp(area.yMin - 5, 0, texture.height - 1);
            int upperY = Mathf.Clamp(area.yMax + 5, 0, texture.height - 1);
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
            {
                float t = Mathf.InverseLerp(area.yMin, Mathf.Max(area.yMin + 1, area.yMax - 1), y);
                Color lower = source[lowerY * texture.width + x];
                Color upper = source[upperY * texture.width + x];
                output[y * texture.width + x] = Color.Lerp(lower, upper, t);
            }
            texture.SetPixels(output);
            texture.Apply(false, false);
        }

        private static void ReplaceBackgroundWithChecker(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            var outside = new bool[pixels.Length];
            var queue = new Queue<int>();
            for (int x = 0; x < texture.width; x++)
            {
                EnqueueBackground(x, 0);
                EnqueueBackground(x, texture.height - 1);
            }
            for (int y = 1; y < texture.height - 1; y++)
            {
                EnqueueBackground(0, y);
                EnqueueBackground(texture.width - 1, y);
            }
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % texture.width;
                int y = index / texture.width;
                EnqueueBackground(x - 1, y);
                EnqueueBackground(x + 1, y);
                EnqueueBackground(x, y - 1);
                EnqueueBackground(x, y + 1);
            }
            for (int index = 0; index < pixels.Length; index++)
                if (outside[index]) pixels[index] = Checker(index % texture.width, index / texture.width);
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            void EnqueueBackground(int x, int y)
            {
                if (x < 0 || x >= texture.width || y < 0 || y >= texture.height) return;
                int index = y * texture.width + x;
                if (outside[index] || ColorDistance(pixels[index], RenderBackground) >= 0.06f) return;
                outside[index] = true;
                queue.Enqueue(index);
            }
        }

        private static Texture2D WithSemitransparentWindow(Texture2D source, RectInt viewport, float opacity)
        {
            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
            Color[] pixels = source.GetPixels();
            for (int y = viewport.yMin; y < viewport.yMax; y++)
            for (int x = viewport.xMin; x < viewport.xMax; x++)
            {
                int index = y * source.width + x;
                float edge = Mathf.Min(Mathf.Min(x - viewport.xMin, viewport.xMax - 1 - x),
                    Mathf.Min(y - viewport.yMin, viewport.yMax - 1 - y));
                float feather = Mathf.Clamp01(edge / 3f);
                Color transparentResult = Color.Lerp(Checker(x, y), pixels[index], opacity);
                pixels[index] = Color.Lerp(pixels[index], transparentResult, feather);
            }
            result.SetPixels(pixels);
            result.Apply(false, false);
            return result;
        }

        private static Color Checker(int x, int y) => ((x / 24 + y / 24) & 1) == 0
            ? new Color32(55, 68, 78, 255)
            : new Color32(105, 121, 132, 255);

        private static float ColorDistance(Color left, Color right) =>
            Mathf.Max(Mathf.Abs(left.r - right.r), Mathf.Abs(left.g - right.g), Mathf.Abs(left.b - right.b));

        private static Texture2D Compose(params Texture2D[] panels)
        {
            const int gap = 20;
            int width = panels.Length * PreviewWidth + (panels.Length - 1) * gap;
            var output = new Texture2D(width, PreviewHeight, TextureFormat.RGBA32, false, false);
            Color[] background = Enumerable.Repeat(new Color(0.025f, 0.03f, 0.04f, 1f),
                width * PreviewHeight).ToArray();
            output.SetPixels(background);
            for (int index = 0; index < panels.Length; index++)
                output.SetPixels(index * (PreviewWidth + gap), 0, PreviewWidth, PreviewHeight, panels[index].GetPixels());
            output.Apply(false, false);
            return output;
        }

        private static void WriteDocumentation(RectInt viewport, RectInt textPanel,
            string modelHash, string textureHash, string materialHash)
        {
            string html = "<!doctype html><html lang=\"ko\"><head><meta charset=\"utf-8\"><title>방패 표면 비교 샘플</title>" +
                "<style>body{margin:0;background:#0c1118;color:#eaf2f8;font-family:system-ui,sans-serif}main{max-width:1500px;margin:auto;padding:32px}" +
                "h1{margin:0 0 8px}.note{color:#a9bdca;margin-bottom:24px}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:16px}" +
                "figure{margin:0;background:#151e28;border:1px solid #314352;border-radius:10px;overflow:hidden}img{width:100%;display:block}" +
                "figcaption{padding:12px 14px}.pick{border-color:#74d4e8;box-shadow:0 0 0 2px #74d4e833}.facts{margin-top:24px;padding:18px;background:#111922;border-radius:10px}" +
                "code{color:#8fe1ef}</style></head><body><main><h1>방패 글자 제거·반투명 창 비교</h1>" +
                "<p class=\"note\">왼쪽은 현재 상태이며, 나머지는 모든 글자 제거를 전제로 창 불투명도만 비교한 시안입니다. 체크무늬가 더 잘 보일수록 투명도가 높습니다.</p>" +
                "<section class=\"grid\"><figure><img src=\"current.png\"><figcaption>현재 상태</figcaption></figure>" +
                "<figure><img src=\"text_removed_window_35.png\"><figcaption>창 불투명도 35% · 투명함</figcaption></figure>" +
                "<figure class=\"pick\"><img src=\"text_removed_window_50_recommended.png\"><figcaption>창 불투명도 50% · 권장</figcaption></figure>" +
                "<figure><img src=\"text_removed_window_65.png\"><figcaption>창 불투명도 65% · 진함</figcaption></figure></section>" +
                "<section class=\"facts\"><p>목표: 방패의 모든 문자 표식을 제거하고, 사용자가 표시한 전면 창만 반투명 재질로 분리합니다.</p>" +
                "<p>제작 방식: 원본 FBX·텍스처·머티리얼을 읽어 만든 비생성형 미리보기입니다. 생성형 이미지 도구를 사용하지 않았습니다.</p>" +
                "<p>Unity 적용 상태: <strong>미적용</strong>. 샘플 승인 뒤 실제 텍스처와 창 머티리얼을 구현합니다.</p>" +
                "<p>보호 범위: 메시 형상, 리그, 스킨 웨이트, 애니메이션, 원본 FBX는 변경하지 않습니다.</p>" +
                "<p><a href=\"text_removed_atlas_preview.png\">글자 제거 텍스처 시안</a> · <a href=\"comparison.png\">한 장 비교 이미지</a> · <a href=\"README.md\">검토 메모</a></p></section>" +
                "</main></body></html>";
            File.WriteAllText(Path.Combine(SampleDirectory, "index.html"), html, Encoding.UTF8);
            string readme = "# 방패 표면 샘플\n\n" +
                "- 범위: 방패의 모든 문자 표식 제거, 전면 창 반투명화\n" +
                "- 권장안: 창 불투명도 50%\n" +
                "- 비교 순서: 현재 / 35% / 50% / 65%\n" +
                "- 창 미리보기 픽셀 영역: " + viewport + "\n" +
                "- 전면 글자 패널 미리보기 픽셀 영역: " + textPanel + "\n" +
                "- 생성형 이미지 사용: 없음\n" +
                "- Unity 실제 에셋 적용: 미적용, 샘플 승인 대기\n" +
                "- 승인 후 구현: base color의 모든 글자 UV 영역을 주변 표면으로 복원하고 전면 창을 별도 반투명 머티리얼로 분리\n" +
                "- 원본 FBX·메시·리그·스킨 웨이트·애니메이션 변경: 없음\n";
            File.WriteAllText(Path.Combine(SampleDirectory, "README.md"), readme, Encoding.UTF8);
            string integrity = "liveAssetsChanged=False\nmodelSHA256=" + modelHash +
                "\nbaseColorSHA256=" + textureHash + "\nmaterialSHA256=" + materialHash + "\n";
            File.WriteAllText(Path.Combine(SampleDirectory, "asset_integrity.txt"), integrity, Encoding.UTF8);
        }

        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }

        private readonly struct PixelRect
        {
            internal readonly int XMin;
            internal readonly int YMin;
            internal readonly int XMax;
            internal readonly int YMax;

            internal PixelRect(int xMin, int yMin, int xMax, int yMax)
            {
                XMin = xMin;
                YMin = yMin;
                XMax = xMax;
                YMax = yMax;
            }
        }

        private static void DescribeConnectedComponents(StringBuilder report, Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            int[] triangles = mesh.triangles;
            int[] parent = Enumerable.Range(0, vertices.Length).ToArray();
            var coincident = new Dictionary<Vector3, int>();
            for (int index = 0; index < vertices.Length; index++)
            {
                if (coincident.TryGetValue(vertices[index], out int existing)) Union(parent, existing, index);
                else coincident.Add(vertices[index], index);
            }
            for (int triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                Union(parent, triangles[triangle], triangles[triangle + 1]);
                Union(parent, triangles[triangle], triangles[triangle + 2]);
            }
            var groups = Enumerable.Range(0, vertices.Length)
                .GroupBy(index => Find(parent, index))
                .Select(group =>
                {
                    int[] indices = group.ToArray();
                    Bounds bounds = new Bounds(vertices[indices[0]], Vector3.zero);
                    Vector2 uvMin = uv.Length == vertices.Length ? uv[indices[0]] : Vector2.zero;
                    Vector2 uvMax = uvMin;
                    foreach (int index in indices.Skip(1))
                    {
                        bounds.Encapsulate(vertices[index]);
                        if (uv.Length == vertices.Length)
                        {
                            uvMin = Vector2.Min(uvMin, uv[index]);
                            uvMax = Vector2.Max(uvMax, uv[index]);
                        }
                    }
                    int triangleCount = 0;
                    for (int triangle = 0; triangle < triangles.Length; triangle += 3)
                        if (Find(parent, triangles[triangle]) == group.Key) triangleCount++;
                    return new { Root = group.Key, Indices = indices, TriangleCount = triangleCount,
                        Bounds = bounds, UvMin = uvMin, UvMax = uvMax };
                })
                .OrderByDescending(group => group.Indices.Length)
                .ToArray();
            report.AppendLine("connectedComponents=" + groups.Length);
            for (int index = 0; index < groups.Length; index++)
            {
                var group = groups[index];
                report.AppendLine("component[" + index + "] root=" + group.Root +
                    " vertices=" + group.Indices.Length + " triangles=" + group.TriangleCount +
                    " bounds=" + FormatBounds(group.Bounds) + " uvMin=" + Format(group.UvMin) +
                    " uvMax=" + Format(group.UvMax));
            }
        }

        private static int Find(int[] parent, int value)
        {
            while (parent[value] != value)
            {
                parent[value] = parent[parent[value]];
                value = parent[value];
            }
            return value;
        }

        private static void Union(int[] parent, int left, int right)
        {
            int leftRoot = Find(parent, left);
            int rightRoot = Find(parent, right);
            if (leftRoot != rightRoot) parent[rightRoot] = leftRoot;
        }

        private static string Format(Vector2 value) => string.Format(CultureInfo.InvariantCulture,
            "({0:F6},{1:F6})", value.x, value.y);

        private static string FormatBounds(Bounds bounds) =>
            "center=" + bounds.center.ToString("R") + " size=" + bounds.size.ToString("R") +
            " min=" + bounds.min.ToString("R") + " max=" + bounds.max.ToString("R");
    }
}
