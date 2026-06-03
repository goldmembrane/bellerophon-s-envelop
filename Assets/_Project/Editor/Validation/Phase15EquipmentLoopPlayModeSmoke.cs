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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class Phase15EquipmentLoopPlayModeSmoke
    {
        private const string RequestFileName = "Phase15EquipmentLoopSmoke.request";
        private const string ActiveFileName = "Phase15EquipmentLoopSmoke.active";
        private const string ErrorsFileName = "Phase15EquipmentLoopSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 35d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase15EquipmentLoopPlayModeSmoke()
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
                throw new TimeoutException($"Phase 15 equipment loop smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 15 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 15 smoke must start from Edit mode.");
            }

            Phase15EquipmentLoopBootstrap.EnsurePhase15Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase15EquipmentLoopBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase15EquipmentLoopEditorValidation.Run();

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

            if (SceneManager.GetActiveScene().path != Phase15EquipmentLoopBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase15EquipmentLoopBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 15 smoke captured Unity errors."));
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
            var equipmentController = UnityEngine.Object.FindFirstObjectByType<PlayerEquipmentController>();
            var shopController = UnityEngine.Object.FindFirstObjectByType<EquipmentShopController>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            if (startController == null ||
                deviceState == null ||
                deviceHud == null ||
                equipmentController == null ||
                shopController == null ||
                settlementController == null ||
                maintenanceController == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 15 start, device, HUD, equipment, shop, settlement, and maintenance controllers.");
            }

            ClickButtonThroughUi(startController.YesButton);
            if (!startController.CurrentSession.Equipment.HasBasicProtectiveSuit ||
                startController.CurrentSession.Equipment.GetHandSlot(0).ItemKind != EquipmentItemKind.Stick ||
                deviceState.CurrentEquipmentState.GetHandSlot(0).ItemKind != EquipmentItemKind.Stick)
            {
                throw new InvalidOperationException("Association start must issue basic suit and one stick into equipment state.");
            }

            equipmentController.RefreshHudForValidation();
            if (equipmentController.EquipmentHudText == null ||
                !equipmentController.EquipmentHudText.text.Contains("Stick"))
            {
                throw new InvalidOperationException("Equipment HUD must show the issued stick.");
            }

            ClickButtonThroughUi(startController.TutorialContractButton);
            deviceState.TickTransportRun(60f);
            settlementController.ProcessTransportArrival();
            if (!settlementController.IsSettlementVisible ||
                settlementController.CurrentSession == null ||
                settlementController.CurrentSession.Phase != GameSessionPhase.Completed)
            {
                throw new InvalidOperationException("Tutorial transport must complete before Phase 15 shop validation.");
            }

            settlementController.ContinueToMaintenance();
            deviceState.ActivateDevice(ShipDeviceType.SupplyRoomStorageCabinet);
            deviceHud.RefreshPanel();
            if (deviceHud.PanelText == null ||
                !deviceHud.PanelText.text.Contains("Basic Protective Suit") ||
                !deviceHud.PanelText.text.Contains("Hand 1: Stick"))
            {
                throw new InvalidOperationException("Supply storage panel must show suit, hand slots, and the issued stick.");
            }

            ClickButtonThroughUi(maintenanceController.ShopButton);
            if (!shopController.IsShopVisible ||
                shopController.BodyText == null ||
                !shopController.BodyText.text.Contains("Musket") ||
                !shopController.BodyText.text.Contains("Treatment items"))
            {
                throw new InvalidOperationException("Shop buy tab must expose Phase 15 weapons and data-only categories.");
            }

            ClickButtonThroughUi(shopController.BuyMusketButton);
            if (startController.CurrentSession.Wallet.Credits != 650 ||
                startController.CurrentSession.Equipment.GetHandSlot(1).ItemKind != EquipmentItemKind.Musket ||
                deviceState.CurrentEquipmentState.ActiveHandSlotIndex != 1)
            {
                throw new InvalidOperationException("Buying a musket must spend $450 and equip it into the second hand slot.");
            }

            ClickButtonThroughUi(shopController.SellTabButton);
            if (shopController.BodyText == null ||
                !shopController.BodyText.text.Contains("Personal cargo sale slot") ||
                !shopController.BodyText.text.Contains("data-only"))
            {
                throw new InvalidOperationException("Shop sell tab must remain a Phase 15 data-only skeleton.");
            }

            ClickButtonThroughUi(shopController.CloseButton);
            if (shopController.IsShopVisible)
            {
                throw new InvalidOperationException("Phase 15 shop close button must hide the shop after purchase.");
            }

            ClickButtonThroughUi(maintenanceController.AssociationContractButton);
            settlementController.ResetArrivalGateForValidation();
            var followUp = maintenanceController.CurrentSession;
            if (followUp == null ||
                followUp.Phase != GameSessionPhase.Transporting ||
                !deviceState.HasActiveTransportRun)
            {
                throw new InvalidOperationException("Follow-up transport must be active before Phase 15 intruder combat validation.");
            }

            deviceState.StartTransportHazardForValidation(TransportHazardState.None);
            var started = false;
            for (var i = 0; i < 200 && !started; i++)
            {
                started = deviceState.TickSeedIntruderOccurrenceForCurrentRun(
                    SeedIntruderRules.OccurrenceCheckIntervalSeconds,
                    followUp);
            }

            if (!started || !deviceState.HasActiveSeedIntruder)
            {
                throw new InvalidOperationException("Phase 15 combat validation requires an active Parvum.");
            }

            deviceState.SelectEquipmentHandSlot(0);
            var stickHit = equipmentController.UseActiveEquipmentForValidation(false);
            if (stickHit.Outcome != EquipmentUseOutcome.MeleeHit ||
                stickHit.Damage != EquipmentRules.StickDamage ||
                deviceState.CurrentSeedIntruder.Intruder.CurrentHealth != 25)
            {
                throw new InvalidOperationException("Stick must damage the active Parvum by the confirmed 30 damage.");
            }

            deviceState.TickEquipmentState(EquipmentRules.StickUseDelaySeconds);
            deviceState.SelectEquipmentHandSlot(1);
            var reload = equipmentController.ReloadActiveEquipmentForValidation();
            var musketHit = equipmentController.UseActiveEquipmentForValidation(true);
            if (reload.Outcome != EquipmentUseOutcome.ReloadSkeleton ||
                musketHit.Outcome != EquipmentUseOutcome.RangedHit ||
                musketHit.Mode != EquipmentUseMode.PrecisionAim ||
                musketHit.Damage != EquipmentRules.MusketDamage ||
                !deviceState.CurrentSeedIntruder.IsResolved)
            {
                throw new InvalidOperationException("Musket precision fire must damage Parvum and reload must remain a skeleton.");
            }

            return $"Wallet=650; Slots=2/3; StickHit={stickHit.Damage}; MusketHit={musketHit.Damage}; Reload={reload.Outcome}; Intruder={deviceState.CurrentSeedIntruder.Intruder.Resolution}; Shop=BuySellSkeleton";
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 15 button.");
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 15 UI click requires an active EventSystem.");
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
            if (results.Count == 0)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, position, null))
                {
                    throw new InvalidOperationException(
                        $"Phase 15 button is not reachable by UI raycast: {button.name}; Position={position}; Hits=none");
                }

                ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                return;
            }

            var firstHit = results[0].gameObject;
            if (firstHit != button.gameObject && !firstHit.transform.IsChildOf(button.transform))
            {
                var hitNames = string.Join(", ", results.Select(result => result.gameObject.name));
                throw new InvalidOperationException(
                    $"Phase 15 button is blocked by another UI graphic: {button.name}; Position={position}; Hits={hitNames}");
            }

            ExecuteEvents.ExecuteHierarchy(firstHit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(firstHit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(firstHit, pointer, ExecuteEvents.pointerClickHandler);
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
            builder.AppendLine($"Phase 15 equipment loop smoke completed: {request.Id}");
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
