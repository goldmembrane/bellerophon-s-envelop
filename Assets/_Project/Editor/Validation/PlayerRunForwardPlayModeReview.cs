using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerRunForwardPlayModeReview
    {
        private const string CapturedSessionKey =
            "Bellerophon.PlayerRunForwardReview.Captured";
        private const string FramesDirectory =
            "Logs/PlayerRunForwardPlayModeReviewFrames";
        private const string MetricsPath =
            "docs/validation/player_run_forward_running_review_metrics.json";
        private const string ReviewContactSheetPath =
            "docs/validation/player_run_forward_running_review_contact_sheet.png";
        private const string FinalCapturePath =
            "docs/validation/player_run_forward_running_final.png";
        private const int CaptureWidth = 640;
        private const int CaptureHeight = 720;
        private const int LoopCount = 2;
        private const float RootPositionTolerance = 0.001f;
        private const float LoopPositionTolerance = 0.0001f;
        private const float LoopRotationTolerance = 0.01f;
        private const float MinimumFootTravel = 0.05f;

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string target;
            public string state;
            public string sourceTake;
            public float clipDurationSeconds;
            public float clipFrameRate;
            public int framesPerLoop;
            public int framesCaptured;
            public int loopsCaptured;
            public float rootPositionDisplacementMax;
            public float hipsHorizontalDisplacementMax;
            public float leftFootTravel;
            public float rightFootTravel;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public bool clipIsLooping;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class Pose
        {
            internal readonly Dictionary<string, Vector3> Positions =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Quaternion> Rotations =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            internal RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            internal void Hide()
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            internal void Restore()
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        [MenuItem("Bellerophon/Player/Capture Run Forward Review")]
        internal static void CaptureReview()
        {
            if (!EditorApplication.isPlaying)
            {
                Scene scene = RequireScene();
                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp must be clean before Player_Run_Forward review.");
                }

                SessionState.SetBool(CapturedSessionKey, false);
                EditorApplication.EnterPlaymode();
                Debug.Log(
                    "[PlayerRunForward] Entering Play Mode for direct two-loop review.");
                return;
            }

            if (!SessionState.GetBool(CapturedSessionKey, false))
            {
                try
                {
                    CaptureTwoLoops();
                    SessionState.SetBool(CapturedSessionKey, true);
                }
                catch
                {
                    SessionState.EraseBool(CapturedSessionKey);
                    EditorApplication.ExitPlaymode();
                    throw;
                }

                return;
            }

            SessionState.EraseBool(CapturedSessionKey);
            EditorApplication.ExitPlaymode();
            Debug.Log(
                "[PlayerRunForward] Exiting Play Mode after direct two-loop review.");
        }

        [MenuItem("Bellerophon/Player/Capture Run Forward Final")]
        internal static void CaptureFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before final capture.");
            }

            string absoluteMetricsPath = Path.GetFullPath(MetricsPath);
            if (!File.Exists(absoluteMetricsPath))
            {
                throw new FileNotFoundException(
                    "Player_Run_Forward review metrics are missing.",
                    absoluteMetricsPath);
            }

            ReviewMetrics metrics = JsonUtility.FromJson<ReviewMetrics>(
                File.ReadAllText(absoluteMetricsPath, Encoding.UTF8));
            if (metrics == null || !metrics.passedNumericChecks ||
                metrics.framesPerLoop < 2 || metrics.loopsCaptured != LoopCount)
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward review did not pass before final capture.");
            }

            int[] indices = FinalFrameIndices(metrics.framesPerLoop);
            Dictionary<int, byte[]> frames = LoadFrames(indices);
            ComposeCapture(frames, indices, FinalCapturePath);
            Debug.Log(
                "[PlayerRunForward] Final composed from directly reviewed running frames." +
                " Output=" + Path.GetFullPath(FinalCapturePath) +
                ", SourceTake=" + metrics.sourceTake +
                ", FramesPerLoop=" + metrics.framesPerLoop.ToString(
                    CultureInfo.InvariantCulture) +
                ", Loops=" + metrics.loopsCaptured.ToString(
                    CultureInfo.InvariantCulture) +
                ", SceneChanged=False.");
        }

        private static void CaptureTwoLoops()
        {
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            Transform target = PlayerRunForwardAnimationTool.RequireTarget(scene);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    "Player_Run_Forward Animator is missing.");
            if (animator.runtimeAnimatorController == null ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward Animator connection or Apply Root Motion differs.");
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    clips[0].name,
                    PlayerRunForwardAnimationTool.ExpectedTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward does not reference the investigated running Take.");
            }

            AnimationClip clip = clips[0];
            if (!clip.isLooping || clip.frameRate <= 0f || clip.length <= 0f)
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward running clip timing or Loop Time is invalid.");
            }

            int framesPerLoop = Mathf.RoundToInt(clip.length * clip.frameRate);
            if (framesPerLoop < 2)
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward running clip has too few frames.");
            }

            Transform hips = PlayerRunForwardAnimationTool.FindUniqueBone(
                target,
                "Hips");
            Transform leftFoot = PlayerRunForwardAnimationTool.FindUniqueBone(
                target,
                "LeftFoot");
            Transform rightFoot = PlayerRunForwardAnimationTool.FindUniqueBone(
                target,
                "RightFoot");
            Renderer[] targetRenderers = target
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (targetRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward has no enabled renderer.");
            }

            AnimatorCullingMode originalCullingMode = animator.cullingMode;
            float originalSpeed = animator.speed;
            RendererState[] hiddenRendererStates = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            Texture2D frameTexture = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                hiddenRendererStates = IsolateTargetRenderers(targetRenderers);
                cameraObject = new GameObject(
                    "PlayerRunForwardReviewCamera",
                    typeof(Camera));
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                Camera camera = cameraObject.GetComponent<Camera>();

                lightObject = new GameObject(
                    "PlayerRunForwardReviewLight",
                    typeof(Light));
                lightObject.hideFlags = HideFlags.HideAndDontSave;
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.transform.rotation = Quaternion.LookRotation(
                    -target.forward - target.up * 0.65f,
                    target.up);

                renderTexture = new RenderTexture(
                    CaptureWidth,
                    CaptureHeight,
                    24,
                    RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 2
                };
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                frameTexture = new Texture2D(
                    CaptureWidth,
                    CaptureHeight,
                    TextureFormat.RGB24,
                    false);

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int stateHash = Animator.StringToHash(
                    PlayerRunForwardAnimationTool.StateName);
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != stateHash)
                {
                    throw new InvalidOperationException(
                        "Player_Run_Forward running state was not entered.");
                }

                ConfigureCamera(camera, target, CalculateBounds(targetRenderers));
                string absoluteFramesDirectory = Path.GetFullPath(FramesDirectory);
                Directory.CreateDirectory(absoluteFramesDirectory);
                foreach (string oldFrame in Directory.GetFiles(
                             absoluteFramesDirectory,
                             "frame_*.png"))
                {
                    File.Delete(oldFrame);
                }

                int totalFrames = framesPerLoop * LoopCount;
                Vector3 rootBaseline = target.position;
                Vector3 hipsBaseline = hips.position;
                float rootDisplacementMax = 0f;
                float hipsHorizontalDisplacementMax = 0f;
                List<Vector3> leftFootPositions = new List<Vector3>();
                List<Vector3> rightFootPositions = new List<Vector3>();
                Pose[] poses = new Pose[totalFrames];
                int[] contactIndices = FinalFrameIndices(framesPerLoop);
                Dictionary<int, byte[]> contactFrames =
                    new Dictionary<int, byte[]>();

                for (int frame = 0; frame < totalFrames; frame++)
                {
                    animator.Play(
                        stateHash,
                        0,
                        frame / (float)framesPerLoop);
                    animator.Update(0f);

                    rootDisplacementMax = Mathf.Max(
                        rootDisplacementMax,
                        Vector3.Distance(target.position, rootBaseline));
                    hipsHorizontalDisplacementMax = Mathf.Max(
                        hipsHorizontalDisplacementMax,
                        HorizontalDistance(hips.position, hipsBaseline));
                    leftFootPositions.Add(leftFoot.position);
                    rightFootPositions.Add(rightFoot.position);
                    poses[frame] = CapturePose(target);

                    camera.Render();
                    RenderTexture.active = renderTexture;
                    frameTexture.ReadPixels(
                        new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                        0,
                        0,
                        false);
                    frameTexture.Apply(false, false);
                    byte[] png = frameTexture.EncodeToPNG();
                    File.WriteAllBytes(
                        FramePath(absoluteFramesDirectory, frame),
                        png);
                    if (contactIndices.Contains(frame))
                    {
                        contactFrames.Add(frame, png);
                    }
                }

                float loopPositionDifferenceMax = 0f;
                float loopRotationDifferenceMax = 0f;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    loopPositionDifferenceMax = Mathf.Max(
                        loopPositionDifferenceMax,
                        PosePositionDifference(
                            poses[frame],
                            poses[frame + framesPerLoop]));
                    loopRotationDifferenceMax = Mathf.Max(
                        loopRotationDifferenceMax,
                        PoseRotationDifference(
                            poses[frame],
                            poses[frame + framesPerLoop]));
                }

                float leftFootTravel = PositionRange(leftFootPositions);
                float rightFootTravel = PositionRange(rightFootPositions);
                bool passedNumericChecks =
                    rootDisplacementMax <= RootPositionTolerance &&
                    loopPositionDifferenceMax <= LoopPositionTolerance &&
                    loopRotationDifferenceMax <= LoopRotationTolerance &&
                    leftFootTravel >= MinimumFootTravel &&
                    rightFootTravel >= MinimumFootTravel &&
                    clip.isLooping &&
                    !animator.applyRootMotion;
                ReviewMetrics metrics = new ReviewMetrics
                {
                    target = PlayerRunForwardAnimationTool.TargetName,
                    state = PlayerRunForwardAnimationTool.StateName,
                    sourceTake = PlayerRunForwardAnimationTool.ExpectedTakeName,
                    clipDurationSeconds = clip.length,
                    clipFrameRate = clip.frameRate,
                    framesPerLoop = framesPerLoop,
                    framesCaptured = totalFrames,
                    loopsCaptured = LoopCount,
                    rootPositionDisplacementMax = rootDisplacementMax,
                    hipsHorizontalDisplacementMax = hipsHorizontalDisplacementMax,
                    leftFootTravel = leftFootTravel,
                    rightFootTravel = rightFootTravel,
                    loopPositionDifferenceMax = loopPositionDifferenceMax,
                    loopRotationDifferenceDegreesMax = loopRotationDifferenceMax,
                    clipIsLooping = clip.isLooping,
                    applyRootMotion = animator.applyRootMotion,
                    passedNumericChecks = passedNumericChecks,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, " +
                        "2순위 수치·스크립트 보조 검증"
                };

                string absoluteMetricsPath = Path.GetFullPath(MetricsPath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absoluteMetricsPath) ??
                    throw new InvalidOperationException(
                        "Player run review metrics directory is unavailable."));
                File.WriteAllText(
                    absoluteMetricsPath,
                    JsonUtility.ToJson(metrics, true),
                    new UTF8Encoding(false));
                if (!passedNumericChecks)
                {
                    throw new InvalidOperationException(
                        "Player_Run_Forward numeric review failed. " +
                        "Root=" + Num(rootDisplacementMax) +
                        ", LoopPosition=" + Num(loopPositionDifferenceMax) +
                        ", LoopRotation=" + Num(loopRotationDifferenceMax) +
                        ", LeftFoot=" + Num(leftFootTravel) +
                        ", RightFoot=" + Num(rightFootTravel) + ".");
                }

                ComposeCapture(
                    contactFrames,
                    contactIndices,
                    ReviewContactSheetPath);
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException(
                        "Player_Run_Forward review changed the scene dirty state.");
                }

                Debug.Log(
                    "[PlayerRunForward] Captured investigated running Take." +
                    " Take=" + clip.name +
                    ", Duration=" + Num(clip.length) +
                    ", FrameRate=" + Num(clip.frameRate) +
                    ", FramesPerLoop=" + framesPerLoop.ToString(
                        CultureInfo.InvariantCulture) +
                    ", Frames=" + totalFrames.ToString(
                        CultureInfo.InvariantCulture) +
                    ", Loops=" + LoopCount.ToString(
                        CultureInfo.InvariantCulture) +
                    ", RootDisplacement=" + Num(rootDisplacementMax) +
                    ", HipsHorizontalDisplacement=" +
                    Num(hipsHorizontalDisplacementMax) +
                    ", LoopPositionDifference=" +
                    Num(loopPositionDifferenceMax) +
                    ", LoopRotationDifference=" +
                    Num(loopRotationDifferenceMax) +
                    ", ApplyRootMotion=False.");
            }
            finally
            {
                animator.cullingMode = originalCullingMode;
                animator.speed = originalSpeed;
                if (hiddenRendererStates != null)
                {
                    foreach (RendererState rendererState in hiddenRendererStates)
                    {
                        rendererState.Restore();
                    }
                }

                RenderTexture.active = previousActive;
                if (cameraObject != null)
                {
                    Camera camera = cameraObject.GetComponent<Camera>();
                    if (camera != null)
                    {
                        camera.targetTexture = null;
                    }
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                if (frameTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(frameTexture);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }
            }
        }

        private static Dictionary<int, byte[]> LoadFrames(
            IReadOnlyCollection<int> indices)
        {
            string absoluteFramesDirectory = Path.GetFullPath(FramesDirectory);
            Dictionary<int, byte[]> frames = new Dictionary<int, byte[]>();
            foreach (int index in indices)
            {
                string path = FramePath(absoluteFramesDirectory, index);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        "A directly reviewed Player_Run_Forward frame is missing.",
                        path);
                }

                frames.Add(index, File.ReadAllBytes(path));
            }

            return frames;
        }

        private static void ComposeCapture(
            IReadOnlyDictionary<int, byte[]> framePngs,
            IReadOnlyList<int> indices,
            string outputPath)
        {
            if (indices.Count != 8 ||
                indices.Any(index => !framePngs.ContainsKey(index)))
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward phase frames are incomplete.");
            }

            Texture2D composite = new Texture2D(
                CaptureWidth * 4,
                CaptureHeight * 2,
                TextureFormat.RGB24,
                false);
            List<Texture2D> panels = new List<Texture2D>();
            try
            {
                for (int index = 0; index < indices.Count; index++)
                {
                    Texture2D panel = new Texture2D(
                        2,
                        2,
                        TextureFormat.RGB24,
                        false);
                    if (!panel.LoadImage(framePngs[indices[index]], false))
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                        throw new InvalidOperationException(
                            "Player_Run_Forward phase frame decoding failed.");
                    }

                    panels.Add(panel);
                    int column = index % 4;
                    int row = index < 4 ? 1 : 0;
                    composite.SetPixels(
                        column * CaptureWidth,
                        row * CaptureHeight,
                        CaptureWidth,
                        CaptureHeight,
                        panel.GetPixels());
                }

                composite.Apply(false, false);
                string absoluteOutputPath = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absoluteOutputPath) ??
                    throw new InvalidOperationException(
                        "Player run capture directory is unavailable."));
                File.WriteAllBytes(absoluteOutputPath, composite.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D panel in panels)
                {
                    UnityEngine.Object.DestroyImmediate(panel);
                }

                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static int[] FinalFrameIndices(int framesPerLoop)
        {
            int firstThird = framesPerLoop / 3;
            int secondThird = framesPerLoop * 2 / 3;
            int last = framesPerLoop - 1;
            return new[]
            {
                0, firstThird, secondThird, last,
                framesPerLoop,
                framesPerLoop + firstThird,
                framesPerLoop + secondThird,
                framesPerLoop + last
            };
        }

        private static string FramePath(string directory, int frame)
        {
            return Path.Combine(
                directory,
                "frame_" + frame.ToString(
                    "D3",
                    CultureInfo.InvariantCulture) + ".png");
        }

        private static RendererState[] IsolateTargetRenderers(
            IReadOnlyCollection<Renderer> targetRenderers)
        {
            HashSet<Renderer> targetSet = targetRenderers.ToHashSet();
            RendererState[] states = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(renderer => !targetSet.Contains(renderer))
                .Select(renderer => new RendererState(renderer))
                .ToArray();
            foreach (RendererState state in states)
            {
                state.Hide();
            }

            return states;
        }

        private static void ConfigureCamera(
            Camera camera,
            Transform target,
            Bounds bounds)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                target.forward,
                Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    "Player_Run_Forward has no usable forward direction.");
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = true;
            camera.aspect = CaptureWidth / (float)CaptureHeight;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.position = bounds.center + forward * 8f;
            camera.transform.LookAt(bounds.center, target.up);
            float verticalExtent = ProjectedHalfExtent(
                bounds.extents,
                camera.transform.up);
            float horizontalExtent = ProjectedHalfExtent(
                bounds.extents,
                camera.transform.right);
            camera.orthographicSize = Mathf.Max(
                verticalExtent * 1.2f,
                horizontalExtent / camera.aspect * 1.2f,
                0.5f);
        }

        private static float ProjectedHalfExtent(Vector3 extents, Vector3 axis)
        {
            axis = new Vector3(
                Mathf.Abs(axis.x),
                Mathf.Abs(axis.y),
                Mathf.Abs(axis.z));
            return Vector3.Dot(extents, axis);
        }

        private static Bounds CalculateBounds(IReadOnlyList<Renderer> renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Count; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static Pose CapturePose(Transform root)
        {
            Pose pose = new Pose();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(item, root);
                pose.Positions[path] = item.localPosition;
                pose.Rotations[path] = item.localRotation;
            }

            return pose;
        }

        private static float PosePositionDifference(Pose first, Pose second)
        {
            if (!first.Positions.Keys.ToHashSet().SetEquals(second.Positions.Keys))
            {
                return float.PositiveInfinity;
            }

            return first.Positions.Keys.Max(path => Vector3.Distance(
                first.Positions[path],
                second.Positions[path]));
        }

        private static float PoseRotationDifference(Pose first, Pose second)
        {
            if (!first.Rotations.Keys.ToHashSet().SetEquals(second.Rotations.Keys))
            {
                return float.PositiveInfinity;
            }

            return first.Rotations.Keys.Max(path => Quaternion.Angle(
                first.Rotations[path],
                second.Rotations[path]));
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            return new Vector2(
                first.x - second.x,
                first.z - second.z).magnitude;
        }

        private static float PositionRange(IReadOnlyCollection<Vector3> positions)
        {
            Bounds bounds = new Bounds(positions.First(), Vector3.zero);
            foreach (Vector3 position in positions.Skip(1))
            {
                bounds.Encapsulate(position);
            }

            return bounds.size.magnitude;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != PlayerRunForwardAnimationTool.ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for Player_Run_Forward review.");
            }

            return scene;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
