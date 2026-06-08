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

            Phase20PresentationBootstrap.EnsurePhase20Assets();
            request.Details = AppendDetails(request.Details, "Scene=Phase20Restored");
            WriteLog(request, false, null);
            TryDelete(ActivePath);
            TryDelete(ErrorsPath);
        }

        private static string AppendDetails(string details, string addition)
        {
            if (string.IsNullOrWhiteSpace(details))
            {
                return addition;
            }

            return details + "; " + addition;
        }

        private static string ValidateRuntime()
        {
            if (SceneManager.GetActiveScene().name != CargoRunSceneName)
            {
                throw new InvalidOperationException($"Expected active scene {CargoRunSceneName}, got {SceneManager.GetActiveScene().name}.");
            }

            var startController = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var playerStatus = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            var equipmentController = UnityEngine.Object.FindFirstObjectByType<PlayerEquipmentController>();
            var shopController = UnityEngine.Object.FindFirstObjectByType<EquipmentShopController>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var planetController = UnityEngine.Object.FindFirstObjectByType<PlanetStayController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            var contractBoardController = UnityEngine.Object.FindFirstObjectByType<ContractBoardController>();
            var personalCargoController = UnityEngine.Object.FindFirstObjectByType<PersonalCargoController>();
            if (startController == null ||
                playerInput == null ||
                playerStatus == null ||
                deviceState == null ||
                deviceHud == null ||
                equipmentController == null ||
                shopController == null ||
                settlementController == null ||
                planetController == null ||
                maintenanceController == null ||
                contractBoardController == null ||
                personalCargoController == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 15 start, device, HUD, equipment, shop, settlement, maintenance, contract board, and personal cargo controllers.");
            }

            startController.FastForwardAssociationContractForValidation();
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

            settlementController.ContinueToPlanet();
            if (!planetController.IsPlanetVisible || !planetController.ShopButton.interactable)
            {
                throw new InvalidOperationException("Planet hub must be visible after settlement and keep the shop entry enabled.");
            }

            ClickButtonThroughUi(planetController.ShopButton);
            if (!shopController.IsShopVisible ||
                planetController.IsPlanetVisible ||
                shopController.BodyText == null ||
                !shopController.BodyText.text.Contains("Musket"))
            {
                throw new InvalidOperationException("Planet hub shop button must open the equipment shop.");
            }

            ClickButtonThroughUi(shopController.CloseButton);
            if (shopController.IsShopVisible || !planetController.IsPlanetVisible)
            {
                throw new InvalidOperationException("Closing the planet hub shop must return to the planet hub.");
            }

            ClickButtonThroughUi(planetController.RepairShopButton);
            if (!maintenanceController.IsMaintenanceVisible || planetController.IsPlanetVisible)
            {
                throw new InvalidOperationException("Planet hub repair button must open maintenance after returning from shop.");
            }

            deviceState.ActivateDevice(ShipDeviceType.SupplyRoomStorageCabinet);
            deviceHud.RefreshPanel();
            if (deviceHud.PanelText == null ||
                !deviceHud.PanelText.text.Contains("Basic Protective Suit") ||
                !deviceHud.PanelText.text.Contains("Hand 1: Stick") ||
                !deviceHud.PanelText.text.Contains("Hand 3: Empty") ||
                !deviceHud.PanelText.text.Contains("Tabs: All, Weapon, Protective, Treatment, Enhancement, Utility"))
            {
                throw new InvalidOperationException("Supply storage panel must show suit, three base hand slots, storage tabs, and the issued stick.");
            }

            ClickButtonThroughUi(maintenanceController.PersonalCargoButton);
            if (!personalCargoController.IsCargoVisible ||
                personalCargoController.BodyText == null ||
                !personalCargoController.BodyText.text.Contains("Current planet trait") ||
                !personalCargoController.CollectButton.interactable)
            {
                throw new InvalidOperationException("Personal cargo collection screen must open from maintenance with a collect action.");
            }

            ClickButtonThroughUi(personalCargoController.CollectButton);
            if (startController.CurrentSession.PersonalCargoHold.Count != 1 ||
                !personalCargoController.BodyText.text.Contains("Water Rich Common Cargo") ||
                !playerInput.CursorLockSuppressed)
            {
                throw new InvalidOperationException("Collecting personal cargo must add one free cargo item without releasing UI cursor mode.");
            }

            ClickButtonThroughUi(personalCargoController.CloseButton);
            if (!maintenanceController.IsMaintenanceVisible || personalCargoController.IsCargoVisible)
            {
                throw new InvalidOperationException("Personal cargo back button must return to maintenance.");
            }

            ClickButtonThroughUi(maintenanceController.ShopButton);
            if (!shopController.IsShopVisible ||
                shopController.BodyText == null ||
                !shopController.BodyText.text.Contains("Musket") ||
                !shopController.BodyText.text.Contains("Shotgun") ||
                !shopController.BodyText.text.Contains("Protective Suit") ||
                !shopController.BodyText.text.Contains("Strength Enhancer") ||
                !shopController.BodyText.text.Contains("Common products") ||
                !shopController.BodyText.text.Contains("Fame-limited products") ||
                !shopController.BodyText.text.Contains("Special products") ||
                !shopController.BodyText.text.Contains("Presence Detector"))
            {
                throw new InvalidOperationException("Shop buy tab must expose Step 8 common, fame-limited, and special product sections.");
            }

            ClickButtonThroughUi(shopController.BuyMusketButton);
            if (startController.CurrentSession.Wallet.Credits != 650 ||
                startController.CurrentSession.Equipment.GetHandSlot(1).ItemKind != EquipmentItemKind.Musket ||
                deviceState.CurrentEquipmentState.ActiveHandSlotIndex != 1 ||
                !playerInput.CursorLockSuppressed)
            {
                throw new InvalidOperationException("Buying a musket must spend $450, equip it, and keep UI cursor mode active.");
            }

            ClickButtonThroughUi(shopController.BuyFlashlightButton);
            if (startController.CurrentSession.Wallet.Credits != 625 ||
                startController.CurrentSession.Equipment.GetHandSlot(2).ItemKind != EquipmentItemKind.Flashlight ||
                deviceState.CurrentEquipmentState.ActiveHandSlotIndex != 2)
            {
                throw new InvalidOperationException("Buying a flashlight must use the third default hand slot.");
            }

            ClickButtonThroughUi(shopController.BuyInjuryRelieverButton);
            if (startController.CurrentSession.Wallet.Credits != 500 ||
                startController.CurrentSession.Equipment.GetSupplySlot(0).ItemKind != EquipmentItemKind.InjuryReliever ||
                startController.CurrentSession.Equipment.GetSupplySlot(0).PurchasePriceCredits != EquipmentRules.InjuryRelieverPriceCredits)
            {
                throw new InvalidOperationException("Buying treatment equipment must store it in supply storage with purchase metadata.");
            }

            ClickButtonThroughUi(shopController.BuyProtectiveSuitButton);
            if (startController.CurrentSession.Wallet.Credits != 100 ||
                startController.CurrentSession.Equipment.GetSupplySlot(1).ItemKind != EquipmentItemKind.ProtectiveSuit)
            {
                throw new InvalidOperationException("Buying protective equipment must store it in the next supply slot.");
            }

            ClickButtonThroughUi(shopController.BuyStrengthEnhancerButton);
            if (startController.CurrentSession.Wallet.Credits != 0 ||
                startController.CurrentSession.Equipment.GetSupplySlot(2).ItemKind != EquipmentItemKind.StrengthEnhancer)
            {
                throw new InvalidOperationException("Buying enhancement equipment must use the remaining base supply slot.");
            }

            deviceState.SetPlayerStatusForValidation(playerStatus);
            var flashlight = equipmentController.UseActiveEquipmentForValidation(false);
            deviceState.TickEquipmentState(0.5f);
            playerStatus.SetVitalsForValidation(60, 20);
            var treatment = deviceState.UseSupplyItem(0);
            var protection = deviceState.UseSupplyItem(1);
            var strength = deviceState.UseSupplyItem(2);
            startController.ApplySessionState(startController.CurrentSession.WithEquipment(deviceState.CurrentEquipmentState));
            equipmentController.RefreshHudForValidation();
            if (flashlight.Outcome != EquipmentUseOutcome.UtilityActivated ||
                !deviceState.CurrentEquipmentState.HasActiveFlashlight ||
                treatment.Outcome != EquipmentUseOutcome.TreatmentApplied ||
                treatment.HealthDelta != EquipmentRules.InjuryRelieverHealAmount ||
                playerStatus.CurrentHealth != 85 ||
                protection.Outcome != EquipmentUseOutcome.ProtectiveEquipped ||
                deviceState.CalculateIncomingDamageAfterProtection(50) != 35 ||
                strength.Outcome != EquipmentUseOutcome.EnhancementApplied ||
                !deviceState.CurrentEquipmentState.HasActiveStrengthEnhancer ||
                !equipmentController.EquipmentHudText.text.Contains("Strength: +40%") ||
                !equipmentController.EquipmentHudText.text.Contains("Flashlight:"))
            {
                throw new InvalidOperationException("Step 8 item use must apply flashlight, treatment, protection, and enhancement effects.");
            }

            ClickButtonThroughUi(shopController.SellTabButton);
            var sellRows = shopController.SellItemButtons;
            if (shopController.BodyText == null ||
                !shopController.BodyText.text.Contains("Purchased item disposal") ||
                !shopController.BodyText.text.Contains("Selected: None") ||
                !shopController.BodyText.text.Contains("Sale $4") ||
                !shopController.BodyText.text.Contains("Contract cargo: Not sellable") ||
                !shopController.BodyText.text.Contains("Water Rich Common Cargo") ||
                !shopController.BodyText.text.Contains("Sale $50") ||
                !shopController.BodyText.text.Contains("Use the numbered row buttons") ||
                shopController.SellSelectedItemButton == null ||
                shopController.SellSelectedItemButton.interactable ||
                sellRows.Length < 4 ||
                !sellRows[0].interactable ||
                !sellRows[3].interactable)
            {
                throw new InvalidOperationException("Shop sell tab must list sell candidates and require selecting one before sale.");
            }

            ClickButtonThroughUi(sellRows[3]);
            if (!shopController.BodyText.text.Contains("Selected: Personal Cargo 1 Water Rich Common Cargo") ||
                !shopController.SellSelectedItemButton.interactable)
            {
                throw new InvalidOperationException("Selecting a personal cargo row must enable the Sell Selected button.");
            }

            ClickButtonThroughUi(shopController.SellSelectedItemButton);
            if (startController.CurrentSession.Wallet.Credits != 50 ||
                startController.CurrentSession.PersonalCargoHold.Count != 0 ||
                shopController.SellSelectedItemButton.interactable ||
                !playerInput.CursorLockSuppressed)
            {
                throw new InvalidOperationException("Selling personal cargo must add sale credits, remove it, and keep UI cursor mode active.");
            }

            sellRows = shopController.SellItemButtons;
            ClickButtonThroughUi(sellRows[1]);
            if (!shopController.BodyText.text.Contains("Selected: Hand 3 Flashlight") ||
                !shopController.SellSelectedItemButton.interactable)
            {
                throw new InvalidOperationException("Selecting a purchased equipment row must enable selected disposal.");
            }

            ClickButtonThroughUi(shopController.SellSelectedItemButton);
            if (startController.CurrentSession.Wallet.Credits != 51 ||
                startController.CurrentSession.Equipment.GetHandSlot(1).ItemKind != EquipmentItemKind.Musket ||
                startController.CurrentSession.Equipment.GetHandSlot(2).ItemKind != EquipmentItemKind.None ||
                !playerInput.CursorLockSuppressed)
            {
                throw new InvalidOperationException("Disposing the selected flashlight must pay 1% of its purchase price while leaving the musket for combat.");
            }

            ClickButtonThroughUi(shopController.CloseButton);
            if (shopController.IsShopVisible || !playerInput.CursorLockSuppressed)
            {
                throw new InvalidOperationException("Phase 15 shop close button must hide the shop while maintenance keeps UI cursor mode active.");
            }

            ClickButtonThroughUi(maintenanceController.PersonalCargoButton);
            if (!personalCargoController.IsCargoVisible || !playerInput.CursorLockSuppressed)
            {
                throw new InvalidOperationException("After shop transactions, maintenance must still open the personal cargo screen.");
            }

            ClickButtonThroughUi(personalCargoController.CloseButton);
            if (!maintenanceController.IsMaintenanceVisible || personalCargoController.IsCargoVisible || !playerInput.CursorLockSuppressed)
            {
                throw new InvalidOperationException("Personal cargo back button must keep maintenance usable after shop transactions.");
            }

            ClickButtonThroughUi(maintenanceController.ContractBoardButton);
            ClickButtonThroughUi(contractBoardController.AssociationContractButton);
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                contractBoardController.SelectedContractId != "association-local-001")
            {
                throw new InvalidOperationException("Association category must select the follow-up contract before acceptance.");
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
                stickHit.Damage != 42 ||
                deviceState.CurrentSeedIntruder.Intruder.CurrentHealth != 13)
            {
                throw new InvalidOperationException("Strength-enhanced stick must damage the active Parvum by 42 damage.");
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
                throw new InvalidOperationException("Musket precision fire must keep base ranged damage and reload must remain a skeleton.");
            }

            return $"Wallet=51; Slots=3/3; StickHit={stickHit.Damage}; MusketHit={musketHit.Damage}; Reload={reload.Outcome}; Intruder={deviceState.CurrentSeedIntruder.Intruder.Resolution}; Shop=Step8ItemEffects";
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                var name = button == null ? "<null>" : button.name;
                var activeSelf = button != null && button.gameObject.activeSelf;
                var activeInHierarchy = button != null && button.gameObject.activeInHierarchy;
                var interactable = button != null && button.interactable;
                throw new InvalidOperationException(
                    "Cannot click an inactive or non-interactable Phase 15 button: " +
                    name +
                    "; activeSelf=" + activeSelf +
                    "; activeInHierarchy=" + activeInHierarchy +
                    "; interactable=" + interactable + ".");
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
