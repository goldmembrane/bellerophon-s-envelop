using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class CodexUnityBridge
    {
        private const string RequestFileName = "UnityEditorBridge.request";
        private static readonly object ClaimLock = new object();
        private static readonly HashSet<string> RecoveredRequestIds =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly ConcurrentQueue<ClaimedRequest> Pending =
            new ConcurrentQueue<ClaimedRequest>();
        private static readonly ConcurrentDictionary<string, DateTime>
            RecoveryCandidates = new ConcurrentDictionary<string, DateTime>(
                StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Commands =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "InspectDetectorAttachedStaticPresenceDetectorSources",
                "ApplyDetectorAttachedStaticPresenceDetector",
                "InspectDetectorAttachedStaticPresenceDetector",
                "CaptureDetectorAttachedStaticPresenceDetectorFinal",
                "InspectDetectorAttachedStaticLocomotionSources",
                "ApplyDetectorAttachedStaticLocomotion",
                "InspectDetectorAttachedStaticLocomotion",
                "CaptureDetectorAttachedStaticLocomotionFinal",
                "InspectElectricMineSetupSources",
                "ApplyElectricMineSetup",
                "InspectElectricMineSetup",
                "CaptureElectricMineSetupFinal",
                "ApplyElectricMineManualStateReplication",
                "InspectElectricMineManualStateReplication",
                "CaptureElectricMineManualStateFinal",
                "InspectElectricMineIdleLocomotionSources",
                "ApplyElectricMineIdleLocomotion",
                "InspectElectricMineIdleLocomotion",
                "CaptureElectricMineIdleLocomotionFinal",
                "InspectElectricMineActivateSources",
                "ApplyElectricMineActivateAnimation",
                "InspectElectricMineActivateAnimation",
                "InspectElectricMineActivateAnimationContinuous",
                "CaptureElectricMineActivateAnimationFinal"
            };

        private static readonly string RequestPath;
        private static readonly FileSystemWatcher Watcher;
        private static readonly FileSystemWatcher RecoveryWatcher;
        private static double nextRecoveryScan;

        static CodexUnityBridge()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            string logsPath = Path.Combine(projectRoot, "Logs");
            Directory.CreateDirectory(logsPath);
            RequestPath = Path.Combine(logsPath, RequestFileName);
            Watcher = new FileSystemWatcher(logsPath, RequestFileName)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                               NotifyFilters.CreationTime | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            Watcher.Created += OnRequestChanged;
            Watcher.Changed += OnRequestChanged;
            Watcher.Renamed += OnRequestChanged;
            RecoveryWatcher = new FileSystemWatcher(logsPath, "*.log")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                               NotifyFilters.CreationTime | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            RecoveryWatcher.Created += OnBridgeLogChanged;
            RecoveryWatcher.Changed += OnBridgeLogChanged;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            RecoverClaimedRequests(logsPath);
            TryClaimRequest();
        }

        private static void RecoverClaimedRequests(string logsPath)
        {
            foreach (string claimedPath in Directory.GetFiles(
                         logsPath, RequestFileName + ".detector.*"))
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(claimedPath, Encoding.UTF8);
                }
                catch (IOException)
                {
                    continue;
                }
                var values = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (string line in lines)
                {
                    int equals = line.IndexOf('=');
                    if (equals <= 0) continue;
                    values[line.Substring(0, equals)] = line.Substring(equals + 1);
                }
                if (!values.TryGetValue("command", out string command) ||
                    !Commands.Contains(command) ||
                    !values.TryGetValue("id", out string id) ||
                    string.IsNullOrWhiteSpace(id) ||
                    !values.TryGetValue("logPath", out string logPath) ||
                    string.IsNullOrWhiteSpace(logPath))
                    continue;
                Pending.Enqueue(new ClaimedRequest(id, command, logPath, claimedPath));
            }
        }

        private static void OnRequestChanged(object sender, FileSystemEventArgs args)
        {
            TryClaimRequest();
        }

        private static void OnBridgeLogChanged(object sender, FileSystemEventArgs args)
        {
            RecoveryCandidates[args.FullPath] = DateTime.UtcNow.AddSeconds(5d);
            if (TryRecoverUnknownCommand(args.FullPath))
                RecoveryCandidates.TryRemove(args.FullPath, out _);
        }

        private static bool TryRecoverUnknownCommand(string logPath)
        {
            string text;
            try
            {
                if (!File.Exists(logPath)) return false;
                text = File.ReadAllText(logPath, Encoding.UTF8);
            }
            catch (IOException)
            {
                return false;
            }
            const string completedPrefix = "Unity editor bridge request completed: ";
            const string commandPrefix = "Unity editor bridge command: ";
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string completed = lines.FirstOrDefault(line =>
                line.StartsWith(completedPrefix, StringComparison.Ordinal));
            string commandLine = lines.FirstOrDefault(line =>
                line.StartsWith(commandPrefix, StringComparison.Ordinal));
            if (completed == null || commandLine == null) return false;
            string id = completed.Substring(completedPrefix.Length).Trim();
            string command = commandLine.Substring(commandPrefix.Length).Trim();
            if (!Commands.Contains(command) ||
                text.IndexOf("Unknown bridge command: " + command,
                    StringComparison.Ordinal) < 0)
                return false;
            lock (ClaimLock)
            {
                if (!RecoveredRequestIds.Add(id)) return false;
                Pending.Enqueue(new ClaimedRequest(id, command, logPath, string.Empty));
            }
            return true;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup >= nextRecoveryScan)
            {
                nextRecoveryScan = EditorApplication.timeSinceStartup + 0.5d;
                RecoverPendingUnknownCommandLogs();
            }
            TryClaimRequest();
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            while (Pending.TryDequeue(out ClaimedRequest request))
                Run(request);
        }

        private static void RecoverPendingUnknownCommandLogs()
        {
            DateTime now = DateTime.UtcNow;
            foreach (KeyValuePair<string, DateTime> candidate in RecoveryCandidates)
            {
                if (candidate.Value < now || TryRecoverUnknownCommand(candidate.Key))
                    RecoveryCandidates.TryRemove(candidate.Key, out _);
            }
        }

        private static void TryClaimRequest()
        {
            lock (ClaimLock)
            {
                if (!File.Exists(RequestPath)) return;
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(RequestPath, Encoding.UTF8);
                }
                catch (IOException)
                {
                    return;
                }

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string line in lines)
                {
                    int equals = line.IndexOf('=');
                    if (equals <= 0) continue;
                    values[line.Substring(0, equals)] = line.Substring(equals + 1);
                }
                if (!values.TryGetValue("command", out string command) ||
                    !Commands.Contains(command) ||
                    !values.TryGetValue("id", out string id) ||
                    string.IsNullOrWhiteSpace(id) ||
                    !values.TryGetValue("logPath", out string logPath) ||
                    string.IsNullOrWhiteSpace(logPath))
                    return;

                string claimedPath = RequestPath + ".detector." + id;
                try
                {
                    File.Move(RequestPath, claimedPath);
                }
                catch (IOException)
                {
                    return;
                }
                Pending.Enqueue(new ClaimedRequest(id, command, logPath, claimedPath));
            }
        }

        private static void Run(ClaimedRequest request)
        {
            bool deferred = false;
            try
            {
                switch (request.Command)
                {
                    case "InspectDetectorAttachedStaticPresenceDetectorSources":
                        DetectorAttachedStaticPresenceDetectorSetupTools.InspectSources();
                        break;
                    case "ApplyDetectorAttachedStaticPresenceDetector":
                        DetectorAttachedStaticPresenceDetectorSetupTools.ImportAndApply();
                        break;
                    case "InspectDetectorAttachedStaticPresenceDetector":
                        DetectorAttachedStaticPresenceDetectorSetupTools.InspectApplied();
                        break;
                    case "CaptureDetectorAttachedStaticPresenceDetectorFinal":
                        DetectorAttachedStaticPresenceDetectorSetupTools.CaptureFinal();
                        break;
                    case "InspectDetectorAttachedStaticLocomotionSources":
                        DetectorAttachedStaticLocomotionTools.InspectSources();
                        break;
                    case "ApplyDetectorAttachedStaticLocomotion":
                        DetectorAttachedStaticLocomotionTools.Apply();
                        break;
                    case "InspectDetectorAttachedStaticLocomotion":
                        DetectorAttachedStaticLocomotionTools.BeginNaturalRuntimeReview(
                            request.Id, request.LogPath);
                        deferred = true;
                        break;
                    case "CaptureDetectorAttachedStaticLocomotionFinal":
                        DetectorAttachedStaticLocomotionTools.CaptureFinal();
                        break;
                    case "InspectElectricMineSetupSources":
                        ElectricMineSetupTools.InspectSources();
                        break;
                    case "ApplyElectricMineSetup":
                        ElectricMineSetupTools.ImportAndApply();
                        break;
                    case "InspectElectricMineSetup":
                        ElectricMineSetupTools.BeginNaturalRuntimeReview(
                            request.Id, request.LogPath);
                        deferred = true;
                        break;
                    case "CaptureElectricMineSetupFinal":
                        ElectricMineSetupTools.CaptureFinal();
                        break;
                    case "ApplyElectricMineManualStateReplication":
                        ElectricMineSetupTools.ApplyManualStateReplication();
                        break;
                    case "InspectElectricMineManualStateReplication":
                        ElectricMineSetupTools.InspectManualStateReplication();
                        break;
                    case "CaptureElectricMineManualStateFinal":
                        ElectricMineSetupTools.CaptureManualStateFinal();
                        break;
                    case "InspectElectricMineIdleLocomotionSources":
                        ElectricMineIdleLocomotionTools.InspectSources();
                        break;
                    case "ApplyElectricMineIdleLocomotion":
                        ElectricMineIdleLocomotionTools.Apply();
                        break;
                    case "InspectElectricMineIdleLocomotion":
                        ElectricMineIdleLocomotionTools.BeginNaturalRuntimeReview(
                            request.Id, request.LogPath);
                        deferred = true;
                        break;
                    case "CaptureElectricMineIdleLocomotionFinal":
                        ElectricMineIdleLocomotionTools.CaptureFinal();
                        break;
                    case "InspectElectricMineActivateSources":
                        ElectricMineActivateAnimationTools.InspectSources();
                        break;
                    case "ApplyElectricMineActivateAnimation":
                        ElectricMineActivateAnimationTools.Apply();
                        break;
                    case "InspectElectricMineActivateAnimation":
                        ElectricMineActivateAnimationTools.BeginNaturalRuntimeReview(
                            request.Id, request.LogPath);
                        deferred = true;
                        break;
                    case "InspectElectricMineActivateAnimationContinuous":
                        ElectricMineActivateAnimationTools.BeginContinuousRuntimeReview(
                            request.Id, request.LogPath);
                        deferred = true;
                        break;
                    case "CaptureElectricMineActivateAnimationFinal":
                        ElectricMineActivateAnimationTools.CaptureFinal();
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Unsupported Detector bridge command: " + request.Command);
                }

                if (!deferred)
                    WriteLog(
                        request.LogPath,
                        "Unity editor bridge request completed: " + request.Id + Environment.NewLine +
                        request.Command + " completed.");
            }
            catch (Exception exception)
            {
                WriteLog(
                    request.LogPath,
                    "Unity editor bridge request completed: " + request.Id + Environment.NewLine +
                    "status=failed" + Environment.NewLine + exception);
                Debug.LogWarning("Codex Unity bridge command failed: " + exception);
            }
            finally
            {
                try
                {
                    if (File.Exists(request.ClaimedPath)) File.Delete(request.ClaimedPath);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "Detector bridge could not remove its claimed request: " + exception.Message);
                }
            }
        }

        private static void WriteLog(string path, string value)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, value, new UTF8Encoding(false));
        }

        private readonly struct ClaimedRequest
        {
            internal ClaimedRequest(
                string id,
                string command,
                string logPath,
                string claimedPath)
            {
                Id = id;
                Command = command;
                LogPath = logPath;
                ClaimedPath = claimedPath;
            }

            internal string Id { get; }
            internal string Command { get; }
            internal string LogPath { get; }
            internal string ClaimedPath { get; }
        }
    }
}
