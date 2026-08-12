using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    [InitializeOnLoad]
    internal static class AtaCommandStanceAlternationPlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_05_Command";
        private const string ModelName = "Ata_Model";
        private const string GuardianEffectName = "Kursa_ShieldStanceIcon";
        private const string BreakthroughEffectName = "Ata_BreakthroughStanceEffect";
        private const string StateFileName = "AtaCommandStanceAlternationCapture.state";
        private const string ResultFileName = "AtaCommandStanceAlternationCapture.result";
        private const string StateEnteringPlayMode = "EnteringPlayMode";
        private const string StateExitingPlayMode = "ExitingPlayMode";
        private const string StateFailedExitingPlayMode = "FailedExitingPlayMode";
        private const int CaptureWidth = 720;
        private const int CaptureHeight = 720;
        private const int CaptureFrameRate = 20;
        private const int CaptureLayer = 31;

        private static Action<string> complete;
        private static Action<Exception> fail;
        private static CaptureSession session;

        static AtaCommandStanceAlternationPlayModeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Start(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Cannot start the Ata command capture while Unity is entering or running Play Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active and clean before the Ata command Play Mode capture.");
            }

            DeleteStateFiles();
            var outputDirectory = Path.Combine(
                ProjectRoot,
                "docs",
                "validation",
                "ata05_command_stance_alternation_2026-08-12",
                "actual_playmode_motion");
            Directory.CreateDirectory(outputDirectory);
            var videoPath = Path.Combine(
                outputDirectory,
                "Ata_05_CommandStanceAlternation_ThreeLoops.mp4");
            var metricsPath = Path.Combine(
                outputDirectory,
                "Ata_05_CommandStanceAlternation_Frames.csv");
            TryDelete(videoPath);
            TryDelete(metricsPath);
            WriteState(new CaptureState
            {
                Phase = StateEnteringPlayMode,
                VideoPath = videoPath,
                MetricsPath = metricsPath
            });
            EditorApplication.EnterPlaymode();
        }

        public static void Resume(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            Tick();
        }

        private static void Tick()
        {
            if (complete == null && fail == null)
            {
                return;
            }

            var state = ReadState();
            if (state == null)
            {
                return;
            }

            try
            {
                if (state.Phase == StateEnteringPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        return;
                    }

                    session ??= new CaptureSession(state.VideoPath, state.MetricsPath);
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (!session.Tick())
                    {
                        return;
                    }

                    state.Summary = session.Complete();
                    File.WriteAllText(ResultPath, state.Summary, new UTF8Encoding(false));
                    session.Dispose();
                    session = null;
                    state.Phase = StateExitingPlayMode;
                    WriteState(state);
                    EditorApplication.ExitPlaymode();
                    return;
                }

                if (state.Phase == StateExitingPlayMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        return;
                    }

                    var callback = complete;
                    var summary = File.Exists(ResultPath)
                        ? File.ReadAllText(ResultPath)
                        : state.Summary;
                    CleanupCallbacks();
                    DeleteStateFiles();
                    callback?.Invoke(
                        "Ata command actual Play Mode three-loop capture completed. " + summary);
                    return;
                }

                if (state.Phase == StateFailedExitingPlayMode &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    var callback = fail;
                    var error = state.Error;
                    CleanupCallbacks();
                    DeleteStateFiles();
                    callback?.Invoke(new InvalidOperationException(error));
                }
            }
            catch (Exception exception)
            {
                session?.Dispose();
                session = null;
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    state.Phase = StateFailedExitingPlayMode;
                    state.Error = exception.ToString();
                    WriteState(state);
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.ExitPlaymode();
                    }
                }
                else
                {
                    var callback = fail;
                    CleanupCallbacks();
                    DeleteStateFiles();
                    callback?.Invoke(exception);
                }
            }
        }

        private sealed class CaptureSession : IDisposable
        {
            private readonly GameObject placement;
            private readonly Transform slot;
            private readonly Transform model;
            private readonly Animator animator;
            private readonly SpriteRenderer guardian;
            private readonly SpriteRenderer breakthrough;
            private readonly bool placementWasActive;
            private readonly bool slotWasActive;
            private readonly bool modelWasActive;
            private readonly float previousAnimatorSpeed;
            private readonly LayerState[] layerStates;
            private readonly GameObject cameraObject;
            private readonly Camera camera;
            private readonly GameObject keyLightObject;
            private readonly GameObject fillLightObject;
            private readonly RenderTexture renderTexture;
            private readonly Texture2D readback;
            private readonly FileStream rawVideo;
            private readonly string rawVideoPath;
            private readonly string videoPath;
            private readonly string metricsPath;
            private readonly StringBuilder metrics = new StringBuilder();
            private readonly double initializedAt;
            private double startedAt;
            private double nextCaptureAt;
            private bool captureStarted;
            private int capturedFrames;
            private int guardianFrames;
            private int breakthroughFrames;
            private int simultaneousVisibleFrames;
            private int invisibleFrames;
            private int highestLoop;

            public CaptureSession(string videoPath, string metricsPath)
            {
                this.videoPath = videoPath;
                this.metricsPath = metricsPath;
                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid() || scene.path != ScenePath)
                {
                    throw new InvalidOperationException(
                        "Play Mode active scene must stay CargoRunMvp.");
                }

                placement = scene.GetRootGameObjects()
                    .SingleOrDefault(item => item.name == PlacementRootName) ??
                    throw new InvalidOperationException("Approved Ata placement is missing.");
                placementWasActive = placement.activeSelf;
                placement.SetActive(true);
                slot = placement.transform.Find(SlotName) ??
                       throw new InvalidOperationException("Ata_05_Command is missing.");
                slotWasActive = slot.gameObject.activeSelf;
                slot.gameObject.SetActive(true);
                model = slot.Find(ModelName) ??
                        throw new InvalidOperationException("Ata_05_Command/Ata_Model is missing.");
                modelWasActive = model.gameObject.activeSelf;
                model.gameObject.SetActive(true);
                animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException("Ata command Animator is missing.");
                guardian = model.GetComponentsInChildren<SpriteRenderer>(true)
                    .SingleOrDefault(item => item.name == GuardianEffectName) ??
                    throw new InvalidOperationException("Ata guardian effect is missing.");
                breakthrough = model.GetComponentsInChildren<SpriteRenderer>(true)
                    .SingleOrDefault(item => item.name == BreakthroughEffectName) ??
                    throw new InvalidOperationException("Ata breakthrough effect is missing.");
                previousAnimatorSpeed = animator.speed;
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                animator.speed = 0f;
                animator.Play("AtaCommand", 0, 0f);
                animator.Update(0f);

                layerStates = SetLayerRecursively(slot, CaptureLayer);
                cameraObject = new GameObject("Ata Command Stance Actual Play Mode Camera")
                    { hideFlags = HideFlags.DontSave };
                camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.024f, 0.03f, 1f);
                camera.fieldOfView = 30f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                keyLightObject = CreateLight(
                    "Ata Command Capture Key Light",
                    Quaternion.Euler(35f, 25f, 0f),
                    1.8f);
                fillLightObject = CreateLight(
                    "Ata Command Capture Fill Light",
                    Quaternion.Euler(15f, -130f, 0f),
                    0.9f);
                renderTexture = new RenderTexture(
                    CaptureWidth,
                    CaptureHeight,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                readback = new Texture2D(
                    CaptureWidth,
                    CaptureHeight,
                    TextureFormat.RGB24,
                    false);
                FrameCamera(camera, model, CalculateBounds(model));

                rawVideoPath = Path.ChangeExtension(videoPath, ".rgb24");
                TryDelete(rawVideoPath);
                rawVideo = new FileStream(
                    rawVideoPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    1024 * 1024,
                    FileOptions.SequentialScan);
                metrics.AppendLine(
                    "frame,elapsedSeconds,normalizedTime,loopIndex,guardianVisible,breakthroughVisible");
                initializedAt = Time.realtimeSinceStartupAsDouble;
            }

            public bool Tick()
            {
                var now = Time.realtimeSinceStartupAsDouble;
                if (!captureStarted)
                {
                    if (now - initializedAt < 0.5d)
                    {
                        return false;
                    }

                    animator.Play("AtaCommand", 0, 0f);
                    animator.Update(0f);
                    animator.speed = 1f;
                    captureStarted = true;
                    startedAt = now;
                    nextCaptureAt = now;
                }

                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.normalizedTime >= 3.02f && capturedFrames > 0)
                {
                    return true;
                }

                if (now - startedAt > 20d)
                {
                    throw new TimeoutException(
                        "The Ata command Animator did not complete three loops in time.");
                }

                if (now + 0.0001d < nextCaptureAt)
                {
                    return false;
                }

                CaptureFrame(now - startedAt, state);
                nextCaptureAt += 1d / CaptureFrameRate;
                if (nextCaptureAt < now - 0.1d)
                {
                    nextCaptureAt = now + 1d / CaptureFrameRate;
                }

                return false;
            }

            public string Complete()
            {
                rawVideo.Flush();
                rawVideo.Dispose();
                EncodeRawVideo();
                File.WriteAllText(metricsPath, metrics.ToString(), new UTF8Encoding(false));
                if (highestLoop < 2 || guardianFrames == 0 || breakthroughFrames == 0 ||
                    simultaneousVisibleFrames != 0 || invisibleFrames != 0)
                {
                    throw new InvalidOperationException(
                        "Ata command stance alternation capture contract failed.");
                }

                return "ActualPlayMode=True, IsolatedAtaSlot5=True" +
                       ", Loops=3" +
                       ", CapturedFrames=" + capturedFrames +
                       ", GuardianVisibleFrames=" + guardianFrames +
                       ", BreakthroughVisibleFrames=" + breakthroughFrames +
                       ", SimultaneousVisibleFrames=" + simultaneousVisibleFrames +
                       ", InvisibleFrames=" + invisibleFrames +
                       ", Order=Guardian,Breakthrough,Guardian" +
                       ", Video=" + videoPath +
                       ", Metrics=" + metricsPath;
            }

            private void CaptureFrame(double elapsed, AnimatorStateInfo state)
            {
                var guardianVisible = guardian.enabled && guardian.gameObject.activeInHierarchy;
                var breakthroughVisible = breakthrough.enabled &&
                                          breakthrough.gameObject.activeInHierarchy;
                if (guardianVisible)
                {
                    guardianFrames++;
                }

                if (breakthroughVisible)
                {
                    breakthroughFrames++;
                }

                if (guardianVisible && breakthroughVisible)
                {
                    simultaneousVisibleFrames++;
                }

                if (!guardianVisible && !breakthroughVisible)
                {
                    invisibleFrames++;
                }

                var loopIndex = Mathf.Max(0, Mathf.FloorToInt(state.normalizedTime));
                highestLoop = Mathf.Max(highestLoop, loopIndex);
                var expectedGuardian = loopIndex % 2 == 0;
                if (guardianVisible != expectedGuardian ||
                    breakthroughVisible == expectedGuardian)
                {
                    throw new InvalidOperationException(
                        "Ata command stance effect order differs at normalized time " +
                        state.normalizedTime.ToString("0.######", CultureInfo.InvariantCulture) + ".");
                }

                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                readback.ReadPixels(
                    new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                    0,
                    0,
                    false);
                readback.Apply(false, false);
                RenderTexture.active = previous;
                rawVideo.Write(readback.GetRawTextureData<byte>());
                metrics.AppendLine(string.Join(",",
                    capturedFrames.ToString(CultureInfo.InvariantCulture),
                    elapsed.ToString("0.######", CultureInfo.InvariantCulture),
                    state.normalizedTime.ToString("0.######", CultureInfo.InvariantCulture),
                    loopIndex.ToString(CultureInfo.InvariantCulture),
                    guardianVisible,
                    breakthroughVisible));
                capturedFrames++;
            }

            private void EncodeRawVideo()
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                startInfo.ArgumentList.Add("-y");
                startInfo.ArgumentList.Add("-f");
                startInfo.ArgumentList.Add("rawvideo");
                startInfo.ArgumentList.Add("-pixel_format");
                startInfo.ArgumentList.Add("rgb24");
                startInfo.ArgumentList.Add("-video_size");
                startInfo.ArgumentList.Add(CaptureWidth + "x" + CaptureHeight);
                startInfo.ArgumentList.Add("-framerate");
                startInfo.ArgumentList.Add(CaptureFrameRate.ToString(CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add("-i");
                startInfo.ArgumentList.Add(rawVideoPath);
                startInfo.ArgumentList.Add("-vf");
                startInfo.ArgumentList.Add("vflip");
                startInfo.ArgumentList.Add("-c:v");
                startInfo.ArgumentList.Add("libx264");
                startInfo.ArgumentList.Add("-pix_fmt");
                startInfo.ArgumentList.Add("yuv420p");
                startInfo.ArgumentList.Add(videoPath);
                using var process = Process.Start(startInfo) ??
                                    throw new InvalidOperationException("Could not start ffmpeg.");
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "Ata command capture ffmpeg failed: " + error);
                }

                TryDelete(rawVideoPath);
            }

            public void Dispose()
            {
                try
                {
                    rawVideo?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }

                animator.speed = previousAnimatorSpeed;
                RestoreLayers(layerStates);
                model.gameObject.SetActive(modelWasActive);
                slot.gameObject.SetActive(slotWasActive);
                placement.SetActive(placementWasActive);
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                UnityEngine.Object.DestroyImmediate(readback);
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
            }
        }

        private static Bounds CalculateBounds(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Ata command capture has no renderer.");
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static void FrameCamera(Camera camera, Transform model, Bounds bounds)
        {
            var direction = Quaternion.AngleAxis(24f, model.up) * model.forward;
            var target = bounds.center + model.up * bounds.extents.y * 0.05f;
            var distance = bounds.extents.magnitude /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.1f;
            camera.transform.position = target - direction.normalized * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                model.up);
        }

        private static GameObject CreateLight(string name, Quaternion rotation, float intensity)
        {
            var lightObject = new GameObject(name) { hideFlags = HideFlags.DontSave };
            lightObject.transform.rotation = rotation;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.cullingMask = 1 << CaptureLayer;
            return lightObject;
        }

        private static LayerState[] SetLayerRecursively(Transform root, int layer)
        {
            var states = root.GetComponentsInChildren<Transform>(true)
                .Select(item => new LayerState(item.gameObject, item.gameObject.layer))
                .ToArray();
            foreach (var state in states)
            {
                state.GameObject.layer = layer;
            }

            return states;
        }

        private static void RestoreLayers(IEnumerable<LayerState> states)
        {
            foreach (var state in states)
            {
                if (state.GameObject != null)
                {
                    state.GameObject.layer = state.Value;
                }
            }
        }

        private static CaptureState ReadState()
        {
            if (!File.Exists(StatePath))
            {
                return null;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(StatePath))
            {
                var split = line.IndexOf('=');
                if (split >= 0)
                {
                    values[line.Substring(0, split)] = line.Substring(split + 1);
                }
            }

            return new CaptureState
            {
                Phase = Get(values, "phase"),
                VideoPath = Get(values, "videoPath"),
                MetricsPath = Get(values, "metricsPath"),
                Summary = Get(values, "summary"),
                Error = Get(values, "error")
            };
        }

        private static void WriteState(CaptureState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
            File.WriteAllLines(StatePath, new[]
            {
                "phase=" + state.Phase,
                "videoPath=" + state.VideoPath,
                "metricsPath=" + state.MetricsPath,
                "summary=" + state.Summary,
                "error=" + state.Error
            });
        }

        private static string Get(IDictionary<string, string> values, string key) =>
            values.TryGetValue(key, out var value) ? value : string.Empty;

        private static void CleanupCallbacks()
        {
            complete = null;
            fail = null;
        }

        private static void DeleteStateFiles()
        {
            TryDelete(StatePath);
            TryDelete(ResultPath);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string StatePath =>
            Path.Combine(ProjectRoot, "Logs", StateFileName);

        private static string ResultPath =>
            Path.Combine(ProjectRoot, "Logs", ResultFileName);

        private sealed class CaptureState
        {
            public string Phase;
            public string VideoPath;
            public string MetricsPath;
            public string Summary;
            public string Error;
        }

        private readonly struct LayerState
        {
            public LayerState(GameObject gameObject, int value)
            {
                GameObject = gameObject;
                Value = value;
            }

            public GameObject GameObject { get; }
            public int Value { get; }
        }
    }
}
