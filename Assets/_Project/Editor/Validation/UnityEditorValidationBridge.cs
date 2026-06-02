using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Bellerophon.Editor.Build;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class UnityEditorValidationBridge
    {
        private const string RequestFileName = "UnityEditorBridge.request";
        private const string ActiveRequestFileName = "UnityEditorBridge.active";
        private const string DefaultTestResultsFileName = "TestResults.xml";
        private const double PollIntervalSeconds = 0.5d;

        private static double nextPollTime;
        private static bool isRunning;
        private static BridgeRequest activeRequest;
        private static StringBuilder activeLog;
        private static TestRunnerApi activeTestRunnerApi;
        private static TestRunCallbacks activeTestRunCallbacks;

        static UnityEditorValidationBridge()
        {
            EditorApplication.update += PollForRequest;
        }

        [MenuItem("Bellerophon/Validation/Run Harness Validation")]
        private static void RunHarnessValidationFromMenu()
        {
            RunSynchronous(
                BridgeRequest.Manual("HarnessValidation", DefaultLogPath("HarnessValidation.log")),
                HarnessValidation.Run,
                "Harness validation passed.");
        }

        private static void PollForRequest()
        {
            if (isRunning)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < nextPollTime)
            {
                return;
            }

            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            if (TryCompleteRecoveredPlayModeRequest())
            {
                return;
            }

            var requestPath = Path.Combine(ProjectRoot, "Logs", RequestFileName);
            if (!File.Exists(requestPath))
            {
                return;
            }

            var request = BridgeRequest.Read(requestPath);
            if (!request.IsValid)
            {
                return;
            }

            TryDelete(requestPath);
            StartRequest(request);
        }

        private static void StartRequest(BridgeRequest request)
        {
            if (request.Command != "PlayModeTests")
            {
                TryDelete(ActiveRequestPath);
            }

            switch (request.Command)
            {
                case "HarnessValidation":
                    RunSynchronous(request, HarnessValidation.Run, "Harness validation passed.");
                    break;
                case "EditModeTests":
                    RunTests(request, TestMode.EditMode);
                    break;
                case "PlayModeTests":
                    RunTests(request, TestMode.PlayMode);
                    break;
                case "WindowsDevBuild":
                    RunSynchronous(
                        request,
                        () => BuildCli.BuildWindows64(request.OutputPath, request.DevelopmentBuild),
                        "Build Finished, Result: Success");
                    break;
                case "EnsurePhase2PlayerMvp":
                    RunSynchronous(
                        request,
                        Phase2PlayerMvpBootstrap.EnsurePhase2Assets,
                        "Phase 2 player MVP assets are ready.");
                    break;
                case "ValidatePhase2PlayerMvp":
                    RunSynchronous(
                        request,
                        Phase2PlayerMvpEditorValidation.Run,
                        "Phase 2 player MVP editor validation passed.");
                    break;
                case "EnsurePhase4CargoShipGraybox":
                    RunSynchronous(
                        request,
                        Phase4CargoShipGrayboxBootstrap.EnsurePhase4Assets,
                        "Phase 4 cargo ship graybox assets are ready.");
                    break;
                case "ValidatePhase4CargoShipGraybox":
                    RunSynchronous(
                        request,
                        Phase4CargoShipGrayboxEditorValidation.Run,
                        "Phase 4 cargo ship graybox editor validation passed.");
                    break;
                case "EnsurePhase6RoomInteractions":
                    RunSynchronous(
                        request,
                        Phase6RoomInteractionsBootstrap.EnsurePhase6Assets,
                        "Phase 6 room interaction assets are ready.");
                    break;
                case "ValidatePhase6RoomInteractions":
                    RunSynchronous(
                        request,
                        Phase6RoomInteractionsEditorValidation.Run,
                        "Phase 6 room interactions editor validation passed.");
                    break;
                case "EnsurePhase7NewGameStart":
                    RunSynchronous(
                        request,
                        Phase7NewGameStartBootstrap.EnsurePhase7Assets,
                        "Phase 7 new game start assets are ready.");
                    break;
                case "ValidatePhase7NewGameStart":
                    RunSynchronous(
                        request,
                        Phase7NewGameStartEditorValidation.Run,
                        "Phase 7 new game start editor validation passed.");
                    break;
                default:
                    RunSynchronous(
                        request,
                        () => throw new InvalidOperationException($"Unknown bridge command: {request.Command}"),
                        string.Empty);
                    break;
            }
        }

        private static void RunSynchronous(BridgeRequest request, Action action, string successMarker)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                action();
                CompleteRequest(successMarker);
            }
            catch (Exception exception)
            {
                FailRequest(exception);
            }
        }

        private static void RunTests(BridgeRequest request, TestMode testMode)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                Directory.CreateDirectory(Path.GetDirectoryName(request.ResultsPath));
                if (testMode == TestMode.PlayMode)
                {
                    request.StartUtcTicks = DateTime.UtcNow.Ticks;
                    TryDelete(DefaultTestResultsPath);
                    request.Write(ActiveRequestPath);
                }

                activeTestRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                activeTestRunCallbacks = new TestRunCallbacks(request);
                activeTestRunnerApi.RegisterCallbacks(activeTestRunCallbacks);
                activeTestRunnerApi.Execute(new ExecutionSettings(new Filter { testMode = testMode }));
            }
            catch (Exception exception)
            {
                ClearTestRunState();
                FailRequest(exception);
            }
        }

        private static void BeginRequest(BridgeRequest request)
        {
            isRunning = true;
            activeRequest = request;
            activeLog = new StringBuilder();
            Application.logMessageReceived += CaptureLog;
        }

        private static void CompleteRequest(string successMarker)
        {
            WriteLog(activeRequest, false, null, successMarker);
            EndRequest();
        }

        private static void FailRequest(Exception exception)
        {
            WriteLog(activeRequest, true, exception, string.Empty);
            EndRequest();
        }

        private static void EndRequest()
        {
            Application.logMessageReceived -= CaptureLog;
            activeRequest = null;
            activeLog = null;
            isRunning = false;
        }

        private static void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (activeLog == null)
            {
                return;
            }

            activeLog.AppendLine($"{type}: {condition}");
            if (type == LogType.Exception || type == LogType.Error)
            {
                activeLog.AppendLine(stackTrace);
            }
        }

        private static void RequireScriptsCompiled()
        {
            if (HasScriptCompilationFailed())
            {
                throw new InvalidOperationException("Scripts have compiler errors.");
            }
        }

        private static bool HasScriptCompilationFailed()
        {
            var property = typeof(EditorUtility).GetProperty(
                "scriptCompilationFailed",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            return property != null &&
                   property.PropertyType == typeof(bool) &&
                   (bool)property.GetValue(null);
        }

        private static void WriteLog(
            BridgeRequest request,
            bool failed,
            Exception exception,
            string successMarker)
        {
            var logPath = string.IsNullOrWhiteSpace(request.LogPath)
                ? DefaultLogPath($"{request.Command}.log")
                : request.LogPath;

            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            var builder = new StringBuilder();
            builder.AppendLine($"Unity editor bridge request completed: {request.Id}");
            builder.AppendLine($"Unity editor bridge command: {request.Command}");
            builder.AppendLine("Unity editor bridge mode: open editor");

            if (!string.IsNullOrWhiteSpace(successMarker))
            {
                builder.AppendLine(successMarker);
            }

            if (failed)
            {
                builder.AppendLine("Unity editor bridge failed.");
                builder.AppendLine(exception.ToString());
            }

            if (activeLog != null && activeLog.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Captured Unity log:");
                builder.Append(activeLog);
            }

            File.WriteAllText(logPath, builder.ToString());
        }

        private static void ClearTestRunState()
        {
            if (activeTestRunCallbacks != null)
            {
                TestRunnerApi.UnregisterTestCallback(activeTestRunCallbacks);
            }

            if (activeTestRunnerApi != null)
            {
                UnityEngine.Object.DestroyImmediate(activeTestRunnerApi);
            }

            activeTestRunCallbacks = null;
            activeTestRunnerApi = null;
            TryDelete(ActiveRequestPath);
        }

        private static bool TryCompleteRecoveredPlayModeRequest()
        {
            if (!File.Exists(ActiveRequestPath))
            {
                return false;
            }

            var request = BridgeRequest.Read(ActiveRequestPath);
            if (!request.IsValid || request.Command != "PlayModeTests")
            {
                TryDelete(ActiveRequestPath);
                return false;
            }

            if (!File.Exists(DefaultTestResultsPath))
            {
                return false;
            }

            if (request.StartUtcTicks > 0 &&
                File.GetLastWriteTimeUtc(DefaultTestResultsPath).Ticks < request.StartUtcTicks)
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(request.ResultsPath));
                File.Copy(DefaultTestResultsPath, request.ResultsPath, true);
                WriteLog(request, false, null, "PlayModeTests completed.");
            }
            catch (Exception exception)
            {
                WriteLog(request, true, exception, string.Empty);
            }
            finally
            {
                TryDelete(ActiveRequestPath);
            }

            return true;
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

        private static string DefaultLogPath(string fileName)
        {
            return Path.Combine(ProjectRoot, "Logs", fileName);
        }

        private static string ActiveRequestPath =>
            Path.Combine(ProjectRoot, "Logs", ActiveRequestFileName);

        private static string DefaultTestResultsPath =>
            Path.Combine(Application.persistentDataPath, DefaultTestResultsFileName);

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private sealed class TestRunCallbacks : ICallbacks
        {
            private readonly BridgeRequest request;

            public TestRunCallbacks(BridgeRequest request)
            {
                this.request = request;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"Running {request.Command} through open editor bridge.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                try
                {
                    TestRunnerApi.SaveResultToFile(result, request.ResultsPath);
                    TryDelete(ActiveRequestPath);
                    CompleteRequest($"{request.Command} completed.");
                }
                catch (Exception exception)
                {
                    FailRequest(exception);
                }
                finally
                {
                    ClearTestRunState();
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogError($"Failed {result.FullName}: {result.Message}");
                }
            }
        }

        private sealed class BridgeRequest
        {
            public string Id { get; private set; }
            public string Command { get; private set; }
            public string LogPath { get; private set; }
            public string ResultsPath { get; private set; }
            public string OutputPath { get; private set; }
            public bool DevelopmentBuild { get; private set; }
            public long StartUtcTicks { get; set; }

            public bool IsValid =>
                !string.IsNullOrWhiteSpace(Id) &&
                !string.IsNullOrWhiteSpace(Command);

            public static BridgeRequest Manual(string command, string logPath)
            {
                return new BridgeRequest
                {
                    Id = "manual",
                    Command = command,
                    LogPath = logPath
                };
            }

            public static BridgeRequest Read(string path)
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

                return new BridgeRequest
                {
                    Id = Get(values, "id"),
                    Command = Get(values, "command"),
                    LogPath = Get(values, "logPath"),
                    ResultsPath = Get(values, "resultsPath"),
                    OutputPath = Get(values, "outputPath"),
                    DevelopmentBuild = bool.TryParse(Get(values, "developmentBuild"), out var developmentBuild) &&
                                       developmentBuild,
                    StartUtcTicks = long.TryParse(Get(values, "startUtcTicks"), out var startUtcTicks)
                        ? startUtcTicks
                        : 0L
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
                        $"command={Command}",
                        $"logPath={LogPath}",
                        $"resultsPath={ResultsPath}",
                        $"outputPath={OutputPath}",
                        $"developmentBuild={DevelopmentBuild}",
                        $"startUtcTicks={StartUtcTicks}"
                    });
            }

            private static string Get(IDictionary<string, string> values, string key)
            {
                return values.TryGetValue(key, out var value) ? value : string.Empty;
            }
        }
    }
}
