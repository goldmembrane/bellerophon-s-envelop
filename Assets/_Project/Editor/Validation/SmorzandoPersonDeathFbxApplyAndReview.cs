using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor.SmorzandoCargoRunScene
{
    internal static class SmorzandoPersonDeathFbxApplyAndReview
    {
        private const string DeathSourceRelativePath = "enemies model/smorzando death.fbx";
        private const string DeathModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person_Death.fbx";
        private const string StaticModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person.fbx";
        private const string InspectionReportRelativePath =
            "docs/validation/smorzando_person_death_fbx_2026-07-18/Smorzando_PersonDeathFbxSourceInspection.txt";

        [MenuItem("Bellerophon/Enemies/Smorzando/Inspect Person Death FBX Source")]
        public static void InspectSmorzandoPersonDeathFbxSource()
        {
            var deathAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DeathModelAssetPath) ??
                throw new InvalidOperationException("Smorzando death FBX has not been imported.");
            var staticAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StaticModelAssetPath) ??
                throw new InvalidOperationException("Smorzando static person FBX is missing.");
            var importer = AssetImporter.GetAtPath(DeathModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Smorzando death FBX importer is missing.");
            var deathRenderers = deathAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var staticRenderers = staticAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var clips = AssetDatabase.LoadAllAssetsAtPath(DeathModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderBy(clip => clip.name, StringComparer.Ordinal)
                .ToArray();

            var report = new StringBuilder();
            report.AppendLine("Asset=" + DeathModelAssetPath);
            report.AppendLine("SourceSha256=" + ComputeSha256(ProjectAbsolutePath(DeathSourceRelativePath)));
            report.AppendLine("ImportedSha256=" + ComputeSha256(ProjectAbsolutePath(DeathModelAssetPath)));
            report.AppendLine("StaticSha256=" + ComputeSha256(ProjectAbsolutePath(StaticModelAssetPath)));
            report.AppendLine("AnimationType=" + importer.animationType);
            report.AppendLine("ImportAnimation=" + importer.importAnimation);
            report.AppendLine("DeathRendererCount=" + deathRenderers.Length);
            report.AppendLine("StaticRendererCount=" + staticRenderers.Length);
            report.AppendLine("ClipCount=" + clips.Length);
            AppendRendererReport(report, "Death", deathRenderers);
            AppendRendererReport(report, "Static", staticRenderers);

            for (var index = 0; index < clips.Length; index++)
            {
                var clip = clips[index];
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                var rootBindings = bindings
                    .Where(IsRootMotionBinding)
                    .Select(binding => binding.path + ":" + binding.type.Name + ":" + binding.propertyName)
                    .Distinct()
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                report.AppendLine(
                    $"Clip[{index}]={clip.name},Length={clip.length:0.######},FrameRate={clip.frameRate:0.######}," +
                    $"Loop={clip.isLooping},Curves={bindings.Length},ObjectCurves={objectBindings.Length}," +
                    $"RootMotionBindings={rootBindings.Length}");
                foreach (var binding in rootBindings)
                {
                    report.AppendLine("RootMotionBinding=" + binding);
                }
            }

            WriteTextReport(InspectionReportRelativePath, report.ToString());
            Selection.activeObject = null;
            Debug.Log(
                $"SmorzandoPersonDeathFbxSourceInspected Renderers={deathRenderers.Length}, " +
                $"Clips={clips.Length}, SelectionCleared=True");
        }

        private static bool IsRootMotionBinding(EditorCurveBinding binding)
        {
            return string.IsNullOrEmpty(binding.path) ||
                binding.propertyName.IndexOf("RootT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                binding.propertyName.IndexOf("MotionT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                binding.propertyName.IndexOf("RootQ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                binding.propertyName.IndexOf("MotionQ", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AppendRendererReport(
            StringBuilder report,
            string prefix,
            SkinnedMeshRenderer[] renderers)
        {
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                var mesh = renderer.sharedMesh;
                report.AppendLine(
                    $"{prefix}Renderer[{index}]={RelativePath(renderer.transform.root, renderer.transform)}," +
                    $"Mesh={(mesh != null ? mesh.name : "None")}," +
                    $"Vertices={(mesh != null ? mesh.vertexCount : 0)}," +
                    $"SubMeshes={(mesh != null ? mesh.subMeshCount : 0)}," +
                    $"Bones={renderer.bones.Length}," +
                    $"BoundsCenter={FormatVector(renderer.localBounds.center)}," +
                    $"BoundsSize={FormatVector(renderer.localBounds.size)}," +
                    $"Materials={string.Join("|", renderer.sharedMaterials.Select(MaterialName))}");
            }
        }

        private static string RelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }
            var parts = new System.Collections.Generic.List<string>();
            for (var current = target; current != null && current != root; current = current.parent)
            {
                parts.Add(current.name);
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void WriteTextReport(string relativePath, string contents)
        {
            var path = ProjectAbsolutePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ProjectAbsolutePath("docs/validation"));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.######},{value.y:0.######},{value.z:0.######})";
        }

        private static string MaterialName(Material material)
        {
            return material != null ? material.name : "None";
        }
    }
}
