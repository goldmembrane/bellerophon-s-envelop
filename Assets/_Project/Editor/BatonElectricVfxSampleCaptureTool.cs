using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor
{
    internal static class BatonElectricVfxSampleCaptureTool
    {
        private const string BatonAssetPath =
            "Assets/_Project/Art/Items/ElectricBaton/electric_baton.fbx";
        private const string OutputRelativePath =
            "artSample/baton_electric_vfx/unity_model_full.png";
        private const int OutputWidth = 1024;
        private const int OutputHeight = 1280;

        [InitializeOnLoadMethod]
        private static void ScheduleCaptureAfterScriptsReload()
        {
            EditorApplication.update -= CaptureWhenEditorIsReady;
            EditorApplication.update += CaptureWhenEditorIsReady;
        }

        [MenuItem("Bellerophon/Art Samples/Capture Electric Baton Full Unity Model")]
        internal static void CaptureElectricBatonFullModelSample()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(BatonAssetPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    "The imported Unity electric baton model was not found: " +
                    BatonAssetPath);
            }

            var preview = new PreviewRenderUtility();
            GameObject clone = null;
            Texture2D rendered = null;
            try
            {
                clone = UnityEngine.Object.Instantiate(source);
                clone.name = "ElectricBaton_UnityModel_FullPreview";
                clone.hideFlags = HideFlags.HideAndDontSave;
                clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                clone.transform.localScale = Vector3.one;

                SetAllRenderersVisible(clone);
                OrientLongestAxisVertically(clone);
                preview.AddSingleGO(clone);

                var bounds = CalculateBounds(clone);
                ConfigureCamera(preview.camera, bounds);
                ConfigureLighting(preview);
                preview.BeginStaticPreview(
                    new Rect(0f, 0f, OutputWidth, OutputHeight));
                preview.Render(true);
                rendered = preview.EndStaticPreview();
                if (rendered == null)
                {
                    throw new InvalidOperationException(
                        "Unity PreviewRenderUtility returned no electric baton image.");
                }

                var outputPath = ProjectAbsolutePath(OutputRelativePath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(outputPath) ??
                    throw new InvalidOperationException(
                        "Electric baton sample output folder is invalid."));
                File.WriteAllBytes(outputPath, rendered.EncodeToPNG());
                Debug.Log(
                    "Electric baton full Unity model sample captured. " +
                    "Asset=" + BatonAssetPath +
                    ", Output=" + OutputRelativePath +
                    ", BoundsCenter=" + bounds.center.ToString("R") +
                    ", BoundsSize=" + bounds.size.ToString("R"));
            }
            finally
            {
                if (rendered != null)
                {
                    UnityEngine.Object.DestroyImmediate(rendered);
                }

                preview.Cleanup();
            }
        }

        private static void CaptureWhenEditorIsReady()
        {
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= CaptureWhenEditorIsReady;
            try
            {
                var outputPath = ProjectAbsolutePath(OutputRelativePath);
                var scriptPath = ProjectAbsolutePath(
                    "Assets/_Project/Editor/BatonElectricVfxSampleCaptureTool.cs");
                var assetPath = ProjectAbsolutePath(BatonAssetPath);
                if (File.Exists(outputPath) &&
                    File.GetLastWriteTimeUtc(outputPath) >= File.GetLastWriteTimeUtc(scriptPath) &&
                    File.GetLastWriteTimeUtc(outputPath) >= File.GetLastWriteTimeUtc(assetPath))
                {
                    return;
                }

                CaptureElectricBatonFullModelSample();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void SetAllRenderersVisible(GameObject clone)
        {
            clone.SetActive(true);
            foreach (var transform in clone.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.SetActive(true);
            }

            var renderers = clone.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "The imported Unity electric baton contains no renderers.");
            }

            foreach (var renderer in renderers)
            {
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.SetPropertyBlock(null);
            }
        }

        private static void OrientLongestAxisVertically(GameObject clone)
        {
            var bounds = CalculateBounds(clone);
            var size = bounds.size;
            if (size.x >= size.y && size.x >= size.z)
            {
                clone.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }
            else if (size.z >= size.x && size.z >= size.y)
            {
                clone.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "The electric baton preview contains no renderers.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void ConfigureCamera(Camera camera, Bounds bounds)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.945f, 0.953f, 0.957f, 1f);
            camera.cullingMask = ~0;
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = Mathf.Max(100f, bounds.size.magnitude * 12f);
            camera.allowHDR = true;

            var target = bounds.center;
            var distance = Mathf.Max(2f, bounds.size.magnitude * 3f);
            camera.transform.position = target + Vector3.forward * distance;
            camera.transform.LookAt(target, Vector3.up);

            var aspect = (float)OutputWidth / OutputHeight;
            var verticalHalf = bounds.extents.y;
            var horizontalHalf = bounds.extents.x / aspect;
            camera.orthographicSize = Mathf.Max(verticalHalf, horizontalHalf) * 1.12f;
        }

        private static void ConfigureLighting(PreviewRenderUtility preview)
        {
            preview.lights[0].transform.rotation = Quaternion.Euler(32f, 28f, 0f);
            preview.lights[0].color = new Color(1f, 0.96f, 0.90f, 1f);
            preview.lights[0].intensity = 1.65f;
            preview.lights[0].shadows = LightShadows.Soft;
            preview.lights[1].transform.rotation = Quaternion.Euler(334f, 214f, 0f);
            preview.lights[1].color = new Color(0.58f, 0.76f, 1f, 1f);
            preview.lights[1].intensity = 1.1f;
            preview.lights[1].shadows = LightShadows.None;
            preview.ambientColor = new Color(0.52f, 0.54f, 0.56f, 1f);
        }

        private static string ProjectAbsolutePath(string projectRelativePath)
        {
            var projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(
                Path.Combine(projectRoot, projectRelativePath));
        }
    }
}
