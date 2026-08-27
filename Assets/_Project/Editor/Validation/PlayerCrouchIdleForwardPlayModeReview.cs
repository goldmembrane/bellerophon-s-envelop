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
    internal static class PlayerCrouchIdleForwardPlayModeReview
    {
        private const string IdleSessionKey =
            "Bellerophon.PlayerCrouchIdle.Review.Captured";
        private const string ForwardSessionKey =
            "Bellerophon.PlayerCrouchForward.Review.Captured";
        private const string IdleFramesDirectory =
            "Logs/PlayerCrouchIdleReviewFrames";
        private const string ForwardFramesDirectory =
            "Logs/PlayerCrouchForwardReviewFrames";
        private const string IdleMetricsPath =
            "docs/validation/player_crouch_idle_review_metrics.json";
        private const string ForwardMetricsPath =
            "docs/validation/player_crouch_forward_review_metrics.json";
        private const string IdleContactPath =
            "docs/validation/player_crouch_idle_review_contact_sheet.png";
        private const string ForwardContactPath =
            "docs/validation/player_crouch_forward_review_contact_sheet.png";
        private const string IdleFinalPath =
            "docs/validation/player_crouch_idle_final.png";
        private const string ForwardFinalPath =
            "docs/validation/player_crouch_forward_final.png";
        private const int CaptureWidth = 400;
        private const int CaptureHeight = 500;
        private const float PositionTolerance = 0.0001f;
        private const float RotationTolerance = 0.01f;

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string mode;
            public string target;
            public string state;
            public string clipName;
            public string clipAssetPath;
            public string sourceTake;
            public float clipDurationSeconds;
            public float clipFrameRate;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootHorizontalDisplacementMax;
            public float hipsHorizontalDisplacementMax;
            public float staticPosePositionDifferenceMax;
            public float staticPoseRotationDifferenceDegreesMax;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public float leftFootHorizontalRange;
            public float rightFootHorizontalRange;
            public bool idleMatchesEnterFinalPose;
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

        [MenuItem("Bellerophon/Player/Capture Crouch Idle Review")]
        internal static void CaptureIdleReview()
        {
            AdvanceReview(idle: true);
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Forward Review")]
        internal static void CaptureForwardReview()
        {
            AdvanceReview(idle: false);
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Idle Final")]
        internal static void CaptureIdleFinal()
        {
            CaptureFinal(idle: true);
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Forward Final")]
        internal static void CaptureForwardFinal()
        {
            CaptureFinal(idle: false);
        }

        private static void AdvanceReview(bool idle)
        {
            string sessionKey = idle ? IdleSessionKey : ForwardSessionKey;
            if (!EditorApplication.isPlaying)
            {
                Scene scene = RequireScene();
                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp must be clean before crouch review.");
                }

                SessionState.SetBool(sessionKey, false);
                EditorApplication.EnterPlaymode();
                Debug.Log(
                    "[PlayerCrouchReview] Entering Play Mode for " +
                    (idle ? "idle" : "forward") + " review.");
                return;
            }

            if (!SessionState.GetBool(sessionKey, false))
            {
                try
                {
                    CaptureReview(idle);
                    SessionState.SetBool(sessionKey, true);
                }
                catch
                {
                    SessionState.EraseBool(sessionKey);
                    EditorApplication.ExitPlaymode();
                    throw;
                }

                return;
            }

            SessionState.EraseBool(sessionKey);
            EditorApplication.ExitPlaymode();
            Debug.Log(
                "[PlayerCrouchReview] Exiting Play Mode after " +
                (idle ? "idle" : "forward") + " review.");
        }

        private static void CaptureReview(bool idle)
        {
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            string targetName = idle
                ? PlayerCrouchIdleForwardAnimationTool.IdleTargetName
                : PlayerCrouchIdleForwardAnimationTool.ForwardTargetName;
            string stateName = idle
                ? PlayerCrouchIdleForwardAnimationTool.IdleStateName
                : PlayerCrouchIdleForwardAnimationTool.ForwardStateName;
            string expectedClipPath = idle
                ? PlayerCrouchIdleForwardAnimationTool.IdleClipPath
                : PlayerCrouchIdleForwardAnimationTool.ForwardClipPath;
            string framesDirectory = idle
                ? IdleFramesDirectory
                : ForwardFramesDirectory;
            string metricsPath = idle ? IdleMetricsPath : ForwardMetricsPath;
            string contactPath = idle ? IdleContactPath : ForwardContactPath;

            Transform target =
                PlayerCrouchIdleForwardAnimationTool.RequireTarget(
                    scene,
                    targetName);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    targetName + " Animator is missing.");
            if (animator.runtimeAnimatorController == null ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    targetName + " Animator configuration differs.");
            }

            AnimationClip[] clips = animator.runtimeAnimatorController
                .animationClips
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(clips[0]),
                    expectedClipPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    targetName + " controller does not reference the approved clip.");
            }

            AnimationClip clip = clips[0];
            if (!clip.isLooping || clip.frameRate <= 0f || clip.length <= 0f)
            {
                throw new InvalidOperationException(
                    targetName + " clip timing or loop setting differs.");
            }

            int framesPerLoop = Mathf.RoundToInt(
                clip.length * clip.frameRate);
            if (framesPerLoop < 4)
            {
                throw new InvalidOperationException(
                    targetName + " has too few review frames.");
            }

            Transform hips =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "Hips");
            Transform leftFoot =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "LeftFoot");
            Transform rightFoot =
                PlayerCrouchIdleForwardAnimationTool.FindUniqueBone(
                    target,
                    "RightFoot");
            Renderer[] targetRenderers = target
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (targetRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    targetName + " has no enabled renderer.");
            }

            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            RendererState[] hiddenRendererStates = null;
            GameObject fullCameraObject = null;
            GameObject legCameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            Texture2D frameTexture = null;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                hiddenRendererStates = IsolateTargetRenderers(targetRenderers);
                fullCameraObject = CreateCameraObject(
                    targetName + "FullCamera");
                legCameraObject = CreateCameraObject(
                    targetName + "LegCamera");
                Camera fullCamera = fullCameraObject.GetComponent<Camera>();
                Camera legCamera = legCameraObject.GetComponent<Camera>();
                ConfigureFixedCamera(
                    fullCamera,
                    target,
                    target.position + target.up * 1.05f,
                    1.35f);
                ConfigureFixedCamera(
                    legCamera,
                    target,
                    target.position + target.up * 0.58f,
                    0.72f);

                lightObject = new GameObject(
                    targetName + "ReviewLight",
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
                frameTexture = new Texture2D(
                    CaptureWidth,
                    CaptureHeight,
                    TextureFormat.RGB24,
                    false);

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int stateHash = Animator.StringToHash(stateName);
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash !=
                    stateHash)
                {
                    throw new InvalidOperationException(
                        targetName + " state was not entered.");
                }

                const int loops = 2;
                int totalFrames = framesPerLoop * loops;
                Pose[] poses = new Pose[totalFrames];
                Vector3 rootBaseline = target.position;
                Vector3 hipsBaseline = hips.position;
                float rootHorizontalMax = 0f;
                float hipsHorizontalMax = 0f;
                List<Vector3> leftFootPositions = new List<Vector3>();
                List<Vector3> rightFootPositions = new List<Vector3>();
                for (int frame = 0; frame < totalFrames; frame++)
                {
                    animator.Play(
                        stateHash,
                        0,
                        frame / (float)framesPerLoop);
                    animator.Update(0f);
                    poses[frame] = CapturePose(target);
                    rootHorizontalMax = Mathf.Max(
                        rootHorizontalMax,
                        HorizontalDistance(target.position, rootBaseline));
                    hipsHorizontalMax = Mathf.Max(
                        hipsHorizontalMax,
                        HorizontalDistance(hips.position, hipsBaseline));
                    leftFootPositions.Add(leftFoot.position);
                    rightFootPositions.Add(rightFoot.position);
                }

                float staticPositionDifference = 0f;
                float staticRotationDifference = 0f;
                if (idle)
                {
                    for (int frame = 1; frame < totalFrames; frame++)
                    {
                        staticPositionDifference = Mathf.Max(
                            staticPositionDifference,
                            PosePositionDifference(poses[0], poses[frame]));
                        staticRotationDifference = Mathf.Max(
                            staticRotationDifference,
                            PoseRotationDifference(poses[0], poses[frame]));
                    }
                }

                float loopPositionDifference = 0f;
                float loopRotationDifference = 0f;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    loopPositionDifference = Mathf.Max(
                        loopPositionDifference,
                        PosePositionDifference(
                            poses[frame],
                            poses[frame + framesPerLoop]));
                    loopRotationDifference = Mathf.Max(
                        loopRotationDifference,
                        PoseRotationDifference(
                            poses[frame],
                            poses[frame + framesPerLoop]));
                }

                bool idleMatchesEnter = !idle || IdleMatchesEnterFinal(clip);
                bool passedNumericChecks =
                    rootHorizontalMax <= PositionTolerance &&
                    hipsHorizontalMax <= PositionTolerance &&
                    loopPositionDifference <= PositionTolerance &&
                    loopRotationDifference <= RotationTolerance &&
                    !animator.applyRootMotion &&
                    clip.isLooping &&
                    (!idle ||
                     (Mathf.Abs(
                          clip.length -
                          PlayerCrouchIdleForwardAnimationTool
                              .IdleDurationSeconds) <= PositionTolerance &&
                      staticPositionDifference <= PositionTolerance &&
                      staticRotationDifference <= RotationTolerance &&
                      idleMatchesEnter));

                string absoluteFramesDirectory = Path.GetFullPath(
                    framesDirectory);
                Directory.CreateDirectory(absoluteFramesDirectory);
                foreach (string oldFrame in Directory.GetFiles(
                             absoluteFramesDirectory,
                             "frame_*.png"))
                {
                    File.Delete(oldFrame);
                }

                int[] phaseIndices = PhaseFrameIndices(framesPerLoop);
                Dictionary<int, byte[]> fullFrames =
                    new Dictionary<int, byte[]>();
                Dictionary<int, byte[]> legFrames =
                    new Dictionary<int, byte[]>();
                foreach (int frame in phaseIndices)
                {
                    animator.Play(
                        stateHash,
                        0,
                        frame / (float)framesPerLoop);
                    animator.Update(0f);
                    byte[] fullPng = CaptureFrame(
                        fullCamera,
                        renderTexture,
                        frameTexture);
                    byte[] legPng = CaptureFrame(
                        legCamera,
                        renderTexture,
                        frameTexture);
                    File.WriteAllBytes(
                        FramePath(
                            absoluteFramesDirectory,
                            frame,
                            "full"),
                        fullPng);
                    File.WriteAllBytes(
                        FramePath(
                            absoluteFramesDirectory,
                            frame,
                            "legs"),
                        legPng);
                    fullFrames.Add(frame, fullPng);
                    legFrames.Add(frame, legPng);
                }

                ReviewMetrics metrics = new ReviewMetrics
                {
                    mode = idle ? "idle" : "forward",
                    target = targetName,
                    state = stateName,
                    clipName = clip.name,
                    clipAssetPath = expectedClipPath,
                    sourceTake = idle
                        ? "Player_Crouch_Enter final 0.5-second hold"
                        : PlayerCrouchIdleForwardAnimationTool
                            .ExpectedForwardTakeName,
                    clipDurationSeconds = clip.length,
                    clipFrameRate = clip.frameRate,
                    framesPerLoop = framesPerLoop,
                    framesSampled = totalFrames,
                    loopsSampled = loops,
                    rootHorizontalDisplacementMax = rootHorizontalMax,
                    hipsHorizontalDisplacementMax = hipsHorizontalMax,
                    staticPosePositionDifferenceMax =
                        staticPositionDifference,
                    staticPoseRotationDifferenceDegreesMax =
                        staticRotationDifference,
                    loopPositionDifferenceMax = loopPositionDifference,
                    loopRotationDifferenceDegreesMax =
                        loopRotationDifference,
                    leftFootHorizontalRange = HorizontalRange(
                        leftFootPositions),
                    rightFootHorizontalRange = HorizontalRange(
                        rightFootPositions),
                    idleMatchesEnterFinalPose = idleMatchesEnter,
                    clipIsLooping = clip.isLooping,
                    applyRootMotion = animator.applyRootMotion,
                    passedNumericChecks = passedNumericChecks,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
                WriteMetrics(metricsPath, metrics);
                ComposeCapture(
                    fullFrames,
                    legFrames,
                    phaseIndices,
                    contactPath);

                if (!passedNumericChecks)
                {
                    throw new InvalidOperationException(
                        targetName + " numeric support checks failed." +
                        " Root=" + Num(rootHorizontalMax) +
                        ", Hips=" + Num(hipsHorizontalMax) +
                        ", StaticPosition=" + Num(staticPositionDifference) +
                        ", StaticRotation=" + Num(staticRotationDifference) +
                        ", LoopPosition=" + Num(loopPositionDifference) +
                        ", LoopRotation=" + Num(loopRotationDifference) +
                        ", IdleMatchesEnter=" + idleMatchesEnter + ".");
                }

                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException(
                        targetName + " review changed the scene dirty state.");
                }

                Debug.Log(
                    "[PlayerCrouchReview] Captured " + targetName +
                    " direct two-loop review." +
                    " Clip=" + clip.name +
                    ", Duration=" + Num(clip.length) +
                    ", FrameRate=" + Num(clip.frameRate) +
                    ", FramesPerLoop=" + framesPerLoop.ToString(
                        CultureInfo.InvariantCulture) +
                    ", Frames=" + totalFrames.ToString(
                        CultureInfo.InvariantCulture) +
                    ", RootHorizontal=" + Num(rootHorizontalMax) +
                    ", HipsHorizontal=" + Num(hipsHorizontalMax) +
                    ", StaticPosition=" + Num(staticPositionDifference) +
                    ", StaticRotation=" + Num(staticRotationDifference) +
                    ", LoopPosition=" + Num(loopPositionDifference) +
                    ", LoopRotation=" + Num(loopRotationDifference) +
                    ", LeftFootRange=" + Num(metrics.leftFootHorizontalRange) +
                    ", RightFootRange=" + Num(metrics.rightFootHorizontalRange) +
                    ", ApplyRootMotion=False.");
            }
            finally
            {
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                if (hiddenRendererStates != null)
                {
                    foreach (RendererState state in hiddenRendererStates)
                    {
                        state.Restore();
                    }
                }

                RenderTexture.active = previousActive;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                if (frameTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(frameTexture);
                }

                if (fullCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(fullCameraObject);
                }

                if (legCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(legCameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }
            }
        }

        private static bool IdleMatchesEnterFinal(AnimationClip idle)
        {
            AnimationClip enter = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                PlayerCrouchIdleForwardAnimationTool.EnterClipPath);
            if (enter == null ||
                enter.length <
                PlayerCrouchIdleForwardAnimationTool.IdleDurationSeconds)
            {
                return false;
            }

            EditorCurveBinding[] enterBindings =
                AnimationUtility.GetCurveBindings(enter);
            if (!new HashSet<EditorCurveBinding>(enterBindings).SetEquals(
                    AnimationUtility.GetCurveBindings(idle)))
            {
                return false;
            }

            float holdStart = enter.length -
                              PlayerCrouchIdleForwardAnimationTool
                                  .IdleDurationSeconds;
            foreach (EditorCurveBinding binding in enterBindings)
            {
                AnimationCurve enterCurve =
                    AnimationUtility.GetEditorCurve(enter, binding);
                AnimationCurve idleCurve =
                    AnimationUtility.GetEditorCurve(idle, binding);
                float expected = enterCurve.Evaluate(holdStart);
                if (idleCurve == null ||
                    Mathf.Abs(idleCurve.Evaluate(0f) - expected) >
                        PositionTolerance ||
                    Mathf.Abs(
                        idleCurve.Evaluate(idle.length) - expected) >
                        PositionTolerance)
                {
                    return false;
                }
            }

            AnimationClipSettings idleSettings =
                AnimationUtility.GetAnimationClipSettings(idle);
            return idleSettings.loopTime &&
                   !idleSettings.loopBlend &&
                   Mathf.Abs(idleSettings.startTime) <=
                       PositionTolerance &&
                   Mathf.Abs(
                       idleSettings.stopTime -
                       PlayerCrouchIdleForwardAnimationTool
                           .IdleDurationSeconds) <=
                       PositionTolerance;
        }

        private static void CaptureFinal(bool idle)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Crouch final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch final capture.");
            }

            string metricsPath = idle ? IdleMetricsPath : ForwardMetricsPath;
            string contactPath = idle ? IdleContactPath : ForwardContactPath;
            string finalPath = idle ? IdleFinalPath : ForwardFinalPath;
            ReviewMetrics metrics = ReadMetrics(metricsPath);
            if (!metrics.passedNumericChecks || metrics.loopsSampled != 2)
            {
                throw new InvalidOperationException(
                    metrics.target + " review did not pass before final capture.");
            }

            string absoluteContact = Path.GetFullPath(contactPath);
            string absoluteFinal = Path.GetFullPath(finalPath);
            if (!File.Exists(absoluteContact))
            {
                throw new FileNotFoundException(
                    "Reviewed crouch contact sheet is missing.",
                    absoluteContact);
            }

            Directory.CreateDirectory(
                Path.GetDirectoryName(absoluteFinal) ??
                throw new InvalidOperationException(
                    "Crouch final directory is unavailable."));
            File.Copy(absoluteContact, absoluteFinal, true);
            Debug.Log(
                "[PlayerCrouchReview] Final copied from directly reviewed " +
                metrics.target + " frames. Output=" + absoluteFinal +
                ", FramesPerLoop=" + metrics.framesPerLoop.ToString(
                    CultureInfo.InvariantCulture) +
                ", Loops=2, SceneChanged=False.");
        }

        private static GameObject CreateCameraObject(string name)
        {
            GameObject cameraObject = new GameObject(name, typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = true;
            camera.aspect = CaptureWidth / (float)CaptureHeight;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            return cameraObject;
        }

        private static void ConfigureFixedCamera(
            Camera camera,
            Transform target,
            Vector3 center,
            float orthographicSize)
        {
            Vector3 forward = Vector3.ProjectOnPlane(
                target.forward,
                Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    target.name + " has no usable forward direction.");
            }

            camera.transform.position = center + forward * 8f;
            camera.transform.LookAt(center, target.up);
            camera.orthographicSize = orthographicSize;
        }

        private static byte[] CaptureFrame(
            Camera camera,
            RenderTexture renderTexture,
            Texture2D frameTexture)
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            frameTexture.ReadPixels(
                new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                0,
                0,
                false);
            frameTexture.Apply(false, false);
            byte[] png = frameTexture.EncodeToPNG();
            camera.targetTexture = null;
            return png;
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

        private static Pose CapturePose(Transform root)
        {
            Pose pose = new Pose();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(
                         true))
            {
                string path = AnimationUtility.CalculateTransformPath(
                    item,
                    root);
                pose.Positions[path] = item.localPosition;
                pose.Rotations[path] = item.localRotation;
            }

            return pose;
        }

        private static float PosePositionDifference(Pose first, Pose second)
        {
            if (first == null || second == null ||
                !first.Positions.Keys.ToHashSet().SetEquals(
                    second.Positions.Keys))
            {
                return float.PositiveInfinity;
            }

            return first.Positions.Keys.Max(path => Vector3.Distance(
                first.Positions[path],
                second.Positions[path]));
        }

        private static float PoseRotationDifference(Pose first, Pose second)
        {
            if (first == null || second == null ||
                !first.Rotations.Keys.ToHashSet().SetEquals(
                    second.Rotations.Keys))
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

        private static float HorizontalRange(
            IReadOnlyCollection<Vector3> positions)
        {
            if (positions.Count == 0)
            {
                return 0f;
            }

            float minX = positions.Min(position => position.x);
            float maxX = positions.Max(position => position.x);
            float minZ = positions.Min(position => position.z);
            float maxZ = positions.Max(position => position.z);
            return new Vector2(maxX - minX, maxZ - minZ).magnitude;
        }

        private static int[] PhaseFrameIndices(int framesPerLoop)
        {
            return new[]
            {
                0,
                framesPerLoop / 4,
                framesPerLoop / 2,
                framesPerLoop * 3 / 4,
                framesPerLoop - 1
            };
        }

        private static string FramePath(
            string directory,
            int frame,
            string view)
        {
            return Path.Combine(
                directory,
                "frame_" + frame.ToString(
                    "D3",
                    CultureInfo.InvariantCulture) + "_" + view + ".png");
        }

        private static void ComposeCapture(
            IReadOnlyDictionary<int, byte[]> fullFrames,
            IReadOnlyDictionary<int, byte[]> legFrames,
            IReadOnlyList<int> indices,
            string outputPath)
        {
            if (indices.Count != 5 ||
                indices.Any(index =>
                    !fullFrames.ContainsKey(index) ||
                    !legFrames.ContainsKey(index)))
            {
                throw new InvalidOperationException(
                    "Crouch review phase frames are incomplete.");
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
                    Texture2D full = DecodeFrame(fullFrames[indices[index]]);
                    Texture2D legs = DecodeFrame(legFrames[indices[index]]);
                    panels.Add(full);
                    panels.Add(legs);
                    composite.SetPixels(
                        index * CaptureWidth,
                        CaptureHeight,
                        CaptureWidth,
                        CaptureHeight,
                        full.GetPixels());
                    composite.SetPixels(
                        index * CaptureWidth,
                        0,
                        CaptureWidth,
                        CaptureHeight,
                        legs.GetPixels());
                }

                composite.Apply(false, false);
                string absoluteOutput = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absoluteOutput) ??
                    throw new InvalidOperationException(
                        "Crouch capture directory is unavailable."));
                File.WriteAllBytes(
                    absoluteOutput,
                    composite.EncodeToPNG());
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

        private static Texture2D DecodeFrame(byte[] png)
        {
            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGB24,
                false);
            if (!texture.LoadImage(png, false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(
                    "Crouch review phase frame decoding failed.");
            }

            return texture;
        }

        private static void WriteMetrics(
            string path,
            ReviewMetrics metrics)
        {
            string absolutePath = Path.GetFullPath(path);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "Crouch metrics directory is unavailable."));
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(metrics, true) + Environment.NewLine,
                new UTF8Encoding(false));
        }

        private static ReviewMetrics ReadMetrics(string path)
        {
            string absolutePath = Path.GetFullPath(path);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Crouch review metrics are missing.",
                    absolutePath);
            }

            ReviewMetrics metrics = JsonUtility.FromJson<ReviewMetrics>(
                File.ReadAllText(absolutePath, Encoding.UTF8));
            return metrics ?? throw new InvalidOperationException(
                "Crouch review metrics could not be read.");
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                scene.path !=
                PlayerCrouchIdleForwardAnimationTool.ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for crouch review.");
            }

            return scene;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
