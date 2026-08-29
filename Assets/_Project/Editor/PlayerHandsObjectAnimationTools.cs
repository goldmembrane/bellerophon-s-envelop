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
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PlayerHandsObjectAnimationTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LayoutRootName = "PlayerAnimationLayout";
        private const string IdleReferenceTargetName = "Player_Idle";
        private const string EmptyTargetName = "Hands_Empty_Idle";
        private const string OneHandTargetName = "Hands_Carry_OneHand";
        private const string TwoHandTargetName = "Hands_Carry_TwoHand";
        private const string DrawBackTargetName = "Hands_Draw_Back";
        private const string StowBackTargetName = "Hands_Stow_Back";
        private const string ThrowReadyTargetName = "Hands_Throw_Ready";
        private const string ThrowReleaseTargetName = "Hands_Throw_Release";
        private const string ThrowCancelTargetName = "Hands_Throw_Cancel";
        private const string EmptyStateName = "HandsEmptyIdle";
        private const string OneHandStateName = "HandsCarryOneHand";
        private const string TwoHandStateName = "HandsCarryTwoHand";
        private const string DrawBackStateName = "HandsDrawBack";
        private const string StowBackStateName = "HandsStowBack";
        private const string ThrowReadyStateName = "HandsThrowReady";
        private const string ThrowReleaseStateName = "HandsThrowRelease";
        private const string ThrowCancelStateName = "HandsThrowCancel";
        private const float PositionTolerance = 0.0001f;
        private const float RotationTolerance = 0.01f;
        private const int CaptureWidth = 400;
        private const int CaptureHeight = 500;
        private const string IdleSourceHash =
            "F835EA47600940846039E4BD323D3D6FEC6B05C676E9DEBB9293344B839AA853";
        private const string OneHandSourceHash =
            "90DFFCC45C58B5BF0876F6A3409026D9E6BC6BF5268068D12516803FBADC6E78";
        private const string TwoHandSourceHash =
            "2CB9CA6EBE34C22770FB2BB23FE026CDCA47EF481324D8D6097CA23E6048D752";
        private const string DrawBackSourceHash =
            "A4AD3D660627A34D47A38811E688C2B19416C57146580963279459F6D4EC396B";
        private const string StowBackSourceHash =
            "ECAA2FCE857BD9E5275ECDDDFFA220F26C3AA802354A36C9980B51D1026A01D9";
        private const string ThrowSourceHash =
            "AF4F841C549ABFD62D5FC0E349CC744BD6A0837E578E9078F515F8A55DDB7BF5";

        private const string IdleClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Idle.anim";
        private const string EmptyClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Empty_Idle.anim";
        private const string EmptyControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Empty_Idle.controller";
        private const string OneHandOriginalPath =
            "player model/transfer 1hand idle.fbx";
        private const string OneHandSourcePath =
            "Assets/_Project/Art/Player/Animations/Hands_Carry_OneHand_Mixamo.fbx";
        private const string OneHandControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Carry_OneHand.controller";
        private const string TwoHandOriginalPath =
            "player model/transfer 2hand Idle.fbx";
        private const string TwoHandSourcePath =
            "Assets/_Project/Art/Player/Animations/Hands_Carry_TwoHand_Mixamo.fbx";
        private const string TwoHandControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Carry_TwoHand.controller";
        private const string DrawBackOriginalPath =
            "player model/transfer grab from behind.fbx";
        private const string DrawBackSourcePath =
            "Assets/_Project/Art/Player/Animations/Hands_Draw_Back_Mixamo.fbx";
        private const string DrawBackControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Draw_Back.controller";
        private const string DrawBackForwardAdjustedClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Draw_Back_ForwardAdjusted.anim";
        private const string StowBackOriginalPath =
            "player model/transfer put back.fbx";
        private const string StowBackSourcePath =
            "Assets/_Project/Art/Player/Animations/Hands_Stow_Back_Mixamo.fbx";
        private const string StowBackControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Stow_Back.controller";
        private const string ThrowOriginalPath =
            "player model/transfer throwing.fbx";
        private const string ThrowSourcePath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Mixamo.fbx";
        private const string PlayerModelPath =
            "Assets/_Project/Art/Player/player.fbx";
        private const string ThrowReadyBaseClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready_MixamoHeadHeightHold.anim";
        private const string ThrowReadyPeakClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready_MixamoPeakHold.anim";
        private const string ThrowReadyClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready_MixamoHeadHeightBreathing.anim";
        private const string ThrowReadyBreathingMeshPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready_Breathing.asset";
        private const string ThrowReadyBreathingBlendShapeName = "Breathing";
        private const string ThrowReadyControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready.controller";
        private const string ThrowReleaseControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Release.controller";
        private const string ThrowCancelClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Cancel_MixamoReverse.anim";
        private const string ThrowCancelControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Cancel.controller";
        private const string ThrowSourceValidationDirectory =
            "docs/validation/player_hands_throw_mixamo_2026-08-29";
        private const string ThrowValidationDirectory =
            "docs/validation/player_hands_throw_ready_breathing_2026-08-29";
        private const string ThrowSourceDiagnosticMetricsPath =
            ThrowSourceValidationDirectory + "/source_diagnostic_metrics.json";
        private const string ThrowSourceDiagnosticPath =
            ThrowSourceValidationDirectory + "/source_all_frames_front_side.png";
        private const string ThrowApplyMetricsPath =
            ThrowValidationDirectory + "/apply_metrics.json";
        private const string ThrowReviewMetricsPath =
            ThrowValidationDirectory + "/review_metrics.json";
        private const string ThrowReviewPath =
            ThrowValidationDirectory + "/direct_review_contact_sheet.png";
        private const string ThrowFinalPath =
            ThrowValidationDirectory + "/final.png";
        private const string ThrowReviewStageKey =
            "Bellerophon.PlayerHandsThrowReadyBreathing.Review.Stage";
        private const string ThrowCancelValidationDirectory =
            "docs/validation/player_hands_throw_cancel_2026-08-29";
        private const string ThrowCancelApplyMetricsPath =
            ThrowCancelValidationDirectory + "/apply_metrics.json";
        private const string ThrowCancelReviewMetricsPath =
            ThrowCancelValidationDirectory + "/review_metrics.json";
        private const string ThrowCancelReviewPath =
            ThrowCancelValidationDirectory + "/direct_review_contact_sheet.png";
        private const string ThrowCancelFinalPath =
            ThrowCancelValidationDirectory + "/final.png";
        private const string ThrowCancelReviewStageKey =
            "Bellerophon.PlayerHandsThrowCancel.Review.Stage";

        private const string ValidationDirectory =
            "docs/validation/player_hands_objects_2026-08-28";
        private const string ApplyMetricsPath =
            ValidationDirectory + "/player_hands_objects_apply_metrics.json";
        private const string ReviewMetricsPath =
            ValidationDirectory + "/player_hands_objects_review_metrics.json";
        private const string EmptyReviewPath =
            ValidationDirectory + "/hands_empty_idle_review_contact_sheet.png";
        private const string OneHandReviewPath =
            ValidationDirectory + "/hands_carry_onehand_review_contact_sheet.png";
        private const string TwoHandReviewPath =
            ValidationDirectory + "/hands_carry_twohand_review_contact_sheet.png";
        private const string EmptyFinalPath =
            ValidationDirectory + "/hands_empty_idle_final.png";
        private const string OneHandFinalPath =
            ValidationDirectory + "/hands_carry_onehand_final.png";
        private const string TwoHandFinalPath =
            ValidationDirectory + "/hands_carry_twohand_final.png";
        private const string ReviewStageKey =
            "Bellerophon.PlayerHandsObjects.Review.Stage";
        private const string OneHandEmbeddedTakeApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_embedded_take_apply_metrics.json";
        private const string OneHandEmbeddedTakeReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_embedded_take_review_metrics.json";
        private const string OneHandEmbeddedTakeReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_embedded_take_review_contact_sheet.png";
        private const string OneHandEmbeddedTakeFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_embedded_take_final.png";
        private const string OneHandEmbeddedTakeReviewStageKey =
            "Bellerophon.PlayerHandsCarryOneHandEmbeddedTake.Review.Stage";
        private const string OneHandEmptyBodyPalmLeftApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_empty_body_palm_left_apply_metrics.json";
        private const string OneHandEmptyBodyPalmLeftReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_empty_body_palm_left_review_metrics.json";
        private const string OneHandEmptyBodyPalmLeftBeforePath =
            ValidationDirectory + "/player_hands_carry_onehand_empty_body_palm_left_before.png";
        private const string OneHandEmptyBodyPalmLeftReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_empty_body_palm_left_review_contact_sheet.png";
        private const string OneHandEmptyBodyPalmLeftReviewCloseFrontPath =
            ValidationDirectory + "/player_hands_carry_onehand_empty_body_palm_left_review_close_front.png";
        private const string OneHandEmptyBodyPalmLeftReviewCloseSidePath =
            ValidationDirectory + "/player_hands_carry_onehand_empty_body_palm_left_review_close_side.png";
        private const string OneHandEmptyBodyPalmLeftReviewPalmPath =
            ValidationDirectory + "/player_hands_carry_onehand_empty_body_palm_left_review_palm_from_character_left.png";
        private const string OneHandEmptyBodyPalmLeftFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_empty_body_palm_left_final.png";
        private const string OneHandEmptyBodyPalmLeftReviewStageKey =
            "Bellerophon.PlayerHandsCarryOneHandEmptyBodyPalmLeft.Review.Stage";
        private const string HandsBackValidationDirectory =
            "docs/validation/player_hands_back_2026-08-28";
        private const string HandsBackApplyMetricsPath =
            HandsBackValidationDirectory + "/player_hands_back_apply_metrics.json";
        private const string HandsBackReviewMetricsPath =
            HandsBackValidationDirectory + "/player_hands_back_review_metrics.json";
        private const string DrawBackReviewPath =
            HandsBackValidationDirectory + "/hands_draw_back_review_contact_sheet.png";
        private const string StowBackReviewPath =
            HandsBackValidationDirectory + "/hands_stow_back_review_contact_sheet.png";
        private const string DrawBackFinalPath =
            HandsBackValidationDirectory + "/hands_draw_back_final.png";
        private const string StowBackFinalPath =
            HandsBackValidationDirectory + "/hands_stow_back_final.png";
        private const string HandsBackReviewStageKey =
            "Bellerophon.PlayerHandsBack.Review.Stage";
        private const string DrawBackExactReconnectValidationDirectory =
            "docs/validation/player_hands_draw_back_exact_mixamo_reconnect_2026-08-29";
        private const string DrawBackExactReconnectApplyMetricsPath =
            DrawBackExactReconnectValidationDirectory + "/apply_metrics.json";
        private const string DrawBackExactReconnectReviewMetricsPath =
            DrawBackExactReconnectValidationDirectory + "/review_metrics.json";
        private const string DrawBackExactReconnectReviewPath =
            DrawBackExactReconnectValidationDirectory + "/direct_review_contact_sheet.png";
        private const string DrawBackExactReconnectFinalPath =
            DrawBackExactReconnectValidationDirectory + "/final.png";
        private const string DrawBackExactReconnectReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackExactReconnect.Review.Stage";
        // These paths isolate the approved common-mesh gate and forward-draw review
        // from the legacy state-only chest-correction validation artifacts.
        private const string DrawBackCommonMeshForwardValidationDirectory =
            "docs/validation/player_hands_draw_back_common_mesh_forward_2026-08-29";
        private const string DrawBackFaceClearanceValidationDirectory =
            "docs/validation/player_hands_draw_back_face_clearance_2026-08-29";
        private const string DrawBackCommonMeshApplyMetricsPath =
            DrawBackCommonMeshForwardValidationDirectory + "/common_mesh_apply_metrics.json";
        private const string DrawBackCommonMeshReviewMetricsPath =
            DrawBackCommonMeshForwardValidationDirectory + "/common_mesh_review_metrics.json";
        private const string DrawBackCommonMeshReviewPath =
            DrawBackCommonMeshForwardValidationDirectory + "/common_mesh_direct_review_contact_sheet.png";
        private const string DrawBackCommonMeshForwardApplyMetricsPath =
            DrawBackFaceClearanceValidationDirectory + "/forward_apply_metrics.json";
        private const string DrawBackCommonMeshForwardReviewMetricsPath =
            DrawBackFaceClearanceValidationDirectory + "/forward_review_metrics.json";
        private const string DrawBackCommonMeshForwardReviewPath =
            DrawBackFaceClearanceValidationDirectory + "/forward_direct_review_contact_sheet.png";
        private const string DrawBackCommonMeshForwardFinalPath =
            DrawBackFaceClearanceValidationDirectory + "/final.png";
        private const string DrawBackCommonMeshReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackCommonMesh.Review.Stage";
        private const string DrawBackCommonMeshForwardReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackCommonMeshForward.Review.Stage";
        private const string DrawBackForwardValidationDirectory =
            "docs/validation/player_hands_draw_back_forward_angle_2026-08-28";
        private const string DrawBackForwardApplyMetricsPath =
            DrawBackForwardValidationDirectory + "/player_hands_draw_back_forward_angle_apply_metrics.json";
        private const string DrawBackForwardReviewMetricsPath =
            DrawBackForwardValidationDirectory + "/player_hands_draw_back_forward_angle_review_metrics.json";
        private const string DrawBackForwardReviewPath =
            DrawBackForwardValidationDirectory + "/hands_draw_back_forward_angle_review_contact_sheet.png";
        private const string DrawBackForwardFinalPath =
            DrawBackForwardValidationDirectory + "/hands_draw_back_forward_angle_final.png";
        private const string DrawBackForwardReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackForwardAngle.Review.Stage";
        private const string DrawBackLowPalmLeftValidationDirectory =
            "docs/validation/player_hands_draw_back_low_palm_left_2026-08-28";
        private const string DrawBackLowPalmLeftApplyMetricsPath =
            DrawBackLowPalmLeftValidationDirectory + "/player_hands_draw_back_low_palm_left_apply_metrics.json";
        private const string DrawBackLowPalmLeftReviewMetricsPath =
            DrawBackLowPalmLeftValidationDirectory + "/player_hands_draw_back_low_palm_left_review_metrics.json";
        private const string DrawBackLowPalmLeftReviewPath =
            DrawBackLowPalmLeftValidationDirectory + "/hands_draw_back_low_palm_left_review_contact_sheet.png";
        private const string DrawBackLowPalmLeftFinalPath =
            DrawBackLowPalmLeftValidationDirectory + "/hands_draw_back_low_palm_left_final.png";
        private const string DrawBackLowPalmLeftReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackLowPalmLeft.Review.Stage";
        private const string DrawBackOuterElbowValidationDirectory =
            "docs/validation/player_hands_draw_back_outer_elbow_2026-08-28";
        private const string DrawBackOuterElbowApplyMetricsPath =
            DrawBackOuterElbowValidationDirectory + "/player_hands_draw_back_outer_elbow_apply_metrics.json";
        private const string DrawBackOuterElbowReviewMetricsPath =
            DrawBackOuterElbowValidationDirectory + "/player_hands_draw_back_outer_elbow_review_metrics.json";
        private const string DrawBackOuterElbowReviewPath =
            DrawBackOuterElbowValidationDirectory + "/hands_draw_back_outer_elbow_review_contact_sheet.png";
        private const string DrawBackOuterElbowFinalPath =
            DrawBackOuterElbowValidationDirectory + "/hands_draw_back_outer_elbow_final.png";
        private const string DrawBackOuterElbowReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackOuterElbow.Review.Stage";
        private const string TransporterPurpleFlagValidationDirectory =
            "docs/validation/player_transporter_purple_flag_draw_back_clearance_start_2026-08-28";
        private const string TransporterPurpleFlagApplyMetricsPath =
            TransporterPurpleFlagValidationDirectory + "/apply_metrics.json";
        private const string TransporterPurpleFlagReviewMetricsPath =
            TransporterPurpleFlagValidationDirectory + "/review_metrics.json";
        private const string TransporterPurpleFlagReviewPath =
            TransporterPurpleFlagValidationDirectory + "/direct_review_contact_sheet.png";
        private const string TransporterPurpleFlagFinalPath =
            TransporterPurpleFlagValidationDirectory + "/final.png";
        private const string TransporterTextureBaselinePath =
            TransporterPurpleFlagValidationDirectory + "/texture_0_before.png";
        private const string TransporterTexturePath =
            "Assets/_Project/Art/Player/Textures/texture_0.png";
        private const string TransporterTextureDuplicatePath =
            "Assets/_Project/Art/Player/player.fbm/texture_0.png";
        private const string TransporterPurpleFlagReviewStageKey =
            "Bellerophon.PlayerTransporterPurpleFlagDrawBackClearanceStart.Review.Stage";
        private const string DrawBackFrontSilhouetteValidationDirectory =
            "docs/validation/player_hands_draw_back_front_silhouette_clearance_2026-08-28";
        private const string DrawBackFrontSilhouetteApplyMetricsPath =
            DrawBackFrontSilhouetteValidationDirectory + "/apply_metrics.json";
        private const string DrawBackFrontSilhouetteReviewMetricsPath =
            DrawBackFrontSilhouetteValidationDirectory + "/review_metrics.json";
        private const string DrawBackFrontSilhouetteReviewPath =
            DrawBackFrontSilhouetteValidationDirectory + "/direct_review_contact_sheet.png";
        private const string DrawBackFrontSilhouetteFinalPath =
            DrawBackFrontSilhouetteValidationDirectory + "/final.png";
        private const string DrawBackFrontSilhouetteReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackFrontSilhouetteClearance.Review.Stage";
        private const string DrawBackChestDeformationValidationDirectory =
            "docs/validation/player_hands_draw_back_chest_deformation_fix_2026-08-28";
        private const string DrawBackChestDeformationApplyMetricsPath =
            DrawBackChestDeformationValidationDirectory + "/apply_metrics.json";
        private const string DrawBackChestDeformationReviewMetricsPath =
            DrawBackChestDeformationValidationDirectory + "/review_metrics.json";
        private const string DrawBackChestDeformationReviewPath =
            DrawBackChestDeformationValidationDirectory + "/direct_review_contact_sheet.png";
        private const string DrawBackChestDeformationFinalPath =
            DrawBackChestDeformationValidationDirectory + "/final.png";
        private const string DrawBackChestDeformationReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackChestDeformationFix.Review.Stage";
        private const float DrawBackChestSafeOutwardDegrees = 28f;
        private const string DrawBackRightChestCorrectionValidationDirectory =
            "docs/validation/player_hands_draw_back_right_chest_video_followup_2026-08-28";
        private const string DrawBackRightChestDiagnosticMetricsPath =
            DrawBackRightChestCorrectionValidationDirectory + "/diagnostic_metrics.json";
        private const string DrawBackRightChestDiagnosticPath =
            DrawBackRightChestCorrectionValidationDirectory + "/diagnostic_contact_sheet.png";
        private const string DrawBackRightChestApplyMetricsPath =
            DrawBackRightChestCorrectionValidationDirectory + "/apply_metrics.json";
        private const string DrawBackRightChestReviewMetricsPath =
            DrawBackRightChestCorrectionValidationDirectory + "/review_metrics.json";
        private const string DrawBackRightChestReviewPath =
            DrawBackRightChestCorrectionValidationDirectory + "/direct_review_contact_sheet.png";
        private const string DrawBackRightChestAllSourceFramesPath =
            DrawBackRightChestCorrectionValidationDirectory + "/all_source_frames.png";
        private const string DrawBackRightChestAllAdjustedBeforeFramesPath =
            DrawBackRightChestCorrectionValidationDirectory + "/all_adjusted_before_frames.png";
        private const string DrawBackRightChestVideoPoseStressPath =
            DrawBackRightChestCorrectionValidationDirectory + "/video_pose_stress_comparison.png";
        private const string DrawBackRightChestFinalPath =
            DrawBackRightChestCorrectionValidationDirectory + "/final.png";
        private const string DrawBackRightChestReviewStageKey =
            "Bellerophon.PlayerHandsDrawBackRightChestCorrection.Review.Stage";
        private const string DrawBackRightChestCorrectedMeshPath =
            "Assets/_Project/Art/Player/Generated/Hands_Draw_Back_ChestCorrected.asset";
        private const string DrawBackRightChestBlendShapePrefix =
            "HandsDrawBackRightChestPhase";
        private const string DrawBackRightChestBlendShapeName =
            DrawBackRightChestBlendShapePrefix + "00";
        private const string DrawBackRightChestResidualBlendShapeName =
            DrawBackRightChestBlendShapePrefix + "01";
        private const string DrawBackRightChestLegacyBlendShapeName =
            "HandsDrawBackRightChestCorrective";
        private const string DrawBackRightChestLegacyResidualBlendShapeName =
            "HandsDrawBackRightChestCorrectiveResidual";
        private const int DrawBackRightChestPhaseStride = 3;
        private const string ArmsMaskPath =
            "Assets/_Project/Art/Player/Animations/Hands_Carry_Arms.mask";
        private const string AlignmentBaseStateName = "HandsEmptyIdleBase";
        private const string AlignmentApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_body_alignment_apply_metrics.json";
        private const string AlignmentReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_body_alignment_review_metrics.json";
        private const string OneHandAlignmentReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_body_alignment_review_contact_sheet.png";
        private const string TwoHandAlignmentReviewPath =
            ValidationDirectory + "/player_hands_carry_twohand_body_alignment_review_contact_sheet.png";
        private const string OneHandAlignmentFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_body_alignment_final.png";
        private const string TwoHandAlignmentFinalPath =
            ValidationDirectory + "/player_hands_carry_twohand_body_alignment_final.png";
        private const string AlignmentReviewStageKey =
            "Bellerophon.PlayerHandsCarryBodyAlignment.Review.Stage";
        private const string OneHandAdjustedClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Carry_OneHand_ArmAdjusted.anim";
        private const string TwoHandAdjustedClipPath =
            "Assets/_Project/Art/Player/Animations/Hands_Carry_TwoHand_ArmAdjusted.anim";
        private const string PoseAdjustmentApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_pose_adjustment_apply_metrics.json";
        private const string PoseAdjustmentReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_pose_adjustment_review_metrics.json";
        private const string OneHandPoseAdjustmentReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_pose_adjustment_review_contact_sheet.png";
        private const string TwoHandPoseAdjustmentReviewPath =
            ValidationDirectory + "/player_hands_carry_twohand_pose_adjustment_review_contact_sheet.png";
        private const string OneHandPoseAdjustmentFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_pose_adjustment_final.png";
        private const string TwoHandPoseAdjustmentFinalPath =
            ValidationDirectory + "/player_hands_carry_twohand_pose_adjustment_final.png";
        private const string PoseAdjustmentReviewStageKey =
            "Bellerophon.PlayerHandsCarryPoseAdjustment.Review.Stage";
        private const string GripClearanceApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_grip_clearance_apply_metrics.json";
        private const string GripClearanceReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_grip_clearance_review_metrics.json";
        private const string GripClearanceBeforePath =
            ValidationDirectory + "/player_hands_carry_onehand_grip_clearance_before.png";
        private const string GripClearanceReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_grip_clearance_review_contact_sheet.png";
        private const string GripClearanceFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_grip_clearance_final.png";
        private const string GripClearanceReviewStageKey =
            "Bellerophon.PlayerHandsCarryOneHandGripClearance.Review.Stage";
        private const string WristGripCorrectionApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_grip_correction_apply_metrics.json";
        private const string WristGripCorrectionReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_grip_correction_review_metrics.json";
        private const string WristGripCorrectionBeforePath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_grip_correction_before.png";
        private const string WristGripCorrectionReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_grip_correction_review_contact_sheet.png";
        private const string WristGripCorrectionFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_grip_correction_final.png";
        private const string WristGripCorrectionReviewStageKey =
            "Bellerophon.PlayerHandsCarryOneHandWristGripCorrection.Review.Stage";
        private const string Wrist180FlipApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_180_flip_apply_metrics.json";
        private const string Wrist180FlipReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_180_flip_review_metrics.json";
        private const string Wrist180FlipBeforePath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_180_flip_before.png";
        private const string Wrist180FlipReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_180_flip_review_contact_sheet.png";
        private const string Wrist180FlipFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_wrist_180_flip_final.png";
        private const string Wrist180FlipReviewStageKey =
            "Bellerophon.PlayerHandsCarryOneHandWrist180Flip.Review.Stage";
        private const string NaturalVerticalGripApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_natural_vertical_grip_apply_metrics.json";
        private const string NaturalVerticalGripReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_natural_vertical_grip_review_metrics.json";
        private const string NaturalVerticalGripBeforePath =
            ValidationDirectory + "/player_hands_carry_onehand_natural_vertical_grip_before.png";
        private const string NaturalVerticalGripReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_natural_vertical_grip_review_contact_sheet.png";
        private const string NaturalVerticalGripFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_natural_vertical_grip_final.png";
        private const string NaturalVerticalGripReviewStageKey =
            "Bellerophon.PlayerHandsCarryOneHandNaturalVerticalGrip.Review.Stage";
        private const string AnatomicalWristGripApplyMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_anatomical_wrist_grip_apply_metrics.json";
        private const string AnatomicalWristGripReviewMetricsPath =
            ValidationDirectory + "/player_hands_carry_onehand_anatomical_wrist_grip_review_metrics.json";
        private const string AnatomicalWristGripBeforePath =
            ValidationDirectory + "/player_hands_carry_onehand_anatomical_wrist_grip_before.png";
        private const string AnatomicalWristGripReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_anatomical_wrist_grip_review_contact_sheet.png";
        private const string AnatomicalWristGripReviewCloseFrontPath =
            ValidationDirectory + "/player_hands_carry_onehand_anatomical_wrist_grip_review_close_front.png";
        private const string AnatomicalWristGripReviewCloseSidePath =
            ValidationDirectory + "/player_hands_carry_onehand_anatomical_wrist_grip_review_close_side.png";
        private const string AnatomicalWristGripFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_anatomical_wrist_grip_final.png";
        private const string AnatomicalWristGripReviewStageKey =
            "Bellerophon.PlayerHandsCarryOneHandAnatomicalWristGrip.Review.Stage";
        private const string ActualPalmInwardGripBeforePath =
            ValidationDirectory + "/player_hands_carry_onehand_actual_palm_inward_grip_before.png";
        private const string ActualPalmInwardGripReviewPath =
            ValidationDirectory + "/player_hands_carry_onehand_actual_palm_inward_grip_review_contact_sheet.png";
        private const string ActualPalmInwardGripReviewCloseFrontPath =
            ValidationDirectory + "/player_hands_carry_onehand_actual_palm_inward_grip_review_close_front.png";
        private const string ActualPalmInwardGripReviewCloseSidePath =
            ValidationDirectory + "/player_hands_carry_onehand_actual_palm_inward_grip_review_close_side.png";
        private const string ActualPalmInwardGripReviewPalmFromTorsoPath =
            ValidationDirectory + "/player_hands_carry_onehand_actual_palm_inward_grip_review_palm_from_torso.png";
        private const string ActualPalmInwardGripFinalPath =
            ValidationDirectory + "/player_hands_carry_onehand_actual_palm_inward_grip_final.png";
        private const string ActualPalmInwardGripReviewStageKey =
            "Bellerophon.PlayerHandsCarryOneHandActualPalmInwardGrip.Review.Stage";
        private const string HipsPath = "Armature/Hips";
        private const string SolarPlexusPath =
            "Armature/Hips/Spine02/Spine01";
        private const string SpinePath =
            "Armature/Hips/Spine02/Spine01/Spine";
        private const string LeftShoulderPath =
            SpinePath + "/LeftShoulder";
        private const string LeftArmPath = LeftShoulderPath + "/LeftArm";
        private const string LeftForeArmPath = LeftArmPath + "/LeftForeArm";
        private const string LeftHandPath = LeftForeArmPath + "/LeftHand";
        private const string RightShoulderPath =
            SpinePath + "/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const string HeadPath = SpinePath + "/neck/Head";
        private const string LeftUpLegPath = HipsPath + "/LeftUpLeg";
        private const string LeftLegPath = LeftUpLegPath + "/LeftLeg";
        private const string LeftFootPath = LeftLegPath + "/LeftFoot";
        private const string RightUpLegPath = HipsPath + "/RightUpLeg";
        private const string RightLegPath = RightUpLegPath + "/RightLeg";
        private const string RightFootPath = RightLegPath + "/RightFoot";

        [Serializable]
        private sealed class TargetApplyMetrics
        {
            public string target;
            public string state;
            public string sourceTake;
            public string clipPath;
            public float durationSeconds;
            public float frameRate;
            public int floatCurveCount;
            public int objectCurveCount;
            public int eventCount;
            public bool stateUsesExactClip;
            public bool loopTime;
            public bool applyRootMotion;
        }

        [Serializable]
        private sealed class ApplyMetrics
        {
            public string targetSet;
            public string idleSourceHashBefore;
            public string idleSourceHashAfter;
            public string oneHandOriginalHash;
            public string oneHandUnityHash;
            public string twoHandOriginalHash;
            public string twoHandUnityHash;
            public TargetApplyMetrics emptyIdle;
            public TargetApplyMetrics oneHand;
            public TargetApplyMetrics twoHand;
            public bool idleSourceUnchanged;
            public bool idleCopyCurvesExact;
            public bool sourceFbxCopiesExact;
            public bool rootsUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class TargetReviewMetrics
        {
            public string target;
            public string state;
            public string sourceTake;
            public float durationSeconds;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootPositionDisplacementMax;
            public float sourcePosePositionDifferenceMax;
            public float sourcePoseRotationDifferenceDegreesMax;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string targetSet;
            public TargetReviewMetrics emptyIdle;
            public TargetReviewMetrics oneHand;
            public TargetReviewMetrics twoHand;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class OneHandEmbeddedTakeApplyMetrics
        {
            public string target;
            public string originalHash;
            public string unityHashBefore;
            public string unityHashAfter;
            public int controllerLayerCount;
            public TargetApplyMetrics oneHand;
            public bool sourceFbxExactAndUnchanged;
            public bool controllerUsesSingleEmbeddedTake;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class OneHandEmbeddedTakeReviewMetrics
        {
            public string targetSet;
            public TargetReviewMetrics oneHand;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class HandsBackApplyMetrics
        {
            public string targetSet;
            public string drawBackOriginalHash;
            public string drawBackUnityHash;
            public string stowBackOriginalHash;
            public string stowBackUnityHash;
            public TargetApplyMetrics drawBack;
            public TargetApplyMetrics stowBack;
            public bool sourceFbxCopiesExact;
            public bool rootsUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class HandsBackReviewMetrics
        {
            public string targetSet;
            public TargetReviewMetrics drawBack;
            public TargetReviewMetrics stowBack;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackExactReconnectApplyMetrics
        {
            public string target;
            public string originalHash;
            public string unityHashBefore;
            public string unityHashAfter;
            public string adjustedClipHashBefore;
            public string adjustedClipHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public string targetMeshPathBefore;
            public string targetMeshPathAfter;
            public int controllerLayerCount;
            public TargetApplyMetrics drawBack;
            public bool sourceFbxExactAndUnchanged;
            public bool adjustedClipUnchanged;
            public bool stowControllerUnchanged;
            public bool targetMeshUnchanged;
            public bool controllerUsesSingleEmbeddedTake;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackExactReconnectReviewMetrics
        {
            public string targetSet;
            public TargetReviewMetrics drawBack;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackCommonMeshApplyMetrics
        {
            public string target;
            public string emptyReference;
            public string rendererPath;
            public string correctedMeshPathBefore;
            public string sharedMeshPathAfter;
            public string emptySharedMeshPath;
            public float[] correctedBlendShapeWeightsBefore;
            public string playerFbxHashBefore;
            public string playerFbxHashAfter;
            public string correctedMeshHashBefore;
            public string correctedMeshHashAfter;
            public string sourceOriginalHash;
            public string sourceUnityHashBefore;
            public string sourceUnityHashAfter;
            public string drawControllerHashBefore;
            public string drawControllerHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public bool rendererPathsMatch;
            public bool meshOverrideRemoved;
            public bool blendShapeOverridesRemoved;
            public bool rendererConfigurationMatchesEmpty;
            public bool correctedMeshUnreferencedByScene;
            public bool sourceAssetsUnchanged;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool otherRendererMeshesUnchanged;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackCommonMeshReviewMetrics
        {
            public string target;
            public string emptyReference;
            public int phasesCaptured;
            public TargetReviewMetrics drawBack;
            public bool rendererConfigurationMatchesEmpty;
            public bool correctedMeshUnreferencedByScene;
            public bool correctedMeshAssetUnchanged;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackCommonMeshForwardApplyMetrics
        {
            public string target;
            public string sourceOriginalHash;
            public string sourceUnityHashBefore;
            public string sourceUnityHashAfter;
            public string playerFbxHashBefore;
            public string playerFbxHashAfter;
            public string correctedMeshHashBefore;
            public string correctedMeshHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public float sourceDurationSeconds;
            public float adjustedDurationSeconds;
            public float frameRate;
            public int framesBaked;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public int extractionStartFrame;
            public int outerPathFrame;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalForwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public float adjustedOuterElbowLateralMeters;
            public float adjustedOuterHandLateralMeters;
            public float torsoOuterBoundaryLateralMeters;
            public float minimumFrontSilhouetteGapMeters;
            public int minimumFrontSilhouetteGapFrame;
            public Quaternion rightHandBindLocalRotation;
            public bool durationAndFrameRatePreserved;
            public bool sourceFbxExactAndUnchanged;
            public bool nonRightArmCurvesAndEventsUnchanged;
            public bool hasOnlyApprovedRightArmReplacementCurves;
            public bool hasNoBlendShapeCurves;
            public bool correctedMeshAssetUnchanged;
            public bool stowBackUnchanged;
            public bool controllerUsesAdjustedClip;
            public bool adjustedClipLoops;
            public bool rendererConfigurationMatchesEmpty;
            public bool correctedMeshUnreferencedByScene;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackCommonMeshForwardReviewMetrics
        {
            public string target;
            public int phasesCaptured;
            public DrawBackOuterElbowReviewMetrics motion;
            public bool rendererConfigurationMatchesEmpty;
            public bool correctedMeshUnreferencedByScene;
            public bool correctedMeshAssetUnchanged;
            public bool hasNoBlendShapeCurves;
            public float minimumFrontSilhouetteGapMeters;
            public int minimumFrontSilhouetteGapFrame;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ThrowSourceDiagnosticMetrics
        {
            public string sourceClipName;
            public float sourceDurationSeconds;
            public float frameRate;
            public int frameIntervals;
            public int framesCaptured;
            public int peakRightHandFrame;
            public float peakRightHandTimeSeconds;
            public float peakRightHandHeightMeters;
            public int peakCandidateCount;
            public bool uniquePeakCandidate;
            public string sourceOriginalHash;
            public string sourceUnityHash;
            public bool sourceCopyExact;
            public bool sceneUnchanged;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ThrowApplyMetrics
        {
            public string sourceClipName;
            public float sourceDurationSeconds;
            public float frameRate;
            public int sourceFrameIntervals;
            public int sourcePeakFrame;
            public int readyEndFrame;
            public float readyEndTimeSeconds;
            public float previousRightHandMinusHeadHeightMeters;
            public float rightHandHeightMeters;
            public float headHeightMeters;
            public float rightHandMinusHeadHeightMeters;
            public float holdDurationSeconds;
            public float breathingFrequencyHertz;
            public int breathingCycleCount;
            public float breathingMaximumWeight;
            public float requestedChestExpansionMeters;
            public float measuredChestExpansionMeters;
            public float requestedBodyDropMeters;
            public float measuredBodyDropMeters;
            public float maximumFootDisplacementMeters;
            public float minimumKneeFlexIncreaseDegrees;
            public float readyDurationSeconds;
            public float releaseDurationSeconds;
            public string rendererPath;
            public string commonMeshPathBefore;
            public string breathingMeshPathAfter;
            public string breathingBlendShapeName;
            public int breathingBlendShapeIndex;
            public int breathingAffectedVertexCount;
            public int breathingFrontVertexCount;
            public int breathingLeftSideVertexCount;
            public int breathingRightSideVertexCount;
            public int sourceFloatCurveCount;
            public int readyFloatCurveCount;
            public int sourceObjectCurveCount;
            public int readyObjectCurveCount;
            public float readyPrefixPositionDifferenceMax;
            public float readyPrefixRotationDifferenceDegreesMax;
            public string sourceOriginalHash;
            public string sourceUnityHash;
            public string playerModelHashBefore;
            public string playerModelHashAfter;
            public string baseClipHashBefore;
            public string baseClipHashAfter;
            public string peakClipHashBefore;
            public string peakClipHashAfter;
            public string releaseControllerHashBefore;
            public string releaseControllerHashAfter;
            public bool sourceCopyExact;
            public bool firstHeadHeightFrame;
            public bool readyEndBeforeSourcePeak;
            public bool readySourcePrefixPreserved;
            public bool breathingBlendShapeBound;
            public bool breathingMeshAppliedOnlyToReady;
            public bool otherRendererMeshesUnchanged;
            public bool releaseUsesExactEmbeddedTake;
            public bool releaseControllerUnchanged;
            public bool sourceAssetsUnchanged;
            public bool previousReadyClipsUnchanged;
            public bool readyControllerUsesClip;
            public bool releaseControllerUsesClip;
            public bool readyLoops;
            public bool releaseLoops;
            public bool readyRootUnchanged;
            public bool releaseRootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool readyAnimatorSettingsCorrect;
            public bool releaseAnimatorSettingsCorrect;
            public bool readyApplyRootMotion;
            public bool releaseApplyRootMotion;
            public bool sceneSavedClean;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ThrowBreathingRuntimeMetrics
        {
            public float maximumBlendShapeWeight;
            public float measuredBodyDropMeters;
            public float maximumLeftFootDisplacementMeters;
            public float maximumRightFootDisplacementMeters;
            public int detectedBreathingPeaks;
            public bool blendShapeCurveApplied;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class ThrowReviewMetrics
        {
            public string targetSet;
            public int phasesCapturedPerComparison;
            public TargetReviewMetrics ready;
            public TargetReviewMetrics release;
            public float readyPrefixPositionDifferenceMax;
            public float readyPrefixRotationDifferenceDegreesMax;
            public ThrowBreathingRuntimeMetrics breathing;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ThrowCancelApplyMetrics
        {
            public string target;
            public string state;
            public string sourceReadyClipPath;
            public string idleClipPath;
            public string cancelClipPath;
            public int readyEndFrame;
            public float frameRate;
            public float readyEndTimeSeconds;
            public float initialHoldDurationSeconds;
            public float reverseDurationSeconds;
            public float finalIdleHoldDurationSeconds;
            public float totalDurationSeconds;
            public int floatCurveCount;
            public int objectCurveCount;
            public int eventCount;
            public float holdPositionDifferenceMax;
            public float holdRotationDifferenceDegreesMax;
            public float reversePositionDifferenceMax;
            public float reverseRotationDifferenceDegreesMax;
            public float finalIdlePositionDifferenceMax;
            public float finalIdleRotationDifferenceDegreesMax;
            public float finalHoldPositionDifferenceMax;
            public float finalHoldRotationDifferenceDegreesMax;
            public string idleClipHashBefore;
            public string idleClipHashAfter;
            public string readyClipHashBefore;
            public string readyClipHashAfter;
            public string readyControllerHashBefore;
            public string readyControllerHashAfter;
            public string releaseControllerHashBefore;
            public string releaseControllerHashAfter;
            public string targetMeshPathBefore;
            public string targetMeshPathAfter;
            public bool hasNoBlendShapeCurves;
            public bool controllerUsesCancelClip;
            public bool clipLoops;
            public bool rootUnchanged;
            public bool targetMeshUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool otherRendererMeshesUnchanged;
            public bool animatorSettingsCorrect;
            public bool applyRootMotion;
            public bool sceneSavedClean;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class ThrowCancelReviewMetrics
        {
            public string target;
            public int phasesCaptured;
            public TargetReviewMetrics runtime;
            public float holdPositionDifferenceMax;
            public float holdRotationDifferenceDegreesMax;
            public float expectedReversePositionDifferenceMax;
            public float expectedReverseRotationDifferenceDegreesMax;
            public float finalIdlePositionDifferenceMax;
            public float finalIdleRotationDifferenceDegreesMax;
            public float finalHoldPositionDifferenceMax;
            public float finalHoldRotationDifferenceDegreesMax;
            public bool hasNoBlendShapeCurves;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class ThrowBreathingMeshBuildResult
        {
            internal Mesh Mesh;
            internal string RendererPath;
            internal int BlendShapeIndex;
            internal int AffectedVertexCount;
            internal int FrontVertexCount;
            internal int LeftSideVertexCount;
            internal int RightSideVertexCount;
            internal float MaximumExpansionAtThirtyPercentMeters;
        }

        private sealed class ThrowBreathingMotionBuildResult
        {
            internal int BreathingCycleCount;
            internal int CurveKeyCount;
            internal float MaximumBodyDropMeters;
            internal float MaximumFootDisplacementMeters;
            internal float MinimumKneeFlexIncreaseDegrees;
        }

        [Serializable]
        private sealed class DrawBackForwardApplyMetrics
        {
            public string target;
            public string sourceOriginalHash;
            public string sourceUnityHashBefore;
            public string sourceUnityHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public float sourceDurationSeconds;
            public float adjustedDurationSeconds;
            public float frameRate;
            public int framesBaked;
            public int sourceCurveCount;
            public int adjustedCurveCount;
            public int sourceEventCount;
            public int adjustedEventCount;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public float sourcePeakShoulderToHandForwardAngleDegrees;
            public float adjustedPeakShoulderToHandForwardAngleDegrees;
            public float sourcePeakElbowFlexDegrees;
            public float adjustedPeakElbowFlexDegrees;
            public float rightHandWorldRotationDifferenceDegreesMax;
            public float shoulderToHandReachDifferenceMetersMax;
            public float targetReachErrorMetersMax;
            public Quaternion rightHandBindLocalRotation;
            public bool durationAndFrameRatePreserved;
            public bool timingPeakFramePreserved;
            public bool sourceFbxExactAndUnchanged;
            public bool nonRightArmCurvesAndEventsUnchanged;
            public bool stowBackUnchanged;
            public bool controllerUsesAdjustedClip;
            public bool adjustedClipLoops;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackForwardReviewMetrics
        {
            public string target;
            public int sourcePeakFrame;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootPositionDisplacementMax;
            public float runtimeAdjustedPosePositionDifferenceMax;
            public float runtimeAdjustedPoseRotationDifferenceDegreesMax;
            public float unchangedPosePositionDifferenceMax;
            public float unchangedPoseRotationDifferenceDegreesMax;
            public float sourcePeakShoulderToHandForwardAngleDegrees;
            public float adjustedPeakShoulderToHandForwardAngleDegrees;
            public float sourcePeakElbowFlexDegrees;
            public float adjustedPeakElbowFlexDegrees;
            public float rightHandWorldRotationDifferenceDegreesMax;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackLowPalmLeftApplyMetrics
        {
            public string target;
            public string sourceOriginalHash;
            public string sourceUnityHashBefore;
            public string sourceUnityHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public float sourceDurationSeconds;
            public float adjustedDurationSeconds;
            public float frameRate;
            public int framesBaked;
            public int sourceCurveCount;
            public int adjustedCurveCount;
            public int sourceEventCount;
            public int adjustedEventCount;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public float expectedElbowFlexDegrees;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalForwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public float targetReachErrorMetersMax;
            public Quaternion rightHandBindLocalRotation;
            public bool durationAndFrameRatePreserved;
            public bool timingPeakFramePreserved;
            public bool sourceFbxExactAndUnchanged;
            public bool nonRightArmCurvesAndEventsUnchanged;
            public bool stowBackUnchanged;
            public bool controllerUsesAdjustedClip;
            public bool adjustedClipLoops;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackLowPalmLeftReviewMetrics
        {
            public string target;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootPositionDisplacementMax;
            public float runtimeAdjustedPosePositionDifferenceMax;
            public float runtimeAdjustedPoseRotationDifferenceDegreesMax;
            public float unchangedPosePositionDifferenceMax;
            public float unchangedPoseRotationDifferenceDegreesMax;
            public float expectedElbowFlexDegrees;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalForwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackOuterElbowApplyMetrics
        {
            public string target;
            public string sourceOriginalHash;
            public string sourceUnityHashBefore;
            public string sourceUnityHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public float sourceDurationSeconds;
            public float adjustedDurationSeconds;
            public float frameRate;
            public int framesBaked;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public int extractionStartFrame;
            public int outerPathFrame;
            public float sourceOuterElbowLateralMeters;
            public float adjustedOuterElbowLateralMeters;
            public float sourceOuterHandLateralMeters;
            public float adjustedOuterHandLateralMeters;
            public float torsoOuterBoundaryLateralMeters;
            public float adjustedElbowBeyondTorsoMeters;
            public float adjustedHandBeyondTorsoMeters;
            public float adjustedElbowBeyondHandMeters;
            public float elbowOutwardIncreaseMeters;
            public float handOutwardIncreaseMeters;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalForwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public float targetReachErrorMetersMax;
            public Quaternion rightHandBindLocalRotation;
            public bool durationAndFrameRatePreserved;
            public bool timingPeakFramePreserved;
            public bool sourceFbxExactAndUnchanged;
            public bool nonRightArmCurvesAndEventsUnchanged;
            public bool stowBackUnchanged;
            public bool controllerUsesAdjustedClip;
            public bool adjustedClipLoops;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackOuterElbowReviewMetrics
        {
            public string target;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public int extractionStartFrame;
            public int outerPathFrame;
            public int framesPerLoop;
            public int framesSampled;
            public int loopsSampled;
            public float rootPositionDisplacementMax;
            public float runtimeAdjustedPosePositionDifferenceMax;
            public float runtimeAdjustedPoseRotationDifferenceDegreesMax;
            public float unchangedPosePositionDifferenceMax;
            public float unchangedPoseRotationDifferenceDegreesMax;
            public float sourceOuterElbowLateralMeters;
            public float adjustedOuterElbowLateralMeters;
            public float sourceOuterHandLateralMeters;
            public float adjustedOuterHandLateralMeters;
            public float torsoOuterBoundaryLateralMeters;
            public float adjustedElbowBeyondTorsoMeters;
            public float adjustedHandBeyondTorsoMeters;
            public float adjustedElbowBeyondHandMeters;
            public float elbowOutwardIncreaseMeters;
            public float handOutwardIncreaseMeters;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalForwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class TransporterPurpleFlagApplyMetrics
        {
            public string targetSet;
            public string textureBaselineHash;
            public string textureHashAfter;
            public string duplicateTextureHashAfter;
            public int sharedPlayerModelInstanceCount;
            public int leftArmTrianglesScanned;
            public int flagSeedTriangleCount;
            public int flagPatchTriangleCount;
            public int recoloredPixelCount;
            public Color targetLightPurple;
            public float drawBackDurationSeconds;
            public float drawBackFrameRate;
            public int drawBackFramesBaked;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public int extractionStartFrame;
            public int outerPathFrame;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalForwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public float minimumRightArmTorsoClearanceMeters;
            public int minimumClearanceFrame;
            public Vector3 playerStartPosition;
            public Quaternion playerStartRotation;
            public float playerCameraPitchDegrees;
            public float playerToEmptyDistanceMeters;
            public bool bothTextureCopiesExact;
            public bool sourceFbxExactAndUnchanged;
            public bool stowBackUnchanged;
            public bool nonRightArmCurvesAndEventsUnchanged;
            public bool adjustedClipLoops;
            public bool controllerUsesAdjustedClip;
            public bool playerStartsOnEmptyFrontSide;
            public bool playerCameraTargetsEmptyCenter;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class TransporterPurpleFlagReviewMetrics
        {
            public string targetSet;
            public int sharedPlayerModelInstanceCount;
            public int transporterTargetsDirectlyCaptured;
            public int drawBackFramesDirectlyCaptured;
            public int drawBackFramesSampled;
            public int drawBackLoopsSampled;
            public float minimumRightArmTorsoClearanceMeters;
            public int minimumClearanceFrame;
            public float runtimeAdjustedPosePositionDifferenceMax;
            public float runtimeAdjustedPoseRotationDifferenceDegreesMax;
            public float startScreenHorizontalCenterErrorNormalized;
            public float startScreenVerticalCenterErrorNormalized;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool sharedTextureAppliedToAllTransporters;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class TransporterTextureEditResult
        {
            internal int LeftArmTrianglesScanned;
            internal int FlagSeedTriangleCount;
            internal int FlagPatchTriangleCount;
            internal int RecoloredPixelCount;
            internal Color TargetLightPurple;
        }

        [Serializable]
        private sealed class DrawBackFrontSilhouetteApplyMetrics
        {
            public string target;
            public string sourceOriginalHash;
            public string sourceUnityHashBefore;
            public string sourceUnityHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public string transporterTextureHashBefore;
            public string transporterTextureHashAfter;
            public float sourceDurationSeconds;
            public float adjustedDurationSeconds;
            public float frameRate;
            public int framesBaked;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public float minimumFrontSilhouetteGapMeters;
            public int minimumFrontSilhouetteGapFrame;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalForwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public float targetReachErrorMetersMax;
            public Quaternion rightHandBindLocalRotation;
            public bool durationAndFrameRatePreserved;
            public bool timingPeakFramePreserved;
            public bool sourceFbxExactAndUnchanged;
            public bool nonRightArmCurvesAndEventsUnchanged;
            public bool stowBackUnchanged;
            public bool transporterTextureUnchanged;
            public bool controllerUsesAdjustedClip;
            public bool adjustedClipLoops;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackFrontSilhouetteReviewMetrics
        {
            public string target;
            public int framesPerLoop;
            public int framesDirectlyCaptured;
            public int framesSampled;
            public int loopsSampled;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public float minimumFrontSilhouetteGapMeters;
            public int minimumFrontSilhouetteGapFrame;
            public float rootPositionDisplacementMax;
            public float runtimeAdjustedPosePositionDifferenceMax;
            public float runtimeAdjustedPoseRotationDifferenceDegreesMax;
            public float unchangedPosePositionDifferenceMax;
            public float unchangedPoseRotationDifferenceDegreesMax;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalForwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackChestDeformationApplyMetrics
        {
            public string target;
            public string sourceOriginalHash;
            public string sourceUnityHashBefore;
            public string sourceUnityHashAfter;
            public string adjustedClipHashBefore;
            public string adjustedClipHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public string transporterTextureHashBefore;
            public string transporterTextureHashAfter;
            public float sourceDurationSeconds;
            public float adjustedDurationSeconds;
            public float frameRate;
            public int framesBaked;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public float previousPeakHorizontalOutwardAngleDegrees;
            public float adjustedPeakHorizontalOutwardAngleDegrees;
            public float outwardAngleReductionDegrees;
            public float minimumFrontSilhouetteGapMeters;
            public int minimumFrontSilhouetteGapFrame;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public float targetReachErrorMetersMax;
            public Quaternion rightHandBindLocalRotation;
            public bool durationAndFrameRatePreserved;
            public bool timingPeakFramePreserved;
            public bool sourceFbxExactAndUnchanged;
            public bool nonRightArmCurvesAndEventsUnchanged;
            public bool stowBackUnchanged;
            public bool transporterTextureUnchanged;
            public bool controllerUsesAdjustedClip;
            public bool adjustedClipLoops;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackChestDeformationReviewMetrics
        {
            public string target;
            public int framesPerLoop;
            public int framesDirectlyCaptured;
            public int framesSampled;
            public int loopsSampled;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public float previousPeakHorizontalOutwardAngleDegrees;
            public float adjustedPeakHorizontalOutwardAngleDegrees;
            public float outwardAngleReductionDegrees;
            public float minimumFrontSilhouetteGapMeters;
            public int minimumFrontSilhouetteGapFrame;
            public float rootPositionDisplacementMax;
            public float runtimeAdjustedPosePositionDifferenceMax;
            public float runtimeAdjustedPoseRotationDifferenceDegreesMax;
            public float unchangedPosePositionDifferenceMax;
            public float unchangedPoseRotationDifferenceDegreesMax;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackRightChestDiagnosticMetrics
        {
            public string target;
            public string rendererPath;
            public string sourceMeshName;
            public string sourceMeshAssetPath;
            public int vertexCount;
            public int sourceBlendShapeCount;
            public int framesPerLoop;
            public int framesSampled;
            public int maximumProtrusionFrame;
            public int maximumProtrusionVertexIndex;
            public float maximumForwardProtrusionMeters;
            public float averageAffectedForwardProtrusionMeters;
            public int affectedVertexCount;
            public float maximumVertexRightArmWeight;
            public float maximumVertexRightShoulderWeight;
            public float maximumVertexTorsoWeight;
            public float maximumVertexOtherWeight;
            public Vector3 maximumVertexSourceWorldPosition;
            public Vector3 maximumVertexAdjustedWorldPosition;
            public string diagnosedCause;
            public string playerFbxHash;
            public string sourceAnimationFbxHash;
            public string adjustedClipHash;
            public bool sourceMeshIsSharedPlayerAsset;
            public bool diagnosisComplete;
            public string validationPriority;
        }

        private sealed class DrawBackRightChestDiagnosticResult
        {
            internal SkinnedMeshRenderer Renderer;
            internal string RendererPath;
            internal int FramesPerLoop;
            internal int MaximumProtrusionFrame;
            internal int MaximumProtrusionVertexIndex;
            internal float MaximumForwardProtrusionMeters;
            internal float AverageAffectedForwardProtrusionMeters;
            internal int AffectedVertexCount;
            internal float MaximumVertexRightArmWeight;
            internal float MaximumVertexRightShoulderWeight;
            internal float MaximumVertexTorsoWeight;
            internal float MaximumVertexOtherWeight;
            internal Vector3 MaximumVertexSourceWorldPosition;
            internal Vector3 MaximumVertexAdjustedWorldPosition;
        }

        [Serializable]
        private sealed class DrawBackRightChestCorrectionApplyMetrics
        {
            public string target;
            public string rendererPath;
            public string correctedMeshPath;
            public string blendShapeName;
            public int blendShapeIndex;
            public int correctedVertexCount;
            public int blendShapeCurveKeyCount;
            public float maximumBindPoseCorrectionMeters;
            public float beforeMaximumForwardProtrusionMeters;
            public float afterMaximumForwardProtrusionMeters;
            public int beforeAffectedVertexCount;
            public int afterAffectedVertexCount;
            public string playerFbxHashBefore;
            public string playerFbxHashAfter;
            public string sourceAnimationFbxHashBefore;
            public string sourceAnimationFbxHashAfter;
            public string stowControllerHashBefore;
            public string stowControllerHashAfter;
            public string transporterTextureHashBefore;
            public string transporterTextureHashAfter;
            public string adjustedClipHashBefore;
            public string adjustedClipHashAfter;
            public string correctedMeshHash;
            public bool rendererUsesCorrectedMesh;
            public bool blendShapeCurveBound;
            public bool sourceAssetsUnchanged;
            public bool otherTransportersKeepSharedPlayerMesh;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class DrawBackRightChestCorrectionReviewMetrics
        {
            public string target;
            public int framesPerLoop;
            public int framesDirectlyCaptured;
            public int framesSampled;
            public int loopsSampled;
            public float beforeMaximumForwardProtrusionMeters;
            public float afterMaximumForwardProtrusionMeters;
            public int afterAffectedVertexCount;
            public int blendShapeIndex;
            public float blendShapeWeightMinimum;
            public float blendShapeWeightMaximum;
            public float minimumFrontSilhouetteGapMeters;
            public float rootPositionDisplacementMax;
            public float runtimeAdjustedPosePositionDifferenceMax;
            public float runtimeAdjustedPoseRotationDifferenceDegreesMax;
            public float unchangedPosePositionDifferenceMax;
            public float unchangedPoseRotationDifferenceDegreesMax;
            public int sourcePeakFrame;
            public int adjustedPeakFrame;
            public float adjustedPeakElbowFlexDegrees;
            public float adjustedPeakHandSolarPlexusHeightDifferenceMeters;
            public float adjustedPeakHorizontalOutwardAngleDegrees;
            public float adjustedPeakPalmCharacterLeftAngleDegrees;
            public bool stateLoops;
            public bool applyRootMotion;
            public bool blendShapeCurveBound;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class DrawBackRightChestCorrectiveBuildResult
        {
            internal Mesh CorrectedMesh;
            internal int BlendShapeIndex;
            internal int CorrectedVertexCount;
            internal int CurveKeyCount;
            internal float MaximumBindPoseCorrectionMeters;
        }

        [Serializable]
        private sealed class AlignmentTargetApplyMetrics
        {
            public string target;
            public string baseState;
            public string armState;
            public string armTake;
            public float baseDurationSeconds;
            public float armDurationSeconds;
            public bool hasTwoLayers;
            public bool baseUsesEmptyIdle;
            public bool armLayerUsesExactTake;
            public bool armLayerUsesMask;
            public bool armLayerOverrideAtFullWeight;
            public bool bothClipsLoop;
            public bool applyRootMotion;
        }

        [Serializable]
        private sealed class AlignmentApplyMetrics
        {
            public string targetSet;
            public string emptyIdleHashBefore;
            public string emptyIdleHashAfter;
            public string oneHandFbxHashBefore;
            public string oneHandFbxHashAfter;
            public string twoHandFbxHashBefore;
            public string twoHandFbxHashAfter;
            public int maskTransformCount;
            public int activeArmTransformCount;
            public bool hasLeftShoulderSubtree;
            public bool hasRightShoulderSubtree;
            public AlignmentTargetApplyMetrics oneHand;
            public AlignmentTargetApplyMetrics twoHand;
            public bool inputAnimationsUnchanged;
            public bool armMaskExact;
            public bool rootsUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class AlignmentTargetReviewMetrics
        {
            public string target;
            public string baseState;
            public string armState;
            public string armTake;
            public float baseDurationSeconds;
            public float armDurationSeconds;
            public float reviewDurationSeconds;
            public int framesSampled;
            public int baseLoopsSampled;
            public int armLoopsSampled;
            public float rootPositionDisplacementMax;
            public float bodyPositionDifferenceMax;
            public float bodyRotationDifferenceDegreesMax;
            public float armPositionDifferenceMax;
            public float armRotationDifferenceDegreesMax;
            public bool baseStateLoops;
            public bool armStateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class AlignmentReviewMetrics
        {
            public string targetSet;
            public AlignmentTargetReviewMetrics oneHand;
            public AlignmentTargetReviewMetrics twoHand;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private enum CarryPoseAdjustmentKind
        {
            OneHandLeftArmDown,
            TwoHandRightChest
        }

        [Serializable]
        private sealed class PoseAdjustmentTargetApplyMetrics
        {
            public string target;
            public string adjustment;
            public string adjustedClipPath;
            public float sourceDurationSeconds;
            public float adjustedDurationSeconds;
            public float frameRate;
            public int framesBaked;
            public int adjustedCurveCount;
            public float targetReachErrorMax;
            public Vector3 rootLocalTranslation;
            public bool durationAndFrameRatePreserved;
            public bool adjustedClipLoops;
            public bool adjustedClipOnlyContainsArmCurves;
            public bool controllerUsesAdjustedClip;
            public bool applyRootMotion;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class PoseAdjustmentApplyMetrics
        {
            public string targetSet;
            public string emptyIdleHashBefore;
            public string emptyIdleHashAfter;
            public string oneHandFbxHashBefore;
            public string oneHandFbxHashAfter;
            public string twoHandFbxHashBefore;
            public string twoHandFbxHashAfter;
            public PoseAdjustmentTargetApplyMetrics oneHand;
            public PoseAdjustmentTargetApplyMetrics twoHand;
            public bool inputAnimationsUnchanged;
            public bool rootsUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool animatorSettingsCorrect;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class PoseAdjustmentTargetReviewMetrics
        {
            public string target;
            public string adjustment;
            public float reviewDurationSeconds;
            public int framesSampled;
            public int baseLoopsSampled;
            public int armLoopsSampled;
            public float rootPositionDisplacementMax;
            public float bodyPositionDifferenceMax;
            public float bodyRotationDifferenceDegreesMax;
            public float leftHandBelowShoulderArmLengthsMin;
            public float leftHandBelowHipsMetersMin;
            public float leftHandOutsideHipsMetersMin;
            public float handCenterRightShoulderSpansMin;
            public float handSpacingDifferenceMax;
            public float sourceHandMotionRange;
            public float adjustedHandMotionRange;
            public bool baseStateLoops;
            public bool armStateLoops;
            public bool applyRootMotion;
            public bool passedNumericChecks;
        }

        [Serializable]
        private sealed class PoseAdjustmentReviewMetrics
        {
            public string targetSet;
            public PoseAdjustmentTargetReviewMetrics oneHand;
            public PoseAdjustmentTargetReviewMetrics twoHand;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class GripClearanceApplyMetrics
        {
            public string target;
            public float expectedGripTwistDegrees;
            public string emptyIdleHashBefore;
            public string emptyIdleHashAfter;
            public string oneHandFbxHashBefore;
            public string oneHandFbxHashAfter;
            public string twoHandAdjustedHashBefore;
            public string twoHandAdjustedHashAfter;
            public string twoHandControllerHashBefore;
            public string twoHandControllerHashAfter;
            public string rightUpperArmCurvesHashBefore;
            public string rightUpperArmCurvesHashAfter;
            public string leftArmCurvesHashBefore;
            public string leftArmCurvesHashAfter;
            public float targetReachErrorMax;
            public bool adjustedClipLoops;
            public bool controllerUsesAdjustedClip;
            public bool rootUnchanged;
            public bool otherAnimatorsUnchanged;
            public bool inputAnimationsUnchanged;
            public bool twoHandAssetsUnchanged;
            public bool rightShoulderArmForeArmCurvesUnchanged;
            public bool leftArmCurvesUnchanged;
            public bool naturalRightArmAdjustment;
            public bool rightArmAdjustmentApplied;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        [Serializable]
        private sealed class GripClearanceReviewMetrics
        {
            public string target;
            public float reviewDurationSeconds;
            public int framesSampled;
            public int baseLoopsSampled;
            public int armLoopsSampled;
            public float rootPositionDisplacementMax;
            public float bodyPositionDifferenceMax;
            public float bodyRotationDifferenceDegreesMax;
            public float leftForeArmOutsideSpineMetersMin;
            public float leftHandOutsideHipsMetersMin;
            public float rightShoulderArmForeArmPositionDifferenceMax;
            public float rightShoulderArmForeArmRotationDifferenceDegreesMax;
            public float rightHandPositionDifferenceMax;
            public float rightElbowOutsideSpineMetersMin;
            public float rightElbowBelowShoulderMetersMin;
            public float rightWristLocalRotationDifferenceDegreesMax;
            public float rightForeArmWristAlignmentDegreesMax;
            public float verticalGripAngleDegreesMax;
            public float expectedGripTwistDegrees;
            public float palmFromInwardAngleDegreesMin;
            public float palmInwardAngleDegreesMax;
            public float palmTargetAngleDegreesMax;
            public float sourceRightHandMotionRange;
            public float adjustedRightHandMotionRange;
            public bool baseStateLoops;
            public bool armStateLoops;
            public bool applyRootMotion;
            public bool naturalRightArmAdjustment;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class BakedArmClipResult
        {
            internal AnimationClip Clip;
            internal int FramesBaked;
            internal float TargetReachErrorMax;
            internal Vector3 RootLocalTranslation;
        }

        private sealed class DrawBackForwardBakeResult
        {
            internal AnimationClip Clip;
            internal int FramesBaked;
            internal int SourcePeakFrame;
            internal int AdjustedPeakFrame;
            internal float SourcePeakForwardAngleDegrees;
            internal float AdjustedPeakForwardAngleDegrees;
            internal float SourcePeakElbowFlexDegrees;
            internal float AdjustedPeakElbowFlexDegrees;
            internal float HandWorldRotationDifferenceDegreesMax;
            internal float ReachDifferenceMetersMax;
            internal float TargetReachErrorMetersMax;
        }

        private sealed class DrawBackLowPalmLeftBakeResult
        {
            internal AnimationClip Clip;
            internal int FramesBaked;
            internal int SourcePeakFrame;
            internal int AdjustedPeakFrame;
            internal float AdjustedPeakElbowFlexDegrees;
            internal float AdjustedPeakHandSolarPlexusHeightDifferenceMeters;
            internal float AdjustedPeakHorizontalForwardAngleDegrees;
            internal float AdjustedPeakPalmCharacterLeftAngleDegrees;
            internal float TargetReachErrorMetersMax;
            internal int ExtractionStartFrame;
            internal int OuterPathFrame;
            internal float SourceOuterElbowLateralMeters;
            internal float AdjustedOuterElbowLateralMeters;
            internal float SourceOuterHandLateralMeters;
            internal float AdjustedOuterHandLateralMeters;
            internal float TorsoOuterBoundaryLateralMeters;
            internal float MinimumRightArmTorsoClearanceMeters;
            internal int MinimumClearanceFrame;
            internal float MinimumFrontSilhouetteGapMeters;
            internal int MinimumFrontSilhouetteGapFrame;
        }

        private sealed class TransformCurveTrack
        {
            internal readonly string Path;
            internal readonly List<Keyframe> PositionX = new List<Keyframe>();
            internal readonly List<Keyframe> PositionY = new List<Keyframe>();
            internal readonly List<Keyframe> PositionZ = new List<Keyframe>();
            internal readonly List<Keyframe> RotationX = new List<Keyframe>();
            internal readonly List<Keyframe> RotationY = new List<Keyframe>();
            internal readonly List<Keyframe> RotationZ = new List<Keyframe>();
            internal readonly List<Keyframe> RotationW = new List<Keyframe>();
            private bool hasPreviousRotation;
            private Quaternion previousRotation;

            internal TransformCurveTrack(string path)
            {
                Path = path;
            }

            internal void Add(float time, Transform value)
            {
                Vector3 position = value.localPosition;
                Quaternion rotation = value.localRotation.normalized;
                if (hasPreviousRotation && Quaternion.Dot(previousRotation, rotation) < 0f)
                {
                    rotation = new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w);
                }

                previousRotation = rotation;
                hasPreviousRotation = true;
                PositionX.Add(new Keyframe(time, position.x));
                PositionY.Add(new Keyframe(time, position.y));
                PositionZ.Add(new Keyframe(time, position.z));
                RotationX.Add(new Keyframe(time, rotation.x));
                RotationY.Add(new Keyframe(time, rotation.y));
                RotationZ.Add(new Keyframe(time, rotation.z));
                RotationW.Add(new Keyframe(time, rotation.w));
            }
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

        private sealed class PoseSnapshot
        {
            internal readonly Dictionary<string, Vector3> Positions =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Quaternion> Rotations =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        }

        [MenuItem("Bellerophon/Player/Apply Hands And Objects Animations")]
        internal static void Apply()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands and Objects apply.");
            }

            string idleHashBefore = HashFile(IdleClipPath);
            RequireHash(IdleClipPath, IdleSourceHash, "Player_Idle source");
            EnsureExactSourceCopy(
                OneHandOriginalPath,
                OneHandSourcePath,
                OneHandSourceHash,
                "one-hand carry");
            EnsureExactSourceCopy(
                TwoHandOriginalPath,
                TwoHandSourcePath,
                TwoHandSourceHash,
                "two-hand carry");
            ConfigureSourceImporter(OneHandSourcePath, "one-hand carry");
            ConfigureSourceImporter(TwoHandSourcePath, "two-hand carry");

            AnimationClip idleSource = LoadClip(IdleClipPath);
            AnimationClip emptyClip = CreateOrUpdateIdleCopy(idleSource);
            AnimationClip oneHandClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip twoHandClip = LoadSingleEmbeddedClip(
                TwoHandSourcePath,
                "two-hand carry");
            AnimatorController emptyController = CreateOrUpdateController(
                EmptyControllerPath,
                EmptyStateName,
                emptyClip);
            AnimatorController oneHandController = CreateOrUpdateController(
                OneHandControllerPath,
                OneHandStateName,
                oneHandClip);
            AnimatorController twoHandController = CreateOrUpdateController(
                TwoHandControllerPath,
                TwoHandStateName,
                twoHandClip);

            Transform layout = RequireLayout(scene);
            Transform emptyTarget = RequireTarget(layout, EmptyTargetName);
            Transform oneHandTarget = RequireTarget(layout, OneHandTargetName);
            Transform twoHandTarget = RequireTarget(layout, TwoHandTargetName);
            RootPose emptyRootBefore = new RootPose(emptyTarget);
            RootPose oneHandRootBefore = new RootPose(oneHandTarget);
            RootPose twoHandRootBefore = new RootPose(twoHandTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureOtherAnimatorStates(layout);

            Animator emptyAnimator = ConfigureAnimator(emptyTarget, emptyController);
            Animator oneHandAnimator = ConfigureAnimator(oneHandTarget, oneHandController);
            Animator twoHandAnimator = ConfigureAnimator(twoHandTarget, twoHandController);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string idleHashAfter = HashFile(IdleClipPath);
            bool idleUnchanged = string.Equals(
                idleHashBefore,
                idleHashAfter,
                StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    idleHashAfter,
                    IdleSourceHash,
                    StringComparison.OrdinalIgnoreCase);
            bool idleCopyExact = ClipsHaveSameContent(idleSource, emptyClip);
            bool sourceCopiesExact =
                HashMatches(OneHandOriginalPath, OneHandSourcePath, OneHandSourceHash) &&
                HashMatches(TwoHandOriginalPath, TwoHandSourcePath, TwoHandSourceHash);
            bool rootsUnchanged =
                RootMatches(emptyTarget, emptyRootBefore) &&
                RootMatches(oneHandTarget, oneHandRootBefore) &&
                RootMatches(twoHandTarget, twoHandRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureOtherAnimatorStates(layout));
            bool animatorSettingsCorrect =
                AnimatorMatches(emptyAnimator, emptyController) &&
                AnimatorMatches(oneHandAnimator, oneHandController) &&
                AnimatorMatches(twoHandAnimator, twoHandController);
            TargetApplyMetrics emptyMetrics = CreateTargetApplyMetrics(
                EmptyTargetName,
                EmptyStateName,
                "Player_Idle copied asset",
                EmptyClipPath,
                emptyClip,
                emptyController,
                emptyAnimator);
            TargetApplyMetrics oneHandMetrics = CreateTargetApplyMetrics(
                OneHandTargetName,
                OneHandStateName,
                oneHandClip.name,
                OneHandSourcePath,
                oneHandClip,
                oneHandController,
                oneHandAnimator);
            TargetApplyMetrics twoHandMetrics = CreateTargetApplyMetrics(
                TwoHandTargetName,
                TwoHandStateName,
                twoHandClip.name,
                TwoHandSourcePath,
                twoHandClip,
                twoHandController,
                twoHandAnimator);
            bool clipsCorrect =
                emptyMetrics.stateUsesExactClip && emptyMetrics.loopTime &&
                oneHandMetrics.stateUsesExactClip && oneHandMetrics.loopTime &&
                twoHandMetrics.stateUsesExactClip && twoHandMetrics.loopTime;

            ApplyMetrics metrics = new ApplyMetrics
            {
                targetSet = EmptyTargetName + ", " + OneHandTargetName + ", " + TwoHandTargetName,
                idleSourceHashBefore = idleHashBefore,
                idleSourceHashAfter = idleHashAfter,
                oneHandOriginalHash = HashFile(OneHandOriginalPath),
                oneHandUnityHash = HashFile(OneHandSourcePath),
                twoHandOriginalHash = HashFile(TwoHandOriginalPath),
                twoHandUnityHash = HashFile(TwoHandSourcePath),
                emptyIdle = emptyMetrics,
                oneHand = oneHandMetrics,
                twoHand = twoHandMetrics,
                idleSourceUnchanged = idleUnchanged,
                idleCopyCurvesExact = idleCopyExact,
                sourceFbxCopiesExact = sourceCopiesExact,
                rootsUnchanged = rootsUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                animatorSettingsCorrect = animatorSettingsCorrect,
                passedNumericChecks = idleUnchanged &&
                    idleCopyExact &&
                    sourceCopiesExact &&
                    rootsUnchanged &&
                    otherAnimatorsUnchanged &&
                    animatorSettingsCorrect &&
                    clipsCorrect,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(ApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands and Objects apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsObjects] Applied exact Idle copy and direct embedded carry Takes. " +
                "Empty=" + Num(emptyClip.length) + "s, " +
                "OneHandTake=" + oneHandClip.name + " (" + Num(oneHandClip.length) + "s), " +
                "TwoHandTake=" + twoHandClip.name + " (" + Num(twoHandClip.length) + "s), " +
                "ExactFbxCopies=True, OtherAnimatorsUnchanged=True, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry OneHand Embedded Take Exact")]
        internal static void ApplyCarryOneHandEmbeddedTakeExact()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(OneHandEmbeddedTakeReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsCarryOneHandEmbeddedTake] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before exact OneHand embedded Take apply.");
            }

            string unityHashBefore = HashFile(OneHandSourcePath);
            RequireHash(OneHandOriginalPath, OneHandSourceHash, "one-hand carry original FBX");
            RequireHash(OneHandSourcePath, OneHandSourceHash, "one-hand carry Unity FBX");
            AnimationClip oneHandClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");

            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, OneHandTargetName);
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, OneHandTargetName);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    OneHandControllerPath,
                    OneHandStateName,
                    oneHandClip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string unityHashAfter = HashFile(OneHandSourcePath);
            TargetApplyMetrics targetMetrics = CreateTargetApplyMetrics(
                OneHandTargetName,
                OneHandStateName,
                oneHandClip.name,
                OneHandSourcePath,
                oneHandClip,
                controller,
                animator);
            bool sourceExactAndUnchanged =
                string.Equals(
                    HashFile(OneHandOriginalPath),
                    OneHandSourceHash,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    unityHashBefore,
                    OneHandSourceHash,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    unityHashAfter,
                    OneHandSourceHash,
                    StringComparison.OrdinalIgnoreCase);
            bool controllerExact =
                controller.layers.Length == 1 &&
                targetMetrics.stateUsesExactClip &&
                targetMetrics.loopTime;
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, OneHandTargetName));
            bool animatorSettingsCorrect = AnimatorMatches(animator, controller);
            OneHandEmbeddedTakeApplyMetrics metrics =
                new OneHandEmbeddedTakeApplyMetrics
                {
                    target = OneHandTargetName,
                    originalHash = HashFile(OneHandOriginalPath),
                    unityHashBefore = unityHashBefore,
                    unityHashAfter = unityHashAfter,
                    controllerLayerCount = controller.layers.Length,
                    oneHand = targetMetrics,
                    sourceFbxExactAndUnchanged = sourceExactAndUnchanged,
                    controllerUsesSingleEmbeddedTake = controllerExact,
                    rootUnchanged = rootUnchanged,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    animatorSettingsCorrect = animatorSettingsCorrect,
                    passedNumericChecks =
                        sourceExactAndUnchanged &&
                        controllerExact &&
                        rootUnchanged &&
                        otherAnimatorsUnchanged &&
                        animatorSettingsCorrect &&
                        !targetMetrics.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteJson(OneHandEmbeddedTakeApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Exact OneHand embedded Take apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(OneHandEmbeddedTakeReviewStageKey);
            Debug.Log(
                "[PlayerHandsCarryOneHandEmbeddedTake] Removed the adjusted layered connection and linked the single embedded Take directly. " +
                "Take=" + oneHandClip.name +
                ", Duration=" + Num(oneHandClip.length) +
                "s, Layers=1, ExactFbxCopy=True, OtherAnimatorsUnchanged=True, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Embedded Take Exact Review")]
        internal static void CaptureCarryOneHandEmbeddedTakeExactReview()
        {
            int stage = SessionState.GetInt(OneHandEmbeddedTakeReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Exact OneHand embedded Take review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before exact OneHand embedded Take review.");
                    }

                    SessionState.SetInt(OneHandEmbeddedTakeReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsCarryOneHandEmbeddedTake] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Exact OneHand embedded Take capture requires Play Mode.");
                    }

                    CaptureCarryOneHandEmbeddedTakeExactActualReview();
                    SessionState.SetInt(OneHandEmbeddedTakeReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Exact OneHand embedded Take review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(OneHandEmbeddedTakeReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsCarryOneHandEmbeddedTake] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Exact OneHand embedded Take review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(OneHandEmbeddedTakeReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Embedded Take Exact Final")]
        internal static void CaptureCarryOneHandEmbeddedTakeExactFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exact OneHand embedded Take final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after exact OneHand embedded Take review.");
            }

            OneHandEmbeddedTakeReviewMetrics metrics =
                ReadJson<OneHandEmbeddedTakeReviewMetrics>(
                    OneHandEmbeddedTakeReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                !metrics.oneHand.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Exact OneHand embedded Take review did not pass before final capture.");
            }

            CopyReviewedContact(
                OneHandEmbeddedTakeReviewPath,
                OneHandEmbeddedTakeFinalPath);
            Debug.Log(
                "[PlayerHandsCarryOneHandEmbeddedTake] Final image copied once from directly reviewed Play Mode frames. " +
                "OneHand=" + Path.GetFullPath(OneHandEmbeddedTakeFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry OneHand Empty Body Palm Left")]
        internal static void ApplyCarryOneHandEmptyBodyPalmLeft()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(OneHandEmptyBodyPalmLeftReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsCarryOneHandEmptyBodyPalmLeft] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before OneHand Empty-body palm-left apply.");
            }

            string emptyHashBefore = HashFile(EmptyClipPath);
            string oneHandHashBefore = HashFile(OneHandSourcePath);
            string twoHandClipHashBefore = HashFile(TwoHandAdjustedClipPath);
            string twoHandControllerHashBefore = HashFile(TwoHandControllerPath);
            RequireHash(OneHandSourcePath, OneHandSourceHash, "one-hand carry Unity FBX");
            CopyReviewedContact(
                OneHandEmbeddedTakeFinalPath,
                OneHandEmptyBodyPalmLeftBeforePath);
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip sourceClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, OneHandTargetName);
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, OneHandTargetName);
            BakedArmClipResult bake = CreateOrUpdateAdjustedArmClip(
                target,
                emptyClip,
                sourceClip,
                OneHandAdjustedClipPath,
                "Hands_Carry_OneHand_ArmAdjusted",
                CarryPoseAdjustmentKind.OneHandLeftArmDown,
                0f,
                true,
                false,
                true);
            AvatarMask armsMask = CreateOrUpdateArmsMask(target);
            AnimatorController controller = CreateOrUpdateLayeredCarryController(
                OneHandControllerPath,
                OneHandStateName,
                emptyClip,
                bake.Clip,
                armsMask);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string emptyHashAfter = HashFile(EmptyClipPath);
            string oneHandHashAfter = HashFile(OneHandSourcePath);
            string twoHandClipHashAfter = HashFile(TwoHandAdjustedClipPath);
            string twoHandControllerHashAfter = HashFile(TwoHandControllerPath);
            bool inputsUnchanged =
                string.Equals(emptyHashBefore, emptyHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oneHandHashBefore, oneHandHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oneHandHashAfter, OneHandSourceHash, StringComparison.OrdinalIgnoreCase);
            bool twoHandUnchanged =
                string.Equals(twoHandClipHashBefore, twoHandClipHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(twoHandControllerHashBefore, twoHandControllerHashAfter, StringComparison.OrdinalIgnoreCase);
            bool controllerUses = controller.layers.Length == 2 &&
                LayerStateUsesClip(controller.layers[0], AlignmentBaseStateName, emptyClip) &&
                LayerStateUsesClip(controller.layers[1], OneHandStateName, bake.Clip);
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, OneHandTargetName));
            GripClearanceApplyMetrics metrics = new GripClearanceApplyMetrics
            {
                target = OneHandTargetName,
                expectedGripTwistDegrees = 0f,
                emptyIdleHashBefore = emptyHashBefore,
                emptyIdleHashAfter = emptyHashAfter,
                oneHandFbxHashBefore = oneHandHashBefore,
                oneHandFbxHashAfter = oneHandHashAfter,
                twoHandAdjustedHashBefore = twoHandClipHashBefore,
                twoHandAdjustedHashAfter = twoHandClipHashAfter,
                twoHandControllerHashBefore = twoHandControllerHashBefore,
                twoHandControllerHashAfter = twoHandControllerHashAfter,
                targetReachErrorMax = bake.TargetReachErrorMax,
                adjustedClipLoops =
                    AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime,
                controllerUsesAdjustedClip = controllerUses,
                rootUnchanged = rootUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                inputAnimationsUnchanged = inputsUnchanged,
                twoHandAssetsUnchanged = twoHandUnchanged,
                rightShoulderArmForeArmCurvesUnchanged = false,
                leftArmCurvesUnchanged = false,
                naturalRightArmAdjustment = true,
                rightArmAdjustmentApplied = true,
                applyRootMotion = animator.applyRootMotion,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            metrics.passedNumericChecks =
                metrics.adjustedClipLoops &&
                controllerUses &&
                rootUnchanged &&
                otherAnimatorsUnchanged &&
                inputsUnchanged &&
                twoHandUnchanged &&
                AdjustedClipOnlyContainsArmCurves(bake.Clip) &&
                bake.TargetReachErrorMax <= 0.005f &&
                AnimatorMatches(animator, controller) &&
                !animator.applyRootMotion;
            WriteJson(OneHandEmptyBodyPalmLeftApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "OneHand Empty-body palm-left apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(OneHandEmptyBodyPalmLeftReviewStageKey);
            Debug.Log(
                "[PlayerHandsCarryOneHandEmptyBodyPalmLeft] Applied Empty Idle body, separated natural left arm, and source-position right arm with actual palm facing character left. " +
                "ReachError=" + Num(bake.TargetReachErrorMax) +
                ", InputsUnchanged=True, TwoHandUnchanged=True, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Empty Body Palm Left Review")]
        internal static void CaptureCarryOneHandEmptyBodyPalmLeftReview()
        {
            CaptureCarryOneHandGripCorrectionReview(
                OneHandEmptyBodyPalmLeftReviewStageKey,
                OneHandEmptyBodyPalmLeftApplyMetricsPath,
                OneHandEmptyBodyPalmLeftReviewMetricsPath,
                OneHandEmptyBodyPalmLeftReviewPath,
                "PlayerHandsCarryOneHandEmptyBodyPalmLeft",
                0f,
                true,
                true);
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Empty Body Palm Left Final")]
        internal static void CaptureCarryOneHandEmptyBodyPalmLeftFinal()
        {
            CaptureCarryOneHandGripCorrectionFinal(
                OneHandEmptyBodyPalmLeftReviewMetricsPath,
                OneHandEmptyBodyPalmLeftReviewPath,
                OneHandEmptyBodyPalmLeftFinalPath,
                "PlayerHandsCarryOneHandEmptyBodyPalmLeft");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw And Stow Back Exact Takes")]
        internal static void ApplyHandsDrawAndStowBackExactTakes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(HandsBackReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsBack] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands Draw/Stow Back apply.");
            }

            EnsureExactSourceCopy(
                DrawBackOriginalPath,
                DrawBackSourcePath,
                DrawBackSourceHash,
                "hands draw back");
            EnsureExactSourceCopy(
                StowBackOriginalPath,
                StowBackSourcePath,
                StowBackSourceHash,
                "hands stow back");
            ConfigureSourceImporter(DrawBackSourcePath, "hands draw back");
            ConfigureSourceImporter(StowBackSourcePath, "hands stow back");
            AnimationClip drawClip = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip stowClip = LoadSingleEmbeddedClip(
                StowBackSourcePath,
                "hands stow back");
            if (!string.Equals(drawClip.name, "mixamo.com", StringComparison.Ordinal) ||
                !string.Equals(stowClip.name, "mixamo.com", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Hands Draw/Stow Back FBX files must expose the exact mixamo.com Take.");
            }

            Transform layout = RequireLayout(scene);
            Transform drawTarget = RequireTarget(layout, DrawBackTargetName);
            Transform stowTarget = RequireTarget(layout, StowBackTargetName);
            RootPose drawRootBefore = new RootPose(drawTarget);
            RootPose stowRootBefore = new RootPose(stowTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTargets(
                    layout,
                    DrawBackTargetName,
                    StowBackTargetName);
            AnimatorController drawController =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    drawClip);
            AnimatorController stowController =
                CreateOrUpdateExactEmbeddedTakeController(
                    StowBackControllerPath,
                    StowBackStateName,
                    stowClip);
            Animator drawAnimator = ConfigureAnimator(drawTarget, drawController);
            Animator stowAnimator = ConfigureAnimator(stowTarget, stowController);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            TargetApplyMetrics drawMetrics = CreateTargetApplyMetrics(
                DrawBackTargetName,
                DrawBackStateName,
                drawClip.name,
                DrawBackSourcePath,
                drawClip,
                drawController,
                drawAnimator);
            TargetApplyMetrics stowMetrics = CreateTargetApplyMetrics(
                StowBackTargetName,
                StowBackStateName,
                stowClip.name,
                StowBackSourcePath,
                stowClip,
                stowController,
                stowAnimator);
            bool sourceCopiesExact =
                HashMatches(DrawBackOriginalPath, DrawBackSourcePath, DrawBackSourceHash) &&
                HashMatches(StowBackOriginalPath, StowBackSourcePath, StowBackSourceHash);
            bool rootsUnchanged =
                RootMatches(drawTarget, drawRootBefore) &&
                RootMatches(stowTarget, stowRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTargets(
                    layout,
                    DrawBackTargetName,
                    StowBackTargetName));
            bool animatorSettingsCorrect =
                AnimatorMatches(drawAnimator, drawController) &&
                AnimatorMatches(stowAnimator, stowController);
            bool controllersExact =
                drawController.layers.Length == 1 &&
                stowController.layers.Length == 1 &&
                drawMetrics.stateUsesExactClip &&
                stowMetrics.stateUsesExactClip &&
                drawMetrics.loopTime &&
                stowMetrics.loopTime;
            HandsBackApplyMetrics metrics = new HandsBackApplyMetrics
            {
                targetSet = DrawBackTargetName + ", " + StowBackTargetName,
                drawBackOriginalHash = HashFile(DrawBackOriginalPath),
                drawBackUnityHash = HashFile(DrawBackSourcePath),
                stowBackOriginalHash = HashFile(StowBackOriginalPath),
                stowBackUnityHash = HashFile(StowBackSourcePath),
                drawBack = drawMetrics,
                stowBack = stowMetrics,
                sourceFbxCopiesExact = sourceCopiesExact,
                rootsUnchanged = rootsUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                animatorSettingsCorrect = animatorSettingsCorrect,
                passedNumericChecks =
                    sourceCopiesExact &&
                    rootsUnchanged &&
                    otherAnimatorsUnchanged &&
                    animatorSettingsCorrect &&
                    controllersExact &&
                    !drawMetrics.applyRootMotion &&
                    !stowMetrics.applyRootMotion,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(HandsBackApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw/Stow Back apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(HandsBackReviewStageKey);
            Debug.Log(
                "[PlayerHandsBack] Applied exact embedded mixamo.com Takes directly. " +
                "Draw=" + Num(drawClip.length) + "s/" + Num(drawClip.frameRate) + "fps, " +
                "Stow=" + Num(stowClip.length) + "s/" + Num(stowClip.frameRate) + "fps, " +
                "ExactFbxCopies=True, OtherAnimatorsUnchanged=True, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw And Stow Back Exact Review")]
        internal static void CaptureHandsDrawAndStowBackExactReview()
        {
            int stage = SessionState.GetInt(HandsBackReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw/Stow Back review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands Draw/Stow Back review.");
                    }

                    SessionState.SetInt(HandsBackReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log("[PlayerHandsBack] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw/Stow Back capture requires Play Mode.");
                    }

                    CaptureHandsDrawAndStowBackExactActualReview();
                    SessionState.SetInt(HandsBackReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw/Stow Back review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(HandsBackReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log("[PlayerHandsBack] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw/Stow Back review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(HandsBackReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw And Stow Back Exact Final")]
        internal static void CaptureHandsDrawAndStowBackExactFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw/Stow Back final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after Hands Draw/Stow Back direct review.");
            }

            HandsBackReviewMetrics metrics =
                ReadJson<HandsBackReviewMetrics>(HandsBackReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                !metrics.drawBack.passedNumericChecks ||
                !metrics.stowBack.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw/Stow Back review did not pass before final capture.");
            }

            CopyReviewedContact(DrawBackReviewPath, DrawBackFinalPath);
            CopyReviewedContact(StowBackReviewPath, StowBackFinalPath);
            Debug.Log(
                "[PlayerHandsBack] Final images copied once from directly reviewed Play Mode frames. " +
                "Draw=" + Path.GetFullPath(DrawBackFinalPath) +
                ", Stow=" + Path.GetFullPath(StowBackFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Reconnect Hands Draw Back Exact Mixamo")]
        internal static void ReconnectPlayerHandsDrawBackExactMixamo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackExactReconnectReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackExactReconnect] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before exact Hands Draw Back reconnect.");
            }

            string unityHashBefore = HashFile(DrawBackSourcePath);
            string adjustedClipHashBefore = HashFile(DrawBackForwardAdjustedClipPath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            RequireHash(
                DrawBackOriginalPath,
                DrawBackSourceHash,
                "hands draw back original FBX");
            RequireHash(
                DrawBackSourcePath,
                DrawBackSourceHash,
                "hands draw back Unity FBX");
            AnimationClip drawClip = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            if (!string.Equals(drawClip.name, "mixamo.com", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Hands Draw Back FBX must expose the exact mixamo.com Take.");
            }

            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            string targetMeshPathBefore = AssetDatabase.GetAssetPath(
                RequirePrimaryPlayerSkinnedMeshRenderer(target).sharedMesh);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    drawClip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string unityHashAfter = HashFile(DrawBackSourcePath);
            string adjustedClipHashAfter = HashFile(DrawBackForwardAdjustedClipPath);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            string targetMeshPathAfter = AssetDatabase.GetAssetPath(
                RequirePrimaryPlayerSkinnedMeshRenderer(target).sharedMesh);
            TargetApplyMetrics targetMetrics = CreateTargetApplyMetrics(
                DrawBackTargetName,
                DrawBackStateName,
                drawClip.name,
                DrawBackSourcePath,
                drawClip,
                controller,
                animator);
            bool sourceExactAndUnchanged =
                string.Equals(
                    HashFile(DrawBackOriginalPath),
                    DrawBackSourceHash,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    unityHashBefore,
                    DrawBackSourceHash,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    unityHashAfter,
                    DrawBackSourceHash,
                    StringComparison.OrdinalIgnoreCase);
            bool adjustedClipUnchanged = string.Equals(
                adjustedClipHashBefore,
                adjustedClipHashAfter,
                StringComparison.OrdinalIgnoreCase);
            bool stowControllerUnchanged = string.Equals(
                stowControllerHashBefore,
                stowControllerHashAfter,
                StringComparison.OrdinalIgnoreCase);
            bool targetMeshUnchanged = string.Equals(
                targetMeshPathBefore,
                targetMeshPathAfter,
                StringComparison.Ordinal);
            bool controllerExact =
                controller.layers.Length == 1 &&
                targetMetrics.stateUsesExactClip &&
                targetMetrics.loopTime;
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName));
            bool animatorSettingsCorrect = AnimatorMatches(animator, controller);
            DrawBackExactReconnectApplyMetrics metrics =
                new DrawBackExactReconnectApplyMetrics
                {
                    target = DrawBackTargetName,
                    originalHash = HashFile(DrawBackOriginalPath),
                    unityHashBefore = unityHashBefore,
                    unityHashAfter = unityHashAfter,
                    adjustedClipHashBefore = adjustedClipHashBefore,
                    adjustedClipHashAfter = adjustedClipHashAfter,
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    targetMeshPathBefore = targetMeshPathBefore,
                    targetMeshPathAfter = targetMeshPathAfter,
                    controllerLayerCount = controller.layers.Length,
                    drawBack = targetMetrics,
                    sourceFbxExactAndUnchanged = sourceExactAndUnchanged,
                    adjustedClipUnchanged = adjustedClipUnchanged,
                    stowControllerUnchanged = stowControllerUnchanged,
                    targetMeshUnchanged = targetMeshUnchanged,
                    controllerUsesSingleEmbeddedTake = controllerExact,
                    rootUnchanged = rootUnchanged,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    animatorSettingsCorrect = animatorSettingsCorrect,
                    passedNumericChecks =
                        sourceExactAndUnchanged &&
                        adjustedClipUnchanged &&
                        stowControllerUnchanged &&
                        targetMeshUnchanged &&
                        controllerExact &&
                        rootUnchanged &&
                        otherAnimatorsUnchanged &&
                        animatorSettingsCorrect &&
                        !targetMetrics.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteJson(DrawBackExactReconnectApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Exact Hands Draw Back reconnect support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackExactReconnectReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackExactReconnect] Disconnected the adjusted animation and linked the exact embedded mixamo.com Take. " +
                "Duration=" + Num(drawClip.length) +
                "s, FrameRate=" + Num(drawClip.frameRate) +
                ", Layers=1, Loop=True, ExactFbxCopy=True, AdjustedClipDeleted=False, OtherAnimatorsUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Exact Mixamo Review")]
        internal static void CapturePlayerHandsDrawBackExactMixamoReview()
        {
            int stage = SessionState.GetInt(
                DrawBackExactReconnectReviewStageKey,
                0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Exact Hands Draw Back review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before exact Hands Draw Back review.");
                    }

                    SessionState.SetInt(
                        DrawBackExactReconnectReviewStageKey,
                        1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackExactReconnect] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Exact Hands Draw Back capture requires Play Mode.");
                    }

                    CapturePlayerHandsDrawBackExactMixamoActualReview();
                    SessionState.SetInt(
                        DrawBackExactReconnectReviewStageKey,
                        2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Exact Hands Draw Back review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(
                        DrawBackExactReconnectReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackExactReconnect] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Exact Hands Draw Back review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackExactReconnectReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Exact Mixamo Final")]
        internal static void CapturePlayerHandsDrawBackExactMixamoFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exact Hands Draw Back final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after exact Hands Draw Back direct review.");
            }

            DrawBackExactReconnectReviewMetrics metrics =
                ReadJson<DrawBackExactReconnectReviewMetrics>(
                    DrawBackExactReconnectReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                !metrics.drawBack.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Exact Hands Draw Back review did not pass before final capture.");
            }

            CopyReviewedContact(
                DrawBackExactReconnectReviewPath,
                DrawBackExactReconnectFinalPath);
            Debug.Log(
                "[PlayerHandsDrawBackExactReconnect] Final image copied once from directly reviewed Play Mode frames. " +
                "DrawBack=" + Path.GetFullPath(DrawBackExactReconnectFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw Back Common Mesh")]
        internal static void ApplyPlayerHandsDrawBackCommonMesh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackCommonMeshReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackCommonMesh] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the Hands Draw Back common-mesh apply.");
            }

            RequireHash(
                DrawBackOriginalPath,
                DrawBackSourceHash,
                "hands draw back original FBX");
            RequireHash(
                DrawBackSourcePath,
                DrawBackSourceHash,
                "hands draw back Unity FBX");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Transform emptyReference = RequireTarget(layout, EmptyTargetName);
            SkinnedMeshRenderer renderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(target);
            SkinnedMeshRenderer emptyRenderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(emptyReference);
            string rendererPath = AnimationUtility.CalculateTransformPath(
                renderer.transform,
                target);
            string emptyRendererPath = AnimationUtility.CalculateTransformPath(
                emptyRenderer.transform,
                emptyReference);
            if (!string.Equals(rendererPath, emptyRendererPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Hands_Draw_Back and Hands_Empty_Idle primary renderer paths differ.");
            }

            string correctedMeshPathBefore = AssetDatabase.GetAssetPath(
                renderer.sharedMesh);
            if (!string.Equals(
                    correctedMeshPathBefore,
                    DrawBackRightChestCorrectedMeshPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Hands_Draw_Back does not use the documented state-only corrected mesh before the common-mesh gate.");
            }

            float[] correctedWeightsBefore = CaptureBlendShapeWeights(renderer);
            string playerFbxHashBefore = HashFile(
                "Assets/_Project/Art/Player/player.fbx");
            string correctedMeshHashBefore = HashFile(
                DrawBackRightChestCorrectedMeshPath);
            string sourceUnityHashBefore = HashFile(DrawBackSourcePath);
            string drawControllerHashBefore = HashFile(DrawBackControllerPath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            Dictionary<string, string> otherRendererMeshesBefore =
                CapturePrimaryRendererMeshPathsExceptTarget(
                    layout,
                    DrawBackTargetName);

            RevertDrawBackRendererToPrefabSource(renderer);
            EditorUtility.SetDirty(renderer);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string sharedMeshPathAfter = AssetDatabase.GetAssetPath(
                renderer.sharedMesh);
            string emptySharedMeshPath = AssetDatabase.GetAssetPath(
                emptyRenderer.sharedMesh);
            bool meshOverrideRemoved =
                !HasPrefabPropertyOverride(renderer, "m_Mesh");
            bool blendShapeOverridesRemoved =
                !HasPrefabPropertyOverride(renderer, "m_BlendShapeWeights");
            bool rendererConfigurationMatchesEmpty =
                RendererConfigurationMatches(
                    renderer,
                    target,
                    emptyRenderer,
                    emptyReference);
            bool correctedMeshUnreferencedByScene =
                !SceneDependsOnAsset(DrawBackRightChestCorrectedMeshPath);
            string playerFbxHashAfter = HashFile(
                "Assets/_Project/Art/Player/player.fbx");
            string correctedMeshHashAfter = HashFile(
                DrawBackRightChestCorrectedMeshPath);
            string sourceUnityHashAfter = HashFile(DrawBackSourcePath);
            string drawControllerHashAfter = HashFile(DrawBackControllerPath);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            bool sourceAssetsUnchanged =
                string.Equals(
                    HashFile(DrawBackOriginalPath),
                    DrawBackSourceHash,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    sourceUnityHashBefore,
                    sourceUnityHashAfter,
                    StringComparison.Ordinal) &&
                string.Equals(
                    playerFbxHashBefore,
                    playerFbxHashAfter,
                    StringComparison.Ordinal) &&
                string.Equals(
                    correctedMeshHashBefore,
                    correctedMeshHashAfter,
                    StringComparison.Ordinal) &&
                string.Equals(
                    drawControllerHashBefore,
                    drawControllerHashAfter,
                    StringComparison.Ordinal) &&
                string.Equals(
                    stowControllerHashBefore,
                    stowControllerHashAfter,
                    StringComparison.Ordinal);
            bool otherRendererMeshesUnchanged = DictionariesEqual(
                otherRendererMeshesBefore,
                CapturePrimaryRendererMeshPathsExceptTarget(
                    layout,
                    DrawBackTargetName));
            DrawBackCommonMeshApplyMetrics metrics =
                new DrawBackCommonMeshApplyMetrics
                {
                    target = DrawBackTargetName,
                    emptyReference = EmptyTargetName,
                    rendererPath = rendererPath,
                    correctedMeshPathBefore = correctedMeshPathBefore,
                    sharedMeshPathAfter = sharedMeshPathAfter,
                    emptySharedMeshPath = emptySharedMeshPath,
                    correctedBlendShapeWeightsBefore = correctedWeightsBefore,
                    playerFbxHashBefore = playerFbxHashBefore,
                    playerFbxHashAfter = playerFbxHashAfter,
                    correctedMeshHashBefore = correctedMeshHashBefore,
                    correctedMeshHashAfter = correctedMeshHashAfter,
                    sourceOriginalHash = HashFile(DrawBackOriginalPath),
                    sourceUnityHashBefore = sourceUnityHashBefore,
                    sourceUnityHashAfter = sourceUnityHashAfter,
                    drawControllerHashBefore = drawControllerHashBefore,
                    drawControllerHashAfter = drawControllerHashAfter,
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    rendererPathsMatch = true,
                    meshOverrideRemoved = meshOverrideRemoved,
                    blendShapeOverridesRemoved = blendShapeOverridesRemoved,
                    rendererConfigurationMatchesEmpty =
                        rendererConfigurationMatchesEmpty,
                    correctedMeshUnreferencedByScene =
                        correctedMeshUnreferencedByScene,
                    sourceAssetsUnchanged = sourceAssetsUnchanged,
                    rootUnchanged = RootMatches(target, rootBefore),
                    otherAnimatorsUnchanged = DictionariesEqual(
                        otherAnimatorsBefore,
                        CaptureAnimatorsExceptTarget(
                            layout,
                            DrawBackTargetName)),
                    otherRendererMeshesUnchanged =
                        otherRendererMeshesUnchanged,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                string.Equals(
                    metrics.sharedMeshPathAfter,
                    metrics.emptySharedMeshPath,
                    StringComparison.Ordinal) &&
                metrics.rendererPathsMatch &&
                metrics.meshOverrideRemoved &&
                metrics.blendShapeOverridesRemoved &&
                metrics.rendererConfigurationMatchesEmpty &&
                metrics.correctedMeshUnreferencedByScene &&
                metrics.sourceAssetsUnchanged &&
                metrics.rootUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.otherRendererMeshesUnchanged;
            WriteJson(DrawBackCommonMeshApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackCommonMeshReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackCommonMesh] Reverted Draw Back to the shared player mesh inherited by Hands_Empty_Idle. " +
                "Renderer=" + rendererPath +
                ", SharedMesh=" + sharedMeshPathAfter +
                ", CorrectedMeshReferenced=False, AnimatorChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Common Mesh Review")]
        internal static void CapturePlayerHandsDrawBackCommonMeshReview()
        {
            int stage = SessionState.GetInt(
                DrawBackCommonMeshReviewStageKey,
                0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back common-mesh review must start in Edit Mode.");
                    }

                    DrawBackCommonMeshApplyMetrics apply =
                        ReadJson<DrawBackCommonMeshApplyMetrics>(
                            DrawBackCommonMeshApplyMetricsPath);
                    if (!apply.passedNumericChecks)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back common-mesh apply metrics did not pass.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before the common-mesh review.");
                    }

                    SessionState.SetInt(DrawBackCommonMeshReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackCommonMesh] Entering Play Mode for the direct common-mesh gate.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back common-mesh capture requires Play Mode.");
                    }

                    CapturePlayerHandsDrawBackCommonMeshActualReview();
                    SessionState.SetInt(DrawBackCommonMeshReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back common-mesh review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(DrawBackCommonMeshReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackCommonMesh] Exiting Play Mode after the direct common-mesh gate.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackCommonMeshReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw Back Common Mesh Forward")]
        internal static void ApplyPlayerHandsDrawBackCommonMeshForward()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackCommonMeshForwardReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackCommonMeshForward] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            DrawBackCommonMeshReviewMetrics commonMeshReview =
                ReadJson<DrawBackCommonMeshReviewMetrics>(
                    DrawBackCommonMeshReviewMetricsPath);
            if (!commonMeshReview.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "The Hands Draw Back common-mesh gate must pass before forward adjustment.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the common-mesh forward apply.");
            }

            RequireHash(
                DrawBackOriginalPath,
                DrawBackSourceHash,
                "hands draw back original FBX");
            RequireHash(
                DrawBackSourcePath,
                DrawBackSourceHash,
                "hands draw back Unity FBX");
            string sourceUnityHashBefore = HashFile(DrawBackSourcePath);
            string playerFbxHashBefore = HashFile(
                "Assets/_Project/Art/Player/player.fbx");
            string correctedMeshHashBefore = HashFile(
                DrawBackRightChestCorrectedMeshPath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Transform emptyReference = RequireTarget(layout, EmptyTargetName);
            SkinnedMeshRenderer renderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(target);
            SkinnedMeshRenderer emptyRenderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(emptyReference);
            if (!RendererConfigurationMatches(
                    renderer,
                    target,
                    emptyRenderer,
                    emptyReference) ||
                SceneDependsOnAsset(DrawBackRightChestCorrectedMeshPath))
            {
                throw new InvalidOperationException(
                    "Hands Draw Back no longer matches the approved common-mesh gate.");
            }

            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            DrawBackLowPalmLeftBakeResult bake =
                CreateOrUpdateDrawBackLowPalmLeftAdjustedClip(
                    target,
                    source,
                    true,
                    true,
                    true);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    bake.Clip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string sourceUnityHashAfter = HashFile(DrawBackSourcePath);
            string playerFbxHashAfter = HashFile(
                "Assets/_Project/Art/Player/player.fbx");
            string correctedMeshHashAfter = HashFile(
                DrawBackRightChestCorrectedMeshPath);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            bool durationAndRate =
                Mathf.Abs(source.length - bake.Clip.length) <= 0.0001f &&
                Mathf.Abs(source.frameRate - bake.Clip.frameRate) <= 0.0001f;
            bool sourceExact =
                HashMatches(
                    DrawBackOriginalPath,
                    DrawBackSourcePath,
                    DrawBackSourceHash) &&
                string.Equals(
                    sourceUnityHashBefore,
                    sourceUnityHashAfter,
                    StringComparison.Ordinal);
            bool nonRightArmUnchanged =
                AnimationMatchesExceptDrawBackRightArmRotations(
                    source,
                    bake.Clip);
            bool noBlendShapeCurves =
                HasNoBlendShapeCurves(bake.Clip);
            bool correctedMeshUnreferencedByScene =
                !SceneDependsOnAsset(DrawBackRightChestCorrectedMeshPath);
            bool controllerUsesAdjusted =
                controller.layers.Length == 1 &&
                LayerStateUsesClip(
                    controller.layers[0],
                    DrawBackStateName,
                    bake.Clip);
            bool loops =
                AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime;
            DrawBackCommonMeshForwardApplyMetrics metrics =
                new DrawBackCommonMeshForwardApplyMetrics
                {
                    target = DrawBackTargetName,
                    sourceOriginalHash = HashFile(DrawBackOriginalPath),
                    sourceUnityHashBefore = sourceUnityHashBefore,
                    sourceUnityHashAfter = sourceUnityHashAfter,
                    playerFbxHashBefore = playerFbxHashBefore,
                    playerFbxHashAfter = playerFbxHashAfter,
                    correctedMeshHashBefore = correctedMeshHashBefore,
                    correctedMeshHashAfter = correctedMeshHashAfter,
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    sourceDurationSeconds = source.length,
                    adjustedDurationSeconds = bake.Clip.length,
                    frameRate = bake.Clip.frameRate,
                    framesBaked = bake.FramesBaked,
                    sourcePeakFrame = bake.SourcePeakFrame,
                    adjustedPeakFrame = bake.AdjustedPeakFrame,
                    extractionStartFrame = bake.ExtractionStartFrame,
                    outerPathFrame = bake.OuterPathFrame,
                    adjustedPeakElbowFlexDegrees =
                        bake.AdjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakHorizontalForwardAngleDegrees =
                        bake.AdjustedPeakHorizontalForwardAngleDegrees,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        bake.AdjustedPeakPalmCharacterLeftAngleDegrees,
                    adjustedOuterElbowLateralMeters =
                        bake.AdjustedOuterElbowLateralMeters,
                    adjustedOuterHandLateralMeters =
                        bake.AdjustedOuterHandLateralMeters,
                    torsoOuterBoundaryLateralMeters =
                        bake.TorsoOuterBoundaryLateralMeters,
                    minimumFrontSilhouetteGapMeters =
                        bake.MinimumFrontSilhouetteGapMeters,
                    minimumFrontSilhouetteGapFrame =
                        bake.MinimumFrontSilhouetteGapFrame,
                    rightHandBindLocalRotation = rightHandBindLocalRotation,
                    durationAndFrameRatePreserved = durationAndRate,
                    sourceFbxExactAndUnchanged = sourceExact,
                    nonRightArmCurvesAndEventsUnchanged =
                        nonRightArmUnchanged,
                    hasOnlyApprovedRightArmReplacementCurves =
                        nonRightArmUnchanged,
                    hasNoBlendShapeCurves = noBlendShapeCurves,
                    correctedMeshAssetUnchanged = string.Equals(
                        correctedMeshHashBefore,
                        correctedMeshHashAfter,
                        StringComparison.Ordinal),
                    stowBackUnchanged = string.Equals(
                        stowControllerHashBefore,
                        stowControllerHashAfter,
                        StringComparison.Ordinal),
                    controllerUsesAdjustedClip = controllerUsesAdjusted,
                    adjustedClipLoops = loops,
                    rendererConfigurationMatchesEmpty =
                        RendererConfigurationMatches(
                            renderer,
                            target,
                            emptyRenderer,
                            emptyReference),
                    correctedMeshUnreferencedByScene =
                        correctedMeshUnreferencedByScene,
                    rootUnchanged = RootMatches(target, rootBefore),
                    otherAnimatorsUnchanged = DictionariesEqual(
                        otherAnimatorsBefore,
                        CaptureAnimatorsExceptTarget(
                            layout,
                            DrawBackTargetName)),
                    animatorSettingsCorrect =
                        AnimatorMatches(animator, controller),
                    applyRootMotion = animator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.durationAndFrameRatePreserved &&
                Mathf.RoundToInt(
                    metrics.sourceDurationSeconds * metrics.frameRate) == 69 &&
                metrics.framesBaked == 70 &&
                metrics.sourceFbxExactAndUnchanged &&
                metrics.nonRightArmCurvesAndEventsUnchanged &&
                metrics.hasOnlyApprovedRightArmReplacementCurves &&
                metrics.hasNoBlendShapeCurves &&
                metrics.correctedMeshAssetUnchanged &&
                metrics.stowBackUnchanged &&
                metrics.controllerUsesAdjustedClip &&
                metrics.adjustedClipLoops &&
                metrics.rendererConfigurationMatchesEmpty &&
                metrics.correctedMeshUnreferencedByScene &&
                metrics.minimumFrontSilhouetteGapMeters >= 0.005f &&
                metrics.adjustedPeakHorizontalForwardAngleDegrees >= 5f &&
                metrics.adjustedPeakHorizontalForwardAngleDegrees <= 45f &&
                metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                metrics.rootUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.animatorSettingsCorrect &&
                !metrics.applyRootMotion &&
                string.Equals(
                    metrics.playerFbxHashBefore,
                    metrics.playerFbxHashAfter,
                    StringComparison.Ordinal);
            WriteJson(DrawBackCommonMeshForwardApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh forward support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackCommonMeshForwardReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackCommonMeshForward] Rebuilt the adjusted clip from the exact source Take and replaced only RightArm/RightForeArm/RightHand rotations. " +
                "Frames=" + bake.ExtractionStartFrame + "/" +
                bake.OuterPathFrame + "/" + bake.SourcePeakFrame +
                ", Forward=" +
                Num(bake.AdjustedPeakHorizontalForwardAngleDegrees) +
                ", Height=" +
                Num(bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters) +
                ", Elbow=" + Num(bake.AdjustedPeakElbowFlexDegrees) +
                ", MinFaceGap=" +
                Num(bake.MinimumFrontSilhouetteGapMeters) + "@" +
                bake.MinimumFrontSilhouetteGapFrame +
                ", SharedMesh=True, CorrectedMeshReferenced=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Common Mesh Forward Review")]
        internal static void CapturePlayerHandsDrawBackCommonMeshForwardReview()
        {
            int stage = SessionState.GetInt(
                DrawBackCommonMeshForwardReviewStageKey,
                0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back common-mesh forward review must start in Edit Mode.");
                    }

                    DrawBackCommonMeshForwardApplyMetrics apply =
                        ReadJson<DrawBackCommonMeshForwardApplyMetrics>(
                            DrawBackCommonMeshForwardApplyMetricsPath);
                    if (!apply.passedNumericChecks)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back common-mesh forward apply metrics did not pass.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before the common-mesh forward review.");
                    }

                    SessionState.SetInt(
                        DrawBackCommonMeshForwardReviewStageKey,
                        1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackCommonMeshForward] Entering Play Mode for the 12-phase direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back common-mesh forward capture requires Play Mode.");
                    }

                    CapturePlayerHandsDrawBackCommonMeshForwardActualReview();
                    SessionState.SetInt(
                        DrawBackCommonMeshForwardReviewStageKey,
                        2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back common-mesh forward review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(
                        DrawBackCommonMeshForwardReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackCommonMeshForward] Exiting Play Mode after the 12-phase direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh forward review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackCommonMeshForwardReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Common Mesh Forward Final")]
        internal static void CapturePlayerHandsDrawBackCommonMeshForwardFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh forward final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after the common-mesh forward direct review.");
            }

            DrawBackCommonMeshForwardReviewMetrics metrics =
                ReadJson<DrawBackCommonMeshForwardReviewMetrics>(
                    DrawBackCommonMeshForwardReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                metrics.motion == null ||
                !metrics.motion.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh forward review did not pass before final capture.");
            }

            CopyReviewedContact(
                DrawBackCommonMeshForwardReviewPath,
                DrawBackCommonMeshForwardFinalPath);
            Debug.Log(
                "[PlayerHandsDrawBackCommonMeshForward] Final image copied once from the directly reviewed Play Mode contact sheet. " +
                "Path=" + Path.GetFullPath(DrawBackCommonMeshForwardFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Throw Source Diagnostic")]
        internal static void CapturePlayerHandsThrowSourceDiagnostic()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Throw source diagnostic requires Edit Mode.");
            }

            RequireHash(ThrowOriginalPath, ThrowSourceHash, "hands throw original FBX");
            RequireHash(ThrowSourcePath, ThrowSourceHash, "hands throw Unity FBX");
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the Hands Throw source diagnostic.");
            }

            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, ThrowReadyTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                ThrowSourcePath,
                "hands throw");
            GameObject sourceObject = UnityEngine.Object.Instantiate(target.gameObject);
            sourceObject.name = "HandsThrowExactSourceDiagnostic";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            List<byte[]> frontFrames = new List<byte[]>();
            List<byte[]> sideFrames = new List<byte[]>();
            List<float> rightHandHeights = new List<float>();
            try
            {
                int frameIntervals = Mathf.Max(
                    1,
                    Mathf.RoundToInt(source.length * source.frameRate));
                using (CaptureEnvironment environment =
                       new CaptureEnvironment(sourceObject.transform))
                {
                    for (int frame = 0; frame <= frameIntervals; frame++)
                    {
                        float time = Mathf.Min(
                            source.length,
                            frame / source.frameRate);
                        source.SampleAnimation(sourceObject, time);
                        Transform rightHand = FindRequired(
                            sourceObject.transform,
                            RightHandPath);
                        rightHandHeights.Add(Vector3.Dot(
                            rightHand.position - sourceObject.transform.position,
                            sourceObject.transform.up));
                        environment.ConfigureView(
                            sourceObject.transform,
                            1.05f,
                            1.35f);
                        frontFrames.Add(environment.CaptureFront());
                        sideFrames.Add(environment.CaptureSide());
                    }
                }

                float peakHeight = rightHandHeights.Max();
                int peakFrame = rightHandHeights.IndexOf(peakHeight);
                int candidateCount = rightHandHeights.Count(height =>
                    Mathf.Abs(height - peakHeight) <= PositionTolerance);
                ComposePairedFrameGrid(
                    frontFrames,
                    sideFrames,
                    10,
                    ThrowSourceDiagnosticPath);
                ThrowSourceDiagnosticMetrics metrics =
                    new ThrowSourceDiagnosticMetrics
                    {
                        sourceClipName = source.name,
                        sourceDurationSeconds = source.length,
                        frameRate = source.frameRate,
                        frameIntervals = frameIntervals,
                        framesCaptured = frontFrames.Count,
                        peakRightHandFrame = peakFrame,
                        peakRightHandTimeSeconds = peakFrame / source.frameRate,
                        peakRightHandHeightMeters = peakHeight,
                        peakCandidateCount = candidateCount,
                        uniquePeakCandidate = candidateCount == 1,
                        sourceOriginalHash = HashFile(ThrowOriginalPath),
                        sourceUnityHash = HashFile(ThrowSourcePath),
                        sourceCopyExact = HashMatches(
                            ThrowOriginalPath,
                            ThrowSourcePath,
                            ThrowSourceHash),
                        sceneUnchanged = !scene.isDirty,
                        validationPriority =
                            "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                    };
                metrics.passedNumericChecks =
                    metrics.sourceCopyExact &&
                    metrics.frameRate > 0f &&
                    metrics.framesCaptured == metrics.frameIntervals + 1 &&
                    metrics.sceneUnchanged;
                WriteJson(ThrowSourceDiagnosticMetricsPath, metrics);
                if (!metrics.passedNumericChecks)
                {
                    throw new InvalidOperationException(
                        "Hands Throw source diagnostic support checks failed. " +
                        JsonUtility.ToJson(metrics));
                }

                Debug.Log(
                    "[PlayerHandsThrow] Captured every exact source frame from the single embedded Take. " +
                    "Clip=" + source.name +
                    ", Frames=" + metrics.framesCaptured +
                    ", PeakCandidate=" + metrics.peakRightHandFrame +
                    "@" + Num(metrics.peakRightHandTimeSeconds) +
                    ", CandidateCount=" + metrics.peakCandidateCount +
                    ", SceneChanged=False.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
            }
        }

        [MenuItem("Bellerophon/Player/Apply Hands Throw Mixamo")]
        internal static void ApplyPlayerHandsThrowMixamo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Throw Mixamo apply requires Edit Mode.");
            }

            ThrowSourceDiagnosticMetrics diagnostic =
                ReadJson<ThrowSourceDiagnosticMetrics>(
                    ThrowSourceDiagnosticMetricsPath);
            if (!diagnostic.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw source diagnostic must pass before the Ready head-height apply.");
            }

            RequireHash(ThrowOriginalPath, ThrowSourceHash, "hands throw original FBX");
            RequireHash(ThrowSourcePath, ThrowSourceHash, "hands throw Unity FBX");
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the Hands Throw Mixamo apply.");
            }

            Transform layout = RequireLayout(scene);
            Transform readyTarget = RequireTarget(layout, ThrowReadyTargetName);
            Transform releaseTarget = RequireTarget(layout, ThrowReleaseTargetName);
            SkinnedMeshRenderer readyRenderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(readyTarget);
            RootPose readyRootBefore = new RootPose(readyTarget);
            RootPose releaseRootBefore = new RootPose(releaseTarget);
            string commonMeshPathBefore = AssetDatabase.GetAssetPath(
                readyRenderer.sharedMesh);
            string playerModelHashBefore = HashFile(PlayerModelPath);
            string baseClipHashBefore = HashFile(ThrowReadyBaseClipPath);
            string peakClipHashBefore = HashFile(ThrowReadyPeakClipPath);
            string releaseControllerHashBefore =
                HashFile(ThrowReleaseControllerPath);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTargets(
                    layout,
                    ThrowReadyTargetName,
                    ThrowReleaseTargetName);
            Dictionary<string, string> otherRendererMeshesBefore =
                CapturePrimaryRendererMeshPathsExceptTarget(
                    layout,
                    ThrowReadyTargetName);

            AnimationClip source = LoadSingleEmbeddedClip(
                ThrowSourcePath,
                "hands throw");
            AnimationClip baseClip = LoadClip(ThrowReadyBaseClipPath);
            MeasureThrowSourceHeadHeightFrame(
                readyTarget,
                source,
                out int sourceFrameIntervals,
                out int readyEndFrame,
                out float previousRightHandMinusHeadHeight,
                out float rightHandHeight,
                out float headHeight);

            const float holdDuration = 3f;
            const float breathingFrequency = 1f;
            const float breathingMaximumWeight = 30f;
            const float requestedChestExpansion = 0.01f;
            const float requestedBodyDrop = 0.03f;
            float readyEndTime = readyEndFrame / source.frameRate;
            ThrowBreathingMeshBuildResult meshBuild =
                CreateOrUpdateThrowReadyBreathingMesh(
                    readyTarget,
                    readyRenderer,
                    baseClip,
                    readyEndTime,
                    requestedChestExpansion,
                    breathingMaximumWeight);
            readyRenderer.sharedMesh = meshBuild.Mesh;
            readyRenderer.SetBlendShapeWeight(meshBuild.BlendShapeIndex, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(readyRenderer);
            AnimationClip readyClip = CreateOrUpdateThrowReadyClip(
                source,
                readyTarget,
                meshBuild.RendererPath,
                readyEndTime,
                holdDuration,
                breathingFrequency,
                breathingMaximumWeight,
                requestedBodyDrop,
                out ThrowBreathingMotionBuildResult motionBuild);
            MeasureThrowReadyPrefixAndHold(
                readyTarget,
                source,
                readyClip,
                readyEndFrame,
                0f,
                out float prefixPositionDifference,
                out float prefixRotationDifference,
                out _,
                out _);
            AnimatorController readyController =
                CreateOrUpdateExactEmbeddedTakeController(
                    ThrowReadyControllerPath,
                    ThrowReadyStateName,
                    readyClip);
            AnimatorController releaseController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ThrowReleaseControllerPath);
            if (releaseController == null)
            {
                throw new InvalidOperationException(
                    "Hands Throw Release controller is missing.");
            }

            Animator readyAnimator = RequireAnimator(readyTarget);
            Animator releaseAnimator = RequireAnimator(releaseTarget);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            string playerModelHashAfter = HashFile(PlayerModelPath);
            string baseClipHashAfter = HashFile(ThrowReadyBaseClipPath);
            string peakClipHashAfter = HashFile(ThrowReadyPeakClipPath);
            string releaseControllerHashAfter =
                HashFile(ThrowReleaseControllerPath);

            int sourceFloatCurveCount =
                AnimationUtility.GetCurveBindings(source).Length;
            int readyFloatCurveCount =
                AnimationUtility.GetCurveBindings(readyClip).Length;
            int sourceObjectCurveCount =
                AnimationUtility.GetObjectReferenceCurveBindings(source).Length;
            int readyObjectCurveCount =
                AnimationUtility.GetObjectReferenceCurveBindings(readyClip).Length;
            EditorCurveBinding breathingBinding =
                EditorCurveBinding.FloatCurve(
                    meshBuild.RendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + ThrowReadyBreathingBlendShapeName);
            bool breathingBlendShapeBound =
                AnimationUtility.GetEditorCurve(
                    readyClip,
                    breathingBinding) != null;
            bool readyPrefixPreserved =
                sourceObjectCurveCount == readyObjectCurveCount &&
                ThrowReadyEventsMatchSourcePrefix(
                    source,
                    readyClip,
                    readyEndTime) &&
                prefixPositionDifference <= PositionTolerance &&
                prefixRotationDifference <= RotationTolerance;
            string breathingMeshPathAfter = AssetDatabase.GetAssetPath(
                readyRenderer.sharedMesh);
            bool otherRendererMeshesUnchanged = DictionariesEqual(
                otherRendererMeshesBefore,
                CapturePrimaryRendererMeshPathsExceptTarget(
                    layout,
                    ThrowReadyTargetName));
            ThrowApplyMetrics metrics = new ThrowApplyMetrics
            {
                sourceClipName = source.name,
                sourceDurationSeconds = source.length,
                frameRate = source.frameRate,
                sourceFrameIntervals = sourceFrameIntervals,
                sourcePeakFrame = diagnostic.peakRightHandFrame,
                readyEndFrame = readyEndFrame,
                readyEndTimeSeconds = readyEndTime,
                previousRightHandMinusHeadHeightMeters =
                    previousRightHandMinusHeadHeight,
                rightHandHeightMeters = rightHandHeight,
                headHeightMeters = headHeight,
                rightHandMinusHeadHeightMeters = rightHandHeight - headHeight,
                holdDurationSeconds = holdDuration,
                breathingFrequencyHertz = breathingFrequency,
                breathingCycleCount = motionBuild.BreathingCycleCount,
                breathingMaximumWeight = breathingMaximumWeight,
                requestedChestExpansionMeters = requestedChestExpansion,
                measuredChestExpansionMeters =
                    meshBuild.MaximumExpansionAtThirtyPercentMeters,
                requestedBodyDropMeters = requestedBodyDrop,
                measuredBodyDropMeters = motionBuild.MaximumBodyDropMeters,
                maximumFootDisplacementMeters =
                    motionBuild.MaximumFootDisplacementMeters,
                minimumKneeFlexIncreaseDegrees =
                    motionBuild.MinimumKneeFlexIncreaseDegrees,
                readyDurationSeconds = readyClip.length,
                releaseDurationSeconds = source.length,
                rendererPath = meshBuild.RendererPath,
                commonMeshPathBefore = commonMeshPathBefore,
                breathingMeshPathAfter = breathingMeshPathAfter,
                breathingBlendShapeName = ThrowReadyBreathingBlendShapeName,
                breathingBlendShapeIndex = meshBuild.BlendShapeIndex,
                breathingAffectedVertexCount = meshBuild.AffectedVertexCount,
                breathingFrontVertexCount = meshBuild.FrontVertexCount,
                breathingLeftSideVertexCount = meshBuild.LeftSideVertexCount,
                breathingRightSideVertexCount = meshBuild.RightSideVertexCount,
                sourceFloatCurveCount = sourceFloatCurveCount,
                readyFloatCurveCount = readyFloatCurveCount,
                sourceObjectCurveCount = sourceObjectCurveCount,
                readyObjectCurveCount = readyObjectCurveCount,
                readyPrefixPositionDifferenceMax = prefixPositionDifference,
                readyPrefixRotationDifferenceDegreesMax = prefixRotationDifference,
                sourceOriginalHash = HashFile(ThrowOriginalPath),
                sourceUnityHash = HashFile(ThrowSourcePath),
                playerModelHashBefore = playerModelHashBefore,
                playerModelHashAfter = playerModelHashAfter,
                baseClipHashBefore = baseClipHashBefore,
                baseClipHashAfter = baseClipHashAfter,
                peakClipHashBefore = peakClipHashBefore,
                peakClipHashAfter = peakClipHashAfter,
                releaseControllerHashBefore = releaseControllerHashBefore,
                releaseControllerHashAfter = releaseControllerHashAfter,
                sourceCopyExact = HashMatches(
                    ThrowOriginalPath,
                    ThrowSourcePath,
                    ThrowSourceHash),
                firstHeadHeightFrame =
                    previousRightHandMinusHeadHeight < 0f &&
                    rightHandHeight - headHeight >= 0f,
                readyEndBeforeSourcePeak =
                    readyEndFrame < diagnostic.peakRightHandFrame,
                readySourcePrefixPreserved = readyPrefixPreserved,
                breathingBlendShapeBound = breathingBlendShapeBound,
                breathingMeshAppliedOnlyToReady =
                    string.Equals(
                        breathingMeshPathAfter,
                        ThrowReadyBreathingMeshPath,
                        StringComparison.Ordinal) &&
                    otherRendererMeshesUnchanged,
                otherRendererMeshesUnchanged = otherRendererMeshesUnchanged,
                releaseUsesExactEmbeddedTake =
                    StateUsesClip(
                        releaseController,
                        ThrowReleaseStateName,
                        source),
                releaseControllerUnchanged = string.Equals(
                    releaseControllerHashBefore,
                    releaseControllerHashAfter,
                    StringComparison.Ordinal),
                sourceAssetsUnchanged =
                    string.Equals(
                        playerModelHashBefore,
                        playerModelHashAfter,
                        StringComparison.Ordinal) &&
                    HashMatches(
                        ThrowOriginalPath,
                        ThrowSourcePath,
                        ThrowSourceHash),
                previousReadyClipsUnchanged =
                    string.Equals(
                        baseClipHashBefore,
                        baseClipHashAfter,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        peakClipHashBefore,
                        peakClipHashAfter,
                        StringComparison.Ordinal),
                readyControllerUsesClip =
                    StateUsesClip(
                        readyController,
                        ThrowReadyStateName,
                        readyClip),
                releaseControllerUsesClip =
                    StateUsesClip(
                        releaseController,
                        ThrowReleaseStateName,
                        source),
                readyLoops =
                    AnimationUtility.GetAnimationClipSettings(readyClip).loopTime,
                releaseLoops =
                    AnimationUtility.GetAnimationClipSettings(source).loopTime,
                readyRootUnchanged = RootMatches(readyTarget, readyRootBefore),
                releaseRootUnchanged = RootMatches(releaseTarget, releaseRootBefore),
                otherAnimatorsUnchanged = DictionariesEqual(
                    otherAnimatorsBefore,
                    CaptureAnimatorsExceptTargets(
                        layout,
                        ThrowReadyTargetName,
                        ThrowReleaseTargetName)),
                readyAnimatorSettingsCorrect =
                    AnimatorMatches(readyAnimator, readyController),
                releaseAnimatorSettingsCorrect =
                    AnimatorMatches(releaseAnimator, releaseController),
                readyApplyRootMotion = readyAnimator.applyRootMotion,
                releaseApplyRootMotion = releaseAnimator.applyRootMotion,
                sceneSavedClean = !scene.isDirty,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            metrics.passedNumericChecks =
                metrics.sourceCopyExact &&
                metrics.firstHeadHeightFrame &&
                metrics.readyEndBeforeSourcePeak &&
                Mathf.Abs(metrics.holdDurationSeconds - 3f) <= 0.00001f &&
                Mathf.Abs(metrics.breathingFrequencyHertz - 1f) <= 0.00001f &&
                metrics.breathingCycleCount == 3 &&
                Mathf.Abs(metrics.breathingMaximumWeight - 30f) <= 0.0001f &&
                Mathf.Abs(
                    metrics.measuredChestExpansionMeters - 0.01f) <= 0.0005f &&
                Mathf.Abs(
                    metrics.measuredBodyDropMeters - 0.03f) <= 0.0005f &&
                metrics.maximumFootDisplacementMeters <= 0.0005f &&
                metrics.minimumKneeFlexIncreaseDegrees > 0.1f &&
                metrics.breathingAffectedVertexCount > 0 &&
                metrics.breathingFrontVertexCount > 0 &&
                metrics.breathingLeftSideVertexCount > 0 &&
                metrics.breathingRightSideVertexCount > 0 &&
                Mathf.Abs(
                    metrics.readyDurationSeconds -
                    (metrics.readyEndTimeSeconds + 3f)) <= 0.0001f &&
                Mathf.Abs(
                    metrics.releaseDurationSeconds -
                    metrics.sourceDurationSeconds) <= 0.0001f &&
                metrics.readySourcePrefixPreserved &&
                metrics.breathingBlendShapeBound &&
                metrics.breathingMeshAppliedOnlyToReady &&
                metrics.otherRendererMeshesUnchanged &&
                metrics.releaseUsesExactEmbeddedTake &&
                metrics.releaseControllerUnchanged &&
                metrics.sourceAssetsUnchanged &&
                metrics.previousReadyClipsUnchanged &&
                metrics.readyControllerUsesClip &&
                metrics.releaseControllerUsesClip &&
                metrics.readyLoops &&
                metrics.releaseLoops &&
                metrics.readyRootUnchanged &&
                metrics.releaseRootUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.readyAnimatorSettingsCorrect &&
                metrics.releaseAnimatorSettingsCorrect &&
                !metrics.readyApplyRootMotion &&
                !metrics.releaseApplyRootMotion &&
                metrics.sceneSavedClean;
            WriteJson(ThrowApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw Mixamo apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(ThrowReviewStageKey);
            Debug.Log(
                "[PlayerHandsThrow] Applied Ready head-height breathing hold. " +
                "ReadyEnd=" + metrics.readyEndFrame +
                "@" + Num(metrics.readyEndTimeSeconds) +
                ", Hold=" + Num(metrics.holdDurationSeconds) +
                ", Cycles=" + metrics.breathingCycleCount +
                ", Chest=" + Num(metrics.measuredChestExpansionMeters) +
                ", BodyDrop=" + Num(metrics.measuredBodyDropMeters) +
                ", Feet=" + Num(metrics.maximumFootDisplacementMeters) +
                ", ReadyLength=" + Num(metrics.readyDurationSeconds) +
                ", ReleaseUnchanged=" + metrics.releaseControllerUnchanged +
                ", SourceHashExact=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Throw Mixamo Review")]
        internal static void CapturePlayerHandsThrowMixamoReview()
        {
            int stage = SessionState.GetInt(ThrowReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Throw Mixamo review must start in Edit Mode.");
                    }

                    ThrowApplyMetrics apply = ReadJson<ThrowApplyMetrics>(
                        ThrowApplyMetricsPath);
                    if (!apply.passedNumericChecks)
                    {
                        throw new InvalidOperationException(
                            "Hands Throw Mixamo apply metrics did not pass.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before the Hands Throw review.");
                    }

                    SessionState.SetInt(ThrowReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsThrow] Entering Play Mode for direct Ready and Release review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Throw Mixamo capture requires Play Mode.");
                    }

                    CapturePlayerHandsThrowMixamoActualReview();
                    SessionState.SetInt(ThrowReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Throw Mixamo review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(ThrowReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsThrow] Exiting Play Mode after direct Ready and Release review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Throw Mixamo review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(ThrowReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Throw Mixamo Final")]
        internal static void CapturePlayerHandsThrowMixamoFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Throw Mixamo final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after the Hands Throw direct review.");
            }

            ThrowReviewMetrics metrics = ReadJson<ThrowReviewMetrics>(
                ThrowReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                metrics.ready == null ||
                metrics.release == null ||
                !metrics.ready.passedNumericChecks ||
                !metrics.release.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw direct review did not pass before final capture.");
            }

            CopyReviewedContact(ThrowReviewPath, ThrowFinalPath);
            Debug.Log(
                "[PlayerHandsThrow] Final image copied once from the directly reviewed Play Mode contact sheet. " +
                "Path=" + Path.GetFullPath(ThrowFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Throw Cancel")]
        internal static void ApplyPlayerHandsThrowCancel()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel apply requires Edit Mode.");
            }

            ThrowApplyMetrics readyApply = ReadJson<ThrowApplyMetrics>(
                ThrowApplyMetricsPath);
            if (!readyApply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw Ready apply metrics must pass before Cancel is copied.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the Hands Throw Cancel apply.");
            }

            Transform layout = RequireLayout(scene);
            Transform cancelTarget = RequireTarget(layout, ThrowCancelTargetName);
            RootPose rootBefore = new RootPose(cancelTarget);
            SkinnedMeshRenderer cancelRenderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(cancelTarget);
            string targetMeshPathBefore = AssetDatabase.GetAssetPath(
                cancelRenderer.sharedMesh);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, ThrowCancelTargetName);
            Dictionary<string, string> otherRendererMeshesBefore =
                CapturePrimaryRendererMeshPathsExceptTarget(
                    layout,
                    ThrowCancelTargetName);
            string readyClipHashBefore = HashFile(ThrowReadyClipPath);
            string idleClipHashBefore = HashFile(IdleClipPath);
            string readyControllerHashBefore = HashFile(ThrowReadyControllerPath);
            string releaseControllerHashBefore = HashFile(
                ThrowReleaseControllerPath);
            AnimationClip readyClip = LoadClip(ThrowReadyClipPath);
            AnimationClip idleClip = LoadClip(IdleClipPath);
            const float initialHoldDuration = 0.5f;
            const float finalIdleHoldDuration = 0.5f;
            float readyEndTime = readyApply.readyEndTimeSeconds;
            AnimationClip cancelClip = CreateOrUpdateThrowCancelClip(
                cancelTarget,
                readyClip,
                idleClip,
                readyEndTime,
                initialHoldDuration,
                finalIdleHoldDuration);
            MeasureThrowCancelClipExact(
                cancelTarget,
                readyClip,
                idleClip,
                cancelClip,
                readyEndTime,
                initialHoldDuration,
                finalIdleHoldDuration,
                out float holdPositionDifference,
                out float holdRotationDifference,
                out float reversePositionDifference,
                out float reverseRotationDifference,
                out float finalIdlePositionDifference,
                out float finalIdleRotationDifference,
                out float finalHoldPositionDifference,
                out float finalHoldRotationDifference);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    ThrowCancelControllerPath,
                    ThrowCancelStateName,
                    cancelClip);
            Animator animator = ConfigureAnimator(cancelTarget, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string readyClipHashAfter = HashFile(ThrowReadyClipPath);
            string idleClipHashAfter = HashFile(IdleClipPath);
            string readyControllerHashAfter = HashFile(ThrowReadyControllerPath);
            string releaseControllerHashAfter = HashFile(
                ThrowReleaseControllerPath);
            string targetMeshPathAfter = AssetDatabase.GetAssetPath(
                cancelRenderer.sharedMesh);
            bool otherRendererMeshesUnchanged = DictionariesEqual(
                otherRendererMeshesBefore,
                CapturePrimaryRendererMeshPathsExceptTarget(
                    layout,
                    ThrowCancelTargetName));
            ThrowCancelApplyMetrics metrics = new ThrowCancelApplyMetrics
            {
                target = ThrowCancelTargetName,
                state = ThrowCancelStateName,
                sourceReadyClipPath = ThrowReadyClipPath,
                idleClipPath = IdleClipPath,
                cancelClipPath = ThrowCancelClipPath,
                readyEndFrame = readyApply.readyEndFrame,
                frameRate = cancelClip.frameRate,
                readyEndTimeSeconds = readyEndTime,
                initialHoldDurationSeconds = initialHoldDuration,
                reverseDurationSeconds = readyEndTime,
                finalIdleHoldDurationSeconds = finalIdleHoldDuration,
                totalDurationSeconds = cancelClip.length,
                floatCurveCount =
                    AnimationUtility.GetCurveBindings(cancelClip).Length,
                objectCurveCount = AnimationUtility
                    .GetObjectReferenceCurveBindings(cancelClip).Length,
                eventCount = AnimationUtility.GetAnimationEvents(cancelClip).Length,
                holdPositionDifferenceMax = holdPositionDifference,
                holdRotationDifferenceDegreesMax = holdRotationDifference,
                reversePositionDifferenceMax = reversePositionDifference,
                reverseRotationDifferenceDegreesMax = reverseRotationDifference,
                finalIdlePositionDifferenceMax = finalIdlePositionDifference,
                finalIdleRotationDifferenceDegreesMax =
                    finalIdleRotationDifference,
                finalHoldPositionDifferenceMax = finalHoldPositionDifference,
                finalHoldRotationDifferenceDegreesMax =
                    finalHoldRotationDifference,
                idleClipHashBefore = idleClipHashBefore,
                idleClipHashAfter = idleClipHashAfter,
                readyClipHashBefore = readyClipHashBefore,
                readyClipHashAfter = readyClipHashAfter,
                readyControllerHashBefore = readyControllerHashBefore,
                readyControllerHashAfter = readyControllerHashAfter,
                releaseControllerHashBefore = releaseControllerHashBefore,
                releaseControllerHashAfter = releaseControllerHashAfter,
                targetMeshPathBefore = targetMeshPathBefore,
                targetMeshPathAfter = targetMeshPathAfter,
                hasNoBlendShapeCurves = HasNoBlendShapeCurves(cancelClip),
                controllerUsesCancelClip = StateUsesClip(
                    controller,
                    ThrowCancelStateName,
                    cancelClip),
                clipLoops = AnimationUtility
                    .GetAnimationClipSettings(cancelClip).loopTime,
                rootUnchanged = RootMatches(cancelTarget, rootBefore),
                targetMeshUnchanged = string.Equals(
                    targetMeshPathBefore,
                    targetMeshPathAfter,
                    StringComparison.Ordinal),
                otherAnimatorsUnchanged = DictionariesEqual(
                    otherAnimatorsBefore,
                    CaptureAnimatorsExceptTarget(
                        layout,
                        ThrowCancelTargetName)),
                otherRendererMeshesUnchanged = otherRendererMeshesUnchanged,
                animatorSettingsCorrect = AnimatorMatches(animator, controller),
                applyRootMotion = animator.applyRootMotion,
                sceneSavedClean = !scene.isDirty,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            metrics.passedNumericChecks =
                metrics.readyEndFrame == 19 &&
                Mathf.Abs(metrics.frameRate - 30f) <= 0.0001f &&
                Mathf.Abs(metrics.initialHoldDurationSeconds - 0.5f) <= 0.00001f &&
                Mathf.Abs(metrics.finalIdleHoldDurationSeconds - 0.5f) <= 0.00001f &&
                Mathf.Abs(
                    metrics.totalDurationSeconds -
                    (metrics.initialHoldDurationSeconds +
                     metrics.reverseDurationSeconds +
                     metrics.finalIdleHoldDurationSeconds)) <= 0.0001f &&
                metrics.holdPositionDifferenceMax <= PositionTolerance &&
                metrics.holdRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.reversePositionDifferenceMax <= PositionTolerance &&
                metrics.reverseRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.finalIdlePositionDifferenceMax <= PositionTolerance &&
                metrics.finalIdleRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.finalHoldPositionDifferenceMax <= PositionTolerance &&
                metrics.finalHoldRotationDifferenceDegreesMax <= RotationTolerance &&
                string.Equals(
                    metrics.idleClipHashBefore,
                    metrics.idleClipHashAfter,
                    StringComparison.Ordinal) &&
                string.Equals(
                    metrics.readyClipHashBefore,
                    metrics.readyClipHashAfter,
                    StringComparison.Ordinal) &&
                string.Equals(
                    metrics.readyControllerHashBefore,
                    metrics.readyControllerHashAfter,
                    StringComparison.Ordinal) &&
                string.Equals(
                    metrics.releaseControllerHashBefore,
                    metrics.releaseControllerHashAfter,
                    StringComparison.Ordinal) &&
                metrics.hasNoBlendShapeCurves &&
                metrics.controllerUsesCancelClip &&
                metrics.clipLoops &&
                metrics.rootUnchanged &&
                metrics.targetMeshUnchanged &&
                metrics.otherAnimatorsUnchanged &&
                metrics.otherRendererMeshesUnchanged &&
                metrics.animatorSettingsCorrect &&
                !metrics.applyRootMotion &&
                metrics.sceneSavedClean;
            WriteJson(ThrowCancelApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(ThrowCancelReviewStageKey);
            Debug.Log(
                "[PlayerHandsThrowCancel] Applied Ready frame 19 hold, linear Idle blend, and final Idle hold loop. " +
                "Hold=" + Num(metrics.initialHoldDurationSeconds) +
                ", Reverse=" + Num(metrics.reverseDurationSeconds) +
                ", IdleHold=" + Num(metrics.finalIdleHoldDurationSeconds) +
                ", Length=" + Num(metrics.totalDurationSeconds) +
                ", HoldPose=" + Num(metrics.holdPositionDifferenceMax) +
                "/" + Num(metrics.holdRotationDifferenceDegreesMax) +
                ", ReversePose=" + Num(metrics.reversePositionDifferenceMax) +
                "/" + Num(metrics.reverseRotationDifferenceDegreesMax) +
                ", FinalIdle=" + Num(metrics.finalIdlePositionDifferenceMax) +
                "/" + Num(metrics.finalIdleRotationDifferenceDegreesMax) +
                ", Breathing=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Throw Cancel Review")]
        internal static void CapturePlayerHandsThrowCancelReview()
        {
            int stage = SessionState.GetInt(ThrowCancelReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Throw Cancel review must start in Edit Mode.");
                    }

                    ThrowCancelApplyMetrics apply =
                        ReadJson<ThrowCancelApplyMetrics>(
                            ThrowCancelApplyMetricsPath);
                    if (!apply.passedNumericChecks)
                    {
                        throw new InvalidOperationException(
                            "Hands Throw Cancel apply metrics did not pass.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before the Hands Throw Cancel review.");
                    }

                    SessionState.SetInt(ThrowCancelReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsThrowCancel] Entering Play Mode for direct reverse-loop review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Throw Cancel capture requires Play Mode.");
                    }

                    CapturePlayerHandsThrowCancelActualReview();
                    SessionState.SetInt(ThrowCancelReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Throw Cancel review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(ThrowCancelReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsThrowCancel] Exiting Play Mode after direct reverse-loop review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Throw Cancel review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(ThrowCancelReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Throw Cancel Final")]
        internal static void CapturePlayerHandsThrowCancelFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after the Hands Throw Cancel direct review.");
            }

            ThrowCancelReviewMetrics metrics =
                ReadJson<ThrowCancelReviewMetrics>(
                    ThrowCancelReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                metrics.runtime == null ||
                !metrics.runtime.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel direct review did not pass before final capture.");
            }

            CopyReviewedContact(ThrowCancelReviewPath, ThrowCancelFinalPath);
            Debug.Log(
                "[PlayerHandsThrowCancel] Final image copied once from the directly reviewed Play Mode contact sheet. " +
                "Path=" + Path.GetFullPath(ThrowCancelFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw Back Forward Angle")]
        internal static void ApplyHandsDrawBackForwardAngle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackForwardReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackForward] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands Draw Back forward-angle apply.");
            }

            RequireHash(DrawBackOriginalPath, DrawBackSourceHash, "hands draw back original");
            RequireHash(DrawBackSourcePath, DrawBackSourceHash, "hands draw back Unity copy");
            string sourceHashBefore = HashFile(DrawBackSourcePath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            DrawBackForwardBakeResult bake =
                CreateOrUpdateDrawBackForwardAdjustedClip(target, source);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    bake.Clip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            bool durationAndRate =
                Mathf.Abs(source.length - bake.Clip.length) <= 0.0001f &&
                Mathf.Abs(source.frameRate - bake.Clip.frameRate) <= 0.0001f;
            bool timingPeakPreserved =
                bake.SourcePeakFrame == bake.AdjustedPeakFrame;
            bool sourceExact =
                HashMatches(
                    DrawBackOriginalPath,
                    DrawBackSourcePath,
                    DrawBackSourceHash) &&
                string.Equals(
                    sourceHashBefore,
                    HashFile(DrawBackSourcePath),
                    StringComparison.Ordinal);
            bool nonRightArmUnchanged =
                AnimationMatchesExceptDrawBackRightArmRotations(
                    source,
                    bake.Clip);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            bool stowUnchanged = string.Equals(
                stowControllerHashBefore,
                stowControllerHashAfter,
                StringComparison.Ordinal);
            bool controllerUsesAdjusted =
                controller.layers.Length == 1 &&
                LayerStateUsesClip(
                    controller.layers[0],
                    DrawBackStateName,
                    bake.Clip);
            bool loops =
                AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime;
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName));
            bool animatorSettingsCorrect = AnimatorMatches(animator, controller);
            int sourceCurveCount = AnimationUtility.GetCurveBindings(source).Length;
            int adjustedCurveCount = AnimationUtility.GetCurveBindings(bake.Clip).Length;
            int sourceEventCount = AnimationUtility.GetAnimationEvents(source).Length;
            int adjustedEventCount = AnimationUtility.GetAnimationEvents(bake.Clip).Length;
            bool peakDirectionCorrect =
                bake.AdjustedPeakForwardAngleDegrees <= 0.25f &&
                bake.SourcePeakForwardAngleDegrees -
                    bake.AdjustedPeakForwardAngleDegrees >= 0.5f;
            bool elbowNaturalAndPreserved =
                bake.AdjustedPeakElbowFlexDegrees >= 5f &&
                Mathf.Abs(
                    bake.SourcePeakElbowFlexDegrees -
                    bake.AdjustedPeakElbowFlexDegrees) <= 0.1f;
            DrawBackForwardApplyMetrics metrics =
                new DrawBackForwardApplyMetrics
                {
                    target = DrawBackTargetName,
                    sourceOriginalHash = HashFile(DrawBackOriginalPath),
                    sourceUnityHashBefore = sourceHashBefore,
                    sourceUnityHashAfter = HashFile(DrawBackSourcePath),
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    sourceDurationSeconds = source.length,
                    adjustedDurationSeconds = bake.Clip.length,
                    frameRate = bake.Clip.frameRate,
                    framesBaked = bake.FramesBaked,
                    sourceCurveCount = sourceCurveCount,
                    adjustedCurveCount = adjustedCurveCount,
                    sourceEventCount = sourceEventCount,
                    adjustedEventCount = adjustedEventCount,
                    sourcePeakFrame = bake.SourcePeakFrame,
                    adjustedPeakFrame = bake.AdjustedPeakFrame,
                    sourcePeakShoulderToHandForwardAngleDegrees =
                        bake.SourcePeakForwardAngleDegrees,
                    adjustedPeakShoulderToHandForwardAngleDegrees =
                        bake.AdjustedPeakForwardAngleDegrees,
                    sourcePeakElbowFlexDegrees =
                        bake.SourcePeakElbowFlexDegrees,
                    adjustedPeakElbowFlexDegrees =
                        bake.AdjustedPeakElbowFlexDegrees,
                    rightHandWorldRotationDifferenceDegreesMax =
                        bake.HandWorldRotationDifferenceDegreesMax,
                    shoulderToHandReachDifferenceMetersMax =
                        bake.ReachDifferenceMetersMax,
                    targetReachErrorMetersMax =
                        bake.TargetReachErrorMetersMax,
                    rightHandBindLocalRotation = rightHandBindLocalRotation,
                    durationAndFrameRatePreserved = durationAndRate,
                    timingPeakFramePreserved = timingPeakPreserved,
                    sourceFbxExactAndUnchanged = sourceExact,
                    nonRightArmCurvesAndEventsUnchanged = nonRightArmUnchanged,
                    stowBackUnchanged = stowUnchanged,
                    controllerUsesAdjustedClip = controllerUsesAdjusted,
                    adjustedClipLoops = loops,
                    rootUnchanged = rootUnchanged,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    animatorSettingsCorrect = animatorSettingsCorrect,
                    applyRootMotion = animator.applyRootMotion,
                    passedNumericChecks =
                        durationAndRate &&
                        timingPeakPreserved &&
                        sourceExact &&
                        nonRightArmUnchanged &&
                        stowUnchanged &&
                        controllerUsesAdjusted &&
                        loops &&
                        rootUnchanged &&
                        otherAnimatorsUnchanged &&
                        animatorSettingsCorrect &&
                        sourceEventCount == adjustedEventCount &&
                        peakDirectionCorrect &&
                        elbowNaturalAndPreserved &&
                        bake.HandWorldRotationDifferenceDegreesMax <= RotationTolerance &&
                        bake.ReachDifferenceMetersMax <= 0.001f &&
                        bake.TargetReachErrorMetersMax <= 0.001f &&
                        !animator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteJson(DrawBackForwardApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back forward-angle apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackForwardReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackForward] Applied forward-angle correction with source timing and hand direction preserved. " +
                "PeakFrame=" + bake.SourcePeakFrame +
                ", ForwardAngle=" + Num(bake.SourcePeakForwardAngleDegrees) +
                "->" + Num(bake.AdjustedPeakForwardAngleDegrees) +
                ", ElbowFlex=" + Num(bake.SourcePeakElbowFlexDegrees) +
                "->" + Num(bake.AdjustedPeakElbowFlexDegrees) +
                ", HandRotation=" + Num(bake.HandWorldRotationDifferenceDegreesMax) +
                ", SourceHashUnchanged=True, StowUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Forward Angle Review")]
        internal static void CaptureHandsDrawBackForwardAngleReview()
        {
            int stage = SessionState.GetInt(DrawBackForwardReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back forward-angle review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands Draw Back forward-angle review.");
                    }

                    SessionState.SetInt(DrawBackForwardReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackForward] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back forward-angle capture requires Play Mode.");
                    }

                    CaptureHandsDrawBackForwardAngleActualReview();
                    SessionState.SetInt(DrawBackForwardReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back forward-angle review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(DrawBackForwardReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackForward] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw Back forward-angle review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackForwardReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Forward Angle Final")]
        internal static void CaptureHandsDrawBackForwardAngleFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back forward-angle final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after Hands Draw Back forward-angle direct review.");
            }

            DrawBackForwardReviewMetrics metrics =
                ReadJson<DrawBackForwardReviewMetrics>(
                    DrawBackForwardReviewMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back forward-angle review did not pass before final capture.");
            }

            CopyReviewedContact(
                DrawBackForwardReviewPath,
                DrawBackForwardFinalPath);
            Debug.Log(
                "[PlayerHandsDrawBackForward] Final image copied once from directly reviewed Play Mode frames. " +
                "Path=" + Path.GetFullPath(DrawBackForwardFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw Back Low Palm Left Pose")]
        internal static void ApplyHandsDrawBackLowPalmLeftPose()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackLowPalmLeftReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackLowPalmLeft] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands Draw Back low palm-left apply.");
            }

            RequireHash(DrawBackOriginalPath, DrawBackSourceHash, "hands draw back original");
            RequireHash(DrawBackSourcePath, DrawBackSourceHash, "hands draw back Unity copy");
            string sourceHashBefore = HashFile(DrawBackSourcePath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            DrawBackLowPalmLeftBakeResult bake =
                CreateOrUpdateDrawBackLowPalmLeftAdjustedClip(target, source);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    bake.Clip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            bool durationAndRate =
                Mathf.Abs(source.length - bake.Clip.length) <= 0.0001f &&
                Mathf.Abs(source.frameRate - bake.Clip.frameRate) <= 0.0001f;
            bool timingPeakPreserved =
                bake.SourcePeakFrame == bake.AdjustedPeakFrame;
            bool sourceExact =
                HashMatches(
                    DrawBackOriginalPath,
                    DrawBackSourcePath,
                    DrawBackSourceHash) &&
                string.Equals(
                    sourceHashBefore,
                    HashFile(DrawBackSourcePath),
                    StringComparison.Ordinal);
            bool nonRightArmUnchanged =
                AnimationMatchesExceptDrawBackRightArmRotations(
                    source,
                    bake.Clip);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            bool stowUnchanged = string.Equals(
                stowControllerHashBefore,
                stowControllerHashAfter,
                StringComparison.Ordinal);
            bool controllerUsesAdjusted =
                controller.layers.Length == 1 &&
                LayerStateUsesClip(
                    controller.layers[0],
                    DrawBackStateName,
                    bake.Clip);
            bool loops =
                AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime;
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName));
            bool animatorSettingsCorrect = AnimatorMatches(animator, controller);
            int sourceCurveCount = AnimationUtility.GetCurveBindings(source).Length;
            int adjustedCurveCount = AnimationUtility.GetCurveBindings(bake.Clip).Length;
            int sourceEventCount = AnimationUtility.GetAnimationEvents(source).Length;
            int adjustedEventCount = AnimationUtility.GetAnimationEvents(bake.Clip).Length;
            DrawBackLowPalmLeftApplyMetrics metrics =
                new DrawBackLowPalmLeftApplyMetrics
                {
                    target = DrawBackTargetName,
                    sourceOriginalHash = HashFile(DrawBackOriginalPath),
                    sourceUnityHashBefore = sourceHashBefore,
                    sourceUnityHashAfter = HashFile(DrawBackSourcePath),
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    sourceDurationSeconds = source.length,
                    adjustedDurationSeconds = bake.Clip.length,
                    frameRate = bake.Clip.frameRate,
                    framesBaked = bake.FramesBaked,
                    sourceCurveCount = sourceCurveCount,
                    adjustedCurveCount = adjustedCurveCount,
                    sourceEventCount = sourceEventCount,
                    adjustedEventCount = adjustedEventCount,
                    sourcePeakFrame = bake.SourcePeakFrame,
                    adjustedPeakFrame = bake.AdjustedPeakFrame,
                    expectedElbowFlexDegrees = 30f,
                    adjustedPeakElbowFlexDegrees =
                        bake.AdjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakHorizontalForwardAngleDegrees =
                        bake.AdjustedPeakHorizontalForwardAngleDegrees,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        bake.AdjustedPeakPalmCharacterLeftAngleDegrees,
                    targetReachErrorMetersMax =
                        bake.TargetReachErrorMetersMax,
                    rightHandBindLocalRotation = rightHandBindLocalRotation,
                    durationAndFrameRatePreserved = durationAndRate,
                    timingPeakFramePreserved = timingPeakPreserved,
                    sourceFbxExactAndUnchanged = sourceExact,
                    nonRightArmCurvesAndEventsUnchanged = nonRightArmUnchanged,
                    stowBackUnchanged = stowUnchanged,
                    controllerUsesAdjustedClip = controllerUsesAdjusted,
                    adjustedClipLoops = loops,
                    rootUnchanged = rootUnchanged,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    animatorSettingsCorrect = animatorSettingsCorrect,
                    applyRootMotion = animator.applyRootMotion,
                    passedNumericChecks =
                        durationAndRate &&
                        timingPeakPreserved &&
                        sourceExact &&
                        nonRightArmUnchanged &&
                        stowUnchanged &&
                        controllerUsesAdjusted &&
                        loops &&
                        rootUnchanged &&
                        otherAnimatorsUnchanged &&
                        animatorSettingsCorrect &&
                        sourceEventCount == adjustedEventCount &&
                        Mathf.Abs(
                            bake.AdjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                        bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                        bake.AdjustedPeakHorizontalForwardAngleDegrees <= 2f &&
                        bake.AdjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                        bake.TargetReachErrorMetersMax <= 0.002f &&
                        !animator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteJson(DrawBackLowPalmLeftApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back low palm-left apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackLowPalmLeftReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackLowPalmLeft] Applied solar-plexus height, 30-degree elbow, and character-left palm pose. " +
                "PeakFrame=" + bake.SourcePeakFrame +
                ", Height=" +
                Num(bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters) +
                ", Elbow=" + Num(bake.AdjustedPeakElbowFlexDegrees) +
                ", HorizontalForward=" +
                Num(bake.AdjustedPeakHorizontalForwardAngleDegrees) +
                ", PalmLeft=" +
                Num(bake.AdjustedPeakPalmCharacterLeftAngleDegrees) +
                ", SourceHashUnchanged=True, StowUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Low Palm Left Pose Review")]
        internal static void CaptureHandsDrawBackLowPalmLeftPoseReview()
        {
            int stage = SessionState.GetInt(
                DrawBackLowPalmLeftReviewStageKey,
                0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back low palm-left review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands Draw Back low palm-left review.");
                    }

                    SessionState.SetInt(DrawBackLowPalmLeftReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackLowPalmLeft] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back low palm-left capture requires Play Mode.");
                    }

                    CaptureHandsDrawBackLowPalmLeftPoseActualReview();
                    SessionState.SetInt(DrawBackLowPalmLeftReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back low palm-left review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(DrawBackLowPalmLeftReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackLowPalmLeft] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw Back low palm-left review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackLowPalmLeftReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Low Palm Left Pose Final")]
        internal static void CaptureHandsDrawBackLowPalmLeftPoseFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back low palm-left final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after Hands Draw Back low palm-left direct review.");
            }

            DrawBackLowPalmLeftReviewMetrics metrics =
                ReadJson<DrawBackLowPalmLeftReviewMetrics>(
                    DrawBackLowPalmLeftReviewMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back low palm-left review did not pass before final capture.");
            }

            CopyReviewedContact(
                DrawBackLowPalmLeftReviewPath,
                DrawBackLowPalmLeftFinalPath);
            Debug.Log(
                "[PlayerHandsDrawBackLowPalmLeft] Final image copied once from directly reviewed Play Mode frames. " +
                "Path=" + Path.GetFullPath(DrawBackLowPalmLeftFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw Back Outer Elbow Path")]
        internal static void ApplyHandsDrawBackOuterElbowPath()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackOuterElbowReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackOuterElbow] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands Draw Back outer-elbow apply.");
            }

            RequireHash(DrawBackOriginalPath, DrawBackSourceHash, "hands draw back original");
            RequireHash(DrawBackSourcePath, DrawBackSourceHash, "hands draw back Unity copy");
            string sourceHashBefore = HashFile(DrawBackSourcePath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            DrawBackLowPalmLeftBakeResult bake =
                CreateOrUpdateDrawBackLowPalmLeftAdjustedClip(
                    target,
                    source,
                    true);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    bake.Clip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            bool durationAndRate =
                Mathf.Abs(source.length - bake.Clip.length) <= 0.0001f &&
                Mathf.Abs(source.frameRate - bake.Clip.frameRate) <= 0.0001f;
            bool timingPeakPreserved =
                bake.SourcePeakFrame == bake.AdjustedPeakFrame;
            bool sourceExact =
                HashMatches(
                    DrawBackOriginalPath,
                    DrawBackSourcePath,
                    DrawBackSourceHash) &&
                string.Equals(
                    sourceHashBefore,
                    HashFile(DrawBackSourcePath),
                    StringComparison.Ordinal);
            bool nonRightArmUnchanged =
                AnimationMatchesExceptDrawBackRightArmRotations(
                    source,
                    bake.Clip);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            bool stowUnchanged = string.Equals(
                stowControllerHashBefore,
                stowControllerHashAfter,
                StringComparison.Ordinal);
            bool controllerUsesAdjusted =
                controller.layers.Length == 1 &&
                LayerStateUsesClip(
                    controller.layers[0],
                    DrawBackStateName,
                    bake.Clip);
            bool loops =
                AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime;
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName));
            bool animatorSettingsCorrect = AnimatorMatches(animator, controller);
            float elbowBeyondTorso =
                bake.AdjustedOuterElbowLateralMeters -
                bake.TorsoOuterBoundaryLateralMeters;
            float handBeyondTorso =
                bake.AdjustedOuterHandLateralMeters -
                bake.TorsoOuterBoundaryLateralMeters;
            float elbowBeyondHand =
                bake.AdjustedOuterElbowLateralMeters -
                bake.AdjustedOuterHandLateralMeters;
            float elbowIncrease =
                bake.AdjustedOuterElbowLateralMeters -
                bake.SourceOuterElbowLateralMeters;
            float handIncrease =
                bake.AdjustedOuterHandLateralMeters -
                bake.SourceOuterHandLateralMeters;
            DrawBackOuterElbowApplyMetrics metrics =
                new DrawBackOuterElbowApplyMetrics
                {
                    target = DrawBackTargetName,
                    sourceOriginalHash = HashFile(DrawBackOriginalPath),
                    sourceUnityHashBefore = sourceHashBefore,
                    sourceUnityHashAfter = HashFile(DrawBackSourcePath),
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    sourceDurationSeconds = source.length,
                    adjustedDurationSeconds = bake.Clip.length,
                    frameRate = bake.Clip.frameRate,
                    framesBaked = bake.FramesBaked,
                    sourcePeakFrame = bake.SourcePeakFrame,
                    adjustedPeakFrame = bake.AdjustedPeakFrame,
                    extractionStartFrame = bake.ExtractionStartFrame,
                    outerPathFrame = bake.OuterPathFrame,
                    sourceOuterElbowLateralMeters =
                        bake.SourceOuterElbowLateralMeters,
                    adjustedOuterElbowLateralMeters =
                        bake.AdjustedOuterElbowLateralMeters,
                    sourceOuterHandLateralMeters =
                        bake.SourceOuterHandLateralMeters,
                    adjustedOuterHandLateralMeters =
                        bake.AdjustedOuterHandLateralMeters,
                    torsoOuterBoundaryLateralMeters =
                        bake.TorsoOuterBoundaryLateralMeters,
                    adjustedElbowBeyondTorsoMeters = elbowBeyondTorso,
                    adjustedHandBeyondTorsoMeters = handBeyondTorso,
                    adjustedElbowBeyondHandMeters = elbowBeyondHand,
                    elbowOutwardIncreaseMeters = elbowIncrease,
                    handOutwardIncreaseMeters = handIncrease,
                    adjustedPeakElbowFlexDegrees =
                        bake.AdjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakHorizontalForwardAngleDegrees =
                        bake.AdjustedPeakHorizontalForwardAngleDegrees,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        bake.AdjustedPeakPalmCharacterLeftAngleDegrees,
                    targetReachErrorMetersMax = bake.TargetReachErrorMetersMax,
                    rightHandBindLocalRotation = rightHandBindLocalRotation,
                    durationAndFrameRatePreserved = durationAndRate,
                    timingPeakFramePreserved = timingPeakPreserved,
                    sourceFbxExactAndUnchanged = sourceExact,
                    nonRightArmCurvesAndEventsUnchanged = nonRightArmUnchanged,
                    stowBackUnchanged = stowUnchanged,
                    controllerUsesAdjustedClip = controllerUsesAdjusted,
                    adjustedClipLoops = loops,
                    rootUnchanged = rootUnchanged,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    animatorSettingsCorrect = animatorSettingsCorrect,
                    applyRootMotion = animator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                durationAndRate &&
                timingPeakPreserved &&
                sourceExact &&
                nonRightArmUnchanged &&
                stowUnchanged &&
                controllerUsesAdjusted &&
                loops &&
                rootUnchanged &&
                otherAnimatorsUnchanged &&
                animatorSettingsCorrect &&
                metrics.outerPathFrame > metrics.extractionStartFrame &&
                metrics.outerPathFrame < metrics.sourcePeakFrame &&
                metrics.adjustedElbowBeyondTorsoMeters >= 0.01f &&
                metrics.adjustedHandBeyondTorsoMeters >= 0f &&
                metrics.adjustedElbowBeyondHandMeters >= 0.015f &&
                metrics.elbowOutwardIncreaseMeters >= 0.03f &&
                metrics.elbowOutwardIncreaseMeters >=
                    metrics.handOutwardIncreaseMeters + 0.015f &&
                Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                metrics.adjustedPeakHorizontalForwardAngleDegrees <= 2f &&
                metrics.adjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                metrics.targetReachErrorMetersMax <= 0.002f &&
                !animator.applyRootMotion;
            WriteJson(DrawBackOuterElbowApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back outer-elbow apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackOuterElbowReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackOuterElbow] Applied outward elbow extraction path. " +
                "Frames=" + bake.ExtractionStartFrame + "/" +
                bake.OuterPathFrame + "/" + bake.SourcePeakFrame +
                ", ElbowBeyondTorso=" + Num(elbowBeyondTorso) +
                ", HandBeyondTorso=" + Num(handBeyondTorso) +
                ", ElbowBeyondHand=" + Num(elbowBeyondHand) +
                ", PeakElbow=" + Num(bake.AdjustedPeakElbowFlexDegrees) +
                ", SourceHashUnchanged=True, StowUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Outer Elbow Path Review")]
        internal static void CaptureHandsDrawBackOuterElbowPathReview()
        {
            int stage = SessionState.GetInt(
                DrawBackOuterElbowReviewStageKey,
                0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back outer-elbow review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands Draw Back outer-elbow review.");
                    }

                    SessionState.SetInt(DrawBackOuterElbowReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackOuterElbow] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back outer-elbow capture requires Play Mode.");
                    }

                    CaptureHandsDrawBackOuterElbowPathActualReview();
                    SessionState.SetInt(DrawBackOuterElbowReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back outer-elbow review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(DrawBackOuterElbowReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackOuterElbow] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw Back outer-elbow review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackOuterElbowReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Outer Elbow Path Final")]
        internal static void CaptureHandsDrawBackOuterElbowPathFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back outer-elbow final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after Hands Draw Back outer-elbow direct review.");
            }

            DrawBackOuterElbowReviewMetrics metrics =
                ReadJson<DrawBackOuterElbowReviewMetrics>(
                    DrawBackOuterElbowReviewMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back outer-elbow review did not pass before final capture.");
            }

            CopyReviewedContact(
                DrawBackOuterElbowReviewPath,
                DrawBackOuterElbowFinalPath);
            Debug.Log(
                "[PlayerHandsDrawBackOuterElbow] Final image copied once from directly reviewed Play Mode frames. " +
                "Path=" + Path.GetFullPath(DrawBackOuterElbowFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Transporter Purple Flag Draw Back Clearance And Start")]
        internal static void ApplyPlayerTransporterPurpleFlagDrawBackClearanceAndStart()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(TransporterPurpleFlagReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerTransporterPurpleFlag] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before transporter purple-flag apply.");
            }

            RequireHash(DrawBackOriginalPath, DrawBackSourceHash, "hands draw back original");
            RequireHash(DrawBackSourcePath, DrawBackSourceHash, "hands draw back Unity copy");
            string sourceHashBefore = HashFile(DrawBackSourcePath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            Transform layout = RequireLayout(scene);
            Transform emptyTarget = RequireTarget(layout, EmptyTargetName);
            Transform drawBackTarget = RequireTarget(layout, DrawBackTargetName);
            TransporterTextureEditResult textureEdit =
                ApplySharedTransporterLeftArmFlagTexture(emptyTarget);

            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            DrawBackLowPalmLeftBakeResult bake =
                CreateOrUpdateDrawBackLowPalmLeftAdjustedClip(
                    drawBackTarget,
                    source,
                    true,
                    true);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    bake.Clip);
            Animator animator = ConfigureAnimator(drawBackTarget, controller);
            ConfigurePlayerStartFacingEmpty(
                scene,
                emptyTarget,
                out Transform playerRoot,
                out Transform playerCamera,
                out Bounds emptyBounds);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            int sharedInstanceCount = CountSharedPlayerModelInstances(scene);
            string textureHash = HashFile(TransporterTexturePath);
            string duplicateHash = HashFile(TransporterTextureDuplicatePath);
            bool sourceExact = HashMatches(
                    DrawBackOriginalPath,
                    DrawBackSourcePath,
                    DrawBackSourceHash) &&
                string.Equals(
                    sourceHashBefore,
                    HashFile(DrawBackSourcePath),
                    StringComparison.Ordinal);
            bool stowUnchanged = string.Equals(
                stowControllerHashBefore,
                HashFile(StowBackControllerPath),
                StringComparison.Ordinal);
            bool nonRightArmUnchanged =
                AnimationMatchesExceptDrawBackRightArmRotations(source, bake.Clip);
            bool loops = AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime;
            bool controllerUsesAdjusted =
                controller.layers.Length == 1 &&
                LayerStateUsesClip(controller.layers[0], DrawBackStateName, bake.Clip) &&
                AnimatorMatches(animator, controller);
            Vector3 cameraToCenter = emptyBounds.center - playerCamera.position;
            bool frontSide = Vector3.Dot(
                playerCamera.position - emptyBounds.center,
                emptyTarget.forward) > 0f;
            bool cameraTargetsCenter = Vector3.Angle(
                playerCamera.forward,
                cameraToCenter) <= 0.1f;
            float cameraPitch = NormalizeSignedAngle(
                playerCamera.localEulerAngles.x);
            float playerDistance = Vector3.Distance(
                Vector3.ProjectOnPlane(playerCamera.position, Vector3.up),
                Vector3.ProjectOnPlane(emptyBounds.center, Vector3.up));
            TransporterPurpleFlagApplyMetrics metrics =
                new TransporterPurpleFlagApplyMetrics
                {
                    targetSet = "All shared player.fbx transporter instances, " + DrawBackTargetName + ", runtime Player start",
                    textureBaselineHash = HashFile(TransporterTextureBaselinePath),
                    textureHashAfter = textureHash,
                    duplicateTextureHashAfter = duplicateHash,
                    sharedPlayerModelInstanceCount = sharedInstanceCount,
                    leftArmTrianglesScanned = textureEdit.LeftArmTrianglesScanned,
                    flagSeedTriangleCount = textureEdit.FlagSeedTriangleCount,
                    flagPatchTriangleCount = textureEdit.FlagPatchTriangleCount,
                    recoloredPixelCount = textureEdit.RecoloredPixelCount,
                    targetLightPurple = textureEdit.TargetLightPurple,
                    drawBackDurationSeconds = bake.Clip.length,
                    drawBackFrameRate = bake.Clip.frameRate,
                    drawBackFramesBaked = bake.FramesBaked,
                    sourcePeakFrame = bake.SourcePeakFrame,
                    adjustedPeakFrame = bake.AdjustedPeakFrame,
                    extractionStartFrame = bake.ExtractionStartFrame,
                    outerPathFrame = bake.OuterPathFrame,
                    adjustedPeakElbowFlexDegrees = bake.AdjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakHorizontalForwardAngleDegrees =
                        bake.AdjustedPeakHorizontalForwardAngleDegrees,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        bake.AdjustedPeakPalmCharacterLeftAngleDegrees,
                    minimumRightArmTorsoClearanceMeters =
                        bake.MinimumRightArmTorsoClearanceMeters,
                    minimumClearanceFrame = bake.MinimumClearanceFrame,
                    playerStartPosition = playerRoot.position,
                    playerStartRotation = playerRoot.rotation,
                    playerCameraPitchDegrees = cameraPitch,
                    playerToEmptyDistanceMeters = playerDistance,
                    bothTextureCopiesExact = string.Equals(
                        textureHash,
                        duplicateHash,
                        StringComparison.Ordinal),
                    sourceFbxExactAndUnchanged = sourceExact,
                    stowBackUnchanged = stowUnchanged,
                    nonRightArmCurvesAndEventsUnchanged = nonRightArmUnchanged,
                    adjustedClipLoops = loops,
                    controllerUsesAdjustedClip = controllerUsesAdjusted,
                    playerStartsOnEmptyFrontSide = frontSide,
                    playerCameraTargetsEmptyCenter = cameraTargetsCenter,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.bothTextureCopiesExact &&
                metrics.sharedPlayerModelInstanceCount > 0 &&
                metrics.flagSeedTriangleCount > 0 &&
                metrics.flagPatchTriangleCount >= metrics.flagSeedTriangleCount &&
                metrics.recoloredPixelCount >= 100 &&
                sourceExact &&
                stowUnchanged &&
                nonRightArmUnchanged &&
                loops &&
                controllerUsesAdjusted &&
                bake.SourcePeakFrame == bake.AdjustedPeakFrame &&
                Mathf.Abs(bake.AdjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                bake.AdjustedPeakHorizontalForwardAngleDegrees <= 2f &&
                bake.AdjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                bake.MinimumRightArmTorsoClearanceMeters >= -0.004f &&
                frontSide &&
                cameraTargetsCenter &&
                !animator.applyRootMotion;
            WriteJson(TransporterPurpleFlagApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Transporter purple-flag apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(TransporterPurpleFlagReviewStageKey);
            Debug.Log(
                "[PlayerTransporterPurpleFlag] Applied shared left-arm light-purple patch, full-loop DrawBack clearance, and front-facing Empty start. " +
                "Instances=" + sharedInstanceCount +
                ", Pixels=" + textureEdit.RecoloredPixelCount +
                ", MinClearance=" + Num(bake.MinimumRightArmTorsoClearanceMeters) +
                "@" + bake.MinimumClearanceFrame +
                ", CameraPitch=" + Num(cameraPitch) + ".");
        }

        [MenuItem("Bellerophon/Player/Capture Transporter Purple Flag Draw Back Clearance And Start Review")]
        internal static void CapturePlayerTransporterPurpleFlagDrawBackClearanceAndStartReview()
        {
            int stage = SessionState.GetInt(TransporterPurpleFlagReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Transporter purple-flag review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before transporter purple-flag review.");
                    }

                    SessionState.SetInt(TransporterPurpleFlagReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerTransporterPurpleFlag] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Transporter purple-flag capture requires Play Mode.");
                    }

                    CapturePlayerTransporterPurpleFlagDrawBackClearanceAndStartActualReview();
                    SessionState.SetInt(TransporterPurpleFlagReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Transporter purple-flag review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(TransporterPurpleFlagReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerTransporterPurpleFlag] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Transporter purple-flag review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(TransporterPurpleFlagReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Transporter Purple Flag Draw Back Clearance And Start Final")]
        internal static void CapturePlayerTransporterPurpleFlagDrawBackClearanceAndStartFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Transporter purple-flag final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after transporter purple-flag direct review.");
            }

            TransporterPurpleFlagReviewMetrics metrics =
                ReadJson<TransporterPurpleFlagReviewMetrics>(
                    TransporterPurpleFlagReviewMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Transporter purple-flag review did not pass before final capture.");
            }

            CopyReviewedContact(
                TransporterPurpleFlagReviewPath,
                TransporterPurpleFlagFinalPath);
            Debug.Log(
                "[PlayerTransporterPurpleFlag] Final image copied once from the directly reviewed Play Mode contact sheet. " +
                "Path=" + Path.GetFullPath(TransporterPurpleFlagFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw Back Front Silhouette Clearance")]
        internal static void ApplyPlayerHandsDrawBackFrontSilhouetteClearance()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackFrontSilhouetteReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackFrontSilhouette] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands Draw Back front-silhouette apply.");
            }

            RequireHash(DrawBackOriginalPath, DrawBackSourceHash, "hands draw back original");
            RequireHash(DrawBackSourcePath, DrawBackSourceHash, "hands draw back Unity copy");
            string sourceHashBefore = HashFile(DrawBackSourcePath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            string transporterTextureHashBefore = HashFile(TransporterTexturePath);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            DrawBackLowPalmLeftBakeResult bake =
                CreateOrUpdateDrawBackLowPalmLeftAdjustedClip(
                    target,
                    source,
                    true,
                    true,
                    true);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    bake.Clip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            bool durationAndRate =
                Mathf.Abs(source.length - bake.Clip.length) <= 0.0001f &&
                Mathf.Abs(source.frameRate - bake.Clip.frameRate) <= 0.0001f;
            bool timingPeakPreserved =
                bake.SourcePeakFrame == bake.AdjustedPeakFrame;
            bool sourceExact =
                HashMatches(
                    DrawBackOriginalPath,
                    DrawBackSourcePath,
                    DrawBackSourceHash) &&
                string.Equals(
                    sourceHashBefore,
                    HashFile(DrawBackSourcePath),
                    StringComparison.Ordinal);
            bool nonRightArmUnchanged =
                AnimationMatchesExceptDrawBackRightArmRotations(
                    source,
                    bake.Clip);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            string transporterTextureHashAfter = HashFile(TransporterTexturePath);
            bool stowUnchanged = string.Equals(
                stowControllerHashBefore,
                stowControllerHashAfter,
                StringComparison.Ordinal);
            bool textureUnchanged = string.Equals(
                transporterTextureHashBefore,
                transporterTextureHashAfter,
                StringComparison.Ordinal);
            bool controllerUsesAdjusted =
                controller.layers.Length == 1 &&
                LayerStateUsesClip(
                    controller.layers[0],
                    DrawBackStateName,
                    bake.Clip);
            bool loops =
                AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime;
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName));
            bool animatorSettingsCorrect = AnimatorMatches(animator, controller);
            DrawBackFrontSilhouetteApplyMetrics metrics =
                new DrawBackFrontSilhouetteApplyMetrics
                {
                    target = DrawBackTargetName,
                    sourceOriginalHash = HashFile(DrawBackOriginalPath),
                    sourceUnityHashBefore = sourceHashBefore,
                    sourceUnityHashAfter = HashFile(DrawBackSourcePath),
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    transporterTextureHashBefore = transporterTextureHashBefore,
                    transporterTextureHashAfter = transporterTextureHashAfter,
                    sourceDurationSeconds = source.length,
                    adjustedDurationSeconds = bake.Clip.length,
                    frameRate = bake.Clip.frameRate,
                    framesBaked = bake.FramesBaked,
                    sourcePeakFrame = bake.SourcePeakFrame,
                    adjustedPeakFrame = bake.AdjustedPeakFrame,
                    minimumFrontSilhouetteGapMeters =
                        bake.MinimumFrontSilhouetteGapMeters,
                    minimumFrontSilhouetteGapFrame =
                        bake.MinimumFrontSilhouetteGapFrame,
                    adjustedPeakElbowFlexDegrees =
                        bake.AdjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakHorizontalForwardAngleDegrees =
                        bake.AdjustedPeakHorizontalForwardAngleDegrees,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        bake.AdjustedPeakPalmCharacterLeftAngleDegrees,
                    targetReachErrorMetersMax = bake.TargetReachErrorMetersMax,
                    rightHandBindLocalRotation = rightHandBindLocalRotation,
                    durationAndFrameRatePreserved = durationAndRate,
                    timingPeakFramePreserved = timingPeakPreserved,
                    sourceFbxExactAndUnchanged = sourceExact,
                    nonRightArmCurvesAndEventsUnchanged = nonRightArmUnchanged,
                    stowBackUnchanged = stowUnchanged,
                    transporterTextureUnchanged = textureUnchanged,
                    controllerUsesAdjustedClip = controllerUsesAdjusted,
                    adjustedClipLoops = loops,
                    rootUnchanged = rootUnchanged,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    animatorSettingsCorrect = animatorSettingsCorrect,
                    applyRootMotion = animator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                durationAndRate &&
                timingPeakPreserved &&
                sourceExact &&
                nonRightArmUnchanged &&
                stowUnchanged &&
                textureUnchanged &&
                controllerUsesAdjusted &&
                loops &&
                rootUnchanged &&
                otherAnimatorsUnchanged &&
                animatorSettingsCorrect &&
                metrics.minimumFrontSilhouetteGapMeters >= 0.005f &&
                Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                metrics.adjustedPeakHorizontalForwardAngleDegrees >= 5f &&
                metrics.adjustedPeakHorizontalForwardAngleDegrees <= 45f &&
                metrics.adjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                metrics.targetReachErrorMetersMax <= 0.002f &&
                !animator.applyRootMotion;
            WriteJson(DrawBackFrontSilhouetteApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back front-silhouette apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackFrontSilhouetteReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackFrontSilhouette] Applied right-arm front-silhouette clearance. " +
                "MinGap=" + Num(metrics.minimumFrontSilhouetteGapMeters) +
                "@" + metrics.minimumFrontSilhouetteGapFrame +
                ", PeakElbow=" + Num(metrics.adjustedPeakElbowFlexDegrees) +
                ", PeakOutwardAngle=" +
                Num(metrics.adjustedPeakHorizontalForwardAngleDegrees) +
                ", SourceHashUnchanged=True, StowUnchanged=True, TextureUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Front Silhouette Clearance Review")]
        internal static void CapturePlayerHandsDrawBackFrontSilhouetteClearanceReview()
        {
            int stage = SessionState.GetInt(
                DrawBackFrontSilhouetteReviewStageKey,
                0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back front-silhouette review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands Draw Back front-silhouette review.");
                    }

                    SessionState.SetInt(
                        DrawBackFrontSilhouetteReviewStageKey,
                        1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackFrontSilhouette] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back front-silhouette capture requires Play Mode.");
                    }

                    CapturePlayerHandsDrawBackFrontSilhouetteClearanceActualReview();
                    SessionState.SetInt(
                        DrawBackFrontSilhouetteReviewStageKey,
                        2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back front-silhouette review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(
                        DrawBackFrontSilhouetteReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackFrontSilhouette] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw Back front-silhouette review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackFrontSilhouetteReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Front Silhouette Clearance Final")]
        internal static void CapturePlayerHandsDrawBackFrontSilhouetteClearanceFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back front-silhouette final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after Hands Draw Back front-silhouette direct review.");
            }

            DrawBackFrontSilhouetteReviewMetrics metrics =
                ReadJson<DrawBackFrontSilhouetteReviewMetrics>(
                    DrawBackFrontSilhouetteReviewMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back front-silhouette review did not pass before final capture.");
            }

            CopyReviewedContact(
                DrawBackFrontSilhouetteReviewPath,
                DrawBackFrontSilhouetteFinalPath);
            Debug.Log(
                "[PlayerHandsDrawBackFrontSilhouette] Final image copied once from the directly reviewed Play Mode contact sheet. " +
                "Path=" + Path.GetFullPath(DrawBackFrontSilhouetteFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Analyze Hands Draw Back Right Chest Deformation")]
        internal static void AnalyzePlayerHandsDrawBackRightChestDeformation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back right-chest diagnosis requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands Draw Back right-chest diagnosis.");
            }

            RequireHash(DrawBackOriginalPath, DrawBackSourceHash, "hands draw back original");
            RequireHash(DrawBackSourcePath, DrawBackSourceHash, "hands draw back Unity copy");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back adjusted clip is missing for right-chest diagnosis.");
            }

            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            DrawBackRightChestDiagnosticResult result =
                MeasureDrawBackRightChestDeformation(
                    target,
                    source,
                    adjusted,
                    rightHandBindLocalRotation);
            DrawBackChestDeformationApplyMetrics current =
                ReadJson<DrawBackChestDeformationApplyMetrics>(
                    DrawBackChestDeformationApplyMetricsPath);
            CaptureDrawBackChestDeformationComparison(
                target,
                source,
                result.MaximumProtrusionFrame,
                current.sourcePeakFrame,
                rightHandBindLocalRotation,
                DrawBackRightChestDiagnosticPath);

            Mesh originalPlayerMesh = LoadPlayerMeshByName("char1");
            string playerFbxPath = AssetDatabase.GetAssetPath(
                originalPlayerMesh);
            DrawBackRightChestDiagnosticMetrics metrics =
                new DrawBackRightChestDiagnosticMetrics
                {
                    target = DrawBackTargetName,
                    rendererPath = result.RendererPath,
                    sourceMeshName = originalPlayerMesh.name,
                    sourceMeshAssetPath = playerFbxPath,
                    vertexCount = originalPlayerMesh.vertexCount,
                    sourceBlendShapeCount = originalPlayerMesh.blendShapeCount,
                    framesPerLoop = result.FramesPerLoop,
                    framesSampled = result.FramesPerLoop,
                    maximumProtrusionFrame = result.MaximumProtrusionFrame,
                    maximumProtrusionVertexIndex =
                        result.MaximumProtrusionVertexIndex,
                    maximumForwardProtrusionMeters =
                        result.MaximumForwardProtrusionMeters,
                    averageAffectedForwardProtrusionMeters =
                        result.AverageAffectedForwardProtrusionMeters,
                    affectedVertexCount = result.AffectedVertexCount,
                    maximumVertexRightArmWeight =
                        result.MaximumVertexRightArmWeight,
                    maximumVertexRightShoulderWeight =
                        result.MaximumVertexRightShoulderWeight,
                    maximumVertexTorsoWeight =
                        result.MaximumVertexTorsoWeight,
                    maximumVertexOtherWeight =
                        result.MaximumVertexOtherWeight,
                    maximumVertexSourceWorldPosition =
                        result.MaximumVertexSourceWorldPosition,
                    maximumVertexAdjustedWorldPosition =
                        result.MaximumVertexAdjustedWorldPosition,
                    diagnosedCause =
                        "현재 파생 클립의 RightArm 회전이 오른쪽 가슴 정점에 포함된 RightArm/RightShoulder 혼합 스킨 가중치를 통해 정점을 캐릭터 전방으로 끌어내고 있다.",
                    playerFbxHash = HashFile("Assets/_Project/Art/Player/player.fbx"),
                    sourceAnimationFbxHash = HashFile(DrawBackSourcePath),
                    adjustedClipHash = HashFile(DrawBackForwardAdjustedClipPath),
                    sourceMeshIsSharedPlayerAsset =
                        string.Equals(
                            playerFbxPath,
                            "Assets/_Project/Art/Player/player.fbx",
                            StringComparison.Ordinal),
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.diagnosisComplete =
                metrics.sourceMeshIsSharedPlayerAsset &&
                metrics.vertexCount > 0 &&
                metrics.framesSampled == metrics.framesPerLoop &&
                metrics.maximumProtrusionVertexIndex >= 0 &&
                metrics.maximumForwardProtrusionMeters > 0.002f &&
                metrics.affectedVertexCount > 0 &&
                metrics.maximumVertexRightArmWeight +
                    metrics.maximumVertexRightShoulderWeight > 0.01f;
            WriteJson(DrawBackRightChestDiagnosticMetricsPath, metrics);
            if (!metrics.diagnosisComplete)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back right-chest diagnosis did not isolate the deformation. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackRightChest] Diagnosed forward chest protrusion. " +
                "Renderer=" + metrics.rendererPath +
                ", Mesh=" + metrics.sourceMeshName +
                ", Max=" + Num(metrics.maximumForwardProtrusionMeters) +
                "m@" + metrics.maximumProtrusionFrame +
                "/v" + metrics.maximumProtrusionVertexIndex +
                ", Affected=" + metrics.affectedVertexCount +
                ", Weights(RightArm/RightShoulder/Torso)=" +
                Num(metrics.maximumVertexRightArmWeight) + "/" +
                Num(metrics.maximumVertexRightShoulderWeight) + "/" +
                Num(metrics.maximumVertexTorsoWeight) + ".");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw Back Right Chest Correction")]
        internal static void ApplyPlayerHandsDrawBackRightChestCorrection()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackRightChestReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackRightChest] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands Draw Back right-chest correction.");
            }

            DrawBackRightChestDiagnosticMetrics diagnostic =
                ReadJson<DrawBackRightChestDiagnosticMetrics>(
                    DrawBackRightChestDiagnosticMetricsPath);
            if (!diagnostic.diagnosisComplete)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back right-chest diagnosis must pass before apply.");
            }

            string playerFbxHashBefore = HashFile(
                "Assets/_Project/Art/Player/player.fbx");
            string sourceAnimationHashBefore = HashFile(DrawBackSourcePath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            string transporterTextureHashBefore = HashFile(TransporterTexturePath);
            string adjustedClipHashBefore = HashFile(
                DrawBackForwardAdjustedClipPath);
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            Dictionary<string, string> otherRendererMeshesBefore =
                CapturePrimaryRendererMeshPathsExceptTarget(
                    layout,
                    DrawBackTargetName);
            SkinnedMeshRenderer renderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(target);
            Mesh originalMesh = LoadPlayerMeshByName(diagnostic.sourceMeshName);
            renderer.sharedMesh = originalMesh;
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back adjusted clip is missing for right-chest correction.");
            }

            string rendererPath = AnimationUtility.CalculateTransformPath(
                renderer.transform,
                target);
            RemoveDrawBackRightChestBlendShapeCurve(
                adjusted,
                rendererPath);
            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            DrawBackRightChestCorrectiveBuildResult build =
                CreateOrUpdateDrawBackRightChestStableSkinMesh(
                    target,
                    renderer,
                    originalMesh,
                    source,
                    adjusted,
                    rightHandBindLocalRotation);
            renderer.sharedMesh = build.CorrectedMesh;
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);

            DrawBackRightChestDiagnosticResult corrected =
                MeasureDrawBackRightChestDeformation(
                    target,
                    source,
                    adjusted,
                    rightHandBindLocalRotation);
            string playerFbxHashAfter = HashFile(
                "Assets/_Project/Art/Player/player.fbx");
            string sourceAnimationHashAfter = HashFile(DrawBackSourcePath);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            string transporterTextureHashAfter = HashFile(TransporterTexturePath);
            Dictionary<string, string> otherRendererMeshesAfter =
                CapturePrimaryRendererMeshPathsExceptTarget(
                    layout,
                    DrawBackTargetName);
            AnimationCurve[] correctiveCurves =
                GetDrawBackRightChestCorrectiveCurves(
                    adjusted,
                    rendererPath);
            DrawBackRightChestCorrectionApplyMetrics metrics =
                new DrawBackRightChestCorrectionApplyMetrics
                {
                    target = DrawBackTargetName,
                    rendererPath = rendererPath,
                    correctedMeshPath = AssetDatabase.GetAssetPath(
                        build.CorrectedMesh),
                    blendShapeName = build.BlendShapeIndex >= 0
                        ? DrawBackRightChestBlendShapeName
                        : "HandsDrawBackRightChestStateSkinWeights",
                    blendShapeIndex = build.BlendShapeIndex,
                    correctedVertexCount = build.CorrectedVertexCount,
                    blendShapeCurveKeyCount = build.CurveKeyCount,
                    maximumBindPoseCorrectionMeters =
                        build.MaximumBindPoseCorrectionMeters,
                    beforeMaximumForwardProtrusionMeters =
                        diagnostic.maximumForwardProtrusionMeters,
                    afterMaximumForwardProtrusionMeters =
                        corrected.MaximumForwardProtrusionMeters,
                    beforeAffectedVertexCount = diagnostic.affectedVertexCount,
                    afterAffectedVertexCount = corrected.AffectedVertexCount,
                    playerFbxHashBefore = playerFbxHashBefore,
                    playerFbxHashAfter = playerFbxHashAfter,
                    sourceAnimationFbxHashBefore = sourceAnimationHashBefore,
                    sourceAnimationFbxHashAfter = sourceAnimationHashAfter,
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    transporterTextureHashBefore = transporterTextureHashBefore,
                    transporterTextureHashAfter = transporterTextureHashAfter,
                    adjustedClipHashBefore = adjustedClipHashBefore,
                    adjustedClipHashAfter = HashFile(
                        DrawBackForwardAdjustedClipPath),
                    correctedMeshHash = HashFile(
                        DrawBackRightChestCorrectedMeshPath),
                    rendererUsesCorrectedMesh =
                        renderer.sharedMesh == build.CorrectedMesh &&
                        string.Equals(
                            AssetDatabase.GetAssetPath(renderer.sharedMesh),
                            DrawBackRightChestCorrectedMeshPath,
                            StringComparison.Ordinal),
                    blendShapeCurveBound =
                        (build.CurveKeyCount == 0
                            ? correctiveCurves.Length == 0 &&
                              build.CorrectedMesh.blendShapeCount == 0 &&
                              build.BlendShapeIndex < 0
                            : correctiveCurves.Length > 0 &&
                              correctiveCurves.Sum(curve => curve.length) ==
                                  build.CurveKeyCount &&
                              build.BlendShapeIndex >= 0),
                    sourceAssetsUnchanged =
                        string.Equals(
                            playerFbxHashBefore,
                            playerFbxHashAfter,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            sourceAnimationHashBefore,
                            sourceAnimationHashAfter,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            stowControllerHashBefore,
                            stowControllerHashAfter,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            transporterTextureHashBefore,
                            transporterTextureHashAfter,
                            StringComparison.Ordinal),
                    otherTransportersKeepSharedPlayerMesh = DictionariesEqual(
                        otherRendererMeshesBefore,
                        otherRendererMeshesAfter) &&
                        otherRendererMeshesAfter.Values.All(path =>
                            !string.Equals(
                                path,
                                DrawBackRightChestCorrectedMeshPath,
                                StringComparison.Ordinal)),
                    rootUnchanged = RootMatches(target, rootBefore),
                    otherAnimatorsUnchanged = DictionariesEqual(
                        otherAnimatorsBefore,
                        CaptureAnimatorsExceptTarget(
                            layout,
                            DrawBackTargetName)),
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.rendererUsesCorrectedMesh &&
                metrics.blendShapeCurveBound &&
                metrics.correctedVertexCount > 0 &&
                metrics.maximumBindPoseCorrectionMeters <= 1f &&
                metrics.sourceAssetsUnchanged &&
                metrics.otherTransportersKeepSharedPlayerMesh &&
                metrics.rootUnchanged &&
                metrics.otherAnimatorsUnchanged;
            WriteJson(DrawBackRightChestApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back right-chest correction support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackRightChestReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackRightChest] Applied state-only right-chest skin-weight correction. " +
                "Vertices=" + metrics.correctedVertexCount +
                ", MaximumTransferredWeight=" +
                Num(metrics.maximumBindPoseCorrectionMeters) +
                ", SharedPlayerMeshUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Right Chest Correction Review")]
        internal static void CapturePlayerHandsDrawBackRightChestCorrectionReview()
        {
            int stage = SessionState.GetInt(DrawBackRightChestReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back right-chest review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands Draw Back right-chest review.");
                    }

                    SessionState.SetInt(DrawBackRightChestReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackRightChest] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back right-chest capture requires Play Mode.");
                    }

                    CapturePlayerHandsDrawBackRightChestCorrectionActualReview();
                    SessionState.SetInt(DrawBackRightChestReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back right-chest review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(DrawBackRightChestReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackRightChest] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw Back right-chest review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackRightChestReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Right Chest Correction Final")]
        internal static void CapturePlayerHandsDrawBackRightChestCorrectionFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back right-chest final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after Hands Draw Back right-chest direct review.");
            }

            DrawBackRightChestCorrectionReviewMetrics metrics =
                ReadJson<DrawBackRightChestCorrectionReviewMetrics>(
                    DrawBackRightChestReviewMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back right-chest review did not pass before final capture.");
            }

            CopyReviewedContact(
                DrawBackRightChestVideoPoseStressPath,
                DrawBackRightChestFinalPath);
            Debug.Log(
                "[PlayerHandsDrawBackRightChest] Final image copied once from the directly reviewed video-pose stress comparison. " +
                "Path=" + Path.GetFullPath(DrawBackRightChestFinalPath) +
                ", SceneChanged=False.");
        }

        internal static void AnalyzePlayerHandsDrawBackRightChestVideoReference()
        {
            AnalyzePlayerHandsDrawBackRightChestDeformation();
        }

        internal static void ApplyPlayerHandsDrawBackRightChestVideoCorrection()
        {
            ApplyPlayerHandsDrawBackRightChestCorrection();
        }

        internal static void CapturePlayerHandsDrawBackRightChestVideoCorrectionReview()
        {
            CapturePlayerHandsDrawBackRightChestCorrectionReview();
        }

        internal static void CapturePlayerHandsDrawBackRightChestVideoCorrectionFinal()
        {
            CapturePlayerHandsDrawBackRightChestCorrectionFinal();
        }

        [MenuItem("Bellerophon/Player/Apply Hands Draw Back Chest Deformation Fix")]
        internal static void ApplyPlayerHandsDrawBackChestDeformationFix()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(DrawBackChestDeformationReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsDrawBackChestDeformation] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands Draw Back chest-deformation apply.");
            }

            DrawBackFrontSilhouetteApplyMetrics previous =
                ReadJson<DrawBackFrontSilhouetteApplyMetrics>(
                    DrawBackFrontSilhouetteApplyMetricsPath);
            if (!previous.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back front-silhouette baseline metrics did not pass.");
            }

            RequireHash(DrawBackOriginalPath, DrawBackSourceHash, "hands draw back original");
            RequireHash(DrawBackSourcePath, DrawBackSourceHash, "hands draw back Unity copy");
            string sourceHashBefore = HashFile(DrawBackSourcePath);
            string adjustedClipHashBefore = HashFile(DrawBackForwardAdjustedClipPath);
            string stowControllerHashBefore = HashFile(StowBackControllerPath);
            string transporterTextureHashBefore = HashFile(TransporterTexturePath);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName);
            DrawBackLowPalmLeftBakeResult bake =
                CreateOrUpdateDrawBackLowPalmLeftAdjustedClip(
                    target,
                    source,
                    true,
                    true,
                    true,
                    DrawBackChestSafeOutwardDegrees);
            AnimatorController controller =
                CreateOrUpdateExactEmbeddedTakeController(
                    DrawBackControllerPath,
                    DrawBackStateName,
                    bake.Clip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            bool durationAndRate =
                Mathf.Abs(source.length - bake.Clip.length) <= 0.0001f &&
                Mathf.Abs(source.frameRate - bake.Clip.frameRate) <= 0.0001f;
            bool timingPeakPreserved =
                bake.SourcePeakFrame == bake.AdjustedPeakFrame;
            bool sourceExact =
                HashMatches(
                    DrawBackOriginalPath,
                    DrawBackSourcePath,
                    DrawBackSourceHash) &&
                string.Equals(
                    sourceHashBefore,
                    HashFile(DrawBackSourcePath),
                    StringComparison.Ordinal);
            bool nonRightArmUnchanged =
                AnimationMatchesExceptDrawBackRightArmRotations(
                    source,
                    bake.Clip);
            string stowControllerHashAfter = HashFile(StowBackControllerPath);
            string transporterTextureHashAfter = HashFile(TransporterTexturePath);
            bool stowUnchanged = string.Equals(
                stowControllerHashBefore,
                stowControllerHashAfter,
                StringComparison.Ordinal);
            bool textureUnchanged = string.Equals(
                transporterTextureHashBefore,
                transporterTextureHashAfter,
                StringComparison.Ordinal);
            bool controllerUsesAdjusted =
                controller.layers.Length == 1 &&
                LayerStateUsesClip(
                    controller.layers[0],
                    DrawBackStateName,
                    bake.Clip);
            bool loops =
                AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime;
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, DrawBackTargetName));
            bool animatorSettingsCorrect = AnimatorMatches(animator, controller);
            DrawBackChestDeformationApplyMetrics metrics =
                new DrawBackChestDeformationApplyMetrics
                {
                    target = DrawBackTargetName,
                    sourceOriginalHash = HashFile(DrawBackOriginalPath),
                    sourceUnityHashBefore = sourceHashBefore,
                    sourceUnityHashAfter = HashFile(DrawBackSourcePath),
                    adjustedClipHashBefore = adjustedClipHashBefore,
                    adjustedClipHashAfter = HashFile(DrawBackForwardAdjustedClipPath),
                    stowControllerHashBefore = stowControllerHashBefore,
                    stowControllerHashAfter = stowControllerHashAfter,
                    transporterTextureHashBefore = transporterTextureHashBefore,
                    transporterTextureHashAfter = transporterTextureHashAfter,
                    sourceDurationSeconds = source.length,
                    adjustedDurationSeconds = bake.Clip.length,
                    frameRate = bake.Clip.frameRate,
                    framesBaked = bake.FramesBaked,
                    sourcePeakFrame = bake.SourcePeakFrame,
                    adjustedPeakFrame = bake.AdjustedPeakFrame,
                    previousPeakHorizontalOutwardAngleDegrees =
                        previous.adjustedPeakHorizontalForwardAngleDegrees,
                    adjustedPeakHorizontalOutwardAngleDegrees =
                        bake.AdjustedPeakHorizontalForwardAngleDegrees,
                    outwardAngleReductionDegrees =
                        previous.adjustedPeakHorizontalForwardAngleDegrees -
                        bake.AdjustedPeakHorizontalForwardAngleDegrees,
                    minimumFrontSilhouetteGapMeters =
                        bake.MinimumFrontSilhouetteGapMeters,
                    minimumFrontSilhouetteGapFrame =
                        bake.MinimumFrontSilhouetteGapFrame,
                    adjustedPeakElbowFlexDegrees =
                        bake.AdjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        bake.AdjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        bake.AdjustedPeakPalmCharacterLeftAngleDegrees,
                    targetReachErrorMetersMax = bake.TargetReachErrorMetersMax,
                    rightHandBindLocalRotation = rightHandBindLocalRotation,
                    durationAndFrameRatePreserved = durationAndRate,
                    timingPeakFramePreserved = timingPeakPreserved,
                    sourceFbxExactAndUnchanged = sourceExact,
                    nonRightArmCurvesAndEventsUnchanged = nonRightArmUnchanged,
                    stowBackUnchanged = stowUnchanged,
                    transporterTextureUnchanged = textureUnchanged,
                    controllerUsesAdjustedClip = controllerUsesAdjusted,
                    adjustedClipLoops = loops,
                    rootUnchanged = rootUnchanged,
                    otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                    animatorSettingsCorrect = animatorSettingsCorrect,
                    applyRootMotion = animator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                durationAndRate &&
                timingPeakPreserved &&
                sourceExact &&
                nonRightArmUnchanged &&
                stowUnchanged &&
                textureUnchanged &&
                controllerUsesAdjusted &&
                loops &&
                rootUnchanged &&
                otherAnimatorsUnchanged &&
                animatorSettingsCorrect &&
                metrics.minimumFrontSilhouetteGapMeters >= 0.005f &&
                Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                Mathf.Abs(
                    metrics.adjustedPeakHorizontalOutwardAngleDegrees -
                    DrawBackChestSafeOutwardDegrees) <= 0.5f &&
                metrics.outwardAngleReductionDegrees >= 5f &&
                metrics.adjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                metrics.targetReachErrorMetersMax <= 0.002f &&
                !animator.applyRootMotion;
            WriteJson(DrawBackChestDeformationApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back chest-deformation apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            SessionState.EraseInt(DrawBackChestDeformationReviewStageKey);
            Debug.Log(
                "[PlayerHandsDrawBackChestDeformation] Applied chest-safe right-arm angle. " +
                "Outward=" +
                Num(metrics.previousPeakHorizontalOutwardAngleDegrees) + "->" +
                Num(metrics.adjustedPeakHorizontalOutwardAngleDegrees) +
                ", MinGap=" + Num(metrics.minimumFrontSilhouetteGapMeters) +
                "@" + metrics.minimumFrontSilhouetteGapFrame +
                ", PeakElbow=" + Num(metrics.adjustedPeakElbowFlexDegrees) +
                ", SourceHashUnchanged=True, StowUnchanged=True, TextureUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Chest Deformation Fix Review")]
        internal static void CapturePlayerHandsDrawBackChestDeformationFixReview()
        {
            int stage = SessionState.GetInt(
                DrawBackChestDeformationReviewStageKey,
                0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back chest-deformation review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands Draw Back chest-deformation review.");
                    }

                    SessionState.SetInt(
                        DrawBackChestDeformationReviewStageKey,
                        1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackChestDeformation] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back chest-deformation capture requires Play Mode.");
                    }

                    CapturePlayerHandsDrawBackChestDeformationFixActualReview();
                    SessionState.SetInt(
                        DrawBackChestDeformationReviewStageKey,
                        2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands Draw Back chest-deformation review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(
                        DrawBackChestDeformationReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsDrawBackChestDeformation] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands Draw Back chest-deformation review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(DrawBackChestDeformationReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Draw Back Chest Deformation Fix Final")]
        internal static void CapturePlayerHandsDrawBackChestDeformationFixFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back chest-deformation final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after Hands Draw Back chest-deformation direct review.");
            }

            DrawBackChestDeformationReviewMetrics metrics =
                ReadJson<DrawBackChestDeformationReviewMetrics>(
                    DrawBackChestDeformationReviewMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back chest-deformation review did not pass before final capture.");
            }

            CopyReviewedContact(
                DrawBackChestDeformationReviewPath,
                DrawBackChestDeformationFinalPath);
            Debug.Log(
                "[PlayerHandsDrawBackChestDeformation] Final image copied once from the directly reviewed Play Mode contact sheet. " +
                "Path=" + Path.GetFullPath(DrawBackChestDeformationFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands And Objects Review")]
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
                            "Hands and Objects review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands and Objects review.");
                    }

                    SessionState.SetInt(ReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log("[PlayerHandsObjects] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands and Objects capture requires Play Mode.");
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
                            "Hands and Objects review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(ReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log("[PlayerHandsObjects] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands and Objects review stage is invalid: " +
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

        [MenuItem("Bellerophon/Player/Capture Hands And Objects Final")]
        internal static void CaptureFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands and Objects final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands and Objects final capture.");
            }

            ReviewMetrics metrics = ReadJson<ReviewMetrics>(ReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                !metrics.emptyIdle.passedNumericChecks ||
                !metrics.oneHand.passedNumericChecks ||
                !metrics.twoHand.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands and Objects review did not pass before final capture.");
            }

            CopyReviewedContact(EmptyReviewPath, EmptyFinalPath);
            CopyReviewedContact(OneHandReviewPath, OneHandFinalPath);
            CopyReviewedContact(TwoHandReviewPath, TwoHandFinalPath);
            Debug.Log(
                "[PlayerHandsObjects] Final images copied once from directly reviewed Play Mode frames. " +
                "Empty=" + Path.GetFullPath(EmptyFinalPath) + ", " +
                "OneHand=" + Path.GetFullPath(OneHandFinalPath) + ", " +
                "TwoHand=" + Path.GetFullPath(TwoHandFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry Body Alignment")]
        internal static void ApplyCarryBodyAlignment()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(AlignmentReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsCarryBodyAlignment] Exited Play Mode before alignment apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands carry body alignment apply.");
            }

            string emptyHashBefore = HashFile(EmptyClipPath);
            string oneHandHashBefore = HashFile(OneHandSourcePath);
            string twoHandHashBefore = HashFile(TwoHandSourcePath);
            RequireHash(OneHandSourcePath, OneHandSourceHash, "one-hand carry Unity FBX");
            RequireHash(TwoHandSourcePath, TwoHandSourceHash, "two-hand carry Unity FBX");
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip oneHandClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip twoHandClip = LoadSingleEmbeddedClip(
                TwoHandSourcePath,
                "two-hand carry");

            Transform layout = RequireLayout(scene);
            Transform oneHandTarget = RequireTarget(layout, OneHandTargetName);
            Transform twoHandTarget = RequireTarget(layout, TwoHandTargetName);
            RootPose oneHandRootBefore = new RootPose(oneHandTarget);
            RootPose twoHandRootBefore = new RootPose(twoHandTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptCarryTargets(layout);
            AvatarMask armsMask = CreateOrUpdateArmsMask(oneHandTarget);
            AnimatorController oneHandController = CreateOrUpdateLayeredCarryController(
                OneHandControllerPath,
                OneHandStateName,
                emptyClip,
                oneHandClip,
                armsMask);
            AnimatorController twoHandController = CreateOrUpdateLayeredCarryController(
                TwoHandControllerPath,
                TwoHandStateName,
                emptyClip,
                twoHandClip,
                armsMask);
            Animator oneHandAnimator = ConfigureAnimator(oneHandTarget, oneHandController);
            Animator twoHandAnimator = ConfigureAnimator(twoHandTarget, twoHandController);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string emptyHashAfter = HashFile(EmptyClipPath);
            string oneHandHashAfter = HashFile(OneHandSourcePath);
            string twoHandHashAfter = HashFile(TwoHandSourcePath);
            bool inputsUnchanged =
                string.Equals(emptyHashBefore, emptyHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oneHandHashBefore, oneHandHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(twoHandHashBefore, twoHandHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oneHandHashAfter, OneHandSourceHash, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(twoHandHashAfter, TwoHandSourceHash, StringComparison.OrdinalIgnoreCase);
            bool maskExact = ArmMaskIsExact(
                armsMask,
                out int maskTransformCount,
                out int activeArmTransformCount,
                out bool hasLeftShoulder,
                out bool hasRightShoulder);
            AlignmentTargetApplyMetrics oneHandMetrics =
                CreateAlignmentTargetApplyMetrics(
                    OneHandTargetName,
                    OneHandStateName,
                    oneHandClip,
                    emptyClip,
                    oneHandController,
                    armsMask,
                    oneHandAnimator);
            AlignmentTargetApplyMetrics twoHandMetrics =
                CreateAlignmentTargetApplyMetrics(
                    TwoHandTargetName,
                    TwoHandStateName,
                    twoHandClip,
                    emptyClip,
                    twoHandController,
                    armsMask,
                    twoHandAnimator);
            bool rootsUnchanged =
                RootMatches(oneHandTarget, oneHandRootBefore) &&
                RootMatches(twoHandTarget, twoHandRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptCarryTargets(layout));
            bool animatorSettingsCorrect =
                AnimatorMatches(oneHandAnimator, oneHandController) &&
                AnimatorMatches(twoHandAnimator, twoHandController);
            bool controllersCorrect =
                AlignmentControllerCorrect(oneHandMetrics) &&
                AlignmentControllerCorrect(twoHandMetrics);

            AlignmentApplyMetrics metrics = new AlignmentApplyMetrics
            {
                targetSet = OneHandTargetName + ", " + TwoHandTargetName,
                emptyIdleHashBefore = emptyHashBefore,
                emptyIdleHashAfter = emptyHashAfter,
                oneHandFbxHashBefore = oneHandHashBefore,
                oneHandFbxHashAfter = oneHandHashAfter,
                twoHandFbxHashBefore = twoHandHashBefore,
                twoHandFbxHashAfter = twoHandHashAfter,
                maskTransformCount = maskTransformCount,
                activeArmTransformCount = activeArmTransformCount,
                hasLeftShoulderSubtree = hasLeftShoulder,
                hasRightShoulderSubtree = hasRightShoulder,
                oneHand = oneHandMetrics,
                twoHand = twoHandMetrics,
                inputAnimationsUnchanged = inputsUnchanged,
                armMaskExact = maskExact,
                rootsUnchanged = rootsUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                animatorSettingsCorrect = animatorSettingsCorrect,
                passedNumericChecks = inputsUnchanged &&
                    maskExact &&
                    rootsUnchanged &&
                    otherAnimatorsUnchanged &&
                    animatorSettingsCorrect &&
                    controllersCorrect,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(AlignmentApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands carry body alignment apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsCarryBodyAlignment] Applied Empty Idle body with exact Carry arm subtrees. " +
                "MaskTransforms=" + maskTransformCount +
                ", ActiveArms=" + activeArmTransformCount +
                ", Base=" + Num(emptyClip.length) + "s" +
                ", OneHandArms=" + Num(oneHandClip.length) + "s" +
                ", TwoHandArms=" + Num(twoHandClip.length) + "s" +
                ", InputsUnchanged=True, OtherAnimatorsUnchanged=True, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry Body Alignment Review")]
        internal static void CaptureCarryBodyAlignmentReview()
        {
            int stage = SessionState.GetInt(AlignmentReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands carry body alignment review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands carry body alignment review.");
                    }

                    SessionState.SetInt(AlignmentReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsCarryBodyAlignment] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands carry body alignment capture requires Play Mode.");
                    }

                    CaptureCarryBodyAlignmentActualReview();
                    SessionState.SetInt(AlignmentReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands carry body alignment review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(AlignmentReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsCarryBodyAlignment] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands carry body alignment review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(AlignmentReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry Body Alignment Final")]
        internal static void CaptureCarryBodyAlignmentFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands carry body alignment final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands carry body alignment final capture.");
            }

            AlignmentReviewMetrics metrics =
                ReadJson<AlignmentReviewMetrics>(AlignmentReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                !metrics.oneHand.passedNumericChecks ||
                !metrics.twoHand.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands carry body alignment review did not pass before final capture.");
            }

            CopyReviewedContact(OneHandAlignmentReviewPath, OneHandAlignmentFinalPath);
            CopyReviewedContact(TwoHandAlignmentReviewPath, TwoHandAlignmentFinalPath);
            Debug.Log(
                "[PlayerHandsCarryBodyAlignment] Final images copied once from directly reviewed Play Mode frames. " +
                "OneHand=" + Path.GetFullPath(OneHandAlignmentFinalPath) + ", " +
                "TwoHand=" + Path.GetFullPath(TwoHandAlignmentFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry Pose Adjustment")]
        internal static void ApplyCarryPoseAdjustment()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(PoseAdjustmentReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsCarryPoseAdjustment] Exited Play Mode before pose apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands carry pose adjustment apply.");
            }

            string emptyHashBefore = HashFile(EmptyClipPath);
            string oneHandHashBefore = HashFile(OneHandSourcePath);
            string twoHandHashBefore = HashFile(TwoHandSourcePath);
            RequireHash(OneHandSourcePath, OneHandSourceHash, "one-hand carry Unity FBX");
            RequireHash(TwoHandSourcePath, TwoHandSourceHash, "two-hand carry Unity FBX");
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip oneHandSource = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip twoHandSource = LoadSingleEmbeddedClip(
                TwoHandSourcePath,
                "two-hand carry");

            Transform layout = RequireLayout(scene);
            Transform oneHandTarget = RequireTarget(layout, OneHandTargetName);
            Transform twoHandTarget = RequireTarget(layout, TwoHandTargetName);
            RootPose oneHandRootBefore = new RootPose(oneHandTarget);
            RootPose twoHandRootBefore = new RootPose(twoHandTarget);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptCarryTargets(layout);
            BakedArmClipResult oneHandBake = CreateOrUpdateAdjustedArmClip(
                oneHandTarget,
                emptyClip,
                oneHandSource,
                OneHandAdjustedClipPath,
                "Hands_Carry_OneHand_ArmAdjusted",
                CarryPoseAdjustmentKind.OneHandLeftArmDown,
                0f,
                false,
                false);
            BakedArmClipResult twoHandBake = CreateOrUpdateAdjustedArmClip(
                twoHandTarget,
                emptyClip,
                twoHandSource,
                TwoHandAdjustedClipPath,
                "Hands_Carry_TwoHand_ArmAdjusted",
                CarryPoseAdjustmentKind.TwoHandRightChest,
                0f,
                false,
                false);
            AvatarMask armsMask = CreateOrUpdateArmsMask(oneHandTarget);
            AnimatorController oneHandController = CreateOrUpdateLayeredCarryController(
                OneHandControllerPath,
                OneHandStateName,
                emptyClip,
                oneHandBake.Clip,
                armsMask);
            AnimatorController twoHandController = CreateOrUpdateLayeredCarryController(
                TwoHandControllerPath,
                TwoHandStateName,
                emptyClip,
                twoHandBake.Clip,
                armsMask);
            Animator oneHandAnimator = ConfigureAnimator(oneHandTarget, oneHandController);
            Animator twoHandAnimator = ConfigureAnimator(twoHandTarget, twoHandController);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string emptyHashAfter = HashFile(EmptyClipPath);
            string oneHandHashAfter = HashFile(OneHandSourcePath);
            string twoHandHashAfter = HashFile(TwoHandSourcePath);
            bool inputsUnchanged =
                string.Equals(emptyHashBefore, emptyHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oneHandHashBefore, oneHandHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(twoHandHashBefore, twoHandHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oneHandHashAfter, OneHandSourceHash, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(twoHandHashAfter, TwoHandSourceHash, StringComparison.OrdinalIgnoreCase);
            PoseAdjustmentTargetApplyMetrics oneHandMetrics =
                CreatePoseAdjustmentTargetApplyMetrics(
                    OneHandTargetName,
                    "왼팔 전체를 자연스럽게 아래로 유지",
                    OneHandAdjustedClipPath,
                    oneHandSource,
                    oneHandBake,
                    oneHandController,
                    oneHandAnimator);
            PoseAdjustmentTargetApplyMetrics twoHandMetrics =
                CreatePoseAdjustmentTargetApplyMetrics(
                    TwoHandTargetName,
                    "두 손 간격을 유지해 캐릭터 오른쪽 가슴으로 이동",
                    TwoHandAdjustedClipPath,
                    twoHandSource,
                    twoHandBake,
                    twoHandController,
                    twoHandAnimator);
            bool rootsUnchanged =
                RootMatches(oneHandTarget, oneHandRootBefore) &&
                RootMatches(twoHandTarget, twoHandRootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptCarryTargets(layout));
            bool animatorSettingsCorrect =
                AnimatorMatches(oneHandAnimator, oneHandController) &&
                AnimatorMatches(twoHandAnimator, twoHandController);
            PoseAdjustmentApplyMetrics metrics = new PoseAdjustmentApplyMetrics
            {
                targetSet = OneHandTargetName + ", " + TwoHandTargetName,
                emptyIdleHashBefore = emptyHashBefore,
                emptyIdleHashAfter = emptyHashAfter,
                oneHandFbxHashBefore = oneHandHashBefore,
                oneHandFbxHashAfter = oneHandHashAfter,
                twoHandFbxHashBefore = twoHandHashBefore,
                twoHandFbxHashAfter = twoHandHashAfter,
                oneHand = oneHandMetrics,
                twoHand = twoHandMetrics,
                inputAnimationsUnchanged = inputsUnchanged,
                rootsUnchanged = rootsUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                animatorSettingsCorrect = animatorSettingsCorrect,
                passedNumericChecks = inputsUnchanged &&
                    rootsUnchanged &&
                    otherAnimatorsUnchanged &&
                    animatorSettingsCorrect &&
                    oneHandMetrics.passedNumericChecks &&
                    twoHandMetrics.passedNumericChecks,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(PoseAdjustmentApplyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands carry pose adjustment apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsCarryPoseAdjustment] Applied continuous arm pose adjustments. " +
                "OneHandShift=" + Num(oneHandBake.RootLocalTranslation.x) + "/" +
                Num(oneHandBake.RootLocalTranslation.y) + "/" +
                Num(oneHandBake.RootLocalTranslation.z) +
                ", TwoHandShift=" + Num(twoHandBake.RootLocalTranslation.x) + "/" +
                Num(twoHandBake.RootLocalTranslation.y) + "/" +
                Num(twoHandBake.RootLocalTranslation.z) +
                ", InputsUnchanged=True, OtherAnimatorsUnchanged=True, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry Pose Adjustment Review")]
        internal static void CaptureCarryPoseAdjustmentReview()
        {
            int stage = SessionState.GetInt(PoseAdjustmentReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Hands carry pose adjustment review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before Hands carry pose adjustment review.");
                    }

                    SessionState.SetInt(PoseAdjustmentReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsCarryPoseAdjustment] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands carry pose adjustment capture requires Play Mode.");
                    }

                    CaptureCarryPoseAdjustmentActualReview();
                    SessionState.SetInt(PoseAdjustmentReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Hands carry pose adjustment review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(PoseAdjustmentReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsCarryPoseAdjustment] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Hands carry pose adjustment review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(PoseAdjustmentReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry Pose Adjustment Final")]
        internal static void CaptureCarryPoseAdjustmentFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Hands carry pose adjustment final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Hands carry pose adjustment final capture.");
            }

            PoseAdjustmentReviewMetrics metrics =
                ReadJson<PoseAdjustmentReviewMetrics>(PoseAdjustmentReviewMetricsPath);
            if (!metrics.passedNumericChecks ||
                !metrics.oneHand.passedNumericChecks ||
                !metrics.twoHand.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands carry pose adjustment review did not pass before final capture.");
            }

            CopyReviewedContact(
                OneHandPoseAdjustmentReviewPath,
                OneHandPoseAdjustmentFinalPath);
            CopyReviewedContact(
                TwoHandPoseAdjustmentReviewPath,
                TwoHandPoseAdjustmentFinalPath);
            Debug.Log(
                "[PlayerHandsCarryPoseAdjustment] Final images copied once from directly reviewed Play Mode frames. " +
                "OneHand=" + Path.GetFullPath(OneHandPoseAdjustmentFinalPath) + ", " +
                "TwoHand=" + Path.GetFullPath(TwoHandPoseAdjustmentFinalPath) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry OneHand Grip Clearance")]
        internal static void ApplyCarryOneHandGripClearance()
        {
            ApplyCarryOneHandGripCorrection(
                GripClearanceReviewStageKey,
                OneHandPoseAdjustmentFinalPath,
                GripClearanceBeforePath,
                GripClearanceApplyMetricsPath,
                "PlayerHandsCarryOneHandGripClearance",
                0f,
                false);
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry OneHand Wrist Grip Correction")]
        internal static void ApplyCarryOneHandWristGripCorrection()
        {
            ApplyCarryOneHandGripCorrection(
                WristGripCorrectionReviewStageKey,
                GripClearanceFinalPath,
                WristGripCorrectionBeforePath,
                WristGripCorrectionApplyMetricsPath,
                "PlayerHandsCarryOneHandWristGripCorrection",
                0f,
                false);
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry OneHand Wrist 180 Flip")]
        internal static void ApplyCarryOneHandWrist180Flip()
        {
            ApplyCarryOneHandGripCorrection(
                Wrist180FlipReviewStageKey,
                WristGripCorrectionFinalPath,
                Wrist180FlipBeforePath,
                Wrist180FlipApplyMetricsPath,
                "PlayerHandsCarryOneHandWrist180Flip",
                180f,
                false);
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry OneHand Natural Vertical Grip")]
        internal static void ApplyCarryOneHandNaturalVerticalGrip()
        {
            ApplyCarryOneHandGripCorrection(
                NaturalVerticalGripReviewStageKey,
                NaturalVerticalGripFinalPath,
                NaturalVerticalGripBeforePath,
                NaturalVerticalGripApplyMetricsPath,
                "PlayerHandsCarryOneHandNaturalVerticalGrip",
                0f,
                true);
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry OneHand Anatomical Wrist Grip")]
        internal static void ApplyCarryOneHandAnatomicalWristGrip()
        {
            ApplyCarryOneHandGripCorrection(
                AnatomicalWristGripReviewStageKey,
                NaturalVerticalGripFinalPath,
                AnatomicalWristGripBeforePath,
                AnatomicalWristGripApplyMetricsPath,
                "PlayerHandsCarryOneHandAnatomicalWristGrip",
                0f,
                true);
        }

        [MenuItem("Bellerophon/Player/Apply Hands Carry OneHand Actual Palm Inward Grip")]
        internal static void ApplyCarryOneHandActualPalmInwardGrip()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(ActualPalmInwardGripReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[PlayerHandsCarryOneHandActualPalmInwardGrip] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before actual-palm OneHand apply.");
            }

            CopyReviewedContact(
                ActualPalmInwardGripFinalPath,
                ActualPalmInwardGripBeforePath);
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip sourceClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, OneHandTargetName);
            BakedArmClipResult bake = CreateOrUpdateAdjustedArmClip(
                target,
                emptyClip,
                sourceClip,
                OneHandAdjustedClipPath,
                "Hands_Carry_OneHand_ArmAdjusted",
                CarryPoseAdjustmentKind.OneHandLeftArmDown,
                0f,
                true,
                false);
            AvatarMask armsMask = CreateOrUpdateArmsMask(target);
            AnimatorController controller = CreateOrUpdateLayeredCarryController(
                OneHandControllerPath,
                OneHandStateName,
                emptyClip,
                bake.Clip,
                armsMask);
            ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            SessionState.EraseInt(ActualPalmInwardGripReviewStageKey);
            Debug.Log(
                "[PlayerHandsCarryOneHandActualPalmInwardGrip] Applied for direct mesh review only.");
        }

        private static void ApplyCarryOneHandGripCorrection(
            string reviewStageKey,
            string beforeSourcePath,
            string beforeOutputPath,
            string applyMetricsPath,
            string logCategory,
            float gripTwistDegrees,
            bool naturalRightArmAdjustment)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.EraseInt(reviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                Debug.Log(
                    "[" + logCategory + "] Exited Play Mode before apply; run apply again in Edit Mode.");
                return;
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before OneHand grip clearance apply.");
            }

            string emptyHashBefore = HashFile(EmptyClipPath);
            string oneHandHashBefore = HashFile(OneHandSourcePath);
            string twoHandClipHashBefore = HashFile(TwoHandAdjustedClipPath);
            string twoHandControllerHashBefore = HashFile(TwoHandControllerPath);
            RequireHash(OneHandSourcePath, OneHandSourceHash, "one-hand carry Unity FBX");
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip sourceClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip currentAdjusted = LoadClip(OneHandAdjustedClipPath);
            string rightUpperCurvesBefore = HashSelectedTransformCurves(
                currentAdjusted,
                RightShoulderPath,
                RightArmPath,
                RightForeArmPath);
            string leftArmCurvesBefore = HashSelectedTransformCurves(
                currentAdjusted,
                LeftShoulderPath,
                LeftArmPath,
                LeftForeArmPath,
                LeftHandPath);
            CopyReviewedContact(
                beforeSourcePath,
                beforeOutputPath);

            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, OneHandTargetName);
            RootPose rootBefore = new RootPose(target);
            Dictionary<string, string> otherAnimatorsBefore =
                CaptureAnimatorsExceptTarget(layout, OneHandTargetName);
            BakedArmClipResult bake = CreateOrUpdateAdjustedArmClip(
                target,
                emptyClip,
                sourceClip,
                OneHandAdjustedClipPath,
                "Hands_Carry_OneHand_ArmAdjusted",
                CarryPoseAdjustmentKind.OneHandLeftArmDown,
                gripTwistDegrees,
                naturalRightArmAdjustment,
                false);
            AvatarMask armsMask = CreateOrUpdateArmsMask(target);
            AnimatorController controller = CreateOrUpdateLayeredCarryController(
                OneHandControllerPath,
                OneHandStateName,
                emptyClip,
                bake.Clip,
                armsMask);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string emptyHashAfter = HashFile(EmptyClipPath);
            string oneHandHashAfter = HashFile(OneHandSourcePath);
            string twoHandClipHashAfter = HashFile(TwoHandAdjustedClipPath);
            string twoHandControllerHashAfter = HashFile(TwoHandControllerPath);
            string rightUpperCurvesAfter = HashSelectedTransformCurves(
                bake.Clip,
                RightShoulderPath,
                RightArmPath,
                RightForeArmPath);
            string leftArmCurvesAfter = HashSelectedTransformCurves(
                bake.Clip,
                LeftShoulderPath,
                LeftArmPath,
                LeftForeArmPath,
                LeftHandPath);
            bool inputAnimationsUnchanged =
                string.Equals(emptyHashBefore, emptyHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oneHandHashBefore, oneHandHashAfter, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(oneHandHashAfter, OneHandSourceHash, StringComparison.OrdinalIgnoreCase);
            bool twoHandUnchanged =
                string.Equals(
                    twoHandClipHashBefore,
                    twoHandClipHashAfter,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    twoHandControllerHashBefore,
                    twoHandControllerHashAfter,
                    StringComparison.OrdinalIgnoreCase);
            bool rightUpperUnchanged = string.Equals(
                rightUpperCurvesBefore,
                rightUpperCurvesAfter,
                StringComparison.OrdinalIgnoreCase);
            bool leftArmUnchanged = string.Equals(
                leftArmCurvesBefore,
                leftArmCurvesAfter,
                StringComparison.OrdinalIgnoreCase);
            bool controllerUses = controller.layers.Length == 2 &&
                LayerStateUsesClip(
                    controller.layers[1],
                    OneHandStateName,
                    bake.Clip);
            bool rootUnchanged = RootMatches(target, rootBefore);
            bool otherAnimatorsUnchanged = DictionariesEqual(
                otherAnimatorsBefore,
                CaptureAnimatorsExceptTarget(layout, OneHandTargetName));
            GripClearanceApplyMetrics metrics = new GripClearanceApplyMetrics
            {
                target = OneHandTargetName,
                expectedGripTwistDegrees = gripTwistDegrees,
                emptyIdleHashBefore = emptyHashBefore,
                emptyIdleHashAfter = emptyHashAfter,
                oneHandFbxHashBefore = oneHandHashBefore,
                oneHandFbxHashAfter = oneHandHashAfter,
                twoHandAdjustedHashBefore = twoHandClipHashBefore,
                twoHandAdjustedHashAfter = twoHandClipHashAfter,
                twoHandControllerHashBefore = twoHandControllerHashBefore,
                twoHandControllerHashAfter = twoHandControllerHashAfter,
                rightUpperArmCurvesHashBefore = rightUpperCurvesBefore,
                rightUpperArmCurvesHashAfter = rightUpperCurvesAfter,
                leftArmCurvesHashBefore = leftArmCurvesBefore,
                leftArmCurvesHashAfter = leftArmCurvesAfter,
                targetReachErrorMax = bake.TargetReachErrorMax,
                adjustedClipLoops =
                    AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime,
                controllerUsesAdjustedClip = controllerUses,
                rootUnchanged = rootUnchanged,
                otherAnimatorsUnchanged = otherAnimatorsUnchanged,
                inputAnimationsUnchanged = inputAnimationsUnchanged,
                twoHandAssetsUnchanged = twoHandUnchanged,
                rightShoulderArmForeArmCurvesUnchanged = rightUpperUnchanged,
                leftArmCurvesUnchanged = leftArmUnchanged,
                naturalRightArmAdjustment = naturalRightArmAdjustment,
                rightArmAdjustmentApplied = !rightUpperUnchanged,
                applyRootMotion = animator.applyRootMotion,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            metrics.passedNumericChecks =
                metrics.adjustedClipLoops &&
                controllerUses &&
                rootUnchanged &&
                otherAnimatorsUnchanged &&
                inputAnimationsUnchanged &&
                twoHandUnchanged &&
                leftArmUnchanged &&
                (naturalRightArmAdjustment
                    ? !rightUpperUnchanged
                    : rightUpperUnchanged) &&
                AdjustedClipOnlyContainsArmCurves(bake.Clip) &&
                bake.TargetReachErrorMax <= 0.005f &&
                !animator.applyRootMotion;
            WriteJson(applyMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "OneHand grip clearance apply support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[" + logCategory + "] Applied vertical grip adjustment twist=" +
                Num(gripTwistDegrees) + " degrees. " +
                "ReachError=" + Num(bake.TargetReachErrorMax) +
                ", NaturalRightArm=" + naturalRightArmAdjustment +
                ", LeftArmUnchanged=True, TwoHandUnchanged=True, ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Grip Clearance Review")]
        internal static void CaptureCarryOneHandGripClearanceReview()
        {
            CaptureCarryOneHandGripCorrectionReview(
                GripClearanceReviewStageKey,
                GripClearanceApplyMetricsPath,
                GripClearanceReviewMetricsPath,
                GripClearanceReviewPath,
                "PlayerHandsCarryOneHandGripClearance",
                0f,
                false);
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Wrist Grip Correction Review")]
        internal static void CaptureCarryOneHandWristGripCorrectionReview()
        {
            CaptureCarryOneHandGripCorrectionReview(
                WristGripCorrectionReviewStageKey,
                WristGripCorrectionApplyMetricsPath,
                WristGripCorrectionReviewMetricsPath,
                WristGripCorrectionReviewPath,
                "PlayerHandsCarryOneHandWristGripCorrection",
                0f,
                false);
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Wrist 180 Flip Review")]
        internal static void CaptureCarryOneHandWrist180FlipReview()
        {
            CaptureCarryOneHandGripCorrectionReview(
                Wrist180FlipReviewStageKey,
                Wrist180FlipApplyMetricsPath,
                Wrist180FlipReviewMetricsPath,
                Wrist180FlipReviewPath,
                "PlayerHandsCarryOneHandWrist180Flip",
                180f,
                false);
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Natural Vertical Grip Review")]
        internal static void CaptureCarryOneHandNaturalVerticalGripReview()
        {
            CaptureCarryOneHandGripCorrectionReview(
                NaturalVerticalGripReviewStageKey,
                NaturalVerticalGripApplyMetricsPath,
                NaturalVerticalGripReviewMetricsPath,
                NaturalVerticalGripReviewPath,
                "PlayerHandsCarryOneHandNaturalVerticalGrip",
                0f,
                true);
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Anatomical Wrist Grip Review")]
        internal static void CaptureCarryOneHandAnatomicalWristGripReview()
        {
            CaptureCarryOneHandGripCorrectionReview(
                AnatomicalWristGripReviewStageKey,
                AnatomicalWristGripApplyMetricsPath,
                AnatomicalWristGripReviewMetricsPath,
                AnatomicalWristGripReviewPath,
                "PlayerHandsCarryOneHandAnatomicalWristGrip",
                0f,
                true);
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Actual Palm Inward Grip Review")]
        internal static void CaptureCarryOneHandActualPalmInwardGripReview()
        {
            int stage = SessionState.GetInt(ActualPalmInwardGripReviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "Actual-palm OneHand review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before actual-palm OneHand review.");
                    }

                    SessionState.SetInt(ActualPalmInwardGripReviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[PlayerHandsCarryOneHandActualPalmInwardGrip] Entering Play Mode for direct mesh review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Actual-palm OneHand capture requires Play Mode.");
                    }

                    CaptureCarryOneHandActualPalmInwardGripActualReview();
                    SessionState.SetInt(ActualPalmInwardGripReviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Actual-palm OneHand review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(ActualPalmInwardGripReviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[PlayerHandsCarryOneHandActualPalmInwardGrip] Exiting Play Mode after direct mesh review.");
                    return;
                }

                throw new InvalidOperationException(
                    "Actual-palm OneHand review stage is invalid.");
            }
            catch
            {
                SessionState.EraseInt(ActualPalmInwardGripReviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        private static void CaptureCarryOneHandActualPalmInwardGripActualReview()
        {
            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, OneHandTargetName);
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip sourceClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip adjustedClip = LoadClip(OneHandAdjustedClipPath);
            CaptureCarryPoseAdjustmentComparison(
                target,
                emptyClip,
                sourceClip,
                adjustedClip,
                OneHandStateName,
                ActualPalmInwardGripReviewPath);
            Debug.Log(
                "[PlayerHandsCarryOneHandActualPalmInwardGrip] Captured unmodified Play Mode frames for direct mesh review only.");
        }

        private static void CaptureCarryOneHandGripCorrectionReview(
            string reviewStageKey,
            string applyMetricsPath,
            string reviewMetricsPath,
            string reviewPath,
            string logCategory,
            float expectedGripTwistDegrees,
            bool naturalRightArmAdjustment,
            bool palmFacingCharacterLeft = false)
        {
            int stage = SessionState.GetInt(reviewStageKey, 0);
            try
            {
                if (stage == 0)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        throw new InvalidOperationException(
                            "OneHand grip clearance review must start in Edit Mode.");
                    }

                    Scene scene = RequireScene();
                    if (scene.isDirty)
                    {
                        throw new InvalidOperationException(
                            "CargoRunMvp must be clean before OneHand grip clearance review.");
                    }

                    SessionState.SetInt(reviewStageKey, 1);
                    EditorApplication.EnterPlaymode();
                    Debug.Log(
                        "[" + logCategory + "] Entering Play Mode for direct review.");
                    return;
                }

                if (stage == 1)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "OneHand grip clearance capture requires Play Mode.");
                    }

                    CaptureCarryOneHandGripCorrectionActualReview(
                        applyMetricsPath,
                        reviewMetricsPath,
                        reviewPath,
                        logCategory,
                        expectedGripTwistDegrees,
                        naturalRightArmAdjustment,
                        palmFacingCharacterLeft);
                    SessionState.SetInt(reviewStageKey, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "OneHand grip clearance review exit requires Play Mode.");
                    }

                    SessionState.EraseInt(reviewStageKey);
                    EditorApplication.ExitPlaymode();
                    Debug.Log(
                        "[" + logCategory + "] Exiting Play Mode after direct review.");
                    return;
                }

                throw new InvalidOperationException(
                    "OneHand grip clearance review stage is invalid: " +
                    stage.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch
            {
                SessionState.EraseInt(reviewStageKey);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Grip Clearance Final")]
        internal static void CaptureCarryOneHandGripClearanceFinal()
        {
            CaptureCarryOneHandGripCorrectionFinal(
                GripClearanceReviewMetricsPath,
                GripClearanceReviewPath,
                GripClearanceFinalPath,
                "PlayerHandsCarryOneHandGripClearance");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Wrist Grip Correction Final")]
        internal static void CaptureCarryOneHandWristGripCorrectionFinal()
        {
            CaptureCarryOneHandGripCorrectionFinal(
                WristGripCorrectionReviewMetricsPath,
                WristGripCorrectionReviewPath,
                WristGripCorrectionFinalPath,
                "PlayerHandsCarryOneHandWristGripCorrection");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Wrist 180 Flip Final")]
        internal static void CaptureCarryOneHandWrist180FlipFinal()
        {
            CaptureCarryOneHandGripCorrectionFinal(
                Wrist180FlipReviewMetricsPath,
                Wrist180FlipReviewPath,
                Wrist180FlipFinalPath,
                "PlayerHandsCarryOneHandWrist180Flip");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Natural Vertical Grip Final")]
        internal static void CaptureCarryOneHandNaturalVerticalGripFinal()
        {
            CaptureCarryOneHandGripCorrectionFinal(
                NaturalVerticalGripReviewMetricsPath,
                NaturalVerticalGripReviewPath,
                NaturalVerticalGripFinalPath,
                "PlayerHandsCarryOneHandNaturalVerticalGrip");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Anatomical Wrist Grip Final")]
        internal static void CaptureCarryOneHandAnatomicalWristGripFinal()
        {
            CaptureCarryOneHandGripCorrectionFinal(
                AnatomicalWristGripReviewMetricsPath,
                AnatomicalWristGripReviewPath,
                AnatomicalWristGripFinalPath,
                "PlayerHandsCarryOneHandAnatomicalWristGrip");
        }

        [MenuItem("Bellerophon/Player/Capture Hands Carry OneHand Actual Palm Inward Grip Final")]
        internal static void CaptureCarryOneHandActualPalmInwardGripFinal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Actual-palm OneHand final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp changed after direct actual-palm review.");
            }

            CopyReviewedContact(
                ActualPalmInwardGripReviewPath,
                ActualPalmInwardGripFinalPath);
            Debug.Log(
                "[PlayerHandsCarryOneHandActualPalmInwardGrip] Final image copied from directly reviewed Play Mode frames. SceneChanged=False.");
        }

        private static void CaptureCarryOneHandGripCorrectionFinal(
            string reviewMetricsPath,
            string reviewPath,
            string finalPath,
            string logCategory)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "OneHand grip clearance final capture requires Edit Mode.");
            }

            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before OneHand grip clearance final capture.");
            }

            GripClearanceReviewMetrics metrics =
                ReadJson<GripClearanceReviewMetrics>(reviewMetricsPath);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "OneHand grip clearance review did not pass before final capture.");
            }

            CopyReviewedContact(reviewPath, finalPath);
            Debug.Log(
                "[" + logCategory + "] Final image copied once from directly reviewed Play Mode frames. " +
                "OneHand=" + Path.GetFullPath(finalPath) +
                ", SceneChanged=False.");
        }

        private static void CaptureCarryOneHandGripCorrectionActualReview(
            string applyMetricsPath,
            string reviewMetricsPath,
            string reviewPath,
            string logCategory,
            float expectedGripTwistDegrees,
            bool naturalRightArmAdjustment,
            bool palmFacingCharacterLeft = false)
        {
            GripClearanceApplyMetrics apply =
                ReadJson<GripClearanceApplyMetrics>(applyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "OneHand grip clearance apply metrics did not pass.");
            }
            if (Mathf.Abs(
                    apply.expectedGripTwistDegrees - expectedGripTwistDegrees) > 0.001f ||
                apply.naturalRightArmAdjustment != naturalRightArmAdjustment)
            {
                throw new InvalidOperationException(
                    "OneHand grip review parameters do not match the applied clip.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, OneHandTargetName);
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip sourceClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip adjustedClip = LoadClip(OneHandAdjustedClipPath);
            CaptureCarryPoseAdjustmentComparison(
                target,
                emptyClip,
                sourceClip,
                adjustedClip,
                OneHandStateName,
                reviewPath);
            GripClearanceReviewMetrics metrics = CaptureGripClearanceMetrics(
                target,
                emptyClip,
                sourceClip,
                adjustedClip,
                expectedGripTwistDegrees,
                naturalRightArmAdjustment,
                palmFacingCharacterLeft);
            metrics.passedNumericChecks = palmFacingCharacterLeft
                ? PalmLeftReviewPassed(metrics)
                : GripClearanceReviewPassed(
                    metrics,
                    naturalRightArmAdjustment);
            metrics.validationPriority =
                "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증";
            WriteJson(reviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "OneHand grip clearance Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[" + logCategory + "] Captured actual Play Mode comparison. " +
                "Frames=" + metrics.framesSampled +
                ", LeftForeArmClearance=" + Num(metrics.leftForeArmOutsideSpineMetersMin) +
                ", VerticalGrip=" + Num(metrics.verticalGripAngleDegreesMax) +
                ", PalmFromInward=" +
                Num(metrics.palmFromInwardAngleDegreesMin) + ".." +
                Num(metrics.palmInwardAngleDegreesMax) +
                ", PalmTargetError=" + Num(metrics.palmTargetAngleDegreesMax) +
                ", RightElbowOutside/Below=" +
                Num(metrics.rightElbowOutsideSpineMetersMin) + "/" +
                Num(metrics.rightElbowBelowShoulderMetersMin) +
                ", ForeArmWristAlignment=" +
                Num(metrics.rightForeArmWristAlignmentDegreesMax) +
                ", WristLocalDelta=" +
                Num(metrics.rightWristLocalRotationDifferenceDegreesMax) +
                ", RightUpperPose=" +
                Num(metrics.rightShoulderArmForeArmPositionDifferenceMax) + "/" +
                Num(metrics.rightShoulderArmForeArmRotationDifferenceDegreesMax) + ".");
        }

        private static void CaptureCarryPoseAdjustmentActualReview()
        {
            PoseAdjustmentApplyMetrics apply =
                ReadJson<PoseAdjustmentApplyMetrics>(PoseAdjustmentApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands carry pose adjustment apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform oneHandTarget = RequireTarget(layout, OneHandTargetName);
            Transform twoHandTarget = RequireTarget(layout, TwoHandTargetName);
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip oneHandSource = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip twoHandSource = LoadSingleEmbeddedClip(
                TwoHandSourcePath,
                "two-hand carry");
            AnimationClip oneHandAdjusted = LoadClip(OneHandAdjustedClipPath);
            AnimationClip twoHandAdjusted = LoadClip(TwoHandAdjustedClipPath);
            CaptureCarryPoseAdjustmentComparison(
                oneHandTarget,
                emptyClip,
                oneHandSource,
                oneHandAdjusted,
                OneHandStateName,
                OneHandPoseAdjustmentReviewPath);
            CaptureCarryPoseAdjustmentComparison(
                twoHandTarget,
                emptyClip,
                twoHandSource,
                twoHandAdjusted,
                TwoHandStateName,
                TwoHandPoseAdjustmentReviewPath);
            PoseAdjustmentTargetReviewMetrics oneHand =
                CaptureCarryPoseAdjustmentMetrics(
                    oneHandTarget,
                    emptyClip,
                    oneHandSource,
                    oneHandAdjusted,
                    OneHandStateName,
                    CarryPoseAdjustmentKind.OneHandLeftArmDown);
            PoseAdjustmentTargetReviewMetrics twoHand =
                CaptureCarryPoseAdjustmentMetrics(
                    twoHandTarget,
                    emptyClip,
                    twoHandSource,
                    twoHandAdjusted,
                    TwoHandStateName,
                    CarryPoseAdjustmentKind.TwoHandRightChest);
            oneHand.passedNumericChecks = PoseAdjustmentTargetReviewPassed(
                oneHand,
                CarryPoseAdjustmentKind.OneHandLeftArmDown);
            twoHand.passedNumericChecks = PoseAdjustmentTargetReviewPassed(
                twoHand,
                CarryPoseAdjustmentKind.TwoHandRightChest);
            PoseAdjustmentReviewMetrics metrics = new PoseAdjustmentReviewMetrics
            {
                targetSet = OneHandTargetName + ", " + TwoHandTargetName,
                oneHand = oneHand,
                twoHand = twoHand,
                passedNumericChecks =
                    oneHand.passedNumericChecks && twoHand.passedNumericChecks,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(PoseAdjustmentReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands carry pose adjustment Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsCarryPoseAdjustment] Captured actual Play Mode comparisons. " +
                "OneHandFrames=" + oneHand.framesSampled +
                ", LeftDown=" + Num(oneHand.leftHandBelowShoulderArmLengthsMin) +
                ", TwoHandFrames=" + twoHand.framesSampled +
                ", RightChest=" + Num(twoHand.handCenterRightShoulderSpansMin) +
                ", Spacing=" + Num(twoHand.handSpacingDifferenceMax) +
                ", BaseAndArmLoopsAtLeast=2.");
        }

        private static void CaptureCarryBodyAlignmentActualReview()
        {
            AlignmentApplyMetrics apply =
                ReadJson<AlignmentApplyMetrics>(AlignmentApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands carry body alignment apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform oneHandTarget = RequireTarget(layout, OneHandTargetName);
            Transform twoHandTarget = RequireTarget(layout, TwoHandTargetName);
            AnimationClip emptyClip = LoadClip(EmptyClipPath);
            AnimationClip oneHandClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip twoHandClip = LoadSingleEmbeddedClip(
                TwoHandSourcePath,
                "two-hand carry");
            CaptureCarryAlignmentComparison(
                oneHandTarget,
                emptyClip,
                oneHandClip,
                OneHandStateName,
                OneHandAlignmentReviewPath);
            CaptureCarryAlignmentComparison(
                twoHandTarget,
                emptyClip,
                twoHandClip,
                TwoHandStateName,
                TwoHandAlignmentReviewPath);
            AlignmentTargetReviewMetrics oneHand = CaptureCarryAlignmentMetrics(
                oneHandTarget,
                emptyClip,
                oneHandClip,
                OneHandStateName);
            AlignmentTargetReviewMetrics twoHand = CaptureCarryAlignmentMetrics(
                twoHandTarget,
                emptyClip,
                twoHandClip,
                TwoHandStateName);
            oneHand.passedNumericChecks = AlignmentTargetReviewPassed(oneHand);
            twoHand.passedNumericChecks = AlignmentTargetReviewPassed(twoHand);
            AlignmentReviewMetrics metrics = new AlignmentReviewMetrics
            {
                targetSet = OneHandTargetName + ", " + TwoHandTargetName,
                oneHand = oneHand,
                twoHand = twoHand,
                passedNumericChecks =
                    oneHand.passedNumericChecks && twoHand.passedNumericChecks,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(AlignmentReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands carry body alignment Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsCarryBodyAlignment] Captured actual layered Play Mode comparisons. " +
                "OneHandFrames=" + oneHand.framesSampled +
                ", OneBody=" + Num(oneHand.bodyPositionDifferenceMax) +
                "/" + Num(oneHand.bodyRotationDifferenceDegreesMax) +
                ", OneArms=" + Num(oneHand.armPositionDifferenceMax) +
                "/" + Num(oneHand.armRotationDifferenceDegreesMax) +
                ", TwoHandFrames=" + twoHand.framesSampled +
                ", TwoBody=" + Num(twoHand.bodyPositionDifferenceMax) +
                "/" + Num(twoHand.bodyRotationDifferenceDegreesMax) +
                ", TwoArms=" + Num(twoHand.armPositionDifferenceMax) +
                "/" + Num(twoHand.armRotationDifferenceDegreesMax) +
                ", BaseAndArmLoopsAtLeast=2.");
        }

        private static void CaptureCarryOneHandEmbeddedTakeExactActualReview()
        {
            OneHandEmbeddedTakeApplyMetrics apply =
                ReadJson<OneHandEmbeddedTakeApplyMetrics>(
                    OneHandEmbeddedTakeApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Exact OneHand embedded Take apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, OneHandTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            CaptureTargetComparison(
                target,
                source,
                OneHandStateName,
                OneHandEmbeddedTakeReviewPath);
            TargetReviewMetrics oneHand = CaptureTargetMetrics(
                target,
                source,
                OneHandStateName,
                source.name);
            oneHand.passedNumericChecks = TargetReviewPassed(oneHand);
            OneHandEmbeddedTakeReviewMetrics metrics =
                new OneHandEmbeddedTakeReviewMetrics
                {
                    targetSet = OneHandTargetName,
                    oneHand = oneHand,
                    passedNumericChecks = oneHand.passedNumericChecks,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteJson(OneHandEmbeddedTakeReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Exact OneHand embedded Take Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsCarryOneHandEmbeddedTake] Captured exact embedded Take comparison in Play Mode. " +
                "Frames=" + oneHand.framesSampled +
                ", Pose=" + Num(oneHand.sourcePosePositionDifferenceMax) +
                "/" + Num(oneHand.sourcePoseRotationDifferenceDegreesMax) +
                ", Root=" + Num(oneHand.rootPositionDisplacementMax) +
                ", Loops=2.");
        }

        private static void ConfigurePlayerStartFacingEmpty(
            Scene scene,
            Transform emptyTarget,
            out Transform playerRoot,
            out Transform playerCamera,
            out Bounds emptyBounds)
        {
            Camera[] mainCameras = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .Where(camera => camera.CompareTag("MainCamera"))
                .ToArray();
            if (mainCameras.Length != 1)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp MainCamera count differs; actual=" +
                    mainCameras.Length + ".");
            }

            playerCamera = mainCameras[0].transform;
            CharacterController controller =
                playerCamera.GetComponentInParent<CharacterController>();
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp runtime Player CharacterController is missing.");
            }

            playerRoot = controller.transform;
            Renderer[] renderers = emptyTarget
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Hands_Empty_Idle has no visible bounds for Player start alignment.");
            }

            emptyBounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                emptyBounds.Encapsulate(renderer.bounds);
            }

            Vector3 frontDirection = Vector3.ProjectOnPlane(
                emptyTarget.forward,
                Vector3.up).normalized;
            if (frontDirection.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    "Hands_Empty_Idle has no usable horizontal front direction.");
            }

            const float viewDistance = 5.4f;
            Vector3 desiredCameraPlanar =
                emptyBounds.center + frontDirection * viewDistance;
            Vector3 currentCameraOffset = playerCamera.position - playerRoot.position;
            playerRoot.position = new Vector3(
                desiredCameraPlanar.x - currentCameraOffset.x,
                emptyTarget.position.y,
                desiredCameraPlanar.z - currentCameraOffset.z);
            playerRoot.rotation = Quaternion.LookRotation(
                -frontDirection,
                Vector3.up);
            Quaternion desiredCameraWorldRotation = Quaternion.LookRotation(
                emptyBounds.center - playerCamera.position,
                Vector3.up);
            playerCamera.localRotation =
                Quaternion.Inverse(playerRoot.rotation) * desiredCameraWorldRotation;
            EditorUtility.SetDirty(playerRoot);
            EditorUtility.SetDirty(playerCamera);
        }

        private static int CountSharedPlayerModelInstances(Scene scene)
        {
            const string playerModelPath = "Assets/_Project/Art/Player/player.fbx";
            SkinnedMeshRenderer[] modelRenderers = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .Where(renderer =>
                    renderer.sharedMesh != null &&
                    string.Equals(
                        AssetDatabase.GetAssetPath(renderer.sharedMesh),
                        playerModelPath,
                        StringComparison.Ordinal))
                .ToArray();
            if (modelRenderers.Length > 0)
            {
                return modelRenderers.Length;
            }

            HashSet<GameObject> roots = new HashSet<GameObject>();
            foreach (Transform transform in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                string assetPath =
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        transform.gameObject);
                if (!string.Equals(assetPath, playerModelPath, StringComparison.Ordinal))
                {
                    continue;
                }

                GameObject instanceRoot =
                    PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
                if (instanceRoot != null)
                {
                    roots.Add(instanceRoot);
                }
            }

            return roots.Count;
        }

        private static bool AllSharedPlayerInstancesUseTransporterTexture(Scene scene)
        {
            const string playerModelPath = "Assets/_Project/Art/Player/player.fbx";
            SkinnedMeshRenderer[] modelRenderers = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .Where(renderer =>
                    renderer.sharedMesh != null &&
                    string.Equals(
                        AssetDatabase.GetAssetPath(renderer.sharedMesh),
                        playerModelPath,
                        StringComparison.Ordinal))
                .ToArray();
            if (modelRenderers.Length > 0)
            {
                return modelRenderers.All(renderer =>
                    renderer.sharedMaterials
                        .Where(material => material != null)
                        .Any(material =>
                        {
                            Texture texture = material.mainTexture;
                            return texture != null && string.Equals(
                                AssetDatabase.GetAssetPath(texture),
                                TransporterTexturePath,
                                StringComparison.Ordinal);
                        }));
            }

            HashSet<GameObject> roots = new HashSet<GameObject>();
            foreach (Transform transform in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<Transform>(true)))
            {
                if (!string.Equals(
                        PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                            transform.gameObject),
                        playerModelPath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                GameObject instanceRoot =
                    PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
                if (instanceRoot != null)
                {
                    roots.Add(instanceRoot);
                }
            }

            return roots.Count > 0 && roots.All(root =>
                root.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Any(material =>
                    {
                        Texture texture = material.mainTexture;
                        return texture != null && string.Equals(
                            AssetDatabase.GetAssetPath(texture),
                            TransporterTexturePath,
                            StringComparison.Ordinal);
                    }));
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private sealed class LeftArmUvTriangle
        {
            internal int A;
            internal int B;
            internal int C;
            internal Vector2 UvA;
            internal Vector2 UvB;
            internal Vector2 UvC;
            internal int PixelCount;
            internal int RedCount;
            internal int WhiteCount;
            internal bool Seed;
            internal bool Selected;
        }

        private static TransporterTextureEditResult
            ApplySharedTransporterLeftArmFlagTexture(Transform target)
        {
            string baselineAbsolute = Path.GetFullPath(TransporterTextureBaselinePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(baselineAbsolute) ??
                throw new InvalidOperationException(
                    "Transporter texture validation directory is unavailable."));
            if (!File.Exists(baselineAbsolute))
            {
                File.Copy(
                    Path.GetFullPath(TransporterTexturePath),
                    baselineAbsolute,
                    false);
            }

            byte[] baselineBytes = File.ReadAllBytes(baselineAbsolute);
            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false,
                false);
            if (!texture.LoadImage(baselineBytes, false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(
                    "Transporter baseline texture could not be decoded.");
            }

            try
            {
                SkinnedMeshRenderer renderer = target
                    .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .FirstOrDefault(candidate =>
                        candidate.sharedMesh != null &&
                        candidate.sharedMaterials.Any(material =>
                            material != null &&
                            string.Equals(
                                AssetDatabase.GetAssetPath(material),
                                "Assets/_Project/Art/Player/Materials/Material_1.mat",
                                StringComparison.Ordinal)));
                if (renderer == null)
                {
                    throw new InvalidOperationException(
                        "Shared transporter SkinnedMeshRenderer using Material_1 is missing.");
                }

                Transform leftArm = FindRequired(target, LeftArmPath);
                int leftArmBoneIndex = Array.IndexOf(renderer.bones, leftArm);
                if (leftArmBoneIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Shared transporter mesh has no LeftArm bone binding.");
                }

                Mesh mesh = renderer.sharedMesh;
                Vector2[] uvs = mesh.uv;
                BoneWeight[] boneWeights = mesh.boneWeights;
                int[] triangleIndices = mesh.triangles;
                if (uvs.Length != mesh.vertexCount ||
                    boneWeights.Length != mesh.vertexCount)
                {
                    throw new InvalidOperationException(
                        "Shared transporter mesh UV or bone-weight data is unavailable.");
                }

                Color32[] pixels = texture.GetPixels32();
                List<LeftArmUvTriangle> triangles = new List<LeftArmUvTriangle>();
                Dictionary<int, List<int>> trianglesByVertex =
                    new Dictionary<int, List<int>>();
                for (int index = 0; index < triangleIndices.Length; index += 3)
                {
                    int a = triangleIndices[index];
                    int b = triangleIndices[index + 1];
                    int c = triangleIndices[index + 2];
                    float leftArmWeight = Mathf.Max(
                        BoneWeightForIndex(boneWeights[a], leftArmBoneIndex),
                        Mathf.Max(
                            BoneWeightForIndex(boneWeights[b], leftArmBoneIndex),
                            BoneWeightForIndex(boneWeights[c], leftArmBoneIndex)));
                    if (leftArmWeight < 0.2f)
                    {
                        continue;
                    }

                    LeftArmUvTriangle triangle = new LeftArmUvTriangle
                    {
                        A = a,
                        B = b,
                        C = c,
                        UvA = uvs[a],
                        UvB = uvs[b],
                        UvC = uvs[c]
                    };
                    RasterizeUvTriangle(
                        triangle,
                        texture.width,
                        texture.height,
                        pixelIndex =>
                        {
                            Color32 color = pixels[pixelIndex];
                            triangle.PixelCount++;
                            if (IsFlagRed(color))
                            {
                                triangle.RedCount++;
                            }

                            if (IsFlagWhite(color))
                            {
                                triangle.WhiteCount++;
                            }
                        });
                    int triangleListIndex = triangles.Count;
                    triangles.Add(triangle);
                    AddTriangleVertexReference(
                        trianglesByVertex,
                        a,
                        triangleListIndex);
                    AddTriangleVertexReference(
                        trianglesByVertex,
                        b,
                        triangleListIndex);
                    AddTriangleVertexReference(
                        trianglesByVertex,
                        c,
                        triangleListIndex);
                }

                foreach (LeftArmUvTriangle triangle in triangles)
                {
                    int minimumRed = Mathf.Max(
                        2,
                        Mathf.CeilToInt(triangle.PixelCount * 0.012f));
                    int minimumWhite = Mathf.Max(
                        2,
                        Mathf.CeilToInt(triangle.PixelCount * 0.02f));
                    triangle.Seed =
                        triangle.RedCount >= minimumRed &&
                        triangle.WhiteCount >= minimumWhite;
                    triangle.Selected = triangle.Seed;
                }

                int seedCount = triangles.Count(triangle => triangle.Seed);
                if (seedCount == 0)
                {
                    throw new InvalidOperationException(
                        "No United States flag-colored UV triangles were found on LeftArm.");
                }

                HashSet<int> firstRing = new HashSet<int>();
                for (int index = 0; index < triangles.Count; index++)
                {
                    if (!triangles[index].Seed)
                    {
                        continue;
                    }

                    AddAdjacentTriangleIndices(
                        triangles[index],
                        trianglesByVertex,
                        firstRing);
                }

                foreach (int index in firstRing)
                {
                    triangles[index].Selected = true;
                }

                HashSet<int> secondRing = new HashSet<int>();
                foreach (int index in firstRing)
                {
                    AddAdjacentTriangleIndices(
                        triangles[index],
                        trianglesByVertex,
                        secondRing);
                }

                foreach (int index in secondRing)
                {
                    LeftArmUvTriangle triangle = triangles[index];
                    float flagLightRatio = triangle.PixelCount <= 0
                        ? 0f
                        : (triangle.RedCount + triangle.WhiteCount) /
                          (float)triangle.PixelCount;
                    if (flagLightRatio >= 0.12f)
                    {
                        triangle.Selected = true;
                    }
                }

                HashSet<int> recoloredPixels = new HashSet<int>();
                Color targetLightPurple = Color.HSVToRGB(0.76f, 0.3f, 0.78f);
                foreach (LeftArmUvTriangle triangle in
                         triangles.Where(candidate => candidate.Selected))
                {
                    RasterizeUvTriangle(
                        triangle,
                        texture.width,
                        texture.height,
                        pixelIndex => recoloredPixels.Add(pixelIndex));
                }

                foreach (int pixelIndex in recoloredPixels)
                {
                    Color original = pixels[pixelIndex];
                    Color.RGBToHSV(original, out _, out _, out float value);
                    float preservedValue = Mathf.Clamp01(
                        Mathf.Lerp(value, 0.78f, 0.38f));
                    Color tinted = Color.HSVToRGB(0.76f, 0.3f, preservedValue);
                    tinted.a = original.a;
                    pixels[pixelIndex] = tinted;
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                byte[] output = texture.EncodeToPNG();
                File.WriteAllBytes(Path.GetFullPath(TransporterTexturePath), output);
                File.WriteAllBytes(
                    Path.GetFullPath(TransporterTextureDuplicatePath),
                    output);
                AssetDatabase.ImportAsset(
                    TransporterTexturePath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(
                    TransporterTextureDuplicatePath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                return new TransporterTextureEditResult
                {
                    LeftArmTrianglesScanned = triangles.Count,
                    FlagSeedTriangleCount = seedCount,
                    FlagPatchTriangleCount = triangles.Count(
                        triangle => triangle.Selected),
                    RecoloredPixelCount = recoloredPixels.Count,
                    TargetLightPurple = targetLightPurple
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static float BoneWeightForIndex(BoneWeight weight, int boneIndex)
        {
            float result = 0f;
            if (weight.boneIndex0 == boneIndex)
            {
                result = Mathf.Max(result, weight.weight0);
            }

            if (weight.boneIndex1 == boneIndex)
            {
                result = Mathf.Max(result, weight.weight1);
            }

            if (weight.boneIndex2 == boneIndex)
            {
                result = Mathf.Max(result, weight.weight2);
            }

            if (weight.boneIndex3 == boneIndex)
            {
                result = Mathf.Max(result, weight.weight3);
            }

            return result;
        }

        private static void RasterizeUvTriangle(
            LeftArmUvTriangle triangle,
            int width,
            int height,
            Action<int> visit)
        {
            Vector2 a = new Vector2(
                triangle.UvA.x * (width - 1),
                triangle.UvA.y * (height - 1));
            Vector2 b = new Vector2(
                triangle.UvB.x * (width - 1),
                triangle.UvB.y * (height - 1));
            Vector2 c = new Vector2(
                triangle.UvC.x * (width - 1),
                triangle.UvC.y * (height - 1));
            int minX = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))),
                0,
                width - 1);
            int maxX = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))),
                0,
                width - 1);
            int minY = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))),
                0,
                height - 1);
            int maxY = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))),
                0,
                height - 1);
            float area = Cross2D(b - a, c - a);
            if (Mathf.Abs(area) <= 0.0001f)
            {
                return;
            }

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float first = Cross2D(b - a, point - a);
                    float second = Cross2D(c - b, point - b);
                    float third = Cross2D(a - c, point - c);
                    bool inside = area > 0f
                        ? first >= 0f && second >= 0f && third >= 0f
                        : first <= 0f && second <= 0f && third <= 0f;
                    if (inside)
                    {
                        visit(y * width + x);
                    }
                }
            }
        }

        private static float Cross2D(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static bool IsFlagRed(Color32 color)
        {
            return color.r >= 105 &&
                   color.r >= color.g + 28 &&
                   color.r >= color.b + 20 &&
                   color.g <= 180;
        }

        private static bool IsFlagWhite(Color32 color)
        {
            int maximum = Math.Max(color.r, Math.Max(color.g, color.b));
            int minimum = Math.Min(color.r, Math.Min(color.g, color.b));
            return minimum >= 135 && maximum - minimum <= 55;
        }

        private static void AddTriangleVertexReference(
            IDictionary<int, List<int>> byVertex,
            int vertex,
            int triangle)
        {
            if (!byVertex.TryGetValue(vertex, out List<int> references))
            {
                references = new List<int>();
                byVertex[vertex] = references;
            }

            references.Add(triangle);
        }

        private static void AddAdjacentTriangleIndices(
            LeftArmUvTriangle triangle,
            IReadOnlyDictionary<int, List<int>> byVertex,
            ISet<int> destination)
        {
            foreach (int vertex in new[] { triangle.A, triangle.B, triangle.C })
            {
                if (!byVertex.TryGetValue(vertex, out List<int> references))
                {
                    continue;
                }

                foreach (int index in references)
                {
                    destination.Add(index);
                }
            }
        }

        private static DrawBackRightChestDiagnosticResult
            MeasureDrawBackRightChestDeformation(
                Transform template,
                AnimationClip source,
                AnimationClip adjusted,
                Quaternion rightHandBindLocalRotation)
        {
            SkinnedMeshRenderer templateRenderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(template);
            string rendererPath = AnimationUtility.CalculateTransformPath(
                templateRenderer.transform,
                template);
            GameObject sourceObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            sourceObject.name = template.name + "RightChestSourceDiagnostic";
            adjustedObject.name = template.name + "RightChestAdjustedDiagnostic";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(adjustedObject);
            Mesh sourceBake = new Mesh();
            Mesh adjustedBake = new Mesh();
            sourceBake.name = "HandsDrawBackRightChestSourceBake";
            adjustedBake.name = "HandsDrawBackRightChestAdjustedBake";
            try
            {
                SkinnedMeshRenderer sourceRenderer =
                    RequireRelativeSkinnedMeshRenderer(
                        sourceObject.transform,
                        rendererPath);
                SkinnedMeshRenderer adjustedRenderer =
                    RequireRelativeSkinnedMeshRenderer(
                        adjustedObject.transform,
                        rendererPath);
                Mesh sharedMesh = sourceRenderer.sharedMesh;
                if (sharedMesh == null ||
                    adjustedRenderer.sharedMesh == null ||
                    adjustedRenderer.sharedMesh.vertexCount != sharedMesh.vertexCount)
                {
                    throw new InvalidOperationException(
                        "Hands Draw Back right-chest renderer meshes do not match.");
                }

                BoneWeight[] boneWeights = sharedMesh.boneWeights;
                if (boneWeights.Length != sharedMesh.vertexCount)
                {
                    throw new InvalidOperationException(
                        "Hands Draw Back right-chest mesh has unsupported variable bone weights.");
                }

                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(source.length * source.frameRate));
                float maximumProtrusion = float.NegativeInfinity;
                int maximumFrame = -1;
                int maximumVertex = -1;
                float maximumRightArmWeight = 0f;
                float maximumRightShoulderWeight = 0f;
                float maximumTorsoWeight = 0f;
                float maximumOtherWeight = 0f;
                Vector3 maximumSourceWorld = Vector3.zero;
                Vector3 maximumAdjustedWorld = Vector3.zero;
                HashSet<int> affectedVertices = new HashSet<int>();
                double affectedProtrusionSum = 0d;
                int affectedSamples = 0;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    float time = source.length * frame / framesPerLoop;
                    FindRequired(sourceObject.transform, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(sourceObject, time);
                    adjusted.SampleAnimation(adjustedObject, time);
                    sourceRenderer.BakeMesh(sourceBake, true);
                    adjustedRenderer.BakeMesh(adjustedBake, true);
                    Vector3[] sourceVertices = sourceBake.vertices;
                    Vector3[] adjustedVertices = adjustedBake.vertices;
                    Transform sourceRoot = sourceObject.transform;
                    Transform sourceSpine = FindRequired(sourceRoot, SpinePath);
                    Transform sourceSolar = FindRequired(sourceRoot, SolarPlexusPath);
                    Transform sourceUpper = FindRequired(sourceRoot, RightArmPath);
                    float armLateral = Vector3.Dot(
                        sourceUpper.position - sourceSpine.position,
                        sourceRoot.right);
                    float solarVertical = Vector3.Dot(
                        sourceSolar.position - sourceSpine.position,
                        sourceRoot.up);
                    float armVertical = Vector3.Dot(
                        sourceUpper.position - sourceSpine.position,
                        sourceRoot.up);
                    float minimumLateral = Mathf.Min(0.015f, armLateral * 0.1f);
                    float maximumLateral = armLateral + 0.08f;
                    float minimumVertical = Mathf.Min(
                        solarVertical,
                        armVertical) - 0.12f;
                    float maximumVertical = Mathf.Max(
                        solarVertical,
                        armVertical) + 0.1f;
                    for (int vertex = 0; vertex < sourceVertices.Length; vertex++)
                    {
                        Vector3 sourceWorld = sourceRenderer.transform.TransformPoint(
                            sourceVertices[vertex]);
                        Vector3 relative = sourceWorld - sourceSpine.position;
                        float lateral = Vector3.Dot(relative, sourceRoot.right);
                        float vertical = Vector3.Dot(relative, sourceRoot.up);
                        float forward = Vector3.Dot(relative, sourceRoot.forward);
                        if (lateral < minimumLateral ||
                            lateral > maximumLateral ||
                            vertical < minimumVertical ||
                            vertical > maximumVertical ||
                            forward < -0.08f)
                        {
                            continue;
                        }

                        BoneWeight weight = boneWeights[vertex];
                        float rightArmWeight = BoneWeightForSuffix(
                            weight,
                            sourceRenderer.bones,
                            "RightArm");
                        float rightShoulderWeight = BoneWeightForSuffix(
                            weight,
                            sourceRenderer.bones,
                            "RightShoulder");
                        float torsoWeight = BoneWeightForSuffixes(
                            weight,
                            sourceRenderer.bones,
                            "Hips",
                            "Spine02",
                            "Spine01",
                            "Spine");
                        if (torsoWeight <= 0.01f ||
                            rightArmWeight + rightShoulderWeight <= 0.005f)
                        {
                            continue;
                        }

                        Vector3 adjustedWorld =
                            adjustedRenderer.transform.TransformPoint(
                                adjustedVertices[vertex]);
                        float protrusion = Vector3.Dot(
                            adjustedWorld - sourceWorld,
                            sourceRoot.forward);
                        if (protrusion > 0.002f)
                        {
                            affectedVertices.Add(vertex);
                            affectedProtrusionSum += protrusion;
                            affectedSamples++;
                        }

                        if (protrusion <= maximumProtrusion)
                        {
                            continue;
                        }

                        maximumProtrusion = protrusion;
                        maximumFrame = frame;
                        maximumVertex = vertex;
                        maximumRightArmWeight = rightArmWeight;
                        maximumRightShoulderWeight = rightShoulderWeight;
                        maximumTorsoWeight = torsoWeight;
                        maximumOtherWeight = Mathf.Max(
                            0f,
                            1f - rightArmWeight - rightShoulderWeight - torsoWeight);
                        maximumSourceWorld = sourceWorld;
                        maximumAdjustedWorld = adjustedWorld;
                    }
                }

                return new DrawBackRightChestDiagnosticResult
                {
                    Renderer = templateRenderer,
                    RendererPath = rendererPath,
                    FramesPerLoop = framesPerLoop,
                    MaximumProtrusionFrame = maximumFrame,
                    MaximumProtrusionVertexIndex = maximumVertex,
                    MaximumForwardProtrusionMeters = maximumProtrusion,
                    AverageAffectedForwardProtrusionMeters =
                        affectedSamples > 0
                            ? (float)(affectedProtrusionSum / affectedSamples)
                            : 0f,
                    AffectedVertexCount = affectedVertices.Count,
                    MaximumVertexRightArmWeight = maximumRightArmWeight,
                    MaximumVertexRightShoulderWeight = maximumRightShoulderWeight,
                    MaximumVertexTorsoWeight = maximumTorsoWeight,
                    MaximumVertexOtherWeight = maximumOtherWeight,
                    MaximumVertexSourceWorldPosition = maximumSourceWorld,
                    MaximumVertexAdjustedWorldPosition = maximumAdjustedWorld
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceBake);
                UnityEngine.Object.DestroyImmediate(adjustedBake);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
            }
        }

        private static DrawBackRightChestCorrectiveBuildResult
            CreateOrUpdateDrawBackRightChestStableSkinMesh(
                Transform template,
                SkinnedMeshRenderer templateRenderer,
                Mesh originalMesh,
                AnimationClip source,
                AnimationClip adjusted,
                Quaternion rightHandBindLocalRotation)
        {
            string rendererPath = AnimationUtility.CalculateTransformPath(
                templateRenderer.transform,
                template);
            AnimationClip empty = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                EmptyClipPath);
            if (empty == null)
            {
                throw new InvalidOperationException(
                    "Hands Empty Idle clip is missing for the right-chest stable skin reference.");
            }

            GameObject referenceObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            referenceObject.name = template.name + "RightChestStableSkinReference";
            referenceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(referenceObject);
            Mesh referenceBake = new Mesh
            {
                name = "HandsDrawBackRightChestStableSkinReferenceBake"
            };
            try
            {
                SkinnedMeshRenderer referenceRenderer =
                    RequireRelativeSkinnedMeshRenderer(
                        referenceObject.transform,
                        rendererPath);
                referenceRenderer.sharedMesh = originalMesh;
                SampleDrawBackStableChestReference(
                    referenceObject,
                    empty,
                    adjusted,
                    0f,
                    rightHandBindLocalRotation);
                referenceRenderer.BakeMesh(referenceBake, true);
                Vector3[] referenceVertices = referenceBake.vertices;
                BoneWeight[] originalWeights = originalMesh.boneWeights;
                BoneWeight[] correctedWeights = originalMesh.boneWeights;
                Transform[] bones = referenceRenderer.bones;
                int spineBoneIndex = Array.FindIndex(
                    bones,
                    bone => bone != null &&
                        bone.name.EndsWith("Spine", StringComparison.Ordinal));
                if (spineBoneIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Hands Draw Back stable skin correction could not find the upper Spine bone.");
                }

                Transform referenceRoot = referenceObject.transform;
                Transform referenceSpine = FindRequired(
                    referenceRoot,
                    SpinePath);
                Transform referenceSolar = FindRequired(
                    referenceRoot,
                    SolarPlexusPath);
                Transform referenceUpper = FindRequired(
                    referenceRoot,
                    RightArmPath);
                float armLateral = Vector3.Dot(
                    referenceUpper.position - referenceSpine.position,
                    referenceRoot.right);
                float solarVertical = Vector3.Dot(
                    referenceSolar.position - referenceSpine.position,
                    referenceRoot.up);
                float armVertical = Vector3.Dot(
                    referenceUpper.position - referenceSpine.position,
                    referenceRoot.up);
                float minimumLateral = Mathf.Min(0.015f, armLateral * 0.1f);
                float fullCorrectionLateral = armLateral * 0.72f;
                float maximumLateral = armLateral + 0.06f;
                float minimumVertical = Mathf.Min(
                    solarVertical,
                    armVertical) - 0.1f;
                float maximumVertical = Mathf.Max(
                    solarVertical,
                    armVertical) + 0.08f;
                int correctedVertexCount = 0;
                float maximumTransferredWeight = 0f;
                for (int vertex = 0; vertex < referenceVertices.Length; vertex++)
                {
                    Vector3 referenceWorld =
                        referenceRenderer.transform.TransformPoint(
                            referenceVertices[vertex]);
                    Vector3 relative = referenceWorld - referenceSpine.position;
                    float lateral = Vector3.Dot(relative, referenceRoot.right);
                    float vertical = Vector3.Dot(relative, referenceRoot.up);
                    float forward = Vector3.Dot(relative, referenceRoot.forward);
                    if (lateral < minimumLateral ||
                        lateral > maximumLateral ||
                        vertical < minimumVertical ||
                        vertical > maximumVertical ||
                        forward < -0.08f ||
                        forward > 0.22f)
                    {
                        continue;
                    }

                    BoneWeight weight = originalWeights[vertex];
                    float rightArmWeight = BoneWeightForSuffixes(
                        weight,
                        bones,
                        "RightArm",
                        "RightForeArm",
                        "RightHand");
                    float rightShoulderWeight = BoneWeightForSuffix(
                        weight,
                        bones,
                        "RightShoulder");
                    float torsoWeight = BoneWeightForSuffixes(
                        weight,
                        bones,
                        "Hips",
                        "Spine02",
                        "Spine01",
                        "Spine");
                    if (torsoWeight <= 0.01f ||
                        rightArmWeight + rightShoulderWeight <= 0.005f)
                    {
                        continue;
                    }

                    float lateralFade = 1f - Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(
                            fullCorrectionLateral,
                            maximumLateral,
                            lateral));
                    float transferFactor = 0.92f * lateralFade;
                    BoneWeight corrected = RedistributeRightChestSkinWeight(
                        weight,
                        bones,
                        spineBoneIndex,
                        transferFactor,
                        out float transferredWeight);
                    if (transferredWeight <= 0.002f)
                    {
                        continue;
                    }

                    correctedWeights[vertex] = corrected;
                    correctedVertexCount++;
                    maximumTransferredWeight = Mathf.Max(
                        maximumTransferredWeight,
                        transferredWeight);
                }

                if (correctedVertexCount == 0)
                {
                    throw new InvalidOperationException(
                        "Hands Draw Back stable skin correction found no mixed chest vertices.");
                }

                Mesh generated = UnityEngine.Object.Instantiate(originalMesh);
                generated.name = "Hands_Draw_Back_ChestCorrected";
                generated.boneWeights = correctedWeights;
                generated.RecalculateBounds();
                Directory.CreateDirectory(Path.GetDirectoryName(
                    DrawBackRightChestCorrectedMeshPath));
                Mesh correctedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                    DrawBackRightChestCorrectedMeshPath);
                if (correctedMesh == null)
                {
                    AssetDatabase.CreateAsset(
                        generated,
                        DrawBackRightChestCorrectedMeshPath);
                    correctedMesh = generated;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, correctedMesh);
                    UnityEngine.Object.DestroyImmediate(generated);
                    correctedMesh.name = "Hands_Draw_Back_ChestCorrected";
                    EditorUtility.SetDirty(correctedMesh);
                }

                EditorUtility.SetDirty(adjusted);
                AssetDatabase.SaveAssets();
                return new DrawBackRightChestCorrectiveBuildResult
                {
                    CorrectedMesh = correctedMesh,
                    BlendShapeIndex = -1,
                    CorrectedVertexCount = correctedVertexCount,
                    CurveKeyCount = 0,
                    MaximumBindPoseCorrectionMeters = maximumTransferredWeight
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(referenceBake);
                UnityEngine.Object.DestroyImmediate(referenceObject);
            }
        }

        private static BoneWeight RedistributeRightChestSkinWeight(
            BoneWeight source,
            Transform[] bones,
            int spineBoneIndex,
            float transferFactor,
            out float transferredWeight)
        {
            Dictionary<int, float> weights = new Dictionary<int, float>();
            transferredWeight = 0f;
            int[] indices =
            {
                source.boneIndex0,
                source.boneIndex1,
                source.boneIndex2,
                source.boneIndex3
            };
            float[] values =
            {
                source.weight0,
                source.weight1,
                source.weight2,
                source.weight3
            };
            for (int influence = 0; influence < indices.Length; influence++)
            {
                int index = indices[influence];
                float value = values[influence];
                if (value <= 0f)
                {
                    continue;
                }

                string boneName = bones[index] == null
                    ? string.Empty
                    : bones[index].name;
                bool rightArmInfluence =
                    boneName.EndsWith("RightShoulder", StringComparison.Ordinal) ||
                    boneName.EndsWith("RightArm", StringComparison.Ordinal) ||
                    boneName.EndsWith("RightForeArm", StringComparison.Ordinal) ||
                    boneName.EndsWith("RightHand", StringComparison.Ordinal);
                float moved = rightArmInfluence
                    ? value * transferFactor
                    : 0f;
                AddSkinWeight(weights, index, value - moved);
                AddSkinWeight(weights, spineBoneIndex, moved);
                transferredWeight += moved;
            }

            KeyValuePair<int, float>[] strongest = weights
                .Where(pair => pair.Value > 0.000001f)
                .OrderByDescending(pair => pair.Value)
                .Take(4)
                .ToArray();
            float total = strongest.Sum(pair => pair.Value);
            if (total <= 0f)
            {
                return source;
            }

            BoneWeight result = new BoneWeight();
            for (int influence = 0; influence < strongest.Length; influence++)
            {
                int index = strongest[influence].Key;
                float value = strongest[influence].Value / total;
                switch (influence)
                {
                    case 0:
                        result.boneIndex0 = index;
                        result.weight0 = value;
                        break;
                    case 1:
                        result.boneIndex1 = index;
                        result.weight1 = value;
                        break;
                    case 2:
                        result.boneIndex2 = index;
                        result.weight2 = value;
                        break;
                    case 3:
                        result.boneIndex3 = index;
                        result.weight3 = value;
                        break;
                }
            }

            return result;
        }

        private static void AddSkinWeight(
            IDictionary<int, float> weights,
            int boneIndex,
            float value)
        {
            if (value <= 0f)
            {
                return;
            }

            weights[boneIndex] = weights.TryGetValue(
                boneIndex,
                out float current)
                ? current + value
                : value;
        }

        private static DrawBackRightChestCorrectiveBuildResult
            CreateOrUpdateDrawBackRightChestCorrectiveMeshAndCurve(
                Transform template,
                SkinnedMeshRenderer templateRenderer,
                Mesh originalMesh,
                AnimationClip source,
                AnimationClip adjusted,
                Quaternion rightHandBindLocalRotation)
        {
            string rendererPath = AnimationUtility.CalculateTransformPath(
                templateRenderer.transform,
                template);
            AnimationClip empty = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                EmptyClipPath);
            if (empty == null)
            {
                throw new InvalidOperationException(
                    "Hands Empty Idle clip is missing for the right-chest stable reference.");
            }

            GameObject referenceObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            referenceObject.name = template.name + "RightChestStableReference";
            adjustedObject.name = template.name + "RightChestCorrectiveAdjusted";
            referenceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(referenceObject);
            DisableAnimators(adjustedObject);
            Mesh referenceBake = new Mesh();
            Mesh adjustedBake = new Mesh();
            referenceBake.name = "HandsDrawBackRightChestStableReferenceBake";
            adjustedBake.name = "HandsDrawBackRightChestCorrectiveAdjustedBake";
            try
            {
                SkinnedMeshRenderer referenceRenderer =
                    RequireRelativeSkinnedMeshRenderer(
                        referenceObject.transform,
                        rendererPath);
                SkinnedMeshRenderer adjustedRenderer =
                    RequireRelativeSkinnedMeshRenderer(
                        adjustedObject.transform,
                        rendererPath);
                referenceRenderer.sharedMesh = originalMesh;
                adjustedRenderer.sharedMesh = originalMesh;
                BoneWeight[] boneWeights = originalMesh.boneWeights;
                Matrix4x4[] bindPoses = originalMesh.bindposes;
                if (boneWeights.Length != originalMesh.vertexCount ||
                    bindPoses.Length != adjustedRenderer.bones.Length)
                {
                    throw new InvalidOperationException(
                        "Hands Draw Back right-chest mesh skin data is incompatible.");
                }

                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(source.length * source.frameRate));
                List<int> phaseFrames = Enumerable.Range(
                        0,
                        Mathf.CeilToInt(
                            framesPerLoop / (float)DrawBackRightChestPhaseStride))
                    .Select(index => index * DrawBackRightChestPhaseStride)
                    .Where(frame => frame < framesPerLoop)
                    .ToList();
                bool[] correctedVertices = new bool[originalMesh.vertexCount];
                float maximumBindCorrection = 0f;
                int curveKeyCount = 0;
                Mesh generated = UnityEngine.Object.Instantiate(originalMesh);
                generated.name = "Hands_Draw_Back_ChestCorrected";
                for (int phaseIndex = 0; phaseIndex < phaseFrames.Count; phaseIndex++)
                {
                    int frame = phaseFrames[phaseIndex];
                    float phase = frame / (float)framesPerLoop;
                    SampleDrawBackStableChestReference(
                        referenceObject,
                        empty,
                        adjusted,
                        phase,
                        rightHandBindLocalRotation);
                    adjusted.SampleAnimation(
                        adjustedObject,
                        adjusted.length * phase);
                    referenceRenderer.BakeMesh(referenceBake, true);
                    adjustedRenderer.BakeMesh(adjustedBake, true);
                    Vector3[] referenceVertices = referenceBake.vertices;
                    Vector3[] adjustedVertices = adjustedBake.vertices;
                    Transform referenceRoot = referenceObject.transform;
                    Transform adjustedRoot = adjustedObject.transform;
                    Transform referenceSpine = FindRequired(
                        referenceRoot,
                        SpinePath);
                    Transform referenceSolar = FindRequired(
                        referenceRoot,
                        SolarPlexusPath);
                    Transform referenceUpper = FindRequired(
                        referenceRoot,
                        RightArmPath);
                    float armLateral = Vector3.Dot(
                        referenceUpper.position - referenceSpine.position,
                        referenceRoot.right);
                    float solarVertical = Vector3.Dot(
                        referenceSolar.position - referenceSpine.position,
                        referenceRoot.up);
                    float armVertical = Vector3.Dot(
                        referenceUpper.position - referenceSpine.position,
                        referenceRoot.up);
                    float minimumLateral = Mathf.Min(0.02f, armLateral * 0.12f);
                    float fullCorrectionLateral = armLateral * 0.72f;
                    float maximumLateral = armLateral * 0.98f;
                    float minimumVertical = Mathf.Min(
                        solarVertical,
                        armVertical) - 0.08f;
                    float maximumVertical = Mathf.Max(
                        solarVertical,
                        armVertical) + 0.06f;
                    Vector3[] correctiveDeltas =
                        new Vector3[originalMesh.vertexCount];
                    for (int vertex = 0; vertex < referenceVertices.Length; vertex++)
                    {
                        Vector3 referenceWorld =
                            referenceRenderer.transform.TransformPoint(
                                referenceVertices[vertex]);
                        Vector3 relative = referenceWorld - referenceSpine.position;
                        float lateral = Vector3.Dot(
                            relative,
                            referenceRoot.right);
                        float vertical = Vector3.Dot(
                            relative,
                            referenceRoot.up);
                        float forward = Vector3.Dot(
                            relative,
                            referenceRoot.forward);
                        if (lateral < minimumLateral ||
                            lateral > maximumLateral ||
                            vertical < minimumVertical ||
                            vertical > maximumVertical ||
                            forward < -0.08f ||
                            forward > 0.22f)
                        {
                            continue;
                        }

                        BoneWeight weight = boneWeights[vertex];
                        float rightArmWeight = BoneWeightForSuffix(
                            weight,
                            referenceRenderer.bones,
                            "RightArm");
                        float rightShoulderWeight = BoneWeightForSuffix(
                            weight,
                            referenceRenderer.bones,
                            "RightShoulder");
                        float torsoWeight = BoneWeightForSuffixes(
                            weight,
                            referenceRenderer.bones,
                            "Hips",
                            "Spine02",
                            "Spine01",
                            "Spine");
                        if (torsoWeight <= 0.01f ||
                            rightArmWeight + rightShoulderWeight <= 0.005f)
                        {
                            continue;
                        }

                        Vector3 adjustedWorld =
                            adjustedRenderer.transform.TransformPoint(
                                adjustedVertices[vertex]);
                        Vector3 displacement = adjustedWorld - referenceWorld;
                        float forwardProtrusion = Vector3.Dot(
                            displacement,
                            referenceRoot.forward);
                        float outwardProtrusion = Vector3.Dot(
                            displacement,
                            referenceRoot.right);
                        if (Mathf.Max(
                                forwardProtrusion,
                                outwardProtrusion) <= 0.001f)
                        {
                            continue;
                        }

                        float lateralFade = 1f - Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                fullCorrectionLateral,
                                maximumLateral,
                                lateral));
                        Vector3 desiredWorldCorrection =
                            -displacement * lateralFade;
                        if (desiredWorldCorrection.magnitude <= 0.001f ||
                            desiredWorldCorrection.magnitude > 0.14f)
                        {
                            continue;
                        }

                        Vector3 desiredRendererLocalCorrection =
                            adjustedRenderer.transform.InverseTransformVector(
                                desiredWorldCorrection);
                        Matrix4x4 skinMatrix = CalculateWeightedSkinMatrix(
                            adjustedRenderer,
                            bindPoses,
                            weight);
                        Vector3 bindPoseDelta = skinMatrix.inverse.MultiplyVector(
                            desiredRendererLocalCorrection);
                        if (!IsFinite(bindPoseDelta) ||
                            bindPoseDelta.magnitude > 0.2f)
                        {
                            continue;
                        }

                        correctiveDeltas[vertex] = bindPoseDelta;
                        correctedVertices[vertex] = true;
                        maximumBindCorrection = Mathf.Max(
                            maximumBindCorrection,
                            bindPoseDelta.magnitude);
                    }
                    string blendShapeName = DrawBackRightChestPhaseBlendShapeName(
                        phaseIndex);
                    generated.AddBlendShapeFrame(
                        blendShapeName,
                        100f,
                        correctiveDeltas,
                        new Vector3[originalMesh.vertexCount],
                        new Vector3[originalMesh.vertexCount]);
                    Keyframe[] keys = new Keyframe[framesPerLoop + 1];
                    for (int curveFrame = 0;
                        curveFrame <= framesPerLoop;
                        curveFrame++)
                    {
                        int loopFrame = curveFrame == framesPerLoop
                            ? 0
                            : curveFrame;
                        int directDistance = Mathf.Abs(loopFrame - frame);
                        int cyclicDistance = Mathf.Min(
                            directDistance,
                            framesPerLoop - directDistance);
                        float weightValue = Mathf.Clamp01(
                            1f - cyclicDistance /
                            (float)DrawBackRightChestPhaseStride);
                        keys[curveFrame] = new Keyframe(
                            adjusted.length * curveFrame / framesPerLoop,
                            weightValue * 100f);
                    }

                    AnimationCurve curve = new AnimationCurve(keys);
                    for (int key = 0; key < curve.length; key++)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(
                            curve,
                            key,
                            AnimationUtility.TangentMode.Linear);
                        AnimationUtility.SetKeyRightTangentMode(
                            curve,
                            key,
                            AnimationUtility.TangentMode.Linear);
                    }

                    AnimationUtility.SetEditorCurve(
                        adjusted,
                        DrawBackRightChestBlendShapeBinding(
                            rendererPath,
                            blendShapeName),
                        curve);
                    curveKeyCount += curve.length;
                }

                int correctedVertexCount = correctedVertices.Count(value => value);
                if (correctedVertexCount == 0)
                {
                    UnityEngine.Object.DestroyImmediate(generated);
                    throw new InvalidOperationException(
                        "Hands Draw Back stable-chest correction found no vertices to correct.");
                }

                generated.RecalculateBounds();
                Directory.CreateDirectory(Path.GetDirectoryName(
                    DrawBackRightChestCorrectedMeshPath));
                Mesh correctedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                    DrawBackRightChestCorrectedMeshPath);
                if (correctedMesh == null)
                {
                    AssetDatabase.CreateAsset(
                        generated,
                        DrawBackRightChestCorrectedMeshPath);
                    correctedMesh = generated;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, correctedMesh);
                    UnityEngine.Object.DestroyImmediate(generated);
                    correctedMesh.name = "Hands_Draw_Back_ChestCorrected";
                    EditorUtility.SetDirty(correctedMesh);
                }

                EditorUtility.SetDirty(adjusted);
                AssetDatabase.SaveAssets();
                int blendShapeIndex = correctedMesh.GetBlendShapeIndex(
                    DrawBackRightChestBlendShapeName);
                if (blendShapeIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Hands Draw Back corrected mesh is missing its BlendShape.");
                }

                return new DrawBackRightChestCorrectiveBuildResult
                {
                    CorrectedMesh = correctedMesh,
                    BlendShapeIndex = blendShapeIndex,
                    CorrectedVertexCount = correctedVertexCount,
                    CurveKeyCount = curveKeyCount,
                    MaximumBindPoseCorrectionMeters = maximumBindCorrection
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(referenceBake);
                UnityEngine.Object.DestroyImmediate(adjustedBake);
                UnityEngine.Object.DestroyImmediate(referenceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
            }
        }

        private static void SampleDrawBackStableChestReference(
            GameObject referenceObject,
            AnimationClip empty,
            AnimationClip adjusted,
            float phase,
            Quaternion rightHandBindLocalRotation)
        {
            string[] armPaths =
            {
                RightShoulderPath,
                RightArmPath,
                RightForeArmPath,
                RightHandPath
            };
            FindRequired(referenceObject.transform, RightHandPath).localRotation =
                rightHandBindLocalRotation;
            empty.SampleAnimation(
                referenceObject,
                Mathf.Repeat(phase, 1f) * empty.length);
            RootPose[] stableArm = armPaths
                .Select(path => new RootPose(
                    FindRequired(referenceObject.transform, path)))
                .ToArray();
            adjusted.SampleAnimation(
                referenceObject,
                Mathf.Repeat(phase, 1f) * adjusted.length);
            for (int index = 0; index < armPaths.Length; index++)
            {
                Transform bone = FindRequired(
                    referenceObject.transform,
                    armPaths[index]);
                bone.localPosition = stableArm[index].LocalPosition;
                bone.localRotation = stableArm[index].LocalRotation;
                bone.localScale = stableArm[index].LocalScale;
            }
        }

        private static string DrawBackRightChestPhaseBlendShapeName(int index)
        {
            return DrawBackRightChestBlendShapePrefix +
                index.ToString("D2", CultureInfo.InvariantCulture);
        }

        private static Mesh LoadPlayerMeshByName(string meshName)
        {
            Mesh[] meshes = AssetDatabase.LoadAllAssetsAtPath(
                    "Assets/_Project/Art/Player/player.fbx")
                .OfType<Mesh>()
                .Where(mesh => string.Equals(
                    mesh.name,
                    meshName,
                    StringComparison.Ordinal))
                .ToArray();
            if (meshes.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected one player mesh named " + meshName +
                    ", found " + meshes.Length + ".");
            }

            return meshes[0];
        }

        private static Dictionary<string, string>
            CapturePrimaryRendererMeshPathsExceptTarget(
                Transform layout,
                string excludedTargetName)
        {
            Transform excludedTarget = RequireTarget(layout, excludedTargetName);
            return layout.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer =>
                    renderer.sharedMesh != null &&
                    !renderer.transform.IsChildOf(excludedTarget))
                .ToDictionary(
                    renderer => AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        layout),
                    renderer => AssetDatabase.GetAssetPath(renderer.sharedMesh),
                    StringComparer.Ordinal);
        }

        private static EditorCurveBinding DrawBackRightChestBlendShapeBinding(
            string rendererPath,
            string blendShapeName = DrawBackRightChestBlendShapeName)
        {
            return EditorCurveBinding.FloatCurve(
                rendererPath,
                typeof(SkinnedMeshRenderer),
                "blendShape." + blendShapeName);
        }

        private static void RemoveDrawBackRightChestBlendShapeCurve(
            AnimationClip clip,
            string rendererPath)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    string.Equals(
                        binding.path,
                        rendererPath,
                        StringComparison.Ordinal) &&
                    IsDrawBackRightChestCorrectiveProperty(
                        binding.propertyName))
                .ToArray();
            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static AnimationCurve[] GetDrawBackRightChestCorrectiveCurves(
            AnimationClip clip,
            string rendererPath)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    string.Equals(
                        binding.path,
                        rendererPath,
                        StringComparison.Ordinal) &&
                    IsDrawBackRightChestCorrectiveProperty(
                        binding.propertyName))
                .Select(binding => AnimationUtility.GetEditorCurve(clip, binding))
                .Where(curve => curve != null)
                .ToArray();
        }

        private static bool IsDrawBackRightChestCorrectiveProperty(
            string propertyName)
        {
            return propertyName.StartsWith(
                    "blendShape." + DrawBackRightChestBlendShapePrefix,
                    StringComparison.Ordinal) ||
                string.Equals(
                    propertyName,
                    "blendShape." + DrawBackRightChestLegacyBlendShapeName,
                    StringComparison.Ordinal) ||
                string.Equals(
                    propertyName,
                    "blendShape." + DrawBackRightChestLegacyResidualBlendShapeName,
                    StringComparison.Ordinal);
        }

        private static void ResetDrawBackRightChestBlendShapeWeights(
            SkinnedMeshRenderer renderer)
        {
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            for (int index = 0; index < mesh.blendShapeCount; index++)
            {
                string name = mesh.GetBlendShapeName(index);
                if (name.StartsWith(
                        DrawBackRightChestBlendShapePrefix,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        name,
                        DrawBackRightChestLegacyBlendShapeName,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        name,
                        DrawBackRightChestLegacyResidualBlendShapeName,
                        StringComparison.Ordinal))
                {
                    renderer.SetBlendShapeWeight(index, 0f);
                }
            }
        }

        private static Matrix4x4 CalculateWeightedSkinMatrix(
            SkinnedMeshRenderer renderer,
            Matrix4x4[] bindPoses,
            BoneWeight weight)
        {
            Matrix4x4 weighted = new Matrix4x4();
            for (int influence = 0; influence < 4; influence++)
            {
                int index;
                float value;
                switch (influence)
                {
                    case 0:
                        index = weight.boneIndex0;
                        value = weight.weight0;
                        break;
                    case 1:
                        index = weight.boneIndex1;
                        value = weight.weight1;
                        break;
                    case 2:
                        index = weight.boneIndex2;
                        value = weight.weight2;
                        break;
                    default:
                        index = weight.boneIndex3;
                        value = weight.weight3;
                        break;
                }

                if (value <= 0f ||
                    index < 0 ||
                    index >= renderer.bones.Length ||
                    index >= bindPoses.Length ||
                    renderer.bones[index] == null)
                {
                    continue;
                }

                Matrix4x4 skin =
                    renderer.transform.worldToLocalMatrix *
                    renderer.bones[index].localToWorldMatrix *
                    bindPoses[index];
                for (int element = 0; element < 16; element++)
                {
                    weighted[element] += skin[element] * value;
                }
            }

            return weighted;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) &&
                   !float.IsNaN(value.y) &&
                   !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.x) &&
                   !float.IsInfinity(value.y) &&
                   !float.IsInfinity(value.z);
        }

        private static SkinnedMeshRenderer RequirePrimaryPlayerSkinnedMeshRenderer(
            Transform target)
        {
            SkinnedMeshRenderer renderer = target
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate =>
                    candidate.sharedMesh != null &&
                    candidate.sharedMesh.vertexCount > 0)
                .OrderByDescending(candidate => candidate.sharedMesh.vertexCount)
                .FirstOrDefault();
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    target.name + " has no usable SkinnedMeshRenderer.");
            }

            return renderer;
        }

        private static SkinnedMeshRenderer RequireRelativeSkinnedMeshRenderer(
            Transform root,
            string rendererPath)
        {
            Transform rendererTransform = string.IsNullOrEmpty(rendererPath)
                ? root
                : root.Find(rendererPath);
            SkinnedMeshRenderer renderer = rendererTransform != null
                ? rendererTransform.GetComponent<SkinnedMeshRenderer>()
                : null;
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    root.name + " is missing SkinnedMeshRenderer at " +
                    rendererPath + ".");
            }

            return renderer;
        }

        private static float BoneWeightForSuffix(
            BoneWeight weight,
            Transform[] bones,
            string suffix)
        {
            float total = 0f;
            for (int influence = 0; influence < 4; influence++)
            {
                int index;
                float value;
                switch (influence)
                {
                    case 0:
                        index = weight.boneIndex0;
                        value = weight.weight0;
                        break;
                    case 1:
                        index = weight.boneIndex1;
                        value = weight.weight1;
                        break;
                    case 2:
                        index = weight.boneIndex2;
                        value = weight.weight2;
                        break;
                    default:
                        index = weight.boneIndex3;
                        value = weight.weight3;
                        break;
                }

                if (value <= 0f || index < 0 || index >= bones.Length)
                {
                    continue;
                }

                Transform bone = bones[index];
                if (bone != null && bone.name.EndsWith(
                        suffix,
                        StringComparison.Ordinal))
                {
                    total += value;
                }
            }

            return total;
        }

        private static float BoneWeightForSuffixes(
            BoneWeight weight,
            Transform[] bones,
            params string[] suffixes)
        {
            return suffixes.Sum(suffix =>
                BoneWeightForSuffix(weight, bones, suffix));
        }

        private static void
            CapturePlayerHandsDrawBackRightChestCorrectionActualReview()
        {
            DrawBackRightChestCorrectionApplyMetrics apply =
                ReadJson<DrawBackRightChestCorrectionApplyMetrics>(
                    DrawBackRightChestApplyMetricsPath);
            DrawBackRightChestDiagnosticMetrics diagnostic =
                ReadJson<DrawBackRightChestDiagnosticMetrics>(
                    DrawBackRightChestDiagnosticMetricsPath);
            if (!apply.passedNumericChecks || !diagnostic.diagnosisComplete)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back right-chest apply and diagnosis must pass before review.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            SkinnedMeshRenderer renderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(target);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back adjusted clip is missing for right-chest review.");
            }

            string rendererPath = AnimationUtility.CalculateTransformPath(
                renderer.transform,
                target);
            Quaternion rightHandBindLocalRotation =
                FindRequired(target, RightHandPath).localRotation;
            CaptureDrawBackRightChestCorrectionComparison(
                target,
                renderer,
                source,
                adjusted,
                diagnostic.maximumProtrusionFrame,
                66,
                rightHandBindLocalRotation,
                DrawBackRightChestReviewPath);
            DrawBackRightChestDiagnosticResult corrected =
                MeasureDrawBackRightChestDeformation(
                    target,
                    source,
                    adjusted,
                    rightHandBindLocalRotation);
            DrawBackOuterElbowReviewMetrics runtime =
                CaptureDrawBackOuterElbowMetrics(
                    target,
                    source,
                    adjusted,
                    43,
                    54,
                    66,
                    rightHandBindLocalRotation);
            MeasureDrawBackClipFrontSilhouetteGap(
                target,
                adjusted,
                out float minimumGap,
                out int _);
            int blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(
                DrawBackRightChestBlendShapeName);

            Animator animator = RequireAnimator(target);
            float weightMinimum = 0f;
            float weightMaximum = 0f;
            for (int frame = 0; frame < runtime.framesPerLoop; frame++)
            {
                SampleAnimator(
                    animator,
                    DrawBackStateName,
                    frame / (float)runtime.framesPerLoop);
                if (blendShapeIndex >= 0)
                {
                    float weight = renderer.GetBlendShapeWeight(blendShapeIndex);
                    weightMinimum = frame == 0
                        ? weight
                        : Mathf.Min(weightMinimum, weight);
                    weightMaximum = frame == 0
                        ? weight
                        : Mathf.Max(weightMaximum, weight);
                }
            }

            SampleAnimator(animator, DrawBackStateName, 0f);
            AnimationCurve[] correctiveCurves =
                GetDrawBackRightChestCorrectiveCurves(
                    adjusted,
                    rendererPath);
            DrawBackRightChestCorrectionReviewMetrics metrics =
                new DrawBackRightChestCorrectionReviewMetrics
                {
                    target = DrawBackTargetName,
                    framesPerLoop = runtime.framesPerLoop,
                    framesDirectlyCaptured = 12,
                    framesSampled = runtime.framesSampled,
                    loopsSampled = runtime.loopsSampled,
                    beforeMaximumForwardProtrusionMeters =
                        apply.beforeMaximumForwardProtrusionMeters,
                    afterMaximumForwardProtrusionMeters =
                        corrected.MaximumForwardProtrusionMeters,
                    afterAffectedVertexCount = corrected.AffectedVertexCount,
                    blendShapeIndex = blendShapeIndex,
                    blendShapeWeightMinimum = weightMinimum,
                    blendShapeWeightMaximum = weightMaximum,
                    minimumFrontSilhouetteGapMeters = minimumGap,
                    rootPositionDisplacementMax =
                        runtime.rootPositionDisplacementMax,
                    runtimeAdjustedPosePositionDifferenceMax =
                        runtime.runtimeAdjustedPosePositionDifferenceMax,
                    runtimeAdjustedPoseRotationDifferenceDegreesMax =
                        runtime.runtimeAdjustedPoseRotationDifferenceDegreesMax,
                    unchangedPosePositionDifferenceMax =
                        runtime.unchangedPosePositionDifferenceMax,
                    unchangedPoseRotationDifferenceDegreesMax =
                        runtime.unchangedPoseRotationDifferenceDegreesMax,
                    sourcePeakFrame = runtime.sourcePeakFrame,
                    adjustedPeakFrame = runtime.adjustedPeakFrame,
                    adjustedPeakElbowFlexDegrees =
                        runtime.adjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        runtime.adjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakHorizontalOutwardAngleDegrees =
                        runtime.adjustedPeakHorizontalForwardAngleDegrees,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        runtime.adjustedPeakPalmCharacterLeftAngleDegrees,
                    stateLoops = runtime.stateLoops,
                    applyRootMotion = runtime.applyRootMotion,
                    blendShapeCurveBound =
                        blendShapeIndex < 0
                            ? correctiveCurves.Length == 0 &&
                              renderer.sharedMesh.blendShapeCount == 0
                            : correctiveCurves.Length > 0 &&
                              correctiveCurves.All(curve => curve.length >= 2),
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.framesSampled == metrics.framesPerLoop * 2 &&
                metrics.loopsSampled == 2 &&
                (metrics.blendShapeIndex < 0 ||
                 (metrics.blendShapeWeightMinimum <= 0.5f &&
                  metrics.blendShapeWeightMaximum >= 95f)) &&
                metrics.minimumFrontSilhouetteGapMeters >= 0.005f &&
                metrics.rootPositionDisplacementMax <= PositionTolerance &&
                metrics.runtimeAdjustedPosePositionDifferenceMax <= PositionTolerance &&
                metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.unchangedPosePositionDifferenceMax <= PositionTolerance &&
                metrics.unchangedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.sourcePeakFrame == metrics.adjustedPeakFrame &&
                Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                Mathf.Abs(
                    metrics.adjustedPeakHorizontalOutwardAngleDegrees -
                    DrawBackChestSafeOutwardDegrees) <= 0.5f &&
                metrics.adjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                metrics.stateLoops &&
                !metrics.applyRootMotion &&
                metrics.blendShapeCurveBound;
            WriteJson(DrawBackRightChestReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back right-chest Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackRightChest] Captured direct Play Mode correction comparison. " +
                "DirectFrames=" + metrics.framesDirectlyCaptured +
                ", Protrusion=" +
                Num(metrics.beforeMaximumForwardProtrusionMeters) + "->" +
                Num(metrics.afterMaximumForwardProtrusionMeters) +
                ", Weight=" + Num(metrics.blendShapeWeightMinimum) + ".." +
                Num(metrics.blendShapeWeightMaximum) +
                ", MinGap=" + Num(metrics.minimumFrontSilhouetteGapMeters) +
                ", PeakElbow=" + Num(metrics.adjustedPeakElbowFlexDegrees) +
                ", Loops=2.");
        }

        private static void CaptureDrawBackRightChestCorrectionComparison(
            Transform target,
            SkinnedMeshRenderer renderer,
            AnimationClip source,
            AnimationClip adjusted,
            int diagnosticFrame,
            int peakFrame,
            Quaternion rightHandBindLocalRotation,
            string outputPath)
        {
            string rendererPath = AnimationUtility.CalculateTransformPath(
                renderer.transform,
                target);
            GameObject reviewObject = UnityEngine.Object.Instantiate(
                target.gameObject);
            reviewObject.name = target.name + "VideoReferenceReview";
            reviewObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(reviewObject);
            Transform reviewTarget = reviewObject.transform;
            SkinnedMeshRenderer reviewRenderer =
                RequireRelativeSkinnedMeshRenderer(
                    reviewTarget,
                    rendererPath);
            reviewRenderer.sharedMesh = renderer.sharedMesh;
            Mesh correctedReviewMesh = renderer.sharedMesh;
            Mesh originalReviewMesh = LoadPlayerMeshByName("char1");
            AnimationClip before = UnityEngine.Object.Instantiate(adjusted);
            before.name = "HandsDrawBackRightChestBeforeCorrectionReference";
            RemoveDrawBackRightChestBlendShapeCurve(before, rendererPath);
            int blendShapeIndex = reviewRenderer.sharedMesh.GetBlendShapeIndex(
                DrawBackRightChestBlendShapeName);
            int framesPerLoop = Mathf.Max(
                4,
                Mathf.RoundToInt(source.length * source.frameRate));
            List<int> frames = Enumerable.Range(0, framesPerLoop)
                .Select(frame =>
                {
                    ResetDrawBackRightChestBlendShapeWeights(reviewRenderer);
                    FindRequired(reviewTarget, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(
                        reviewObject,
                        source.length * frame / framesPerLoop);
                    Transform spine = FindRequired(reviewTarget, SpinePath);
                    Transform solar = FindRequired(reviewTarget, SolarPlexusPath);
                    Transform elbow = FindRequired(reviewTarget, RightForeArmPath);
                    Transform hand = FindRequired(reviewTarget, RightHandPath);
                    float elbowOutward = Vector3.Dot(
                        elbow.position - spine.position,
                        reviewTarget.right);
                    float handSolarHeightDifference = Mathf.Abs(Vector3.Dot(
                        hand.position - solar.position,
                        reviewTarget.up));
                    float score = elbowOutward -
                        handSolarHeightDifference * 1.5f;
                    return new KeyValuePair<int, float>(frame, score);
                })
                .OrderByDescending(pair => pair.Value)
                .Take(10)
                .Select(pair => pair.Key)
                .Concat(new[] { diagnosticFrame, peakFrame })
                .Select(frame => Mathf.Clamp(frame, 0, framesPerLoop - 1))
                .Distinct()
                .OrderBy(frame => frame)
                .ToList();
            while (frames.Count < 12)
            {
                int candidate = Mathf.Clamp(
                    frames.Count * (framesPerLoop - 1) / 11,
                    0,
                    framesPerLoop - 1);
                if (!frames.Contains(candidate))
                {
                    frames.Add(candidate);
                    frames.Sort();
                }
                else
                {
                    candidate = Enumerable.Range(0, framesPerLoop)
                        .First(frame => !frames.Contains(frame));
                    frames.Add(candidate);
                    frames.Sort();
                }
            }

            try
            {
                Animator runtimeAnimator = RequireAnimator(target);
                bool runtimeAnimatorEnabled = runtimeAnimator.enabled;
                float runtimeAnimatorSpeed = runtimeAnimator.speed;
                reviewObject.SetActive(false);
                try
                {
                    using (CaptureEnvironment runtimeEnvironment =
                        new CaptureEnvironment(target))
                    {
                        List<byte[]> allAdjustedBeforeFrames = new List<byte[]>();
                        int maximumHandHeightFrame = 0;
                        float maximumHandHeight = float.NegativeInfinity;
                        int runtimeStateHash = Animator.StringToHash(
                            DrawBackStateName);
                        runtimeAnimator.enabled = true;
                        runtimeAnimator.speed = 1f;
                        runtimeAnimator.Rebind();
                        runtimeAnimator.Update(0f);
                        runtimeAnimator.Play(runtimeStateHash, 0, 0f);
                        runtimeAnimator.Update(0f);
                        for (int frame = 0; frame < framesPerLoop; frame++)
                        {
                            if (frame > 0)
                            {
                                runtimeAnimator.Update(
                                    adjusted.length / framesPerLoop);
                            }

                            renderer.sharedMesh = originalReviewMesh;
                            ResetDrawBackRightChestBlendShapeWeights(renderer);
                            float handHeight = Vector3.Dot(
                                FindRequired(target, RightHandPath).position -
                                target.position,
                                target.up);
                            if (handHeight > maximumHandHeight)
                            {
                                maximumHandHeight = handHeight;
                                maximumHandHeightFrame = frame;
                            }

                            runtimeEnvironment.ConfigureElevatedView(
                                target,
                                target.position + target.up * 1.05f,
                                1.35f);
                            allAdjustedBeforeFrames.Add(
                                runtimeEnvironment.CaptureFront());
                        }

                        while (allAdjustedBeforeFrames.Count % 10 != 0)
                        {
                            allAdjustedBeforeFrames.Add(
                                allAdjustedBeforeFrames.Last());
                        }

                        List<List<byte[]>> allAdjustedBeforeRows =
                            Enumerable.Range(
                                    0,
                                    allAdjustedBeforeFrames.Count / 10)
                                .Select(row => allAdjustedBeforeFrames
                                    .Skip(row * 10)
                                    .Take(10)
                                    .ToList())
                                .ToList();
                        ComposeRows(
                            allAdjustedBeforeRows,
                            DrawBackRightChestAllAdjustedBeforeFramesPath);

                        Transform stressUpper = FindRequired(
                            target,
                            RightArmPath);
                        Transform stressLower = FindRequired(
                            target,
                            RightForeArmPath);
                        Transform stressHand = FindRequired(
                            target,
                            RightHandPath);
                        Quaternion stressUpperBaseRotation =
                            stressUpper.localRotation;
                        Quaternion stressLowerBaseRotation =
                            stressLower.localRotation;
                        Quaternion stressHandBaseRotation =
                            stressHand.localRotation;
                        List<byte[]> stressBeforeFront = new List<byte[]>();
                        List<byte[]> stressCorrectedFront = new List<byte[]>();
                        List<byte[]> stressBeforeSide = new List<byte[]>();
                        List<byte[]> stressCorrectedSide = new List<byte[]>();
                        bool stressRendererEnabled = renderer.enabled;
                        GameObject stressPreviewObject = new GameObject(
                            "HandsDrawBackRightChestVideoPosePreview",
                            typeof(MeshFilter),
                            typeof(MeshRenderer));
                        stressPreviewObject.hideFlags = HideFlags.HideAndDontSave;
                        stressPreviewObject.transform.SetParent(
                            renderer.transform,
                            false);
                        MeshFilter stressPreviewFilter =
                            stressPreviewObject.GetComponent<MeshFilter>();
                        MeshRenderer stressPreviewRenderer =
                            stressPreviewObject.GetComponent<MeshRenderer>();
                        stressPreviewRenderer.sharedMaterials =
                            renderer.sharedMaterials;
                        Mesh stressBeforeBaked = new Mesh
                        {
                            name = "HandsDrawBackRightChestVideoPoseBefore"
                        };
                        Mesh stressCorrectedBaked = new Mesh
                        {
                            name = "HandsDrawBackRightChestVideoPoseCorrected"
                        };
                        runtimeAnimator.enabled = false;
                        try
                        {
                            for (int pose = 0; pose < 6; pose++)
                            {
                                stressUpper.localRotation =
                                    stressUpperBaseRotation;
                                stressLower.localRotation =
                                    stressLowerBaseRotation;
                                stressHand.localRotation =
                                    stressHandBaseRotation;
                                float phase = pose / 5f;
                                Vector3 currentUpperDirection =
                                    stressLower.position - stressUpper.position;
                                Vector3 desiredUpperDirection =
                                    (target.right +
                                     target.up * Mathf.Lerp(0.55f, -0.05f, phase) -
                                     target.forward * 0.08f).normalized;
                                stressUpper.rotation = Quaternion.FromToRotation(
                                    currentUpperDirection,
                                    desiredUpperDirection) * stressUpper.rotation;
                                Vector3 currentLowerDirection =
                                    stressHand.position - stressLower.position;
                                Vector3 desiredLowerDirection =
                                    (-target.up +
                                     target.right * Mathf.Lerp(-0.18f, 0.06f, phase) -
                                     target.forward * 0.06f).normalized;
                                stressLower.rotation = Quaternion.FromToRotation(
                                    currentLowerDirection,
                                    desiredLowerDirection) * stressLower.rotation;
                                Vector3 stressChestCenter =
                                    (FindRequired(target, SpinePath).position +
                                     stressUpper.position) * 0.5f;
                                runtimeEnvironment.ConfigureElevatedView(
                                    target,
                                    stressChestCenter,
                                    0.62f);

                                renderer.enabled = true;
                                renderer.sharedMesh = originalReviewMesh;
                                renderer.BakeMesh(stressBeforeBaked, true);
                                renderer.enabled = false;
                                stressPreviewFilter.sharedMesh =
                                    stressBeforeBaked;
                                stressBeforeFront.Add(
                                    runtimeEnvironment.CaptureFront());
                                stressBeforeSide.Add(
                                    runtimeEnvironment.CaptureSide());

                                renderer.enabled = true;
                                renderer.sharedMesh = correctedReviewMesh;
                                renderer.BakeMesh(stressCorrectedBaked, true);
                                renderer.enabled = false;
                                stressPreviewFilter.sharedMesh =
                                    stressCorrectedBaked;
                                stressCorrectedFront.Add(
                                    runtimeEnvironment.CaptureFront());
                                stressCorrectedSide.Add(
                                    runtimeEnvironment.CaptureSide());
                            }
                        }
                        finally
                        {
                            stressUpper.localRotation = stressUpperBaseRotation;
                            stressLower.localRotation = stressLowerBaseRotation;
                            stressHand.localRotation = stressHandBaseRotation;
                            renderer.enabled = stressRendererEnabled;
                            stressPreviewFilter.sharedMesh = null;
                            UnityEngine.Object.DestroyImmediate(
                                stressPreviewObject);
                            UnityEngine.Object.DestroyImmediate(
                                stressBeforeBaked);
                            UnityEngine.Object.DestroyImmediate(
                                stressCorrectedBaked);
                        }

                        ComposeRows(
                            new[]
                            {
                                stressBeforeFront,
                                stressCorrectedFront,
                                stressBeforeSide,
                                stressCorrectedSide
                            },
                            DrawBackRightChestVideoPoseStressPath);
                        Debug.Log(
                            "[PlayerHandsDrawBackRightChest] Captured actual adjusted runtime frames. " +
                            "MaximumRightHandHeightFrame=" + maximumHandHeightFrame +
                            ", MaximumRightHandHeight=" + Num(maximumHandHeight) +
                            ", AnimatorEnabled=" + runtimeAnimatorEnabled +
                            ", AnimatorSpeed=" + Num(runtimeAnimatorSpeed) + ".");
                    }
                }
                finally
                {
                    runtimeAnimator.speed = runtimeAnimatorSpeed;
                    runtimeAnimator.enabled = runtimeAnimatorEnabled;
                    renderer.sharedMesh = correctedReviewMesh;
                    reviewObject.SetActive(true);
                }

                using (CaptureEnvironment environment =
                    new CaptureEnvironment(reviewTarget))
                {
                    List<byte[]> allSourceFrames = new List<byte[]>();
                    for (int frame = 0; frame < framesPerLoop; frame++)
                    {
                        reviewRenderer.sharedMesh = originalReviewMesh;
                        ResetDrawBackRightChestBlendShapeWeights(reviewRenderer);
                        FindRequired(reviewTarget, RightHandPath).localRotation =
                            rightHandBindLocalRotation;
                        source.SampleAnimation(
                            reviewObject,
                            source.length * frame / framesPerLoop);
                        environment.ConfigureElevatedView(
                            reviewTarget,
                            reviewTarget.position + reviewTarget.up * 1.05f,
                            1.35f);
                        allSourceFrames.Add(environment.CaptureFront());
                    }

                    while (allSourceFrames.Count % 10 != 0)
                    {
                        allSourceFrames.Add(allSourceFrames.Last());
                    }

                    List<List<byte[]>> allSourceRows = Enumerable.Range(
                            0,
                            allSourceFrames.Count / 10)
                        .Select(row => allSourceFrames
                            .Skip(row * 10)
                            .Take(10)
                            .ToList())
                        .ToList();
                    ComposeRows(
                        allSourceRows,
                        DrawBackRightChestAllSourceFramesPath);

                    List<byte[]> allAdjustedBeforePlayableFrames =
                        new List<byte[]>();
                    Animator reviewAnimator = RequireAnimator(reviewTarget);
                    using (AnimationClipPoseSampler playableSampler =
                        new AnimationClipPoseSampler(reviewAnimator, before))
                    {
                        for (int frame = 0; frame < framesPerLoop; frame++)
                        {
                            reviewRenderer.sharedMesh = originalReviewMesh;
                            ResetDrawBackRightChestBlendShapeWeights(
                                reviewRenderer);
                            playableSampler.Sample(
                                before.length * frame / framesPerLoop);
                            environment.ConfigureElevatedView(
                                reviewTarget,
                                reviewTarget.position +
                                reviewTarget.up * 1.05f,
                                1.35f);
                            allAdjustedBeforePlayableFrames.Add(
                                environment.CaptureFront());
                        }
                    }

                    while (allAdjustedBeforePlayableFrames.Count % 10 != 0)
                    {
                        allAdjustedBeforePlayableFrames.Add(
                            allAdjustedBeforePlayableFrames.Last());
                    }

                    List<List<byte[]>> allAdjustedBeforePlayableRows =
                        Enumerable.Range(
                                0,
                                allAdjustedBeforePlayableFrames.Count / 10)
                            .Select(row => allAdjustedBeforePlayableFrames
                                .Skip(row * 10)
                                .Take(10)
                                .ToList())
                            .ToList();
                    ComposeRows(
                        allAdjustedBeforePlayableRows,
                        DrawBackRightChestAllAdjustedBeforeFramesPath);

                    List<byte[]> sourceFront = new List<byte[]>();
                    List<byte[]> beforeFront = new List<byte[]>();
                    List<byte[]> correctedFront = new List<byte[]>();
                    List<byte[]> correctedFull = new List<byte[]>();
                    List<byte[]> correctedSide = new List<byte[]>();
                    foreach (int frame in frames)
                    {
                        float phase = frame / (float)framesPerLoop;
                        reviewRenderer.sharedMesh = originalReviewMesh;
                        ResetDrawBackRightChestBlendShapeWeights(reviewRenderer);
                        FindRequired(reviewTarget, RightHandPath).localRotation =
                            rightHandBindLocalRotation;
                        source.SampleAnimation(
                            reviewObject,
                            phase * source.length);
                        Vector3 chestCenter =
                            (FindRequired(reviewTarget, SpinePath).position +
                             FindRequired(reviewTarget, RightArmPath).position) * 0.5f;
                        environment.ConfigureElevatedView(
                            reviewTarget,
                            chestCenter,
                            0.58f);
                        sourceFront.Add(environment.CaptureFront());

                        reviewRenderer.sharedMesh = originalReviewMesh;
                        ResetDrawBackRightChestBlendShapeWeights(reviewRenderer);
                        before.SampleAnimation(
                            reviewObject,
                            phase * before.length);
                        chestCenter =
                            (FindRequired(reviewTarget, SpinePath).position +
                             FindRequired(reviewTarget, RightArmPath).position) * 0.5f;
                        environment.ConfigureElevatedView(
                            reviewTarget,
                            chestCenter,
                            0.58f);
                        beforeFront.Add(environment.CaptureFront());

                        reviewRenderer.sharedMesh = correctedReviewMesh;
                        adjusted.SampleAnimation(
                            reviewObject,
                            phase * adjusted.length);
                        chestCenter =
                            (FindRequired(reviewTarget, SpinePath).position +
                             FindRequired(reviewTarget, RightArmPath).position) * 0.5f;
                        environment.ConfigureElevatedView(
                            reviewTarget,
                            chestCenter,
                            0.58f);
                        correctedFront.Add(environment.CaptureFront());
                        correctedSide.Add(environment.CaptureSide());
                        environment.ConfigureElevatedView(
                            reviewTarget,
                            reviewTarget.position + reviewTarget.up * 1.05f,
                            1.35f);
                        correctedFull.Add(environment.CaptureFront());
                    }

                    ComposeRows(
                        new[]
                        {
                            sourceFront.Take(6).ToList(),
                            sourceFront.Skip(6).Take(6).ToList(),
                            beforeFront.Take(6).ToList(),
                            beforeFront.Skip(6).Take(6).ToList(),
                            correctedFront.Take(6).ToList(),
                            correctedFront.Skip(6).Take(6).ToList(),
                            correctedFull.Take(6).ToList(),
                            correctedFull.Skip(6).Take(6).ToList(),
                            correctedSide.Take(6).ToList(),
                            correctedSide.Skip(6).Take(6).ToList()
                        },
                        outputPath);

                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(before);
                UnityEngine.Object.DestroyImmediate(reviewObject);
            }
        }

        private static void
            CapturePlayerHandsDrawBackChestDeformationFixActualReview()
        {
            DrawBackChestDeformationApplyMetrics apply =
                ReadJson<DrawBackChestDeformationApplyMetrics>(
                    DrawBackChestDeformationApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back chest-deformation apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back chest-deformation adjusted clip is missing.");
            }

            CaptureDrawBackChestDeformationComparison(
                target,
                source,
                apply.minimumFrontSilhouetteGapFrame,
                apply.sourcePeakFrame,
                apply.rightHandBindLocalRotation,
                DrawBackChestDeformationReviewPath);
            DrawBackOuterElbowReviewMetrics runtime =
                CaptureDrawBackOuterElbowMetrics(
                    target,
                    source,
                    adjusted,
                    Mathf.Max(0, apply.sourcePeakFrame - 23),
                    Mathf.Max(0, apply.sourcePeakFrame - 12),
                    apply.sourcePeakFrame,
                    apply.rightHandBindLocalRotation);
            MeasureDrawBackClipFrontSilhouetteGap(
                target,
                adjusted,
                out float minimumGap,
                out int minimumGapFrame);
            DrawBackChestDeformationReviewMetrics metrics =
                new DrawBackChestDeformationReviewMetrics
                {
                    target = DrawBackTargetName,
                    framesPerLoop = runtime.framesPerLoop,
                    framesDirectlyCaptured = 12,
                    framesSampled = runtime.framesSampled,
                    loopsSampled = runtime.loopsSampled,
                    sourcePeakFrame = runtime.sourcePeakFrame,
                    adjustedPeakFrame = runtime.adjustedPeakFrame,
                    previousPeakHorizontalOutwardAngleDegrees =
                        apply.previousPeakHorizontalOutwardAngleDegrees,
                    adjustedPeakHorizontalOutwardAngleDegrees =
                        runtime.adjustedPeakHorizontalForwardAngleDegrees,
                    outwardAngleReductionDegrees =
                        apply.previousPeakHorizontalOutwardAngleDegrees -
                        runtime.adjustedPeakHorizontalForwardAngleDegrees,
                    minimumFrontSilhouetteGapMeters = minimumGap,
                    minimumFrontSilhouetteGapFrame = minimumGapFrame,
                    rootPositionDisplacementMax =
                        runtime.rootPositionDisplacementMax,
                    runtimeAdjustedPosePositionDifferenceMax =
                        runtime.runtimeAdjustedPosePositionDifferenceMax,
                    runtimeAdjustedPoseRotationDifferenceDegreesMax =
                        runtime.runtimeAdjustedPoseRotationDifferenceDegreesMax,
                    unchangedPosePositionDifferenceMax =
                        runtime.unchangedPosePositionDifferenceMax,
                    unchangedPoseRotationDifferenceDegreesMax =
                        runtime.unchangedPoseRotationDifferenceDegreesMax,
                    adjustedPeakElbowFlexDegrees =
                        runtime.adjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        runtime.adjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        runtime.adjustedPeakPalmCharacterLeftAngleDegrees,
                    stateLoops = runtime.stateLoops,
                    applyRootMotion = runtime.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.sourcePeakFrame == metrics.adjustedPeakFrame &&
                metrics.framesSampled == metrics.framesPerLoop * 2 &&
                metrics.loopsSampled == 2 &&
                metrics.minimumFrontSilhouetteGapMeters >= 0.005f &&
                metrics.rootPositionDisplacementMax <= PositionTolerance &&
                metrics.runtimeAdjustedPosePositionDifferenceMax <= PositionTolerance &&
                metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.unchangedPosePositionDifferenceMax <= PositionTolerance &&
                metrics.unchangedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                Mathf.Abs(
                    metrics.adjustedPeakHorizontalOutwardAngleDegrees -
                    DrawBackChestSafeOutwardDegrees) <= 0.5f &&
                metrics.outwardAngleReductionDegrees >= 5f &&
                metrics.adjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                metrics.stateLoops &&
                !metrics.applyRootMotion;
            WriteJson(DrawBackChestDeformationReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back chest-deformation Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackChestDeformation] Captured direct Play Mode chest comparison. " +
                "DirectFrames=" + metrics.framesDirectlyCaptured +
                ", Sampled=" + metrics.framesSampled +
                ", Outward=" +
                Num(metrics.previousPeakHorizontalOutwardAngleDegrees) + "->" +
                Num(metrics.adjustedPeakHorizontalOutwardAngleDegrees) +
                ", MinGap=" + Num(metrics.minimumFrontSilhouetteGapMeters) +
                "@" + metrics.minimumFrontSilhouetteGapFrame +
                ", PeakElbow=" + Num(metrics.adjustedPeakElbowFlexDegrees) +
                ", RuntimePose=" +
                Num(metrics.runtimeAdjustedPosePositionDifferenceMax) + "/" +
                Num(metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax) +
                ", Loops=2.");
        }

        private static void CaptureDrawBackChestDeformationComparison(
            Transform target,
            AnimationClip source,
            int minimumGapFrame,
            int peakFrame,
            Quaternion rightHandBindLocalRotation,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            int framesPerLoop = Mathf.Max(
                4,
                Mathf.RoundToInt(source.length * source.frameRate));
            List<int> frames = Enumerable.Range(0, 12)
                .Select(index => Mathf.RoundToInt(
                    (framesPerLoop - 1) * index / 11f))
                .ToList();
            if (!frames.Contains(minimumGapFrame))
            {
                int replaceIndex = Enumerable.Range(1, frames.Count - 2)
                    .OrderBy(index => Mathf.Abs(frames[index] - minimumGapFrame))
                    .First();
                frames[replaceIndex] = minimumGapFrame;
                frames.Sort();
            }

            if (!frames.Contains(peakFrame))
            {
                frames[frames.Count - 2] = peakFrame;
                frames.Sort();
            }

            using (CaptureEnvironment environment = new CaptureEnvironment(target))
            {
                List<byte[]> sourceChestFront = new List<byte[]>();
                List<byte[]> adjustedChestFront = new List<byte[]>();
                List<byte[]> adjustedFullFront = new List<byte[]>();
                List<byte[]> adjustedChestSide = new List<byte[]>();
                foreach (int frame in frames)
                {
                    float phase = frame / (float)framesPerLoop;
                    FindRequired(target, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(
                        target.gameObject,
                        phase * source.length);
                    Vector3 sourceChestCenter =
                        (FindRequired(target, SpinePath).position +
                         FindRequired(target, RightArmPath).position) * 0.5f;
                    environment.ConfigureView(target, sourceChestCenter, 0.58f);
                    sourceChestFront.Add(environment.CaptureFront());

                    SampleAnimator(animator, DrawBackStateName, phase);
                    Vector3 adjustedChestCenter =
                        (FindRequired(target, SpinePath).position +
                         FindRequired(target, RightArmPath).position) * 0.5f;
                    environment.ConfigureView(target, adjustedChestCenter, 0.58f);
                    adjustedChestFront.Add(environment.CaptureFront());
                    adjustedChestSide.Add(environment.CaptureSide());
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    adjustedFullFront.Add(environment.CaptureFront());
                }

                ComposeRows(
                    new[]
                    {
                        sourceChestFront.Take(6).ToList(),
                        sourceChestFront.Skip(6).Take(6).ToList(),
                        adjustedChestFront.Take(6).ToList(),
                        adjustedChestFront.Skip(6).Take(6).ToList(),
                        adjustedFullFront.Take(6).ToList(),
                        adjustedFullFront.Skip(6).Take(6).ToList(),
                        adjustedChestSide.Take(6).ToList(),
                        adjustedChestSide.Skip(6).Take(6).ToList()
                    },
                    outputPath);
            }

            animator.Rebind();
            animator.Update(0f);
        }

        private static void
            CapturePlayerHandsDrawBackFrontSilhouetteClearanceActualReview()
        {
            DrawBackFrontSilhouetteApplyMetrics apply =
                ReadJson<DrawBackFrontSilhouetteApplyMetrics>(
                    DrawBackFrontSilhouetteApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back front-silhouette apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back front-silhouette adjusted clip is missing.");
            }

            CaptureDrawBackFrontSilhouetteComparison(
                target,
                source,
                apply.minimumFrontSilhouetteGapFrame,
                apply.sourcePeakFrame,
                DrawBackFrontSilhouetteReviewPath);
            DrawBackOuterElbowReviewMetrics runtime =
                CaptureDrawBackOuterElbowMetrics(
                    target,
                    source,
                    adjusted,
                    Mathf.Max(0, apply.sourcePeakFrame - 23),
                    Mathf.Max(0, apply.sourcePeakFrame - 12),
                    apply.sourcePeakFrame,
                    apply.rightHandBindLocalRotation);
            MeasureDrawBackClipFrontSilhouetteGap(
                target,
                adjusted,
                out float minimumGap,
                out int minimumGapFrame);
            DrawBackFrontSilhouetteReviewMetrics metrics =
                new DrawBackFrontSilhouetteReviewMetrics
                {
                    target = DrawBackTargetName,
                    framesPerLoop = runtime.framesPerLoop,
                    framesDirectlyCaptured = 12,
                    framesSampled = runtime.framesSampled,
                    loopsSampled = runtime.loopsSampled,
                    sourcePeakFrame = runtime.sourcePeakFrame,
                    adjustedPeakFrame = runtime.adjustedPeakFrame,
                    minimumFrontSilhouetteGapMeters = minimumGap,
                    minimumFrontSilhouetteGapFrame = minimumGapFrame,
                    rootPositionDisplacementMax =
                        runtime.rootPositionDisplacementMax,
                    runtimeAdjustedPosePositionDifferenceMax =
                        runtime.runtimeAdjustedPosePositionDifferenceMax,
                    runtimeAdjustedPoseRotationDifferenceDegreesMax =
                        runtime.runtimeAdjustedPoseRotationDifferenceDegreesMax,
                    unchangedPosePositionDifferenceMax =
                        runtime.unchangedPosePositionDifferenceMax,
                    unchangedPoseRotationDifferenceDegreesMax =
                        runtime.unchangedPoseRotationDifferenceDegreesMax,
                    adjustedPeakElbowFlexDegrees =
                        runtime.adjustedPeakElbowFlexDegrees,
                    adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                        runtime.adjustedPeakHandSolarPlexusHeightDifferenceMeters,
                    adjustedPeakHorizontalForwardAngleDegrees =
                        runtime.adjustedPeakHorizontalForwardAngleDegrees,
                    adjustedPeakPalmCharacterLeftAngleDegrees =
                        runtime.adjustedPeakPalmCharacterLeftAngleDegrees,
                    stateLoops = runtime.stateLoops,
                    applyRootMotion = runtime.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.sourcePeakFrame == metrics.adjustedPeakFrame &&
                metrics.framesSampled == metrics.framesPerLoop * 2 &&
                metrics.loopsSampled == 2 &&
                metrics.minimumFrontSilhouetteGapMeters >= 0.005f &&
                metrics.rootPositionDisplacementMax <= PositionTolerance &&
                metrics.runtimeAdjustedPosePositionDifferenceMax <= PositionTolerance &&
                metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.unchangedPosePositionDifferenceMax <= PositionTolerance &&
                metrics.unchangedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                metrics.adjustedPeakHorizontalForwardAngleDegrees >= 5f &&
                metrics.adjustedPeakHorizontalForwardAngleDegrees <= 45f &&
                metrics.adjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                metrics.stateLoops &&
                !metrics.applyRootMotion;
            WriteJson(DrawBackFrontSilhouetteReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back front-silhouette Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackFrontSilhouette] Captured direct Play Mode review. " +
                "DirectFrames=" + metrics.framesDirectlyCaptured +
                ", Sampled=" + metrics.framesSampled +
                ", MinGap=" + Num(metrics.minimumFrontSilhouetteGapMeters) +
                "@" + metrics.minimumFrontSilhouetteGapFrame +
                ", PeakElbow=" + Num(metrics.adjustedPeakElbowFlexDegrees) +
                ", PeakOutwardAngle=" +
                Num(metrics.adjustedPeakHorizontalForwardAngleDegrees) +
                ", RuntimePose=" +
                Num(metrics.runtimeAdjustedPosePositionDifferenceMax) + "/" +
                Num(metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax) +
                ", Loops=2.");
        }

        private static void CaptureDrawBackFrontSilhouetteComparison(
            Transform target,
            AnimationClip source,
            int minimumGapFrame,
            int peakFrame,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            int framesPerLoop = Mathf.Max(
                4,
                Mathf.RoundToInt(source.length * source.frameRate));
            List<int> frames = Enumerable.Range(0, 12)
                .Select(index => Mathf.RoundToInt(
                    (framesPerLoop - 1) * index / 11f))
                .ToList();
            if (!frames.Contains(minimumGapFrame))
            {
                int replaceIndex = Enumerable.Range(1, frames.Count - 2)
                    .OrderBy(index => Mathf.Abs(frames[index] - minimumGapFrame))
                    .First();
                frames[replaceIndex] = minimumGapFrame;
                frames.Sort();
            }

            if (!frames.Contains(peakFrame))
            {
                frames[frames.Count - 2] = peakFrame;
                frames.Sort();
            }

            using (CaptureEnvironment environment = new CaptureEnvironment(target))
            {
                List<byte[]> fullFront = new List<byte[]>();
                List<byte[]> closeFront = new List<byte[]>();
                List<byte[]> closeSide = new List<byte[]>();
                foreach (int frame in frames)
                {
                    SampleAnimator(
                        animator,
                        DrawBackStateName,
                        frame / (float)framesPerLoop);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    fullFront.Add(environment.CaptureFront());
                    Vector3 armCenter =
                        (FindRequired(target, RightArmPath).position +
                         FindRequired(target, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(target, armCenter, 0.78f);
                    closeFront.Add(environment.CaptureFront());
                    closeSide.Add(environment.CaptureSide());
                }

                ComposeRows(
                    new[]
                    {
                        fullFront.Take(6).ToList(),
                        fullFront.Skip(6).Take(6).ToList(),
                        closeFront.Take(6).ToList(),
                        closeFront.Skip(6).Take(6).ToList(),
                        closeSide.Take(6).ToList(),
                        closeSide.Skip(6).Take(6).ToList()
                    },
                    outputPath);
            }

            animator.Rebind();
            animator.Update(0f);
        }

        private static void MeasureDrawBackClipFrontSilhouetteGap(
            Transform template,
            AnimationClip clip,
            out float minimumGap,
            out int minimumGapFrame)
        {
            GameObject workObject = UnityEngine.Object.Instantiate(template.gameObject);
            workObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(workObject);
            try
            {
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(clip.length * clip.frameRate));
                minimumGap = float.PositiveInfinity;
                minimumGapFrame = 0;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    clip.SampleAnimation(
                        workObject,
                        clip.length * frame / framesPerLoop);
                    float gap = MeasureRightArmFrontSilhouetteGap(
                        workObject.transform,
                        FindRequired(workObject.transform, RightArmPath),
                        FindRequired(workObject.transform, RightForeArmPath),
                        FindRequired(workObject.transform, RightHandPath));
                    if (gap < minimumGap)
                    {
                        minimumGap = gap;
                        minimumGapFrame = frame;
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(workObject);
            }
        }

        private static void
            CapturePlayerTransporterPurpleFlagDrawBackClearanceAndStartActualReview()
        {
            TransporterPurpleFlagApplyMetrics apply =
                ReadJson<TransporterPurpleFlagApplyMetrics>(
                    TransporterPurpleFlagApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Transporter purple-flag apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform drawBackTarget = RequireTarget(layout, DrawBackTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back clearance-adjusted clip is missing.");
            }

            Quaternion rightHandBindLocalRotation =
                FindRequired(drawBackTarget, RightHandPath).localRotation;
            CaptureTransporterPurpleFlagDrawBackAndStartContactSheet(
                scene,
                layout,
                source,
                apply.minimumClearanceFrame,
                TransporterPurpleFlagReviewPath);
            DrawBackOuterElbowReviewMetrics runtime =
                CaptureDrawBackOuterElbowMetrics(
                    drawBackTarget,
                    source,
                    adjusted,
                    apply.extractionStartFrame,
                    apply.outerPathFrame,
                    apply.sourcePeakFrame,
                    rightHandBindLocalRotation);
            MeasureDrawBackClipClearance(
                drawBackTarget,
                adjusted,
                out float minimumClearance,
                out int minimumClearanceFrame);

            Transform emptyTarget = RequireTarget(layout, EmptyTargetName);
            Camera mainCamera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .Single(camera => camera.CompareTag("MainCamera"));
            Bounds emptyBounds = CalculateVisibleBounds(emptyTarget);
            Vector3 viewportCenter = mainCamera.WorldToViewportPoint(emptyBounds.center);
            float horizontalError = Mathf.Abs(viewportCenter.x - 0.5f);
            float verticalError = Mathf.Abs(viewportCenter.y - 0.5f);
            bool sharedTextureApplied =
                AllSharedPlayerInstancesUseTransporterTexture(scene) &&
                string.Equals(
                    HashFile(TransporterTexturePath),
                    HashFile(TransporterTextureDuplicatePath),
                    StringComparison.Ordinal);
            TransporterPurpleFlagReviewMetrics metrics =
                new TransporterPurpleFlagReviewMetrics
                {
                    targetSet = apply.targetSet,
                    sharedPlayerModelInstanceCount =
                        CountSharedPlayerModelInstances(scene),
                    transporterTargetsDirectlyCaptured = 4,
                    drawBackFramesDirectlyCaptured = 8,
                    drawBackFramesSampled = runtime.framesSampled,
                    drawBackLoopsSampled = runtime.loopsSampled,
                    minimumRightArmTorsoClearanceMeters = minimumClearance,
                    minimumClearanceFrame = minimumClearanceFrame,
                    runtimeAdjustedPosePositionDifferenceMax =
                        runtime.runtimeAdjustedPosePositionDifferenceMax,
                    runtimeAdjustedPoseRotationDifferenceDegreesMax =
                        runtime.runtimeAdjustedPoseRotationDifferenceDegreesMax,
                    startScreenHorizontalCenterErrorNormalized = horizontalError,
                    startScreenVerticalCenterErrorNormalized = verticalError,
                    stateLoops = runtime.stateLoops,
                    applyRootMotion = runtime.applyRootMotion,
                    sharedTextureAppliedToAllTransporters = sharedTextureApplied,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                sharedTextureApplied &&
                metrics.sharedPlayerModelInstanceCount ==
                    apply.sharedPlayerModelInstanceCount &&
                minimumClearance >= -0.004f &&
                metrics.runtimeAdjustedPosePositionDifferenceMax <= 0.001f &&
                metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax <= 0.1f &&
                horizontalError <= 0.01f &&
                verticalError <= 0.01f &&
                runtime.stateLoops &&
                !runtime.applyRootMotion;
            WriteJson(TransporterPurpleFlagReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Transporter purple-flag Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerTransporterPurpleFlag] Captured direct Play Mode review. " +
                "Instances=" + metrics.sharedPlayerModelInstanceCount +
                ", DrawBackFrames=" + metrics.drawBackFramesDirectlyCaptured +
                "/" + metrics.drawBackFramesSampled +
                ", MinClearance=" + Num(minimumClearance) +
                "@" + minimumClearanceFrame +
                ", StartCenterError=" + Num(horizontalError) +
                "/" + Num(verticalError) + ".");
        }

        private static void CaptureTransporterPurpleFlagDrawBackAndStartContactSheet(
            Scene scene,
            Transform layout,
            AnimationClip drawBackSource,
            int minimumClearanceFrame,
            string outputPath)
        {
            List<byte[]> flagPanels = new List<byte[]>();
            string[] flagTargets =
            {
                EmptyTargetName,
                OneHandTargetName,
                TwoHandTargetName,
                DrawBackTargetName
            };
            for (int targetIndex = 0; targetIndex < flagTargets.Length; targetIndex++)
            {
                Transform target = RequireTarget(layout, flagTargets[targetIndex]);
                Animator animator = RequireAnimator(target);
                animator.Update(0f);
                using (CaptureEnvironment environment = new CaptureEnvironment(target))
                {
                    Vector3 armCenter =
                        (FindRequired(target, LeftArmPath).position +
                         FindRequired(target, LeftForeArmPath).position) * 0.5f;
                    environment.ConfigureView(target, armCenter, 0.34f);
                    flagPanels.Add(environment.CaptureFront());
                    flagPanels.Add(environment.CaptureSide());
                }
            }

            Transform drawBackTarget = RequireTarget(layout, DrawBackTargetName);
            Animator drawBackAnimator = RequireAnimator(drawBackTarget);
            int framesPerLoop = Mathf.Max(
                4,
                Mathf.RoundToInt(drawBackSource.length * drawBackSource.frameRate));
            List<int> frameList = Enumerable.Range(0, 8)
                .Select(index => Mathf.RoundToInt(
                    (framesPerLoop - 1) * index / 7f))
                .ToList();
            if (!frameList.Contains(minimumClearanceFrame))
            {
                int replaceIndex = Enumerable.Range(1, frameList.Count - 2)
                    .OrderBy(index =>
                        Mathf.Abs(frameList[index] - minimumClearanceFrame))
                    .First();
                frameList[replaceIndex] = minimumClearanceFrame;
                frameList.Sort();
            }

            List<byte[]> drawBackFront = new List<byte[]>();
            List<byte[]> drawBackSide = new List<byte[]>();
            List<byte[]> drawBackClose = new List<byte[]>();
            using (CaptureEnvironment environment = new CaptureEnvironment(drawBackTarget))
            {
                foreach (int frame in frameList)
                {
                    float phase = frame / (float)framesPerLoop;
                    SampleAnimator(drawBackAnimator, DrawBackStateName, phase);
                    environment.ConfigureView(drawBackTarget, 1.05f, 1.35f);
                    drawBackFront.Add(environment.CaptureFront());
                    drawBackSide.Add(environment.CaptureSide());
                    Vector3 rightArmCenter =
                        (FindRequired(drawBackTarget, RightArmPath).position +
                         FindRequired(drawBackTarget, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(drawBackTarget, rightArmCenter, 0.62f);
                    drawBackClose.Add(environment.CaptureFront());
                }
            }

            Camera mainCamera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .Single(camera => camera.CompareTag("MainCamera"));
            byte[] startScreen = CaptureCameraFrame(mainCamera);
            List<byte[]> startPanels = Enumerable.Range(0, 8)
                .Select(_ => startScreen)
                .ToList();
            ComposeRows(
                new[]
                {
                    flagPanels,
                    drawBackFront,
                    drawBackSide,
                    drawBackClose,
                    startPanels
                },
                outputPath);
            drawBackAnimator.Rebind();
            drawBackAnimator.Update(0f);
        }

        private static byte[] CaptureCameraFrame(Camera camera)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = new RenderTexture(
                CaptureWidth,
                CaptureHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2
            };
            Texture2D frame = new Texture2D(
                CaptureWidth,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                frame.ReadPixels(
                    new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                    0,
                    0,
                    false);
                frame.Apply(false, false);
                return frame.EncodeToPNG();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(frame);
            }
        }

        private static Bounds CalculateVisibleBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    target.name + " has no visible bounds.");
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static void MeasureDrawBackClipClearance(
            Transform template,
            AnimationClip clip,
            out float minimumClearance,
            out int minimumClearanceFrame)
        {
            GameObject workObject = UnityEngine.Object.Instantiate(template.gameObject);
            workObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(workObject);
            try
            {
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(clip.length * clip.frameRate));
                minimumClearance = float.PositiveInfinity;
                minimumClearanceFrame = 0;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    clip.SampleAnimation(
                        workObject,
                        clip.length * frame / framesPerLoop);
                    float clearance = MeasureRightArmTorsoClearance(
                        workObject.transform,
                        FindRequired(workObject.transform, RightArmPath),
                        FindRequired(workObject.transform, RightForeArmPath),
                        FindRequired(workObject.transform, RightHandPath));
                    if (clearance < minimumClearance)
                    {
                        minimumClearance = clearance;
                        minimumClearanceFrame = frame;
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(workObject);
            }
        }

        private static void CaptureHandsDrawBackOuterElbowPathActualReview()
        {
            DrawBackOuterElbowApplyMetrics apply =
                ReadJson<DrawBackOuterElbowApplyMetrics>(
                    DrawBackOuterElbowApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back outer-elbow apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back outer-elbow adjusted clip is missing.");
            }

            CaptureDrawBackOuterElbowComparison(
                target,
                source,
                apply.extractionStartFrame,
                apply.outerPathFrame,
                apply.sourcePeakFrame,
                apply.rightHandBindLocalRotation,
                DrawBackOuterElbowReviewPath);
            DrawBackOuterElbowReviewMetrics metrics =
                CaptureDrawBackOuterElbowMetrics(
                    target,
                    source,
                    adjusted,
                    apply.extractionStartFrame,
                    apply.outerPathFrame,
                    apply.sourcePeakFrame,
                    apply.rightHandBindLocalRotation);
            metrics.validationPriority =
                "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증";
            WriteJson(DrawBackOuterElbowReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back outer-elbow Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackOuterElbow] Captured extraction path comparison in Play Mode. " +
                "Frames=" + metrics.extractionStartFrame + "/" +
                metrics.outerPathFrame + "/" + metrics.sourcePeakFrame +
                ", ElbowBeyondTorso=" +
                Num(metrics.adjustedElbowBeyondTorsoMeters) +
                ", HandBeyondTorso=" +
                Num(metrics.adjustedHandBeyondTorsoMeters) +
                ", ElbowBeyondHand=" +
                Num(metrics.adjustedElbowBeyondHandMeters) +
                ", RuntimePose=" +
                Num(metrics.runtimeAdjustedPosePositionDifferenceMax) + "/" +
                Num(metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax) +
                ", Loops=2.");
        }

        private static void CaptureDrawBackOuterElbowComparison(
            Transform target,
            AnimationClip source,
            int extractionStartFrame,
            int outerPathFrame,
            int sourcePeakFrame,
            Quaternion rightHandBindLocalRotation,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            int framesPerLoop = Mathf.Max(
                4,
                Mathf.RoundToInt(source.length * source.frameRate));
            int[] frames = new[]
                {
                    0,
                    extractionStartFrame,
                    Mathf.RoundToInt(Mathf.Lerp(extractionStartFrame, sourcePeakFrame, 0.2f)),
                    Mathf.RoundToInt(Mathf.Lerp(extractionStartFrame, sourcePeakFrame, 0.4f)),
                    outerPathFrame,
                    Mathf.RoundToInt(Mathf.Lerp(extractionStartFrame, sourcePeakFrame, 0.6f)),
                    Mathf.RoundToInt(Mathf.Lerp(extractionStartFrame, sourcePeakFrame, 0.8f)),
                    sourcePeakFrame
                }
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            CaptureEnvironment environment = new CaptureEnvironment(target);
            try
            {
                List<List<byte[]>> rows = Enumerable.Range(0, 8)
                    .Select(_ => new List<byte[]>())
                    .ToList();
                foreach (int frame in frames)
                {
                    float phase = frame / (float)framesPerLoop;
                    FindRequired(target, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(target.gameObject, phase * source.length);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    rows[0].Add(environment.CaptureFront());
                    rows[1].Add(environment.CaptureSide());
                    Vector3 sourceArmCenter =
                        (FindRequired(target, RightArmPath).position +
                         FindRequired(target, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(target, sourceArmCenter, 0.62f);
                    rows[4].Add(environment.CaptureFront());
                    rows[5].Add(environment.CaptureSide());

                    SampleAnimator(animator, DrawBackStateName, phase);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());
                    Vector3 adjustedArmCenter =
                        (FindRequired(target, RightArmPath).position +
                         FindRequired(target, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(target, adjustedArmCenter, 0.62f);
                    rows[6].Add(environment.CaptureFront());
                    rows[7].Add(environment.CaptureSide());
                }

                ComposeRows(rows, outputPath);
            }
            finally
            {
                environment.Dispose();
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static DrawBackOuterElbowReviewMetrics
            CaptureDrawBackOuterElbowMetrics(
                Transform target,
                AnimationClip source,
                AnimationClip adjusted,
                int extractionStartFrame,
                int outerPathFrame,
                int sourcePeakFrame,
                Quaternion rightHandBindLocalRotation)
        {
            Animator animator = RequireAnimator(target);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = target.position;
            float rootMax = 0f;
            float runtimePositionMax = 0f;
            float runtimeRotationMax = 0f;
            float unchangedPositionMax = 0f;
            float unchangedRotationMax = 0f;
            float adjustedPeakProjection = float.NegativeInfinity;
            int adjustedPeakFrame = 0;
            float sourceOuterElbowLateral = 0f;
            float adjustedOuterElbowLateral = 0f;
            float sourceOuterHandLateral = 0f;
            float adjustedOuterHandLateral = 0f;
            float torsoOuterBoundaryLateral = 0f;
            float peakHeightDifference = 0f;
            float peakElbowFlex = 0f;
            float peakHorizontalForwardAngle = 0f;
            float peakPalmLeftAngle = 0f;
            GameObject sourceObject = UnityEngine.Object.Instantiate(target.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(target.gameObject);
            sourceObject.name = target.name + "OuterElbowSourceReference";
            adjustedObject.name = target.name + "OuterElbowAdjustedReference";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(adjustedObject);
            try
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(source.length * source.frameRate));
                for (int frame = 0; frame < framesPerLoop * 2; frame++)
                {
                    int phaseFrame = frame % framesPerLoop;
                    float time = source.length * phaseFrame / framesPerLoop;
                    FindRequired(sourceObject.transform, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(sourceObject, time);
                    adjusted.SampleAnimation(adjustedObject, time);
                    SampleAnimator(
                        animator,
                        DrawBackStateName,
                        frame / (float)framesPerLoop);
                    PoseSnapshot sourcePose = CapturePose(sourceObject.transform);
                    PoseSnapshot adjustedPose = CapturePose(adjustedObject.transform);
                    PoseSnapshot runtimePose = CapturePose(target);
                    MeasureArmaturePoseDifference(
                        adjustedPose,
                        runtimePose,
                        out float runtimePositionDifference,
                        out float runtimeRotationDifference);
                    MeasurePoseDifferenceExceptDrawBackRightArm(
                        sourcePose,
                        adjustedPose,
                        out float unchangedPositionDifference,
                        out float unchangedRotationDifference);
                    runtimePositionMax = Mathf.Max(
                        runtimePositionMax,
                        runtimePositionDifference);
                    runtimeRotationMax = Mathf.Max(
                        runtimeRotationMax,
                        runtimeRotationDifference);
                    unchangedPositionMax = Mathf.Max(
                        unchangedPositionMax,
                        unchangedPositionDifference);
                    unchangedRotationMax = Mathf.Max(
                        unchangedRotationMax,
                        unchangedRotationDifference);
                    Transform adjustedUpper = FindRequired(
                        adjustedObject.transform,
                        RightArmPath);
                    Transform adjustedLower = FindRequired(
                        adjustedObject.transform,
                        RightForeArmPath);
                    Transform adjustedHand = FindRequired(
                        adjustedObject.transform,
                        RightHandPath);
                    float projection = Vector3.Dot(
                        adjustedHand.position - adjustedUpper.position,
                        adjustedObject.transform.forward);
                    if (projection > adjustedPeakProjection)
                    {
                        adjustedPeakProjection = projection;
                        adjustedPeakFrame = phaseFrame;
                    }

                    if (phaseFrame == outerPathFrame)
                    {
                        Transform sourceSpine = FindRequired(
                            sourceObject.transform,
                            SpinePath);
                        Transform sourceLower = FindRequired(
                            sourceObject.transform,
                            RightForeArmPath);
                        Transform sourceHand = FindRequired(
                            sourceObject.transform,
                            RightHandPath);
                        Transform adjustedSpine = FindRequired(
                            adjustedObject.transform,
                            SpinePath);
                        Transform adjustedShoulder = FindRequired(
                            adjustedObject.transform,
                            RightShoulderPath);
                        sourceOuterElbowLateral = Vector3.Dot(
                            sourceLower.position - sourceSpine.position,
                            sourceObject.transform.right);
                        sourceOuterHandLateral = Vector3.Dot(
                            sourceHand.position - sourceSpine.position,
                            sourceObject.transform.right);
                        adjustedOuterElbowLateral = Vector3.Dot(
                            adjustedLower.position - adjustedSpine.position,
                            adjustedObject.transform.right);
                        adjustedOuterHandLateral = Vector3.Dot(
                            adjustedHand.position - adjustedSpine.position,
                            adjustedObject.transform.right);
                        torsoOuterBoundaryLateral = Vector3.Dot(
                            adjustedShoulder.position - adjustedSpine.position,
                            adjustedObject.transform.right);
                    }

                    if (phaseFrame == sourcePeakFrame)
                    {
                        Transform solarPlexus = FindRequired(
                            adjustedObject.transform,
                            SolarPlexusPath);
                        peakHeightDifference = Mathf.Abs(Vector3.Dot(
                            adjustedHand.position - solarPlexus.position,
                            adjustedObject.transform.up));
                        peakElbowFlex = ElbowFlexDegrees(
                            adjustedUpper,
                            adjustedLower,
                            adjustedHand);
                        Vector3 horizontalDirection = Vector3.ProjectOnPlane(
                            adjustedHand.position - adjustedUpper.position,
                            adjustedObject.transform.up);
                        if (horizontalDirection.sqrMagnitude < 0.0000001f)
                        {
                            throw new InvalidOperationException(
                                "Hands_Draw_Back has no horizontal forward arm direction at peak.");
                        }

                        peakHorizontalForwardAngle = Vector3.Angle(
                            horizontalDirection,
                            adjustedObject.transform.forward);
                        peakPalmLeftAngle = Vector3.Angle(
                            -adjustedHand.right,
                            -adjustedObject.transform.right);
                    }

                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(target.position, rootBaseline));
                }

                SampleAnimator(animator, DrawBackStateName, 0f);
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                DrawBackOuterElbowReviewMetrics metrics =
                    new DrawBackOuterElbowReviewMetrics
                    {
                        target = target.name,
                        sourcePeakFrame = sourcePeakFrame,
                        adjustedPeakFrame = adjustedPeakFrame,
                        extractionStartFrame = extractionStartFrame,
                        outerPathFrame = outerPathFrame,
                        framesPerLoop = framesPerLoop,
                        framesSampled = framesPerLoop * 2,
                        loopsSampled = 2,
                        rootPositionDisplacementMax = rootMax,
                        runtimeAdjustedPosePositionDifferenceMax =
                            runtimePositionMax,
                        runtimeAdjustedPoseRotationDifferenceDegreesMax =
                            runtimeRotationMax,
                        unchangedPosePositionDifferenceMax =
                            unchangedPositionMax,
                        unchangedPoseRotationDifferenceDegreesMax =
                            unchangedRotationMax,
                        sourceOuterElbowLateralMeters = sourceOuterElbowLateral,
                        adjustedOuterElbowLateralMeters = adjustedOuterElbowLateral,
                        sourceOuterHandLateralMeters = sourceOuterHandLateral,
                        adjustedOuterHandLateralMeters = adjustedOuterHandLateral,
                        torsoOuterBoundaryLateralMeters = torsoOuterBoundaryLateral,
                        adjustedElbowBeyondTorsoMeters =
                            adjustedOuterElbowLateral - torsoOuterBoundaryLateral,
                        adjustedHandBeyondTorsoMeters =
                            adjustedOuterHandLateral - torsoOuterBoundaryLateral,
                        adjustedElbowBeyondHandMeters =
                            adjustedOuterElbowLateral - adjustedOuterHandLateral,
                        elbowOutwardIncreaseMeters =
                            adjustedOuterElbowLateral - sourceOuterElbowLateral,
                        handOutwardIncreaseMeters =
                            adjustedOuterHandLateral - sourceOuterHandLateral,
                        adjustedPeakElbowFlexDegrees = peakElbowFlex,
                        adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                            peakHeightDifference,
                        adjustedPeakHorizontalForwardAngleDegrees =
                            peakHorizontalForwardAngle,
                        adjustedPeakPalmCharacterLeftAngleDegrees =
                            peakPalmLeftAngle,
                        stateLoops = info.loop,
                        applyRootMotion = animator.applyRootMotion
                    };
                metrics.passedNumericChecks =
                    metrics.sourcePeakFrame == metrics.adjustedPeakFrame &&
                    metrics.outerPathFrame > metrics.extractionStartFrame &&
                    metrics.outerPathFrame < metrics.sourcePeakFrame &&
                    metrics.framesSampled == metrics.framesPerLoop * 2 &&
                    metrics.loopsSampled == 2 &&
                    metrics.rootPositionDisplacementMax <= PositionTolerance &&
                    metrics.runtimeAdjustedPosePositionDifferenceMax <= PositionTolerance &&
                    metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                    metrics.unchangedPosePositionDifferenceMax <= PositionTolerance &&
                    metrics.unchangedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                    metrics.adjustedElbowBeyondTorsoMeters >= 0.01f &&
                    metrics.adjustedHandBeyondTorsoMeters >= 0f &&
                    metrics.adjustedElbowBeyondHandMeters >= 0.015f &&
                    metrics.elbowOutwardIncreaseMeters >= 0.03f &&
                    metrics.elbowOutwardIncreaseMeters >=
                        metrics.handOutwardIncreaseMeters + 0.015f &&
                    Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                    metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                    metrics.adjustedPeakHorizontalForwardAngleDegrees <= 2f &&
                    metrics.adjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                    metrics.stateLoops &&
                    !metrics.applyRootMotion;
                return metrics;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void CaptureHandsDrawBackLowPalmLeftPoseActualReview()
        {
            DrawBackLowPalmLeftApplyMetrics apply =
                ReadJson<DrawBackLowPalmLeftApplyMetrics>(
                    DrawBackLowPalmLeftApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back low palm-left apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back low palm-left adjusted clip is missing.");
            }

            CaptureDrawBackLowPalmLeftComparison(
                target,
                source,
                apply.sourcePeakFrame,
                apply.rightHandBindLocalRotation,
                DrawBackLowPalmLeftReviewPath);
            DrawBackLowPalmLeftReviewMetrics metrics =
                CaptureDrawBackLowPalmLeftMetrics(
                    target,
                    source,
                    adjusted,
                    apply.sourcePeakFrame,
                    apply.rightHandBindLocalRotation);
            metrics.validationPriority =
                "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증";
            WriteJson(DrawBackLowPalmLeftReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back low palm-left Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackLowPalmLeft] Captured source/adjusted direct comparison in Play Mode. " +
                "Frames=" + metrics.framesSampled +
                ", PeakFrame=" + metrics.sourcePeakFrame +
                "/" + metrics.adjustedPeakFrame +
                ", Height=" +
                Num(metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters) +
                ", Elbow=" + Num(metrics.adjustedPeakElbowFlexDegrees) +
                ", HorizontalForward=" +
                Num(metrics.adjustedPeakHorizontalForwardAngleDegrees) +
                ", PalmLeft=" +
                Num(metrics.adjustedPeakPalmCharacterLeftAngleDegrees) +
                ", RuntimePose=" +
                Num(metrics.runtimeAdjustedPosePositionDifferenceMax) +
                "/" +
                Num(metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax) +
                ", Loops=2.");
        }

        private static void CaptureDrawBackLowPalmLeftComparison(
            Transform target,
            AnimationClip source,
            int sourcePeakFrame,
            Quaternion rightHandBindLocalRotation,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            int framesPerLoop = Mathf.Max(
                4,
                Mathf.RoundToInt(source.length * source.frameRate));
            float peakPhase = sourcePeakFrame / (float)framesPerLoop;
            float[] phases = Enumerable.Range(0, 8)
                .Select(index => index / 8f)
                .Concat(new[] { peakPhase })
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            CaptureEnvironment environment = new CaptureEnvironment(target);
            try
            {
                List<List<byte[]>> rows = Enumerable.Range(0, 9)
                    .Select(_ => new List<byte[]>())
                    .ToList();
                foreach (float phase in phases)
                {
                    FindRequired(target, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(target.gameObject, phase * source.length);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    rows[0].Add(environment.CaptureFront());
                    rows[1].Add(environment.CaptureSide());
                    Vector3 sourceArmCenter =
                        (FindRequired(target, RightArmPath).position +
                         FindRequired(target, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(target, sourceArmCenter, 0.62f);
                    rows[4].Add(environment.CaptureFront());
                    rows[5].Add(environment.CaptureSide());

                    SampleAnimator(animator, DrawBackStateName, phase);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());
                    Transform adjustedHand = FindRequired(target, RightHandPath);
                    Vector3 adjustedArmCenter =
                        (FindRequired(target, RightArmPath).position +
                         adjustedHand.position) * 0.5f;
                    environment.ConfigureView(target, adjustedArmCenter, 0.62f);
                    rows[6].Add(environment.CaptureFront());
                    rows[7].Add(environment.CaptureSide());
                    environment.ConfigurePalmView(
                        target,
                        adjustedHand.position,
                        -target.right,
                        0.38f);
                    rows[8].Add(environment.CapturePalmFromTorso());
                }

                ComposeRows(rows, outputPath);
            }
            finally
            {
                environment.Dispose();
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static DrawBackLowPalmLeftReviewMetrics
            CaptureDrawBackLowPalmLeftMetrics(
                Transform target,
                AnimationClip source,
                AnimationClip adjusted,
                int sourcePeakFrame,
                Quaternion rightHandBindLocalRotation)
        {
            Animator animator = RequireAnimator(target);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = target.position;
            float rootMax = 0f;
            float runtimePositionMax = 0f;
            float runtimeRotationMax = 0f;
            float unchangedPositionMax = 0f;
            float unchangedRotationMax = 0f;
            float adjustedPeakProjection = float.NegativeInfinity;
            int adjustedPeakFrame = 0;
            float peakHeightDifference = 0f;
            float peakElbowFlex = 0f;
            float peakHorizontalForwardAngle = 0f;
            float peakPalmLeftAngle = 0f;
            GameObject sourceObject = UnityEngine.Object.Instantiate(target.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(target.gameObject);
            sourceObject.name = target.name + "LowPalmLeftSourceReference";
            adjustedObject.name = target.name + "LowPalmLeftAdjustedReference";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(adjustedObject);
            try
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(source.length * source.frameRate));
                for (int frame = 0; frame < framesPerLoop * 2; frame++)
                {
                    int phaseFrame = frame % framesPerLoop;
                    float time = source.length * phaseFrame / framesPerLoop;
                    FindRequired(sourceObject.transform, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(sourceObject, time);
                    adjusted.SampleAnimation(adjustedObject, time);
                    SampleAnimator(
                        animator,
                        DrawBackStateName,
                        frame / (float)framesPerLoop);
                    PoseSnapshot sourcePose = CapturePose(sourceObject.transform);
                    PoseSnapshot adjustedPose = CapturePose(adjustedObject.transform);
                    PoseSnapshot runtimePose = CapturePose(target);
                    MeasureArmaturePoseDifference(
                        adjustedPose,
                        runtimePose,
                        out float runtimePositionDifference,
                        out float runtimeRotationDifference);
                    MeasurePoseDifferenceExceptDrawBackRightArm(
                        sourcePose,
                        adjustedPose,
                        out float unchangedPositionDifference,
                        out float unchangedRotationDifference);
                    runtimePositionMax = Mathf.Max(
                        runtimePositionMax,
                        runtimePositionDifference);
                    runtimeRotationMax = Mathf.Max(
                        runtimeRotationMax,
                        runtimeRotationDifference);
                    unchangedPositionMax = Mathf.Max(
                        unchangedPositionMax,
                        unchangedPositionDifference);
                    unchangedRotationMax = Mathf.Max(
                        unchangedRotationMax,
                        unchangedRotationDifference);
                    Transform adjustedUpper = FindRequired(
                        adjustedObject.transform,
                        RightArmPath);
                    Transform adjustedLower = FindRequired(
                        adjustedObject.transform,
                        RightForeArmPath);
                    Transform adjustedHand = FindRequired(
                        adjustedObject.transform,
                        RightHandPath);
                    float projection = Vector3.Dot(
                        adjustedHand.position - adjustedUpper.position,
                        adjustedObject.transform.forward);
                    if (projection > adjustedPeakProjection)
                    {
                        adjustedPeakProjection = projection;
                        adjustedPeakFrame = phaseFrame;
                    }

                    if (phaseFrame == sourcePeakFrame)
                    {
                        Transform solarPlexus = FindRequired(
                            adjustedObject.transform,
                            SolarPlexusPath);
                        peakHeightDifference = Mathf.Abs(Vector3.Dot(
                            adjustedHand.position - solarPlexus.position,
                            adjustedObject.transform.up));
                        peakElbowFlex = ElbowFlexDegrees(
                            adjustedUpper,
                            adjustedLower,
                            adjustedHand);
                        Vector3 horizontalDirection = Vector3.ProjectOnPlane(
                            adjustedHand.position - adjustedUpper.position,
                            adjustedObject.transform.up);
                        if (horizontalDirection.sqrMagnitude < 0.0000001f)
                        {
                            throw new InvalidOperationException(
                                "Hands_Draw_Back has no horizontal forward arm direction at peak.");
                        }

                        peakHorizontalForwardAngle = Vector3.Angle(
                            horizontalDirection,
                            adjustedObject.transform.forward);
                        peakPalmLeftAngle = Vector3.Angle(
                            -adjustedHand.right,
                            -adjustedObject.transform.right);
                    }

                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(target.position, rootBaseline));
                }

                SampleAnimator(animator, DrawBackStateName, 0f);
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                DrawBackLowPalmLeftReviewMetrics metrics =
                    new DrawBackLowPalmLeftReviewMetrics
                    {
                        target = target.name,
                        sourcePeakFrame = sourcePeakFrame,
                        adjustedPeakFrame = adjustedPeakFrame,
                        framesPerLoop = framesPerLoop,
                        framesSampled = framesPerLoop * 2,
                        loopsSampled = 2,
                        rootPositionDisplacementMax = rootMax,
                        runtimeAdjustedPosePositionDifferenceMax =
                            runtimePositionMax,
                        runtimeAdjustedPoseRotationDifferenceDegreesMax =
                            runtimeRotationMax,
                        unchangedPosePositionDifferenceMax =
                            unchangedPositionMax,
                        unchangedPoseRotationDifferenceDegreesMax =
                            unchangedRotationMax,
                        expectedElbowFlexDegrees = 30f,
                        adjustedPeakElbowFlexDegrees = peakElbowFlex,
                        adjustedPeakHandSolarPlexusHeightDifferenceMeters =
                            peakHeightDifference,
                        adjustedPeakHorizontalForwardAngleDegrees =
                            peakHorizontalForwardAngle,
                        adjustedPeakPalmCharacterLeftAngleDegrees =
                            peakPalmLeftAngle,
                        stateLoops = info.loop,
                        applyRootMotion = animator.applyRootMotion
                    };
                metrics.passedNumericChecks =
                    metrics.sourcePeakFrame == metrics.adjustedPeakFrame &&
                    metrics.framesSampled == metrics.framesPerLoop * 2 &&
                    metrics.loopsSampled == 2 &&
                    metrics.rootPositionDisplacementMax <= PositionTolerance &&
                    metrics.runtimeAdjustedPosePositionDifferenceMax <= PositionTolerance &&
                    metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax <=
                        RotationTolerance &&
                    metrics.unchangedPosePositionDifferenceMax <= PositionTolerance &&
                    metrics.unchangedPoseRotationDifferenceDegreesMax <=
                        RotationTolerance &&
                    Mathf.Abs(metrics.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                    metrics.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                    metrics.adjustedPeakHorizontalForwardAngleDegrees <= 2f &&
                    metrics.adjustedPeakPalmCharacterLeftAngleDegrees <= 8f &&
                    metrics.stateLoops &&
                    !metrics.applyRootMotion;
                return metrics;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void CaptureHandsDrawBackForwardAngleActualReview()
        {
            DrawBackForwardApplyMetrics apply =
                ReadJson<DrawBackForwardApplyMetrics>(
                    DrawBackForwardApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back forward-angle apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back forward-angle adjusted clip is missing.");
            }

            CaptureDrawBackForwardAngleComparison(
                target,
                source,
                adjusted,
                apply.sourcePeakFrame,
                apply.rightHandBindLocalRotation,
                DrawBackForwardReviewPath);
            DrawBackForwardReviewMetrics metrics =
                CaptureDrawBackForwardAngleMetrics(
                    target,
                    source,
                    adjusted,
                    apply.sourcePeakFrame,
                    apply.rightHandBindLocalRotation);
            metrics.validationPriority =
                "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증";
            WriteJson(DrawBackForwardReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back forward-angle Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackForward] Captured source/adjusted direct comparison in Play Mode. " +
                "Frames=" + metrics.framesSampled +
                ", ForwardAngle=" +
                Num(metrics.sourcePeakShoulderToHandForwardAngleDegrees) +
                "->" +
                Num(metrics.adjustedPeakShoulderToHandForwardAngleDegrees) +
                ", ElbowFlex=" + Num(metrics.sourcePeakElbowFlexDegrees) +
                "->" + Num(metrics.adjustedPeakElbowFlexDegrees) +
                ", HandRotation=" +
                Num(metrics.rightHandWorldRotationDifferenceDegreesMax) +
                ", RuntimePose=" +
                Num(metrics.runtimeAdjustedPosePositionDifferenceMax) +
                "/" +
                Num(metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax) +
                ", Loops=2.");
        }

        private static void CaptureDrawBackForwardAngleComparison(
            Transform target,
            AnimationClip source,
            AnimationClip adjusted,
            int sourcePeakFrame,
            Quaternion rightHandBindLocalRotation,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            int framesPerLoop = Mathf.Max(
                4,
                Mathf.RoundToInt(source.length * source.frameRate));
            float peakPhase = sourcePeakFrame / (float)framesPerLoop;
            float[] phases = Enumerable.Range(0, 8)
                .Select(index => index / 8f)
                .Concat(new[] { peakPhase })
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            CaptureEnvironment environment = new CaptureEnvironment(target);
            try
            {
                List<List<byte[]>> rows = Enumerable.Range(0, 8)
                    .Select(_ => new List<byte[]>())
                    .ToList();
                foreach (float phase in phases)
                {
                    FindRequired(target, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(target.gameObject, phase * source.length);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    rows[0].Add(environment.CaptureFront());
                    rows[1].Add(environment.CaptureSide());
                    Vector3 sourceArmCenter =
                        (FindRequired(target, RightArmPath).position +
                         FindRequired(target, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(target, sourceArmCenter, 0.62f);
                    rows[4].Add(environment.CaptureFront());
                    rows[5].Add(environment.CaptureSide());

                    SampleAnimator(animator, DrawBackStateName, phase);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());
                    Vector3 adjustedArmCenter =
                        (FindRequired(target, RightArmPath).position +
                         FindRequired(target, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(target, adjustedArmCenter, 0.62f);
                    rows[6].Add(environment.CaptureFront());
                    rows[7].Add(environment.CaptureSide());
                }

                ComposeRows(rows, outputPath);
            }
            finally
            {
                environment.Dispose();
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static DrawBackForwardReviewMetrics
            CaptureDrawBackForwardAngleMetrics(
                Transform target,
                AnimationClip source,
                AnimationClip adjusted,
                int sourcePeakFrame,
                Quaternion rightHandBindLocalRotation)
        {
            Animator animator = RequireAnimator(target);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = target.position;
            float rootMax = 0f;
            float runtimePositionMax = 0f;
            float runtimeRotationMax = 0f;
            float unchangedPositionMax = 0f;
            float unchangedRotationMax = 0f;
            float handWorldRotationMax = 0f;
            float sourcePeakAngle = 0f;
            float adjustedPeakAngle = 0f;
            float sourcePeakElbow = 0f;
            float adjustedPeakElbow = 0f;
            GameObject sourceObject = UnityEngine.Object.Instantiate(target.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(target.gameObject);
            sourceObject.name = target.name + "ForwardAngleSourceReference";
            adjustedObject.name = target.name + "ForwardAngleAdjustedReference";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(adjustedObject);
            try
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(source.length * source.frameRate));
                for (int frame = 0; frame < framesPerLoop * 2; frame++)
                {
                    int phaseFrame = frame % framesPerLoop;
                    float time = source.length * phaseFrame / framesPerLoop;
                    FindRequired(sourceObject.transform, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(sourceObject, time);
                    adjusted.SampleAnimation(adjustedObject, time);
                    SampleAnimator(
                        animator,
                        DrawBackStateName,
                        frame / (float)framesPerLoop);
                    PoseSnapshot sourcePose = CapturePose(sourceObject.transform);
                    PoseSnapshot adjustedPose = CapturePose(adjustedObject.transform);
                    PoseSnapshot runtimePose = CapturePose(target);
                    MeasureArmaturePoseDifference(
                        adjustedPose,
                        runtimePose,
                        out float runtimePositionDifference,
                        out float runtimeRotationDifference);
                    MeasurePoseDifferenceExceptDrawBackRightArm(
                        sourcePose,
                        adjustedPose,
                        out float unchangedPositionDifference,
                        out float unchangedRotationDifference);
                    runtimePositionMax = Mathf.Max(
                        runtimePositionMax,
                        runtimePositionDifference);
                    runtimeRotationMax = Mathf.Max(
                        runtimeRotationMax,
                        runtimeRotationDifference);
                    unchangedPositionMax = Mathf.Max(
                        unchangedPositionMax,
                        unchangedPositionDifference);
                    unchangedRotationMax = Mathf.Max(
                        unchangedRotationMax,
                        unchangedRotationDifference);
                    Transform sourceHand = FindRequired(
                        sourceObject.transform,
                        RightHandPath);
                    Transform adjustedHand = FindRequired(
                        adjustedObject.transform,
                        RightHandPath);
                    handWorldRotationMax = Mathf.Max(
                        handWorldRotationMax,
                        Quaternion.Angle(
                            sourceHand.rotation,
                            adjustedHand.rotation));
                    if (phaseFrame == sourcePeakFrame)
                    {
                        Transform sourceUpper = FindRequired(
                            sourceObject.transform,
                            RightArmPath);
                        Transform sourceLower = FindRequired(
                            sourceObject.transform,
                            RightForeArmPath);
                        Transform adjustedUpper = FindRequired(
                            adjustedObject.transform,
                            RightArmPath);
                        Transform adjustedLower = FindRequired(
                            adjustedObject.transform,
                            RightForeArmPath);
                        sourcePeakAngle = Vector3.Angle(
                            sourceHand.position - sourceUpper.position,
                            sourceObject.transform.forward);
                        adjustedPeakAngle = Vector3.Angle(
                            adjustedHand.position - adjustedUpper.position,
                            adjustedObject.transform.forward);
                        sourcePeakElbow = ElbowFlexDegrees(
                            sourceUpper,
                            sourceLower,
                            sourceHand);
                        adjustedPeakElbow = ElbowFlexDegrees(
                            adjustedUpper,
                            adjustedLower,
                            adjustedHand);
                    }

                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(target.position, rootBaseline));
                }

                SampleAnimator(animator, DrawBackStateName, 0f);
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                DrawBackForwardReviewMetrics metrics =
                    new DrawBackForwardReviewMetrics
                    {
                        target = target.name,
                        sourcePeakFrame = sourcePeakFrame,
                        framesPerLoop = framesPerLoop,
                        framesSampled = framesPerLoop * 2,
                        loopsSampled = 2,
                        rootPositionDisplacementMax = rootMax,
                        runtimeAdjustedPosePositionDifferenceMax =
                            runtimePositionMax,
                        runtimeAdjustedPoseRotationDifferenceDegreesMax =
                            runtimeRotationMax,
                        unchangedPosePositionDifferenceMax =
                            unchangedPositionMax,
                        unchangedPoseRotationDifferenceDegreesMax =
                            unchangedRotationMax,
                        sourcePeakShoulderToHandForwardAngleDegrees =
                            sourcePeakAngle,
                        adjustedPeakShoulderToHandForwardAngleDegrees =
                            adjustedPeakAngle,
                        sourcePeakElbowFlexDegrees = sourcePeakElbow,
                        adjustedPeakElbowFlexDegrees = adjustedPeakElbow,
                        rightHandWorldRotationDifferenceDegreesMax =
                            handWorldRotationMax,
                        stateLoops = info.loop,
                        applyRootMotion = animator.applyRootMotion
                    };
                metrics.passedNumericChecks =
                    metrics.framesSampled == metrics.framesPerLoop * 2 &&
                    metrics.loopsSampled == 2 &&
                    metrics.rootPositionDisplacementMax <= PositionTolerance &&
                    metrics.runtimeAdjustedPosePositionDifferenceMax <= PositionTolerance &&
                    metrics.runtimeAdjustedPoseRotationDifferenceDegreesMax <=
                        RotationTolerance &&
                    metrics.unchangedPosePositionDifferenceMax <= PositionTolerance &&
                    metrics.unchangedPoseRotationDifferenceDegreesMax <=
                        RotationTolerance &&
                    metrics.adjustedPeakShoulderToHandForwardAngleDegrees <= 0.25f &&
                    metrics.sourcePeakShoulderToHandForwardAngleDegrees -
                        metrics.adjustedPeakShoulderToHandForwardAngleDegrees >= 0.5f &&
                    metrics.adjustedPeakElbowFlexDegrees >= 5f &&
                    Mathf.Abs(
                        metrics.sourcePeakElbowFlexDegrees -
                        metrics.adjustedPeakElbowFlexDegrees) <= 0.1f &&
                    metrics.rightHandWorldRotationDifferenceDegreesMax <=
                        RotationTolerance &&
                    metrics.stateLoops &&
                    !metrics.applyRootMotion;
                return metrics;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void MeasurePoseDifferenceExceptDrawBackRightArm(
            PoseSnapshot first,
            PoseSnapshot second,
            out float positionMax,
            out float rotationMax)
        {
            positionMax = 0f;
            rotationMax = 0f;
            string[] paths = first.Positions.Keys
                .Where(path =>
                    (string.Equals(path, "Armature", StringComparison.Ordinal) ||
                     path.StartsWith("Armature/", StringComparison.Ordinal)) &&
                    !string.Equals(path, RightArmPath, StringComparison.Ordinal) &&
                    !path.StartsWith(RightArmPath + "/", StringComparison.Ordinal))
                .ToArray();
            foreach (string path in paths)
            {
                if (!second.Positions.TryGetValue(path, out Vector3 secondPosition) ||
                    !first.Rotations.TryGetValue(path, out Quaternion firstRotation) ||
                    !second.Rotations.TryGetValue(path, out Quaternion secondRotation))
                {
                    throw new InvalidOperationException(
                        "Hands Draw Back hierarchy changed during forward-angle review at " +
                        path + ".");
                }

                positionMax = Mathf.Max(
                    positionMax,
                    Vector3.Distance(first.Positions[path], secondPosition));
                rotationMax = Mathf.Max(
                    rotationMax,
                    Quaternion.Angle(firstRotation, secondRotation));
            }
        }

        private static void CaptureHandsDrawAndStowBackExactActualReview()
        {
            HandsBackApplyMetrics apply =
                ReadJson<HandsBackApplyMetrics>(HandsBackApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw/Stow Back exact Take apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform drawTarget = RequireTarget(layout, DrawBackTargetName);
            Transform stowTarget = RequireTarget(layout, StowBackTargetName);
            AnimationClip drawClip = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip stowClip = LoadSingleEmbeddedClip(
                StowBackSourcePath,
                "hands stow back");

            CaptureTargetComparison(
                drawTarget,
                drawClip,
                DrawBackStateName,
                DrawBackReviewPath);
            CaptureTargetComparison(
                stowTarget,
                stowClip,
                StowBackStateName,
                StowBackReviewPath);
            TargetReviewMetrics drawBack = CaptureTargetMetrics(
                drawTarget,
                drawClip,
                DrawBackStateName,
                drawClip.name);
            TargetReviewMetrics stowBack = CaptureTargetMetrics(
                stowTarget,
                stowClip,
                StowBackStateName,
                stowClip.name);
            drawBack.passedNumericChecks = TargetReviewPassed(drawBack);
            stowBack.passedNumericChecks = TargetReviewPassed(stowBack);
            HandsBackReviewMetrics metrics = new HandsBackReviewMetrics
            {
                targetSet = DrawBackTargetName + ", " + StowBackTargetName,
                drawBack = drawBack,
                stowBack = stowBack,
                passedNumericChecks =
                    drawBack.passedNumericChecks &&
                    stowBack.passedNumericChecks,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(HandsBackReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw/Stow Back Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsBack] Captured exact embedded Take comparisons in Play Mode. " +
                "DrawFrames=" + drawBack.framesSampled +
                ", DrawPose=" + Num(drawBack.sourcePosePositionDifferenceMax) +
                "/" + Num(drawBack.sourcePoseRotationDifferenceDegreesMax) +
                ", DrawRoot=" + Num(drawBack.rootPositionDisplacementMax) +
                ", StowFrames=" + stowBack.framesSampled +
                ", StowPose=" + Num(stowBack.sourcePosePositionDifferenceMax) +
                "/" + Num(stowBack.sourcePoseRotationDifferenceDegreesMax) +
                ", StowRoot=" + Num(stowBack.rootPositionDisplacementMax) +
                ", LoopsPerTarget=2.");
        }

        private static void CapturePlayerHandsDrawBackCommonMeshActualReview()
        {
            DrawBackCommonMeshApplyMetrics apply =
                ReadJson<DrawBackCommonMeshApplyMetrics>(
                    DrawBackCommonMeshApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Transform emptyReference = RequireTarget(layout, EmptyTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            CaptureDrawBackCommonMeshComparison(
                target,
                emptyReference,
                source,
                apply.correctedBlendShapeWeightsBefore,
                DrawBackCommonMeshReviewPath);
            TargetReviewMetrics drawBack = CaptureTargetMetrics(
                target,
                source,
                DrawBackStateName,
                source.name);
            drawBack.passedNumericChecks = TargetReviewPassed(drawBack);
            SkinnedMeshRenderer renderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(target);
            SkinnedMeshRenderer emptyRenderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(emptyReference);
            DrawBackCommonMeshReviewMetrics metrics =
                new DrawBackCommonMeshReviewMetrics
                {
                    target = DrawBackTargetName,
                    emptyReference = EmptyTargetName,
                    phasesCaptured = 12,
                    drawBack = drawBack,
                    rendererConfigurationMatchesEmpty =
                        RendererConfigurationMatches(
                            renderer,
                            target,
                            emptyRenderer,
                            emptyReference),
                    correctedMeshUnreferencedByScene =
                        !SceneDependsOnAsset(
                            DrawBackRightChestCorrectedMeshPath),
                    correctedMeshAssetUnchanged = string.Equals(
                        HashFile(DrawBackRightChestCorrectedMeshPath),
                        apply.correctedMeshHashAfter,
                        StringComparison.Ordinal),
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.phasesCaptured == 12 &&
                metrics.drawBack.passedNumericChecks &&
                metrics.rendererConfigurationMatchesEmpty &&
                metrics.correctedMeshUnreferencedByScene &&
                metrics.correctedMeshAssetUnchanged;
            WriteJson(DrawBackCommonMeshReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackCommonMesh] Captured corrected-before, shared-mesh Draw Back, and Hands_Empty_Idle in the same 12 normalized phases. " +
                "Frames=" + drawBack.framesSampled +
                ", Root=" + Num(drawBack.rootPositionDisplacementMax) +
                ", CommonRenderer=True, CorrectedMeshReferenced=False.");
        }

        private static void CapturePlayerHandsDrawBackCommonMeshForwardActualReview()
        {
            DrawBackCommonMeshForwardApplyMetrics apply =
                ReadJson<DrawBackCommonMeshForwardApplyMetrics>(
                    DrawBackCommonMeshForwardApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh forward apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            Transform emptyReference = RequireTarget(layout, EmptyTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            AnimationClip adjusted = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                DrawBackForwardAdjustedClipPath);
            if (adjusted == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh forward adjusted clip is missing.");
            }

            CaptureDrawBackCommonMeshForwardComparison(
                target,
                source,
                DrawBackCommonMeshForwardReviewPath);
            DrawBackOuterElbowReviewMetrics motion =
                CaptureDrawBackOuterElbowMetrics(
                    target,
                    source,
                    adjusted,
                    apply.extractionStartFrame,
                    apply.outerPathFrame,
                    apply.sourcePeakFrame,
                    apply.rightHandBindLocalRotation);
            MeasureDrawBackClipFrontSilhouetteGap(
                target,
                adjusted,
                out float minimumFrontSilhouetteGap,
                out int minimumFrontSilhouetteGapFrame);
            motion.validationPriority =
                "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증";
            motion.passedNumericChecks =
                motion.framesPerLoop == 69 &&
                motion.framesSampled == 138 &&
                motion.loopsSampled == 2 &&
                motion.rootPositionDisplacementMax <= PositionTolerance &&
                motion.runtimeAdjustedPosePositionDifferenceMax <= PositionTolerance &&
                motion.runtimeAdjustedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                motion.unchangedPosePositionDifferenceMax <= PositionTolerance &&
                motion.unchangedPoseRotationDifferenceDegreesMax <= RotationTolerance &&
                motion.adjustedPeakHorizontalForwardAngleDegrees >= 5f &&
                motion.adjustedPeakHorizontalForwardAngleDegrees <= 45f &&
                motion.adjustedPeakHandSolarPlexusHeightDifferenceMeters <= 0.005f &&
                Mathf.Abs(motion.adjustedPeakElbowFlexDegrees - 30f) <= 0.5f &&
                motion.stateLoops &&
                !motion.applyRootMotion;
            SkinnedMeshRenderer renderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(target);
            SkinnedMeshRenderer emptyRenderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(emptyReference);
            DrawBackCommonMeshForwardReviewMetrics metrics =
                new DrawBackCommonMeshForwardReviewMetrics
                {
                    target = DrawBackTargetName,
                    phasesCaptured = 12,
                    motion = motion,
                    rendererConfigurationMatchesEmpty =
                        RendererConfigurationMatches(
                            renderer,
                            target,
                            emptyRenderer,
                            emptyReference),
                    correctedMeshUnreferencedByScene =
                        !SceneDependsOnAsset(
                            DrawBackRightChestCorrectedMeshPath),
                    correctedMeshAssetUnchanged = string.Equals(
                        HashFile(DrawBackRightChestCorrectedMeshPath),
                        apply.correctedMeshHashAfter,
                        StringComparison.Ordinal),
                    hasNoBlendShapeCurves =
                        HasNoBlendShapeCurves(adjusted),
                    minimumFrontSilhouetteGapMeters =
                        minimumFrontSilhouetteGap,
                    minimumFrontSilhouetteGapFrame =
                        minimumFrontSilhouetteGapFrame,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            metrics.passedNumericChecks =
                metrics.phasesCaptured == 12 &&
                metrics.motion.passedNumericChecks &&
                metrics.rendererConfigurationMatchesEmpty &&
                metrics.correctedMeshUnreferencedByScene &&
                metrics.correctedMeshAssetUnchanged &&
                metrics.hasNoBlendShapeCurves &&
                metrics.minimumFrontSilhouetteGapMeters >= 0.005f;
            WriteJson(DrawBackCommonMeshForwardReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back common-mesh forward Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackCommonMeshForward] Captured the exact source and adjusted common-mesh result in 12 Play Mode phases. " +
                "Frames=" + motion.framesSampled +
                ", Forward=" +
                Num(motion.adjustedPeakHorizontalForwardAngleDegrees) +
                ", Height=" +
                Num(motion.adjustedPeakHandSolarPlexusHeightDifferenceMeters) +
                ", Elbow=" + Num(motion.adjustedPeakElbowFlexDegrees) +
                ", MinFaceGap=" +
                Num(metrics.minimumFrontSilhouetteGapMeters) + "@" +
                metrics.minimumFrontSilhouetteGapFrame +
                ", Root=" + Num(motion.rootPositionDisplacementMax) + ".");
        }

        private static void CaptureDrawBackCommonMeshComparison(
            Transform target,
            Transform emptyReference,
            AnimationClip source,
            float[] correctedBlendShapeWeightsBefore,
            string outputPath)
        {
            GameObject correctedBeforeObject =
                UnityEngine.Object.Instantiate(target.gameObject);
            correctedBeforeObject.name =
                target.name + "CorrectedMeshBeforeReference";
            correctedBeforeObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(correctedBeforeObject);
            SkinnedMeshRenderer correctedBeforeRenderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(
                    correctedBeforeObject.transform);
            Mesh correctedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                DrawBackRightChestCorrectedMeshPath);
            if (correctedMesh == null)
            {
                throw new InvalidOperationException(
                    "Hands Draw Back state-only corrected mesh is missing for the before reference.");
            }

            correctedBeforeRenderer.sharedMesh = correctedMesh;
            if (correctedBlendShapeWeightsBefore.Length !=
                correctedMesh.blendShapeCount)
            {
                UnityEngine.Object.DestroyImmediate(correctedBeforeObject);
                throw new InvalidOperationException(
                    "Hands Draw Back recorded before-state BlendShape count changed.");
            }

            for (int index = 0;
                 index < correctedBlendShapeWeightsBefore.Length;
                 index++)
            {
                correctedBeforeRenderer.SetBlendShapeWeight(
                    index,
                    correctedBlendShapeWeightsBefore[index]);
            }

            Animator targetAnimator = RequireAnimator(target);
            Animator emptyAnimator = RequireAnimator(emptyReference);
            List<List<byte[]>> rows = Enumerable.Range(0, 9)
                .Select(_ => new List<byte[]>())
                .ToList();
            try
            {
                CaptureDrawBackThreeViewRows(
                    correctedBeforeObject.transform,
                    phase => source.SampleAnimation(
                        correctedBeforeObject,
                        phase * source.length),
                    rows,
                    0);
                CaptureDrawBackThreeViewRows(
                    target,
                    phase => SampleAnimator(
                        targetAnimator,
                        DrawBackStateName,
                        phase),
                    rows,
                    3);
                CaptureDrawBackThreeViewRows(
                    emptyReference,
                    phase => SampleAnimator(
                        emptyAnimator,
                        EmptyStateName,
                        phase),
                    rows,
                    6);
                ComposeRows(rows, outputPath);
            }
            finally
            {
                targetAnimator.Rebind();
                targetAnimator.Update(0f);
                emptyAnimator.Rebind();
                emptyAnimator.Update(0f);
                UnityEngine.Object.DestroyImmediate(correctedBeforeObject);
            }
        }

        private static void CaptureDrawBackThreeViewRows(
            Transform subject,
            Action<float> sample,
            IReadOnlyList<List<byte[]>> rows,
            int rowOffset)
        {
            CaptureEnvironment environment = new CaptureEnvironment(subject);
            try
            {
                for (int phaseIndex = 0; phaseIndex < 12; phaseIndex++)
                {
                    float phase = phaseIndex / 12f;
                    sample(phase);
                    environment.ConfigureView(subject, 1.05f, 1.35f);
                    rows[rowOffset].Add(environment.CaptureFront());
                    rows[rowOffset + 1].Add(environment.CaptureSide());
                    Vector3 chestCenter =
                        (FindRequired(subject, SolarPlexusPath).position +
                         FindRequired(subject, RightShoulderPath).position) * 0.5f;
                    environment.ConfigureView(subject, chestCenter, 0.48f);
                    rows[rowOffset + 2].Add(environment.CaptureFront());
                }
            }
            finally
            {
                environment.Dispose();
            }
        }

        private static void CaptureDrawBackCommonMeshForwardComparison(
            Transform target,
            AnimationClip source,
            string outputPath)
        {
            GameObject sourceObject = UnityEngine.Object.Instantiate(
                target.gameObject);
            sourceObject.name = target.name + "ExactSourceReference";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            Animator animator = RequireAnimator(target);
            List<List<byte[]>> rows = Enumerable.Range(0, 10)
                .Select(_ => new List<byte[]>())
                .ToList();
            try
            {
                CaptureDrawBackFiveViewRows(
                    sourceObject.transform,
                    phase => source.SampleAnimation(
                        sourceObject,
                        phase * source.length),
                    rows,
                    0);
                CaptureDrawBackFiveViewRows(
                    target,
                    phase => SampleAnimator(
                        animator,
                        DrawBackStateName,
                        phase),
                    rows,
                    5);
                ComposeRows(rows, outputPath);
            }
            finally
            {
                animator.Rebind();
                animator.Update(0f);
                UnityEngine.Object.DestroyImmediate(sourceObject);
            }
        }

        private static void CaptureDrawBackFiveViewRows(
            Transform subject,
            Action<float> sample,
            IReadOnlyList<List<byte[]>> rows,
            int rowOffset)
        {
            CaptureEnvironment environment = new CaptureEnvironment(subject);
            try
            {
                for (int phaseIndex = 0; phaseIndex < 12; phaseIndex++)
                {
                    float phase = phaseIndex / 12f;
                    sample(phase);
                    environment.ConfigureView(subject, 1.05f, 1.35f);
                    rows[rowOffset].Add(environment.CaptureFront());
                    rows[rowOffset + 1].Add(environment.CaptureSide());
                    Vector3 armCenter =
                        (FindRequired(subject, RightArmPath).position +
                         FindRequired(subject, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(subject, armCenter, 0.62f);
                    rows[rowOffset + 2].Add(environment.CaptureFront());
                    rows[rowOffset + 3].Add(environment.CaptureSide());
                    Vector3 chestCenter =
                        (FindRequired(subject, SolarPlexusPath).position +
                         FindRequired(subject, RightShoulderPath).position) * 0.5f;
                    environment.ConfigureView(subject, chestCenter, 0.48f);
                    rows[rowOffset + 4].Add(environment.CaptureFront());
                }
            }
            finally
            {
                environment.Dispose();
            }
        }

        private static void CapturePlayerHandsDrawBackExactMixamoActualReview()
        {
            DrawBackExactReconnectApplyMetrics apply =
                ReadJson<DrawBackExactReconnectApplyMetrics>(
                    DrawBackExactReconnectApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Exact Hands Draw Back reconnect apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, DrawBackTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                DrawBackSourcePath,
                "hands draw back");
            CaptureTargetComparison(
                target,
                source,
                DrawBackStateName,
                DrawBackExactReconnectReviewPath);
            TargetReviewMetrics drawBack = CaptureTargetMetrics(
                target,
                source,
                DrawBackStateName,
                source.name);
            drawBack.passedNumericChecks = TargetReviewPassed(drawBack);
            DrawBackExactReconnectReviewMetrics metrics =
                new DrawBackExactReconnectReviewMetrics
                {
                    targetSet = DrawBackTargetName,
                    drawBack = drawBack,
                    passedNumericChecks = drawBack.passedNumericChecks,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };
            WriteJson(DrawBackExactReconnectReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Exact Hands Draw Back Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsDrawBackExactReconnect] Captured the exact embedded Take as-is in Play Mode. " +
                "Frames=" + drawBack.framesSampled +
                ", Pose=" + Num(drawBack.sourcePosePositionDifferenceMax) +
                "/" + Num(drawBack.sourcePoseRotationDifferenceDegreesMax) +
                ", Root=" + Num(drawBack.rootPositionDisplacementMax) +
                ", Loops=2.");
        }

        private static void CaptureActualReview()
        {
            ApplyMetrics apply = ReadJson<ApplyMetrics>(ApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands and Objects apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform emptyTarget = RequireTarget(layout, EmptyTargetName);
            Transform oneHandTarget = RequireTarget(layout, OneHandTargetName);
            Transform twoHandTarget = RequireTarget(layout, TwoHandTargetName);
            AnimationClip idleSource = LoadClip(IdleClipPath);
            AnimationClip oneHandClip = LoadSingleEmbeddedClip(
                OneHandSourcePath,
                "one-hand carry");
            AnimationClip twoHandClip = LoadSingleEmbeddedClip(
                TwoHandSourcePath,
                "two-hand carry");

            CaptureTargetComparison(
                emptyTarget,
                idleSource,
                EmptyStateName,
                EmptyReviewPath);
            CaptureTargetComparison(
                oneHandTarget,
                oneHandClip,
                OneHandStateName,
                OneHandReviewPath);
            CaptureTargetComparison(
                twoHandTarget,
                twoHandClip,
                TwoHandStateName,
                TwoHandReviewPath);
            TargetReviewMetrics empty = CaptureTargetMetrics(
                emptyTarget,
                idleSource,
                EmptyStateName,
                "Player_Idle copied asset");
            TargetReviewMetrics oneHand = CaptureTargetMetrics(
                oneHandTarget,
                oneHandClip,
                OneHandStateName,
                oneHandClip.name);
            TargetReviewMetrics twoHand = CaptureTargetMetrics(
                twoHandTarget,
                twoHandClip,
                TwoHandStateName,
                twoHandClip.name);
            empty.passedNumericChecks = TargetReviewPassed(empty);
            oneHand.passedNumericChecks = TargetReviewPassed(oneHand);
            twoHand.passedNumericChecks = TargetReviewPassed(twoHand);
            ReviewMetrics metrics = new ReviewMetrics
            {
                targetSet = EmptyTargetName + ", " + OneHandTargetName + ", " + TwoHandTargetName,
                emptyIdle = empty,
                oneHand = oneHand,
                twoHand = twoHand,
                passedNumericChecks = empty.passedNumericChecks &&
                    oneHand.passedNumericChecks &&
                    twoHand.passedNumericChecks,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            WriteJson(ReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands and Objects Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsObjects] Captured actual Play Mode comparisons. " +
                "EmptyFrames=" + empty.framesSampled +
                ", EmptyPose=" + Num(empty.sourcePosePositionDifferenceMax) +
                "/" + Num(empty.sourcePoseRotationDifferenceDegreesMax) +
                ", OneHandFrames=" + oneHand.framesSampled +
                ", OneHandPose=" + Num(oneHand.sourcePosePositionDifferenceMax) +
                "/" + Num(oneHand.sourcePoseRotationDifferenceDegreesMax) +
                ", TwoHandFrames=" + twoHand.framesSampled +
                ", TwoHandPose=" + Num(twoHand.sourcePosePositionDifferenceMax) +
                "/" + Num(twoHand.sourcePoseRotationDifferenceDegreesMax) +
                ", LoopsPerTarget=2.");
        }

        private static bool TargetReviewPassed(TargetReviewMetrics metrics)
        {
            return metrics.framesSampled == metrics.framesPerLoop * 2 &&
                   metrics.loopsSampled == 2 &&
                   metrics.rootPositionDisplacementMax <= PositionTolerance &&
                   metrics.sourcePosePositionDifferenceMax <= PositionTolerance &&
                   metrics.sourcePoseRotationDifferenceDegreesMax <= RotationTolerance &&
                   metrics.stateLoops &&
                   !metrics.applyRootMotion;
        }

        private static TargetApplyMetrics CreateTargetApplyMetrics(
            string target,
            string state,
            string sourceTake,
            string clipPath,
            AnimationClip clip,
            AnimatorController controller,
            Animator animator)
        {
            return new TargetApplyMetrics
            {
                target = target,
                state = state,
                sourceTake = sourceTake,
                clipPath = clipPath,
                durationSeconds = clip.length,
                frameRate = clip.frameRate,
                floatCurveCount = AnimationUtility.GetCurveBindings(clip).Length,
                objectCurveCount =
                    AnimationUtility.GetObjectReferenceCurveBindings(clip).Length,
                eventCount = AnimationUtility.GetAnimationEvents(clip).Length,
                stateUsesExactClip = StateUsesClip(controller, state, clip),
                loopTime = AnimationUtility.GetAnimationClipSettings(clip).loopTime,
                applyRootMotion = animator.applyRootMotion
            };
        }

        private static void EnsureExactSourceCopy(
            string originalPath,
            string assetPath,
            string expectedHash,
            string label)
        {
            RequireHash(originalPath, expectedHash, label + " original FBX");
            string originalAbsolute = Path.GetFullPath(originalPath);
            string assetAbsolute = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(assetAbsolute) ??
                throw new InvalidOperationException(label + " asset directory is unavailable."));
            if (!File.Exists(assetAbsolute) ||
                !string.Equals(
                    HashFile(assetPath),
                    expectedHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(originalAbsolute, assetAbsolute, true);
            }

            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            RequireHash(assetPath, expectedHash, label + " Unity FBX");
        }

        private static void ConfigureSourceImporter(string path, string label)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    label + " FBX ModelImporter is unavailable.");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    label + " FBX ModelImporter disappeared after reimport.");
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    label + " FBX must expose exactly one embedded Take; actual=" +
                    clips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadSingleEmbeddedClip(string path, string label)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    label + " FBX must expose exactly one non-preview AnimationClip; actual=" +
                    clips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return clips[0];
        }

        private static AnimationClip CreateOrUpdateIdleCopy(AnimationClip source)
        {
            AnimationClip generated = new AnimationClip();
            EditorUtility.CopySerialized(source, generated);
            generated.name = "Hands_Empty_Idle";
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(generated);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(generated, settings);
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(EmptyClipPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, EmptyClipPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                existing.name = "Hands_Empty_Idle";
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static AnimatorController CreateOrUpdateExactEmbeddedTakeController(
            string path,
            string stateName,
            AnimationClip clip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            while (controller.layers.Length > 1)
            {
                controller.RemoveLayer(controller.layers.Length - 1);
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    Path.GetFileName(path) + " must contain exactly one layer.");
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (BlendTree tree in AssetDatabase.LoadAllAssetsAtPath(path)
                         .OfType<BlendTree>()
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(tree, true);
            }

            AnimatorControllerLayer[] layers = controller.layers;
            ClearStateMachine(layers[0].stateMachine);
            AnimatorState state = layers[0].stateMachine.AddState(stateName);
            state.motion = clip;
            state.speed = 1f;
            state.mirror = false;
            state.cycleOffset = 0f;
            state.writeDefaultValues = false;
            layers[0].name = "Base Layer";
            layers[0].avatarMask = null;
            layers[0].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[0].defaultWeight = 1f;
            layers[0].stateMachine.defaultState = state;
            controller.layers = layers;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(layers[0].stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController CreateOrUpdateController(
            string path,
            string stateName,
            AnimationClip clip)
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
            foreach (BlendTree tree in AssetDatabase.LoadAllAssetsAtPath(path)
                         .OfType<BlendTree>()
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(tree, true);
            }

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

        private static AvatarMask CreateOrUpdateArmsMask(Transform template)
        {
            AvatarMask generated = new AvatarMask
            {
                name = "Hands_Carry_Arms"
            };
            for (int part = 0; part < (int)AvatarMaskBodyPart.LastBodyPart; part++)
            {
                generated.SetHumanoidBodyPartActive((AvatarMaskBodyPart)part, false);
            }

            Transform[] armatures = template.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(item.name, "Armature", StringComparison.Ordinal))
                .ToArray();
            if (armatures.Length != 1)
            {
                throw new InvalidOperationException(
                    template.name + " must contain exactly one Armature root; actual=" +
                    armatures.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            GameObject maskRoot = new GameObject("MaskRoot");
            GameObject detachedArmature = UnityEngine.Object.Instantiate(
                armatures[0].gameObject);
            maskRoot.hideFlags = HideFlags.HideAndDontSave;
            detachedArmature.name = "Armature";
            detachedArmature.hideFlags = HideFlags.HideAndDontSave;
            detachedArmature.transform.SetParent(maskRoot.transform, false);
            try
            {
                generated.AddTransformPath(maskRoot.transform, true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(maskRoot);
            }
            for (int index = 0; index < generated.transformCount; index++)
            {
                generated.SetTransformActive(
                    index,
                    IsArmTransformPath(generated.GetTransformPath(index)));
            }

            AvatarMask existing = AssetDatabase.LoadAssetAtPath<AvatarMask>(ArmsMaskPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, ArmsMaskPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                existing.name = "Hands_Carry_Arms";
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static bool IsArmTransformPath(string path)
        {
            return path.Split('/')
                .Any(segment =>
                    segment.EndsWith("LeftShoulder", StringComparison.Ordinal) ||
                    segment.EndsWith("RightShoulder", StringComparison.Ordinal));
        }

        private static bool ArmMaskIsExact(
            AvatarMask mask,
            out int transformCount,
            out int activeCount,
            out bool hasLeftShoulder,
            out bool hasRightShoulder)
        {
            transformCount = mask.transformCount;
            activeCount = 0;
            hasLeftShoulder = false;
            hasRightShoulder = false;
            if (transformCount == 0)
            {
                return false;
            }

            for (int index = 0; index < transformCount; index++)
            {
                string path = mask.GetTransformPath(index);
                if (!string.IsNullOrEmpty(path) &&
                    !string.Equals(path, "Armature", StringComparison.Ordinal) &&
                    !path.StartsWith("Armature/", StringComparison.Ordinal))
                {
                    return false;
                }

                bool expected = IsArmTransformPath(path);
                bool actual = mask.GetTransformActive(index);
                if (expected != actual)
                {
                    return false;
                }

                if (actual)
                {
                    activeCount++;
                    string last = path.Split('/').LastOrDefault() ?? string.Empty;
                    hasLeftShoulder |= last.EndsWith(
                        "LeftShoulder",
                        StringComparison.Ordinal);
                    hasRightShoulder |= last.EndsWith(
                        "RightShoulder",
                        StringComparison.Ordinal);
                }
            }

            return activeCount > 0 && hasLeftShoulder && hasRightShoulder;
        }

        private static AnimatorController CreateOrUpdateLayeredCarryController(
            string path,
            string armStateName,
            AnimationClip emptyClip,
            AnimationClip armClip,
            AvatarMask armsMask)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            if (controller.layers.Length > 2)
            {
                throw new InvalidOperationException(
                    Path.GetFileName(path) + " has unexpected extra layers.");
            }

            while (controller.layers.Length < 2)
            {
                controller.AddLayer("Carry Arms");
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (BlendTree tree in AssetDatabase.LoadAllAssetsAtPath(path)
                         .OfType<BlendTree>()
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(tree, true);
            }

            AnimatorControllerLayer[] layers = controller.layers;
            ClearStateMachine(layers[0].stateMachine);
            ClearStateMachine(layers[1].stateMachine);
            AnimatorState baseState = layers[0].stateMachine.AddState(
                AlignmentBaseStateName);
            baseState.motion = emptyClip;
            baseState.speed = 1f;
            baseState.mirror = false;
            baseState.cycleOffset = 0f;
            baseState.writeDefaultValues = false;
            layers[0].stateMachine.defaultState = baseState;
            AnimatorState armState = layers[1].stateMachine.AddState(armStateName);
            armState.motion = armClip;
            armState.speed = 1f;
            armState.mirror = false;
            armState.cycleOffset = 0f;
            armState.writeDefaultValues = false;
            layers[1].stateMachine.defaultState = armState;

            layers[0].name = "Base Empty Idle";
            layers[0].avatarMask = null;
            layers[0].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[0].defaultWeight = 1f;
            layers[1].name = "Carry Arms";
            layers[1].avatarMask = armsMask;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[1].defaultWeight = 1f;
            controller.layers = layers;
            EditorUtility.SetDirty(baseState);
            EditorUtility.SetDirty(armState);
            EditorUtility.SetDirty(layers[0].stateMachine);
            EditorUtility.SetDirty(layers[1].stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ClearStateMachine(AnimatorStateMachine stateMachine)
        {
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(child.stateMachine);
            }
        }

        private static AlignmentTargetApplyMetrics CreateAlignmentTargetApplyMetrics(
            string target,
            string armStateName,
            AnimationClip armClip,
            AnimationClip emptyClip,
            AnimatorController controller,
            AvatarMask armsMask,
            Animator animator)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            bool hasTwoLayers = layers.Length == 2;
            bool baseUsesEmpty = hasTwoLayers &&
                LayerStateUsesClip(
                    layers[0],
                    AlignmentBaseStateName,
                    emptyClip);
            bool armsUseTake = hasTwoLayers &&
                LayerStateUsesClip(layers[1], armStateName, armClip);
            bool armsUseMask = hasTwoLayers && layers[1].avatarMask == armsMask;
            bool overrideFullWeight = hasTwoLayers &&
                layers[1].blendingMode == AnimatorLayerBlendingMode.Override &&
                Mathf.Abs(layers[1].defaultWeight - 1f) <= 0.0001f;
            return new AlignmentTargetApplyMetrics
            {
                target = target,
                baseState = AlignmentBaseStateName,
                armState = armStateName,
                armTake = armClip.name,
                baseDurationSeconds = emptyClip.length,
                armDurationSeconds = armClip.length,
                hasTwoLayers = hasTwoLayers,
                baseUsesEmptyIdle = baseUsesEmpty,
                armLayerUsesExactTake = armsUseTake,
                armLayerUsesMask = armsUseMask,
                armLayerOverrideAtFullWeight = overrideFullWeight,
                bothClipsLoop =
                    AnimationUtility.GetAnimationClipSettings(emptyClip).loopTime &&
                    AnimationUtility.GetAnimationClipSettings(armClip).loopTime,
                applyRootMotion = animator.applyRootMotion
            };
        }

        private static bool LayerStateUsesClip(
            AnimatorControllerLayer layer,
            string stateName,
            AnimationClip clip)
        {
            AnimatorState[] states = layer.stateMachine.states
                .Select(child => child.state)
                .ToArray();
            return states.Length == 1 &&
                   string.Equals(states[0].name, stateName, StringComparison.Ordinal) &&
                   states[0].motion == clip &&
                   Mathf.Abs(states[0].speed - 1f) <= 0.0001f &&
                   !states[0].mirror &&
                   Mathf.Abs(states[0].cycleOffset) <= 0.0001f;
        }

        private static bool AlignmentControllerCorrect(
            AlignmentTargetApplyMetrics metrics)
        {
            return metrics.hasTwoLayers &&
                   metrics.baseUsesEmptyIdle &&
                   metrics.armLayerUsesExactTake &&
                   metrics.armLayerUsesMask &&
                   metrics.armLayerOverrideAtFullWeight &&
                   metrics.bothClipsLoop &&
                   !metrics.applyRootMotion;
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

        private static bool StateUsesClip(
            AnimatorController controller,
            string stateName,
            AnimationClip clip)
        {
            AnimatorState[] states = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .ToArray();
            return states.Length == 1 &&
                   string.Equals(states[0].name, stateName, StringComparison.Ordinal) &&
                   states[0].motion == clip &&
                   Mathf.Abs(states[0].speed - 1f) <= 0.0001f &&
                   !states[0].mirror &&
                   Mathf.Abs(states[0].cycleOffset) <= 0.0001f;
        }

        private static bool ClipsHaveSameContent(
            AnimationClip source,
            AnimationClip copy)
        {
            if (Mathf.Abs(source.length - copy.length) > 0.00001f ||
                Mathf.Abs(source.frameRate - copy.frameRate) > 0.00001f)
            {
                return false;
            }

            EditorCurveBinding[] sourceBindings = AnimationUtility.GetCurveBindings(source);
            EditorCurveBinding[] copyBindings = AnimationUtility.GetCurveBindings(copy);
            if (sourceBindings.Length != copyBindings.Length)
            {
                return false;
            }

            foreach (EditorCurveBinding binding in sourceBindings)
            {
                if (!copyBindings.Contains(binding))
                {
                    return false;
                }

                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationCurve copyCurve = AnimationUtility.GetEditorCurve(copy, binding);
                if (sourceCurve == null || copyCurve == null ||
                    sourceCurve.length != copyCurve.length)
                {
                    return false;
                }

                for (int sample = 0; sample <= 240; sample++)
                {
                    float time = source.length * sample / 240f;
                    if (Mathf.Abs(sourceCurve.Evaluate(time) - copyCurve.Evaluate(time)) > 0.00001f)
                    {
                        return false;
                    }
                }
            }

            EditorCurveBinding[] sourceObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(source);
            EditorCurveBinding[] copyObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(copy);
            if (sourceObjectBindings.Length != copyObjectBindings.Length)
            {
                return false;
            }

            foreach (EditorCurveBinding binding in sourceObjectBindings)
            {
                if (!copyObjectBindings.Contains(binding))
                {
                    return false;
                }

                ObjectReferenceKeyframe[] sourceKeys =
                    AnimationUtility.GetObjectReferenceCurve(source, binding);
                ObjectReferenceKeyframe[] copyKeys =
                    AnimationUtility.GetObjectReferenceCurve(copy, binding);
                if (sourceKeys.Length != copyKeys.Length)
                {
                    return false;
                }

                for (int index = 0; index < sourceKeys.Length; index++)
                {
                    if (Mathf.Abs(sourceKeys[index].time - copyKeys[index].time) > 0.00001f ||
                        sourceKeys[index].value != copyKeys[index].value)
                    {
                        return false;
                    }
                }
            }

            AnimationEvent[] sourceEvents = AnimationUtility.GetAnimationEvents(source);
            AnimationEvent[] copyEvents = AnimationUtility.GetAnimationEvents(copy);
            if (sourceEvents.Length != copyEvents.Length)
            {
                return false;
            }

            for (int index = 0; index < sourceEvents.Length; index++)
            {
                AnimationEvent first = sourceEvents[index];
                AnimationEvent second = copyEvents[index];
                if (Mathf.Abs(first.time - second.time) > 0.00001f ||
                    !string.Equals(first.functionName, second.functionName, StringComparison.Ordinal) ||
                    !string.Equals(first.stringParameter, second.stringParameter, StringComparison.Ordinal) ||
                    Mathf.Abs(first.floatParameter - second.floatParameter) > 0.00001f ||
                    first.intParameter != second.intParameter ||
                    first.objectReferenceParameter != second.objectReferenceParameter)
                {
                    return false;
                }
            }

            return AnimationUtility.GetAnimationClipSettings(source).loopTime &&
                   AnimationUtility.GetAnimationClipSettings(copy).loopTime;
        }

        private static DrawBackLowPalmLeftBakeResult
            CreateOrUpdateDrawBackLowPalmLeftAdjustedClip(
                Transform template,
                AnimationClip sourceClip,
                bool outerElbowPath = false,
                bool fullTorsoClearance = false,
                bool frontSilhouetteClearance = false,
                float frontSilhouetteOutwardDegrees = 35f)
        {
            GameObject workObject = UnityEngine.Object.Instantiate(template.gameObject);
            workObject.name = "HandsDrawBackLowPalmLeftBakeWork";
            workObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(workObject);
            try
            {
                Transform workRoot = workObject.transform;
                Quaternion rightHandBindLocalRotation =
                    FindRequired(workRoot, RightHandPath).localRotation;
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate));
                float peakForwardProjection = float.NegativeInfinity;
                int sourcePeakFrame = 0;
                float[] sourceForwardProjections = new float[framesPerLoop];
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    float time = sourceClip.length * frame / framesPerLoop;
                    FindRequired(workRoot, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    sourceClip.SampleAnimation(workObject, time);
                    Transform upper = FindRequired(workRoot, RightArmPath);
                    Transform hand = FindRequired(workRoot, RightHandPath);
                    float projection = Vector3.Dot(
                        hand.position - upper.position,
                        workRoot.forward);
                    sourceForwardProjections[frame] = projection;
                    if (projection > peakForwardProjection)
                    {
                        peakForwardProjection = projection;
                        sourcePeakFrame = frame;
                    }
                }

                if (peakForwardProjection <= 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Hands_Draw_Back has no forward right-hand extension to lower.");
                }

                int extractionStartFrame = 0;
                float mostBehindProjection = float.PositiveInfinity;
                for (int frame = 0; frame <= sourcePeakFrame; frame++)
                {
                    if (sourceForwardProjections[frame] < mostBehindProjection)
                    {
                        mostBehindProjection = sourceForwardProjections[frame];
                        extractionStartFrame = frame;
                    }
                }

                int outerPathFrame = Mathf.RoundToInt(
                    Mathf.Lerp(extractionStartFrame, sourcePeakFrame, 0.5f));
                if (outerElbowPath &&
                    sourcePeakFrame - extractionStartFrame < 4)
                {
                    throw new InvalidOperationException(
                        "Hands_Draw_Back has no usable behind-to-forward extraction interval.");
                }

                float sourcePeakTime =
                    sourceClip.length * sourcePeakFrame / framesPerLoop;
                FindRequired(workRoot, RightHandPath).localRotation =
                    rightHandBindLocalRotation;
                sourceClip.SampleAnimation(workObject, sourcePeakTime);
                Transform peakUpper = FindRequired(workRoot, RightArmPath);
                Transform peakLower = FindRequired(workRoot, RightForeArmPath);
                Transform peakHand = FindRequired(workRoot, RightHandPath);
                Vector3 peakFullTarget = CalculateDrawBackLowPalmLeftTarget(
                    workRoot,
                    peakUpper,
                    peakLower,
                    peakHand,
                    30f);
                if (frontSilhouetteClearance)
                {
                    peakFullTarget = RotateDrawBackTargetOutwardKeepingReach(
                        workRoot,
                        peakUpper,
                        peakFullTarget,
                        frontSilhouetteOutwardDegrees);
                }
                float peakTargetProjection = Vector3.Dot(
                    peakFullTarget - peakUpper.position,
                    workRoot.forward);

                string[] adjustedPaths =
                {
                    RightArmPath,
                    RightForeArmPath,
                    RightHandPath
                };
                Dictionary<string, TransformCurveTrack> tracks = adjustedPaths
                    .ToDictionary(
                        path => path,
                        path => new TransformCurveTrack(path),
                        StringComparer.Ordinal);
                float targetReachErrorMax = 0f;
                for (int frame = 0; frame <= framesPerLoop; frame++)
                {
                    int phaseFrame = frame == framesPerLoop ? 0 : frame;
                    float sampleTime =
                        sourceClip.length * phaseFrame / framesPerLoop;
                    float keyTime = sourceClip.length * frame / framesPerLoop;
                    FindRequired(workRoot, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    sourceClip.SampleAnimation(workObject, sampleTime);
                    Transform upper = FindRequired(workRoot, RightArmPath);
                    Transform lower = FindRequired(workRoot, RightForeArmPath);
                    Transform hand = FindRequired(workRoot, RightHandPath);
                    Vector3 sourceTarget = hand.position;
                    float sourceForwardProjection = Vector3.Dot(
                        sourceTarget - upper.position,
                        workRoot.forward);
                    float correctionWeight = Mathf.Clamp01(
                        sourceForwardProjection / peakForwardProjection);
                    correctionWeight = correctionWeight * correctionWeight *
                        (3f - 2f * correctionWeight);
                    Vector3 fullTarget = CalculateDrawBackLowPalmLeftTarget(
                        workRoot,
                        upper,
                        lower,
                        hand,
                        30f);
                    if (frontSilhouetteClearance)
                    {
                        fullTarget = RotateDrawBackTargetOutwardKeepingReach(
                            workRoot,
                            upper,
                            fullTarget,
                            frontSilhouetteOutwardDegrees);
                    }
                    if (phaseFrame != sourcePeakFrame &&
                        correctionWeight > 0.000001f)
                    {
                        float fullProjection = Vector3.Dot(
                            fullTarget - upper.position,
                            workRoot.forward);
                        float maximumProjection = peakTargetProjection - 0.002f;
                        float candidateProjection = Mathf.Lerp(
                            sourceForwardProjection,
                            fullProjection,
                            correctionWeight);
                        float projectionRange =
                            fullProjection - sourceForwardProjection;
                        if (candidateProjection > maximumProjection &&
                            projectionRange > 0.000001f)
                        {
                            correctionWeight = Mathf.Min(
                                correctionWeight,
                                Mathf.Clamp01(
                                    (maximumProjection - sourceForwardProjection) /
                                    projectionRange));
                        }
                    }

                    float outerPathWeight = 0f;
                    if (outerElbowPath &&
                        phaseFrame >= extractionStartFrame &&
                        phaseFrame <= sourcePeakFrame)
                    {
                        float extractionProgress = Mathf.InverseLerp(
                            extractionStartFrame,
                            sourcePeakFrame,
                            phaseFrame);
                        outerPathWeight = Mathf.Sin(
                            Mathf.PI * extractionProgress);
                        outerPathWeight *= outerPathWeight;
                    }

                    float torsoClearanceWeight = 0f;
                    if ((fullTorsoClearance || frontSilhouetteClearance) &&
                        phaseFrame > 0)
                    {
                        float broadWeight;
                        if (frontSilhouetteClearance)
                        {
                            if (phaseFrame <= sourcePeakFrame)
                            {
                                float progress = phaseFrame /
                                    (float)Mathf.Max(sourcePeakFrame, 1);
                                broadWeight = Mathf.SmoothStep(
                                    0f,
                                    1f,
                                    progress / 0.18f);
                            }
                            else
                            {
                                float returnProgress =
                                    (framesPerLoop - phaseFrame) /
                                    (float)Mathf.Max(
                                        framesPerLoop - sourcePeakFrame,
                                        1);
                                broadWeight = Mathf.SmoothStep(
                                    0f,
                                    1f,
                                    returnProgress);
                            }
                        }
                        else
                        {
                            float broadProgress =
                                phaseFrame / (float)sourcePeakFrame;
                            broadWeight = Mathf.Min(
                                Mathf.SmoothStep(
                                    0f,
                                    1f,
                                    broadProgress / 0.18f),
                                Mathf.SmoothStep(
                                    0f,
                                    1f,
                                    (1f - broadProgress) / 0.18f));
                        }

                        Transform torsoCenter = FindRequired(workRoot, SpinePath);
                        Transform shoulder = FindRequired(workRoot, RightShoulderPath);
                        float upperLength = Vector3.Distance(
                            upper.position,
                            lower.position);
                        float torsoBoundary = Mathf.Abs(Vector3.Dot(
                            (frontSilhouetteClearance
                                ? upper.position
                                : shoulder.position) - torsoCenter.position,
                            workRoot.right));
                        float desiredElbowLateral =
                            torsoBoundary + upperLength *
                            (frontSilhouetteClearance ? 0.8f : 0.28f);
                        float desiredHandLateral =
                            torsoBoundary + upperLength *
                            (frontSilhouetteClearance ? 0.55f : 0.12f);
                        float elbowLateral = Vector3.Dot(
                            lower.position - torsoCenter.position,
                            workRoot.right);
                        float handLateral = Vector3.Dot(
                            hand.position - torsoCenter.position,
                            workRoot.right);
                        float elbowNeed = Mathf.Clamp01(
                            (desiredElbowLateral - elbowLateral) /
                            Mathf.Max(upperLength * 0.35f, 0.0001f));
                        float handNeed = Mathf.Clamp01(
                            (desiredHandLateral - handLateral) /
                            Mathf.Max(upperLength * 0.25f, 0.0001f));
                        torsoClearanceWeight = broadWeight * Mathf.Max(
                            0.75f,
                            Mathf.Max(elbowNeed, handNeed * 0.85f));
                    }

                    float effectiveOuterPathWeight = Mathf.Max(
                        outerPathWeight,
                        torsoClearanceWeight);

                    if (correctionWeight > 0.000001f ||
                        effectiveOuterPathWeight > 0.000001f)
                    {
                        Vector3 requestedTarget = Vector3.Lerp(
                            sourceTarget,
                            fullTarget,
                            correctionWeight);
                        Vector3 originalElbowPole = lower.position;
                        if (effectiveOuterPathWeight > 0.000001f)
                        {
                            Transform torsoCenter = FindRequired(
                                workRoot,
                                SpinePath);
                            Transform shoulder = FindRequired(
                                workRoot,
                                RightShoulderPath);
                            float upperLength = Vector3.Distance(
                                upper.position,
                                lower.position);
                            float shoulderLateral = Vector3.Dot(
                                (frontSilhouetteClearance
                                    ? upper.position
                                    : shoulder.position) - torsoCenter.position,
                                workRoot.right);
                            float desiredHandLateral =
                                shoulderLateral + upperLength *
                                (frontSilhouetteClearance
                                    ? 0.55f
                                    : fullTorsoClearance ? 0.12f : 0.08f);
                            float requestedHandLateral = Vector3.Dot(
                                requestedTarget - torsoCenter.position,
                                workRoot.right);
                            float handLateralCorrection =
                                desiredHandLateral - requestedHandLateral;
                            if (frontSilhouetteClearance)
                            {
                                handLateralCorrection = Mathf.Max(
                                    0f,
                                    handLateralCorrection);
                            }

                            requestedTarget += workRoot.right *
                                handLateralCorrection *
                                effectiveOuterPathWeight;
                            Vector3 requestedFromShoulder =
                                requestedTarget - upper.position;
                            float shortenedReach = Mathf.Max(
                                Mathf.Abs(
                                    Vector3.Distance(upper.position, lower.position) -
                                    Vector3.Distance(lower.position, hand.position)) +
                                0.001f,
                                requestedFromShoulder.magnitude -
                                upperLength * 0.35f *
                                (frontSilhouetteClearance
                                    ? outerPathWeight
                                    : effectiveOuterPathWeight));
                            requestedTarget = upper.position +
                                requestedFromShoulder.normalized * shortenedReach;
                            originalElbowPole += workRoot.right *
                                (upperLength *
                                 (fullTorsoClearance ? 3.4f : 2.5f) *
                                 (frontSilhouetteClearance ? 1.15f : 1f) *
                                 effectiveOuterPathWeight);
                        }

                        targetReachErrorMax = Mathf.Max(
                            targetReachErrorMax,
                            SolveTwoBoneIk(
                                upper,
                                lower,
                                hand,
                                requestedTarget,
                                originalElbowPole));
                        Quaternion lowerBeforePalm = lower.localRotation;
                        Quaternion handBeforePalm = hand.localRotation;
                        targetReachErrorMax = Mathf.Max(
                            targetReachErrorMax,
                            AdjustRightArmForPalmFacingCharacterLeft(workRoot));
                        Quaternion lowerPalmLeft = lower.localRotation;
                        Quaternion handPalmLeft = hand.localRotation;
                        lower.localRotation = Quaternion.Slerp(
                            lowerBeforePalm,
                            lowerPalmLeft,
                            correctionWeight);
                        hand.localRotation = Quaternion.Slerp(
                            handBeforePalm,
                            handPalmLeft,
                            correctionWeight);
                    }

                    foreach (string path in adjustedPaths)
                    {
                        tracks[path].Add(keyTime, FindRequired(workRoot, path));
                    }
                }

                AnimationClip generated = new AnimationClip();
                EditorUtility.CopySerialized(sourceClip, generated);
                generated.name = "Hands_Draw_Back_ForwardAdjusted";
                generated.frameRate = sourceClip.frameRate;
                generated.wrapMode = WrapMode.Loop;
                foreach (EditorCurveBinding binding in
                         AnimationUtility.GetCurveBindings(generated)
                             .Where(IsDrawBackRightArmRotationBinding)
                             .ToArray())
                {
                    AnimationUtility.SetEditorCurve(generated, binding, null);
                }

                foreach (TransformCurveTrack track in tracks.Values)
                {
                    SetRotationTrackCurves(generated, track);
                }

                AnimationClipSettings settings =
                    AnimationUtility.GetAnimationClipSettings(generated);
                settings.loopTime = true;
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(generated, settings);
                AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    DrawBackForwardAdjustedClipPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(
                        generated,
                        DrawBackForwardAdjustedClipPath);
                    existing = generated;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, existing);
                    UnityEngine.Object.DestroyImmediate(generated);
                    existing.name = "Hands_Draw_Back_ForwardAdjusted";
                    EditorUtility.SetDirty(existing);
                }

                AssetDatabase.SaveAssets();
                ApplyDrawBackLowPalmLeftResidualPalmCompensation(
                    template,
                    sourceClip,
                    existing,
                    framesPerLoop,
                    peakForwardProjection,
                    rightHandBindLocalRotation);
                DrawBackLowPalmLeftBakeResult result =
                    new DrawBackLowPalmLeftBakeResult
                    {
                        Clip = existing,
                        FramesBaked = framesPerLoop + 1,
                        SourcePeakFrame = sourcePeakFrame,
                        ExtractionStartFrame = extractionStartFrame,
                        OuterPathFrame = outerPathFrame,
                        TargetReachErrorMetersMax = targetReachErrorMax
                    };
                MeasureDrawBackLowPalmLeftAdjustedClip(
                    template,
                    sourceClip,
                    existing,
                    result,
                    rightHandBindLocalRotation);
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(workObject);
            }
        }

        private static Vector3 CalculateDrawBackLowPalmLeftTarget(
            Transform characterRoot,
            Transform upper,
            Transform lower,
            Transform hand,
            float elbowFlexDegrees)
        {
            Transform solarPlexus = FindRequired(characterRoot, SolarPlexusPath);
            float upperLength = Vector3.Distance(upper.position, lower.position);
            float lowerLength = Vector3.Distance(lower.position, hand.position);
            float elbowRadians = elbowFlexDegrees * Mathf.Deg2Rad;
            float targetReach = Mathf.Sqrt(
                upperLength * upperLength +
                lowerLength * lowerLength +
                2f * upperLength * lowerLength * Mathf.Cos(elbowRadians));
            float verticalOffset = Vector3.Dot(
                solarPlexus.position - upper.position,
                characterRoot.up);
            float horizontalSquared =
                targetReach * targetReach - verticalOffset * verticalOffset;
            if (horizontalSquared <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Draw_Back cannot reach solar-plexus height with a 30-degree elbow bend.");
            }

            float horizontalReach = Mathf.Sqrt(horizontalSquared);
            return upper.position +
                   characterRoot.up * verticalOffset +
                   characterRoot.forward * horizontalReach;
        }

        private static Vector3 RotateDrawBackTargetOutwardKeepingReach(
            Transform characterRoot,
            Transform upper,
            Vector3 target,
            float outwardDegrees)
        {
            Vector3 fromUpper = target - upper.position;
            float vertical = Vector3.Dot(fromUpper, characterRoot.up);
            Vector3 horizontal = Vector3.ProjectOnPlane(
                fromUpper,
                characterRoot.up);
            if (horizontal.sqrMagnitude <= 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Draw_Back target has no horizontal reach to move outward.");
            }

            Vector3 outwardDirection = Quaternion.AngleAxis(
                outwardDegrees,
                characterRoot.up) * characterRoot.forward;
            return upper.position +
                   characterRoot.up * vertical +
                   outwardDirection.normalized * horizontal.magnitude;
        }

        private static float MeasureRightArmTorsoClearance(
            Transform root,
            Transform upper,
            Transform lower,
            Transform hand)
        {
            Transform torsoCenter = FindRequired(root, SpinePath);
            Transform shoulder = FindRequired(root, RightShoulderPath);
            Transform hips = FindRequired(root, HipsPath);
            float upperLength = Vector3.Distance(upper.position, lower.position);
            float torsoLateralRadius = Mathf.Max(
                Mathf.Abs(Vector3.Dot(
                    shoulder.position - torsoCenter.position,
                    root.right)),
                upperLength * 0.2f);
            float torsoDepthRadius = upperLength * 0.55f;
            float armRadius = upperLength * 0.16f;
            float lowerY = Vector3.Dot(
                hips.position - torsoCenter.position,
                root.up) - upperLength * 0.08f;
            float upperY = Vector3.Dot(
                shoulder.position - torsoCenter.position,
                root.up) + upperLength * 0.18f;
            float minimum = float.PositiveInfinity;
            for (int index = 0; index < 4; index++)
            {
                float t = Mathf.Lerp(0.45f, 1f, index / 3f);
                minimum = Mathf.Min(
                    minimum,
                    MeasurePointTorsoClearance(
                        root,
                        torsoCenter.position,
                        Vector3.Lerp(upper.position, lower.position, t),
                        torsoLateralRadius,
                        torsoDepthRadius,
                        armRadius,
                        lowerY,
                        upperY));
            }

            for (int index = 0; index < 5; index++)
            {
                float t = index / 4f;
                minimum = Mathf.Min(
                    minimum,
                    MeasurePointTorsoClearance(
                        root,
                        torsoCenter.position,
                        Vector3.Lerp(lower.position, hand.position, t),
                        torsoLateralRadius,
                        torsoDepthRadius,
                        armRadius,
                        lowerY,
                        upperY));
            }

            return minimum;
        }

        private static float MeasureRightArmFrontSilhouetteGap(
            Transform root,
            Transform upper,
            Transform lower,
            Transform hand)
        {
            Transform spine = FindRequired(root, SpinePath);
            Transform hips = FindRequired(root, HipsPath);
            float upperLength = Vector3.Distance(upper.position, lower.position);
            float boundaryLateral = Vector3.Dot(
                upper.position - spine.position,
                root.right);
            float armSilhouetteRadius = upperLength * 0.16f;
            float lowerVertical = Vector3.Dot(
                hips.position - spine.position,
                root.up) - upperLength * 0.08f;
            float upperVertical = Vector3.Dot(
                upper.position - spine.position,
                root.up) + upperLength * 2.8f;
            float minimum = float.PositiveInfinity;
            foreach (Vector3 point in new[] { lower.position, hand.position })
            {
                Vector3 relative = point - spine.position;
                float vertical = Vector3.Dot(relative, root.up);
                if (vertical < lowerVertical || vertical > upperVertical)
                {
                    continue;
                }

                float lateral = Vector3.Dot(relative, root.right);
                minimum = Mathf.Min(
                    minimum,
                    lateral - boundaryLateral - armSilhouetteRadius);
            }

            return float.IsPositiveInfinity(minimum)
                ? armSilhouetteRadius
                : minimum;
        }

        private static float MeasurePointTorsoClearance(
            Transform root,
            Vector3 torsoCenter,
            Vector3 point,
            float torsoLateralRadius,
            float torsoDepthRadius,
            float armRadius,
            float lowerY,
            float upperY)
        {
            Vector3 relative = point - torsoCenter;
            float vertical = Vector3.Dot(relative, root.up);
            if (vertical < lowerY || vertical > upperY)
            {
                return armRadius;
            }

            float lateral = Mathf.Abs(Vector3.Dot(relative, root.right));
            float forward = Mathf.Abs(Vector3.Dot(relative, root.forward));
            float lateralLimit = torsoLateralRadius + armRadius;
            float depthLimit = torsoDepthRadius + armRadius;
            float normalized = Mathf.Sqrt(
                lateral * lateral / (lateralLimit * lateralLimit) +
                forward * forward / (depthLimit * depthLimit));
            return (normalized - 1f) * Mathf.Min(lateralLimit, depthLimit);
        }

        private static void ApplyDrawBackLowPalmLeftResidualPalmCompensation(
            Transform template,
            AnimationClip source,
            AnimationClip adjusted,
            int framesPerLoop,
            float peakForwardProjection,
            Quaternion rightHandBindLocalRotation)
        {
            GameObject sourceObject = UnityEngine.Object.Instantiate(template.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(template.gameObject);
            sourceObject.name = "HandsDrawBackLowPalmLeftPalmSource";
            adjustedObject.name = "HandsDrawBackLowPalmLeftPalmAdjusted";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(adjustedObject);
            try
            {
                TransformCurveTrack handTrack =
                    new TransformCurveTrack(RightHandPath);
                for (int frame = 0; frame <= framesPerLoop; frame++)
                {
                    int phaseFrame = frame == framesPerLoop ? 0 : frame;
                    float sampleTime = source.length * phaseFrame / framesPerLoop;
                    float keyTime = source.length * frame / framesPerLoop;
                    FindRequired(sourceObject.transform, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(sourceObject, sampleTime);
                    adjusted.SampleAnimation(adjustedObject, sampleTime);
                    Transform sourceUpper = FindRequired(
                        sourceObject.transform,
                        RightArmPath);
                    Transform sourceHand = FindRequired(
                        sourceObject.transform,
                        RightHandPath);
                    float sourceProjection = Vector3.Dot(
                        sourceHand.position - sourceUpper.position,
                        sourceObject.transform.forward);
                    float weight = Mathf.Clamp01(
                        sourceProjection / peakForwardProjection);
                    weight = weight * weight * (3f - 2f * weight);
                    Transform adjustedHand = FindRequired(
                        adjustedObject.transform,
                        RightHandPath);
                    Quaternion currentRotation = adjustedHand.rotation;
                    Quaternion palmLeftRotation = Quaternion.FromToRotation(
                        -adjustedHand.right,
                        -adjustedObject.transform.right) * currentRotation;
                    adjustedHand.rotation = Quaternion.Slerp(
                        currentRotation,
                        palmLeftRotation,
                        weight);
                    handTrack.Add(keyTime, adjustedHand);
                }

                SetRotationTrackCurves(adjusted, handTrack);
                EditorUtility.SetDirty(adjusted);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
            }
        }

        private static void MeasureDrawBackLowPalmLeftAdjustedClip(
            Transform template,
            AnimationClip source,
            AnimationClip adjusted,
            DrawBackLowPalmLeftBakeResult result,
            Quaternion rightHandBindLocalRotation)
        {
            GameObject sourceObject = UnityEngine.Object.Instantiate(template.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(template.gameObject);
            sourceObject.name = "HandsDrawBackLowPalmLeftMeasureSource";
            adjustedObject.name = "HandsDrawBackLowPalmLeftMeasureAdjusted";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(adjustedObject);
            try
            {
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(source.length * source.frameRate));
                float adjustedPeakProjection = float.NegativeInfinity;
                result.AdjustedPeakFrame = 0;
                result.MinimumRightArmTorsoClearanceMeters = float.PositiveInfinity;
                result.MinimumClearanceFrame = 0;
                result.MinimumFrontSilhouetteGapMeters = float.PositiveInfinity;
                result.MinimumFrontSilhouetteGapFrame = 0;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    float time = source.length * frame / framesPerLoop;
                    FindRequired(sourceObject.transform, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(sourceObject, time);
                    adjusted.SampleAnimation(adjustedObject, time);
                    Transform adjustedUpper = FindRequired(
                        adjustedObject.transform,
                        RightArmPath);
                    Transform adjustedLower = FindRequired(
                        adjustedObject.transform,
                        RightForeArmPath);
                    Transform adjustedHand = FindRequired(
                        adjustedObject.transform,
                        RightHandPath);
                    float frameClearance = MeasureRightArmTorsoClearance(
                        adjustedObject.transform,
                        adjustedUpper,
                        adjustedLower,
                        adjustedHand);
                    if (frameClearance < result.MinimumRightArmTorsoClearanceMeters)
                    {
                        result.MinimumRightArmTorsoClearanceMeters = frameClearance;
                        result.MinimumClearanceFrame = frame;
                    }
                    float frontSilhouetteGap =
                        MeasureRightArmFrontSilhouetteGap(
                            adjustedObject.transform,
                            adjustedUpper,
                            adjustedLower,
                            adjustedHand);
                    if (frontSilhouetteGap < result.MinimumFrontSilhouetteGapMeters)
                    {
                        result.MinimumFrontSilhouetteGapMeters = frontSilhouetteGap;
                        result.MinimumFrontSilhouetteGapFrame = frame;
                    }
                    float projection = Vector3.Dot(
                        adjustedHand.position - adjustedUpper.position,
                        adjustedObject.transform.forward);
                    if (projection > adjustedPeakProjection)
                    {
                        adjustedPeakProjection = projection;
                        result.AdjustedPeakFrame = frame;
                    }

                    if (frame == result.OuterPathFrame)
                    {
                        Transform sourceSpine = FindRequired(
                            sourceObject.transform,
                            SpinePath);
                        Transform sourceLower = FindRequired(
                            sourceObject.transform,
                            RightForeArmPath);
                        Transform sourceHand = FindRequired(
                            sourceObject.transform,
                            RightHandPath);
                        Transform adjustedSpine = FindRequired(
                            adjustedObject.transform,
                            SpinePath);
                        Transform adjustedShoulder = FindRequired(
                            adjustedObject.transform,
                            RightShoulderPath);
                        result.SourceOuterElbowLateralMeters = Vector3.Dot(
                            sourceLower.position - sourceSpine.position,
                            sourceObject.transform.right);
                        result.SourceOuterHandLateralMeters = Vector3.Dot(
                            sourceHand.position - sourceSpine.position,
                            sourceObject.transform.right);
                        result.AdjustedOuterElbowLateralMeters = Vector3.Dot(
                            adjustedLower.position - adjustedSpine.position,
                            adjustedObject.transform.right);
                        result.AdjustedOuterHandLateralMeters = Vector3.Dot(
                            adjustedHand.position - adjustedSpine.position,
                            adjustedObject.transform.right);
                        result.TorsoOuterBoundaryLateralMeters = Vector3.Dot(
                            adjustedShoulder.position - adjustedSpine.position,
                            adjustedObject.transform.right);
                    }

                    if (frame == result.SourcePeakFrame)
                    {
                        Transform solarPlexus = FindRequired(
                            adjustedObject.transform,
                            SolarPlexusPath);
                        result.AdjustedPeakHandSolarPlexusHeightDifferenceMeters =
                            Mathf.Abs(Vector3.Dot(
                                adjustedHand.position - solarPlexus.position,
                                adjustedObject.transform.up));
                        result.AdjustedPeakElbowFlexDegrees = ElbowFlexDegrees(
                            adjustedUpper,
                            adjustedLower,
                            adjustedHand);
                        Vector3 horizontalDirection = Vector3.ProjectOnPlane(
                            adjustedHand.position - adjustedUpper.position,
                            adjustedObject.transform.up);
                        if (horizontalDirection.sqrMagnitude < 0.0000001f)
                        {
                            throw new InvalidOperationException(
                                "Hands_Draw_Back low palm-left result has no horizontal direction.");
                        }

                        result.AdjustedPeakHorizontalForwardAngleDegrees =
                            Vector3.Angle(
                                horizontalDirection,
                                adjustedObject.transform.forward);
                        result.AdjustedPeakPalmCharacterLeftAngleDegrees =
                            Vector3.Angle(
                                -adjustedHand.right,
                                -adjustedObject.transform.right);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
            }
        }

        private static DrawBackForwardBakeResult
            CreateOrUpdateDrawBackForwardAdjustedClip(
                Transform template,
                AnimationClip sourceClip)
        {
            GameObject workObject = UnityEngine.Object.Instantiate(template.gameObject);
            workObject.name = "HandsDrawBackForwardAngleBakeWork";
            workObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(workObject);
            try
            {
                Transform workRoot = workObject.transform;
                Quaternion rightHandBindLocalRotation =
                    FindRequired(workRoot, RightHandPath).localRotation;
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate));
                float peakForwardProjection = float.NegativeInfinity;
                float sourcePeakReach = 0f;
                int sourcePeakFrame = 0;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    float time = sourceClip.length * frame / framesPerLoop;
                    FindRequired(workRoot, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    sourceClip.SampleAnimation(workObject, time);
                    Transform upper = FindRequired(workRoot, RightArmPath);
                    Transform hand = FindRequired(workRoot, RightHandPath);
                    float projection = Vector3.Dot(
                        hand.position - upper.position,
                        workRoot.forward);
                    if (projection > peakForwardProjection)
                    {
                        peakForwardProjection = projection;
                        sourcePeakReach = Vector3.Distance(
                            upper.position,
                            hand.position);
                        sourcePeakFrame = frame;
                    }
                }

                if (peakForwardProjection <= 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Hands_Draw_Back has no forward right-hand extension to adjust.");
                }

                string[] adjustedPaths =
                {
                    RightArmPath,
                    RightForeArmPath,
                    RightHandPath
                };
                Dictionary<string, TransformCurveTrack> tracks = adjustedPaths
                    .ToDictionary(
                        path => path,
                        path => new TransformCurveTrack(path),
                        StringComparer.Ordinal);
                int adjustedPeakFrame = 0;
                float adjustedPeakProjection = float.NegativeInfinity;
                float sourcePeakAngle = 0f;
                float adjustedPeakAngle = 0f;
                float sourcePeakElbow = 0f;
                float adjustedPeakElbow = 0f;
                float handWorldRotationMax = 0f;
                float reachDifferenceMax = 0f;
                float targetReachErrorMax = 0f;
                for (int frame = 0; frame <= framesPerLoop; frame++)
                {
                    int phaseFrame = frame == framesPerLoop ? 0 : frame;
                    float sampleTime =
                        sourceClip.length * phaseFrame / framesPerLoop;
                    float keyTime = sourceClip.length * frame / framesPerLoop;
                    FindRequired(workRoot, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    sourceClip.SampleAnimation(workObject, sampleTime);
                    Transform upper = FindRequired(workRoot, RightArmPath);
                    Transform lower = FindRequired(workRoot, RightForeArmPath);
                    Transform hand = FindRequired(workRoot, RightHandPath);
                    Vector3 sourceShoulderToHand = hand.position - upper.position;
                    float sourceReach = sourceShoulderToHand.magnitude;
                    if (sourceReach <= 0.0001f)
                    {
                        throw new InvalidOperationException(
                            "Hands_Draw_Back right arm has no usable shoulder-to-hand reach.");
                    }

                    Vector3 sourceDirection = sourceShoulderToHand / sourceReach;
                    Quaternion sourceHandWorldRotation = hand.rotation;
                    float sourceForwardProjection = Vector3.Dot(
                        sourceShoulderToHand,
                        workRoot.forward);
                    float correctionWeight = Mathf.Clamp01(
                        sourceForwardProjection / peakForwardProjection);
                    correctionWeight = correctionWeight * correctionWeight *
                        (3f - 2f * correctionWeight);
                    if (phaseFrame != sourcePeakFrame &&
                        correctionWeight > 0.000001f)
                    {
                        float maximumProjection = sourcePeakReach - 0.001f;
                        Vector3 candidateDirection = Vector3.Slerp(
                            sourceDirection,
                            workRoot.forward,
                            correctionWeight).normalized;
                        float candidateProjection = sourceReach * Vector3.Dot(
                            candidateDirection,
                            workRoot.forward);
                        if (candidateProjection > maximumProjection)
                        {
                            float low = 0f;
                            float high = correctionWeight;
                            for (int iteration = 0; iteration < 16; iteration++)
                            {
                                float middle = (low + high) * 0.5f;
                                Vector3 middleDirection = Vector3.Slerp(
                                    sourceDirection,
                                    workRoot.forward,
                                    middle).normalized;
                                float middleProjection = sourceReach * Vector3.Dot(
                                    middleDirection,
                                    workRoot.forward);
                                if (middleProjection <= maximumProjection)
                                {
                                    low = middle;
                                }
                                else
                                {
                                    high = middle;
                                }
                            }

                            correctionWeight = low;
                        }
                    }

                    float sourceElbow = ElbowFlexDegrees(upper, lower, hand);
                    if (correctionWeight > 0.000001f)
                    {
                        Vector3 desiredDirection = Vector3.Slerp(
                            sourceDirection,
                            workRoot.forward,
                            correctionWeight).normalized;
                        Vector3 requestedTarget =
                            upper.position + desiredDirection * sourceReach;
                        Vector3 originalElbowPole = lower.position;
                        targetReachErrorMax = Mathf.Max(
                            targetReachErrorMax,
                            SolveTwoBoneIk(
                                upper,
                                lower,
                                hand,
                                requestedTarget,
                                originalElbowPole));
                        hand.rotation = sourceHandWorldRotation;
                    }

                    handWorldRotationMax = Mathf.Max(
                        handWorldRotationMax,
                        Quaternion.Angle(sourceHandWorldRotation, hand.rotation));
                    reachDifferenceMax = Mathf.Max(
                        reachDifferenceMax,
                        Mathf.Abs(
                            Vector3.Distance(upper.position, hand.position) -
                            sourceReach));
                    float adjustedProjection = Vector3.Dot(
                        hand.position - upper.position,
                        workRoot.forward);
                    if (frame < framesPerLoop &&
                        adjustedProjection > adjustedPeakProjection)
                    {
                        adjustedPeakProjection = adjustedProjection;
                        adjustedPeakFrame = frame;
                    }

                    if (phaseFrame == sourcePeakFrame)
                    {
                        sourcePeakAngle = Vector3.Angle(
                            sourceDirection,
                            workRoot.forward);
                        sourcePeakElbow = sourceElbow;
                        adjustedPeakAngle = Vector3.Angle(
                            hand.position - upper.position,
                            workRoot.forward);
                        adjustedPeakElbow = ElbowFlexDegrees(
                            upper,
                            lower,
                            hand);
                    }

                    foreach (string path in adjustedPaths)
                    {
                        tracks[path].Add(keyTime, FindRequired(workRoot, path));
                    }
                }

                AnimationClip generated = new AnimationClip();
                EditorUtility.CopySerialized(sourceClip, generated);
                generated.name = "Hands_Draw_Back_ForwardAdjusted";
                generated.frameRate = sourceClip.frameRate;
                generated.wrapMode = WrapMode.Loop;
                foreach (EditorCurveBinding binding in
                         AnimationUtility.GetCurveBindings(generated)
                             .Where(IsDrawBackRightArmRotationBinding)
                             .ToArray())
                {
                    AnimationUtility.SetEditorCurve(generated, binding, null);
                }

                foreach (TransformCurveTrack track in tracks.Values)
                {
                    SetRotationTrackCurves(generated, track);
                }

                AnimationClipSettings settings =
                    AnimationUtility.GetAnimationClipSettings(generated);
                settings.loopTime = true;
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(generated, settings);
                AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    DrawBackForwardAdjustedClipPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(
                        generated,
                        DrawBackForwardAdjustedClipPath);
                    existing = generated;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, existing);
                    UnityEngine.Object.DestroyImmediate(generated);
                    existing.name = "Hands_Draw_Back_ForwardAdjusted";
                    EditorUtility.SetDirty(existing);
                }

                AssetDatabase.SaveAssets();
                ApplyDrawBackForwardHandOrientationCompensation(
                    template,
                    sourceClip,
                    existing,
                    framesPerLoop,
                    rightHandBindLocalRotation);
                DrawBackForwardBakeResult result = new DrawBackForwardBakeResult
                {
                    Clip = existing,
                    FramesBaked = framesPerLoop + 1,
                    SourcePeakFrame = sourcePeakFrame,
                    TargetReachErrorMetersMax = targetReachErrorMax
                };
                MeasureDrawBackForwardAdjustedClip(
                    template,
                    sourceClip,
                    existing,
                    result,
                    rightHandBindLocalRotation);
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(workObject);
            }
        }

        private static void ApplyDrawBackForwardHandOrientationCompensation(
            Transform template,
            AnimationClip source,
            AnimationClip adjusted,
            int framesPerLoop,
            Quaternion rightHandBindLocalRotation)
        {
            GameObject sourceObject = UnityEngine.Object.Instantiate(template.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(template.gameObject);
            sourceObject.name = "HandsDrawBackForwardHandSource";
            adjustedObject.name = "HandsDrawBackForwardHandAdjusted";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(adjustedObject);
            try
            {
                TransformCurveTrack handTrack =
                    new TransformCurveTrack(RightHandPath);
                for (int frame = 0; frame <= framesPerLoop; frame++)
                {
                    int phaseFrame = frame == framesPerLoop ? 0 : frame;
                    float sampleTime = source.length * phaseFrame / framesPerLoop;
                    float keyTime = source.length * frame / framesPerLoop;
                    FindRequired(sourceObject.transform, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(sourceObject, sampleTime);
                    adjusted.SampleAnimation(adjustedObject, sampleTime);
                    Transform sourceHand = FindRequired(
                        sourceObject.transform,
                        RightHandPath);
                    Transform adjustedHand = FindRequired(
                        adjustedObject.transform,
                        RightHandPath);
                    adjustedHand.rotation = sourceHand.rotation;
                    handTrack.Add(keyTime, adjustedHand);
                }

                SetRotationTrackCurves(adjusted, handTrack);
                EditorUtility.SetDirty(adjusted);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
            }
        }

        private static void MeasureDrawBackForwardAdjustedClip(
            Transform template,
            AnimationClip source,
            AnimationClip adjusted,
            DrawBackForwardBakeResult result,
            Quaternion rightHandBindLocalRotation)
        {
            GameObject sourceObject = UnityEngine.Object.Instantiate(template.gameObject);
            GameObject adjustedObject = UnityEngine.Object.Instantiate(template.gameObject);
            sourceObject.name = "HandsDrawBackForwardMeasureSource";
            adjustedObject.name = "HandsDrawBackForwardMeasureAdjusted";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            adjustedObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(adjustedObject);
            try
            {
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(source.length * source.frameRate));
                float adjustedPeakProjection = float.NegativeInfinity;
                result.AdjustedPeakFrame = 0;
                result.HandWorldRotationDifferenceDegreesMax = 0f;
                result.ReachDifferenceMetersMax = 0f;
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    float time = source.length * frame / framesPerLoop;
                    FindRequired(sourceObject.transform, RightHandPath).localRotation =
                        rightHandBindLocalRotation;
                    source.SampleAnimation(sourceObject, time);
                    adjusted.SampleAnimation(adjustedObject, time);
                    Transform sourceUpper = FindRequired(
                        sourceObject.transform,
                        RightArmPath);
                    Transform sourceLower = FindRequired(
                        sourceObject.transform,
                        RightForeArmPath);
                    Transform sourceHand = FindRequired(
                        sourceObject.transform,
                        RightHandPath);
                    Transform adjustedUpper = FindRequired(
                        adjustedObject.transform,
                        RightArmPath);
                    Transform adjustedLower = FindRequired(
                        adjustedObject.transform,
                        RightForeArmPath);
                    Transform adjustedHand = FindRequired(
                        adjustedObject.transform,
                        RightHandPath);
                    float adjustedProjection = Vector3.Dot(
                        adjustedHand.position - adjustedUpper.position,
                        adjustedObject.transform.forward);
                    if (adjustedProjection > adjustedPeakProjection)
                    {
                        adjustedPeakProjection = adjustedProjection;
                        result.AdjustedPeakFrame = frame;
                    }

                    result.HandWorldRotationDifferenceDegreesMax = Mathf.Max(
                        result.HandWorldRotationDifferenceDegreesMax,
                        Quaternion.Angle(
                            sourceHand.rotation,
                            adjustedHand.rotation));
                    result.ReachDifferenceMetersMax = Mathf.Max(
                        result.ReachDifferenceMetersMax,
                        Mathf.Abs(
                            Vector3.Distance(
                                sourceUpper.position,
                                sourceHand.position) -
                            Vector3.Distance(
                                adjustedUpper.position,
                                adjustedHand.position)));
                    if (frame == result.SourcePeakFrame)
                    {
                        result.SourcePeakForwardAngleDegrees = Vector3.Angle(
                            sourceHand.position - sourceUpper.position,
                            sourceObject.transform.forward);
                        result.AdjustedPeakForwardAngleDegrees = Vector3.Angle(
                            adjustedHand.position - adjustedUpper.position,
                            adjustedObject.transform.forward);
                        result.SourcePeakElbowFlexDegrees = ElbowFlexDegrees(
                            sourceUpper,
                            sourceLower,
                            sourceHand);
                        result.AdjustedPeakElbowFlexDegrees = ElbowFlexDegrees(
                            adjustedUpper,
                            adjustedLower,
                            adjustedHand);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(adjustedObject);
            }
        }

        private static float ElbowFlexDegrees(
            Transform upper,
            Transform lower,
            Transform hand)
        {
            Vector3 upperDirection = lower.position - upper.position;
            Vector3 lowerDirection = hand.position - lower.position;
            if (upperDirection.sqrMagnitude < 0.0000001f ||
                lowerDirection.sqrMagnitude < 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Player Hands arm has no usable elbow segments.");
            }

            return Vector3.Angle(upperDirection, lowerDirection);
        }

        private static bool AnimationMatchesExceptDrawBackRightArmRotations(
            AnimationClip source,
            AnimationClip adjusted)
        {
            EditorCurveBinding[] sourceBindings =
                AnimationUtility.GetCurveBindings(source);
            EditorCurveBinding[] adjustedBindings =
                AnimationUtility.GetCurveBindings(adjusted);
            EditorCurveBinding[] sourceUnchangedBindings = sourceBindings
                .Where(binding => !IsDrawBackRightArmRotationBinding(binding))
                .ToArray();
            EditorCurveBinding[] adjustedUnchangedBindings = adjustedBindings
                .Where(binding => !IsDrawBackRightArmRotationBinding(binding))
                .ToArray();
            if (sourceUnchangedBindings.Length != adjustedUnchangedBindings.Length ||
                sourceUnchangedBindings.Any(
                    binding => !adjustedUnchangedBindings.Contains(binding)))
            {
                return false;
            }

            foreach (EditorCurveBinding binding in sourceUnchangedBindings)
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(
                    source,
                    binding);
                AnimationCurve adjustedCurve = AnimationUtility.GetEditorCurve(
                    adjusted,
                    binding);
                if (!AnimationCurvesEqual(sourceCurve, adjustedCurve))
                {
                    return false;
                }
            }

            EditorCurveBinding[] sourceObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(source);
            EditorCurveBinding[] adjustedObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(adjusted);
            if (sourceObjectBindings.Length != adjustedObjectBindings.Length ||
                sourceObjectBindings.Any(
                    binding => !adjustedObjectBindings.Contains(binding)))
            {
                return false;
            }

            foreach (EditorCurveBinding binding in sourceObjectBindings)
            {
                ObjectReferenceKeyframe[] sourceKeys =
                    AnimationUtility.GetObjectReferenceCurve(source, binding);
                ObjectReferenceKeyframe[] adjustedKeys =
                    AnimationUtility.GetObjectReferenceCurve(adjusted, binding);
                if (sourceKeys.Length != adjustedKeys.Length)
                {
                    return false;
                }

                for (int index = 0; index < sourceKeys.Length; index++)
                {
                    if (Mathf.Abs(
                            sourceKeys[index].time - adjustedKeys[index].time) >
                        0.000001f ||
                        sourceKeys[index].value != adjustedKeys[index].value)
                    {
                        return false;
                    }
                }
            }

            AnimationEvent[] sourceEvents =
                AnimationUtility.GetAnimationEvents(source);
            AnimationEvent[] adjustedEvents =
                AnimationUtility.GetAnimationEvents(adjusted);
            if (sourceEvents.Length != adjustedEvents.Length)
            {
                return false;
            }

            for (int index = 0; index < sourceEvents.Length; index++)
            {
                AnimationEvent first = sourceEvents[index];
                AnimationEvent second = adjustedEvents[index];
                if (Mathf.Abs(first.time - second.time) > 0.000001f ||
                    !string.Equals(
                        first.functionName,
                        second.functionName,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        first.stringParameter,
                        second.stringParameter,
                        StringComparison.Ordinal) ||
                    Mathf.Abs(first.floatParameter - second.floatParameter) >
                        0.000001f ||
                    first.intParameter != second.intParameter ||
                    first.objectReferenceParameter != second.objectReferenceParameter)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsDrawBackRightArmRotationBinding(
            EditorCurveBinding binding)
        {
            return (string.Equals(binding.path, RightArmPath, StringComparison.Ordinal) ||
                    string.Equals(binding.path, RightForeArmPath, StringComparison.Ordinal) ||
                    string.Equals(binding.path, RightHandPath, StringComparison.Ordinal)) &&
                   (binding.propertyName.IndexOf(
                        "Rotation",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    binding.propertyName.IndexOf(
                        "Euler",
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool AnimationCurvesEqual(
            AnimationCurve first,
            AnimationCurve second)
        {
            if (first == null || second == null ||
                first.length != second.length ||
                first.preWrapMode != second.preWrapMode ||
                first.postWrapMode != second.postWrapMode)
            {
                return false;
            }

            for (int index = 0; index < first.length; index++)
            {
                Keyframe firstKey = first.keys[index];
                Keyframe secondKey = second.keys[index];
                if (Mathf.Abs(firstKey.time - secondKey.time) > 0.000001f ||
                    Mathf.Abs(firstKey.value - secondKey.value) > 0.000001f ||
                    Mathf.Abs(firstKey.inTangent - secondKey.inTangent) > 0.000001f ||
                    Mathf.Abs(firstKey.outTangent - secondKey.outTangent) > 0.000001f ||
                    Mathf.Abs(firstKey.inWeight - secondKey.inWeight) > 0.000001f ||
                    Mathf.Abs(firstKey.outWeight - secondKey.outWeight) > 0.000001f ||
                    firstKey.weightedMode != secondKey.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }

        private static BakedArmClipResult CreateOrUpdateAdjustedArmClip(
            Transform template,
            AnimationClip emptyClip,
            AnimationClip sourceClip,
            string assetPath,
            string clipName,
            CarryPoseAdjustmentKind kind,
            float rightHandGripTwistDegrees,
            bool naturalRightArmAdjustment,
            bool actualPalmInward,
            bool palmFacingCharacterLeft = false)
        {
            GameObject workObject = UnityEngine.Object.Instantiate(template.gameObject);
            GameObject sourceObject = UnityEngine.Object.Instantiate(template.gameObject);
            workObject.name = clipName + "BakeWork";
            sourceObject.name = clipName + "SourceWork";
            workObject.hideFlags = HideFlags.HideAndDontSave;
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(workObject);
            DisableAnimators(sourceObject);
            try
            {
                Transform workRoot = workObject.transform;
                Transform sourceRoot = sourceObject.transform;
                string[] armPaths = template.GetComponentsInChildren<Transform>(true)
                    .Select(item => AnimationUtility.CalculateTransformPath(item, template))
                    .Where(IsArmTransformPath)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                if (armPaths.Length != 8)
                {
                    throw new InvalidOperationException(
                        template.name + " must expose exactly eight shoulder-to-hand transforms; actual=" +
                        armPaths.Length.ToString(CultureInfo.InvariantCulture) + ".");
                }

                PrepareArmPose(workRoot, sourceRoot, emptyClip, sourceClip, 0f, 0f);
                Vector3 shiftWorld;
                Quaternion rightHandLocalDelta = Quaternion.identity;
                if (kind == CarryPoseAdjustmentKind.OneHandLeftArmDown)
                {
                    Transform upper = FindRequired(workRoot, LeftArmPath);
                    Transform lower = FindRequired(workRoot, LeftForeArmPath);
                    Transform hand = FindRequired(workRoot, LeftHandPath);
                    float armLength = Vector3.Distance(upper.position, lower.position) +
                        Vector3.Distance(lower.position, hand.position);
                    Vector3 desired = upper.position -
                        workRoot.up * (armLength * 0.92f) -
                        workRoot.right * (armLength * 0.22f) +
                        workRoot.forward * (armLength * 0.08f);
                    shiftWorld = desired - hand.position;
                    if (!naturalRightArmAdjustment)
                    {
                        Transform rightHand = FindRequired(workRoot, RightHandPath);
                        Transform spine = FindRequired(workRoot, SpinePath);
                        Quaternion originalRightHandLocal = rightHand.localRotation;
                        AlignRightHandForVerticalGrip(
                            rightHand,
                            workRoot,
                            spine,
                            rightHandGripTwistDegrees,
                            false);
                        rightHandLocalDelta =
                            rightHand.localRotation *
                            Quaternion.Inverse(originalRightHandLocal);
                    }
                }
                else
                {
                    Transform spine = FindRequired(workRoot, SpinePath);
                    Transform rightShoulder = FindRequired(workRoot, RightShoulderPath);
                    Transform leftHand = FindRequired(workRoot, LeftHandPath);
                    Transform rightHand = FindRequired(workRoot, RightHandPath);
                    Vector3 handCenter = (leftHand.position + rightHand.position) * 0.5f;
                    Vector3 rightChest = Vector3.Lerp(
                        spine.position,
                        rightShoulder.position,
                        0.65f);
                    shiftWorld = workRoot.right * Vector3.Dot(
                        rightChest - handCenter,
                        workRoot.right);
                }

                Vector3 rootLocalTranslation =
                    workRoot.InverseTransformVector(shiftWorld);
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate));
                Dictionary<string, TransformCurveTrack> tracks = armPaths.ToDictionary(
                    path => path,
                    path => new TransformCurveTrack(path),
                    StringComparer.Ordinal);
                float reachErrorMax = 0f;
                for (int frame = 0; frame <= framesPerLoop; frame++)
                {
                    float time = sourceClip.length * frame / framesPerLoop;
                    PrepareArmPose(
                        workRoot,
                        sourceRoot,
                        emptyClip,
                        sourceClip,
                        0f,
                        time);
                    Vector3 frameShift = workRoot.TransformVector(rootLocalTranslation);
                    if (kind == CarryPoseAdjustmentKind.OneHandLeftArmDown)
                    {
                        Transform upper = FindRequired(workRoot, LeftArmPath);
                        Transform lower = FindRequired(workRoot, LeftForeArmPath);
                        Transform hand = FindRequired(workRoot, LeftHandPath);
                        float armLength = Vector3.Distance(upper.position, lower.position) +
                            Vector3.Distance(lower.position, hand.position);
                        Vector3 target = hand.position + frameShift;
                        Vector3 pole = upper.position -
                            workRoot.forward * armLength +
                            -workRoot.right * (armLength * 0.25f) -
                            workRoot.up * (armLength * 0.2f);
                        reachErrorMax = Mathf.Max(
                            reachErrorMax,
                            SolveTwoBoneIk(upper, lower, hand, target, pole));
                        if (naturalRightArmAdjustment)
                        {
                            reachErrorMax = Mathf.Max(
                                reachErrorMax,
                                palmFacingCharacterLeft
                                    ? AdjustRightArmForPalmFacingCharacterLeft(workRoot)
                                    : AdjustRightArmForNaturalVerticalGrip(
                                        workRoot,
                                        actualPalmInward));
                        }
                        else
                        {
                            Transform rightHand = FindRequired(workRoot, RightHandPath);
                            rightHand.localRotation =
                                rightHandLocalDelta * rightHand.localRotation;
                        }
                    }
                    else
                    {
                        Transform leftUpper = FindRequired(workRoot, LeftArmPath);
                        Transform leftLower = FindRequired(workRoot, LeftForeArmPath);
                        Transform leftHand = FindRequired(workRoot, LeftHandPath);
                        Transform rightUpper = FindRequired(workRoot, RightArmPath);
                        Transform rightLower = FindRequired(workRoot, RightForeArmPath);
                        Transform rightHand = FindRequired(workRoot, RightHandPath);
                        Vector3 leftTarget = leftHand.position + frameShift;
                        Vector3 rightTarget = rightHand.position + frameShift;
                        reachErrorMax = Mathf.Max(
                            reachErrorMax,
                            SolveTwoBoneIk(
                                leftUpper,
                                leftLower,
                                leftHand,
                                leftTarget,
                                leftLower.position + frameShift * 0.5f));
                        reachErrorMax = Mathf.Max(
                            reachErrorMax,
                            SolveTwoBoneIk(
                                rightUpper,
                                rightLower,
                                rightHand,
                                rightTarget,
                                rightLower.position + frameShift * 0.5f));
                    }

                    foreach (string path in armPaths)
                    {
                        tracks[path].Add(time, FindRequired(workRoot, path));
                    }
                }

                AnimationClip generated = new AnimationClip
                {
                    name = clipName,
                    frameRate = sourceClip.frameRate,
                    wrapMode = WrapMode.Loop,
                    legacy = false
                };
                foreach (TransformCurveTrack track in tracks.Values)
                {
                    SetTransformTrackCurves(generated, track);
                }

                generated.EnsureQuaternionContinuity();
                AnimationClipSettings settings =
                    AnimationUtility.GetAnimationClipSettings(generated);
                settings.loopTime = true;
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(generated, settings);
                AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(generated, assetPath);
                    existing = generated;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, existing);
                    UnityEngine.Object.DestroyImmediate(generated);
                    existing.name = clipName;
                    EditorUtility.SetDirty(existing);
                }

                AssetDatabase.SaveAssets();
                return new BakedArmClipResult
                {
                    Clip = existing,
                    FramesBaked = framesPerLoop + 1,
                    TargetReachErrorMax = reachErrorMax,
                    RootLocalTranslation = rootLocalTranslation
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(workObject);
                UnityEngine.Object.DestroyImmediate(sourceObject);
            }
        }

        private static PoseAdjustmentTargetApplyMetrics
            CreatePoseAdjustmentTargetApplyMetrics(
                string target,
                string adjustment,
                string adjustedClipPath,
                AnimationClip sourceClip,
                BakedArmClipResult bake,
                AnimatorController controller,
                Animator animator)
        {
            bool durationAndRate =
                Mathf.Abs(sourceClip.length - bake.Clip.length) <= 0.0001f &&
                Mathf.Abs(sourceClip.frameRate - bake.Clip.frameRate) <= 0.0001f;
            bool loops = AnimationUtility.GetAnimationClipSettings(bake.Clip).loopTime;
            bool onlyArms = AdjustedClipOnlyContainsArmCurves(bake.Clip);
            bool controllerUses = controller.layers.Length == 2 &&
                LayerStateUsesClip(
                    controller.layers[1],
                    target == OneHandTargetName ? OneHandStateName : TwoHandStateName,
                    bake.Clip);
            PoseAdjustmentTargetApplyMetrics metrics =
                new PoseAdjustmentTargetApplyMetrics
                {
                    target = target,
                    adjustment = adjustment,
                    adjustedClipPath = adjustedClipPath,
                    sourceDurationSeconds = sourceClip.length,
                    adjustedDurationSeconds = bake.Clip.length,
                    frameRate = bake.Clip.frameRate,
                    framesBaked = bake.FramesBaked,
                    adjustedCurveCount = AnimationUtility.GetCurveBindings(bake.Clip).Length,
                    targetReachErrorMax = bake.TargetReachErrorMax,
                    rootLocalTranslation = bake.RootLocalTranslation,
                    durationAndFrameRatePreserved = durationAndRate,
                    adjustedClipLoops = loops,
                    adjustedClipOnlyContainsArmCurves = onlyArms,
                    controllerUsesAdjustedClip = controllerUses,
                    applyRootMotion = animator.applyRootMotion
                };
            metrics.passedNumericChecks =
                durationAndRate &&
                loops &&
                onlyArms &&
                controllerUses &&
                bake.TargetReachErrorMax <= 0.005f &&
                !animator.applyRootMotion;
            return metrics;
        }

        private static bool AdjustedClipOnlyContainsArmCurves(AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            return bindings.Length == 56 && bindings.All(binding =>
                IsArmTransformPath(binding.path) &&
                (binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal) ||
                 binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal)));
        }

        private static void DisableAnimators(GameObject value)
        {
            foreach (Animator animator in value.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }
        }

        private static Transform FindRequired(Transform root, string path)
        {
            return root.Find(path) ??
                throw new InvalidOperationException(
                    root.name + " is missing required transform " + path + ".");
        }

        private static void PrepareArmPose(
            Transform workRoot,
            Transform sourceRoot,
            AnimationClip emptyClip,
            AnimationClip armClip,
            float emptyTime,
            float armTime)
        {
            emptyClip.SampleAnimation(
                workRoot.gameObject,
                Mathf.Repeat(emptyTime, emptyClip.length));
            armClip.SampleAnimation(
                sourceRoot.gameObject,
                Mathf.Repeat(armTime, armClip.length));
            foreach (Transform source in sourceRoot.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(source, sourceRoot);
                if (!IsArmTransformPath(path))
                {
                    continue;
                }

                Transform destination = FindRequired(workRoot, path);
                destination.localPosition = source.localPosition;
                destination.localRotation = source.localRotation;
                destination.localScale = source.localScale;
            }
        }

        private static float SolveTwoBoneIk(
            Transform upper,
            Transform lower,
            Transform hand,
            Vector3 requestedTarget,
            Vector3 pole)
        {
            Vector3 rootPosition = upper.position;
            float upperLength = Vector3.Distance(rootPosition, lower.position);
            float lowerLength = Vector3.Distance(lower.position, hand.position);
            float minimumReach = Mathf.Abs(upperLength - lowerLength) + 0.0001f;
            float maximumReach = upperLength + lowerLength - 0.0001f;
            Vector3 targetVector = requestedTarget - rootPosition;
            Vector3 direction = targetVector.sqrMagnitude > 0.0000001f
                ? targetVector.normalized
                : (hand.position - rootPosition).normalized;
            float reach = Mathf.Clamp(targetVector.magnitude, minimumReach, maximumReach);
            Vector3 target = rootPosition + direction * reach;
            Vector3 poleVector = pole - rootPosition;
            Vector3 bendDirection = Vector3.ProjectOnPlane(poleVector, direction);
            if (bendDirection.sqrMagnitude < 0.0000001f)
            {
                bendDirection = Vector3.ProjectOnPlane(
                    lower.position - rootPosition,
                    direction);
            }

            if (bendDirection.sqrMagnitude < 0.0000001f)
            {
                bendDirection = Vector3.Cross(direction, Vector3.up);
            }

            bendDirection.Normalize();
            float along =
                (upperLength * upperLength + reach * reach - lowerLength * lowerLength) /
                (2f * reach);
            float perpendicular = Mathf.Sqrt(
                Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 elbowTarget =
                rootPosition + direction * along + bendDirection * perpendicular;
            Vector3 currentUpperDirection = lower.position - rootPosition;
            upper.rotation = Quaternion.FromToRotation(
                currentUpperDirection,
                elbowTarget - rootPosition) * upper.rotation;
            Vector3 elbowPosition = lower.position;
            Vector3 currentLowerDirection = hand.position - elbowPosition;
            lower.rotation = Quaternion.FromToRotation(
                currentLowerDirection,
                target - elbowPosition) * lower.rotation;
            return Vector3.Distance(hand.position, requestedTarget);
        }

        private static void AlignRightHandForVerticalGrip(
            Transform hand,
            Transform characterRoot,
            Transform spine,
            float gripTwistDegrees,
            bool actualPalmInward)
        {
            Vector3 gripAxis = characterRoot.up.normalized;
            Vector3 inward = Vector3.ProjectOnPlane(
                spine.position - hand.position,
                gripAxis);
            if (inward.sqrMagnitude < 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Carry_OneHand right hand has no usable palm direction.");
            }

            Vector3 palmInward = inward.normalized;
            Vector3 handLocalRightTarget = actualPalmInward
                ? palmInward
                : -palmInward;
            Vector3 fingerDirection = Vector3.Cross(
                gripAxis,
                handLocalRightTarget).normalized;
            if (fingerDirection.sqrMagnitude < 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Carry_OneHand right hand has no usable finger direction.");
            }

            hand.rotation = Quaternion.LookRotation(
                gripAxis,
                fingerDirection);
            hand.rotation = Quaternion.AngleAxis(
                gripTwistDegrees,
                gripAxis) * hand.rotation;
        }

        private static float AdjustRightArmForNaturalVerticalGrip(
            Transform characterRoot,
            bool actualPalmInward)
        {
            Transform upper = FindRequired(characterRoot, RightArmPath);
            Transform lower = FindRequired(characterRoot, RightForeArmPath);
            Transform hand = FindRequired(characterRoot, RightHandPath);
            Transform spine = FindRequired(characterRoot, SpinePath);
            Vector3 requestedHandPosition = hand.position;
            Quaternion sourceHandLocalRotation = hand.localRotation;
            float upperLength = Vector3.Distance(upper.position, lower.position);
            float lowerLength = Vector3.Distance(lower.position, hand.position);
            Vector3 gripAxis = characterRoot.up.normalized;
            Vector3 palmInward = Vector3.ProjectOnPlane(
                spine.position - hand.position,
                gripAxis);
            if (palmInward.sqrMagnitude < 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Carry_OneHand right hand has no usable anatomical palm direction.");
            }

            palmInward.Normalize();
            Vector3 desiredForeArmDirection = Vector3.Cross(
                gripAxis,
                palmInward).normalized;
            if (desiredForeArmDirection.sqrMagnitude < 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Carry_OneHand right hand has no usable anatomical forearm direction.");
            }

            Vector3 candidateElbow =
                requestedHandPosition - desiredForeArmDirection * lowerLength;
            Vector3 shoulderToElbow = candidateElbow - upper.position;
            if (shoulderToElbow.sqrMagnitude < 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Carry_OneHand right arm has no usable anatomical elbow direction.");
            }

            Vector3 elbowTarget = upper.position +
                shoulderToElbow.normalized * upperLength;
            Vector3 handTarget = elbowTarget +
                desiredForeArmDirection * lowerLength;
            Vector3 currentUpperDirection = lower.position - upper.position;
            upper.rotation = Quaternion.FromToRotation(
                currentUpperDirection,
                elbowTarget - upper.position) * upper.rotation;
            Vector3 currentLowerDirection = hand.position - lower.position;
            lower.rotation = Quaternion.FromToRotation(
                currentLowerDirection,
                handTarget - lower.position) * lower.rotation;
            float reachError = Vector3.Distance(hand.position, handTarget);

            Quaternion baseForeArmRotation = lower.rotation;
            Vector3 foreArmAxis = hand.position - lower.position;
            if (foreArmAxis.sqrMagnitude < 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Carry_OneHand right forearm has no usable twist axis.");
            }

            foreArmAxis.Normalize();
            AlignRightHandForVerticalGrip(
                hand,
                characterRoot,
                spine,
                0f,
                actualPalmInward);
            Quaternion desiredHandRotation = hand.rotation;
            float bestTwistDegrees = 0f;
            float bestScore = float.PositiveInfinity;
            for (int sample = 0; sample <= 120; sample++)
            {
                float twistDegrees = -180f + sample * 3f;
                lower.rotation = Quaternion.AngleAxis(
                    twistDegrees,
                    foreArmAxis) * baseForeArmRotation;
                hand.rotation = desiredHandRotation;
                float wristDifference = Quaternion.Angle(
                    sourceHandLocalRotation,
                    hand.localRotation);
                float foreArmTwist = Mathf.Abs(twistDegrees);
                float score = Mathf.Max(wristDifference, foreArmTwist) +
                    (wristDifference + foreArmTwist) * 0.15f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTwistDegrees = twistDegrees;
                }
            }

            lower.rotation = Quaternion.AngleAxis(
                bestTwistDegrees,
                foreArmAxis) * baseForeArmRotation;
            hand.rotation = desiredHandRotation;
            return Mathf.Max(
                reachError,
                Vector3.Distance(hand.position, handTarget));
        }

        private static float AdjustRightArmForPalmFacingCharacterLeft(
            Transform characterRoot)
        {
            Transform lower = FindRequired(characterRoot, RightForeArmPath);
            Transform hand = FindRequired(characterRoot, RightHandPath);
            Vector3 requestedHandPosition = hand.position;
            Quaternion sourceHandLocalRotation = hand.localRotation;
            Vector3 desiredPalmDirection = -characterRoot.right.normalized;
            Vector3 actualPalmDirection = -hand.right.normalized;
            if (desiredPalmDirection.sqrMagnitude < 0.999f ||
                actualPalmDirection.sqrMagnitude < 0.999f)
            {
                throw new InvalidOperationException(
                    "Hands_Carry_OneHand right hand has no usable character-left palm direction.");
            }

            Quaternion desiredHandRotation = Quaternion.FromToRotation(
                actualPalmDirection,
                desiredPalmDirection) * hand.rotation;
            Quaternion baseForeArmRotation = lower.rotation;
            Vector3 foreArmAxis = hand.position - lower.position;
            if (foreArmAxis.sqrMagnitude < 0.0000001f)
            {
                throw new InvalidOperationException(
                    "Hands_Carry_OneHand right forearm has no usable palm-left twist axis.");
            }

            foreArmAxis.Normalize();
            float bestTwistDegrees = 0f;
            float bestScore = float.PositiveInfinity;
            for (int sample = 0; sample <= 180; sample++)
            {
                float twistDegrees = -180f + sample * 2f;
                lower.rotation = Quaternion.AngleAxis(
                    twistDegrees,
                    foreArmAxis) * baseForeArmRotation;
                hand.rotation = desiredHandRotation;
                float wristDifference = Quaternion.Angle(
                    sourceHandLocalRotation,
                    hand.localRotation);
                float foreArmTwist = Mathf.Abs(twistDegrees);
                float score = Mathf.Max(wristDifference, foreArmTwist) +
                    (wristDifference + foreArmTwist) * 0.15f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTwistDegrees = twistDegrees;
                }
            }

            lower.rotation = Quaternion.AngleAxis(
                bestTwistDegrees,
                foreArmAxis) * baseForeArmRotation;
            hand.rotation = desiredHandRotation;
            return Vector3.Distance(hand.position, requestedHandPosition);
        }

        private static void SetTransformTrackCurves(
            AnimationClip clip,
            TransformCurveTrack track)
        {
            SetTransformCurve(clip, track.Path, "m_LocalPosition.x", track.PositionX);
            SetTransformCurve(clip, track.Path, "m_LocalPosition.y", track.PositionY);
            SetTransformCurve(clip, track.Path, "m_LocalPosition.z", track.PositionZ);
            SetTransformCurve(clip, track.Path, "m_LocalRotation.x", track.RotationX);
            SetTransformCurve(clip, track.Path, "m_LocalRotation.y", track.RotationY);
            SetTransformCurve(clip, track.Path, "m_LocalRotation.z", track.RotationZ);
            SetTransformCurve(clip, track.Path, "m_LocalRotation.w", track.RotationW);
        }

        private static void SetRotationTrackCurves(
            AnimationClip clip,
            TransformCurveTrack track)
        {
            SetTransformCurve(clip, track.Path, "m_LocalRotation.x", track.RotationX);
            SetTransformCurve(clip, track.Path, "m_LocalRotation.y", track.RotationY);
            SetTransformCurve(clip, track.Path, "m_LocalRotation.z", track.RotationZ);
            SetTransformCurve(clip, track.Path, "m_LocalRotation.w", track.RotationW);
        }

        private static void SetTransformCurve(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyList<Keyframe> keys)
        {
            AnimationCurve curve = new AnimationCurve(keys.ToArray());
            for (int index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static string HashSelectedTransformCurves(
            AnimationClip clip,
            params string[] paths)
        {
            HashSet<string> selected = new HashSet<string>(
                paths,
                StringComparer.Ordinal);
            StringBuilder builder = new StringBuilder();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding => selected.Contains(binding.path))
                         .OrderBy(binding => binding.path, StringComparer.Ordinal)
                         .ThenBy(binding => binding.propertyName, StringComparer.Ordinal))
            {
                builder.Append(binding.path).Append('|')
                    .Append(binding.propertyName).Append('|');
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (Keyframe key in curve.keys)
                {
                    builder.Append(key.time.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.value.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture))
                        .Append(',')
                        .Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture))
                        .Append(';');
                }
            }

            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())))
                    .Replace("-", string.Empty);
            }
        }

        private static GripClearanceReviewMetrics CaptureGripClearanceMetrics(
            Transform target,
            AnimationClip emptyClip,
            AnimationClip sourceClip,
            AnimationClip adjustedClip,
            float expectedGripTwistDegrees,
            bool naturalRightArmAdjustment,
            bool palmFacingCharacterLeft = false)
        {
            Animator animator = RequireAnimator(target);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = target.position;
            float reviewDuration = Mathf.Max(
                emptyClip.length * 2f,
                adjustedClip.length * 2f);
            float sampleRate = Mathf.Max(emptyClip.frameRate, adjustedClip.frameRate);
            int framesSampled = Mathf.Max(
                8,
                Mathf.CeilToInt(reviewDuration * sampleRate));
            float rootMax = 0f;
            float bodyPositionMax = 0f;
            float bodyRotationMax = 0f;
            float leftForeArmClearanceMin = float.PositiveInfinity;
            float leftHandClearanceMin = float.PositiveInfinity;
            float rightUpperPositionMax = 0f;
            float rightUpperRotationMax = 0f;
            float rightHandPositionMax = 0f;
            float rightElbowOutsideMin = float.PositiveInfinity;
            float rightElbowBelowShoulderMin = float.PositiveInfinity;
            float rightWristLocalRotationMax = 0f;
            float rightForeArmWristAlignmentMax = 0f;
            float verticalGripAngleMax = 0f;
            float palmFromInwardAngleMin = float.PositiveInfinity;
            float palmInwardAngleMax = 0f;
            float palmTargetAngleMax = 0f;
            Vector3 sourceMin = new Vector3(
                float.PositiveInfinity,
                float.PositiveInfinity,
                float.PositiveInfinity);
            Vector3 sourceMax = new Vector3(
                float.NegativeInfinity,
                float.NegativeInfinity,
                float.NegativeInfinity);
            Vector3 adjustedMin = sourceMin;
            Vector3 adjustedMax = sourceMax;
            GameObject emptyObject = UnityEngine.Object.Instantiate(target.gameObject);
            GameObject baselineObject = UnityEngine.Object.Instantiate(target.gameObject);
            GameObject sourceObject = UnityEngine.Object.Instantiate(target.gameObject);
            emptyObject.hideFlags = HideFlags.HideAndDontSave;
            baselineObject.hideFlags = HideFlags.HideAndDontSave;
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(emptyObject);
            DisableAnimators(baselineObject);
            DisableAnimators(sourceObject);
            string[] unchangedRightPaths =
            {
                RightShoulderPath,
                RightArmPath,
                RightForeArmPath
            };
            try
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                for (int frame = 0; frame < framesSampled; frame++)
                {
                    float time = reviewDuration * frame / framesSampled;
                    emptyClip.SampleAnimation(
                        emptyObject,
                        Mathf.Repeat(time, emptyClip.length));
                    PrepareArmPose(
                        baselineObject.transform,
                        sourceObject.transform,
                        emptyClip,
                        sourceClip,
                        time,
                        time);
                    SampleLayeredAnimator(
                        animator,
                        OneHandStateName,
                        time,
                        emptyClip.length,
                        adjustedClip.length);
                    MeasureFilteredPoseDifference(
                        CapturePose(emptyObject.transform),
                        CapturePose(target),
                        false,
                        out float bodyPositionDifference,
                        out float bodyRotationDifference);
                    bodyPositionMax = Mathf.Max(bodyPositionMax, bodyPositionDifference);
                    bodyRotationMax = Mathf.Max(bodyRotationMax, bodyRotationDifference);
                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(target.position, rootBaseline));

                    foreach (string path in unchangedRightPaths)
                    {
                        Transform expected = FindRequired(baselineObject.transform, path);
                        Transform actual = FindRequired(target, path);
                        rightUpperPositionMax = Mathf.Max(
                            rightUpperPositionMax,
                            Vector3.Distance(expected.localPosition, actual.localPosition));
                        rightUpperRotationMax = Mathf.Max(
                            rightUpperRotationMax,
                            Quaternion.Angle(expected.localRotation, actual.localRotation));
                    }

                    Transform actualSpine = FindRequired(target, SpinePath);
                    Transform actualHips = FindRequired(target, HipsPath);
                    Transform actualLeftForeArm = FindRequired(target, LeftForeArmPath);
                    Transform actualLeftHand = FindRequired(target, LeftHandPath);
                    Transform actualRightHand = FindRequired(target, RightHandPath);
                    Transform actualRightArm = FindRequired(target, RightArmPath);
                    Transform actualRightForeArm = FindRequired(target, RightForeArmPath);
                    Transform baselineRightHand = FindRequired(
                        baselineObject.transform,
                        RightHandPath);
                    Transform baselineRightForeArm = FindRequired(
                        baselineObject.transform,
                        RightForeArmPath);
                    leftForeArmClearanceMin = Mathf.Min(
                        leftForeArmClearanceMin,
                        Vector3.Dot(
                            actualLeftForeArm.position - actualSpine.position,
                            -target.right));
                    leftHandClearanceMin = Mathf.Min(
                        leftHandClearanceMin,
                        Vector3.Dot(
                            actualLeftHand.position - actualHips.position,
                            -target.right));
                    rightHandPositionMax = Mathf.Max(
                        rightHandPositionMax,
                        Vector3.Distance(
                            baselineRightHand.position,
                            actualRightHand.position));
                    rightElbowOutsideMin = Mathf.Min(
                        rightElbowOutsideMin,
                        Vector3.Dot(
                            actualRightForeArm.position - actualSpine.position,
                            target.right));
                    rightElbowBelowShoulderMin = Mathf.Min(
                        rightElbowBelowShoulderMin,
                        Vector3.Dot(
                            actualRightArm.position - actualRightForeArm.position,
                            target.up));
                    rightWristLocalRotationMax = Mathf.Max(
                        rightWristLocalRotationMax,
                        Quaternion.Angle(
                            baselineRightHand.localRotation,
                            actualRightHand.localRotation));
                    Vector3 actualForeArmDirection =
                        actualRightHand.position - actualRightForeArm.position;
                    verticalGripAngleMax = Mathf.Max(
                        verticalGripAngleMax,
                        Vector3.Angle(actualRightHand.forward, target.up));
                    Vector3 actualPalm = -actualRightHand.right;
                    Vector3 palm = Vector3.ProjectOnPlane(
                        actualPalm,
                        target.up);
                    Vector3 inward = Vector3.ProjectOnPlane(
                        actualSpine.position - actualRightHand.position,
                        target.up);
                    if (palm.sqrMagnitude < 0.0000001f || inward.sqrMagnitude < 0.0000001f)
                    {
                        throw new InvalidOperationException(
                            "OneHand grip review has no usable palm direction.");
                    }

                    palmInwardAngleMax = Mathf.Max(
                        palmInwardAngleMax,
                        Vector3.Angle(palm, inward));
                    palmFromInwardAngleMin = Mathf.Min(
                        palmFromInwardAngleMin,
                        Vector3.Angle(palm, inward));
                    Vector3 expectedPalm = palmFacingCharacterLeft
                        ? -target.right
                        : Quaternion.AngleAxis(
                            expectedGripTwistDegrees,
                            target.up) * inward;
                    palmTargetAngleMax = Mathf.Max(
                        palmTargetAngleMax,
                        Vector3.Angle(
                            palmFacingCharacterLeft ? actualPalm : palm,
                            expectedPalm));
                    Vector3 expectedForeArmDirection = palmFacingCharacterLeft
                        ? baselineRightHand.position - baselineRightForeArm.position
                        : Vector3.Cross(target.up, inward).normalized;
                    rightForeArmWristAlignmentMax = Mathf.Max(
                        rightForeArmWristAlignmentMax,
                        Vector3.Angle(
                            actualForeArmDirection,
                            expectedForeArmDirection));
                    ExpandBounds(
                        baselineObject.transform.InverseTransformPoint(
                            baselineRightHand.position),
                        ref sourceMin,
                        ref sourceMax);
                    ExpandBounds(
                        target.InverseTransformPoint(actualRightHand.position),
                        ref adjustedMin,
                        ref adjustedMax);
                }

                SampleLayeredAnimator(
                    animator,
                    OneHandStateName,
                    0f,
                    emptyClip.length,
                    adjustedClip.length);
                AnimatorStateInfo baseInfo = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo armInfo = animator.GetCurrentAnimatorStateInfo(1);
                return new GripClearanceReviewMetrics
                {
                    target = OneHandTargetName,
                    reviewDurationSeconds = reviewDuration,
                    framesSampled = framesSampled,
                    baseLoopsSampled = Mathf.FloorToInt(
                        reviewDuration / emptyClip.length + 0.0001f),
                    armLoopsSampled = Mathf.FloorToInt(
                        reviewDuration / adjustedClip.length + 0.0001f),
                    rootPositionDisplacementMax = rootMax,
                    bodyPositionDifferenceMax = bodyPositionMax,
                    bodyRotationDifferenceDegreesMax = bodyRotationMax,
                    leftForeArmOutsideSpineMetersMin = leftForeArmClearanceMin,
                    leftHandOutsideHipsMetersMin = leftHandClearanceMin,
                    rightShoulderArmForeArmPositionDifferenceMax = rightUpperPositionMax,
                    rightShoulderArmForeArmRotationDifferenceDegreesMax = rightUpperRotationMax,
                    rightHandPositionDifferenceMax = rightHandPositionMax,
                    rightElbowOutsideSpineMetersMin = rightElbowOutsideMin,
                    rightElbowBelowShoulderMetersMin = rightElbowBelowShoulderMin,
                    rightWristLocalRotationDifferenceDegreesMax = rightWristLocalRotationMax,
                    rightForeArmWristAlignmentDegreesMax = rightForeArmWristAlignmentMax,
                    verticalGripAngleDegreesMax = verticalGripAngleMax,
                    expectedGripTwistDegrees = expectedGripTwistDegrees,
                    palmFromInwardAngleDegreesMin = palmFromInwardAngleMin,
                    palmInwardAngleDegreesMax = palmInwardAngleMax,
                    palmTargetAngleDegreesMax = palmTargetAngleMax,
                    sourceRightHandMotionRange = Vector3.Distance(sourceMin, sourceMax),
                    adjustedRightHandMotionRange = Vector3.Distance(adjustedMin, adjustedMax),
                    baseStateLoops = baseInfo.loop,
                    armStateLoops = armInfo.loop,
                    applyRootMotion = animator.applyRootMotion,
                    naturalRightArmAdjustment = naturalRightArmAdjustment
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(emptyObject);
                UnityEngine.Object.DestroyImmediate(baselineObject);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static bool GripClearanceReviewPassed(
            GripClearanceReviewMetrics metrics,
            bool naturalRightArmAdjustment)
        {
            return metrics.framesSampled > 0 &&
                   metrics.baseLoopsSampled >= 2 &&
                   metrics.armLoopsSampled >= 2 &&
                   metrics.rootPositionDisplacementMax <= PositionTolerance &&
                   metrics.bodyPositionDifferenceMax <= PositionTolerance &&
                   metrics.bodyRotationDifferenceDegreesMax <= RotationTolerance &&
                   metrics.leftForeArmOutsideSpineMetersMin >= 0.15f &&
                   metrics.leftHandOutsideHipsMetersMin >= 0.28f &&
                   (naturalRightArmAdjustment
                       ? metrics.rightElbowOutsideSpineMetersMin >= 0.12f &&
                         metrics.rightElbowBelowShoulderMetersMin >= 0.03f &&
                         metrics.rightWristLocalRotationDifferenceDegreesMax <= 120f &&
                         metrics.rightForeArmWristAlignmentDegreesMax <= 10f
                       : metrics.rightShoulderArmForeArmPositionDifferenceMax <= 0.005f &&
                         metrics.rightShoulderArmForeArmRotationDifferenceDegreesMax <=
                             RotationTolerance) &&
                   metrics.rightHandPositionDifferenceMax <=
                       (naturalRightArmAdjustment ? 0.15f : 0.005f) &&
                   metrics.verticalGripAngleDegreesMax <= 8f &&
                   metrics.palmTargetAngleDegreesMax <= 12f &&
                   metrics.adjustedRightHandMotionRange + 0.0001f >=
                       metrics.sourceRightHandMotionRange *
                       (naturalRightArmAdjustment ? 0.7f : 0.8f) &&
                   metrics.baseStateLoops &&
                   metrics.armStateLoops &&
                   !metrics.applyRootMotion;
        }

        private static bool PalmLeftReviewPassed(
            GripClearanceReviewMetrics metrics)
        {
            return metrics.framesSampled > 0 &&
                   metrics.baseLoopsSampled >= 2 &&
                   metrics.armLoopsSampled >= 2 &&
                   metrics.rootPositionDisplacementMax <= PositionTolerance &&
                   metrics.bodyPositionDifferenceMax <= PositionTolerance &&
                   metrics.bodyRotationDifferenceDegreesMax <= RotationTolerance &&
                   metrics.leftForeArmOutsideSpineMetersMin >= 0.15f &&
                   metrics.leftHandOutsideHipsMetersMin >= 0.28f &&
                   metrics.rightHandPositionDifferenceMax <= 0.005f &&
                   metrics.rightForeArmWristAlignmentDegreesMax <= 1f &&
                   metrics.palmTargetAngleDegreesMax <= 3f &&
                   metrics.adjustedRightHandMotionRange + 0.0001f >=
                       metrics.sourceRightHandMotionRange * 0.95f &&
                   metrics.baseStateLoops &&
                   metrics.armStateLoops &&
                   !metrics.applyRootMotion;
        }

        private static void CaptureCarryPoseAdjustmentComparison(
            Transform target,
            AnimationClip emptyClip,
            AnimationClip sourceClip,
            AnimationClip adjustedClip,
            string stateName,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            GameObject sourceObject = UnityEngine.Object.Instantiate(target.gameObject);
            sourceObject.name = target.name + "PoseAdjustmentSourceReference";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            float reviewDuration = Mathf.Max(
                emptyClip.length * 2f,
                adjustedClip.length * 2f);
            float[] times = Enumerable.Range(0, 8)
                .Select(index => reviewDuration * index / 8f)
                .ToArray();
            CaptureEnvironment environment = new CaptureEnvironment(target);
            try
            {
                bool actualPalmReview = string.Equals(
                    outputPath,
                    ActualPalmInwardGripReviewPath,
                    StringComparison.Ordinal);
                bool palmLeftReview = string.Equals(
                    outputPath,
                    OneHandEmptyBodyPalmLeftReviewPath,
                    StringComparison.Ordinal);
                List<List<byte[]>> rows = Enumerable.Range(
                        0,
                        actualPalmReview || palmLeftReview ? 9 : 8)
                    .Select(_ => new List<byte[]>())
                    .ToList();
                foreach (float time in times)
                {
                    PrepareArmPose(
                        target,
                        sourceObject.transform,
                        emptyClip,
                        sourceClip,
                        time,
                        time);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    rows[0].Add(environment.CaptureFront());
                    rows[1].Add(environment.CaptureSide());
                    environment.ConfigureView(
                        target,
                        FindRequired(target, RightHandPath).position,
                        0.55f);
                    rows[4].Add(environment.CaptureFront());
                    rows[5].Add(environment.CaptureSide());

                    SampleLayeredAnimator(
                        animator,
                        stateName,
                        time,
                        emptyClip.length,
                        adjustedClip.length);
                    environment.ConfigureView(target, 1.05f, 1.35f);
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());
                    environment.ConfigureView(
                        target,
                        FindRequired(target, RightHandPath).position,
                        0.55f);
                    rows[6].Add(environment.CaptureFront());
                    rows[7].Add(environment.CaptureSide());
                    if (actualPalmReview || palmLeftReview)
                    {
                        Transform actualRightHand = FindRequired(target, RightHandPath);
                        Transform actualSpine = FindRequired(target, SpinePath);
                        environment.ConfigurePalmView(
                            target,
                            actualRightHand.position,
                            palmLeftReview
                                ? -target.right
                                : actualSpine.position - actualRightHand.position,
                            0.38f);
                        rows[8].Add(environment.CapturePalmFromTorso());
                    }
                }

                ComposeRows(rows, outputPath);
                if (string.Equals(
                    outputPath,
                    AnatomicalWristGripReviewPath,
                    StringComparison.Ordinal))
                {
                    File.WriteAllBytes(
                        Path.GetFullPath(AnatomicalWristGripReviewCloseFrontPath),
                        rows[6][0]);
                    File.WriteAllBytes(
                        Path.GetFullPath(AnatomicalWristGripReviewCloseSidePath),
                        rows[7][0]);
                }
                else if (actualPalmReview)
                {
                    File.WriteAllBytes(
                        Path.GetFullPath(ActualPalmInwardGripReviewCloseFrontPath),
                        rows[6][0]);
                    File.WriteAllBytes(
                        Path.GetFullPath(ActualPalmInwardGripReviewCloseSidePath),
                        rows[7][0]);
                    File.WriteAllBytes(
                        Path.GetFullPath(ActualPalmInwardGripReviewPalmFromTorsoPath),
                        rows[8][0]);
                }
                else if (palmLeftReview)
                {
                    File.WriteAllBytes(
                        Path.GetFullPath(OneHandEmptyBodyPalmLeftReviewCloseFrontPath),
                        rows[6][0]);
                    File.WriteAllBytes(
                        Path.GetFullPath(OneHandEmptyBodyPalmLeftReviewCloseSidePath),
                        rows[7][0]);
                    File.WriteAllBytes(
                        Path.GetFullPath(OneHandEmptyBodyPalmLeftReviewPalmPath),
                        rows[8][0]);
                }
            }
            finally
            {
                environment.Dispose();
                UnityEngine.Object.DestroyImmediate(sourceObject);
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static PoseAdjustmentTargetReviewMetrics
            CaptureCarryPoseAdjustmentMetrics(
                Transform target,
                AnimationClip emptyClip,
                AnimationClip sourceClip,
                AnimationClip adjustedClip,
                string stateName,
                CarryPoseAdjustmentKind kind)
        {
            Animator animator = RequireAnimator(target);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = target.position;
            float reviewDuration = Mathf.Max(
                emptyClip.length * 2f,
                adjustedClip.length * 2f);
            float sampleRate = Mathf.Max(emptyClip.frameRate, adjustedClip.frameRate);
            int framesSampled = Mathf.Max(
                8,
                Mathf.CeilToInt(reviewDuration * sampleRate));
            float rootMax = 0f;
            float bodyPositionMax = 0f;
            float bodyRotationMax = 0f;
            float belowShoulderMin = float.PositiveInfinity;
            float belowHipsMin = float.PositiveInfinity;
            float outsideHipsMin = float.PositiveInfinity;
            float rightChestMin = float.PositiveInfinity;
            float spacingDifferenceMax = 0f;
            Vector3 sourceMin = new Vector3(
                float.PositiveInfinity,
                float.PositiveInfinity,
                float.PositiveInfinity);
            Vector3 sourceMax = new Vector3(
                float.NegativeInfinity,
                float.NegativeInfinity,
                float.NegativeInfinity);
            Vector3 adjustedMin = sourceMin;
            Vector3 adjustedMax = sourceMax;
            GameObject emptyObject = UnityEngine.Object.Instantiate(target.gameObject);
            GameObject baselineObject = UnityEngine.Object.Instantiate(target.gameObject);
            GameObject sourceObject = UnityEngine.Object.Instantiate(target.gameObject);
            emptyObject.hideFlags = HideFlags.HideAndDontSave;
            baselineObject.hideFlags = HideFlags.HideAndDontSave;
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(emptyObject);
            DisableAnimators(baselineObject);
            DisableAnimators(sourceObject);
            try
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                for (int frame = 0; frame < framesSampled; frame++)
                {
                    float time = reviewDuration * frame / framesSampled;
                    emptyClip.SampleAnimation(
                        emptyObject,
                        Mathf.Repeat(time, emptyClip.length));
                    PrepareArmPose(
                        baselineObject.transform,
                        sourceObject.transform,
                        emptyClip,
                        sourceClip,
                        time,
                        time);
                    SampleLayeredAnimator(
                        animator,
                        stateName,
                        time,
                        emptyClip.length,
                        adjustedClip.length);
                    MeasureFilteredPoseDifference(
                        CapturePose(emptyObject.transform),
                        CapturePose(target),
                        false,
                        out float bodyPositionDifference,
                        out float bodyRotationDifference);
                    bodyPositionMax = Mathf.Max(bodyPositionMax, bodyPositionDifference);
                    bodyRotationMax = Mathf.Max(bodyRotationMax, bodyRotationDifference);
                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(target.position, rootBaseline));

                    Transform actualLeftHand = FindRequired(target, LeftHandPath);
                    Transform actualRightHand = FindRequired(target, RightHandPath);
                    Transform baselineLeftHand = FindRequired(
                        baselineObject.transform,
                        LeftHandPath);
                    Transform baselineRightHand = FindRequired(
                        baselineObject.transform,
                        RightHandPath);
                    Vector3 sourceTracked;
                    Vector3 adjustedTracked;
                    if (kind == CarryPoseAdjustmentKind.OneHandLeftArmDown)
                    {
                        Transform actualLeftArm = FindRequired(target, LeftArmPath);
                        Transform actualLeftForeArm = FindRequired(target, LeftForeArmPath);
                        Transform actualHips = FindRequired(target, HipsPath);
                        float armLength =
                            Vector3.Distance(actualLeftArm.position, actualLeftForeArm.position) +
                            Vector3.Distance(actualLeftForeArm.position, actualLeftHand.position);
                        belowShoulderMin = Mathf.Min(
                            belowShoulderMin,
                            Vector3.Dot(
                                actualLeftArm.position - actualLeftHand.position,
                                target.up) / armLength);
                        belowHipsMin = Mathf.Min(
                            belowHipsMin,
                            Vector3.Dot(
                                actualHips.position - actualLeftHand.position,
                                target.up));
                        outsideHipsMin = Mathf.Min(
                            outsideHipsMin,
                            Vector3.Dot(
                                actualLeftHand.position - actualHips.position,
                                -target.right));
                        sourceTracked = baselineObject.transform.InverseTransformPoint(
                            baselineLeftHand.position);
                        adjustedTracked = target.InverseTransformPoint(
                            actualLeftHand.position);
                    }
                    else
                    {
                        Transform actualSpine = FindRequired(target, SpinePath);
                        Transform actualLeftShoulder = FindRequired(target, LeftShoulderPath);
                        Transform actualRightShoulder = FindRequired(target, RightShoulderPath);
                        Vector3 actualCenter =
                            (actualLeftHand.position + actualRightHand.position) * 0.5f;
                        Vector3 baselineCenter =
                            (baselineLeftHand.position + baselineRightHand.position) * 0.5f;
                        float shoulderSpan = Vector3.Distance(
                            actualLeftShoulder.position,
                            actualRightShoulder.position);
                        rightChestMin = Mathf.Min(
                            rightChestMin,
                            Vector3.Dot(
                                actualCenter - actualSpine.position,
                                target.right) / shoulderSpan);
                        spacingDifferenceMax = Mathf.Max(
                            spacingDifferenceMax,
                            Mathf.Abs(
                                Vector3.Distance(
                                    actualLeftHand.position,
                                    actualRightHand.position) -
                                Vector3.Distance(
                                    baselineLeftHand.position,
                                    baselineRightHand.position)));
                        sourceTracked = baselineObject.transform.InverseTransformPoint(
                            baselineCenter);
                        adjustedTracked = target.InverseTransformPoint(actualCenter);
                    }

                    ExpandBounds(sourceTracked, ref sourceMin, ref sourceMax);
                    ExpandBounds(adjustedTracked, ref adjustedMin, ref adjustedMax);
                }

                SampleLayeredAnimator(
                    animator,
                    stateName,
                    0f,
                    emptyClip.length,
                    adjustedClip.length);
                AnimatorStateInfo baseInfo = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo armInfo = animator.GetCurrentAnimatorStateInfo(1);
                return new PoseAdjustmentTargetReviewMetrics
                {
                    target = target.name,
                    adjustment = kind == CarryPoseAdjustmentKind.OneHandLeftArmDown
                        ? "왼팔 전체 자연스러운 하강"
                        : "양손 간격 유지 및 오른쪽 가슴 배치",
                    reviewDurationSeconds = reviewDuration,
                    framesSampled = framesSampled,
                    baseLoopsSampled = Mathf.FloorToInt(
                        reviewDuration / emptyClip.length + 0.0001f),
                    armLoopsSampled = Mathf.FloorToInt(
                        reviewDuration / adjustedClip.length + 0.0001f),
                    rootPositionDisplacementMax = rootMax,
                    bodyPositionDifferenceMax = bodyPositionMax,
                    bodyRotationDifferenceDegreesMax = bodyRotationMax,
                    leftHandBelowShoulderArmLengthsMin =
                        float.IsPositiveInfinity(belowShoulderMin) ? 0f : belowShoulderMin,
                    leftHandBelowHipsMetersMin =
                        float.IsPositiveInfinity(belowHipsMin) ? 0f : belowHipsMin,
                    leftHandOutsideHipsMetersMin =
                        float.IsPositiveInfinity(outsideHipsMin) ? 0f : outsideHipsMin,
                    handCenterRightShoulderSpansMin =
                        float.IsPositiveInfinity(rightChestMin) ? 0f : rightChestMin,
                    handSpacingDifferenceMax = spacingDifferenceMax,
                    sourceHandMotionRange = Vector3.Distance(sourceMin, sourceMax),
                    adjustedHandMotionRange = Vector3.Distance(adjustedMin, adjustedMax),
                    baseStateLoops = baseInfo.loop,
                    armStateLoops = armInfo.loop,
                    applyRootMotion = animator.applyRootMotion
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(emptyObject);
                UnityEngine.Object.DestroyImmediate(baselineObject);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static bool PoseAdjustmentTargetReviewPassed(
            PoseAdjustmentTargetReviewMetrics metrics,
            CarryPoseAdjustmentKind kind)
        {
            bool common =
                metrics.framesSampled > 0 &&
                metrics.baseLoopsSampled >= 2 &&
                metrics.armLoopsSampled >= 2 &&
                metrics.rootPositionDisplacementMax <= PositionTolerance &&
                metrics.bodyPositionDifferenceMax <= PositionTolerance &&
                metrics.bodyRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.adjustedHandMotionRange + 0.0001f >=
                    metrics.sourceHandMotionRange * 0.2f &&
                metrics.baseStateLoops &&
                metrics.armStateLoops &&
                !metrics.applyRootMotion;
            if (!common)
            {
                return false;
            }

            return kind == CarryPoseAdjustmentKind.OneHandLeftArmDown
                ? metrics.leftHandBelowShoulderArmLengthsMin >= 0.72f &&
                  metrics.leftHandBelowHipsMetersMin >= -0.12f &&
                  metrics.leftHandOutsideHipsMetersMin >= -0.08f
                : metrics.handCenterRightShoulderSpansMin >= 0.18f &&
                  metrics.handSpacingDifferenceMax <= 0.015f;
        }

        private static void ExpandBounds(
            Vector3 value,
            ref Vector3 minimum,
            ref Vector3 maximum)
        {
            minimum = Vector3.Min(minimum, value);
            maximum = Vector3.Max(maximum, value);
        }

        private static void CaptureCarryAlignmentComparison(
            Transform target,
            AnimationClip emptyClip,
            AnimationClip armClip,
            string armStateName,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            float reviewDuration = Mathf.Max(
                emptyClip.length * 2f,
                armClip.length * 2f);
            float[] times = Enumerable.Range(0, 8)
                .Select(index => reviewDuration * index / 8f)
                .ToArray();
            CaptureEnvironment environment = new CaptureEnvironment(target);
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
                    emptyClip.SampleAnimation(
                        target.gameObject,
                        Mathf.Repeat(time, emptyClip.length));
                    rows[0].Add(environment.CaptureFront());
                    rows[1].Add(environment.CaptureSide());
                    armClip.SampleAnimation(
                        target.gameObject,
                        Mathf.Repeat(time, armClip.length));
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());
                    SampleLayeredAnimator(
                        animator,
                        armStateName,
                        time,
                        emptyClip.length,
                        armClip.length);
                    rows[4].Add(environment.CaptureFront());
                    rows[5].Add(environment.CaptureSide());
                }

                ComposeRows(rows, outputPath);
            }
            finally
            {
                environment.Dispose();
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static AlignmentTargetReviewMetrics CaptureCarryAlignmentMetrics(
            Transform target,
            AnimationClip emptyClip,
            AnimationClip armClip,
            string armStateName)
        {
            Animator animator = RequireAnimator(target);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = target.position;
            float rootMax = 0f;
            float bodyPositionMax = 0f;
            float bodyRotationMax = 0f;
            float armPositionMax = 0f;
            float armRotationMax = 0f;
            float reviewDuration = Mathf.Max(
                emptyClip.length * 2f,
                armClip.length * 2f);
            float sampleRate = Mathf.Max(emptyClip.frameRate, armClip.frameRate);
            int framesSampled = Mathf.Max(
                8,
                Mathf.CeilToInt(reviewDuration * sampleRate));
            GameObject emptyClone = UnityEngine.Object.Instantiate(target.gameObject);
            emptyClone.name = target.name + "EmptyBodyReferenceClone";
            emptyClone.hideFlags = HideFlags.HideAndDontSave;
            GameObject armClone = UnityEngine.Object.Instantiate(target.gameObject);
            armClone.name = target.name + "CarryArmsReferenceClone";
            armClone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (Animator cloneAnimator in emptyClone
                             .GetComponentsInChildren<Animator>(true)
                             .Concat(armClone.GetComponentsInChildren<Animator>(true)))
                {
                    cloneAnimator.enabled = false;
                }

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                for (int frame = 0; frame < framesSampled; frame++)
                {
                    float time = reviewDuration * frame / framesSampled;
                    emptyClip.SampleAnimation(
                        emptyClone,
                        Mathf.Repeat(time, emptyClip.length));
                    armClip.SampleAnimation(
                        armClone,
                        Mathf.Repeat(time, armClip.length));
                    SampleLayeredAnimator(
                        animator,
                        armStateName,
                        time,
                        emptyClip.length,
                        armClip.length);
                    PoseSnapshot emptyPose = CapturePose(emptyClone.transform);
                    PoseSnapshot armPose = CapturePose(armClone.transform);
                    PoseSnapshot appliedPose = CapturePose(target);
                    MeasureFilteredPoseDifference(
                        emptyPose,
                        appliedPose,
                        false,
                        out float bodyPositionDifference,
                        out float bodyRotationDifference);
                    MeasureFilteredPoseDifference(
                        armPose,
                        appliedPose,
                        true,
                        out float armPositionDifference,
                        out float armRotationDifference);
                    bodyPositionMax = Mathf.Max(
                        bodyPositionMax,
                        bodyPositionDifference);
                    bodyRotationMax = Mathf.Max(
                        bodyRotationMax,
                        bodyRotationDifference);
                    armPositionMax = Mathf.Max(
                        armPositionMax,
                        armPositionDifference);
                    armRotationMax = Mathf.Max(
                        armRotationMax,
                        armRotationDifference);
                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(target.position, rootBaseline));
                }

                SampleLayeredAnimator(
                    animator,
                    armStateName,
                    0f,
                    emptyClip.length,
                    armClip.length);
                AnimatorStateInfo baseInfo = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo armInfo = animator.GetCurrentAnimatorStateInfo(1);
                return new AlignmentTargetReviewMetrics
                {
                    target = target.name,
                    baseState = AlignmentBaseStateName,
                    armState = armStateName,
                    armTake = armClip.name,
                    baseDurationSeconds = emptyClip.length,
                    armDurationSeconds = armClip.length,
                    reviewDurationSeconds = reviewDuration,
                    framesSampled = framesSampled,
                    baseLoopsSampled = Mathf.FloorToInt(
                        reviewDuration / emptyClip.length + 0.0001f),
                    armLoopsSampled = Mathf.FloorToInt(
                        reviewDuration / armClip.length + 0.0001f),
                    rootPositionDisplacementMax = rootMax,
                    bodyPositionDifferenceMax = bodyPositionMax,
                    bodyRotationDifferenceDegreesMax = bodyRotationMax,
                    armPositionDifferenceMax = armPositionMax,
                    armRotationDifferenceDegreesMax = armRotationMax,
                    baseStateLoops = baseInfo.loop,
                    armStateLoops = armInfo.loop,
                    applyRootMotion = animator.applyRootMotion
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(emptyClone);
                UnityEngine.Object.DestroyImmediate(armClone);
                animator.speed = originalSpeed;
                animator.cullingMode = originalCulling;
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static bool AlignmentTargetReviewPassed(
            AlignmentTargetReviewMetrics metrics)
        {
            return metrics.framesSampled > 0 &&
                   metrics.baseLoopsSampled >= 2 &&
                   metrics.armLoopsSampled >= 2 &&
                   metrics.rootPositionDisplacementMax <= PositionTolerance &&
                   metrics.bodyPositionDifferenceMax <= PositionTolerance &&
                   metrics.bodyRotationDifferenceDegreesMax <= RotationTolerance &&
                   metrics.armRotationDifferenceDegreesMax <= RotationTolerance &&
                   metrics.baseStateLoops &&
                   metrics.armStateLoops &&
                   !metrics.applyRootMotion;
        }

        private static void SampleLayeredAnimator(
            Animator animator,
            string armStateName,
            float time,
            float baseDuration,
            float armDuration)
        {
            if (animator.layerCount != 2)
            {
                throw new InvalidOperationException(
                    animator.name + " must have exactly two carry alignment layers.");
            }

            int baseHash = Animator.StringToHash(AlignmentBaseStateName);
            int armHash = Animator.StringToHash(armStateName);
            animator.Rebind();
            animator.Update(0f);
            animator.SetLayerWeight(1, 1f);
            animator.Play(
                baseHash,
                0,
                Mathf.Repeat(time, baseDuration) / baseDuration);
            animator.Play(
                armHash,
                1,
                Mathf.Repeat(time, armDuration) / armDuration);
            animator.Update(0f);
            AnimatorStateInfo baseInfo = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo armInfo = animator.GetCurrentAnimatorStateInfo(1);
            if (!baseInfo.IsName(AlignmentBaseStateName) ||
                !armInfo.IsName(armStateName))
            {
                throw new InvalidOperationException(
                    animator.name + " did not enter both expected carry alignment states.");
            }
        }

        private static void MeasureFilteredPoseDifference(
            PoseSnapshot first,
            PoseSnapshot second,
            bool armPaths,
            out float positionMax,
            out float rotationMax)
        {
            positionMax = 0f;
            rotationMax = 0f;
            string[] paths = first.Positions.Keys
                .Where(path =>
                    (string.Equals(path, "Armature", StringComparison.Ordinal) ||
                     path.StartsWith("Armature/", StringComparison.Ordinal)) &&
                    IsArmTransformPath(path) == armPaths)
                .ToArray();
            if (paths.Length == 0)
            {
                throw new InvalidOperationException(
                    armPaths
                        ? "Player Hands pose has no arm transforms to compare."
                        : "Player Hands pose has no body transforms to compare.");
            }

            foreach (string path in paths)
            {
                if (!second.Positions.TryGetValue(path, out Vector3 secondPosition) ||
                    !first.Rotations.TryGetValue(path, out Quaternion firstRotation) ||
                    !second.Rotations.TryGetValue(path, out Quaternion secondRotation))
                {
                    throw new InvalidOperationException(
                        "Player Hands hierarchy changed during filtered review at " + path + ".");
                }

                positionMax = Mathf.Max(
                    positionMax,
                    Vector3.Distance(first.Positions[path], secondPosition));
                rotationMax = Mathf.Max(
                    rotationMax,
                    Quaternion.Angle(firstRotation, secondRotation));
            }
        }

        private static float[] CaptureBlendShapeWeights(
            SkinnedMeshRenderer renderer)
        {
            int count = renderer.sharedMesh != null
                ? renderer.sharedMesh.blendShapeCount
                : 0;
            float[] weights = new float[count];
            for (int index = 0; index < count; index++)
            {
                weights[index] = renderer.GetBlendShapeWeight(index);
            }

            return weights;
        }

        private static void RevertDrawBackRendererToPrefabSource(
            SkinnedMeshRenderer renderer)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(renderer))
            {
                throw new InvalidOperationException(
                    "Hands_Draw_Back primary renderer is not a prefab instance.");
            }

            SerializedObject serializedRenderer = new SerializedObject(renderer);
            SerializedProperty meshProperty =
                serializedRenderer.FindProperty("m_Mesh");
            SerializedProperty blendShapeProperty =
                serializedRenderer.FindProperty("m_BlendShapeWeights");
            if (meshProperty == null || blendShapeProperty == null)
            {
                throw new InvalidOperationException(
                    "Hands_Draw_Back renderer mesh properties are unavailable.");
            }

            if (meshProperty.prefabOverride)
            {
                PrefabUtility.RevertPropertyOverride(
                    meshProperty,
                    InteractionMode.AutomatedAction);
            }

            serializedRenderer.Update();
            blendShapeProperty =
                serializedRenderer.FindProperty("m_BlendShapeWeights");
            if (blendShapeProperty.prefabOverride)
            {
                PrefabUtility.RevertPropertyOverride(
                    blendShapeProperty,
                    InteractionMode.AutomatedAction);
            }

            GameObject prefabRoot =
                PrefabUtility.GetOutermostPrefabInstanceRoot(
                    renderer.gameObject);
            UnityEngine.Object sourceRenderer =
                PrefabUtility.GetCorrespondingObjectFromSource(renderer);
            if (prefabRoot == null || sourceRenderer == null)
            {
                throw new InvalidOperationException(
                    "Hands_Draw_Back renderer prefab source is unavailable.");
            }

            PropertyModification[] modifications =
                PrefabUtility.GetPropertyModifications(prefabRoot) ??
                Array.Empty<PropertyModification>();
            PropertyModification[] retained = modifications
                .Where(modification =>
                    modification.target != sourceRenderer ||
                    (!string.Equals(
                         modification.propertyPath,
                         "m_Mesh",
                         StringComparison.Ordinal) &&
                     !modification.propertyPath.StartsWith(
                         "m_BlendShapeWeights",
                         StringComparison.Ordinal)))
                .ToArray();
            if (retained.Length != modifications.Length)
            {
                PrefabUtility.SetPropertyModifications(prefabRoot, retained);
            }

            serializedRenderer.Update();
            if (HasPrefabPropertyOverride(renderer, "m_Mesh") ||
                HasPrefabPropertyOverride(
                    renderer,
                    "m_BlendShapeWeights"))
            {
                throw new InvalidOperationException(
                    "Hands_Draw_Back mesh or BlendShape prefab overrides remain after revert.");
            }
        }

        private static bool HasPrefabPropertyOverride(
            SkinnedMeshRenderer renderer,
            string propertyPathPrefix)
        {
            GameObject prefabRoot =
                PrefabUtility.GetOutermostPrefabInstanceRoot(
                    renderer.gameObject);
            UnityEngine.Object sourceRenderer =
                PrefabUtility.GetCorrespondingObjectFromSource(renderer);
            if (prefabRoot == null || sourceRenderer == null)
            {
                return false;
            }

            PropertyModification[] modifications =
                PrefabUtility.GetPropertyModifications(prefabRoot);
            return modifications != null &&
                   modifications.Any(modification =>
                       modification.target == sourceRenderer &&
                       (string.Equals(
                            modification.propertyPath,
                            propertyPathPrefix,
                            StringComparison.Ordinal) ||
                        modification.propertyPath.StartsWith(
                            propertyPathPrefix + ".",
                            StringComparison.Ordinal)));
        }

        private static bool RendererConfigurationMatches(
            SkinnedMeshRenderer first,
            Transform firstRoot,
            SkinnedMeshRenderer second,
            Transform secondRoot)
        {
            if (first.sharedMesh == null ||
                second.sharedMesh == null ||
                first.sharedMesh != second.sharedMesh ||
                !first.sharedMaterials.SequenceEqual(second.sharedMaterials) ||
                first.bones.Length != second.bones.Length ||
                first.quality != second.quality ||
                first.updateWhenOffscreen != second.updateWhenOffscreen ||
                first.skinnedMotionVectors != second.skinnedMotionVectors ||
                first.enabled != second.enabled ||
                first.shadowCastingMode != second.shadowCastingMode ||
                first.receiveShadows != second.receiveShadows ||
                Vector3.Distance(
                    first.localBounds.center,
                    second.localBounds.center) > PositionTolerance ||
                Vector3.Distance(
                    first.localBounds.extents,
                    second.localBounds.extents) > PositionTolerance)
            {
                return false;
            }

            string firstRendererPath = AnimationUtility.CalculateTransformPath(
                first.transform,
                firstRoot);
            string secondRendererPath = AnimationUtility.CalculateTransformPath(
                second.transform,
                secondRoot);
            if (!string.Equals(
                    firstRendererPath,
                    secondRendererPath,
                    StringComparison.Ordinal))
            {
                return false;
            }

            string firstRootBonePath = first.rootBone != null
                ? AnimationUtility.CalculateTransformPath(
                    first.rootBone,
                    firstRoot)
                : string.Empty;
            string secondRootBonePath = second.rootBone != null
                ? AnimationUtility.CalculateTransformPath(
                    second.rootBone,
                    secondRoot)
                : string.Empty;
            if (!string.Equals(
                    firstRootBonePath,
                    secondRootBonePath,
                    StringComparison.Ordinal))
            {
                return false;
            }

            for (int index = 0; index < first.bones.Length; index++)
            {
                string firstBonePath = AnimationUtility.CalculateTransformPath(
                    first.bones[index],
                    firstRoot);
                string secondBonePath = AnimationUtility.CalculateTransformPath(
                    second.bones[index],
                    secondRoot);
                if (!string.Equals(
                        firstBonePath,
                        secondBonePath,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return first.sharedMesh.vertexCount == second.sharedMesh.vertexCount &&
                   first.sharedMesh.subMeshCount == second.sharedMesh.subMeshCount &&
                   first.sharedMesh.bindposes.Length == second.sharedMesh.bindposes.Length &&
                   first.sharedMesh.blendShapeCount == second.sharedMesh.blendShapeCount;
        }

        private static bool SceneDependsOnAsset(string assetPath)
        {
            return AssetDatabase.GetDependencies(ScenePath, true)
                .Any(path => string.Equals(
                    path,
                    assetPath,
                    StringComparison.Ordinal));
        }

        private static bool HasNoBlendShapeCurves(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .All(binding =>
                    !binding.propertyName.StartsWith(
                        "blendShape.",
                        StringComparison.Ordinal));
        }

        private static void CaptureTargetComparison(
            Transform target,
            AnimationClip source,
            string stateName,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            float[] phases = { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f };
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
                    source.SampleAnimation(target.gameObject, phase * source.length);
                    rows[0].Add(environment.CaptureFront());
                    rows[1].Add(environment.CaptureSide());
                    SampleAnimator(animator, stateName, phase);
                    rows[2].Add(environment.CaptureFront());
                    rows[3].Add(environment.CaptureSide());
                }

                ComposeRows(rows, outputPath);
            }
            finally
            {
                environment.Dispose();
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static TargetReviewMetrics CaptureTargetMetrics(
            Transform target,
            AnimationClip source,
            string stateName,
            string sourceTake)
        {
            Animator animator = RequireAnimator(target);
            AnimatorCullingMode originalCulling = animator.cullingMode;
            float originalSpeed = animator.speed;
            Vector3 rootBaseline = target.position;
            float rootMax = 0f;
            float positionMax = 0f;
            float rotationMax = 0f;
            GameObject sourceClone = UnityEngine.Object.Instantiate(target.gameObject);
            sourceClone.name = target.name + "SourcePoseClone";
            sourceClone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (Animator cloneAnimator in
                         sourceClone.GetComponentsInChildren<Animator>(true))
                {
                    cloneAnimator.enabled = false;
                }

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                animator.Rebind();
                animator.Update(0f);
                int framesPerLoop = Mathf.Max(
                    4,
                    Mathf.CeilToInt(source.length * source.frameRate));
                for (int frame = 0; frame < framesPerLoop * 2; frame++)
                {
                    float normalizedTime = frame / (float)framesPerLoop;
                    float phase = normalizedTime - Mathf.Floor(normalizedTime);
                    source.SampleAnimation(sourceClone, phase * source.length);
                    PoseSnapshot sourcePose = CapturePose(sourceClone.transform);
                    SampleAnimator(animator, stateName, normalizedTime);
                    PoseSnapshot appliedPose = CapturePose(target);
                    MeasureArmaturePoseDifference(
                        sourcePose,
                        appliedPose,
                        out float positionDifference,
                        out float rotationDifference);
                    positionMax = Mathf.Max(positionMax, positionDifference);
                    rotationMax = Mathf.Max(rotationMax, rotationDifference);
                    rootMax = Mathf.Max(
                        rootMax,
                        Vector3.Distance(target.position, rootBaseline));
                }

                SampleAnimator(animator, stateName, 0f);
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                return new TargetReviewMetrics
                {
                    target = target.name,
                    state = stateName,
                    sourceTake = sourceTake,
                    durationSeconds = source.length,
                    framesPerLoop = framesPerLoop,
                    framesSampled = framesPerLoop * 2,
                    loopsSampled = 2,
                    rootPositionDisplacementMax = rootMax,
                    sourcePosePositionDifferenceMax = positionMax,
                    sourcePoseRotationDifferenceDegreesMax = rotationMax,
                    stateLoops = info.loop,
                    applyRootMotion = animator.applyRootMotion
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceClone);
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

        private static PoseSnapshot CapturePose(Transform root)
        {
            PoseSnapshot pose = new PoseSnapshot();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(item, root);
                pose.Positions[path] = item.localPosition;
                pose.Rotations[path] = item.localRotation;
            }

            return pose;
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
                    "Player Hands pose hierarchy has no Armature transforms to compare.");
            }

            foreach (string path in paths)
            {
                if (!second.Positions.TryGetValue(path, out Vector3 secondPosition) ||
                    !first.Rotations.TryGetValue(path, out Quaternion firstRotation) ||
                    !second.Rotations.TryGetValue(path, out Quaternion secondRotation))
                {
                    throw new InvalidOperationException(
                        "Player Hands Armature hierarchy changed during review at " + path + ".");
                }

                positionMax = Mathf.Max(
                    positionMax,
                    Vector3.Distance(first.Positions[path], secondPosition));
                rotationMax = Mathf.Max(
                    rotationMax,
                    Quaternion.Angle(firstRotation, secondRotation));
            }
        }

        private sealed class AnimationClipPoseSampler : IDisposable
        {
            private readonly Animator animator;
            private readonly bool animatorEnabled;
            private readonly PlayableGraph graph;
            private readonly AnimationClipPlayable playable;

            internal AnimationClipPoseSampler(
                Animator targetAnimator,
                AnimationClip clip)
            {
                animator = targetAnimator;
                animatorEnabled = animator.enabled;
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
                graph = PlayableGraph.Create(
                    targetAnimator.name + "ClipPoseReview");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false);
                playable.SetApplyPlayableIK(false);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                    graph,
                    "ClipPose",
                    animator);
                output.SetSourcePlayable(playable);
                graph.Play();
            }

            internal void Sample(float time)
            {
                playable.SetTime(time);
                graph.Evaluate(0f);
            }

            public void Dispose()
            {
                if (graph.IsValid())
                {
                    graph.Destroy();
                }

                animator.enabled = animatorEnabled;
            }
        }

        private sealed class CaptureEnvironment : IDisposable
        {
            private readonly RendererState[] hiddenRenderers;
            private readonly GameObject frontCameraObject;
            private readonly GameObject sideCameraObject;
            private readonly GameObject palmCameraObject;
            private readonly GameObject lightObject;
            private readonly RenderTexture renderTexture;
            private readonly Texture2D frameTexture;
            private readonly RenderTexture previousActive;

            internal CaptureEnvironment(Transform target)
            {
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
                palmCameraObject = CreateCameraObject(target.name + "PalmCamera");
                ConfigureView(target, 1.05f, 1.35f);
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

            internal byte[] CapturePalmFromTorso()
            {
                return CaptureFrame(palmCameraObject.GetComponent<Camera>());
            }

            internal void ConfigurePalmView(
                Transform target,
                Vector3 center,
                Vector3 torsoToHandViewDirection,
                float orthographicSize)
            {
                Vector3 direction = Vector3.ProjectOnPlane(
                    torsoToHandViewDirection,
                    Vector3.up).normalized;
                if (direction.sqrMagnitude < 0.99f)
                {
                    throw new InvalidOperationException(
                        target.name + " has no usable torso-side palm review direction.");
                }

                Camera camera = palmCameraObject.GetComponent<Camera>();
                camera.transform.position = center + direction * 0.12f;
                camera.transform.LookAt(center, target.up);
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.005f;
                camera.farClipPlane = 4f;
            }

            internal void ConfigureView(
                Transform target,
                float centerHeight,
                float orthographicSize)
            {
                Vector3 center = target.position + target.up * centerHeight;
                ConfigureView(target, center, orthographicSize);
            }

            internal void ConfigureView(
                Transform target,
                Vector3 center,
                float orthographicSize)
            {
                ConfigureFixedCamera(
                    frontCameraObject.GetComponent<Camera>(),
                    target,
                    center,
                    target.forward,
                    orthographicSize);
                ConfigureFixedCamera(
                    sideCameraObject.GetComponent<Camera>(),
                    target,
                    center,
                    target.right,
                    orthographicSize);
            }

            internal void ConfigureElevatedView(
                Transform target,
                Vector3 center,
                float orthographicSize)
            {
                ConfigureElevatedCamera(
                    frontCameraObject.GetComponent<Camera>(),
                    target,
                    center,
                    target.forward,
                    orthographicSize);
                ConfigureElevatedCamera(
                    sideCameraObject.GetComponent<Camera>(),
                    target,
                    center,
                    target.right,
                    orthographicSize);
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
                UnityEngine.Object.DestroyImmediate(palmCameraObject);
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

        private static void ConfigureElevatedCamera(
            Camera camera,
            Transform target,
            Vector3 center,
            Vector3 viewDirection,
            float orthographicSize)
        {
            Vector3 direction = Vector3.ProjectOnPlane(
                viewDirection,
                Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    target.name + " has no usable elevated review direction.");
            }

            camera.transform.position =
                center + direction * 8f + target.up * 1.15f;
            camera.transform.LookAt(center, target.up);
            camera.orthographicSize = orthographicSize;
        }

        private static void MeasureThrowSourcePeak(
            Transform template,
            AnimationClip source,
            out int frameIntervals,
            out int peakFrame,
            out int peakCandidateCount)
        {
            GameObject sourceObject = UnityEngine.Object.Instantiate(template.gameObject);
            sourceObject.name = "HandsThrowPeakMeasure";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            try
            {
                frameIntervals = Mathf.Max(
                    1,
                    Mathf.RoundToInt(source.length * source.frameRate));
                List<float> heights = new List<float>();
                for (int frame = 0; frame <= frameIntervals; frame++)
                {
                    float time = Mathf.Min(
                        source.length,
                        frame / source.frameRate);
                    source.SampleAnimation(sourceObject, time);
                    Transform rightHand = FindRequired(
                        sourceObject.transform,
                        RightHandPath);
                    heights.Add(Vector3.Dot(
                        rightHand.position - sourceObject.transform.position,
                        sourceObject.transform.up));
                }

                float peakHeight = heights.Max();
                peakFrame = heights.IndexOf(peakHeight);
                peakCandidateCount = heights.Count(height =>
                    Mathf.Abs(height - peakHeight) <= PositionTolerance);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
            }
        }

        private static void MeasureThrowSourceHeadHeightFrame(
            Transform template,
            AnimationClip source,
            out int frameIntervals,
            out int readyEndFrame,
            out float previousRightHandMinusHeadHeight,
            out float rightHandHeight,
            out float headHeight)
        {
            GameObject sourceObject = UnityEngine.Object.Instantiate(template.gameObject);
            sourceObject.name = "HandsThrowHeadHeightMeasure";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            try
            {
                frameIntervals = Mathf.Max(
                    1,
                    Mathf.RoundToInt(source.length * source.frameRate));
                readyEndFrame = -1;
                previousRightHandMinusHeadHeight = float.NaN;
                rightHandHeight = float.NaN;
                headHeight = float.NaN;
                float previousDifference = float.NaN;
                for (int frame = 0; frame <= frameIntervals; frame++)
                {
                    float time = Mathf.Min(
                        source.length,
                        frame / source.frameRate);
                    source.SampleAnimation(sourceObject, time);
                    Transform rightHand = FindRequired(
                        sourceObject.transform,
                        RightHandPath);
                    Transform head = FindRequired(
                        sourceObject.transform,
                        HeadPath);
                    float sampledRightHandHeight = Vector3.Dot(
                        rightHand.position - sourceObject.transform.position,
                        sourceObject.transform.up);
                    float sampledHeadHeight = Vector3.Dot(
                        head.position - sourceObject.transform.position,
                        sourceObject.transform.up);
                    float difference =
                        sampledRightHandHeight - sampledHeadHeight;
                    if (frame > 0 &&
                        previousDifference < 0f &&
                        difference >= 0f)
                    {
                        readyEndFrame = frame;
                        previousRightHandMinusHeadHeight = previousDifference;
                        rightHandHeight = sampledRightHandHeight;
                        headHeight = sampledHeadHeight;
                        break;
                    }

                    previousDifference = difference;
                }

                if (readyEndFrame < 0)
                {
                    throw new InvalidOperationException(
                        "Hands Throw source has no rising frame where the right hand first reaches Head height.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
            }
        }

        private static ThrowBreathingMeshBuildResult
            CreateOrUpdateThrowReadyBreathingMesh(
                Transform template,
                SkinnedMeshRenderer templateRenderer,
                AnimationClip baseClip,
                float readyEndTime,
                float expansionAtAnimatedWeight,
                float animatedWeight)
        {
            Mesh originalMesh = AssetDatabase.LoadAllAssetsAtPath(PlayerModelPath)
                .OfType<Mesh>()
                .OrderByDescending(mesh => mesh.vertexCount)
                .FirstOrDefault();
            if (originalMesh == null || originalMesh.vertexCount == 0)
            {
                throw new InvalidOperationException(
                    "Player FBX has no usable source mesh for Ready breathing.");
            }

            if (animatedWeight <= 0f || animatedWeight > 100f)
            {
                throw new InvalidOperationException(
                    "Ready breathing animated BlendShape weight is invalid.");
            }

            string rendererPath = AnimationUtility.CalculateTransformPath(
                templateRenderer.transform,
                template);
            GameObject workObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            workObject.name = "HandsThrowReadyBreathingMeshBuild";
            workObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(workObject);
            Mesh baseBake = new Mesh { name = "HandsThrowReadyBreathingBaseBake" };
            Mesh expandedBake = new Mesh { name = "HandsThrowReadyBreathingExpandedBake" };
            try
            {
                SkinnedMeshRenderer renderer = RequireRelativeSkinnedMeshRenderer(
                    workObject.transform,
                    rendererPath);
                renderer.sharedMesh = originalMesh;
                baseClip.SampleAnimation(workObject, readyEndTime);
                renderer.BakeMesh(baseBake, true);
                Vector3[] bakedVertices = baseBake.vertices;
                BoneWeight[] boneWeights = originalMesh.boneWeights;
                Matrix4x4[] bindPoses = originalMesh.bindposes;
                if (bakedVertices.Length != originalMesh.vertexCount ||
                    boneWeights.Length != originalMesh.vertexCount ||
                    bindPoses.Length != renderer.bones.Length)
                {
                    throw new InvalidOperationException(
                        "Ready breathing source mesh skinning data is unsupported.");
                }

                Transform root = workObject.transform;
                Transform spine = FindRequired(root, SpinePath);
                Transform solar = FindRequired(root, SolarPlexusPath);
                Transform leftShoulder = FindRequired(root, LeftShoulderPath);
                Transform rightShoulder = FindRequired(root, RightShoulderPath);
                float lowerHeight = Vector3.Dot(
                    solar.position - root.position,
                    root.up);
                float upperHeight = Vector3.Dot(
                    ((leftShoulder.position + rightShoulder.position) * 0.5f) -
                    root.position,
                    root.up);
                if (upperHeight <= lowerHeight)
                {
                    throw new InvalidOperationException(
                        "Ready breathing chest vertical range is invalid.");
                }

                Vector3[] worldDirections = new Vector3[originalMesh.vertexCount];
                float[] rawFades = new float[originalMesh.vertexCount];
                int[] regions = new int[originalMesh.vertexCount];
                float maximumRawFade = 0f;
                for (int vertex = 0; vertex < originalMesh.vertexCount; vertex++)
                {
                    string dominantBone = DominantBoneName(
                        boneWeights[vertex],
                        renderer.bones);
                    if (!string.Equals(dominantBone, "Spine02", StringComparison.Ordinal) &&
                        !string.Equals(dominantBone, "Spine01", StringComparison.Ordinal) &&
                        !string.Equals(dominantBone, "Spine", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Vector3 world = renderer.transform.TransformPoint(
                        bakedVertices[vertex]);
                    float height = Vector3.Dot(
                        world - root.position,
                        root.up);
                    if (height <= lowerHeight || height >= upperHeight)
                    {
                        continue;
                    }

                    Vector3 fromSpine = world - spine.position;
                    float lateral = Vector3.Dot(fromSpine, root.right);
                    float forward = Vector3.Dot(fromSpine, root.forward);
                    if (forward < 0f)
                    {
                        continue;
                    }

                    Vector3 horizontal =
                        root.right * lateral + root.forward * forward;
                    if (horizontal.sqrMagnitude <= 0.0000001f)
                    {
                        continue;
                    }

                    float verticalPhase = Mathf.InverseLerp(
                        lowerHeight,
                        upperHeight,
                        height);
                    float fade = Mathf.Sin(verticalPhase * Mathf.PI);
                    if (fade <= 0f)
                    {
                        continue;
                    }

                    worldDirections[vertex] = horizontal.normalized;
                    rawFades[vertex] = fade;
                    regions[vertex] = Mathf.Abs(lateral) <= Mathf.Abs(forward)
                        ? 1
                        : lateral < 0f
                            ? 2
                            : 3;
                    maximumRawFade = Mathf.Max(maximumRawFade, fade);
                }

                if (maximumRawFade <= 0f)
                {
                    throw new InvalidOperationException(
                        "Ready breathing mesh found no chest vertices.");
                }

                float fullWeightExpansion =
                    expansionAtAnimatedWeight * 100f / animatedWeight;
                Vector3[] deltaVertices = new Vector3[originalMesh.vertexCount];
                bool[] affected = new bool[originalMesh.vertexCount];
                int frontCount = 0;
                int leftCount = 0;
                int rightCount = 0;
                for (int vertex = 0; vertex < originalMesh.vertexCount; vertex++)
                {
                    if (rawFades[vertex] <= 0f)
                    {
                        continue;
                    }

                    Vector3 desiredWorldDelta = worldDirections[vertex] *
                        (fullWeightExpansion * rawFades[vertex] / maximumRawFade);
                    Vector3 desiredRendererLocal =
                        renderer.transform.InverseTransformVector(
                            desiredWorldDelta);
                    Matrix4x4 skinMatrix = CalculateWeightedSkinMatrix(
                        renderer,
                        bindPoses,
                        boneWeights[vertex]);
                    Vector3 bindDelta = skinMatrix.inverse.MultiplyVector(
                        desiredRendererLocal);
                    if (!IsFinite(bindDelta) || bindDelta.magnitude > 0.1f)
                    {
                        throw new InvalidOperationException(
                            "Ready breathing mesh produced an invalid chest delta.");
                    }

                    deltaVertices[vertex] = bindDelta;
                    affected[vertex] = true;
                    switch (regions[vertex])
                    {
                        case 1:
                            frontCount++;
                            break;
                        case 2:
                            leftCount++;
                            break;
                        case 3:
                            rightCount++;
                            break;
                    }
                }

                int affectedCount = affected.Count(value => value);
                if (affectedCount == 0 ||
                    frontCount == 0 ||
                    leftCount == 0 ||
                    rightCount == 0)
                {
                    throw new InvalidOperationException(
                        "Ready breathing mesh does not cover the front and both chest sides.");
                }

                Mesh generated = UnityEngine.Object.Instantiate(originalMesh);
                generated.name = "Hands_Throw_Ready_Breathing";
                generated.AddBlendShapeFrame(
                    ThrowReadyBreathingBlendShapeName,
                    100f,
                    deltaVertices,
                    new Vector3[originalMesh.vertexCount],
                    new Vector3[originalMesh.vertexCount]);
                generated.RecalculateBounds();
                Directory.CreateDirectory(Path.GetDirectoryName(
                    ThrowReadyBreathingMeshPath));
                Mesh breathingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                    ThrowReadyBreathingMeshPath);
                if (breathingMesh == null)
                {
                    AssetDatabase.CreateAsset(
                        generated,
                        ThrowReadyBreathingMeshPath);
                    breathingMesh = generated;
                }
                else
                {
                    EditorUtility.CopySerialized(generated, breathingMesh);
                    UnityEngine.Object.DestroyImmediate(generated);
                    breathingMesh.name = "Hands_Throw_Ready_Breathing";
                    EditorUtility.SetDirty(breathingMesh);
                }

                AssetDatabase.SaveAssets();
                int blendShapeIndex = breathingMesh.GetBlendShapeIndex(
                    ThrowReadyBreathingBlendShapeName);
                if (blendShapeIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Ready breathing mesh is missing its Breathing BlendShape.");
                }

                renderer.sharedMesh = breathingMesh;
                baseClip.SampleAnimation(workObject, readyEndTime);
                renderer.SetBlendShapeWeight(blendShapeIndex, 0f);
                renderer.BakeMesh(baseBake, true);
                renderer.SetBlendShapeWeight(blendShapeIndex, animatedWeight);
                renderer.BakeMesh(expandedBake, true);
                Vector3[] baseVertices = baseBake.vertices;
                Vector3[] expandedVertices = expandedBake.vertices;
                float maximumExpansion = 0f;
                for (int vertex = 0; vertex < baseVertices.Length; vertex++)
                {
                    maximumExpansion = Mathf.Max(
                        maximumExpansion,
                        Vector3.Distance(
                            renderer.transform.TransformPoint(baseVertices[vertex]),
                            renderer.transform.TransformPoint(expandedVertices[vertex])));
                }

                return new ThrowBreathingMeshBuildResult
                {
                    Mesh = breathingMesh,
                    RendererPath = rendererPath,
                    BlendShapeIndex = blendShapeIndex,
                    AffectedVertexCount = affectedCount,
                    FrontVertexCount = frontCount,
                    LeftSideVertexCount = leftCount,
                    RightSideVertexCount = rightCount,
                    MaximumExpansionAtThirtyPercentMeters = maximumExpansion
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baseBake);
                UnityEngine.Object.DestroyImmediate(expandedBake);
                UnityEngine.Object.DestroyImmediate(workObject);
            }
        }

        private static string DominantBoneName(
            BoneWeight weight,
            Transform[] bones)
        {
            int[] indices =
            {
                weight.boneIndex0,
                weight.boneIndex1,
                weight.boneIndex2,
                weight.boneIndex3
            };
            float[] values =
            {
                weight.weight0,
                weight.weight1,
                weight.weight2,
                weight.weight3
            };
            int best = 0;
            for (int index = 1; index < values.Length; index++)
            {
                if (values[index] > values[best])
                {
                    best = index;
                }
            }

            int boneIndex = indices[best];
            return boneIndex >= 0 && boneIndex < bones.Length && bones[boneIndex] != null
                ? bones[boneIndex].name
                : string.Empty;
        }

        private static AnimationClip CreateOrUpdateThrowReadyClip(
            AnimationClip source,
            Transform template,
            string rendererPath,
            float readyEndTime,
            float holdDuration,
            float breathingFrequency,
            float breathingMaximumWeight,
            float requestedBodyDrop,
            out ThrowBreathingMotionBuildResult motionBuild)
        {
            float holdEndTime = readyEndTime + holdDuration;
            AnimationClip generated = new AnimationClip();
            EditorUtility.CopySerialized(source, generated);
            generated.name = "Hands_Throw_Ready_MixamoHeadHeightBreathing";
            generated.frameRate = source.frameRate;
            generated.wrapMode = WrapMode.Loop;
            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetCurveBindings(source))
            {
                AnimationCurve sourceCurve =
                    AnimationUtility.GetEditorCurve(source, binding);
                AnimationCurve readyCurve = CreateThrowReadyCurve(
                    sourceCurve,
                    readyEndTime,
                    holdEndTime,
                    binding.path + "/" + binding.propertyName);
                AnimationUtility.SetEditorCurve(generated, binding, readyCurve);
            }

            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                ObjectReferenceKeyframe[] sourceKeys =
                    AnimationUtility.GetObjectReferenceCurve(source, binding);
                List<ObjectReferenceKeyframe> readyKeys = sourceKeys
                    .Where(key => key.time <= readyEndTime + 0.0001f)
                    .ToList();
                ObjectReferenceKeyframe[] available = sourceKeys
                    .Where(key => key.time <= readyEndTime + 0.0001f)
                    .ToArray();
                UnityEngine.Object readyEndValue = available.Length > 0
                    ? available[available.Length - 1].value
                    : sourceKeys.Length > 0
                        ? sourceKeys[0].value
                        : null;
                if (!readyKeys.Any(key =>
                        Mathf.Abs(key.time - readyEndTime) <= 0.0001f))
                {
                    readyKeys.Add(new ObjectReferenceKeyframe
                    {
                        time = readyEndTime,
                        value = readyEndValue
                    });
                }

                readyKeys.Add(new ObjectReferenceKeyframe
                {
                    time = holdEndTime,
                    value = readyEndValue
                });
                AnimationUtility.SetObjectReferenceCurve(
                    generated,
                    binding,
                    readyKeys.OrderBy(key => key.time).ToArray());
            }

            AnimationEvent[] readyEvents = AnimationUtility
                .GetAnimationEvents(source)
                .Where(animationEvent =>
                    animationEvent.time <= readyEndTime + 0.0001f)
                .ToArray();
            AnimationUtility.SetAnimationEvents(generated, readyEvents);
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(source);
            settings.startTime = 0f;
            settings.stopTime = holdEndTime;
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(generated, settings);
            motionBuild = ApplyThrowReadyBreathingCurves(
                template,
                source,
                generated,
                rendererPath,
                readyEndTime,
                holdDuration,
                breathingFrequency,
                breathingMaximumWeight,
                requestedBodyDrop);
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ThrowReadyClipPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, ThrowReadyClipPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                existing.name = "Hands_Throw_Ready_MixamoHeadHeightBreathing";
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static ThrowBreathingMotionBuildResult
            ApplyThrowReadyBreathingCurves(
                Transform template,
                AnimationClip source,
                AnimationClip generated,
                string rendererPath,
                float readyEndTime,
                float holdDuration,
                float breathingFrequency,
                float breathingMaximumWeight,
                float requestedBodyDrop)
        {
            GameObject workObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            workObject.name = "HandsThrowReadyBreathingMotionBuild";
            workObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(workObject);
            try
            {
                Transform root = workObject.transform;
                SkinnedMeshRenderer renderer = RequireRelativeSkinnedMeshRenderer(
                    root,
                    rendererPath);
                int blendShapeIndex = renderer.sharedMesh != null
                    ? renderer.sharedMesh.GetBlendShapeIndex(
                        ThrowReadyBreathingBlendShapeName)
                    : -1;
                if (blendShapeIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Ready breathing motion build is missing its state mesh BlendShape.");
                }

                int readyEndFrame = Mathf.RoundToInt(
                    readyEndTime * source.frameRate);
                int totalFrames = Mathf.RoundToInt(
                    (readyEndTime + holdDuration) * source.frameRate);
                source.SampleAnimation(workObject, readyEndTime);
                renderer.SetBlendShapeWeight(blendShapeIndex, 0f);
                Transform hips = FindRequired(root, HipsPath);
                Transform leftUpper = FindRequired(root, LeftUpLegPath);
                Transform leftLower = FindRequired(root, LeftLegPath);
                Transform leftFoot = FindRequired(root, LeftFootPath);
                Transform rightUpper = FindRequired(root, RightUpLegPath);
                Transform rightLower = FindRequired(root, RightLegPath);
                Transform rightFoot = FindRequired(root, RightFootPath);
                Vector3 baseHipsWorld = hips.position;
                Vector3 baseLeftKneeWorld = leftLower.position;
                Vector3 baseRightKneeWorld = rightLower.position;
                Vector3 baseLeftFootWorld = leftFoot.position;
                Vector3 baseRightFootWorld = rightFoot.position;
                Quaternion baseLeftFootRotation = leftFoot.rotation;
                Quaternion baseRightFootRotation = rightFoot.rotation;
                float baseLeftFlex = MeasureKneeFlexDegrees(
                    leftUpper,
                    leftLower,
                    leftFoot);
                float baseRightFlex = MeasureKneeFlexDegrees(
                    rightUpper,
                    rightLower,
                    rightFoot);

                TransformCurveTrack hipsTrack = new TransformCurveTrack(HipsPath);
                TransformCurveTrack leftUpperTrack =
                    new TransformCurveTrack(LeftUpLegPath);
                TransformCurveTrack leftLowerTrack =
                    new TransformCurveTrack(LeftLegPath);
                TransformCurveTrack leftFootTrack =
                    new TransformCurveTrack(LeftFootPath);
                TransformCurveTrack rightUpperTrack =
                    new TransformCurveTrack(RightUpLegPath);
                TransformCurveTrack rightLowerTrack =
                    new TransformCurveTrack(RightLegPath);
                TransformCurveTrack rightFootTrack =
                    new TransformCurveTrack(RightFootPath);
                List<Keyframe> breathingKeys = new List<Keyframe>();
                float maximumBodyDrop = 0f;
                float maximumFootDisplacement = 0f;
                float maximumLeftFlexIncrease = 0f;
                float maximumRightFlexIncrease = 0f;
                for (int frame = 0; frame <= totalFrames; frame++)
                {
                    float time = frame / source.frameRate;
                    float factor = 0f;
                    if (frame <= readyEndFrame)
                    {
                        source.SampleAnimation(workObject, time);
                    }
                    else
                    {
                        source.SampleAnimation(workObject, readyEndTime);
                        float holdTime = time - readyEndTime;
                        factor = 0.5f - 0.5f * Mathf.Cos(
                            2f * Mathf.PI * breathingFrequency * holdTime);
                        hips.position = baseHipsWorld -
                            root.up * (requestedBodyDrop * factor);
                        float leftError = SolveTwoBoneLeg(
                            root,
                            leftUpper,
                            leftLower,
                            leftFoot,
                            baseLeftFootWorld,
                            baseLeftFootRotation,
                            baseLeftKneeWorld);
                        float rightError = SolveTwoBoneLeg(
                            root,
                            rightUpper,
                            rightLower,
                            rightFoot,
                            baseRightFootWorld,
                            baseRightFootRotation,
                            baseRightKneeWorld);
                        maximumFootDisplacement = Mathf.Max(
                            maximumFootDisplacement,
                            leftError,
                            rightError);
                        maximumBodyDrop = Mathf.Max(
                            maximumBodyDrop,
                            Vector3.Dot(
                                baseHipsWorld - hips.position,
                                root.up));
                        maximumLeftFlexIncrease = Mathf.Max(
                            maximumLeftFlexIncrease,
                            MeasureKneeFlexDegrees(
                                leftUpper,
                                leftLower,
                                leftFoot) - baseLeftFlex);
                        maximumRightFlexIncrease = Mathf.Max(
                            maximumRightFlexIncrease,
                            MeasureKneeFlexDegrees(
                                rightUpper,
                                rightLower,
                                rightFoot) - baseRightFlex);
                    }

                    renderer.SetBlendShapeWeight(
                        blendShapeIndex,
                        factor * breathingMaximumWeight);
                    hipsTrack.Add(time, hips);
                    leftUpperTrack.Add(time, leftUpper);
                    leftLowerTrack.Add(time, leftLower);
                    leftFootTrack.Add(time, leftFoot);
                    rightUpperTrack.Add(time, rightUpper);
                    rightLowerTrack.Add(time, rightLower);
                    rightFootTrack.Add(time, rightFoot);
                    breathingKeys.Add(new Keyframe(
                        time,
                        factor * breathingMaximumWeight));
                }

                RemoveThrowReadyBreathingTransformCurves(generated);
                SetTransformTrackCurves(generated, hipsTrack);
                SetRotationTrackCurves(generated, leftUpperTrack);
                SetRotationTrackCurves(generated, leftLowerTrack);
                SetRotationTrackCurves(generated, leftFootTrack);
                SetRotationTrackCurves(generated, rightUpperTrack);
                SetRotationTrackCurves(generated, rightLowerTrack);
                SetRotationTrackCurves(generated, rightFootTrack);
                AnimationCurve breathingCurve = new AnimationCurve(
                    breathingKeys.ToArray());
                for (int key = 0; key < breathingCurve.length; key++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        breathingCurve,
                        key,
                        AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        breathingCurve,
                        key,
                        AnimationUtility.TangentMode.Linear);
                }

                AnimationUtility.SetEditorCurve(
                    generated,
                    EditorCurveBinding.FloatCurve(
                        rendererPath,
                        typeof(SkinnedMeshRenderer),
                        "blendShape." + ThrowReadyBreathingBlendShapeName),
                    breathingCurve);
                int trackKeyCount =
                    hipsTrack.PositionX.Count * 3 +
                    (hipsTrack.RotationX.Count +
                     leftUpperTrack.RotationX.Count +
                     leftLowerTrack.RotationX.Count +
                     leftFootTrack.RotationX.Count +
                     rightUpperTrack.RotationX.Count +
                     rightLowerTrack.RotationX.Count +
                     rightFootTrack.RotationX.Count) * 4 +
                    breathingCurve.length;
                return new ThrowBreathingMotionBuildResult
                {
                    BreathingCycleCount = Mathf.RoundToInt(
                        holdDuration * breathingFrequency),
                    CurveKeyCount = trackKeyCount,
                    MaximumBodyDropMeters = maximumBodyDrop,
                    MaximumFootDisplacementMeters = maximumFootDisplacement,
                    MinimumKneeFlexIncreaseDegrees = Mathf.Min(
                        maximumLeftFlexIncrease,
                        maximumRightFlexIncrease)
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(workObject);
            }
        }

        private static void RemoveThrowReadyBreathingTransformCurves(
            AnimationClip clip)
        {
            HashSet<string> legPaths = new HashSet<string>(
                new[]
                {
                    LeftUpLegPath,
                    LeftLegPath,
                    LeftFootPath,
                    RightUpLegPath,
                    RightLegPath,
                    RightFootPath
                },
                StringComparer.Ordinal);
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    (string.Equals(binding.path, HipsPath, StringComparison.Ordinal) &&
                     (binding.propertyName.StartsWith(
                          "m_LocalPosition.",
                          StringComparison.Ordinal) ||
                      IsTransformRotationProperty(binding.propertyName))) ||
                    (legPaths.Contains(binding.path) &&
                     IsTransformRotationProperty(binding.propertyName)))
                .ToArray();
            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }
        }

        private static bool IsTransformRotationProperty(string property)
        {
            return property.StartsWith("m_LocalRotation.", StringComparison.Ordinal) ||
                   property.IndexOf("Euler", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float SolveTwoBoneLeg(
            Transform characterRoot,
            Transform upper,
            Transform lower,
            Transform foot,
            Vector3 targetFootPosition,
            Quaternion targetFootRotation,
            Vector3 poleReference)
        {
            Vector3 hipPosition = upper.position;
            float upperLength = Vector3.Distance(upper.position, lower.position);
            float lowerLength = Vector3.Distance(lower.position, foot.position);
            Vector3 toTarget = targetFootPosition - hipPosition;
            float targetDistance = toTarget.magnitude;
            if (upperLength <= 0.00001f ||
                lowerLength <= 0.00001f ||
                targetDistance <= 0.00001f)
            {
                throw new InvalidOperationException(
                    "Ready breathing leg has invalid two-bone dimensions.");
            }

            Vector3 direction = toTarget / targetDistance;
            float solvedDistance = Mathf.Clamp(
                targetDistance,
                Mathf.Abs(upperLength - lowerLength) + 0.00001f,
                upperLength + lowerLength - 0.00001f);
            float along =
                (upperLength * upperLength - lowerLength * lowerLength +
                 solvedDistance * solvedDistance) /
                (2f * solvedDistance);
            float height = Mathf.Sqrt(Mathf.Max(
                0f,
                upperLength * upperLength - along * along));
            Vector3 linePoint = hipPosition + direction * Vector3.Dot(
                poleReference - hipPosition,
                direction);
            Vector3 poleDirection = poleReference - linePoint;
            if (poleDirection.sqrMagnitude <= 0.0000001f)
            {
                poleDirection = Vector3.ProjectOnPlane(
                    characterRoot.forward,
                    direction);
            }

            if (poleDirection.sqrMagnitude <= 0.0000001f)
            {
                poleDirection = Vector3.ProjectOnPlane(
                    characterRoot.right,
                    direction);
            }

            poleDirection.Normalize();
            Vector3 desiredKnee =
                hipPosition + direction * along + poleDirection * height;
            Vector3 currentUpperDirection = lower.position - upper.position;
            Vector3 desiredUpperDirection = desiredKnee - upper.position;
            upper.rotation = Quaternion.FromToRotation(
                currentUpperDirection,
                desiredUpperDirection) * upper.rotation;
            Vector3 currentLowerDirection = foot.position - lower.position;
            Vector3 desiredLowerDirection = targetFootPosition - lower.position;
            lower.rotation = Quaternion.FromToRotation(
                currentLowerDirection,
                desiredLowerDirection) * lower.rotation;
            foot.rotation = targetFootRotation;
            return Vector3.Distance(foot.position, targetFootPosition);
        }

        private static float MeasureKneeFlexDegrees(
            Transform upper,
            Transform lower,
            Transform foot)
        {
            float jointAngle = Vector3.Angle(
                upper.position - lower.position,
                foot.position - lower.position);
            return 180f - jointAngle;
        }

        private static AnimationCurve CreateThrowReadyCurve(
            AnimationCurve source,
            float readyEndTime,
            float holdEndTime,
            string label)
        {
            Keyframe[] sourceKeys = source.keys;
            List<Keyframe> readyKeys = sourceKeys
                .Where(key => key.time <= readyEndTime + 0.0001f)
                .ToList();
            int readyEndKeyIndex = readyKeys.FindIndex(key =>
                Mathf.Abs(key.time - readyEndTime) <= 0.0001f);
            if (readyEndKeyIndex < 0)
            {
                bool constantCurve = sourceKeys.Length <= 1 ||
                    sourceKeys.All(key =>
                        Mathf.Abs(key.value - sourceKeys[0].value) <= 0.000001f);
                if (!constantCurve)
                {
                    throw new InvalidOperationException(
                        "Hands Throw source curve has no exact key at the directly confirmed head-height frame: " +
                        label + ".");
                }

                readyKeys.Add(new Keyframe(
                    readyEndTime,
                    source.Evaluate(readyEndTime),
                    0f,
                    0f));
                readyEndKeyIndex = readyKeys.Count - 1;
            }

            Keyframe readyEndKey = readyKeys[readyEndKeyIndex];
            readyEndKey.time = readyEndTime;
            readyEndKey.outTangent = 0f;
            readyEndKey.outWeight = 0f;
            readyEndKey.weightedMode = (WeightedMode)(
                (int)readyEndKey.weightedMode & (int)WeightedMode.In);
            readyKeys[readyEndKeyIndex] = readyEndKey;
            Keyframe holdKey = readyEndKey;
            holdKey.time = holdEndTime;
            holdKey.inTangent = 0f;
            holdKey.outTangent = 0f;
            holdKey.inWeight = 0f;
            holdKey.outWeight = 0f;
            holdKey.weightedMode = WeightedMode.None;
            readyKeys.Add(holdKey);
            AnimationCurve result = new AnimationCurve(
                readyKeys.OrderBy(key => key.time).ToArray())
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = WrapMode.Loop
            };
            return result;
        }

        private static void MeasureThrowReadyPrefixAndHold(
            Transform template,
            AnimationClip source,
            AnimationClip ready,
            int readyEndFrame,
            float holdDuration,
            out float prefixPositionDifference,
            out float prefixRotationDifference,
            out float holdPositionDifference,
            out float holdRotationDifference)
        {
            GameObject sourceObject = UnityEngine.Object.Instantiate(template.gameObject);
            GameObject readyObject = UnityEngine.Object.Instantiate(template.gameObject);
            sourceObject.name = "HandsThrowReadyPrefixSource";
            readyObject.name = "HandsThrowReadyPrefixResult";
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            readyObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(sourceObject);
            DisableAnimators(readyObject);
            try
            {
                prefixPositionDifference = 0f;
                prefixRotationDifference = 0f;
                holdPositionDifference = 0f;
                holdRotationDifference = 0f;
                for (int frame = 0; frame <= readyEndFrame; frame++)
                {
                    float time = frame / source.frameRate;
                    source.SampleAnimation(sourceObject, time);
                    ready.SampleAnimation(readyObject, time);
                    MeasureArmaturePoseDifference(
                        CapturePose(sourceObject.transform),
                        CapturePose(readyObject.transform),
                        out float positionDifference,
                        out float rotationDifference);
                    prefixPositionDifference = Mathf.Max(
                        prefixPositionDifference,
                        positionDifference);
                    prefixRotationDifference = Mathf.Max(
                        prefixRotationDifference,
                        rotationDifference);
                }

                float readyEndTime = readyEndFrame / source.frameRate;
                source.SampleAnimation(sourceObject, readyEndTime);
                PoseSnapshot readyEndPose = CapturePose(sourceObject.transform);
                int holdIntervals = Mathf.Max(
                    1,
                    Mathf.RoundToInt(holdDuration * ready.frameRate));
                for (int interval = 0; interval < holdIntervals; interval++)
                {
                    float time = readyEndTime +
                        holdDuration * interval / holdIntervals;
                    ready.SampleAnimation(readyObject, time);
                    MeasureArmaturePoseDifference(
                        readyEndPose,
                        CapturePose(readyObject.transform),
                        out float positionDifference,
                        out float rotationDifference);
                    holdPositionDifference = Mathf.Max(
                        holdPositionDifference,
                        positionDifference);
                    holdRotationDifference = Mathf.Max(
                        holdRotationDifference,
                        rotationDifference);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceObject);
                UnityEngine.Object.DestroyImmediate(readyObject);
            }
        }

        private static bool ThrowReadyEventsMatchSourcePrefix(
            AnimationClip source,
            AnimationClip ready,
            float readyEndTime)
        {
            AnimationEvent[] sourceEvents = AnimationUtility
                .GetAnimationEvents(source)
                .Where(animationEvent =>
                    animationEvent.time <= readyEndTime + 0.0001f)
                .ToArray();
            AnimationEvent[] readyEvents = AnimationUtility.GetAnimationEvents(ready);
            if (sourceEvents.Length != readyEvents.Length)
            {
                return false;
            }

            for (int index = 0; index < sourceEvents.Length; index++)
            {
                AnimationEvent first = sourceEvents[index];
                AnimationEvent second = readyEvents[index];
                if (Mathf.Abs(first.time - second.time) > 0.00001f ||
                    !string.Equals(
                        first.functionName,
                        second.functionName,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        first.stringParameter,
                        second.stringParameter,
                        StringComparison.Ordinal) ||
                    Mathf.Abs(first.floatParameter - second.floatParameter) > 0.00001f ||
                    first.intParameter != second.intParameter ||
                    first.objectReferenceParameter != second.objectReferenceParameter)
                {
                    return false;
                }
            }

            return true;
        }

        private static AnimationClip CreateOrUpdateThrowCancelClip(
            Transform template,
            AnimationClip readyClip,
            AnimationClip idleClip,
            float readyEndTime,
            float initialHoldDuration,
            float finalIdleHoldDuration)
        {
            AnimationClip generated = new AnimationClip
            {
                name = "Hands_Throw_Cancel_MixamoReverse",
                frameRate = readyClip.frameRate,
                wrapMode = WrapMode.Loop,
                legacy = false
            };
            string[] transformPaths = AnimationUtility
                .GetCurveBindings(readyClip)
                .Concat(AnimationUtility.GetCurveBindings(idleClip))
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (transformPaths.Length == 0)
            {
                throw new InvalidOperationException(
                    "Hands Throw Ready has no Transform curves to copy into Cancel.");
            }

            GameObject readyObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            GameObject idleObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            readyObject.name = "HandsThrowCancelReadyFrameCopy";
            idleObject.name = "HandsThrowCancelIdleFrameZero";
            readyObject.hideFlags = HideFlags.HideAndDontSave;
            idleObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(readyObject);
            DisableAnimators(idleObject);
            try
            {
                Transform readyRoot = readyObject.transform;
                Transform idleRoot = idleObject.transform;
                Dictionary<string, TransformCurveTrack> tracks = transformPaths
                    .ToDictionary(
                        path => path,
                        path => new TransformCurveTrack(path),
                        StringComparer.Ordinal);
                int holdFrames = Mathf.RoundToInt(
                    initialHoldDuration * readyClip.frameRate);
                int reverseFrames = Mathf.RoundToInt(
                    readyEndTime * readyClip.frameRate);
                int finalHoldFrames = Mathf.RoundToInt(
                    finalIdleHoldDuration * readyClip.frameRate);
                idleClip.SampleAnimation(idleObject, 0f);
                for (int frame = 0; frame <= holdFrames; frame++)
                {
                    float time = frame / readyClip.frameRate;
                    readyClip.SampleAnimation(readyObject, readyEndTime);
                    foreach (string path in transformPaths)
                    {
                        Transform value = string.IsNullOrEmpty(path)
                            ? readyRoot
                            : FindRequired(readyRoot, path);
                        tracks[path].Add(time, value);
                    }
                }

                for (int frame = 1; frame <= reverseFrames; frame++)
                {
                    float offset = frame / readyClip.frameRate;
                    float time = initialHoldDuration + offset;
                    readyClip.SampleAnimation(
                        readyObject,
                        readyEndTime - offset);
                    BlendThrowCancelPoseTowardIdle(
                        readyRoot,
                        idleRoot,
                        transformPaths,
                        frame / (float)reverseFrames);
                    foreach (string path in transformPaths)
                    {
                        Transform value = string.IsNullOrEmpty(path)
                            ? readyRoot
                            : FindRequired(readyRoot, path);
                        tracks[path].Add(time, value);
                    }
                }

                float finalHoldStart = initialHoldDuration + readyEndTime;
                for (int frame = 1; frame <= finalHoldFrames; frame++)
                {
                    float time = finalHoldStart +
                        frame / readyClip.frameRate;
                    foreach (string path in transformPaths)
                    {
                        Transform value = string.IsNullOrEmpty(path)
                            ? idleRoot
                            : FindRequired(idleRoot, path);
                        tracks[path].Add(time, value);
                    }
                }

                foreach (TransformCurveTrack track in tracks.Values)
                {
                    SetTransformTrackCurves(generated, track);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readyObject);
                UnityEngine.Object.DestroyImmediate(idleObject);
            }

            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetObjectReferenceCurveBindings(readyClip))
            {
                ObjectReferenceKeyframe[] sourceKeys =
                    AnimationUtility.GetObjectReferenceCurve(
                        readyClip,
                        binding);
                AnimationUtility.SetObjectReferenceCurve(
                    generated,
                    binding,
                    CreateThrowCancelReverseObjectKeys(
                        sourceKeys,
                        readyEndTime,
                        initialHoldDuration,
                        binding.path + ":" + binding.propertyName));
            }

            AnimationEvent[] events = AnimationUtility
                .GetAnimationEvents(readyClip)
                .Where(animationEvent =>
                    animationEvent.time <= readyEndTime + 0.0001f)
                .Select(animationEvent =>
                {
                    AnimationEvent reversed = new AnimationEvent
                    {
                        time = initialHoldDuration +
                            (readyEndTime - animationEvent.time),
                        functionName = animationEvent.functionName,
                        stringParameter = animationEvent.stringParameter,
                        floatParameter = animationEvent.floatParameter,
                        intParameter = animationEvent.intParameter,
                        objectReferenceParameter =
                            animationEvent.objectReferenceParameter,
                        messageOptions = animationEvent.messageOptions
                    };
                    return reversed;
                })
                .OrderBy(animationEvent => animationEvent.time)
                .ToArray();
            AnimationUtility.SetAnimationEvents(generated, events);
            generated.EnsureQuaternionContinuity();
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(readyClip);
            settings.startTime = 0f;
            settings.stopTime = initialHoldDuration + readyEndTime +
                finalIdleHoldDuration;
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(generated, settings);
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ThrowCancelClipPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, ThrowCancelClipPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                existing.name = "Hands_Throw_Cancel_MixamoReverse";
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static AnimationCurve CreateThrowCancelReverseCurve(
            AnimationCurve source,
            float readyEndTime,
            float initialHoldDuration,
            string label)
        {
            Keyframe[] sourceKeys = source.keys
                .Where(key => key.time <= readyEndTime + 0.0001f)
                .OrderBy(key => key.time)
                .ToArray();
            if (sourceKeys.Length == 0)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel source curve has no Ready prefix keys: " +
                    label + ".");
            }

            int endIndex = Array.FindLastIndex(
                sourceKeys,
                key => Mathf.Abs(key.time - readyEndTime) <= 0.0001f);
            if (endIndex < 0)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel source curve has no exact Ready frame 19 key: " +
                    label + ".");
            }

            Keyframe endpoint = sourceKeys[endIndex];
            Keyframe holdStart = endpoint;
            holdStart.time = 0f;
            holdStart.inTangent = 0f;
            holdStart.outTangent = 0f;
            holdStart.inWeight = 0f;
            holdStart.outWeight = 0f;
            holdStart.weightedMode = WeightedMode.None;
            List<Keyframe> result = new List<Keyframe> { holdStart };
            for (int index = endIndex; index >= 0; index--)
            {
                Keyframe sourceKey = sourceKeys[index];
                Keyframe reversed = sourceKey;
                reversed.time = initialHoldDuration +
                    (readyEndTime - sourceKey.time);
                reversed.inTangent = -sourceKey.outTangent;
                reversed.outTangent = -sourceKey.inTangent;
                reversed.inWeight = sourceKey.outWeight;
                reversed.outWeight = sourceKey.inWeight;
                reversed.weightedMode = ReverseWeightedMode(
                    sourceKey.weightedMode);
                if (index == endIndex)
                {
                    reversed.time = initialHoldDuration;
                    reversed.inTangent = 0f;
                    reversed.inWeight = 0f;
                    reversed.weightedMode = (WeightedMode)(
                        (int)reversed.weightedMode &
                        ~(int)WeightedMode.In);
                }

                result.Add(reversed);
            }

            return new AnimationCurve(result.ToArray());
        }

        private static WeightedMode ReverseWeightedMode(WeightedMode source)
        {
            WeightedMode result = WeightedMode.None;
            if (((int)source & (int)WeightedMode.In) != 0)
            {
                result = (WeightedMode)((int)result | (int)WeightedMode.Out);
            }

            if (((int)source & (int)WeightedMode.Out) != 0)
            {
                result = (WeightedMode)((int)result | (int)WeightedMode.In);
            }

            return result;
        }

        private static ObjectReferenceKeyframe[]
            CreateThrowCancelReverseObjectKeys(
                IReadOnlyList<ObjectReferenceKeyframe> sourceKeys,
                float readyEndTime,
                float initialHoldDuration,
                string label)
        {
            ObjectReferenceKeyframe[] prefix = sourceKeys
                .Where(key => key.time <= readyEndTime + 0.0001f)
                .OrderBy(key => key.time)
                .ToArray();
            if (prefix.Length == 0)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel object curve has no Ready prefix keys: " +
                    label + ".");
            }

            UnityEngine.Object endpointValue = prefix[prefix.Length - 1].value;
            List<ObjectReferenceKeyframe> result =
                new List<ObjectReferenceKeyframe>
                {
                    new ObjectReferenceKeyframe
                    {
                        time = 0f,
                        value = endpointValue
                    },
                    new ObjectReferenceKeyframe
                    {
                        time = initialHoldDuration,
                        value = endpointValue
                    }
                };
            for (int index = prefix.Length - 1; index >= 0; index--)
            {
                ObjectReferenceKeyframe key = prefix[index];
                float reversedTime = initialHoldDuration +
                    (readyEndTime - key.time);
                if (Mathf.Abs(reversedTime - initialHoldDuration) <= 0.0001f)
                {
                    continue;
                }

                key.time = reversedTime;
                result.Add(key);
            }

            return result.OrderBy(key => key.time).ToArray();
        }

        private static string[] GetThrowCancelTransformPaths(
            AnimationClip readyClip,
            AnimationClip idleClip)
        {
            return AnimationUtility.GetCurveBindings(readyClip)
                .Concat(AnimationUtility.GetCurveBindings(idleClip))
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static void BlendThrowCancelPoseTowardIdle(
            Transform readyRoot,
            Transform idleRoot,
            IReadOnlyList<string> transformPaths,
            float idleWeight)
        {
            float weight = Mathf.Clamp01(idleWeight);
            foreach (string path in transformPaths)
            {
                Transform readyValue = string.IsNullOrEmpty(path)
                    ? readyRoot
                    : FindRequired(readyRoot, path);
                Transform idleValue = string.IsNullOrEmpty(path)
                    ? idleRoot
                    : FindRequired(idleRoot, path);
                readyValue.localPosition = Vector3.Lerp(
                    readyValue.localPosition,
                    idleValue.localPosition,
                    weight);
                readyValue.localRotation = Quaternion.Slerp(
                    readyValue.localRotation,
                    idleValue.localRotation,
                    weight);
            }
        }

        private static void MeasureThrowCancelClipExact(
            Transform template,
            AnimationClip readyClip,
            AnimationClip idleClip,
            AnimationClip cancelClip,
            float readyEndTime,
            float initialHoldDuration,
            float finalIdleHoldDuration,
            out float holdPositionDifference,
            out float holdRotationDifference,
            out float reversePositionDifference,
            out float reverseRotationDifference,
            out float finalIdlePositionDifference,
            out float finalIdleRotationDifference,
            out float finalHoldPositionDifference,
            out float finalHoldRotationDifference)
        {
            GameObject readyObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            GameObject idleObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            GameObject cancelObject = UnityEngine.Object.Instantiate(
                template.gameObject);
            readyObject.name = "HandsThrowCancelReadyExpected";
            idleObject.name = "HandsThrowCancelIdleExpected";
            cancelObject.name = "HandsThrowCancelGenerated";
            readyObject.hideFlags = HideFlags.HideAndDontSave;
            idleObject.hideFlags = HideFlags.HideAndDontSave;
            cancelObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(readyObject);
            DisableAnimators(idleObject);
            DisableAnimators(cancelObject);
            try
            {
                string[] transformPaths = GetThrowCancelTransformPaths(
                    readyClip,
                    idleClip);
                idleClip.SampleAnimation(idleObject, 0f);
                cancelClip.SampleAnimation(cancelObject, 0f);
                PoseSnapshot holdBaseline = CapturePose(cancelObject.transform);
                holdPositionDifference = 0f;
                holdRotationDifference = 0f;
                int holdFrames = Mathf.RoundToInt(
                    initialHoldDuration * cancelClip.frameRate);
                for (int frame = 0; frame <= holdFrames; frame++)
                {
                    float time = frame / cancelClip.frameRate;
                    cancelClip.SampleAnimation(cancelObject, time);
                    MeasureArmaturePoseDifference(
                        holdBaseline,
                        CapturePose(cancelObject.transform),
                        out float positionDifference,
                        out float rotationDifference);
                    holdPositionDifference = Mathf.Max(
                        holdPositionDifference,
                        positionDifference);
                    holdRotationDifference = Mathf.Max(
                        holdRotationDifference,
                        rotationDifference);
                }

                reversePositionDifference = 0f;
                reverseRotationDifference = 0f;
                int reverseFrames = Mathf.RoundToInt(
                    readyEndTime * cancelClip.frameRate);
                for (int frame = 0; frame <= reverseFrames; frame++)
                {
                    float offset = frame / cancelClip.frameRate;
                    readyClip.SampleAnimation(
                        readyObject,
                        readyEndTime - offset);
                    BlendThrowCancelPoseTowardIdle(
                        readyObject.transform,
                        idleObject.transform,
                        transformPaths,
                        frame / (float)reverseFrames);
                    cancelClip.SampleAnimation(
                        cancelObject,
                        initialHoldDuration + offset);
                    MeasureArmaturePoseDifference(
                        CapturePose(readyObject.transform),
                        CapturePose(cancelObject.transform),
                        out float positionDifference,
                        out float rotationDifference);
                    reversePositionDifference = Mathf.Max(
                        reversePositionDifference,
                        positionDifference);
                    reverseRotationDifference = Mathf.Max(
                        reverseRotationDifference,
                        rotationDifference);
                }

                float finalHoldStart = initialHoldDuration + readyEndTime;
                cancelClip.SampleAnimation(cancelObject, finalHoldStart);
                PoseSnapshot idlePose = CapturePose(idleObject.transform);
                PoseSnapshot finalPose = CapturePose(cancelObject.transform);
                MeasureArmaturePoseDifference(
                    idlePose,
                    finalPose,
                    out finalIdlePositionDifference,
                    out finalIdleRotationDifference);
                finalHoldPositionDifference = 0f;
                finalHoldRotationDifference = 0f;
                int finalHoldFrames = Mathf.RoundToInt(
                    finalIdleHoldDuration * cancelClip.frameRate);
                for (int frame = 0; frame < finalHoldFrames; frame++)
                {
                    cancelClip.SampleAnimation(
                        cancelObject,
                        finalHoldStart + frame / cancelClip.frameRate);
                    MeasureArmaturePoseDifference(
                        idlePose,
                        CapturePose(cancelObject.transform),
                        out float positionDifference,
                        out float rotationDifference);
                    finalHoldPositionDifference = Mathf.Max(
                        finalHoldPositionDifference,
                        positionDifference);
                    finalHoldRotationDifference = Mathf.Max(
                        finalHoldRotationDifference,
                        rotationDifference);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readyObject);
                UnityEngine.Object.DestroyImmediate(idleObject);
                UnityEngine.Object.DestroyImmediate(cancelObject);
            }
        }

        private static void CapturePlayerHandsThrowCancelActualReview()
        {
            ThrowCancelApplyMetrics apply =
                ReadJson<ThrowCancelApplyMetrics>(
                    ThrowCancelApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform target = RequireTarget(layout, ThrowCancelTargetName);
            Transform idleReference = RequireTarget(
                layout,
                IdleReferenceTargetName);
            AnimationClip readyClip = LoadClip(ThrowReadyClipPath);
            AnimationClip idleClip = LoadClip(IdleClipPath);
            AnimationClip cancelClip = LoadClip(ThrowCancelClipPath);
            CaptureThrowCancelComparison(
                target,
                idleReference,
                readyClip,
                idleClip,
                cancelClip,
                apply.readyEndTimeSeconds,
                apply.initialHoldDurationSeconds,
                ThrowCancelReviewPath);
            TargetReviewMetrics runtime = CaptureTargetMetrics(
                target,
                cancelClip,
                ThrowCancelStateName,
                "Ready frame 19 hold 0.5s + linear Player_Idle frame 0 blend + Idle hold 0.5s");
            runtime.passedNumericChecks = TargetReviewPassed(runtime);
            MeasureThrowCancelRuntimeExpected(
                target,
                idleReference,
                readyClip,
                idleClip,
                cancelClip,
                apply.readyEndTimeSeconds,
                apply.initialHoldDurationSeconds,
                out float holdPositionDifference,
                out float holdRotationDifference,
                out float reversePositionDifference,
                out float reverseRotationDifference,
                out float finalIdlePositionDifference,
                out float finalIdleRotationDifference,
                out float finalHoldPositionDifference,
                out float finalHoldRotationDifference);
            ThrowCancelReviewMetrics metrics = new ThrowCancelReviewMetrics
            {
                target = ThrowCancelTargetName,
                phasesCaptured = 12,
                runtime = runtime,
                holdPositionDifferenceMax = holdPositionDifference,
                holdRotationDifferenceDegreesMax = holdRotationDifference,
                expectedReversePositionDifferenceMax =
                    reversePositionDifference,
                expectedReverseRotationDifferenceDegreesMax =
                    reverseRotationDifference,
                finalIdlePositionDifferenceMax = finalIdlePositionDifference,
                finalIdleRotationDifferenceDegreesMax =
                    finalIdleRotationDifference,
                finalHoldPositionDifferenceMax = finalHoldPositionDifference,
                finalHoldRotationDifferenceDegreesMax =
                    finalHoldRotationDifference,
                hasNoBlendShapeCurves = HasNoBlendShapeCurves(cancelClip),
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            metrics.passedNumericChecks =
                metrics.phasesCaptured == 12 &&
                metrics.runtime.passedNumericChecks &&
                metrics.holdPositionDifferenceMax <= PositionTolerance &&
                metrics.holdRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.expectedReversePositionDifferenceMax <= PositionTolerance &&
                metrics.expectedReverseRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.finalIdlePositionDifferenceMax <= PositionTolerance &&
                metrics.finalIdleRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.finalHoldPositionDifferenceMax <= PositionTolerance &&
                metrics.finalHoldRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.hasNoBlendShapeCurves;
            WriteJson(ThrowCancelReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw Cancel Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsThrowCancel] Captured static Ready hold, linear Idle blend, and final Idle hold in Play Mode. " +
                "Frames=" + runtime.framesSampled +
                ", RuntimePose=" +
                Num(runtime.sourcePosePositionDifferenceMax) + "/" +
                Num(runtime.sourcePoseRotationDifferenceDegreesMax) +
                ", Hold=" + Num(metrics.holdPositionDifferenceMax) +
                "/" + Num(metrics.holdRotationDifferenceDegreesMax) +
                ", Reverse=" +
                Num(metrics.expectedReversePositionDifferenceMax) + "/" +
                Num(metrics.expectedReverseRotationDifferenceDegreesMax) +
                ", FinalIdle=" +
                Num(metrics.finalIdlePositionDifferenceMax) + "/" +
                Num(metrics.finalIdleRotationDifferenceDegreesMax) +
                ", IdleHold=" +
                Num(metrics.finalHoldPositionDifferenceMax) + "/" +
                Num(metrics.finalHoldRotationDifferenceDegreesMax) +
                ", Breathing=False, Loops=2.");
        }

        private static void CaptureThrowCancelComparison(
            Transform target,
            Transform idleReference,
            AnimationClip readyClip,
            AnimationClip idleClip,
            AnimationClip cancelClip,
            float readyEndTime,
            float initialHoldDuration,
            string outputPath)
        {
            Animator animator = RequireAnimator(target);
            GameObject idleObject = UnityEngine.Object.Instantiate(
                idleReference.gameObject);
            idleObject.name = "HandsThrowCancelReviewIdleFrameZero";
            idleObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(idleObject);
            foreach (Renderer renderer in
                     idleObject.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            idleClip.SampleAnimation(idleObject, 0f);
            string[] transformPaths = GetThrowCancelTransformPaths(
                readyClip,
                idleClip);
            List<List<byte[]>> rows = Enumerable.Range(0, 4)
                .Select(_ => new List<byte[]>())
                .ToList();
            try
            {
                using (CaptureEnvironment environment =
                       new CaptureEnvironment(target))
                {
                    for (int index = 0; index < 12; index++)
                    {
                        float cancelTime = cancelClip.length * index / 12f;
                        ApplyExpectedThrowCancelPose(
                            target.gameObject,
                            idleObject.transform,
                            readyClip,
                            transformPaths,
                            cancelTime,
                            readyEndTime,
                            initialHoldDuration);
                        environment.ConfigureView(target, 1.05f, 1.35f);
                        rows[0].Add(environment.CaptureFront());
                        rows[1].Add(environment.CaptureSide());
                        SampleAnimator(
                            animator,
                            ThrowCancelStateName,
                            cancelTime / cancelClip.length);
                        environment.ConfigureView(target, 1.05f, 1.35f);
                        rows[2].Add(environment.CaptureFront());
                        rows[3].Add(environment.CaptureSide());
                    }
                }

                ComposeRows(rows, outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(idleObject);
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void MeasureThrowCancelRuntimeExpected(
            Transform target,
            Transform idleReference,
            AnimationClip readyClip,
            AnimationClip idleClip,
            AnimationClip cancelClip,
            float readyEndTime,
            float initialHoldDuration,
            out float holdPositionDifference,
            out float holdRotationDifference,
            out float reversePositionDifference,
            out float reverseRotationDifference,
            out float finalIdlePositionDifference,
            out float finalIdleRotationDifference,
            out float finalHoldPositionDifference,
            out float finalHoldRotationDifference)
        {
            Animator animator = RequireAnimator(target);
            GameObject expectedObject = UnityEngine.Object.Instantiate(
                target.gameObject);
            GameObject idleObject = UnityEngine.Object.Instantiate(
                idleReference.gameObject);
            expectedObject.name = "HandsThrowCancelRuntimeExpected";
            idleObject.name = "HandsThrowCancelRuntimeIdleFrameZero";
            expectedObject.hideFlags = HideFlags.HideAndDontSave;
            idleObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(expectedObject);
            DisableAnimators(idleObject);
            try
            {
                idleClip.SampleAnimation(idleObject, 0f);
                string[] transformPaths = GetThrowCancelTransformPaths(
                    readyClip,
                    idleClip);
                SampleAnimator(animator, ThrowCancelStateName, 0f);
                PoseSnapshot holdBaseline = CapturePose(target);
                holdPositionDifference = 0f;
                holdRotationDifference = 0f;
                reversePositionDifference = 0f;
                reverseRotationDifference = 0f;
                finalHoldPositionDifference = 0f;
                finalHoldRotationDifference = 0f;
                float finalHoldStart = initialHoldDuration + readyEndTime;
                SampleAnimator(
                    animator,
                    ThrowCancelStateName,
                    finalHoldStart / cancelClip.length);
                MeasureArmaturePoseDifference(
                    CapturePose(idleObject.transform),
                    CapturePose(target),
                    out finalIdlePositionDifference,
                    out finalIdleRotationDifference);
                int framesPerLoop = Mathf.CeilToInt(
                    cancelClip.length * cancelClip.frameRate);
                for (int frame = 0; frame < framesPerLoop; frame++)
                {
                    float time = frame / cancelClip.frameRate;
                    ApplyExpectedThrowCancelPose(
                        expectedObject,
                        idleObject.transform,
                        readyClip,
                        transformPaths,
                        time,
                        readyEndTime,
                        initialHoldDuration);
                    SampleAnimator(
                        animator,
                        ThrowCancelStateName,
                        time / cancelClip.length);
                    PoseSnapshot actualPose = CapturePose(target);
                    MeasureArmaturePoseDifference(
                        CapturePose(expectedObject.transform),
                        actualPose,
                        out float positionDifference,
                        out float rotationDifference);
                    reversePositionDifference = Mathf.Max(
                        reversePositionDifference,
                        positionDifference);
                    reverseRotationDifference = Mathf.Max(
                        reverseRotationDifference,
                        rotationDifference);
                    if (time <= initialHoldDuration + 0.0001f)
                    {
                        MeasureArmaturePoseDifference(
                            holdBaseline,
                            actualPose,
                            out float holdPosition,
                            out float holdRotation);
                        holdPositionDifference = Mathf.Max(
                            holdPositionDifference,
                            holdPosition);
                        holdRotationDifference = Mathf.Max(
                            holdRotationDifference,
                            holdRotation);
                    }

                    if (time >= finalHoldStart - 0.0001f)
                    {
                        MeasureArmaturePoseDifference(
                            CapturePose(idleObject.transform),
                            actualPose,
                            out float finalPosition,
                            out float finalRotation);
                        finalHoldPositionDifference = Mathf.Max(
                            finalHoldPositionDifference,
                            finalPosition);
                        finalHoldRotationDifference = Mathf.Max(
                            finalHoldRotationDifference,
                            finalRotation);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expectedObject);
                UnityEngine.Object.DestroyImmediate(idleObject);
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private static void ApplyExpectedThrowCancelPose(
            GameObject expectedObject,
            Transform idleRoot,
            AnimationClip readyClip,
            IReadOnlyList<string> transformPaths,
            float cancelTime,
            float readyEndTime,
            float initialHoldDuration)
        {
            if (cancelTime <= initialHoldDuration)
            {
                readyClip.SampleAnimation(expectedObject, readyEndTime);
                return;
            }

            float reverseTime = cancelTime - initialHoldDuration;
            readyClip.SampleAnimation(
                expectedObject,
                Mathf.Max(0f, readyEndTime - reverseTime));
            BlendThrowCancelPoseTowardIdle(
                expectedObject.transform,
                idleRoot,
                transformPaths,
                reverseTime >= readyEndTime
                    ? 1f
                    : reverseTime / readyEndTime);
        }

        private static void CapturePlayerHandsThrowMixamoActualReview()
        {
            ThrowApplyMetrics apply = ReadJson<ThrowApplyMetrics>(
                ThrowApplyMetricsPath);
            if (!apply.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw Mixamo apply metrics did not pass.");
            }

            Scene scene = RequireScene();
            Transform layout = RequireLayout(scene);
            Transform readyTarget = RequireTarget(layout, ThrowReadyTargetName);
            Transform releaseTarget = RequireTarget(layout, ThrowReleaseTargetName);
            AnimationClip source = LoadSingleEmbeddedClip(
                ThrowSourcePath,
                "hands throw");
            AnimationClip readyClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ThrowReadyClipPath);
            if (readyClip == null)
            {
                throw new InvalidOperationException(
                    "Hands Throw Ready head-height breathing clip is missing.");
            }

            CaptureThrowReviewComparison(
                readyTarget,
                readyClip,
                apply.readyEndTimeSeconds,
                apply.holdDurationSeconds,
                ThrowReviewPath);
            TargetReviewMetrics readyMetrics = CaptureTargetMetrics(
                readyTarget,
                readyClip,
                ThrowReadyStateName,
                source.name + " frames 0.." + apply.readyEndFrame +
                " + 3s breathing hold");
            TargetReviewMetrics releaseMetrics = CaptureTargetMetrics(
                releaseTarget,
                source,
                ThrowReleaseStateName,
                source.name + " full Take");
            readyMetrics.passedNumericChecks = TargetReviewPassed(readyMetrics);
            releaseMetrics.passedNumericChecks = TargetReviewPassed(releaseMetrics);
            MeasureThrowReadyPrefixAndHold(
                readyTarget,
                source,
                readyClip,
                apply.readyEndFrame,
                0f,
                out float prefixPositionDifference,
                out float prefixRotationDifference,
                out _,
                out _);
            ThrowBreathingRuntimeMetrics breathing =
                MeasureThrowBreathingRuntime(
                    readyTarget,
                    readyClip,
                    apply);
            ThrowReviewMetrics metrics = new ThrowReviewMetrics
            {
                targetSet = ThrowReadyTargetName + ", " + ThrowReleaseTargetName,
                phasesCapturedPerComparison = 12,
                ready = readyMetrics,
                release = releaseMetrics,
                readyPrefixPositionDifferenceMax = prefixPositionDifference,
                readyPrefixRotationDifferenceDegreesMax = prefixRotationDifference,
                breathing = breathing,
                validationPriority =
                    "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
            };
            metrics.passedNumericChecks =
                metrics.phasesCapturedPerComparison == 12 &&
                metrics.ready.passedNumericChecks &&
                metrics.release.passedNumericChecks &&
                metrics.readyPrefixPositionDifferenceMax <= PositionTolerance &&
                metrics.readyPrefixRotationDifferenceDegreesMax <= RotationTolerance &&
                metrics.breathing.passedNumericChecks;
            WriteJson(ThrowReviewMetricsPath, metrics);
            if (!metrics.passedNumericChecks)
            {
                throw new InvalidOperationException(
                    "Hands Throw Mixamo Play Mode support checks failed. " +
                    JsonUtility.ToJson(metrics));
            }

            Debug.Log(
                "[PlayerHandsThrow] Captured Ready 3-second breathing hold and unchanged Release in Play Mode. " +
                "ReadyFrames=" + readyMetrics.framesSampled +
                ", ReleaseFrames=" + releaseMetrics.framesSampled +
                ", ReadyPose=" +
                Num(readyMetrics.sourcePosePositionDifferenceMax) + "/" +
                Num(readyMetrics.sourcePoseRotationDifferenceDegreesMax) +
                ", Breath=" + Num(breathing.maximumBlendShapeWeight) +
                ", Drop=" + Num(breathing.measuredBodyDropMeters) +
                ", Feet=" + Num(Mathf.Max(
                    breathing.maximumLeftFootDisplacementMeters,
                    breathing.maximumRightFootDisplacementMeters)) +
                ", ReleasePose=" +
                Num(releaseMetrics.sourcePosePositionDifferenceMax) + "/" +
                Num(releaseMetrics.sourcePoseRotationDifferenceDegreesMax) +
                ", Loops=2.");
        }

        private static void CaptureThrowReviewComparison(
            Transform readyTarget,
            AnimationClip readyClip,
            float readyEndTime,
            float holdDuration,
            string outputPath)
        {
            GameObject baselineObject = UnityEngine.Object.Instantiate(
                readyTarget.gameObject);
            baselineObject.name = "HandsThrowBreathingReviewBaseline";
            baselineObject.hideFlags = HideFlags.HideAndDontSave;
            DisableAnimators(baselineObject);
            Animator readyAnimator = RequireAnimator(readyTarget);
            List<List<byte[]>> rows = Enumerable.Range(0, 8)
                .Select(_ => new List<byte[]>())
                .ToList();
            try
            {
                CaptureThrowBreathingFourViewRows(
                    baselineObject.transform,
                    _ => readyClip.SampleAnimation(
                        baselineObject,
                        readyEndTime),
                    rows,
                    0);
                CaptureThrowBreathingFourViewRows(
                    readyTarget,
                    phase =>
                    {
                        float holdTime = phase * holdDuration;
                        SampleAnimator(
                            readyAnimator,
                            ThrowReadyStateName,
                            (readyEndTime + holdTime) / readyClip.length);
                    },
                    rows,
                    4);
                ComposeRows(rows, outputPath);
            }
            finally
            {
                readyAnimator.Rebind();
                readyAnimator.Update(0f);
                UnityEngine.Object.DestroyImmediate(baselineObject);
            }
        }

        private static void CaptureThrowBreathingFourViewRows(
            Transform subject,
            Action<float> sample,
            IReadOnlyList<List<byte[]>> rows,
            int rowOffset)
        {
            using (CaptureEnvironment environment = new CaptureEnvironment(subject))
            {
                for (int phaseIndex = 0; phaseIndex < 12; phaseIndex++)
                {
                    float phase = phaseIndex / 12f;
                    sample(phase);
                    environment.ConfigureView(subject, 1.05f, 1.35f);
                    rows[rowOffset].Add(environment.CaptureFront());
                    rows[rowOffset + 1].Add(environment.CaptureSide());
                    Vector3 chestCenter =
                        (FindRequired(subject, SolarPlexusPath).position +
                         FindRequired(subject, SpinePath).position) * 0.5f;
                    environment.ConfigureView(subject, chestCenter, 0.48f);
                    rows[rowOffset + 2].Add(environment.CaptureFront());
                    Vector3 legCenter =
                        (FindRequired(subject, HipsPath).position +
                         FindRequired(subject, LeftFootPath).position +
                         FindRequired(subject, RightFootPath).position) / 3f;
                    environment.ConfigureView(subject, legCenter, 0.72f);
                    rows[rowOffset + 3].Add(environment.CaptureSide());
                }
            }
        }

        private static ThrowBreathingRuntimeMetrics MeasureThrowBreathingRuntime(
            Transform readyTarget,
            AnimationClip readyClip,
            ThrowApplyMetrics apply)
        {
            Animator animator = RequireAnimator(readyTarget);
            SkinnedMeshRenderer renderer =
                RequirePrimaryPlayerSkinnedMeshRenderer(readyTarget);
            int blendShapeIndex = renderer.sharedMesh != null
                ? renderer.sharedMesh.GetBlendShapeIndex(
                    ThrowReadyBreathingBlendShapeName)
                : -1;
            if (blendShapeIndex < 0)
            {
                throw new InvalidOperationException(
                    "Ready breathing runtime renderer is missing Breathing.");
            }

            SampleAnimator(
                animator,
                ThrowReadyStateName,
                apply.readyEndTimeSeconds / readyClip.length);
            Transform hips = FindRequired(readyTarget, HipsPath);
            Transform leftFoot = FindRequired(readyTarget, LeftFootPath);
            Transform rightFoot = FindRequired(readyTarget, RightFootPath);
            Vector3 baseHips = hips.position;
            Vector3 baseLeftFoot = leftFoot.position;
            Vector3 baseRightFoot = rightFoot.position;
            float maximumWeight = 0f;
            float maximumDrop = 0f;
            float maximumLeftFoot = 0f;
            float maximumRightFoot = 0f;
            int framesPerLoop = Mathf.RoundToInt(
                readyClip.length * readyClip.frameRate);
            // A looping Animator maps normalizedTime == 1 back to frame zero.
            // That pose is the requested return to the start, not part of the
            // three-second breathing hold whose foot contact is measured here.
            for (int frame = 0; frame < framesPerLoop; frame++)
            {
                float time = frame / readyClip.frameRate;
                if (time + 0.0001f < apply.readyEndTimeSeconds)
                {
                    continue;
                }

                SampleAnimator(
                    animator,
                    ThrowReadyStateName,
                    time / readyClip.length);
                maximumWeight = Mathf.Max(
                    maximumWeight,
                    renderer.GetBlendShapeWeight(blendShapeIndex));
                maximumDrop = Mathf.Max(
                    maximumDrop,
                    Vector3.Dot(
                        baseHips - hips.position,
                        readyTarget.up));
                maximumLeftFoot = Mathf.Max(
                    maximumLeftFoot,
                    Vector3.Distance(baseLeftFoot, leftFoot.position));
                maximumRightFoot = Mathf.Max(
                    maximumRightFoot,
                    Vector3.Distance(baseRightFoot, rightFoot.position));
            }

            int detectedPeaks = 0;
            for (int cycle = 0; cycle < 3; cycle++)
            {
                float peakTime =
                    apply.readyEndTimeSeconds + cycle + 0.5f;
                SampleAnimator(
                    animator,
                    ThrowReadyStateName,
                    peakTime / readyClip.length);
                if (Mathf.Abs(
                        renderer.GetBlendShapeWeight(blendShapeIndex) - 30f) <= 0.01f &&
                    Mathf.Abs(
                        Vector3.Dot(
                            baseHips - hips.position,
                            readyTarget.up) - 0.03f) <= 0.0005f)
                {
                    detectedPeaks++;
                }
            }

            ThrowBreathingRuntimeMetrics metrics =
                new ThrowBreathingRuntimeMetrics
                {
                    maximumBlendShapeWeight = maximumWeight,
                    measuredBodyDropMeters = maximumDrop,
                    maximumLeftFootDisplacementMeters = maximumLeftFoot,
                    maximumRightFootDisplacementMeters = maximumRightFoot,
                    detectedBreathingPeaks = detectedPeaks,
                    blendShapeCurveApplied = true
                };
            metrics.passedNumericChecks =
                Mathf.Abs(metrics.maximumBlendShapeWeight - 30f) <= 0.01f &&
                Mathf.Abs(metrics.measuredBodyDropMeters - 0.03f) <= 0.0005f &&
                metrics.maximumLeftFootDisplacementMeters <= 0.0005f &&
                metrics.maximumRightFootDisplacementMeters <= 0.0005f &&
                metrics.detectedBreathingPeaks == 3 &&
                metrics.blendShapeCurveApplied;
            animator.Rebind();
            animator.Update(0f);
            return metrics;
        }

        private static void CaptureThrowThreeViewRows(
            Transform subject,
            Action<float> sample,
            IReadOnlyList<List<byte[]>> rows,
            int rowOffset)
        {
            using (CaptureEnvironment environment = new CaptureEnvironment(subject))
            {
                for (int phaseIndex = 0; phaseIndex < 12; phaseIndex++)
                {
                    float phase = phaseIndex / 12f;
                    sample(phase);
                    environment.ConfigureView(subject, 1.05f, 1.35f);
                    rows[rowOffset].Add(environment.CaptureFront());
                    rows[rowOffset + 1].Add(environment.CaptureSide());
                    Vector3 armCenter =
                        (FindRequired(subject, RightArmPath).position +
                         FindRequired(subject, RightHandPath).position) * 0.5f;
                    environment.ConfigureView(subject, armCenter, 0.62f);
                    rows[rowOffset + 2].Add(environment.CaptureFront());
                }
            }
        }

        private static void ComposeRows(
            IReadOnlyList<List<byte[]>> rows,
            string outputPath)
        {
            if (rows.Count == 0 || rows.Any(row => row.Count != rows[0].Count))
            {
                throw new InvalidOperationException(
                    "Player Hands comparison rows have inconsistent frame counts.");
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
                                "Player Hands comparison frame could not be decoded.");
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
                        "Player Hands comparison output directory is unavailable."));
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

        private static void ComposePairedFrameGrid(
            IReadOnlyList<byte[]> frontFrames,
            IReadOnlyList<byte[]> sideFrames,
            int columns,
            string outputPath)
        {
            if (frontFrames.Count == 0 ||
                frontFrames.Count != sideFrames.Count ||
                columns <= 0)
            {
                throw new InvalidOperationException(
                    "Player Hands paired frame grid input is invalid.");
            }

            int frameRows = Mathf.CeilToInt(frontFrames.Count / (float)columns);
            int totalRows = frameRows * 2;
            Texture2D composite = new Texture2D(
                CaptureWidth * columns,
                CaptureHeight * totalRows,
                TextureFormat.RGB24,
                false);
            try
            {
                Color[] background = Enumerable.Repeat(
                    new Color(0.055f, 0.065f, 0.08f, 1f),
                    composite.width * composite.height).ToArray();
                composite.SetPixels(background);
                for (int frame = 0; frame < frontFrames.Count; frame++)
                {
                    int blockRow = frame / columns;
                    int column = frame % columns;
                    SetCompositePanel(
                        composite,
                        frontFrames[frame],
                        column,
                        blockRow * 2,
                        totalRows);
                    SetCompositePanel(
                        composite,
                        sideFrames[frame],
                        column,
                        blockRow * 2 + 1,
                        totalRows);
                }

                composite.Apply(false, false);
                string absoluteOutput = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(absoluteOutput) ??
                    throw new InvalidOperationException(
                        "Player Hands paired frame output directory is unavailable."));
                File.WriteAllBytes(absoluteOutput, composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static void SetCompositePanel(
            Texture2D composite,
            byte[] encodedPanel,
            int column,
            int row,
            int totalRows)
        {
            Texture2D panel = new Texture2D(
                CaptureWidth,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                if (!panel.LoadImage(encodedPanel))
                {
                    throw new InvalidOperationException(
                        "Player Hands paired frame could not be decoded.");
                }

                composite.SetPixels(
                    column * CaptureWidth,
                    (totalRows - row - 1) * CaptureHeight,
                    CaptureWidth,
                    CaptureHeight,
                    panel.GetPixels());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panel);
            }
        }

        private static AnimationClip LoadClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path) ??
                   throw new FileNotFoundException(
                       "Required player animation clip is missing.",
                       Path.GetFullPath(path));
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
            HashSet<string> targetNames = new HashSet<string>(StringComparer.Ordinal)
            {
                EmptyTargetName,
                OneHandTargetName,
                TwoHandTargetName
            };
            return layout.GetComponentsInChildren<Animator>(true)
                .Where(animator => !targetNames.Contains(animator.name))
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

        private static Dictionary<string, string> CaptureAnimatorsExceptCarryTargets(
            Transform layout)
        {
            return layout.GetComponentsInChildren<Animator>(true)
                .Where(animator =>
                    !string.Equals(animator.name, OneHandTargetName, StringComparison.Ordinal) &&
                    !string.Equals(animator.name, TwoHandTargetName, StringComparison.Ordinal))
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

        private static Dictionary<string, string> CaptureAnimatorsExceptTarget(
            Transform layout,
            string excludedTargetName)
        {
            return layout.GetComponentsInChildren<Animator>(true)
                .Where(animator =>
                    !string.Equals(
                        animator.name,
                        excludedTargetName,
                        StringComparison.Ordinal))
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

        private static Dictionary<string, string> CaptureAnimatorsExceptTargets(
            Transform layout,
            params string[] excludedTargetNames)
        {
            HashSet<string> excluded = new HashSet<string>(
                excludedTargetNames,
                StringComparer.Ordinal);
            return layout.GetComponentsInChildren<Animator>(true)
                .Where(animator => !excluded.Contains(animator.name))
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

        private static bool HashMatches(
            string originalPath,
            string copyPath,
            string expectedHash)
        {
            return string.Equals(
                       HashFile(originalPath),
                       expectedHash,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       HashFile(copyPath),
                       expectedHash,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static void RequireHash(string path, string expectedHash, string label)
        {
            string actualHash = HashFile(path);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    label + " hash differs. Expected=" + expectedHash +
                    ", Actual=" + actualHash + ".");
            }
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
                    "Player Hands metrics directory is unavailable."));
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
                    "Required Player Hands metrics file is missing.",
                    absolute);
            }

            T result = JsonUtility.FromJson<T>(File.ReadAllText(absolute, Encoding.UTF8));
            return result ?? throw new InvalidOperationException(
                "Player Hands metrics file could not be decoded: " + absolute);
        }

        private static void CopyReviewedContact(string source, string destination)
        {
            string absoluteSource = Path.GetFullPath(source);
            string absoluteDestination = Path.GetFullPath(destination);
            if (!File.Exists(absoluteSource))
            {
                throw new FileNotFoundException(
                    "Reviewed Player Hands contact sheet is missing.",
                    absoluteSource);
            }

            Directory.CreateDirectory(
                Path.GetDirectoryName(absoluteDestination) ??
                throw new InvalidOperationException(
                    "Player Hands final output directory is unavailable."));
            File.Copy(absoluteSource, absoluteDestination, true);
        }

        private static string Num(float value)
        {
            return value.ToString("0.#########", CultureInfo.InvariantCulture);
        }
    }
}
