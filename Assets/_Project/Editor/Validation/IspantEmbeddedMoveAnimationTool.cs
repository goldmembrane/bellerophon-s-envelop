using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantEmbeddedMoveAnimationTool
    {
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Embedded Move Source")]
        public static void InspectIspantEmbeddedMoveSource()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                           throw new InvalidOperationException("The direct Ispant ModelImporter is missing.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                         throw new InvalidOperationException("The direct Ispant FBX prefab is missing.");
            var defaultClips = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            var importedClips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            var renderers = prefab.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item.transform, prefab.transform), StringComparer.Ordinal)
                .ToArray();
            var separateWeaponRenderers = renderers.Where(item =>
                    ContainsWeaponName(item.name) ||
                    item.sharedMaterials.Any(material => material != null && ContainsWeaponName(material.name)))
                .ToArray();
            var report = new StringBuilder()
                .Append("IspantEmbeddedMoveSourceInspected Result=PASS")
                .Append(", ModelPath=").Append(ModelPath)
                .Append(", ImportAnimation=").Append(importer.importAnimation)
                .Append(", DefaultClipCount=").Append(defaultClips.Length)
                .Append(", ImportedClipCount=").Append(importedClips.Length)
                .Append(", DefaultClips=").Append(string.Join("|", defaultClips.Select(ClipDescription)))
                .Append(", ImportedClips=").Append(string.Join("|", importedClips.Select(item => item.name)))
                .Append(", RendererCount=").Append(renderers.Length)
                .Append(", Renderers=").Append(string.Join("|", renderers.Select(item => RendererDescription(prefab.transform, item))))
                .Append(", SeparateWeaponRendererCount=").Append(separateWeaponRenderers.Length)
                .Append(", SeparateWeaponRenderers=").Append(string.Join("|", separateWeaponRenderers.Select(item => item.name)))
                .Append('.');
            Debug.Log(report.ToString());
        }

        private static string ClipDescription(ModelImporterClipAnimation clip)
        {
            return clip.name + "@" + clip.takeName + "[" + clip.firstFrame + "-" + clip.lastFrame + "]";
        }

        private static string RendererDescription(Transform root, Renderer renderer)
        {
            var mesh = renderer is SkinnedMeshRenderer skinned
                ? skinned.sharedMesh
                : renderer.GetComponent<MeshFilter>()?.sharedMesh;
            return AnimationUtility.CalculateTransformPath(renderer.transform, root) +
                   ":" + renderer.GetType().Name +
                   ":Mesh=" + (mesh != null ? mesh.name : "<none>") +
                   ":Vertices=" + (mesh != null ? mesh.vertexCount : 0) +
                   ":Materials=" + string.Join("+", renderer.sharedMaterials.Select(item => item != null ? item.name : "<null>"));
        }

        private static bool ContainsWeaponName(string value)
        {
            return value.IndexOf("musket", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("rifle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("sword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf("longsword", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
