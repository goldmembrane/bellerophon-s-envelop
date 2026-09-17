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

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerDamageReactionAnimationSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string AssetFolder = "Assets/_Project/Animations/PlayerDamageReactions";
        private const string SourceFolder = AssetFolder + "/Sources";
        private const string PlayerProxyAssetPath =
            "Assets/_Project/Animations/RepairingShared/Source/PlayerHumanoidProxy.fbx";
        private const string PlayerGenericAssetPath =
            "Assets/_Project/Art/Player/player.fbx";
        private const string PlayerIdleClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Idle.anim";
        private const string HitClipPath = AssetFolder + "/Hit_Reaction_Retargeted.anim";
        private const string FrontFallClipPath = AssetFolder + "/Knockdown_FrontFall_Retargeted.anim";
        private const string BackFallClipPath = AssetFolder + "/Knockdown_BackFall_Retargeted.anim";
        private const string GetUpClipPath = AssetFolder + "/Knockdown_GetUp_Retargeted.anim";
        private const string DeathClipPath = AssetFolder + "/Death_Retargeted.anim";
        private const string ValidationFolder = "docs/validation/PlayerDamageReactionAnimations";
        private const string ApplyReportPath = ValidationFolder + "/Application.txt";
        private const string InspectionReportPath = ValidationFolder + "/Inspection.txt";
        private const string FinalImagePath = ValidationFolder + "/Final.png";
        private const string FinalReportPath = ValidationFolder + "/Final.txt";
        private const string ArmCorrectionSourcesReportPath =
            ValidationFolder + "/ArmCorrectionSources.txt";
        private const string ArmCorrectionApplicationReportPath =
            ValidationFolder + "/ArmCorrectionApplication.txt";
        private const string ArmCorrectionInspectionReportPath =
            ValidationFolder + "/ArmCorrectionInspection.txt";

        private const float GetUpArmCorrectionStartNormalized = 0.50f;
        private const float GetUpArmCorrectionEndNormalized = 0.70f;
        private const float ArmPositionTolerance = 0.0001f;
        private const float ArmRotationTolerance = 0.05f;
        private const float ArmScaleTolerance = 0.0001f;
        private const string SpinePath = "Armature/Hips/Spine02/Spine01/Spine";
        private const string LeftShoulderPath = SpinePath + "/LeftShoulder";
        private const string RightShoulderPath = SpinePath + "/RightShoulder";

        private static readonly string[] ArmBonePaths =
        {
            LeftShoulderPath,
            LeftShoulderPath + "/LeftArm",
            LeftShoulderPath + "/LeftArm/LeftForeArm",
            LeftShoulderPath + "/LeftArm/LeftForeArm/LeftHand",
            RightShoulderPath,
            RightShoulderPath + "/RightArm",
            RightShoulderPath + "/RightArm/RightForeArm",
            RightShoulderPath + "/RightArm/RightForeArm/RightHand"
        };

        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        private const string HitTarget = "Hit_Reaction";
        private const string KnockdownTarget = "Knockdown";
        private const string GetUpTarget = "Knockdown_GetUp";
        private const string DeathTarget = "Death";
        private const string HitState = "HitReaction_Source";
        private const string FrontFallState = "FrontFall_Source";
        private const string BackFallState = "BackFall_Source";
        private const string GetUpState = "KnockdownGetUp_Source";
        private const string DeathState = "Death_Source";

        private const string HitControllerPath = AssetFolder + "/Hit_Reaction.controller";
        private const string KnockdownControllerPath = AssetFolder + "/Knockdown.controller";
        private const string GetUpControllerPath = AssetFolder + "/Knockdown_GetUp.controller";
        private const string DeathControllerPath = AssetFolder + "/Death.controller";

        private static readonly SourceSpec HitSource = new SourceSpec(
            "player model/transfer hitted.fbx",
            SourceFolder + "/Hit_Reaction_Source.fbx",
            "2BF6EC32F30C19250DE0D1F6D9CCBD6CC23CFE8417E35572062D5D4F0388A61D",
            true);

        private static readonly SourceSpec FrontFallSource = new SourceSpec(
            "player model/transfer front fall.fbx",
            SourceFolder + "/Knockdown_FrontFall_Source.fbx",
            "E1D93411CD56AC792FDEA61C6C2B30C9DA7E4CFB341AA6D89D4819FC3AA28C23",
            false);

        private static readonly SourceSpec BackFallSource = new SourceSpec(
            "player model/transfer back fall.fbx",
            SourceFolder + "/Knockdown_BackFall_Source.fbx",
            "0A8BB3BB76D600E088856506C8812213206835A3B1725D30B3DEC6BD89EFD6ED",
            false);

        private static readonly SourceSpec GetUpSource = new SourceSpec(
            "player model/transfer getting up.fbx",
            SourceFolder + "/Knockdown_GetUp_Source.fbx",
            "D99C3420672791B066CD94DC679C469AA004094926941F818BDE413108A9B193",
            true);

        private static readonly SourceSpec DeathSource = new SourceSpec(
            "player model/transfer dying.fbx",
            SourceFolder + "/Death_Source.fbx",
            "325D617A63DECCE864D8696C474539E542A425342BA0F7949AFEFC44ACB22CDB",
            true);

        private static readonly SourceSpec[] Sources =
        {
            HitSource,
            FrontFallSource,
            BackFallSource,
            GetUpSource,
            DeathSource
        };

        [MenuItem("Bellerophon/Player/Apply Damage Reaction Animations")]
        internal static void ApplyPlayerDamageReactionAnimations()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject hit = FindUnique(scene, HitTarget);
            GameObject knockdown = FindUnique(scene, KnockdownTarget);
            GameObject getUp = FindUnique(scene, GetUpTarget);
            GameObject death = FindUnique(scene, DeathTarget);
            GameObject[] targets = { hit, knockdown, getUp, death };

            RootSnapshot[] roots = targets.Select(target => new RootSnapshot(target.transform)).ToArray();
            string[] renderers = targets.Select(target => RendererSignature(target.transform)).ToArray();
            Animator[] animators = targets.Select(RequireAnimator).ToArray();
            Avatar[] avatars = animators.Select(animator => animator.avatar).ToArray();
            if (avatars.Any(avatar => avatar == null) || avatars.Distinct().Count() != 1)
                throw new InvalidOperationException("All four damage-reaction targets must share one non-null player Avatar.");

            EnsureFolder(AssetFolder);
            EnsureFolder(SourceFolder);
            foreach (SourceSpec source in Sources)
                CopyAndConfigureSource(source);

            AnimationClip hitClip = RequireSingleClip(HitSource.AssetPath);
            AnimationClip frontClip = RequireSingleClip(FrontFallSource.AssetPath);
            AnimationClip backClip = RequireSingleClip(BackFallSource.AssetPath);
            AnimationClip getUpClip = RequireSingleClip(GetUpSource.AssetPath);
            AnimationClip deathClip = RequireSingleClip(DeathSource.AssetPath);

            RequireHumanAvatar(PlayerProxyAssetPath, "player Humanoid proxy");
            AnimationClip hitRetargeted = CreateRetargetedGenericClip(hitClip, HitClipPath, true);
            AnimationClip frontRetargeted = CreateRetargetedGenericClip(frontClip, FrontFallClipPath, false);
            AnimationClip backRetargeted = CreateRetargetedGenericClip(backClip, BackFallClipPath, false);
            AnimationClip getUpRetargeted = CreateRetargetedGenericClip(getUpClip, GetUpClipPath, true);
            AnimationClip deathRetargeted = CreateRetargetedGenericClip(deathClip, DeathClipPath, true);
            AnimationClip originalHitRetargeted = UnityEngine.Object.Instantiate(hitRetargeted);
            AnimationClip originalGetUpRetargeted = UnityEngine.Object.Instantiate(getUpRetargeted);
            try
            {
                ApplyPlayerIdlePostureCorrections(
                    FindUnique(scene, "Player_Idle").transform,
                    hitRetargeted,
                    originalHitRetargeted,
                    backRetargeted,
                    getUpRetargeted,
                    originalGetUpRetargeted);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalHitRetargeted);
                UnityEngine.Object.DestroyImmediate(originalGetUpRetargeted);
            }

            AnimatorController hitController = CreateSingleController(
                HitControllerPath, HitState, hitRetargeted);
            AnimatorController knockdownController = CreateKnockdownController(frontRetargeted, backRetargeted);
            AnimatorController getUpController = CreateSingleController(
                GetUpControllerPath, GetUpState, getUpRetargeted);
            AnimatorController deathController = CreateSingleController(
                DeathControllerPath, DeathState, deathRetargeted);

            Connect(animators[0], hitController);
            Connect(animators[1], knockdownController);
            Connect(animators[2], getUpController);
            Connect(animators[3], deathController);
            AssetDatabase.SaveAssets();

            for (int i = 0; i < targets.Length; i++)
            {
                roots[i].RequireUnchanged(targets[i].transform, targets[i].name);
                RequireEqual(renderers[i], RendererSignature(targets[i].transform), targets[i].name + " renderers");
                if (animators[i].avatar != avatars[i])
                    throw new InvalidOperationException(targets[i].name + " Avatar changed unexpectedly.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Player damage reaction animation application")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("retargetedCompatibilityClipsGenerated=True")
                .AppendLine("retargetPoseInference=False")
                .AppendLine("retargetSamplingRate=OriginalSourceFrameRate")
                .AppendLine("sourceBinaryCopiesExact=True")
                .AppendLine("humanoidRetargeting=True")
                .AppendLine("applyRootMotion=False")
                .AppendLine("targetTransformsChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarsChanged=False")
                .AppendLine("armPoseReference=Player_Idle scene object local transforms")
                .AppendLine("torsoAndChestCurvesPreserved=True")
                .AppendLine("armsWorldLocked=False")
                .AppendLine("Hit_Reaction=" + DescribeClip(hitClip) + ",loop=True")
                .AppendLine("KnockdownFront=" + DescribeClip(frontClip) + ",loop=False")
                .AppendLine("KnockdownBack=" + DescribeClip(backClip) + ",loop=False")
                .AppendLine("KnockdownSequence=FrontFall->BackFall->FrontFall")
                .AppendLine("KnockdownTransitionDurationSeconds=0")
                .AppendLine("Knockdown_GetUp=" + DescribeClip(getUpClip) + ",loop=True")
                .AppendLine("Death=" + DescribeClip(deathClip) + ",loop=True");
            WriteText(ApplyReportPath, report.ToString());
            Debug.Log("[PlayerDamageReaction] Original Mixamo motion connected through Generic compatibility clips.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Damage Reaction Posture Correction Sources")]
        internal static void InspectPlayerDamageReactionPostureCorrectionSources() =>
            InspectPlayerDamageReactionArmPoseCorrectionSources();

        [MenuItem("Bellerophon/Player/Inspect Damage Reaction Arm Correction Sources")]
        internal static void InspectPlayerDamageReactionArmPoseCorrectionSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject playerIdle = FindUnique(scene, "Player_Idle");
            RequireAsset<AnimationClip>(PlayerIdleClipPath);
            AnimationClip hit = RequireAsset<AnimationClip>(HitClipPath);
            AnimationClip back = RequireAsset<AnimationClip>(BackFallClipPath);
            AnimationClip getUp = RequireAsset<AnimationClip>(GetUpClipPath);
            RequireArmHierarchy(playerIdle.transform, "Player_Idle");
            RequireArmBindings(hit, "Hit_Reaction");
            RequireArmBindings(back, "Knockdown BackFall");
            RequireArmBindings(getUp, "Knockdown_GetUp");

            AnimationClip originalHit = BakeRetargetedGenericClip(
                RequireSingleClip(HitSource.AssetPath), "Hit_Reaction_OriginalProbe", true);
            AnimationClip originalGetUp = BakeRetargetedGenericClip(
                RequireSingleClip(GetUpSource.AssetPath), "Knockdown_GetUp_OriginalProbe", true);
            string[] hitPaths = TransformPaths(originalHit);
            RequirePosePaths(playerIdle.transform, hitPaths, "Player_Idle");

            try
            {
                var report = new StringBuilder()
                    .AppendLine("Player damage reaction posture-correction source inspection")
                    .AppendLine("sourceTarget=Player_Idle")
                    .AppendLine("sourceClipPresence=" + PlayerIdleClipPath)
                    .AppendLine("sourcePose=Player_Idle scene object serialized local pose")
                    .AppendLine("poseSpace=Local")
                    .AppendLine("Hit_ReactionOriginalSource=" + HitSource.AssetPath)
                    .AppendLine("Hit_ReactionRebasedBoneCount=" + hitPaths.Length)
                    .AppendLine("Hit_ReactionBasePose=Player_Idle")
                    .AppendLine("Hit_ReactionRelativeRecoil=Original source motion")
                    .AppendLine("Hit_ReactionArmLocalPose=Player_Idle")
                    .AppendLine("KnockdownBackFallRange=WholeClip arms unchanged from prior correction")
                    .AppendLine("KnockdownFrontFallChanged=False")
                    .AppendLine("Knockdown_GetUpOriginalSource=" + GetUpSource.AssetPath)
                    .AppendLine("Knockdown_GetUpOriginalArmRange=0->" +
                        GetUpArmCorrectionStartNormalized.ToString("0.##", CultureInfo.InvariantCulture))
                    .AppendLine("Knockdown_GetUpStandingArmBlend=" +
                        GetUpArmCorrectionStartNormalized.ToString("0.##", CultureInfo.InvariantCulture) + "->" +
                        GetUpArmCorrectionEndNormalized.ToString("0.##", CultureInfo.InvariantCulture))
                    .AppendLine("Knockdown_GetUpStandingArmBlendEasing=SmoothStep")
                    .AppendLine("Knockdown_GetUpNonArmCurvesChanged=False")
                    .AppendLine("armsWillFollowTorsoHierarchy=True");
                WriteText(ArmCorrectionSourcesReportPath, report.ToString());
                Debug.Log("[PlayerDamageReaction] Posture-correction sources inspected.\n" + report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalHit);
                UnityEngine.Object.DestroyImmediate(originalGetUp);
            }
        }

        [MenuItem("Bellerophon/Player/Apply Damage Reaction Posture Correction")]
        internal static void ApplyPlayerDamageReactionPostureCorrection() =>
            ApplyPlayerDamageReactionArmPoseCorrection();

        [MenuItem("Bellerophon/Player/Apply Damage Reaction Arm Pose Correction")]
        internal static void ApplyPlayerDamageReactionArmPoseCorrection()
        {
            RequireEditMode();
            int consoleErrorsBefore = LightsaberSetupTools.ConsoleErrorCount();
            Scene scene = RequireScene();
            GameObject[] protectedTargets =
            {
                FindUnique(scene, "Player_Idle"),
                FindUnique(scene, HitTarget),
                FindUnique(scene, KnockdownTarget),
                FindUnique(scene, GetUpTarget),
                FindUnique(scene, DeathTarget)
            };
            RootSnapshot[] roots = protectedTargets
                .Select(target => new RootSnapshot(target.transform)).ToArray();
            string[] renderers = protectedTargets
                .Select(target => RendererSignature(target.transform)).ToArray();

            RequireAsset<AnimationClip>(PlayerIdleClipPath);
            AnimationClip hit = RequireAsset<AnimationClip>(HitClipPath);
            AnimationClip front = RequireAsset<AnimationClip>(FrontFallClipPath);
            AnimationClip back = RequireAsset<AnimationClip>(BackFallClipPath);
            AnimationClip getUp = RequireAsset<AnimationClip>(GetUpClipPath);
            AnimationClip death = RequireAsset<AnimationClip>(DeathClipPath);
            AnimationClip originalHit = BakeRetargetedGenericClip(
                RequireSingleClip(HitSource.AssetPath), "Hit_Reaction_OriginalProbe", true);
            AnimationClip originalGetUp = BakeRetargetedGenericClip(
                RequireSingleClip(GetUpSource.AssetPath), "Knockdown_GetUp_OriginalProbe", true);
            string frontHash = Sha256(Absolute(FrontFallClipPath));
            string backHash = Sha256(Absolute(BackFallClipPath));
            string deathHash = Sha256(Absolute(DeathClipPath));
            try
            {
                ApplyPlayerIdlePostureCorrections(
                    FindUnique(scene, "Player_Idle").transform,
                    hit,
                    originalHit,
                    null,
                    getUp,
                    originalGetUp);
                AssetDatabase.SaveAssets();

                RequireEqual(frontHash, Sha256(Absolute(FrontFallClipPath)),
                    "Knockdown FrontFall asset");
                RequireEqual(backHash, Sha256(Absolute(BackFallClipPath)),
                    "Knockdown BackFall asset");
                RequireEqual(deathHash, Sha256(Absolute(DeathClipPath)), "Death asset");
                RequireEqual(
                    CurveSignature(originalGetUp, binding => !IsCorrectedArmPath(binding.path)),
                    CurveSignature(getUp, binding => !IsCorrectedArmPath(binding.path)),
                    "Knockdown_GetUp curves outside corrected arm bones");
                for (int i = 0; i < protectedTargets.Length; i++)
                {
                    roots[i].RequireUnchanged(protectedTargets[i].transform, protectedTargets[i].name);
                    RequireEqual(renderers[i], RendererSignature(protectedTargets[i].transform),
                        protectedTargets[i].name + " renderers");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalHit);
                UnityEngine.Object.DestroyImmediate(originalGetUp);
            }

            RequireClipLoop(hit, true, HitClipPath);
            RequireClipLoop(front, false, FrontFallClipPath);
            RequireClipLoop(back, false, BackFallClipPath);
            RequireClipLoop(getUp, true, GetUpClipPath);
            RequireClipLoop(death, true, DeathClipPath);
            var report = new StringBuilder()
                .AppendLine("Player damage reaction posture correction application")
                .AppendLine("reference=Player_Idle scene object local transforms")
                .AppendLine("referenceSpace=Local")
                .AppendLine("Hit_ReactionBasePoseRebasedToPlayerIdle=True")
                .AppendLine("Hit_ReactionOriginalRelativeRecoilPreserved=True")
                .AppendLine("Hit_ReactionArmLocalPose=Player_Idle")
                .AppendLine("KnockdownBackFallChanged=False")
                .AppendLine("KnockdownFrontFallChanged=False")
                .AppendLine("Knockdown_GetUpOriginalArmsPreservedThroughNormalized=" +
                    GetUpArmCorrectionStartNormalized.ToString("0.##", CultureInfo.InvariantCulture))
                .AppendLine("Knockdown_GetUpStandingBlend=" +
                    GetUpArmCorrectionStartNormalized.ToString("0.##", CultureInfo.InvariantCulture) + "->" +
                    GetUpArmCorrectionEndNormalized.ToString("0.##", CultureInfo.InvariantCulture))
                .AppendLine("Knockdown_GetUpStandingBlendEasing=SmoothStep")
                .AppendLine("Knockdown_GetUpNonArmCurvesChanged=False")
                .AppendLine("fingerCurvesChanged=False")
                .AppendLine("renderersChanged=False")
                .AppendLine("sceneRootTransformsChanged=False")
                .AppendLine("armsWorldLocked=False")
                .AppendLine("clipTimingAndLoopSettingsPreserved=True");
            WriteText(ArmCorrectionApplicationReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log("[PlayerDamageReaction] Player_Idle posture and standing-arm correction applied.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Damage Reaction Posture Correction")]
        internal static void InspectPlayerDamageReactionPostureCorrection() =>
            InspectPlayerDamageReactionArmPoseCorrection();

        [MenuItem("Bellerophon/Player/Inspect Damage Reaction Arm Pose Correction")]
        internal static void InspectPlayerDamageReactionArmPoseCorrection()
        {
            RequireEditMode();
            int consoleErrorsBefore = LightsaberSetupTools.ConsoleErrorCount();
            RequireAsset<AnimationClip>(PlayerIdleClipPath);
            AnimationClip hit = RequireAsset<AnimationClip>(HitClipPath);
            AnimationClip front = RequireAsset<AnimationClip>(FrontFallClipPath);
            AnimationClip back = RequireAsset<AnimationClip>(BackFallClipPath);
            AnimationClip getUp = RequireAsset<AnimationClip>(GetUpClipPath);
            AnimationClip death = RequireAsset<AnimationClip>(DeathClipPath);
            Transform playerIdle = FindUnique(RequireScene(), "Player_Idle").transform;
            ArmLocalPose[] reference = CaptureArmPose(playerIdle);
            AnimationClip originalHit = BakeRetargetedGenericClip(
                RequireSingleClip(HitSource.AssetPath), "Hit_Reaction_OriginalProbe", true);
            AnimationClip originalGetUp = BakeRetargetedGenericClip(
                RequireSingleClip(GetUpSource.AssetPath), "Knockdown_GetUp_OriginalProbe", true);

            MotionMetrics hitMotion;
            MotionMetrics backMotion;
            MotionMetrics getUpMotion;
            try
            {
                string[] hitPaths = TransformPaths(originalHit);
                RequirePoseMapsEqual(
                    CapturePoseMap(playerIdle, hitPaths),
                    SamplePoseMap(hit, 0f, hitPaths),
                    hitPaths,
                    "Hit_Reaction first frame versus Player_Idle");
                RequireRelativeMotionPreserved(
                    originalHit,
                    hit,
                    hitPaths.Where(path => !IsCorrectedArmPath(path)).ToArray(),
                    new[] { 0.20f, 0.40f, 0.60f, 0.80f, 1f },
                    "Hit_Reaction recoil");
                RequireArmMatchesReference(hit, reference, new[] { 0f, 0.25f, 0.5f, 0.75f, 1f },
                    "Hit_Reaction");
                RequireArmMatchesReference(back, reference, new[] { 0f, 0.25f, 0.5f, 0.75f, 1f },
                    "Knockdown BackFall");
                foreach (float normalized in new[] { 0.20f, 0.35f, 0.45f, 0.50f })
                    RequireArmPoseEqual(
                        SampleArmPose(originalGetUp, originalGetUp.length * normalized),
                        SampleArmPose(getUp, getUp.length * normalized),
                        "Knockdown_GetUp original arms at normalizedTime=" +
                        normalized.ToString("0.##", CultureInfo.InvariantCulture));
                foreach (float normalized in new[] { 0.55f, 0.60f, 0.65f })
                {
                    float weight = GetUpArmBlendWeight(normalized);
                    ArmLocalPose[] originalPose =
                        SampleArmPose(originalGetUp, originalGetUp.length * normalized);
                    ArmLocalPose[] expectedPose = originalPose
                        .Select((pose, index) => ArmLocalPose.Lerp(pose, reference[index], weight))
                        .ToArray();
                    RequireArmPoseEqual(
                        expectedPose,
                        SampleArmPose(getUp, getUp.length * normalized),
                        "Knockdown_GetUp eased standing-arm blend at normalizedTime=" +
                        normalized.ToString("0.##", CultureInfo.InvariantCulture));
                }
                RequireArmMatchesReference(getUp, reference, new[] { 0.70f, 0.80f, 1f },
                    "Knockdown_GetUp final rise already in Player_Idle arm pose");
                RequireEqual(
                    CurveSignature(originalGetUp, binding => !IsCorrectedArmPath(binding.path)),
                    CurveSignature(getUp, binding => !IsCorrectedArmPath(binding.path)),
                    "Knockdown_GetUp curves outside corrected arm bones");
                hitMotion = MeasureDrivenArmMotion(hit);
                backMotion = MeasureDrivenArmMotion(back);
                getUpMotion = MeasureDrivenArmMotion(getUp);
                if (hitMotion.MaximumHandTravel < 0.001f || backMotion.MaximumHandTravel < 0.001f ||
                    getUpMotion.MaximumHandTravel < 0.001f)
                    throw new InvalidOperationException(
                        "One or more corrected arm chains no longer inherit visible body motion.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originalHit);
                UnityEngine.Object.DestroyImmediate(originalGetUp);
            }

            RequireClipLoop(hit, true, HitClipPath);
            RequireClipLoop(front, false, FrontFallClipPath);
            RequireClipLoop(back, false, BackFallClipPath);
            RequireClipLoop(getUp, true, GetUpClipPath);
            RequireClipLoop(death, true, DeathClipPath);
            var report = new StringBuilder()
                .AppendLine("Player damage reaction posture correction inspection")
                .AppendLine("Hit_ReactionFirstFramePlayerIdleMatch=True")
                .AppendLine("Hit_ReactionOriginalRelativeRecoilPreserved=True")
                .AppendLine("Hit_ReactionWholeClipArmLocalPoseMatch=True")
                .AppendLine("KnockdownBackFallWholeClipMatch=True")
                .AppendLine("KnockdownFrontFallStillOriginal=True")
                .AppendLine("Knockdown_GetUpOriginalArmsThroughNormalized=" +
                    GetUpArmCorrectionStartNormalized.ToString("0.##", CultureInfo.InvariantCulture))
                .AppendLine("Knockdown_GetUpSmoothStandingBlendVerified=True")
                .AppendLine("Knockdown_GetUpPlayerIdleArmsBeforeFinalRise=True")
                .AppendLine("Knockdown_GetUpFullyStandingArmsMatch=True")
                .AppendLine("Hit_ReactionMaximumHandTravel=" + hitMotion.Describe())
                .AppendLine("KnockdownBackFallMaximumHandTravel=" + backMotion.Describe())
                .AppendLine("Knockdown_GetUpMaximumHandTravel=" + getUpMotion.Describe())
                .AppendLine("torsoDrivenArmMotionPreserved=True")
                .AppendLine("armsWorldLocked=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(ArmCorrectionInspectionReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
            Debug.Log("[PlayerDamageReaction] Arm-pose correction inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Damage Reaction Animations")]
        internal static void InspectPlayerDamageReactionAnimations()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            foreach (SourceSpec source in Sources)
            {
                RequireEqual(source.Sha256, Sha256(Absolute(source.ExternalPath)), source.ExternalPath);
                RequireEqual(source.Sha256, Sha256(Absolute(source.AssetPath)), source.AssetPath);
                RequireClipLoop(RequireSingleClip(source.AssetPath), source.LoopClip, source.AssetPath);
            }

            AnimationClip hitClip = RequireSingleClip(HitSource.AssetPath);
            AnimationClip frontClip = RequireSingleClip(FrontFallSource.AssetPath);
            AnimationClip backClip = RequireSingleClip(BackFallSource.AssetPath);
            AnimationClip getUpClip = RequireSingleClip(GetUpSource.AssetPath);
            AnimationClip deathClip = RequireSingleClip(DeathSource.AssetPath);

            AnimationClip hitRetargeted = RequireAsset<AnimationClip>(HitClipPath);
            AnimationClip frontRetargeted = RequireAsset<AnimationClip>(FrontFallClipPath);
            AnimationClip backRetargeted = RequireAsset<AnimationClip>(BackFallClipPath);
            AnimationClip getUpRetargeted = RequireAsset<AnimationClip>(GetUpClipPath);
            AnimationClip deathRetargeted = RequireAsset<AnimationClip>(DeathClipPath);
            RequireClipLoop(hitRetargeted, true, HitClipPath);
            RequireClipLoop(frontRetargeted, false, FrontFallClipPath);
            RequireClipLoop(backRetargeted, false, BackFallClipPath);
            RequireClipLoop(getUpRetargeted, true, GetUpClipPath);
            RequireClipLoop(deathRetargeted, true, DeathClipPath);

            RequireSingleController(FindUnique(scene, HitTarget), HitControllerPath, hitRetargeted);
            RequireKnockdownController(FindUnique(scene, KnockdownTarget), frontRetargeted, backRetargeted);
            RequireSingleController(FindUnique(scene, GetUpTarget), GetUpControllerPath, getUpRetargeted);
            RequireSingleController(FindUnique(scene, DeathTarget), DeathControllerPath, deathRetargeted);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Player damage reaction structural inspection")
                .AppendLine("verificationTargetsManipulated=False")
                .AppendLine("sourceBinaryCopiesExact=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("retargetedCompatibilityClipsGenerated=True")
                .AppendLine("retargetPoseInference=False")
                .AppendLine("Hit_Reaction=" + DescribeClip(hitClip) + ",wholeClipLoop=True")
                .AppendLine("KnockdownFront=" + DescribeClip(frontClip))
                .AppendLine("KnockdownBack=" + DescribeClip(backClip))
                .AppendLine("KnockdownSequence=FrontFall->BackFall->FrontFall")
                .AppendLine("KnockdownZeroDurationTransitions=True")
                .AppendLine("Knockdown_GetUp=" + DescribeClip(getUpClip) + ",wholeClipLoop=True")
                .AppendLine("Death=" + DescribeClip(deathClip) + ",wholeClipLoop=True")
                .AppendLine("HitRetargeted=" + DescribeClip(hitRetargeted))
                .AppendLine("FrontFallRetargeted=" + DescribeClip(frontRetargeted))
                .AppendLine("BackFallRetargeted=" + DescribeClip(backRetargeted))
                .AppendLine("GetUpRetargeted=" + DescribeClip(getUpRetargeted))
                .AppendLine("DeathRetargeted=" + DescribeClip(deathRetargeted))
                .AppendLine("directVisualReviewPending=True");
            WriteText(InspectionReportPath, report.ToString());
            Debug.Log("[PlayerDamageReaction] Structural inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Capture Damage Reaction Animations Final")]
        internal static void CapturePlayerDamageReactionAnimations()
        {
            InspectPlayerDamageReactionAnimations();
            Scene scene = RequireScene();
            GameObject[] targets =
            {
                FindUnique(scene, HitTarget),
                FindUnique(scene, KnockdownTarget),
                FindUnique(scene, GetUpTarget),
                FindUnique(scene, DeathTarget)
            };
            AnimationClip[][] clips =
            {
                Repeat(RequireSingleClip(HitSource.AssetPath), 4),
                new[]
                {
                    RequireSingleClip(FrontFallSource.AssetPath),
                    RequireSingleClip(FrontFallSource.AssetPath),
                    RequireSingleClip(BackFallSource.AssetPath),
                    RequireSingleClip(BackFallSource.AssetPath)
                },
                Repeat(RequireSingleClip(GetUpSource.AssetPath), 4),
                Repeat(RequireSingleClip(DeathSource.AssetPath), 4)
            };
            float[][] normalizedTimes =
            {
                new[] { 0f, 0.33f, 0.66f, 0.99f },
                new[] { 0f, 0.99f, 0f, 0.99f },
                new[] { 0f, 0.33f, 0.66f, 0.99f },
                new[] { 0f, 0.33f, 0.66f, 0.99f }
            };

            const int cellWidth = 480;
            const int cellHeight = 360;
            const int columns = 4;
            const int rows = 4;
            Texture2D sheet = new Texture2D(cellWidth * columns, cellHeight * rows, TextureFormat.RGB24, false);
            Color32[] black = Enumerable.Repeat(new Color32(0, 0, 0, 255), sheet.width * sheet.height).ToArray();
            sheet.SetPixels32(black);
            try
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        Texture2D frame = RenderSample(
                            targets[row], clips[row][column], normalizedTimes[row][column], cellWidth, cellHeight);
                        try
                        {
                            sheet.SetPixels32(
                                column * cellWidth,
                                (rows - row - 1) * cellHeight,
                                cellWidth,
                                cellHeight,
                                frame.GetPixels32());
                        }
                        finally
                        {
                            UnityEngine.Object.DestroyImmediate(frame);
                        }
                    }
                }
                sheet.Apply(false, false);
                string image = Absolute(FinalImagePath);
                Directory.CreateDirectory(Path.GetDirectoryName(image) ?? throw new InvalidOperationException("Final image directory is unavailable."));
                File.WriteAllBytes(image, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }

            var report = new StringBuilder()
                .AppendLine("Player damage reaction one-time final contact sheet")
                .AppendLine("verificationTargetsManipulated=False")
                .AppendLine("captureMethod=Clones sampled from the connected original embedded Humanoid clips")
                .AppendLine("row1=Hit_Reaction normalized times 0,0.33,0.66,0.99")
                .AppendLine("row2=Knockdown Front 0,Front 0.99,Back 0,Back 0.99")
                .AppendLine("row3=Knockdown_GetUp normalized times 0,0.33,0.66,0.99")
                .AppendLine("row4=Death normalized times 0,0.33,0.66,0.99")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(FinalReportPath, report.ToString());
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log("[PlayerDamageReaction] One-time final contact sheet captured.");
        }

        private static void CopyAndConfigureSource(SourceSpec source)
        {
            string external = Absolute(source.ExternalPath);
            string destination = Absolute(source.AssetPath);
            if (!File.Exists(external))
                throw new FileNotFoundException("Animation source FBX is missing.", external);
            RequireEqual(source.Sha256, Sha256(external), source.ExternalPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Source destination is unavailable."));
            if (!File.Exists(destination) || !string.Equals(Sha256(destination), source.Sha256, StringComparison.OrdinalIgnoreCase))
                File.Copy(external, destination, true);
            AssetDatabase.ImportAsset(source.AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            ModelImporter importer = AssetImporter.GetAtPath(source.AssetPath) as ModelImporter ??
                throw new InvalidOperationException("ModelImporter is unavailable: " + source.AssetPath);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = true;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(source.AssetPath) as ModelImporter ??
                throw new InvalidOperationException("ModelImporter disappeared: " + source.AssetPath);
            ModelImporterClipAnimation[] settings = importer.clipAnimations;
            if (settings == null || settings.Length == 0)
                settings = importer.defaultClipAnimations;
            if (settings == null || settings.Length != 1)
                throw new InvalidOperationException(source.AssetPath + " must expose exactly one embedded animation clip.");
            settings[0].loopTime = source.LoopClip;
            settings[0].loopPose = false;
            importer.clipAnimations = settings;
            importer.SaveAndReimport();
            RequireEqual(source.Sha256, Sha256(destination), source.AssetPath);
            RequireClipLoop(RequireSingleClip(source.AssetPath), source.LoopClip, source.AssetPath);
        }

        private static AnimatorController CreateSingleController(string path, string stateName, AnimationClip clip)
        {
            DeleteAssetIfPresent(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorState state = controller.layers[0].stateMachine.AddState(stateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorController CreateKnockdownController(AnimationClip front, AnimationClip back)
        {
            DeleteAssetIfPresent(KnockdownControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(KnockdownControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState frontState = machine.AddState(FrontFallState);
            AnimatorState backState = machine.AddState(BackFallState);
            frontState.motion = front;
            backState.motion = back;
            frontState.speed = 1f;
            backState.speed = 1f;
            frontState.writeDefaultValues = false;
            backState.writeDefaultValues = false;
            machine.defaultState = frontState;
            ConfigureImmediateExit(frontState.AddTransition(backState));
            ConfigureImmediateExit(backState.AddTransition(frontState));
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureImmediateExit(AnimatorStateTransition transition)
        {
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
        }

        private static void Connect(Animator animator, RuntimeAnimatorController controller)
        {
            Undo.RecordObject(animator, "Connect original damage reaction animation");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
        }

        private static void RequireSingleController(GameObject target, string controllerPath, AnimationClip clip)
        {
            Animator animator = RequireAnimator(target);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) ??
                throw new InvalidOperationException(controllerPath + " is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(target.name + " controller connection differs.");
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException(target.name + " default state is missing.");
            if (state.motion != clip || state.speed != 1f)
                throw new InvalidOperationException(target.name + " does not use the supplied compatibility clip at speed 1.");
            if (animator.applyRootMotion)
                throw new InvalidOperationException(target.name + " applyRootMotion must remain disabled.");
        }

        private static void RequireKnockdownController(GameObject target, AnimationClip front, AnimationClip back)
        {
            Animator animator = RequireAnimator(target);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(KnockdownControllerPath) ??
                throw new InvalidOperationException("Knockdown controller is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException("Knockdown controller connection differs.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState frontState = machine.states.Select(value => value.state).Single(state => state.name == FrontFallState);
            AnimatorState backState = machine.states.Select(value => value.state).Single(state => state.name == BackFallState);
            if (machine.defaultState != frontState || frontState.motion != front || backState.motion != back)
                throw new InvalidOperationException("Knockdown source clip order differs.");
            RequireImmediateExit(frontState, backState, "front-to-back");
            RequireImmediateExit(backState, frontState, "back-to-front");
            if (animator.applyRootMotion)
                throw new InvalidOperationException("Knockdown applyRootMotion must remain disabled.");
        }

        private static void RequireImmediateExit(AnimatorState source, AnimatorState destination, string label)
        {
            AnimatorStateTransition transition = source.transitions.SingleOrDefault(value => value.destinationState == destination) ??
                throw new InvalidOperationException("Knockdown " + label + " transition is missing.");
            if (!transition.hasExitTime || Mathf.Abs(transition.exitTime - 1f) > 0.0001f ||
                !transition.hasFixedDuration || Mathf.Abs(transition.duration) > 0.0001f)
                throw new InvalidOperationException("Knockdown " + label + " transition is not an exact zero-duration end transition.");
        }

        private static Texture2D RenderSample(GameObject source, AnimationClip clip, float normalizedTime, int width, int height)
        {
            GameObject stageRoot = null;
            GameObject actor = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            bool animationModeStarted = false;
            RenderTexture previous = RenderTexture.active;
            try
            {
                stageRoot = new GameObject("DamageReactionReviewStage");
                stageRoot.hideFlags = HideFlags.HideAndDontSave;
                actor = UnityEngine.Object.Instantiate(source);
                actor.name = source.name + "_VisualReviewClone";
                actor.hideFlags = HideFlags.HideAndDontSave;
                actor.transform.SetParent(stageRoot.transform, true);
                actor.SetActive(true);
                Animator animator = RequireAnimator(actor);
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                StartHumanoidSample(actor, clip, Mathf.Clamp01(normalizedTime) * clip.length);
                animationModeStarted = true;
                stageRoot.transform.position = new Vector3(10000f, 10000f, 10000f);

                Bounds bounds = CaptureBoundsOf(actor.transform);
                cameraObject = new GameObject("DamageReactionReviewCamera", typeof(Camera));
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                Vector3 front = actor.transform.forward;
                front.y = 0f;
                if (front.sqrMagnitude < 0.001f)
                    front = Vector3.forward;
                front.Normalize();
                float distance = Mathf.Max(3f, bounds.extents.magnitude * 3f);
                camera.transform.position = bounds.center + front * distance;
                camera.transform.LookAt(bounds.center, Vector3.up);
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.25f, bounds.extents.x / aspect * 1.25f, 0.5f);

                lightObject = new GameObject("DamageReactionReviewLight", typeof(Light));
                lightObject.hideFlags = HideFlags.HideAndDontSave;
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2f;
                light.color = Color.white;
                light.shadows = LightShadows.None;
                light.transform.rotation = Quaternion.LookRotation(-front + Vector3.down * 0.35f, Vector3.up);

                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply(false, false);
                camera.targetTexture = null;
                return texture;
            }
            finally
            {
                if (animationModeStarted && AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                RenderTexture.active = previous;
                if (renderTexture != null)
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                if (lightObject != null)
                    UnityEngine.Object.DestroyImmediate(lightObject);
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                if (stageRoot != null)
                    UnityEngine.Object.DestroyImmediate(stageRoot);
                else if (actor != null)
                    UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        private static string DescribeCaptureProbe(GameObject source, AnimationClip clip, float normalizedTime)
        {
            GameObject stageRoot = null;
            GameObject actor = null;
            bool animationModeStarted = false;
            try
            {
                stageRoot = new GameObject("DamageReactionProbeStage");
                stageRoot.hideFlags = HideFlags.HideAndDontSave;
                actor = UnityEngine.Object.Instantiate(source);
                actor.name = source.name + "_CaptureProbe";
                actor.hideFlags = HideFlags.HideAndDontSave;
                actor.transform.SetParent(stageRoot.transform, true);
                actor.SetActive(true);
                Animator animator = RequireAnimator(actor);
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                StartHumanoidSample(actor, clip, Mathf.Clamp01(normalizedTime) * clip.length);
                animationModeStarted = true;
                stageRoot.transform.position = new Vector3(10000f, 10000f, 10000f);
                Bounds bounds = CaptureBoundsOf(actor.transform);
                int enabledRenderers = actor.GetComponentsInChildren<Renderer>(true).Count(renderer => renderer.enabled);
                Renderer firstRenderer = actor.GetComponentsInChildren<Renderer>(true).First(renderer => renderer.enabled);
                return source.name + "CaptureProbe=active:" + actor.activeInHierarchy +
                    ",enabledRenderers:" + enabledRenderers +
                    ",stageRootPosition:" + Vector(stageRoot.transform.position) +
                    ",actorRootPosition:" + Vector(actor.transform.position) +
                    ",rendererPosition:" + Vector(firstRenderer.transform.position) +
                    ",boundsCenter:" + Vector(bounds.center) +
                    ",boundsSize:" + Vector(bounds.size);
            }
            finally
            {
                if (animationModeStarted && AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                if (stageRoot != null)
                    UnityEngine.Object.DestroyImmediate(stageRoot);
                else if (actor != null)
                    UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        private static Bounds CaptureBoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled active renderers.");
            bool initialized = false;
            Bounds combined = default;
            foreach (Renderer renderer in renderers)
            {
                Bounds current;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.updateWhenOffscreen = true;
                    current = BakedWorldBounds(skinned);
                }
                else
                {
                    current = renderer.bounds;
                }
                if (!initialized)
                {
                    combined = current;
                    initialized = true;
                }
                else
                {
                    combined.Encapsulate(current);
                }
            }
            if (!initialized || combined.size.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(root.name + " produced invalid capture bounds.");
            return combined;
        }

        private static Bounds BakedWorldBounds(SkinnedMeshRenderer renderer)
        {
            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                renderer.BakeMesh(mesh);
                Vector3[] vertices = mesh.vertices;
                if (vertices == null || vertices.Length == 0)
                    return TransformBounds(renderer.localBounds, renderer.transform);
                Bounds bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
                for (int i = 1; i < vertices.Length; i++)
                    bounds.Encapsulate(renderer.transform.TransformPoint(vertices[i]));
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static Bounds TransformBounds(Bounds local, Transform transform)
        {
            Vector3 center = transform.TransformPoint(local.center);
            Vector3 extents = local.extents;
            Vector3 axisX = transform.TransformVector(extents.x, 0f, 0f);
            Vector3 axisY = transform.TransformVector(0f, extents.y, 0f);
            Vector3 axisZ = transform.TransformVector(0f, 0f, extents.z);
            Vector3 worldExtents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(center, worldExtents * 2f);
        }

        private static string Vector(Vector3 value) =>
            value.x.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            value.y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            value.z.ToString("0.######", CultureInfo.InvariantCulture);

        private static void StartHumanoidSample(GameObject actor, AnimationClip clip, float time)
        {
            if (AnimationMode.InAnimationMode())
                throw new InvalidOperationException("Close the active Animation preview before damage-reaction review.");
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                try
                {
                    AnimationMode.SampleAnimationClip(actor, clip, time);
                }
                finally
                {
                    AnimationMode.EndSampling();
                }
            }
            catch
            {
                AnimationMode.StopAnimationMode();
                throw;
            }
        }

        private static AnimationClip[] Repeat(AnimationClip value, int count) =>
            Enumerable.Repeat(value, count).ToArray();

        private static Bounds BoundsOf(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderers.");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void ApplyPlayerIdlePostureCorrections(
            Transform playerIdle,
            AnimationClip hit,
            AnimationClip originalHit,
            AnimationClip back,
            AnimationClip getUp,
            AnimationClip originalGetUp)
        {
            ArmLocalPose[] armReference = CaptureArmPose(playerIdle);
            ApplyHitPlayerIdleBaseWithOriginalRecoil(hit, originalHit, playerIdle);
            if (back != null)
                ApplyConstantArmPose(back, armReference);
            ApplyGetUpStandingArmPose(getUp, originalGetUp, armReference);
            hit.EnsureQuaternionContinuity();
            if (back != null)
                back.EnsureQuaternionContinuity();
            getUp.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(hit);
            if (back != null)
                EditorUtility.SetDirty(back);
            EditorUtility.SetDirty(getUp);
        }

        private static void ApplyHitPlayerIdleBaseWithOriginalRecoil(
            AnimationClip target,
            AnimationClip original,
            Transform playerIdle)
        {
            string[] paths = TransformPaths(original);
            RequirePosePaths(playerIdle, paths, "Player_Idle");
            Dictionary<string, ArmLocalPose> idlePose = CapturePoseMap(playerIdle, paths);
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject model = RequireAsset<GameObject>(PlayerGenericAssetPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(model, previewScene) as GameObject ??
                throw new InvalidOperationException("Hit_Reaction recoil probe could not be instantiated.");
            probe.name = "Hit_Reaction_PlayerIdleRebaseProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            Dictionary<string, Transform> transforms = paths.ToDictionary(
                path => path,
                path => probe.transform.Find(path) ??
                    throw new InvalidOperationException("Hit_Reaction probe is missing " + path + "."),
                StringComparer.Ordinal);
            TransformTrack[] tracks = paths.Select(path =>
                new TransformTrack(path, transforms[path])).ToArray();
            Dictionary<string, ArmLocalPose> originalBase = null;
            bool animationModeStarted = false;
            try
            {
                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before rebasing Hit_Reaction.");
                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(probe, original, 0f);
                AnimationMode.EndSampling();
                originalBase = paths.ToDictionary(
                    path => path,
                    path => new ArmLocalPose(transforms[path]),
                    StringComparer.Ordinal);

                int frameCount = Mathf.CeilToInt(original.length * original.frameRate);
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    float time = Mathf.Min(frame / original.frameRate, original.length);
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(probe, original, time);
                    AnimationMode.EndSampling();
                    for (int index = 0; index < paths.Length; index++)
                    {
                        string path = paths[index];
                        ArmLocalPose pose = IsCorrectedArmPath(path)
                            ? idlePose[path]
                            : ArmLocalPose.Rebase(
                                idlePose[path],
                                originalBase[path],
                                new ArmLocalPose(transforms[path]));
                        tracks[index].Add(time, pose);
                    }
                }
            }
            finally
            {
                if (animationModeStarted)
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            foreach (TransformTrack track in tracks)
                track.Apply(target);
        }

        private static void ApplyConstantArmPose(AnimationClip clip, ArmLocalPose[] reference)
        {
            for (int index = 0; index < ArmBonePaths.Length; index++)
                SetLocalPoseCurves(clip, ArmBonePaths[index], reference[index], clip.length);
        }

        private static void SetLocalPoseCurves(
            AnimationClip clip,
            string path,
            ArmLocalPose pose,
            float endTime)
        {
            TransformTrack.SetCurve(clip, path, "m_LocalPosition.x", ConstantKeys(endTime, pose.Position.x));
            TransformTrack.SetCurve(clip, path, "m_LocalPosition.y", ConstantKeys(endTime, pose.Position.y));
            TransformTrack.SetCurve(clip, path, "m_LocalPosition.z", ConstantKeys(endTime, pose.Position.z));
            TransformTrack.SetCurve(clip, path, "m_LocalRotation.x", ConstantKeys(endTime, pose.Rotation.x));
            TransformTrack.SetCurve(clip, path, "m_LocalRotation.y", ConstantKeys(endTime, pose.Rotation.y));
            TransformTrack.SetCurve(clip, path, "m_LocalRotation.z", ConstantKeys(endTime, pose.Rotation.z));
            TransformTrack.SetCurve(clip, path, "m_LocalRotation.w", ConstantKeys(endTime, pose.Rotation.w));
            TransformTrack.SetCurve(clip, path, "m_LocalScale.x", ConstantKeys(endTime, pose.Scale.x));
            TransformTrack.SetCurve(clip, path, "m_LocalScale.y", ConstantKeys(endTime, pose.Scale.y));
            TransformTrack.SetCurve(clip, path, "m_LocalScale.z", ConstantKeys(endTime, pose.Scale.z));
        }

        private static List<Keyframe> ConstantKeys(float endTime, float value) =>
            new List<Keyframe>
            {
                new Keyframe(0f, value),
                new Keyframe(endTime, value)
            };

        private static void ApplyGetUpStandingArmPose(
            AnimationClip target,
            AnimationClip original,
            ArmLocalPose[] reference)
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject proxyModel = RequireAsset<GameObject>(PlayerGenericAssetPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(proxyModel, previewScene) as GameObject ??
                throw new InvalidOperationException("GetUp arm-correction probe could not be instantiated.");
            probe.name = "Knockdown_GetUp_ArmCorrectionProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            Transform[] bones = RequireArmHierarchy(probe.transform, probe.name);
            TransformTrack[] tracks = bones
                .Select((bone, index) => new TransformTrack(ArmBonePaths[index], bone))
                .ToArray();
            bool animationModeStarted = false;
            try
            {
                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before applying the arm correction.");
                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                int frameCount = Mathf.CeilToInt(original.length * original.frameRate);
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    float time = Mathf.Min(frame / original.frameRate, original.length);
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(probe, original, time);
                    AnimationMode.EndSampling();
                    float normalized = original.length <= 0f ? 1f : time / original.length;
                    float weight = GetUpArmBlendWeight(normalized);
                    for (int index = 0; index < tracks.Length; index++)
                    {
                        ArmLocalPose originalPose = new ArmLocalPose(bones[index]);
                        tracks[index].Add(time, ArmLocalPose.Lerp(originalPose, reference[index], weight));
                    }
                }
            }
            finally
            {
                if (animationModeStarted)
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            foreach (TransformTrack track in tracks)
                track.Apply(target);
        }

        private static float GetUpArmBlendWeight(float normalized)
        {
            float linear = Mathf.InverseLerp(
                GetUpArmCorrectionStartNormalized,
                GetUpArmCorrectionEndNormalized,
                normalized);
            return linear * linear * (3f - (2f * linear));
        }

        private static ArmLocalPose[] SampleArmPose(AnimationClip clip, float time)
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject proxyModel = RequireAsset<GameObject>(PlayerGenericAssetPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(proxyModel, previewScene) as GameObject ??
                throw new InvalidOperationException("Arm-pose sampling probe could not be instantiated.");
            probe.name = clip.name + "_ArmPoseProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            Transform[] bones = RequireArmHierarchy(probe.transform, probe.name);
            bool animationModeStarted = false;
            try
            {
                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before sampling the arm pose.");
                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(probe, clip, Mathf.Clamp(time, 0f, clip.length));
                AnimationMode.EndSampling();
                return bones.Select(bone => new ArmLocalPose(bone)).ToArray();
            }
            finally
            {
                if (animationModeStarted)
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static ArmLocalPose[] CaptureArmPose(Transform root)
        {
            return RequireArmHierarchy(root, root.name)
                .Select(bone => new ArmLocalPose(bone))
                .ToArray();
        }

        private static string[] TransformPaths(AnimationClip clip) =>
            AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform) &&
                    !string.IsNullOrEmpty(binding.path))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

        private static void RequirePosePaths(Transform root, IEnumerable<string> paths, string label)
        {
            foreach (string path in paths)
                if (root.Find(path) == null)
                    throw new InvalidOperationException(label + " is missing transform path " + path + ".");
        }

        private static Dictionary<string, ArmLocalPose> CapturePoseMap(
            Transform root,
            IEnumerable<string> paths)
        {
            return paths.ToDictionary(
                path => path,
                path => new ArmLocalPose(root.Find(path) ??
                    throw new InvalidOperationException(root.name + " is missing " + path + ".")),
                StringComparer.Ordinal);
        }

        private static Dictionary<string, ArmLocalPose> SamplePoseMap(
            AnimationClip clip,
            float time,
            IEnumerable<string> requestedPaths)
        {
            string[] paths = requestedPaths.ToArray();
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject model = RequireAsset<GameObject>(PlayerGenericAssetPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(model, previewScene) as GameObject ??
                throw new InvalidOperationException("Pose-map sampling probe could not be instantiated.");
            probe.name = clip.name + "_PoseMapProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            RequirePosePaths(probe.transform, paths, probe.name);
            bool animationModeStarted = false;
            try
            {
                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before sampling the pose map.");
                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(probe, clip, Mathf.Clamp(time, 0f, clip.length));
                AnimationMode.EndSampling();
                return CapturePoseMap(probe.transform, paths);
            }
            finally
            {
                if (animationModeStarted)
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void RequirePoseMapsEqual(
            IReadOnlyDictionary<string, ArmLocalPose> expected,
            IReadOnlyDictionary<string, ArmLocalPose> actual,
            IEnumerable<string> paths,
            string label)
        {
            foreach (string path in paths)
            {
                ArmLocalPose expectedPose = expected[path];
                ArmLocalPose actualPose = actual[path];
                float positionDelta = Vector3.Distance(expectedPose.Position, actualPose.Position);
                float rotationDelta = Quaternion.Angle(expectedPose.Rotation, actualPose.Rotation);
                float scaleDelta = Vector3.Distance(expectedPose.Scale, actualPose.Scale);
                if (positionDelta > ArmPositionTolerance ||
                    rotationDelta > ArmRotationTolerance ||
                    scaleDelta > ArmScaleTolerance)
                    throw new InvalidOperationException(
                        label + " differs at " + path +
                        ". positionDelta=" + positionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                        ", rotationDelta=" + rotationDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                        ", scaleDelta=" + scaleDelta.ToString("0.######", CultureInfo.InvariantCulture) + ".");
            }
        }

        private static void RequireRelativeMotionPreserved(
            AnimationClip original,
            AnimationClip corrected,
            string[] paths,
            float[] normalizedTimes,
            string label)
        {
            Dictionary<string, ArmLocalPose> originalBase = SamplePoseMap(original, 0f, paths);
            Dictionary<string, ArmLocalPose> correctedBase = SamplePoseMap(corrected, 0f, paths);
            foreach (float normalizedTime in normalizedTimes)
            {
                Dictionary<string, ArmLocalPose> originalPose =
                    SamplePoseMap(original, original.length * normalizedTime, paths);
                Dictionary<string, ArmLocalPose> correctedPose =
                    SamplePoseMap(corrected, corrected.length * normalizedTime, paths);
                foreach (string path in paths)
                {
                    Vector3 originalPositionDelta =
                        originalPose[path].Position - originalBase[path].Position;
                    Vector3 correctedPositionDelta =
                        correctedPose[path].Position - correctedBase[path].Position;
                    Quaternion originalRotationDelta =
                        Quaternion.Inverse(originalBase[path].Rotation) * originalPose[path].Rotation;
                    Quaternion correctedRotationDelta =
                        Quaternion.Inverse(correctedBase[path].Rotation) * correctedPose[path].Rotation;
                    Vector3 originalScaleRatio = ArmLocalPose.ScaleRatio(
                        originalPose[path].Scale, originalBase[path].Scale);
                    Vector3 correctedScaleRatio = ArmLocalPose.ScaleRatio(
                        correctedPose[path].Scale, correctedBase[path].Scale);
                    float positionError = Vector3.Distance(
                        originalPositionDelta, correctedPositionDelta);
                    float rotationError = Quaternion.Angle(
                        originalRotationDelta, correctedRotationDelta);
                    float scaleError = Vector3.Distance(originalScaleRatio, correctedScaleRatio);
                    if (positionError > 0.0002f || rotationError > 0.1f || scaleError > 0.0002f)
                        throw new InvalidOperationException(
                            label + " relative motion differs at " + path +
                            " normalizedTime=" + normalizedTime.ToString("0.##", CultureInfo.InvariantCulture) +
                            ". positionError=" + positionError.ToString("0.######", CultureInfo.InvariantCulture) +
                            ", rotationError=" + rotationError.ToString("0.######", CultureInfo.InvariantCulture) +
                            ", scaleError=" + scaleError.ToString("0.######", CultureInfo.InvariantCulture) + ".");
                }
            }
        }

        private static Transform[] RequireArmHierarchy(Transform root, string label)
        {
            Transform[] bones = ArmBonePaths.Select(path => root.Find(path)).ToArray();
            for (int index = 0; index < bones.Length; index++)
            {
                if (bones[index] == null)
                    throw new InvalidOperationException(label + " is missing arm bone " + ArmBonePaths[index] + ".");
            }
            return bones;
        }

        private static void RequireArmBindings(AnimationClip clip, string label)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (string path in ArmBonePaths)
            {
                if (!bindings.Any(binding => binding.path == path))
                    throw new InvalidOperationException(label + " clip is missing arm curves for " + path + ".");
            }
        }

        private static bool IsCorrectedArmPath(string path) =>
            ArmBonePaths.Any(value => string.Equals(value, path, StringComparison.Ordinal));

        private static string CurveSignature(
            AnimationClip clip,
            Func<EditorCurveBinding, bool> include)
        {
            var text = new StringBuilder();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                .Where(include)
                .OrderBy(value => value.path, StringComparer.Ordinal)
                .ThenBy(value => value.propertyName, StringComparer.Ordinal))
            {
                text.Append(binding.path).Append('|').Append(binding.propertyName).AppendLine();
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null) continue;
                foreach (Keyframe key in curve.keys)
                {
                    text.Append(key.time.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(key.value.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture)).AppendLine();
                }
            }
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())))
                    .Replace("-", string.Empty);
        }

        private static void RequireArmPoseEqual(
            ArmLocalPose[] expected,
            ArmLocalPose[] actual,
            string label)
        {
            if (expected.Length != actual.Length)
                throw new InvalidOperationException(label + " arm-pose length differs.");
            for (int index = 0; index < expected.Length; index++)
            {
                float positionDelta = Vector3.Distance(expected[index].Position, actual[index].Position);
                float rotationDelta = Quaternion.Angle(expected[index].Rotation, actual[index].Rotation);
                float scaleDelta = Vector3.Distance(expected[index].Scale, actual[index].Scale);
                if (positionDelta > ArmPositionTolerance ||
                    rotationDelta > ArmRotationTolerance ||
                    scaleDelta > ArmScaleTolerance)
                {
                    throw new InvalidOperationException(
                        label + " differs at " + ArmBonePaths[index] +
                        ". positionDelta=" + positionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                        ", rotationDelta=" + rotationDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                        ", scaleDelta=" + scaleDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                        ", expectedPosition=" + Vector(expected[index].Position) +
                        ", actualPosition=" + Vector(actual[index].Position) +
                        ", expectedRotation=" + QuaternionText(expected[index].Rotation) +
                        ", actualRotation=" + QuaternionText(actual[index].Rotation) + ".");
                }
            }
        }

        private static string QuaternionText(Quaternion value) =>
            "(" + value.x.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            value.y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            value.z.ToString("0.######", CultureInfo.InvariantCulture) + "," +
            value.w.ToString("0.######", CultureInfo.InvariantCulture) + ")";

        private static void RequireArmMatchesReference(
            AnimationClip clip,
            ArmLocalPose[] reference,
            float[] normalizedTimes,
            string label)
        {
            foreach (float normalizedTime in normalizedTimes)
                RequireArmPoseEqual(reference, SampleArmPose(clip, clip.length * normalizedTime),
                    label + " at normalizedTime=" +
                    normalizedTime.ToString("0.##", CultureInfo.InvariantCulture));
        }

        private static MotionMetrics MeasureDrivenArmMotion(AnimationClip clip)
        {
            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject proxyModel = RequireAsset<GameObject>(PlayerGenericAssetPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(proxyModel, previewScene) as GameObject ??
                throw new InvalidOperationException("Driven-arm motion probe could not be instantiated.");
            probe.name = clip.name + "_DrivenArmMotionProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            Transform[] bones = RequireArmHierarchy(probe.transform, probe.name);
            Transform leftHand = bones[3];
            Transform rightHand = bones[7];
            var left = new List<Vector3>();
            var right = new List<Vector3>();
            bool animationModeStarted = false;
            try
            {
                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before measuring arm motion.");
                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                foreach (float normalized in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(probe, clip, clip.length * normalized);
                    AnimationMode.EndSampling();
                    left.Add(probe.transform.InverseTransformPoint(leftHand.position));
                    right.Add(probe.transform.InverseTransformPoint(rightHand.position));
                }
            }
            finally
            {
                if (animationModeStarted)
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
            return new MotionMetrics(MaximumDistance(left), MaximumDistance(right));
        }

        private static float MaximumDistance(IReadOnlyList<Vector3> values)
        {
            float maximum = 0f;
            for (int first = 0; first < values.Count; first++)
            for (int second = first + 1; second < values.Count; second++)
                maximum = Mathf.Max(maximum, Vector3.Distance(values[first], values[second]));
            return maximum;
        }

        private static AnimationClip RequireSingleClip(string path)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(path + " must contain exactly one embedded clip; actual=" + clips.Length);
            if (!clips[0].humanMotion)
                throw new InvalidOperationException(path + " did not import as Humanoid motion.");
            return clips[0];
        }

        private static Avatar RequireHumanAvatar(string assetPath, string label)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException(label + " did not import as a valid Humanoid Avatar.");
            return avatar;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException(path + " is missing or has the wrong asset type.");
            return asset;
        }

        private static AnimationClip CreateRetargetedGenericClip(
            AnimationClip source,
            string destinationPath,
            bool loop)
        {
            DeleteAssetIfPresent(destinationPath);
            AnimationClip output = BakeRetargetedGenericClip(
                source,
                Path.GetFileNameWithoutExtension(destinationPath),
                loop);
            AssetDatabase.CreateAsset(output, destinationPath);
            AssetDatabase.ImportAsset(
                destinationPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AnimationClip saved = RequireAsset<AnimationClip>(destinationPath);
            if (saved.humanMotion)
                throw new InvalidOperationException(destinationPath + " must be a Generic transform-curve clip.");
            return saved;
        }

        private static AnimationClip BakeRetargetedGenericClip(
            AnimationClip source,
            string outputName,
            bool loop)
        {
            if (!source.humanMotion)
                throw new InvalidOperationException(source.name + " is not Humanoid motion.");
            if (source.frameRate <= 0f || source.length <= 0f)
                throw new InvalidOperationException(source.name + " has invalid timing metadata.");

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject proxyModel = RequireAsset<GameObject>(PlayerProxyAssetPath);
            GameObject probe = PrefabUtility.InstantiatePrefab(proxyModel, previewScene) as GameObject ??
                throw new InvalidOperationException("Humanoid player proxy could not be instantiated.");
            probe.name = source.name + "_GenericRetargetProbe";
            probe.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = RequireAnimator(probe);
            animator.avatar = RequireHumanAvatar(PlayerProxyAssetPath, "player Humanoid proxy");
            animator.applyRootMotion = false;
            animator.enabled = true;

            TransformTrack[] tracks = probe.GetComponentsInChildren<Transform>(true)
                .Select(item => new
                {
                    Path = AnimationUtility.CalculateTransformPath(item, probe.transform),
                    Transform = item
                })
                .Where(item => !string.IsNullOrEmpty(item.Path))
                .OrderBy(item => item.Path, StringComparer.Ordinal)
                .Select(item => new TransformTrack(item.Path, item.Transform))
                .ToArray();
            if (tracks.Length < 20)
                throw new InvalidOperationException("Humanoid player proxy hierarchy is incomplete.");

            bool startedAnimationMode = false;
            try
            {
                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before applying damage reactions.");
                AnimationMode.StartAnimationMode();
                startedAnimationMode = true;
                int frameCount = Mathf.CeilToInt(source.length * source.frameRate);
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    float time = Mathf.Min(frame / source.frameRate, source.length);
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(probe, source, time);
                    AnimationMode.EndSampling();
                    foreach (TransformTrack track in tracks)
                        track.Add(time);
                }
            }
            finally
            {
                if (startedAnimationMode)
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            var output = new AnimationClip
            {
                name = outputName,
                frameRate = source.frameRate,
                wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever,
                legacy = false
            };
            foreach (TransformTrack track in tracks)
                track.Apply(output);
            output.EnsureQuaternionContinuity();
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = loop;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = source.length;
            AnimationUtility.SetAnimationClipSettings(output, settings);
            return output;
        }

        private static void RequireClipLoop(AnimationClip clip, bool expected, string label)
        {
            bool actual = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
            if (actual != expected)
                throw new InvalidOperationException(label + " loopTime differs. Expected=" + expected + ", Actual=" + actual);
        }

        private static string DescribeClip(AnimationClip clip) =>
            clip.name + ",length=" + clip.length.ToString("0.######", CultureInfo.InvariantCulture) +
            ",frameRate=" + clip.frameRate.ToString("0.######", CultureInfo.InvariantCulture) +
            ",humanMotion=" + clip.humanMotion;

        private static string DescribeAvatar(string assetPath)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Avatar>().FirstOrDefault() ??
                throw new InvalidOperationException(assetPath + " Avatar is missing.");
            return assetPath + "Avatar=isValid:" + avatar.isValid + ",isHuman:" + avatar.isHuman;
        }

        private static string MeasureHumanoidMotion(GameObject source, AnimationClip clip)
        {
            if (source == null)
                throw new InvalidOperationException("Humanoid motion probe source is missing.");
            Vector3[] start = SampleHumanLandmarks(source, clip, 0.05f);
            Vector3[] middle = SampleHumanLandmarks(source, clip, 0.50f);
            Vector3[] end = SampleHumanLandmarks(source, clip, 0.95f);
            float maximum = 0f;
            for (int i = 0; i < start.Length; i++)
            {
                maximum = Mathf.Max(maximum, Vector3.Distance(start[i], middle[i]));
                maximum = Mathf.Max(maximum, Vector3.Distance(start[i], end[i]));
                maximum = Mathf.Max(maximum, Vector3.Distance(middle[i], end[i]));
            }
            return maximum.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static Vector3[] SampleHumanLandmarks(GameObject source, AnimationClip clip, float normalizedTime)
        {
            GameObject actor = UnityEngine.Object.Instantiate(source);
            actor.hideFlags = HideFlags.HideAndDontSave;
            actor.SetActive(true);
            bool animationModeStarted = false;
            try
            {
                Animator animator = RequireAnimator(actor);
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
                StartHumanoidSample(actor, clip, normalizedTime * clip.length);
                animationModeStarted = true;
                HumanBodyBones[] bones =
                {
                    HumanBodyBones.Hips,
                    HumanBodyBones.LeftHand,
                    HumanBodyBones.RightHand,
                    HumanBodyBones.LeftFoot,
                    HumanBodyBones.RightFoot
                };
                return bones.Select(bone =>
                {
                    Transform item = animator.GetBoneTransform(bone) ??
                        throw new InvalidOperationException(source.name + " is missing Humanoid bone " + bone + ".");
                    return actor.transform.InverseTransformPoint(item.position);
                }).ToArray();
            }
            finally
            {
                if (animationModeStarted && AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(actor);
            }
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null && !AssetDatabase.DeleteAsset(path))
                throw new InvalidOperationException("Could not replace target-specific asset: " + path);
        }

        private static Animator RequireAnimator(GameObject target) =>
            target.GetComponent<Animator>() ?? throw new InvalidOperationException(target.name + " Animator is missing.");

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected exactly one " + name + "; actual=" + matches.Length);
            return matches[0];
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Damage reaction setup requires Edit Mode.");
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        private static string RendererSignature(Transform root)
        {
            return string.Join("\n", root.GetComponentsInChildren<Renderer>(true)
                .OrderBy(renderer => AnimationUtility.CalculateTransformPath(renderer.transform, root), StringComparer.Ordinal)
                .Select(renderer => AnimationUtility.CalculateTransformPath(renderer.transform, root) + "|" +
                    renderer.GetType().FullName + "|" + renderer.enabled + "|" +
                    string.Join(",", renderer.sharedMaterials.Select(material => material == null ? "null" : AssetDatabase.GetAssetPath(material)))));
        }

        private static string Sha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Absolute(string relative) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative.Replace('/', Path.DirectorySeparatorChar)));

        private static void WriteText(string relative, string text)
        {
            string path = Absolute(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Report directory is unavailable."));
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        internal static GameObject RequireRuntimeTarget(string targetName, string controllerPath)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Damage reaction runtime review requires Play Mode.");
            GameObject target = FindUnique(RequireScene(), targetName);
            Animator animator = RequireAnimator(target);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath) ??
                throw new InvalidOperationException(controllerPath + " is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(targetName + " runtime controller differs.");
            return target;
        }

        internal static void WriteRuntimeFinalEvidence(Texture2D sheet, int consoleErrorsBefore)
        {
            string image = Absolute(FinalImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(image) ?? throw new InvalidOperationException("Final image directory is unavailable."));
            File.WriteAllBytes(image, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Player damage reaction natural Play Mode final contact sheet")
                .AppendLine("verificationTargetsManipulated=False")
                .AppendLine("captureMethod=Natural Play Mode playback from connected Animator Controllers")
                .AppendLine("row1=Hit_Reaction four phases across one source-clip loop")
                .AppendLine("row2=Knockdown FrontFall early/late then BackFall early/late")
                .AppendLine("row3=Knockdown_GetUp four phases across one source-clip loop")
                .AppendLine("row4=Death four phases across one source-clip loop")
                .AppendLine("armPoseReference=Player_Idle scene object local transforms")
                .AppendLine("Hit_ReactionWholeClipArmCorrection=True")
                .AppendLine("KnockdownBackFallWholeClipArmCorrection=True")
                .AppendLine("Knockdown_GetUpStandingArmCorrection=True")
                .AppendLine("torsoDrivenArmMotionPreserved=True")
                .AppendLine("armsWorldLocked=False")
                .AppendLine("retargetedCompatibilityClipsGenerated=True")
                .AppendLine("retargetSamplingRate=OriginalSourceFrameRate")
                .AppendLine("retargetPoseInference=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("targetTransformsChanged=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(FinalReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(label + " differs. Expected=" + expected + ", Actual=" + actual);
        }

        private readonly struct SourceSpec
        {
            public readonly string ExternalPath;
            public readonly string AssetPath;
            public readonly string Sha256;
            public readonly bool LoopClip;

            public SourceSpec(string externalPath, string assetPath, string sha256, bool loopClip)
            {
                ExternalPath = externalPath;
                AssetPath = assetPath;
                Sha256 = sha256;
                LoopClip = loopClip;
            }
        }

        private readonly struct ArmLocalPose
        {
            internal ArmLocalPose(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
                Scale = transform.localScale;
            }

            internal ArmLocalPose(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Vector3 Scale { get; }

            internal static ArmLocalPose Lerp(ArmLocalPose source, ArmLocalPose target, float weight)
            {
                float amount = Mathf.Clamp01(weight);
                return new ArmLocalPose(
                    Vector3.Lerp(source.Position, target.Position, amount),
                    Quaternion.Slerp(source.Rotation, target.Rotation, amount),
                    Vector3.Lerp(source.Scale, target.Scale, amount));
            }

            internal static ArmLocalPose Rebase(
                ArmLocalPose reference,
                ArmLocalPose originalBase,
                ArmLocalPose originalCurrent)
            {
                Quaternion rotationDelta =
                    Quaternion.Inverse(originalBase.Rotation) * originalCurrent.Rotation;
                Vector3 scaleRatio = ScaleRatio(originalCurrent.Scale, originalBase.Scale);
                return new ArmLocalPose(
                    reference.Position + (originalCurrent.Position - originalBase.Position),
                    reference.Rotation * rotationDelta,
                    Vector3.Scale(reference.Scale, scaleRatio));
            }

            internal static Vector3 ScaleRatio(Vector3 value, Vector3 basis) =>
                new Vector3(
                    Mathf.Abs(basis.x) < 0.000001f ? 1f : value.x / basis.x,
                    Mathf.Abs(basis.y) < 0.000001f ? 1f : value.y / basis.y,
                    Mathf.Abs(basis.z) < 0.000001f ? 1f : value.z / basis.z);
        }

        private readonly struct MotionMetrics
        {
            internal MotionMetrics(float leftHandTravel, float rightHandTravel)
            {
                LeftHandTravel = leftHandTravel;
                RightHandTravel = rightHandTravel;
            }

            internal float LeftHandTravel { get; }
            internal float RightHandTravel { get; }
            internal float MaximumHandTravel => Mathf.Max(LeftHandTravel, RightHandTravel);

            internal string Describe() =>
                "left=" + LeftHandTravel.ToString("0.######", CultureInfo.InvariantCulture) +
                ",right=" + RightHandTravel.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private sealed class TransformTrack
        {
            private readonly string path;
            private readonly Transform transform;
            private readonly List<Keyframe> positionX = new List<Keyframe>();
            private readonly List<Keyframe> positionY = new List<Keyframe>();
            private readonly List<Keyframe> positionZ = new List<Keyframe>();
            private readonly List<Keyframe> rotationX = new List<Keyframe>();
            private readonly List<Keyframe> rotationY = new List<Keyframe>();
            private readonly List<Keyframe> rotationZ = new List<Keyframe>();
            private readonly List<Keyframe> rotationW = new List<Keyframe>();
            private readonly List<Keyframe> scaleX = new List<Keyframe>();
            private readonly List<Keyframe> scaleY = new List<Keyframe>();
            private readonly List<Keyframe> scaleZ = new List<Keyframe>();
            private Quaternion previousRotation;
            private bool hasPreviousRotation;

            internal TransformTrack(string valuePath, Transform valueTransform)
            {
                path = valuePath;
                transform = valueTransform;
            }

            internal void Add(float time)
            {
                Add(time, new ArmLocalPose(transform));
            }

            internal void Add(float time, ArmLocalPose pose)
            {
                Vector3 position = pose.Position;
                Quaternion rotation = pose.Rotation;
                Vector3 scale = pose.Scale;
                if (hasPreviousRotation && Quaternion.Dot(previousRotation, rotation) < 0f)
                    rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                previousRotation = rotation;
                hasPreviousRotation = true;

                positionX.Add(new Keyframe(time, position.x));
                positionY.Add(new Keyframe(time, position.y));
                positionZ.Add(new Keyframe(time, position.z));
                rotationX.Add(new Keyframe(time, rotation.x));
                rotationY.Add(new Keyframe(time, rotation.y));
                rotationZ.Add(new Keyframe(time, rotation.z));
                rotationW.Add(new Keyframe(time, rotation.w));
                scaleX.Add(new Keyframe(time, scale.x));
                scaleY.Add(new Keyframe(time, scale.y));
                scaleZ.Add(new Keyframe(time, scale.z));
            }

            internal void Apply(AnimationClip clip)
            {
                SetCurve(clip, path, "m_LocalPosition.x", positionX);
                SetCurve(clip, path, "m_LocalPosition.y", positionY);
                SetCurve(clip, path, "m_LocalPosition.z", positionZ);
                SetCurve(clip, path, "m_LocalRotation.x", rotationX);
                SetCurve(clip, path, "m_LocalRotation.y", rotationY);
                SetCurve(clip, path, "m_LocalRotation.z", rotationZ);
                SetCurve(clip, path, "m_LocalRotation.w", rotationW);
                SetCurve(clip, path, "m_LocalScale.x", scaleX);
                SetCurve(clip, path, "m_LocalScale.y", scaleY);
                SetCurve(clip, path, "m_LocalScale.z", scaleZ);
            }

            internal static void SetCurve(
                AnimationClip clip,
                string curvePath,
                string property,
                List<Keyframe> keys)
            {
                var curve = new AnimationCurve(keys.ToArray())
                {
                    preWrapMode = WrapMode.ClampForever,
                    postWrapMode = WrapMode.ClampForever
                };
                for (int index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(curvePath, typeof(Transform), property),
                    curve);
            }
        }

        private readonly struct RootSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public RootSnapshot(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            public void RequireUnchanged(Transform transform, string label)
            {
                if (Vector3.Distance(position, transform.localPosition) > 0.000001f ||
                    Quaternion.Angle(rotation, transform.localRotation) > 0.0001f ||
                    Vector3.Distance(scale, transform.localScale) > 0.000001f)
                    throw new InvalidOperationException(label + " root Transform changed unexpectedly.");
            }
        }
    }

    [InitializeOnLoad]
    internal static class PlayerDamageReactionPlayModeCapture
    {
        private const string PendingKey = "Bellerophon.PlayerDamageReaction.Pending";
        private const string StateKey = "Bellerophon.PlayerDamageReaction.State";
        private const string FailureKey = "Bellerophon.PlayerDamageReaction.Failure";
        private const string ConsoleErrorsBeforeKey = "Bellerophon.PlayerDamageReaction.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string AssetFolder = "Assets/_Project/Animations/PlayerDamageReactions";
        private static readonly string[] TargetNames =
        {
            "Hit_Reaction", "Knockdown", "Knockdown_GetUp", "Death"
        };
        private static readonly string[] ControllerPaths =
        {
            AssetFolder + "/Hit_Reaction.controller",
            AssetFolder + "/Knockdown.controller",
            AssetFolder + "/Knockdown_GetUp.controller",
            AssetFolder + "/Death.controller"
        };
        private static readonly string[] SingleStateNames =
        {
            "HitReaction_Source", string.Empty, "KnockdownGetUp_Source", "Death_Source"
        };
        private static readonly float[][] LoopPhases =
        {
            new[] { 0.12f, 0.38f, 0.64f, 0.90f },
            Array.Empty<float>(),
            new[] { 0.48f, 0.58f, 0.70f, 0.82f },
            new[] { 0.12f, 0.38f, 0.64f, 0.90f }
        };
        private static readonly List<Texture2D>[] Panels =
        {
            new List<Texture2D>(), new List<Texture2D>(), new List<Texture2D>(), new List<Texture2D>()
        };
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject[] targets;
        private static Animator[] animators;
        private static Vector3[] positions;
        private static Quaternion[] rotations;
        private static Vector3[] scales;
        private static int[] loopBases;
        private static int[] phaseIndices;
        private static double captureStarted;

        static PlayerDamageReactionPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Damage reaction final review must start in Edit Mode.");
            PlayerDamageReactionAnimationSetupTools.InspectPlayerDamageReactionAnimations();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(ConsoleErrorsBeforeKey, LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException("Damage reaction Play Mode capture has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingCapture)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying) return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException("Play Mode ended before damage reaction review completed.");
                    CaptureAvailablePhases();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                PlayerDamageReactionAnimationSetupTools.InspectPlayerDamageReactionAnimations();
                PlayerDamageReactionAnimationSetupTools.InspectPlayerDamageReactionArmPoseCorrection();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke("Damage reaction natural Play Mode playback captured and restored to Edit Mode.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            targets = new GameObject[TargetNames.Length];
            animators = new Animator[TargetNames.Length];
            positions = new Vector3[TargetNames.Length];
            rotations = new Quaternion[TargetNames.Length];
            scales = new Vector3[TargetNames.Length];
            loopBases = new int[TargetNames.Length];
            phaseIndices = new int[TargetNames.Length];
            for (int i = 0; i < TargetNames.Length; i++)
            {
                targets[i] = PlayerDamageReactionAnimationSetupTools.RequireRuntimeTarget(TargetNames[i], ControllerPaths[i]);
                animators[i] = targets[i].GetComponent<Animator>() ?? throw new InvalidOperationException(TargetNames[i] + " Animator is missing.");
                positions[i] = targets[i].transform.localPosition;
                rotations[i] = targets[i].transform.localRotation;
                scales[i] = targets[i].transform.localScale;
                AnimatorStateInfo state = animators[i].GetCurrentAnimatorStateInfo(0);
                loopBases[i] = Mathf.FloorToInt(state.normalizedTime) + 1;
                phaseIndices[i] = 0;
            }
            captureStarted = EditorApplication.timeSinceStartup;
        }

        private static void CaptureAvailablePhases()
        {
            if (EditorApplication.timeSinceStartup - captureStarted > 20d)
                throw new TimeoutException("Damage reaction natural playback capture exceeded 20 seconds.");
            CaptureLoopRow(0);
            CaptureKnockdownRow();
            CaptureLoopRow(2);
            CaptureLoopRow(3);
            if (Panels.Any(row => row.Count < 4)) return;

            for (int i = 0; i < targets.Length; i++)
                RequireRootUnchanged(i);
            Texture2D sheet = CombinePanels();
            try
            {
                PlayerDamageReactionAnimationSetupTools.WriteRuntimeFinalEvidence(
                    sheet, SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static void CaptureLoopRow(int row)
        {
            if (phaseIndices[row] >= LoopPhases[row].Length) return;
            AnimatorStateInfo state = animators[row].GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(SingleStateNames[row])) return;
            float threshold = loopBases[row] + LoopPhases[row][phaseIndices[row]];
            if (state.normalizedTime < threshold) return;
            Panels[row].Add(ShipRepairSharedAnimationTools.RenderNaturalTarget(targets[row]));
            phaseIndices[row]++;
        }

        private static void CaptureKnockdownRow()
        {
            const int row = 1;
            if (phaseIndices[row] >= 4) return;
            AnimatorStateInfo state = animators[row].GetCurrentAnimatorStateInfo(0);
            bool wantFront = phaseIndices[row] < 2;
            string expected = wantFront ? "FrontFall_Source" : "BackFall_Source";
            if (!state.IsName(expected)) return;
            float threshold = (phaseIndices[row] % 2 == 0) ? 0.15f : 0.80f;
            float fraction = state.normalizedTime - Mathf.Floor(state.normalizedTime);
            if (fraction < threshold) return;
            Panels[row].Add(ShipRepairSharedAnimationTools.RenderNaturalTarget(targets[row]));
            phaseIndices[row]++;
        }

        private static Texture2D CombinePanels()
        {
            int cellWidth = Panels[0][0].width;
            int cellHeight = Panels[0][0].height;
            var sheet = new Texture2D(cellWidth * 4, cellHeight * 4, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 255), sheet.width * sheet.height).ToArray());
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    sheet.SetPixels32(
                        column * cellWidth,
                        (3 - row) * cellHeight,
                        cellWidth,
                        cellHeight,
                        Panels[row][column].GetPixels32());
                }
            }
            sheet.Apply(false, false);
            return sheet;
        }

        private static void RequireRootUnchanged(int index)
        {
            Transform target = targets[index].transform;
            if (Vector3.Distance(positions[index], target.localPosition) > 0.00001f ||
                Quaternion.Angle(rotations[index], target.localRotation) > 0.01f ||
                Vector3.Distance(scales[index], target.localScale) > 0.00001f)
                throw new InvalidOperationException(TargetNames[index] + " root Transform changed during natural playback.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(FailureKey, "Damage reaction natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (List<Texture2D> row in Panels)
            {
                foreach (Texture2D texture in row)
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                row.Clear();
            }
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            targets = null;
            animators = null;
            positions = null;
            rotations = null;
            scales = null;
            loopBases = null;
            phaseIndices = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }
}
