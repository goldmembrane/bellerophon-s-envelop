using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class ArmorProtectiveIdleStartViewTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlayerName = "Player";
        private const string TargetName = "Armor_Protective_Idle";
        private const string OutputFolder =
            "docs/validation/armor_protective_idle_start_view_2026-09-08";
        private const float ReviewDistanceMeters = 2f;
        private const float PositionToleranceMeters = 0.01f;
        private const float FacingDotMinimum = 0.995f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        static ArmorProtectiveIdleStartViewTools()
        {
            EditorApplication.update -= CompletePendingFirstFrameCapture;
            EditorApplication.update += CompletePendingFirstFrameCapture;
        }

        [MenuItem("Bellerophon/Player/Apply Armor Protective Idle Start View")]
        internal static void ApplyArmorProtectiveIdleStartView()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Armor_Protective_Idle start view must be applied in Edit Mode.");
            }

            Scene scene = RequireScene();
            Transform player = FindUnique(PlayerName);
            Transform target = FindUnique(TargetName);
            Camera camera = RequirePlayerCamera(player);
            Bounds bounds = BoundsOf(target);
            Vector3 front = Horizontal(target.forward);
            if (front.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    "Armor_Protective_Idle has no valid horizontal forward direction.");
            }

            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;
            float playerHeightBefore = player.position.y;

            Vector3 desiredCamera = bounds.center + front * ReviewDistanceMeters;
            Quaternion desiredYaw = Quaternion.LookRotation(-front, Vector3.up);
            Vector3 cameraOffsetInPlayerYaw =
                Quaternion.Inverse(player.rotation) *
                (camera.transform.position - player.position);
            Vector3 desiredPlayer =
                desiredCamera - desiredYaw * cameraOffsetInPlayerYaw;
            desiredPlayer.y = playerHeightBefore;
            player.SetPositionAndRotation(desiredPlayer, desiredYaw);

            EditorUtility.SetDirty(player.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            }

            StartViewMetrics metrics = Inspect(player, target, camera, bounds);
            bool targetUnchanged =
                Vector3.Distance(targetPositionBefore, target.position) <= 0.000001f &&
                Quaternion.Angle(targetRotationBefore, target.rotation) <= 0.000001f &&
                Vector3.Distance(targetScaleBefore, target.localScale) <= 0.000001f;
            if (!targetUnchanged)
            {
                throw new InvalidOperationException(
                    "Armor_Protective_Idle transform changed while positioning the start view.");
            }

            Directory.CreateDirectory(Absolute(OutputFolder));
            var report = new StringBuilder();
            report.AppendLine("Armor_Protective_Idle start view applied.");
            report.AppendLine("targetPosition=" + Vec(target.position));
            report.AppendLine("targetForward=" + Vec(front));
            report.AppendLine("playerPosition=" + Vec(player.position));
            report.AppendLine("playerForward=" + Vec(player.forward));
            report.AppendLine("playerHeightPreserved=True");
            report.AppendLine("targetTransformUnchanged=True");
            AppendMetrics(report, metrics);
            File.WriteAllText(
                Absolute(OutputFolder + "/applied.txt"),
                report.ToString(),
                new UTF8Encoding(false));

            Debug.Log(
                "Armor_Protective_Idle start view applied. CameraDistance=" +
                Num(metrics.CameraDistance) + ", FacingDot=" + Num(metrics.FacingDot) +
                ", SceneSaved=True.");
        }

        internal static void InspectArmorProtectiveIdleStartView()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.EnterPlaymode();
                Debug.Log("Armor_Protective_Idle start-view inspection entered Play Mode.");
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Armor_Protective_Idle start-view inspection is waiting for Play Mode.");
            }

            RequireScene();
            Transform player = FindUnique(PlayerName);
            Transform target = FindUnique(TargetName);
            Camera camera = RequirePlayerCamera(player);
            Bounds bounds = BoundsOf(target);
            StartViewMetrics metrics = Inspect(player, target, camera, bounds);
            Directory.CreateDirectory(Absolute(OutputFolder));

            var report = new StringBuilder();
            report.AppendLine("Read-only live Armor_Protective_Idle start-view inspection.");
            report.AppendLine("playMode=True");
            report.AppendLine("target=Armor_Protective_Idle");
            report.AppendLine("targetTransformChanged=False");
            report.AppendLine("animationOrMeshChanged=False");
            AppendMetrics(report, metrics);
            report.AppendLine("inspectionPassed=True");
            File.WriteAllText(
                Absolute(OutputFolder + "/runtime_metrics.txt"),
                report.ToString(),
                new UTF8Encoding(false));

            Debug.Log(
                "Armor_Protective_Idle live start-view inspection passed. CameraDistance=" +
                Num(metrics.CameraDistance) + ", FacingDot=" + Num(metrics.FacingDot) + ".");
        }

        [MenuItem("Bellerophon/Player/Capture Armor Protective Idle Start View")]
        internal static void CaptureArmorProtectiveIdleStartView()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Armor_Protective_Idle first-frame capture must begin in Edit Mode.");
            }

            string finalPath = Absolute(OutputFolder + "/final.png");
            if (File.Exists(finalPath))
            {
                throw new InvalidOperationException(
                    "The one final start-view capture already exists: " + finalPath);
            }

            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(
                Absolute(OutputFolder + "/capture_request.txt"),
                "Capture the unmodified first Play frame after inspection.",
                new UTF8Encoding(false));
            EditorApplication.EnterPlaymode();
            Debug.Log("Armor_Protective_Idle first-frame capture entered Play Mode.");
        }

        [MenuItem("Bellerophon/Player/Stop Armor Protective Idle Start View Review")]
        internal static void StopArmorProtectiveIdleStartViewReview()
        {
            string requestPath = Absolute(OutputFolder + "/capture_request.txt");
            if (File.Exists(requestPath))
            {
                File.Delete(requestPath);
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static void CompletePendingFirstFrameCapture()
        {
            string requestPath = Absolute(OutputFolder + "/capture_request.txt");
            if (!EditorApplication.isPlaying || !File.Exists(requestPath))
            {
                return;
            }

            EditorApplication.update -= CompletePendingFirstFrameCapture;
            string finalPath = Absolute(OutputFolder + "/final.png");
            try
            {
                if (File.Exists(finalPath))
                {
                    throw new InvalidOperationException(
                        "The one final start-view capture already exists: " + finalPath);
                }

                RequireScene();
                Transform player = FindUnique(PlayerName);
                Transform target = FindUnique(TargetName);
                Camera camera = RequirePlayerCamera(player);
                Bounds bounds = BoundsOf(target);
                StartViewMetrics metrics = Inspect(player, target, camera, bounds);

                var metricsReport = new StringBuilder();
                metricsReport.AppendLine(
                    "Read-only first-frame Armor_Protective_Idle start-view inspection.");
                metricsReport.AppendLine("playMode=True");
                metricsReport.AppendLine("target=Armor_Protective_Idle");
                metricsReport.AppendLine("targetTransformChanged=False");
                metricsReport.AppendLine("animationOrMeshChanged=False");
                AppendMetrics(metricsReport, metrics);
                metricsReport.AppendLine("inspectionPassed=True");
                File.WriteAllText(
                    Absolute(OutputFolder + "/runtime_metrics.txt"),
                    metricsReport.ToString(),
                    new UTF8Encoding(false));

                Render(camera, finalPath);
                var captureReport = new StringBuilder();
                captureReport.AppendLine(
                    "Armor_Protective_Idle one-time unmodified first-frame capture.");
                AppendMetrics(captureReport, metrics);
                File.WriteAllText(
                    Absolute(OutputFolder + "/capture.txt"),
                    captureReport.ToString(),
                    new UTF8Encoding(false));
                File.Delete(requestPath);
                Debug.Log(
                    "Armor_Protective_Idle one-time first-frame capture completed: " +
                    finalPath);
            }
            catch (Exception exception)
            {
                File.WriteAllText(
                    Absolute(OutputFolder + "/capture_failure.txt"),
                    exception.ToString(),
                    new UTF8Encoding(false));
                if (File.Exists(requestPath))
                {
                    File.Delete(requestPath);
                }
                Debug.LogException(exception);
            }
            finally
            {
                EditorApplication.delayCall += () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        EditorApplication.ExitPlaymode();
                    }
                };
            }
        }

        private static StartViewMetrics Inspect(
            Transform player,
            Transform target,
            Camera camera,
            Bounds bounds)
        {
            Vector3 front = Horizontal(target.forward);
            Vector3 centerToCamera = Vector3.ProjectOnPlane(
                camera.transform.position - bounds.center,
                Vector3.up);
            float cameraDistance = centerToCamera.magnitude;
            float frontAxisDot = Vector3.Dot(centerToCamera.normalized, front);
            Vector3 cameraForward = Horizontal(camera.transform.forward);
            Vector3 cameraToCenter = Horizontal(bounds.center - camera.transform.position);
            float facingDot = Vector3.Dot(cameraForward, cameraToCenter);
            Vector3 centerViewport = camera.WorldToViewportPoint(bounds.center);
            float playerHeight = player.position.y;

            if (Mathf.Abs(cameraDistance - ReviewDistanceMeters) > PositionToleranceMeters)
            {
                throw new InvalidOperationException(
                    "Player camera distance differs from the established two-meter start view. " +
                    "Distance=" + Num(cameraDistance) + ".");
            }
            if (frontAxisDot < FacingDotMinimum)
            {
                throw new InvalidOperationException(
                    "Player camera is not on the Armor_Protective_Idle front axis. Dot=" +
                    Num(frontAxisDot) + ".");
            }
            if (facingDot < FacingDotMinimum)
            {
                throw new InvalidOperationException(
                    "Player camera is not facing Armor_Protective_Idle. Dot=" +
                    Num(facingDot) + ".");
            }
            if (centerViewport.z <= camera.nearClipPlane ||
                centerViewport.x < 0.15f || centerViewport.x > 0.85f ||
                centerViewport.y < 0.15f || centerViewport.y > 0.85f)
            {
                throw new InvalidOperationException(
                    "Armor_Protective_Idle is not centered in the player camera. Viewport=" +
                    Vec(centerViewport) + ".");
            }

            return new StartViewMetrics(
                cameraDistance,
                frontAxisDot,
                facingDot,
                centerViewport,
                playerHeight);
        }

        private static void AppendMetrics(StringBuilder report, StartViewMetrics metrics)
        {
            report.AppendLine("cameraHorizontalDistance=" + Num(metrics.CameraDistance));
            report.AppendLine("cameraFrontAxisDot=" + Num(metrics.FrontAxisDot));
            report.AppendLine("cameraFacingDot=" + Num(metrics.FacingDot));
            report.AppendLine("targetCenterViewport=" + Vec(metrics.CenterViewport));
            report.AppendLine("playerStartHeight=" + Num(metrics.PlayerHeight));
        }

        private static void Render(Camera camera, string destination)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(
                CaptureWidth,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. Active=" + scene.path + ".");
            }

            return scene;
        }

        private static Transform FindUnique(string name)
        {
            Transform[] matches = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(name + " count=" + matches.Length + ".");
            }

            return matches[0];
        }

        private static Camera RequirePlayerCamera(Transform player)
        {
            Camera[] cameras = player.GetComponentsInChildren<Camera>(true);
            if (cameras.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player must contain exactly one camera. Count=" + cameras.Length + ".");
            }

            return cameras[0];
        }

        private static Bounds BoundsOf(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Armor_Protective_Idle has no enabled renderer for start framing.");
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static Vector3 Horizontal(Vector3 value)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(value, Vector3.up);
            return horizontal.sqrMagnitude < 0.000001f
                ? Vector3.zero
                : horizontal.normalized;
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable."),
                projectRelativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " + Num(value.z) + ")";
        }

        private readonly struct StartViewMetrics
        {
            internal StartViewMetrics(
                float cameraDistance,
                float frontAxisDot,
                float facingDot,
                Vector3 centerViewport,
                float playerHeight)
            {
                CameraDistance = cameraDistance;
                FrontAxisDot = frontAxisDot;
                FacingDot = facingDot;
                CenterViewport = centerViewport;
                PlayerHeight = playerHeight;
            }

            internal float CameraDistance { get; }
            internal float FrontAxisDot { get; }
            internal float FacingDot { get; }
            internal Vector3 CenterViewport { get; }
            internal float PlayerHeight { get; }
        }
    }
}
