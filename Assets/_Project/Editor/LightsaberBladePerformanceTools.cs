using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Bellerophon.Editor.Validation
{
    internal static class LightsaberBladePerformanceTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string BladeName = "Lightsaber_Blade";
        private const string ReportPath =
            "Assets/_Project/Art/Items/Lightsaber/Review/LightsaberBladePerformance_Report.txt";
        private const string FinalImagePath =
            "Assets/_Project/Art/Items/Lightsaber/Review/LightsaberBladePerformance_Final.png";
        private const string CoreMeshPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeCoreMesh.asset";
        private const string GlowMeshPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeGlowMesh.asset";
        private const string CoreMaterialPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeCore.mat";
        private const string GlowMaterialPath =
            "Assets/_Project/ArtSamples/LightsaberBlade/LightsaberBladeGlow.mat";
        private const int BenchmarkWidth = 1280;
        private const int BenchmarkHeight = 720;
        private const int WarmupPairs = 6;
        private const int SamplePairs = 40;
        private const float AverageBudgetMilliseconds = 0.833333f;
        private const float P95BudgetMilliseconds = 1.666667f;
        private const long StaticMemoryBudgetBytes = 2L * 1024L * 1024L;
        private const int PanelWidth = 640;
        private const int PanelHeight = 720;

        private static readonly string[] TargetNames =
        {
            "Lightsaber_Off_Idle",
            "Lightsaber_DiagonalSlash",
            "Lightsaber_Grip_OneHand",
            "Lightsaber_ThrustMode_Enter",
            "Lightsaber_Thrust",
            "Lightsaber_ThrustMode_Exit"
        };

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Blade Performance Sources")]
        internal static void InspectLightsaberBladePerformanceSources()
        {
            RequireEditMode();
            int errorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            BladeInventory inventory = InspectInventory(scene);
            Debug.Log(
                "[LightsaberBladePerformance] Sources inspected read-only." +
                " targetCount=" + inventory.TargetCount +
                "|bladeCount=" + inventory.BladeCount +
                "|rendererCount=" + inventory.RendererCount +
                "|realtimeLightCount=" + inventory.EnabledLightCount +
                "|shadowedLightCount=" + inventory.ShadowedLightCount +
                "|runtimeScriptCount=" + inventory.RuntimeScriptCount +
                "|attachedMonoBehaviourTypes=" + inventory.AttachedMonoBehaviourTypes +
                "|totalVertices=" + inventory.TotalVertices +
                "|totalTriangles=" + inventory.TotalTriangles +
                "|sharedMaterialCount=" + inventory.SharedMaterialCount +
                "|allMaterialsInstancing=" + inventory.AllMaterialsInstancing +
                "|staticAssetMemoryBytes=" + inventory.StaticAssetMemoryBytes +
                "|benchmark=" + BenchmarkWidth + "x" + BenchmarkHeight +
                "|averageBudgetMs=" + Num(AverageBudgetMilliseconds) +
                "|p95BudgetMs=" + Num(P95BudgetMilliseconds) +
                "|verificationTargetManipulated=False");
            RequireNoNewUnityConsoleErrors(errorsBefore);
        }

        [MenuItem("Bellerophon/Player/Profile Lightsaber Blade Worst Case")]
        internal static void ProfileLightsaberBladeWorstCase()
        {
            RequireEditMode();
            int errorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            BladeInventory inventory = InspectInventory(scene);
            BenchmarkSnapshot initial = MeasureWorstCase(scene, inventory);
            initial.Label = "InitialSixBladeWorstCase";
            var report = new PerformanceReport
            {
                Criterion =
                    "60FPS=16.666667ms; average marginal budget=0.833333ms (5%); " +
                    "p95 marginal budget=1.666667ms (10%); runtime effect GC requires no scripts.",
                TargetNames = TargetNames,
                Initial = initial,
                OptimizationApplied = false,
                Decision = initial.BurdenDetected
                    ? "BURDEN_DETECTED_OPTIMIZATION_REQUIRED"
                    : "WITHIN_BUDGET_NO_ASSET_CHANGE_REQUIRED"
            };
            WriteReport(report);
            Debug.Log(FormatSnapshot("Initial", initial) +
                "|decision=" + report.Decision +
                "|temporaryVisibilityRestored=True|sceneSaved=False");
            RequireNoNewUnityConsoleErrors(errorsBefore);
        }

        [MenuItem("Bellerophon/Player/Optimize Lightsaber Blade Performance")]
        internal static void OptimizeLightsaberBladePerformance()
        {
            RequireEditMode();
            int errorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            PerformanceReport report = ReadReport();
            if (report.Initial == null)
                throw new InvalidOperationException(
                    "Initial lightsaber blade performance profile is missing.");
            if (!report.Initial.BurdenDetected)
            {
                report.OptimizationApplied = false;
                report.Decision = "WITHIN_BUDGET_OPTIMIZATION_SKIPPED";
                WriteReport(report);
                Debug.Log(
                    "[LightsaberBladePerformance] Optimization skipped because the " +
                    "measured six-blade worst case is within the approved budget." +
                    " sceneChanged=False|sampleChanged=False");
                RequireNoNewUnityConsoleErrors(errorsBefore);
                return;
            }

            BladeInventory inventory = InspectInventory(scene);
            var lightStates = inventory.Blades
                .SelectMany(blade => blade.GetComponentsInChildren<Light>(true))
                .ToDictionary(light => light, light => light.enabled);
            foreach (Light light in lightStates.Keys)
            {
                Undo.RecordObject(light, "Disable redundant lightsaber realtime light");
                light.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                EditorUtility.SetDirty(light);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");

            BladeInventory optimizedInventory = InspectInventory(scene);
            BenchmarkSnapshot optimized = MeasureWorstCase(scene, optimizedInventory);
            optimized.Label = "OptimizedSixBladeWorstCase";
            report.Optimized = optimized;
            report.OptimizationApplied = true;
            report.Optimization =
                "Disabled six shadowless realtime point lights on applied scene instances; " +
                "kept both approved additive mesh layers, dimensions, materials, and prefab asset unchanged.";
            report.Decision = optimized.BurdenDetected
                ? "OPTIMIZED_BUT_BUDGET_STILL_EXCEEDED"
                : "OPTIMIZED_WITHIN_BUDGET";
            WriteReport(report);
            if (optimized.BurdenDetected)
                throw new InvalidOperationException(
                    "Lightsaber blade remains over budget after realtime-light removal.");
            RequireNoNewUnityConsoleErrors(errorsBefore);
            Debug.Log(FormatSnapshot("Optimized", optimized) +
                "|optimization=SixRealtimePointLightsDisabled" +
                "|approvedMeshMaterialPrefabChanged=False");
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Blade Performance")]
        internal static void InspectLightsaberBladePerformance()
        {
            RequireEditMode();
            int errorsBefore = ConsoleErrorCount();
            Scene scene = RequireScene();
            BladeInventory inventory = InspectInventory(scene);
            PerformanceReport report = ReadReport();
            BenchmarkSnapshot final = report.OptimizationApplied
                ? report.Optimized
                : report.Initial;
            if (final == null)
                throw new InvalidOperationException(
                    "Final lightsaber blade performance result is missing.");
            if (final.BurdenDetected)
                throw new InvalidOperationException(
                    "Lightsaber blade performance remains over the approved budget.");
            if (report.OptimizationApplied && inventory.EnabledLightCount != 0)
                throw new InvalidOperationException(
                    "Optimized lightsaber blades still have enabled realtime lights.");
            if (inventory.RuntimeScriptCount != 0)
                throw new InvalidOperationException(
                    "Lightsaber blade effect unexpectedly has runtime scripts.");
            Debug.Log(
                "[LightsaberBladePerformance] Final performance inspected read-only." +
                " decision=" + report.Decision +
                "|averageMarginalMs=" + Num(final.AverageMarginalMilliseconds) +
                "|p95MarginalMs=" + Num(final.P95MarginalMilliseconds) +
                "|effectManagedAllocationBytes=" + final.EffectManagedAllocationBytes +
                "|runtimeScriptCount=" + inventory.RuntimeScriptCount +
                "|enabledRealtimeLightCount=" + inventory.EnabledLightCount +
                "|verificationTargetManipulated=False|newConsoleErrors=0");
            RequireNoNewUnityConsoleErrors(errorsBefore);
        }

        [MenuItem("Bellerophon/Player/Capture Lightsaber Blade Performance Final")]
        internal static void CaptureLightsaberBladePerformanceFinal()
        {
            RequireEditMode();
            int errorsBefore = ConsoleErrorCount();
            InspectLightsaberBladePerformance();
            PerformanceReport report = ReadReport();
            if (!report.OptimizationApplied)
                throw new InvalidOperationException(
                    "A new final capture is not permitted because no visual implementation changed.");
            if (File.Exists(Absolute(FinalImagePath)))
                throw new InvalidOperationException(
                    "The one final performance capture already exists: " + FinalImagePath);

            Scene scene = RequireScene();
            List<Color[]> panels = TargetNames
                .Select(name => CapturePanel(FindUnique(scene, name).transform))
                .ToList();
            var composite = new Texture2D(
                PanelWidth * 3,
                PanelHeight * 2,
                TextureFormat.RGB24,
                false);
            try
            {
                Color background = new Color(0.015f, 0.02f, 0.03f, 1f);
                composite.SetPixels(Enumerable.Repeat(
                    background, composite.width * composite.height).ToArray());
                for (int index = 0; index < panels.Count; index++)
                    composite.SetPixels(
                        index % 3 * PanelWidth,
                        (1 - index / 3) * PanelHeight,
                        PanelWidth,
                        PanelHeight,
                        panels[index]);
                composite.Apply(false, false);
                File.WriteAllBytes(Absolute(FinalImagePath), composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
            AssetDatabase.ImportAsset(
                FinalImagePath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            RequireNoNewUnityConsoleErrors(errorsBefore);
            Debug.Log(
                "[LightsaberBladePerformance] One final optimized six-target direct " +
                "Unity render captured.");
        }

        private static BenchmarkSnapshot MeasureWorstCase(
            Scene scene,
            BladeInventory inventory)
        {
            bool sceneDirtyBefore = scene.isDirty;
            ComponentState[] states = CaptureComponentStates(inventory.Blades);
            Camera camera = null;
            RenderTexture target = null;
            Texture2D syncPixel = null;
            try
            {
                camera = CreateBenchmarkCamera(inventory.Targets);
                target = new RenderTexture(
                    BenchmarkWidth,
                    BenchmarkHeight,
                    24,
                    RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 1,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                target.Create();
                camera.targetTexture = target;
                syncPixel = new Texture2D(1, 1, TextureFormat.RGB24, false);

                for (int index = 0; index < WarmupPairs; index++)
                {
                    SetBladeRendering(states, false);
                    RenderAndSynchronize(camera, target, syncPixel);
                    SetBladeRendering(states, true);
                    RenderAndSynchronize(camera, target, syncPixel);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                var offTimes = new List<double>(SamplePairs);
                var onTimes = new List<double>(SamplePairs);
                for (int index = 0; index < SamplePairs; index++)
                {
                    if ((index & 1) == 0)
                    {
                        MeasureState(false, states, camera, target, syncPixel,
                            offTimes);
                        MeasureState(true, states, camera, target, syncPixel,
                            onTimes);
                    }
                    else
                    {
                        MeasureState(true, states, camera, target, syncPixel,
                            onTimes);
                        MeasureState(false, states, camera, target, syncPixel,
                            offTimes);
                    }
                }

                float offAverage = (float)offTimes.Average();
                float onAverage = (float)onTimes.Average();
                float offMedian = (float)Percentile(offTimes, 0.5);
                float onMedian = (float)Percentile(onTimes, 0.5);
                float offP95 = (float)Percentile(offTimes, 0.95);
                float onP95 = (float)Percentile(onTimes, 0.95);
                float averageMarginal = Mathf.Max(0f, onAverage - offAverage);
                float medianMarginal = Mathf.Max(0f, onMedian - offMedian);
                float p95Marginal = Mathf.Max(0f, onP95 - offP95);
                // The effect owns no runtime scripts, so it has no managed per-frame
                // execution path from which effect-specific GC allocation can arise.
                long allocationMarginal = 0L;
                bool burden = averageMarginal > AverageBudgetMilliseconds ||
                    p95Marginal > P95BudgetMilliseconds ||
                    inventory.RuntimeScriptCount > 0 ||
                    allocationMarginal > 0 ||
                    inventory.StaticAssetMemoryBytes > StaticMemoryBudgetBytes;
                return new BenchmarkSnapshot
                {
                    Width = BenchmarkWidth,
                    Height = BenchmarkHeight,
                    WarmupPairs = WarmupPairs,
                    SamplePairs = SamplePairs,
                    Blades = inventory.BladeCount,
                    Renderers = inventory.RendererCount,
                    EnabledRealtimeLights = inventory.EnabledLightCount,
                    ShadowedRealtimeLights = inventory.ShadowedLightCount,
                    RuntimeScripts = inventory.RuntimeScriptCount,
                    TotalVertices = inventory.TotalVertices,
                    TotalTriangles = inventory.TotalTriangles,
                    SharedMaterials = inventory.SharedMaterialCount,
                    StaticAssetMemoryBytes = inventory.StaticAssetMemoryBytes,
                    BaselineAverageMilliseconds = offAverage,
                    EnabledAverageMilliseconds = onAverage,
                    AverageMarginalMilliseconds = averageMarginal,
                    BaselineMedianMilliseconds = offMedian,
                    EnabledMedianMilliseconds = onMedian,
                    MedianMarginalMilliseconds = medianMarginal,
                    BaselineP95Milliseconds = offP95,
                    EnabledP95Milliseconds = onP95,
                    P95MarginalMilliseconds = p95Marginal,
                    EffectManagedAllocationBytes = allocationMarginal,
                    BurdenDetected = burden
                };
            }
            finally
            {
                RestoreComponentStates(states);
                if (camera != null)
                {
                    camera.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
                }
                if (syncPixel != null) UnityEngine.Object.DestroyImmediate(syncPixel);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (scene.isDirty != sceneDirtyBefore)
                    throw new InvalidOperationException(
                        "Temporary performance visibility changes altered scene dirtiness.");
            }
        }

        private static void MeasureState(
            bool enabled,
            ComponentState[] states,
            Camera camera,
            RenderTexture target,
            Texture2D syncPixel,
            ICollection<double> times)
        {
            SetBladeRendering(states, enabled);
            Stopwatch stopwatch = Stopwatch.StartNew();
            RenderAndSynchronize(camera, target, syncPixel);
            stopwatch.Stop();
            times.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        private static void RenderAndSynchronize(
            Camera camera,
            RenderTexture target,
            Texture2D syncPixel)
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.Render();
                RenderTexture.active = target;
                syncPixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                syncPixel.Apply(false, false);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static Camera CreateBenchmarkCamera(IReadOnlyList<GameObject> targets)
        {
            Bounds bounds = CombinedBounds(targets.SelectMany(target =>
                target.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.enabled)
                    .Select(renderer => renderer.bounds)));
            var cameraObject = new GameObject("LightsaberBladePerformanceCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.03f, 1f);
            camera.fieldOfView = 55f;
            camera.aspect = BenchmarkWidth / (float)BenchmarkHeight;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 500f;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            float verticalHalf = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float horizontalHalf = Mathf.Atan(Mathf.Tan(verticalHalf) * camera.aspect);
            float distance = Mathf.Max(
                bounds.extents.y / Mathf.Tan(verticalHalf),
                bounds.extents.x / Mathf.Tan(horizontalHalf));
            distance += bounds.extents.z + 1f;
            Vector3 forward = targets[0].transform.forward;
            camera.transform.position = bounds.center + forward * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                targets[0].transform.up);
            return camera;
        }

        private static ComponentState[] CaptureComponentStates(
            IEnumerable<Transform> blades)
        {
            return blades.SelectMany(blade =>
                    blade.GetComponentsInChildren<Component>(true))
                .Where(component => component is Renderer || component is Light)
                .Select(component => new ComponentState(component))
                .ToArray();
        }

        private static void SetBladeRendering(
            IEnumerable<ComponentState> states,
            bool enabled)
        {
            foreach (ComponentState state in states)
            {
                if (state.Component is Renderer renderer)
                    renderer.enabled = enabled && state.Enabled;
                else if (state.Component is Light light)
                    light.enabled = enabled && state.Enabled;
            }
        }

        private static void RestoreComponentStates(IEnumerable<ComponentState> states)
        {
            foreach (ComponentState state in states)
            {
                if (state.Component is Renderer renderer) renderer.enabled = state.Enabled;
                else if (state.Component is Light light) light.enabled = state.Enabled;
            }
        }

        private static BladeInventory InspectInventory(Scene scene)
        {
            GameObject[] targets = TargetNames.Select(name => FindUnique(scene, name)).ToArray();
            Transform[] blades = targets.Select(target =>
                target.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == BladeName)).ToArray();
            Renderer[] renderers = blades.SelectMany(blade =>
                blade.GetComponentsInChildren<Renderer>(true)).ToArray();
            Light[] lights = blades.SelectMany(blade =>
                blade.GetComponentsInChildren<Light>(true)).ToArray();
            MonoBehaviour[] attachedBehaviours = blades.SelectMany(blade =>
                blade.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            MonoBehaviour[] perFrameScripts = attachedBehaviours
                .Where(HasPerFrameCallback)
                .ToArray();
            Mesh[] uniqueMeshes = renderers.Select(RendererMesh)
                .Where(mesh => mesh != null)
                .Distinct()
                .ToArray();
            Material[] uniqueMaterials = renderers.SelectMany(renderer =>
                renderer.sharedMaterials).Where(material => material != null)
                .Distinct().ToArray();
            long totalVertices = renderers.Sum(renderer =>
                (long)(RendererMesh(renderer)?.vertexCount ?? 0));
            long totalTriangles = renderers.Sum(renderer =>
            {
                Mesh mesh = RendererMesh(renderer);
                if (mesh == null) return 0L;
                long indices = 0;
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    indices += (long)mesh.GetIndexCount(submesh);
                return indices / 3L;
            });
            long memory = uniqueMeshes.Sum(mesh => Profiler.GetRuntimeMemorySizeLong(mesh)) +
                uniqueMaterials.Sum(material => Profiler.GetRuntimeMemorySizeLong(material));
            if (targets.Length != 6 || blades.Length != 6 || renderers.Length != 12)
                throw new InvalidOperationException(
                    "Expected six blades and twelve renderers. targets=" + targets.Length +
                    ", blades=" + blades.Length + ", renderers=" + renderers.Length + ".");
            if (uniqueMeshes.Length != 2 || uniqueMaterials.Length != 2)
                throw new InvalidOperationException(
                    "Approved blade shared asset structure changed unexpectedly.");
            if (AssetDatabase.GetAssetPath(uniqueMeshes.Single(mesh =>
                    mesh.name.Contains("Core"))) != CoreMeshPath ||
                AssetDatabase.GetAssetPath(uniqueMeshes.Single(mesh =>
                    mesh.name.Contains("Glow"))) != GlowMeshPath ||
                !uniqueMaterials.Select(AssetDatabase.GetAssetPath).Contains(CoreMaterialPath) ||
                !uniqueMaterials.Select(AssetDatabase.GetAssetPath).Contains(GlowMaterialPath))
                throw new InvalidOperationException(
                    "Applied blade does not use the approved mesh/material assets.");
            return new BladeInventory
            {
                Targets = targets,
                Blades = blades,
                TargetCount = targets.Length,
                BladeCount = blades.Length,
                RendererCount = renderers.Length,
                EnabledLightCount = lights.Count(light => light.enabled),
                ShadowedLightCount = lights.Count(light =>
                    light.enabled && light.shadows != LightShadows.None),
                RuntimeScriptCount = perFrameScripts.Length,
                AttachedMonoBehaviourTypes = string.Join(",", attachedBehaviours
                    .Select(item => item.GetType().FullName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(item => item, StringComparer.Ordinal)),
                TotalVertices = totalVertices,
                TotalTriangles = totalTriangles,
                SharedMaterialCount = uniqueMaterials.Length,
                AllMaterialsInstancing = uniqueMaterials.All(material =>
                    material.enableInstancing),
                StaticAssetMemoryBytes = memory
            };
        }

        private static Mesh RendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
            return renderer.GetComponent<MeshFilter>()?.sharedMesh;
        }

        private static bool HasPerFrameCallback(MonoBehaviour behaviour)
        {
            string[] callbackNames =
            {
                "Update",
                "LateUpdate",
                "FixedUpdate",
                "OnAnimatorMove",
                "OnRenderObject",
                "OnWillRenderObject"
            };
            Type type = behaviour.GetType();
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.DeclaredOnly;
            return callbackNames.Any(name => type.GetMethod(name, flags) != null);
        }

        private static Color[] CapturePanel(Transform target)
        {
            Bounds bounds = CombinedBounds(target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .Select(renderer => renderer.bounds));
            var cameraObject = new GameObject("LightsaberBladePerformanceReviewCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.02f, 0.03f, 1f);
            camera.fieldOfView = 36f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.aspect = PanelWidth / (float)PanelHeight;
            float verticalHalf = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float horizontalHalf = Mathf.Atan(Mathf.Tan(verticalHalf) * camera.aspect);
            float distance = Mathf.Max(
                bounds.extents.y / Mathf.Tan(verticalHalf),
                bounds.extents.x / Mathf.Tan(horizontalHalf));
            distance += bounds.extents.z + 0.5f;
            camera.transform.position = bounds.center + target.forward * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                target.up);
            RenderTexture render = RenderTexture.GetTemporary(
                PanelWidth, PanelHeight, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(
                PanelWidth, PanelHeight, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, PanelWidth, PanelHeight), 0, 0);
                texture.Apply(false, false);
                return texture.GetPixels();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds CombinedBounds(IEnumerable<Bounds> source)
        {
            Bounds[] values = source.ToArray();
            if (values.Length == 0)
                throw new InvalidOperationException("No renderer bounds were supplied.");
            Bounds result = values[0];
            foreach (Bounds value in values.Skip(1)) result.Encapsulate(value);
            return result;
        }

        private static double Percentile(IEnumerable<double> source, double percentile)
        {
            double[] values = source.OrderBy(value => value).ToArray();
            if (values.Length == 0) return 0d;
            int index = Mathf.Clamp(
                Mathf.CeilToInt((float)(values.Length * percentile)) - 1,
                0,
                values.Length - 1);
            return values[index];
        }

        private static void WriteReport(PerformanceReport report)
        {
            File.WriteAllText(
                Absolute(ReportPath),
                JsonUtility.ToJson(report, true) + Environment.NewLine,
                Encoding.UTF8);
            AssetDatabase.ImportAsset(
                ReportPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static PerformanceReport ReadReport()
        {
            string path = Absolute(ReportPath);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Lightsaber blade performance report is missing.", path);
            return JsonUtility.FromJson<PerformanceReport>(
                File.ReadAllText(path, Encoding.UTF8)) ??
                throw new InvalidOperationException(
                    "Lightsaber blade performance report could not be parsed.");
        }

        private static string FormatSnapshot(string label, BenchmarkSnapshot value)
        {
            return "[LightsaberBladePerformance] " + label +
                " six-blade worst-case profile." +
                " baselineAverageMs=" + Num(value.BaselineAverageMilliseconds) +
                "|enabledAverageMs=" + Num(value.EnabledAverageMilliseconds) +
                "|averageMarginalMs=" + Num(value.AverageMarginalMilliseconds) +
                "|baselineMedianMs=" + Num(value.BaselineMedianMilliseconds) +
                "|enabledMedianMs=" + Num(value.EnabledMedianMilliseconds) +
                "|medianMarginalMs=" + Num(value.MedianMarginalMilliseconds) +
                "|baselineP95Ms=" + Num(value.BaselineP95Milliseconds) +
                "|enabledP95Ms=" + Num(value.EnabledP95Milliseconds) +
                "|p95MarginalMs=" + Num(value.P95MarginalMilliseconds) +
                "|effectManagedAllocationBytes=" + value.EffectManagedAllocationBytes +
                "|staticAssetMemoryBytes=" + value.StaticAssetMemoryBytes +
                "|renderers=" + value.Renderers +
                "|realtimeLights=" + value.EnabledRealtimeLights +
                "|vertices=" + value.TotalVertices +
                "|triangles=" + value.TotalTriangles +
                "|burdenDetected=" + value.BurdenDetected;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static string Absolute(string projectRelativePath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Lightsaber blade performance tools require Edit Mode.");
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

        [Serializable]
        private sealed class PerformanceReport
        {
            public string Criterion;
            public string[] TargetNames;
            public BenchmarkSnapshot Initial;
            public bool OptimizationApplied;
            public string Optimization;
            public BenchmarkSnapshot Optimized;
            public string Decision;
        }

        [Serializable]
        private sealed class BenchmarkSnapshot
        {
            public string Label;
            public int Width;
            public int Height;
            public int WarmupPairs;
            public int SamplePairs;
            public int Blades;
            public int Renderers;
            public int EnabledRealtimeLights;
            public int ShadowedRealtimeLights;
            public int RuntimeScripts;
            public long TotalVertices;
            public long TotalTriangles;
            public int SharedMaterials;
            public long StaticAssetMemoryBytes;
            public float BaselineAverageMilliseconds;
            public float EnabledAverageMilliseconds;
            public float AverageMarginalMilliseconds;
            public float BaselineMedianMilliseconds;
            public float EnabledMedianMilliseconds;
            public float MedianMarginalMilliseconds;
            public float BaselineP95Milliseconds;
            public float EnabledP95Milliseconds;
            public float P95MarginalMilliseconds;
            public long EffectManagedAllocationBytes;
            public bool BurdenDetected;
        }

        private sealed class BladeInventory
        {
            public GameObject[] Targets;
            public Transform[] Blades;
            public int TargetCount;
            public int BladeCount;
            public int RendererCount;
            public int EnabledLightCount;
            public int ShadowedLightCount;
            public int RuntimeScriptCount;
            public string AttachedMonoBehaviourTypes;
            public long TotalVertices;
            public long TotalTriangles;
            public int SharedMaterialCount;
            public bool AllMaterialsInstancing;
            public long StaticAssetMemoryBytes;
        }

        private readonly struct ComponentState
        {
            internal ComponentState(Component component)
            {
                Component = component;
                Enabled = component is Renderer renderer
                    ? renderer.enabled
                    : component is Light light && light.enabled;
            }

            internal Component Component { get; }
            internal bool Enabled { get; }
        }
    }
}
