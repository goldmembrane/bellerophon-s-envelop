using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class PlayerWalkForwardPlayModeReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LayoutRootName = "PlayerAnimationLayout";
        private const string WalkKey = "Player_Walk_Forward";
        private const string AnimatorStateName = "PlayerWalkForward";
        private const string ReviewCameraName = "PlayerWalkForwardPlayModeReviewCamera";
        private const int CaptureWidth = 640;
        private const int CaptureHeight = 720;
        private const int CaptureFrameRate = 30;
        private const int CaptureLoopCount = 2;
        private const string SessionSummaryKey =
            "Bellerophon.PlayerWalkForwardPlayModeReview.Summary";
        private const string SessionFrameCountKey =
            "Bellerophon.PlayerWalkForwardPlayModeReview.FrameCount";

        private static Camera reviewCamera;
        private static RendererState[] rendererStates;
        private static Transform target;
        private static Transform hips;
        private static Transform head;
        private static Transform leftUpLeg;
        private static Transform rightUpLeg;
        private static Vector3 rootStartPosition;
        private static int sampleCount;
        private static float minimumPelvisLateral;
        private static float maximumPelvisLateral;
        private static float minimumPelvisRoll;
        private static float maximumPelvisRoll;
        private static float maximumAbsolutePelvisRoll;
        private static float minimumTorsoLean;
        private static float maximumTorsoLean;
        private static float maximumAbsoluteTorsoLean;
        private static float maximumRootDisplacement;

        public static void CaptureReferenceMatchReview()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseString(SessionSummaryKey);
                SessionState.EraseInt(SessionFrameCountKey);
                EnterPlayMode();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Player walk reference review is waiting for Play Mode.");
            }

            var summary = SessionState.GetString(SessionSummaryKey, string.Empty);
            if (string.IsNullOrWhiteSpace(summary))
            {
                Prepare();
                return;
            }

            Finish();
            ExitPlayMode();
        }

        public static void CaptureMeshyWalkingReview()
        {
            CaptureReferenceMatchReview();
        }

        public static void EnterPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player walk review must start from Edit Mode.");
            }

            var scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Player walk Play Mode review.");
            }

            EditorApplication.EnterPlaymode();
        }

        public static void Prepare()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Player walk review preparation requires Play Mode.");
            }

            CleanupCamera();
            target = RequireWalkInstance();
            var animator = target.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "Player_Walk_Forward Animator is missing in Play Mode.");
            if (animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward Animator Controller is missing in Play Mode.");
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(AnimatorStateName, 0, 0f);
            animator.Update(0f);

            hips = RequireNamedBone(target, "Hips");
            head = RequireNamedBone(target, "Head");
            leftUpLeg = RequireNamedBone(target, "LeftUpLeg");
            rightUpLeg = RequireNamedBone(target, "RightUpLeg");
            IsolateTargetRenderers(target);
            ConfigureReviewCamera(target);
            FocusGameView();

            rootStartPosition = target.position;
            ResetMetrics();
            var summary = CaptureRuntimeSequence(animator);
            SessionState.SetString(SessionSummaryKey, summary);
            SessionState.SetInt(SessionFrameCountKey, sampleCount);

            animator.speed = 1f;
            animator.Play(AnimatorStateName, 0, 0f);
            animator.Update(0f);
            Debug.Log(summary);
        }

        public static void Finish()
        {
            var summary = SessionState.GetString(SessionSummaryKey, string.Empty);
            var expectedFrameCount = SessionState.GetInt(SessionFrameCountKey, 0);
            if (string.IsNullOrWhiteSpace(summary) || expectedFrameCount <= 0)
            {
                throw new InvalidOperationException(
                    "Player walk Play Mode review has no completed runtime capture.");
            }

            var actualFrameCount = Directory.Exists(RuntimeFrameDirectory)
                ? Directory.GetFiles(RuntimeFrameDirectory, "frame_*.png").Length
                : 0;
            if (actualFrameCount != expectedFrameCount)
            {
                throw new InvalidOperationException(
                    "Player walk runtime frame count differs. Expected=" +
                    expectedFrameCount.ToString(CultureInfo.InvariantCulture) +
                    ", Actual=" +
                    actualFrameCount.ToString(CultureInfo.InvariantCulture) + ".");
            }

            Debug.Log(summary + " VerifiedFrameFiles=" +
                      actualFrameCount.ToString(CultureInfo.InvariantCulture) + ".");
            CleanupCamera();
        }

        public static void ExitPlayMode()
        {
            CleanupCamera();
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static string CaptureRuntimeSequence(Animator animator)
        {
            animator.speed = 1f;
            animator.Play(AnimatorStateName, 0, 0f);
            animator.Update(0f);
            var clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
            if (float.IsNaN(clipLength) || float.IsInfinity(clipLength) ||
                clipLength <= 0.01f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward runtime state length is invalid.");
            }

            animator.speed = 0f;

            Directory.CreateDirectory(RuntimeFrameDirectory);
            foreach (var oldFrame in Directory.GetFiles(
                         RuntimeFrameDirectory,
                         "frame_*.png"))
            {
                File.Delete(oldFrame);
            }

            var frameCount = Mathf.CeilToInt(
                clipLength * CaptureLoopCount * CaptureFrameRate);
            var renderTexture = new RenderTexture(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2
            };
            var texture = new Texture2D(
                CaptureWidth,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            var previousActive = RenderTexture.active;
            var previousTarget = reviewCamera.targetTexture;
            try
            {
                reviewCamera.targetTexture = renderTexture;
                reviewCamera.aspect = CaptureWidth / (float)CaptureHeight;
                for (var frame = 0; frame < frameCount; frame++)
                {
                    var elapsed = frame / (float)CaptureFrameRate;
                    animator.Play(AnimatorStateName, 0, elapsed / clipLength);
                    animator.Update(0f);
                    UpdateMetrics();
                    reviewCamera.Render();
                    RenderTexture.active = renderTexture;
                    texture.ReadPixels(
                        new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                        0,
                        0,
                        false);
                    texture.Apply(false, false);
                    File.WriteAllBytes(
                        Path.Combine(
                            RuntimeFrameDirectory,
                            "frame_" + frame.ToString("D4", CultureInfo.InvariantCulture) +
                            ".png"),
                        texture.EncodeToPNG());
                }
            }
            finally
            {
                reviewCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }

            var duration = frameCount / (float)CaptureFrameRate;
            return
                "PlayerWalkForward actual Play Mode runtime review captured." +
                " Duration=" + Num(duration) +
                ", FrameRate=" + CaptureFrameRate.ToString(
                    CultureInfo.InvariantCulture) +
                ", Frames=" + frameCount.ToString(CultureInfo.InvariantCulture) +
                ", Loops=" + CaptureLoopCount.ToString(CultureInfo.InvariantCulture) +
                ", PelvisLateralTravel=" +
                Num(maximumPelvisLateral - minimumPelvisLateral) +
                ", PelvisRollRange=" +
                Num(maximumPelvisRoll - minimumPelvisRoll) +
                ", MaximumPelvisRoll=" + Num(maximumAbsolutePelvisRoll) +
                ", TorsoLeanRange=" +
                Num(maximumTorsoLean - minimumTorsoLean) +
                ", MaximumTorsoLean=" + Num(maximumAbsoluteTorsoLean) +
                ", MaximumRootDisplacement=" + Num(maximumRootDisplacement) +
                ", Target=" + WalkKey +
                ", FrontCamera=True" +
                ", ActualPlayMode=True" +
                ", AnimatorPlayback=True" +
                ", ApplyRootMotion=False.";
        }

        private static void ResetMetrics()
        {
            sampleCount = 0;
            minimumPelvisLateral = float.PositiveInfinity;
            maximumPelvisLateral = float.NegativeInfinity;
            minimumPelvisRoll = float.PositiveInfinity;
            maximumPelvisRoll = float.NegativeInfinity;
            maximumAbsolutePelvisRoll = 0f;
            minimumTorsoLean = float.PositiveInfinity;
            maximumTorsoLean = float.NegativeInfinity;
            maximumAbsoluteTorsoLean = 0f;
            maximumRootDisplacement = 0f;
        }

        private static void UpdateMetrics()
        {
            if (target == null || hips == null || head == null ||
                leftUpLeg == null || rightUpLeg == null)
            {
                throw new InvalidOperationException(
                    "Player walk review target hierarchy was lost during Play Mode.");
            }

            var pelvisLateral = Vector3.Dot(
                hips.position - target.position,
                target.right);
            var pelvisRoll = PelvisRollDegrees();
            var centerLine = head.position - hips.position;
            var torsoLean = Mathf.Atan2(
                                Vector3.Dot(centerLine, target.right),
                                Vector3.Dot(centerLine, target.up)) *
                            Mathf.Rad2Deg;
            minimumPelvisLateral = Mathf.Min(minimumPelvisLateral, pelvisLateral);
            maximumPelvisLateral = Mathf.Max(maximumPelvisLateral, pelvisLateral);
            minimumPelvisRoll = Mathf.Min(minimumPelvisRoll, pelvisRoll);
            maximumPelvisRoll = Mathf.Max(maximumPelvisRoll, pelvisRoll);
            maximumAbsolutePelvisRoll = Mathf.Max(
                maximumAbsolutePelvisRoll,
                Mathf.Abs(pelvisRoll));
            minimumTorsoLean = Mathf.Min(minimumTorsoLean, torsoLean);
            maximumTorsoLean = Mathf.Max(maximumTorsoLean, torsoLean);
            maximumAbsoluteTorsoLean = Mathf.Max(
                maximumAbsoluteTorsoLean,
                Mathf.Abs(torsoLean));
            maximumRootDisplacement = Mathf.Max(
                maximumRootDisplacement,
                Vector3.Distance(target.position, rootStartPosition));
            sampleCount++;
        }

        private static float PelvisRollDegrees()
        {
            var hipLine = rightUpLeg.position - leftUpLeg.position;
            if (Vector3.Dot(hipLine, target.right) < 0f)
            {
                hipLine = -hipLine;
            }

            var horizontal = Vector3.Dot(hipLine, target.right);
            if (Mathf.Abs(horizontal) <= 0.0001f)
            {
                throw new InvalidOperationException(
                    "Player walk review pelvis line is invalid.");
            }

            return Mathf.Atan2(
                       Vector3.Dot(hipLine, target.up),
                       horizontal) *
                   Mathf.Rad2Deg;
        }

        private static void ConfigureReviewCamera(Transform walkInstance)
        {
            var renderers = walkInstance
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward has no enabled renderer in Play Mode.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            var front = Vector3.ProjectOnPlane(walkInstance.forward, Vector3.up).normalized;
            if (front.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward has no usable front direction.");
            }

            var cameraObject = new GameObject(ReviewCameraName, typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            reviewCamera = cameraObject.GetComponent<Camera>();
            reviewCamera.depth = 1000f;
            reviewCamera.clearFlags = CameraClearFlags.SolidColor;
            reviewCamera.backgroundColor = Color.black;
            reviewCamera.orthographic = true;
            reviewCamera.allowHDR = false;
            reviewCamera.allowMSAA = true;
            reviewCamera.aspect = CaptureWidth / (float)CaptureHeight;
            reviewCamera.nearClipPlane = 0.01f;
            reviewCamera.farClipPlane = 100f;
            reviewCamera.transform.position = bounds.center + front * 8f;
            reviewCamera.transform.LookAt(bounds.center, Vector3.up);
            var verticalExtent = ProjectedHalfExtent(
                bounds.extents,
                reviewCamera.transform.up);
            var horizontalExtent = ProjectedHalfExtent(
                bounds.extents,
                reviewCamera.transform.right);
            var aspect = Mathf.Max(1f, reviewCamera.aspect);
            reviewCamera.orthographicSize = Mathf.Max(
                verticalExtent * 1.08f,
                horizontalExtent / aspect * 1.08f,
                0.5f);
        }

        private static float ProjectedHalfExtent(Vector3 extents, Vector3 axis)
        {
            axis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
            return Vector3.Dot(extents, axis);
        }

        private static void FocusGameView()
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
            {
                throw new InvalidOperationException("Unity Game View type is unavailable.");
            }

            var gameView = EditorWindow.GetWindow(gameViewType);
            gameView.Show();
            gameView.Focus();
        }

        private static Transform RequireWalkInstance()
        {
            var scene = RequireScene();
            var rootMatches = scene.GetRootGameObjects()
                .Where(root => root.name == LayoutRootName)
                .ToArray();
            if (rootMatches.Length != 1)
            {
                throw new InvalidOperationException(
                    "PlayerAnimationLayout root count differs in Play Mode.");
            }

            var matches = Enumerable.Range(0, rootMatches[0].transform.childCount)
                .Select(rootMatches[0].transform.GetChild)
                .Where(child => child.name == WalkKey)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward instance count differs in Play Mode.");
            }

            return matches[0];
        }

        private static Transform RequireNamedBone(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player walk review bone count differs: " + name + ".");
            }

            return matches[0];
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene for Player walk review.");
            }

            return scene;
        }

        private static void CleanupCamera()
        {
            if (reviewCamera != null)
            {
                UnityEngine.Object.DestroyImmediate(reviewCamera.gameObject);
            }

            foreach (var staleCamera in Resources.FindObjectsOfTypeAll<Camera>()
                         .Where(camera => camera != null &&
                                          camera.name == ReviewCameraName)
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(staleCamera.gameObject);
            }

            reviewCamera = null;
            if (rendererStates != null)
            {
                foreach (var state in rendererStates)
                {
                    state.Restore();
                }

                rendererStates = null;
            }
        }

        private static void IsolateTargetRenderers(Transform reviewTarget)
        {
            if (rendererStates != null)
            {
                foreach (var state in rendererStates)
                {
                    state.Restore();
                }
            }

            var targetRenderers = reviewTarget
                .GetComponentsInChildren<Renderer>(true)
                .ToHashSet();
            rendererStates = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(renderer => !targetRenderers.Contains(renderer))
                .Select(renderer => new RendererState(renderer))
                .ToArray();
            foreach (var state in rendererStates)
            {
                state.Hide();
            }
        }

        private static string RuntimeFrameDirectory => Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ??
            throw new InvalidOperationException("Unity project root is unavailable."),
            "Logs",
            "PlayerWalkForwardPlayModeReviewFrames");

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Hide()
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            public void Restore()
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

    }
}
