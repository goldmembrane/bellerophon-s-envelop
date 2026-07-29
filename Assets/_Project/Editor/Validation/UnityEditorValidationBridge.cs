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
                RequireScriptsCompiled();
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
