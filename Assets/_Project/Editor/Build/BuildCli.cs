using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Bellerophon.Editor.Build
{
    public static class BuildCli
    {
        public static void BuildWindows64()
        {
            var outputPath = GetArgument("-buildOutputPath") ?? "Builds/WindowsDev/Bellerophon.exe";
            BuildWindows64(outputPath, HasArgument("-developmentBuild"));
        }

        public static void BuildWindows64(string outputPath, bool developmentBuild)
        {
            outputPath = Path.GetFullPath(outputPath);
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes found in EditorBuildSettings.");
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new BuildFailedException($"Invalid build output path: {outputPath}");
            }

            Directory.CreateDirectory(outputDirectory);

            var options = BuildOptions.None;
            if (developmentBuild)
            {
                options |= BuildOptions.Development | BuildOptions.AllowDebugging;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Build failed with result {report.summary.result}.");
            }
        }

        private static bool HasArgument(string name)
        {
            return Environment.GetCommandLineArgs().Any(arg => arg.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
