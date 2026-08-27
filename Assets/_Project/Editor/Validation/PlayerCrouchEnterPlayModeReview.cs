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
    internal static class PlayerCrouchEnterPlayModeReview
    {
        private const string SourceSessionKey =
            "Bellerophon.PlayerCrouchEnter.SourceReview.Captured";
        private const string CorrectedSessionKey =
            "Bellerophon.PlayerCrouchEnter.CorrectedReview.Captured";
        private const string SourceFramesDirectory =
            "Logs/PlayerCrouchEnterSourceReviewFrames";
        private const string CorrectedFramesDirectory =
            "Logs/PlayerCrouchEnterCorrectedReviewFrames";
        private const string SourceMetricsPath =
            "docs/validation/player_crouch_enter_source_metrics.json";
        private const string CorrectedMetricsPath =
            "docs/validation/player_crouch_enter_corrected_metrics.json";
        private const string SourceContactSheetPath =
            "docs/validation/player_crouch_enter_source_contact_sheet.png";
        private const string CorrectedContactSheetPath =
            "docs/validation/player_crouch_enter_corrected_contact_sheet.png";
        private const string FinalCapturePath =
            "docs/validation/player_crouch_enter_final.png";
        private const int CaptureWidth = 400;
        private const int CaptureHeight = 500;
        private const float RootPositionTolerance = 0.001f;
        private const float LoopPositionTolerance = 0.0001f;
        private const float LoopRotationTolerance = 0.01f;
        private const float HoldDurationTolerance = 0.0001f;

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string mode;
            public string target;
            public string state;
            public string clipName;
            public string clipAssetPath;
            public float clipDurationSeconds;
            public float clipFrameRate;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float sourceMotionDurationSeconds;
            public float finalPoseHoldDurationSeconds;
            public int finalPoseHoldFrames;
            public float holdPositionDifferenceMax;
            public float holdRotationDifferenceDegreesMax;
            public float rootPositionDisplacementMax;
            public float hipsVerticalRange;
            public float endLeftKneeOutwardExcess;
            public float endLeftKneeBendDegrees;
            public float endRightKneeOutwardExcess;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public float sourceEndLeftKneeOutwardExcess;
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

        [MenuItem("Bellerophon/Player/Capture Crouch Enter Source Review")]
        internal static void CaptureSourceReview()
        {
            AdvanceReview(corrected: false);
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Enter Corrected Review")]
        internal static void CaptureCorrectedReview()
        {
            AdvanceReview(corrected: true);
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Enter Hold Review")]
        internal static void CaptureHoldReview()
        {
            AdvanceReview(corrected: true);
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Enter Final")]
        internal static void CaptureFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Player_Crouch_Enter final capture.");
            }

            ReviewMetrics metrics = ReadMetrics(CorrectedMetricsPath);
            if (!metrics.passedNumericChecks || metrics.loopsSampled != 2)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter corrected review did not pass before final capture.");
            }

            int[] indices = PhaseFrameIndices(
                metrics.framesPerLoop,
                metrics.finalPoseHoldFrames);
            Dictionary<int, byte[]> fullFrames = LoadFrames(
                CorrectedFramesDirectory,
                indices,
                "full");
            Dictionary<int, byte[]> legFrames = LoadFrames(
                CorrectedFramesDirectory,
                indices,
                "legs");
            ComposeCapture(
                fullFrames,
                legFrames,
                indices,
                FinalCapturePath);
            Debug.Log(
                "[PlayerCrouchEnter] Final composed from directly reviewed corrected frames." +
                " Output=" + Path.GetFullPath(FinalCapturePath) +
                ", FramesPerLoop=" + metrics.framesPerLoop.ToString(
                    CultureInfo.InvariantCulture) +
                ", FinalPoseHoldSeconds=" +
                Num(metrics.finalPoseHoldDurationSeconds) +
                ", Loops=2, SceneChanged=False.");
        }

        private static void AdvanceReview(bool corrected)
        {
            string sessionKey = corrected ? CorrectedSessionKey : SourceSessionKey;
            if (!EditorApplication.isPlaying)
            {
                Scene scene = RequireScene();
                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp must be clean before Player_Crouch_Enter review.");
                }

                SessionState.SetBool(sessionKey, false);
                EditorApplication.EnterPlaymode();
                Debug.Log(
                    "[PlayerCrouchEnter] Entering Play Mode for " +
                    (corrected ? "corrected" : "source") + " review.");
                return;
            }

            if (!SessionState.GetBool(sessionKey, false))
            {
                try
                {
                    CaptureReview(corrected);
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
                "[PlayerCrouchEnter] Exiting Play Mode after " +
                (corrected ? "corrected" : "source") + " review.");
        }

        private static void CaptureReview(bool corrected)
        {
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            Transform target = PlayerCrouchEnterAnimationTool.RequireTarget(scene);
            Animator animator = target.GetComponent<Animator>() ??
                                throw new InvalidOperationException(
                                    "Player_Crouch_Enter Animator is missing.");
            if (animator.runtimeAnimatorController == null ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter Animator configuration differs.");
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter controller must reference exactly one clip.");
            }

            AnimationClip clip = clips[0];
            string clipAssetPath = AssetDatabase.GetAssetPath(clip);
            string expectedPath = corrected
                ? PlayerCrouchEnterAnimationTool.CorrectedClipPath
                : PlayerCrouchEnterAnimationTool.SourcePath;
            if (!string.Equals(
                    clipAssetPath,
                    expectedPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter review clip differs. Actual=" +
                    clipAssetPath + ".");
            }

            bool sourceClipDirect = string.Equals(
                clipAssetPath,
                PlayerCrouchEnterAnimationTool.SourcePath,
                StringComparison.Ordinal);
            if (!clip.isLooping || clip.frameRate <= 0f || clip.length <= 0f)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter clip timing or Loop Time is invalid.");
            }

            int framesPerLoop = Mathf.RoundToInt(clip.length * clip.frameRate);
            if (framesPerLoop < 4)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter clip has too few frames.");
            }

            float sourceMotionDuration = corrected
                ? PlayerCrouchEnterAnimationTool.LoadSingleSourceClip().length
                : clip.length;
            float finalPoseHoldDuration = corrected
                ? clip.length - sourceMotionDuration
                : 0f;
            int finalPoseHoldFrames = corrected
                ? Mathf.RoundToInt(finalPoseHoldDuration * clip.frameRate)
                : 0;
            int sourceMotionFrames = framesPerLoop - finalPoseHoldFrames;
            if (corrected &&
                (sourceMotionFrames < 4 || finalPoseHoldFrames < 1))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter source motion or final hold frame count is invalid.");
            }

            Transform hips = PlayerCrouchEnterAnimationTool.FindUniqueBone(target, "Hips");
            Transform leftUpLeg = PlayerCrouchEnterAnimationTool.FindUniqueBone(target, "LeftUpLeg");
            Transform leftLeg = PlayerCrouchEnterAnimationTool.FindUniqueBone(target, "LeftLeg");
            Transform leftFoot = PlayerCrouchEnterAnimationTool.FindUniqueBone(target, "LeftFoot");
            Transform rightUpLeg = PlayerCrouchEnterAnimationTool.FindUniqueBone(target, "RightUpLeg");
            Transform rightLeg = PlayerCrouchEnterAnimationTool.FindUniqueBone(target, "RightLeg");
            Transform rightFoot = PlayerCrouchEnterAnimationTool.FindUniqueBone(target, "RightFoot");
            Renderer[] targetRenderers = target
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (targetRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter has no enabled renderer.");
            }

            AnimatorCullingMode originalCullingMode = animator.cullingMode;
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
                fullCameraObject = CreateCameraObject("PlayerCrouchEnterFullCamera");
                legCameraObject = CreateCameraObject("PlayerCrouchEnterLegCamera");
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
                    "PlayerCrouchEnterReviewLight",
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
                int stateHash = Animator.StringToHash(
                    PlayerCrouchEnterAnimationTool.StateName);
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != stateHash)
                {
                    throw new InvalidOperationException(
                        "Player_Crouch_Enter state was not entered.");
                }

                int loops = corrected ? 2 : 1;
                int totalFrames = framesPerLoop * loops;
                Vector3 rootBaseline = target.position;
                float rootDisplacementMax = 0f;
                List<Vector3> hipsPositions = new List<Vector3>();
                Pose[] poses = new Pose[totalFrames];
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
                    hipsPositions.Add(hips.position);
                    poses[frame] = CapturePose(target);
                }

                float loopPositionDifferenceMax = 0f;
                float loopRotationDifferenceMax = 0f;
                if (corrected)
                {
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
                }

                float holdPositionDifferenceMax = 0f;
                float holdRotationDifferenceMax = 0f;
                if (corrected)
                {
                    Pose holdStartPose = poses[sourceMotionFrames];
                    for (int frame = sourceMotionFrames + 1;
                         frame < framesPerLoop;
                         frame++)
                    {
                        holdPositionDifferenceMax = Mathf.Max(
                            holdPositionDifferenceMax,
                            PosePositionDifference(
                                holdStartPose,
                                poses[frame]));
                        holdRotationDifferenceMax = Mathf.Max(
                            holdRotationDifferenceMax,
                            PoseRotationDifference(
                                holdStartPose,
                                poses[frame]));
                    }
                }

                int endFrame = framesPerLoop - 1;
                animator.Play(stateHash, 0, endFrame / (float)framesPerLoop);
                animator.Update(0f);
                float endLeftKneeOutwardExcess = KneeOutwardExcess(
                    target,
                    hips,
                    leftUpLeg,
                    leftLeg,
                    leftFoot);
                float endRightKneeOutwardExcess = KneeOutwardExcess(
                    target,
                    hips,
                    rightUpLeg,
                    rightLeg,
                    rightFoot);
                float endLeftKneeBendDegrees = KneeBendDegrees(
                    leftUpLeg,
                    leftLeg,
                    leftFoot);

                float sourceEndLeftKneeOutwardExcess =
                    endLeftKneeOutwardExcess;
                if (corrected)
                {
                    sourceEndLeftKneeOutwardExcess =
                        ReadMetrics(SourceMetricsPath).endLeftKneeOutwardExcess;
                }

                bool passedNumericChecks =
                    rootDisplacementMax <= RootPositionTolerance &&
                    clip.isLooping &&
                    !animator.applyRootMotion &&
                    (!corrected ||
                     (loopPositionDifferenceMax <= LoopPositionTolerance &&
                      loopRotationDifferenceMax <= LoopRotationTolerance &&
                      Mathf.Abs(
                          finalPoseHoldDuration -
                          PlayerCrouchEnterAnimationTool.HoldDurationSeconds) <=
                          HoldDurationTolerance &&
                      finalPoseHoldFrames == Mathf.RoundToInt(
                          PlayerCrouchEnterAnimationTool.HoldDurationSeconds *
                          clip.frameRate) &&
                      holdPositionDifferenceMax <= LoopPositionTolerance &&
                      holdRotationDifferenceMax <= LoopRotationTolerance &&
                      endLeftKneeOutwardExcess <
                          sourceEndLeftKneeOutwardExcess - 0.0001f &&
                      endLeftKneeOutwardExcess >= -0.005f));

                string framesDirectory = corrected
                    ? CorrectedFramesDirectory
                    : SourceFramesDirectory;
                string absoluteFramesDirectory = Path.GetFullPath(framesDirectory);
                Directory.CreateDirectory(absoluteFramesDirectory);
                foreach (string oldFrame in Directory.GetFiles(
                             absoluteFramesDirectory,
                             "frame_*.png"))
                {
                    File.Delete(oldFrame);
                }

                int[] phaseIndices = PhaseFrameIndices(
                    framesPerLoop,
                    finalPoseHoldFrames);
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
                        FramePath(absoluteFramesDirectory, frame, "full"),
                        fullPng);
                    File.WriteAllBytes(
                        FramePath(absoluteFramesDirectory, frame, "legs"),
                        legPng);
                    fullFrames.Add(frame, fullPng);
                    legFrames.Add(frame, legPng);
                }

                ReviewMetrics metrics = new ReviewMetrics
                {
                    mode = corrected ? "corrected" : "source",
                    target = PlayerCrouchEnterAnimationTool.TargetName,
                    state = PlayerCrouchEnterAnimationTool.StateName,
                    clipName = clip.name,
                    clipAssetPath = clipAssetPath,
                    clipDurationSeconds = clip.length,
                    clipFrameRate = clip.frameRate,
                    framesPerLoop = framesPerLoop,
                    framesSampled = totalFrames,
                    loopsSampled = loops,
                    sourceMotionDurationSeconds = sourceMotionDuration,
                    finalPoseHoldDurationSeconds = finalPoseHoldDuration,
                    finalPoseHoldFrames = finalPoseHoldFrames,
                    holdPositionDifferenceMax = holdPositionDifferenceMax,
                    holdRotationDifferenceDegreesMax =
                        holdRotationDifferenceMax,
                    rootPositionDisplacementMax = rootDisplacementMax,
                    hipsVerticalRange = VerticalRange(hipsPositions),
                    endLeftKneeOutwardExcess = endLeftKneeOutwardExcess,
                    endLeftKneeBendDegrees = endLeftKneeBendDegrees,
                    endRightKneeOutwardExcess = endRightKneeOutwardExcess,
                    loopPositionDifferenceMax = loopPositionDifferenceMax,
                    loopRotationDifferenceDegreesMax = loopRotationDifferenceMax,
                    sourceEndLeftKneeOutwardExcess =
                        sourceEndLeftKneeOutwardExcess,
                    clipIsLooping = clip.isLooping,
                    applyRootMotion = animator.applyRootMotion,
                    sourceClipDirect = sourceClipDirect,
                    passedNumericChecks = passedNumericChecks,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
                string metricsPath = corrected
                    ? CorrectedMetricsPath
                    : SourceMetricsPath;
                string contactSheetPath = corrected
                    ? CorrectedContactSheetPath
                    : SourceContactSheetPath;
                WriteMetrics(metricsPath, metrics);
                ComposeCapture(
                    fullFrames,
                    legFrames,
                    phaseIndices,
                    contactSheetPath);

                if (!passedNumericChecks)
                {
                    throw new InvalidOperationException(
                        "Player_Crouch_Enter numeric support checks failed." +
                        " Mode=" + metrics.mode +
                        ", Root=" + Num(rootDisplacementMax) +
                        ", LeftKneeOutward=" + Num(endLeftKneeOutwardExcess) +
                        ", SourceLeftKneeOutward=" +
                        Num(sourceEndLeftKneeOutwardExcess) +
                        ", LoopPosition=" + Num(loopPositionDifferenceMax) +
                        ", LoopRotation=" + Num(loopRotationDifferenceMax) +
                        ", HoldSeconds=" + Num(finalPoseHoldDuration) +
                        ", HoldFrames=" + finalPoseHoldFrames.ToString(
                            CultureInfo.InvariantCulture) +
                        ", HoldPosition=" + Num(holdPositionDifferenceMax) +
                        ", HoldRotation=" + Num(holdRotationDifferenceMax) + ".");
                }

                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException(
                        "Player_Crouch_Enter review changed the scene dirty state.");
                }

                Debug.Log(
                    "[PlayerCrouchEnter] Captured " + metrics.mode + " crouch review." +
                    " Clip=" + clip.name +
                    ", Duration=" + Num(clip.length) +
                    ", FrameRate=" + Num(clip.frameRate) +
                    ", FramesPerLoop=" + framesPerLoop.ToString(
                        CultureInfo.InvariantCulture) +
                    ", Frames=" + totalFrames.ToString(
                        CultureInfo.InvariantCulture) +
                    ", Loops=" + loops.ToString(CultureInfo.InvariantCulture) +
                    ", RootDisplacement=" + Num(rootDisplacementMax) +
                    ", HipsVerticalRange=" + Num(metrics.hipsVerticalRange) +
                    ", EndLeftKneeOutward=" + Num(endLeftKneeOutwardExcess) +
                    ", EndLeftKneeBend=" + Num(endLeftKneeBendDegrees) +
                    ", FinalPoseHoldSeconds=" + Num(finalPoseHoldDuration) +
                    ", FinalPoseHoldFrames=" + finalPoseHoldFrames.ToString(
                        CultureInfo.InvariantCulture) +
                    ", HoldPositionDifference=" +
                    Num(holdPositionDifferenceMax) +
                    ", HoldRotationDifference=" +
                    Num(holdRotationDifferenceMax) +
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
                    "Player_Crouch_Enter has no usable forward direction.");
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

        private static void WriteMetrics(string path, ReviewMetrics metrics)
        {
            string absolutePath = Path.GetFullPath(path);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath) ??
                throw new InvalidOperationException(
                    "Player_Crouch_Enter metrics directory is unavailable."));
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
                    "Player_Crouch_Enter review metrics are missing.",
                    absolutePath);
            }

            ReviewMetrics metrics = JsonUtility.FromJson<ReviewMetrics>(
                File.ReadAllText(absolutePath, Encoding.UTF8));
            return metrics ?? throw new InvalidOperationException(
                "Player_Crouch_Enter review metrics could not be read.");
        }

        private static Dictionary<int, byte[]> LoadFrames(
            string directory,
            IReadOnlyCollection<int> indices,
            string view)
        {
            string absoluteDirectory = Path.GetFullPath(directory);
            Dictionary<int, byte[]> frames = new Dictionary<int, byte[]>();
            foreach (int index in indices)
            {
                string path = FramePath(absoluteDirectory, index, view);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        "A reviewed Player_Crouch_Enter frame is missing.",
                        path);
                }

                frames.Add(index, File.ReadAllBytes(path));
            }

            return frames;
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
                    "Player_Crouch_Enter phase frames are incomplete.");
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
                string absoluteOutputPath = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absoluteOutputPath) ??
                    throw new InvalidOperationException(
                        "Player_Crouch_Enter capture directory is unavailable."));
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
                    "Player_Crouch_Enter phase frame decoding failed.");
            }

            return texture;
        }

        private static int[] PhaseFrameIndices(
            int framesPerLoop,
            int finalPoseHoldFrames)
        {
            if (finalPoseHoldFrames > 0)
            {
                int sourceMotionFrames = framesPerLoop - finalPoseHoldFrames;
                return new[]
                {
                    0,
                    sourceMotionFrames / 2,
                    sourceMotionFrames - 1,
                    sourceMotionFrames,
                    framesPerLoop - 1
                };
            }

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

        private static float KneeOutwardExcess(
            Transform target,
            Transform hips,
            Transform upperLeg,
            Transform lowerLeg,
            Transform foot)
        {
            Vector3 lateral = target.right.normalized;
            float sideSign = Mathf.Sign(Vector3.Dot(
                upperLeg.position - hips.position,
                lateral));
            if (Mathf.Approximately(sideSign, 0f))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter leg side could not be determined directly.");
            }

            float thighLength = Vector3.Distance(
                upperLeg.position,
                lowerLeg.position);
            float shinLength = Vector3.Distance(
                lowerLeg.position,
                foot.position);
            float ratio = thighLength / Mathf.Max(
                thighLength + shinLength,
                0.000001f);
            float hipLateral = sideSign * Vector3.Dot(
                upperLeg.position - hips.position,
                lateral);
            float kneeLateral = sideSign * Vector3.Dot(
                lowerLeg.position - hips.position,
                lateral);
            float ankleLateral = sideSign * Vector3.Dot(
                foot.position - hips.position,
                lateral);
            float alignedLateral = Mathf.Lerp(
                hipLateral,
                ankleLateral,
                ratio);
            return kneeLateral - alignedLateral;
        }

        private static float KneeBendDegrees(
            Transform upperLeg,
            Transform lowerLeg,
            Transform foot)
        {
            Vector3 thighToHip = upperLeg.position - lowerLeg.position;
            Vector3 shinToAnkle = foot.position - lowerLeg.position;
            return Vector3.Angle(thighToHip, shinToAnkle);
        }

        private static float VerticalRange(IReadOnlyCollection<Vector3> positions)
        {
            return positions.Max(position => position.y) -
                   positions.Min(position => position.y);
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                scene.path != PlayerCrouchEnterAnimationTool.ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for Player_Crouch_Enter review.");
            }

            return scene;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
