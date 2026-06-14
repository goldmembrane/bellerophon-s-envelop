using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bellerophon.Editor.Validation
{
    public static class AssetInventoryCatalogRenderer
    {
        private const string OutputRootRelativePath = "artSample/asset_inventory_catalog_2026-06-14";
        private const string SuccessMarker = "Asset inventory catalog saved:";
        private const int PreviewLayer = 28;
        private const int ObjectPreviewSize = 384;
        private const int TexturePreviewSize = 256;

        private static readonly string[] AssetRootPaths =
        {
            "Assets/Heavy Station Kit",
            "Assets/Sci-Fi Styled Modular Pack",
            "Assets/ScifiOfficeLite",
            "Assets/GoldenFrame_Terminal_FREE"
        };

        private static readonly string[] ObjectExtensions =
        {
            ".prefab",
            ".fbx"
        };

        private static readonly string[] TextureExtensions =
        {
            ".png",
            ".tga"
        };

        private static readonly string[] MaterialExtensions =
        {
            ".mat"
        };

        [MenuItem("Bellerophon/Validation/Capture Asset Inventory Catalog")]
        public static void Capture()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for asset inventory catalog output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, OutputRootRelativePath);
            EnsureCleanOutputRoot(projectRoot.FullName, outputRoot);

            var thumbnailRoot = Path.Combine(outputRoot, "thumbnails");
            var objectThumbnailRoot = Path.Combine(thumbnailRoot, "objects");
            var textureThumbnailRoot = Path.Combine(thumbnailRoot, "textures");
            var materialThumbnailRoot = Path.Combine(thumbnailRoot, "materials");
            Directory.CreateDirectory(objectThumbnailRoot);
            Directory.CreateDirectory(textureThumbnailRoot);
            Directory.CreateDirectory(materialThumbnailRoot);

            var records = CollectRecords(projectRoot.FullName);
            var objectRecords = records.Where(record => record.AssetType == "Prefab" || record.AssetType == "Model").ToList();
            var textureRecords = records.Where(record => record.AssetType == "Texture").ToList();
            var materialRecords = records.Where(record => record.AssetType == "Material").ToList();

            using (var context = PreviewRenderContext.Create())
            {
                for (var i = 0; i < objectRecords.Count; i++)
                {
                    CaptureObjectRecord(context, objectRecords[i], objectThumbnailRoot, "thumbnails/objects", i + 1);
                }

                for (var i = 0; i < materialRecords.Count; i++)
                {
                    CaptureMaterialRecord(context, materialRecords[i], materialThumbnailRoot, "thumbnails/materials", i + 1);
                }
            }

            for (var i = 0; i < textureRecords.Count; i++)
            {
                CaptureTextureRecord(textureRecords[i], textureThumbnailRoot, "thumbnails/textures", i + 1);
            }

            WriteCsv(records, Path.Combine(outputRoot, "asset_catalog.csv"));
            WriteJson(records, Path.Combine(outputRoot, "asset_catalog.json"));
            WriteHtml(records, Path.Combine(outputRoot, "index.html"));
            WriteReadme(records, Path.Combine(outputRoot, "README.md"));
            WriteApprovalStatus(Path.Combine(outputRoot, "APPROVAL_STATUS.json"));

            AssetDatabase.Refresh();
            Debug.Log(SuccessMarker + " " + outputRoot);
        }

        private static void EnsureCleanOutputRoot(string projectRoot, string outputRoot)
        {
            var normalizedProjectRoot = Path.GetFullPath(projectRoot);
            var normalizedOutputRoot = Path.GetFullPath(outputRoot);
            if (!normalizedOutputRoot.StartsWith(normalizedProjectRoot, StringComparison.OrdinalIgnoreCase) ||
                normalizedOutputRoot.IndexOf(Path.Combine("artSample", "asset_inventory_catalog_2026-06-14"), StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("Refusing to clean unexpected asset catalog output path: " + normalizedOutputRoot);
            }

            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, true);
            }

            Directory.CreateDirectory(outputRoot);
        }

        private static List<AssetCatalogRecord> CollectRecords(string projectRoot)
        {
            var records = new List<AssetCatalogRecord>();
            foreach (var rootPath in AssetRootPaths)
            {
                var absoluteRoot = Path.Combine(projectRoot, rootPath.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(absoluteRoot))
                {
                    continue;
                }

                var files = Directory.EnumerateFiles(absoluteRoot, "*.*", SearchOption.AllDirectories)
                    .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    .Where(IsCatalogAsset)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

                foreach (var absolutePath in files)
                {
                    var assetPath = ToAssetPath(projectRoot, absolutePath);
                    var extension = Path.GetExtension(assetPath).ToLowerInvariant();
                    var type = GetAssetType(extension);
                    var pack = GetPackName(assetPath);
                    var category = Classify(assetPath, type);
                    records.Add(new AssetCatalogRecord
                    {
                        AssetPath = assetPath,
                        FileName = Path.GetFileName(assetPath),
                        AssetType = type,
                        Pack = pack,
                        Category = category,
                        SuggestedUse = GetSuggestedUse(category, type, assetPath),
                        Notes = GetNotes(category, type, assetPath)
                    });
                }
            }

            return records
                .OrderBy(record => record.Pack, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => TypeSortKey(record.AssetType))
                .ThenBy(record => record.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(record => record.AssetPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsCatalogAsset(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return ObjectExtensions.Contains(extension) ||
                TextureExtensions.Contains(extension) ||
                MaterialExtensions.Contains(extension);
        }

        private static string ToAssetPath(string projectRoot, string absolutePath)
        {
            var normalizedProjectRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedPath = Path.GetFullPath(absolutePath);
            var relative = normalizedPath.Substring(normalizedProjectRoot.Length + 1);
            return relative.Replace(Path.DirectorySeparatorChar, '/');
        }

        private static string GetAssetType(string extension)
        {
            if (extension == ".prefab")
            {
                return "Prefab";
            }

            if (extension == ".fbx")
            {
                return "Model";
            }

            if (extension == ".mat")
            {
                return "Material";
            }

            return "Texture";
        }

        private static int TypeSortKey(string type)
        {
            switch (type)
            {
                case "Prefab":
                    return 0;
                case "Model":
                    return 1;
                case "Material":
                    return 2;
                default:
                    return 3;
            }
        }

        private static string GetPackName(string assetPath)
        {
            if (assetPath.StartsWith("Assets/Heavy Station Kit", StringComparison.OrdinalIgnoreCase))
            {
                return "Heavy Station Kit";
            }

            if (assetPath.StartsWith("Assets/Sci-Fi Styled Modular Pack", StringComparison.OrdinalIgnoreCase))
            {
                return "Sci-Fi Styled Modular Pack";
            }

            if (assetPath.StartsWith("Assets/ScifiOfficeLite", StringComparison.OrdinalIgnoreCase))
            {
                return "Free Sci-Fi Office Pack";
            }

            if (assetPath.StartsWith("Assets/GoldenFrame_Terminal_FREE", StringComparison.OrdinalIgnoreCase))
            {
                return "GoldenFrame Terminal - FREE";
            }

            return "Unknown";
        }

        private static string Classify(string assetPath, string type)
        {
            var lower = assetPath.ToLowerInvariant();
            var fileName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();

            if (type == "Texture")
            {
                if (lower.Contains("normal") || fileName.EndsWith("_n", StringComparison.OrdinalIgnoreCase))
                {
                    return "텍스처/노멀";
                }

                if (lower.Contains("emiss") || lower.Contains("glow"))
                {
                    return "텍스처/발광";
                }

                if (lower.Contains("rough") || lower.Contains("metal") || lower.Contains("spec") || fileName.EndsWith("_s", StringComparison.OrdinalIgnoreCase))
                {
                    return "텍스처/금속·거칠기";
                }

                return "텍스처/색상";
            }

            if (type == "Material")
            {
                return "머티리얼";
            }

            if (ContainsAny(lower, "corridor", "tunnel"))
            {
                return "복도 셸";
            }

            if (ContainsAny(lower, "arch", "arche", "gate", "threshold"))
            {
                return "문틀·게이트";
            }

            if (ContainsAny(lower, "door"))
            {
                return "문";
            }

            if (ContainsAny(lower, "wall", "panel", "blank"))
            {
                return "벽·패널";
            }

            if (ContainsAny(lower, "floor", "ground", "grate", "stairs", "stair"))
            {
                return "바닥·계단";
            }

            if (ContainsAny(lower, "light", "lamp", "emissive"))
            {
                return "조명";
            }

            if (ContainsAny(lower, "display", "monitor", "screen", "terminal", "disp", "computer", "pc"))
            {
                return "터미널·화면";
            }

            if (ContainsAny(lower, "rail", "handrail", "railing"))
            {
                return "난간·손잡이";
            }

            if (ContainsAny(lower, "window", "glass"))
            {
                return "창·유리";
            }

            if (ContainsAny(lower, "pipe", "cylinder", "engine", "beam", "channel", "chan", "joint", "connector"))
            {
                return "기계·구조물";
            }

            if (ContainsAny(lower, "shelf", "table", "chair", "cabinet", "box", "crate", "office", "storage"))
            {
                return "가구·보관";
            }

            if (ContainsAny(lower, "equipment", "/eq", "_eq", "equip", "arm", "weapon"))
            {
                return "장비·소품";
            }

            return "기타";
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (text.Contains(values[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetSuggestedUse(string category, string type, string assetPath)
        {
            if (type == "Texture" || type == "Material")
            {
                return "재질 후보, 색감/마모/발광 확인";
            }

            switch (category)
            {
                case "복도 셸":
                case "문틀·게이트":
                case "문":
                case "벽·패널":
                case "바닥·계단":
                case "난간·손잡이":
                case "조명":
                    return "복도, 화물칸, 방 입구, 공통 선체 마감";
                case "터미널·화면":
                    return "조종실, 통제실, 무기실, 상점/정비 UI 배경 후보";
                case "기계·구조물":
                    return "동력실, 무기실, 화물칸 외곽, 복도 보강재";
                case "가구·보관":
                    return "비품창고, 통제실, 조종실 보조 소품";
                case "창·유리":
                    return "조종실 전면, 관측창, 통제실 화면 주변";
                case "장비·소품":
                    return "무기실, 동력실, 통제실, 화물칸 장식";
                default:
                    return "형태 확인 후 용도 결정";
            }
        }

        private static string GetNotes(string category, string type, string assetPath)
        {
            if (assetPath.IndexOf("Demo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                assetPath.IndexOf("Example", StringComparison.OrdinalIgnoreCase) >= 0 ||
                assetPath.IndexOf("/Scene/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "데모/예시 폴더 출처입니다. 실제 적용 전 재확인 필요.";
            }

            if (type == "Model")
            {
                return "FBX 원본 모델입니다. 같은 형태의 프리팹이 있으면 프리팹 우선 검토.";
            }

            if (type == "Texture")
            {
                return "텍스처 파일입니다. 형태가 아니라 재질 후보로 보십시오.";
            }

            if (type == "Material")
            {
                return "머티리얼 파일입니다. 렌더러 적용 전 셰이더 호환성 확인 필요.";
            }

            return string.Empty;
        }

        private static void CaptureObjectRecord(PreviewRenderContext context, AssetCatalogRecord record, string outputRoot, string relativeRoot, int index)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(record.AssetPath);
            var fileName = MakeThumbnailName(index, record);
            var outputPath = Path.Combine(outputRoot, fileName);
            record.ThumbnailPath = relativeRoot + "/" + fileName;

            if (asset == null)
            {
                WritePlaceholder(outputPath, ObjectPreviewSize, new Color32(55, 43, 38, 255));
                record.Notes = AppendNote(record.Notes, "미리보기 로드 실패.");
                return;
            }

            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                if (instance == null)
                {
                    instance = UnityEngine.Object.Instantiate(asset);
                }

                instance.name = "Catalog Preview " + record.FileName;
                instance.hideFlags = HideFlags.HideAndDontSave;
                SetLayerRecursive(instance.transform, PreviewLayer);
                DisableEmbeddedCamerasAndLights(instance);
                var unsupportedMaterialCount = CountUnsupportedMaterials(instance);
                if (unsupportedMaterialCount > 0)
                {
                    record.Notes = AppendNote(record.Notes, "원본 머티리얼 " + unsupportedMaterialCount.ToString(CultureInfo.InvariantCulture) + "개가 현재 URP 프로젝트에서 미지원입니다. 썸네일은 형태 확인용 중립 재질로 생성했습니다.");
                }

                ApplyObjectPreviewMaterial(instance, context.FallbackMaterial);
                var renderers = instance.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.enabled)
                    .ToArray();
                record.RendererCount = renderers.Length;

                if (renderers.Length == 0)
                {
                    WritePlaceholder(outputPath, ObjectPreviewSize, new Color32(42, 44, 39, 255));
                    record.Notes = AppendNote(record.Notes, "렌더러가 없어 형태 미리보기가 없습니다.");
                    return;
                }

                var bounds = CalculateBounds(renderers);
                record.Bounds = FormatBounds(bounds.size);
                var offset = -bounds.center;
                instance.transform.position += offset;
                bounds.center += offset;
                RenderObject(context, outputPath, bounds, ObjectPreviewSize, ObjectPreviewSize);
            }
            catch (Exception exception)
            {
                WritePlaceholder(outputPath, ObjectPreviewSize, new Color32(65, 36, 36, 255));
                record.Notes = AppendNote(record.Notes, "썸네일 생성 실패: " + exception.Message);
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
        }

        private static void CaptureMaterialRecord(PreviewRenderContext context, AssetCatalogRecord record, string outputRoot, string relativeRoot, int index)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(record.AssetPath);
            var fileName = MakeThumbnailName(index, record);
            var outputPath = Path.Combine(outputRoot, fileName);
            record.ThumbnailPath = relativeRoot + "/" + fileName;
            Material previewMaterial = null;
            var ownsPreviewMaterial = false;

            if (material == null)
            {
                WritePlaceholder(outputPath, TexturePreviewSize, new Color32(55, 43, 38, 255));
                record.Notes = AppendNote(record.Notes, "머티리얼 로드 실패.");
                return;
            }

            GameObject sphere = null;
            try
            {
                if (IsUnsupportedMaterial(material))
                {
                    var baseTexture = TryGetTexture(material, "_BaseColorMap", "_BaseMap", "_MainTex", "_DiffuseMap", "_AlbedoMap");
                    if (baseTexture != null)
                    {
                        CaptureTexturePreview(baseTexture, outputPath, TexturePreviewSize);
                        record.Bounds = baseTexture.width.ToString(CultureInfo.InvariantCulture) + "x" + baseTexture.height.ToString(CultureInfo.InvariantCulture);
                        record.RendererCount = 0;
                        record.Notes = AppendNote(record.Notes, GetMaterialCompatibilityLabel(material) + "이 현재 URP 프로젝트와 맞지 않습니다. 썸네일은 연결된 기본 텍스처로 대체했습니다.");
                        return;
                    }

                    previewMaterial = CreateCompatibleMaterialPreview(material, context.FallbackMaterial);
                    ownsPreviewMaterial = previewMaterial != context.FallbackMaterial;
                    record.Notes = AppendNote(record.Notes, GetMaterialCompatibilityLabel(material) + "이 현재 URP 프로젝트와 맞지 않습니다. 썸네일은 연결된 텍스처를 임시 URP 재질에 복사해 생성했습니다.");
                }
                else
                {
                    previewMaterial = material;
                }

                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Catalog Material Preview";
                sphere.hideFlags = HideFlags.HideAndDontSave;
                SetLayerRecursive(sphere.transform, PreviewLayer);
                sphere.GetComponent<Renderer>().sharedMaterial = previewMaterial;

                var bounds = CalculateBounds(sphere.GetComponentsInChildren<Renderer>(true));
                record.RendererCount = 1;
                record.Bounds = FormatBounds(bounds.size);
                RenderObject(context, outputPath, bounds, TexturePreviewSize, TexturePreviewSize);
            }
            catch (Exception exception)
            {
                WritePlaceholder(outputPath, TexturePreviewSize, new Color32(65, 36, 36, 255));
                record.Notes = AppendNote(record.Notes, "머티리얼 썸네일 실패: " + exception.Message);
            }
            finally
            {
                if (sphere != null)
                {
                    UnityEngine.Object.DestroyImmediate(sphere);
                }

                if (ownsPreviewMaterial && previewMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(previewMaterial);
                }
            }
        }

        private static void CaptureTextureRecord(AssetCatalogRecord record, string outputRoot, string relativeRoot, int index)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(record.AssetPath);
            var fileName = MakeThumbnailName(index, record);
            var outputPath = Path.Combine(outputRoot, fileName);
            record.ThumbnailPath = relativeRoot + "/" + fileName;

            if (texture == null)
            {
                WritePlaceholder(outputPath, TexturePreviewSize, new Color32(55, 43, 38, 255));
                record.Notes = AppendNote(record.Notes, "텍스처 로드 실패.");
                return;
            }

            record.Bounds = texture.width.ToString(CultureInfo.InvariantCulture) + "x" + texture.height.ToString(CultureInfo.InvariantCulture);
            try
            {
                CaptureTexturePreview(texture, outputPath, TexturePreviewSize);
            }
            catch (Exception exception)
            {
                WritePlaceholder(outputPath, TexturePreviewSize, new Color32(65, 36, 36, 255));
                record.Notes = AppendNote(record.Notes, "텍스처 썸네일 실패: " + exception.Message);
            }
        }

        private static string MakeThumbnailName(int index, AssetCatalogRecord record)
        {
            var baseName = Path.GetFileNameWithoutExtension(record.FileName);
            var safe = new StringBuilder();
            for (var i = 0; i < baseName.Length; i++)
            {
                var c = baseName[i];
                safe.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return index.ToString("0000", CultureInfo.InvariantCulture) + "_" +
                record.AssetType.ToLowerInvariant() + "_" +
                safe.ToString().Trim('_') + "_" +
                ComputeHash(record.AssetPath).ToString("x8", CultureInfo.InvariantCulture) +
                ".png";
        }

        private static uint ComputeHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (var i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619;
                }

                return hash;
            }
        }

        private static void RenderObject(PreviewRenderContext context, string outputPath, Bounds bounds, int width, int height)
        {
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (size <= 0.001f)
            {
                size = 1f;
            }

            var center = bounds.center;
            var direction = new Vector3(1.35f, 0.88f, -1.45f).normalized;
            context.Camera.transform.position = center + (direction * size * 3.2f);
            context.Camera.transform.LookAt(center);
            context.Camera.orthographic = true;
            context.Camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.45f;
            context.Camera.nearClipPlane = 0.01f;
            context.Camera.farClipPlane = Mathf.Max(50f, size * 8f);

            CaptureCamera(context.Camera, outputPath, width, height);
        }

        private static void CaptureCamera(Camera camera, string path, int width, int height)
        {
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void CaptureTexturePreview(Texture texture, string outputPath, int size)
        {
            var previousActive = RenderTexture.active;
            var renderTexture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var output = new Texture2D(size, size, TextureFormat.RGB24, false);

            try
            {
                Graphics.Blit(texture, renderTexture);
                RenderTexture.active = renderTexture;
                output.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
                output.Apply();
                File.WriteAllBytes(outputPath, output.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(output);
            }
        }

        private static void WritePlaceholder(string path, int size, Color32 color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var pixels = Enumerable.Repeat(color, size * size).ToArray();
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static Bounds CalculateBounds(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void DisableEmbeddedCamerasAndLights(GameObject instance)
        {
            foreach (var camera in instance.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }

            foreach (var light in instance.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }
        }

        private static int CountUnsupportedMaterials(GameObject instance)
        {
            var count = 0;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (IsUnsupportedMaterial(materials[i]))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void ApplyObjectPreviewMaterial(GameObject instance, Material previewMaterial)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var sharedMaterials = renderer.sharedMaterials;
                for (var i = 0; i < sharedMaterials.Length; i++)
                {
                    sharedMaterials[i] = previewMaterial;
                }

                renderer.sharedMaterials = sharedMaterials;
            }
        }

        private static bool IsUnsupportedMaterial(Material material)
        {
            return material == null ||
                material.shader == null ||
                !material.shader.isSupported ||
                material.shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                material.shader.name.IndexOf("HDRP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                material.shader.name.IndexOf("HDRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0 ||
                IsSerializedHdrpMaterial(material);
        }

        private static bool IsSerializedHdrpMaterial(Material material)
        {
            var assetPath = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            try
            {
                if (!File.Exists(assetPath))
                {
                    return false;
                }

                var text = File.ReadAllText(assetPath);
                return text.IndexOf("_HdrpVersion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("HDRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("HDRP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("DistortionVectors", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    text.IndexOf("_BaseColorMap", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static string GetShaderName(Material material)
        {
            if (material == null || material.shader == null)
            {
                return "Missing Shader";
            }

            return material.shader.name;
        }

        private static string GetMaterialCompatibilityLabel(Material material)
        {
            if (IsSerializedHdrpMaterial(material))
            {
                return "원본 HDRP 계열 머티리얼 설정";
            }

            return "원본 셰이더 `" + GetShaderName(material) + "`";
        }

        private static Material CreateCompatibleMaterialPreview(Material source, Material fallbackMaterial)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard") ??
                Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                return fallbackMaterial;
            }

            var preview = new Material(shader)
            {
                name = source.name + " Catalog URP Preview",
                hideFlags = HideFlags.HideAndDontSave
            };

            var baseColor = TryGetColor(source, "_BaseColor", "_Color", "_DiffuseColor");
            if (baseColor.HasValue)
            {
                ApplyColor(preview, baseColor.Value);
            }
            else
            {
                ApplyColor(preview, Color.white);
            }

            var baseTexture = TryGetTexture(source, "_BaseColorMap", "_BaseMap", "_MainTex", "_DiffuseMap", "_AlbedoMap");
            if (baseTexture != null)
            {
                ApplyTexture(preview, baseTexture, "_BaseMap", "_MainTex");
            }

            var normalTexture = TryGetTexture(source, "_NormalMap", "_BumpMap");
            if (normalTexture != null)
            {
                ApplyTexture(preview, normalTexture, "_BumpMap");
                if (preview.HasProperty("_BumpScale"))
                {
                    preview.SetFloat("_BumpScale", TryGetFloat(source, 1f, "_NormalScale", "_BumpScale"));
                }

                preview.EnableKeyword("_NORMALMAP");
            }

            var metallicTexture = TryGetTexture(source, "_MetallicGlossMap", "_MaskMap");
            if (metallicTexture != null)
            {
                ApplyTexture(preview, metallicTexture, "_MetallicGlossMap");
                preview.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            ApplyFloat(preview, TryGetFloat(source, 0f, "_Metallic"), "_Metallic");
            ApplyFloat(preview, TryGetFloat(source, 0.22f, "_Smoothness", "_Glossiness"), "_Smoothness", "_Glossiness");

            return preview;
        }

        private static Texture TryGetTexture(Material material, params string[] propertyNames)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                var propertyName = propertyNames[i];
                if (!material.HasProperty(propertyName))
                {
                    continue;
                }

                try
                {
                    var texture = material.GetTexture(propertyName);
                    if (texture != null)
                    {
                        return texture;
                    }
                }
                catch (ArgumentException)
                {
                }
            }

            return null;
        }

        private static Color? TryGetColor(Material material, params string[] propertyNames)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                var propertyName = propertyNames[i];
                if (!material.HasProperty(propertyName))
                {
                    continue;
                }

                try
                {
                    return material.GetColor(propertyName);
                }
                catch (ArgumentException)
                {
                }
            }

            return null;
        }

        private static float TryGetFloat(Material material, float fallback, params string[] propertyNames)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                var propertyName = propertyNames[i];
                if (!material.HasProperty(propertyName))
                {
                    continue;
                }

                try
                {
                    return material.GetFloat(propertyName);
                }
                catch (ArgumentException)
                {
                }
            }

            return fallback;
        }

        private static void ApplyColor(Material material, Color color)
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

        private static void ApplyTexture(Material material, Texture texture, params string[] propertyNames)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                if (material.HasProperty(propertyNames[i]))
                {
                    material.SetTexture(propertyNames[i], texture);
                }
            }
        }

        private static void ApplyFloat(Material material, float value, params string[] propertyNames)
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                if (material.HasProperty(propertyNames[i]))
                {
                    material.SetFloat(propertyNames[i], value);
                }
            }
        }

        private static void SetLayerRecursive(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            for (var i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursive(transform.GetChild(i), layer);
            }
        }

        private static string FormatBounds(Vector3 size)
        {
            return size.x.ToString("0.##", CultureInfo.InvariantCulture) + " x " +
                size.y.ToString("0.##", CultureInfo.InvariantCulture) + " x " +
                size.z.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string AppendNote(string current, string note)
        {
            if (string.IsNullOrWhiteSpace(current))
            {
                return note;
            }

            return current + " " + note;
        }

        private static void WriteCsv(List<AssetCatalogRecord> records, string path)
        {
            var builder = new StringBuilder();
            builder.AppendLine("AssetType,Pack,Category,SuggestedUse,FileName,AssetPath,ThumbnailPath,RendererCount,Bounds,Notes");
            foreach (var record in records)
            {
                builder.Append(Csv(record.AssetType)).Append(',')
                    .Append(Csv(record.Pack)).Append(',')
                    .Append(Csv(record.Category)).Append(',')
                    .Append(Csv(record.SuggestedUse)).Append(',')
                    .Append(Csv(record.FileName)).Append(',')
                    .Append(Csv(record.AssetPath)).Append(',')
                    .Append(Csv(record.ThumbnailPath)).Append(',')
                    .Append(Csv(record.RendererCount.ToString(CultureInfo.InvariantCulture))).Append(',')
                    .Append(Csv(record.Bounds)).Append(',')
                    .Append(Csv(record.Notes)).AppendLine();
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static void WriteJson(List<AssetCatalogRecord> records, string path)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[");
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                builder.AppendLine("  {");
                AppendJson(builder, "assetType", record.AssetType, true);
                AppendJson(builder, "pack", record.Pack, true);
                AppendJson(builder, "category", record.Category, true);
                AppendJson(builder, "suggestedUse", record.SuggestedUse, true);
                AppendJson(builder, "fileName", record.FileName, true);
                AppendJson(builder, "assetPath", record.AssetPath, true);
                AppendJson(builder, "thumbnailPath", record.ThumbnailPath, true);
                builder.Append("    \"rendererCount\": ").Append(record.RendererCount.ToString(CultureInfo.InvariantCulture)).AppendLine(",");
                AppendJson(builder, "bounds", record.Bounds, true);
                AppendJson(builder, "notes", record.Notes, false);
                builder.Append("  }");
                if (i < records.Count - 1)
                {
                    builder.Append(',');
                }

                builder.AppendLine();
            }

            builder.AppendLine("]");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static void AppendJson(StringBuilder builder, string name, string value, bool comma)
        {
            builder.Append("    \"").Append(JsonEscape(name)).Append("\": \"").Append(JsonEscape(value ?? string.Empty)).Append("\"");
            if (comma)
            {
                builder.Append(',');
            }

            builder.AppendLine();
        }

        private static string JsonEscape(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static void WriteHtml(List<AssetCatalogRecord> records, string path)
        {
            var prefabCount = records.Count(record => record.AssetType == "Prefab");
            var modelCount = records.Count(record => record.AssetType == "Model");
            var materialCount = records.Count(record => record.AssetType == "Material");
            var textureCount = records.Count(record => record.AssetType == "Texture");
            var packs = records.Select(record => record.Pack).Distinct().OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();
            var categories = records.Select(record => record.Category).Distinct().OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList();

            var builder = new StringBuilder();
            builder.AppendLine("<!doctype html>");
            builder.AppendLine("<html lang=\"ko\">");
            builder.AppendLine("<head>");
            builder.AppendLine("  <meta charset=\"utf-8\">");
            builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            builder.AppendLine("  <title>Bellerophon Asset Inventory Catalog</title>");
            builder.AppendLine("  <style>");
            builder.AppendLine("    :root{--bg:#151612;--panel:#20221b;--panel2:#292b22;--text:#ece7da;--muted:#aaa38e;--line:#4a4a3d;--amber:#d9a441;--green:#8ea65b;--rust:#a65c36;--shadow:rgba(0,0,0,.35)}");
            builder.AppendLine("    *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font-family:Georgia,'Times New Roman',serif;font-size:15px;line-height:1.45}");
            builder.AppendLine("    header{position:sticky;top:0;z-index:10;background:linear-gradient(180deg,#1c1d18 0%,#151612 100%);border-bottom:1px solid var(--line);box-shadow:0 10px 24px var(--shadow)}");
            builder.AppendLine("    .wrap{max-width:1480px;margin:0 auto;padding:18px 22px}.title{display:flex;gap:18px;align-items:flex-end;justify-content:space-between;flex-wrap:wrap}.title h1{font-size:28px;margin:0;letter-spacing:0;font-weight:700}.title p{margin:4px 0 0;color:var(--muted)}");
            builder.AppendLine("    .stats{display:grid;grid-template-columns:repeat(5,minmax(120px,1fr));gap:10px;margin-top:14px}.stat{background:var(--panel);border:1px solid var(--line);border-radius:6px;padding:10px 12px}.stat b{display:block;font-size:22px;color:var(--amber)}.stat span{color:var(--muted);font-size:13px}");
            builder.AppendLine("    .toolbar{display:grid;grid-template-columns:minmax(220px,1.4fr) repeat(3,minmax(160px,.55fr));gap:10px;margin-top:14px}.toolbar input,.toolbar select{width:100%;border:1px solid var(--line);background:#11120f;color:var(--text);border-radius:5px;padding:10px 11px;font:inherit}");
            builder.AppendLine("    main{max-width:1480px;margin:0 auto;padding:18px 22px 40px}.hint{margin:0 0 12px;color:var(--muted)}.count{color:var(--amber);font-weight:700}");
            builder.AppendLine("    table{width:100%;border-collapse:separate;border-spacing:0 8px}thead th{position:sticky;top:176px;background:#151612;color:var(--muted);font-size:12px;text-align:left;text-transform:uppercase;letter-spacing:.08em;padding:8px;z-index:5}");
            builder.AppendLine("    tbody tr{background:var(--panel);box-shadow:0 5px 18px var(--shadow)}tbody tr[hidden]{display:none}td{border-top:1px solid var(--line);border-bottom:1px solid var(--line);padding:10px;vertical-align:middle}td:first-child{border-left:1px solid var(--line);border-radius:6px 0 0 6px;width:150px}td:last-child{border-right:1px solid var(--line);border-radius:0 6px 6px 0}");
            builder.AppendLine("    .thumb{width:128px;height:128px;display:block;object-fit:contain;background:#0e0f0c;border:1px solid #37382e;border-radius:4px}.file strong{display:block;font-size:16px}.file code{display:block;max-width:560px;color:var(--muted);font-family:Consolas,'Courier New',monospace;font-size:12px;white-space:normal;word-break:break-all;margin-top:4px}.tag{display:inline-block;border:1px solid var(--line);border-radius:4px;padding:3px 7px;background:var(--panel2);color:var(--text);font-size:13px}.use{max-width:260px;color:#d8d0bc}.notes{max-width:280px;color:var(--muted);font-size:13px}.metric{color:#cfc7af;font-size:13px;white-space:nowrap}");
            builder.AppendLine("    @media (max-width:900px){.stats{grid-template-columns:repeat(2,1fr)}.toolbar{grid-template-columns:1fr}thead{display:none}table,tbody,tr,td{display:block}tbody tr{margin-bottom:12px}td:first-child,td:last-child,td{border:1px solid var(--line);border-radius:0;width:auto}.thumb{width:100%;height:220px}.file code{max-width:none}}");
            builder.AppendLine("  </style>");
            builder.AppendLine("</head>");
            builder.AppendLine("<body>");
            builder.AppendLine("  <header>");
            builder.AppendLine("    <div class=\"wrap\">");
            builder.AppendLine("      <div class=\"title\"><div><h1>에셋 이미지 분류표</h1><p>프리팹, FBX, 머티리얼, 텍스처를 썸네일과 파일명 중심으로 정리했습니다. 3D 객체 썸네일은 형태 식별용 중립 재질입니다. URP와 맞지 않는 머티리얼은 연결된 텍스처를 임시 URP 미리보기 재질에 복사해 표시합니다. 이 표는 선택용 조사 자료이며 Unity 런타임 씬에는 적용하지 않았습니다.</p></div><div class=\"count\" id=\"visibleCount\"></div></div>");
            builder.AppendLine("      <div class=\"stats\">");
            builder.AppendLine("        <div class=\"stat\"><b>" + records.Count.ToString(CultureInfo.InvariantCulture) + "</b><span>전체 파일</span></div>");
            builder.AppendLine("        <div class=\"stat\"><b>" + prefabCount.ToString(CultureInfo.InvariantCulture) + "</b><span>프리팹</span></div>");
            builder.AppendLine("        <div class=\"stat\"><b>" + modelCount.ToString(CultureInfo.InvariantCulture) + "</b><span>FBX 모델</span></div>");
            builder.AppendLine("        <div class=\"stat\"><b>" + materialCount.ToString(CultureInfo.InvariantCulture) + "</b><span>머티리얼</span></div>");
            builder.AppendLine("        <div class=\"stat\"><b>" + textureCount.ToString(CultureInfo.InvariantCulture) + "</b><span>텍스처</span></div>");
            builder.AppendLine("      </div>");
            builder.AppendLine("      <div class=\"toolbar\">");
            builder.AppendLine("        <input id=\"search\" type=\"search\" placeholder=\"파일명, 경로, 분류, 추천 용도로 검색\">");
            builder.AppendLine("        <select id=\"typeFilter\"><option value=\"\">전체 유형</option><option>Prefab</option><option>Model</option><option>Material</option><option>Texture</option></select>");
            builder.AppendLine("        <select id=\"packFilter\"><option value=\"\">전체 에셋 팩</option>");
            foreach (var pack in packs)
            {
                builder.AppendLine("          <option>" + Html(pack) + "</option>");
            }

            builder.AppendLine("        </select>");
            builder.AppendLine("        <select id=\"categoryFilter\"><option value=\"\">전체 분류</option>");
            foreach (var category in categories)
            {
                builder.AppendLine("          <option>" + Html(category) + "</option>");
            }

            builder.AppendLine("        </select>");
            builder.AppendLine("      </div>");
            builder.AppendLine("    </div>");
            builder.AppendLine("  </header>");
            builder.AppendLine("  <main>");
            builder.AppendLine("    <p class=\"hint\">표의 왼쪽 이미지를 누르면 썸네일 원본 PNG가 열립니다. 실제 적용 전에는 여기서 고른 후보로 별도 `artSample` 배치 시안을 다시 만들어 승인받아야 합니다.</p>");
            builder.AppendLine("    <table>");
            builder.AppendLine("      <thead><tr><th>이미지</th><th>파일명 / 경로</th><th>유형</th><th>분류</th><th>추천 사용처</th><th>크기/렌더러</th><th>비고</th></tr></thead>");
            builder.AppendLine("      <tbody id=\"rows\">");
            foreach (var record in records)
            {
                var searchText = (record.FileName + " " + record.AssetPath + " " + record.Pack + " " + record.Category + " " + record.SuggestedUse + " " + record.Notes).ToLowerInvariant();
                builder.Append("        <tr data-type=\"").Append(Html(record.AssetType)).Append("\" data-pack=\"").Append(Html(record.Pack)).Append("\" data-category=\"").Append(Html(record.Category)).Append("\" data-search=\"").Append(Html(searchText)).AppendLine("\">");
                builder.Append("          <td><a href=\"").Append(Html(record.ThumbnailPath)).Append("\"><img class=\"thumb\" loading=\"lazy\" src=\"").Append(Html(record.ThumbnailPath)).Append("\" alt=\"").Append(Html(record.FileName)).AppendLine("\"></a></td>");
                builder.Append("          <td class=\"file\"><strong>").Append(Html(record.FileName)).Append("</strong><code>").Append(Html(record.AssetPath)).AppendLine("</code></td>");
                builder.Append("          <td><span class=\"tag\">").Append(Html(record.AssetType)).AppendLine("</span></td>");
                builder.Append("          <td><span class=\"tag\">").Append(Html(record.Category)).AppendLine("</span></td>");
                builder.Append("          <td class=\"use\">").Append(Html(record.SuggestedUse)).AppendLine("</td>");
                builder.Append("          <td class=\"metric\">").Append(Html(record.Bounds)).Append("<br>Renderers: ").Append(record.RendererCount.ToString(CultureInfo.InvariantCulture)).AppendLine("</td>");
                builder.Append("          <td class=\"notes\">").Append(Html(record.Notes)).AppendLine("</td>");
                builder.AppendLine("        </tr>");
            }

            builder.AppendLine("      </tbody>");
            builder.AppendLine("    </table>");
            builder.AppendLine("  </main>");
            builder.AppendLine("  <script>");
            builder.AppendLine("    const rows=[...document.querySelectorAll('#rows tr')];");
            builder.AppendLine("    const visibleCount=document.getElementById('visibleCount');");
            builder.AppendLine("    const controls=['search','typeFilter','packFilter','categoryFilter'].map(id=>document.getElementById(id));");
            builder.AppendLine("    function applyFilters(){const q=document.getElementById('search').value.trim().toLowerCase();const type=document.getElementById('typeFilter').value;const pack=document.getElementById('packFilter').value;const category=document.getElementById('categoryFilter').value;let visible=0;for(const row of rows){const show=(!q||row.dataset.search.includes(q))&&(!type||row.dataset.type===type)&&(!pack||row.dataset.pack===pack)&&(!category||row.dataset.category===category);row.hidden=!show;if(show)visible++;}visibleCount.textContent=visible+'개 표시';}");
            builder.AppendLine("    controls.forEach(control=>control.addEventListener('input',applyFilters));applyFilters();");
            builder.AppendLine("  </script>");
            builder.AppendLine("</body>");
            builder.AppendLine("</html>");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static string Html(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static void WriteReadme(List<AssetCatalogRecord> records, string path)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# 에셋 이미지 분류표");
            builder.AppendLine();
            builder.AppendLine("이 폴더는 임포트된 네 개 에셋 팩을 사용자가 직접 고르기 쉽도록 정리한 조사 자료입니다.");
            builder.AppendLine("실제 `CargoRunMvp` 씬, 프리팹, 런타임 자산에는 아무 것도 적용하지 않았습니다.");
            builder.AppendLine();
            builder.AppendLine("## 보기");
            builder.AppendLine();
            builder.AppendLine("- `index.html`: 이미지 - 파일명 중심의 필터 가능한 분류표");
            builder.AppendLine("- `asset_catalog.csv`: 스프레드시트용 분류표");
            builder.AppendLine("- `asset_catalog.json`: 후속 자동화용 원본 데이터");
            builder.AppendLine("- `thumbnails/`: 썸네일 이미지");
            builder.AppendLine();
            builder.AppendLine("3D 프리팹과 FBX 썸네일은 셰이더 호환 문제를 피하고 형태를 비교하기 쉽도록 중립 재질로 렌더링했습니다.");
            builder.AppendLine("URP와 맞지 않는 머티리얼은 원본 머티리얼 파일을 수정하지 않고, 연결된 텍스처를 임시 URP 미리보기 재질에 복사해 썸네일만 생성했습니다.");
            builder.AppendLine("실제 적용 단계에서는 선택된 에셋에 대해 프로젝트 소유 머티리얼/프리팹 변형을 따로 만들어야 합니다.");
            builder.AppendLine();
            builder.AppendLine("## 포함 범위");
            builder.AppendLine();
            foreach (var group in records.GroupBy(record => record.AssetType).OrderBy(group => TypeSortKey(group.Key)))
            {
                builder.AppendLine("- " + group.Key + ": " + group.Count().ToString(CultureInfo.InvariantCulture));
            }

            builder.AppendLine();
            builder.AppendLine("## 사용 기준");
            builder.AppendLine();
            builder.AppendLine("여기에서 후보를 고른 뒤, 해당 후보만 사용해 별도의 `artSample/asset_dressing_samples/` 배치 시안을 만들어야 합니다.");
            builder.AppendLine("사용자 승인 전에는 새 에셋을 실제 Unity 런타임 씬에 적용하지 않습니다.");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static void WriteApprovalStatus(string path)
        {
            var json = "{\n" +
                "  \"sampleName\": \"Asset inventory catalog\",\n" +
                "  \"createdDate\": \"2026-06-14\",\n" +
                "  \"approvalState\": \"selection-catalog-only\",\n" +
                "  \"unityApplicationAllowed\": false,\n" +
                "  \"runtimeSceneModified\": false,\n" +
                "  \"reviewable\": true,\n" +
                "  \"nextStep\": \"User selects candidate asset files, then a separate artSample placement proposal is generated for approval.\"\n" +
                "}\n";
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        private sealed class AssetCatalogRecord
        {
            public string AssetPath;
            public string FileName;
            public string AssetType;
            public string Pack;
            public string Category;
            public string SuggestedUse;
            public string ThumbnailPath;
            public string Bounds = string.Empty;
            public string Notes = string.Empty;
            public int RendererCount;
        }

        private sealed class PreviewRenderContext : IDisposable
        {
            public Camera Camera { get; private set; }
            public Material FallbackMaterial { get; private set; }
            private GameObject root;

            public static PreviewRenderContext Create()
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.3f, 1f);
                RenderSettings.fog = false;

                var context = new PreviewRenderContext();
                context.root = new GameObject("Asset Inventory Catalog Preview Context")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                var cameraObject = new GameObject("Catalog Preview Camera")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                cameraObject.transform.SetParent(context.root.transform, false);
                context.Camera = cameraObject.AddComponent<Camera>();
                context.Camera.clearFlags = CameraClearFlags.SolidColor;
                context.Camera.backgroundColor = new Color(0.055f, 0.058f, 0.048f, 1f);
                context.Camera.cullingMask = 1 << PreviewLayer;
                context.Camera.allowHDR = false;
                context.Camera.allowMSAA = true;

                CreateLight(context.root.transform, "Catalog Key Light", new Vector3(-4f, 6f, -5f), new Color(1f, 0.92f, 0.78f, 1f), 1.15f);
                CreateLight(context.root.transform, "Catalog Fill Light", new Vector3(4f, 3f, 2f), new Color(0.54f, 0.67f, 0.58f, 1f), 0.55f);
                CreateLight(context.root.transform, "Catalog Rim Light", new Vector3(2f, 5f, 5f), new Color(0.78f, 0.82f, 0.72f, 1f), 0.45f);

                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                context.FallbackMaterial = new Material(shader)
                {
                    name = "Catalog Neutral Fallback Material",
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (context.FallbackMaterial.HasProperty("_BaseColor"))
                {
                    context.FallbackMaterial.SetColor("_BaseColor", new Color(0.36f, 0.37f, 0.32f, 1f));
                }

                if (context.FallbackMaterial.HasProperty("_Color"))
                {
                    context.FallbackMaterial.SetColor("_Color", new Color(0.36f, 0.37f, 0.32f, 1f));
                }

                if (context.FallbackMaterial.HasProperty("_Metallic"))
                {
                    context.FallbackMaterial.SetFloat("_Metallic", 0.15f);
                }

                if (context.FallbackMaterial.HasProperty("_Smoothness"))
                {
                    context.FallbackMaterial.SetFloat("_Smoothness", 0.18f);
                }

                return context;
            }

            public void Dispose()
            {
                if (FallbackMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(FallbackMaterial);
                }

                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            private static void CreateLight(Transform parent, string name, Vector3 position, Color color, float intensity)
            {
                var lightObject = new GameObject(name)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.position = position;
                lightObject.transform.LookAt(Vector3.zero);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = color;
                light.intensity = intensity;
                light.cullingMask = 1 << PreviewLayer;
            }
        }
    }
}
