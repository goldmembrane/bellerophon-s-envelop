using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using Bellerophon.Vfx;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class MarkerSpraySprayVfxSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "MarkerSpray_Spray";
        private const string AssetRoot = "Assets/_Project/VFX/MarkerSpray";
        private const string TextureRoot = AssetRoot + "/Textures";
        private const string MaterialRoot = AssetRoot + "/Materials";
        private const string ReviewRoot = AssetRoot + "/Review";
        private const string MistTexturePath = TextureRoot + "/marker_spray_mist_particle.png";
        private const string FlowTexturePath = TextureRoot + "/marker_spray_red_cone_mist_texture.png";
        private const string SpecksTexturePath = TextureRoot + "/marker_spray_droplet_particle.png";
        private const string MistMaterialPath = MaterialRoot + "/MarkerSpray_Mist.mat";
        private const string FlowMaterialPath = MaterialRoot + "/MarkerSpray_Flow.mat";
        private const string SpecksMaterialPath = MaterialRoot + "/MarkerSpray_Specks.mat";
        private const string PrefabPath = AssetRoot + "/MarkerSpraySprayVfx.prefab";
        private const string FinalImagePath = ReviewRoot + "/MarkerSpraySprayVfx_Final.png";
        private const string ReportPath = ReviewRoot + "/MarkerSpraySprayVfx_Inspection.txt";
        private const string AppliedMarkerPath = AssetRoot + "/MarkerSpraySprayVfx.applied";
        private const string Revision = "marker-spray-vfx-7deg-r3";
        private const float ConeAngleDegrees = 7f;
        private const float RangeMeters = 1f;
        private const int ErrorModeMask =
            (1 << 0) |
            (1 << 1) |
            (1 << 4) |
            (1 << 6) |
            (1 << 8) |
            (1 << 11) |
            (1 << 13) |
            (1 << 17) |
            (1 << 20) |
            (1 << 21);

        private static bool applicationAttemptedThisDomain;

        private static readonly string[] TextureNames =
        {
            "marker_spray_mist_particle.png",
            "marker_spray_red_cone_mist_texture.png",
            "marker_spray_droplet_particle.png"
        };

        static MarkerSpraySprayVfxSetupTools()
        {
            EditorApplication.update += TryApplyApprovedSampleOnce;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.update -= TryApplyApprovedSampleOnce;
            EditorApplication.update += TryApplyApprovedSampleOnce;
        }

        internal static void QueueApprovedSampleApplication()
        {
            EditorApplication.update -= TryApplyApprovedSampleOnce;
            EditorApplication.update += TryApplyApprovedSampleOnce;
        }

        private static void TryApplyApprovedSampleOnce()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            EditorApplication.update -= TryApplyApprovedSampleOnce;

            string markerAbsolute = Absolute(AppliedMarkerPath);
            if (File.Exists(markerAbsolute) &&
                File.ReadAllText(markerAbsolute, Encoding.UTF8).Trim() == Revision)
            {
                return;
            }

            if (applicationAttemptedThisDomain)
            {
                return;
            }

            applicationAttemptedThisDomain = true;

            try
            {
                ApplyMarkerSpraySprayVfx();
            }
            catch (Exception exception)
            {
                EnsureDirectory(ReviewRoot);
                File.WriteAllText(
                    Absolute(ReportPath),
                    "MarkerSpray_Spray VFX application failed.\r\n" + exception,
                    new UTF8Encoding(false));
                AssetDatabase.ImportAsset(
                    ReportPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                Debug.LogException(exception);
            }
        }

        internal static void ApplyMarkerSpraySprayVfx()
        {
            RequireEditModeAndScene();
            int consoleErrorsBefore = CurrentUnityConsoleErrorCount();
            EnsureDirectory(TextureRoot);
            EnsureDirectory(MaterialRoot);
            EnsureDirectory(ReviewRoot);

            CopyApprovedTexturesByteExact();
            ConfigureTextureImporter(MistTexturePath);
            ConfigureTextureImporter(FlowTexturePath);
            ConfigureTextureImporter(SpecksTexturePath);

            Material mistMaterial = CreateOrUpdateMaterial(
                MistMaterialPath,
                RequireAsset<Texture2D>(MistTexturePath));
            Material flowMaterial = CreateOrUpdateMaterial(
                FlowMaterialPath,
                RequireAsset<Texture2D>(FlowTexturePath));
            Material specksMaterial = CreateOrUpdateMaterial(
                SpecksMaterialPath,
                RequireAsset<Texture2D>(SpecksTexturePath));
            GameObject effectPrefab = CreateOrUpdatePrefab(
                mistMaterial,
                flowMaterial,
                specksMaterial);

            Scene scene = SceneManager.GetActiveScene();
            GameObject target = FindUnique(scene, TargetName);
            MarkerSprayRightHandFollowBehaviour carry =
                target.GetComponent<MarkerSprayRightHandFollowBehaviour>() ??
                throw new MissingReferenceException(
                    "MarkerSpray_Spray right-hand follow component is missing.");
            carry.RefreshPreview();
            if (carry.SprayModel == null || carry.SprayHolder == null)
            {
                throw new MissingReferenceException(
                    "MarkerSpray_Spray dynamic marking-spray model is missing.");
            }

            string protectedBefore = ProtectedSignature(target, carry);
            ComputeNozzlePose(
                target.transform,
                carry.SprayModel,
                out Vector3 nozzleLocalPosition,
                out Quaternion nozzleLocalRotation,
                out Bounds modelLocalBounds);

            MarkerSpraySprayVfx controller =
                target.GetComponent<MarkerSpraySprayVfx>();
            if (controller == null)
            {
                controller = Undo.AddComponent<MarkerSpraySprayVfx>(target);
            }

            Undo.RecordObject(controller, "Apply approved MarkerSpray spray VFX");
            controller.Configure(
                effectPrefab,
                nozzleLocalPosition,
                nozzleLocalRotation);
            controller.RefreshPreview();
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
            EditorUtility.SetDirty(controller);

            string protectedAfter = ProtectedSignature(target, carry);
            RequireEqual(protectedBefore, protectedAfter,
                "protected MarkerSpray model, material, pose and animation signature");

            InspectAppliedVfx(target, carry, controller, modelLocalBounds);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp scene could not be saved.");
            }

            AssetDatabase.SaveAssets();
            if (!File.Exists(Absolute(FinalImagePath)))
            {
                CaptureFinalComparison(target, carry, controller);
            }
            WriteInspectionReport(target, carry, controller, modelLocalBounds);
            File.WriteAllText(
                Absolute(AppliedMarkerPath),
                Revision + Environment.NewLine,
                new UTF8Encoding(false));
            AssetDatabase.ImportAsset(
                AppliedMarkerPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log(
                "[MarkerSpraySprayVfx] Approved seven-degree red cone mist applied " +
                "to the dynamic MarkerSpray_Spray nozzle and captured once.");
        }

        private static void CopyApprovedTexturesByteExact()
        {
            string sourceRoot = Path.Combine(
                ProjectRoot,
                "artSample",
                "vfx",
                "marker_spray");
            foreach (string fileName in TextureNames)
            {
                string source = Path.Combine(sourceRoot, fileName);
                string destination = Path.Combine(
                    Absolute(TextureRoot),
                    fileName);
                if (!File.Exists(source))
                {
                    throw new FileNotFoundException(
                        "Approved MarkerSpray texture is missing.",
                        source);
                }

                File.Copy(source, destination, true);
                RequireEqual(
                    Sha256(source),
                    Sha256(destination),
                    fileName + " source/imported byte hash");
                AssetDatabase.ImportAsset(
                    TextureRoot + "/" + fileName,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
            }
        }

        private static void ConfigureTextureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException(
                    "Texture importer is unavailable: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateMaterial(
            string path,
            Texture2D texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                throw new InvalidOperationException(
                    "Universal Render Pipeline/Particles/Unlit shader is unavailable.");
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

            material.name = Path.GetFileNameWithoutExtension(path);
            material.mainTexture = texture;
            SetTextureIfPresent(material, "_BaseMap", texture);
            SetTextureIfPresent(material, "_MainTex", texture);
            SetColorIfPresent(material, "_BaseColor", Color.white);
            SetColorIfPresent(material, "_Color", Color.white);
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_Cull", (float)CullMode.Off);
            SetFloatIfPresent(material, "_ZWrite", 0f);
            SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfPresent(material, "_SoftParticlesEnabled", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_FADING_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateOrUpdatePrefab(
            Material mistMaterial,
            Material flowMaterial,
            Material specksMaterial)
        {
            var root = new GameObject("MarkerSpraySprayVfx");
            try
            {
                CreateLayer(
                    root.transform,
                    "MarkerSpray_Mist",
                    mistMaterial,
                    ParticleSystemRenderMode.Billboard,
                    72f,
                    new Vector2(0.95f, 1.05f),
                    new Vector2(0.85f, 0.95f),
                    new Vector2(0.055f, 0.13f),
                    new Vector3(0.35f, 1f, 1.22f),
                    true,
                    0f,
                    0f);
                CreateLayer(
                    root.transform,
                    "MarkerSpray_Flow",
                    flowMaterial,
                    ParticleSystemRenderMode.Stretch,
                    46f,
                    new Vector2(0.72f, 0.82f),
                    new Vector2(1.1f, 1.25f),
                    new Vector2(0.022f, 0.052f),
                    Vector3.one,
                    false,
                    1.6f,
                    0.12f);
                CreateLayer(
                    root.transform,
                    "MarkerSpray_Specks",
                    specksMaterial,
                    ParticleSystemRenderMode.Billboard,
                    118f,
                    new Vector2(0.78f, 0.86f),
                    new Vector2(1.05f, 1.18f),
                    new Vector2(0.008f, 0.024f),
                    new Vector3(0.42f, 0.9f, 0.28f),
                    true,
                    0f,
                    0f);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) ??
                    throw new InvalidOperationException(
                        "MarkerSpray VFX prefab could not be saved.");
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateLayer(
            Transform parent,
            string name,
            Material material,
            ParticleSystemRenderMode renderMode,
            float rate,
            Vector2 lifetime,
            Vector2 speed,
            Vector2 size,
            Vector3 sizeCurve,
            bool useSizeCurve,
            float lengthScale,
            float velocityScale)
        {
            var layerObject = new GameObject(name, typeof(ParticleSystem));
            layerObject.transform.SetParent(parent, false);
            ParticleSystem system = layerObject.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 512;
            main.stopAction = ParticleSystemStopAction.None;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = ConeAngleDegrees;
            shape.radius = 0.006f;
            shape.length = 0.02f;
            shape.radiusThickness = 1f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(ApprovedGradient());

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.separateAxes = false;
            noise.strength = 0.045f;
            noise.frequency = 0.85f;
            noise.scrollSpeed = 0.25f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.High;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
                system.sizeOverLifetime;
            sizeOverLifetime.enabled = useSizeCurve;
            if (useSizeCurve)
            {
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                    1f,
                    new AnimationCurve(
                        new Keyframe(0f, sizeCurve.x),
                        new Keyframe(0.58f, sizeCurve.y),
                        new Keyframe(1f, sizeCurve.z)));
            }

            ParticleSystemRenderer renderer =
                layerObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = renderMode;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 0.5f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (renderMode == ParticleSystemRenderMode.Stretch)
            {
                renderer.lengthScale = lengthScale;
                renderer.velocityScale = velocityScale;
            }
        }

        private static Gradient ApprovedGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(HtmlColor("FF2637"), 0f),
                    new GradientColorKey(HtmlColor("FF2637"), 0.08f),
                    new GradientColorKey(HtmlColor("DF172A"), 0.62f),
                    new GradientColorKey(HtmlColor("B40E20"), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.82f, 0.08f),
                    new GradientAlphaKey(0.42f, 0.62f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static void ComputeNozzlePose(
            Transform target,
            Transform sprayModel,
            out Vector3 localPosition,
            out Quaternion localRotation,
            out Bounds localBounds)
        {
            var vertices = new List<Vector3>();
            foreach (MeshFilter filter in sprayModel.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                foreach (Vector3 vertex in mesh.vertices)
                {
                    vertices.Add(sprayModel.InverseTransformPoint(
                        filter.transform.TransformPoint(vertex)));
                }
            }

            if (vertices.Count == 0)
            {
                throw new InvalidOperationException(
                    "Marking-spray mesh vertices are unavailable for nozzle placement.");
            }

            localBounds = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 vertex in vertices.Skip(1))
            {
                localBounds.Encapsulate(vertex);
            }

            Vector3 localUp = sprayModel.InverseTransformDirection(target.up).normalized;
            Vector3 localForward =
                sprayModel.InverseTransformDirection(target.forward).normalized;
            Vector3 localRight =
                sprayModel.InverseTransformDirection(target.right).normalized;

            float minUp = vertices.Min(vertex => Vector3.Dot(vertex, localUp));
            float maxUp = vertices.Max(vertex => Vector3.Dot(vertex, localUp));
            float height = maxUp - minUp;
            Vector3[] topVertices = vertices
                .Where(vertex => Vector3.Dot(vertex, localUp) >= maxUp - height * 0.10f)
                .ToArray();
            if (topVertices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Marking-spray top nozzle vertices could not be resolved.");
            }

            float front = topVertices.Max(vertex => Vector3.Dot(vertex, localForward));
            float minFront = vertices.Min(vertex => Vector3.Dot(vertex, localForward));
            float maxFront = vertices.Max(vertex => Vector3.Dot(vertex, localForward));
            float depth = maxFront - minFront;
            Vector3[] outletVertices = topVertices
                .Where(vertex => Vector3.Dot(vertex, localForward) >= front - depth * 0.035f)
                .ToArray();
            float rightCenter = outletVertices.Length > 0
                ? outletVertices.Average(vertex => Vector3.Dot(vertex, localRight))
                : vertices.Average(vertex => Vector3.Dot(vertex, localRight));

            float outletUp = maxUp - height * 0.048f;
            float outletForward = front + Mathf.Max(0.00005f, depth * 0.008f);
            localPosition =
                localRight * rightCenter +
                localUp * outletUp +
                localForward * outletForward;
            localRotation = Quaternion.Inverse(sprayModel.rotation) *
                Quaternion.LookRotation(target.forward, target.up);
        }

        private static void InspectAppliedVfx(
            GameObject target,
            MarkerSprayRightHandFollowBehaviour carry,
            MarkerSpraySprayVfx controller,
            Bounds modelLocalBounds)
        {
            controller.RefreshPreview();
            if (AssetDatabase.GetAssetPath(controller.EffectPrefab) != PrefabPath)
            {
                throw new InvalidOperationException(
                    "MarkerSpray VFX prefab reference differs.");
            }
            if (controller.ConfigurationVersion !=
                MarkerSpraySprayVfx.CurrentConfigurationVersion)
            {
                throw new InvalidOperationException(
                    "MarkerSpray VFX configuration version differs.");
            }
            if (controller.EffectInstance == null ||
                controller.BoundSprayModel != carry.SprayModel ||
                controller.EffectInstance.transform.parent != carry.SprayHolder.transform)
            {
                throw new InvalidOperationException(
                    "MarkerSpray VFX is not bound to the current dynamic spray holder.");
            }

            float originError = Vector3.Distance(
                controller.EffectInstance.transform.position,
                controller.NozzleWorldPosition);
            float rotationError = Quaternion.Angle(
                controller.EffectInstance.transform.rotation,
                controller.NozzleWorldRotation);
            if (originError > 0.00001f || rotationError > 0.001f)
            {
                throw new InvalidOperationException(
                    "MarkerSpray VFX nozzle follow error exceeds tolerance.");
            }

            ParticleSystem[] systems = controller.EffectInstance
                .GetComponentsInChildren<ParticleSystem>(true);
            RequireEqual(3, systems.Length, "particle layer count");
            string[] expectedNames =
            {
                "MarkerSpray_Mist",
                "MarkerSpray_Flow",
                "MarkerSpray_Specks"
            };
            foreach (string expectedName in expectedNames)
            {
                ParticleSystem system = systems.SingleOrDefault(
                    item => item.name == expectedName) ??
                    throw new InvalidOperationException(
                        "MarkerSpray VFX layer is missing: " + expectedName);
                ParticleSystem.MainModule main = system.main;
                ParticleSystem.ShapeModule shape = system.shape;
                if (!main.loop || main.playOnAwake ||
                    main.simulationSpace != ParticleSystemSimulationSpace.World ||
                    main.maxParticles != 512 ||
                    shape.shapeType != ParticleSystemShapeType.Cone ||
                    !Approximately(shape.angle, ConeAngleDegrees) ||
                    !Approximately(shape.radius, 0.006f) ||
                    !Approximately(shape.length, 0.02f))
                {
                    throw new InvalidOperationException(
                        "MarkerSpray VFX shared specification differs: " + expectedName);
                }
            }

            if (modelLocalBounds.size.sqrMagnitude <= 0f)
            {
                throw new InvalidOperationException(
                    "Marking-spray model bounds are invalid.");
            }

            RequireTextureHashes();
        }

        private static void CaptureFinalComparison(
            GameObject target,
            MarkerSprayRightHandFollowBehaviour carry,
            MarkerSpraySprayVfx controller)
        {
            controller.RefreshPreview(0.92f);
            Texture2D unityPanel = CaptureUnityPanel(target, controller);
            Texture2D reference = LoadExternalTexture(Path.Combine(
                ProjectRoot,
                "artSample",
                "vfx",
                "marker_spray",
                "marker_spray_red_cone_mist_reference.png"));
            Texture2D composite = null;
            try
            {
                composite = ComposeComparison(reference, unityPanel);
                File.WriteAllBytes(Absolute(FinalImagePath), composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reference);
                UnityEngine.Object.DestroyImmediate(unityPanel);
                if (composite != null)
                {
                    UnityEngine.Object.DestroyImmediate(composite);
                }
            }

            AssetDatabase.ImportAsset(
                FinalImagePath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static Texture2D CaptureUnityPanel(
            GameObject target,
            MarkerSpraySprayVfx controller)
        {
            Vector3 direction = controller.NozzleWorldRotation * Vector3.forward;
            Vector3 up = target.transform.up.normalized;
            Vector3 side = Vector3.Cross(up, direction).normalized;
            if (side.sqrMagnitude < 0.5f)
            {
                side = target.transform.right.normalized;
            }

            Vector3 center = controller.NozzleWorldPosition +
                direction * 0.42f - up * 0.12f;
            var cameraObject = new GameObject("MarkerSpraySprayVfx_FinalCamera");
            var lightObject = new GameObject("MarkerSpraySprayVfx_FinalLight");
            RenderTexture render = RenderTexture.GetTemporary(
                1200,
                600,
                24,
                RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.021f, 0.025f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 0.64f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 10f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.targetTexture = render;
                camera.transform.SetPositionAndRotation(
                    center - side * 4f,
                    Quaternion.LookRotation(side, up));

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(0.95f, 0.98f, 1f);
                light.transform.rotation = Quaternion.LookRotation(
                    (direction - up * 0.35f - side * 0.4f).normalized,
                    up);

                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(
                    1200,
                    600,
                    TextureFormat.RGBA32,
                    false,
                    false);
                image.ReadPixels(new Rect(0, 0, 1200, 600), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Texture2D ComposeComparison(
            Texture2D reference,
            Texture2D unityPanel)
        {
            const int panelWidth = 1200;
            const int height = 600;
            var output = new Texture2D(
                panelWidth * 2,
                height,
                TextureFormat.RGBA32,
                false,
                false);
            Color background = new Color(0.018f, 0.021f, 0.025f, 1f);
            var pixels = Enumerable.Repeat(
                background,
                output.width * output.height).ToArray();
            output.SetPixels(pixels);
            BlitAspect(reference, output, 0, 0, panelWidth, height);
            BlitAspect(unityPanel, output, panelWidth, 0, panelWidth, height);
            for (int y = 0; y < height; y++)
            {
                output.SetPixel(panelWidth - 1, y, new Color(0.7f, 0.08f, 0.1f, 1f));
                output.SetPixel(panelWidth, y, new Color(0.7f, 0.08f, 0.1f, 1f));
            }
            output.Apply(false, false);
            return output;
        }

        private static void BlitAspect(
            Texture2D source,
            Texture2D destination,
            int panelX,
            int panelY,
            int panelWidth,
            int panelHeight)
        {
            float scale = Mathf.Min(
                (float)panelWidth / source.width,
                (float)panelHeight / source.height);
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            int offsetX = panelX + (panelWidth - width) / 2;
            int offsetY = panelY + (panelHeight - height) / 2;
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    destination.SetPixel(
                        offsetX + x,
                        offsetY + y,
                        source.GetPixelBilinear(u, v));
                }
            }
        }

        private static Texture2D LoadExternalTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(
                    "Approved MarkerSpray reference could not be decoded.");
            }
            return texture;
        }

        private static void WriteInspectionReport(
            GameObject target,
            MarkerSprayRightHandFollowBehaviour carry,
            MarkerSpraySprayVfx controller,
            Bounds modelLocalBounds)
        {
            ParticleSystem[] systems = controller.EffectInstance
                .GetComponentsInChildren<ParticleSystem>(true);
            var report = new StringBuilder()
                .AppendLine("MarkerSpray_Spray approved VFX inspection")
                .AppendLine("Revision=" + Revision)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + target.name)
                .AppendLine("DynamicModel=" + carry.SprayModel.name)
                .AppendLine("EffectPrefab=" + AssetDatabase.GetAssetPath(controller.EffectPrefab))
                .AppendLine("EffectParent=" + controller.EffectInstance.transform.parent.name)
                .AppendLine("LayerCount=" + systems.Length)
                .AppendLine("ConeHalfAngleDegrees=" + F(ConeAngleDegrees))
                .AppendLine("RangeMeters=" + F(RangeMeters))
                .AppendLine("SimulationSpace=World")
                .AppendLine("PlayOnAwake=False")
                .AppendLine("Activation=MarkerSpray_Spray active / BeginEmission")
                .AppendLine("Release=StopEmitting with natural particle fade")
                .AppendLine("NozzleLocalPosition=" + Vector(controller.NozzleLocalPosition))
                .AppendLine("NozzleLocalRotation=" + QuaternionText(controller.NozzleLocalRotation))
                .AppendLine("NozzleWorldPosition=" + Vector(controller.NozzleWorldPosition))
                .AppendLine("NozzleForward=" + Vector(
                    controller.NozzleWorldRotation * Vector3.forward))
                .AppendLine("ModelLocalBoundsCenter=" + Vector(modelLocalBounds.center))
                .AppendLine("ModelLocalBoundsSize=" + Vector(modelLocalBounds.size));
            foreach (string fileName in TextureNames)
            {
                report.AppendLine(fileName + ".SourceSha256=" +
                    Sha256(Path.Combine(
                        ProjectRoot,
                        "artSample",
                        "vfx",
                        "marker_spray",
                        fileName)));
                report.AppendLine(fileName + ".ImportedSha256=" +
                    Sha256(Path.Combine(Absolute(TextureRoot), fileName)));
            }
            report.AppendLine("ProtectedModelMaterialPoseAnimationSignature=Unchanged");
            report.AppendLine("FinalComparison=" + FinalImagePath);
            File.WriteAllText(
                Absolute(ReportPath),
                report.ToString(),
                new UTF8Encoding(false));
            AssetDatabase.ImportAsset(
                ReportPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static string ProtectedSignature(
            GameObject target,
            MarkerSprayRightHandFollowBehaviour carry)
        {
            var text = new StringBuilder()
                .AppendLine("targetPosition=" + Vector(target.transform.localPosition))
                .AppendLine("targetRotation=" + QuaternionText(target.transform.localRotation))
                .AppendLine("targetScale=" + Vector(target.transform.localScale))
                .AppendLine("profile=" + AssetDatabase.GetAssetPath(carry.Profile))
                .AppendLine("holderPosition=" + Vector(carry.SprayHolder.transform.localPosition))
                .AppendLine("holderRotation=" + QuaternionText(carry.SprayHolder.transform.localRotation))
                .AppendLine("holderScale=" + Vector(carry.SprayHolder.transform.localScale))
                .AppendLine("modelPosition=" + Vector(carry.SprayModel.localPosition))
                .AppendLine("modelRotation=" + QuaternionText(carry.SprayModel.localRotation))
                .AppendLine("modelScale=" + Vector(carry.SprayModel.localScale));
            Animator animator = target.GetComponent<Animator>();
            if (animator != null)
            {
                text.AppendLine("controller=" +
                    AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
                text.AppendLine("avatar=" + AssetDatabase.GetAssetPath(animator.avatar));
            }
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true)
                .Where(item => !IsVfxTransform(item))
                .OrderBy(item => RelativePath(target.transform, item), StringComparer.Ordinal))
            {
                text.Append(RelativePath(target.transform, item)).Append('|')
                    .Append(Vector(item.localPosition)).Append('|')
                    .Append(QuaternionText(item.localRotation)).Append('|')
                    .AppendLine(Vector(item.localScale));
            }
            foreach (Renderer renderer in carry.SprayModel.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => RelativePath(carry.SprayModel, item.transform),
                    StringComparer.Ordinal))
            {
                text.Append("renderer|")
                    .Append(RelativePath(carry.SprayModel, renderer.transform)).Append('|')
                    .Append(string.Join(",", renderer.sharedMaterials.Select(
                        AssetDatabase.GetAssetPath))).AppendLine();
            }
            return text.ToString();
        }

        private static bool IsVfxTransform(Transform item)
        {
            Transform current = item;
            while (current != null)
            {
                if (current.name == "MarkerSpraySpray_VFX")
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        private static string RelativePath(Transform root, Transform item)
        {
            if (root == item)
            {
                return string.Empty;
            }
            var names = new Stack<string>();
            Transform current = item;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", names);
        }

        private static void RequireTextureHashes()
        {
            foreach (string fileName in TextureNames)
            {
                RequireEqual(
                    Sha256(Path.Combine(
                        ProjectRoot,
                        "artSample",
                        "vfx",
                        "marker_spray",
                        fileName)),
                    Sha256(Path.Combine(Absolute(TextureRoot), fileName)),
                    fileName + " approved/imported byte hash");
            }
        }

        private static void RequireEditModeAndScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "MarkerSpray VFX application requires Edit Mode.");
            }
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene before applying MarkerSpray VFX.");
            }
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + name + ", found " + matches.Length + ".");
            }
            return matches[0];
        }

        private static void EnsureDirectory(string assetPath)
        {
            Directory.CreateDirectory(Absolute(assetPath));
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Required asset is missing: " + path);
            }
            return asset;
        }

        private static void SetTextureIfPresent(
            Material material,
            string property,
            Texture texture)
        {
            if (material.HasProperty(property))
            {
                material.SetTexture(property, texture);
            }
        }

        private static void SetColorIfPresent(
            Material material,
            string property,
            Color color)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, color);
            }
        }

        private static void SetFloatIfPresent(
            Material material,
            string property,
            float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static Color HtmlColor(string html)
        {
            if (!ColorUtility.TryParseHtmlString("#" + html, out Color color))
            {
                throw new InvalidOperationException("Invalid HTML color: " + html);
            }
            return color;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= 0.00001f;
        }

        private static int CurrentUnityConsoleErrorCount()
        {
            Type logEntriesType = Type.GetType(
                "UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException(
                    "Unity console count type could not be resolved.");
            MethodInfo method = logEntriesType.GetMethod(
                "GetCountsByType",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new InvalidOperationException(
                    "Unity console count API could not be resolved.");
            var arguments = new object[] { 0, 0, 0 };
            method.Invoke(null, arguments);
            return (int)arguments[0];
        }

        private static void RequireNoNewUnityConsoleErrors(int before)
        {
            int after = CurrentUnityConsoleErrorCount();
            if (after > before)
            {
                throw new InvalidOperationException(
                    "MarkerSpray VFX application added Unity console errors. " +
                    "before=" + before + ", after=" + after + ".\r\n" +
                    CurrentUnityConsoleErrorMessages());
            }
        }

        private static string CurrentUnityConsoleErrorMessages()
        {
            Type logEntriesType = Type.GetType(
                "UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException(
                    "Unity console entry type could not be resolved.");
            Type logEntryType = Type.GetType(
                "UnityEditor.LogEntry,UnityEditor.dll") ??
                throw new InvalidOperationException(
                    "Unity console item type could not be resolved.");
            BindingFlags staticFlags = BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo getCount = logEntriesType.GetMethod(
                "GetCount", staticFlags) ??
                throw new InvalidOperationException(
                    "Unity console entry count API could not be resolved.");
            MethodInfo start = logEntriesType.GetMethod(
                "StartGettingEntries", staticFlags) ??
                throw new InvalidOperationException(
                    "Unity console entry start API could not be resolved.");
            MethodInfo end = logEntriesType.GetMethod(
                "EndGettingEntries", staticFlags) ??
                throw new InvalidOperationException(
                    "Unity console entry end API could not be resolved.");
            MethodInfo getEntry = logEntriesType.GetMethods(staticFlags)
                .Single(method =>
                {
                    if (method.Name != "GetEntryInternal")
                    {
                        return false;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(int) &&
                        parameters[1].ParameterType == logEntryType;
                });
            var messages = new List<string>();
            start.Invoke(null, null);
            try
            {
                int count = (int)getCount.Invoke(null, null);
                for (int index = 0; index < count; index++)
                {
                    object entry = Activator.CreateInstance(logEntryType, true);
                    object returned = getEntry.Invoke(
                        null,
                        new[] { (object)index, entry });
                    if (returned is bool succeeded && !succeeded)
                    {
                        continue;
                    }

                    int mode = Convert.ToInt32(
                        ReadConsoleMember(entry, "mode") ?? 0,
                        CultureInfo.InvariantCulture);
                    if ((mode & ErrorModeMask) == 0)
                    {
                        continue;
                    }

                    string message = Convert.ToString(
                        ReadConsoleMember(entry, "message") ??
                        ReadConsoleMember(entry, "condition") ??
                        "<empty>",
                        CultureInfo.InvariantCulture);
                    messages.Add("Mode=" + mode + " " + message);
                }
            }
            finally
            {
                end.Invoke(null, null);
            }

            return string.Join("\r\n", messages);
        }

        private static object ReadConsoleMember(object instance, string name)
        {
            BindingFlags flags = BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(name, flags);
            return field != null
                ? field.GetValue(instance)
                : type.GetProperty(name, flags)?.GetValue(instance);
        }

        private static void RequireEqual<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    label + " differs. expected=" + expected + ", actual=" + actual);
            }
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static string Vector(Vector3 value)
        {
            return string.Join(",", new[] { F(value.x), F(value.y), F(value.z) });
        }

        private static string QuaternionText(Quaternion value)
        {
            return string.Join(",", new[]
            {
                F(value.x), F(value.y), F(value.z), F(value.w)
            });
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Absolute(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, assetPath));
        }

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ??
            throw new InvalidOperationException("Unity project root is unavailable.");
    }

    internal sealed class MarkerSpraySprayVfxAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            MarkerSpraySprayVfxSetupTools.QueueApprovedSampleApplication();
        }
    }
}
