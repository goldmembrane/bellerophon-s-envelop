using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.NegatifCargoRunScene
{
    [InitializeOnLoad]
    internal static class NegatifFleePlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Negatif Enemy Placement";
        private const string FleeSlotName = "Negatif_05_Flee";
        private const string AnimatorStateName = "Flee";
        private const double ClipDurationSeconds = 6d;
        private const string SessionStateKey =
            "Bellerophon.NegatifFleePlayModeCapture.State";
        private const string SessionIndexKey =
            "Bellerophon.NegatifFleePlayModeCapture.Index";
        private const string SessionStartTimeKey =
            "Bellerophon.NegatifFleePlayModeCapture.StartTime";
        private const string SessionFailureKey =
            "Bellerophon.NegatifFleePlayModeCapture.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int FailedWaitingForEditMode = 4;

        private static readonly double[] CaptureTimes =
        {
            0d,
            0.1d,
            0.2d,
            0.3d,
            0.4d,
            0.75d,
            1.5d,
            2.25d,
            3d,
            3.75d,
            4.5d,
            5.25d,
            6d
        };

        private static Action<string> complete;
        private static Action<Exception> fail;

        static NegatifFleePlayModeCapture()
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
                    "Cannot start the Negatif flee review while Unity is entering Play Mode.");
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
                    "CargoRunMvp must be clean before the Negatif flee review.");
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
                            "Unity left Play Mode before the Negatif flee review finished.");
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
                SessionState.SetInt(SessionStateKey, FailedWaitingForEditMode);
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
            var slot = RequireSlot();
            var animator = slot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(
                    "Negatif_05_Flee runtime Animator is not configured.");
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(AnimatorStateName, 0, 0f);
            animator.Update(0f);
            SessionState.SetFloat(
                SessionStartTimeKey,
                (float)Time.realtimeSinceStartupAsDouble);
        }

        private static void CaptureRuntimeFramesWhenDue()
        {
            var startTime = SessionState.GetFloat(SessionStartTimeKey, 0f);
            if (Time.realtimeSinceStartupAsDouble - startTime < 0.2d)
            {
                return;
            }

            var index = SessionState.GetInt(SessionIndexKey, 0);
            if (index >= CaptureTimes.Length)
            {
                return;
            }

            var animator = RequireSlot().GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(
                    "Negatif_05_Flee runtime Animator is missing.");
            }

            animator.speed = 0f;
            animator.Play(
                AnimatorStateName,
                0,
                (float)(CaptureTimes[index] / ClipDurationSeconds));
            animator.Update(0f);

            NegatifFleeAnimationTool.CaptureRuntimeFrame(PanelPath(index));
            index++;
            SessionState.SetInt(SessionIndexKey, index);
            if (index < CaptureTimes.Length)
            {
                return;
            }

            var panelPaths = new string[CaptureTimes.Length];
            for (var panelIndex = 0; panelIndex < CaptureTimes.Length; panelIndex++)
            {
                panelPaths[panelIndex] = PanelPath(panelIndex);
            }

            NegatifFleeAnimationTool.ComposeRuntimeReview(
                panelPaths,
                FinalReviewPath);
            foreach (var panelPath in panelPaths)
            {
                TryDelete(panelPath);
            }

            SessionState.SetInt(SessionStateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static Transform RequireSlot()
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

                return root.transform.Find(FleeSlotName) ??
                       throw new InvalidOperationException(
                           FleeSlotName + " is missing in Play Mode.");
            }

            throw new InvalidOperationException(
                PlacementRootName + " is missing in Play Mode.");
        }

        private static void FinishSuccess()
        {
            var callback = complete;
            CleanupSession();
            callback?.Invoke(
                "Negatif actual Play Mode flee review captured at 13 exact clip times over the six-second loop.");
        }

        private static void FinishFailure()
        {
            var error = SessionState.GetString(
                SessionFailureKey,
                "Negatif flee Play Mode capture failed.");
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
            TryDelete(FinalReviewPath);
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
                "Negatif_Flee_PlayMode_" + index + ".png");
        }

        private static string FinalReviewPath =>
            Path.Combine(
                ProjectRoot,
                "Logs",
                "Negatif_Flee_VisualReview.png");

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
