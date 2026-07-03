using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.LongaArmaCargoRunScene
{
    internal static class LongaArmaLowPolyUnityApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string PlacementObjectName = "LongaArma_00_Static_Review";
        private const string ModelChildName = "LongaArmaLowPolyFromOriginal_Model";
        private const string ReviewCameraName = "Model Cam";
        private const string ReviewLightName = "LongaArma_LowPoly_Review_KeyLight";

        private const string SampleRootRelativePath = "artSample/enemies/longa_arma/lowpoly_from_original";
        private const string SourceModelRelativePath = SampleRootRelativePath + "/exports/longa_arma_lowpoly_from_original.fbx";
        private const string SourceTextureRootRelativePath = SampleRootRelativePath + "/textures";
        private const string ApprovedBladeSideRenderRelativePath = SampleRootRelativePath + "/renders/02_blade_side_neg_x.png";
        private const string ReviewOutputRelativePath = "docs/validation/longa_arma_lowpoly_from_original";

        private const string LongaArtRoot = "Assets/_Project/Art/Enemies/LongaArma";
        private const string UnityModelFolder = LongaArtRoot + "/Models";
        private const string UnityMaterialFolder = LongaArtRoot + "/Materials";
        private const string UnityTextureFolder = LongaArtRoot + "/Textures";
        private const string UnityModelAssetPath = UnityModelFolder + "/longa_arma_lowpoly_from_original.fbx";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Enemies/LongaArma";
        private const string PrefabPath = PrefabFolder + "/LongaArmaLowPolyFromOriginal.prefab";

        private const string BodyTextureName = "longa_lowpoly_body_mottled_green.png";
        private const string BladeTextureName = "longa_lowpoly_dark_scratched_blade.png";
        private const string SlimeTextureName = "longa_lowpoly_glossy_slime_drips.png";

        private const int MaximumExpectedTriangles = 15000;
        private const int VisualCaptureWidth = 1280;
        private const int VisualCaptureHeight = 720;
        private const int SideBySideGapPixels = 24;
        private static readonly Vector3 FallbackPlacementPosition = new(57.85f, 2.20f, -37.97f);

        [MenuItem("Bellerophon/Enemies/Longa Arma/Apply Low-Poly From Original To CargoRunMvp")]
        public static void ApplyLowPolyFromOriginalToCurrentCargoRunScene()
        {
            RequireSampleFiles();
            EnsureUnityFolders();
            CopySampleAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureImportedAssets();

            var materialSet = EnsureMaterials();
            var prefab = EnsurePrefab(materialSet);

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlaceStaticReviewObject(prefab, scene);
            ConfigureReviewLighting(placementRoot);
            ConfigureReviewCamera(placementRoot);
            InspectSceneState(placementRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Longa Arma low-poly-from-original sample applied to CargoRunMvp scene.");
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
            Debug.Log("Longa Arma low-poly-from-original CargoRunMvp scene state inspected.");
        }

        public static void CaptureUnityVisualComparison()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRootObject = GameObject.Find(PlacementRootName);
            if (placementRootObject == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            var placementRoot = placementRootObject.transform;
            InspectSceneState(placementRoot);
            ConfigureReviewCamera(placementRoot);
            ConfigureReviewLighting(placementRoot);

            var camera = FindReviewCamera();
            if (camera == null)
            {
                throw new InvalidOperationException("Longa Arma low-poly review camera is missing.");
            }

            var outputRoot = ProjectPath(ReviewOutputRelativePath);
            Directory.CreateDirectory(outputRoot);
            var unityCapturePath = Path.Combine(outputRoot, "longa_arma_lowpoly_unity_model_cam.png");
            var comparisonPath = Path.Combine(outputRoot, "longa_arma_lowpoly_blade_side_vs_unity.png");
            var readmePath = Path.Combine(outputRoot, "README.md");
            var approvedRenderPath = ProjectPath(ApprovedBladeSideRenderRelativePath);

            if (!File.Exists(approvedRenderPath))
            {
                throw new FileNotFoundException("Approved low-poly Longa Arma blade-side render is missing.", approvedRenderPath);
            }

            CaptureCamera(camera, unityCapturePath, VisualCaptureWidth, VisualCaptureHeight);
            BuildSideBySideImage(approvedRenderPath, unityCapturePath, comparisonPath);
            File.WriteAllText(
                readmePath,
                "# Longa Arma Low-Poly From Original Unity Review\n\n" +
                $"- Created at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"- Scene: `{CargoRunScenePath}`\n" +
                $"- Placement root: `{PlacementRootName}`\n" +
                $"- Unity prefab: `{PrefabPath}`\n" +
                $"- Approved sample render: `{ApprovedBladeSideRenderRelativePath}`\n" +
                "- Unity capture: `longa_arma_lowpoly_unity_model_cam.png`\n" +
                "- Side-by-side comparison: `longa_arma_lowpoly_blade_side_vs_unity.png`\n" +
                "- Scope: static model replacement only; rigging, animation, AI, and combat wiring are intentionally not included.\n");

            Debug.Log("Longa Arma low-poly-from-original Unity visual comparison captured.");
        }

        private static void RequireSampleFiles()
        {
            RequireFile(SourceModelRelativePath);
            RequireFile(ApprovedBladeSideRenderRelativePath);
            RequireFile(SourceTextureRootRelativePath + "/" + BodyTextureName);
            RequireFile(SourceTextureRootRelativePath + "/" + BladeTextureName);
            RequireFile(SourceTextureRootRelativePath + "/" + SlimeTextureName);
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(LongaArtRoot);
            EnsureUnityFolder(UnityModelFolder);
            EnsureUnityFolder(UnityMaterialFolder);
            EnsureUnityFolder(UnityTextureFolder);
            EnsureUnityFolder("Assets/_Project/Prefabs/Enemies");
            EnsureUnityFolder(PrefabFolder);
        }

        private static void CopySampleAssets()
        {
            CopyFileToAsset(ProjectPath(SourceModelRelativePath), UnityModelAssetPath);
            CopyFileToAsset(ProjectPath(SourceTextureRootRelativePath + "/" + BodyTextureName), UnityTextureFolder + "/" + BodyTextureName);
            CopyFileToAsset(ProjectPath(SourceTextureRootRelativePath + "/" + BladeTextureName), UnityTextureFolder + "/" + BladeTextureName);
            CopyFileToAsset(ProjectPath(SourceTextureRootRelativePath + "/" + SlimeTextureName), UnityTextureFolder + "/" + SlimeTextureName);
        }

        private static void ConfigureImportedAssets()
        {
            var modelImporter = AssetImporter.GetAtPath(UnityModelAssetPath) as ModelImporter;
            if (modelImporter != null)
            {
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.importAnimation = false;
                modelImporter.importBlendShapes = false;
                modelImporter.importVisibility = false;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                modelImporter.importNormals = ModelImporterNormals.Import;
                modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
                modelImporter.globalScale = 1f;
                modelImporter.SaveAndReimport();
            }

            ConfigureTexture(BodyTextureName, true);
            ConfigureTexture(BladeTextureName, true);
            ConfigureTexture(SlimeTextureName, true);
        }

        private static void ConfigureTexture(string fileName, bool srgb)
        {
            var importer = AssetImporter.GetAtPath(UnityTextureFolder + "/" + fileName) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.SaveAndReimport();
        }

        private static MaterialSet EnsureMaterials()
        {
            var bodyTexture = LoadTexture(BodyTextureName);
            var bladeTexture = LoadTexture(BladeTextureName);
            var slimeTexture = LoadTexture(SlimeTextureName);

            return new MaterialSet(
                EnsureMaterial("M_LongaLowPoly_WetMottledBody", bodyTexture, new Color(0.42f, 0.58f, 0.39f, 1f), 0.58f, 0.0f, false),
                EnsureMaterial("M_LongaLowPoly_DarkCrescentBlade", bladeTexture, new Color(0.20f, 0.24f, 0.24f, 1f), 0.46f, 0.08f, false),
                EnsureMaterial("M_LongaLowPoly_GlossySlimeDrips", slimeTexture, new Color(0.65f, 0.88f, 0.70f, 0.78f), 0.88f, 0.0f, true));
        }

        private static GameObject EnsurePrefab(MaterialSet materialSet)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Could not load Longa Arma low-poly model asset at {UnityModelAssetPath}.");
            }

            var root = new GameObject("LongaArmaLowPolyFromOriginal");
            try
            {
                var modelInstance = UnityEngine.Object.Instantiate(modelAsset);
                modelInstance.name = ModelChildName;
                modelInstance.transform.SetParent(root.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
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
                throw new InvalidOperationException($"Could not create Longa Arma low-poly prefab at {PrefabPath}.");
            }

            return prefab;
        }

        private static Transform PlaceStaticReviewObject(GameObject prefab, Scene scene)
        {
            var placementRootObject = GameObject.Find(PlacementRootName);
            if (placementRootObject == null)
            {
                placementRootObject = new GameObject(PlacementRootName);
                SceneManager.MoveGameObjectToScene(placementRootObject, scene);
                placementRootObject.transform.position = FallbackPlacementPosition;
                placementRootObject.transform.rotation = Quaternion.identity;
                placementRootObject.transform.localScale = Vector3.one;
            }

            var placementRoot = placementRootObject.transform;
            ClearPlacementChildren(placementRoot);

            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Could not instantiate Longa Arma low-poly prefab at {PrefabPath}.");
            }

            instance.name = PlacementObjectName;
            instance.transform.SetParent(placementRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            instance.transform.localScale = Vector3.one;
            EnsureScenePhysics(instance.transform);
            EditorUtility.SetDirty(instance);
            EditorUtility.SetDirty(placementRootObject);
            return placementRoot;
        }

        private static void ClearPlacementChildren(Transform placementRoot)
        {
            for (var i = placementRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(placementRoot.GetChild(i).gameObject);
            }
        }

        private static void ConfigureReviewLighting(Transform placementRoot)
        {
            var existing = placementRoot.Find(ReviewLightName);
            var lightObject = existing != null ? existing.gameObject : new GameObject(ReviewLightName);
            lightObject.transform.SetParent(placementRoot, false);

            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var lightPosition = bounds.center + new Vector3(-2.4f, 2.2f, -1.6f);
            lightObject.transform.position = lightPosition;
            lightObject.transform.rotation = Quaternion.LookRotation((bounds.center - lightPosition).normalized, Vector3.up);

            var light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Spot;
            light.range = 9f;
            light.intensity = 5.8f;
            light.spotAngle = 68f;
            light.innerSpotAngle = 42f;
            light.shadows = LightShadows.Soft;
            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(lightObject.transform);
        }

        private static void ConfigureReviewCamera(Transform placementRoot)
        {
            var bounds = CalculateRendererBounds(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var camera = FindOrCreateReviewCamera();
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.05f, 0.20f);
            var distance = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 3.4f, 3.2f, 7.5f);
            var position = lookAt + new Vector3(-distance, Mathf.Clamp(bounds.extents.y * 0.26f, 0.15f, 0.45f), -distance * 0.08f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = distance + Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) + 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.12f, 1f);
            camera.orthographic = false;
            camera.fieldOfView = 34f;
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(camera.transform);
        }

        private static void InspectSceneState(Transform placementRoot)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Longa Arma low-poly model asset is missing: {UnityModelAssetPath}");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Longa Arma low-poly prefab is missing: {PrefabPath}");
            }

            if (placementRoot.childCount != 2)
            {
                throw new InvalidOperationException($"{PlacementRootName} must contain one static review object and one review light. Current child count: {placementRoot.childCount}");
            }

            var staticReview = placementRoot.Find(PlacementObjectName);
            if (staticReview == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            if (staticReview.Find(ModelChildName) == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} must contain {ModelChildName}.");
            }

            if (staticReview.GetComponentsInChildren<Animator>(true).Length > 0 ||
                staticReview.GetComponentsInChildren<Animation>(true).Length > 0)
            {
                throw new InvalidOperationException("Static low-poly Longa Arma replacement must not contain Animator or Animation components.");
            }

            var renderers = staticReview.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Static low-poly Longa Arma replacement has no renderers.");
            }

            RequireMaterialAssigned(renderers, "M_LongaLowPoly_WetMottledBody");
            RequireMaterialAssigned(renderers, "M_LongaLowPoly_DarkCrescentBlade");
            RequireMaterialAssigned(renderers, "M_LongaLowPoly_GlossySlimeDrips");

            var meshStats = CalculateMeshStats(staticReview);
            if (meshStats.Triangles <= 0)
            {
                throw new InvalidOperationException("Static low-poly Longa Arma replacement has no mesh triangles.");
            }

            if (meshStats.Triangles > MaximumExpectedTriangles)
            {
                throw new InvalidOperationException($"Static low-poly Longa Arma triangle count is too high: {meshStats.Triangles}");
            }

            if (staticReview.GetComponent<Rigidbody>() == null || staticReview.GetComponent<BoxCollider>() == null)
            {
                throw new InvalidOperationException("Static low-poly Longa Arma replacement must have Rigidbody and BoxCollider on its root.");
            }

            Debug.Log(
                $"Longa Arma low-poly scene inspection passed. Renderers={renderers.Length}, " +
                $"Vertices={meshStats.Vertices}, Triangles={meshStats.Triangles}, Children={placementRoot.childCount}");
        }

        private static void RequireMaterialAssigned(Renderer[] renderers, string materialName)
        {
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null && string.Equals(material.name, materialName, StringComparison.Ordinal))
                    {
                        return;
                    }
                }
            }

            throw new InvalidOperationException($"Material is not assigned to Longa Arma low-poly renderer: {materialName}");
        }

        private static MeshStats CalculateMeshStats(Transform root)
        {
            return CalculateMeshStats(root.gameObject);
        }

        private static MeshStats CalculateMeshStats(GameObject root)
        {
            var stats = new MeshStats();
            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                stats.Vertices += mesh.vertexCount;
                stats.Triangles += mesh.triangles.Length / 3;
            }

            foreach (var skinnedRenderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skinnedRenderer.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                stats.Vertices += mesh.vertexCount;
                stats.Triangles += mesh.triangles.Length / 3;
            }

            return stats;
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
            if (sourceName.Contains("slime", StringComparison.Ordinal) ||
                sourceName.Contains("drip", StringComparison.Ordinal) ||
                sourceName.Contains("gloss", StringComparison.Ordinal))
            {
                return materialSet.Slime;
            }

            if (sourceName.Contains("blade", StringComparison.Ordinal) ||
                sourceName.Contains("edge", StringComparison.Ordinal) ||
                sourceName.Contains("metal", StringComparison.Ordinal))
            {
                return materialSet.Blade;
            }

            if (materialCount >= 3 && materialIndex == 2)
            {
                return materialSet.Slime;
            }

            if (materialCount >= 2 && materialIndex == 1)
            {
                return materialSet.Blade;
            }

            return materialSet.Body;
        }

        private static void EnsurePrefabPhysics(GameObject root)
        {
            EnsureScenePhysics(root.transform);
        }

        private static void EnsureScenePhysics(Transform root)
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
            Color fallbackColor,
            float smoothness,
            float metallic,
            bool transparent)
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

            if (transparent)
            {
                ConfigureTransparentMaterial(material);
            }
            else
            {
                ConfigureOpaqueMaterial(material);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureOpaqueMaterial(Material material)
        {
            SetMaterialFloat(material, "_Surface", 0f);
            SetMaterialFloat(material, "_Mode", 0f);
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            SetMaterialFloat(material, "_Surface", 1f);
            SetMaterialFloat(material, "_Mode", 3f);
            SetMaterialFloat(material, "_AlphaClip", 0f);
            SetMaterialFloat(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            SetMaterialFloat(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            SetMaterialFloat(material, "_ZWrite", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
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
                throw new InvalidOperationException("Longa Arma low-poly Unity camera capture appears blank or nearly uniform.");
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

        private static void RequireFile(string relativePath)
        {
            var fullPath = ProjectPath(relativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Required Longa Arma low-poly sample file is missing.", fullPath);
            }
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

        private static float SafeDivide(float value, float divisor)
        {
            return divisor > 0.0001f ? value / divisor : value;
        }

        private static string ProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private readonly struct MaterialSet
        {
            public MaterialSet(Material body, Material blade, Material slime)
            {
                Body = body;
                Blade = blade;
                Slime = slime;
            }

            public Material Body { get; }
            public Material Blade { get; }
            public Material Slime { get; }
        }

        private struct MeshStats
        {
            public int Vertices;
            public int Triangles;
        }
    }
}
