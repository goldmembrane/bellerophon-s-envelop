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
    internal static class Phase12ManualTurretPlayModeSmoke
    {
        private const string RequestFileName = "Phase12ManualTurretSmoke.request";
        private const string ActiveFileName = "Phase12ManualTurretSmoke.active";
        private const string ErrorsFileName = "Phase12ManualTurretSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase12ManualTurretPlayModeSmoke()
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
                throw new TimeoutException($"Phase 12 manual turret smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 12 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 12 smoke must start from Edit mode.");
            }

            Phase12ManualTurretBootstrap.EnsurePhase12Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase12ManualTurretBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase12ManualTurretEditorValidation.Run();

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

            if (SceneManager.GetActiveScene().path != Phase12ManualTurretBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase12ManualTurretBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 12 smoke captured Unity errors."));
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
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            var contractBoardController = UnityEngine.Object.FindFirstObjectByType<ContractBoardController>();
            var turretView = UnityEngine.Object.FindFirstObjectByType<ManualTurretView>();
            if (startController == null ||
                playerInput == null ||
                deviceState == null ||
                deviceHud == null ||
                settlementController == null ||
                maintenanceController == null ||
                contractBoardController == null ||
                turretView == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 12 start, player, device, HUD, settlement, maintenance, contract board, and turret view controllers.");
            }

            ClickButtonThroughUi(startController.YesButton);
            ClickButtonThroughUi(startController.TutorialContractButton);
            deviceState.TickTransportRun(60f);
            settlementController.ProcessTransportArrival();
            settlementController.ContinueToMaintenance();
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
            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidFieldSmall(0, 10));

            if (!deviceState.HasActiveTransportHazard ||
                !deviceState.CurrentExternalTarget.IsActive ||
                deviceState.CurrentExternalTarget.TargetType != ExternalTargetType.Asteroid)
            {
                throw new InvalidOperationException("Follow-up transport must start an asteroid hazard with an external turret target.");
            }

            deviceState.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
            turretView.RefreshView();
            Canvas.ForceUpdateCanvases();
            if (!turretView.IsViewActive ||
                !playerInput.GameplayInputSuppressed ||
                deviceState.ActivePanelMode != ShipDevicePanelMode.TurretManual ||
                !deviceState.CurrentManualTurret.IntruderHitPossible)
            {
                throw new InvalidOperationException("Manual turret must open as an input-suppressing full-screen mode.");
            }

            var turretCanvas = turretView.ViewRoot.transform.parent.GetComponent<Canvas>();
            var turretBackground = turretView.ViewRoot.GetComponent<Image>();
            var turretRect = turretView.ViewRoot.GetComponent<RectTransform>();
            var legacyBackdrop = turretView.ViewRoot.transform.Find(Phase12ManualTurretBootstrap.ManualTurretBackdropName);
            if (turretRect == null ||
                turretRect.rect.width < 1000f ||
                turretRect.rect.height < 600f ||
                turretCanvas == null ||
                !turretCanvas.overrideSorting ||
                turretCanvas.sortingOrder < 30 ||
                turretBackground == null ||
                turretBackground.color.a < 1f)
            {
                var canvasState = turretCanvas == null
                    ? "null"
                    : $"override={turretCanvas.overrideSorting}, order={turretCanvas.sortingOrder}";
                var alphaState = turretBackground == null ? "null" : turretBackground.color.a.ToString("0.00");
                var rectState = turretRect == null ? "null" : $"{turretRect.rect.width:0}x{turretRect.rect.height:0}";
                throw new InvalidOperationException(
                    $"Manual turret view must cover the full screen opaquely at runtime. Rect={rectState}; Canvas={canvasState}; Alpha={alphaState}");
            }

            if (legacyBackdrop != null)
            {
                throw new InvalidOperationException("Manual turret view must not render the legacy center backdrop panel.");
            }

            var target = deviceState.CurrentExternalTarget;
            deviceState.SetManualTurretAimForValidation(target.PositionX, target.PositionY);
            var firstShot = turretView.ProcessHeldFireForValidation(0f, true, true);
            var blockedHeldShot = turretView.ProcessHeldFireForValidation(
                ManualTurretState.HeldFireIntervalSeconds * 0.5f,
                true,
                false);
            if (firstShot.Outcome != ManualTurretFireOutcome.Hit ||
                blockedHeldShot.Outcome != ManualTurretFireOutcome.None ||
                firstShot.Turret.AmmoInMagazine != ManualTurretState.MagazineSize - 1 ||
                firstShot.Target.CurrentHealth != target.MaxHealth - ManualTurretState.ShotDamage)
            {
                throw new InvalidOperationException("Manual turret first held shot must hit, consume ammo, and respect the repeat-fire interval.");
            }

            deviceState.BeginManualTurretReload();
            deviceState.TickTransportRun(1f);
            if (!deviceState.CurrentManualTurret.IsReloading)
            {
                throw new InvalidOperationException("Manual turret reload must remain active before two seconds.");
            }

            deviceState.TickTransportRun(1f);
            if (deviceState.CurrentManualTurret.IsReloading ||
                deviceState.CurrentManualTurret.AmmoInMagazine != ManualTurretState.MagazineSize)
            {
                throw new InvalidOperationException("Manual turret reload must restore a full magazine after two seconds.");
            }

            var ammoAfterReload = deviceState.CurrentManualTurret.AmmoInMagazine;
            target = deviceState.CurrentExternalTarget;
            deviceState.SetManualTurretAimForValidation(target.PositionX, target.PositionY);
            var repeatedShot = turretView.ProcessHeldFireForValidation(0f, true, true);
            var destroyedShot = turretView.ProcessHeldFireForValidation(
                ManualTurretState.HeldFireIntervalSeconds,
                true,
                false);
            while (deviceState.CurrentExternalTarget.IsActive)
            {
                destroyedShot = deviceState.FireManualTurret();
            }

            deviceHud.RefreshTransportStatus();
            if (repeatedShot.Outcome != ManualTurretFireOutcome.Hit ||
                destroyedShot.Outcome != ManualTurretFireOutcome.Destroyed ||
                deviceState.HasActiveTransportHazard ||
                deviceState.CurrentExternalTarget.IsActive ||
                deviceState.LastTransportHazardResult.Resolution != TransportHazardResolution.Neutralized ||
                ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState) != 0 ||
                deviceHud.TransportStatusText == null ||
                !deviceHud.TransportStatusText.text.Contains("Neutralized"))
            {
                throw new InvalidOperationException("Destroying the external asteroid target must neutralize the hazard without ship repair cost.");
            }

            deviceState.SetShipState(ShipState.CreateDefault());
            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(5567, 10));
            deviceState.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
            deviceState.TickTransportRun(10f);
            var failureResult = deviceState.LastTransportHazardResult;
            var failureRepairCost = ShipStateRules.CalculateRepairCost(deviceState.CurrentShipState);
            if (failureResult.Resolution != TransportHazardResolution.DirectHit ||
                failureRepairCost <= 0)
            {
                throw new InvalidOperationException(
                    $"Ignoring the external target in turret mode must leave the asteroid hazard damaging the ship. Result={failureResult.Resolution}; Repair={failureRepairCost}");
            }

            var weaponUpgrades = ShipUpgradeState.Empty
                .WithPurchasedTier(ShipUpgradeCategory.WeaponSystems, 2)
                .WithEquippedTier(ShipUpgradeCategory.WeaponSystems, 2);
            deviceState.SetShipState(ShipState.CreateDefault());
            deviceState.SetShipUpgradeStateForValidation(weaponUpgrades);
            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(7712, 10));
            deviceState.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
            target = deviceState.CurrentExternalTarget;
            deviceState.SetManualTurretAimForValidation(target.PositionX, target.PositionY);
            var plasma = deviceState.FireManualTurretPlasma();
            deviceState.TickTransportRun(0.9f);
            if (deviceState.CurrentManualTurret.MagazineCapacity != 75 ||
                plasma.Outcome != ManualTurretPlasmaOutcome.Activated ||
                deviceState.HasActiveTransportHazard ||
                deviceState.LastTransportHazardResult.Resolution != TransportHazardResolution.Neutralized)
            {
                throw new InvalidOperationException("Weapon Systems tier 2 must upgrade magazine capacity and let right-click plasma neutralize an external target.");
            }

            deviceState.TickTransportRun(ManualTurretState.PlasmaCannonDurationSeconds);
            deviceState.ExitManualTurretMode();
            deviceState.SetShipState(ShipState.CreateDefault());
            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(8831, 12));
            deviceState.ActivateDevice(ShipDeviceType.CockpitHelm);
            var boosted = deviceState.UseManualFlightBooster();
            deviceState.TickTransportRun(2f);
            if (!boosted ||
                deviceState.HasActiveTransportHazard ||
                deviceState.LastTransportHazardResult.Resolution != TransportHazardResolution.Avoided)
            {
                throw new InvalidOperationException(
                    "Manual flight booster must reduce asteroid hazard time and resolve the hazard through the manual avoidance path. " +
                    $"Boosted={boosted}; Mode={deviceState.CurrentFlightMode}; ActiveHazard={deviceState.HasActiveTransportHazard}; " +
                    $"Result={deviceState.LastTransportHazardResult.Resolution}; Summary={deviceState.LastInteractionSummary}");
            }

            deviceState.SetShipState(ShipState.CreateDefault());
            deviceState.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(8832, 12));
            if (deviceState.CurrentFlightMode != ShipFlightMode.ManualFlight)
            {
                deviceState.ActivateDevice(ShipDeviceType.CockpitHelm);
            }

            deviceState.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
            if (deviceState.CurrentWeaponOperationMode != ShipWeaponOperationMode.AutoTurret ||
                deviceState.TurretManualModeActive)
            {
                throw new InvalidOperationException("Manual flight must force the weapon room into auto turret mode.");
            }

            return $"First={firstShot.Outcome}; HoldBlocked={blockedHeldShot.Outcome}; HeldRepeat={destroyedShot.Outcome}; Delay={ManualTurretState.HeldFireIntervalSeconds:0.00}; AmmoAfterReload={ammoAfterReload}; Success={TransportHazardResolution.Neutralized}; Failure={failureResult.Resolution}; RepairCost={failureRepairCost}; Plasma={plasma.Outcome}; Booster={TransportHazardResolution.Avoided}";
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 12 button.");
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 12 UI click requires an active EventSystem.");
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
                        $"Phase 12 button is not reachable by UI raycast: {button.name}; Position={position}; Hits={hitNames}");
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
            builder.AppendLine($"Phase 12 manual turret smoke completed: {request.Id}");
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
