using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    [InitializeOnLoad]
    internal static class Ispant08ContinuousMotionPlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string SlotName = "Ispant_08_StowMusketDrawSword";
        private const string ModelName = "Ispant_ChangingToSword_Model";
        private const string SequenceClipName = "Ispant_08_StowMusketDrawSword_ContinuousSequence";
        private const string StateFileName = "Ispant08ContinuousMotionCapture.state";
        private const string ResultFileName = "Ispant08ContinuousMotionCapture.result";
        private const string StateEnteringPlayMode = "EnteringPlayMode";
        private const string StateExitingPlayMode = "ExitingPlayMode";
        private const string StateFailedExitingPlayMode = "FailedExitingPlayMode";
        private const int CaptureWidth = 480;
        private const int CaptureHeight = 360;
        private const int CaptureFrameRate = 30;
        private const int CaptureLayer = 31;
        private const string UnityWindowTitle =
            "Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) <DX12>";

        private static Action<string> complete;
        private static Action<Exception> fail;
        private static CaptureSession session;

        static Ispant08ContinuousMotionPlayModeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Start(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Cannot start the Ispant slot-8 motion capture while Unity is entering or running Play Mode.");
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("Current active scene must be CargoRunMvp. ActiveScene=" + scene.path);
            if (scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp must be clean before the actual slot-8 capture.");

            DeleteStateFiles();
            var outputDirectory = Path.Combine(
                ProjectRoot,
                "docs",
                "validation",
                "ispant_changing_to_sword_2026-08-11",
                "actual_playmode_motion");
            Directory.CreateDirectory(outputDirectory);
            var videoPath = Path.Combine(outputDirectory, "Ispant_08_ContinuousMotion_TwoLoops.mp4");
            var metricsPath = Path.Combine(outputDirectory, "Ispant_08_ContinuousMotion_Frames.csv");
            TryDelete(videoPath);
            TryDelete(metricsPath);
            WriteState(new CaptureState
            {
                Phase = StateEnteringPlayMode,
                VideoPath = videoPath,
                MetricsPath = metricsPath,
                StartedUtcTicks = DateTime.UtcNow.Ticks
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
            if (complete == null && fail == null) return;
            var state = ReadState();
            if (state == null) return;
            try
            {
                if (state.Phase == StateEnteringPlayMode)
                {
                    if (!EditorApplication.isPlaying) return;
                    session ??= new CaptureSession(state.VideoPath, state.MetricsPath);
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (!session.Tick()) return;
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
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CompleteFromState(state);
                    return;
                }

                if (state.Phase == StateFailedExitingPlayMode &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                    FailFromState(state);
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
                    if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
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
            private readonly Transform slot;
            private readonly Transform model;
            private readonly GameObject placementObject;
            private readonly Animator animator;
            private readonly Transform rightHand;
            private readonly ActiveState[] siblingStates;
            private readonly LayerState[] layerStates;
            private readonly bool slotWasActive;
            private readonly bool placementWasActive;
            private readonly bool modelWasActive;
            private readonly AnimatorCullingMode previousCullingMode;
            private readonly bool previousApplyRootMotion;
            private readonly float previousAnimatorSpeed;
            private readonly GameObject cameraObject;
            private readonly GameObject keyLightObject;
            private readonly GameObject fillLightObject;
            private readonly Camera camera;
            private readonly Process screenEncoder;
            private readonly string screenTempPath;
            private readonly EditorWindow gameView;
            private readonly bool gameViewWasMaximized;
            private readonly RenderTexture renderTexture;
            private readonly string rawVideoPath;
            private readonly SortedDictionary<int, byte[]> videoFrames =
                new SortedDictionary<int, byte[]>();
            private readonly string videoPath;
            private readonly string metricsPath;
            private readonly StringBuilder metrics = new StringBuilder();
            private double startedAt;
            private readonly double encoderStartedAt;
            private readonly float clipLength;
            private double nextCaptureAt;
            private int capturedFrames;
            private double lastElapsed;
            private float lastNormalizedTime;
            private bool captureStarted;
            private bool screenEncoderCompleted;
            private bool captureEndReached;
            private int pendingReadbacks;
            private string readbackError;

            public CaptureSession(string videoPath, string metricsPath)
            {
                this.videoPath = videoPath;
                this.metricsPath = metricsPath;
                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid() || scene.path != ScenePath)
                    throw new InvalidOperationException(
                        "Play Mode active scene must stay CargoRunMvp. ActiveScene=" + scene.path);
                var placement = scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                                throw new InvalidOperationException("Missing placement root: " + PlacementRootName);
                placementObject = placement;
                placementWasActive = placement.activeSelf;
                placement.SetActive(true);
                slot = placement.transform.Find(SlotName) ??
                       throw new InvalidOperationException("Missing Ispant slot 8: " + SlotName);
                model = slot.Find(ModelName) ??
                        throw new InvalidOperationException("Missing slot-8 model: " + ModelName);
                modelWasActive = model.gameObject.activeSelf;
                model.gameObject.SetActive(true);
                animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException("The actual slot-8 Animator is missing.");
                if (animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException("The actual slot-8 AnimatorController is missing.");
                var sequenceClips = animator.runtimeAnimatorController.animationClips
                    .Where(item => item.name == SequenceClipName).Distinct().ToArray();
                if (sequenceClips.Length != 1)
                    throw new InvalidOperationException(
                        "The actual slot-8 controller does not expose one continuous sequence clip.");
                clipLength = sequenceClips[0].length;
                rightHand = model.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == "mixamorig:RightHand" || item.name == "RightHand") ??
                            throw new InvalidOperationException("The slot-8 right hand is missing.");

                // Isolation is performed by the dedicated capture layer and camera mask.
                // Keeping sibling objects active avoids making their runtime Animators inactive
                // while unrelated scene scripts are still completing their Play Mode startup.
                siblingStates = Array.Empty<ActiveState>();
                layerStates = SetLayerRecursively(slot, CaptureLayer);
                slotWasActive = slot.gameObject.activeSelf;
                slot.gameObject.SetActive(true);
                previousCullingMode = animator.cullingMode;
                previousApplyRootMotion = animator.applyRootMotion;
                previousAnimatorSpeed = animator.speed;
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                animator.speed = 1f;
                animator.Play(0, 0, 0f);
                animator.Update(0f);
                foreach (var renderer in slot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                    renderer.forceMatrixRecalculationPerRender = true;
                }

                cameraObject = new GameObject("Ispant Slot 8 Actual Motion Camera")
                    { hideFlags = HideFlags.DontSave };
                camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.04f, 0.05f, 1f);
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                var bodyRenderer = model.GetComponentsInChildren<Renderer>(true)
                    .SingleOrDefault(item => item.name == "Ispant_Armed_Body") ??
                                   throw new InvalidOperationException(
                                       "The slot-8 synchronized body renderer is missing.");
                var bounds = bodyRenderer.bounds;
                var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.5f);
                var center = bounds.center + Vector3.up * size * 0.04f;
                camera.transform.position = center + Vector3.forward * size * 2.05f;
                camera.transform.LookAt(center);

                keyLightObject = CreateLight(
                    "Ispant Slot 8 Actual Motion Key Light",
                    Quaternion.Euler(34f, 150f, 0f),
                    2.6f,
                    new Color(1f, 0.94f, 0.86f, 1f));
                fillLightObject = CreateLight(
                    "Ispant Slot 8 Actual Motion Fill Light",
                    Quaternion.Euler(18f, -35f, 0f),
                    1.25f,
                    new Color(0.62f, 0.76f, 1f, 1f));
                renderTexture = new RenderTexture(
                    CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                rawVideoPath = Path.ChangeExtension(videoPath, ".rgb24");
                TryDelete(rawVideoPath);
                screenEncoder = null;
                screenTempPath = null;
                gameView = null;
                gameViewWasMaximized = false;
                encoderStartedAt = Time.realtimeSinceStartupAsDouble;
                animator.speed = 0f;
                metrics.AppendLine(
                    "frame,elapsedSeconds,normalizedTime,rightHandX,rightHandY,rightHandZ");
                startedAt = 0d;
                nextCaptureAt = double.MaxValue;
            }

            public bool Tick()
            {
                var now = Time.realtimeSinceStartupAsDouble;
                if (!captureStarted)
                {
                    if (now - encoderStartedAt < 0.5d) return false;
                    animator.Play(0, 0, 0f);
                    animator.Update(0f);
                    animator.speed = 1f;
                    captureStarted = true;
                    startedAt = now;
                    nextCaptureAt = now;
                }
                if (captureEndReached)
                {
                    if (pendingReadbacks > 0) return false;
                    if (!string.IsNullOrWhiteSpace(readbackError))
                        throw new InvalidOperationException(readbackError);
                    return true;
                }
                var normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                if (capturedFrames > 0 && normalizedTime >= 2.05f)
                {
                    captureEndReached = true;
                    return pendingReadbacks == 0;
                }
                if (now - startedAt > 30d)
                    throw new TimeoutException(
                        "The actual slot-8 Animator did not complete two continuous loops within 30 seconds.");
                if (now + 0.0001d < nextCaptureAt) return false;
                CaptureFrame(now - startedAt);
                capturedFrames++;
                nextCaptureAt += 1d / CaptureFrameRate;
                if (nextCaptureAt < now - 0.1d)
                    nextCaptureAt = now + 1d / CaptureFrameRate;
                return false;
            }

            public string Complete()
            {
                var recordedAnimatorSeconds = lastNormalizedTime * clipLength;
                if (videoFrames.Count != capturedFrames)
                    throw new InvalidOperationException(
                        "The asynchronous slot-8 capture did not return every requested frame. Requested=" +
                        capturedFrames + ", Returned=" + videoFrames.Count + ".");
                using (var raw = new FileStream(
                           rawVideoPath, FileMode.Create, FileAccess.Write, FileShare.Read,
                           1024 * 1024, FileOptions.SequentialScan))
                    foreach (var frame in videoFrames.OrderBy(item => item.Key))
                        raw.Write(frame.Value, 0, frame.Value.Length);
                var actualFrameRate = capturedFrames > 1 && recordedAnimatorSeconds > 0.0001d
                    ? (capturedFrames - 1d) / recordedAnimatorSeconds
                    : CaptureFrameRate;
                EncodeRawVideo(actualFrameRate);
                File.WriteAllText(metricsPath, metrics.ToString(), new UTF8Encoding(false));
                if (!File.Exists(videoPath) || new FileInfo(videoPath).Length < 1024L)
                    throw new InvalidOperationException("The actual slot-8 motion video was not encoded.");
                return "ActualPlayMode=True, IsolatedSlot8=True, ContinuousState=True" +
                       ", ClipSeconds=" + Num(clipLength) +
                       ", CapturedFrames=" + capturedFrames +
                       ", RecordedSeconds=" + lastElapsed.ToString("0.######", CultureInfo.InvariantCulture) +
                       ", RecordedAnimatorSeconds=" +
                       recordedAnimatorSeconds.ToString("0.######", CultureInfo.InvariantCulture) +
                       ", EncodedFrameRate=" +
                       actualFrameRate.ToString("0.######", CultureInfo.InvariantCulture) +
                       ", Loops=" + Num(lastNormalizedTime) +
                       ", Video=" + videoPath +
                       ", Metrics=" + metricsPath;
            }

            private void CaptureFrame(double elapsed)
            {
                camera.Render();
                var frameIndex = capturedFrames;
                pendingReadbacks++;
                AsyncGPUReadback.Request(
                    renderTexture,
                    0,
                    TextureFormat.RGB24,
                    request =>
                    {
                        try
                        {
                            if (request.hasError)
                                readbackError =
                                    "Unity reported an asynchronous GPU readback error for slot-8 frame " +
                                    frameIndex + ".";
                            else
                                videoFrames[frameIndex] = request.GetData<byte>().ToArray();
                        }
                        catch (Exception exception)
                        {
                            readbackError = exception.ToString();
                        }
                        finally
                        {
                            pendingReadbacks--;
                        }
                    });
                var state = animator.GetCurrentAnimatorStateInfo(0);
                lastElapsed = elapsed;
                lastNormalizedTime = state.normalizedTime;
                var hand = model.InverseTransformPoint(rightHand.position);
                metrics.Append(capturedFrames.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(elapsed.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(state.normalizedTime.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hand.x.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hand.y.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hand.z.ToString("0.000000", CultureInfo.InvariantCulture)).AppendLine();
            }

            private static Process StartScreenEncoder(string outputPath)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = "-y -f gdigrab -draw_mouse 0 -framerate 30 -i title=\"" + UnityWindowTitle +
                                "\" -an -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" " +
                                "-c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p \"" +
                                outputPath + "\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                };
                return Process.Start(startInfo) ??
                       throw new InvalidOperationException(
                           "ffmpeg could not start the Unity Game View motion capture.");
            }

            private void EncodeRawVideo(double actualFrameRate)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = "-y -f rawvideo -pixel_format rgb24 -video_size " +
                                CaptureWidth + "x" + CaptureHeight + " -framerate " +
                                actualFrameRate.ToString("0.######", CultureInfo.InvariantCulture) +
                                " -i \"" + rawVideoPath + "\" -an -vf vflip -c:v libx264 " +
                                "-preset fast -crf 18 -pix_fmt yuv420p \"" + videoPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo) ??
                                    throw new InvalidOperationException(
                                        "ffmpeg could not encode the asynchronous slot-8 motion frames.");
                if (!process.WaitForExit(30000))
                {
                    process.Kill();
                    throw new TimeoutException(
                        "ffmpeg did not finish the asynchronous slot-8 motion video.");
                }
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(
                        "ffmpeg failed for the asynchronous slot-8 motion video. ExitCode=" +
                        process.ExitCode);
                TryDelete(rawVideoPath);
            }

            private void FinishScreenEncoder()
            {
                if (screenEncoderCompleted) return;
                screenEncoder.StandardInput.WriteLine("q");
                screenEncoder.StandardInput.Flush();
                if (!screenEncoder.WaitForExit(15000))
                {
                    screenEncoder.Kill();
                    throw new TimeoutException("ffmpeg did not finish the Unity Game View motion video.");
                }
                if (screenEncoder.ExitCode != 0)
                    throw new InvalidOperationException(
                        "ffmpeg failed for the Unity Game View motion video. ExitCode=" +
                        screenEncoder.ExitCode);
                screenEncoderCompleted = true;
            }

            private void TrimScreenVideo(double offsetSeconds, double durationSeconds)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = "-y -ss " + offsetSeconds.ToString("0.######", CultureInfo.InvariantCulture) +
                                " -i \"" + screenTempPath + "\" -t " +
                                durationSeconds.ToString("0.######", CultureInfo.InvariantCulture) +
                                " -an -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p \"" +
                                videoPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo) ??
                                    throw new InvalidOperationException(
                                        "ffmpeg could not trim the Unity Game View motion video.");
                if (!process.WaitForExit(30000))
                {
                    process.Kill();
                    throw new TimeoutException("ffmpeg did not trim the Unity Game View motion video.");
                }
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(
                        "ffmpeg failed to trim the Unity Game View motion video. ExitCode=" +
                        process.ExitCode);
                TryDelete(screenTempPath);
            }

            public void Dispose()
            {
                try
                {
                    if (!screenEncoderCompleted && screenEncoder != null && !screenEncoder.HasExited)
                    {
                        try { screenEncoder.StandardInput.WriteLine("q"); } catch (IOException) { }
                        if (!screenEncoder.WaitForExit(2000)) screenEncoder.Kill();
                    }
                }
                finally
                {
                    screenEncoder?.Dispose();
                    if (gameView != null) gameView.maximized = gameViewWasMaximized;
                    if (camera != null) camera.targetTexture = null;
                    if (renderTexture != null)
                    {
                        renderTexture.Release();
                        UnityEngine.Object.Destroy(renderTexture);
                    }
                    if (fillLightObject != null) UnityEngine.Object.Destroy(fillLightObject);
                    if (keyLightObject != null) UnityEngine.Object.Destroy(keyLightObject);
                    if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
                    if (animator != null)
                    {
                        animator.cullingMode = previousCullingMode;
                        animator.applyRootMotion = previousApplyRootMotion;
                        animator.speed = previousAnimatorSpeed;
                    }
                    if (slot != null) slot.gameObject.SetActive(slotWasActive);
                    if (model != null) model.gameObject.SetActive(modelWasActive);
                    RestoreLayers(layerStates);
                    RestoreOtherSlots(siblingStates);
                    if (placementObject != null) placementObject.SetActive(placementWasActive);
                }
            }
        }

        private static GameObject CreateLight(
            string name, Quaternion rotation, float intensity, Color color)
        {
            var lightObject = new GameObject(name) { hideFlags = HideFlags.DontSave };
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.cullingMask = 1 << CaptureLayer;
            lightObject.transform.rotation = rotation;
            return lightObject;
        }

        private static Bounds CalculateBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.one);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static ActiveState[] HideOtherSlots(Transform placement, Transform target)
        {
            var states = new List<ActiveState>();
            for (var index = 0; index < placement.childCount; index++)
            {
                var child = placement.GetChild(index);
                if (child == target) continue;
                states.Add(new ActiveState(child.gameObject, child.gameObject.activeSelf));
                child.gameObject.SetActive(false);
            }
            return states.ToArray();
        }

        private static void RestoreOtherSlots(IEnumerable<ActiveState> states)
        {
            foreach (var state in states)
                if (state.GameObject != null) state.GameObject.SetActive(state.Value);
        }

        private static LayerState[] SetLayerRecursively(Transform root, int layer)
        {
            var states = root.GetComponentsInChildren<Transform>(true)
                .Select(item => new LayerState(item.gameObject, item.gameObject.layer)).ToArray();
            foreach (var state in states) state.GameObject.layer = layer;
            return states;
        }

        private static void RestoreLayers(IEnumerable<LayerState> states)
        {
            foreach (var state in states)
                if (state.GameObject != null) state.GameObject.layer = state.Value;
        }

        private static void CompleteFromState(CaptureState state)
        {
            var summary = File.Exists(ResultPath) ? File.ReadAllText(ResultPath) : state.Summary;
            var callback = complete;
            CleanupCallbacks();
            DeleteStateFiles();
            callback?.Invoke("Ispant slot 8 actual Play Mode two-loop motion capture completed. " + summary);
        }

        private static void FailFromState(CaptureState state)
        {
            var callback = fail;
            var error = string.IsNullOrWhiteSpace(state.Error)
                ? "Ispant slot-8 actual motion capture failed."
                : state.Error;
            CleanupCallbacks();
            DeleteStateFiles();
            callback?.Invoke(new InvalidOperationException(error));
        }

        private static void CleanupCallbacks()
        {
            complete = null;
            fail = null;
        }

        private static CaptureState ReadState()
        {
            if (!File.Exists(StatePath)) return null;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(StatePath))
            {
                var split = line.IndexOf('=');
                if (split >= 0) values[line.Substring(0, split)] = line.Substring(split + 1);
            }
            return new CaptureState
            {
                Phase = Get(values, "phase"),
                VideoPath = Get(values, "videoPath"),
                MetricsPath = Get(values, "metricsPath"),
                Summary = Get(values, "summary"),
                Error = Get(values, "error"),
                StartedUtcTicks = long.TryParse(Get(values, "startedUtcTicks"), out var ticks) ? ticks : 0L
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
                "error=" + state.Error,
                "startedUtcTicks=" + state.StartedUtcTicks.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static string Get(IDictionary<string, string> values, string key) =>
            values.TryGetValue(key, out var value) ? value : string.Empty;

        private static void DeleteStateFiles()
        {
            TryDelete(StatePath);
            TryDelete(ResultPath);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string Num(float value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

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
            public long StartedUtcTicks;
        }

        private readonly struct ActiveState
        {
            public ActiveState(GameObject gameObject, bool value)
            {
                GameObject = gameObject;
                Value = value;
            }

            public GameObject GameObject { get; }
            public bool Value { get; }
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
