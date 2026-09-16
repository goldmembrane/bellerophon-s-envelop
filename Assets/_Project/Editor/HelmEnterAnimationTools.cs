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
    internal static class HelmEnterAnimationTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Helm_Enter";
        private const string IdleName = "Player_Idle";
        private const string TempFolder = "Temp/HelmEnter";
        private const string AssetFolder = "Assets/_Project/Animations/HelmEnter";
        private const string ClipPath = AssetFolder + "/Helm_Enter_WheelGrip.anim";
        private const string ControllerPath = AssetFolder + "/Helm_Enter_WheelGrip.controller";
        private const string ExitTargetName = "Helm_Exit";
        private const string ExitTempFolder = "Temp/HelmExit";
        private const string ExitAssetFolder = "Assets/_Project/Animations/HelmExit";
        private const string ExitClipPath = ExitAssetFolder + "/Helm_Exit_WheelRelease.anim";
        private const string ExitControllerPath = ExitAssetFolder + "/Helm_Exit_WheelRelease.controller";
        private const string Spine = "Armature/Hips/Spine02/Spine01/Spine";
        private const string LU = Spine + "/LeftShoulder/LeftArm";
        private const string LF = LU + "/LeftForeArm";
        private const string LH = LF + "/LeftHand";
        private const string RU = Spine + "/RightShoulder/RightArm";
        private const string RF = RU + "/RightForeArm";
        private const string RH = RF + "/RightHand";
        private const string HelmRoot = "Approved Cockpit 02 Console";
        private const string HubName = "helm bearing housing";
        private const string LeftKnobName = "helm outer hand knob 2";
        private const string RightKnobName = "helm outer hand knob 3";
        private const float Transition = 0.7f;
        private const float Hold = 0.5f;
        private const float Duration = Transition + Hold;
        private const int Rate = 60;
        private const int PreviewLayer = 31;
        private const int Panel = 420;
        private const int Gap = 8;

        private static readonly string[] WheelParts =
        {
            "helm bearing housing", "helm worn brass hub cap",
            "large ship helm wheel ring",
            "helm outer hand knob 1", "helm outer hand knob 2",
            "helm outer hand knob 3", "helm outer hand knob 4",
            "helm outer hand knob 5", "helm outer hand knob 6",
            "helm outer hand knob 7", "helm outer hand knob 8",
            "helm radial spoke 1", "helm radial spoke 2",
            "helm radial spoke 3", "helm radial spoke 4",
            "helm radial spoke 5", "helm radial spoke 6",
            "helm radial spoke 7", "helm radial spoke 8"
        };

        [MenuItem("Bellerophon/Player/Inspect Helm Enter Sources")]
        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName);
            Animator targetAnimator = RequireAnimator(target);
            AnimationClip idle = DefaultClip(RequireAnimator(Find(scene, IdleName)), IdleName);
            Geometry geometry = ReadGeometry(scene);
            var report = new StringBuilder()
                .AppendLine("Helm_Enter source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("scene=" + scene.path)
                .AppendLine("targetPosition=" + Vec(target.transform.position))
                .AppendLine("targetRotation=" + Quat(target.transform.rotation))
                .AppendLine("targetScale=" + Vec(target.transform.localScale))
                .AppendLine("targetAnimatorIsHuman=" + targetAnimator.isHuman)
                .AppendLine("idleClip=" + AssetDatabase.GetAssetPath(idle))
                .AppendLine("idleClipLength=" + F(idle.length))
                .AppendLine("helmHub=" + Vec(geometry.Hub))
                .AppendLine("leftHandle=" + LeftKnobName)
                .AppendLine("rightHandle=" + RightKnobName)
                .AppendLine("leftHandleOffset=" + Vec(geometry.LeftOffset))
                .AppendLine("rightHandleOffset=" + Vec(geometry.RightOffset))
                .AppendLine("preferredClockPositions=10,2")
                .AppendLine("fallbackClockPositionsUsed=False");
            Write(TempFolder + "/SourceInspection.txt", report.ToString());
            Debug.Log("[HelmEnter] Sources inspected read-only.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Apply Helm Enter Wheel Grip")]
        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName);
            Animator animator = RequireAnimator(target);
            AnimationClip idle = DefaultClip(RequireAnimator(Find(scene, IdleName)), IdleName);
            Geometry geometry = ReadGeometry(scene);
            Vector3 rootPosition = target.transform.position;
            Quaternion rootRotation = target.transform.rotation;
            Vector3 rootScale = target.transform.localScale;

            Pose start;
            Pose grip;
            Metrics solved;
            BuildPoses(target, idle, geometry, out start, out grip, out solved);
            RequireMetrics(solved);
            EnsureFolder(AssetFolder);
            AnimationClip clip = CreateClip(start, grip);
            AnimatorController controller = CreateController(clip);
            Undo.RecordObject(animator, "Connect Helm_Enter wheel grip");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            if (Vector3.Distance(rootPosition, target.transform.position) > 0.00001f ||
                Quaternion.Angle(rootRotation, target.transform.rotation) > 0.01f ||
                Vector3.Distance(rootScale, target.transform.localScale) > 0.00001f)
                throw new InvalidOperationException("Helm_Enter root transform changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            var report = new StringBuilder()
                .AppendLine("Helm_Enter wheel grip application")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourcePose=Player_Idle frame 0")
                .AppendLine("sourcePoseGenerated=False")
                .AppendLine("preferredClockPositions=10,2")
                .AppendLine("fallbackClockPositionsUsed=False")
                .AppendLine("transitionSeconds=0.7")
                .AppendLine("holdSeconds=0.5")
                .AppendLine("instantReturnAfterHold=True")
                .AppendLine("lowerBodyChanged=False")
                .AppendLine("wheelModelChanged=False")
                .AppendLine("clip=" + ClipPath)
                .AppendLine("controller=" + ControllerPath)
                .AppendLine(solved.Describe());
            Write(TempFolder + "/Application.txt", report.ToString());
            Debug.Log("[HelmEnter] First implementation applied.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Helm Enter Wheel Grip")]
        internal static void InspectAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName);
            AnimationClip clip = DefaultClip(RequireAnimator(target), TargetName);
            if (AssetDatabase.GetAssetPath(clip) != ClipPath)
                throw new InvalidOperationException("Helm_Enter clip differs.");
            if (Mathf.Abs(clip.length - Duration) > 0.001f)
                throw new InvalidOperationException("Helm_Enter duration differs: " + F(clip.length));

            GameObject probe = Clone(target, "HelmEnter_InspectProbe", false);
            try
            {
                Pose start = SamplePose(probe, clip, 0f);
                Pose grip = SamplePose(probe, clip, Transition);
                Pose held = SamplePose(probe, clip, Duration);
                AnimationClip idle = DefaultClip(RequireAnimator(Find(scene, IdleName)), IdleName);
                GameObject idleProbe = Clone(target, "HelmEnter_IdleComparisonProbe", false);
                Pose idleStart;
                try
                {
                    idleStart = SamplePose(idleProbe, idle, 0f);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(idleProbe);
                }
                float idleError = MaxRotation(start, idleStart);
                float lowerError = MaxLower(start, grip);
                float holdError = MaxRotation(grip, held);
                if (idleError > 0.02f || lowerError > 0.02f || holdError > 0.02f)
                    throw new InvalidOperationException(
                        "Pose preservation failed. idle=" + F(idleError) +
                        " lower=" + F(lowerError) + " hold=" + F(holdError));
                Metrics metrics = Measure(target, clip, ReadGeometry(scene));
                RequireMetrics(metrics);
                var report = new StringBuilder()
                    .AppendLine("Helm_Enter sampled animation inspection")
                    .AppendLine("verificationTargetManipulated=False")
                    .AppendLine("durationSeconds=" + F(clip.length))
                    .AppendLine("transitionSeconds=0.7")
                    .AppendLine("holdSeconds=0.5")
                    .AppendLine("loopTime=True")
                    .AppendLine("instantReturnAfterHold=True")
                    .AppendLine("startPoseIdleErrorDegrees=" + F(idleError))
                    .AppendLine("lowerBodyErrorDegrees=" + F(lowerError))
                    .AppendLine("holdErrorDegrees=" + F(holdError))
                    .AppendLine(metrics.Describe())
                    .AppendLine("directVisualReviewPending=True");
                Write(TempFolder + "/Verification.txt", report.ToString());
                Debug.Log("[HelmEnter] Sampled inspection passed.\n" + report);
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        [MenuItem("Bellerophon/Player/Capture Helm Enter Final")]
        internal static void CaptureFinal()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, TargetName);
            AnimationClip clip = DefaultClip(RequireAnimator(target), TargetName);
            Geometry geometry = ReadGeometry(scene);
            Metrics metrics = Measure(target, clip, geometry);
            RequireMetrics(metrics);
            GameObject actor = Clone(target, "HelmEnter_FinalActor", true);
            actor.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            Layer(actor, PreviewLayer);
            clip.SampleAnimation(actor, 0f);
            GameObject wheel = CreateWheel(scene, actor.transform, geometry);
            try
            {
                Texture2D[] panels =
                {
                    Render(actor, wheel, clip, 0f, false, false),
                    Render(actor, wheel, clip, Transition * 0.5f, false, false),
                    Render(actor, wheel, clip, Transition, false, false),
                    Render(actor, wheel, clip, Duration, true, true)
                };
                Texture2D sheet = Combine(panels);
                try
                {
                    string path = Absolute(TempFolder + "/Final.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
                Write(TempFolder + "/Final.txt",
                    "Helm_Enter final direct contact sheet\n" +
                    "verificationTargetManipulated=False\n" +
                    "actualCharacterClone=True\n" +
                    "actualWheelMeshesClonedForReadOnlyPreview=True\n" +
                    "panels=0.0s-wheel,0.35s-wheel,0.7s-wheel,1.2s-front-no-wheel\n" +
                    "preferredClockPositions=10,2\n" +
                    metrics.Describe() + "\n" +
                    "directVisualReviewPrimary=True\n" +
                    "numericMetricsSecondary=True\n");
                Debug.Log("[HelmEnter] Final contact sheet captured once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wheel);
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        internal static string FinalAbsolutePath => Absolute(TempFolder + "/Final.png");

        [MenuItem("Bellerophon/Player/Apply Helm Exit Wheel Release")]
        internal static void ApplyExit()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, ExitTargetName);
            Animator animator = RequireAnimator(target);
            AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException("Helm_Enter source clip is missing: " + ClipPath);
            Hash128 sourceHashBefore = AssetDatabase.GetAssetDependencyHash(ClipPath);
            Vector3 rootPosition = target.transform.position;
            Quaternion rootRotation = target.transform.rotation;
            Vector3 rootScale = target.transform.localScale;

            EnsureFolder(ExitAssetFolder);
            AnimationClip clip = CreateExitClip(source);
            AnimatorController controller = CreateExitController(clip);
            Undo.RecordObject(animator, "Connect Helm_Exit wheel release");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            if (Vector3.Distance(rootPosition, target.transform.position) > 0.00001f ||
                Quaternion.Angle(rootRotation, target.transform.rotation) > 0.01f ||
                Vector3.Distance(rootScale, target.transform.localScale) > 0.00001f)
                throw new InvalidOperationException("Helm_Exit root transform changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Hash128 sourceHashAfter = AssetDatabase.GetAssetDependencyHash(ClipPath);
            if (sourceHashBefore != sourceHashAfter)
                throw new InvalidOperationException("Helm_Enter source clip changed during Helm_Exit application.");

            var report = new StringBuilder()
                .AppendLine("Helm_Exit exact reverse application")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourceClip=" + ClipPath)
                .AppendLine("sourceClipChanged=False")
                .AppendLine("sourceTransitionRangeSeconds=0.0..0.7")
                .AppendLine("destinationReverseRangeSeconds=0.0..0.7")
                .AppendLine("sourceTimeMapping=0.7-destinationTime")
                .AppendLine("gripHoldAtStartSeconds=0")
                .AppendLine("playerIdleHoldRangeSeconds=0.7..1.2")
                .AppendLine("playerIdleHoldSeconds=0.5")
                .AppendLine("instantLoopReturnToGrip=True")
                .AppendLine("generatedMotion=False")
                .AppendLine("clip=" + ExitClipPath)
                .AppendLine("controller=" + ExitControllerPath);
            Write(ExitTempFolder + "/Application.txt", report.ToString());
            Debug.Log("[HelmExit] Exact reverse of Helm_Enter transition applied.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Helm Exit Wheel Release")]
        internal static void InspectExitAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, ExitTargetName);
            AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException("Helm_Enter source clip is missing.");
            AnimationClip exit = DefaultClip(RequireAnimator(target), ExitTargetName);
            if (AssetDatabase.GetAssetPath(exit) != ExitClipPath)
                throw new InvalidOperationException("Helm_Exit clip differs: " + AssetDatabase.GetAssetPath(exit));
            if (Mathf.Abs(exit.length - Duration) > 0.001f)
                throw new InvalidOperationException("Helm_Exit duration differs: " + F(exit.length));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(exit);
            if (!settings.loopTime)
                throw new InvalidOperationException("Helm_Exit loopTime is disabled.");

            float maximumCurveError = MaximumExitCurveError(source, exit);
            GameObject sourceProbe = Clone(target, "HelmExit_SourceProbe", false);
            GameObject exitProbe = Clone(target, "HelmExit_ExitProbe", false);
            GameObject idleProbe = Clone(target, "HelmExit_IdleProbe", false);
            try
            {
                Pose sourceGrip = SamplePose(sourceProbe, source, Transition);
                Pose exitStart = SamplePose(exitProbe, exit, 0f);
                Pose sourceIdle = SamplePose(sourceProbe, source, 0f);
                Pose exitIdle = SamplePose(exitProbe, exit, Transition);
                Pose exitHeld = SamplePose(exitProbe, exit, Duration);
                AnimationClip idle = DefaultClip(RequireAnimator(Find(scene, IdleName)), IdleName);
                Pose playerIdle = SamplePose(idleProbe, idle, 0f);
                float startPoseError = MaxRotation(sourceGrip, exitStart);
                float idlePoseError = MaxRotation(sourceIdle, exitIdle);
                float playerIdleError = MaxRotation(playerIdle, exitIdle);
                float holdError = MaxRotation(exitIdle, exitHeld);
                const float initialMotionWindow = 0.1f;
                Pose initialMotionPose = SamplePose(exitProbe, exit, initialMotionWindow);
                float initialMotionDegrees = MaxRotation(exitStart, initialMotionPose);
                if (maximumCurveError > 0.00001f || startPoseError > 0.02f ||
                    idlePoseError > 0.02f || playerIdleError > 0.02f ||
                    holdError > 0.02f || initialMotionDegrees < 0.001f)
                    throw new InvalidOperationException(
                        "Helm_Exit exact reverse validation failed. curve=" + F(maximumCurveError) +
                        " start=" + F(startPoseError) + " sourceIdle=" + F(idlePoseError) +
                        " playerIdle=" + F(playerIdleError) + " hold=" + F(holdError) +
                        " initialMotion=" + F(initialMotionDegrees));

                var report = new StringBuilder()
                    .AppendLine("Helm_Exit sampled animation inspection")
                    .AppendLine("verificationTargetManipulated=False")
                    .AppendLine("durationSeconds=" + F(exit.length))
                    .AppendLine("reverseTransitionSeconds=0.7")
                    .AppendLine("gripHoldAtStartSeconds=0")
                    .AppendLine("playerIdleHoldSeconds=0.5")
                    .AppendLine("loopTime=True")
                    .AppendLine("instantLoopReturnToGrip=True")
                    .AppendLine("maximumReverseCurveError=" + F(maximumCurveError))
                    .AppendLine("startPoseVsHelmEnterGripErrorDegrees=" + F(startPoseError))
                    .AppendLine("idlePoseVsHelmEnterStartErrorDegrees=" + F(idlePoseError))
                    .AppendLine("idlePoseVsPlayerIdleErrorDegrees=" + F(playerIdleError))
                    .AppendLine("idleHoldErrorDegrees=" + F(holdError))
                    .AppendLine("initialMotionWindowSeconds=" + F(initialMotionWindow))
                    .AppendLine("initialMotionDegrees=" + F(initialMotionDegrees))
                    .AppendLine("directVisualReviewPending=True");
                Write(ExitTempFolder + "/Verification.txt", report.ToString());
                Debug.Log("[HelmExit] Exact reverse sampled inspection passed.\n" + report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(idleProbe);
                UnityEngine.Object.DestroyImmediate(exitProbe);
                UnityEngine.Object.DestroyImmediate(sourceProbe);
            }
        }

        [MenuItem("Bellerophon/Player/Capture Helm Exit Final")]
        internal static void CaptureExitFinal()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = Find(scene, ExitTargetName);
            AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException("Helm_Enter source clip is missing.");
            AnimationClip clip = DefaultClip(RequireAnimator(target), ExitTargetName);
            if (MaximumExitCurveError(source, clip) > 0.00001f)
                throw new InvalidOperationException("Helm_Exit curve validation must pass before capture.");
            Geometry geometry = ReadGeometry(scene);
            GameObject actor = Clone(target, "HelmExit_FinalActor", true);
            actor.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            Layer(actor, PreviewLayer);
            clip.SampleAnimation(actor, 0f);
            GameObject wheel = CreateWheel(scene, actor.transform, geometry);
            try
            {
                Texture2D[] panels =
                {
                    Render(actor, wheel, clip, 0f, false, false),
                    Render(actor, wheel, clip, Transition * 0.5f, false, false),
                    Render(actor, wheel, clip, Transition, false, false),
                    Render(actor, wheel, clip, Duration, true, true)
                };
                Texture2D sheet = Combine(panels);
                try
                {
                    string path = Absolute(ExitTempFolder + "/Final.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
                Write(ExitTempFolder + "/Final.txt",
                    "Helm_Exit final direct contact sheet\n" +
                    "verificationTargetManipulated=False\n" +
                    "actualCharacterClone=True\n" +
                    "actualWheelMeshesClonedForReadOnlyPreview=True\n" +
                    "panels=0.0s-grip-wheel,0.35s-release-wheel,0.7s-idle-wheel,1.2s-idle-front-no-wheel\n" +
                    "sourceTransitionReversedExactly=True\n" +
                    "gripHoldAtStartSeconds=0\n" +
                    "playerIdleHoldSeconds=0.5\n" +
                    "directVisualReviewPrimary=True\n" +
                    "numericMetricsSecondary=True\n");
                Debug.Log("[HelmExit] Final direct contact sheet captured once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wheel);
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        internal static string ExitFinalAbsolutePath => Absolute(ExitTempFolder + "/Final.png");

        private static AnimationClip CreateExitClip(AnimationClip source)
        {
            if (AnimationUtility.GetObjectReferenceCurveBindings(source).Length != 0)
                throw new InvalidOperationException("Helm_Enter contains unsupported object reference curves.");
            if (AnimationUtility.GetAnimationEvents(source).Length != 0)
                throw new InvalidOperationException("Helm_Enter contains unexpected animation events.");
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ExitClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ExitClipPath);
            }
            foreach (EditorCurveBinding oldBinding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, oldBinding, null);
            clip.name = "Helm_Exit_WheelRelease";
            clip.frameRate = Rate;
            clip.wrapMode = WrapMode.Loop;
            int frames = Mathf.RoundToInt(Duration * Rate);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                var destinationCurve = new AnimationCurve();
                for (int frame = 0; frame <= frames; frame++)
                {
                    float time = frame / (float)Rate;
                    float sourceTime = time <= Transition ? Transition - time : 0f;
                    destinationCurve.AddKey(time, sourceCurve.Evaluate(sourceTime));
                }
                Linear(destinationCurve);
                AnimationUtility.SetEditorCurve(clip, binding, destinationCurve);
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

        private static AnimatorController CreateExitController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ExitControllerPath) != null)
                AssetDatabase.DeleteAsset(ExitControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ExitControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState("HelmExitWheelRelease");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static float MaximumExitCurveError(AnimationClip source, AnimationClip exit)
        {
            EditorCurveBinding[] sourceBindings = AnimationUtility.GetCurveBindings(source);
            EditorCurveBinding[] exitBindings = AnimationUtility.GetCurveBindings(exit);
            if (sourceBindings.Length != exitBindings.Length)
                throw new InvalidOperationException("Helm_Exit curve binding count differs.");
            var exitMap = exitBindings.ToDictionary(BindingKey,
                binding => AnimationUtility.GetEditorCurve(exit, binding), StringComparer.Ordinal);
            float maximum = 0f;
            int frames = Mathf.RoundToInt(Duration * Rate);
            foreach (EditorCurveBinding binding in sourceBindings)
            {
                string key = BindingKey(binding);
                if (!exitMap.TryGetValue(key, out AnimationCurve destinationCurve))
                    throw new InvalidOperationException("Helm_Exit binding is missing: " + key);
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                for (int frame = 0; frame <= frames; frame++)
                {
                    float time = frame / (float)Rate;
                    float sourceTime = time <= Transition ? Transition - time : 0f;
                    maximum = Mathf.Max(maximum,
                        Mathf.Abs(destinationCurve.Evaluate(time) - sourceCurve.Evaluate(sourceTime)));
                }
            }
            return maximum;
        }

        private static string BindingKey(EditorCurveBinding binding) =>
            binding.path + "|" + binding.type.AssemblyQualifiedName + "|" + binding.propertyName;

        private static void BuildPoses(GameObject target, AnimationClip idle,
            Geometry geometry, out Pose start, out Pose grip, out Metrics metrics)
        {
            GameObject clone = Clone(target, "HelmEnter_PoseSolver", false);
            try
            {
                idle.SampleAnimation(clone, 0f);
                start = CapturePose(clone);
                Transform lu = FindPath(clone.transform, LU);
                Transform lf = FindPath(clone.transform, LF);
                Transform lh = FindPath(clone.transform, LH);
                Transform ru = FindPath(clone.transform, RU);
                Transform rf = FindPath(clone.transform, RF);
                Transform rh = FindPath(clone.transform, RH);
                Vector3 up = clone.transform.up.normalized;
                Vector3 forward = clone.transform.forward.normalized;
                Vector3 right = clone.transform.right.normalized;
                Vector3 hub = (lu.position + ru.position) * 0.5f +
                    forward * 0.44f - up * 0.30f;
                Vector3 leftKnob = hub + right * geometry.LeftOffset.x +
                    up * geometry.LeftOffset.y;
                Vector3 rightKnob = hub + right * geometry.RightOffset.x +
                    up * geometry.RightOffset.y;
                Vector3 leftRadial = (leftKnob - hub).normalized;
                Vector3 rightRadial = (rightKnob - hub).normalized;
                Vector3 leftWrist = leftKnob - leftRadial * 0.082f - forward * 0.060f;
                Vector3 rightWrist = rightKnob - rightRadial * 0.082f - forward * 0.060f;
                Solve(lu, lf, lh, leftWrist,
                    -right * 0.82f - up * 0.42f + forward * 0.16f);
                OrientHand(lf, lh, true, forward, up);
                CurlHand(lh, true, leftKnob, forward, up);
                Solve(ru, rf, rh, rightWrist,
                    right * 0.82f - up * 0.42f + forward * 0.16f);
                OrientHand(rf, rh, false, forward, up);
                CurlHand(rh, false, rightKnob, forward, up);
                grip = CapturePose(clone);
                HandFrame leftFrame = MeasureHandFrame(lh, true);
                HandFrame rightFrame = MeasureHandFrame(rh, false);
                metrics = new Metrics(
                    Vector3.Distance(lh.position, leftWrist),
                    Vector3.Distance(rh.position, rightWrist),
                    Elbow(lu, lf, lh), Elbow(ru, rf, rh),
                    MaxLower(start, grip),
                    Vector3.Angle(leftFrame.Finger, forward),
                    Vector3.Angle(rightFrame.Finger, forward),
                    Vector3.Angle(leftFrame.Dorsal, up),
                    Vector3.Angle(rightFrame.Dorsal, up),
                    MaximumFingerChordForwardAngle(lh, true, forward),
                    MaximumFingerChordForwardAngle(rh, false, forward),
                    MaximumFingerBulgeDownAngle(lh, true, forward, up),
                    MaximumFingerBulgeDownAngle(rh, false, forward, up),
                    MinimumFingerCurlAngle(lh, true),
                    MinimumFingerCurlAngle(rh, false));
            }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
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
            clip.name = "Helm_Enter_WheelGrip";
            clip.frameRate = Rate;
            clip.wrapMode = WrapMode.Loop;
            int frames = Mathf.RoundToInt(Duration * Rate);
            foreach (string path in start.Rotation.Keys.OrderBy(value => value, StringComparer.Ordinal))
            {
                Quaternion a = Normalize(start.Rotation[path]);
                Quaternion b = Normalize(grip.Rotation[path]);
                if (Quaternion.Dot(a, b) < 0f) b = Negate(b);
                var x = new AnimationCurve(); var y = new AnimationCurve();
                var z = new AnimationCurve(); var w = new AnimationCurve();
                Quaternion previous = a;
                for (int frame = 0; frame <= frames; frame++)
                {
                    float time = frame / (float)Rate;
                    float p = time >= Transition ? 1f : Ease(time / Transition);
                    Quaternion q = Normalize(Quaternion.SlerpUnclamped(a, b, p));
                    if (Quaternion.Dot(previous, q) < 0f) q = Negate(q);
                    previous = q;
                    x.AddKey(time, q.x); y.AddKey(time, q.y);
                    z.AddKey(time, q.z); w.AddKey(time, q.w);
                }
                Linear(x); Linear(y); Linear(z); Linear(w);
                Curve(clip, path, "m_LocalRotation.x", x);
                Curve(clip, path, "m_LocalRotation.y", y);
                Curve(clip, path, "m_LocalRotation.z", z);
                Curve(clip, path, "m_LocalRotation.w", w);
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
            AnimatorState state = controller.layers[0].stateMachine.AddState("HelmEnterWheelGrip");
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
            GameObject probe = Clone(target, "HelmEnter_Metrics", false);
            try
            {
                clip.SampleAnimation(probe, 0f);
                Pose start = CapturePose(probe);
                clip.SampleAnimation(probe, Transition);
                Transform lu = FindPath(probe.transform, LU); Transform lf = FindPath(probe.transform, LF);
                Transform lh = FindPath(probe.transform, LH); Transform ru = FindPath(probe.transform, RU);
                Transform rf = FindPath(probe.transform, RF); Transform rh = FindPath(probe.transform, RH);
                Vector3 up = probe.transform.up.normalized;
                Vector3 forward = probe.transform.forward.normalized;
                Vector3 right = probe.transform.right.normalized;
                Vector3 hub = (lu.position + ru.position) * 0.5f + forward * 0.44f - up * 0.30f;
                Vector3 lk = hub + right * geometry.LeftOffset.x + up * geometry.LeftOffset.y;
                Vector3 rk = hub + right * geometry.RightOffset.x + up * geometry.RightOffset.y;
                Vector3 lw = lk - (lk - hub).normalized * 0.082f - forward * 0.060f;
                Vector3 rw = rk - (rk - hub).normalized * 0.082f - forward * 0.060f;
                HandFrame leftFrame = MeasureHandFrame(lh, true);
                HandFrame rightFrame = MeasureHandFrame(rh, false);
                return new Metrics(Vector3.Distance(lh.position, lw),
                    Vector3.Distance(rh.position, rw), Elbow(lu, lf, lh),
                    Elbow(ru, rf, rh), MaxLower(start, CapturePose(probe)),
                    Vector3.Angle(leftFrame.Finger, forward),
                    Vector3.Angle(rightFrame.Finger, forward),
                    Vector3.Angle(leftFrame.Dorsal, up),
                    Vector3.Angle(rightFrame.Dorsal, up),
                    MaximumFingerChordForwardAngle(lh, true, forward),
                    MaximumFingerChordForwardAngle(rh, false, forward),
                    MaximumFingerBulgeDownAngle(lh, true, forward, up),
                    MaximumFingerBulgeDownAngle(rh, false, forward, up),
                    MinimumFingerCurlAngle(lh, true),
                    MinimumFingerCurlAngle(rh, false));
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        private static void RequireMetrics(Metrics value)
        {
            if (value.LeftError > 0.012f || value.RightError > 0.012f ||
                value.LeftElbow < 20f || value.LeftElbow > 120f ||
                value.RightElbow < 20f || value.RightElbow > 120f ||
                value.LowerError > 0.02f ||
                value.LeftFingerForwardAngle > 0.5f ||
                value.RightFingerForwardAngle > 0.5f ||
                value.LeftDorsalUpAngle > 0.5f ||
                value.RightDorsalUpAngle > 0.5f ||
                value.LeftFingerChordForwardAngle > 0.5f ||
                value.RightFingerChordForwardAngle > 0.5f ||
                value.LeftFingerBulgeDownAngle > 0.5f ||
                value.RightFingerBulgeDownAngle > 0.5f ||
                value.LeftMinimumFingerCurlAngle < 20f ||
                value.RightMinimumFingerCurlAngle < 20f)
                throw new InvalidOperationException("Helm grip metrics failed. " + value.Describe());
        }

        private static void Solve(Transform upper, Transform fore, Transform hand,
            Vector3 goalRequested, Vector3 poleRequested)
        {
            Vector3 root = upper.position;
            Vector3 joint = fore.position;
            Vector3 end = hand.position;
            float a = Vector3.Distance(root, joint);
            float b = Vector3.Distance(joint, end);
            Vector3 request = goalRequested - root;
            float distance = Mathf.Clamp(request.magnitude,
                Mathf.Abs(a - b) + 0.0005f, a + b - 0.012f);
            Vector3 direction = request.normalized;
            Vector3 goal = root + direction * distance;
            Vector3 pole = Vector3.ProjectOnPlane(poleRequested, direction);
            if (pole.sqrMagnitude < 0.000001f)
                pole = Vector3.ProjectOnPlane(joint - root, direction);
            pole.Normalize();
            float along = (a * a + distance * distance - b * b) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
            Vector3 desiredJoint = root + direction * along + pole * height;
            upper.rotation = Quaternion.FromToRotation(joint - root, desiredJoint - root) * upper.rotation;
            joint = fore.position;
            end = hand.position;
            fore.rotation = Quaternion.FromToRotation(end - joint, goal - joint) * fore.rotation;
        }

        private static void OrientHand(Transform fore, Transform hand, bool left,
            Vector3 desiredFinger, Vector3 desiredDorsal)
        {
            desiredFinger.Normalize();
            desiredDorsal = Vector3.ProjectOnPlane(
                desiredDorsal, desiredFinger).normalized;

            HandFrame frame = MeasureHandFrame(hand, left);
            Vector3 foreAxis = (hand.position - fore.position).normalized;
            Vector3 currentForeDorsal = Vector3.ProjectOnPlane(
                frame.Dorsal, foreAxis).normalized;
            Vector3 desiredForeDorsal = Vector3.ProjectOnPlane(
                desiredDorsal, foreAxis).normalized;
            if (currentForeDorsal.sqrMagnitude > 0.000001f &&
                desiredForeDorsal.sqrMagnitude > 0.000001f)
            {
                float foreTwist = Vector3.SignedAngle(
                    currentForeDorsal, desiredForeDorsal, foreAxis) * 0.65f;
                foreTwist = Mathf.Clamp(foreTwist, -78f, 78f);
                fore.rotation = Quaternion.AngleAxis(
                    foreTwist, foreAxis) * fore.rotation;
            }

            frame = MeasureHandFrame(hand, left);
            hand.rotation = Quaternion.FromToRotation(
                frame.Finger, desiredFinger) * hand.rotation;
            frame = MeasureHandFrame(hand, left);
            Vector3 currentDorsal = Vector3.ProjectOnPlane(
                frame.Dorsal, desiredFinger).normalized;
            float residualTwist = Vector3.SignedAngle(
                currentDorsal, desiredDorsal, desiredFinger);
            hand.rotation = Quaternion.AngleAxis(
                residualTwist, desiredFinger) * hand.rotation;
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
            if (left) dorsal = -dorsal;
            return new HandFrame(finger, dorsal);
        }

        private static void CurlHand(Transform hand, bool left, Vector3 center,
            Vector3 desiredForward, Vector3 desiredUp)
        {
            string prefix = left ? "Left" : "Right";
            foreach (string digit in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                Transform p = Child(hand, prefix + digit + "Proximal");
                Transform m = Child(p, prefix + digit + "Intermediate");
                Transform d = Child(m, prefix + digit + "Distal");
                for (int i = 0; i < 3; i++)
                {
                    Bend(p, m, center, digit == "Thumb" ? 24f : 38f);
                    Bend(m, d, center, digit == "Thumb" ? 30f : 46f);
                    Vector3 from = (d.position - m.position).normalized;
                    Vector3 to = (center - d.position).normalized;
                    Vector3 axis = Vector3.Cross(from, to);
                    if (axis.sqrMagnitude > 0.000001f)
                        d.rotation = Quaternion.AngleAxis(
                            digit == "Thumb" ? 16f : 24f, axis.normalized) * d.rotation;
                }
                OrientCurledFingerForward(p, m, d, desiredForward, desiredUp);
            }
        }

        private static void OrientCurledFingerForward(
            Transform proximal, Transform intermediate, Transform distal,
            Vector3 desiredForward, Vector3 desiredUp)
        {
            desiredForward.Normalize();
            desiredUp = Vector3.ProjectOnPlane(desiredUp, desiredForward).normalized;
            Vector3 tip = FingerTip(intermediate, distal);
            Vector3 chord = (tip - proximal.position).normalized;
            proximal.rotation = Quaternion.FromToRotation(
                chord, desiredForward) * proximal.rotation;

            tip = FingerTip(intermediate, distal);
            Vector3 bulge = FingerBulge(
                proximal, intermediate, distal, desiredForward, tip);
            if (bulge.sqrMagnitude > 0.000001f)
            {
                float twist = Vector3.SignedAngle(
                    bulge.normalized, -desiredUp, desiredForward);
                proximal.rotation = Quaternion.AngleAxis(
                    twist, desiredForward) * proximal.rotation;
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
            Vector3 forward, Vector3 tip)
        {
            Vector3 middle = (intermediate.position + distal.position) * 0.5f;
            float along = Vector3.Dot(middle - proximal.position, forward);
            Vector3 linePoint = proximal.position + forward * along;
            return Vector3.ProjectOnPlane(middle - linePoint, forward);
        }

        private static float MaximumFingerChordForwardAngle(
            Transform hand, bool left, Vector3 forward)
        {
            string prefix = left ? "Left" : "Right";
            float maximum = 0f;
            foreach (string digit in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                Transform p = Child(hand, prefix + digit + "Proximal");
                Transform m = Child(p, prefix + digit + "Intermediate");
                Transform d = Child(m, prefix + digit + "Distal");
                maximum = Mathf.Max(maximum, Vector3.Angle(
                    FingerTip(m, d) - p.position, forward));
            }
            return maximum;
        }

        private static float MaximumFingerBulgeDownAngle(
            Transform hand, bool left, Vector3 forward, Vector3 up)
        {
            string prefix = left ? "Left" : "Right";
            float maximum = 0f;
            foreach (string digit in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                Transform p = Child(hand, prefix + digit + "Proximal");
                Transform m = Child(p, prefix + digit + "Intermediate");
                Transform d = Child(m, prefix + digit + "Distal");
                Vector3 bulge = FingerBulge(p, m, d, forward, FingerTip(m, d));
                if (bulge.sqrMagnitude > 0.000001f)
                    maximum = Mathf.Max(maximum,
                        Vector3.Angle(bulge, -up));
            }
            return maximum;
        }

        private static float MinimumFingerCurlAngle(Transform hand, bool left)
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
                Vector3 third = distalTipDirection(d);
                minimum = Mathf.Min(minimum,
                    Vector3.Angle(first, second) + Vector3.Angle(second, third));
            }
            return minimum;
        }

        private static Vector3 distalTipDirection(Transform distal)
        {
            return distal.up.normalized;
        }

        private static void Bend(Transform joint, Transform child,
            Vector3 target, float maximum)
        {
            Quaternion wanted = Quaternion.FromToRotation(
                (child.position - joint.position).normalized,
                (target - joint.position).normalized) * joint.rotation;
            joint.rotation = Quaternion.RotateTowards(joint.rotation, wanted, maximum);
        }

        private static GameObject CreateWheel(Scene scene, Transform actor, Geometry geometry)
        {
            Transform sourceRoot = FindTransform(scene, HelmRoot);
            var root = new GameObject("HelmEnter_ReadOnlyWheelPreview");
            root.hideFlags = HideFlags.HideAndDontSave;
            Transform lu = FindPath(actor, LU); Transform ru = FindPath(actor, RU);
            Vector3 hub = (lu.position + ru.position) * 0.5f +
                actor.forward * 0.44f - actor.up * 0.30f;
            foreach (string name in WheelParts)
            {
                Transform source = sourceRoot.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == name);
                GameObject clone = UnityEngine.Object.Instantiate(source.gameObject);
                clone.name = source.name;
                clone.hideFlags = HideFlags.HideAndDontSave;
                clone.transform.SetParent(root.transform, true);
                clone.transform.position = hub + actor.rotation * (source.position - geometry.Hub);
                clone.transform.rotation = actor.rotation * source.rotation;
                clone.transform.localScale = source.lossyScale;
                Layer(clone, PreviewLayer);
            }
            Layer(root, PreviewLayer);
            return root;
        }

        private static Texture2D Render(GameObject actor, GameObject wheel,
            AnimationClip clip, float time, bool close, bool front)
        {
            clip.SampleAnimation(actor, Mathf.Clamp(time, 0f, clip.length));
            bool wheelActive = wheel.activeSelf;
            wheel.SetActive(!front);
            SkinnedMeshRenderer[] skins =
                actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer skin in skins)
                skin.updateWhenOffscreen = true;
            Bounds bounds = front ? BoundsOf(actor) : BoundsOf(actor, wheel);
            var bakedObjects = new List<GameObject>();
            var bakedMeshes = new List<Mesh>();
            var enabledStates = new Dictionary<SkinnedMeshRenderer, bool>();
            foreach (SkinnedMeshRenderer skin in skins)
            {
                enabledStates.Add(skin, skin.enabled);
                if (!skin.enabled || skin.sharedMesh == null) continue;
                var mesh = new Mesh { name = skin.sharedMesh.name + "_HelmEnterPreviewBake" };
                skin.BakeMesh(mesh, false);
                var baked = new GameObject(skin.name + "_HelmEnterPreviewBake");
                baked.hideFlags = HideFlags.HideAndDontSave;
                baked.layer = PreviewLayer;
                baked.transform.SetPositionAndRotation(
                    skin.transform.position, skin.transform.rotation);
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
            Vector3 handsCenter = (
                FindPath(actor.transform, LH).position +
                FindPath(actor.transform, RH).position) * 0.5f;
            Vector3 focus = front
                ? handsCenter - up * 0.08f
                : close
                ? handsCenter + forward * 0.035f
                : bounds.center + forward * 0.04f + up * 0.05f;
            Vector3 position = front
                ? focus + forward * 2.05f + up * 0.05f
                : close
                ? focus - right * 1.48f - forward * 0.08f + up * 0.10f
                : focus - forward * 2.85f + right * 1.95f + up * 0.34f;
            var cameraObject = new GameObject("HelmEnter_FinalCamera");
            var lightObject = new GameObject("HelmEnter_FinalLight");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            RenderTexture rt = RenderTexture.GetTemporary(Panel, Panel, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << PreviewLayer;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 12f;
                camera.fieldOfView = front ? 32f : close ? 31f : 38f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
                camera.transform.SetPositionAndRotation(position,
                    Quaternion.LookRotation((focus - position).normalized, up));
                camera.targetTexture = rt;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.transform.rotation = Quaternion.LookRotation(
                    -forward - right * 0.35f - up * 0.55f, up);
                camera.Render();
                RenderTexture.active = rt;
                var image = new Texture2D(Panel, Panel, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Panel, Panel), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                foreach (KeyValuePair<SkinnedMeshRenderer, bool> item in enabledStates)
                    if (item.Key != null) item.Key.enabled = item.Value;
                foreach (GameObject baked in bakedObjects)
                    UnityEngine.Object.DestroyImmediate(baked);
                foreach (Mesh mesh in bakedMeshes)
                    UnityEngine.Object.DestroyImmediate(mesh);
                wheel.SetActive(wheelActive);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D Combine(IReadOnlyList<Texture2D> panels)
        {
            int width = panels.Count * Panel + (panels.Count - 1) * Gap;
            var result = new Texture2D(width, Panel, TextureFormat.RGB24, false);
            result.SetPixels32(Enumerable.Repeat(new Color32(15, 18, 26, 255),
                width * Panel).ToArray());
            for (int i = 0; i < panels.Count; i++)
                result.SetPixels(i * (Panel + Gap), 0, Panel, Panel, panels[i].GetPixels());
            result.Apply(false, false);
            return result;
        }

        private static Bounds BoundsOf(params GameObject[] roots)
        {
            Renderer[] renderers = roots.SelectMany(root =>
                root.GetComponentsInChildren<Renderer>(true)).Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Preview has no renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
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
                all.ToDictionary(t => Relative(root.transform, t), t => t.localRotation,
                    StringComparer.Ordinal),
                all.ToDictionary(t => Relative(root.transform, t), t => t.localPosition,
                    StringComparer.Ordinal),
                all.ToDictionary(t => Relative(root.transform, t), t => t.localScale,
                    StringComparer.Ordinal));
        }

        private static float MaxLower(Pose a, Pose b)
        {
            string[] tokens = { "/LeftUpLeg", "/LeftLeg", "/LeftFoot", "/LeftToeBase",
                "/RightUpLeg", "/RightLeg", "/RightFoot", "/RightToeBase" };
            return a.Rotation.Keys.Where(path => tokens.Any(path.Contains))
                .Max(path => Quaternion.Angle(a.Rotation[path], b.Rotation[path]));
        }

        private static float MaxRotation(Pose a, Pose b) =>
            a.Rotation.Keys.Intersect(b.Rotation.Keys)
                .Max(path => Quaternion.Angle(a.Rotation[path], b.Rotation[path]));

        private static Geometry ReadGeometry(Scene scene)
        {
            Transform root = FindTransform(scene, HelmRoot);
            Transform hub = root.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == HubName);
            Vector3 left = root.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == LeftKnobName).position - hub.position;
            Vector3 right = root.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == RightKnobName).position - hub.position;
            if (left.y <= 0f || right.y <= 0f || left.x >= 0f || right.x <= 0f)
                throw new InvalidOperationException("Cockpit wheel 10/2 geometry is unexpected.");
            return new Geometry(hub.position, left, right);
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
            if (animator != null) animator.enabled = false;
            return clone;
        }

        private static void Hide(GameObject root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        private static void Layer(GameObject root, int layer)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        private static float Elbow(Transform upper, Transform fore, Transform hand) =>
            180f - Vector3.Angle(upper.position - fore.position, hand.position - fore.position);

        private static float Ease(float value)
        {
            value = Mathf.Clamp01(value);
            float q = value * value;
            float c = q * value;
            return c * (10f - 15f * value + 6f * q);
        }

        private static void ConstantVector(AnimationClip clip, string path,
            string property, Vector3 value)
        {
            Curve(clip, path, property + ".x", AnimationCurve.Linear(0f, value.x, Duration, value.x));
            Curve(clip, path, property + ".y", AnimationCurve.Linear(0f, value.y, Duration, value.y));
            Curve(clip, path, property + ".z", AnimationCurve.Linear(0f, value.z, Duration, value.z));
        }

        private static void Curve(AnimationClip clip, string path,
            string property, AnimationCurve curve) =>
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);

        private static void Linear(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active: " + scene.path);
            return scene;
        }

        private static GameObject Find(Scene scene, string name) =>
            FindTransform(scene, name).gameObject;

        private static Transform FindTransform(Scene scene, string name)
        {
            Transform[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(name + " count=" + matches.Length);
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject root) =>
            root.GetComponent<Animator>() ??
            throw new InvalidOperationException(root.name + " Animator is missing.");

        private static Transform FindPath(Transform root, string path) =>
            root.Find(path) ?? throw new InvalidOperationException(path + " is missing.");

        private static Transform Child(Transform root, string name) =>
            root.Find(name) ?? throw new InvalidOperationException(name + " is missing.");

        private static string Relative(Transform root, Transform item) =>
            AnimationUtility.CalculateTransformPath(item, root);

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Helm_Enter tooling requires Edit Mode.");
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

        private static string Absolute(string path)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root unavailable.");
            return Path.GetFullPath(Path.Combine(root,
                path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static Quaternion Normalize(Quaternion q)
        {
            float m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return m < 0.000001f ? Quaternion.identity :
                new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m);
        }

        private static Quaternion Negate(Quaternion q) =>
            new Quaternion(-q.x, -q.y, -q.z, -q.w);

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 v) =>
            "(" + F(v.x) + "," + F(v.y) + "," + F(v.z) + ")";
        private static string Quat(Quaternion q) =>
            "(" + F(q.x) + "," + F(q.y) + "," + F(q.z) + "," + F(q.w) + ")";

        private sealed class Pose
        {
            internal Pose(Dictionary<string, Quaternion> rotation,
                Dictionary<string, Vector3> position, Dictionary<string, Vector3> scale)
            { Rotation = rotation; Position = position; Scale = scale; }
            internal Dictionary<string, Quaternion> Rotation { get; }
            internal Dictionary<string, Vector3> Position { get; }
            internal Dictionary<string, Vector3> Scale { get; }
        }

        private readonly struct Geometry
        {
            internal Geometry(Vector3 hub, Vector3 leftOffset, Vector3 rightOffset)
            { Hub = hub; LeftOffset = leftOffset; RightOffset = rightOffset; }
            internal Vector3 Hub { get; }
            internal Vector3 LeftOffset { get; }
            internal Vector3 RightOffset { get; }
        }

        private readonly struct Metrics
        {
            internal Metrics(float leftError, float rightError,
                float leftElbow, float rightElbow, float lowerError,
                float leftFingerForwardAngle, float rightFingerForwardAngle,
                float leftDorsalUpAngle, float rightDorsalUpAngle,
                float leftFingerChordForwardAngle,
                float rightFingerChordForwardAngle,
                float leftFingerBulgeDownAngle,
                float rightFingerBulgeDownAngle,
                float leftMinimumFingerCurlAngle,
                float rightMinimumFingerCurlAngle)
            {
                LeftError = leftError; RightError = rightError;
                LeftElbow = leftElbow; RightElbow = rightElbow;
                LowerError = lowerError;
                LeftFingerForwardAngle = leftFingerForwardAngle;
                RightFingerForwardAngle = rightFingerForwardAngle;
                LeftDorsalUpAngle = leftDorsalUpAngle;
                RightDorsalUpAngle = rightDorsalUpAngle;
                LeftFingerChordForwardAngle = leftFingerChordForwardAngle;
                RightFingerChordForwardAngle = rightFingerChordForwardAngle;
                LeftFingerBulgeDownAngle = leftFingerBulgeDownAngle;
                RightFingerBulgeDownAngle = rightFingerBulgeDownAngle;
                LeftMinimumFingerCurlAngle = leftMinimumFingerCurlAngle;
                RightMinimumFingerCurlAngle = rightMinimumFingerCurlAngle;
            }
            internal float LeftError { get; }
            internal float RightError { get; }
            internal float LeftElbow { get; }
            internal float RightElbow { get; }
            internal float LowerError { get; }
            internal float LeftFingerForwardAngle { get; }
            internal float RightFingerForwardAngle { get; }
            internal float LeftDorsalUpAngle { get; }
            internal float RightDorsalUpAngle { get; }
            internal float LeftFingerChordForwardAngle { get; }
            internal float RightFingerChordForwardAngle { get; }
            internal float LeftFingerBulgeDownAngle { get; }
            internal float RightFingerBulgeDownAngle { get; }
            internal float LeftMinimumFingerCurlAngle { get; }
            internal float RightMinimumFingerCurlAngle { get; }
            internal string Describe() =>
                "leftGripErrorMeters=" + F(LeftError) + "\n" +
                "rightGripErrorMeters=" + F(RightError) + "\n" +
                "leftElbowBendDegrees=" + F(LeftElbow) + "\n" +
                "rightElbowBendDegrees=" + F(RightElbow) + "\n" +
                "lowerBodyMaximumRotationErrorDegrees=" + F(LowerError) + "\n" +
                "leftFingerForwardAngleDegrees=" + F(LeftFingerForwardAngle) + "\n" +
                "rightFingerForwardAngleDegrees=" + F(RightFingerForwardAngle) + "\n" +
                "leftDorsalUpAngleDegrees=" + F(LeftDorsalUpAngle) + "\n" +
                "rightDorsalUpAngleDegrees=" + F(RightDorsalUpAngle) + "\n" +
                "leftFingerChordForwardAngleDegrees=" + F(LeftFingerChordForwardAngle) + "\n" +
                "rightFingerChordForwardAngleDegrees=" + F(RightFingerChordForwardAngle) + "\n" +
                "leftFingerBulgeDownAngleDegrees=" + F(LeftFingerBulgeDownAngle) + "\n" +
                "rightFingerBulgeDownAngleDegrees=" + F(RightFingerBulgeDownAngle) + "\n" +
                "leftMinimumFingerCurlAngleDegrees=" + F(LeftMinimumFingerCurlAngle) + "\n" +
                "rightMinimumFingerCurlAngleDegrees=" + F(RightMinimumFingerCurlAngle);
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
    }
}
