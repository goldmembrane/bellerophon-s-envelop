using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Bellerophon.Editor.Build;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class UnityEditorValidationBridge
    {
        private const string RequestFileName = "UnityEditorBridge.request";
        private const string ActiveRequestFileName = "UnityEditorBridge.active";
        private const string DefaultTestResultsFileName = "TestResults.xml";
        private const string TergoPierceAttackCurrentSceneVisualRunCommand = "RunTergoPierceAttackCurrentSceneVisualRun";
        private const string Dolore04TentacleStabDiagnosticCommand = "CaptureDolore04TentacleStabFullMotionDiagnostic";
        private const string Dolore04TentacleStabFinalCommand = "CaptureDolore04TentacleStabFullMotionFinal";
        private const string Ispant08ContinuousMotionCaptureCommand =
            "CaptureIspant08ContinuousMotionTwoLoops";
        private const string AtaPistolTriggerFollowCaptureCommand =
            "CaptureAtaPistolTriggerFollowTwoLoops";
        private const string AtaCommandStanceAlternationCaptureCommand =
            "CaptureAtaCommandStanceAlternationThreeLoops";
        private const string StickAttackForwardAttackingCorrectionsPlayModeCommand =
            "CaptureStickAttackForwardAttackingCorrectionsPlayModeVideo";
        private const string StickAttackForwardAttackingCorrectionsFinalCommand =
            "CaptureStickAttackForwardAttackingCorrectionsFinal";
        private const string StickAttackForwardLeftPalmRightPlayModeCommand =
            "CaptureStickAttackForwardLeftHandPalmContactPlayMode";
        private const string StickAttackForwardGifWeaponPlayModeCommand =
            "CaptureStickAttackForwardGifWeaponMotionPlayMode";
        private const string StickThrowReadyReleaseCancelPlayModeCommand =
            "CaptureStickThrowReadyReleaseCancelPlayMode";
        private const string MusketBackCarryPlayModeCommand =
            "CaptureMusketBackCarryModelsPlayMode";
        private const string StickThrowReleasePhysicsArcPlayModeCommand =
            "CaptureStickThrowReleasePhysicsArcPlayMode";
        private const string Dolore05ExecutionOpeningDiagnosticCommand = "CaptureDolore05ExecutionOpeningDiagnostic";
        private const string RebellionMoveVisualReviewCommand =
            "CaptureRebellionMoveVisualReview";
        private const string RebellionFrontArtifactVisualReviewCommand =
            "CaptureRebellionFrontArtifactReview";
        private const string RebellionAttackTransitionVisualReviewCommand =
            "CaptureRebellionAttackModeTransitionReview";
        private const string RebellionForwardScanVisualReviewCommand =
            "CaptureRebellionForwardScanReview";
        private const string RebellionForwardBurstVisualReviewCommand =
            "CaptureRebellionForwardBurstFireReview";
        private const string RebellionHitReactionVisualReviewCommand =
            "CaptureRebellionHitReactionReview";
        private const string RebellionDeathVisualReviewCommand =
            "CaptureRebellionDeathReview";
        private const string NegatifIdleEyeEmissionVisualReviewCommand =
            "CaptureNegatifIdleEyeEmissionVisualReview";
        private const string NegatifMoveVisualReviewCommand =
            "CaptureNegatifMoveVisualReview";
        private const string NegatifClawAttackVisualReviewCommand =
            "CaptureNegatifClawAttackVisualReview";
        private const string NegatifHitReactionVisualReviewCommand =
            "CaptureNegatifHitReactionVisualReview";
        private const string NegatifFleeVisualReviewCommand =
            "CaptureNegatifFleeVisualReview";
        private const string NegatifDeathVisualReviewCommand =
            "CaptureNegatifDeathVisualReview";
        private const double PollIntervalSeconds = 0.5d;
        private const double TestRunnerStaleTimeoutSeconds = 120d;

        private static double nextPollTime;
        private static double activeRequestStartTime;
        private static bool isRunning;
        private static BridgeRequest activeRequest;
        private static StringBuilder activeLog;
        private static TestRunnerApi activeTestRunnerApi;
        private static TestRunCallbacks activeTestRunCallbacks;
        private static string activeTestRunGuid;

        static UnityEditorValidationBridge()
        {
            EditorApplication.update += PollForRequest;
        }

        [MenuItem("Bellerophon/Validation/Run Harness Validation")]
        private static void RunHarnessValidationFromMenu()
        {
            RunSynchronous(
                BridgeRequest.Manual("HarnessValidation", DefaultLogPath("HarnessValidation.log")),
                HarnessValidation.Run,
                "Harness validation passed.");
        }

        private static void PollForRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < nextPollTime)
            {
                return;
            }

            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            if (TryCompleteRecoveredPlayModeRequest())
            {
                return;
            }

            if (isRunning)
            {
                if (TryFailStaleTestRunnerRequest())
                {
                    return;
                }

                return;
            }

            if (TryResumeActivePlayModeRequest())
            {
                return;
            }

            var requestPath = Path.Combine(ProjectRoot, "Logs", RequestFileName);
            if (!File.Exists(requestPath))
            {
                return;
            }

            var request = BridgeRequest.Read(requestPath);
            if (!request.IsValid)
            {
                return;
            }

            if (ShouldWaitForEditModeBeforeStarting(request))
            {
                return;
            }

            TryDelete(requestPath);
            StartRequest(request);
        }

        private static bool ShouldWaitForEditModeBeforeStarting(BridgeRequest request)
        {
            if (request.Command != "PlayModeTests" || !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            return true;
        }

        private static void StartRequest(BridgeRequest request)
        {
            if (request.Command != "PlayModeTests" &&
                request.Command != TergoPierceAttackCurrentSceneVisualRunCommand &&
                request.Command != Dolore04TentacleStabDiagnosticCommand &&
                request.Command != Dolore04TentacleStabFinalCommand &&
                request.Command != Ispant08ContinuousMotionCaptureCommand &&
                request.Command != AtaPistolTriggerFollowCaptureCommand &&
                request.Command != AtaCommandStanceAlternationCaptureCommand &&
                request.Command != StickAttackForwardAttackingCorrectionsPlayModeCommand &&
                request.Command != StickAttackForwardLeftPalmRightPlayModeCommand &&
                request.Command != StickAttackForwardGifWeaponPlayModeCommand &&
                request.Command != StickThrowReadyReleaseCancelPlayModeCommand &&
                request.Command != StickThrowReleasePhysicsArcPlayModeCommand &&
                request.Command != Dolore05ExecutionOpeningDiagnosticCommand &&
                request.Command != RebellionAttackTransitionVisualReviewCommand &&
                request.Command != RebellionForwardScanVisualReviewCommand &&
                request.Command != RebellionForwardBurstVisualReviewCommand &&
                request.Command != RebellionHitReactionVisualReviewCommand &&
                request.Command != RebellionDeathVisualReviewCommand &&
                request.Command != NegatifIdleEyeEmissionVisualReviewCommand &&
                request.Command != NegatifMoveVisualReviewCommand &&
                request.Command != NegatifClawAttackVisualReviewCommand &&
                request.Command != NegatifHitReactionVisualReviewCommand &&
                request.Command != NegatifFleeVisualReviewCommand &&
                request.Command != NegatifDeathVisualReviewCommand)
            {
                TryDelete(ActiveRequestPath);
            }

            switch (request.Command)
            {
                case "HarnessValidation":
                    RunSynchronous(request, HarnessValidation.Run, "Harness validation passed.");
                    break;
                case "EditModeTests":
                    RunTests(request, TestMode.EditMode);
                    break;
                case "PlayModeTests":
                    RunTests(request, TestMode.PlayMode);
                    break;
                case "WindowsDevBuild":
                    RunSynchronous(
                        request,
                        () => BuildCli.BuildWindows64(request.OutputPath, request.DevelopmentBuild),
                        "Build Finished, Result: Success");
                    break;
                case "RefreshAssets":
                    RunSynchronous(
                        request,
                        RefreshAssets,
                        "Unity assets refreshed.");
                    break;
                case "ArrangePlayerAnimationLayout":
                    RunSynchronous(
                        request,
                        PlayerAnimationLayoutTool.Arrange,
                        "Player animation layout arranged.");
                    break;
                case "CapturePlayerAnimationLayout":
                    RunSynchronous(
                        request,
                        () => PlayerAnimationLayoutTool.Capture(request.OutputPath),
                        "Player animation layout captured.");
                    break;
                case "ApplyPlayerAnimationMaterials":
                    RunSynchronous(
                        request,
                        PlayerAnimationLayoutTool.ApplyMaterials,
                        "Player animation materials applied.");
                    break;
                case "CapturePlayerAnimationMaterials":
                    RunSynchronous(
                        request,
                        () => PlayerAnimationLayoutTool.CaptureMaterials(request.OutputPath),
                        "Player animation materials captured.");
                    break;
                case "ApplyPlayerIdleAnimation":
                    RunSynchronous(
                        request,
                        PlayerAnimationLayoutTool.ApplyIdleAnimation,
                        "Player idle animation applied.");
                    break;
                case "CapturePlayerIdleAnimation":
                    RunSynchronous(
                        request,
                        () => PlayerAnimationLayoutTool.CaptureIdleAnimation(request.OutputPath),
                        "Player idle animation captured.");
                    break;
                case "ApplyPlayerWalkForwardAnimation":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardAnimationTool.Apply,
                        "Player walk forward animation applied.");
                    break;
                case "ApplyPlayerWalkForwardReferenceMatch":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardAnimationTool.ApplyReferenceMatch,
                        "Player walk forward reference match applied.");
                    break;
                case "ApplyPlayerWalkForwardMeshyWalking":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardAnimationTool.ApplyMeshyWalking,
                        "Player walk forward Meshy walking applied.");
                    break;
                case "InspectPlayerEmbeddedAnimationClips":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardAnimationTool.InspectEmbeddedSourceClips,
                        "Player embedded animation clips inspected.");
                    break;
                case "CapturePlayerWalkForwardAnimation":
                    RunSynchronous(
                        request,
                        () => PlayerWalkForwardAnimationTool.Capture(request.OutputPath),
                        "Player walk forward animation captured.");
                    break;
                case "CapturePlayerWalkForwardReferenceMatchReview":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardPlayModeReview.CaptureReferenceMatchReview,
                        "Player walk forward reference match review advanced.");
                    break;
                case "CapturePlayerWalkForwardReferenceMatchFinal":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardAnimationTool.CaptureReferenceMatchFinal,
                        "Player walk forward reference match final captured.");
                    break;
                case "CapturePlayerWalkForwardMeshyWalkingReview":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardPlayModeReview.CaptureMeshyWalkingReview,
                        "Player walk forward Meshy walking review advanced.");
                    break;
                case "CapturePlayerWalkForwardMeshyWalkingFinal":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardAnimationTool.CaptureMeshyWalkingFinal,
                        "Player walk forward Meshy walking final captured.");
                    break;
                case "ApplyPlayerWalkBackwardMixamo":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardAnimationTool.Apply,
                        "Player walk backward Mixamo animation applied.");
                    break;
                case "CapturePlayerWalkBackwardMixamoReview":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardPlayModeReview.CaptureReview,
                        "Player walk backward Mixamo review advanced.");
                    break;
                case "CapturePlayerWalkBackwardMixamoFinal":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardAnimationTool.CaptureFinal,
                        "Player walk backward Mixamo final captured.");
                    break;
                case "ApplyPlayerWalkBackwardMeshyDirect":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardAnimationTool.ApplyMeshyDirect,
                        "Player walk backward direct Meshy animation applied.");
                    break;
                case "CapturePlayerWalkBackwardMeshyDirectReview":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardPlayModeReview.CaptureReview,
                        "Player walk backward direct Meshy review advanced.");
                    break;
                case "CapturePlayerWalkBackwardMeshyDirectFinal":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardAnimationTool.CaptureMeshyDirectFinal,
                        "Player walk backward direct Meshy final captured.");
                    break;
                case "ApplyPlayerWalkBackwardMeshyInPlace":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardAnimationTool.ApplyMeshyInPlace,
                        "Player walk backward Meshy in-place animation applied.");
                    break;
                case "CapturePlayerWalkBackwardMeshyInPlaceReview":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardPlayModeReview.CaptureReview,
                        "Player walk backward Meshy in-place review advanced.");
                    break;
                case "CapturePlayerWalkBackwardMeshyInPlaceFinal":
                    RunSynchronous(
                        request,
                        PlayerWalkBackwardAnimationTool.CaptureMeshyInPlaceFinal,
                        "Player walk backward Meshy in-place final captured.");
                    break;
                case "ApplyPlayerSidestepMixamoInPlace":
                    RunSynchronous(
                        request,
                        PlayerSidestepAnimationTool.Apply,
                        "Player sidestep exact Mixamo in-place animation applied.");
                    break;
                case "CapturePlayerSidestepMixamoInPlaceReview":
                    RunSynchronous(
                        request,
                        PlayerSidestepPlayModeReview.CaptureReview,
                        "Player sidestep exact Mixamo in-place review advanced.");
                    break;
                case "CapturePlayerSidestepMixamoInPlaceFinal":
                    RunSynchronous(
                        request,
                        PlayerSidestepAnimationTool.CaptureFinal,
                        "Player sidestep exact Mixamo in-place final captured.");
                    break;
                case "ApplyPlayerWalkDiagonalForwardBlend":
                    RunSynchronous(
                        request,
                        PlayerWalkDiagonalBlendTreeTool.Apply,
                        "Player walk diagonal forward 50:50 Blend Tree applied.");
                    break;
                case "CapturePlayerWalkDiagonalForwardBlendReview":
                    RunSynchronous(
                        request,
                        PlayerWalkDiagonalPlayModeReview.CaptureReview,
                        "Player walk diagonal forward Blend Tree review advanced.");
                    break;
                case "CapturePlayerWalkDiagonalForwardBlendFinal":
                    RunSynchronous(
                        request,
                        PlayerWalkDiagonalBlendTreeTool.CaptureFinal,
                        "Player walk diagonal forward Blend Tree final captured.");
                    break;
                case "ApplyPlayerRunForwardEmbeddedAnimation":
                    RunSynchronous(
                        request,
                        PlayerRunForwardAnimationTool.Apply,
                        "Player run forward embedded animation applied.");
                    break;
                case "CapturePlayerRunForwardReview":
                    RunSynchronous(
                        request,
                        PlayerRunForwardPlayModeReview.CaptureReview,
                        "Player run forward Mixamo in-place review advanced.");
                    break;
                case "CapturePlayerRunForwardFinal":
                    RunSynchronous(
                        request,
                        PlayerRunForwardPlayModeReview.CaptureFinal,
                        "Player run forward final captured.");
                    break;
                case "ApplyPlayerJumpEmbeddedAnimation":
                    RunSynchronous(
                        request,
                        PlayerJumpAnimationTool.Apply,
                        "Player jump embedded animation applied.");
                    break;
                case "CapturePlayerJumpReview":
                    RunSynchronous(
                        request,
                        PlayerJumpPlayModeReview.CaptureReview,
                        "Player jump direct two-loop review advanced.");
                    break;
                case "CapturePlayerJumpFinal":
                    RunSynchronous(
                        request,
                        PlayerJumpPlayModeReview.CaptureFinal,
                        "Player jump final captured.");
                    break;
                case "ApplyPlayerCrouchEnterSourceAnimation":
                    RunSynchronous(
                        request,
                        PlayerCrouchEnterAnimationTool.ApplySource,
                        "Player crouch enter source animation applied.");
                    break;
                case "CapturePlayerCrouchEnterSourceReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchEnterPlayModeReview.CaptureSourceReview,
                        "Player crouch enter source review advanced.");
                    break;
                case "ApplyPlayerCrouchEnterCorrection":
                    RunSynchronous(
                        request,
                        PlayerCrouchEnterAnimationTool.ApplyCorrection,
                        "Player crouch enter left-leg correction applied.");
                    break;
                case "ApplyPlayerCrouchEnterHalfSecondHold":
                    RunSynchronous(
                        request,
                        PlayerCrouchEnterAnimationTool.ApplyHalfSecondHold,
                        "Player crouch enter half-second hold applied.");
                    break;
                case "CapturePlayerCrouchEnterCorrectedReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchEnterPlayModeReview.CaptureCorrectedReview,
                        "Player crouch enter corrected review advanced.");
                    break;
                case "CapturePlayerCrouchEnterHoldReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchEnterPlayModeReview.CaptureHoldReview,
                        "Player crouch enter half-second hold review advanced.");
                    break;
                case "CapturePlayerCrouchEnterFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchEnterPlayModeReview.CaptureFinal,
                        "Player crouch enter final captured.");
                    break;
                case "ApplyPlayerCrouchIdleFromEnter":
                    RunSynchronous(
                        request,
                        PlayerCrouchIdleForwardAnimationTool.ApplyIdleFromEnter,
                        "Player crouch idle copied from the Enter final hold.");
                    break;
                case "CapturePlayerCrouchIdleReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchIdleForwardPlayModeReview.CaptureIdleReview,
                        "Player crouch idle review advanced.");
                    break;
                case "CapturePlayerCrouchIdleFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchIdleForwardPlayModeReview.CaptureIdleFinal,
                        "Player crouch idle final captured.");
                    break;
                case "ApplyPlayerCrouchForwardMixamoInPlace":
                    RunSynchronous(
                        request,
                        PlayerCrouchIdleForwardAnimationTool.ApplyForwardMixamoInPlace,
                        "Player crouch forward exact Mixamo in-place animation applied.");
                    break;
                case "CapturePlayerCrouchForwardReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchIdleForwardPlayModeReview.CaptureForwardReview,
                        "Player crouch forward review advanced.");
                    break;
                case "CapturePlayerCrouchForwardFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchIdleForwardPlayModeReview.CaptureForwardFinal,
                        "Player crouch forward final captured.");
                    break;
                case "ApplyPlayerCrouchPoseAlignment":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentTool.ApplyQuaternionAlignment,
                        "Player crouch pose alignment applied.");
                    break;
                case "ApplyPlayerCrouchForwardArmReach":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentTool.ApplyForwardArmReach,
                        "Player crouch forward arm reach applied.");
                    break;
                case "ApplyPlayerCrouchForwardUpperBodyAndRightArmCorrection":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentTool
                            .ApplyForwardUpperBodyAndRightArmCorrection,
                        "Player crouch forward upper body and right arm corrected.");
                    break;
                case "ApplyPlayerCrouchForwardLeftArmAndHeadCorrection":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentTool
                            .ApplyForwardLeftArmAndHeadCorrection,
                        "Player crouch forward left arm and crouch heads corrected.");
                    break;
                case "ApplyPlayerCrouchForwardLeftArmStraightDown":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentTool
                            .ApplyForwardLeftArmStraightDown,
                        "Player crouch forward left arm straightened downward.");
                    break;
                case "ApplyPlayerCrouchBackwardAndSidestepAnimations":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepAnimationTool.Apply,
                        "Player crouch backward and sidestep exact Mixamo in-place animations applied.");
                    break;
                case "ApplyPlayerCrouchDiagonalAndExit":
                    RunSynchronous(
                        request,
                        PlayerCrouchDiagonalExitTools.Apply,
                        "Player crouch diagonal Blend Tree and exact Mixamo exit applied.");
                    break;
                case "CapturePlayerCrouchDiagonalAndExitReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchDiagonalExitTools.CaptureReview,
                        "Player crouch diagonal and exit review advanced.");
                    break;
                case "CapturePlayerCrouchDiagonalAndExitFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchDiagonalExitTools.CaptureFinal,
                        "Player crouch diagonal and exit final images captured.");
                    break;
                case "ApplyPlayerCrouchExitIdleTransition":
                    RunSynchronous(
                        request,
                        PlayerCrouchDiagonalExitTools.ApplyExitIdleTransition,
                        "Player crouch exit transition to Idle first-frame pose applied.");
                    break;
                case "CapturePlayerCrouchExitIdleTransitionReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchDiagonalExitTools.CaptureExitIdleTransitionReview,
                        "Player crouch exit transition to Idle review advanced.");
                    break;
                case "CapturePlayerCrouchExitIdleTransitionFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchDiagonalExitTools.CaptureExitIdleTransitionFinal,
                        "Player crouch exit transition to Idle final image captured.");
                    break;
                case "ApplyPlayerHandsObjectAnimations":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.Apply,
                        "Player Hands and Objects exact source animations applied.");
                    break;
                case "ApplyPlayerStickCarryStartView":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerStickCarryStartView,
                        "Player start moved to the existing front framing for Stick_Carry.");
                    break;
                case "CapturePlayerStickCarryStartViewReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerStickCarryStartViewReview,
                        "Player Stick_Carry start-view review advanced.");
                    break;
                case "ApplyMusketDrawStartView":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyMusketDrawStartView,
                        "Player start moved to the Stick_Carry-equivalent front framing for Musket_Draw.");
                    break;
                case "CaptureMusketDrawStartViewPlayMode":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureMusketDrawStartViewPlayMode,
                        "Player Musket_Draw start-view Play Mode review advanced.");
                    break;
                case "CaptureMusketDrawStartViewFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureMusketDrawStartViewFinal,
                        "Player Musket_Draw start-view final image copied from the reviewed Play Mode frame.");
                    break;
                case "ApplyMusketBackCarryModels":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyMusketBackCarryModels,
                        "Exact musket FBX attached diagonally to all eight Musket target Spine bones.");
                    break;
                case MusketBackCarryPlayModeCommand:
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureMusketBackCarryModelsPlayMode,
                        "Musket back-carry Play Mode review advanced.");
                    break;
                case "CaptureMusketBackCarryModelsFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureMusketBackCarryModelsFinal,
                        "Musket back-carry overview finalized from directly reviewed Play Mode frames.");
                    break;
                case "ApplyMusketAnimationSet":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyMusketAnimationSet,
                        "Eight exact embedded Musket Mixamo Takes and item-state bindings applied.");
                    break;
                case "CaptureMusketAnimationSetPlayMode":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureMusketAnimationSetPlayMode,
                        "Musket animation set actual Play Mode review advanced.");
                    break;
                case "CaptureMusketAnimationSetFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureMusketAnimationSetFinal,
                        "Musket animation set final overview copied from directly reviewed Play Mode frames.");
                    break;
                case "ApplyStickCarryOneHandAnimationAndStickGrip":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickCarryOneHandAnimationAndStickGrip,
                        "Stick_Carry copied OneHand animation and lower-end right-hand stick grip applied.");
                    break;
                case "CaptureStickCarryCurrentRightHandLocalTransform":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickCarryCurrentRightHandLocalTransform,
                        "Stick_Carry current RightHand-local stick transform captured without modification.");
                    break;
                case "CaptureStickCarryGripDiagnostic":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickCarryGripDiagnostic,
                        "Stick_Carry weighted-hand grip diagnostic image captured.");
                    break;
                case "CaptureStickCarryOneHandAnimationAndStickGrip":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickCarryOneHandAnimationAndStickGrip,
                        "Stick_Carry OneHand animation and stick grip final image captured.");
                    break;
                case "ApplyStickGripTwoHandSequence":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickGripTwoHandSequence,
                        "Stick_Grip_TwoHand two-second carry, transition, and exact Mixamo sequence applied.");
                    break;
                case "CaptureStickGripTwoHandSequenceReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickGripTwoHandSequenceReview,
                        "Stick_Grip_TwoHand direct review contact sheet captured.");
                    break;
                case "CaptureStickGripTwoHandSequenceFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickGripTwoHandSequenceFinal,
                        "Stick_Grip_TwoHand reviewed contact sheet finalized after support checks.");
                    break;
                case "ApplyStickAttackForwardAndGripOneHand":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickAttackForwardAndGripOneHand,
                        "Stick_Attack_Forward exact Mixamo loop and Stick_Grip_OneHand exact reversed two-second sequence applied.");
                    break;
                case "CaptureStickAttackForwardAndGripOneHandReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardAndGripOneHandReview,
                        "Stick attack and reverse sequence direct review contact sheet captured.");
                    break;
                case "CaptureStickAttackForwardAndGripOneHandFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardAndGripOneHandFinal,
                        "Stick attack and reverse sequence reviewed contact sheet finalized after support checks.");
                    break;
                case "ApplyStickAttackForwardTrimAndStickMotion":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickAttackForwardTrimAndStickMotion,
                        "Stick_Attack_Forward trimmed at the approved below-abdomen frame with right-hand-relative stick motion.");
                    break;
                case "CaptureStickAttackForwardTrimAndStickMotionReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardTrimAndStickMotionReview,
                        "Stick_Attack_Forward trim and stick motion direct review contact sheet captured.");
                    break;
                case "CaptureStickAttackForwardTrimAndStickMotionFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardTrimAndStickMotionFinal,
                        "Stick_Attack_Forward trim and stick motion reviewed contact sheet finalized after support checks.");
                    break;
                case "CaptureStickAttackForwardAttackingSourceDiagnostic":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardAttackingSourceDiagnostic,
                        "Stick_Attack_Forward attacking exact embedded source diagnostic captured.");
                    break;
                case "ApplyStickAttackForwardAttackingMixamoWithStickMotion":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickAttackForwardAttackingMixamoWithStickMotion,
                        "Stick_Attack_Forward attacking exact Mixamo body, dynamic stick transform, and final 0.5-second hold applied.");
                    break;
                case "CaptureStickAttackForwardAttackingReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardAttackingReview,
                        "Stick_Attack_Forward attacking direct review contact sheet captured.");
                    break;
                case "CaptureStickAttackForwardAttackingFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardAttackingFinal,
                        "Stick_Attack_Forward attacking reviewed contact sheet finalized after support checks.");
                    break;
                case "ApplyStickAttackForwardAttackingCorrections":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickAttackForwardAttackingCorrections,
                        "Stick_Attack_Forward attacking corrections applied.");
                    break;
                case StickAttackForwardAttackingCorrectionsPlayModeCommand:
                    RunStickAttackForwardAttackingCorrectionsPlayModeCapture(
                        request);
                    break;
                case StickAttackForwardAttackingCorrectionsFinalCommand:
                    RunStickAttackForwardAttackingCorrectionsFinal(request);
                    break;
                case "ApplyStickAttackForwardLeftHandPalmContact":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickAttackForwardLeftPalmRightRestore,
                        "Stick_Attack_Forward left hand moved to character-left for palm contact without crossing either arm or changing stick curves.");
                    break;
                case StickAttackForwardLeftPalmRightPlayModeCommand:
                    RunStickAttackForwardLeftPalmRightPlayModeCapture(
                        request);
                    break;
                case "CaptureStickAttackForwardLeftHandPalmContactFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardLeftPalmRightFinal,
                        "Stick_Attack_Forward left-hand palm-contact correction finalized without changing stick curves.");
                    break;
                case "ApplyStickAttackForwardGifWeaponMotion":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickAttackForwardGifWeaponMotion,
                        "Stick_Attack_Forward GIF weapon motion applied without changing its fixed timing.");
                    break;
                case StickAttackForwardGifWeaponPlayModeCommand:
                    RunStickAttackForwardGifWeaponPlayModeCapture(request);
                    break;
                case "CaptureStickAttackForwardGifWeaponMotionFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardGifWeaponMotionFinal,
                        "Stick_Attack_Forward GIF weapon motion finalized.");
                    break;
                case "ApplyStickThrowReadyReleaseCancel":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickThrowReadyReleaseCancel,
                        "Stick Throw Ready/Release/Cancel copied animations and exact stick behavior applied.");
                    break;
                case StickThrowReadyReleaseCancelPlayModeCommand:
                    RunStickThrowReadyReleaseCancelPlayModeCapture(request);
                    break;
                case "CaptureStickThrowReadyReleaseCancelFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickThrowReadyReleaseCancelFinal,
                        "Stick Throw Ready/Release/Cancel finalized after direct Play Mode review.");
                    break;
                case "ApplyStickThrowReleasePhysicsArc":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyStickThrowReleasePhysicsArc,
                        "Stick_Throw_Release physical parabola and velocity-following stick rotation applied without changing Ready/Cancel.");
                    break;
                case StickThrowReleasePhysicsArcPlayModeCommand:
                    RunStickThrowReadyReleaseCancelPlayModeCapture(request);
                    break;
                case "CaptureStickThrowReleasePhysicsArcFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureStickThrowReleasePhysicsArcFinal,
                        "Stick_Throw_Release physical parabola finalized after direct Play Mode review.");
                    break;
                case "ApplyPlayerHandsCarryOneHandEmbeddedTakeExact":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyCarryOneHandEmbeddedTakeExact,
                        "Player OneHand adjusted animation removed and exact embedded Take linked directly.");
                    break;
                case "CapturePlayerHandsCarryOneHandEmbeddedTakeExactReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureCarryOneHandEmbeddedTakeExactReview,
                        "Player OneHand exact embedded Take review advanced.");
                    break;
                case "CapturePlayerHandsCarryOneHandEmbeddedTakeExactFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureCarryOneHandEmbeddedTakeExactFinal,
                        "Player OneHand exact embedded Take final image captured.");
                    break;
                case "ApplyPlayerHandsCarryOneHandEmptyBodyPalmLeft":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyCarryOneHandEmptyBodyPalmLeft,
                        "Player OneHand Empty Idle body, separated left arm, and character-left actual palm applied.");
                    break;
                case "CapturePlayerHandsCarryOneHandEmptyBodyPalmLeftReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureCarryOneHandEmptyBodyPalmLeftReview,
                        "Player OneHand Empty-body palm-left review advanced.");
                    break;
                case "CapturePlayerHandsCarryOneHandEmptyBodyPalmLeftFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureCarryOneHandEmptyBodyPalmLeftFinal,
                        "Player OneHand Empty-body palm-left final image captured.");
                    break;
                case "ApplyPlayerHandsDrawAndStowBackExactTakes":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyHandsDrawAndStowBackExactTakes,
                        "Player Hands Draw/Stow Back exact embedded Takes applied.");
                    break;
                case "CapturePlayerHandsDrawAndStowBackExactReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureHandsDrawAndStowBackExactReview,
                        "Player Hands Draw/Stow Back exact Take review advanced.");
                    break;
                case "CapturePlayerHandsDrawAndStowBackExactFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureHandsDrawAndStowBackExactFinal,
                        "Player Hands Draw/Stow Back exact Take final images captured.");
                    break;
                case "ReconnectPlayerHandsDrawBackExactMixamo":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ReconnectPlayerHandsDrawBackExactMixamo,
                        "Player Hands Draw Back exact embedded Mixamo Take reconnected.");
                    break;
                case "CapturePlayerHandsDrawBackExactMixamoReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackExactMixamoReview,
                        "Player Hands Draw Back exact Mixamo review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackExactMixamoFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackExactMixamoFinal,
                        "Player Hands Draw Back exact Mixamo final image captured.");
                    break;
                case "ApplyPlayerHandsDrawBackCommonMesh":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerHandsDrawBackCommonMesh,
                        "Player Hands Draw Back reverted to the shared Hands Empty Idle player mesh.");
                    break;
                case "CapturePlayerHandsDrawBackCommonMeshReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackCommonMeshReview,
                        "Player Hands Draw Back common-mesh direct review advanced.");
                    break;
                case "ApplyPlayerHandsDrawBackCommonMeshForward":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerHandsDrawBackCommonMeshForward,
                        "Player Hands Draw Back forward extraction rebuilt on the shared player mesh.");
                    break;
                case "CapturePlayerHandsDrawBackCommonMeshForwardReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackCommonMeshForwardReview,
                        "Player Hands Draw Back common-mesh forward direct review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackCommonMeshForwardFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackCommonMeshForwardFinal,
                        "Player Hands Draw Back common-mesh forward final image captured.");
                    break;
                case "CapturePlayerHandsThrowSourceDiagnostic":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsThrowSourceDiagnostic,
                        "Player Hands Throw exact source all-frame diagnostic captured.");
                    break;
                case "ApplyPlayerHandsThrowMixamo":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerHandsThrowMixamo,
                        "Player Hands Throw Ready peak-hold and full Release Mixamo takes applied.");
                    break;
                case "CapturePlayerHandsThrowMixamoReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsThrowMixamoReview,
                        "Player Hands Throw Ready and Release direct review advanced.");
                    break;
                case "CapturePlayerHandsThrowMixamoFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsThrowMixamoFinal,
                        "Player Hands Throw Ready and Release final image captured.");
                    break;
                case "ApplyPlayerHandsThrowCancel":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerHandsThrowCancel,
                        "Player Hands Throw Cancel exact Ready reverse loop applied.");
                    break;
                case "CapturePlayerHandsThrowCancelReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsThrowCancelReview,
                        "Player Hands Throw Cancel direct review advanced.");
                    break;
                case "CapturePlayerHandsThrowCancelFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsThrowCancelFinal,
                        "Player Hands Throw Cancel final image captured.");
                    break;
                case "ApplyPlayerHandsDrawBackForwardAngle":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyHandsDrawBackForwardAngle,
                        "Player Hands Draw Back right-arm forward angle applied with timing preserved.");
                    break;
                case "CapturePlayerHandsDrawBackForwardAngleReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureHandsDrawBackForwardAngleReview,
                        "Player Hands Draw Back forward-angle review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackForwardAngleFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureHandsDrawBackForwardAngleFinal,
                        "Player Hands Draw Back forward-angle final image captured.");
                    break;
                case "ApplyPlayerHandsDrawBackLowPalmLeftPose":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyHandsDrawBackLowPalmLeftPose,
                        "Player Hands Draw Back solar-plexus-height, 30-degree elbow, palm-left pose applied with timing preserved.");
                    break;
                case "CapturePlayerHandsDrawBackLowPalmLeftPoseReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureHandsDrawBackLowPalmLeftPoseReview,
                        "Player Hands Draw Back low palm-left pose review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackLowPalmLeftPoseFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureHandsDrawBackLowPalmLeftPoseFinal,
                        "Player Hands Draw Back low palm-left pose final image captured.");
                    break;
                case "ApplyPlayerHandsDrawBackOuterElbowPath":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyHandsDrawBackOuterElbowPath,
                        "Player Hands Draw Back outward elbow extraction path applied with final pose and timing preserved.");
                    break;
                case "CapturePlayerHandsDrawBackOuterElbowPathReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureHandsDrawBackOuterElbowPathReview,
                        "Player Hands Draw Back outer-elbow path review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackOuterElbowPathFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureHandsDrawBackOuterElbowPathFinal,
                        "Player Hands Draw Back outer-elbow path final image captured.");
                    break;
                case "ApplyPlayerTransporterPurpleFlagDrawBackClearanceAndStart":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerTransporterPurpleFlagDrawBackClearanceAndStart,
                        "Shared transporter light-purple left-arm patch, Draw Back torso clearance, and Empty-facing start applied.");
                    break;
                case "CapturePlayerTransporterPurpleFlagDrawBackClearanceAndStartReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerTransporterPurpleFlagDrawBackClearanceAndStartReview,
                        "Transporter purple-flag, Draw Back clearance, and start-position review advanced.");
                    break;
                case "CapturePlayerTransporterPurpleFlagDrawBackClearanceAndStartFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerTransporterPurpleFlagDrawBackClearanceAndStartFinal,
                        "Transporter purple-flag, Draw Back clearance, and start-position final image captured.");
                    break;
                case "ApplyAllTransporterLeftArmFlagRectangleOpaque":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyAllTransporterLeftArmFlagRectangleOpaque,
                        "All transporter left-arm United States flag bounds filled with one opaque light-purple rectangle.");
                    break;
                case "CaptureAllTransporterLeftArmFlagRectangleOpaque":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CaptureAllTransporterLeftArmFlagRectangleOpaque,
                        "All transporter left-arm opaque rectangle final image captured.");
                    break;
                case "ApplyPlayerHandsDrawBackFrontSilhouetteClearance":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerHandsDrawBackFrontSilhouetteClearance,
                        "Player Hands Draw Back right arm moved outside the torso and face front silhouette with timing preserved.");
                    break;
                case "CapturePlayerHandsDrawBackFrontSilhouetteClearanceReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackFrontSilhouetteClearanceReview,
                        "Player Hands Draw Back front-silhouette clearance review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackFrontSilhouetteClearanceFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackFrontSilhouetteClearanceFinal,
                        "Player Hands Draw Back front-silhouette clearance final image captured.");
                    break;
                case "ApplyPlayerHandsDrawBackChestDeformationFix":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerHandsDrawBackChestDeformationFix,
                        "Player Hands Draw Back right-chest deformation reduced with arm clearance, final pose, and timing preserved.");
                    break;
                case "CapturePlayerHandsDrawBackChestDeformationFixReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackChestDeformationFixReview,
                        "Player Hands Draw Back right-chest deformation review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackChestDeformationFixFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackChestDeformationFixFinal,
                        "Player Hands Draw Back right-chest deformation final image captured.");
                    break;
                case "AnalyzePlayerHandsDrawBackRightChestDeformation":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .AnalyzePlayerHandsDrawBackRightChestDeformation,
                        "Player Hands Draw Back right-chest skinned-mesh deformation analyzed.");
                    break;
                case "ApplyPlayerHandsDrawBackRightChestCorrection":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerHandsDrawBackRightChestCorrection,
                        "Player Hands Draw Back state-only right-chest corrective BlendShape applied.");
                    break;
                case "CapturePlayerHandsDrawBackRightChestCorrectionReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackRightChestCorrectionReview,
                        "Player Hands Draw Back right-chest correction review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackRightChestCorrectionFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackRightChestCorrectionFinal,
                        "Player Hands Draw Back right-chest correction final image captured.");
                    break;
                case "AnalyzePlayerHandsDrawBackRightChestVideoReference":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .AnalyzePlayerHandsDrawBackRightChestVideoReference,
                        "Player Hands Draw Back video-reference right-chest deformation analyzed.");
                    break;
                case "ApplyPlayerHandsDrawBackRightChestVideoCorrection":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .ApplyPlayerHandsDrawBackRightChestVideoCorrection,
                        "Player Hands Draw Back video-reference right-chest correction applied.");
                    break;
                case "CapturePlayerHandsDrawBackRightChestVideoCorrectionReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackRightChestVideoCorrectionReview,
                        "Player Hands Draw Back video-reference right-chest review advanced.");
                    break;
                case "CapturePlayerHandsDrawBackRightChestVideoCorrectionFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools
                            .CapturePlayerHandsDrawBackRightChestVideoCorrectionFinal,
                        "Player Hands Draw Back video-reference right-chest final image captured.");
                    break;
                case "CapturePlayerHandsObjectAnimationsReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureReview,
                        "Player Hands and Objects review advanced.");
                    break;
                case "CapturePlayerHandsObjectAnimationsFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureFinal,
                        "Player Hands and Objects final images captured.");
                    break;
                case "ApplyPlayerHandsCarryBodyAlignment":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyCarryBodyAlignment,
                        "Player carry bodies aligned to Empty Idle with exact arm subtrees preserved.");
                    break;
                case "CapturePlayerHandsCarryBodyAlignmentReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryBodyAlignmentReview,
                        "Player carry body alignment review advanced.");
                    break;
                case "CapturePlayerHandsCarryBodyAlignmentFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryBodyAlignmentFinal,
                        "Player carry body alignment final images captured.");
                    break;
                case "ApplyPlayerHandsCarryPoseAdjustment":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyCarryPoseAdjustment,
                        "Player carry arm pose adjustments applied continuously.");
                    break;
                case "CapturePlayerHandsCarryPoseAdjustmentReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryPoseAdjustmentReview,
                        "Player carry arm pose adjustment review advanced.");
                    break;
                case "CapturePlayerHandsCarryPoseAdjustmentFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryPoseAdjustmentFinal,
                        "Player carry arm pose adjustment final images captured.");
                    break;
                case "ApplyPlayerHandsCarryOneHandGripClearance":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyCarryOneHandGripClearance,
                        "Player OneHand left arm clearance and vertical grip applied.");
                    break;
                case "CapturePlayerHandsCarryOneHandGripClearanceReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandGripClearanceReview,
                        "Player OneHand grip clearance review advanced.");
                    break;
                case "CapturePlayerHandsCarryOneHandGripClearanceFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandGripClearanceFinal,
                        "Player OneHand grip clearance final image captured.");
                    break;
                case "ApplyPlayerHandsCarryOneHandWristGripCorrection":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyCarryOneHandWristGripCorrection,
                        "Player OneHand wrist-only vertical grip correction applied.");
                    break;
                case "CapturePlayerHandsCarryOneHandWristGripCorrectionReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandWristGripCorrectionReview,
                        "Player OneHand wrist grip correction review advanced.");
                    break;
                case "CapturePlayerHandsCarryOneHandWristGripCorrectionFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandWristGripCorrectionFinal,
                        "Player OneHand wrist grip correction final image captured.");
                    break;
                case "ApplyPlayerHandsCarryOneHandWrist180Flip":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyCarryOneHandWrist180Flip,
                        "Player OneHand wrist-only 180-degree vertical-axis flip applied.");
                    break;
                case "CapturePlayerHandsCarryOneHandWrist180FlipReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandWrist180FlipReview,
                        "Player OneHand wrist 180-degree flip review advanced.");
                    break;
                case "CapturePlayerHandsCarryOneHandWrist180FlipFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandWrist180FlipFinal,
                        "Player OneHand wrist 180-degree flip final image captured.");
                    break;
                case "ApplyPlayerHandsCarryOneHandNaturalVerticalGrip":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyCarryOneHandNaturalVerticalGrip,
                        "Player OneHand natural right-arm vertical grip applied.");
                    break;
                case "CapturePlayerHandsCarryOneHandNaturalVerticalGripReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandNaturalVerticalGripReview,
                        "Player OneHand natural vertical grip review advanced.");
                    break;
                case "CapturePlayerHandsCarryOneHandNaturalVerticalGripFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandNaturalVerticalGripFinal,
                        "Player OneHand natural vertical grip final image captured.");
                    break;
                case "ApplyPlayerHandsCarryOneHandAnatomicalWristGrip":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyCarryOneHandAnatomicalWristGrip,
                        "Player OneHand anatomical wrist grip applied.");
                    break;
                case "CapturePlayerHandsCarryOneHandAnatomicalWristGripReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandAnatomicalWristGripReview,
                        "Player OneHand anatomical wrist grip review advanced.");
                    break;
                case "CapturePlayerHandsCarryOneHandAnatomicalWristGripFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandAnatomicalWristGripFinal,
                        "Player OneHand anatomical wrist grip final image captured.");
                    break;
                case "ApplyPlayerHandsCarryOneHandActualPalmInwardGrip":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.ApplyCarryOneHandActualPalmInwardGrip,
                        "Player OneHand actual-palm inward grip applied for direct review.");
                    break;
                case "CapturePlayerHandsCarryOneHandActualPalmInwardGripReview":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandActualPalmInwardGripReview,
                        "Player OneHand actual-palm inward direct review advanced.");
                    break;
                case "CapturePlayerHandsCarryOneHandActualPalmInwardGripFinal":
                    RunSynchronous(
                        request,
                        PlayerHandsObjectAnimationTools.CaptureCarryOneHandActualPalmInwardGripFinal,
                        "Player OneHand actual-palm inward final image captured.");
                    break;
                case "ApplyPlayerCrouchBackwardAndSidestepIdleArmAlignment":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepAnimationTool
                            .ApplyIdleArmAlignment,
                        "Player crouch backward and sidestep arms aligned to Idle with swing preserved.");
                    break;
                case "ApplyPlayerCrouchBackwardAndSidestepArmClearance":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepAnimationTool
                            .ApplyArmClearance,
                        "Player crouch backward and sidestep arms moved outward with swing preserved.");
                    break;
                case "ApplyPlayerCrouchMovingKneeSideArmPose":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepAnimationTool
                            .ApplyMovingKneeSideArmPose,
                        "Player crouch moving arms gathered beside same-side knees with swing preserved.");
                    break;
                case "CapturePlayerCrouchMovingKneeSideArmReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepPlayModeReview.CaptureReview,
                        "Player crouch moving knee-side arm review advanced.");
                    break;
                case "CapturePlayerCrouchMovingKneeSideArmFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepPlayModeReview.CaptureFinal,
                        "Player crouch moving knee-side arm final captured.");
                    break;
                case "ApplyPlayerCrouchBackwardAndSidestepLeftArmsStraightDown":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepAnimationTool
                            .ApplyLeftArmsStraightDown,
                        "Player crouch backward and sidestep left arms straightened downward.");
                    break;
                case "CapturePlayerCrouchBackwardAndSidestepLeftArmsStraightDownReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepPlayModeReview
                            .CaptureLeftArmsStraightDownReview,
                        "Player crouch backward and sidestep left-arm straight-down review advanced.");
                    break;
                case "CapturePlayerCrouchBackwardAndSidestepLeftArmsStraightDownFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepPlayModeReview
                            .CaptureLeftArmsStraightDownFinal,
                        "Player crouch backward and sidestep left-arm straight-down final captured.");
                    break;
                case "CapturePlayerCrouchBackwardAndSidestepReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepPlayModeReview.CaptureReview,
                        "Player crouch backward and sidestep review advanced.");
                    break;
                case "CapturePlayerCrouchBackwardAndSidestepFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchBackwardSidestepPlayModeReview.CaptureFinal,
                        "Player crouch backward and sidestep final captured.");
                    break;
                case "CapturePlayerCrouchPoseAlignmentReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentPlayModeReview.CaptureActualReview,
                        "Player crouch pose alignment review advanced.");
                    break;
                case "CapturePlayerCrouchPoseAlignmentFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentPlayModeReview.CaptureActualFinal,
                        "Player crouch pose alignment final captured.");
                    break;
                case "CapturePlayerCrouchForwardLeftArmStraightDownReview":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentPlayModeReview
                            .CaptureForwardLeftArmStraightDownReview,
                        "Player crouch forward left-arm straight-down review advanced.");
                    break;
                case "CapturePlayerCrouchForwardLeftArmStraightDownFinal":
                    RunSynchronous(
                        request,
                        PlayerCrouchPoseAlignmentPlayModeReview
                            .CaptureForwardLeftArmStraightDownFinal,
                        "Player crouch forward left-arm straight-down final captured.");
                    break;
                case "EnterPlayerWalkForwardReviewPlayMode":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardPlayModeReview.EnterPlayMode,
                        "Player walk forward review Play Mode requested.");
                    break;
                case "PreparePlayerWalkForwardReview":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardPlayModeReview.Prepare,
                        "Player walk forward Play Mode review prepared.");
                    break;
                case "FinishPlayerWalkForwardReview":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardPlayModeReview.Finish,
                        "Player walk forward Play Mode review finished.");
                    break;
                case "ExitPlayerWalkForwardReviewPlayMode":
                    RunSynchronous(
                        request,
                        PlayerWalkForwardPlayModeReview.ExitPlayMode,
                        "Player walk forward Play Mode exit requested.");
                    break;
                case "ApplyPlayerStartView":
                    RunSynchronous(
                        request,
                        PlayerAnimationLayoutTool.ApplyPlayerStartView,
                        "Player start view applied.");
                    break;
                case "EnterPlayerStartViewPlayMode":
                    RunSynchronous(
                        request,
                        PlayerAnimationLayoutTool.EnterPlayerStartViewPlayMode,
                        "Player start view Play Mode requested.");
                    break;
                case "CapturePlayerStartView":
                    RunSynchronous(
                        request,
                        () => PlayerAnimationLayoutTool.CapturePlayerStartView(request.OutputPath),
                        "Player start view captured.");
                    break;
                case "ExitPlayerStartViewPlayMode":
                    RunSynchronous(
                        request,
                        PlayerAnimationLayoutTool.ExitPlayerStartViewPlayMode,
                        "Player start view Play Mode exit requested.");
                    break;
                case "StartAtaModelPerformanceProbe":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaPerformance.AtaModelPerformanceProbe.Start,
                        "Ata model performance probe started.");
                    break;
                case "ApplyAtaNineSlotPlacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCargoRunScenePlacementTool.ApplyAtaNineSlotPlacement,
                        "Ata nine-slot placement applied.");
                    break;
                case "ApplyAtaEmbeddedTexture":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCargoRunScenePlacementTool.ApplyAtaEmbeddedTexture,
                        "Ata embedded texture applied.");
                    break;
                case "ApplyAtaFacingRotation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCargoRunScenePlacementTool.ApplyAtaFacingRotation,
                        "Ata facing rotation applied.");
                    break;
                case "CaptureAtaNineSlotPlacementDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCargoRunScenePlacementTool.CaptureAtaNineSlotPlacementDiagnostic,
                        "Ata nine-slot placement diagnostic captured.");
                    break;
                case "CaptureAtaNineSlotPlacementFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCargoRunScenePlacementTool.CaptureAtaNineSlotPlacementFinal,
                        "Ata nine-slot placement final captured.");
                    break;
                case "CaptureAtaEmbeddedTextureFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCargoRunScenePlacementTool.CaptureAtaEmbeddedTextureFinal,
                        "Ata embedded texture final captured.");
                    break;
                case "CaptureAtaFacingRotationFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCargoRunScenePlacementTool.CaptureAtaFacingRotationFinal,
                        "Ata facing rotation final captured.");
                    break;
                case "CaptureAtaFacingRotationDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCargoRunScenePlacementTool.CaptureAtaFacingRotationDiagnostic,
                        "Ata facing rotation diagnostic captured.");
                    break;
                case "ApplyAtaIdleAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaIdleAnimationTool.ApplyAtaIdleAnimation,
                        "Ata idle animation applied.");
                    break;
                case "CaptureAtaIdleAnimationDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaIdleAnimationTool.CaptureAtaIdleAnimationDiagnostic,
                        "Ata idle animation diagnostic captured.");
                    break;
                case "CaptureAtaIdleAnimationFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaIdleAnimationTool.CaptureAtaIdleAnimationFinal,
                        "Ata idle animation final captured.");
                    break;
                case "ApplyAtaMoveAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaMoveAnimationTool.ApplyAtaMoveAnimation,
                        "Ata move animation applied.");
                    break;
                case "CaptureAtaMoveAnimationDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaMoveAnimationTool.CaptureAtaMoveAnimationDiagnostic,
                        "Ata move animation diagnostic captured.");
                    break;
                case "CaptureAtaMoveAnimationFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaMoveAnimationTool.CaptureAtaMoveAnimationFinal,
                        "Ata move animation final captured.");
                    break;
                case "ApplyAtaCommandAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCommandAnimationTool.ApplyAtaCommandAnimation,
                        "Ata command animation applied.");
                    break;
                case "CaptureAtaCommandShieldReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaCommandAnimationTool.CaptureAtaCommandShieldReview,
                        "Ata command shield review captured.");
                    break;
                case "ApplyAtaSabotageAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaSabotageAnimationTool.ApplyAtaSabotageAnimation,
                        "Ata sabotage animation applied.");
                    break;
                case "CaptureAtaSabotageAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaSabotageAnimationTool.CaptureAtaSabotageAnimation,
                        "Ata sabotage animation captured.");
                    break;
                case "ApplyAtaSabotageProgressBar":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaSabotageAnimationTool.ApplyAtaSabotageProgressBar,
                        "Ata sabotage progress bar applied.");
                    break;
                case "CaptureAtaSabotageProgressBar":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaSabotageAnimationTool.CaptureAtaSabotageProgressBar,
                        "Ata sabotage progress bar captured.");
                    break;
                case "ApplyAtaSabotageRepeatingCycle":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaSabotageAnimationTool.ApplyAtaSabotageRepeatingCycle,
                        "Ata sabotage repeating cycle applied.");
                    break;
                case "CaptureAtaSabotageRepeatingCycle":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaSabotageAnimationTool.CaptureAtaSabotageRepeatingCycle,
                        "Ata sabotage repeating cycle captured.");
                    break;
                case "ApplyAtaBombInstallAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaBombInstallAnimationTool.ApplyAtaBombInstallAnimation,
                        "Ata bomb-install animation applied.");
                    break;
                case "CaptureAtaBombInstallAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaBombInstallAnimationTool.CaptureAtaBombInstallAnimation,
                        "Ata bomb-install animation captured.");
                    break;
                case "ApplyAtaBombInstallProgressBar":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaBombInstallAnimationTool.ApplyAtaBombInstallProgressBar,
                        "Ata bomb-install progress bar applied.");
                    break;
                case "CaptureAtaBombInstallProgressBar":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaBombInstallAnimationTool.CaptureAtaBombInstallProgressBar,
                        "Ata bomb-install progress bar captured.");
                    break;
                case "CaptureAtaBombInstallSourceAnalysis":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaBombInstallAnimationTool.CaptureAtaBombInstallSourceAnalysis,
                        "Ata bomb-install source motion analyzed.");
                    break;
                case "ApplyAtaBombInstallSeatedLoop":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaBombInstallAnimationTool.ApplyAtaBombInstallSeatedLoop,
                        "Ata bomb-install seated loop applied.");
                    break;
                case "CaptureAtaBombInstallSeatedLoop":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaBombInstallAnimationTool.CaptureAtaBombInstallSeatedLoop,
                        "Ata bomb-install seated loop captured.");
                    break;
                case "ApplyAtaHitAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaHitAnimationTool.ApplyAtaHitAnimation,
                        "Ata hit animation applied.");
                    break;
                case "CaptureAtaHitAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaHitAnimationTool.CaptureAtaHitAnimation,
                        "Ata hit animation captured.");
                    break;
                case "ApplyAtaHitStaticArms":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaHitAnimationTool.ApplyAtaHitStaticArms,
                        "Ata hit static arms applied.");
                    break;
                case "CaptureAtaHitStaticArms":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaHitAnimationTool.CaptureAtaHitStaticArms,
                        "Ata hit static arms captured.");
                    break;
                case "ApplyAtaDeathAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaDeathAnimationTool.ApplyAtaDeathAnimation,
                        "Ata death animation applied.");
                    break;
                case "CaptureAtaDeathAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaDeathAnimationTool.CaptureAtaDeathAnimation,
                        "Ata death animation captured.");
                    break;
                case "ApplyAtaDeathPreFallStaticArms":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaDeathAnimationTool.ApplyAtaDeathPreFallStaticArms,
                        "Ata death pre-fall static arms applied.");
                    break;
                case "CaptureAtaDeathPreFallStaticArms":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaDeathAnimationTool.CaptureAtaDeathPreFallStaticArms,
                        "Ata death pre-fall static arms captured.");
                    break;
                case "DiagnoseAtaBombInstallAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaBombInstallAnimationTool.DiagnoseAtaBombInstallAnimation,
                        "Ata bomb-install animation diagnosed.");
                    break;
                case "InspectAndFixAtaOtherSlotsRightArmMesh":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaOtherSlotsRightArmMeshTool.InspectAndFix,
                        "Ata other slots right-arm mesh inspected and corrected.");
                    break;
                case "CaptureAtaCurrentRightArmReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaOtherSlotsRightArmMeshTool.CaptureCurrentRightArmReview,
                        "Ata current right-arm review captured.");
                    break;
                case "ApplyAtaPistolAimFireAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.ApplyAtaPistolAimFireAnimation,
                        "Ata pistol aim and fire animation applied.");
                    break;
                case "RecoverAtaPistolInterruptedApply":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.RecoverAtaPistolInterruptedApply,
                        "Ata interrupted pistol apply recovered.");
                    break;
                case "InspectAtaPistolStructure":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.InspectAtaPistolStructure,
                        "Ata pistol structure inspected.");
                    break;
                case "InspectExtractedPistolTriangleGeometry":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.InspectExtractedPistolTriangleGeometry,
                        "Ata extracted pistol triangle geometry inspected.");
                    break;
                case "CaptureAtaPistolResidualComponents":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaPistolResidualComponents,
                        "Ata pistol residual components captured.");
                    break;
                case "CaptureAtaPistolWaistGeometryDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaPistolWaistGeometryDiagnostic,
                        "Ata pistol waist geometry diagnostic captured.");
                    break;
                case "CaptureAtaPistolRegionDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaPistolRegionDiagnostic,
                        "Ata pistol region diagnostic captured.");
                    break;
                case "CaptureAtaExtractedPistolGeometryDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaExtractedPistolGeometryDiagnostic,
                        "Extracted Ata pistol geometry diagnostic captured.");
                    break;
                case "CaptureAtaPistolAimFireAnimationDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaPistolAimFireAnimationDiagnostic,
                        "Ata pistol aim and fire animation diagnostic captured.");
                    break;
                case "CaptureAtaShootingSourceDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaShootingSourceDiagnostic,
                        "Ata shooting source diagnostic captured.");
                    break;
                case "InspectAtaShootingMotionTiming":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.InspectAtaShootingMotionTiming,
                        "Ata shooting motion timing inspected.");
                    break;
                case "CaptureAtaPistolAimFireAnimationFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaPistolAimFireAnimationFinal,
                        "Ata pistol aim and fire animation final captured.");
                    break;
                case "CaptureAtaPistolLeftSideFillReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaPistolLeftSideFillReview,
                        "Ata pistol left-side fill review captured.");
                    break;
                case "CaptureAtaPistolLeftSideFillIsolatedReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.AtaCargoRunScene.AtaPistolAimFireAnimationTool.CaptureAtaPistolLeftSideFillIsolatedReview,
                        "Ata isolated pistol left-side fill review captured.");
                    break;
                case "ApplyApprovedPahurAppearance":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurApprovedAppearanceApplicator.ApplyApprovedPahurAppearance,
                        "Approved Pahur appearance applied.");
                    break;
                case "ValidateApprovedPahurAppearance":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurApprovedAppearanceApplicator.ValidateApprovedPahurAppearance,
                        "Approved Pahur appearance validation passed.");
                    break;
                case "CaptureApprovedPahurAppearance":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurApprovedAppearanceApplicator.CaptureApprovedPahurAppearance,
                        "Approved Pahur appearance capture saved.");
                    break;
                case "ApplyPahurApprovedAppearanceSceneLightingParity":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurApprovedAppearanceApplicator.ApplyPahurApprovedAppearanceSceneLightingParity,
                        "Approved Pahur scene parity applied.");
                    break;
                case "InspectPahurApprovedAppearanceSceneLightingParity":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurApprovedAppearanceApplicator.InspectPahurApprovedAppearanceSceneLightingParity,
                        "Approved Pahur scene parity inspected.");
                    break;
                case "CapturePahurApprovedAppearanceActualSceneParity":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurApprovedAppearanceApplicator.CapturePahurApprovedAppearanceActualSceneParity,
                        "Approved Pahur actual-scene parity capture saved.");
                    break;
                case "ApplyPahurGroundedIdleAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurGroundedIdleAnimationTool.ApplyPahurGroundedIdleAnimation,
                        "Pahur grounded idle animation applied.");
                    break;
                case "ApplyKursaIdleAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaGroundedIdleAnimationTool.ApplyKursaIdleAnimation,
                        "Kursa grounded idle animation applied.");
                    break;
                case "InspectKursaIdleAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaGroundedIdleAnimationTool.InspectKursaIdleAnimation,
                        "Kursa grounded idle animation inspected.");
                    break;
                case "CaptureKursaIdleAnimationReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaGroundedIdleAnimationTool.CaptureKursaIdleAnimationReview,
                        "Kursa grounded idle animation review captured.");
                    break;
                case "ApplyIspantIdleAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantVerticalIdleAnimationTool.ApplyIspantIdleAnimation,
                        "Ispant idle animation applied.");
                    break;
                case "InspectIspantIdleAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantVerticalIdleAnimationTool.InspectIspantIdleAnimation,
                        "Ispant idle animation inspected.");
                    break;
                case "CaptureIspantIdleAnimationDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantVerticalIdleAnimationTool.CaptureIspantIdleAnimationDiagnostic,
                        "Ispant idle animation diagnostic captured.");
                    break;
                case "CaptureIspantIdleAnimationFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantVerticalIdleAnimationTool.CaptureIspantIdleAnimationFinalReview,
                        "Ispant idle animation final review captured.");
                    break;
                case "StartIspantIdleReviewPlayback":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantVerticalIdleAnimationTool.StartIspantIdleReviewPlayback,
                        "The live Unity Scene View review started for the newly authored looping Ispant vertical idle motion.");
                    break;
                case "StopIspantIdleReviewPlayback":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantVerticalIdleAnimationTool.StopIspantIdleReviewPlayback,
                        "The live Ispant vertical idle review completed at least two loops.");
                    break;
                case "InspectIspantEmbeddedMoveSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantEmbeddedMoveAnimationTool.InspectIspantEmbeddedMoveSource,
                        "The current direct Ispant FBX embedded clips and renderer separation were inspected without changing assets or the scene.");
                    break;
                case "InspectIspantNewWalkingSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewWalkingAnimationTool.InspectIspantNewWalkingSource,
                        "The supplied Ispant walking FBX, its Mixamo take, both humanoid Avatars, and the current move target were inspected without changing the scene.");
                    break;
                case "ApplyIspantNewWalkingAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewWalkingAnimationTool.ApplyIspantNewWalkingAnimation,
                        "The supplied Mixamo walking take was connected only to the current direct Ispant move object as an in-place loop.");
                    break;
                case "InspectIspantNewWalkingAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewWalkingAnimationTool.InspectIspantNewWalkingAnimation,
                        "The applied Ispant Mixamo walking loop, target isolation, and weapon follow were inspected without changing the scene.");
                    break;
                case "StartIspantNewWalkingReviewPlayback":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewWalkingAnimationTool.StartIspantNewWalkingReviewPlayback,
                        "The live Ispant Mixamo walking review started in Edit Mode.");
                    break;
                case "StopIspantNewWalkingReviewPlayback":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewWalkingAnimationTool.StopIspantNewWalkingReviewPlayback,
                        "The live Ispant Mixamo walking review completed multiple loops and restored the scene.");
                    break;
                case "ApplyIspantMoveModel":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantMoveAnimationTool.ApplyIspantMoveModel,
                        "The revised Ispant move model was applied only to Ispant_03_Move with exact static appearance, in-place walking, matched size, and rigid body-following weapons.");
                    break;
                case "ApplyIspantMoveRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantMoveAnimationTool.ApplyIspantMoveRevision,
                        "The revised Ispant move model was applied only to Ispant_03_Move with exact static appearance, in-place walking, matched size, and rigid body-following weapons.");
                    break;
                case "ApplyIspantMoveLeftArmClearance":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantMoveAnimationTool.ApplyIspantMoveLeftArmClearance,
                        "The Ispant move left arm was moved outward only on Ispant_03_Move while preserving the in-place walk and rigid body-following weapons.");
                    break;
                case "InspectIspantMoveLeftArmClearance":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantMoveAnimationTool.InspectIspantMoveLeftArmClearance,
                        "The Ispant move left-arm clearance was inspected across the complete walking cycle without changing the scene.");
                    break;
                case "CaptureIspantMoveReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantMoveAnimationTool.CaptureIspantMoveReview,
                        "A static-reference and five-phase revised Ispant in-place walking review was captured after size and rigid body-following weapon inspection passed.");
                    break;
                case "CaptureIspantMoveRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantMoveAnimationTool.CaptureIspantMoveRevisionReview,
                        "A static-reference and five-phase revised Ispant in-place walking review was captured after size and rigid body-following weapon inspection passed.");
                    break;
                case "CaptureIspantMoveLeftArmClearanceReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantMoveAnimationTool.CaptureIspantMoveLeftArmClearanceReview,
                        "A static-reference and five-phase Ispant left-arm clearance review was captured after full-cycle inspection passed.");
                    break;
                case "ApplyIspantDrawSwordAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantDrawSwordAnimationTool.ApplyIspantDrawSwordAnimation,
                        "The supplied Ispant draw-sword model replaced only Ispant_04_DrawSword with exact static appearance, looping Mixamo animation, and its own sword rigidly attached to the right hand.");
                    break;
                case "InspectIspantNewDrawSwordSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewDrawSwordAnimationTool.InspectIspantNewDrawSwordSource,
                        "The supplied new Ispant draw-sword FBX, Mixamo take, exact 24-bone compatibility, current sword, and right palm were inspected without changing the scene.");
                    break;
                case "ApplyIspantNewDrawSwordAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewDrawSwordAnimationTool.ApplyIspantNewDrawSwordAnimation,
                        "The supplied Mixamo draw-sword take was connected only to the current direct Ispant draw-sword object as a forward-only immediate-reset loop with real-time rigid right-arm sword follow.");
                    break;
                case "ApplyIspantNewDrawSwordLeftArmSkinningFix":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewDrawSwordAnimationTool.ApplyIspantNewDrawSwordLeftArmSkinningFix,
                        "The slot-four mesh islands were separated into left-arm and body-leg skinning domains, removing all cross-domain influences while preserving geometry, transforms, and animation curves; the eight malformed isolated components remain removed.");
                    break;
                case "InspectIspantNewDrawSwordAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewDrawSwordAnimationTool.InspectIspantNewDrawSwordAnimation,
                        "The applied forward-only Ispant draw-sword loop, adjusted outward grip, whole-clip blade turn to visible upward, real-time rigid sword follow, and target isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspantNewDrawSwordVisualReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewDrawSwordAnimationTool.CaptureIspantNewDrawSwordVisualReview,
                        "Twenty-one isolated Ispant draw-sword poses, including frames 40, 60, and 62 in left-arm close views, were rendered after numeric inspection.");
                    break;
                case "StartIspantNewDrawSwordReviewPlayback":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewDrawSwordAnimationTool.StartIspantNewDrawSwordReviewPlayback,
                        "The live Ispant Mixamo draw-sword review started in Edit Mode.");
                    break;
                case "StopIspantNewDrawSwordReviewPlayback":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantNewDrawSwordAnimationTool.StopIspantNewDrawSwordReviewPlayback,
                        "The live Ispant Mixamo draw-sword review completed multiple loops and restored the scene.");
                    break;
                case "InspectIspantDrawSwordAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantDrawSwordAnimationTool.InspectIspantDrawSwordAnimation,
                        "The Ispant draw-sword replacement, exact static appearance, looping Mixamo clip, and right-hand source-sword attachment were inspected across every frame without changing the scene.");
                    break;
                case "CaptureIspantDrawSwordAnimationReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantDrawSwordAnimationTool.CaptureIspantDrawSwordAnimationReview,
                        "A static-reference and five-phase Ispant draw-sword review was captured after full-cycle inspection passed.");
                    break;
                case "ApplyIspantRunningSwordAttackAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantRunningSwordAttackAnimationTool.ApplyIspantRunningSwordAttackAnimation,
                        "Only Ispant_05_RunningOneHandedSwordAttack was replaced with the supplied looping in-place Mixamo attack, exact static appearance, exact right-hand sword, and rigid back musket.");
                    break;
                case "InspectIspantRunningSwordAttackAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantRunningSwordAttackAnimationTool.InspectIspantRunningSwordAttackAnimation,
                        "The slot-5 in-place loop, exact static appearance, rigid right-hand sword, and rigid back musket were inspected across every frame without changing the scene.");
                    break;
                case "CaptureIspantRunningSwordAttackAnimationReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantRunningSwordAttackAnimationTool.CaptureIspantRunningSwordAttackAnimationReview,
                        "A static-reference and five-phase slot-5 running sword attack review was captured after full-cycle inspection passed.");
                    break;
                case "InspectIspantSlashRunningSources":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.InspectIspantSlashRunningSources,
                        "The supplied slash and running mixamo.com takes, Generic bone hierarchies, and current slot-5 direct-model target were inspected without changing the scene.");
                    break;
                case "DiagnoseIspantSlashRunningPresentation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.DiagnoseIspantSlashRunningPresentation,
                        "The slot-5 face, left-arm body mesh, sword renderer, and missing/present right-hand follower were diagnosed without changing the scene.");
                    break;
                case "DiagnoseIspantSlashRunningRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.DiagnoseIspantSlashRunningRevision,
                        "The slot-5 upper-body lateral offset, blade angle range, forearm relationship, and intact-versus-corrected body mesh were diagnosed without changing the scene.");
                    break;
                case "DiagnoseIspant06LegacyMotionTransfer":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.DiagnoseIspant06LegacyMotionTransfer,
                        "The current slot-6 model and the two legacy slot-6 source models were diagnosed without changing the scene.");
                    break;
                case "DiagnoseIspant06MusketComponents":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.DiagnoseIspant06MusketComponents,
                        "The current slot-6 rigid LeftShoulder mesh components were diagnosed without changing the scene.");
                    break;
                case "DiagnoseIspant06LegacyRecoveryMotion":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.DiagnoseIspant06LegacyRecoveryMotion,
                        "The finalized legacy slot-6 recovery object hierarchy was diagnosed without saving either scene.");
                    break;
                case "DiagnoseIspant06WeaponAlignment":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.DiagnoseIspant06WeaponAlignment,
                        "The current and finalized legacy slot-6 hand, musket, and forward-axis alignment was diagnosed without saving either scene.");
                    break;
                case "CaptureIspant06MusketComponentGroups":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.CaptureIspant06MusketComponentGroups,
                        "The current slot-6 upper-back component groups were isolated with the original material for direct diagnosis without saving the scene.");
                    break;
                case "CaptureIspant06WeaponIdentity":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.CaptureIspant06WeaponIdentity,
                        "The current slot-6 back musket, hand musket, and sword were isolated at the same rifle phase without saving the scene.");
                    break;
                case "StopPlayModeForIspant06Inspection":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.StopPlayModeForIspant06Inspection,
                        "Unity play mode was stopped for the approved slot-6 inspection.");
                    break;
                case "DiagnoseIspant06RetargetSamples":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.DiagnoseIspant06RetargetSamples,
                        "The legacy and current slot-6 shoulder, arm, forearm, and hand samples were measured at matching clip phases without changing the scene.");
                    break;
                case "ApplyIspant06LegacyMotionTransfer":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.ApplyIspant06LegacyMotionTransfer,
                        "The legacy slot-6 sheath, hold, bridge, and rifle-aim sequence was retargeted to the current direct model and saved only in slot 6.");
                    break;
                case "OptimizeCargoRunMvpShadowLights":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.OptimizeCargoRunMvpShadowLights,
                        "Realtime shadow casting was disabled on the current CargoRunMvp scene lights while preserving their brightness, color, and range for direct visual review.");
                    break;
                case "ApplyIspant06SwordGripOnly":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.ApplyIspant06SwordGripOnly,
                        "Only the current slot-6 hand-sword grip was restored without regenerating the four motion clips; the visual verdict remains pending user review.");
                    break;
                case "ApplyIspant06SheathLeftArmStaticPose":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.ApplyIspant06SheathLeftArmStaticPose,
                        "Only the current slot-6 sheath clip left shoulder, arm, forearm, and hand were matched to the static-model pose while preserving the approved right-hand sword grip and sword motion.");
                    break;
                case "InspectIspant06LegacyMotionTransfer":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.InspectIspant06LegacyMotionTransfer,
                        "The current slot-6 legacy-motion retarget, sword grip, intact mesh, controller sequence, and finite deformation were inspected without changing the scene.");
                    break;
                case "CaptureIspant06LegacyMotionComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.CaptureIspant06LegacyMotionComparison,
                        "The legacy final slot-6 sequence and the current retarget were captured once at the same 11 phases for direct visual comparison.");
                    break;
                case "CaptureIspant06SwordGripReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.CaptureIspant06SwordGripReview,
                        "Four close views of the current slot-6 right hand and sword hilt were captured for direct visual review without an automatic motion verdict.");
                    break;
                case "CaptureIspant06SheathLeftArmReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06LegacyMotionTransferTool.CaptureIspant06SheathLeftArmReview,
                        "The static left arm and four slot-6 sheath phases were captured for direct visual review; the visual verdict remains pending user review.");
                    break;
                case "InspectIspant06EmbeddedSheathingSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06EmbeddedSheathingSource,
                        "The user-supplied slot-6 FBX clip paths and current direct-model paths were inspected before replacing the Animator connection.");
                    break;
                case "ApplyIspant06EmbeddedSheathingLoop":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06EmbeddedSheathingLoop,
                        "The previous slot-6 four-state Animator connection was replaced by the user-supplied mixamo.com clip in one looping state while preserving the current model.");
                    break;
                case "CaptureIspant06EmbeddedSheathingLoopReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06EmbeddedSheathingLoopReview,
                        "Six phases of the new slot-6 embedded Mixamo sheathing loop were captured for direct visual review without an automatic verdict.");
                    break;
                case "ApplyIspant06EmbeddedLoopSwordGrip":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06EmbeddedLoopSwordGrip,
                        "The slot-6 long-sword hilt was attached to the visible right glove and its blade direction was made to follow the right forearm throughout the approved Mixamo loop.");
                    break;
                case "CaptureIspant06EmbeddedLoopSwordGripReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06EmbeddedLoopSwordGripReview,
                        "Six full-body and six right-hand close views of the slot-6 sword grip were captured for direct visual review without an automatic verdict.");
                    break;
                case "ApplyIspant06EmbeddedLoopSwordSheathPath":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06EmbeddedLoopSwordSheathPath,
                        "The slot-6 long sword now follows the actual right-hand position and rotation through the motion and transitions into the Ispant_01_Static left-waist sword pose at the end.");
                    break;
                case "CaptureIspant06EmbeddedLoopSwordSheathReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06EmbeddedLoopSwordSheathReview,
                        "The static left-waist sword reference and eight slot-6 motion phases were captured for direct visual review without an automatic verdict.");
                    break;
                case "ApplyIspant06EmbeddedLoopArmAndMusketFix":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06EmbeddedLoopArmAndMusketFix,
                        "The slot-6 right upper-arm and forearm now use the source full rotation delta in the current model rest basis, the hand keeps the current forearm-relative rest offset, and the rigid back musket remains separated under Spine.");
                    break;
                case "InspectIspant06EmbeddedLoopArmAxisFix":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06EmbeddedLoopArmAxisFix,
                        "The slot-6 right-arm rest-basis transfer, fixed hand offset, quaternion continuity, loop settings, and untouched source curves were inspected without an automatic visual verdict.");
                    break;
                case "ApplyIspant06LeftArmStretchRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06LeftArmStretchRemoval,
                        "The stretching left-arm-driven triangles and the vertices left unused by them were removed from the slot-6 body mesh.");
                    break;
                case "ApplyIspant06WaistRemnantRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06WaistRemnantRemoval,
                        "The leftover sheathed hilt pieces on the Ispant left hip were removed from the slot-6 body mesh.");
                    break;
                case "InspectIspant06Renderers":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06Renderers,
                        "Every renderer under slot 6 was listed with its enabled flag and mesh without changing the scene.");
                    break;
                case "ApplyIspant06RestoreWeaponVisibility":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06RestoreWeaponVisibility,
                        "The slot-6 weapon renderers were put back to the intended visibility.");
                    break;
                case "CaptureIspant06PickedPartHighlight":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06PickedPartHighlight,
                        "A ray pick through the marked pixel selected one part and painted it red for comparison, without changing the mesh.");
                    break;
                case "ApplyIspant06PickedPartRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06PickedPartRemoval,
                        "The part selected by the ray pick was removed from the slot-6 body mesh.");
                    break;
                case "CaptureIspant06SelectionHighlight":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06SelectionHighlight,
                        "Candidate selections were painted red over the posed body and rendered for direct comparison, without changing the mesh.");
                    break;
                case "InspectIspant06HipAsymmetry":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06HipAsymmetry,
                        "Hip geometry with no mirrored counterpart on the other side was listed without changing the scene.");
                    break;
                case "ApplyIspant06HipAsymmetryRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06HipAsymmetryRemoval,
                        "The unmatched hip geometry on the Ispant left side was removed from the slot-6 body mesh.");
                    break;
                case "CaptureIspant06LeftHipCloseup":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06LeftHipCloseup,
                        "The Ispant left hip was rendered close up from four angles without changing the scene.");
                    break;
                case "CaptureIspant06FloatingHiltComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06FloatingHiltComparison,
                        "The left hip was rendered with props visible and body only from one camera, so leftover geometry can be told apart from the sword.");
                    break;
                case "CaptureIspant06FloatingHiltHighlight":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06FloatingHiltHighlight,
                        "The flakes the ray pick found beside the left belt were painted red and rendered from four angles, without changing the mesh.");
                    break;
                case "CaptureIspant06FloatingHiltRemovalPreview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06FloatingHiltRemovalPreview,
                        "The hip was rendered as it stands and as it would look after the removal, without changing the scene.");
                    break;
                case "ApplyIspant06FloatingHiltRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06FloatingHiltRemoval,
                        "The floating hilt flakes beside the Ispant left belt were removed from the slot-6 body mesh.");
                    break;
                case "CaptureIspant06LeftThighRestorePreview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06LeftThighRestorePreview,
                        "The body was rendered as it stands and with the chosen clusters put back, without changing the scene.");
                    break;
                case "ApplyIspant06LeftThighRestore":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06LeftThighRestore,
                        "The wrongly removed left thigh geometry was put back into the slot-6 body mesh.");
                    break;
                case "CaptureIspant06RestoredClusterRemovalPreview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06RestoredClusterRemovalPreview,
                        "The body was rendered as it stands and with the chosen restored clusters dropped, without changing the scene.");
                    break;
                case "ApplyIspant06RestoredClusterRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06RestoredClusterRemoval,
                        "The arm driven waist debris clusters were taken back out of the slot-6 body mesh.");
                    break;
                case "CaptureIspant06RestoredClusterAtlas":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06RestoredClusterAtlas,
                        "Each restored cluster was painted red on the current body so the leg armour can be told apart from the sword flakes, without changing the scene.");
                    break;
                case "CaptureIspant06MissingClusterAtlas":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06MissingClusterAtlas,
                        "Each removed cluster was painted red on the bind-pose shell so it can be matched to the hole it left, without changing the scene.");
                    break;
                case "InspectIspant06MissingGeometry":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06MissingGeometry,
                        "Every triangle cut away from the slot-6 body since the untouched export was listed and measured, without changing the scene.");
                    break;
                case "InspectIspant06RemainingIslands":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06RemainingIslands,
                        "Every connected piece of the slot-6 body except the main shell was listed with its size and position without changing the scene.");
                    break;
                case "ApplyIspant06ArmTorsoBridgeRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06ArmTorsoBridgeRemoval,
                        "Only the triangles welding the left arm surface to the torso were removed; triangles that sit entirely on the arm were kept.");
                    break;
                case "ApplyIspant06LeftArmRegionWeightClean":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06LeftArmRegionWeightClean,
                        "The whole connected left arm surface kept only left arm influences, with no vertex duplicated or deleted.");
                    break;
                case "InspectIspant06SeamGap":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06SeamGap,
                        "The widest separation between vertices that share a bind position was measured without changing the scene.");
                    break;
                case "ApplyIspant06LeftArmSeamSplit":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06LeftArmSeamSplit,
                        "Every stretching triangle received its own rigid copies of its corners, so the seam opens instead of the mesh smearing, and no geometry was removed.");
                    break;
                case "ApplyIspant06LeftArmWeightFix":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06LeftArmWeightFix,
                        "The stretching vertices kept only the influences of their dominant bone and its immediate neighbours, so the modelled shape is preserved and nothing was deleted.");
                    break;
                case "RestoreIspant06BodyBeforeStretchRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.RestoreIspant06BodyBeforeStretchRemoval,
                        "The slot-6 body mesh was restored to the version that still contains the left arm geometry.");
                    break;
                case "InspectIspant06StretchTriangles":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06StretchTriangles,
                        "The stretching triangles were broken down vertex by vertex without changing the scene.");
                    break;
                case "InspectIspant06LeftArmStretch":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06LeftArmStretch,
                        "The slot-6 body vertices that are skinned to the left arm but sit far away from it were listed without changing the scene.");
                    break;
                case "ApplyIspant06StaticReturnTail":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06StaticReturnTail,
                        "A 0.4 second tail was appended in which every animated bone eases to the static model pose while the sword stays on the left waist.");
                    break;
                case "InspectIspant06StaticReturnBoundary":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06StaticReturnBoundary,
                        "The slot-6 loop start and loop end poses were compared against the static model pose without changing the scene.");
                    break;
                case "ApplyIspant06WaistHiltSeparation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06WaistHiltSeparation,
                        "The leftover waist hilt island was rebound from LeftShoulder to Hips so the left arm mesh is no longer dragged by it.");
                    break;
                case "ApplyIspant06WaistHiltRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06WaistHiltRemoval,
                        "The separated waist hilt geometry was removed from the slot-6 body mesh while the remaining vertices, weights, and bind poses stayed intact.");
                    break;
                case "InspectIspant06WaistHilt":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06WaistHilt,
                        "The slot-6 body mesh islands, their model-space centres, their driving bones, and their left-arm weighting were listed without changing the scene.");
                    break;
                case "ApplyIspant06HandSwordGripAndWaist":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06HandSwordGripAndWaist,
                        "The existing approved long sword now follows the corrected right hand with one rigid grip mount and returns to the left waist mount at the end of the loop.");
                    break;
                case "InspectIspant06HandSwordClearance":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06HandSwordClearance,
                        "The hand sword grip rigidity, hilt to palm distance, blade clearance from the torso and right arm, and the left waist mount at the loop end were measured without changing the scene.");
                    break;
                case "CaptureIspant06HandSwordGripReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06HandSwordGripReview,
                        "One review image with the full body and the right-hand grip close-up was captured without changing the scene.");
                    break;
                case "CaptureIspant06RightArmRollCorrectionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06RightArmRollCorrectionReview,
                        "One review image with the raw source arm, the corrected arm, and the corrected full body was captured without changing the scene.");
                    break;
                case "ApplyIspant06RightArmRollCorrection":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06RightArmRollCorrection,
                        "Only the excess axial roll on the slot-6 right upper arm was removed while every right arm joint kept its exact model-space direction, and the hand returned to the raw source curve.");
                    break;
                case "InspectIspant06RightArmRestBasisDiff":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.InspectIspant06RightArmRestBasisDiff,
                        "The slot-6 right arm chain rest orientations, raw source curve identity, and limb direction trajectories were compared against the source rig without changing the scene.");
                    break;
                case "InspectIspant01StaticStandingPose":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant01StaticStandingPoseTool.InspectIspant01StaticStandingPose,
                        "The slot-1 static model's torso, leg, foot, toe, and weighted sole heights were inspected without changing the scene.");
                    break;
                case "ApplyIspant01StaticStandingPose":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant01StaticStandingPoseTool.ApplyIspant01StaticStandingPose,
                        "Only the slot-1 static model's hips and lower body were aligned upright with both weighted soles on the ground while preserving both arms and hands.");
                    break;
                case "CaptureIspant01StaticStandingPoseReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant01StaticStandingPoseTool.CaptureIspant01StaticStandingPoseReview,
                        "One final front and three-quarter review image was captured for the slot-1 static standing pose without changing the scene.");
                    break;
                case "ApplyIspant01StaticRightArmCorrection":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant01StaticStandingPoseTool.ApplyIspant01StaticRightArmCorrection,
                        "Only the slot-1 static model's right shoulder, upper arm, forearm, and hand rotations were mirrored from the intact left arm while preserving the standing pose.");
                    break;
                case "InspectIspant01StaticRightArmCorrection":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant01StaticStandingPoseTool.InspectIspant01StaticRightArmCorrection,
                        "The corrected right-arm mirror basis, untouched left arm and fingers, upright torso, and grounded feet were inspected without changing the scene.");
                    break;
                case "CaptureIspant01StaticRightArmCorrectionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant01StaticStandingPoseTool.CaptureIspant01StaticRightArmCorrectionReview,
                        "One final front, right-front, and right-arm-close review image was captured without changing the scene.");
                    break;
                case "CaptureIspant06EmbeddedLoopArmAndMusketReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06EmbeddedLoopArmAndMusketReview,
                        "Eight full-body and eight close arm-and-musket views were captured for direct visual review without an automatic verdict.");
                    break;
                case "ApplyIspantSlashRunningComposite":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.ApplyIspantSlashRunningComposite,
                        "Only slot 5 received the centered whole upper body and right-hand radial outward forward-cut sword follow while preserving the looping slash/running composite and intact direct model.");
                    break;
                case "ApplyIspantLegacySwordRightHandGrip":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.ApplyIspantLegacySwordRightHandGrip,
                        "Only the slot-5 sword right-hand grip point was moved into the closed fist while preserving the legacy trajectory.");
                    break;
                case "RestoreIspantPreAugust17ModelAndOriginalAnimations":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantPreAugust17RestoreTool.RestoreIspantPreAugust17ModelAndOriginalAnimations,
                        "The exact pre-August-17 Ispant placement root was restored from the historical Git LFS scene with its original model, Animator, Avatar, controller, weapon, and physics connections.");
                    break;
                case "CapturePlacedIspantPreAugust17RestoreInspection":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantPreAugust17RestoreTool.CapturePlacedIspantPreAugust17RestoreInspection,
                        "The actual placed pre-August-17 Ispant models and running animations were captured without manipulating the targets.");
                    break;
                case "InspectIspantPreAugust17RestoreResult":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantPreAugust17RestoreTool.InspectIspantPreAugust17RestoreResult,
                        "The restored Ispant placement matches the historical scene contract and uses eleven historical animation connections.");
                    break;
                case "CaptureIspantPreAugust17RestoreFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantPreAugust17RestoreTool.CaptureIspantPreAugust17RestoreFinal,
                        "The one-time final image was created from the directly inspected actual Play Mode capture.");
                    break;
                case "InspectAllIspantHiltFragmentRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantAllHiltFragmentRemovalTool.InspectAllIspantHiltFragmentRemoval,
                        "All twelve placed Ispant body meshes and their exact waist-hilt reference-triangle matches were inspected without changing the scene.");
                    break;
                case "InspectAllIspantLeftWaistHiltCorrection":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantLeftWaistHiltCorrectionTool.InspectAllIspantLeftWaistHiltCorrection,
                        "The three pre-error Ispant body sources were inspected separately for left-waist component topology, reference proximity, UV correspondence, and dominant bone influence without changing the scene.");
                    break;
                case "ApplyAllIspantLeftWaistHiltCorrection":
                    global::Bellerophon.Editor.IspantCargoRunScene
                        .IspantLeftWaistHiltCorrectionTool.SetBridgeOperation("Apply");
                    goto case "PreviewAllIspantLeftWaistHiltCorrection";
                case "CaptureAllIspantLeftWaistHiltCorrection":
                    global::Bellerophon.Editor.IspantCargoRunScene
                        .IspantLeftWaistHiltCorrectionTool.SetBridgeOperation("Capture");
                    goto case "PreviewAllIspantLeftWaistHiltCorrection";
                case "PreviewAllIspantLeftWaistHiltCorrection":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantLeftWaistHiltCorrectionTool.PreviewAllIspantLeftWaistHiltCorrection,
                        "The non-slot-6 waist sword and embedded body targets plus the slot-6 gray-fragment candidate were captured as before, red target, and after panels without changing the scene.");
                    break;
                case "PreviewAllIspantHiltFragmentRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantAllHiltFragmentRemovalTool.PreviewAllIspantHiltFragmentRemoval,
                        "All twelve placed Ispant waist-hilt selections were captured as before, red-highlight, and removal-preview closeups for direct review.");
                    break;
                case "ApplyAllIspantHiltFragmentRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantAllHiltFragmentRemovalTool.ApplyAllIspantHiltFragmentRemoval,
                        "Only the reference-matched waist-hilt triangles were removed from every placed Ispant body while preserving animation, bones, materials, and other scene roots.");
                    break;
                case "CaptureAllIspantHiltFragmentRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantAllHiltFragmentRemovalTool.CaptureAllIspantHiltFragmentRemoval,
                        "One final twelve-slot closeup sheet was captured after all reference-matched waist-hilt triangles were absent.");
                    break;
                case "PreviewIspant06MarkedHiltFragmentRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.PreviewIspant06MarkedHiltFragmentRemoval,
                        "The script-selected 56-triangle lineage was previewed against the preserved 455-triangle leg-armour restoration; a direct visual verdict is still required.");
                    break;
                case "ApplyIspant06MarkedHiltFragmentRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.ApplyIspant06MarkedHiltFragmentRemoval,
                        "The script-selected 56-triangle lineage was removed while preserving the 455-triangle leg armour and bind poses; this does not assert that the marked hilt is resolved.");
                    break;
                case "CaptureIspant06MarkedHiltFragmentRemoval":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.Ispant06EmbeddedSheathingLoopTool.CaptureIspant06MarkedHiltFragmentRemoval,
                        "The resulting slot-6 body was captured once for direct visual review without an automatic completion verdict.");
                    break;
                case "InspectIspantSlashRunningComposite":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.InspectIspantSlashRunningComposite,
                        "The slot-5 composite source curves, upper/lower local poses, finite skinned mesh, controller layers, and scene isolation were inspected.");
                    break;
                case "StartIspantSlashRunningCompositeReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.StartIspantSlashRunningCompositeReview,
                        "The live Edit Mode review started for the independent looping slash and lower-body running clips.");
                    break;
                case "StopIspantSlashRunningCompositeReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.StopIspantSlashRunningCompositeReview,
                        "The live composite review completed two loops of both clips and restored the scene state.");
                    break;
                case "CaptureIspantSlashRunningSourceComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantSlashRunningSourceComparison,
                        "A one-time direct comparison of supplied slash motion, supplied running motion, and the final slot-5 composite was captured at five matched phases.");
                    break;
                case "CaptureIspantSlashRunningFixComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantSlashRunningFixComparison,
                        "A one-time direct comparison of the actual supplied source models and the centered outward-forward-slash slot-5 result was captured after the numeric and live reviews passed.");
                    break;
                case "CaptureIspantSlashGifTrajectoryComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantSlashGifTrajectoryComparison,
                        "All 15 supplied GIF frames and the final slot-5 sword trajectory were captured side by side at matching normalized times after numeric and live review passed.");
                    break;
                case "CaptureIspantSlashGifTrajectoryRevisionComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantSlashGifTrajectoryRevisionComparison,
                        "The revised slot-5 Model Cam screen-space sword trajectory and all 15 supplied GIF frames were captured side by side once after numeric and live review passed.");
                    break;
                case "CaptureIspantSlashGifUpwardTrajectoryComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantSlashGifUpwardTrajectoryComparison,
                        "The stable front-view slot-5 upward sword trajectory and all 15 supplied GIF frames were captured side by side once after numeric and live review passed.");
                    break;
                case "CaptureIspantSlashGifActualTraceComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantSlashGifActualTraceComparison,
                        "The slot-5 sword driven by measured GIF grip and tip pixels and all 15 supplied frames were captured side by side once after numeric and live review passed.");
                    break;
                case "CaptureIspantSlashGifActualTraceDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantSlashGifActualTraceDiagnostic,
                        "A temporary enlarged direct-review diagnostic captured the post-final body-relative side correction without changing the scene.");
                    break;
                case "CaptureIspantLegacySwordMotionComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantLegacySwordMotionComparison,
                        "The pre-model-revision slot-5 sword motion and current slot-5 transfer were captured at matching times after inspection passed.");
                    break;
                case "InspectIspantLegacySwordRightHandGrip":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.InspectIspantLegacySwordRightHandGrip,
                        "The slot-5 closed-fist sword grip and unchanged legacy blade and roll trajectories were inspected.");
                    break;
                case "CaptureIspantLegacySwordRightHandGripComparison":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSlashRunningCompositeAnimationTool.CaptureIspantLegacySwordRightHandGripComparison,
                        "The supplied GIF and current slot-5 closed-fist sword grip were captured across all 15 matching times.");
                    break;
                case "ApplyIspant09SlashReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantOneHandedSwordAttackAnimationTool.ApplyIspantOneHandedSwordAttackAnimation,
                        "Only Ispant_09_OneHandedSwordAttack was replaced with the supplied slash model, exact static appearance, and looping embedded Mixamo animation.");
                    break;
                case "InspectIspant09SlashReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantOneHandedSwordAttackAnimationTool.InspectIspantOneHandedSwordAttackAnimation,
                        "The slot-9 supplied slash source, exact static appearance references, and looping Mixamo state were inspected without changing the scene.");
                    break;
                case "CaptureIspant09SlashReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantOneHandedSwordAttackAnimationTool.CaptureIspantOneHandedSwordAttackReview,
                        "A static-reference and five-phase slot-9 one-handed sword attack review was captured after inspection passed.");
                    break;
                case "CaptureIspant09VisualDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantOneHandedSwordAttackAnimationTool.CaptureIspant09VisualDiagnostic,
                        "A dense slot-9 visual diagnostic strip was captured without numeric inspection.");
                    break;
                case "CaptureIspant09VisualFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantOneHandedSwordAttackAnimationTool.CaptureIspant09VisualFinal,
                        "The one-time dense slot-9 final visual strip was captured without numeric inspection.");
                    break;
                case "ApplyIspant10StopAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantStopAnimationTool.ApplyIspant10StopAnimation,
                        "Only Ispant_10_Stop received the rotation-only bowed-head, hanging-arms, and gradual eye-desaturation loop.");
                    break;
                case "CaptureIspant10StopDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantStopAnimationTool.CaptureIspant10StopDiagnostic,
                        "A body-and-face slot-10 visual diagnostic was captured without numeric visual inspection.");
                    break;
                case "CaptureIspant10StopFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantStopAnimationTool.CaptureIspant10StopFinal,
                        "The one-time body-and-face slot-10 final visual strip was captured.");
                    break;
                case "ApplyIspant11HitReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantHitReactionAnimationTool.ApplyIspant11HitReplacement,
                        "Only Ispant_11_HitReaction was replaced with the supplied hit FBX, exact static appearance, looping Mixamo motion, and both arms lowered for the entire clip.");
                    break;
                case "CaptureIspant11HitDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantHitReactionAnimationTool.CaptureIspant11HitDiagnostic,
                        "A static-reference and thirteen-phase slot-11 hit-reaction diagnostic strip was captured for direct visual review.");
                    break;
                case "CaptureIspant11HitFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantHitReactionAnimationTool.CaptureIspant11HitFinal,
                        "The one-time slot-11 hit-reaction final visual strip was captured after direct review.");
                    break;
                case "ApplyIspant11HeightAlignment":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantHitReactionAnimationTool.ApplyIspant11HeightAlignment,
                        "Only the slot-11 hit model local Y position was copied from the already height-corrected slot-9 model.");
                    break;
                case "CaptureIspant11HeightDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantHitReactionAnimationTool.CaptureIspant11HeightDiagnostic,
                        "A fixed-vertical-reference slot-11 height diagnostic was captured for direct visual review.");
                    break;
                case "CaptureIspant11HeightFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantHitReactionAnimationTool.CaptureIspant11HeightFinal,
                        "The one-time fixed-reference slot-11 height final was captured after direct review.");
                    break;
                  case "ApplyIspant12DeathReplacement":
                      RunSynchronous(
                          request,
                          global::Bellerophon.Editor.IspantCargoRunScene.IspantDeathAnimationTool.ApplyIspant12DeathReplacement,
                          "Only Ispant_12_Death was replaced with the supplied FBX, exact static appearance, the embedded Mixamo loop, aligned ground height, and thigh-to-foot closure during the fall.");
                      break;
                  case "InspectIspant12SwordStructure":
                      RunSynchronous(
                          request,
                          global::Bellerophon.Editor.IspantCargoRunScene.IspantDeathAnimationTool.InspectIspant12SwordStructure,
                          "The slot-12 death source and current scene renderer structures were inspected without changing the scene.");
                      break;
                  case "CaptureIspant12DeathDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantDeathAnimationTool.CaptureIspant12DeathDiagnostic,
                        "Full-body and lower-body slot-12 death diagnostics were captured for direct visual review.");
                    break;
                case "CaptureIspant12DeathFinal":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantDeathAnimationTool.CaptureIspant12DeathFinal,
                        "The one-time full-body and lower-body slot-12 death finals were captured after direct review.");
                    break;
                case "ApplyIspantSheathSwordAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathSwordAnimation,
                        "Only Ispant_06_SheathSwordDrawMusket was replaced with the supplied looping Mixamo motion, exact static appearance, exact right-hand sword, and rigid back musket.");
                    break;
                case "InspectIspantSheathSwordAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathSwordAnimation,
                        "The slot-6 looping Mixamo clip, exact static appearance, rigid right-hand sword, and rigid back musket were inspected across every frame without changing the scene.");
                    break;
                case "CaptureIspantSheathSwordAnimationReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathSwordAnimationReview,
                        "A static-reference and five-phase slot-6 sheath-sword review was captured after full-cycle inspection passed.");
                    break;
                case "ApplyIspantSheathSwordStaticHold":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathSwordStaticHold,
                        "Only slot 6 was updated to play the unchanged Mixamo sheath motion, place its sword at the exact static-model transform, hold the final pose for 0.5 seconds, and repeat.");
                    break;
                case "InspectIspantSheathSwordStaticHold":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathSwordStaticHold,
                        "The slot-6 Mixamo-to-static-hold sequence, exact 0.5-second duration, fixed sword transform, and repeat transitions were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathSwordStaticHoldReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathSwordStaticHoldReview,
                        "A static, Mixamo-end, three-phase hold, and repeat-start slot-6 review was captured after the static-hold inspection passed.");
                    break;
                case "ApplyIspantSheathSwordWaistHoldRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathSwordWaistHoldRevision,
                        "Only slot 6 was revised to keep the unchanged Mixamo arm return, switch from the hand sword to an exact static-reference left-waist sword, hold for 0.5 seconds, and repeat.");
                    break;
                case "InspectIspantSheathSwordWaistHoldRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathSwordWaistHoldRevision,
                        "The slot-6 final arm pose, exact left-waist sword reference, rigid 0.5-second hold, visibility switch, and unchanged neighboring slots were inspected.");
                    break;
                case "CaptureIspantSheathSwordWaistHoldRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathSwordWaistHoldRevisionReview,
                        "A one-time static, Mixamo-end, left-waist hold, and repeat-start slot-6 review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleSequence":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleSequence,
                        "Only slot 6 was extended from the approved sheath and 0.5-second hold into the supplied 213-frame Mixamo rifle-change motion, with a measured back-to-right-hand rigid musket switch and full-sequence repeat.");
                    break;
                case "InspectIspantSheathToRifleSequence":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleSequence,
                        "The slot-6 three-state sequence, unchanged Mixamo curves, measured musket grab, rigid right-hand follow, forward motion, weapon visibility, and scene isolation were inspected.");
                    break;
                case "CaptureIspantSheathToRifleSequenceReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleSequenceReview,
                        "A one-time static, sheath-end, hold, rifle-start, pre-grab, grab, forward, and rifle-end slot-6 review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleMotionRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleMotionRevision,
                        "Slot 6 received the measured sheath-to-rifle bridge and right-hand-driven musket muzzle rotation revision.");
                    break;
                case "InspectIspantSheathToRifleMotionRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleMotionRevision,
                        "The bridge continuity, right-hand musket attachment, angle change, and forward muzzle direction were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathToRifleMotionRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleMotionRevisionReview,
                        "The one-time final slot-6 motion-revision review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleTwoHandGripRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleTwoHandGripRevision,
                        "Slot 6 received the right-hand contact pivot and data-derived two-hand musket grip revision.");
                    break;
                case "InspectIspantSheathToRifleTwoHandGripRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleTwoHandGripRevision,
                        "The slot-6 right-hand pivot, left-hand support contact, muzzle direction, and sequence isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathToRifleTwoHandGripRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleTwoHandGripRevisionReview,
                        "The one-time final slot-6 two-hand musket grip review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleArmDrivenAimRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleArmDrivenAimRevision,
                        "Slot 6 received the right-hand-driven musket rotation and final two-hand forward-aim revision.");
                    break;
                case "InspectIspantSheathToRifleArmDrivenAimRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleArmDrivenAimRevision,
                        "The slot-6 right-hand-driven local rotation, two-hand support, final muzzle direction, and sequence isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathToRifleArmDrivenAimRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleArmDrivenAimRevisionReview,
                        "The one-time final slot-6 arm-driven two-hand aim review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleForwardMuzzleRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleForwardMuzzleRevision,
                        "Slot 6 received the corrected transformed model-forward muzzle aim while preserving its arm-driven two-hand grip.");
                    break;
                case "InspectIspantSheathToRifleForwardMuzzleRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleForwardMuzzleRevision,
                        "The slot-6 muzzle endpoint, transformed model-forward aim, arm-driven grip, two-hand support, and scene isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathToRifleForwardMuzzleRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleForwardMuzzleRevisionReview,
                        "The one-time final slot-6 forward-muzzle review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleUprightTriggerGripRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleUprightTriggerGripRevision,
                        "Slot 6 received the upright trigger-below-barrel roll while preserving its forward muzzle and arm-driven grip.");
                    break;
                case "InspectIspantSheathToRifleUprightTriggerGripRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleUprightTriggerGripRevision,
                        "The slot-6 upright trigger roll, right-hand grip pivot, left-hand support, forward muzzle, arm-driven rotation, and scene isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathToRifleUprightTriggerGripRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleUprightTriggerGripRevisionReview,
                        "The one-time final slot-6 upright trigger-grip review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleStockAndTriggerDownRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleStockAndTriggerDownRevision,
                        "Slot 6 received a mesh-axis-derived stock-thick-side and trigger-down firing roll while preserving its forward muzzle and right-hand pivot.");
                    break;
                case "InspectIspantSheathToRifleStockAndTriggerDownRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleStockAndTriggerDownRevision,
                        "The slot-6 broad stock side, trigger-down axis, right-hand grip pivot, forward muzzle, left support, and scene isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathToRifleStockAndTriggerDownRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleStockAndTriggerDownRevisionReview,
                        "The one-time final slot-6 stock-and-trigger-down firing-pose review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleWaistSwordBodyFollowRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleWaistSwordBodyFollowRevision,
                        "Slot 6 received a rigid left-waist sword attachment under mixamorig:Hips while preserving its approved sheath-end placement and musket sequence.");
                    break;
                case "InspectIspantSheathToRifleWaistSwordBodyFollowRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleWaistSwordBodyFollowRevision,
                        "The slot-6 left-waist sword hip attachment, rigid local mount, body-driven position and angle motion, musket pose, and scene isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathToRifleWaistSwordBodyFollowRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleWaistSwordBodyFollowRevisionReview,
                        "The one-time final slot-6 waist-sword body-follow review was captured after inspection passed.");
                    break;
                case "ApplyIspantSheathToRifleFinalAimArmLiftRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleFinalAimArmLiftRevision,
                        "Slot 6 received a 0.15m final aiming lift through bilateral arm-bone rotation while preserving musket aim, grips, and waist-sword follow.");
                    break;
                case "InspectIspantSheathToRifleFinalAimArmLiftRevision":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.InspectIspantSheathToRifleFinalAimArmLiftRevision,
                        "The slot-6 final weapon-pivot lift, arm-only implementation, muzzle and stock orientation, bilateral grips, waist-sword follow, and scene isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspantSheathToRifleFinalAimArmLiftRevisionReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantSheathSwordAnimationTool.CaptureIspantSheathToRifleFinalAimArmLiftRevisionReview,
                        "The one-time final slot-6 0.15m arm-lift aiming review was captured after inspection passed.");
                    break;
                case "ApplyIspant07FiringReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantFiringAnimationTool.ApplyIspant07FiringReplacement,
                        "Slot 7 was rebuilt with the supplied Mixamo firing model, the exact static shared appearance, the slot-6 final two-hand musket placement, the design-source 2.5-second breakthrough attack interval, and the approved reused muzzle flash.");
                    break;
                case "InspectIspant07FiringReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantFiringAnimationTool.InspectIspant07FiringReplacement,
                        "The slot-7 source hash, single Mixamo take, 2.5-second breakthrough interval, detected recoil firing frame, approved muzzle flash, exact static shared appearance, and slot-6 final musket placement were inspected without changing the scene.");
                    break;
                case "CaptureIspant07FiringReplacementReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantFiringAnimationTool.CaptureIspant07FiringReplacementReview,
                        "A one-time pre-fire, firing, and post-fire slot-7 muzzle-flash review was captured after inspection passed.");
                    break;
                case "ApplyIspant08ChangingToSwordReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantChangingToSwordAnimationTool.ApplyIspant08ChangingToSwordReplacement,
                        "Slot 8 was rebuilt so its hand-lowering endpoint immediately enters the exact 0.3-second pose bridge and vertically aligned slot-4 draw continuation while preserving equipment.");
                    break;
                case "InspectIspant08ChangingToSwordReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantChangingToSwordAnimationTool.InspectIspant08ChangingToSwordReplacement,
                        "The slot-8 source motion, removed lowered-pose hold, immediate 0.3-second exact-pose bridge, vertical continuity, shared appearance, equipment, and scene isolation were inspected without changing the scene.");
                    break;
                case "CaptureIspant08ChangingToSwordReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantChangingToSwordAnimationTool.CaptureIspant08ChangingToSwordReview,
                        "A one-time front-and-back review of the musket sequence, hand-lowering endpoint, immediate 0.3-second bridge, draw start, draw motion, and loop start was captured after inspection passed.");
                    break;
                case "CaptureIspant06And07GripDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantFiringAnimationTool.CaptureIspant06And07GripDiagnostic,
                        "Slot 6 final aim and slot 7 start, middle, and end grip close-ups were captured from the front, left, and right without changing the scene.");
                    break;
                case "CaptureIspant07GroundAlignmentDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantFiringAnimationTool.CaptureIspant07GroundAlignmentDiagnostic,
                        "The static model and slot 7 start, middle, and end were captured with one shared camera and ground height without changing the scene.");
                    break;
                case "CaptureIspant07MuzzleFlashDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantFiringAnimationTool.CaptureIspant07MuzzleFlashDiagnostic,
                        "Slot 7 pre-fire, detected firing instant, and post-fire muzzle close-ups were captured with the approved reused muzzle flash without changing the scene.");
                    break;
                case "ApplyIspantApprovedLongSwordAllSlots":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantApprovedLongSwordTool.ApplyIspantApprovedLongSwordAllSlots,
                        "The approved Ispant long sword was applied to all twelve slots while preserving the eleven non-draw mounts and binding only the draw-sword slot to its right hand.");
                    break;
                case "InspectIspantApprovedLongSwordAllSlots":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantApprovedLongSwordTool.InspectIspantApprovedLongSwordAllSlots,
                        "The twelve-slot approved Ispant long-sword structure and draw-sword right-hand attachment were inspected without changing the scene.");
                    break;
                case "CaptureIspantApprovedLongSwordReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantApprovedLongSwordTool.CaptureIspantApprovedLongSwordReview,
                        "The final twelve-slot approved Ispant long-sword review was captured after structural and animation inspection passed.");
                    break;
                case "ApplyIspantStaticSwordMeshConsistency":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantApprovedLongSwordTool.ApplyIspantStaticSwordMeshConsistency,
                        "The exact Ispant_01_Static sword mesh was applied only to Ispant_03_Move and Ispant_04_DrawSword while preserving their mounts and animation.");
                    break;
                case "InspectIspantSwordMeshConsistency":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantApprovedLongSwordTool.InspectIspantSwordMeshConsistency,
                        "All twelve Ispant slots were inspected for exact Ispant_01_Static sword mesh identity without changing the scene.");
                    break;
                case "CaptureIspantSwordConsistencyReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.IspantCargoRunScene.IspantApprovedLongSwordTool.CaptureIspantSwordConsistencyReview,
                        "A one-time static, move, and draw sword mesh consistency review was captured after inspection passed.");
                    break;
                case "ApplyKursaMoveAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.ApplyKursaMoveAnimation,
                        "Kursa move animation applied.");
                    break;
                case "InspectKursaMoveAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.InspectKursaMoveAnimation,
                        "Kursa move animation inspected.");
                    break;
                case "CaptureKursaMoveAnimationReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.CaptureKursaMoveAnimationReview,
                        "Kursa move animation review captured.");
                    break;
                case "ApplyKursaMoveRightArmClearance":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.ApplyKursaMoveRightArmClearance,
                        "Kursa move right-arm clearance applied.");
                    break;
                case "CaptureKursaMoveRightArmClearanceDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.CaptureKursaMoveRightArmClearanceDiagnostic,
                        "Kursa move right-arm clearance diagnostic captured.");
                    break;
                case "CaptureKursaMoveRightArmClearanceFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.CaptureKursaMoveRightArmClearanceFinalReview,
                        "Kursa move right-arm clearance final review captured.");
                    break;
                case "ApplyKursaMoveFaceDeformationFix":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.ApplyKursaMoveFaceDeformationFix,
                        "Kursa move face deformation fix applied.");
                    break;
                case "InspectKursaShieldBashScale":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBashAnimationTool.InspectKursaShieldBashScale,
                        "Kursa shield-bash scale inspected.");
                    break;
                case "ApplyKursaShieldBashScaleMatch":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBashAnimationTool.ApplyKursaShieldBashScaleMatch,
                        "Kursa shield-bash scale matched to the static Kursa.");
                    break;
                case "ApplyKursaShieldBashAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBashAnimationTool.ApplyKursaShieldBashAnimation,
                        "Kursa shield-bash animation applied.");
                    break;
                case "CaptureKursaShieldBashDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBashAnimationTool.CaptureKursaShieldBashDiagnostic,
                        "Kursa shield-bash diagnostic captured.");
                    break;
                case "CaptureKursaShieldBashFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBashAnimationTool.CaptureKursaShieldBashFinalReview,
                        "Kursa shield-bash final review captured.");
                    break;
                case "CaptureKursaShieldBashScaleFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBashAnimationTool.CaptureKursaShieldBashScaleFinalReview,
                        "Kursa shield-bash scale final review captured.");
                    break;
                case "CaptureKursaShieldBashScaleDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBashAnimationTool.CaptureKursaShieldBashScaleDiagnostic,
                        "Kursa shield-bash scale diagnostic captured.");
                    break;
                case "ApplyKursaToShieldStanceAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaToShieldStanceAnimationTool.ApplyKursaToShieldStanceAnimation,
                        "Kursa to-shield-stance animation applied.");
                    break;
                case "CaptureKursaToShieldStanceDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaToShieldStanceAnimationTool.CaptureKursaToShieldStanceDiagnostic,
                        "Kursa to-shield-stance diagnostic captured.");
                    break;
                case "CaptureKursaToShieldStanceFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaToShieldStanceAnimationTool.CaptureKursaToShieldStanceFinalReview,
                        "Kursa to-shield-stance final review captured.");
                    break;
                case "ApplyKursaShieldStanceMoveAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaToShieldStanceAnimationTool.ApplyKursaShieldStanceMoveAnimation,
                        "Kursa shield-stance move animation applied.");
                    break;
                case "CaptureKursaShieldStanceMoveDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaToShieldStanceAnimationTool.CaptureKursaShieldStanceMoveDiagnostic,
                        "Kursa shield-stance move diagnostic captured.");
                    break;
                case "CaptureKursaShieldStanceMoveFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaToShieldStanceAnimationTool.CaptureKursaShieldStanceMoveFinalReview,
                        "Kursa shield-stance move final review captured.");
                    break;
                case "ApplyKursaShieldStanceMoveFbx":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldStanceMoveFbxTool.ApplyKursaShieldStanceMoveFbx,
                        "Kursa shield-stance move FBX applied.");
                    break;
                case "CaptureKursaShieldStanceMoveFbxDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldStanceMoveFbxTool.CaptureKursaShieldStanceMoveFbxDiagnostic,
                        "Kursa shield-stance move FBX diagnostic captured.");
                    break;
                case "CaptureKursaShieldStanceMoveFbxFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldStanceMoveFbxTool.CaptureKursaShieldStanceMoveFbxFinalReview,
                        "Kursa shield-stance move FBX final review captured.");
                    break;
                case "ApplyKursaFromShieldStanceAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaFromShieldStanceAnimationTool.ApplyKursaFromShieldStanceAnimation,
                        "Kursa from-shield-stance animation applied.");
                    break;
                case "ClearKursaFromShieldStanceFailedApplyDirtyState":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaFromShieldStanceAnimationTool.ClearFailedApplyDirtyState,
                        "Kursa from-shield-stance failed apply dirty state cleared.");
                    break;
                case "CaptureKursaFromShieldStanceDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaFromShieldStanceAnimationTool.CaptureKursaFromShieldStanceDiagnostic,
                        "Kursa from-shield-stance diagnostic captured.");
                    break;
                case "CaptureKursaFromShieldStanceFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaFromShieldStanceAnimationTool.CaptureKursaFromShieldStanceFinalReview,
                        "Kursa from-shield-stance final review captured.");
                    break;
                case "ApplyKursaStopAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaStopAnimationTool.ApplyKursaStopAnimation,
                        "Kursa stop animation applied.");
                    break;
                case "CaptureKursaStopDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaStopAnimationTool.CaptureKursaStopDiagnostic,
                        "Kursa stop diagnostic captured.");
                    break;
                case "CaptureKursaStopFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaStopAnimationTool.CaptureKursaStopFinalReview,
                        "Kursa stop final review captured.");
                    break;
                case "InspectKursaStopShieldSkinning":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaStopAnimationTool.InspectKursaStopShieldSkinning,
                        "Kursa stop shield skinning inspected.");
                    break;
                case "ApplyKursaPostBreakRecoveryFbxReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaPostBreakRecoveryFbxTool.ApplyKursaPostBreakRecoveryFbxReplacement,
                        "Kursa post-break recovery FBX replacement applied.");
                    break;
                case "CaptureKursaPostBreakRecoveryFbxDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaPostBreakRecoveryFbxTool.CaptureKursaPostBreakRecoveryFbxDiagnostic,
                        "Kursa post-break recovery FBX diagnostic captured.");
                    break;
                case "CaptureKursaPostBreakRecoveryFbxFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaPostBreakRecoveryFbxTool.CaptureKursaPostBreakRecoveryFbxFinalReview,
                        "Kursa post-break recovery FBX final review captured.");
                    break;
                case "ApplyKursaHitFbxReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaHitFbxTool.ApplyKursaHitFbxReplacement,
                        "Kursa hit FBX replacement applied.");
                    break;
                case "CaptureKursaHitFbxDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaHitFbxTool.CaptureKursaHitFbxDiagnostic,
                        "Kursa hit FBX diagnostic captured.");
                    break;
                case "CaptureKursaHitFbxFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaHitFbxTool.CaptureKursaHitFbxFinalReview,
                        "Kursa hit FBX final review captured.");
                    break;
                case "ApplyKursaDeathFbxReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaDeathFbxTool.ApplyKursaDeathFbxReplacement,
                        "Kursa death FBX replacement applied.");
                    break;
                case "CaptureKursaDeathFbxDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaDeathFbxTool.CaptureKursaDeathFbxDiagnostic,
                        "Kursa death FBX diagnostic captured.");
                    break;
                case "CaptureKursaDeathFbxFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaDeathFbxTool.CaptureKursaDeathFbxFinalReview,
                        "Kursa death FBX final review captured.");
                    break;
                case "ApplyKursaShieldBreakFbxReplacement":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBreakFbxTool.ApplyKursaShieldBreakFbxReplacement,
                        "Kursa shield-break FBX replacement applied.");
                    break;
                case "CaptureKursaShieldBreakFbxDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBreakFbxTool.CaptureKursaShieldBreakFbxDiagnostic,
                        "Kursa shield-break FBX diagnostic captured.");
                    break;
                case "CaptureKursaShieldBreakFbxFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaShieldBreakFbxTool.CaptureKursaShieldBreakFbxFinalReview,
                        "Kursa shield-break FBX final review captured.");
                    break;
                case "CaptureKursaMoveFaceDeformationDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.CaptureKursaMoveFaceDeformationDiagnostic,
                        "Kursa move face deformation diagnostic captured.");
                    break;
                case "CaptureKursaMoveFaceDeformationFinalReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaMoveAnimationTool.CaptureKursaMoveFaceDeformationFinalReview,
                        "Kursa move face deformation final review captured.");
                    break;
                case "InstallKursaFbxExporterDependency":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaStaticFbxExportTool.InstallKursaFbxExporterDependency,
                        "Kursa FBX Exporter dependency installed.");
                    break;
                case "ExportKursaStaticFbx":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaStaticFbxExportTool.ExportKursaStaticFbx,
                        "Kursa static rigged FBX exported.");
                    break;
                case "ApplyKursaForwardHeadAlignment":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.ApplyKursaForwardHeadAlignment,
                        "Kursa forward head alignment applied.");
                    break;
                case "InspectKursaForwardHeadAlignment":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.InspectKursaForwardHeadAlignment,
                        "Kursa forward head alignment inspected.");
                    break;
                case "CaptureKursaForwardHeadAlignmentDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.CaptureKursaForwardHeadAlignmentDiagnostic,
                        "Kursa forward head alignment diagnostic captured.");
                    break;
                case "CaptureKursaForwardHeadAlignmentReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.CaptureKursaForwardHeadAlignmentReview,
                        "Kursa forward head alignment review captured.");
                    break;
                case "InspectKursaEyeShapeCorrection":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.InspectKursaEyeShapeCorrection,
                        "Kursa eye shape correction inspected.");
                    break;
                case "CaptureKursaEyeShapeDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.CaptureKursaEyeShapeDiagnostic,
                        "Kursa eye shape diagnostic captured.");
                    break;
                case "CaptureKursaEyeShapeReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.CaptureKursaEyeShapeReview,
                        "Kursa eye shape review captured.");
                    break;
                case "InspectKursaChinAlignment":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.InspectKursaChinAlignment,
                        "Kursa chin alignment inspected.");
                    break;
                case "CaptureKursaChinAlignmentDiagnostic":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.CaptureKursaChinAlignmentDiagnostic,
                        "Kursa chin alignment diagnostic captured.");
                    break;
                case "CaptureKursaChinAlignmentReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.KursaCargoRunScene.KursaForwardHeadAlignmentTool.CaptureKursaChinAlignmentReview,
                        "Kursa chin alignment review captured.");
                    break;
                case "ValidatePahurGroundedIdleAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurGroundedIdleAnimationTool.ValidatePahurGroundedIdleAnimation,
                        "Pahur grounded idle animation validation passed.");
                    break;
                case "CapturePahurGroundedIdleAnimationReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurGroundedIdleAnimationTool.CapturePahurGroundedIdleAnimationReview,
                        "Pahur grounded idle animation review captured.");
                    break;
                case "ApplyPahurStopAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurStopAnimationTool.ApplyPahurStopAnimation,
                        "Pahur stop animation applied.");
                    break;
                case "ValidatePahurStopAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurStopAnimationTool.ValidatePahurStopAnimation,
                        "Pahur stop animation validation passed.");
                    break;
                case "CapturePahurStopAnimationReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurStopAnimationTool.CapturePahurStopAnimationReview,
                        "Pahur stop animation review captured.");
                    break;
                case "ApplyPahurElevenSlotExpansion":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurElevenSlotExpansionTool.ApplyPahurElevenSlotExpansion,
                        "Pahur eleven-slot expansion applied.");
                    break;
                case "ValidatePahurElevenSlotExpansion":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurElevenSlotExpansionTool.ValidatePahurElevenSlotExpansion,
                        "Pahur eleven-slot expansion validation passed.");
                    break;
                case "ApplyPahurToGuardianStanceAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurToGuardianStanceAnimationTool.ApplyPahurToGuardianStanceAnimation,
                        "Pahur to-guardian-stance animation applied.");
                    break;
                case "ValidatePahurToGuardianStanceAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurToGuardianStanceAnimationTool.ValidatePahurToGuardianStanceAnimation,
                        "Pahur to-guardian-stance animation validation passed.");
                    break;
                case "CapturePahurToGuardianStanceReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurToGuardianStanceAnimationTool.CapturePahurToGuardianStanceReview,
                        "Pahur to-guardian-stance review captured.");
                    break;
                case "ApplyPahurFromGuardianStanceAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurFromGuardianStanceAnimationTool.ApplyPahurFromGuardianStanceAnimation,
                        "Pahur from-guardian-stance animation applied.");
                    break;
                case "ValidatePahurFromGuardianStanceAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurFromGuardianStanceAnimationTool.ValidatePahurFromGuardianStanceAnimation,
                        "Pahur from-guardian-stance animation validation passed.");
                    break;
                case "CapturePahurFromGuardianStanceReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurFromGuardianStanceAnimationTool.CapturePahurFromGuardianStanceReview,
                        "Pahur from-guardian-stance review captured.");
                    break;
                case "InspectPahurHitSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.InspectPahurHitSource,
                        "Pahur hit source inspected.");
                    break;
                case "ApplyPahurHitAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ApplyPahurHitAnimation,
                        "Pahur hit animation applied.");
                    break;
                case "ValidatePahurHitAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ValidatePahurHitAnimation,
                        "Pahur hit animation validation passed.");
                    break;
                case "CapturePahurHitReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.CapturePahurHitReview,
                        "Pahur hit review captured.");
                    break;
                case "InspectPahurDeathSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.InspectPahurDeathSource,
                        "Pahur death source inspected.");
                    break;
                case "ApplyPahurDeathAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ApplyPahurDeathAnimation,
                        "Pahur death animation applied.");
                    break;
                case "ValidatePahurDeathAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ValidatePahurDeathAnimation,
                        "Pahur death animation validation passed.");
                    break;
                case "InspectPahurDeathVerticalMotion":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.InspectPahurDeathVerticalMotion,
                        "Pahur death vertical motion inspected.");
                    break;
                case "CapturePahurDeathReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.CapturePahurDeathReview,
                        "Pahur death review captured.");
                    break;
                case "ApplyPahurRunningModelAndAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ApplyPahurRunningModelAndAnimation,
                        "Pahur running model and animation applied.");
                    break;
                case "InspectPahurRunningSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.InspectPahurRunningSource,
                        "Pahur running source inspected.");
                    break;
                case "InspectPahurMiniFlameAttackSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.InspectPahurMiniFlameAttackSource,
                        "Pahur mini flame attack source inspected.");
                    break;
                case "ApplyPahurMiniFlameAttack":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ApplyPahurMiniFlameAttack,
                        "Pahur mini flame attack applied.");
                    break;
                case "ValidatePahurMiniFlameAttack":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ValidatePahurMiniFlameAttack,
                        "Pahur mini flame attack validated.");
                    break;
                case "CapturePahurMiniFlameAttackReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.CapturePahurMiniFlameAttackReview,
                        "Pahur mini flame attack review captured.");
                    break;
                case "InspectPahurBreakthroughSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.InspectPahurBreakthroughSource,
                        "Pahur breakthrough source inspected.");
                    break;
                case "ApplyPahurBreakthroughFlamethrower":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ApplyPahurBreakthroughFlamethrower,
                        "Pahur breakthrough flamethrower applied.");
                    break;
                case "ValidatePahurBreakthroughFlamethrower":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ValidatePahurBreakthroughFlamethrower,
                        "Pahur breakthrough flamethrower validated.");
                    break;
                case "CapturePahurBreakthroughFlamethrowerReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.CapturePahurBreakthroughFlamethrowerReview,
                        "Pahur breakthrough flamethrower review captured.");
                    break;
                case "InspectPahurGuardianSource":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.InspectPahurGuardianSource,
                        "Pahur guardian source inspected.");
                    break;
                case "ApplyPahurGuardianFlamethrower":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ApplyPahurGuardianFlamethrower,
                        "Pahur guardian flamethrower applied.");
                    break;
                case "ValidatePahurGuardianFlamethrower":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ValidatePahurGuardianFlamethrower,
                        "Pahur guardian flamethrower validated.");
                    break;
                case "AlignPahurMoveModelY":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.AlignPahurMoveModelY,
                        "Pahur move model Y aligned.");
                    break;
                case "ValidatePahurRunningModelAndAnimation":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.ValidatePahurRunningModelAndAnimation,
                        "Pahur running model and animation validation passed.");
                    break;
                case "CapturePahurRunningModelAndAnimationReview":
                    RunSynchronous(
                        request,
                        global::Bellerophon.Editor.PahurCargoRunScene.PahurRunningModelAndAnimationTool.CapturePahurRunningModelAndAnimationReview,
                        "Pahur running model and animation review captured.");
                    break;
                case "EnsureApprovedEngineRoomShell":
                    RunSynchronous(
                        request,
                        ApprovedEngineRoomShellBootstrap.EnsureApprovedEngineRoomShell,
                        "Approved engine room 01 shell applied.");
                    break;
                case "EnsureApprovedArmoryShell":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.EnsureApprovedArmoryShell,
                        "Approved armory 01 shell applied.");
                    break;
                case "CreateApprovedSupplyRoomShell":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.CreateApprovedSupplyRoomShell,
                        "Approved supply room 01 shell created.");
                    break;
                case "CreateApprovedCargoHoldShell":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.CreateApprovedCargoHoldShell,
                        "Approved cargo hold 01 shell created.");
                    break;
                case "AddApprovedCargoHoldCh10DirectionMarkersOnly":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.AddApprovedCargoHoldCh10DirectionMarkersOnly,
                        "Approved cargo hold CH-10 direction markers added only.");
                    break;
                case "UpdateApprovedCargoHoldEntranceColorsOnly":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.UpdateApprovedCargoHoldEntranceColorsOnly,
                        "Approved cargo hold entrance colors updated only.");
                    break;
                case "UpdateApprovedCargoHoldCh11DisplayOnly":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.UpdateApprovedCargoHoldCh11DisplayOnly,
                        "Approved cargo hold CH-11 display updated only.");
                    break;
                case "AddApprovedShipCorridorSegmentsOnly":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.AddApprovedShipCorridorSegmentsOnly,
                        "Approved ship corridor segments added only.");
                    break;
                case "CaptureApprovedShipCorridorSegmentsState":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.CaptureApprovedShipCorridorSegmentsState,
                        "Approved ship corridor segments current state captured.");
                    break;
                case "BackupApprovedShipCorridorSegmentsOnly":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.BackupApprovedShipCorridorSegmentsOnly,
                        "Approved ship corridor segments backup captured.");
                    break;
                case "RestoreApprovedShipCorridorSegmentsCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.RestoreApprovedShipCorridorSegmentsCurrentState,
                        "Approved ship corridor segments current state restored and saved.");
                    break;
                case "ApplyApprovedParvumEnemyPlacement":
                    RunSynchronous(
                        request,
                        ApprovedParvumEnemyUnityPlacementBootstrap.ApplyApprovedParvumEnemyPlacement,
                        "Approved Parvum enemy placement applied.");
                    break;
                case "ValidateApprovedParvumEnemyPlacement":
                    RunSynchronous(
                        request,
                        ApprovedParvumEnemyUnityPlacementBootstrap.ValidateApprovedParvumEnemyPlacement,
                        "Approved Parvum enemy placement validated.");
                    break;
                case "ApplyApprovedParvumToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumCargoRunSceneApplyAndReview.ApplyApprovedSampleToCurrentCargoRunScene,
                        "Approved Parvum sample applied to current CargoRunMvp scene.");
                    break;
                case "ApplyParvumGlbReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumGlbReplacementTool.ApplyParvumGlbReplacement,
                        "The supplied Parvum GLB replaced the existing visible models, matched the current Ispant X spacing, and updated the Player start framing.");
                    break;
                case "CaptureParvumGlbReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumGlbReplacementTool.CaptureParvumGlbReplacement,
                        "The final supplied Parvum GLB lineup was captured from the actual Player start camera.");
                    break;
                case "ApplyParvumTergoVisibleGapAndYAlignment":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumGlbReplacementTool.ApplyParvumTergoVisibleGapAndYAlignment,
                        "Parvum active renderer-bound gaps and ground height were aligned to the current Tergo lineup, and the Player start framing was updated.");
                    break;
                case "CaptureParvumTergoVisibleGapAndYAlignment":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumGlbReplacementTool.CaptureParvumTergoVisibleGapAndYAlignment,
                        "The Tergo-referenced Parvum visible-gap and ground-aligned lineup was captured from the actual Player start camera.");
                    break;
                case "ApplyParvumIdleBreathing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumIdleBreathingTool.ApplyParvumIdleBreathing,
                        "The new two-second, 2.5-percent full-body BlendShape breathing loop was applied only to Parvum_01_Idle.");
                    break;
                case "InspectParvumIdleBreathing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumIdleBreathingTool.InspectParvumIdleBreathing,
                        "The Parvum idle full-body breathing assets and current scene binding were inspected.");
                    break;
                case "CaptureParvumIdleBreathingComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumIdleBreathingTool.CaptureParvumIdleBreathingComparison,
                        "The final five-panel Parvum idle breathing comparison was captured without changing the scene.");
                    break;
                case "ApplyParvumMoveMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumMoveMotionTool.ApplyParvumMoveMotion,
                        "The new-model Parvum forward slime movement was applied only to Parvum_02_Move.");
                    break;
                case "InspectParvumMoveMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumMoveMotionTool.InspectParvumMoveMotion,
                        "The new-model Parvum forward slime movement assets and scene binding were inspected.");
                    break;
                case "CaptureParvumMoveMouthRootRigIdentification":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumMoveMotionTool.CaptureParvumMoveMouthRootRigIdentification,
                        "The Parvum body-side mouth-root rig candidates were captured without changing the scene.");
                    break;
                case "CaptureParvumMoveMotionComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumMoveMotionTool.CaptureParvumMoveMotionComparison,
                        "The final five-panel new-model Parvum move comparison was captured without changing the scene.");
                    break;
                case "ApplyParvumAttackMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumAttackMotionTool.ApplyParvumAttackMotion,
                        "The new-model Parvum wide-open forward bite attack was applied only to Parvum_03_Attack.");
                    break;
                case "InspectParvumAttackMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumAttackMotionTool.InspectParvumAttackMotion,
                        "The new-model Parvum bite attack assets, mouth roots, physics binding, and scene scope were inspected.");
                    break;
                case "InspectParvumAttackOuterLipRegion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumAttackMotionTool.InspectParvumAttackOuterLipRegion,
                        "The Parvum attack outer-lip rig and vertex regions were identified without changing the scene.");
                    break;
                case "CaptureParvumAttackMotionComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumAttackMotionTool.CaptureParvumAttackMotionComparison,
                        "The final five-panel new-model Parvum bite attack comparison was captured without changing the scene.");
                    break;
                case "ApplyParvumHitMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumHitMotionTool.ApplyParvumHitMotion,
                        "The new three-second Parvum left-crush hit motion was applied only to Parvum_04_Hit.");
                    break;
                case "InspectParvumHitMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumHitMotionTool.InspectParvumHitMotion,
                        "The new Parvum left-crush and single object-left head-shake motion was inspected.");
                    break;
                case "CaptureParvumHitMotionComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumHitMotionTool.CaptureParvumHitMotionComparison,
                        "The final six-panel Parvum hit motion comparison was captured without changing the scene.");
                    break;
                case "ApplyParvumDeathMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumDeathMotionTool.ApplyParvumDeathMotion,
                        "The three-second whole-body melt and one-second melted-body hold motion was applied only to Parvum_05_Death, with the legacy puddle visual removed.");
                    break;
                case "InspectParvumDeathMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumDeathMotionTool.InspectParvumDeathMotion,
                        "The Parvum whole-body melt, one-second melted-body hold, removed puddle visual, loop timing, physics, and scene scope were inspected.");
                    break;
                case "CaptureParvumDeathMotionComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ParvumCargoRunScene.ParvumDeathMotionTool.CaptureParvumDeathMotionComparison,
                        "The final seven-panel Parvum whole-body melt and melted-body hold comparison was captured without changing the scene.");
                    break;
                case "ApplyApprovedFugaToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaCargoRunSceneApplyAndReview.ApplyApprovedSampleToCurrentCargoRunScene,
                        "Approved Fuga sample applied to current CargoRunMvp scene.");
                    break;
                case "InspectApprovedFugaCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaCargoRunSceneApplyAndReview.InspectAppliedSceneState,
                        "Approved Fuga CargoRunMvp scene state inspected.");
                    break;
                case "ApplyFugaModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.ApplyFugaModelReplacement,
                        "The exact supplied Fuga GLB replaced every placed Fuga model and updated the Player start framing.");
                    break;
                case "InspectFugaModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.InspectFugaModelReplacement,
                        "The exact supplied Fuga GLB instances, preserved slot contracts, prefab, and Player start were inspected.");
                    break;
                case "CaptureFugaModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.CaptureFugaModelReplacement,
                        "The exact supplied Fuga GLB lineup was captured from the Player start camera.");
                    break;
                case "ApplyFugaFacingAndDisconnectLegacyAnimations":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.ApplyFugaFacingAndDisconnectLegacyAnimations,
                        "Every placed Fuga model was rotated 180 degrees toward the Player start side and all legacy animation playback connections were removed.");
                    break;
                case "InspectFugaFacingAndDisconnectedAnimations":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.InspectFugaFacingAndDisconnectedAnimations,
                        "The Fuga 180-degree facing, static local positions, disconnected legacy animation playback, and protected scene scope were inspected.");
                    break;
                case "CaptureFugaFacingAndDisconnectedAnimations":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.CaptureFugaFacingAndDisconnectedAnimations,
                        "The final static Fuga front-facing lineup was captured from the unchanged Player start camera.");
                    break;
                case "InspectFugaRotationPivotAndPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.InspectFugaRotationPivotAndPlacement,
                        "The Fuga placement root, named slots, model pivots, and rotation ownership were inspected.");
                    break;
                case "RestoreFugaPlacementAndApplyPerObject180Facing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.RestoreFugaPlacementAndApplyPerObject180Facing,
                        "Every Fuga model was reverted to identity and rotated 180 degrees around its own unchanged local pivot.");
                    break;
                case "InspectCorrectedFugaPerObjectFacing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.InspectCorrectedFugaPerObjectFacing,
                        "The corrected per-object Fuga facing and unchanged placement order were inspected.");
                    break;
                case "CaptureCorrectedFugaPerObjectFacing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.CaptureCorrectedFugaPerObjectFacing,
                        "The corrected per-object Fuga facing lineup was captured without changing placement or Player.");
                    break;
                case "ApplyFugaScreenLeftToRightOrder":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.ApplyFugaScreenLeftToRightOrder,
                        "The seven Fuga slots were reordered left-to-right on the Player screen while preserving their state ownership.");
                    break;
                case "InspectFugaScreenLeftToRightOrder":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.InspectFugaScreenLeftToRightOrder,
                        "The Fuga Player-screen order and protected slot state were inspected without changing the scene.");
                    break;
                case "CaptureFugaScreenLeftToRightOrder":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.CaptureFugaScreenLeftToRightOrder,
                        "The final Fuga Player-screen left-to-right order was captured without changing the scene.");
                    break;
                case "ApplyFugaPerObjectFrontFacingPlayerAndOrder":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.ApplyFugaPerObjectFrontFacingPlayerAndOrder,
                        "Each Fuga model was rotated 180 degrees on its own pivot, the Player was moved to the corrected front, and the state order was restored left-to-right.");
                    break;
                case "InspectFugaPerObjectFrontFacingPlayerAndOrder":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.InspectFugaPerObjectFrontFacingPlayerAndOrder,
                        "The corrected per-object facing, Player start, and Player-screen state order were inspected.");
                    break;
                case "CaptureFugaPerObjectFrontFacingPlayerAndOrder":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaGlbReplacementTool.CaptureFugaPerObjectFrontFacingPlayerAndOrder,
                        "The final corrected Fuga facing and ordered lineup were captured from the corrected Player start.");
                    break;
                case "InspectFugaIdleRigAndBirdReference":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.InspectFugaIdleRigAndBirdReference,
                        "The supplied Fuga rig, skin influences, and approved bird-reference parameters were inspected.");
                    break;
                case "InspectFugaConsumeMouthRig":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaConsumeMotionTool.InspectFugaConsumeMouthRig,
                        "The current Fuga upper and lower mouth skin regions were identified before consume-motion implementation without changing the scene.");
                    break;
                case "ApplyFugaEmbeddedLipRigToAllModels":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaLipRigTool.ApplyFugaEmbeddedLipRigToAllModels,
                        "The embedded upper and lower lip bones were connected to every approved Fuga model and the consume motion was assigned to those bones.");
                    break;
                case "InspectFugaEmbeddedLipRigOnAllModels":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaLipRigTool.InspectFugaEmbeddedLipRigOnAllModels,
                        "Every approved Fuga model and prefab was inspected for the embedded upper and lower lip rig without changing the scene.");
                    break;
                case "ApplyFugaConsumeMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaConsumeMotionTool.ApplyFugaConsumeMotion,
                        "The two-second looping Fuga consume motion with 0.7-Hz wings, 30-degree lean, 60-degree mouth opening, and 0.08-meter Rigidbody bite was applied only to Fuga_06_Consume.");
                    break;
                case "InspectFugaConsumeMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaConsumeMotionTool.InspectFugaConsumeMotion,
                        "The Fuga consume mouth mapping, continuous wing cadence, body lean, Rigidbody bite, return, and loop configuration were inspected.");
                    break;
                case "StartFugaConsumeMotionReviewPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaConsumeMotionTool.StartFugaConsumeMotionReviewPlayback,
                        "The live Unity Game View review started for the looping Fuga consume motion without creating a capture.");
                    break;
                case "StopFugaConsumeMotionReviewPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaConsumeMotionTool.StopFugaConsumeMotionReviewPlayback,
                        "The live Fuga consume motion review completed the required loops and continuous wingbeats without creating a capture.");
                    break;
                case "ApplyFugaIdleMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.ApplyFugaIdleMotion,
                        "The new two-second Fuga wingbeat, breathing, and Rigidbody hover idle motion was applied.");
                    break;
                case "InspectFugaIdleMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.InspectFugaIdleMotion,
                        "The new Fuga idle motion assets, scene connection, and protected scope were inspected.");
                    break;
                case "CaptureFugaIdleMotionComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.CaptureFugaIdleMotionComparison,
                        "The new Fuga idle motion phase comparison was captured without changing the scene.");
                    break;
                case "ApplyFugaIdleWingbeatAndHover1Hz":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.ApplyFugaIdleWingbeatAndHover1Hz,
                        "The Fuga idle wingbeat and Rigidbody hover cadence were synchronized to one cycle per second.");
                    break;
                case "InspectFugaIdleWingbeatAndHover1Hz":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.InspectFugaIdleWingbeatAndHover1Hz,
                        "The one-Hertz Fuga wingbeat, matching hover cadence, and preserved idle-motion scope were inspected.");
                    break;
                case "CaptureFugaIdleWingbeatAndHover1Hz":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.CaptureFugaIdleWingbeatAndHover1Hz,
                        "The final one-Hertz Fuga idle wingbeat phase comparison was captured without changing the scene.");
                    break;
                case "ApplyFugaMoveMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaMoveMotionTool.ApplyFugaMoveMotion,
                        "The new stationary Fuga move flight motion and five-meter Player start were applied.");
                    break;
                case "InspectFugaMoveMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaMoveMotionTool.InspectFugaMoveMotion,
                        "The Fuga move wingbeat, forward tilt, Rigidbody hover, stationary position, and Player distance were inspected.");
                    break;
                case "CaptureFugaMoveMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaMoveMotionTool.CaptureFugaMoveMotion,
                        "The final Fuga move-motion phase comparison was captured without changing the scene.");
                    break;
                case "ApplyFugaMoveTilt15":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaMoveMotionTool.ApplyFugaMoveTilt15,
                        "The Fuga move body tilt was reduced to 15 degrees while preserving the inherited wing tilt and flap curves.");
                    break;
                case "InspectFugaMoveTilt15":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaMoveMotionTool.InspectFugaMoveTilt15,
                        "The 15-degree Fuga move body and inherited wing tilt were inspected without changing the scene or controller.");
                    break;
                case "CaptureFugaMoveTilt15":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaMoveMotionTool.CaptureFugaMoveTilt15,
                        "The final 15-degree Fuga move-tilt comparison was captured without changing the scene.");
                    break;
                case "ApplyFugaAttackMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.ApplyFugaAttackMotion,
                        "The new one-second alternating-wing Fuga attack motion was applied with a uniform random starting wing.");
                    break;
                case "InspectFugaAttackMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.InspectFugaAttackMotion,
                        "The Fuga alternating attack cadence, 90-degree strikes, 20-degree body tilt, and fixed altitude were inspected.");
                    break;
                case "CaptureFugaAttackMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.CaptureFugaAttackMotion,
                        "The final Fuga alternating-wing attack phase comparison was captured without changing the scene.");
                    break;
                case "ApplyFugaAttackBodyYaw90":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.ApplyFugaAttackBodyYaw90,
                        "The Fuga attack body yaw was set to 90 degrees with simultaneous inherited wing motion, and missing scene JiggleRig roots were repaired.");
                    break;
                case "InspectFugaAttackBodyYaw90":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.InspectFugaAttackBodyYaw90,
                        "The Fuga attack 90-degree body yaw, simultaneous wing motion, and scene JiggleRig roots were inspected.");
                    break;
                case "CaptureFugaAttackBodyYaw90":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.CaptureFugaAttackBodyYaw90,
                        "The final Fuga attack 90-degree body-yaw comparison was captured without changing the scene.");
                    break;
                case "ApplyFugaAttackJerkDrivenAcceleration":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.ApplyFugaAttackJerkDrivenAcceleration,
                        "Each Fuga impact now moves Fuga_Model and both child wings through a +0.1 to -0.1 meter vertical recoil and returns in 0.07 seconds without moving the Rigidbody root.");
                    break;
                case "InspectFugaAttackJerkDrivenAcceleration":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.InspectFugaAttackJerkDrivenAcceleration,
                        "Both Fuga impacts were inspected for exact +0.1 and -0.1 meter Fuga_Model recoil, 0.07-second return, child-wing inheritance, unchanged Rigidbody root, and preserved attack motion.");
                    break;
                case "StartFugaAttackLeftFirstMotionReviewPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.StartFugaAttackLeftFirstMotionReviewPlayback,
                        "The live Unity Game View review started for two complete left-first Fuga attack loops without creating a capture.");
                    break;
                case "StartFugaAttackRightFirstMotionReviewPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.StartFugaAttackRightFirstMotionReviewPlayback,
                        "The live Unity Game View review started for two complete right-first Fuga attack loops without creating a capture.");
                    break;
                case "StopFugaAttackMotionReviewPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.StopFugaAttackMotionReviewPlayback,
                        "The live Fuga attack motion review stopped and reported completed loop counts without creating a capture.");
                    break;
                case "CaptureFugaAttackJerkDrivenAcceleration":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.CaptureFugaAttackJerkDrivenAcceleration,
                        "The final jerk-driven Fuga acceleration comparison was captured without changing the scene.");
                    break;
                case "ApplyFugaDeathFallAndMelt":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaDeathMotionTool.ApplyFugaDeathFallAndMelt,
                        "The Fuga death slot now stops wing flapping, falls by Rigidbody gravity, melts its body and both wings only after ground contact, holds for one second, and loops.");
                    break;
                case "InspectFugaDeathFallAndMelt":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaDeathMotionTool.InspectFugaDeathFallAndMelt,
                        "The Fuga collision-driven fall, Parvum-matched melt curve, whole-body wing inclusion, hold, and loop reset were inspected.");
                    break;
                case "StartFugaDeathMotionReviewPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaDeathMotionTool.StartFugaDeathMotionReviewPlayback,
                        "The live Unity Game View physics review started for the looping Fuga death motion without creating a capture.");
                    break;
                case "StopFugaDeathMotionReviewPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaDeathMotionTool.StopFugaDeathMotionReviewPlayback,
                        "The live Fuga death motion review completed at least two full fall-impact-melt loops without creating a capture.");
                    break;
                case "ApplyFugaHitReaction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaHitReactionMotionTool.ApplyFugaHitReaction,
                        "The new random left/right Fuga hit reaction was applied without using the legacy hit animation.");
                    break;
                case "InspectFugaHitReaction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaHitReactionMotionTool.InspectFugaHitReaction,
                        "The Fuga hit roll, vertical recoil, 0.3-second return, wing inheritance, and 50:50 selection were inspected.");
                    break;
                case "CaptureFugaHitReaction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaHitReactionMotionTool.CaptureFugaHitReaction,
                        "The final left/right Fuga hit-reaction comparison was captured without changing the scene.");
                    break;
                case "InspectCurrentUnityConsoleErrorsForFugaAttack":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaAttackMotionTool.InspectCurrentUnityConsoleErrorsForFugaAttack,
                        "The current Unity console counts and scene JiggleRig root state were inspected for the Fuga attack task.");
                    break;
                case "InspectFugaIdleDeathVisualIdentity":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.InspectFugaIdleDeathVisualIdentity,
                        "The Fuga Idle/Death ownership, screen positions, and labels were inspected without changing the scene.");
                    break;
                case "CaptureFugaIdleDeathIdentityComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.FugaCargoRunScene.FugaIdleMotionTool.CaptureFugaIdleDeathIdentityComparison,
                        "The Player-view Fuga Idle/Death identity comparison was captured without changing the scene.");
                    break;
                case "ApplyApprovedLongaArmaToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaCargoRunSceneApplyAndReview.ApplyApprovedSampleToCurrentCargoRunScene,
                        "Approved Longa Arma sample applied to current CargoRunMvp scene.");
                    break;
                case "InspectApprovedLongaArmaCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaCargoRunSceneApplyAndReview.InspectAppliedSceneState,
                        "Approved Longa Arma CargoRunMvp scene state inspected.");
                    break;
                case "ApplyPreparedUrzereToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ApplyPreparedModelToCurrentCargoRunScene,
                        "Prepared Urzere model applied to current CargoRunMvp scene.");
                    break;
                case "InspectPreparedUrzereCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.InspectAppliedSceneState,
                        "Prepared Urzere CargoRunMvp scene state inspected.");
                    break;
                case "ApplyPreparedAccelerandoToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ApplyPreparedModelToCurrentCargoRunScene,
                        "Prepared Accelerando model applied to current CargoRunMvp scene.");
                    break;
                case "ApplyApprovedAccelerandoRiggedModelToAllPlacements":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ApplyApprovedRiggedModelToAllPlacements,
                        "Approved Accelerando rigged model applied to all placements.");
                    break;
                case "ValidateApprovedAccelerandoRiggedModelAllPlacements":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ValidateApprovedRiggedModelAllPlacements,
                        "Approved Accelerando rigged model validated for all placements.");
                    break;
                case "CaptureApprovedAccelerandoRiggedModelAllPlacements":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureApprovedRiggedModelAllPlacements,
                        "Approved Accelerando rigged model comparison captures completed.");
                    break;
                case "RotateApprovedAccelerandoRiggedModelsToBackFacing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.RotateApprovedRiggedModelsToBackFacing,
                        "Approved Accelerando rigged models rotated to back-facing.");
                    break;
                case "ValidateApprovedAccelerandoRiggedModelsBackFacing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ValidateApprovedRiggedModelsBackFacing,
                        "Approved Accelerando rigged models back-facing direction validated.");
                    break;
                case "CaptureApprovedAccelerandoRiggedModelsBackFacing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureApprovedRiggedModelsBackFacing,
                        "Approved Accelerando rigged models back-facing review captured.");
                    break;
                case "ApplyApprovedAccelerandoForwardMaceStrikeMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ApplyApprovedAccelerandoForwardMaceStrikeMotion,
                        "Approved Accelerando forward mace strike motion applied.");
                    break;
                case "InspectApprovedAccelerandoAttackAntennaSkinConstraints":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.InspectApprovedAccelerandoAttackAntennaSkinConstraints,
                        "Approved Accelerando attack antenna skin constraints inspected.");
                    break;
                case "ValidateApprovedAccelerandoForwardMaceStrikeMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ValidateApprovedAccelerandoForwardMaceStrikeMotion,
                        "Approved Accelerando forward mace strike motion validated.");
                    break;
                case "CaptureApprovedAccelerandoForwardMaceStrikeMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureApprovedAccelerandoForwardMaceStrikeMotion,
                        "Approved Accelerando forward mace strike motion captured.");
                    break;
                case "ApplyApprovedAccelerandoHitRecoilMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ApplyApprovedAccelerandoHitRecoilMotion,
                        "Approved Accelerando hit recoil motion applied.");
                    break;
                case "ValidateApprovedAccelerandoHitRecoilMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ValidateApprovedAccelerandoHitRecoilMotion,
                        "Approved Accelerando hit recoil motion validated.");
                    break;
                case "CaptureApprovedAccelerandoHitRecoilMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureApprovedAccelerandoHitRecoilMotion,
                        "Approved Accelerando hit recoil motion captured.");
                    break;
                case "ApplyApprovedAccelerandoDeathCollapseMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ApplyApprovedAccelerandoDeathCollapseMotion,
                        "Approved Accelerando death collapse motion applied.");
                    break;
                case "ValidateApprovedAccelerandoDeathCollapseMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ValidateApprovedAccelerandoDeathCollapseMotion,
                        "Approved Accelerando death collapse motion validated.");
                    break;
                case "CaptureApprovedAccelerandoDeathCollapseMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureApprovedAccelerandoDeathCollapseMotion,
                        "Approved Accelerando death collapse motion captured.");
                    break;
                case "ApplyApprovedGravePlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyApprovedGravePlacement,
                        "Approved Grave static placement applied.");
                    break;
                case "MoveApprovedGravePlayerStartToOppositeSide":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.MoveApprovedGravePlayerStartToOppositeSide,
                        "Approved Grave Player start moved to the opposite side.");
                    break;
                case "ValidateApprovedGravePlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ValidateApprovedGravePlacement,
                        "Approved Grave static placement validated.");
                    break;
                case "CaptureApprovedGravePlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureApprovedGravePlacement,
                        "Approved Grave static placement captured.");
                    break;
                case "ArrangeApprovedGraveAnimationSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ArrangeApprovedGraveAnimationSlots,
                        "Approved Grave animation slots arranged.");
                    break;
                case "ValidateApprovedGraveAnimationSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ValidateApprovedGraveAnimationSlots,
                        "Approved Grave animation slots validated.");
                    break;
                case "CaptureApprovedGraveAnimationSlotLayout":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureApprovedGraveAnimationSlotLayout,
                        "Approved Grave animation slot layout captured.");
                    break;
                case "ApplyApprovedGraveReproduction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyApprovedGraveReproduction,
                        "Approved Grave reproduction applied to all slots.");
                    break;
                case "ValidateApprovedGraveReproduction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ValidateApprovedGraveReproduction,
                        "Approved Grave reproduction validated for all slots.");
                    break;
                case "CaptureApprovedGraveReproduction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureApprovedGraveReproduction,
                        "Approved Grave reproduction review captured.");
                    break;
                case "ApplyApprovedGraveBackFacingRotation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyApprovedGraveBackFacingRotation,
                        "Approved Grave models rotated 180 degrees to the back-facing direction.");
                    break;
                case "InspectApprovedGraveIdleRig":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.InspectApprovedGraveIdleRig,
                        "Approved Grave idle rig inspected.");
                    break;
                case "ApplyApprovedGraveIdleBreathing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyApprovedGraveIdleBreathing,
                        "Approved Grave idle breathing applied.");
                    break;
                case "ValidateApprovedGraveIdleBreathing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ValidateApprovedGraveIdleBreathing,
                        "Approved Grave idle breathing validated.");
                    break;
                case "CaptureApprovedGraveIdleBreathing":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureApprovedGraveIdleBreathing,
                        "Approved Grave idle breathing review captured.");
                    break;
                case "ApplyApprovedGraveSlowWalk":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyApprovedGraveSlowWalk,
                        "Approved Grave slow walk applied.");
                    break;
                case "ValidateApprovedGraveSlowWalk":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ValidateApprovedGraveSlowWalk,
                        "Approved Grave slow walk validated.");
                    break;
                case "CaptureApprovedGraveSlowWalk":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureApprovedGraveSlowWalk,
                        "Approved Grave slow-walk review captured.");
                    break;
                case "InspectApprovedGraveAttackRig":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.InspectApprovedGraveAttackRig,
                        "Approved Grave attack rig inspected.");
                    break;
                case "ApplyApprovedGraveCurtainCallAttack":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyApprovedGraveCurtainCallAttack,
                        "Approved Grave curtain-call attack applied.");
                    break;
                case "ApplyRestoredGraveAttackFromUserVideo214018":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyRestoredGraveAttackFromUserVideo214018,
                        "Grave attack restored from user video 21:40:18 and applied to the working slot.");
                    break;
                case "ValidateApprovedGraveCurtainCallAttack":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ValidateApprovedGraveCurtainCallAttack,
                        "Approved Grave curtain-call attack validated.");
                    break;
                case "CaptureApprovedGraveCurtainCallAttack":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureApprovedGraveCurtainCallAttack,
                        "Approved Grave curtain-call attack review captured.");
                    break;
                case "CaptureGraveAttackCurtainEndingFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureGraveAttackCurtainEndingFrames,
                        "Grave working curtain-ending frames captured without Scene View focus.");
                    break;
                case "ApplyGraveDeathBackFallWorking":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyGraveDeathBackFallWorking,
                        "Grave working death back-fall motion applied to the death slot.");
                    break;
                case "CaptureGraveDeathBackFallFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureGraveDeathBackFallFrames,
                        "Grave working death back-fall frames captured without Scene View focus.");
                    break;
                case "ApplyGraveDeathReviewLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyGraveDeathReviewLoop,
                        "Grave death review object loop applied without changing the clip loop setting.");
                    break;
                case "CaptureGraveDeathReviewLoopFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureGraveDeathReviewLoopFrames,
                        "Grave death review object loop frames captured without Scene View focus.");
                    break;
                case "ApplySmorzandoScenePlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoCargoRunSceneApplyAndReview.ApplySmorzandoScenePlacement,
                        "Smorzando installed and person review models placed in the CargoRunMvp scene.");
                    break;
                case "InspectSmorzandoMaterialUvState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoCargoRunSceneApplyAndReview.InspectSmorzandoMaterialUvState,
                        "Smorzando FBX material slots and UV state inspected without changing the scene.");
                    break;
                case "InspectSmorzandoInstalledIdleGeometry":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoCargoRunSceneApplyAndReview.InspectSmorzandoInstalledIdleGeometry,
                        "Smorzando installed idle geometry inspected without changing the scene.");
                    break;
                case "ApplySmorzandoInstalledIdle":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoInstalledIdleApplyAndReview.ApplySmorzandoInstalledIdle,
                        "Smorzando installed idle motion and lit flame applied to the three review models.");
                    break;
                case "CaptureSmorzandoInstalledIdleFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoInstalledIdleApplyAndReview.CaptureSmorzandoInstalledIdleFrames,
                        "Smorzando installed idle frames captured without Scene View focus.");
                    break;
                case "InspectSmorzandoModeledFlameGeometry":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoInstalledIdleApplyAndReview.InspectSmorzandoModeledFlameGeometry,
                        "Smorzando modeled flame geometry inspected without changing the scene.");
                    break;
                case "ApplySmorzandoHybridFlame":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoInstalledIdleApplyAndReview.ApplySmorzandoHybridFlame,
                        "Smorzando modeled flame core, black wick, and attached envelope effect applied.");
                    break;
                case "CaptureSmorzandoHybridFlameFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoInstalledIdleApplyAndReview.CaptureSmorzandoHybridFlameFrames,
                        "Smorzando hybrid flame frames captured without Scene View focus.");
                    break;
                case "ApplySmorzandoInstalledTransform":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoInstalledTransformApplyAndReview.ApplySmorzandoInstalledTransform,
                        "Smorzando third installed model transform motion applied.");
                    break;
                case "ExportSmorzandoTransformBakeMesh":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoInstalledTransformApplyAndReview.ExportSmorzandoTransformBakeMesh,
                        "Smorzando transform person Bake Mesh exported from the open scene.");
                    break;
                case "CaptureSmorzandoInstalledTransformFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoInstalledTransformApplyAndReview.CaptureSmorzandoInstalledTransformFrames,
                        "Smorzando installed-to-person transform frames captured without Scene View focus.");
                    break;
                case "ApplySmorzandoPersonIdle":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonIdleApplyAndReview.ApplySmorzandoPersonIdle,
                        "Smorzando person whole-body idle morph applied to the second person review model.");
                    break;
                case "CaptureSmorzandoPersonIdleFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonIdleApplyAndReview.CaptureSmorzandoPersonIdleFrames,
                        "Smorzando person idle morph frames captured without Scene View focus.");
                    break;
                case "InspectSmorzandoPersonWalkingSource":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonWalkApplyAndReview.InspectSmorzandoPersonWalkingSource,
                        "Smorzando person walking FBX structure and animation clips inspected.");
                    break;
                case "ApplySmorzandoPersonWalk":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonWalkApplyAndReview.ApplySmorzandoPersonWalk,
                        "Smorzando walking FBX and synchronized static-person materials applied to the third person review model.");
                    break;
                case "CaptureSmorzandoPersonWalkFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonWalkApplyAndReview.CaptureSmorzandoPersonWalkFrames,
                        "Smorzando person walking frames captured without Scene View focus.");
                    break;
                case "InspectSmorzandoPersonRunningSource":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonRunApplyAndReview.InspectSmorzandoPersonRunningSource,
                        "Smorzando person running FBX structure and animation clips inspected.");
                    break;
                case "ApplySmorzandoPersonRun":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonRunApplyAndReview.ApplySmorzandoPersonRun,
                        "Smorzando running FBX and synchronized static-person materials applied to the fourth person review model.");
                    break;
                case "CaptureSmorzandoPersonRunFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonRunApplyAndReview.CaptureSmorzandoPersonRunFrames,
                        "Smorzando person running frames captured without Scene View focus.");
                    break;
                case "InspectSmorzandoPersonHitTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonHitApplyAndReview.InspectSmorzandoPersonHitTarget,
                        "Smorzando fifth-person hit target rig and current state inspected.");
                    break;
                case "ApplySmorzandoPersonHit":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonHitApplyAndReview.ApplySmorzandoPersonHit,
                        "Smorzando looping recoil-and-return hit motion applied to the fifth person review model.");
                    break;
                case "CaptureSmorzandoPersonHitFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonHitApplyAndReview.CaptureSmorzandoPersonHitFrames,
                        "Smorzando person hit frames captured without Scene View focus.");
                    break;
                case "InspectSmorzandoPersonDeathTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonDeathApplyAndReview.InspectSmorzandoPersonDeathTarget,
                        "Smorzando static-person copy source and sixth death-slot placement inspected.");
                    break;
                case "ApplySmorzandoPersonDeath":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonDeathApplyAndReview.ApplySmorzandoPersonDeath,
                        "Smorzando copied sixth-person looping backward-fall death motion applied.");
                    break;
                case "CaptureSmorzandoPersonDeathFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonDeathApplyAndReview.CaptureSmorzandoPersonDeathFrames,
                        "Smorzando person death frames captured without Scene View focus.");
                    break;
                case "InspectSmorzandoPersonDeathFbxSource":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonDeathFbxApplyAndReview.InspectSmorzandoPersonDeathFbxSource,
                        "Smorzando person death FBX structure and animation clips inspected.");
                    break;
                case "ApplySmorzandoPersonDeathFbx":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonDeathApplyAndReview.ApplySmorzandoPersonDeath,
                        "Smorzando death FBX and synchronized static-person materials applied to the sixth person review model.");
                    break;
                case "CaptureSmorzandoPersonDeathFbxFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoPersonDeathApplyAndReview.CaptureSmorzandoPersonDeathFrames,
                        "Smorzando person death FBX frames captured without Scene View focus.");
                    break;
                case "ApplySmorzandoReferenceColors":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoCargoRunSceneApplyAndReview.ApplySmorzandoReferenceColors,
                        "Smorzando reference-image wax colors applied to all eight review models.");
                    break;
                case "CaptureSmorzandoReferenceColorFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoCargoRunSceneApplyAndReview.CaptureSmorzandoReferenceColorFrames,
                        "Smorzando reference-color frames captured without Scene View focus.");
                    break;
                case "CaptureSmorzandoScenePlacementFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoCargoRunSceneApplyAndReview.CaptureSmorzandoScenePlacementFrames,
                        "Smorzando scene placement frames captured without Scene View focus.");
                    break;
                case "MoveSmorzandoPlayerStartToFront":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoCargoRunSceneApplyAndReview.MoveSmorzandoPlayerStartToFront,
                        "Player start moved to the front of the complete Smorzando review row.");
                    break;
                case "CaptureSmorzandoPlayerStartView":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SmorzandoCargoRunScene.SmorzandoCargoRunSceneApplyAndReview.CaptureSmorzandoPlayerStartView,
                        "Smorzando player-start Main Camera view captured without Scene View focus.");
                    break;
                case "InspectOstinatoPlacementTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoCargoRunScene.OstinatoCargoRunSceneApplyAndReview.InspectOstinatoPlacementTarget,
                        "Ostinato source model, placement anchors, spacing, and player start inspected.");
                    break;
                case "ApplyOstinatoPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoCargoRunScene.OstinatoCargoRunSceneApplyAndReview.ApplyOstinatoPlacement,
                        "Nine Ostinato models and centered player start applied to CargoRunMvp.");
                    break;
                case "InspectOstinatoAppliedGrounding":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoCargoRunScene.OstinatoCargoRunSceneApplyAndReview.InspectOstinatoAppliedGrounding,
                        "Applied Ostinato renderer grounding inspected without scene changes.");
                    break;
                case "CaptureOstinatoPlacementFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoCargoRunScene.OstinatoCargoRunSceneApplyAndReview.CaptureOstinatoPlacementFrames,
                        "Ostinato row and player-start views captured without Scene View focus.");
                    break;
                case "InspectOstinatoHitFbxReplacementTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoHitRecoilAnimation.InspectOstinatoHitFbxReplacementTarget,
                        "Supplied Ostinato hit FBX source hash and Unity default takes inspected without scene changes.");
                    break;
                case "ApplyOstinatoHitFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoHitRecoilAnimation.ApplyOstinatoHitFbxReplacement,
                        "Supplied mixamo.com hit take instantiated in slot 05 with approved static appearance.");
                    break;
                case "InspectOstinatoHitFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoHitRecoilAnimation.InspectOstinatoHitFbxReplacement,
                        "Supplied mixamo.com hit take, direct FBX instance, and approved appearance inspected.");
                    break;
                case "CaptureOstinatoHitFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoHitRecoilAnimation.CaptureOstinatoHitFbxReplacement,
                        "Supplied mixamo.com hit take runtime capture started after replacement inspection.");
                    break;
                case "InspectOstinatoRoarFbxReplacementTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoRoarEnrageTransitionAnimation.InspectOstinatoRoarFbxReplacementTarget,
                        "Supplied Ostinato roar FBX source hash and selected mixamo.com take inspected without scene changes.");
                    break;
                case "ApplyOstinatoRoarFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoRoarEnrageTransitionAnimation.ApplyOstinatoRoarFbxReplacement,
                        "Supplied mixamo.com roar take instantiated in slot 06 with approved static appearance.");
                    break;
                case "InspectOstinatoRoarFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoRoarEnrageTransitionAnimation.InspectOstinatoRoarFbxReplacement,
                        "Supplied mixamo.com roar take, direct FBX instance, and approved appearance inspected.");
                    break;
                case "CaptureOstinatoRoarFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoRoarEnrageTransitionAnimation.CaptureOstinatoRoarFbxReplacement,
                        "Supplied mixamo.com roar take runtime capture started after replacement inspection.");
                    break;
                case "InspectOstinatoSeatedRestFbxReplacementTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoSeatedRestAnimation.InspectOstinatoSeatedRestFbxReplacementTarget,
                        "Supplied Ostinato seated-rest FBX source hash and selected mixamo.com take inspected without scene changes.");
                    break;
                case "ApplyOstinatoSeatedRestFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoSeatedRestAnimation.ApplyOstinatoSeatedRestFbxReplacement,
                        "Supplied mixamo.com seated-rest take instantiated in slot 07 with approved static appearance.");
                    break;
                case "InspectOstinatoSeatedRestFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoSeatedRestAnimation.InspectOstinatoSeatedRestFbxReplacement,
                        "Supplied mixamo.com seated-rest take, direct FBX instance, and approved appearance inspected.");
                    break;
                case "CaptureOstinatoSeatedRestFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoSeatedRestAnimation.CaptureOstinatoSeatedRestFbxReplacement,
                        "Supplied mixamo.com seated-rest take runtime capture started after replacement inspection.");
                    break;
                case "InspectOstinatoStandUpFbxReplacementTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoStandUpAnimation.InspectOstinatoStandUpFbxReplacementTarget,
                        "Supplied Ostinato stand-up FBX source hash and selected mixamo.com take inspected without scene changes.");
                    break;
                case "ApplyOstinatoStandUpFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoStandUpAnimation.ApplyOstinatoStandUpFbxReplacement,
                        "Supplied mixamo.com stand-up take instantiated in slot 08 with approved static appearance.");
                    break;
                case "InspectOstinatoStandUpFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoStandUpAnimation.InspectOstinatoStandUpFbxReplacement,
                        "Supplied mixamo.com stand-up take, direct FBX instance, and approved appearance inspected.");
                    break;
                case "CaptureOstinatoStandUpFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoStandUpAnimation.CaptureOstinatoStandUpFbxReplacement,
                        "Supplied mixamo.com stand-up take runtime capture started after replacement inspection.");
                    break;
                case "InspectOstinatoDeathFbxReplacementTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoDeathAnimation.InspectOstinatoDeathFbxReplacementTarget,
                        "Supplied Ostinato death FBX source hash and selected mixamo.com take inspected without scene changes.");
                    break;
                case "ApplyOstinatoDeathFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoDeathAnimation.ApplyOstinatoDeathFbxReplacement,
                        "Supplied mixamo.com death take instantiated in slot 09 with approved static appearance.");
                    break;
                case "InspectOstinatoDeathFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoDeathAnimation.InspectOstinatoDeathFbxReplacement,
                        "Supplied mixamo.com death take, direct FBX instance, and approved appearance inspected.");
                    break;
                case "CaptureOstinatoDeathFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoDeathAnimation.CaptureOstinatoDeathFbxReplacement,
                        "Supplied mixamo.com death take runtime capture started after replacement inspection.");
                    break;
                case "InspectApprovedOstinatoMaterialTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoApprovedMaterial.OstinatoApprovedMaterialApplyAndReview.InspectApprovedOstinatoMaterialTarget,
                        "Approved Ostinato material target, UV layout, and nine scene slots inspected without scene changes.");
                    break;
                case "ApplyApprovedOstinatoMaterialSample":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoApprovedSample.OstinatoApprovedSampleApplicator.ApplyApprovedOstinatoSampleToCargoRunMvp,
                        "Approved Blender Ostinato mesh and baked PBR materials applied to nine CargoRunMvp scene slots.");
                    break;
                case "CaptureApprovedOstinatoMaterialReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoApprovedMaterial.OstinatoApprovedMaterialApplyAndReview.CaptureApprovedOstinatoMaterialReview,
                        "Approved sample and Unity Ostinato front, side, and back views captured in one comparison image.");
                    break;
                case "InspectApprovedOstinatoMaterialRender":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoApprovedMaterial.OstinatoApprovedMaterialApplyAndReview.InspectApprovedOstinatoMaterialRender,
                        "Unity Ostinato front, side, and back material distribution inspected in memory without image capture.");
                    break;
                case "ApplyOstinatoIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoIdleBreathingAnimation.ApplyOstinatoIdleBreathingAnimation,
                        "Ostinato regional BlendShape and connected-pose idle breathing animation applied to slot 02.");
                    break;
                case "ReviewOstinatoIdleAnimatorPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoIdleBreathingAnimation.ReviewOstinatoIdleAnimatorPlayback,
                        "Ostinato slot 02 idle Animator playback reviewed through one complete loop.");
                    break;
                case "CaptureOstinatoIdleBreathingRuntimePlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoIdleBreathingAnimation.CaptureOstinatoIdleBreathingRuntimePlayback,
                        "Ostinato slot 02 runtime playback capture started in the open Unity editor.");
                    break;
                case "InspectOstinatoWalkingSource":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoWalkingAnimation.InspectOstinatoWalkingSource,
                        "Ostinato walking FBX source, mesh, material slots, rig, and clips inspected without mesh changes.");
                    break;
                case "ApplyOstinatoWalkingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoWalkingAnimation.ApplyOstinatoWalkingAnimation,
                        "Ostinato slot 03 walking loop and approved body-tone material applied without mesh changes.");
                    break;
                case "CaptureOstinatoWalkingRuntimePlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoWalkingAnimation.CaptureOstinatoWalkingRuntimePlayback,
                        "Ostinato slot 03 runtime walking playback capture started in the open Unity editor.");
                    break;
                case "InspectOstinatoScissorAttackTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.InspectOstinatoScissorAttackTarget,
                        "Ostinato slot 04 target, approved appearance, and four-second scissor attack source inspected.");
                    break;
                case "ApplyOstinatoScissorAttackAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.ApplyOstinatoScissorAttackAnimation,
                        "Ostinato slot 04 looping root-locked whole-arm hook attack applied with approved appearance.");
                    break;
                case "ReviewOstinatoScissorAttackAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.ReviewOstinatoScissorAttackAnimation,
                        "Ostinato slot 04 forceful spread, whole-arm blade impact, hook-pull, neutral wrists, loop return, and fixed root reviewed.");
                    break;
                case "CaptureOstinatoScissorAttackRuntimePlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.CaptureOstinatoScissorAttackRuntimePlayback,
                        "Ostinato slot 04 whole-arm hook attack runtime keyframe capture started in the open Unity editor.");
                    break;
                case "ApplyOstinatoAttackFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.ApplyOstinatoAttackFbxReplacement,
                        "Supplied Ostinato attack FBX applied to slot 04 with the approved static appearance.");
                    break;
                case "InspectOstinatoAttackAppearanceSync":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.InspectOstinatoAttackAppearanceSync,
                        "Ostinato slot 04 supplied attack clip bindings and approved static appearance inspected.");
                    break;
                case "CaptureOstinatoAttackAppearanceComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.CaptureOstinatoAttackAppearanceComparison,
                        "Ostinato slot 04 supplied-FBX attack appearance comparison capture started.");
                    break;
                case "ApplyOstinatoAttackFbxUnmodified":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.ApplyOstinatoAttackFbxUnmodified,
                        "Supplied Ostinato attack FBX applied to slot 04 without movement overrides and with approved static appearance.");
                    break;
                case "InspectOstinatoAttackFbxUnmodified":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.InspectOstinatoAttackFbxUnmodified,
                        "Ostinato slot 04 source attack curves, default take settings, bindings, and approved appearance inspected.");
                    break;
                case "ApplyOstinatoAttackDownstrikeAcceleration":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.ApplyOstinatoAttackDownstrikeAcceleration,
                        "Ostinato attack speed-only downstrike acceleration profile applied.");
                    break;
                case "InspectOstinatoAttackDownstrikeAcceleration":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.InspectOstinatoAttackDownstrikeAcceleration,
                        "Ostinato attack speed-only downstrike acceleration profile inspected.");
                    break;
                case "CaptureOstinatoAttackDownstrikeAcceleration":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.CaptureOstinatoAttackDownstrikeAcceleration,
                        "Ostinato attack speed-only downstrike acceleration capture started.");
                    break;
                case "ApplyOstinatoAttackForwardSlashMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackForwardSlashMotion.ApplyOstinatoAttackForwardSlashMotion,
                        "Ostinato attack arm and forearm rotation curves corrected into a forward slash during FBX import.");
                    break;
                case "InspectOstinatoAttackForwardSlashMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackForwardSlashMotion.InspectOstinatoAttackForwardSlashMotion,
                        "Ostinato attack forward-slash import correction inspected.");
                    break;
                case "AnalyzeOstinatoAttackForwardSlashVelocity":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackForwardSlashMotion.AnalyzeOstinatoAttackForwardSlashVelocity,
                        "Ostinato attack forward-slash frame-by-frame hand velocity analyzed.");
                    break;
                case "InspectOstinatoAttackForwardSlashBindings":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackForwardSlashMotion.InspectOstinatoAttackForwardSlashBindings,
                        "Ostinato attack source rotation bindings inspected before import correction.");
                    break;
                case "CaptureOstinatoAttackForwardSlashMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackForwardSlashMotion.CaptureOstinatoAttackForwardSlashMotion,
                        "Ostinato attack forward-slash import correction capture started.");
                    break;
                case "AnalyzeOstinatoAttackMotionSegments":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackMotionSegmentAnalysis.AnalyzeOstinatoAttackMotionSegments,
                        "Ostinato slot 04 attack motion sampled across the full source timeline without modifying the animation.");
                    break;
                case "ApplyOstinatoAttackMotionPhaseCut":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackMotionPhaseCut.ApplyOstinatoAttackMotionPhaseCut,
                        "Ostinato attack frames 0 through 100 preserved and a smooth return-to-default applied after impact.");
                    break;
                case "InspectOstinatoAttackMotionPhaseCut":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackMotionPhaseCut.InspectOstinatoAttackMotionPhaseCut,
                        "Ostinato unchanged attack, smooth return continuity, loop, controller, source integrity, and appearance inspected.");
                    break;
                case "CaptureOstinatoAttackMotionPhaseCut":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackMotionPhaseCut.CaptureOstinatoAttackMotionPhaseCut,
                        "Ostinato unchanged attack and smooth return-to-default contact sheet captured once after numeric inspection.");
                    break;
                case "ApplyOstinatoAttackFbxForwardCloseLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackForwardCloseLoop.ApplyOstinatoAttackFbxForwardCloseLoop,
                        "Ostinato supplied-FBX attack object replaced, approved static appearance synchronized, and frames 0 through 93 loop applied.");
                    break;
                case "InspectOstinatoAttackFbxForwardCloseLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackForwardCloseLoop.InspectOstinatoAttackFbxForwardCloseLoop,
                        "Ostinato direct supplied-FBX instance, approved appearance, and exact frames 0 through 93 loop inspected.");
                    break;
                case "CaptureOstinatoAttackFbxForwardCloseLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackForwardCloseLoop.CaptureOstinatoAttackFbxForwardCloseLoop,
                        "Ostinato frames 0 through 93 forward-close loop contact sheet captured once after numeric inspection.");
                    break;
                case "EnableOstinatoAttackLoopPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.EnableOstinatoAttackLoopPlayback,
                        "Ostinato slot 04 attack loop playback enabled without changing movement curves.");
                    break;
                case "InspectOstinatoAttackLoopPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.InspectOstinatoAttackLoopPlayback,
                        "Ostinato slot 04 loop-only override, source curves, playback speed, and appearance inspected.");
                    break;
                case "ApplyOstinatoAttackHorizontalInwardWrist":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackWristOrientation.ApplyOstinatoAttackHorizontalInwardWrist,
                        "Ostinato attack blade angles corrected horizontally inward without changing the body composition.");
                    break;
                case "InspectOstinatoAttackHorizontalInwardWrist":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackWristOrientation.InspectOstinatoAttackHorizontalInwardWrist,
                        "Ostinato wrist-only blade orientation and unchanged body animation curves inspected.");
                    break;
                case "CaptureOstinatoAttackHorizontalInwardWrist":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackWristOrientation.CaptureOstinatoAttackHorizontalInwardWrist,
                        "Ostinato wrist-only source/corrected exact-frame comparison captured.");
                    break;
                case "CaptureOstinatoAttackFbxUnmodifiedComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoScissorAttackAnimation.CaptureOstinatoAttackFbxUnmodifiedComparison,
                        "Ostinato slot 04 unmodified supplied-FBX attack comparison capture started.");
                    break;
                case "ApplyOstinatoAttackDownstrikeBladeRotation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackDownstrikeBladeRotation.ApplyOstinatoAttackDownstrikeBladeRotation,
                        "Ostinato downstrike-only rigid blade rotation applied.");
                    break;
                case "InspectOstinatoAttackDownstrikeBladeRotation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackDownstrikeBladeRotation.InspectOstinatoAttackDownstrikeBladeRotation,
                        "Ostinato downstrike rigid blade rotation inspected.");
                    break;
                case "CaptureOstinatoAttackDownstrikeBladeRotation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoAttackDownstrikeBladeRotation.CaptureOstinatoAttackDownstrikeBladeRotation,
                        "Ostinato downstrike rigid blade source/corrected comparison captured.");
                    break;
                case "ApplyApprovedGraveHitRecoil":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyApprovedGraveHitRecoil,
                        "Approved Grave hit-recoil motion applied.");
                    break;
                case "ValidateApprovedGraveHitRecoil":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ValidateApprovedGraveHitRecoil,
                        "Approved Grave hit-recoil motion validated.");
                    break;
                case "CaptureApprovedGraveHitRecoil":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureApprovedGraveHitRecoil,
                        "Approved Grave hit-recoil motion captured.");
                    break;
                case "ApplyGraveWalkFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ApplyGraveWalkFbxReplacement,
                        "Grave walk FBX replacement applied.");
                    break;
                case "ValidateGraveWalkFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.ValidateGraveWalkFbxReplacement,
                        "Grave walk FBX replacement validated.");
                    break;
                case "CaptureGraveWalkFbxReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureGraveWalkFbxReplacement,
                        "Grave walk FBX replacement comparison captured.");
                    break;
                case "CaptureApprovedGraveBackFacingRotation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.GraveCargoRunScene.GraveCargoRunSceneApplyAndReview.CaptureApprovedGraveBackFacingRotation,
                        "Approved Grave back-facing 180-degree review captured.");
                    break;
                case "InspectPreparedAccelerandoCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.InspectAppliedSceneState,
                        "Prepared Accelerando CargoRunMvp scene state inspected.");
                    break;
                case "CapturePreparedAccelerandoReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureReview,
                        "Prepared Accelerando static review captured.");
                    break;
                case "CapturePreparedAccelerandoAntennaTipConnectionCloseups":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureAntennaTipConnectionCloseups,
                        "Prepared Accelerando antenna tip connection closeups captured.");
                    break;
                case "CapturePreparedAccelerandoMaceChainCloseups":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureMaceChainCloseups,
                        "Prepared Accelerando mace chain closeups captured.");
                    break;
                case "CapturePreparedAccelerandoAnimationSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureAnimationSlotsReview,
                        "Prepared Accelerando animation slots review captured.");
                    break;
                case "ApplyPreparedAccelerandoCrawlForwardMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ApplyCrawlForwardMotionToCurrentScene,
                        "Prepared Accelerando crawl forward motion applied.");
                    break;
                case "InspectPreparedAccelerandoCrawlForwardMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.InspectCrawlForwardMotionInScene,
                        "Prepared Accelerando crawl forward motion inspected.");
                    break;
                case "ValidatePreparedAccelerandoCrawlChainPhysicsResponse":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ValidateCrawlForwardChainPhysicsResponseInScene,
                        "Prepared Accelerando crawl chain physics response validated.");
                    break;
                case "ApplyPreparedAccelerandoPhysicsAntennaStrikeMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ApplyPhysicsAntennaStrikeMotionToCurrentScene,
                        "Prepared Accelerando physics antenna strike motion applied.");
                    break;
                case "InspectPreparedAccelerandoPhysicsAntennaStrikeMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.InspectPhysicsAntennaStrikeMotionInScene,
                        "Prepared Accelerando physics antenna strike motion inspected.");
                    break;
                case "InspectPreparedAccelerandoForwardMaceSwingAntennaRig":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.InspectForwardMaceSwingAntennaRigInScene,
                        "Prepared Accelerando forward mace swing antenna rig inspected.");
                    break;
                case "ValidatePreparedAccelerandoPhysicsAntennaStrikeResponse":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ValidatePhysicsAntennaStrikeResponseInScene,
                        "Prepared Accelerando physics antenna strike response validated.");
                    break;
                case "CapturePreparedAccelerandoPhysicsAntennaStrikeMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CapturePhysicsAntennaStrikeMotionReview,
                        "Prepared Accelerando physics antenna strike motion captured.");
                    break;
                case "ApplyPreparedAccelerandoIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.ApplyIdleBreathingAnimationToCurrentScene,
                        "Prepared Accelerando idle breathing animation applied.");
                    break;
                case "InspectPreparedAccelerandoIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.InspectIdleBreathingAnimationInScene,
                        "Prepared Accelerando idle breathing animation inspected.");
                    break;
                case "CapturePreparedAccelerandoIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.CaptureIdleBreathingAnimationReview,
                        "Prepared Accelerando idle breathing animation captured.");
                    break;
                case "InspectPreparedAccelerandoModelStructure":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.AccelerandoCargoRunScene.AccelerandoCargoRunSceneApplyAndReview.InspectPreparedModelStructure,
                        "Prepared Accelerando model structure inspected.");
                    break;
                case "ApplyPreparedSocietasToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ApplyPreparedModelToCurrentCargoRunScene,
                        "Prepared Societas model applied to current CargoRunMvp scene.");
                    break;
                case "InspectPreparedSocietasCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.InspectAppliedSceneState,
                        "Prepared Societas CargoRunMvp scene state inspected.");
                    break;
                case "CapturePreparedSocietasReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.CaptureReview,
                        "Prepared Societas static review captured.");
                    break;
                case "ApplyPreparedMonstrumToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyPreparedModelToCurrentCargoRunScene,
                        "Prepared Monstrum model applied to current CargoRunMvp scene.");
                    break;
                case "InspectPreparedMonstrumCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.InspectAppliedSceneState,
                        "Prepared Monstrum CargoRunMvp scene state inspected.");
                    break;
                case "CapturePreparedMonstrumReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.CaptureReview,
                        "Prepared Monstrum static review captured.");
                    break;
                case "CapturePreparedMonstrumEyeCloseupReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.CaptureEyeCloseupReview,
                        "Prepared Monstrum eye closeup review captured.");
                    break;
                case "MovePreparedMonstrumPlayerStartToOppositeSide":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.MovePlayerStartToOppositeSide,
                        "Prepared Monstrum player start moved to the opposite side.");
                    break;
                case "InspectPreparedMonstrumPlayerStart":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.InspectPlayerStartInScene,
                        "Prepared Monstrum player start inspected.");
                    break;
                case "ApplyPreparedMonstrumAnimationReviewSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyAnimationReviewSlots,
                        "Prepared Monstrum animation review slots applied.");
                    break;
                case "ValidatePreparedMonstrumAnimationReviewSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ValidateAnimationReviewSlots,
                        "Prepared Monstrum animation review slots validated.");
                    break;
                case "ApplyPreparedMonstrumIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyIdleBreathingAnimation,
                        "Prepared Monstrum idle breathing animation applied.");
                    break;
                case "ValidatePreparedMonstrumIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ValidateIdleBreathingAnimation,
                        "Prepared Monstrum idle breathing animation validated.");
                    break;
                case "ApplyPreparedMonstrumMoveSourceAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyMoveSourceAnimation,
                        "Prepared Monstrum move source animation applied.");
                    break;
                case "ValidatePreparedMonstrumMoveSourceAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ValidateMoveSourceAnimation,
                        "Prepared Monstrum move source animation validated.");
                    break;
                case "ApplyPreparedMonstrumAttackModelToAttackSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyAttackModelToAttackSlots,
                        "Prepared Monstrum attack model applied to attack slots.");
                    break;
                case "RebuildPreparedMonstrumAttackSlot04FromStaticVisual":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.RebuildAttackSlot04FromStaticVisual,
                        "Prepared Monstrum attack slot 04 rebuilt from static visual.");
                    break;
                case "ApplyPreparedMonstrumAttackSlot04ModelWithStaticAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyAttackSlot04ModelWithStaticAppearance,
                        "Prepared Monstrum attack slot 04 model synced with static appearance.");
                    break;
                case "ApplyPreparedMonstrumDeathSlot05ModelWithStaticAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyDeathSlot05ModelWithStaticAppearance,
                        "Prepared Monstrum death slot 05 model synced with static appearance.");
                    break;
                case "ApplyPreparedMonstrumDeathMeltPuddleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyDeathMeltPuddleAnimation,
                        "Prepared Monstrum death slot 05 melt puddle animation applied.");
                    break;
                case "ApplyPreparedMonstrumRemoveNonAnimatedReviewObjects":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyPreparedMonstrumRemoveNonAnimatedReviewObjects,
                        "Prepared Monstrum non-animated review objects removed.");
                    break;
                case "ApplyPreparedMonstrumLooseGrainRemoval":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyLooseGrainRemoval,
                        "Prepared Monstrum loose grain removal applied.");
                    break;
                case "ValidatePreparedMonstrumLooseGrainRemoval":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ValidateLooseGrainRemoval,
                        "Prepared Monstrum loose grain removal validated.");
                    break;
                case "CreatePreparedMonstrumVisualRecolorEyeArtSample":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.CreateVisualRecolorEyeArtSample,
                        "Prepared Monstrum visual recolor eye art sample created.");
                    break;
                case "ApplyPreparedMonstrumVisualRecolorEyeToScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ApplyVisualRecolorEyeToScene,
                        "Prepared Monstrum visual recolor eye scene visuals applied.");
                    break;
                case "ValidatePreparedMonstrumVisualRecolorEyeScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.MonstrumCargoRunScene.MonstrumCargoRunSceneApplyAndReview.ValidateVisualRecolorEyeScene,
                        "Prepared Monstrum visual recolor eye scene visuals validated.");
                    break;
                case "ApplyPreparedCantabileToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.CantabileCargoRunScene.CantabileCargoRunSceneApplyAndReview.ApplyPreparedModelToCurrentCargoRunScene,
                        "Prepared Cantabile model applied to current CargoRunMvp scene.");
                    break;
                case "InspectPreparedCantabileCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.CantabileCargoRunScene.CantabileCargoRunSceneApplyAndReview.InspectAppliedSceneState,
                        "Prepared Cantabile CargoRunMvp scene state inspected.");
                    break;
                case "ApplyPreparedCantabileAnimationReviewObjects":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.CantabileCargoRunScene.CantabileCargoRunSceneApplyAndReview.ApplyAnimationReviewObjects,
                        "Prepared Cantabile animation review objects applied.");
                    break;
                case "ApplyApprovedCantabileColorSampleToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.CantabileCargoRunScene.CantabileCargoRunSceneApplyAndReview.ApplyApprovedColorSampleToCurrentCargoRunScene,
                        "Approved Cantabile color sample applied to current CargoRunMvp scene.");
                    break;
                case "CaptureApprovedCantabileColorSampleReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.CantabileCargoRunScene.CantabileCargoRunSceneApplyAndReview.CaptureApprovedColorSampleReview,
                        "Approved Cantabile color sample review capture saved.");
                    break;
                case "InspectDolorePlacementTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreCargoRunScene.DoloreCargoRunSceneApplyAndReview.InspectPlacementTarget,
                        "Supplied Dolore FBX and approved scene spacing target inspected without scene changes.");
                    break;
                case "ApplyDolorePlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreCargoRunScene.DoloreCargoRunSceneApplyAndReview.ApplyPlacement,
                        "Supplied Dolore FBX placed in seven review slots and Player start moved to the front view.");
                    break;
                case "InspectDolorePlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreCargoRunScene.DoloreCargoRunSceneApplyAndReview.InspectAppliedPlacement,
                        "Dolore placement, spacing, direct FBX instances, and Player start inspected.");
                    break;
                case "CaptureDolorePlayerStartView":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreCargoRunScene.DoloreCargoRunSceneApplyAndReview.CapturePlayerStartView,
                        "Player start camera view of the complete Dolore lineup captured after inspection.");
                    break;
                case "ApplyApprovedDoloreMaterialToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreApprovedMaterial.DoloreApprovedMaterialApplyAndReview.ApplyApprovedMaterialToCurrentCargoRunScene,
                        "Approved Dolore sample textures applied to all seven current CargoRunMvp placements without model changes.");
                    break;
                case "InspectApprovedDoloreMaterialState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreApprovedMaterial.DoloreApprovedMaterialApplyAndReview.InspectApprovedMaterialState,
                        "Approved Dolore material state and model invariants inspected without scene changes.");
                    break;
                case "CaptureApprovedDoloreMaterialReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreApprovedMaterial.DoloreApprovedMaterialApplyAndReview.CaptureApprovedMaterialReview,
                        "Approved Dolore material review captured from the current Player camera.");
                    break;
                case "InspectDoloreAttackAttachmentTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreAttackAttachment.DoloreAttackAttachmentApplyAndReview.InspectTarget,
                        "Dolore motion objects 3 and 4 inspected as approved attack attachment targets.");
                    break;
                case "ApplyApprovedDoloreAttackAttachment":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreAttackAttachment.DoloreAttackAttachmentApplyAndReview.ApplyApprovedAttachment,
                        "Approved Dolore attack attachment applied to motion objects 3 and 4 only.");
                    break;
                case "InspectDoloreAttackAttachment":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreAttackAttachment.DoloreAttackAttachmentApplyAndReview.InspectAppliedAttachment,
                        "Dolore attack attachment placement, appearance, source, and rig inspected.");
                    break;
                case "CaptureDoloreAttackAttachment":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreAttackAttachment.DoloreAttackAttachmentApplyAndReview.CaptureApprovedAttachment,
                        "Approved sample and Unity motion objects 3 and 4 captured for attack attachment comparison.");
                    break;
                case "CaptureDoloreAttackImportedReference":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreAttackAttachment.DoloreAttackAttachmentApplyAndReview.CaptureImportedReferenceDiagnostic,
                        "Imported approved Dolore attack reference captured for coordinate diagnostic.");
                    break;
                case "InspectDoloreAttackCoordinates":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreAttackAttachment.DoloreAttackAttachmentApplyAndReview.InspectCoordinateDiagnostic,
                        "Dolore target and approved attack reference coordinate relationships inspected.");
                    break;
                case "CaptureDoloreAttackVisibility":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreAttackAttachment.DoloreAttackAttachmentApplyAndReview.CaptureVisibilityDiagnostic,
                        "Dolore attack attachment-only and target back views captured.");
                    break;
                case "CaptureDoloreAttackContact":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.DoloreAttackAttachment.DoloreAttackAttachmentApplyAndReview.CaptureContactDiagnostic,
                        "Dolore attack attachment right, left, and upper three-quarter contact views captured.");
                    break;
                case "InspectDolore04TentacleStabTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabAnimationApplyAndReview.InspectTarget,
                        "Dolore motion object 3 built-in tentacle rig inspected without scene changes.");
                    break;
                case "ApplyDolore04TentacleStabAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabAnimationApplyAndReview.ApplyAnimation,
                        "Dolore motion object 3 tentacle stab animation applied with the built-in 13-bone rig.");
                    break;
                case "InspectDolore04TentacleStabAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabAnimationApplyAndReview.InspectAnimation,
                        "Dolore motion object 3 tentacle stab timing, rig curves, fixed root, and loop inspected.");
                    break;
                case "CaptureDolore04TentacleStabAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabAnimationApplyAndReview.CaptureAnimation,
                        "Dolore motion object 3 tentacle emergence, strike, and recovery poses captured.");
                    break;
                case "InspectDolore04TentacleStabFullMotionTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabAnimationApplyAndReview.InspectTarget,
                        "Dolore motion object 3 full tentacle motion target and built-in rig inspected.");
                    break;
                case "ApplyDolore04TentacleStabFullMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabAnimationApplyAndReview.ApplyAnimation,
                        "Dolore motion object 3 emergence, outward downstrike, and recovery motion applied.");
                    break;
                case "InspectDolore04TentacleStabFullMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabAnimationApplyAndReview.InspectAnimation,
                        "Dolore motion object 3 full motion timing, outward direction, fixed anchor, and loop inspected.");
                    break;
                case "ApplyDolore05ExecutionOpening":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore05ExecutionOpening.Dolore05ExecutionOpeningApplyAndReview.ApplyAnimation,
                        "Dolore motion object 4 execution opening copied through the first frontal pierce impact.");
                    break;
                case "InspectDolore05ExecutionOpening":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore05ExecutionOpening.Dolore05ExecutionOpeningApplyAndReview.InspectAnimation,
                        "Dolore motion object 4 execution opening source motion, timing, fixed anchor, and hold inspected.");
                    break;
                case "ApplyDolore05ExecutionTargetTransfer":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore05ExecutionTarget.Dolore05ExecutionTargetTransferApplyAndReview.ApplyPlacement,
                        "transfer.fbx placed below Dolore motion object 4 within the PierceHold range.");
                    break;
                case "InspectDolore05ExecutionTargetTransfer":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore05ExecutionTarget.Dolore05ExecutionTargetTransferApplyAndReview.InspectPlacement,
                        "Dolore motion object 4 transfer target position and PierceHold range inspected.");
                    break;
                case "CaptureDolore05ExecutionTargetTransferDiagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore05ExecutionTarget.Dolore05ExecutionTargetTransferApplyAndReview.CapturePlacementDiagnostic,
                        "Dolore motion object 4 transfer target diagnostic views captured.");
                    break;
                case "ApplyDolore05ExecutionPullInLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore05ExecutionPullInLoop.Dolore05ExecutionPullInLoopApplyAndReview.ApplyLoop,
                        "Dolore motion object 4 standing-to-lying penetration and two-second pull-in loop applied.");
                    break;
                case "InspectDolore05ExecutionPullInLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore05ExecutionPullInLoop.Dolore05ExecutionPullInLoopApplyAndReview.InspectLoop,
                        "Dolore motion object 4 target scale, immediate lying swap, pull-in timing, and loop inspected.");
                    break;
                case "CaptureDolore05ExecutionPullInLoopDiagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore05ExecutionPullInLoop.Dolore05ExecutionPullInLoopApplyAndReview.CaptureDiagnostic,
                        "Dolore motion object 4 actual Animator pull-in loop diagnostic states captured.");
                    break;
                case "InspectDolore06HitReactionTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore06HitReaction.Dolore06HitReactionApplyAndReview.InspectTarget,
                        "Dolore motion object 5 hit-reaction rig and weighted target inspected.");
                    break;
                case "ApplyDolore06HitReaction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore06HitReaction.Dolore06HitReactionApplyAndReview.ApplyAnimation,
                        "Dolore motion object 5 two-second backward recoil and left head turn applied.");
                    break;
                case "InspectDolore06HitReaction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore06HitReaction.Dolore06HitReactionApplyAndReview.InspectAnimation,
                        "Dolore motion object 5 recoil direction, left head turn, recovery, and loop inspected.");
                    break;
                case "CaptureDolore06HitReactionDiagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore06HitReaction.Dolore06HitReactionApplyAndReview.CaptureDiagnostic,
                        "Dolore motion object 5 actual hit-reaction Animator diagnostic views captured.");
                    break;
                case "InspectDolore07DeathTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore07Death.Dolore07DeathAnimationTool.InspectTarget,
                        "Dolore motion object 6 death rig, portrait material slot, and target inspected.");
                    break;
                case "ApplyDolore07DeathAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore07Death.Dolore07DeathAnimationTool.ApplyAnimation,
                        "Dolore motion object 6 left fall, white noise, black signal, and loop applied.");
                    break;
                case "InspectDolore07DeathAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore07Death.Dolore07DeathAnimationTool.InspectAnimation,
                        "Dolore motion object 6 death direction, ground contact, portrait signal phases, and loop inspected.");
                    break;
                case "CaptureDolore07DeathAnimationDiagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore07Death.Dolore07DeathAnimationTool.CaptureDiagnostic,
                        "Dolore motion object 6 death diagnostic views captured.");
                    break;
                case "CaptureDolore07DeathAnimationFinal":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.Dolore07Death.Dolore07DeathAnimationTool.CaptureFinal,
                        "Dolore motion object 6 death final views captured.");
                    break;
                case "InspectRebellionImportedModel":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionCargoRunScenePlacementTool.InspectImportedModel,
                        "Supplied Rebellion GLB imported directly and its visible structure inspected.");
                    break;
                case "ApplyRebellionPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionCargoRunScenePlacementTool.ApplyPlacement,
                        "Supplied Rebellion GLB placed in one static and six finalized animation slots; Player start moved to the front view.");
                    break;
                case "CaptureRebellionPlayerStartView":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionCargoRunScenePlacementTool.CapturePlayerStartView,
                        "Rebellion Player start camera view captured once after placement inspection.");
                    break;
                case "ApplyPahurPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.PahurCargoRunScene.PahurPlacementEditor.ApplyPahurPlacement,
                        "Supplied Pahur FBX placed in ten named static slots below Revolution using Longa Arma/Tergo Z spacing and Revolution X spacing; Player start moved to the full lineup front view.");
                    break;
                case "ReplacePlacedPahurModels":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.PahurCargoRunScene.PahurPlacementEditor.ReplacePlacedPahurModels,
                        "All ten placed Pahur model children replaced from the supplied FBX while preserving slot names, transforms, Player, and other scene roots.");
                    break;
                case "InspectPahurPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.PahurCargoRunScene.PahurPlacementEditor.InspectPahurPlacement,
                        "Pahur source hash, ten direct FBX instances, spacing, grounding, static state, unchanged roots, and Player front framing inspected.");
                    break;
                case "CapturePahurPlacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.PahurCargoRunScene.PahurPlacementEditor.CapturePahurPlacementReview,
                        "Pahur ten-model Player start front view captured once after placement inspection.");
                    break;
                case "ApplyKursaPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.KursaCargoRunScene.KursaPlacementEditor.ApplyKursaPlacement,
                        "Supplied Kursa FBX placed in twelve named static slots below Pahur using Longa Arma/Tergo Z spacing and Pahur X spacing; Player start moved to the full lineup front view.");
                    break;
                case "InspectKursaPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.KursaCargoRunScene.KursaPlacementEditor.InspectKursaPlacement,
                        "Kursa source hash, twelve direct FBX instances, spacing, grounding, static state, and Player front framing inspected.");
                    break;
                case "ApplyIspantArmedPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.ApplyIspantArmedPlacement,
                        "Supplied Ispant armed FBX placed in twelve static slots below Kursa using Longa Arma/Tergo Z spacing and Kursa X spacing without changing existing scene roots.");
                    break;
                case "InspectIspantArmedPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.InspectIspantArmedPlacement,
                        "Ispant source hash, twelve direct FBX instances, spacing, grounding, static state, and unchanged scene state inspected.");
                    break;
                case "InspectIspantUnitySideAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantModelReplacementTool.InspectUnitySideAppearance,
                        "The direct FBX importer, embedded material and texture bindings, scene renderer materials, and prefab material overrides were inspected without changing the scene.");
                    break;
                case "ApplyIspantUnitySideCleanup":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantModelReplacementTool.ApplyUnitySideCleanup,
                        "The wrong cross-enemy texture binding and prior derived Ispant model assets were removed, the FBX-packed texture was extracted exactly, and all twelve direct instances were rebuilt without animation connections.");
                    break;
                case "InspectIspantUnitySideCleanup":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantModelReplacementTool.InspectUnitySideCleanup,
                        "The exact FBX and packed texture hashes, source-local material binding, twelve direct instances, deleted derived assets, and absent animation connections were inspected without changing the scene.");
                    break;
                case "ExportIspantStaticFbx":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantStaticFbxExportTool.ExportIspantStaticFbx,
                        "One currently placed approved Ispant was baked and exported as a pure static four-mesh FBX without rig, bones, animation, or scene changes.");
                    break;
                case "ExportIspant01StaticForMixamo":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantStaticFbxExportTool.ExportIspant01StaticForMixamo,
                        "The current slot-1 standing pose was baked to the requested static binary FBX for Mixamo auto-rig upload without changing the Unity scene.");
                    break;
                case "InspectIspant01StaticMixamoFbx":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantStaticFbxExportTool.InspectIspant01StaticMixamoFbx,
                        "The requested slot-1 static Mixamo FBX header, mesh tokens, finite source data, excluded rig and animation data, and unchanged scene were inspected.");
                    break;
                case "ApplyApprovedIspantAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.ApplyApprovedIspantAppearance,
                        "The exact approved belt-removed Ispant body, preserved diagonal chest strap, beveled crescent, explicit cyan eye mesh, edge-connected face pattern, approved materials, and twenty-eight copied textures applied to all twelve placed Ispant slots while preserving brightness, slot transforms, and other scene roots.");
                    break;
                case "InspectApprovedIspantAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.InspectApprovedIspantAppearance,
                        "The twelve approved Ispant FBX instances, belt-removed body, crescent and explicit eye topology, face UV, rig, exact material order, texture hashes, preserved brightness, and unchanged scene state inspected.");
                    break;
                case "CaptureApprovedIspantAppearanceReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CaptureApprovedIspantAppearanceReview,
                        "A single side-by-side approved Blender sample and Unity Ispant appearance review image captured after the dedicated inspection passed.");
                    break;
                case "DiagnoseApprovedIspantPreviewLighting":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.DiagnoseApprovedIspantPreviewLighting,
                        "The approved custom shader and standard URP Lit were rendered under identical off-scene preview lighting with luminance metrics and no scene changes.");
                    break;
                case "CaptureApprovedIspantAppearanceReviewReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CaptureApprovedIspantAppearanceReviewReplacement,
                        "One corrected side-by-side approved Blender sample and Unity Ispant appearance comparison was captured after the lighting diagnosis and dedicated structural inspection passed.");
                    break;
                case "ApplyApprovedIspantBrightnessSync":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.ApplyApprovedIspantBrightnessSync,
                        "The body-only white armor brightness was corrected on all twelve Ispant instances while preserving the helmet, approved UV-independent crescent material, and scene contract.");
                    break;
                case "CaptureApprovedIspantBrightnessDiagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CaptureApprovedIspantBrightnessDiagnostic,
                        "A diagnostic side-by-side approved sample and Unity Ispant brightness comparison was captured after dedicated structural inspection.");
                    break;
                case "CaptureApprovedIspantBrightnessReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CaptureApprovedIspantBrightnessReview,
                        "One final side-by-side approved sample and Unity Ispant white armor and crescent brightness review was captured after the dedicated inspection passed.");
                    break;
                case "CaptureApprovedIspantBodyBrightnessDiagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CaptureApprovedIspantBodyBrightnessDiagnostic,
                        "A diagnostic side-by-side comparison was captured after increasing only the twelve approved Ispant body and helmet white armor materials while preserving the crescent material.");
                    break;
                case "CaptureApprovedIspantBodyBrightnessReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CaptureApprovedIspantBodyBrightnessReview,
                        "One final side-by-side approved sample and Unity Ispant body white armor brightness review was captured after the dedicated inspection passed.");
                    break;
                case "CalibrateApprovedIspantBodyArmorMeanLuminance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CalibrateApprovedIspantBodyArmorMeanLuminance,
                        "The body-only white armor brightness required to reach the user-approved mean luminance target was calibrated in an off-scene preview without changing the helmet, crescent, or scene.");
                    break;
                case "InspectApprovedIspantBodyArmorMeanLuminance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.InspectApprovedIspantBodyArmorMeanLuminance,
                        "The applied body-only white armor mean luminance matched the user-approved 25 percent increase target while the helmet, crescent, and scene remained unchanged.");
                    break;
                case "CaptureApprovedIspantBodyMean25Diagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CaptureApprovedIspantBodyMean25Diagnostic,
                        "A diagnostic comparison was captured after applying the calibrated 25 percent body-only white armor mean luminance increase.");
                    break;
                case "CaptureApprovedIspantBodyMean25Review":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantApprovedAppearanceApplicator.CaptureApprovedIspantBodyMean25Review,
                        "One final comparison was captured after the body-only white armor mean luminance and dedicated structural inspections passed.");
                    break;
                case "CaptureIspantArmedPlacementDiagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.CaptureIspantArmedPlacementDiagnostic,
                        "Ispant placement diagnostic review image captured for direct visual inspection.");
                    break;
                case "CaptureIspantArmedPlacementFinalReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.CaptureIspantArmedPlacementFinalReview,
                        "Ispant placement final review image captured once after direct visual diagnostics passed.");
                    break;
                case "ApplyIspantPlayerStartFraming":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.ApplyIspantPlayerStartFraming,
                        "Player start moved between Ata and Ispant so the central Ispant motion objects are seen from the front while Ata remains behind the camera and other scene roots remain unchanged.");
                    break;
                case "StartIspantPlayerFrontPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.StartIspantPlayerFrontPlayback,
                        "Unity entered Play Mode for direct inspection of the Ispant Player-start front view.");
                    break;
                case "InspectIspantPlayerFrontPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.InspectIspantPlayerFrontPlayback,
                        "The actual Play Mode Player camera faced the central Ispant motion objects from the front with Ata behind the camera.");
                    break;
                case "StopIspantPlayerFrontPlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.StopIspantPlayerFrontPlayback,
                        "Unity Play Mode stop was requested immediately after the Ispant Player-start inspection.");
                    break;
                case "InspectStoppedIspantPlayerFront":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.InspectStoppedIspantPlayerFront,
                        "Unity was stopped and the saved Player start still faced Ispant from between the Ispant and Ata placements without changing the scene.");
                    break;
                case "CaptureIspantPlayerStartDiagnostic":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.CaptureIspantPlayerStartDiagnostic,
                        "Ispant Player-start diagnostic image captured from the actual Player camera for direct visual inspection.");
                    break;
                case "CaptureIspantPlayerStartFinalReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.IspantCargoRunScene.IspantPlacementEditor.CaptureIspantPlayerStartFinalReview,
                        "Ispant Player-start final image captured once from the actual Player camera after diagnostics passed.");
                    break;
                case "ApplyApprovedKursaAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.KursaCargoRunScene.KursaApprovedAppearanceApplicator.ApplyApprovedKursaAppearance,
                        "The exact approved Kursa material-only FBX mesh, nine material slots, copied textures, and approved projection values applied to the twelve placed Kursa renderers while preserving transforms, rig, animation, and other scene roots.");
                    break;
                case "InspectApprovedKursaAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.KursaCargoRunScene.KursaApprovedAppearanceApplicator.InspectApprovedKursaAppearance,
                        "The twelve placed Kursa renderers, exact approved mesh/material order, copied texture hashes, geometry, UV, weights, rig, animation, and unchanged scene state inspected.");
                    break;
                case "CaptureApprovedKursaAppearanceReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.KursaCargoRunScene.KursaApprovedAppearanceApplicator.CaptureApprovedKursaAppearanceReview,
                        "A single side-by-side approved Blender sample and Unity Kursa appearance review image captured after the dedicated inspection passed.");
                    break;
                case "ApplyResistancePlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceCargoRunScenePlacementTool.ApplyPlacement,
                        "Supplied Resistance FBX placed in fourteen static slots; Player start moved to the Resistance_07 front view.");
                    break;
                case "InspectResistancePlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceCargoRunScenePlacementTool.InspectPlacement,
                        "Resistance placement, spacing, source hash, grounding, and Resistance_07 camera framing inspected.");
                    break;
                case "CaptureResistancePlacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceCargoRunScenePlacementTool.CapturePlacementReview,
                        "Resistance_07 Player start camera view captured once after placement inspection.");
                    break;
                case "ApplyRevolutionModelPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionCargoRunScenePlacementTool.ApplyRevolutionModelPlacement,
                        "Supplied Revolution FBX placed in eight static slots below Resistance, using Resistance X spacing and Longa Arma/Tergo Z spacing; Player start moved to the full front view.");
                    break;
                case "InspectRevolutionModelPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionCargoRunScenePlacementTool.InspectRevolutionModelPlacement,
                        "Revolution source hash, direct FBX instances, eight-slot spacing, grounding, unchanged roots, and Player front framing inspected.");
                    break;
                case "CaptureRevolutionModelPlacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionCargoRunScenePlacementTool.CaptureRevolutionModelPlacementReview,
                        "Revolution eight-model Player start front view captured once after placement inspection.");
                    break;
                case "ApplyRevolutionAttackModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionCargoRunScenePlacementTool.ApplyRevolutionAttackModelReplacement,
                        "All eight placed Revolution model children replaced by direct instances of the supplied attack FBX while preserving slots, lineup, and Player start.");
                    break;
                case "InspectRevolutionAttackModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionCargoRunScenePlacementTool.InspectRevolutionAttackModelReplacement,
                        "Revolution attack FBX hash, eight direct instances, static playback state, height, grounding, unchanged slots, and Player framing inspected.");
                    break;
                case "CaptureRevolutionAttackModelReplacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionCargoRunScenePlacementTool.CaptureRevolutionAttackModelReplacementReview,
                        "All eight replaced Revolution attack models captured once from the preserved Player start.");
                    break;
                case "ApplyRevolutionBaseModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionBaseModelReplacementTool.ApplyRevolutionBaseModelReplacement,
                        "All eight placed Revolution model children replaced by direct instances of the supplied base FBX while preserving slots, lineup, Player, and camera transforms.");
                    break;
                case "InspectRevolutionBaseModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionBaseModelReplacementTool.InspectRevolutionBaseModelReplacement,
                        "Revolution base FBX hash, eight direct instances, imported geometry, static playback state, target height, and grounding inspected without changing the scene.");
                    break;
                case "ApplyRevolutionApprovedAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionApprovedAppearanceTool.ApplyRevolutionApprovedAppearance,
                        "The approved Revolution sample mesh partition, direct-crop textures, and converted material graph were applied to all eight placed models without changing placement transforms.");
                    break;
                case "InspectRevolutionApprovedAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionApprovedAppearanceTool.InspectRevolutionApprovedAppearance,
                        "All eight Revolution models use the approved sample mesh, eight active materials, direct-crop textures, symmetric mapping, and preserved placement; Unity omitted the approved Blender sample's zero-face ninth slot.");
                    break;
                case "CaptureRevolutionApprovedAppearanceReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionApprovedAppearanceTool.CaptureRevolutionApprovedAppearanceReview,
                        "The approved Revolution appearance lineup was captured once from the preserved Player camera after inspection.");
                    break;
                case "ApplyRevolutionIdleMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionIdleMotionTool.ApplyRevolutionIdleMotion,
                        "A two-second planted-foot breathing idle was applied only to Revolution_02 with 3 cm pelvis travel and no root motion.");
                    break;
                case "InspectRevolutionIdleMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionIdleMotionTool.InspectRevolutionIdleMotion,
                        "Revolution_02 idle loop duration, pelvis travel, planted feet, knee flex, approved appearance, and unchanged placement were inspected.");
                    break;
                case "CaptureRevolutionIdleMotionReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionIdleMotionTool.CaptureRevolutionIdleMotionReview,
                        "Revolution_02 idle poses at 0, 0.5, 1, 1.5, and 2 seconds were captured once after inspection.");
                    break;
                case "ApplyRevolutionMoveMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMoveMotionTool.ApplyRevolutionMoveMotion,
                        "Revolution_03 now repeats the walking_man clip embedded in its existing FBX without changing its mesh or appearance.");
                    break;
                case "ApplyRevolutionMeleeAttack":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMeleeAttackModelReplacementTool.ApplyRevolutionMeleeAttack,
                        "Revolution_05 was replaced by the supplied slash FBX, synchronized to the current Revolution_01 appearance, and configured to loop only its Mixamo attack take.");
                    break;
                case "ApplyRevolutionDeath":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMeleeAttackModelReplacementTool.ApplyRevolutionDeath,
                        "Revolution_08 was replaced by the supplied death FBX, synchronized to the current Revolution_01 appearance, and configured to loop only its Mixamo death take.");
                    break;
                case "ReviewRevolutionDeathToEnd":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMeleeAttackModelReplacementTool.ReviewRevolutionDeathToEnd,
                        "Revolution_08 death was sampled at its exact authored end without changing the runtime clip, then returned to looping Play Mode.");
                    break;
                case "PlayCurrentRevolutionDeathLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMeleeAttackModelReplacementTool.PlayCurrentRevolutionDeathLoop,
                        "Revolution_08 current full-length Mixamo death clip is playing in a loop.");
                    break;
                case "ApplyRevolutionHit":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionHitModelReplacementTool.ApplyRevolutionHit,
                        "Revolution_06 was replaced by the supplied hit FBX, synchronized to the current Revolution_01 appearance, and configured to loop only its Mixamo hit take.");
                    break;
                case "ApplyRevolutionHitStaticArmPose":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionHitModelReplacementTool.ApplyRevolutionHitStaticArmPose,
                        "Revolution_06 keeps its Mixamo hit motion while both Shoulder-to-Hand chains remain at the Revolution_01 static local rotations.");
                    break;
                // Revolution_07 uses only the supplied FBX Mixamo turn take.
                case "ApplyRevolutionTurn":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionTurnModelReplacementTool.ApplyRevolutionTurn,
                        "Revolution_07 was replaced by the supplied turn FBX, synchronized to the current Revolution_01 appearance, and configured to loop only its Mixamo turn take.");
                    break;
                case "InspectRevolutionTurnSourceCurves":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionTurnModelReplacementTool.InspectRevolutionTurnSourceCurves,
                        "The supplied Revolution turn Mixamo clip curve bindings and sampled source rotations were recorded without changing the scene.");
                    break;
                case "ApplyRevolutionTurn360StaticArms":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionTurnModelReplacementTool.ApplyRevolutionTurn360StaticArms,
                        "Revolution_07 now performs the supplied Mixamo turn direction and stepping motion as an exact 360-degree turn over 3 seconds while both Shoulder-to-Hand chains remain at the Revolution_01 static local rotations.");
                    break;
                case "ApplyRevolutionMachineGunAttackMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMachineGunAttackMotionTool.ApplyRevolutionMachineGunAttackMotion,
                        "Revolution_04 received a looping bilateral machine-gun attack with rightward barrel rotation and reused Rebellion muzzle flashes.");
                    break;
                case "InspectRevolutionMachineGunAttackMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMachineGunAttackMotionTool.InspectRevolutionMachineGunAttackMotion,
                        "Revolution_04 bilateral gun rotation, firing cadence, muzzle flashes, loop closure, approved appearance, and protected scene scope were inspected.");
                    break;
                case "CaptureRevolutionMachineGunAttackMotionReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMachineGunAttackMotionTool.CaptureRevolutionMachineGunAttackMotionReview,
                        "Revolution_04 machine-gun firing poses and a direct visual-review frame sequence were captured without automated motion judgement.");
                    break;
                case "CaptureRevolutionMachineGunRightArmShapeReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMachineGunAttackMotionTool.CaptureRevolutionMachineGunRightArmShapeReview,
                        "Revolution_01 static and Revolution_04 animated right-arm shapes were captured from matched front and oblique views.");
                    break;
                case "ApplyRevolutionMachineGunRightArmShapeRepair":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RevolutionCargoRunScene.RevolutionMachineGunAttackMotionTool.ApplyRevolutionMachineGunRightArmShapeRepair,
                        "Revolution_04 right-arm firing shape was restored from Revolution_01 while preserving every barrel-spin and left-arm curve.");
                    break;
                case "ApplyApprovedResistanceAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceApprovedBakedAppearanceApply.Apply,
                        "Directly baked user-approved Resistance sample applied to all fourteen slots.");
                    break;
                case "InspectApprovedResistanceAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceApprovedAppearanceApplyAndReview.InspectApprovedAppearance,
                        "Approved Resistance material, unchanged mesh topology, placement, and Player camera inspected.");
                    break;
                case "CaptureApprovedResistanceAppearanceFinal":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceApprovedAppearanceApplyAndReview.CaptureApprovedAppearanceFinal,
                        "Resistance_07 approved appearance final view captured once.");
                    break;
                case "ApplyResistanceIdleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceIdleAnimationTool.ApplyResistanceIdleAnimation,
                        "Resistance_02 two-second grounded full-body idle morph loop applied.");
                    break;
                case "InspectResistanceIdleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceIdleAnimationTool.InspectResistanceIdleAnimation,
                        "Resistance_02 idle loop, grounded feet, fixed slot position, and unchanged meshes inspected.");
                    break;
                case "CaptureResistanceIdleAnimationReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceIdleAnimationTool.CaptureResistanceIdleAnimationReview,
                        "Resistance_02 idle animation review progression captured once.");
                    break;
                case "ApplyResistanceMoveModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceMoveModelReplacementTool.ApplyResistanceMoveModelReplacement,
                        "Resistance_03 replaced by the supplied walking FBX, synchronized to the Resistance_01 approved appearance, and configured to loop the selected Mixamo clip.");
                    break;
                case "InspectResistanceMoveModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceMoveModelReplacementTool.InspectResistanceMoveModelReplacement,
                        "Resistance_03 walking source, exact approved appearance references, rig, grounding, playback, and loop inspected.");
                    break;
                case "CaptureResistanceMoveModelReplacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceMoveModelReplacementTool.CaptureResistanceMoveModelReplacementReview,
                        "Resistance_01 static reference and Resistance_03 Mixamo walking progression captured once.");
                    break;
                case "ApplyResistanceBasicAttackModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceBasicAttackModelReplacementTool.ApplyResistanceBasicAttackModelReplacement,
                        "Resistance_04 replaced by the supplied punching FBX, directly linked to the approved Resistance appearance, and configured to loop the selected Mixamo clip.");
                    break;
                case "InspectResistanceBasicAttackModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceBasicAttackModelReplacementTool.InspectResistanceBasicAttackModelReplacement,
                        "Resistance_04 punching source, exact approved appearance references, right-hand motion, grounding, playback, and loop inspected.");
                    break;
                case "CaptureResistanceBasicAttackModelReplacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceBasicAttackModelReplacementTool.CaptureResistanceBasicAttackModelReplacementReview,
                        "Resistance_01 static reference and Resistance_04 Mixamo punching progression captured once.");
                    break;
                case "ApplyResistanceHitModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceHitModelReplacementTool.ApplyResistanceHitModelReplacement,
                        "Resistance_05 replaced by the supplied hit FBX, directly linked to the approved Resistance appearance, and configured to loop the selected Mixamo clip.");
                    break;
                case "InspectResistanceHitModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceHitModelReplacementTool.InspectResistanceHitModelReplacement,
                        "Resistance_05 hit source, exact approved appearance references, visible motion, grounding, playback, and loop inspected.");
                    break;
                case "CaptureResistanceHitModelReplacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceHitModelReplacementTool.CaptureResistanceHitModelReplacementReview,
                        "Resistance_01 static reference and Resistance_05 Mixamo hit progression captured once.");
                    break;
                case "ApplyResistanceDeathModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceDeathModelReplacementTool.ApplyResistanceDeathModelReplacement,
                        "Resistance_06 replaced by the supplied death FBX, linked to the exact approved appearance, and configured with the repeating death-to-self-destruct sequence.");
                    break;
                case "InspectResistanceDeathModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceDeathModelReplacementTool.InspectResistanceDeathModelReplacement,
                        "Resistance_06 Mixamo death, exact appearance, grounding, model hide, yellow-orange explosion, and sequence reset inspected.");
                    break;
                case "CaptureResistanceDeathModelReplacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistanceDeathModelReplacementTool.CaptureResistanceDeathModelReplacementReview,
                        "Resistance_01 static reference and Resistance_06 death-to-explosion sequence captured once.");
                    break;
                case "ApplyResistancePickingModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistancePickingModelReplacementTool.ApplyResistancePickingModelReplacement,
                        "Resistance_07 replaced by the supplied picking FBX, directly linked to the approved Resistance appearance, and configured to loop its stationary Mixamo clip.");
                    break;
                case "InspectResistancePickingModelReplacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistancePickingModelReplacementTool.InspectResistancePickingModelReplacement,
                        "Resistance_07 picking source, exact approved appearance references, stationary model root, visible motion, grounding, playback, and loop inspected.");
                    break;
                case "CaptureResistancePickingModelReplacementReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ResistanceCargoRunScene.ResistancePickingModelReplacementTool.CaptureResistancePickingModelReplacementReview,
                        "Resistance_01 static reference and Resistance_07 stationary Mixamo picking progression captured once.");
                    break;
                case "ApplyApprovedRebellionAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionApprovedAppearanceApplyAndReview.ApplyApprovedRebellionAppearance,
                        "User-approved Rebellion appearance applied to all seven existing slots.");
                    break;
                case "InspectApprovedRebellionAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionApprovedAppearanceApplyAndReview.InspectApprovedRebellionAppearance,
                        "Approved Rebellion appearance state inspected without scene changes.");
                    break;
                case "CaptureApprovedRebellionAppearanceFinal":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionApprovedAppearanceApplyAndReview.CaptureApprovedRebellionAppearanceFinal,
                        "Approved Rebellion appearance final view captured once.");
                    break;
                case "InspectRebellionMoveRig":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionMoveAnimationTool.InspectMoveRig,
                        "Rebellion move slot embedded rig hierarchy inspected without scene changes.");
                    break;
                case "InspectRebellionAttackTransitionRig":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionAttackModeTransitionTool.InspectRig,
                        "Rebellion attack transition rig separation inspected without scene changes.");
                    break;
                case "ApplyRebellionAttackModeTransition":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionAttackModeTransitionTool
                            .ApplyAttackModeTransition,
                        "Rebellion attack mode transition applied to Rebellion_02.");
                    break;
                case "InspectRebellionAttackModeTransition":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionAttackModeTransitionTool
                            .InspectAttackModeTransition,
                        "Rebellion attack mode transition structure and poses inspected.");
                    break;
                case "CaptureRebellionAttackModeTransitionReview":
                    RunRebellionAttackTransitionVisualReview(request);
                    break;
                case "ApplyRebellionForwardScan":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionForwardScanTool.ApplyForwardScan,
                        "Rebellion forward scan applied to Rebellion_03.");
                    break;
                case "InspectRebellionForwardScan":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionForwardScanTool.InspectForwardScan,
                        "Rebellion forward scan structure, standing pose, and sweep inspected.");
                    break;
                case "CaptureRebellionForwardScanReview":
                    RunRebellionForwardScanVisualReview(request);
                    break;
                case "ApplyRebellionForwardBurstFire":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionForwardBurstFireTool
                            .ApplyForwardBurstFire,
                        "Rebellion forward burst fire applied to Rebellion_04.");
                    break;
                case "InspectRebellionForwardBurstFire":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionForwardBurstFireTool
                            .InspectForwardBurstFire,
                        "Rebellion forward burst fire structure, standing " +
                        "pose, firing cadence, and weapon rotation inspected.");
                    break;
                case "CaptureRebellionForwardBurstFireReview":
                    RunRebellionForwardBurstVisualReview(request);
                    break;
                case "ApplyRebellionHitReaction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionAttackModeTransitionTool
                            .ApplyHitReaction,
                        "Rebellion hit reaction applied to Rebellion_05.");
                    break;
                case "InspectRebellionHitReaction":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionAttackModeTransitionTool
                            .InspectHitReaction,
                        "Rebellion hit reaction structure, recoil, and " +
                        "left-rear step inspected.");
                    break;
                case "CaptureRebellionHitReactionReview":
                    RunRebellionHitReactionVisualReview(request);
                    break;
                case "ApplyRebellionDeath":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionAttackModeTransitionTool
                            .ApplyDeath,
                        "Rebellion death animation applied to Rebellion_06.");
                    break;
                case "InspectRebellionDeath":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionAttackModeTransitionTool
                            .InspectDeath,
                        "Rebellion death structure, collapse, body tilt, " +
                        "floor contact, and final hold inspected.");
                    break;
                case "CaptureRebellionDeathReview":
                    RunRebellionDeathVisualReview(request);
                    break;
                case "ApplyRebellionMoveAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionMoveAnimationTool.ApplyMoveAnimation,
                        "Rebellion diagonal spider crawl animation applied to Rebellion_01_Move.");
                    break;
                case "InspectRebellionMoveAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene.RebellionMoveAnimationTool.InspectMoveAnimation,
                        "Rebellion move animation and corrected rig attachments inspected.");
                    break;
                case "CaptureRebellionMoveVisualReview":
                    RunRebellionMoveVisualReview(request);
                    break;
                case "RemoveRebellionFrontArtifact":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionFrontArtifactReviewTool.RemoveFrontArtifact,
                        "Rebellion front square animation artifact removed only.");
                    break;
                case "InspectRebellionFrontArtifact":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.RebellionCargoRunScene
                            .RebellionFrontArtifactReviewTool.InspectFrontArtifact,
                        "Rebellion front artifact correction inspected without scene changes.");
                    break;
                case "CaptureRebellionFrontArtifactReview":
                    RunRebellionFrontArtifactVisualReview(request);
                    break;
                case "ApplyNegatifPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.ApplyPlacement,
                        "Negatif static model and six animation placeholders placed below Dolore.");
                    break;
                case "InspectNegatifPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.InspectAppliedPlacement,
                        "Negatif placement, supplied FBX instances, lineup, and Player start inspected.");
                    break;
                case "CaptureNegatifPlayerStartView":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.CapturePlayerStartView,
                        "Negatif Player start camera view captured once after placement inspection.");
                    break;
                case "ApplyNegatifApprovedAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.ApplyApprovedAppearance,
                        "Approved Negatif sample appearance applied to all seven placed models.");
                    break;
                case "InspectNegatifApprovedAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.InspectApprovedAppearance,
                        "Approved Negatif model, material, texture, geometry, and placement contracts inspected.");
                    break;
                case "CaptureNegatifApprovedAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.CaptureApprovedAppearance,
                        "Approved Negatif Unity appearance captured once for visual comparison.");
                    break;
                case "ApplyNegatifApprovedGlbAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.ApplyApprovedGlbAppearance,
                        "Approved Negatif GLB sample applied to all seven placed models.");
                    break;
                case "InspectNegatifApprovedGlbAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.InspectApprovedGlbAppearance,
                        "Approved Negatif GLB instances, eyes, materials, geometry, and placement inspected.");
                    break;
                case "CaptureNegatifApprovedGlbAppearance":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.CaptureApprovedGlbAppearance,
                        "Approved Negatif GLB Unity appearance captured once for visual comparison.");
                    break;
                case "ApplyNegatifIdleEyeEmission":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.ApplyIdleEyeEmissionAnimation,
                        "Negatif idle eye emission animation applied to Negatif_01_Idle.");
                    break;
                case "InspectNegatifIdleEyeEmission":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifCargoRunScenePlacementTool.InspectIdleEyeEmissionAnimation,
                        "Negatif idle eye emission animation inspected.");
                    break;
                case "CaptureNegatifIdleEyeEmissionVisualReview":
                    RunNegatifIdleEyeEmissionVisualReview(request);
                    break;
                case "CaptureNegatifMoveVisualReview":
                    RunNegatifMoveVisualReview(request);
                    break;
                case "ApplyNegatifMoveAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifMoveAnimationTool.ApplyMoveAnimation,
                        "Negatif quadruped rig move animation applied to Negatif_02_Move.");
                    break;
                case "ApplyNegatifClawAttackAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifClawAttackAnimationTool.ApplyClawAttackAnimation,
                        "Negatif alternating upright claw attack applied to Negatif_03_Claw_Attack.");
                    break;
                case "CaptureNegatifClawAttackVisualReview":
                    RunNegatifClawAttackVisualReview(request);
                    break;
                case "ApplyNegatifHitReactionAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifHitReactionAnimationTool.ApplyHitReactionAnimation,
                        "Negatif left hit reaction applied to Negatif_04_Hit_Reaction.");
                    break;
                case "CaptureNegatifHitReactionVisualReview":
                    RunNegatifHitReactionVisualReview(request);
                    break;
                case "ApplyNegatifFleeAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifFleeAnimationTool.ApplyFleeAnimation,
                        "Negatif accelerated quadruped flee and tail swing applied to Negatif_05_Flee.");
                    break;
                case "CaptureNegatifFleeVisualReview":
                    RunNegatifFleeVisualReview(request);
                    break;
                case "ApplyNegatifDeathAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.NegatifCargoRunScene.NegatifDeathAnimationTool.ApplyDeathAnimation,
                        "Negatif right-roll belly-up death animation applied to Negatif_06_Death.");
                    break;
                case "CaptureNegatifDeathVisualReview":
                    RunNegatifDeathVisualReview(request);
                    break;
                case "ApplyConSpiritoToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.ApplyReriggedModelToCurrentCargoRunScene,
                        "Rerigged Con Spirito model applied to current CargoRunMvp scene.");
                    break;
                case "InspectConSpiritoCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectAppliedSceneState,
                        "Rerigged Con Spirito CargoRunMvp scene state inspected.");
                    break;
                case "CaptureConSpiritoCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.CaptureReview,
                        "Rerigged Con Spirito CargoRunMvp scene capture saved.");
                    break;
                case "ApplyConSpiritoDefaultAnimationLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.ApplyDefaultAnimationLoopToCurrentCargoRunScene,
                        "Con Spirito default FBX animation loop applied to current CargoRunMvp scene.");
                    break;
                case "InspectConSpiritoDefaultAnimationLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectDefaultAnimationLoopInScene,
                        "Con Spirito default FBX animation loop scene state inspected.");
                    break;
                case "ApplyConSpiritoDogWalkLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.ApplyDogWalkLoopToCurrentCargoRunScene,
                        "Con Spirito dog walk loop applied to current CargoRunMvp scene.");
                    break;
                case "InspectConSpiritoDogWalkLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectDogWalkLoopInScene,
                        "Con Spirito dog walk loop scene state inspected.");
                    break;
                case "CaptureConSpiritoDogWalkLoopReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.CaptureDogWalkLoopReview,
                        "Con Spirito dog walk loop review captures saved.");
                    break;
                case "ApplyOriginalConSpiritoAnimationLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.ApplyOriginalAnimationLoopToCurrentCargoRunScene,
                        "Original Con Spirito FBX animation loop applied to current CargoRunMvp scene.");
                    break;
                case "InspectOriginalConSpiritoAnimationLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectOriginalAnimationLoopInScene,
                        "Original Con Spirito FBX animation loop scene state inspected.");
                    break;
                case "CaptureOriginalConSpiritoAnimationLoopReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.CaptureOriginalAnimationLoopReview,
                        "Original Con Spirito FBX animation loop review captures saved.");
                    break;
                case "ApplyConSpiritoAnimationReviewSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.ApplyAnimationReviewSlots,
                        "Con Spirito animation review slots applied.");
                    break;
                case "InspectConSpiritoAnimationReviewSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectAnimationReviewSlotsInScene,
                        "Con Spirito animation review slots inspected.");
                    break;
                case "CaptureConSpiritoAnimationReviewSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.CaptureAnimationReviewSlots,
                        "Con Spirito animation review slots captured.");
                    break;
                case "InspectConSpiritoMaterialState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectMaterialStateInScene,
                        "Con Spirito material state inspected.");
                    break;
                case "CaptureConSpiritoMaterialInspection":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.CaptureMaterialInspection,
                        "Con Spirito material inspection captures saved.");
                    break;
                case "ApplyConSpiritoApprovedMaterialSample":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.ApplyApprovedMaterialSampleToCurrentScene,
                        "Con Spirito approved material sample applied.");
                    break;
                case "InspectConSpiritoApprovedMaterialSample":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectApprovedMaterialSampleInScene,
                        "Con Spirito approved material sample inspected.");
                    break;
                case "CaptureConSpiritoApprovedMaterialSample":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.CaptureApprovedMaterialSampleReview,
                        "Con Spirito approved material sample captures saved.");
                    break;
                case "ApplyConSpiritoIdleBreathLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.ApplyIdleBreathLoopToCurrentScene,
                        "Con Spirito idle breath loop applied.");
                    break;
                case "InspectConSpiritoIdleBreathLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectIdleBreathLoopInScene,
                        "Con Spirito idle breath loop inspected.");
                    break;
                case "CaptureConSpiritoIdleBreathLoopReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.CaptureIdleBreathLoopReview,
                        "Con Spirito idle breath loop review captures saved.");
                    break;
                case "ApplyConSpiritoChargeLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.ApplyChargeLoopToCurrentScene,
                        "Con Spirito charge loop applied.");
                    break;
                case "InspectConSpiritoChargeLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.InspectChargeLoopInScene,
                        "Con Spirito charge loop inspected.");
                    break;
                case "CaptureConSpiritoChargeLoopReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.CaptureChargeLoopReview,
                        "Con Spirito charge loop review captures saved.");
                    break;
                case "CaptureConSpiritoReferenceRunVideos":
                    RunConSpiritoReferenceRunVideoCapture(request);
                    break;
                case "MovePreparedSocietasPlayerStartToOppositeSide":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.MovePlayerStartToOppositeSide,
                        "Prepared Societas player start moved to the opposite side.");
                    break;
                case "ApplyPreparedSocietasAnimationReviewSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ApplyAnimationReviewSlots,
                        "Prepared Societas animation review slots applied.");
                    break;
                case "ValidatePreparedSocietasAnimationReviewSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ValidateAnimationReviewSlots,
                        "Prepared Societas animation review slots validated.");
                    break;
                case "CapturePreparedSocietasAnimationReviewSlots":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.CaptureAnimationReviewSlots,
                        "Prepared Societas animation review slots captured.");
                    break;
                case "ApplyPreparedSocietasIdleBreathTentacleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ApplyIdleBreathTentacleAnimation,
                        "Prepared Societas 01 idle breath tentacle animation applied.");
                    break;
                case "ValidatePreparedSocietasIdleBreathTentacleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ValidateIdleBreathTentacleAnimation,
                        "Prepared Societas 01 idle breath tentacle animation validated.");
                    break;
                case "CapturePreparedSocietasIdleBreathTentacleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.CaptureIdleBreathTentacleAnimation,
                        "Prepared Societas 01 idle breath tentacle animation captured.");
                    break;
                case "InspectPreparedSocietasIdleRigStructure":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.InspectIdleRigStructure,
                        "Prepared Societas 01 idle rig structure inspected.");
                    break;
                case "ApplyPreparedSocietasMoveCaterpillarAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ApplyMoveCaterpillarAnimation,
                        "Prepared Societas 02 move caterpillar animation applied.");
                    break;
                case "ValidatePreparedSocietasMoveCaterpillarAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ValidateMoveCaterpillarAnimation,
                        "Prepared Societas 02 move caterpillar animation validated.");
                    break;
                case "CapturePreparedSocietasMoveCaterpillarAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.CaptureMoveCaterpillarAnimation,
                        "Prepared Societas 02 move caterpillar animation captured.");
                    break;
                case "InspectPreparedSocietasAttackConsumeRigStructure":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.InspectAttackConsumeRigStructure,
                        "Prepared Societas 03 attack consume rig structure inspected.");
                    break;
                case "ApplyPreparedSocietasAttackConsumeBiteChewAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ApplyAttackConsumeBiteChewAnimation,
                        "Prepared Societas 03 attack consume bite chew animation applied.");
                    break;
                case "ApplyPreparedSocietasAttackConsumeBiteChewAnimationVisualOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ApplyAttackConsumeBiteChewAnimationVisualOnly,
                        "Prepared Societas 03 attack consume bite chew animation applied for visual review.");
                    break;
                case "RemovePreparedSocietasAttackConsumeAnimationVisualOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.RemovePreparedSocietasAttackConsumeAnimationVisualOnly,
                        "Prepared Societas 03 attack consume animation removed for visual review.");
                    break;
                case "ValidatePreparedSocietasAttackConsumeBiteChewAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ValidateAttackConsumeBiteChewAnimation,
                        "Prepared Societas 03 attack consume bite chew animation validated.");
                    break;
                case "CapturePreparedSocietasAttackConsumeBiteChewAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.CaptureAttackConsumeBiteChewAnimation,
                        "Prepared Societas 03 attack consume bite chew animation captured.");
                    break;
                case "CapturePreparedSocietasAttackConsumeBiteChewAnimationVisualOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.CaptureAttackConsumeBiteChewAnimationVisualOnly,
                        "Prepared Societas 03 attack consume bite chew animation captured for visual review.");
                    break;
                case "InspectPreparedSocietasAttackConsumeBoneWeightsVisualOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.InspectAttackConsumeBoneWeightsVisualOnly,
                        "Prepared Societas 03 attack consume bone weights inspected for visual retune.");
                    break;
                case "ApplyPreparedSocietasDeathMeltPuddleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ApplyDeathMeltPuddleAnimation,
                        "Prepared Societas 04 death melt puddle animation applied.");
                    break;
                case "ValidatePreparedSocietasDeathMeltPuddleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.ValidateDeathMeltPuddleAnimation,
                        "Prepared Societas 04 death melt puddle animation validated.");
                    break;
                case "CapturePreparedSocietasDeathMeltPuddleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.SocietasCargoRunScene.SocietasCargoRunSceneApplyAndReview.CaptureDeathMeltPuddleAnimation,
                        "Prepared Societas 04 death melt puddle animation captured.");
                    break;
                case "MovePreparedUrzerePlayerStartToOppositeSide":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.MovePlayerStartToOppositeSide,
                        "Prepared Urzere player start moved to the opposite side.");
                    break;
                case "AddPreparedUrzereMotionSlotObjects":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.AddMotionSlotObjectsOnCurrentZAxis,
                        "Prepared Urzere motion slot objects added.");
                    break;
                case "InspectPreparedUrzereMotionSlotObjects":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.InspectMotionSlotObjectsInScene,
                        "Prepared Urzere motion slot objects inspected.");
                    break;
                case "ApplyPreparedUrzereIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ApplyIdleBreathingAnimation,
                        "Prepared Urzere idle breathing animation applied.");
                    break;
                case "ValidatePreparedUrzereIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ValidateIdleBreathingAnimation,
                        "Prepared Urzere idle breathing animation validated.");
                    break;
                case "ApplyPreparedUrzereMoveBodyLiftWheelRollAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ApplyMoveBodyLiftWheelRollAnimation,
                        "Prepared Urzere move body-lift wheel-roll animation applied.");
                    break;
                case "ValidatePreparedUrzereMoveBodyLiftWheelRollAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ValidateMoveBodyLiftWheelRollAnimation,
                        "Prepared Urzere move body-lift wheel-roll animation validated.");
                    break;
                case "ApplyPreparedUrzereMoveWheelOnlyAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ApplyMoveWheelOnlyAnimation,
                        "Prepared Urzere move wheel-only animation applied.");
                    break;
                case "ValidatePreparedUrzereMoveWheelOnlyAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ValidateMoveWheelOnlyAnimation,
                        "Prepared Urzere move wheel-only animation validated.");
                    break;
                case "CapturePreparedUrzereMoveWheelOnlyReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.CaptureMoveWheelOnlyReview,
                        "Prepared Urzere move wheel-only review captured.");
                    break;
                case "RemovePreparedUrzereMoveAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.RemoveMoveAnimationFromScene,
                        "Prepared Urzere move animation removed.");
                    break;
                case "ApplyPreparedUrzereSeedEmitBuffPulseAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ApplySeedEmitBuffPulseAnimation,
                        "Prepared Urzere seed emit buff pulse animation applied.");
                    break;
                case "ValidatePreparedUrzereSeedEmitBuffPulseAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ValidateSeedEmitBuffPulseAnimation,
                        "Prepared Urzere seed emit buff pulse animation validated.");
                    break;
                case "CapturePreparedUrzereSeedEmitBuffPulseReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.CaptureSeedEmitBuffPulseReview,
                        "Prepared Urzere seed emit buff pulse review captured.");
                    break;
                case "ApplyPreparedUrzereDeathAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ApplyDeathAnimation,
                        "Prepared Urzere death animation applied.");
                    break;
                case "ValidatePreparedUrzereDeathAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ValidateDeathAnimation,
                        "Prepared Urzere death animation validated.");
                    break;
                case "CapturePreparedUrzereDeathReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.CaptureDeathReview,
                        "Prepared Urzere death review captured.");
                    break;
                case "InspectPreparedUrzereRendererStructure":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.InspectRendererStructure,
                        "Prepared Urzere renderer structure inspected.");
                    break;
                case "RemovePreparedUrzereGroundPuddle":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.RemoveGroundPuddleFromAllUrzereObjects,
                        "Prepared Urzere ground puddle removed.");
                    break;
                case "ValidatePreparedUrzereGroundPuddleRemoved":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ValidateGroundPuddleRemoval,
                        "Prepared Urzere ground puddle removal validated.");
                    break;
                case "RemovePreparedUrzereOuterFootPlatforms":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.RemoveOuterFootPlatformsFromAllUrzereObjects,
                        "Prepared Urzere outer foot platforms removed.");
                    break;
                case "ValidatePreparedUrzereOuterFootPlatformsRemoved":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.UrzereCargoRunScene.UrzereCargoRunSceneApplyAndReview.ValidateOuterFootPlatformRemoval,
                        "Prepared Urzere outer foot platform removal validated.");
                    break;
                case "CaptureLongaArmaUnityVisualComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaCargoRunSceneApplyAndReview.CaptureUnityVisualComparison,
                        "Approved Longa Arma Unity visual comparison captured.");
                    break;
                case "ApplyLongaArmaLowPolyFromOriginalToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaLowPolyUnityApplyAndReview.ApplyLowPolyFromOriginalToCurrentCargoRunScene,
                        "Longa Arma low-poly-from-original sample applied to current CargoRunMvp scene.");
                    break;
                case "InspectLongaArmaLowPolyFromOriginalCargoRunSceneState":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaLowPolyUnityApplyAndReview.InspectAppliedSceneState,
                        "Longa Arma low-poly-from-original CargoRunMvp scene state inspected.");
                    break;
                case "CaptureLongaArmaLowPolyFromOriginalUnityVisualComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaLowPolyUnityApplyAndReview.CaptureUnityVisualComparison,
                        "Longa Arma low-poly-from-original Unity visual comparison captured.");
                    break;
                case "ApplyLongaArmaWalkingFbxToMoveOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaWalkingFbxApply.ApplyWalkingFbxToMoveOnly,
                        "Longa Arma walking FBX applied to move state only.");
                    break;
                case "ReplaceRemainingLongaArmaWithMoveWalkingFbxCopy":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaWalkingFbxApply
                            .ReplaceRemainingApprovedStatesWithMoveWalkingFbxCopy,
                        "Remaining Longa Arma states replaced with move walking FBX copy.");
                    break;
                case "RemoveNonMoveLongaArmaAnimationComponents":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaWalkingFbxApply
                            .RemoveAnimationComponentsFromNonMoveApprovedStates,
                        "Non-move Longa Arma animation components removed.");
                    break;
                case "ApplyLongaArmaIdleBodyMorph":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaWalkingFbxApply
                            .ApplyIdleBodyMorphToIdleState,
                        "Longa Arma idle body morph applied.");
                    break;
                case "ApplyLongaArmaAttackSlamDrag":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaWalkingFbxApply
                            .ApplyAttackSlamDragToAttackState,
                        "Longa Arma attack slam-drag applied.");
                    break;
                case "ApplyLongaArmaHitRecoil":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaWalkingFbxApply
                            .ApplyHitRecoilToHitState,
                        "Longa Arma hit recoil applied.");
                    break;
                case "ApplyLongaArmaConsumePeck":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaWalkingFbxApply
                            .ApplyConsumePeckToConsumeState,
                        "Longa Arma consume peck applied.");
                    break;
                case "ApplyLongaArmaDeathMeltPuddle":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaWalkingFbxApply
                            .ApplyDeathMeltPuddleToDeathState,
                        "Longa Arma death melt-puddle applied.");
                    break;
                case "ApplyTergoAnimationPlacement":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoAnimationPlacement.ApplyTergoAnimationPlacement,
                        "Tergo animation placement applied.");
                    break;
                case "ApplyTergoApprovedVisualsToCurrentCargoRunScene":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoApprovedVisualApply.ApplyApprovedVisualsToCurrentCargoRunScene,
                        "Approved Tergo visuals applied to current CargoRunMvp scene.");
                    break;
                case "CaptureTergoApprovedEyeShapeComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoApprovedVisualApply.CaptureApprovedEyeShapeComparison,
                        "Approved Tergo eye shape comparison captured.");
                    break;
                case "ApplyTergoIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoIdleBreathingAnimation.ApplyTergoIdleBreathingAnimation,
                        "Tergo idle breathing animation applied.");
                    break;
                case "ValidateTergoIdleBreathingAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoIdleBreathingAnimation.ValidateTergoIdleBreathingAnimation,
                        "Tergo idle breathing animation validated.");
                    break;
                case "ApplyTergoWalkWanderImportedAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoWalkWanderImportedAnimation.ApplyTergoWalkWanderImportedAnimation,
                        "Tergo walk wander imported animation applied.");
                    break;
                case "ValidateTergoWalkWanderImportedAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoWalkWanderImportedAnimation.ValidateTergoWalkWanderImportedAnimation,
                        "Tergo walk wander imported animation validated.");
                    break;
                case "ApplyTergoRunChaseAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoRunChaseAnimation,
                        "Tergo run chase animation applied.");
                    break;
                case "ValidateTergoRunChaseAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoRunChaseAnimation,
                        "Tergo run chase animation validated.");
                    break;
                case "ReplaceTergoBackRushWithRunningModel":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ReplaceTergoBackRushWithRunningModel,
                        "Tergo BackRush running model replaced.");
                    break;
                case "ValidateTergoBackRushRunningModel":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushRunningModel,
                        "Tergo BackRush running model validated.");
                    break;
                case "SyncTergoBackRushVisualDetails":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoBackRushVisualDetails,
                        "Tergo BackRush visual details synced.");
                    break;
                case "ValidateTergoBackRushVisualDetails":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushVisualDetails,
                        "Tergo BackRush visual details validated.");
                    break;
                case "ApplyTergoPierceAttackAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoPierceAttackAnimation,
                        "Tergo pierce attack animation applied.");
                    break;
                case "ValidateTergoPierceAttackAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoPierceAttackAnimation,
                        "Tergo pierce attack animation validated.");
                    break;
                case "ReplaceTergoPierceAttackWithThrustFbx":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ReplaceTergoPierceAttackWithThrustFbx,
                        "Tergo pierce attack thrust FBX replacement applied.");
                    break;
                case "ApplyTergoPierceAttackThrustFbxAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoPierceAttackThrustFbxAnimation,
                        "Tergo pierce attack thrust FBX animation applied.");
                    break;
                case "SyncTergoPierceAttackVisualDetailsFromStaticReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoPierceAttackVisualDetailsFromStaticReview,
                        "Tergo pierce attack visual details synced from static review.");
                    break;
                case "SyncTergoPierceAttackLightsFromStaticReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoPierceAttackLightsFromStaticReview,
                        "Tergo pierce attack lights synced from static review.");
                    break;
                case "ReplaceTergoDownedPounceWithTakedownFbx":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ReplaceTergoDownedPounceWithTakedownFbx,
                        "Tergo downed pounce takedown FBX replacement applied.");
                    break;
                case "ApplyTergoDownedPounceTakedownFbxLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoDownedPounceTakedownFbxLoop,
                        "Tergo downed pounce takedown FBX loop applied.");
                    break;
                case "SyncTergoDownedPounceVisualDetailsFromStaticReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoDownedPounceVisualDetailsFromStaticReview,
                        "Tergo downed pounce visual details synced from static review.");
                    break;
                case "ApplyTergoStandUpAfterFallFbxLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoStandUpAfterFallFbxLoop,
                        "Tergo stand up after fall FBX loop applied.");
                    break;
                case "ValidateTergoStandUpAfterFallFbxLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoStandUpAfterFallFbxLoop,
                        "Tergo stand up after fall FBX loop validated.");
                    break;
                case "SyncTergoStandUpAfterFallVisualDetailsFromStaticReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoStandUpAfterFallVisualDetailsFromStaticReview,
                        "Tergo stand up after fall visual details synced from static review.");
                    break;
                case "ReplaceTergoInterruptStaggerWithFallOverFbx":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ReplaceTergoInterruptStaggerWithFallOverFbx,
                        "Tergo interrupt stagger fall-over FBX replacement applied.");
                    break;
                case "ApplyTergoInterruptStaggerFallOverFbxLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoInterruptStaggerFallOverFbxLoop,
                        "Tergo interrupt stagger fall-over FBX loop applied.");
                    break;
                case "SyncTergoInterruptStaggerVisualDetailsFromStaticReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoInterruptStaggerVisualDetailsFromStaticReview,
                        "Tergo interrupt stagger visual details synced from static review.");
                    break;
                case "ApplyTergoInterruptStaggerBackwardFall":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoInterruptStaggerBackwardFall,
                        "Tergo interrupt stagger backward fall applied.");
                    break;
                case "ValidateTergoInterruptStaggerBackwardFall":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoInterruptStaggerBackwardFall,
                        "Tergo interrupt stagger backward fall validated.");
                    break;
                case "ApplyTergoCrouchTremble5s":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoCrouchTremble5s,
                        "Tergo crouch tremble 5s applied.");
                    break;
                case "ValidateTergoCrouchTremble5s":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoCrouchTremble5s,
                        "Tergo crouch tremble 5s validated.");
                    break;
                case "ReplaceTergoCrouchTrembleWithTerrifiedFbx":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ReplaceTergoCrouchTrembleWithTerrifiedFbx,
                        "Tergo crouch tremble terrified FBX replacement applied.");
                    break;
                case "ApplyTergoCrouchTrembleTerrifiedFbxLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoCrouchTrembleTerrifiedFbxLoop,
                        "Tergo crouch tremble terrified FBX loop applied.");
                    break;
                case "ValidateTergoCrouchTrembleTerrifiedFbxLoop":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoCrouchTrembleTerrifiedFbxLoop,
                        "Tergo crouch tremble terrified FBX loop validated.");
                    break;
                case "SyncTergoCrouchTrembleVisualDetailsFromStaticReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoCrouchTrembleVisualDetailsFromStaticReview,
                        "Tergo crouch tremble visual details synced from static review.");
                    break;
                case "ApplyTergoHitNormal":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoHitNormal,
                        "Tergo hit normal applied.");
                    break;
                case "ValidateTergoHitNormal":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoHitNormal,
                        "Tergo hit normal validated.");
                    break;
                case "ApplyTergoHittedModelAsHitNormal":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoHittedModelAsHitNormal,
                        "Tergo hit normal hitted FBX applied.");
                    break;
                case "ValidateTergoHittedModelAsHitNormal":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoHittedModelAsHitNormal,
                        "Tergo hit normal hitted FBX validated.");
                    break;
                case "ApplyTergoDyingModelAsDeath":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoDyingModelAsDeath,
                        "Tergo death dying FBX applied.");
                    break;
                case "ValidateTergoDyingModelAsDeath":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoDyingModelAsDeath,
                        "Tergo death dying FBX validated.");
                    break;
                case "SyncTergoDeathVisualDetailsFromStaticReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoDeathVisualDetailsFromStaticReview,
                        "Tergo death visual details synced from static review.");
                    break;
                case "ApplyTergoApprovedDeathMeltPuddleModel":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoApprovedDeathMeltPuddleModel,
                        "Tergo approved death melt puddle model applied.");
                    break;
                case "ValidateTergoApprovedDeathMeltPuddleModel":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoApprovedDeathMeltPuddleModel,
                        "Tergo approved death melt puddle model validated.");
                    break;
                case "CaptureTergoApprovedDeathMeltPuddlePoseFrames":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.CaptureTergoApprovedDeathMeltPuddlePoseFrames,
                        "Tergo approved death melt puddle pose frames captured.");
                    break;
                case "ApplyTergoDeathMeltPuddleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoDeathMeltPuddleAnimation,
                        "Tergo death melt puddle animation applied.");
                    break;
                case "ValidateTergoDeathMeltPuddleAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoDeathMeltPuddleAnimation,
                        "Tergo death melt puddle animation validated.");
                    break;
                case "SyncTergoHitNormalVisualDetailsFromStaticReview":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.SyncTergoHitNormalVisualDetailsFromStaticReview,
                        "Tergo hit normal visual details synced from static review.");
                    break;
                case "InspectTergoPierceAttackRuntimePlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.InspectTergoPierceAttackRuntimePlayback,
                        "Tergo pierce attack runtime playback inspected.");
                    break;
                case "RepairTergoPierceAttackRuntimePlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.RepairTergoPierceAttackRuntimePlayback,
                        "Tergo pierce attack runtime playback repaired.");
                    break;
                case "ValidateTergoPierceAttackRuntimePlayback":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoPierceAttackRuntimePlayback,
                        "Tergo pierce attack runtime playback validated.");
                    break;
                case TergoPierceAttackCurrentSceneVisualRunCommand:
                    RunTergoPierceAttackCurrentSceneVisualRun(request);
                    break;
                case Dolore04TentacleStabDiagnosticCommand:
                    RunDolore04TentacleStabFullMotionCapture(request, false);
                    break;
                case Dolore04TentacleStabFinalCommand:
                    RunDolore04TentacleStabFullMotionCapture(request, true);
                    break;
                case Ispant08ContinuousMotionCaptureCommand:
                    RunIspant08ContinuousMotionCapture(request);
                    break;
                case AtaPistolTriggerFollowCaptureCommand:
                    RunAtaPistolTriggerFollowCapture(request);
                    break;
                case AtaCommandStanceAlternationCaptureCommand:
                    RunAtaCommandStanceAlternationCapture(request);
                    break;
                case Dolore05ExecutionOpeningDiagnosticCommand:
                    RunDolore05ExecutionOpeningCapture(request);
                    break;
                case "InspectTergoBackRushAuthoredSprintRig":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.InspectTergoBackRushAuthoredSprintRig,
                        "Tergo BackRush authored sprint rig inspected.");
                    break;
                case "ApplyTergoBackRushAuthoredSprint":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoBackRushAuthoredSprint,
                        "Tergo BackRush authored sprint applied.");
                    break;
                case "ValidateTergoBackRushAuthoredSprint":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushAuthoredSprint,
                        "Tergo BackRush authored sprint validated.");
                    break;
                case "InspectTergoBackRushSprintReferenceMotion":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.InspectTergoBackRushSprintReferenceMotion,
                        "Tergo BackRush sprint reference motion inspected.");
                    break;
                case "RewriteTergoBackRushAuthoredSprintFromReference":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.RewriteTergoBackRushAuthoredSprintFromReference,
                        "Tergo BackRush authored sprint rewritten from reference.");
                    break;
                case "ValidateTergoBackRushAuthoredSprintRewrite":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushAuthoredSprintRewrite,
                        "Tergo BackRush authored sprint rewrite validated.");
                    break;
                case "InspectTergoBackRushSprintVideoReference":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.InspectTergoBackRushSprintVideoReference,
                        "Tergo BackRush sprint video reference inspected.");
                    break;
                case "RewriteTergoBackRushSprintToVideoReference":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.RewriteTergoBackRushSprintToVideoReference,
                        "Tergo BackRush sprint rewritten to video reference.");
                    break;
                case "ValidateTergoBackRushSprintVideoReference":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushSprintVideoReference,
                        "Tergo BackRush sprint video reference validated.");
                    break;
                case "RemoveTergoRunChaseAnimation":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.RemoveTergoRunChaseAnimation,
                        "Tergo run chase animation removed.");
                    break;
                case "ValidateTergoRunChaseAnimationRemoved":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoRunChaseAnimationRemoved,
                        "Tergo run chase animation removal validated.");
                    break;
                case "RestoreTergoBackRushVisualModel":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.RestoreTergoBackRushVisualModel,
                        "Tergo BackRush visual model restored.");
                    break;
                case "ValidateTergoBackRushVisualModel":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushVisualModel,
                        "Tergo BackRush visual model validated.");
                    break;
                case "ReplaceTergoBackRushRigOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ReplaceTergoBackRushRigOnly,
                        "Tergo BackRush rig-only replacement applied.");
                    break;
                case "ValidateTergoBackRushRigOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushRigOnly,
                        "Tergo BackRush rig-only replacement validated.");
                    break;
                case "ApplyTergoBackRushAnimationOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ApplyTergoBackRushAnimationOnly,
                        "Tergo BackRush animation-only application completed.");
                    break;
                case "ValidateTergoBackRushAnimationOnly":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushAnimationOnly,
                        "Tergo BackRush animation-only application validated.");
                    break;
                case "InspectTergoBackRushWaistTwist":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.InspectTergoBackRushWaistTwist,
                        "Tergo BackRush waist twist inspected.");
                    break;
                case "RepairTergoBackRushWaistTwist":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.RepairTergoBackRushWaistTwist,
                        "Tergo BackRush waist twist repaired.");
                    break;
                case "ValidateTergoBackRushWaistTwistFixed":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushWaistTwistFixed,
                        "Tergo BackRush waist twist fix validated.");
                    break;
                case "InspectTergoBackRushRunningPose":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.InspectTergoBackRushRunningPose,
                        "Tergo BackRush running pose inspected.");
                    break;
                case "RepairTergoBackRushRunningPose":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.RepairTergoBackRushRunningPose,
                        "Tergo BackRush running pose repaired.");
                    break;
                case "ValidateTergoBackRushRunningPose":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushRunningPose,
                        "Tergo BackRush running pose validated.");
                    break;
                case "InspectTergoBackRushNormalRun":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.InspectTergoBackRushNormalRun,
                        "Tergo BackRush normal run inspected.");
                    break;
                case "RepairTergoBackRushNormalRun":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.RepairTergoBackRushNormalRun,
                        "Tergo BackRush normal run repaired.");
                    break;
                case "ValidateTergoBackRushNormalRun":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.TergoCargoRunScene.TergoRunChaseAnimation.ValidateTergoBackRushNormalRun,
                        "Tergo BackRush normal run validated.");
                    break;
                case "RestoreFugaAndLongaArmaPlacementsFromRecoveryScene":
                    RunSynchronous(
                        request,
                        FugaLongaArmaPlacementRecovery.RestoreFugaAndLongaArmaPlacementsFromRecoveryScene,
                        "Fuga and Longa Arma placement roots restored from recovery scene.");
                    break;
                case "MoveApprovedSupplyRoomShellBelowEngineRoom":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.MoveApprovedSupplyRoomShellBelowEngineRoom,
                        "Approved supply room 01 shell moved below engine room.");
                    break;
                case "SwapApprovedSupplyRoomCabinetAndEjectionBay":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.SwapApprovedSupplyRoomCabinetAndEjectionBay,
                        "Approved supply room cabinet and ejection bay swapped.");
                    break;
                case "AddApprovedSupplyRoomSr08Only":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.AddApprovedSupplyRoomSr08Only,
                        "Approved supply room SR-08 ejection floor panel added only.");
                    break;
                case "AddApprovedSupplyRoomSr11TextOnly":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.AddApprovedSupplyRoomSr11TextOnly,
                        "Approved supply room SR-11 corridor text added only.");
                    break;
                case "AddApprovedSupplyRoomSr12CctvOnly":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.AddApprovedSupplyRoomSr12CctvOnly,
                        "Approved supply room SR-12 CCTV added only.");
                    break;
                case "AddApprovedSupplyRoomSr07HskScreenOnly":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.AddApprovedSupplyRoomSr07HskScreenOnly,
                        "Approved supply room SR-07 HSK terminal screen added only.");
                    break;
                case "CaptureApprovedSupplyRoomShellCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.CaptureApprovedSupplyRoomShellCurrentState,
                        "Approved supply room 01 current state capture saved.");
                    break;
                case "CaptureApprovedCargoHoldShellCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.CaptureApprovedCargoHoldShellCurrentState,
                        "Approved cargo hold 01 current state capture saved.");
                    break;
                case "RestoreApprovedSupplyRoomShellCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.RestoreApprovedSupplyRoomShellCurrentState,
                        "Approved supply room 01 current state restored and saved.");
                    break;
                case "RestoreApprovedCargoHoldShellCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.RestoreApprovedCargoHoldShellCurrentState,
                        "Approved cargo hold 01 current state restored and saved.");
                    break;
                case "RestoreApprovedSupplyRoomShellCurrentStateByName":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.RestoreApprovedSupplyRoomShellCurrentStateByName,
                        "Approved supply room 01 current state restored by name and saved.");
                    break;
                case "MoveApprovedArmoryShellToZBelowControlRoom":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.MoveApprovedArmoryShellToZBelowControlRoom,
                        "Approved armory 01 shell moved to Z-below control room.");
                    break;
                case "UpdateApprovedArmoryAr03Only":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.UpdateApprovedArmoryAr03Only,
                        "Approved armory AR-03 updated only.");
                    break;
                case "UpdateApprovedArmoryAr05Only":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.UpdateApprovedArmoryAr05Only,
                        "Approved armory AR-05 updated only.");
                    break;
                case "UpdateApprovedArmoryAr02Ar03Only":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.UpdateApprovedArmoryAr02Ar03Only,
                        "Approved armory AR-02 and AR-03 updated only.");
                    break;
                case "CaptureApprovedArmoryShellCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.CaptureCurrentEditorObjects,
                        "Approved armory 01 current state capture saved:");
                    break;
                case "RestoreApprovedArmoryShellCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedArmoryShellBootstrap.RestoreApprovedArmoryShellCurrentState,
                        "Approved armory 01 current state restored and saved.");
                    break;
                case "EnsureApprovedControlRoomShell":
                    RunSynchronous(
                        request,
                        ApprovedControlRoomShellBootstrap.EnsureApprovedControlRoomShell,
                        "Approved CR-01 control room shell applied.");
                    break;
                case "EnsureApprovedControlRoomAuxScreen":
                    RunSynchronous(
                        request,
                        ApprovedControlRoomAuxScreenBootstrap.EnsureApprovedControlRoomAuxScreen,
                        "Approved CR-07 control room auxiliary screen applied.");
                    break;
                case "EnsureApprovedControlRoomVerticalAuxScreens":
                    RunSynchronous(
                        request,
                        ApprovedControlRoomVerticalAuxScreensBootstrap.EnsureApprovedControlRoomVerticalAuxScreens,
                        "Approved CR-08 control room vertical auxiliary screens applied.");
                    break;
                case "EnsureApprovedControlRoomDirectionLabels":
                    RunSynchronous(
                        request,
                        ApprovedControlRoomDirectionLabelsBootstrap.EnsureApprovedControlRoomDirectionLabels,
                        "Approved CR-17 control room direction labels applied.");
                    break;
                case "CaptureApprovedControlRoomShellCurrentObjects":
                    RunSynchronous(
                        request,
                        ApprovedControlRoomShellBootstrap.CaptureCurrentEditorObjects,
                        "Approved CR-01 current object capture saved:");
                    break;
                case "CaptureApprovedControlRoomCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedControlRoomCurrentStateCapture.CaptureCurrentEditorObjects,
                        "Approved control room current state capture saved:");
                    break;
                case "EnsureApprovedEngineRoomHealthScreen":
                    RunSynchronous(
                        request,
                        ApprovedEngineRoomHealthScreenBootstrap.EnsureApprovedEngineRoomHealthScreen,
                        "Approved ER-09 engine room health screen applied.");
                    break;
                case "FlipApprovedEngineRoomHealthScreenDisplayUvs":
                    RunSynchronous(
                        request,
                        ApprovedEngineRoomHealthScreenBootstrap.FlipApprovedEngineRoomHealthScreenDisplayUvs,
                        "Approved ER-09 display UVs flipped horizontally:");
                    break;
                case "CaptureApprovedEngineRoomHealthScreenCurrentObjects":
                    RunSynchronous(
                        request,
                        ApprovedEngineRoomHealthScreenBootstrap.CaptureCurrentEditorObjects,
                        "Approved ER-09 current object capture saved:");
                    break;
                case "CaptureApprovedEngineRoomShellCurrentObjects":
                    RunSynchronous(
                        request,
                        ApprovedEngineRoomShellBootstrap.CaptureCurrentEditorObjects,
                        "Approved engine room 01 current object capture saved:");
                    break;
                case "ValidatePhase1SessionModels":
                    RunSynchronous(
                        request,
                        Phase1SessionModelsEditorValidation.Run,
                        "Phase 1 session models editor validation passed.");
                    break;
                case "EnsurePhase2PlayerMvp":
                    RunSynchronous(
                        request,
                        Phase2PlayerMvpBootstrap.EnsurePhase2Assets,
                        "Phase 2 player MVP assets are ready.");
                    break;
                case "ValidatePhase2PlayerMvp":
                    RunSynchronous(
                        request,
                        Phase2PlayerMvpEditorValidation.Run,
                        "Phase 2 player MVP editor validation passed.");
                    break;
                case "ValidatePhase3InteractionSystem":
                    RunSynchronous(
                        request,
                        Phase3InteractionSystemEditorValidation.Run,
                        "Phase 3 interaction system editor validation passed.");
                    break;
                case "EnsurePhase4CargoShipGraybox":
                    RunSynchronous(
                        request,
                        Phase4CargoShipGrayboxBootstrap.EnsurePhase4Assets,
                        "Phase 4 cargo ship graybox assets are ready.");
                    break;
                case "ValidatePhase4CargoShipGraybox":
                    RunSynchronous(
                        request,
                        Phase4CargoShipGrayboxEditorValidation.Run,
                        "Phase 4 cargo ship graybox editor validation passed.");
                    break;
                case "ApplyModeledCockpitPlayStart":
                    RunSynchronous(
                        request,
                        Phase4CargoShipGrayboxBootstrap.ApplyModeledCockpitPlayStart,
                        "Modeled cockpit play start applied.");
                    break;
                case "ValidatePhase5ShipStateModels":
                    RunSynchronous(
                        request,
                        Phase5ShipStateModelsEditorValidation.Run,
                        "Phase 5 ship state models editor validation passed.");
                    break;
                case "EnsurePhase6RoomInteractions":
                    RunSynchronous(
                        request,
                        Phase6RoomInteractionsBootstrap.EnsurePhase6Assets,
                        "Phase 6 room interaction assets are ready.");
                    break;
                case "ValidatePhase6RoomInteractions":
                    RunSynchronous(
                        request,
                        Phase6RoomInteractionsEditorValidation.Run,
                        "Phase 6 room interactions editor validation passed.");
                    break;
                case "EnsurePhase7NewGameStart":
                    RunSynchronous(
                        request,
                        Phase7NewGameStartBootstrap.EnsurePhase7Assets,
                        "Phase 7 new game start assets are ready.");
                    break;
                case "ValidatePhase7NewGameStart":
                    RunSynchronous(
                        request,
                        Phase7NewGameStartEditorValidation.Run,
                        "Phase 7 new game start editor validation passed.");
                    break;
                case "EnsurePhase8TransportRun":
                    RunSynchronous(
                        request,
                        Phase8TransportRunBootstrap.EnsurePhase8Assets,
                        "Phase 8 transport run assets are ready.");
                    break;
                case "ValidatePhase8TransportRun":
                    RunSynchronous(
                        request,
                        Phase8TransportRunEditorValidation.Run,
                        "Phase 8 transport run editor validation passed.");
                    break;
                case "EnsurePhase9SettlementGameOver":
                    RunSynchronous(
                        request,
                        Phase9SettlementGameOverBootstrap.EnsurePhase9Assets,
                        "Phase 9 settlement and game over assets are ready.");
                    break;
                case "ValidatePhase9SettlementGameOver":
                    RunSynchronous(
                        request,
                        Phase9SettlementGameOverEditorValidation.Run,
                        "Phase 9 settlement game over editor validation passed.");
                    break;
                case "EnsurePhase10PlanetMaintenance":
                    RunSynchronous(
                        request,
                        Phase10PlanetMaintenanceBootstrap.EnsurePhase10Assets,
                        "Phase 10 planet maintenance assets are ready.");
                    break;
                case "ValidatePhase10PlanetMaintenance":
                    RunSynchronous(
                        request,
                        Phase10PlanetMaintenanceEditorValidation.Run,
                        "Phase 10 planet maintenance editor validation passed.");
                    break;
                case "EnsurePhase11AsteroidHazard":
                    RunSynchronous(
                        request,
                        Phase11AsteroidHazardBootstrap.EnsurePhase11Assets,
                        "Phase 11 asteroid hazard assets are ready.");
                    break;
                case "ValidatePhase11AsteroidHazard":
                    RunSynchronous(
                        request,
                        Phase11AsteroidHazardEditorValidation.Run,
                        "Phase 11 asteroid hazard editor validation passed.");
                    break;
                case "EnsurePhase12ManualTurret":
                    RunSynchronous(
                        request,
                        Phase12ManualTurretBootstrap.EnsurePhase12Assets,
                        "Phase 12 manual turret assets are ready.");
                    break;
                case "ValidatePhase12ManualTurret":
                    RunSynchronous(
                        request,
                        Phase12ManualTurretEditorValidation.Run,
                        "Phase 12 manual turret editor validation passed.");
                    break;
                case "EnsurePhase13IntruderFramework":
                    RunSynchronous(
                        request,
                        Phase13IntruderFrameworkBootstrap.EnsurePhase13Assets,
                        "Phase 13 intruder framework assets are ready.");
                    break;
                case "ValidatePhase13IntruderFramework":
                    RunSynchronous(
                        request,
                        Phase13IntruderFrameworkEditorValidation.Run,
                        "Phase 13 intruder framework editor validation passed.");
                    break;
                case "EnsurePhase14ParvumIntruder":
                    RunSynchronous(
                        request,
                        Phase14ParvumIntruderBootstrap.EnsurePhase14Assets,
                        "Phase 14 parvum intruder assets are ready.");
                    break;
                case "ValidatePhase14ParvumIntruder":
                    RunSynchronous(
                        request,
                        Phase14ParvumIntruderEditorValidation.Run,
                        "Phase 14 parvum intruder editor validation passed.");
                    break;
                case "EnsurePhase15EquipmentLoop":
                    RunSynchronous(
                        request,
                        Phase15EquipmentLoopBootstrap.EnsurePhase15Assets,
                        "Phase 15 equipment loop assets are ready.");
                    break;
                case "ValidatePhase15EquipmentLoop":
                    RunSynchronous(
                        request,
                        Phase15EquipmentLoopEditorValidation.Run,
                        "Phase 15 equipment loop editor validation passed.");
                    break;
                case "EnsurePhase16HudMapAtmosphere":
                    RunSynchronous(
                        request,
                        Phase16HudMapAtmosphereBootstrap.EnsurePhase16Assets,
                        "Phase 16 HUD map atmosphere assets are ready.");
                    break;
                case "ValidatePhase16HudMapAtmosphere":
                    RunSynchronous(
                        request,
                        Phase16HudMapAtmosphereEditorValidation.Run,
                        "Phase 16 HUD map atmosphere editor validation passed.");
                    break;
                case "ValidatePhase17CoopFoundation":
                    RunSynchronous(
                        request,
                        Phase17CoopFoundationEditorValidation.Run,
                        "Phase 17 coop foundation editor validation passed.");
                    break;
                case "ValidateDetailedStep13SeedEntity":
                    RunSynchronous(
                        request,
                        DetailedStep13SeedEntityEditorValidation.Run,
                        "Detailed step 13 seed entity editor validation passed.");
                    break;
                case "ValidateDetailedStep14AlienLifeform":
                    RunSynchronous(
                        request,
                        DetailedStep14AlienLifeformEditorValidation.Run,
                        "Detailed step 14 alien lifeform editor validation passed.");
                    break;
                case "ValidateDetailedStep15CargoFreedomLeague":
                    RunSynchronous(
                        request,
                        DetailedStep15CargoFreedomLeagueEditorValidation.Run,
                        "Detailed step 15 Cargo Freedom League editor validation passed.");
                    break;
                case "ValidateDetailedStep16SpacePirate":
                    RunSynchronous(
                        request,
                        DetailedStep16SpacePirateEditorValidation.Run,
                        "Detailed step 16 space pirate editor validation passed.");
                    break;
                case "ValidateDetailedStep17SpecialContracts":
                    RunSynchronous(
                        request,
                        DetailedStep17SpecialContractsEditorValidation.Run,
                        "Detailed step 17 special contracts editor validation passed.");
                    break;
                case "ValidateDetailedStep18PlanetUx":
                    RunSynchronous(
                        request,
                        DetailedStep18PlanetUxEditorValidation.Run,
                        "Detailed step 18 planet UX editor validation passed.");
                    break;
                case "ValidateDetailedStep19SaveSettingsPlatform":
                    RunSynchronous(
                        request,
                        DetailedStep19SaveSettingsPlatformEditorValidation.Run,
                        "Detailed step 19 save settings platform editor validation passed.");
                    break;
                case "EnsurePhase20Presentation":
                    RunSynchronous(
                        request,
                        Phase20PresentationBootstrap.EnsurePhase20Assets,
                        "Phase 20 presentation polish assets are ready.");
                    break;
                case "ValidatePhase20Presentation":
                    RunSynchronous(
                        request,
                        Phase20PresentationEditorValidation.Run,
                        "Phase 20 presentation polish editor validation passed.");
                    break;
                case "ValidateDetailedStep20Presentation":
                    RunSynchronous(
                        request,
                        DetailedStep20PresentationEditorValidation.Run,
                        "Detailed step 20 presentation editor validation passed.");
                    break;
                case "ValidateDetailedStep21BalancePlaytestHardening":
                    RunSynchronous(
                        request,
                        DetailedStep21BalancePlaytestHardeningEditorValidation.Run,
                        "Detailed step 21 balance playtest hardening editor validation passed.");
                    break;
                case "ValidatePostDetailedStage2ShipInterior":
                    RunSynchronous(
                        request,
                        PostDetailedStage2ShipInteriorEditorValidation.Run,
                        "Post-detailed stage 2 ship interior editor validation passed.");
                    break;
                case "ValidatePostDetailedStage3GameplayProps":
                    RunSynchronous(
                        request,
                        PostDetailedStage3GameplayPropsEditorValidation.Run,
                        "Post-detailed stage 3 gameplay props editor validation passed.");
                    break;
                case "ValidatePostDetailedStage3GameplayPropsArtOnly":
                    RunSynchronous(
                        request,
                        PostDetailedStage3GameplayPropsEditorValidation.ValidateScene,
                        "Post-detailed stage 3 gameplay props editor validation passed.");
                    break;
                case "CapturePostDetailedStage3GameplayPropsArtSnapshots":
                    RunSynchronous(
                        request,
                        PostDetailedStage3GameplayPropsEditorValidation.CaptureUnityComparisonSnapshots,
                        "Stage 3 art sample Unity comparison snapshots saved:");
                    break;
                case "ValidateAssetStoreShipDressingStep1":
                    RunSynchronous(
                        request,
                        AssetStoreShipDressingEditorValidation.Run,
                        "Asset Store ship dressing step 1 validation passed.");
                    break;
                case "ValidateAssetStoreShipDressingStep2":
                    RunSynchronous(
                        request,
                        AssetStoreShipDressingEditorValidation.RunStep2,
                        "Asset Store ship dressing step 2 corridor validation passed.");
                    break;
                case "CaptureAssetStoreShipDressingStep2Comparison":
                    RunSynchronous(
                        request,
                        AssetStoreShipDressingEditorValidation.CaptureApprovedStep2Comparison,
                        "Asset Store ship dressing step 2 Unity comparison snapshots saved:");
                    break;
                case "CaptureAssetDressingStep02Sample":
                    RunSynchronous(
                        request,
                        AssetDressingStep02SampleRenderer.Capture,
                        "Asset dressing step 02 sample renders saved:");
                    break;
                case "CaptureAssetDressingStep02WornSample":
                    RunSynchronous(
                        request,
                        AssetDressingStep02SampleRenderer.CaptureWorn,
                        "Asset dressing step 02 worn sample renders saved:");
                    break;
                case "CaptureAssetDressingStep02SteelPlateSample":
                    RunSynchronous(
                        request,
                        AssetDressingStep02SampleRenderer.CaptureSteelPlate,
                        "Asset dressing step 02 steel plate sample renders saved:");
                    break;
                case "CaptureAssetDressingStep02BumpyWornPlateSample":
                    RunSynchronous(
                        request,
                        AssetDressingStep02SampleRenderer.CaptureBumpyWornPlate,
                        "Asset dressing step 02 bumpy worn plate sample renders saved:");
                    break;
                case "CaptureAssetInventoryCatalog":
                    RunSynchronous(
                        request,
                        AssetInventoryCatalogRenderer.Capture,
                        "Asset inventory catalog saved:");
                    break;
                case "CaptureAssetDressingStep02SelectedCorridorSample":
                    RunSynchronous(
                        request,
                        AssetDressingStep02SelectedCorridorSampleRenderer.Capture,
                        "Asset dressing step 02 selected corridor sample renders saved:");
                    break;
                case "EnsureApprovedCockpitStructure":
                    RunSynchronous(
                        request,
                        ApprovedCockpitStructureBootstrap.EnsureApprovedCockpitStructure,
                        "Approved cockpit 01 structure applied.");
                    break;
                case "ValidateApprovedCockpitStructure":
                    RunSynchronous(
                        request,
                        ApprovedCockpitStructureBootstrap.ValidateScene,
                        "Approved cockpit 01 structure validation passed.");
                    break;
                case "CaptureApprovedCockpitStructureComparison":
                    RunSynchronous(
                        request,
                        ApprovedCockpitStructureBootstrap.CaptureUnityComparison,
                        "Approved cockpit 01 Unity comparison snapshots saved:");
                    break;
                case "EnsureApprovedCockpitWindow":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWindowBootstrap.EnsureApprovedCockpitWindow,
                        "Approved cockpit 01 window applied.");
                    break;
                case "ValidateApprovedCockpitWindow":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWindowBootstrap.ValidateScene,
                        "Approved cockpit 01 window validation passed.");
                    break;
                case "CaptureApprovedCockpitWindowComparison":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWindowBootstrap.CaptureUnityComparison,
                        "Approved cockpit 01 window Unity comparison snapshots saved:");
                    break;
                case "CaptureApprovedCockpitWindowCurrentTransforms":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWindowBootstrap.CaptureCurrentEditorTransforms,
                        "Approved cockpit 01 window current transform capture saved:");
                    break;
                case "EnsureApprovedCockpitConsole":
                    RunSynchronous(
                        request,
                        ApprovedCockpitConsoleBootstrap.EnsureApprovedCockpitConsole,
                        "Approved cockpit 02 console applied.");
                    break;
                case "ValidateApprovedCockpitConsole":
                    RunSynchronous(
                        request,
                        ApprovedCockpitConsoleBootstrap.ValidateScene,
                        "Approved cockpit 02 console validation passed.");
                    break;
                case "CaptureApprovedCockpitConsoleComparison":
                    RunSynchronous(
                        request,
                        ApprovedCockpitConsoleBootstrap.CaptureUnityComparison,
                        "Approved cockpit 02 console Unity comparison snapshots saved:");
                    break;
                case "CaptureApprovedCockpitConsoleCurrentObjects":
                    RunSynchronous(
                        request,
                        ApprovedCockpitConsoleBootstrap.CaptureCurrentEditorObjects,
                        "Approved cockpit 02 console current object capture saved:");
                    break;
                case "EnsureApprovedCockpitDestroyedConsole":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDestroyedConsoleBootstrap.EnsureApprovedCockpitDestroyedConsole,
                        "Approved cockpit 09 destroyed console applied.");
                    break;
                case "ValidateApprovedCockpitDestroyedConsole":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDestroyedConsoleBootstrap.ValidateScene,
                        "Approved cockpit 09 destroyed console validation passed.");
                    break;
                case "CaptureApprovedCockpitDestroyedConsoleComparison":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDestroyedConsoleBootstrap.CaptureUnityComparison,
                        "Approved cockpit 09 destroyed console Unity comparison snapshots saved:");
                    break;
                case "ShowApprovedCockpitDestroyedConsoleForInspection":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDestroyedConsoleBootstrap.ShowDestroyedConsoleForInspection,
                        "Approved cockpit 09 destroyed console shown for inspection.");
                    break;
                case "ShowApprovedCockpitNormalConsoleForInspection":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDestroyedConsoleBootstrap.ShowNormalConsoleForInspection,
                        "Approved cockpit 02 normal console shown for inspection.");
                    break;
                case "CaptureApprovedCockpitDestroyedConsoleCurrentObjects":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDestroyedConsoleBootstrap.CaptureCurrentEditorObjects,
                        "Approved cockpit 09 current object capture saved:");
                    break;
                case "EnsureApprovedCockpitWarning":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWarningBootstrap.EnsureApprovedCockpitWarning,
                        "Approved cockpit 04 warning applied.");
                    break;
                case "ValidateApprovedCockpitWarning":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWarningBootstrap.ValidateScene,
                        "Approved cockpit 04 warning validation passed.");
                    break;
                case "CaptureApprovedCockpitWarningComparison":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWarningBootstrap.CaptureUnityComparison,
                        "Approved cockpit 04 warning Unity comparison snapshots saved:");
                    break;
                case "CaptureApprovedCockpitWarningCurrentObjects":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWarningBootstrap.CaptureCurrentEditorObjects,
                        "Approved cockpit 04 warning current object capture saved:");
                    break;
                case "EnsureApprovedCockpitDirection":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDirectionBootstrap.EnsureApprovedCockpitDirection,
                        "Approved cockpit 11 direction applied.");
                    break;
                case "ValidateApprovedCockpitDirection":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDirectionBootstrap.ValidateScene,
                        "Approved cockpit 11 direction validation passed.");
                    break;
                case "CaptureApprovedCockpitDirectionComparison":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDirectionBootstrap.CaptureUnityComparison,
                        "Approved cockpit 11 direction Unity comparison snapshots saved:");
                    break;
                case "CaptureApprovedCockpitDirectionCurrentObjects":
                    RunSynchronous(
                        request,
                        ApprovedCockpitDirectionBootstrap.CaptureCurrentEditorObjects,
                        "Approved cockpit 11 direction current object capture saved:");
                    break;
                case "EnsureApprovedCockpitLighting":
                    RunSynchronous(
                        request,
                        ApprovedCockpitLightingBootstrap.EnsureApprovedCockpitLighting,
                        "Approved cockpit 12 inspection lighting applied.");
                    break;
                case "ValidateApprovedCockpitLighting":
                    RunSynchronous(
                        request,
                        ApprovedCockpitLightingBootstrap.ValidateScene,
                        "Approved cockpit 12 inspection lighting validation passed.");
                    break;
                case "CaptureApprovedCockpitLightingComparison":
                    RunSynchronous(
                        request,
                        ApprovedCockpitLightingBootstrap.CaptureUnityComparison,
                        "Approved cockpit 12 inspection lighting Unity comparison snapshots saved:");
                    break;
                case "CaptureApprovedCockpitLightingCurrentObjects":
                    RunSynchronous(
                        request,
                        ApprovedCockpitLightingBootstrap.CaptureCurrentEditorObjects,
                        "Approved cockpit 12 lighting current object capture saved:");
                    break;
                case "CaptureApprovedCockpitCurrentStateRecoverySnapshot":
                    RunSynchronous(
                        request,
                        ApprovedCockpitCurrentStateRecoveryBootstrap.CaptureCurrentStateRecoverySnapshot,
                        "Approved cockpit current state recovery snapshot captured:");
                    break;
                case "ValidateApprovedCockpitCurrentStateRecoverySnapshot":
                    RunSynchronous(
                        request,
                        ApprovedCockpitCurrentStateRecoveryBootstrap.ValidateSnapshotAgainstCurrentScene,
                        "Approved cockpit current state recovery snapshot validation passed.");
                    break;
                case "RestoreApprovedCockpitCurrentState":
                    RunSynchronous(
                        request,
                        ApprovedCockpitCurrentStateRecoveryBootstrap.RestoreCurrentState,
                        "Approved cockpit current state restored from recovery snapshot:");
                    break;
                case "CaptureApprovedCockpitWarningBackupObjects":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWarningBootstrap.CaptureBackupEditorObjects,
                        "Approved cockpit 04 warning backup object capture saved.");
                    break;
                case "CaptureApprovedCockpitBackupDiff":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWarningBootstrap.CaptureApprovedCockpitBackupDiff,
                        "Approved cockpit backup diff saved:");
                    break;
                case "CaptureSceneBackupDiff":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWarningBootstrap.CaptureSceneBackupDiff,
                        "Scene backup diff saved:");
                    break;
                case "CaptureSceneTransformBackupDiff":
                    RunSynchronous(
                        request,
                        ApprovedCockpitWarningBootstrap.CaptureSceneTransformBackupDiff,
                        "Scene transform backup diff saved:");
                    break;
                case "DisableTutorialLogicForModeling":
                    RunSynchronous(
                        request,
                        ModelingInspectionModeBootstrap.DisableTutorialLogicForModeling,
                        "Tutorial logic disabled for modeling inspection.");
                    break;
                case "ValidateModelingInspectionMode":
                    RunSynchronous(
                        request,
                        ModelingInspectionModeBootstrap.ValidateScene,
                        "Modeling inspection mode validation passed.");
                    break;
                case "RestoreGameplayModeAfterModelingInspection":
                    RunSynchronous(
                        request,
                        ModelingInspectionModeBootstrap.RestoreGameplayModeAfterInspection,
                        "Gameplay mode restored after modeling inspection.");
                    break;
                case "EnableModelingInspectionFreeCamera":
                    RunSynchronous(
                        request,
                        ModelingInspectionModeBootstrap.EnableFreeCameraForModeling,
                        "Modeling inspection free camera enabled.");
                    break;
                case "ValidateModelingInspectionFreeCamera":
                    RunSynchronous(
                        request,
                        ModelingInspectionModeBootstrap.ValidateFreeCamera,
                        "Modeling inspection free camera validation passed.");
                    break;
                case "DisableCargoShipVisualModeling":
                    RunSynchronous(
                        request,
                        CargoShipVisualModelingBootstrap.DisableVisualModeling,
                        "Cargo ship visual modeling disabled.");
                    break;
                case "OpenCargoRunMvpScene":
                    RunSynchronous(
                        request,
                        OpenCargoRunMvpScene,
                        "CargoRunMvp scene opened.");
                    break;
                case "ClearUnityConsole":
                    RunSynchronous(
                        request,
                        ClearUnityConsole,
                        "Unity console cleared.");
                    break;
                case "InspectUnityConsoleErrors":
                    RunSynchronous(
                        request,
                        UnityConsoleDiagnostics.InspectCurrentErrors,
                        "Unity console errors inspected.");
                    break;
                case "AssertNoUnityConsoleErrors":
                    RunSynchronous(
                        request,
                        UnityConsoleDiagnostics.AssertNoErrors,
                        "Unity console contains no errors.");
                    break;
                default:
                    RunSynchronous(
                        request,
                        () => throw new InvalidOperationException($"Unknown bridge command: {request.Command}"),
                        string.Empty);
                    break;
            }
        }

        private static void RefreshAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException("Cannot refresh assets while Unity is entering or leaving Play Mode.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log("Unity assets refreshed from validation bridge.");
        }

        private static void ClearUnityConsole()
        {
            var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            var clearMethod = logEntriesType?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            if (clearMethod == null)
            {
                throw new InvalidOperationException("Unity console clear API could not be resolved.");
            }

            clearMethod.Invoke(null, null);
            Debug.Log("Unity console cleared.");
        }

        private static void OpenCargoRunMvpScene()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("CargoRunMvp scene asset was not found: " + Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException("Cannot open CargoRunMvp while Unity is entering or leaving Play Mode.");
            }

            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != Phase4CargoShipGrayboxBootstrap.CargoRunScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp did not become the active scene. ActiveScene=" + activeScene.path);
            }

            Selection.activeObject = sceneAsset;
            EditorGUIUtility.PingObject(sceneAsset);
            Debug.Log("CargoRunMvp scene opened from validation bridge.");
        }

        private static void RunSynchronous(BridgeRequest request, Action action, string successMarker)
        {
            BeginRequest(request);
            try
            {
                // RefreshAssets is the recovery path that imports a corrected script after a
                // previous compilation failure. Checking the stale failure flag before the
                // refresh would permanently prevent that corrected source from compiling.
            if (!string.Equals(request.Command, "RefreshAssets", StringComparison.Ordinal))
            {
                RequireScriptsCompiled();
            }
                action();
                CompleteRequest(successMarker);
            }
            catch (Exception exception)
            {
                FailRequest(exception);
            }
        }

        private static void RunTergoPierceAttackCurrentSceneVisualRun(BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Bellerophon.Editor.TergoCargoRunScene.TergoPierceAttackCurrentSceneVisualRun.Start(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunDolore04TentacleStabFullMotionCapture(BridgeRequest request, bool finalCapture)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabFullMotionPlayModeCapture.Start(
                    finalCapture,
                    successMarker => { TryDelete(ActiveRequestPath); CompleteRequest(request, successMarker); },
                    exception => { TryDelete(ActiveRequestPath); FailRequest(request, exception); });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunIspant08ContinuousMotionCapture(BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Bellerophon.Editor.IspantCargoRunScene.Ispant08ContinuousMotionPlayModeCapture.Start(
                    successMarker => { TryDelete(ActiveRequestPath); CompleteRequest(request, successMarker); },
                    exception => { TryDelete(ActiveRequestPath); FailRequest(request, exception); });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunAtaPistolTriggerFollowCapture(BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Bellerophon.Editor.AtaCargoRunScene.AtaPistolTriggerFollowPlayModeCapture.Start(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunAtaCommandStanceAlternationCapture(BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Bellerophon.Editor.AtaCargoRunScene.AtaCommandStanceAlternationPlayModeCapture.Start(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunStickAttackForwardAttackingCorrectionsPlayModeCapture(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                PlayerHandsObjectAnimationTools
                    .StickAttackForwardAttackingCorrectionsPlayModeCapture.Start(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunStickAttackForwardLeftPalmRightPlayModeCapture(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                PlayerHandsObjectAnimationTools
                    .StickAttackForwardAttackingCorrectionsPlayModeCapture
                    .StartLeftPalmRight(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunStickAttackForwardAttackingCorrectionsFinal(
            BridgeRequest request)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RunSynchronous(
                    request,
                    PlayerHandsObjectAnimationTools
                        .CaptureStickAttackForwardAttackingCorrectionsFinal,
                    "Stick_Attack_Forward attacking corrections finalized.");
                return;
            }

            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<PlayModeStateChange> handler = null;
                handler = state =>
                {
                    if (state != PlayModeStateChange.EnteredEditMode)
                    {
                        return;
                    }

                    EditorApplication.playModeStateChanged -= handler;
                    try
                    {
                        PlayerHandsObjectAnimationTools
                            .CaptureStickAttackForwardAttackingCorrectionsFinal();
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(
                            request,
                            "Stick_Attack_Forward attacking corrections finalized.");
                    }
                    catch (Exception exception)
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    }
                };
                EditorApplication.playModeStateChanged += handler;
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunStickAttackForwardGifWeaponPlayModeCapture(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                PlayerHandsObjectAnimationTools
                    .StickAttackForwardGifWeaponMotionPlayModeCapture.Start(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunMusketBackCarryPlayModeCapture(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                PlayerHandsObjectAnimationTools
                    .MusketBackCarryPlayModeCapture.Start(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunStickThrowReadyReleaseCancelPlayModeCapture(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                PlayerHandsObjectAnimationTools
                    .StickThrowReadyReleaseCancelPlayModeCapture.Start(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunDolore05ExecutionOpeningCapture(BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Bellerophon.Editor.Dolore05ExecutionOpening.Dolore05ExecutionOpeningPlayModeCapture.Start(
                    successMarker => { TryDelete(ActiveRequestPath); CompleteRequest(request, successMarker); },
                    exception => { TryDelete(ActiveRequestPath); FailRequest(request, exception); });
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunNegatifIdleEyeEmissionVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.NegatifCargoRunScene.NegatifIdleEyeEmissionPlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifIdleEyeEmissionPlayModeCapture.Resume(
                        completeCallback,
                        failCallback);
                }
                else
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifIdleEyeEmissionPlayModeCapture.Start(
                        completeCallback,
                        failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunRebellionMoveVisualReview(BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionMovePlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionMovePlayModeCapture.Resume(
                            completeCallback,
                            failCallback);
                }
                else
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionMovePlayModeCapture.Start(
                            completeCallback,
                            failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunRebellionFrontArtifactVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionFrontArtifactPlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionFrontArtifactPlayModeCapture.Resume(
                            completeCallback,
                            failCallback);
                }
                else
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionFrontArtifactPlayModeCapture.Start(
                            completeCallback,
                            failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunRebellionAttackTransitionVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionAttackModeTransitionPlayModeCapture
                    .HasPendingCapture)
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionAttackModeTransitionPlayModeCapture.Resume(
                            completeCallback,
                            failCallback);
                }
                else
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionAttackModeTransitionPlayModeCapture.Start(
                            completeCallback,
                            failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunRebellionForwardScanVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionForwardScanPlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionForwardScanPlayModeCapture.Resume(
                            completeCallback,
                            failCallback);
                }
                else
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionForwardScanPlayModeCapture.Start(
                            completeCallback,
                            failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunRebellionForwardBurstVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionForwardBurstFirePlayModeCapture
                    .HasPendingCapture)
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionForwardBurstFirePlayModeCapture.Resume(
                            completeCallback,
                            failCallback);
                }
                else
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionForwardBurstFirePlayModeCapture.Start(
                            completeCallback,
                            failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunRebellionHitReactionVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionHitReactionPlayModeCapture
                    .HasPendingCapture)
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionHitReactionPlayModeCapture.Resume(
                            completeCallback,
                            failCallback);
                }
                else
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionHitReactionPlayModeCapture.Start(
                            completeCallback,
                            failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunRebellionDeathVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionDeathPlayModeCapture
                    .HasPendingCapture)
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionDeathPlayModeCapture.Resume(
                            completeCallback,
                            failCallback);
                }
                else
                {
                    Bellerophon.Editor.RebellionCargoRunScene
                        .RebellionDeathPlayModeCapture.Start(
                            completeCallback,
                            failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunNegatifMoveVisualReview(BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.NegatifCargoRunScene.NegatifMovePlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifMovePlayModeCapture.Resume(
                        completeCallback,
                        failCallback);
                }
                else
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifMovePlayModeCapture.Start(
                        completeCallback,
                        failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunNegatifClawAttackVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.NegatifCargoRunScene.NegatifClawAttackPlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifClawAttackPlayModeCapture.Resume(
                        completeCallback,
                        failCallback);
                }
                else
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifClawAttackPlayModeCapture.Start(
                        completeCallback,
                        failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunNegatifHitReactionVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.NegatifCargoRunScene.NegatifHitReactionPlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifHitReactionPlayModeCapture.Resume(
                        completeCallback,
                        failCallback);
                }
                else
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifHitReactionPlayModeCapture.Start(
                        completeCallback,
                        failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunNegatifFleeVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.NegatifCargoRunScene.NegatifFleePlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifFleePlayModeCapture.Resume(
                        completeCallback,
                        failCallback);
                }
                else
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifFleePlayModeCapture.Start(
                        completeCallback,
                        failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunNegatifDeathVisualReview(
            BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                request.Write(ActiveRequestPath);
                Action<string> completeCallback =
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    };
                Action<Exception> failCallback =
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    };
                if (Bellerophon.Editor.NegatifCargoRunScene.NegatifDeathPlayModeCapture.HasPendingCapture)
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifDeathPlayModeCapture.Resume(
                        completeCallback,
                        failCallback);
                }
                else
                {
                    Bellerophon.Editor.NegatifCargoRunScene.NegatifDeathPlayModeCapture.Start(
                        completeCallback,
                        failCallback);
                }
            }
            catch (Exception exception)
            {
                TryDelete(ActiveRequestPath);
                FailRequest(request, exception);
            }
        }

        private static void RunConSpiritoReferenceRunVideoCapture(BridgeRequest request)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                Bellerophon.Editor.ConSpiritoCargoRunScene.ConSpiritoCargoRunSceneApplyAndReview.StartReferenceRunVideoCapture(
                    successMarker => CompleteRequest(request, successMarker),
                    exception => FailRequest(request, exception));
            }
            catch (Exception exception)
            {
                FailRequest(request, exception);
            }
        }

        private static void RunTests(BridgeRequest request, TestMode testMode)
        {
            BeginRequest(request);
            try
            {
                RequireScriptsCompiled();
                Directory.CreateDirectory(Path.GetDirectoryName(request.ResultsPath));
                if (testMode == TestMode.PlayMode)
                {
                    PreparePlayModeTestRunnerEnvironment();
                    request.StartUtcTicks = DateTime.UtcNow.Ticks;
                    TryDelete(DefaultTestResultsPath);
                    request.Write(ActiveRequestPath);
                }

                activeTestRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                activeTestRunCallbacks = TestRunCallbacks.Create(request);
                activeTestRunnerApi.RegisterCallbacks(activeTestRunCallbacks);
                activeTestRunGuid = activeTestRunnerApi.Execute(new ExecutionSettings(new Filter { testMode = testMode }));
            }
            catch (Exception exception)
            {
                ClearTestRunState(cancelActiveRun: true);
                FailRequest(exception);
            }
        }

        private static void PreparePlayModeTestRunnerEnvironment()
        {
            if (EditorSceneManager.playModeStartScene == null)
            {
                return;
            }

            activeLog.AppendLine(
                "Clearing Play Mode start scene for Unity Test Runner: " +
                AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = null;
        }

        private static void BeginRequest(BridgeRequest request)
        {
            isRunning = true;
            activeRequestStartTime = EditorApplication.timeSinceStartup;
            activeRequest = request;
            activeLog = new StringBuilder();
            Application.logMessageReceived += CaptureLog;
        }

        private static void CompleteRequest(string successMarker)
        {
            CompleteRequest(activeRequest, successMarker);
        }

        private static void CompleteRequest(BridgeRequest request, string successMarker)
        {
            WriteLog(request, false, null, successMarker);
            EndRequest();
        }

        private static void FailRequest(Exception exception)
        {
            FailRequest(activeRequest, exception);
        }

        private static void FailRequest(BridgeRequest request, Exception exception)
        {
            WriteLog(request, true, exception, string.Empty);
            EndRequest();
        }

        private static void EndRequest()
        {
            Application.logMessageReceived -= CaptureLog;
            activeRequest = null;
            activeLog = null;
            activeRequestStartTime = 0d;
            isRunning = false;
        }

        private static void CaptureLog(string condition, string stackTrace, LogType type)
        {
            if (activeLog == null)
            {
                return;
            }

            activeLog.AppendLine($"{type}: {condition}");
            if (type == LogType.Exception || type == LogType.Error)
            {
                activeLog.AppendLine(stackTrace);
            }
        }

        private static void RequireScriptsCompiled()
        {
            if (HasScriptCompilationFailed())
            {
                throw new InvalidOperationException("Scripts have compiler errors.");
            }
        }

        private static bool HasScriptCompilationFailed()
        {
            var property = typeof(EditorUtility).GetProperty(
                "scriptCompilationFailed",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            return property != null &&
                   property.PropertyType == typeof(bool) &&
                   (bool)property.GetValue(null);
        }

        private static void WriteLog(
            BridgeRequest request,
            bool failed,
            Exception exception,
            string successMarker)
        {
            var logPath = string.IsNullOrWhiteSpace(request.LogPath)
                ? DefaultLogPath($"{request.Command}.log")
                : request.LogPath;

            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            var builder = new StringBuilder();
            builder.AppendLine($"Unity editor bridge request completed: {request.Id}");
            builder.AppendLine($"Unity editor bridge command: {request.Command}");
            builder.AppendLine("Unity editor bridge mode: open editor");

            if (!string.IsNullOrWhiteSpace(successMarker))
            {
                builder.AppendLine(successMarker);
            }

            if (failed)
            {
                builder.AppendLine("Unity editor bridge failed.");
                builder.AppendLine(exception.ToString());
            }

            if (activeLog != null && activeLog.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Captured Unity log:");
                builder.Append(activeLog);
            }

            File.WriteAllText(logPath, builder.ToString());
        }

        private static void ClearTestRunState(TestRunCallbacks callbackToClear = null, bool cancelActiveRun = false)
        {
            if (cancelActiveRun && !string.IsNullOrWhiteSpace(activeTestRunGuid))
            {
                try
                {
                    TestRunnerApi.CancelTestRun(activeTestRunGuid);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            if (activeTestRunCallbacks != null)
            {
                TestRunnerApi.UnregisterTestCallback(activeTestRunCallbacks);
                UnityEngine.Object.DestroyImmediate(activeTestRunCallbacks);
            }

            if (activeTestRunnerApi != null)
            {
                UnityEngine.Object.DestroyImmediate(activeTestRunnerApi);
            }

            if (callbackToClear != null && !ReferenceEquals(callbackToClear, activeTestRunCallbacks))
            {
                TestRunnerApi.UnregisterTestCallback(callbackToClear);
                UnityEngine.Object.DestroyImmediate(callbackToClear);
            }

            activeTestRunCallbacks = null;
            activeTestRunnerApi = null;
            activeTestRunGuid = null;
            TryDelete(ActiveRequestPath);
        }

        private static bool TryCompleteRecoveredPlayModeRequest()
        {
            if (!File.Exists(ActiveRequestPath))
            {
                return false;
            }

            var request = BridgeRequest.Read(ActiveRequestPath);
            if (!request.IsValid)
            {
                TryDelete(ActiveRequestPath);
                return false;
            }

            if (request.Command == TergoPierceAttackCurrentSceneVisualRunCommand ||
                request.Command == Dolore04TentacleStabDiagnosticCommand ||
                request.Command == Dolore04TentacleStabFinalCommand ||
                request.Command == Ispant08ContinuousMotionCaptureCommand ||
                request.Command == AtaPistolTriggerFollowCaptureCommand ||
                request.Command == AtaCommandStanceAlternationCaptureCommand ||
                request.Command == StickAttackForwardAttackingCorrectionsPlayModeCommand ||
                request.Command == StickAttackForwardAttackingCorrectionsFinalCommand ||
                request.Command == StickAttackForwardLeftPalmRightPlayModeCommand ||
                request.Command == StickAttackForwardGifWeaponPlayModeCommand ||
                request.Command == StickThrowReadyReleaseCancelPlayModeCommand ||
                request.Command == StickThrowReleasePhysicsArcPlayModeCommand ||
                request.Command == Dolore05ExecutionOpeningDiagnosticCommand ||
                request.Command == RebellionMoveVisualReviewCommand ||
                request.Command == RebellionFrontArtifactVisualReviewCommand ||
                request.Command == RebellionAttackTransitionVisualReviewCommand ||
                request.Command == RebellionForwardScanVisualReviewCommand ||
                request.Command == RebellionForwardBurstVisualReviewCommand ||
                request.Command == RebellionHitReactionVisualReviewCommand ||
                request.Command == RebellionDeathVisualReviewCommand ||
                request.Command == NegatifIdleEyeEmissionVisualReviewCommand ||
                request.Command == NegatifMoveVisualReviewCommand ||
                request.Command == NegatifClawAttackVisualReviewCommand ||
                request.Command == NegatifHitReactionVisualReviewCommand ||
                request.Command == NegatifFleeVisualReviewCommand ||
                request.Command == NegatifDeathVisualReviewCommand)
            {
                return false;
            }

            if (request.Command != "PlayModeTests")
            {
                TryDelete(ActiveRequestPath);
                return false;
            }

            if (!File.Exists(DefaultTestResultsPath))
            {
                return false;
            }

            if (request.StartUtcTicks > 0 &&
                File.GetLastWriteTimeUtc(DefaultTestResultsPath).Ticks < request.StartUtcTicks)
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(request.ResultsPath));
                File.Copy(DefaultTestResultsPath, request.ResultsPath, true);
                WriteLog(request, false, null, "PlayModeTests completed.");
            }
            catch (Exception exception)
            {
                WriteLog(request, true, exception, string.Empty);
            }
            finally
            {
                if (activeRequest != null && activeRequest.Id == request.Id)
                {
                    ClearTestRunState();
                    EndRequest();
                }
                else
                {
                    TryDelete(ActiveRequestPath);
                }
            }

            return true;
        }

        private static bool TryResumeActivePlayModeRequest()
        {
            if (!File.Exists(ActiveRequestPath))
            {
                return false;
            }

            var request = BridgeRequest.Read(ActiveRequestPath);
            if (!request.IsValid)
            {
                TryDelete(ActiveRequestPath);
                return false;
            }

            if (request.Command == TergoPierceAttackCurrentSceneVisualRunCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine("Resuming Tergo current scene visual run after Play Mode transition.");
                Bellerophon.Editor.TergoCargoRunScene.TergoPierceAttackCurrentSceneVisualRun.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command == Dolore04TentacleStabDiagnosticCommand ||
                request.Command == Dolore04TentacleStabFinalCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine("Resuming Dolore motion 3 actual Animator capture after Play Mode transition.");
                Bellerophon.Editor.Dolore04TentacleStabAnimation.Dolore04TentacleStabFullMotionPlayModeCapture.Resume(
                    successMarker => { TryDelete(ActiveRequestPath); CompleteRequest(request, successMarker); },
                    exception => { TryDelete(ActiveRequestPath); FailRequest(request, exception); });
                return true;
            }

            if (request.Command == Ispant08ContinuousMotionCaptureCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Ispant slot 8 actual Play Mode two-loop motion capture after Play Mode transition.");
                Bellerophon.Editor.IspantCargoRunScene.Ispant08ContinuousMotionPlayModeCapture.Resume(
                    successMarker => { TryDelete(ActiveRequestPath); CompleteRequest(request, successMarker); },
                    exception => { TryDelete(ActiveRequestPath); FailRequest(request, exception); });
                return true;
            }

            if (request.Command == AtaPistolTriggerFollowCaptureCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Ata pistol actual Play Mode two-loop capture after Play Mode transition.");
                Bellerophon.Editor.AtaCargoRunScene.AtaPistolTriggerFollowPlayModeCapture.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command == AtaCommandStanceAlternationCaptureCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Ata command stance alternation actual Play Mode capture after Play Mode transition.");
                Bellerophon.Editor.AtaCargoRunScene.AtaCommandStanceAlternationPlayModeCapture.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command ==
                StickAttackForwardAttackingCorrectionsPlayModeCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Stick_Attack_Forward correction actual Play Mode Animator capture after Play Mode transition.");
                PlayerHandsObjectAnimationTools
                    .StickAttackForwardAttackingCorrectionsPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command ==
                StickAttackForwardAttackingCorrectionsFinalCommand)
            {
                RunStickAttackForwardAttackingCorrectionsFinal(request);
                return true;
            }

            if (request.Command ==
                StickAttackForwardLeftPalmRightPlayModeCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Stick_Attack_Forward left-palm-right actual Play Mode capture after Play Mode transition.");
                PlayerHandsObjectAnimationTools
                    .StickAttackForwardAttackingCorrectionsPlayModeCapture
                    .ResumeLeftPalmRight(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == StickAttackForwardGifWeaponPlayModeCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Stick_Attack_Forward GIF weapon actual Play Mode capture after Play Mode transition.");
                PlayerHandsObjectAnimationTools
                    .StickAttackForwardGifWeaponMotionPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == MusketBackCarryPlayModeCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Musket back-carry actual Play Mode capture after Play Mode transition.");
                PlayerHandsObjectAnimationTools
                    .MusketBackCarryPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == StickThrowReadyReleaseCancelPlayModeCommand ||
                request.Command == StickThrowReleasePhysicsArcPlayModeCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Stick Throw actual Play Mode capture after Play Mode transition.");
                PlayerHandsObjectAnimationTools
                    .StickThrowReadyReleaseCancelPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == Dolore05ExecutionOpeningDiagnosticCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine("Resuming Dolore motion 4 actual Animator execution opening capture after Play Mode transition.");
                Bellerophon.Editor.Dolore05ExecutionOpening.Dolore05ExecutionOpeningPlayModeCapture.Resume(
                    successMarker => { TryDelete(ActiveRequestPath); CompleteRequest(request, successMarker); },
                    exception => { TryDelete(ActiveRequestPath); FailRequest(request, exception); });
                return true;
            }

            if (request.Command == NegatifIdleEyeEmissionVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Negatif idle eye emission actual Play Mode capture after Play Mode transition.");
                Bellerophon.Editor.NegatifCargoRunScene.NegatifIdleEyeEmissionPlayModeCapture.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command == RebellionMoveVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Rebellion actual Play Mode move capture after " +
                    "Play Mode transition.");
                Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionMovePlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == RebellionFrontArtifactVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Rebellion front artifact actual Play Mode " +
                    "capture after Play Mode transition.");
                Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionFrontArtifactPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command ==
                RebellionAttackTransitionVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Rebellion attack transition actual Play Mode " +
                    "capture after Play Mode transition.");
                Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionAttackModeTransitionPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == RebellionForwardScanVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Rebellion forward scan actual Play Mode " +
                    "capture after Play Mode transition.");
                Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionForwardScanPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == RebellionForwardBurstVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Rebellion forward burst actual Play Mode " +
                    "capture after Play Mode transition.");
                Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionForwardBurstFirePlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == RebellionHitReactionVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Rebellion hit reaction actual Play Mode " +
                    "capture after Play Mode transition.");
                Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionHitReactionPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == RebellionDeathVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Rebellion death actual Play Mode capture after " +
                    "Play Mode transition.");
                Bellerophon.Editor.RebellionCargoRunScene
                    .RebellionDeathPlayModeCapture.Resume(
                        successMarker =>
                        {
                            TryDelete(ActiveRequestPath);
                            CompleteRequest(request, successMarker);
                        },
                        exception =>
                        {
                            TryDelete(ActiveRequestPath);
                            FailRequest(request, exception);
                        });
                return true;
            }

            if (request.Command == NegatifMoveVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Negatif actual Play Mode move capture after Play Mode transition.");
                Bellerophon.Editor.NegatifCargoRunScene.NegatifMovePlayModeCapture.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command == NegatifClawAttackVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Negatif actual Play Mode claw attack capture after Play Mode transition.");
                Bellerophon.Editor.NegatifCargoRunScene.NegatifClawAttackPlayModeCapture.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command == NegatifHitReactionVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Negatif actual Play Mode hit reaction capture after Play Mode transition.");
                Bellerophon.Editor.NegatifCargoRunScene.NegatifHitReactionPlayModeCapture.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command == NegatifFleeVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Negatif actual Play Mode flee capture after Play Mode transition.");
                Bellerophon.Editor.NegatifCargoRunScene.NegatifFleePlayModeCapture.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command == NegatifDeathVisualReviewCommand)
            {
                BeginRequest(request);
                activeLog.AppendLine(
                    "Resuming Negatif actual Play Mode death capture after Play Mode transition.");
                Bellerophon.Editor.NegatifCargoRunScene.NegatifDeathPlayModeCapture.Resume(
                    successMarker =>
                    {
                        TryDelete(ActiveRequestPath);
                        CompleteRequest(request, successMarker);
                    },
                    exception =>
                    {
                        TryDelete(ActiveRequestPath);
                        FailRequest(request, exception);
                    });
                return true;
            }

            if (request.Command != "PlayModeTests")
            {
                TryDelete(ActiveRequestPath);
                return false;
            }

            if (activeTestRunCallbacks != null)
            {
                return true;
            }

            BeginRequest(request);
            activeLog.AppendLine("Resuming PlayModeTests callbacks after domain reload.");
            activeTestRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeTestRunCallbacks = TestRunCallbacks.Create(request);
            activeTestRunnerApi.RegisterCallbacks(activeTestRunCallbacks);
            return true;
        }

        private static bool TryFailStaleTestRunnerRequest()
        {
            if (activeRequest == null)
            {
                EndRequest();
                return true;
            }

            if (activeRequest.Command != "EditModeTests" && activeRequest.Command != "PlayModeTests")
            {
                return false;
            }

            if (EditorApplication.timeSinceStartup - activeRequestStartTime < TestRunnerStaleTimeoutSeconds)
            {
                return false;
            }

            var request = activeRequest;
            ClearTestRunState(cancelActiveRun: true);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            FailRequest(
                request,
                new TimeoutException(
                    request.Command +
                    " did not return a Unity Test Runner completion callback within " +
                    TestRunnerStaleTimeoutSeconds.ToString("0") +
                    " seconds."));
            return true;
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string DefaultLogPath(string fileName)
        {
            return Path.Combine(ProjectRoot, "Logs", fileName);
        }

        private static string ActiveRequestPath =>
            Path.Combine(ProjectRoot, "Logs", ActiveRequestFileName);

        private static string DefaultTestResultsPath =>
            Path.Combine(Application.persistentDataPath, DefaultTestResultsFileName);

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private sealed class TestRunCallbacks : ScriptableObject, IErrorCallbacks
        {
            [SerializeField]
            private string id;
            [SerializeField]
            private string command;
            [SerializeField]
            private string logPath;
            [SerializeField]
            private string resultsPath;
            [SerializeField]
            private string outputPath;
            [SerializeField]
            private bool developmentBuild;
            [SerializeField]
            private long startUtcTicks;

            public static TestRunCallbacks Create(BridgeRequest request)
            {
                var callbacks = CreateInstance<TestRunCallbacks>();
                callbacks.Initialize(request);
                return callbacks;
            }

            private void Initialize(BridgeRequest request)
            {
                id = request.Id;
                command = request.Command;
                logPath = request.LogPath;
                resultsPath = request.ResultsPath;
                outputPath = request.OutputPath;
                developmentBuild = request.DevelopmentBuild;
                startUtcTicks = request.StartUtcTicks;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"Running {command} through open editor bridge.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var request = ToRequest();
                try
                {
                    TestRunnerApi.SaveResultToFile(result, request.ResultsPath);
                    TryDelete(ActiveRequestPath);
                    CompleteRequest(request, $"{request.Command} completed.");
                }
                catch (Exception exception)
                {
                    FailRequest(request, exception);
                }
                finally
                {
                    ClearTestRunState(this);
                }
            }

            public void OnError(string message)
            {
                var request = ToRequest();
                try
                {
                    FailRequest(request, new InvalidOperationException(message));
                }
                finally
                {
                    ClearTestRunState(this);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogError($"Failed {result.FullName}: {result.Message}");
                }
            }

            private BridgeRequest ToRequest()
            {
                return BridgeRequest.Create(
                    id,
                    command,
                    logPath,
                    resultsPath,
                    outputPath,
                    developmentBuild,
                    startUtcTicks);
            }
        }

        private sealed class BridgeRequest
        {
            public string Id { get; private set; }
            public string Command { get; private set; }
            public string LogPath { get; private set; }
            public string ResultsPath { get; private set; }
            public string OutputPath { get; private set; }
            public bool DevelopmentBuild { get; private set; }
            public long StartUtcTicks { get; set; }

            public bool IsValid =>
                !string.IsNullOrWhiteSpace(Id) &&
                !string.IsNullOrWhiteSpace(Command);

            public static BridgeRequest Manual(string command, string logPath)
            {
                return new BridgeRequest
                {
                    Id = "manual",
                    Command = command,
                    LogPath = logPath
                };
            }

            public static BridgeRequest Create(
                string id,
                string command,
                string logPath,
                string resultsPath,
                string outputPath,
                bool developmentBuild,
                long startUtcTicks)
            {
                return new BridgeRequest
                {
                    Id = id,
                    Command = command,
                    LogPath = logPath,
                    ResultsPath = resultsPath,
                    OutputPath = outputPath,
                    DevelopmentBuild = developmentBuild,
                    StartUtcTicks = startUtcTicks
                };
            }

            public static BridgeRequest Read(string path)
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = rawLine.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex < 0)
                    {
                        continue;
                    }

                    values[line.Substring(0, separatorIndex)] = line.Substring(separatorIndex + 1);
                }

                return new BridgeRequest
                {
                    Id = Get(values, "id"),
                    Command = Get(values, "command"),
                    LogPath = Get(values, "logPath"),
                    ResultsPath = Get(values, "resultsPath"),
                    OutputPath = Get(values, "outputPath"),
                    DevelopmentBuild = bool.TryParse(Get(values, "developmentBuild"), out var developmentBuild) &&
                                       developmentBuild,
                    StartUtcTicks = long.TryParse(Get(values, "startUtcTicks"), out var startUtcTicks)
                        ? startUtcTicks
                        : 0L
                };
            }

            public void Write(string path)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllLines(
                    path,
                    new[]
                    {
                        $"id={Id}",
                        $"command={Command}",
                        $"logPath={LogPath}",
                        $"resultsPath={ResultsPath}",
                        $"outputPath={OutputPath}",
                        $"developmentBuild={DevelopmentBuild}",
                        $"startUtcTicks={StartUtcTicks}"
                    });
            }

            private static string Get(IDictionary<string, string> values, string key)
            {
                return values.TryGetValue(key, out var value) ? value : string.Empty;
            }
        }
    }
}
