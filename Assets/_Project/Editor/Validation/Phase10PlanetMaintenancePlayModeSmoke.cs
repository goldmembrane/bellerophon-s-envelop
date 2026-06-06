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
    internal static class Phase10PlanetMaintenancePlayModeSmoke
    {
        private const string RequestFileName = "Phase10PlanetMaintenanceSmoke.request";
        private const string ActiveFileName = "Phase10PlanetMaintenanceSmoke.active";
        private const string ErrorsFileName = "Phase10PlanetMaintenanceSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase10PlanetMaintenancePlayModeSmoke()
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
                throw new TimeoutException($"Phase 10 planet maintenance smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 10 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 10 smoke must start from Edit mode.");
            }

            Phase10PlanetMaintenanceBootstrap.EnsurePhase10Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase10PlanetMaintenanceBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase10PlanetMaintenanceEditorValidation.Run();

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

            if (SceneManager.GetActiveScene().path != Phase10PlanetMaintenanceBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase10PlanetMaintenanceBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 10 smoke captured Unity errors."));
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
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            var contractBoardController = UnityEngine.Object.FindFirstObjectByType<ContractBoardController>();
            var shipUpgradeController = UnityEngine.Object.FindFirstObjectByType<ShipUpgradeController>();
            if (startController == null ||
                playerInput == null ||
                deviceState == null ||
                settlementController == null ||
                maintenanceController == null ||
                contractBoardController == null ||
                shipUpgradeController == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 10 start flow, player input, device state, settlement, maintenance, contract board, and upgrade controllers.");
            }

            ClickButtonThroughUi(startController.YesButton);
            ClickButtonThroughUi(startController.TutorialContractButton);

            var damagedShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(1, 100))
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100));
            var expectedRepairCost = ShipStateRules.CalculateRepairCost(damagedShip);
            deviceState.SetShipState(damagedShip);
            deviceState.TickTransportRun(60f);
            settlementController.ProcessTransportArrival();

            if (!settlementController.IsSettlementVisible ||
                settlementController.ContinueToMaintenanceButton == null ||
                !settlementController.ContinueToMaintenanceButton.interactable ||
                !playerInput.CursorLockSuppressed ||
                settlementController.CurrentSession.SettlementResult.PendingRepairCost != expectedRepairCost)
            {
                throw new InvalidOperationException("Phase 10 settlement must expose the maintenance continuation with pending repair cost.");
            }

            var continueRect = settlementController.ContinueToMaintenanceButton.GetComponent<RectTransform>();
            var continuePosition = RectTransformUtility.WorldToScreenPoint(
                null,
                continueRect.TransformPoint(continueRect.rect.center));
            if (!settlementController.ProcessContinueButtonClickForValidation(continuePosition))
            {
                throw new InvalidOperationException("Maintenance continuation click fallback must activate inside the continue button bounds.");
            }

            if (!maintenanceController.IsMaintenanceVisible || settlementController.IsSettlementVisible)
            {
                throw new InvalidOperationException("Maintenance screen must replace the settlement panel.");
            }

            Canvas.ForceUpdateCanvases();
            var maintenanceRect = maintenanceController.MaintenanceRoot.GetComponent<RectTransform>();
            if (maintenanceRect == null || maintenanceRect.rect.width < 1000f || maintenanceRect.rect.height < 600f)
            {
                throw new InvalidOperationException("Maintenance screen must cover the full screen.");
            }

            if (maintenanceController.RoomStatusText == null ||
                !maintenanceController.RoomStatusText.text.Contains("Cargo Hold") ||
                !maintenanceController.RoomStatusText.text.Contains("personal cargo") ||
                !maintenanceController.RoomStatusText.text.Contains("Capacity") ||
                maintenanceController.ContractListText == null ||
                !maintenanceController.ContractListText.text.Contains("Contract Board") ||
                !maintenanceController.ContractListText.text.Contains("Fame") ||
                !maintenanceController.ContractListText.text.Contains("Entry points"))
            {
                throw new InvalidOperationException("Maintenance UI must show room damage effects and the separate contract board entry summary.");
            }

            if (!maintenanceController.RepairButton.interactable ||
                !maintenanceController.ContractBoardButton.interactable ||
                contractBoardController.IsBoardVisible)
            {
                throw new InvalidOperationException("Damaged ship must keep repair available while exposing the separate contract board entry.");
            }

            ClickButtonThroughUi(maintenanceController.UpgradesButton);
            if (!shipUpgradeController.IsUpgradeVisible ||
                maintenanceController.IsMaintenanceVisible ||
                shipUpgradeController.BodyText == null ||
                !shipUpgradeController.BodyText.text.Contains("Durability") ||
                !shipUpgradeController.BodyText.text.Contains("$1000") ||
                shipUpgradeController.PurchaseButtons.Length != Phase10PlanetMaintenanceBootstrap.ShipUpgradeCategoryButtonCount ||
                shipUpgradeController.EquipButtons.Length != Phase10PlanetMaintenanceBootstrap.ShipUpgradeCategoryButtonCount ||
                !shipUpgradeController.PurchaseButtons[0].interactable ||
                shipUpgradeController.EquipButtons[0].gameObject.activeSelf ||
                shipUpgradeController.EquipButtons[0].interactable)
            {
                throw new InvalidOperationException("Upgrade screen must open with durability auto-apply and separate equip actions for other upgrades.");
            }

            ClickButtonThroughUi(shipUpgradeController.PurchaseButtons[0]);
            var purchasedUpgradeSession = maintenanceController.CurrentSession;
            if (purchasedUpgradeSession.Wallet.Credits != 100 ||
                purchasedUpgradeSession.ShipUpgrades.GetPurchasedTier(ShipUpgradeCategory.Durability) != 1 ||
                purchasedUpgradeSession.ShipUpgrades.GetEquippedTier(ShipUpgradeCategory.Durability) != 1 ||
                shipUpgradeController.EquipButtons[0].gameObject.activeSelf ||
                shipUpgradeController.EquipButtons[0].interactable)
            {
                throw new InvalidOperationException("Durability upgrade purchase must spend credits and apply the tier automatically.");
            }

            ClickButtonThroughUi(shipUpgradeController.CloseButton);
            if (!maintenanceController.IsMaintenanceVisible || shipUpgradeController.IsUpgradeVisible)
            {
                throw new InvalidOperationException("Upgrade back button must return to planet maintenance.");
            }

            ClickButtonThroughUi(maintenanceController.ShopButton);
            if (maintenanceController.StatusText == null ||
                !maintenanceController.StatusText.text.Contains("Shop"))
            {
                throw new InvalidOperationException("Shop button must expose an entry point without implementing detailed shop features.");
            }

            ClickButtonThroughUi(maintenanceController.ContractBoardButton);
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.IsMaintenanceVisible ||
                contractBoardController.ContractListText == null ||
                !contractBoardController.ContractListText.text.Contains("Association") ||
                !contractBoardController.ContractListText.text.Contains("Private") ||
                !contractBoardController.ContractListText.text.Contains("Reward") ||
                !contractBoardController.ContractListText.text.Contains("Duration") ||
                !contractBoardController.ContractListText.text.Contains("Required cargo score") ||
                !contractBoardController.ContractListText.text.Contains("Difficulty") ||
                contractBoardController.ContractSlotButtons == null ||
                contractBoardController.ContractSlotButtons.Length == 0 ||
                !contractBoardController.ContractSlotButtons[0].interactable ||
                !contractBoardController.AssociationContractButton.interactable ||
                !contractBoardController.PrivateContractButton.interactable ||
                contractBoardController.AcceptContractButton.interactable ||
                contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Contract board must be a separate screen with selectable contract rows and repair-gated acceptance.");
            }

            ClickButtonThroughUi(contractBoardController.AssociationContractButton);
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                contractBoardController.SelectedContractId != "association-local-001" ||
                contractBoardController.AcceptContractButton.interactable ||
                contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Association category must select a row without accepting while the ship still needs repair.");
            }

            ClickButtonThroughUi(contractBoardController.BackButton);
            if (!maintenanceController.IsMaintenanceVisible ||
                contractBoardController.IsBoardVisible ||
                shipUpgradeController.IsUpgradeVisible)
            {
                throw new InvalidOperationException("Contract board back button must return to planet maintenance without opening upgrades.");
            }

            ClickButtonThroughUi(maintenanceController.RepairButton);
            var repairedSession = maintenanceController.CurrentSession;
            if (repairedSession.Wallet.Credits != 100 - expectedRepairCost ||
                !repairedSession.Wallet.HasUnpaidDebtGrace ||
                repairedSession.SettlementResult.PendingRepairCost != 0 ||
                repairedSession.Ship.GetRoom(ShipRoomId.CargoHold).CurrentDurability != 100 ||
                repairedSession.ShipUpgrades.GetEquippedTier(ShipUpgradeCategory.Durability) != 1 ||
                maintenanceController.RepairButton.interactable ||
                !maintenanceController.ContractBoardButton.interactable ||
                !maintenanceController.RoomStatusText.text.Contains("Cargo Hold: 100% Optimal"))
            {
                throw new InvalidOperationException(
                    $"Repair must charge pending cost and restore next transport readiness. Balance={repairedSession.Wallet.Credits}; Pending={repairedSession.SettlementResult.PendingRepairCost}");
            }

            ClickButtonThroughUi(maintenanceController.ContractBoardButton);
            if (!contractBoardController.IsBoardVisible ||
                !contractBoardController.AssociationContractButton.interactable ||
                !contractBoardController.PrivateContractButton.interactable ||
                !contractBoardController.AcceptContractButton.interactable ||
                contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Repaired ship must unlock selection and acceptance while keeping Start Run disabled until a contract is accepted.");
            }

            ClickButtonThroughUi(contractBoardController.AssociationContractButton);
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                contractBoardController.SelectedContractId != "association-local-001")
            {
                throw new InvalidOperationException("Association button must select the category without immediately starting transport.");
            }

            ClickButtonThroughUi(contractBoardController.ContractSlotButtons[0]);
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                contractBoardController.SelectedContractId != "association-local-001")
            {
                throw new InvalidOperationException("Contract row click must select the listed contract without immediately starting transport.");
            }

            ClickButtonThroughUi(contractBoardController.AcceptContractButton);
            var acceptedAssociation = maintenanceController.CurrentSession;
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.IsMaintenanceVisible ||
                acceptedAssociation.Phase != GameSessionPhase.Completed ||
                acceptedAssociation.PendingTransportContractCount != 1 ||
                !acceptedAssociation.IsTransportContractPending("association-local-001") ||
                contractBoardController.AcceptContractButton.interactable ||
                !contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Accept must add the association follow-up to the pending run without starting transport.");
            }

            ClickButtonThroughUi(contractBoardController.PrivateContractButton);
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                contractBoardController.SelectedContractId != "private-sample-001" ||
                !contractBoardController.AcceptContractButton.interactable)
            {
                throw new InvalidOperationException("Private category must select a second acceptable contract while the board stays open.");
            }

            ClickButtonThroughUi(contractBoardController.AcceptContractButton);
            var acceptedPrivate = maintenanceController.CurrentSession;
            if (!contractBoardController.IsBoardVisible ||
                acceptedPrivate.Phase != GameSessionPhase.Completed ||
                acceptedPrivate.PendingTransportContractCount != 2 ||
                !acceptedPrivate.IsTransportContractPending("association-local-001") ||
                !acceptedPrivate.IsTransportContractPending("private-sample-001") ||
                !contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Contract board must allow multiple accepted contracts before starting a run.");
            }

            ClickButtonThroughUi(contractBoardController.StartRunButton);
            var nextRun = maintenanceController.CurrentSession;
            if (maintenanceController.IsMaintenanceVisible ||
                contractBoardController.IsBoardVisible ||
                nextRun.Phase != GameSessionPhase.Transporting ||
                !nextRun.ActiveTransportContract.HasValue ||
                nextRun.ActiveTransportContract.Value.Id != "association-local-001" ||
                nextRun.ActiveTransportContractCount != 2 ||
                nextRun.PendingTransportContractCount != 0 ||
                !deviceState.HasActiveTransportRun ||
                deviceState.CurrentTransportRun.BaseDurationSeconds != 90 ||
                deviceState.CurrentShipState.GetRoom(ShipRoomId.CargoHold).CurrentDurability != 100)
            {
                throw new InvalidOperationException("Start Run must begin a repaired transport with all accepted contracts.");
            }

            var cargoScore = Mathf.RoundToInt(ShipStateRules.CalculateCargoHoldScore(nextRun.Ship) * 100f);
            return $"RepairCost={expectedRepairCost}; Upgrade=DurabilityT{nextRun.ShipUpgrades.GetEquippedTier(ShipUpgradeCategory.Durability)}; Balance={nextRun.Wallet.Credits}; Contracts={nextRun.ActiveTransportContractCount}; Duration={deviceState.CurrentTransportRun.BaseDurationSeconds}; CargoScore={cargoScore}";
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 10 button.");
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 10 UI click requires an active EventSystem.");
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
                        $"Phase 10 button is not reachable by UI raycast: {button.name}; Position={position}; Hits={hitNames}");
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
            builder.AppendLine($"Phase 10 planet maintenance smoke completed: {request.Id}");
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
