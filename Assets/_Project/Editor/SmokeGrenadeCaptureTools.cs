using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class SmokeGrenadeCaptureTools
    {
        private const int ReviewLayer = 31;
        private const float FollowPositionTolerance = 0.00001f;
        private const float FollowRotationTolerance = 0.05f;
        private const float FlightAlignmentTolerance = 8f;

        private static string captureKind;
        private static double startedAt;
        private static Texture2D[] panels;
        private static bool running;

        static SmokeGrenadeCaptureTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        internal static void CaptureFourStateDiagnostic()
        {
            Begin("FourStateDiagnostic", SmokeGrenadeSetupTools.FourStateDiagnosticPath, false);
        }

        internal static void CaptureThrowReleaseFlightDiagnostic()
        {
            Begin("FlightDiagnostic", SmokeGrenadeSetupTools.FlightDiagnosticPath, false);
        }

        internal static void CaptureFourStateFinal()
        {
            Begin("FourStateFinal", SmokeGrenadeSetupTools.FourStateFinalPath, true);
        }

        internal static void CaptureThrowReleaseFlightFinal()
        {
            Begin("FlightFinal", SmokeGrenadeSetupTools.FlightFinalPath, true);
        }

        private static void Begin(string kind, string outputPath, bool final)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("SmokeGrenade capture must begin in Edit Mode.");
            SmokeGrenadeSetupTools.RequireScene();
            if (final && File.Exists(SmokeGrenadeSetupTools.AbsolutePath(outputPath)))
                throw new InvalidOperationException(
                    "SmokeGrenade final capture already exists and cannot be overwritten: " + outputPath);
            Directory.CreateDirectory(SmokeGrenadeSetupTools.AbsolutePath(
                SmokeGrenadeSetupTools.ReviewFolder));
            File.WriteAllText(
                SmokeGrenadeSetupTools.AbsolutePath(SmokeGrenadeSetupTools.CaptureRequestPath),
                kind,
                Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || running) return;
            string request = SmokeGrenadeSetupTools.AbsolutePath(
                SmokeGrenadeSetupTools.CaptureRequestPath);
            if (!File.Exists(request)) return;
            captureKind = File.ReadAllText(request, Encoding.UTF8).Trim();
            if (!captureKind.StartsWith("FourState", StringComparison.Ordinal) &&
                !captureKind.StartsWith("Flight", StringComparison.Ordinal))
                return;
            panels = new Texture2D[8];
            startedAt = EditorApplication.timeSinceStartup;
            running = true;
            EditorApplication.update -= CaptureUpdate;
            EditorApplication.update += CaptureUpdate;
        }

        private static void CaptureUpdate()
        {
            try
            {
                if (!EditorApplication.isPlaying) return;
                if (captureKind.StartsWith("FourState", StringComparison.Ordinal))
                    CaptureFourStateUpdate();
                else CaptureFlightUpdate();
            }
            catch (Exception exception)
            {
                WriteFailure(exception);
                Debug.LogException(exception);
                Finish(false);
            }
        }

        private static void CaptureFourStateUpdate()
        {
            double elapsed = EditorApplication.timeSinceStartup - startedAt;
            Scene scene = SmokeGrenadeSetupTools.RequireScene();
            GameObject[] targets = SmokeGrenadeSetupTools.TargetNames
                .Select(name => SmokeGrenadeSetupTools.FindUnique(scene, name)).ToArray();
            bool ready = targets.All(target =>
            {
                Animator animator = SmokeGrenadeSetupTools.RequireAnimator(target);
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                return state.fullPathHash != 0 && state.normalizedTime >= 0.18f;
            });
            if (!ready && elapsed < 4d) return;
            if (!ready)
                throw new InvalidOperationException(
                    "The four SmokeGrenade animations did not begin naturally within four seconds.");

            for (int index = 0; index < targets.Length; index++)
            {
                GameObject target = targets[index];
                Transform prop = SmokeGrenadeSetupTools.RequireProp(target);
                Bounds full = RendererBounds(target.transform);
                if (!IsDescendantOf(prop, target.transform)) full.Encapsulate(RendererBounds(prop));
                Bounds grip = RendererBounds(prop);
                grip.Encapsulate(SmokeGrenadeSetupTools.RequirePath(
                    target.transform, SmokeGrenadeSetupTools.RightHandPath).position);
                grip.Expand(0.18f);
                Vector3 front = HorizontalForward(target.transform);
                panels[index] = CapturePanel(full, front, target.transform, prop);
                panels[4 + index] = CapturePanel(
                    grip,
                    (front + target.transform.right * 0.55f).normalized,
                    target.transform,
                    prop);
            }

            UnityConsoleDiagnostics.AssertNoErrors();
            WriteCompositeAndReport(CurrentOutputPath(), FourStateReport(targets, elapsed));
            Finish(true);
        }

        private static void CaptureFlightUpdate()
        {
            double elapsed = EditorApplication.timeSinceStartup - startedAt;
            Scene scene = SmokeGrenadeSetupTools.RequireScene();
            GameObject target = SmokeGrenadeSetupTools.FindUnique(
                scene, "SmokeGrenade_Throw_Release");
            SmokeGrenadeSetupTools.MotionProxy motion =
                SmokeGrenadeSetupTools.RequireMotionProxy(target, "ThrowRelease");
            Transform prop = motion.Prop;
            float flightDuration = Mathf.Max(0.1f, motion.ClipLength - motion.ReleaseTime);

            if (panels[0] == null && !motion.IsReleased && motion.ReleaseCount == 0 &&
                motion.CurrentClipTime >= Mathf.Max(0f, motion.ReleaseTime - 0.10f))
                panels[0] = CaptureFlightPanel(target, prop, true);
            if (panels[1] == null && motion.ReleaseCount == 1 && motion.IsReleased &&
                motion.FlightElapsed <= 0.05f)
                panels[1] = CaptureFlightPanel(target, prop, true);
            if (panels[2] == null && motion.ReleaseCount == 1 && motion.IsReleased &&
                motion.FlightElapsed >= flightDuration * 0.25f)
                panels[2] = CaptureFlightPanel(target, prop, false);
            if (panels[3] == null && motion.ReleaseCount == 1 && motion.IsReleased &&
                motion.FlightElapsed >= flightDuration * 0.50f)
                panels[3] = CaptureFlightPanel(target, prop, false);
            if (panels[4] == null && motion.ReleaseCount == 1 && motion.IsReleased &&
                motion.FlightElapsed >= flightDuration * 0.75f)
                panels[4] = CaptureFlightPanel(target, prop, false);
            if (panels[5] == null && motion.ReleaseCount == 1 && !motion.IsReleased &&
                motion.CurrentClipTime < motion.ReleaseTime)
                panels[5] = CaptureFlightPanel(target, prop, true);
            if (panels[6] == null && motion.ReleaseCount >= 2 && motion.IsReleased &&
                motion.FlightElapsed <= 0.05f)
                panels[6] = CaptureFlightPanel(target, prop, true);
            if (panels[7] == null && motion.ReleaseCount >= 2 && motion.IsReleased &&
                motion.FlightElapsed >= flightDuration * 0.50f)
                panels[7] = CaptureFlightPanel(target, prop, false);

            if (panels.All(panel => panel != null))
            {
                InspectFlightRuntime(motion);
                UnityConsoleDiagnostics.AssertNoErrors();
                WriteCompositeAndReport(CurrentOutputPath(), FlightReport(motion, elapsed));
                Finish(true);
                return;
            }
            if (elapsed >= 12d)
                throw new InvalidOperationException(
                    "SmokeGrenade flight direct review did not collect all eight natural-playback " +
                    "panels: " + string.Join(",", panels.Select(
                        (panel, index) => index + "=" + (panel != null))) +
                    "|releaseCount=" + motion.ReleaseCount +
                    "|resetCount=" + motion.ResetCount +
                    "|clipTime=" + SmokeGrenadeSetupTools.Num(motion.CurrentClipTime));
        }

        private static Texture2D CaptureFlightPanel(
            GameObject target,
            Transform prop,
            bool gripScale)
        {
            Bounds bounds;
            if (gripScale)
            {
                bounds = RendererBounds(prop);
                bounds.Encapsulate(SmokeGrenadeSetupTools.RequirePath(
                    target.transform, SmokeGrenadeSetupTools.RightHandPath).position);
                bounds.Expand(0.24f);
            }
            else
            {
                bounds = RendererBounds(target.transform);
                bounds.Encapsulate(RendererBounds(prop));
                bounds.Expand(0.25f);
            }
            return CapturePanel(bounds, HorizontalForward(target.transform), target.transform, prop);
        }

        private static void InspectFlightRuntime(SmokeGrenadeSetupTools.MotionProxy motion)
        {
            if (motion.ReleaseCount < 2 || motion.ResetCount < 1)
                throw new InvalidOperationException(
                    "Smoke grenade did not restore and repeat its throw twice.");
            if (motion.MaximumHeldPositionError > FollowPositionTolerance ||
                motion.MaximumReleaseHandoffPositionError > FollowPositionTolerance)
                throw new InvalidOperationException(
                    "Smoke grenade position error: held=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumHeldPositionError) +
                    ", handoff=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumReleaseHandoffPositionError) +
                    ", verticalDrop=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumReleaseHandoffVerticalDrop));
            if (motion.MaximumHeldRotationError > FollowRotationTolerance ||
                motion.MaximumReleaseHandoffRotationError > FollowRotationTolerance)
                throw new InvalidOperationException(
                    "Smoke grenade hand follow/handoff rotation error=" +
                    SmokeGrenadeSetupTools.Num(Mathf.Max(
                        motion.MaximumHeldRotationError,
                        motion.MaximumReleaseHandoffRotationError)));
            if (motion.MaximumReleaseHandoffVerticalDrop > FollowPositionTolerance)
                throw new InvalidOperationException(
                    "Smoke grenade dropped below the recorded handoff pose by " +
                    SmokeGrenadeSetupTools.Num(motion.MaximumReleaseHandoffVerticalDrop));
            if (motion.MaximumVelocityAlignmentError > FlightAlignmentTolerance)
                throw new InvalidOperationException(
                    "Smoke grenade flight-axis alignment error=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumVelocityAlignmentError));
            if (motion.ReleaseVelocity.sqrMagnitude <= 0.01f ||
                motion.SpinRadiansPerSecond <= 0.01f)
                throw new InvalidOperationException(
                    "Smoke grenade release velocity or natural spin is missing.");
        }

        private static string FourStateReport(GameObject[] targets, double elapsed)
        {
            var report = new StringBuilder()
                .AppendLine("SmokeGrenade four-state direct Play Mode review")
                .AppendLine("captureKind=" + captureKind)
                .AppendLine("verificationTargetAnimationManipulated=False")
                .AppendLine("naturalAnimatorPlayback=True")
                .AppendLine("panelOrder=IdleFull,AimFull,ReleaseFull,CancelFull,IdleGrip,AimGrip,ReleaseGrip,CancelGrip")
                .AppendLine("elapsedSeconds=" + SmokeGrenadeSetupTools.Num((float)elapsed));
            foreach (GameObject target in targets)
            {
                AnimatorStateInfo state = SmokeGrenadeSetupTools.RequireAnimator(target)
                    .GetCurrentAnimatorStateInfo(0);
                report.AppendLine(target.name + "|normalizedTime=" +
                    SmokeGrenadeSetupTools.Num(state.normalizedTime) +
                    "|loop=" + state.loop + "|rightHandParent=True");
            }
            return report.ToString();
        }

        private static string FlightReport(
            SmokeGrenadeSetupTools.MotionProxy motion,
            double elapsed)
        {
            return new StringBuilder()
                .AppendLine("SmokeGrenade Throw Release direct Play Mode review")
                .AppendLine("captureKind=" + captureKind)
                .AppendLine("verificationTargetAnimationManipulated=False")
                .AppendLine("naturalAnimatorAndRigidbodyPlayback=True")
                .AppendLine("releaseAtRecordedMaximumHandHeight=True")
                .AppendLine("handoffAtExactHeldPropPose=True")
                .AppendLine("gravity=True")
                .AppendLine("velocityAlignedTilt=True")
                .AppendLine("naturalSpin=True")
                .AppendLine("restoreAndRepeat=True")
                .AppendLine("releaseCount=" + motion.ReleaseCount)
                .AppendLine("resetCount=" + motion.ResetCount)
                .AppendLine("maximumHeldPositionError=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumHeldPositionError))
                .AppendLine("maximumHeldRotationErrorDegrees=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumHeldRotationError))
                .AppendLine("maximumHandoffPositionError=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumReleaseHandoffPositionError))
                .AppendLine("maximumHandoffRotationErrorDegrees=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumReleaseHandoffRotationError))
                .AppendLine("maximumHandoffVerticalDrop=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumReleaseHandoffVerticalDrop))
                .AppendLine("maximumVelocityAlignmentErrorDegrees=" +
                    SmokeGrenadeSetupTools.Num(motion.MaximumVelocityAlignmentError))
                .AppendLine("releaseVelocity=" + SmokeGrenadeSetupTools.Vec(motion.ReleaseVelocity))
                .AppendLine("spinRadiansPerSecond=" +
                    SmokeGrenadeSetupTools.Num(motion.SpinRadiansPerSecond))
                .AppendLine("elapsedSeconds=" + SmokeGrenadeSetupTools.Num((float)elapsed))
                .ToString();
        }

        private static string CurrentOutputPath()
        {
            switch (captureKind)
            {
                case "FourStateDiagnostic": return SmokeGrenadeSetupTools.FourStateDiagnosticPath;
                case "FlightDiagnostic": return SmokeGrenadeSetupTools.FlightDiagnosticPath;
                case "FourStateFinal": return SmokeGrenadeSetupTools.FourStateFinalPath;
                case "FlightFinal": return SmokeGrenadeSetupTools.FlightFinalPath;
                default: throw new InvalidOperationException("Unknown SmokeGrenade capture kind: " + captureKind);
            }
        }

        private static void WriteCompositeAndReport(string imagePath, string report)
        {
            var composite = new Texture2D(2048, 1024, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < panels.Length; index++)
                {
                    int column = index % 4;
                    int row = 1 - index / 4;
                    composite.SetPixels32(
                        column * 512, row * 512, 512, 512, panels[index].GetPixels32());
                }
                composite.Apply(false, false);
                string absoluteImage = SmokeGrenadeSetupTools.AbsolutePath(imagePath);
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteImage) ??
                    throw new InvalidOperationException("Capture directory is unavailable."));
                File.WriteAllBytes(absoluteImage, composite.EncodeToPNG());
                File.WriteAllText(
                    Path.ChangeExtension(absoluteImage, ".txt"), report, Encoding.UTF8);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static Texture2D CapturePanel(
            Bounds bounds,
            Vector3 viewDirection,
            params Transform[] visibleRoots)
        {
            Transform[] hierarchy = visibleRoots
                .Where(root => root != null)
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Distinct()
                .ToArray();
            int[] originalLayers = hierarchy.Select(item => item.gameObject.layer).ToArray();
            GameObject cameraObject = new GameObject("SmokeGrenade_Review_Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject lightObject = new GameObject("SmokeGrenade_Review_Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                foreach (Transform item in hierarchy) item.gameObject.layer = ReviewLayer;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.36f, 0.39f, 0.42f, 1f);
                camera.cullingMask = 1 << ReviewLayer;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    0.16f,
                    Mathf.Max(bounds.extents.y * 1.18f,
                        Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.18f));
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 40f;
                Vector3 direction = viewDirection.sqrMagnitude > 0.5f
                    ? viewDirection.normalized
                    : Vector3.forward;
                camera.transform.position = bounds.center + direction *
                    Mathf.Max(2f, bounds.extents.magnitude * 4f);
                camera.transform.LookAt(bounds.center, Vector3.up);
                camera.targetTexture = render;

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.1f;
                light.color = Color.white;
                light.cullingMask = 1 << ReviewLayer;
                light.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);

                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                for (int index = 0; index < hierarchy.Length; index++)
                    hierarchy[index].gameObject.layer = originalLayers[index];
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Bounds RendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Vector3 HorizontalForward(Transform target)
        {
            Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            return forward.sqrMagnitude > 0.5f ? forward : Vector3.forward;
        }

        private static bool IsDescendantOf(Transform item, Transform ancestor)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (current == ancestor) return true;
            return false;
        }

        private static void WriteFailure(Exception exception)
        {
            string path = SmokeGrenadeSetupTools.AbsolutePath(
                SmokeGrenadeSetupTools.CaptureFailurePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Failure directory is unavailable."));
            File.WriteAllText(path, exception.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void Finish(bool succeeded)
        {
            EditorApplication.update -= CaptureUpdate;
            running = false;
            if (panels != null)
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            }
            panels = null;
            string request = SmokeGrenadeSetupTools.AbsolutePath(
                SmokeGrenadeSetupTools.CaptureRequestPath);
            if (File.Exists(request)) File.WriteAllText(request, string.Empty, Encoding.UTF8);
            if (succeeded) Debug.Log("[SmokeGrenade] Direct " + captureKind + " capture passed.");
            captureKind = string.Empty;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
