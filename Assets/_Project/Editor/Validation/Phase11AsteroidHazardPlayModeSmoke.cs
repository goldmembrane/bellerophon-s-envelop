using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class Phase11AsteroidHazardPlayModeSmoke
    {
        private const string RequestFileName = "Phase11AsteroidHazardSmoke.request";
        private const string ActiveFileName = "Phase11AsteroidHazardSmoke.active";
        private const string ErrorsFileName = "Phase11AsteroidHazardSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase11AsteroidHazardPlayModeSmoke()
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
                throw new TimeoutException($"Phase 11 asteroid hazard smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 11 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 11 smoke must start from Edit mode.");
            }

            Phase11AsteroidHazardBootstrap.EnsurePhase11Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase11AsteroidHazardBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase11AsteroidHazardEditorValidation.Run();

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

            if (SceneManager.GetActiveScene().path != Phase11AsteroidHazardBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase11AsteroidHazardBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 11 smoke captured Unity errors."));
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

            var startController = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            var contractBoardController = UnityEngine.Object.FindFirstObjectByType<ContractBoardController>();
            if (startController == null ||
                deviceState == null ||
                deviceHud == null ||
                settlementController == null ||
                maintenanceController == null ||
                contractBoardController == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 11 start, device, HUD, settlement, maintenance, and contract board controllers.");
            }

            ClickButtonThroughUi(startController.YesButton);
            ClickButtonThroughUi(startController.TutorialContractButton);

            if (deviceState.HasActiveTransportHazard)
            {
                throw new InvalidOperationException("Tutorial transport must not start an asteroid hazard.");
            }

            deviceState.TickTransportRun(60f);
            settlementController.ProcessTransportArrival();
            if (!settlementController.IsSettlementVisible ||
                settlementController.CurrentSession == null ||
                settlementController.CurrentSession.Phase != GameSessionPhase.Completed)
            {
                throw new InvalidOperationException("Tutorial transport must complete before Phase 11 follow-up hazard validation.");
            }

            settlementController.ContinueToMaintenance();
            if (!maintenanceController.IsMaintenanceVisible ||
                !maintenanceController.ContractBoardButton.interactable)
            {
                throw new InvalidOperationException("Maintenance screen must expose the contract board entry for a ready follow-up association contract.");
            }

            ClickButtonThroughUi(maintenanceController.ContractBoardButton);
            if (!contractBoardController.IsBoardVisible ||
                !contractBoardController.AssociationContractButton.interactable ||
                !contractBoardController.AcceptContractButton.interactable ||
                contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Contract board must expose a ready follow-up association contract.");
            }

            ClickButtonThroughUi(contractBoardController.AssociationContractButton);
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                contractBoardController.SelectedContractId != "association-local-001")
            {
                throw new InvalidOperationException("Association category must select the follow-up contract without starting transport.");
            }

            ClickButtonThroughUi(contractBoardController.AcceptContractButton);
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                maintenanceController.CurrentSession.PendingTransportContractCount != 1 ||
                !contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Accept must queue the follow-up contract before Start Run begins transport.");
            }

            ClickButtonThroughUi(contractBoardController.StartRunButton);
            var followUp = maintenanceController.CurrentSession;
            if (maintenanceController.IsMaintenanceVisible ||
                contractBoardController.IsBoardVisible ||
                followUp.Phase != GameSessionPhase.Transporting ||
                !deviceState.HasActiveTransportRun)
            {
                throw new InvalidOperationException("Follow-up transport must start before Phase 11 hazard validation.");
            }

            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidFieldSmall(0, 10));
            var sessionHazard = deviceState.CurrentTransportHazard;
            deviceHud.RefreshTransportStatus();
            if (deviceHud.TransportStatusText == null ||
                !deviceHud.TransportStatusText.text.Contains("Hazard: Asteroid Field Small"))
            {
                throw new InvalidOperationException("Transport HUD must show the active small asteroid field hazard.");
            }

            var repairBeforeAuto = ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState);
            deviceState.TickTransportRun(sessionHazard.DurationSeconds);
            var autoResult = deviceState.LastTransportHazardResult;
            var repairAfterAuto = ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState);
            if (deviceState.HasActiveTransportHazard ||
                autoResult.Resolution != TransportHazardResolution.DirectHit ||
                autoResult.RoomDamages.Length <= 0 ||
                autoResult.RoomDamages[0].Damage != TransportHazardRules.AsteroidFieldSmallDamage ||
                repairAfterAuto <= repairBeforeAuto)
            {
                throw new InvalidOperationException(
                    $"Auto pilot ignored small asteroid field must damage ship. Result={autoResult.Resolution}; Repair={repairAfterAuto}");
            }

            deviceState.SetShipState(ShipState.CreateDefault());
            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidFieldLarge(0, 10));
            deviceState.ActivateDevice(ShipDeviceType.CockpitHelm);
            deviceState.ApplyManualFlightInput(1f, 1f, 1f);
            deviceState.TickTransportRun(10f);
            var manualResult = deviceState.LastTransportHazardResult;
            var manualRepairCost = ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState);
            if (deviceState.HasActiveTransportHazard ||
                manualResult.Resolution != TransportHazardResolution.Avoided ||
                manualRepairCost != 0)
            {
                throw new InvalidOperationException(
                    $"Manual flight asteroid avoidance must prevent damage. Result={manualResult.Resolution}; Repair={manualRepairCost}");
            }

            deviceState.SetShipState(ShipState.CreateDefault());
            deviceState.ExitManualFlightToAutoPilot();
            deviceState.StartTransportHazardForValidation(TransportHazardState.Start(
                TransportHazardType.CargoFreedomLeagueRegion,
                0,
                6));
            deviceState.TickTransportRun(6f);
            var cargoFreedomResult = deviceState.LastTransportHazardResult;
            if (cargoFreedomResult.HazardType != TransportHazardType.CargoFreedomLeagueRegion ||
                cargoFreedomResult.BoardingEventCount <= 0 ||
                cargoFreedomResult.RoomDamages.Length != 0)
            {
                throw new InvalidOperationException(
                    $"Cargo Freedom League region must create boarding events without invented room damage. Boardings={cargoFreedomResult.BoardingEventCount}; Damage={cargoFreedomResult.RoomDamages.Length}");
            }

            deviceState.SetShipState(ShipState.CreateDefault());
            deviceState.ExitManualFlightToAutoPilot();
            deviceState.StartTransportHazardForValidation(TransportHazardState.Start(
                TransportHazardType.SpacePirateRegion,
                0,
                60));
            deviceState.TickTransportRun(60f);
            var pirateResult = deviceState.LastTransportHazardResult;
            var pirateRepairCost = ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState);
            if (pirateResult.HazardType != TransportHazardType.SpacePirateRegion ||
                pirateResult.BoardingEventCount <= 0 ||
                pirateResult.BombardmentHitCount <= 0 ||
                pirateResult.RoomDamages.Length != pirateResult.BombardmentHitCount ||
                pirateResult.RoomDamages[0].Damage != TransportHazardRules.SpacePirateBombardmentDamage ||
                pirateRepairCost <= 0)
            {
                throw new InvalidOperationException(
                    $"Space Pirate region must combine boarding and bombardment. Boardings={pirateResult.BoardingEventCount}; Bombardment={pirateResult.BombardmentHitCount}; Repair={pirateRepairCost}");
            }

            deviceState.SetShipState(ShipState.CreateDefault());
            deviceState.ExitManualFlightToAutoPilot();
            deviceState.StartTransportHazardForValidation(TransportHazardState.Start(
                TransportHazardType.AlienLifeRegion,
                0,
                30));
            deviceState.TickTransportRun(30f);
            var alienResult = deviceState.LastTransportHazardResult;
            if (alienResult.HazardType != TransportHazardType.AlienLifeRegion ||
                alienResult.BoardingEventCount <= 0 ||
                alienResult.RoomDamages.Length != 0)
            {
                throw new InvalidOperationException(
                    $"Alien Life region must create boarding events without direct ship-damage placeholder. Boardings={alienResult.BoardingEventCount}; Damage={alienResult.RoomDamages.Length}");
            }

            return $"Asteroid={autoResult.Resolution}/{autoResult.RoomDamages[0].Damage}; Manual={manualResult.Resolution}; CargoFreedomBoardings={cargoFreedomResult.BoardingEventCount}; PirateBoardings={pirateResult.BoardingEventCount}; PirateBombardment={pirateResult.BombardmentHitCount}; AlienBoardings={alienResult.BoardingEventCount}";
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 11 button.");
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 11 UI click requires an active EventSystem.");
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
                hitButton = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, position, null);
                if (!hitButton)
                {
                    var hitNames = results.Count == 0
                        ? "none"
                        : string.Join(", ", results.Select(result => result.gameObject.name));
                    throw new InvalidOperationException(
                        $"Phase 11 button is not reachable by UI raycast: {button.name}; Position={position}; Hits={hitNames}");
                }
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
            builder.AppendLine($"Phase 11 asteroid hazard smoke completed: {request.Id}");
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
