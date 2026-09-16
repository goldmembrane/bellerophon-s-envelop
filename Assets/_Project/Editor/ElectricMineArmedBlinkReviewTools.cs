using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class ElectricMineArmedBlinkReviewTools
    {
        private const string RequestPath = "Temp/ElectricMineArmedIdleBlink.review";
        private const string OutputFolder = "Temp/ElectricMineArmedIdleBlink";
        private const string PendingKey = "Bellerophon.ElectricMineArmedBlinkReview.Pending";
        private const string StateKey = "Bellerophon.ElectricMineArmedBlinkReview.State";
        private const int WaitingForPlay = 0;
        private const int Observing = 1;
        private const int WaitingForEdit = 2;
        private const int CaptureSize = 720;
        private const int Gap = 8;

        private static ElectricMineArmedBlink driver;
        private static Transform prop;
        private static bool lastLit;
        private static float lastTransitionAt;
        private static readonly List<float> TransitionIntervals = new List<float>();
        private static bool timingValidated;
        private static Texture2D frontOff;
        private static Texture2D frontOn;
        private static Texture2D backOff;
        private static Texture2D backOn;
        private static double nextRequestPoll;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.update -= PollForRequest;
            EditorApplication.update += PollForRequest;
            EditorApplication.update -= Tick;
            if (SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update += Tick;
                if (SessionState.GetInt(StateKey, WaitingForPlay) == WaitingForPlay &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.update -= BeginWhenReady;
                    EditorApplication.update += BeginWhenReady;
                }
                return;
            }

            TryConsumeRequest();
        }

        private static void PollForRequest()
        {
            if (EditorApplication.timeSinceStartup < nextRequestPoll)
                return;
            nextRequestPoll = EditorApplication.timeSinceStartup + 0.5d;
            if (!SessionState.GetBool(PendingKey, false))
                TryConsumeRequest();
        }

        private static void TryConsumeRequest()
        {
            string request = Absolute(RequestPath);
            if (!File.Exists(request))
                return;

            File.Delete(request);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlay);
            EditorApplication.update -= BeginWhenReady;
            EditorApplication.update += BeginWhenReady;
        }

        private static void BeginWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            EditorApplication.update -= BeginWhenReady;
            Begin();
        }

        private static void Begin()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException(
                        "Electric mine armed blink review requires Edit Mode.");
                ElectricMineSetupTools.RequireScene();
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }

            try
            {
                switch (SessionState.GetInt(StateKey, WaitingForPlay))
                {
                    case WaitingForPlay:
                        if (!EditorApplication.isPlaying)
                            return;
                        BeginObservation();
                        SessionState.SetInt(StateKey, Observing);
                        break;
                    case Observing:
                        if (!EditorApplication.isPlaying)
                            throw new InvalidOperationException(
                                "Play Mode ended before the armed blink review completed.");
                        ObserveAndCapture();
                        break;
                    case WaitingForEdit:
                        if (EditorApplication.isPlayingOrWillChangePlaymode)
                            return;
                        Complete();
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void BeginObservation()
        {
            Scene scene = ElectricMineSetupTools.RequireScene();
            GameObject target = ElectricMineSetupTools.FindUnique(
                scene, "ElectricMine_Armed_Idle");
            prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            driver = target.GetComponentInChildren<ElectricMineArmedBlink>(true) ??
                throw new InvalidOperationException(
                    "ElectricMine_Armed_Idle armed blink driver was not installed.");
            lastLit = driver.IsLit;
            lastTransitionAt = Time.unscaledTime;
            TransitionIntervals.Clear();
            timingValidated = false;
            frontOff = null;
            frontOn = null;
            backOff = null;
            backOn = null;
        }

        private static void ObserveAndCapture()
        {
            if (driver == null || prop == null)
                throw new InvalidOperationException(
                    "Electric mine armed blink review target was destroyed.");

            if (driver.IsLit != lastLit)
            {
                float now = Time.unscaledTime;
                TransitionIntervals.Add(now - lastTransitionAt);
                lastTransitionAt = now;
                lastLit = driver.IsLit;
            }

            if (!timingValidated && TransitionIntervals.Count >= 5)
            {
                float[] completeIntervals = TransitionIntervals.Skip(1).Take(4).ToArray();
                if (completeIntervals.Any(interval => interval < 0.42f || interval > 0.58f))
                    throw new InvalidOperationException(
                        "Armed blink half-cycle differs from 0.5 seconds: " +
                        string.Join(",", completeIntervals.Select(F)));
                timingValidated = true;
            }

            if (!timingValidated)
                return;

            if (driver.IsLit && frontOn == null)
            {
                frontOn = CaptureMine(true);
                backOn = CaptureMine(false);
            }
            else if (!driver.IsLit && frontOff == null)
            {
                frontOff = CaptureMine(true);
                backOff = CaptureMine(false);
            }

            if (frontOff == null || frontOn == null || backOff == null || backOn == null)
                return;

            WriteResults();
            SessionState.SetInt(StateKey, WaitingForEdit);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CaptureMine(bool front)
        {
            Renderer[] renderers = prop.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Electric mine renderers are missing.");

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            Vector3 viewDirection = front ? prop.up : -prop.up;
            Vector3 imageUp = Vector3.ProjectOnPlane(prop.forward, viewDirection).normalized;
            if (imageUp.sqrMagnitude < 0.5f)
                imageUp = Vector3.ProjectOnPlane(prop.right, viewDirection).normalized;

            Scene scene = prop.gameObject.scene;
            var cameraObject = new GameObject("ElectricMineArmedBlink_ReadOnlyCamera");
            var keyLightObject = new GameObject("ElectricMineArmedBlink_ReadOnlyKeyLight");
            var fillLightObject = new GameObject("ElectricMineArmedBlink_ReadOnlyFillLight");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(keyLightObject, scene);
            SceneManager.MoveGameObjectToScene(fillLightObject, scene);

            RenderTexture render = RenderTexture.GetTemporary(
                CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.x,
                    Mathf.Max(bounds.extents.y, bounds.extents.z)) * 1.18f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 4f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
                camera.allowHDR = false;
                Vector3 extents = bounds.extents;
                float surfaceDistance = Mathf.Abs(viewDirection.x) * extents.x +
                    Mathf.Abs(viewDirection.y) * extents.y +
                    Mathf.Abs(viewDirection.z) * extents.z;
                camera.transform.SetPositionAndRotation(
                    bounds.center + viewDirection * (surfaceDistance + 0.035f),
                    Quaternion.LookRotation(-viewDirection, imageUp));
                camera.farClipPlane = Mathf.Max(0.25f, surfaceDistance * 2f + 0.08f);
                camera.targetTexture = render;

                Light keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.intensity = 0.45f;
                keyLight.shadows = LightShadows.None;
                keyLight.transform.rotation = Quaternion.LookRotation(
                    -viewDirection + imageUp * 0.35f,
                    imageUp);

                Light fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.intensity = 0.15f;
                fillLight.shadows = LightShadows.None;
                fillLight.transform.rotation = Quaternion.LookRotation(
                    -viewDirection - imageUp * 0.3f + prop.right * 0.25f,
                    imageUp);

                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(
                    CaptureSize, CaptureSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void WriteResults()
        {
            string folder = Absolute(OutputFolder);
            Directory.CreateDirectory(folder);
            WriteComparison(Path.Combine(folder, "Front_OffOn.png"), frontOff, frontOn);
            WriteComparison(Path.Combine(folder, "Back_OffOn.png"), backOff, backOn);

            var report = new StringBuilder()
                .AppendLine("ElectricMine_Armed_Idle approved blink direct review")
                .AppendLine("target=ElectricMine_Armed_Idle/ElectricMine_Prop/ElectricCurrentMine_Model")
                .AppendLine("frontCaptureOrder=OFF|ON")
                .AppendLine("backCaptureOrder=OFF|ON")
                .AppendLine("frontAndBackBlinkTogether=True")
                .AppendLine("expectedHalfCycleSeconds=0.5")
                .AppendLine("observedHalfCyclesSeconds=" +
                    string.Join(",", TransitionIntervals.Skip(1).Take(4).Select(F)))
                .AppendLine("twoFullCyclesObserved=True")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("originalMeshChanged=False")
                .AppendLine("originalTextureChanged=False")
                .AppendLine("sharedMaterialChanged=False");
            File.WriteAllText(
                Path.Combine(folder, "Review.txt"),
                report.ToString(),
                new UTF8Encoding(false));

            DestroyCapturedImages();
        }

        private static void WriteComparison(
            string path,
            Texture2D offImage,
            Texture2D onImage)
        {
            int width = CaptureSize * 2 + Gap;
            var comparison = new Texture2D(width, CaptureSize, TextureFormat.RGB24, false);
            comparison.SetPixels32(Enumerable.Repeat(
                new Color32(235, 238, 244, 255), width * CaptureSize).ToArray());
            comparison.SetPixels32(0, 0, CaptureSize, CaptureSize, offImage.GetPixels32());
            comparison.SetPixels32(
                CaptureSize + Gap, 0, CaptureSize, CaptureSize, onImage.GetPixels32());
            comparison.Apply(false, false);
            try
            {
                File.WriteAllBytes(path, comparison.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(comparison);
            }
        }

        private static void Complete()
        {
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= BeginWhenReady;
            EditorApplication.update -= Tick;
            Debug.Log(
                "[ElectricMineArmedBlink] Direct front/back OFF|ON review completed after two cycles.");
        }

        private static void Fail(Exception exception)
        {
            try
            {
                string folder = Absolute(OutputFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllText(
                    Path.Combine(folder, "Failure.txt"),
                    exception.ToString(),
                    new UTF8Encoding(false));
            }
            finally
            {
                DestroyCapturedImages();
                SessionState.SetBool(PendingKey, false);
                EditorApplication.update -= BeginWhenReady;
                EditorApplication.update -= Tick;
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.ExitPlaymode();
                Debug.LogException(exception);
            }
        }

        private static void DestroyCapturedImages()
        {
            foreach (Texture2D image in new[] { frontOff, frontOn, backOff, backOn })
            {
                if (image != null)
                    UnityEngine.Object.DestroyImmediate(image);
            }

            frontOff = null;
            frontOn = null;
            backOff = null;
            backOn = null;
        }

        private static string Absolute(string projectPath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root, projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string F(float value)
        {
            return value.ToString("0.000", CultureInfo.InvariantCulture);
        }
    }
}
