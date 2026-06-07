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
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            var playerStatus = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var state = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            if (playerMotor == null || playerStatus == null || playerInput == null ||
                state == null || deviceHud == null || deviceHud.PanelText == null)
            {
                throw new InvalidOperationException("Runtime scene must contain player, player input/status, phase 6 state, and phase 6 device HUD.");
            }

            var context = new PlayerInteractionContext(playerMotor.gameObject, Camera.main?.transform, new RaycastHit());

            InteractDevice(ShipDeviceType.CockpitHelm, context);
            if (!state.ManualFlightModeActive || state.ActivePanelMode != ShipDevicePanelMode.ManualFlight)
            {
                throw new InvalidOperationException("Cockpit helm did not enter manual flight mode state.");
            }

            var engine = InteractDevice(ShipDeviceType.EngineRoomPowerScreen, context);
            engine.Interact(context);
            deviceHud.RefreshPanel();
            if (!state.EngineOverclockUsedThisRun || state.EngineOverclockActivationCount != 1)
            {
                throw new InvalidOperationException($"Engine overclock must activate once per run. Count={state.EngineOverclockActivationCount}");
            }

            PressDeviceKey(Key.Escape, deviceHud);
            deviceHud.RefreshPanel();
            if (state.ActivePanelMode != ShipDevicePanelMode.None || deviceHud.PanelText.enabled)
            {
                throw new InvalidOperationException("Escape must close the engine room interaction panel.");
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

            state.SwitchControlRoomScreenByRightClick();
            deviceHud.RefreshPanel();
            if (state.CurrentControlRoomScreenMode != ShipControlRoomScreenMode.VerticalRoomList ||
                !deviceHud.PanelText.text.Contains("Vertical Room List"))
            {
                throw new InvalidOperationException("Control room right-click must switch from main CCTV to the vertical room list.");
            }

            if (!playerInput.CursorLockSuppressed || !playerInput.GameplayInputSuppressed)
            {
                throw new InvalidOperationException("Control room screen must unlock the cursor and suppress first-person input while open.");
            }

            var shieldBeforePurification = playerStatus.CurrentShield;
            var armoryPurificationButton = deviceHud.GetControlRoomRoomButtonForValidation(ShipRoomId.Armory);
            ClickButtonThroughUi(armoryPurificationButton);
            deviceHud.RefreshPanel();
            if (!state.CurrentControlRoomPurification.IsActive ||
                state.CurrentControlRoomPurification.TargetRoom != ShipRoomId.Armory)
            {
                throw new InvalidOperationException("Vertical control room list click must start selected-room internal purification.");
            }

            state.TickControlRoomOperations(3f, ShipRoomId.Armory);
            deviceHud.RefreshPanel();
            if (!state.CurrentControlRoomPurification.IsActive ||
                !state.CurrentShipState.GetRoom(ShipRoomId.Armory).IsSealed ||
                state.CurrentShipState.GetRoom(ShipRoomId.Armory).CurrentDurability != 100 ||
                playerStatus.CurrentShield >= shieldBeforePurification ||
                !deviceHud.PanelText.text.Contains("Internal Purification"))
            {
                throw new InvalidOperationException("Internal purification must seal only the selected room, avoid room durability damage, and damage players inside that room.");
            }

            state.TickControlRoomOperations(27f, ShipRoomId.Cockpit);
            if (state.CurrentControlRoomPurification.IsActive ||
                state.CurrentShipState.GetRoom(ShipRoomId.Armory).IsSealed)
            {
                throw new InvalidOperationException("Internal purification must reopen the selected room after 30 seconds.");
            }

            InteractDevice(ShipDeviceType.ControlRoomMainScreen, context);
            deviceHud.RefreshPanel();
            PressControlRoomKey(Key.Escape, deviceHud);
            deviceHud.RefreshPanel();
            if (state.ActivePanelMode != ShipDevicePanelMode.None ||
                deviceHud.PanelText.enabled ||
                playerInput.CursorLockSuppressed ||
                playerInput.GameplayInputSuppressed)
            {
                throw new InvalidOperationException("Escape must close the control room screen and restore first-person input.");
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

            PressDeviceKey(Key.Escape, deviceHud);
            deviceHud.RefreshPanel();
            if (state.ActivePanelMode != ShipDevicePanelMode.None || deviceHud.PanelText.enabled)
            {
                throw new InvalidOperationException("Escape must close the supply room interaction panel.");
            }

            InteractDevice(ShipDeviceType.CargoHoldCargoStatus, context);
            deviceHud.RefreshPanel();
            if (!deviceHud.PanelText.text.Contains("Loss: 0%"))
            {
                throw new InvalidOperationException("Cargo status must display cargo loss percent.");
            }

            state.SetShipState(ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(25, 100)));
            InteractDevice(ShipDeviceType.ControlRoomMainScreen, context);
            PressCctvKey(Key.D, deviceHud);
            deviceHud.RefreshPanel();
            if (state.CurrentCctvTarget != ShipCctvTarget.Cockpit ||
                !deviceHud.PanelText.text.Contains("CCTV Channels: 0/5"))
            {
                throw new InvalidOperationException("Control room damage must disable CCTV channels.");
            }

            state.SetShipState(ShipState.CreateDefault()
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(75, 100)));
            InteractDevice(ShipDeviceType.SupplyRoomStorageCabinet, context);
            deviceHud.RefreshPanel();
            if (state.SupplySlotCount != 0 || !deviceHud.PanelText.text.Contains("Usable Slots: 0/3"))
            {
                throw new InvalidOperationException("Supply room damage must reduce usable storage slots.");
            }

            state.SetShipState(ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(0, 100)));
            InteractDevice(ShipDeviceType.ArmoryTurretHandle, context);
            deviceHud.RefreshPanel();
            if (state.TurretManualModeActive || !deviceHud.PanelText.text.Contains("Manual Turret: Offline"))
            {
                throw new InvalidOperationException("Destroyed armory must disable manual turret operation.");
            }

            return $"Devices=6; ActivePanel={state.ActivePanelMode}; CCTV={state.CurrentCctvTarget}; EngineOverclockCount={state.EngineOverclockActivationCount}; Purification=SelectedRoom; DamageEffects=Linked; PanelText={deviceHud.PanelText.enabled}";
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
            PressControlRoomKey(key, deviceHud);
        }

        private static void PressControlRoomKey(Key key, ShipDeviceHud deviceHud)
        {
            PressDeviceKey(key, deviceHud);
        }

        private static void PressDeviceKey(Key key, ShipDeviceHud deviceHud)
        {
            if (Keyboard.current == null)
            {
                throw new InvalidOperationException("Keyboard input is required for Phase 6 device smoke.");
            }

            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
            InputSystem.Update();
            deviceHud.ProcessDeviceInput();
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            InputSystem.Update();
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 6 button.");
            }

            EnsureRuntimeEventSystem();

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
                hitButton = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, position, null);
                if (!hitButton)
                {
                    var hitNames = results.Count == 0
                        ? "none"
                        : string.Join(", ", results.Select(result => result.gameObject.name));
                    throw new InvalidOperationException(
                        $"Phase 6 button is not reachable by UI raycast: {button.name}; Position={position}; Hits={hitNames}");
                }
            }

            ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static void EnsureRuntimeEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("Phase 6 Runtime EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 6 UI click requires an active EventSystem.");
            }
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
