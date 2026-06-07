using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.Core.Coop;
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
    internal static class Phase18MvpPlaytestLoopPlayModeSmoke
    {
        private const string RequestFileName = "Phase18MvpPlaytestLoopSmoke.request";
        private const string ActiveFileName = "Phase18MvpPlaytestLoopSmoke.active";
        private const string ErrorsFileName = "Phase18MvpPlaytestLoopSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 45d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase18MvpPlaytestLoopPlayModeSmoke()
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
                throw new TimeoutException($"Phase 18 MVP playtest loop smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 18 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 18 smoke must start from Edit mode.");
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase16HudMapAtmosphereBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 18 smoke.");
            }

            Phase16HudMapAtmosphereBootstrap.EnsurePhase16Assets();
            EditorSceneManager.OpenScene(Phase16HudMapAtmosphereBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            Phase16HudMapAtmosphereEditorValidation.Run();
            Phase17CoopFoundationEditorValidation.Run();
            EditorSceneManager.playModeStartScene = sceneAsset;

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

            if (SceneManager.GetActiveScene().path != Phase16HudMapAtmosphereBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase16HudMapAtmosphereBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 18 smoke captured Unity errors."));
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
            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var map = UnityEngine.Object.FindFirstObjectByType<ShipInteriorMapHud>();
            var equipmentController = UnityEngine.Object.FindFirstObjectByType<PlayerEquipmentController>();
            if (startController == null ||
                playerInput == null ||
                deviceState == null ||
                settlementController == null ||
                maintenanceController == null ||
                contractBoardController == null ||
                hud == null ||
                map == null ||
                equipmentController == null)
            {
                throw new InvalidOperationException("Runtime scene must contain the MVP start, HUD, map, equipment, device, settlement, maintenance, and contract board controllers.");
            }

            AssertPhase16HudStillPresent(hud, map, equipmentController);
            AssertLocalCoopFoundationStillWorks();

            ClickButtonThroughUi(startController.YesButton, "association yes");
            if (startController.CurrentSession.Phase != GameSessionPhase.Ready ||
                !startController.CurrentSession.IsAssociationMember ||
                !startController.CurrentSession.Equipment.HasBasicProtectiveSuit ||
                startController.AvailableContractCount != 1)
            {
                throw new InvalidOperationException("MVP loop must reach association start with default loadout and one tutorial contract.");
            }

            ClickButtonThroughUi(startController.TutorialContractButton, "tutorial contract");
            var tutorialSession = startController.CurrentSession;
            if (tutorialSession.Phase != GameSessionPhase.Transporting ||
                !tutorialSession.ActiveTransportContract.HasValue ||
                !tutorialSession.ActiveTransportContract.Value.IsTutorial ||
                !deviceState.HasActiveTransportRun ||
                deviceState.CurrentTransportRun.BaseDurationSeconds != 60 ||
                TransportHazardRules.ShouldStartAsteroidField(tutorialSession) ||
                SeedIntruderRules.CanCheckSeedIntruder(tutorialSession))
            {
                throw new InvalidOperationException("Tutorial transport must start as a 60 second safe first run.");
            }

            var damagedShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(20, 100));
            var expectedRepairCost = ShipStateRules.CalculateRepairCost(damagedShip);
            deviceState.SetShipState(damagedShip);
            deviceState.TickTransportRun(60f);
            settlementController.ProcessTransportArrival();

            if (!settlementController.IsSettlementVisible ||
                settlementController.CurrentSession.Phase != GameSessionPhase.Completed ||
                settlementController.CurrentSession.CompletedTransportCount != 1 ||
                settlementController.CurrentSession.Wallet.Credits < 1100 ||
                settlementController.CurrentSession.SettlementResult.PendingRepairCost != expectedRepairCost ||
                !settlementController.ContinueToMaintenanceButton.interactable ||
                !playerInput.CursorLockSuppressed)
            {
                var session = settlementController.CurrentSession;
                throw new InvalidOperationException(
                    "Tutorial arrival must show settlement, pending repair, and maintenance continuation. " +
                    "Visible=" + settlementController.IsSettlementVisible +
                    "; Phase=" + (session == null ? "null" : session.Phase.ToString()) +
                    "; Count=" + (session == null ? -1 : session.CompletedTransportCount) +
                    "; Wallet=" + (session == null ? -1 : session.Wallet.Credits) +
                    "; PendingRepair=" + (session == null ? -1 : session.SettlementResult.PendingRepairCost) +
                    "; ExpectedRepair=" + expectedRepairCost +
                    "; Continue=" + (settlementController.ContinueToMaintenanceButton != null && settlementController.ContinueToMaintenanceButton.interactable) +
                    "; CursorSuppressed=" + playerInput.CursorLockSuppressed +
                    "; RunComplete=" + (deviceState.HasActiveTransportRun && deviceState.CurrentTransportRun.IsComplete));
            }

            ClickButtonThroughUi(settlementController.ContinueToMaintenanceButton, "maintenance continuation");
            var firstSettlementBalance = settlementController.CurrentSession.Wallet.Credits;
            if (!maintenanceController.IsMaintenanceVisible ||
                settlementController.IsSettlementVisible ||
                maintenanceController.ContractListText == null ||
                !maintenanceController.ContractListText.text.Contains("Contract Board") ||
                !maintenanceController.RepairButton.interactable ||
                !maintenanceController.ContractBoardButton.interactable ||
                contractBoardController.IsBoardVisible)
            {
                throw new InvalidOperationException("Maintenance must replace settlement and expose a separate repair-gated contract board.");
            }

            ClickButtonThroughUi(maintenanceController.ContractBoardButton, "contract board before repair");
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.IsMaintenanceVisible ||
                !contractBoardController.ContractListText.text.Contains("Association") ||
                !contractBoardController.ContractListText.text.Contains("Private") ||
                contractBoardController.ContractSlotButtons == null ||
                contractBoardController.ContractSlotButtons.Length == 0 ||
                !contractBoardController.ContractSlotButtons[0].interactable ||
                !contractBoardController.AssociationContractButton.interactable ||
                !contractBoardController.PrivateContractButton.interactable ||
                contractBoardController.AcceptContractButton.interactable ||
                contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Contract board must show selectable association/private rows while blocking acceptance before repair.");
            }

            ClickButtonThroughUi(contractBoardController.AssociationContractButton, "association select before repair");
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                contractBoardController.SelectedContractId != "association-local-001" ||
                contractBoardController.AcceptContractButton.interactable ||
                contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Association category must select a row without accepting before repair.");
            }

            ClickButtonThroughUi(contractBoardController.BackButton, "contract board back");
            ClickButtonThroughUi(maintenanceController.RepairButton, "repair");
            var repairedSession = maintenanceController.CurrentSession;
            if (repairedSession.Wallet.Credits != firstSettlementBalance - expectedRepairCost ||
                repairedSession.SettlementResult.PendingRepairCost != 0 ||
                repairedSession.Ship.GetRoom(ShipRoomId.CargoHold).CurrentDurability != 100 ||
                !maintenanceController.ContractBoardButton.interactable)
            {
                throw new InvalidOperationException("Repair must charge pending cost, restore ship state, and keep the contract board available.");
            }

            ClickButtonThroughUi(maintenanceController.ContractBoardButton, "contract board after repair");
            if (!contractBoardController.IsBoardVisible ||
                !contractBoardController.AssociationContractButton.interactable ||
                !contractBoardController.PrivateContractButton.interactable ||
                !contractBoardController.AcceptContractButton.interactable ||
                contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Contract board must unlock acceptance after repair while keeping Start Run disabled until a contract is accepted.");
            }

            ClickButtonThroughUi(contractBoardController.AssociationContractButton, "association follow-up");
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.CurrentSession.Phase != GameSessionPhase.Completed ||
                contractBoardController.SelectedContractId != "association-local-001")
            {
                throw new InvalidOperationException("Association category must select the follow-up without starting transport.");
            }

            ClickButtonThroughUi(contractBoardController.AcceptContractButton, "accept association follow-up");
            var acceptedFollowUp = maintenanceController.CurrentSession;
            if (!contractBoardController.IsBoardVisible ||
                maintenanceController.IsMaintenanceVisible ||
                acceptedFollowUp.Phase != GameSessionPhase.Completed ||
                acceptedFollowUp.PendingTransportContractCount != 1 ||
                !acceptedFollowUp.IsTransportContractPending("association-local-001") ||
                contractBoardController.AcceptContractButton.interactable ||
                !contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Accept must queue the association follow-up without starting transport.");
            }

            ClickButtonThroughUi(contractBoardController.StartRunButton, "start accepted follow-up");
            var followUpSession = maintenanceController.CurrentSession;
            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidFieldSmall(0, 12));
            if (maintenanceController.IsMaintenanceVisible ||
                contractBoardController.IsBoardVisible ||
                followUpSession.Phase != GameSessionPhase.Transporting ||
                !followUpSession.ActiveTransportContract.HasValue ||
                followUpSession.ActiveTransportContract.Value.Id != "association-local-001" ||
                followUpSession.PendingTransportContractCount != 0 ||
                !deviceState.HasActiveTransportRun ||
                !deviceState.HasActiveTransportHazard ||
                deviceState.CurrentExternalTarget.TargetType != ExternalTargetType.Asteroid)
            {
                throw new InvalidOperationException("Association follow-up must start transport and accept a validation asteroid hazard.");
            }

            deviceState.ActivateDevice(ShipDeviceType.CockpitHelm);
            deviceState.ApplyManualFlightInput(1f, 1f, 1f);
            var firstHazardDuration = deviceState.CurrentTransportHazard.DurationSeconds;
            deviceState.TickTransportRun(firstHazardDuration);
            if (deviceState.HasActiveTransportHazard ||
                deviceState.LastTransportHazardResult.Resolution != TransportHazardResolution.Avoided ||
                ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState) != 0)
            {
                throw new InvalidOperationException("Manual flight avoidance must clear the first post-tutorial hazard without damage.");
            }

            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(2718, 12));
            var turretTarget = deviceState.CurrentExternalTarget;
            deviceState.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
            deviceState.SetManualTurretAimForValidation(turretTarget.PositionX, turretTarget.PositionY);
            ManualTurretFireResult finalShot = default;
            for (var i = 0; i < 20 && deviceState.CurrentExternalTarget.IsActive; i++)
            {
                finalShot = deviceState.FireManualTurret();
            }

            if (finalShot.Outcome != ManualTurretFireOutcome.Destroyed ||
                deviceState.HasActiveTransportHazard ||
                deviceState.LastTransportHazardResult.Resolution != TransportHazardResolution.Neutralized)
            {
                throw new InvalidOperationException("Manual turret must destroy an external target and neutralize the hazard.");
            }

            var parvum = SeedIntruderRules.CreateParvumIntrusionForSeed(17, ShipRoomId.Cockpit, "phase18-parvum");
            deviceState.StartSeedIntruderForValidation(parvum);
            deviceState.SetEquipmentStateForValidation(
                PlayerEquipmentState.CreateDefaultAssociationIssue()
                    .WithHandSlot(1, EquipmentSlotState.One(EquipmentItemKind.Musket)));
            var stickHit = deviceState.UseActiveEquipment(false);
            deviceState.TickEquipmentState(EquipmentRules.StickUseDelaySeconds);
            deviceState.SelectEquipmentHandSlot(1);
            var musketHit = deviceState.UseActiveEquipment(false);
            if (stickHit.Outcome != EquipmentUseOutcome.MeleeHit ||
                musketHit.Outcome != EquipmentUseOutcome.RangedHit ||
                !deviceState.CurrentSeedIntruder.IsResolved)
            {
                throw new InvalidOperationException("Issued stick plus purchased musket must resolve the MVP Parvum intruder.");
            }

            deviceState.TickTransportRun(deviceState.CurrentTransportRun.RemainingSeconds);
            settlementController.ProcessTransportArrival();
            var secondSettlement = settlementController.CurrentSession;
            if (!settlementController.IsSettlementVisible ||
                secondSettlement.Phase != GameSessionPhase.Completed ||
                secondSettlement.CompletedTransportCount != 2 ||
                secondSettlement.SettlementResult.PendingRepairCost != 0 ||
                secondSettlement.Wallet.Credits <= repairedSession.Wallet.Credits)
            {
                throw new InvalidOperationException(
                    "Second transport must arrive cleanly after manual hazard and intruder responses. " +
                    "Visible=" + settlementController.IsSettlementVisible +
                    "; Phase=" + (secondSettlement == null ? "null" : secondSettlement.Phase.ToString()) +
                    "; Count=" + (secondSettlement == null ? -1 : secondSettlement.CompletedTransportCount) +
                    "; PendingRepair=" + (secondSettlement == null ? -1 : secondSettlement.SettlementResult.PendingRepairCost) +
                    "; Wallet=" + (secondSettlement == null ? -1 : secondSettlement.Wallet.Credits) +
                    "; PreviousWallet=" + repairedSession.Wallet.Credits +
                    "; Remaining=" + (deviceState.HasActiveTransportRun ? deviceState.CurrentTransportRun.RemainingSeconds : -1f) +
                    "; RunComplete=" + (deviceState.HasActiveTransportRun && deviceState.CurrentTransportRun.IsComplete) +
                    "; LastHazard=" + deviceState.LastTransportHazardResult.Resolution +
                    "; IntruderResolved=" + deviceState.CurrentSeedIntruder.IsResolved +
                    "; ArrivalGate=" + settlementController.ArrivalGateClosedForValidation +
                    "; ShownCount=" + settlementController.SettlementShownCompletedTransportCountForValidation +
                    "; Observed=" + settlementController.HasObservedPhaseForValidation +
                    "; LastObserved=" + settlementController.LastObservedPhaseForValidation);
            }

            ClickButtonThroughUi(settlementController.ContinueToMaintenanceButton, "second maintenance continuation");
            if (!maintenanceController.IsMaintenanceVisible ||
                !maintenanceController.ContractBoardButton.interactable ||
                contractBoardController.IsBoardVisible)
            {
                throw new InvalidOperationException("Second maintenance screen must be ready to reopen the separate contract board.");
            }

            ClickButtonThroughUi(maintenanceController.ContractBoardButton, "second contract board");
            if (!contractBoardController.IsBoardVisible ||
                !contractBoardController.AssociationContractButton.interactable ||
                !contractBoardController.PrivateContractButton.interactable ||
                !contractBoardController.AcceptContractButton.interactable ||
                contractBoardController.StartRunButton.interactable)
            {
                throw new InvalidOperationException("Second contract board must be ready for repeated playtest selection.");
            }

            return "Start=Association; Tutorial=Completed; Repair=" + expectedRepairCost +
                   "; FollowUp=association-local-001; Hazard=Avoided+Neutralized; Intruder=Neutralized; Completed=2; NextReady=True";
        }

        private static void AssertPhase16HudStillPresent(
            FirstPersonHud hud,
            ShipInteriorMapHud map,
            PlayerEquipmentController equipmentController)
        {
            if (hud.HealthText == null ||
                hud.ShieldText == null ||
                hud.HealthText.text != "100%" ||
                hud.ShieldText.text != "100%")
            {
                throw new InvalidOperationException("Phase 18 loop requires Phase 16 health and shield HUD to remain visible.");
            }

            map.RefreshForValidation();
            if (map.CurrentRoomText == null ||
                map.CurrentRoomMarker == null ||
                map.CurrentRoom != ShipRoomId.CargoHold)
            {
                throw new InvalidOperationException("Phase 18 loop requires Phase 16 ship map to remain active.");
            }

            if (equipmentController.PrecisionAimReticleText == null ||
                equipmentController.PrecisionAimReticleText.enabled)
            {
                throw new InvalidOperationException("Phase 18 loop requires default precision reticle to remain hidden.");
            }
        }

        private static void AssertLocalCoopFoundationStillWorks()
        {
            var authority = LocalCoopSessionAuthority.CreateLocalSimulation(
                GameSessionState.StartAssociationSession());
            var first = new CoopParticipantId("phase18-local-a");
            var second = new CoopParticipantId("phase18-local-b");
            authority.Join(first);
            authority.Join(second);
            authority.UpdatePlayerPose(new CoopPlayerPoseState(
                first,
                0f,
                0f,
                18f,
                15f,
                5f,
                ShipRoomId.Cockpit));
            authority.SubmitInteraction(CoopInteractionRequest.BeginDevice(first, ShipDeviceType.CockpitHelm));
            authority.SubmitInteraction(CoopInteractionRequest.StartTransportRun(first, 60));
            var snapshot = authority.CreateSnapshot(second);
            if (snapshot.ParticipantCount != CoopSessionLimits.LocalSimulationPlayerCount ||
                snapshot.Session.Phase != GameSessionPhase.Transporting ||
                !snapshot.TryGetPlayerPose(first, out var pose) ||
                pose.CurrentRoom != ShipRoomId.Cockpit)
            {
                throw new InvalidOperationException("Phase 18 loop requires the Phase 17 local coop snapshot boundary to remain valid.");
            }
        }

        private static void ClickButtonThroughUi(Button button, string label)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click inactive or non-interactable MVP loop button: " + label);
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("MVP loop UI click requires an active EventSystem.");
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
                        $"MVP loop button is not reachable by UI raycast: {label}; Position={position}; Hits={hitNames}");
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
            builder.AppendLine($"Phase 18 MVP playtest loop smoke completed: {request.Id}");
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
                    PlayFrameCount = int.TryParse(Get(values, "playFrameCount"), out var playFrameCount) ? playFrameCount : 0,
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
                        $"playFrameCount={PlayFrameCount}",
                        $"details={Details ?? string.Empty}"
                    });
            }

            private static string Get(IDictionary<string, string> values, string key)
            {
                return values.TryGetValue(key, out var value) ? value : string.Empty;
            }
        }
    }
}
