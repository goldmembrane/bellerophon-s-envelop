using System;
using System.Globalization;
using System.IO;
using System.Xml;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(Bellerophon.Tests.PlayMode.UnityEditorBridgePlayModeResultCallback))]

namespace Bellerophon.Tests.PlayMode
{
    public sealed class UnityEditorBridgePlayModeResultCallback : ITestRunCallback
    {
        private const string ActiveRequestFileName = "UnityEditorBridge.active";
        private const string DefaultTestResultsFileName = "TestResults.xml";
        private const string NUnitVersion = "3.5.0.0";
        private const string PlayModeCommand = "PlayModeTests";

        public void RunStarted(ITest testsToRun)
        {
        }

        public void RunFinished(ITestResult testResults)
        {
            if (!IsOpenEditorBridgePlayModeRequestActive())
            {
                return;
            }

            try
            {
                var resultsPath = Path.Combine(Application.persistentDataPath, DefaultTestResultsFileName);
                var resultsDirectory = Path.GetDirectoryName(resultsPath);
                if (!string.IsNullOrWhiteSpace(resultsDirectory))
                {
                    Directory.CreateDirectory(resultsDirectory);
                }

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    NewLineOnAttributes = false
                };

                using (var writer = XmlWriter.Create(resultsPath, settings))
                {
                    WriteResultsXml(testResults, writer);
                }

                Debug.Log($"Saved open editor PlayMode bridge results to: {resultsPath}");
            }
            catch (Exception exception)
            {
                Debug.LogError("Saving open editor PlayMode bridge results failed.");
                Debug.LogException(exception);
            }
        }

        public void TestStarted(ITest test)
        {
        }

        public void TestFinished(ITestResult result)
        {
        }

        private static void WriteResultsXml(ITestResult result, XmlWriter writer)
        {
            var total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            var testRunNode = new TNode("test-run");
            testRunNode.AddAttribute("id", "2");
            testRunNode.AddAttribute("testcasecount", total.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("result", result.ResultState.Status.ToString());
            testRunNode.AddAttribute("total", total.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("passed", result.PassCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("failed", result.FailCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("inconclusive", result.InconclusiveCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("skipped", result.SkipCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("asserts", result.AssertCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("engine-version", NUnitVersion);
            testRunNode.AddAttribute("clr-version", Environment.Version.ToString());
            testRunNode.AddAttribute("start-time", result.StartTime.ToString("u", CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("end-time", result.EndTime.ToString("u", CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("duration", result.Duration.ToString(CultureInfo.InvariantCulture));
            testRunNode.ChildNodes.Add(result.ToXml(true));
            testRunNode.WriteTo(writer);
        }

        private static bool IsOpenEditorBridgePlayModeRequestActive()
        {
            var path = Path.Combine(ProjectRoot, "Logs", ActiveRequestFileName);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
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
                    if (!string.Equals(key, "command", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value = line.Substring(separatorIndex + 1);
                    return string.Equals(value, PlayModeCommand, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }
}
