using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class TurretEnterAnimationTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Turret_Enter";
        private const string ExitTargetName = "Turret_Exit";
        private const string IdleName = "Player_Idle";
        private const string ArmoryRootName = "Approved Armory 01 Shell";
        private const string ControlAssemblyName = "AR-05 U-yoke turret handle assembly";
        private const string LeftGripName = "AR-05 left vertical thumb grip";
        private const string RightGripName = "AR-05 right vertical thumb grip";
        private const string TempFolder = "Temp/TurretEnter";
        private const string AssetFolder = "Assets/_Project/Animations/TurretEnter";
        private const string ClipPath = AssetFolder + "/Turret_Enter_TwoHandGrip.anim";
        private const string ControllerPath = AssetFolder + "/Turret_Enter_TwoHandGrip.controller";
        private const string ExitTempFolder = "Temp/TurretExit";
        private const string ExitAssetFolder = "Assets/_Project/Animations/TurretExit";
        private const string ExitClipPath = ExitAssetFolder + "/Turret_Exit_Reverse.anim";
        private const string ExitControllerPath = ExitAssetFolder + "/Turret_Exit_Reverse.controller";
        private const string Spine = "Armature/Hips/Spine02/Spine01/Spine";
        private const string LeftUpper = Spine + "/LeftShoulder/LeftArm";
        private const string LeftFore = LeftUpper + "/LeftForeArm";
        private const string LeftHand = LeftFore + "/LeftHand";
        private const string RightUpper = Spine + "/RightShoulder/RightArm";
        private const string RightFore = RightUpper + "/RightForeArm";
        private const string RightHand = RightFore + "/RightHand";
        private const float Transition = 0.7f;
        private const float Hold = 0.5f;
        private const float Duration = Transition + Hold;
        private const int FrameRate = 60;
        private const int PreviewLayer = 31;
        private const int PanelSize = 440;
        private const int PanelGap = 8;

        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Transform target = Find(scene, TargetName);
            Transform idle = Find(scene, IdleName);
            Transform armory = Find(scene, ArmoryRootName);
            Geometry geometry = ReadGeometry(scene);
            Transform[] parts = armory.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name.StartsWith("AR-05", StringComparison.Ordinal))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            var report = new StringBuilder()
                .AppendLine("Turret_Enter source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("scene=" + scene.path)
                .AppendLine("targetPosition=" + Vec(target.position))
                .AppendLine("targetRotation=" + Quat(target.rotation))
                .AppendLine("targetScale=" + Vec(target.localScale))
                .AppendLine("idlePosition=" + Vec(idle.position))
                .AppendLine("leftGripPath=" + PathOf(armory, geometry.Left))
                .AppendLine("leftGripPosition=" + Vec(geometry.Left.position))
                .AppendLine("leftGripBounds=" + Vec(geometry.LeftBounds.size))
                .AppendLine("rightGripPath=" + PathOf(armory, geometry.Right))
                .AppendLine("rightGripPosition=" + Vec(geometry.Right.position))
                .AppendLine("rightGripBounds=" + Vec(geometry.RightBounds.size))
                .AppendLine("gripSpacingMeters=" + F(geometry.Spacing));
            foreach (Transform part in parts)
            {
                Renderer renderer = part.GetComponent<Renderer>();
                report.Append("part=").Append(PathOf(armory, part))
                    .Append("|position=").Append(Vec(part.position));
                if (renderer != null)
                    report.Append("|boundsCenter=").Append(Vec(renderer.bounds.center))
                        .Append("|boundsSize=").Append(Vec(renderer.bounds.size));
                report.AppendLine();
            }
            Write(TempFolder + "/SourceInspection.txt", report.ToString());
            Debug.Log("[TurretEnter] Sources inspected read-only.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Apply Turret Enter Two-Hand Grip")]
        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName).gameObject;
            Animator animator = RequireAnimator(target);
            AnimationClip idle = DefaultClip(RequireAnimator(Find(scene, IdleName).gameObject), IdleName);
            Geometry geometry = ReadGeometry(scene);
            RootState targetRoot = new RootState(target.transform);
            RootState leftControl = new RootState(geometry.Left);
            RootState rightControl = new RootState(geometry.Right);

            Pose start;
            Pose grip;
            Metrics solved;
            BuildPoses(target, idle, geometry, out start, out grip, out solved);
            RequireMetrics(solved);
            EnsureFolder(AssetFolder);
            AnimationClip clip = CreateClip(start, grip);
            AnimatorController controller = CreateController(clip);
            Undo.RecordObject(animator, "Connect Turret_Enter two-hand control grip");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            targetRoot.RequireUnchanged(target.transform, TargetName + " root");
            leftControl.RequireUnchanged(geometry.Left, LeftGripName);
            rightControl.RequireUnchanged(geometry.Right, RightGripName);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Turret_Enter two-hand control grip application")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourcePose=Player_Idle frame 0")
                .AppendLine("sourcePoseGenerated=False")
                .AppendLine("leftHandTarget=" + LeftGripName)
                .AppendLine("rightHandTarget=" + RightGripName)
                .AppendLine("transitionSeconds=0.7")
                .AppendLine("holdSeconds=0.5")
                .AppendLine("instantReturnAfterHold=True")
                .AppendLine("loopTime=True")
                .AppendLine("controlTransformsChanged=False")
                .AppendLine("targetRootTransformChanged=False")
                .AppendLine("clip=" + ClipPath)
                .AppendLine("controller=" + ControllerPath)
                .AppendLine(solved.Describe());
            Write(TempFolder + "/Application.txt", report.ToString());
            Debug.Log("[TurretEnter] First implementation applied.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Apply Turret Exit Exact Reverse")]
        internal static void ApplyExit()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject sourceTarget = Find(scene, TargetName).gameObject;
            GameObject exitTarget = Find(scene, ExitTargetName).gameObject;
            Animator exitAnimator = RequireAnimator(exitTarget);
            AnimationClip sourceClip = DefaultClip(
                RequireAnimator(sourceTarget), TargetName);
            if (AssetDatabase.GetAssetPath(sourceClip) != ClipPath)
                throw new InvalidOperationException(
                    "Turret_Enter source clip differs before Turret_Exit copy.");
            if (Mathf.Abs(sourceClip.length - Duration) > 0.001f)
                throw new InvalidOperationException(
                    "Turret_Enter source duration differs: " + F(sourceClip.length));

            Geometry geometry = ReadGeometry(scene);
            RootState sourceRoot = new RootState(sourceTarget.transform);
            RootState exitRoot = new RootState(exitTarget.transform);
            RootState leftControl = new RootState(geometry.Left);
            RootState rightControl = new RootState(geometry.Right);
            EnsureFolder(ExitAssetFolder);
            AnimationClip exitClip = CreateExitClip(sourceClip);
            AnimatorController controller = CreateExitController(exitClip);
            Undo.RecordObject(exitAnimator, "Connect Turret_Exit exact reverse");
            exitAnimator.runtimeAnimatorController = controller;
            exitAnimator.applyRootMotion = false;
            exitAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            exitAnimator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(exitAnimator);
            EditorUtility.SetDirty(exitAnimator);

            sourceRoot.RequireUnchanged(sourceTarget.transform, TargetName + " root");
            exitRoot.RequireUnchanged(exitTarget.transform, ExitTargetName + " root");
            leftControl.RequireUnchanged(geometry.Left, LeftGripName);
            rightControl.RequireUnchanged(geometry.Right, RightGripName);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Turret_Exit exact reverse application")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourceClip=" + ClipPath)
                .AppendLine("sourceGenerated=False")
                .AppendLine("sourceModified=False")
                .AppendLine("reverseTransitionSeconds=0.7")
                .AppendLine("initialGripHoldSeconds=0")
                .AppendLine("playerIdleHoldSeconds=0.5")
                .AppendLine("instantReturnAfterIdleHold=True")
                .AppendLine("loopTime=True")
                .AppendLine("controlTransformsChanged=False")
                .AppendLine("sourceRootTransformChanged=False")
                .AppendLine("targetRootTransformChanged=False")
                .AppendLine("clip=" + ExitClipPath)
                .AppendLine("controller=" + ExitControllerPath);
            Write(ExitTempFolder + "/Application.txt", report.ToString());
            Debug.Log("[TurretExit] Exact reverse applied.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Turret Exit Exact Reverse")]
        internal static void InspectExitAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject sourceTarget = Find(scene, TargetName).gameObject;
            GameObject exitTarget = Find(scene, ExitTargetName).gameObject;
            AnimationClip sourceClip = DefaultClip(
                RequireAnimator(sourceTarget), TargetName);
            AnimationClip exitClip = DefaultClip(
                RequireAnimator(exitTarget), ExitTargetName);
            AnimationClip idleClip = DefaultClip(
                RequireAnimator(Find(scene, IdleName).gameObject), IdleName);
            if (AssetDatabase.GetAssetPath(sourceClip) != ClipPath)
                throw new InvalidOperationException("Turret_Enter source clip differs.");
            if (AssetDatabase.GetAssetPath(exitClip) != ExitClipPath)
                throw new InvalidOperationException("Turret_Exit reverse clip differs.");
            if (Mathf.Abs(exitClip.length - Duration) > 0.001f)
                throw new InvalidOperationException(
                    "Turret_Exit duration differs: " + F(exitClip.length));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(exitClip);
            if (!settings.loopTime)
                throw new InvalidOperationException("Turret_Exit loopTime is disabled.");

            Geometry geometry = ReadGeometry(scene);
            RootState sourceRoot = new RootState(sourceTarget.transform);
            RootState exitRoot = new RootState(exitTarget.transform);
            RootState leftControl = new RootState(geometry.Left);
            RootState rightControl = new RootState(geometry.Right);
            GameObject exitProbe = Clone(exitTarget, "TurretExit_InspectProbe", false);
            GameObject sourceProbe = Clone(exitTarget, "TurretExit_SourceProbe", false);
            GameObject idleProbe = Clone(exitTarget, "TurretExit_IdleProbe", false);
            try
            {
                float reverseRotationError = 0f;
                float reversePositionError = 0f;
                float reverseScaleError = 0f;
                int transitionFrames = Mathf.RoundToInt(Transition * FrameRate);
                for (int frame = 0; frame <= transitionFrames; frame++)
                {
                    float exitTime = frame / (float)FrameRate;
                    float sourceTime = Transition - exitTime;
                    Pose exitPose = SamplePose(exitProbe, exitClip, exitTime);
                    Pose sourcePose = SamplePose(sourceProbe, sourceClip, sourceTime);
                    reverseRotationError = Mathf.Max(
                        reverseRotationError, MaxRotation(exitPose, sourcePose));
                    reversePositionError = Mathf.Max(
                        reversePositionError,
                        MaxVector(exitPose.Position, sourcePose.Position));
                    reverseScaleError = Mathf.Max(
                        reverseScaleError,
                        MaxVector(exitPose.Scale, sourcePose.Scale));
                }

                Pose exitStart = SamplePose(exitProbe, exitClip, 0f);
                Pose sourceGrip = SamplePose(sourceProbe, sourceClip, Transition);
                Pose exitIdle = SamplePose(exitProbe, exitClip, Transition);
                Pose exitHeld = SamplePose(exitProbe, exitClip, Duration);
                Pose idlePose = SamplePose(idleProbe, idleClip, 0f);
                float startRotationError = MaxRotation(exitStart, sourceGrip);
                float startPositionError = MaxVector(
                    exitStart.Position, sourceGrip.Position);
                float startScaleError = MaxVector(exitStart.Scale, sourceGrip.Scale);
                float idleRotationError = MaxRotation(exitIdle, idlePose);
                float idlePositionError = MaxVector(exitIdle.Position, idlePose.Position);
                float idleScaleError = MaxVector(exitIdle.Scale, idlePose.Scale);
                float idleHoldRotationError = MaxRotation(exitIdle, exitHeld);
                float idleHoldPositionError = MaxVector(
                    exitIdle.Position, exitHeld.Position);
                float idleHoldScaleError = MaxVector(exitIdle.Scale, exitHeld.Scale);
                float firstFrameCurveDelta = MaxCurveDelta(
                    exitClip, 0f, 1f / FrameRate);

                if (startRotationError > 0.02f || startPositionError > 0.00001f ||
                    startScaleError > 0.00001f || reverseRotationError > 0.02f ||
                    reversePositionError > 0.00001f || reverseScaleError > 0.00001f ||
                    idleRotationError > 0.02f || idlePositionError > 0.00001f ||
                    idleScaleError > 0.00001f || idleHoldRotationError > 0.02f ||
                    idleHoldPositionError > 0.00001f || idleHoldScaleError > 0.00001f ||
                    firstFrameCurveDelta <= 0.0000001f)
                    throw new InvalidOperationException(
                        "Turret_Exit exact reverse inspection failed.");

                sourceRoot.RequireUnchanged(sourceTarget.transform, TargetName + " root");
                exitRoot.RequireUnchanged(exitTarget.transform, ExitTargetName + " root");
                leftControl.RequireUnchanged(geometry.Left, LeftGripName);
                rightControl.RequireUnchanged(geometry.Right, RightGripName);
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                var report = new StringBuilder()
                    .AppendLine("Turret_Exit exact reverse sampled inspection")
                    .AppendLine("verificationTargetManipulated=False")
                    .AppendLine("durationSeconds=" + F(exitClip.length))
                    .AppendLine("reverseTransitionSeconds=0.7")
                    .AppendLine("initialGripHoldSeconds=0")
                    .AppendLine("playerIdleHoldSeconds=0.5")
                    .AppendLine("loopTime=True")
                    .AppendLine("instantReturnAfterIdleHold=True")
                    .AppendLine("sourceClipModified=False")
                    .AppendLine("exitStartVsEnterGripRotationErrorDegrees=" + F(startRotationError))
                    .AppendLine("exitStartVsEnterGripPositionErrorMeters=" + F(startPositionError))
                    .AppendLine("exitStartVsEnterGripScaleError=" + F(startScaleError))
                    .AppendLine("reverseTransitionRotationErrorDegrees=" + F(reverseRotationError))
                    .AppendLine("reverseTransitionPositionErrorMeters=" + F(reversePositionError))
                    .AppendLine("reverseTransitionScaleError=" + F(reverseScaleError))
                    .AppendLine("exitIdleVsPlayerIdleRotationErrorDegrees=" + F(idleRotationError))
                    .AppendLine("exitIdleVsPlayerIdlePositionErrorMeters=" + F(idlePositionError))
                    .AppendLine("exitIdleVsPlayerIdleScaleError=" + F(idleScaleError))
                    .AppendLine("idleHoldRotationErrorDegrees=" + F(idleHoldRotationError))
                    .AppendLine("idleHoldPositionErrorMeters=" + F(idleHoldPositionError))
                    .AppendLine("idleHoldScaleError=" + F(idleHoldScaleError))
                    .AppendLine("firstFrameCurveDelta=" + F(firstFrameCurveDelta))
                    .AppendLine("controlTransformsChanged=False")
                    .AppendLine("sourceRootTransformChanged=False")
                    .AppendLine("targetRootTransformChanged=False")
                    .AppendLine("directVisualReviewPending=True");
                Write(ExitTempFolder + "/Verification.txt", report.ToString());
                Debug.Log("[TurretExit] Exact reverse inspection passed.\n" + report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(idleProbe);
                UnityEngine.Object.DestroyImmediate(sourceProbe);
                UnityEngine.Object.DestroyImmediate(exitProbe);
            }
        }

        [MenuItem("Bellerophon/Player/Capture Turret Exit Final")]
        internal static void CaptureExitFinal()
        {
            InspectExitAnimation();
            Scene scene = RequireScene();
            GameObject target = Find(scene, ExitTargetName).gameObject;
            AnimationClip clip = DefaultClip(
                RequireAnimator(target), ExitTargetName);
            Geometry geometry = ReadGeometry(scene);
            string imagePath = Absolute(ExitTempFolder + "/Final.png");
            string reportPath = Absolute(ExitTempFolder + "/Final.txt");
            if (File.Exists(imagePath) || File.Exists(reportPath))
                throw new InvalidOperationException(
                    "Turret_Exit final capture already exists.");

            RootState targetRoot = new RootState(target.transform);
            RootState leftControl = new RootState(geometry.Left);
            RootState rightControl = new RootState(geometry.Right);
            GameObject actor = Clone(target, "TurretExit_FinalActor", true);
            actor.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            Layer(actor, PreviewLayer);
            clip.SampleAnimation(actor, 0f);
            GameObject control = CreateControlPreview(scene, actor.transform, geometry);
            try
            {
                Texture2D[] panels =
                {
                    Render(actor, control, clip, 0f, true, true, 1f),
                    Render(actor, control, clip, Transition * 0.5f, true, false),
                    Render(actor, control, clip, Transition, false, false),
                    Render(actor, control, clip, Duration, true, false)
                };
                Texture2D sheet = Combine(panels);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                    File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
                Write(ExitTempFolder + "/Final.txt",
                    "Turret_Exit final direct contact sheet\n" +
                    "verificationTargetManipulated=False\n" +
                    "actualCharacterClone=True\n" +
                    "actualControlMeshesClonedForReadOnlyPreview=True\n" +
                    "panels=0.0s-grip,0.35s-reverseTransition,0.7s-playerIdle,1.2s-playerIdleHold\n" +
                    "sourceAnimationCopiedExactly=True\n" +
                    "reverseTransitionSeconds=0.7\n" +
                    "initialGripHoldSeconds=0\n" +
                    "playerIdleHoldSeconds=0.5\n" +
                    "directVisualReviewPrimary=True\n" +
                    "numericMetricsSecondary=True\n");
                targetRoot.RequireUnchanged(target.transform, ExitTargetName + " root");
                leftControl.RequireUnchanged(geometry.Left, LeftGripName);
                rightControl.RequireUnchanged(geometry.Right, RightGripName);
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                Debug.Log("[TurretExit] Final direct contact sheet captured once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(control);
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        [MenuItem("Bellerophon/Player/Inspect Turret Enter Two-Hand Grip")]
        internal static void InspectAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName).gameObject;
            Animator animator = RequireAnimator(target);
            AnimationClip clip = DefaultClip(animator, TargetName);
            if (AssetDatabase.GetAssetPath(clip) != ClipPath)
            {
                InspectSources();
                return;
            }
            if (Mathf.Abs(clip.length - Duration) > 0.001f)
                throw new InvalidOperationException("Turret_Enter duration differs: " + F(clip.length));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
                throw new InvalidOperationException("Turret_Enter loopTime is disabled.");

            Geometry geometry = ReadGeometry(scene);
            RootState targetRoot = new RootState(target.transform);
            RootState leftControl = new RootState(geometry.Left);
            RootState rightControl = new RootState(geometry.Right);
            GameObject probe = Clone(target, "TurretEnter_InspectProbe", false);
            GameObject idleProbe = Clone(target, "TurretEnter_IdleProbe", false);
            try
            {
                Pose start = SamplePose(probe, clip, 0f);
                Pose grip = SamplePose(probe, clip, Transition);
                Pose held = SamplePose(probe, clip, Duration);
                AnimationClip idle = DefaultClip(
                    RequireAnimator(Find(scene, IdleName).gameObject), IdleName);
                Pose idleStart = SamplePose(idleProbe, idle, 0f);
                float startRotationError = MaxRotation(start, idleStart);
                float startPositionError = MaxVector(start.Position, idleStart.Position);
                float startScaleError = MaxVector(start.Scale, idleStart.Scale);
                float holdRotationError = MaxRotation(grip, held);
                float unrelatedRotation = MaxUnrelatedRotation(start, grip);
                Metrics metrics = Measure(target, clip, geometry);
                RequireMetrics(metrics);
                if (startRotationError > 0.02f || startPositionError > 0.00001f ||
                    startScaleError > 0.00001f || holdRotationError > 0.02f ||
                    unrelatedRotation > 0.02f)
                    throw new InvalidOperationException(
                        "Turret_Enter pose preservation failed. startRotation=" +
                        F(startRotationError) + "; startPosition=" + F(startPositionError) +
                        "; startScale=" + F(startScaleError) + "; hold=" +
                        F(holdRotationError) + "; unrelated=" + F(unrelatedRotation));

                targetRoot.RequireUnchanged(target.transform, TargetName + " root");
                leftControl.RequireUnchanged(geometry.Left, LeftGripName);
                rightControl.RequireUnchanged(geometry.Right, RightGripName);
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                var report = new StringBuilder()
                    .AppendLine("Turret_Enter sampled animation inspection")
                    .AppendLine("verificationTargetManipulated=False")
                    .AppendLine("durationSeconds=" + F(clip.length))
                    .AppendLine("transitionSeconds=0.7")
                    .AppendLine("holdSeconds=0.5")
                    .AppendLine("loopTime=True")
                    .AppendLine("instantReturnAfterHold=True")
                    .AppendLine("startPoseVsPlayerIdleRotationErrorDegrees=" + F(startRotationError))
                    .AppendLine("startPoseVsPlayerIdlePositionErrorMeters=" + F(startPositionError))
                    .AppendLine("startPoseVsPlayerIdleScaleError=" + F(startScaleError))
                    .AppendLine("gripHoldRotationErrorDegrees=" + F(holdRotationError))
                    .AppendLine("unrelatedBoneMaximumRotationErrorDegrees=" + F(unrelatedRotation))
                    .AppendLine("controlTransformsChanged=False")
                    .AppendLine("targetRootTransformChanged=False")
                    .AppendLine(metrics.Describe())
                    .AppendLine("directVisualReviewPending=True");
                Write(TempFolder + "/Verification.txt", report.ToString());
                Debug.Log("[TurretEnter] Sampled inspection passed.\n" + report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(idleProbe);
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        [MenuItem("Bellerophon/Player/Capture Turret Enter Final")]
        internal static void CaptureFinal()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName).gameObject;
            AnimationClip clip = DefaultClip(RequireAnimator(target), TargetName);
            if (AssetDatabase.GetAssetPath(clip) != ClipPath)
                throw new InvalidOperationException("Turret_Enter clip differs before final capture.");
            Geometry geometry = ReadGeometry(scene);
            Metrics metrics = Measure(target, clip, geometry);
            RequireMetrics(metrics);
            string imagePath = Absolute(TempFolder + "/Final.png");
            string reportPath = Absolute(TempFolder + "/Final.txt");
            if (File.Exists(imagePath) || File.Exists(reportPath))
                throw new InvalidOperationException("Turret_Enter final capture already exists.");

            RootState targetRoot = new RootState(target.transform);
            RootState leftControl = new RootState(geometry.Left);
            RootState rightControl = new RootState(geometry.Right);
            GameObject actor = Clone(target, "TurretEnter_FinalActor", true);
            actor.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            Layer(actor, PreviewLayer);
            clip.SampleAnimation(actor, 0f);
            GameObject control = CreateControlPreview(scene, actor.transform, geometry);
            try
            {
                Texture2D[] panels =
                {
                    Render(actor, control, clip, 0f, false, false),
                    Render(actor, control, clip, Transition * 0.5f, false, false),
                    Render(actor, control, clip, Transition, true, false),
                    Render(actor, control, clip, Duration, true, true)
                };
                Texture2D sheet = Combine(panels);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                    File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
                Write(TempFolder + "/Final.txt",
                    "Turret_Enter final direct contact sheet\n" +
                    "verificationTargetManipulated=False\n" +
                    "actualCharacterClone=True\n" +
                    "actualControlMeshesClonedForReadOnlyPreview=True\n" +
                    "panels=0.0s-threeQuarter,0.35s-threeQuarter,0.7s-frontClose,1.2s-sideClose\n" +
                    "leftHandTargetsLeftVerticalRod=True\n" +
                    "rightHandTargetsRightVerticalRod=True\n" +
                    metrics.Describe() + "\n" +
                    "directVisualReviewPrimary=True\n" +
                    "numericMetricsSecondary=True\n");
                targetRoot.RequireUnchanged(target.transform, TargetName + " root");
                leftControl.RequireUnchanged(geometry.Left, LeftGripName);
                rightControl.RequireUnchanged(geometry.Right, RightGripName);
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                Debug.Log("[TurretEnter] Final direct contact sheet captured once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(control);
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        [MenuItem("Bellerophon/Player/Capture Turret Enter Wrist Correction Final")]
        internal static void CaptureWristCorrectionFinal()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName).gameObject;
            AnimationClip clip = DefaultClip(RequireAnimator(target), TargetName);
            if (AssetDatabase.GetAssetPath(clip) != ClipPath)
                throw new InvalidOperationException(
                    "Turret_Enter clip differs before wrist-correction capture.");
            Geometry geometry = ReadGeometry(scene);
            Metrics metrics = Measure(target, clip, geometry);
            RequireMetrics(metrics);
            string imagePath = Absolute(TempFolder + "/WristCorrectionFinal.png");
            string reportPath = Absolute(TempFolder + "/WristCorrectionFinal.txt");
            if (File.Exists(imagePath) || File.Exists(reportPath))
                throw new InvalidOperationException(
                    "Turret_Enter wrist-correction final capture already exists.");

            RootState targetRoot = new RootState(target.transform);
            RootState leftControl = new RootState(geometry.Left);
            RootState rightControl = new RootState(geometry.Right);
            GameObject actor = Clone(target, "TurretEnter_WristCorrectionActor", true);
            actor.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            Layer(actor, PreviewLayer);
            clip.SampleAnimation(actor, Transition);
            GameObject control = CreateControlPreview(scene, actor.transform, geometry);
            try
            {
                Texture2D[] panels =
                {
                    Render(actor, control, clip, Transition, false, false),
                    Render(actor, control, clip, Transition, true, false),
                    Render(actor, control, clip, Transition, true, true, -1f),
                    Render(actor, control, clip, Duration, true, true, 1f)
                };
                Texture2D sheet = Combine(panels);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                    File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
                Write(TempFolder + "/WristCorrectionFinal.txt",
                    "Turret_Enter wrist-correction final direct contact sheet\n" +
                    "verificationTargetManipulated=False\n" +
                    "actualCharacterClone=True\n" +
                    "actualControlMeshesClonedForReadOnlyPreview=True\n" +
                    "panels=0.7s-threeQuarter,0.7s-frontClose,0.7s-leftClose,1.2s-rightClose\n" +
                    "leftHandDorsalFacesTransporterLeft=True\n" +
                    "rightHandDorsalFacesTransporterRight=True\n" +
                    "wristsContinueForearmWithoutDownwardFlexion=True\n" +
                    metrics.Describe() + "\n" +
                    "directVisualReviewPrimary=True\n" +
                    "numericMetricsSecondary=True\n");
                targetRoot.RequireUnchanged(target.transform, TargetName + " root");
                leftControl.RequireUnchanged(geometry.Left, LeftGripName);
                rightControl.RequireUnchanged(geometry.Right, RightGripName);
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                Debug.Log("[TurretEnter] Wrist-correction direct contact sheet captured once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(control);
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        [MenuItem("Bellerophon/Player/Capture Turret Enter Finger Grip Correction Final")]
        internal static void CaptureFingerGripCorrectionFinal()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName).gameObject;
            AnimationClip clip = DefaultClip(RequireAnimator(target), TargetName);
            if (AssetDatabase.GetAssetPath(clip) != ClipPath)
                throw new InvalidOperationException(
                    "Turret_Enter clip differs before finger-grip correction capture.");
            Geometry geometry = ReadGeometry(scene);
            Metrics metrics = Measure(target, clip, geometry);
            RequireMetrics(metrics);
            string imagePath = Absolute(TempFolder + "/FingerGripCorrectionFinal.png");
            string reportPath = Absolute(TempFolder + "/FingerGripCorrectionFinal.txt");
            if (File.Exists(imagePath) || File.Exists(reportPath))
                throw new InvalidOperationException(
                    "Turret_Enter finger-grip correction final capture already exists.");

            RootState targetRoot = new RootState(target.transform);
            RootState leftControl = new RootState(geometry.Left);
            RootState rightControl = new RootState(geometry.Right);
            GameObject actor = Clone(target, "TurretEnter_FingerGripCorrectionActor", true);
            actor.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            Layer(actor, PreviewLayer);
            clip.SampleAnimation(actor, Transition);
            GameObject control = CreateControlPreview(scene, actor.transform, geometry);
            try
            {
                Texture2D[] panels =
                {
                    Render(actor, control, clip, Transition, false, false),
                    Render(actor, control, clip, Transition, true, true, 1f),
                    RenderFingerGripClose(actor, control, clip, Transition, LeftHand, -1f),
                    RenderFingerGripClose(actor, control, clip, Duration, RightHand, 1f)
                };
                Texture2D sheet = Combine(panels);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                    File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
                Write(TempFolder + "/FingerGripCorrectionFinal.txt",
                    "Turret_Enter finger-grip correction final direct contact sheet\n" +
                    "verificationTargetManipulated=False\n" +
                    "actualCharacterClone=True\n" +
                    "actualControlMeshesClonedForReadOnlyPreview=True\n" +
                    "panels=0.7s-threeQuarter,0.7s-rightProfile,0.7s-leftHandProfileClose,1.2s-rightHandProfileClose\n" +
                    "fingersWrapVerticalRodsFromTransporterFront=True\n" +
                    "fingerTipsReturnBehindRods=True\n" +
                    "downwardFingerHookRemoved=True\n" +
                    metrics.Describe() + "\n" +
                    "directVisualReviewPrimary=True\n" +
                    "numericMetricsSecondary=True\n");
                targetRoot.RequireUnchanged(target.transform, TargetName + " root");
                leftControl.RequireUnchanged(geometry.Left, LeftGripName);
                rightControl.RequireUnchanged(geometry.Right, RightGripName);
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                Debug.Log("[TurretEnter] Finger-grip correction direct contact sheet captured once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(control);
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        private static void BuildPoses(GameObject target, AnimationClip idle,
            Geometry geometry, out Pose start, out Pose grip, out Metrics metrics)
        {
            GameObject clone = Clone(target, "TurretEnter_PoseSolver", false);
            try
            {
                idle.SampleAnimation(clone, 0f);
                start = CapturePose(clone);
                SolveGripPose(clone, geometry);
                grip = CapturePose(clone);
                metrics = MeasureSolved(clone, start, geometry);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static void SolveGripPose(GameObject actor, Geometry geometry)
        {
            Transform lu = FindPath(actor.transform, LeftUpper);
            Transform lf = FindPath(actor.transform, LeftFore);
            Transform lh = FindPath(actor.transform, LeftHand);
            Transform ru = FindPath(actor.transform, RightUpper);
            Transform rf = FindPath(actor.transform, RightFore);
            Transform rh = FindPath(actor.transform, RightHand);
            Vector3 up = actor.transform.up.normalized;
            Vector3 forward = actor.transform.forward.normalized;
            Vector3 right = actor.transform.right.normalized;
            Vector3 center = (lu.position + ru.position) * 0.5f +
                forward * 0.34f - up * 0.13f;
            float half = geometry.Spacing * 0.5f;
            Vector3 leftRod = center - right * half;
            Vector3 rightRod = center + right * half;
            float wristOffset = geometry.Radius + 0.025f;
            Vector3 leftWrist = leftRod - right * wristOffset + up * 0.075f - forward * 0.015f;
            Vector3 rightWrist = rightRod + right * wristOffset + up * 0.075f - forward * 0.015f;

            SolveArm(lu, lf, lh, leftWrist,
                -right * 0.84f - up * 0.38f - forward * 0.18f);
            OrientHand(lf, lh,
                NeutralFingerDirection(lf, lh, -right, forward), -right);
            CurlAroundRod(lh, true, leftRod, up, forward);
            SolveArm(ru, rf, rh, rightWrist,
                right * 0.84f - up * 0.38f - forward * 0.18f);
            OrientHand(rf, rh,
                NeutralFingerDirection(rf, rh, right, forward), right);
            CurlAroundRod(rh, false, rightRod, up, forward);
        }

        private static AnimationClip CreateClip(Pose start, Pose grip)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            clip.name = "Turret_Enter_TwoHandGrip";
            clip.frameRate = FrameRate;
            clip.wrapMode = WrapMode.Loop;
            int frames = Mathf.RoundToInt(Duration * FrameRate);
            foreach (string path in start.Rotation.Keys.OrderBy(value => value, StringComparer.Ordinal))
            {
                Quaternion from = Normalize(start.Rotation[path]);
                Quaternion to = Normalize(grip.Rotation[path]);
                if (Quaternion.Dot(from, to) < 0f)
                    to = Negate(to);
                var x = new AnimationCurve();
                var y = new AnimationCurve();
                var z = new AnimationCurve();
                var w = new AnimationCurve();
                Quaternion previous = from;
                for (int frame = 0; frame <= frames; frame++)
                {
                    float time = frame / (float)FrameRate;
                    float progress = time >= Transition ? 1f : Ease(time / Transition);
                    Quaternion value = Normalize(Quaternion.SlerpUnclamped(from, to, progress));
                    if (Quaternion.Dot(previous, value) < 0f)
                        value = Negate(value);
                    previous = value;
                    x.AddKey(time, value.x);
                    y.AddKey(time, value.y);
                    z.AddKey(time, value.z);
                    w.AddKey(time, value.w);
                }
                Linear(x); Linear(y); Linear(z); Linear(w);
                SetCurve(clip, path, "m_LocalRotation.x", x);
                SetCurve(clip, path, "m_LocalRotation.y", y);
                SetCurve(clip, path, "m_LocalRotation.z", z);
                SetCurve(clip, path, "m_LocalRotation.w", w);
            }
            foreach (string path in start.Position.Keys)
            {
                ConstantVector(clip, path, "m_LocalPosition", start.Position[path]);
                ConstantVector(clip, path, "m_LocalScale", start.Scale[path]);
            }
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = Duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState("TurretEnterTwoHandGrip");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip CreateExitClip(AnimationClip source)
        {
            if (AnimationUtility.GetObjectReferenceCurveBindings(source).Length != 0)
                throw new InvalidOperationException(
                    "Turret_Enter contains object-reference curves that cannot be inferred.");
            if (AnimationUtility.GetAnimationEvents(source).Length != 0)
                throw new InvalidOperationException(
                    "Turret_Enter contains events that cannot be inferred.");
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ExitClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ExitClipPath);
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            clip.name = "Turret_Exit_Reverse";
            clip.frameRate = source.frameRate;
            clip.wrapMode = WrapMode.Loop;
            int frames = Mathf.RoundToInt(Duration * FrameRate);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                    throw new InvalidOperationException(
                        "Turret_Enter source curve is missing: " + binding.path + " " +
                        binding.propertyName);
                var reversed = new AnimationCurve();
                for (int frame = 0; frame <= frames; frame++)
                {
                    float exitTime = frame / (float)FrameRate;
                    float sourceTime = exitTime <= Transition
                        ? Transition - exitTime
                        : 0f;
                    reversed.AddKey(exitTime, sourceCurve.Evaluate(sourceTime));
                }
                Linear(reversed);
                AnimationUtility.SetEditorCurve(clip, binding, reversed);
            }
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = Duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController CreateExitController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ExitControllerPath) != null)
                AssetDatabase.DeleteAsset(ExitControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ExitControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState(
                "TurretExitExactReverse");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Metrics Measure(GameObject target, AnimationClip clip, Geometry geometry)
        {
            GameObject probe = Clone(target, "TurretEnter_Metrics", false);
            try
            {
                clip.SampleAnimation(probe, 0f);
                Pose start = CapturePose(probe);
                clip.SampleAnimation(probe, Transition);
                return MeasureSolved(probe, start, geometry);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        private static Metrics MeasureSolved(GameObject actor, Pose start, Geometry geometry)
        {
            Transform lu = FindPath(actor.transform, LeftUpper);
            Transform lf = FindPath(actor.transform, LeftFore);
            Transform lh = FindPath(actor.transform, LeftHand);
            Transform ru = FindPath(actor.transform, RightUpper);
            Transform rf = FindPath(actor.transform, RightFore);
            Transform rh = FindPath(actor.transform, RightHand);
            Vector3 up = actor.transform.up.normalized;
            Vector3 forward = actor.transform.forward.normalized;
            Vector3 right = actor.transform.right.normalized;
            Vector3 center = (lu.position + ru.position) * 0.5f +
                forward * 0.34f - up * 0.13f;
            float half = geometry.Spacing * 0.5f;
            Vector3 leftRod = center - right * half;
            Vector3 rightRod = center + right * half;
            float wristOffset = geometry.Radius + 0.025f;
            Vector3 leftWrist = leftRod - right * wristOffset + up * 0.075f - forward * 0.015f;
            Vector3 rightWrist = rightRod + right * wristOffset + up * 0.075f - forward * 0.015f;
            HandFrame leftFrame = MeasureHandFrame(lh, true);
            HandFrame rightFrame = MeasureHandFrame(rh, false);
            Vector3 leftForeAxis = (lh.position - lf.position).normalized;
            Vector3 rightForeAxis = (rh.position - rf.position).normalized;
            Vector3 leftNeutral = NeutralFingerDirection(
                lf, lh, -right, forward);
            Vector3 rightNeutral = NeutralFingerDirection(
                rf, rh, right, forward);
            Vector3 shoulderCenter = (lu.position + ru.position) * 0.5f;
            return new Metrics(
                Vector3.Distance(lh.position, leftWrist),
                Vector3.Distance(rh.position, rightWrist),
                ElbowAngle(lu, lf, lh),
                ElbowAngle(ru, rf, rh),
                Vector3.Angle(leftFrame.Finger, leftNeutral),
                Vector3.Angle(rightFrame.Finger, rightNeutral),
                Vector3.Angle(leftFrame.Finger, leftForeAxis),
                Vector3.Angle(rightFrame.Finger, rightForeAxis),
                Vector3.Angle(leftFrame.Dorsal, -right),
                Vector3.Angle(rightFrame.Dorsal, right),
                MinimumFingerCurl(lh, true),
                MinimumFingerCurl(rh, false),
                MaximumFingerVerticalDeflection(lh, true, up),
                MaximumFingerVerticalDeflection(rh, false, up),
                MinimumFingerFrontWrap(lh, true, up),
                MinimumFingerFrontWrap(rh, false, up),
                -Vector3.Dot(lf.position - shoulderCenter, right),
                Vector3.Dot(rf.position - shoulderCenter, right),
                MaxUnrelatedRotation(start, CapturePose(actor)));
        }

        private static void RequireMetrics(Metrics value)
        {
            if (value.LeftWristError > 0.012f || value.RightWristError > 0.012f ||
                value.LeftElbow < 15f || value.LeftElbow > 145f ||
                value.RightElbow < 15f || value.RightElbow > 145f ||
                value.LeftNeutralDirectionError > 1f ||
                value.RightNeutralDirectionError > 1f ||
                value.LeftWristBend > 35f || value.RightWristBend > 35f ||
                value.LeftDorsalOutward > 1f || value.RightDorsalOutward > 1f ||
                value.LeftCurl < 18f || value.RightCurl < 18f ||
                value.LeftFingerVerticalDeflection > 5f ||
                value.RightFingerVerticalDeflection > 5f ||
                value.LeftFingerFrontWrap < 55f ||
                value.RightFingerFrontWrap < 55f ||
                value.LeftForearmLateralClearance < 0.12f ||
                value.RightForearmLateralClearance < 0.12f ||
                value.UnrelatedRotation > 0.02f)
                throw new InvalidOperationException(
                    "Turret_Enter two-hand grip metrics failed.\n" + value.Describe());
        }

        private static Vector3 NeutralFingerDirection(
            Transform fore, Transform hand, Vector3 outward, Vector3 fallbackForward)
        {
            Vector3 foreAxis = (hand.position - fore.position).normalized;
            Vector3 neutral = Vector3.ProjectOnPlane(foreAxis, outward);
            if (neutral.sqrMagnitude < 0.000001f)
                neutral = Vector3.ProjectOnPlane(fallbackForward, outward);
            if (neutral.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    "Turret_Enter neutral wrist direction cannot be resolved.");
            return neutral.normalized;
        }

        private static void SolveArm(Transform upper, Transform fore, Transform hand,
            Vector3 requestedGoal, Vector3 requestedPole)
        {
            Vector3 root = upper.position;
            Vector3 joint = fore.position;
            Vector3 end = hand.position;
            float upperLength = Vector3.Distance(root, joint);
            float foreLength = Vector3.Distance(joint, end);
            Vector3 request = requestedGoal - root;
            float distance = Mathf.Clamp(request.magnitude,
                Mathf.Abs(upperLength - foreLength) + 0.0005f,
                upperLength + foreLength - 0.012f);
            Vector3 direction = request.normalized;
            Vector3 goal = root + direction * distance;
            Vector3 pole = Vector3.ProjectOnPlane(requestedPole, direction);
            if (pole.sqrMagnitude < 0.000001f)
                pole = Vector3.ProjectOnPlane(joint - root, direction);
            pole.Normalize();
            float along = (upperLength * upperLength + distance * distance -
                foreLength * foreLength) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f,
                upperLength * upperLength - along * along));
            Vector3 desiredJoint = root + direction * along + pole * height;
            upper.rotation = Quaternion.FromToRotation(
                joint - root, desiredJoint - root) * upper.rotation;
            joint = fore.position;
            end = hand.position;
            fore.rotation = Quaternion.FromToRotation(
                end - joint, goal - joint) * fore.rotation;
        }

        private static void OrientHand(Transform fore, Transform hand,
            Vector3 desiredFinger, Vector3 desiredDorsal)
        {
            desiredFinger.Normalize();
            desiredDorsal = Vector3.ProjectOnPlane(desiredDorsal, desiredFinger).normalized;
            bool left = hand.name.StartsWith("Left", StringComparison.Ordinal);
            HandFrame frame = MeasureHandFrame(hand, left);
            Vector3 foreAxis = (hand.position - fore.position).normalized;
            Vector3 currentForeDorsal = Vector3.ProjectOnPlane(frame.Dorsal, foreAxis).normalized;
            Vector3 wantedForeDorsal = Vector3.ProjectOnPlane(desiredDorsal, foreAxis).normalized;
            if (currentForeDorsal.sqrMagnitude > 0.000001f &&
                wantedForeDorsal.sqrMagnitude > 0.000001f)
            {
                float twist = Mathf.Clamp(Vector3.SignedAngle(
                    currentForeDorsal, wantedForeDorsal, foreAxis) * 0.62f, -76f, 76f);
                fore.rotation = Quaternion.AngleAxis(twist, foreAxis) * fore.rotation;
            }
            frame = MeasureHandFrame(hand, left);
            hand.rotation = Quaternion.FromToRotation(frame.Finger, desiredFinger) * hand.rotation;
            frame = MeasureHandFrame(hand, left);
            Vector3 currentDorsal = Vector3.ProjectOnPlane(frame.Dorsal, desiredFinger).normalized;
            float residual = Vector3.SignedAngle(currentDorsal, desiredDorsal, desiredFinger);
            hand.rotation = Quaternion.AngleAxis(residual, desiredFinger) * hand.rotation;
        }

        private static HandFrame MeasureHandFrame(Transform hand, bool left)
        {
            string prefix = left ? "Left" : "Right";
            Transform middle = Child(hand, prefix + "MiddleProximal");
            Transform index = Child(hand, prefix + "IndexProximal");
            Transform little = Child(hand, prefix + "LittleProximal");
            Vector3 finger = (middle.position - hand.position).normalized;
            Vector3 across = Vector3.ProjectOnPlane(
                little.position - index.position, finger).normalized;
            Vector3 dorsal = Vector3.Cross(finger, across).normalized;
            if (left)
                dorsal = -dorsal;
            return new HandFrame(finger, dorsal);
        }

        private static void CurlAroundRod(Transform hand, bool left,
            Vector3 rodCenter, Vector3 rodAxis, Vector3 actorForward)
        {
            string prefix = left ? "Left" : "Right";
            Vector3 front = Vector3.ProjectOnPlane(actorForward, rodAxis).normalized;
            if (front.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    "Turret_Enter front-facing finger wrap cannot be resolved.");
            foreach (string digit in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                Transform proximal = Child(hand, prefix + digit + "Proximal");
                Transform intermediate = Child(proximal, prefix + digit + "Intermediate");
                Transform distal = Child(intermediate, prefix + digit + "Distal");
                Vector3 inward = Vector3.ProjectOnPlane(
                    rodCenter - proximal.position, rodAxis).normalized;
                if (inward.sqrMagnitude < 0.000001f)
                    throw new InvalidOperationException(
                        digit + " finger has no radial direction to its control rod.");
                Vector3 first;
                Vector3 second = inward;
                Vector3 third;
                if (digit == "Thumb")
                {
                    first = (-front * 0.65f + inward * 0.76f).normalized;
                    third = (front * 0.58f + inward * 0.81f).normalized;
                }
                else
                {
                    first = (front * 0.78f + inward * 0.62f).normalized;
                    third = (-front * 0.60f + inward * 0.80f).normalized;
                }
                AimSegment(proximal, intermediate, first);
                AimSegment(intermediate, distal, second);
                distal.rotation = Quaternion.FromToRotation(
                    distal.up.normalized, third) * distal.rotation;
            }
        }

        private static void AimSegment(
            Transform joint, Transform child, Vector3 desiredDirection)
        {
            joint.rotation = Quaternion.FromToRotation(
                (child.position - joint.position).normalized,
                desiredDirection.normalized) * joint.rotation;
        }

        private static void OrientCurledFinger(
            Transform proximal, Transform intermediate, Transform distal,
            Vector3 desiredDirection, Vector3 desiredOutward)
        {
            desiredDirection.Normalize();
            desiredOutward = Vector3.ProjectOnPlane(
                desiredOutward, desiredDirection).normalized;
            Vector3 tip = FingerTip(intermediate, distal);
            Vector3 chord = (tip - proximal.position).normalized;
            proximal.rotation = Quaternion.FromToRotation(
                chord, desiredDirection) * proximal.rotation;

            tip = FingerTip(intermediate, distal);
            Vector3 bulge = FingerBulge(
                proximal, intermediate, distal, desiredDirection, tip);
            if (bulge.sqrMagnitude > 0.000001f)
            {
                float twist = Vector3.SignedAngle(
                    bulge.normalized, -desiredOutward, desiredDirection);
                proximal.rotation = Quaternion.AngleAxis(
                    twist, desiredDirection) * proximal.rotation;
            }
        }

        private static Vector3 FingerTip(Transform intermediate, Transform distal)
        {
            float length = Mathf.Max(
                0.015f, Vector3.Distance(intermediate.position, distal.position));
            return distal.position + distal.up.normalized * length * 0.82f;
        }

        private static Vector3 FingerBulge(
            Transform proximal, Transform intermediate, Transform distal,
            Vector3 direction, Vector3 tip)
        {
            Vector3 middle = (intermediate.position + distal.position) * 0.5f;
            float along = Vector3.Dot(middle - proximal.position, direction);
            Vector3 linePoint = proximal.position + direction * along;
            return Vector3.ProjectOnPlane(middle - linePoint, direction);
        }

        private static void Bend(Transform joint, Transform child,
            Vector3 target, float maximum)
        {
            Quaternion wanted = Quaternion.FromToRotation(
                (child.position - joint.position).normalized,
                (target - joint.position).normalized) * joint.rotation;
            joint.rotation = Quaternion.RotateTowards(joint.rotation, wanted, maximum);
        }

        private static float MinimumFingerCurl(Transform hand, bool left)
        {
            string prefix = left ? "Left" : "Right";
            float minimum = float.MaxValue;
            foreach (string digit in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                Transform p = Child(hand, prefix + digit + "Proximal");
                Transform m = Child(p, prefix + digit + "Intermediate");
                Transform d = Child(m, prefix + digit + "Distal");
                Vector3 first = (m.position - p.position).normalized;
                Vector3 second = (d.position - m.position).normalized;
                Vector3 third = d.up.normalized;
                minimum = Mathf.Min(minimum,
                    Vector3.Angle(first, second) + Vector3.Angle(second, third));
            }
            return minimum;
        }

        private static float MaximumFingerVerticalDeflection(
            Transform hand, bool left, Vector3 up)
        {
            string prefix = left ? "Left" : "Right";
            float maximum = 0f;
            foreach (string digit in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                Transform proximal = Child(hand, prefix + digit + "Proximal");
                Transform intermediate = Child(proximal, prefix + digit + "Intermediate");
                Transform distal = Child(intermediate, prefix + digit + "Distal");
                foreach (Vector3 direction in new[]
                {
                    (intermediate.position - proximal.position).normalized,
                    (distal.position - intermediate.position).normalized,
                    distal.up.normalized
                })
                {
                    float deflection = Mathf.Abs(90f - Vector3.Angle(direction, up));
                    maximum = Mathf.Max(maximum, deflection);
                }
            }
            return maximum;
        }

        private static float MinimumFingerFrontWrap(
            Transform hand, bool left, Vector3 up)
        {
            string prefix = left ? "Left" : "Right";
            float minimum = float.MaxValue;
            foreach (string digit in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                Transform proximal = Child(hand, prefix + digit + "Proximal");
                Transform intermediate = Child(proximal, prefix + digit + "Intermediate");
                Transform distal = Child(intermediate, prefix + digit + "Distal");
                Vector3 first = Vector3.ProjectOnPlane(
                    intermediate.position - proximal.position, up).normalized;
                Vector3 third = Vector3.ProjectOnPlane(distal.up, up).normalized;
                if (first.sqrMagnitude < 0.000001f || third.sqrMagnitude < 0.000001f)
                    return 0f;
                minimum = Mathf.Min(minimum, Vector3.Angle(first, third));
            }
            return minimum;
        }

        private static GameObject CreateControlPreview(
            Scene scene, Transform actor, Geometry geometry)
        {
            Transform source = Find(scene, ControlAssemblyName);
            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = "TurretEnter_ReadOnlyControlPreview";
            Hide(clone);
            clone.SetActive(true);
            clone.transform.rotation = actor.rotation;
            Transform left = clone.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == LeftGripName);
            Transform right = clone.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == RightGripName);
            Transform lu = FindPath(actor, LeftUpper);
            Transform ru = FindPath(actor, RightUpper);
            Vector3 desiredCenter = (lu.position + ru.position) * 0.5f +
                actor.forward * 0.34f - actor.up * 0.13f;
            clone.transform.position += desiredCenter - (left.position + right.position) * 0.5f;
            Layer(clone, PreviewLayer);
            return clone;
        }

        private static Texture2D Render(GameObject actor, GameObject control,
            AnimationClip clip, float time, bool close, bool side)
        {
            return Render(actor, control, clip, time, close, side, 1f);
        }

        private static Texture2D Render(GameObject actor, GameObject control,
            AnimationClip clip, float time, bool close, bool side, float sideSign)
        {
            return Render(actor, control, clip, time, close, side, sideSign, null, false);
        }

        private static Texture2D RenderFingerGripClose(
            GameObject actor, GameObject control, AnimationClip clip,
            float time, string handPath, float sideSign)
        {
            return Render(actor, control, clip, time, true, true,
                sideSign, handPath, true);
        }

        private static Texture2D Render(GameObject actor, GameObject control,
            AnimationClip clip, float time, bool close, bool side,
            float sideSign, string handFocusPath, bool profile)
        {
            clip.SampleAnimation(actor, Mathf.Clamp(time, 0f, clip.length));
            SkinnedMeshRenderer[] skins =
                actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer skin in skins)
                skin.updateWhenOffscreen = true;
            Bounds bounds = BoundsOf(actor, control);
            var bakedObjects = new List<GameObject>();
            var bakedMeshes = new List<Mesh>();
            var enabledStates = new Dictionary<SkinnedMeshRenderer, bool>();
            foreach (SkinnedMeshRenderer skin in skins)
            {
                enabledStates.Add(skin, skin.enabled);
                if (!skin.enabled || skin.sharedMesh == null)
                    continue;
                var mesh = new Mesh { name = skin.sharedMesh.name + "_TurretEnterPreviewBake" };
                skin.BakeMesh(mesh, false);
                var baked = new GameObject(skin.name + "_TurretEnterPreviewBake");
                baked.hideFlags = HideFlags.HideAndDontSave;
                baked.layer = PreviewLayer;
                baked.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                baked.transform.localScale = skin.transform.lossyScale;
                baked.AddComponent<MeshFilter>().sharedMesh = mesh;
                baked.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                bakedMeshes.Add(mesh);
                bakedObjects.Add(baked);
                skin.enabled = false;
            }

            Vector3 forward = actor.transform.forward.normalized;
            Vector3 right = actor.transform.right.normalized;
            Vector3 up = actor.transform.up.normalized;
            Vector3 handsCenter = (FindPath(actor.transform, LeftHand).position +
                FindPath(actor.transform, RightHand).position) * 0.5f;
            Vector3 focus = !string.IsNullOrEmpty(handFocusPath)
                ? FindPath(actor.transform, handFocusPath).position
                : close ? handsCenter : bounds.center + up * 0.04f;
            Vector3 position = profile
                ? focus + forward * 0.28f + right * 1.35f * sideSign + up * 0.02f
                : side
                ? focus + forward * 1.10f + right * 1.35f * sideSign + up * 0.12f
                : close
                    ? focus + forward * 1.55f + up * 0.08f
                    : focus + forward * 2.75f + right * 1.45f + up * 0.30f;
            var cameraObject = new GameObject("TurretEnter_FinalCamera");
            var lightObject = new GameObject("TurretEnter_FinalLight");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            RenderTexture texture = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << PreviewLayer;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 12f;
                camera.fieldOfView = profile ? 23f : close ? 30f : 38f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
                camera.transform.SetPositionAndRotation(position,
                    Quaternion.LookRotation((focus - position).normalized, up));
                camera.targetTexture = texture;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.4f;
                light.transform.rotation = Quaternion.LookRotation(
                    -forward - right * 0.3f - up * 0.55f, up);
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(texture);
                foreach (KeyValuePair<SkinnedMeshRenderer, bool> item in enabledStates)
                    if (item.Key != null)
                        item.Key.enabled = item.Value;
                foreach (GameObject baked in bakedObjects)
                    UnityEngine.Object.DestroyImmediate(baked);
                foreach (Mesh mesh in bakedMeshes)
                    UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D Combine(IReadOnlyList<Texture2D> panels)
        {
            int width = panels.Count * PanelSize + (panels.Count - 1) * PanelGap;
            var result = new Texture2D(width, PanelSize, TextureFormat.RGB24, false);
            result.SetPixels32(Enumerable.Repeat(new Color32(15, 18, 26, 255),
                width * PanelSize).ToArray());
            for (int i = 0; i < panels.Count; i++)
                result.SetPixels(i * (PanelSize + PanelGap), 0,
                    PanelSize, PanelSize, panels[i].GetPixels());
            result.Apply(false, false);
            return result;
        }

        private static Bounds BoundsOf(params GameObject[] roots)
        {
            Renderer[] renderers = roots.SelectMany(root =>
                root.GetComponentsInChildren<Renderer>(true)).Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Turret_Enter preview has no renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Pose SamplePose(GameObject root, AnimationClip clip, float time)
        {
            clip.SampleAnimation(root, Mathf.Clamp(time, 0f, clip.length));
            return CapturePose(root);
        }

        private static Pose CapturePose(GameObject root)
        {
            Transform armature = FindPath(root.transform, "Armature");
            Transform[] all = armature.GetComponentsInChildren<Transform>(true);
            return new Pose(
                all.ToDictionary(item => PathOf(root.transform, item),
                    item => item.localRotation, StringComparer.Ordinal),
                all.ToDictionary(item => PathOf(root.transform, item),
                    item => item.localPosition, StringComparer.Ordinal),
                all.ToDictionary(item => PathOf(root.transform, item),
                    item => item.localScale, StringComparer.Ordinal));
        }

        private static float MaxRotation(Pose left, Pose right) =>
            left.Rotation.Keys.Intersect(right.Rotation.Keys)
                .Max(path => Quaternion.Angle(left.Rotation[path], right.Rotation[path]));

        private static float MaxUnrelatedRotation(Pose start, Pose changed) =>
            start.Rotation.Keys.Where(path =>
                    !path.StartsWith(LeftUpper, StringComparison.Ordinal) &&
                    !path.StartsWith(RightUpper, StringComparison.Ordinal))
                .Max(path => Quaternion.Angle(start.Rotation[path], changed.Rotation[path]));

        private static float MaxVector(
            IReadOnlyDictionary<string, Vector3> left,
            IReadOnlyDictionary<string, Vector3> right) =>
            left.Keys.Intersect(right.Keys).Max(path => Vector3.Distance(left[path], right[path]));

        private static float MaxCurveDelta(
            AnimationClip clip, float firstTime, float secondTime)
        {
            float maximum = 0f;
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                    continue;
                maximum = Mathf.Max(maximum,
                    Mathf.Abs(curve.Evaluate(firstTime) - curve.Evaluate(secondTime)));
            }
            return maximum;
        }

        private static Geometry ReadGeometry(Scene scene)
        {
            Transform armory = Find(scene, ArmoryRootName);
            Transform assembly = armory.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == ControlAssemblyName);
            Transform left = assembly.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == LeftGripName);
            Transform right = assembly.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == RightGripName);
            Renderer leftRenderer = left.GetComponent<Renderer>() ??
                throw new InvalidOperationException(LeftGripName + " renderer is missing.");
            Renderer rightRenderer = right.GetComponent<Renderer>() ??
                throw new InvalidOperationException(RightGripName + " renderer is missing.");
            float spacing = Vector3.Distance(left.position, right.position);
            if (spacing < 0.5f || spacing > 1.2f)
                throw new InvalidOperationException("Turret control grip spacing is unexpected: " + F(spacing));
            float radius = (leftRenderer.bounds.size.x + leftRenderer.bounds.size.z +
                rightRenderer.bounds.size.x + rightRenderer.bounds.size.z) * 0.125f;
            return new Geometry(assembly, left, right,
                leftRenderer.bounds, rightRenderer.bounds, spacing, radius);
        }

        private static AnimationClip DefaultClip(Animator animator, string label)
        {
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(label + " controller is missing.");
            Motion motion = controller.layers[0].stateMachine.defaultState?.motion ??
                throw new InvalidOperationException(label + " default motion is missing.");
            return motion as AnimationClip ??
                throw new InvalidOperationException(label + " default motion is not a clip.");
        }

        private static GameObject Clone(GameObject source, string name, bool active)
        {
            GameObject clone = UnityEngine.Object.Instantiate(source);
            clone.name = name;
            Hide(clone);
            clone.SetActive(active);
            Animator animator = clone.GetComponent<Animator>();
            if (animator != null)
                animator.enabled = false;
            return clone;
        }

        private static void Hide(GameObject root)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        private static void Layer(GameObject root, int layer)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static float ElbowAngle(Transform upper, Transform fore, Transform hand) =>
            180f - Vector3.Angle(upper.position - fore.position, hand.position - fore.position);

        private static float Ease(float value)
        {
            value = Mathf.Clamp01(value);
            float square = value * value;
            float cube = square * value;
            return cube * (10f - 15f * value + 6f * square);
        }

        private static void ConstantVector(AnimationClip clip, string path,
            string property, Vector3 value)
        {
            SetCurve(clip, path, property + ".x",
                AnimationCurve.Linear(0f, value.x, Duration, value.x));
            SetCurve(clip, path, property + ".y",
                AnimationCurve.Linear(0f, value.y, Duration, value.y));
            SetCurve(clip, path, property + ".z",
                AnimationCurve.Linear(0f, value.z, Duration, value.z));
        }

        private static void SetCurve(AnimationClip clip, string path,
            string property, AnimationCurve curve) =>
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);

        private static void Linear(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, i, AnimationUtility.TangentMode.Linear);
            }
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active: " + scene.path);
            return scene;
        }

        private static Transform Find(Scene scene, string name)
        {
            Transform[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(name + " count=" + matches.Length);
            return matches[0];
        }

        private static Transform FindPath(Transform root, string path) =>
            root.Find(path) ?? throw new InvalidOperationException(path + " is missing.");

        private static Transform Child(Transform root, string name) =>
            root.Find(name) ?? throw new InvalidOperationException(name + " is missing.");

        private static Animator RequireAnimator(GameObject root) =>
            root.GetComponent<Animator>() ??
            throw new InvalidOperationException(root.name + " Animator is missing.");

        private static string PathOf(Transform root, Transform item) =>
            AnimationUtility.CalculateTransformPath(item, root);

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Turret_Enter tooling requires Edit Mode.");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void Write(string projectPath, string content)
        {
            string path = Absolute(projectPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static string Absolute(string projectPath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root unavailable.");
            return Path.GetFullPath(Path.Combine(root,
                projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static Quaternion Normalize(Quaternion value)
        {
            float magnitude = Mathf.Sqrt(value.x * value.x + value.y * value.y +
                value.z * value.z + value.w * value.w);
            return magnitude < 0.000001f ? Quaternion.identity :
                new Quaternion(value.x / magnitude, value.y / magnitude,
                    value.z / magnitude, value.w / magnitude);
        }

        private static Quaternion Negate(Quaternion value) =>
            new Quaternion(-value.x, -value.y, -value.z, -value.w);

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            "(" + F(value.x) + "," + F(value.y) + "," + F(value.z) + ")";
        private static string Quat(Quaternion value) =>
            "(" + F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w) + ")";

        private sealed class Pose
        {
            internal Pose(Dictionary<string, Quaternion> rotation,
                Dictionary<string, Vector3> position,
                Dictionary<string, Vector3> scale)
            {
                Rotation = rotation;
                Position = position;
                Scale = scale;
            }
            internal Dictionary<string, Quaternion> Rotation { get; }
            internal Dictionary<string, Vector3> Position { get; }
            internal Dictionary<string, Vector3> Scale { get; }
        }

        private readonly struct Geometry
        {
            internal Geometry(Transform assembly, Transform left, Transform right,
                Bounds leftBounds, Bounds rightBounds, float spacing, float radius)
            {
                Assembly = assembly;
                Left = left;
                Right = right;
                LeftBounds = leftBounds;
                RightBounds = rightBounds;
                Spacing = spacing;
                Radius = radius;
            }
            internal Transform Assembly { get; }
            internal Transform Left { get; }
            internal Transform Right { get; }
            internal Bounds LeftBounds { get; }
            internal Bounds RightBounds { get; }
            internal float Spacing { get; }
            internal float Radius { get; }
        }

        private readonly struct HandFrame
        {
            internal HandFrame(Vector3 finger, Vector3 dorsal)
            {
                Finger = finger;
                Dorsal = dorsal;
            }
            internal Vector3 Finger { get; }
            internal Vector3 Dorsal { get; }
        }

        private readonly struct RootState
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private readonly bool active;

            internal RootState(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
                active = transform.gameObject.activeSelf;
            }

            internal void RequireUnchanged(Transform transform, string label)
            {
                if (transform.localPosition != position ||
                    transform.localRotation != rotation ||
                    transform.localScale != scale ||
                    transform.gameObject.activeSelf != active)
                    throw new InvalidOperationException(label + " changed unexpectedly.");
            }
        }

        private readonly struct Metrics
        {
            internal Metrics(float leftWristError, float rightWristError,
                float leftElbow, float rightElbow,
                float leftNeutralDirectionError,
                float rightNeutralDirectionError,
                float leftWristBend, float rightWristBend,
                float leftDorsalOutward, float rightDorsalOutward,
                float leftCurl, float rightCurl,
                float leftFingerVerticalDeflection,
                float rightFingerVerticalDeflection,
                float leftFingerFrontWrap,
                float rightFingerFrontWrap,
                float leftForearmLateralClearance,
                float rightForearmLateralClearance,
                float unrelatedRotation)
            {
                LeftWristError = leftWristError;
                RightWristError = rightWristError;
                LeftElbow = leftElbow;
                RightElbow = rightElbow;
                LeftNeutralDirectionError = leftNeutralDirectionError;
                RightNeutralDirectionError = rightNeutralDirectionError;
                LeftWristBend = leftWristBend;
                RightWristBend = rightWristBend;
                LeftDorsalOutward = leftDorsalOutward;
                RightDorsalOutward = rightDorsalOutward;
                LeftCurl = leftCurl;
                RightCurl = rightCurl;
                LeftFingerVerticalDeflection = leftFingerVerticalDeflection;
                RightFingerVerticalDeflection = rightFingerVerticalDeflection;
                LeftFingerFrontWrap = leftFingerFrontWrap;
                RightFingerFrontWrap = rightFingerFrontWrap;
                LeftForearmLateralClearance = leftForearmLateralClearance;
                RightForearmLateralClearance = rightForearmLateralClearance;
                UnrelatedRotation = unrelatedRotation;
            }
            internal float LeftWristError { get; }
            internal float RightWristError { get; }
            internal float LeftElbow { get; }
            internal float RightElbow { get; }
            internal float LeftNeutralDirectionError { get; }
            internal float RightNeutralDirectionError { get; }
            internal float LeftWristBend { get; }
            internal float RightWristBend { get; }
            internal float LeftDorsalOutward { get; }
            internal float RightDorsalOutward { get; }
            internal float LeftCurl { get; }
            internal float RightCurl { get; }
            internal float LeftFingerVerticalDeflection { get; }
            internal float RightFingerVerticalDeflection { get; }
            internal float LeftFingerFrontWrap { get; }
            internal float RightFingerFrontWrap { get; }
            internal float LeftForearmLateralClearance { get; }
            internal float RightForearmLateralClearance { get; }
            internal float UnrelatedRotation { get; }
            internal string Describe() =>
                "leftWristTargetErrorMeters=" + F(LeftWristError) + "\n" +
                "rightWristTargetErrorMeters=" + F(RightWristError) + "\n" +
                "leftElbowBendDegrees=" + F(LeftElbow) + "\n" +
                "rightElbowBendDegrees=" + F(RightElbow) + "\n" +
                "leftNeutralFingerDirectionErrorDegrees=" +
                    F(LeftNeutralDirectionError) + "\n" +
                "rightNeutralFingerDirectionErrorDegrees=" +
                    F(RightNeutralDirectionError) + "\n" +
                "leftWristBendFromForearmDegrees=" + F(LeftWristBend) + "\n" +
                "rightWristBendFromForearmDegrees=" + F(RightWristBend) + "\n" +
                "leftHandDorsalOutwardAngleDegrees=" + F(LeftDorsalOutward) + "\n" +
                "rightHandDorsalOutwardAngleDegrees=" + F(RightDorsalOutward) + "\n" +
                "leftMinimumFingerCurlDegrees=" + F(LeftCurl) + "\n" +
                "rightMinimumFingerCurlDegrees=" + F(RightCurl) + "\n" +
                "leftMaximumFingerVerticalDeflectionDegrees=" +
                    F(LeftFingerVerticalDeflection) + "\n" +
                "rightMaximumFingerVerticalDeflectionDegrees=" +
                    F(RightFingerVerticalDeflection) + "\n" +
                "leftMinimumFingerFrontWrapDegrees=" + F(LeftFingerFrontWrap) + "\n" +
                "rightMinimumFingerFrontWrapDegrees=" + F(RightFingerFrontWrap) + "\n" +
                "leftForearmLateralClearanceMeters=" + F(LeftForearmLateralClearance) + "\n" +
                "rightForearmLateralClearanceMeters=" + F(RightForearmLateralClearance) + "\n" +
                "unrelatedBoneMaximumRotationErrorDegrees=" + F(UnrelatedRotation);
        }
    }
}
