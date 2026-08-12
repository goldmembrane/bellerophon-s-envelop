using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    [InitializeOnLoad]
    internal static class AtaPistolTriggerFollowPlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_04_PistolAimAndFire";
        private const string ModelName = "Ata_Model";
        private const string PistolRootName = "Ata_Pistol_Transfer";
        private const string HandAnchorName = "Ata_Pistol_RightHandAnchor";
        private const string RecoilRotationAnchorName = "Ata_Pistol_ShootingRecoilRotationAnchor";
        private const string MuzzleFlashName = "Ata_Pistol_MuzzleFlash";
        private const string HeadPath =
            "Armature/Hips/Spine02/Spine01/Spine/neck/Head";
        private const string AimStateName = "PistolAimAndFire";
        private const string ShootingStateName = "PistolShooting";
        private const string ReviewCameraName = "Model Cam";
        private const string StateFileName = "AtaPistolTriggerFollowCapture.state";
        private const string ResultFileName = "AtaPistolTriggerFollowCapture.result";
        private const string StateEnteringPlayMode = "EnteringPlayMode";
        private const string StateExitingPlayMode = "ExitingPlayMode";
        private const string StateFailedExitingPlayMode = "FailedExitingPlayMode";
        private const int CaptureWidth = 720;
        private const int CaptureHeight = 720;
        private const int CaptureFrameRate = 20;
        private const int CaptureLayer = 31;
        private const float AimToShootingTransitionSeconds = 0.5f;
        private const float ShootingToStartTransitionSeconds = 0.05f;
        private const float ShootingStateDurationSeconds = 3f;
        private const float ShootingExitNormalized = 3f;

        private static Action<string> complete;
        private static Action<Exception> fail;
        private static CaptureSession session;

        static AtaPistolTriggerFollowPlayModeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Start(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Cannot start the Ata pistol capture while Unity is entering or running Play Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Current active scene must be CargoRunMvp. ActiveScene=" + scene.path);
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the Ata pistol Play Mode capture.");
            }

            DeleteStateFiles();
            var outputDirectory = Path.Combine(
                ProjectRoot,
                "docs",
                "validation",
                "ata_pistol_shooting_sequence_2026-08-12",
                "actual_playmode_motion");
            Directory.CreateDirectory(outputDirectory);
            var videoPath = Path.Combine(
                outputDirectory,
                "Ata_04_PistolShootingSequence_TwoLoops.mp4");
            var metricsPath = Path.Combine(
                outputDirectory,
                "Ata_04_PistolShootingSequence_Frames.csv");
            TryDelete(videoPath);
            TryDelete(metricsPath);
            WriteState(new CaptureState
            {
                Phase = StateEnteringPlayMode,
                VideoPath = videoPath,
                MetricsPath = metricsPath,
                StartedUtcTicks = DateTime.UtcNow.Ticks
            });
            EditorApplication.EnterPlaymode();
        }

        public static void Resume(Action<string> completeCallback, Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            Tick();
        }

        private static void Tick()
        {
            if (complete == null && fail == null) return;
            var state = ReadState();
            if (state == null) return;
            try
            {
                if (state.Phase == StateEnteringPlayMode)
                {
                    if (!EditorApplication.isPlaying) return;
                    session ??= new CaptureSession(state.VideoPath, state.MetricsPath);
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (!session.Tick()) return;
                    state.Summary = session.Complete();
                    File.WriteAllText(ResultPath, state.Summary, new UTF8Encoding(false));
                    session.Dispose();
                    session = null;
                    state.Phase = StateExitingPlayMode;
                    WriteState(state);
                    EditorApplication.ExitPlaymode();
                    return;
                }

                if (state.Phase == StateExitingPlayMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CompleteFromState(state);
                    return;
                }

                if (state.Phase == StateFailedExitingPlayMode &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    FailFromState(state);
                }
            }
            catch (Exception exception)
            {
                session?.Dispose();
                session = null;
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    state.Phase = StateFailedExitingPlayMode;
                    state.Error = exception.ToString();
                    WriteState(state);
                    if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                }
                else
                {
                    var callback = fail;
                    CleanupCallbacks();
                    DeleteStateFiles();
                    callback?.Invoke(exception);
                }
            }
        }

        private sealed class CaptureSession : IDisposable
        {
            private readonly Transform slot;
            private readonly Transform model;
            private readonly GameObject placementObject;
            private readonly Transform pistol;
            private readonly Transform handAnchor;
            private readonly Transform recoilRotationAnchor;
            private readonly Transform head;
            private readonly Transform reviewCameraTransform;
            private readonly Camera reviewCamera;
            private readonly Transform muzzleFlash;
            private readonly Animator animator;
            private readonly bool placementWasActive;
            private readonly bool slotWasActive;
            private readonly bool modelWasActive;
            private readonly AnimatorCullingMode previousCullingMode;
            private readonly bool previousApplyRootMotion;
            private readonly float previousAnimatorSpeed;
            private readonly LayerState[] layerStates;
            private readonly GameObject cameraObject;
            private readonly GameObject keyLightObject;
            private readonly GameObject fillLightObject;
            private readonly Camera camera;
            private readonly RenderTexture renderTexture;
            private readonly Texture2D readback;
            private readonly string rawVideoPath;
            private readonly string videoPath;
            private readonly string metricsPath;
            private readonly FileStream rawVideo;
            private readonly StringBuilder metrics = new StringBuilder();
            private readonly float sequenceLength;
            private readonly int aimStateHash;
            private readonly int shootingStateHash;
            private readonly double initializedAt;
            private double startedAt;
            private double nextCaptureAt;
            private int capturedFrames;
            private float maximumHeldPivotDistance;
            private float maximumHeldPivotAngle;
            private float maximumFlashMuzzleToScreenForwardAngle;
            private float maximumFlashToMuzzleAngle;
            private float maximumShootingFrameRotationDelta;
            private Quaternion previousPistolRotation;
            private bool hasPreviousPistolRotation;
            private int previousCapturedStateHash;
            private int completedSequences;
            private int lastStateHash;
            private int flashVisibleFrames;
            private int flashEvents;
            private bool flashWasVisible;
            private bool captureStarted;

            public CaptureSession(string videoPath, string metricsPath)
            {
                this.videoPath = videoPath;
                this.metricsPath = metricsPath;
                var scene = SceneManager.GetActiveScene();
                if (!scene.IsValid() || scene.path != ScenePath)
                {
                    throw new InvalidOperationException(
                        "Play Mode active scene must stay CargoRunMvp. ActiveScene=" + scene.path);
                }

                placementObject = scene.GetRootGameObjects()
                    .SingleOrDefault(item => item.name == PlacementRootName) ??
                    throw new InvalidOperationException("Missing placement root: " + PlacementRootName);
                placementWasActive = placementObject.activeSelf;
                placementObject.SetActive(true);
                slot = placementObject.transform.Find(SlotName) ??
                       throw new InvalidOperationException("Missing Ata pistol slot: " + SlotName);
                slotWasActive = slot.gameObject.activeSelf;
                slot.gameObject.SetActive(true);
                model = slot.Find(ModelName) ??
                        throw new InvalidOperationException("Missing Ata pistol model: " + ModelName);
                modelWasActive = model.gameObject.activeSelf;
                model.gameObject.SetActive(true);
                pistol = model.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == PistolRootName) ??
                         throw new InvalidOperationException("The runtime Ata pistol root is missing.");
                handAnchor = model.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == HandAnchorName) ??
                             throw new InvalidOperationException("The runtime Ata hand anchor is missing.");
                recoilRotationAnchor = model.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == RecoilRotationAnchorName) ??
                    throw new InvalidOperationException(
                        "The runtime Ata shooting recoil rotation anchor is missing.");
                head = model.Find(HeadPath) ??
                       throw new InvalidOperationException("The runtime Ata Head bone is missing.");
                reviewCamera = GameObject.Find(ReviewCameraName)?
                    .GetComponent<Camera>() ??
                    throw new InvalidOperationException(
                        "The runtime Ata review camera is missing: " + ReviewCameraName);
                reviewCameraTransform = reviewCamera.transform;
                muzzleFlash = pistol.Find(MuzzleFlashName) ??
                              throw new InvalidOperationException(
                                  "The runtime Ata pistol muzzle flash is missing.");
                animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException("The runtime Ata pistol Animator is missing.");
                var clips = animator.runtimeAnimatorController?.animationClips
                    .Where(item => item != null)
                    .Distinct()
                    .ToArray() ?? Array.Empty<AnimationClip>();
                if (clips.Length != 2)
                {
                    throw new InvalidOperationException(
                        "The Ata pistol controller must expose the aim and shooting clips.");
                }

                sequenceLength = clips.Max(clip => clip.length) +
                                 AimToShootingTransitionSeconds +
                                 ShootingStateDurationSeconds +
                                 ShootingToStartTransitionSeconds;
                aimStateHash = Animator.StringToHash(AimStateName);
                shootingStateHash = Animator.StringToHash(ShootingStateName);
                layerStates = SetLayerRecursively(slot, CaptureLayer);
                previousCullingMode = animator.cullingMode;
                previousApplyRootMotion = animator.applyRootMotion;
                previousAnimatorSpeed = animator.speed;
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                animator.speed = 0f;
                foreach (var renderer in slot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                    renderer.forceMatrixRecalculationPerRender = true;
                }

                cameraObject = new GameObject("Ata Pistol Actual Play Mode Camera")
                    { hideFlags = HideFlags.DontSave };
                camera = cameraObject.AddComponent<Camera>();
                camera.CopyFrom(reviewCamera);
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.024f, 0.03f, 1f);
                camera.transform.SetPositionAndRotation(
                    reviewCameraTransform.position,
                    reviewCameraTransform.rotation);

                keyLightObject = CreateLight(
                    "Ata Pistol Capture Key Light",
                    Quaternion.Euler(35f, 25f, 0f),
                    1.8f);
                fillLightObject = CreateLight(
                    "Ata Pistol Capture Fill Light",
                    Quaternion.Euler(15f, -130f, 0f),
                    0.9f);
                renderTexture = new RenderTexture(
                    CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                var captureBounds = CalculateBounds(model);
                // Use a three-quarter front view: the face direction, barrel axis,
                // trigger, and downward grip remain visible in the same frame.
                var threeQuarterCameraLook = Quaternion.AngleAxis(55f, model.up) *
                                             -model.forward;
                FrameAtaFrontCamera(camera, captureBounds, threeQuarterCameraLook);
                readback = new Texture2D(
                    CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                rawVideoPath = Path.ChangeExtension(videoPath, ".rgb24");
                TryDelete(rawVideoPath);
                rawVideo = new FileStream(
                    rawVideoPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    1024 * 1024,
                    FileOptions.SequentialScan);
                metrics.AppendLine(
                    "frame,elapsedSeconds,sequence,state,stateNormalizedTime,inTransition," +
                    "transitionNormalizedTime,nextState," +
                    "pistolToHandAnchorDistance,pistolToHandAnchorAngle," +
                    "pistolFrameRotationDelta,muzzleToScreenForwardAngle," +
                    "flashToMuzzleAngle,muzzleFlashVisible");
                initializedAt = Time.realtimeSinceStartupAsDouble;
            }

            public bool Tick()
            {
                var now = Time.realtimeSinceStartupAsDouble;
                if (!captureStarted)
                {
                    if (now - initializedAt < 0.5d) return false;
                    animator.Play(aimStateHash, 0, 0f);
                    animator.Update(0f);
                    lastStateHash = aimStateHash;
                    animator.speed = 1f;
                    captureStarted = true;
                    startedAt = now;
                    nextCaptureAt = now;
                }

                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (lastStateHash == shootingStateHash && state.shortNameHash == aimStateHash)
                {
                    completedSequences++;
                }
                lastStateHash = state.shortNameHash;
                if (capturedFrames > 0 && completedSequences >= 2) return true;
                if (now - startedAt > Math.Max(30d, sequenceLength * 3d))
                {
                    throw new TimeoutException(
                        "The actual Ata Animator did not complete two loops in time.");
                }

                if (now + 0.0001d < nextCaptureAt) return false;
                CaptureFrame(now - startedAt, state);
                nextCaptureAt += 1d / CaptureFrameRate;
                if (nextCaptureAt < now - 0.1d) nextCaptureAt = now + 1d / CaptureFrameRate;
                return false;
            }

            public string Complete()
            {
                rawVideo.Flush();
                rawVideo.Dispose();
                EncodeRawVideo();
                File.WriteAllText(metricsPath, metrics.ToString(), new UTF8Encoding(false));
                if (!File.Exists(videoPath) || new FileInfo(videoPath).Length < 1024L)
                {
                    throw new InvalidOperationException(
                        "The actual Ata pistol Play Mode video was not encoded.");
                }

                return "ActualPlayMode=True, IsolatedAtaSlot4=True, ContinuousAnimator=True" +
                       ", Sequences=" + completedSequences +
                       ", CapturedFrames=" + capturedFrames +
                       ", MaximumHeldPivotDistance=" +
                       maximumHeldPivotDistance.ToString("0.######", CultureInfo.InvariantCulture) +
                       ", MaximumHeldPivotAngle=" +
                       maximumHeldPivotAngle.ToString("0.######", CultureInfo.InvariantCulture) +
                       ", MaximumFlashMuzzleToAtaGazeAngle=" +
                       maximumFlashMuzzleToScreenForwardAngle.ToString("0.######", CultureInfo.InvariantCulture) +
                       ", MaximumFlashToMuzzleAngle=" +
                       maximumFlashToMuzzleAngle.ToString("0.######", CultureInfo.InvariantCulture) +
                       ", MaximumShootingFrameRotationDelta=" +
                       maximumShootingFrameRotationDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                       ", MuzzleFlashEvents=" + flashEvents +
                       ", MuzzleFlashVisibleFrames=" + flashVisibleFrames +
                       ", Video=" + videoPath +
                       ", Metrics=" + metricsPath;
            }

            private void CaptureFrame(double elapsed, AnimatorStateInfo state)
            {
                camera.Render();
                var oldActive = RenderTexture.active;
                RenderTexture.active = renderTexture;
                readback.ReadPixels(
                    new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0, false);
                readback.Apply(false, false);
                RenderTexture.active = oldActive;
                var pixels = readback.GetRawTextureData<byte>();
                rawVideo.Write(pixels.ToArray(), 0, pixels.Length);

                var phase = Mathf.Clamp01(state.normalizedTime);
                var distance = Vector3.Distance(pistol.position, handAnchor.position);
                var expectedRotation = state.shortNameHash == shootingStateHash
                    ? ShootingAimRotation()
                    : handAnchor.rotation;
                var angle = Quaternion.Angle(pistol.rotation, expectedRotation);
                var muzzleDirection = muzzleFlash.TransformDirection(Vector3.forward);
                var pistolFrameRotationDelta = hasPreviousPistolRotation
                    ? Quaternion.Angle(previousPistolRotation, pistol.rotation)
                    : 0f;
                var shootingCyclePhase = Mathf.Repeat(state.normalizedTime, 1f);
                if (state.shortNameHash == shootingStateHash &&
                    previousCapturedStateHash == shootingStateHash &&
                    shootingCyclePhase >= 0.20f &&
                    shootingCyclePhase <= 0.50f)
                {
                    maximumShootingFrameRotationDelta = Mathf.Max(
                        maximumShootingFrameRotationDelta,
                        pistolFrameRotationDelta);
                }
                previousPistolRotation = pistol.rotation;
                hasPreviousPistolRotation = true;
                previousCapturedStateHash = state.shortNameHash;
                var muzzleToScreenForwardAngle = Vector3.Angle(
                    muzzleDirection,
                    ShootingForwardDirection());
                var flashToMuzzleAngle = Vector3.Angle(
                    muzzleFlash.TransformDirection(Vector3.forward),
                    muzzleDirection);
                var held =
                    (state.shortNameHash == aimStateHash && phase >= 0.34f) ||
                    (state.shortNameHash == shootingStateHash &&
                     state.normalizedTime < ShootingExitNormalized - 0.01f);
                if (held)
                {
                    maximumHeldPivotDistance = Mathf.Max(maximumHeldPivotDistance, distance);
                    maximumHeldPivotAngle = Mathf.Max(maximumHeldPivotAngle, angle);
                }
                var flashVisible = muzzleFlash.localScale.sqrMagnitude > 0.5f;
                if (flashVisible && !flashWasVisible) flashEvents++;
                if (flashVisible)
                {
                    flashVisibleFrames++;
                    maximumFlashMuzzleToScreenForwardAngle = Mathf.Max(
                        maximumFlashMuzzleToScreenForwardAngle,
                        muzzleToScreenForwardAngle);
                    maximumFlashToMuzzleAngle = Mathf.Max(
                        maximumFlashToMuzzleAngle,
                        flashToMuzzleAngle);
                }
                flashWasVisible = flashVisible;
                var stateName = state.shortNameHash == shootingStateHash
                    ? ShootingStateName
                    : state.shortNameHash == aimStateHash
                        ? AimStateName
                        : "Unknown";
                var inTransition = animator.IsInTransition(0);
                var transitionNormalizedTime = inTransition
                    ? animator.GetAnimatorTransitionInfo(0).normalizedTime
                    : 0f;
                var nextStateInfo = inTransition
                    ? animator.GetNextAnimatorStateInfo(0)
                    : default;
                var nextStateName = nextStateInfo.shortNameHash == shootingStateHash
                    ? ShootingStateName
                    : nextStateInfo.shortNameHash == aimStateHash
                        ? AimStateName
                        : string.Empty;

                metrics.Append(capturedFrames.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(elapsed.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(completedSequences.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(stateName).Append(',')
                    .Append(state.normalizedTime.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(inTransition ? "1" : "0").Append(',')
                    .Append(transitionNormalizedTime.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(nextStateName).Append(',')
                    .Append(distance.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(angle.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(pistolFrameRotationDelta.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(muzzleToScreenForwardAngle.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(flashToMuzzleAngle.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(flashVisible ? "1" : "0").AppendLine();
                capturedFrames++;
            }

            private Quaternion ShootingAimRotation()
            {
                var up = model.up.normalized;
                var forward = ShootingForwardDirection();
                return forward.sqrMagnitude >= 0.999f && up.sqrMagnitude >= 0.999f
                    ? Quaternion.LookRotation(forward, up) *
                      Quaternion.Inverse(muzzleFlash.localRotation)
                    : recoilRotationAnchor.rotation;
            }

            private Vector3 ShootingForwardDirection()
            {
                return Vector3.ProjectOnPlane(model.forward, model.up).normalized;
            }

            private void EncodeRawVideo()
            {
                var frameRate = CaptureFrameRate;
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = "-y -f rawvideo -pixel_format rgb24 -video_size " +
                                CaptureWidth + "x" + CaptureHeight + " -framerate " +
                                frameRate.ToString("0.######", CultureInfo.InvariantCulture) +
                                " -i \"" + rawVideoPath + "\" -an -vf vflip -c:v libx264 " +
                                "-preset fast -crf 18 -pix_fmt yuv420p \"" + videoPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo) ??
                                    throw new InvalidOperationException(
                                        "ffmpeg could not encode the Ata pistol Play Mode frames.");
                if (!process.WaitForExit(30000))
                {
                    process.Kill();
                    throw new TimeoutException(
                        "ffmpeg did not finish the Ata pistol Play Mode video.");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "ffmpeg failed for the Ata pistol Play Mode video. ExitCode=" +
                        process.ExitCode);
                }

                TryDelete(rawVideoPath);
            }

            public void Dispose()
            {
                rawVideo?.Dispose();
                if (camera != null) camera.targetTexture = null;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.Destroy(renderTexture);
                }

                if (readback != null) UnityEngine.Object.Destroy(readback);
                if (fillLightObject != null) UnityEngine.Object.Destroy(fillLightObject);
                if (keyLightObject != null) UnityEngine.Object.Destroy(keyLightObject);
                if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
                if (animator != null)
                {
                    animator.cullingMode = previousCullingMode;
                    animator.applyRootMotion = previousApplyRootMotion;
                    animator.speed = previousAnimatorSpeed;
                }

                if (model != null) model.gameObject.SetActive(modelWasActive);
                if (slot != null) slot.gameObject.SetActive(slotWasActive);
                RestoreLayers(layerStates);
                if (placementObject != null) placementObject.SetActive(placementWasActive);
            }
        }

        private static GameObject CreateLight(
            string name,
            Quaternion rotation,
            float intensity)
        {
            var lightObject = new GameObject(name) { hideFlags = HideFlags.DontSave };
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.cullingMask = 1 << CaptureLayer;
            lightObject.transform.rotation = rotation;
            return lightObject;
        }

        private static Bounds CalculateBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled)
                .ToArray();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.one);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void FrameAtaFrontCamera(
            Camera camera,
            Bounds bounds,
            Vector3 cameraForward)
        {
            cameraForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
            if (cameraForward.sqrMagnitude < 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata model forward cannot define the front-view capture camera.");
            }

            camera.orthographic = false;
            camera.aspect = CaptureWidth / (float)CaptureHeight;
            camera.fieldOfView = 34f;
            var halfVerticalFov = camera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            var distance = bounds.extents.magnitude /
                           Mathf.Max(0.01f, Mathf.Tan(halfVerticalFov)) * 0.82f;
            camera.transform.SetPositionAndRotation(
                bounds.center - cameraForward * distance,
                Quaternion.LookRotation(cameraForward, Vector3.up));
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = Mathf.Max(40f, distance + bounds.size.magnitude * 2f);
        }

        private static LayerState[] SetLayerRecursively(Transform root, int layer)
        {
            var states = root.GetComponentsInChildren<Transform>(true)
                .Select(item => new LayerState(item.gameObject, item.gameObject.layer))
                .ToArray();
            foreach (var state in states) state.GameObject.layer = layer;
            return states;
        }

        private static void RestoreLayers(IEnumerable<LayerState> states)
        {
            foreach (var state in states)
            {
                if (state.GameObject != null) state.GameObject.layer = state.Value;
            }
        }

        private static void CompleteFromState(CaptureState state)
        {
            var summary = File.Exists(ResultPath)
                ? File.ReadAllText(ResultPath)
                : state.Summary;
            var callback = complete;
            CleanupCallbacks();
            DeleteStateFiles();
            callback?.Invoke(
                "Ata pistol actual Play Mode two-loop capture completed. " + summary);
        }

        private static void FailFromState(CaptureState state)
        {
            var callback = fail;
            var error = string.IsNullOrWhiteSpace(state.Error)
                ? "Ata pistol actual Play Mode capture failed."
                : state.Error;
            CleanupCallbacks();
            DeleteStateFiles();
            callback?.Invoke(new InvalidOperationException(error));
        }

        private static void CleanupCallbacks()
        {
            complete = null;
            fail = null;
        }

        private static CaptureState ReadState()
        {
            if (!File.Exists(StatePath)) return null;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(StatePath))
            {
                var split = line.IndexOf('=');
                if (split >= 0) values[line.Substring(0, split)] = line.Substring(split + 1);
            }

            return new CaptureState
            {
                Phase = Get(values, "phase"),
                VideoPath = Get(values, "videoPath"),
                MetricsPath = Get(values, "metricsPath"),
                Summary = Get(values, "summary"),
                Error = Get(values, "error"),
                StartedUtcTicks = long.TryParse(Get(values, "startedUtcTicks"), out var ticks)
                    ? ticks
                    : 0L
            };
        }

        private static void WriteState(CaptureState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath));
            File.WriteAllLines(StatePath, new[]
            {
                "phase=" + state.Phase,
                "videoPath=" + state.VideoPath,
                "metricsPath=" + state.MetricsPath,
                "summary=" + state.Summary,
                "error=" + state.Error,
                "startedUtcTicks=" + state.StartedUtcTicks.ToString(CultureInfo.InvariantCulture)
            });
        }

        private static string Get(IDictionary<string, string> values, string key) =>
            values.TryGetValue(key, out var value) ? value : string.Empty;

        private static void DeleteStateFiles()
        {
            TryDelete(StatePath);
            TryDelete(ResultPath);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string StatePath =>
            Path.Combine(ProjectRoot, "Logs", StateFileName);

        private static string ResultPath =>
            Path.Combine(ProjectRoot, "Logs", ResultFileName);

        private sealed class CaptureState
        {
            public string Phase;
            public string VideoPath;
            public string MetricsPath;
            public string Summary;
            public string Error;
            public long StartedUtcTicks;
        }

        private readonly struct LayerState
        {
            public LayerState(GameObject gameObject, int value)
            {
                GameObject = gameObject;
                Value = value;
            }

            public GameObject GameObject { get; }
            public int Value { get; }
        }
    }
}
