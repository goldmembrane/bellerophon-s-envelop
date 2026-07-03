using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class Phase8TransportRunPlayModeSmoke
    {
        private const string RequestFileName = "Phase8TransportRunSmoke.request";
        private const string ActiveFileName = "Phase8TransportRunSmoke.active";
        private const string ErrorsFileName = "Phase8TransportRunSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase8TransportRunPlayModeSmoke()
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
                throw new TimeoutException($"Phase 8 transport run smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 8 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 8 smoke must start from Edit mode.");
            }

            Phase8TransportRunBootstrap.EnsurePhase8Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase8TransportRunBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase8TransportRunEditorValidation.Run();

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

            request.Details = ValidateRuntime();
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

            if (SceneManager.GetActiveScene().path != Phase8TransportRunBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase8TransportRunBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 8 smoke captured Unity errors."));
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
                throw new InvalidOperationException($"Expected active scene {CargoRunSceneName}, got {SceneManager.GetActiveScene().name}.");
            }

            var controller = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            var playerMotor = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            var manualView = UnityEngine.Object.FindFirstObjectByType<ManualFlightView>();
            if (controller == null ||
                playerMotor == null ||
                playerInput == null ||
                deviceState == null ||
                deviceHud == null ||
                manualView == null ||
                deviceHud.TransportStatusText == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 8 controller, player, device state, HUD, and manual flight view.");
            }

            controller.FastForwardAssociationContractForValidation();
            ClickButtonThroughUi(controller.YesButton);
            ClickButtonThroughUi(controller.TutorialContractButton);

            if (!deviceState.HasActiveTransportRun ||
                deviceState.CurrentFlightMode != ShipFlightMode.AutoPilot ||
                !deviceState.IsAutoPilotAvailable)
            {
                throw new InvalidOperationException("Tutorial transport must start in available auto pilot mode.");
            }

            deviceState.TickTransportRun(15f);
            deviceHud.RefreshTransportStatus();
            if (!deviceHud.TransportStatusText.enabled ||
                !deviceHud.TransportStatusText.text.Contains("Progress: 25%") ||
                !deviceHud.TransportStatusText.text.Contains("Remaining: 45s"))
            {
                throw new InvalidOperationException("Transport HUD must display tutorial progress and remaining time.");
            }

            var context = new PlayerInteractionContext(playerMotor.gameObject, Camera.main?.transform, new RaycastHit());
            InteractDevice(ShipDeviceType.CockpitHelm, context);
            manualView.RefreshView();
            if (deviceState.CurrentFlightMode != ShipFlightMode.ManualFlight ||
                !manualView.IsViewActive ||
                !playerInput.GameplayInputSuppressed)
            {
                throw new InvalidOperationException("Cockpit helm must enter manual flight mode and show the manual flight view.");
            }

            Canvas.ForceUpdateCanvases();
            var manualRootTransform = manualView.ViewRoot.GetComponent<RectTransform>();
            if (manualRootTransform == null ||
                manualRootTransform.rect.width < 1000f ||
                manualRootTransform.rect.height < 600f)
            {
                throw new InvalidOperationException("Manual flight view must cover the full screen instead of opening as a modal panel.");
            }

            var manualBackground = manualView.ViewRoot.GetComponent<Image>();
            if (manualBackground == null || manualBackground.color.a < 1f)
            {
                throw new InvalidOperationException("Manual flight view background must be fully opaque.");
            }

            PressManualFlightKey(Key.D, manualView, 0.5f);
            PressManualFlightKey(Key.W, manualView, 0.5f);
            if (deviceState.ManualFlightOffsetX <= 0f ||
                deviceState.ManualFlightOffsetY <= 0f ||
                manualView.PlayerMarker.anchoredPosition.x <= 0f ||
                manualView.PlayerMarker.anchoredPosition.y <= 0f)
            {
                throw new InvalidOperationException("Manual flight WASD input must move the avoidance marker.");
            }

            PressManualFlightKey(Key.Escape, manualView, 0.1f);
            manualView.RefreshView();
            if (deviceState.CurrentFlightMode != ShipFlightMode.AutoPilot ||
                deviceState.ManualFlightModeActive ||
                manualView.IsViewActive ||
                playerInput.GameplayInputSuppressed)
            {
                throw new InvalidOperationException("Escape must leave manual flight and restore auto pilot when auto pilot is available.");
            }

            deviceState.SetShipState(ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(50, 100)));
            deviceHud.RefreshTransportStatus();
            if (deviceState.IsAutoPilotAvailable ||
                deviceState.CurrentFlightMode != ShipFlightMode.ManualFlight ||
                !deviceHud.TransportStatusText.text.Contains("Auto Pilot: Unavailable"))
            {
                throw new InvalidOperationException("Cockpit durability at 50% must disable auto pilot and force manual flight.");
            }

            if (deviceState.ExitManualFlightToAutoPilot())
            {
                throw new InvalidOperationException("Manual flight must not exit to auto pilot while cockpit durability is at or below 50%.");
            }

            return $"Mode={deviceState.CurrentFlightMode}; Progress={Mathf.RoundToInt(deviceState.TransportProgressPercent * 100f)}%; Remaining={Mathf.CeilToInt(deviceState.TransportRemainingSeconds)}; AutoPilot={deviceState.IsAutoPilotAvailable}; Offset={deviceState.ManualFlightOffsetX:0.00},{deviceState.ManualFlightOffsetY:0.00}";
        }

        private static ShipDeviceInteractable InteractDevice(ShipDeviceType deviceType, PlayerInteractionContext context)
        {
            var devices = UnityEngine.Object.FindObjectsByType<ShipDeviceInteractable>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (var i = 0; i < devices.Length; i++)
            {
                var device = devices[i];
                if (device.DeviceType != deviceType)
                {
                    continue;
                }

                if (!device.CanInteract(context, out var failureReason))
                {
                    throw new InvalidOperationException(deviceType + " refused interaction: " + failureReason);
                }

                device.Interact(context);
                return device;
            }

            throw new InvalidOperationException("Missing runtime phase 8 device: " + deviceType);
        }

        private static void PressManualFlightKey(Key key, ManualFlightView manualView, float deltaSeconds)
        {
            if (Keyboard.current == null)
            {
                throw new InvalidOperationException("Keyboard input is required for Phase 8 manual flight smoke.");
            }

            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
            InputSystem.Update();
            manualView.ProcessManualFlightInput(deltaSeconds);
            manualView.RefreshView();
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            InputSystem.Update();
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 8 button.");
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 8 UI click requires an active EventSystem.");
            }

            Canvas.ForceUpdateCanvases();
            var rectTransform = button.GetComponent<RectTransform>();
            var position = RectTransformUtility.WorldToScreenPoint(null, rectTransform.TransformPoint(rectTransform.rect.center));
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = position,
                button = PointerEventData.InputButton.Left,
                eligibleForClick = true,
                clickCount = 1
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            var hitButton = false;
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject == button.gameObject ||
                    results[i].gameObject.transform.IsChildOf(button.transform))
                {
                    hitButton = true;
                    break;
                }
            }

            if (!hitButton)
            {
                var hitNames = results.Count == 0
                    ? "none"
                    : string.Join(", ", results.Select(result => result.gameObject.name));
                throw new InvalidOperationException(
                    $"Phase 8 button is not reachable by UI raycast: {button.name}; Position={position}; Hits={hitNames}");
            }

            ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
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
            builder.AppendLine($"Phase 8 transport run smoke completed: {request.Id}");
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
                var request = new SmokeRequest();
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

                    var key = line.Substring(0, separatorIndex);
                    var value = line.Substring(separatorIndex + 1);
                    switch (key)
                    {
                        case "id":
                            request.Id = value;
                            break;
                        case "logPath":
                            request.LogPath = value;
                            break;
                        case "phase":
                            if (Enum.TryParse(value, out SmokePhase phase))
                            {
                                request.Phase = phase;
                            }

                            break;
                        case "startUtcTicks":
                            if (long.TryParse(value, out var ticks))
                            {
                                request.StartUtcTicks = ticks;
                            }

                            break;
                        case "playFrameCount":
                            if (int.TryParse(value, out var count))
                            {
                                request.PlayFrameCount = count;
                            }

                            break;
                        case "details":
                            request.Details = value;
                            break;
                    }
                }

                return request;
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
                        $"playFrameCount={PlayFrameCount}",
                        $"details={Details ?? string.Empty}"
                    });
            }
        }
    }
}
