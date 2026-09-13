using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class LightsaberOffIdleStartSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlayerName = "Player";
        private const string TargetName = "Lightsaber_Off_Idle";
        private const string ReviewFolder = "Assets/_Project/Animation/Lightsaber/Review";
        private const string FinalImagePath = ReviewFolder + "/LightsaberOffIdleStartView_Final.png";
        private const float MinimumDistanceMeters = 2.5f;
        private const float MaximumDistanceMeters = 8f;
        private const float DistanceStepMeters = 0.25f;
        private const float ViewportMargin = 0.05f;
        private const float PositionTolerance = 0.002f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        [MenuItem("Bellerophon/Player/Apply Lightsaber Off Idle Start View")]
        internal static void ApplyLightsaberOffIdleStartView()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, TargetName);
            Camera camera = RequirePlayerCamera(player);

            string outsidePlayerBefore = SceneSignature(scene, player);
            string playerChildrenBefore = PlayerChildrenSignature(player);
            Vector3 playerScaleBefore = player.localScale;
            Vector3 targetPositionBefore = target.position;
            Quaternion targetRotationBefore = target.rotation;
            Vector3 targetScaleBefore = target.localScale;

            Bounds targetBounds = BoundsOf(target);
            Vector3 targetFront = HorizontalDirection(target.forward, TargetName + " forward");
            Vector3 cameraLocalOffset = Quaternion.Inverse(player.rotation) *
                (camera.transform.position - player.position);
            Vector3 cameraForwardInPlayerSpace = Quaternion.Inverse(player.rotation) *
                camera.transform.forward;
            Vector3 localCameraHeading = HorizontalDirection(
                cameraForwardInPlayerSpace,
                "player camera forward");
            Vector3 desiredCameraHeading = -targetFront;
            float desiredCameraYaw = Mathf.Atan2(
                desiredCameraHeading.x,
                desiredCameraHeading.z) * Mathf.Rad2Deg;
            float localCameraYaw = Mathf.Atan2(
                localCameraHeading.x,
                localCameraHeading.z) * Mathf.Rad2Deg;
            Quaternion desiredPlayerRotation = Quaternion.Euler(
                0f,
                desiredCameraYaw - localCameraYaw,
                0f);

            Vector3 chosenPosition = default;
            float chosenDistance = 0f;
            bool found = false;
            Undo.RecordObject(player, "Place startup view before Lightsaber_Off_Idle");
            for (float distance = MinimumDistanceMeters;
                 distance <= MaximumDistanceMeters + 0.0001f;
                 distance += DistanceStepMeters)
            {
                Vector3 desiredCameraPosition = targetBounds.center + targetFront * distance;
                Vector3 candidatePosition = desiredCameraPosition -
                    desiredPlayerRotation * cameraLocalOffset;
                candidatePosition.y = player.position.y;
                player.SetPositionAndRotation(candidatePosition, desiredPlayerRotation);

                if (!BoundsFullyVisible(camera, targetBounds))
                    continue;
                if (FacingDot(camera, targetBounds.center) < 0.97f)
                    continue;

                chosenPosition = candidatePosition;
                chosenDistance = distance;
                found = true;
                break;
            }

            if (!found)
                throw new InvalidOperationException(
                    "No 2.5-8m startup position preserves the camera while showing the full " +
                    TargetName + " target from its front axis.");

            player.SetPositionAndRotation(chosenPosition, desiredPlayerRotation);
            EditorUtility.SetDirty(player.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);

            RequireEqual(
                outsidePlayerBefore,
                SceneSignature(scene, player),
                "scene objects outside Player");
            RequireEqual(
                playerChildrenBefore,
                PlayerChildrenSignature(player),
                "Player children and camera settings");
            RequireNear(player.localScale, playerScaleBefore, "Player scale");
            RequireNear(target.position, targetPositionBefore, TargetName + " position");
            RequireNear(target.rotation, targetRotationBefore, TargetName + " rotation");
            RequireNear(target.localScale, targetScaleBefore, TargetName + " scale");
            RequireStartView(player, target, camera);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            RequireNoUnityConsoleErrors();
            Debug.Log(
                "[LightsaberOffIdleStart] Startup view saved. Camera distance=" +
                Num(chosenDistance) + "m.");
        }

        [MenuItem("Bellerophon/Player/Inspect Lightsaber Off Idle Start View")]
        internal static void InspectLightsaberOffIdleStartView()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, TargetName);
            Camera camera = RequirePlayerCamera(player);
            RequireStartView(player, target, camera);
            RequireNoUnityConsoleErrors();
            Debug.Log("[LightsaberOffIdleStart] Read-only startup view inspection passed.");
        }

        [MenuItem("Bellerophon/Player/Capture Lightsaber Off Idle Start View Final")]
        internal static void CaptureLightsaberOffIdleStartViewFinal()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform player = FindUnique(scene, PlayerName);
            Transform target = FindUnique(scene, TargetName);
            Camera camera = RequirePlayerCamera(player);
            RequireStartView(player, target, camera);
            RequireNoUnityConsoleErrors();
            if (File.Exists(Absolute(FinalImagePath)))
                throw new InvalidOperationException(
                    "The one final startup-view capture already exists: " + FinalImagePath);

            EnsureFolder(ReviewFolder);
            Render(camera, Absolute(FinalImagePath));
            AssetDatabase.ImportAsset(
                FinalImagePath,
                ImportAssetOptions.ForceSynchronousImport);
            RequireNoUnityConsoleErrors();
            Debug.Log("[LightsaberOffIdleStart] One final startup-view capture completed.");
        }

        private static void RequireStartView(
            Transform player,
            Transform target,
            Camera camera)
        {
            Bounds bounds = BoundsOf(target);
            Vector3 targetFront = HorizontalDirection(target.forward, TargetName + " forward");
            Vector3 targetToCamera = HorizontalDirection(
                camera.transform.position - bounds.center,
                "target-to-camera direction");
            float frontAxisDot = Vector3.Dot(targetToCamera, targetFront);
            if (frontAxisDot < 0.995f)
                throw new InvalidOperationException(
                    "Startup camera is not on the target front axis. Dot=" +
                    Num(frontAxisDot) + ".");

            float horizontalDistance = Vector3.Distance(
                Vector3.ProjectOnPlane(camera.transform.position, Vector3.up),
                Vector3.ProjectOnPlane(bounds.center, Vector3.up));
            if (horizontalDistance < MinimumDistanceMeters - PositionTolerance ||
                horizontalDistance > MaximumDistanceMeters + PositionTolerance)
                throw new InvalidOperationException(
                    "Startup camera distance is outside 2.5-8m. Actual=" +
                    Num(horizontalDistance) + "m.");
            if (!BoundsFullyVisible(camera, bounds))
                throw new InvalidOperationException(
                    TargetName + " full renderer bounds are not visible in the startup camera.");

            float facingDot = FacingDot(camera, bounds.center);
            if (facingDot < 0.97f)
                throw new InvalidOperationException(
                    "Startup camera is not facing the target. Dot=" + Num(facingDot) + ".");
            if (Mathf.Abs(player.position.y) > 0.001f)
                throw new InvalidOperationException(
                    "Player root height changed unexpectedly. Y=" + Num(player.position.y) + ".");
        }

        private static bool BoundsFullyVisible(Camera camera, Bounds bounds)
        {
            foreach (Vector3 corner in BoundsCorners(bounds))
            {
                Vector3 viewport = camera.WorldToViewportPoint(corner);
                if (viewport.z <= camera.nearClipPlane ||
                    viewport.x < ViewportMargin || viewport.x > 1f - ViewportMargin ||
                    viewport.y < ViewportMargin || viewport.y > 1f - ViewportMargin)
                    return false;
            }
            return true;
        }

        private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
                yield return new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
        }

        private static float FacingDot(Camera camera, Vector3 targetCenter)
        {
            Vector3 toTarget = (targetCenter - camera.transform.position).normalized;
            return Vector3.Dot(camera.transform.forward, toTarget);
        }

        private static Vector3 HorizontalDirection(Vector3 value, string label)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(value, Vector3.up);
            if (horizontal.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(label + " has no horizontal direction.");
            return horizontal.normalized;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene. Actual=" + scene.path + ".");
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Lightsaber_Off_Idle startup setup requires Edit Mode.");
        }

        private static Transform FindUnique(Scene scene, string name)
        {
            Transform[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(name + " count=" + matches.Length + ".");
            return matches[0];
        }

        private static Camera RequirePlayerCamera(Transform player)
        {
            Camera[] cameras = player.GetComponentsInChildren<Camera>(true);
            if (cameras.Length != 1)
                throw new InvalidOperationException(
                    "Player must contain exactly one camera. Count=" + cameras.Length + ".");
            return cameras[0];
        }

        private static Bounds BoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static string SceneSignature(Scene scene, Transform ignoredRoot)
        {
            var lines = new List<string>();
            foreach (Transform item in scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                if (item == ignoredRoot || item.IsChildOf(ignoredRoot))
                    continue;
                lines.Add(TransformLine(item));
            }
            lines.Sort(StringComparer.Ordinal);
            return Sha256Text(string.Join("\n", lines));
        }

        private static string PlayerChildrenSignature(Transform player)
        {
            var lines = new List<string>();
            foreach (Transform item in player.GetComponentsInChildren<Transform>(true))
            {
                if (item == player)
                    continue;
                lines.Add(TransformLine(item));
                Camera camera = item.GetComponent<Camera>();
                if (camera != null)
                    lines.Add(
                        HierarchyPath(item) + "|CAMERA|" + Num(camera.fieldOfView) + "|" +
                        Num(camera.nearClipPlane) + "|" + Num(camera.farClipPlane) + "|" +
                        camera.orthographic + "|" + Num(camera.orthographicSize));
            }
            lines.Sort(StringComparer.Ordinal);
            return Sha256Text(string.Join("\n", lines));
        }

        private static string TransformLine(Transform item)
        {
            return HierarchyPath(item) + "|" + Vec(item.localPosition) + "|" +
                Quat(item.localRotation) + "|" + Vec(item.localScale) + "|" +
                item.gameObject.activeSelf;
        }

        private static string HierarchyPath(Transform item)
        {
            var names = new Stack<string>();
            for (Transform current = item; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
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
            var image = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
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

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot(), projectRelativePath));
        }

        private static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
        }

        private static string Sha256Text(string text)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void RequireNear(Vector3 actual, Vector3 expected, string label)
        {
            if (Vector3.Distance(actual, expected) > 0.000001f)
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void RequireNear(Quaternion actual, Quaternion expected, string label)
        {
            if (Quaternion.Angle(actual, expected) > 0.0001f)
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void RequireNoUnityConsoleErrors()
        {
            Type type = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            var method = type.GetMethod(
                "GetCountsByType",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console count API is unavailable.");
            object[] arguments = { 0, 0, 0 };
            method.Invoke(null, arguments);
            if ((int)arguments[0] != 0)
                throw new InvalidOperationException(
                    "Unity console contains " + arguments[0] + " error(s).");
        }

        private static string Num(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " + Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + ", " + Num(value.y) + ", " +
                Num(value.z) + ", " + Num(value.w) + ")";
        }
    }
}
