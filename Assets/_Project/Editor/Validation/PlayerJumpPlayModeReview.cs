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
    internal static class PlayerJumpPlayModeReview
    {
        private const string CapturedSessionKey =
            "Bellerophon.PlayerJumpReview.Captured";
        private const string FramesDirectory =
            "Logs/PlayerJumpPlayModeReviewFrames";
        private const string MetricsPath =
            "docs/validation/player_jump_mixamo_review_metrics.json";
        private const string ReviewContactSheetPath =
            "docs/validation/player_jump_mixamo_review_contact_sheet.png";
        private const string FinalCapturePath =
            "docs/validation/player_jump_mixamo_final.png";
        private const int CaptureWidth = 480;
        private const int CaptureHeight = 640;
        private const int LoopCount = 2;
        private const float RootPositionTolerance = 0.001f;
        private const float LoopPositionTolerance = 0.0001f;
        private const float LoopRotationTolerance = 0.01f;
        private const float MinimumVerticalTravel = 0.05f;

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string target;
            public string state;
            public string sourceTake;
            public float clipDurationSeconds;
            public float clipFrameRate;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootPositionDisplacementMax;
            public float hipsVerticalRange;
            public float hipsHorizontalDisplacementMax;
            public float leftFootVerticalRange;
            public float rightFootVerticalRange;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public bool clipIsLooping;
            public bool applyRootMotion;
            public bool sourceClipDirect;
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

        [MenuItem("Bellerophon/Player/Capture Jump Review")]
        internal static void CaptureReview()
        {
            if (!EditorApplication.isPlaying)
            {
                Scene scene = RequireScene();
                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp must be clean before Player_Jump review.");
                }

                SessionState.SetBool(CapturedSessionKey, false);
                EditorApplication.EnterPlaymode();
                Debug.Log(
                    "[PlayerJump] Entering Play Mode for direct two-loop review.");
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
                "[PlayerJump] Exiting Play Mode after direct two-loop review.");
        }

        [MenuItem("Bellerophon/Player/Capture Jump Final")]
        internal static void CaptureFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player_Jump final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Player_Jump final capture.");
            }

            string absoluteMetricsPath = Path.GetFullPath(MetricsPath);
            if (!File.Exists(absoluteMetricsPath))
            {
                throw new FileNotFoundException(
                    "Player_Jump review metrics are missing.",
                    absoluteMetricsPath);
            }

            ReviewMetrics metrics = JsonUtility.FromJson<ReviewMetrics>(
                File.ReadAllText(absoluteMetricsPath, Encoding.UTF8));
            if (metrics == null || !metrics.passedNumericChecks ||
                metrics.framesPerLoop < 2 || metrics.loopsSampled != LoopCount)
            {
                throw new InvalidOperationException(
                    "Player_Jump review did not pass before final capture.");
            }

            int[] indices = PhaseFrameIndices(metrics.framesPerLoop);
            Dictionary<int, byte[]> frames = LoadFrames(indices);
            ComposeCapture(frames, indices, FinalCapturePath);
            Debug.Log(
                "[PlayerJump] Final composed from directly reviewed jump frames." +
                " Output=" + Path.GetFullPath(FinalCapturePath) +
                ", SourceTake=" + metrics.sourceTake +
                ", FramesPerLoop=" + metrics.framesPerLoop.ToString(
                    CultureInfo.InvariantCulture) +
                ", Loops=" + metrics.loopsSampled.ToString(
                    CultureInfo.InvariantCulture) +
                ", SceneChanged=False.");
        }

        private static void CaptureTwoLoops()
        {
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            Transform target = PlayerJumpAnimationTool.RequireTarget(scene);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    "Player_Jump Animator is missing.");
            if (animator.runtimeAnimatorController == null ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Player_Jump Animator connection or Apply Root Motion differs.");
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    clips[0].name,
                    PlayerJumpAnimationTool.ExpectedTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Jump does not reference the investigated Mixamo Take.");
            }

            AnimationClip clip = clips[0];
            bool sourceClipDirect = string.Equals(
                AssetDatabase.GetAssetPath(clip),
                PlayerJumpAnimationTool.SourcePath,
                StringComparison.Ordinal);
            if (!sourceClipDirect || !clip.isLooping ||
                clip.frameRate <= 0f || clip.length <= 0f)
            {
                throw new InvalidOperationException(
                    "Player_Jump direct clip, timing, or Loop Time is invalid.");
            }

            int framesPerLoop = Mathf.RoundToInt(clip.length * clip.frameRate);
            if (framesPerLoop < 4)
            {
                throw new InvalidOperationException(
                    "Player_Jump clip has too few frames for direct review.");
            }

            Transform hips = PlayerJumpAnimationTool.FindUniqueBone(target, "Hips");
            Transform leftFoot = PlayerJumpAnimationTool.FindUniqueBone(target, "LeftFoot");
            Transform rightFoot = PlayerJumpAnimationTool.FindUniqueBone(target, "RightFoot");
            Renderer[] targetRenderers = target
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (targetRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Jump has no enabled renderer.");
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
                    "PlayerJumpReviewCamera",
                    typeof(Camera));
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                Camera camera = cameraObject.GetComponent<Camera>();

                lightObject = new GameObject(
                    "PlayerJumpReviewLight",
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
                    PlayerJumpAnimationTool.StateName);
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != stateHash)
                {
                    throw new InvalidOperationException(
                        "Player_Jump state was not entered.");
                }

                int totalFrames = framesPerLoop * LoopCount;
                Vector3 rootBaseline = target.position;
                Vector3 hipsBaseline = hips.position;
                float rootDisplacementMax = 0f;
                float hipsHorizontalDisplacementMax = 0f;
                List<Vector3> hipsPositions = new List<Vector3>();
                List<Vector3> leftFootPositions = new List<Vector3>();
                List<Vector3> rightFootPositions = new List<Vector3>();
                Pose[] poses = new Pose[totalFrames];
                Bounds motionBounds = default;
                bool hasMotionBounds = false;

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
                    hipsPositions.Add(hips.position);
                    leftFootPositions.Add(leftFoot.position);
                    rightFootPositions.Add(rightFoot.position);
                    poses[frame] = CapturePose(target);
                    foreach (Renderer renderer in targetRenderers)
                    {
                        if (!hasMotionBounds)
                        {
                            motionBounds = renderer.bounds;
                            hasMotionBounds = true;
                        }
                        else
                        {
                            motionBounds.Encapsulate(renderer.bounds);
                        }
                    }
                }

                if (!hasMotionBounds)
                {
                    throw new InvalidOperationException(
                        "Player_Jump motion bounds are unavailable.");
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

                float hipsVerticalRange = VerticalRange(hipsPositions);
                float leftFootVerticalRange = VerticalRange(leftFootPositions);
                float rightFootVerticalRange = VerticalRange(rightFootPositions);
                bool passedNumericChecks =
                    rootDisplacementMax <= RootPositionTolerance &&
                    loopPositionDifferenceMax <= LoopPositionTolerance &&
                    loopRotationDifferenceMax <= LoopRotationTolerance &&
                    hipsVerticalRange >= MinimumVerticalTravel &&
                    leftFootVerticalRange >= MinimumVerticalTravel &&
                    rightFootVerticalRange >= MinimumVerticalTravel &&
                    clip.isLooping &&
                    !animator.applyRootMotion &&
                    sourceClipDirect;

                ConfigureCamera(camera, target, motionBounds);
                string absoluteFramesDirectory = Path.GetFullPath(FramesDirectory);
                Directory.CreateDirectory(absoluteFramesDirectory);
                foreach (string oldFrame in Directory.GetFiles(
                             absoluteFramesDirectory,
                             "frame_*.png"))
                {
                    File.Delete(oldFrame);
                }

                int[] phaseIndices = PhaseFrameIndices(framesPerLoop);
                Dictionary<int, byte[]> phaseFrames =
                    new Dictionary<int, byte[]>();
                foreach (int frame in phaseIndices)
                {
                    animator.Play(
                        stateHash,
                        0,
                        frame / (float)framesPerLoop);
                    animator.Update(0f);
                    byte[] png = CaptureFrame(
                        camera,
                        renderTexture,
                        frameTexture);
                    File.WriteAllBytes(
                        FramePath(absoluteFramesDirectory, frame),
                        png);
                    phaseFrames.Add(frame, png);
                }

                ReviewMetrics metrics = new ReviewMetrics
                {
                    target = PlayerJumpAnimationTool.TargetName,
                    state = PlayerJumpAnimationTool.StateName,
                    sourceTake = PlayerJumpAnimationTool.ExpectedTakeName,
                    clipDurationSeconds = clip.length,
                    clipFrameRate = clip.frameRate,
                    framesPerLoop = framesPerLoop,
                    framesSampled = totalFrames,
                    loopsSampled = LoopCount,
                    rootPositionDisplacementMax = rootDisplacementMax,
                    hipsVerticalRange = hipsVerticalRange,
                    hipsHorizontalDisplacementMax = hipsHorizontalDisplacementMax,
                    leftFootVerticalRange = leftFootVerticalRange,
                    rightFootVerticalRange = rightFootVerticalRange,
                    loopPositionDifferenceMax = loopPositionDifferenceMax,
                    loopRotationDifferenceDegreesMax = loopRotationDifferenceMax,
                    clipIsLooping = clip.isLooping,
                    applyRootMotion = animator.applyRootMotion,
                    sourceClipDirect = sourceClipDirect,
                    passedNumericChecks = passedNumericChecks,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
                WriteMetrics(metrics);
                ComposeCapture(
                    phaseFrames,
                    phaseIndices,
                    ReviewContactSheetPath);

                if (!passedNumericChecks)
                {
                    throw new InvalidOperationException(
                        "Player_Jump numeric support checks failed." +
                        " Root=" + Num(rootDisplacementMax) +
                        ", HipsVertical=" + Num(hipsVerticalRange) +
                        ", LeftFootVertical=" + Num(leftFootVerticalRange) +
                        ", RightFootVertical=" + Num(rightFootVerticalRange) +
                        ", LoopPosition=" + Num(loopPositionDifferenceMax) +
                        ", LoopRotation=" + Num(loopRotationDifferenceMax) + ".");
                }

                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException(
                        "Player_Jump review changed the scene dirty state.");
                }

                Debug.Log(
                    "[PlayerJump] Captured investigated Mixamo jump Take." +
                    " Take=" + clip.name +
                    ", Duration=" + Num(clip.length) +
                    ", FrameRate=" + Num(clip.frameRate) +
                    ", FramesPerLoop=" + framesPerLoop.ToString(
                        CultureInfo.InvariantCulture) +
                    ", Frames=" + totalFrames.ToString(
                        CultureInfo.InvariantCulture) +
                    ", Loops=2" +
                    ", RootDisplacement=" + Num(rootDisplacementMax) +
                    ", HipsVerticalRange=" + Num(hipsVerticalRange) +
                    ", LoopPositionDifference=" + Num(loopPositionDifferenceMax) +
                    ", LoopRotationDifference=" + Num(loopRotationDifferenceMax) +
                    ", ApplyRootMotion=False.");
            }
            finally
            {
                animator.speed = originalSpeed;
                animator.cullingMode = originalCullingMode;
                if (hiddenRendererStates != null)
                {
                    foreach (RendererState state in hiddenRendererStates)
                    {
                        state.Restore();
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

        private static byte[] CaptureFrame(
            Camera camera,
            RenderTexture renderTexture,
            Texture2D frameTexture)
        {
            camera.Render();
            RenderTexture.active = renderTexture;
            frameTexture.ReadPixels(
                new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                0,
                0,
                false);
            frameTexture.Apply(false, false);
            return frameTexture.EncodeToPNG();
        }

        private static void WriteMetrics(ReviewMetrics metrics)
        {
            string absolutePath = Path.GetFullPath(MetricsPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "Player_Jump metrics directory is unavailable."));
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(metrics, true) + Environment.NewLine,
                new UTF8Encoding(false));
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
                        "A directly reviewed Player_Jump frame is missing.",
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
            if (indices.Count != 10 ||
                indices.Any(index => !framePngs.ContainsKey(index)))
            {
                throw new InvalidOperationException(
                    "Player_Jump phase frames are incomplete.");
            }

            Texture2D composite = new Texture2D(
                CaptureWidth * 5,
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
                            "Player_Jump phase frame decoding failed.");
                    }

                    panels.Add(panel);
                    int column = index % 5;
                    int row = index < 5 ? 1 : 0;
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
                        "Player_Jump capture directory is unavailable."));
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

        private static int[] PhaseFrameIndices(int framesPerLoop)
        {
            int quarter = framesPerLoop / 4;
            int half = framesPerLoop / 2;
            int threeQuarters = framesPerLoop * 3 / 4;
            int last = framesPerLoop - 1;
            return new[]
            {
                0, quarter, half, threeQuarters, last,
                framesPerLoop,
                framesPerLoop + quarter,
                framesPerLoop + half,
                framesPerLoop + threeQuarters,
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
                    "Player_Jump has no usable forward direction.");
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
                verticalExtent * 1.15f,
                horizontalExtent / camera.aspect * 1.15f,
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

        private static float VerticalRange(IReadOnlyCollection<Vector3> positions)
        {
            float minimum = positions.Min(position => position.y);
            float maximum = positions.Max(position => position.y);
            return maximum - minimum;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != PlayerJumpAnimationTool.ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for Player_Jump review.");
            }

            return scene;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
