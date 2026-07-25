using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RebellionCargoRunScene
{
    [InitializeOnLoad]
    internal static class RebellionMovePlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Rebellion Enemy Placement";
        private const string MoveSlotName = "Rebellion_01_Move";
        private const string SessionStateKey =
            "Bellerophon.RebellionMovePlayModeCapture.State";
        private const string SessionIndexKey =
            "Bellerophon.RebellionMovePlayModeCapture.Index";
        private const string SessionStartTimeKey =
            "Bellerophon.RebellionMovePlayModeCapture.StartTime";
        private const string SessionFailureKey =
            "Bellerophon.RebellionMovePlayModeCapture.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int FailedWaitingForEditMode = 4;

        private static readonly double[] CaptureTimes =
        {
            0.05d,
            0.25d,
            0.5d,
            0.75d,
            0.99d
        };

        private static Action<string> complete;
        private static Action<Exception> fail;

        static RebellionMovePlayModeCapture()
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
                    "Cannot start the Rebellion move visual review while Unity " +
                    "is entering Play Mode.");
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
                    "CargoRunMvp must be clean before the Rebellion move visual review.");
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
                            "Unity left Play Mode before the Rebellion move visual " +
                            "review finished.");
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
                    "Rebellion_01_Move runtime Animator is not configured.");
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(
                RebellionMoveAnimationTool.AnimatorStateName,
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

            RebellionMoveAnimationTool.CaptureRuntimeFrame(PanelPath(index));
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

            RebellionMoveAnimationTool.ComposeRuntimeReview(
                panelPaths,
                RebellionMoveAnimationTool.FinalReviewAbsolutePath);
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

                return root.transform.Find(MoveSlotName) ??
                       throw new InvalidOperationException(
                           MoveSlotName + " is missing in Play Mode.");
            }

            throw new InvalidOperationException(
                PlacementRootName + " is missing in Play Mode.");
        }

        private static void FinishSuccess()
        {
            var callback = complete;
            CleanupSession();
            callback?.Invoke(
                "Rebellion actual Animator move frames captured at 0.05, 0.25, " +
                "0.5, 0.75, and 0.99 seconds.");
        }

        private static void FinishFailure()
        {
            var error = SessionState.GetString(
                SessionFailureKey,
                "Rebellion move Play Mode capture failed.");
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
            TryDelete(RebellionMoveAnimationTool.FinalReviewAbsolutePath);
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
                "Rebellion_Move_PlayMode_" + index + ".png");
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
