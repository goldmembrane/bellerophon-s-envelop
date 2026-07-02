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
            if (request.Command != "PlayModeTests")
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
                case "CaptureLongaArmaUnityVisualComparison":
                    RunSynchronous(
                        request,
                        Bellerophon.Editor.LongaArmaCargoRunScene.LongaArmaCargoRunSceneApplyAndReview.CaptureUnityVisualComparison,
                        "Approved Longa Arma Unity visual comparison captured.");
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
            if (!request.IsValid || request.Command != "PlayModeTests")
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
            if (!request.IsValid || request.Command != "PlayModeTests")
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
