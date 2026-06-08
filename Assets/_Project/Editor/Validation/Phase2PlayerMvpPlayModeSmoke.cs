using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class Phase2PlayerMvpPlayModeSmoke
    {
        private const string RequestFileName = "Phase2PlayerMvpPlayModeSmoke.request";
        private const string ActiveFileName = "Phase2PlayerMvpPlayModeSmoke.active";
        private const string ErrorsFileName = "Phase2PlayerMvpPlayModeSmoke.errors";
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase2PlayerMvpPlayModeSmoke()
        {
            EditorApplication.update += Poll;
            Application.logMessageReceived += CaptureLog;
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPollTime)
            {
                return;
            }

            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            try
            {
                if (TryContinueActiveRequest())
                {
                    return;
                }

                TryStartRequest();
            }
            catch (Exception exception)
            {
                FailCurrentRequest(exception);
            }
        }

        private static void TryStartRequest()
        {
            if (!File.Exists(RequestPath) || File.Exists(ActivePath))
            {
                return;
            }

            var request = SmokeRequest.Read(RequestPath);
            TryDelete(RequestPath);
            if (!request.IsValid)
            {
                return;
            }

            request.Phase = SmokePhase.Prepare;
            request.StartUtcTicks = DateTime.UtcNow.Ticks;
            request.PlayFrameCount = 0;
            TryDelete(ErrorsPath);
            request.Write(ActivePath);
        }

        private static bool TryContinueActiveRequest()
        {
            if (!File.Exists(ActivePath))
            {
                return false;
            }

            var request = SmokeRequest.Read(ActivePath);
            if (!request.IsValid)
            {
                TryDelete(ActivePath);
                return false;
            }

            if (IsExpired(request))
            {
                throw new TimeoutException($"Phase 2 quick PlayMode smoke exceeded {MaxRunSeconds:0} seconds.");
            }

            switch (request.Phase)
            {
                case SmokePhase.Prepare:
                    PrepareAndEnterPlayMode(request);
                    break;
                case SmokePhase.WaitForPlayMode:
                    WaitForPlayMode(request);
                    break;
                case SmokePhase.ValidateRuntime:
                    ValidateRuntimeWhenReady(request);
                    break;
                case SmokePhase.ExitPlayMode:
                    FinishAfterPlayModeExit(request);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown quick PlayMode smoke phase: {request.Phase}");
            }

            return true;
        }

        private static void PrepareAndEnterPlayMode(SmokeRequest request)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Quick PlayMode smoke must start from Edit mode.");
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase2PlayerMvpEditorValidation.Run();

            request.Phase = SmokePhase.WaitForPlayMode;
            request.Write(ActivePath);
            EditorApplication.EnterPlaymode();
        }

        private static void WaitForPlayMode(SmokeRequest request)
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            request.Phase = SmokePhase.ValidateRuntime;
            request.PlayFrameCount = 0;
            request.Write(ActivePath);
        }

        private static void ValidateRuntimeWhenReady(SmokeRequest request)
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            request.PlayFrameCount++;
            if (request.PlayFrameCount < RequiredPlayFrames)
            {
                request.Write(ActivePath);
                return;
            }

            var result = ValidateRuntime();
            request.Details = result;
            request.Phase = SmokePhase.ExitPlayMode;
            request.Write(ActivePath);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishAfterPlayModeExit(SmokeRequest request)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (SceneManager.GetActiveScene().path != CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Quick PlayMode smoke captured Unity errors."));
                TryDelete(ActivePath);
                TryDelete(ErrorsPath);
                return;
            }

            WriteLog(request, false, null);
            TryDelete(ActivePath);
            TryDelete(ErrorsPath);
        }

        private static string ValidateRuntime()
        {
            if (SceneManager.GetActiveScene().name != CargoRunSceneName)
            {
                throw new InvalidOperationException(
                    $"Expected active scene {CargoRunSceneName}, got {SceneManager.GetActiveScene().name}.");
            }

            var playerMotor = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var interaction = UnityEngine.Object.FindFirstObjectByType<FirstPersonInteractionController>();
            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();

            if (playerMotor == null || playerInput == null || interaction == null || hud == null)
            {
                throw new InvalidOperationException("Runtime scene must contain player motor/input/interaction and HUD.");
            }

            CompleteBlockingStartFlowIfPresent();

            if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            {
                throw new InvalidOperationException(
                    $"Runtime cursor must be locked and hidden. LockState={Cursor.lockState}, Visible={Cursor.visible}");
            }

            var camera = Camera.main;
            if (camera == null || !camera.isActiveAndEnabled)
            {
                throw new InvalidOperationException("Runtime scene must have an active MainCamera.");
            }

            var renderedPixels = CountRenderedScenePixels(camera);
            if (renderedPixels < 200)
            {
                throw new InvalidOperationException($"Runtime camera rendered too few visible pixels: {renderedPixels}.");
            }

            var visibleRenderers = CountVisibleRenderers(camera);
            if (visibleRenderers < 2)
            {
                throw new InvalidOperationException($"Runtime camera frustum has too few visible renderers: {visibleRenderers}.");
            }

            var canvas = hud.GetComponent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                throw new InvalidOperationException("Runtime HUD must have an active ScreenSpaceOverlay Canvas.");
            }

            if (hud.GetComponentsInChildren<Text>(true).Length < 4)
            {
                throw new InvalidOperationException("Runtime HUD must include health, shield, crosshair, and interaction prompt labels.");
            }

            playerMotor.transform.rotation = Quaternion.identity;
            camera.transform.localRotation = Quaternion.identity;
            Physics.SyncTransforms();

            if (!interaction.TryInteract())
            {
                throw new InvalidOperationException($"Runtime interaction failed: {interaction.LastFailureReason}");
            }

            var target = interaction.LastInteractable;
            if (target == null)
            {
                throw new InvalidOperationException("Runtime interaction must record the last interactable.");
            }

            var interactionCount = GetInteractionCount(target);
            if (interactionCount < 1)
            {
                throw new InvalidOperationException("Runtime interaction target did not record the interaction.");
            }

            var promptText = FindHudText(hud, "Interaction Prompt Text");
            if (promptText == null)
            {
                throw new InvalidOperationException("Runtime HUD must include an interaction prompt label.");
            }

            return $"Scene={CargoRunSceneName}; RenderedPixels={renderedPixels}; VisibleRenderers={visibleRenderers}; InteractionCount={interactionCount}; Target={target.DisplayName}; Cursor={Cursor.lockState}";
        }

        private static void CompleteBlockingStartFlowIfPresent()
        {
            var startFlow = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            if (startFlow == null || !startFlow.gameObject.activeInHierarchy)
            {
                return;
            }

            if (startFlow.FlowState.Phase == NewGameStartFlowPhase.ContractPrompt)
            {
                startFlow.FastForwardAssociationContractForValidation();
                startFlow.AcceptAssociationContract();
            }

            if (startFlow.FlowState.Phase == NewGameStartFlowPhase.AssociationPlanet)
            {
                startFlow.AcceptTutorialContract();
            }
        }

        private static int GetInteractionCount(IPlayerInteractable target)
        {
            if (target is DebugInteractable debugInteractable)
            {
                return debugInteractable.InteractionCount;
            }

            if (target is ShipDeviceInteractable shipDeviceInteractable)
            {
                return shipDeviceInteractable.InteractionCount;
            }

            return 1;
        }

        private static Text FindHudText(FirstPersonHud hud, string name)
        {
            var labels = hud.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].name == name)
                {
                    return labels[i];
                }
            }

            return null;
        }

        private static void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            if (!File.Exists(ActivePath))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ErrorsPath));
                File.AppendAllText(ErrorsPath, $"{type}: {condition}{Environment.NewLine}{stackTrace}{Environment.NewLine}");
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static int CountRenderedScenePixels(Camera camera)
        {
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var renderTexture = new RenderTexture(160, 90, 24, RenderTextureFormat.ARGB32);
            var readableTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                readableTexture.Apply();

                var background = camera.backgroundColor;
                var pixels = readableTexture.GetPixels();
                var visiblePixelCount = 0;
                for (var i = 0; i < pixels.Length; i++)
                {
                    if (ColorDistance(pixels[i], background) > 0.08f)
                    {
                        visiblePixelCount++;
                    }
                }

                return visiblePixelCount;
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                DestroyTexture(renderTexture);
                DestroyTexture(readableTexture);
            }
        }

        private static void DestroyTexture(UnityEngine.Object texture)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
                return;
            }

            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static int CountVisibleRenderers(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            var visibleRendererCount = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                {
                    visibleRendererCount++;
                }
            }

            return visibleRendererCount;
        }

        private static float ColorDistance(Color left, Color right)
        {
            var red = left.r - right.r;
            var green = left.g - right.g;
            var blue = left.b - right.b;
            return Mathf.Sqrt((red * red) + (green * green) + (blue * blue));
        }

        private static bool IsExpired(SmokeRequest request)
        {
            if (request.StartUtcTicks <= 0)
            {
                return false;
            }

            var elapsed = DateTime.UtcNow - new DateTime(request.StartUtcTicks, DateTimeKind.Utc);
            return elapsed.TotalSeconds > MaxRunSeconds;
        }

        private static void FailCurrentRequest(Exception exception)
        {
            if (!File.Exists(ActivePath))
            {
                return;
            }

            var request = SmokeRequest.Read(ActivePath);
            if (request.IsValid)
            {
                WriteLog(request, true, exception);
            }

            TryDelete(ActivePath);
            TryDelete(ErrorsPath);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static void WriteLog(SmokeRequest request, bool failed, Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Phase 2 quick PlayMode smoke completed: {request.Id}");
            builder.AppendLine("Unity editor smoke mode: open editor quick playmode");
            builder.AppendLine($"Result: {(failed ? "Failed" : "Passed")}");

            if (!string.IsNullOrWhiteSpace(request.Details))
            {
                builder.AppendLine(request.Details);
            }

            if (failed && exception != null)
            {
                builder.AppendLine(exception.ToString());
            }

            if (File.Exists(ErrorsPath))
            {
                builder.AppendLine();
                builder.AppendLine("Captured Unity errors:");
                builder.Append(File.ReadAllText(ErrorsPath));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(request.LogPath));
            File.WriteAllText(request.LogPath, builder.ToString());
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string RequestPath =>
            Path.Combine(ProjectRoot, "Logs", RequestFileName);

        private static string ActivePath =>
            Path.Combine(ProjectRoot, "Logs", ActiveFileName);

        private static string ErrorsPath =>
            Path.Combine(ProjectRoot, "Logs", ErrorsFileName);

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private enum SmokePhase
        {
            Prepare,
            WaitForPlayMode,
            ValidateRuntime,
            ExitPlayMode
        }

        private sealed class SmokeRequest
        {
            public string Id { get; private set; }
            public string LogPath { get; private set; }
            public SmokePhase Phase { get; set; }
            public long StartUtcTicks { get; set; }
            public int PlayFrameCount { get; set; }
            public string Details { get; set; }

            public bool IsValid =>
                !string.IsNullOrWhiteSpace(Id) &&
                !string.IsNullOrWhiteSpace(LogPath);

            public static SmokeRequest Read(string path)
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = rawLine.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex < 0)
                    {
                        continue;
                    }

                    values[line.Substring(0, separatorIndex)] = line.Substring(separatorIndex + 1);
                }

                return new SmokeRequest
                {
                    Id = Get(values, "id"),
                    LogPath = Get(values, "logPath"),
                    Phase = Enum.TryParse<SmokePhase>(Get(values, "phase"), out var phase) ? phase : SmokePhase.Prepare,
                    StartUtcTicks = long.TryParse(Get(values, "startUtcTicks"), out var startUtcTicks) ? startUtcTicks : 0L,
                    PlayFrameCount = int.TryParse(Get(values, "playFrameCount"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var playFrameCount)
                        ? playFrameCount
                        : 0,
                    Details = Get(values, "details")
                };
            }

            public void Write(string path)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllLines(
                    path,
                    new[]
                    {
                        $"id={Id}",
                        $"logPath={LogPath}",
                        $"phase={Phase}",
                        $"startUtcTicks={StartUtcTicks}",
                        $"playFrameCount={PlayFrameCount.ToString(CultureInfo.InvariantCulture)}",
                        $"details={Details}"
                    });
            }

            private static string Get(IDictionary<string, string> values, string key)
            {
                return values.TryGetValue(key, out var value) ? value : string.Empty;
            }
        }
    }
}
