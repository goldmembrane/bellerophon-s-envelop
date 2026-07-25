using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RebellionCargoRunScene
{
    [InitializeOnLoad]
    internal static class RebellionForwardBurstFirePlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Rebellion Enemy Placement";
        private const string SlotName = "Rebellion_04_Forward_Burst_Fire";
        private const string SessionStateKey =
            "Bellerophon.RebellionForwardBurstFireCapture.State";
        private const string SessionIndexKey =
            "Bellerophon.RebellionForwardBurstFireCapture.Index";
        private const string SessionStartTimeKey =
            "Bellerophon.RebellionForwardBurstFireCapture.StartTime";
        private const string SessionFailureKey =
            "Bellerophon.RebellionForwardBurstFireCapture.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int FailedWaitingForEditMode = 4;

        private static readonly double[] CaptureTimes =
        {
            0.02d, 0.10d, 0.22d, 0.30d, 0.42d,
            1.02d, 1.10d, 2.02d, 2.10d, 3.02d,
            3.10d, 4.02d, 4.10d, 4.82d, 4.94d
        };

        private static Action<string> complete;
        private static Action<Exception> fail;

        static RebellionForwardBurstFirePlayModeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static bool HasPendingCapture =>
            SessionState.GetInt(SessionStateKey, 0) != 0;

        public static void Start(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            complete = completeCallback;
            fail = failCallback;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Cannot start the Rebellion forward burst review while " +
                    "Unity is entering Play Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Current active scene must be CargoRunMvp.");
            }
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the Rebellion forward " +
                    "burst review.");
            }

            DeleteCaptureFiles();
            SessionState.SetString(SessionFailureKey, string.Empty);
            SessionState.SetInt(SessionIndexKey, 0);
            SessionState.SetInt(SessionStateKey, WaitingForPlayMode);
            EditorApplication.EnterPlaymode();
        }

        public static void Resume(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            var state = SessionState.GetInt(SessionStateKey, 0);
            if ((state == WaitingForPlayMode || state == Capturing) &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                CleanupSession();
                Start(completeCallback, failCallback);
                return;
            }

            complete = completeCallback;
            fail = failCallback;
            Tick();
        }

        private static void Tick()
        {
            if (complete == null && fail == null)
            {
                return;
            }

            var state = SessionState.GetInt(SessionStateKey, 0);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        return;
                    }
                    BeginRuntimePlayback();
                    SessionState.SetInt(SessionStateKey, Capturing);
                    return;
                }

                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Unity left Play Mode before the Rebellion " +
                            "forward burst review finished.");
                    }
                    CaptureRuntimeFramesWhenDue();
                    return;
                }

                if (state == WaitingForEditMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        return;
                    }
                    FinishSuccess();
                    return;
                }

                if (state == FailedWaitingForEditMode &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    FinishFailure();
                }
            }
            catch (Exception exception)
            {
                SessionState.SetString(SessionFailureKey, exception.ToString());
                SessionState.SetInt(
                    SessionStateKey,
                    FailedWaitingForEditMode);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }
                else if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    FinishFailure();
                }
            }
        }

        private static void BeginRuntimePlayback()
        {
            var animator = RequireAnimator();
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(
                RebellionForwardBurstFireTool.AnimatorStateName,
                0,
                0f);
            animator.Update(0f);
            SessionState.SetFloat(
                SessionStartTimeKey,
                (float)Time.realtimeSinceStartupAsDouble);
        }

        private static void CaptureRuntimeFramesWhenDue()
        {
            var index = SessionState.GetInt(SessionIndexKey, 0);
            if (index >= CaptureTimes.Length)
            {
                return;
            }

            var startTime = SessionState.GetFloat(SessionStartTimeKey, 0f);
            var elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            if (elapsed < CaptureTimes[index])
            {
                return;
            }

            var animator = RequireAnimator();
            var normalizedTime =
                (float)(CaptureTimes[index] /
                        RebellionForwardBurstFireTool.LoopSeconds);
            animator.Play(
                RebellionForwardBurstFireTool.AnimatorStateName,
                0,
                normalizedTime);
            animator.Update(0f);
            RebellionForwardBurstFireTool.CaptureRuntimeFrame(
                PanelPath(index));
            index++;
            SessionState.SetInt(SessionIndexKey, index);
            if (index < CaptureTimes.Length)
            {
                return;
            }

            var panels = new string[CaptureTimes.Length];
            for (var panelIndex = 0;
                 panelIndex < CaptureTimes.Length;
                 panelIndex++)
            {
                panels[panelIndex] = PanelPath(panelIndex);
            }
            RebellionForwardBurstFireTool.ComposeRuntimeReview(
                panels,
                RebellionForwardBurstFireTool.FinalReviewAbsolutePath);
            foreach (var panel in panels)
            {
                TryDelete(panel);
            }

            SessionState.SetInt(SessionStateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static Animator RequireAnimator()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Play Mode active scene must stay CargoRunMvp.");
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != PlacementRootName)
                {
                    continue;
                }
                var slot = root.transform.Find(SlotName) ??
                           throw new InvalidOperationException(
                               SlotName + " is missing in Play Mode.");
                var animator = slot.GetComponent<Animator>();
                if (animator == null ||
                    animator.runtimeAnimatorController == null)
                {
                    throw new InvalidOperationException(
                        SlotName + " runtime Animator is not configured.");
                }
                return animator;
            }

            throw new InvalidOperationException(
                PlacementRootName + " is missing in Play Mode.");
        }

        private static void FinishSuccess()
        {
            var callback = complete;
            CleanupSession();
            callback?.Invoke(
                "Rebellion actual Animator forward burst frames captured " +
                "across the complete 5-second loop.");
        }

        private static void FinishFailure()
        {
            var error = SessionState.GetString(
                SessionFailureKey,
                "Rebellion forward burst Play Mode capture failed.");
            var callback = fail;
            CleanupSession();
            callback?.Invoke(new InvalidOperationException(error));
        }

        private static void CleanupSession()
        {
            complete = null;
            fail = null;
            SessionState.EraseInt(SessionStateKey);
            SessionState.EraseInt(SessionIndexKey);
            SessionState.EraseFloat(SessionStartTimeKey);
            SessionState.EraseString(SessionFailureKey);
        }

        private static void DeleteCaptureFiles()
        {
            TryDelete(RebellionForwardBurstFireTool.FinalReviewAbsolutePath);
            for (var index = 0; index < CaptureTimes.Length; index++)
            {
                TryDelete(PanelPath(index));
            }
        }

        private static string PanelPath(int index)
        {
            return Path.Combine(
                ProjectRoot,
                "Logs",
                "Rebellion_ForwardBurst_PlayMode_" + index + ".png");
        }

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ??
            throw new InvalidOperationException("Project root is unavailable.");

        private static void TryDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
