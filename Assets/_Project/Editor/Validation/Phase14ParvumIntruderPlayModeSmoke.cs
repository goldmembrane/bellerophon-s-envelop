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
    internal static class Phase14ParvumIntruderPlayModeSmoke
    {
        private const string RequestFileName = "Phase14ParvumIntruderSmoke.request";
        private const string ActiveFileName = "Phase14ParvumIntruderSmoke.active";
        private const string ErrorsFileName = "Phase14ParvumIntruderSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase14ParvumIntruderPlayModeSmoke()
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
                throw new TimeoutException($"Phase 14 parvum intruder smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 14 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 14 smoke must start from Edit mode.");
            }

            Phase14ParvumIntruderBootstrap.EnsurePhase14Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase14ParvumIntruderBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase14ParvumIntruderEditorValidation.Run();

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

            if (SceneManager.GetActiveScene().path != Phase14ParvumIntruderBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase14ParvumIntruderBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 14 smoke captured Unity errors."));
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
            var intruderView = UnityEngine.Object.FindFirstObjectByType<SeedIntruderVisualView>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            if (startController == null ||
                deviceState == null ||
                deviceHud == null ||
                intruderView == null ||
                settlementController == null ||
                maintenanceController == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 14 start, device, HUD, intruder view, settlement, and maintenance controllers.");
            }

            ClickButtonThroughUi(startController.YesButton);
            ClickButtonThroughUi(startController.TutorialContractButton);
            if (deviceState.TickSeedIntruderOccurrenceForCurrentRun(20f, startController.CurrentSession) ||
                deviceState.HasActiveSeedIntruder)
            {
                throw new InvalidOperationException("Tutorial transport must not start a seed intruder.");
            }

            deviceState.TickTransportRun(60f);
            settlementController.ProcessTransportArrival();
            if (!settlementController.IsSettlementVisible ||
                settlementController.CurrentSession == null ||
                settlementController.CurrentSession.Phase != GameSessionPhase.Completed)
            {
                throw new InvalidOperationException("Tutorial transport must complete before Phase 14 follow-up intruder validation.");
            }

            settlementController.ContinueToMaintenance();
            if (!maintenanceController.IsMaintenanceVisible ||
                !maintenanceController.AssociationContractButton.interactable)
            {
                throw new InvalidOperationException("Maintenance screen must expose a ready follow-up association contract.");
            }

            ClickButtonThroughUi(maintenanceController.AssociationContractButton);
            settlementController.ResetArrivalGateForValidation();
            var followUp = maintenanceController.CurrentSession;
            if (maintenanceController.IsMaintenanceVisible ||
                followUp.Phase != GameSessionPhase.Transporting ||
                !deviceState.HasActiveTransportRun)
            {
                throw new InvalidOperationException("Follow-up transport must be active before Phase 14 seed intruder checks.");
            }

            deviceState.StartTransportHazardForValidation(TransportHazardState.None);
            var started = false;
            for (var i = 0; i < 200 && !started; i++)
            {
                started = deviceState.TickSeedIntruderOccurrenceForCurrentRun(
                    SeedIntruderRules.OccurrenceCheckIntervalSeconds,
                    followUp);
            }

            if (!started ||
                !deviceState.HasActiveSeedIntruder ||
                deviceState.CurrentSeedIntruder.Kind != SeedIntruderKind.Parvum ||
                deviceState.CurrentExternalTarget.IsActive)
            {
                throw new InvalidOperationException("Follow-up transport must start a Parvum seed intruder without creating an external target.");
            }

            var intruder = deviceState.CurrentSeedIntruder;
            deviceHud.RefreshTransportStatus();
            intruderView.RefreshView();
            if (deviceHud.TransportStatusText == null ||
                !deviceHud.TransportStatusText.text.Contains("Intruder: Parvum"))
            {
                throw new InvalidOperationException("Transport HUD must show the active Parvum seed intruder.");
            }

            AssertParvumVisualAtCurrentRoom(deviceState, intruderView, "spawn");

            var repairBefore = ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState);
            deviceState.TickTransportRun(SeedIntruderRules.ParvumAttackDelaySeconds);
            intruderView.RefreshView();
            AssertParvumVisualAtCurrentRoom(deviceState, intruderView, "first attack");
            var damageAfterFirstAttack = deviceState.CurrentSeedIntruder.TotalRoomDamageApplied;
            var repairAfter = ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState);
            if (damageAfterFirstAttack != SeedIntruderRules.ParvumShipFacilityDamage ||
                repairAfter <= repairBefore)
            {
                throw new InvalidOperationException(
                    $"Parvum must damage a ship room every attack delay. Damage={damageAfterFirstAttack}; Repair={repairAfter}");
            }

            var neutralized = deviceState.NeutralizeActiveSeedIntruderForValidation();
            intruderView.RefreshView();
            if (intruderView.IsViewActive)
            {
                throw new InvalidOperationException("Neutralized Parvum visual placeholder must hide immediately.");
            }

            var repairAfterNeutralize = ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState);
            deviceState.TickTransportRun(2f);
            if (!neutralized.IsResolved ||
                neutralized.Intruder.Resolution != IntruderResolution.Neutralized ||
                ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState) != repairAfterNeutralize)
            {
                throw new InvalidOperationException("Neutralized Parvum must stop applying further ship damage.");
            }

            CompleteRemainingTransport(deviceState);
            settlementController.ProcessTransportArrival();
            var completed = settlementController.CurrentSession;
            if (!settlementController.IsSettlementVisible ||
                completed == null ||
                completed.Phase != GameSessionPhase.Completed ||
                completed.SettlementResult.PendingRepairCost <= 0 ||
                settlementController.SettlementBodyText == null ||
                !settlementController.SettlementBodyText.text.Contains("Repair charge due at maintenance"))
            {
                throw new InvalidOperationException("Parvum ship damage must remain as a maintenance repair charge after arrival settlement.");
            }

            return $"Intruder={intruder.Kind}; Check={deviceState.SeedIntruderCheckCount}; Target={intruder.TargetRoom}; Visible=True; Damage={damageAfterFirstAttack}; RepairCost={completed.SettlementResult.PendingRepairCost}";
        }

        private static void AssertParvumVisualAtCurrentRoom(
            ShipDeviceInteractionState deviceState,
            SeedIntruderVisualView intruderView,
            string phaseName)
        {
            var currentRoom = deviceState.CurrentSeedIntruder.Intruder.CurrentRoom;
            var anchor = intruderView.GetAnchorForValidation(currentRoom);
            if (anchor == null)
            {
                throw new InvalidOperationException($"Phase 14 Parvum visual missing anchor for {currentRoom} during {phaseName}.");
            }

            if (!intruderView.IsViewActive ||
                intruderView.LastDisplayedRoom != currentRoom ||
                intruderView.ParvumVisualRoot == null ||
                Vector3.Distance(intruderView.ParvumVisualRoot.transform.position, anchor.position) > 0.01f)
            {
                throw new InvalidOperationException(
                    $"Phase 14 Parvum visual must be active at {currentRoom} during {phaseName}.");
            }
        }

        private static void CompleteRemainingTransport(ShipDeviceInteractionState deviceState)
        {
            var guard = 0;
            while (!deviceState.CurrentTransportRun.IsComplete && guard < 40)
            {
                var step = Mathf.Min(10f, deviceState.CurrentTransportRun.RemainingSeconds);
                deviceState.TickTransportRun(step <= 0f ? 0.1f : step);
                guard++;
            }

            if (!deviceState.CurrentTransportRun.IsComplete)
            {
                throw new InvalidOperationException("Phase 14 smoke could not complete the follow-up transport run.");
            }
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 14 button.");
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 14 UI click requires an active EventSystem.");
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
                        $"Phase 14 button is not reachable by UI raycast: {button.name}; Position={position}; Hits={hitNames}");
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
            builder.AppendLine($"Phase 14 parvum intruder smoke completed: {request.Id}");
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
