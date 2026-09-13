using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class LightsaberBladeUnitySampleTools
    {
        private const string GameplayScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string HandleModelPath =
            "Assets/_Project/Art/Items/Lightsaber/Lightsaber.fbx";
        private const string ConceptImageRelativePath =
            "artSample/lightsaber_blade/LightsaberBlade_ArtSample.png";
        private const string FinalImageRelativePath =
            "artSample/lightsaber_blade/LightsaberBlade_UnitySample.png";
        private const string SampleRoot =
            "Assets/_Project/ArtSamples/LightsaberBlade";
        private const string ShaderPath = SampleRoot + "/LightsaberBladeAdditive.shader";
        private const string CoreMeshPath = SampleRoot + "/LightsaberBladeCoreMesh.asset";
        private const string GlowMeshPath = SampleRoot + "/LightsaberBladeGlowMesh.asset";
        private const string CoreMaterialPath = SampleRoot + "/LightsaberBladeCore.mat";
        private const string GlowMaterialPath = SampleRoot + "/LightsaberBladeGlow.mat";
        private const string PrefabPath = SampleRoot + "/LightsaberBladeSample.prefab";
        private const string ScenePath = SampleRoot + "/LightsaberBladeSample.unity";
        private const string ShaderName =
            "Bellerophon/ArtSamples/LightsaberBladeAdditive";
        private const int SampleLayer = 30;
        private const float TotalLengthMeters = 0.9f;
        private const float HandleModelLengthMeters = 0.273845215f;
        private const float BladeLengthMeters =
            TotalLengthMeters - HandleModelLengthMeters;
        private const float GlowDiameterMeters = 0.07f;
        private const float CoreDiameterMeters = 0.049f;
        private const float PositionToleranceMeters = 0.00001f;

        [MenuItem("Bellerophon/Art Samples/Inspect Lightsaber Blade Unity Sources")]
        internal static void InspectLightsaberBladeUnitySampleSources()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            GameObject handle = AssetDatabase.LoadAssetAtPath<GameObject>(HandleModelPath) ??
                throw new InvalidOperationException(
                    "Imported lightsaber handle is missing: " + HandleModelPath);
            Shader shader = Shader.Find(ShaderName) ??
                throw new InvalidOperationException(
                    "Lightsaber blade sample shader is unavailable: " + ShaderName);
            if (!File.Exists(AbsoluteProjectPath(ConceptImageRelativePath)))
                throw new FileNotFoundException(
                    "Approved visual target image is missing.",
                    AbsoluteProjectPath(ConceptImageRelativePath));
            Renderer[] renderers = handle.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Imported lightsaber handle has no renderer.");
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[LightsaberBladeSample] Sources inspected read-only." +
                " totalLengthMeters=" + Num(TotalLengthMeters) +
                "|handleLengthMeters=" + Num(HandleModelLengthMeters) +
                "|bladeLengthMeters=" + Num(BladeLengthMeters) +
                "|glowDiameterMeters=" + Num(GlowDiameterMeters) +
                "|coreDiameterMeters=" + Num(CoreDiameterMeters) +
                "|handleRendererCount=" + renderers.Length +
                "|shader=" + shader.name +
                "|handleSha256=" + Sha256Asset(HandleModelPath) +
                "|conceptSha256=" + Sha256ProjectFile(ConceptImageRelativePath) +
                "|verificationTargetManipulated=False");
        }

        [MenuItem("Bellerophon/Art Samples/Create Lightsaber Blade Unity Sample")]
        internal static void CreateLightsaberBladeUnitySample()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            string gameplaySceneHashBefore = Sha256Asset(GameplayScenePath);
            string handleHashBefore = Sha256Asset(HandleModelPath);
            string conceptHashBefore = Sha256ProjectFile(ConceptImageRelativePath);
            GameObject handleSource = AssetDatabase.LoadAssetAtPath<GameObject>(
                HandleModelPath) ?? throw new InvalidOperationException(
                "Imported lightsaber handle is missing: " + HandleModelPath);
            Shader shader = Shader.Find(ShaderName) ??
                throw new InvalidOperationException(
                    "Lightsaber blade sample shader is unavailable: " + ShaderName);

            EnsureFolder("Assets/_Project/ArtSamples");
            EnsureFolder(SampleRoot);
            Mesh coreMesh = CreateOrUpdateBladeMesh(
                CoreMeshPath,
                "LightsaberBladeCoreMesh",
                BladeLengthMeters,
                CoreDiameterMeters * 0.5f);
            Mesh glowMesh = CreateOrUpdateBladeMesh(
                GlowMeshPath,
                "LightsaberBladeGlowMesh",
                BladeLengthMeters,
                GlowDiameterMeters * 0.5f);
            Material coreMaterial = CreateOrUpdateMaterial(
                CoreMaterialPath,
                "LightsaberBladeCore",
                shader,
                new Color(7.5f, 8.0f, 8.0f, 1f),
                3010);
            Material glowMaterial = CreateOrUpdateMaterial(
                GlowMaterialPath,
                "LightsaberBladeGlow",
                shader,
                new Color(0.02f, 0.34f, 5.8f, 0.72f),
                3000);
            BuildPrefab(coreMesh, glowMesh, coreMaterial, glowMaterial);
            CreateSampleScene(handleSource);
            AssetDatabase.SaveAssets();

            RequireEqual(
                gameplaySceneHashBefore,
                Sha256Asset(GameplayScenePath),
                "CargoRunMvp scene hash");
            RequireEqual(handleHashBefore, Sha256Asset(HandleModelPath),
                "imported lightsaber handle hash");
            RequireEqual(
                conceptHashBefore,
                Sha256ProjectFile(ConceptImageRelativePath),
                "approved concept image hash");
            InspectSampleAssetsAndScene();
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[LightsaberBladeSample] Reusable static-on Unity sample created." +
                " prefab=" + PrefabPath +
                "|scene=" + ScenePath +
                "|totalLengthMeters=" + Num(TotalLengthMeters) +
                "|bladeLengthMeters=" + Num(BladeLengthMeters) +
                "|outerDiameterMeters=" + Num(GlowDiameterMeters) +
                "|roundedTip=True|whiteCore=True|blueOuterGlow=True" +
                "|additiveOuterGlow=True|gameplaySceneChanged=False|gameplayLinked=False" +
                "|handleAssetChanged=False|conceptChanged=False");
        }

        [MenuItem("Bellerophon/Art Samples/Inspect Lightsaber Blade Unity Sample")]
        internal static void InspectLightsaberBladeUnitySample()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            InspectSampleAssetsAndScene();
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[LightsaberBladeSample] Unity sample inspected read-only." +
                " verificationTargetManipulated=False" +
                "|prefabReady=True|sampleSceneReady=True" +
                "|gameplayLinked=False|newConsoleErrors=0");
        }

        [MenuItem("Bellerophon/Art Samples/Capture Lightsaber Blade Unity Sample Final")]
        internal static void CaptureLightsaberBladeUnitySampleFinal()
        {
            RequireEditMode();
            int consoleErrorsBefore = ConsoleErrorCount();
            InspectSampleAssetsAndScene();
            string outputPath = AbsoluteProjectPath(FinalImageRelativePath);
            if (File.Exists(outputPath))
                throw new InvalidOperationException(
                    "The one final Unity blade sample capture already exists: " +
                    FinalImageRelativePath);

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene sampleScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            Texture2D image = null;
            try
            {
                Camera camera = RequireSceneComponent<Camera>(
                    sampleScene,
                    "LightsaberBladeSampleCamera");
                image = RenderCamera(camera, 1536, 864);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ??
                    throw new InvalidOperationException(
                        "Final image directory could not be resolved."));
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                EditorSceneManager.CloseScene(sampleScene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[LightsaberBladeSample] One final direct Unity render captured." +
                " output=" + FinalImageRelativePath +
                "|gameplaySceneChanged=False|verificationTargetManipulated=False");
        }

        private static Mesh CreateOrUpdateBladeMesh(
            string path,
            string meshName,
            float length,
            float radius)
        {
            const int radialSegments = 48;
            const int capSegments = 12;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var rings = new List<int>();

            AddRing(vertices, normals, uvs, rings, 0f, radius, radialSegments, false,
                Vector3.zero, 0f);
            float capCenterX = length - radius;
            AddRing(vertices, normals, uvs, rings, capCenterX, radius,
                radialSegments, false, Vector3.zero, capCenterX / length);
            for (int index = 1; index < capSegments; index++)
            {
                float angle = index / (float)capSegments * Mathf.PI * 0.5f;
                float x = capCenterX + Mathf.Sin(angle) * radius;
                float ringRadius = Mathf.Cos(angle) * radius;
                AddRing(
                    vertices,
                    normals,
                    uvs,
                    rings,
                    x,
                    ringRadius,
                    radialSegments,
                    true,
                    new Vector3(capCenterX, 0f, 0f),
                    x / length);
            }

            for (int ring = 0; ring < rings.Count - 1; ring++)
            {
                int current = rings[ring];
                int next = rings[ring + 1];
                for (int segment = 0; segment < radialSegments; segment++)
                {
                    int following = (segment + 1) % radialSegments;
                    triangles.Add(current + segment);
                    triangles.Add(next + segment);
                    triangles.Add(current + following);
                    triangles.Add(current + following);
                    triangles.Add(next + segment);
                    triangles.Add(next + following);
                }
            }

            int tipIndex = vertices.Count;
            vertices.Add(new Vector3(length, 0f, 0f));
            normals.Add(Vector3.right);
            uvs.Add(new Vector2(1f, 0.5f));
            int lastRing = rings[rings.Count - 1];
            for (int segment = 0; segment < radialSegments; segment++)
            {
                int following = (segment + 1) % radialSegments;
                triangles.Add(lastRing + segment);
                triangles.Add(tipIndex);
                triangles.Add(lastRing + following);
            }

            int baseCenterIndex = vertices.Count;
            vertices.Add(Vector3.zero);
            normals.Add(Vector3.left);
            uvs.Add(new Vector2(0.5f, 0.5f));
            int baseRing = rings[0];
            for (int segment = 0; segment < radialSegments; segment++)
            {
                int following = (segment + 1) % radialSegments;
                triangles.Add(baseCenterIndex);
                triangles.Add(baseRing + following);
                triangles.Add(baseRing + segment);
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                mesh.Clear();
            }
            mesh.name = meshName;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void AddRing(
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<Vector2> uvs,
            ICollection<int> rings,
            float x,
            float radius,
            int segments,
            bool sphericalNormal,
            Vector3 capCenter,
            float u)
        {
            int start = vertices.Count;
            rings.Add(start);
            for (int segment = 0; segment < segments; segment++)
            {
                float angle = segment / (float)segments * Mathf.PI * 2f;
                Vector3 vertex = new Vector3(
                    x,
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius);
                vertices.Add(vertex);
                normals.Add(sphericalNormal
                    ? (vertex - capCenter).normalized
                    : new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle)));
                uvs.Add(new Vector2(u, segment / (float)segments));
            }
        }

        private static Material CreateOrUpdateMaterial(
            string path,
            string materialName,
            Shader shader,
            Color color,
            int renderQueue)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }
            material.name = materialName;
            material.SetColor("_BaseColor", color);
            material.renderQueue = renderQueue;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildPrefab(
            Mesh coreMesh,
            Mesh glowMesh,
            Material coreMaterial,
            Material glowMaterial)
        {
            var root = new GameObject("LightsaberBladeSample");
            try
            {
                CreateMeshLayer(root.transform, "BlueOuterGlow", glowMesh, glowMaterial, 0);
                CreateMeshLayer(root.transform, "WhiteCore", coreMesh, coreMaterial, 1);
                var lightObject = new GameObject("BlueBladeLight");
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.localPosition =
                    new Vector3(BladeLengthMeters * 0.48f, 0f, 0f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.08f, 0.32f, 1f, 1f);
                light.range = 1.35f;
                light.intensity = 2.4f;
                light.shadows = LightShadows.None;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateMeshLayer(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            int sortingOrder)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.sortingOrder = sortingOrder;
        }

        private static void CreateSampleScene(GameObject handleSource)
        {
            GameObject bladePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ??
                throw new InvalidOperationException("Blade sample prefab was not created.");
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene sampleScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            try
            {
                var root = new GameObject("LightsaberBladeUnitySample");
                SceneManager.MoveGameObjectToScene(root, sampleScene);

                GameObject handle = (GameObject)PrefabUtility.InstantiatePrefab(
                    handleSource,
                    sampleScene);
                handle.name = "ExistingLightsaberHandle";
                handle.transform.SetParent(root.transform, false);
                handle.transform.localPosition = Vector3.zero;
                handle.transform.localRotation = Quaternion.identity;
                handle.transform.localScale = Vector3.one * 27.3845215f;

                GameObject blade = (GameObject)PrefabUtility.InstantiatePrefab(
                    bladePrefab,
                    sampleScene);
                blade.name = "LightsaberBladeSample";
                blade.transform.SetParent(root.transform, false);
                blade.transform.localPosition =
                    new Vector3(HandleModelLengthMeters * 0.5f, 0f, 0f);
                blade.transform.localRotation = Quaternion.identity;
                blade.transform.localScale = Vector3.one;

                SetLayerRecursively(root, SampleLayer);
                Camera camera = CreateCamera(sampleScene, root.transform);
                CreateLighting(sampleScene, root.transform);
                ConfigureCamera(camera);

                if (!EditorSceneManager.SaveScene(sampleScene, ScenePath))
                    throw new InvalidOperationException(
                        "Lightsaber blade sample scene save failed: " + ScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(sampleScene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
        }

        private static Camera CreateCamera(Scene scene, Transform parent)
        {
            var cameraObject = new GameObject("LightsaberBladeSampleCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.003f, 0.007f, 0.016f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.cullingMask = 1 << SampleLayer;
            return camera;
        }

        private static void ConfigureCamera(Camera camera)
        {
            Vector3 center = new Vector3(
                -HandleModelLengthMeters * 0.5f + TotalLengthMeters * 0.5f,
                0f,
                0f);
            camera.transform.position = center + new Vector3(0.05f, 0.15f, -1.55f);
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position,
                Vector3.up);
        }

        private static void CreateLighting(Scene scene, Transform parent)
        {
            var keyObject = new GameObject("LightsaberBladeKeyLight");
            SceneManager.MoveGameObjectToScene(keyObject, scene);
            keyObject.transform.SetParent(parent, false);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.75f, 0.86f, 1f, 1f);
            key.intensity = 1.5f;
            key.shadows = LightShadows.Soft;
            key.cullingMask = 1 << SampleLayer;
            key.transform.rotation = Quaternion.Euler(42f, -38f, 0f);

            var fillObject = new GameObject("LightsaberBladeFillLight");
            SceneManager.MoveGameObjectToScene(fillObject, scene);
            fillObject.transform.SetParent(parent, false);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.22f, 0.38f, 0.78f, 1f);
            fill.intensity = 0.8f;
            fill.shadows = LightShadows.None;
            fill.cullingMask = 1 << SampleLayer;
            fill.transform.rotation = Quaternion.Euler(330f, 150f, 0f);
        }

        private static void InspectSampleAssetsAndScene()
        {
            Mesh coreMesh = AssetDatabase.LoadAssetAtPath<Mesh>(CoreMeshPath) ??
                throw new InvalidOperationException("Core mesh asset is missing.");
            Mesh glowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GlowMeshPath) ??
                throw new InvalidOperationException("Glow mesh asset is missing.");
            Material coreMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                CoreMaterialPath) ?? throw new InvalidOperationException(
                "Core material is missing.");
            Material glowMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                GlowMaterialPath) ?? throw new InvalidOperationException(
                "Glow material is missing.");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ??
                throw new InvalidOperationException("Blade sample prefab is missing.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Blade sample scene is missing.");

            RequireNear(coreMesh.bounds.size.x, BladeLengthMeters,
                "core mesh length");
            RequireNear(glowMesh.bounds.size.x, BladeLengthMeters,
                "glow mesh length");
            RequireNear(coreMesh.bounds.size.y, CoreDiameterMeters,
                "core mesh diameter");
            RequireNear(glowMesh.bounds.size.y, GlowDiameterMeters,
                "glow mesh diameter");
            if (coreMaterial.shader == null || coreMaterial.shader.name != ShaderName ||
                glowMaterial.shader == null || glowMaterial.shader.name != ShaderName)
                throw new InvalidOperationException(
                    "Blade sample materials do not use the approved shader.");
            if (prefab.transform.Find("WhiteCore") == null ||
                prefab.transform.Find("BlueOuterGlow") == null ||
                prefab.transform.Find("BlueBladeLight") == null)
                throw new InvalidOperationException(
                    "Blade sample prefab hierarchy is incomplete.");

            string[] prefabDependencies = AssetDatabase.GetDependencies(PrefabPath, true);
            RequireDependency(prefabDependencies, CoreMeshPath, "prefab core mesh");
            RequireDependency(prefabDependencies, GlowMeshPath, "prefab glow mesh");
            RequireDependency(prefabDependencies, CoreMaterialPath, "prefab core material");
            RequireDependency(prefabDependencies, GlowMaterialPath, "prefab glow material");

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene sampleScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Transform root = RequireSceneObject(
                    sampleScene,
                    "LightsaberBladeUnitySample").transform;
                Transform handle = root.Find("ExistingLightsaberHandle") ??
                    throw new InvalidOperationException(
                        "Sample scene handle is missing.");
                Transform blade = root.Find("LightsaberBladeSample") ??
                    throw new InvalidOperationException(
                        "Sample scene blade is missing.");
                RequireNear(
                    blade.localPosition.x,
                    HandleModelLengthMeters * 0.5f,
                    "blade emitter position");
                RequireNear(
                    handle.localScale.x,
                    27.3845215f,
                    "sample handle scale");
                Camera camera = RequireSceneComponent<Camera>(
                    sampleScene,
                    "LightsaberBladeSampleCamera");
                if (!camera.allowHDR)
                    throw new InvalidOperationException(
                        "Sample camera HDR is disabled.");
            }
            finally
            {
                EditorSceneManager.CloseScene(sampleScene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }

            string[] sceneDependencies = AssetDatabase.GetDependencies(ScenePath, true);
            RequireDependency(sceneDependencies, HandleModelPath, "scene handle model");
            RequireDependency(sceneDependencies, PrefabPath, "scene blade prefab");
            if (sceneDependencies.Contains(GameplayScenePath, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    "Standalone sample scene unexpectedly depends on CargoRunMvp.");
        }

        private static Texture2D RenderCamera(Camera camera, int width, int height)
        {
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGBHalf)
            {
                antiAliasing = 4,
                useMipMap = false,
                autoGenerateMips = false
            };
            target.Create();
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply(false, false);
                return result;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(result);
                throw;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static GameObject RequireSceneObject(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + " in sample scene; found " +
                    matches.Length + ".");
            return matches[0];
        }

        private static T RequireSceneComponent<T>(Scene scene, string name)
            where T : Component
        {
            GameObject item = RequireSceneObject(scene, name);
            return item.GetComponent<T>() ?? throw new InvalidOperationException(
                name + " has no " + typeof(T).Name + ".");
        }

        private static void RequireDependency(
            IEnumerable<string> dependencies,
            string expected,
            string label)
        {
            if (!dependencies.Contains(expected, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    label + " dependency is missing: " + expected);
        }

        private static void RequireNear(float actual, float expected, string label)
        {
            if (Mathf.Abs(actual - expected) > PositionToleranceMeters)
                throw new InvalidOperationException(
                    label + " expected=" + Num(expected) +
                    ", actual=" + Num(actual) + ".");
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Invalid sample folder: " + path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string Sha256Asset(string assetPath)
        {
            return Sha256File(AbsoluteAssetPath(assetPath));
        }

        private static string Sha256ProjectFile(string relativePath)
        {
            return Sha256File(AbsoluteProjectPath(relativePath));
        }

        private static string Sha256File(string path)
        {
            using (SHA256 algorithm = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(algorithm.ComputeHash(stream))
                    .Replace("-", string.Empty);
        }

        private static string AbsoluteAssetPath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("Expected Assets path: " + assetPath);
            return Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException("Project root is unavailable."),
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string AbsoluteProjectPath(string relativePath)
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException("Project root is unavailable."),
                relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Lightsaber blade sample tools require Edit Mode.");
        }

        private static int ConsoleErrorCount()
        {
            Type type = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            var method = type.GetMethod(
                "GetCountsByType",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) ??
                throw new InvalidOperationException(
                    "Unity console count API is unavailable.");
            object[] arguments = { 0, 0, 0 };
            method.Invoke(null, arguments);
            return (int)arguments[0];
        }

        private static void RequireNoNewUnityConsoleErrors(int before)
        {
            int after = ConsoleErrorCount();
            if (after > before)
                throw new InvalidOperationException(
                    "Unity console gained " + (after - before) + " new error(s).");
        }

        private static string Num(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }
    }
}
