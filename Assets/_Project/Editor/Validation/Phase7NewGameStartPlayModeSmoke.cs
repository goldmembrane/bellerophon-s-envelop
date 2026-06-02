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
    internal static class Phase7NewGameStartPlayModeSmoke
    {
        private const string RequestFileName = "Phase7NewGameStartSmoke.request";
        private const string ActiveFileName = "Phase7NewGameStartSmoke.active";
        private const string ErrorsFileName = "Phase7NewGameStartSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 30d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase7NewGameStartPlayModeSmoke()
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
                throw new TimeoutException($"Phase 7 new game start smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 7 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 7 smoke must start from Edit mode.");
            }

            Phase7NewGameStartBootstrap.EnsurePhase7Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase7NewGameStartBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase7NewGameStartEditorValidation.Run();

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

            if (SceneManager.GetActiveScene().path != Phase7NewGameStartBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase7NewGameStartBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (File.Exists(ErrorsPath))
            {
                WriteLog(request, true, new InvalidOperationException("Phase 7 smoke captured Unity errors."));
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

            var controller = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if (controller == null || playerInput == null || deviceState == null)
            {
                throw new InvalidOperationException("Runtime scene must contain the Phase 7 start controller, player input, and ship device state.");
            }

            if (controller.FlowState.Phase != NewGameStartFlowPhase.ContractPrompt ||
                controller.YesButton == null ||
                controller.TutorialContractButton == null ||
                !controller.YesButton.gameObject.activeInHierarchy ||
                !controller.TutorialContractButton.gameObject.activeInHierarchy ||
                !controller.YesButton.interactable ||
                controller.TutorialContractButton.interactable)
            {
                throw new InvalidOperationException("Phase 7 initial contract UI state is invalid.");
            }

            if (!playerInput.CursorLockSuppressed || Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                throw new InvalidOperationException(
                    $"Phase 7 contract UI must unlock the cursor. Suppressed={playerInput.CursorLockSuppressed}; Lock={Cursor.lockState}; Visible={Cursor.visible}");
            }

            ClickButtonThroughUi(controller.YesButton);
            var planetState = controller.FlowState;
            if (planetState.Phase != NewGameStartFlowPhase.AssociationPlanet)
            {
                throw new InvalidOperationException("Association Yes button did not advance through the UI click path.");
            }

            var session = planetState.Session;
            if (!session.IsAssociationMember ||
                !session.CurrentPlanet.HasAssociationLogoSign ||
                session.Wallet.Credits != 0 ||
                !session.StartingLoadout.HasDefaultCargoShip ||
                !session.StartingLoadout.HasBasicProtectiveSuit ||
                session.StartingLoadout.StickCount != 1)
            {
                throw new InvalidOperationException("Association planet start session state is invalid.");
            }

            if (planetState.AvailableContractCount != 1)
            {
                throw new InvalidOperationException($"Only the tutorial contract may be available. Count={planetState.AvailableContractCount}");
            }

            var tutorial = planetState.GetAvailableContract(0);
            if (!tutorial.IsTutorial ||
                tutorial.DurationSeconds != 60 ||
                tutorial.ContractType != ContractType.Association ||
                tutorial.Difficulty != ContractDifficulty.Intro)
            {
                throw new InvalidOperationException("Tutorial contract definition does not match the Phase 7 MVP scope.");
            }

            if (!playerInput.CursorLockSuppressed || Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                throw new InvalidOperationException("Tutorial contract UI must keep the cursor unlocked.");
            }

            ClickButtonThroughUi(controller.TutorialContractButton);
            var accepted = controller.FlowState;
            if (accepted.Phase != NewGameStartFlowPhase.TutorialContractAccepted)
            {
                throw new InvalidOperationException("Tutorial contract button did not accept the tutorial contract.");
            }

            session = accepted.Session;
            if (session.Phase != GameSessionPhase.Transporting ||
                session.Ship.RunState != ShipRunState.InTransit ||
                !session.ActiveTransportContract.HasValue ||
                !session.ActiveCargo.HasValue)
            {
                throw new InvalidOperationException("Accepted tutorial contract did not start active transport state.");
            }

            var activeContract = session.ActiveTransportContract.Value;
            var activeCargo = session.ActiveCargo.Value;
            if (activeContract.DurationSeconds != 60 ||
                activeContract.TransportTargetName != "Cargo Hold Center Cargo" ||
                activeCargo.DurabilityPercent < 0.999f ||
                deviceState.CurrentCargoState.DurabilityPercent < 0.999f)
            {
                throw new InvalidOperationException("Active tutorial cargo was not registered on the session and ship device state.");
            }

            if (playerInput.CursorLockSuppressed || Cursor.lockState != CursorLockMode.Locked)
            {
                throw new InvalidOperationException(
                    $"Tutorial acceptance should restore first-person cursor lock. Suppressed={playerInput.CursorLockSuppressed}; Lock={Cursor.lockState}");
            }

            if (controller.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Tutorial acceptance should close the Phase 7 start UI.");
            }

            return $"Phase={accepted.Phase}; Session={session.Phase}; Duration={activeContract.DurationSeconds}; Cargo={activeContract.TransportTargetName}; Credits={session.Wallet.Credits}; StickCount={session.StartingLoadout.StickCount}";
        }

        private static void ClickButtonThroughUi(Button button)
        {
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                throw new InvalidOperationException("Cannot click an inactive or non-interactable Phase 7 button.");
            }

            if (EventSystem.current == null)
            {
                throw new InvalidOperationException("Phase 7 UI click requires an active EventSystem.");
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
                    $"Phase 7 button is not reachable by UI raycast: {button.name}; Position={position}; Hits={hitNames}");
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
            builder.AppendLine($"Phase 7 new game start smoke completed: {request.Id}");
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
