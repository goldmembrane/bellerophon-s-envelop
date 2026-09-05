using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class BatonElectricVfxUnitySampleTool
    {
        private const int SampleCaptureLayer = 30;
        private const string BatonAssetPath =
            "Assets/_Project/Art/Items/ElectricBaton/electric_baton.fbx";
        private const string SampleRoot =
            "Assets/_Project/ArtSamples/BatonElectricVfx";
        private const string PrefabFolder = SampleRoot + "/Prefabs";
        private const string MaterialFolder = SampleRoot + "/Materials";
        private const string TextureFolder = SampleRoot + "/Textures";
        private const string ChargePrefabPath =
            PrefabFolder + "/BatonChargeReadyVfx.prefab";
        private const string DischargePrefabPath =
            PrefabFolder + "/BatonDischargeVfx.prefab";
        private const string ScenePath =
            SampleRoot + "/BatonElectricVfxSample.unity";
        private const string RuntimeAssemblyName =
            "Bellerophon.BatonElectricVfxSample";
        private const string EffectTypeName =
            "Bellerophon.ArtSamples.BatonElectricVfxUnitySample";
        private const string SequenceTypeName =
            "Bellerophon.ArtSamples.BatonElectricVfxSampleSequence";

        private static readonly float[] LightningOffsets =
        {
            0f,
            0.22f,
            -0.16f,
            0.12f,
            -0.27f,
            0.18f,
            -0.11f,
            0.24f,
            0f
        };

        private const string ChargeCaptureRelativePath =
            "artSample/baton_electric_vfx/unity_charge_ready.png";
        private const string DischargeCaptureRelativePath =
            "artSample/baton_electric_vfx/unity_discharge_peak.png";
        private const string ComparisonCaptureRelativePath =
            "artSample/baton_electric_vfx/unity_sample_comparison.png";

        [MenuItem("Bellerophon/Art Samples/Apply Unity Electric Baton VFX Sample")]
        internal static void ApplyBatonElectricVfxUnitySample()
        {
            Type effectType = RequireRuntimeType(EffectTypeName);
            Type sequenceType = RequireRuntimeType(SequenceTypeName);
            GameObject batonSource = AssetDatabase.LoadAssetAtPath<GameObject>(
                BatonAssetPath);
            if (batonSource == null)
            {
                throw new InvalidOperationException(
                    "The imported electric baton FBX was not found: " +
                    BatonAssetPath);
            }

            EnsureFolder("Assets/_Project/ArtSamples");
            EnsureFolder(SampleRoot);
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            DeleteParticleSampleAssets();

            Material haloMaterial = CreateOrUpdateLineMaterial(
                MaterialFolder + "/BatonElectricVioletHalo.mat",
                "BatonElectricVioletHalo",
                new Color(0.32f, 0.24f, 1f, 0.42f));
            Material deepMaterial = CreateOrUpdateLineMaterial(
                MaterialFolder + "/BatonElectricDeepBlue.mat",
                "BatonElectricDeepBlue",
                new Color(0.015f, 0.055f, 0.82f, 0.92f));
            Material blueMaterial = CreateOrUpdateLineMaterial(
                MaterialFolder + "/BatonElectricBrightBlue.mat",
                "BatonElectricBrightBlue",
                new Color(0.10f, 0.34f, 1f, 0.96f));
            Material glowMaterial = CreateOrUpdateLineMaterial(
                MaterialFolder + "/BatonElectricCyanGlow.mat",
                "BatonElectricCyanGlow",
                new Color(0.005f, 0.56f, 1f, 0.96f));
            Material coreMaterial = CreateOrUpdateLineMaterial(
                MaterialFolder + "/BatonElectricWhiteCore.mat",
                "BatonElectricWhiteCore",
                new Color(0.48f, 0.90f, 1f, 1f));

            BuildEffectPrefab(
                ChargePrefabPath,
                "BatonChargeReadyVfx",
                effectType,
                0,
                haloMaterial,
                deepMaterial,
                blueMaterial,
                glowMaterial,
                coreMaterial);
            BuildEffectPrefab(
                DischargePrefabPath,
                "BatonDischargeVfx",
                effectType,
                1,
                haloMaterial,
                deepMaterial,
                blueMaterial,
                glowMaterial,
                coreMaterial);

            CreateSampleScene(batonSource, effectType, sequenceType);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Five-layer LineRenderer Unity electric baton VFX sample applied. " +
                "ChargePrefab=" + ChargePrefabPath +
                ", DischargePrefab=" + DischargePrefabPath +
                ", SampleScene=" + ScenePath +
                ", OriginalBatonAssetChanged=False, GameplaySceneChanged=False.");
        }

        [MenuItem("Bellerophon/Art Samples/Capture Unity Electric Baton VFX Review")]
        internal static void CaptureBatonElectricVfxUnitySampleReview()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ChargePrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(DischargePrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                throw new InvalidOperationException(
                    "Apply the Unity electric baton VFX sample before capture.");
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene sampleScene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            Texture2D chargeCapture = null;
            Texture2D dischargeCapture = null;
            try
            {
                Camera camera = RequireSceneComponent<Camera>(
                    sampleScene,
                    "BatonElectricVfxSampleCamera");
                Behaviour sequence = RequireSceneBehaviour(
                    sampleScene,
                    SequenceTypeName);
                sequence.enabled = false;

                GameObject charge = RequireSceneObject(
                    sampleScene,
                    "BatonChargeReadyVfx");
                GameObject discharge = RequireSceneObject(
                    sampleScene,
                    "BatonDischargeVfx");

                charge.SetActive(true);
                discharge.SetActive(false);
                InvokePreviewTime(charge, 0.78f);
                ConfigureChargeCamera(camera);
                chargeCapture = RenderCamera(camera, 900, 900);
                WritePng(chargeCapture, ChargeCaptureRelativePath);

                charge.SetActive(false);
                discharge.SetActive(true);
                InvokePreviewTime(discharge, 0.64f);
                ConfigureDischargeCamera(camera);
                dischargeCapture = RenderCamera(camera, 1200, 675);
                WritePng(dischargeCapture, DischargeCaptureRelativePath);

                Texture2D comparison = CreateComparison(
                    chargeCapture,
                    dischargeCapture);
                try
                {
                    WritePng(comparison, ComparisonCaptureRelativePath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(comparison);
                }

                Debug.Log(
                    "Unity electric baton VFX direct review captured from the " +
                    "standalone sample scene. Charge=" +
                    ChargeCaptureRelativePath + ", Discharge=" +
                    DischargeCaptureRelativePath + ", Comparison=" +
                    ComparisonCaptureRelativePath + ".");
            }
            finally
            {
                if (chargeCapture != null)
                {
                    UnityEngine.Object.DestroyImmediate(chargeCapture);
                }

                if (dischargeCapture != null)
                {
                    UnityEngine.Object.DestroyImmediate(dischargeCapture);
                }

                EditorSceneManager.CloseScene(sampleScene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static void BuildEffectPrefab(
            string prefabPath,
            string rootName,
            Type effectType,
            int mode,
            Material haloMaterial,
            Material deepMaterial,
            Material blueMaterial,
            Material glowMaterial,
            Material coreMaterial)
        {
            var root = new GameObject(rootName);
            try
            {
                Component controller = root.AddComponent(effectType);
                var haloLayers = new List<LineRenderer>();
                var deepLayers = new List<LineRenderer>();
                var blueLayers = new List<LineRenderer>();
                var glowLayers = new List<LineRenderer>();
                var coreLayers = new List<LineRenderer>();
                int groupCount = mode == 0 ? 11 : 8;
                for (int group = 0; group < groupCount; group++)
                {
                    bool mainDischarge = mode == 1 && group == 0;
                    bool collectorArc = mode == 1 && group >= 4;
                    float widthScale = mode == 0
                        ? 1f
                        : mainDischarge ? 1f : collectorArc ? 0.28f : 0.44f;
                    float deepWidth = (mode == 0 ? 0.014f : 0.115f) *
                        widthScale;
                    string groupName = mode == 0
                        ? $"ChargeArc_{group:00}"
                        : group == 0
                            ? "DischargeMain"
                            : group < 4
                                ? $"DischargeBranch_{group:00}"
                                : $"CollectorArc_{group - 3:00}";
                    CreateLineGroup(
                        root.transform,
                        groupName,
                        haloMaterial,
                        deepMaterial,
                        blueMaterial,
                        glowMaterial,
                        coreMaterial,
                        deepWidth,
                        haloLayers,
                        deepLayers,
                        blueLayers,
                        glowLayers,
                        coreLayers);
                }

                var serialized = new SerializedObject(controller);
                serialized.FindProperty("mode").enumValueIndex = mode;
                SetObjectArray(
                    serialized.FindProperty("haloLayers"),
                    haloLayers);
                SetObjectArray(
                    serialized.FindProperty("deepLayers"),
                    deepLayers);
                SetObjectArray(
                    serialized.FindProperty("blueLayers"),
                    blueLayers);
                SetObjectArray(
                    serialized.FindProperty("glowLayers"),
                    glowLayers);
                SetObjectArray(
                    serialized.FindProperty("coreLayers"),
                    coreLayers);
                serialized.FindProperty("dischargeRangeMeters").floatValue = 5f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateLineGroup(
            Transform parent,
            string groupName,
            Material haloMaterial,
            Material deepMaterial,
            Material blueMaterial,
            Material glowMaterial,
            Material coreMaterial,
            float deepWidth,
            ICollection<LineRenderer> haloLayers,
            ICollection<LineRenderer> deepLayers,
            ICollection<LineRenderer> blueLayers,
            ICollection<LineRenderer> glowLayers,
            ICollection<LineRenderer> coreLayers)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(parent, false);
            haloLayers.Add(CreateLineRenderer(
                group.transform,
                groupName + "_Halo",
                haloMaterial,
                deepWidth * 1.57f,
                0));
            deepLayers.Add(CreateLineRenderer(
                group.transform,
                groupName + "_Deep",
                deepMaterial,
                deepWidth,
                1));
            blueLayers.Add(CreateLineRenderer(
                group.transform,
                groupName + "_Blue",
                blueMaterial,
                deepWidth * 0.78f,
                2));
            glowLayers.Add(CreateLineRenderer(
                group.transform,
                groupName + "_Glow",
                glowMaterial,
                deepWidth * 0.39f,
                3));
            coreLayers.Add(CreateLineRenderer(
                group.transform,
                groupName + "_Core",
                coreMaterial,
                deepWidth * 0.15f,
                4));
        }

        private static LineRenderer CreateLineRenderer(
            Transform parent,
            string name,
            Material material,
            float width,
            int sortingOrder)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            LineRenderer renderer = item.AddComponent<LineRenderer>();
            renderer.useWorldSpace = false;
            renderer.loop = false;
            renderer.positionCount = 2;
            renderer.SetPosition(0, Vector3.zero);
            renderer.SetPosition(1, Vector3.right * 0.01f);
            renderer.alignment = LineAlignment.View;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.numCornerVertices = 3;
            renderer.numCapVertices = 3;
            renderer.startWidth = width;
            renderer.endWidth = width * 0.58f;
            renderer.startColor = Color.white;
            renderer.endColor = new Color(1f, 1f, 1f, 0.72f);
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        private static ParticleSystem[] CreateChargeParticleLayers(
            Transform parent,
            Material deepMaterial,
            Material cyanMaterial,
            Material whiteMaterial)
        {
            ParticleSystem outer = CreateChargeParticleSystem(
                parent,
                "ChargeOuterDrift",
                deepMaterial,
                7101u,
                180f,
                new ParticleSystem.MinMaxCurve(0.12f, 0.22f),
                new ParticleSystem.MinMaxCurve(0.18f, 0.38f),
                new ParticleSystem.MinMaxCurve(0.008f, 0.014f),
                18f,
                0.28f,
                true,
                0);
            ParticleSystem cyan = CreateChargeParticleSystem(
                parent,
                "ChargeCyanSparks",
                cyanMaterial,
                7102u,
                150f,
                new ParticleSystem.MinMaxCurve(0.10f, 0.20f),
                new ParticleSystem.MinMaxCurve(0.34f, 0.72f),
                new ParticleSystem.MinMaxCurve(0.006f, 0.011f),
                24f,
                0.38f,
                true,
                1);
            ParticleSystem core = CreateChargeParticleSystem(
                parent,
                "ChargeWhiteFlicker",
                whiteMaterial,
                7103u,
                105f,
                new ParticleSystem.MinMaxCurve(0.045f, 0.11f),
                new ParticleSystem.MinMaxCurve(0.44f, 0.95f),
                new ParticleSystem.MinMaxCurve(0.004f, 0.008f),
                30f,
                0.52f,
                false,
                2);
            return new[] { outer, cyan, core };
        }

        private static ParticleSystem CreateChargeParticleSystem(
            Transform parent,
            string name,
            Material material,
            uint seed,
            float emissionRate,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve size,
            float orbitalSpeed,
            float noiseStrength,
            bool useTrails,
            int sortingOrder)
        {
            ParticleSystem system = CreateBaseParticleSystem(
                parent,
                name,
                material,
                seed,
                1.2f,
                128,
                lifetime,
                speed,
                size,
                emissionRate,
                ParticleSystemRenderMode.Billboard,
                useTrails,
                sortingOrder);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, 0.15f, 0f);
            shape.scale = new Vector3(0.058f, 0.30f, 0.058f);
            shape.randomDirectionAmount = 1f;

            ParticleSystem.MainModule main = system.main;
            main.startSize3D = true;
            if (sortingOrder == 0)
            {
                main.startSizeX = new ParticleSystem.MinMaxCurve(0.052f, 0.105f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(0.008f, 0.015f);
                main.startSizeZ = new ParticleSystem.MinMaxCurve(0.008f, 0.015f);
            }
            else if (sortingOrder == 1)
            {
                main.startSizeX = new ParticleSystem.MinMaxCurve(0.042f, 0.086f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(0.006f, 0.012f);
                main.startSizeZ = new ParticleSystem.MinMaxCurve(0.006f, 0.012f);
            }
            else
            {
                main.startSizeX = new ParticleSystem.MinMaxCurve(0.026f, 0.058f);
                main.startSizeY = new ParticleSystem.MinMaxCurve(0.004f, 0.009f);
                main.startSizeZ = new ParticleSystem.MinMaxCurve(0.004f, 0.009f);
            }

            ParticleSystem.VelocityOverLifetimeModule velocity =
                system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(orbitalSpeed);
            velocity.radial = new ParticleSystem.MinMaxCurve(-0.04f, 0.025f);

            ConfigureNoise(system, noiseStrength, 3.1f, 0.72f);
            ParticleSystemRenderer renderer =
                system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (useTrails)
            {
                ParticleSystem.TrailModule trails = system.trails;
                trails.ratio = 0.50f;
                trails.lifetime = new ParticleSystem.MinMaxCurve(0.030f, 0.065f);
                trails.minVertexDistance = 0.004f;
            }

            return system;
        }

        private static ParticleSystem[] CreateDischargeParticleLayers(
            Transform parent,
            Material deepMaterial,
            Material cyanMaterial,
            Material whiteMaterial,
            Material pulseMaterial)
        {
            ParticleSystem outer = CreateDischargeStream(
                parent,
                "DischargeOuterFlow",
                deepMaterial,
                7201u,
                8,
                new ParticleSystem.MinMaxCurve(0.40f, 0.50f),
                new ParticleSystem.MinMaxCurve(9.6f, 11.2f),
                new ParticleSystem.MinMaxCurve(0.018f, 0.036f),
                0.60f,
                1.18f,
                0);
            ParticleSystem cyan = CreateDischargeStream(
                parent,
                "DischargeCyanFlow",
                cyanMaterial,
                7202u,
                7,
                new ParticleSystem.MinMaxCurve(0.38f, 0.47f),
                new ParticleSystem.MinMaxCurve(10.0f, 11.5f),
                new ParticleSystem.MinMaxCurve(0.012f, 0.025f),
                0.70f,
                0.96f,
                1);
            ParticleSystem core = CreateDischargeStream(
                parent,
                "DischargeWhiteCore",
                whiteMaterial,
                7203u,
                5,
                new ParticleSystem.MinMaxCurve(0.36f, 0.44f),
                new ParticleSystem.MinMaxCurve(10.4f, 11.8f),
                new ParticleSystem.MinMaxCurve(0.008f, 0.018f),
                0.50f,
                0.72f,
                2);
            ParticleSystem branches = CreateDischargeBranches(
                parent,
                cyanMaterial);
            ParticleSystem source = CreateDischargeSource(
                parent,
                pulseMaterial);
            return new[] { outer, cyan, core, branches, source };
        }

        private static ParticleSystem CreateDischargeStream(
            Transform parent,
            string name,
            Material material,
            uint seed,
            short burstCount,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve size,
            float lengthScale,
            float noiseStrength,
            int sortingOrder)
        {
            ParticleSystem system = CreateBaseParticleSystem(
                parent,
                name,
                material,
                seed,
                0.48f,
                160,
                lifetime,
                speed,
                size,
                0f,
                ParticleSystemRenderMode.Stretch,
                true,
                sortingOrder);
            system.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 2.6f;
            shape.radius = 0.006f;
            shape.radiusThickness = 1f;
            shape.length = 0.01f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(
                    0f,
                    burstCount,
                    burstCount,
                    10,
                    0.04f)
            });
            ConfigureNoise(system, noiseStrength, 6.8f, 2.4f);

            ParticleSystemRenderer renderer =
                system.GetComponent<ParticleSystemRenderer>();
            renderer.lengthScale = lengthScale;
            renderer.velocityScale = 0.04f;
            ParticleSystem.TrailModule trails = system.trails;
            trails.ratio = 0.62f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.035f, 0.080f);
            trails.minVertexDistance = 0.016f;
            return system;
        }

        private static ParticleSystem CreateDischargeBranches(
            Transform parent,
            Material material)
        {
            ParticleSystem system = CreateBaseParticleSystem(
                parent,
                "DischargeBranchSparks",
                material,
                7204u,
                0.48f,
                180,
                new ParticleSystem.MinMaxCurve(0.28f, 0.44f),
                new ParticleSystem.MinMaxCurve(7.2f, 10.5f),
                new ParticleSystem.MinMaxCurve(0.010f, 0.022f),
                0f,
                ParticleSystemRenderMode.Stretch,
                true,
                3);
            system.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 13f;
            shape.radius = 0.012f;
            shape.radiusThickness = 1f;
            shape.length = 0.015f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 5, 5, 10, 0.04f)
            });
            ConfigureNoise(system, 1.75f, 7.6f, 2.8f);
            ParticleSystemRenderer renderer =
                system.GetComponent<ParticleSystemRenderer>();
            renderer.lengthScale = 0.55f;
            renderer.velocityScale = 0.04f;
            ParticleSystem.TrailModule trails = system.trails;
            trails.ratio = 0.56f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            trails.minVertexDistance = 0.014f;
            return system;
        }

        private static ParticleSystem CreateDischargeSource(
            Transform parent,
            Material material)
        {
            ParticleSystem system = CreateBaseParticleSystem(
                parent,
                "DischargeCollectorPulse",
                material,
                7205u,
                0.32f,
                48,
                new ParticleSystem.MinMaxCurve(0.06f, 0.14f),
                new ParticleSystem.MinMaxCurve(0.08f, 0.35f),
                new ParticleSystem.MinMaxCurve(0.04f, 0.10f),
                0f,
                ParticleSystemRenderMode.Billboard,
                false,
                4);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.024f;
            shape.radiusThickness = 1f;
            shape.randomDirectionAmount = 1f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 6, 6, 10, 0.04f)
            });
            return system;
        }

        private static ParticleSystem CreateBaseParticleSystem(
            Transform parent,
            string name,
            Material material,
            uint seed,
            float duration,
            int maxParticles,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve size,
            float emissionRate,
            ParticleSystemRenderMode renderMode,
            bool useTrails,
            int sortingOrder)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            ParticleSystem system = item.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.useAutoRandomSeed = false;
            system.randomSeed = seed;

            ParticleSystem.MainModule main = system.main;
            main.duration = duration;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = maxParticles;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = Color.white;
            main.startRotation = new ParticleSystem.MinMaxCurve(
                0f,
                Mathf.PI * 2f);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            ParticleSystem.ColorOverLifetimeModule color =
                system.colorOverLifetime;
            color.enabled = true;
            color.color = CreateFadeGradient();

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime =
                system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.10f),
                    new Keyframe(0.08f, 1f),
                    new Keyframe(0.72f, 0.72f),
                    new Keyframe(1f, 0f)));

            ConfigureTrails(system, useTrails);

            ParticleSystemRenderer renderer =
                item.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = renderMode;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = material;
            renderer.trailMaterial = material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.cameraVelocityScale = 0f;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 1.8f;
            renderer.maxParticleSize = 0.35f;
            return system;
        }

        private static void ConfigureNoise(
            ParticleSystem system,
            float strength,
            float frequency,
            float scrollSpeed)
        {
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = new ParticleSystem.MinMaxCurve(
                strength * 0.42f,
                strength * 0.72f);
            noise.strengthY = new ParticleSystem.MinMaxCurve(
                strength * 0.72f,
                strength);
            noise.strengthZ = new ParticleSystem.MinMaxCurve(
                strength * 0.58f,
                strength * 0.86f);
            noise.frequency = frequency;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(scrollSpeed);
            noise.octaveCount = 2;
            noise.quality = ParticleSystemNoiseQuality.High;
            noise.damping = true;
        }

        private static void ConfigureTrails(
            ParticleSystem system,
            bool enabled)
        {
            ParticleSystem.TrailModule trails = system.trails;
            trails.enabled = enabled;
            if (!enabled)
            {
                return;
            }

            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 0.82f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            trails.minVertexDistance = 0.018f;
            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
            trails.worldSpace = false;
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.colorOverLifetime = CreateFadeGradient();
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.72f, 0.62f),
                    new Keyframe(1f, 0f)));
        }

        private static ParticleSystem.MinMaxGradient CreateFadeGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.08f, 0f),
                    new GradientAlphaKey(1f, 0.035f),
                    new GradientAlphaKey(0.38f, 0.20f),
                    new GradientAlphaKey(1f, 0.34f),
                    new GradientAlphaKey(0.48f, 0.55f),
                    new GradientAlphaKey(1f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void CreateSampleScene(
            GameObject batonSource,
            Type effectType,
            Type sequenceType)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene sampleScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            SceneManager.SetActiveScene(sampleScene);
            try
            {
                var sampleRoot = new GameObject("BatonElectricVfxSampleRoot");
                SceneManager.MoveGameObjectToScene(sampleRoot, sampleScene);

                var baton = PrefabUtility.InstantiatePrefab(
                    batonSource,
                    sampleScene) as GameObject;
                if (baton == null)
                {
                    throw new InvalidOperationException(
                        "The electric baton FBX could not be instantiated.");
                }

                baton.name = "ElectricBaton_Model_Locked";
                baton.transform.SetParent(sampleRoot.transform, true);
                SetAllRenderersVisible(baton);
                OrientLongestAxisVertically(baton);
                NormalizeBatonHeight(baton, 0.5f);
                Bounds batonBounds = CalculateBounds(baton);

                GameObject chargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    ChargePrefabPath);
                GameObject dischargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    DischargePrefabPath);
                var charge = PrefabUtility.InstantiatePrefab(
                    chargePrefab,
                    sampleScene) as GameObject;
                var discharge = PrefabUtility.InstantiatePrefab(
                    dischargePrefab,
                    sampleScene) as GameObject;
                if (charge == null || discharge == null)
                {
                    throw new InvalidOperationException(
                        "The electric baton VFX prefabs could not be instantiated.");
                }

                charge.name = "BatonChargeReadyVfx";
                discharge.name = "BatonDischargeVfx";
                charge.transform.SetParent(sampleRoot.transform, true);
                discharge.transform.SetParent(sampleRoot.transform, true);
                charge.transform.position = new Vector3(
                    batonBounds.center.x,
                    Mathf.Lerp(batonBounds.min.y, batonBounds.max.y, 0.32f),
                    batonBounds.center.z);
                discharge.transform.position = new Vector3(
                    batonBounds.center.x,
                    Mathf.Lerp(batonBounds.min.y, batonBounds.max.y, 0.91f),
                    batonBounds.center.z);

                Camera camera = CreateCamera(sampleScene, sampleRoot.transform);
                CreateLighting(sampleScene, sampleRoot.transform);

                var sequenceObject = new GameObject(
                    "BatonElectricVfxSampleSequence");
                sequenceObject.transform.SetParent(sampleRoot.transform, false);
                Component sequence = sequenceObject.AddComponent(sequenceType);
                Component chargeController = charge.GetComponent(effectType);
                Component dischargeController = discharge.GetComponent(effectType);
                var serialized = new SerializedObject(sequence);
                serialized.FindProperty("chargeReady").objectReferenceValue =
                    chargeController;
                serialized.FindProperty("discharge").objectReferenceValue =
                    dischargeController;
                serialized.FindProperty("sampleCamera").objectReferenceValue =
                    camera;
                serialized.FindProperty("cycleSeconds").floatValue = 4f;
                serialized.FindProperty("chargeSeconds").floatValue = 2.4f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                charge.SetActive(true);
                discharge.SetActive(false);
                SetLayerRecursively(sampleRoot, SampleCaptureLayer);
                ConfigureChargeCamera(camera);

                if (!EditorSceneManager.SaveScene(sampleScene, ScenePath))
                {
                    throw new InvalidOperationException(
                        "The electric baton VFX sample scene could not be saved: " +
                        ScenePath);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(sampleScene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static Camera CreateCamera(Scene scene, Transform parent)
        {
            var cameraObject = new GameObject("BatonElectricVfxSampleCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.transform.SetParent(parent, true);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.945f, 0.953f, 0.957f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.cullingMask = 1 << SampleCaptureLayer;
            ConfigureChargeCamera(camera);
            return camera;
        }

        private static void CreateLighting(Scene scene, Transform parent)
        {
            var keyObject = new GameObject("BatonElectricVfxKeyLight");
            SceneManager.MoveGameObjectToScene(keyObject, scene);
            keyObject.transform.SetParent(parent, true);
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.96f, 0.90f, 1f);
            key.intensity = 1.65f;
            key.shadows = LightShadows.Soft;
            key.cullingMask = 1 << SampleCaptureLayer;
            key.transform.rotation = Quaternion.Euler(32f, 28f, 0f);

            var fillObject = new GameObject("BatonElectricVfxFillLight");
            SceneManager.MoveGameObjectToScene(fillObject, scene);
            fillObject.transform.SetParent(parent, true);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.58f, 0.76f, 1f, 1f);
            fill.intensity = 1.1f;
            fill.shadows = LightShadows.None;
            fill.cullingMask = 1 << SampleCaptureLayer;
            fill.transform.rotation = Quaternion.Euler(334f, 214f, 0f);
        }

        private static void ConfigureChargeCamera(Camera camera)
        {
            Vector3 target = new Vector3(0f, 0.25f, 0f);
            camera.orthographicSize = 0.33f;
            camera.transform.position = target - Vector3.forward * 2f;
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.up);
        }

        private static void ConfigureDischargeCamera(Camera camera)
        {
            Vector3 target = new Vector3(2.48f, 0.25f, 0f);
            camera.orthographicSize = 1.55f;
            camera.transform.position = target - Vector3.forward * 4f;
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.up);
        }

        private static Texture2D RenderCamera(
            Camera camera,
            int width,
            int height)
        {
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            var result = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            float previousAspect = camera.aspect;
            try
            {
                target.Create();
                camera.targetTexture = target;
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.aspect = (float)width / height;
                RenderTexture.active = target;
                GL.Clear(true, true, camera.backgroundColor);
                camera.Render();
                RenderTexture.active = target;
                result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                result.Apply();
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
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Texture2D CreateComparison(
            Texture2D charge,
            Texture2D discharge)
        {
            const int width = 1600;
            const int height = 900;
            var result = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);
            Color32 background = new Color32(232, 237, 240, 255);
            var pixels = new Color32[width * height];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = background;
            }

            result.SetPixels32(pixels);
            CopyScaled(charge, result, 20, 90, 720, 720);
            CopyScaled(discharge, result, 760, 222, 820, 461);
            result.Apply();
            return result;
        }

        private static void CopyScaled(
            Texture2D source,
            Texture2D destination,
            int destinationX,
            int destinationY,
            int destinationWidth,
            int destinationHeight)
        {
            var colors = new Color[destinationWidth * destinationHeight];
            for (int y = 0; y < destinationHeight; y++)
            {
                float v = (y + 0.5f) / destinationHeight;
                for (int x = 0; x < destinationWidth; x++)
                {
                    float u = (x + 0.5f) / destinationWidth;
                    colors[y * destinationWidth + x] =
                        source.GetPixelBilinear(u, v);
                }
            }

            destination.SetPixels(
                destinationX,
                destinationY,
                destinationWidth,
                destinationHeight,
                colors);
        }

        private static void WritePng(Texture2D image, string relativePath)
        {
            string absolutePath = ProjectAbsolutePath(relativePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "The capture output folder is invalid."));
            File.WriteAllBytes(absolutePath, image.EncodeToPNG());
        }

        private static void DeleteParticleSampleAssets()
        {
            string[] particleMaterials =
            {
                MaterialFolder + "/BatonElectricParticleDeepBlue.mat",
                MaterialFolder + "/BatonElectricParticleCyan.mat",
                MaterialFolder + "/BatonElectricParticleWhiteArc.mat",
                MaterialFolder + "/BatonElectricParticleWhite.mat"
            };
            foreach (string path in particleMaterials)
            {
                DeleteAssetIfPresent(path);
            }

            DeleteAssetIfPresent(TextureFolder);
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null ||
                AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static Texture2D CreateOrUpdateParticleTexture(
            string path,
            int width,
            int height,
            bool radial)
        {
            var generated = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false,
                true);
            try
            {
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                {
                    float normalizedY =
                        ((y + 0.5f) / height - 0.5f) * 2f;
                    for (int x = 0; x < width; x++)
                    {
                        float normalizedX =
                            ((x + 0.5f) / width - 0.5f) * 2f;
                        float alpha;
                        if (radial)
                        {
                            float radius = Mathf.Sqrt(
                                normalizedX * normalizedX +
                                normalizedY * normalizedY);
                            float horizontal = Mathf.Pow(
                                Mathf.Clamp01(
                                    1f - Mathf.Abs(normalizedY) / 0.10f),
                                3.5f);
                            float vertical = Mathf.Pow(
                                Mathf.Clamp01(
                                    1f - Mathf.Abs(normalizedX) / 0.10f),
                                3.5f);
                            float diagonalA = Mathf.Pow(
                                Mathf.Clamp01(
                                    1f - Mathf.Abs(
                                        normalizedY - normalizedX) / 0.13f),
                                3.5f) * 0.72f;
                            float diagonalB = Mathf.Pow(
                                Mathf.Clamp01(
                                    1f - Mathf.Abs(
                                        normalizedY + normalizedX) / 0.13f),
                                3.5f) * 0.72f;
                            float rays = Mathf.Max(
                                Mathf.Max(horizontal, vertical),
                                Mathf.Max(diagonalA, diagonalB));
                            alpha = rays * Mathf.Pow(
                                Mathf.Clamp01(1f - radius),
                                0.75f);
                        }
                        else
                        {
                            float t = Mathf.Clamp01(
                                normalizedX * 0.5f + 0.5f);
                            float offsetPosition =
                                t * (LightningOffsets.Length - 1);
                            int offsetIndex = Mathf.Min(
                                Mathf.FloorToInt(offsetPosition),
                                LightningOffsets.Length - 2);
                            float center = Mathf.Lerp(
                                LightningOffsets[offsetIndex],
                                LightningOffsets[offsetIndex + 1],
                                offsetPosition - offsetIndex);
                            float core = Mathf.Pow(
                                Mathf.Clamp01(
                                    1f - Mathf.Abs(
                                        normalizedY - center) / 0.15f),
                                3.4f);
                            float endFade = Mathf.Pow(
                                Mathf.Clamp01(Mathf.Sin(Mathf.PI * t)),
                                0.42f);
                            float branchA = LightningSegmentAlpha(
                                new Vector2(normalizedX, normalizedY),
                                new Vector2(-0.52f, 0.04f),
                                new Vector2(-0.24f, 0.58f));
                            float branchB = LightningSegmentAlpha(
                                new Vector2(normalizedX, normalizedY),
                                new Vector2(0.02f, -0.20f),
                                new Vector2(0.30f, -0.66f));
                            float branchC = LightningSegmentAlpha(
                                new Vector2(normalizedX, normalizedY),
                                new Vector2(0.46f, 0.05f),
                                new Vector2(0.72f, 0.48f));
                            alpha = Mathf.Max(
                                core * endFade,
                                Mathf.Max(
                                    branchA,
                                    Mathf.Max(branchB, branchC)) * 0.78f);
                        }

                        byte intensity = (byte)Mathf.RoundToInt(
                            Mathf.Pow(Mathf.Clamp01(alpha), 0.42f) * 255f);
                        byte opacity = (byte)Mathf.RoundToInt(
                            Mathf.Clamp01(alpha) * 255f);
                        pixels[y * width + x] = new Color32(
                            intensity,
                            intensity,
                            intensity,
                            opacity);
                    }
                }

                generated.SetPixels32(pixels);
                generated.Apply();
                string absolutePath = ProjectAbsolutePath(path);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absolutePath) ??
                    throw new InvalidOperationException(
                        "The particle texture folder is invalid."));
                File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(generated);
            }

            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path)
                as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "The generated particle texture could not be imported: " +
                    path);
            }

            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                throw new InvalidOperationException(
                    "The generated particle texture is unavailable: " + path);
        }

        private static float LightningSegmentAlpha(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            float position = lengthSquared > Mathf.Epsilon
                ? Mathf.Clamp01(
                    Vector2.Dot(point - start, segment) / lengthSquared)
                : 0f;
            float distance = Vector2.Distance(
                point,
                start + segment * position);
            float width = Mathf.Pow(
                Mathf.Clamp01(1f - distance / 0.085f),
                3.2f);
            float taper = Mathf.Sin(Mathf.PI * position);
            return width * Mathf.Pow(Mathf.Clamp01(taper), 0.35f);
        }

        private static Material CreateOrUpdateLineMaterial(
            string path,
            string materialName,
            Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Color") ??
                            Shader.Find("Sprites/Default") ??
                            throw new InvalidOperationException(
                                "No supported unlit line shader was found.");
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
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)CullMode.Off);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateOrUpdateParticleMaterial(
            string path,
            string materialName,
            Color color,
            Texture texture,
            bool additive)
        {
            Shader shader = Shader.Find(
                                "Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit") ??
                            throw new InvalidOperationException(
                                "No supported particle shader was found.");
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
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", additive ? 1f : 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    additive
                        ? (float)BlendMode.One
                        : (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetObjectArray<T>(
            SerializedProperty property,
            IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }
        }

        private static void InvokePreviewTime(GameObject root, float previewTime)
        {
            Type effectType = RequireRuntimeType(EffectTypeName);
            Component controller = root.GetComponent(effectType) ??
                throw new InvalidOperationException(
                    root.name + " has no Unity sample controller.");
            InvokeMethod(controller, "SetPreviewTime", previewTime);
        }

        private static void InvokeMethod(
            Component component,
            string methodName,
            float argument)
        {
            MethodInfo method = component.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                throw new InvalidOperationException(
                    component.GetType().FullName + "." + methodName +
                    " was not found.");
            }

            method.Invoke(component, new object[] { argument });
        }

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = Type.GetType(fullName + ", " + RuntimeAssemblyName);
            if (type == null)
            {
                throw new InvalidOperationException(
                    "The Unity electric baton sample runtime type was not loaded: " +
                    fullName);
            }

            return type;
        }

        private static T RequireSceneComponent<T>(
            Scene scene,
            string objectName)
            where T : Component
        {
            GameObject gameObject = RequireSceneObject(scene, objectName);
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    objectName + " has no " + typeof(T).Name + ".");
            }

            return component;
        }

        private static Behaviour RequireSceneBehaviour(
            Scene scene,
            string typeName)
        {
            Type type = RequireRuntimeType(typeName);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Component component = root.GetComponentInChildren(type, true);
                if (component is Behaviour behaviour)
                {
                    return behaviour;
                }
            }

            throw new InvalidOperationException(
                "The sample scene behaviour was not found: " + typeName);
        }

        private static GameObject RequireSceneObject(
            Scene scene,
            string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in
                         root.GetComponentsInChildren<Transform>(true))
                {
                    if (string.Equals(
                        transform.name,
                        objectName,
                        StringComparison.Ordinal))
                    {
                        return transform.gameObject;
                    }
                }
            }

            throw new InvalidOperationException(
                "The sample scene object was not found: " + objectName);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)
                ?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) ||
                string.IsNullOrEmpty(folderName))
            {
                throw new InvalidOperationException(
                    "Invalid Unity sample folder: " + folderPath);
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void SetAllRenderersVisible(GameObject root)
        {
            root.SetActive(true);
            foreach (Transform transform in
                     root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.SetActive(true);
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "The electric baton contains no renderers.");
            }

            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.SetPropertyBlock(null);
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform transform in
                     root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = layer;
            }
        }

        private static void OrientLongestAxisVertically(GameObject root)
        {
            Bounds bounds = CalculateBounds(root);
            Vector3 size = bounds.size;
            if (size.x >= size.y && size.x >= size.z)
            {
                root.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
            else if (size.z >= size.x && size.z >= size.y)
            {
                root.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }
        }

        private static void NormalizeBatonHeight(GameObject root, float height)
        {
            Bounds before = CalculateBounds(root);
            if (before.size.y <= Mathf.Epsilon)
            {
                throw new InvalidOperationException(
                    "The electric baton has zero vertical extent.");
            }

            float scale = height / before.size.y;
            root.transform.localScale *= scale;
            Bounds afterScale = CalculateBounds(root);
            root.transform.position += new Vector3(
                -afterScale.center.x,
                -afterScale.min.y,
                -afterScale.center.z);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    root.name + " contains no renderers.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        }
    }
}
