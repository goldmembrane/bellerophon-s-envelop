using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class Phase16HudMapAtmospherePlayModeSmoke
    {
        private const string RequestFileName = "Phase16HudMapAtmosphereSmoke.request";
        private const string ActiveFileName = "Phase16HudMapAtmosphereSmoke.active";
        private const string ErrorsFileName = "Phase16HudMapAtmosphereSmoke.errors";
        private const string CargoRunSceneName = "CargoRunMvp";
        private const double PollIntervalSeconds = 0.1d;
        private const double MaxRunSeconds = 35d;
        private const int RequiredPlayFrames = 2;

        private static double nextPollTime;

        static Phase16HudMapAtmospherePlayModeSmoke()
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
                throw new TimeoutException($"Phase 16 HUD map atmosphere smoke exceeded {MaxRunSeconds:0} seconds.");
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
                    throw new InvalidOperationException($"Unknown phase 16 smoke phase: {request.Phase}");
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
                throw new InvalidOperationException("Phase 16 smoke must start from Edit mode.");
            }

            Phase16HudMapAtmosphereBootstrap.EnsurePhase16Assets();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase16HudMapAtmosphereBootstrap.CargoRunScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
            Phase16HudMapAtmosphereEditorValidation.Run();

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
                WriteLog(request, true, new InvalidOperationException("Phase 16 smoke captured Unity errors."));
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

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var map = UnityEngine.Object.FindFirstObjectByType<ShipInteriorMapHud>();
            var atmosphere = UnityEngine.Object.FindFirstObjectByType<ShipInteriorAtmosphereController>();
            var audioHooks = UnityEngine.Object.FindFirstObjectByType<ShipSignalAudioHooks>();
            var equipmentController = UnityEngine.Object.FindFirstObjectByType<PlayerEquipmentController>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if (hud == null ||
                map == null ||
                atmosphere == null ||
                audioHooks == null ||
                equipmentController == null ||
                deviceState == null ||
                player == null)
            {
                throw new InvalidOperationException("Runtime scene must contain Phase 16 HUD, map, atmosphere, audio, equipment, device state, and player.");
            }

            AssertDefaultCrosshairHidden();
            if (hud.HealthFillImage == null ||
                hud.ShieldFillImage == null ||
                hud.HealthText == null ||
                hud.ShieldText == null ||
                hud.HealthText.text != "100%" ||
                hud.ShieldText.text != "100%")
            {
                throw new InvalidOperationException("Runtime Phase 16 vitals must show full health and shield percentages.");
            }

            if (!RenderSettings.fog ||
                RenderSettings.fogDensity < ShipInteriorAtmosphereController.TargetFogDensity * 0.9f ||
                atmosphere.TargetCamera == null ||
                atmosphere.TargetCamera.farClipPlane > ShipInteriorAtmosphereController.TargetCameraFarClip + 0.01f)
            {
                throw new InvalidOperationException("Runtime Phase 16 atmosphere must apply fog and limited visibility.");
            }

            map.RefreshForValidation();
            if (map.CurrentRoom != ShipRoomId.CargoHold ||
                !map.CurrentRoomText.text.Contains("Cargo Hold"))
            {
                throw new InvalidOperationException("Runtime Phase 16 map must start at Cargo Hold.");
            }

            player.transform.position = new Vector3(0f, 0f, 18f);
            Physics.SyncTransforms();
            map.RefreshForValidation();
            if (map.CurrentRoom != ShipRoomId.Cockpit ||
                !map.CurrentRoomText.text.Contains("Cockpit"))
            {
                throw new InvalidOperationException("Runtime Phase 16 map must update the current room from player position.");
            }

            audioHooks.TriggerShipDamageSignal();
            audioHooks.TriggerExternalDangerSignal();
            audioHooks.TriggerIntruderSignal();
            if (audioHooks.ShipDamageSignalCount < 1 ||
                audioHooks.ExternalDangerSignalCount < 1 ||
                audioHooks.IntruderSignalCount < 1 ||
                audioHooks.LastCue != ShipSignalAudioCue.IntruderSignal)
            {
                throw new InvalidOperationException("Runtime Phase 16 audio hooks must count all signal triggers.");
            }

            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithHandSlot(1, EquipmentSlotState.One(EquipmentItemKind.Musket))
                .WithActiveHandSlot(1);
            deviceState.SetEquipmentStateForValidation(equipment);
            equipmentController.RefreshHudForValidation();
            equipmentController.ToggleAlternateModeForValidation();
            if (!equipmentController.AlternateModeActive ||
                deviceState.CurrentEquipmentState.ActiveMode != EquipmentUseMode.PrecisionAim ||
                equipmentController.PrecisionAimReticleText == null ||
                !equipmentController.PrecisionAimReticleText.enabled)
            {
                throw new InvalidOperationException("Runtime right-click alternate mode must toggle musket precision aim on.");
            }

            equipmentController.ToggleAlternateModeForValidation();
            if (equipmentController.AlternateModeActive ||
                deviceState.CurrentEquipmentState.ActiveMode != EquipmentUseMode.Primary ||
                equipmentController.PrecisionAimReticleText.enabled)
            {
                throw new InvalidOperationException("Runtime right-click alternate mode must toggle musket precision aim off.");
            }

            deviceState.SetEquipmentStateForValidation(
                deviceState.CurrentEquipmentState.WithActiveHandSlot(0));
            equipmentController.RefreshHudForValidation();
            equipmentController.ToggleAlternateModeForValidation();
            if (!equipmentController.AlternateModeActive ||
                deviceState.CurrentEquipmentState.ActiveMode != EquipmentUseMode.Throwing ||
                equipmentController.PrecisionAimReticleText.enabled)
            {
                throw new InvalidOperationException("Runtime right-click alternate mode must toggle stick throwing mode without showing the precision reticle.");
            }

            return "Vitals=100/100; Map=Cockpit after move; Crosshair=Hidden; Fog=On; AudioSignals=3; RightClick=Toggle";
        }

        private static void AssertDefaultCrosshairHidden()
        {
            var labels = UnityEngine.Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].name != "Crosshair Text")
                {
                    continue;
                }

                if (labels[i].enabled || !string.IsNullOrEmpty(labels[i].text))
                {
                    throw new InvalidOperationException("Runtime default center crosshair must be hidden.");
                }
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
            builder.AppendLine($"Phase 16 HUD map atmosphere smoke completed: {request.Id}");
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
