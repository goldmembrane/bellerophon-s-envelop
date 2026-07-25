using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.NegatifCargoRunScene
{
    [InitializeOnLoad]
    internal static class NegatifMovePlayModeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string DolorePlacementRootName = "Approved Dolore Enemy Placement";
        private const string DoloreMoveSlotName = "Dolore_03_Move_Quadruped";
        private const string NegatifPlacementRootName = "Approved Negatif Enemy Placement";
        private const string NegatifMoveSlotName = "Negatif_02_Move";
        private const string AnimatorStateName = "MoveQuadruped";
        private const string SessionStateKey =
            "Bellerophon.NegatifMovePlayModeCapture.State";
        private const string SessionIndexKey =
            "Bellerophon.NegatifMovePlayModeCapture.Index";
        private const string SessionStartTimeKey =
            "Bellerophon.NegatifMovePlayModeCapture.StartTime";
        private const string SessionFailureKey =
            "Bellerophon.NegatifMovePlayModeCapture.Failure";
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

        static NegatifMovePlayModeCapture()
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
                    "Cannot start the Negatif move visual review while Unity is entering Play Mode.");
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
                    "CargoRunMvp must be clean before the Negatif move visual review.");
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
                            "Unity left Play Mode before the Negatif move visual review finished.");
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
            var negatifSlot = RequireSlot(
                NegatifPlacementRootName,
                NegatifMoveSlotName);
            var animator = negatifSlot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(
                    "Negatif_02_Move runtime Animator is not configured.");
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(AnimatorStateName, 0, 0f);
            animator.Update(0f);

            var doloreSlot = RequireSlot(
                DolorePlacementRootName,
                DoloreMoveSlotName);
            var doloreAnimation =
                doloreSlot.GetComponentInChildren<Animation>(true) ??
                throw new InvalidOperationException(
                    "Dolore_03_Move_Quadruped has no runtime Animation component.");
            var doloreClip = doloreAnimation.clip;
            if (doloreClip == null)
            {
                foreach (AnimationState state in doloreAnimation)
                {
                    doloreClip = state.clip;
                    break;
                }
            }

            if (doloreClip == null)
            {
                throw new InvalidOperationException(
                    "Dolore_03_Move_Quadruped has no runtime clip.");
            }

            doloreAnimation.enabled = true;
            doloreAnimation.wrapMode = WrapMode.Loop;
            doloreAnimation.Play(doloreClip.name);
            var doloreState = doloreAnimation[doloreClip.name];
            if (doloreState != null)
            {
                doloreState.time = 0f;
                doloreState.speed = 1f;
                doloreAnimation.Sample();
            }

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

            NegatifMoveAnimationTool.CaptureRuntimeFrame(
                true,
                DolorePanelPath(index));
            NegatifMoveAnimationTool.CaptureRuntimeFrame(
                false,
                NegatifPanelPath(index));
            index++;
            SessionState.SetInt(SessionIndexKey, index);
            if (index < CaptureTimes.Length)
            {
                return;
            }

            var panelPaths = new string[CaptureTimes.Length * 2];
            for (var panelIndex = 0; panelIndex < CaptureTimes.Length; panelIndex++)
            {
                panelPaths[panelIndex] = DolorePanelPath(panelIndex);
                panelPaths[CaptureTimes.Length + panelIndex] =
                    NegatifPanelPath(panelIndex);
            }

            NegatifMoveAnimationTool.ComposeRuntimeReview(
                panelPaths,
                FinalReviewPath);
            foreach (var panelPath in panelPaths)
            {
                TryDelete(panelPath);
            }

            SessionState.SetInt(SessionStateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static Transform RequireSlot(
            string placementRootName,
            string slotName)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Play Mode active scene must stay CargoRunMvp.");
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != placementRootName)
                {
                    continue;
                }

                return root.transform.Find(slotName) ??
                       throw new InvalidOperationException(
                           slotName + " is missing in Play Mode.");
            }

            throw new InvalidOperationException(
                placementRootName + " is missing in Play Mode.");
        }

        private static void FinishSuccess()
        {
            var callback = complete;
            CleanupSession();
            callback?.Invoke(
                "Dolore and Negatif actual Play Mode move frames captured at 0.05, 0.25, 0.5, 0.75, and 0.99 seconds.");
        }

        private static void FinishFailure()
        {
            var error = SessionState.GetString(
                SessionFailureKey,
                "Negatif move Play Mode capture failed.");
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
                TryDelete(DolorePanelPath(index));
                TryDelete(NegatifPanelPath(index));
            }
        }

        private static string DolorePanelPath(int index)
        {
            return Path.Combine(
                ProjectRoot,
                "Logs",
                "Negatif_Move_PlayMode_Dolore_" + index + ".png");
        }

        private static string NegatifPanelPath(int index)
        {
            return Path.Combine(
                ProjectRoot,
                "Logs",
                "Negatif_Move_PlayMode_Negatif_" + index + ".png");
        }

        private static string FinalReviewPath =>
            Path.Combine(
                ProjectRoot,
                "Logs",
                "Negatif_Move_VisualReview.png");

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
