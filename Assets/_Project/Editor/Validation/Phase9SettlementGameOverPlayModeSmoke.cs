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
    internal static class Phase9SettlementGameOverPlayModeSmoke
    {
        private const string RequestFileName = "Phase9SettlementGameOverSmoke.request";
        private const string ActiveFileName = "Phase9SettlementGameOverSmoke.active";
        private const string ErrorsFileName = "Phase9SettlementGameOverSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase9SettlementGameOverPlayModeSmoke()
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
                throw new TimeoutException($"Phase 9 settlement game over smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 9 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 9 smoke must start from Edit mode.");
            }

            Phase9SettlementGameOverBootstrap.EnsurePhase9Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase9SettlementGameOverBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase9SettlementGameOverEditorValidation.Run();

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

            if (SceneManager.GetActiveScene().path != Phase9SettlementGameOverBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase9SettlementGameOverBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 9 smoke captured Unity errors."));
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
            if (startController == null || playerInput == null || deviceState == null || settlementController == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 9 start flow, player input, device state, and settlement controller.");
            }

            ClickButtonThroughUi(startController.YesButton);
            ClickButtonThroughUi(startController.TutorialContractButton);
            if (!deviceState.HasActiveTransportRun)
            {
                throw new InvalidOperationException("Tutorial transport must start before Phase 9 settlement smoke.");
            }

            var tutorialContract = startController.CurrentSession.ActiveTransportContract.Value;
            var damagedShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(70, 100));
            var expectedPendingRepairCost = ShipStateRules.CalculateRepairCost(damagedShip);
            var expectedInsurancePayout = ShipStateRules.CalculateShipLossInsurancePayout(damagedShip);
            var expectedGrossRevenue = tutorialContract.RewardCredits + 100 + expectedInsurancePayout;
            deviceState.SetShipState(damagedShip);
            deviceState.TickTransportRun(60f);
            settlementController.ProcessTransportArrival();

            var firstSettlement = settlementController.CurrentSession;
            if (firstSettlement.Phase != GameSessionPhase.Completed ||
                firstSettlement.Wallet.Credits != expectedGrossRevenue ||
                firstSettlement.Wallet.HasUnpaidDebtGrace ||
                firstSettlement.SettlementResult.GrossRevenue != expectedGrossRevenue ||
                firstSettlement.SettlementResult.Expenses != 0 ||
                firstSettlement.SettlementResult.PendingRepairCost != expectedPendingRepairCost ||
                firstSettlement.SettlementResult.DebtStatus != SettlementDebtStatus.Clear ||
                !settlementController.IsSettlementVisible ||
                settlementController.IsGameOverVisible)
            {
                throw new InvalidOperationException(
                    $"Arrival settlement must keep repair cost pending. Phase={firstSettlement.Phase}; Balance={firstSettlement.Wallet.Credits}; Expenses={firstSettlement.SettlementResult.Expenses}; PendingRepair={firstSettlement.SettlementResult.PendingRepairCost}");
            }

            if (settlementController.SettlementBodyText == null ||
                !settlementController.SettlementBodyText.text.Contains("Contract reward") ||
                !settlementController.SettlementBodyText.text.Contains("+$" + tutorialContract.RewardCredits) ||
                !settlementController.SettlementBodyText.text.Contains("Association support bonus") ||
                !settlementController.SettlementBodyText.text.Contains("+$100") ||
                !settlementController.SettlementBodyText.text.Contains("Ship repair cost") ||
                !settlementController.SettlementBodyText.text.Contains("Repair charge due at maintenance") ||
                !settlementController.SettlementBodyText.text.Contains("charged at maintenance") ||
                !settlementController.SettlementBodyText.text.Contains("Final balance"))
            {
                throw new InvalidOperationException("Settlement UI must display itemized calculation lines.");
            }

            if (!firstSettlement.ActiveTransportContract.HasValue)
            {
                throw new InvalidOperationException("Debt grace settlement must keep the active contract available for the next run setup.");
            }

            var nextRun = firstSettlement.StartTransport(firstSettlement.ActiveTransportContract.Value);
            startController.ApplySessionState(nextRun);
            settlementController.ResetArrivalGateForValidation();
            var firstDebtInput = CreateDebtSettlementInput(
                nextRun,
                CalculateCargoPenaltyForTargetBalance(nextRun, -300));
            settlementController.CompleteCurrentTransportForValidation(firstDebtInput);

            var debtSettlement = settlementController.CurrentSession;
            if (debtSettlement.Phase != GameSessionPhase.Completed ||
                debtSettlement.Wallet.Credits != -300 ||
                !debtSettlement.Wallet.HasUnpaidDebtGrace ||
                debtSettlement.SettlementResult.DebtStatus != SettlementDebtStatus.GraceActive)
            {
                throw new InvalidOperationException(
                    $"First non-repair debt settlement must show debt grace. Phase={debtSettlement.Phase}; Balance={debtSettlement.Wallet.Credits}; Debt={debtSettlement.SettlementResult.DebtStatus}");
            }

            var finalRun = debtSettlement.StartTransport(debtSettlement.ActiveTransportContract.Value);
            startController.ApplySessionState(finalRun);
            settlementController.ResetArrivalGateForValidation();
            var finalDebtInput = CreateDebtSettlementInput(finalRun, 2500);
            settlementController.CompleteCurrentTransportForValidation(finalDebtInput);

            var gameOver = settlementController.CurrentSession;
            if (gameOver.Phase != GameSessionPhase.GameOver ||
                !gameOver.SettlementResult.IsGameOver ||
                gameOver.SettlementResult.DebtStatus != SettlementDebtStatus.FinalGameOver ||
                !settlementController.IsGameOverVisible ||
                !playerInput.GameplayInputSuppressed)
            {
                throw new InvalidOperationException(
                    $"Second negative settlement must enter game over. Phase={gameOver.Phase}; Balance={gameOver.Wallet.Credits}; Debt={gameOver.SettlementResult.DebtStatus}");
            }

            Canvas.ForceUpdateCanvases();
            var gameOverRect = settlementController.GameOverRoot.GetComponent<RectTransform>();
            if (gameOverRect == null || gameOverRect.rect.width < 1000f || gameOverRect.rect.height < 600f)
            {
                throw new InvalidOperationException("Game over cutscene must cover the full screen.");
            }

            var podStart = settlementController.PodVisual.anchoredPosition;
            settlementController.AdvanceGameOverCutsceneForValidation(3f);
            var podEnd = settlementController.PodVisual.anchoredPosition;
            if (podEnd.x <= podStart.x ||
                podEnd.y >= podStart.y ||
                !settlementController.IsGameOverCutsceneComplete ||
                settlementController.GameOverTitleText.text != "GAME OVER")
            {
                throw new InvalidOperationException("Game over cutscene must show the pod being discarded before the final game over title.");
            }

            return $"ArrivalBalance={firstSettlement.Wallet.Credits}; PendingRepair={firstSettlement.SettlementResult.PendingRepairCost}; FirstDebt={debtSettlement.Wallet.Credits}; FinalBalance={gameOver.Wallet.Credits}; Pod={podStart.x:0},{podStart.y:0}->{podEnd.x:0},{podEnd.y:0}; Suppressed={playerInput.GameplayInputSuppressed}";
        }

        private static int CalculateCargoPenaltyForTargetBalance(GameSessionState session, int targetFinalBalance)
        {
            var contract = session.ActiveTransportContract.Value;
            return session.Wallet.Credits + contract.RewardCredits + 100 - targetFinalBalance;
        }

        private static SettlementInput CreateDebtSettlementInput(GameSessionState session, int cargoLossPenalty)
        {
            var contract = session.ActiveTransportContract.Value;
            return new SettlementInput(
                contract.ContractType,
                contract.Difficulty,
                contract.Cargo,
                ShipState.CreateDefault(),
                new CrewState(1, 0),
                session.Wallet,
                towingCost: 2000,
                revivalCostPerDeadCrew: 300,
                contractBasePay: contract.RewardCredits,
                repairSupportAmount: 100,
                cargoLossPenalty: cargoLossPenalty);
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 9 button.");
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 9 UI click requires an active EventSystem.");
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
                    $"Phase 9 button is not reachable by UI raycast: {button.name}; Position={position}; Hits={hitNames}");
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
            builder.AppendLine($"Phase 9 settlement game over smoke completed: {request.Id}");
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
