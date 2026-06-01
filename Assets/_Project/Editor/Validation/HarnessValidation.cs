using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class HarnessValidation
    {
        public static void Run()
        {
            var failures = new List<string>();

            RequireUnityVersion(failures);
            RequirePath(failures, "AGENTS.md");
            RequirePath(failures, "docs/HARNESS.md");
            RequirePath(failures, "docs/ARCHITECTURE.md");
            RequirePath(failures, "Assets/_Project/Runtime");
            RequirePath(failures, "Assets/_Project/Editor");
            RequirePath(failures, "Assets/_Project/Tests/EditMode");
            RequirePath(failures, "Assets/_Project/Tests/PlayMode");
            RequirePackage(failures, "com.unity.test-framework");

            if (failures.Count > 0)
            {
                throw new InvalidOperationException("Harness validation failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log("Harness validation passed.");
        }

        private static void RequireUnityVersion(ICollection<string> failures)
        {
            if (!Application.unityVersion.StartsWith("6000.3.", StringComparison.Ordinal))
            {
                failures.Add($"Unity version must be 6000.3.x LTS. Current: {Application.unityVersion}");
            }
        }

        private static void RequirePath(ICollection<string> failures, string relativePath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                failures.Add($"Missing required path: {relativePath}");
            }
        }

        private static void RequirePackage(ICollection<string> failures, string packageName)
        {
            var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                failures.Add("Missing Packages/manifest.json");
                return;
            }

            var manifest = File.ReadAllText(manifestPath);
            if (manifest.IndexOf($"\"{packageName}\"", StringComparison.Ordinal) < 0)
            {
                failures.Add($"Missing required package: {packageName}");
            }
        }
    }
}
