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
                request.Command != TergoPierceAttackCurrentSceneVisualRunCommand)
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
                case "InspectApprovedOstinatoMaterialTarget":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoApprovedMaterial.OstinatoApprovedMaterialApplyAndReview.InspectApprovedOstinatoMaterialTarget,
                        "Approved Ostinato material target, UV layout, and nine scene slots inspected without scene changes.");
                    break;
                case "ApplyApprovedOstinatoMaterialSample":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.OstinatoApprovedMaterial.OstinatoApprovedMaterialApplyAndReview.ApplyApprovedOstinatoMaterialSample,
                        "Approved Ostinato front, side, and back sample images projected directly and applied to nine scene slots.");
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

            if (request.Command == TergoPierceAttackCurrentSceneVisualRunCommand)
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
