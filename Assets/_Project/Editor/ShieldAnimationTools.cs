using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class ShieldAnimationTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceModelPath = "Assets/_Project/Art/Player/Animations/Shield_Transfer_Mixamo.fbx";
        private const string ImpactSourceModelPath = AssetFolder + "/Shield_Block_Impact_Mixamo.fbx";
        private const string BreakSourceModelPath = AssetFolder + "/Shield_Break_Reaction_Mixamo.fbx";
        private const string ShieldModelPath = "Assets/_Project/Art/Items/Shield/shield.fbx";
        private const string IdleLegsClipPath = "Assets/_Project/Art/Player/Animations/Hands_Empty_Idle.anim";
        private const string BreathingClipPath = "Assets/_Project/Art/Player/Animations/Hands_Carry_OneHand_ArmAdjusted.anim";
        private const string AssetFolder = "Assets/_Project/Art/Player/Animations/Shield";
        private const string ControllerPath = AssetFolder + "/Shield_AllStates.controller";
        private const string DrawControllerPath = AssetFolder + "/Shield_Draw.controller";
        private const string RaiseControllerPath = AssetFolder + "/Shield_Raise.controller";
        private const string BlockIdleControllerPath = AssetFolder + "/Shield_Block_Idle.controller";
        private const string LowerControllerPath = AssetFolder + "/Shield_Lower.controller";
        private const string ImpactControllerPath = AssetFolder + "/Shield_Block_Impact.controller";
        private const string BreakControllerPath = AssetFolder + "/Shield_Break_Reaction.controller";
        private const string DrawClipPath = AssetFolder + "/Shield_Draw.anim";
        private const string RaiseClipPath = AssetFolder + "/Shield_Raise.anim";
        private const string LowerClipPath = AssetFolder + "/Shield_Lower.anim";
        private const string BlockIdleClipPath = AssetFolder + "/Shield_Block_Idle.anim";
        private const string LegMaskPath = AssetFolder + "/Shield_LowerBody.mask";
        private const string UpperBodyMaskPath = AssetFolder + "/Shield_UpperBody.mask";
        private const string ImportedClipName = "Shield_Transfer_Mixamo";
        private const string ImpactImportedClipName = "Shield_Block_Impact_Mixamo";
        private const string BreakImportedClipName = "Shield_Break_Reaction_Mixamo";
        private const string ShieldObjectName = "Shield_RightForeArm";
        private const string OutputFolder = "docs/validation/shield_state_motion_set_2026-09-07";
        private const string GroundImpactBreakOutputFolder =
            "docs/validation/shield_ground_impact_break_2026-09-08";
        private const string DrawFollowOutputFolder =
            "docs/validation/shield_draw_2cm_idle_vertical_2026-09-08";
        // Measured with the established inward RightArm pose and used to keep only 2 cm of handle insertion.
        private const float ForearmSkinBeyondFrontBoneMeters = 0.06261f;
        private const float RequiredHandleForearmOverlapMeters = 0.02f;
        private const float MaximumForearmCenterRightOffsetMeters = 0.22f;
        private const float ForearmCenterDownwardOffsetMeters = 0.05f;
        private const float AdditionalShieldForwardOffsetMeters = 0.01f;
        private const float DrawDurationSeconds = 1f;
        private const float RaiseDurationSeconds = 0.8f;
        private const float RaiseLowerEndHoldSeconds = 0.5f;
        private const float RaiseLowerCycleSeconds = RaiseDurationSeconds + RaiseLowerEndHoldSeconds;
        private const float ImpactShieldForwardOffsetMeters = 0f;
        private const float BreakShieldForwardOffsetMeters = 0.10f;
        private const float ImpactArmTowardShieldMeters = 0.10f;
        private const float MaximumFollowYawDegrees = 30f;
        private const float DrawMaximumVerticalTiltDegrees = 10f;
        private const float DrawStartForearmRightOffsetMeters = 0.36f;
        private static readonly Vector3 RaisedArmOffsetMeters = new Vector3(0f, 0.05f, 0.10f);
        private const float ObservationSeconds = 8f;
        private const int CaptureWidth = 256;
        private const int CaptureHeight = 320;

        private static readonly string[] TargetNames =
        {
            "Shield_Draw",
            "Shield_Idle",
            "Shield_Stow",
            "Shield_Raise",
            "Shield_Block_Idle",
            "Shield_Lower",
            "Shield_Block_Impact",
            "Shield_Break_Reaction"
        };

        private static readonly string[] LegRoots =
        {
            "Armature/Hips/LeftUpLeg",
            "Armature/Hips/RightUpLeg"
        };
        private const string HipsPath = "Armature/Hips";

        private static bool observing;
        private static double observationEnd;
        private static float[] observationStartBase;
        private static int[] observationStartMotionCycles;
        private static int[] observedBaseLoops;
        private static float maximumShieldCenterError;
        private static float minimumShieldDesiredFaceAlignment;
        private static float minimumShieldVerticalAlignment;
        private static float maximumBodyBoundsRatio;
        private static float maximumLocalScaleChange;
        private static float minimumBlockIdleBreathingPitch;
        private static float maximumBlockIdleBreathingPitch;
        private static float[] initialBodyBoundsMagnitude;
        private static string[][] observedScalePaths;
        private static Vector3[][] initialLocalScales;

        private static bool capturing;
        private static int capturePhase;
        private static double nextCaptureTime;
        private static string captureOutputPath;
        private static string captureDirectory;
        private static float captureInterval;

        private static bool observingGroundImpactBreak;
        private static double groundImpactBreakObservationEnd;
        private static double previousGroundImpactBreakSampleTime;
        private static double nextGroundImpactGeometrySampleTime;
        private static float maximumRaiseLeftFootError;
        private static float maximumRaiseRightFootError;
        private static float maximumLowerLeftFootError;
        private static float maximumLowerRightFootError;
        private static float maximumRaisePreGroundDelta;
        private static float maximumLowerPreGroundDelta;
        private static float maximumRaiseRigidBodyDrop;
        private static float maximumLowerRigidBodyDrop;
        private static float currentRaiseHoldSeconds;
        private static float currentLowerHoldSeconds;
        private static float maximumRaiseHoldSeconds;
        private static float maximumLowerHoldSeconds;
        private static float minimumRaiseHoldNormalizedTime;
        private static float maximumLowerHoldNormalizedTime;
        private static float maximumImpactForearmRotationError;
        private static float minimumImpactHandleOverlap;
        private static float maximumImpactHandleOverlap;
        private static float maximumBreakForearmRotationError;
        private static float maximumImpactLegFrontProtrusion;
        private static float maximumImpactLowerPositionError;
        private static float maximumImpactLowerRotationError;
        private static float maximumImpactLowerScaleError;
        private static float maximumBreakLowerPositionError;
        private static float maximumBreakLowerRotationError;
        private static float maximumBreakLowerScaleError;
        private static float maximumBreakSpineMotionDegrees;
        private static float maximumBreakForearmMotionDegrees;
        private static Quaternion initialBreakSpineRotation;
        private static Quaternion initialBreakForearmRotation;

        private static readonly float[] DrawFollowCapturePhaseTimesSeconds =
            { 0.03f, 0.20f, 0.40f, 0.60f, 0.80f, 0.97f };
        private static bool observingDrawFollow;
        private static int drawFollowObservationStartCycle;
        private static int drawFollowPreviousCycle;
        private static float maximumDrawVerticalTiltDegrees;
        private static float maximumDrawForearmRotationErrorDegrees;
        private static float bestDrawStartRightAlignment;
        private static float bestDrawEndForwardAlignment;
        private static float bestDrawEndVerticalAlignment;
        private static float minimumDrawEndIdleBasisErrorDegrees;
        private static float minimumDrawFaceYawDegrees;
        private static float maximumDrawFaceYawDegrees;
        private static float minimumDrawHandleOverlapMeters;
        private static float maximumDrawHandleOverlapMeters;
        private static float maximumDrawScaleChange;
        private static int drawForearmMotionSamples;
        private static int drawCoupledRotationSamples;
        private static Quaternion previousDrawForearmRotation;
        private static Quaternion previousDrawShieldRotation;
        private static string[] drawObservedScalePaths;
        private static Vector3[] drawInitialLocalScales;
        private static bool capturingDrawFollow;
        private static int drawFollowCapturePhase;
        private static int drawFollowCaptureCycle;
        private static bool drawFollowCaptureWaitingForNextCycle;
        private static string drawFollowCaptureOutputPath;
        private static string drawFollowCaptureFrameFolder;

        public static void Apply()
        {
            var scene = RequireScene();
            Directory.CreateDirectory(Absolute(OutputFolder));
            Directory.CreateDirectory(Absolute(AssetFolder));

            string sourceTakeName = ConfigureSourceImporter(SourceModelPath, ImportedClipName);
            string impactTakeName = ConfigureSourceImporter(ImpactSourceModelPath, ImpactImportedClipName);
            string breakTakeName = ConfigureSourceImporter(BreakSourceModelPath, BreakImportedClipName);
            var sourceClip = RequireSourceClip();
            var impactClip = RequireImportedClip(ImpactSourceModelPath, ImpactImportedClipName);
            var breakClip = RequireImportedClip(BreakSourceModelPath, BreakImportedClipName);
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleLegsClipPath) ??
                throw new InvalidOperationException("Hands_Empty_Idle clip is missing.");
            var breathingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BreathingClipPath) ??
                throw new InvalidOperationException("Hands_Carry_OneHand breathing source clip is missing.");
            var targets = TargetNames.Select(name => FindUnique(scene, name)).ToArray();
            var shieldIdleReference = targets.Single(target => target.name == "Shield_Idle");
            RequireSourceBindings(targets[0].transform, sourceClip);
            RequireSourceBindings(targets[0].transform, impactClip);
            RequireSourceBindings(targets[0].transform, breakClip);
            RequireIdleLegBindings(targets[0].transform, idleClip);

            var unchangedController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("Existing Shield_Idle/Shield_Stow controller is missing.");

            var drawClip = CreateShieldTransitionClip(targets[0], sourceClip, idleClip,
                DrawClipPath, DrawDurationSeconds, false);
            var raiseClip = CreateShieldTransitionClip(targets[0], sourceClip, idleClip,
                RaiseClipPath, RaiseDurationSeconds, true);
            var lowerClip = CopyAnimationClipAsset(RaiseClipPath, LowerClipPath);
            var blockIdleClip = CreateBlockIdleClip(targets[0], raiseClip, breathingClip);
            var drawController = CreateSingleController(DrawControllerPath, "ShieldDraw", drawClip, 1f, 0f);
            var raiseController = CreateSingleController(RaiseControllerPath, "ShieldRaise", raiseClip, 1f, 0f);
            var blockIdleController = CreateSingleController(BlockIdleControllerPath, "ShieldBlockIdle",
                blockIdleClip, 1f, 0f);
            var lowerController = CreateSingleController(LowerControllerPath, "ShieldLowerReverse",
                lowerClip, 1f, 0f);
            var impactController = CreateSingleController(ImpactControllerPath, "ShieldBlockImpactOriginal",
                impactClip, 1f, 0f);
            var breakController = CreateSingleController(BreakControllerPath, "ShieldBreakReactionOriginal",
                breakClip, 1f, 0f);
            string[] otherRootSignatures = OtherRootSignatures(scene);
            var report = new StringBuilder();
            report.AppendLine("Shield state-specific animation application.");
            report.AppendLine("sourceTake=" + sourceTakeName +
                " sourceClip=" + sourceClip.name +
                " sourceLength=" + sourceClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " sourceLoop=" + sourceClip.isLooping +
                " sourceCurveCount=" + AnimationUtility.GetCurveBindings(sourceClip).Length +
                " sourceCurveDigest=" + CurveDigest(sourceClip));
            report.AppendLine("idleLegClip=" + idleClip.name +
                " idleLength=" + idleClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " idleLoop=" + idleClip.isLooping +
                " idleLegCurveCount=" + LegBindings(idleClip).Length +
                " idleLegCurveDigest=" + CurveDigest(idleClip, onlyLegs: true));
            report.AppendLine("impactTake=" + impactTakeName + " clip=" + impactClip.name +
                " length=" + impactClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " directImportedClip=True digest=" + CurveDigest(impactClip));
            report.AppendLine("breakTake=" + breakTakeName + " clip=" + breakClip.name +
                " length=" + breakClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " directImportedClip=True digest=" + CurveDigest(breakClip));
            report.AppendLine("raiseLowerMotionSeconds=0.800000 endHoldSeconds=0.500000 cycleSeconds=1.300000 raisedArmUpMeters=0.050000 raisedArmForwardMeters=0.100000");
            report.AppendLine("drawDurationSeconds=1.000000 drawFinalUsesShieldIdleSourceAndLegClips=True");
            report.AppendLine("blockIdleBreathingReferenceInspected=" + BreathingClipPath +
                " breathingReferenceSpineCurveCount=" + AnimationUtility.GetCurveBindings(breathingClip)
                    .Count(binding => binding.type == typeof(Transform) && IsBreathingPath(binding.path)) +
                " breathingMode=ProceduralChestPitch breathingPitchDegrees=1.500000 noPositionOrScaleChange=True");
            report.AppendLine("lowerCopiedFromRaiseAsset=True lowerReversePlayback=True raiseDigest=" +
                CurveDigest(raiseClip) + " lowerDigest=" + CurveDigest(lowerClip));
            if (!string.Equals(CurveDigest(raiseClip), CurveDigest(lowerClip), StringComparison.Ordinal))
                throw new InvalidOperationException("Shield_Lower copied clip differs from Shield_Raise.");

            foreach (var target in targets)
            {
                if (target.name == "Shield_Idle" || target.name == "Shield_Stow")
                {
                    var unchangedAnimator = target.GetComponent<Animator>() ??
                        throw new InvalidOperationException(target.name + " Animator is missing.");
                    if (unchangedAnimator.runtimeAnimatorController != unchangedController)
                        throw new InvalidOperationException(target.name + " must retain the established controller.");
                    report.AppendLine(target.name + " unchanged=True controller=" + ControllerPath);
                    continue;
                }

                AnimatorController targetController;
                AnimationClip targetClip;
                ShieldStateMotionKind motionKind;
                float duration;
                switch (target.name)
                {
                    case "Shield_Draw":
                        targetController = drawController;
                        targetClip = drawClip;
                        motionKind = ShieldStateMotionKind.Draw;
                        duration = DrawDurationSeconds;
                        break;
                    case "Shield_Raise":
                        targetController = raiseController;
                        targetClip = raiseClip;
                        motionKind = ShieldStateMotionKind.Raise;
                        duration = RaiseLowerCycleSeconds;
                        break;
                    case "Shield_Block_Idle":
                        targetController = blockIdleController;
                        targetClip = blockIdleClip;
                        motionKind = ShieldStateMotionKind.BlockIdle;
                        duration = blockIdleClip.length;
                        break;
                    case "Shield_Lower":
                        targetController = lowerController;
                        targetClip = lowerClip;
                        motionKind = ShieldStateMotionKind.Lower;
                        duration = RaiseLowerCycleSeconds;
                        break;
                    case "Shield_Block_Impact":
                        targetController = impactController;
                        targetClip = impactClip;
                        motionKind = ShieldStateMotionKind.Impact;
                        duration = impactClip.length;
                        break;
                    case "Shield_Break_Reaction":
                        targetController = breakController;
                        targetClip = breakClip;
                        motionKind = ShieldStateMotionKind.BreakReaction;
                        duration = breakClip.length;
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected Shield state target " + target.name + ".");
                }

                var animator = ConfigureAnimator(target, targetController, targetClip);
                var forearm = RequireNamedTransform(target.transform, "RightForeArm");
                var hand = RequireNamedTransform(target.transform, "RightHand");
                var spine = RequireNamedTransform(target.transform, "Spine");
                var rightShoulder = RequireNamedTransform(target.transform, "RightShoulder");
                var rightArm = RequireNamedTransform(target.transform, "RightArm");
                Transform shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
                var follower = shield.GetComponent<ShieldForearmFollower>() ??
                    throw new InvalidOperationException(target.name + " shield follower is missing.");
                ShieldFacingTransition facing = target.name == "Shield_Draw"
                    ? ShieldFacingTransition.RightToForward
                    : target.name == "Shield_Block_Impact" || target.name == "Shield_Break_Reaction"
                        ? ShieldFacingTransition.FollowForearm
                        : ShieldFacingTransition.Forward;
                follower.ConfigureFacing(facing, duration);
                follower.ConfigureFollowYawLimit(MaximumFollowYawDegrees);
                follower.ConfigureAdditionalForwardOffset(target.name == "Shield_Block_Impact"
                    ? ImpactShieldForwardOffsetMeters
                    : target.name == "Shield_Break_Reaction"
                        ? BreakShieldForwardOffsetMeters
                        : AdditionalShieldForwardOffsetMeters);
                follower.ConfigureForearmSurface(ForearmLocalSurfacePoints(target, forearm));

                var stateMotion = target.GetComponent<ShieldStateMotion>() ?? target.AddComponent<ShieldStateMotion>();
                var armClearance = target.GetComponent<ShieldRightArmClearance>() ??
                    throw new InvalidOperationException(target.name + " arm clearance component is missing.");
                var headForward = target.GetComponent<ShieldHeadForward>() ??
                    throw new InvalidOperationException(target.name + " head-forward component is missing.");
                bool preserveOriginalUpperBody = motionKind == ShieldStateMotionKind.BreakReaction;
                armClearance.ConfigureRuntimeCorrection(!preserveOriginalUpperBody);
                headForward.ConfigureRuntimeAlignment(!preserveOriginalUpperBody);
                Vector3 targetArmOffset = motionKind == ShieldStateMotionKind.Impact
                    ? RaisedArmOffsetMeters + new Vector3(0f, 0f, ImpactArmTowardShieldMeters)
                    : RaisedArmOffsetMeters;
                stateMotion.ConfigureCommon(motionKind, animator, target.transform, spine, rightShoulder, rightArm,
                    forearm, hand, duration, DrawStartForearmRightOffsetMeters, targetArmOffset);
                stateMotion.ConfigureTimedPlayback(
                    motionKind == ShieldStateMotionKind.Raise || motionKind == ShieldStateMotionKind.Lower
                        ? RaiseDurationSeconds
                        : duration,
                    motionKind == ShieldStateMotionKind.Raise || motionKind == ShieldStateMotionKind.Lower
                        ? RaiseLowerEndHoldSeconds
                        : 0f,
                    motionKind == ShieldStateMotionKind.Lower);
                ConfigurePositionLocks(stateMotion, target, targetClip,
                    preserveOriginalUpperBody ? IsLegPath : IsSkeletonPath);
                stateMotion.ConfigureBaseline(Array.Empty<Transform>(), Array.Empty<Vector3>(),
                    Array.Empty<Quaternion>(), Array.Empty<Vector3>(), Array.Empty<Quaternion>(), false);
                stateMotion.ConfigureRotationLocks(Array.Empty<Transform>(), Array.Empty<Quaternion>());
                if (motionKind == ShieldStateMotionKind.Impact)
                {
                    ConfigureBaselineMotion(stateMotion, target, impactClip, blockIdleClip,
                        IsArmPath, true);
                    ConfigureRotationLocksFromDesired(stateMotion, target, impactClip, blockIdleClip, IsLegPath);
                }
                else if (motionKind == ShieldStateMotionKind.BreakReaction)
                    ConfigureRotationLocksFromDesired(stateMotion, target, breakClip, blockIdleClip, IsLegPath);
                ConfigureFootGrounding(stateMotion, target, shieldIdleReference,
                    motionKind == ShieldStateMotionKind.Raise || motionKind == ShieldStateMotionKind.Lower);

                EditorUtility.SetDirty(follower);
                EditorUtility.SetDirty(stateMotion);
                EditorUtility.SetDirty(armClearance);
                EditorUtility.SetDirty(headForward);
                report.AppendLine(target.name +
                    " animatorController=" + AssetDatabase.GetAssetPath(targetController) +
                    " connectedClip=" + AssetDatabase.GetAssetPath(targetClip) +
                    " connectedClipDirect=" + ControllerUsesClip(targetController, 0, targetClip) +
                    " motionKind=" + motionKind +
                    " durationSeconds=" + duration.ToString("F6", CultureInfo.InvariantCulture) +
                    " shieldFollowerPreserved=True approvedSurfacePreserved=True");
            }

            if (!otherRootSignatures.SequenceEqual(OtherRootSignatures(scene), StringComparer.Ordinal))
                throw new InvalidOperationException("A scene root outside the eight Shield targets changed.");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after Shield application.");
            report.AppendLine("otherSceneRootsUnchanged=True sceneSaved=True");
            report.AppendLine("Draw starts with the arm on transporter-right and the shield exterior facing right, then reaches the unchanged Shield_Idle arm and shield basis at 1.0 seconds.");
            report.AppendLine("Raise and Lower complete the transition in 0.8 seconds, hold the final pose for 0.5 seconds, keep both foot targets at Shield_Idle height through rotational two-bone grounding, and repeat every 1.3 seconds.");
            report.AppendLine("Lower uses an asset copy of Raise and samples that copy in reverse without position or scale edits.");
            report.AppendLine("Impact keeps the shield placement while moving the complete right-arm chain 10 cm toward its handle; the handle overlaps the forearm by the established 2 cm target.");
            report.AppendLine("Break references the newly copied imported Mixamo clip directly from Spine upward with arm clearance, arm lowering and head-forward corrections disabled; only Hips and both legs are pose-locked.");
            report.AppendLine("Source FBX files, source curves, target meshes, UVs, rigs, skin weights, blendshapes, Shield materials and Shield textures were not modified.");
            File.WriteAllText(Absolute(OutputFolder + "/applied.txt"), report.ToString(), Encoding.UTF8);
            Debug.Log("Six requested Shield states now use their state-specific motions; Shield_Idle and Shield_Stow remain unchanged.");
        }

        public static void ApplyShieldDrawVerticalForearmFollow()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Shield_Draw follow configuration must be applied outside Play Mode.");
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, "Shield_Draw");
            Animator animator = RequireConfiguredAnimator(target);
            AnimationClip drawClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DrawClipPath) ??
                throw new InvalidOperationException("Shield_Draw clip is missing.");
            if (!ControllerUsesClip(animator.runtimeAnimatorController as AnimatorController, 0, drawClip))
                throw new InvalidOperationException("Shield_Draw controller no longer references its established clip.");
            Transform forearm = RequireNamedTransform(target.transform, "RightForeArm");
            Transform shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
            ShieldForearmFollower follower = shield.GetComponent<ShieldForearmFollower>() ??
                throw new InvalidOperationException("Shield_Draw shield follower is missing.");
            follower.ConfigureFacing(ShieldFacingTransition.RightToForward, DrawDurationSeconds);
            follower.ConfigureVerticalTiltLimit(DrawMaximumVerticalTiltDegrees);
            follower.ConfigureAdditionalForwardOffset(0f);
            EditorUtility.SetDirty(follower);
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after Shield_Draw follow configuration.");

            Directory.CreateDirectory(Absolute(DrawFollowOutputFolder));
            var report = new StringBuilder();
            report.AppendLine("Shield_Draw forearm-driven shield rotation applied.");
            report.AppendLine("target=Shield_Draw facingTransition=RightToForward rotationDriver=liveRightForearmDelta");
            report.AppendLine("maximumVerticalTiltDegrees=" +
                DrawMaximumVerticalTiltDegrees.ToString("F6", CultureInfo.InvariantCulture));
            report.AppendLine("positionDriver=liveRightForearmAndRightHand handleContactPreserved=True");
            report.AppendLine("requiredHandleOverlapMeters=0.020000 additionalForwardOffsetMeters=0.000000");
            report.AppendLine("finalPoseUsesShieldIdleForwardAndVerticalBasis=True finalBasisStartProgress=0.950000");
            report.AppendLine("drawClip=" + DrawClipPath + " curveDigest=" + CurveDigest(drawClip));
            report.AppendLine("drawController=" + DrawControllerPath + " connectedClipDirect=True");
            report.AppendLine("sourceAnimationMeshRigAndMaterialsUnchanged=True sceneSaved=True");
            File.WriteAllText(Absolute(DrawFollowOutputFolder + "/applied.txt"),
                report.ToString(), Encoding.UTF8);
            Debug.Log("Shield_Draw shield now follows live forearm rotation with a ten-degree vertical limit.");
        }

        public static void InspectShieldDrawVerticalForearmFollow()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
                Debug.Log("Shield_Draw live follow inspection entered Play Mode; invoke inspection again after transition.");
                return;
            }
            if (observingDrawFollow)
                throw new InvalidOperationException("Shield_Draw live follow inspection is already running.");

            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, "Shield_Draw");
            ShieldStateMotion motion = target.GetComponent<ShieldStateMotion>() ??
                throw new InvalidOperationException("Shield_Draw state motion is missing.");
            Transform forearm = RequireNamedTransform(target.transform, "RightForeArm");
            Transform shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
            ShieldForearmFollower follower = shield.GetComponent<ShieldForearmFollower>() ??
                throw new InvalidOperationException("Shield_Draw shield follower is missing.");
            RequireDrawFollowConfiguration(follower);

            drawFollowObservationStartCycle = motion.CompletedCycleCount;
            drawFollowPreviousCycle = motion.CompletedCycleCount;
            maximumDrawVerticalTiltDegrees = 0f;
            maximumDrawForearmRotationErrorDegrees = 0f;
            bestDrawStartRightAlignment = -1f;
            bestDrawEndForwardAlignment = -1f;
            bestDrawEndVerticalAlignment = -1f;
            minimumDrawEndIdleBasisErrorDegrees = float.PositiveInfinity;
            minimumDrawFaceYawDegrees = float.PositiveInfinity;
            maximumDrawFaceYawDegrees = float.NegativeInfinity;
            minimumDrawHandleOverlapMeters = float.PositiveInfinity;
            maximumDrawHandleOverlapMeters = float.NegativeInfinity;
            maximumDrawScaleChange = 0f;
            drawForearmMotionSamples = 0;
            drawCoupledRotationSamples = 0;
            previousDrawForearmRotation = forearm.rotation;
            previousDrawShieldRotation = shield.rotation;
            drawObservedScalePaths = target.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, target.transform))
                .Where(IsSkeletonPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            drawInitialLocalScales = drawObservedScalePaths
                .Select(path => target.transform.Find(path).localScale)
                .ToArray();
            Directory.CreateDirectory(Absolute(DrawFollowOutputFolder));
            string incompletePath = Absolute(DrawFollowOutputFolder + "/runtime_metrics_incomplete.txt");
            if (File.Exists(incompletePath)) File.Delete(incompletePath);
            observingDrawFollow = true;
            EditorApplication.update += ObserveDrawFollowTick;
            Debug.Log("Read-only Shield_Draw live forearm-follow observation started.");
        }

        public static void CaptureShieldDrawVerticalForearmFollowReview(string outputPath)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Shield_Draw final capture requires live Play Mode.");
            string metricsPath = Absolute(DrawFollowOutputFolder + "/runtime_metrics.txt");
            if (!File.Exists(metricsPath) ||
                File.ReadAllText(metricsPath, Encoding.UTF8).IndexOf("overallPassed=True", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Shield_Draw runtime metrics must pass before the one final capture.");
            if (capturingDrawFollow)
                throw new InvalidOperationException("Shield_Draw final capture is already running.");

            drawFollowCaptureOutputPath = Path.IsPathRooted(outputPath) ? outputPath : Absolute(outputPath);
            if (File.Exists(drawFollowCaptureOutputPath))
                throw new InvalidOperationException("The one-time Shield_Draw final capture already exists: " +
                    drawFollowCaptureOutputPath);
            string outputDirectory = Path.GetDirectoryName(drawFollowCaptureOutputPath);
            if (string.IsNullOrEmpty(outputDirectory))
                throw new InvalidOperationException("Shield_Draw final capture output directory is invalid.");
            Directory.CreateDirectory(outputDirectory);
            string incompletePath = Path.Combine(outputDirectory, "capture_incomplete.txt");
            if (File.Exists(incompletePath)) File.Delete(incompletePath);
            drawFollowCaptureFrameFolder = Path.Combine(outputDirectory, "capture_frames_final");
            Directory.CreateDirectory(drawFollowCaptureFrameFolder);
            if (Directory.GetFiles(drawFollowCaptureFrameFolder, "draw_*.png").Length > 0)
                throw new InvalidOperationException("Shield_Draw capture frame folder is not empty.");

            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, "Shield_Draw");
            ShieldStateMotion motion = target.GetComponent<ShieldStateMotion>() ??
                throw new InvalidOperationException("Shield_Draw state motion is missing.");
            Transform forearm = RequireNamedTransform(target.transform, "RightForeArm");
            Transform shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
            RequireDrawFollowConfiguration(shield.GetComponent<ShieldForearmFollower>() ??
                throw new InvalidOperationException("Shield_Draw shield follower is missing."));
            drawFollowCaptureCycle = motion.CompletedCycleCount;
            drawFollowCapturePhase = -1;
            drawFollowCaptureWaitingForNextCycle = true;
            capturingDrawFollow = true;
            EditorApplication.update += CaptureDrawFollowTick;
            Debug.Log("Shield_Draw one-time final capture is waiting for the next unmodified loop.");
        }

        public static void StartReview()
        {
            var scene = RequireScene();
            foreach (string targetName in TargetNames)
                RequireConfiguredAnimator(FindUnique(scene, targetName));
            if (!EditorApplication.isPlaying)
                EditorApplication.isPlaying = true;
            Debug.Log("Shield animation review uses normal live playback without pose sampling.");
        }

        public static void StopReview()
        {
            if (observing)
            {
                EditorApplication.update -= ObserveTick;
                observing = false;
            }
            if (capturing)
            {
                EditorApplication.update -= CaptureTick;
                capturing = false;
            }
            if (observingDrawFollow)
            {
                EditorApplication.update -= ObserveDrawFollowTick;
                observingDrawFollow = false;
            }
            if (capturingDrawFollow)
            {
                EditorApplication.update -= CaptureDrawFollowTick;
                capturingDrawFollow = false;
            }
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            Debug.Log("Shield animation live review stopped.");
        }

        public static void Inspect()
        {
            var scene = RequireScene();
            var sourceClip = RequireSourceClip();
            var impactClip = RequireImportedClip(ImpactSourceModelPath, ImpactImportedClipName);
            var breakClip = RequireImportedClip(BreakSourceModelPath, BreakImportedClipName);
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleLegsClipPath) ??
                throw new InvalidOperationException("Hands_Empty_Idle clip is missing.");
            var breathingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BreathingClipPath) ??
                throw new InvalidOperationException("Hands_Carry_OneHand breathing source clip is missing.");
            var drawClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DrawClipPath) ??
                throw new InvalidOperationException("Shield_Draw clip is missing.");
            var raiseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RaiseClipPath) ??
                throw new InvalidOperationException("Shield_Raise clip is missing.");
            var lowerClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LowerClipPath) ??
                throw new InvalidOperationException("Shield_Lower copied clip is missing.");
            var blockIdleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(BlockIdleClipPath) ??
                throw new InvalidOperationException("Shield_Block_Idle clip is missing.");
            var targets = TargetNames.Select(name => FindUnique(scene, name)).ToArray();
            var report = new StringBuilder();
            report.AppendLine("Read-only Shield state source and connection inspection.");
            report.AppendLine("sourceClip=" + sourceClip.name +
                " length=" + sourceClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " loop=" + sourceClip.isLooping +
                " curveCount=" + AnimationUtility.GetCurveBindings(sourceClip).Length +
                " digest=" + CurveDigest(sourceClip));
            report.AppendLine("idleClip=" + idleClip.name +
                " length=" + idleClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " loop=" + idleClip.isLooping +
                " legCurveCount=" + LegBindings(idleClip).Length +
                " legDigest=" + CurveDigest(idleClip, onlyLegs: true));
            report.AppendLine("impactClip=" + impactClip.name + " directSource=True length=" +
                impactClip.length.ToString("F6", CultureInfo.InvariantCulture) + " digest=" + CurveDigest(impactClip));
            report.AppendLine("breakClip=" + breakClip.name + " directSource=True length=" +
                breakClip.length.ToString("F6", CultureInfo.InvariantCulture) + " digest=" + CurveDigest(breakClip));
            report.AppendLine("raiseClipLength=" + raiseClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " lowerClipLength=" + lowerClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " raiseLowerCurveDigestEqual=" + (CurveDigest(raiseClip) == CurveDigest(lowerClip)));
            report.AppendLine("drawClipLength=" + drawClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " drawFinalUsesShieldIdlePose=True");
            report.AppendLine("blockIdleLength=" + blockIdleClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " breathingSource=" + BreathingClipPath + " breathingSourceLength=" +
                breathingClip.length.ToString("F6", CultureInfo.InvariantCulture) +
                " breathingMode=ProceduralChestPitch breathingPitchDegrees=1.500000");
            foreach (var target in targets)
            {
                var animator = RequireConfiguredAnimator(target);
                var controller = animator.runtimeAnimatorController as AnimatorController ??
                    throw new InvalidOperationException(target.name + " controller is not an AnimatorController.");
                AnimationClip expectedClip = ExpectedClipForTarget(target.name, sourceClip, drawClip, raiseClip,
                    blockIdleClip, lowerClip, impactClip, breakClip);
                var forearm = RequireNamedTransform(target.transform, "RightForeArm");
                var hand = RequireNamedTransform(target.transform, "RightHand");
                var shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
                var follower = shield.GetComponent<ShieldForearmFollower>() ??
                    throw new InvalidOperationException(target.name + " shield follower is missing.");
                ShieldMetrics(shield, forearm, hand, target.transform,
                    out float centerError, out float forwardAlignment, out float verticalAlignment,
                    out Vector3 boundsSize, out _, out _);
                var stateMotion = target.GetComponent<ShieldStateMotion>();
                report.AppendLine(target.name +
                    " controller=" + AssetDatabase.GetAssetPath(controller) +
                    " animatorLayers=" + animator.layerCount +
                    " connectedClipDirect=" + ControllerUsesClip(controller, 0, expectedClip) +
                    " secondaryIdleLegClipDirect=" +
                    (controller.layers.Length > 1 && ControllerUsesClip(controller, 1, idleClip)) +
                    " motionKind=" + (stateMotion == null ? "EstablishedUnchanged" : stateMotion.MotionKind.ToString()) +
                    " configuredDurationSeconds=" +
                    (stateMotion == null ? "Established" : stateMotion.CycleDurationSeconds.ToString("F6", CultureInfo.InvariantCulture)) +
                    " handleForearmContactErrorMeters=" + centerError.ToString("F8", CultureInfo.InvariantCulture) +
                    " shieldForwardAlignment=" + forwardAlignment.ToString("F8", CultureInfo.InvariantCulture) +
                    " shieldVerticalAlignment=" + verticalAlignment.ToString("F8", CultureInfo.InvariantCulture) +
                    " followerDesiredFaceAlignment=" + follower.LastDesiredFaceAlignment.ToString("F8", CultureInfo.InvariantCulture) +
                    " shieldBoundsSize=" + Format(boundsSize));
            }
            File.WriteAllText(Absolute(OutputFolder + "/sources.txt"), report.ToString(), Encoding.UTF8);

            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("StartShieldAnimationReviewLoop must be active before runtime observation.");
            BeginObservation(targets);
            Debug.Log("Shield state source inspection complete; eight-second live observation started.");
        }

        public static void InspectGroundImpactBreakCorrections()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "StartShieldAnimationReviewLoop must be active before Shield ground/impact/break inspection.");
            if (observingGroundImpactBreak)
                throw new InvalidOperationException("Shield ground/impact/break inspection is already running.");

            Directory.CreateDirectory(Absolute(GroundImpactBreakOutputFolder));
            var scene = RequireScene();
            GameObject breakTarget = FindUnique(scene, "Shield_Break_Reaction");
            initialBreakSpineRotation = RequireNamedTransform(breakTarget.transform, "Spine").localRotation;
            initialBreakForearmRotation = RequireNamedTransform(breakTarget.transform, "RightForeArm").localRotation;
            maximumRaiseLeftFootError = 0f;
            maximumRaiseRightFootError = 0f;
            maximumLowerLeftFootError = 0f;
            maximumLowerRightFootError = 0f;
            maximumRaisePreGroundDelta = 0f;
            maximumLowerPreGroundDelta = 0f;
            maximumRaiseRigidBodyDrop = 0f;
            maximumLowerRigidBodyDrop = 0f;
            currentRaiseHoldSeconds = 0f;
            currentLowerHoldSeconds = 0f;
            maximumRaiseHoldSeconds = 0f;
            maximumLowerHoldSeconds = 0f;
            minimumRaiseHoldNormalizedTime = float.PositiveInfinity;
            maximumLowerHoldNormalizedTime = float.NegativeInfinity;
            maximumImpactForearmRotationError = 0f;
            minimumImpactHandleOverlap = float.PositiveInfinity;
            maximumImpactHandleOverlap = float.NegativeInfinity;
            maximumBreakForearmRotationError = 0f;
            maximumImpactLegFrontProtrusion = float.NegativeInfinity;
            maximumImpactLowerPositionError = 0f;
            maximumImpactLowerRotationError = 0f;
            maximumImpactLowerScaleError = 0f;
            maximumBreakLowerPositionError = 0f;
            maximumBreakLowerRotationError = 0f;
            maximumBreakLowerScaleError = 0f;
            maximumBreakSpineMotionDegrees = 0f;
            maximumBreakForearmMotionDegrees = 0f;
            previousGroundImpactBreakSampleTime = EditorApplication.timeSinceStartup;
            nextGroundImpactGeometrySampleTime = previousGroundImpactBreakSampleTime;
            groundImpactBreakObservationEnd = previousGroundImpactBreakSampleTime + 7.2d;
            observingGroundImpactBreak = true;
            EditorApplication.update += ObserveGroundImpactBreakTick;
            Debug.Log("Read-only Shield ground, impact and break correction observation started.");
        }

        private static void ObserveDrawFollowTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException("Play Mode ended during Shield_Draw follow observation.");
                Scene scene = RequireScene();
                GameObject target = FindUnique(scene, "Shield_Draw");
                ShieldStateMotion motion = target.GetComponent<ShieldStateMotion>() ??
                    throw new InvalidOperationException("Shield_Draw state motion disappeared.");
                Transform forearm = RequireNamedTransform(target.transform, "RightForeArm");
                Transform hand = RequireNamedTransform(target.transform, "RightHand");
                Transform shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
                ShieldForearmFollower follower = shield.GetComponent<ShieldForearmFollower>() ??
                    throw new InvalidOperationException("Shield_Draw shield follower disappeared.");
                RequireDrawFollowConfiguration(follower);
                GameObject idleTarget = FindUnique(scene, "Shield_Idle");
                Transform idleForearm = RequireNamedTransform(idleTarget.transform, "RightForeArm");
                Transform idleHand = RequireNamedTransform(idleTarget.transform, "RightHand");
                Transform idleShield = idleForearm.Cast<Transform>()
                    .Single(item => item.name == ShieldObjectName);

                ShieldMetrics(shield, forearm, hand, target.transform,
                    out _, out _, out _, out _, out Vector3 faceAxis, out Vector3 verticalAxis);
                ShieldMetrics(idleShield, idleForearm, idleHand, idleTarget.transform,
                    out _, out _, out _, out _, out Vector3 idleFaceAxis, out Vector3 idleVerticalAxis);
                Vector3 up = target.transform.up.normalized;
                Vector3 worldFace = shield.TransformDirection(faceAxis).normalized;
                Vector3 worldVertical = shield.TransformDirection(verticalAxis).normalized;
                Vector3 horizontalFace = Vector3.ProjectOnPlane(worldFace, up);
                if (horizontalFace.sqrMagnitude > 0.000001f)
                {
                    horizontalFace.Normalize();
                    float yaw = Vector3.SignedAngle(target.transform.right, horizontalFace, up);
                    minimumDrawFaceYawDegrees = Mathf.Min(minimumDrawFaceYawDegrees, yaw);
                    maximumDrawFaceYawDegrees = Mathf.Max(maximumDrawFaceYawDegrees, yaw);
                    if (motion.LastMotionProgress <= 0.08f)
                        bestDrawStartRightAlignment = Mathf.Max(bestDrawStartRightAlignment,
                            Vector3.Dot(horizontalFace, target.transform.right.normalized));
                    if (motion.LastMotionProgress >= 0.92f)
                    {
                        bestDrawEndForwardAlignment = Mathf.Max(bestDrawEndForwardAlignment,
                            Vector3.Dot(horizontalFace, target.transform.forward.normalized));
                        bestDrawEndVerticalAlignment = Mathf.Max(bestDrawEndVerticalAlignment,
                            Vector3.Dot(worldVertical, up));
                        Quaternion drawBasis = Quaternion.LookRotation(worldFace, worldVertical);
                        Quaternion idleBasis = Quaternion.LookRotation(
                            idleShield.TransformDirection(idleFaceAxis).normalized,
                            idleShield.TransformDirection(idleVerticalAxis).normalized);
                        minimumDrawEndIdleBasisErrorDegrees = Mathf.Min(
                            minimumDrawEndIdleBasisErrorDegrees, Quaternion.Angle(drawBasis, idleBasis));
                    }
                }
                maximumDrawVerticalTiltDegrees = Mathf.Max(maximumDrawVerticalTiltDegrees,
                    Vector3.Angle(worldVertical, up));
                maximumDrawForearmRotationErrorDegrees = Mathf.Max(maximumDrawForearmRotationErrorDegrees,
                    follower.LastForearmRotationErrorDegrees);
                if (follower.LastHandleOverlapMeters > 0f)
                {
                    minimumDrawHandleOverlapMeters = Mathf.Min(minimumDrawHandleOverlapMeters,
                        follower.LastHandleOverlapMeters);
                    maximumDrawHandleOverlapMeters = Mathf.Max(maximumDrawHandleOverlapMeters,
                        follower.LastHandleOverlapMeters);
                }

                if (motion.CompletedCycleCount == drawFollowPreviousCycle)
                {
                    float forearmStep = Quaternion.Angle(previousDrawForearmRotation, forearm.rotation);
                    float shieldStep = Quaternion.Angle(previousDrawShieldRotation, shield.rotation);
                    if (forearmStep > 0.01f)
                    {
                        drawForearmMotionSamples++;
                        if (shieldStep > 0.005f) drawCoupledRotationSamples++;
                    }
                }
                else drawFollowPreviousCycle = motion.CompletedCycleCount;
                previousDrawForearmRotation = forearm.rotation;
                previousDrawShieldRotation = shield.rotation;

                for (int index = 0; index < drawObservedScalePaths.Length; index++)
                {
                    Transform bone = target.transform.Find(drawObservedScalePaths[index]);
                    maximumDrawScaleChange = Mathf.Max(maximumDrawScaleChange,
                        Vector3.Distance(bone.localScale, drawInitialLocalScales[index]));
                }

                int observedCycles = motion.CompletedCycleCount - drawFollowObservationStartCycle;
                if (observedCycles < 3) return;
                EditorApplication.update -= ObserveDrawFollowTick;
                observingDrawFollow = false;

                float couplingRatio = drawForearmMotionSamples == 0 ? 0f :
                    drawCoupledRotationSamples / (float)drawForearmMotionSamples;
                float yawRange = maximumDrawFaceYawDegrees - minimumDrawFaceYawDegrees;
                bool verticalPassed = maximumDrawVerticalTiltDegrees <=
                    DrawMaximumVerticalTiltDegrees + 0.05f;
                bool endpointsPassed = bestDrawStartRightAlignment >= 0.98f &&
                    bestDrawEndForwardAlignment >= 0.9999f &&
                    bestDrawEndVerticalAlignment >= 0.999999f &&
                    minimumDrawEndIdleBasisErrorDegrees <= 0.05f;
                bool handleOverlapPassed = minimumDrawHandleOverlapMeters >= 0.0199f &&
                    maximumDrawHandleOverlapMeters <= 0.0201f;
                bool forearmDrivenPassed = maximumDrawForearmRotationErrorDegrees <= 0.01f &&
                    yawRange >= 70f && couplingRatio >= 0.75f;
                bool scalePassed = maximumDrawScaleChange <= 0.00001f;
                bool overallPassed = verticalPassed && endpointsPassed && handleOverlapPassed &&
                    forearmDrivenPassed && scalePassed;

                var report = new StringBuilder();
                report.AppendLine("Read-only live Shield_Draw forearm-follow observation.");
                report.AppendLine("observedCycles=" + observedCycles);
                report.AppendLine("rotationDriver=liveRightForearmDelta facingTransition=" + follower.FacingTransition);
                report.AppendLine("maximumAllowedVerticalTiltDegrees=" +
                    DrawMaximumVerticalTiltDegrees.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("maximumObservedVerticalTiltDegrees=" +
                    maximumDrawVerticalTiltDegrees.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("maximumForearmDrivenBasisErrorDegrees=" +
                    maximumDrawForearmRotationErrorDegrees.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("startRightFaceAlignment=" +
                    bestDrawStartRightAlignment.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("endForwardFaceAlignment=" +
                    bestDrawEndForwardAlignment.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("endVerticalAlignment=" +
                    bestDrawEndVerticalAlignment.ToString("F8", CultureInfo.InvariantCulture) +
                    " endShieldIdleBasisErrorDegrees=" +
                    minimumDrawEndIdleBasisErrorDegrees.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("handleOverlapMeters=" +
                    minimumDrawHandleOverlapMeters.ToString("F8", CultureInfo.InvariantCulture) + ".." +
                    maximumDrawHandleOverlapMeters.ToString("F8", CultureInfo.InvariantCulture) +
                    " required=0.02000000");
                report.AppendLine("observedFaceYawRangeDegrees=" +
                    minimumDrawFaceYawDegrees.ToString("F6", CultureInfo.InvariantCulture) + ".." +
                    maximumDrawFaceYawDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                    " span=" + yawRange.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("forearmMotionSamples=" + drawForearmMotionSamples +
                    " coupledShieldRotationSamples=" + drawCoupledRotationSamples +
                    " couplingRatio=" + couplingRatio.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("maximumSkeletonLocalScaleChange=" +
                    maximumDrawScaleChange.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("verticalLimitPassed=" + verticalPassed +
                    " endpointFacingPassed=" + endpointsPassed +
                    " handleOverlapPassed=" + handleOverlapPassed +
                    " forearmDrivenRotationPassed=" + forearmDrivenPassed +
                    " scalePreservationPassed=" + scalePassed);
                report.AppendLine("Observation did not sample, move, rotate, scale, enable or disable the target.");
                report.AppendLine("overallPassed=" + overallPassed);
                File.WriteAllText(Absolute(DrawFollowOutputFolder + "/runtime_metrics.txt"),
                    report.ToString(), Encoding.UTF8);
                if (!overallPassed)
                    throw new InvalidOperationException("Shield_Draw forearm-follow observation failed. See runtime_metrics.txt.");
                Debug.Log("Shield_Draw live forearm-follow observation passed.");
            }
            catch (Exception exception)
            {
                EditorApplication.update -= ObserveDrawFollowTick;
                observingDrawFollow = false;
                Directory.CreateDirectory(Absolute(DrawFollowOutputFolder));
                File.WriteAllText(Absolute(DrawFollowOutputFolder + "/runtime_metrics_incomplete.txt"),
                    exception.ToString(), Encoding.UTF8);
                Debug.LogException(exception);
            }
        }

        private static void CaptureDrawFollowTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException("Play Mode ended during Shield_Draw final capture.");
                Scene scene = RequireScene();
                GameObject target = FindUnique(scene, "Shield_Draw");
                ShieldStateMotion motion = target.GetComponent<ShieldStateMotion>() ??
                    throw new InvalidOperationException("Shield_Draw state motion disappeared.");
                if (drawFollowCapturePhase < 0)
                {
                    if (motion.CompletedCycleCount <= drawFollowCaptureCycle || motion.LastMotionProgress > 0.05f)
                        return;
                    drawFollowCaptureCycle = motion.CompletedCycleCount;
                    drawFollowCapturePhase = 0;
                    drawFollowCaptureWaitingForNextCycle = false;
                }
                if (drawFollowCaptureWaitingForNextCycle)
                {
                    if (motion.CompletedCycleCount <= drawFollowCaptureCycle) return;
                    drawFollowCaptureCycle = motion.CompletedCycleCount;
                    drawFollowCaptureWaitingForNextCycle = false;
                }
                if (motion.CompletedCycleCount != drawFollowCaptureCycle)
                    throw new InvalidOperationException("Shield_Draw final capture missed a phase before the loop ended.");
                if (motion.LastMotionProgress < DrawFollowCapturePhaseTimesSeconds[drawFollowCapturePhase]) return;

                string label = drawFollowCapturePhase.ToString("D2") + "_" +
                    motion.LastMotionProgress.ToString("F3", CultureInfo.InvariantCulture).Replace('.', '_');
                RenderSubject(target, target.transform.forward, target.transform.up,
                    Path.Combine(drawFollowCaptureFrameFolder, "draw_front_" + label + ".png"), false);
                RenderSubject(target, target.transform.right, target.transform.up,
                    Path.Combine(drawFollowCaptureFrameFolder, "draw_side_" + label + ".png"), false);
                RenderSubject(target, target.transform.forward, target.transform.up,
                    Path.Combine(drawFollowCaptureFrameFolder, "draw_close_" + label + ".png"), true);
                drawFollowCapturePhase++;
                if (drawFollowCapturePhase < DrawFollowCapturePhaseTimesSeconds.Length)
                {
                    drawFollowCaptureWaitingForNextCycle = true;
                    return;
                }

                EditorApplication.update -= CaptureDrawFollowTick;
                capturingDrawFollow = false;
                string[] front = Directory.GetFiles(drawFollowCaptureFrameFolder, "draw_front_*.png")
                    .OrderBy(path => path, StringComparer.Ordinal).ToArray();
                string[] side = Directory.GetFiles(drawFollowCaptureFrameFolder, "draw_side_*.png")
                    .OrderBy(path => path, StringComparer.Ordinal).ToArray();
                string[] close = Directory.GetFiles(drawFollowCaptureFrameFolder, "draw_close_*.png")
                    .OrderBy(path => path, StringComparer.Ordinal).ToArray();
                int count = DrawFollowCapturePhaseTimesSeconds.Length;
                if (front.Length != count || side.Length != count || close.Length != count)
                    throw new InvalidOperationException("Shield_Draw final capture phase count is incomplete.");
                GameObject idleTarget = FindUnique(scene, "Shield_Idle");
                string idleFront = Path.Combine(drawFollowCaptureFrameFolder, "idle_reference_front.png");
                string idleSide = Path.Combine(drawFollowCaptureFrameFolder, "idle_reference_side.png");
                string idleClose = Path.Combine(drawFollowCaptureFrameFolder, "idle_reference_close.png");
                RenderSubject(idleTarget, idleTarget.transform.forward, idleTarget.transform.up,
                    idleFront, false);
                RenderSubject(idleTarget, idleTarget.transform.right, idleTarget.transform.up,
                    idleSide, false);
                RenderSubject(idleTarget, idleTarget.transform.forward, idleTarget.transform.up,
                    idleClose, true);
                Compose(front.Concat(side).Concat(close)
                    .Concat(new[] { idleFront, idleSide, idleClose }).ToArray(),
                    count, drawFollowCaptureOutputPath);
                var manifest = new StringBuilder();
                manifest.AppendLine("Unmodified live Shield_Draw playback.");
                manifest.AppendLine("Rows: transporter front, transporter right side, right-arm close front, then Shield_Idle front/side/close references in the final row.");
                manifest.AppendLine("Requested cycle times: " + string.Join(", ",
                    DrawFollowCapturePhaseTimesSeconds.Select(time =>
                        time.ToString("F2", CultureInfo.InvariantCulture))));
                manifest.AppendLine("Shield rotation follows live RightForeArm delta, keeps exactly 2 cm handle overlap, and matches the Shield_Idle forward/vertical basis from 0.95 seconds.");
                manifest.AppendLine("No target pose, transform, renderer state, mesh, rig, material or animation curve was changed for capture.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(drawFollowCaptureOutputPath),
                    "capture_manifest.txt"), manifest.ToString(), Encoding.UTF8);
                Debug.Log("Shield_Draw one-time final capture complete: " + drawFollowCaptureOutputPath);
            }
            catch (Exception exception)
            {
                EditorApplication.update -= CaptureDrawFollowTick;
                capturingDrawFollow = false;
                string directory = string.IsNullOrEmpty(drawFollowCaptureOutputPath)
                    ? Absolute(DrawFollowOutputFolder)
                    : Path.GetDirectoryName(drawFollowCaptureOutputPath);
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "capture_incomplete.txt"),
                    exception.ToString(), Encoding.UTF8);
                Debug.LogException(exception);
            }
        }

        private static void RequireDrawFollowConfiguration(ShieldForearmFollower follower)
        {
            if (follower.FacingTransition != ShieldFacingTransition.RightToForward)
                throw new InvalidOperationException("Shield_Draw does not use its forearm-driven right-to-forward mode.");
            if (Mathf.Abs(follower.MaximumVerticalTiltDegrees - DrawMaximumVerticalTiltDegrees) > 0.0001f)
                throw new InvalidOperationException("Shield_Draw vertical tilt limit is not ten degrees.");
            if (Mathf.Abs(follower.AdditionalForwardOffsetMeters) > 0.0001f)
                throw new InvalidOperationException("Shield_Draw still has an additional forward offset that reduces handle overlap.");
        }

        private static void ObserveGroundImpactBreakTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException("Shield review stopped during correction observation.");
                double now = EditorApplication.timeSinceStartup;
                float deltaSeconds = Mathf.Clamp((float)(now - previousGroundImpactBreakSampleTime), 0f, 0.1f);
                previousGroundImpactBreakSampleTime = now;
                Scene scene = RequireScene();
                GameObject raise = FindUnique(scene, "Shield_Raise");
                GameObject lower = FindUnique(scene, "Shield_Lower");
                GameObject blockIdle = FindUnique(scene, "Shield_Block_Idle");
                GameObject impact = FindUnique(scene, "Shield_Block_Impact");
                GameObject breakTarget = FindUnique(scene, "Shield_Break_Reaction");
                ShieldStateMotion raiseMotion = raise.GetComponent<ShieldStateMotion>();
                ShieldStateMotion lowerMotion = lower.GetComponent<ShieldStateMotion>();
                ShieldStateMotion impactMotion = impact.GetComponent<ShieldStateMotion>();
                ShieldStateMotion breakMotion = breakTarget.GetComponent<ShieldStateMotion>();
                if (raiseMotion == null || lowerMotion == null || impactMotion == null || breakMotion == null)
                    throw new InvalidOperationException("A corrected Shield state is missing ShieldStateMotion.");

                maximumRaiseLeftFootError = Mathf.Max(maximumRaiseLeftFootError,
                    raiseMotion.LastLeftFootGroundErrorMeters);
                maximumRaiseRightFootError = Mathf.Max(maximumRaiseRightFootError,
                    raiseMotion.LastRightFootGroundErrorMeters);
                maximumLowerLeftFootError = Mathf.Max(maximumLowerLeftFootError,
                    lowerMotion.LastLeftFootGroundErrorMeters);
                maximumLowerRightFootError = Mathf.Max(maximumLowerRightFootError,
                    lowerMotion.LastRightFootGroundErrorMeters);
                maximumRaisePreGroundDelta = Mathf.Max(maximumRaisePreGroundDelta,
                    Mathf.Max(Mathf.Abs(raiseMotion.LastLeftFootPreGroundDeltaMeters),
                        Mathf.Abs(raiseMotion.LastRightFootPreGroundDeltaMeters)));
                maximumLowerPreGroundDelta = Mathf.Max(maximumLowerPreGroundDelta,
                    Mathf.Max(Mathf.Abs(lowerMotion.LastLeftFootPreGroundDeltaMeters),
                        Mathf.Abs(lowerMotion.LastRightFootPreGroundDeltaMeters)));
                maximumRaiseRigidBodyDrop = Mathf.Max(maximumRaiseRigidBodyDrop,
                    raiseMotion.LastRigidBodyGroundDropMeters);
                maximumLowerRigidBodyDrop = Mathf.Max(maximumLowerRigidBodyDrop,
                    lowerMotion.LastRigidBodyGroundDropMeters);
                AccumulateHold(raiseMotion.LastHoldingFinalPose, deltaSeconds,
                    ref currentRaiseHoldSeconds, ref maximumRaiseHoldSeconds);
                AccumulateHold(lowerMotion.LastHoldingFinalPose, deltaSeconds,
                    ref currentLowerHoldSeconds, ref maximumLowerHoldSeconds);
                if (raiseMotion.LastHoldingFinalPose)
                    minimumRaiseHoldNormalizedTime = Mathf.Min(minimumRaiseHoldNormalizedTime,
                        raiseMotion.LastSampledNormalizedTime);
                if (lowerMotion.LastHoldingFinalPose)
                    maximumLowerHoldNormalizedTime = Mathf.Max(maximumLowerHoldNormalizedTime,
                        lowerMotion.LastSampledNormalizedTime);

                Transform impactShield = RequireShield(impact);
                Transform breakShield = RequireShield(breakTarget);
                ShieldForearmFollower impactFollower = impactShield.GetComponent<ShieldForearmFollower>();
                maximumImpactForearmRotationError = Mathf.Max(maximumImpactForearmRotationError,
                    impactFollower.LastForearmRotationErrorDegrees);
                minimumImpactHandleOverlap = Mathf.Min(minimumImpactHandleOverlap,
                    impactFollower.LastHandleOverlapMeters);
                maximumImpactHandleOverlap = Mathf.Max(maximumImpactHandleOverlap,
                    impactFollower.LastHandleOverlapMeters);
                maximumBreakForearmRotationError = Mathf.Max(maximumBreakForearmRotationError,
                    breakShield.GetComponent<ShieldForearmFollower>().LastForearmRotationErrorDegrees);
                ComparePose(impact.transform, blockIdle.transform, IsLegPath,
                    ref maximumImpactLowerPositionError, ref maximumImpactLowerRotationError,
                    ref maximumImpactLowerScaleError);
                ComparePose(breakTarget.transform, blockIdle.transform, IsLegPath,
                    ref maximumBreakLowerPositionError, ref maximumBreakLowerRotationError,
                    ref maximumBreakLowerScaleError);
                Transform breakSpine = RequireNamedTransform(breakTarget.transform, "Spine");
                Transform breakForearm = RequireNamedTransform(breakTarget.transform, "RightForeArm");
                maximumBreakSpineMotionDegrees = Mathf.Max(maximumBreakSpineMotionDegrees,
                    Quaternion.Angle(initialBreakSpineRotation, breakSpine.localRotation));
                maximumBreakForearmMotionDegrees = Mathf.Max(maximumBreakForearmMotionDegrees,
                    Quaternion.Angle(initialBreakForearmRotation, breakForearm.localRotation));

                if (now >= nextGroundImpactGeometrySampleTime)
                {
                    maximumImpactLegFrontProtrusion = Mathf.Max(maximumImpactLegFrontProtrusion,
                        MeasureLegFrontProtrusion(impact, impactShield));
                    nextGroundImpactGeometrySampleTime = now + 0.12d;
                }
                if (now < groundImpactBreakObservationEnd) return;

                EditorApplication.update -= ObserveGroundImpactBreakTick;
                observingGroundImpactBreak = false;
                bool timingPassed = Mathf.Abs(raiseMotion.MotionDurationSeconds - 0.8f) <= 0.0001f &&
                    Mathf.Abs(lowerMotion.MotionDurationSeconds - 0.8f) <= 0.0001f &&
                    Mathf.Abs(raiseMotion.EndHoldDurationSeconds - 0.5f) <= 0.0001f &&
                    Mathf.Abs(lowerMotion.EndHoldDurationSeconds - 0.5f) <= 0.0001f &&
                    Mathf.Abs(raiseMotion.CycleDurationSeconds - 1.3f) <= 0.0001f &&
                    Mathf.Abs(lowerMotion.CycleDurationSeconds - 1.3f) <= 0.0001f &&
                    maximumRaiseHoldSeconds >= 0.42f && maximumLowerHoldSeconds >= 0.42f &&
                    minimumRaiseHoldNormalizedTime >= 0.999f &&
                    maximumLowerHoldNormalizedTime <= 0.001f &&
                    raiseMotion.CompletedCycleCount >= 4 && lowerMotion.CompletedCycleCount >= 4;
                float maximumFootError = Mathf.Max(Mathf.Max(maximumRaiseLeftFootError, maximumRaiseRightFootError),
                    Mathf.Max(maximumLowerLeftFootError, maximumLowerRightFootError));
                bool groundingPassed = maximumFootError <= 0.002f;
                bool impactPassed = maximumImpactLegFrontProtrusion <= 0.005f &&
                    maximumImpactLowerPositionError <= 0.0001f && maximumImpactLowerRotationError <= 0.05f &&
                    maximumImpactLowerScaleError <= 0.0001f && maximumImpactForearmRotationError <= 0.01f &&
                    Mathf.Abs(minimumImpactHandleOverlap - RequiredHandleForearmOverlapMeters) <= 0.003f &&
                    Mathf.Abs(maximumImpactHandleOverlap - RequiredHandleForearmOverlapMeters) <= 0.003f;
                bool breakCorrectionsDisabled =
                    !breakTarget.GetComponent<ShieldRightArmClearance>().ApplyRuntimeCorrection &&
                    !breakTarget.GetComponent<ShieldHeadForward>().ApplyRuntimeAlignment;
                bool breakPassed = maximumBreakLowerPositionError <= 0.0001f &&
                    maximumBreakLowerRotationError <= 0.05f && maximumBreakLowerScaleError <= 0.0001f &&
                    maximumBreakForearmRotationError <= 0.01f && maximumBreakSpineMotionDegrees >= 5f &&
                    maximumBreakForearmMotionDegrees >= 5f && breakCorrectionsDisabled;
                var report = new StringBuilder("Read-only live Shield ground, impact and break inspection.\n");
                report.AppendLine("raiseMotionSeconds=" + raiseMotion.MotionDurationSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " raiseHoldSeconds=" + raiseMotion.EndHoldDurationSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " raiseCycleSeconds=" + raiseMotion.CycleDurationSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " observedMaximumContinuousHoldSeconds=" + maximumRaiseHoldSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " holdSampledNormalizedTimeMinimum=" + minimumRaiseHoldNormalizedTime.ToString("F6", CultureInfo.InvariantCulture) +
                    " completedCycles=" + raiseMotion.CompletedCycleCount);
                report.AppendLine("lowerMotionSeconds=" + lowerMotion.MotionDurationSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " lowerHoldSeconds=" + lowerMotion.EndHoldDurationSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " lowerCycleSeconds=" + lowerMotion.CycleDurationSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " observedMaximumContinuousHoldSeconds=" + maximumLowerHoldSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " holdSampledNormalizedTimeMaximum=" + maximumLowerHoldNormalizedTime.ToString("F6", CultureInfo.InvariantCulture) +
                    " completedCycles=" + lowerMotion.CompletedCycleCount);
                report.AppendLine("maximumFootHeightErrorMeters=" + maximumFootError.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("raiseLeftFootErrorMeters=" + maximumRaiseLeftFootError.ToString("F8", CultureInfo.InvariantCulture) +
                    " raiseRightFootErrorMeters=" + maximumRaiseRightFootError.ToString("F8", CultureInfo.InvariantCulture) +
                    " raiseMaximumPreGroundDeltaMeters=" + maximumRaisePreGroundDelta.ToString("F8", CultureInfo.InvariantCulture) +
                    " raiseMaximumRigidBodyDropMeters=" + maximumRaiseRigidBodyDrop.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("lowerLeftFootErrorMeters=" + maximumLowerLeftFootError.ToString("F8", CultureInfo.InvariantCulture) +
                    " lowerRightFootErrorMeters=" + maximumLowerRightFootError.ToString("F8", CultureInfo.InvariantCulture) +
                    " lowerMaximumPreGroundDeltaMeters=" + maximumLowerPreGroundDelta.ToString("F8", CultureInfo.InvariantCulture) +
                    " lowerMaximumRigidBodyDropMeters=" + maximumLowerRigidBodyDrop.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("impactMaximumLegFrontProtrusionMeters=" + maximumImpactLegFrontProtrusion.ToString("F8", CultureInfo.InvariantCulture) +
                    " lowerPosePositionErrorMeters=" + maximumImpactLowerPositionError.ToString("F8", CultureInfo.InvariantCulture) +
                    " lowerPoseRotationErrorDegrees=" + maximumImpactLowerRotationError.ToString("F8", CultureInfo.InvariantCulture) +
                    " lowerPoseScaleError=" + maximumImpactLowerScaleError.ToString("F8", CultureInfo.InvariantCulture) +
                    " shieldForearmRotationErrorDegrees=" + maximumImpactForearmRotationError.ToString("F8", CultureInfo.InvariantCulture) +
                    " handleOverlapMeters=" + minimumImpactHandleOverlap.ToString("F8", CultureInfo.InvariantCulture) +
                    ".." + maximumImpactHandleOverlap.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("breakSourceLengthSeconds=2.866667 upperSpineMotionDegrees=" +
                    maximumBreakSpineMotionDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                    " upperForearmMotionDegrees=" + maximumBreakForearmMotionDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                    " lowerPosePositionErrorMeters=" + maximumBreakLowerPositionError.ToString("F8", CultureInfo.InvariantCulture) +
                    " lowerPoseRotationErrorDegrees=" + maximumBreakLowerRotationError.ToString("F8", CultureInfo.InvariantCulture) +
                    " lowerPoseScaleError=" + maximumBreakLowerScaleError.ToString("F8", CultureInfo.InvariantCulture) +
                    " shieldForearmRotationErrorDegrees=" + maximumBreakForearmRotationError.ToString("F8", CultureInfo.InvariantCulture) +
                    " upperBodyCorrectionsDisabled=" + breakCorrectionsDisabled);
                report.AppendLine("timingPassed=" + timingPassed + " groundingPassed=" + groundingPassed +
                    " impactPassed=" + impactPassed + " breakPassed=" + breakPassed);
                report.AppendLine("Inspection observed normal live playback and did not sample or alter any validation target.");
                File.WriteAllText(Absolute(GroundImpactBreakOutputFolder + "/runtime_metrics.txt"),
                    report.ToString(), Encoding.UTF8);
                if (!timingPassed || !groundingPassed || !impactPassed || !breakPassed)
                    throw new InvalidOperationException("Shield correction inspection failed. " +
                        "timing=" + timingPassed + " grounding=" + groundingPassed +
                        " impact=" + impactPassed + " break=" + breakPassed + ".");
                Debug.Log("Shield ground, impact and break correction observation passed.");
            }
            catch (Exception exception)
            {
                EditorApplication.update -= ObserveGroundImpactBreakTick;
                observingGroundImpactBreak = false;
                File.WriteAllText(Absolute(GroundImpactBreakOutputFolder + "/runtime_metrics_incomplete.txt"),
                    exception.ToString(), Encoding.UTF8);
                Debug.LogException(exception);
            }
        }

        private static void AccumulateHold(bool holding, float deltaSeconds, ref float current, ref float maximum)
        {
            if (holding)
            {
                current += deltaSeconds;
                maximum = Mathf.Max(maximum, current);
            }
            else current = 0f;
        }

        private static Transform RequireShield(GameObject target)
        {
            Transform forearm = RequireNamedTransform(target.transform, "RightForeArm");
            return forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
        }

        private static void ComparePose(Transform targetRoot, Transform referenceRoot, Func<string, bool> filter,
            ref float positionError, ref float rotationError, ref float scaleError)
        {
            string[] paths = targetRoot.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, targetRoot))
                .Where(filter)
                .ToArray();
            foreach (string path in paths)
            {
                Transform target = targetRoot.Find(path);
                Transform reference = referenceRoot.Find(path) ??
                    throw new InvalidOperationException("Reference pose is missing " + path + ".");
                positionError = Mathf.Max(positionError, Vector3.Distance(target.localPosition, reference.localPosition));
                rotationError = Mathf.Max(rotationError, Quaternion.Angle(target.localRotation, reference.localRotation));
                scaleError = Mathf.Max(scaleError, Vector3.Distance(target.localScale, reference.localScale));
            }
        }

        private static float MeasureLegFrontProtrusion(GameObject target, Transform shield)
        {
            ShieldHandleBasis(shield, out _, out _, out Vector3 localFace, out _);
            Vector3 outward = shield.TransformDirection(localFace).normalized;
            float shieldFront = float.NegativeInfinity;
            foreach (Renderer renderer in shield.GetComponentsInChildren<Renderer>(true))
            {
                Bounds bounds = renderer.bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                    shieldFront = Mathf.Max(shieldFront, Vector3.Dot(new Vector3(
                        x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z), outward));
            }

            var legBones = new HashSet<Transform>();
            Transform leftRoot = RequireNamedTransform(target.transform, "LeftUpLeg");
            Transform rightRoot = RequireNamedTransform(target.transform, "RightUpLeg");
            foreach (Transform bone in leftRoot.GetComponentsInChildren<Transform>(true)) legBones.Add(bone);
            foreach (Transform bone in rightRoot.GetComponentsInChildren<Transform>(true)) legBones.Add(bone);
            float legFront = float.NegativeInfinity;
            foreach (SkinnedMeshRenderer renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh shared = renderer.sharedMesh;
                if (shared == null) continue;
                var legIndices = new HashSet<int>(Enumerable.Range(0, renderer.bones.Length)
                    .Where(index => legBones.Contains(renderer.bones[index])));
                if (legIndices.Count == 0) continue;
                BoneWeight[] weights = shared.boneWeights;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    Vector3[] vertices = baked.vertices;
                    for (int index = 0; index < vertices.Length; index++)
                    {
                        BoneWeight weight = weights[index];
                        float influence = 0f;
                        if (legIndices.Contains(weight.boneIndex0)) influence += weight.weight0;
                        if (legIndices.Contains(weight.boneIndex1)) influence += weight.weight1;
                        if (legIndices.Contains(weight.boneIndex2)) influence += weight.weight2;
                        if (legIndices.Contains(weight.boneIndex3)) influence += weight.weight3;
                        if (influence < 0.2f) continue;
                        legFront = Mathf.Max(legFront,
                            Vector3.Dot(renderer.transform.TransformPoint(vertices[index]), outward));
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            if (float.IsNegativeInfinity(legFront) || float.IsNegativeInfinity(shieldFront))
                throw new InvalidOperationException("Could not measure Shield impact leg depth.");
            return legFront - shieldFront;
        }

        public static void Capture(string outputPath)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Shield capture requires normal live playback.");
            if (capturing) throw new InvalidOperationException("A Shield capture is already running.");
            string relativeOutput = string.IsNullOrWhiteSpace(outputPath)
                ? OutputFolder + "/final.png"
                : outputPath.Replace('\\', '/');
            string absoluteOutput = Absolute(relativeOutput);
            if (File.Exists(absoluteOutput))
                throw new InvalidOperationException("The one-time Shield final capture already exists: " + relativeOutput);
            captureOutputPath = absoluteOutput;
            captureDirectory = Absolute(OutputFolder + "/playback_" + DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(captureDirectory);
            capturePhase = 0;
            captureInterval = 0.45f;
            nextCaptureTime = EditorApplication.timeSinceStartup + 0.05d;
            capturing = true;
            EditorApplication.update += CaptureTick;
            Debug.Log("One final unmodified Shield live-playback capture started: " + relativeOutput);
        }

        private static string ConfigureSourceImporter(string modelPath, string importedClipName)
        {
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter ??
                throw new InvalidOperationException("Shield animation FBX importer is unavailable: " + modelPath);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = false;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;

            var defaults = importer.defaultClipAnimations;
            var matches = defaults.Where(item =>
                    item.name.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.takeName.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Shield animation FBX must expose exactly one Mixamo take. Path=" +
                    modelPath + " Matches=" + matches.Length + " Defaults=" +
                    string.Join("|", defaults.Select(item => item.name + ":" + item.takeName)));
            var selected = matches[0];
            selected.name = importedClipName;
            selected.loopTime = true;
            selected.loopPose = false;
            selected.wrapMode = WrapMode.Loop;
            importer.animationWrapMode = WrapMode.Loop;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
            return selected.takeName;
        }

        private static AnimationClip RequireSourceClip()
        {
            return RequireImportedClip(SourceModelPath, ImportedClipName);
        }

        private static AnimationClip RequireImportedClip(string modelPath, string importedClipName)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != importedClipName)
                throw new InvalidOperationException("Expected one imported Shield Mixamo clip at " + modelPath +
                    ". Found=" +
                    string.Join("|", clips.Select(item => item.name)));
            return clips[0];
        }

        private static AvatarMask CreateMask(Transform referenceRoot, string assetPath, string maskName,
            Func<string, bool> isActive)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null &&
                !AssetDatabase.DeleteAsset(assetPath))
                throw new InvalidOperationException("Existing Shield animation mask could not be replaced: " + assetPath);
            var mask = new AvatarMask { name = maskName };
            string[] paths = referenceRoot.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, referenceRoot))
                .Where(item => !string.IsNullOrEmpty(item))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item.Count(character => character == '/'))
                .ThenBy(item => item, StringComparer.Ordinal)
                .ToArray();
            if (paths.Length == 0)
                throw new InvalidOperationException("Shield target has no transform paths for animation mask " + maskName + ".");
            mask.transformCount = paths.Length;
            for (int index = 0; index < paths.Length; index++)
            {
                mask.SetTransformPath(index, paths[index]);
                mask.SetTransformActive(index, isActive(paths[index]));
            }
            for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
            AssetDatabase.CreateAsset(mask, assetPath);
            return mask;
        }

        private static AnimatorController CreateSingleController(string controllerPath, string stateName,
            AnimationClip clip, float speed, float cycleOffset)
        {
            DeleteAssetIfPresent(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var state = controller.layers[0].stateMachine.AddState(stateName);
            state.motion = clip;
            state.speed = speed;
            state.cycleOffset = cycleOffset;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Animator ConfigureAnimator(GameObject target, AnimatorController controller,
            AnimationClip expectedClip)
        {
            var animators = target.GetComponentsInChildren<Animator>(true);
            Animator animator;
            if (animators.Length == 0)
            {
                animator = target.AddComponent<Animator>();
            }
            else
            {
                if (animators.Length != 1 || animators[0].transform != target.transform)
                    throw new InvalidOperationException(target.name +
                        " must contain no Animator or exactly one root Animator. Found=" + animators.Length);
                animator = animators[0];
            }
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            if (!ControllerUsesClip(controller, 0, expectedClip))
                throw new InvalidOperationException(target.name + " controller clip references are not exact.");
            return animator;
        }

        private static Animator RequireConfiguredAnimator(GameObject target)
        {
            var animator = target.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                throw new InvalidOperationException(target.name + " Animator is missing.");
            string expectedPath = ExpectedControllerPath(target.name);
            if (AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != expectedPath)
                throw new InvalidOperationException(target.name + " does not use its expected Shield controller. Expected=" +
                    expectedPath + " Actual=" + AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            return animator;
        }

        private static string ExpectedControllerPath(string targetName)
        {
            switch (targetName)
            {
                case "Shield_Draw": return DrawControllerPath;
                case "Shield_Idle":
                case "Shield_Stow": return ControllerPath;
                case "Shield_Raise": return RaiseControllerPath;
                case "Shield_Block_Idle": return BlockIdleControllerPath;
                case "Shield_Lower": return LowerControllerPath;
                case "Shield_Block_Impact": return ImpactControllerPath;
                case "Shield_Break_Reaction": return BreakControllerPath;
                default: throw new InvalidOperationException("Unknown Shield target " + targetName + ".");
            }
        }

        private static AnimationClip ExpectedClipForTarget(string targetName, AnimationClip sourceClip,
            AnimationClip drawClip, AnimationClip raiseClip, AnimationClip blockIdleClip, AnimationClip lowerClip,
            AnimationClip impactClip, AnimationClip breakClip)
        {
            switch (targetName)
            {
                case "Shield_Draw": return drawClip;
                case "Shield_Idle":
                case "Shield_Stow": return sourceClip;
                case "Shield_Raise": return raiseClip;
                case "Shield_Block_Idle": return blockIdleClip;
                case "Shield_Lower": return lowerClip;
                case "Shield_Block_Impact": return impactClip;
                case "Shield_Break_Reaction": return breakClip;
                default: throw new InvalidOperationException("Unknown Shield target " + targetName + ".");
            }
        }

        private static bool ControllerUsesClip(AnimatorController controller, int layerIndex, AnimationClip clip)
        {
            if (controller.layers.Length <= layerIndex) return false;
            return controller.layers[layerIndex].stateMachine.states.Any(item => ReferenceEquals(item.state.motion, clip));
        }

        private static AnimationClip CreateShieldTransitionClip(GameObject reference, AnimationClip sourceClip,
            AnimationClip idleClip, string assetPath, float durationSeconds, bool transitionLowerBodyToSource)
        {
            string[] sourceLegPaths = BoundTransformPaths(sourceClip, IsLegPath);
            if (transitionLowerBodyToSource && sourceLegPaths.Length == 0)
                throw new InvalidOperationException("Shield transfer Mixamo clip has no lower-body pose curves.");
            string[] paths = BoundTransformPaths(sourceClip, IsSkeletonPath)
                .Concat(BoundTransformPaths(idleClip, IsLegPath))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            PoseSnapshot sourcePose = SamplePose(reference, sourceClip, 0f, paths);
            PoseSnapshot idlePose = SamplePose(reference, idleClip, 0f, paths);
            DeleteAssetIfPresent(assetPath);
            var result = new AnimationClip
            {
                name = Path.GetFileNameWithoutExtension(assetPath),
                frameRate = 60f,
                wrapMode = WrapMode.Loop
            };
            for (int index = 0; index < paths.Length; index++)
            {
                Transform stableBone = reference.transform.Find(paths[index]) ??
                    throw new InvalidOperationException(reference.name + " is missing transition bone " + paths[index] + ".");
                Vector3 stablePosition = stableBone.localPosition;
                Quaternion startRotation = IsLegPath(paths[index])
                    ? idlePose.Rotations[index]
                    : sourcePose.Rotations[index];
                Quaternion endRotation = transitionLowerBodyToSource && IsLegPath(paths[index])
                    ? sourcePose.Rotations[index]
                    : startRotation;
                if (Quaternion.Dot(startRotation, endRotation) < 0f)
                    endRotation = new Quaternion(-endRotation.x, -endRotation.y, -endRotation.z, -endRotation.w);
                AnimationCurve[] curves = NewTransformCurves();
                AddTransformKeys(curves, 0f, stablePosition, startRotation);
                AddTransformKeys(curves, durationSeconds, stablePosition, endRotation);
                SetTransformCurves(result, paths[index], curves);
            }
            SetClipLooping(result, !transitionLowerBodyToSource);
            result.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(result, assetPath);
            return result;
        }

        private static AnimationClip CopyAnimationClipAsset(string sourcePath, string destinationPath)
        {
            DeleteAssetIfPresent(destinationPath);
            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
                throw new InvalidOperationException("Shield_Raise clip could not be copied to Shield_Lower.");
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath) ??
                throw new InvalidOperationException("Copied Shield_Lower clip is missing.");
        }

        private static AnimationClip CreateBlockIdleClip(GameObject reference, AnimationClip raiseClip,
            AnimationClip breathingClip)
        {
            DeleteAssetIfPresent(BlockIdleClipPath);
            GameObject basePreview = UnityEngine.Object.Instantiate(reference);
            GameObject breathingPreview = UnityEngine.Object.Instantiate(reference);
            basePreview.hideFlags = HideFlags.HideAndDontSave;
            breathingPreview.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                string[] paths = BoundTransformPaths(raiseClip, IsSkeletonPath);
                float raiseFinalTime = Mathf.Max(0f, raiseClip.length - 1f / 120f);
                raiseClip.SampleAnimation(basePreview, raiseFinalTime);
                breathingClip.SampleAnimation(breathingPreview, 0f);
                var basePose = CaptureCurrentPose(basePreview.transform, paths);
                var breathingReference = CaptureCurrentPose(breathingPreview.transform, paths);
                var outputCurves = paths.ToDictionary(path => path, path => NewTransformCurves(),
                    StringComparer.Ordinal);
                int sampleCount = Mathf.Max(2, Mathf.CeilToInt(breathingClip.length * 60f) + 1);
                Quaternion[] previousRotations = new Quaternion[paths.Length];
                for (int sample = 0; sample < sampleCount; sample++)
                {
                    float time = sample == sampleCount - 1
                        ? breathingClip.length
                        : sample / 60f;
                    breathingClip.SampleAnimation(breathingPreview, time);
                    PoseSnapshot breathingPose = CaptureCurrentPose(breathingPreview.transform, paths);
                    for (int index = 0; index < paths.Length; index++)
                    {
                        Vector3 position = basePose.Positions[index];
                        Quaternion rotation = basePose.Rotations[index];
                        if (IsBreathingPath(paths[index]))
                        {
                            position += breathingPose.Positions[index] - breathingReference.Positions[index];
                            Quaternion delta = Quaternion.Inverse(breathingReference.Rotations[index]) *
                                breathingPose.Rotations[index];
                            rotation *= delta;
                        }
                        if (sample > 0 && Quaternion.Dot(previousRotations[index], rotation) < 0f)
                            rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                        previousRotations[index] = rotation;
                        AddTransformKeys(outputCurves[paths[index]], time, position, rotation);
                    }
                }

                var result = new AnimationClip
                {
                    name = "Shield_Block_Idle",
                    frameRate = 60f,
                    wrapMode = WrapMode.Loop
                };
                foreach (string path in paths)
                    SetTransformCurves(result, path, outputCurves[path]);
                SetClipLooping(result, true);
                result.EnsureQuaternionContinuity();
                AssetDatabase.CreateAsset(result, BlockIdleClipPath);
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(basePreview);
                UnityEngine.Object.DestroyImmediate(breathingPreview);
            }
        }

        private static void ConfigurePositionLocks(ShieldStateMotion motion, GameObject target, AnimationClip clip,
            Func<string, bool> pathFilter)
        {
            string[] paths = BoundTransformPaths(clip, pathFilter);
            Transform[] bones = paths.Select(path => target.transform.Find(path) ??
                    throw new InvalidOperationException(target.name + " is missing position-lock bone " + path + "."))
                .ToArray();
            motion.ConfigurePositionLocks(bones, bones.Select(bone => bone.localPosition).ToArray(),
                bones.Select(bone => bone.localScale).ToArray());
        }

        private static void ConfigureRotationLocksFromDesired(ShieldStateMotion motion, GameObject target,
            AnimationClip sourceClip, AnimationClip desiredClip, Func<string, bool> pathFilter)
        {
            string[] paths = BoundTransformPaths(sourceClip, pathFilter);
            PoseSnapshot desiredPose = SamplePose(target, desiredClip, 0f, paths);
            Transform[] bones = paths.Select(path => target.transform.Find(path) ??
                    throw new InvalidOperationException(target.name + " is missing rotation-lock bone " + path + "."))
                .ToArray();
            motion.ConfigureRotationLocks(bones, desiredPose.Rotations);
        }

        private static void ConfigureFootGrounding(ShieldStateMotion motion, GameObject target,
            GameObject idleReference, bool enabled)
        {
            Transform leftUpper = RequireNamedTransform(target.transform, "LeftUpLeg");
            Transform leftLower = RequireNamedTransform(target.transform, "LeftLeg");
            Transform leftFoot = RequireNamedTransform(target.transform, "LeftFoot");
            Transform rightUpper = RequireNamedTransform(target.transform, "RightUpLeg");
            Transform rightLower = RequireNamedTransform(target.transform, "RightLeg");
            Transform rightFoot = RequireNamedTransform(target.transform, "RightFoot");
            Transform idleLeftFoot = RequireNamedTransform(idleReference.transform, "LeftFoot");
            Transform idleRightFoot = RequireNamedTransform(idleReference.transform, "RightFoot");
            float leftHeight = Vector3.Dot(idleLeftFoot.position - idleReference.transform.position,
                idleReference.transform.up.normalized);
            float rightHeight = Vector3.Dot(idleRightFoot.position - idleReference.transform.position,
                idleReference.transform.up.normalized);
            motion.ConfigureFootGrounding(leftUpper, leftLower, leftFoot, rightUpper, rightLower, rightFoot,
                leftHeight, rightHeight, enabled);
        }

        private static void ConfigureBaselineMotion(ShieldStateMotion motion, GameObject target,
            AnimationClip sourceClip, AnimationClip desiredClip, Func<string, bool> pathFilter,
            bool preserveRelativeMotion)
        {
            string[] paths = BoundTransformPaths(sourceClip, pathFilter);
            PoseSnapshot sourcePose = SamplePose(target, sourceClip, 0f, paths);
            PoseSnapshot desiredPose = SamplePose(target, desiredClip, 0f, paths);
            Transform[] bones = paths.Select(path => target.transform.Find(path) ??
                    throw new InvalidOperationException(target.name + " is missing baseline bone " + path + "."))
                .ToArray();
            Vector3[] stablePositions = bones.Select(bone => bone.localPosition).ToArray();
            motion.ConfigureBaseline(bones, stablePositions, sourcePose.Rotations,
                stablePositions, desiredPose.Rotations, preserveRelativeMotion);
        }

        private static PoseSnapshot SamplePose(GameObject reference, AnimationClip clip, float time, string[] paths)
        {
            GameObject preview = UnityEngine.Object.Instantiate(reference);
            preview.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                clip.SampleAnimation(preview, Mathf.Clamp(time, 0f, clip.length));
                return CaptureCurrentPose(preview.transform, paths);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static PoseSnapshot CaptureCurrentPose(Transform root, string[] paths)
        {
            var positions = new Vector3[paths.Length];
            var rotations = new Quaternion[paths.Length];
            for (int index = 0; index < paths.Length; index++)
            {
                Transform bone = root.Find(paths[index]) ??
                    throw new InvalidOperationException(root.name + " is missing sampled bone " + paths[index] + ".");
                positions[index] = bone.localPosition;
                rotations[index] = bone.localRotation;
            }
            return new PoseSnapshot(paths, positions, rotations);
        }

        private static string[] BoundTransformPaths(AnimationClip clip, Func<string, bool> pathFilter) =>
            AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform) && pathFilter(binding.path))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

        private static AnimationCurve[] NewTransformCurves() =>
            Enumerable.Range(0, 7).Select(_ => new AnimationCurve()).ToArray();

        private static void AddTransformKeys(AnimationCurve[] curves, float time, Vector3 position,
            Quaternion rotation)
        {
            curves[0].AddKey(time, position.x);
            curves[1].AddKey(time, position.y);
            curves[2].AddKey(time, position.z);
            curves[3].AddKey(time, rotation.x);
            curves[4].AddKey(time, rotation.y);
            curves[5].AddKey(time, rotation.z);
            curves[6].AddKey(time, rotation.w);
        }

        private static void SetTransformCurves(AnimationClip clip, string path, AnimationCurve[] curves)
        {
            string[] properties =
            {
                "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
                "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
            };
            for (int curveIndex = 0; curveIndex < curves.Length; curveIndex++)
            {
                AnimationCurve curve = curves[curveIndex];
                for (int keyIndex = 0; keyIndex < curve.length; keyIndex++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, keyIndex, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding
                {
                    path = path,
                    type = typeof(Transform),
                    propertyName = properties[curveIndex]
                }, curve);
            }
        }

        private static void SetClipLooping(AnimationClip clip, bool loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void DeleteAssetIfPresent(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null &&
                !AssetDatabase.DeleteAsset(assetPath))
                throw new InvalidOperationException("Existing Shield animation asset could not be replaced: " + assetPath);
        }

        private static bool IsSkeletonPath(string path) =>
            (path == HipsPath || path.StartsWith(HipsPath + "/", StringComparison.Ordinal)) &&
            path.IndexOf("/" + ShieldObjectName, StringComparison.Ordinal) < 0;

        private static bool IsBreathingPath(string path) =>
            path == HipsPath + "/Spine02" ||
            path == HipsPath + "/Spine02/Spine01" ||
            path == HipsPath + "/Spine02/Spine01/Spine";

        private static bool IsArmPath(string path) =>
            path.IndexOf("/LeftShoulder", StringComparison.Ordinal) >= 0 ||
            path.IndexOf("/RightShoulder", StringComparison.Ordinal) >= 0;

        private static void AlignShield(Transform shield, Transform forearm, Transform hand, Transform targetRoot,
            Vector3 handleAnchor, Vector3 exteriorFaceAxis, Vector3 verticalAxis)
        {
            Vector3 worldFace = shield.TransformDirection(exteriorFaceAxis).normalized;
            Vector3 worldVertical = shield.TransformDirection(verticalAxis).normalized;
            Quaternion currentBasis = Quaternion.LookRotation(worldFace, worldVertical);
            Quaternion desiredBasis = Quaternion.LookRotation(targetRoot.forward, targetRoot.up);
            shield.rotation = desiredBasis * Quaternion.Inverse(currentBasis) * shield.rotation;
            Vector3 forearmCenter = Vector3.Lerp(forearm.position, hand.position, 0.5f);
            shield.position += forearmCenter - shield.TransformPoint(handleAnchor);
        }

        private static void ShieldMetrics(Transform shield, Transform forearm, Transform hand, Transform targetRoot,
            out float centerError, out float forwardAlignment, out float verticalAlignment,
            out Vector3 boundsSize, out Vector3 faceAxis, out Vector3 verticalAxis)
        {
            ShieldHandleBasis(shield, out Vector3 handleAnchor, out _, out faceAxis, out verticalAxis);
            Bounds worldBounds = WorldRendererBounds(shield);
            Vector3 forearmCenter = Vector3.Lerp(forearm.position, hand.position, 0.5f);
            centerError = Vector3.Distance(shield.TransformPoint(handleAnchor), forearmCenter);
            forwardAlignment = Vector3.Dot(shield.TransformDirection(faceAxis).normalized, targetRoot.forward.normalized);
            verticalAlignment = Vector3.Dot(shield.TransformDirection(verticalAxis).normalized, targetRoot.up.normalized);
            boundsSize = worldBounds.size;
        }

        private static float EstimatedHandleForearmOverlap(Transform shield, Transform forearm, Transform hand,
            Transform targetRoot)
        {
            ShieldHandleBasis(shield, out _, out Bounds handleBounds, out Vector3 faceAxis, out _);
            Vector3 outward = shield.TransformDirection(faceAxis).normalized;
            float handleBack = ProjectedBoundsMinimum(shield, handleBounds, outward);
            float forearmFront = Mathf.Max(Vector3.Dot(forearm.position, outward), Vector3.Dot(hand.position, outward)) +
                ForearmSkinBeyondFrontBoneMeters;
            return Mathf.Max(0f, forearmFront - handleBack);
        }

        private static float ProjectedBoundsMinimum(Transform owner, Bounds bounds, Vector3 axis)
        {
            float minimum = float.PositiveInfinity;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 local = new Vector3(x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
                minimum = Mathf.Min(minimum, Vector3.Dot(owner.TransformPoint(local), axis));
            }
            return minimum;
        }

        private sealed class GeometryComponent
        {
            internal int VertexCount;
            internal Bounds Bounds;
        }

        internal static void GetShieldHandleBasis(Transform shield, out Vector3 handleAnchor,
            out Bounds handleBounds, out Vector3 exteriorFaceAxis, out Vector3 verticalAxis)
        {
            ShieldHandleBasis(shield, out handleAnchor, out handleBounds, out exteriorFaceAxis, out verticalAxis);
        }

        private static void ShieldHandleBasis(Transform shield, out Vector3 handleAnchor,
            out Bounds handleBounds, out Vector3 exteriorFaceAxis, out Vector3 verticalAxis)
        {
            Bounds total = LocalGeometryBounds(shield);
            AxisBasis(total.size, out Vector3 unsignedFaceAxis, out verticalAxis);
            int faceIndex = AxisIndex(unsignedFaceAxis);
            int verticalIndex = AxisIndex(verticalAxis);
            int horizontalIndex = 3 - faceIndex - verticalIndex;
            GeometryComponent[] candidates = ConnectedGeometryComponents(shield)
                .Where(component => component.VertexCount >= 100 &&
                    Component(component.Bounds.size, horizontalIndex) >= Component(total.size, horizontalIndex) * 0.35f &&
                    Component(component.Bounds.size, verticalIndex) <= Component(total.size, verticalIndex) * 0.15f &&
                    Mathf.Abs(Component(component.Bounds.center - total.center, verticalIndex)) <=
                        Component(total.size, verticalIndex) * 0.1f)
                .OrderByDescending(component => Component(component.Bounds.center - total.center, faceIndex))
                .ToArray();
            if (candidates.Length == 0)
                throw new InvalidOperationException("The original shield geometry has no central forearm handle component.");
            GeometryComponent handle = candidates[0];
            float handleFaceOffset = Component(handle.Bounds.center - total.center, faceIndex);
            if (Mathf.Abs(handleFaceOffset) < Component(total.size, faceIndex) * 0.1f)
                throw new InvalidOperationException("The shield handle side cannot be distinguished from its exterior face.");
            handleBounds = handle.Bounds;
            handleAnchor = handle.Bounds.center;
            exteriorFaceAxis = handleFaceOffset > 0f ? -unsignedFaceAxis : unsignedFaceAxis;
        }

        private static GeometryComponent[] ConnectedGeometryComponents(Transform root)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true)
                .Where(filter => filter.sharedMesh != null).ToArray();
            if (filters.Length != 1)
                throw new InvalidOperationException("Shield handle detection expects one original mesh, found " + filters.Length + ".");
            MeshFilter filter = filters[0];
            Vector3[] vertices = filter.sharedMesh.vertices;
            int[] triangles = filter.sharedMesh.triangles;
            int[] parent = Enumerable.Range(0, vertices.Length).ToArray();
            var coincident = new Dictionary<Vector3, int>();
            for (int index = 0; index < vertices.Length; index++)
            {
                if (coincident.TryGetValue(vertices[index], out int existing)) Union(parent, existing, index);
                else coincident.Add(vertices[index], index);
            }
            for (int index = 0; index < triangles.Length; index += 3)
            {
                Union(parent, triangles[index], triangles[index + 1]);
                Union(parent, triangles[index], triangles[index + 2]);
            }
            return Enumerable.Range(0, vertices.Length).GroupBy(index => Find(parent, index)).Select(group =>
            {
                int[] indices = group.ToArray();
                Vector3 first = root.InverseTransformPoint(filter.transform.TransformPoint(vertices[indices[0]]));
                Bounds bounds = new Bounds(first, Vector3.zero);
                foreach (int index in indices.Skip(1))
                    bounds.Encapsulate(root.InverseTransformPoint(filter.transform.TransformPoint(vertices[index])));
                return new GeometryComponent { VertexCount = indices.Length, Bounds = bounds };
            }).ToArray();
        }

        private static int Find(int[] parent, int value)
        {
            while (parent[value] != value)
            {
                parent[value] = parent[parent[value]];
                value = parent[value];
            }
            return value;
        }

        private static void Union(int[] parent, int left, int right)
        {
            int leftRoot = Find(parent, left);
            int rightRoot = Find(parent, right);
            if (leftRoot != rightRoot) parent[rightRoot] = leftRoot;
        }

        private static int AxisIndex(Vector3 axis) =>
            Mathf.Abs(axis.x) > 0.5f ? 0 : Mathf.Abs(axis.y) > 0.5f ? 1 : 2;

        private static float Component(Vector3 value, int index) =>
            index == 0 ? value.x : index == 1 ? value.y : value.z;

        private static void AxisBasis(Vector3 size, out Vector3 faceAxis, out Vector3 verticalAxis)
        {
            float[] values = { size.x, size.y, size.z };
            int face = Array.IndexOf(values, values.Min());
            int vertical = Array.IndexOf(values, values.Max());
            if (face == vertical) throw new InvalidOperationException("Shield bounds cannot determine distinct face and vertical axes.");
            faceAxis = Axis(face);
            verticalAxis = Axis(vertical);
        }

        private static Vector3 Axis(int index) => index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;

        private static Bounds LocalGeometryBounds(Transform root)
        {
            bool initialized = false;
            Bounds result = default;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                EncapsulateLocalBounds(root, filter.transform, filter.sharedMesh != null ? filter.sharedMesh.bounds : default,
                    filter.sharedMesh != null, ref initialized, ref result);
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                EncapsulateLocalBounds(root, renderer.transform, renderer.localBounds, renderer.sharedMesh != null,
                    ref initialized, ref result);
            if (!initialized) throw new InvalidOperationException("Shield FBX contains no renderable mesh bounds.");
            return result;
        }

        private static void EncapsulateLocalBounds(Transform root, Transform owner, Bounds bounds, bool valid,
            ref bool initialized, ref Bounds result)
        {
            if (!valid) return;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 point = new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
                Vector3 local = root.InverseTransformPoint(owner.TransformPoint(point));
                if (!initialized)
                {
                    result = new Bounds(local, Vector3.zero);
                    initialized = true;
                }
                else result.Encapsulate(local);
            }
        }

        private static Bounds WorldRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException(root.name + " has no enabled Renderer.");
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static void RequireSourceBindings(Transform targetRoot, AnimationClip sourceClip)
        {
            string[] missing = AnimationUtility.GetCurveBindings(sourceClip)
                .Where(binding => binding.type == typeof(Transform) && !string.IsNullOrEmpty(binding.path))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .Where(path => targetRoot.Find(path) == null)
                .ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("Shield Mixamo paths do not bind exactly to the target rig: " + string.Join("|", missing));
        }

        private static void RequireIdleLegBindings(Transform targetRoot, AnimationClip idleClip)
        {
            var bindings = LegBindings(idleClip);
            if (bindings.Length == 0) throw new InvalidOperationException("Hands_Empty_Idle has no leg curves.");
            string[] missing = bindings.Select(item => item.path).Distinct(StringComparer.Ordinal)
                .Where(path => targetRoot.Find(path) == null).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("Hands_Empty_Idle leg paths do not bind to the Shield target: " + string.Join("|", missing));
        }

        private static EditorCurveBinding[] LegBindings(AnimationClip clip) =>
            AnimationUtility.GetCurveBindings(clip).Where(item => IsLegPath(item.path)).ToArray();

        private static bool IsLegPath(string path) =>
            path == HipsPath ||
            LegRoots.Any(root => path == root || path.StartsWith(root + "/", StringComparison.Ordinal));

        private static bool IsUpperBodyPath(string path) =>
            path.StartsWith(HipsPath + "/Spine02", StringComparison.Ordinal);

        private static string CurveDigest(AnimationClip clip, bool onlyLegs = false)
        {
            var builder = new StringBuilder();
            var bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(item => !onlyLegs || IsLegPath(item.path))
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal);
            foreach (var binding in bindings)
            {
                builder.Append(binding.path).Append('|').Append(binding.propertyName).Append('|').Append(binding.type.FullName).Append('\n');
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (Keyframe key in curve.keys)
                    builder.Append(key.time.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(key.value.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
            }
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()))).Replace("-", string.Empty);
        }

        private static void BeginObservation(GameObject[] targets)
        {
            if (observing) EditorApplication.update -= ObserveTick;
            observationStartBase = new float[targets.Length];
            observationStartMotionCycles = new int[targets.Length];
            observedBaseLoops = new int[targets.Length];
            initialBodyBoundsMagnitude = new float[targets.Length];
            observedScalePaths = new string[targets.Length][];
            initialLocalScales = new Vector3[targets.Length][];
            maximumShieldCenterError = 0f;
            minimumShieldDesiredFaceAlignment = 1f;
            minimumShieldVerticalAlignment = 1f;
            maximumBodyBoundsRatio = 1f;
            maximumLocalScaleChange = 0f;
            minimumBlockIdleBreathingPitch = float.PositiveInfinity;
            maximumBlockIdleBreathingPitch = float.NegativeInfinity;
            for (int index = 0; index < targets.Length; index++)
            {
                var animator = RequireConfiguredAnimator(targets[index]);
                observationStartBase[index] = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                var stateMotion = targets[index].GetComponent<ShieldStateMotion>();
                observationStartMotionCycles[index] = stateMotion == null ? 0 : stateMotion.CompletedCycleCount;
                initialBodyBoundsMagnitude[index] = BodyBounds(targets[index]).size.magnitude;
                observedScalePaths[index] = targets[index].GetComponentsInChildren<Transform>(true)
                    .Select(item => AnimationUtility.CalculateTransformPath(item, targets[index].transform))
                    .Where(IsSkeletonPath)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                initialLocalScales[index] = observedScalePaths[index]
                    .Select(path => targets[index].transform.Find(path).localScale)
                    .ToArray();
            }
            observationEnd = EditorApplication.timeSinceStartup + ObservationSeconds;
            observing = true;
            EditorApplication.update += ObserveTick;
        }

        private static void ObserveTick()
        {
            try
            {
                var scene = RequireScene();
                for (int index = 0; index < TargetNames.Length; index++)
                {
                    var target = FindUnique(scene, TargetNames[index]);
                    var animator = RequireConfiguredAnimator(target);
                    var stateMotion = target.GetComponent<ShieldStateMotion>();
                    bool usesTimedPlayback = stateMotion != null &&
                        (stateMotion.MotionKind == ShieldStateMotionKind.Raise ||
                         stateMotion.MotionKind == ShieldStateMotionKind.Lower);
                    int completedLoops = usesTimedPlayback
                        ? stateMotion.CompletedCycleCount - observationStartMotionCycles[index]
                        : Mathf.FloorToInt(Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime -
                            observationStartBase[index]));
                    observedBaseLoops[index] = Mathf.Max(observedBaseLoops[index], completedLoops);
                    var forearm = RequireNamedTransform(target.transform, "RightForeArm");
                    var hand = RequireNamedTransform(target.transform, "RightHand");
                    var shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
                    var follower = shield.GetComponent<ShieldForearmFollower>() ??
                        throw new InvalidOperationException(target.name + " shield follower is missing.");
                    if (target.name == "Shield_Block_Idle")
                    {
                        var blockIdleMotion = target.GetComponent<ShieldStateMotion>() ??
                            throw new InvalidOperationException("Shield_Block_Idle state motion is missing.");
                        minimumBlockIdleBreathingPitch = Mathf.Min(minimumBlockIdleBreathingPitch,
                            blockIdleMotion.LastBreathingPitchDegrees);
                        maximumBlockIdleBreathingPitch = Mathf.Max(maximumBlockIdleBreathingPitch,
                            blockIdleMotion.LastBreathingPitchDegrees);
                    }
                    ShieldMetrics(shield, forearm, hand, target.transform,
                        out float centerError, out _, out _,
                        out _, out _, out _);
                    maximumShieldCenterError = Mathf.Max(maximumShieldCenterError, centerError);
                    minimumShieldDesiredFaceAlignment = Mathf.Min(minimumShieldDesiredFaceAlignment,
                        follower.LastDesiredFaceAlignment);
                    minimumShieldVerticalAlignment = Mathf.Min(minimumShieldVerticalAlignment,
                        follower.LastVerticalAlignment);
                    float bodyMagnitude = BodyBounds(target).size.magnitude;
                    if (initialBodyBoundsMagnitude[index] > 0.0001f)
                        maximumBodyBoundsRatio = Mathf.Max(maximumBodyBoundsRatio,
                            bodyMagnitude / initialBodyBoundsMagnitude[index]);
                    for (int pathIndex = 0; pathIndex < observedScalePaths[index].Length; pathIndex++)
                    {
                        Transform bone = target.transform.Find(observedScalePaths[index][pathIndex]);
                        maximumLocalScaleChange = Mathf.Max(maximumLocalScaleChange,
                            Vector3.Distance(bone.localScale, initialLocalScales[index][pathIndex]));
                    }
                }
                if (EditorApplication.timeSinceStartup < observationEnd) return;
                EditorApplication.update -= ObserveTick;
                observing = false;
                var report = new StringBuilder("Read-only actual Shield playback observation.\n");
                for (int index = 0; index < TargetNames.Length; index++)
                    report.AppendLine(TargetNames[index] +
                        " observedControllerLoops=" + observedBaseLoops[index]);
                report.AppendLine("maximumHandleAnchorToForearmCenterDistanceMeters=" + maximumShieldCenterError.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("minimumShieldDesiredFaceAlignment=" + minimumShieldDesiredFaceAlignment.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("minimumShieldVerticalAlignment=" + minimumShieldVerticalAlignment.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("maximumBodyBoundsMagnitudeRatio=" + maximumBodyBoundsRatio.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("maximumSkeletonLocalScaleChange=" + maximumLocalScaleChange.ToString("F8", CultureInfo.InvariantCulture));
                bool looped = observedBaseLoops.All(count => count >= 1);
                bool alignmentPassed = minimumShieldDesiredFaceAlignment >= 0.9999f &&
                    minimumShieldVerticalAlignment >= 0.9999f;
                bool scalePassed = maximumLocalScaleChange <= 0.00001f;
                bool breathingPassed = minimumBlockIdleBreathingPitch <= -1.4f &&
                    maximumBlockIdleBreathingPitch >= 1.4f;
                report.AppendLine("blockIdleBreathingPitchRangeDegrees=" +
                    minimumBlockIdleBreathingPitch.ToString("F6", CultureInfo.InvariantCulture) + ".." +
                    maximumBlockIdleBreathingPitch.ToString("F6", CultureInfo.InvariantCulture) +
                    " breathingMotionPassed=" + breathingPassed);
                report.AppendLine("allStatesLooped=" + looped +
                    " shieldAlignmentPassed=" + alignmentPassed +
                    " skeletonScalePreservationPassed=" + scalePassed);
                report.AppendLine("Observation did not sample, move, rotate, scale, enable or disable any validation target.");
                File.WriteAllText(Absolute(OutputFolder + "/runtime_observation.txt"), report.ToString(), Encoding.UTF8);
                if (!looped || !alignmentPassed || !scalePassed || !breathingPassed)
                    throw new InvalidOperationException("Shield runtime observation failed. allStatesLooped=" + looped +
                        " alignmentPassed=" + alignmentPassed + " scalePassed=" + scalePassed +
                        " breathingPassed=" + breathingPassed + ".");
                Debug.Log("Shield live playback observation complete.");
            }
            catch (Exception exception)
            {
                EditorApplication.update -= ObserveTick;
                observing = false;
                File.WriteAllText(Absolute(OutputFolder + "/runtime_observation_incomplete.txt"), exception.ToString(), Encoding.UTF8);
                Debug.LogException(exception);
            }
        }

        private static Bounds BodyBounds(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(item => !item.transform.IsChildOf(RequireNamedTransform(target.transform, "RightForeArm").Find(ShieldObjectName)))
                .ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException(target.name + " has no body renderer.");
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static Vector3[] ForearmLocalSurfacePoints(GameObject target, Transform forearm)
        {
            var points = new List<Vector3>();
            foreach (SkinnedMeshRenderer renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh shared = renderer.sharedMesh;
                if (shared == null) continue;
                Transform[] bones = renderer.bones;
                int forearmBoneIndex = Array.FindIndex(bones, bone => bone == forearm);
                if (forearmBoneIndex < 0) continue;
                BoneWeight[] weights = shared.boneWeights;
                if (weights.Length != shared.vertexCount) continue;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    Vector3[] vertices = baked.vertices;
                    for (int index = 0; index < vertices.Length; index++)
                    {
                        if (BoneInfluence(weights[index], forearmBoneIndex) < 0.15f) continue;
                        Vector3 world = renderer.transform.TransformPoint(vertices[index]);
                        points.Add(forearm.InverseTransformPoint(world));
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            if (points.Count == 0)
                throw new InvalidOperationException(target.name + " has no RightForeArm-influenced surface points.");
            return points.ToArray();
        }

        private static float BoneInfluence(BoneWeight weight, int boneIndex)
        {
            float result = 0f;
            if (weight.boneIndex0 == boneIndex) result += weight.weight0;
            if (weight.boneIndex1 == boneIndex) result += weight.weight1;
            if (weight.boneIndex2 == boneIndex) result += weight.weight2;
            if (weight.boneIndex3 == boneIndex) result += weight.weight3;
            return result;
        }

        private static void CompareLowerBodyTransforms(Transform targetRoot, Transform referenceRoot,
            ref float positionError, ref float rotationError, ref float scaleError)
        {
            string[] paths = targetRoot.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, targetRoot))
                .Where(IsLegPath)
                .ToArray();
            foreach (string path in paths)
            {
                Transform target = targetRoot.Find(path);
                Transform reference = referenceRoot.Find(path) ??
                    throw new InvalidOperationException("Hands_Empty_Idle is missing lower-body path " + path + ".");
                positionError = Mathf.Max(positionError, Vector3.Distance(target.localPosition, reference.localPosition));
                rotationError = Mathf.Max(rotationError, Quaternion.Angle(target.localRotation, reference.localRotation));
                scaleError = Mathf.Max(scaleError, Vector3.Distance(target.localScale, reference.localScale));
            }
        }

        private static void CaptureTick()
        {
            if (EditorApplication.timeSinceStartup < nextCaptureTime) return;
            try
            {
                var scene = RequireScene();
                var subjects = new List<GameObject> { FindUnique(scene, "Hands_Empty_Idle") };
                subjects.AddRange(TargetNames.Select(name => FindUnique(scene, name)));
                if (capturePhase < 7)
                {
                    foreach (var subject in subjects)
                        RenderSubject(subject, subject.transform.forward, subject.transform.up,
                            Path.Combine(captureDirectory, Safe(subject.name) + "_front_" + capturePhase + ".png"), false);
                    capturePhase++;
                    nextCaptureTime = EditorApplication.timeSinceStartup + captureInterval;
                    return;
                }

                foreach (var subject in subjects)
                {
                    RenderSubject(subject, subject.transform.right, subject.transform.up,
                        Path.Combine(captureDirectory, Safe(subject.name) + "_side.png"), false);
                    RenderSubject(subject, -subject.transform.forward, subject.transform.up,
                        Path.Combine(captureDirectory, Safe(subject.name) + "_back.png"), false);
                    RenderSubject(subject, subject.transform.forward, subject.transform.up,
                        Path.Combine(captureDirectory, Safe(subject.name) + "_right_arm.png"), true);
                    RenderSubject(subject, -subject.transform.forward, subject.transform.up,
                        Path.Combine(captureDirectory, Safe(subject.name) + "_right_arm_back.png"), true);
                }

                var paths = new List<string>();
                foreach (var subject in subjects)
                {
                    string stem = Path.Combine(captureDirectory, Safe(subject.name));
                    for (int phase = 0; phase < 7; phase++)
                        paths.Add(stem + "_front_" + phase + ".png");
                    paths.Add(stem + "_side.png");
                    paths.Add(stem + "_back.png");
                    paths.Add(stem + "_right_arm.png");
                    paths.Add(stem + "_right_arm_back.png");
                }
                Compose(paths.ToArray(), 11, captureOutputPath);
                File.WriteAllText(Path.Combine(captureDirectory, "complete.txt"),
                    "Unmodified live playback capture.\nRows: Hands_Empty_Idle reference, " +
                    string.Join(", ", TargetNames) +
                    "\nColumns: seven front phases at 0.45-second intervals covering the full 2.866667-second Break source, side, back, right-arm front close, right-arm back close.\n" +
                    "No target pose, transform, renderer state, mesh, rig or animation curve was changed for capture.\n",
                    Encoding.UTF8);
                EditorApplication.update -= CaptureTick;
                capturing = false;
                Debug.Log("Shield final live-playback capture complete: " + captureOutputPath);
            }
            catch (Exception exception)
            {
                EditorApplication.update -= CaptureTick;
                capturing = false;
                File.WriteAllText(Path.Combine(captureDirectory, "incomplete.txt"), exception.ToString(), Encoding.UTF8);
                Debug.LogException(exception);
            }
        }

        private static void RenderSubject(GameObject subject, Vector3 viewDirection, Vector3 cameraUp,
            string outputPath, bool rightArmClose)
        {
            Bounds bounds = CombinedSubjectBounds(subject);
            Vector3 center = bounds.center;
            float orthographicSize;
            if (rightArmClose)
            {
                center = RequireNamedTransform(subject.transform, "RightForeArm").position;
                orthographicSize = Mathf.Max(0.65f, bounds.size.y * 0.24f);
            }
            else
            {
                float aspect = CaptureWidth / (float)CaptureHeight;
                orthographicSize = Mathf.Max(bounds.size.y * 0.58f, bounds.size.x / (2f * aspect) * 1.15f);
            }
            float distance = Mathf.Max(4f, bounds.size.magnitude * 1.5f);
            var cameraObject = new GameObject("ShieldReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var render = RenderTexture.GetTemporary(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                camera.cameraType = CameraType.Game;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.size.magnitude + 1f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.transform.position = center + viewDirection.normalized * distance;
                camera.transform.LookAt(center, cameraUp.normalized);
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                pixels.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                pixels.Apply(false, false);
                File.WriteAllBytes(outputPath, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds CombinedSubjectBounds(GameObject subject)
        {
            var renderers = subject.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException(subject.name + " has no visible renderer.");
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static void Compose(string[] paths, int columns, string outputPath)
        {
            var textures = paths.Select(path =>
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                    throw new InvalidOperationException("Capture tile could not be loaded: " + path);
                return texture;
            }).ToArray();
            int rows = Mathf.CeilToInt(textures.Length / (float)columns);
            var sheet = new Texture2D(columns * CaptureWidth, rows * CaptureHeight, TextureFormat.RGB24, false);
            var background = Enumerable.Repeat(new Color(0.035f, 0.045f, 0.06f, 1f), sheet.width * sheet.height).ToArray();
            sheet.SetPixels(background);
            for (int index = 0; index < textures.Length; index++)
            {
                int column = index % columns;
                int row = index / columns;
                sheet.SetPixels(column * CaptureWidth, (rows - row - 1) * CaptureHeight,
                    CaptureWidth, CaptureHeight, textures[index].GetPixels());
            }
            sheet.Apply(false, false);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            foreach (var texture in textures) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must already be the active scene. Active=" + scene.path);
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            var matches = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(item => item.scene == scene && item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one scene object named " + name + ". Found=" + matches.Length);
            return matches[0];
        }

        private static Transform RequireNamedTransform(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one " + name + ". Found=" + matches.Length);
            return matches[0];
        }

        private static string[] OtherRootSignatures(Scene scene)
        {
            var excluded = new HashSet<string>(TargetNames, StringComparer.Ordinal);
            return scene.GetRootGameObjects().Where(item => !excluded.Contains(item.name))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => item.name + "|" + item.activeSelf + "|" + Format(item.transform.localPosition) + "|" +
                    Format(item.transform.localEulerAngles) + "|" + Format(item.transform.localScale))
                .ToArray();
        }

        private static void RequireClose(float actual, float expected, float tolerance, string label)
        {
            if (Mathf.Abs(actual - expected) > tolerance)
                throw new InvalidOperationException(label + " expected " + expected + " but was " + actual + ".");
        }

        private static void RequireMinimum(float actual, float minimum, string label)
        {
            if (actual < minimum)
                throw new InvalidOperationException(label + " expected at least " + minimum + " but was " + actual + ".");
        }

        private static string Format(Vector3 value) =>
            "(" + value.x.ToString("F6", CultureInfo.InvariantCulture) + "," +
            value.y.ToString("F6", CultureInfo.InvariantCulture) + "," +
            value.z.ToString("F6", CultureInfo.InvariantCulture) + ")";

        private static string Safe(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }

        private sealed class PoseSnapshot
        {
            internal readonly string[] Paths;
            internal readonly Vector3[] Positions;
            internal readonly Quaternion[] Rotations;

            internal PoseSnapshot(string[] paths, Vector3[] positions, Quaternion[] rotations)
            {
                Paths = paths;
                Positions = positions;
                Rotations = rotations;
            }
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
    }
}
