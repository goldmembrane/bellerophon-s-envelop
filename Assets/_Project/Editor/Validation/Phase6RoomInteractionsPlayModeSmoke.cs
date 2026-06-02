using System;
using System.IO;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class Phase6RoomInteractionsPlayModeSmoke
    {
        private const string RequestFileName = "Phase6RoomInteractionsSmoke.request";
        private const string ActiveFileName = "Phase6RoomInteractionsSmoke.active";
        private const string ErrorsFileName = "Phase6RoomInteractionsSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase6RoomInteractionsPlayModeSmoke()
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
                throw new TimeoutException($"Phase 6 room interactions smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 6 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 6 smoke must start from Edit mode.");
            }

            Phase6RoomInteractionsBootstrap.EnsurePhase6Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase6RoomInteractionsBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase6RoomInteractionsEditorValidation.Run();

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

            if (SceneManager.GetActiveScene().path != Phase6RoomInteractionsBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase6RoomInteractionsBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 6 smoke captured Unity errors."));
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

            var playerMotor = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var state = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            if (playerMotor == null || state == null || deviceHud == null || deviceHud.PanelText == null)
            {
                throw new InvalidOperationException("Runtime scene must contain player, phase 6 state, and phase 6 device HUD.");
            }

            var context = new PlayerInteractionContext(playerMotor.gameObject, Camera.main?.transform, new RaycastHit());

            InteractDevice(ShipDeviceType.CockpitHelm, context);
            if (!state.ManualFlightModeActive || state.ActivePanelMode != ShipDevicePanelMode.ManualFlight)
            {
                throw new InvalidOperationException("Cockpit helm did not enter manual flight mode state.");
            }

            var engine = InteractDevice(ShipDeviceType.EngineRoomPowerScreen, context);
            engine.Interact(context);
            if (!state.EngineOverclockUsedThisRun || state.EngineOverclockActivationCount != 1)
            {
                throw new InvalidOperationException($"Engine overclock must activate once per run. Count={state.EngineOverclockActivationCount}");
            }

            InteractDevice(ShipDeviceType.ControlRoomMainScreen, context);
            deviceHud.RefreshPanel();
            if (!deviceHud.PanelText.enabled || !deviceHud.PanelText.text.Contains("CCTV A/D"))
            {
                throw new InvalidOperationException("Control room screen did not display CCTV A/D status.");
            }

            PressCctvKey(Key.D, deviceHud);
            if (state.CurrentCctvTarget != ShipCctvTarget.CargoHold)
            {
                throw new InvalidOperationException($"D key must switch CCTV to Cargo Hold. Current={state.CurrentCctvTarget}");
            }

            PressCctvKey(Key.A, deviceHud);
            if (state.CurrentCctvTarget != ShipCctvTarget.Cockpit)
            {
                throw new InvalidOperationException($"A key must switch CCTV to Cockpit. Current={state.CurrentCctvTarget}");
            }

            InteractDevice(ShipDeviceType.ArmoryTurretHandle, context);
            if (!state.TurretManualModeActive || state.ActivePanelMode != ShipDevicePanelMode.TurretManual)
            {
                throw new InvalidOperationException("Armory turret handle did not enter manual turret mode state.");
            }

            InteractDevice(ShipDeviceType.SupplyRoomStorageCabinet, context);
            deviceHud.RefreshPanel();
            if (!deviceHud.PanelText.text.Contains("Slot 3"))
            {
                throw new InvalidOperationException("Supply storage must display the default 3 storage slots.");
            }

            InteractDevice(ShipDeviceType.CargoHoldCargoStatus, context);
            deviceHud.RefreshPanel();
            if (!deviceHud.PanelText.text.Contains("Loss: 0%"))
            {
                throw new InvalidOperationException("Cargo status must display cargo loss percent.");
            }

            return $"Devices=6; ActivePanel={state.ActivePanelMode}; CCTV={state.CurrentCctvTarget}; EngineOverclockCount={state.EngineOverclockActivationCount}; PanelText={deviceHud.PanelText.enabled}";
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

                if (string.IsNullOrWhiteSpace(device.DisplayName) || string.IsNullOrWhiteSpace(device.InteractionPrompt))
                {
                    throw new InvalidOperationException(deviceType + " must provide display name and prompt.");
                }

                if (!device.CanInteract(context, out var failureReason))
                {
                    throw new InvalidOperationException(deviceType + " refused interaction: " + failureReason);
                }

                device.Interact(context);
                return device;
            }

            throw new InvalidOperationException("Missing runtime phase 6 device: " + deviceType);
        }

        private static void PressCctvKey(Key key, ShipDeviceHud deviceHud)
        {
            if (Keyboard.current == null)
            {
                throw new InvalidOperationException("Keyboard input is required for Phase 6 CCTV smoke.");
            }

            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
            InputSystem.Update();
            deviceHud.ProcessDeviceInput();
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            InputSystem.Update();
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
            builder.AppendLine($"Phase 6 room interactions smoke completed: {request.Id}");
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
