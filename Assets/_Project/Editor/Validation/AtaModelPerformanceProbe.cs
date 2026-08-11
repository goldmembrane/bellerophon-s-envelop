using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Bellerophon.Editor.AtaPerformance
{
    [InitializeOnLoad]
    internal static class AtaModelPerformanceProbe
    {
        private const string ModelAssetPath =
            "Assets/_Project/Art/Enemies/Ata/PerformanceProbe/Ata_PerformanceProbe.fbx";
        private const string SourceRelativePath = "enemies model/attas.fbx";
        private const string OutputRelativeDirectory =
            "docs/validation/ata_model_performance_2026-08-11";
        private const string SessionStateKey =
            "Bellerophon.AtaModelPerformanceProbe.State";
        private const string SessionFailureKey =
            "Bellerophon.AtaModelPerformanceProbe.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Running = 2;
        private const int WaitingForEditMode = 3;
        private const double WarmupSeconds = 2.5d;
        private const double SampleSeconds = 8d;
        private const int MinimumSampleCount = 60;

        private static readonly int[] InstanceCounts = { 0, 1, 9, 18 };
        private static readonly List<GameObject> instances = new List<GameObject>();
        private static readonly List<AnimationClipPlayable> clipPlayables =
            new List<AnimationClipPlayable>();
        private static readonly List<double> frameTimesMs = new List<double>();
        private static readonly List<double> mainThreadTimesMs = new List<double>();
        private static readonly List<double> renderThreadTimesMs = new List<double>();
        private static readonly List<long> gcAllocBytes = new List<long>();

        private static ProbeDocument document;
        private static GameObject probeRoot;
        private static Transform modelsRoot;
        private static Camera probeCamera;
        private static AnimationClip selectedClip;
        private static PlayableGraph animationGraph;
        private static ProfilerRecorder mainThreadRecorder;
        private static ProfilerRecorder renderThreadRecorder;
        private static ProfilerRecorder gcAllocRecorder;
        private static double phaseStartTime;
        private static double sampleStartTime;
        private static bool sampling;
        private static int phaseIndex;
        private static int lastSampledFrame = -1;
        private static double currentSpawnMilliseconds;
        private static long currentMemoryBeforeBytes;
        private static long currentMemoryAfterBytes;
        private static int originalVSyncCount;
        private static int originalTargetFrameRate;

        static AtaModelPerformanceProbe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Ata performance probe must start from Edit Mode.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) == null)
            {
                throw new FileNotFoundException(
                    "The temporary Ata FBX has not been imported.",
                    ModelAssetPath);
            }

            DeletePreviousOutputs();
            SessionState.SetString(SessionFailureKey, string.Empty);
            SessionState.SetInt(SessionStateKey, WaitingForPlayMode);
            Debug.Log(
                "Ata performance probe queued: empty, 1, 9, and 18 instances; " +
                "2.5 s warmup and 8 s steady-state sampling per phase.");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            var state = SessionState.GetInt(SessionStateKey, 0);
            if (state == 0)
            {
                return;
            }

            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        return;
                    }

                    BeginRuntimeProbe();
                    SessionState.SetInt(SessionStateKey, Running);
                    return;
                }

                if (state == Running)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Unity left Play Mode before the Ata performance probe finished.");
                    }

                    UpdateRuntimeProbe();
                    return;
                }

                if (state == WaitingForEditMode &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    FinishAfterEditMode();
                }
            }
            catch (Exception exception)
            {
                HandleFailure(exception);
            }
        }

        private static void BeginRuntimeProbe()
        {
            var activeScene = SceneManager.GetActiveScene();
            document = new ProbeDocument
            {
                capturedAt = DateTime.Now.ToString("O"),
                unityVersion = Application.unityVersion,
                activeScenePath = activeScene.path,
                activeSceneWasDirty = activeScene.isDirty,
                existingSceneRootsActive = true,
                sourcePath = FullSourcePath,
                importedAssetPath = ModelAssetPath,
                sourceSha256 = ComputeSha256(FullSourcePath),
                importedSha256 = ComputeSha256(FullImportedPath),
                warmupSeconds = WarmupSeconds,
                sampleSeconds = SampleSeconds,
                environment = CaptureEnvironment(),
                model = InspectModel(),
                phases = new List<PhaseResult>()
            };

            if (!string.Equals(
                    document.sourceSha256,
                    document.importedSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The temporary imported FBX does not match the source FBX hash.");
            }

            if (document.model.rendererCount == 0 || document.model.vertexCount == 0)
            {
                throw new InvalidOperationException(
                    "The imported Ata FBX has no measurable render geometry.");
            }

            CreateProbeStage();
            originalVSyncCount = QualitySettings.vSyncCount;
            originalTargetFrameRate = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Time.timeScale = 1f;

            mainThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            renderThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Render Thread");
            gcAllocRecorder = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");

            var clips = LoadAnimationClips();
            selectedClip = clips.Count > 0 ? clips[0] : null;
            document.selectedAnimationClip = selectedClip != null ? selectedClip.name : string.Empty;
            document.animationPlaybackMode = selectedClip != null
                ? "Embedded clip looped with Animation Playables"
                : "Static model only: no embedded AnimationClip was imported";

            phaseIndex = 0;
            BeginPhase();
        }

        private static void CreateProbeStage()
        {
            probeRoot = new GameObject("Ata Performance Probe Runtime Root");
            modelsRoot = new GameObject("Ata Models").transform;
            modelsRoot.SetParent(probeRoot.transform, false);

            var cameraObject = new GameObject("Ata Probe Camera");
            cameraObject.transform.SetParent(probeRoot.transform, false);
            probeCamera = cameraObject.AddComponent<Camera>();
            probeCamera.clearFlags = CameraClearFlags.SolidColor;
            probeCamera.backgroundColor = new Color(0.045f, 0.055f, 0.075f, 1f);
            probeCamera.fieldOfView = 35f;
            probeCamera.nearClipPlane = 0.01f;
            probeCamera.farClipPlane = 5000f;
            probeCamera.depth = 100f;

            var keyLightObject = new GameObject("Ata Probe Key Light");
            keyLightObject.transform.SetParent(probeRoot.transform, false);
            keyLightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.25f;
            keyLight.color = new Color(1f, 0.94f, 0.86f);

            var fillLightObject = new GameObject("Ata Probe Fill Light");
            fillLightObject.transform.SetParent(probeRoot.transform, false);
            fillLightObject.transform.rotation = Quaternion.Euler(20f, 145f, 0f);
            var fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.65f;
            fillLight.color = new Color(0.62f, 0.76f, 1f);
        }

        private static void BeginPhase()
        {
            DisposeAnimationGraph();
            foreach (var instance in instances)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }

            instances.Clear();
            frameTimesMs.Clear();
            mainThreadTimesMs.Clear();
            renderThreadTimesMs.Clear();
            gcAllocBytes.Clear();
            sampling = false;
            lastSampledFrame = -1;

            var count = InstanceCounts[phaseIndex];
            currentMemoryBeforeBytes = Profiler.GetTotalAllocatedMemoryLong();
            var stopwatch = Stopwatch.StartNew();
            if (count > 0)
            {
                SpawnModels(count);
            }

            stopwatch.Stop();
            currentSpawnMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            currentMemoryAfterBytes = Profiler.GetTotalAllocatedMemoryLong();
            FrameCamera();
            phaseStartTime = Time.realtimeSinceStartupAsDouble;
            Debug.Log($"Ata performance phase started: {count} instance(s).");
        }

        private static void SpawnModels(int count)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            if (prefab == null)
            {
                throw new FileNotFoundException("Ata probe model prefab is missing.", ModelAssetPath);
            }

            var columns = Math.Min(6, count);
            var rows = Mathf.CeilToInt(count / (float)columns);
            for (var index = 0; index < count; index++)
            {
                var instance = Object.Instantiate(prefab, modelsRoot);
                instance.name = $"Ata_Probe_{index + 1:00}";
                NormalizeVisualHeight(instance, 2f);

                var column = index % columns;
                var row = index / columns;
                var x = (column - (columns - 1) * 0.5f) * 2.25f;
                var z = (row - (rows - 1) * 0.5f) * 2.1f;
                var bounds = CalculateBounds(instance);
                instance.transform.position += new Vector3(x, -bounds.min.y, z);
                instances.Add(instance);
            }

            if (selectedClip == null)
            {
                var clips = LoadAnimationClips();
                selectedClip = clips.Count > 0 ? clips[0] : null;
            }

            if (selectedClip == null)
            {
                return;
            }

            animationGraph = PlayableGraph.Create("Ata Performance Probe Animation Graph");
            animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            for (var index = 0; index < instances.Count; index++)
            {
                var instance = instances[index];
                var animator = instance.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var playable = AnimationClipPlayable.Create(animationGraph, selectedClip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                var output = AnimationPlayableOutput.Create(
                    animationGraph,
                    $"Ata_{index + 1:00}",
                    animator);
                output.SetSourcePlayable(playable);
                clipPlayables.Add(playable);
            }

            animationGraph.Play();
        }

        private static void UpdateRuntimeProbe()
        {
            var now = Time.realtimeSinceStartupAsDouble;
            if (selectedClip != null && selectedClip.length > 0f)
            {
                var localTime = (now - phaseStartTime) % selectedClip.length;
                foreach (var playable in clipPlayables)
                {
                    if (playable.IsValid())
                    {
                        playable.SetTime(localTime);
                    }
                }
            }

            if (!sampling)
            {
                if (now - phaseStartTime < WarmupSeconds)
                {
                    return;
                }

                sampling = true;
                sampleStartTime = now;
                frameTimesMs.Clear();
                mainThreadTimesMs.Clear();
                renderThreadTimesMs.Clear();
                gcAllocBytes.Clear();
                lastSampledFrame = -1;
                return;
            }

            if (Time.frameCount != lastSampledFrame)
            {
                lastSampledFrame = Time.frameCount;
                var frameMilliseconds = Time.unscaledDeltaTime * 1000d;
                if (frameMilliseconds > 0d && frameMilliseconds < 1000d)
                {
                    frameTimesMs.Add(frameMilliseconds);
                }

                AddRecorderTime(mainThreadRecorder, mainThreadTimesMs);
                AddRecorderTime(renderThreadRecorder, renderThreadTimesMs);
                if (gcAllocRecorder.Valid)
                {
                    gcAllocBytes.Add(Math.Max(0L, gcAllocRecorder.LastValue));
                }
            }

            if (now - sampleStartTime < SampleSeconds)
            {
                return;
            }

            CompletePhase();
        }

        private static void CompletePhase()
        {
            if (frameTimesMs.Count < MinimumSampleCount)
            {
                throw new InvalidOperationException(
                    $"Ata phase {InstanceCounts[phaseIndex]} produced only " +
                    $"{frameTimesMs.Count} frame samples.");
            }

            var result = new PhaseResult
            {
                instanceCount = InstanceCounts[phaseIndex],
                sampleCount = frameTimesMs.Count,
                spawnMilliseconds = currentSpawnMilliseconds,
                memoryBeforeSpawnBytes = currentMemoryBeforeBytes,
                memoryAfterSpawnBytes = currentMemoryAfterBytes,
                memoryDeltaBytes = currentMemoryAfterBytes - currentMemoryBeforeBytes,
                totalAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong(),
                totalReservedMemoryBytes = Profiler.GetTotalReservedMemoryLong(),
                monoUsedMemoryBytes = Profiler.GetMonoUsedSizeLong(),
                averageFrameMilliseconds = Average(frameTimesMs),
                p95FrameMilliseconds = Percentile(frameTimesMs, 0.95d),
                p99FrameMilliseconds = Percentile(frameTimesMs, 0.99d),
                maximumFrameMilliseconds = Maximum(frameTimesMs),
                averageFps = 1000d / Average(frameTimesMs),
                framesOver33Milliseconds = CountOver(frameTimesMs, 33.333d),
                framesOver50Milliseconds = CountOver(frameTimesMs, 50d),
                averageMainThreadMilliseconds = Average(mainThreadTimesMs),
                p99MainThreadMilliseconds = Percentile(mainThreadTimesMs, 0.99d),
                averageRenderThreadMilliseconds = Average(renderThreadTimesMs),
                p99RenderThreadMilliseconds = Percentile(renderThreadTimesMs, 0.99d),
                averageGcAllocatedBytesPerFrame = AverageLong(gcAllocBytes),
                maximumGcAllocatedBytesPerFrame = MaximumLong(gcAllocBytes)
            };
            document.phases.Add(result);
            Debug.Log(
                $"Ata phase completed: {result.instanceCount} instance(s), " +
                $"avg={result.averageFrameMilliseconds:F3} ms, " +
                $"p99={result.p99FrameMilliseconds:F3} ms, " +
                $"max={result.maximumFrameMilliseconds:F3} ms.");

            phaseIndex++;
            if (phaseIndex < InstanceCounts.Length)
            {
                BeginPhase();
                return;
            }

            WriteRawResults();
            CaptureFinalImage();
            DisposeRuntimeResources();
            SessionState.SetInt(SessionStateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void CaptureFinalImage()
        {
            if (File.Exists(FinalImagePath))
            {
                Debug.Log("Ata final capture already exists; keeping the single approved final capture.");
                return;
            }

            if (instances.Count != 18)
            {
                throw new InvalidOperationException(
                    "The final Ata capture requires the 18-instance phase.");
            }

            FrameCamera();
            const int width = 1920;
            const int height = 1080;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previousActive = RenderTexture.active;
            var previousTarget = probeCamera.targetTexture;
            try
            {
                probeCamera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                probeCamera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(FinalImagePath, texture.EncodeToPNG());
            }
            finally
            {
                probeCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(texture);
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }

        private static void FrameCamera()
        {
            if (probeCamera == null)
            {
                return;
            }

            if (instances.Count == 0)
            {
                probeCamera.transform.position = new Vector3(0f, 2f, -8f);
                probeCamera.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                return;
            }

            var bounds = CalculateBounds(modelsRoot.gameObject);
            var center = bounds.center;
            var verticalFov = probeCamera.fieldOfView * Mathf.Deg2Rad;
            var horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * (16f / 9f));
            var verticalDistance = bounds.extents.y / Mathf.Tan(verticalFov * 0.5f);
            var horizontalDistance = bounds.extents.x / Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(verticalDistance, horizontalDistance) + bounds.extents.z + 2f;
            probeCamera.transform.position = center + new Vector3(0f, bounds.extents.y * 0.12f, -distance);
            probeCamera.transform.LookAt(center + Vector3.up * bounds.extents.y * 0.08f);
        }

        private static void NormalizeVisualHeight(GameObject instance, float targetHeight)
        {
            var bounds = CalculateBounds(instance);
            if (bounds.size.y <= 0.0001f)
            {
                return;
            }

            var multiplier = targetHeight / bounds.size.y;
            instance.transform.localScale *= multiplier;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static ModelStatistics InspectModel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var skinnedRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            var animators = prefab.GetComponentsInChildren<Animator>(true);
            var meshes = new HashSet<Mesh>();
            var materials = new HashSet<Material>();
            var bones = new HashSet<Transform>();

            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        materials.Add(material);
                    }
                }
            }

            foreach (var renderer in skinnedRenderers)
            {
                if (renderer.sharedMesh != null)
                {
                    meshes.Add(renderer.sharedMesh);
                }

                foreach (var bone in renderer.bones)
                {
                    if (bone != null)
                    {
                        bones.Add(bone);
                    }
                }
            }

            foreach (var filter in meshFilters)
            {
                if (filter.sharedMesh != null)
                {
                    meshes.Add(filter.sharedMesh);
                }
            }

            long vertexCount = 0;
            long triangleCount = 0;
            var subMeshCount = 0;
            foreach (var mesh in meshes)
            {
                vertexCount += mesh.vertexCount;
                subMeshCount += mesh.subMeshCount;
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    triangleCount += (long)mesh.GetIndexCount(subMesh) / 3L;
                }
            }

            var clips = LoadAnimationClips();
            var clipNames = new string[clips.Count];
            var clipLengths = new double[clips.Count];
            for (var index = 0; index < clips.Count; index++)
            {
                clipNames[index] = clips[index].name;
                clipLengths[index] = clips[index].length;
            }

            return new ModelStatistics
            {
                fileSizeBytes = new FileInfo(FullImportedPath).Length,
                rendererCount = renderers.Length,
                skinnedMeshRendererCount = skinnedRenderers.Length,
                meshRendererCount = renderers.Length - skinnedRenderers.Length,
                animatorCount = animators.Length,
                uniqueMeshCount = meshes.Count,
                materialCount = materials.Count,
                boneCount = bones.Count,
                vertexCount = vertexCount,
                triangleCount = triangleCount,
                subMeshCount = subMeshCount,
                animationClipCount = clips.Count,
                animationClipNames = clipNames,
                animationClipLengthsSeconds = clipLengths,
                importedDependencies = AssetDatabase.GetDependencies(ModelAssetPath, true)
            };
        }

        private static List<AnimationClip> LoadAnimationClips()
        {
            var clips = new List<AnimationClip>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelAssetPath))
            {
                if (asset is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    clip.length > 0f)
                {
                    clips.Add(clip);
                }
            }

            return clips;
        }

        private static ProbeEnvironment CaptureEnvironment()
        {
            return new ProbeEnvironment
            {
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                processorFrequencyMHz = SystemInfo.processorFrequency,
                systemMemoryMB = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsMemoryMB = SystemInfo.graphicsMemorySize,
                graphicsApiVersion = SystemInfo.graphicsDeviceVersion,
                editorPlayMode = Application.isEditor,
                screenWidth = Screen.width,
                screenHeight = Screen.height
            };
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string markerName)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, markerName, 32);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Ata probe could not start profiler recorder '{markerName}': {exception.Message}");
                return default;
            }
        }

        private static void AddRecorderTime(ProfilerRecorder recorder, List<double> target)
        {
            if (recorder.Valid && recorder.LastValue > 0L)
            {
                target.Add(recorder.LastValue / 1000000d);
            }
        }

        private static void WriteRawResults()
        {
            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllText(
                RawJsonPath,
                JsonUtility.ToJson(document, true));
        }

        private static void DisposeRuntimeResources()
        {
            DisposeAnimationGraph();
            if (mainThreadRecorder.Valid)
            {
                mainThreadRecorder.Dispose();
            }

            if (renderThreadRecorder.Valid)
            {
                renderThreadRecorder.Dispose();
            }

            if (gcAllocRecorder.Valid)
            {
                gcAllocRecorder.Dispose();
            }

            QualitySettings.vSyncCount = originalVSyncCount;
            Application.targetFrameRate = originalTargetFrameRate;
        }

        private static void DisposeAnimationGraph()
        {
            clipPlayables.Clear();
            if (animationGraph.IsValid())
            {
                animationGraph.Destroy();
            }
        }

        private static void FinishAfterEditMode()
        {
            var failure = SessionState.GetString(SessionFailureKey, string.Empty);
            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllText(
                CompletionPath,
                string.IsNullOrWhiteSpace(failure)
                    ? "SUCCESS\n"
                    : "FAILED\n" + failure);
            SessionState.SetInt(SessionStateKey, 0);
            SessionState.EraseString(SessionFailureKey);
            Debug.Log(
                string.IsNullOrWhiteSpace(failure)
                    ? "Ata performance probe completed."
                    : "Ata performance probe failed: " + failure);
        }

        private static void HandleFailure(Exception exception)
        {
            Debug.LogException(exception);
            SessionState.SetString(SessionFailureKey, exception.ToString());
            try
            {
                DisposeRuntimeResources();
            }
            catch (Exception cleanupException)
            {
                Debug.LogException(cleanupException);
            }

            SessionState.SetInt(SessionStateKey, WaitingForEditMode);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
            else if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                FinishAfterEditMode();
            }
        }

        private static void DeletePreviousOutputs()
        {
            Directory.CreateDirectory(OutputDirectory);
            TryDelete(RawJsonPath);
            TryDelete(CompletionPath);
        }

        private static void TryDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static double Average(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0d;
            }

            double sum = 0d;
            foreach (var value in values)
            {
                sum += value;
            }

            return sum / values.Count;
        }

        private static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0)
            {
                return 0d;
            }

            var sorted = values.ToArray();
            Array.Sort(sorted);
            var index = Math.Max(0, Math.Min(
                sorted.Length - 1,
                (int)Math.Ceiling(percentile * sorted.Length) - 1));
            return sorted[index];
        }

        private static double Maximum(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0d;
            }

            var maximum = double.MinValue;
            foreach (var value in values)
            {
                maximum = Math.Max(maximum, value);
            }

            return maximum;
        }

        private static int CountOver(List<double> values, double threshold)
        {
            var count = 0;
            foreach (var value in values)
            {
                if (value > threshold)
                {
                    count++;
                }
            }

            return count;
        }

        private static double AverageLong(List<long> values)
        {
            if (values.Count == 0)
            {
                return 0d;
            }

            double sum = 0d;
            foreach (var value in values)
            {
                sum += value;
            }

            return sum / values.Count;
        }

        private static long MaximumLong(List<long> values)
        {
            long maximum = 0L;
            foreach (var value in values)
            {
                maximum = Math.Max(maximum, value);
            }

            return maximum;
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string OutputDirectory =>
            Path.Combine(ProjectRoot, OutputRelativeDirectory);

        private static string RawJsonPath =>
            Path.Combine(OutputDirectory, "Ata_Performance_Raw.json");

        private static string FinalImagePath =>
            Path.Combine(OutputDirectory, "Ata_18Instances_Final.png");

        private static string CompletionPath =>
            Path.Combine(OutputDirectory, "Ata_Performance_Probe.complete");

        private static string FullSourcePath =>
            Path.Combine(ProjectRoot, SourceRelativePath);

        private static string FullImportedPath =>
            Path.Combine(ProjectRoot, ModelAssetPath);

        [Serializable]
        private sealed class ProbeDocument
        {
            public string capturedAt;
            public string unityVersion;
            public string activeScenePath;
            public bool activeSceneWasDirty;
            public bool existingSceneRootsActive;
            public string sourcePath;
            public string importedAssetPath;
            public string sourceSha256;
            public string importedSha256;
            public double warmupSeconds;
            public double sampleSeconds;
            public string selectedAnimationClip;
            public string animationPlaybackMode;
            public ProbeEnvironment environment;
            public ModelStatistics model;
            public List<PhaseResult> phases;
        }

        [Serializable]
        private sealed class ProbeEnvironment
        {
            public string operatingSystem;
            public string processorType;
            public int processorCount;
            public int processorFrequencyMHz;
            public int systemMemoryMB;
            public string graphicsDeviceName;
            public string graphicsDeviceType;
            public int graphicsMemoryMB;
            public string graphicsApiVersion;
            public bool editorPlayMode;
            public int screenWidth;
            public int screenHeight;
        }

        [Serializable]
        private sealed class ModelStatistics
        {
            public long fileSizeBytes;
            public int rendererCount;
            public int skinnedMeshRendererCount;
            public int meshRendererCount;
            public int animatorCount;
            public int uniqueMeshCount;
            public int materialCount;
            public int boneCount;
            public long vertexCount;
            public long triangleCount;
            public int subMeshCount;
            public int animationClipCount;
            public string[] animationClipNames;
            public double[] animationClipLengthsSeconds;
            public string[] importedDependencies;
        }

        [Serializable]
        private sealed class PhaseResult
        {
            public int instanceCount;
            public int sampleCount;
            public double spawnMilliseconds;
            public long memoryBeforeSpawnBytes;
            public long memoryAfterSpawnBytes;
            public long memoryDeltaBytes;
            public long totalAllocatedMemoryBytes;
            public long totalReservedMemoryBytes;
            public long monoUsedMemoryBytes;
            public double averageFrameMilliseconds;
            public double p95FrameMilliseconds;
            public double p99FrameMilliseconds;
            public double maximumFrameMilliseconds;
            public double averageFps;
            public int framesOver33Milliseconds;
            public int framesOver50Milliseconds;
            public double averageMainThreadMilliseconds;
            public double p99MainThreadMilliseconds;
            public double averageRenderThreadMilliseconds;
            public double p99RenderThreadMilliseconds;
            public double averageGcAllocatedBytesPerFrame;
            public long maximumGcAllocatedBytesPerFrame;
        }
    }
}
