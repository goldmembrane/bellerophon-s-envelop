using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class HoloSprayIdleLocomotionPlayModeCapture
    {
        private const int MotionCount = 6;
        private const int FullPanelCount = MotionCount * 2;
        private const int GripPanelCount = MotionCount;
        private const float CapturePhaseTime = 0.48f;
        private const double TimeoutSeconds = 32d;

        private static readonly Color[] PhaseColors =
        {
            new Color(0.35f, 0.75f, 1f),
            new Color(0.3f, 0.9f, 0.45f),
            new Color(1f, 0.65f, 0.25f),
            new Color(0.9f, 0.35f, 0.85f),
            new Color(0.95f, 0.85f, 0.25f),
            new Color(1f, 0.3f, 0.3f)
        };

        private static bool active;
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static double startTime;
        private static int baseAbsolutePhase = -1;
        private static int nextPanel;
        private static GameObject target;
        private static Animator animator;
        private static Texture2D[] fullPanels;
        private static Texture2D[] gripPanels;
        private static readonly List<string> Observations = new List<string>();

        private static bool swayBaselineSet;
        private static Quaternion initialSpine;
        private static Quaternion initialHead;
        private static Quaternion initialLeftArm;
        private static Quaternion initialRightShoulder;
        private static float maximumSpineSway;
        private static float maximumHeadSway;
        private static float maximumLeftArmSway;
        private static float maximumRightShoulderSway;
        private static float maximumFollowPosition;
        private static float maximumFollowRotation;
        private static float maximumCanUp;
        private static float maximumCanFront;
        private static float maximumPalmLeft;
        private static float maximumWrist;
        private static float maximumArmForward;
        private static float maximumElbowBend;
        private static float maximumArmDeviation;
        private static float maximumForeArmDeviation;
        private static float maximumHandDeviation;
        private static float maximumFingerDeviation;

        static HoloSprayIdleLocomotionPlayModeCapture()
        {
            EditorApplication.update -= Tick;
        }

        internal static bool HasPendingCapture => active;

        internal static void Start(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (!EditorApplication.isPlaying)
            {
                failCallback(new InvalidOperationException(
                    "EnterHoloSprayIdleLocomotionReview must reach Play Mode before capture."));
                return;
            }
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }

            try
            {
                active = true;
                complete = completeCallback;
                fail = failCallback;
                startTime = EditorApplication.timeSinceStartup;
                baseAbsolutePhase = -1;
                nextPanel = 0;
                target = HoloSprayIdleLocomotionTools.RequireRuntimeTarget();
                animator = target.GetComponent<Animator>() ??
                    throw new MissingReferenceException(
                        "HoloSpray_Idle runtime Animator is missing.");
                if (!animator.enabled || animator.applyRootMotion ||
                    animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException(
                        "HoloSpray_Idle runtime Animator settings are invalid.");
                fullPanels = new Texture2D[FullPanelCount];
                gripPanels = new Texture2D[GripPanelCount];
                Observations.Clear();
                ResetMetrics();
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        internal static void Resume(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }
            Start(completeCallback, failCallback);
        }

        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException(
                        "Play Mode ended before HoloSpray_Idle capture completed.");
                if (EditorApplication.timeSinceStartup - startTime > TimeoutSeconds)
                    throw new TimeoutException(
                        "HoloSpray_Idle natural two-cycle capture exceeded 32 seconds.");
                if (!animator.isInitialized ||
                    !HoloSprayIdleLocomotionCycleBehaviour.TryGetSequenceState(
                        animator, out int absolutePhase, out int phase, out float phaseElapsed))
                    return;

                if (baseAbsolutePhase < 0)
                {
                    if (phase != 0 || phaseElapsed > 0.65f) return;
                    baseAbsolutePhase = absolutePhase;
                }

                if (nextPanel < FullPanelCount)
                {
                    int expectedAbsolutePhase = baseAbsolutePhase + nextPanel;
                    if (absolutePhase < expectedAbsolutePhase ||
                        phaseElapsed < CapturePhaseTime) return;
                    if (absolutePhase > expectedAbsolutePhase)
                        throw new InvalidOperationException(
                            "A natural HoloSpray_Idle sequence phase was missed.");
                    int expectedPhase = nextPanel % MotionCount;
                    if (phase != expectedPhase)
                        throw new InvalidOperationException(
                            "Observed HoloSpray_Idle phase order differs from the request.");

                    Vector2 expectedMove =
                        HoloSprayIdleLocomotionCycleBehaviour.MotionPosition(expectedPhase);
                    if (Mathf.Abs(animator.GetFloat(
                            HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter) -
                            expectedMove.x) > 0.001f ||
                        Mathf.Abs(animator.GetFloat(
                            HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter) -
                            expectedMove.y) > 0.001f)
                        throw new InvalidOperationException(
                            "HoloSpray_Idle Blend Tree parameters differ from the natural phase.");

                    AccumulateMetrics();
                    Texture2D full = HoloSprayIdleLocomotionTools.CaptureRuntimePanel(false);
                    DrawBorder(full, PhaseColors[phase]);
                    fullPanels[nextPanel] = full;
                    if (nextPanel < GripPanelCount)
                    {
                        Texture2D grip =
                            HoloSprayIdleLocomotionTools.CaptureRuntimePanel(true);
                        DrawBorder(grip, PhaseColors[phase]);
                        gripPanels[nextPanel] = grip;
                    }
                    HoloSprayIdleLocomotionTools.RuntimePoseMetrics metrics =
                        HoloSprayIdleLocomotionTools.MeasureRuntimePose();
                    Observations.Add(
                        "panel=" + nextPanel +
                        "|cycle=" + (nextPanel / MotionCount + 1) +
                        "|phase=" + phase +
                        "|motion=" +
                            HoloSprayIdleLocomotionCycleBehaviour.MotionName(phase) +
                        "|phaseElapsed=" + F(phaseElapsed) +
                        "|moveX=" + F(animator.GetFloat(
                            HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter)) +
                        "|moveY=" + F(animator.GetFloat(
                            HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter)) +
                        "|followPosition=" + F(metrics.FollowPosition) +
                        "|followRotation=" + F(metrics.FollowRotation) +
                        "|palmLeft=" + F(metrics.PalmLeft) +
                        "|wrist=" + F(metrics.Wrist));
                    nextPanel++;
                    return;
                }

                if (absolutePhase < baseAbsolutePhase + FullPanelCount) return;
                if (absolutePhase != baseAbsolutePhase + FullPanelCount || phase != 0)
                    throw new InvalidOperationException(
                        "RunForward did not naturally return to Idle after two cycles.");
                Finish();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void AccumulateMetrics()
        {
            HoloSprayIdleLocomotionTools.RuntimePoseMetrics metrics =
                HoloSprayIdleLocomotionTools.MeasureRuntimePose();
            maximumFollowPosition = Mathf.Max(
                maximumFollowPosition, metrics.FollowPosition);
            maximumFollowRotation = Mathf.Max(
                maximumFollowRotation, metrics.FollowRotation);
            maximumCanUp = Mathf.Max(maximumCanUp, metrics.CanUp);
            maximumCanFront = Mathf.Max(maximumCanFront, metrics.CanFront);
            maximumPalmLeft = Mathf.Max(maximumPalmLeft, metrics.PalmLeft);
            maximumWrist = Mathf.Max(maximumWrist, metrics.Wrist);
            maximumArmForward = Mathf.Max(maximumArmForward, metrics.ArmForward);
            maximumElbowBend = Mathf.Max(maximumElbowBend, metrics.ElbowBend);
            maximumArmDeviation = Mathf.Max(
                maximumArmDeviation, metrics.ArmDeviation);
            maximumForeArmDeviation = Mathf.Max(
                maximumForeArmDeviation, metrics.ForeArmDeviation);
            maximumHandDeviation = Mathf.Max(
                maximumHandDeviation, metrics.HandDeviation);
            maximumFingerDeviation = Mathf.Max(
                maximumFingerDeviation, metrics.FingerDeviation);

            if (!swayBaselineSet)
            {
                swayBaselineSet = true;
                initialSpine = metrics.Spine;
                initialHead = metrics.Head;
                initialLeftArm = metrics.LeftArm;
                initialRightShoulder = metrics.RightShoulder;
                return;
            }
            maximumSpineSway = Mathf.Max(
                maximumSpineSway, Quaternion.Angle(initialSpine, metrics.Spine));
            maximumHeadSway = Mathf.Max(
                maximumHeadSway, Quaternion.Angle(initialHead, metrics.Head));
            maximumLeftArmSway = Mathf.Max(
                maximumLeftArmSway, Quaternion.Angle(initialLeftArm, metrics.LeftArm));
            maximumRightShoulderSway = Mathf.Max(
                maximumRightShoulderSway,
                Quaternion.Angle(initialRightShoulder, metrics.RightShoulder));
        }

        private static void Finish()
        {
            HoloSprayIdleLocomotionTools.WriteRuntimeReport(
                "runtime_metrics.txt",
                new StringBuilder()
                    .AppendLine("maximumFollowPositionError=" + F(maximumFollowPosition))
                    .AppendLine("maximumFollowRotationErrorDegrees=" + F(maximumFollowRotation))
                    .AppendLine("maximumCanUpDeviationDegrees=" + F(maximumCanUp))
                    .AppendLine("maximumCanFrontDeviationDegrees=" + F(maximumCanFront))
                    .AppendLine("maximumPalmLeftDeviationDegrees=" + F(maximumPalmLeft))
                    .AppendLine("maximumWristStraightnessDegrees=" + F(maximumWrist))
                    .AppendLine("maximumArmForwardDeviationDegrees=" + F(maximumArmForward))
                    .AppendLine("maximumElbowBendDegrees=" + F(maximumElbowBend))
                    .AppendLine("maximumRightArmCarryDeviationDegrees=" + F(maximumArmDeviation))
                    .AppendLine("maximumRightForeArmCarryDeviationDegrees=" +
                        F(maximumForeArmDeviation))
                    .AppendLine("maximumRightHandCarryDeviationDegrees=" +
                        F(maximumHandDeviation))
                    .AppendLine("maximumRightFingerGripDeviationDegrees=" +
                        F(maximumFingerDeviation))
                    .AppendLine("maximumSpineSwayDegrees=" + F(maximumSpineSway))
                    .AppendLine("maximumHeadSwayDegrees=" + F(maximumHeadSway))
                    .AppendLine("maximumLeftArmSwayDegrees=" + F(maximumLeftArmSway))
                    .AppendLine("maximumRightShoulderSwayDegrees=" +
                        F(maximumRightShoulderSway))
                    .ToString());
            RequireAtMost(maximumFollowPosition, 0.00005f,
                "spray right-hand follow position error");
            RequireAtMost(maximumFollowRotation, 0.05f,
                "spray right-hand follow rotation error");
            RequireAtMost(maximumCanUp, 20f, "spray vertical deviation");
            RequireAtMost(maximumCanFront, 20f, "spray nozzle-forward deviation");
            RequireAtMost(maximumPalmLeft, 45f,
                "right palm root-relative angle during parent-body sway");
            RequireAtMost(maximumWrist, 20f, "right wrist straightness");
            RequireAtMost(maximumArmForward, 45f,
                "right arm root-relative angle during parent-body sway");
            RequireAtMost(maximumElbowBend, 60f, "right elbow bend");
            RequireAtMost(maximumArmDeviation, 40f, "right arm carry deviation");
            RequireAtMost(maximumForeArmDeviation, 30f,
                "right forearm carry deviation");
            RequireAtMost(maximumHandDeviation, 20f, "right hand carry deviation");
            RequireAtMost(maximumFingerDeviation, 0.05f,
                "right finger grip deviation");
            float maximumNaturalSway = Mathf.Max(
                Mathf.Max(maximumSpineSway, maximumHeadSway),
                Mathf.Max(maximumLeftArmSway, maximumRightShoulderSway));
            if (maximumNaturalSway <= 0.1f)
                throw new InvalidOperationException(
                    "Source upper-body locomotion sway was rigidly removed.");

            HoloSprayIdleLocomotionTools.ComposeRuntimeReview(
                fullPanels, gripPanels, HoloSprayIdleLocomotionTools.FinalAbsolutePath);
            var report = new StringBuilder()
                .AppendLine("HoloSpray_Idle natural locomotion runtime capture")
                .AppendLine("captureKind=Final")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cyclesObserved=2")
                .AppendLine("fullPanelsCaptured=12")
                .AppendLine("gripPanelsCaptured=6")
                .AppendLine(
                    "sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("returnedToIdleAfterRun=True")
                .AppendLine("sourceFullBodyMotionEnabled=True")
                .AppendLine("upperBodyNaturalSwayPreserved=True")
                .AppendLine("rightArmGripOffsetPreserved=True")
                .AppendLine("rightFingerGripLocked=True")
                .AppendLine("sprayFollowsAnimatedRightHand=True")
                .AppendLine("sprayPositionFollowWeight=1")
                .AppendLine("sprayRotationFollowWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.SprayRotationFollowWeight))
                .AppendLine("maximumFollowPositionError=" + F(maximumFollowPosition))
                .AppendLine("maximumFollowRotationErrorDegrees=" + F(maximumFollowRotation))
                .AppendLine("maximumCanUpDeviationDegrees=" + F(maximumCanUp))
                .AppendLine("maximumCanFrontDeviationDegrees=" + F(maximumCanFront))
                .AppendLine("maximumPalmLeftDeviationDegrees=" + F(maximumPalmLeft))
                .AppendLine("maximumWristStraightnessDegrees=" + F(maximumWrist))
                .AppendLine("maximumArmForwardDeviationDegrees=" + F(maximumArmForward))
                .AppendLine("maximumElbowBendDegrees=" + F(maximumElbowBend))
                .AppendLine("maximumRightArmCarryDeviationDegrees=" + F(maximumArmDeviation))
                .AppendLine(
                    "maximumRightForeArmCarryDeviationDegrees=" + F(maximumForeArmDeviation))
                .AppendLine("maximumRightHandCarryDeviationDegrees=" + F(maximumHandDeviation))
                .AppendLine("maximumRightFingerGripDeviationDegrees=" + F(maximumFingerDeviation))
                .AppendLine("maximumSpineSwayDegrees=" + F(maximumSpineSway))
                .AppendLine("maximumHeadSwayDegrees=" + F(maximumHeadSway))
                .AppendLine("maximumLeftArmSwayDegrees=" + F(maximumLeftArmSway))
                .AppendLine("maximumRightShoulderSwayDegrees=" + F(maximumRightShoulderSway));
            foreach (string observation in Observations) report.AppendLine(observation);
            HoloSprayIdleLocomotionTools.WriteRuntimeReport(
                "final_runtime.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Action<string> callback = complete;
            Cleanup();
            callback?.Invoke(
                "HoloSpray_Idle natural two-cycle final capture completed.");
        }

        private static void DrawBorder(Texture2D image, Color color)
        {
            const int width = 6;
            for (int y = 0; y < image.height; y++)
            for (int x = 0; x < image.width; x++)
                if (x < width || y < width ||
                    x >= image.width - width || y >= image.height - width)
                    image.SetPixel(x, y, color);
            image.Apply(false, false);
        }

        private static void RequireAtMost(float actual, float maximum, string label)
        {
            if (actual > maximum)
                throw new InvalidOperationException(
                    label + " exceeded. actual=" + F(actual) +
                    " maximum=" + F(maximum));
        }

        private static void ResetMetrics()
        {
            swayBaselineSet = false;
            maximumSpineSway = 0f;
            maximumHeadSway = 0f;
            maximumLeftArmSway = 0f;
            maximumRightShoulderSway = 0f;
            maximumFollowPosition = 0f;
            maximumFollowRotation = 0f;
            maximumCanUp = 0f;
            maximumCanFront = 0f;
            maximumPalmLeft = 0f;
            maximumWrist = 0f;
            maximumArmForward = 0f;
            maximumElbowBend = 0f;
            maximumArmDeviation = 0f;
            maximumForeArmDeviation = 0f;
            maximumHandDeviation = 0f;
            maximumFingerDeviation = 0f;
        }

        private static void Fail(Exception exception)
        {
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(exception);
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (fullPanels != null)
                foreach (Texture2D panel in fullPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (gripPanels != null)
                foreach (Texture2D panel in gripPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            active = false;
            complete = null;
            fail = null;
            target = null;
            animator = null;
            fullPanels = null;
            gripPanels = null;
            Observations.Clear();
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
