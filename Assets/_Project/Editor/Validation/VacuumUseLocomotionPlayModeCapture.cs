using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class VacuumUseLocomotionPlayModeCapture
    {
        private const string StateKey = "Bellerophon.VacuumUseLocomotionCapture.State";
        private const string IndexKey = "Bellerophon.VacuumUseLocomotionCapture.Index";
        private const string StartTimeKey = "Bellerophon.VacuumUseLocomotionCapture.StartTime";
        private const string FinalKey = "Bellerophon.VacuumUseLocomotionCapture.Final";
        private const string FailureKey = "Bellerophon.VacuumUseLocomotionCapture.Failure";
        private const string PreviousPhaseKey =
            "Bellerophon.VacuumUseLocomotionCapture.PreviousPhase";
        private const string BoundaryWaitStartKey =
            "Bellerophon.VacuumUseLocomotionCapture.BoundaryWaitStart";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int FailedWaitingForEditMode = 4;
        private const int WaitingForForwardBoundary = 5;

        private static readonly double[] CaptureTimes =
        {
            0.5d, 1.5d, 2.5d, 3.5d, 4.5d,
            5.5d, 6.5d, 7.5d, 8.5d, 9.5d
        };

        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static readonly List<string> Observations = new List<string>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static float maximumUpperPosition;
        private static float maximumUpperRotation;
        private static float maximumUpperScale;
        private static float maximumVacuumPosition;
        private static float maximumVacuumRotation;
        private static float maximumVacuumScale;

        static VacuumUseLocomotionPlayModeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        internal static bool HasPendingCapture => SessionState.GetInt(StateKey, 0) != 0;

        internal static void Start(
            bool final,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            bool alreadyPlaying = EditorApplication.isPlaying;
            if (EditorApplication.isPlayingOrWillChangePlaymode && !alreadyPlaying)
                throw new InvalidOperationException(
                    "Vacuum capture cannot start while Play Mode is changing.");
            if (!alreadyPlaying) VacuumUseLocomotionTools.Inspect();

            CleanupPanels();
            Observations.Clear();
            ResetMetrics();
            string destination = final
                ? VacuumUseLocomotionTools.FinalAbsolutePath
                : VacuumUseLocomotionTools.DiagnosticAbsolutePath;
            if (File.Exists(destination)) File.Delete(destination);
            SessionState.SetBool(FinalKey, final);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetInt(IndexKey, 0);
            if (alreadyPlaying)
            {
                BeginNaturalObservation();
                SessionState.SetInt(StateKey, WaitingForForwardBoundary);
            }
            else
            {
                SessionState.SetInt(StateKey, WaitingForPlayMode);
                EditorApplication.EnterPlaymode();
            }
        }

        internal static void Resume(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            int state = SessionState.GetInt(StateKey, 0);
            if ((state == WaitingForPlayMode || state == WaitingForForwardBoundary ||
                 state == Capturing) &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                bool final = SessionState.GetBool(FinalKey, false);
                CleanupSession();
                Start(final, completeCallback, failCallback);
                return;
            }
            complete = completeCallback;
            fail = failCallback;
            Tick();
        }

        private static void Tick()
        {
            if (complete == null && fail == null) return;
            int state = SessionState.GetInt(StateKey, 0);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying) return;
                    BeginNaturalObservation();
                    SessionState.SetInt(StateKey, WaitingForForwardBoundary);
                    return;
                }
                if (state == WaitingForForwardBoundary)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Unity left Play Mode before the natural forward boundary.");
                    WaitForNaturalForwardBoundary();
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Unity left Play Mode before two natural cycles completed.");
                    CaptureWhenDue();
                    return;
                }
                if (state == WaitingForEditMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    FinishSuccess();
                    return;
                }
                if (state == FailedWaitingForEditMode &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                    FinishFailure();
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                SessionState.SetInt(StateKey, FailedWaitingForEditMode);
                CleanupPanels();
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                else if (!EditorApplication.isPlayingOrWillChangePlaymode) FinishFailure();
            }
        }

        private static void BeginNaturalObservation()
        {
            GameObject target = VacuumUseLocomotionTools.RequireRuntimeTarget();
            Animator animator = target.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("Vacuum_Use runtime animation is not configured.");
            if (!animator.enabled || animator.applyRootMotion)
                throw new InvalidOperationException("Vacuum_Use runtime Animator settings changed.");
            SessionState.SetInt(PreviousPhaseKey, DetectPhase(animator));
            SessionState.SetFloat(
                BoundaryWaitStartKey,
                (float)Time.realtimeSinceStartupAsDouble);
        }

        private static void WaitForNaturalForwardBoundary()
        {
            Animator animator = RequireRuntimeAnimator();
            int currentPhase = DetectPhase(animator);
            float waitStarted = SessionState.GetFloat(BoundaryWaitStartKey, 0f);
            if (Time.realtimeSinceStartupAsDouble - waitStarted > 8d)
                throw new InvalidOperationException(
                    "Vacuum Animator did not reach a natural diagonal-to-forward boundary within eight seconds.");
            int previousPhase = SessionState.GetInt(PreviousPhaseKey, currentPhase);
            if (previousPhase == 4 && currentPhase == 0)
            {
                SessionState.SetFloat(StartTimeKey, (float)Time.realtimeSinceStartupAsDouble);
                SessionState.SetInt(IndexKey, 0);
                ResetMetrics();
                SessionState.SetInt(StateKey, Capturing);
                return;
            }
            if (currentPhase >= 0) SessionState.SetInt(PreviousPhaseKey, currentPhase);
        }

        private static void CaptureWhenDue()
        {
            int index = SessionState.GetInt(IndexKey, 0);
            float start = SessionState.GetFloat(StartTimeKey, 0f);
            double elapsed = Time.realtimeSinceStartupAsDouble - start;
            AccumulateMetrics();
            if (index < CaptureTimes.Length)
            {
                if (elapsed < CaptureTimes[index]) return;
                Animator animator = RequireRuntimeAnimator();
                int expectedPhase = index % VacuumUseLocomotionCycleBehaviour.MotionCount;
                Vector2 expectedMove = ExpectedMove(expectedPhase);
                if (Mathf.Abs(animator.GetFloat("MoveX") - expectedMove.x) > 0.001f ||
                    Mathf.Abs(animator.GetFloat("MoveY") - expectedMove.y) > 0.001f)
                    throw new InvalidOperationException(
                        "Natural sequence parameter mismatch. index=" + index +
                        ", expectedPhase=" + expectedPhase + ".");
                Panels.Add(VacuumUseLocomotionTools.CaptureRuntimePanel());
                Observations.Add(
                    "panel=" + index +
                    ",elapsed=" + Num((float)elapsed) +
                    ",phase=" + expectedPhase +
                    ",completedSequences=" + (index / 5) +
                    ",move=" + Num(animator.GetFloat("MoveX")) + "," +
                    Num(animator.GetFloat("MoveY")));
                SessionState.SetInt(IndexKey, index + 1);
                return;
            }

            if (elapsed < 10.1d) return;
            Animator boundaryAnimator = RequireRuntimeAnimator();
            Vector2 boundaryMove = ExpectedMove(0);
            int completedSequenceCount = Mathf.FloorToInt(
                (float)elapsed /
                (VacuumUseLocomotionCycleBehaviour.MotionCount *
                 VacuumUseLocomotionCycleBehaviour.SecondsPerMotion));
            if (completedSequenceCount < 2 ||
                Mathf.Abs(boundaryAnimator.GetFloat("MoveX") - boundaryMove.x) > 0.001f ||
                Mathf.Abs(boundaryAnimator.GetFloat("MoveY") - boundaryMove.y) > 0.001f)
                throw new InvalidOperationException(
                    "Diagonal did not return to forward after two complete natural cycles.");
            RequireMetricAtMost(maximumUpperPosition, 0.00001f,
                "upper-body local-position error");
            RequireMetricAtMost(maximumUpperRotation, 0.05f,
                "upper-body local-rotation error");
            RequireMetricAtMost(maximumUpperScale, 0.00001f,
                "upper-body local-scale error");
            RequireMetricAtMost(maximumVacuumPosition, 0.000001f,
                "vacuum local-position error");
            RequireMetricAtMost(maximumVacuumRotation, 0.0001f,
                "vacuum local-rotation error");
            RequireMetricAtMost(maximumVacuumScale, 0.000001f,
                "vacuum local-scale error");

            bool final = SessionState.GetBool(FinalKey, false);
            string destination = final
                ? VacuumUseLocomotionTools.FinalAbsolutePath
                : VacuumUseLocomotionTools.DiagnosticAbsolutePath;
            VacuumUseLocomotionTools.ComposeRuntimeReview(Panels, destination);
            var report = new StringBuilder()
                .AppendLine("Vacuum_Use natural Play Mode locomotion review")
                .AppendLine("captureKind=" + (final ? "Final" : "Diagnostic"))
                .AppendLine("naturalPlayback=True")
                .AppendLine("forcedAnimatorTime=False")
                .AppendLine("animatorPlayCalled=False")
                .AppendLine("animatorRebindCalled=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("capturedPanelCount=" + Panels.Count)
                .AppendLine("observedTwoCycles=True")
                .AppendLine("diagonalToForwardBoundary=True")
                .AppendLine("completedSequenceCount=" + completedSequenceCount)
                .AppendLine("boundaryPhase=0")
                .AppendLine("maximumUpperBodyPositionError=" + Num(maximumUpperPosition))
                .AppendLine("maximumUpperBodyRotationErrorDegrees=" + Num(maximumUpperRotation))
                .AppendLine("maximumUpperBodyScaleError=" + Num(maximumUpperScale))
                .AppendLine("maximumVacuumPositionError=" + Num(maximumVacuumPosition))
                .AppendLine("maximumVacuumRotationErrorDegrees=" + Num(maximumVacuumRotation))
                .AppendLine("maximumVacuumScaleError=" + Num(maximumVacuumScale));
            foreach (string observation in Observations) report.AppendLine(observation);
            VacuumUseLocomotionTools.WriteRuntimeReport(
                final ? "final_runtime.txt" : "diagnostic_runtime.txt",
                report.ToString());
            CleanupPanels();
            SessionState.SetInt(StateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void RequireMetricAtMost(float actual, float limit, string label)
        {
            if (float.IsNaN(actual) || float.IsInfinity(actual) || actual > limit)
                throw new InvalidOperationException(
                    "Vacuum natural playback " + label + " exceeded limit. actual=" +
                    Num(actual) + ", limit=" + Num(limit) + ".");
        }

        private static Animator RequireRuntimeAnimator()
        {
            return VacuumUseLocomotionTools.RequireRuntimeTarget().GetComponent<Animator>() ??
                throw new InvalidOperationException("Vacuum_Use Animator disappeared in Play Mode.");
        }

        private static Vector2 ExpectedMove(int phase)
        {
            switch (phase)
            {
                case 0: return new Vector2(0f, 1f);
                case 1: return new Vector2(0f, -1f);
                case 2: return new Vector2(1f, 0f);
                case 3: return new Vector2(0f, 2f);
                default: return new Vector2(0.70710677f, 0.70710677f);
            }
        }

        private static int DetectPhase(Animator animator)
        {
            Vector2 actual = new Vector2(
                animator.GetFloat("MoveX"),
                animator.GetFloat("MoveY"));
            int closest = 0;
            float closestDistance = float.PositiveInfinity;
            for (int phase = 0; phase < VacuumUseLocomotionCycleBehaviour.MotionCount; phase++)
            {
                float distance = Vector2.SqrMagnitude(actual - ExpectedMove(phase));
                if (distance < closestDistance)
                {
                    closest = phase;
                    closestDistance = distance;
                }
            }
            return closestDistance > 0.0001f ? -1 : closest;
        }

        private static void AccumulateMetrics()
        {
            VacuumUseLocomotionTools.RuntimePoseMetrics metrics =
                VacuumUseLocomotionTools.MeasureRuntimePose();
            maximumUpperPosition = Mathf.Max(maximumUpperPosition, metrics.UpperPosition);
            maximumUpperRotation = Mathf.Max(maximumUpperRotation, metrics.UpperRotation);
            maximumUpperScale = Mathf.Max(maximumUpperScale, metrics.UpperScale);
            maximumVacuumPosition = Mathf.Max(maximumVacuumPosition, metrics.VacuumPosition);
            maximumVacuumRotation = Mathf.Max(maximumVacuumRotation, metrics.VacuumRotation);
            maximumVacuumScale = Mathf.Max(maximumVacuumScale, metrics.VacuumScale);
        }

        private static void ResetMetrics()
        {
            maximumUpperPosition = 0f;
            maximumUpperRotation = 0f;
            maximumUpperScale = 0f;
            maximumVacuumPosition = 0f;
            maximumVacuumRotation = 0f;
            maximumVacuumScale = 0f;
        }

        private static void FinishSuccess()
        {
            bool final = SessionState.GetBool(FinalKey, false);
            Action<string> callback = complete;
            CleanupSession();
            callback?.Invoke(
                "Vacuum_Use " + (final ? "final" : "diagnostic") +
                " natural two-cycle review captured in Edit Mode-restored state.");
        }

        private static void FinishFailure()
        {
            string error = SessionState.GetString(FailureKey,
                "Vacuum_Use natural Play Mode capture failed.");
            Action<Exception> callback = fail;
            CleanupSession();
            callback?.Invoke(new InvalidOperationException(error));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
        }

        private static void CleanupSession()
        {
            CleanupPanels();
            Observations.Clear();
            ResetMetrics();
            complete = null;
            fail = null;
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(IndexKey);
            SessionState.EraseFloat(StartTimeKey);
            SessionState.EraseBool(FinalKey);
            SessionState.EraseString(FailureKey);
            SessionState.EraseInt(PreviousPhaseKey);
            SessionState.EraseFloat(BoundaryWaitStartKey);
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
