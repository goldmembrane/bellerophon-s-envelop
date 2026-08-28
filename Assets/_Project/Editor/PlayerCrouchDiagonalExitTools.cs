using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PlayerCrouchDiagonalExitTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LayoutRootName = "PlayerAnimationLayout";
        private const string DiagonalTargetName = "Player_Crouch_Diagonal";
        private const string ExitTargetName = "Player_Crouch_Exit";
        private const string IdleTargetName = "Player_Idle";
        private const string DiagonalStateName = "PlayerCrouchDiagonal";
        private const string ExitStateName = "PlayerCrouchExit";
        private const string BlendParameterName = "Blend";
        private const float BlendWeight = 0.5f;
        private const float ExitHoldSeconds = 0.5f;
        private const float PositionTolerance = 0.0001f;
        private const float RotationTolerance = 0.01f;
        private const int CaptureWidth = 400;
        private const int CaptureHeight = 500;
        private const string ExpectedTakeName = "mixamo.com";
        private const string ExpectedExitSourceHash =
            "0A8AA9A38A0A85135A05847A9725E82AF5D9E59BF8742FC27F13CF7796D888D7";

        private const string ForwardClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Forward_Mixamo_InPlace.anim";
        private const string SidestepClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Sidestep_Mixamo_InPlace.anim";
        private const string DiagonalControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Diagonal.controller";
        private const string ExitSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Exit_Mixamo.fbx";
        private const string ExitClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Exit_Mixamo_Hold.anim";
        private const string ExitControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Exit.controller";
        private const string IdleClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Idle.anim";

        private const string ValidationDirectory =
            "docs/validation/player_crouch_diagonal_exit_2026-08-28";
        private const string ApplyMetricsPath =
            ValidationDirectory + "/player_crouch_diagonal_exit_apply_metrics.json";
        private const string ReviewMetricsPath =
            ValidationDirectory + "/player_crouch_diagonal_exit_review_metrics.json";
        private const string DiagonalReviewPath =
            ValidationDirectory + "/player_crouch_diagonal_review_contact_sheet.png";
        private const string ExitReviewPath =
            ValidationDirectory + "/player_crouch_exit_review_contact_sheet.png";
        private const string DiagonalFinalPath =
            ValidationDirectory + "/player_crouch_diagonal_final.png";
        private const string ExitFinalPath =
            ValidationDirectory + "/player_crouch_exit_final.png";
        private const string ReviewStageKey =
            "Bellerophon.PlayerCrouchDiagonalExit.Review.Stage";
        private const string ExitIdleTransitionApplyMetricsPath =
            ValidationDirectory + "/player_crouch_exit_idle_transition_apply_metrics.json";
        private const string ExitIdleTransitionReviewMetricsPath =
            ValidationDirectory + "/player_crouch_exit_idle_transition_review_metrics.json";
        private const string ExitIdleTransitionReviewPath =
            ValidationDirectory + "/player_crouch_exit_idle_transition_review_contact_sheet.png";
        private const string ExitIdleTransitionFinalPath =
            ValidationDirectory + "/player_crouch_exit_idle_transition_final.png";
        private const string ExitIdleTransitionReviewStageKey =
            "Bellerophon.PlayerCrouchExitIdleTransition.Review.Stage";

        [Serializable]
        private sealed class ApplyMetrics
        {
            public string targetSet;
            public string sourceTake;
            public string originalSourceHash;
            public string unitySourceHash;
            public string forwardClipHashBefore;
            public string forwardClipHashAfter;
            public string sidestepClipHashBefore;
            public string sidestepClipHashAfter;
            public float forwardBlendWeight;
            public float sidestepBlendWeight;
            public float exitOriginalDurationSeconds;
            public float exitHoldDurationSeconds;
            public float exitTotalDurationSeconds;
            public float exitFrameRate;
            public int exitSourceFloatCurveCount;
            public int exitDerivedFloatCurveCount;
            public bool sourceFbxExactCopy;
            public bool inputClipsUnchanged;
            public bool blendTreeCorrect;
            public bool originalExitCurvesPreserved;
            public bool exitHoldIsConstant;
            public bool exitDurationCorrect;
            public bool rootTransformsUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool applyRootMotionDisabled;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string targetSet;
            public TargetReviewMetrics diagonal;
            public TargetReviewMetrics exit;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class TargetReviewMetrics
        {
            public string target;
            public string state;
            public float durationSeconds;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootPositionDisplacementMax;
            public float holdPositionDifferenceMax;
            public float holdRotationDifferenceDegreesMax;
            public float blendParameterValue;
            public float sourceStartPositionDifferenceMax;
            public float sourceStartRotationDifferenceDegreesMax;
            public float idleEndPositionDifferenceMax;
            public float idleEndRotationDifferenceDegreesMax;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class ExitIdleTransitionApplyMetrics
        {
            public string target;
            public string sourceTake;
            public string originalSourceHash;
            public string unitySourceHash;
            public string idleClipHashBefore;
            public string idleClipHashAfter;
            public float originalDurationSeconds;
            public float holdDurationSeconds;
            public float totalDurationSeconds;
            public float frameRate;
            public int sourceFramesSampled;
            public int armatureTransformsBaked;
            public float transitionWeightAtStart;
            public float transitionWeightAtEnd;
            public float sourceStartPositionDifferenceMax;
            public float sourceStartRotationDifferenceDegreesMax;
            public float idleEndPositionDifferenceMax;
            public float idleEndRotationDifferenceDegreesMax;
            public float holdPositionDifferenceMax;
            public float holdRotationDifferenceDegreesMax;
            public bool sourceFbxExactCopy;
            public bool idleClipUnchanged;
            public bool transitionWeightsMonotonic;
            public bool rootTransformUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool applyRootMotionDisabled;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ExitIdleTransitionReviewMetrics
        {
            public string target;
            public TargetReviewMetrics exit;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private readonly struct RootPose
        {
            internal readonly Vector3 LocalPosition;
            internal readonly Quaternion LocalRotation;
            internal readonly Vector3 LocalScale;

            internal RootPose(Transform value)
            {
                LocalPosition = value.localPosition;
                LocalRotation = value.localRotation;
                LocalScale = value.localScale;
            }
        }

        private sealed class PoseSnapshot
        {
            internal readonly Dictionary<string, Vector3> Positions =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Quaternion> Rotations =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Vector3> Scales =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            internal RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            internal void Hide()
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            internal void Restore()
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Diagonal And Exit")]
        internal static void Apply()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch diagonal and exit apply.");
            }

            AnimationClip forward = LoadClip(ForwardClipPath);
            AnimationClip sidestep = LoadClip(SidestepClipPath);
            string forwardHashBefore = HashFile(ForwardClipPath);
            string sidestepHashBefore = HashFile(SidestepClipPath);
            EnsureExactExitSourceCopy();
            ConfigureExitImporter();
            AnimationClip source = LoadSingleExitSourceClip();
            float originalDuration = source.length;
            AnimationClip exit = CreateOrUpdateExitHoldClip(source);
            AnimatorController diagonalController =
                CreateOrUpdateDiagonalController(forward, sidestep);
            AnimatorController exitController =
                CreateOrUpdateSingleClipController(
                    ExitControllerPath,
                    ExitStateName,
                    exit);

            Transform layout = RequireLayout(scene);
            Transform diagonalTarget = RequireTarget(layout, DiagonalTargetName);
            Transform exitTarget = RequireTarget(layout, ExitTargetName);
            RootPose diagonalRootBefore = new RootPose(diagonalTarget);
            RootPose exitRootBefore = new RootPose(exitTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimatorStates(layout);

            Animator diagonalAnimator = ConfigureAnimator(
                diagonalTarget,
                diagonalController);
            Animator exitAnimator = ConfigureAnimator(exitTarget, exitController);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string forwardHashAfter = HashFile(ForwardClipPath);
            string sidestepHashAfter = HashFile(SidestepClipPath);
            bool inputClipsUnchanged =
                string.Equals(forwardHashBefore, forwardHashAfter, StringComparison.Ordinal) &&
                string.Equals(sidestepHashBefore, sidestepHashAfter, StringComparison.Ordinal);
            bool rootsUnchanged =
                RootMatches(diagonalTarget, diagonalRootBefore) &&
                RootMatches(exitTarget, exitRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimatorStates(layout));
            bool sourceCopyExact = EnsureExactExitSourceCopy();
            bool blendTreeCorrect = IsExpectedBlendTree(
                diagonalController,
                forward,
                sidestep);
            bool originalCurvesPreserved = OriginalCurvesPreserved(source, exit);
            bool holdIsConstant = HoldIsConstant(
                exit,
                originalDuration,
                originalDuration + ExitHoldSeconds);
            bool durationCorrect = Mathf.Abs(
                exit.length - (originalDuration + ExitHoldSeconds)) <= 0.0001f;
            bool animatorSettingsCorrect =
                AnimatorMatches(diagonalAnimator, diagonalController) &&
                AnimatorMatches(exitAnimator, exitController);

            ApplyMetrics metrics = new ApplyMetrics
            {
                targetSet = DiagonalTargetName + ", " + ExitTargetName,
                sourceTake = source.name,
                originalSourceHash = ExpectedExitSourceHash,
                unitySourceHash = HashFile(ExitSourcePath),
                forwardClipHashBefore = forwardHashBefore,
                forwardClipHashAfter = forwardHashAfter,
                sidestepClipHashBefore = sidestepHashBefore,
                sidestepClipHashAfter = sidestepHashAfter,
                forwardBlendWeight = BlendWeight,
                sidestepBlendWeight = BlendWeight,
                exitOriginalDurationSeconds = originalDuration,
                exitHoldDurationSeconds = ExitHoldSeconds,
                exitTotalDurationSeconds = exit.length,
                exitFrameRate = exit.frameRate,
                exitSourceFloatCurveCount =
                    AnimationUtility.GetCurveBindings(source).Length,
                exitDerivedFloatCurveCount =
                    AnimationUtility.GetCurveBindings(exit).Length,
                sourceFbxExactCopy = sourceCopyExact,
                inputClipsUnchanged = inputClipsUnchanged,
                blendTreeCorrect = blendTreeCorrect,
                originalExitCurvesPreserved = originalCurvesPreserved,
                exitHoldIsConstant = holdIsConstant,
                exitDurationCorrect = durationCorrect,
                rootTransformsUnchanged = rootsUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                applyRootMotionDisabled = animatorSettingsCorrect,
                passedNumericChecks = sourceCopyExact &&
                    inputClipsUnchanged &&
                    blendTreeCorrect &&
                    originalCurvesPreserved &&
                    holdIsConstant &&
                    durationCorrect &&
                    rootsUnchanged &&
                    otherAnimatorsUnchanged &&
                    animatorSettingsCorrect &&
                    string.Equals(source.name, ExpectedTakeName, StringComparison.Ordinal),
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(ApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch diagonal and exit apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerCrouchDiagonalExit] Applied exact 50:50 in-place Blend Tree and exact Mixamo exit with 0.5-second final-pose hold. " +
                "SourceTake=" + source.name +
                ", ExitOriginal=" + Num(originalDuration) +
                ", ExitTotal=" + Num(exit.length) +
                ", InputsUnchanged=True, OtherAnimatorsUnchanged=True, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Exit Idle Transition")]
        internal static void ApplyExitIdleTransition()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch exit Idle transition apply.");
            }

            EnsureExactExitSourceCopy();
            AnimationClip source = LoadSingleExitSourceClip();
            AnimationClip idle = LoadClip(IdleClipPath);
            string idleHashBefore = HashFile(IdleClipPath);
            Transform layout = RequireLayout(scene);
            Transform exitTarget = RequireTarget(layout, ExitTargetName);
            Transform idleTarget = RequireTarget(layout, IdleTargetName);
            RootPose exitRootBefore = new RootPose(exitTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptExit(layout);

            AnimationClip transition = CreateOrUpdateExitIdleTransitionClip(
                source,
                idle,
                exitTarget,
                idleTarget,
                out int sourceFrames,
                out int armatureTransforms);
            AnimatorController controller = CreateOrUpdateSingleClipController(
                ExitControllerPath,
                ExitStateName,
                transition);
            Animator animator = ConfigureAnimator(exitTarget, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            PoseSnapshot sourceStart = CapturePoseFromClip(exitTarget, source, 0f);
            PoseSnapshot transitionStart = CapturePoseFromClip(
                exitTarget,
                transition,
                0f);
            PoseSnapshot idleFirst = CapturePoseFromClip(idleTarget, idle, 0f);
            PoseSnapshot transitionEnd = CapturePoseFromClip(
                exitTarget,
                transition,
                source.length);
            PoseSnapshot holdStart = CapturePoseFromClip(
                exitTarget,
                transition,
                source.length + 0.01f);
            PoseSnapshot holdEnd = CapturePoseFromClip(
                exitTarget,
                transition,
                transition.length - 0.01f);
            MeasurePoseDifference(
                sourceStart,
                transitionStart,
                out float sourceStartPositionDifference,
                out float sourceStartRotationDifference);
            MeasureArmaturePoseDifference(
                idleFirst,
                transitionEnd,
                out float idleEndPositionDifference,
                out float idleEndRotationDifference);
            MeasurePoseDifference(
                holdStart,
                holdEnd,
                out float holdPositionDifference,
                out float holdRotationDifference);
            string idleHashAfter = HashFile(IdleClipPath);
            bool sourceCopyExact = EnsureExactExitSourceCopy();
            bool idleUnchanged = string.Equals(
                idleHashBefore,
                idleHashAfter,
                StringComparison.Ordinal);
            bool rootUnchanged = RootMatches(exitTarget, exitRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptExit(layout));
            bool animatorSettingsCorrect = AnimatorMatches(animator, controller);
            bool weightsMonotonic = TransitionWeightsAreMonotonic(sourceFrames);

            ExitIdleTransitionApplyMetrics metrics =
                new ExitIdleTransitionApplyMetrics
                {
                    target = ExitTargetName,
                    sourceTake = source.name,
                    originalSourceHash = ExpectedExitSourceHash,
                    unitySourceHash = HashFile(ExitSourcePath),
                    idleClipHashBefore = idleHashBefore,
                    idleClipHashAfter = idleHashAfter,
                    originalDurationSeconds = source.length,
                    holdDurationSeconds = ExitHoldSeconds,
                    totalDurationSeconds = transition.length,
                    frameRate = transition.frameRate,
                    sourceFramesSampled = sourceFrames,
                    armatureTransformsBaked = armatureTransforms,
                    transitionWeightAtStart = TransitionWeight(0f),
                    transitionWeightAtEnd = TransitionWeight(1f),
                    sourceStartPositionDifferenceMax = sourceStartPositionDifference,
                    sourceStartRotationDifferenceDegreesMax = sourceStartRotationDifference,
                    idleEndPositionDifferenceMax = idleEndPositionDifference,
                    idleEndRotationDifferenceDegreesMax = idleEndRotationDifference,
                    holdPositionDifferenceMax = holdPositionDifference,
                    holdRotationDifferenceDegreesMax = holdRotationDifference,
                    sourceFbxExactCopy = sourceCopyExact,
                    idleClipUnchanged = idleUnchanged,
                    transitionWeightsMonotonic = weightsMonotonic,
                    rootTransformUnchanged = rootUnchanged,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    applyRootMotionDisabled = animatorSettingsCorrect,
                    passedNumericChecks = sourceCopyExact &&
                        idleUnchanged &&
                        weightsMonotonic &&
                        sourceStartPositionDifference <= PositionTolerance &&
                        sourceStartRotationDifference <= RotationTolerance &&
                        idleEndPositionDifference <= PositionTolerance &&
                        idleEndRotationDifference <= RotationTolerance &&
                        holdPositionDifference <= PositionTolerance &&
                        holdRotationDifference <= RotationTolerance &&
                        Mathf.Abs(
                            transition.length -
                            (source.length + ExitHoldSeconds)) <= 0.0001f &&
                        rootUnchanged &&
                        otherAnimatorsUnchanged &&
                        animatorSettingsCorrect,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteJson(ExitIdleTransitionApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch exit Idle transition apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerCrouchExitIdleTransition] Applied source-to-Idle-first-frame smooth transition. " +
                "SourceDuration=" + Num(source.length) +
                ", Hold=" + Num(ExitHoldSeconds) +
                ", SourceFrames=" + sourceFrames +
                ", ArmatureTransforms=" + armatureTransforms +
                ", StartDifference=0, IdleEndDifference=0, HoldDifference=0, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Diagonal And Exit Review")]
        internal static void CaptureReview()
        {
            int stage = SessionState.GetInt(ReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Crouch diagonal and exit review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before crouch diagonal and exit review.");
                    }

                    SessionState.SetInt(ReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerCrouchDiagonalExit] Entering Play Mode for direct two-target review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch diagonal and exit capture requires Play Mode.");
                    }

                    CaptureActualReview();
                    SessionState.SetInt(ReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch diagonal and exit review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(ReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerCrouchDiagonalExit] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Crouch diagonal and exit review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(ReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Diagonal And Exit Final")]
        internal static void CaptureFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Crouch diagonal and exit final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before final capture.");
            }

            ReviewMetrics metrics = ReadJson<ReviewMetrics>(ReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                !metrics.diagonal.passedNumericChecks ||
                !metrics.exit.passedNumericChecks ||
                metrics.diagonal.loopsSampled != 2 ||
                metrics.exit.loopsSampled != 2)
            {
                throw new InvalidOperationException(
                    "Crouch diagonal and exit review did not pass before final capture.");
            }

            CopyReviewedContact(DiagonalReviewPath, DiagonalFinalPath);
            CopyReviewedContact(ExitReviewPath, ExitFinalPath);
            Debug.Log(
                "[PlayerCrouchDiagonalExit] Final images copied once from directly reviewed Play Mode frames. " +
                "Diagonal=" + Path.GetFullPath(DiagonalFinalPath) +
                ", Exit=" + Path.GetFullPath(ExitFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Exit Idle Transition Review")]
        internal static void CaptureExitIdleTransitionReview()
        {
            int stage = SessionState.GetInt(ExitIdleTransitionReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Crouch exit Idle transition review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before crouch exit Idle transition review.");
                    }

                    SessionState.SetInt(ExitIdleTransitionReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerCrouchExitIdleTransition] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch exit Idle transition capture requires Play Mode.");
                    }

                    CaptureExitIdleTransitionActualReview();
                    SessionState.SetInt(ExitIdleTransitionReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Crouch exit Idle transition review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(ExitIdleTransitionReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerCrouchExitIdleTransition] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Crouch exit Idle transition review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(ExitIdleTransitionReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Crouch Exit Idle Transition Final")]
        internal static void CaptureExitIdleTransitionFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Crouch exit Idle transition final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch exit Idle transition final capture.");
            }

            ExitIdleTransitionReviewMetrics metrics =
                ReadJson<ExitIdleTransitionReviewMetrics>(
                    ExitIdleTransitionReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                !metrics.exit.passedNumericChecks ||
                metrics.exit.loopsSampled != 2)
            {
                throw new InvalidOperationException(
                    "Crouch exit Idle transition review did not pass before final capture.");
            }

            CopyReviewedContact(
                ExitIdleTransitionReviewPath,
                ExitIdleTransitionFinalPath);
            Debug.Log(
                "[PlayerCrouchExitIdleTransition] Final image copied once from directly reviewed Play Mode frames. " +
                "Exit=" + Path.GetFullPath(ExitIdleTransitionFinalPath) +
                ", SceneChanged=False.");
        }

        private static void CaptureExitIdleTransitionActualReview()
        {
            ExitIdleTransitionApplyMetrics apply =
                ReadJson<ExitIdleTransitionApplyMetrics>(
                    ExitIdleTransitionApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch exit Idle transition apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform exitTarget = RequireTarget(layout, ExitTargetName);
            Transform idleTarget = RequireTarget(layout, IdleTargetName);
            AnimationClip source = LoadSingleExitSourceClip();
            AnimationClip transition = LoadClip(ExitClipPath);
            AnimationClip idle = LoadClip(IdleClipPath);
            CaptureExitIdleTransitionComparison(
                exitTarget,
                idleTarget,
                source,
                transition,
                idle,
                apply.originalDurationSeconds);
            TargetReviewMetrics exit = CaptureExitIdleTransitionMetrics(
                exitTarget,
                idleTarget,
                source,
                transition,
                idle,
                apply.originalDurationSeconds);
            exit.passedNumericChecks =
                exit.framesSampled == exit.framesPerLoop * 2 &&
                exit.loopsSampled == 2 &&
                exit.rootPositionDisplacementMax <= PositionTolerance &&
                exit.sourceStartPositionDifferenceMax <= PositionTolerance &&
                exit.sourceStartRotationDifferenceDegreesMax <= RotationTolerance &&
                exit.idleEndPositionDifferenceMax <= PositionTolerance &&
                exit.idleEndRotationDifferenceDegreesMax <= RotationTolerance &&
                exit.holdPositionDifferenceMax <= PositionTolerance &&
                exit.holdRotationDifferenceDegreesMax <= RotationTolerance &&
                exit.stateLoops &&
                !exit.applyRootMotion;
            ExitIdleTransitionReviewMetrics metrics =
                new ExitIdleTransitionReviewMetrics
                {
                    target = ExitTargetName,
                    exit = exit,
                    passedNumericChecks = exit.passedNumericChecks,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteJson(ExitIdleTransitionReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch exit Idle transition Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerCrouchExitIdleTransition] Captured actual Play Mode source-to-Idle transition. " +
                "Frames=" + exit.framesSampled +
                ", Root=" + Num(exit.rootPositionDisplacementMax) +
                ", StartPosition=" + Num(exit.sourceStartPositionDifferenceMax) +
                ", StartRotation=" + Num(exit.sourceStartRotationDifferenceDegreesMax) +
                ", IdleEndPosition=" + Num(exit.idleEndPositionDifferenceMax) +
                ", IdleEndRotation=" + Num(exit.idleEndRotationDifferenceDegreesMax) +
                ", HoldPosition=" + Num(exit.holdPositionDifferenceMax) +
                ", HoldRotation=" + Num(exit.holdRotationDifferenceDegreesMax) +
                ", Loops=2.");
        }

        private static void CaptureActualReview()
        {
            ApplyMetrics apply = ReadJson<ApplyMetrics>(ApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch diagonal and exit apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform diagonalTarget = RequireTarget(layout, DiagonalTargetName);
            Transform exitTarget = RequireTarget(layout, ExitTargetName);
            AnimationClip forward = LoadClip(ForwardClipPath);
            AnimationClip sidestep = LoadClip(SidestepClipPath);
            AnimationClip exitSource = LoadSingleExitSourceClip();
            AnimationClip exitClip = LoadClip(ExitClipPath);

            CaptureDiagonalComparison(diagonalTarget, forward, sidestep);
            CaptureExitComparison(
                exitTarget,
                exitSource,
                exitClip,
                apply.exitOriginalDurationSeconds);
            TargetReviewMetrics diagonal = CaptureTargetMetrics(
                diagonalTarget,
                DiagonalStateName,
                Mathf.Lerp(forward.length, sidestep.length, BlendWeight),
                Mathf.Max(forward.frameRate, sidestep.frameRate),
                false,
                0f);
            TargetReviewMetrics exit = CaptureTargetMetrics(
                exitTarget,
                ExitStateName,
                exitClip.length,
                exitClip.frameRate,
                true,
                apply.exitOriginalDurationSeconds);
            diagonal.passedNumericChecks =
                diagonal.framesSampled == diagonal.framesPerLoop * 2 &&
                diagonal.loopsSampled == 2 &&
                diagonal.rootPositionDisplacementMax <= PositionTolerance &&
                Mathf.Abs(diagonal.blendParameterValue - BlendWeight) <= 0.0001f &&
                diagonal.stateLoops &&
                !diagonal.applyRootMotion;
            exit.passedNumericChecks =
                exit.framesSampled == exit.framesPerLoop * 2 &&
                exit.loopsSampled == 2 &&
                exit.rootPositionDisplacementMax <= PositionTolerance &&
                exit.holdPositionDifferenceMax <= PositionTolerance &&
                exit.holdRotationDifferenceDegreesMax <= RotationTolerance &&
                exit.stateLoops &&
                !exit.applyRootMotion;
            ReviewMetrics metrics = new ReviewMetrics
            {
                targetSet = DiagonalTargetName + ", " + ExitTargetName,
                diagonal = diagonal,
                exit = exit,
                passedNumericChecks =
                    diagonal.passedNumericChecks && exit.passedNumericChecks,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(ReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Crouch diagonal and exit Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerCrouchDiagonalExit] Captured actual Play Mode direct comparisons. " +
                "DiagonalFrames=" + diagonal.framesSampled +
                ", DiagonalRoot=" + Num(diagonal.rootPositionDisplacementMax) +
                ", ExitFrames=" + exit.framesSampled +
                ", ExitRoot=" + Num(exit.rootPositionDisplacementMax) +
                ", ExitHoldPosition=" + Num(exit.holdPositionDifferenceMax) +
                ", ExitHoldRotation=" + Num(exit.holdRotationDifferenceDegreesMax) +
                ", LoopsPerTarget=2.");
        }

        private static AnimationClip CreateOrUpdateExitIdleTransitionClip(
            AnimationClip source,
            AnimationClip idle,
            Transform exitTemplate,
            Transform idleTemplate,
            out int sourceFramesSampled,
            out int armatureTransformsBaked)
        {
            float originalDuration = source.length;
            float holdEnd = originalDuration + ExitHoldSeconds;
            int frameIntervals = Mathf.Max(
                1,
                Mathf.RoundToInt(originalDuration * source.frameRate));
            float[] times = Enumerable.Range(0, frameIntervals + 1)
                .Select(index => index == frameIntervals
                    ? originalDuration
                    : index / source.frameRate)
                .ToArray();
            sourceFramesSampled = times.Length;
            PoseSnapshot idlePose = CapturePoseFromClip(idleTemplate, idle, 0f);
            List<PoseSnapshot> sourcePoses = CapturePoseSequence(
                exitTemplate,
                source,
                times);
            string[] paths = sourcePoses[0].Positions.Keys
                .Where(path =>
                    string.Equals(path, "Armature", StringComparison.Ordinal) ||
                    path.StartsWith("Armature/", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (paths.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player crouch exit rig has no Armature transforms to bake.");
            }

            foreach (string path in paths)
            {
                if (!idlePose.Positions.ContainsKey(path) ||
                    !idlePose.Rotations.ContainsKey(path))
                {
                    throw new InvalidOperationException(
                        "Player Idle first-frame pose is missing crouch exit path " + path + ".");
                }
            }

            armatureTransformsBaked = paths.Length;
            Dictionary<string, List<Vector3>> blendedPositions = paths.ToDictionary(
                path => path,
                _ => new List<Vector3>(times.Length),
                StringComparer.Ordinal);
            Dictionary<string, List<Quaternion>> blendedRotations = paths.ToDictionary(
                path => path,
                _ => new List<Quaternion>(times.Length),
                StringComparer.Ordinal);
            for (int frame = 0; frame < times.Length; frame++)
            {
                float normalizedTime = originalDuration <= 0f
                    ? 1f
                    : times[frame] / originalDuration;
                float weight = TransitionWeight(normalizedTime);
                PoseSnapshot sourcePose = sourcePoses[frame];
                foreach (string path in paths)
                {
                    Vector3 position = Vector3.LerpUnclamped(
                        sourcePose.Positions[path],
                        idlePose.Positions[path],
                        weight);
                    Quaternion rotation = Quaternion.SlerpUnclamped(
                        sourcePose.Rotations[path],
                        idlePose.Rotations[path],
                        weight).normalized;
                    List<Quaternion> rotations = blendedRotations[path];
                    if (rotations.Count > 0 &&
                        Quaternion.Dot(rotations[rotations.Count - 1], rotation) < 0f)
                    {
                        rotation = new Quaternion(
                            -rotation.x,
                            -rotation.y,
                            -rotation.z,
                            -rotation.w);
                    }

                    blendedPositions[path].Add(position);
                    rotations.Add(rotation);
                }
            }

            AnimationClip generated = new AnimationClip();
            EditorUtility.CopySerialized(source, generated);
            generated.name = "Player_Crouch_Exit_Mixamo_Hold";
            generated.wrapMode = WrapMode.Loop;
            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetCurveBindings(generated)
                         .Where(binding => binding.type == typeof(Transform))
                         .ToArray())
            {
                AnimationUtility.SetEditorCurve(generated, binding, null);
            }

            foreach (string path in paths)
            {
                List<Vector3> positions = blendedPositions[path];
                List<Quaternion> rotations = blendedRotations[path];
                SetBakedCurve(
                    generated,
                    path,
                    "m_LocalPosition.x",
                    times,
                    positions.Select(value => value.x).ToArray(),
                    holdEnd);
                SetBakedCurve(
                    generated,
                    path,
                    "m_LocalPosition.y",
                    times,
                    positions.Select(value => value.y).ToArray(),
                    holdEnd);
                SetBakedCurve(
                    generated,
                    path,
                    "m_LocalPosition.z",
                    times,
                    positions.Select(value => value.z).ToArray(),
                    holdEnd);
                SetBakedCurve(
                    generated,
                    path,
                    "m_LocalRotation.x",
                    times,
                    rotations.Select(value => value.x).ToArray(),
                    holdEnd);
                SetBakedCurve(
                    generated,
                    path,
                    "m_LocalRotation.y",
                    times,
                    rotations.Select(value => value.y).ToArray(),
                    holdEnd);
                SetBakedCurve(
                    generated,
                    path,
                    "m_LocalRotation.z",
                    times,
                    rotations.Select(value => value.z).ToArray(),
                    holdEnd);
                SetBakedCurve(
                    generated,
                    path,
                    "m_LocalRotation.w",
                    times,
                    rotations.Select(value => value.w).ToArray(),
                    holdEnd);
            }

            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetCurveBindings(source)
                         .Where(binding => binding.type != typeof(Transform)))
            {
                AnimationUtility.SetEditorCurve(
                    generated,
                    binding,
                    ExtendCurveWithConstantHold(
                        AnimationUtility.GetEditorCurve(source, binding),
                        originalDuration,
                        holdEnd));
            }

            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                ObjectReferenceKeyframe[] sourceKeys =
                    AnimationUtility.GetObjectReferenceCurve(source, binding);
                List<ObjectReferenceKeyframe> keys =
                    new List<ObjectReferenceKeyframe>(sourceKeys);
                if (sourceKeys.Length > 0)
                {
                    keys.Add(new ObjectReferenceKeyframe
                    {
                        time = holdEnd,
                        value = sourceKeys[sourceKeys.Length - 1].value
                    });
                }

                AnimationUtility.SetObjectReferenceCurve(
                    generated,
                    binding,
                    keys.ToArray());
            }

            AnimationUtility.SetAnimationEvents(
                generated,
                AnimationUtility.GetAnimationEvents(source));
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(generated);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(generated, settings);
            AnimationClip saved = SaveClip(generated, ExitClipPath);
            AssetDatabase.ImportAsset(ExitClipPath, ImportAssetOptions.ForceUpdate);
            return saved;
        }

        private static void SetBakedCurve(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyList<float> times,
            IReadOnlyList<float> values,
            float holdEnd)
        {
            if (times.Count != values.Count || times.Count < 2)
            {
                throw new InvalidOperationException(
                    "Crouch exit transition curve sample count differs for " +
                    path + " " + property + ".");
            }

            Keyframe[] keys = new Keyframe[times.Count + 1];
            for (int index = 0; index < times.Count; index++)
            {
                float previousSlope = index == 0
                    ? (values[1] - values[0]) / (times[1] - times[0])
                    : (values[index] - values[index - 1]) /
                      (times[index] - times[index - 1]);
                float nextSlope = index == times.Count - 1
                    ? 0f
                    : (values[index + 1] - values[index]) /
                      (times[index + 1] - times[index]);
                keys[index] = new Keyframe(
                    times[index],
                    values[index],
                    previousSlope,
                    nextSlope);
            }

            keys[keys.Length - 1] = new Keyframe(
                holdEnd,
                values[values.Count - 1],
                0f,
                0f);
            AnimationCurve curve = new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static float TransitionWeight(float normalizedTime)
        {
            float value = Mathf.Clamp01(normalizedTime);
            return value * value * (3f - 2f * value);
        }

        private static bool TransitionWeightsAreMonotonic(int sampleCount)
        {
            if (sampleCount < 2)
            {
                return false;
            }

            float previous = -1f;
            for (int index = 0; index < sampleCount; index++)
            {
                float weight = TransitionWeight(index / (float)(sampleCount - 1));
                if (weight + 0.000001f < previous)
                {
                    return false;
                }

                previous = weight;
            }

            return Mathf.Abs(TransitionWeight(0f)) <= 0.000001f &&
                   Mathf.Abs(TransitionWeight(1f) - 1f) <= 0.000001f;
        }

        private static List<PoseSnapshot> CapturePoseSequence(
            Transform template,
            AnimationClip clip,
            IReadOnlyList<float> times)
        {
            GameObject clone = UnityEngine.Object.Instantiate(template.gameObject);
            clone.name = template.name + "PoseSequenceClone";
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (Animator animator in clone.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                }

                List<PoseSnapshot> poses = new List<PoseSnapshot>(times.Count);
                foreach (float time in times)
                {
                    clip.SampleAnimation(clone, time);
                    poses.Add(CapturePose(clone.transform));
                }

                return poses;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static PoseSnapshot CapturePoseFromClip(
            Transform template,
            AnimationClip clip,
            float time)
        {
            return CapturePoseSequence(template, clip, new[] { time })[0];
        }

        private static AnimationClip CreateOrUpdateExitHoldClip(AnimationClip source)
        {
            float originalDuration = source.length;
            float holdEnd = originalDuration + ExitHoldSeconds;
            AnimationClip generated = new AnimationClip();
            EditorUtility.CopySerialized(source, generated);
            generated.name = "Player_Crouch_Exit_Mixamo_Hold";
            generated.wrapMode = WrapMode.Loop;

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationUtility.SetEditorCurve(
                    generated,
                    binding,
                    ExtendCurveWithConstantHold(
                        sourceCurve,
                        originalDuration,
                        holdEnd));
            }

            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                ObjectReferenceKeyframe[] sourceKeys =
                    AnimationUtility.GetObjectReferenceCurve(source, binding);
                List<ObjectReferenceKeyframe> keys =
                    new List<ObjectReferenceKeyframe>(sourceKeys);
                if (sourceKeys.Length > 0)
                {
                    keys.Add(new ObjectReferenceKeyframe
                    {
                        time = holdEnd,
                        value = sourceKeys[sourceKeys.Length - 1].value
                    });
                }

                AnimationUtility.SetObjectReferenceCurve(
                    generated,
                    binding,
                    keys.ToArray());
            }

            AnimationUtility.SetAnimationEvents(
                generated,
                AnimationUtility.GetAnimationEvents(source));
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(generated);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(generated, settings);
            AnimationClip saved = SaveClip(generated, ExitClipPath);
            AssetDatabase.ImportAsset(ExitClipPath, ImportAssetOptions.ForceUpdate);
            return saved;
        }

        private static AnimationCurve ExtendCurveWithConstantHold(
            AnimationCurve source,
            float originalDuration,
            float holdEnd)
        {
            if (source == null || source.length == 0)
            {
                return source == null ? null : new AnimationCurve();
            }

            List<Keyframe> keys = new List<Keyframe>(source.keys);
            int lastIndex = keys.Count - 1;
            Keyframe last = keys[lastIndex];
            last.outTangent = 0f;
            last.outWeight = 0f;
            last.weightedMode &= ~WeightedMode.Out;
            keys[lastIndex] = last;
            float finalValue = source.Evaluate(originalDuration);
            if (last.time < originalDuration - 0.00001f)
            {
                keys.Add(new Keyframe(originalDuration, finalValue, 0f, 0f));
            }

            keys.Add(new Keyframe(holdEnd, finalValue, 0f, 0f));
            AnimationCurve extended = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return extended;
        }

        private static AnimationClip SaveClip(AnimationClip generated, string path)
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static AnimatorController CreateOrUpdateDiagonalController(
            AnimationClip forward,
            AnimationClip sidestep)
        {
            AnimatorController controller = LoadOrCreateController(DiagonalControllerPath);
            ClearController(controller);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = BlendParameterName,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = BlendWeight
            });
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            BlendTree tree = new BlendTree
            {
                name = "PlayerCrouchDiagonal50_50",
                blendType = BlendTreeType.Simple1D,
                blendParameter = BlendParameterName,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(forward, 0f);
            tree.AddChild(sidestep, 1f);
            AnimatorState state = stateMachine.AddState(DiagonalStateName);
            state.motion = tree;
            state.speed = 1f;
            state.mirror = false;
            state.cycleOffset = 0f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController CreateOrUpdateSingleClipController(
            string path,
            string stateName,
            AnimationClip clip)
        {
            AnimatorController controller = LoadOrCreateController(path);
            ClearController(controller);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
            state.speed = 1f;
            state.mirror = false;
            state.cycleOffset = 0f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController LoadOrCreateController(string path)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    Path.GetFileName(path) + " must contain exactly one layer.");
            }

            return controller;
        }

        private static void ClearController(AnimatorController controller)
        {
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(child.stateMachine);
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (BlendTree tree in AssetDatabase.LoadAllAssetsAtPath(
                         AssetDatabase.GetAssetPath(controller)).OfType<BlendTree>().ToArray())
            {
                UnityEngine.Object.DestroyImmediate(tree, true);
            }
        }

        private static Animator ConfigureAnimator(
            Transform target,
            RuntimeAnimatorController controller)
        {
            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                animator = target.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return animator;
        }

        private static void ConfigureExitImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ExitSourcePath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Player crouch exit FBX ModelImporter is unavailable.");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadSingleExitSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ExitSourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player crouch exit FBX must expose exactly one non-preview AnimationClip; actual=" +
                    clips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            if (!string.Equals(clips[0].name, ExpectedTakeName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player crouch exit embedded Take differs; expected '" +
                    ExpectedTakeName + "', actual '" + clips[0].name + "'.");
            }

            return clips[0];
        }

        private static AnimationClip LoadClip(string path)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            return clip ?? throw new FileNotFoundException(
                "Required player crouch clip is missing.",
                Path.GetFullPath(path));
        }

        private static bool EnsureExactExitSourceCopy()
        {
            string actual = HashFile(ExitSourcePath);
            if (!string.Equals(actual, ExpectedExitSourceHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Player crouch exit Unity FBX differs from the approved source. Expected=" +
                    ExpectedExitSourceHash + ", Actual=" + actual + ".");
            }

            return true;
        }

        private static bool IsExpectedBlendTree(
            AnimatorController controller,
            AnimationClip forward,
            AnimationClip sidestep)
        {
            if (controller.parameters.Length != 1 ||
                controller.parameters[0].type != AnimatorControllerParameterType.Float ||
                !string.Equals(
                    controller.parameters[0].name,
                    BlendParameterName,
                    StringComparison.Ordinal) ||
                Mathf.Abs(controller.parameters[0].defaultFloat - BlendWeight) > 0.0001f)
            {
                return false;
            }

            AnimatorState[] states = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .ToArray();
            if (states.Length != 1 ||
                !string.Equals(states[0].name, DiagonalStateName, StringComparison.Ordinal) ||
                !(states[0].motion is BlendTree tree) ||
                tree.blendType != BlendTreeType.Simple1D ||
                !string.Equals(tree.blendParameter, BlendParameterName, StringComparison.Ordinal))
            {
                return false;
            }

            ChildMotion[] children = tree.children;
            return children.Length == 2 &&
                   children[0].motion == forward &&
                   Mathf.Abs(children[0].threshold) <= 0.0001f &&
                   children[1].motion == sidestep &&
                   Mathf.Abs(children[1].threshold - 1f) <= 0.0001f;
        }

        private static bool OriginalCurvesPreserved(
            AnimationClip source,
            AnimationClip derived)
        {
            EditorCurveBinding[] sourceBindings = AnimationUtility.GetCurveBindings(source);
            EditorCurveBinding[] derivedBindings = AnimationUtility.GetCurveBindings(derived);
            if (sourceBindings.Length != derivedBindings.Length)
            {
                return false;
            }

            foreach (EditorCurveBinding binding in sourceBindings)
            {
                if (!derivedBindings.Contains(binding))
                {
                    return false;
                }

                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationCurve derivedCurve = AnimationUtility.GetEditorCurve(derived, binding);
                for (int sample = 0; sample <= 240; sample++)
                {
                    float time = source.length * sample / 240f;
                    if (Mathf.Abs(sourceCurve.Evaluate(time) - derivedCurve.Evaluate(time)) > 0.00001f)
                    {
                        return false;
                    }
                }
            }

            EditorCurveBinding[] sourceObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(source);
            EditorCurveBinding[] derivedObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(derived);
            if (sourceObjectBindings.Length != derivedObjectBindings.Length)
            {
                return false;
            }

            foreach (EditorCurveBinding binding in sourceObjectBindings)
            {
                if (!derivedObjectBindings.Contains(binding))
                {
                    return false;
                }

                ObjectReferenceKeyframe[] sourceKeys =
                    AnimationUtility.GetObjectReferenceCurve(source, binding);
                ObjectReferenceKeyframe[] derivedKeys =
                    AnimationUtility.GetObjectReferenceCurve(derived, binding);
                if (derivedKeys.Length < sourceKeys.Length)
                {
                    return false;
                }

                for (int index = 0; index < sourceKeys.Length; index++)
                {
                    if (Mathf.Abs(sourceKeys[index].time - derivedKeys[index].time) > 0.00001f ||
                        sourceKeys[index].value != derivedKeys[index].value)
                    {
                        return false;
                    }
                }
            }

            return Mathf.Abs(source.frameRate - derived.frameRate) <= 0.0001f;
        }

        private static bool HoldIsConstant(
            AnimationClip clip,
            float holdStart,
            float holdEnd)
        {
            float first = Mathf.Min(holdStart + 0.001f, holdEnd);
            float second = Mathf.Max(first, holdEnd - 0.001f);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (Mathf.Abs(curve.Evaluate(first) - curve.Evaluate(second)) > 0.00001f)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CaptureDiagonalComparison(
            Transform target,
            AnimationClip forward,
            AnimationClip sidestep)
        {
            Animator animator = RequireAnimator(target);
            float[] phases = { 0f, 0.25f, 0.5f, 0.75f, 0.99f };
            CaptureEnvironment environment = new CaptureEnvironment(target);
            try
            {
                List<List<byte[]>> rows = new List<List<byte[]>>
                {
                    new List<byte[]>(),
                    new List<byte[]>(),
                    new List<byte[]>(),
                    new List<byte[]>()
                };
                foreach (float phase in phases)
                {
                    forward.SampleAnimation(target.gameObject, phase * forward.length);
                    rows[0].Add(environment.CaptureFront());
                    sidestep.SampleAnimation(target.gameObject, phase * sidestep.length);
                    rows[1].Add(environment.CaptureFront());
                    SampleAnimator(animator, DiagonalStateName, phase);
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());
                }

                ComposeRows(rows, DiagonalReviewPath);
            }
            finally
            {
                environment.Dispose();
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void CaptureExitComparison(
            Transform target,
            AnimationClip source,
            AnimationClip applied,
            float originalDuration)
        {
            Animator animator = RequireAnimator(target);
            float epsilon = 0.001f;
            float[] times =
            {
                0f,
                originalDuration * 0.33f,
                originalDuration * 0.66f,
                Mathf.Max(0f, originalDuration - epsilon),
                originalDuration + ExitHoldSeconds * 0.5f,
                applied.length - epsilon
            };
            CaptureEnvironment environment = new CaptureEnvironment(target);
            try
            {
                List<List<byte[]>> rows = new List<List<byte[]>>
                {
                    new List<byte[]>(),
                    new List<byte[]>(),
                    new List<byte[]>(),
                    new List<byte[]>()
                };
                foreach (float time in times)
                {
                    source.SampleAnimation(
                        target.gameObject,
                        Mathf.Min(time, originalDuration));
                    rows[0].Add(environment.CaptureFront());
                    rows[1].Add(environment.CaptureSide());
                    SampleAnimator(
                        animator,
                        ExitStateName,
                        Mathf.Clamp01(time / applied.length));
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());
                }

                ComposeRows(rows, ExitReviewPath);
            }
            finally
            {
                environment.Dispose();
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void CaptureExitIdleTransitionComparison(
            Transform exitTarget,
            Transform idleTarget,
            AnimationClip source,
            AnimationClip transition,
            AnimationClip idle,
            float originalDuration)
        {
            if (idleTarget == null)
            {
                throw new ArgumentNullException(nameof(idleTarget));
            }

            Animator animator = RequireAnimator(exitTarget);
            float epsilon = 0.001f;
            float[] times =
            {
                0f,
                originalDuration * 0.2f,
                originalDuration * 0.4f,
                originalDuration * 0.6f,
                originalDuration * 0.8f,
                Mathf.Max(0f, originalDuration - epsilon),
                originalDuration + ExitHoldSeconds * 0.5f,
                transition.length - epsilon
            };
            CaptureEnvironment environment = new CaptureEnvironment(exitTarget);
            try
            {
                List<List<byte[]>> rows = new List<List<byte[]>>
                {
                    new List<byte[]>(),
                    new List<byte[]>(),
                    new List<byte[]>(),
                    new List<byte[]>(),
                    new List<byte[]>(),
                    new List<byte[]>()
                };
                foreach (float time in times)
                {
                    float sourceTime = Mathf.Min(
                        time,
                        Mathf.Max(0f, originalDuration - epsilon));
                    source.SampleAnimation(exitTarget.gameObject, sourceTime);
                    rows[0].Add(environment.CaptureFront());
                    rows[1].Add(environment.CaptureSide());

                    SampleAnimator(
                        animator,
                        ExitStateName,
                        Mathf.Clamp01(time / transition.length));
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());

                    idle.SampleAnimation(exitTarget.gameObject, 0f);
                    rows[4].Add(environment.CaptureFront());
                    rows[5].Add(environment.CaptureSide());
                }

                ComposeRows(rows, ExitIdleTransitionReviewPath);
            }
            finally
            {
                environment.Dispose();
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static TargetReviewMetrics CaptureExitIdleTransitionMetrics(
            Transform exitTarget,
            Transform idleTarget,
            AnimationClip source,
            AnimationClip transition,
            AnimationClip idle,
            float originalDuration)
        {
            Animator animator = RequireAnimator(exitTarget);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = exitTarget.position;
            float rootMax = 0f;
            try
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.CeilToInt(transition.length * transition.frameRate));
                for (int frame = 0; frame < framesPerLoop * 2; frame++)
                {
                    SampleAnimator(
                        animator,
                        ExitStateName,
                        frame / (float)framesPerLoop);
                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(exitTarget.position, rootBaseline));
                }

                PoseSnapshot sourceStart = CapturePoseFromClip(
                    exitTarget,
                    source,
                    0f);
                SampleAnimator(animator, ExitStateName, 0f);
                PoseSnapshot transitionStart = CapturePose(exitTarget);
                PoseSnapshot idleFirst = CapturePoseFromClip(
                    idleTarget,
                    idle,
                    0f);
                SampleAnimator(
                    animator,
                    ExitStateName,
                    originalDuration / transition.length);
                PoseSnapshot transitionEnd = CapturePose(exitTarget);
                float holdFirstTime = Mathf.Min(
                    originalDuration + 0.01f,
                    transition.length - 0.02f);
                float holdSecondTime = Mathf.Max(
                    holdFirstTime,
                    transition.length - 0.01f);
                SampleAnimator(
                    animator,
                    ExitStateName,
                    holdFirstTime / transition.length);
                PoseSnapshot holdStart = CapturePose(exitTarget);
                SampleAnimator(
                    animator,
                    ExitStateName,
                    holdSecondTime / transition.length);
                PoseSnapshot holdEnd = CapturePose(exitTarget);
                MeasurePoseDifference(
                    sourceStart,
                    transitionStart,
                    out float sourceStartPositionDifference,
                    out float sourceStartRotationDifference);
                MeasureArmaturePoseDifference(
                    idleFirst,
                    transitionEnd,
                    out float idleEndPositionDifference,
                    out float idleEndRotationDifference);
                MeasurePoseDifference(
                    holdStart,
                    holdEnd,
                    out float holdPositionDifference,
                    out float holdRotationDifference);

                SampleAnimator(animator, ExitStateName, 0f);
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                return new TargetReviewMetrics
                {
                    target = exitTarget.name,
                    state = ExitStateName,
                    durationSeconds = transition.length,
                    framesPerLoop = framesPerLoop,
                    framesSampled = framesPerLoop * 2,
                    loopsSampled = 2,
                    rootPositionDisplacementMax = rootMax,
                    sourceStartPositionDifferenceMax = sourceStartPositionDifference,
                    sourceStartRotationDifferenceDegreesMax = sourceStartRotationDifference,
                    idleEndPositionDifferenceMax = idleEndPositionDifference,
                    idleEndRotationDifferenceDegreesMax = idleEndRotationDifference,
                    holdPositionDifferenceMax = holdPositionDifference,
                    holdRotationDifferenceDegreesMax = holdRotationDifference,
                    blendParameterValue = 0f,
                    stateLoops = info.loop,
                    applyRootMotion = animator.applyRootMotion
                };
            }
            finally
            {
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static TargetReviewMetrics CaptureTargetMetrics(
            Transform target,
            string stateName,
            float duration,
            float frameRate,
            bool measureHold,
            float holdStart)
        {
            Animator animator = RequireAnimator(target);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = target.position;
            float rootMax = 0f;
            try
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int framesPerLoop = Mathf.Max(4, Mathf.CeilToInt(duration * frameRate));
                for (int frame = 0; frame < framesPerLoop * 2; frame++)
                {
                    SampleAnimator(
                        animator,
                        stateName,
                        frame / (float)framesPerLoop);
                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(target.position, rootBaseline));
                }

                SampleAnimator(animator, stateName, 0f);
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                float holdPosition = 0f;
                float holdRotation = 0f;
                if (measureHold)
                {
                    float firstTime = Mathf.Min(holdStart + 0.01f, duration - 0.02f);
                    float secondTime = Mathf.Max(firstTime, duration - 0.01f);
                    SampleAnimator(animator, stateName, firstTime / duration);
                    PoseSnapshot first = CapturePose(target);
                    SampleAnimator(animator, stateName, secondTime / duration);
                    PoseSnapshot second = CapturePose(target);
                    MeasurePoseDifference(
                        first,
                        second,
                        out holdPosition,
                        out holdRotation);
                }

                float blendValue = 0f;
                if (HasFloatParameter(animator, BlendParameterName))
                {
                    blendValue = animator.GetFloat(BlendParameterName);
                }

                return new TargetReviewMetrics
                {
                    target = target.name,
                    state = stateName,
                    durationSeconds = duration,
                    framesPerLoop = framesPerLoop,
                    framesSampled = framesPerLoop * 2,
                    loopsSampled = 2,
                    rootPositionDisplacementMax = rootMax,
                    holdPositionDifferenceMax = holdPosition,
                    holdRotationDifferenceDegreesMax = holdRotation,
                    blendParameterValue = blendValue,
                    stateLoops = info.loop,
                    applyRootMotion = animator.applyRootMotion
                };
            }
            finally
            {
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void SampleAnimator(
            Animator animator,
            string stateName,
            float normalizedTime)
        {
            int stateHash = Animator.StringToHash(stateName);
            animator.Rebind();
            animator.Update(0f);
            animator.Play(stateHash, 0, normalizedTime);
            animator.Update(0f);
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(stateName))
            {
                throw new InvalidOperationException(
                    animator.name + " did not enter expected state " + stateName + ".");
            }
        }

        private static bool HasFloatParameter(Animator animator, string name)
        {
            return animator.parameters.Any(parameter =>
                parameter.type == AnimatorControllerParameterType.Float &&
                string.Equals(parameter.name, name, StringComparison.Ordinal));
        }

        private static PoseSnapshot CapturePose(Transform root)
        {
            PoseSnapshot pose = new PoseSnapshot();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(item, root);
                pose.Positions[path] = item.localPosition;
                pose.Rotations[path] = item.localRotation;
                pose.Scales[path] = item.localScale;
            }

            return pose;
        }

        private static void MeasurePoseDifference(
            PoseSnapshot first,
            PoseSnapshot second,
            out float positionMax,
            out float rotationMax)
        {
            positionMax = 0f;
            rotationMax = 0f;
            foreach (KeyValuePair<string, Vector3> item in first.Positions)
            {
                if (!second.Positions.TryGetValue(item.Key, out Vector3 secondPosition) ||
                    !first.Rotations.TryGetValue(item.Key, out Quaternion firstRotation) ||
                    !second.Rotations.TryGetValue(item.Key, out Quaternion secondRotation))
                {
                    throw new InvalidOperationException(
                        "Crouch pose hierarchy changed during review at " + item.Key + ".");
                }

                positionMax = Mathf.Max(
                    positionMax,
                    Vector3.Distance(item.Value, secondPosition));
                rotationMax = Mathf.Max(
                    rotationMax,
                    Quaternion.Angle(firstRotation, secondRotation));
            }
        }

        private static void MeasureArmaturePoseDifference(
            PoseSnapshot first,
            PoseSnapshot second,
            out float positionMax,
            out float rotationMax)
        {
            positionMax = 0f;
            rotationMax = 0f;
            string[] paths = first.Positions.Keys
                .Where(path =>
                    string.Equals(path, "Armature", StringComparison.Ordinal) ||
                    path.StartsWith("Armature/", StringComparison.Ordinal))
                .ToArray();
            if (paths.Length == 0)
            {
                throw new InvalidOperationException(
                    "Crouch pose hierarchy has no Armature transforms to compare.");
            }

            foreach (string path in paths)
            {
                if (!second.Positions.TryGetValue(path, out Vector3 secondPosition) ||
                    !first.Rotations.TryGetValue(path, out Quaternion firstRotation) ||
                    !second.Rotations.TryGetValue(path, out Quaternion secondRotation))
                {
                    throw new InvalidOperationException(
                        "Crouch Armature hierarchy changed during review at " + path + ".");
                }

                positionMax = Mathf.Max(
                    positionMax,
                    Vector3.Distance(first.Positions[path], secondPosition));
                rotationMax = Mathf.Max(
                    rotationMax,
                    Quaternion.Angle(firstRotation, secondRotation));
            }
        }

        private sealed class CaptureEnvironment : IDisposable
        {
            private readonly Transform target;
            private readonly RendererState[] hiddenRenderers;
            private readonly GameObject frontCameraObject;
            private readonly GameObject sideCameraObject;
            private readonly GameObject lightObject;
            private readonly RenderTexture renderTexture;
            private readonly Texture2D frameTexture;
            private readonly RenderTexture previousActive;

            internal CaptureEnvironment(Transform value)
            {
                target = value;
                Renderer[] targetRenderers = target
                    .GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.enabled)
                    .ToArray();
                if (targetRenderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        target.name + " has no enabled renderer.");
                }

                HashSet<Renderer> targetSet = new HashSet<Renderer>(targetRenderers);
                hiddenRenderers = Resources.FindObjectsOfTypeAll<Renderer>()
                    .Where(renderer =>
                        renderer != null &&
                        renderer.enabled &&
                        renderer.gameObject.scene.IsValid() &&
                        !targetSet.Contains(renderer))
                    .Select(renderer => new RendererState(renderer))
                    .ToArray();
                foreach (RendererState state in hiddenRenderers)
                {
                    state.Hide();
                }

                frontCameraObject = CreateCameraObject(target.name + "FrontCamera");
                sideCameraObject = CreateCameraObject(target.name + "SideCamera");
                Vector3 center = target.position + target.up * 1.05f;
                ConfigureFixedCamera(
                    frontCameraObject.GetComponent<Camera>(),
                    target,
                    center,
                    target.forward,
                    1.35f);
                ConfigureFixedCamera(
                    sideCameraObject.GetComponent<Camera>(),
                    target,
                    center,
                    target.right,
                    1.35f);
                lightObject = new GameObject(target.name + "ReviewLight", typeof(Light));
                lightObject.hideFlags = HideFlags.HideAndDontSave;
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.transform.rotation = Quaternion.LookRotation(
                    -target.forward - target.up * 0.65f,
                    target.up);
                renderTexture = new RenderTexture(
                    CaptureWidth,
                    CaptureHeight,
                    24,
                    RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 2
                };
                renderTexture.Create();
                frameTexture = new Texture2D(
                    CaptureWidth,
                    CaptureHeight,
                    TextureFormat.RGB24,
                    false);
                previousActive = RenderTexture.active;
            }

            internal byte[] CaptureFront()
            {
                return CaptureFrame(frontCameraObject.GetComponent<Camera>());
            }

            internal byte[] CaptureSide()
            {
                return CaptureFrame(sideCameraObject.GetComponent<Camera>());
            }

            private byte[] CaptureFrame(Camera camera)
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                frameTexture.ReadPixels(
                    new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                    0,
                    0,
                    false);
                frameTexture.Apply(false, false);
                byte[] png = frameTexture.EncodeToPNG();
                camera.targetTexture = null;
                return png;
            }

            public void Dispose()
            {
                foreach (RendererState state in hiddenRenderers)
                {
                    state.Restore();
                }

                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(frameTexture);
                UnityEngine.Object.DestroyImmediate(frontCameraObject);
                UnityEngine.Object.DestroyImmediate(sideCameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static GameObject CreateCameraObject(string name)
        {
            GameObject cameraObject = new GameObject(name, typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = true;
            camera.aspect = CaptureWidth / (float)CaptureHeight;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            return cameraObject;
        }

        private static void ConfigureFixedCamera(
            Camera camera,
            Transform target,
            Vector3 center,
            Vector3 viewDirection,
            float orthographicSize)
        {
            Vector3 direction = Vector3.ProjectOnPlane(viewDirection, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    target.name + " has no usable review direction.");
            }

            camera.transform.position = center + direction * 8f;
            camera.transform.LookAt(center, target.up);
            camera.orthographicSize = orthographicSize;
        }

        private static void ComposeRows(
            IReadOnlyList<List<byte[]>> rows,
            string outputPath)
        {
            if (rows.Count == 0 || rows.Any(row => row.Count != rows[0].Count))
            {
                throw new InvalidOperationException(
                    "Crouch comparison rows have inconsistent frame counts.");
            }

            int columns = rows[0].Count;
            Texture2D composite = new Texture2D(
                CaptureWidth * columns,
                CaptureHeight * rows.Count,
                TextureFormat.RGB24,
                false);
            List<Texture2D> panels = new List<Texture2D>();
            try
            {
                for (int row = 0; row < rows.Count; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        Texture2D panel = new Texture2D(
                            CaptureWidth,
                            CaptureHeight,
                            TextureFormat.RGB24,
                            false);
                        if (!panel.LoadImage(rows[row][column]))
                        {
                            throw new InvalidOperationException(
                                "Crouch comparison frame could not be decoded.");
                        }

                        panels.Add(panel);
                        composite.SetPixels(
                            column * CaptureWidth,
                            (rows.Count - row - 1) * CaptureHeight,
                            CaptureWidth,
                            CaptureHeight,
                            panel.GetPixels());
                    }
                }

                composite.Apply(false, false);
                string absoluteOutput = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absoluteOutput) ??
                    throw new InvalidOperationException(
                        "Crouch comparison output directory is unavailable."));
                File.WriteAllBytes(absoluteOutput, composite.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D panel in panels)
                {
                    UnityEngine.Object.DestroyImmediate(panel);
                }

                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static bool AnimatorMatches(
            Animator animator,
            RuntimeAnimatorController controller)
        {
            return animator != null &&
                   animator.runtimeAnimatorController == controller &&
                   !animator.applyRootMotion &&
                   animator.cullingMode == AnimatorCullingMode.AlwaysAnimate &&
                   animator.updateMode == AnimatorUpdateMode.Normal;
        }

        private static Animator RequireAnimator(Transform target)
        {
            return target.GetComponent<Animator>() ??
                   throw new InvalidOperationException(
                       target.name + " Animator is missing.");
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active loaded scene.");
            }

            return scene;
        }

        private static Transform RequireLayout(Scene scene)
        {
            GameObject layout = scene.GetRootGameObjects()
                .SingleOrDefault(root =>
                    string.Equals(root.name, LayoutRootName, StringComparison.Ordinal));
            return layout != null
                ? layout.transform
                : throw new InvalidOperationException(
                    LayoutRootName + " root is missing from CargoRunMvp.");
        }

        private static Transform RequireTarget(Transform layout, string name)
        {
            Transform[] matches = layout.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(item.name, name, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    name + " target count differs; actual=" + matches.Length + ".");
            }

            return matches[0];
        }

        private static Dictionary<string, string> CaptureOtherAnimatorStates(Transform layout)
        {
            return layout.GetComponentsInChildren<Animator>(true)
                .Where(animator =>
                    !string.Equals(animator.name, DiagonalTargetName, StringComparison.Ordinal) &&
                    !string.Equals(animator.name, ExitTargetName, StringComparison.Ordinal))
                .ToDictionary(
                    animator => AnimationUtility.CalculateTransformPath(
                        animator.transform,
                        layout),
                    animator => string.Join(
                        "|",
                        animator.enabled,
                        animator.applyRootMotion,
                        animator.cullingMode,
                        animator.updateMode,
                        AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)),
                    StringComparer.Ordinal);
        }

        private static Dictionary<string, string> CaptureAnimatorsExceptExit(Transform layout)
        {
            return layout.GetComponentsInChildren<Animator>(true)
                .Where(animator =>
                    !string.Equals(animator.name, ExitTargetName, StringComparison.Ordinal))
                .ToDictionary(
                    animator => AnimationUtility.CalculateTransformPath(
                        animator.transform,
                        layout),
                    animator => string.Join(
                        "|",
                        animator.enabled,
                        animator.applyRootMotion,
                        animator.cullingMode,
                        animator.updateMode,
                        AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)),
                    StringComparer.Ordinal);
        }

        private static bool DictionariesEqual(
            IReadOnlyDictionary<string, string> expected,
            IReadOnlyDictionary<string, string> actual)
        {
            return expected.Count == actual.Count && expected.All(item =>
                actual.TryGetValue(item.Key, out string value) &&
                string.Equals(item.Value, value, StringComparison.Ordinal));
        }

        private static bool RootMatches(Transform target, RootPose expected)
        {
            return Vector3.Distance(target.localPosition, expected.LocalPosition) <= PositionTolerance &&
                   Quaternion.Angle(target.localRotation, expected.LocalRotation) <= RotationTolerance &&
                   Vector3.Distance(target.localScale, expected.LocalScale) <= PositionTolerance;
        }

        private static string HashFile(string path)
        {
            string absolute = Path.GetFullPath(path);
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolute))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static void WriteJson<T>(string path, T value)
        {
            string absolute = Path.GetFullPath(path);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Crouch metrics directory is unavailable."));
            File.WriteAllText(
                absolute,
                JsonUtility.ToJson(value, true),
                new UTF8Encoding(false));
        }

        private static T ReadJson<T>(string path) where T : class
        {
            string absolute = Path.GetFullPath(path);
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Required crouch metrics file is missing.",
                    absolute);
            }

            T result = JsonUtility.FromJson<T>(File.ReadAllText(absolute, Encoding.UTF8));
            return result ?? throw new InvalidOperationException(
                "Crouch metrics file could not be decoded: " + absolute);
        }

        private static void CopyReviewedContact(string source, string destination)
        {
            string absoluteSource = Path.GetFullPath(source);
            string absoluteDestination = Path.GetFullPath(destination);
            if (!File.Exists(absoluteSource))
            {
                throw new FileNotFoundException(
                    "Reviewed crouch contact sheet is missing.",
                    absoluteSource);
            }

            Directory.CreateDirectory(
                Path.GetDirectoryName(absoluteDestination) ??
                throw new InvalidOperationException(
                    "Crouch final output directory is unavailable."));
            File.Copy(absoluteSource, absoluteDestination, true);
        }

        private static string Num(float value)
        {
            return value.ToString("0.#########", CultureInfo.InvariantCulture);
        }
    }
}
